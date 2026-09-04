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

        /// <summary>
        /// Accorde ou retire le droit d'administrer. Rend faux si le parent
        /// n'existe pas.
        /// </summary>
        Task<bool> DefinirAdministrateurAsync(int parentId, bool actif);

        /// <summary>Le tunnel : visiteurs, essais lancés, essais convertis.</summary>
        Task<Tunnel> GetTunnelAsync(DateTime debut, DateTime fin);

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

        /// <summary>
        /// L adresse d un compte, sans le reste de sa fiche.
        ///
        /// Sert a reconnaitre le compte protege AVANT de le modifier ou de
        /// le supprimer. Charger la fiche entiere pour lire un seul champ
        /// ferait payer une dizaine de sous-requetes a chaque garde.
        /// </summary>
        /// <summary>Où en est le fichier clients, à cet instant.</summary>
        /// <param name="mailsExclus">
        /// Adresses à ne pas compter — les comptes de l'exploitant, quand
        /// l'appelant n'a pas le droit de les voir. Vide = tout compter.
        /// </param>
        Task<RepartitionParents> GetRepartitionParentsAsync(
            IReadOnlyCollection<string> mailsExclus);

        Task<string?> MailDuParentAsync(int id);

        Task<bool> SupprimerParentAsync(int id);

        Task<bool> SupprimerEleveAsync(int id);
    }
}
