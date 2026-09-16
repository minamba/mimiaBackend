using SchoolWebApp.Domain.Emails;

namespace SchoolWebApp.IdentityServer.Services
{
    /// <summary>
    /// La liste des adresses bannies, devant le service d'envoi du serveur
    /// d'identité : bienvenue, confirmation d'adresse, réinitialisation du mot
    /// de passe. Une adresse bannie ne reçoit plus rien, ceux-là compris.
    ///
    /// Lue par `IBannissementService`, qui interroge déjà la base métier pour
    /// refuser la connexion : une seule façon de reconnaître un banni.
    /// </summary>
    public class FiltreEnvoiIdentite : IFiltreEnvoi
    {
        private readonly IBannissementService _bannissement;

        public FiltreEnvoiIdentite(IBannissementService bannissement)
        {
            _bannissement = bannissement ?? throw new ArgumentNullException(nameof(bannissement));
        }

        public Task<bool> EstBloqueAsync(string? adresse, CancellationToken ct = default) =>
            _bannissement.EstBanniAsync(adresse, ct);
    }
}
