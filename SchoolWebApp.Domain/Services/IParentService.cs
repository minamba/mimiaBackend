using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface IParentService
    {
        Task<IEnumerable<Parent>> GetParentsAsync();

        Task<Parent?> GetParentByIdAsync(int id);

        /// <summary>Sert à l'admin, qui pense en adresse et non en identifiant.</summary>
        Task<Parent?> GetParentByMailAsync(string mail);

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

        /// <summary>Note que ce parent a vu le tutoriel. La première date reste.</summary>
        Task MarquerTutorielVuAsync(int parentId, CancellationToken ct = default);

        /// <summary>Note que ce parent a vu le tutoriel de la fiche. La première date reste.</summary>
        Task MarquerTutorielFicheVuAsync(int parentId, CancellationToken ct = default);
        Task MarquerTutorielEnfantsVuAsync(int parentId, CancellationToken ct = default);

        /// <summary>Note la version des conditions générales que ce parent accepte, et quand.</summary>
        Task AccepterConditionsAsync(int parentId, string version, DateTime le, CancellationToken ct = default);

        /// <summary>Note que ce parent consent, de lui-même, à recevoir les offres par email. Un consentement déjà donné garde sa date.</summary>
        Task ConsentirOffresEmailAsync(int parentId, DateTime le, CancellationToken ct = default);
    }
}
