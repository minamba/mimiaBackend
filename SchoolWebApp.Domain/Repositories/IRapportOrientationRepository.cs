using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>Les rapports d'orientation de la conseillère (11/10/2026).</summary>
    public interface IRapportOrientationRepository
    {
        /// <summary>
        /// Enregistre le rapport d'une séance. UN PAR SÉANCE : une conversation
        /// traverse les séances (et les années), alors un second rapport de la
        /// même conversation dans les <c>FenetreMinutes</c> minutes REMPLACE le
        /// premier — une conclusion anticipée suivie d'une vraie ne fait pas
        /// deux rapports.
        /// </summary>
        Task<RapportOrientationEleve> EnregistrerAsync(
            int eleveId, int? conversationId, RapportOrientationEleve rapport, CancellationToken ct = default);

        /// <summary>Les rapports de l'élève, le plus récent d'abord.</summary>
        Task<IReadOnlyList<RapportOrientationEleve>> GetParEleveAsync(int eleveId, int limite, CancellationToken ct = default);

        /// <summary>Combien l'élève n'en a pas encore ouvert.</summary>
        Task<int> CompterNouveauxAsync(int eleveId, CancellationToken ct = default);

        /// <summary>L'élève les a ouverts : ils ne sont plus nouveaux.</summary>
        Task MarquerVusAsync(int eleveId, CancellationToken ct = default);
    }
}
