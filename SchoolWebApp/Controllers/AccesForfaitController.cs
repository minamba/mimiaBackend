using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Auth;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// CE QU'UN ENFANT PEUT OUVRIR, SELON LE FORFAIT DE SA FAMILLE — Camara, le
    /// 16/10/2026 : les fiches de révision et les jeux demandent un vrai
    /// forfait (ni essai, ni pause, ni plus d'abonnement). Voir
    /// `AccesForfaitService`.
    ///
    /// LISIBLE PAR L'ENFANT (`AutoriseEleve`) : c'est sa page qui grise la carte
    /// des fiches et les jeux, et sa session n'a pas accès aux routes du
    /// forfait de ses parents.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("eleves/{eleveId:int}/acces")]
    public class AccesForfaitController : ControllerBase
    {
        private readonly AccesForfaitService _acces;
        private readonly IChatContexteResolver _resolveur;

        public AccesForfaitController(AccesForfaitService acces, IChatContexteResolver resolveur)
        {
            _acces = acces ?? throw new ArgumentNullException(nameof(acces));
            _resolveur = resolveur ?? throw new ArgumentNullException(nameof(resolveur));
        }

        [HttpGet]
        [AutoriseEleve]
        [SwaggerResponse(200, "{ ouvert, motif, essaiFiches } — motif : essai, sans-forfait ou pause ; essaiFiches : libre, en-route, utilise (essai seulement).")]
        public async Task<IActionResult> Lire(
            int eleveId, [FromServices] IFichesDeCoursRepository fiches, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var acces = await _acces.LirePourEleveAsync(eleveId, ct);

            // L'ESSAI (18/10/2026) : un cours de fiches par famille — la carte
            // des fiches reste ouverte, la page de création sait s'il reste.
            var essaiFiches = !acces.Ouvert && acces.Motif == AccesForfaitService.MotifEssai
                ? await fiches.EssaiFichesAsync(eleveId, ct)
                : null;

            return Ok(new { ouvert = acces.Ouvert, motif = acces.Motif, essaiFiches });
        }
    }
}
