using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainMatiere = SchoolWebApp.Domain.Models.Matiere;
using DomainNiveau = SchoolWebApp.Domain.Models.NiveauScolaire;
using DomainAcademie = SchoolWebApp.Domain.Models.Academie;
using DomainPeriodeVacances = SchoolWebApp.Domain.Models.PeriodeVacances;

namespace SchoolWebApp.Dal.Repositories
{
    public class ReferentielRepository : IReferentielRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ReferentielRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<DomainNiveau>> GetNiveauxScolairesAsync()
        {
            var entities = await _context.NiveauxScolaires
                .AsNoTracking()
                .OrderBy(n => n.Ordre)

                // LE TRI SECONDAIRE N EST PAS COSMÉTIQUE.
                //
                // Trois classes partagent désormais le rang 11. Sur le seul
                // rang, SQL Server rend un ordre non garanti : la liste
                // déroulante afficherait « Première professionnelle » avant
                // « Première générale » un jour, l inverse le lendemain, sans
                // qu une ligne de code ait bougé.
                //
                // L identifiant suit l ordre d insertion du semeur, qui va de
                // la voie générale à la voie professionnelle. C est donc lui
                // qui porte l ordre voulu, sur une base neuve comme sur une
                // base déjà semée où les trois classes générales sont les plus
                // anciennes.
                .ThenBy(n => n.Id)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        public async Task<DomainNiveau?> GetNiveauScolaireByIdAsync(int id)
        {
            var entity = await _context.NiveauxScolaires
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            return entity is null ? null : Map(entity);
        }

        public async Task<IEnumerable<DomainMatiere>> GetMatieresAsync(bool activesSeulement)
        {
            var query = _context.Matieres.AsNoTracking().AsQueryable();

            if (activesSeulement)
            {
                query = query.Where(m => m.Active);
            }

            var entities = await query.OrderBy(m => m.Ordre).ToListAsync();
            return entities.Select(Map).ToList();
        }

        public async Task<IEnumerable<DomainAcademie>> GetAcademiesAsync()
        {
            var entities = await _context.Academies
                .AsNoTracking()
                .OrderBy(a => a.Libelle)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        public async Task<IEnumerable<DomainPeriodeVacances>> GetPeriodesVacancesAsync(
            string? zone, DateTime debut, DateTime fin)
        {
            if (string.IsNullOrWhiteSpace(zone)) return [];

            // Recoupe l'intervalle demandé : une période commencée avant le
            // mois affiché mais qui déborde dedans (les vacances de Noël à
            // cheval sur décembre et janvier) doit apparaître aussi.
            var entities = await _context.PeriodesVacances
                .AsNoTracking()
                .Where(p => p.Zone == zone && p.DateDebut < fin && p.DateFin >= debut)
                .OrderBy(p => p.DateDebut)
                .ToListAsync();

            return entities.Select(MapPeriode).ToList();
        }

        public async Task<DomainPeriodeVacances?> GetProchainePeriodeVacancesAsync(string? zone, DateTime aujourdhui)
        {
            if (string.IsNullOrWhiteSpace(zone)) return null;

            // La première période dont la fin n'est pas encore passée : celle
            // en cours (l'enfant y est déjà, DateFin >= aujourd'hui) ou, à
            // défaut, la toute prochaine. Une fois une période terminée,
            // DateFin < aujourd'hui l'écarte d'elle-même — pas de calcul de
            // "suivante" à faire, le tri par DateDebut s'en charge.
            var entite = await _context.PeriodesVacances
                .AsNoTracking()
                .Where(p => p.Zone == zone && p.DateFin >= aujourdhui)
                .OrderBy(p => p.DateDebut)
                .FirstOrDefaultAsync();

            return entite is null ? null : MapPeriode(entite);
        }

        public async Task<IEnumerable<DomainPeriodeVacances>> GetToutesLesPeriodesVacancesAsync()
        {
            var entities = await _context.PeriodesVacances
                .AsNoTracking()
                .OrderBy(p => p.Zone).ThenBy(p => p.DateDebut)
                .ToListAsync();

            return entities.Select(MapPeriode).ToList();
        }

        public async Task<DomainPeriodeVacances> AjouterPeriodeVacancesAsync(
            string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin)
        {
            var entite = new PeriodeVacances
            {
                Zone = zone,
                AnneeScolaire = anneeScolaire,
                Libelle = libelle,
                DateDebut = debut,
                DateFin = fin,
            };

            _context.PeriodesVacances.Add(entite);
            await _context.SaveChangesAsync();

            return MapPeriode(entite);
        }

        public async Task<DomainPeriodeVacances?> ModifierPeriodeVacancesAsync(
            int id, string zone, string anneeScolaire, string libelle, DateTime debut, DateTime fin)
        {
            var entite = await _context.PeriodesVacances.FirstOrDefaultAsync(p => p.Id == id);
            if (entite is null) return null;

            entite.Zone = zone;
            entite.AnneeScolaire = anneeScolaire;
            entite.Libelle = libelle;
            entite.DateDebut = debut;
            entite.DateFin = fin;

            await _context.SaveChangesAsync();

            return MapPeriode(entite);
        }

        public async Task<bool> SupprimerPeriodeVacancesAsync(int id)
        {
            var entite = await _context.PeriodesVacances.FirstOrDefaultAsync(p => p.Id == id);
            if (entite is null) return false;

            _context.PeriodesVacances.Remove(entite);
            await _context.SaveChangesAsync();
            return true;
        }

        private static DomainPeriodeVacances MapPeriode(PeriodeVacances entity) => new()
        {
            Id = entity.Id,
            Zone = entity.Zone,
            AnneeScolaire = entity.AnneeScolaire,
            Libelle = entity.Libelle,
            DateDebut = entity.DateDebut,
            DateFin = entity.DateFin,
        };

        private static DomainAcademie Map(Academie entity) => new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Libelle = entity.Libelle,
            Zone = entity.Zone
        };

        private static DomainNiveau Map(NiveauScolaire entity) => new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Libelle = entity.Libelle,
            Cycle = entity.Cycle,
            Ordre = entity.Ordre
        };

        private static DomainMatiere Map(Matiere entity) => new()
        {
            Id = entity.Id,
            Code = entity.Code,
            Libelle = entity.Libelle,
            AgentSlug = entity.AgentSlug,
            ProfPrenom = entity.ProfPrenom,
            ProfAvatar = entity.ProfAvatar,
            ProfCouleur = entity.ProfCouleur,
            Promesse = entity.Promesse,
            Ordre = entity.Ordre,
            NiveauOrdreMin = entity.NiveauOrdreMin,
            NiveauOrdreMax = entity.NiveauOrdreMax,
            Active = entity.Active
        };
    }
}
