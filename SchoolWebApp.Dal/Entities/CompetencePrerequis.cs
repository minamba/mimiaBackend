namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Arête du graphe de compétences (relation plusieurs-à-plusieurs réflexive).
    /// CompetenceId dépend de PrerequisId.
    /// </summary>
    public partial class CompetencePrerequis
    {
        public int Id { get; set; }

        /// <summary>La compétence aval.</summary>
        public int CompetenceId { get; set; }

        /// <summary>La compétence qu'il faut maîtriser avant.</summary>
        public int PrerequisId { get; set; }

        /// <summary>
        /// De 1 (utile) à 3 (bloquant). Un prérequis bloquant non maîtrisé
        /// fait basculer le diagnostic vers la compétence amont.
        /// </summary>
        public int Poids { get; set; }

        public virtual Competence? Competence { get; set; }

        public virtual Competence? Prerequis { get; set; }
    }
}
