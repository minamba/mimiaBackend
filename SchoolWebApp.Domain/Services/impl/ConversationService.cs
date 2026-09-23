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

        public Task DefinirDureeChoisieAsync(int conversationId, int dureeMinutes, CancellationToken ct = default) =>
            _conversationRepository.DefinirDureeChoisieAsync(conversationId, dureeMinutes, ct);

        public Task DefinirModeSeanceAsync(
            int conversationId, string mode, int? controleId, string? epreuveCode, CancellationToken ct = default) =>
            _conversationRepository.DefinirModeSeanceAsync(conversationId, mode, controleId, epreuveCode, ct);

        public Task<bool> ADuTravailNonConcluAsync(int conversationId, CancellationToken ct = default) =>
            _conversationRepository.ADuTravailNonConcluAsync(conversationId, ct);

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
        // Le professeur doit voir les memes fragilites que le parent : ce
        // seuil valait 0,6 face au 0,75 de la fiche, et l un des deux se
        // trompait forcement.
        private const double SeuilLacune = SeuilsMaitrise.Fragile;
        private const double SeuilAcquis = SeuilsMaitrise.Acquis;

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

        // Le seuil de l'ACQUIS, pas celui de la lacune : c'est tout ce qui n'est
        // pas tenu qui peut se consolider par un jeu.
        public Task<IEnumerable<MaitriseCompetence>> GetFragilesEnAmontAsync(
            int eleveId, int matiereId, int rangMax, int limite) =>
            _maitriseRepository.GetFragilesEnAmontAsync(eleveId, matiereId, rangMax, SeuilAcquis, limite);

        // SA CLASSE EXACTE, PAS UNE FENETRE DE NIVEAUX.
        //
        // L observateur remonte cinq niveaux en amont — un blocage en 6e vient
        // souvent du CM1, et il doit pouvoir le dire. Ici l objet est different :
        // c est la liste dans laquelle le professeur choisit le NOM d une fiche.
        // Y melanger cinq niveaux la rendrait illisible et l inviterait a titrer
        // une fiche de 6e avec un libelle de CM1.
        public Task<IEnumerable<CompetenceCandidate>> GetNotionsDuProgrammeAsync(
            int matiereId, int niveauScolaireId, CancellationToken ct = default) =>
            _maitriseRepository.GetNotionsDuNiveauAsync(matiereId, niveauScolaireId, ct);
    }
}
