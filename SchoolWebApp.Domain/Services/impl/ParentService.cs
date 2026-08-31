using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class ParentService : IParentService
    {
        private readonly IParentRepository _parentRepository;

        public ParentService(IParentRepository parentRepository)
        {
            _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        }

        public Task<IEnumerable<Parent>> GetParentsAsync() =>
            _parentRepository.GetParentsAsync();

        public Task<Parent?> GetParentByIdAsync(int id) =>
            _parentRepository.GetParentByIdAsync(id);

        public Task<Parent?> GetParentByIdentityUserIdAsync(string identityUserId) =>
            _parentRepository.GetParentByIdentityUserIdAsync(identityUserId);

        public async Task<Parent> GetOrCreateAsync(string identityUserId, string? mail, string? prenom, string? nom)
        {
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                throw new ArgumentException("L'identifiant utilisateur est obligatoire.", nameof(identityUserId));
            }

            var existant = await _parentRepository.GetParentByIdentityUserIdAsync(identityUserId);
            if (existant is not null)
            {
                return existant;
            }

            return await _parentRepository.AddParentAsync(new Parent
            {
                IdentityUserId = identityUserId,
                Mail = mail,
                Prenom = prenom,
                Nom = nom,
                DateCreation = DateTime.UtcNow
            });
        }

        public Task<Parent> AddParentAsync(Parent model) =>
            _parentRepository.AddParentAsync(model);

        public Task<Parent?> UpdateParentAsync(Parent model) =>
            _parentRepository.UpdateParentAsync(model);

        public Task<bool> DeleteParentAsync(int id) =>
            _parentRepository.DeleteParentAsync(id);
    }
}
