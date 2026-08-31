using System.ComponentModel.DataAnnotations;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Request
{
    public class EleveRequest
    {
        /// <summary>Renseigné uniquement pour un PUT.</summary>
        public int Id { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100, MinimumLength = 2)]
        public string? Prenom { get; set; }

        /// <summary>
        /// Administratif : distinguer deux homonymes dans le tableau de bord.
        /// N'est jamais transmis au professeur.
        /// </summary>
        [Required(ErrorMessage = "Le nom de famille est obligatoire.")]
        [StringLength(100, MinimumLength = 2)]
        public string? Nom { get; set; }

        /// <summary>
        /// Pilote le ton et l'interface. Volontairement indépendant du niveau :
        /// un 14 ans en 5e après redoublement ne doit pas être traité comme un 11 ans.
        /// </summary>
        [Range(5, 25, ErrorMessage = "L'âge doit être compris entre 5 et 25 ans.")]
        public int Age { get; set; }

        /// <summary>
        /// Sert uniquement aux accords du professeur. Obligatoire : sans lui,
        /// une élève s'entend dire « tu es prêt ? » à chaque séance.
        /// </summary>
        [Range(1, 2, ErrorMessage = "Merci d'indiquer s'il s'agit d'une fille ou d'un garçon.")]
        public Sexe Sexe { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Le niveau scolaire est obligatoire.")]
        public int NiveauScolaireId { get; set; }

        // Pas de ParentId : il est déduit du JWT, jamais du corps de la requête.
    }
}
