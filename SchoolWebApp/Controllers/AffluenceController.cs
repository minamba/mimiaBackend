using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Middleware;
using SchoolWebApp.Api.Services.Affluence;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Le guichet de la salle d'attente : on y prend son rang, et on y revient
    /// voir s'il est venu.
    ///
    /// HORS DE LA SALLE, ÉVIDEMMENT. Faire garder le guichet par le garde
    /// enfermerait tout le monde dehors : personne ne pourrait plus demander à
    /// entrer, pas même ceux qui attendent depuis dix minutes.
    ///
    /// ANONYME, et il le faut : la file se prend avant la connexion, puisque
    /// c'est la connexion elle-même qui coûte cher au serveur.
    ///
    /// LA ROUTE LA PLUS APPELÉE DU SITE un jour d'affluence — mille personnes
    /// qui reviennent toutes les quelques secondes. Elle ne touche donc NI la
    /// base NI le disque : tout se joue dans un dictionnaire en mémoire. Une
    /// seule lecture en base ici, et le guichet deviendrait la cause de
    /// l'embouteillage qu'il gère.
    /// </summary>
    [ApiController]
    [Route("affluence")]
    [HorsSalleDAttente]
    public class AffluenceController : ControllerBase
    {
        private readonly ISalleDAttente _salle;
        private readonly ReglageAffluence _reglage;

        public AffluenceController(ISalleDAttente salle, ReglageAffluence reglage)
        {
            _salle = salle ?? throw new ArgumentNullException(nameof(salle));
            _reglage = reglage ?? throw new ArgumentNullException(nameof(reglage));
        }

        /// <summary>
        /// Demande à entrer, ou renouvelle une place déjà obtenue.
        ///
        /// SALLE ÉTEINTE : on répond « admis », sans billet et sans rien
        /// compter. Le site n'a alors aucune raison de savoir que cette
        /// mécanique existe, et le jour où Camara l'allume, rien ne change
        /// dans le code du site — seulement la réponse d'ici.
        /// </summary>
        [HttpGet("billet")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Admis, ou rang dans la file.")]
        public IActionResult Billet([FromQuery] Guid? jeton)
        {
            if (!_reglage.Active)
                return Ok(new { salle = false, admis = true });

            var place = _salle.Demander(jeton ?? Guid.Empty, _reglage.Places);

            return Ok(new
            {
                salle = true,
                admis = place.Admis,
                billet = place.Jeton,
                rang = place.Rang,
                devant = place.Devant,
                attenteSecondes = place.AttenteSecondes,
                rappelDans = place.RappelDans,
            });
        }

        /// <summary>
        /// Rend sa place.
        ///
        /// APPELÉ QUAND L'ONGLET SE FERME, au mieux : le navigateur ne le
        /// garantit pas. La place se libère de toute façon toute seule après
        /// quelques secondes de silence — ceci ne fait que l'accélérer, et
        /// c'est déjà beaucoup un jour d'affluence, où chaque place rendue
        /// plus tôt fait avancer toute la file.
        /// </summary>
        [HttpDelete("billet")]
        [AllowAnonymous]
        [SwaggerResponse(204, "Place rendue.")]
        public IActionResult Rendre([FromQuery] Guid jeton)
        {
            _salle.Liberer(jeton);
            return NoContent();
        }

        /// <summary>
        /// L'état de la salle, pour l'administration : combien dedans, combien
        /// dehors, à quel rythme la file avance.
        /// </summary>
        [HttpGet("etat")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Occupation et file d'attente.")]
        public IActionResult Etat()
        {
            var etat = _salle.Etat(_reglage.Places);
            return Ok(new
            {
                active = _reglage.Active,
                places = etat.Places,
                occupees = etat.Occupees,
                enAttente = etat.EnAttente,
                debitParMinute = etat.DebitParMinute,
            });
        }

        /// <summary>
        /// Change le nombre de places.
        ///
        /// SUPER-ADMINISTRATEUR, comme les autres modes : ce nombre décide qui
        /// entre sur le site et qui attend dehors. Ce n'est pas un réglage de
        /// confort qu'on délègue avec l'accès au tableau de bord.
        ///
        /// ON NE VIDE PAS LA SALLE EN CHANGEANT LE NOMBRE. Baisser le plafond
        /// ne met personne dehors : ceux qui sont entrés finissent leur séance,
        /// et le plafond se fait sentir à mesure qu'ils partent. Expulser des
        /// parents en pleine séance pour respecter un chiffre serait obtenir la
        /// panne qu'on voulait éviter, en l'ayant provoquée soi-même.
        /// </summary>
        [HttpPut("places")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Plafond enregistré.")]
        [SwaggerResponse(400, "Nombre hors bornes.")]
        public async Task<IActionResult> DefinirPlaces(
            [FromBody] DefinirPlacesRequest requete,
            [FromServices] SchoolWebApp.Domain.Repositories.IReglageRepository reglages,
            CancellationToken ct)
        {
            var places = requete?.Places ?? 0;

            // LES BORNES SONT DES GARDE-FOUS CONTRE LA FAUTE DE FRAPPE, pas
            // une opinion sur la capacité de la machine. Un zéro fermerait le
            // site à tout le monde sans que personne comprenne pourquoi ; un
            // chiffre à six zéros rendrait la salle décorative.
            if (places < 1 || places > 100_000)
                return BadRequest(new { message = "Entre 1 et 100 000 places." });

            await reglages.EcrireAsync(ReglageAffluence.ClePlaces, places.ToString(), ct);
            _reglage.Oublier();

            return NoContent();
        }

        public class DefinirPlacesRequest
        {
            public int? Places { get; set; }
        }
    }
}
