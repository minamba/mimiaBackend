using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IAdminRepository
    {
        Task<ResumeAdmin> GetResumeAsync();

        /// <summary>
        /// Requêtes envoyées à l'agent, agrégées par période.
        /// <paramref name="eleveId"/> null = tous les élèves.
        /// </summary>
        Task<IEnumerable<PointSerie>> GetSerieRequetesAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId);

        /// <summary>
        /// Le parc d'abonnements période par période : actifs en fin de
        /// période, résiliations demandées, abonnements arrivés à terme.
        /// </summary>
        Task<IEnumerable<PointAbonnements>> GetSerieAbonnementsAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId);

        /// <summary>Inscriptions de parents par période, avec le cumul.</summary>
        /// <summary>Visiteurs uniques du site public, par période.</summary>
        Task<IEnumerable<PointSerie>> GetSerieVisitesAsync(
            Granularite granularite, DateTime debut, DateTime fin);

        /// <summary>Personnes distinctes sur toute la fenêtre.</summary>
        Task<int> CompterVisiteursAsync(DateTime debut, DateTime fin);

        /// <summary>Note une venue, au plus une par visiteur et par heure.</summary>
        Task EnregistrerVisiteAsync(string visiteur, DateTime quand);

        Task<IEnumerable<PointSerie>> GetSerieParentsAsync(
            Granularite granularite, DateTime debut, DateTime fin);

        /// <summary>Créations de profils élèves par période, avec le cumul.</summary>
        Task<IEnumerable<PointSerie>> GetSerieElevesAsync(
            Granularite granularite, DateTime debut, DateTime fin);

        Task<IEnumerable<ParentAdmin>> GetParentsAsync(string? recherche, DateTime debut, DateTime fin);

        /// <summary>Ce que le produit a coûté entre deux dates.</summary>
        Task<CoutPeriode> GetCoutAsync(DateTime debut, DateTime fin);

        /// <summary>
        /// La place occupée par la base, et la part des documents.
        ///
        /// SUR EXPRESS, UNE BASE PLEINE ARRÊTE TOUT. Le chiffre n'était lisible
        /// qu'en ouvrant un client SQL sur la production — autant dire jamais.
        /// </summary>
        Task<EtatBase> GetEtatBaseAsync();

        Task<IEnumerable<EleveAdmin>> GetElevesAsync(int? parentId, string? recherche);

        Task<bool> SupprimerParentAsync(int id);

        Task<bool> SupprimerEleveAsync(int id);

        Task<ParentAdmin?> ModifierParentAsync(int id, string? prenom, string? nom, string? mail);

        /// <summary>Fiche complète d'un élève, ou null s'il n'existe pas.</summary>
        Task<FicheEleve?> GetFicheEleveAsync(int eleveId);

        Task<EleveAdmin?> ModifierEleveAsync(int id, string? prenom, string? nom, int? age, int? niveauScolaireId, Sexe? sexe);
    }
}
