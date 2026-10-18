using System.Security.Cryptography;
using System.Text;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LE MOT DE PASSE DES BOÎTES MAIL DES ADMINISTRATEURS, CHIFFRÉ (17/10/2026).
    ///
    /// Camara : « la première fois que l'utilisateur clique sur l'onglet mail, lui
    /// demander son mot de passe pour récupérer sa boîte ». Pour relire la boîte
    /// chez LWS à chaque ouverture, il faut retrouver ce mot de passe : il est donc
    /// chiffré (AES-GCM, authentifié), jamais haché ni gardé en clair.
    ///
    /// LA CLÉ VIT HORS DU CODE ET DE LA BASE : `Messagerie:CleBoites` ou la
    /// variable `MIMIA_CLE_BOITES`. Une copie de la base volée sans elle ne
    /// donne aucun mot de passe. Sans clé, la fonction est simplement
    /// indisponible — rien n'est enregistré. CHANGER LA CLÉ rend les mots de
    /// passe gardés illisibles : chacun les ressaisit, rien d'autre ne casse.
    /// </summary>
    public interface IChiffrementBoites
    {
        bool Disponible { get; }

        string Chiffrer(string clair);

        /// <summary>Null si le texte a été chiffré avec une autre clé, ou altéré.</summary>
        string? Dechiffrer(string chiffre);
    }

    public class ChiffrementBoites : IChiffrementBoites
    {
        private const int TailleNonce = 12;
        private const int TailleEtiquette = 16;

        private readonly byte[]? _cle;

        public ChiffrementBoites(IConfiguration configuration, ILogger<ChiffrementBoites> logger)
        {
            var cle = configuration["Messagerie:CleBoites"]
                      ?? Environment.GetEnvironmentVariable("MIMIA_CLE_BOITES");

            if (string.IsNullOrWhiteSpace(cle))
            {
                logger.LogWarning(
                    "Messagerie:CleBoites (ou MIMIA_CLE_BOITES) absente : les boites mail des "
                    + "administrateurs ne peuvent pas etre connectees.");
                return;
            }

            // N'importe quelle phrase secrète ; la clé AES en est l'empreinte (256 bits).
            _cle = SHA256.HashData(Encoding.UTF8.GetBytes(cle));
        }

        public bool Disponible => _cle is not null;

        public string Chiffrer(string clair)
        {
            if (_cle is null) throw new InvalidOperationException("Cle de chiffrement des boites absente.");

            var nonce = RandomNumberGenerator.GetBytes(TailleNonce);
            var octets = Encoding.UTF8.GetBytes(clair);
            var chiffre = new byte[octets.Length];
            var etiquette = new byte[TailleEtiquette];

            using var aes = new AesGcm(_cle, TailleEtiquette);
            aes.Encrypt(nonce, octets, chiffre, etiquette);

            return Convert.ToBase64String([.. nonce, .. etiquette, .. chiffre]);
        }

        public string? Dechiffrer(string chiffre)
        {
            if (_cle is null) return null;

            try
            {
                var tout = Convert.FromBase64String(chiffre);
                if (tout.Length < TailleNonce + TailleEtiquette) return null;

                var nonce = tout.AsSpan(0, TailleNonce);
                var etiquette = tout.AsSpan(TailleNonce, TailleEtiquette);
                var donnees = tout.AsSpan(TailleNonce + TailleEtiquette);
                var clair = new byte[donnees.Length];

                using var aes = new AesGcm(_cle, TailleEtiquette);
                aes.Decrypt(nonce, donnees, etiquette, clair);
                return Encoding.UTF8.GetString(clair);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return null;
            }
        }
    }
}
