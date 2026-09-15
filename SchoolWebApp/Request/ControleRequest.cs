using System.ComponentModel.DataAnnotations;

namespace SchoolWebApp.Api.Request
{
    /// <summary>Un contrôle posé depuis le calendrier, par le parent ou l'enfant.</summary>
    public class CreerControleRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La matière est obligatoire.")]
        public int MatiereId { get; set; }

        [StringLength(300)]
        public string? Sujet { get; set; }

        [Required]
        public DateTime DateControle { get; set; }

        public TimeSpan? HeureControle { get; set; }
    }

    /// <summary>
    /// La modification d'un contrôle : une date se décale, un sujet se
    /// précise.
    ///
    /// PAS DE MATIÈRE ICI, VOLONTAIREMENT. Elle se fige à la création : la
    /// déplacer viderait le programme et rattacherait les séances de
    /// préparation déjà faites au mauvais professeur. Une erreur de matière se
    /// répare en supprimant le contrôle, pas en le déménageant.
    /// </summary>
    public class ModifierControleRequest
    {
        [StringLength(300)]
        public string? Sujet { get; set; }

        [Required]
        public DateTime DateControle { get; set; }

        public TimeSpan? HeureControle { get; set; }
    }
}
