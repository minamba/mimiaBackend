using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Données de référence, en lecture seule côté API :
    /// elles sont alimentées par le seed, pas par les utilisateurs.
    /// </summary>
    public interface IReferentielRepository
    {
        Task<IEnumerable<NiveauScolaire>> GetNiveauxScolairesAsync();

        Task<NiveauScolaire?> GetNiveauScolaireByIdAsync(int id);

        /// <summary>Matières actives uniquement quand <paramref name="activesSeulement"/> est vrai.</summary>
        Task<IEnumerable<Matiere>> GetMatieresAsync(bool activesSeulement);
    }
}
