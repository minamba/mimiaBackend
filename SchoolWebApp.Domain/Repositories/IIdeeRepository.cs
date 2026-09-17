using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le carnet d'idées d'évolution.
    ///
    /// TOUTES LES LECTURES DE LISTE SONT SANS OCTETS. Seul
    /// <see cref="GetPieceAsync"/> ramène un fichier, et un seul à la fois.
    /// </summary>
    public interface IIdeeRepository
    {
        /// <summary>Toutes les idées, la plus récente d'abord.</summary>
        Task<IEnumerable<Idee>> GetToutesAsync(CancellationToken ct = default);

        Task<Idee?> GetAsync(int id, CancellationToken ct = default);

        /// <summary>Les octets d'une pièce jointe : image, audio ou document.</summary>
        Task<ImageIdee?> GetPieceAsync(int ideeId, int pieceId, CancellationToken ct = default);

        /// <summary>Crée une idée. Son statut est « Nouveau », toujours.</summary>
        Task<Idee> CreerAsync(
            string titre, string urgence, string? description,
            string? auteurPrenom, string? auteurNom, CancellationToken ct = default);

        /// <summary>
        /// Modifie une idée. Renvoie null si elle n'existe pas.
        /// </summary>
        Task<Idee?> ModifierAsync(
            int id, string titre, string urgence, string? description, string statut,
            CancellationToken ct = default);

        /// <summary>
        /// Supprime une idée ET TOUTES SES PIÈCES — images, audio, documents.
        ///
        /// Rien ne survit : les octets sont dans la table, pas sur le disque, et
        /// la contrainte porte ON DELETE CASCADE. Aucun fichier orphelin ne peut
        /// rester derrière, par construction.
        /// </summary>
        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Attache un fichier à une idée et renvoie la pièce créée. Null si
        /// l'idée n'existe pas.
        ///
        /// SON RANG VAUT 0 SI CE N'EST PAS UNE IMAGE. Le rang est ce que
        /// `[image:N]` désigne dans le texte : un audio ou un document ne
        /// s'insère pas dans la description, il se pose en dessous.
        /// </summary>
        Task<PieceJointeIdee?> AjouterPieceAsync(
            int ideeId, string nomFichier, string typeMime, byte[] donnees,
            CancellationToken ct = default);

        /// <summary>
        /// Retire une pièce et renvoie la description, réécrite quand il le faut.
        ///
        /// POUR UNE IMAGE, les suivantes sont RENUMÉROTÉES et le texte réécrit —
        /// `[image:3]` supprimé, les marqueurs au-dessus décalés. LES DEUX GESTES
        /// SONT INSÉPARABLES : renuméroter sans réécrire le texte ferait pointer
        /// un marqueur sur une autre image que celle voulue, en silence. C'est
        /// pour cela que le dépôt rend la description — l'appelant n'a pas à la
        /// recalculer.
        ///
        /// POUR UN AUDIO OU UN DOCUMENT, il n'y a rien à renuméroter : ils ne
        /// sont cités nulle part. La description revient inchangée.
        /// </summary>
        Task<string?> RetirerPieceAsync(int ideeId, int pieceId, CancellationToken ct = default);
    }
}
