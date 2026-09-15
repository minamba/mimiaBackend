using SchoolWebApp.Api.ViewModels;

namespace SchoolWebApp.Api.Builders
{
    public interface IReferentielViewModelBuilder
    {
        Task<IEnumerable<NiveauScolaireViewModel>> GetNiveauxScolairesAsync();

        Task<IEnumerable<MatiereViewModel>> GetMatieresAsync(bool activesSeulement);

        Task<IEnumerable<AcademieViewModel>> GetAcademiesAsync();

        /// <summary>
        /// L équipe pédagogique : un professeur par visage, avec ses matières.
        /// </summary>
        Task<IEnumerable<ProfesseurViewModel>> GetEquipeAsync();
    }
}
