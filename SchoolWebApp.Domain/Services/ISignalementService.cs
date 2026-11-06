using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface ISignalementService
    {
        Task<Signalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description,
            CancellationToken ct = default);

        Task<IEnumerable<Signalement>> GetTousAsync(CancellationToken ct = default);

        Task<Signalement> CreerAdminAsync(
            int parentId, string categorie, string description, string? competence, int? ligneDeTestId,
            CancellationToken ct = default);

        Task<IEnumerable<Signalement>> GetAdminAsync(CancellationToken ct = default);

        Task<Signalement?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<Signalement?> ModifierAsync(
            int id, string categorie, string description, string etat, string? competence = null, CancellationToken ct = default);

        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);

        Task<IEnumerable<SignalementCommentaire>> GetCommentairesAsync(int signalementId, CancellationToken ct = default);

        Task<SignalementCommentaire?> AjouterCommentaireAsync(
            int signalementId, string? auteurMail, string texte, CancellationToken ct = default);

        Task<SignalementCommentaire?> ModifierCommentaireAsync(
            int signalementId, int id, string texte, CancellationToken ct = default);

        Task<bool> SupprimerCommentaireAsync(int signalementId, int id, CancellationToken ct = default);

        Task<IEnumerable<SignalementPiece>> GetPiecesAsync(int signalementId, CancellationToken ct = default);

        Task<AjoutPieceSignalement> AjouterPieceAsync(
            int signalementId, string? nom, byte[] octets, string? ajoutePar, CancellationToken ct = default);

        /// <summary>La pièce et son contenu ; null si l'une ou l'autre manque.</summary>
        Task<(SignalementPiece Piece, byte[] Octets)?> LirePieceAsync(
            int signalementId, int id, CancellationToken ct = default);

        Task<bool> SupprimerPieceAsync(int signalementId, int id, CancellationToken ct = default);
    }
}
