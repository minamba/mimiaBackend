using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainSignalement = SchoolWebApp.Domain.Models.Signalement;

namespace SchoolWebApp.Dal.Repositories
{
    public class SignalementRepository : ISignalementRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public SignalementRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DomainSignalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description,
            CancellationToken ct = default)
        {
            var entite = new Signalement
            {
                ParentId = parentId,
                EleveId = eleveId,
                Categorie = categorie,
                Description = description,
                Etat = "nouveau",
                DateCreation = DateTime.UtcNow,
            };

            _context.Signalements.Add(entite);
            await _context.SaveChangesAsync(ct);

            return await _context.Signalements
                .AsNoTracking()
                .Where(s => s.Id == entite.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<IEnumerable<DomainSignalement>> GetTousAsync(CancellationToken ct = default) =>
            await _context.Signalements
                .AsNoTracking()
                .OrderByDescending(s => s.DateCreation)
                .Select(Projection)
                .ToListAsync(ct);

        public async Task<DomainSignalement?> GetByIdAsync(int id, CancellationToken ct = default) =>
            await _context.Signalements
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);

        public async Task<DomainSignalement?> ModifierAsync(
            int id, string categorie, string description, string etat, CancellationToken ct = default)
        {
            var entite = await _context.Signalements.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entite is null) return null;

            entite.Categorie = categorie;
            entite.Description = description;
            entite.Etat = etat;
            entite.DateMiseAJour = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await _context.Signalements
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            var entite = await _context.Signalements.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entite is null) return false;

            _context.Signalements.Remove(entite);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static readonly Expression<Func<Signalement, DomainSignalement>> Projection =
            s => new DomainSignalement
            {
                Id = s.Id,
                ParentId = s.ParentId,
                EleveId = s.EleveId,
                Categorie = s.Categorie,
                Description = s.Description,
                Etat = s.Etat,
                DateCreation = s.DateCreation,
                DateMiseAJour = s.DateMiseAJour,
                ParentMail = s.Parent!.Mail,
                ElevePrenom = s.Eleve == null ? null : s.Eleve.Prenom,
            };
    }
}
