using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class AdminService : IAdminService
    {
        /// <summary>
        /// La tranche d'historique servie d'un coup, ici comme sur les pages
        /// suivantes.
        ///
        /// Dix : de quoi couvrir la dizaine de jours écoulée sans repousser les
        /// points fragiles hors de l'écran. Ce n'était pas un plafond de vingt
        /// qu'il fallait — un plafond TRONQUE, et sans le dire : le parent d'un
        /// élève qui a passé soixante évaluations en voyait vingt et croyait
        /// les avoir toutes vues.
        /// </summary>
        private const int TaillePage = 10;

        private readonly IAdminRepository _adminRepository;
        private readonly IEvaluationRepository _evaluationRepository;
        private readonly IRapportRepository _rapportRepository;

        public AdminService(
            IAdminRepository adminRepository,
            IEvaluationRepository evaluationRepository,
            IRapportRepository rapportRepository)
        {
            _adminRepository = adminRepository ?? throw new ArgumentNullException(nameof(adminRepository));
            _evaluationRepository = evaluationRepository ?? throw new ArgumentNullException(nameof(evaluationRepository));
            _rapportRepository = rapportRepository ?? throw new ArgumentNullException(nameof(rapportRepository));
        }

        public Task<ResumeAdmin> GetResumeAsync() => _adminRepository.GetResumeAsync();

        public Task<IEnumerable<PointSerie>> GetSerieRequetesAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId) =>
            _adminRepository.GetSerieRequetesAsync(granularite, debut, fin, eleveId);

        public Task<IEnumerable<PointAbonnements>> GetSerieAbonnementsAsync(
            Granularite granularite, DateTime debut, DateTime fin, int? eleveId) =>
            _adminRepository.GetSerieAbonnementsAsync(granularite, debut, fin, eleveId);

        public Task<bool> DefinirAdministrateurAsync(int parentId, bool actif) =>
            _adminRepository.DefinirAdministrateurAsync(parentId, actif);

        public Task<Tunnel> GetTunnelAsync(DateTime debut, DateTime fin) =>
            _adminRepository.GetTunnelAsync(debut, fin);

        public Task<IEnumerable<PointSerie>> GetSerieVisitesAsync(
            Granularite granularite, DateTime debut, DateTime fin) =>
            _adminRepository.GetSerieVisitesAsync(granularite, debut, fin);

        public Task<int> CompterVisiteursAsync(DateTime debut, DateTime fin) =>
            _adminRepository.CompterVisiteursAsync(debut, fin);

        /// <summary>
        /// L'HORLOGE EST POSÉE ICI, PAS PAR L'APPELANT.
        ///
        /// La route qui reçoit la venue est ouverte à tous : accepter une date
        /// venue du navigateur laisserait n'importe qui écrire dans le passé —
        /// ou dans l'avenir — et fausser une courbe qu'on lit pour décider.
        /// </summary>
        public Task EnregistrerVisiteAsync(string visiteur) =>
            _adminRepository.EnregistrerVisiteAsync(visiteur, DateTime.UtcNow);

        public Task<IEnumerable<PointSerie>> GetSerieParentsAsync(
            Granularite granularite, DateTime debut, DateTime fin) =>
            _adminRepository.GetSerieParentsAsync(granularite, debut, fin);

        public Task<IEnumerable<PointSerie>> GetSerieElevesAsync(
            Granularite granularite, DateTime debut, DateTime fin) =>
            _adminRepository.GetSerieElevesAsync(granularite, debut, fin);

        public Task<IEnumerable<ParentAdmin>> GetParentsAsync(
            string? recherche, DateTime debut, DateTime fin) =>
            _adminRepository.GetParentsAsync(recherche, debut, fin);

        public Task<CoutPeriode> GetCoutAsync(DateTime debut, DateTime fin) =>
            _adminRepository.GetCoutAsync(debut, fin);

        public Task<EtatBase> GetEtatBaseAsync() =>
            _adminRepository.GetEtatBaseAsync();

        public Task<IEnumerable<EleveAdmin>> GetElevesAsync(int? parentId, string? recherche) =>
            _adminRepository.GetElevesAsync(parentId, recherche);

        public Task<ParentAdmin?> ModifierParentAsync(int id, string? prenom, string? nom, string? mail) =>
            _adminRepository.ModifierParentAsync(id, prenom, nom, mail);

        /// <summary>
        /// La fiche, complétée des évaluations et de la progression.
        ///
        /// Assemblé ici plutôt que dans le repository : les notes vivent dans
        /// une autre table que l'activité, et faire porter les deux au même
        /// repository l'obligerait à connaître le modèle d'évaluation.
        /// </summary>
        public async Task<FicheEleve?> GetFicheEleveAsync(int eleveId)
        {
            var fiche = await _adminRepository.GetFicheEleveAsync(eleveId);
            if (fiche is null) return null;

            fiche.Evaluations = await _evaluationRepository.GetHistoriqueAsync(eleveId, null, TaillePage);
            fiche.Rapports = await _rapportRepository.GetHistoriqueAsync(eleveId, null, TaillePage);
            fiche.Progression = await _evaluationRepository.GetProgressionAsync(eleveId);

            return fiche;
        }

        public Task<EleveAdmin?> ModifierEleveAsync(int id, string? prenom, string? nom, int? age, int? niveauScolaireId, Sexe? sexe) =>
            _adminRepository.ModifierEleveAsync(id, prenom, nom, age, niveauScolaireId, sexe);

        public Task<RepartitionParents> GetRepartitionParentsAsync(
            IReadOnlyCollection<string> mailsExclus) =>
            _adminRepository.GetRepartitionParentsAsync(mailsExclus);

        public Task<string?> MailDuParentAsync(int id) => _adminRepository.MailDuParentAsync(id);

        public Task<bool> SupprimerParentAsync(int id) => _adminRepository.SupprimerParentAsync(id);

        public Task<bool> SupprimerEleveAsync(int id) => _adminRepository.SupprimerEleveAsync(id);
    }
}
