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

        /// <summary>
        /// Pose la durée choisie (15/25/35/45 min) pour la séance qui
        /// commence. Silencieux si la conversation n'existe pas : c'est un
        /// à-côté de l'accueil, jamais une condition de son bon déroulement.
        /// </summary>
        Task DefinirDureeChoisieAsync(int conversationId, int dureeMinutes, CancellationToken ct = default);

        /// <summary>D'où vient l'élève pour la séance qui commence — voir `ModesSeance`.</summary>
        Task DefinirModeSeanceAsync(
            int conversationId, string mode, int? controleId, string? epreuveCode, CancellationToken ct = default);

        /// <summary>
        /// Vrai s'il existe au moins un message élève depuis le dernier
        /// compte rendu de cette conversation (ou depuis sa création, s'il
        /// n'y en a pas encore). Sert à décider si un départ anticipé mérite
        /// une conclusion, ou si rien ne s'est passé.
        /// </summary>
        Task<bool> ADuTravailNonConcluAsync(int conversationId, CancellationToken ct = default);

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

        /// <summary>
        /// Les notions des classes d'avant qui ne sont pas acquises, dans une
        /// matière — celles qu'un jeu d'une classe inférieure peut consolider.
        /// « Pas acquise » et non « lacune » : à 65 %, une notion n'est pas une
        /// lacune, mais elle mérite qu'on la rejoue.
        /// </summary>
        Task<IEnumerable<MaitriseCompetence>> GetFragilesEnAmontAsync(
            int eleveId, int matiereId, int rangMax, int limite);

        /// <summary>
        /// Les notions du programme officiel, pour le niveau et la matière de
        /// la séance.
        ///
        /// SERT AU PROFESSEUR QUAND IL NOMME UNE FICHE. Il ne recevait
        /// jusqu'ici que les notions DÉJÀ mesurées chez cet élève : sur une
        /// notion neuve, il n'avait aucun libellé officiel sous les yeux et
        /// en inventait un. D'où, dans les seize premières fiches, un mélange
        /// de « Utiliser le théorème de Thalès » (le programme) et de
        /// « Division décimale » (un titre de chapitre) — deux styles pour un
        /// même objet, et des fiches qui ne se raccrochent plus à ce qui est
        /// mesuré.
        /// </summary>
        Task<IEnumerable<CompetenceCandidate>> GetNotionsDuProgrammeAsync(
            int matiereId, int niveauScolaireId, CancellationToken ct = default);
    }
}
