namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Ce qu'un enfant a fait pendant une semaine, tel qu'on le raconte à ses
    /// parents.
    ///
    /// Volontairement séparé de <see cref="FicheEleve"/> : la fiche est un état
    /// à un instant donné, pour l'administrateur. Le bilan est une tranche de
    /// temps, pour un parent — mêmes tables, questions différentes.
    /// </summary>
    public class BilanEleve
    {
        public int EleveId { get; set; }

        public string? Prenom { get; set; }

        public int Age { get; set; }

        public Sexe Sexe { get; set; }

        public string? NiveauLibelle { get; set; }

        public int ParentId { get; set; }

        public string? ParentMail { get; set; }

        public string? ParentPrenom { get; set; }

        public DateTime Debut { get; set; }

        public DateTime Fin { get; set; }

        public int NombreSeances { get; set; }

        /// <summary>Réponses du professeur : la mesure la plus proche du temps passé.</summary>
        public int NombreEchanges { get; set; }

        public DateTime? DerniereActivite { get; set; }

        public List<BilanMatiere> Matieres { get; set; } = [];

        /// <summary>
        /// Les évaluations notées de la semaine. C'est la trace que le parent
        /// conserve : le reste du bilan est du récit, celle-ci est un fait daté.
        /// </summary>
        public List<EvaluationEleve> Evaluations { get; set; } = [];

        /// <summary>Une semaine sans séance appelle un tout autre message.</summary>
        public bool Actif => NombreSeances > 0;

        // ------------------------------------------------- rédigé par l'agent
        /// <summary>Le mot du professeur, adressé au parent.</summary>
        public string? Remarque { get; set; }

        /// <summary>Professeur qui signe : celui de la matière la plus travaillée.</summary>
        public string? SignatureProf { get; set; }

        public string? SignatureMatiere { get; set; }
    }

    public class BilanMatiere
    {
        public int MatiereId { get; set; }

        public string? Libelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfCouleur { get; set; }

        public int NombreSeances { get; set; }

        public int NombreEchanges { get; set; }

        /// <summary>
        /// Transcription condensée de la semaine. Sert à faire rédiger le bilan
        /// et n'est jamais envoyée au parent : reproduire les échanges de
        /// l'enfant mot à mot serait une intrusion, pas un suivi.
        /// </summary>
        public List<string> Extraits { get; set; } = [];

        // ------------------------------------------------- rédigé par l'agent
        public string? Travaille { get; set; }

        public string? Difficultes { get; set; }
    }
}
