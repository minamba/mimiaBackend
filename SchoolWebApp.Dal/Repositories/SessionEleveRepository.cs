using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainSession = SchoolWebApp.Domain.Models.SessionEleve;

namespace SchoolWebApp.Dal.Repositories
{
    public class SessionEleveRepository : ISessionEleveRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public SessionEleveRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DomainSession> OuvrirAsync(
            int eleveId, string jetonHache, string? appareil, CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;

            var entity = new SessionEleve
            {
                EleveId = eleveId,
                JetonHache = jetonHache,
                DateCreation = maintenant,
                DernierAcces = maintenant,
                Appareil = appareil?.Length > 200 ? appareil[..200] : appareil,
            };

            _context.SessionsEleves.Add(entity);
            await _context.SaveChangesAsync(ct);

            return await TrouverAsync(jetonHache, ct)
                   ?? new DomainSession { Id = entity.Id, EleveId = eleveId };
        }

        /// <summary>
        /// Une seule requête, une seule projection.
        ///
        /// Elle est faite à CHAQUE appel d'un enfant : la charger avec des
        /// `Include` inutiles se paierait sur chaque message d'un cours d'une
        /// demi-heure.
        /// </summary>
        public Task<DomainSession?> TrouverAsync(string jetonHache, CancellationToken ct = default) =>
            _context.SessionsEleves
                .AsNoTracking()
                .Where(s => s.JetonHache == jetonHache)
                .Select(s => new DomainSession
                {
                    Id = s.Id,
                    EleveId = s.EleveId,
                    ParentId = s.Eleve!.ParentId,
                    PrenomEleve = s.Eleve.Prenom,
                    DateCreation = s.DateCreation,
                    DernierAcces = s.DernierAcces,
                    Appareil = s.Appareil,
                })
                .FirstOrDefaultAsync(ct);

        public Task ToucherAsync(int sessionId, CancellationToken ct = default) =>
            _context.SessionsEleves
                .Where(s => s.Id == sessionId)
                .ExecuteUpdateAsync(
                    m => m.SetProperty(s => s.DernierAcces, DateTime.UtcNow), ct);

        public Task FermerAsync(string jetonHache, CancellationToken ct = default) =>
            _context.SessionsEleves
                .Where(s => s.JetonHache == jetonHache)
                .ExecuteDeleteAsync(ct);

        public Task<int> FermerToutesAsync(int eleveId, CancellationToken ct = default) =>
            _context.SessionsEleves
                .Where(s => s.EleveId == eleveId)
                .ExecuteDeleteAsync(ct);
    }
}
