using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class SignalementService : ISignalementService
    {
        private readonly ISignalementRepository _signalements;

        public SignalementService(ISignalementRepository signalements)
        {
            _signalements = signalements ?? throw new ArgumentNullException(nameof(signalements));
        }

        public Task<Signalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description, CancellationToken ct = default) =>
            _signalements.CreerAsync(parentId, eleveId, categorie, description, ct);

        public Task<IEnumerable<Signalement>> GetTousAsync(CancellationToken ct = default) =>
            _signalements.GetTousAsync(ct);

        public Task<Signalement?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _signalements.GetByIdAsync(id, ct);

        public Task<Signalement?> ModifierAsync(
            int id, string categorie, string description, string etat, CancellationToken ct = default) =>
            _signalements.ModifierAsync(id, categorie, description, etat, ct);

        public Task<bool> SupprimerAsync(int id, CancellationToken ct = default) =>
            _signalements.SupprimerAsync(id, ct);
    }
}
