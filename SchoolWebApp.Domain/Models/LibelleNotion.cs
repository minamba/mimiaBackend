using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Le libellé d'une notion ramené à ce qui compte pour le reconnaître.
    ///
    /// RELEVÉ EN PROD LE 13/10/2026 : le professeur avait déclaré « de repérer
    /// une personnification ». Le référentiel ne se rapprochait qu'au mot
    /// près : rien ne correspondait, la notion restait en texte libre, et sa
    /// barre à 25 % pour toujours — quoi que fasse l'élève. Accents, casse,
    /// ponctuation et le « de » d'une tournure à l'infinitif ne disent rien
    /// de la notion ; on les retire avant de comparer.
    /// </summary>
    public static partial class LibelleNotion
    {
        [GeneratedRegex(@"[^a-z0-9 ]+")]
        private static partial Regex PasUneLettre();

        [GeneratedRegex(@"^\s*(savoir|etre capable de|de|d)\s+")]
        private static partial Regex Amorce();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Blancs();

        public static string CleSouple(string? libelle)
        {
            if (string.IsNullOrWhiteSpace(libelle)) return string.Empty;

            var sansAccent = string.Concat(libelle.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

            var cle = PasUneLettre().Replace(sansAccent.ToLowerInvariant(), " ");
            cle = Amorce().Replace(cle, "");
            return Blancs().Replace(cle, " ").Trim();
        }
    }
}
