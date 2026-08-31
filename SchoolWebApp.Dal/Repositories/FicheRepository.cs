using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class FicheRepository : IFicheRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public FicheRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<FicheRevisionEleve?> EnregistrerAsync(
            int eleveId,
            int conversationId,
            string notion,
            string? domaine,
            string contenu,
            string? etat = null,
            CancellationToken ct = default)
        {
            // La matière vient de la conversation, jamais du texte du modèle.
            var conversation = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Id == conversationId && c.EleveId == eleveId)
                .Select(c => new { c.Id, c.MatiereId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null) return null;

            var titre = Tronquer(notion, 300)!;

            // La correspondance se fait sur une forme NORMALISÉE du titre, pas
            // sur la chaîne exacte.
            //
            // Le titre est écrit librement par le modèle, à chaque séance. Une
            // comparaison stricte échouait dès qu'il variait d'une virgule :
            // « Division euclidienne, quotient et reste » puis « Division
            // euclidienne : quotient et reste » donnaient DEUX fiches. La règle
            // « il n'y a jamais deux fiches pour une même notion » n'était donc
            // pas tenue, et surtout la date de mise à jour de la fiche
            // d'origine ne bougeait plus : l'élève ne voyait jamais le badge
            // « Mise à jour », puisque aucune fiche n'était jamais mise à jour.
            //
            // On ramène les candidates en mémoire — deux colonnes, quelques
            // dizaines de lignes par matière — parce que cette normalisation
            // (accents, ponctuation, articles) ne se traduit pas en SQL sans
            // colonne calculée, donc sans migration.
            var candidates = await _context.FichesRevision
                .Where(f => f.EleveId == eleveId && f.MatiereId == conversation.MatiereId)
                .Select(f => new { f.Id, f.Notion })
                .ToListAsync(ct);

            var cle = CleNotion(titre);

            var existante = candidates.FirstOrDefault(c => CleNotion(c.Notion) == cle);

            var fiche = existante is null
                ? null
                : await _context.FichesRevision.FirstOrDefaultAsync(f => f.Id == existante.Id, ct);

            var maintenant = DateTime.UtcNow;

            if (fiche is null)
            {
                fiche = new FicheRevision
                {
                    EleveId = eleveId,
                    MatiereId = conversation.MatiereId,
                    Notion = titre,
                    DateCreation = maintenant,
                };

                _context.FichesRevision.Add(fiche);
            }

            fiche.ConversationId = conversation.Id;
            fiche.Domaine = Tronquer(domaine, 150);
            fiche.Contenu = contenu.Trim();
            fiche.DateMiseAJour = maintenant;

            // Une notion acquise ne redevient pas « en cours » parce que le
            // professeur a omis le champ en la révisant plus tard.
            var nouvelEtat = EtatFiche.Normaliser(etat);
            if (fiche.Etat != EtatFiche.Acquise || nouvelEtat == EtatFiche.Acquise)
            {
                fiche.Etat = nouvelEtat;
            }

            await _context.SaveChangesAsync(ct);

            return await _context.FichesRevision
                .AsNoTracking()
                .Where(f => f.Id == fiche.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        /// <summary>
        /// Longueur lue pour fabriquer l'aperçu.
        ///
        /// On coupe DANS SQL SERVER, pas en mémoire : la base ne remonte que ces
        /// caractères au lieu du texte intégral de chaque fiche. Trois cents
        /// suffisent — l'aperçu tient en cent quarante, et il faut de quoi
        /// franchir un titre et une puce avant la première vraie phrase.
        /// </summary>
        private const int LongueurApercu = 300;

        /// <summary>
        /// La liste d'une matière. Le CONTENU n'y figure pas.
        ///
        /// Il y était, pour calculer l'aperçu côté application : une matière
        /// suivie deux ans compte des dizaines de fiches, et on transportait le
        /// texte intégral de chacune pour n'en afficher que deux lignes. Le
        /// contenu ne part plus que sur l'ouverture d'une fiche précise.
        /// </summary>
        public async Task<IEnumerable<FicheRevisionEleve>> GetParMatiereAsync(
            int eleveId, int matiereId, CancellationToken ct = default)
        {
            var lignes = await _context.FichesRevision
                .AsNoTracking()
                .Where(f => f.EleveId == eleveId && f.MatiereId == matiereId)
                .OrderByDescending(f => f.DateMiseAJour)
                .Select(f => new
                {
                    f.Id,
                    f.MatiereId,
                    MatiereLibelle = f.Matiere!.Libelle,
                    f.Matiere.ProfPrenom,
                    f.Matiere.ProfCouleur,
                    f.Notion,
                    f.Domaine,
                    f.Etat,
                    f.DateCreation,
                    f.DateMiseAJour,
                    f.DateConsultation,
                    Debut = f.Contenu == null ? null : f.Contenu.Substring(0, LongueurApercu),
                })
                .ToListAsync(ct);

            return lignes.Select(l => new FicheRevisionEleve
            {
                Id = l.Id,
                MatiereId = l.MatiereId,
                MatiereLibelle = l.MatiereLibelle,
                ProfPrenom = l.ProfPrenom,
                ProfCouleur = l.ProfCouleur,
                Notion = l.Notion,
                Domaine = l.Domaine,
                Etat = l.Etat,
                DateCreation = l.DateCreation,
                DateMiseAJour = l.DateMiseAJour,
                DateConsultation = l.DateConsultation,
                Apercu = FicheRevisionEleve.ExtraireApercu(l.Debut),
            }).ToList();
        }

        public async Task<FicheRevisionEleve?> GetDetailAsync(
            int ficheId, int eleveId, CancellationToken ct = default) =>
            // Le filtre sur l'élève est la garde d'accès.
            await _context.FichesRevision
                .AsNoTracking()
                .Where(f => f.Id == ficheId && f.EleveId == eleveId)
                .Select(f => new FicheRevisionEleve
                {
                    Id = f.Id,
                    MatiereId = f.MatiereId,
                    MatiereLibelle = f.Matiere!.Libelle,
                    ProfPrenom = f.Matiere.ProfPrenom,
                    ProfCouleur = f.Matiere.ProfCouleur,
                    Notion = f.Notion,
                    Domaine = f.Domaine,
                    Contenu = f.Contenu,
                    Etat = f.Etat,
                    DateCreation = f.DateCreation,
                    DateMiseAJour = f.DateMiseAJour,
                    DateConsultation = f.DateConsultation,
                    ElevePrenom = f.Eleve!.Prenom,
                    EleveNom = f.Eleve.Nom,
                    EleveNiveau = f.Eleve.NiveauScolaire!.Libelle,
                })
                .FirstOrDefaultAsync(ct);

        public async Task<bool> MarquerVueAsync(
            int ficheId, int eleveId, CancellationToken ct = default)
        {
            // Le filtre sur l'élève est la garde d'accès, comme à la lecture.
            var fiche = await _context.FichesRevision
                .FirstOrDefaultAsync(f => f.Id == ficheId && f.EleveId == eleveId, ct);

            if (fiche is null) return false;

            fiche.DateConsultation = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<IReadOnlyDictionary<int, CompteurFiches>> CompterParMatiereAsync(
            int eleveId, CancellationToken ct = default)
        {
            // Le décompte des non-lues est fait en SQL et non en mémoire : la
            // carte de matière n'a besoin que de deux entiers par matière.
            var lignes = await _context.FichesRevision
                .AsNoTracking()
                .Where(f => f.EleveId == eleveId)
                .GroupBy(f => f.MatiereId)
                .Select(g => new
                {
                    MatiereId = g.Key,
                    Total = g.Count(),
                    Nouveautes = g.Count(f =>
                        f.DateConsultation == null || f.DateConsultation < f.DateMiseAJour),
                })
                .ToListAsync(ct);

            return lignes.ToDictionary(
                x => x.MatiereId, x => new CompteurFiches(x.Total, x.Nouveautes));
        }

        /// <summary>
        /// Projection d'UNE fiche, contenu compris — celle qu'on vient d'écrire
        /// et qu'on rend à l'appelant. Les listes, elles, passent par une
        /// projection allégée qui laisse le contenu en base.
        /// </summary>
        private static readonly Expression<Func<FicheRevision, FicheRevisionEleve>> Projection =
            f => new FicheRevisionEleve
            {
                Id = f.Id,
                MatiereId = f.MatiereId,
                MatiereLibelle = f.Matiere!.Libelle,
                ProfPrenom = f.Matiere.ProfPrenom,
                ProfCouleur = f.Matiere.ProfCouleur,
                Notion = f.Notion,
                Domaine = f.Domaine,
                Contenu = f.Contenu,
                Etat = f.Etat,
                DateCreation = f.DateCreation,
                DateMiseAJour = f.DateMiseAJour,
                DateConsultation = f.DateConsultation,
            };

        public async Task<IReadOnlyList<string>> GetTitresAsync(
            int eleveId, int matiereId, CancellationToken ct = default) =>
            await _context.FichesRevision
                .AsNoTracking()
                .Where(f => f.EleveId == eleveId && f.MatiereId == matiereId)
                .OrderByDescending(f => f.DateMiseAJour)
                .Select(f => f.Notion!)
                .ToListAsync(ct);

        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;

            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }

        /// <summary>
        /// Articles et petits mots de liaison, retirés de la clé.
        ///
        /// « La division euclidienne » et « Division euclidienne » désignent la
        /// même notion. Les garder ferait deux fiches.
        /// </summary>
        private static readonly HashSet<string> MotsVides = new(StringComparer.Ordinal)
        {
            "le", "la", "les", "l", "un", "une", "des", "du", "de", "d",
            "et", "ou", "a", "au", "aux", "en", "dans", "sur", "par", "pour",
        };

        /// <summary>
        /// La clé de rapprochement de deux titres de notion.
        ///
        /// Accents retirés, casse abaissée, ponctuation ramenée à l'espace,
        /// articles écartés, et les mots restants TRIÉS. Ce dernier point est
        /// ce qui rattrape le plus de cas : « Quotient et reste d'une division
        /// euclidienne » et « Division euclidienne : quotient et reste »
        /// portent les mêmes mots dans un autre ordre, et sont bien la même
        /// notion.
        ///
        /// Le rapprochement reste volontairement conservateur : on n'unifie que
        /// des titres composés des MÊMES mots. Deux notions voisines mais
        /// distinctes — « division décimale » et « division euclidienne » —
        /// gardent chacune leur fiche, ce qui est le comportement voulu. Se
        /// tromper dans ce sens ferait perdre une fiche à l'élève ; se tromper
        /// dans l'autre lui en laisse deux, ce qui se voit et se corrige.
        /// </summary>
        private static string CleNotion(string? titre)
        {
            if (string.IsNullOrWhiteSpace(titre)) return string.Empty;

            var sansAccents = new StringBuilder();

            foreach (var c in titre.Normalize(NormalizationForm.FormD))
            {
                var categorie = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categorie == UnicodeCategory.NonSpacingMark) continue;

                sansAccents.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }

            var mots = sansAccents
                .ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(m => !MotsVides.Contains(m))
                .OrderBy(m => m, StringComparer.Ordinal);

            return string.Join(' ', mots);
        }
    }
}
