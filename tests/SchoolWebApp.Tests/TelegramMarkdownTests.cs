using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Services.Fournisseurs;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Models;
using Xunit.Abstractions;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// LES ALERTES TELEGRAM DOIVENT ÊTRE DU MARKDOWNV2 VALIDE (23/11/2026). Un point littéral non échappé dans la
    /// veille des tarifs faisait rejeter l'alerte en 400 (« Character '.' is reserved ») à chaque démarrage : 21 alertes
    /// perdues en deux jours. Ces tests construisent CHAQUE message par les vraies méthodes du service, avec des
    /// valeurs pleines de caractères réservés, et vérifient qu'aucun caractère réservé n'est laissé nu. AUCUNE BASE,
    /// AUCUN RÉSEAU : un faux gestionnaire HTTP capture ce qui serait parti chez Telegram.
    /// </summary>
    public class TelegramMarkdownTests
    {
        private const string Piege = "a.b-c_d*e[f]g(h)i~j`k>l#m+n=o|p{q}r!s\\t";

        private readonly ITestOutputHelper _sortie;

        public TelegramMarkdownTests(ITestOutputHelper sortie) => _sortie = sortie;

        private sealed class FauxTelegram : HttpMessageHandler
        {
            private readonly Func<Dictionary<string, string>, HttpResponseMessage> _repondre;

            public FauxTelegram(Func<Dictionary<string, string>, HttpResponseMessage>? repondre = null)
            {
                _repondre = repondre ?? (_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") });
            }

            public List<Dictionary<string, string>> Envois { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken jeton)
            {
                var corps = await requete.Content!.ReadAsStringAsync(jeton);
                var champs = corps.Split('&').Select(p => p.Split('=', 2))
                    .ToDictionary(p => WebUtility.UrlDecode(p[0]), p => WebUtility.UrlDecode(p[1]));
                Envois.Add(champs);
                return _repondre(champs);
            }
        }

        private static (TelegramService Service, FauxTelegram Faux) Creer(FauxTelegram? faux = null)
        {
            faux ??= new FauxTelegram();
            var fabrique = new Mock<IHttpClientFactory>();
            fabrique.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(faux));

            var salons = new Dictionary<string, string?>();
            foreach (var s in new[] { "TelegramNouveauParent", "TelegramAbonnement", "TelegramResiliation", "TelegramAvis",
                                      "TelegramContact", "TelegramExploitation", "TelegramEmails" })
            {
                salons[$"{s}:BotToken"] = "jeton-factice";
                salons[$"{s}:ChatId"] = "salon-factice";
            }

            var config = new ConfigurationBuilder().AddInMemoryCollection(salons).Build();
            return (new TelegramService(fabrique.Object, config, NullLogger<TelegramService>.Instance), faux);
        }

        /// <summary>
        /// Les erreurs de MarkdownV2 que Telegram rejette : un caractère réservé non précédé de « \ » hors code, et
        /// une marque de mise en forme (* _ ~) non refermée. Dans un `code`, seuls ` et \ comptent.
        /// </summary>
        private static List<string> ErreursMarkdownV2(string texte)
        {
            const string Reserves = "_*[]()~`>#+-=|{}.!";
            var erreurs = new List<string>();
            var ouvertes = new Dictionary<char, int> { ['*'] = 0, ['_'] = 0, ['~'] = 0 };
            var dansCode = false;

            for (var i = 0; i < texte.Length; i++)
            {
                var c = texte[i];
                if (c == '\\') { i++; continue; }
                if (c == '`') { dansCode = !dansCode; continue; }
                if (dansCode) continue;
                if (ouvertes.ContainsKey(c)) { ouvertes[c] ^= 1; continue; }
                if (Reserves.Contains(c))
                {
                    var debut = Math.Max(0, i - 25);
                    erreurs.Add($"« {c} » nu en position {i} : …{texte[debut..Math.Min(texte.Length, i + 10)]}…");
                }
            }

            if (dansCode) erreurs.Add("` non refermé");
            erreurs.AddRange(ouvertes.Where(o => o.Value != 0).Select(o => $"« {o.Key} » non refermé"));
            return erreurs;
        }

        public static IEnumerable<object[]> ToutesLesAlertes()
        {
            var parent = new Parent { Prenom = Piege, Nom = Piege, Mail = "jean.dupont-x@exemple.invalid" };
            var etat = new EtatFournisseur
            {
                Code = "openai", Nom = Piege, Usage = Piege, LienFacturation = "https://platform.openai.com/a_b?c=d",
                Statut = StatutFournisseur.CreditEpuise, CodeHttp = 429, CodeErreur = "insufficient_quota", Detail = Piege,
                VerifieLe = DateTime.UtcNow,
            };

            yield return Cas("mail reçu", s => s.NotifierMailRecuAsync(Piege, Piege, "a.b@c.d", Piege, Piege));
            yield return Cas("nouveau parent", s => s.NotifierNouveauParentAsync(Piege, Piege, "a.b@c.d"));
            yield return Cas("abonnement", s => s.NotifierAbonnementAsync(parent, Piege, Piege, false));
            yield return Cas("changement", s => s.NotifierAbonnementAsync(null, Piege, null, true));
            yield return Cas("résiliation", s => s.NotifierResiliationAsync(parent, Piege, DateTime.UtcNow));
            yield return Cas("avis", s => s.NotifierAvisAsync("a.b@c.d", 3, Piege, Piege, DateTime.UtcNow));
            yield return Cas("étalement bilans", s => s.NotifierEtalementBilansAsync(1200, 3, 400, 2));
            yield return Cas("contact", s => s.NotifierContactAsync(Piege, "a.b@c.d", Piege, Piege, true));
            yield return Cas("signalement", s => s.NotifierSignalementAsync("TECHNIQUE", "a.b@c.d", Piege, Piege));
            yield return Cas("signalement admin", s => s.NotifierSignalementAdminAsync("AUTRE", Piege, Piege, Piege, Piege));
            yield return Cas("signalement admin sans jeu", s => s.NotifierSignalementAdminAsync("X.Y", Piege, null, null, null));
            yield return Cas("idée", s => s.NotifierIdeeAsync(Piege, Piege, Piege, Piege));
            yield return Cas("fournisseur en panne", s => s.NotifierFournisseurAsync(etat, Piege, false));
            yield return Cas("fournisseur rétabli", s => s.NotifierFournisseurAsync(etat, Piege, true));
            // Les deux coupables du 23/11 : appelées par les veilles des tarifs à chaque démarrage.
            yield return Cas("tarif mis à jour", s => s.NotifierTarifAsync("OpenAI", "gpt-4.1-mini", ["Entrée : 0.40 $ → 0.50 $", Piege]));
            yield return Cas("veille des tarifs", s => s.NotifierVeilleTarifsAsync("Anthropic", Piege));
        }

        private static object[] Cas(string nom, Func<TelegramService, Task> envoyer) => [nom, envoyer];

        [Theory]
        [MemberData(nameof(ToutesLesAlertes))]
        public async Task Chaque_alerte_est_du_MarkdownV2_valide(string nom, Func<TelegramService, Task> envoyer)
        {
            var (service, faux) = Creer();

            await envoyer(service);

            var envoi = Assert.Single(faux.Envois);
            Assert.Equal("MarkdownV2", envoi["parse_mode"]);
            var erreurs = ErreursMarkdownV2(envoi["text"]);
            Assert.True(erreurs.Count == 0, $"{nom} : {string.Join(" | ", erreurs)}\n---\n{envoi["text"]}");
        }

        [Fact]
        public async Task Les_valeurs_reservees_sont_toutes_echappees()
        {
            var (service, faux) = Creer();

            await service.NotifierVeilleTarifsAsync("Anthropic", Piege);

            Assert.Contains(
                "a\\.b\\-c\\_d\\*e\\[f\\]g\\(h\\)i\\~j\\`k\\>l\\#m\\+n\\=o\\|p\\{q\\}r\\!s\\\\t",
                faux.Envois[0]["text"]);
        }

        [Fact]
        public async Task Un_Markdown_refuse_repart_en_texte_brut_lisible()
        {
            // Telegram refuse la mise en forme la 1re fois, accepte le texte brut ensuite.
            var faux = new FauxTelegram(champs => champs.ContainsKey("parse_mode")
                ? new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"ok\":false,\"error_code\":400,\"description\":\"Bad Request: can't parse entities: Character '.' is reserved and must be escaped with the preceding '\\\\'\"}", Encoding.UTF8, "application/json"),
                }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") });
            var (service, _) = Creer(faux);

            await service.NotifierAvisAsync("jean.dupont@exemple.invalid", 4, "Très bien !", "Merci (vraiment).", DateTime.UtcNow);

            Assert.Equal(2, faux.Envois.Count);
            var brut = faux.Envois[1];
            Assert.False(brut.ContainsKey("parse_mode"));
            Assert.DoesNotContain("\\", brut["text"]);
            Assert.DoesNotContain("*", brut["text"]);
            Assert.Contains("⭐ Nouvel avis", brut["text"]);
            Assert.Contains("jean.dupont@exemple.invalid", brut["text"]);
            Assert.Contains("Merci (vraiment).", brut["text"]);
            Assert.Contains("En attente de relecture.", brut["text"]);
        }

        /// <summary>
        /// ESSAI RÉEL CONTRE L'ANALYSEUR DE TELEGRAM, SANS RIEN ENVOYER (24/11/2026). Inactif par défaut : ne fait rien
        /// sans `MIMIA_TELEGRAM_ESSAI=1` et `TELEGRAM_BOT_TOKEN` (n'importe quel bot valide).
        ///
        /// Telegram analyse le MarkdownV2 AVANT de chercher le salon : vers le salon inexistant « 0 », un texte fautif
        /// répond « can't parse entities », un texte valide « chat not found ». On soumet ainsi chaque alerte, telle que
        /// le service la construit, au vrai analyseur de Telegram — et aucun message n'arrive nulle part.
        /// </summary>
        [Fact]
        public async Task Essai_reel_chaque_alerte_passe_l_analyseur_de_Telegram_sans_envoi()
        {
            var jeton = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            if (Environment.GetEnvironmentVariable("MIMIA_TELEGRAM_ESSAI") != "1" || string.IsNullOrWhiteSpace(jeton)) return;

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

            async Task<string> Analyser(string texte)
            {
                var reponse = await client.PostAsync(
                    $"https://api.telegram.org/bot{jeton}/sendMessage",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["chat_id"] = "0",
                        ["text"] = texte,
                        ["parse_mode"] = "MarkdownV2",
                    }));
                return await reponse.Content.ReadAsStringAsync();
            }

            // TÉMOINS : l'essai ne prouve quelque chose que si Telegram distingue bien les deux cas. La ligne de la
            // veille des tarifs telle qu'en production avant le correctif (points nus) doit être refusée à l'analyse ;
            // la même ligne échappée doit passer (« chat not found »). Envoyés par .NET : texte en UTF-8 quelle que
            // soit la console (la v1 les passait par la ligne de commande Windows : « text must be encoded in UTF-8 »).
            var temoinFautif = await Analyser(
                "— Le dernier prix connu reste appliqué. À vérifier dans Administration › Anthropic / OpenAI / Gemini › Tarifs.");
            var temoinEchappe = await Analyser(
                "— Le dernier prix connu reste appliqué\\. À vérifier dans Administration › Anthropic / OpenAI / Gemini › Tarifs\\.");
            _sortie.WriteLine($"TÉMOIN 1 (points nus) : {temoinFautif}");
            _sortie.WriteLine($"TÉMOIN 2 (échappés)   : {temoinEchappe}");
            Assert.Contains("can't parse entities", temoinFautif);
            Assert.Contains("chat not found", temoinEchappe);

            var refus = new List<string>();
            var n = 0;
            foreach (var cas in ToutesLesAlertes())
            {
                var (nom, envoyer) = ((string)cas[0], (Func<TelegramService, Task>)cas[1]);
                var (service, faux) = Creer();
                await envoyer(service);

                var corps = await Analyser(faux.Envois[0]["text"]);
                _sortie.WriteLine($"ALERTE {++n,2}/16 {nom} : {corps}");

                // Seul « chat not found » prouve que le texte a passé l'analyse ; tout le reste est un échec.
                if (!corps.Contains("chat not found", StringComparison.OrdinalIgnoreCase)) refus.Add($"{nom} : {corps}");
            }

            Assert.Equal(16, n);
            Assert.True(refus.Count == 0, string.Join("\n", refus));
        }

        [Fact]
        public async Task Un_autre_refus_ne_relance_pas_en_texte_brut()
        {
            var faux = new FauxTelegram(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"ok\":false,\"error_code\":400,\"description\":\"Bad Request: chat not found\"}"),
            });
            var (service, _) = Creer(faux);

            await service.NotifierVeilleTarifsAsync("Anthropic", "page illisible");

            Assert.Single(faux.Envois);
        }
    }
}
