using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class MotsVerifiesRepository : IMotsVerifiesRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public MotsVerifiesRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool?> FiableAsync(string mot, CancellationToken ct = default, string langue = LanguesDesSons.Francais)
        {
            var justes = await _context.MotsVerifies
                .AsNoTracking()
                .Where(m => m.Mot == mot && m.Langue == langue)
                .Select(m => (int?)m.Justes)
                .FirstOrDefaultAsync(ct);

            return justes is null ? null : ReglesSonsVerifies.EstFiable(justes.Value);
        }

        public async Task<IReadOnlyList<string>> FiablesAsync(string langue, CancellationToken ct = default)
        {
            var mots = await _context.MotsVerifies
                .AsNoTracking()
                .Where(m => m.Langue == langue && m.Justes >= ReglesSonsVerifies.JustesMin)
                .Select(m => m.Mot)
                .ToListAsync(ct);

            mots.Sort(StringComparer.Ordinal);
            return mots;
        }

        public async Task EnregistrerAsync(string mot, int justes, int total, CancellationToken ct = default, string langue = LanguesDesSons.Francais)
        {
            var ligne = await _context.MotsVerifies.FirstOrDefaultAsync(m => m.Mot == mot && m.Langue == langue, ct);

            if (ligne is null)
            {
                ligne = new MotVerifie { Mot = mot, Langue = langue };
                _context.MotsVerifies.Add(ligne);
            }

            ligne.Justes = justes;
            ligne.Total = total;
            ligne.DateTest = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
        }
    }
}
