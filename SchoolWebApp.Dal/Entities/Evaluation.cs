namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une évaluation notée, passée en fin de notion.
    ///
    /// Distincte de <see cref="MaitriseEleve"/> et volontairement : la maîtrise
    /// est une estimation continue que l'observateur révise en silence après
    /// chaque séance, l'évaluation est un événement daté que l'enfant a vécu et
    /// dont le parent garde une trace. On ne peut pas déduire l'une de l'autre.
    /// </summary>
    public partial class Evaluation
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int MatiereId { get; set; }

        /// <summary>Séance pendant laquelle l'évaluation a été passée.</summary>
        public int ConversationId { get; set; }

        /// <summary>Ce sur quoi a porté l'évaluation, dit avec les mots du professeur.</summary>
        public string? Notion { get; set; }

        /// <summary>Note sur 20. Décimale : un 13,5 est une note, pas un arrondi manquant.</summary>
        public double Note { get; set; }

        /// <summary>Le mot du professeur sur cette évaluation, lisible par l'élève et par le parent.</summary>
        public string? Remarque { get; set; }

        /// <summary>Ce qu'il reste à reprendre. Vide quand tout est acquis.</summary>
        public string? ARevoir { get; set; }

        /// <summary>
        /// Le détail du contrôle en JSON : les questions, ce que l'élève a
        /// répondu, et le verdict de chacune.
        ///
        /// En JSON plutôt qu'en table fille : ce détail se lit toujours en
        /// entier, avec son évaluation, et jamais requêté question par
        /// question. Une table de plus n'apporterait qu'une jointure.
        /// </summary>
        public string? Detail { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }
    }
}
