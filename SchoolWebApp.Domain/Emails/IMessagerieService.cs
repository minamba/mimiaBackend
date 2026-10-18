namespace SchoolWebApp.Domain.Emails
{
    /// <summary>
    /// LES DOSSIERS DE LA MESSAGERIE — Camara, le 17/10/2026 : « Boîte de
    /// réception », « Messages envoyés », « Brouillons ». Les deux premiers sont
    /// les dossiers IMAP de la boîte ; les brouillons sont en base (voir
    /// `BrouillonMail`).
    /// </summary>
    public static class DossiersMessagerie
    {
        public const string Reception = "reception";
        public const string Envoyes = "envoyes";
        public const string Brouillons = "brouillons";

        /// <summary>Un dossier connu, sinon la réception.</summary>
        public static string Normaliser(string? dossier) => dossier?.Trim().ToLowerInvariant() switch
        {
            Envoyes => Envoyes,
            Brouillons => Brouillons,
            _ => Reception,
        };
    }

    /// <summary>Une ligne de la liste : ce qu'on lit sans ouvrir le message.</summary>
    public class MessageRecu
    {
        /// <summary>
        /// L'identifiant IMAP du message dans son dossier.
        ///
        /// STABLE TANT QUE LE DOSSIER NE CHANGE PAS DE GÉNÉRATION, ce qui
        /// n'arrive qu'à une reconstruction de la boîte côté hébergeur. C'est
        /// la clé sur laquelle l'écran demande le détail et poste sa réponse.
        /// </summary>
        public uint Identifiant { get; set; }

        public string? DeNom { get; set; }

        public string? DeAdresse { get; set; }

        /// <summary>
        /// L'adresse « Répondre à » quand elle diffère de l'expéditeur (18/10/2026) :
        /// un message du formulaire de contact part de no-reply@mimia.fr, la
        /// réponse va au visiteur.
        /// </summary>
        public string? RepondreA { get; set; }

        /// <summary>Le premier destinataire : ce qu'on montre dans « Messages envoyés ».</summary>
        public string? ANom { get; set; }

        public string? AAdresse { get; set; }

        public string? Sujet { get; set; }

        public DateTime Date { get; set; }

        public bool Lu { get; set; }

        /// <summary>Vrai si une réponse est déjà partie — drapeau IMAP `\Answered`.</summary>
        public bool Repondu { get; set; }

        /// <summary>Les premiers mots, pour reconnaître un message dans la liste.</summary>
        public string? Extrait { get; set; }

        public bool AvecPiecesJointes { get; set; }
    }

    /// <summary>Un fichier reçu avec un message.</summary>
    public class PieceJointeRecue
    {
        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public long Taille { get; set; }
    }

    /// <summary>Le message ouvert, avec son corps.</summary>
    public class MessageDetail : MessageRecu
    {
        /// <summary>
        /// Le corps en HTML, NETTOYÉ.
        ///
        /// Un courriel entrant est du contenu hostile par défaut : n'importe
        /// qui connaissant l'adresse peut y mettre ce qu'il veut, et
        /// l'administration a tous les droits. Les scripts sont retirés ici, et
        /// l'écran l'affiche en plus dans un cadre isolé — deux barrières, dont
        /// aucune ne suffirait seule.
        /// </summary>
        public string? Html { get; set; }

        /// <summary>La version texte, quand le message en propose une.</summary>
        public string? Texte { get; set; }

        /// <summary>Vrai si des images distantes ont été neutralisées.</summary>
        public bool ImagesBloquees { get; set; }

        public List<PieceJointeRecue> PiecesJointes { get; set; } = [];
    }

    /// <summary>
    /// Le contenu de la boîte, ou la raison pour laquelle on n'a pas pu la lire.
    /// </summary>
    /// <param name="Erreur">
    /// Null quand tout s'est bien passé. Renseignée, elle contient un message
    /// destiné à l'exploitant — pas au parent : personne d'autre qu'un
    /// administrateur ne voit cet écran.
    /// </param>
    /// <param name="Total">Le nombre de messages du dossier, toutes pages confondues.</param>
    /// <param name="NonLus">Les non lus du dossier entier (réception seulement).</param>
    /// <summary>
    /// Les compteurs des dossiers de la boîte (17/10/2026) : ce que portent les
    /// pastilles des onglets, lus en une connexion (STATUS, sans rien rapatrier).
    /// </summary>
    public record CompteursBoite(int Reception, int NonLus, int Envoyes, string? Erreur = null);

    public record ResultatBoite(IReadOnlyList<MessageRecu> Messages, string? Erreur = null, int Total = 0, int NonLus = 0);

    /// <summary>
    /// Ce qu'un envoi a produit, tel que le serveur l'a dit.
    ///
    /// POURQUOI ON REMONTE ÇA JUSQU'À L'ÉCRAN, ET NON SEULEMENT DANS LES LOGS
    /// ---------------------------------------------------------------------
    /// « Réponse envoyée » n'est pas une information quand le message n'arrive
    /// pas : on a cherché pendant une heure si le code envoyait vraiment,
    /// alors que le serveur SMTP répondait et qu'il suffisait de lire ce qu'il
    /// disait. L'accusé porte souvent un identifiant de file d'attente — le
    /// seul élément qu'un hébergeur accepte d'examiner.
    /// </summary>
    /// <param name="Destinataire">L'adresse RÉELLEMENT visée, pas celle qu'on croit.</param>
    /// <param name="Accuse">La dernière réponse du serveur SMTP.</param>
    /// <param name="CopieDeposee">
    /// Faux si la copie dans « Envoyés » a échoué — le message est parti quand
    /// même, seule la trace manque.
    /// </param>
    public record ResultatEnvoi(
        bool Envoye,
        string? Destinataire = null,
        string? Accuse = null,
        bool CopieDeposee = false,
        string? Detail = null);

    /// <summary>
    /// La boîte d'un administrateur (17/10/2026) : son adresse chez LWS, son mot
    /// de passe (déchiffré le temps d'une requête), le nom affiché à l'envoi.
    /// </summary>
    public record CompteMessagerie(string Adresse, string MotDePasse, string? Nom);

    public interface IMessagerieService
    {
        /// <summary>
        /// Le même service, branché sur la boîte d'un administrateur plutôt que
        /// sur celle du support (17/10/2026) : lecture chez LWS avec SES
        /// identifiants, envoi par le même service, à SON nom et SON adresse.
        /// </summary>
        IMessagerieService Pour(CompteMessagerie compte);

        /// <summary>Ouvre puis referme la boîte : null si l'adresse et le mot de passe sont bons, sinon la raison.</summary>
        Task<string?> VerifierAsync(CancellationToken ct = default);

        /// <summary>Faux si la boîte n'est pas configurée : l'écran le dit au lieu d'échouer.</summary>
        bool Disponible { get; }

        /// <summary>
        /// Les derniers messages reçus, du plus récent au plus ancien.
        ///
        /// REND AUSSI L'ERREUR, ET C'EST TOUT LE POINT. En rendant une simple
        /// liste, une boîte injoignable était indistinguable d'une boîte vide :
        /// l'écran affichait « aucun message » sur une panne d'authentification,
        /// et on cherchait pourquoi les courriels n'arrivaient plus alors que
        /// c'est la lecture qui échouait.
        /// </summary>
        /// <param name="dossier">Voir `DossiersMessagerie` : réception ou envoyés.</param>
        /// <param name="page">À partir de 1 : la page 1 porte les plus récents.</param>
        Task<ResultatBoite> ListerAsync(
            string? dossier = null, int page = 1, int taille = 10, CancellationToken ct = default);

        /// <summary>
        /// Un message avec son corps. Null s'il n'existe plus — supprimé depuis
        /// un autre client entre l'affichage de la liste et le clic.
        /// </summary>
        /// <param name="afficherImages">
        /// Les images distantes restent bloquées par défaut : ce sont
        /// principalement des traceurs, qui disent à l'expéditeur que le
        /// message a été ouvert, quand il a été ouvert, et depuis quelle
        /// adresse. On ne les charge que si on le demande.
        /// </param>
        Task<MessageDetail?> LireAsync(
            uint identifiant, bool afficherImages = false, CancellationToken ct = default, string? dossier = null);

        /// <summary>
        /// Télécharge une pièce jointe d'un message reçu.
        /// </summary>
        Task<(byte[] Donnees, string TypeMime, string NomFichier)?> PieceJointeAsync(
            uint identifiant, string nomFichier, CancellationToken ct = default, string? dossier = null);

        /// <summary>
        /// Répond à un message, dans le fil.
        ///
        /// LES ENTÊTES DE FIL NE SONT PAS DÉCORATIVES. Sans `In-Reply-To` et
        /// `References`, la réponse arrive détachée dans le client du parent :
        /// il reçoit un message isolé qui commence par « Bonjour » sans savoir
        /// de quoi il parle, et le fil se coupe en deux.
        /// </summary>
        /// <param name="composer">
        /// Fabrique le corps de la réponse à partir du message d'origine.
        ///
        /// POURQUOI UNE FONCTION ET NON UN TEXTE DÉJÀ COMPOSÉ. Le corps a
        /// besoin du message d'origine — son objet, sa date, son expéditeur
        /// pour la salutation, son texte pour la citation. Le contrôleur le
        /// lisait donc AVANT d'appeler cette méthode, qui le relisait ensuite :
        /// deux connexions IMAP successives par réponse, plus une troisième
        /// pour le SMTP.
        ///
        /// Les hébergeurs mutualisés bornent les connexions simultanées par
        /// compte — souvent trois ou quatre. La seconde réponse envoyée
        /// coup sur coup tapait dans la limite et échouait sans rien dire au
        /// parent, ni à l'expéditeur.
        ///
        /// En passant la composition ici, on lit une fois et on envoie.
        /// </param>
        /// <returns>
        /// Le compte rendu de l'envoi. `Envoye` faux si le message d'origine
        /// est introuvable ou si le serveur a refusé.
        /// </returns>
        Task<ResultatEnvoi> RepondreAsync(
            uint identifiant,
            Func<MessageDetail, Task<string>> composer,
            CancellationToken ct = default);

        /// <summary>Marque un message lu ou non lu.</summary>
        Task MarquerLuAsync(uint identifiant, bool lu, CancellationToken ct = default, string? dossier = null);

        /// <summary>Le nombre de messages reçus, de non lus et d'envoyés, en une connexion.</summary>
        Task<CompteursBoite> CompterAsync(CancellationToken ct = default);

        /// <summary>
        /// SUPPRIMER (Camara, le 17/10/2026) : les messages vont à la corbeille de
        /// la boîte — rattrapables depuis le webmail LWS — et n'en sont effacés
        /// pour de bon que s'il n'y en a pas. Rend le nombre de messages retirés.
        /// </summary>
        Task<int> SupprimerAsync(
            IReadOnlyCollection<uint> identifiants, string? dossier = null, CancellationToken ct = default);

        /// <summary>
        /// Dépose dans « Envoyés » la copie d'un message parti par le service
        /// d'envoi (« Écrire à un parent », un template) : sans elle, il
        /// n'apparaîtrait nulle part dans la boîte. Tolérante à l'échec : le
        /// message est déjà parti.
        /// </summary>
        Task<bool> DeposerCopieAsync(
            string destinataire,
            string sujet,
            string html,
            IReadOnlyList<PieceMail>? imagesIntegrees = null,
            IReadOnlyList<PieceMail>? piecesJointes = null,
            CancellationToken ct = default);
    }
}
