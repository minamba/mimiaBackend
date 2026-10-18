namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une image ou un document d'un modèle de courriel.
    ///
    /// LES OCTETS SONT SUR LE DISQUE (18/10/2026, règle « aucun document en
    /// base ») : `Fichier` est leur chemin dans le dossier des templates, monté
    /// depuis l'hôte et sauvegardé. `Donnees` ne sert plus qu'au repli, tant que
    /// ce dossier n'est pas prêt — puis il est vidé au démarrage.
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

        /// <summary>Le chemin du fichier dans le dossier des templates ; nul tant qu'il est en base.</summary>
        public string? Fichier { get; set; }

        /// <summary>Les octets, seulement tant que le dossier des templates n'est pas prêt.</summary>
        public byte[]? Donnees { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual ModeleMail? ModeleMail { get; set; }
    }
}
