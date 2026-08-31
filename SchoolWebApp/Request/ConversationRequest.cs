using System.ComponentModel.DataAnnotations;

namespace SchoolWebApp.Api.Request
{
    public class CreerConversationRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "L'élève est obligatoire.")]
        public int EleveId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La matière est obligatoire.")]
        public int MatiereId { get; set; }

        [StringLength(300)]
        public string? Titre { get; set; }
    }

    public class EnvoyerMessageRequest
    {
        /// <summary>
        /// Le texte du tour. PLUS obligatoire depuis les pièces jointes : un
        /// élève peut envoyer sa feuille sans un mot. On l'y encourage — le
        /// professeur ne saurait sinon pas ce qui le bloque — mais on ne le
        /// refuse pas, parce qu'un enfant de CE1 qui photographie son exercice
        /// a déjà fait le plus dur.
        /// </summary>
        [StringLength(10000, ErrorMessage = "Le message ne peut pas dépasser 10 000 caractères.")]
        public string? Contenu { get; set; }

        /// <summary>
        /// Le document déposé avant l'envoi, s'il y en a un. Déposé d'abord,
        /// accroché ensuite : c'est ce qui laisse à l'élève le temps d'écrire
        /// sa phrase pendant que le fichier monte.
        /// </summary>
        public int? PieceJointeId { get; set; }

        /// <summary>
        /// Temps restant dans la séance, en secondes. Facultatif — une séance
        /// sans minuteur n'en envoie pas.
        ///
        /// C'est le navigateur qui tient le chronomètre : sans cette valeur, le
        /// professeur ne peut que DEVINER combien il reste, et il devine mal —
        /// il conclut avec trois minutes au compteur.
        /// </summary>
        [Range(0, 24 * 3600)]
        public int? SecondesRestantes { get; set; }
    }
}
