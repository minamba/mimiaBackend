using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.MesFiches;
using SchoolWebApp.Api.Services.Podcasts;
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
    /// LES PODCASTS DE L'ESPACE PARENT — Camara, le 15/11/2026 : « dans mon espace parent je veux aussi la fonctionnalité
    /// podcast ». Le chemin des podcasts de l'élève (`MesPodcastsController`), propriétaire changé (`Proprietaire.Parent`),
    /// comme les fiches de l'espace parent (`EspaceParentController`).
    ///
    /// DÉCISIONS DE CAMARA, LE MÊME JOUR
    /// - LA VOIX : ses catégories n'ont pas de professeur ; le parent choisit celui qui lit (`ProfesseursAsync`), gardé
    ///   sur le cours (`prof_avatar`).
    /// - LES RÈGLES DE SES FICHES : le même droit coché dans Admin › Parents ; un administrateur n'a ni quota ni
    ///   plafond ; les autres puisent dans les crédits de PODCASTS achetés par la famille, jamais dans le mois des enfants.
    ///
    /// PARENT SEULEMENT : aucune route n'a `AutoriseEleve`. Pas de scan au téléphone (comme ses fiches).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("parents/moi/mes-podcasts")]
    [PodcastsInclus]
    public class EspaceParentPodcastsController : ControllerBase
    {
        private const string Chemin = "parents/moi/mes-podcasts";

        public sealed record NouveauPodcastParentRequest(int? CategorieId, string? Prof);

        private readonly IFichesDeCoursRepository _depot;
        private readonly ImportCoursService _import;
        private readonly ICreditsFichesRepository _credits;
        private readonly IParentService _parents;
        private readonly ICurrentUserAccessor _utilisateur;
        private readonly IStockagePodcasts _stockage;
        private readonly ProgressionPodcasts _progression;
        private readonly ILogger<EspaceParentPodcastsController> _logger;

        public EspaceParentPodcastsController(
            IFichesDeCoursRepository depot, ImportCoursService import, ICreditsFichesRepository credits,
            IParentService parents, ICurrentUserAccessor utilisateur, IStockagePodcasts stockage,
            ProgressionPodcasts progression, ILogger<EspaceParentPodcastsController> logger)
        {
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _import = import ?? throw new ArgumentNullException(nameof(import));
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _parents = parents ?? throw new ArgumentNullException(nameof(parents));
            _utilisateur = utilisateur ?? throw new ArgumentNullException(nameof(utilisateur));
            _stockage = stockage ?? throw new ArgumentNullException(nameof(stockage));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // TOUTE LA PORTÉE NE VOIT QUE LES PODCASTS (même contexte pour le dépôt, les crédits et l'import).
            _depot.Nature = NaturesCours.Podcasts;
        }

        /// <summary>Le parent connecté, s'il a le droit à son espace ; null sinon (et pour une session d'enfant).</summary>
        private async Task<int?> ParentAutoriseAsync(CancellationToken ct)
        {
            var identifiant = _utilisateur.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identifiant) || _utilisateur.EleveId is not null) return null;

            var parent = await _parents.GetOrCreateAsync(
                identifiant, _utilisateur.Mail, _utilisateur.Prenom, _utilisateur.Nom, noterLaVenue: false);
            return (await _depot.DroitsEspaceParentAsync(parent.Id, ct)).Fiches ? parent.Id : null;
        }

        // ------------------------------------------------------------ la liste

        [HttpGet]
        [SwaggerResponse(200, "Ses podcasts (cours et épisodes), sa jauge, ses catégories.")]
        public async Task<IActionResult> Lister(CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();

            // PAS DE PODCASTS AVEC STARTER (15/11/2026) : rien à lister, la page dit pourquoi.
            if (await _import.PorteDuForfaitAsync(Proprietaire.Parent(parentId), UsageFiches.Consulter, ct) is not null)
            {
                return Ok(new { actif = await _import.ModuleActifAsync(ct), forfait = false, nonInclus = true, cours = Array.Empty<object>() });
            }

            var cours = await _depot.ListerAsync(Proprietaire.Parent(parentId), ct);
            return Ok(new
            {
                actif = await _import.ModuleActifAsync(ct),
                forfait = true,
                nonInclus = false,
                quota = await QuotaAsync(parentId, ct),
                categories = await _depot.ListerCategoriesAsync(parentId, ct),
                cours = cours.Select(c => MesPodcastsController.Projeter(Chemin, c, false)),
            });
        }

        [HttpGet("quota")]
        public async Task<IActionResult> Quota(CancellationToken ct) =>
            await ParentAutoriseAsync(ct) is int parentId ? Ok(await QuotaAsync(parentId, ct)) : NotFound();

        /// <summary>Ses crédits de podcasts (la famille) et ses plafonds ; `sansLimite` pour un administrateur.</summary>
        private async Task<object> QuotaAsync(int parentId, CancellationToken ct)
        {
            var proprietaire = Proprietaire.Parent(parentId);
            var q = await _import.QuotaDuProprietaireAsync(proprietaire, ct);
            var l = await _import.LimitesAsync(proprietaire, ct);
            var packs = (await _credits.GetOffresAsync(ct)).Select(p => new { p.Code, p.Libelle, p.Fiches, p.PrixCentimes }).ToList();

            return new
            {
                parent = true,
                sansLimite = l.SansLimite,
                q.Utilisees, q.Maximum, q.Restantes, q.Credits, q.Disponibles, q.RemiseLe, q.CreditsPris,
                packs,
                limites = new { l.SansLimite, l.PagesMax, l.PiecesMax, l.TailleFichierMax, l.NotionsMax },
            };
        }

        /// <summary>Les professeurs qu'il peut choisir pour lire son podcast : un par voix.</summary>
        [HttpGet("professeurs")]
        public async Task<IActionResult> Professeurs(CancellationToken ct) =>
            await ParentAutoriseAsync(ct) is null ? NotFound() : Ok(await _depot.ProfesseursAsync(ct));

        // ------------------------------------------------------------ le brouillon

        [HttpPost("cours")]
        [SwaggerResponse(200, "Le brouillon créé : on y dépose ensuite les pages, le texte ou le livre.")]
        [SwaggerResponse(400, "Catégorie ou voix refusée.")]
        public async Task<IActionResult> Creer([FromBody] NouveauPodcastParentRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les podcasts ne sont pas ouverts." });
            if (await _import.PorteDuForfaitAsync(Proprietaire.Parent(parentId), UsageFiches.NouveauCours, ct) is Refus fermee)
                return BadRequest(new { message = fermee.Message });
            if (await VerifierAsync(parentId, requete, ct) is string refus) return BadRequest(new { message = refus });

            var proprietaire = Proprietaire.Parent(parentId);
            var id =await _depot.CreerBrouillonAsync(proprietaire, null, PodcastsEleve.Type, DateTime.UtcNow, null, ct, requete.CategorieId);
            await _depot.DefinirProfAsync(proprietaire, id, requete.Prof, ct);
            return Ok(new { id });
        }

        [HttpPut("cours/{coursId:int}")]
        [SwaggerResponse(204, "Le brouillon porte la nouvelle catégorie et la nouvelle voix.")]
        public async Task<IActionResult> Modifier(int coursId, [FromBody] NouveauPodcastParentRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (await VerifierAsync(parentId, requete, ct) is string refus) return BadRequest(new { message = refus });

            var proprietaire = Proprietaire.Parent(parentId);
            var modifie = await _depot.ModifierBrouillonAsync(proprietaire, coursId, null, PodcastsEleve.Type, DateTime.UtcNow, ct, requete.CategorieId)
                && await _depot.DefinirProfAsync(proprietaire, coursId, requete.Prof, ct);
            return modifie ? NoContent() : BadRequest(new { message = "Ce podcast est déjà envoyé : il ne se modifie plus." });
        }

        /// <summary>Une catégorie à lui (ou aucune), et une voix qui existe.</summary>
        private async Task<string?> VerifierAsync(int parentId, NouveauPodcastParentRequest? requete, CancellationToken ct)
        {
            if (requete?.CategorieId is int id && !await _depot.CategorieDuParentAsync(parentId, id, ct)) return "Cette catégorie n’existe plus.";
            if (string.IsNullOrWhiteSpace(requete?.Prof)) return "Choisissez le professeur qui lira votre podcast.";

            var voix = await _depot.ProfesseursAsync(ct);
            return voix.Any(p => string.Equals(p.Avatar, requete.Prof, StringComparison.OrdinalIgnoreCase))
                ? null
                : "Ce professeur n’existe plus : choisissez-en un autre.";
        }

        [HttpGet("cours/{coursId:int}")]
        public async Task<IActionResult> Lire(int coursId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            var cours = await _depot.GetAsync(Proprietaire.Parent(parentId), coursId, ct);
            return cours is null ? NotFound() : Ok(MesPodcastsController.Projeter(Chemin, cours, true));
        }

        /// <summary>L'avancement d'un podcast : la barre qui suit le parent sur toutes les pages.</summary>
        [HttpGet("cours/{coursId:int}/avancement")]
        public async Task<IActionResult> Avancement(int coursId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            var cours = await _depot.GetAsync(Proprietaire.Parent(parentId), coursId, ct);
            return cours is null ? NotFound() : Ok(MesPodcastsController.ProjeterAvancement(cours, _progression));
        }

        [HttpDelete("cours/{coursId:int}")]
        public async Task<IActionResult> Supprimer(int coursId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (!await _depot.SupprimerCoursAsync(Proprietaire.Parent(parentId), coursId, ct)) return NotFound();

            // Son audio part avec lui. Les épisodes d'un parent sont rangés sous le dossier 0 (pas d'élève) : voir
            // `PodcastsWorker` (`cours.EleveId ?? 0`).
            _stockage.SupprimerCours(0, coursId);
            return NoContent();
        }

        // ------------------------------------------------------------ pages, texte, livre

        [HttpPost("cours/{coursId:int}/pages")]
        [RequestSizeLimit(Regles.TailleFichierMaxAdmin + 4096)]
        public async Task<IActionResult> AjouterPage(int coursId, IFormFile fichier, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (fichier is null || fichier.Length == 0) return BadRequest(new { message = "Aucun fichier reçu." });

            var proprietaire = Proprietaire.Parent(parentId);
            if (fichier.Length > (await _import.LimitesAsync(proprietaire, ct)).TailleFichierMax)
            {
                return BadRequest(new { message = "Ce fichier est trop lourd." });
            }

            using var flux = new MemoryStream();
            await fichier.CopyToAsync(flux, ct);

            var (page, refus) = await _import.AjouterPageAsync(proprietaire, coursId, fichier.FileName, fichier.ContentType, flux.ToArray(), ct);
            return page is null ? BadRequest(new { message = refus?.Message }) : Ok(page);
        }

        [HttpPut("cours/{coursId:int}/texte")]
        [RequestSizeLimit(Regles.TexteMax * 4 + 4096)]
        public async Task<IActionResult> EcrireTexte(int coursId, [FromBody] MesPodcastsController.TexteRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            var (page, refus) = await _import.EcrireTexteAsync(Proprietaire.Parent(parentId), coursId, requete?.Texte, ct);
            return page is null ? BadRequest(new { message = refus?.Message }) : Ok(page);
        }

        [HttpPut("cours/{coursId:int}/livre")]
        public async Task<IActionResult> EcrireLivre(int coursId, [FromBody] MesPodcastsController.LivreRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();

            var titre = (requete?.Titre ?? string.Empty).Trim();
            var auteur = (requete?.Auteur ?? string.Empty).Trim();
            if (MesPodcastsController.RefusLivre(titre, auteur, true) is string message) return BadRequest(new { message });

            var (page, refus) = await _import.EcrireTexteAsync(
                Proprietaire.Parent(parentId), coursId, PodcastsEleve.TexteLivre(titre, auteur), ct, livre: true);
            return page is null ? BadRequest(new { message = refus?.Message }) : Ok(page);
        }

        [HttpDelete("cours/{coursId:int}/pages/{pageId:int}")]
        public async Task<IActionResult> SupprimerPage(int coursId, int pageId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            return await _depot.SupprimerPageAsync(Proprietaire.Parent(parentId), coursId, pageId, ct) ? NoContent() : NotFound();
        }

        // ------------------------------------------------------------ envoi, épisodes, audio

        [HttpPost("cours/{coursId:int}/envoyer")]
        public async Task<IActionResult> Envoyer(int coursId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();

            var refus = await _import.EnvoyerAsync(Proprietaire.Parent(parentId), coursId, ct);
            if (refus is not null) return BadRequest(new { message = refus.Message });

            _logger.LogInformation("Podcast {CoursId} du parent {ParentId} envoye a la lecture.", coursId, parentId);
            return NoContent();
        }

        [HttpPost("cours/{coursId:int}/episodes")]
        public async Task<IActionResult> Demander(int coursId, [FromBody] MesPodcastsController.DemandeEpisodesRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            var refus = await _import.DemanderAsync(Proprietaire.Parent(parentId), coursId, PodcastsEleve.Type, requete.Notions, ct);
            return refus is null ? NoContent() : BadRequest(new { message = refus.Message });
        }

        [HttpDelete("episodes/{episodeId:int}")]
        public async Task<IActionResult> SupprimerEpisode(int episodeId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            return await _depot.SupprimerFicheAsync(Proprietaire.Parent(parentId), episodeId, ct) ? NoContent() : NotFound();
        }

        [HttpGet("episodes/{episodeId:int}/audio")]
        public async Task<IActionResult> Audio(int episodeId, [FromQuery] string? format, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            var episode = await _depot.GetFicheAsync(Proprietaire.Parent(parentId), episodeId, ct);
            return await MesPodcastsController.AudioAsync(this, episode, format, _stockage, ct);
        }
    }
}
