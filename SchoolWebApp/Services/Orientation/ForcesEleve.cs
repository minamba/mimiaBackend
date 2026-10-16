using System.Globalization;
using System.Text;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.Orientation
{
    /// <summary>
    /// SES POINTS FORTS ET SES POINTS À RENFORCER, POUR LA CONSEILLÈRE — Camara,
    /// le 11/10/2026 : « en connaissant les forces et faiblesses de l'enfant,
    /// elle pourra aussi l'orienter, faire des propositions ».
    ///
    /// Sans ce bloc, la conseillère ne savait rien du travail de l'élève : le
    /// contexte des professeurs est filtré par matière, et la sienne,
    /// l'orientation, n'a aucune notion.
    ///
    /// AU NIVEAU DES NOTIONS, PAS SEULEMENT DES MATIÈRES (relu avec Camara le
    /// même jour) : « maths = faible » ne dit rien d'utile ; un élève peut être
    /// solide en géométrie et fragile en calcul littéral. Une même matière peut
    /// donc paraître dans les deux listes. Les chiffres par matière suivent, en
    /// appui.
    ///
    /// LES MÊMES CHIFFRES QUE LA FICHE DU PARENT (<c>GetFicheEleveAsync</c>) :
    /// cours suivis, maîtrise des seules notions vues au moins deux fois,
    /// moyenne des contrôles notés. Le parent et la conseillère lisent la même
    /// chose.
    ///
    /// DES INDICES, PAS UN VERDICT. Une matière jamais travaillée sur Mimia
    /// n'est ni forte ni faible, et le bloc le dit. La manière de s'en servir
    /// est dans <c>PromptsOrientation</c>.
    ///
    /// UNE BRIQUE RÉUTILISABLE : une fonction pure, sans base, les lectures
    /// sont faites par l'appelant. Les professeurs, les révisions ou la fiche
    /// du parent pourront la reprendre — aucun ne s'en sert encore.
    /// </summary>
    public static class ForcesEleve
    {
        /// <summary>En dessous, la maîtrise d'une matière repose sur trop peu de notions pour la classer.</summary>
        public const int NotionsPourClasser = 3;

        /// <summary>Sans maîtrise assise, au moins autant de contrôles notés pour classer une matière : un seul ne dit rien.</summary>
        public const int ControlesPourClasser = 2;

        /// <summary>Combien de notions lire de chaque côté, toutes matières confondues, avant de les grouper.</summary>
        public const int NotionsLues = 40;

        /// <summary>Au plus, par liste : des matières, et des notions dans chacune.</summary>
        public const int MatieresParListe = 4;
        public const int NotionsParMatiere = 3;

        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        public static string Construire(
            IEnumerable<StatMatiereEleve> matieres,
            IEnumerable<EvaluationEleve> evaluations,
            IEnumerable<MaitriseCompetence> notionsSolides,
            IEnumerable<MaitriseCompetence> notionsFragiles)
        {
            var notes = evaluations
                .GroupBy(e => e.MatiereId)
                .ToDictionary(g => g.Key, g => (Moyenne: g.Average(e => e.Note), Nombre: g.Count()));

            var travaillees = matieres
                .Where(m => !ConseillereOrientation.Est(m.MatiereCode))
                .Where(m => m.NombreCours > 0 || notes.ContainsKey(m.MatiereId))
                .Select(m => new
                {
                    Stat = m,
                    Notes = notes.TryGetValue(m.MatiereId, out var n) ? n : ((double Moyenne, int Nombre)?)null,
                })
                .Select(x => new
                {
                    x.Stat,
                    x.Notes,
                    // LE SCORE QUI CLASSE : la maîtrise quand elle est assise,
                    // sinon la moyenne d'au moins deux contrôles, ramenée sur 1.
                    // Ni l'un ni l'autre : la matière n'est pas classée.
                    Score = x.Stat.MaitriseMoyenne is double maitrise && x.Stat.CompetencesAssises >= NotionsPourClasser
                        ? maitrise
                        : x.Notes is { } nn && nn.Nombre >= ControlesPourClasser ? nn.Moyenne / 20 : (double?)null,
                })
                .OrderByDescending(x => x.Stat.NombreCours)
                .ToList();

            var forts = ParMatiere(notionsSolides.Where(n => !ConseillereOrientation.Est(n.MatiereCode)));
            var aRenforcer = ParMatiere(notionsFragiles.Where(n => !ConseillereOrientation.Est(n.MatiereCode)));

            var texte = new StringBuilder();
            texte.AppendLine("## Ses points forts et ses points à renforcer");
            texte.AppendLine();

            if (travaillees.Count == 0 && forts.Count == 0 && aRenforcer.Count == 0)
            {
                texte.AppendLine("""
                    L'élève n'a encore travaillé aucune matière sur Mimia : tu ne connais PAS
                    ses points forts ni ses points à renforcer. Ne suppose rien ; demande-lui
                    ce qu'il aime, où il se sent à l'aise, et ses résultats en classe.
                    """);
                return texte.ToString();
            }

            texte.AppendLine("""
                Basé UNIQUEMENT sur les séances réalisées sur Mimia : ce sont des indices,
                pas son bulletin. Une matière ou une notion absente d'ici n'est ni forte ni
                faible : tu ne sais rien d'elle.
                """);

            texte.AppendLine();
            texte.AppendLine("Points forts observés :");
            texte.AppendLine(forts.Count > 0
                ? string.Join("\n", forts.Select(f => $"- {f.Matiere} — {string.Join(" ; ", f.Notions)}"))
                : "- aucune notion encore solidement acquise.");

            texte.AppendLine();
            texte.AppendLine("Points à renforcer :");
            texte.AppendLine(aRenforcer.Count > 0
                ? string.Join("\n", aRenforcer.Select(f => $"- {f.Matiere} — {string.Join(" ; ", f.Notions)}"))
                : "- aucune notion n'apparaît fragile.");

            if (travaillees.Count > 0)
            {
                texte.AppendLine();
                texte.AppendLine("En chiffres, par matière (cours suivis ; maîtrise des notions vues au moins deux fois ; contrôles notés) :");

                foreach (var m in travaillees)
                {
                    var morceaux = new List<string> { $"{m.Stat.NombreCours} cours" };

                    morceaux.Add(m.Stat.MaitriseMoyenne is double maitrise && m.Stat.CompetencesAssises > 0
                        ? $"maîtrise {Math.Round(maitrise * 100)} % sur {m.Stat.CompetencesAssises} notion{(m.Stat.CompetencesAssises > 1 ? "s" : "")}"
                        : "maîtrise pas encore établie");

                    morceaux.Add(m.Notes is { } n
                        ? $"{n.Moyenne.ToString("0.#", Fr)}/20 en moyenne sur {n.Nombre} contrôle{(n.Nombre > 1 ? "s" : "")}"
                        : "aucun contrôle noté");

                    texte.AppendLine($"- {m.Stat.MatiereLibelle} : {string.Join(" ; ", morceaux)}.");
                }

                // LES SEUILS DE LA FICHE : « acquis » à 80 %, « fragile » sous
                // 50 %. Entre les deux, une matière est en construction, et
                // n'est citée dans aucune des deux listes.
                var classees = travaillees.Where(m => m.Score is not null).ToList();
                var solides = classees
                    .Where(m => m.Score >= SeuilsMaitrise.Acquis)
                    .OrderByDescending(m => m.Score)
                    .Take(MatieresParListe)
                    .Select(m => m.Stat.MatiereLibelle)
                    .ToList();
                var fragiles = classees
                    .Where(m => m.Score < SeuilsMaitrise.Fragile)
                    .OrderBy(m => m.Score)
                    .Take(MatieresParListe)
                    .Select(m => m.Stat.MatiereLibelle)
                    .ToList();

                if (solides.Count > 0) texte.AppendLine($"Matières les plus solides dans l'ensemble : {string.Join(", ", solides)}.");
                if (fragiles.Count > 0) texte.AppendLine($"Matières les plus fragiles dans l'ensemble : {string.Join(", ", fragiles)}.");
            }

            return texte.ToString();
        }

        /// <summary>
        /// Les notions groupées par matière, dans l'ordre où elles arrivent (le
        /// plus marquant d'abord : le dépôt trie déjà par score).
        /// </summary>
        private static List<(string Matiere, List<string> Notions)> ParMatiere(IEnumerable<MaitriseCompetence> notions) => notions
            .Where(n => !string.IsNullOrWhiteSpace(n.Libelle) && !string.IsNullOrWhiteSpace(n.MatiereLibelle))
            .GroupBy(n => n.MatiereLibelle!)
            .Take(MatieresParListe)
            .Select(g => (g.Key, g.Select(n => n.Libelle!).Distinct().Take(NotionsParMatiere).ToList()))
            .ToList();
    }
}
