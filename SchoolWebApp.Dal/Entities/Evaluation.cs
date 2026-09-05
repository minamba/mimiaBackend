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

        /// <summary>
        /// LA CLASSE DE L'ÉLÈVE LE JOUR DE LA NOTE, ET NON SA CLASSE ACTUELLE.
        ///
        /// Sans elle, une note prise en 4e remonte dans la fiche d'un élève de
        /// 3e comme si elle était de cette année. Le suivi mélangeait ainsi
        /// toute la scolarité en une seule courbe, alors qu'on suit le niveau
        /// d'une classe.
        ///
        /// NULLABLE, ET DÉFINITIVEMENT. Les notes antérieures à cette colonne
        /// n'ont pas de classe et n'en auront jamais : rien n'enregistrait le
        /// niveau au moment de l'évaluation, et le changement de classe est une
        /// simple mise à jour de colonne qui ne laisse aucune trace datée. On ne
        /// peut donc rien reconstituer. Ces notes se rangent dans « Toute la
        /// scolarité » plutôt que d'être attribuées au hasard à une année.
        /// </summary>
        public int? NiveauScolaireId { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual Conversation? Conversation { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }
    }
}
