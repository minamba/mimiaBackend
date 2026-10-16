using SchoolWebApp.Api.Services.Voix;

namespace SchoolWebApp.Api.Services.Fournisseurs
{
    /// <summary>
    /// QUAND GOOGLE DIT « PLUS AUJOURD'HUI », ON LE SAIT TOUT DE SUITE — Camara, le
    /// 28/09/2026 : « les refus ne me sont pas signalés sur Telegram ».
    ///
    /// Au premier refus pour quota du jour (<see cref="CompteurGemini.JourPlein"/>),
    /// on relance la surveillance des fournisseurs sans attendre son prochain
    /// passage : la carte passe à « Quota atteint », et l'alerte Telegram part par
    /// le même chemin que toutes les autres — une fois, puis « fonctionne de
    /// nouveau » à la remise à zéro.
    ///
    /// DE MÊME POUR LE CRÉDIT (13/10/2026) : au premier refus faute de crédit
    /// prépayé, la carte passe au rouge (Telegram + courriel) ; à la première
    /// phrase acceptée après la recharge, « fonctionne de nouveau ».
    /// </summary>
    public sealed class AlerteQuotaGemini : IHostedService
    {
        private readonly CompteurGemini _compteur;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AlerteQuotaGemini> _logger;

        public AlerteQuotaGemini(CompteurGemini compteur, IServiceScopeFactory scopes, ILogger<AlerteQuotaGemini> logger)
        {
            _compteur = compteur ?? throw new ArgumentNullException(nameof(compteur));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _compteur.JourPlein += Verifier;
            _compteur.CreditChange += Verifier;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _compteur.JourPlein -= Verifier;
            _compteur.CreditChange -= Verifier;
            return Task.CompletedTask;
        }

        private void Verifier()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var surveillance = scope.ServiceProvider.GetRequiredService<SurveillanceFournisseurs>();
                    await surveillance.VerifierAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Quota ou credit Gemini change, mais la verification immediate a echoue.");
                }
            });
        }
    }
}
