using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IEvaluationRepository
    {
        /// <summary>
        /// Enregistre une évaluation. Retourne null si la conversation n'existe
        /// pas ou n'appartient pas à cet élève — le professeur peut halluciner
        /// une balise, la base n'a pas à s'en accommoder.
        /// </summary>
        Task<EvaluationEleve?> AjouterAsync(
            int eleveId,
            int conversationId,
            string? notion,
            double note,
            string? remarque,
            string? aRevoir,
            IEnumerable<QuestionEvaluation>? questions,
            CancellationToken ct = default);

        /// <summary>
        /// Une évaluation complète, avec le détail des questions et l'identité
        /// de l'élève : de quoi imprimer la copie. Null si elle n'existe pas ou
        /// n'appartient pas à cet élève.
        /// </summary>
        Task<EvaluationEleve?> GetCopieAsync(
            int evaluationId, int eleveId, CancellationToken ct = default);

        /// <summary>Les évaluations d'un élève, la plus récente d'abord.</summary>
        Task<IEnumerable<EvaluationEleve>> GetParEleveAsync(
            int eleveId, int limite, CancellationToken ct = default);

        /// <summary>
        /// Une tranche de l'historique des évaluations. `curseur` null demande
        /// la première ; la suivante se demande avec le curseur rendu par la
        /// précédente.
        ///
        /// Le filtre et l'ordre s'appliquent à l'historique entier, et le
        /// curseur n'est valable que pour le couple qui l'a produit : changer
        /// l'un ou l'autre repart de la première tranche.
        /// </summary>
        /// <param name="matiereId">Null = toutes les matières.</param>
        /// <param name="duPlusAncien">Faux = le plus récent d'abord.</param>
        Task<PageHistorique<EvaluationEleve>> GetHistoriqueAsync(
            int eleveId,
            string? curseur,
            int taille,
            int? matiereId = null,
            bool duPlusAncien = false,
            int? niveauScolaireId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Les évaluations passées dans une tranche de temps. Sert au bilan
        /// hebdomadaire : le parent reçoit les notes de la semaine, pas toutes.
        /// </summary>
        Task<IEnumerable<EvaluationEleve>> GetEntreAsync(
            int eleveId, DateTime debut, DateTime fin, CancellationToken ct = default);

        /// <summary>
        /// Progression par matière : la courbe des notes et l'état des notions.
        /// Une entrée par matière où l'élève a laissé une trace.
        /// </summary>
        Task<IEnumerable<ProgressionMatiere>> GetProgressionAsync(
            int eleveId, int? niveauScolaireId = null, CancellationToken ct = default);
    }
}
