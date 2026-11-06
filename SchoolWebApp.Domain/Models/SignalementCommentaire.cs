namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une note de l'administration sur un signalement, invisible du parent.</summary>
    public class SignalementCommentaire
    {
        public int Id { get; set; }

        public int SignalementId { get; set; }

        public string? AuteurMail { get; set; }

        public string Texte { get; set; } = null!;

        public DateTime DateCreation { get; set; }

        public DateTime? DateMiseAJour { get; set; }
    }
}
