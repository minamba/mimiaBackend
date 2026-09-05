using SchoolWebApp.IdentityServer.Services;
using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using SchoolWebApp.IdentityServer.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SchoolWebApp.IdentityServer.Controllers
{
    /// <summary>
    /// Endpoints OIDC consommés par la SPA React :
    /// /connect/authorize, /connect/token, /connect/userinfo, /connect/logout.
    /// Le client étant first-party, aucun écran de consentement n'est présenté.
    /// </summary>
    public class AuthorizationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly IModeTestService _modeTest;
        private readonly IBannissementService _bannissements;
        private readonly IRolesDelegues _rolesDelegues;

        public AuthorizationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictScopeManager scopeManager,
            IModeTestService modeTest,
            IBannissementService bannissements,
            IRolesDelegues rolesDelegues)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _applicationManager = applicationManager ?? throw new ArgumentNullException(nameof(applicationManager));
            _scopeManager = scopeManager ?? throw new ArgumentNullException(nameof(scopeManager));
            _modeTest = modeTest ?? throw new ArgumentNullException(nameof(modeTest));
            _bannissements = bannissements
                ?? throw new ArgumentNullException(nameof(bannissements));
            _rolesDelegues = rolesDelegues ?? throw new ArgumentNullException(nameof(rolesDelegues));
        }

        /// <summary>
        /// Les rôles du compte : ceux de la base d'identité, plus le droit
        /// délégué s'il a été accordé.
        ///
        /// LES DEUX SE CUMULENT SANS SE CONFONDRE. « SuperAdmin » vient de la
        /// configuration et ne se retire pas depuis l'interface ; « Admin »
        /// vient du tableau de bord et s'y retire. Un super-administrateur
        /// porte les deux, ce qui laisse toutes les autorisations existantes
        /// fonctionner telles quelles.
        ///
        /// « Distinct » parce qu'un super-administrateur à qui on aurait aussi
        /// coché le droit délégué porterait sinon « Admin » deux fois — sans
        /// conséquence, mais un jeton ne gagne rien à répéter.
        /// </summary>
        private async Task<IReadOnlyList<string>> RolesDuCompteAsync(ApplicationUser user)
        {
            var roles = (await _userManager.GetRolesAsync(user)).ToList();

            if (await _rolesDelegues.EstAdministrateurAsync(await _userManager.GetUserIdAsync(user)))
            {
                roles.Add("Admin");
            }

            return roles.Distinct(StringComparer.Ordinal).ToList();
        }

        // ------------------------------------------------------------------
        // GET/POST /connect/authorize
        // ------------------------------------------------------------------
        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("La requête OpenID Connect n'a pas pu être récupérée.");

            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            // Utilisateur non connecté (ou prompt=login) : on renvoie vers la page de connexion.
            if (!result.Succeeded || request.HasPromptValue(PromptValues.Login))
            {
                if (request.HasPromptValue(PromptValues.None))
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "L'utilisateur n'est pas connecté."
                        }));
                }

                // Là où le visiteur voulait aller : cette même requête
                // d'autorisation, une fois qu'il se sera identifié.
                var retour = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form : Request.Query);

                // IL VOULAIT S'INSCRIRE, PAS SE CONNECTER.
                //
                // « Créer mon compte parent » l'envoyait sur le formulaire de
                // CONNEXION, où il fallait repérer un lien discret pour arriver
                // enfin là où le bouton avait promis de le mener. Un visiteur
                // qui n'a pas de compte se retrouvait devant un champ de mot de
                // passe qu'il ne pouvait pas remplir.
                //
                // Un paramètre libre plutôt que `prompt=create` : cette valeur
                // de `prompt` est une extension récente qu'OpenIddict valide et
                // refuserait. Celui-ci traverse sans être interprété.
                //
                // En accès privé, on n'y envoie personne : l'inscription est
                // fermée, et `Register` renverrait ici même. Un aller-retour
                // visible pour rien, alors que la page de connexion est
                // justement celle qui explique l'accès privé.
                if (request.GetParameter("inscription") is not null
                    && !await _modeTest.EstActifAsync())
                {
                    return RedirectToAction(
                        "Register", "Account", new { returnUrl = retour });
                }

                return Challenge(
                    authenticationSchemes: IdentityConstants.ApplicationScheme,
                    properties: new AuthenticationProperties { RedirectUri = retour });
            }

            var user = await _userManager.GetUserAsync(result.Principal)
                ?? throw new InvalidOperationException("Le compte utilisateur est introuvable.");

            // LA DERNIERE PORTE, ET LA PLUS DISCRETE.
            //
            // Refuser la connexion ne suffit pas : le navigateur d un parent
            // deja entre renouvelle son jeton en silence, par ce point de
            // terminaison, sans jamais repasser par le formulaire. Sa session
            // au serveur d identite tient des semaines. Sans cette garde, un
            // banni resterait servi tant qu il ne ferme pas son onglet.
            //
            // On DECONNECTE avant de refuser : laisser la session ouverte
            // ferait boucler le navigateur, qui redemanderait un jeton a
            // chaque essai.
            if (await _bannissements.EstBanniAsync(user.Email))
            {
                await _signInManager.SignOutAsync();

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var identity = await BuildIdentityAsync(user, request.GetScopes());

            return SignIn(new ClaimsPrincipal(identity),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // ------------------------------------------------------------------
        // POST /connect/token
        // ------------------------------------------------------------------
        [HttpPost("~/connect/token")]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("La requête OpenID Connect n'a pas pu être récupérée.");

            if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            {
                throw new NotImplementedException("Ce type de grant n'est pas supporté.");
            }

            var result = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var userId = result.Principal?.GetClaim(Claims.Subject);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: BuildError(Errors.InvalidGrant, "Le jeton fourni n'est plus valide."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: BuildError(Errors.InvalidGrant, "Le compte associé à ce jeton n'existe plus."));
            }

            if (!await _signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: BuildError(Errors.InvalidGrant, "Ce compte n'est plus autorisé à se connecter."));
            }

            var identity = await BuildIdentityAsync(user, result.Principal!.GetScopes());

            return SignIn(new ClaimsPrincipal(identity),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // ------------------------------------------------------------------
        // GET /connect/userinfo
        // ------------------------------------------------------------------
        [Microsoft.AspNetCore.Authorization.Authorize(
            AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo")]
        [HttpPost("~/connect/userinfo")]
        [Produces("application/json")]
        public async Task<IActionResult> UserInfo()
        {
            var user = await _userManager.FindByIdAsync(User.GetClaim(Claims.Subject) ?? string.Empty);
            if (user is null)
            {
                return Challenge(
                    authenticationSchemes: OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                    properties: BuildError(Errors.InvalidToken, "Le compte associé à ce jeton n'existe plus."));
            }

            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [Claims.Subject] = await _userManager.GetUserIdAsync(user)
            };

            if (User.HasScope(Scopes.Email))
            {
                claims[Claims.Email] = await _userManager.GetEmailAsync(user) ?? string.Empty;
                claims[Claims.EmailVerified] = await _userManager.IsEmailConfirmedAsync(user);
            }

            if (User.HasScope(Scopes.Profile))
            {
                claims[Claims.Name] = $"{user.Prenom} {user.Nom}".Trim();
                claims[Claims.GivenName] = user.Prenom ?? string.Empty;
                claims[Claims.FamilyName] = user.Nom ?? string.Empty;
            }

            if (User.HasScope(Scopes.Roles))
            {
                claims[Claims.Role] = await RolesDuCompteAsync(user);
            }

            return Ok(claims);
        }

        // ------------------------------------------------------------------
        // GET/POST /connect/logout
        // ------------------------------------------------------------------
        [HttpGet("~/connect/logout")]
        [HttpPost("~/connect/logout")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            await _signInManager.SignOutAsync();

            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties { RedirectUri = "/" });
        }

        // ------------------------------------------------------------------
        // Construction de l'identité émise dans les jetons
        // ------------------------------------------------------------------
        private async Task<ClaimsIdentity> BuildIdentityAsync(ApplicationUser user, IEnumerable<string> scopes)
        {
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                    .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                    .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                    .SetClaim(Claims.GivenName, user.Prenom)
                    .SetClaim(Claims.FamilyName, user.Nom)
                    .SetClaims(Claims.Role, (await RolesDuCompteAsync(user)).ToImmutableArray());

            identity.SetScopes(scopes);

            var resources = new List<string>();
            await foreach (var resource in _scopeManager.ListResourcesAsync(identity.GetScopes()))
            {
                resources.Add(resource);
            }
            identity.SetResources(resources);

            identity.SetDestinations(GetDestinations);

            return identity;
        }

        /// <summary>
        /// Décide, claim par claim, s'il part dans l'access token, l'identity token, ou les deux.
        /// Sans ça, tous les claims restent confinés à l'access token et le front ne voit rien.
        /// </summary>
        private static IEnumerable<string> GetDestinations(Claim claim)
        {
            switch (claim.Type)
            {
                case Claims.Name:
                case Claims.GivenName:
                case Claims.FamilyName:
                    yield return Destinations.AccessToken;
                    if (claim.Subject?.HasScope(Scopes.Profile) == true)
                    {
                        yield return Destinations.IdentityToken;
                    }
                    yield break;

                case Claims.Email:
                    yield return Destinations.AccessToken;
                    if (claim.Subject?.HasScope(Scopes.Email) == true)
                    {
                        yield return Destinations.IdentityToken;
                    }
                    yield break;

                case Claims.Role:
                    yield return Destinations.AccessToken;
                    if (claim.Subject?.HasScope(Scopes.Roles) == true)
                    {
                        yield return Destinations.IdentityToken;
                    }
                    yield break;

                // Jamais exposé : hash de sécurité interne d'ASP.NET Identity.
                case "AspNet.Identity.SecurityStamp":
                    yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }

        private static AuthenticationProperties BuildError(string error, string description) =>
            new(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            });
    }
}
