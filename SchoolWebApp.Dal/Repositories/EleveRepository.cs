using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainEleve = SchoolWebApp.Domain.Models.Eleve;

namespace SchoolWebApp.Dal.Repositories
{
    public class EleveRepository : IEleveRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public EleveRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<DomainEleve>> GetElevesAsync()
        {
            var entities = await _context.Eleves
                .AsNoTracking()
                .Include(e => e.NiveauScolaire)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        public async Task<IEnumerable<DomainEleve>> GetElevesByParentAsync(int parentId)
        {
            var entities = await _context.Eleves
                .AsNoTracking()
                .Include(e => e.NiveauScolaire)
                // Les profils retirés ne remontent pas : le parent les a mis de
                // côté, il ne doit pas les retrouver dans la liste de ses
                // enfants. Ils restent en base pour les consommations.
                .Where(e => e.ParentId == parentId && e.ArchiveLe == null)
                .OrderBy(e => e.Prenom)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        /// <summary>
        /// Les profils retirés d'un parent, pour la page qui permet de les
        /// restaurer. Les anonymisés en sont exclus : il n'y a plus rien à
        /// montrer ni à restaurer.
        /// </summary>
        public async Task<IEnumerable<DomainEleve>> GetArchivesByParentAsync(int parentId)
        {
            var entities = await _context.Eleves
                .AsNoTracking()
                .Include(e => e.NiveauScolaire)
                .Where(e => e.ParentId == parentId
                            && e.ArchiveLe != null
                            && e.AnonymiseLe == null)
                .OrderByDescending(e => e.ArchiveLe)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        public async Task<DomainEleve?> GetEleveByIdAsync(int id)
        {
            var entity = await _context.Eleves
                .AsNoTracking()
                .Include(e => e.NiveauScolaire)
                .FirstOrDefaultAsync(e => e.Id == id);

            return entity is null ? null : Map(entity);
        }

        public async Task<DomainEleve> AddEleveAsync(DomainEleve model)
        {
            var entity = new Eleve
            {
                ParentId = model.ParentId,
                NiveauScolaireId = model.NiveauScolaireId,
                Prenom = model.Prenom,
                Nom = model.Nom,
                Age = model.Age,
                Sexe = model.Sexe,
                DateCreation = model.DateCreation == default ? DateTime.UtcNow : model.DateCreation
            };

            _context.Eleves.Add(entity);
            await _context.SaveChangesAsync();

            // Sa première année s'ouvre ici, et elle part de sa création : c'est
            // elle qui rattachera tout son travail à une classe.
            HistoriqueClasse.Ouvrir(_context, entity.Id, entity.NiveauScolaireId, entity.DateCreation);
            await _context.SaveChangesAsync();

            // Recharge pour récupérer le niveau dénormalisé.
            return await GetEleveByIdAsync(entity.Id) ?? Map(entity);
        }

        public async Task<DomainEleve?> UpdateEleveAsync(DomainEleve model)
        {
            var entity = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == model.Id);
            if (entity is null) return null;

            if (model.Prenom is not null) entity.Prenom = model.Prenom;
            if (model.Nom is not null) entity.Nom = model.Nom;
            if (model.Age > 0) entity.Age = model.Age;
            if (model.Sexe != Domain.Models.Sexe.NonPrecise) entity.Sexe = model.Sexe;
            // AVANT d'écraser la classe : le changement doit être vu pour
            // fermer l'intervalle en cours. Une fois la colonne écrasée,
            // l'ancienne classe est perdue.
            if (model.NiveauScolaireId > 0)
            {
                await HistoriqueClasse.ChangerAsync(_context, entity.Id, model.NiveauScolaireId);
                entity.NiveauScolaireId = model.NiveauScolaireId;
            }

            await _context.SaveChangesAsync();
            return await GetEleveByIdAsync(entity.Id);
        }

        /// <summary>
        /// Retire le profil : il quitte les listes et libère sa place.
        ///
        /// Rien n'est effacé. C'est ce que veut le parent qui bascule d'un
        /// enfant à l'autre, et il peut revenir dessus.
        /// </summary>
        public async Task<bool> ArchiverEleveAsync(int id)
        {
            var entity = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null || entity.AnonymiseLe is not null) return false;

            // Idempotent : archiver deux fois ne déplace pas la date, sinon un
            // double clic ferait croire à un retrait tout frais.
            entity.ArchiveLe ??= DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestaurerEleveAsync(int id)
        {
            var entity = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == id);

            // Un profil anonymisé ne se restaure pas : il n'y a plus d'enfant
            // derrière, seulement une ligne comptable.
            if (entity is null || entity.AnonymiseLe is not null) return false;

            entity.ArchiveLe = null;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Le droit à l'effacement, pour de bon.
        ///
        /// L'identité est vidée et TOUT l'historique pédagogique supprimé :
        /// conversations et messages, évaluations, comptes rendus, fiches de
        /// révision, maîtrise des compétences.
        ///
        /// La ligne `Eleve`, elle, subsiste — vidée de tout ce qui est
        /// personnel. Ce n'est pas une demi-mesure, c'est ce qui empêche un
        /// trou de facturation : le pot mensuel se calcule en SOMMANT les
        /// consommations de la période, et ces lignes pointent sur l'élève. Les
        /// emporter rendrait à la famille les heures déjà utilisées — il
        /// suffirait de supprimer l'enfant et de le recréer pour repartir avec
        /// un forfait plein, autant de fois qu'on veut.
        ///
        /// Une consommation n'est d'ailleurs pas une donnée de l'enfant : c'est
        /// la comptabilité d'un abonnement, et elle ne dit plus rien de lui une
        /// fois le nom retiré.
        /// </summary>
        public async Task<bool> AnonymiserEleveAsync(int id)
        {
            var entity = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null) return false;
            if (entity.AnonymiseLe is not null) return true;

            var conversations = await _context.Conversations
                .Where(c => c.EleveId == id)
                .Select(c => c.Id)
                .ToListAsync();

            // Les messages d'abord : ce sont eux qui portent les mots de
            // l'enfant, et ils sont de loin les plus nombreux.
            await _context.Messages
                .Where(m => conversations.Contains(m.ConversationId))
                .ExecuteDeleteAsync();

            await _context.FichesRevision.Where(f => f.EleveId == id).ExecuteDeleteAsync();
            await _context.RapportsSeance.Where(r => r.EleveId == id).ExecuteDeleteAsync();
            await _context.Evaluations.Where(e => e.EleveId == id).ExecuteDeleteAsync();
            await _context.MaitrisesEleves.Where(m => m.EleveId == id).ExecuteDeleteAsync();
            await _context.Conversations.Where(c => c.EleveId == id).ExecuteDeleteAsync();

            entity.Prenom = "Profil supprimé";
            entity.Nom = null;
            entity.Age = 0;
            entity.Sexe = Domain.Models.Sexe.NonPrecise;
            entity.DerniereActivite = null;
            entity.ArchiveLe ??= DateTime.UtcNow;
            entity.AnonymiseLe = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// LE TIRAGE EST REPRIS TANT QUE LA BASE REFUSE.
        ///
        /// L'index unique sur le code est la seule autorité : vérifier d'abord
        /// « ce code existe-t-il ? » puis écrire laisserait passer deux
        /// attributions simultanées, qui se croiseraient entre la lecture et
        /// l'écriture. On écrit, et on retire si ça casse.
        ///
        /// Cinq essais : la probabilité d'en manquer cinq d'affilée est de
        /// l'ordre de un sur cent millions de milliards. Au-delà, ce n'est plus
        /// une collision — c'est une panne qu'il vaut mieux voir.
        /// </summary>
        public async Task<string?> AttribuerCodeAsync(int eleveId, CancellationToken ct = default)
        {
            var entity = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == eleveId, ct);
            if (entity is null) return null;

            for (var essai = 0; essai < 5; essai++)
            {
                entity.CodeAcces = Domain.Services.CodeAccesEleve.Generer();

                try
                {
                    await _context.SaveChangesAsync(ct);
                    return entity.CodeAcces;
                }
                catch (DbUpdateException)
                {
                    // Le code est déjà pris. On en retire un autre — mais il
                    // faut d'abord oublier l'échec, sinon EF rejouerait le même
                    // ordre à la sauvegarde suivante.
                    _context.Entry(entity).State = EntityState.Detached;
                    entity = await _context.Eleves.FirstOrDefaultAsync(e => e.Id == eleveId, ct);
                    if (entity is null) return null;
                }
            }

            return null;
        }

        public async Task<DomainEleve?> GetParCodeAsync(string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var entity = await _context.Eleves
                .AsNoTracking()
                .Include(e => e.NiveauScolaire)
                .FirstOrDefaultAsync(e => e.CodeAcces == code, ct);

            return entity is null ? null : Map(entity);
        }

        public async Task<bool> SuspendreAccesAsync(
            int eleveId, bool suspendu, CancellationToken ct = default)
        {
            var modifiees = await _context.Eleves
                .Where(e => e.Id == eleveId)
                .ExecuteUpdateAsync(
                    m => m.SetProperty(
                        e => e.AccesSuspenduLe,
                        suspendu ? DateTime.UtcNow : (DateTime?)null),
                    ct);

            return modifiees > 0;
        }

        private static DomainEleve Map(Eleve entity) => new()
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            NiveauScolaireId = entity.NiveauScolaireId,
            Prenom = entity.Prenom,
            Nom = entity.Nom,
            Age = entity.Age,
            Sexe = entity.Sexe,
            DateCreation = entity.DateCreation,
            DerniereActivite = entity.DerniereActivite,
            ArchiveLe = entity.ArchiveLe,
            AnonymiseLe = entity.AnonymiseLe,
            CodeAcces = entity.CodeAcces,
            AccesSuspenduLe = entity.AccesSuspenduLe,
            NiveauCode = entity.NiveauScolaire?.Code,
            NiveauLibelle = entity.NiveauScolaire?.Libelle,
            NiveauCycle = entity.NiveauScolaire?.Cycle
        };
    }
}
