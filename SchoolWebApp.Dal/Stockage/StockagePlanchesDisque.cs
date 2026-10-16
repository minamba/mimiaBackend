using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>
    /// Les images des planches rangées sur le disque — voir `IStockagePlanches`.
    ///
    /// UN DOSSIER PLAT, un fichier par (clé, variante, contenu) :
    /// `svt-respiratoire--legende--3f9a0c1b2d4e.png`. Quelques centaines de
    /// planches tiennent sans peine dans un seul dossier, et le nom se lit :
    /// on retrouve une planche à l'œil dans le dossier sauvegardé.
    ///
    /// L'EMPREINTE DANS LE NOM rend chaque fichier immuable : une planche
    /// remplacée donne un nouveau fichier, l'ancien n'est effacé qu'une fois la
    /// base à jour. Jamais un instant où la base pointe vers une image à moitié
    /// écrite.
    /// </summary>
    public class StockagePlanchesDisque : IStockagePlanches
    {
        /// <summary>Le fichier posé à la main dans le dossier monté de l'hôte.</summary>
        public const string Marque = "STOCKAGE-MIMIA";

        private readonly string _racine;
        private readonly ILogger<StockagePlanchesDisque> _logger;

        public StockagePlanchesDisque(IConfiguration configuration, ILogger<StockagePlanchesDisque> logger)
        {
            _logger = logger;

            var configure = configuration["Stockage:Planches"];
            _racine = Path.GetFullPath(string.IsNullOrWhiteSpace(configure)
                ? Path.Combine(AppContext.BaseDirectory, "stockage", "planches")
                : configure);
        }

        public bool Pret => File.Exists(Path.Combine(_racine, Marque));

        public async Task<string> EcrireAsync(
            string cle, string variante, byte[] donnees, string empreinte, string? typeMime, CancellationToken ct = default)
        {
            if (donnees.Length == 0) throw new ArgumentException("Image vide.", nameof(donnees));

            var nom = $"{Propre(cle)}--{Propre(variante)}--{empreinte[..12]}{Extension(typeMime)}";
            var cible = Chemin(nom);

            // Déjà là, avec le même contenu (une planche remise à l'identique) :
            // rien à écrire.
            if (File.Exists(cible)
                && IStockagePlanches.Empreinte(await File.ReadAllBytesAsync(cible, ct)) == empreinte)
            {
                return nom;
            }

            // ÉCRIT À CÔTÉ, PUIS RENOMMÉ : un arrêt en pleine écriture laisse un
            // fichier temporaire orphelin, jamais une image tronquée sous le
            // vrai nom.
            var temporaire = cible + ".ecriture";
            await File.WriteAllBytesAsync(temporaire, donnees, ct);
            File.Move(temporaire, cible, overwrite: true);

            // RELU ET COMPARÉ avant de rendre la main : c'est sur la foi de ce
            // retour que l'appelant retire l'image de la base.
            var relu = await File.ReadAllBytesAsync(cible, ct);
            if (IStockagePlanches.Empreinte(relu) != empreinte)
            {
                throw new IOException($"Planche relue differente de celle ecrite : {nom}.");
            }

            return nom;
        }

        public async Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cheminRelatif)) return null;

            try
            {
                var chemin = Chemin(cheminRelatif);
                if (!File.Exists(chemin))
                {
                    _logger.LogError("Planche absente du disque : {Chemin}.", cheminRelatif);
                    return null;
                }

                return await File.ReadAllBytesAsync(chemin, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Planche illisible : {Chemin}.", cheminRelatif);
                return null;
            }
        }

        public bool Supprimer(string cheminRelatif)
        {
            if (string.IsNullOrWhiteSpace(cheminRelatif)) return false;

            try
            {
                var chemin = Chemin(cheminRelatif);
                if (File.Exists(chemin)) File.Delete(chemin);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Planche non effacee du disque : {Chemin}.", cheminRelatif);
                return false;
            }
        }

        /// <summary>Une clé de catalogue en nom de fichier : lettres, chiffres, tirets.</summary>
        private static string Propre(string texte)
        {
            var propre = new string(texte.Trim().ToLowerInvariant()
                .Select(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_' ? c : '-')
                .ToArray());
            return propre.Length > 100 ? propre[..100] : propre;
        }

        private static string Extension(string? typeMime) => typeMime?.ToLowerInvariant() switch
        {
            "image/svg+xml" => ".svg",
            "image/png" => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".bin",
        };

        /// <summary>
        /// Le chemin absolu, en refusant tout ce qui sortirait de la racine :
        /// le chemin relu vient de la base, une colonne se modifie à la main.
        /// </summary>
        private string Chemin(string cheminRelatif)
        {
            var complet = Path.GetFullPath(Path.Combine(_racine, cheminRelatif));
            var racine = _racine.EndsWith(Path.DirectorySeparatorChar) ? _racine : _racine + Path.DirectorySeparatorChar;

            if (!complet.StartsWith(racine, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Chemin de planche hors de la racine de stockage.");
            }

            return complet;
        }
    }
}
