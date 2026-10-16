using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.ViewModels
{
    public class EleveViewModel
    {
        public int Id { get; set; }

        public string? Prenom { get; set; }

        public string? Nom { get; set; }

        public int Age { get; set; }

        public Sexe Sexe { get; set; }

        public int NiveauScolaireId { get; set; }

        public string? NiveauCode { get; set; }

        /// <summary>L'espagnol en LV2, choisi par la famille.</summary>
        public bool Lv2Espagnol { get; set; }

        /// <summary>Les spécialités de la voie générale cochées par la famille (codes).</summary>
        public List<string> Specialites { get; set; } = [];

        /// <summary>La filière professionnelle (15/10/2026) : famille de métiers, spécialité, et le nom libre si « Autre ».</summary>
        public string? FamilleMetiers { get; set; }

        public string? SpecialitePro { get; set; }

        public string? SpecialiteProLibre { get; set; }

        /// <summary>
        /// Les autres langues vivantes B de l'élève (ALLEMAND, ITALIEN…). En
        /// base elles vivent avec les spécialités ; ici elles en sont séparées,
        /// pour que l'écran compte ses spécialités sans compter une langue.
        /// </summary>
        public List<string> Langues { get; set; } = [];

        public string? NiveauLibelle { get; set; }

        public string? NiveauCycle { get; set; }

        public int? AcademieId { get; set; }

        public string? AcademieLibelle { get; set; }

        /// <summary>A, B, C, ou le calendrier propre à la Corse et à chaque académie d'outre-mer.</summary>
        public string? Zone { get; set; }

        /// <summary>L'établissement de l'élève, pour la conseillère d'orientation (11/10/2026).</summary>
        public string? EtablissementUai { get; set; }

        public string? EtablissementLibelle { get; set; }

        public string? EtablissementCommune { get; set; }

        /// <summary>
        /// Les besoins particuliers cochés (pour le formulaire du parent) et
        /// l'écran adapté. Jamais envoyés au navigateur de l'enfant.
        /// </summary>
        public List<string> BesoinsParticuliers { get; set; } = [];

        public bool EcranAdapte { get; set; } = true;

        public DateTime DateCreation { get; set; }

        public DateTime? DerniereActivite { get; set; }

        /// <summary>
        /// La date de retrait du profil, `null` tant qu'il est actif.
        ///
        /// Elle manquait, et l'écran des profils retirés affichait « retiré
        /// le » suivi de rien : le champ n'existait pas dans la réponse, donc
        /// le front lisait `undefined` et n'avait aucun moyen de s'en
        /// apercevoir — pas d'erreur, juste une phrase inachevée.
        ///
        /// C'est aussi le seul renseignement qui distingue un profil retiré
        /// d'un profil actif dans cette réponse. Sans lui, un écran ne peut pas
        /// savoir lequel des deux il tient en main.
        /// </summary>
        public DateTime? ArchiveLe { get; set; }

        // ParentId volontairement absent : le front n'en a pas besoin et
        // l'exposer inviterait à le passer en paramètre, ce qui rouvrirait
        // la porte à la lecture croisée entre comptes.
    }
}
