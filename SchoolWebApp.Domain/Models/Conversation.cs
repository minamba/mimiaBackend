namespace SchoolWebApp.Domain.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        public string? Titre { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateDernierMessage { get; set; }

        public DateTime? DatePurge { get; set; }

        /// <summary>Moment où l'élève a explicitement quitté le cours.</summary>
        public DateTime? DateSortie { get; set; }

        public string? MatiereCode { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? AgentSlug { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }
    }
}
