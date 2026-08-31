using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'image bitmap qu'un SVG peut contenir, extraite de son enveloppe.
    ///
    /// POURQUOI CE CAS EXISTE, ET POURQUOI IL NOUS A COÛTÉ UNE SÉANCE
    /// -------------------------------------------------------------
    /// Un SVG est réputé porter son texte en clair : c'est toute la raison pour
    /// laquelle <c>LecteurLegendesSvg</c> lit ses balises <c>text</c> plutôt que
    /// de payer un modèle de vision. L'hypothèse est juste pour un SVG DESSINÉ.
    ///
    /// Elle est fausse pour un SVG qui n'est qu'une ENVELOPPE autour d'une
    /// image matricielle — ce que produisent la plupart des exports
    /// d'illustration, et tous les générateurs d'images. Le fichier pèse deux
    /// mégaoctets, ses légendes sont dans les pixels, et il ne contient pas une
    /// seule balise <c>text</c>.
    ///
    /// Le lecteur concluait alors « figure sans aucune étiquette, à faire
    /// légender de mémoire », et le professeur le répétait à l'élève — qui
    /// avait sous les yeux une planche entièrement légendée et s'entendait
    /// dire le contraire.
    ///
    /// Une enveloppe n'est pas un dessin. On en sort l'image, et on la fait
    /// décrire comme n'importe quelle autre.
    /// </summary>
    public static class ImageEmbarqueeSvg
    {
        /// <summary>
        /// Ce qu'on accepte de sortir d'une enveloppe : exactement les formats
        /// que le modèle de vision sait lire.
        /// </summary>
        private static readonly Dictionary<string, string> TypesLisibles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["png"] = "image/png",
            ["jpeg"] = "image/jpeg",
            ["jpg"] = "image/jpeg",
            ["gif"] = "image/gif",
            ["webp"] = "image/webp",
        };

        /// <summary>
        /// Cherche la PREMIÈRE image encodée en base64 du document.
        ///
        /// La première et non la plus grande : une enveloppe d'export n'en
        /// contient qu'une, et parcourir toutes les correspondances d'un
        /// fichier de deux mégaoctets pour comparer leurs tailles coûterait
        /// plus que ça ne rapporte.
        /// </summary>
        private static readonly Regex Motif = new(
            @"data:image/(?<type>[a-z]+);base64,(?<donnees>[A-Za-z0-9+/=\s]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromSeconds(2));

        /// <summary>
        /// Les octets de l'image embarquée et son type, ou null si ce SVG est
        /// un vrai dessin — auquel cas il n'y a rien à extraire et le lecteur
        /// de légendes reste le bon outil.
        /// </summary>
        public static (byte[] Donnees, string TypeMime)? Extraire(byte[]? svg)
        {
            if (svg is not { Length: > 0 }) return null;

            string texte;
            try { texte = Encoding.UTF8.GetString(svg); }
            catch { return null; }

            Match trouve;
            try { trouve = Motif.Match(texte); }
            catch (RegexMatchTimeoutException) { return null; }

            if (!trouve.Success) return null;

            var type = trouve.Groups["type"].Value;
            if (!TypesLisibles.TryGetValue(type, out var mime)) return null;

            // Les sauts de ligne sont légaux dans un attribut XML et fréquents
            // dans les fichiers formatés : `FromBase64String` les refuse.
            var brut = trouve.Groups["donnees"].Value;
            var propre = new StringBuilder(brut.Length);

            foreach (var c in brut)
            {
                if (!char.IsWhiteSpace(c)) propre.Append(c);
            }

            try
            {
                var donnees = Convert.FromBase64String(propre.ToString());
                return donnees.Length > 0 ? (donnees, mime) : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
