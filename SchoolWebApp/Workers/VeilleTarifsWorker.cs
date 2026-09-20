using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Services.Tarifs;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// LA VEILLE DES TARIFS D'UN FOURNISSEUR D'IA — voulue par Camara le
    /// 19/09/2026 : « un worker qui va lire les tarifs afin de les mettre à jour
    /// dans l'application, pour que nos calculs soient toujours bons ».
    ///
    /// LE JOUR MÊME, le coût affiché comptait Sonnet 5 à 3 $ / 15 $ quand la
    /// facture disait 2 $ / 10 $ : la hausse annoncée pour le 1er septembre avait
    /// été annulée, et personne ne l'avait su.
    ///
    /// UNE FOIS PAR JOUR, elle lit la page de tarifs publique du fournisseur (voir
    /// <see cref="LecteurGrilleTarifs"/>) et compare avec la grille en base.
    ///   - PRIX CHANGÉ : il est enregistré ET appliqué à nos calculs, la date de
    ///     mise à jour passe à aujourd'hui, et un message part sur Telegram.
    ///   - PRIX DOUTEUX — plus de cinq fois plus haut ou plus bas que l'ancien :
    ///     il n'est PAS appliqué. Une page mal lue ne doit jamais diviser nos
    ///     coûts par dix en silence ; le message demande de vérifier.
    ///   - MODÈLE INTROUVABLE ou page inaccessible : rien ne change, et un message
    ///     le dit — une seule fois, tant que le problème dure.
    /// </summary>
    public abstract class VeilleTarifsWorker : BackgroundService
    {
        /// <summary>Au-delà de ce facteur, un nouveau prix est jugé douteux.</summary>
        private const decimal FacteurDouteux = 5m;

        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        private readonly IServiceScopeFactory _scopes;
        private readonly IHttpClientFactory _http;
        private readonly ITelegramService _telegram;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        /// <summary>Un problème déjà signalé ne se re-signale pas chaque jour.</summary>
        private bool _problemeSignale;

        protected VeilleTarifsWorker(
            IServiceScopeFactory scopes, IHttpClientFactory http, ITelegramService telegram,
            IConfiguration configuration, ILogger logger)
        {
            _scopes = scopes;
            _http = http;
            _telegram = telegram;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary><c>anthropic</c> ou <c>openai</c>, comme dans la grille.</summary>
        protected abstract string Fournisseur { get; }

        /// <summary>Le nom affiché dans les messages.</summary>
        protected abstract string Libelle { get; }

        /// <summary>La page de tarifs, en markdown.</summary>
        protected abstract string Adresse { get; }

        /// <summary>Lit les prix d'un modèle dans la page, ou null s'il n'y est pas.</summary>
        protected abstract PrixLus? Lire(string page, string modele);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Tarifs:Veille:Actif", true))
            {
                _logger.LogInformation("Veille des tarifs {Fournisseur} desactivee par configuration.", Libelle);
                return;
            }

            var intervalle = TimeSpan.FromHours(_configuration.GetValue("Tarifs:Veille:IntervalleHeures", 24));

            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerifierAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Veille des tarifs {Fournisseur} en echec.", Libelle);
                    await SignalerAsync($"La vérification a échoué : {ex.Message}");
                }

                try { await Task.Delay(intervalle, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }

        private async Task VerifierAsync(CancellationToken ct)
        {
            var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MimiaVeilleTarifs/1.0");

            using var reponse = await client.GetAsync(Adresse, ct);
            if (!reponse.IsSuccessStatusCode)
            {
                await SignalerAsync($"La page des tarifs ne répond pas (HTTP {(int)reponse.StatusCode}) : {Adresse}");
                return;
            }

            var page = await reponse.Content.ReadAsStringAsync(ct);

            using var portee = _scopes.CreateScope();
            var db = portee.ServiceProvider.GetRequiredService<SchoolWebAppDatabaseContext>();

            var grille = await db.TarifsFournisseurs
                .Where(t => t.Fournisseur == Fournisseur)
                .ToListAsync(ct);

            var maintenant = DateTime.UtcNow;
            var problemes = new List<string>();
            var changements = new List<(string Modele, List<string> Lignes)>();

            foreach (var tarif in grille)
            {
                var lus = Lire(page, tarif.Modele);
                if (lus is null)
                {
                    problemes.Add($"{tarif.Modele} introuvable sur la page — sa mise en page a peut-être changé.");
                    continue;
                }

                var comparaisons = new[]
                {
                    (Nom: "Entrée", Ancien: tarif.PrixEntree, Nouveau: lus.Entree, Unite: "le million de jetons"),
                    (Nom: "Sortie", Ancien: tarif.PrixSortie, Nouveau: lus.Sortie, Unite: "le million de jetons"),
                    (Nom: "Minute", Ancien: tarif.PrixMinute, Nouveau: lus.Minute, Unite: "la minute"),
                };

                // Un champ que la page ne donne pas n'est pas un changement : on
                // ne compare que ce qui a été lu.
                var douteux = comparaisons.FirstOrDefault(c =>
                    c.Nouveau is { } n && c.Ancien is { } a && a > 0
                    && (n > a * FacteurDouteux || n < a / FacteurDouteux));

                if (douteux.Nom is not null)
                {
                    problemes.Add(
                        $"{tarif.Modele} : {douteux.Nom.ToLowerInvariant()} lue à {Dollars(douteux.Nouveau)} au lieu de "
                        + $"{Dollars(douteux.Ancien)} — écart trop grand, NON appliqué.");
                    continue;
                }

                var lignes = comparaisons
                    .Where(c => c.Nouveau is not null && c.Nouveau != c.Ancien)
                    .Select(c => $"{c.Nom} : {Dollars(c.Ancien)} → {Dollars(c.Nouveau)} {c.Unite}")
                    .ToList();

                tarif.DerniereVerification = maintenant;

                if (lignes.Count == 0) continue;

                // Nos calculs suivent aussitôt — sauf une ligne qui n'entre dans
                // aucun calcul (la transcription) : elle le reste.
                var compte = tarif.PrixEntreeApplique is not null || tarif.PrixSortieApplique is not null
                             || tarif.PrixMinuteApplique is not null;

                tarif.PrixEntree = lus.Entree ?? tarif.PrixEntree;
                tarif.PrixSortie = lus.Sortie ?? tarif.PrixSortie;
                tarif.PrixMinute = lus.Minute ?? tarif.PrixMinute;

                if (compte)
                {
                    tarif.PrixEntreeApplique = tarif.PrixEntree;
                    tarif.PrixSortieApplique = tarif.PrixSortie;
                    tarif.PrixMinuteApplique = tarif.PrixMinute;
                }

                tarif.DateMiseAJour = maintenant;
                changements.Add((tarif.Modele, lignes));
            }

            await db.SaveChangesAsync(ct);

            foreach (var (modele, lignes) in changements)
            {
                _logger.LogInformation("Tarif {Fournisseur} {Modele} mis a jour : {Lignes}",
                    Libelle, modele, string.Join(" ; ", lignes));
                await _telegram.NotifierTarifAsync(Libelle, modele, lignes);
            }

            if (problemes.Count > 0)
            {
                await SignalerAsync(string.Join(" ", problemes));
            }
            else
            {
                _problemeSignale = false;
            }
        }

        /// <summary>Un message par problème, pas un par jour.</summary>
        private async Task SignalerAsync(string probleme)
        {
            _logger.LogWarning("Veille des tarifs {Fournisseur} : {Probleme}", Libelle, probleme);

            if (_problemeSignale) return;
            _problemeSignale = true;

            await _telegram.NotifierVeilleTarifsAsync(Libelle, probleme);
        }

        private static string Dollars(decimal? prix) =>
            prix is null ? "—" : $"{prix.Value.ToString("0.#####", Fr)} $";
    }

    /// <summary>La veille des tarifs d'Anthropic — voir <see cref="VeilleTarifsWorker"/>.</summary>
    public sealed class VeilleTarifsAnthropicWorker : VeilleTarifsWorker
    {
        public VeilleTarifsAnthropicWorker(
            IServiceScopeFactory scopes, IHttpClientFactory http, ITelegramService telegram,
            IConfiguration configuration, ILogger<VeilleTarifsAnthropicWorker> logger)
            : base(scopes, http, telegram, configuration, logger) { }

        protected override string Fournisseur => "anthropic";

        protected override string Libelle => "Anthropic";

        protected override string Adresse => "https://docs.claude.com/en/docs/about-claude/pricing.md";

        protected override PrixLus? Lire(string page, string modele) =>
            LecteurGrilleTarifs.LireAnthropic(page, modele);
    }

    /// <summary>La veille des tarifs d'OpenAI — voir <see cref="VeilleTarifsWorker"/>.</summary>
    public sealed class VeilleTarifsOpenAiWorker : VeilleTarifsWorker
    {
        public VeilleTarifsOpenAiWorker(
            IServiceScopeFactory scopes, IHttpClientFactory http, ITelegramService telegram,
            IConfiguration configuration, ILogger<VeilleTarifsOpenAiWorker> logger)
            : base(scopes, http, telegram, configuration, logger) { }

        protected override string Fournisseur => "openai";

        protected override string Libelle => "OpenAI";

        protected override string Adresse => "https://developers.openai.com/api/docs/pricing.md";

        protected override PrixLus? Lire(string page, string modele) =>
            LecteurGrilleTarifs.LireOpenAi(page, modele);
    }
}
