using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface IReferentielService
    {
        Task<IEnumerable<NiveauScolaire>> GetNiveauxScolairesAsync();

        Task<NiveauScolaire?> GetNiveauScolaireByIdAsync(int id);

        Task<IEnumerable<Matiere>> GetMatieresAsync(bool activesSeulement);
    }
}
