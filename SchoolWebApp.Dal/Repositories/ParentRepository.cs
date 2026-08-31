using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainParent = SchoolWebApp.Domain.Models.Parent;

namespace SchoolWebApp.Dal.Repositories
{
    public class ParentRepository : IParentRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public ParentRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<DomainParent>> GetParentsAsync()
        {
            var entities = await _context.Parents.AsNoTracking().ToListAsync();
            return entities.Select(Map).ToList();
        }

        public async Task<IReadOnlyList<string>> GetAdressesParentsAsync(
            CancellationToken ct = default) =>
            await _context.Parents
                .AsNoTracking()
                .Where(p => p.Mail != null && p.Mail != "")
                .Select(p => p.Mail!)
                .ToListAsync(ct);

        public async Task<DomainParent?> GetParentByIdAsync(int id)
        {
            var entity = await _context.Parents.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return entity is null ? null : Map(entity);
        }

        public async Task<DomainParent?> GetParentByIdentityUserIdAsync(string identityUserId)
        {
            var entity = await _context.Parents
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId);

            return entity is null ? null : Map(entity);
        }

        public async Task<DomainParent> AddParentAsync(DomainParent model)
        {
            var entity = new Parent
            {
                IdentityUserId = model.IdentityUserId,
                Prenom = model.Prenom,
                Nom = model.Nom,
                Mail = model.Mail,
                DateCreation = model.DateCreation == default ? DateTime.UtcNow : model.DateCreation
            };

            _context.Parents.Add(entity);
            await _context.SaveChangesAsync();

            model.Id = entity.Id;
            model.DateCreation = entity.DateCreation;
            return model;
        }

        public async Task<DomainParent?> UpdateParentAsync(DomainParent model)
        {
            var entity = await _context.Parents.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (entity is null) return null;

            if (model.Prenom is not null) entity.Prenom = model.Prenom;
            if (model.Nom is not null) entity.Nom = model.Nom;
            if (model.Mail is not null) entity.Mail = model.Mail;

            await _context.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<bool> DeleteParentAsync(int id)
        {
            var entity = await _context.Parents.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null) return false;

            _context.Parents.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Efface le compte et TOUT ce qui s'y rattache. Sans retour possible.
        ///
        /// POURQUOI À LA MAIN, ALORS QUE LES CASCADES EXISTENT
        /// -------------------------------------------------
        /// `DELETE FROM Parent` échouerait. Trois clés étrangères sont en NO
        /// ACTION — ConsommationEleve→Eleve, Evaluation→Conversation,
        /// RapportSeance→Conversation — parce que SQL Server refuse les
        /// chemins de cascade multiples : ces tables sont atteignables par
        /// deux routes à la fois, et il ne veut pas arbitrer. Le serveur
        /// rendrait donc une violation de contrainte, et le parent un écran
        /// d'erreur au moment le plus sensible de son parcours.
        ///
        /// L'ordre ci-dessous n'est pas une précaution : c'est le seul qui
        /// passe. Chaque ligne part avant celle dont elle dépend.
        ///
        /// POURQUOI EFFACER PLUTÔT QU'ANONYMISER
        /// ------------------------------------
        /// Un profil d'enfant retiré est anonymisé : la consommation reste,
        /// sinon on rendrait à la famille les heures déjà utilisées. Ici la
        /// famille entière s'en va — il n'y a plus de quota à protéger, plus
        /// de fratrie à qui les heures pourraient profiter. La demande est
        /// « qu'il ne reste plus rien », et c'est ce qui est fait.
        /// </summary>
        public async Task<bool> SupprimerCompteAsync(
            int parentId, CancellationToken ct = default)
        {
            var existe = await _context.Parents.AnyAsync(p => p.Id == parentId, ct);
            if (!existe) return false;

            // TOUT OU RIEN.
            //
            // Sans transaction, une coupure au milieu laisserait un compte
            // amputé de ses cours mais toujours connectable — le pire des
            // deux états. Une transaction rend l'échec propre : rien n'a
            // bougé, le parent redemande.
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            var eleves = await _context.Eleves
                .Where(e => e.ParentId == parentId)
                .Select(e => e.Id)
                .ToListAsync(ct);

            var conversations = await _context.Conversations
                .Where(c => eleves.Contains(c.EleveId))
                .Select(c => c.Id)
                .ToListAsync(ct);

            var abonnements = await _context.Abonnements
                .Where(a => a.ParentId == parentId)
                .Select(a => a.Id)
                .ToListAsync(ct);

            // Les pièces jointes avant les messages : elles pointent sur les
            // deux, et le lien vers le message est en NO ACTION.
            await _context.PiecesJointes
                .Where(p => conversations.Contains(p.ConversationId)).ExecuteDeleteAsync(ct);

            await _context.Messages
                .Where(m => conversations.Contains(m.ConversationId)).ExecuteDeleteAsync(ct);

            // Notes et comptes rendus avant les conversations dont ils
            // dépendent — le lien est volontairement en NO ACTION, pour qu'une
            // conversation purgée au bout d'un an n'emporte pas la note.
            await _context.Evaluations
                .Where(e => eleves.Contains(e.EleveId)).ExecuteDeleteAsync(ct);

            await _context.RapportsSeance
                .Where(r => eleves.Contains(r.EleveId)).ExecuteDeleteAsync(ct);

            await _context.FichesRevision
                .Where(f => eleves.Contains(f.EleveId)).ExecuteDeleteAsync(ct);

            await _context.MaitrisesEleves
                .Where(m => eleves.Contains(m.EleveId)).ExecuteDeleteAsync(ct);

            await _context.SessionsEleves
                .Where(s => eleves.Contains(s.EleveId)).ExecuteDeleteAsync(ct);

            await _context.Conversations
                .Where(c => eleves.Contains(c.EleveId)).ExecuteDeleteAsync(ct);

            // La comptabilité de l'abonnement : elle tient à l'élève ET à
            // l'abonnement, donc elle part avant les deux.
            await _context.ConsommationsEleves
                .Where(c => eleves.Contains(c.EleveId) || abonnements.Contains(c.AbonnementId))
                .ExecuteDeleteAsync(ct);

            await _context.Recharges
                .Where(r => abonnements.Contains(r.AbonnementId)).ExecuteDeleteAsync(ct);

            await _context.Eleves.Where(e => e.ParentId == parentId).ExecuteDeleteAsync(ct);
            await _context.Abonnements.Where(a => a.ParentId == parentId).ExecuteDeleteAsync(ct);
            await _context.Parents.Where(p => p.Id == parentId).ExecuteDeleteAsync(ct);

            await transaction.CommitAsync(ct);
            return true;
        }

        /// <summary>
        /// Retient le client Stripe créé pour ce parent.
        ///
        /// Écrit UNE FOIS, à la première visite de la caisse : le rappeler
        /// ensuite créerait un second client, et le parent perdrait la carte
        /// enregistrée et l'historique de factures rattachés au premier.
        /// </summary>
        public async Task EnregistrerClientStripeAsync(
            int parentId, string clientId, CancellationToken ct = default)
        {
            await _context.Parents
                .Where(p => p.Id == parentId)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.StripeClientId, clientId), ct);
        }

        /// <summary>
        /// Le parent derrière un client Stripe. C'est le chemin de retour des
        /// webhooks : un événement Stripe ne connaît pas nos identifiants.
        /// </summary>
        public async Task<DomainParent?> GetParentParClientStripeAsync(
            string clientId, CancellationToken ct = default)
        {
            var entity = await _context.Parents
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.StripeClientId == clientId, ct);

            return entity is null ? null : Map(entity);
        }

        private static DomainParent Map(Parent entity) => new()
        {
            Id = entity.Id,
            IdentityUserId = entity.IdentityUserId,
            Prenom = entity.Prenom,
            Nom = entity.Nom,
            Mail = entity.Mail,
            StripeClientId = entity.StripeClientId,
            DateCreation = entity.DateCreation,
        };
    }
}
