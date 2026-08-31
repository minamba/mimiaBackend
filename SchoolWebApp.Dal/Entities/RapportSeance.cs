namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Le compte rendu d'une séance, rédigé par le professeur en la concluant.
    ///
    /// Distinct de <see cref="Evaluation"/> : une évaluation est un contrôle
    /// annoncé, accepté et noté sur 20. Un rapport existe pour CHAQUE séance,
    /// y compris celles où l'élève n'a fait que travailler — c'est ce qui donne
    /// au parent une trace continue et pas seulement des points d'étape.
    /// </summary>
    public partial class RapportSeance
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        public int ConversationId { get; set; }

        /// <summary>Ce qui a été travaillé, avec les mots du professeur.</summary>
        public string? Travaille { get; set; }

        /// <summary>
        /// Note de compréhension sur 20 : ce que l'élève a saisi de ce qui a
        /// été vu pendant CETTE séance.
        /// </summary>
        public double? NoteComprehension { get; set; }

        /// <summary>
        /// Note de révision sur 20 : la tenue des notions déjà vues, jugée sur
        /// les questions qui les remobilisaient.
        ///
        /// Nullable, et c'est le point important : une séance entièrement
        /// consacrée à une notion neuve ne remobilise rien. Mettre zéro serait
        /// un mensonge, mettre vingt aussi — d'où l'absence de note, affichée
        /// « Pas évalué dans le cours ».
        /// </summary>
        public double? NoteRevision { get; set; }

        /// <summary>Le mot du professeur sur la séance.</summary>
        public string? Remarque { get; set; }

        /// <summary>Ce qu'il reste à retravailler. Vide quand tout est en place.</summary>
        public string? ARevoir { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }
    }
}
