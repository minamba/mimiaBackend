using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// LES EXAMENS BLANCS DE L'ÉLÈVE — Camara, le 23/11/2026. Brevet blanc en 3e, bac blanc en 1re et terminale,
    /// bac pro blanc en voie professionnelle ; réservés aux abonnés (la lecture reste ouverte : ce qu'il a déjà
    /// envoyé lui appartient). Étape 1 : la date, les matières, les documents et leur aperçu.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("eleves/{id:int}/examens-blancs")]
    public class ExamensBlancsController : ControllerBase
    {
        public const string MessageReserveAbonnes =
            "Les examens blancs sont réservés aux abonnés. Demande à ton parent de choisir une formule.";

        private readonly IChatContexteResolver _resolveur;
        private readonly IExamenBlancRepository _examens;
        private readonly ILogger<ExamensBlancsController> _logger;

        public ExamensBlancsController(
            IChatContexteResolver resolveur, IExamenBlancRepository examens, ILogger<ExamensBlancsController> logger)
        {
            _resolveur = resolveur ?? throw new ArgumentNullException(nameof(resolveur));
            _examens = examens ?? throw new ArgumentNullException(nameof(examens));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public sealed class AjoutMatiereRequest
        {
            [Range(1, int.MaxValue)]
            public int MatiereId { get; set; }

            [Required]
            public DateTime? DateExamen { get; set; }

            /// <summary>Facultative, « HH:mm ».</summary>
            public TimeSpan? HeureExamen { get; set; }
        }

        /// <summary>
        /// Le type d'examen blanc de sa classe (« Brevet blanc », « Bac blanc », « Bac pro blanc »), ou rien : le
        /// bouton ne s'affiche que s'il en a un.
        /// </summary>
        [HttpGet("type")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "{ type, libelle } — tous deux null si sa classe n'en a pas.")]
        public async Task<IActionResult> GetType(int id)
        {
            var eleve = await _resolveur.ResoudreEleveAsync(id);
            if (eleve is null) return NotFound();

            var type = ExamensBlancs.TypeDe(eleve.NiveauCode);
            return Ok(new { type, libelle = type is null ? null : ExamensBlancs.Libelle(type) });
        }

        [HttpGet]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Ses examens blancs, du plus proche au plus lointain.")]
        public async Task<IActionResult> GetListe(int id, [FromServices] ExamensBlancsService service, CancellationToken ct)
        {
            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();

                var examens = await _examens.GetListeAsync(id, ct);
                foreach (var examen in examens) await service.CompleterAsync(id, examen, ct);
                return Ok(examens.Select(ExamenBlancVue.Depuis));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des examens blancs de l'eleve {EleveId}.", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("{examenId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "L'examen blanc, ses matières et leurs documents.")]
        [SwaggerResponse(404, "Inexistant ou n'appartenant pas à cet élève.")]
        public async Task<IActionResult> Get(int id, int examenId, [FromServices] ExamensBlancsService service, CancellationToken ct)
        {
            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();

                var examen = await _examens.GetAsync(id, examenId, ct);
                if (examen is null) return NotFound();

                await service.CompleterAsync(id, examen, ct);
                return Ok(ExamenBlancVue.Depuis(examen));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture de l'examen blanc {ExamenId}.", examenId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>
        /// Ajoute une matière pour une date : crée l'examen blanc de ce jour-là, ou rejoint celui qui existe.
        /// </summary>
        [HttpPost("matieres")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "{ examenBlancId, matiereLigneId }.")]
        [SwaggerResponse(400, "Matière absente de sa classe, date passée, ou classe sans examen blanc.")]
        [SwaggerResponse(403, "Réservé aux abonnés.")]
        public async Task<IActionResult> AjouterMatiere(
            int id,
            [FromBody] AjoutMatiereRequest requete,
            [FromServices] IEleveViewModelBuilder eleves,
            [FromServices] AccesForfaitService acces,
            [FromServices] ExamensBlancsService service,
            CancellationToken ct)
        {
            if (!ModelState.IsValid || requete.DateExamen is not DateTime date) return BadRequest(ModelState);

            try
            {
                var eleve = await _resolveur.ResoudreEleveAsync(id);
                if (eleve is null) return NotFound();

                var type = ExamensBlancs.TypeDe(eleve.NiveauCode);
                if (type is null) return BadRequest(new { message = "Ta classe n'a pas d'examen blanc." });

                if (!(await acces.LirePourEleveAsync(id, ct)).Ouvert)
                {
                    return StatusCode(403, new { message = MessageReserveAbonnes });
                }

                // Un examen blanc d'hier n'a plus rien à préparer : il se crée aujourd'hui ou plus tard.
                if (date.Date < DateTime.Today) return BadRequest(new { message = "Choisis une date à venir." });

                var matieres = await eleves.GetMatieresDeLEleveAsync(id);
                if (matieres is null
                    || !matieres.Any(m => m.Id == requete.MatiereId && !ConseillereOrientation.Est(m.Code)))
                {
                    return BadRequest(new { message = "Cette matière n'est pas dans ta classe." });
                }

                var (examenBlancId, matiereLigneId) = await _examens.AjouterMatiereAsync(
                    id, type, date, requete.HeureExamen, requete.MatiereId, DateTime.UtcNow, ct);

                // LE CONTRÔLE CACHÉ DE LA MATIÈRE, tout de suite : la préparation peut commencer dès maintenant.
                if (await _examens.GetAsync(id, examenBlancId, ct) is ExamenBlanc cree)
                {
                    await service.CompleterAsync(id, cree, ct);
                }

                return Ok(new { examenBlancId, matiereLigneId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'une matiere d'examen blanc (eleve {EleveId}).", id);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("{examenId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "Supprimé, avec ses fichiers.")]
        public async Task<IActionResult> Supprimer(int id, int examenId, [FromServices] ExamensBlancsService service, CancellationToken ct)
        {
            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();
                return await service.SupprimerAsync(id, examenId, ct) ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'examen blanc {ExamenId}.", examenId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("{examenId:int}/matieres/{ligneId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "Matière retirée avec ses documents ; l'examen blanc part avec sa dernière matière.")]
        public async Task<IActionResult> RetirerMatiere(
            int id, int examenId, int ligneId, [FromServices] ExamensBlancsService service, CancellationToken ct)
        {
            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();
                return await service.RetirerMatiereAsync(id, examenId, ligneId, ct) ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du retrait d'une matiere de l'examen blanc {ExamenId}.", examenId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpPost("{examenId:int}/matieres/{ligneId:int}/documents")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [RequestSizeLimit(ExamensBlancs.TailleMax + 64 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = ExamensBlancs.TailleMax + 64 * 1024)]
        [SwaggerResponse(200, "Le document ajouté.")]
        [SwaggerResponse(400, "Refusé — le motif est écrit pour l'élève.")]
        [SwaggerResponse(403, "Réservé aux abonnés.")]
        [SwaggerResponse(503, "Le dossier des documents n'est pas prêt sur le serveur.")]
        public async Task<IActionResult> AjouterDocument(
            int id, int examenId, int ligneId, IFormFile fichier,
            [FromServices] ExamensBlancsService service,
            [FromServices] AccesForfaitService acces,
            CancellationToken ct)
        {
            if (fichier is null || fichier.Length == 0) return BadRequest(new { message = "Aucun fichier reçu." });

            if (fichier.Length > ExamensBlancs.TailleMax)
            {
                return BadRequest(new { message = "Ce fichier dépasse 100 Mo : enregistre-le en plus léger (en PDF, ou sans ses plus grosses images)." });
            }

            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();

                if (!(await acces.LirePourEleveAsync(id, ct)).Ouvert)
                {
                    return StatusCode(403, new { message = MessageReserveAbonnes });
                }

                using var flux = new MemoryStream();
                await fichier.CopyToAsync(flux, ct);

                var resultat = await service.AjouterAsync(
                    id, examenId, ligneId, fichier.FileName, fichier.ContentType, flux.ToArray(), ct);

                if (resultat.StockageIndisponible) return StatusCode(503, new { message = resultat.Motif });

                return resultat.Document is null
                    ? BadRequest(new { message = resultat.Motif })
                    : Ok(DocumentExamenBlancVue.Depuis(resultat.Document));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'un document a l'examen blanc {ExamenId}.", examenId);
                return StatusCode(500, new { message = "Le document n'a pas pu être envoyé. Tu peux réessayer ?" });
            }
        }

        [HttpDelete("{examenId:int}/documents/{documentId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(204, "Document retiré, et son fichier effacé du disque.")]
        public async Task<IActionResult> RetirerDocument(
            int id, int examenId, int documentId, [FromServices] ExamensBlancsService service, CancellationToken ct)
        {
            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();
                return await service.RetirerDocumentAsync(id, examenId, documentId, ct) ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du retrait d'un document de l'examen blanc {ExamenId}.", examenId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("{examenId:int}/documents/{documentId:int}/fichier")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "Le fichier, avec son type.")]
        [SwaggerResponse(410, "Effacé, deux mois après l'examen blanc.")]
        public async Task<IActionResult> GetFichier(
            int id, int examenId, int documentId, [FromServices] ExamensBlancsService service, CancellationToken ct)
        {
            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();

                var document = await _examens.GetDocumentAsync(id, examenId, documentId, ct);
                if (document is null) return NotFound();

                var lu = await service.LireAsync(id, examenId, documentId, ct);
                if (lu is null)
                {
                    return StatusCode(410, new { message = "Ce document n'est plus sur nos serveurs : il est effacé deux mois après l'examen blanc." });
                }

                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{Uri.EscapeDataString(document.NomFichier)}";
                return File(lu.Value.Octets, document.TypeMime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture d'un document de l'examen blanc {ExamenId}.", examenId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpGet("{examenId:int}/documents/{documentId:int}/texte")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        [SwaggerResponse(200, "{ texte, lu } — vide tant que le document n'est pas lu.")]
        public async Task<IActionResult> GetTexte(int id, int examenId, int documentId, CancellationToken ct)
        {
            try
            {
                if (await _resolveur.ResoudreEleveAsync(id) is null) return NotFound();

                var document = await _examens.GetDocumentAsync(id, examenId, documentId, ct);
                if (document is null) return NotFound();

                var illisible = document.Transcription?.StartsWith(DocumentControle.PrefixeIllisible, StringComparison.Ordinal) == true;
                return Ok(new { texte = illisible ? string.Empty : document.Transcription ?? string.Empty, lu = document.Lu, illisible });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture du texte d'un document de l'examen blanc {ExamenId}.", examenId);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }
    }

    /// <summary>Un examen blanc, tel que l'écran le montre.</summary>
    public sealed class ExamenBlancVue
    {
        public int Id { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Libelle { get; init; } = string.Empty;
        public DateTime DateExamen { get; init; }
        public string? HeureExamen { get; init; }
        /// <summary>Le jour où ses fichiers seront effacés.</summary>
        public DateTime EffacementLe { get; init; }

        /// <summary>La moyenne des matières, comme la préparation au brevet (« la moyenne de tes épreuves »).</summary>
        public int Pourcent { get; init; }

        public List<MatiereExamenBlancVue> Matieres { get; init; } = [];

        public static ExamenBlancVue Depuis(ExamenBlanc e) => new()
        {
            Id = e.Id,
            Type = e.Type,
            Libelle = e.Libelle,
            DateExamen = e.DateExamen,
            HeureExamen = e.HeureExamen?.ToString(@"hh\:mm"),
            EffacementLe = ExamensBlancs.EffacementLe(e.DateExamen),
            Pourcent = e.Matieres.Count == 0
                ? 0
                : (int)Math.Round(e.Matieres.Average(m => (double)(m.Preparation?.Pourcent ?? 0))),
            Matieres = e.Matieres.Select(m => new MatiereExamenBlancVue
            {
                Id = m.Id,
                MatiereId = m.MatiereId,
                MatiereLibelle = m.MatiereLibelle,
                ProfPrenom = m.ProfPrenom,
                ProfAvatar = m.ProfAvatar,
                ProfCouleur = m.ProfCouleur,
                Documents = m.Documents.Select(DocumentExamenBlancVue.Depuis).ToList(),
                DossiersAJour = m.DossiersAJour,
                DocumentsLus = m.Documents.Count(d => d.Lu),
                ControleId = m.ControleId,
                ProgrammeValide = m.ProgrammeValideLe is not null,
                Preparation = m.Preparation is not PreparationControle p ? null : new PreparationMatiereVue
                {
                    Pourcent = p.Pourcent,
                    PerimetreConnu = p.PerimetreConnu,
                    PretStatut = p.PretStatut,
                    PretObservation = p.PretObservation,
                    Commence = p.DernierePreparationLe is not null,
                    Notions = p.Notions
                        .Select(n => new NotionPreparationVue { Libelle = n.Libelle, Etat = n.Etat, Pourcent = n.Pourcent })
                        .ToList(),
                },
                Dossiers = m.Notions
                    .Select(n => new DossierExamenBlancVue
                    {
                        Id = n.Id,
                        Libelle = n.Libelle,
                        CompetenceId = n.CompetenceId,
                        DocumentIds = n.DocumentIds,
                    })
                    .ToList(),
            }).ToList(),
        };
    }

    public sealed class MatiereExamenBlancVue
    {
        /// <summary>L'identifiant de la LIGNE (la matière dans cet examen blanc), pas celui de la matière.</summary>
        public int Id { get; init; }
        public int MatiereId { get; init; }
        public string? MatiereLibelle { get; init; }
        public string? ProfPrenom { get; init; }
        public string? ProfAvatar { get; init; }
        public string? ProfCouleur { get; init; }
        public List<DocumentExamenBlancVue> Documents { get; init; } = [];

        /// <summary>
        /// Faux tant que le professeur lit ou range (un document attend, ou ils ont changé depuis le rangement) :
        /// l'écran dit « ton professeur range tes documents » et relit la page de temps en temps.
        /// </summary>
        public bool DossiersAJour { get; init; }

        public int DocumentsLus { get; init; }

        /// <summary>Les dossiers par notion (étape 2) ; un document peut être dans plusieurs.</summary>
        public List<DossierExamenBlancVue> Dossiers { get; init; } = [];

        /// <summary>Le contrôle caché (étape 3) : la séance de préparation s'ouvre sur lui.</summary>
        public int? ControleId { get; init; }

        /// <summary>L'élève a dit d'accord au programme proposé au tableau.</summary>
        public bool ProgrammeValide { get; init; }

        public PreparationMatiereVue? Preparation { get; init; }
    }

    /// <summary>La préparation d'une matière : celle de son contrôle caché.</summary>
    public sealed class PreparationMatiereVue
    {
        public int Pourcent { get; init; }
        public bool PerimetreConnu { get; init; }
        public string? PretStatut { get; init; }
        public string? PretObservation { get; init; }
        public bool Commence { get; init; }
        public List<NotionPreparationVue> Notions { get; init; } = [];
    }

    public sealed class NotionPreparationVue
    {
        public string Libelle { get; init; } = string.Empty;
        public string Etat { get; init; } = string.Empty;
        public int Pourcent { get; init; }
    }

    public sealed class DossierExamenBlancVue
    {
        public int Id { get; init; }
        public string Libelle { get; init; } = string.Empty;
        public int? CompetenceId { get; init; }
        public List<int> DocumentIds { get; init; } = [];
    }

    /// <summary>Même forme que <c>DocumentControleViewModel</c> : l'aperçu de l'élève est le même.</summary>
    public sealed class DocumentExamenBlancVue
    {
        public int Id { get; init; }
        public string NomFichier { get; init; } = string.Empty;
        public string TypeMime { get; init; } = string.Empty;
        public int Taille { get; init; }
        public int NombrePages { get; init; }
        public bool Lu { get; init; }
        public bool Consultable { get; init; }
        public bool Purge { get; init; }
        public string Genre { get; init; } = "autre";
        public DateTime DateCreation { get; init; }

        public static DocumentExamenBlancVue Depuis(DocumentExamenBlanc d) => new()
        {
            Id = d.Id,
            NomFichier = d.NomFichier,
            TypeMime = d.TypeMime,
            Taille = d.Taille,
            NombrePages = d.NombrePages,
            Lu = d.Lu,
            Consultable = d.Consultable,
            Purge = d.Purge,
            Genre = d.Genre,
            DateCreation = d.DateCreation,
        };
    }
}
