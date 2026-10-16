using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// SORT LES PLANCHES DE LA BASE, UNE FOIS, AU DÉMARRAGE — Camara, le
    /// 15/10/2026 : la base SQL Server Express plafonne à 10 Go, le disque du
    /// VPS en a 400.
    ///
    /// Tourne à chaque démarrage et ne fait rien s'il n'y a plus rien en base :
    /// le premier démarrage après la pose du dossier fait tout le travail, les
    /// suivants passent en une requête.
    ///
    /// TANT QUE LE DOSSIER N'EST PAS PRÊT (marque `STOCKAGE-MIMIA` absente), il
    /// ne déplace rien et le dit au journal : écrire dans un dossier du
    /// conteneur, que le prochain `docker compose up` effacerait, puis retirer
    /// l'image de la base, serait la perdre.
    /// </summary>
    public class DeplacementPlanchesWorker : BackgroundService
    {
        private const int Lot = 10;

        private readonly IServiceScopeFactory _scopes;
        private readonly IStockagePlanches _stockage;
        private readonly ILogger<DeplacementPlanchesWorker> _logger;

        public DeplacementPlanchesWorker(
            IServiceScopeFactory scopes, IStockagePlanches stockage, ILogger<DeplacementPlanchesWorker> logger)
        {
            _scopes = scopes;
            _stockage = stockage;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                // Laisse l'application démarrer et répondre d'abord.
                await Task.Delay(TimeSpan.FromSeconds(20), ct);

                if (!_stockage.Pret)
                {
                    _logger.LogWarning(
                        "Stockage des planches NON PRET (marque STOCKAGE-MIMIA absente) : les planches restent en base.");
                    return;
                }

                var total = 0;
                while (!ct.IsCancellationRequested)
                {
                    using var portee = _scopes.CreateScope();
                    var planches = portee.ServiceProvider.GetRequiredService<IPlancheRepository>();

                    var faites = await planches.DeplacerVersDisqueAsync(Lot, ct);
                    if (faites == 0) break;
                    total += faites;
                }

                if (total > 0) _logger.LogWarning("Planches sorties de la base vers le disque : {Nombre}.", total);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deplacement des planches vers le disque interrompu.");
            }
        }
    }
}
