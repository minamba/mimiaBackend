using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Middleware;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// « Ne plus recevoir ces messages », depuis un lien de courriel.
    ///
    /// ANONYME, ET C'EST TOUT LE POINT. Le lien arrive dans une messagerie ;
    /// exiger une connexion avant de désabonner garantirait le clic sur
    /// « Signaler comme spam » à la place. Le jeton signé suffit à dire qui
    /// est concerné — personne ne peut en fabriquer un pour un autre parent.
    ///
    /// LA LECTURE NE DÉSABONNE PAS. Les antivirus de messagerie ouvrent les
    /// liens d'un courriel avant le destinataire : un désabonnement sur simple
    /// ouverture désabonnerait des parents qui n'ont jamais cliqué. La page du
    /// site lit (GET), puis c'est le bouton qui désabonne (POST).
    /// </summary>
    [ApiController]
    // HORS DE LA SALLE D'ATTENTE : le lien de desabonnement d'un courriel
    // doit fonctionner toujours, et c'est la loi autant que la correction.
    // Mettre en file d'attente quelqu'un qui demande a ne plus etre contacte
    // serait la plus mauvaise reponse possible a cette demande-la.
    [HorsSalleDAttente]
    [Route("desabonnement")]
    [AllowAnonymous]
    public class DesabonnementController : ControllerBase
    {
        // Une route publique qui écrit en base se protège d'un robot qui la
        // bombarderait. Trente appels par heure suffisent à un humain qui se
        // trompe, se réabonne et se ravise.
        private static readonly ConcurrentDictionary<string, List<DateTime>> Appels = new();
        private const int MaxParHeure = 30;

        private const string LienInvalide =
            "Ce lien n’est pas valide. S’il a été coupé en le copiant, ouvrez-le directement depuis le courriel.";

        private readonly IJetonDesabonnement _jetons;
        private readonly IEnvoiAutomatiqueRepository _envois;
        private readonly ILogger<DesabonnementController> _logger;

        public DesabonnementController(
            IJetonDesabonnement jetons,
            IEnvoiAutomatiqueRepository envois,
            ILogger<DesabonnementController> logger)
        {
            _jetons = jetons ?? throw new ArgumentNullException(nameof(jetons));
            _envois = envois ?? throw new ArgumentNullException(nameof(envois));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Ce que désigne le lien, et si c'est déjà fait. N'écrit rien.</summary>
        [HttpGet]
        [SwaggerResponse(200, "La catégorie et l'état.")]
        [SwaggerResponse(400, "Lien invalide.")]
        [SwaggerResponse(429, "Trop d'appels.")]
        public async Task<IActionResult> Lire([FromQuery] string? jeton, CancellationToken ct)
        {
            if (TropDAppels()) return StatusCode(429, new { message = "Trop de tentatives. Réessayez dans une heure." });

            var lu = _jetons.Lire(jeton);
            if (lu is null) return BadRequest(new { message = LienInvalide });

            var desabonne = await _envois.EstDesabonneAsync(lu.Value.ParentId, lu.Value.Categorie, ct);

            return Ok(new
            {
                categorie = lu.Value.Categorie,
                libelle = CategorieDesabonnement.Libelle(lu.Value.Categorie),
                desabonne,
            });
        }

        [HttpPost]
        [SwaggerResponse(200, "Désabonné.")]
        [SwaggerResponse(400, "Lien invalide.")]
        public async Task<IActionResult> Desabonner([FromBody] JetonRequest? requete, CancellationToken ct)
        {
            if (TropDAppels()) return StatusCode(429, new { message = "Trop de tentatives. Réessayez dans une heure." });

            var lu = _jetons.Lire(requete?.Jeton);
            if (lu is null) return BadRequest(new { message = LienInvalide });

            await _envois.DesabonnerAsync(lu.Value.ParentId, lu.Value.Categorie, ct);

            _logger.LogInformation(
                "Parent {Parent} desabonne de « {Categorie} ».", lu.Value.ParentId, lu.Value.Categorie);

            return Ok(new { desabonne = true });
        }

        /// <summary>« Je me suis trompé » : le parent recevra de nouveau ces messages.</summary>
        [HttpPost("reabonnement")]
        [SwaggerResponse(200, "Réabonné.")]
        [SwaggerResponse(400, "Lien invalide.")]
        public async Task<IActionResult> Reabonner([FromBody] JetonRequest? requete, CancellationToken ct)
        {
            if (TropDAppels()) return StatusCode(429, new { message = "Trop de tentatives. Réessayez dans une heure." });

            var lu = _jetons.Lire(requete?.Jeton);
            if (lu is null) return BadRequest(new { message = LienInvalide });

            await _envois.ReabonnerAsync(lu.Value.ParentId, lu.Value.Categorie, ct);

            _logger.LogInformation(
                "Parent {Parent} reabonne a « {Categorie} ».", lu.Value.ParentId, lu.Value.Categorie);

            return Ok(new { desabonne = false });
        }

        /// <summary>
        /// Le désabonnement en un clic des messageries (RFC 8058) : Gmail envoie
        /// directement un POST ici quand le parent clique « Se désabonner » en
        /// haut du message.
        /// </summary>
        [HttpPost("un-clic")]
        [SwaggerResponse(200, "Désabonné.")]
        [SwaggerResponse(400, "Lien invalide.")]
        public async Task<IActionResult> UnClic([FromQuery] string? jeton, CancellationToken ct)
        {
            if (TropDAppels()) return StatusCode(429);

            var lu = _jetons.Lire(jeton);
            if (lu is null) return BadRequest();

            await _envois.DesabonnerAsync(lu.Value.ParentId, lu.Value.Categorie, ct);
            return Ok();
        }

        private bool TropDAppels()
        {
            var depuis = DateTime.UtcNow.AddHours(-1);
            var cle = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnue";
            var recents = Appels.GetOrAdd(cle, _ => new List<DateTime>());

            lock (recents)
            {
                recents.RemoveAll(d => d < depuis);
                if (recents.Count >= MaxParHeure) return true;

                recents.Add(DateTime.UtcNow);
                return false;
            }
        }

        public class JetonRequest
        {
            public string? Jeton { get; set; }
        }
    }
}
