using SchoolWebApp.Api.Services.Fournisseurs;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Vérifie Anthropic et OpenAI à intervalle régulier, et prévient quand
    /// l'un des deux se met à refuser.
    ///
    /// NÉ D'UNE PANNE DU 14/09/2026. Après un redémarrage, le professeur ne
    /// parlait plus et le micro ne captait plus rien. On a d'abord accusé le
    /// code ; c'était le crédit OpenAI, épuisé. Aucune des deux API ne donne
    /// le solde restant avec une clé ordinaire — la seule façon de le savoir
    /// sans ouvrir leurs sites est d'essayer un vrai appel payant, minuscule.
    /// C'est ce que fait ce worker, pour que la panne se découvre par un
    /// courriel et non par un enfant devant un professeur muet.
    /// </summary>
    public class SurveillanceFournisseursWorker : BackgroundService
    {
        /// <summary>
        /// Toutes les trente minutes quand tout va bien : une quarantaine
        /// d'appels par jour, quelques centimes par mois en tout.
        /// </summary>
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Toutes les cinq minutes tant qu'un fournisseur est en défaut, rouge
        /// ou orange : un appel refusé ne coûte rien, et le retour à la normale
        /// — après une recharge, ou la fin d'une panne chez eux — s'annonce
        /// alors presque tout de suite sur Telegram.
        /// </summary>
        private static readonly TimeSpan IntervalleBloque = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopes;
        private readonly EtatFournisseurs _etats;
        private readonly ILogger<SurveillanceFournisseursWorker> _logger;

        public SurveillanceFournisseursWorker(
            IServiceScopeFactory scopes,
            EtatFournisseurs etats,
            ILogger<SurveillanceFournisseursWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _etats = etats ?? throw new ArgumentNullException(nameof(etats));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Une minute : le temps que les migrations passent. Pas davantage —
            // un redémarrage sur un crédit épuisé est précisément le cas qu'on
            // veut voir tout de suite.
            try { await Task.Delay(TimeSpan.FromMinutes(1), ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Un scope par passage : la surveillance envoie des
                    // courriels, et le service de courriel est Scoped.
                    using var scope = _scopes.CreateScope();
                    var surveillance = scope.ServiceProvider.GetRequiredService<SurveillanceFournisseurs>();

                    await surveillance.VerifierAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec de la verification des fournisseurs d'IA.");
                }

                var enDefaut = _etats.Tous().Any(e => StatutFournisseur.EnDefaut(e.Statut));

                try { await Task.Delay(enDefaut ? IntervalleBloque : Intervalle, ct); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
