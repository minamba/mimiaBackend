using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// LA PURGE DES COURS DE CONTRÔLE — Camara, le 04/11/2026 : « les documents ajoutés par
    /// l'enfant seront physiquement supprimés du dossier une semaine après le contrôle ; la
    /// consultation ne sera plus possible après ». Une purge PERSONNALISÉE : chaque fichier
    /// part selon SON contrôle et SA date d'ajout, jamais selon l'âge du dossier — deux
    /// enfants peuvent avoir des documents du même jour pour des contrôles à un mois d'écart.
    ///
    /// DEUX BALAYAGES, UNE FOIS PAR JOUR :
    ///   1. les fichiers dont le contrôle ET l'ajout datent de plus d'une semaine : effacés
    ///      du disque, et la ligne le note (la transcription reste pour le professeur) ;
    ///   2. les dossiers orphelins : un contrôle supprimé, allégé ou emporté avec le compte
    ///      part en cascade dans la base sans toucher au disque.
    /// </summary>
    public class PurgeCoursControlesWorker : BackgroundService
    {
        /// <summary>Une semaine après le contrôle (et après l'ajout).</summary>
        public static readonly TimeSpan Delai = TimeSpan.FromDays(7);

        private const int Lot = 200;

        private readonly IServiceScopeFactory _scopes;
        private readonly IStockageCoursControles _stockage;
        private readonly ILogger<PurgeCoursControlesWorker> _logger;

        public PurgeCoursControlesWorker(
            IServiceScopeFactory scopes, IStockageCoursControles stockage, ILogger<PurgeCoursControlesWorker> logger)
        {
            _scopes = scopes;
            _stockage = stockage;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(7), ct);

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
                    _logger.LogWarning("Stockage des cours de controle NON PRET (marque STOCKAGE-MIMIA absente) : aucun document possible.");
                    return;
                }

                using var portee = _scopes.CreateScope();
                var controles = portee.ServiceProvider.GetRequiredService<IControleScolaireRepository>();

                await PurgerExpiresAsync(controles, ct);
                await PurgerOrphelinsAsync(controles, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Purge des cours de controle interrompue.");
            }
        }

        private async Task PurgerExpiresAsync(IControleScolaireRepository controles, CancellationToken ct)
        {
            var maintenant = DateTime.UtcNow;
            var limite = maintenant - Delai;
            var total = 0;

            // Par lots, jusqu'à épuisement : chaque lot est marqué avant le suivant, la
            // boucle s'arrête donc même si un fichier manque déjà sur le disque.
            for (var tour = 0; tour < 50; tour += 1)
            {
                var expires = await controles.GetFichiersAPurgerAsync(limite, Lot, ct);
                if (expires.Count == 0) break;

                foreach (var document in expires) _stockage.Supprimer(document.Chemin);

                await controles.MarquerFichiersEffacesAsync(expires.Select(d => d.Id).ToList(), maintenant, ct);
                total += expires.Count;

                if (expires.Count < Lot) break;
            }

            if (total > 0) _logger.LogInformation("Purge des cours de controle : {Total} fichier(s) efface(s), une semaine apres le controle.", total);
        }

        private async Task PurgerOrphelinsAsync(IControleScolaireRepository controles, CancellationToken ct)
        {
            var dossiers = _stockage.DossiersAnciens();
            if (dossiers.Count == 0) return;

            var vivants = await controles.ControlesAvecFichiersAsync(dossiers.Select(d => d.ControleId).Distinct().ToList(), ct);

            foreach (var (eleveId, controleId) in dossiers.Where(d => !vivants.Contains(d.ControleId)))
            {
                _stockage.SupprimerDossier(eleveId, controleId);
                _logger.LogWarning("Dossier orphelin du cours de controle {ControleId} (eleve {EleveId}) efface.", controleId, eleveId);
            }
        }
    }
}
