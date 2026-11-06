namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UNE CAPTURE D'ÉCRAN OU UN DOCUMENT JOINT À UN SIGNALEMENT — Camara, le 03/11/2026.
    /// La base ne garde que le chemin : voir IStockageSignalements.
    /// </summary>
    public partial class SignalementPiece
    {
        public int Id { get; set; }

        public int SignalementId { get; set; }

        public string Nom { get; set; } = null!;

        public string TypeMime { get; set; } = null!;

        public long Taille { get; set; }

        public bool EstImage { get; set; }

        public string Fichier { get; set; } = null!;

        public string? AjoutePar { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Signalement? Signalement { get; set; }
    }
}
