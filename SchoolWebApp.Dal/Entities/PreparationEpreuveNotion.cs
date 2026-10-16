namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// L'ÉTAT QUE LE PROFESSEUR DONNE À UNE NOTION D'UNE ÉPREUVE — Camara, le
    /// 13/10/2026 : « si on a ça pour les contrôles, on peut l'avoir pour les
    /// préparations aux examens ». Le pendant de
    /// <see cref="ControleNotion.EtatProfesseur"/>, dans une table à part :
    /// le programme d'une épreuve n'est pas une liste en base mais une RÈGLE
    /// (les compétences actives de la matière aux niveaux de l'épreuve), il
    /// n'y a donc pas de ligne par notion sur laquelle poser l'état.
    ///
    /// Une ligne par (préparation, compétence), remplacée à chaque verdict.
    /// Lue par la fiche quand elle est plus récente que la dernière mesure ;
    /// une copie notée garde le dessus. Le moteur de maîtrise, lui, n'en
    /// sait rien : la fiche de l'élève ne change pas.
    /// </summary>
    public partial class PreparationEpreuveNotion
    {
        public int Id { get; set; }

        public int PreparationId { get; set; }

        public int CompetenceId { get; set; }

        /// <summary>"tenue", "en-cours" ou "fragile" — les constantes de `PretControle`.</summary>
        public string Etat { get; set; } = null!;

        public DateTime Le { get; set; }

        public virtual PreparationEpreuve? Preparation { get; set; }

        public virtual Competence? Competence { get; set; }
    }
}
