using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface IConversationService
    {
        Task<IEnumerable<Conversation>> GetConversationsByEleveAsync(int eleveId);

        Task<Conversation?> GetConversationByIdAsync(int id);

        /// <summary>
        /// Null si la conversation n'existe pas ou n'appartient pas à cet élève.
        /// Même principe que pour les élèves : toute lecture issue d'une requête
        /// authentifiée passe par ici.
        /// </summary>
        Task<Conversation?> GetConversationForEleveAsync(int conversationId, int eleveId);

        Task<Conversation> AddConversationAsync(Conversation model);

        Task<IEnumerable<Message>> GetMessagesAsync(int conversationId, int limite);

        /// <summary>
        /// L'historique pour le modèle, sur une fenêtre qui ne glisse que par
        /// paliers — sans quoi la mise en cache du préfixe ne sert à rien.
        /// </summary>
        Task<IEnumerable<Message>> GetFenetreStableAsync(int conversationId, int plancher, int pas);

        Task<Message> AddMessageAsync(Message model);

        Task TouchConversationAsync(int conversationId, int eleveId);

        /// <summary>Note la sortie explicite de l'élève du cours.</summary>
        Task MarquerSortieAsync(int conversationId, int eleveId);

        /// <summary>Sortie signalee par une page qui se ferme, sans identite.</summary>
        Task MarquerFermetureAsync(int conversationId);

        /// <summary>Consomme le marqueur de sortie et dit s'il existait.</summary>
        Task<bool> ConsommerSortieAsync(int conversationId, CancellationToken ct = default);

        // Pièces jointes — le document que l'élève montre au professeur.
        Task<int> AjouterPieceJointeAsync(PieceJointe piece, CancellationToken ct = default);
        Task<PieceJointe?> GetPieceJointeAsync(int id, CancellationToken ct = default);
        Task<PieceJointe?> GetPieceJointeAvecDonneesAsync(int id, CancellationToken ct = default);
        Task AttacherAuMessageAsync(int pieceJointeId, int messageId, CancellationToken ct = default);
        Task<IEnumerable<PieceJointe>> GetPiecesDesMessagesAsync(IEnumerable<int> messageIds, CancellationToken ct = default);
        Task<IEnumerable<PieceJointe>> GetMetadonneesDesMessagesAsync(IEnumerable<int> messageIds, CancellationToken ct = default);
        Task<int> PurgerPiecesOrphelinesAsync(TimeSpan anciennete, CancellationToken ct = default);
        Task<IEnumerable<PieceJointe>> GetATranscrireAsync(int limite, CancellationToken ct = default);
        Task EnregistrerTranscriptionAsync(int pieceJointeId, string texte, CancellationToken ct = default);
        Task<(int Documents, long Octets)> PurgerOctetsAsync(TimeSpan anciennete, int limite, CancellationToken ct = default);
    }

    public interface IMaitriseService
    {
        Task<IEnumerable<MaitriseCompetence>> GetLacunesAsync(int eleveId, int? matiereId, int limite);

        Task<IEnumerable<MaitriseCompetence>> GetAcquisesAsync(int eleveId, int? matiereId, int limite);

        /// <summary>Notions dues en révision espacée, la plus fragile en tête.</summary>
        Task<IEnumerable<MaitriseCompetence>> GetARevoirAsync(int eleveId, int? matiereId, int limite);
    }
}
