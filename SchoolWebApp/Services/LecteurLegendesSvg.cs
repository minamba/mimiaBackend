using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Lit les légendes d'une planche SVG, telles qu'elles s'AFFICHENT.
    ///
    /// POURQUOI UN LECTEUR PLUTÔT QU'UN MODÈLE DE VISION
    /// ------------------------------------------------
    /// L'API n'accepte pas le SVG comme image. Mais elle n'en a pas besoin :
    /// les légendes d'un SVG sont du texte, en clair, dans le fichier. Les lire
    /// est plus fiable que de les faire déchiffrer, et gratuit.
    ///
    /// POURQUOI CE N'EST PAS UNE EXPRESSION RÉGULIÈRE
    /// ---------------------------------------------
    /// C'était la première version, et elle était fausse sur la toute première
    /// planche importée. Les figures de Wikimedia sont multilingues : chaque
    /// étiquette est un `switch` contenant le même mot en dix langues, dont une
    /// seule s'affiche — celle qui correspond à la langue du lecteur, ou la
    /// variante sans `systemLanguage`, qui est le repli.
    ///
    /// La lecture naïve rendait 36 étiquettes dont 25 en kurde et en persan, et
    /// le professeur se serait mis à interroger sur « گەروو ». Le `switch` doit
    /// être RÉSOLU, pas parcouru.
    ///
    /// Second piège du même fichier : un `text` porte plusieurs `tspan` —
    /// « Artère » et « pulmonaire ». Les concaténer sans séparateur donnait
    /// « Artèrepulmonaire ».
    /// </summary>
    public static class LecteurLegendesSvg
    {
        /// <summary>Au-delà, ce n'est plus un inventaire mais un dictionnaire.</summary>
        private const int MaxLegendes = 50;

        /// <summary>
        /// Les légendes visibles, séparées par des virgules. Chaîne vide si le
        /// fichier est illisible ou ne porte aucun texte.
        /// </summary>
        public static string Lire(byte[] donnees)
        {
            if (donnees is not { Length: > 0 }) return string.Empty;

            XDocument doc;
            try
            {
                doc = XDocument.Parse(Encoding.UTF8.GetString(donnees));
            }
            catch (XmlException)
            {
                return string.Empty;
            }

            if (doc.Root is null) return string.Empty;

            var legendes = new List<string>();
            Parcourir(doc.Root, legendes);

            var propres = legendes
                .Where(l => !EstUneNoteDEdition(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxLegendes)
                .ToList();

            return propres.Count == 0 ? string.Empty : string.Join(", ", propres);
        }

        /// <summary>
        /// Les textes d'un SVG ne sont pas tous des légendes.
        ///
        /// Les contributeurs de Commons y laissent des notes à destination des
        /// autres contributeurs, dans des calques masqués. Sur la double
        /// circulation sanguine, le relevé commençait par « Pour les
        /// modifications de texte, lisez User:Jmarchn/Edition of Inkscape draw
        /// with layers » — que le professeur aurait annoncé à l'élève comme un
        /// élément de la figure.
        ///
        /// On écarte aussi les crédits d'institution collés dans l'image :
        /// « National Cancer Institute » n'est pas un organe.
        /// </summary>
        private static bool EstUneNoteDEdition(string texte)
        {
            var t = texte.ToLowerInvariant();

            return t.Contains("user:")
                || t.Contains("http")
                || t.Contains("inkscape")
                || t.Contains("wikimedia")
                || t.Contains("wikipedia")
                || t.Contains("creative commons")
                || t.Contains("licence")
                || t.Contains("license")
                || t.Contains("institute")
                || t.Contains("copyright")
                // Une « légende » d'une phrase entière est une consigne, pas un
                // mot désignant une partie de la figure.
                || texte.Split(' ').Length > 8;
        }

        private static void Parcourir(XElement element, List<string> legendes)
        {
            switch (element.Name.LocalName)
            {
                case "text":
                    var t = TexteAffiche(element);
                    if (t.Length > 1) legendes.Add(t);
                    return;

                // Une seule branche du switch s'affiche. La règle de SVG est
                // « la première dont les conditions sont remplies » ; on prend
                // donc la variante française si elle est déclarée, sinon celle
                // sans condition, qui est le repli du fichier.
                case "switch":
                    var enfants = element.Elements().ToList();

                    var choisie =
                        enfants.FirstOrDefault(e =>
                            e.Attribute("systemLanguage")?.Value
                                .StartsWith("fr", StringComparison.OrdinalIgnoreCase) == true)
                        ?? enfants.FirstOrDefault(e => e.Attribute("systemLanguage") is null);

                    if (choisie is not null) Parcourir(choisie, legendes);
                    return;

                default:
                    foreach (var enfant in element.Elements()) Parcourir(enfant, legendes);
                    return;
            }
        }

        private static string TexteAffiche(XElement texte)
        {
            var tspans = texte.Descendants()
                .Where(e => e.Name.LocalName == "tspan")
                .Select(e => e.Value)
                .ToList();

            var brut = tspans.Count > 0 ? string.Join(" ", tspans) : texte.Value;

            return Regex.Replace(brut, @"\s+", " ").Trim();
        }
    }
}
