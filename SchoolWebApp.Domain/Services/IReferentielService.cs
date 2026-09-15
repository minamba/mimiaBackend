using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface IReferentielService
    {
        Task<IEnumerable<NiveauScolaire>> GetNiveauxScolairesAsync();

        Task<NiveauScolaire?> GetNiveauScolaireByIdAsync(int id);

        Task<IEnumerable<Matiere>> GetMatieresAsync(bool activesSeulement);

        Task<IEnumerable<Academie>> GetAcademiesAsync();

        Task<IEnumerable<PeriodeVacances>> GetPeriodesVacancesAsync(string? zone, DateTime debut, DateTime fin);

        Task<PeriodeVacances?> GetProchainePeriodeVacancesAsync(string? zone, DateTime aujourdhui);

        Task<IEnumerable<PeriodeVacances>> GetToutesLesPeriodesVacancesAsync();

        Task<PeriodeVacances> AjouterPeriodeVacancesAsync(
            string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin);

        Task<PeriodeVacances?> ModifierPeriodeVacancesAsync(
            int id, string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin);

        Task<bool> SupprimerPeriodeVacancesAsync(int id);
    }
}
