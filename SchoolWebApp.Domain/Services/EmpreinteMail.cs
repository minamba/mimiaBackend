using System.Security.Cryptography;
using System.Text;

namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// L'empreinte d'une adresse, pour reconnaître un revenant sans conserver
    /// son adresse.
    ///
    /// POURQUOI NORMALISER AVANT DE HACHER
    /// ----------------------------------
    /// Hacher l'adresse telle quelle ne servirait presque à rien : chez Gmail,
    /// `jean.dupont@gmail.com`, `jeandupont@gmail.com` et
    /// `jean.dupont+essai@gmail.com` sont LA MÊME boîte. Trois empreintes
    /// différentes, trois essais gratuits, sans même avoir à créer un compte
    /// ailleurs. La normalisation ramène ces variantes à une seule.
    ///
    /// CE QU'ELLE NE PRÉTEND PAS FAIRE
    /// ------------------------------
    /// Arrêter quelqu'un qui possède vingt adresses distinctes. C'est
    /// impossible sans demander une carte bancaire, et une carte à l'entrée
    /// coûterait plus d'inscriptions qu'elle n'éviterait d'abus. Ce qu'on
    /// ferme ici, c'est la boucle facile — supprimer, se réinscrire,
    /// recommencer — celle qu'on trouve sans chercher.
    /// </summary>
    public static class EmpreinteMail
    {
        /// <summary>
        /// Les fournisseurs qui ignorent les points dans la partie locale.
        /// Gmail est le seul cas répandu, et il est massif.
        /// </summary>
        private static readonly string[] PointsIgnores = ["gmail.com", "googlemail.com"];

        /// <summary>
        /// Domaines qui désignent la même boîte que leur équivalent canonique.
        ///
        /// `googlemail.com` est l'ancien domaine de Gmail, toujours accepté :
        /// `jean@googlemail.com` et `jean@gmail.com` arrivent au même endroit.
        /// Sans ce repli, retirer les points ne servait à rien pour lui — les
        /// deux empreintes restaient différentes, et l'essai se reprenait en
        /// changeant un seul mot du domaine.
        /// </summary>
        private static readonly Dictionary<string, string> DomainesEquivalents =
            new() { ["googlemail.com"] = "gmail.com" };

        /// <summary>
        /// Ramène une adresse à sa forme canonique.
        ///
        /// Le `+suffixe` est retiré pour tous : c'est une convention quasi
        /// universelle chez les fournisseurs grand public, et là où elle
        /// n'existe pas, personne n'a de `+` dans son adresse — le risque de
        /// confondre deux vraies adresses distinctes est donc nul en pratique.
        /// </summary>
        public static string Normaliser(string? mail)
        {
            if (string.IsNullOrWhiteSpace(mail)) return string.Empty;

            var propre = mail.Trim().ToLowerInvariant();
            var arobase = propre.LastIndexOf('@');

            if (arobase <= 0) return propre;

            var local = propre[..arobase];
            var domaine = propre[(arobase + 1)..];

            var plus = local.IndexOf('+');
            if (plus >= 0) local = local[..plus];

            if (PointsIgnores.Contains(domaine)) local = local.Replace(".", string.Empty);

            if (DomainesEquivalents.TryGetValue(domaine, out var canonique)) domaine = canonique;

            // Une partie locale vidée par la normalisation — « +truc@… » — ne
            // doit pas se réduire à « @domaine », qui confondrait tous les
            // comptes du même fournisseur. On garde alors l'original.
            return local.Length == 0 ? propre : $"{local}@{domaine}";
        }

        /// <summary>
        /// L'empreinte à stocker. `sel` est un secret de configuration : sans
        /// lui, une fuite de la table permettrait de tester des adresses au
        /// dictionnaire et de savoir lesquelles sont inscrites chez nous.
        /// </summary>
        public static string Calculer(string? mail, string? sel)
        {
            var normalise = Normaliser(mail);
            if (normalise.Length == 0) return string.Empty;

            var octets = Encoding.UTF8.GetBytes($"{sel}|{normalise}");
            return Convert.ToHexString(SHA256.HashData(octets)).ToLowerInvariant();
        }
    }
}
