using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IBilanRepository
    {
        /// <summary>
        /// Bilans bruts de la période, un par élève dont le parent a une adresse.
        /// Les élèves sans activité sont inclus : une semaine vide se signale,
        /// elle ne se tait pas.
        /// </summary>
        Task<IEnumerable<BilanEleve>> GetBilansAsync(
            DateTime debut,
            DateTime fin,
            int? eleveId = null,
            int? groupeDuJour = null,
            int? nombreDeGroupes = null,
            CancellationToken ct = default);

        /// <summary>
        /// Combien de bilans partiraient aujourd hui. Sert a decider sur
        /// combien de jours etaler la serie AVANT de commencer a envoyer.
        /// </summary>
        Task<int> CompterEligiblesAsync(CancellationToken ct = default);
    }
}
