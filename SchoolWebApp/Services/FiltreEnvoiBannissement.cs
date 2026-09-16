using SchoolWebApp.Domain.Emails;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// La liste des adresses bannies, devant le service d'envoi de l'API.
    ///
    /// LA MÊME LECTURE QUE LE VERROU QUI COUPE DÉJÀ LEURS SESSIONS : un
    /// instantané en mémoire, oublié à chaque bannissement. Deux cents bilans
    /// ne font donc pas deux cents requêtes, et un parent banni à 9 h ne reçoit
    /// plus rien dès le courriel suivant.
    /// </summary>
    public class FiltreEnvoiBannissement : IFiltreEnvoi
    {
        private readonly IVerrouBannissement _verrou;

        public FiltreEnvoiBannissement(IVerrouBannissement verrou)
        {
            _verrou = verrou ?? throw new ArgumentNullException(nameof(verrou));
        }

        public Task<bool> EstBloqueAsync(string? adresse, CancellationToken ct = default) =>
            _verrou.EstBloqueAsync(adresse, null, ct);
    }
}
