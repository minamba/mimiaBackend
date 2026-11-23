using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// LES DOCUMENTS D'UN EXAMEN BLANC S'EFFACENT DEUX MOIS APRÈS LUI — Camara, le 23/11/2026. Une fois par jour :
    /// chaque fichier dont l'examen blanc date de plus de deux mois est effacé du disque, et sa ligne marquée
    /// (l'élève voit « Effacé deux mois après l'examen blanc » au lieu d'un aperçu vide). Puis les dossiers
    /// orphelins : un examen blanc supprimé avec son élève (cascade en base) laisse ses fichiers sur le disque.
    /// Même schéma que <see cref="PurgeCoursControlesWorker"/>.
    /// </summary>
    public class PurgeExamensBlancsWorker : BackgroundService
    {
        private const int Lot = 200;

        private readonly IServiceScopeFactory _scopes;
        private readonly IStockageExamensBlancs _stockage;
        private readonly ILogger<PurgeExamensBlancsWorker> _logger;

        public PurgeExamensBlancsWorker(
            IServiceScopeFactory scopes, IStockageExamensBlancs stockage, ILogger<PurgeExamensBlancsWorker> logger)
        {
            _scopes = scopes;
            _stockage = stockage;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                // Après le démarrage, et décalé de la purge des contrôles (7 min).
                await Task.Delay(TimeSpan.FromMinutes(11), ct);

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
                if (!_stockage.Pret) return;

                using var portee = _scopes.CreateScope();
                var examens = portee.ServiceProvider.GetRequiredService<IExamenBlancRepository>();

                await PurgerExpiresAsync(examens, ct);
                await PurgerOrphelinsAsync(examens, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Purge des examens blancs interrompue.");
            }
        }

        private async Task PurgerExpiresAsync(IExamenBlancRepository examens, CancellationToken ct)
        {
            var maintenant = DateTime.UtcNow;
            var total = 0;

            for (var tour = 0; tour < 50; tour += 1)
            {
                var expires = await examens.GetFichiersAPurgerAsync(maintenant, Lot, ct);
                if (expires.Count == 0) break;

                foreach (var (_, _, document) in expires)
                {
                    _stockage.Supprimer(document.Chemin);
                    await examens.MarquerFichierEffaceAsync(document.Id, maintenant, ct);
                }

                total += expires.Count;
                if (expires.Count < Lot) break;
            }

            if (total > 0)
            {
                _logger.LogInformation("Purge des examens blancs : {Total} fichier(s) efface(s), deux mois apres l'examen.", total);
            }
        }

        private async Task PurgerOrphelinsAsync(IExamenBlancRepository examens, CancellationToken ct)
        {
            var dossiers = _stockage.DossiersAnciens();
            if (dossiers.Count == 0) return;

            var vivants = await examens.ExistantsAsync(dossiers.Select(d => d.ControleId), ct);

            foreach (var (eleveId, examenId) in dossiers.Where(d => !vivants.Contains(d.ControleId)))
            {
                _stockage.SupprimerDossier(eleveId, examenId);
                _logger.LogWarning("Dossier orphelin de l'examen blanc {ExamenId} (eleve {EleveId}) efface.", examenId, eleveId);
            }
        }
    }
}
