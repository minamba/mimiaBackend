using System.ComponentModel.DataAnnotations;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Request
{
    public class ParentAdminRequest
    {
        [StringLength(100)]
        public string? Prenom { get; set; }

        [StringLength(100)]
        public string? Nom { get; set; }

        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [StringLength(255)]
        public string? Mail { get; set; }
    }

    public class EleveAdminRequest
    {
        [StringLength(100, MinimumLength = 2)]
        public string? Prenom { get; set; }

        [StringLength(100, MinimumLength = 2)]
        public string? Nom { get; set; }

        [Range(5, 25, ErrorMessage = "L'âge doit être compris entre 5 et 25 ans.")]
        public int? Age { get; set; }

        public int? NiveauScolaireId { get; set; }

        /// <summary>Corrige les accords du professeur. Ignoré s'il vaut NonPrecise.</summary>
        public Sexe? Sexe { get; set; }
    }

    /// <summary>
    /// Un ajustement manuel du pot d'heures.
    /// </summary>
    public class AjustementHeuresRequest
    {
        /// <summary>
        /// Positif pour ajouter, négatif pour retirer. En MINUTES et non en
        /// heures : les packs se comptent en minutes partout ailleurs, et
        /// deux unités dans le même écran finissent par se confondre.
        /// </summary>
        public int Minutes { get; set; }

        /// <summary>
        /// Pourquoi. Obligatoire — voir la route.
        ///
        /// LU PAR LE PARENT quand <see cref="PrevenirLeParent"/> est vrai : il
        /// devient le corps du courriel. Un motif écrit pour soi-même
        /// (« encore un partiel ») passe mal une fois adressé à un client.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string Motif { get; set; } = string.Empty;

        /// <summary>
        /// Faut-il écrire au parent ?
        ///
        /// VRAI PAR DÉFAUT, ET C'EST LE SENS QUI COMPTE. Un solde qui bouge
        /// sans explication ne se lit pas comme « il doit y avoir une raison »
        /// mais comme une panne — ou comme un prélèvement qu'on n'a pas
        /// demandé. Le silence coûte plus cher qu'un courriel de trop.
        ///
        /// L'exception existe pour les corrections internes : créditer le
        /// mauvais compte et se reprendre trente secondes plus tard enverrait
        /// deux messages contradictoires en une minute.
        /// </summary>
        public bool PrevenirLeParent { get; set; } = true;
    }

    /// <summary>Le texte d'une réponse du support.</summary>
    public class ReponseMessageRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(10000)]
        public string Texte { get; set; } = string.Empty;
    }
}
