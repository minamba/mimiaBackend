using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Services;
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
        /// <summary>
        /// La carte des compétences de l enfant.
        ///
        /// UNE TRACE, PAS UN CHEMIN IMPOSÉ. Le professeur laisse l élève
        /// choisir son sujet — c est une règle écrite en majuscules dans son
        /// prompt. Cette carte montre ce qui est acquis derrière lui, elle ne
        /// verrouille rien devant.
        ///
        /// LA VISITE DU PARENT NE CONSOMME PAS LA RÉCOMPENSE. Les compétences
        /// gagnées depuis la dernière fois ne sont marquées comme vues que
        /// lorsque c est l ENFANT qui ouvre l écran. Sans cette distinction, un
        /// parent curieux effacerait la seule surprise du produit.
        /// </summary>
        [HttpGet("{id:int}/progression")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La carte des compétences.")]
        [SwaggerResponse(404, "Profil inexistant ou n appartenant pas à ce compte.")]
        public async Task<IActionResult> GetProgression(
            int id,
            [FromServices] IMaitriseRepository maitrises,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] SchoolWebApp.Api.Utils.ICurrentUserAccessor utilisateur,
            CancellationToken ct)
        {
            // La même garde que les autres routes enfant : rend null si le
            // profil n appartient pas au parent du jeton, ou si un enfant
            // demande la fiche d un autre.
            var eleve = await resolveur.ResoudreEleveAsync(id);
            if (eleve is null) return NotFound();

            var progression = await maitrises.GetProgressionAsync(
                id, utilisateur.EleveId is not null, ct);

            return progression is null ? NotFound() : Ok(progression);
        }

        /// <summary>
        /// Le calendrier de l'élève : ses vacances, ses séances par matière,
        /// ses évaluations passées, et celles qui restent à venir.
        ///
        /// UNE ROUTE MINCE : la donnée la plus difficile — quel jour l'élève
        /// a eu cours, dans quelle matière — existe déjà via
        /// <see cref="IRapportRepository.GetEntreAsync"/>, écrite à chaque
        /// séance. Rien de neuf à construire là-dessus.
        /// </summary>
        [HttpGet("{id:int}/calendrier")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Le calendrier.", typeof(CalendrierEleveViewModel))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetCalendrier(
            int id,
            [FromQuery] int annee,
            [FromQuery] int mois,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IReferentielService referentiel,
            [FromServices] IRapportRepository rapports,
            [FromServices] IEvaluationRepository evaluations,
            [FromServices] IControleScolaireRepository controles,
            CancellationToken ct)
        {
            if (mois < 1 || mois > 12) return BadRequest();

            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                var debutMois = new DateTime(annee, mois, 1, 0, 0, 0, DateTimeKind.Utc);
                var finMoisExclusif = debutMois.AddMonths(1);

                var vacances = await referentiel.GetPeriodesVacancesAsync(eleve.Zone, debutMois, finMoisExclusif);

                // Pas bornée au mois affiché, à dessein : le décompte sous
                // « Retour » doit rester juste qu'on regarde le mois en
                // cours ou celui d'après.
                var prochaineVacances = await referentiel.GetProchainePeriodeVacancesAsync(eleve.Zone, DateTime.UtcNow.Date);

                var seances = await rapports.GetEntreAsync(id, debutMois, finMoisExclusif, ct);
                var evaluationsDuMois = await evaluations.GetEntreAsync(id, debutMois, finMoisExclusif, ct);
                var controlesDuMois = await controles.GetEntreAsync(id, debutMois, finMoisExclusif, ct);

                return Ok(new CalendrierEleveViewModel
                {
                    Zone = eleve.Zone,
                    Vacances = vacances.Select(v => new PeriodeVacancesViewModel
                    {
                        Libelle = v.Libelle,
                        DateDebut = v.DateDebut,
                        DateFin = v.DateFin,
                    }),
                    ProchaineVacances = prochaineVacances is null ? null : new PeriodeVacancesViewModel
                    {
                        Libelle = prochaineVacances.Libelle,
                        DateDebut = prochaineVacances.DateDebut,
                        DateFin = prochaineVacances.DateFin,
                    },
                    Seances = seances.Select(s => new SeanceJourViewModel
                    {
                        Date = s.DateCreation,
                        MatiereId = s.MatiereId,
                        MatiereLibelle = s.MatiereLibelle,
                        ProfCouleur = s.ProfCouleur,
                        DureeChoisieMinutes = s.DureeChoisieMinutes,
                    }),
                    Evaluations = evaluationsDuMois.Select(e => new EvaluationJourViewModel
                    {
                        Date = e.DateCreation,
                        MatiereId = e.MatiereId,
                        MatiereLibelle = e.MatiereLibelle,
                        Note = e.Note,
                    }),
                    Controles = controlesDuMois.Select(c => new ControleViewModel
                    {
                        Id = c.Id,
                        MatiereId = c.MatiereId,
                        MatiereLibelle = c.MatiereLibelle,
                        ProfCouleur = c.ProfCouleur,
                        Sujet = c.Sujet,
                        DateControle = c.DateControle,
                        HeureControle = c.HeureControle,
                    }),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du calendrier de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Pose un contrôle depuis le calendrier — par le parent ou l'enfant,
        /// hors séance.
        ///
        /// MATIÈRE VALIDÉE CONTRE LE RÉFÉRENTIEL DE L'ÉLÈVE, jamais reçue en
        /// confiance : même garde que côté prompt, où la matière vient
        /// toujours de la conversation, jamais du texte libre.
        /// </summary>
        [HttpPost("{id:int}/controles")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Le contrôle enregistré.", typeof(ControleViewModel))]
        [SwaggerResponse(400, "Matière absente de l'emploi du temps de l'élève, ou date invalide.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> CreerControle(
            int id,
            [FromBody] SchoolWebApp.Api.Request.CreerControleRequest request,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IControleScolaireRepository controles,
            [FromServices] IPlanificateurControleService planificateur,
            [FromServices] SchoolWebApp.Api.Utils.ICurrentUserAccessor utilisateur,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                var matieres = await _eleveBuilder.GetMatieresDeLEleveAsync(id);
                if (matieres is null) return NotFound();

                if (!matieres.Any(m => m.Id == request.MatiereId)) return BadRequest();

                var posePar = utilisateur.EleveId is not null ? "ELEVE" : "PARENT";

                var enregistre = await controles.EnregistrerDepuisCalendrierAsync(
                    id, request.MatiereId, posePar, request.Sujet,
                    request.DateControle, request.HeureControle, ct);

                if (enregistre is null) return BadRequest();

                // LE PROGRAMME SE DÉDUIT DU SUJET, TOUT DE SUITE.
                //
                // Un contrôle posé depuis le calendrier n'a vu aucun
                // professeur : sans ça, sa barre restait à zéro et « on ne
                // sait pas ce qu'il y a dessus » jusqu'à une hypothétique
                // séance de préparation. Or le programme de la classe est
                // connu — « Thalès », ce n'est pas un milliard de notions.
                //
                // Silencieux en cas d'échec : un contrôle sans programme
                // reste utilisable, et le professeur le complétera. On ne
                // refuse jamais la création pour ça.
                try
                {
                    await planificateur.PoserProgrammeAsync(
                        id, enregistre.Id, request.MatiereId, eleve.NiveauScolaireId,
                        request.Sujet, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Echec de la deduction du programme du controle {ControleId}.", enregistre.Id);
                }

                return Ok(new ControleViewModel
                {
                    MatiereId = enregistre.MatiereId,
                    MatiereLibelle = enregistre.MatiereLibelle,
                    ProfCouleur = enregistre.ProfCouleur,
                    Sujet = enregistre.Sujet,
                    DateControle = enregistre.DateControle,
                    HeureControle = enregistre.HeureControle,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la creation d'un controle pour l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Les contrôles de l'élève, à venir ou passés, avec l'état de leur
        /// préparation.
        ///
        /// LE DÉLAI EST CALCULÉ ICI, en heure de Paris : le navigateur ne
        /// recalcule jamais « dans 3 jours » lui-même, sans quoi deux horloges
        /// donneraient deux réponses.
        /// </summary>
        [HttpGet("{id:int}/controles")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les contrôles.", typeof(IEnumerable<ControleEleveViewModel>))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetControles(
            int id,
            [FromQuery] string statut,
            [FromQuery] int limite,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IControleScolaireRepository controles,
            CancellationToken ct)
        {
            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                var maintenant = HeureFrance.Locale(DateTime.UtcNow);
                var plafond = limite is > 0 and <= 100 ? limite : 20;

                var liste = string.Equals(statut, "passes", StringComparison.OrdinalIgnoreCase)
                    ? await controles.GetPassesAsync(id, maintenant, plafond, ct)
                    : await controles.GetAVenirAsync(id, maintenant, plafond, ct);

                var lignes = liste.ToList();

                // Une seule requête de préparation pour toute la liste : une
                // par contrôle serait un N+1 sur l'écran d'accueil.
                var preparations = await controles.GetPreparationsAsync(
                    id, lignes.Select(c => c.Id), ct);

                return Ok(lignes.Select(c => Projeter(c, maintenant, preparations)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des controles de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("{id:int}/controles/{controleId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La fiche du contrôle.", typeof(ControleEleveViewModel))]
        [SwaggerResponse(404, "Contrôle inexistant ou n'appartenant pas à cet élève.")]
        public async Task<IActionResult> GetControle(
            int id,
            int controleId,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IControleScolaireRepository controles,
            CancellationToken ct)
        {
            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                var controle = await controles.GetAsync(id, controleId, ct);

                // 404 plutôt que 403 : ne pas révéler qu'un contrôle existe
                // chez quelqu'un d'autre.
                if (controle is null) return NotFound();

                var maintenant = HeureFrance.Locale(DateTime.UtcNow);
                var preparations = await controles.GetPreparationsAsync(id, [controleId], ct);

                return Ok(Projeter(controle, maintenant, preparations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du controle {ControleId}.", controleId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// LA PRÉPARATION À L'EXAMEN DE L'ÉLÈVE — la section sous « Mes
        /// contrôles ». `examen` vaut null quand sa classe n'a pas d'examen
        /// cette année : un objet enveloppe plutôt qu'un 204, pour que le
        /// navigateur n'ait pas à distinguer « rien » d'« échec ».
        /// </summary>
        [HttpGet("{id:int}/examen")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La préparation à l'examen, ou null.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetExamen(
            int id,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IExamenRepository examens,
            CancellationToken ct)
        {
            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                var preparation = await examens.GetPreparationExamenAsync(id, eleve.NiveauCode, ct);
                return Ok(new { examen = preparation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de l'examen de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("{id:int}/examen/epreuves/{code}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "L'épreuve, avec ses notions.")]
        [SwaggerResponse(404, "Épreuve inexistante ou ne concernant pas cet élève.")]
        public async Task<IActionResult> GetEpreuve(
            int id,
            string code,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IExamenRepository examens,
            CancellationToken ct)
        {
            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                var epreuve = await examens.GetPreparationEpreuveAsync(id, eleve.NiveauCode, code, ct);
                return epreuve is null ? NotFound() : Ok(epreuve);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de l'epreuve {Code}.", code);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Modifie un contrôle. CHANGER DE MATIÈRE VIDE SON PÉRIMÈTRE — des
        /// notions de français n'ont aucun sens dans un contrôle de maths.
        /// </summary>
        [HttpPut("{id:int}/controles/{controleId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Le contrôle modifié.", typeof(ControleViewModel))]
        [SwaggerResponse(400, "Matière absente de l'emploi du temps de l'élève, ou date invalide.")]
        [SwaggerResponse(404, "Contrôle inexistant ou n'appartenant pas à cet élève.")]
        public async Task<IActionResult> ModifierControle(
            int id,
            int controleId,
            [FromBody] SchoolWebApp.Api.Request.ModifierControleRequest request,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IControleScolaireRepository controles,
            [FromServices] IPlanificateurControleService planificateur,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                // Ce qu'il était AVANT : c'est la comparaison qui dit s'il
                // faut redéduire le programme. Relancer un appel au modèle
                // parce que l'heure a bougé de dix minutes serait payé pour
                // rien.
                var avant = await controles.GetAsync(id, controleId, ct);
                if (avant is null) return NotFound();

                // LA MATIÈRE N'EST PAS MODIFIABLE : celle de la requête est
                // ignorée, et c'est celle du contrôle qui vaut. Un contrôle
                // déplacé de matière perdrait son programme et laisserait ses
                // séances de préparation rattachées au mauvais professeur.
                var modifie = await controles.ModifierAsync(
                    id, controleId, request.Sujet,
                    request.DateControle, request.HeureControle, ct);

                if (modifie is null) return NotFound();

                // LE PROGRAMME SE REFAIT QUAND LE SUJET CHANGE.
                //
                // C'est le cas courant : l'enfant crée « contrôle de maths »
                // sans savoir, puis revient compléter quand son professeur a
                // annoncé le chapitre. Sans ça, sa fiche resterait vide alors
                // même qu'il vient de dire ce qu'il y avait dessus.
                if (!string.Equals(modifie.Sujet, avant.Sujet, StringComparison.Ordinal))
                {
                    try
                    {
                        await planificateur.PoserProgrammeAsync(
                            id, controleId, modifie.MatiereId, eleve.NiveauScolaireId,
                            request.Sujet, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Echec de la rededuction du programme du controle {ControleId}.", controleId);
                    }
                }

                return Ok(new ControleViewModel
                {
                    Id = modifie.Id,
                    MatiereId = modifie.MatiereId,
                    MatiereLibelle = modifie.MatiereLibelle,
                    ProfCouleur = modifie.ProfCouleur,
                    Sujet = modifie.Sujet,
                    DateControle = modifie.DateControle,
                    HeureControle = modifie.HeureControle,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du controle {ControleId}.", controleId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("{id:int}/controles/{controleId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "Contrôle supprimé.")]
        [SwaggerResponse(404, "Contrôle inexistant ou n'appartenant pas à cet élève.")]
        public async Task<IActionResult> SupprimerControle(
            int id,
            int controleId,
            [FromServices] IChatContexteResolver resolveur,
            [FromServices] IControleScolaireRepository controles,
            CancellationToken ct)
        {
            try
            {
                var eleve = await resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                return await controles.SupprimerAsync(id, controleId, ct)
                    ? NoContent()
                    : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du controle {ControleId}.", controleId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        private static ControleEleveViewModel Projeter(
            ControleScolaireEleve controle,
            DateTime maintenant,
            IReadOnlyDictionary<int, PreparationControle> preparations)
        {
            preparations.TryGetValue(controle.Id, out var preparation);

            return new ControleEleveViewModel
            {
                Id = controle.Id,
                MatiereId = controle.MatiereId,
                MatiereLibelle = controle.MatiereLibelle,
                ProfCouleur = controle.ProfCouleur,
                Sujet = controle.Sujet,
                DateControle = controle.DateControle,
                HeureControle = controle.HeureControle,
                JoursRestants = (controle.DateControle.Date - maintenant.Date).Days,
                DernierePreparationLe = controle.DernierePreparationLe,
                NombrePreparations = controle.NombrePreparations,
                Note = controle.Note,
                Ressenti = controle.Ressenti,
                BilanLe = controle.BilanLe,
                BilanClos = controle.BilanClos,
                Preparation = preparation is null ? null : new PreparationViewModel
                {
                    Pourcent = preparation.Pourcent,
                    PerimetreConnu = preparation.PerimetreConnu,
                    Total = preparation.Total,
                    Acquises = preparation.Acquises,
                    PretStatut = preparation.PretStatut,
                    PretObservation = preparation.PretObservation,
                    PretLe = preparation.PretLe,
                    Notions = preparation.Notions.Select(n => new NotionControleViewModel
                    {
                        Id = n.Id,
                        Libelle = n.Libelle,
                        Etat = n.Etat,
                        TravailleeLe = n.TravailleeLe,
                        Resultat = n.Resultat,
                        Pourcent = n.Pourcent,
                        ValideeParMesure = n.ValideeParMesure,
                    }),
                },
            };
        }

        [HttpGet("{id:int}/fiche")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Fiche de l'enfant.", typeof(FicheEleve))]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetFiche(int id, [FromServices] IAdminService ficheService, [FromQuery] int? niveau = null)
        {
            try
            {
                // Renvoie null si l'élève n'est pas rattaché au parent du jeton :
                // c'est cette vérification qui autorise la suite.
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                // PAS D'ANNÉE DEMANDÉE = SON ANNÉE EN COURS, et non toute sa
                // scolarité.
                //
                // Sans ça, la fiche s'ouvrirait sur l'onglet « en cours » tout
                // en affichant les données de toutes les années cumulées :
                // l'onglet dirait « seconde » et les chiffres compteraient aussi
                // la troisième. Un parent lirait un niveau qui n'existe pas.
                //
                // La route d'administration, elle, garde la vue globale : elle
                // n'a pas de sélecteur, et un aperçu réduit à l'année en cours
                // sans moyen d'en sortir cacherait le reste du dossier.
                var fiche = await ficheService.GetFicheEleveAsync(
                    id, niveau ?? eleve.NiveauScolaireId);
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

        /// <summary>
        /// Les dictées corrigées d'un enfant dans une matière.
        ///
        /// Sans `matiereId`, renvoie le NOMBRE de dictées par matière — même
        /// principe que <see cref="GetFiches"/>, pour la même raison.
        /// </summary>
        [HttpGet("{id:int}/dictees")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les dictées, ou leur nombre par matière.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetDictees(
            int id,
            [FromQuery] int? matiereId,
            [FromServices] IDicteeRepository dictees)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                if (matiereId is null) return Ok(await dictees.CompterParMatiereAsync(id));

                return Ok(await dictees.GetParMatiereAsync(id, matiereId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des dictees de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Une dictée corrigée complète.</summary>
        [HttpGet("{id:int}/dictees/{dicteeId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La dictée.", typeof(DicteeEleve))]
        [SwaggerResponse(404, "Dictee inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetDictee(
            int id, int dicteeId, [FromServices] IDicteeRepository dictees)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var dictee = await dictees.GetDetailAsync(dicteeId, id);
                return dictee is null ? NotFound() : Ok(dictee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de la dictee {DicteeId} de l'eleve {EleveId}.", dicteeId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// L'élève vient d'ouvrir la dictée : la pastille « à consulter »
        /// s'éteint.
        /// </summary>
        [HttpPost("{id:int}/dictees/{dicteeId:int}/vue")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "C'est noté.")]
        [SwaggerResponse(404, "Dictee inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> MarquerDicteeVue(
            int id, int dicteeId, [FromServices] IDicteeRepository dictees)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return await dictees.MarquerVueAsync(dicteeId, id) ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du marquage de la dictee {DicteeId} de l'eleve {EleveId}.", dicteeId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Les conversations d'expression orale d'un enfant dans une matière —
        /// voulu par Camara le 18/09/2026.
        ///
        /// CE N'EST PAS LA COMPRÉHENSION ORALE, juste en dessous : là-bas le
        /// professeur lit un passage et l'élève explique EN FRANÇAIS ce qu'il a
        /// compris ; ici les deux parlent DANS LA LANGUE du cours, et ce sont ces
        /// échanges qu'on relit.
        ///
        /// Sans `matiereId`, renvoie le NOMBRE par matière — même principe que
        /// les dictées.
        /// </summary>
        [HttpGet("{id:int}/expressions-orales")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les conversations, ou leur nombre par matière.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetExpressionsOrales(
            int id,
            [FromQuery] int? matiereId,
            [FromServices] IExpressionOraleRepository expressionsOrales)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                if (matiereId is null) return Ok(await expressionsOrales.CompterParMatiereAsync(id));

                return Ok(await expressionsOrales.GetParMatiereAsync(id, matiereId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement des expressions orales de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Une conversation complète, avec tous ses tours de parole.</summary>
        [HttpGet("{id:int}/expressions-orales/{expressionOraleId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La conversation.", typeof(ExpressionOraleEleve))]
        [SwaggerResponse(404, "Conversation inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetExpressionOrale(
            int id, int expressionOraleId,
            [FromServices] IExpressionOraleRepository expressionsOrales)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var conversation = await expressionsOrales.GetDetailAsync(expressionOraleId, id);
                return conversation is null ? NotFound() : Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de l'expression orale {ExpressionOraleId} "
                    + "de l'eleve {EleveId}.", expressionOraleId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// L'élève vient d'ouvrir la conversation : la pastille « à consulter »
        /// s'éteint.
        /// </summary>
        [HttpPost("{id:int}/expressions-orales/{expressionOraleId:int}/vue")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "C'est noté.")]
        [SwaggerResponse(404, "Conversation inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> MarquerExpressionOraleVue(
            int id, int expressionOraleId,
            [FromServices] IExpressionOraleRepository expressionsOrales)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return await expressionsOrales.MarquerVueAsync(expressionOraleId, id)
                    ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du marquage de l'expression orale {ExpressionOraleId} "
                    + "de l'eleve {EleveId}.", expressionOraleId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Les textes d'expression écrite d'un enfant dans une matière.
        ///
        /// LE TROISIÈME DE LA FAMILLE : la compréhension orale garde ce qu'il a
        /// ENTENDU, l'expression orale ce qu'il a DIT, celle-ci ce qu'il a ÉCRIT.
        /// C'est la seule où son orthographe se voit.
        ///
        /// Sans `matiereId`, renvoie le NOMBRE par matière.
        /// </summary>
        [HttpGet("{id:int}/expressions-ecrites")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les textes, ou leur nombre par matière.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetExpressionsEcrites(
            int id,
            [FromQuery] int? matiereId,
            [FromServices] IExpressionEcriteRepository expressionsEcrites)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                if (matiereId is null) return Ok(await expressionsEcrites.CompterParMatiereAsync(id));

                return Ok(await expressionsEcrites.GetParMatiereAsync(id, matiereId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement des expressions ecrites de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Un texte complet, avec sa correction.</summary>
        [HttpGet("{id:int}/expressions-ecrites/{expressionEcriteId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Le texte et sa correction.", typeof(ExpressionEcriteEleve))]
        [SwaggerResponse(404, "Texte inexistant, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetExpressionEcrite(
            int id, int expressionEcriteId,
            [FromServices] IExpressionEcriteRepository expressionsEcrites)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var texte = await expressionsEcrites.GetDetailAsync(expressionEcriteId, id);
                return texte is null ? NotFound() : Ok(texte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de l'expression ecrite {ExpressionEcriteId} "
                    + "de l'eleve {EleveId}.", expressionEcriteId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// LA PHOTO DE SON CAHIER, tant qu'elle n'a pas été recopiée.
        ///
        /// 404 QUAND ELLE A ÉTÉ PURGÉE, et c'est le cas normal d'un texte déjà
        /// transcrit : on ne garde pas indéfiniment l'écriture manuscrite d'un
        /// enfant. L'écran affiche alors le texte recopié, qui est ce qu'on relit
        /// à froid.
        /// </summary>
        [HttpGet("{id:int}/expressions-ecrites/{expressionEcriteId:int}/photo")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La photo de sa copie.")]
        [SwaggerResponse(404, "Photo purgée, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetExpressionEcritePhoto(
            int id, int expressionEcriteId,
            [FromServices] IExpressionEcriteRepository expressionsEcrites)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var photo = await expressionsEcrites.GetPhotoAsync(expressionEcriteId, id);
                if (photo is null) return NotFound();

                // PRIVÉ, JAMAIS PARTAGÉ : c'est la page d'un cahier d'enfant, avec
                // souvent son nom écrit en haut. Et pas d'`immutable` non plus,
                // contrairement à l'audio : celle-ci finit par être effacée.
                Response.Headers.CacheControl = "private, max-age=600";

                return File(photo.Value.Donnees, photo.Value.TypeMime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de la photo de l'expression ecrite "
                    + "{ExpressionEcriteId} de l'eleve {EleveId}.", expressionEcriteId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>L'élève vient d'ouvrir son texte : la pastille s'éteint.</summary>
        [HttpPost("{id:int}/expressions-ecrites/{expressionEcriteId:int}/vue")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "C'est noté.")]
        [SwaggerResponse(404, "Texte inexistant, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> MarquerExpressionEcriteVue(
            int id, int expressionEcriteId,
            [FromServices] IExpressionEcriteRepository expressionsEcrites)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return await expressionsEcrites.MarquerVueAsync(expressionEcriteId, id)
                    ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du marquage de l'expression ecrite {ExpressionEcriteId} "
                    + "de l'eleve {EleveId}.", expressionEcriteId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Les compréhensions orales d'un enfant dans une matière.
        ///
        /// Sans `matiereId`, renvoie le NOMBRE de compréhensions orales par
        /// matière — même principe que <see cref="GetDictees"/>.
        /// </summary>
        [HttpGet("{id:int}/comprehensions-orales")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Les compréhensions orales, ou leur nombre par matière.")]
        [SwaggerResponse(404, "Profil inexistant ou n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetComprehensionsOrales(
            int id,
            [FromQuery] int? matiereId,
            [FromServices] IComprehensionOraleRepository comprehensionsOrales)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                if (matiereId is null) return Ok(await comprehensionsOrales.CompterParMatiereAsync(id));

                return Ok(await comprehensionsOrales.GetParMatiereAsync(id, matiereId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement des comprehensions orales de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Une compréhension orale complète (sans l'audio — voir la route dédiée).</summary>
        [HttpGet("{id:int}/comprehensions-orales/{comprehensionOraleId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "La compréhension orale.", typeof(ComprehensionOraleEleve))]
        [SwaggerResponse(404, "Compréhension orale inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetComprehensionOrale(
            int id, int comprehensionOraleId,
            [FromServices] IComprehensionOraleRepository comprehensionsOrales)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var comprehensionOrale = await comprehensionsOrales.GetDetailAsync(comprehensionOraleId, id);
                return comprehensionOrale is null ? NotFound() : Ok(comprehensionOrale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de la comprehension orale {ComprehensionOraleId} "
                    + "de l'eleve {EleveId}.", comprehensionOraleId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// L'audio du passage, tel que régénéré à l'archivage. 404 si la
        /// synthèse a échoué à l'époque, ou si la fiche n'appartient pas à
        /// ce compte.
        /// </summary>
        [HttpGet("{id:int}/comprehensions-orales/{comprehensionOraleId:int}/audio")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "L'audio du passage.")]
        [SwaggerResponse(404, "Audio indisponible, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> GetComprehensionOraleAudio(
            int id, int comprehensionOraleId,
            [FromServices] IComprehensionOraleRepository comprehensionsOrales)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                var audio = await comprehensionsOrales.GetAudioAsync(comprehensionOraleId, id);
                if (audio is null) return NotFound();

                // Privé : la voix du professeur pour cet enfant précisément.
                // Un cache partagé ne doit jamais la garder. Un an côté
                // navigateur en revanche — le contenu ne change jamais.
                Response.Headers.CacheControl = "private, max-age=31536000, immutable";

                return File(audio, "audio/wav");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du chargement de l'audio de la comprehension orale "
                    + "{ComprehensionOraleId} de l'eleve {EleveId}.", comprehensionOraleId, id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// L'élève vient d'ouvrir la compréhension orale : la pastille « à
        /// consulter » s'éteint.
        /// </summary>
        [HttpPost("{id:int}/comprehensions-orales/{comprehensionOraleId:int}/vue")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "C'est noté.")]
        [SwaggerResponse(404, "Compréhension orale inexistante, ou enfant n'appartenant pas à ce compte.")]
        public async Task<IActionResult> MarquerComprehensionOraleVue(
            int id, int comprehensionOraleId,
            [FromServices] IComprehensionOraleRepository comprehensionsOrales)
        {
            try
            {
                var eleve = await _eleveBuilder.GetEleveByIdAsync(id);
                if (eleve is null) return NotFound();

                return await comprehensionsOrales.MarquerVueAsync(comprehensionOraleId, id)
                    ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du marquage de la comprehension orale {ComprehensionOraleId} "
                    + "de l'eleve {EleveId}.", comprehensionOraleId, id);
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

                // LE DROIT AVANT LA CAPACITÉ, et les deux refus ne disent pas la
                // même chose : la formule peut très bien couvrir trois enfants
                // pendant que ce compte-ci n'a plus le droit d'en ajouter. Parler
                // de formule dans ce cas enverrait le parent changer d'offre pour
                // rien.
                //
                // 403 ET NON 409 : le conflit dit « votre formule ne suffit pas »,
                // ce qui appelle une action du parent. Ici il ne PEUT pas, quoi
                // qu'il fasse — c'est une permission retirée, pas une limite à
                // repousser.
                if (!parent.PeutAjouterEnfant)
                {
                    _logger.LogInformation(
                        "Ajout d'enfant REFUSE pour le parent {ParentId} : droit retire.",
                        parent.Id);

                    return StatusCode(403, new
                    {
                        message = "L'ajout d'un enfant n'est pas autorisé sur ce compte. "
                                  + "Contactez-nous si vous pensez que c'est une erreur.",
                    });
                }

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
