using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Api.ViewModels;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Profils enfants du parent authentifié.
    /// Toutes les routes sont implicitement filtrées par le parent porteur du JWT :
    /// aucune ne prend de ParentId en paramètre.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("eleves")]
    public class ElevesController : Controller
    {
        private readonly IEleveViewModelBuilder _eleveBuilder;
        private readonly ILogger<ElevesController> _logger;

        public ElevesController(IEleveViewModelBuilder eleveBuilder, ILogger<ElevesController> logger)
        {
            _eleveBuilder = eleveBuilder ?? throw new ArgumentNullException(nameof(eleveBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Fiche détaillée d'un enfant, pour son parent.
        ///
        /// Même contenu que la fiche d'administration, mais la propriété est
        /// vérifiée AVANT toute lecture : un parent ne peut demander que la
        /// fiche d'un enfant rattaché à son compte.
        /// </summary>
        [HttpGet("{id:int}/fiche")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Fiche de l'enfant.", typeof(FicheEleve))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetFiche(int id, [FromServices] IAdminService ficheService)
        {
            try
            {
                // Renvoie null si l'élève n'est pas rattaché au parent du jeton :
                // c'est cette vérification qui autorise la suite.
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var fiche = await ficheService.GetFicheEleveAsync(id);
                return fiche is null ? NotFound() : Ok(fiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la fiche de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Les évaluations d'un enfant, la plus récente d'abord.</summary>
        [HttpGet("{id:int}/evaluations")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les évaluations.", typeof(IEnumerable<EvaluationEleve>))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetEvaluations(
            int id, [FromServices] IEvaluationRepository evaluations)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return Ok(await evaluations.GetParEleveAsync(id, 20));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des evaluations de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Une tranche de l'historique des évaluations.
        ///
        /// Distincte de la route ci-dessus, qui rend une liste plate et sert à
        /// trois écrans de l'élève : y greffer une enveloppe paginée aurait
        /// changé la forme de la réponse sous leurs pieds.
        /// </summary>
        [HttpGet("{id:int}/evaluations/historique")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Une tranche d'évaluations.", typeof(PageHistorique<EvaluationEleve>))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        /// <param name="matiereId">Null = toutes les matières.</param>
        /// <param name="ancien">Vrai = du plus ancien au plus récent.</param>
        public async Task<IActionResult> GetHistoriqueEvaluations(
            int id,
            [FromQuery] string? curseur,
            [FromQuery] int taille,
            [FromQuery] int? matiereId,
            [FromQuery] bool ancien,
            [FromServices] IEvaluationRepository evaluations)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return Ok(await evaluations.GetHistoriqueAsync(
                    id, curseur, Borner(taille), matiereId, ancien));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de l'historique des evaluations de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Les fiches de révision d'un enfant dans une matière.
        ///
        /// Sans `matiereId`, renvoie plutôt le NOMBRE de fiches par matière :
        /// c'est ce dont la grille des matières a besoin pour son compteur, et
        /// charger toutes les fiches de toutes les matières pour afficher un
        /// chiffre serait disproportionné.
        /// </summary>
        [HttpGet("{id:int}/fiches")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les fiches, ou leur nombre par matière.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetFiches(
            int id,
            [FromQuery] int? matiereId,
            [FromServices] IFicheRepository fiches)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                if (matiereId is null) return Ok(await fiches.CompterParMatiereAsync(id));

                return Ok(await fiches.GetParMatiereAsync(id, matiereId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des fiches de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Une fiche de révision complète.</summary>
        [HttpGet("{id:int}/fiches/{ficheId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La fiche.", typeof(FicheRevisionEleve))]
        [SwaggerResponse(404, "Fiche inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetFiche(
            int id, int ficheId, [FromServices] IFicheRepository fiches)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var fiche = await fiches.GetDetailAsync(ficheId, id);
                return fiche is null ? NotFound() : Ok(fiche);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de la fiche {FicheId} de l'eleve {EleveId}.", ficheId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// L'élève vient d'ouvrir la fiche : la pastille « à consulter »
        /// s'éteint. Appelé par la page de lecture, pas par la liste.
        /// </summary>
        [HttpPost("{id:int}/fiches/{ficheId:int}/vue")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "C'est noté.")]
        [SwaggerResponse(404, "Fiche inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> MarquerFicheVue(
            int id, int ficheId, [FromServices] IFicheRepository fiches)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return await fiches.MarquerVueAsync(ficheId, id) ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du marquage de la fiche {FicheId} de l'eleve {EleveId}.", ficheId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Les comptes rendus de séance d'un enfant, le plus récent d'abord.</summary>
        [HttpGet("{id:int}/rapports")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les rapports.", typeof(IEnumerable<RapportEleve>))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetRapports(
            int id, [FromServices] IRapportRepository rapports)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return Ok(await rapports.GetParEleveAsync(id, 20));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des rapports de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Une tranche de l'historique des comptes rendus.</summary>
        [HttpGet("{id:int}/rapports/historique")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Une tranche de comptes rendus.", typeof(PageHistorique<RapportEleve>))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        /// <param name="matiereId">Null = toutes les matières.</param>
        /// <param name="ancien">Vrai = du plus ancien au plus récent.</param>
        public async Task<IActionResult> GetHistoriqueRapports(
            int id,
            [FromQuery] string? curseur,
            [FromQuery] int taille,
            [FromQuery] int? matiereId,
            [FromQuery] bool ancien,
            [FromServices] IRapportRepository rapports)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return Ok(await rapports.GetHistoriqueAsync(
                    id, curseur, Borner(taille), matiereId, ancien));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de l'historique des rapports de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// La taille de tranche demandée, ramenée dans le raisonnable.
        ///
        /// Bornée et non validée : `taille=100000` n'est pas une faute de
        /// l'appelant à lui renvoyer en 400, c'est une requête qui ferait
        /// remonter tout l'historique d'un élève en une fois. On sert la
        /// tranche maximale et le curseur fait le reste. Zéro — le cas du
        /// paramètre absent — retombe sur la taille par défaut.
        /// </summary>
        private static int Borner(int taille) =>
            taille <= 0 ? 10 : Math.Min(taille, 50);

        /// <summary>Le détail d'un compte rendu de séance.</summary>
        [HttpGet("{id:int}/rapports/{rapportId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Le rapport.", typeof(RapportEleve))]
        [SwaggerResponse(404, "Rapport inexistant, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetRapport(
            int id, int rapportId, [FromServices] IRapportRepository rapports)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var rapport = await rapports.GetDetailAsync(rapportId, id);
                return rapport is null ? NotFound() : Ok(rapport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement du rapport {RapportId} de l'eleve {EleveId}.",
                    rapportId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// La copie d'une évaluation : les questions, ce que l'enfant a
        /// répondu, le verdict de chacune et la remarque du professeur.
        ///
        /// Sert à l'élève qui télécharge son contrôle comme au parent qui le
        /// consulte depuis la fiche — c'est le même document.
        /// </summary>
        [HttpGet("{id:int}/evaluations/{evaluationId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La copie.", typeof(EvaluationEleve))]
        [SwaggerResponse(404, "Évaluation inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetCopie(
            int id, int evaluationId, [FromServices] IEvaluationRepository evaluations)
        {
            try
            {
                // Deux gardes : l'enfant appartient bien au parent du jeton,
                // et l'évaluation appartient bien à cet enfant.
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var copie = await evaluations.GetCopieAsync(evaluationId, id);
                return copie is null ? NotFound() : Ok(copie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de l'evaluation {EvaluationId} de l'eleve {EleveId}.",
                    evaluationId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Les matières qui figurent à l emploi du temps de cet élève.
        ///
        /// C EST LE SERVEUR QUI DÉCIDE, PAS LA GRILLE. La règle — bornes de
        /// niveau et exclusions de voie — vivait aussi en JavaScript, et la
        /// copie s ouvrait en grand quand le niveau de l élève n était pas
        /// encore chargé. Un enfant de sixième s est vu proposer la
        /// philosophie, et rien derrière ne l a empêché de l ouvrir.
        /// </summary>
        [HttpGet("{id:int}/matieres")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les matières de cet élève.", typeof(IEnumerable<MatiereViewModel>))]
        [SwaggerResponse(404, "Élève inexistant ou n appartenant pas au parent.")]
        public async Task<IActionResult> GetMatieresDeLEleve(int id)
        {
            try
            {
                var matieres = await _eleveBuilder.GetMatieresDeLEleveAsync(id);
                return matieres is null ? NotFound() : Ok(matieres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des matieres de l eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet]
        [SwaggerResponse(200, "Les profils enfants du parent authentifié.", typeof(IEnumerable<EleveViewModel>))]
        [SwaggerResponse(401, "Jeton absent ou invalide.")]
        [SwaggerResponse(500, "Erreur interne.")]
        public async Task<IActionResult> GetEleves()
        {
            try
            {
                return Ok(await _eleveBuilder.GetElevesAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation des eleves.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("{id:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "L'élève demandé.", typeof(EleveViewModel))]
        [SwaggerResponse(404, "Élève inexistant ou n'appartenant pas au parent authentifié.")]
        [SwaggerResponse(401, "Jeton absent ou invalide.")]
        public async Task<IActionResult> GetEleveById(int id)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                return eleve is null ? NotFound() : Ok(eleve);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recuperation de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpPost]
        [SwaggerResponse(201, "Profil créé.", typeof(EleveViewModel))]
        [SwaggerResponse(400, "Requête invalide ou niveau scolaire inexistant.")]
        [SwaggerResponse(401, "Jeton absent ou invalide.")]
        [SwaggerResponse(409, "La formule ne permet pas d'enregistrer un enfant de plus.")]
        public async Task<IActionResult> AddEleve(
            [FromBody] EleveRequest model,
            [FromServices] IAbonnementRepository abonnements,
            [FromServices] IChatContexteResolver resolver)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Contrôle AVANT création : un profil créé puis refusé à
                // l'usage laisserait un enfant visible mais inutilisable, ce
                // qui se lit comme une panne et pas comme une limite d'offre.
                var parent = await resolver.ResoudreParentAsync();
                var capacite = await abonnements.CapaciteAsync(parent.Id);

                if (!capacite.PeutAjouter)
                {
                    _logger.LogInformation(
                        "Ajout d'enfant refuse pour le parent {ParentId} : {Actuels}/{Maximum}.",
                        parent.Id, capacite.Actuels, capacite.Maximum);

                    return Conflict(new
                    {
                        message = capacite.OffreLibelle is null
                            ? "Choisissez une formule pour enregistrer d'autres enfants."
                            : $"Votre formule {capacite.OffreLibelle} couvre {capacite.Maximum} enfant"
                              + $"{(capacite.Maximum > 1 ? "s" : "")}. Changez de formule pour en ajouter un de plus.",
                        actuels = capacite.Actuels,
                        maximum = capacite.Maximum,
                    });
                }

                var eleve = await _eleveBuilder.AddEleveAsync(model);
                if (eleve is null)
                {
                    return BadRequest(new { message = "Le niveau scolaire indiqué n'existe pas." });
                }

                return CreatedAtAction(nameof(GetEleveById), new { id = eleve.Id }, eleve);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la creation d'un eleve.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpPut]
        [SwaggerResponse(200, "Profil mis à jour.", typeof(EleveViewModel))]
        [SwaggerResponse(404, "Élève inexistant ou n'appartenant pas au parent authentifié.")]
        [SwaggerResponse(400, "Requête invalide.")]
        public async Task<IActionResult> UpdateEleve([FromBody] EleveRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (model.Id <= 0) return BadRequest(new { message = "L'identifiant de l'élève est obligatoire." });

            try
            {
                var eleve = await _eleveBuilder.UpdateEleveAsync(model);
                return eleve is null ? NotFound() : Ok(eleve);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise a jour de l'eleve {EleveId}.", model.Id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Le code d'accès d'un enfant, tel que le parent doit pouvoir le lire.
        ///
        /// Il est CRÉÉ au premier appel s'il n'existe pas. Un enfant n'a pas de
        /// code tant que personne ne l'a regardé — inutile d'en semer dans la
        /// base pour des profils dont personne ne se servira.
        ///
        /// Réservé au parent : le filtre des sessions enfants ferme cette route
        /// par défaut, faute d'attribut `[AutoriseEleve]`. Un enfant ne doit pas
        /// pouvoir lire le code de son frère.
        /// </summary>
        [HttpGet("{id:int}/code")]
        [SwaggerResponse(200, "Le code d'accès de l'enfant.")]
        [SwaggerResponse(404, "Élève inexistant ou n'appartenant pas au parent authentifié.")]
        public async Task<IActionResult> GetCode(
            int id,
            [FromServices] IEleveService eleves)
        {
            var vue = await _eleveBuilder.GetEleveByIdAsync(id);
            if (vue is null) return NotFound();

            // Le contrôle d'appartenance est fait ci-dessus par le builder ;
            // on relit ensuite le modèle, seul à porter le code.
            var eleve = await eleves.GetEleveByIdAsync(id);
            if (eleve is null) return NotFound();

            var code = eleve.CodeAcces;

            if (string.IsNullOrEmpty(code))
            {
                code = await eleves.AttribuerCodeAsync(id, HttpContext.RequestAborted);
                if (code is null) return StatusCode(500, new { message = "Code impossible à créer." });
            }

            return Ok(new
            {
                code = Domain.Services.CodeAccesEleve.Presenter(code),
                suspendu = eleve.AccesSuspenduLe is not null,
                depuis = eleve.AccesSuspenduLe,
            });
        }

        /// <summary>
        /// Régénère le code : l'ancien cesse de marcher immédiatement.
        ///
        /// TOUTES LES SESSIONS OUVERTES SONT FERMÉES AVEC.
        ///
        /// Sans ça, un parent qui régénère le code parce que son enfant l'a
        /// écrit au tableau de la classe croirait avoir coupé quelque chose :
        /// les appareils déjà connectés, eux, continueraient de travailler.
        /// </summary>
        [HttpPost("{id:int}/code")]
        [SwaggerResponse(200, "Nouveau code.")]
        public async Task<IActionResult> RegenererCode(
            int id,
            [FromServices] IEleveService eleves,
            [FromServices] ISessionEleveRepository sessions)
        {
            var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
            if (eleve is null) return NotFound();

            var code = await eleves.AttribuerCodeAsync(id, HttpContext.RequestAborted);
            if (code is null) return StatusCode(500, new { message = "Code impossible à créer." });

            var fermees = await sessions.FermerToutesAsync(id, HttpContext.RequestAborted);

            _logger.LogInformation(
                "Code regenere pour l'eleve {Id} ({Fermees} session(s) fermee(s)).", id, fermees);

            return Ok(new { code = Domain.Services.CodeAccesEleve.Presenter(code), sessionsFermees = fermees });
        }

        /// <summary>
        /// Coupe ou rouvre l'accès d'un enfant, sans toucher à son code.
        ///
        /// C'est le geste du soir sans écran : l'enfant garde son code, il ne
        /// peut simplement plus s'en servir. Rouvrir ne demande rien d'autre
        /// que de rebasculer l'interrupteur.
        /// </summary>
        [HttpPut("{id:int}/acces")]
        [SwaggerResponse(204, "Accès mis à jour.")]
        public async Task<IActionResult> ChangerAcces(
            int id,
            [FromBody] AccesRequest requete,
            [FromServices] IEleveService eleves,
            [FromServices] ISessionEleveRepository sessions)
        {
            var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
            if (eleve is null) return NotFound();

            await eleves.SuspendreAccesAsync(id, requete.Suspendu, HttpContext.RequestAborted);

            // Couper l'accès sans fermer les sessions ouvertes ne couperait
            // rien du tout : l'appareil déjà connecté continuerait.
            if (requete.Suspendu)
            {
                await sessions.FermerToutesAsync(id, HttpContext.RequestAborted);
            }

            return NoContent();
        }

        public class AccesRequest
        {
            public bool Suspendu { get; set; }
        }

        /// <summary>
        /// Retire un profil : il quitte les listes et LIBÈRE SA PLACE dans la
        /// formule, sans que rien ne soit effacé.
        ///
        /// C'est le geste courant — un parent qui bascule d'un enfant à
        /// l'autre — et il se défait. L'effacement réel est ailleurs.
        /// </summary>
        [HttpPost("{id:int}/archiver")]
        [SwaggerResponse(204, "Profil retiré.")]
        [SwaggerResponse(404, "Élève inexistant ou n'appartenant pas au parent authentifié.")]
        public async Task<IActionResult> ArchiverEleve(int id)
        {
            try
            {
                var fait = await _eleveBuilder.ArchiverEleveAsync(id);
                return fait ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du retrait de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Les profils retirés, ceux qu'on peut encore remettre.</summary>
        [HttpGet("archives")]
        [SwaggerResponse(200, "Les profils retirés.", typeof(IEnumerable<EleveViewModel>))]
        public async Task<IActionResult> GetArchives()
        {
            try
            {
                return Ok(await _eleveBuilder.GetArchivesAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des profils retires.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpPost("{id:int}/restaurer")]
        [SwaggerResponse(204, "Profil remis parmi les enfants actifs.")]
        [SwaggerResponse(404, "Profil inexistant, non retiré, ou déjà effacé.")]
        public async Task<IActionResult> RestaurerEleve(int id)
        {
            try
            {
                var fait = await _eleveBuilder.RestaurerEleveAsync(id);
                return fait ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la restauration de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Efface les données de l'enfant : identité, conversations,
        /// évaluations, comptes rendus, fiches. Définitif.
        ///
        /// La ligne subsiste, vidée de tout ce qui est personnel, parce que le
        /// registre de consommation pointe dessus — sans elle, supprimer un
        /// enfant rendrait à la famille les heures déjà utilisées.
        ///
        /// Reste un DELETE : c'est bien une suppression du point de vue du
        /// parent, et le verbe HTTP décrit son intention, pas notre plomberie.
        /// </summary>
        [HttpDelete("{id:int}")]
        [SwaggerResponse(204, "Données effacées.")]
        [SwaggerResponse(404, "Élève inexistant ou n'appartenant pas au parent authentifié.")]
        public async Task<IActionResult> DeleteEleve(int id)
        {
            try
            {
                var fait = await _eleveBuilder.AnonymiserEleveAsync(id);
                return fait ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'effacement des donnees de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }
    }
}
