using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.ViewModels
{
    public class DocumentControleViewModel
    {
        public int Id { get; set; }

        public string NomFichier { get; set; } = string.Empty;

        public string TypeMime { get; set; } = string.Empty;

        public int Taille { get; set; }

        public int NombrePages { get; set; }

        /// <summary>Le professeur l'a-t-il déjà lu ? Faux pendant les quelques secondes de transcription.</summary>
        public bool Lu { get; set; }

        /// <summary>L'élève peut-il encore l'ouvrir ? Faux une semaine après le contrôle (fichier purgé).</summary>
        public bool Consultable { get; set; }

        /// <summary>Le fichier a été effacé par la purge : l'écran dit pourquoi l'aperçu n'est plus là.</summary>
        public bool Purge { get; set; }

        /// <summary>image, pdf, word, excel, powerpoint ou autre : l'écran choisit son aperçu.</summary>
        public string Genre { get; set; } = "autre";

        public DateTime DateCreation { get; set; }

        public static DocumentControleViewModel Depuis(DocumentControle d) => new()
        {
            Id = d.Id,
            NomFichier = d.NomFichier,
            TypeMime = d.TypeMime,
            Taille = d.Taille,
            NombrePages = d.NombrePages,
            Lu = d.Lu,
            Consultable = d.Consultable,
            Purge = d.Purge,
            Genre = d.Genre,
            DateCreation = d.DateCreation,
        };
    }
}
