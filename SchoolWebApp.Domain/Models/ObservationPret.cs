using System.Globalization;
using System.Text;

namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// L'ÉTAT DE CHAQUE NOTION, LU DANS L'OBSERVATION DU PROFESSEUR — quand la
    /// ligne `notions:` du bloc manque.
    ///
    /// Relevé en prod le 13/10/2026 (Syan) : « Comprendre que la matière de
    /// l'Univers est partout de même nature — tenu : Syan sait maintenant
    /// l'expliquer », et la barre à 25 %. Le verdict datait d'avant la ligne
    /// `notions:` ; et un professeur peut l'oublier. Mais son observation suit
    /// un format IMPOSÉ par sa consigne : un paragraphe par notion, ouvert par
    /// le nom de la notion, un tiret long, puis où il en est. C'est lisible.
    ///
    /// PRUDENT PAR CONSTRUCTION : seuls les mots qui ouvrent le paragraphe
    /// après le tiret comptent, et une tournure inconnue ne pose RIEN — la
    /// barre garde alors sa mesure. « Pas encore travaillée » n'est pas
    /// « fragile » : c'est une notion qu'on n'a pas vue.
    /// </summary>
    public static class ObservationPret
    {
        private static readonly string[] Tirets = [" — ", " – ", " -- "];

        // L'ORDRE COMPTE : « pas encore travaillée » avant « pas encore »,
        // « pas encore » avant tout le reste.
        private static readonly (string Debut, string? Etat)[] Tournures =
        [
            ("pas encore travaill", null), ("pas encore vu", null), ("pas encore abord", null),
            ("pas travaill", null), ("pas vu", null), ("pas abord", null), ("non travaill", null),
            ("jamais", null), ("a voir", null), ("a travailler", null), ("hors programme", null),

            ("fragile", PretControle.NotionFragile), ("a revoir", PretControle.NotionFragile),
            ("a reprendre", PretControle.NotionFragile), ("pas encore", PretControle.NotionFragile),
            ("pas acquis", PretControle.NotionFragile), ("pas sur", PretControle.NotionFragile),
            ("pas tenu", PretControle.NotionFragile), ("en difficulte", PretControle.NotionFragile),
            ("difficile", PretControle.NotionFragile), ("non acquis", PretControle.NotionFragile),

            ("en cours", PretControle.NotionEnCours), ("presque", PretControle.NotionEnCours),
            ("a consolider", PretControle.NotionEnCours), ("a confirmer", PretControle.NotionEnCours),
            ("bientot", PretControle.NotionEnCours), ("en progres", PretControle.NotionEnCours),
            ("ca vient", PretControle.NotionEnCours), ("en bonne voie", PretControle.NotionEnCours),

            ("tenu", PretControle.NotionTenue), ("c est tenu", PretControle.NotionTenue),
            ("bien tenu", PretControle.NotionTenue), ("acquis", PretControle.NotionTenue),
            ("bien acquis", PretControle.NotionTenue), ("solide", PretControle.NotionTenue),
            ("maitrise", PretControle.NotionTenue), ("maitrisee", PretControle.NotionTenue),
        ];

        /// <summary>Les notions nommées en tête de paragraphe, et l'état que leurs premiers mots disent.</summary>
        public static IReadOnlyList<EtatNotionDeclare> Etats(string? observation)
        {
            if (string.IsNullOrWhiteSpace(observation)) return [];

            var lus = new List<EtatNotionDeclare>();

            foreach (var paragraphe in observation.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var (nom, suite) = Couper(paragraphe);
                if (nom is null || suite is null || nom.Length > 200) continue;

                var etat = EtatDe(suite);
                if (etat is null) continue;

                lus.Add(new EtatNotionDeclare(nom, etat));
            }

            return lus;
        }

        private static (string? Nom, string? Suite) Couper(string paragraphe)
        {
            foreach (var tiret in Tirets)
            {
                var position = paragraphe.IndexOf(tiret, StringComparison.Ordinal);
                if (position > 0)
                {
                    return (paragraphe[..position].Trim().Trim('*'), paragraphe[(position + tiret.Length)..]);
                }
            }

            return (null, null);
        }

        private static string? EtatDe(string suite)
        {
            var debut = Simplifier(suite);

            // Un début de mot suffit : « travaill » couvre travaillé, travaillée,
            // travaillées ; « tenu » couvre tenue.
            foreach (var (tournure, etat) in Tournures)
            {
                if (debut.StartsWith(tournure, StringComparison.Ordinal))
                {
                    return etat;
                }
            }

            return null;
        }

        /// <summary>Minuscules, sans accent, la ponctuation en espaces : « Tenu : » → « tenu ».</summary>
        private static string Simplifier(string texte)
        {
            var sansAccent = string.Concat(texte.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

            var propre = new StringBuilder(sansAccent.Length);
            foreach (var c in sansAccent.ToLowerInvariant())
            {
                propre.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            return string.Join(' ', propre.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
