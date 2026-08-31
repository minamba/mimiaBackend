using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SchoolWebApp.Api.Auth;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// L'entrée des enfants : un code, une session.
    ///
    /// Ce contrôleur est le SEUL point anonyme du parcours enfant. Tout ce qui
    /// vient après exige le jeton qu'il délivre.
    /// </summary>
    [ApiController]
    [Route("sessions/eleve")]
    public class SessionsElevesController : Controller
    {
        /// <summary>
        /// Tentatives ratées tolérées avant blocage, par adresse.
        ///
        /// Le code fait six caractères sur un alphabet de vingt-cinq : deux
        /// cent quarante-quatre millions de possibilités. Sans limite, une
        /// machine les épuise en une nuit. Avec dix essais par quart d'heure,
        /// il lui faudrait des millénaires.
        ///
        /// Dix et non trois : un enfant de huit ans se trompe, recommence, et
        /// ne doit pas être puni pour ça.
        /// </summary>
        private const int TentativesMax = 10;

        private static readonly TimeSpan Fenetre = TimeSpan.FromMinutes(15);

        private readonly IEleveService _eleves;
        private readonly ISessionEleveRepository _sessions;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SessionsElevesController> _logger;

        public SessionsElevesController(
            IEleveService eleves,
            ISessionEleveRepository sessions,
            IMemoryCache cache,
            ILogger<SessionsElevesController> logger)
        {
            _eleves = eleves ?? throw new ArgumentNullException(nameof(eleves));
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Échange le code d'un enfant contre un jeton de session.
        ///
        /// LES REFUS NE DISENT PAS POURQUOI, SAUF UN
        /// ----------------------------------------
        /// « Code inconnu » et « code juste mais enfant suspendu » sont deux
        /// réponses différentes, et la seconde apprend à qui tâtonne qu'il a
        /// trouvé un code valide. On ne distingue donc que le cas où le code
        /// est bon ET l'accès suspendu — parce que l'enfant, lui, a besoin de
        /// comprendre qu'il doit demander à ses parents plutôt que de retaper.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [SwaggerResponse(200, "Session ouverte.")]
        [SwaggerResponse(403, "Accès suspendu par le parent.")]
        [SwaggerResponse(404, "Code inconnu.")]
        [SwaggerResponse(429, "Trop de tentatives.")]
        public async Task<IActionResult> Ouvrir([FromBody] CodeRequest requete)
        {
            var cle = $"session-eleve:{HttpContext.Connection.RemoteIpAddress}";
            var tentatives = _cache.Get<int>(cle);

            if (tentatives >= TentativesMax)
            {
                return StatusCode(429, new
                {
                    message = "Trop d'essais. Réessaie dans un quart d'heure.",
                });
            }

            var eleve = await _eleves.GetParCodeAsync(
                requete.Code ?? string.Empty, HttpContext.RequestAborted);

            if (eleve is null)
            {
                // Le compteur ne monte QUE sur un échec : un enfant qui entre
                // du premier coup, dix jours de suite, ne s'approche jamais du
                // plafond.
                _cache.Set(cle, tentatives + 1, Fenetre);

                return NotFound(new { message = "Ce code ne correspond à aucun élève." });
            }

            if (!eleve.AccesOuvert)
            {
                return StatusCode(403, new
                {
                    message = "Tes cours sont en pause. Demande à un parent de les rouvrir.",
                });
            }

            // Le code était bon : on efface l'ardoise, y compris les essais
            // ratés d'un enfant qui cherchait ses lunettes.
            _cache.Remove(cle);

            var jeton = AuthentificationEleve.GenererJeton();

            var session = await _sessions.OuvrirAsync(
                eleve.Id,
                AuthentificationEleve.Hacher(jeton),
                Request.Headers.UserAgent.ToString(),
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "Session ouverte pour l'eleve {EleveId} ({Session}).", eleve.Id, session.Id);

            return Ok(new
            {
                jeton,
                eleve = new
                {
                    eleve.Id,
                    eleve.Prenom,
                    eleve.NiveauLibelle,
                },
            });
        }

        /// <summary>
        /// L'enfant se déconnecte : sa session disparaît, son code reste.
        ///
        /// C'est la différence entre se déconnecter et perdre son accès. Il
        /// retapera son code — que son parent peut toujours lui relire.
        /// </summary>
        [HttpDelete]
        [AllowAnonymous]
        [SwaggerResponse(204, "Session fermée.")]
        public async Task<IActionResult> Fermer()
        {
            var entete = Request.Headers.Authorization.ToString();

            if (entete.StartsWith("Eleve ", StringComparison.Ordinal))
            {
                var jeton = entete["Eleve ".Length..].Trim();

                await _sessions.FermerAsync(
                    AuthentificationEleve.Hacher(jeton), HttpContext.RequestAborted);
            }

            return NoContent();
        }

        public class CodeRequest
        {
            public string? Code { get; set; }
        }
    }
}
