using SchoolWebApp.Domain.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Auth;
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
        public async Task<IActionResult> Get(CancellationToken ct = default)
        {
            var parent = await ResoudreAsync();
            var eleves = await _eleveService.GetElevesByParentAsync(parent.Id);

            // LA CASE COCHÉE À L'INSCRIPTION arrive ici : le formulaire vit sur le serveur d'identité, avec sa propre base. Sa
            // date est celle du clic, pas celle de cette première venue.
            if (_currentUser.EleveId is null
                && !ConditionsEnVigueur.Acceptees(parent.ConditionsVersion)
                && ConditionsEnVigueur.ValentPourLaCaseDe(_currentUser.ConditionsVersionCochee, _currentUser.ConditionsCocheesLe))
            {
                await _parentService.AccepterConditionsAsync(parent.Id, ConditionsEnVigueur.Version, _currentUser.ConditionsCocheesLe!.Value, ct);
                parent.ConditionsVersion = ConditionsEnVigueur.Version;
            }

            // LA CASE DES OFFRES COCHÉE À L'INSCRIPTION (19/11/2026), même chemin, datée du clic. Appliquée UNE SEULE FOIS :
            // seulement si le parent n'a encore jamais rien décidé (ni consentement ni retrait enregistrés). Le jeton
            // porte cette date pour toujours ; sans ce garde-fou, un parent qui s'est désinscrit serait réinscrit à sa
            // connexion suivante.
            if (_currentUser.EleveId is null
                && _currentUser.OffresCocheesLe is { } offresCocheesLe
                && !parent.OffresEmail
                && parent.OffresEmailConsentiLe is null
                && parent.OffresEmailRetireLe is null)
            {
                await _parentService.ConsentirOffresEmailAsync(parent.Id, offresCocheesLe, SourceConsentement.Inscription, ct);
                parent.OffresEmail = true;
                parent.OffresEmailConsentiLe = offresCocheesLe;
                _logger.LogInformation("Parent {ParentId} : consentement aux offres par email donne a l'inscription.", parent.Id);
            }

            return Ok(new
            {
                parent.Prenom,
                parent.Nom,
                parent.Mail,
                parent.DateCreation,
                NombreEleves = eleves.Count(),

                // Lu par la page d'un enfant : faux, le tutoriel se lance (02/11/2026).
                TutorielVu = parent.TutorielVuLe != null,

                // Lu par la fiche d'un enfant : faux, son tutoriel se lance (08/11/2026).
                TutorielFicheVu = parent.TutorielFicheVuLe != null,

                // Lu par « Vos enfants » : faux, son tutoriel se lance (17/11/2026).
                TutorielEnfantsVu = parent.TutorielEnfantsVuLe != null,

                // Lus par `VerrouConditions` : vrai, le parent n'entre pas tant qu'il n'a pas accepté (08/11/2026).
                ConditionsAAccepter = !ConditionsEnVigueur.Acceptees(parent.ConditionsVersion),
                ConditionsPremiereFois = ConditionsEnVigueur.PremiereFois(parent.ConditionsVersion, parent.DateCreation),

                // Lu par `VerrouConditions` : vrai, la case des offres n'est pas reproposée à qui a déjà consenti (19/11/2026).
                parent.OffresEmail,
            });
        }

        /// <summary>
        /// L'ENFANT ET LES CONDITIONS DE SON PARENT — Camara, le 08/11/2026 : un enfant dont le parent n'a pas accepté voit un
        /// message et n'entre pas. `GET /profil` lui reste fermé, il porte le nom et l'adresse du parent : cette route-ci
        /// répond à une seule question, et rien d'autre ne part. Elle ne note aucune venue du parent (voir `ResoudreAsync`).
        /// </summary>
        [HttpGet("conditions")]
        [AutoriseEleve]
        [SwaggerOperation(Summary = "Dit si les conditions générales en vigueur restent à accepter par le parent")]
        public async Task<IActionResult> EtatConditions()
        {
            var parent = await ResoudreAsync();
            return Ok(new { ConditionsAAccepter = !ConditionsEnVigueur.Acceptees(parent.ConditionsVersion) });
        }

        /// <summary>
        /// LES CONDITIONS, ET À CÔTÉ LA CASE DES OFFRES — Min's, le 19/11/2026. Deux accords distincts (RGPD) : les conditions
        /// sont obligatoires pour entrer ; les offres par email sont facultatives, et seul `OffresEmail = true`, envoyé parce
        /// que le parent a coché lui-même la case, les fait consentir. Absent ou faux : rien ne change, ni dans un sens ni
        /// dans l'autre (un consentement déjà donné n'est pas retiré ici). Le corps est facultatif : un front plus ancien
        /// n'envoie rien.
        /// </summary>
        [HttpPost("conditions")]
        [SwaggerOperation(Summary = "Note que le parent accepte les conditions en vigueur (CGV + politique de confidentialité), et s'il le coche, les offres par email")]
        public async Task<IActionResult> AccepterConditions(
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] AccepterConditionsRequest? requete,
            CancellationToken ct)
        {
            // L'ENFANT N'ACCEPTE RIEN : son jeton porte le « sub » de son parent, seul ce test les distingue.
            if (_currentUser.EleveId is not null) return StatusCode(StatusCodes.Status403Forbidden);

            var parent = await ResoudreAsync();
            var maintenant = DateTime.UtcNow;
            await _parentService.AccepterConditionsAsync(parent.Id, ConditionsEnVigueur.Version, maintenant, ct);
            _logger.LogInformation("Parent {ParentId} : conditions {Version} (CGV + politique de confidentialite) acceptees.", parent.Id, ConditionsEnVigueur.Version);

            if (requete?.OffresEmail == true)
            {
                await _parentService.ConsentirOffresEmailAsync(parent.Id, maintenant, SourceConsentement.FenetreConditions, ct);
                _logger.LogInformation("Parent {ParentId} : consentement aux offres par email donne.", parent.Id);
            }

            return NoContent();
        }

        /// <summary>Le corps de `POST /profil/conditions`. `OffresEmail` : la case des offres, cochée par le parent.</summary>
        public sealed class AccepterConditionsRequest
        {
            public bool OffresEmail { get; set; }
        }

        [HttpPost("tutoriel-vu")]
        [SwaggerOperation(Summary = "Note que le parent a vu le tutoriel : il ne se relancera plus tout seul")]
        public async Task<IActionResult> TutorielVu(CancellationToken ct)
        {
            var parent = await ResoudreAsync();
            await _parentService.MarquerTutorielVuAsync(parent.Id, ct);
            return NoContent();
        }

        [HttpPost("tutoriel-enfants-vu")]
        [SwaggerOperation(Summary = "Note que le parent a vu le tutoriel de « Vos enfants » : il ne se relancera plus tout seul")]
        public async Task<IActionResult> TutorielEnfantsVu(CancellationToken ct)
        {
            var parent = await ResoudreAsync();
            await _parentService.MarquerTutorielEnfantsVuAsync(parent.Id, ct);
            return NoContent();
        }

        [HttpPost("tutoriel-fiche-vu")]
        [SwaggerOperation(Summary = "Note que le parent a vu le tutoriel de la fiche : il ne se relancera plus tout seul")]
        public async Task<IActionResult> TutorielFicheVu(CancellationToken ct)
        {
            var parent = await ResoudreAsync();
            await _parentService.MarquerTutorielFicheVuAsync(parent.Id, ct);
            return NoContent();
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

        /// <summary>
        /// MES COURRIELS — Camara, le 17/10/2026 : un parent désabonné ne pouvait
        /// se réabonner que par le lien d'un ancien courriel ; s'il l'avait
        /// effacé, plus rien. Ici, chaque catégorie se coche et se décoche quand
        /// il veut. Les bilans et les messages sur le compte n'y figurent pas :
        /// ils font partie du service et ne se coupent pas.
        /// </summary>
        [HttpGet("courriels")]
        [SwaggerResponse(200, "Les catégories et leur état pour ce parent.")]
        public async Task<IActionResult> Courriels(
            [FromServices] Domain.Repositories.IEnvoiAutomatiqueRepository envois, CancellationToken ct)
        {
            var parent = await ResoudreAsync();
            var categories = new[]
            {
                Domain.Models.CategorieDesabonnement.Diffusion,
                Domain.Models.CategorieDesabonnement.Avis,
                Domain.Models.CategorieDesabonnement.Relance,
                Domain.Models.CategorieDesabonnement.Rappels,
            };

            var etat = new List<object>();
            foreach (var categorie in categories)
            {
                etat.Add(new { categorie, abonne = !await envois.EstDesabonneAsync(parent.Id, categorie, ct) });
            }

            return Ok(etat);
        }

        /// <summary>S'abonne ou se désabonne d'une catégorie, depuis son compte.</summary>
        [HttpPut("courriels/{categorie}")]
        [SwaggerResponse(204, "Choix enregistré.")]
        [SwaggerResponse(400, "Catégorie inconnue.")]
        public async Task<IActionResult> ChoisirCourriel(
            string categorie,
            [FromBody] CourrielRequest requete,
            [FromServices] Domain.Repositories.IEnvoiAutomatiqueRepository envois,
            CancellationToken ct)
        {
            if (!Domain.Models.CategorieDesabonnement.EstConnue(categorie))
            {
                return BadRequest(new { message = "Catégorie inconnue." });
            }

            var parent = await ResoudreAsync();
            // POUR LES OFFRES (19/11/2026), cet interrupteur EST le consentement : l'allumer consent (daté), l'éteindre le
            // retire (daté), avec effet immédiat — voir `IEnvoiAutomatiqueRepository`.
            if (requete?.Abonne == true) await envois.ReabonnerAsync(parent.Id, categorie, SourceConsentement.EspaceParent, ct);
            else await envois.DesabonnerAsync(parent.Id, categorie, SourceConsentement.EspaceParent, ct);

            _logger.LogInformation("Parent {ParentId} {Choix} « {Categorie} » depuis son compte.",
                parent.Id, requete?.Abonne == true ? "abonne a" : "desabonne de", categorie);
            return NoContent();
        }

        /// <param name="Abonne">Vrai pour recevoir cette catégorie, faux pour ne plus la recevoir.</param>
        public record CourrielRequest(bool Abonne);

        private Task<Domain.Models.Parent> ResoudreAsync()
        {
            var identifiant = _currentUser.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                throw new UnauthorizedAccessException("Le jeton ne contient pas de claim `sub`.");
            }

            return _parentService.GetOrCreateAsync(
                identifiant, _currentUser.Mail, _currentUser.Prenom, _currentUser.Nom,

                // SEULEMENT SI CE N EST PAS L ENFANT. Son jeton porte le « sub »
                // du parent : sans ce test, chaque clic d un enfant ferait
                // passer son parent pour présent.
                noterLaVenue: _currentUser.EleveId is null);
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
