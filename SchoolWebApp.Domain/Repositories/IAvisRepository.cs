using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les avis des familles : dépôt, relecture, publication.
    /// </summary>
    public interface IAvisRepository
    {
        /// <summary>
        /// Ce que la page d'accueil affiche. Publiés uniquement.
        /// </summary>
        /// <param name="limite">Combien d'avis ramener.</param>
        /// <param name="decalage">Combien en sauter, pour la page suivante.</param>
        /// <remarks>
        /// LA MOYENNE ET LA RÉPARTITION IGNORENT CES DEUX BORNES : elles
        /// portent toujours sur TOUS les avis publiés. Les calculer sur la
        /// page affichée les ferait changer en tournant les pages, et
        /// « 4,3 sur 5 » cesserait de vouloir dire quoi que ce soit.
        /// </remarks>
        Task<AvisPublics> GetPublicsAsync(
            int limite, int decalage = 0, CancellationToken ct = default);

        /// <summary>L'avis de ce parent, ou null s'il n'en a jamais laissé.</summary>
        Task<MonAvis?> GetMonAvisAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Dépose ou remplace l'avis d'un parent.
        ///
        /// REPASSE TOUJOURS EN ATTENTE DE RELECTURE, y compris quand l'avis
        /// était déjà publié. Sans quoi il suffirait de publier une phrase
        /// anodine, d'attendre la validation, puis de la remplacer.
        /// </summary>
        Task<MonAvis> DeposerAsync(
            int parentId, int note, string? titre, string? commentaire, CancellationToken ct = default);

        /// <summary>Efface l'avis d'un parent. Rend faux s'il n'en avait pas.</summary>
        Task<bool> RetirerAsync(int parentId, CancellationToken ct = default);

        /// <summary>Tous les avis, publiés ou non, pour la relecture.</summary>
        Task<IEnumerable<AvisAdmin>> GetPourRelectureAsync(CancellationToken ct = default);

        /// <summary>Publie ou dépublie un avis. Rend faux s'il n'existe pas.</summary>
        Task<bool> PublierAsync(int avisId, bool publie, CancellationToken ct = default);

        /// <summary>Supprime un avis depuis l'administration.</summary>
        Task<bool> SupprimerAsync(int avisId, CancellationToken ct = default);
    }
}
