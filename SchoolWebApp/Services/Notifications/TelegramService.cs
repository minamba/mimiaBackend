using System.Text;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.Notifications
{
    /// <summary>
    /// Les alertes Telegram de l'exploitant.
    ///
    /// POURQUOI CES ÉVÉNEMENTS-LÀ ET PAS D'AUTRES
    /// ------------------------------------------
    /// Quatre moments où quelque chose bouge : quelqu'un arrive, quelqu'un
    /// paie, quelqu'un s'en va, quelqu'un écrit. Ce sont les seuls qu'on veut
    /// savoir dans la minute, sans ouvrir un tableau de bord.
    ///
    /// QUATRE BOTS, QUATRE SALONS, ET C'EST VOULU. Un salon unique mélangerait
    /// une inscription anodine et une résiliation ; la seconde se noierait dans
    /// les premières, or c'est celle à laquelle on veut réagir vite.
    ///
    /// UNE ALERTE NE DOIT JAMAIS CASSER CE QU'ELLE OBSERVE. Toutes les erreurs
    /// sont avalées et journalisées : Telegram indisponible, jeton révoqué,
    /// réseau coupé — rien de tout cela ne doit empêcher un parent de
    /// s'abonner ou de résilier. Le message est perdu, l'opération réussit.
    /// </summary>
    public interface ITelegramService
    {
        /// <summary>Un parent vient de créer son compte.</summary>
        Task NotifierNouveauParentAsync(string? prenom, string? nom, string? mail);

        /// <summary>Un parent vient de souscrire, ou de changer de formule.</summary>
        /// <param name="changement">
        /// Vrai pour un changement de formule. Le titre du message en dépend :
        /// une souscription et un changement se lisent très différemment quand
        /// on compte ses abonnés.
        /// </param>
        Task NotifierAbonnementAsync(
            Parent? parent, string formule, string? periodicite, bool changement);

        /// <summary>Un parent a demandé la résiliation de son abonnement.</summary>
        Task NotifierResiliationAsync(Parent? parent, string? formule, DateTime? finPeriode);

        /// <summary>
        /// Quelqu'un a écrit par le formulaire de contact.
        /// </summary>
        /// <param name="connecte">
        /// Vrai si l'adresse vient du jeton et non du formulaire. La nuance
        /// compte pour répondre : l'adresse d'un membre connecté est certifiée,
        /// celle d'un visiteur est déclarative et peut être fautive.
        /// </param>
        /// <summary>
        /// Prévient que la série des bilans dépasse le nombre de jours
        /// d'étalement qu'on s'est fixé.
        ///
        /// C'EST UNE ALERTE DE CROISSANCE, PAS UNE PANNE. Rien n'est cassé :
        /// l'étalement absorbe la charge tout seul. Mais chaque jour ajouté
        /// éloigne le dernier groupe de parents de son lundi, et au-delà d'un
        /// certain point la bonne réponse n'est plus technique — c'est de payer
        /// une offre supérieure. Ce message est là pour que la décision se
        /// prenne À TEMPS, et non le jour où un parent écrit qu'il n'a rien
        /// reçu.
        /// </summary>
        Task NotifierEtalementBilansAsync(int total, int groupes, int budget, int seuil);

        Task NotifierContactAsync(
            string? nom, string? mail, string? sujet, string? message, bool connecte);
    }

    public class TelegramService : ITelegramService
    {
        private readonly IHttpClientFactory _fabrique;
        private readonly ILogger<TelegramService> _logger;

        private readonly string? _parentToken;
        private readonly string? _parentChatId;
        private readonly string? _abonnementToken;
        private readonly string? _abonnementChatId;
        private readonly string? _resiliationToken;
        private readonly string? _resiliationChatId;
        private readonly string? _contactToken;
        private readonly string? _contactChatId;
        private readonly string? _exploitationToken;
        private readonly string? _exploitationChatId;

        public TelegramService(
            IHttpClientFactory fabrique, IConfiguration config, ILogger<TelegramService> logger)
        {
            _fabrique = fabrique ?? throw new ArgumentNullException(nameof(fabrique));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _parentToken = config["TelegramNouveauParent:BotToken"];
            _parentChatId = config["TelegramNouveauParent:ChatId"];
            _abonnementToken = config["TelegramAbonnement:BotToken"];
            _abonnementChatId = config["TelegramAbonnement:ChatId"];
            _resiliationToken = config["TelegramResiliation:BotToken"];
            _resiliationChatId = config["TelegramResiliation:ChatId"];
            _contactToken = config["TelegramContact:BotToken"];
            _contactChatId = config["TelegramContact:ChatId"];

            // LES ALERTES D'EXPLOITATION, AVEC REPLI SUR LE SALON DES
            // ABONNEMENTS.
            //
            // Ce sont des messages rares et techniques : croissance qui dépasse
            // le quota d'envoi, et ce qui viendra s'y ajouter. Ils méritent leur
            // propre salon, mais en créer un est une démarche manuelle chez
            // BotFather — et tant qu'elle n'est pas faite, une alerte qui ne
            // part nulle part ne vaut rien. Le salon des abonnements est le
            // moins mauvais repli : il parle déjà d'argent, et l'action
            // attendue ici est justement de payer une offre supérieure.
            _exploitationToken = config["TelegramExploitation:BotToken"];
            _exploitationChatId = config["TelegramExploitation:ChatId"];

            if (string.IsNullOrWhiteSpace(_exploitationToken))
            {
                _exploitationToken = _abonnementToken;
                _exploitationChatId = _abonnementChatId;
            }
        }

        public Task NotifierNouveauParentAsync(string? prenom, string? nom, string? mail)
        {
            var sb = new StringBuilder();
            sb.AppendLine("👋 *Nouveau parent inscrit*");
            sb.AppendLine($"— Prénom : {Echapper(prenom)}");
            sb.AppendLine($"— Nom : {Echapper(nom)}");
            sb.AppendLine($"— Email : {Echapper(mail)}");
            sb.AppendLine($"— Le : {Horodatage()}");

            return EnvoyerAsync(_parentToken, _parentChatId, sb.ToString());
        }

        public Task NotifierAbonnementAsync(
            Parent? parent, string formule, string? periodicite, bool changement)
        {
            var sb = new StringBuilder();

            // Le titre porte la distinction : c'est la première ligne qu'on lit
            // sur un téléphone, souvent la seule.
            sb.AppendLine(changement
                ? "🔄 *Changement de formule*"
                : "💳 *Nouvel abonnement*");

            AjouterParent(sb, parent);
            sb.AppendLine($"— Formule : {Echapper(formule)}");

            if (!string.IsNullOrWhiteSpace(periodicite))
            {
                sb.AppendLine($"— Rythme : {Echapper(periodicite)}");
            }

            sb.AppendLine($"— Le : {Horodatage()}");

            return EnvoyerAsync(_abonnementToken, _abonnementChatId, sb.ToString());
        }

        public Task NotifierResiliationAsync(Parent? parent, string? formule, DateTime? finPeriode)
        {
            var sb = new StringBuilder();
            sb.AppendLine("⚠️ *Demande de résiliation*");

            AjouterParent(sb, parent);

            if (!string.IsNullOrWhiteSpace(formule))
            {
                sb.AppendLine($"— Formule : {Echapper(formule)}");
            }

            // LA DATE DE FIN EST LE SEUL CHIFFRE ACTIONNABLE DU MESSAGE.
            //
            // Une résiliation ne coupe rien tout de suite : l'accès court
            // jusqu'au bout de la période payée, et la décision s'annule
            // jusque-là. C'est la fenêtre pendant laquelle on peut encore
            // écrire au parent.
            if (finPeriode is not null)
            {
                sb.AppendLine($"— Accès jusqu'au : {Echapper(finPeriode.Value.ToString("dd/MM/yyyy"))}");
            }

            sb.AppendLine($"— Le : {Horodatage()}");

            return EnvoyerAsync(_resiliationToken, _resiliationChatId, sb.ToString());
        }

        public Task NotifierEtalementBilansAsync(int total, int groupes, int budget, int seuil)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📈 *Bilans : l'étalement dépasse le seuil*");
            sb.AppendLine($"— Bilans à envoyer : {Echapper(total.ToString())}");
            sb.AppendLine($"— Étalés sur : {Echapper(groupes.ToString())} jours");
            sb.AppendLine($"— Seuil fixé : {Echapper(seuil.ToString())} jours");
            sb.AppendLine($"— Budget actuel : {Echapper(budget.ToString())} courriels/jour");
            sb.AppendLine();
            sb.AppendLine("Rien n'est cassé : la série s'étale toute seule\\.");
            sb.AppendLine(
                "Mais le dernier groupe de parents reçoit son bilan "
                + $"{Echapper((groupes - 1).ToString())} jours après lundi\\.");
            sb.AppendLine();
            sb.AppendLine(
                "*Pour revenir à moins de jours* : passer à une offre supérieure "
                + "chez le service d'envoi, puis remonter `Bilans:BilansParJour` "
                + "dans la configuration\\.");
            sb.AppendLine($"— Le : {Horodatage()}");

            return EnvoyerAsync(_exploitationToken, _exploitationChatId, sb.ToString());
        }

        public Task NotifierContactAsync(
            string? nom, string? mail, string? sujet, string? message, bool connecte)
        {
            var sb = new StringBuilder();
            sb.AppendLine("✉️ *Nouveau message de contact*");
            sb.AppendLine($"— Nom : {Echapper(nom)}");
            sb.AppendLine($"— Email : {Echapper(mail)}");

            // Dit d'où vient l'adresse. Celle d'un membre connecté est tirée du
            // jeton, donc certifiée ; celle d'un visiteur est déclarative, et
            // une faute de frappe y envoie la réponse dans le vide.
            sb.AppendLine($"— Compte : {(connecte ? "membre connecté" : "visiteur non connecté")}");

            sb.AppendLine($"— Sujet : {Echapper(sujet)}");
            sb.AppendLine($"— Le : {Horodatage()}");
            sb.AppendLine("———————————————");
            sb.AppendLine(Echapper(Borner(message)));

            return EnvoyerAsync(_contactToken, _contactChatId, sb.ToString());
        }

        /// <summary>
        /// Tronque un message trop long pour Telegram.
        ///
        /// LA LIMITE EST DE 4 096 CARACTÈRES, ET ELLE EST DURE : au-delà,
        /// Telegram refuse le message ENTIER — on n'aurait donc pas un message
        /// coupé, on n'aurait rien du tout, et personne ne remarque une alerte
        /// qui n'arrive pas. La marge couvre l'entête et l'échappement, qui
        /// peut doubler certains caractères.
        /// </summary>
        private static string Borner(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "—";
            if (message.Length <= 1500) return message;

            return message[..1500] + "\n[…] message tronqué, voir le courriel";
        }

        private static void AjouterParent(StringBuilder sb, Parent? parent)
        {
            if (parent is null)
            {
                sb.AppendLine("— Parent inconnu");
                return;
            }

            sb.AppendLine($"— Prénom : {Echapper(parent.Prenom)}");
            sb.AppendLine($"— Nom : {Echapper(parent.Nom)}");
            sb.AppendLine($"— Email : {Echapper(parent.Mail)}");
        }

        private static string Horodatage() =>
            Echapper($"{DateTime.UtcNow:dd/MM/yyyy} à {DateTime.UtcNow:HH:mm} UTC");

        /// <summary>
        /// Échappe les caractères réservés de MarkdownV2.
        ///
        /// SANS ÇA, TELEGRAM REFUSE LE MESSAGE ENTIER. Un point dans une
        /// adresse électronique, un tiret dans un nom composé : le message
        /// part en 400 et l'alerte n'arrive jamais — silencieusement, puisque
        /// personne ne surveille l'absence d'un message.
        ///
        /// La barre oblique est échappée EN PREMIER : la traiter après aurait
        /// doublé les échappements qu'on vient de poser.
        /// </summary>
        private static string Echapper(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "—";

            return s.Replace("\\", "\\\\")
                    .Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[")
                    .Replace("]", "\\]").Replace("(", "\\(").Replace(")", "\\)")
                    .Replace("~", "\\~").Replace("`", "\\`")
                    .Replace(">", "\\>").Replace("#", "\\#").Replace("+", "\\+")
                    .Replace("-", "\\-").Replace("=", "\\=").Replace("|", "\\|")
                    .Replace("{", "\\{").Replace("}", "\\}").Replace(".", "\\.")
                    .Replace("!", "\\!");
        }

        private async Task EnvoyerAsync(string? jeton, string? salon, string texte)
        {
            // Non configuré : on se tait. C'est l'état normal en développement,
            // et ce n'est pas une panne — journaliser à chaque événement
            // remplirait les traces locales d'avertissements sans objet.
            if (string.IsNullOrWhiteSpace(jeton) || string.IsNullOrWhiteSpace(salon)) return;

            try
            {
                var client = _fabrique.CreateClient();

                // Court : une alerte en retard ne sert à rien, et une alerte
                // qui traîne retiendrait le fil d'exécution qui l'a déclenchée.
                client.Timeout = TimeSpan.FromSeconds(10);

                var reponse = await client.PostAsync(
                    $"https://api.telegram.org/bot{jeton}/sendMessage",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["chat_id"] = salon,
                        ["text"] = texte,
                        ["parse_mode"] = "MarkdownV2",
                    }));

                if (!reponse.IsSuccessStatusCode)
                {
                    // Le corps de la réponse EST le diagnostic : Telegram y dit
                    // « chat not found », « bot was blocked », ou quel caractère
                    // a fait échouer le Markdown. Sans lui on ne saurait que
                    // « ça n'a pas marché ».
                    var corps = await reponse.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Telegram : envoi refuse ({Statut}) — {Corps}",
                        (int)reponse.StatusCode, corps);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram : envoi impossible vers le salon {Salon}.", salon);
            }
        }
    }
}
