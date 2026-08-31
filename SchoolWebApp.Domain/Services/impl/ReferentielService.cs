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
    }
}
