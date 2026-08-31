namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Session de dialogue entre un élève et l'agent d'une matière.
    /// </summary>
    public partial class Conversation
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        public string? Titre { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateDernierMessage { get; set; }

        /// <summary>
        /// Date de purge programmée (RGPD). Renseignée à la création selon la
        /// durée de conservation retenue ; une tâche n8n supprime au-delà.
        /// </summary>
        public DateTime? DatePurge { get; set; }

        /// <summary>
        /// Jusqu'où les échanges ont déjà été analysés pour en tirer des
        /// observations de compétences. Null = jamais analysée.
        ///
        /// Ce marqueur évite de repayer l'analyse des mêmes messages à chaque
        /// passage du worker, et garantit qu'une observation n'est comptée
        /// qu'une fois dans le calcul de maîtrise.
        /// </summary>
        public DateTime? DateDerniereObservation { get; set; }

        /// <summary>
        /// Moment où l'élève a explicitement quitté le cours.
        ///
        /// Vaut mieux que n'importe quel délai deviné : le professeur sait avec
        /// certitude que l'élève est parti, et l'accueille donc à son retour,
        /// même s'il revient deux minutes plus tard. Aucune remise à zéro n'est
        /// nécessaire — dès qu'un nouveau message arrive, il est postérieur à
        /// cette date et la condition ne joue plus.
        /// </summary>
        public DateTime? DateSortie { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

        public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

        public virtual ICollection<RapportSeance> Rapports { get; set; } = new List<RapportSeance>();
    }
}
