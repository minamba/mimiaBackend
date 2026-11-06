using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.Voix;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// UN MOT JAMAIS VU SE TESTE UNE FOIS, EN ARRIÈRE-PLAN — Camara, le 04/11/2026. Quatre voix de
    /// synthèse OpenAI le lisent, le vrai jugement les écoute, le résultat reste en base. Jamais la
    /// voix d'un enfant : la synthèse ne reçoit que le mot écrit. Mesuré contre le premier banc :
    /// même conclusion sur 30 sons sur 39, et « nain » reste à 0 sur 4. Une panne ne garde rien :
    /// le mot reste non jugé, et se retestera à sa prochaine apparition.
    /// </summary>
    public sealed class TestDesSonsWorker : BackgroundService
    {
        private const string Endpoint = "https://api.openai.com/v1/audio/speech";

        private const string Consigne =
            "Lis uniquement ce mot ou cette syllabe, en français de France, distinctement, sans rien ajouter.";

        /// <summary>Un plafond de dépense : un mot revient le lendemain s'il réapparaît.</summary>
        private const int MotsParJour = 300;

        private static readonly string[] Voix = ["coral", "sage", "ash", "cedar"];

        private readonly FiabiliteDesSons _fiabilite;
        private readonly IServiceScopeFactory _scopes;
        private readonly IHttpClientFactory _http;
        private readonly OptionsVoix _options;
        private readonly ILogger<TestDesSonsWorker> _logger;

        public TestDesSonsWorker(
            FiabiliteDesSons fiabilite,
            IServiceScopeFactory scopes,
            IHttpClientFactory http,
            IOptions<OptionsVoix> options,
            ILogger<TestDesSonsWorker> logger)
        {
            _fiabilite = fiabilite;
            _scopes = scopes;
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var jour = DateOnly.FromDateTime(DateTime.UtcNow);
            var testes = 0;

            try
            {
                await foreach (var mot in _fiabilite.Demandes.ReadAllAsync(ct))
                {
                    var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
                    if (aujourdhui != jour)
                    {
                        jour = aujourdhui;
                        testes = 0;
                    }

                    try
                    {
                        if (testes < MotsParJour)
                        {
                            testes += 1;
                            await TesterAsync(mot, ct);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
                    {
                        _logger.LogWarning("Test d'un son impossible : {Cause}.", ex.GetType().Name);
                    }
                    finally
                    {
                        _fiabilite.Terminer(mot);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
        }

        private async Task TesterAsync(string mot, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey)) return;

            using var scope = _scopes.CreateScope();
            var verification = scope.ServiceProvider.GetRequiredService<IVerificationSonService>();
            var justes = 0;

            foreach (var voix in Voix)
            {
                var pcm = await LireAsync(mot, voix, ct);
                var verdict = await verification.JugerAsync(mot, pcm, ct);

                // Une panne ne dit rien du mot : rien n'est gardé, il se retestera.
                if (verdict is null) return;
                if (verdict.Juste) justes += 1;
            }

            await scope.ServiceProvider.GetRequiredService<IMotsVerifiesRepository>()
                .EnregistrerAsync(mot, justes, Voix.Length, ct);

            _logger.LogInformation("Son teste : {Mot}, {Justes}/{Total}.", mot, justes, Voix.Length);
        }

        private async Task<byte[]> LireAsync(string mot, string voix, CancellationToken ct)
        {
            using var requete = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new
                {
                    model = _options.Modele,
                    voice = voix,
                    input = mot,
                    instructions = Consigne,
                    response_format = "pcm",
                }),
            };
            requete.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var client = _http.CreateClient(nameof(TestDesSonsWorker));
            using var reponse = await client.SendAsync(requete, ct);
            reponse.EnsureSuccessStatusCode();

            return await reponse.Content.ReadAsByteArrayAsync(ct);
        }
    }
}
