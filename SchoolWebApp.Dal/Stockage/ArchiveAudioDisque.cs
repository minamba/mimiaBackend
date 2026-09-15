using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>
    /// Les audio rangés sur le disque, sous une racine configurée.
    ///
    /// L'arborescence est datée — `2026/09/xxxxx.wav` — et non pas plate. Un
    /// seul dossier finirait par contenir des centaines de milliers de
    /// fichiers, ce que ni l'explorateur, ni `ls`, ni une sauvegarde
    /// incrémentale n'aiment. Découpé par mois, chaque dossier reste de la
    /// taille d'un mois de trafic, et une purge par ancienneté se lit
    /// directement dans les noms de dossiers.
    ///
    /// Le nom de fichier est un identifiant tiré au hasard, jamais l'identifiant
    /// de la ligne : il est connu AVANT l'insertion, ce qui évite d'insérer
    /// puis de revenir écrire le chemin. Il n'est pas devinable non plus, ce
    /// qui ne remplace pas le contrôle d'accès mais ne l'affaiblit pas.
    /// </summary>
    public class ArchiveAudioDisque : IArchiveAudio
    {
        private readonly string _racine;
        private readonly ILogger<ArchiveAudioDisque> _logger;

        public ArchiveAudioDisque(IConfiguration configuration, ILogger<ArchiveAudioDisque> logger)
        {
            _logger = logger;

            // Par défaut sous le dossier de l'application : en conteneur, c'est
            // le point de montage à déclarer dans le docker-compose ; en local,
            // un dossier qui se crée tout seul au premier archivage.
            var configure = configuration["Stockage:Audio"];

            _racine = string.IsNullOrWhiteSpace(configure)
                ? Path.Combine(AppContext.BaseDirectory, "archives", "audio")
                : configure;
        }

        public async Task<string?> EcrireAsync(
            byte[] donnees, string extension, CancellationToken ct = default)
        {
            if (donnees.Length == 0) return null;

            try
            {
                var maintenant = DateTime.UtcNow;
                var dossierRelatif = Path.Combine(
                    maintenant.ToString("yyyy"), maintenant.ToString("MM"));

                var nom = Guid.NewGuid().ToString("N") + extension;
                var cheminRelatif = Path.Combine(dossierRelatif, nom);

                var cible = Chemin(cheminRelatif);
                Directory.CreateDirectory(Path.GetDirectoryName(cible)!);

                await File.WriteAllBytesAsync(cible, donnees, ct);

                // Toujours avec des barres obliques : le chemin part en base et
                // peut être relu depuis un autre système que celui qui l'a
                // écrit. Windows accepte les deux, Linux une seule.
                return cheminRelatif.Replace(Path.DirectorySeparatorChar, '/');
            }
            catch (Exception ex)
            {
                // Un disque plein ne doit pas faire perdre l'archive : la ligne
                // texte s'enregistre sans son, et l'exercice reste consultable.
                _logger.LogError(ex, "Audio non ecrit sur le disque.");
                return null;
            }
        }

        public async Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cheminRelatif)) return null;

            try
            {
                var chemin = Chemin(cheminRelatif);
                if (!File.Exists(chemin)) return null;

                return await File.ReadAllBytesAsync(chemin, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audio illisible : {Chemin}.", cheminRelatif);
                return null;
            }
        }

        public bool Supprimer(string cheminRelatif)
        {
            if (string.IsNullOrWhiteSpace(cheminRelatif)) return false;

            try
            {
                var chemin = Chemin(cheminRelatif);

                // Déjà parti : le résultat voulu est atteint, on le dit.
                if (!File.Exists(chemin)) return true;

                File.Delete(chemin);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audio non efface : {Chemin}.", cheminRelatif);
                return false;
            }
        }

        /// <summary>
        /// Le chemin absolu, en refusant tout ce qui sortirait de la racine.
        ///
        /// LE CHEMIN VIENT DE LA BASE, DONC IL EST À VÉRIFIER. Il y a été écrit
        /// par cette classe, mais une colonne se modifie, se restaure d'une
        /// sauvegarde, se remplit par un script : un « ../../etc/passwd » qui
        /// y arriverait ne doit pas devenir une lecture de fichier arbitraire.
        /// </summary>
        private string Chemin(string cheminRelatif)
        {
            var racine = Path.GetFullPath(_racine);
            var complet = Path.GetFullPath(Path.Combine(racine, cheminRelatif));

            if (!complet.StartsWith(racine, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Chemin d'archive audio hors de la racine de stockage.");
            }

            return complet;
        }
    }
}
