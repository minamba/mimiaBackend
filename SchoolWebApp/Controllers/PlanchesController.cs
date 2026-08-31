using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Les planches qui remplacent les dessins des professeurs.
    ///
    /// DEUX ROUTES, DEUX RÉGIMES D'ACCÈS
    /// ---------------------------------
    /// La LECTURE est anonyme : ce sont des figures sous licence libre —
    /// schémas d'anatomie, circuits, cartes — sans la moindre donnée
    /// personnelle. Les protéger obligerait le tableau à les charger en blob
    /// avec le jeton, comme les copies d'élèves, pour rien : on perdrait la
    /// mise en cache du navigateur sur des fichiers qui ne changent jamais.
    ///
    /// L'ÉCRITURE est réservée à l'administration. Une planche s'affiche
    /// devant tous les élèves : l'importation est un acte éditorial, et le
    /// crédit de licence engage l'éditeur du service.
    /// </summary>
    [ApiController]
    [Route("planches")]
    public class PlanchesController : Controller
    {
        /// <summary>
        /// Cinq mégaoctets. Une planche SVG en fait quelques dizaines de
        /// kilo-octets ; au-delà de ce plafond, c'est une photo haute
        /// définition qui n'a rien à faire au tableau.
        /// </summary>
        private const int TailleMax = 5 * 1024 * 1024;

        /// <summary>
        /// Exactement ce qu'un navigateur affiche dans une balise `img`.
        /// Le SVG est accepté ici alors qu'il est refusé pour les pièces
        /// jointes des élèves : la source est différente — un administrateur
        /// authentifié, pas un enfant — et le fichier est servi tel quel dans
        /// une `img`, contexte où le navigateur n'exécute pas les scripts.
        /// </summary>
        private static readonly HashSet<string> TypesAcceptes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/svg+xml", "image/png", "image/jpeg", "image/webp", "image/gif",
        };

        private readonly IPlancheRepository _planches;
        private readonly IBibliothequePlanchesService _bibliotheque;
        private readonly Workers.ReveilPlanches _reveil;
        private readonly Workers.DescriptionPlanchesWorker _worker;
        private readonly ILogger<PlanchesController> _logger;

        public PlanchesController(
            IPlancheRepository planches,
            IBibliothequePlanchesService bibliotheque,
            Workers.ReveilPlanches reveil,
            Workers.DescriptionPlanchesWorker worker,
            ILogger<PlanchesController> logger)
        {
            _planches = planches ?? throw new ArgumentNullException(nameof(planches));
            _bibliotheque = bibliotheque ?? throw new ArgumentNullException(nameof(bibliotheque));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
            _worker = worker ?? throw new ArgumentNullException(nameof(worker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sert une planche. 404 si elle n'a pas été importée — c'est ce
        /// signal qui fait retomber le tableau sur le dessin du professeur.
        /// </summary>
        [HttpGet("{cle}")]
        [AllowAnonymous]
        [SwaggerResponse(200, "La planche.")]
        [SwaggerResponse(404, "Aucune planche importée pour cette clé.")]
        public async Task<IActionResult> Get(string cle)
        {
            var planche = await _planches.GetAsync(cle, HttpContext.RequestAborted);
            if (planche?.Donnees is not { Length: > 0 }) return NotFound();

            // REVALIDATION PLUTÔT QUE CACHE LONG.
            //
            // Un `max-age` de 24 h était le réflexe — une planche ne change
            // presque jamais. Mais « presque » est le mot : remplacer une
            // figure médiocre est l'opération courante de l'administration, et
            // avec un cache long l'ancienne resterait affichée toute la
            // journée. On croirait le remplacement cassé.
            //
            // L'étiquette porte la date de dernière modification : le
            // navigateur redemande à chaque affichage, le serveur répond 304
            // sans renvoyer les octets tant que rien n'a bougé, et une
            // nouvelle planche apparaît immédiatement. Le coût est une requête
            // vide ; le gain, une administration qui se comporte comme on
            // l'attend.
            var version = planche.DateModification ?? planche.DateCreation;
            var etiquette = $"\"{version.Ticks:x}\"";

            if (Request.Headers.IfNoneMatch.Contains(etiquette)) return StatusCode(304);

            Response.Headers.ETag = etiquette;
            Response.Headers.CacheControl = "public, max-age=0, must-revalidate";

            return File(planche.Donnees, planche.TypeMime ?? "application/octet-stream");
        }

        /// <summary>
        /// Le crédit d'une planche : auteur, source, licence.
        ///
        /// POURQUOI UN APPEL SÉPARÉ PLUTÔT QUE DES EN-TÊTES
        /// -----------------------------------------------
        /// La planche s'affiche dans une balise `img`, et une `img` ne donne
        /// aucun accès aux en-têtes de sa réponse. Le crédit doit donc voyager
        /// par un appel à lui. Il ne pèse rien — trois chaînes — et il est
        /// caché par le navigateur comme l'image.
        ///
        /// POURQUOI IL EST PUBLIC
        /// ---------------------
        /// C'est une mention légale, pas une donnée. CC BY et CC BY-SA imposent
        /// de nommer l'auteur et la licence PARTOUT où l'œuvre est montrée : la
        /// réserver aux administrateurs reviendrait à ne jamais la remplir là où
        /// elle est due, c'est-à-dire devant l'élève.
        ///
        /// Rend 204 quand la planche existe sans crédit : ce n'est pas une
        /// erreur, c'est un domaine public sans auteur connu, ou une figure
        /// dessinée par nous.
        /// </summary>
        [HttpGet("{cle}/credit")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Le crédit à afficher sous la planche.")]
        [SwaggerResponse(204, "Planche sans crédit à afficher.")]
        [SwaggerResponse(404, "Aucune planche importée pour cette clé.")]
        public async Task<IActionResult> Credit(string cle)
        {
            var planche = await _planches.GetSansDonneesAsync(cle, HttpContext.RequestAborted);
            if (planche is null) return NotFound();

            // Une planche maison a bien quelque chose à dire — la propriété —
            // même sans auteur tiers. Elle ne tombe donc pas dans le 204.
            if (!planche.Maison
                && string.IsNullOrWhiteSpace(planche.Auteur)
                && string.IsNullOrWhiteSpace(planche.Licence))
            {
                return NoContent();
            }

            var version = planche.DateModification ?? planche.DateCreation;
            var etiquette = $"\"c{version.Ticks:x}\"";

            if (Request.Headers.IfNoneMatch.Contains(etiquette)) return StatusCode(304);

            Response.Headers.ETag = etiquette;
            Response.Headers.CacheControl = "public, max-age=0, must-revalidate";

            return Ok(new
            {
                auteur = planche.Auteur,
                source = planche.Source,
                licence = planche.Licence,
                maison = planche.Maison,
            });
        }

        /// <summary>
        /// La liste des planches importées, sans les octets.
        ///
        /// Le serveur ne renvoie QUE ce qui existe : c'est le front qui tient
        /// le catalogue des figures possibles et croise les deux pour afficher
        /// « importée » ou « dessinée par le professeur ».
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "PeutImporterPlanches")]
        [SwaggerResponse(200, "Planches importées.", typeof(IEnumerable<Planche>))]
        public async Task<IActionResult> Lister() =>
            Ok(await _planches.GetToutesAsync(HttpContext.RequestAborted));

        /// <summary>
        /// Ce qui reste à traiter, par file.
        ///
        /// LE JOURNAL DIT CE QU'ON A DÉPENSÉ, JAMAIS CE QU'IL RESTE À DÉPENSER.
        /// C'est ce qui manquait la nuit où la facture montait sans qu'on sache
        /// combien de planches attendaient derrière.
        /// </summary>
        [HttpGet("files")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Les trois files d'attente.", typeof(FilesPlanches))]
        public async Task<IActionResult> Files() =>
            Ok(await _planches.GetFilesAsync(HttpContext.RequestAborted));

        /// <summary>
        /// Vide les trois files SANS appeler le modèle.
        ///
        /// Le coupe-feu : chaque planche en attente est marquée comme traitée,
        /// avec un contenu qui dit qu'elle ne l'a pas été. Rien n'est supprimé —
        /// réimporter la planche la remet dans la file.
        /// </summary>
        /// <summary>
        /// Re-juge la langue des planches écartées, sans relire aucune image.
        ///
        /// La règle de langue a changé : elle acceptait mal les noms propres
        /// et les emprunts — « Sputnik », « perestroïka », « Erlenmeyer » —, et
        /// condamnait des planches entièrement françaises. Cette passe leur
        /// donne une seconde chance sans repayer une lecture d image.
        /// </summary>
        [HttpPost("requalifier")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Planches re-jugées.")]
        public async Task<IActionResult> Requalifier(
            [FromServices] Workers.DescriptionPlanchesWorker worker,
            CancellationToken ct)
        {
            var (examinees, liberees) = await worker.RequalifierAsync(ct);
            return Ok(new { examinees, liberees });
        }

        [HttpPost("files/vider")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "Nombre de marquages effectués.")]
        public async Task<IActionResult> ViderLesFiles()
        {
            var marquees = await _planches.ViderLesFilesAsync(HttpContext.RequestAborted);
            _logger.LogWarning(
                "Files des planches vidées à la main : {Marquees} marquage(s), aucun appel au modèle.",
                marquees);
            return Ok(new { marquees });
        }

        /// <summary>
        /// Importe une planche, ou remplace celle qui portait déjà cette clé.
        ///
        /// LE NOM DU FICHIER ENVOYÉ EST IGNORÉ. C'est la clé qui commande, et
        /// le nom est reconstruit à partir d'elle : l'administrateur choisit
        /// « appareil_respiratoire_wikimedia_v2.svg » sur son disque, et la
        /// planche est enregistrée sous « svt-respiratoire.svg ». Sans ça,
        /// chaque import introduirait un nom arbitraire à retrouver plus tard.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "PeutImporterPlanches")]
        [RequestSizeLimit(TailleMax + 8192)]
        [SwaggerResponse(200, "Planche importée.", typeof(Planche))]
        [SwaggerResponse(400, "Fichier absent, trop lourd, ou format refusé.")]
        public async Task<IActionResult> Importer(
            [FromForm] string cle,
            [FromForm] string matiereCode,
            IFormFile fichier,
            [FromForm] string? auteur = null,
            [FromForm] string? source = null,
            [FromForm] string? licence = null,
            [FromForm] bool maison = false)
        {
            if (string.IsNullOrWhiteSpace(cle) || string.IsNullOrWhiteSpace(matiereCode))
            {
                return BadRequest(new { message = "Clé et matière obligatoires." });
            }

            if (fichier is null || fichier.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier reçu." });
            }

            if (fichier.Length > TailleMax)
            {
                return BadRequest(new { message = "Fichier trop lourd (5 Mo maximum)." });
            }

            if (!TypesAcceptes.Contains(fichier.ContentType ?? string.Empty))
            {
                return BadRequest(new
                {
                    message = "Format refusé. Accepté : SVG, PNG, JPEG, WebP, GIF.",
                });
            }

            try
            {
                using var flux = new MemoryStream();
                await fichier.CopyToAsync(flux, HttpContext.RequestAborted);

                var planche = await _planches.ImporterAsync(new Planche
                {
                    Cle = cle.Trim().ToLowerInvariant(),
                    MatiereCode = matiereCode.Trim().ToUpperInvariant(),
                    NomFichier = $"{cle.Trim().ToLowerInvariant()}{Extension(fichier.ContentType)}",
                    TypeMime = fichier.ContentType,
                    Taille = (int)fichier.Length,
                    Donnees = flux.ToArray(),
                    // Une planche maison n'a pas d'auteur tiers, et les champs
                    // du formulaire sont masqués dans ce cas. On les écrase
                    // quand même : si l'administrateur a saisi un auteur puis
                    // coché la case, garder sa saisie créditerait Wikimedia
                    // pour une figure qui n'en vient pas.
                    Auteur = maison ? null : auteur?.Trim(),
                    Source = maison ? null : source?.Trim(),
                    Licence = maison ? null : licence?.Trim(),
                    Maison = maison,
                }, HttpContext.RequestAborted);

                // LES DEUX GESTES QUI SUPPRIMENT L'ATTENTE, ET IL EN FAUT DEUX.
                //
                // La sonnerie fait repartir le worker tout de suite au lieu de
                // son intervalle de deux minutes : la description sera extraite
                // dans la foulée.
                //
                // L'oubli du cache est indispensable AVEC elle, pas à sa place.
                // Le bloc de consigne était gardé cinq minutes : sans l'oubli,
                // la description serait en base dans la seconde et le
                // professeur continuerait de recevoir, quatre minutes durant, un
                // bloc construit avant qu'elle existe.
                //
                // Ensemble, une planche importée est utilisable au tour
                // suivant. Séparément, chacun laisse l'autre attendre.
                _bibliotheque.Oublier(planche.MatiereCode);

                // ON ATTEND LA DESCRIPTION, ON NE LA DÉLÈGUE PAS.
                //
                // Sonner le worker raccourcissait l'attente ; l'attendre la
                // supprime. Tant que la description manque, le professeur peut
                // afficher la figure sans savoir la lire — et en SVT, où les
                // clés sont dans sa consigne en dur, il l'affiche vraiment.
                //
                // Quelques secondes sur un clic d'administration valent mieux
                // qu'une fenêtre, même courte, où un enfant s'entend dire que
                // la planche qu'il regarde n'a pas de légendes.
                //
                // `RequestAborted` n'est PAS passé : si l'administrateur ferme
                // son onglet, la description doit se terminer quand même — elle
                // est déjà payée, et l'abandonner laisserait exactement le trou
                // qu'on vient de fermer.
                await _worker.DecrireMaintenantAsync(planche.Cle, CancellationToken.None);
                _reveil.Sonner();

                // RELUE APRÈS LA LECTURE, ET C EST TOUT L INTÉRÊT.
                //
                // `ImporterAsync` a rendu la planche AVANT que le modèle la
                // regarde : son `Contenu` est donc vide quoi qu il advienne
                // ensuite. L écran a besoin de savoir si le professeur SAIT LA
                // LIRE, pas seulement si les octets sont arrivés — une planche
                // stockée mais muette est le seul échec qui ne se voit pas.
                //
                // Sans octets : la relire avec eux renverrait le fichier qu on
                // vient d envoyer.
                var relue = await _planches.GetSansDonneesAsync(
                    planche.Cle, CancellationToken.None) ?? planche;

                _logger.LogInformation(
                    "Planche {Cle} importee ({Type}, {Taille} octets), description {Etat}.",
                    planche.Cle, planche.TypeMime, planche.Taille,
                    string.IsNullOrWhiteSpace(relue.Contenu) ? "ABSENTE" : "extraite");

                return Ok(relue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de l'import de la planche {Cle}.", cle);
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        [HttpDelete("{cle}")]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(204, "Planche retirée — le professeur redessine.")]
        [SwaggerResponse(404, "Aucune planche pour cette clé.")]
        public async Task<IActionResult> Supprimer(string cle)
        {
            // La matière se lit AVANT la suppression : après, la ligne n'existe
            // plus et on ne saurait plus quel bloc oublier.
            var matiere = (await _planches.GetSansDonneesAsync(cle, HttpContext.RequestAborted))
                ?.MatiereCode;

            var retiree = await _planches.SupprimerAsync(cle, HttpContext.RequestAborted);
            if (!retiree) return NotFound();

            // Retirer une planche compte autant qu'en poser une : sans cet
            // oubli, le professeur continuerait cinq minutes à croire qu'il
            // dispose d'une figure qui n'affichera plus rien.
            _bibliotheque.Oublier(matiere);

            return NoContent();
        }

        /// <summary>L'extension qui correspond au type déclaré.</summary>
        private static string Extension(string? typeMime) => typeMime?.ToLowerInvariant() switch
        {
            "image/svg+xml" => ".svg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg",
        };
    }
}
