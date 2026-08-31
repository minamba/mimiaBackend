namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Vue complète d'un élève pour l'administration.
    ///
    /// Structurée PAR MATIÈRE dès maintenant, alors qu'une seule est active.
    /// Une fiche pensée pour les maths seules devrait être redécoupée à
    /// l'ouverture du français, et l'historique accumulé d'ici là rendrait la
    /// reprise coûteuse. Ici, activer une matière la fait apparaître seule.
    /// </summary>
    public class FicheEleve
    {
        // ---------------------------------------------------------- identité
        public int Id { get; set; }

        public string? Prenom { get; set; }

        public string? Nom { get; set; }

        public int Age { get; set; }

        public Sexe Sexe { get; set; }

        public string? NiveauCode { get; set; }

        public string? NiveauLibelle { get; set; }

        public string? NiveauCycle { get; set; }

        // ------------------------------------------------------------ parent
        public int ParentId { get; set; }

        public string? ParentMail { get; set; }

        public string? ParentNomComplet { get; set; }

        // ---------------------------------------------------------- activité
        public DateTime DateCreation { get; set; }

        public DateTime? DerniereActivite { get; set; }

        /// <summary>Réponses du professeur, tous cours confondus.</summary>
        public int NombreRequetes { get; set; }

        /// <summary>Cours ouverts, toutes matières confondues.</summary>
        public int NombreCours { get; set; }

        /// <summary>Le dernier cours suivi, matière comprise. Null si l'élève n'a jamais travaillé.</summary>
        public SeanceEleve? DernierCours { get; set; }

        // ------------------------------------------------------- par matière
        /// <summary>
        /// Une entrée par matière active, y compris celles où l'élève n'a rien
        /// fait : une matière absente de la fiche se lit comme une matière qui
        /// n'existe pas, alors qu'elle signale un élève qui ne s'y met pas.
        /// </summary>
        public IEnumerable<StatMatiereEleve> Matieres { get; set; } = [];

        /// <summary>Les cours les plus récents, toutes matières confondues.</summary>
        public IEnumerable<SeanceEleve> DernieresSeances { get; set; } = [];

        // -------------------------------------------------------- compétences
        public int CompetencesEvaluees { get; set; }

        public IEnumerable<CompetenceEleve> Lacunes { get; set; } = [];

        public IEnumerable<CompetenceEleve> Acquises { get; set; } = [];

        // -------------------------------------------------------- évaluations
        /// <summary>
        /// La PREMIÈRE TRANCHE des évaluations notées, la plus récente d'abord,
        /// avec le total et de quoi demander la suite.
        ///
        /// Une tranche et non la liste entière : au bout d'une année un élève
        /// en accumule des centaines, et la fiche les envoyait toutes à chaque
        /// ouverture. Elle porte quand même la première page plutôt que de
        /// laisser l'écran la réclamer : un aller-retour de plus, c'est un
        /// tableau vide le temps qu'il revienne.
        /// </summary>
        public PageHistorique<EvaluationEleve> Evaluations { get; set; } = new();

        /// <summary>
        /// La première tranche des comptes rendus de séance. Plus nombreux que
        /// les évaluations — il y en a un par cours — et c'est ce qui donne au
        /// parent une trace continue plutôt que des points d'étape isolés.
        /// </summary>
        public PageHistorique<RapportEleve> Rapports { get; set; } = new();

        /// <summary>
        /// De quoi tracer la progression : une entrée par matière travaillée,
        /// avec la courbe des notes et l'état des notions.
        /// </summary>
        public IEnumerable<ProgressionMatiere> Progression { get; set; } = [];
    }

    /// <summary>Activité et niveau d'un élève dans une matière donnée.</summary>
    public class StatMatiereEleve
    {
        public int MatiereId { get; set; }

        public string? MatiereCode { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? ProfCouleur { get; set; }

        public int NombreCours { get; set; }

        public int NombreRequetes { get; set; }

        public DateTime? DernierCours { get; set; }

        /// <summary>Nombre de compétences évaluées dans cette matière.</summary>
        public int CompetencesEvaluees { get; set; }

        /// <summary>Maîtrise moyenne, de 0 à 1. Null tant que rien n'a été évalué.</summary>
        public double? MaitriseMoyenne { get; set; }
    }

    /// <summary>Un cours suivi.</summary>
    public class SeanceEleve
    {
        public int ConversationId { get; set; }

        public int MatiereId { get; set; }

        public string? MatiereLibelle { get; set; }

        public string? ProfPrenom { get; set; }

        public string? ProfAvatar { get; set; }

        public string? Titre { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateDernierMessage { get; set; }

        public int NombreMessages { get; set; }
    }

    /// <summary>Une compétence et son état de maîtrise pour un élève.</summary>
    public class CompetenceEleve
    {
        public int CompetenceId { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? Domaine { get; set; }

        public string? MatiereLibelle { get; set; }

        /// <summary>Le niveau où la compétence s'apprend — pas celui de l'élève.</summary>
        public string? NiveauLibelle { get; set; }

        public double Score { get; set; }

        public double Confiance { get; set; }

        public int NombreObservations { get; set; }

        public DateTime DerniereEvaluation { get; set; }
    }
}
