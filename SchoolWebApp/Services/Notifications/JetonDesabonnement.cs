using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.Notifications
{
    public interface IJetonDesabonnement
    {
        /// <summary>Faux si aucune clé n'est configurée : aucun lien ne peut être fabriqué.</summary>
        bool Disponible { get; }

        /// <summary>Le lien « Ne plus recevoir ces messages », vers la page du site.</summary>
        string? Lien(int parentId, string categorie);

        /// <summary>
        /// Le lien de désabonnement en un clic des messageries (en-tête
        /// `List-Unsubscribe-Post`). Nul si l'adresse publique de l'API n'est
        /// pas configurée.
        /// </summary>
        string? LienUnClic(int parentId, string categorie);

        /// <summary>Le parent et la catégorie d'un jeton, ou nul s'il est faux ou abîmé.</summary>
        (int ParentId, string Categorie)? Lire(string? jeton);

        /// <summary>
        /// LE JETON D'ANNULATION (19/11/2026, retour d'Omar) : remis par la désinscription à la page qui vient de la faire,
        /// il permet « Je me suis trompé, me réabonner » pendant <paramref name="validite"/> seulement. Nul sans clé.
        /// </summary>
        string? CreerAnnulation(int parentId, string categorie, DateTime maintenantUtc, TimeSpan validite);

        /// <summary>Vrai si ce jeton d'annulation est authentique, pour ce parent et cette catégorie, et pas expiré.</summary>
        bool AnnulationValide(string? jeton, int parentId, string categorie, DateTime maintenantUtc);
    }

    /// <summary>
    /// Les liens de désabonnement.
    ///
    /// UN JETON SIGNÉ, SANS COMPTE NI CONNEXION. Un parent qui ne veut plus
    /// recevoir de demandes d'avis doit pouvoir le dire en un clic depuis sa
    /// messagerie — lui demander de se connecter d'abord, c'est garantir qu'il
    /// cliquera plutôt sur « Signaler comme spam ». Le jeton porte le parent et
    /// la catégorie, signés : personne ne peut en fabriquer un pour un autre.
    ///
    /// UNE CLÉ DE CONFIGURATION, PAS LA PROTECTION DES DONNÉES D'ASP.NET. Les
    /// clés de cette dernière vivent sur le disque du conteneur et changent à
    /// chaque `docker restart` : tous les liens déjà envoyés cesseraient de
    /// fonctionner au premier déploiement. La clé vient de
    /// `Courrier:CleDesabonnement` ou de la variable `MIMIA_CLE_DESABONNEMENT`,
    /// et NE DOIT JAMAIS CHANGER une fois des courriels partis.
    ///
    /// EN DÉVELOPPEMENT, SANS CLÉ, une clé jetable est tirée au démarrage : les
    /// liens marchent le temps d'une session de travail, et personne n'a
    /// besoin de configurer quoi que ce soit pour essayer.
    /// </summary>
    public class JetonDesabonnement : IJetonDesabonnement
    {
        private readonly byte[]? _cle;
        private readonly string _urlSite;
        private readonly string? _urlApi;

        public JetonDesabonnement(
            IConfiguration configuration,
            IHostEnvironment environnement,
            IOptions<OptionsEmail> options,
            ILogger<JetonDesabonnement> logger)
        {
            var cle = configuration["Courrier:CleDesabonnement"]
                      ?? Environment.GetEnvironmentVariable("MIMIA_CLE_DESABONNEMENT");

            if (!string.IsNullOrWhiteSpace(cle))
            {
                _cle = Encoding.UTF8.GetBytes(cle);
            }
            else if (environnement.IsDevelopment())
            {
                _cle = RandomNumberGenerator.GetBytes(32);
                logger.LogWarning(
                    "Courrier:CleDesabonnement absente : cle jetable de developpement. Les liens de "
                    + "desabonnement ne survivront pas au redemarrage.");
            }
            else
            {
                logger.LogError(
                    "Courrier:CleDesabonnement (ou MIMIA_CLE_DESABONNEMENT) absente : les courriels qui "
                    + "portent un lien de desabonnement NE PARTIRONT PAS.");
            }

            _urlSite = (options.Value.UrlSite ?? "https://mimia.fr").TrimEnd('/');
            _urlApi = configuration["Courrier:UrlApi"]?.TrimEnd('/');
        }

        public bool Disponible => _cle is not null;

        public string? Lien(int parentId, string categorie)
        {
            var jeton = Creer(parentId, categorie);
            return jeton is null ? null : $"{_urlSite}/desabonnement?jeton={Uri.EscapeDataString(jeton)}";
        }

        public string? LienUnClic(int parentId, string categorie)
        {
            if (string.IsNullOrWhiteSpace(_urlApi)) return null;

            var jeton = Creer(parentId, categorie);
            return jeton is null ? null : $"{_urlApi}/desabonnement/un-clic?jeton={Uri.EscapeDataString(jeton)}";
        }

        public (int ParentId, string Categorie)? Lire(string? jeton)
        {
            if (_cle is null || string.IsNullOrWhiteSpace(jeton)) return null;

            var morceaux = jeton.Split('.');
            if (morceaux.Length != 2) return null;

            try
            {
                var charge = WebEncoders.Base64UrlDecode(morceaux[0]);
                var signature = WebEncoders.Base64UrlDecode(morceaux[1]);

                // Comparaison à temps constant : une comparaison ordinaire
                // s'arrête au premier octet faux, et le temps de réponse
                // trahirait petit à petit la bonne signature.
                if (!CryptographicOperations.FixedTimeEquals(signature, Signer(charge))) return null;

                var texte = Encoding.UTF8.GetString(charge);
                var point = texte.IndexOf('.');
                if (point <= 0) return null;

                var categorie = texte[(point + 1)..];

                return int.TryParse(texte[..point], out var parentId) && CategorieDesabonnement.EstConnue(categorie)
                    ? (parentId, categorie)
                    : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// UNE CHARGE QUI NE PEUT PAS PASSER POUR UN LIEN DE DÉSINSCRIPTION : elle commence par « annulation. », que `Lire`
        /// refuse (ce n'est pas un numéro de parent). Et l'inverse : un lien de courriel, sans échéance, n'est jamais pris
        /// pour une annulation. Le lien d'un vieux courriel ne suffit donc plus à redonner un consentement aux offres.
        /// </summary>
        public string? CreerAnnulation(int parentId, string categorie, DateTime maintenantUtc, TimeSpan validite)
        {
            if (_cle is null) return null;

            var echeance = new DateTimeOffset(DateTime.SpecifyKind(maintenantUtc, DateTimeKind.Utc) + validite).ToUnixTimeSeconds();
            var charge = Encoding.UTF8.GetBytes($"{PrefixeAnnulation}{parentId}.{categorie}.{echeance}");
            return $"{WebEncoders.Base64UrlEncode(charge)}.{WebEncoders.Base64UrlEncode(Signer(charge))}";
        }

        public bool AnnulationValide(string? jeton, int parentId, string categorie, DateTime maintenantUtc)
        {
            if (_cle is null || string.IsNullOrWhiteSpace(jeton)) return false;

            var morceaux = jeton.Split('.');
            if (morceaux.Length != 2) return false;

            try
            {
                var charge = WebEncoders.Base64UrlDecode(morceaux[0]);
                var signature = WebEncoders.Base64UrlDecode(morceaux[1]);
                if (!CryptographicOperations.FixedTimeEquals(signature, Signer(charge))) return false;

                var texte = Encoding.UTF8.GetString(charge);
                var attendu = $"{PrefixeAnnulation}{parentId}.{categorie}.";
                if (!texte.StartsWith(attendu, StringComparison.Ordinal)) return false;

                return long.TryParse(texte[attendu.Length..], out var echeance)
                       && DateTimeOffset.FromUnixTimeSeconds(echeance) > new DateTimeOffset(DateTime.SpecifyKind(maintenantUtc, DateTimeKind.Utc));
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private const string PrefixeAnnulation = "annulation.";

        private string? Creer(int parentId, string categorie)
        {
            if (_cle is null) return null;

            var charge = Encoding.UTF8.GetBytes($"{parentId}.{categorie}");
            return $"{WebEncoders.Base64UrlEncode(charge)}.{WebEncoders.Base64UrlEncode(Signer(charge))}";
        }

        private byte[] Signer(byte[] charge) => HMACSHA256.HashData(_cle!, charge);
    }

    public static class JetonDesabonnementExtensions
    {
        /// <summary>
        /// Les en-têtes `List-Unsubscribe` d'un courriel, ou nuls sans clé.
        ///
        /// Gmail et Yahoo les exigent des expéditeurs en nombre, et affichent
        /// alors « Se désabonner » en haut du message : un parent qui n'en veut
        /// plus clique là plutôt que sur « Spam », ce qui protège la réputation
        /// de l'adresse d'envoi — celle des bilans aussi.
        ///
        /// LE DÉSABONNEMENT EN UN CLIC (`List-Unsubscribe-Post`) n'est annoncé
        /// que si l'adresse publique de l'API est connue : la messagerie y
        /// envoie directement un POST, sans passer par la page du site.
        /// </summary>
        public static IReadOnlyDictionary<string, string>? EnTetes(
            this IJetonDesabonnement jetons, int parentId, string categorie)
        {
            var lien = jetons.Lien(parentId, categorie);
            if (lien is null) return null;

            var unClic = jetons.LienUnClic(parentId, categorie);

            var enTetes = new Dictionary<string, string>
            {
                ["List-Unsubscribe"] = unClic is null ? $"<{lien}>" : $"<{unClic}>, <{lien}>",
            };

            if (unClic is not null) enTetes["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click";

            return enTetes;
        }
    }
}
