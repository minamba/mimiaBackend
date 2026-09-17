using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Utils;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Le carnet d'idées d'évolution — voulu par Camara le 17/09/2026.
    ///
    /// ENTIÈREMENT RÉSERVÉ À L'ADMINISTRATION. Ce n'est pas une boîte à idées
    /// ouverte aux parents : c'est le carnet de celui qui construit le produit,
    /// et on y lit des choses qui n'ont pas à être publiques — ce qui manque,
    /// ce qui ne marche pas, ce qui est abandonné.
    ///
    /// LES OCTETS ONT LEUR PROPRE ROUTE, comme pour les bandeaux promo : une
    /// capture d'écran pèse un mégaoctet, le texte d'une idée deux cents
    /// octets. Les mêler ferait payer les images à chaque affichage du tableau.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EstAdmin")]
    [Route("admin/idees")]
    public class IdeesController : ControllerBase
    {
        /// <summary>
        /// Le poids maximal d'une image, la même borne que les bandeaux.
        ///
        /// Une capture d'écran pèse 200 Ko à 2 Mo. Cinq mégaoctets laissent
        /// passer un PNG plein écran non compressé sans obliger à comprendre
        /// pourquoi le fichier est refusé.
        /// </summary>
        private const int TailleMax = 5 * 1024 * 1024;

        /// <summary>
        /// Le poids maximal d'un audio ou d'un document — Camara, le 17/09/2026.
        ///
        /// Cinq mégaoctets suffisent à une capture d'écran, pas à deux minutes
        /// de dictaphone ni à un PDF de maquettes. Vingt tiennent l'un et
        /// l'autre, et restent loin de ce qui ferait grossir la base : le carnet
        /// compte des dizaines d'idées, pas des milliers.
        /// </summary>
        private const int TailleMaxLourde = 20 * 1024 * 1024;

        /// <summary>
        /// Les types acceptés, en liste blanche.
        ///
        /// UNE LISTE BLANCHE ET NON UN FILTRE SUR L'EXTENSION : le nom de
        /// fichier vient du navigateur, donc de l'utilisateur. Le type MIME est
        /// ensuite VÉRIFIÉ contre la signature du fichier — un `.png` renommé
        /// ne suffit pas à faire entrer autre chose.
        /// </summary>
        private static readonly string[] TypesImages =
            ["image/png", "image/jpeg", "image/gif", "image/webp"];

        /// <summary>
        /// Les audios, pour être écoutés sur place.
        ///
        /// LEUR SIGNATURE N'EST PAS VÉRIFIÉE, contrairement aux images, et c'est
        /// assumé : un conteneur audio se reconnaît à un entête qui varie selon
        /// l'encodeur, et se tromper ferait refuser des fichiers valides. Le
        /// risque est nul par ailleurs : ces octets partent avec un
        /// `Content-Disposition` et un type que le navigateur ne peut pas
        /// exécuter, et l'accès est réservé aux administrateurs.
        /// </summary>
        private static readonly string[] TypesAudio =
        [
            "audio/mpeg", "audio/mp3", "audio/mp4", "audio/m4a", "audio/x-m4a",
            "audio/aac", "audio/ogg", "audio/wav", "audio/x-wav", "audio/webm",
            "audio/flac", "audio/x-flac",
        ];

        /// <summary>
        /// Les documents, joints pour être téléchargés.
        ///
        /// CE QUI N'Y EST PAS EST AUSSI IMPORTANT QUE CE QUI Y EST : ni
        /// `text/html`, ni `image/svg+xml`, ni aucun script. Ces trois-là
        /// s'exécutent quand un navigateur les ouvre, et une pièce jointe
        /// téléchargée finit toujours par être ouverte. Le reste de la liste ne
        /// s'exécute pas tout seul.
        /// </summary>
        private static readonly string[] TypesDocuments =
        [
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "text/plain", "text/csv", "application/rtf", "application/zip",
        ];

        private readonly IIdeeRepository _idees;
        private readonly ICurrentUserAccessor _utilisateur;
        private readonly ITelegramService _telegram;
        private readonly ILogger<IdeesController> _logger;

        public IdeesController(
            IIdeeRepository idees,
            ICurrentUserAccessor utilisateur,
            ITelegramService telegram,
            ILogger<IdeesController> logger)
        {
            _idees = idees ?? throw new ArgumentNullException(nameof(idees));
            _utilisateur = utilisateur ?? throw new ArgumentNullException(nameof(utilisateur));
            _telegram = telegram ?? throw new ArgumentNullException(nameof(telegram));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public record IdeeRequete(string? Titre, string? Urgence, string? Description, string? Statut);

        [HttpGet]
        public async Task<IActionResult> Lister(CancellationToken ct)
            => Ok(await _idees.GetToutesAsync(ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Lire(int id, CancellationToken ct)
        {
            var idee = await _idees.GetAsync(id, ct);
            return idee is null ? NotFound() : Ok(idee);
        }

        [HttpPost]
        public async Task<IActionResult> Creer([FromBody] IdeeRequete requete, CancellationToken ct)
        {
            var titre = requete.Titre?.Trim();
            if (string.IsNullOrWhiteSpace(titre))
            {
                return BadRequest(new { message = "Une idée a besoin d'un titre." });
            }

            if (!UrgenceIdee.EstConnue(requete.Urgence))
            {
                return BadRequest(new { message = "Niveau d'urgence inconnu." });
            }

            // SIGNÉE AU MOMENT OÙ ELLE EST NOTÉE. Le prénom et le nom viennent du
            // jeton, pas de la base : ils y sont déjà, et une lecture de plus par
            // idée créée ne dirait rien que le jeton ne dise. Recopiés plutôt que
            // rattachés par une clé — voir `IdeeEvolution`.
            var idee = await _idees.CreerAsync(
                titre, requete.Urgence!, requete.Description,
                _utilisateur.Prenom, _utilisateur.Nom, ct);

            _logger.LogInformation("Idée créée : {Id} — {Titre}.", idee.Id, idee.Titre);

            // APRÈS L'ÉCRITURE EN BASE, comme pour les signalements : une panne de
            // Telegram ne doit jamais faire perdre l'idée elle-même, qui est déjà
            // en sécurité dans la table.
            //
            // ET SEULEMENT À LA CRÉATION. Une idée se modifie beaucoup — un statut
            // avancé d'un cran, une phrase reprise — et notifier chaque
            // enregistrement noierait le salon sous des messages que personne ne
            // lirait plus.
            var auteur = string.Join(' ', new[] { idee.AuteurPrenom, idee.AuteurNom }
                .Where(m => !string.IsNullOrWhiteSpace(m)));

            await _telegram.NotifierIdeeAsync(
                idee.Titre, idee.Urgence, idee.Description, auteur);

            return CreatedAtAction(nameof(Lire), new { id = idee.Id }, idee);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Modifier(
            int id, [FromBody] IdeeRequete requete, CancellationToken ct)
        {
            var titre = requete.Titre?.Trim();
            if (string.IsNullOrWhiteSpace(titre))
            {
                return BadRequest(new { message = "Une idée a besoin d'un titre." });
            }

            if (!UrgenceIdee.EstConnue(requete.Urgence))
            {
                return BadRequest(new { message = "Niveau d'urgence inconnu." });
            }

            // LE STATUT SE VÉRIFIE ICI, PAS SEULEMENT DANS LA LISTE DÉROULANTE.
            // Une valeur inconnue en base ne casserait rien tout de suite : elle
            // s'afficherait simplement sans couleur, et on chercherait longtemps.
            if (!StatutIdee.EstConnu(requete.Statut))
            {
                return BadRequest(new { message = "Statut inconnu." });
            }

            var idee = await _idees.ModifierAsync(
                id, titre, requete.Urgence!, requete.Description, requete.Statut!, ct);

            return idee is null ? NotFound() : Ok(idee);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Supprimer(int id, CancellationToken ct)
        {
            var supprimee = await _idees.SupprimerAsync(id, ct);

            if (supprimee) _logger.LogInformation("Idée supprimée : {Id}.", id);

            return supprimee ? NoContent() : NotFound();
        }

        /// <summary>
        /// Attache un fichier à une idée : image, audio ou document.
        ///
        /// TROIS GENRES, UNE SEULE ROUTE — Camara, le 17/09/2026. Ce sont la
        /// même chose ici : des octets accrochés à une idée. Ce qui les sépare
        /// est ce qu'on en fait à l'écran, et ça se déduit du type mime :
        /// l'image s'affiche dans le texte, l'audio s'écoute sur place, le reste
        /// se télécharge.
        ///
        /// Renvoie la pièce créée, avec son rang — le nombre à citer dans
        /// `[image:N]` — qui vaut 0 quand ce n'est pas une image.
        /// </summary>
        [HttpPost("{id:int}/pieces")]
        [RequestSizeLimit(TailleMaxLourde + 1024)]
        public async Task<IActionResult> AjouterPiece(
            int id, IFormFile fichier, CancellationToken ct)
        {
            if (fichier is null || fichier.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier reçu." });
            }

            var type = fichier.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;

            var estImage = TypesImages.Contains(type);
            var estAudio = TypesAudio.Contains(type);
            var estDocument = TypesDocuments.Contains(type);

            if (!estImage && !estAudio && !estDocument)
            {
                return BadRequest(new
                {
                    message = "Format refusé. Acceptés : images, audio, PDF, "
                           + "Word, Excel, PowerPoint, texte, CSV, ZIP.",
                });
            }

            // DEUX BORNES, PARCE QUE DEUX USAGES. Une capture tient dans cinq
            // mégaoctets ; deux minutes de dictaphone, non. Garder la borne des
            // images pour tout aurait fait refuser l'audio que Camara vient
            // justement de demander.
            var borne = estImage ? TailleMax : TailleMaxLourde;

            if (fichier.Length > borne)
            {
                return BadRequest(new
                {
                    message = $"Le fichier dépasse {borne / (1024 * 1024)} Mo.",
                });
            }

            using var memoire = new MemoryStream();
            await fichier.CopyToAsync(memoire, ct);
            var donnees = memoire.ToArray();

            // LE TYPE ANNONCÉ NE SUFFIT PAS. `ContentType` vient du navigateur :
            // il se change. On lit donc les premiers octets, qui eux ne mentent
            // pas — c'est ce qui empêche un script déguisé en PNG d'entrer.
            //
            // Vérifié sur les images SEULEMENT, parce qu'elles sont les seules à
            // être rendues DANS la page. Un audio et un document partent en
            // téléchargement, avec un type que le navigateur ne peut pas
            // exécuter.
            if (estImage && !SignatureEstImage(donnees))
            {
                return BadRequest(new { message = "Ce fichier n'est pas une image." });
            }

            var piece = await _idees.AjouterPieceAsync(
                id, NomPropre(fichier.FileName), type, donnees, ct);

            return piece is null ? NotFound() : Ok(piece);
        }

        /// <summary>
        /// Le nom du fichier, ramené à son dernier segment et borné.
        ///
        /// IL VIENT DU NAVIGATEUR. Un nom contenant `..\` ou un chemin complet
        /// n'irait nulle part — rien n'écrit sur le disque — mais il ressortirait
        /// tel quel dans l'en-tête de téléchargement et dans la liste des pièces.
        /// On le coupe ici, une fois, plutôt que de se demander à chaque
        /// affichage.
        /// </summary>
        private static string NomPropre(string? brut)
        {
            var nom = Path.GetFileName(brut?.Trim() ?? string.Empty);

            if (string.IsNullOrWhiteSpace(nom)) return "piece-jointe";

            return nom.Length > 180 ? nom[^180..] : nom;
        }

        /// <summary>
        /// Retire une pièce. Renvoie la description, RÉÉCRITE si c'était une
        /// image : les marqueurs des suivantes ont reculé d'un rang.
        /// </summary>
        [HttpDelete("{id:int}/pieces/{pieceId:int}")]
        public async Task<IActionResult> RetirerPiece(int id, int pieceId, CancellationToken ct)
        {
            var description = await _idees.RetirerPieceAsync(id, pieceId, ct);
            return description is null ? NotFound() : Ok(new { description });
        }

        /// <summary>Les octets d'une pièce, un fichier à la fois.</summary>
        [HttpGet("{id:int}/pieces/{pieceId:int}")]
        public async Task<IActionResult> LirePiece(int id, int pieceId, CancellationToken ct)
        {
            var piece = await _idees.GetPieceAsync(id, pieceId, ct);
            if (piece is null) return NotFound();

            // Les octets d'une pièce ne changent jamais : une fois téléchargée,
            // elle n'a aucune raison d'être redemandée pendant l'heure qui suit.
            Response.Headers.CacheControl = "private, max-age=3600";

            var genre = Domain.Models.GenrePiece.Deduire(piece.TypeMime);

            // LE DOCUMENT PART EN TÉLÉCHARGEMENT, PAS DANS LA PAGE.
            //
            // Passer un nom de fichier à `File` pose un `Content-Disposition:
            // attachment` : le navigateur enregistre au lieu d'ouvrir. C'est ce
            // qui empêche un fichier au type mal deviné de s'exécuter dans
            // l'origine du site, et c'est aussi ce que Camara demande — « il sera
            // juste mis en pj téléchargeable ».
            //
            // L'image et l'audio, eux, sont RENDUS dans la page — une balise
            // `img`, un lecteur — donc sans nom de fichier : le navigateur les
            // affiche en place.
            return genre == Domain.Models.GenrePiece.Document
                ? File(piece.Donnees, piece.TypeMime, piece.NomFichier)
                : File(piece.Donnees, piece.TypeMime);
        }

        /// <summary>
        /// Les premiers octets disent le format, quel que soit le nom du
        /// fichier ou le type annoncé.
        /// </summary>
        private static bool SignatureEstImage(byte[] o)
        {
            if (o.Length < 12) return false;

            // PNG : 89 50 4E 47
            if (o[0] == 0x89 && o[1] == 0x50 && o[2] == 0x4E && o[3] == 0x47) return true;

            // JPEG : FF D8 FF
            if (o[0] == 0xFF && o[1] == 0xD8 && o[2] == 0xFF) return true;

            // GIF : « GIF8 »
            if (o[0] == 0x47 && o[1] == 0x49 && o[2] == 0x46 && o[3] == 0x38) return true;

            // WebP : « RIFF » …… « WEBP »
            if (o[0] == 0x52 && o[1] == 0x49 && o[2] == 0x46 && o[3] == 0x46
                && o[8] == 0x57 && o[9] == 0x45 && o[10] == 0x42 && o[11] == 0x50) return true;

            return false;
        }
    }
}
