using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class EcheanceReferentielRepository : IEcheanceReferentielRepository
    {
        /// <summary>Les trois mots que porte `DernierStatutVeille` — écrits ici une fois.</summary>
        public const string StatutChangee = "changee";
        public const string StatutInchangee = "inchangee";
        public const string StatutInjoignable = "injoignable";

        private readonly SchoolWebAppDatabaseContext _context;

        public EcheanceReferentielRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<EcheanceASignaler>> GetAVeillerAsync(CancellationToken ct = default) =>
            await _context.EcheancesReferentiel
                .AsNoTracking()
                .Where(e => e.TraiteeLe == null && e.Url != null && e.Url != "")
                .OrderBy(e => e.DateEcheance)
                .Select(Projeter())
                .ToListAsync(ct);

        public async Task<IEnumerable<EcheanceASignaler>> GetASignalerAsync(
            DateTime maintenant, TimeSpan fenetre, TimeSpan espacementRappel, CancellationToken ct = default)
        {
            var horizon = maintenant + fenetre;
            var pasDeRappelRecent = maintenant - espacementRappel;

            return await _context.EcheancesReferentiel
                .AsNoTracking()
                .Where(e => e.TraiteeLe == null
                    && (e.DerniereAlerteLe == null || e.DerniereAlerteLe < pasDeRappelRecent)
                    // Une échéance datée qui approche, OU une page qui a
                    // changé — la sentinelle n'entre que par la seconde porte.
                    && ((!e.Sentinelle && e.DateEcheance <= horizon)
                        || e.DernierStatutVeille == StatutChangee))
                .OrderBy(e => e.DateEcheance)
                .Select(Projeter())
                .ToListAsync(ct);
        }

        public async Task MarquerAlerteEnvoyeeAsync(int id, DateTime maintenant, CancellationToken ct = default)
        {
            var echeance = await _context.EcheancesReferentiel.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (echeance is null) return;

            echeance.DerniereAlerteLe = maintenant;
            await _context.SaveChangesAsync(ct);
        }

        public async Task EnregistrerReleveAsync(
            int id, string hash, string? statut, DateTime maintenant, CancellationToken ct = default)
        {
            var echeance = await _context.EcheancesReferentiel.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (echeance is null) return;

            // COLLANT : un changement constaté hier et pas encore traité ne
            // redevient pas « inchangée » parce qu'aujourd'hui rien n'a bougé
            // DEPUIS HIER. Voir l'interface.
            var resteChangee = echeance.DernierStatutVeille == StatutChangee && statut != StatutChangee;

            echeance.DernierHashPage = hash;
            echeance.DernierStatutVeille = resteChangee ? StatutChangee : statut;
            echeance.DernierePageVerifieeLe = maintenant;
            await _context.SaveChangesAsync(ct);
        }

        public async Task EnregistrerEchecReleveAsync(int id, DateTime maintenant, CancellationToken ct = default)
        {
            var echeance = await _context.EcheancesReferentiel.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (echeance is null) return;

            // L'empreinte n'est PAS touchée : voir EcheanceReferentiel.DernierHashPage.
            // Un « changee » non traité non plus : une panne de réseau
            // n'efface pas ce qu'on a vu la veille.
            if (echeance.DernierStatutVeille != StatutChangee)
            {
                echeance.DernierStatutVeille = StatutInjoignable;
            }

            echeance.DernierePageVerifieeLe = maintenant;
            await _context.SaveChangesAsync(ct);
        }

        public async Task<int> CompterEnRetardAsync(DateTime maintenant, CancellationToken ct = default) =>
            await _context.EcheancesReferentiel
                .AsNoTracking()
                .CountAsync(e => !e.Sentinelle && e.DateConnue && e.TraiteeLe == null
                    && e.DateEcheance < maintenant, ct);

        public async Task<IEnumerable<EcheanceReferentielDetail>> GetToutesAsync(CancellationToken ct = default) =>
            await _context.EcheancesReferentiel
                .AsNoTracking()
                .OrderBy(e => e.DateEcheance)
                .Select(e => new EcheanceReferentielDetail(
                    e.Id, e.MatiereLibelle, e.NiveauxConcernes, e.DateEcheance, e.DateConnue, e.Sentinelle,
                    e.TexteOfficiel, e.Notes, e.Url, e.DernierStatutVeille, e.DernierePageVerifieeLe,
                    e.DerniereAlerteLe, e.TraiteeLe))
                .ToListAsync(ct);

        public async Task<bool> MarquerTraiteeAsync(int id, DateTime maintenant, CancellationToken ct = default)
        {
            var echeance = await _context.EcheancesReferentiel.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (echeance is null) return false;

            if (echeance.Sentinelle)
            {
                // Vue, pas close : le prochain relevé repart de l'empreinte
                // actuelle, et le prochain changement alertera à nouveau.
                echeance.DernierStatutVeille = null;
                echeance.DerniereAlerteLe = null;
            }
            else
            {
                echeance.TraiteeLe = maintenant;
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task SeedSiAbsenteAsync(
            string matiereLibelle, string niveauxConcernes, DateTime dateEcheance,
            bool dateConnue, string? texteOfficiel, string? notes, string? url = null,
            bool sentinelle = false, string? matieresCodes = null, string? niveauxCodes = null,
            CancellationToken ct = default)
        {
            // Clé naturelle : matière + niveaux + date. Le seed est rejoué à
            // chaque démarrage, comme le référentiel lui-même — sans ce
            // contrôle, chaque redémarrage dupliquerait la ligne.
            var existante = await _context.EcheancesReferentiel.FirstOrDefaultAsync(
                e => e.MatiereLibelle == matiereLibelle
                    && e.NiveauxConcernes == niveauxConcernes
                    && e.DateEcheance == dateEcheance,
                ct);

            if (existante is not null)
            {
                // L'ADRESSE SUIT LE CODE, PAS LA BASE. Une adresse corrigée
                // dans le seed (13/09/2026 : retour sur education.gouv.fr
                // après un détour par Légifrance) doit atteindre une ligne
                // déjà semée — sinon le worker relirait pour toujours la
                // page d'hier. L'empreinte est remise à zéro avec elle : deux
                // pages différentes ne se comparent pas.
                if (existante.Url != url)
                {
                    existante.Url = url;
                    existante.DernierHashPage = null;
                    existante.DernierStatutVeille = null;
                }

                // Les codes aussi suivent le code : une ligne semée avant
                // qu'ils existent (13/09/2026) les reçoit au démarrage suivant.
                existante.MatieresCodes = matieresCodes;
                existante.NiveauxCodes = niveauxCodes;

                await _context.SaveChangesAsync(ct);
                return;
            }

            _context.EcheancesReferentiel.Add(new EcheanceReferentiel
            {
                MatiereLibelle = matiereLibelle,
                NiveauxConcernes = niveauxConcernes,
                DateEcheance = dateEcheance,
                DateConnue = dateConnue,
                Sentinelle = sentinelle,
                TexteOfficiel = texteOfficiel,
                Url = url,
                MatieresCodes = matieresCodes,
                NiveauxCodes = niveauxCodes,
                Notes = notes,
                DateCreation = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync(ct);
        }

        private static System.Linq.Expressions.Expression<Func<EcheanceReferentiel, EcheanceASignaler>> Projeter() =>
            e => new EcheanceASignaler(
                e.Id, e.MatiereLibelle, e.NiveauxConcernes, e.DateEcheance, e.DateConnue, e.Sentinelle,
                e.TexteOfficiel, e.Notes, e.Url, e.DernierHashPage, e.DernierStatutVeille);
    }
}
