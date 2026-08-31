namespace SchoolWebApp.Domain.Emails
{
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
    public record ResultatBoite(IReadOnlyList<MessageRecu> Messages, string? Erreur = null);

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

    public interface IMessagerieService
    {
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
        Task<ResultatBoite> ListerAsync(CancellationToken ct = default);

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
            uint identifiant, bool afficherImages = false, CancellationToken ct = default);

        /// <summary>
        /// Télécharge une pièce jointe d'un message reçu.
        /// </summary>
        Task<(byte[] Donnees, string TypeMime, string NomFichier)?> PieceJointeAsync(
            uint identifiant, string nomFichier, CancellationToken ct = default);

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
        Task MarquerLuAsync(uint identifiant, bool lu, CancellationToken ct = default);
    }
}
