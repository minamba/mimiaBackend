using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface IParentService
    {
        Task<IEnumerable<Parent>> GetParentsAsync();

        Task<Parent?> GetParentByIdAsync(int id);

        Task<Parent?> GetParentByIdentityUserIdAsync(string identityUserId);

        /// <summary>
        /// Retourne le parent correspondant au JWT, en le créant au premier appel.
        /// Le compte existe côté serveur d'identité mais pas encore côté métier :
        /// c'est ici que les deux se rejoignent, plutôt que par un webhook fragile.
        /// </summary>
        /// <param name="noterLaVenue">
        /// Vrai seulement quand c'est LE PARENT qui appelle — voir
        /// IParentRepository. Un enfant porte le « sub » de son parent.
        /// </param>
        Task<Parent> GetOrCreateAsync(
            string identityUserId, string? mail, string? prenom, string? nom,
            bool noterLaVenue = false);

        Task<Parent> AddParentAsync(Parent model);

        Task<Parent?> UpdateParentAsync(Parent model);

        Task<bool> DeleteParentAsync(int id);
    }
}
