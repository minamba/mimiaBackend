namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN RAPPORT D'ORIENTATION — Camara, le 11/10/2026 : « la conseillère
    /// n'est pas un professeur ; ses fiches se nommeraient Rapports
    /// d'orientation ».
    ///
    /// Écrit par la conseillère à la fin de chaque séance, ADRESSÉ À L'ÉLÈVE
    /// (tutoiement) : ce qu'ils se sont dit, les pistes, le chemin de
    /// parcours, ses atouts, ce qui reste à consolider, les prochaines étapes.
    /// L'élève le lit sur la carte de la conseillère, le parent sur la fiche.
    ///
    /// UNE TABLE À PART, PAS LES FICHES DE RÉVISION : les professeurs lisent
    /// celles-ci pour savoir ce qui a été travaillé, et un rapport
    /// d'orientation mêlé à elles les induirait en erreur.
    /// </summary>
    public class RapportOrientation
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>La séance d'où il vient. Pas de clé étrangère : il survit à la purge des conversations.</summary>
        public int? ConversationId { get; set; }

        /// <summary>Ce qu'ils se sont dit : ses goûts, ses envies, ses questions.</summary>
        public string? Discussion { get; set; }

        public string? Pistes { get; set; }

        /// <summary>Le chemin de parcours, tel qu'il a été dessiné au tableau (texte, lignes et flèches).</summary>
        public string? Chemin { get; set; }

        public string? Atouts { get; set; }

        public string? AConsolider { get; set; }

        /// <summary>Les prochaines étapes concrètes.</summary>
        public string? Etapes { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Quand l'élève l'a ouvert ; null tant qu'il est nouveau pour lui.</summary>
        public DateTime? VuLe { get; set; }

        public virtual Eleve? Eleve { get; set; }
    }
}
