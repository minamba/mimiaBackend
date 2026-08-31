using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;

        public ConversationService(IConversationRepository conversationRepository)
        {
            _conversationRepository = conversationRepository
                ?? throw new ArgumentNullException(nameof(conversationRepository));
        }

        public Task<IEnumerable<Conversation>> GetConversationsByEleveAsync(int eleveId) =>
            _conversationRepository.GetConversationsByEleveAsync(eleveId);

        public Task<Conversation?> GetConversationByIdAsync(int id) =>
            _conversationRepository.GetConversationByIdAsync(id);

        public async Task<Conversation?> GetConversationForEleveAsync(int conversationId, int eleveId)
        {
            var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
            return conversation is null || conversation.EleveId != eleveId ? null : conversation;
        }

        public Task<Conversation> AddConversationAsync(Conversation model) =>
            _conversationRepository.AddConversationAsync(model);

        public Task<IEnumerable<Message>> GetMessagesAsync(int conversationId, int limite) =>
            _conversationRepository.GetMessagesAsync(conversationId, limite);

        public Task<IEnumerable<Message>> GetFenetreStableAsync(
            int conversationId, int plancher, int pas) =>
            _conversationRepository.GetFenetreStableAsync(conversationId, plancher, pas);

        public Task<Message> AddMessageAsync(Message model) =>
            _conversationRepository.AddMessageAsync(model);

        public Task TouchConversationAsync(int conversationId, int eleveId) =>
            _conversationRepository.TouchConversationAsync(conversationId, eleveId);

        public Task MarquerSortieAsync(int conversationId, int eleveId) =>
            _conversationRepository.MarquerSortieAsync(conversationId, eleveId);

        public Task MarquerFermetureAsync(int conversationId) =>
            _conversationRepository.MarquerFermetureAsync(conversationId);

        public Task<bool> ConsommerSortieAsync(int conversationId, CancellationToken ct = default) =>
            _conversationRepository.ConsommerSortieAsync(conversationId, ct);

        public Task<int> AjouterPieceJointeAsync(PieceJointe piece, CancellationToken ct = default) =>
            _conversationRepository.AjouterPieceJointeAsync(piece, ct);

        public Task<PieceJointe?> GetPieceJointeAsync(int id, CancellationToken ct = default) =>
            _conversationRepository.GetPieceJointeAsync(id, ct);

        public Task<PieceJointe?> GetPieceJointeAvecDonneesAsync(int id, CancellationToken ct = default) =>
            _conversationRepository.GetPieceJointeAvecDonneesAsync(id, ct);

        public Task AttacherAuMessageAsync(int pieceJointeId, int messageId, CancellationToken ct = default) =>
            _conversationRepository.AttacherAuMessageAsync(pieceJointeId, messageId, ct);

        public Task<IEnumerable<PieceJointe>> GetPiecesDesMessagesAsync(IEnumerable<int> messageIds, CancellationToken ct = default) =>
            _conversationRepository.GetPiecesDesMessagesAsync(messageIds, ct);

        public Task<IEnumerable<PieceJointe>> GetMetadonneesDesMessagesAsync(IEnumerable<int> messageIds, CancellationToken ct = default) =>
            _conversationRepository.GetMetadonneesDesMessagesAsync(messageIds, ct);

        public Task<int> PurgerPiecesOrphelinesAsync(TimeSpan anciennete, CancellationToken ct = default) =>
            _conversationRepository.PurgerPiecesOrphelinesAsync(anciennete, ct);

        public Task<IEnumerable<PieceJointe>> GetATranscrireAsync(int limite, CancellationToken ct = default) =>
            _conversationRepository.GetATranscrireAsync(limite, ct);

        public Task EnregistrerTranscriptionAsync(int pieceJointeId, string texte, CancellationToken ct = default) =>
            _conversationRepository.EnregistrerTranscriptionAsync(pieceJointeId, texte, ct);

        public Task<(int Documents, long Octets)> PurgerOctetsAsync(TimeSpan anciennete, int limite, CancellationToken ct = default) =>
            _conversationRepository.PurgerOctetsAsync(anciennete, limite, ct);
    }

    public class MaitriseService : IMaitriseService
    {
        private const double SeuilLacune = 0.6;
        private const double SeuilAcquis = 0.8;

        private readonly IMaitriseRepository _maitriseRepository;

        public MaitriseService(IMaitriseRepository maitriseRepository)
        {
            _maitriseRepository = maitriseRepository ?? throw new ArgumentNullException(nameof(maitriseRepository));
        }

        public Task<IEnumerable<MaitriseCompetence>> GetLacunesAsync(int eleveId, int? matiereId, int limite) =>
            _maitriseRepository.GetLacunesAsync(eleveId, matiereId, SeuilLacune, limite);

        public Task<IEnumerable<MaitriseCompetence>> GetAcquisesAsync(int eleveId, int? matiereId, int limite) =>
            _maitriseRepository.GetAcquisesAsync(eleveId, matiereId, SeuilAcquis, limite);

        public Task<IEnumerable<MaitriseCompetence>> GetARevoirAsync(int eleveId, int? matiereId, int limite) =>
            _maitriseRepository.GetARevoirAsync(eleveId, matiereId, limite);
    }
}
