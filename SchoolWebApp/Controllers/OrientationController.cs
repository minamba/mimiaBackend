using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Orientation;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// LA CONSEILLÈRE D'ORIENTATION, CÔTÉ PARENT ET CÔTÉ ADMINISTRATION
    /// (11/10/2026) :
    ///   - le parent cherche l'établissement de son enfant dans l'Annuaire de
    ///     l'éducation, et l'enregistre sur son profil ;
    ///   - l'administrateur voit l'état de l'import de l'offre des lycées, et
    ///     peut le lancer sans attendre la tâche hebdomadaire.
    ///
    /// AUCUNE DE CES ROUTES N'EST OUVERTE À L'ENFANT : elles ne portent pas
    /// <c>[AutoriseEleve]</c>. C'est le parent qui décide de l'établissement.
    /// </summary>
    [ApiController]
    [Authorize]
    public class OrientationController : Controller
    {
        private readonly ILogger<OrientationController> _logger;

        public OrientationController(ILogger<OrientationController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Cherche un collège ou un lycée par son nom, sa ville ou son code postal.</summary>
        [HttpGet("orientation/etablissements")]
        [SwaggerResponse(200, "Les établissements trouvés (15 au plus).")]
        [SwaggerResponse(502, "L'Annuaire de l'éducation ne répond pas.")]
        public async Task<IActionResult> Chercher(
            [FromQuery] string? q,
            [FromServices] IAnnuaireEtablissementsService annuaire,
            CancellationToken ct)
        {
            try
            {
                return Ok(await annuaire.ChercherAsync(q ?? string.Empty, ct));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Annuaire de l'education injoignable.");
                return StatusCode(502, new { message = "L'annuaire officiel ne répond pas. Réessaie dans un instant." });
            }
        }

        /// <summary>
        /// Enregistre l'établissement de l'élève — ou l'efface, avec un code vide.
        ///
        /// LE CODE SEUL VIENT DU NAVIGATEUR : le nom, la commune et la position
        /// sont relus dans l'Annuaire par le serveur. Un parent ne peut pas
        /// placer son enfant ailleurs qu'à l'adresse réelle de l'établissement.
        /// </summary>
        [HttpPut("eleves/{id:int}/etablissement")]
        [SwaggerResponse(200, "Établissement enregistré.")]
        [SwaggerResponse(400, "Code inconnu de l'Annuaire.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> DefinirEtablissement(
            int id,
            [FromBody] EtablissementRequest requete,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IEleveRepository eleves,
            [FromServices] IAnnuaireEtablissementsService annuaire,
            [FromServices] IOrientationRepository offres,
            CancellationToken ct)
        {
            var eleve = await resolveur.ResoudreEleveAsync(id);
            if (eleve is null) return NotFound();

            if (string.IsNullOrWhiteSpace(requete.Uai))
            {
                await eleves.DefinirEtablissementAsync(id, null, null, null, null, null, ct);
                return Ok(new { etablissement = (object?)null });
            }

            EtablissementTrouve? trouve;
            try
            {
                trouve = await annuaire.GetParUaiAsync(requete.Uai, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Annuaire de l'education injoignable.");
                return StatusCode(502, new { message = "L'annuaire officiel ne répond pas. Réessaie dans un instant." });
            }

            if (trouve is null) return BadRequest(new { message = "Cet établissement est inconnu de l'annuaire officiel." });

            await eleves.DefinirEtablissementAsync(
                id, trouve.Uai, trouve.Nom, trouve.Commune, trouve.Latitude, trouve.Longitude, ct);

            // Ce que la conseillère saura : l'offre de ce lycée, s'il est connu,
            // et le nombre de lycées autour.
            var offre = await offres.GetParUaiAsync(trouve.Uai, ct);
            var proches = trouve.Latitude is { } lat && trouve.Longitude is { } lon
                ? (await offres.ProchesAsync(lat, lon, 30, 25, ct)).Count
                : 0;

            return Ok(new
            {
                etablissement = trouve,
                offreConnue = offre is not null,
                lyceesProches = proches,
            });
        }

        /// <summary>L'état de l'import de l'offre des lycées, pour Admin › Modes.</summary>
        [HttpGet("orientation/import")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "L'état de l'import.")]
        public async Task<IActionResult> EtatImport(
            [FromServices] IOrientationRepository offres,
            [FromServices] IImportOrientationService import,
            CancellationToken ct) =>
            Ok(new
            {
                enCours = import.EnCours,
                lycees = await offres.CompterAsync(ct),
                passages = await offres.DerniersPassagesAsync(5, ct),
            });

        /// <summary>
        /// Lance l'import maintenant, sans attendre la tâche de la semaine. Il
        /// tourne en arrière-plan : la réponse part tout de suite, l'écran relit
        /// l'état ensuite.
        /// </summary>
        [HttpPost("orientation/import")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(202, "Import lancé.")]
        [SwaggerResponse(409, "Un import est déjà en cours.")]
        public IActionResult LancerImport(
            [FromQuery] bool forcer,
            [FromServices] IServiceScopeFactory scopes,
            [FromServices] IImportOrientationService import)
        {
            if (import.EnCours) return Conflict(new { message = "Un import est déjà en cours." });

            _ = Task.Run(async () =>
            {
                using var scope = scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IImportOrientationService>();
                try
                {
                    await service.ImporterAsync(forcer, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Import de l'offre des lycees lance a la main : echec.");
                }
            });

            _logger.LogWarning("Import de l'offre des lycees lance par un administrateur (forcer : {Forcer}).", forcer);
            return Accepted();
        }
    }

    public class EtablissementRequest
    {
        /// <summary>Le code UAI de l'établissement ; vide pour l'effacer.</summary>
        public string? Uai { get; set; }
    }
}
