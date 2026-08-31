namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Nœud du graphe de compétences — l'actif central du produit.
    /// C'est lui qui permet, quand un 4e bloque sur les équations, de remonter
    /// jusqu'à la distributivité (5e) ou aux fractions (6e).
    /// </summary>
    public partial class Competence
    {
        public int Id { get; set; }

        public int MatiereId { get; set; }

        public int NiveauScolaireId { get; set; }

        /// <summary>Code stable, ex : MATH_6E_FRAC_SIMPLIFIER.</summary>
        public string? Code { get; set; }

        /// <summary>Référence au programme officiel (Éduscol) quand elle existe.</summary>
        public string? CodeEduscol { get; set; }

        /// <summary>Domaine au sein de la matière : "Nombres et calculs", "Géométrie"...</summary>
        public string? Domaine { get; set; }

        public string? Libelle { get; set; }

        public string? Description { get; set; }

        public int Ordre { get; set; }

        public virtual Matiere? Matiere { get; set; }

        public virtual NiveauScolaire? NiveauScolaire { get; set; }

        /// <summary>Compétences qu'il faut maîtriser avant celle-ci.</summary>
        public virtual ICollection<CompetencePrerequis> Prerequis { get; set; } = new List<CompetencePrerequis>();

        /// <summary>Compétences qui dépendent de celle-ci.</summary>
        public virtual ICollection<CompetencePrerequis> Successeurs { get; set; } = new List<CompetencePrerequis>();

        public virtual ICollection<MaitriseEleve> Maitrises { get; set; } = new List<MaitriseEleve>();
    }
}
