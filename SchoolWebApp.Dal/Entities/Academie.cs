namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une académie française. Table de référence : détermine la ZONE de
    /// vacances scolaires de l'enfant (A, B, C, ou le calendrier propre à la
    /// Corse et à chaque académie d'outre-mer), rien d'autre.
    /// </summary>
    public partial class Academie
    {
        public int Id { get; set; }

        /// <summary>Code court, en capitales : PARIS, GUYANE...</summary>
        public string Code { get; set; } = null!;

        public string Libelle { get; set; } = null!;

        /// <summary>
        /// A, B, C — ou CORSE, GUADELOUPE, MARTINIQUE, GUYANE, REUNION,
        /// MAYOTTE pour les académies qui suivent leur propre calendrier.
        /// En texte, pas de table à part : même choix que
        /// <see cref="NiveauScolaire.Cycle"/>, personne n'interroge les zones
        /// indépendamment des académies qui les composent.
        /// </summary>
        public string Zone { get; set; } = null!;

        public virtual ICollection<Eleve> Eleves { get; set; } = new List<Eleve>();
    }
}
