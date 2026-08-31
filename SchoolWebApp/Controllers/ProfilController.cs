using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Compte du parent authentifié.
    ///
    /// Le prénom et le nom existent des deux côtés : dans la base d'identité
    /// (source des claims du jeton) et ici, dans la base métier. C'est cette
    /// copie-ci que l'application affiche, et donc celle qu'on modifie. Le
    /// mot de passe, lui, ne peut être changé que par le serveur d'identité —
    /// l'API n'a pas accès aux credentials, et c'est très bien ainsi.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("profil")]
    public class ProfilController : Controller
    {
        private readonly IParentService _parentService;
        private readonly IEleveService _eleveService;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly ILogger<ProfilController> _logger;

        public ProfilController(
            IParentService parentService,
            IEleveService eleveService,
            ICurrentUserAccessor currentUser,
            ILogger<ProfilController> logger)
        {
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
            _eleveService = eleveService ?? throw new ArgumentNullException(nameof(eleveService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Compte du parent authentifié")]
        public async Task<IActionResult> Get()
        {
            var parent = await ResoudreAsync();
            var eleves = await _eleveService.GetElevesByParentAsync(parent.Id);

            return Ok(new
            {
                parent.Prenom,
                parent.Nom,
                parent.Mail,
                parent.DateCreation,
                NombreEleves = eleves.Count(),
            });
        }

        [HttpPut]
        [SwaggerOperation(Summary = "Met à jour l'identité du parent")]
        public async Task<IActionResult> Modifier([FromBody] ProfilRequest requete)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var parent = await ResoudreAsync();

            parent.Prenom = requete.Prenom?.Trim();
            parent.Nom = requete.Nom?.Trim();

            // L'adresse n'est volontairement pas modifiable ici : c'est
            // l'identifiant de connexion. La changer sans confirmer la nouvelle
            // boîte permettrait de verrouiller un compte hors de son propriétaire.
            var maj = await _parentService.UpdateParentAsync(parent);
            if (maj is null) return NotFound();

            _logger.LogInformation("Profil du parent {ParentId} mis a jour.", parent.Id);

            return Ok(new { maj.Prenom, maj.Nom, maj.Mail, maj.DateCreation });
        }

        /// <summary>
        /// Efface le compte et tout ce qu'il contient.
        ///
        /// L'ORDRE DES TROIS ÉTAPES N'EST PAS INDIFFÉRENT.
        ///
        /// 1. Couper les prélèvements chez Stripe. En dernier, on encaisserait
        ///    encore un parent dont le compte a disparu.
        /// 2. Effacer les données ici.
        /// 3. Le compte d'identité, que l'appelant demande ENSUITE au serveur
        ///    d'identité — il vit dans une autre base, et l'API n'y touche pas.
        ///
        /// Si la troisième étape échoue, il reste une identité sans données :
        /// le parent peut se reconnecter et retomber sur un compte vide, puis
        /// redemander. C'est le sens de panne acceptable. L'inverse — effacer
        /// l'identité d'abord — laisserait des données d'enfants sans personne
        /// pour y accéder ni les réclamer, ce qui est exactement ce que la
        /// suppression doit empêcher.
        /// </summary>
        [HttpDelete]
        [SwaggerOperation(Summary = "Efface définitivement le compte et toutes ses données")]
        [SwaggerResponse(204, "Compte effacé.")]
        public async Task<IActionResult> Supprimer(
            [FromServices] IParentRepository parents,
            [FromServices] ICaisseStripeService caisse,
            CancellationToken ct)
        {
            var parent = await ResoudreAsync();

            // Tracé AVANT l'effacement : après, il n'y a plus rien pour dire
            // qui c'était. Le journal ne garde que l'identifiant technique,
            // pas les noms — la trace de l'opération, pas les données.
            _logger.LogWarning(
                "Suppression du compte {ParentId} demandee par son titulaire.", parent.Id);

            await caisse.CouperLesPrelevementsAsync(parent.Id, ct);

            var efface = await parents.SupprimerCompteAsync(parent.Id, ct);

            _logger.LogWarning(
                efface
                    ? "Compte {ParentId} efface."
                    : "Compte {ParentId} deja absent : rien a effacer.", parent.Id);

            // 204 même si le compte était déjà parti : la demande était « qu'il
            // n'en reste rien », et il n'en reste rien.
            return NoContent();
        }

        private Task<Domain.Models.Parent> ResoudreAsync()
        {
            var identifiant = _currentUser.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                throw new UnauthorizedAccessException("Le jeton ne contient pas de claim `sub`.");
            }

            return _parentService.GetOrCreateAsync(
                identifiant, _currentUser.Mail, _currentUser.Prenom, _currentUser.Nom);
        }

        public class ProfilRequest
        {
            [Required(ErrorMessage = "Le prénom est obligatoire.")]
            [StringLength(100, MinimumLength = 2)]
            public string? Prenom { get; set; }

            [Required(ErrorMessage = "Le nom est obligatoire.")]
            [StringLength(100, MinimumLength = 2)]
            public string? Nom { get; set; }
        }
    }
}
