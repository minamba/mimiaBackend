namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une capture d'écran ou un document joint à un signalement. Le fichier est sur le disque.</summary>
    public class SignalementPiece
    {
        public int Id { get; set; }

        public int SignalementId { get; set; }

        /// <summary>Le nom d'origine, pour le téléchargement.</summary>
        public string Nom { get; set; } = null!;

        public string TypeMime { get; set; } = null!;

        public long Taille { get; set; }

        public bool EstImage { get; set; }

        /// <summary>Le chemin relatif sur le disque. Ne sort jamais de l'API.</summary>
        public string Fichier { get; set; } = null!;

        public string? AjoutePar { get; set; }

        public DateTime DateCreation { get; set; }
    }

    /// <summary>Ce qu'a donné l'ajout d'une pièce : la pièce, ou la raison du refus.</summary>
    public sealed record AjoutPieceSignalement(
        SignalementPiece? Piece, string? Refus = null, bool Introuvable = false, bool StockageIndisponible = false);
}
