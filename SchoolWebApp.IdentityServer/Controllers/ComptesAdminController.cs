using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using SchoolWebApp.IdentityServer.Data;

namespace SchoolWebApp.IdentityServer.Controllers
{
    /// <summary>
    /// L'effacement d'une identité par un ADMINISTRATEUR.
    ///
    /// POURQUOI CE CONTRÔLEUR EXISTE
    /// -----------------------------
    /// Un compte vit dans deux bases : les données de la famille dans l'API
    /// métier, les identifiants ici. `CompteController` sait effacer la seconde,
    /// mais seulement pour SON titulaire — le jeton dit qui parle, et on efface
    /// celui-là.
    ///
    /// L'administrateur n'avait donc aucun chemin. Sa suppression d'un parent
    /// n'emportait que les données, et l'identité restait. Deux conséquences,
    /// dont la seconde est la grave :
    ///
    ///   1. Une réinscription avec la même adresse était refusée — « un compte
    ///      existe déjà avec cette adresse email ». C'est le symptôme visible.
    ///
    ///   2. Le parant « supprimé » pouvait SE RECONNECTER. Son identité étant
    ///      intacte, l'API lui recréait un compte parent vierge à la volée
    ///      (`GetOrCreateAsync`). La suppression effaçait les données, pas
    ///      l'accès.
    ///
    /// POURQUOI ICI ET NON DANS L'API MÉTIER
    /// -------------------------------------
    /// L'API n'a pas accès à la base d'identité, et lui donner ce pouvoir
    /// reviendrait à dupliquer la gestion des credentials dans deux
    /// applications. Le front enchaîne donc les deux appels, exactement comme il
    /// le fait déjà quand un parent efface son propre compte.
    /// </summary>
    [ApiController]
    [Route("api/admin/comptes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class ComptesAdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ComptesAdminController> _logger;

        public ComptesAdminController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<ComptesAdminController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Efface l'identité d'un autre utilisateur, désigné par son adresse.
        ///
        /// APPELÉ APRÈS l'effacement des données par l'API métier, jamais avant.
        /// C'est le même ordre que pour une suppression demandée par le parent
        /// lui-même, et pour la même raison : tant que l'identité existe,
        /// quelqu'un peut encore atteindre les données. Dans l'autre sens, on
        /// laisserait des données d'enfants sans personne pour y accéder.
        ///
        /// L'ADRESSE ET NON L'IDENTIFIANT : c'est ce que la liste des parents de
        /// l'administration affiche, et le seul lien qu'elle ait entre les deux
        /// bases. Le `sub` n'y figure pas — il est délibérément absent des
        /// réponses de l'API métier.
        /// </summary>
        [HttpDelete("{email}")]
        public async Task<IActionResult> Supprimer(string email)
        {
            // LE RÔLE SE LIT DANS LE CLAIM `role`, PAS AVEC IsInRole().
            //
            // Un jeton OpenIddict porte le rôle sous le type "role" et non sous
            // le `ClaimTypes.Role` de .NET. `IsInRole("Admin")` renverrait faux
            // sans remappage — et un contrôle d'accès qui échoue à l'ouverture
            // se remarque, alors qu'ici il aurait fermé la porte à tout le
            // monde, y compris aux administrateurs.
            //
            // ET ON REND LE CODE DIRECTEMENT, PAS `Forbid()`.
            //
            // `AddIdentity` fait du COOKIE le schéma par défaut de cette
            // application. `Forbid()` sans argument s'adresse à ce schéma-là,
            // qui répond par une REDIRECTION vers une page de refus — un 302 et
            // du HTML, là où le navigateur attend un statut. Le front aurait vu
            // une réponse incompréhensible au lieu d'un refus net.
            if (!User.HasClaim("role", "Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (string.IsNullOrWhiteSpace(email)) return BadRequest();

            var utilisateur = await _userManager.FindByEmailAsync(email.Trim());

            // DÉJÀ PARTI VAUT SUCCÈS. L'administrateur veut que ce compte
            // n'existe plus ; s'il n'existe pas, c'est fait. Rendre 404 ferait
            // échouer l'enchaînement du front sur une suppression pourtant
            // aboutie — le cas se produit dès qu'on rejoue l'opération.
            if (utilisateur is null)
            {
                _logger.LogInformation(
                    "Suppression admin : aucune identite pour cette adresse, rien a faire.");
                return NoContent();
            }

            // Le compte de démonstration ne se supprime pas, ici comme ailleurs.
            // Il est partagé, c'est celui qu'on donne à voir, et le semis de
            // démarrage ne ressuscite pas un compte disparu.
            if (EstCompteDemonstration(utilisateur.Email))
            {
                return BadRequest(new
                {
                    message = "Le compte de démonstration ne peut pas être supprimé.",
                });
            }

            // UN ADMINISTRATEUR NE S'EFFACE PAS PAR CETTE PORTE.
            //
            // Rien ne l'interdirait techniquement, et c'est justement le
            // problème : un clic dans une liste, et il perd son propre accès
            // sans avoir rien confirmé. La suppression de son compte existe, sur
            // sa page de profil, avec la saisie du prénom qui va avec.
            var soi = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(soi) && soi == utilisateur.Id)
            {
                return BadRequest(new
                {
                    message = "Vous ne pouvez pas supprimer votre propre compte depuis "
                              + "l'administration. Passez par votre page de profil.",
                });
            }

            var resultat = await _userManager.DeleteAsync(utilisateur);

            if (!resultat.Succeeded)
            {
                _logger.LogError(
                    "Echec de la suppression admin du compte {UserId} : {Erreurs}.",
                    utilisateur.Id,
                    string.Join(" ", resultat.Errors.Select(e => e.Description)));

                return StatusCode(500, new
                {
                    message = "Le compte de connexion n'a pas pu être supprimé.",
                });
            }

            // Tracé en avertissement : c'est un acte d'administration
            // irréversible sur le compte de quelqu'un d'autre.
            _logger.LogWarning(
                "Compte {UserId} supprime par un administrateur.", utilisateur.Id);

            return NoContent();
        }

        private bool EstCompteDemonstration(string? mail)
        {
            var adresse = _configuration["Admin:CompteTest:Email"];

            return !string.IsNullOrWhiteSpace(adresse)
                   && string.Equals(mail?.Trim(), adresse, StringComparison.OrdinalIgnoreCase);
        }
    }
}
