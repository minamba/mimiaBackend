namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UNE IMAGE OU UN DOCUMENT D'UN BROUILLON (18/10/2026) — Camara : « en
    /// rouvrant le brouillon je n'ai que le marqueur ». Le fichier est sur le
    /// disque (<c>Chemin</c>), jamais en base ; effacé quand le message part ou
    /// que le brouillon est supprimé.
    /// </summary>
    public partial class PieceBrouillonMail
    {
        public int Id { get; set; }

        public int BrouillonId { get; set; }

        /// <summary>« Image » (placée par un marqueur <c>[image:N]</c>) ou « Document ».</summary>
        public string Genre { get; set; } = null!;

        /// <summary>L'ordre dans son genre : la première image est <c>[image:1]</c>.</summary>
        public int Rang { get; set; }

        public string NomFichier { get; set; } = null!;

        public string TypeMime { get; set; } = null!;

        public int Taille { get; set; }

        /// <summary>Le chemin du fichier, relatif au dossier des brouillons.</summary>
        public string Chemin { get; set; } = null!;

        public virtual BrouillonMail? Brouillon { get; set; }
    }
}
