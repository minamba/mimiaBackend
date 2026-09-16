namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un parent qui ne veut plus recevoir une catégorie de courriels.
    ///
    /// PAR CATÉGORIE, PAS « TOUT OU RIEN » — Camara, le 15/09/2026. Ne plus
    /// vouloir de demandes d'avis ne veut pas dire ne plus vouloir d'annonces,
    /// et aucune de ces catégories ne concerne les bilans ni l'avertissement
    /// de fin d'essai, qui font partie du service.
    ///
    /// UNE LIGNE PAR (PARENT, CATÉGORIE), unique : cliquer deux fois sur le
    /// lien ne crée rien de plus. Se réabonner supprime la ligne.
    /// </summary>
    public partial class DesabonnementMail
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        /// <summary>avis, relance, rappels ou diffusion.</summary>
        public string Categorie { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }
    }
}
