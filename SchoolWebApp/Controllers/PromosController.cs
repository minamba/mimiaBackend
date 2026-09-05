using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Les bandeaux promotionnels de la page d'accueil.
    ///
    /// DEUX PUBLICS, DEUX RÉGIMES
    /// --------------------------
    /// Le bandeau affiché et ses images sont ANONYMES : ils sont sur la page
    /// d'accueil, donc lus par des visiteurs qui n'ont pas de compte — c'est
    /// même tout l'intérêt d'une promotion. La bibliothèque et les écritures
    /// sont réservées à l'administration.
    ///
    /// LES OCTETS ONT LEUR PROPRE ROUTE, et ne transitent par aucune liste.
    /// La description d'un bandeau pèse deux cents octets, son visuel un
    /// mégaoctet : les mêler ferait payer le second à chaque lecture du
    /// premier, y compris pour dessiner un tableau d'administration.
    /// </summary>
    [ApiController]
    [Route("promos")]
    public class PromosController : ControllerBase
    {
        /// <summary>
        /// Le poids maximal d'un visuel, la même borne que les planches.
        ///
        /// Un bandeau bien exporté pèse 150 à 400 Ko. Cinq mégaoctets laissent
        /// passer un PNG non compressé sorti de Canva sans obliger
        /// l'administrateur à comprendre pourquoi son fichier est refusé — la
        /// page se chargera plus lentement, ce qui est un moindre mal devant
        /// une promotion qu'on n'arrive pas à mettre en ligne.
        /// </summary>
        private const int TailleMax = 5 * 1024 * 1024;

        /// <summary>
        /// Le poids maximal d'une VIDÉO. Quatre fois celui d'une image, et
        /// pas davantage.
        ///
        /// LA VRAIE CONTRAINTE N'EST PAS LE TÉLÉVERSEMENT, C'EST LA BASE.
        /// Les octets vivent dans `BandeauPromo`, et SQL Server Express
        /// plafonne à 10 Go — au-delà, il refuse d'écrire et l'application
        /// s'arrête. Une bibliothèque de dix vidéos à 20 Mo pèse 200 Mo :
        /// tenable. À 200 Mo la vidéo, c'est un cinquième du plafond pour un
        /// seul bandeau.
        ///
        /// C'EST AUSSI CE QUE LE VISITEUR TÉLÉCHARGE avant de voir la page.
        /// Vingt mégaoctets sur un réseau mobile, ce sont plusieurs secondes
        /// d'attente pour un ornement — et la promotion arrive après que le
        /// visiteur a fini de lire.
        /// </summary>
        private const int TailleMaxVideo = 20 * 1024 * 1024;

        /// <summary>
        /// Les formats acceptés.
        ///
        /// PAS DE SVG, contrairement aux planches. Une planche est un schéma
        /// vectoriel qu'on veut net à toutes les tailles ; un visuel
        /// promotionnel sort d'un outil de mise en page et contient des
        /// photos. Surtout, un SVG est un document : il peut porter du script,
        /// et celui-ci vient d'un téléversement. Le refuser ici coûte zéro et
        /// ferme la question.
        /// </summary>
        private static readonly string[] TypesAcceptes =
            ["image/jpeg", "image/png", "image/webp", "image/gif"];

        /// <summary>
        /// Les formats vidéo acceptés.
        ///
        /// MP4 D'ABORD : c'est le seul que lisent à la fois Safari, Chrome,
        /// Firefox et les téléphones. WebM est meilleur à poids égal mais
        /// Safari ne l'a jamais pris en charge partout — une promotion
        /// invisible sur iPhone serait invisible pour la moitié du trafic.
        ///
        /// PAS DE MOV NI D'AVI : ce sont des conteneurs d'export, pas des
        /// formats de diffusion. Ils pèsent dix fois plus et ne se lisent pas
        /// dans un navigateur.
        /// </summary>
        private static readonly string[] TypesVideoAcceptes =
            ["video/mp4", "video/webm"];

        private static bool EstVideo(string? typeMime) =>
            typeMime is not null
            && typeMime.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        private readonly IPromoRepository _promos;
        private readonly ILogger<PromosController> _logger;

        public PromosController(IPromoRepository promos, ILogger<PromosController> logger)
        {
            _promos = promos ?? throw new ArgumentNullException(nameof(promos));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------------------------------------------------ public

        /// <summary>
        /// Le bandeau affiché, sans ses octets. 204 s'il n'y en a aucun.
        /// </summary>
        /// <remarks>
        /// 204 ET NON 404. « Aucune promotion en ce moment » est un état
        /// normal du site — le plus fréquent, même. Un 404 le ferait
        /// journaliser comme une erreur par tous les outils de surveillance,
        /// et le bruit finirait par masquer les vraies pannes.
        /// </remarks>
        [HttpGet("actif")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Le bandeau affiché.")]
        [SwaggerResponse(204, "Aucun bandeau affiché.")]
        public async Task<IActionResult> Actif(CancellationToken ct)
        {
            try
            {
                var promo = await _promos.GetActifAsync(ct);
                if (promo is null) return NoContent();

                return Ok(new
                {
                    promo.Id,
                    promo.TexteAlternatif,
                    promo.Lien,
                    promo.AvecImageMobile,
                    promo.PleineLargeur,

                    // L'écran doit choisir entre `<picture>` et `<video>`
                    // AVANT de demander les octets : les deux balises ne se
                    // remplacent pas, et une vidéo posée dans un `<img>` ne
                    // montre rien du tout.
                    promo.EstVideo,

                    // La version part avec : c'est elle que l'écran met dans
                    // l'adresse des images, pour qu'un visuel remplacé
                    // remplace aussi celui qui est déjà dans le cache du
                    // navigateur.
                    version = promo.Version.Ticks,
                });
            }
            catch (Exception ex)
            {
                // UNE PROMOTION ILLISIBLE NE DOIT PAS FERMER LA PAGE D'ACCUEIL.
                // C'est un ornement commercial : en cas de panne de lecture, le
                // site s'affiche sans lui, et on le journalise.
                _logger.LogError(ex, "Lecture du bandeau promotionnel impossible.");
                return NoContent();
            }
        }

        /// <summary>
        /// Les octets d'un visuel.
        /// </summary>
        /// <param name="format">« mobile » pour la version téléphone.</param>
        [HttpGet("{id:int}/image")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Le visuel.")]
        [SwaggerResponse(404, "Aucun visuel pour ce bandeau.")]
        public async Task<IActionResult> Image(
            int id, [FromQuery] string? format, CancellationToken ct)
        {
            var mobile = string.Equals(format, "mobile", StringComparison.OrdinalIgnoreCase);

            var image = await _promos.GetImageAsync(id, mobile, ct);
            if (image is null) return NotFound();

            // REVALIDATION PLUTÔT QUE CACHE LONG, comme pour les planches.
            //
            // L'étiquette porte la date de dernière modification du bandeau :
            // le navigateur redemande à chaque affichage, le serveur répond 304
            // sans renvoyer les octets tant que rien n'a bougé, et un visuel
            // remplacé apparaît immédiatement. Le coût est une requête vide ;
            // le gain, une administration qui se comporte comme on l'attend —
            // un cache de 24 h laisserait l'ancienne promotion en ligne toute
            // la journée après son remplacement.
            var etiquette = $"\"{image.Version.Ticks:x}{(mobile ? "m" : "l")}\"";

            if (Request.Headers.IfNoneMatch.Contains(etiquette)) return StatusCode(304);

            Response.Headers.ETag = etiquette;
            Response.Headers.CacheControl = "public, max-age=0, must-revalidate";

            // LES REQUÊTES PAR PLAGE SONT INDISPENSABLES À LA VIDÉO.
            //
            // Un lecteur vidéo ne télécharge pas le fichier entier puis le
            // joue : il demande les premiers octets, lit l'en-tête, puis
            // réclame des morceaux. Sans `Accept-Ranges`, Safari refuse
            // simplement de lire — c'est un silence, pas une erreur.
            //
            // Activé pour tout, image comprise : une image n'en demandera
            // jamais, et le seul coût est un en-tête de plus.
            return File(image.Donnees, image.TypeMime, enableRangeProcessing: true);
        }

        // --------------------------------------------------- administration

        /// <summary>Toute la bibliothèque, sans les octets.</summary>
        [HttpGet]
        [Authorize(Policy = "EstAdmin")]
        [SwaggerResponse(200, "La bibliothèque des bandeaux.")]
        public async Task<IActionResult> Tous(CancellationToken ct) =>
            Ok((await _promos.GetTousAsync(ct)).Select(Decrire));

        /// <summary>
        /// Ajoute un bandeau. Il est ÉTEINT : on l'allume ensuite.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "EstSuperAdmin")]
        [RequestSizeLimit(2 * TailleMaxVideo + 16384)]
        [SwaggerResponse(201, "Le bandeau créé.")]
        [SwaggerResponse(400, "Fichier manquant, trop lourd ou d'un format refusé.")]
        public async Task<IActionResult> Creer(
            [FromForm] string titre,
            [FromForm] string texteAlternatif,
            [FromForm] string? lien,
            [FromForm] bool pleineLargeur,
            IFormFile imageLarge,
            IFormFile? imageMobile,
            CancellationToken ct)
        {
            var faute = Verifier(titre, texteAlternatif, lien);
            if (faute is not null) return BadRequest(new { message = faute });

            if (imageLarge is null || imageLarge.Length == 0)
            {
                return BadRequest(new { message = "L'image pour ordinateur est obligatoire." });
            }

            faute = VerifierFichier(imageLarge, "L'image pour ordinateur")
                 ?? VerifierFichier(imageMobile, "L'image pour téléphone");

            if (faute is not null) return BadRequest(new { message = faute });

            var promo = await _promos.CreerAsync(
                titre.Trim(),
                texteAlternatif.Trim(),
                string.IsNullOrWhiteSpace(lien) ? null : lien.Trim(),
                await LireAsync(imageLarge, ct),
                imageLarge.ContentType,
                imageMobile is null ? null : await LireAsync(imageMobile, ct),
                imageMobile?.ContentType,
                pleineLargeur,
                ct);

            _logger.LogInformation("Bandeau promotionnel {Id} cree : {Titre}.", promo.Id, promo.Titre);

            // `Location` pointe la bibliothèque et non la ressource : il
            // n'existe pas de route qui rende UN bandeau — seulement la liste
            // et les octets. Fabriquer une adresse vers `Tous` en y glissant
            // un identifiant produirait `/promos?id=7`, qui ne mène à rien de
            // particulier.
            return CreatedAtAction(nameof(Tous), null, Decrire(promo));
        }

        /// <summary>
        /// Modifie un bandeau. Les fichiers absents ne sont PAS effacés.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "EstSuperAdmin")]
        [RequestSizeLimit(2 * TailleMaxVideo + 16384)]
        [SwaggerResponse(200, "Le bandeau modifié.")]
        [SwaggerResponse(404, "Bandeau introuvable.")]
        public async Task<IActionResult> Modifier(
            int id,
            [FromForm] string titre,
            [FromForm] string texteAlternatif,
            [FromForm] string? lien,
            [FromForm] bool pleineLargeur,
            IFormFile? imageLarge,
            IFormFile? imageMobile,
            CancellationToken ct)
        {
            var faute = Verifier(titre, texteAlternatif, lien)
                     ?? VerifierFichier(imageLarge, "L'image pour ordinateur")
                     ?? VerifierFichier(imageMobile, "L'image pour téléphone");

            if (faute is not null) return BadRequest(new { message = faute });

            var promo = await _promos.ModifierAsync(
                id,
                titre.Trim(),
                texteAlternatif.Trim(),
                string.IsNullOrWhiteSpace(lien) ? null : lien.Trim(),
                imageLarge is null ? null : await LireAsync(imageLarge, ct),
                imageLarge?.ContentType,
                imageMobile is null ? null : await LireAsync(imageMobile, ct),
                imageMobile?.ContentType,
                pleineLargeur,
                ct);

            return promo is null ? NotFound() : Ok(Decrire(promo));
        }

        /// <summary>
        /// Affiche ce bandeau, ou le retire. L'afficher éteint les autres.
        /// </summary>
        [HttpPut("{id:int}/affichage")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Affichage enregistré.")]
        [SwaggerResponse(404, "Bandeau introuvable.")]
        public async Task<IActionResult> Afficher(
            int id, [FromBody] AffichageRequest requete, CancellationToken ct)
        {
            var actif = requete?.Actif ?? false;

            if (!await _promos.DefinirAffichageAsync(id, actif, ct)) return NotFound();

            _logger.LogWarning(
                "Bandeau promotionnel {Id} {Etat}.", id, actif ? "AFFICHE" : "RETIRE");

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "EstSuperAdmin")]
        [SwaggerResponse(204, "Bandeau supprimé.")]
        [SwaggerResponse(404, "Bandeau introuvable.")]
        public async Task<IActionResult> Supprimer(int id, CancellationToken ct) =>
            await _promos.SupprimerAsync(id, ct) ? NoContent() : NotFound();

        // ------------------------------------------------------------ outils

        /// <summary>
        /// Ce que l'administration reçoit : tout, sauf les octets.
        /// </summary>
        private static object Decrire(Domain.Models.Promo p) => new
        {
            p.Id,
            p.Titre,
            p.TexteAlternatif,
            p.Lien,
            p.Actif,
            p.PleineLargeur,
            p.EstVideo,
            p.AvecImageMobile,
            p.TailleLarge,
            p.TailleMobile,
            p.DateCreation,
            p.DateModification,
            version = p.Version.Ticks,
        };

        /// <summary>
        /// Les règles du texte. Elles rendent le premier message de refus, pas
        /// la liste : un formulaire à trois champs se corrige un champ à la
        /// fois, et une liste d'erreurs se lit moins bien qu'une phrase.
        /// </summary>
        private static string? Verifier(string? titre, string? texteAlternatif, string? lien)
        {
            if (string.IsNullOrWhiteSpace(titre))
            {
                return "Donnez un nom au bandeau, pour le retrouver plus tard.";
            }

            // OBLIGATOIRE, ET LA RÈGLE EST ICI PLUTÔT QUE DANS L'ÉCRAN.
            // Toute la promotion est dans l'image : le prix, la date, la
            // remise. Sans ce texte, un visiteur aveugle ou un visiteur dont
            // l'image ne charge pas n'a rigoureusement rien.
            if (string.IsNullOrWhiteSpace(texteAlternatif))
            {
                return "Décrivez l'offre en une phrase : c'est ce que lisent les "
                     + "personnes aveugles, et ce qui s'affiche si l'image ne charge pas.";
            }

            if (string.IsNullOrWhiteSpace(lien)) return null;

            // UN LIEN INTERNE OU UNE ADRESSE COMPLÈTE, RIEN D'AUTRE.
            //
            // `javascript:` dans un `href` exécute du code chez le visiteur.
            // Le champ est réservé au super-administrateur, mais une porte
            // fermée par le seul fait que personne n'y touche n'est pas une
            // porte fermée.
            var propre = lien.Trim();

            var valide = propre.StartsWith('/')
                || propre.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || propre.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

            return valide
                ? null
                : "Le lien doit commencer par « / » pour une page du site, ou par "
                + "« https:// » pour un site extérieur.";
        }

        /// <summary>
        /// Les règles du fichier : format, puis poids.
        ///
        /// DANS CET ORDRE, ET IL COMPTE. Le plafond dépend du format — une
        /// vidéo a droit à quatre fois plus qu'une image. Mesurer avant de
        /// savoir ce qu'on tient obligerait à prendre le plus grand des deux
        /// plafonds, et laisserait passer une image de 18 Mo.
        /// </summary>
        private static string? VerifierFichier(IFormFile? fichier, string quoi)
        {
            if (fichier is null || fichier.Length == 0) return null;

            var video = EstVideo(fichier.ContentType);

            var accepte = video
                ? TypesVideoAcceptes.Contains(fichier.ContentType, StringComparer.OrdinalIgnoreCase)
                : TypesAcceptes.Contains(fichier.ContentType, StringComparer.OrdinalIgnoreCase);

            if (!accepte)
            {
                return $"{quoi} doit être une image (JPEG, PNG, WebP, GIF) ou "
                     + "une vidéo MP4 ou WebM.";
            }

            var plafond = video ? TailleMaxVideo : TailleMax;

            return fichier.Length > plafond
                ? $"{quoi} pèse {fichier.Length / (1024 * 1024)} Mo : la limite est de "
                  + $"{plafond / (1024 * 1024)} Mo pour {(video ? "une vidéo" : "une image")}."
                : null;
        }

        private static async Task<byte[]> LireAsync(IFormFile fichier, CancellationToken ct)
        {
            using var flux = new MemoryStream();
            await fichier.CopyToAsync(flux, ct);

            return flux.ToArray();
        }

        public class AffichageRequest
        {
            public bool Actif { get; set; }
        }
    }
}
