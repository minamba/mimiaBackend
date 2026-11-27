using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>Les dates officielles des épreuves (25/11/2026). Voir <see cref="CalendrierExamens"/>.</summary>
    public interface ICalendrierExamensRepository
    {
        /// <summary>Les épreuves actives que Mimia connaît : ce que la veille cherche dans le calendrier officiel.</summary>
        Task<IReadOnlyList<EpreuveCalendrier>> GetEpreuvesAsync(CancellationToken ct = default);

        /// <summary>Toutes les lignes, validées et proposées (l'onglet « Sessions d'examens »).</summary>
        Task<IReadOnlyList<DateEpreuveOfficielle>> GetToutesAsync(CancellationToken ct = default);

        /// <summary>Les dates VALIDÉES de ces épreuves, par code : ce que voient les familles.</summary>
        Task<IReadOnlyDictionary<string, DateEpreuveOfficielle>> GetValideesAsync(
            IEnumerable<string> codes, CancellationToken ct = default);

        /// <summary>Pose la date validée d'une épreuve qui n'en a pas encore (semis) ; ne remplace jamais.</summary>
        Task SeedSiAbsenteAsync(DateEpreuveOfficielle date, CancellationToken ct = default);

        /// <summary>
        /// Pose (ou remplace) la proposition d'une épreuve. Faux si elle est identique à la date validée ou à la
        /// proposition déjà en attente : rien de nouveau à montrer.
        /// </summary>
        Task<bool> ProposerAsync(DateEpreuveOfficielle proposition, DateTime maintenant, CancellationToken ct = default);

        /// <summary>La proposition devient la date validée de son épreuve. Faux si elle n'existe pas.</summary>
        Task<bool> ValiderAsync(int propositionId, DateTime maintenant, CancellationToken ct = default);

        /// <summary>La proposition est écartée. Faux si elle n'existe pas.</summary>
        Task<bool> RejeterAsync(int propositionId, CancellationToken ct = default);
    }
}
