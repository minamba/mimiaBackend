namespace SchoolWebApp.Domain.Models
{
    public class Eleve
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public int NiveauScolaireId { get; set; }

        public string? Prenom { get; set; }

        /// <summary>
        /// Administratif uniquement : distinguer deux Emma dans le tableau de
        /// bord. Le professeur n'y a pas accès — il appelle l'élève par son
        /// prénom, comme le ferait un vrai professeur particulier.
        /// </summary>
        public string? Nom { get; set; }

        public int Age { get; set; }

        /// <summary>Pilote uniquement les accords grammaticaux du professeur.</summary>
        public Sexe Sexe { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DerniereActivite { get; set; }

        /// <summary>Profil retiré par le parent : hors des listes, hors du quota.</summary>
        public DateTime? ArchiveLe { get; set; }

        /// <summary>Données personnelles effacées, définitivement.</summary>
        public DateTime? AnonymiseLe { get; set; }

        /// <summary>Un profil anonymisé n'a plus rien à restaurer.</summary>
        public bool Restaurable => ArchiveLe is not null && AnonymiseLe is null;

        /// <summary>Le code que l'enfant tape pour entrer dans ses cours.</summary>
        public string? CodeAcces { get; set; }

        /// <summary>Quand le parent a coupé son accès. Null s'il est ouvert.</summary>
        public DateTime? AccesSuspenduLe { get; set; }

        /// <summary>L'enfant peut-il ouvrir une session avec son code ?</summary>
        public bool AccesOuvert =>
            AccesSuspenduLe is null && ArchiveLe is null && AnonymiseLe is null;

        // Dénormalisé pour l'affichage : évite un aller-retour côté front
        // uniquement pour afficher « 6e » à côté du prénom.
        public string? NiveauCode { get; set; }

        public string? NiveauLibelle { get; set; }

        public string? NiveauCycle { get; set; }
    }
}
