using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IEvaluationPrevueRepository
    {
        /// <summary>
        /// Enregistre un report d'évaluation. Null si la conversation
        /// n'appartient pas à cet élève.
        /// </summary>
        Task<EvaluationPrevueEleve?> EnregistrerAsync(
            int eleveId, int conversationId, string? notion, CancellationToken ct = default);

        /// <summary>
        /// Les évaluations prévues encore attendues pour cet élève, toutes
        /// matières confondues — pas de correction reçue depuis, et pas
        /// trop anciennes (voir `EvaluationPrevueRepository`).
        /// </summary>
        Task<IEnumerable<EvaluationPrevueEleve>> GetEnAttenteAsync(
            int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Une vraie évaluation vient d'être enregistrée pour cette matière :
        /// toute évaluation prévue encore ouverte pour cet élève et cette
        /// matière est marquée consommée.
        /// </summary>
        Task MarquerConsommeesAsync(int eleveId, int matiereId, CancellationToken ct = default);
    }
}
