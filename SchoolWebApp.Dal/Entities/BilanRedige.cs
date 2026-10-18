namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// CE QUE LE PROFESSEUR A ÉCRIT DANS LE BILAN D'UNE SEMAINE — 17/10/2026.
    ///
    /// Camara veut un bouton « Voir le bilan hebdomadaire » sur la fiche de
    /// l'enfant. Le bilan se recalcule depuis la base (séances, matières,
    /// chiffres) ; seul son texte vient du modèle. Le récrire à chaque
    /// ouverture coûterait un appel et, surtout, donnerait un AUTRE texte que
    /// celui du courriel reçu le lundi. Il est donc gardé ici, une fois par
    /// enfant et par semaine, et relu par l'envoi comme par l'affichage.
    ///
    /// UN PETIT TEXTE, PAS UN DOCUMENT : la réponse JSON du modèle (quelques
    /// kilo-octets). Les plus vieilles semaines sont purgées à l'écriture.
    /// </summary>
    public partial class BilanRedige
    {
        public int Id { get; set; }

        public int EleveId { get; set; }

        /// <summary>Le lundi qui clôt la semaine du bilan (minuit, UTC).</summary>
        public DateTime FinSemaine { get; set; }

        /// <summary>La réponse du modèle : remarque, travaillé et difficultés par matière.</summary>
        public string Reponse { get; set; } = null!;

        public DateTime DateCreation { get; set; }

        public virtual Eleve? Eleve { get; set; }
    }
}
