using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>
    /// Les pièces des templates de courriel, sur le disque (18/10/2026) — voir
    /// <see cref="IStockageModelesMail"/>. Réglage <c>Stockage:ModelesMail</c> ;
    /// en production <c>/donnees/modeles-mail</c>, monté depuis l'hôte et copié
    /// chaque nuit vers Google Drive (exploitation/sauvegarde-base.sh).
    /// </summary>
    public class StockageModelesMailDisque : IStockageModelesMail
    {
        private readonly string _racine;
        private readonly ILogger<StockageModelesMailDisque> _logger;

        public StockageModelesMailDisque(IConfiguration configuration, ILogger<StockageModelesMailDisque> logger)
        {
            _logger = logger;

            var configure = configuration["Stockage:ModelesMail"];
            _racine = Path.GetFullPath(string.IsNullOrWhiteSpace(configure)
                ? Path.Combine(AppContext.BaseDirectory, "stockage", "modeles-mail")
                : configure);
        }

        public bool Pret => File.Exists(Path.Combine(_racine, StockagePlanchesDisque.Marque));

        public async Task<string> EcrireAsync(byte[] donnees, string? nomFichier, CancellationToken ct = default)
        {
            if (donnees.Length == 0) throw new ArgumentException("Fichier vide.", nameof(donnees));

            var extension = Path.GetExtension(nomFichier ?? string.Empty);
            if (extension.Length > 10 || extension.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '.')) extension = ".bin";
            var empreinte = IStockagePlanches.Empreinte(donnees);
            var nom = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

            var cible = Chemin(nom);
            var temporaire = cible + ".ecriture";
            await File.WriteAllBytesAsync(temporaire, donnees, ct);
            File.Move(temporaire, cible, overwrite: true);

            // RELU ET COMPARÉ : c'est sur la foi de ce retour que la base se vide.
            if (IStockagePlanches.Empreinte(await File.ReadAllBytesAsync(cible, ct)) != empreinte)
            {
                throw new IOException($"Piece de template relue differente de celle ecrite : {nom}.");
            }

            return nom;
        }

        public async Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default)
        {
            try
            {
                var chemin = Chemin(cheminRelatif);
                if (File.Exists(chemin)) return await File.ReadAllBytesAsync(chemin, ct);

                _logger.LogError("Piece de template absente du disque : {Chemin}.", cheminRelatif);
                return null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Piece de template illisible : {Chemin}.", cheminRelatif);
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
                _logger.LogWarning(ex, "Piece de template non effacee : {Chemin}.", cheminRelatif);
            }
        }

        /// <summary>Le chemin absolu, en refusant tout ce qui sortirait de la racine.</summary>
        private string Chemin(string cheminRelatif)
        {
            var complet = Path.GetFullPath(Path.Combine(_racine, cheminRelatif));
            var racine = _racine.EndsWith(Path.DirectorySeparatorChar) ? _racine : _racine + Path.DirectorySeparatorChar;
            if (!complet.StartsWith(racine, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chemin de piece hors du dossier des templates.");
            }

            return complet;
        }
    }
}
