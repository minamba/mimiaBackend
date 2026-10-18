using SchoolWebApp.Domain.Emails;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>Un administrateur, avec la signature qui lui est attribuée (onglet Signatures).</summary>
    public record SignataireAdmin(int ParentId, string? Prenom, string? Nom, string? Mail, string? Role, int? SignatureId, string? AdresseBoite);

    /// <summary>Une pièce d'un brouillon (18/10/2026) : son fichier est sur le disque, à `Chemin`.</summary>
    public record PieceBrouillon(int Id, string Genre, int Rang, string NomFichier, string TypeMime, int Taille, string Chemin);

    /// <summary>Une pièce à ranger dans un brouillon, déjà écrite sur le disque.</summary>
    public record NouvellePieceBrouillon(string Genre, int Rang, string NomFichier, string TypeMime, int Taille, string Chemin);

    /// <summary>Un brouillon de la messagerie (17/10/2026), à son auteur.</summary>
    /// <remarks>`MessageOrigine` nul : un message écrit depuis « Écrire à un parent ».</remarks>
    public record BrouillonMessagerie(int Id, long? MessageOrigine, string? Destinataire, string? Sujet, string Texte, DateTime DateModification, string? Titre = null);

    /// <summary>
    /// Les boîtes mail des administrateurs et leurs signatures (17/10/2026).
    /// Le mot de passe d'une boîte n'entre et ne sort d'ici que CHIFFRÉ.
    /// </summary>
    public interface IMessagerieAdminRepository
    {
        /// <summary>L'adresse et le mot de passe chiffré de la boîte de ce parent, ou null.</summary>
        Task<(string Adresse, string MotDePasseChiffre)?> BoiteAsync(int parentId, CancellationToken ct = default);

        Task EnregistrerBoiteAsync(int parentId, string adresse, string motDePasseChiffre, CancellationToken ct = default);

        Task SupprimerBoiteAsync(int parentId, CancellationToken ct = default);

        Task<IReadOnlyList<ModeleSignature>> SignaturesAsync(CancellationToken ct = default);

        /// <summary>Le modèle attribué à ce parent, sinon le modèle par défaut, sinon null.</summary>
        Task<ModeleSignature?> SignatureDuParentAsync(int parentId, CancellationToken ct = default);

        /// <summary>Null + erreur si le nom manque ou est déjà pris.</summary>
        Task<(ModeleSignature? Modele, string? Erreur)> EnregistrerSignatureAsync(int? id, SaisieSignature saisie, CancellationToken ct = default);

        /// <summary>Faux + erreur pour le dernier modèle (il en faut toujours un).</summary>
        Task<(bool Fait, string? Erreur)> SupprimerSignatureAsync(int id, CancellationToken ct = default);

        /// <summary>Les comptes qui ouvrent l'administration, et ceux dont l'adresse est donnée (le super-admin).</summary>
        Task<IReadOnlyList<SignataireAdmin>> SignatairesAsync(IEnumerable<string> autresMails, CancellationToken ct = default);

        /// <summary>Le compte connecté, retrouvé par son adresse de connexion ; null s'il n'existe pas.</summary>
        Task<SignataireAdmin?> SignataireAsync(string? mail, CancellationToken ct = default);

        /// <summary>Null : le modèle par défaut.</summary>
        Task<bool> AttribuerSignatureAsync(int parentId, int? signatureId, CancellationToken ct = default);

        // ---------------------------------------------------- brouillons (17/10/2026)

        /// <summary>Une page de ses brouillons sur cette boîte, du plus récent au plus ancien.</summary>
        Task<(IReadOnlyList<BrouillonMessagerie> Brouillons, int Total)> BrouillonsAsync(
            int parentId, string boite, int page, int taille, CancellationToken ct = default);

        /// <summary>Son brouillon de réponse à ce message, ou null.</summary>
        Task<BrouillonMessagerie?> BrouillonPourAsync(int parentId, string boite, long messageOrigine, CancellationToken ct = default);

        /// <summary>Parmi ces messages, ceux auxquels il a une réponse en brouillon.</summary>
        Task<IReadOnlySet<long>> MessagesAvecBrouillonAsync(
            int parentId, string boite, IEnumerable<long> messages, CancellationToken ct = default);

        /// <summary>Crée ou met à jour son brouillon de réponse à ce message.</summary>
        Task<BrouillonMessagerie> EnregistrerBrouillonAsync(
            int parentId, string boite, long messageOrigine, string? destinataire, string? sujet, string texte,
            CancellationToken ct = default);

        /// <summary>Un de ses brouillons, par son identifiant ; null s'il n'est pas à lui.</summary>
        Task<BrouillonMessagerie?> BrouillonAsync(int parentId, string boite, int id, CancellationToken ct = default);

        /// <summary>
        /// Crée (`id` nul ou inconnu) ou met à jour un brouillon de message libre
        /// — « Écrire à un parent », sans message d'origine.
        /// </summary>
        Task<BrouillonMessagerie> EnregistrerBrouillonLibreAsync(
            int parentId, string boite, int? id, string? destinataire, string? sujet, string? titre, string texte,
            CancellationToken ct = default);

        /// <summary>Les pièces d'un de ses brouillons ; null s'il n'est pas à lui.</summary>
        Task<IReadOnlyList<PieceBrouillon>?> PiecesBrouillonAsync(int parentId, string boite, int brouillonId, CancellationToken ct = default);

        /// <summary>
        /// Remplace toutes les pièces d'un de ses brouillons. Rend les chemins des
        /// anciennes, à effacer du disque ; null s'il n'est pas à lui.
        /// </summary>
        Task<IReadOnlyList<string>?> RemplacerPiecesBrouillonAsync(
            int parentId, string boite, int brouillonId, IReadOnlyList<NouvellePieceBrouillon> pieces, CancellationToken ct = default);

        /// <summary>Supprime ces brouillons, s'ils sont bien à lui ; rend le nombre supprimé.</summary>
        Task<int> SupprimerBrouillonsAsync(int parentId, string boite, IEnumerable<int> ids, CancellationToken ct = default);

        /// <summary>Supprime son brouillon de réponse à ce message (réponse envoyée, ou texte effacé).</summary>
        Task SupprimerBrouillonPourAsync(int parentId, string boite, long messageOrigine, CancellationToken ct = default);
    }
}
