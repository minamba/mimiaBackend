using System.Security.Cryptography;
using System.Text.Json;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Ce que Commons sait d'un fichier : qui, où, sous quelle licence.</summary>
    public record OriginePlanche(string? Auteur, string? Source, string? Licence);

    /// <summary>
    /// Retrouve l'origine d'une planche à partir de SON EMPREINTE.
    ///
    /// POURQUOI L'EMPREINTE ET NON LE NOM DU FICHIER
    /// --------------------------------------------
    /// L'administrateur télécharge une planche depuis Commons et la dépose ici.
    /// Le nom qu'il donne au fichier est perdu — l'import le reconstruit à
    /// partir de la clé — et de toute façon il l'aurait peut-être renommé. Rien
    /// dans ce qu'on stocke ne dit d'où l'image vient.
    ///
    /// Rien, SAUF les octets eux-mêmes. Commons indexe ses fichiers par SHA1 et
    /// les rend interrogeables ainsi : une empreinte identique désigne le même
    /// fichier, au bit près. C'est une identification certaine, pas une
    /// ressemblance.
    ///
    /// Éprouvé sur la coupe respiratoire déposée à la main : son bloc de
    /// métadonnées interne était VIDE — aucun auteur, aucun titre — et
    /// l'empreinte a rendu « Berrucomons, CC BY-SA 3.0 » sans ambiguïté.
    ///
    /// CE QUE ÇA NE TROUVE PAS
    /// ----------------------
    /// Un fichier ré-enregistré, recadré ou recompressé a une autre empreinte :
    /// Commons ne le reconnaîtra pas. C'est le prix de la certitude — mieux
    /// vaut ne rien dire que d'attribuer une œuvre au mauvais auteur.
    /// </summary>
    public interface IOrigineCommonsService
    {
        Task<OriginePlanche?> ChercherAsync(byte[] donnees, CancellationToken ct = default);
    }

    public class OrigineCommonsService : IOrigineCommonsService
    {
        private readonly HttpClient _http;
        private readonly ILogger<OrigineCommonsService> _logger;

        public OrigineCommonsService(HttpClient http, ILogger<OrigineCommonsService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OriginePlanche?> ChercherAsync(
            byte[] donnees, CancellationToken ct = default)
        {
            if (donnees is not { Length: > 0 }) return null;

            var empreinte = Convert.ToHexString(SHA1.HashData(donnees)).ToLowerInvariant();

            var adresse = "https://commons.wikimedia.org/w/api.php"
                          + "?action=query&format=json&list=allimages"
                          + $"&aisha1={empreinte}"
                          + "&aiprop=url|extmetadata&ailimit=1";

            try
            {
                using var reponse = await _http.GetAsync(adresse, ct);
                if (!reponse.IsSuccessStatusCode) return null;

                using var flux = await reponse.Content.ReadAsStreamAsync(ct);
                using var document = await JsonDocument.ParseAsync(flux, cancellationToken: ct);

                if (!document.RootElement.TryGetProperty("query", out var requete)) return null;
                if (!requete.TryGetProperty("allimages", out var images)) return null;
                if (images.GetArrayLength() == 0) return null;

                var image = images[0];
                var meta = image.TryGetProperty("extmetadata", out var m) ? m : default;

                return new OriginePlanche(
                    Auteur: EnTexte(Champ(meta, "Artist")) ?? "Wikimedia Commons",
                    Source: Texte(image, "descriptionurl"),
                    Licence: Champ(meta, "LicenseShortName")?.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Commons injoignable pour l'origine d'une planche.");
                return null;
            }
        }

        private static string? Texte(JsonElement element, string nom) =>
            element.TryGetProperty(nom, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;

        private static string? Champ(JsonElement meta, string nom) =>
            meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(nom, out var c)
                ? Texte(c, "value")
                : null;

        /// <summary>
        /// Commons rend l'auteur en HTML, suivi des liens d'interface du wiki.
        /// Sans ce nettoyage, le crédit affiché sous la planche devenait
        /// « Lvcvlvs ( d · contributions ) » — le nom, puis deux boutons de
        /// navigation qui n'ont rien à faire devant un élève.
        /// </summary>
        private static string? EnTexte(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            var propre = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ");
            propre = System.Text.RegularExpressions.Regex.Replace(propre, "&[a-z]+;", " ");
            propre = System.Text.RegularExpressions.Regex.Replace(propre, @"\s+", " ");
            propre = System.Text.RegularExpressions.Regex.Replace(
                propre,
                @"\s*\(\s*(d|discussion|talk|c|contribs?|contributions)\s*([·|]\s*[^)]*)?\)",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            propre = propre.Trim();

            return propre.Length == 0 ? null : propre[..Math.Min(200, propre.Length)];
        }
    }
}
