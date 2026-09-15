namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une évaluation proposée par le professeur, reportée à la prochaine
    /// fois — telle que le calendrier de l'élève la montre.
    /// </summary>
    public class EvaluationPrevueEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Notion { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
