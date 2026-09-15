using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface ISignalementRepository
    {
        Task<Signalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description,
            CancellationToken ct = default);

        /// <summary>Tous les signalements, le plus récent d'abord — pour l'écran d'administration.</summary>
        Task<IEnumerable<Signalement>> GetTousAsync(CancellationToken ct = default);

        Task<Signalement?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Null si le signalement n'existe pas.</summary>
        Task<Signalement?> ModifierAsync(
            int id, string categorie, string description, string etat, CancellationToken ct = default);

        Task<bool> SupprimerAsync(int id, CancellationToken ct = default);
    }
}
