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

        /// <summary>
        /// La vitesse de lecture des passages d'écoute, telle que l'élève l'a
        /// choisie : `tres_lent`, `lent`, `normal` ou `rapide`.
        ///
        /// MÊME RAISON QUE LE TEMPS RESTANT : ce choix vit dans le navigateur.
        /// Sans lui, le professeur ne peut ni descendre d'un cran quand l'élève
        /// demande « plus lent », ni répondre qu'il lit déjà au plus lent.
        ///
        /// Facultatif : un cours sans exercice d'écoute n'en envoie pas, et une
        /// valeur inconnue est ignorée côté serveur plutôt que rejetée — un
        /// libellé inattendu ne doit pas faire échouer un message d'élève.
        /// </summary>
        [StringLength(20)]
        public string? VitesseEcoute { get; set; }
    }

    /// <summary>
    /// La réponse de l'élève à « L'énoncé et ta copie sont-ils séparés ? ».
    /// Envoyée À PART de la conversation, et c'est voulu : voir
    /// `ConversationsController.PoserChoixCopie`.
    /// </summary>
    public class ChoixCopieRequest
    {
        [Range(1, int.MaxValue)]
        public int ControleId { get; set; }

        public bool Separee { get; set; }
    }

    public class AccueilRequest
    {
        /// <summary>
        /// La durée choisie par l'élève pour la séance qui commence
        /// (15/25/35/45 min). Facultatif : un accueil sans minuteur n'en
        /// envoie pas, et une valeur hors des quatre proposées est ignorée
        /// côté serveur plutôt que rejetée — mieux vaut un compte rendu sans
        /// durée qu'un accueil en échec pour un détail d'affichage.
        /// </summary>
        [Range(1, 24 * 60)]
        public int? DureeChoisieMinutes { get; set; }

        /// <summary>
        /// Le contrôle que l'élève vient préparer, quand la séance a été
        /// ouverte par « Préparer ce contrôle ». Facultatif : une séance
        /// ordinaire n'en envoie pas. Vérifié côté serveur — un identifiant
        /// qui ne lui appartient pas est ignoré, jamais rejeté.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? ControleId { get; set; }

        /// <summary>
        /// D'où vient l'élève : « cours », « controle », « bilan » ou « examen »
        /// — voir `ModesSeance`. Vérifié côté serveur : ce qui ne tient pas
        /// ramène au cours normal, jamais à une erreur.
        /// </summary>
        [StringLength(20)]
        public string? Mode { get; set; }

        /// <summary>L'épreuve préparée, en mode « examen » (ex. DNB_2027_MATHS).</summary>
        [StringLength(60)]
        public string? EpreuveCode { get; set; }
    }
}
