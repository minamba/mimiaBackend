using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Services
{
    public interface IAdminService
    {
        Task<ResumeAdmin> GetResumeAsync();

        Task<IEnumerable<PointSerie>> GetSerieRequetesAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId);

        Task<IEnumerable<PointAbonnements>> GetSerieAbonnementsAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId);

        /// <summary>Visiteurs uniques du site public, par période.</summary>
        Task<IEnumerable<PointSerie>> GetSerieVisitesAsync(
            Granularite granularite, DateTime debut, DateTime fin);

        /// <summary>Personnes distinctes sur toute la fenêtre.</summary>
        Task<int> CompterVisiteursAsync(DateTime debut, DateTime fin);

        /// <summary>Note une venue du site public.</summary>
        Task EnregistrerVisiteAsync(string visiteur);

        Task<IEnumerable<PointSerie>> GetSerieParentsAsync(
            Granularite granularite, DateTime debut, DateTime fin);

        Task<IEnumerable<PointSerie>> GetSerieElevesAsync(
            Granularite granularite, DateTime debut, DateTime fin);

        Task<IEnumerable<ParentAdmin>> GetParentsAsync(string? recherche, DateTime debut, DateTime fin);

        /// <summary>Ce que le produit a coûté entre deux dates.</summary>
        Task<CoutPeriode> GetCoutAsync(DateTime debut, DateTime fin);

        /// <summary>La place occupée par la base, et la part des documents.</summary>
        Task<EtatBase> GetEtatBaseAsync();

        Task<IEnumerable<EleveAdmin>> GetElevesAsync(int? parentId, string? recherche);

        Task<ParentAdmin?> ModifierParentAsync(int id, string? prenom, string? nom, string? mail);

        Task<FicheEleve?> GetFicheEleveAsync(int eleveId);

        Task<EleveAdmin?> ModifierEleveAsync(int id, string? prenom, string? nom, int? age, int? niveauScolaireId, Sexe? sexe);

        Task<bool> SupprimerParentAsync(int id);

        Task<bool> SupprimerEleveAsync(int id);
    }
}
