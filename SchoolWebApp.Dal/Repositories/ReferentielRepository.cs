using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainMatiere = SchoolWebApp.Domain.Models.Matiere;
using DomainNiveau = SchoolWebApp.Domain.Models.NiveauScolaire;

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
