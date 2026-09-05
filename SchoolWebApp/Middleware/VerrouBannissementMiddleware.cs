using System.Text.Json;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Utils;

namespace SchoolWebApp.Api.Middleware
{
    /// <summary>
    /// Coupe net la session d'un compte banni.
    ///
    /// APRÈS L'AUTHENTIFICATION, ET SEULEMENT POUR CEUX QUI SONT ENTRÉS.
    /// Une requête anonyme n'a rien à vérifier : la page d'accueil, les
    /// tarifs, les avis publics et la page de contact ne traversent aucun
    /// jeton. Les faire passer par ce verrou coûterait une lecture pour rien
    /// sur les écrans les plus visités du site.
    ///
    /// UN 403 ET NON UN 401, ET LA NUANCE COMPTE. Un 401 dit « identifie-toi »,
    /// ce qui déclencherait un renouvellement de jeton et une boucle : le
    /// navigateur redemanderait un jeton, l'obtiendrait, et se ferait refuser
    /// à nouveau. Un 403 dit « je sais qui tu es, et c'est non ».
    ///
    /// LE CORPS PORTE UN CODE, pas seulement une phrase. Le navigateur doit
    /// pouvoir distinguer ce refus-ci d'un droit manquant sur une page
    /// d'administration — le premier ferme la session, le second ne doit
    /// surtout pas.
    /// </summary>
    public class VerrouBannissementMiddleware
    {
        public const string Code = "BANNI";

        private readonly RequestDelegate _suivant;
        private readonly ILogger<VerrouBannissementMiddleware> _logger;

        public VerrouBannissementMiddleware(
            RequestDelegate suivant, ILogger<VerrouBannissementMiddleware> logger)
        {
            _suivant = suivant ?? throw new ArgumentNullException(nameof(suivant));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(
            HttpContext contexte,
            IVerrouBannissement verrou,
            ICurrentUserAccessor utilisateur)
        {
            if (contexte.User?.Identity?.IsAuthenticated != true)
            {
                await _suivant(contexte);
                return;
            }

            // LES DEUX IDENTIFIANTS, parce qu'un jeton d'enfant ne porte pas
            // d'adresse : il n'a que le « sub » de son parent.
            var bloque = await verrou.EstBloqueAsync(
                utilisateur.Mail, utilisateur.IdentityUserId, contexte.RequestAborted);

            if (!bloque)
            {
                await _suivant(contexte);
                return;
            }

            _logger.LogWarning(
                "Requete refusee : compte banni ({Chemin}).", contexte.Request.Path);

            contexte.Response.StatusCode = StatusCodes.Status403Forbidden;
            contexte.Response.ContentType = "application/json; charset=utf-8";

            await contexte.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                code = Code,
                message = "Ce compte n'est plus accessible. Si vous pensez qu'il s'agit "
                        + "d'une erreur, écrivez-nous depuis la page Contact.",
            }));
        }
    }
}
