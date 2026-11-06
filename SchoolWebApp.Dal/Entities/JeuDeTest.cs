namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN CARNET DE TEST — Camara, le 04/11/2026 : une section, une classe, un auteur. Son
    /// statut ne se range pas ici : il se calcule sur ses lignes (<c>StatutsDeTest.DuJeu</c>).
    ///
    /// LA CLASSE, LA MATIÈRE ET L'AUTEUR SONT GARDÉS EN TEXTE, sans clé étrangère : un carnet
    /// garde ce qui a été testé même si une matière est retirée du référentiel ou si le compte
    /// de son auteur disparaît.
    /// </summary>
    public partial class JeuDeTest
    {
        public int Id { get; set; }

        /// <summary>elementaire / college / lycee.</summary>
        public string Section { get; set; } = null!;

        /// <summary>Le nom du carnet, donné à la création (Camara, le 05/11/2026). Vide pour les carnets d'avant.</summary>
        public string Titre { get; set; } = string.Empty;

        public string NiveauCode { get; set; } = null!;

        public string Classe { get; set; } = null!;

        public string Auteur { get; set; } = null!;

        public DateTime DateCreation { get; set; }

        public virtual ICollection<TestDuJeu> Tests { get; set; } = new List<TestDuJeu>();
    }

    /// <summary>Un test d'un carnet : un titre, une matière, et ses lignes.</summary>
    public partial class TestDuJeu
    {
        public int Id { get; set; }

        public int JeuId { get; set; }

        public string Titre { get; set; } = null!;

        public string MatiereCode { get; set; } = null!;

        public string Matiere { get; set; } = null!;

        public virtual JeuDeTest? Jeu { get; set; }

        public virtual ICollection<LigneDeTest> Lignes { get; set; } = new List<LigneDeTest>();
    }

    /// <summary>Une compétence à vérifier : ce qu'on change depuis la liste, ligne par ligne.</summary>
    public partial class LigneDeTest
    {
        public int Id { get; set; }

        public int TestId { get; set; }

        /// <summary>Vide tant que personne ne l'a écrite.</summary>
        public string Competence { get; set; } = string.Empty;

        /// <summary>a_tester / en_cours / ok / ko.</summary>
        public string Statut { get; set; } = "a_tester";

        public DateTime DateCreation { get; set; }

        /// <summary>Choisie à la main, sans heure.</summary>
        public DateTime? DateEffectuee { get; set; }

        public string? Commentaire { get; set; }

        public virtual TestDuJeu? Test { get; set; }
    }
}
