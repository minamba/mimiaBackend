using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IConversationRepository
    {
        Task<IEnumerable<Conversation>> GetConversationsByEleveAsync(int eleveId);

        Task<Conversation?> GetConversationByIdAsync(int id);

        Task<Conversation> AddConversationAsync(Conversation model);

        /// <summary>Messages d'une conversation, du plus ancien au plus récent.</summary>
        Task<IEnumerable<Message>> GetMessagesAsync(int conversationId, int limite);

        /// <summary>
        /// L'historique pour le modèle, sur une fenêtre qui ne glisse que par
        /// paliers — sans quoi la mise en cache du préfixe ne sert à rien.
        /// </summary>
        Task<IEnumerable<Message>> GetFenetreStableAsync(int conversationId, int plancher, int pas);

        Task<Message> AddMessageAsync(Message model);

        /// <summary>Met à jour l'horodatage du dernier message et la dernière activité de l'élève.</summary>
        Task TouchConversationAsync(int conversationId, int eleveId);

        /// <summary>
        /// Séances terminées depuis <paramref name="inactivite"/> et dont les
        /// échanges n'ont pas encore été analysés.
        ///
        /// On attend que la séance soit finie plutôt que d'analyser à chaque
        /// tour : un échange isolé ne dit pas si l'élève a compris, la fin de
        /// séance si.
        /// </summary>
        Task<IEnumerable<SeanceAObserver>> GetSeancesAObserverAsync(
            TimeSpan inactivite, int limite, CancellationToken ct = default);

        /// <summary>
        /// Une séance précise, quelle que soit son inactivité. Sert quand
        /// l'élève vient de quitter le cours : on sait qu'elle est terminée,
        /// inutile d'attendre un délai de silence.
        /// </summary>
        Task<SeanceAObserver?> GetSeanceAObserverAsync(int conversationId, CancellationToken ct = default);

        /// <summary>Repousse le marqueur d'observation d'une conversation.</summary>
        Task MarquerObserveeAsync(int conversationId, DateTime jusqua, CancellationToken ct = default);

        /// <summary>
        /// Note que l'élève a quitté le cours. Le professeur l'accueillera à son
        /// retour sans avoir à deviner, à partir d'un délai, s'il est parti.
        /// </summary>
        Task MarquerSortieAsync(int conversationId, int eleveId);

        /// <summary>Sortie signalee par une page qui se ferme, sans identite.</summary>
        Task MarquerFermetureAsync(int conversationId);

        /// <summary>
        /// Consomme le marqueur de sortie et dit s'il existait. Referme la
        /// fenêtre pendant laquelle deux requêtes concurrentes croiraient
        /// toutes deux devoir accueillir.
        /// </summary>
        Task<bool> ConsommerSortieAsync(int conversationId, CancellationToken ct = default);

        // ------------------------------------------------------------------
        // Pièces jointes
        // ------------------------------------------------------------------

        /// <summary>Enregistre un document et rend son identifiant.</summary>
        Task<int> AjouterPieceJointeAsync(PieceJointe piece, CancellationToken ct = default);

        /// <summary>
        /// Les métadonnées d'une pièce, sans les octets. Sert au contrôle
        /// d'accès et à l'accrochage au message.
        /// </summary>
        Task<PieceJointe?> GetPieceJointeAsync(int id, CancellationToken ct = default);

        /// <summary>Le contenu d'une pièce, octets compris.</summary>
        Task<PieceJointe?> GetPieceJointeAvecDonneesAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Accroche une pièce au message qui vient d'être écrit. C'est ce qui
        /// la fait entrer dans l'historique, et donc dans la mémoire du
        /// professeur pour les tours suivants.
        /// </summary>
        Task AttacherAuMessageAsync(int pieceJointeId, int messageId, CancellationToken ct = default);

        /// <summary>
        /// Les documents attachés à ces messages, octets compris. Un seul
        /// aller-retour : la fenêtre d'historique peut en contenir plusieurs.
        /// </summary>
        Task<IEnumerable<PieceJointe>> GetPiecesDesMessagesAsync(
            IEnumerable<int> messageIds, CancellationToken ct = default);

        /// <summary>
        /// Les métadonnées des pièces de ces messages, sans les octets — pour
        /// afficher les vignettes dans la conversation.
        /// </summary>
        Task<IEnumerable<PieceJointe>> GetMetadonneesDesMessagesAsync(
            IEnumerable<int> messageIds, CancellationToken ct = default);

        /// <summary>
        /// Efface les pièces jointes jamais envoyées. L'élève en choisit une,
        /// change d'avis, ferme l'onglet : les octets resteraient en base sans
        /// message auquel se rattacher, et rien ne les emporterait jamais.
        /// </summary>
        Task<int> PurgerPiecesOrphelinesAsync(TimeSpan anciennete, CancellationToken ct = default);

        /// <summary>
        /// Les documents en attente de transcription : octets encore présents,
        /// texte pas encore extrait. Rendus du plus ancien au plus récent.
        /// </summary>
        Task<IEnumerable<PieceJointe>> GetATranscrireAsync(int limite, CancellationToken ct = default);

        /// <summary>Enregistre le texte extrait d'un document.</summary>
        Task EnregistrerTranscriptionAsync(int pieceJointeId, string texte, CancellationToken ct = default);

        /// <summary>
        /// Efface les octets des documents assez vieux et DÉJÀ transcrits, en
        /// gardant la ligne et son texte. Rend le nombre de documents allégés
        /// et le nombre d'octets récupérés.
        /// </summary>
        Task<(int Documents, long Octets)> PurgerOctetsAsync(
            TimeSpan anciennete, int limite, CancellationToken ct = default);
    }
}
