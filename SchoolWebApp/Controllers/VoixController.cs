using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Voix;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Voix des professeurs.
    ///
    /// La synthèse passe par le serveur et non par le navigateur : c'est la clé
    /// API qui est en jeu, elle ne doit jamais atteindre le front.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("voix")]
    // Un enfant travaille ici : c'est tout l'objet de sa session.
    [SchoolWebApp.Api.Auth.AutoriseEleve]
    public class VoixController : Controller
    {
        private readonly ISyntheseVocaleService _synthese;
        private readonly ILogger<VoixController> _logger;

        public VoixController(ISyntheseVocaleService synthese, ILogger<VoixController> logger)
        {
            _synthese = synthese ?? throw new ArgumentNullException(nameof(synthese));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Indique si la voix serveur est configurée. Le front interroge cette
        /// route au démarrage pour choisir entre la vraie voix et le repli
        /// navigateur, plutôt que de découvrir la panne au milieu d'une phrase.
        /// </summary>
        [HttpGet("disponible")]
        [SwaggerOperation(Summary = "Disponibilité de la synthèse vocale serveur")]
        public IActionResult Disponible() => Ok(new { disponible = _synthese.Disponible });

        /// <summary>
        /// Synthétise UNE phrase. Le découpage est fait par le front, qui
        /// enchaîne les phrases au fil du flux de génération.
        /// </summary>
        [HttpPost("synthese")]
        [SwaggerOperation(Summary = "Audio d'une phrase, dans la voix du professeur")]
        [Produces("audio/pcm")]
        public async Task<IActionResult> Synthese(
            [FromBody] SyntheseRequest requete, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!_synthese.Disponible)
            {
                // 503 et non 500 : ce n'est pas un bug, c'est une absence de
                // configuration, et le front sait basculer sur le navigateur.
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { message = "La voix serveur n'est pas configurée." });
            }

            try
            {
                // On écrit dans le corps de la réponse au fil de l'arrivée
                // plutôt que de rendre un File() : `File()` attend le tableau
                // complet, donc la totalité de la génération OpenAI, avant que
                // le navigateur ne reçoive le premier octet.
                Response.ContentType = "audio/pcm";

                // Sans cet en-tête, un proxy ou IIS peut retamponner la réponse
                // et réintroduire l'attente qu'on vient de supprimer.
                Response.Headers["X-Accel-Buffering"] = "no";
                Response.Headers.CacheControl = "no-store";

                await _synthese.CopierAudioAsync(
                    requete.Texte!, requete.Avatar, requete.Age, Response.Body,
                    requete.Dictee, requete.Langue, requete.Vitesse, ct);

                return new EmptyResult();
            }
            catch (OperationCanceledException)
            {
                // L'élève a repris la parole ou changé de page : rien à signaler.
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de synthese vocale.");

                // Une fois le premier octet parti, le code de statut est figé :
                // tenter de le changer lèverait une seconde exception qui
                // masquerait la vraie. Le client verra un flux tronqué et
                // basculera sur la voix du navigateur.
                if (Response.HasStarted) return new EmptyResult();

                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "La voix est momentanément indisponible." });
            }
        }

        public class SyntheseRequest
        {
            [Required]
            [StringLength(800, MinimumLength = 1)]
            public string? Texte { get; set; }

            /// <summary>Identifiant du professeur : nora, adrien, salim, chloe, yann.</summary>
            [StringLength(50)]
            public string? Avatar { get; set; }

            [Range(5, 25)]
            public int Age { get; set; } = 12;

            /// <summary>
            /// Ce passage est DICTÉ : l élève l écrit à la main pendant qu on
            /// le prononce. Le débit change entièrement — lent, régulier, avec
            /// de vrais silences entre les groupes de souffle — et cette
            /// consigne remplace le registre lié à l âge, qui demande au
            /// contraire de ne pas ralentir à partir du collège.
            /// </summary>
            public bool Dictee { get; set; }

            /// <summary>
            /// Code de la langue dans laquelle CE passage se prononce — "en",
            /// "es", "de", "it", "zh" — ou absent pour le registre habituel.
            ///
            /// Un professeur de langue explique en français et fait pratiquer
            /// dans la langue étudiée. Sa consigne lui interdisait jusqu'ici de
            /// prononcer le moindre mot de cette langue — la voix lisant en
            /// français, un mot étranger y devenait une fausse prononciation
            /// apprise.
            ///
            /// L'interdit rendait la compréhension orale impossible : une
            /// compétence d'écoute par niveau, du CP à la terminale, restait
            /// grise faute de pouvoir être travaillée. Ce champ lève la règle
            /// PHRASE PAR PHRASE, sur les seuls passages que le professeur borne
            /// lui-même — l'annonce et la consigne restent en français. UNE
            /// BALISE PAR LANGUE, MÊME GABARIT POUR TOUTES : voir
            /// `PromptsPedagogiques.EnseignerUneLangue`.
            /// </summary>
            [StringLength(10)]
            public string? Langue { get; set; }

            /// <summary>
            /// La vitesse choisie par l'élève avant un exercice d'écoute :
            /// "tres_lent", "lent", "normal" ou "rapide" — absente partout
            /// ailleurs. Voulu par Camara le 12/09/2026 : celui qui n'a rien
            /// compris n'ose pas toujours demander qu'on ralentisse.
            ///
            /// Une valeur inconnue vaut "normal" : mieux vaut une lecture
            /// ordinaire qu'une erreur au milieu d'un exercice.
            /// </summary>
            [StringLength(20)]
            public string? Vitesse { get; set; }
        }
    }
}
