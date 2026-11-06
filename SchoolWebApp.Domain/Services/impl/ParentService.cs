using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class ParentService : IParentService
    {
        private readonly IParentRepository _parentRepository;
        private readonly IAbonnementRepository? _abonnements;

        public ParentService(IParentRepository parentRepository, IAbonnementRepository? abonnements = null)
        {
            _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
            _abonnements = abonnements;
        }

        public Task<IEnumerable<Parent>> GetParentsAsync() =>
            _parentRepository.GetParentsAsync();

        public Task<Parent?> GetParentByIdAsync(int id) =>
            _parentRepository.GetParentByIdAsync(id);

        public Task<Parent?> GetParentByMailAsync(string mail) =>
            _parentRepository.GetParentByMailAsync(mail);

        public Task<Parent?> GetParentByIdentityUserIdAsync(string identityUserId) =>
            _parentRepository.GetParentByIdentityUserIdAsync(identityUserId);

        public async Task<Parent> GetOrCreateAsync(
            string identityUserId, string? mail, string? prenom, string? nom,
            bool noterLaVenue = false)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                throw new ArgumentException("L'identifiant utilisateur est obligatoire.", nameof(identityUserId));
            }

            var existant = await _parentRepository.GetParentByIdentityUserIdAsync(identityUserId, noterLaVenue);
            if (existant is not null)
            {
                return existant;
            }

            var cree = await _parentRepository.AddParentAsync(new Parent
            {
                IdentityUserId = identityUserId,
                Mail = mail,
                Prenom = prenom,
                Nom = nom,
                DateCreation = DateTime.UtcNow
            });

            // L'ESSAI S'OUVRE AVEC LE COMPTE — Camara, le 16/10/2026 : « quand un
            // parent clique sur Commencer gratuitement et crée un compte, ça ouvre
            // son essai directement ? ». Pas toujours : l'intention voyageait
            // dans l'onglet (`essaiVise`, sessionStorage), et l'inscription
            // passe par un lien de confirmation reçu par courriel — ouvert dans
            // un AUTRE onglet, voire sur le téléphone, où l'intention n'existe
            // pas. Le compte arrivait sans essai.
            //
            // Ouvert ici, à la naissance de la fiche, il ne dépend plus d'aucun
            // onglet. Mêmes gardes que partout (`SouscrireAsync`) : essais fermés
            // dans les Modes, ou adresse qui a déjà eu le sien, et il ne s'ouvre
            // pas. Un parent venu payer une formule le remplace en souscrivant.
            // Un échec ne bloque pas la connexion.
            if (_abonnements is not null)
            {
                try
                {
                    await _abonnements.OuvrirEssaiAsync(cree.Id);
                }
                catch
                {
                    // Le compte existe ; l'essai reste à prendre sur les tarifs.
                }
            }

            return cree;
        }

        public Task<Parent> AddParentAsync(Parent model) =>
            _parentRepository.AddParentAsync(model);

        public Task MarquerTutorielVuAsync(int parentId, CancellationToken ct = default) =>
            _parentRepository.MarquerTutorielVuAsync(parentId, ct);

        public Task<Parent?> UpdateParentAsync(Parent model) =>
            _parentRepository.UpdateParentAsync(model);

        public Task<bool> DeleteParentAsync(int id) =>
            _parentRepository.DeleteParentAsync(id);
    }
}
