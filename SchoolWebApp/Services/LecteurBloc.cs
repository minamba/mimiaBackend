using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Analyse des blocs `clé: valeur` que le professeur écrit dans ses
    /// messages — évaluation, rapport de séance.
    ///
    /// Mis en commun plutôt que recopié : la tolérance de lecture est la partie
    /// délicate — clés accentuées, deux-points dans une valeur, ligne de
    /// continuation, « 11/20 » au lieu de « 11 ». Deux copies de ces règles
    /// finiraient par diverger, et un des deux blocs cesserait d'être lu sans
    /// que rien ne le signale.
    /// </summary>
    public static partial class LecteurBloc
    {
        /// <summary>
        /// Capture le premier nombre d'un champ de note. Couvre « 11 »,
        /// « 11/20 », « 11,5 » et « 11.5 » — les quatre formes observées.
        /// </summary>
        [GeneratedRegex(@"(\d{1,2})(?:[.,](\d+))?")]
        private static partial Regex Nombre();

        /// <summary>
        /// Découpe un corps de bloc en couples clé/valeur.
        ///
        /// Une ligne sans deux-points est rattachée à la clé précédente : le
        /// modèle passe volontiers à la ligne au milieu d'une remarque longue.
        /// </summary>
        public static Dictionary<string, string> Champs(string corps)
        {
            var champs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? courante = null;

            foreach (var ligne in corps.Split('\n'))
            {
                var propre = ligne.Trim();
                if (propre.Length == 0) continue;

                var separateur = propre.IndexOf(':');

                // Le deux-points doit venir tôt : « Bravo : c'est acquis » est
                // la suite d'une remarque, pas une clé nommée « Bravo ».
                if (separateur > 0 && separateur <= 20)
                {
                    courante = Normaliser(propre[..separateur]);
                    champs[courante] = propre[(separateur + 1)..].Trim();
                    continue;
                }

                if (courante is not null)
                {
                    champs[courante] = (champs[courante] + " " + propre).Trim();
                }
            }

            return champs;
        }

        /// <summary>« À revoir », « a-revoir » et « a_revoir » sont la même clé.</summary>
        public static string Normaliser(string cle)
        {
            var sansAccent = string.Concat(cle.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

            return sansAccent.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        }

        /// <summary>Valeur d'un champ, ou null s'il est absent ou vide.</summary>
        public static string? Valeur(Dictionary<string, string> champs, params string[] cles)
        {
            foreach (var cle in cles)
            {
                if (champs.TryGetValue(cle, out var valeur) && !string.IsNullOrWhiteSpace(valeur))
                {
                    return valeur.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Note sur 20 lue dans un champ, bornée. Null quand le champ est
        /// absent, vide, ou ne contient aucun chiffre — ce dernier cas couvre
        /// le « non évalué » qu'écrit le professeur quand il n'a rien à noter.
        /// </summary>
        public static double? Note(Dictionary<string, string> champs, params string[] cles)
        {
            var brut = Valeur(champs, cles);
            if (brut is null) return null;

            var nombre = Nombre().Match(brut);
            if (!nombre.Success) return null;

            var entier = double.Parse(nombre.Groups[1].Value, CultureInfo.InvariantCulture);
            var decimales = nombre.Groups[2].Success
                ? double.Parse("0." + nombre.Groups[2].Value, CultureInfo.InvariantCulture)
                : 0;

            return Math.Clamp(entier + decimales, 0, 20);
        }

        /// <summary>Retire un bloc et ses balises d'un message.</summary>
        public static string Retirer(string? message, Regex bloc, params string[] marqueurs)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;

            var texte = bloc.Replace(message, string.Empty);

            foreach (var marqueur in marqueurs)
            {
                texte = texte.Replace(marqueur, string.Empty);
            }

            return texte.Trim();
        }
    }
}
