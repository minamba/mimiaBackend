namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un problème ou une suggestion signalé depuis le bouton « Signaler »,
    /// visible sur toutes les pages une fois connecté.
    ///
    /// Peut venir du PARENT ou de L'ENFANT (session ouverte avec son propre
    /// code) — <see cref="EleveId"/> dit lequel. Dans tous les cas, tous les
    /// mails de suivi partent vers <see cref="ParentId"/> : un enfant n'a pas
    /// de messagerie.
    /// </summary>
    public partial class Signalement
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        /// <summary>Renseigné seulement si c'est l'enfant qui a signalé.</summary>
        public int? EleveId { get; set; }

        /// <summary>PROFESSEUR / TECHNIQUE / SUGGESTION / AUTRE.</summary>
        public string? Categorie { get; set; }

        public string? Description { get; set; }

        /// <summary>nouveau / en_cours / traite.</summary>
        public string Etat { get; set; } = "nouveau";

        public DateTime DateCreation { get; set; }

        /// <summary>Posée à chaque modification faite par un administrateur.</summary>
        public DateTime? DateMiseAJour { get; set; }

        public virtual Parent? Parent { get; set; }

        public virtual Eleve? Eleve { get; set; }
    }
}
