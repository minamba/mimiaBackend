using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// L'ONGLET « SESSIONS D'EXAMENS » — Camara, le 25/11/2026 : les dates officielles de chaque épreuve, ce que la
    /// veille propose, et les liens cliquables des sources « pour que je puisse valider ». Rien n'est visible des
    /// familles avant sa validation.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EstAdmin")]
    [Route("admin/sessions-examens")]
    public class SessionsExamensController : ControllerBase
    {
        private readonly ICalendrierExamensRepository _calendrier;
        private readonly ILogger<SessionsExamensController> _logger;

        public SessionsExamensController(ICalendrierExamensRepository calendrier, ILogger<SessionsExamensController> logger)
        {
            _calendrier = calendrier ?? throw new ArgumentNullException(nameof(calendrier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Chaque épreuve, sa date validée et la proposition en attente ; et les pages surveillées.</summary>
        [HttpGet]
        [SwaggerResponse(200, "{ epreuves, pages }.")]
        public async Task<IActionResult> Get([FromServices] IEcheanceReferentielRepository echeances, CancellationToken ct)
        {
            var epreuves = await _calendrier.GetEpreuvesAsync(ct);
            var dates = await _calendrier.GetToutesAsync(ct);

            var parCode = dates.GroupBy(d => d.EpreuveCode).ToDictionary(g => g.Key, g => g.ToList());

            var lignes = epreuves
                .Select(e =>
                {
                    parCode.TryGetValue(e.Code, out var siennes);
                    return new
                    {
                        e.Code,
                        e.Libelle,
                        e.ExamenCode,
                        e.ExamenLibelle,
                        e.Session,
                        validee = siennes?.FirstOrDefault(d => d.Etat == CalendrierExamens.Validee) is { } v ? DateVue.Depuis(v) : null,
                        proposee = siennes?.FirstOrDefault(d => d.Etat == CalendrierExamens.Proposee) is { } p ? DateVue.Depuis(p) : null,
                    };
                })
                .OrderBy(l => l.validee?.Date ?? DateTime.MaxValue)
                .ThenBy(l => l.ExamenLibelle).ThenBy(l => l.Libelle)
                .ToList();

            var pages = (await echeances.GetAVeillerAsync(ct))
                .Where(e => e.MatiereLibelle == CalendrierExamens.LibelleVeille)
                .Select(e => new
                {
                    e.Url,
                    libelle = e.NiveauxConcernes,
                    statut = e.DernierStatutVeille,
                    jamaisLue = e.DernierHashPage is null,
                    copieDu = e.CopieArchiveDu,
                })
                .ToList();

            return Ok(new { epreuves = lignes, pages });
        }

        [HttpPost("{id:int}/valider")]
        [SwaggerResponse(204, "Validée : les familles la voient.")]
        [SwaggerResponse(404, "Proposition introuvable.")]
        public async Task<IActionResult> Valider(int id, CancellationToken ct) =>
            await _calendrier.ValiderAsync(id, DateTime.UtcNow, ct) ? NoContent() : NotFound(new { message = "Cette proposition n'existe plus." });

        [HttpPost("{id:int}/rejeter")]
        [SwaggerResponse(204, "Écartée.")]
        [SwaggerResponse(404, "Proposition introuvable.")]
        public async Task<IActionResult> Rejeter(int id, CancellationToken ct) =>
            await _calendrier.RejeterAsync(id, ct) ? NoContent() : NotFound(new { message = "Cette proposition n'existe plus." });

        /// <summary>
        /// RELIRE MAINTENANT, sans attendre la nuit : chaque page du calendrier est relue, et ce qui diffère des dates
        /// validées devient une proposition. Coûte un appel au modèle par page lue (quelques centimes).
        /// </summary>
        [HttpPost("relire")]
        [SwaggerResponse(200, "{ pages: [{ url, lue, proposees, session }] }.")]
        public async Task<IActionResult> Relire(
            [FromServices] IEcheanceReferentielRepository echeances,
            [FromServices] IVeilleReferentielService veille,
            [FromServices] CalendrierExamensService service,
            CancellationToken ct)
        {
            var resultats = new List<object>();

            foreach (var page in (await echeances.GetAVeillerAsync(ct)).Where(e => e.MatiereLibelle == CalendrierExamens.LibelleVeille))
            {
                if (string.IsNullOrWhiteSpace(page.Url)) continue;

                try
                {
                    var releve = await veille.RelireAsync(page.Url, ct);
                    if (!releve.Reussi || string.IsNullOrWhiteSpace(releve.Texte))
                    {
                        resultats.Add(new { url = page.Url, lue = false, proposees = 0, session = (int?)null });
                        continue;
                    }

                    var lecture = await service.ProposerAsync(releve.Texte, page.Url, releve.CopieDu, ct);
                    resultats.Add(new { url = page.Url, lue = lecture.Lu, proposees = lecture.Proposees.Count, session = lecture.Session });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Sessions d'examens : relecture de {Url} impossible.", page.Url);
                    resultats.Add(new { url = page.Url, lue = false, proposees = 0, session = (int?)null });
                }
            }

            return Ok(new { pages = resultats });
        }

        /// <summary>Une date, telle que l'onglet l'affiche.</summary>
        public sealed record DateVue(
            int Id, string Nature, DateTime Date, DateTime? DateFin, string? HeureDebut, string? HeureFin,
            DateTime? RemplacementLe, string SourceLibelle, string SourceUrl, string? AnnexeUrl, string? Extrait,
            DateTime? ProposeLe, DateTime? ValideLe)
        {
            public static DateVue Depuis(DateEpreuveOfficielle d) => new(
                d.Id, d.Nature, d.DateEpreuve, d.DateFin, d.HeureDebut?.ToString(@"hh\:mm"), d.HeureFin?.ToString(@"hh\:mm"),
                d.RemplacementLe, d.SourceLibelle, d.SourceUrl, d.AnnexeUrl, d.Extrait, d.ProposeLe, d.ValideLe);
        }
    }
}
