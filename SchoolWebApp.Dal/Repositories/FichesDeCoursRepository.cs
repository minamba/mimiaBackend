using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Les cours importés et leurs fiches — voir `IFichesDeCoursRepository`.
    ///
    /// LES OCTETS NE SORTENT QUE VERS LA TÂCHE DE FOND : toutes les lectures
    /// côté élève projettent les pages sans leur colonne `donnees`. Une liste
    /// de vingt photos ne doit pas charger vingt photos.
    /// </summary>
    public class FichesDeCoursRepository : IFichesDeCoursRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;
        private readonly SchoolWebApp.Domain.Services.IComptesProteges? _comptesProteges;

        public FichesDeCoursRepository(
            SchoolWebAppDatabaseContext context, SchoolWebApp.Domain.Services.IComptesProteges? comptesProteges = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _comptesProteges = comptesProteges;
        }

        /// <summary>
        /// LE PROPRIÉTAIRE, EN UNE CONDITION : les cours de cet élève, ou ceux de
        /// ce parent (espace parent, 16/10/2026). Toutes les lectures et écritures
        /// côté propriétaire passent par là.
        /// </summary>
        private static Expression<Func<CoursImporte, bool>> DuCours(Proprietaire p)
        {
            if (p.ParentId is int parentId) return c => c.ParentId == parentId;
            var eleveId = p.EleveId;
            return c => c.EleveId == eleveId;
        }

        private static Expression<Func<FicheDeCours, bool>> DeLaFiche(Proprietaire p)
        {
            if (p.ParentId is int parentId) return f => f.ParentId == parentId;
            var eleveId = p.EleveId;
            return f => f.EleveId == eleveId;
        }

        public async Task<DroitsEspaceParent> DroitsEspaceParentAsync(int parentId, CancellationToken ct = default)
        {
            var parent = await _context.Parents
                .AsNoTracking()
                .Where(p => p.Id == parentId)
                .Select(p => new { p.PeutCreerFiches, p.EstAdministrateur, p.Mail })
                .FirstOrDefaultAsync(ct);

            if (parent is null) return new DroitsEspaceParent(false, false);

            // Le super-administrateur a toujours le droit ; un administrateur doit
            // l'avoir coché, mais n'a alors ni plafond ni quota.
            var superAdmin = _comptesProteges?.Protege(parent.Mail) ?? false;
            return new DroitsEspaceParent(
                parent.PeutCreerFiches || superAdmin,
                parent.EstAdministrateur || superAdmin);
        }

        public async Task<IReadOnlyList<CategorieFiches>> ListerCategoriesAsync(int parentId, CancellationToken ct = default) =>
            await _context.CategoriesFichesParents
                .AsNoTracking()
                .Where(k => k.ParentId == parentId)
                .OrderBy(k => k.Ordre).ThenBy(k => k.Id)
                .Select(k => new CategorieFiches(
                    k.Id, k.Libelle, k.Couleur, k.Ordre,
                    _context.CoursImportes.Count(c => c.CategorieId == k.Id && c.ParentId == parentId
                        && c.Etat != Regles.EtatBrouillon && c.ComplementDe == null)))
                .ToListAsync(ct);

        public async Task<CategorieFiches?> CreerCategorieAsync(
            int parentId, string libelle, string couleur, DateTime maintenant, CancellationToken ct = default)
        {
            var nombre = await _context.CategoriesFichesParents.CountAsync(k => k.ParentId == parentId, ct);
            if (nombre >= Regles.CategoriesMax) return null;

            var categorie = new CategorieFichesParent
            {
                ParentId = parentId,
                Libelle = libelle,
                Couleur = couleur,
                Ordre = nombre + 1,
                DateCreation = maintenant,
            };

            _context.CategoriesFichesParents.Add(categorie);
            await _context.SaveChangesAsync(ct);
            return new CategorieFiches(categorie.Id, categorie.Libelle, categorie.Couleur, categorie.Ordre, 0);
        }

        public async Task<bool> ModifierCategorieAsync(
            int parentId, int categorieId, string libelle, string couleur, CancellationToken ct = default) =>
            await _context.CategoriesFichesParents
                .Where(k => k.Id == categorieId && k.ParentId == parentId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(k => k.Libelle, libelle)
                    .SetProperty(k => k.Couleur, couleur), ct) > 0;

        public async Task<bool> SupprimerCategorieAsync(int parentId, int categorieId, CancellationToken ct = default)
        {
            if (!await CategorieDuParentAsync(parentId, categorieId, ct)) return false;

            // SES COURS RESTENT, « sans catégorie » : supprimer un classement ne
            // supprime pas des fiches. Le lien est en NO ACTION, on le défait d'abord.
            await _context.CoursImportes
                .Where(c => c.CategorieId == categorieId && c.ParentId == parentId)
                .ExecuteUpdateAsync(u => u.SetProperty(c => c.CategorieId, (int?)null), ct);

            return await _context.CategoriesFichesParents
                .Where(k => k.Id == categorieId && k.ParentId == parentId)
                .ExecuteDeleteAsync(ct) > 0;
        }

        public Task<bool> CategorieDuParentAsync(int parentId, int categorieId, CancellationToken ct = default) =>
            _context.CategoriesFichesParents.AnyAsync(k => k.Id == categorieId && k.ParentId == parentId, ct);

        public async Task<int> CompterEnRouteDuParentAsync(int parentId, CancellationToken ct = default) =>
            await _context.CoursImportes
                .Where(c => c.ParentId == parentId && EtatsEnRoute.Contains(c.Etat))
                .SumAsync(c => (int?)c.Poids, ct) ?? 0;

        public async Task<bool> FamilleSansLimiteAsync(int eleveId, CancellationToken ct = default)
        {
            var parent = await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId && e.Parent != null)
                .Select(e => new { e.Parent!.EstAdministrateur, e.Parent.Mail })
                .FirstOrDefaultAsync(ct);

            // Le super-administrateur se reconnaît à son adresse : c'est la
            // configuration, pas la base, qui lui donne son rôle.
            return parent is not null
                && (parent.EstAdministrateur || (_comptesProteges?.Protege(parent.Mail) ?? false));
        }

        public async Task<int> RangDeLEleveAsync(int eleveId, CancellationToken ct = default) =>
            await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => e.NiveauScolaire != null ? e.NiveauScolaire.Ordre : 0)
                .FirstOrDefaultAsync(ct);

        public async Task<int> CreerBrouillonAsync(
            Proprietaire proprietaire, int? matiereId, string type, DateTime maintenant, int? complementDe = null,
            CancellationToken ct = default, int? categorieId = null)
        {
            // Un seul complément commencé à la fois : un brouillon laissé en plan
            // (pages déposées, jamais envoyé) cède la place au nouveau.
            if (complementDe is not null)
            {
                await _context.CoursImportes
                    .Where(DuCours(proprietaire))
                    .Where(c => c.ComplementDe == complementDe && c.Etat == Regles.EtatBrouillon)
                    .ExecuteDeleteAsync(ct);
            }

            var cours = new CoursImporte
            {
                EleveId = proprietaire.EleveId,
                ParentId = proprietaire.ParentId,
                MatiereId = proprietaire.EstParent ? null : matiereId,
                CategorieId = proprietaire.EstParent ? categorieId : null,
                Type = type,
                Etat = Regles.EtatBrouillon,
                ComplementDe = complementDe,
                DateCreation = maintenant,
                DateModification = maintenant,
            };

            _context.CoursImportes.Add(cours);
            await _context.SaveChangesAsync(ct);
            return cours.Id;
        }

        public async Task<bool> ModifierBrouillonAsync(
            Proprietaire proprietaire, int coursId, int? matiereId, string type, DateTime maintenant,
            CancellationToken ct = default, int? categorieId = null)
        {
            var requete = _context.CoursImportes
                .Where(DuCours(proprietaire))
                .Where(c => c.Id == coursId && c.Etat == Regles.EtatBrouillon);

            // Un parent classe par catégorie, un élève par matière ; un complément
            // garde celle du cours qu'il complète.
            return proprietaire.EstParent
                ? await requete.ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.CategorieId, c => c.ComplementDe == null ? categorieId : c.CategorieId)
                    .SetProperty(c => c.Type, type)
                    .SetProperty(c => c.DateModification, maintenant), ct) > 0
                : await requete.ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.MatiereId, c => c.ComplementDe == null ? matiereId : c.MatiereId)
                    .SetProperty(c => c.Type, type)
                    .SetProperty(c => c.DateModification, maintenant), ct) > 0;
        }

        public async Task<CoursDeLEleve?> GetAsync(Proprietaire proprietaire, int coursId, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes
                .AsNoTracking()
                .Where(DuCours(proprietaire))
                .Where(c => c.Id == coursId)
                .Select(c => new
                {
                    Cours = c,
                    MatiereLibelle = c.Matiere != null ? c.Matiere.Libelle : c.Categorie != null ? c.Categorie.Libelle : null,
                    MatiereCode = c.Matiere != null ? c.Matiere.Code : null,
                    CategorieCouleur = c.Categorie != null ? c.Categorie.Couleur : null,
                })
                .FirstOrDefaultAsync(ct);

            if (cours is null) return null;

            var pages = await _context.CoursImportesPages
                .AsNoTracking()
                .Where(p => p.CoursId == coursId)
                .OrderBy(p => p.Ordre)
                .Select(p => new PageDuCours(p.Id, p.Ordre, p.NomFichier, p.TypeMime, p.Taille, p.NombrePages))
                .ToListAsync(ct);

            // Le complément : le titre du cours qu'il complète. Le cours : son
            // complément en route, ou raté depuis moins d'un jour (l'enfant doit
            // savoir pourquoi ses fiches ne sont pas venues).
            var titreComplete = cours.Cours.ComplementDe is { } completeId
                ? await _context.CoursImportes.AsNoTracking().Where(c => c.Id == completeId).Select(c => c.Titre).FirstOrDefaultAsync(ct)
                : null;

            var depuis = DateTime.UtcNow.AddDays(-1);
            var complement = cours.Cours.ComplementDe is null
                ? await _context.CoursImportes
                    .AsNoTracking()
                    .Where(c => c.ComplementDe == coursId
                        && (EtatsEnRoute.Contains(c.Etat) || (c.Etat == Regles.EtatEchec && c.DateModification >= depuis)))
                    .OrderByDescending(c => c.Id)
                    .Select(c => new ComplementDuCours(c.Id, c.Etat, c.Remarque))
                    .FirstOrDefaultAsync(ct)
                : null;

            var fiches = await _context.FichesDeCours
                .AsNoTracking()
                .Where(f => f.CoursId == coursId)
                .OrderBy(f => f.DateCreation).ThenBy(f => f.NotionRang)
                .Select(f => new FicheDuCours(f.Id, f.CoursId, f.Type, f.Titre, f.NotionRang, f.NotionTotal, f.DateCreation, f.Contenu))
                .ToListAsync(ct);

            return Vue(cours.Cours, cours.MatiereLibelle, cours.MatiereCode, pages, fiches, titreComplete, complement, cours.CategorieCouleur);
        }

        private static readonly string[] EtatsEnRoute =
            [Regles.EtatALire, Regles.EtatNotions, Regles.EtatAGenerer];

        public async Task<IReadOnlyList<CoursDeLEleve>> ListerAsync(Proprietaire proprietaire, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes
                .AsNoTracking()
                .Where(DuCours(proprietaire))
                .Where(c => c.Etat != Regles.EtatBrouillon && c.ComplementDe == null)
                .OrderByDescending(c => c.DateCreation)
                .Select(c => new
                {
                    Cours = c,
                    MatiereLibelle = c.Matiere != null ? c.Matiere.Libelle : c.Categorie != null ? c.Categorie.Libelle : null,
                    MatiereCode = c.Matiere != null ? c.Matiere.Code : null,
                    CategorieCouleur = c.Categorie != null ? c.Categorie.Couleur : null,
                })
                .ToListAsync(ct);

            var ids = cours.Select(c => c.Cours.Id).ToList();

            var fiches = ids.Count == 0
                ? []
                : await _context.FichesDeCours
                    .AsNoTracking()
                    .Where(f => ids.Contains(f.CoursId))
                    .OrderBy(f => f.DateCreation).ThenBy(f => f.NotionRang)
                    .Select(f => new FicheDuCours(f.Id, f.CoursId, f.Type, f.Titre, f.NotionRang, f.NotionTotal, f.DateCreation, null))
                    .ToListAsync(ct);

            return cours
                .Select(c => Vue(c.Cours, c.MatiereLibelle, c.MatiereCode, [], fiches.Where(f => f.CoursId == c.Cours.Id).ToList(),
                    categorieCouleur: c.CategorieCouleur))
                .ToList();
        }

        public Task<FicheDuCours?> GetFicheAsync(Proprietaire proprietaire, int ficheId, CancellationToken ct = default) =>
            _context.FichesDeCours
                .AsNoTracking()
                .Where(DeLaFiche(proprietaire))
                .Where(f => f.Id == ficheId)
                .Select(f => new FicheDuCours(f.Id, f.CoursId, f.Type, f.Titre, f.NotionRang, f.NotionTotal, f.DateCreation, f.Contenu))
                .FirstOrDefaultAsync(ct);

        public async Task<PageDuCours?> AjouterPageAsync(
            Proprietaire proprietaire, int coursId, string? nomFichier, string typeMime, byte[] donnees, int nombrePages,
            DateTime maintenant, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes
                .Where(DuCours(proprietaire))
                .FirstOrDefaultAsync(c => c.Id == coursId && c.Etat == Regles.EtatBrouillon, ct);

            if (cours is null) return null;

            var ordre = await _context.CoursImportesPages
                .Where(p => p.CoursId == coursId)
                .Select(p => (int?)p.Ordre)
                .MaxAsync(ct) ?? 0;

            var page = new CoursImportePage
            {
                CoursId = coursId,
                Ordre = ordre + 1,
                NomFichier = nomFichier,
                TypeMime = typeMime,
                Taille = donnees.Length,
                NombrePages = Math.Max(1, nombrePages),
                Donnees = donnees,
                DateCreation = maintenant,
            };

            _context.CoursImportesPages.Add(page);
            cours.DateModification = maintenant;
            await _context.SaveChangesAsync(ct);

            return new PageDuCours(page.Id, page.Ordre, page.NomFichier, page.TypeMime, page.Taille, page.NombrePages);
        }

        public async Task<bool> SupprimerPageAsync(Proprietaire proprietaire, int coursId, int pageId, CancellationToken ct = default)
        {
            var aLui = await _context.CoursImportes
                .Where(DuCours(proprietaire))
                .AnyAsync(c => c.Id == coursId && c.Etat == Regles.EtatBrouillon, ct);
            if (!aLui) return false;

            var supprimees = await _context.CoursImportesPages
                .Where(p => p.Id == pageId && p.CoursId == coursId)
                .ExecuteDeleteAsync(ct);

            return supprimees > 0;
        }

        public async Task<bool> EnvoyerAsync(Proprietaire proprietaire, int coursId, int poids, DateTime maintenant, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes
                .Where(DuCours(proprietaire))
                .FirstOrDefaultAsync(c => c.Id == coursId && c.Etat == Regles.EtatBrouillon, ct);

            if (cours is null || !await _context.CoursImportesPages.AnyAsync(p => p.CoursId == coursId, ct)) return false;

            cours.Etat = Regles.EtatALire;
            cours.EnvoyeLe = maintenant;
            cours.Poids = poids;
            cours.Remarque = null;
            cours.DateModification = maintenant;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public Task<int> CompterEnvoisAsync(int eleveId, DateTime depuis, CancellationToken ct = default) =>
            _context.CoursImportes.CountAsync(c => c.EleveId == eleveId && c.EnvoyeLe != null && c.EnvoyeLe >= depuis, ct);

        public async Task<bool> DemanderAsync(
            Proprietaire proprietaire, int coursId, string type, IReadOnlyList<int> notions, DateTime maintenant, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes
                .Where(DuCours(proprietaire))
                .FirstOrDefaultAsync(c => c.Id == coursId, ct);

            // Un cours se rédige UNE fois, juste après sa lecture : la rédaction
            // relit les pages, et les pages partent dès que les fiches sont
            // faites (le cours n'est pas gardé). Pour un autre type, l'enfant
            // réimporte son cours.
            if (cours is null
                || cours.PagesEffaceesLe is not null
                || cours.Etat != Regles.EtatNotions
                || cours.Notions is null)
            {
                return false;
            }

            cours.Type = type;
            cours.NotionsDemandees = JsonSerializer.Serialize(notions);
            cours.DemandeLe = maintenant;
            cours.Tentatives = 0;
            cours.Remarque = null;
            cours.Etat = Regles.EtatAGenerer;
            cours.DateModification = maintenant;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SupprimerCoursAsync(Proprietaire proprietaire, int coursId, CancellationToken ct = default)
        {
            var supprime = await _context.CoursImportes.Where(DuCours(proprietaire)).Where(c => c.Id == coursId).ExecuteDeleteAsync(ct) > 0;

            // Ses compléments partent avec lui : sans clé étrangère, c'est ici.
            if (supprime)
            {
                await _context.CoursImportes.Where(DuCours(proprietaire)).Where(c => c.ComplementDe == coursId).ExecuteDeleteAsync(ct);
            }

            return supprime;
        }

        public async Task<bool> SupprimerFicheAsync(Proprietaire proprietaire, int ficheId, CancellationToken ct = default) =>
            await _context.FichesDeCours.Where(DeLaFiche(proprietaire)).Where(f => f.Id == ficheId).ExecuteDeleteAsync(ct) > 0;

        public Task<FichePourEntrainement?> GetPourEntrainementAsync(Proprietaire proprietaire, int ficheId, CancellationToken ct = default) =>
            _context.FichesDeCours
                .AsNoTracking()
                .Where(DeLaFiche(proprietaire))
                .Where(f => f.Id == ficheId)
                .Select(f => new FichePourEntrainement(
                    f.Id,
                    f.Titre,
                    f.Contenu,
                    f.Quiz,
                    f.Cours != null && f.Cours.Matiere != null ? f.Cours.Matiere.Libelle
                        : f.Cours != null && f.Cours.Categorie != null ? f.Cours.Categorie.Libelle
                        : f.ParentId != null ? "ses notes" : "sa matière",
                    // Un parent révise pour lui-même : un adulte, sans classe.
                    f.ParentId != null ? Regles.ClasseAdulte
                        : f.Cours != null && f.Cours.Eleve != null && f.Cours.Eleve.NiveauScolaire != null ? f.Cours.Eleve.NiveauScolaire.Libelle : "sa classe",
                    f.ParentId != null ? Regles.RangAdulte
                        : f.Cours != null && f.Cours.Eleve != null && f.Cours.Eleve.NiveauScolaire != null ? f.Cours.Eleve.NiveauScolaire.Ordre : 0))
                .FirstOrDefaultAsync(ct);

        public async Task EnregistrerEntrainementAsync(int ficheId, string quiz, CancellationToken ct = default) =>
            await _context.FichesDeCours
                .Where(f => f.Id == ficheId)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.Quiz, quiz), ct);

        // ----------------------------------------------------------------
        // Tâche de fond
        // ----------------------------------------------------------------

        public async Task<IReadOnlyList<int>> GetATraiterAsync(int taille, CancellationToken ct = default) =>
            await _context.CoursImportes
                .AsNoTracking()
                .Where(c => c.Etat == Regles.EtatALire || c.Etat == Regles.EtatAGenerer)
                .OrderBy(c => c.DateModification)
                .Select(c => c.Id)
                .Take(taille)
                .ToListAsync(ct);

        public async Task<CoursATraiter?> GetPourTraitementAsync(int coursId, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes
                .AsNoTracking()
                .Where(c => c.Id == coursId)
                .Select(c => new
                {
                    Cours = c,
                    MatiereLibelle = c.Matiere != null ? c.Matiere.Libelle : c.Categorie != null ? c.Categorie.Libelle : null,
                    ClasseLibelle = c.ParentId != null ? Regles.ClasseAdulte
                        : c.Eleve != null && c.Eleve.NiveauScolaire != null ? c.Eleve.NiveauScolaire.Libelle : null,
                    Rang = c.ParentId != null ? Regles.RangAdulte
                        : c.Eleve != null && c.Eleve.NiveauScolaire != null ? c.Eleve.NiveauScolaire.Ordre : 0,
                })
                .FirstOrDefaultAsync(ct);

            if (cours is null) return null;

            var pages = await _context.CoursImportesPages
                .AsNoTracking()
                .Where(p => p.CoursId == coursId && p.Donnees != null)
                .OrderBy(p => p.Ordre)
                .Select(p => new { p.TypeMime, p.NomFichier, p.Donnees })
                .ToListAsync(ct);

            var demandeLe = cours.Cours.DemandeLe;
            var type = cours.Cours.Type;
            var complete = cours.Cours.ComplementDe;
            var notions = LireNotions(cours.Cours.Notions);

            var ecrites = demandeLe is null
                ? []
                : await _context.FichesDeCours
                    .AsNoTracking()
                    .Where(f => f.CoursId == (complete ?? coursId) && f.Type == type && f.DateCreation >= demandeLe)
                    .Select(f => f.NotionRang ?? 0)
                    .ToListAsync(ct);

            // Un complément range ses fiches au rang du cours complété : on le
            // ramène au rang de SA notion (le rang noté dans ses notions).
            var deja = complete is null
                ? ecrites
                : ecrites
                    .Select(r => r == 0 ? 0 : notions.Select((n, i) => (n, i)).Where(x => x.n.Rang == r).Select(x => x.i + 1).FirstOrDefault())
                    .Where(r => r >= 0)
                    .ToList();

            var existantes = complete is null
                ? []
                : LireNotions(await _context.CoursImportes.AsNoTracking()
                    .Where(c => c.Id == complete).Select(c => c.Notions).FirstOrDefaultAsync(ct))
                    .Select(n => n.Titre).ToList();

            var sansLimite = cours.Cours.ParentId is int parentId
                ? (await DroitsEspaceParentAsync(parentId, ct)).SansLimite
                : cours.Cours.EleveId is int eleveId && await FamilleSansLimiteAsync(eleveId, ct);

            return new CoursATraiter
            {
                SansLimite = sansLimite,
                Id = cours.Cours.Id,
                EleveId = cours.Cours.EleveId,
                ParentId = cours.Cours.ParentId,
                MatiereId = cours.Cours.MatiereId,
                MatiereLibelle = cours.MatiereLibelle ?? (cours.Cours.ParentId != null ? "ses notes" : "sa matière"),
                ClasseLibelle = cours.ClasseLibelle ?? "sa classe",
                Rang = cours.Rang,
                Etat = cours.Cours.Etat,
                Titre = cours.Cours.Titre,
                Type = type,
                Notions = notions,
                NotionsDemandees = LireRangs(cours.Cours.NotionsDemandees),
                DemandeLe = demandeLe,
                Tentatives = cours.Cours.Tentatives,
                Poids = cours.Cours.Poids,
                ComplementDe = complete,
                NotionsExistantes = existantes,
                Pages = pages.Select(p => (p.TypeMime, p.NomFichier, p.Donnees!)).ToList(),
                DejaRediges = deja,
            };
        }

        public async Task EnregistrerLectureAsync(
            int coursId, string etat, string? titre, IReadOnlyList<NotionDuCours> notions, string? remarque,
            DateTime maintenant, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes.FirstOrDefaultAsync(c => c.Id == coursId, ct);
            if (cours is null) return;

            cours.Etat = etat;
            cours.Titre = Tronquer(titre, 200);
            cours.Notions = notions.Count == 0 ? null : JsonSerializer.Serialize(notions);
            cours.Remarque = Tronquer(remarque, 500);
            cours.DateModification = maintenant;
            await _context.SaveChangesAsync(ct);

            if (EstFini(etat)) await EffacerPagesAsync([coursId], maintenant, ct);
        }

        public async Task<int> AjouterFicheAsync(
            int coursId, string type, string titre, int? notionRang, int? notionTotal, string contenu,
            DateTime maintenant, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes
                .AsNoTracking()
                .Where(c => c.Id == coursId)
                .Select(c => new { c.EleveId, c.ParentId, c.MatiereId, c.ComplementDe })
                .FirstOrDefaultAsync(ct);

            if (cours is null) return 0;

            if (cours.ComplementDe is { } completeId)
            {
                return await AjouterAuCoursCompleteAsync(coursId, completeId, type, titre, notionRang, contenu, maintenant, ct);
            }

            var fiche = new FicheDeCours
            {
                CoursId = coursId,
                EleveId = cours.EleveId,
                ParentId = cours.ParentId,
                MatiereId = cours.MatiereId,
                Type = type,
                Titre = Tronquer(titre, 200) ?? "Fiche de révision",
                NotionRang = notionRang,
                NotionTotal = notionTotal,
                Contenu = contenu,
                DateCreation = maintenant,
            };

            _context.FichesDeCours.Add(fiche);
            await _context.SaveChangesAsync(ct);
            return fiche.Id;
        }

        /// <summary>
        /// UNE FICHE DE COMPLÉMENT, RANGÉE DANS LE COURS COMPLÉTÉ — Camara, le
        /// 16/10/2026 : « s'il y a des doublons, tu les remplaces par les
        /// nouvelles ; sinon elles s'ajoutent à la suite ».
        ///
        /// - Une notion que la lecture a reconnue (`Rang` noté) garde le rang de
        ///   l'ancienne, et la fiche de même type faite avant ce complément part.
        /// - Une notion nouvelle s'ajoute à la fin des notions du cours ; son rang
        ///   est noté dans le complément (une reprise ne l'ajoute pas deux fois).
        /// - Une fiche du cours entier (express, spéciale contrôle) s'ajoute.
        /// Toutes les fiches du cours passent ensuite au nouveau total de notions.
        /// </summary>
        private async Task<int> AjouterAuCoursCompleteAsync(
            int complementId, int completeId, string type, string titre, int? rangLocal, string contenu,
            DateTime maintenant, CancellationToken ct)
        {
            var complement = await _context.CoursImportes.FirstOrDefaultAsync(c => c.Id == complementId, ct);
            var complete = await _context.CoursImportes.FirstOrDefaultAsync(c => c.Id == completeId, ct);
            if (complement is null || complete is null) return 0;

            var notionsCompletees = LireNotions(complete.Notions).ToList();
            int? rang = null;

            if (rangLocal is { } local)
            {
                var siennes = LireNotions(complement.Notions).ToList();
                if (local < 1 || local > siennes.Count) return 0;
                var notion = siennes[local - 1];

                if (notion.Rang is { } connu && connu >= 1 && connu <= notionsCompletees.Count)
                {
                    rang = connu;
                    var avant = complement.DemandeLe ?? maintenant;
                    await _context.FichesDeCours
                        .Where(f => f.CoursId == completeId && f.Type == type && f.NotionRang == connu && f.DateCreation < avant)
                        .ExecuteDeleteAsync(ct);
                }
                else
                {
                    notionsCompletees.Add(new NotionDuCours(notion.Titre, notion.Resume));
                    rang = notionsCompletees.Count;
                    siennes[local - 1] = notion with { Rang = rang };
                    complement.Notions = JsonSerializer.Serialize(siennes);
                    complete.Notions = JsonSerializer.Serialize(notionsCompletees);
                }
            }

            var fiche = new FicheDeCours
            {
                CoursId = completeId,
                EleveId = complete.EleveId,
                ParentId = complete.ParentId,
                MatiereId = complete.MatiereId,
                Type = type,
                Titre = Tronquer(titre, 200) ?? "Fiche de révision",
                NotionRang = rang,
                NotionTotal = rang is null ? null : notionsCompletees.Count,
                Contenu = contenu,
                DateCreation = maintenant,
            };

            _context.FichesDeCours.Add(fiche);
            complete.DateModification = maintenant;
            await _context.SaveChangesAsync(ct);

            var total = notionsCompletees.Count;
            await _context.FichesDeCours
                .Where(f => f.CoursId == completeId && f.NotionRang != null && f.NotionTotal != total)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.NotionTotal, total), ct);

            return fiche.Id;
        }

        public async Task MarquerAsync(int coursId, string etat, string? remarque, int tentatives, DateTime maintenant, CancellationToken ct = default)
        {
            var cours = await _context.CoursImportes.FirstOrDefaultAsync(c => c.Id == coursId, ct);
            if (cours is null) return;

            cours.Etat = etat;
            cours.Remarque = Tronquer(remarque, 500);
            cours.Tentatives = tentatives;
            cours.DateModification = maintenant;
            await _context.SaveChangesAsync(ct);

            if (EstFini(etat)) await EffacerPagesAsync([coursId], maintenant, ct);
        }

        /// <summary>
        /// LE COURS N'EST PAS GARDÉ (Camara, 15/10/2026) : un cours fini, prêt
        /// ou en échec, n'a plus besoin de ses pages. Elles partent là, au
        /// moment même — c'est le dépôt qui le tient, pas l'appelant, pour
        /// qu'aucun chemin vers « fini » ne les oublie.
        /// </summary>
        private static bool EstFini(string etat) => etat is Regles.EtatPret or Regles.EtatEchec;

        private async Task EffacerPagesAsync(IReadOnlyCollection<int> coursIds, DateTime maintenant, CancellationToken ct)
        {
            if (coursIds.Count == 0) return;

            await _context.CoursImportesPages
                .Where(p => coursIds.Contains(p.CoursId))
                .ExecuteDeleteAsync(ct);

            await _context.CoursImportes
                .Where(c => coursIds.Contains(c.Id) && c.PagesEffaceesLe == null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.PagesEffaceesLe, maintenant), ct);
        }

        public async Task<int> PurgerAsync(DateTime maintenant, CancellationToken ct = default)
        {
            // Les brouillons jamais envoyés : l'enfant a déposé des pages puis
            // est parti. Ils n'ont produit aucune fiche, rien à garder.
            var limiteBrouillon = maintenant.AddHours(-Regles.HeuresConservationBrouillon);
            var brouillons = await _context.CoursImportes
                .Where(c => c.Etat == Regles.EtatBrouillon && c.DateModification < limiteBrouillon)
                .ExecuteDeleteAsync(ct);

            // L'enfant n'a pas choisi ses notions à temps : le cours ne reste
            // pas en attente indéfiniment avec ses photos. Il passe en échec,
            // avec une phrase pour lui, et ses pages partent.
            var limiteChoix = maintenant.AddHours(-Regles.HeuresChoixDesNotions);
            var oublies = await _context.CoursImportes
                .Where(c => c.Etat == Regles.EtatNotions && c.DateModification < limiteChoix)
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .Take(200)
                .ToListAsync(ct);

            if (oublies.Count > 0)
            {
                await _context.CoursImportes
                    .Where(c => oublies.Contains(c.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.Etat, Regles.EtatEchec)
                        .SetProperty(c => c.Remarque, "Tu n’as pas choisi tes notions à temps : ton cours a été effacé. Importe-le à nouveau pour créer tes fiches.")
                        .SetProperty(c => c.DateModification, maintenant), ct);
            }

            // Filet : un cours fini qui aurait gardé des pages (arrêt du
            // serveur entre les deux écritures, cours d'avant la règle).
            var restes = await _context.CoursImportes
                .Where(c => (c.Etat == Regles.EtatPret || c.Etat == Regles.EtatEchec)
                    && _context.CoursImportesPages.Any(p => p.CoursId == c.Id))
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .Take(200)
                .ToListAsync(ct);

            var anciens = oublies.Union(restes).ToList();
            await EffacerPagesAsync(anciens, maintenant, ct);

            return brouillons + anciens.Count;
        }

        // ----------------------------------------------------------------

        private static CoursDeLEleve Vue(
            CoursImporte c, string? matiereLibelle, string? matiereCode,
            IReadOnlyList<PageDuCours> pages, IReadOnlyList<FicheDuCours> fiches,
            string? titreComplete = null, ComplementDuCours? complement = null, string? categorieCouleur = null) => new()
            {
                Id = c.Id,
                EleveId = c.EleveId,
                ParentId = c.ParentId,
                MatiereId = c.MatiereId,
                CategorieId = c.CategorieId,
                CategorieCouleur = categorieCouleur,
                MatiereLibelle = matiereLibelle,
                MatiereCode = matiereCode,
                Titre = c.Titre,
                Etat = c.Etat,
                Remarque = c.Remarque,
                Type = c.Type,
                Notions = LireNotions(c.Notions),
                NotionsDemandees = LireRangs(c.NotionsDemandees),
                DemandeLe = c.DemandeLe,
                PagesDisponibles = c.PagesEffaceesLe is null,
                DateCreation = c.DateCreation,
                ComplementDe = c.ComplementDe,
                ComplementDeTitre = titreComplete,
                Complement = complement,
                Pages = pages,
                Fiches = fiches,
            };

        private static IReadOnlyList<NotionDuCours> LireNotions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            try
            {
                return JsonSerializer.Deserialize<List<NotionDuCours>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static IReadOnlyList<int> LireRangs(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            try
            {
                return JsonSerializer.Deserialize<List<int>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string? Tronquer(string? texte, int max)
        {
            if (string.IsNullOrWhiteSpace(texte)) return null;
            var propre = texte.Trim();
            return propre.Length <= max ? propre : propre[..max];
        }
    }
}
