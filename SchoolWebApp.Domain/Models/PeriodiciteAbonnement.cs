namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Rythme de facturation d'un abonnement : au mois ou à l'année.
    ///
    /// Dans le DOMAINE et non dans la couche de données, parce que trois
    /// couches s'en servent — le contrôleur pour valider ce que demande le
    /// parent, le dépôt pour l'écrire, et bientôt Stripe pour choisir le tarif
    /// à prélever. La ranger près de la table obligerait l'API à dépendre de
    /// l'accès aux données pour deux constantes.
    ///
    /// Chaîne plutôt qu'énumération, comme le statut d'abonnement : la valeur
    /// se lit telle quelle en base, et un ajout futur — trimestriel, par
    /// exemple — ne décale aucun entier déjà écrit.
    /// </summary>
    public static class PeriodiciteAbonnement
    {
        public const string Mensuel = "Mensuel";
        public const string Annuel = "Annuel";

        public static bool EstValide(string? valeur) =>
            valeur == Mensuel || valeur == Annuel;

        /// <summary>
        /// Le rythme demandé est-il l'annuel ? Comparaison insensible à la
        /// casse : la valeur arrive d'une requête HTTP ou d'une étiquette
        /// Stripe, où « annuel » vaut « Annuel ». Tout le reste — y compris
        /// l'absence — est mensuel, jamais deviné à l'envers : facturer
        /// l'année à qui croyait prendre le mois se voit au prélèvement.
        /// </summary>
        public static bool EstAnnuel(string? valeur) =>
            string.Equals(valeur, Annuel, StringComparison.OrdinalIgnoreCase);
    }
}
