using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class ReglageRepository : IReglageRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ReglageRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTousAsync(
            CancellationToken ct = default) =>
            await _context.Reglages
                .AsNoTracking()
                .ToDictionaryAsync(r => r.Cle, r => r.Valeur, ct);

        public async Task<bool> EstActifAsync(
            string cle, bool parDefaut = false, CancellationToken ct = default)
        {
            var valeur = await _context.Reglages
                .AsNoTracking()
                .Where(r => r.Cle == cle)
                .Select(r => r.Valeur)
                .FirstOrDefaultAsync(ct);

            // Jamais posé : c'est l'appelant qui sait ce que « absent » veut
            // dire pour lui. Rien ne dit qu'un drapeau absent vaut « éteint ».
            return valeur is null
                ? parDefaut
                : string.Equals(valeur, "true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task DefinirAsync(string cle, bool actif, CancellationToken ct = default)
        {
            var reglage = await _context.Reglages.FirstOrDefaultAsync(r => r.Cle == cle, ct);

            if (reglage is null)
            {
                reglage = new Reglage { Cle = cle };
                _context.Reglages.Add(reglage);
            }

            reglage.Valeur = actif ? "true" : "false";
            reglage.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
        }

        public async Task<string?> LireAsync(string cle, CancellationToken ct = default) =>
            (await _context.Reglages
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Cle == cle, ct))?.Valeur;

        public async Task EcrireAsync(string cle, string valeur, CancellationToken ct = default)
        {
            var reglage = await _context.Reglages.FirstOrDefaultAsync(r => r.Cle == cle, ct);

            if (reglage is null)
            {
                reglage = new Reglage { Cle = cle };
                _context.Reglages.Add(reglage);
            }

            reglage.Valeur = valeur;
            reglage.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
        }
    }
}
