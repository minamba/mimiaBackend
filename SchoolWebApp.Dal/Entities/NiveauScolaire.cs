namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Niveau de classe, du CP à la Terminale.
    /// Table de référence : pilote le contenu (programme, exercices).
    /// L'âge de l'élève est stocké séparément et pilote le ton et l'interface —
    /// les deux divergent (redoublement, avance) et ne doivent jamais être déduits l'un de l'autre.
    /// </summary>
    public partial class NiveauScolaire
    {
        public int Id { get; set; }

        /// <summary>Code court : CP, CE1, SIXIEME, TERMINALE...</summary>
        public string? Code { get; set; }

        public string? Libelle { get; set; }

        /// <summary>Primaire, College, Lycee — pilote l'interface et le ton de l'agent.</summary>
        public string? Cycle { get; set; }

        /// <summary>Rang de 1 (CP) à 12 (Terminale) : permet de remonter le graphe de prérequis.</summary>
        public int Ordre { get; set; }

        public virtual ICollection<Eleve> Eleves { get; set; } = new List<Eleve>();

        public virtual ICollection<Competence> Competences { get; set; } = new List<Competence>();
    }
}
