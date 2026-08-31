using Microsoft.Data.SqlClient;

namespace SchoolWebApp.IdentityServer.Services
{
    /// <summary>
    /// Le serveur d'identité est-il en accès privé ?
    ///
    /// POURQUOI IL VA LIRE DANS L'AUTRE BASE
    /// -------------------------------------
    /// Le réglage appartient au PRODUIT : c'est lui qui décide s'il est ouvert
    /// au public. Le serveur d'identité ne fait qu'appliquer, il n'a pas à en
    /// détenir sa propre copie — deux copies d'un même drapeau finissent
    /// toujours par diverger, et le jour où elles divergent, l'inscription est
    /// ouverte d'un côté et fermée de l'autre.
    ///
    /// Une lecture SQL directe plutôt qu'un appel à l'API : la page de
    /// connexion ne doit pas dépendre de la disponibilité d'un autre service
    /// web. Les deux bases vivent sur la même instance, c'est une requête sur
    /// une ligne.
    ///
    /// EN CAS D'ÉCHEC, on rend la valeur de repli configurée. Le choix n'est
    /// pas neutre : pendant la bêta, mettre `true` garantit qu'une base
    /// momentanément injoignable n'ouvre pas les inscriptions à tout le monde.
    /// </summary>
    public interface IModeTestService
    {
        /// <summary>Le service est-il en accès privé ?</summary>
        Task<bool> EstActifAsync(CancellationToken ct = default);

        /// <summary>Le compte de démonstration accepte-t-il les connexions ?</summary>
        Task<bool> CompteTestActifAsync(CancellationToken ct = default);

        /// <summary>
        /// Le site est-il en maintenance ?
        ///
        /// Lu ici et pas seulement côté navigateur : le rideau du front n'est
        /// qu'un affichage, alors que ce drapeau-ci REFUSE la connexion. C'est
        /// la différence entre cacher la porte et la fermer.
        ///
        /// FAUX PAR DÉFAUT, contrairement au mode test. Le repli du mode test
        /// est configuré à `true` pendant la bêta : une base injoignable ne
        /// doit pas ouvrir les inscriptions. Ici c'est l'inverse — une base
        /// injoignable qui conclurait « en maintenance » refuserait toutes les
        /// connexions, y compris la vôtre, sur une panne de lecture.
        /// </summary>
        Task<bool> MaintenanceAsync(CancellationToken ct = default);
    }

    public class ModeTestService : IModeTestService
    {
        private const string CleModeTest = "MODE_TEST";
        private const string CleCompteTest = "COMPTE_TEST_ACTIF";
        private const string CleMaintenance = "MAINTENANCE_ACTIVE";

        /// <summary>
        /// Durée de mise en cache. Assez courte pour qu'un basculement depuis
        /// l'administration se voie tout de suite, assez longue pour ne pas
        /// interroger la base à chaque rafraîchissement de la page.
        /// </summary>
        private static readonly TimeSpan Fraicheur = TimeSpan.FromSeconds(15);

        private readonly string? _connexion;
        private readonly bool _parDefaut;
        private readonly ILogger<ModeTestService> _logger;

        // Un cache par clé : les deux réglages ne changent pas ensemble, et
        // les lire séparément évite qu'un basculement de l'un rafraîchisse
        // inutilement l'autre.
        private readonly Dictionary<string, (bool Valeur, DateTime LuLe)> _cache = new();

        public ModeTestService(IConfiguration configuration, ILogger<ModeTestService> logger)
        {
            _connexion = configuration.GetConnectionString("MetierConnection");
            _parDefaut = configuration.GetValue("ModeTest:ParDefaut", false);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> EstActifAsync(CancellationToken ct = default) =>
            LireAsync(CleModeTest, _parDefaut, ct);

        /// <summary>
        /// Actif par défaut : le compte de démonstration existe pour servir. Un
        /// réglage jamais posé ne doit pas le rendre inutilisable.
        /// </summary>
        public Task<bool> CompteTestActifAsync(CancellationToken ct = default) =>
            LireAsync(CleCompteTest, true, ct);

        public Task<bool> MaintenanceAsync(CancellationToken ct = default) =>
            LireAsync(CleMaintenance, false, ct);

        private async Task<bool> LireAsync(string cle, bool parDefaut, CancellationToken ct)
        {
            if (_cache.TryGetValue(cle, out var connu)
                && DateTime.UtcNow - connu.LuLe < Fraicheur)
            {
                return connu.Valeur;
            }

            var valeur = parDefaut;

            if (string.IsNullOrWhiteSpace(_connexion))
            {
                // Pas de chaîne configurée : on ne journalise pas en boucle,
                // c'est un choix de déploiement, pas une panne.
                _cache[cle] = (valeur, DateTime.UtcNow);
                return valeur;
            }

            try
            {
                await using var connexion = new SqlConnection(_connexion);
                await connexion.OpenAsync(ct);

                await using var commande = connexion.CreateCommand();
                commande.CommandText = "SELECT TOP 1 valeur FROM Reglage WHERE cle = @cle";
                commande.Parameters.AddWithValue("@cle", cle);

                var lu = await commande.ExecuteScalarAsync(ct) as string;

                valeur = lu is null
                    ? parDefaut
                    : string.Equals(lu, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lecture du reglage {Cle} impossible : repli sur {ParDefaut}.", cle, parDefaut);
                valeur = parDefaut;
            }

            _cache[cle] = (valeur, DateTime.UtcNow);
            return valeur;
        }
    }
}
