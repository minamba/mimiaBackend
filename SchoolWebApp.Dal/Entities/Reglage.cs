namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Un interrupteur du produit, actionnable depuis l'administration.
    ///
    /// Une table clé/valeur plutôt qu'une colonne par mode : le prochain
    /// interrupteur ne coûtera alors ni migration ni déploiement de schéma. Ce
    /// sont des réglages d'exploitation, pas des données métier — ils changent
    /// au rythme des décisions, pas à celui des séances.
    ///
    /// Elle vit dans la base MÉTIER et non dans celle des identités, alors même
    /// que le premier réglage change la page de connexion. C'est le produit qui
    /// décide s'il est en accès privé ; le serveur d'identité ne fait
    /// qu'appliquer.
    /// </summary>
    public class Reglage
    {
        public int Id { get; set; }

        /// <summary>Nom du réglage, en majuscules. Unique.</summary>
        public string Cle { get; set; } = string.Empty;

        /// <summary>
        /// Valeur, en texte. Un booléen s'écrit « true » ou « false ».
        ///
        /// Volontairement non typée : le jour où un réglage vaudra un nombre ou
        /// une liste, il n'y aura rien à migrer.
        /// </summary>
        public string Valeur { get; set; } = string.Empty;

        public DateTime DateModification { get; set; }
    }
}
