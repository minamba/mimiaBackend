namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Ce que la page d'accueil affiche : une moyenne, une répartition, et les
    /// derniers avis publiés.
    ///
    /// LA RÉPARTITION EST RENVOYÉE MÊME QUAND ELLE EST VIDE. Cinq barres à zéro
    /// se dessinent ; cinq barres absentes obligeraient l'écran à inventer sa
    /// propre structure, et les deux finiraient par diverger.
    /// </summary>
    public class AvisPublics
    {
        /// <summary>Sur 5, arrondie au dixième. Zéro s'il n'y a aucun avis.</summary>
        public double Moyenne { get; set; }

        /// <summary>Le nombre d'avis publiés, celui qu'on affiche sous la note.</summary>
        public int Total { get; set; }

        /// <summary>
        /// Cinq entrées, de 1 à 5 étoiles dans cet ordre. Des nombres, pas des
        /// pourcentages : le calcul du pourcentage appartient à l'affichage, qui
        /// est le seul à savoir sur quoi il rapporte.
        /// </summary>
        public List<int> Repartition { get; set; } = [0, 0, 0, 0, 0];

        public List<AvisPublic> Avis { get; set; } = [];
    }

    /// <summary>Un avis tel qu'un visiteur le voit.</summary>
    public class AvisPublic
    {
        public int Id { get; set; }

        /// <summary>
        /// Prénom et initiale du nom — « Marie C. ».
        ///
        /// DÉRIVÉ À LA LECTURE, JAMAIS STOCKÉ. Un nom recopié dans la table des
        /// avis serait une donnée personnelle de plus, qu'un changement de nom
        /// ne suivrait pas et qu'un effacement de compte oublierait.
        /// </summary>
        public string? Auteur { get; set; }

        public int Note { get; set; }

        public string? Titre { get; set; }

        public string? Commentaire { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// L'équivalent de l'« achat vérifié » des sites marchands : ce parent
        /// a un abonnement payant. Un avis d'essayeur ne vaut pas celui d'une
        /// famille qui paie depuis trois mois, et le lecteur doit pouvoir faire
        /// la différence sans qu'on décide à sa place.
        /// </summary>
        public bool Abonne { get; set; }
    }

    /// <summary>Un avis tel que l'administration le voit, avant décision.</summary>
    public class AvisAdmin : AvisPublic
    {
        public string? Mail { get; set; }

        public bool Publie { get; set; }

        public DateTime? DateModification { get; set; }
    }

    /// <summary>Ce qu'un parent a écrit, et où en est son avis.</summary>
    public class MonAvis
    {
        public int Note { get; set; }

        public string? Titre { get; set; }

        public string? Commentaire { get; set; }

        public DateTime Date { get; set; }

        /// <summary>
        /// Faux tant qu'il n'a pas été relu. C'est dit au parent : un avis qui
        /// n'apparaît pas sans explication se lit comme un avis censuré.
        /// </summary>
        public bool Publie { get; set; }
    }
}
