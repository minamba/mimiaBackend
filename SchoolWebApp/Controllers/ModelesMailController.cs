using System.Globalization;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Les templates de courriel de l'administration.
    ///
    /// UN TEMPLATE CHOISI S'ENREGISTRE À CHAQUE MODIFICATION — Camara, le
    /// 15/09/2026. D'où la forme des routes : le texte part en JSON à chaque
    /// pause de frappe (`PUT`), les pièces une à une au moment où on les
    /// ajoute ou les retire. Renvoyer tout le formulaire, images comprises,
    /// toutes les huit cents millisecondes ferait remonter des mégaoctets pour
    /// une virgule.
    ///
    /// L'ENVOI RELIT LES PIÈCES EN BASE. Un template choisi n'a pas à renvoyer
    /// ses images depuis le navigateur : le serveur les a, et ce qu'il envoie
    /// est exactement ce qu'il a enregistré.
    ///
    /// DEUX NATURES. Une diffusion s'envoie d'ici, à la main. Un courriel
    /// automatique se programme (`planification`) et part tout seul, par
    /// `EnvoisAutomatiquesWorker` — s'il est relié à une règle d'envoi. Ceux
    /// qu'on crée depuis l'écran n'en ont pas encore.
    ///
    /// Les anciennes routes multipart de `AdminController` (`diffusion`,
    /// `diffusion/apercu`, `mails/parent`) restent : elles servent l'aperçu
    /// d'un message qui n'est pas encore un template.
    /// </summary>
    [ApiController]
    [Route("admin/modeles-mail")]
    [Authorize(Policy = "EstAdmin")]
    public class ModelesMailController : ControllerBase
    {
        /// <summary>
        /// Cinq mégaoctets pour TOUT le courriel, la même borne que la
        /// diffusion : au-delà, beaucoup de messageries le refusent.
        /// </summary>
        private const int PoidsMax = 5 * 1024 * 1024;

        /// <summary>Ce que pèsent les champs texte autour des fichiers.</summary>
        private const int MargeFormulaire = 256 * 1024;

        private const int LongueurTexteMax = 20_000;

        private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

        private static readonly string[] ImagesAcceptees =
            ["image/jpeg", "image/png", "image/webp", "image/gif"];

        /// <summary>
        /// Les documents qu'on refuse de joindre. UNE LISTE NOIRE, et c'est un
        /// choix : la diffusion acceptait tout jusqu'ici — PDF, tableurs,
        /// formats de traitement de texte variés — et une liste blanche
        /// refuserait demain un format légitime oublié. Ce qu'on écarte, ce
        /// sont les fichiers qui s'exécutent ou s'ouvrent comme une page : les
        /// messageries les bloquent, et le parent recevrait un courriel amputé.
        /// </summary>
        private static readonly string[] ExtensionsRefusees =
            [".exe", ".bat", ".cmd", ".com", ".msi", ".scr", ".ps1", ".vbs", ".js", ".jar", ".html", ".htm", ".svg"];

        private readonly IModeleMailRepository _modeles;
        private readonly IComposeurModeleMail _composeur;
        private readonly IDiffusionService _diffusion;
        private readonly OptionsEmail _options;
        private readonly ILogger<ModelesMailController> _logger;

        public ModelesMailController(
            IModeleMailRepository modeles,
            IComposeurModeleMail composeur,
            IDiffusionService diffusion,
            IOptions<OptionsEmail> options,
            ILogger<ModelesMailController> logger)
        {
            _modeles = modeles ?? throw new ArgumentNullException(nameof(modeles));
            _composeur = composeur ?? throw new ArgumentNullException(nameof(composeur));
            _diffusion = diffusion ?? throw new ArgumentNullException(nameof(diffusion));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------------------------------------------------ lecture

        /// <summary>Les templates d'une nature, sans texte ni octets.</summary>
        [HttpGet]
        [SwaggerResponse(200, "La liste des templates.")]
        public async Task<IActionResult> Lister([FromQuery] string? nature, CancellationToken ct)
        {
            var choisie = NatureModeleMail.EstConnue(nature) ? nature! : NatureModeleMail.Diffusion;
            var resumes = await _modeles.GetResumesAsync(choisie, ct);

            return Ok(resumes.Select(DecrireResume));
        }

        [HttpGet("{id:int}")]
        [SwaggerResponse(200, "Le template, texte et pièces (sans octets) compris.")]
        [SwaggerResponse(404, "Template introuvable.")]
        public async Task<IActionResult> Lire(int id, CancellationToken ct)
        {
            var detail = await _modeles.GetDetailAsync(id, ct);
            return detail is null ? NotFound() : Ok(DecrireDetail(detail));
        }

        /// <summary>Les octets d'une pièce, pour la télécharger depuis l'écran.</summary>
        [HttpGet("{id:int}/pieces/{pieceId:int}")]
        [SwaggerResponse(200, "Le fichier.")]
        [SwaggerResponse(404, "Pièce introuvable.")]
        public async Task<IActionResult> LirePiece(int id, int pieceId, CancellationToken ct)
        {
            var piece = await _modeles.GetPieceAsync(id, pieceId, ct);

            return piece is null
                ? NotFound()
                : File(piece.Donnees, string.IsNullOrWhiteSpace(piece.TypeMime)
                    ? "application/octet-stream"
                    : piece.TypeMime, piece.NomFichier);
        }

        // ----------------------------------------------------------- écriture

        /// <summary>
        /// Crée un template, pièces comprises : une diffusion (aussi appelée par
        /// « Envoyer à tous les parents » quand le message n'avait pas été
        /// enregistré), ou un courriel automatique — Camara, le 15/09/2026 —,
        /// qui naît sans règle d'envoi.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(PoidsMax + MargeFormulaire)]
        [SwaggerResponse(201, "Le template créé.")]
        [SwaggerResponse(400, "Nom ou objet manquant, texte trop long ou fichier refusé.")]
        [SwaggerResponse(413, "Pièces trop lourdes.")]
        public async Task<IActionResult> Creer(
            [FromForm] string? nature,
            [FromForm] string? nom,
            [FromForm] string? description,
            [FromForm] string? sujet,
            [FromForm] string? titre,
            [FromForm] string? texte,
            [FromForm] List<IFormFile>? images,
            [FromForm] List<IFormFile>? documents,
            CancellationToken ct)
        {
            var automatique = nature == NatureModeleMail.Automatique;

            // UN COURRIEL AUTOMATIQUE SE RECONNAÎT À SON NOM dans le menu ; une
            // diffusion, à son objet, qui lui sert de nom.
            if (automatique && string.IsNullOrWhiteSpace(nom))
            {
                return BadRequest(new { message = "Donnez un nom au courriel automatique." });
            }

            if (!automatique && string.IsNullOrWhiteSpace(sujet))
            {
                return BadRequest(new { message = "L'objet est obligatoire pour enregistrer un template." });
            }

            var faute = VerifierContenu(sujet, titre, texte, nom, description);
            if (faute is not null) return BadRequest(new { message = faute });

            var lesImages = new List<NouvellePieceMail>();
            var lesDocuments = new List<NouvellePieceMail>();

            foreach (var fichier in images ?? new List<IFormFile>())
            {
                var (piece, erreur) = await LireAsync(fichier, GenrePieceMail.Image, ct);
                if (erreur is not null) return BadRequest(new { message = erreur });
                if (piece is not null) lesImages.Add(piece);
            }

            foreach (var fichier in documents ?? new List<IFormFile>())
            {
                var (piece, erreur) = await LireAsync(fichier, GenrePieceMail.Document, ct);
                if (erreur is not null) return BadRequest(new { message = erreur });
                if (piece is not null) lesDocuments.Add(piece);
            }

            if (lesImages.Concat(lesDocuments).Sum(p => p.Donnees.Length) > PoidsMax)
            {
                return StatusCode(413, new { message = "Les pièces dépassent 5 Mo au total." });
            }

            var nomFinal = Tronquer(string.IsNullOrWhiteSpace(nom) ? sujet! : nom, 120);

            var detail = await _modeles.CreerAsync(
                automatique ? NatureModeleMail.Automatique : NatureModeleMail.Diffusion,
                nomFinal,
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                sujet?.Trim() ?? string.Empty,
                titre?.Trim() ?? string.Empty,
                texte ?? string.Empty,
                lesImages,
                lesDocuments,
                ct);

            _logger.LogInformation(
                "Template de courriel {Id} ({Nature}) cree : « {Nom} », {Images} image(s), {Docs} document(s).",
                detail.Id, detail.Nature, detail.Nom, lesImages.Count, lesDocuments.Count);

            return StatusCode(201, DecrireDetail(detail));
        }

        /// <summary>
        /// La sauvegarde automatique : nom, description, objet, titre, message.
        /// Les champs vides sont acceptés : on enregistre aussi un brouillon
        /// qu'on est en train d'effacer. Un nom vide, lui, ne remplace rien —
        /// un template sans nom ne se retrouverait plus dans la liste.
        /// </summary>
        [HttpPut("{id:int}")]
        [SwaggerResponse(200, "Enregistré.")]
        [SwaggerResponse(400, "Un champ dépasse sa longueur maximale.")]
        [SwaggerResponse(404, "Template introuvable.")]
        public async Task<IActionResult> Modifier(int id, [FromBody] ContenuRequest requete, CancellationToken ct)
        {
            if (requete is null) return BadRequest(new { message = "Contenu manquant." });

            var faute = VerifierContenu(
                requete.Sujet, requete.Titre, requete.Texte, requete.Nom, requete.Description);

            if (faute is not null) return BadRequest(new { message = faute });

            var date = await _modeles.ModifierContenuAsync(
                id,
                string.IsNullOrWhiteSpace(requete.Nom) ? null : requete.Nom.Trim(),
                requete.Description?.Trim(),
                requete.Sujet ?? string.Empty,
                requete.Titre ?? string.Empty,
                requete.Texte ?? string.Empty,
                ct);

            return date is null ? NotFound() : Ok(new { dateModification = Utc(date) });
        }

        /// <summary>
        /// Programme un courriel automatique — ou le déprogramme.
        ///
        /// JAMAIS RÉTROACTIF : l'occurrence déjà échue au moment du réglage est
        /// marquée comme prise. Programmer « tous les jours à 10 h » à 14 h
        /// n'envoie rien avant demain 10 h.
        /// </summary>
        [HttpPut("{id:int}/planification")]
        [SwaggerResponse(200, "Le template, planification à jour.")]
        [SwaggerResponse(400, "Règle incomplète, ou courriel qui ne se programme pas.")]
        [SwaggerResponse(404, "Template introuvable.")]
        public async Task<IActionResult> Planifier(
            int id, [FromBody] PlanificationRequest requete, CancellationToken ct)
        {
            if (requete is null) return BadRequest(new { message = "Planification manquante." });

            var detail = await _modeles.GetDetailAsync(id, ct);
            if (detail is null) return NotFound();

            if (detail.Nature != NatureModeleMail.Automatique)
            {
                return BadRequest(new { message = "Seul un courriel automatique se programme." });
            }

            if (CodeModeleMail.ReglesADefinir(detail.Code))
            {
                return BadRequest(new
                {
                    message = "Les règles de ce courriel ne sont pas encore définies : il ne peut pas être programmé.",
                });
            }

            if (!FrequenceEnvoi.EstConnue(requete.Frequence) || requete.Frequence == FrequenceEnvoi.Aucune)
            {
                return BadRequest(new { message = "Choisissez une fréquence : chaque jour, chaque semaine ou chaque mois." });
            }

            if (detail.Code == CodeModeleMail.Bilans && requete.Frequence != FrequenceEnvoi.Semaine)
            {
                return BadRequest(new
                {
                    message = "Les bilans sont écrits pour une semaine : ils ne se programment que chaque semaine.",
                });
            }

            if (!TimeOnly.TryParseExact(
                    requete.Heure, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var heure))
            {
                return BadRequest(new { message = "L’heure doit s’écrire HH:mm." });
            }

            var regle = new RegleEnvoi(
                requete.Frequence!,
                heure,
                requete.Frequence == FrequenceEnvoi.Semaine ? requete.JourSemaine : null,
                requete.Frequence == FrequenceEnvoi.Mois ? requete.JourMois : null);

            if (!Planification.EstComplete(regle))
            {
                return BadRequest(new { message = "Choisissez le jour de l’envoi." });
            }

            await _modeles.DefinirPlanificationAsync(
                id,
                regle.Frequence,
                heure,
                regle.JourSemaine,
                regle.JourMois,
                requete.Actif,
                Planification.OccurrencePrecedente(DateTime.UtcNow, regle),
                ct);

            _logger.LogWarning(
                "Courriel automatique {Code} {Etat} : {Frequence} a {Heure} (jour {JourSemaine}/{JourMois}).",
                detail.Code, requete.Actif ? "PROGRAMME" : "deprogramme",
                regle.Frequence, requete.Heure, regle.JourSemaine, regle.JourMois);

            var aJour = await _modeles.GetDetailAsync(id, ct);
            return Ok(DecrireDetail(aJour!));
        }

        /// <summary>Ajoute une image ou un document, au rang suivant de son genre.</summary>
        [HttpPost("{id:int}/pieces")]
        [RequestSizeLimit(PoidsMax + MargeFormulaire)]
        [SwaggerResponse(201, "La pièce ajoutée.")]
        [SwaggerResponse(400, "Genre inconnu ou fichier refusé.")]
        [SwaggerResponse(404, "Template introuvable.")]
        [SwaggerResponse(413, "Le template dépasserait 5 Mo.")]
        public async Task<IActionResult> AjouterPiece(
            int id, [FromForm] string? genre, IFormFile? fichier, CancellationToken ct)
        {
            if (!GenrePieceMail.EstConnu(genre))
            {
                return BadRequest(new { message = "Le genre doit être « Image » ou « Document »." });
            }

            if (fichier is null || fichier.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier reçu." });
            }

            var modele = await _modeles.GetDetailAsync(id, ct);
            if (modele is null) return NotFound();

            // Deux cents bilans par semaine avec une image chacun, ce seraient
            // des centaines de mégaoctets sortants pour un ornement.
            if (modele.Code == CodeModeleMail.Bilans)
            {
                return BadRequest(new { message = "Les bilans ne portent ni image ni document." });
            }

            var (piece, erreur) = await LireAsync(fichier, genre!, ct);
            if (erreur is not null || piece is null) return BadRequest(new { message = erreur });

            var resultat = await _modeles.AjouterPieceAsync(id, genre!, piece, PoidsMax, ct);

            if (resultat.Introuvable) return NotFound();

            if (resultat.TropLourd)
            {
                return StatusCode(413, new
                {
                    message = $"« {piece.NomFichier} » ferait dépasser 5 Mo au total : au-delà, "
                              + "beaucoup de messageries refusent le courriel.",
                });
            }

            return StatusCode(201, DecrirePiece(resultat.Piece!));
        }

        /// <summary>
        /// Retire une pièce. Rend le template à jour : pour une image, le texte
        /// revient avec ses marqueurs renumérotés.
        /// </summary>
        [HttpDelete("{id:int}/pieces/{pieceId:int}")]
        [SwaggerResponse(200, "Le template après retrait.")]
        [SwaggerResponse(404, "Template introuvable.")]
        public async Task<IActionResult> RetirerPiece(int id, int pieceId, CancellationToken ct)
        {
            var detail = await _modeles.RetirerPieceAsync(id, pieceId, ct);
            return detail is null ? NotFound() : Ok(DecrireDetail(detail));
        }

        /// <summary>
        /// Supprime un template — diffusion ou courriel automatique. Les quatre
        /// courriels reliés à un envoi (bilans, fin d'essai, demandes d'avis)
        /// ne se suppriment pas : le code d'envoi n'aurait plus de texte.
        /// </summary>
        [HttpDelete("{id:int}")]
        [SwaggerResponse(204, "Supprimé.")]
        [SwaggerResponse(400, "Courriel relié à un envoi : il ne se supprime pas.")]
        [SwaggerResponse(404, "Introuvable.")]
        public async Task<IActionResult> Supprimer(int id, CancellationToken ct)
        {
            var detail = await _modeles.GetDetailAsync(id, ct);
            if (detail is null) return NotFound();

            if (CodeModeleMail.EstRelieAUnEnvoi(detail.Code))
            {
                return BadRequest(new
                {
                    message = "Ce courriel est relié à un envoi automatique : il se modifie ou se déprogramme, "
                              + "mais ne se supprime pas.",
                });
            }

            if (!await _modeles.SupprimerAsync(id, ct)) return NotFound();

            _logger.LogInformation("Template de courriel {Id} supprime : « {Nom} ».", id, detail.Nom);
            return NoContent();
        }

        // --------------------------------------------------- aperçu et envoi

        /// <summary>
        /// Le template rendu en HTML, par le même code que l'envoi, avec des
        /// valeurs d'exemple à la place des variables.
        /// </summary>
        [HttpPost("{id:int}/apercu")]
        [SwaggerResponse(200, "Le courriel rendu en HTML.")]
        [SwaggerResponse(404, "Template introuvable.")]
        public async Task<IActionResult> Apercu(
            int id,
            [FromServices] IServiceEmail email,
            [FromServices] IEnvoiBilansService bilans,
            CancellationToken ct)
        {
            var detail = await _modeles.GetDetailAsync(id, ct);
            if (detail is null) return NotFound();

            // LES BILANS SE COMPOSENT PAR ENFANT : l'aperçu montre un bilan
            // fictif, dans son vrai gabarit, avec l'objet et le mot
            // d'introduction du template — sans appel au modèle de rédaction.
            if (detail.Code == CodeModeleMail.Bilans)
            {
                return Content(await bilans.ApercuExempleAsync(ct), "text/html");
            }

            // Le lien de désabonnement apparaît dans l'aperçu là où il
            // apparaîtra chez le parent — il ne mène nulle part d'ici.
            var categorie = detail.Nature == NatureModeleMail.Diffusion
                ? CategorieDesabonnement.Diffusion
                : CodeModeleMail.Categorie(detail.Code);

            var courriel = _composeur.Composer(
                detail,
                await _modeles.GetPiecesAsync(id, ct),
                Exemples(),
                categorie is null ? null : _diffusion.ComposerMentionPied("#"));

            var html = await email.RendreAsync(courriel.Sujet, "diffusion", courriel.Valeurs);

            // Les images en base64 pour le navigateur, qui ne résout pas `cid:`.
            // GUILLEMET COMPRIS dans le motif, et rangs décroissants : sans
            // cela `cid:diffusion1` remplacerait aussi le début de
            // `cid:diffusion10`.
            foreach (var image in courriel.Images.Reverse())
            {
                html = html.Replace(
                    $"cid:{image.Reference}\"",
                    $"data:{image.TypeMime};base64,{Convert.ToBase64String(image.Donnees)}\"");
            }

            return Content(html, "text/html");
        }

        /// <summary>
        /// Lance la diffusion du template à TOUS les parents, en arrière-plan —
        /// le même service et le même verrou que la diffusion d'un message non
        /// enregistré.
        /// </summary>
        [HttpPost("{id:int}/diffusion")]
        [SwaggerResponse(202, "Diffusion lancée.")]
        [SwaggerResponse(400, "Objet ou message vide, ou courriel automatique.")]
        [SwaggerResponse(404, "Template introuvable.")]
        [SwaggerResponse(409, "Une diffusion est déjà en cours.")]
        public async Task<IActionResult> Diffuser(int id, CancellationToken ct)
        {
            var detail = await _modeles.GetDetailAsync(id, ct);
            if (detail is null) return NotFound();

            var faute = VerifierEnvoi(detail);
            if (faute is not null) return BadRequest(new { message = faute });

            var courriel = _composeur.Composer(detail, await _modeles.GetPiecesAsync(id, ct));

            // Le sujet et le titre partent BRUTS : la diffusion remplace
            // `{{prenom}}` pour chaque parent.
            var titre = string.IsNullOrWhiteSpace(detail.Titre) ? detail.Sujet : detail.Titre;

            if (!_diffusion.Lancer(detail.Sujet, titre, detail.Texte, courriel.Images, courriel.Documents))
            {
                return Conflict(new { message = "Une diffusion est déjà en cours. Attendez qu'elle se termine." });
            }

            await _modeles.NoterEnvoiAsync(id, DateTime.UtcNow, "Diffusion à tous les parents", ct);

            _logger.LogWarning(
                "DIFFUSION LANCEE depuis le template {Id} : « {Sujet} », {Images} image(s), {Docs} document(s).",
                id, detail.Sujet, courriel.Images.Count, courriel.Documents.Count);

            return Accepted(new { message = "Diffusion lancée." });
        }

        /// <summary>Envoie le template à UN parent, dans le temps de la requête.</summary>
        [HttpPost("{id:int}/envoi-parent")]
        [SwaggerResponse(200, "Message envoyé.")]
        [SwaggerResponse(400, "Adresse invalide ou bannie, objet ou message vide.")]
        [SwaggerResponse(404, "Template introuvable.")]
        [SwaggerResponse(502, "L'envoi a échoué.")]
        public async Task<IActionResult> EnvoyerAUnParent(
            int id,
            [FromBody] EnvoiParentRequest requete,
            [FromServices] IServiceEmail email,
            [FromServices] IBannissementRepository bannis,
            CancellationToken ct)
        {
            var adresse = requete?.Destinataire?.Trim() ?? string.Empty;

            try
            {
                _ = new MailAddress(adresse);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "L'adresse du parent n'est pas valide." });
            }

            // Le service d'envoi l'écarterait sans bruit ; ici on le DIT, plutôt
            // qu'un « l'envoi a échoué » qui ferait chercher une panne.
            if (await bannis.EstBanniAsync(adresse, ct))
            {
                return BadRequest(new { message = "Cette adresse est bannie : elle ne reçoit plus aucun courriel." });
            }

            var detail = await _modeles.GetDetailAsync(id, ct);
            if (detail is null) return NotFound();

            var faute = VerifierEnvoi(detail);
            if (faute is not null) return BadRequest(new { message = faute });

            // Une adresse tapée à la main n'a pas de prénom connu : la variable
            // disparaît proprement (« Bonjour, »).
            var courriel = _composeur.Composer(
                detail,
                await _modeles.GetPiecesAsync(id, ct),
                new Dictionary<string, ValeurVariable> { ["prenom"] = new(string.Empty) });

            var resultat = await email.DiffuserAsync(
                new[] { adresse }, courriel.Sujet, "diffusion", courriel.Valeurs,
                courriel.Images, courriel.Documents, ct: ct);

            if (resultat.Envoyes == 0)
            {
                return StatusCode(502, new { message = "Le message n'a pas pu être envoyé." });
            }

            // L'adresse n'est pas recopiée dans le résultat affiché : la liste
            // des templates n'a pas à devenir un carnet d'adresses.
            await _modeles.NoterEnvoiAsync(id, DateTime.UtcNow, "Envoyé à un parent", ct);

            _logger.LogInformation(
                "Template {Id} envoye a un parent : « {Sujet} ».", id, courriel.Sujet);

            return Ok(new { message = "Message envoyé." });
        }

        // ------------------------------------------------------------ outils

        private string Site() => (_options.UrlSite ?? "https://mimia.fr").TrimEnd('/');

        /// <summary>Des valeurs vraisemblables pour l'aperçu : « Bonjour Camille, ».</summary>
        private IReadOnlyDictionary<string, ValeurVariable> Exemples()
        {
            var site = Site();
            var maintenant = HeureFrance.Locale(DateTime.UtcNow);

            return new Dictionary<string, ValeurVariable>
            {
                ["prenom"] = new("Camille"),
                ["prenomEnfant"] = new("Léo"),
                ["dateDebut"] = new(maintenant.AddDays(-7).ToString("d MMMM", Francais)),
                ["dateFinEssai"] = new(maintenant.AddDays(1).ToString("dddd d MMMM", Francais)),
                ["lienTarifs"] = new("voir les formules", $"{site}/tarifs"),
                ["lienAvis"] = new("donner mon avis", $"{site}{EnvoisAutomatiquesService.CheminAvis}"),
            };
        }

        /// <summary>Les variables qu'un template peut employer, pour la légende de l'écran.</summary>
        private static IReadOnlyList<object> Variables(string nature, string? code)
        {
            static object V(string cle, string description) => new { cle, description };

            if (nature == NatureModeleMail.Diffusion)
            {
                return [V("prenom", "Le prénom du parent")];
            }

            return code switch
            {
                CodeModeleMail.Bilans =>
                    [V("prenomEnfant", "Le prénom de l’enfant"), V("dateDebut", "Le premier jour de la semaine du bilan")],
                CodeModeleMail.FinEssai =>
                    [V("prenom", "Le prénom du parent"), V("dateFinEssai", "Le jour où l’essai se termine"),
                     V("lienTarifs", "Le lien vers les formules")],
                CodeModeleMail.AvisEssai or CodeModeleMail.AvisGeneral =>
                    [V("prenom", "Le prénom du parent"), V("lienAvis", "Le lien pour donner son avis")],
                _ => [V("prenom", "Le prénom du parent")],
            };
        }

        private static string? VerifierEnvoi(ModeleMailDetail detail)
        {
            if (detail.Nature != NatureModeleMail.Diffusion)
            {
                return "Un courriel automatique part tout seul, à l'heure programmée.";
            }

            return string.IsNullOrWhiteSpace(detail.Sujet) || string.IsNullOrWhiteSpace(detail.Texte)
                ? "L'objet et le message sont obligatoires."
                : null;
        }

        private static string? VerifierContenu(
            string? sujet, string? titre, string? texte, string? nom, string? description)
        {
            if ((sujet?.Length ?? 0) > 150) return "L'objet dépasse 150 caractères.";
            if ((titre?.Length ?? 0) > 150) return "Le titre dépasse 150 caractères.";
            if ((texte?.Length ?? 0) > LongueurTexteMax) return "Le message dépasse 20 000 caractères.";
            if ((nom?.Trim().Length ?? 0) > 120) return "Le nom du template dépasse 120 caractères.";
            if ((description?.Trim().Length ?? 0) > 500) return "La description dépasse 500 caractères.";

            return null;
        }

        /// <summary>
        /// Lit un fichier reçu et vérifie qu'on peut le mettre dans un courriel.
        ///
        /// UNE IMAGE EST VÉRIFIÉE PAR SES PREMIERS OCTETS, pas seulement par le
        /// type annoncé : ce type vient du navigateur, qui le déduit de
        /// l'extension. Un fichier renommé en `.png` passerait sinon, et la
        /// messagerie du parent afficherait un cadre cassé.
        /// </summary>
        private static async Task<(NouvellePieceMail? Piece, string? Erreur)> LireAsync(
            IFormFile fichier, string genre, CancellationToken ct)
        {
            if (fichier.Length == 0) return (null, null);

            var nom = Path.GetFileName(fichier.FileName);

            if (fichier.Length > PoidsMax)
            {
                return (null, $"« {nom} » dépasse 5 Mo.");
            }

            if (genre == GenrePieceMail.Document
                && ExtensionsRefusees.Contains(Path.GetExtension(nom), StringComparer.OrdinalIgnoreCase))
            {
                return (null, $"« {nom} » ne peut pas être joint : ce type de fichier est bloqué par les messageries.");
            }

            using var flux = new MemoryStream();
            await fichier.CopyToAsync(flux, ct);
            var octets = flux.ToArray();

            var typeMime = string.IsNullOrWhiteSpace(fichier.ContentType)
                ? "application/octet-stream"
                : fichier.ContentType;

            if (genre == GenrePieceMail.Image
                && (!ImagesAcceptees.Contains(typeMime, StringComparer.OrdinalIgnoreCase) || !EstUneImage(octets)))
            {
                return (null, $"« {nom} » n'est pas une image JPEG, PNG, WebP ou GIF.");
            }

            return (new NouvellePieceMail(nom, typeMime, octets), null);
        }

        private static bool EstUneImage(byte[] o) =>
            (o.Length > 3 && o[0] == 0xFF && o[1] == 0xD8 && o[2] == 0xFF)
            || (o.Length > 8 && o[0] == 0x89 && o[1] == 0x50 && o[2] == 0x4E && o[3] == 0x47)
            || (o.Length > 6 && o[0] == 0x47 && o[1] == 0x49 && o[2] == 0x46 && o[3] == 0x38)
            || (o.Length > 12 && o[0] == 0x52 && o[1] == 0x49 && o[2] == 0x46 && o[3] == 0x46
                && o[8] == 0x57 && o[9] == 0x45 && o[10] == 0x42 && o[11] == 0x50);

        private static string Tronquer(string texte, int longueur)
        {
            var propre = texte.Trim();
            return propre.Length <= longueur ? propre : propre[..longueur];
        }

        /// <summary>
        /// Les dates partent marquées UTC. Lues sans fuseau par EF, elles
        /// seraient prises pour des heures locales par le navigateur, et
        /// « Enregistré à 14:32 » s'afficherait décalé de deux heures.
        /// </summary>
        private static DateTime? Utc(DateTime? date) =>
            date is null ? null : DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);

        /// <summary>Un courriel automatique sans règle d'envoi : il ne se programme pas.</summary>
        private static bool SansRegle(ModeleMailResume m) =>
            m.Nature == NatureModeleMail.Automatique && CodeModeleMail.ReglesADefinir(m.Code);

        /// <summary>Programmé et doté d'une règle d'envoi : il a une prochaine date.</summary>
        private static DateTime? Prochaine(ModeleMailResume m) =>
            m.Nature == NatureModeleMail.Automatique && !SansRegle(m)
                ? Utc(Planification.ProchaineExecution(
                    RegleEnvoi.De(m), m.Actif, m.DerniereOccurrence, DateTime.UtcNow))
                : null;

        private static object DecrireResume(ModeleMailResume m) => new
        {
            m.Id,
            m.Nature,
            m.Code,
            m.Nom,
            m.Description,
            m.Sujet,
            m.Frequence,
            heure = m.HeureEnvoi?.ToString("HH:mm"),
            m.JourSemaine,
            m.JourMois,
            m.Actif,
            reglesADefinir = SansRegle(m),
            supprimable = !CodeModeleMail.EstRelieAUnEnvoi(m.Code),
            prochaineExecution = Prochaine(m),
            dernierEnvoiLe = Utc(m.DernierEnvoiLe),
            m.DernierResultat,
            dateCreation = Utc(m.DateCreation),
            dateModification = Utc(m.DateModification),
            m.NombreImages,
            m.NombreDocuments,
            m.PoidsTotal,
        };

        private static object DecrireDetail(ModeleMailDetail d) => new
        {
            d.Id,
            d.Nature,
            d.Code,
            d.Nom,
            d.Description,
            d.Sujet,
            d.Titre,
            d.Texte,
            d.Frequence,
            heure = d.HeureEnvoi?.ToString("HH:mm"),
            d.JourSemaine,
            d.JourMois,
            d.Actif,
            reglesADefinir = SansRegle(d),
            supprimable = !CodeModeleMail.EstRelieAUnEnvoi(d.Code),
            prochaineExecution = Prochaine(d),
            dernierEnvoiLe = Utc(d.DernierEnvoiLe),
            d.DernierResultat,
            dateCreation = Utc(d.DateCreation),
            dateModification = Utc(d.DateModification),
            d.NombreImages,
            d.NombreDocuments,
            d.PoidsTotal,
            images = d.Images.Select(DecrirePiece),
            documents = d.Documents.Select(DecrirePiece),
            variables = Variables(d.Nature, d.Code),
        };

        private static object DecrirePiece(PieceModeleMailInfo p) => new
        {
            p.Id,
            p.Genre,
            p.Rang,
            p.NomFichier,
            p.TypeMime,
            p.Taille,
        };

        public class ContenuRequest
        {
            public string? Nom { get; set; }

            public string? Description { get; set; }

            public string? Sujet { get; set; }

            public string? Titre { get; set; }

            public string? Texte { get; set; }
        }

        public class EnvoiParentRequest
        {
            public string? Destinataire { get; set; }
        }

        public class PlanificationRequest
        {
            public bool Actif { get; set; }

            public string? Frequence { get; set; }

            /// <summary>« HH:mm », à l'horloge de Paris.</summary>
            public string? Heure { get; set; }

            public int? JourSemaine { get; set; }

            public int? JourMois { get; set; }
        }
    }
}
