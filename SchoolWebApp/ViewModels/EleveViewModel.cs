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

        public string? NiveauLibelle { get; set; }

        public string? NiveauCycle { get; set; }

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
