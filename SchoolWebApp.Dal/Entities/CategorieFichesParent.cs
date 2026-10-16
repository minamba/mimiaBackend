namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UNE CATÉGORIE DE FICHES, CRÉÉE PAR LE PARENT — 16/10/2026. Camara :
    /// « le parent crée ses propres catégories, contrairement aux enfants qui
    /// doivent sélectionner une matière ». Elle tient lieu de matière pour ses
    /// cours : le classement, la couleur, le nom donné à la lecture.
    /// </summary>
    public partial class CategorieFichesParent
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public string Libelle { get; set; } = null!;

        /// <summary>Une couleur de la palette (« #RRGGBB »).</summary>
        public string Couleur { get; set; } = null!;

        public int Ordre { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Parent? Parent { get; set; }
    }
}
