using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.ViewModels
{
    /// <summary>Une page du cours importé avec un contrôle, telle que l'écran la liste.</summary>
    public class DocumentControleViewModel
    {
        public int Id { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        /// <summary>Le professeur l'a-t-il déjà lu ? Faux pendant les quelques secondes de transcription.</summary>
        public bool Lu { get; set; }

        public DateTime DateCreation { get; set; }

        public static DocumentControleViewModel Depuis(DocumentControle d) => new()
        {
            Id = d.Id,
            NomFichier = d.NomFichier,
            TypeMime = d.TypeMime,
            Taille = d.Taille,
            NombrePages = d.NombrePages,
            Lu = d.Lu,
            DateCreation = d.DateCreation,
        };
    }
}
