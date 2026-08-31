using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Ce que le navigateur a réellement vécu.
    ///
    /// Le reste de l'application mesure ce que le serveur fait ; ici on
    /// enregistre ce que l'enfant a perçu, et les deux ne coïncident pas. Un
    /// serveur qui répond en cent millisecondes sur une ligne saturée donne une
    /// voix qui met deux secondes à démarrer, et aucun journal côté serveur ne
    /// le montrera jamais.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("mesures")]
    // Un enfant travaille ici : c'est tout l'objet de sa session.
    [SchoolWebApp.Api.Auth.AutoriseEleve]
    public class MesuresController : Controller
    {
        private readonly IMesureVoixRepository _mesures;
        private readonly ILogger<MesuresController> _logger;

        public MesuresController(
            IMesureVoixRepository mesures, ILogger<MesuresController> logger)
        {
            _mesures = mesures ?? throw new ArgumentNullException(nameof(mesures));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class MesureVoixRequest
        {
            /// <summary>Tirage au sort du navigateur, valable une séance.</summary>
            [Required]
            [StringLength(36, MinimumLength = 8)]
            public string Seance { get; set; } = string.Empty;

            /// <summary>Délai jusqu'à la première syllabe, en millisecondes.</summary>
            [Range(0, 300_000)]
            public int DelaiMs { get; set; }

            /// <summary>Prononcé par la voix du navigateur, faute de mieux ?</summary>
            public bool Repli { get; set; }

            /// <summary>
            /// Les trois maillons qui manquaient au décompte.
            ///
            /// Facultatifs : un tour où l élève a ÉCRIT au lieu de parler n a
            /// ni transcription ni assemblage, et la mesure garde tout son sens
            /// pour les deux autres.
            /// </summary>
            [Range(0, 300_000)]
            public int? TranscriptionMs { get; set; }

            [Range(0, 300_000)]
            public int? AssemblageMs { get; set; }

            [Range(0, 300_000)]
            public int? ReponseMs { get; set; }
        }

        /// <summary>
        /// Enregistre un délai de prise de parole.
        ///
        /// Répond TOUJOURS 204, même en cas d'échec. Une mesure est une
        /// commodité pour nous, jamais une gêne pour l'élève : si la table est
        /// pleine, si la base est indisponible, si la requête est mal formée,
        /// le cours continue sans que rien ne s'affiche. Une erreur remontée au
        /// navigateur ferait apparaître un message rouge pendant une séance —
        /// pour un chiffre dont l'enfant n'a que faire.
        /// </summary>
        [HttpPost("voix")]
        [SwaggerResponse(204, "Mesure prise en compte, ou ignorée sans bruit.")]
        public async Task<IActionResult> Voix([FromBody] MesureVoixRequest requete)
        {
            try
            {
                await _mesures.EnregistrerAsync(
                    requete.Seance,
                    requete.DelaiMs,
                    requete.Repli,
                    requete.TranscriptionMs,
                    requete.AssemblageMs,
                    requete.ReponseMs,
                    HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mesure de voix non enregistrée.");
            }

            return NoContent();
        }
    }
}
