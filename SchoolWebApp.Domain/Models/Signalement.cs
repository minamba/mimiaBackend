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
    }
}
