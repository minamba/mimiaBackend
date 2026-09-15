using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>Le référentiel lu pour l'administration — voir <see cref="ProgrammeScolaireAdmin"/>.</summary>
    public interface IProgrammeScolaireRepository
    {
        Task<ProgrammeScolaireAdmin> GetProgrammeAsync(CancellationToken ct = default);
    }
}
