using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Paiement;
using Stripe;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Ce que Stripe nous envoie.
    ///
    /// La route est ANONYME, et elle doit l'être : Stripe appelle depuis ses
    /// serveurs, sans jeton et sans session. Ce n'est pas un trou — la
    /// signature `Stripe-Signature` fait le travail d'authentification, avec un
    /// secret que seuls Stripe et nous connaissons.
    /// </summary>
    [ApiController]
    [Route("stripe")]
    public class StripeController : Controller
    {
        private readonly IEvenementsStripeService _evenements;
        private readonly ILogger<StripeController> _logger;

        public StripeController(
            IEvenementsStripeService evenements, ILogger<StripeController> logger)
        {
            _evenements = evenements ?? throw new ArgumentNullException(nameof(evenements));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Le point d'entrée des événements.
        ///
        /// CE QUE VEUT DIRE CHAQUE CODE DE RETOUR, POUR STRIPE
        /// -------------------------------------------------
        /// 200 : « reçu, n'y reviens pas. » 400 : « ce message ne vient pas de
        /// toi » — définitif, Stripe abandonne. 500 : « je n'ai pas pu, mais le
        /// message est bon » — Stripe RÉÉMET, avec un délai croissant, pendant
        /// trois jours.
        ///
        /// D'où la règle qui gouverne ce bloc : une base indisponible doit
        /// rendre 500, jamais 200. Un 200 avalé sur une panne de dix secondes
        /// perdrait définitivement un paiement — le parent est débité, et rien
        /// ne s'ouvre chez nous.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Événement traité.")]
        [SwaggerResponse(400, "Signature absente ou invalide.")]
        public async Task<IActionResult> Webhook(CancellationToken ct)
        {
            // Le corps est relu BRUT : la signature porte sur les octets
            // exacts. Le laisser désérialiser puis re-sérialiser changerait un
            // espace ou l'ordre d'une clé, et l'empreinte ne tomberait plus.
            using var lecteur = new StreamReader(Request.Body);
            var charge = await lecteur.ReadToEndAsync(ct);

            try
            {
                await _evenements.TraiterAsync(
                    charge, Request.Headers["Stripe-Signature"], ct);

                return Ok();
            }
            catch (StripeException ex)
            {
                // Signature invalide, ou horodatage trop ancien. Ce message
                // n'est pas de Stripe — ou plus assez frais pour qu'on le
                // croie. 400 : inutile de le réémettre.
                _logger.LogWarning(ex, "Webhook Stripe refuse : signature invalide.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec du traitement d'un webhook Stripe.");
                return StatusCode(500);
            }
        }
    }
}
