namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// État de maîtrise d'une compétence par un élève, dans le temps.
    /// C'est la mémoire longue du "prof particulier" : elle est injectée dans
    /// tous les agents, quelle que soit la matière consultée.
    /// </summary>
    public partial class MaitriseEleve
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        public int CompetenceId { get; set; }

        /// <summary>Probabilité de maîtrise, de 0 à 1 (Bayesian Knowledge Tracing).</summary>
        public double Score { get; set; }

        /// <summary>
        /// Confiance dans le score, de 0 à 1. Faible après une seule observation,
        /// forte après plusieurs évaluations concordantes.
        /// </summary>
        public double Confiance { get; set; }

        /// <summary>Nombre d'observations ayant contribué au score.</summary>
        public int NombreObservations { get; set; }

        public DateTime DerniereEvaluation { get; set; }

        /// <summary>Prochaine échéance de révision espacée. Pilote les relances n8n.</summary>
        public DateTime? ProchaineRevision { get; set; }

        /// <summary>Origine de la dernière mise à jour : Diagnostic, Exercice, Conversation.</summary>
        public string? Source { get; set; }

        /// <summary>
        /// LA CLASSE DE L'ÉLÈVE À CE MOMENT-LÀ, ET NON SA CLASSE ACTUELLE.
        ///
        /// Sans elle, la fiche ne se découpe pas par année : tout le travail
        /// remonte comme s'il datait de l'année en cours. Le changement de
        /// classe est une simple mise à jour de colonne sur `Eleve`, qui ne
        /// laisse aucune trace datée — ce qui n'est pas capturé ici est perdu
        /// pour toujours.
        ///
        /// NULLABLE POUR L'EXISTANT, qui n'a pas d'année et n'en aura jamais.
        /// </summary>
        public int? NiveauScolaireId { get; set; }

        public virtual Eleve? Eleve { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }

        public virtual Competence? Competence { get; set; }
    }
}
