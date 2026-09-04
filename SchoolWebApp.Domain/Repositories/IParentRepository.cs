using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IParentRepository
    {
        Task<IEnumerable<Parent>> GetParentsAsync();

        /// <summary>
        /// Les adresses de TOUS les parents, pour une diffusion.
        ///
        /// Seulement les adresses : charger les comptes entiers pour n'en lire
        /// qu'un champ ferait transiter les fiches de toutes les familles.
        /// </summary>
        Task<IReadOnlyList<string>> GetAdressesParentsAsync(CancellationToken ct = default);

        Task<Parent?> GetParentByIdAsync(int id);

        /// <summary>
        /// Retrouve le parent à partir du claim `sub` du JWT.
        /// Point d'entrée de toute requête authentifiée.
        /// </summary>
        /// <param name="noterLaVenue">
        /// Vrai seulement quand c'est LE PARENT qui appelle : le jeton d'un
        /// enfant porte le même « sub », et noter sa venue ferait passer le
        /// parent pour présent chaque fois que son enfant travaille.
        /// </param>
        Task<Parent?> GetParentByIdentityUserIdAsync(
            string identityUserId, bool noterLaVenue = false);

        Task<Parent> AddParentAsync(Parent model);

        Task<Parent?> UpdateParentAsync(Parent model);

        Task<bool> DeleteParentAsync(int id);

        /// <summary>
        /// Efface le compte et tout ce qui s'y rattache — enfants, cours,
        /// évaluations, comptes rendus, abonnement. Sans retour possible.
        ///
        /// N'efface PAS le compte d'identité, qui vit dans une autre base :
        /// c'est au serveur d'identité de le faire, et l'appelant doit le lui
        /// demander ensuite.
        /// </summary>
        Task<bool> SupprimerCompteAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Retient le client Stripe créé pour ce parent, à sa première visite
        /// de la caisse.
        /// </summary>
        Task EnregistrerClientStripeAsync(
            int parentId, string clientId, CancellationToken ct = default);

        /// <summary>
        /// Le parent derrière un client Stripe. C'est le chemin de retour des
        /// webhooks : un événement Stripe ne cite que ses propres identifiants.
        /// </summary>
        Task<Parent?> GetParentParClientStripeAsync(
            string clientId, CancellationToken ct = default);
    }
}
