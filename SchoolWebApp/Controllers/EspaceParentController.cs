using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.MesFiches;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// L'ESPACE PARENT — Camara, le 16/10/2026 : « le système des fiches, mais
    /// pour les parents qui en ont le droit, PAS LIÉ AUX ENFANTS ; le parent
    /// crée ses propres catégories ; ces fiches sont visibles par lui seul ».
    ///
    /// LE MÊME MODULE QUE CELUI DE L'ÉLÈVE (`ImportCoursService`, la tâche de
    /// fond, la rédaction) : seul le propriétaire change (`Proprietaire.Parent`).
    ///
    /// PARENT SEULEMENT : aucune route n'a `AutoriseEleve`, une session d'enfant
    /// est donc refusée ici par le filtre global. Et chaque route vérifie le
    /// DROIT du parent (coché dans Admin › Parents ; le super-administrateur l'a
    /// toujours) — sans lui, l'espace n'existe pas (404).
    ///
    /// LIMITES ET QUOTA : un administrateur ou le super-administrateur n'a ni
    /// plafond d'import ni quota ; un autre parent a les plafonds habituels et
    /// puise dans les crédits achetés par la famille (`QuotaFichesService`).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("parents/moi")]
    public class EspaceParentController : ControllerBase
    {
        public sealed record CategorieFichesRequest(string? Libelle, string? Couleur);

        public sealed record NouveauCoursParentRequest(int? CategorieId, string Type, int? ComplementDe = null);

        public sealed record DemandeFichesParentRequest(string Type, List<int>? Notions);

        private readonly IFichesDeCoursRepository _depot;
        private readonly ImportCoursService _import;
        private readonly ICreditsFichesRepository _credits;
        private readonly IParentService _parents;
        private readonly ICurrentUserAccessor _utilisateur;
        private readonly ILogger<EspaceParentController> _logger;

        public EspaceParentController(
            IFichesDeCoursRepository depot, ImportCoursService import, ICreditsFichesRepository credits,
            IParentService parents, ICurrentUserAccessor utilisateur, ILogger<EspaceParentController> logger)
        {
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _import = import ?? throw new ArgumentNullException(nameof(import));
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _parents = parents ?? throw new ArgumentNullException(nameof(parents));
            _utilisateur = utilisateur ?? throw new ArgumentNullException(nameof(utilisateur));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Le parent connecté, et ce que son espace lui ouvre.</summary>
        private async Task<(int ParentId, DroitsEspaceParent Droits)?> ParentAsync(CancellationToken ct)
        {
            var identifiant = _utilisateur.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identifiant) || _utilisateur.EleveId is not null) return null;

            var parent = await _parents.GetOrCreateAsync(
                identifiant, _utilisateur.Mail, _utilisateur.Prenom, _utilisateur.Nom, noterLaVenue: false);
            return (parent.Id, await _depot.DroitsEspaceParentAsync(parent.Id, ct));
        }

        /// <summary>Le parent connecté, s'il a le droit aux fiches ; null sinon.</summary>
        private async Task<int?> ParentAutoriseAsync(CancellationToken ct) =>
            await ParentAsync(ct) is { Droits.Fiches: true } p ? p.ParentId : null;

        // ------------------------------------------------------------ l'espace

        /// <summary>Ce que l'espace ouvre : la barre de navigation n'affiche le bouton que si `fiches` est vrai.</summary>
        [HttpGet("espace")]
        [SwaggerResponse(200, "fiches : le droit aux fiches ; sansLimite : administrateur ; actif : le module est ouvert.")]
        public async Task<IActionResult> Espace(CancellationToken ct)
        {
            var parent = await ParentAsync(ct);
            return Ok(new
            {
                fiches = parent?.Droits.Fiches ?? false,
                sansLimite = parent?.Droits.SansLimite ?? false,
                actif = await _import.ModuleActifAsync(ct),
            });
        }

        // --------------------------------------------------------- catégories

        [HttpGet("categories")]
        public async Task<IActionResult> Categories(CancellationToken ct) =>
            await ParentAutoriseAsync(ct) is int parentId
                ? Ok(await _depot.ListerCategoriesAsync(parentId, ct))
                : NotFound();

        [HttpPost("categories")]
        [SwaggerResponse(200, "La catégorie créée.")]
        [SwaggerResponse(400, "Nom vide, couleur inconnue, ou trop de catégories.")]
        public async Task<IActionResult> CreerCategorie([FromBody] CategorieFichesRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (Verifier(requete) is not ({ } libelle, { } couleur)) return BadRequest(new { message = "Donnez un nom à la catégorie." });

            var categorie = await _depot.CreerCategorieAsync(parentId, libelle, couleur, DateTime.UtcNow, ct);
            return categorie is null
                ? BadRequest(new { message = $"Vous avez déjà {Regles.CategoriesMax} catégories : c’est le maximum." })
                : Ok(categorie);
        }

        [HttpPut("categories/{categorieId:int}")]
        public async Task<IActionResult> ModifierCategorie(int categorieId, [FromBody] CategorieFichesRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (Verifier(requete) is not ({ } libelle, { } couleur)) return BadRequest(new { message = "Donnez un nom à la catégorie." });

            return await _depot.ModifierCategorieAsync(parentId, categorieId, libelle, couleur, ct) ? NoContent() : NotFound();
        }

        /// <summary>Supprime la catégorie ; ses fiches restent, « sans catégorie ».</summary>
        [HttpDelete("categories/{categorieId:int}")]
        public async Task<IActionResult> SupprimerCategorie(int categorieId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            return await _depot.SupprimerCategorieAsync(parentId, categorieId, ct) ? NoContent() : NotFound();
        }

        /// <summary>Un nom (60 caractères au plus) et une couleur de la palette ; la première par défaut.</summary>
        private static (string? Libelle, string? Couleur) Verifier(CategorieFichesRequest? requete)
        {
            var libelle = requete?.Libelle?.Trim();
            if (string.IsNullOrWhiteSpace(libelle)) return (null, null);
            if (libelle.Length > Regles.CategorieLibelleMax) libelle = libelle[..Regles.CategorieLibelleMax].Trim();

            var couleur = Regles.CouleursCategories.FirstOrDefault(c =>
                string.Equals(c, requete?.Couleur?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Regles.CouleursCategories[0];
            return (libelle, couleur);
        }

        // ----------------------------------------------------------- les fiches

        [HttpGet("mes-fiches")]
        [SwaggerResponse(200, "Ses cours envoyés et leurs fiches, du plus récent au plus ancien.")]
        public async Task<IActionResult> Lister(CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();

            var proprietaire = Proprietaire.Parent(parentId);
            var cours = await _depot.ListerAsync(proprietaire, ct);
            return Ok(new
            {
                actif = await _import.ModuleActifAsync(ct),
                forfait = true,
                quota = await QuotaAsync(parentId, ct),
                categories = await _depot.ListerCategoriesAsync(parentId, ct),
                cours = cours.Select(c => MesFichesController.Projeter(c, false)),
            });
        }

        [HttpGet("mes-fiches/quota")]
        public async Task<IActionResult> Quota(CancellationToken ct) =>
            await ParentAutoriseAsync(ct) is int parentId ? Ok(await QuotaAsync(parentId, ct)) : NotFound();

        /// <summary>
        /// La jauge du parent : ses crédits (la famille) et ses plafonds, avec
        /// les packs à acheter. `sansLimite` : rien à compter, l'écran n'affiche
        /// pas de jauge.
        /// </summary>
        private async Task<object> QuotaAsync(int parentId, CancellationToken ct)
        {
            var proprietaire = Proprietaire.Parent(parentId);
            var q = await _import.QuotaDuProprietaireAsync(proprietaire, ct);
            var l = await _import.LimitesAsync(proprietaire, ct);
            var packs = (await _credits.GetOffresAsync(ct))
                .Select(p => new { p.Code, p.Libelle, p.Fiches, p.PrixCentimes })
                .ToList();

            return new
            {
                parent = true,
                sansLimite = l.SansLimite,
                q.Utilisees, q.Maximum, q.Restantes, q.Credits, q.Disponibles, q.RemiseLe, q.CreditsPris,
                packs,
                limites = new { l.SansLimite, l.PagesMax, l.PiecesMax, l.TailleFichierMax, l.NotionsMax },
            };
        }

        [HttpPost("mes-fiches/cours")]
        [SwaggerResponse(200, "Le brouillon créé : on y dépose ensuite les pages.")]
        [SwaggerResponse(400, "Catégorie ou type refusé.")]
        public async Task<IActionResult> Creer([FromBody] NouveauCoursParentRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les fiches de révision ne sont pas ouvertes." });

            var proprietaire = Proprietaire.Parent(parentId);
            var categorieId = requete.CategorieId;

            // COMPLÉTER UN COURS : un cours à lui, prêt, qui n'est pas lui-même un
            // complément ; sa catégorie s'impose.
            if (requete.ComplementDe is { } completeId)
            {
                var complete = await _depot.GetAsync(proprietaire, completeId, ct);
                if (complete is null || complete.ComplementDe is not null || complete.Etat != Regles.EtatPret)
                {
                    return BadRequest(new { message = "Ce cours ne peut pas être complété." });
                }

                if (complete.Complement is { } enRoute && enRoute.Etat != Regles.EtatEchec)
                {
                    return BadRequest(new { message = "Un complément de ce cours est déjà en préparation." });
                }

                categorieId = complete.CategorieId;
            }

            if (await VerifierChoixAsync(parentId, categorieId, requete.Type, ct) is string refus) return BadRequest(new { message = refus });

            var id = await _depot.CreerBrouillonAsync(
                proprietaire, null, Regles.Type(requete.Type)!.Code, DateTime.UtcNow, requete.ComplementDe, ct, categorieId);
            return Ok(new { id });
        }

        [HttpPut("mes-fiches/cours/{coursId:int}")]
        public async Task<IActionResult> Modifier(int coursId, [FromBody] NouveauCoursParentRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (await VerifierChoixAsync(parentId, requete.CategorieId, requete.Type, ct) is string refus) return BadRequest(new { message = refus });

            return await _depot.ModifierBrouillonAsync(
                Proprietaire.Parent(parentId), coursId, null, Regles.Type(requete.Type)!.Code, DateTime.UtcNow, ct, requete.CategorieId)
                ? NoContent()
                : BadRequest(new { message = "Ce cours est déjà envoyé : il ne se modifie plus." });
        }

        /// <summary>Une catégorie à lui (ou aucune), et un type qui existe : un adulte a tous les types.</summary>
        private async Task<string?> VerifierChoixAsync(int parentId, int? categorieId, string? type, CancellationToken ct)
        {
            if (categorieId is int id && !await _depot.CategorieDuParentAsync(parentId, id, ct)) return "Cette catégorie n’existe plus.";
            return Regles.Type(type) is null ? "Ce type de fiche n’existe pas." : null;
        }

        [HttpGet("mes-fiches/cours/{coursId:int}")]
        public async Task<IActionResult> Lire(int coursId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            var cours = await _depot.GetAsync(Proprietaire.Parent(parentId), coursId, ct);
            return cours is null ? NotFound() : Ok(MesFichesController.Projeter(cours, true));
        }

        [HttpDelete("mes-fiches/cours/{coursId:int}")]
        public async Task<IActionResult> Supprimer(int coursId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            return await _depot.SupprimerCoursAsync(Proprietaire.Parent(parentId), coursId, ct) ? NoContent() : NotFound();
        }

        [HttpPost("mes-fiches/cours/{coursId:int}/pages")]
        // Le plafond des administrateurs ; celui des autres est vérifié dans le service.
        [RequestSizeLimit(Regles.TailleFichierMaxAdmin + 4096)]
        [SwaggerResponse(200, "La page reçue.")]
        [SwaggerResponse(400, "Fichier refusé.")]
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

        [HttpDelete("mes-fiches/cours/{coursId:int}/pages/{pageId:int}")]
        public async Task<IActionResult> SupprimerPage(int coursId, int pageId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            return await _depot.SupprimerPageAsync(Proprietaire.Parent(parentId), coursId, pageId, ct) ? NoContent() : NotFound();
        }

        [HttpPost("mes-fiches/cours/{coursId:int}/envoyer")]
        [SwaggerResponse(204, "Le cours part à la lecture.")]
        [SwaggerResponse(400, "Refusé : plus de crédits, ou aucune page.")]
        public async Task<IActionResult> Envoyer(int coursId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            var refus = await _import.EnvoyerAsync(Proprietaire.Parent(parentId), coursId, ct);
            return refus is null ? NoContent() : BadRequest(new { message = refus.Message });
        }

        [HttpPost("mes-fiches/cours/{coursId:int}/fiches")]
        public async Task<IActionResult> Demander(int coursId, [FromBody] DemandeFichesParentRequest requete, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();

            var type = Regles.Type(requete.Type);
            if (type is null) return BadRequest(new { message = "Ce type de fiche n’existe pas." });

            var refus = await _import.DemanderAsync(Proprietaire.Parent(parentId), coursId, type.Code, requete.Notions, ct);
            return refus is null ? NoContent() : BadRequest(new { message = refus.Message });
        }

        /// <summary>« Me tester » sur une fiche : hors quota, comme pour l'élève.</summary>
        [HttpPost("mes-fiches/fiches/{ficheId:int}/entrainement")]
        public async Task<IActionResult> Entrainement(int ficheId, [FromServices] EntrainementFicheService entrainement, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les fiches de révision ne sont pas ouvertes." });

            var proprietaire = Proprietaire.Parent(parentId);
            if (await _depot.GetPourEntrainementAsync(proprietaire, ficheId, ct) is null) return NotFound();

            try
            {
                var quiz = await entrainement.LireOuCreerAsync(_depot, proprietaire, ficheId, ct);
                return quiz is null
                    ? BadRequest(new { message = "Les questions n’ont pas pu être préparées. Réessayez dans un moment." })
                    : Ok(quiz);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Entrainement de la fiche {FicheId} (espace parent) impossible.", ficheId);
                return BadRequest(new { message = "Les questions n’ont pas pu être préparées. Réessayez dans un moment." });
            }
        }

        [HttpPost("mes-fiches/cours/{coursId:int}/entrainement")]
        public async Task<IActionResult> EntrainementCours(
            int coursId, [FromServices] EntrainementFicheService entrainement, [FromServices] IServiceScopeFactory portees,
            CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            if (!await _import.ModuleActifAsync(ct)) return BadRequest(new { message = "Les fiches de révision ne sont pas ouvertes." });

            var quiz = await entrainement.LireOuCreerCoursAsync(portees, Proprietaire.Parent(parentId), coursId, ct);
            return quiz is null
                ? BadRequest(new { message = "Les questions n’ont pas pu être préparées. Réessayez dans un moment." })
                : Ok(quiz);
        }

        [HttpDelete("mes-fiches/fiches/{ficheId:int}")]
        public async Task<IActionResult> SupprimerFiche(int ficheId, CancellationToken ct)
        {
            if (await ParentAutoriseAsync(ct) is not int parentId) return NotFound();
            return await _depot.SupprimerFicheAsync(Proprietaire.Parent(parentId), ficheId, ct) ? NoContent() : NotFound();
        }
    }
}
