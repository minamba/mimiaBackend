namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une compréhension orale, telle qu'elle s'affiche à l'élève.</summary>
    public class ComprehensionOraleEleve
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        /// <summary>
        /// La séance pendant laquelle l'exercice a eu lieu. Sert à REGROUPER
        /// les exercices d'un même cours : sans elle, six écoutes faites le
        /// même jour s'affichaient à plat, sans qu'on sache laquelle allait
        /// avec quel cours.
        /// </summary>
        public int ConversationId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        public string? Titre { get; set; }

        public string Langue { get; set; } = null!;

        public string? Passage { get; set; }

        public string? ReponseEleve { get; set; }

        public string? Comprehension { get; set; }

        public string? Remarque { get; set; }

        /// <summary>Vrai si l'audio a pu être régénéré à l'archivage.</summary>
        public bool AudioDisponible { get; set; }

        /// <summary>
        /// Quand le son a été effacé, ou null s'il ne l'a jamais été.
        ///
        /// UN AUDIO PURGÉ N'EST PAS UNE PANNE. Sans cette date, l'écran
        /// affichait « l'audio n'a pas pu être régénéré » — un message d'erreur
        /// — pour un fichier volontairement effacé après trois mois. L'enfant
        /// et son parent doivent lire la différence entre « c'est cassé » et
        /// « c'est rangé ».
        /// </summary>
        public DateTime? AudioEffaceLe { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateConsultation { get; set; }

        /// <summary>L'élève ne l'a jamais ouverte.</summary>
        public bool JamaisLue => DateConsultation is null;

        // ------------------------------------------- l'élève, pour l'en-tête
        public string? ElevePrenom { get; set; }

        public string? EleveNom { get; set; }

        public string? EleveNiveau { get; set; }
    }
}
