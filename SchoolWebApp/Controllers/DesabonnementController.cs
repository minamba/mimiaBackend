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

        /// <summary>Le temps laissé pour revenir sur une désinscription des offres, depuis la page qui l'a faite.</summary>
        public static readonly TimeSpan DureeAnnulation = TimeSpan.FromMinutes(30);

        private const string ReabonnementEspaceParent =
            "Pour recevoir de nouveau nos offres, activez-les depuis votre espace parent : Mon compte › Paramètres.";

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
                // Lu par la page : pour les offres, « me réabonner » n'est proposé qu'à la session qui vient de se désinscrire.
                commerciale = CategorieDesabonnement.EstCommerciale(lu.Value.Categorie),
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

            await _envois.DesabonnerAsync(lu.Value.ParentId, lu.Value.Categorie, SourceConsentement.LienCourriel, ct);

            _logger.LogInformation(
                "Parent {Parent} desabonne de « {Categorie} ».", lu.Value.ParentId, lu.Value.Categorie);

            // Pour les offres, le droit de revenir sur ce clic, et lui seulement : un jeton d'annulation de courte durée,
            // remis à la page qui vient de désinscrire (voir `Reabonner`).
            var jetonAnnulation = CategorieDesabonnement.EstCommerciale(lu.Value.Categorie)
                ? _jetons.CreerAnnulation(lu.Value.ParentId, lu.Value.Categorie, DateTime.UtcNow, DureeAnnulation)
                : null;

            return Ok(new { desabonne = true, jetonAnnulation });
        }

        /// <summary>
        /// « Je me suis trompé » : le parent recevra de nouveau ces messages.
        ///
        /// POUR LES OFFRES, PAS AVEC LE SEUL LIEN DU COURRIEL (19/11/2026, retour d'Omar). Ce lien n'expire pas et peut
        /// avoir été transféré : quiconque l'a en main pourrait redonner un consentement à la place du parent. Se
        /// réabonner aux offres, c'est consentir : il faut soit l'espace parent connecté (interrupteur de Paramètres),
        /// soit le jeton d'annulation que la désinscription vient de remettre à cette page (valable 30 minutes). Les
        /// autres catégories (avis, relances, rappels) restent en opt-out : le lien suffit, comme avant.
        /// </summary>
        [HttpPost("reabonnement")]
        [SwaggerResponse(200, "Réabonné.")]
        [SwaggerResponse(400, "Lien invalide.")]
        [SwaggerResponse(403, "Offres : réabonnement possible seulement depuis l'espace parent ou juste après la désinscription.")]
        public async Task<IActionResult> Reabonner([FromBody] JetonRequest? requete, CancellationToken ct)
        {
            if (TropDAppels()) return StatusCode(429, new { message = "Trop de tentatives. Réessayez dans une heure." });

            var lu = _jetons.Lire(requete?.Jeton);
            if (lu is null) return BadRequest(new { message = LienInvalide });

            if (CategorieDesabonnement.EstCommerciale(lu.Value.Categorie)
                && !_jetons.AnnulationValide(requete?.JetonAnnulation, lu.Value.ParentId, lu.Value.Categorie, DateTime.UtcNow))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "reabonnement_espace_parent",
                    message = ReabonnementEspaceParent,
                });
            }

            await _envois.ReabonnerAsync(
                lu.Value.ParentId,
                lu.Value.Categorie,
                CategorieDesabonnement.EstCommerciale(lu.Value.Categorie)
                    ? SourceConsentement.AnnulationDesinscription
                    : SourceConsentement.LienCourriel,
                ct);

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

            await _envois.DesabonnerAsync(lu.Value.ParentId, lu.Value.Categorie, SourceConsentement.UnClic, ct);
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

            /// <summary>Offres seulement : le jeton remis par la désinscription qui vient d'avoir lieu (réabonnement).</summary>
            public string? JetonAnnulation { get; set; }
        }
    }
}
