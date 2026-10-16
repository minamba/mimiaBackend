using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// L'offre des lycées et le journal de son import — pour la conseillère
    /// d'orientation (11/10/2026).
    /// </summary>
    public interface IOrientationRepository
    {
        /// <summary>L'offre d'un lycée, ou null s'il n'est pas dans la base.</summary>
        Task<OffreLycee?> GetParUaiAsync(string uai, CancellationToken ct = default);

        /// <summary>
        /// Les lycées les plus proches d'un point, du plus près au plus loin,
        /// dans un rayon donné.
        /// </summary>
        Task<IReadOnlyList<LyceeProche>> ProchesAsync(
            double latitude, double longitude, int combien, double rayonKm, CancellationToken ct = default);

        /// <summary>Le nombre de lycées en base.</summary>
        Task<int> CompterAsync(CancellationToken ct = default);

        /// <summary>
        /// Remplace TOUTE l'offre par celle-ci, en une transaction : en cas
        /// d'échec, l'ancienne reste intacte. Rend le nombre de lycées écrits.
        /// </summary>
        Task<int> RemplacerToutAsync(IReadOnlyList<OffreLycee> lycees, CancellationToken ct = default);

        /// <summary>Inscrit un passage de l'import au journal.</summary>
        Task NoterPassageAsync(PassageImportOrientation passage, CancellationToken ct = default);

        /// <summary>Les derniers passages, du plus récent au plus ancien.</summary>
        Task<IReadOnlyList<PassageImportOrientation>> DerniersPassagesAsync(int combien, CancellationToken ct = default);
    }
}
