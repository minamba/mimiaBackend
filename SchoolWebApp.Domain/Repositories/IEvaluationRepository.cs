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
        /// La note est donnée, la correction orale est remise au cours suivant.
        /// Faux si l'évaluation n'appartient pas à cet élève.
        ///
        /// Méthode à part plutôt qu'un paramètre de plus à <see cref="AjouterAsync"/> :
        /// un report est un fait qui ne concerne qu'une minorité de copies, et
        /// allonger la signature de l'enregistrement pour elles l'aurait rendue
        /// fragile à chaque appel.
        /// </summary>
        Task<bool> ReporterCorrectionAsync(
            int eleveId, int evaluationId, CancellationToken ct = default);

        /// <summary>
        /// L'évaluation la plus récente de cette matière dont la correction
        /// attend encore, et qu'on n'a pas déjà proposée autant de fois qu'on se
        /// l'autorise. Null s'il n'y en a pas — le cas ordinaire.
        ///
        /// La plus récente et pas la plus ancienne : c'est la dernière copie
        /// dont l'enfant se souvient, même règle que pour la dictée en attente.
        /// </summary>
        Task<EvaluationEleve?> GetCorrectionEnAttenteAsync(
            int eleveId, int matiereId, CancellationToken ct = default);

        /// <summary>
        /// Compte une proposition de plus. C'est ce compteur, et non la bonne
        /// volonté du modèle, qui garantit qu'on cesse de proposer : passé le
        /// plafond, <see cref="GetCorrectionEnAttenteAsync"/> ne rend plus
        /// cette évaluation.
        /// </summary>
        Task<bool> MarquerRelanceCorrectionAsync(
            int eleveId, int evaluationId, CancellationToken ct = default);

        /// <summary>
        /// La correction a eu lieu — ou l'élève n'a pas voulu : on n'en reparle
        /// plus. Faux si l'évaluation n'appartient pas à cet élève.
        /// </summary>
        Task<bool> CloreCorrectionAsync(
            int eleveId, int evaluationId, CancellationToken ct = default);

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
