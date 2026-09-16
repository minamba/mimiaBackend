using System.Text.RegularExpressions;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// Les marqueurs `[image:N]` d'un message.
    ///
    /// LE MÊME CONTRAT DES DEUX CÔTÉS. L'écran applique la même règle
    /// (`src/lib/storage/marqueursImages.js`) quand on retire une image d'un
    /// message non enregistré ; le dépôt l'applique quand on la retire d'un
    /// template. Deux règles différentes finiraient par placer la mauvaise
    /// image au mauvais endroit, sans que rien ne le signale.
    /// </summary>
    public static class MarqueursImages
    {
        private static readonly Regex Marqueur = new(@"\[image:(\d+)\]", RegexOptions.Compiled);

        /// <summary>
        /// Le texte après le retrait de l'image de rang <paramref name="rang"/> :
        /// son marqueur disparaît, ceux des images suivantes reculent d'un rang.
        ///
        /// SANS CELA, retirer la première de trois images laissait `[image:3]`
        /// dans le texte alors qu'il n'en restait que deux : la dernière image
        /// ne s'affichait plus, et la deuxième prenait la place de la première.
        /// </summary>
        public static string RetirerImage(string? texte, int rang)
        {
            if (string.IsNullOrEmpty(texte)) return texte ?? string.Empty;

            return Marqueur.Replace(texte, m =>
            {
                var n = int.Parse(m.Groups[1].Value);

                if (n == rang) return string.Empty;
                return n > rang ? $"[image:{n - 1}]" : m.Value;
            });
        }
    }
}
