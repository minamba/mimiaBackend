using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Api.Services.ScanMobile;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// LA PAGE DU TÉLÉPHONE, APRÈS LE QR CODE.
    ///
    /// OUVERTE À TOUS, ET C'EST TOUT L'OBJET : le téléphone de l'enfant n'est
    /// connecté à rien. La seule autorisation est le jeton du QR code — voir
    /// `JetonsScanMobile` : aléatoire, dix minutes, une seule photo, et il ne
    /// permet que de DÉPOSER un document dans UNE conversation. Rien ne se lit
    /// par ici, sinon le prénom du professeur et la matière.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("scan-mobile")]
    public class ScanMobileController : ControllerBase
    {
        private readonly IChatViewModelBuilder _chatBuilder;
        private readonly JetonsScanMobile _jetons;
        private readonly Workers.ReveilDocuments _reveilDocuments;
        private readonly ILogger<ScanMobileController> _logger;

        public ScanMobileController(
            IChatViewModelBuilder chatBuilder,
            JetonsScanMobile jetons,
            Workers.ReveilDocuments reveilDocuments,
            ILogger<ScanMobileController> logger)
        {
            _chatBuilder = chatBuilder ?? throw new ArgumentNullException(nameof(chatBuilder));
            _jetons = jetons ?? throw new ArgumentNullException(nameof(jetons));
            _reveilDocuments = reveilDocuments ?? throw new ArgumentNullException(nameof(reveilDocuments));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{jeton}")]
        [SwaggerResponse(200, "À qui la photo sera envoyée.", typeof(InfoScanMobile))]
        [SwaggerResponse(404, "QR code inconnu ou expiré.")]
        public async Task<IActionResult> Lire(
            string jeton, [FromServices] IReglageRepository reglages, CancellationToken ct)
        {
            var info = _jetons.Lire(jeton, DateTime.UtcNow);
            if (info is null) return NotFound();

            // LE STYLE DU SITE VOYAGE AVEC LA PAGE — Camara, le 17/09/2026 :
            // « un mode normal et un mode Blue Sky qui s'adaptera ». Le
            // téléphone arrive sans compte et sans rien en mémoire ; il ne
            // saurait donc pas que le site a changé d'allure. Le drapeau part
            // dans CETTE réponse plutôt que dans une seconde requête : la page
            // est encore vide quand il arrive, et la bascule ne se voit pas.
            var blueSky = false;

            try
            {
                blueSky = await reglages.EstActifAsync(ReglagesController.BlueSky, false, ct);
            }
            catch (Exception ex)
            {
                // Le style d'origine sur une panne de lecture, comme sur la
                // route publique : un réglage illisible ne doit pas empêcher
                // l'enfant d'envoyer sa copie.
                _logger.LogWarning(ex, "Lecture du style pour la page de scan impossible.");
            }

            return Ok(info with { BlueSky = blueSky });
        }

        [HttpPost("{jeton}")]
        [RequestSizeLimit(ValidationPieceJointe.TailleMax + 4096)]
        [SwaggerResponse(200, "Photo reçue.")]
        [SwaggerResponse(400, "Fichier refusé — le motif est destiné à l'élève.")]
        [SwaggerResponse(404, "QR code inconnu, expiré ou déjà utilisé.")]
        public async Task<IActionResult> Envoyer(string jeton, IFormFile fichier)
        {
            if (fichier is null || fichier.Length == 0)
            {
                return BadRequest(new { message = "Aucune photo reçue." });
            }

            if (fichier.Length > ValidationPieceJointe.TailleMax)
            {
                return BadRequest(new { message = "Cette photo est trop lourde. Essaie de la reprendre." });
            }

            // RÉSERVÉ AVANT DE LIRE LE FICHIER : deux envois simultanés avec le
            // même QR code ne passent pas tous les deux.
            if (!_jetons.Reserver(jeton, DateTime.UtcNow, out var conversationId))
            {
                return NotFound(new
                {
                    message = "Ce QR code a expiré, l'envoi est déjà terminé, ou tu as atteint "
                            + $"le maximum de {JetonsScanMobile.PiecesMax} photos.",
                });
            }

            try
            {
                using var flux = new MemoryStream();
                await fichier.CopyToAsync(flux, HttpContext.RequestAborted);

                var resultat = await _chatBuilder.AjouterPieceJointeParScanAsync(
                    conversationId, fichier.FileName, fichier.ContentType,
                    flux.ToArray(), HttpContext.RequestAborted);

                if (resultat.Piece is null)
                {
                    _jetons.Liberer(jeton);
                    return BadRequest(new { message = resultat.Motif });
                }

                _jetons.Livrer(jeton, resultat.Piece.Id);
                _reveilDocuments.Sonner();

                _logger.LogInformation(
                    "Photo recue par QR code sur la conversation {ConversationId} (piece {PieceId}).",
                    conversationId, resultat.Piece.Id);

                return Ok(new { recu = true });
            }
            catch (Exception ex)
            {
                _jetons.Liberer(jeton);
                _logger.LogError(ex, "Echec de l'envoi d'une photo par QR code sur {ConversationId}.", conversationId);
                return StatusCode(500, new { message = "La photo n'a pas pu être envoyée. Tu peux réessayer ?" });
            }
        }

        /// <summary>
        /// LE TÉLÉPHONE A FINI — Camara, le 16/09/2026 : « prendre plusieurs
        /// photos et les envoyer en une seule fois ». Les photos montent une
        /// par une ; c'est cet appel, et lui seul, qui dit à l'ordinateur que
        /// tout est là et qu'il peut expédier le message au professeur.
        /// </summary>
        [HttpPost("{jeton}/terminer")]
        [SwaggerResponse(200, "L'envoi est clos.")]
        [SwaggerResponse(404, "QR code inconnu ou expiré.")]
        public IActionResult Terminer(string jeton)
        {
            if (!_jetons.Terminer(jeton, DateTime.UtcNow))
            {
                return NotFound(new { message = "Ce QR code a expiré." });
            }

            return Ok(new { termine = true });
        }
    }
}
