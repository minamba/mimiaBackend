namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Un bandeau promotionnel SANS SES OCTETS.
    ///
    /// C'est le point du modèle : les images ne traversent jamais une liste.
    /// L'administration en affiche six, la page d'accueil en lit un — ramener
    /// les visuels à chaque fois tirerait des mégaoctets de la base pour
    /// dessiner un tableau. Les octets sortent par leur propre route, un
    /// fichier à la fois, avec un cache derrière.
    /// </summary>
    public class Promo
    {
        public int Id { get; set; }

        /// <summary>Le nom interne, jamais montré aux visiteurs.</summary>
        public string Titre { get; set; } = string.Empty;

        /// <summary>Ce que lit un lecteur d'écran. Jamais vide.</summary>
        public string TexteAlternatif { get; set; } = string.Empty;

        public string? Lien { get; set; }

        public bool Actif { get; set; }

        /// <summary>
        /// Y a-t-il une version téléphone ? Faux, l'affichage retombe sur
        /// l'image large — l'écran a besoin de le savoir pour choisir sa
        /// source, et c'est aussi ce que l'administration signale à
        /// l'administrateur qui n'a téléversé qu'un fichier.
        /// </summary>
        public bool AvecImageMobile { get; set; }

        public int TailleLarge { get; set; }

        public int TailleMobile { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        /// <summary>
        /// La date qui sert d'étiquette de cache aux images. Modification si
        /// elle existe, création sinon.
        /// </summary>
        public DateTime Version => DateModification ?? DateCreation;
    }

    /// <summary>Les octets d'un visuel, avec ce qu'il faut pour les servir.</summary>
    public class ImagePromo
    {
        public byte[] Donnees { get; set; } = Array.Empty<byte>();

        public string TypeMime { get; set; } = "application/octet-stream";

        /// <summary>La version de la promo, pour l'étiquette de cache.</summary>
        public DateTime Version { get; set; }
    }
}
