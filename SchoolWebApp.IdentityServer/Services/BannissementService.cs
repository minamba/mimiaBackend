using Microsoft.Data.SqlClient;

namespace SchoolWebApp.IdentityServer.Services
{
    /// <summary>
    /// Cette adresse est-elle bannie ?
    ///
    /// POURQUOI LE REFUS TOMBE ICI ET NON DANS L'API
    /// ---------------------------------------------
    /// L'API administre la liste ; c'est le serveur d'identité qui tient les
    /// deux portes — l'inscription et la connexion. Un bannissement appliqué
    /// côté API n'arriverait qu'après l'ouverture de la session : la personne
    /// serait entrée, puis refusée écran par écran. Ici, elle n'entre pas.
    ///
    /// LECTURE SQL DIRECTE, comme pour les interrupteurs de l'onglet Modes.
    /// La page de connexion ne doit dépendre de la disponibilité d'aucun autre
    /// service web : les deux bases vivent sur la même instance, et c'est une
    /// recherche exacte sur un index unique.
    ///
    /// AUCUN CACHE, ET C'EST DÉLIBÉRÉ
    /// ------------------------------
    /// `ModeTestService` garde ses drapeaux quinze secondes, parce qu'ils sont
    /// lus à chaque affichage et qu'ils changent une fois par mois. Ici, les
    /// deux sens du retard sont fâcheux : une adresse bannie resterait
    /// admise le quart de minute suivant, et surtout un bannissement LEVÉ
    /// continuerait de refuser quelqu'un à qui on vient de rouvrir la porte —
    /// il réessaierait, échouerait, et écrirait au support.
    ///
    /// Le coût évité serait d'une requête indexée par tentative de connexion.
    /// Ce n'est pas un prix à payer en fraîcheur.
    ///
    /// EN CAS D'ÉCHEC, ON LAISSE PASSER. Le choix n'est pas neutre : une base
    /// injoignable qui conclurait « banni » fermerait le site à tout le monde,
    /// sur une panne de lecture. Le bannissement est une mesure ciblée ; il ne
    /// doit jamais devenir une panne générale.
    /// </summary>
    public interface IBannissementService
    {
        Task<bool> EstBanniAsync(string? mail, CancellationToken ct = default);
    }

    public class BannissementService : IBannissementService
    {
        private readonly string? _connexion;
        private readonly ILogger<BannissementService> _logger;

        public BannissementService(
            IConfiguration configuration, ILogger<BannissementService> logger)
        {
            // La MÊME connexion que les interrupteurs : la base métier, où
            // l'administration tient sa liste.
            _connexion = configuration.GetConnectionString("MetierConnection");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> EstBanniAsync(string? mail, CancellationToken ct = default)
        {
            // LA MÊME NORMALISATION QUE `BannissementRepository`, ET C'EST LA
            // SEULE DUPLICATION QU'ON NE PEUT PAS SUPPRIMER : ce projet ne
            // référence pas l'assemblage de l'API. Minuscules et espaces
            // retirés — si l'une des deux règles change, l'autre doit suivre,
            // sans quoi un bannissement posé ne serait plus reconnu.
            var propre = (mail ?? string.Empty).Trim().ToLowerInvariant();

            if (propre.Length == 0 || string.IsNullOrWhiteSpace(_connexion)) return false;

            try
            {
                await using var connexion = new SqlConnection(_connexion);
                await connexion.OpenAsync(ct);

                await using var commande = connexion.CreateCommand();

                // `TOP 1` et non `COUNT` : on demande une existence, pas un
                // dénombrement — le moteur s'arrête au premier trouvé.
                commande.CommandText =
                    "SELECT TOP 1 1 FROM MailBanni WHERE mail = @mail";

                commande.Parameters.Add(new SqlParameter("@mail", propre));

                return await commande.ExecuteScalarAsync(ct) is not null;
            }
            catch (Exception ex)
            {
                // ON LAISSE PASSER. Voir la note de l'interface : une lecture
                // ratée ne doit pas fermer le site à tout le monde.
                _logger.LogError(ex, "Lecture de la liste des bannis impossible.");
                return false;
            }
        }
    }
}
