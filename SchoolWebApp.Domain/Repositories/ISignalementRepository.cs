using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface ISignalementRepository
    {
        Task<Signalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description,
            CancellationToken ct = default);

        /// <summary>Le signalement d'un administrateur, relié ou non à une ligne de jeu de test (05/11/2026).</summary>
        Task<Signalement> CreerAdminAsync(
            int parentId, string categorie, string description, string? competence, int? ligneDeTestId,
            CancellationToken ct = default);

        /// <summary>Les signalements des parents, le plus récent d'abord — pour l'onglet « Signalements ».</summary>
        Task<IEnumerable<Signalement>> GetTousAsync(CancellationToken ct = default);

        /// <summary>Ceux des administrateurs, le plus récent d'abord — pour l'onglet « Signalements Admin ».</summary>
        Task<IEnumerable<Signalement>> GetAdminAsync(CancellationToken ct = default);

        Task<Signalement?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Null si le signalement n'existe pas. <paramref name="competence"/> : celle d'un signalement admin ;
        /// null la laisse telle quelle.
        /// </summary>
        Task<Signalement?> ModifierAsync(
            int id, string categorie, string description, string etat, string? competence = null, CancellationToken ct = default);

        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);

        /// <summary>Du plus ancien au plus récent : ils se lisent comme un fil.</summary>
        Task<IEnumerable<SignalementCommentaire>> GetCommentairesAsync(int signalementId, CancellationToken ct = default);

        /// <summary>Null si le signalement n'existe pas.</summary>
        Task<SignalementCommentaire?> AjouterCommentaireAsync(
            int signalementId, string? auteurMail, string texte, CancellationToken ct = default);

        /// <summary>Null si ce commentaire n'existe pas sur ce signalement.</summary>
        Task<SignalementCommentaire?> ModifierCommentaireAsync(
            int signalementId, int id, string texte, CancellationToken ct = default);

        Task<bool> SupprimerCommentaireAsync(int signalementId, int id, CancellationToken ct = default);

        Task<IEnumerable<SignalementPiece>> GetPiecesAsync(int signalementId, CancellationToken ct = default);

        Task<SignalementPiece> AjouterPieceAsync(SignalementPiece piece, CancellationToken ct = default);

        /// <summary>La pièce retirée de la base, pour effacer son fichier ; null si elle n'existe pas.</summary>
        Task<SignalementPiece?> RetirerPieceAsync(int signalementId, int id, CancellationToken ct = default);

        /// <summary>Parmi ces signalements, ceux qui existent encore.</summary>
        Task<IReadOnlyList<int>> IdsExistantsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    }
}
