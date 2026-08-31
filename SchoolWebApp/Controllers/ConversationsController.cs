using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("conversations")]
    // Un enfant travaille ici : c'est tout l'objet de sa session.
    [SchoolWebApp.Api.Auth.AutoriseEleve]
    public class ConversationsController : Controller
    {
        private readonly IChatViewModelBuilder _chatBuilder;
        private readonly Workers.ReveilDocuments _reveilDocuments;
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(
            IChatViewModelBuilder chatBuilder,
            Workers.ReveilDocuments reveilDocuments,
            ILogger<ConversationsController> logger)
        {
            _chatBuilder = chatBuilder ?? throw new ArgumentNullException(nameof(chatBuilder));
            _reveilDocuments = reveilDocuments
                ?? throw new ArgumentNullException(nameof(reveilDocuments));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [SwaggerResponse(200, "Conversations de l'élève.", typeof(IEnumerable<ConversationViewModel>))]
        [SwaggerResponse(404, "Élève inexistant ou n'appartenant pas au parent authentifié.")]
        public async Task<IActionResult> GetConversations([FromQuery] int eleveId)
        {
            try
            {
                var conversations = await _chatBuilder.GetConversationsAsync(eleveId);
                return conversations is null ? NotFound() : Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation des conversations.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpPost]
        [SwaggerResponse(201, "Conversation créée.", typeof(ConversationViewModel))]
        [SwaggerResponse(400, "Élève ou matière invalide.")]
        public async Task<IActionResult> CreerConversation([FromBody] CreerConversationRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var conversation = await _chatBuilder.CreerConversationAsync(model);
                if (conversation is null)
                {
                    return BadRequest(new { message = "Élève ou matière invalide." });
                }

                return CreatedAtAction(nameof(GetMessages), new { id = conversation.Id }, conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la creation d'une conversation.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("{id:int}/messages")]
        [SwaggerResponse(200, "Historique de la conversation.", typeof(IEnumerable<MessageViewModel>))]
        [SwaggerResponse(404, "Conversation inexistante ou n'appartenant pas au parent authentifié.")]
        public async Task<IActionResult> GetMessages(int id)
        {
            try
            {
                var messages = await _chatBuilder.GetMessagesAsync(id);
                return messages is null ? NotFound() : Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation des messages de {ConversationId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Envoie un message et diffuse la réponse de l'agent en Server-Sent Events.
        ///
        /// Format du flux :
        ///   data: {"type":"delta","texte":"..."}
        ///   data: {"type":"fin"}
        ///   data: {"type":"erreur","message":"..."}
        ///
        /// C'est un POST : le front consomme le flux via fetch + ReadableStream,
        /// pas via EventSource (qui ne sait faire que du GET).
        /// </summary>
        [HttpPost("{id:int}/messages")]
        [SwaggerResponse(200, "Flux SSE de la réponse.")]
        [SwaggerResponse(404, "Conversation inexistante ou n'appartenant pas au parent authentifié.")]
        public async Task EnvoyerMessage(int id, [FromBody] EnvoyerMessageRequest model)
        {
            PreparerFluxSse();

            // Un tour vide reste refusé — mais un document SANS texte est un
            // tour plein : l'élève montre sa feuille, c'est un message.
            if (!ModelState.IsValid
                || (string.IsNullOrWhiteSpace(model.Contenu) && model.PieceJointeId is null))
            {
                await EcrireEvenementAsync(new { type = "erreur", message = "Message vide ou invalide." });
                return;
            }

            if (await QuotaEpuiseAsync(id)) return;

            try
            {
                await foreach (var fragment in _chatBuilder.StreamReponseAsync(
                    id, model.Contenu ?? string.Empty, model.PieceJointeId,
                    model.SecondesRestantes, HttpContext.RequestAborted))
                {
                    await EcrireEvenementAsync(new { type = "delta", texte = fragment });
                }

                await EcrireEvenementAsync(new { type = "fin" });
            }
            catch (UnauthorizedAccessException)
            {
                await EcrireEvenementAsync(new { type = "erreur", message = "Conversation introuvable." });
            }
            catch (OperationCanceledException)
            {
                // L'élève a fermé l'onglet : rien à signaler, le message partiel
                // a déjà été persisté côté builder.
                _logger.LogInformation("Flux interrompu par le client (conversation {ConversationId}).", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur pendant le streaming de la conversation {ConversationId}.", id);
                await EcrireEvenementAsync(new { type = "erreur", message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Fait parler l'agent en premier, à l'ouverture de la séance.
        /// Ne renvoie rien si la conversation a déjà des messages : l'élève
        /// reprend une séance, il ne débarque pas.
        /// </summary>
        [HttpPost("{id:int}/accueil")]
        [SwaggerResponse(200, "Flux SSE du message d'accueil.")]
        public async Task Accueil(int id)
        {
            PreparerFluxSse();

            if (await QuotaEpuiseAsync(id)) return;

            try
            {
                await foreach (var fragment in _chatBuilder.StreamAccueilAsync(
                    id, HttpContext.RequestAborted))
                {
                    await EcrireEvenementAsync(new { type = "delta", texte = fragment });
                }

                await EcrireEvenementAsync(new { type = "fin" });
            }
            catch (UnauthorizedAccessException)
            {
                await EcrireEvenementAsync(new { type = "erreur", message = "Conversation introuvable." });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Accueil interrompu par le client (conversation {ConversationId}).", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur pendant l'accueil de la conversation {ConversationId}.", id);
                await EcrireEvenementAsync(new { type = "erreur", message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Prise de parole commandée par l'horloge de séance.
        ///
        /// Le minuteur vit dans le navigateur — c'est lui qui sait où en est la
        /// séance. Le serveur ne fait qu'exécuter : il ne connaît ni la durée
        /// choisie ni le moment où l'élève est entré.
        /// </summary>
        /// <param name="type">
        /// `fin-proche` à cinq minutes du terme, `fin` à l'échéance,
        /// `nouvelle-seance` quand l'élève en relance une dans la foulée,
        /// `cloture-proche` et `cloture-forcee` quand un contrôle fait
        /// dépasser l'horaire.
        /// </param>
        [HttpPost("{id:int}/annonce")]
        [SwaggerResponse(200, "Flux SSE de l'annonce.")]
        public async Task Annonce(int id, [FromQuery] string type = "fin-proche")
        {
            PreparerFluxSse();

            var annonce = type switch
            {
                "fin" => TypeAccueil.FinSeance,
                "nouvelle-seance" => TypeAccueil.NouvelleSeance,
                "fin-imminente" => TypeAccueil.FinImminente,
                "cloture-proche" => TypeAccueil.ClotureProche,
                "cloture-forcee" => TypeAccueil.ClotureForcee,
                _ => TypeAccueil.FinProche,
            };

            try
            {
                await foreach (var fragment in _chatBuilder.StreamAnnonceAsync(
                    id, annonce, HttpContext.RequestAborted))
                {
                    await EcrireEvenementAsync(new { type = "delta", texte = fragment });
                }

                await EcrireEvenementAsync(new { type = "fin" });
            }
            catch (UnauthorizedAccessException)
            {
                await EcrireEvenementAsync(new { type = "erreur", message = "Conversation introuvable." });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Annonce interrompue (conversation {ConversationId}).", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur pendant l'annonce de la conversation {ConversationId}.", id);
                await EcrireEvenementAsync(new { type = "erreur", message = "Une erreur est survenue." });
            }
        }

        /// <summary>
        /// L'élève quitte le cours.
        ///
        /// Sans ce signal, le serveur ne peut que deviner à partir d'un délai
        /// de silence — et se trompe dans les deux sens : il salue un élève qui
        /// a juste rechargé la page, ou reste muet devant un élève qui revient
        /// après une pause de cinq minutes.
        /// </summary>
        /// <summary>
        /// La même sortie, signalée par le navigateur QUI SE FERME.
        ///
        /// POURQUOI UNE SECONDE ROUTE
        /// --------------------------
        /// Un élève ferme son onglet bien plus souvent qu'il ne clique sur
        /// « Quitter le cours ». Le navigateur annule alors toute requête en
        /// vol : `sendBeacon` est le seul envoi qui survive à la fermeture de
        /// la page. Mais il ne permet AUCUN en-tête — donc pas de jeton
        /// d'autorisation, et la route de départ, qui en exige un, ne peut pas
        /// le recevoir.
        ///
        /// POURQUOI L'OUVRIR SANS AUTHENTIFICATION EST ACCEPTABLE
        /// ------------------------------------------------------
        /// Elle ne lit rien et ne rend rien : elle pose une date de sortie sur
        /// une conversation et demande son analyse. Un tiers qui devinerait un
        /// identifiant ne gagnerait aucune donnée — au pire il ferait analyser
        /// une séance un peu plus tôt qu'elle ne l'aurait été, ce que le
        /// balayage périodique fait de toute façon.
        ///
        /// Le gain, lui, est réel : sans ce signal, les séances s'accumulent
        /// jusqu'au balayage et l'analyse arrive une demi-heure plus tard.
        /// </summary>
        [HttpPost("{id:int}/fermeture")]
        [AllowAnonymous]
        [SwaggerResponse(204, "Fermeture enregistrée.")]
        public async Task<IActionResult> Fermeture(int id)
        {
            try
            {
                await _chatBuilder.MarquerFermetureAsync(id);
            }
            catch (Exception ex)
            {
                // Rien ne doit remonter : la page est déjà en train de mourir,
                // personne ne lira cette réponse.
                _logger.LogWarning(ex, "Fermeture non enregistree pour la seance {Id}.", id);
            }

            return NoContent();
        }

        [HttpPost("{id:int}/quitter")]
        [SwaggerResponse(204, "Sortie enregistrée.")]
        public async Task<IActionResult> Quitter(int id)
        {
            try
            {
                await _chatBuilder.MarquerSortieAsync(id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                // Le départ de l'élève ne doit jamais bloquer sa navigation :
                // on journalise et on laisse partir.
                _logger.LogError(ex, "Erreur a la sortie de la conversation {ConversationId}.", id);
                return NoContent();
            }
        }

        /// <summary>
        /// Dépose un document que l'élève veut montrer au professeur.
        ///
        /// Il n'est envoyé à personne à ce stade : il attend qu'un message
        /// l'emporte. C'est ce qui permet à l'élève d'écrire sa phrase avant
        /// d'envoyer — un document seul ne dit pas ce qui le bloque.
        /// </summary>
        [HttpPost("{id:int}/pieces-jointes")]
        [RequestSizeLimit(ValidationPieceJointe.TailleMax + 4096)]
        [SwaggerResponse(200, "Document accepté.", typeof(PieceJointeViewModel))]
        [SwaggerResponse(400, "Document refusé — le motif est destiné à l'élève.")]
        [SwaggerResponse(404, "Conversation inexistante ou n'appartenant pas au compte authentifié.")]
        public async Task<IActionResult> DeposerPieceJointe(int id, IFormFile fichier)
        {
            if (fichier is null || fichier.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier reçu." });
            }

            if (fichier.Length > ValidationPieceJointe.TailleMax)
            {
                return BadRequest(new
                {
                    message = "Ce fichier est trop lourd. Essaie une photo un peu moins grande.",
                });
            }

            try
            {
                // En mémoire, et c'est assumé : le plafond est à cinq
                // mégaoctets, et les octets vont de toute façon en base d'un
                // seul tenant. Un fichier temporaire n'ajouterait qu'un chemin
                // à nettoyer.
                using var flux = new MemoryStream();
                await fichier.CopyToAsync(flux, HttpContext.RequestAborted);

                var resultat = await _chatBuilder.AjouterPieceJointeAsync(
                    id, fichier.FileName, fichier.ContentType,
                    flux.ToArray(), HttpContext.RequestAborted);

                if (!resultat.Autorise) return NotFound();

                if (resultat.Piece is null) return BadRequest(new { message = resultat.Motif });

                // ON SONNE APRÈS AVOIR RÉPONDU DE L'ACCEPTATION, PAS AVANT.
                //
                // L'élève n'attend rien de la transcription : c'est le document
                // lui-même qui part au professeur. Sonner ne fait donc que
                // réveiller un worker — quelques microsecondes — et surtout
                // n'ajoute aucune façon d'échouer sur le chemin de l'envoi.
                _reveilDocuments.Sonner();

                return Ok(resultat.Piece);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec du depot d'une piece jointe sur {ConversationId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Rend le document, pour l'afficher dans la conversation.
        ///
        /// Passe par l'API et non par un fichier statique : ce sont des
        /// documents personnels, souvent la copie d'un enfant avec son nom
        /// dessus. Ils ne doivent être lisibles que par le compte qui les a
        /// déposés, et une URL publique le rendrait impossible.
        /// </summary>
        [HttpGet("{id:int}/pieces-jointes/{pieceId:int}")]
        [SwaggerResponse(200, "Le document.")]
        [SwaggerResponse(404, "Document inexistant ou n'appartenant pas au compte authentifié.")]
        public async Task<IActionResult> GetPieceJointe(int id, int pieceId)
        {
            try
            {
                var contenu = await _chatBuilder.GetPieceJointeAsync(
                    pieceId, HttpContext.RequestAborted);

                if (contenu is null) return NotFound();

                // Privé : un cache partagé ne doit jamais garder la copie d'un
                // élève. Un an côté navigateur en revanche — le contenu ne
                // change jamais, il est identifié par sa clé primaire.
                Response.Headers.CacheControl = "private, max-age=31536000, immutable";

                return File(contenu.Donnees, contenu.TypeMime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de lecture de la piece jointe {PieceId}.", pieceId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Vérifie le quota et, s'il est épuisé, écrit l'événement qui va bien.
        /// Retourne vrai si l'appelant doit s'arrêter là.
        ///
        /// Un événement dédié plutôt qu'une erreur : le front doit distinguer
        /// « c'est cassé » de « il n'y a plus d'heures », qui n'appellent ni le
        /// même message ni la même action. Le motif dit lequel des quatre cas.
        ///
        /// L'annonce de fin de séance n'est PAS soumise à cette vérification :
        /// le professeur doit toujours pouvoir conclure et rendre une note, même
        /// si le quota vient de tomber à zéro pendant la séance.
        /// </summary>
        private async Task<bool> QuotaEpuiseAsync(int conversationId)
        {
            var verdict = await _chatBuilder.VerifierQuotaAsync(
                conversationId, HttpContext.RequestAborted);

            if (verdict.Autorise) return false;

            _logger.LogInformation(
                "Quota refuse ({Motif}) pour la conversation {ConversationId}.",
                verdict.Motif, conversationId);

            await EcrireEvenementAsync(new
            {
                type = "quota",
                motif = verdict.Motif.ToString(),
            });

            return true;
        }

        private void PreparerFluxSse()
        {
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            // Sans ça, ASP.NET accumule la réponse et le client reçoit tout d'un
            // bloc à la fin — ce qui annule l'intérêt du streaming.
            HttpContext.Features
                .Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()
                ?.DisableBuffering();
        }

        private async Task EcrireEvenementAsync(object charge)
        {
            var json = JsonSerializer.Serialize(charge);
            var octets = Encoding.UTF8.GetBytes($"data: {json}\n\n");

            await Response.Body.WriteAsync(octets, HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }
    }
}
