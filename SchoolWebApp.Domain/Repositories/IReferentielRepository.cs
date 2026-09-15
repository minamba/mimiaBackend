using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Données de référence, en lecture seule côté API pour l'essentiel :
    /// niveaux, matières et académies sont alimentés par le seed, pas par
    /// les utilisateurs.
    ///
    /// EXCEPTION ASSUMÉE : les PÉRIODES DE VACANCES. Elles sont tenues à
    /// jour automatiquement par `CalendrierScolaireSyncWorker`, mais un
    /// administrateur doit pouvoir corriger une date à la main — la source
    /// officielle elle-même contient des coquilles (voir
    /// `CalendrierScolaireApiService`) — donc les méthodes d'écriture
    /// vivent ici plutôt que sur un second dépôt, pour ne pas répartir
    /// l'accès à une même table entre deux endroits.
    /// </summary>
    public interface IReferentielRepository
    {
        Task<IEnumerable<NiveauScolaire>> GetNiveauxScolairesAsync();

        Task<NiveauScolaire?> GetNiveauScolaireByIdAsync(int id);

        /// <summary>Matières actives uniquement quand <paramref name="activesSeulement"/> est vrai.</summary>
        Task<IEnumerable<Matiere>> GetMatieresAsync(bool activesSeulement);

        /// <summary>Les 30 académies, triées par libellé.</summary>
        Task<IEnumerable<Academie>> GetAcademiesAsync();

        /// <summary>
        /// Les périodes de vacances d'une zone qui recoupent l'intervalle
        /// donné. Liste vide si la zone est nulle — l'appelant n'a alors rien
        /// à demander.
        /// </summary>
        Task<IEnumerable<PeriodeVacances>> GetPeriodesVacancesAsync(
            string? zone, DateTime debut, DateTime fin);

        /// <summary>
        /// La période en cours ou, à défaut, la toute prochaine pour cette
        /// zone — celle dont la date de fin n'est pas encore passée. Sert au
        /// décompte affiché sur « Mon calendrier » : dedans, c'est le nombre
        /// de jours avant la rentrée ; avant, avant les vacances. Null si la
        /// zone est nulle ou si aucune période à venir n'est connue.
        /// </summary>
        Task<PeriodeVacances?> GetProchainePeriodeVacancesAsync(string? zone, DateTime aujourdhui);

        /// <summary>
        /// TOUTES les périodes, toutes zones et années confondues — pour
        /// l'écran d'administration « Périodes scolaires », qui les montre
        /// et permet de les corriger à la main par-dessus ce que
        /// `CalendrierScolaireSyncWorker` synchronise chaque jour.
        /// </summary>
        Task<IEnumerable<PeriodeVacances>> GetToutesLesPeriodesVacancesAsync();

        Task<PeriodeVacances> AjouterPeriodeVacancesAsync(
            string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin);

        /// <summary>Null si la période n'existe pas.</summary>
        Task<PeriodeVacances?> ModifierPeriodeVacancesAsync(
            int id, string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin);

        Task<bool> SupprimerPeriodeVacancesAsync(int id);
    }
}
