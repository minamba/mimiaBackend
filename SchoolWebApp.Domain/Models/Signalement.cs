namespace SchoolWebApp.Domain.Models
{
    /// <summary>Un problème ou une suggestion signalé, avec de quoi le retrouver côté admin.</summary>
    public class Signalement
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public int? EleveId { get; set; }

        public string? Categorie { get; set; }

        public string? Description { get; set; }

        public string Etat { get; set; } = "nouveau";

        public DateTime DateCreation { get; set; }

        public DateTime? DateMiseAJour { get; set; }

        /// <summary>Dénormalisé pour l'écran d'administration — la seule adresse où écrire.</summary>
        public string? ParentMail { get; set; }

        /// <summary>Dénormalisé — null si le signalement vient du parent lui-même.</summary>
        public string? ElevePrenom { get; set; }

        /// <summary>Affiché dans la liste, sans charger les commentaires eux-mêmes.</summary>
        public int NombreCommentaires { get; set; }

        /// <summary>Le nombre de captures et de documents joints.</summary>
        public int NombrePieces { get; set; }

        /// <summary>Fait par un administrateur, depuis le bouton à côté du bouclier (05/11/2026).</summary>
        public bool EstAdmin { get; set; }

        /// <summary>« Compétence / test à réaliser », s'il est relié à un jeu de test.</summary>
        public string? Competence { get; set; }

        public int? LigneDeTestId { get; set; }

        /// <summary>Où se trouve la ligne : rempli pour l'onglet « Signalements Admin » seulement.</summary>
        public LigneDeSignalement? Test { get; set; }
    }

    /// <summary>La ligne de jeu de test d'un signalement d'administrateur, avec son jeu et son bloc.</summary>
    public class LigneDeSignalement
    {
        public int LigneId { get; set; }

        public int JeuId { get; set; }

        public string JeuTitre { get; set; } = string.Empty;

        public string Section { get; set; } = string.Empty;

        public string Classe { get; set; } = string.Empty;

        public int TestId { get; set; }

        public string TestTitre { get; set; } = string.Empty;

        public string Matiere { get; set; } = string.Empty;

        public string Competence { get; set; } = string.Empty;

        public string? Commentaire { get; set; }

        public string Statut { get; set; } = StatutsDeTest.ATester;
    }
}
