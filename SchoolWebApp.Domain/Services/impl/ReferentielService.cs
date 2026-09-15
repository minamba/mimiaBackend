using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class ReferentielService : IReferentielService
    {
        private readonly IReferentielRepository _referentielRepository;

        public ReferentielService(IReferentielRepository referentielRepository)
        {
            _referentielRepository = referentielRepository
                ?? throw new ArgumentNullException(nameof(referentielRepository));
        }

        public Task<IEnumerable<NiveauScolaire>> GetNiveauxScolairesAsync() =>
            _referentielRepository.GetNiveauxScolairesAsync();

        public Task<NiveauScolaire?> GetNiveauScolaireByIdAsync(int id) =>
            _referentielRepository.GetNiveauScolaireByIdAsync(id);

        public Task<IEnumerable<Matiere>> GetMatieresAsync(bool activesSeulement) =>
            _referentielRepository.GetMatieresAsync(activesSeulement);

        public Task<IEnumerable<Academie>> GetAcademiesAsync() =>
            _referentielRepository.GetAcademiesAsync();

        public Task<IEnumerable<PeriodeVacances>> GetPeriodesVacancesAsync(
            string? zone, DateTime debut, DateTime fin) =>
            _referentielRepository.GetPeriodesVacancesAsync(zone, debut, fin);

        public Task<PeriodeVacances?> GetProchainePeriodeVacancesAsync(string? zone, DateTime aujourdhui) =>
            _referentielRepository.GetProchainePeriodeVacancesAsync(zone, aujourdhui);

        public Task<IEnumerable<PeriodeVacances>> GetToutesLesPeriodesVacancesAsync() =>
            _referentielRepository.GetToutesLesPeriodesVacancesAsync();

        public Task<PeriodeVacances> AjouterPeriodeVacancesAsync(
            string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin) =>
            _referentielRepository.AjouterPeriodeVacancesAsync(zone, anneeScolaire, libelle, debut, fin);

        public Task<PeriodeVacances?> ModifierPeriodeVacancesAsync(
            int id, string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin) =>
            _referentielRepository.ModifierPeriodeVacancesAsync(id, zone, anneeScolaire, libelle, debut, fin);

        public Task<bool> SupprimerPeriodeVacancesAsync(int id) =>
            _referentielRepository.SupprimerPeriodeVacancesAsync(id);
    }
}
