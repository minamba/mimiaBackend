using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les cours importés et les fiches de révision qui en sont tirées
    /// (14/10/2026). TOUTE lecture et écriture passe par leur PROPRIÉTAIRE —
    /// un élève, ou un parent depuis l'espace parent (16/10/2026) : un cours
    /// d'un autre n'existe pas pour lui (null). Un identifiant d'élève se
    /// convertit tout seul en propriétaire (`Proprietaire`).
    /// </summary>
    public interface IFichesDeCoursRepository
    {
        /// <summary>Le rang de sa classe : 1 = CP … 12 = terminale ; 0 si inconnu.</summary>
        Task<int> RangDeLEleveAsync(int eleveId, CancellationToken ct = default);

        /// <summary>Un brouillon : une matière pour un élève, une catégorie (ou aucune) pour un parent.</summary>
        Task<int> CreerBrouillonAsync(
            Proprietaire proprietaire, int? matiereId, string type, DateTime maintenant, int? complementDe = null,
            CancellationToken ct = default, int? categorieId = null);

        /// <summary>Change la matière (ou la catégorie) et le type d'un BROUILLON ; faux s'il est déjà envoyé.</summary>
        Task<bool> ModifierBrouillonAsync(
            Proprietaire proprietaire, int coursId, int? matiereId, string type, DateTime maintenant,
            CancellationToken ct = default, int? categorieId = null);

        /// <summary>Un cours de ce propriétaire, ses pages (sans octets) et ses fiches (avec leur contenu).</summary>
        Task<CoursDeLEleve?> GetAsync(Proprietaire proprietaire, int coursId, CancellationToken ct = default);

        /// <summary>Tous les cours envoyés du propriétaire, du plus récent au plus ancien, fiches sans contenu.</summary>
        Task<IReadOnlyList<CoursDeLEleve>> ListerAsync(Proprietaire proprietaire, CancellationToken ct = default);

        /// <summary>Une fiche du propriétaire, avec son contenu.</summary>
        Task<FicheDuCours?> GetFicheAsync(Proprietaire proprietaire, int ficheId, CancellationToken ct = default);

        Task<PageDuCours?> AjouterPageAsync(
            Proprietaire proprietaire, int coursId, string? nomFichier, string typeMime, byte[] donnees, int nombrePages,
            DateTime maintenant, CancellationToken ct = default);

        Task<bool> SupprimerPageAsync(Proprietaire proprietaire, int coursId, int pageId, CancellationToken ct = default);

        /// <summary>Brouillon → à lire. Faux si le cours n'est pas un brouillon ou n'a aucune page.</summary>
        Task<bool> EnvoyerAsync(Proprietaire proprietaire, int coursId, int poids, DateTime maintenant, CancellationToken ct = default);

        /// <summary>Les cours envoyés depuis cette date : c'est la limite du mois.</summary>
        Task<int> CompterEnvoisAsync(int eleveId, DateTime depuis, CancellationToken ct = default);

        /// <summary>
        /// Une demande de fiches : le type et les notions (rangs à partir de 1 ;
        /// vide pour un type qui couvre tout le cours). Faux si le cours n'est
        /// pas lu, ou si ses pages sont déjà effacées.
        /// </summary>
        Task<bool> DemanderAsync(Proprietaire proprietaire, int coursId, string type, IReadOnlyList<int> notions, DateTime maintenant, CancellationToken ct = default);

        Task<bool> SupprimerCoursAsync(Proprietaire proprietaire, int coursId, CancellationToken ct = default);

        Task<bool> SupprimerFicheAsync(Proprietaire proprietaire, int ficheId, CancellationToken ct = default);

        /// <summary>La fiche du propriétaire, avec son quiz s'il est déjà écrit (16/10/2026).</summary>
        Task<FichePourEntrainement?> GetPourEntrainementAsync(Proprietaire proprietaire, int ficheId, CancellationToken ct = default);

        /// <summary>Garde le quiz écrit pour la fiche : il ne se paie qu'une fois.</summary>
        Task EnregistrerEntrainementAsync(int ficheId, string quiz, CancellationToken ct = default);

        // ---- L'espace parent (16/10/2026) ----

        /// <summary>Ce que l'espace parent ouvre à ce parent : le droit aux fiches, et l'absence de limite.</summary>
        Task<DroitsEspaceParent> DroitsEspaceParentAsync(int parentId, CancellationToken ct = default);

        /// <summary>Ses catégories, dans leur ordre, avec le nombre de cours de chacune.</summary>
        Task<IReadOnlyList<CategorieFiches>> ListerCategoriesAsync(int parentId, CancellationToken ct = default);

        /// <summary>Crée une catégorie ; null si le parent en a déjà le maximum.</summary>
        Task<CategorieFiches?> CreerCategorieAsync(int parentId, string libelle, string couleur, DateTime maintenant, CancellationToken ct = default);

        Task<bool> ModifierCategorieAsync(int parentId, int categorieId, string libelle, string couleur, CancellationToken ct = default);

        /// <summary>Supprime une catégorie ; ses cours restent, « sans catégorie ».</summary>
        Task<bool> SupprimerCategorieAsync(int parentId, int categorieId, CancellationToken ct = default);

        /// <summary>La catégorie existe-t-elle, et est-elle à ce parent ?</summary>
        Task<bool> CategorieDuParentAsync(int parentId, int categorieId, CancellationToken ct = default);

        /// <summary>Les cours de ce parent encore en route (poids compris) : ils réservent leurs crédits.</summary>
        Task<int> CompterEnRouteDuParentAsync(int parentId, CancellationToken ct = default);

        // ---- Tâche de fond ----

        /// <summary>Les cours à lire ou à rédiger, les plus anciens d'abord.</summary>
        Task<IReadOnlyList<int>> GetATraiterAsync(int taille, CancellationToken ct = default);

        Task<CoursATraiter?> GetPourTraitementAsync(int coursId, CancellationToken ct = default);

        /// <summary>
        /// La famille de cet élève est-elle administratrice (parent `EstAdministrateur`,
        /// ou super-administrateur) ? Alors ses imports n'ont pas les plafonds habituels.
        /// </summary>
        Task<bool> FamilleSansLimiteAsync(int eleveId, CancellationToken ct = default);

        Task EnregistrerLectureAsync(int coursId, string etat, string? titre, IReadOnlyList<NotionDuCours> notions, string? remarque, DateTime maintenant, CancellationToken ct = default);

        /// <summary>Range une fiche rédigée ; rend son identifiant (0 si le cours a disparu).</summary>
        Task<int> AjouterFicheAsync(int coursId, string type, string titre, int? notionRang, int? notionTotal, string contenu, DateTime maintenant, CancellationToken ct = default);

        Task MarquerAsync(int coursId, string etat, string? remarque, int tentatives, DateTime maintenant, CancellationToken ct = default);

        /// <summary>Efface les brouillons abandonnés et les pages trop anciennes. Rend le nombre d'éléments touchés.</summary>
        Task<int> PurgerAsync(DateTime maintenant, CancellationToken ct = default);
    }
}
