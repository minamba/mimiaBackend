namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES JEUX DE TESTS — Camara, le 04/11/2026 : un onglet d'administration pour tenir des
    /// carnets de test par classe. Trois étages : le JEU (une section, une classe, un auteur),
    /// ses TESTS (un titre, une matière), et leurs LIGNES (une compétence à vérifier, un
    /// statut, des dates, un commentaire).
    /// </summary>
    public static class StatutsDeTest
    {
        public const string ATester = "a_tester";
        public const string EnCours = "en_cours";
        public const string Ok = "ok";
        public const string Ko = "ko";

        public const string JeuNonDemarre = "non_demarre";
        public const string JeuEnCours = "en_cours";
        public const string JeuOk = "ok";

        public const string SectionElementaire = "elementaire";
        public const string SectionCollege = "college";
        public const string SectionLycee = "lycee";

        /// <summary>
        /// « TESTS GÉNÉRAUX » — Min's, le 22/11/2026 : une section ET une classe pour tout ce qui n'est ni un cours ni
        /// un professeur (compte parent, ajout d'enfant, abonnements, administration…). Rangée dans les mêmes colonnes
        /// texte que les autres sections (aucune migration) : section « generaux », classe « Tests Généraux ».
        /// </summary>
        public const string SectionGeneraux = "generaux";
        public const string NiveauGeneraux = "GENERAUX";
        public const string ClasseGeneraux = "Tests Généraux";

        /// <summary>
        /// UN BLOC DES TESTS GÉNÉRAUX N'A PAS DE MATIÈRE MAIS UNE CATÉGORIE, écrite librement (UX, Admin, Technique…) :
        /// son libellé va dans la colonne de la matière, avec ce code à la place du code de matière. Les catégories
        /// déjà employées sont reproposées (<c>IJeuxDeTestsRepository.CategoriesAsync</c>).
        /// </summary>
        public const string CodeCategorie = "categorie";

        /// <summary>La longueur d'une catégorie : un mot ou deux, pas une phrase.</summary>
        public const int CategorieMax = 100;

        public static bool EstGeneraux(string? section) => section == SectionGeneraux;

        /// <summary>« Élémentaire · CP », et pour les tests généraux « Tests Généraux » une seule fois (mails, Telegram).</summary>
        public static string SectionEtClasse(string? section, string? classe) => section switch
        {
            SectionGeneraux => ClasseGeneraux,
            SectionElementaire => $"Élémentaire · {classe}",
            SectionCollege => $"Collège · {classe}",
            SectionLycee => $"Lycée · {classe}",
            _ => $"{section} · {classe}",
        };

        /// <summary>
        /// Une catégorie écrite à la main, mise au propre : espaces en trop retirés. Null si vide ou trop longue. Une
        /// catégorie DÉJÀ EMPLOYÉE à la casse près reprend son orthographe (« ux » → « UX ») : pas de doublon de groupe.
        /// </summary>
        public static string? Categorie(string? saisie, IEnumerable<string>? existantes = null)
        {
            var propre = string.Join(' ', (saisie ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (propre.Length == 0 || propre.Length > CategorieMax) return null;

            var deja = existantes?.FirstOrDefault(c => string.Equals(c, propre, StringComparison.CurrentCultureIgnoreCase));
            return deja ?? propre;
        }

        /// <summary>Au plus autant de lignes créées d'un coup : une faute de frappe ne doit pas en créer dix mille.</summary>
        public const int LignesMaxParAjout = 50;

        public static bool Connu(string? statut) =>
            statut is ATester or EnCours or Ok or Ko;

        /// <summary>
        /// LE STATUT D'UN JEU SE CALCULE, IL NE SE RANGE PAS : stocké, il finirait par contredire
        /// ses lignes. Rien de démarré : « Non démarré ». Toutes les lignes OK : « OK ». Tout le
        /// reste, un KO compris, reste « En cours ». Un jeu sans ligne n'a rien de démarré.
        /// </summary>
        public static string DuJeu(IReadOnlyCollection<string?> statuts)
        {
            if (statuts.All(s => s == ATester)) return JeuNonDemarre;

            return statuts.All(s => s == Ok) ? JeuOk : JeuEnCours;
        }

        /// <summary>La section d'une classe, d'après son cycle dans le référentiel ; null s'il est inconnu.</summary>
        public static string? SectionDuCycle(string? cycle) => cycle switch
        {
            "Primaire" => SectionElementaire,
            "College" => SectionCollege,
            "Lycee" => SectionLycee,
            _ => null,
        };
    }

    public class LigneDeTest
    {
        public int Id { get; set; }

        public int TestId { get; set; }

        /// <summary>Vide tant que personne ne l'a écrite : « 5 lignes » en crée 5 à remplir.</summary>
        public string Competence { get; set; } = string.Empty;

        public string Statut { get; set; } = StatutsDeTest.ATester;

        public DateTime DateCreation { get; set; }

        /// <summary>Choisie à la main, sans heure.</summary>
        public DateTime? DateEffectuee { get; set; }

        public string? Commentaire { get; set; }
    }

    public class TestDuJeu
    {
        public int Id { get; set; }

        public int JeuId { get; set; }

        public string Titre { get; set; } = string.Empty;

        public string? MatiereCode { get; set; }

        public string? Matiere { get; set; }

        public List<LigneDeTest> Lignes { get; set; } = [];
    }

    public class JeuDeTest
    {
        public int Id { get; set; }

        /// <summary>elementaire / college / lycee / generaux.</summary>
        public string Section { get; set; } = string.Empty;

        /// <summary>Le nom donné au carnet ; vide pour ceux d'avant le 05/11/2026.</summary>
        public string Titre { get; set; } = string.Empty;

        public string? NiveauCode { get; set; }

        public string? Classe { get; set; }

        public string? Auteur { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>non_demarre / en_cours / ok : voir <see cref="StatutsDeTest.DuJeu"/>.</summary>
        public string Statut { get; set; } = StatutsDeTest.JeuNonDemarre;

        /// <summary>Les titres de ses tests, le plus récent d'abord : la liste générale les montre.</summary>
        public List<string> Titres { get; set; } = [];

        public int NombreLignes { get; set; }

        public int NombreOk { get; set; }

        /// <summary>Remplis pour la fiche d'un jeu, vides dans la liste générale.</summary>
        public List<TestDuJeu> Tests { get; set; } = [];
    }
}
