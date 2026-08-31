namespace SchoolWebApp.Api.Services.Paiement
{
    /// <summary>
    /// Réglages Stripe.
    ///
    /// La clé ne figure jamais dans le fichier versionné : elle vient
    /// d'appsettings.Development.json en local et d'une variable
    /// d'environnement en production.
    /// </summary>
    public class OptionsStripe
    {
        public const string Section = "Stripe";

        /// <summary>
        /// Clé secrète. `sk_test_…` en développement, `sk_live_…` en production.
        /// </summary>
        public string? CleSecrete { get; set; }

        /// <summary>
        /// Secret de signature des webhooks.
        ///
        /// En local il est donné par `stripe listen` et CHANGE à chaque
        /// lancement du CLI ; en production il est fixe et vient du tableau de
        /// bord. Sans lui, on ne peut pas vérifier qu'un événement vient bien
        /// de Stripe — et un webhook non vérifié est une porte ouverte :
        /// n'importe qui pourrait annoncer un paiement qui n'a pas eu lieu.
        /// </summary>
        public string? SecretWebhook { get; set; }

        /// <summary>Où Stripe renvoie le parent après un paiement réussi.</summary>
        public string? UrlRetour { get; set; }

        /// <summary>Où Stripe le renvoie s'il abandonne.</summary>
        public string? UrlAnnulation { get; set; }

        public bool EstConfigure => !string.IsNullOrWhiteSpace(CleSecrete);

        /// <summary>La clé pointe-t-elle vers de l'argent réel ?</summary>
        public bool EstEnProduction =>
            CleSecrete?.StartsWith("sk_live_", StringComparison.Ordinal) == true;

        /// <summary>
        /// Refuse de démarrer sur une combinaison dangereuse.
        ///
        /// UNE CLÉ DE PRODUCTION EN DÉVELOPPEMENT est le cas grave : déboguer
        /// un tunnel de paiement, c'est le parcourir vingt fois, se tromper,
        /// recommencer. Avec une clé `sk_live_`, chacun de ces essais devient
        /// un vrai débit sur une vraie carte, et chaque correction de bug se
        /// paie en remboursements à demander.
        ///
        /// Une exception au démarrage plutôt qu'un avertissement dans les
        /// logs : un avertissement se lit après coup, quand l'argent est déjà
        /// parti. On veut que l'application refuse de se lancer.
        ///
        /// Le cas inverse — clé de test en production — ne détruit rien : les
        /// paiements échouent simplement à exister. Il mérite un cri dans les
        /// logs, pas un arrêt : couper un site en ligne pour ça ferait plus de
        /// dégâts que le défaut lui-même.
        /// </summary>
        public void Verifier(bool environnementDeDeveloppement, ILogger logger)
        {
            if (!EstConfigure) return;

            if (environnementDeDeveloppement && EstEnProduction)
            {
                throw new InvalidOperationException(
                    "Clé Stripe de PRODUCTION (sk_live_) détectée en environnement de "
                    + "développement. Chaque essai de paiement débiterait une vraie carte. "
                    + "Remplacez-la par une clé de test (sk_test_) dans "
                    + "appsettings.Development.json.");
            }

            if (!environnementDeDeveloppement && !EstEnProduction)
            {
                logger.LogCritical(
                    "Clé Stripe de TEST en production : aucun paiement réel ne sera encaissé.");
            }
        }
    }
}
