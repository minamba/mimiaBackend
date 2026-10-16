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

        /// <summary>
        /// Détermine la zone de vacances scolaires. Facultatif : un parent
        /// qui ne la connaît pas ou ne s'en soucie pas ne doit pas être
        /// bloqué à la création — le calendrier se dégrade simplement tant
        /// qu'elle n'est pas renseignée.
        /// </summary>
        public int? AcademieId { get; set; }

        /// <summary>
        /// L'enfant a l'espagnol en LV2. Coché par le parent, seulement proposé
        /// dans les classes où une LV2 existe ; ailleurs il est sans effet.
        /// </summary>
        public bool Lv2Espagnol { get; set; }

        /// <summary>
        /// Les spécialités de la voie générale cochées par le parent (codes). Le
        /// serveur n'en garde que ce que la classe permet — voir
        /// `VoiesScolaires.SpecialitesRetenues`.
        /// </summary>
        public List<string>? Specialites { get; set; }

        /// <summary>
        /// LA FILIÈRE PROFESSIONNELLE (15/10/2026) : la famille de métiers en
        /// seconde pro, la spécialité en première et terminale pro, et le nom
        /// écrit à la main quand c'est « Autre ». Les trois à null : l'écran ne
        /// les envoie pas, ce qui était enregistré est gardé. Voir
        /// `VoiesScolaires.FiliereProRetenue`.
        /// </summary>
        public string? FamilleMetiers { get; set; }

        public string? SpecialitePro { get; set; }

        public string? SpecialiteProLibre { get; set; }

        /// <summary>
        /// Les autres langues vivantes B cochées par le parent (codes de
        /// matière : ALLEMAND, ITALIEN…). Null : le champ n'a pas été envoyé —
        /// un écran antérieur — et rien n'est changé. Voir
        /// `VoiesScolaires.LanguesAuChoix`.
        /// </summary>
        public List<string>? Langues { get; set; }

        /// <summary>
        /// Les besoins particuliers cochés par le parent (DYSLEXIE, DYSCALCULIE,
        /// TDAH) ; la coche vaut accord. Null : rien n'est changé.
        /// </summary>
        public List<string>? BesoinsParticuliers { get; set; }

        /// <summary>L'écran adapté ; null : rien n'est changé.</summary>
        public bool? EcranAdapte { get; set; }

        // Pas de ParentId : il est déduit du JWT, jamais du corps de la requête.
    }
}
