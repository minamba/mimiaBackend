using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.ViewModels;

namespace SchoolWebApp.Api.Builders
{
    public interface IEleveViewModelBuilder
    {
        /// <summary>Les profils enfants du parent authentifié.</summary>
        Task<IEnumerable<EleveViewModel>> GetElevesAsync();

        /// <summary>Null si l'élève n'existe pas ou n'appartient pas au parent authentifié.</summary>
        Task<EleveViewModel?> GetEleveByIdAsync(int id);

        /// <summary>
        /// Les matières qui figurent à l emploi du temps de CET élève.
        /// Null si l élève n existe pas ou n appartient pas au parent.
        /// </summary>
        Task<IEnumerable<MatiereViewModel>?> GetMatieresDeLEleveAsync(int id);

        /// <summary>Null si le niveau scolaire fourni n'existe pas.</summary>
        Task<EleveViewModel?> AddEleveAsync(EleveRequest model);

        Task<EleveViewModel?> UpdateEleveAsync(EleveRequest model);

        /// <summary>Les profils retirés, restaurables.</summary>
        Task<IEnumerable<EleveViewModel>> GetArchivesAsync();

        /// <summary>Retire le profil des listes et du quota, sans rien effacer.</summary>
        Task<bool> ArchiverEleveAsync(int id);

        /// <summary>Remet un profil retiré parmi les enfants actifs.</summary>
        Task<bool> RestaurerEleveAsync(int id);

        /// <summary>Efface l'identité et tout l'historique. Irréversible.</summary>
        Task<bool> AnonymiserEleveAsync(int id);
    }
}
