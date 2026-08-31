using Microsoft.EntityFrameworkCore;
using SchoolWebApp.IdentityServer.Data;

namespace SchoolWebApp.IdentityServer.Workers
{
    /// <summary>
    /// Confirme d'office les comptes créés AVANT que la confirmation d'adresse
    /// ne devienne obligatoire.
    ///
    /// LE PROBLÈME QU'IL RÉSOUT
    /// -----------------------
    /// Activer `RequireConfirmedEmail` ferme la porte à tous les comptes dont
    /// l'adresse n'a jamais été confirmée — c'est-à-dire TOUS ceux qui
    /// existaient avant. Ils se sont inscrits sous une règle qui ne le
    /// demandait pas ; les enfermer dehors du jour au lendemain, sans qu'ils
    /// aient rien fait, serait leur faire payer notre changement d'avis.
    ///
    /// POURQUOI UNE DATE LIMITE EN DUR
    /// -----------------------------
    /// Sans elle, ce travail confirmerait aussi les inscriptions du jour même,
    /// et la règle ne s'appliquerait jamais à personne. La date sépare les deux
    /// mondes : avant, on répare ; après, on demande. Elle est figée dans le
    /// code plutôt que configurée, parce qu'elle ne se règle pas — c'est un
    /// fait historique, celui du jour où la règle a changé.
    ///
    /// Il s'exécute à chaque démarrage sans rien casser : au second passage il
    /// ne trouve plus personne à confirmer, et ne fait rien.
    /// </summary>
    public class ConfirmationRetroactiveWorker : IHostedService
    {
        /// <summary>
        /// Le jour où la confirmation est devenue obligatoire. Les comptes
        /// antérieurs sont réputés confirmés ; les suivants doivent cliquer.
        /// </summary>
        private static readonly DateTime Bascule = new(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

        private readonly IServiceProvider _services;
        private readonly ILogger<ConfirmationRetroactiveWorker> _logger;

        public ConfirmationRetroactiveWorker(
            IServiceProvider services, ILogger<ConfirmationRetroactiveWorker> logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var repares = await db.Users
                    .Where(u => !u.EmailConfirmed && u.DateCreation < Bascule)
                    .ExecuteUpdateAsync(
                        m => m.SetProperty(u => u.EmailConfirmed, true),
                        cancellationToken);

                if (repares > 0)
                {
                    _logger.LogWarning(
                        "{Nombre} compte(s) anterieur(s) au {Date:d} confirme(s) d'office : "
                        + "ils existaient avant que la confirmation ne soit exigee.",
                        repares, Bascule);
                }
            }
            catch (Exception ex)
            {
                // Une base pas encore migrée au premier démarrage, par exemple.
                // Ce rattrapage n'est pas assez important pour empêcher le
                // serveur d'identité de se lancer.
                _logger.LogError(ex, "Confirmation retroactive impossible.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
