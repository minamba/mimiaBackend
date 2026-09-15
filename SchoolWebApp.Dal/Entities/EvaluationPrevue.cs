namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une évaluation proposée par le professeur, mais reportée « à la
    /// prochaine fois » plutôt que donnée dans l'instant — soit parce que
    /// l'élève a préféré ne pas la faire tout de suite, soit parce que le
    /// temps de séance a manqué.
    ///
    /// Distincte d'<see cref="Evaluation"/> : celle-ci n'a ni note ni copie,
    /// c'est une simple annonce. Elle « se consomme » d'elle-même dès qu'une
    /// vraie évaluation est enregistrée pour cet élève et cette matière (voir
    /// <see cref="ConsommeeLe"/>) — jamais supprimée, seulement marquée.
    /// </summary>
    public partial class EvaluationPrevue
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>Résolue depuis la conversation, jamais depuis le texte du modèle.</summary>
        public int MatiereId { get; set; }

        public int ConversationId { get; set; }

        public string? Notion { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Quand la vraie évaluation a eu lieu et a effacé celle-ci de la
        /// liste « à venir ». Null tant qu'elle est encore attendue.
        /// </summary>
        public DateTime? ConsommeeLe { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }
    }
}
