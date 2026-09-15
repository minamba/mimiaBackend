using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Données de référence nécessaires à l'onboarding : niveaux et matières.
    /// Lecture seule — elles sont alimentées par le seed, pas par les utilisateurs.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("referentiel")]
    // Un enfant travaille ici : c'est tout l'objet de sa session.
    [SchoolWebApp.Api.Auth.AutoriseEleve]
    public class ReferentielController : Controller
    {
        private readonly IReferentielViewModelBuilder _referentielBuilder;
        private readonly ILogger<ReferentielController> _logger;

        public ReferentielController(
            IReferentielViewModelBuilder referentielBuilder,
            ILogger<ReferentielController> logger)
        {
            _referentielBuilder = referentielBuilder ?? throw new ArgumentNullException(nameof(referentielBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("niveaux")]
        [SwaggerResponse(200, "Les niveaux du CP à la Terminale, triés.", typeof(IEnumerable<NiveauScolaireViewModel>))]
        public async Task<IActionResult> GetNiveaux()
        {
            try
            {
                return Ok(await _referentielBuilder.GetNiveauxScolairesAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation des niveaux.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <param name="toutes">
        /// Faux par défaut : seules les matières ouvertes sont renvoyées.
        /// Vrai pour l'administration, qui doit voir aussi les matières à venir.
        /// </param>
        /// <summary>
        /// L équipe pédagogique — la seule route de ce contrôleur ouverte à
        /// tous, parce qu elle sert la page d accueil, qui est publique.
        ///
        /// Rien de sensible n en sort : des prénoms inventés, des visages
        /// dessinés et des noms de matières. C est exactement ce que le site
        /// affiche déjà à qui n a pas de compte.
        /// </summary>
        [HttpGet("equipe")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Les professeurs, un par visage.", typeof(IEnumerable<ProfesseurViewModel>))]
        public async Task<IActionResult> GetEquipe()
        {
            try
            {
                return Ok(await _referentielBuilder.GetEquipeAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation de l equipe.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// L'année scolaire en cours et le libellé du badge « Programmes
        /// officiels 2026-2027 » — PUBLIC, parce que la page d'accueil
        /// l'affiche à côté de son titre. Calculé à chaque appel : le 1er
        /// août prochain, le site passera à 2027-2028 tout seul. Voulu par
        /// Camara le 13/09/2026.
        ///
        /// `echeancesEnRetard` ne sort qu'à titre d'information pour
        /// l'administration ; le site n'en fait rien, et c'est voulu — on
        /// n'affiche pas aux familles un retard interne de vérification.
        /// </summary>
        [HttpGet("annee-scolaire")]
        [AllowAnonymous]
        [SwaggerResponse(200, "L'année scolaire en cours et le libellé des programmes.")]
        public async Task<IActionResult> GetAnneeScolaire(
            [FromServices] Domain.Repositories.IEcheanceReferentielRepository echeances,
            CancellationToken ct)
        {
            var maintenant = DateTime.UtcNow;

            var enRetard = 0;
            try { enRetard = await echeances.CompterEnRetardAsync(maintenant, ct); }
            catch (Exception ex)
            {
                // Le badge ne dépend pas de la base : une panne ici ne doit
                // pas priver la page d'accueil de son année.
                _logger.LogWarning(ex, "Impossible de compter les echeances en retard.");
            }

            return Ok(new
            {
                anneeScolaire = Domain.Models.AnneeScolaire.Courante(maintenant),
                libelle = Domain.Models.AnneeScolaire.LibelleProgrammes(maintenant),
                echeancesEnRetard = enRetard,
            });
        }

        /// <summary>
        /// Les 30 académies françaises, chacune avec sa zone de vacances
        /// scolaires. Sert le sélecteur du formulaire de création d'un
        /// profil enfant — voir aussi « Mon calendrier ».
        /// </summary>
        [HttpGet("academies")]
        [SwaggerResponse(200, "Les académies, triées.", typeof(IEnumerable<AcademieViewModel>))]
        public async Task<IActionResult> GetAcademies()
        {
            try
            {
                return Ok(await _referentielBuilder.GetAcademiesAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation des academies.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("matieres")]
        [SwaggerResponse(200, "Les matières disponibles.", typeof(IEnumerable<MatiereViewModel>))]
        public async Task<IActionResult> GetMatieres([FromQuery] bool toutes = false)
        {
            try
            {
                return Ok(await _referentielBuilder.GetMatieresAsync(activesSeulement: !toutes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation des matieres.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }
    }
}
