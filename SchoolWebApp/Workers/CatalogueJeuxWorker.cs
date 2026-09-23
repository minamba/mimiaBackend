using SchoolWebApp.Api.Services.Jeux;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// LE CATALOGUE DES JEUX SE RELIT TOUT SEUL — Camara, le 23/09/2026 : « un
    /// worker qui va nourrir le cerveau du professeur à chaque fois qu'un
    /// nouveau jeu est ajouté dans sa matière ».
    ///
    /// IL N'Y A PAS DE CERVEAU À NOURRIR, ET C'EST CE QUI REND LA CHOSE SIMPLE.
    /// Le professeur ne mémorise rien : à chaque séance, son contexte est
    /// composé à partir du catalogue et de la maîtrise actuelle de l'enfant
    /// (voir <c>JeuxService</c>). Nourrir le professeur, c'est donc tenir le
    /// catalogue à jour — et c'est tout ce que fait ce worker.
    ///
    /// LE SITE FAIT FOI. Le front publie <c>/jeux.json</c>, généré depuis son
    /// vrai catalogue ; l'API vient le relire toutes les heures et remplace sa
    /// liste. Publier le front suffit : aucune republication de l'API, aucune
    /// table à remplir. La copie embarquée dans la DLL n'est que l'état de
    /// départ, valable jusqu'à la première relecture.
    ///
    /// CE QUI NE PEUT PAS MAL TOURNER : un site injoignable, un fichier vide ou
    /// illisible laissent l'ancien catalogue en place et une ligne dans le
    /// journal. Le professeur garde ce qu'il savait ; il n'oublie jamais tout
    /// d'un coup parce qu'un déploiement du site est en cours.
    ///
    /// LES CONTEXTES DÉJÀ EN CACHE (une heure, par séance) gardent l'ancienne
    /// liste jusqu'à leur expiration : un jeu publié à 14 h est connu de toute
    /// séance ouverte après 15 h au plus tard.
    /// </summary>
    public class CatalogueJeuxWorker : BackgroundService
    {
        private static readonly TimeSpan Intervalle = TimeSpan.FromHours(1);
        private static readonly TimeSpan PremierTour = TimeSpan.FromMinutes(2);

        private readonly CatalogueJeux _catalogue;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CatalogueJeuxWorker> _logger;

        public CatalogueJeuxWorker(
            CatalogueJeux catalogue,
            IHttpClientFactory http,
            IConfiguration configuration,
            ILogger<CatalogueJeuxWorker> logger)
        {
            _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// L'adresse du catalogue publié : <c>Jeux:UrlCatalogue</c> si elle est
        /// donnée, sinon <c>/jeux.json</c> à l'adresse du site
        /// (<c>Clients:Spa:Url</c>), qui est déjà connue partout où l'API
        /// écrit un lien vers le site.
        /// </summary>
        private string? UrlCatalogue()
        {
            var explicite = _configuration["Jeux:UrlCatalogue"];
            if (!string.IsNullOrWhiteSpace(explicite)) return explicite.Trim();

            var site = _configuration["Clients:Spa:Url"];
            return string.IsNullOrWhiteSpace(site) ? null : $"{site.TrimEnd('/')}/jeux.json";
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var url = UrlCatalogue();
            if (url is null)
            {
                _logger.LogInformation("Pas d'adresse de catalogue des jeux (Jeux:UrlCatalogue ni Clients:Spa:Url) : la copie embarquee reste seule.");
                return;
            }

            // Laisse finir le démarrage.
            try { await Task.Delay(PremierTour, ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await RelireAsync(url, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Le site est peut-être en cours de déploiement : on garde
                    // l'ancien catalogue et on réessaie à l'heure suivante.
                    _logger.LogWarning(ex, "Relecture du catalogue des jeux impossible depuis {Url} : ancien catalogue conserve.", url);
                }

                try { await Task.Delay(Intervalle, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task RelireAsync(string url, CancellationToken ct)
        {
            var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            using var reponse = await client.GetAsync(url, ct);
            reponse.EnsureSuccessStatusCode();

            await using var flux = await reponse.Content.ReadAsStreamAsync(ct);
            var jeux = CatalogueJeux.Lire(flux);

            var (nouveaux, retires) = _catalogue.Remplacer(jeux);

            if (nouveaux.Count == 0 && retires.Count == 0)
            {
                _logger.LogDebug("Catalogue des jeux relu : {Nombre} jeux, sans changement.", _catalogue.Jeux.Count);
                return;
            }

            // C'est cette ligne qui dit « le professeur connaît maintenant ce
            // jeu » — la seule trace de la mise à jour, à lire dans
            // `docker logs mimia`.
            _logger.LogInformation(
                "Catalogue des jeux mis a jour : {Total} jeux. Nouveaux : {Nouveaux}. Retires : {Retires}.",
                _catalogue.Jeux.Count,
                nouveaux.Count == 0 ? "aucun" : string.Join(", ", nouveaux),
                retires.Count == 0 ? "aucun" : string.Join(", ", retires));
        }
    }
}
