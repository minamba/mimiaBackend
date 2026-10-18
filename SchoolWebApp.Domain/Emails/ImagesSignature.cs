using MimeKit;

namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// LES IMAGES DE LA SIGNATURE, EN PIÈCES LIÉES — Camara, le 17/10/2026 : « les
    /// images ne s'affichent pas ». Servies par le site, elles dépendaient d'un
    /// front déployé, et Outlook bloque de toute façon les images distantes.
    /// Jointes au message comme la bannière (`cid:`), elles s'affichent partout.
    ///
    /// Les fichiers sont les mêmes que ceux de `public/mail/` côté site (aperçus
    /// de l'administration) : les deux copies se refont ensemble.
    /// </summary>
    public static class ImagesSignature
    {
        private static readonly string[] Noms =
            ["signature-logo", "signature-trait", "signature-mail", "signature-tel", "signature-web"];

        private static readonly System.Reflection.Assembly Assemblee = typeof(ImagesSignature).Assembly;

        /// <summary>
        /// L'adresse d'une image : `cid:` pour un courriel (`urlSite` nul), sinon
        /// le site (vide = le site où s'affiche l'aperçu).
        /// </summary>
        public static string Source(string nom, string? urlSite) =>
            urlSite is null ? $"cid:{nom}" : $"{urlSite.TrimEnd('/')}/mail/{nom}.png";

        /// <summary>Joint au message chaque image de la signature que le corps cite.</summary>
        public static void Attacher(BodyBuilder constructeur)
        {
            var corps = constructeur.HtmlBody ?? string.Empty;

            foreach (var nom in Noms)
            {
                if (!corps.Contains($"cid:{nom}", StringComparison.Ordinal)) continue;

                using var flux = Assemblee.GetManifestResourceStream(
                    $"SchoolWebApp.Domain.Emails.Ressources.{nom}.png");
                if (flux is null) continue;

                var image = constructeur.LinkedResources.Add($"{nom}.png", flux, new ContentType("image", "png"));
                image.ContentId = nom;
            }
        }

        /// <summary>
        /// Pour un aperçu dans le navigateur, qui ne sait pas résoudre `cid:` :
        /// les images incorporées en base64.
        /// </summary>
        public static string EnDataUri(string corps)
        {
            foreach (var nom in Noms)
            {
                if (!corps.Contains($"cid:{nom}", StringComparison.Ordinal)) continue;

                using var flux = Assemblee.GetManifestResourceStream(
                    $"SchoolWebApp.Domain.Emails.Ressources.{nom}.png");
                if (flux is null) continue;

                using var memoire = new MemoryStream();
                flux.CopyTo(memoire);
                corps = corps.Replace(
                    $"cid:{nom}", $"data:image/png;base64,{Convert.ToBase64String(memoire.ToArray())}");
            }

            return corps;
        }
    }
}
