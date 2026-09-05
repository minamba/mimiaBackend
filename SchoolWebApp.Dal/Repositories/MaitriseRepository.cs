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

            // L'ANNÉE OÙ LE TRAVAIL A EU LIEU, PAS LE NIVEAU DE LA NOTION.
            //
            // Les deux se confondent facilement et ce n'est pas la même chose :
            // un élève de 3e qui rattrape une notion de CM1 travaille EN 3e. La
            // compétence garde son niveau — celui où elle s'apprend — tandis que
            // la maîtrise porte l'année de l'observation. Sans cette distinction,
            // la fiche d'un élève de 3e afficherait un onglet « CM1 » pour une
            // année qu'il n'a jamais passée chez nous.
            var niveauId = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaireId)
                .FirstOrDefaultAsync(ct);

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

                // Réécrit à CHAQUE observation, et non seulement à la création :
                // une notion travaillée en 4e puis reprise en 3e appartient à la
                // 3e, qui est l'année où l'élève y est revenu.
                if (niveauId != 0) maitrise.NiveauScolaireId = niveauId;

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
        /// <summary>
        /// LES SEUILS SONT CEUX DU PRODUIT, recopiés depuis `MaitriseService`.
        ///
        /// Ils y sont privés, et cette copie est assumée : l'alternative était
        /// de faire remonter la lecture dans le service, qui n'a pas accès au
        /// contexte, ou de rendre publics deux nombres qui ne regardent que la
        /// pédagogie. Le jour où ils bougent, ils bougent ici aussi — c'est
        /// écrit des deux côtés.
        /// </summary>
        private const double SeuilAcquis = 0.8;

        private const double SeuilEnCours = 0.4;

        public async Task<Progression?> GetProgressionAsync(
            int eleveId, bool marquerVue, CancellationToken ct = default)
        {
            var eleve = await _context.Eleves
                .Include(e => e.NiveauScolaire)
                .FirstOrDefaultAsync(e => e.Id == eleveId, ct);

            if (eleve is null) return null;

            var vueLe = eleve.ProgressionVueLe;

            // SON NIVEAU, PLUS TOUT CE QU IL A DÉJÀ TRAVAILLÉ.
            //
            // La première version ne montrait que le niveau courant, et
            // c était faux : la maîtrise d un élève est répartie sur
            // plusieurs années. Mesuré sur les données de développement, un
            // élève de 3e avait sept notions acquises — six en 6e, une en 5e,
            // aucune en 3e. Sa carte affichait ZÉRO.
            //
            // C est même tout l intérêt du graphe, et le dépôt le dit ailleurs :
            // « un blocage en 6e vient souvent d une notion de CM1 ». Restreindre
            // au niveau courant jetait précisément ce qui a été travaillé.
            //
            // ON NE MONTRE PAS TOUT POUR AUTANT. Les mille compétences du CP
            // à la Terminale noieraient un enfant de CE1. La règle est donc :
            // ce qui est de son année — la carte à remplir — et ce qu il a
            // touché ailleurs — la carte déjà remplie.
            var deja = await _context.MaitrisesEleves
                .AsNoTracking()
                .Where(m => m.EleveId == eleveId)
                .Select(m => new { m.CompetenceId, m.Score, m.DerniereEvaluation })
                .ToListAsync(ct);

            var parCompetence = deja.ToDictionary(m => m.CompetenceId);

            var travaillees = parCompetence.Keys.ToList();

            // LE RANG D ANNÉE, ET NON L IDENTIFIANT DU NIVEAU.
            //
            // C'est la clé de toute la scolarité au lycée, et le semeur du
            // référentiel le dit : « trois classes partagent le rang 11 —
            // première générale, technologique, professionnelle. En partageant
            // le rang, une première professionnelle hérite SANS RIEN ÉCRIRE du
            // référentiel. »
            //
            // La première version comparait `NiveauScolaireId`. Six classes
            // n'ont aucune compétence à leur nom — 3e prépa-métiers, les voies
            // professionnelles et technologiques — et leur carte sortait vide,
            // alors que les 108 à 125 compétences de leur rang existaient et
            // que le professeur les utilisait déjà pour eux.
            //
            // Le reste du produit interroge le rang depuis toujours : voir
            // `GetCandidatesAsync`, qui prend un `niveauOrdre`. Cette carte
            // était le seul endroit à s'y prendre autrement.
            var rang = eleve.NiveauScolaire!.Ordre;

            var competences = await _context.Competences
                .AsNoTracking()
                .Where(co => co.NiveauScolaire!.Ordre == rang
                             || travaillees.Contains(co.Id))
                .Select(co => new
                {
                    co.Id,
                    co.Libelle,
                    co.Domaine,
                    co.Ordre,
                    co.MatiereId,
                    co.NiveauScolaireId,
                    NiveauLibelle = co.NiveauScolaire!.Libelle,
                    NiveauOrdre = co.NiveauScolaire!.Ordre,
                    MatiereLibelle = co.Matiere!.Libelle,
                    MatiereCouleur = co.Matiere!.ProfCouleur,
                })
                .ToListAsync(ct);

            // AUCUNE COMPÉTENCE N EST UNE CARTE VIDE, PAS UNE ERREUR.
            //
            // Six niveaux du référentiel n en ont aucune : 3e prépa-métiers,
            // les secondes, premières et terminales professionnelles et
            // technologiques. Rendre null y faisait répondre 404, et l enfant
            // lisait « ta carte n a pas pu être chargée » — un message de
            // panne pour une absence de données.
            //
            // On rend donc une carte vide, que l écran sait présenter. Le seul
            // null qui subsiste est celui d un élève qui n existe pas.

            var progression = new Progression
            {
                Niveau = await _context.NiveauxScolaires
                    .AsNoTracking()
                    .Where(n => n.Id == eleve.NiveauScolaireId)
                    .Select(n => n.Libelle)
                    .FirstOrDefaultAsync(ct),

                // Lu AVANT que `marquerVue` ne l'écrase plus bas : après, la
                // date existe et la première visite n'a plus de trace.
                PremiereVisite = vueLe is null,
            };

            // LES MATIÈRES QUI NE SONT PAS AU PROGRAMME DE SA VOIE SORTENT.
            //
            // Le rang donne accès au référentiel de son année, toutes voies
            // confondues — c'est ce qu'on veut. Mais il n'y a pas de
            // philosophie au bac professionnel, ni de français en terminale
            // générale : les afficher mettrait devant un élève des dizaines de
            // notions qu'il ne verra jamais, et son pourcentage
            // s'effondrerait pour rien.
            //
            // La règle vit dans `VoiesScolaires`, où le reste du produit la
            // lit déjà. On ne la réécrit pas ici.
            var exclues = new HashSet<int>();

            foreach (var matiereId in competences.Select(co => co.MatiereId).Distinct())
            {
                var matiere = await _context.Matieres
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == matiereId, ct);

                if (matiere is null) { exclues.Add(matiereId); continue; }

                // LES TROIS COMPOSANTS D EstAuProgramme, ET NON L APPEL.
                //
                // Cette méthode prend les modèles du DOMAINE ; ici on tient des
                // entités de la couche de données, et les deux ne se convertissent
                // pas. On réutilise donc la seule partie qui porte une décision
                // — la table des exclusions par voie — et on recopie les deux
                // bornes, qui sont de simples comparaisons.
                var horsBornes = rang < matiere.NiveauOrdreMin || rang > matiere.NiveauOrdreMax;

                var horsVoie = VoiesScolaires
                    .NiveauxExclus(matiere.Code)
                    .Contains(eleve.NiveauScolaire!.Code, StringComparer.OrdinalIgnoreCase);

                if (horsBornes || horsVoie) exclues.Add(matiereId);
            }

            foreach (var groupe in competences
                .Where(co => !exclues.Contains(co.MatiereId))
                .GroupBy(co => new { co.MatiereId, co.MatiereLibelle, co.MatiereCouleur })
                .OrderBy(g => g.Key.MatiereLibelle))
            {
                var matiere = new MatiereProgression
                {
                    MatiereId = groupe.Key.MatiereId,
                    Libelle = groupe.Key.MatiereLibelle,
                    Couleur = groupe.Key.MatiereCouleur,
                    // LE TOTAL DE L'ANNÉE, et non de tout ce qui est affiché.
                    // Les notions des années précédentes figurent dans la liste
                    // — l'enfant doit les voir — mais elles ne rallongent pas le
                    // chemin de sa classe.
                    Total = groupe.Count(x => x.NiveauOrdre == rang),
                };

                // LES NOTIONS D AUTRES ANNÉES D ABORD, par ordre de niveau :
                // ce sont les acquis les plus anciens, et les voir en tête
                // donne à l enfant le sentiment d une base sous ses pieds
                // avant la liste de ce qui reste.
                foreach (var co in groupe
                    .OrderBy(x => x.NiveauOrdre)
                    .ThenBy(x => x.Domaine)
                    .ThenBy(x => x.Ordre))
                {
                    parCompetence.TryGetValue(co.Id, out var m);

                    var score = m?.Score ?? 0;

                    // QUATRE ÉTATS, PARCE QUE TROIS EN CONFONDAIENT DEUX.
                    //
                    // « À découvrir » et « fragile » se ressemblent — un score
                    // bas dans les deux cas — mais ne disent pas du tout la même
                    // chose à l'enfant : l'une n'a jamais été ouverte, l'autre a
                    // été travaillée sans encore tenir. Les afficher pareil
                    // revenait à dire « tu n'as pas encore travaillé ça » à un
                    // élève qui venait d'y passer quatre séances.
                    //
                    // C'est l'EXISTENCE de la ligne de maîtrise qui tranche, pas
                    // le score : une notion jamais observée n'en a aucune.
                    var etat = m is null ? "a-decouvrir"
                        : score >= SeuilAcquis ? "acquise"
                        : score >= SeuilEnCours ? "en-cours"
                        : "fragile";

                    var vue = new CompetenceVue
                    {
                        Id = co.Id,
                        Libelle = co.Libelle,
                        Domaine = co.Domaine,
                        Matiere = groupe.Key.MatiereLibelle,
                        Etat = etat,
                        Niveau = co.NiveauLibelle,

                        // AU RANG, ET NON À LA LIGNE DE NIVEAU. Pour un élève
                        // de première professionnelle, une compétence de
                        // première générale est de SON année — la signaler
                        // comme un rattrapage serait faux, et vexant.
                        AutreNiveau = co.NiveauOrdre != rang,
                        NiveauOrdre = co.NiveauOrdre,
                    };

                    matiere.Competences.Add(vue);

                    if (etat != "acquise") continue;

                    // Acquise DE SON ANNÉE d'un côté, rattrapage de l'autre :
                    // les deux se comptent, jamais dans le même total.
                    if (co.NiveauOrdre == rang) matiere.Acquises += 1;
                    else progression.AcquisesAutresAnnees += 1;

                    // NOUVELLE DEPUIS LA DERNIÈRE VISITE.
                    //
                    // Première ouverture (`vueLe` nul) : TOUT ce qui est déjà
                    // acquis compte comme nouveau. C'est la bonne première
                    // impression — pas un écran vide sur des mois de travail.
                    if (vueLe is null || m?.DerniereEvaluation > vueLe)
                    {
                        progression.Nouvelles.Add(vue);
                    }
                }

                progression.Acquises += matiere.Acquises;
                progression.Total += matiere.Total;

                progression.Matieres.Add(matiere);
            }

            // LA DATE N EST AVANCÉE QUE SI C EST L ENFANT QUI REGARDE.
            //
            // Le parent consulte la même carte ; si sa visite consommait les
            // victoires, l'enfant ne les verrait jamais — et le seul moment de
            // récompense du produit serait volé par quelqu un qui ne le
            // cherchait même pas.
            if (marquerVue && progression.Nouvelles.Count > 0)
            {
                eleve.ProgressionVueLe = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            return progression;
        }
    }
}
