using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les examens nationaux et la préparation de chaque élève — voir
    /// <see cref="PreparationExamenEleve"/>.
    ///
    /// L'EXAMEN D'UN ÉLÈVE SE DÉDUIT, IL NE SE CHOISIT PAS : sa classe, et la
    /// session de l'année scolaire en cours (bascule au 1er août, voir
    /// `AnneeScolaire`). Un élève de 3e en 2026-2027 passe le brevet 2027.
    /// </summary>
    public interface IExamenRepository
    {
        /// <summary>
        /// L'épreuve, si elle appartient bien à l'examen que passe un élève de
        /// cette classe cette année. Null sinon — un lien recopié d'une autre
        /// classe ou d'une autre session ne mène à rien.
        /// </summary>
        Task<EpreuveExamenInfo?> GetEpreuveApplicableAsync(
            string? niveauCode, string? epreuveCode, CancellationToken ct = default);

        /// <summary>Toute la section d'accueil. Null quand la classe n'a pas d'examen cette année.</summary>
        Task<PreparationExamenEleve?> GetPreparationExamenAsync(
            int eleveId, string? niveauCode, CancellationToken ct = default);

        /// <summary>Une épreuve, avec ses notions. Null si elle ne concerne pas cet élève.</summary>
        Task<EpreuvePreparation?> GetPreparationEpreuveAsync(
            int eleveId, string? niveauCode, string epreuveCode, CancellationToken ct = default);

        /// <summary>Une séance de préparation de plus, ouverte par le bouton de l'épreuve.</summary>
        Task MarquerPreparationAsync(
            int eleveId, int epreuveId, int matiereId, DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Le verdict du professeur de cette matière sur cette épreuve. Plafonné
        /// comme celui d'un contrôle : ni « prêt » ni « bientôt prêt » sans
        /// programme ni révision. Faux si l'épreuve ou la matière ne tient pas.
        /// </summary>
        Task<bool> PoserVerdictAsync(
            int eleveId, int epreuveId, int matiereId, string verdict, string? observation,
            DateTime maintenant, CancellationToken ct = default);

        /// <summary>
        /// Toutes les cartes d'examen actives, avec le nombre de notions retenues
        /// par matière et ce qui cloche. Voir <see cref="VerificationExamen"/>.
        /// </summary>
        Task<List<VerificationExamen>> VerifierAsync(CancellationToken ct = default);
    }
}
