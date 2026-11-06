namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// UN COMMENTAIRE SUR UN SIGNALEMENT — Camara, le 03/11/2026. Une note de
    /// l'administration : le parent ne la voit pas, et aucun mail ne part.
    /// </summary>
    public partial class SignalementCommentaire
    {
        public int Id { get; set; }

        public int SignalementId { get; set; }

        /// <summary>
        /// L'adresse de l'administrateur qui l'a écrit, en texte : le commentaire
        /// garde son auteur même si ce compte disparaît un jour.
        /// </summary>
        public string? AuteurMail { get; set; }

        public string Texte { get; set; } = null!;

        public DateTime DateCreation { get; set; }

        /// <summary>Posée à chaque modification.</summary>
        public DateTime? DateMiseAJour { get; set; }

        public virtual Signalement? Signalement { get; set; }
    }
}
