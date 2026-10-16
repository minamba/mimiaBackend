using SchoolWebApp.Api.Services.Orientation;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// LA TÂCHE PROGRAMMÉE DE LA CONSEILLÈRE D'ORIENTATION — Camara, le
    /// 11/10/2026 : « une tâche programmée qui ira vérifier les changements et
    /// mettra à jour les connaissances de la conseillère ».
    ///
    /// UNE FOIS PAR SEMAINE, MÊME APRÈS UN REDÉMARRAGE. Elle se réveille toutes
    /// les six heures et regarde le journal : si le dernier passage a plus de
    /// sept jours, elle en lance un. Un compte à rebours de sept jours en mémoire
    /// repartirait de zéro à chaque déploiement, et n'aurait peut-être jamais
    /// lieu.
    ///
    /// Le passage lui-même ne télécharge que si les sources ont changé — voir
    /// <see cref="ImportOrientationService"/>. Il ne coûte aucun jeton.
    ///
    /// ELLE OBÉIT À « TÂCHES DE FOND » (Admin › Modes), comme les autres.
    /// </summary>
    public class ImportOrientationWorker : BackgroundService
    {
        private static readonly TimeSpan Reveil = TimeSpan.FromHours(6);
        private static readonly TimeSpan Rythme = TimeSpan.FromDays(7);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ImportOrientationWorker> _logger;

        public ImportOrientationWorker(IServiceScopeFactory scopes, ILogger<ImportOrientationWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Laisse l'application finir de démarrer : migrations et semis.
            try { await Task.Delay(TimeSpan.FromMinutes(5), ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await PasserSiDuAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // L'offre en base reste valable : un échec reporte la mise à jour.
                    _logger.LogError(ex, "Tache d'import de l'offre des lycees en echec.");
                }

                try { await Task.Delay(Reveil, ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        private async Task PasserSiDuAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();

            var reglages = scope.ServiceProvider.GetRequiredService<IReglageRepository>();
            if (!await reglages.EstActifAsync(Controllers.ReglagesController.TachesDeFond, parDefaut: true, ct)) return;

            var depot = scope.ServiceProvider.GetRequiredService<IOrientationRepository>();
            var passages = (await depot.DerniersPassagesAsync(20, ct)).ToList();
            var maintenant = DateTime.UtcNow;

            // LE RYTHME SE COMPTE DEPUIS LE DERNIER PASSAGE RÉUSSI, PAS DEPUIS LE
            // DERNIER ESSAI — relevé le 11/10/2026 : deux imports ratés à midi
            // (429) comptaient comme « fait cette semaine », la base est restée
            // VIDE, et la conseillère ne connaissait aucun lycée. Désormais :
            //   - à jour (réussi il y a moins de 7 jours, et des lycées en base) :
            //     rien à faire ;
            //   - sinon, on réessaie à chaque réveil (6 h), y compris après un échec.
            var dernierReussi = passages.FirstOrDefault(p => p.Reussi && p.Lycees > 0);
            if (dernierReussi is not null && maintenant - dernierReussi.Date < Rythme) return;

            var import = scope.ServiceProvider.GetRequiredService<IImportOrientationService>();
            var passage = await import.ImporterAsync(forcer: false, ct);

            _logger.LogInformation("Import hebdomadaire de l'offre des lycees : {Message}", passage.Message);
        }
    }
}
