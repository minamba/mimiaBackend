using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// LES FICHIERS D'UN SIGNALEMENT DISPARU NE RESTENT PAS SUR LE DISQUE. Supprimer un
    /// signalement à l'écran efface son dossier ; mais un compte parent supprimé emporte
    /// ses signalements en cascade, directement dans la base, sans que personne touche
    /// au disque. Ce balayage quotidien efface les dossiers devenus orphelins : ce sont
    /// des données personnelles, et elles doivent partir avec le compte.
    /// </summary>
    public class PurgePiecesSignalementsWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IStockageSignalements _stockage;
        private readonly ILogger<PurgePiecesSignalementsWorker> _logger;

        public PurgePiecesSignalementsWorker(
            IServiceScopeFactory scopes, IStockageSignalements stockage, ILogger<PurgePiecesSignalementsWorker> logger)
        {
            _scopes = scopes;
            _stockage = stockage;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct);

                while (!ct.IsCancellationRequested)
                {
                    await BalayerAsync(ct);
                    await Task.Delay(TimeSpan.FromDays(1), ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
        }

        private async Task BalayerAsync(CancellationToken ct)
        {
            try
            {
                if (!_stockage.Pret)
                {
                    _logger.LogWarning("Stockage des signalements NON PRET (marque STOCKAGE-MIMIA absente) : aucune piece jointe possible.");
                    return;
                }

                var dossiers = _stockage.DossiersAnciens();
                if (dossiers.Count == 0) return;

                using var portee = _scopes.CreateScope();
                var existants = await portee.ServiceProvider
                    .GetRequiredService<ISignalementRepository>()
                    .IdsExistantsAsync(dossiers, ct);

                foreach (var orphelin in dossiers.Except(existants))
                {
                    _stockage.SupprimerDossier(orphelin);
                    _logger.LogWarning("Dossier orphelin du signalement {Id} efface.", orphelin);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Balayage des pieces de signalements interrompu.");
            }
        }
    }
}
