using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>Les rapports d'orientation — voir <see cref="IRapportOrientationRepository"/>.</summary>
    public class RapportOrientationRepository : IRapportOrientationRepository
    {
        /// <summary>
        /// Deux rapports d'une même conversation dans cette fenêtre sont la même
        /// séance (une conclusion suivie d'une autre, quelques secondes après).
        /// TROIS HEURES AVANT LE 12/10/2026, ET C'ÉTAIT FAUX : deux séances à une
        /// heure d'écart dans le même fil, et la seconde aurait remplacé le
        /// rapport de la première.
        /// </summary>
        public const int FenetreMinutes = 5;

        private readonly SchoolWebAppDatabaseContext _context;

        public RapportOrientationRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<RapportOrientationEleve> EnregistrerAsync(
            int eleveId, int? conversationId, RapportOrientationEleve rapport, CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;
            var depuis = maintenant.AddMinutes(-FenetreMinutes);

            var entite = conversationId is int id
                ? await _context.RapportsOrientation
                    .Where(r => r.EleveId == eleveId && r.ConversationId == id && r.DateCreation >= depuis)
                    .OrderByDescending(r => r.DateCreation)
                    .FirstOrDefaultAsync(ct)
                : null;

            if (entite is null)
            {
                entite = new RapportOrientation { EleveId = eleveId, ConversationId = conversationId };
                _context.RapportsOrientation.Add(entite);
            }

            entite.Discussion = rapport.Discussion;
            entite.Pistes = rapport.Pistes;
            entite.Chemin = rapport.Chemin;
            entite.Atouts = rapport.Atouts;
            entite.AConsolider = rapport.AConsolider;
            entite.Etapes = rapport.Etapes;
            entite.DateCreation = maintenant;
            // Réécrit, il redevient nouveau pour l'élève.
            entite.VuLe = null;

            await _context.SaveChangesAsync(ct);
            return Map(entite);
        }

        public async Task<IReadOnlyList<RapportOrientationEleve>> GetParEleveAsync(
            int eleveId, int limite, CancellationToken ct = default) =>
            (await _context.RapportsOrientation
                .AsNoTracking()
                .Where(r => r.EleveId == eleveId)
                .OrderByDescending(r => r.DateCreation)
                .Take(limite)
                .ToListAsync(ct))
            .Select(Map)
            .ToList();

        public Task<int> CompterNouveauxAsync(int eleveId, CancellationToken ct = default) =>
            _context.RapportsOrientation.CountAsync(r => r.EleveId == eleveId && r.VuLe == null, ct);

        public async Task MarquerVusAsync(int eleveId, CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;
            await _context.RapportsOrientation
                .Where(r => r.EleveId == eleveId && r.VuLe == null)
                .ExecuteUpdateAsync(m => m.SetProperty(r => r.VuLe, maintenant), ct);
        }

        private static RapportOrientationEleve Map(RapportOrientation r) => new()
        {
            Id = r.Id,
            ConversationId = r.ConversationId,
            Discussion = r.Discussion,
            Pistes = r.Pistes,
            Chemin = r.Chemin,
            Atouts = r.Atouts,
            AConsolider = r.AConsolider,
            Etapes = r.Etapes,
            DateCreation = r.DateCreation,
            VuLe = r.VuLe,
        };
    }
}
