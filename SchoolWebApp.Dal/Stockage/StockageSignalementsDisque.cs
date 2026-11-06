using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>
    /// Les pièces jointes des signalements, sur le disque — voir <see cref="IStockageSignalements"/>.
    /// Réglage <c>Stockage:Signalements</c> ; en production <c>/donnees/signalements</c>,
    /// monté depuis l'hôte.
    /// </summary>
    public class StockageSignalementsDisque : IStockageSignalements
    {
        private readonly string _racine;
        private readonly ILogger<StockageSignalementsDisque> _logger;

        public StockageSignalementsDisque(IConfiguration configuration, ILogger<StockageSignalementsDisque> logger)
        {
            _logger = logger;

            var configure = configuration["Stockage:Signalements"];
            _racine = Path.GetFullPath(string.IsNullOrWhiteSpace(configure)
                ? Path.Combine(AppContext.BaseDirectory, "stockage", "signalements")
                : configure);
        }

        public bool Pret => File.Exists(Path.Combine(_racine, StockagePlanchesDisque.Marque));

        public async Task<string> EcrireAsync(int signalementId, byte[] donnees, string nom, CancellationToken ct = default)
        {
            if (donnees.Length == 0) throw new ArgumentException("Fichier vide.", nameof(donnees));

            var extension = Path.GetExtension(nom);
            if (extension.Length > 10 || extension.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '.')) extension = string.Empty;

            // UN NOM TIRÉ AU SORT : celui du fichier d'origine ne touche jamais le disque.
            var relatif = Path.Combine(signalementId.ToString(), $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
            var cible = Chemin(relatif);
            Directory.CreateDirectory(Path.GetDirectoryName(cible)!);

            var temporaire = cible + ".ecriture";
            await File.WriteAllBytesAsync(temporaire, donnees, ct);
            File.Move(temporaire, cible, overwrite: true);

            if (IStockagePlanches.Empreinte(await File.ReadAllBytesAsync(cible, ct)) != IStockagePlanches.Empreinte(donnees))
            {
                File.Delete(cible);
                throw new IOException($"Piece de signalement relue differente de celle ecrite : {relatif}.");
            }

            return relatif;
        }

        public async Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default)
        {
            try
            {
                var chemin = Chemin(cheminRelatif);
                if (File.Exists(chemin)) return await File.ReadAllBytesAsync(chemin, ct);

                _logger.LogError("Piece de signalement absente du disque : {Chemin}.", cheminRelatif);
                return null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Piece de signalement illisible : {Chemin}.", cheminRelatif);
                return null;
            }
        }

        public void Supprimer(string? cheminRelatif)
        {
            if (string.IsNullOrWhiteSpace(cheminRelatif)) return;

            try
            {
                var chemin = Chemin(cheminRelatif);
                if (File.Exists(chemin)) File.Delete(chemin);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Piece de signalement non effacee : {Chemin}.", cheminRelatif);
            }
        }

        public void SupprimerDossier(int signalementId)
        {
            try
            {
                var dossier = Chemin(signalementId.ToString());
                if (Directory.Exists(dossier)) Directory.Delete(dossier, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dossier de signalement non efface : {Id}.", signalementId);
            }
        }

        public IReadOnlyList<int> DossiersAnciens()
        {
            if (!Directory.Exists(_racine)) return Array.Empty<int>();

            var limite = DateTime.UtcNow.AddDays(-1);

            return Directory.EnumerateDirectories(_racine)
                .Where(d => Directory.GetCreationTimeUtc(d) < limite)
                .Select(d => int.TryParse(Path.GetFileName(d), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }

        /// <summary>Le chemin absolu, en refusant tout ce qui sortirait de la racine.</summary>
        private string Chemin(string cheminRelatif)
        {
            var complet = Path.GetFullPath(Path.Combine(_racine, cheminRelatif));
            var racine = _racine.EndsWith(Path.DirectorySeparatorChar) ? _racine : _racine + Path.DirectorySeparatorChar;
            if (!complet.StartsWith(racine, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chemin de piece hors du dossier des signalements.");
            }

            return complet;
        }
    }
}
