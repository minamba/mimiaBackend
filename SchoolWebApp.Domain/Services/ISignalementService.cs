using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface ISignalementService
    {
        Task<Signalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description,
            CancellationToken ct = default);

        Task<IEnumerable<Signalement>> GetTousAsync(CancellationToken ct = default);

        Task<Signalement?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<Signalement?> ModifierAsync(
            int id, string categorie, string description, string etat, CancellationToken ct = default);

        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);
    }
}
