using System.Linq.Expressions;
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
        private IQueryable<Entities.Parent> Joignables(string? categorie)
        {
            var requete = _context.Parents
                .AsNoTracking()
                .Where(p => p.Mail != null && p.Mail != ""
                            && !_context.MailsBannis.Any(b => b.Mail == p.Mail));

            // LE FILTRE D'ENVOI COMMERCIAL (19/11/2026) : une offre ne part qu'à qui a consenti. Posé ICI, une seule fois,
            // il vaut pour la diffusion à tous comme pour les courriels programmés par cible. Le consentement est LA
            // source de vérité : un ancien désabonnement des « annonces », antérieur, ne l'emporte pas sur un accord donné
            // depuis en cochant la case ; et le retrait, lui, remet `OffresEmail` à faux.
            if (CategorieDesabonnement.EstCommerciale(categorie)) return requete.Where(ConsentEnVigueur(categorie!));

            return categorie == null
                ? requete
                : requete.Where(p => !_context.DesabonnementsMail.Any(d => d.ParentId == p.Id && d.Categorie == categorie));
        }

        /// <summary>
        /// LE CONSENTEMENT AUX OFFRES EN VIGUEUR : la case cochée, ET aucune désinscription écrite APRÈS l'accord.
        ///
        /// LE PIÈGE DU RETOUR ARRIÈRE (20/11/2026, revue d'Omar). L'ancien code ne connaît pas `OffresEmail` : un parent
        /// qui se désinscrit pendant un retour arrière n'y laisse qu'une ligne `DesabonnementMail` « diffusion », et
        /// garde `OffresEmail = 1`. Sans ce test, la remise en ligne du lot lui renverrait les offres. Une ligne
        /// ANTÉRIEURE à l'accord, elle, ne compte pas : c'est l'ancien désabonnement des « annonces », dépassé par la case
        /// cochée depuis. Un accord sans date (impossible par le code) ne résiste à aucune ligne.
        ///
        /// UNE SEULE DÉFINITION, pour l'envoi (`Joignables`) comme pour l'état affiché (`EstDesabonneAsync`).
        /// </summary>
        private Expression<Func<Entities.Parent, bool>> ConsentEnVigueur(string categorie) =>
            p => p.OffresEmail
                 && !_context.DesabonnementsMail.Any(d =>
                     d.ParentId == p.Id
                     && d.Categorie == categorie
                     && (p.OffresEmailConsentiLe == null || d.DateCreation > p.OffresEmailConsentiLe));

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

        public async Task<IReadOnlyList<DestinataireAutomatique>> GetDestinatairesOffresAsync(
            CancellationToken ct = default)
        {
            // « OFFRES ET PROMOTIONS » : ni les bannis — ils ne reçoivent plus rien du
            // tout —, et SEULEMENT les parents qui ont consenti aux offres par email (19/11/2026).
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

        public async Task<IReadOnlyList<DestinataireAutomatique>> GetDestinatairesInformationAsync(
            CancellationToken ct = default)
        {
            // LE « MESSAGE DE DIFFUSION » VA À TOUS (décision de Min's, 20/11/2026) : le filtre sans catégorie — une
            // adresse, pas bannie —, SANS le consentement aux offres ni un ancien désabonnement des « annonces » : ce sont
            // les informations de Mimia que Min's envoie à tous les parents.
            var lignes = await Joignables(null)
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.Mail, p.Prenom })
                .ToListAsync(ct);

            return lignes
                .GroupBy(p => p.Mail!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new DestinataireAutomatique(g.First().Id, g.Key, g.First().Prenom, null, null))
                .ToList();
        }

        public Task<IReadOnlyList<DestinataireAutomatique>> GetDestinatairesAsync(string mode, CancellationToken ct = default) =>
            mode switch
            {
                ModeDiffusion.Information => GetDestinatairesInformationAsync(ct),
                ModeDiffusion.Offres => GetDestinatairesOffresAsync(ct),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Mode de diffusion inconnu."),
            };

        public async Task<IReadOnlyList<DestinataireAutomatique>> GetDestinatairesCibleAsync(
            string cible, string? categorie, DateTime maintenantUtc, CancellationToken ct = default)
        {
            var requete = Joignables(categorie);

            // Une formule payante en cours ; un essai en cours (la date fait foi :
            // un essai expiré reste « Actif » en base tant que personne ne l'a relu).
            requete = cible switch
            {
                CibleEnvoi.Tous => requete,
                CibleEnvoi.Abonnes => requete.Where(p => p.Abonnements.Any(a =>
                    a.Statut == StatutAbonnement.Actif && !a.Offre!.EstEssai)),
                CibleEnvoi.Essai => requete.Where(p =>
                    p.Abonnements.Any(a => a.Offre!.EstEssai && a.Statut != StatutAbonnement.Resilie
                                           && a.DateFin != null && a.DateFin > maintenantUtc)
                    && !p.Abonnements.Any(a => a.Statut == StatutAbonnement.Actif && !a.Offre!.EstEssai)),
                CibleEnvoi.SansFormule => requete.Where(p =>
                    !p.Abonnements.Any(a => a.Statut == StatutAbonnement.Actif && !a.Offre!.EstEssai)
                    && !p.Abonnements.Any(a => a.Offre!.EstEssai && a.Statut != StatutAbonnement.Resilie
                                               && a.DateFin != null && a.DateFin > maintenantUtc)),
                CibleEnvoi.Nouveaux => requete.Where(p => p.DateCreation >= maintenantUtc.AddDays(-CibleEnvoi.JoursNouveaux)),
                CibleEnvoi.Inactifs => requete.Where(p =>
                    p.Eleves.Any(e => e.ArchiveLe == null)
                    && !p.Eleves.Any(e => e.ArchiveLe == null && e.DerniereActivite != null
                                          && e.DerniereActivite >= maintenantUtc.AddDays(-CibleEnvoi.JoursInactifs))),
                _ => requete.Where(_ => false),
            };

            var lignes = await requete
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.Mail, p.Prenom })
                .ToListAsync(ct);

            return lignes
                .GroupBy(p => p.Mail!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new DestinataireAutomatique(g.First().Id, g.Key, g.First().Prenom, null, null))
                .ToList();
        }

        public async Task<DestinataireAutomatique?> GetDestinataireParMailAsync(
            string mail, string categorie, CancellationToken ct = default)
        {
            // LE MÊME FILTRE QUE LA DIFFUSION (`Joignables`) : une offre envoyée à un seul parent obéit à la même règle
            // qu'une offre envoyée à tous. Comparaison sans casse ni espaces, comme le dédoublonnage des listes.
            var adresse = mail?.Trim() ?? string.Empty;
            if (adresse.Length == 0) return null;

            var ligne = await Joignables(categorie)
                .Where(p => p.Mail!.Trim().ToLower() == adresse.ToLower())
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.Mail, p.Prenom })
                .FirstOrDefaultAsync(ct);

            return ligne is null ? null : new DestinataireAutomatique(ligne.Id, ligne.Mail!.Trim(), ligne.Prenom, null, null);
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

        /// <summary>
        /// Pour les offres (catégorie commerciale), « désabonné » veut dire « n'a pas de consentement en vigueur » : un
        /// parent qui n'a jamais coché la case ne reçoit rien, exactement comme celui qui s'est désinscrit — y compris par
        /// l'ancien code pendant un retour arrière (`ConsentEnVigueur`, la même règle que l'envoi).
        /// </summary>
        public async Task<bool> EstDesabonneAsync(int parentId, string categorie, CancellationToken ct = default) =>
            CategorieDesabonnement.EstCommerciale(categorie)
                ? !await _context.Parents.Where(p => p.Id == parentId).AnyAsync(ConsentEnVigueur(categorie), ct)
                : await _context.DesabonnementsMail.AnyAsync(d => d.ParentId == parentId && d.Categorie == categorie, ct);

        public async Task<bool> DesabonnerAsync(int parentId, string categorie, string source, CancellationToken ct = default)
        {
            // LE RETRAIT DU CONSENTEMENT, EFFET IMMÉDIAT (19/11/2026) : la date du retrait est la preuve. Aucune ligne de
            // désabonnement n'est écrite pour les offres : le consentement suffit, et une ligne survivrait à un nouvel
            // accord. Rien ne change pour qui n'avait pas consenti (pas de fausse date de retrait).
            if (CategorieDesabonnement.EstCommerciale(categorie))
            {
                // Déjà désinscrit pendant un retour arrière : ce retrait-là entre au journal, à sa date. Ce clic-ci ne
                // retire rien — le parent ne recevait déjà plus d'offres —, il n'ouvre donc aucun droit d'annulation.
                if (await ConsentementOffres.InscrireRetraitDuRetourArriereAsync(_context, parentId, ct)) return false;

                return await ConsentementOffres.ChangerAsync(_context, parentId, false, source, DateTime.UtcNow, ct);
            }

            if (await EstDesabonneAsync(parentId, categorie, ct)) return false;

            // Un lien qui désigne un compte supprimé depuis : rien à faire.
            if (!await _context.Parents.AnyAsync(p => p.Id == parentId, ct)) return false;

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
                return true;
            }
            catch (DbUpdateException)
            {
                // Deux clics simultanés : l'autre a gagné, le résultat est le même.
                _context.Entry(ligne).State = EntityState.Detached;
                return false;
            }
        }

        /// <summary>
        /// Pour les offres, se réabonner c'est CONSENTIR : un geste du parent lui-même (l'interrupteur de son espace, ou
        /// le bouton « me réabonner » juste après une désinscription, avec le jeton d'annulation), daté, avec sa source.
        /// Un consentement déjà en vigueur garde sa date d'origine ; la date d'un retrait passé reste, c'est l'historique.
        /// </summary>
        public async Task ReabonnerAsync(int parentId, string categorie, string source, CancellationToken ct = default)
        {
            // POUR LES OFFRES, L'ACCORD D'ABORD : `ConsentementOffres` inscrit au passage, à sa date, une désinscription faite
            // pendant un retour arrière (il la lit dans `DesabonnementMail`), puis le nouvel accord, daté, avec sa source.
            if (CategorieDesabonnement.EstCommerciale(categorie))
            {
                await ConsentementOffres.ChangerAsync(_context, parentId, true, source, DateTime.UtcNow, ct);
            }

            // Puis le ménage : pour les offres, il efface une éventuelle ligne d'avant le 19/11/2026 (désabonnement des
            // « annonces ») ou du retour arrière, désormais inscrite au journal.
            await _context.DesabonnementsMail
                .Where(d => d.ParentId == parentId && d.Categorie == categorie)
                .ExecuteDeleteAsync(ct);
        }
    }
}
