namespace SchoolWebApp.Domain.Models
{
    /// <summary>La session d'un enfant sur un appareil, telle qu'elle circule.</summary>
    public class SessionEleve
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>L'identifiant du parent, pour les contrôles d'appartenance.</summary>
        public int ParentId { get; set; }

        public string? PrenomEleve { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime DernierAcces { get; set; }

        public string? Appareil { get; set; }
    }
}
