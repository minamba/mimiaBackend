using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les sessions ouvertes par les enfants sur leurs appareils.
    ///
    /// Le jeton n'entre et ne sort d'ici que HACHÉ : c'est la couche au-dessus
    /// qui tire le jeton en clair, le rend une fois à l'appareil, et n'en garde
    /// que l'empreinte. Rien dans cette interface ne permet de retrouver un
    /// jeton perdu, et c'est voulu — un jeton perdu se remplace, il ne se
    /// récupère pas.
    /// </summary>
    public interface ISessionEleveRepository
    {
        /// <summary>Ouvre une session et rend ce qu'il faut pour la décrire.</summary>
        Task<SessionEleve> OuvrirAsync(
            int eleveId, string jetonHache, string? appareil, CancellationToken ct = default);

        /// <summary>
        /// La session portant ce jeton, ou null.
        ///
        /// Rend AUSSI le parent et le prénom : c'est ce que l'authentification
        /// a besoin de savoir à chaque appel, et un aller-retour de plus par
        /// requête serait payé sur toute la journée d'un enfant.
        /// </summary>
        Task<SessionEleve?> TrouverAsync(string jetonHache, CancellationToken ct = default);

        /// <summary>Note l'activité, pour que le parent voie qui travaille.</summary>
        Task ToucherAsync(int sessionId, CancellationToken ct = default);

        /// <summary>Ferme une session précise — l'enfant se déconnecte.</summary>
        Task FermerAsync(string jetonHache, CancellationToken ct = default);

        /// <summary>
        /// Ferme TOUTES les sessions d'un enfant.
        ///
        /// Appelé quand le parent suspend l'accès ou régénère le code : sans
        /// ça, l'appareil déjà connecté continuerait de travailler, et le
        /// parent croirait avoir coupé quelque chose qu'il n'a pas coupé.
        /// </summary>
        Task<int> FermerToutesAsync(int eleveId, CancellationToken ct = default);
    }
}
