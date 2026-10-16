using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le quota de fiches de révision et les crédits de la famille — voir
    /// `FichesRevisionEleve.CoursParMois` et `CreditsFiches`.
    /// </summary>
    public interface ICreditsFichesRepository
    {
        Task<int?> ParentDeLEleveAsync(int eleveId, CancellationToken ct = default);

        /// <summary>Les cours de l'enfant décomptés sur son mois depuis `depuis`.</summary>
        Task<int> CompterDuMoisAsync(int eleveId, DateTime depuis, CancellationToken ct = default);

        /// <summary>Les places prises sur les crédits de la famille par cet enfant depuis cette date.</summary>
        Task<int> CompterCreditsPrisAsync(int eleveId, DateTime depuis, CancellationToken ct = default);

        Task<int> SoldeAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Les cours de l'enfant en route (à lire, notions à choisir, à générer) :
        /// ils réservent leur place dans le quota avant d'être décomptés.
        /// </summary>
        Task<int> CompterEnCoursAsync(int eleveId, CancellationToken ct = default);

        /// <summary>Les enfants de la famille, leurs cours décomptés depuis `depuis` et leurs cours en route.</summary>
        Task<IReadOnlyList<EnfantFiches>> EnfantsAsync(int parentId, DateTime depuis, CancellationToken ct = default);

        /// <summary>
        /// Décompte UN cours dont les fiches sont prêtes : sur le mois tant qu'il en reste
        /// (`maximum` depuis `depuis`), sinon sur un crédit de la famille.
        /// </summary>
        Task<ImputationFiche> ImputerAsync(
            int eleveId, int parentId, int? ficheId, DateTime depuis, int maximum, DateTime maintenant,
            CancellationToken ct = default);

        /// <summary>
        /// L'ESPACE PARENT (16/10/2026) : un cours de parent prêt prend UN crédit
        /// de la famille, sans passer par le mois d'un enfant. Faux si le solde
        /// était déjà à zéro (pris en même temps ailleurs).
        /// </summary>
        Task<bool> DebiterFamilleAsync(int parentId, DateTime maintenant, CancellationToken ct = default);

        Task<IReadOnlyList<OffreFichesTarif>> GetOffresAsync(CancellationToken ct = default);

        Task EnregistrerPrixStripeAsync(string code, string prixId, CancellationToken ct = default);

        /// <summary>
        /// Crédite un pack à la famille. Rend le nouveau solde, ou null quand le
        /// pack est inconnu ou que cette session Stripe a déjà été créditée.
        /// Sans session (compte exempté), le crédit est direct.
        /// </summary>
        Task<int?> CrediterAsync(
            int parentId, string codePack, string? sessionStripeId, string? paiementStripeId,
            CancellationToken ct = default);

        /// <summary>
        /// Remboursement total d'un achat : les crédits non encore utilisés sont
        /// repris (le solde ne descend jamais sous zéro). Faux si ce paiement
        /// n'était pas un achat de fiches, ou déjà repris.
        /// </summary>
        Task<bool> RembourserAsync(string paiementStripeId, CancellationToken ct = default);

        /// <summary>Les mouvements de crédits d'une famille, du plus récent au plus ancien.</summary>
        Task<IReadOnlyList<LigneCreditsFiches>> HistoriqueAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Ajoute (positif) ou retire (négatif) des crédits à la main, avec un
        /// motif. Rend le nouveau solde, ou null pour un retrait supérieur au
        /// solde — on ne descend jamais sous zéro.
        /// </summary>
        Task<int?> AjusterAsync(int parentId, int credits, string motif, CancellationToken ct = default);
    }
}
