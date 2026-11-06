using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    public class SignalementRequest
    {
        /// <summary>PROFESSEUR / TECHNIQUE / SUGGESTION / AUTRE.</summary>
        [Required]
        public string? Categorie { get; set; }

        [Required, StringLength(2000, MinimumLength = 3)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Le bouton « Signaler », accessible dès qu'on est connecté — parent
    /// comme enfant.
    ///
    /// TOUS LES MAILS PARTENT VERS LE PARENT, y compris quand c'est l'enfant
    /// qui a signalé : lui n'a pas de messagerie. `ICurrentUserAccessor`
    /// porte le même `sub` dans les deux cas (voir `AuthentificationEleve`),
    /// donc `_parentService` résout toujours le bon compte.
    /// </summary>
    [ApiController]
    [Authorize]
    [SchoolWebApp.Api.Auth.AutoriseEleve]
    [Route("signalements")]
    public class SignalementsController : ControllerBase
    {
        private static readonly HashSet<string> CategoriesConnues =
            new(StringComparer.OrdinalIgnoreCase) { "PROFESSEUR", "TECHNIQUE", "SUGGESTION", "AUTRE" };

        private static readonly Dictionary<string, string> LibellesCategorie = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PROFESSEUR"] = "Problème avec un professeur",
            ["TECHNIQUE"] = "Problème technique",
            ["SUGGESTION"] = "Suggestion",
            ["AUTRE"] = "Autre",
        };

        private readonly ISignalementService _signalements;
        private readonly IParentService _parentService;
        private readonly IEleveService _eleveService;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly IServiceEmail _email;
        private readonly ITelegramService _telegram;
        private readonly IEvenementsAdminHub _evenementsAdmin;
        private readonly ILogger<SignalementsController> _logger;

        public SignalementsController(
            ISignalementService signalements,
            IParentService parentService,
            IEleveService eleveService,
            ICurrentUserAccessor currentUser,
            IServiceEmail email,
            ITelegramService telegram,
            IEvenementsAdminHub evenementsAdmin,
            ILogger<SignalementsController> logger)
        {
            _evenementsAdmin = evenementsAdmin ?? throw new ArgumentNullException(nameof(evenementsAdmin));
            _signalements = signalements ?? throw new ArgumentNullException(nameof(signalements));
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
            _eleveService = eleveService ?? throw new ArgumentNullException(nameof(eleveService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _telegram = telegram ?? throw new ArgumentNullException(nameof(telegram));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [SwaggerResponse(201, "Signalement enregistré.")]
        [SwaggerResponse(400, "Catégorie ou description invalide.")]
        public async Task<IActionResult> Creer([FromBody] SignalementRequest requete, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var categorie = requete.Categorie?.Trim().ToUpperInvariant();
            if (categorie is null || !CategoriesConnues.Contains(categorie))
            {
                return BadRequest(new { message = "Catégorie inconnue." });
            }

            var description = requete.Description!.Trim();

            var identifiant = _currentUser.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                throw new UnauthorizedAccessException("Le jeton ne contient pas de claim `sub`.");
            }

            // SEULEMENT SI CE N'EST PAS L'ENFANT, même garde qu'ailleurs :
            // sans elle, chaque signalement d'un enfant ferait passer son
            // parent pour présent.
            var parent = await _parentService.GetOrCreateAsync(
                identifiant, _currentUser.Mail, _currentUser.Prenom, _currentUser.Nom,
                noterLaVenue: _currentUser.EleveId is null);

            if (string.IsNullOrWhiteSpace(parent.Mail))
            {
                return BadRequest(new { message = "Aucune adresse e-mail n'est associée à ce compte." });
            }

            var eleveId = _currentUser.EleveId;
            string? elevePrenom = null;

            if (eleveId is int id)
            {
                var eleve = await _eleveService.GetEleveByIdAsync(id);
                elevePrenom = eleve?.Prenom;
            }

            var signalement = await _signalements.CreerAsync(parent.Id, eleveId, categorie, description, ct);

            var theme = LibellesCategorie.GetValueOrDefault(categorie, categorie);

            var envoye = await _email.EnvoyerAsync(
                parent.Mail,
                "Votre signalement a bien été reçu",
                "signalement-recu",
                new Dictionary<string, string>
                {
                    ["theme"] = theme,
                    ["description"] = description,
                },
                ct);

            if (!envoye)
            {
                _logger.LogError(
                    "Accuse de reception NON ENVOYE pour le signalement {Id} (parent {ParentId}).",
                    signalement.Id, parent.Id);
            }

            // Alerte APRÈS l'écriture en base, comme pour le contact : une
            // panne de Telegram ne doit jamais faire perdre le signalement
            // lui-même, qui est déjà en sécurité dans la table.
            await _telegram.NotifierSignalementAsync(categorie, parent.Mail, description, elevePrenom);

            // Prévient tout onglet « Signalements » actuellement ouvert :
            // c'est lui qui remplace le sondage périodique par une vraie
            // mise à jour en direct.
            _evenementsAdmin.Publier("signalement");

            _logger.LogInformation(
                "Signalement {Id} recu ({Categorie}) de {Mail}.", signalement.Id, categorie, parent.Mail);

            return StatusCode(201, new { message = "Signalement transmis.", id = signalement.Id });
        }

        /// <summary>
        /// UNE CAPTURE D'ÉCRAN OU UN DOCUMENT, JOINT À SON PROPRE SIGNALEMENT — Camara, le
        /// 03/11/2026. Le parent ne peut rien relire ni supprimer : seule l'administration
        /// voit les pièces.
        /// </summary>
        [HttpPost("{id:int}/pieces")]
        [RequestSizeLimit(ReglesPieceSignalement.TailleRequeteMax)]
        [SwaggerResponse(204, "Pièce jointe enregistrée.")]
        [SwaggerResponse(400, "Fichier refusé.")]
        [SwaggerResponse(404, "Ce signalement n'existe pas sur ce compte.")]
        [SwaggerResponse(503, "Stockage indisponible.")]
        public async Task<IActionResult> AjouterPiece(int id, IFormFile? fichier, CancellationToken ct)
        {
            if (fichier is null) return BadRequest(new { message = "Aucun fichier reçu." });
            if (fichier.Length > ReglesPieceSignalement.TailleMax) return BadRequest(new { message = "Ce fichier dépasse 10 Mo." });

            var identifiant = _currentUser.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                throw new UnauthorizedAccessException("Le jeton ne contient pas de claim `sub`.");
            }

            var parent = await _parentService.GetOrCreateAsync(
                identifiant, _currentUser.Mail, _currentUser.Prenom, _currentUser.Nom,
                noterLaVenue: _currentUser.EleveId is null);

            // SEULEMENT SUR SON PROPRE SIGNALEMENT : même réponse s'il n'existe pas, pour
            // ne rien apprendre de ceux des autres.
            var signalement = await _signalements.GetByIdAsync(id, ct);
            if (signalement is null || signalement.ParentId != parent.Id) return NotFound();

            using var memoire = new MemoryStream();
            await fichier.CopyToAsync(memoire, ct);

            var ajout = await _signalements.AjouterPieceAsync(id, fichier.FileName, memoire.ToArray(), parent.Mail, ct);

            if (ajout.Introuvable) return NotFound();
            if (ajout.StockageIndisponible) return StatusCode(503, new { message = ajout.Refus });
            if (ajout.Piece is null) return BadRequest(new { message = ajout.Refus });

            _evenementsAdmin.Publier("signalement");
            return NoContent();
        }
    }
}
