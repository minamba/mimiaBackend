namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Un rapport d'orientation, tel que l'élève et son parent le lisent — écrit
    /// par la conseillère à la fin d'une séance (11/10/2026), adressé à l'élève.
    /// Chaque rubrique peut manquer : la conseillère n'écrit que ce qui a eu lieu.
    /// </summary>
    public class RapportOrientationEleve
    {
        public int Id { get; set; }

        public int? ConversationId { get; set; }

        /// <summary>Ce qu'ils se sont dit : ses goûts, ses envies, ses questions.</summary>
        public string? Discussion { get; set; }

        public string? Pistes { get; set; }

        /// <summary>Le chemin de parcours dessiné au tableau, lignes et flèches comprises.</summary>
        public string? Chemin { get; set; }

        public string? Atouts { get; set; }

        public string? AConsolider { get; set; }

        /// <summary>Les prochaines étapes concrètes.</summary>
        public string? Etapes { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Null tant que l'élève ne l'a pas ouvert.</summary>
        public DateTime? VuLe { get; set; }
    }
}
