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

        /// <summary>Trois lectures plantées, et le document est abandonné (16/10/2026).</summary>
        public const int LecturesMax = 3;

        /// <summary>
        /// La mention que le professeur lit à la place du cours abandonné : elle
        /// lui dit quoi demander à l'élève, dont l'écran n'affiche pas l'état
        /// de lecture.
        /// </summary>
        public const string MentionAbandon =
            PrefixeIllisible + " : la lecture a échoué trois fois — demande à l'élève de le réimporter, "
            + "en photo nette ou en PDF)";

        /// <summary>
        /// Toute transcription qui commence ainsi n'est PAS un cours : c'est une
        /// mention d'échec (lecture vide, ou abandon après trois lectures).
        /// </summary>
        public const string PrefixeIllisible = "(document illisible";

        /// <summary>La mention écrite quand la lecture ne rend aucun texte.</summary>
        public const string MentionVide = PrefixeIllisible + " : transcription automatique vide)";

        public int Id { get; set; }

        public int ControleId { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        /// <summary>Chargées seulement quand il faut transcrire — les documents d'avant le disque (04/11/2026).</summary>
        public byte[]? Donnees { get; set; }

        /// <summary>
        /// Le chemin du fichier sur le disque (voir <c>IStockageCoursControles</c>) ; null
        /// pour un document d'avant le 04/11/2026, ou une fois le fichier effacé.
        /// </summary>
        public string? Chemin { get; set; }

        /// <summary>Quand la purge a effacé le fichier : une semaine après le contrôle.</summary>
        public DateTime? FichierEffaceLe { get; set; }

        /// <summary>L'élève peut-il encore l'ouvrir ? Faux avant le disque, et après la purge.</summary>
        public bool Consultable => Chemin is not null;

        /// <summary>Le fichier a existé sur le disque et a été purgé : l'écran le dit.</summary>
        public bool Purge => Chemin is null && FichierEffaceLe is not null;

        /// <summary>La famille du fichier, pour l'aperçu : image, pdf, word, excel, powerpoint ou autre.</summary>
        public string Genre => GenreDe(TypeMime);

        public static string GenreDe(string? typeMime) => typeMime switch
        {
            null => "autre",
            "application/pdf" => "pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "word",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "excel",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "powerpoint",
            _ when typeMime.StartsWith("image/", StringComparison.Ordinal) => "image",
            _ => "autre",
        };

        public string? Transcription { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>Le professeur peut-il le lire ? Faux tant que la transcription n'est pas écrite.</summary>
        public bool Lu => Transcription is not null;

        public bool EstPdf => TypeMime == "application/pdf";

        /// <summary>
        /// Lu, mais sans cours dedans : la transcription est une mention
        /// d'échec. Posé par les requêtes qui ne chargent pas le texte
        /// (<c>GetDocumentsAsync</c>), lu par le fait du tour (26/10/2026) : un
        /// tel document ne compte pas comme « cours importé », sinon le
        /// professeur lirait « ne lui redemande pas » à côté de « demande-lui
        /// de le réimporter ».
        /// </summary>
        public bool Illisible { get; set; }

        /// <summary>Un cours que le professeur peut suivre : lu, ou pas encore lu (il le sera à la volée).</summary>
        public bool Exploitable => !Illisible;
    }
}
