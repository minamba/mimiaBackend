namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une évaluation passée par un élève, telle qu'on la montre à un adulte :
    /// professeur, matière, note et remarque.
    /// </summary>
    public class EvaluationEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Notion { get; set; }

        /// <summary>Sur 20.</summary>
        public double Note { get; set; }

        public string? Remarque { get; set; }

        public string? ARevoir { get; set; }

        /// <summary>
        /// Le détail question par question. Vide pour une évaluation dont le
        /// professeur n'a pas déclaré le détail — la note reste valable, seule
        /// la copie manque.
        /// </summary>
        public List<QuestionEvaluation> Questions { get; set; } = [];

        public DateTime DateCreation { get; set; }

        // ------------------------------------------- l'élève, pour l'en-tête
        /// <summary>Rempli quand l'évaluation est lue pour être affichée en copie.</summary>
        public string? ElevePrenom { get; set; }

        public string? EleveNom { get; set; }

        public string? EleveNiveau { get; set; }
    }

    /// <summary>Une question du contrôle, telle qu'elle apparaît sur la copie.</summary>
    public class QuestionEvaluation
    {
        public int Numero { get; set; }

        public string? Enonce { get; set; }

        /// <summary>Ce que l'élève a répondu, tel qu'il l'a dit.</summary>
        public string? Reponse { get; set; }

        /// <summary>« juste », « partiel » ou « faux ».</summary>
        public string? Verdict { get; set; }

        /// <summary>Précision du professeur sur cette question. Souvent vide quand c'est juste.</summary>
        public string? Commentaire { get; set; }
    }

    /// <summary>
    /// Ce qu'un enfant a travaillé dans une matière, sous la forme qu'un
    /// graphique peut consommer directement.
    ///
    /// Deux séries et non une seule, parce qu'elles ne répondent pas à la même
    /// question : les notes disent l'évolution dans le temps, les notions
    /// disent où en est la maîtrise aujourd'hui. Les mélanger sur un même axe
    /// n'aurait aucun sens — une note sur 20 et une probabilité de maîtrise ne
    /// se comparent pas.
    /// </summary>
    public class ProgressionMatiere
    {
        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        /// <summary>Les notes dans l'ordre chronologique : la courbe de progression.</summary>
        public List<PointNote> Notes { get; set; } = [];

        /// <summary>Les notions abordées et leur niveau de maîtrise actuel.</summary>
        public List<NotionMaitrisee> Notions { get; set; } = [];

        /// <summary>Moyenne des notes de la matière. Null tant qu'aucune évaluation n'a eu lieu.</summary>
        public double? Moyenne => Notes.Count == 0 ? null : Notes.Average(n => n.Note);
    }

    public class PointNote
    {
        public DateTime Date { get; set; }

        public double Note { get; set; }

        public string? Notion { get; set; }

        /// <summary>
        /// La classe où l'élève était le jour de l'évaluation.
        ///
        /// NULL POUR LES NOTES ANTÉRIEURES À CE CHAMP, et c'est irrattrapable :
        /// rien n'enregistrait la classe de l'élève au moment de la note, et le
        /// changement de classe ne laisse aucune trace datée. On ne peut donc
        /// pas la reconstituer après coup. Ces notes-là restent rangées dans
        /// « Toute la scolarité ».
        /// </summary>
        public string? NiveauLibelle { get; set; }

        public int? NiveauOrdre { get; set; }
    }

    public class NotionMaitrisee
    {
        public int CompetenceId { get; set; }

        public string? Libelle { get; set; }

        public string? Domaine { get; set; }

        /// <summary>Niveau où la notion s'apprend — pas celui de l'élève.</summary>
        public string? NiveauLibelle { get; set; }

        /// <summary>
        /// Le RANG de ce niveau, et non son libellé, parce que c'est lui qui
        /// ordonne l'historique. Trier sur le libellé rangerait « CM2 » avant
        /// « 6e » — l'ordre alphabétique n'est pas l'ordre scolaire.
        /// </summary>
        public int NiveauOrdre { get; set; }

        /// <summary>Maîtrise estimée, de 0 à 1.</summary>
        public double Score { get; set; }

        public double Confiance { get; set; }

        public int NombreObservations { get; set; }

        public DateTime DerniereEvaluation { get; set; }
    }
}
