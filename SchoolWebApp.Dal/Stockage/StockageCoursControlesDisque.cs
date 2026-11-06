using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Stockage
{
    /// <summary>
    /// Le cours importé avec un contrôle, sur le disque — voir <see cref="IStockageCoursControles"/>.
    /// Réglage <c>Stockage:CoursControles</c> ; en production <c>/donnees/cours-controles</c>,
    /// monté depuis l'hôte. Même construction que les pièces des signalements.
    /// </summary>
    public class StockageCoursControlesDisque : IStockageCoursControles
    {
        private readonly string _racine;
        private readonly ILogger<StockageCoursControlesDisque> _logger;

        public StockageCoursControlesDisque(IConfiguration configuration, ILogger<StockageCoursControlesDisque> logger)
        {
            _logger = logger;

            var configure = configuration["Stockage:CoursControles"];
            _racine = Path.GetFullPath(string.IsNullOrWhiteSpace(configure)
                ? Path.Combine(AppContext.BaseDirectory, "stockage", "cours-controles")
                : configure);
        }

        public bool Pret => File.Exists(Path.Combine(_racine, StockagePlanchesDisque.Marque));

        public async Task<string> EcrireAsync(int eleveId, int controleId, byte[] donnees, string nom, CancellationToken ct = default)
        {
            if (donnees.Length == 0) throw new ArgumentException("Fichier vide.", nameof(donnees));

            var extension = Path.GetExtension(nom);
            if (extension.Length > 10 || extension.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '.')) extension = string.Empty;

            // UN NOM TIRÉ AU SORT : celui du fichier d'origine ne touche jamais le disque.
            var relatif = Path.Combine(eleveId.ToString(), controleId.ToString(), $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
            var cible = Chemin(relatif);
            Directory.CreateDirectory(Path.GetDirectoryName(cible)!);

            var temporaire = cible + ".ecriture";
            await File.WriteAllBytesAsync(temporaire, donnees, ct);
            File.Move(temporaire, cible, overwrite: true);

            if (IStockagePlanches.Empreinte(await File.ReadAllBytesAsync(cible, ct)) != IStockagePlanches.Empreinte(donnees))
            {
                File.Delete(cible);
                throw new IOException($"Document de cours relu different de celui ecrit : {relatif}.");
            }

            return relatif;
        }

        public async Task<byte[]?> LireAsync(string cheminRelatif, CancellationToken ct = default)
        {
            try
            {
                var chemin = Chemin(cheminRelatif);
                if (File.Exists(chemin)) return await File.ReadAllBytesAsync(chemin, ct);

                _logger.LogError("Document de cours absent du disque : {Chemin}.", cheminRelatif);
                return null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Document de cours illisible : {Chemin}.", cheminRelatif);
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

                // Le dossier du contrôle part avec son dernier fichier, puis celui de l'élève.
                var dossier = Path.GetDirectoryName(chemin);
                for (var i = 0; i < 2 && dossier is not null && dossier.Length > _racine.Length; i += 1)
                {
                    if (!Directory.Exists(dossier) || Directory.EnumerateFileSystemEntries(dossier).Any()) break;
                    Directory.Delete(dossier);
                    dossier = Path.GetDirectoryName(dossier);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Document de cours non efface : {Chemin}.", cheminRelatif);
            }
        }

        public void SupprimerDossier(int eleveId, int controleId)
        {
            try
            {
                var dossier = Chemin(Path.Combine(eleveId.ToString(), controleId.ToString()));
                if (Directory.Exists(dossier)) Directory.Delete(dossier, recursive: true);

                var eleve = Path.GetDirectoryName(dossier);
                if (eleve is not null && Directory.Exists(eleve) && !Directory.EnumerateFileSystemEntries(eleve).Any())
                {
                    Directory.Delete(eleve);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dossier de cours non efface : eleve {EleveId}, controle {ControleId}.", eleveId, controleId);
            }
        }

        public IReadOnlyList<(int EleveId, int ControleId)> DossiersAnciens()
        {
            if (!Directory.Exists(_racine)) return Array.Empty<(int, int)>();

            var limite = DateTime.UtcNow.AddDays(-1);
            var sortie = new List<(int, int)>();

            foreach (var eleve in Directory.EnumerateDirectories(_racine))
            {
                if (!int.TryParse(Path.GetFileName(eleve), out var eleveId) || eleveId <= 0) continue;

                foreach (var controle in Directory.EnumerateDirectories(eleve))
                {
                    if (Directory.GetCreationTimeUtc(controle) >= limite) continue;
                    if (int.TryParse(Path.GetFileName(controle), out var controleId) && controleId > 0) sortie.Add((eleveId, controleId));
                }
            }

            return sortie;
        }

        /// <summary>Le chemin absolu, en refusant tout ce qui sortirait de la racine.</summary>
        private string Chemin(string cheminRelatif)
        {
            var complet = Path.GetFullPath(Path.Combine(_racine, cheminRelatif));
            var racine = _racine.EndsWith(Path.DirectorySeparatorChar) ? _racine : _racine + Path.DirectorySeparatorChar;
            if (!complet.StartsWith(racine, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chemin de document hors du dossier des cours de controle.");
            }

            return complet;
        }
    }
}
