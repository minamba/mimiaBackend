using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Auth;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Services.MesFiches;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Api.Services.ScanMobile;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// MES FICHES DE RÉVISION — les fiches que l'élève tire de son propre cours
    /// (14/10/2026). Un espace à part : rien ici ne touche aux fiches des
    /// professeurs (`eleves/{id}/fiches`), ni au suivi des compétences.
    ///
    /// L'ENFANT COMME LE PARENT : chaque route accepte la session de l'enfant
    /// (`AutoriseEleve` — le filtre global vérifie que `eleveId` est le sien),
    /// et `ResoudreEleveAsync` vérifie qu'un parent ne lit que ses enfants.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("eleves/{eleveId:int}/mes-fiches")]
    public class MesFichesController : ControllerBase
    {
        /// <summary>`ComplementDe` : le cours que ce brouillon complète (16/10/2026).</summary>
        public sealed record NouveauCoursRequest(int MatiereId, string Type, int? ComplementDe = null);

        public sealed record DemandeFichesRequest(string Type, List<int>? Notions);

        private readonly IFichesDeCoursRepository _depot;
        private readonly ImportCoursService _import;
        private readonly ICreditsFichesRepository _credits;
        private readonly IChatContexteResolver _resolveur;
        private readonly ILogger<MesFichesController> _logger;

        public MesFichesController(
            IFichesDeCoursRepository depot, ImportCoursService import, ICreditsFichesRepository credits,
            IChatContexteResolver resolveur, ILogger<MesFichesController> logger)
        {
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _import = import ?? throw new ArgumentNullException(nameof(import));
            _resolveur = resolveur ?? throw new ArgumentNullException(nameof(resolveur));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [AutoriseEleve]
        [SwaggerResponse(200, "Les cours envoyés et leurs fiches, du plus récent au plus ancien.")]
        public async Task<IActionResult> Lister(int eleveId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var cours = await _depot.ListerAsync(eleveId, ct);
            return Ok(new
            {
                actif = await _import.ModuleActifAsync(ct),
                // Sans vrai forfait, la page dit pourquoi au lieu de proposer de créer.
                forfait = await _import.PorteDuForfaitAsync(eleveId, ct) is null,
                quota = await QuotaEtPacks(eleveId, ct),
                cours = cours.Select(c => Projeter(c, false)),
            });
        }

        /// <summary>
        /// La jauge et, avec elle, les packs que le parent peut acheter (sans
        /// les identifiants Stripe) : la jauge qui dit « plus rien » propose
        /// tout de suite d'en ajouter.
        /// </summary>
        private async Task<object> QuotaEtPacks(int eleveId, CancellationToken ct)
        {
            var q = await _import.QuotaAsync(eleveId, ct);
            var packs = (await _credits.GetOffresAsync(ct))
                .Select(p => new { p.Code, p.Libelle, p.Fiches, p.PrixCentimes })
                .ToList();

            // LES PLAFONDS DE L'IMPORT voyagent avec la jauge : la page de création
            // les applique (une famille administratrice n'a pas ceux des autres).
            var l = await _import.LimitesAsync(eleveId, ct);
            var limites = new { l.SansLimite, l.PagesMax, l.PiecesMax, l.TailleFichierMax, l.NotionsMax };

            return new { q.Utilisees, q.Maximum, q.Restantes, q.Credits, q.Disponibles, q.RemiseLe, q.CreditsPris, packs, limites };
        }

        /// <summary>La jauge du mois, seule : la page de création n'a pas besoin des cours.</summary>
        [HttpGet("quota")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Fiches du mois utilisées, maximum, restantes, crédits de la famille, remise à zéro.")]
        public async Task<IActionResult> Quota(int eleveId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            return Ok(await QuotaEtPacks(eleveId, ct));
        }

        [HttpPost("cours")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Le brouillon créé : on y dépose ensuite les pages.")]
        [SwaggerResponse(400, "Matière ou type refusé — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> Creer(
            int eleveId, [FromBody] NouveauCoursRequest requete,
            [FromServices] IEleveViewModelBuilder eleves, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les fiches de révision ne sont pas ouvertes." });

            if (await _import.PorteDuForfaitAsync(eleveId, ct) is Refus fermee) return BadRequest(new { message = fermee.Message });

            // COMPLÉTER UN COURS (16/10/2026) : un cours de l'enfant, prêt, qui
            // n'est pas lui-même un complément ; sa matière s'impose.
            if (requete.ComplementDe is { } completeId)
            {
                var complete = await _depot.GetAsync(eleveId, completeId, ct);
                if (complete is null || complete.ComplementDe is not null || complete.Etat != Regles.EtatPret)
                {
                    return BadRequest(new { message = "Ce cours ne peut pas être complété." });
                }

                if (complete.Complement is { } enRoute && enRoute.Etat != Regles.EtatEchec)
                {
                    return BadRequest(new { message = "Un complément de ce cours est déjà en préparation. Attends qu’il soit prêt." });
                }

                requete = requete with { MatiereId = complete.MatiereId ?? 0 };
            }

            var (matiereId, type, refus) = await VerifierChoixAsync(eleveId, requete, eleves, ct);
            if (refus is not null) return BadRequest(new { message = refus });

            var id = await _depot.CreerBrouillonAsync(eleveId, matiereId, type!, DateTime.UtcNow, requete.ComplementDe, ct);
            return Ok(new { id });
        }

        /// <summary>
        /// CHANGER DE MATIÈRE OU DE TYPE APRÈS AVOIR IMPORTÉ — Camara, le
        /// 16/10/2026 : les pages déjà déposées restent, seuls les deux choix
        /// changent. Tant que le cours est un brouillon : une fois parti à la
        /// lecture, il est trop tard.
        /// </summary>
        [HttpPut("cours/{coursId:int}")]
        [AutoriseEleve]
        [SwaggerResponse(204, "Le brouillon porte la nouvelle matière et le nouveau type.")]
        [SwaggerResponse(400, "Matière ou type refusé, ou cours déjà envoyé.")]
        public async Task<IActionResult> Modifier(
            int eleveId, int coursId, [FromBody] NouveauCoursRequest requete,
            [FromServices] IEleveViewModelBuilder eleves, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les fiches de révision ne sont pas ouvertes." });

            var (matiereId, type, refus) = await VerifierChoixAsync(eleveId, requete, eleves, ct);
            if (refus is not null) return BadRequest(new { message = refus });

            return await _depot.ModifierBrouillonAsync(eleveId, coursId, matiereId, type!, DateTime.UtcNow, ct)
                ? NoContent()
                : BadRequest(new { message = "Ce cours est déjà envoyé : il ne se modifie plus." });
        }

        /// <summary>
        /// UNE MATIÈRE DE SA CLASSE, OUVERTE, et pas la conseillère — la même
        /// liste que sa grille de matières —, et un type proposé dans sa classe.
        /// Une seule règle pour créer et pour modifier un brouillon.
        /// </summary>
        private async Task<(int MatiereId, string? Type, string? Refus)> VerifierChoixAsync(
            int eleveId, NouveauCoursRequest requete, IEleveViewModelBuilder eleves, CancellationToken ct)
        {
            var matieres = await eleves.GetMatieresDeLEleveAsync(eleveId) ?? [];
            var matiere = matieres.FirstOrDefault(m => m.Id == requete.MatiereId && m.Active && !ConseillereOrientation.Est(m.Code));
            if (matiere is null) return (0, null, "Choisis une de tes matières.");

            var type = Regles.Type(requete.Type);
            var rang = await _depot.RangDeLEleveAsync(eleveId, ct);
            if (type is null || (rang > 0 && rang < type.RangMin))
            {
                return (0, null, "Ce type de fiche n’est pas proposé dans ta classe.");
            }

            return (matiere.Id, type.Code, null);
        }

        [HttpGet("cours/{coursId:int}")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Le cours, ses pages et ses fiches avec leur contenu.")]
        public async Task<IActionResult> Lire(int eleveId, int coursId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var cours = await _depot.GetAsync(eleveId, coursId, ct);
            return cours is null ? NotFound() : Ok(Projeter(cours, true));
        }

        [HttpDelete("cours/{coursId:int}")]
        [AutoriseEleve]
        public async Task<IActionResult> Supprimer(int eleveId, int coursId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            return await _depot.SupprimerCoursAsync(eleveId, coursId, ct) ? NoContent() : NotFound();
        }

        [HttpPost("cours/{coursId:int}/pages")]
        [AutoriseEleve]
        // Le plafond des familles administratrices ; celui des autres est
        // vérifié juste après, et par `ImportCoursService`.
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
                eleveId, coursId, $"{cours.MatiereLibelle} — ton cours", DateTime.UtcNow);
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

            _logger.LogInformation("Cours {CoursId} envoye a la lecture (eleve {EleveId}).", coursId, eleveId);
            return NoContent();
        }

        [HttpPost("cours/{coursId:int}/fiches")]
        [AutoriseEleve]
        [SwaggerResponse(204, "Les fiches sont demandées.")]
        [SwaggerResponse(400, "Refusé — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> Demander(int eleveId, int coursId, [FromBody] DemandeFichesRequest requete, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();

            var type = Regles.Type(requete.Type);
            var rang = await _depot.RangDeLEleveAsync(eleveId, ct);
            if (type is null || (rang > 0 && rang < type.RangMin))
            {
                return BadRequest(new { message = "Ce type de fiche n’est pas proposé dans ta classe." });
            }

            var refus = await _import.DemanderAsync(eleveId, coursId, type.Code, requete.Notions, ct);
            return refus is null ? NoContent() : BadRequest(new { message = refus.Message });
        }

        /// <summary>
        /// FLASH CARDS ET QUIZ D'UNE FICHE (16/10/2026) : écrits au premier appel
        /// (≈ 1,4 centime), gardés ensuite. Hors quota ; un vrai forfait exigé.
        /// </summary>
        [HttpPost("fiches/{ficheId:int}/entrainement")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Les questions : réponse, trois leurres, explication.")]
        [SwaggerResponse(400, "Refusé — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> Entrainement(
            int eleveId, int ficheId, [FromServices] EntrainementFicheService entrainement, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les fiches de révision ne sont pas ouvertes." });
            if (await _import.PorteDuForfaitAsync(eleveId, ct) is Refus fermee) return BadRequest(new { message = fermee.Message });

            if (await _depot.GetPourEntrainementAsync(eleveId, ficheId, ct) is null) return NotFound();

            try
            {
                var quiz = await entrainement.LireOuCreerAsync(_depot, eleveId, ficheId, ct);
                return quiz is null
                    ? BadRequest(new { message = "Je n’ai pas réussi à préparer tes questions. Réessaie dans un moment." })
                    : Ok(quiz);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Entrainement de la fiche {FicheId} impossible.", ficheId);
                return BadRequest(new { message = "Je n’ai pas réussi à préparer tes questions. Réessaie dans un moment." });
            }
        }

        /// <summary>« Me tester » sur tout le cours, depuis sa carte (16/10/2026).</summary>
        [HttpPost("cours/{coursId:int}/entrainement")]
        [AutoriseEleve]
        [SwaggerResponse(200, "Les questions de toutes les fiches du cours, chacune avec le titre de sa fiche.")]
        [SwaggerResponse(400, "Refusé — le message est écrit pour l'enfant.")]
        public async Task<IActionResult> EntrainementCours(
            int eleveId, int coursId, [FromServices] EntrainementFicheService entrainement,
            [FromServices] IServiceScopeFactory portees, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les fiches de révision ne sont pas ouvertes." });
            if (await _import.PorteDuForfaitAsync(eleveId, ct) is Refus fermee) return BadRequest(new { message = fermee.Message });

            var quiz = await entrainement.LireOuCreerCoursAsync(portees, eleveId, coursId, ct);
            return quiz is null
                ? BadRequest(new { message = "Je n’ai pas réussi à préparer tes questions. Réessaie dans un moment." })
                : Ok(quiz);
        }

        [HttpDelete("fiches/{ficheId:int}")]
        [AutoriseEleve]
        public async Task<IActionResult> SupprimerFiche(int eleveId, int ficheId, CancellationToken ct)
        {
            if (await _resolveur.ResoudreEleveAsync(eleveId) is null) return NotFound();
            return await _depot.SupprimerFicheAsync(eleveId, ficheId, ct) ? NoContent() : NotFound();
        }

        /// <summary>
        /// Le cours tel que l'écran le lit. Le contenu d'une fiche part en JSON
        /// (ses briques), pas en chaîne : le front n'a rien à reparser.
        /// </summary>
        internal static object Projeter(CoursDeLEleve c, bool avecContenu) => new
        {
            c.Id,
            c.MatiereId,
            c.MatiereLibelle,
            c.MatiereCode,
            // Espace parent (16/10/2026) : la catégorie tient lieu de matière.
            c.CategorieId,
            c.CategorieCouleur,
            c.Titre,
            c.Etat,
            c.Remarque,
            c.Type,
            notions = c.Notions,
            c.NotionsDemandees,
            c.DemandeLe,
            c.PagesDisponibles,
            c.DateCreation,
            c.ComplementDe,
            c.ComplementDeTitre,
            complement = c.Complement,
            pages = c.Pages,
            fiches = c.Fiches.Select(f => new
            {
                f.Id,
                f.Type,
                f.Titre,
                f.NotionRang,
                f.NotionTotal,
                f.DateCreation,
                blocs = avecContenu && f.Contenu is not null ? LireBlocs(f.Contenu) : null,
            }),
        };

        private static JsonNode? LireBlocs(string contenu)
        {
            try
            {
                return JsonNode.Parse(contenu);
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
