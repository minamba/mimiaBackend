using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Auth;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Services.MesFiches;
using SchoolWebApp.Api.Services.Podcasts;
using SchoolWebApp.Api.Services.ScanMobile;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Api.Workers;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// MES PODCASTS — les podcasts audio que l'élève tire de son cours (Camara, le 15/11/2026). Le même chemin que
    /// « Mes fiches de révision » (`MesFichesController`), dans une portée qui ne voit QUE les podcasts : le constructeur
    /// pose la nature, et le dépôt, le quota, les crédits et l'import ne voient plus rien d'autre.
    ///
    /// L'ENFANT COMME LE PARENT : `AutoriseEleve` (l'enfant ne lit que lui), `ResoudreEleveAsync` (un parent ne lit que
    /// ses enfants).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("eleves/{eleveId:int}/mes-podcasts")]
    [PodcastsInclus]
    public class MesPodcastsController : ControllerBase
    {
        public sealed record NouveauPodcastRequest(int MatiereId);

        public sealed record DemandeEpisodesRequest(List<int>? Notions);

        public sealed record TexteRequest(string? Texte);

        public sealed record LivreRequest(string? Titre, string? Auteur);

        private readonly IFichesDeCoursRepository _depot;
        private readonly ImportCoursService _import;
        private readonly ICreditsFichesRepository _credits;
        private readonly IChatContexteResolver _resolveur;
        private readonly IStockagePodcasts _stockage;
        private readonly ProgressionPodcasts _progression;
        private readonly ILogger<MesPodcastsController> _logger;

        public MesPodcastsController(
            IFichesDeCoursRepository depot, ImportCoursService import, ICreditsFichesRepository credits,
            IChatContexteResolver resolveur, IStockagePodcasts stockage, ProgressionPodcasts progression,
            ILogger<MesPodcastsController> logger)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _import = import ?? throw new ArgumentNullException(nameof(import));
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _resolveur = resolveur ?? throw new ArgumentNullException(nameof(resolveur));
            _stockage = stockage ?? throw new ArgumentNullException(nameof(stockage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // TOUTE LA PORTÉE NE VOIT QUE LES PODCASTS (même contexte pour le dépôt, les crédits et l'import).
            _depot.Nature = NaturesCours.Podcasts;
        }

        [HttpGet]
        [AutoriseEleve]
        [SwaggerResponse(200, "Les podcasts de l'élève (cours et épisodes), du plus récent au plus ancien, et sa jauge.")]
        public async Task<IActionResult> Lister(
            int eleveId, [FromServices] Services.Paiement.AccesForfaitService acces, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            // PAS DE PODCASTS AVEC STARTER (15/11/2026) : rien à lister, la page dit pourquoi.
            if (!await acces.PodcastsPourEleveAsync(eleveId, ct))
            {
                return Ok(new { actif = await _import.ModuleActifAsync(ct), forfait = false, nonInclus = true, cours = Array.Empty<object>() });
            }

            var cours = await _depot.ListerAsync(eleveId, ct);
            return Ok(new
            {
                actif = await _import.ModuleActifAsync(ct),
                nonInclus = false,
                forfait = await _import.PorteDuForfaitAsync(eleveId, UsageFiches.Consulter, ct) is null,
                essai = await _import.EssaiFichesAsync(eleveId, ct),
                quota = await QuotaEtPacks(eleveId, ct),
                cours = cours.Select(c => Projeter(Chemin(eleveId), c, false)),
            });
        }

        [HttpGet("quota")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Podcasts du mois utilisés, maximum, restants, crédits de la famille, remise à zéro.")]
        public async Task<IActionResult> Quota(int eleveId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            return Ok(await QuotaEtPacks(eleveId, ct));
        }

        private async Task<object> QuotaEtPacks(int eleveId, CancellationToken ct)
        {
            var q = await _import.QuotaAsync(eleveId, ct);
            var packs = (await _credits.GetOffresAsync(ct)).Select(p => new { p.Code, p.Libelle, p.Fiches, p.PrixCentimes }).ToList();
            var l = await _import.LimitesAsync(eleveId, ct);
            var limites = new { l.SansLimite, l.PagesMax, l.PiecesMax, l.TailleFichierMax, l.NotionsMax };
            return new { q.Utilisees, q.Maximum, q.Restantes, q.Credits, q.Disponibles, q.RemiseLe, q.CreditsPris, packs, limites };
        }

        [HttpPost("cours")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Le brouillon créé : on y dépose ensuite les pages ou le texte.")]
        [SwaggerResponse(400, "Matière refusée — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> Creer(
            int eleveId, [FromBody] NouveauPodcastRequest requete, [FromServices] IEleveViewModelBuilder eleves, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les podcasts ne sont pas ouverts." });
            if (await _import.PorteDuForfaitAsync(eleveId, UsageFiches.NouveauCours, ct) is Refus fermee) return BadRequest(new { message = fermee.Message });

            var matiereId = await MatiereAsync(eleveId, requete.MatiereId, eleves);
            if (matiereId is null) return BadRequest(new { message = "Choisis une de tes matières." });

            var id = await _depot.CreerBrouillonAsync(eleveId, matiereId, PodcastsEleve.Type, DateTime.UtcNow, null, ct);
            return Ok(new { id });
        }

        [HttpPut("cours/{coursId:int}")]
        [AutoriseEleve]
        [SwaggerResponse(204, "Le brouillon porte la nouvelle matière.")]
        public async Task<IActionResult> Modifier(
            int eleveId, int coursId, [FromBody] NouveauPodcastRequest requete, [FromServices] IEleveViewModelBuilder eleves, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var matiereId = await MatiereAsync(eleveId, requete.MatiereId, eleves);
            if (matiereId is null) return BadRequest(new { message = "Choisis une de tes matières." });

            return await _depot.ModifierBrouillonAsync(eleveId, coursId, matiereId, PodcastsEleve.Type, DateTime.UtcNow, ct)
                ? NoContent()
                : BadRequest(new { message = "Ce cours est déjà envoyé : il ne se modifie plus." });
        }

        /// <summary>Une matière de sa classe, ouverte, et pas la conseillère : la même liste que sa grille.</summary>
        private static async Task<int?> MatiereAsync(int eleveId, int matiereId, IEleveViewModelBuilder eleves)
        {
            var matieres = await eleves.GetMatieresDeLEleveAsync(eleveId) ?? [];
            return matieres.FirstOrDefault(m => m.Id == matiereId && m.Active && !ConseillereOrientation.Est(m.Code))?.Id;
        }

        [HttpGet("cours/{coursId:int}")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Le cours, ses pages et ses épisodes avec leur texte.")]
        public async Task<IActionResult> Lire(int eleveId, int coursId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            var cours = await _depot.GetAsync(eleveId, coursId, ct);
            return cours is null ? NotFound() : Ok(Projeter(Chemin(eleveId), cours, true));
        }

        /// <summary>
        /// L'AVANCEMENT D'UN PODCAST — Camara, le 15/11/2026 : la barre qui suit l'élève sur toutes les pages et la
        /// fenêtre « ton podcast est prêt ». Une réponse courte, lue toutes les quelques secondes : ni pages ni texte.
        /// </summary>
        [HttpGet("cours/{coursId:int}/avancement")]
        [AutoriseEleve]
        [SwaggerResponse(200, "L'état du cours et son pourcentage (0 à 100).")]
        public async Task<IActionResult> Avancement(int eleveId, int coursId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            var c = await _depot.GetAsync(eleveId, coursId, ct);
            return c is null ? NotFound() : Ok(ProjeterAvancement(c, _progression));
        }

        /// <summary>L'avancement d'un cours (la barre de toutes les pages) : l'élève et le parent le lisent pareil.</summary>
        internal static object ProjeterAvancement(CoursDeLEleve c, ProgressionPodcasts progression)
        {
            var demandes = c.NotionsDemandees.Count;
            var faits = c.Fiches.Count;
            int? pourcent = c.Etat switch
            {
                FichesRevisionEleve.EtatPret => 100,
                FichesRevisionEleve.EtatAGenerer => progression.Pourcent(c.Id)
                    ?? (demandes > 0 ? Math.Min(99, faits * 100 / demandes) : 0),
                _ => null,
            };

            return new
            {
                c.Id,
                c.Etat,
                c.Titre,
                c.MatiereLibelle,
                c.MatiereCode,
                c.CategorieCouleur,
                c.Remarque,
                demandes,
                faits,
                pourcent,
            };
        }

        [HttpDelete("cours/{coursId:int}")]
        [AutoriseEleve]
        public async Task<IActionResult> Supprimer(int eleveId, int coursId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            if (!await _depot.SupprimerCoursAsync(eleveId, coursId, ct)) return NotFound();

            // Son audio part avec lui.
            _stockage.SupprimerCours(eleveId, coursId);
            return NoContent();
        }

        [HttpPost("cours/{coursId:int}/pages")]
        [AutoriseEleve]
        [RequestSizeLimit(Regles.TailleFichierMaxAdmin + 4096)]
        [SwaggerResponse(200, "La page reçue.")]
        [SwaggerResponse(400, "Fichier refusé — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> AjouterPage(int eleveId, int coursId, IFormFile fichier, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            if (fichier is null || fichier.Length == 0) return BadRequest(new { message = "Aucun fichier reçu." });
            if (fichier.Length > (await _import.LimitesAsync(eleveId, ct)).TailleFichierMax)
            {
                return BadRequest(new { message = "Ce fichier est trop lourd. Essaie une photo un peu moins grande." });
            }

            using var flux = new MemoryStream();
            await fichier.CopyToAsync(flux, ct);

            var (page, refus) = await _import.AjouterPageAsync(eleveId, coursId, fichier.FileName, fichier.ContentType, flux.ToArray(), ct);
            return page is null ? BadRequest(new { message = refus?.Message }) : Ok(page);
        }

        [HttpPut("cours/{coursId:int}/texte")]
        [AutoriseEleve]
        [RequestSizeLimit(Regles.TexteMax * 4 + 4096)]
        [SwaggerResponse(200, "Le texte reçu, comme une page.")]
        public async Task<IActionResult> EcrireTexte(int eleveId, int coursId, [FromBody] TexteRequest requete, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            var (page, refus) = await _import.EcrireTexteAsync(eleveId, coursId, requete?.Texte, ct);
            return page is null ? BadRequest(new { message = refus?.Message }) : Ok(page);
        }

        /// <summary>
        /// LE RÉSUMÉ DE LIVRE — Camara, le 15/11/2026 : le titre du livre (écrit ou dicté) et, s'il le connaît, son
        /// auteur. Rangé comme le texte d'un podcast libre, marqué « livre ».
        /// </summary>
        [HttpPut("cours/{coursId:int}/livre")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Le livre reçu, comme une page.")]
        public async Task<IActionResult> EcrireLivre(int eleveId, int coursId, [FromBody] LivreRequest requete, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var titre = (requete?.Titre ?? string.Empty).Trim();
            var auteur = (requete?.Auteur ?? string.Empty).Trim();
            if (RefusLivre(titre, auteur, false) is string message) return BadRequest(new { message });

            var (page, refus) = await _import.EcrireTexteAsync(
                eleveId, coursId, PodcastsEleve.TexteLivre(titre, auteur), ct, livre: true);
            return page is null ? BadRequest(new { message = refus?.Message }) : Ok(page);
        }

        /// <summary>Le titre (deux à deux cents caractères) et l'auteur facultatif d'un résumé de livre ; null s'ils vont.</summary>
        internal static string? RefusLivre(string titre, string auteur, bool vous)
        {
            if (titre.Length < 2) return vous ? "Écrivez ou dictez le titre du livre." : "Écris ou dicte le titre du livre.";
            if (titre.Length > PodcastsEleve.TitreLivreMax)
            {
                return $"Le titre fait {titre.Length} caractères : c’est {PodcastsEleve.TitreLivreMax} au maximum.";
            }

            return auteur.Length > PodcastsEleve.AuteurLivreMax
                ? $"Le nom de l’auteur est trop long : {PodcastsEleve.AuteurLivreMax} caractères au maximum."
                : null;
        }

        [HttpDelete("cours/{coursId:int}/pages/{pageId:int}")]
        [AutoriseEleve]
        public async Task<IActionResult> SupprimerPage(int eleveId, int coursId, int pageId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            return await _depot.SupprimerPageAsync(eleveId, coursId, pageId, ct) ? NoContent() : NotFound();
        }

        [HttpPost("cours/{coursId:int}/scan-mobile")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Le jeton du QR code et son expiration.")]
        public async Task<IActionResult> CreerScan(int eleveId, int coursId, [FromServices] JetonsScanMobile jetons, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var cours = await _depot.GetAsync(eleveId, coursId, ct);
            if (cours is null || cours.Etat != Regles.EtatBrouillon) return NotFound();

            var (jeton, expireLe) = jetons.CreerPourCoursImporte(
                eleveId, coursId, $"{cours.MatiereLibelle} — ton podcast", DateTime.UtcNow, NaturesCours.Podcasts);
            return Ok(new { jeton, expireLe });
        }

        [HttpGet("cours/{coursId:int}/scan-mobile/{jeton}")]
        [AutoriseEleve]
        [SwaggerResponse(200, "attente, recu (avec les pages) ou expire.")]
        public async Task<IActionResult> EtatScan(int eleveId, int coursId, string jeton, [FromServices] JetonsScanMobile jetons, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var cours = await _depot.GetAsync(eleveId, coursId, ct);
            if (cours is null) return NotFound();

            var (etat, ids, termine) = jetons.EtatCoursImporte(jeton, coursId, DateTime.UtcNow);
            var recues = cours.Pages.Where(p => ids.Contains(p.Id)).ToList();

            return Ok(new
            {
                etat = etat switch
                {
                    EtatJetonScan.Recu when recues.Count > 0 => "recu",
                    EtatJetonScan.Attente => "attente",
                    _ => "expire",
                },
                pieces = recues,
                termine,
            });
        }

        [HttpPost("cours/{coursId:int}/envoyer")]
        [AutoriseEleve]
        [SwaggerResponse(204, "Le cours part à la lecture.")]
        [SwaggerResponse(400, "Refusé — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> Envoyer(int eleveId, int coursId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var refus = await _import.EnvoyerAsync(eleveId, coursId, ct);
            if (refus is not null) return BadRequest(new { message = refus.Message });

            _logger.LogInformation("Cours de podcast {CoursId} envoye a la lecture (eleve {EleveId}).", coursId, eleveId);
            return NoContent();
        }

        [HttpPost("cours/{coursId:int}/episodes")]
        [AutoriseEleve]
        [SwaggerResponse(204, "Les épisodes sont demandés.")]
        [SwaggerResponse(400, "Refusé — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> Demander(int eleveId, int coursId, [FromBody] DemandeEpisodesRequest requete, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            var refus = await _import.DemanderAsync(eleveId, coursId, PodcastsEleve.Type, requete.Notions, ct);
            return refus is null ? NoContent() : BadRequest(new { message = refus.Message });
        }

        [HttpDelete("episodes/{episodeId:int}")]
        [AutoriseEleve]
        public async Task<IActionResult> SupprimerEpisode(int eleveId, int episodeId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            return await _depot.SupprimerFicheAsync(eleveId, episodeId, ct) ? NoContent() : NotFound();
        }

        /// <summary>
        /// L'AUDIO D'UN ÉPISODE. En Ogg Opus, avec la lecture par morceaux (le lecteur du navigateur avance et recule
        /// sans tout recharger). `?format=wav` pour un navigateur qui ne lit pas l'Opus (iPhone avant iOS 18.4) : refait
        /// à la volée, plus lourd.
        /// </summary>
        [HttpGet("episodes/{episodeId:int}/audio")]
        [AutoriseEleve]
        [SwaggerResponse(200, "L'audio de l'épisode.")]
        [SwaggerResponse(404, "Épisode inconnu, ou audio plus disponible.")]
        public async Task<IActionResult> Audio(int eleveId, int episodeId, [FromQuery] string? format, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var episode = await _depot.GetFicheAsync(eleveId, episodeId, ct);
            return await AudioAsync(this, episode, format, _stockage, ct);
        }

        /// <summary>L'audio d'un épisode, pour l'élève comme pour le parent.</summary>
        internal static async Task<IActionResult> AudioAsync(
            ControllerBase moi, FicheDuCours? episode, string? format, IStockagePodcasts stockage, CancellationToken ct)
        {
            if (episode?.Contenu is null || LireContenu(episode.Contenu) is not { } contenu) return moi.NotFound();

            // Privé (la voix de son professeur, pour cet enfant) ; le contenu ne change jamais.
            moi.Response.Headers.CacheControl = "private, max-age=31536000, immutable";

            if (string.Equals(format, "wav", StringComparison.OrdinalIgnoreCase))
            {
                var opus = await stockage.LireAsync(contenu.Audio, ct);
                return opus is null ? moi.NotFound() : moi.File(AudioPodcast.EnWav(opus), "audio/wav", enableRangeProcessing: true);
            }

            var flux = stockage.Ouvrir(contenu.Audio);
            return flux is null ? moi.NotFound() : moi.File(flux, PodcastsEleve.TypeAudio, enableRangeProcessing: true);
        }

        private static string Chemin(int eleveId) => $"eleves/{eleveId}/mes-podcasts";

        private static ContenuEpisode? LireContenu(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ContenuEpisode>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>Le cours tel que l'écran le lit : ses épisodes, avec leur texte seulement sur la page du cours.</summary>
        internal static object Projeter(string chemin, CoursDeLEleve c, bool avecTexte) => new
        {
            c.Id,
            c.MatiereId,
            c.MatiereLibelle,
            c.MatiereCode,
            // L'ESPACE PARENT (15/11/2026) : ses catégories à la place des matières.
            c.CategorieId,
            c.CategorieCouleur,
            c.Titre,
            c.Etat,
            c.Remarque,
            notions = c.Notions,
            c.NotionsDemandees,
            c.DemandeLe,
            c.PagesDisponibles,
            c.Libre,
            c.DateCreation,
            pages = c.Pages,
            episodes = c.Fiches.Select(f =>
            {
                var contenu = f.Contenu is null ? null : LireContenu(f.Contenu);
                return new
                {
                    f.Id,
                    f.Titre,
                    f.NotionRang,
                    f.NotionTotal,
                    f.DateCreation,
                    duree = contenu?.Duree,
                    voix = contenu?.Voix,
                    prof = contenu?.Prof,
                    audio = $"{chemin}/episodes/{f.Id}/audio",
                    paragraphes = avecTexte ? contenu?.Paragraphes : null,
                    // Le texte qui suit la voix (15/11/2026) : null pour un épisode fait avant, l'écran estime.
                    reperes = avecTexte ? contenu?.Reperes : null,
                };
            }),
        };
    }
}
