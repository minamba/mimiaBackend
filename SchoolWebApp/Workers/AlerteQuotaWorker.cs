using SchoolWebApp.Api.Services;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Envoie les alertes de quota déposées dans la file.
    ///
    /// Un worker plutôt qu'un envoi sur place : le franchissement des 80 % se
    /// détecte au milieu d'un tour de parole, et un aller-retour SMTP ajouterait
    /// plusieurs secondes de silence pour l'enfant à cause d'un mail destiné à
    /// son parent.
    /// </summary>
    public class AlerteQuotaWorker : BackgroundService
    {
        private readonly IFileAlertesQuota _file;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AlerteQuotaWorker> _logger;

        public AlerteQuotaWorker(
            IFileAlertesQuota file,
            IServiceScopeFactory scopes,
            ILogger<AlerteQuotaWorker> logger)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await foreach (var eleveId in _file.LireAsync(ct))
            {
                try
                {
                    // Un scope par alerte : le service dépend du DbContext, qui
                    // est enregistré par requête. Le réutiliser d'une alerte à
                    // l'autre garderait des entités suivies indéfiniment.
                    using var scope = _scopes.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IAlerteQuotaService>();

                    await service.PrevenirAsync(eleveId, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Une alerte perdue ne doit pas arrêter le worker : les
                    // suivantes concernent d'autres familles.
                    _logger.LogError(ex,
                        "Echec de l'alerte de quota pour l'eleve {EleveId}.", eleveId);
                }
            }
        }
    }
}
