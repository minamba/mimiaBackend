using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class MaitriseRepository : IMaitriseRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public MaitriseRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<IEnumerable<MaitriseCompetence>> GetLacunesAsync(
            int eleveId, int? matiereId, double seuil, int limite) =>
            QueryAsync(eleveId, matiereId, m => m.Score < seuil, ascendant: true, limite);

        public Task<IEnumerable<MaitriseCompetence>> GetAcquisesAsync(
            int eleveId, int? matiereId, double seuil, int limite) =>
            QueryAsync(eleveId, matiereId, m => m.Score >= seuil, ascendant: false, limite);

        /// <summary>
        /// Les notions dont l'échéance de révision est passée.
        ///
        /// POURQUOI CETTE LECTURE MANQUAIT
        /// -------------------------------
        /// `ProchaineRevision` était calculée à chaque observation, selon une
        /// vraie courbe d'espacement — une lacune revient le lendemain, une
        /// notion solide dans plusieurs semaines — et indexée. Mais RIEN ne la
        /// lisait : le calcul tournait dans le vide depuis toujours.
        ///
        /// L'ORDRE N'EST PAS CELUI DE L'ÉCHÉANCE
        /// -------------------------------------
        /// On trie par fragilité d'abord, retard ensuite. Une séance ne permet
        /// de reprendre qu'une ou deux notions : mieux vaut la plus fragile
        /// que la plus ancienne. Une notion solide en retard de trois semaines
        /// ne coûte rien à laisser attendre ; une lacune en retard d'un jour,
        /// si.
        /// </summary>
        public async Task<IEnumerable<MaitriseCompetence>> GetARevoirAsync(
            int eleveId, int? matiereId, int limite)
        {
            var maintenant = DateTime.UtcNow;

            var query = _context.MaitrisesEleves
                .AsNoTracking()
                .Include(m => m.Competence)!.ThenInclude(c => c!.NiveauScolaire)
                .Include(m => m.Competence)!.ThenInclude(c => c!.Matiere)
                .Where(m => m.EleveId == eleveId)
                .Where(m => m.ProchaineRevision != null && m.ProchaineRevision <= maintenant);

            if (matiereId.HasValue)
            {
                query = query.Where(m => m.Competence!.MatiereId == matiereId.Value);
            }

            var entities = await query
                .OrderBy(m => m.Score)
                .ThenBy(m => m.ProchaineRevision)
                .Take(limite)
                .ToListAsync();

            return entities.Select(Projeter).ToList();
        }

        // ------------------------------------------------------------------
        // Écriture : c'est ici que l'agent laisse une trace durable
        // ------------------------------------------------------------------

        /// <summary>Probabilité de maîtrise attribuée à une compétence jamais observée.</summary>
        private const double ProbaInitiale = 0.25;

        /// <summary>L'élève sait, mais se trompe quand même (inattention).</summary>
        private const double Glissement = 0.10;

        /// <summary>L'élève ne sait pas, mais tombe juste (hasard, imitation).</summary>
        private const double Chance = 0.20;

        /// <summary>Ce que l'élève apprend du seul fait d'avoir travaillé la notion.</summary>
        private const double Apprentissage = 0.12;

        public async Task<IEnumerable<CompetenceCandidate>> GetCandidatesAsync(
            int matiereId, int niveauOrdre, int margeAmont, int margeAval, CancellationToken ct = default)
        {
            // On propose le niveau de l'élève ET les précédents : c'est tout
            // l'intérêt du graphe. Un élève de 6e qui bute sur les fractions
            // doit pouvoir être rattaché à une compétence de CM1.
            var min = niveauOrdre - margeAmont;
            var max = niveauOrdre + margeAval;

            return await _context.Competences
                .AsNoTracking()
                .Where(c => c.MatiereId == matiereId
                            && c.NiveauScolaire!.Ordre >= min
                            && c.NiveauScolaire.Ordre <= max)
                .OrderBy(c => c.NiveauScolaire!.Ordre)
                .ThenBy(c => c.Ordre)
                .Select(c => new CompetenceCandidate
                {
                    Id = c.Id,
                    Code = c.Code,
                    Libelle = c.Libelle,
                    Domaine = c.Domaine,
                    NiveauLibelle = c.NiveauScolaire!.Libelle,
                    NiveauOrdre = c.NiveauScolaire.Ordre,
                })
                .ToListAsync(ct);
        }

        public async Task<int> AppliquerObservationsAsync(
            int eleveId,
            IEnumerable<ObservationCompetence> observations,
            string source,
            CancellationToken ct = default)
        {
            var liste = observations.ToList();
            if (liste.Count == 0) return 0;

            var codes = liste.Select(o => o.Code).Distinct().ToList();

            // Un code inventé par le modèle ne doit rien créer : on ne retient
            // que ce qui existe réellement au référentiel.
            var competences = await _context.Competences
                .Where(c => c.Code != null && codes.Contains(c.Code))
                .ToDictionaryAsync(c => c.Code!, c => c.Id, ct);

            if (competences.Count == 0) return 0;

            var identifiants = competences.Values.ToList();

            var existantes = await _context.MaitrisesEleves
                .Where(m => m.EleveId == eleveId && identifiants.Contains(m.CompetenceId))
                .ToDictionaryAsync(m => m.CompetenceId, ct);

            var maintenant = DateTime.UtcNow;
            var appliquees = 0;

            foreach (var observation in liste)
            {
                if (!competences.TryGetValue(observation.Code, out var competenceId)) continue;

                if (!existantes.TryGetValue(competenceId, out var maitrise))
                {
                    maitrise = new MaitriseEleve
                    {
                        EleveId = eleveId,
                        CompetenceId = competenceId,
                        Score = ProbaInitiale,
                        Confiance = 0,
                        NombreObservations = 0,
                    };

                    _context.MaitrisesEleves.Add(maitrise);
                    existantes[competenceId] = maitrise;
                }

                maitrise.Score = Recalculer(maitrise.Score, observation.Resultat);
                maitrise.NombreObservations += 1;
                maitrise.Confiance = ConfianceApres(maitrise.NombreObservations);
                maitrise.DerniereEvaluation = maintenant;
                maitrise.ProchaineRevision = ProchaineRevision(maintenant, maitrise.Score, maitrise.NombreObservations);
                maitrise.Source = source;

                appliquees += 1;
            }

            await _context.SaveChangesAsync(ct);
            return appliquees;
        }

        /// <summary>
        /// Mise à jour bayésienne du niveau de maîtrise (Bayesian Knowledge
        /// Tracing). On ne remplace pas le score par l'observation : on corrige
        /// une croyance. Une réussite isolée après dix échecs ne fait pas passer
        /// la compétence pour acquise, et c'est le comportement voulu.
        /// </summary>
        private static double Recalculer(double prior, ResultatObservation resultat)
        {
            var posterieur = resultat switch
            {
                ResultatObservation.Reussi => Posterieur(prior, correct: true),
                ResultatObservation.Echoue => Posterieur(prior, correct: false),

                // Hésitant : l'observation ne tranche pas. On prend le milieu
                // des deux lectures plutôt que d'inventer un troisième modèle.
                _ => (Posterieur(prior, true) + Posterieur(prior, false)) / 2,
            };

            // Le seul fait de travailler la notion fait progresser : c'est le
            // terme d'apprentissage du modèle, sans lui un élève resterait
            // bloqué au même score en répétant les mêmes erreurs.
            var apres = posterieur + (1 - posterieur) * Apprentissage;

            return Math.Clamp(apres, 0.02, 0.98);
        }

        private static double Posterieur(double prior, bool correct)
        {
            var numerateur = correct ? prior * (1 - Glissement) : prior * Glissement;

            var denominateur = correct
                ? prior * (1 - Glissement) + (1 - prior) * Chance
                : prior * Glissement + (1 - prior) * (1 - Chance);

            return denominateur <= 0 ? prior : numerateur / denominateur;
        }

        /// <summary>
        /// La confiance ne dépend pas du score mais du NOMBRE d'observations :
        /// un score de 0,9 après une seule réponse ne vaut pas le même score
        /// après six. C'est ce qui empêche d'annoncer une compétence acquise
        /// sur un coup de chance.
        /// </summary>
        private static double ConfianceApres(int observations) =>
            Math.Round(1 - Math.Pow(0.62, observations), 3);

        /// <summary>
        /// Révision espacée : plus la compétence est solide, plus on attend
        /// avant d'y revenir. Une lacune, elle, revient dès le lendemain.
        /// </summary>
        private static DateTime ProchaineRevision(DateTime depuis, double score, int observations) =>
            score switch
            {
                >= 0.75 => depuis.AddDays(Math.Min(60, 3 * Math.Pow(2, Math.Max(0, observations - 1)))),
                >= 0.40 => depuis.AddDays(3),
                _ => depuis.AddDays(1),
            };

        private async Task<IEnumerable<MaitriseCompetence>> QueryAsync(
            int eleveId,
            int? matiereId,
            System.Linq.Expressions.Expression<Func<MaitriseEleve, bool>> filtre,
            bool ascendant,
            int limite)
        {
            var query = _context.MaitrisesEleves
                .AsNoTracking()
                .Include(m => m.Competence)!.ThenInclude(c => c!.NiveauScolaire)
                .Include(m => m.Competence)!.ThenInclude(c => c!.Matiere)
                .Where(m => m.EleveId == eleveId)
                .Where(filtre);

            if (matiereId.HasValue)
            {
                query = query.Where(m => m.Competence!.MatiereId == matiereId.Value);
            }

            // Tri secondaire sur l'ordre du niveau : à score égal, la lacune la
            // plus en amont passe devant. C'est elle qui bloque tout le reste.
            query = ascendant
                ? query.OrderBy(m => m.Score).ThenBy(m => m.Competence!.NiveauScolaire!.Ordre)
                : query.OrderByDescending(m => m.Score).ThenByDescending(m => m.Competence!.NiveauScolaire!.Ordre);

            var entities = await query.Take(limite).ToListAsync();

            return entities.Select(Projeter).ToList();
        }

        /// <summary>Entité vers modèle. Partagé par toutes les lectures — trois copies finissaient par diverger.</summary>
        private static MaitriseCompetence Projeter(MaitriseEleve m) => new()
        {
            CompetenceId = m.CompetenceId,
            Code = m.Competence?.Code,
            Libelle = m.Competence?.Libelle,
            Domaine = m.Competence?.Domaine,
            MatiereCode = m.Competence?.Matiere?.Code,
            MatiereLibelle = m.Competence?.Matiere?.Libelle,
            NiveauCode = m.Competence?.NiveauScolaire?.Code,
            NiveauLibelle = m.Competence?.NiveauScolaire?.Libelle,
            Score = m.Score,
            Confiance = m.Confiance,
            DerniereEvaluation = m.DerniereEvaluation,
        };
    }
}
