namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une page du cours de l'élève, importée avec son contrôle — voir
    /// <c>ControleDocument</c> côté base. Le professeur en lit la
    /// transcription, jamais le document lui-même.
    /// </summary>
    public class DocumentControle
    {
        /// <summary>Au plus six documents par contrôle, comme les pièces d'un message.</summary>
        public const int Maximum = 6;

        public int Id { get; set; }

        public int ControleId { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        /// <summary>Chargées seulement quand il faut transcrire.</summary>
        public byte[]? Donnees { get; set; }

        public string? Transcription { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Le professeur peut-il le lire ? Faux tant que la transcription n'est pas écrite.</summary>
        public bool Lu => Transcription is not null;

        public bool EstPdf => TypeMime == "application/pdf";
    }
}
