namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Le compte rendu d'une séance, tel qu'on le montre à un adulte.
    /// </summary>
    public class RapportEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Travaille { get; set; }

        /// <summary>Sur 20. Null si le professeur n'a pas su trancher.</summary>
        public double? NoteComprehension { get; set; }

        /// <summary>
        /// Sur 20, ou null quand la séance n'a porté que sur du neuf : il n'y
        /// avait alors rien à réviser, et l'interface affiche « Pas évalué dans
        /// le cours » plutôt qu'une note inventée.
        /// </summary>
        public double? NoteRevision { get; set; }

        public string? Remarque { get; set; }

        public string? ARevoir { get; set; }

        public DateTime DateCreation { get; set; }

        // ------------------------------------------- l'élève, pour l'en-tête
        public string? ElevePrenom { get; set; }

        public string? EleveNom { get; set; }

        public string? EleveNiveau { get; set; }

        /// <summary>
        /// Moyenne des deux notes, pour la pastille de la liste. Null quand la
        /// compréhension elle-même manque — une moyenne calculée sur une seule
        /// note ne serait pas une moyenne.
        /// </summary>
        public double? NoteGlobale =>
            NoteComprehension is null
                ? null
                : NoteRevision is null
                    ? NoteComprehension
                    : Math.Round((NoteComprehension.Value + NoteRevision.Value) / 2, 1);
    }
}
