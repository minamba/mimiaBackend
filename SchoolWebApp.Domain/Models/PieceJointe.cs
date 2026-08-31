namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Le document qu'un élève montre à son professeur : la photo de son
    /// exercice, ou le PDF distribué en classe.
    ///
    /// <see cref="Donnees"/> n'est chargé que lorsqu'on en a réellement besoin
    /// — construire le tour envoyé au modèle, ou rendre l'image au navigateur.
    /// Les listes de messages, elles, ne transportent que les métadonnées :
    /// remonter les octets pour afficher un nom de fichier ferait passer
    /// plusieurs mégaoctets par requête sans que personne les regarde.
    /// </summary>
    public class PieceJointe
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public int? MessageId { get; set; }

        public string? NomFichier { get; set; }

        public string? TypeMime { get; set; }

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        public byte[]? Donnees { get; set; }

        /// <summary>Ce que le document contient, en texte. Survit aux octets.</summary>
        public string? Transcription { get; set; }

        /// <summary>Quand les octets ont été effacés. Null tant qu'il est consultable.</summary>
        public DateTime? DonneesEffaceesLe { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Les octets sont-ils encore là ?</summary>
        public bool Consultable => DonneesEffaceesLe is null;

        /// <summary>Un PDF, par opposition à une image.</summary>
        public bool EstPdf =>
            string.Equals(TypeMime, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }
}
