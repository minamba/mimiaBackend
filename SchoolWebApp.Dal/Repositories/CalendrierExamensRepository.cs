using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using M = SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>Les dates officielles des épreuves (25/11/2026) — voir <see cref="ICalendrierExamensRepository"/>.</summary>
    public class CalendrierExamensRepository : ICalendrierExamensRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public CalendrierExamensRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IReadOnlyList<M.EpreuveCalendrier>> GetEpreuvesAsync(CancellationToken ct = default) =>
            (await _context.EpreuvesExamens.AsNoTracking()
                .Where(e => e.Examen != null && e.Examen.Actif)
                .Select(e => new { e.Code, e.Libelle, ExamenCode = e.Examen!.Code, ExamenLibelle = e.Examen.Libelle, e.Examen.Session })
                .ToListAsync(ct))
            .Select(e => new M.EpreuveCalendrier(e.Code, e.Libelle, e.ExamenCode, e.ExamenLibelle, e.Session))
            .OrderBy(e => e.ExamenCode).ThenBy(e => e.Code)
            .ToList();

        public async Task<IReadOnlyList<M.DateEpreuveOfficielle>> GetToutesAsync(CancellationToken ct = default) =>
            (await _context.DatesEpreuvesOfficielles.AsNoTracking()
                .OrderBy(d => d.DateEpreuve).ThenBy(d => d.EpreuveCode).ThenBy(d => d.Etat)
                .ToListAsync(ct))
            .Select(VersModele).ToList();

        public async Task<IReadOnlyDictionary<string, M.DateEpreuveOfficielle>> GetValideesAsync(
            IEnumerable<string> codes, CancellationToken ct = default)
        {
            var liste = codes.Distinct().ToList();
            if (liste.Count == 0) return new Dictionary<string, M.DateEpreuveOfficielle>();

            return (await _context.DatesEpreuvesOfficielles.AsNoTracking()
                    .Where(d => d.Etat == M.CalendrierExamens.Validee && liste.Contains(d.EpreuveCode))
                    .ToListAsync(ct))
                .ToDictionary(d => d.EpreuveCode, VersModele);
        }

        public async Task SeedSiAbsenteAsync(M.DateEpreuveOfficielle date, CancellationToken ct = default)
        {
            var existe = await _context.DatesEpreuvesOfficielles
                .AnyAsync(d => d.EpreuveCode == date.EpreuveCode && d.Etat == M.CalendrierExamens.Validee, ct);
            if (existe) return;

            var ligne = VersEntite(date);
            ligne.Etat = M.CalendrierExamens.Validee;
            ligne.DateCreation = DateTime.UtcNow;
            ligne.ValideLe = DateTime.UtcNow;
            _context.DatesEpreuvesOfficielles.Add(ligne);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> ProposerAsync(M.DateEpreuveOfficielle proposition, DateTime maintenant, CancellationToken ct = default)
        {
            var lignes = await _context.DatesEpreuvesOfficielles
                .Where(d => d.EpreuveCode == proposition.EpreuveCode)
                .ToListAsync(ct);

            var validee = lignes.FirstOrDefault(d => d.Etat == M.CalendrierExamens.Validee);
            var enAttente = lignes.FirstOrDefault(d => d.Etat == M.CalendrierExamens.Proposee);

            // RIEN DE NOUVEAU : identique à ce que voient les familles (on retire alors une proposition périmée), ou à
            // ce qui attend déjà la validation.
            if (validee is not null && proposition.MemeDateQue(VersModele(validee)))
            {
                if (enAttente is not null)
                {
                    _context.DatesEpreuvesOfficielles.Remove(enAttente);
                    await _context.SaveChangesAsync(ct);
                }
                return false;
            }

            if (enAttente is not null && proposition.MemeDateQue(VersModele(enAttente))) return false;

            if (enAttente is not null) _context.DatesEpreuvesOfficielles.Remove(enAttente);

            var ligne = VersEntite(proposition);
            ligne.Etat = M.CalendrierExamens.Proposee;
            ligne.DateCreation = maintenant;
            ligne.ProposeLe = maintenant;
            ligne.ValideLe = null;
            _context.DatesEpreuvesOfficielles.Add(ligne);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ValiderAsync(int propositionId, DateTime maintenant, CancellationToken ct = default)
        {
            var proposition = await _context.DatesEpreuvesOfficielles
                .FirstOrDefaultAsync(d => d.Id == propositionId && d.Etat == M.CalendrierExamens.Proposee, ct);
            if (proposition is null) return false;

            var ancienne = await _context.DatesEpreuvesOfficielles
                .FirstOrDefaultAsync(d => d.EpreuveCode == proposition.EpreuveCode && d.Etat == M.CalendrierExamens.Validee, ct);

            // L'index unique (épreuve, état) : l'ancienne part AVANT que la proposition prenne sa place.
            if (ancienne is not null)
            {
                _context.DatesEpreuvesOfficielles.Remove(ancienne);
                await _context.SaveChangesAsync(ct);
            }

            proposition.Etat = M.CalendrierExamens.Validee;
            proposition.ValideLe = maintenant;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RejeterAsync(int propositionId, CancellationToken ct = default)
        {
            var proposition = await _context.DatesEpreuvesOfficielles
                .FirstOrDefaultAsync(d => d.Id == propositionId && d.Etat == M.CalendrierExamens.Proposee, ct);
            if (proposition is null) return false;

            _context.DatesEpreuvesOfficielles.Remove(proposition);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static M.DateEpreuveOfficielle VersModele(DateEpreuveOfficielle d) => new()
        {
            Id = d.Id,
            EpreuveCode = d.EpreuveCode,
            Etat = d.Etat,
            Nature = d.Nature,
            DateEpreuve = d.DateEpreuve,
            DateFin = d.DateFin,
            HeureDebut = d.HeureDebut,
            HeureFin = d.HeureFin,
            RemplacementLe = d.RemplacementLe,
            SourceLibelle = d.SourceLibelle,
            SourceUrl = d.SourceUrl,
            AnnexeUrl = d.AnnexeUrl,
            Extrait = d.Extrait,
            DateCreation = d.DateCreation,
            ProposeLe = d.ProposeLe,
            ValideLe = d.ValideLe,
        };

        private static DateEpreuveOfficielle VersEntite(M.DateEpreuveOfficielle d) => new()
        {
            EpreuveCode = d.EpreuveCode,
            Etat = d.Etat,
            Nature = d.Nature,
            DateEpreuve = d.DateEpreuve.Date,
            DateFin = d.DateFin?.Date,
            HeureDebut = d.HeureDebut,
            HeureFin = d.HeureFin,
            RemplacementLe = d.RemplacementLe?.Date,
            SourceLibelle = Tronquer(d.SourceLibelle, 300) ?? string.Empty,
            SourceUrl = Tronquer(d.SourceUrl, 500) ?? string.Empty,
            AnnexeUrl = Tronquer(d.AnnexeUrl, 500),
            Extrait = Tronquer(d.Extrait, 1000),
        };

        private static string? Tronquer(string? texte, int max) =>
            texte is null ? null : texte.Length <= max ? texte : texte[..max];
    }
}
