using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// SORT LES PIÈCES DES TEMPLATES DE LA BASE, UNE FOIS, AU DÉMARRAGE — Camara,
    /// le 18/10/2026. Même méthode que <see cref="DeplacementPlanchesWorker"/> :
    /// écrit, RELIT, compare, puis seulement vide la base ; ne fait rien tant que
    /// la marque STOCKAGE-MIMIA manque dans le dossier des templates.
    /// </summary>
    public class DeplacementPiecesModelesWorker : BackgroundService
    {
        private const int Lot = 10;

        private readonly IServiceScopeFactory _scopes;
        private readonly IStockageModelesMail _stockage;
        private readonly ILogger<DeplacementPiecesModelesWorker> _logger;

        public DeplacementPiecesModelesWorker(
            IServiceScopeFactory scopes, IStockageModelesMail stockage, ILogger<DeplacementPiecesModelesWorker> logger)
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
                await Task.Delay(TimeSpan.FromSeconds(25), ct);

                if (!_stockage.Pret)
                {
                    _logger.LogWarning(
                        "Stockage des templates NON PRET (marque STOCKAGE-MIMIA absente) : leurs pieces restent en base.");
                    return;
                }

                var total = 0;
                while (!ct.IsCancellationRequested)
                {
                    using var portee = _scopes.CreateScope();
                    var modeles = portee.ServiceProvider.GetRequiredService<IModeleMailRepository>();

                    var faites = await modeles.DeplacerPiecesVersDisqueAsync(Lot, ct);
                    if (faites == 0) break;
                    total += faites;
                }

                if (total > 0) _logger.LogWarning("Pieces de templates sorties de la base vers le disque : {Nombre}.", total);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deplacement des pieces de templates vers le disque interrompu.");
            }
        }
    }
}
