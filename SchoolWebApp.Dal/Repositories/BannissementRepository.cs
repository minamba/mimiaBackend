using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class BannissementRepository : IBannissementRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public BannissementRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// La forme sous laquelle une adresse est stockée et comparée.
        ///
        /// UNE SEULE FONCTION, TRAVERSÉE PAR TOUS LES CHEMINS. Écrire la
        /// normalisation en trois endroits garantit qu'ils divergeront : celui
        /// qui bannit mettrait en minuscules, celui qui lève ne le ferait pas,
        /// et le bannissement deviendrait impossible à retirer.
        ///
        /// Le serveur d'identité applique la MÊME règle de son côté, en SQL.
        /// C'est la seule duplication du projet qu'on ne peut pas supprimer —
        /// il ne référence pas cet assemblage — et elle est signalée des deux
        /// côtés.
        /// </summary>
        private static string Normaliser(string? mail) =>
            (mail ?? string.Empty).Trim().ToLowerInvariant();

        public async Task<IEnumerable<MailBanniVue>> GetTousAsync(CancellationToken ct = default)
        {
            var bannis = await _context.MailsBannis
                .AsNoTracking()
                .OrderByDescending(b => b.DateCreation)
                .Select(b => new MailBanniVue
                {
                    Id = b.Id,
                    Mail = b.Mail,
                    Motif = b.Motif,
                    DateCreation = b.DateCreation,
                    BanniPar = b.BanniPar,
                })
                .ToListAsync(ct);

            if (bannis.Count == 0) return bannis;

            // UNE SEULE REQUÊTE POUR TOUTE LA LISTE, et non une par ligne : la
            // version naïve interrogerait la base autant de fois qu'il y a de
            // bannis, et l'écran ralentirait à mesure que la liste s'allonge.
            var adresses = bannis.Select(b => b.Mail).ToList();

            var existants = await _context.Parents
                .AsNoTracking()
                .Where(p => adresses.Contains(p.Mail!))
                .Select(p => p.Mail!)
                .ToListAsync(ct);

            var ensemble = existants
                .Select(m => m.Trim().ToLowerInvariant())
                .ToHashSet();

            foreach (var banni in bannis) banni.CompteExiste = ensemble.Contains(banni.Mail);

            return bannis;
        }

        public async Task<bool> EstBanniAsync(string? mail, CancellationToken ct = default)
        {
            var propre = Normaliser(mail);

            if (propre.Length == 0) return false;

            return await _context.MailsBannis
                .AsNoTracking()
                .AnyAsync(b => b.Mail == propre, ct);
        }

        public async Task<bool> BannirAsync(
            string mail, string? motif, string? par, CancellationToken ct = default)
        {
            var propre = Normaliser(mail);

            if (propre.Length == 0) return false;

            // Déjà présent : le résultat voulu est atteint, on ne double pas la
            // ligne. L'index unique refuserait de toute façon.
            if (await _context.MailsBannis.AnyAsync(b => b.Mail == propre, ct)) return false;

            _context.MailsBannis.Add(new MailBanni
            {
                Mail = propre,
                Motif = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim(),
                BanniPar = string.IsNullOrWhiteSpace(par) ? null : par.Trim(),
                DateCreation = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> LeverAsync(string mail, CancellationToken ct = default)
        {
            var propre = Normaliser(mail);

            var ligne = await _context.MailsBannis.FirstOrDefaultAsync(b => b.Mail == propre, ct);
            if (ligne is null) return false;

            // EFFACÉE, PAS DATÉE. Un bannissement levé ne laisse pas de trace :
            // garder la ligne pour l'histoire ferait conserver une donnée
            // personnelle sur quelqu'un dont on vient de décider qu'il n'a plus
            // rien à faire ici.
            _context.MailsBannis.Remove(ligne);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
