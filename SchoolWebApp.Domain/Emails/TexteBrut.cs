using System.Net;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// LA VERSION TEXTE D'UN COURRIEL — Camara, le 18/10/2026 : la notification
    /// de son téléphone affichait « [Contact] Informations @media only screen and
    /// (max-width: 620px) { table[width="600"]… ». Les messageries montrent la
    /// partie texte dans leurs aperçus ; on y laissait le titre de la page, le
    /// bloc de style et le texte d'aperçu caché. Une seule conversion pour tous
    /// les envois, qui ne garde que ce qu'un lecteur lirait.
    /// </summary>
    public static class TexteBrut
    {
        private static readonly Regex ATaire = new(
            @"<!--.*?-->|<head\b[^>]*>.*?</head>|<(style|script|title)\b[^>]*>.*?</\1>"
            + @"|<div\b[^>]*display\s*:\s*none[^>]*>.*?</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex FinsDeLigne = new(
            @"<br\s*/?>|</p>|</tr>|</h[1-6]>|</div>|</li>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Balises = new("<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// L'ADRESSE D'UN BOUTON RESTE DANS LE TEXTE — contrôle d'Omar sur le lot groupé (22/11/2026) : le bouton du message
        /// de diffusion devenait « Voir les formules », sans adresse, dans la version texte. Un lien qui porte cet attribut
        /// s'écrit « texte : adresse ». Les autres liens ne changent pas (rien ne bouge pour les courriels existants).
        /// </summary>
        public const string AttributAdresseEnTexte = "data-adresse-en-texte=\"1\"";

        private static readonly Regex LienAdresseEnTexte = new(
            @"<a\b(?=[^>]*\bdata-adresse-en-texte=""1"")[^>]*\bhref=""(?<adresse>https?://[^""]+)""[^>]*>(?<texte>.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>Le texte lisible d'un HTML : sans en-tête, style, script ni texte caché.</summary>
        public static string Depuis(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var texte = ATaire.Replace(html, " ");
            texte = LienAdresseEnTexte.Replace(texte, m => $"{m.Groups["texte"].Value} : {m.Groups["adresse"].Value}");

            // Les retours à la ligne du SOURCE ne sont pas ceux du texte : seules les
            // balises de fin de paragraphe en font.
            texte = Regex.Replace(texte, @"\s+", " ");
            texte = FinsDeLigne.Replace(texte, "\n");
            texte = Balises.Replace(texte, string.Empty);
            texte = WebUtility.HtmlDecode(texte);
            texte = Regex.Replace(texte, @"[ \t ]+", " ");
            texte = Regex.Replace(texte, @" *\n *", "\n");
            texte = Regex.Replace(texte, @"\n{3,}", "\n\n");
            return texte.Trim();
        }

        /// <summary>
        /// Le texte d'aperçu (« preheader ») : les premiers mots du message, sur
        /// une ligne, déjà échappés pour le HTML.
        /// </summary>
        public static string Apercu(string? html, int taille = 140)
        {
            var texte = Regex.Replace(Depuis(html), @"\s+", " ").Trim();
            if (texte.Length > taille) texte = texte[..taille].TrimEnd() + "…";
            return WebUtility.HtmlEncode(texte);
        }
    }
}
