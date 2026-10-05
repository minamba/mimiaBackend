namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UNE PAGE DU COURS DE L'ÉLÈVE, IMPORTÉE AVEC SON CONTRÔLE — voulu par
    /// Camara le 26/09/2026, sur la remontée d'un parent : « le professeur ne
    /// garde pas en mémoire le cours ». En cours ordinaire, c'est normal : une
    /// pièce jointe vit dans la séance où elle a été envoyée. Pour un contrôle,
    /// l'enfant veut que son professeur prépare AVEC sa leçon — celle de son
    /// professeur de classe, avec ses mots —, séance après séance.
    ///
    /// RATTACHÉ AU CONTRÔLE, PAS À UNE CONVERSATION : un contrôle créé depuis
    /// le calendrier n'en a aucune, et le cours doit servir à toutes les
    /// séances de préparation, puis à la correction de la copie.
    ///
    /// LE PROFESSEUR LIT LA TRANSCRIPTION, PAS LE DOCUMENT. Elle est faite une
    /// fois, par la même consigne que les pièces jointes, et posée dans le
    /// contexte mis en cache de la séance. Les octets partent dès qu'elle est
    /// écrite : ils ne servent plus à rien, et un cours photographié reste un
    /// document personnel.
    /// </summary>
    public class ControleDocument
    {
        public int Id { get; set; }

        public int ControleId { get; set; }

        public string NomFichier { get; set; } = null!;

        /// <summary>Le type LU dans les octets, jamais celui annoncé par le navigateur.</summary>
        public string TypeMime { get; set; } = null!;

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        /// <summary>Null une fois la transcription écrite — voir <see cref="DonneesEffaceesLe"/>.</summary>
        public byte[]? Donnees { get; set; }

        /// <summary>Null tant que le document n'a pas été lu.</summary>
        public string? Transcription { get; set; }

        public DateTime? DonneesEffaceesLe { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual ControleScolaire? Controle { get; set; }
    }
}
