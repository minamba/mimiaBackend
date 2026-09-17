namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une idée d'évolution SANS LES OCTETS DE SES PIÈCES JOINTES.
    ///
    /// Même règle que les bandeaux promo : une capture d'écran pèse un
    /// mégaoctet, le texte d'une idée deux cents octets. Les mêler ferait
    /// payer les images à chaque affichage du tableau. Elles sortent par leur
    /// propre route, une à la fois.
    /// </summary>
    public class Idee
    {
        public int Id { get; set; }

        public string Titre { get; set; } = string.Empty;

        /// <summary>« Basse », « Moyenne » ou « Haute ».</summary>
        public string Urgence { get; set; } = string.Empty;

        /// <summary>Le texte balisé, tel qu'il a été écrit.</summary>
        public string? Description { get; set; }

        public string Statut { get; set; } = string.Empty;

        /// <summary>Qui l'a notée, tel qu'il s'appelait ce jour-là.</summary>
        public string? AuteurPrenom { get; set; }

        public string? AuteurNom { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        /// <summary>
        /// Ses pièces jointes, sans octets : rang, nom, type et poids.
        ///
        /// IMAGES, AUDIO ET DOCUMENTS DANS LA MÊME LISTE, parce qu'ils sont la
        /// même chose en base — un fichier accroché à une idée. Ce qui les
        /// sépare est ce qu'on en fait à l'écran, et ça se lit dans
        /// <see cref="PieceJointeIdee.Genre"/>.
        /// </summary>
        public List<PieceJointeIdee> Pieces { get; set; } = [];
    }

    /// <summary>Une pièce jointe, décrite mais pas chargée.</summary>
    public class PieceJointeIdee
    {
        public int Id { get; set; }

        /// <summary>
        /// Le numéro cité par `[image:N]` dans la description.
        ///
        /// ZÉRO POUR TOUT CE QUI N'EST PAS UNE IMAGE. Un audio et un document
        /// ne s'insèrent pas dans le texte : ils se posent en dessous, comme les
        /// pièces jointes d'un courriel. Les faire entrer dans la numérotation
        /// aurait décalé les marqueurs des images à chaque dépôt d'un PDF.
        /// </summary>
        public int Rang { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }

        /// <summary>
        /// `image`, `audio` ou `document` — voir <see cref="GenrePiece"/>.
        ///
        /// EN LECTURE SEULE, DÉDUIT DU TYPE : une colonne aurait dû être
        /// remplie, migrée, et tenue d'accord avec le type mime. Ici les deux ne
        /// peuvent pas diverger.
        /// </summary>
        public string Genre => GenrePiece.Deduire(TypeMime);
    }

    /// <summary>Les octets d'une pièce jointe, sortis un par un.</summary>
    public record ImageIdee(byte[] Donnees, string TypeMime, string NomFichier);
}
