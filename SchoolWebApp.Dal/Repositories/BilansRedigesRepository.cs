using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class BilansRedigesRepository : IBilansRedigesRepository
    {
        /// <summary>
        /// On ne relit que la dernière semaine ; douze suffisent largement à
        /// un renvoi tardif, et la table ne grossit pas avec les années.
        /// </summary>
        private const int SemainesGardees = 12;

        private readonly SchoolWebAppDatabaseContext _context;

        public BilansRedigesRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<string?> LireAsync(int eleveId, DateTime finSemaine, CancellationToken ct = default)
        {
            var jour = finSemaine.Date;
            return await _context.BilansRediges
                .AsNoTracking()
                .Where(b => b.EleveId == eleveId && b.FinSemaine == jour)
                .Select(b => b.Reponse)
                .FirstOrDefaultAsync(ct);
        }

        public async Task EnregistrerAsync(int eleveId, DateTime finSemaine, string reponse, CancellationToken ct = default)
        {
            var jour = finSemaine.Date;
            var existant = await _context.BilansRediges
                .FirstOrDefaultAsync(b => b.EleveId == eleveId && b.FinSemaine == jour, ct);

            if (existant is null)
            {
                _context.BilansRediges.Add(new BilanRedige
                {
                    EleveId = eleveId,
                    FinSemaine = jour,
                    Reponse = reponse,
                    DateCreation = DateTime.UtcNow,
                });
            }
            else
            {
                existant.Reponse = reponse;
            }

            await _context.SaveChangesAsync(ct);

            var limite = jour.AddDays(-7 * SemainesGardees);
            await _context.BilansRediges
                .Where(b => b.EleveId == eleveId && b.FinSemaine < limite)
                .ExecuteDeleteAsync(ct);
        }
    }
}
