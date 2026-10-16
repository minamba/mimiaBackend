using System.Text;
using System.Text.RegularExpressions;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Extrait le bloc [RAPPORT_ORIENTATION] d'un message de la conseillère
    /// (11/10/2026) :
    ///
    ///   [RAPPORT_ORIENTATION]
    ///   discussion: Tu m'as parlé de ton goût pour …
    ///   pistes: BUT informatique ; école d'ingénieurs après une prépa
    ///   chemin:
    ///   🎯 Ingénieur en informatique
    ///   3e (toi aujourd'hui)
    ///    ↓
    ///   Bac : spécialités maths + NSI …
    ///   atouts: la logique, les sciences
    ///   a_consolider: le calcul littéral
    ///   etapes: en parler à tes parents ; les portes ouvertes du lycée …
    ///   [/RAPPORT_ORIENTATION]
    ///
    /// SEULES LES SIX CLÉS CONNUES OUVRENT UNE RUBRIQUE. Toute autre ligne
    /// continue la précédente, telle quelle : le chemin compte des lignes
    /// comme « Bac : Parcoursup… », qu'un découpage au premier deux-points
    /// prendrait pour une clé, et des espaces qui alignent ses flèches.
    /// </summary>
    public static partial class LecteurRapportOrientation
    {
        private const int MaxRubrique = 2000;
        private const int MaxCourte = 1000;

        [GeneratedRegex(@"\[RAPPORT_ORIENTATION\](?<corps>.*?)\[/RAPPORT_ORIENTATION\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex Bloc();

        [GeneratedRegex(@"^\s*(?<cle>[\p{L}_ ]{3,20}?)\s*:\s?(?<valeur>.*)$")]
        private static partial Regex Cle();

        private static readonly Dictionary<string, string> Rubriques = new(StringComparer.Ordinal)
        {
            ["discussion"] = "discussion",
            ["pistes"] = "pistes",
            ["chemin"] = "chemin",
            ["atouts"] = "atouts",
            ["a_consolider"] = "a_consolider",
            ["etapes"] = "etapes",
            ["prochaines_etapes"] = "etapes",
        };

        /// <summary>Null s'il n'y a pas de bloc, ou s'il est vide.</summary>
        public static RapportOrientationEleve? Lire(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var bloc = Bloc().Match(message);
            if (!bloc.Success) return null;

            var champs = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
            string? courante = null;

            foreach (var brute in bloc.Groups["corps"].Value.Replace("\r", "").Split('\n'))
            {
                var ligne = brute.TrimEnd();
                var cle = Cle().Match(ligne);

                if (cle.Success && Rubriques.TryGetValue(LecteurBloc.Normaliser(cle.Groups["cle"].Value), out var rubrique))
                {
                    courante = rubrique;
                    if (!champs.ContainsKey(courante)) champs[courante] = new StringBuilder();
                    var valeur = cle.Groups["valeur"].Value.Trim();
                    if (valeur.Length > 0) Ajouter(champs[courante], valeur);
                }
                else if (courante is not null && (ligne.Trim().Length > 0 || courante == "chemin"))
                {
                    // Le chemin garde ses espaces de tête : ils alignent ses flèches.
                    Ajouter(champs[courante], courante == "chemin" ? ligne : ligne.Trim());
                }
            }

            string? Valeur(string cle, int max)
            {
                if (!champs.TryGetValue(cle, out var texte)) return null;
                var propre = texte.ToString().Trim('\n', ' ');
                if (propre.Length == 0) return null;
                return propre.Length <= max ? propre : propre[..max];
            }

            var rapport = new RapportOrientationEleve
            {
                Discussion = Valeur("discussion", MaxRubrique),
                Pistes = Valeur("pistes", MaxRubrique),
                Chemin = Valeur("chemin", MaxRubrique),
                Atouts = Valeur("atouts", MaxCourte),
                AConsolider = Valeur("a_consolider", MaxCourte),
                Etapes = Valeur("etapes", MaxRubrique),
            };

            return rapport.Discussion is null && rapport.Pistes is null && rapport.Chemin is null
                && rapport.Atouts is null && rapport.AConsolider is null && rapport.Etapes is null
                ? null
                : rapport;
        }

        private static void Ajouter(StringBuilder texte, string ligne)
        {
            if (texte.Length > 0) texte.Append('\n');
            texte.Append(ligne);
        }

        /// <summary>Retire le bloc. Sert aux relectures d'historique.</summary>
        public static string Retirer(string? message) => LecteurBloc.Retirer(message, Bloc());
    }
}
