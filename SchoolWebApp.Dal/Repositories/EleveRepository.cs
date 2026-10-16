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
                .Include(e => e.Academie)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        public async Task<IEnumerable<DomainEleve>> GetElevesByParentAsync(int parentId)
        {
            var entities = await _context.Eleves
                .AsNoTracking()
                .Include(e => e.NiveauScolaire)
                .Include(e => e.Academie)
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
                .Include(e => e.Academie)
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
                .Include(e => e.Academie)
                .FirstOrDefaultAsync(e => e.Id == id);

            return entity is null ? null : Map(entity);
        }

        public async Task<DomainEleve> AddEleveAsync(DomainEleve model)
        {
            var entity = new Eleve
            {
                ParentId = model.ParentId,
                NiveauScolaireId = model.NiveauScolaireId,
                AcademieId = model.AcademieId,
                Prenom = model.Prenom,
                Nom = model.Nom,
                Age = model.Age,
                Sexe = model.Sexe,
                Lv2Espagnol = model.Lv2Espagnol,
                Specialites = Domain.Models.VoiesScolaires.EcrireSpecialites(model.Specialites),
                DateCreation = model.DateCreation == default ? DateTime.UtcNow : model.DateCreation,
                EcranAdapte = model.EcranAdapte ?? true,
            };

            // LA COCHE VAUT ACCORD, ET SA DATE EN EST LA PREUVE (11/10/2026).
            DefinirBesoins(entity, model.BesoinsParticuliers);

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

            if (model.AcademieId.HasValue) entity.AcademieId = model.AcademieId;

            // Toujours écrit : le formulaire d'édition envoie le profil entier,
            // et une case décochée est une information, pas un oubli.
            entity.Lv2Espagnol = model.Lv2Espagnol;
            entity.Specialites = Domain.Models.VoiesScolaires.EcrireSpecialites(model.Specialites);

            // Les besoins particuliers : seulement s'ils ont été envoyés. Un
            // formulaire qui ne les porte pas (module éteint, écran ancien) ne
            // les efface pas.
            if (model.BesoinsParticuliers is not null) DefinirBesoins(entity, model.BesoinsParticuliers);
            if (model.EcranAdapte is { } ecran) entity.EcranAdapte = ecran;

            await _context.SaveChangesAsync();
            return await GetEleveByIdAsync(entity.Id);
        }

        /// <summary>
        /// Écrit les besoins cochés. La date de l'accord suit la coche : posée
        /// quand la liste change et n'est pas vide, effacée quand elle se vide.
        /// </summary>
        private static void DefinirBesoins(Eleve entity, IEnumerable<string>? codes)
        {
            var nouveaux = Domain.Models.BesoinsParticuliers.Ecrire(codes);
            if (nouveaux == entity.BesoinsParticuliers) return;

            entity.BesoinsParticuliers = nouveaux;
            entity.BesoinsAccordLe = nouveaux is null ? null : DateTime.UtcNow;
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
            await _context.RapportsOrientation.Where(r => r.EleveId == id).ExecuteDeleteAsync();

            // Les cours importés pour ses fiches de révision (14/10/2026) : leurs
            // pages et leurs fiches partent avec eux (cascade). Des photos de son
            // cahier ne survivent pas à son profil.
            await _context.CoursImportes.Where(c => c.EleveId == id).ExecuteDeleteAsync();

            entity.Prenom = "Profil supprimé";
            entity.Nom = null;
            entity.Age = 0;
            entity.Sexe = Domain.Models.Sexe.NonPrecise;
            entity.DerniereActivite = null;
            // Une donnée de santé ne survit pas à l'effacement du profil.
            entity.BesoinsParticuliers = null;
            entity.BesoinsAccordLe = null;
            // L'établissement non plus : avec la classe, il situe l'enfant.
            entity.EtablissementUai = null;
            entity.EtablissementLibelle = null;
            entity.EtablissementCommune = null;
            entity.EtablissementLatitude = null;
            entity.EtablissementLongitude = null;
            // Ni son projet d'orientation.
            entity.ProjetObjectif = null;
            entity.ProjetMatieres = null;
            entity.ProjetLe = null;
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
                .Include(e => e.Academie)
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

        /// <summary>
        /// L'ACTIVITÉ HORS COURS — Camara, le 11/10/2026 : « quand un enfant se
        /// connecte et va directement dans les jeux, la dernière activité ne se
        /// met pas à jour ». Elle n'était notée qu'à un message de cours.
        ///
        /// UNE ÉCRITURE PAR HEURE AU PLUS : la colonne se lit au jour près, et
        /// une partie relancée dix fois ne doit pas écrire dix fois. Le filtre
        /// est dans la requête même — une seule instruction, sans lecture.
        /// </summary>
        /// <summary>
        /// L'ÉTABLISSEMENT DE L'ÉLÈVE (11/10/2026), par une route à part : les
        /// formulaires de profil le réécriraient à vide s'ils le portaient sans
        /// le connaître. Une seule instruction, sans lecture.
        /// </summary>
        public async Task<bool> DefinirEtablissementAsync(
            int eleveId, string? uai, string? libelle, string? commune,
            double? latitude, double? longitude, CancellationToken ct = default)
        {
            var modifiees = await _context.Eleves
                .Where(e => e.Id == eleveId)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(e => e.EtablissementUai, uai)
                    .SetProperty(e => e.EtablissementLibelle, libelle)
                    .SetProperty(e => e.EtablissementCommune, commune)
                    .SetProperty(e => e.EtablissementLatitude, latitude)
                    .SetProperty(e => e.EtablissementLongitude, longitude), ct);

            return modifiees > 0;
        }

        public async Task NoterActiviteAsync(int eleveId, CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;
            var ilYaUneHeure = maintenant.AddHours(-1);

            await _context.Eleves
                .Where(e => e.Id == eleveId
                    && (e.DerniereActivite == null || e.DerniereActivite < ilYaUneHeure))
                .ExecuteUpdateAsync(m => m.SetProperty(e => e.DerniereActivite, maintenant), ct);
        }

        public async Task DefinirProjetOrientationAsync(
            int eleveId, Domain.Models.ProjetOrientation? projet, CancellationToken ct = default)
        {
            var objectif = projet?.Objectif;
            var matieres = projet is null ? null : Domain.Models.ProjetOrientation.Ecrire(projet.Matieres);
            var le = projet?.Le;

            await _context.Eleves
                .Where(e => e.Id == eleveId)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(e => e.ProjetObjectif, objectif)
                    .SetProperty(e => e.ProjetMatieres, matieres)
                    .SetProperty(e => e.ProjetLe, le), ct);
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
            Lv2Espagnol = entity.Lv2Espagnol,
            Specialites = Domain.Models.VoiesScolaires.LireSpecialites(entity.Specialites),
            NiveauCode = entity.NiveauScolaire?.Code,
            NiveauLibelle = entity.NiveauScolaire?.Libelle,
            NiveauCycle = entity.NiveauScolaire?.Cycle,
            AcademieId = entity.AcademieId,
            AcademieLibelle = entity.Academie?.Libelle,
            Zone = entity.Academie?.Zone,
            EtablissementUai = entity.EtablissementUai,
            EtablissementLibelle = entity.EtablissementLibelle,
            EtablissementCommune = entity.EtablissementCommune,
            EtablissementLatitude = entity.EtablissementLatitude,
            EtablissementLongitude = entity.EtablissementLongitude,
            BesoinsParticuliers = Domain.Models.BesoinsParticuliers.Lire(entity.BesoinsParticuliers),
            BesoinsAccordLe = entity.BesoinsAccordLe,
            EcranAdapte = entity.EcranAdapte,
            ProjetOrientation = Domain.Models.ProjetOrientation.Lire(entity.ProjetObjectif, entity.ProjetMatieres, entity.ProjetLe),
        };
    }
}
