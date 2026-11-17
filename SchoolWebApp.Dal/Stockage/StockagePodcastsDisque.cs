using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>L'audio des podcasts sur le disque — voir `IStockagePodcasts`.</summary>
    public class StockagePodcastsDisque : IStockagePodcasts
    {
        private const string Dossier = "podcasts";

        private readonly string _racine;
        private readonly ILogger<StockagePodcastsDisque> _logger;

        public StockagePodcastsDisque(IConfiguration configuration, ILogger<StockagePodcastsDisque> logger)
        {
            _logger = logger;

            // La même racine que l'audio des archives : en conteneur, le montage /donnees/audio ; en local, un dossier
            // qui se crée tout seul au premier épisode.
            var configure = configuration["Stockage:Audio"];
            var audio = string.IsNullOrWhiteSpace(configure)
                ? Path.Combine(AppContext.BaseDirectory, "archives", "audio")
                : configure;

            _racine = Path.GetFullPath(Path.Combine(audio, Dossier));
        }

        public async Task<string?> EcrireAsync(int eleveId, int coursId, byte[] donnees, CancellationToken ct = default)
        {
            if (donnees.Length == 0) return null;

            try
            {
                var relatif = Path.Combine(eleveId.ToString(), coursId.ToString(), Guid.NewGuid().ToString("N") + ".opus");
                var cible = Chemin(relatif);
                Directory.CreateDirectory(Path.GetDirectoryName(cible)!);
                await File.WriteAllBytesAsync(cible, donnees, ct);

                // Barres obliques : le chemin part en base et peut être relu d'un autre système.
                return relatif.Replace(Path.DirectorySeparatorChar, '/');
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Audio de podcast non ecrit sur le disque (cours {CoursId}).", coursId);
                return null;
            }
        }

        public Stream? Ouvrir(string cheminRelatif)
        {
            if (string.IsNullOrWhiteSpace(cheminRelatif)) return null;
            try
            {
                var chemin = Chemin(cheminRelatif);
                return File.Exists(chemin) ? new FileStream(chemin, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true) : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Audio de podcast illisible.");
                return null;
            }
        }

        public async Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default)
        {
            await using var flux = Ouvrir(cheminRelatif);
            if (flux is null) return null;
            using var copie = new MemoryStream();
            await flux.CopyToAsync(copie, ct);
            return copie.ToArray();
        }

        public void SupprimerCours(int eleveId, int coursId) =>
            SupprimerDossier(Path.Combine(eleveId.ToString(), coursId.ToString()));

        public void SupprimerEleve(int eleveId) => SupprimerDossier(eleveId.ToString());

        public IReadOnlyList<(int EleveId, int CoursId)> Dossiers()
        {
            var trouves = new List<(int, int)>();
            if (!Directory.Exists(_racine)) return trouves;

            try
            {
                foreach (var eleve in Directory.EnumerateDirectories(_racine))
                {
                    if (!int.TryParse(Path.GetFileName(eleve), out var eleveId)) continue;
                    foreach (var cours in Directory.EnumerateDirectories(eleve))
                    {
                        if (int.TryParse(Path.GetFileName(cours), out var coursId)) trouves.Add((eleveId, coursId));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Dossiers des podcasts illisibles.");
            }

            return trouves;
        }

        private void SupprimerDossier(string relatif)
        {
            try
            {
                var chemin = Chemin(relatif);
                if (Directory.Exists(chemin)) Directory.Delete(chemin, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Dossier de podcasts non efface : {Dossier}.", relatif);
            }
        }

        /// <summary>Le chemin absolu, en refusant tout ce qui sortirait de la racine (le chemin vient de la base).</summary>
        private string Chemin(string relatif)
        {
            var complet = Path.GetFullPath(Path.Combine(_racine, relatif));
            if (!complet.StartsWith(_racine + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Chemin de podcast hors de la racine de stockage.");
            }

            return complet;
        }
    }
}
