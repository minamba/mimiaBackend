using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IRapportRepository
    {
        /// <summary>
        /// Enregistre le compte rendu d'une séance.
        ///
        /// Idempotent par séance : un professeur peut conclure deux fois — un
        /// au revoir, puis l'échéance du minuteur — et un cours ne doit pas
        /// produire deux rapports. Le second écrase le premier, qui était
        /// forcément moins complet.
        ///
        /// Null si la conversation n'existe pas ou n'appartient pas à l'élève.
        /// </summary>
        Task<RapportEleve?> EnregistrerAsync(
            int eleveId,
            int conversationId,
            string? travaille,
            double? noteComprehension,
            double? noteRevision,
            string? remarque,
            string? aRevoir,
            CancellationToken ct = default);

        /// <summary>
        /// Une tranche de l'historique des comptes rendus. `curseur` null
        /// demande la première ; la suivante se demande avec le curseur rendu
        /// par la précédente.
        ///
        /// Le filtre et l'ordre s'appliquent à l'historique entier, et le
        /// curseur n'est valable que pour le couple qui l'a produit : changer
        /// l'un ou l'autre repart de la première tranche.
        /// </summary>
        /// <param name="matiereId">Null = toutes les matières.</param>
        /// <param name="duPlusAncien">Faux = le plus récent d'abord.</param>
        Task<PageHistorique<RapportEleve>> GetHistoriqueAsync(
            int eleveId,
            string? curseur,
            int taille,
            int? matiereId = null,
            bool duPlusAncien = false,
            CancellationToken ct = default);

        /// <summary>Les rapports d'un élève, le plus récent d'abord.</summary>
        Task<IEnumerable<RapportEleve>> GetParEleveAsync(
            int eleveId, int limite, CancellationToken ct = default);

        /// <summary>Un rapport complet, avec l'identité de l'élève, pour l'imprimer.</summary>
        Task<RapportEleve?> GetDetailAsync(
            int rapportId, int eleveId, CancellationToken ct = default);

        /// <summary>Les rapports d'une tranche de temps. Sert au bilan hebdomadaire.</summary>
        Task<IEnumerable<RapportEleve>> GetEntreAsync(
            int eleveId, DateTime debut, DateTime fin, CancellationToken ct = default);
    }
}
