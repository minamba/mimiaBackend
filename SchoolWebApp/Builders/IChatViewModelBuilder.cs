using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.ViewModels;

namespace SchoolWebApp.Api.Builders
{
    public interface IChatViewModelBuilder
    {
        /// <summary>Null si l'élève n'appartient pas au parent authentifié, ou si la matière est inconnue.</summary>
        Task<ConversationViewModel?> CreerConversationAsync(CreerConversationRequest model);

        Task<IEnumerable<ConversationViewModel>?> GetConversationsAsync(int eleveId);

        Task<IEnumerable<MessageViewModel>?> GetMessagesAsync(int conversationId);

        /// <summary>
        /// Diffuse la réponse de l'agent. Enregistre le message de l'élève avant
        /// de commencer, et celui de l'agent une fois le flux terminé.
        /// Lève <see cref="UnauthorizedAccessException"/> si la conversation
        /// n'appartient pas au parent authentifié.
        /// </summary>
        /// <param name="secondesRestantes">
        /// Temps restant dans la séance, tel que le navigateur le compte. Sans
        /// lui, l'agent ne peut que le deviner — et il conclut trop tôt.
        /// </param>
        /// <param name="vitesseEcoute">
        /// La vitesse de lecture choisie par l'élève, telle que le navigateur
        /// la tient. Sans elle, le professeur ne peut ni descendre d'un cran,
        /// ni dire qu'il lit déjà au plus lent.
        /// </param>
        IAsyncEnumerable<string> StreamReponseAsync(
            int conversationId, string contenu, int? pieceJointeId,
            int? secondesRestantes, string? vitesseEcoute, CancellationToken ct);

        /// <summary>
        /// Fait parler l'agent en premier, à l'ouverture de la conversation.
        /// Aucun message élève n'est enregistré — seule la parole de l'agent l'est.
        /// </summary>
        /// <param name="dureeChoisieMinutes">
        /// La durée choisie par l'élève pour cette séance (15/25/35/45 min),
        /// telle que le navigateur la connaît — le serveur ne la devine pas.
        /// Posée sur la conversation avant même le premier mot du professeur,
        /// pour qu'elle rejoigne le compte rendu de cette séance quand il
        /// s'écrit. Null si absente ou hors des valeurs attendues.
        /// </param>
        /// <param name="controleId">
        /// Le contrôle que l'élève vient préparer, quand il a ouvert la séance
        /// par « Préparer ce contrôle ». Le professeur ouvre alors dessus au
        /// lieu de le proposer. Ignoré sans erreur s'il ne désigne pas un
        /// contrôle de cet élève dans cette matière — un lien recopié ou
        /// périmé ne doit pas faire échouer un accueil.
        /// </param>
        /// <param name="mode">
        /// D'où vient l'élève — voir `ModesSeance`. Posé sur la conversation
        /// avant le premier mot, et vérifié : un contrôle qui n'est pas à cet
        /// élève, une épreuve qui ne le concerne pas ramènent au cours normal.
        /// </param>
        /// <param name="epreuveCode">L'épreuve préparée, en mode « examen ».</param>
        IAsyncEnumerable<string> StreamAccueilAsync(
            int conversationId, int? dureeChoisieMinutes, int? controleId, string? mode, string? epreuveCode,
            CancellationToken ct);

        /// <summary>
        /// Prise de parole commandée par l'horloge de séance : les cinq
        /// dernières minutes, puis la clôture.
        /// </summary>
        IAsyncEnumerable<string> StreamAnnonceAsync(
            int conversationId, Services.TypeAccueil annonce, CancellationToken ct);

        /// <summary>
        /// Fait conclure une séance quittée avant l'heure (compte rendu,
        /// fiche, évaluation prévue) — appelée hors requête HTTP, par le
        /// worker qui traite <see cref="Services.IFileConclusion"/>. Ne fait
        /// rien si l'élève n'a rien dit depuis le dernier compte rendu.
        /// </summary>
        Task ConclureDepartAnticipeAsync(int conversationId, CancellationToken ct = default);

        /// <summary>
        /// L'élève de cette conversation a-t-il encore du quota ?
        ///
        /// Vérifié AVANT d'ouvrir le flux : découvrir le refus au milieu de la
        /// génération obligerait à couper une phrase déjà commencée, et le tour
        /// serait facturé pour rien.
        /// </summary>
        /// <summary>
        /// Le code matière d une conversation, ou null si elle n existe pas.
        ///
        /// Sert à orienter le vocabulaire du transcripteur : « pluriel » en
        /// français, « hypoténuse » en mathématiques. Sans lui, il arbitre
        /// entre des sons proches sans savoir de quel cours il s agit.
        /// </summary>
        Task<string?> GetMatiereCodeAsync(int conversationId);

        Task<Domain.Models.VerdictQuota> VerifierQuotaAsync(
            int conversationId, CancellationToken ct = default);

        /// <summary>
        /// Note que l'élève a quitté le cours, pour qu'il soit accueilli à son
        /// retour sans qu'on ait à le déduire d'un délai de silence.
        /// </summary>
        Task MarquerSortieAsync(int conversationId);

        /// <summary>Sortie signalee par une page qui se ferme, sans identite.</summary>
        Task MarquerFermetureAsync(int conversationId);

        // ------------------------------------------------------------------
        // Pièces jointes
        // ------------------------------------------------------------------

        /// <summary>
        /// Enregistre le document que l'élève veut montrer, après validation.
        /// La pièce reste orpheline jusqu'à ce qu'un message l'emporte : c'est
        /// l'envoi du tour qui l'accroche.
        ///
        /// Rend le refus plutôt que de lever : « ton PDF fait 40 pages » n'est
        /// pas une anomalie du programme, c'est une réponse à afficher.
        /// </summary>
        Task<ResultatPieceJointe> AjouterPieceJointeAsync(
            int conversationId, string? nomFichier, string? typeAnnonce,
            byte[] donnees, CancellationToken ct = default);

        /// <summary>
        /// Dépose un document envoyé par le téléphone, via un QR code du
        /// scanner. SANS contrôle de propriété, et c'est voulu : le téléphone
        /// n'est connecté à rien. L'APPELANT a déjà prouvé le droit en
        /// réservant un jeton valide pour CETTE conversation — voir
        /// `JetonsScanMobile`. Ne jamais appeler sans cette réservation.
        /// </summary>
        Task<ResultatPieceJointe> AjouterPieceJointeParScanAsync(
            int conversationId, string? nomFichier, string? typeAnnonce,
            byte[] donnees, CancellationToken ct = default);

        /// <summary>Le professeur et la matière d'une conversation du compte connecté, pour le QR code. Null sinon.</summary>
        Task<EnTeteScan?> ResoudreEnTeteScanAsync(int conversationId);

        /// <summary>L'aperçu d'un document de cette conversation du compte connecté. Null sinon.</summary>
        Task<ViewModels.PieceJointeViewModel?> GetApercuPieceAsync(
            int conversationId, int pieceJointeId, CancellationToken ct = default);

        /// <summary>
        /// Enregistre « énoncé et copie séparés ? » SANS tour de conversation.
        /// Faux si la conversation ou le contrôle n'est pas à ce compte, dans
        /// la matière de la conversation.
        /// </summary>
        Task<bool> PoserChoixCopieAsync(
            int conversationId, int controleId, bool separee, CancellationToken ct = default);

        /// <summary>
        /// Le choix déjà enregistré pour ce contrôle — pour que la carte ne
        /// repose pas la question après un rechargement. `Trouve` est faux si
        /// la conversation ou le contrôle n'est pas à ce compte.
        /// </summary>
        Task<(bool Trouve, bool? Separee)> GetChoixCopieAsync(
            int conversationId, int controleId, CancellationToken ct = default);

        /// <summary>
        /// Le contenu d'un document, pour l'afficher. Null si la pièce
        /// n'existe pas ou n'appartient pas au parent authentifié.
        /// </summary>
        Task<ContenuPieceJointe?> GetPieceJointeAsync(int pieceJointeId, CancellationToken ct = default);
    }

    /// <summary>Refus motivé, ou pièce acceptée.</summary>
    /// <param name="Piece">Null si refusée.</param>
    /// <param name="Motif">Message destiné à l'élève. Null si acceptée.</param>
    /// <param name="Autorise">
    /// Faux quand la conversation n'appartient pas au compte : le contrôleur
    /// doit alors répondre 404 et non 400 — on ne confirme pas l'existence
    /// d'une conversation qui n'est pas la sienne.
    /// </param>
    public record ResultatPieceJointe(
        ViewModels.PieceJointeViewModel? Piece, string? Motif, bool Autorise = true);

    public record ContenuPieceJointe(byte[] Donnees, string TypeMime, string NomFichier);

    public record EnTeteScan(string? ProfPrenom, string? Matiere);
}
