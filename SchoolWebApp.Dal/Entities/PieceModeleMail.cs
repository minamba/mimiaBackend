namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une image ou un document d'un modèle de courriel.
    ///
    /// LES OCTETS SONT EN BASE, comme pour les bandeaux promotionnels : le
    /// disque du conteneur ne survit pas à un `docker restart`, et un template
    /// qui perdrait ses images au premier déploiement partirait avec des cadres
    /// vides chez tous les parents.
    ///
    /// LE RANG FAIT LA RÉFÉRENCE. `[image:2]` dans le texte désigne l'image de
    /// rang 2. Les rangs sont donc tenus SANS TROU, par genre : retirer une
    /// image renumérote les suivantes et réécrit les marqueurs du texte dans la
    /// même écriture (voir `ModeleMailRepository.RetirerPieceAsync`).
    /// </summary>
    public partial class PieceModeleMail
    {
        public int Id { get; set; }

        public int ModeleMailId { get; set; }

        /// <summary>Image ou Document — voir `GenrePieceMail`.</summary>
        public string Genre { get; set; } = string.Empty;

        /// <summary>1, 2, 3… dans son genre, sans trou.</summary>
        public int Rang { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }

        public byte[] Donnees { get; set; } = Array.Empty<byte>();

        public DateTime DateCreation { get; set; }

        public virtual ModeleMail? ModeleMail { get; set; }
    }
}
