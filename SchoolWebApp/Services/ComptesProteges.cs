namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// Les comptes auxquels l'administration ne touche pas.
    ///
    /// POURQUOI CETTE CLASSE EXISTE
    /// ----------------------------
    /// Le super-administrateur est le seul compte qui ne peut pas être repris
    /// depuis l'interface. Sans cette garde, un administrateur — ou le
    /// super-administrateur lui-même, d'un clic malheureux — pouvait supprimer
    /// ce compte, ou lui changer son adresse. Or c'est l'ADRESSE qui porte le
    /// rôle : elle est comparée à `Admin:Emails` au démarrage du serveur
    /// d'identité. La renommer ne « déplace » donc pas le super-administrateur,
    /// elle le fait disparaître — et plus personne ne peut le rétablir depuis
    /// le site, puisque distribuer ce rôle demande précisément de l'avoir.
    ///
    /// Une seule ligne de configuration séparait le produit d'un état dont on
    /// ne sort qu'en SQL sur le serveur.
    ///
    /// LA MÊME CLÉ QUE LE SERVEUR D'IDENTITÉ, ET C'EST VOULU. `Admin:Emails`
    /// est déjà ce qui décide du rôle là-bas ; lire ailleurs ferait diverger
    /// les deux réponses au premier changement d'adresse, et la garde
    /// protégerait alors un compte qui n'est plus le bon.
    /// </summary>
    public sealed class ComptesProteges
    {
        private readonly HashSet<string> _adresses;

        public ComptesProteges(IConfiguration configuration)
        {
            _adresses = (configuration.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>())
                .Where(mail => !string.IsNullOrWhiteSpace(mail))
                .Select(mail => mail.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ce compte est-il celui du super-administrateur ?
        ///
        /// Rend faux quand la liste est vide, et c'est assumé : une
        /// configuration absente ne doit pas geler tous les comptes du site.
        /// Le risque inverse — ne rien protéger — reste visible à l'écran,
        /// puisque le badge disparaît en même temps que la garde.
        /// </summary>
        public bool Protege(string? mail) =>
            !string.IsNullOrWhiteSpace(mail) && _adresses.Contains(mail.Trim());
    }
}
