using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// LES COURRIELS QUI ARRIVENT — Camara, le 19/10/2026 : « quand un mail
    /// arrive sur la boîte de l'utilisateur, une notification en bas à droite
    /// de l'écran, avec une petite cloche », et pour minamba.camara@mimia.fr,
    /// un message dans le groupe Telegram des emails.
    ///
    /// UNE RELÈVE PAR MINUTE, ET SEULEMENT DES BOÎTES QUI ONT UN DESTINATAIRE.
    /// La boîte d'un administrateur n'est lue que tant qu'il a un onglet
    /// d'administration ouvert (le flux `admin/evenements` le dit) ; une boîte
    /// déclarée dans `TelegramEmails:Adresses` est lue tout le temps. Les
    /// autres ne sont pas touchées : inutile de frapper chez LWS pour rien.
    ///
    /// « NOUVEAU » = un message non lu, d'identifiant IMAP plus grand que le
    /// plus grand déjà vu. La première relève d'une boîte — ou la première
    /// après une absence (onglet fermé plus de trois minutes) — ne fait que
    /// prendre ce repère, sans rien annoncer : sinon un administrateur qui
    /// revient le matin recevrait d'un coup les courriels de la nuit, qu'il
    /// verra de toute façon en ouvrant sa boîte.
    ///
    /// Le repère vit en mémoire : un redémarrage le reprend à la première
    /// relève, sans annonce.
    /// </summary>
    public class SurveillanceBoitesWorker : BackgroundService
    {
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Absence = TimeSpan.FromMinutes(3);

        /// <summary>Au plus autant de messages Telegram par relève et par boîte.</summary>
        private const int TelegramParReleve = 5;

        private sealed record Repere(uint DernierUid, DateTime VuLe);

        private readonly IServiceScopeFactory _scopes;
        private readonly IEvenementsAdminHub _hub;
        private readonly IChiffrementBoites _chiffrement;
        private readonly ITelegramService _telegram;
        private readonly HashSet<string> _adressesTelegram;
        private readonly ILogger<SurveillanceBoitesWorker> _logger;
        private readonly Dictionary<string, Repere> _reperes = new(StringComparer.OrdinalIgnoreCase);

        public SurveillanceBoitesWorker(
            IServiceScopeFactory scopes, IEvenementsAdminHub hub, IChiffrementBoites chiffrement,
            ITelegramService telegram, IConfiguration config, ILogger<SurveillanceBoitesWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _chiffrement = chiffrement ?? throw new ArgumentNullException(nameof(chiffrement));
            _telegram = telegram ?? throw new ArgumentNullException(nameof(telegram));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adressesTelegram = new HashSet<string>(
                (config.GetSection("TelegramEmails:Adresses").Get<string[]>() ?? [])
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ReleverAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Surveillance des boites mail : releve impossible.");
                }

                try
                {
                    await Task.Delay(Intervalle, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task ReleverAsync(CancellationToken ct)
        {
            var connectes = _hub.ParentsConnectes();
            if (connectes.Count == 0 && _adressesTelegram.Count == 0) return;

            using var scope = _scopes.CreateScope();
            var depot = scope.ServiceProvider.GetRequiredService<IMessagerieAdminRepository>();
            var messagerie = scope.ServiceProvider.GetRequiredService<IMessagerieService>();

            // Les boîtes des administrateurs.
            if (_chiffrement.Disponible)
            {
                foreach (var b in await depot.BoitesAsync(ct))
                {
                    var ecran = connectes.Contains(b.ParentId);
                    var telegram = _adressesTelegram.Contains(b.Adresse);
                    if (!ecran && !telegram) continue;

                    var motDePasse = _chiffrement.Dechiffrer(b.MotDePasseChiffre);
                    if (motDePasse is null) continue;

                    var boite = messagerie.Pour(new CompteMessagerie(b.Adresse, motDePasse, null));
                    await ReleverBoiteAsync(b.Adresse, boite, ecran ? b.ParentId : null, "moi", telegram, ct);
                }
            }

            // La boîte commune du support, quand elle est déclarée pour Telegram.
            var support = scope.ServiceProvider.GetRequiredService<IOptions<OptionsMessagerie>>().Value;
            if (support.Configure && support.Adresse is { } adresseSupport && _adressesTelegram.Contains(adresseSupport))
            {
                await ReleverBoiteAsync(adresseSupport, messagerie, null, "support", true, ct);
            }
        }

        private async Task ReleverBoiteAsync(
            string adresse, IMessagerieService boite, int? pourParent, string nomBoite, bool telegram, CancellationToken ct)
        {
            var resultat = await boite.ListerAsync(DossiersMessagerie.Reception, 1, 10, ct);
            if (resultat.Erreur is not null)
            {
                _logger.LogWarning("Surveillance des boites mail : {Adresse} illisible ({Erreur}).", adresse, resultat.Erreur);
                return;
            }

            var maintenant = DateTime.UtcNow;
            var plusGrand = resultat.Messages.Count == 0 ? 0u : resultat.Messages.Max(m => m.Identifiant);

            // Première relève, retour après une absence, ou boîte renumérotée
            // par le serveur (identifiants qui reculent) : on prend le repère.
            if (!_reperes.TryGetValue(adresse, out var repere)
                || maintenant - repere.VuLe > Absence
                || plusGrand < repere.DernierUid)
            {
                _reperes[adresse] = new Repere(plusGrand, maintenant);
                return;
            }

            var nouveaux = resultat.Messages
                .Where(m => m.Identifiant > repere.DernierUid && !m.Lu)
                .OrderBy(m => m.Identifiant)
                .ToList();

            _reperes[adresse] = new Repere(Math.Max(plusGrand, repere.DernierUid), maintenant);
            if (nouveaux.Count == 0) return;

            if (pourParent is int parentId)
            {
                _hub.PublierA(parentId, new
                {
                    type = "mail",
                    boite = nomBoite,
                    nonLus = resultat.NonLus,
                    messages = nouveaux.Select(m => new
                    {
                        id = m.Identifiant,
                        deNom = m.DeNom,
                        deAdresse = m.DeAdresse,
                        sujet = m.Sujet,
                        extrait = m.Extrait,
                    }),
                });
            }

            if (telegram)
            {
                foreach (var m in nouveaux.TakeLast(TelegramParReleve))
                {
                    await _telegram.NotifierMailRecuAsync(adresse, m.DeNom, m.DeAdresse, m.Sujet, m.Extrait);
                }
            }
        }
    }
}
