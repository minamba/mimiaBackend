using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Prompts;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// GARDE LE NOYAU CHAUD DANS LE CACHE D'ANTHROPIC — voulu par Camara le
    /// 19/09/2026, après la mesure des ouvertures de séance.
    ///
    /// LE PROBLÈME QU'IL RÈGLE. Le noyau — ~45 000 jetons, identique pour tous
    /// les élèves et toutes les matières — est mis en cache une heure. Toute
    /// séance ouverte alors qu'il est chaud le relit à un dixième du prix ;
    /// ouverte alors qu'il a refroidi, elle le RÉÉCRIT au double du prix :
    /// 0,27 $ par ouverture, la moitié du coût d'une ouverture (0,54 $). En
    /// séances de trente minutes, c'était jusqu'à 1 $ de l'heure — près d'un
    /// tiers du coût d'une heure de cours.
    ///
    /// À PLEIN RÉGIME, LE PROBLÈME N'EXISTE PAS : avec des dizaines de familles,
    /// une séance ouvre toujours dans l'heure qui suit la précédente et le
    /// noyau ne refroidit jamais. Au lancement, il refroidit entre deux
    /// séances. Ce réchauffeur comble l'écart : un appel minuscule toutes les
    /// cinquante minutes, qui relit le noyau — une lecture de cache prolonge sa
    /// vie d'une heure — pour 0,0135 $. Quinze appels par jour : 0,20 $ par
    /// jour, 6 $ par mois. Rentable dès UNE ouverture à froid évitée par jour.
    ///
    /// L'APPEL DOIT COMMENCER EXACTEMENT COMME UN TOUR DE COURS. Le cache est un
    /// préfixe au caractère près : même modèle, même premier bloc système, même
    /// marque de cache. Le bloc est construit ici comme dans
    /// <see cref="AgentPedagogiqueService"/> — si l'un change, l'autre doit
    /// suivre, sinon ce réchauffeur chaufferait un cache que personne ne lit.
    ///
    /// IL SE TAIT QUAND LE PRODUIT TRAVAILLE : un tour de cours dans les
    /// cinquante dernières minutes a déjà prolongé le cache. Et il ne tourne
    /// que de 7 h à 23 h, heure de Paris : personne n'ouvre de séance à 3 h.
    ///
    /// Chaque appel est journalisé sous « rechauffeur-cache », visible dans les
    /// frais de fond de l'administration : une dépense de fond ne doit jamais
    /// être silencieuse.
    /// </summary>
    public class RechauffeurCacheWorker : BackgroundService
    {
        private static readonly TimeSpan Verification = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Sous l'heure de vie du cache, avec dix minutes de marge : un appel qui
        /// arriverait à l'heure pile trouverait le cache déjà froid.
        /// </summary>
        private static readonly TimeSpan Fraicheur = TimeSpan.FromMinutes(50);

        private readonly IServiceScopeFactory _scopes;
        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RechauffeurCacheWorker> _logger;

        private DateTime _dernierAppelUtc = DateTime.MinValue;

        public RechauffeurCacheWorker(
            IServiceScopeFactory scopes,
            AnthropicClient client,
            IOptions<OptionsClaude> options,
            IJournalClaudeRepository journal,
            IConfiguration configuration,
            ILogger<RechauffeurCacheWorker> logger)
        {
            _scopes = scopes;
            _client = client;
            _options = options.Value;
            _journal = journal;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Claude:Rechauffeur:Actif", true))
            {
                _logger.LogInformation("Rechauffeur de cache desactive par configuration.");
                return;
            }

            var heureDebut = _configuration.GetValue("Claude:Rechauffeur:HeureDebut", 7);
            var heureFin = _configuration.GetValue("Claude:Rechauffeur:HeureFin", 23);

            // Une minute après le démarrage : le temps que tout soit en place,
            // et surtout pour ne pas réchauffer pendant qu'on redéploie en
            // boucle — chaque redémarrage en développement aurait payé un appel.
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var heure = HeureFrance.Locale(DateTime.UtcNow).Hour;

                    if (heure >= heureDebut && heure < heureFin && await ADroitDeChaufferAsync(stoppingToken))
                    {
                        await ChaufferAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Un appel raté ne coûte qu'une ouverture à froid : on
                    // réessaie au tour suivant, sans faire tomber le service.
                    _logger.LogWarning(ex, "Rechauffage du cache impossible ; nouvel essai dans {Minutes} min.", Verification.TotalMinutes);
                }

                try { await Task.Delay(Verification, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }

        /// <summary>
        /// Rien à faire si le cache a été touché — par nous ou par un vrai tour de
        /// cours — dans les cinquante dernières minutes.
        /// </summary>
        private async Task<bool> ADroitDeChaufferAsync(CancellationToken ct)
        {
            var limite = DateTime.UtcNow - Fraicheur;
            if (_dernierAppelUtc > limite) return false;

            using var portee = _scopes.CreateScope();
            var db = portee.ServiceProvider.GetRequiredService<SchoolWebAppDatabaseContext>();

            // `> 0` : un tour du professeur qui a vraiment appelé le modèle,
            // donc relu le noyau. Un message sans jetons n'a rien chauffé.
            var dernierTour = await db.Messages
                .AsNoTracking()
                .Where(m => m.Role == "assistant" && m.TokensEntree > 0)
                .MaxAsync(m => (DateTime?)m.DateCreation, ct);

            return dernierTour is null || dernierTour < limite;
        }

        private async Task ChaufferAsync(CancellationToken ct)
        {
            var modele = _options.Modele(TypeTache.Dialogue);

            var parametres = new MessageCreateParams
            {
                Model = modele,

                // Assez pour une réponse d'un mot ; le prix est dans la lecture
                // du noyau, pas ici.
                MaxTokens = 16,

                // LE MÊME EFFORT QUE LE COURS — corrigé le 19/09/2026, mesure à
                // l'appui. Sans lui, le réchauffeur partait à l'effort par défaut
                // (élevé) quand le cours part en « medium » : six passages ont
                // relu leur propre noyau en cache, et la séance de maths qui a
                // suivi, deux minutes après le dernier, n'en a relu AUCUN jeton
                // — ouverture à froid, 0,26 $. L'effort est la seule différence de
                // requête entre les deux : c'est le suspect, pas encore une
                // certitude. À VÉRIFIER : la première séance qui suit un passage
                // du réchauffeur doit relire ~50 000 jetons dès son premier tour.
                OutputConfig = new OutputConfig
                {
                    Effort = AgentPedagogiqueService.ConvertirEffort(_options.Effort(TypeTache.Dialogue)),
                },

                // LE MÊME PREMIER BLOC QUE LE COURS, au caractère près, avec la
                // même marque de cache d'une heure — voir `ConstruireSystemAsync`.
                System = new List<TextBlockParam>
                {
                    new()
                    {
                        Text = PromptsPedagogiques.Noyau,
                        CacheControl = new CacheControlEphemeral { Ttl = "1h" },
                    },
                },

                Messages =
                [
                    new MessageParam
                    {
                        Role = Role.User,
                        Content = "[Vérification technique : réponds « ok », rien d'autre.]",
                    },
                ],
            };

            var reponse = await _client.Messages.Create(parametres, cancellationToken: ct);
            _dernierAppelUtc = DateTime.UtcNow;

            var lu = reponse.Usage.CacheReadInputTokens ?? 0;
            var ecrit = reponse.Usage.CacheCreationInputTokens ?? 0;

            await _journal.EnregistrerAsync(
                "rechauffeur-cache", reponse.Model, reponse.Usage.InputTokens,
                reponse.Usage.OutputTokens, lu, ecrit, ct: ct);

            // « Écrit » veut dire que le noyau ÉTAIT froid : l'appel a payé
            // l'écriture, mais la prochaine séance, elle, ne la paiera pas.
            _logger.LogInformation(
                "Noyau rechauffe ({Modele}) : {Lu} jetons relus, {Ecrit} ecrits.",
                reponse.Model, lu, ecrit);
        }
    }
}
