using Microsoft.Data.SqlClient;

namespace SchoolWebApp.IdentityServer.Services
{
    /// <summary>
    /// Le rôle d'administrateur DÉLÉGUÉ, lu dans la base métier.
    ///
    /// POURQUOI CE RÔLE NE VIT PAS DANS LA BASE D'IDENTITÉ
    /// ---------------------------------------------------
    /// Les rôles y vivent d'ordinaire, et « SuperAdmin » y reste. Mais celui-ci
    /// est accordé et retiré depuis le tableau de bord, c'est-à-dire par l'API —
    /// qui n'a pas cette base : seule la base métier lui est ouverte.
    ///
    /// Les deux solutions étaient : ouvrir un appel entre les deux services,
    /// avec le secret partagé qu'il faudrait pour l'authentifier ; ou poser le
    /// drapeau dans la base que l'API possède déjà, et le lire ici. La seconde
    /// n'ajoute ni surface d'attaque ni point de panne — ce service ouvre déjà
    /// cette base pour les interrupteurs du mode test.
    ///
    /// DEUX SOURCES, MAIS DEUX RÔLES DE NATURES DIFFÉRENTES. « SuperAdmin » est
    /// un fait de déploiement, écrit en configuration, hors de portée de
    /// l'interface : c'est ce qui garantit qu'une fausse manœuvre ne peut pas
    /// fermer la maison. « Admin » est une décision d'exploitation, prise en
    /// cours de route. Les confondre reviendrait à rendre le premier révocable.
    /// </summary>
    public interface IRolesDelegues
    {
        /// <summary>Ce compte a-t-il reçu le droit d'administrer ?</summary>
        Task<bool> EstAdministrateurAsync(string identityUserId, CancellationToken ct = default);
    }

    public class RolesDelegues : IRolesDelegues
    {
        private readonly string? _connexion;
        private readonly ILogger<RolesDelegues> _logger;

        public RolesDelegues(IConfiguration configuration, ILogger<RolesDelegues> logger)
        {
            _connexion = configuration.GetConnectionString("MetierConnection");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> EstAdministrateurAsync(
            string identityUserId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_connexion) || string.IsNullOrWhiteSpace(identityUserId))
            {
                return false;
            }

            try
            {
                await using var connexion = new SqlConnection(_connexion);
                await connexion.OpenAsync(ct);

                await using var commande = connexion.CreateCommand();
                commande.CommandText =
                    "SELECT TOP 1 est_administrateur FROM Parent WHERE identity_user_id = @id";
                commande.Parameters.AddWithValue("@id", identityUserId);

                var valeur = await commande.ExecuteScalarAsync(ct);
                return valeur is bool actif && actif;
            }
            catch (Exception ex)
            {
                // ON REFUSE PLUTÔT QUE D'ACCORDER. Une base injoignable ne doit
                // pas ouvrir l'administration : le droit délégué disparaît le
                // temps de la panne, et le super-administrateur — dont le rôle
                // vient de la configuration — garde le sien pour la réparer.
                _logger.LogWarning(ex,
                    "Role delegue non lu pour {Utilisateur} : administration refusee.",
                    identityUserId);

                return false;
            }
        }
    }
}
