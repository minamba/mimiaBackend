using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>
    /// Les pièces des brouillons de la messagerie, sur le disque (18/10/2026) —
    /// voir <see cref="IStockageBrouillonsMail"/>. Réglage <c>Stockage:BrouillonsMail</c> ;
    /// en production, un sous-dossier du montage <c>/donnees/audio</c>, qui survit
    /// aux déploiements sans être sauvegardé : ce sont des fichiers de passage.
    /// </summary>
    public class StockageBrouillonsMailDisque : IStockageBrouillonsMail
    {
        private readonly string _racine;
        private readonly ILogger<StockageBrouillonsMailDisque> _logger;

        public StockageBrouillonsMailDisque(IConfiguration configuration, ILogger<StockageBrouillonsMailDisque> logger)
        {
            _logger = logger;

            var configure = configuration["Stockage:BrouillonsMail"];
            _racine = Path.GetFullPath(string.IsNullOrWhiteSpace(configure)
                ? Path.Combine(AppContext.BaseDirectory, "stockage", "brouillons-mail")
                : configure);
        }

        public async Task<string> EcrireAsync(byte[] donnees, string? nomFichier, CancellationToken ct = default)
        {
            Directory.CreateDirectory(_racine);

            // Un nom neuf à chaque fois : deux brouillons ne se marchent jamais dessus.
            var extension = Path.GetExtension(nomFichier ?? string.Empty);
            if (extension.Length > 10 || extension.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '.')) extension = ".bin";
            var nom = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

            var cible = Chemin(nom);
            var temporaire = cible + ".ecriture";
            await File.WriteAllBytesAsync(temporaire, donnees, ct);
            File.Move(temporaire, cible, overwrite: true);
            return nom;
        }

        public async Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default)
        {
            try
            {
                var chemin = Chemin(cheminRelatif);
                return File.Exists(chemin) ? await File.ReadAllBytesAsync(chemin, ct) : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Piece de brouillon illisible : {Chemin}.", cheminRelatif);
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
                _logger.LogWarning(ex, "Piece de brouillon non effacee : {Chemin}.", cheminRelatif);
            }
        }

        /// <summary>Le chemin absolu, en refusant tout ce qui sortirait de la racine.</summary>
        private string Chemin(string cheminRelatif)
        {
            var complet = Path.GetFullPath(Path.Combine(_racine, cheminRelatif));
            var racine = _racine.EndsWith(Path.DirectorySeparatorChar) ? _racine : _racine + Path.DirectorySeparatorChar;
            if (!complet.StartsWith(racine, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chemin de piece hors du dossier des brouillons.");
            }

            return complet;
        }
    }
}
