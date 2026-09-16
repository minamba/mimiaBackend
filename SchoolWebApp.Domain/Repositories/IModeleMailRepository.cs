using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les modèles de courriel — diffusions enregistrées et courriels
    /// automatiques.
    ///
    /// LES LISTES NE RAMÈNENT NI TEXTE NI OCTETS. Seuls `GetPiecesAsync` et
    /// `GetPieceAsync` lisent les fichiers, au moment d'envoyer ou de servir
    /// une pièce.
    /// </summary>
    public interface IModeleMailRepository
    {
        /// <summary>Les modèles d'une nature, les plus récemment modifiés d'abord.</summary>
        Task<IReadOnlyList<ModeleMailResume>> GetResumesAsync(string nature, CancellationToken ct = default);

        Task<ModeleMailDetail?> GetDetailAsync(int id, CancellationToken ct = default);

        /// <summary>Toutes les pièces d'un modèle, octets compris, par genre puis rang.</summary>
        Task<IReadOnlyList<PieceModeleMailContenu>> GetPiecesAsync(int id, CancellationToken ct = default);

        Task<PieceModeleMailContenu?> GetPieceAsync(int id, int pieceId, CancellationToken ct = default);

        /// <summary>
        /// Crée un modèle — une diffusion, ou un courriel automatique sans code
        /// (donc sans règle d'envoi) — avec ses pièces, dans l'ordre reçu : la
        /// première image reçue devient `[image:1]`.
        /// </summary>
        Task<ModeleMailDetail> CreerAsync(
            string nature,
            string nom,
            string? description,
            string sujet,
            string titre,
            string texte,
            IReadOnlyList<NouvellePieceMail> images,
            IReadOnlyList<NouvellePieceMail> documents,
            CancellationToken ct = default);

        /// <summary>
        /// La sauvegarde automatique : le texte, pas les pièces.
        /// Nul si le modèle n'existe pas ; sinon la nouvelle date de modification.
        /// </summary>
        /// <param name="nom">Nul : inchangé.</param>
        /// <param name="description">Nul : inchangée.</param>
        Task<DateTime?> ModifierContenuAsync(
            int id,
            string? nom,
            string? description,
            string sujet,
            string titre,
            string texte,
            CancellationToken ct = default);

        /// <summary>
        /// Ajoute une pièce au rang suivant de son genre. Refusée si le poids
        /// total du modèle dépasserait <paramref name="poidsMax"/>.
        /// </summary>
        Task<ResultatAjoutPiece> AjouterPieceAsync(
            int id,
            string genre,
            NouvellePieceMail piece,
            int poidsMax,
            CancellationToken ct = default);

        /// <summary>
        /// Retire une pièce, renumérote les suivantes et, pour une image,
        /// réécrit les marqueurs du texte — le tout dans la même transaction.
        /// Rend le modèle à jour, ou nul s'il n'existe pas.
        /// </summary>
        Task<ModeleMailDetail?> RetirerPieceAsync(int id, int pieceId, CancellationToken ct = default);

        /// <summary>
        /// Supprime un modèle et ses pièces. Faux s'il est introuvable, ou s'il
        /// est relié à un envoi (bilans, période d'essai, demandes d'avis) :
        /// ceux-là ne se suppriment pas.
        /// </summary>
        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);

        /// <summary>Note un envoi (date et résultat), pour l'écran.</summary>
        Task NoterEnvoiAsync(int id, DateTime dateUtc, string? resultat, CancellationToken ct = default);

        /// <summary>Note un résultat sans envoi — « envoi manqué », par exemple.</summary>
        Task NoterResultatAsync(int id, string resultat, CancellationToken ct = default);

        // ------------------------------------------------- courriels automatiques

        /// <summary>Un courriel automatique par son code, ou nul s'il n'a pas été semé.</summary>
        Task<ModeleMailDetail?> GetParCodeAsync(string code, CancellationToken ct = default);

        /// <summary>Les courriels automatiques programmés.</summary>
        Task<IReadOnlyList<ModeleMailResume>> GetAutomatiquesActifsAsync(CancellationToken ct = default);

        /// <summary>
        /// Enregistre la planification d'un courriel AUTOMATIQUE. Faux si le
        /// modèle n'existe pas ou n'est pas automatique.
        /// </summary>
        /// <param name="derniereOccurrence">
        /// L'occurrence déjà échue au moment du réglage : la marquer comme prise
        /// empêche qu'un courriel programmé à 10 h, allumé à 14 h, parte
        /// aussitôt pour le créneau du matin.
        /// </param>
        Task<bool> DefinirPlanificationAsync(
            int id,
            string frequence,
            TimeOnly? heure,
            int? jourSemaine,
            int? jourMois,
            bool actif,
            DateTime? derniereOccurrence,
            CancellationToken ct = default);

        /// <summary>
        /// Prend une occurrence en charge, en une seule écriture conditionnelle.
        /// Faux si elle l'était déjà — un second tour du planificateur, ou un
        /// redémarrage pendant l'envoi, ne la relance pas.
        /// </summary>
        Task<bool> ReserverOccurrenceAsync(int id, DateTime occurrenceUtc, CancellationToken ct = default);
    }
}
