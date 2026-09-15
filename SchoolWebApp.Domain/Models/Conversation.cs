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

        /// <summary>
        /// La durée choisie pour la séance en cours (15/25/35/45 min). Décrit
        /// la séance qui vient de commencer, pas la conversation entière —
        /// voir le commentaire complet sur l'entité.
        /// </summary>
        public int? DureeChoisieMinutes { get; set; }

        /// <summary>D'où vient l'élève pour la séance en cours — voir <see cref="ModesSeance"/>.</summary>
        public string? ModeSeance { get; set; }

        public int? ModeControleId { get; set; }

        public string? ModeEpreuveCode { get; set; }

        public string? MatiereCode { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? AgentSlug { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }
    }
}
