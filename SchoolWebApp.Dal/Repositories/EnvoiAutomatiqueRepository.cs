using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class EnvoiAutomatiqueRepository : IEnvoiAutomatiqueRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public EnvoiAutomatiqueRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Les parents qu'on a le droit d'écrire pour une catégorie : une
        /// adresse, pas bannie, pas désabonnée.
        ///
        /// ÉCRIT UNE FOIS, partagé par toutes les requêtes : un filtre recopié
        /// quatre fois finit toujours par manquer à l'une d'elles — et c'est
        /// alors un parent désabonné qui reçoit quand même.
        /// </summary>
        private IQueryable<Entities.Parent> Joignables(string? categorie) =>
            _context.Parents
                .AsNoTracking()
                .Where(p => p.Mail != null && p.Mail != ""
                            && !_context.MailsBannis.Any(b => b.Mail == p.Mail)
                            && (categorie == null
                                || !_context.DesabonnementsMail.Any(d => d.ParentId == p.Id && d.Categorie == categorie)));

        public async Task<IReadOnlyList<DestinataireAutomatique>> GetEssaisExpirantAsync(
            DateTime debutUtc, DateTime finUtc, DateTime maintenantUtc, int limite, CancellationToken ct = default)
        {
            // LA FIN D'ESSAI SE LIT SUR LA DATE, PAS SUR LE STATUT : un essai
            // expiré reste « Actif » en base tant que personne ne l'a relu (la
            // bascule est paresseuse). `Resilie` écarte en revanche un essai
            // interrompu par le choix d'une formule payante.
            var essais = await _context.Abonnements
                .AsNoTracking()
                .Where(a => a.Offre!.EstEssai
                            && a.Statut != StatutAbonnement.Resilie
                            && a.DateFin != null
                            && a.DateFin >= debutUtc
                            && a.DateFin < finUtc
                            && a.DateFin > maintenantUtc)
                .OrderBy(a => a.DateFin)
                .Select(a => new { a.Id, a.ParentId, a.DateFin })
                .ToListAsync(ct);

            if (essais.Count == 0) return [];

            var cles = essais.Select(e => $"abonnement:{e.Id}").ToList();

            var dejaPrevenus = await _context.EnvoisAutomatiques
                .AsNoTracking()
                .Where(e => e.CodeModele == CodeModeleMail.FinEssai && e.Cle != null && cles.Contains(e.Cle))
                .Select(e => e.Cle!)
                .ToListAsync(ct);

            var parentsIds = essais.Select(e => e.ParentId).Distinct().ToList();

            // UN PARENT QUI A DÉJÀ UNE FORMULE PAYANTE n'a rien à apprendre de la
            // fin de son essai.
            var parents = await Joignables(null)
                .Where(p => parentsIds.Contains(p.Id)
                            && !p.Abonnements.Any(b => !b.Offre!.EstEssai && b.Statut != StatutAbonnement.Resilie))
                .Select(p => new { p.Id, p.Mail, p.Prenom })
                .ToDictionaryAsync(p => p.Id, ct);

            return essais
                .Where(e => !dejaPrevenus.Contains($"abonnement:{e.Id}") && parents.ContainsKey(e.ParentId))
                .Take(limite)
                .Select(e => new DestinataireAutomatique(
                    e.ParentId, parents[e.ParentId].Mail!, parents[e.ParentId].Prenom, e.Id, e.DateFin))
                .ToList();
        }

        public async Task<IReadOnlyList<DestinataireAutomatique>> GetAvisEssaiAsync(
            DateTime inscritApresUtc, DateTime inscritAvantUtc, int limite, CancellationToken ct = default) =>
            await Joignables(CategorieDesabonnement.Avis)
                .Where(p => p.DateCreation > inscritApresUtc
                            && p.DateCreation <= inscritAvantUtc
                            && p.Abonnements.Any(a => a.Offre!.EstEssai)
                            && !_context.AvisClients.Any(v => v.ParentId == p.Id)
                            && !_context.EnvoisAutomatiques.Any(e =>
                                e.ParentId == p.Id && e.CodeModele == CodeModeleMail.AvisEssai))
                .OrderBy(p => p.DateCreation)
                .Take(limite)
                .Select(p => new DestinataireAutomatique(p.Id, p.Mail!, p.Prenom, null, null))
                .ToListAsync(ct);

        public async Task<IReadOnlyList<DestinataireAutomatique>> GetAvisGeneralAsync(
            DateTime depuisUtc, int limite, CancellationToken ct = default) =>
            await Joignables(CategorieDesabonnement.Avis)
                .Where(p => p.Abonnements.Any(a => a.Statut == StatutAbonnement.Actif && !a.Offre!.EstEssai)
                            && !_context.AvisClients.Any(v => v.ParentId == p.Id)

                            // LA FENÊTRE COMPTE LES DEUX DEMANDES D'AVIS : un parent
                            // sollicité à la fin de son essai n'a pas à l'être de
                            // nouveau le mois suivant.
                            && !_context.EnvoisAutomatiques.Any(e =>
                                e.ParentId == p.Id
                                && (e.CodeModele == CodeModeleMail.AvisGeneral || e.CodeModele == CodeModeleMail.AvisEssai)
                                && e.DateEnvoi > depuisUtc))
                .OrderBy(p => p.Id)
                .Take(limite)
                .Select(p => new DestinataireAutomatique(p.Id, p.Mail!, p.Prenom, null, null))
                .ToListAsync(ct);

        public async Task<IReadOnlyList<DestinataireAutomatique>> GetDestinatairesDiffusionAsync(
            CancellationToken ct = default)
        {
            // Ni les bannis — ils ne reçoivent plus rien du tout —, ni ceux qui se
            // sont désabonnés des annonces.
            var lignes = await Joignables(CategorieDesabonnement.Diffusion)
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.Mail, p.Prenom })
                .ToListAsync(ct);

            // Une même adresse sur deux comptes ne reçoit qu'un courriel.
            return lignes
                .GroupBy(p => p.Mail!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new DestinataireAutomatique(g.First().Id, g.Key, g.First().Prenom, null, null))
                .ToList();
        }

        public async Task<int?> ReserverAsync(
            string codeModele, int parentId, string? cle, DateTime occurrenceUtc, CancellationToken ct = default)
        {
            var ligne = new EnvoiAutomatique
            {
                CodeModele = codeModele,
                ParentId = parentId,
                Cle = cle,
                Occurrence = occurrenceUtc,
                DateEnvoi = DateTime.UtcNow,
                Statut = StatutEnvoiAutomatique.Reserve,
            };

            _context.EnvoisAutomatiques.Add(ligne);

            try
            {
                await _context.SaveChangesAsync(ct);
                return ligne.Id;
            }
            catch (DbUpdateException)
            {
                // L'index unique a refusé : ce courriel est déjà parti. On
                // détache la ligne, sinon le prochain `SaveChanges` de ce
                // contexte retenterait l'insertion et échouerait à son tour.
                _context.Entry(ligne).State = EntityState.Detached;
                return null;
            }
        }

        public Task MarquerAsync(int id, string statut, CancellationToken ct = default) =>
            _context.EnvoisAutomatiques
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Statut, statut), ct);

        public Task<bool> EstDesabonneAsync(int parentId, string categorie, CancellationToken ct = default) =>
            _context.DesabonnementsMail.AnyAsync(d => d.ParentId == parentId && d.Categorie == categorie, ct);

        public async Task DesabonnerAsync(int parentId, string categorie, CancellationToken ct = default)
        {
            if (await EstDesabonneAsync(parentId, categorie, ct)) return;

            // Un lien qui désigne un compte supprimé depuis : rien à faire.
            if (!await _context.Parents.AnyAsync(p => p.Id == parentId, ct)) return;

            var ligne = new DesabonnementMail
            {
                ParentId = parentId,
                Categorie = categorie,
                DateCreation = DateTime.UtcNow,
            };

            _context.DesabonnementsMail.Add(ligne);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Deux clics simultanés : l'autre a gagné, le résultat est le même.
                _context.Entry(ligne).State = EntityState.Detached;
            }
        }

        public Task ReabonnerAsync(int parentId, string categorie, CancellationToken ct = default) =>
            _context.DesabonnementsMail
                .Where(d => d.ParentId == parentId && d.Categorie == categorie)
                .ExecuteDeleteAsync(ct);
    }
}
