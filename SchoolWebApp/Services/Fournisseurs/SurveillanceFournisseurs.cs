using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Services.Voix;
using SchoolWebApp.Domain.Emails;

namespace SchoolWebApp.Api.Services.Fournisseurs
{
    /// <summary>
    /// Essaie un appel minuscule chez Anthropic et chez OpenAI, range ce
    /// qu'il a constaté, et écrit aux administrateurs quand l'un des deux se
    /// met à refuser — ou recommence à accepter.
    ///
    /// UN VRAI APPEL PAYANT, PARCE QUE RIEN D'AUTRE NE DIT LA VÉRITÉ. Le
    /// 14/09/2026, `GET /v1/models` répondait 200 — clé valide — pendant que
    /// chaque synthèse vocale était refusée faute de crédit. Une sonde gratuite
    /// aurait affiché vert sur un produit muet.
    ///
    /// CE QUE ÇA COÛTE : chez OpenAI, le mot « ok » dit par le modèle de voix
    /// réellement utilisé en séance, soit une fraction de seconde d'audio ;
    /// chez Anthropic, un jeton de sortie sur le plus petit modèle. Le crédit
    /// est commun à tout le compte, donc un modèle bon marché suffit à le
    /// constater. Au rythme du worker, quelques centimes par mois en tout.
    /// </summary>
    public class SurveillanceFournisseurs
    {
        private const string EndpointVoix = "https://api.openai.com/v1/audio/speech";

        private readonly HttpClient _http;
        private readonly AnthropicClient _anthropic;
        private readonly OptionsClaude _claude;
        private readonly OptionsVoix _voix;
        private readonly IConfiguration _configuration;
        private readonly EtatFournisseurs _etats;
        private readonly IServiceScopeFactory _scopes;
        private readonly ITelegramService _telegram;
        private readonly ILogger<SurveillanceFournisseurs> _logger;

        public SurveillanceFournisseurs(
            HttpClient http,
            AnthropicClient anthropic,
            IOptions<OptionsClaude> claude,
            IOptions<OptionsVoix> voix,
            IConfiguration configuration,
            EtatFournisseurs etats,
            IServiceScopeFactory scopes,
            ITelegramService telegram,
            ILogger<SurveillanceFournisseurs> logger)
        {
            _telegram = telegram ?? throw new ArgumentNullException(nameof(telegram));
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _anthropic = anthropic ?? throw new ArgumentNullException(nameof(anthropic));
            _claude = claude?.Value ?? throw new ArgumentNullException(nameof(claude));
            _voix = voix?.Value ?? throw new ArgumentNullException(nameof(voix));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _etats = etats ?? throw new ArgumentNullException(nameof(etats));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Vérifie les deux fournisseurs, prévient si besoin, et rend leur état.</summary>
        public async Task<IReadOnlyList<EtatFournisseur>> VerifierAsync(CancellationToken ct = default)
        {
            await _etats.Verrou.WaitAsync(ct);

            try
            {
                var anthropic = SonderAnthropicAsync(ct);
                var openai = SonderOpenAiAsync(ct);
                await Task.WhenAll(anthropic, openai);

                var maintenant = DateTime.UtcNow;
                _etats.Enregistrer(EtatFournisseurs.Anthropic, _claude.ModeleTranscription, await anthropic, maintenant);
                _etats.Enregistrer(EtatFournisseurs.OpenAi, _voix.Modele, await openai, maintenant);

                foreach (var etat in _etats.Tous())
                {
                    await PrevenirTelegramSiBesoinAsync(etat);
                    await PrevenirSiBesoinAsync(etat, ct);
                }

                return _etats.Tous();
            }
            finally
            {
                _etats.Verrou.Release();
            }
        }

        private async Task<ResultatSonde> SonderAnthropicAsync(CancellationToken ct)
        {
            var cle = _configuration["Claude:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrWhiteSpace(cle)) return new ResultatSonde(StatutFournisseur.NonConfigure);

            var chrono = Stopwatch.StartNew();

            try
            {
                await _anthropic.Messages.Create(
                    new MessageCreateParams
                    {
                        Model = _claude.ModeleTranscription,
                        MaxTokens = 1,
                        Messages = new List<MessageParam>
                        {
                            new() { Role = Role.User, Content = "ok" },
                        },
                    },
                    cancellationToken: ct);

                return new ResultatSonde(StatutFournisseur.Ok, 200, DureeMs: chrono.ElapsedMilliseconds);
            }
            catch (AnthropicApiException ex)
            {
                var resultat = EtatFournisseurs.Classer((int)ex.StatusCode, ex.ResponseBody, chrono.ElapsedMilliseconds);
                Journaliser("Anthropic", resultat);
                return resultat;
            }
            catch (Exception ex) when (ex is AnthropicIOException or HttpRequestException
                                       || (ex is OperationCanceledException && !ct.IsCancellationRequested))
            {
                _logger.LogWarning(ex, "Sonde Anthropic : fournisseur injoignable.");
                return new ResultatSonde(StatutFournisseur.Injoignable, Detail: ex.Message, DureeMs: chrono.ElapsedMilliseconds);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Sonde Anthropic : erreur inattendue.");
                return new ResultatSonde(StatutFournisseur.Erreur, Detail: ex.Message, DureeMs: chrono.ElapsedMilliseconds);
            }
        }

        private async Task<ResultatSonde> SonderOpenAiAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_voix.ApiKey)) return new ResultatSonde(StatutFournisseur.NonConfigure);

            var chrono = Stopwatch.StartNew();

            try
            {
                using var requete = new HttpRequestMessage(HttpMethod.Post, EndpointVoix)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new
                        {
                            model = _voix.Modele,
                            input = "ok",
                            voice = "coral",
                            response_format = "pcm",
                        }),
                        Encoding.UTF8,
                        "application/json"),
                };
                requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _voix.ApiKey);

                // Les en-têtes suffisent : l'audio d'un succès n'est pas lu.
                using var reponse = await _http.SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, ct);

                if (reponse.IsSuccessStatusCode)
                {
                    return new ResultatSonde(StatutFournisseur.Ok, 200, DureeMs: chrono.ElapsedMilliseconds);
                }

                var corps = await reponse.Content.ReadAsStringAsync(ct);
                var resultat = EtatFournisseurs.Classer((int)reponse.StatusCode, corps, chrono.ElapsedMilliseconds);
                Journaliser("OpenAI", resultat);
                return resultat;
            }
            catch (Exception ex) when (ex is HttpRequestException
                                       || (ex is OperationCanceledException && !ct.IsCancellationRequested))
            {
                _logger.LogWarning(ex, "Sonde OpenAI : fournisseur injoignable.");
                return new ResultatSonde(StatutFournisseur.Injoignable, Detail: ex.Message, DureeMs: chrono.ElapsedMilliseconds);
            }
        }

        private void Journaliser(string fournisseur, ResultatSonde resultat)
        {
            var niveau = StatutFournisseur.Bloquant(resultat.Statut) ? LogLevel.Error : LogLevel.Warning;

            _logger.Log(niveau,
                "Sonde {Fournisseur} : {Statut} ({Code} {CodeErreur}) {Detail}",
                fournisseur, resultat.Statut, resultat.CodeHttp, resultat.CodeErreur, resultat.Detail);
        }

        /// <summary>
        /// Un message Telegram dès qu'un fournisseur passe au rouge ou à
        /// l'orange — ou change d'état sans en sortir, d'orange à rouge par
        /// exemple — puis un autre quand il fonctionne de nouveau.
        ///
        /// JAMAIS DEUX FOIS LE MÊME ÉTAT : le worker repasse toutes les cinq
        /// minutes sur un fournisseur en défaut, et le salon des signalements
        /// deviendrait illisible s'il répétait « crédit épuisé » à chaque fois.
        ///
        /// Les états neutres (pas de clé, pas encore vérifié) ne disent rien et
        /// ne changent rien à la mémoire : un redémarrage ne doit pas annoncer
        /// « rétabli » ni rejouer une alerte déjà lue.
        ///
        /// Marqué sans attendre de preuve d'envoi : `TelegramService` avale ses
        /// erreurs et ne dit pas si le message est parti. Une alerte Telegram
        /// perdue reste couverte par le courriel pour les états rouges.
        /// </summary>
        private async Task PrevenirTelegramSiBesoinAsync(EtatFournisseur etat)
        {
            var dejaSignale = _etats.DernierSignaleTelegram(etat.Code);

            if (StatutFournisseur.EnDefaut(etat.Statut))
            {
                if (dejaSignale == etat.Statut) return;

                await _telegram.NotifierFournisseurAsync(etat, StatutFournisseur.Libelle(etat.Statut), retabli: false);
                _etats.MarquerSignaleTelegram(etat.Code, etat.Statut);
                return;
            }

            if (etat.Statut == StatutFournisseur.Ok
                && dejaSignale is not null
                && StatutFournisseur.EnDefaut(dejaSignale))
            {
                await _telegram.NotifierFournisseurAsync(etat, StatutFournisseur.Libelle(etat.Statut), retabli: true);
                _etats.MarquerSignaleTelegram(etat.Code, etat.Statut);
            }
        }

        /// <summary>
        /// Un courriel à l'entrée dans un état bloquant, un autre au retour à la
        /// normale — et rien entre les deux, même si le worker repasse toutes
        /// les cinq minutes sur le même compte à sec.
        ///
        /// Les états passagers (freinage, panne chez eux, réseau) ne changent
        /// rien à la mémoire des envois : un compte à sec qui répond « surchargé »
        /// entre deux passages ne doit ni relancer l'alerte, ni l'annoncer réglée.
        /// </summary>
        private async Task PrevenirSiBesoinAsync(EtatFournisseur etat, CancellationToken ct)
        {
            var dejaSignale = _etats.DernierSignale(etat.Code);
            var bloque = StatutFournisseur.Bloquant(etat.Statut);

            var panne = bloque && dejaSignale != etat.Statut;
            var retabli = etat.Statut == StatutFournisseur.Ok
                          && dejaSignale is not null
                          && StatutFournisseur.Bloquant(dejaSignale);

            if (!panne && !retabli) return;

            // LA MÊME LISTE QUI DONNE LE RÔLE ADMIN — voir EcheanceReferentielWorker.
            var destinataires = _configuration.GetSection("Admin:Emails").Get<string[]>() ?? [];
            if (destinataires.Length == 0)
            {
                _logger.LogWarning("Fournisseur {Code} : {Statut}, mais aucune adresse dans Admin:Emails.",
                    etat.Code, etat.Statut);
                return;
            }

            using var scope = _scopes.CreateScope();
            var email = scope.ServiceProvider.GetRequiredService<IServiceEmail>();

            var (sujet, titre, message) = Rediger(etat, panne);
            var envoye = false;

            foreach (var destinataire in destinataires)
            {
                try
                {
                    envoye |= await email.EnvoyerAsync(
                        destinataire,
                        sujet,
                        "fournisseur-ia",
                        new Dictionary<string, string>
                        {
                            ["titre"] = titre,
                            ["fournisseur"] = etat.Nom,
                            ["date"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Paris)
                                .ToString("dd/MM/yyyy 'à' HH:mm"),
                            ["message"] = message,
                            ["detail"] = Detail(etat),
                        },
                        ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Courriel d'alerte fournisseur non envoye a {Destinataire}.", destinataire);
                }
            }

            // Marqué seulement si un courriel est parti : sinon le passage
            // suivant retentera, au lieu de croire l'alerte donnée.
            if (envoye) _etats.MarquerSignale(etat.Code, etat.Statut);
        }

        private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

        private static (string Sujet, string Titre, string Message) Rediger(EtatFournisseur etat, bool panne)
        {
            if (!panne)
            {
                return (
                    $"{etat.Nom} fonctionne de nouveau",
                    $"{etat.Nom} fonctionne de nouveau",
                    $"Les appels à {etat.Nom} passent à nouveau. Ce qui en dépend est rétabli : "
                    + WebUtility.HtmlEncode(etat.Usage));
            }

            return etat.Statut == StatutFournisseur.CreditEpuise
                ? (
                    $"Crédit {etat.Nom} épuisé — une partie de Mimia est coupée",
                    $"Plus de crédit chez {etat.Nom}",
                    $"{etat.Nom} refuse les appels faute de crédit. Tant que le compte n'est pas rechargé, "
                    + $"ceci ne fonctionne plus : {WebUtility.HtmlEncode(etat.Usage)}")
                : (
                    $"Clé {etat.Nom} refusée — une partie de Mimia est coupée",
                    $"La clé {etat.Nom} est refusée",
                    $"{etat.Nom} refuse la clé API du serveur (invalide, révoquée ou sans droit). Tant "
                    + $"qu'elle n'est pas remplacée, ceci ne fonctionne plus : {WebUtility.HtmlEncode(etat.Usage)}");
        }

        private static string Detail(EtatFournisseur etat)
        {
            var lignes = new List<string>();

            if (etat.CodeHttp is not null || etat.CodeErreur is not null)
            {
                lignes.Add(WebUtility.HtmlEncode(
                    $"Réponse : {etat.CodeHttp} {etat.CodeErreur}".Trim()));
            }

            if (!string.IsNullOrWhiteSpace(etat.Detail))
            {
                lignes.Add(WebUtility.HtmlEncode(etat.Detail));
            }

            lignes.Add($"<a href=\"{etat.LienFacturation}\">{etat.LienFacturation}</a>");

            return string.Join("<br>", lignes);
        }
    }
}
