using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Les avis des familles.
    ///
    /// TROIS PUBLICS, TROIS NIVEAUX D'ACCÈS. Le visiteur lit ce qui est publié
    /// sans être connecté ; le parent connecté écrit le sien et lui seul ;
    /// l'administrateur relit et décide.
    /// </summary>
    [ApiController]
    [Route("avis")]
    public class AvisController : Controller
    {
        /// <summary>
        /// Le plus grand nombre d'avis qu'une seule requête peut ramener.
        ///
        /// La moyenne et la répartition portent sur TOUS les avis publiés — ce
        /// plafond ne concerne que la liste. Sans lui, un paramètre à un
        /// million ferait ramener toute la table à chaque appel, depuis une
        /// route ouverte sans authentification.
        /// </summary>
        private const int PlafondPage = 60;

        /// <summary>Ce que la page d'accueil demande, faute de précision.</summary>
        private const int ParDefaut = 10;

        private readonly IAvisRepository _avis;
        private readonly IChatContexteResolver _resolver;
        private readonly ILogger<AvisController> _logger;

        public AvisController(
            IAvisRepository avis,
            IChatContexteResolver resolver,
            ILogger<AvisController> logger)
        {
            _avis = avis ?? throw new ArgumentNullException(nameof(avis));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ce que la page d'accueil affiche. Sans authentification : c'est une
        /// vitrine, et un visiteur non inscrit est exactement son public.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [SwaggerResponse(200, "Moyenne, répartition et derniers avis.", typeof(AvisPublics))]
        public async Task<IActionResult> Publics(
            CancellationToken ct,
            [FromQuery] int limite = ParDefaut,
            [FromQuery] int decalage = 0)
        {
            try
            {
                // LES BORNES SONT RECALÉES, PAS REFUSÉES. Cette route est
                // ouverte : répondre 400 à un paramètre farfelu apprendrait
                // seulement à celui qui essaie où sont les limites, pendant
                // qu'un visiteur avec un lien abîmé verrait une page cassée.
                var combien = Math.Clamp(limite, 1, PlafondPage);
                var saut = Math.Max(decalage, 0);

                return Ok(await _avis.GetPublicsAsync(combien, saut, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la lecture des avis publics.");

                // UNE VITRINE VIDE PLUTÔT QU'UNE PAGE CASSÉE. Les avis sont un
                // ornement de la page d'accueil, pas sa raison d'être : une
                // erreur ici ne doit pas empêcher quelqu'un de découvrir le
                // produit ni de s'inscrire.
                return Ok(new AvisPublics());
            }
        }

        /// <summary>L'avis du parent connecté, s'il en a laissé un.</summary>
        [HttpGet("mien")]
        [Authorize]
        [SwaggerResponse(200, "Son avis.", typeof(MonAvis))]
        [SwaggerResponse(204, "Il n'en a pas encore laissé.")]
        public Task<IActionResult> Mien(CancellationToken ct) =>
            Executer(async parentId =>
            {
                var mien = await _avis.GetMonAvisAsync(parentId, ct);
                return mien is null ? NoContent() : Ok(mien);
            });

        /// <summary>
        /// Dépose ou remplace son avis.
        ///
        /// IL FAUT ÊTRE INSCRIT, et c'est la seule barrière qui tienne : sans
        /// elle, la page d'accueil accepterait du texte de n'importe qui, y
        /// compris d'un robot ou d'un concurrent, à raison d'un avis par
        /// requête.
        /// </summary>
        [HttpPut]
        [Authorize]
        [SwaggerResponse(200, "Avis enregistré, en attente de relecture.", typeof(MonAvis))]
        [SwaggerResponse(400, "Note hors de 1 à 5, ou commentaire trop long.")]
        public Task<IActionResult> Deposer(
            [FromBody] AvisRequest requete,
            [FromServices] ITelegramService telegram,
            CancellationToken ct) =>
            Executer(async parentId =>
            {
                if (requete is null || requete.Note is < 1 or > 5)
                {
                    return BadRequest(new { message = "La note doit aller de 1 à 5 étoiles." });
                }

                // LES LONGUEURS SONT VÉRIFIÉES ICI AUSSI, pas seulement en base.
                // Une chaîne trop longue y lèverait une exception SQL, que le
                // parent verrait sous la forme d'un « une erreur est survenue »
                // sans savoir lequel de ses deux champs déborde.
                if ((requete.Titre?.Length ?? 0) > 120)
                {
                    return BadRequest(new { message = "Le titre ne peut pas dépasser 120 caractères." });
                }

                if ((requete.Commentaire?.Length ?? 0) > 2000)
                {
                    return BadRequest(new { message = "L'avis ne peut pas dépasser 2000 caractères." });
                }

                var mien = await _avis.DeposerAsync(
                    parentId, requete.Note, requete.Titre, requete.Commentaire, ct);

                _logger.LogInformation(
                    "Avis {Note}/5 depose par le parent {ParentId}, en attente de relecture.",
                    requete.Note, parentId);

                // L'ALERTE EST ENVOYÉE APRÈS L'ENREGISTREMENT, JAMAIS AVANT.
                //
                // Prévenir d'abord annoncerait un avis qui n'existe pas si
                // l'écriture échouait juste après. L'ordre inverse fait courir
                // le risque opposé — un avis enregistré dont personne n'est
                // prévenu — et il est bien moindre : il reste visible dans
                // l'onglet Avis, qui est de toute façon l'endroit où l'on
                // décide.
                //
                // POURQUOI CETTE ALERTE EXISTE. L'avis arrive non publié : il
                // n'est visible de personne tant qu'il n'a pas été relu. Sans
                // ce message, il faudrait ouvrir le tableau de bord au hasard
                // pour découvrir qu'une famille attend depuis trois jours.
                var parent = await _resolver.ResoudreParentAsync();

                await telegram.NotifierAvisAsync(
                    parent.Mail, requete.Note, requete.Titre, requete.Commentaire, DateTime.UtcNow);

                return Ok(mien);
            });

        /// <summary>Retire son propre avis.</summary>
        [HttpDelete]
        [Authorize]
        [SwaggerResponse(204, "Avis retiré, ou déjà absent.")]
        public Task<IActionResult> Retirer(CancellationToken ct) =>
            Executer(async parentId =>
            {
                await _avis.RetirerAsync(parentId, ct);

                // 204 même s'il n'y avait rien : la demande était « qu'il n'en
                // reste rien », et il n'en reste rien.
                return NoContent();
            });

        // ------------------------------------------------------------------
        // Relecture
        // ------------------------------------------------------------------

        /// <summary>Tous les avis, en attente d'abord.</summary>
        [HttpGet("relecture")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Les avis à relire.", typeof(IEnumerable<AvisAdmin>))]
        public async Task<IActionResult> Relecture(CancellationToken ct)
        {
            try
            {
                return Ok(await _avis.GetPourRelectureAsync(ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la lecture des avis a relire.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Publie ou dépublie un avis.
        ///
        /// RÉVERSIBLE DANS LES DEUX SENS, à dessein. Un avis publié qu'on
        /// regrette se retire d'un clic, sans passer par une suppression qui
        /// effacerait ce que la famille a écrit.
        /// </summary>
        [HttpPut("{id:int}/publication")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(204, "Publication mise à jour.")]
        [SwaggerResponse(404, "Avis inexistant.")]
        public async Task<IActionResult> Publier(
            int id, [FromBody] PublicationRequest requete, CancellationToken ct)
        {
            try
            {
                var publie = requete?.Publie ?? false;
                var fait = await _avis.PublierAsync(id, publie, ct);

                if (fait)
                {
                    _logger.LogInformation(
                        "Avis {AvisId} {Etat} par un administrateur.",
                        id, publie ? "publie" : "depublie");
                }

                return fait ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la publication de l avis {AvisId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Supprime définitivement un avis.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(204, "Avis supprimé.")]
        [SwaggerResponse(404, "Avis inexistant.")]
        public async Task<IActionResult> Supprimer(int id, CancellationToken ct)
        {
            try
            {
                var fait = await _avis.SupprimerAsync(id, ct);

                if (fait)
                {
                    _logger.LogWarning("Avis {AvisId} supprime par un administrateur.", id);
                }

                return fait ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la suppression de l avis {AvisId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <param name="Note">De 1 à 5.</param>
        /// <param name="Titre">Facultatif, 120 caractères au plus.</param>
        /// <param name="Commentaire">Facultatif, 2000 caractères au plus.</param>
        public record AvisRequest(int Note, string? Titre, string? Commentaire);

        /// <param name="Publie">Vrai pour publier, faux pour retirer de la vitrine.</param>
        public record PublicationRequest(bool Publie);

        /// <summary>
        /// Résout le parent courant, comme les autres contrôleurs de l'espace
        /// famille. Un enfant porte le « sub » de son parent : il pourrait donc
        /// déposer un avis au nom du foyer, et c'est accepté — la note vient de
        /// la famille, pas d'une personne.
        /// </summary>
        private async Task<IActionResult> Executer(Func<int, Task<IActionResult>> action)
        {
            try
            {
                var parent = await _resolver.ResoudreParentAsync();
                return await action(parent.Id);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur sur une operation d avis.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }
    }
}
