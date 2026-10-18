using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// LES SIGNATURES DE COURRIEL — Camara, le 17/10/2026 : « un sous-onglet pour
    /// créer des templates de signature qui apparaîtront à la fin du mail, et les
    /// attribuer aux utilisateurs ». Le modèle de base est semé au démarrage.
    ///
    /// RÉSERVÉ AU SUPER-ADMINISTRATEUR, comme les rôles : il décide de ce qui
    /// signe au nom de l'entreprise.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EstSuperAdmin")]
    [Route("admin")]
    public class SignaturesController : ControllerBase
    {
        private readonly IMessagerieAdminRepository _depot;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SignaturesController> _logger;

        public SignaturesController(IMessagerieAdminRepository depot, IConfiguration configuration, ILogger<SignaturesController> logger)
        {
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Les modèles, et les comptes à qui en attribuer un (administrateurs et super-administrateur).</summary>
        [HttpGet("signatures")]
        [SwaggerResponse(200, "Modèles et signataires.")]
        public async Task<IActionResult> Lister(CancellationToken ct)
        {
            return Ok(new
            {
                modeles = await _depot.SignaturesAsync(ct),
                signataires = await SignatairesAsync(ct),
            });
        }

        /// <summary>
        /// Les comptes qui signent. Le super-administrateur n'a pas de rôle en
        /// base (il vient de la configuration) : il s'affiche « Super-admin ».
        /// </summary>
        private async Task<IReadOnlyList<SignataireAdmin>> SignatairesAsync(CancellationToken ct)
        {
            var superAdmins = _configuration.GetSection("Admin:Emails").Get<string[]>() ?? [];
            return (await _depot.SignatairesAsync(superAdmins, ct))
                .Select(s => s.Role is null && s.Mail is not null
                             && superAdmins.Contains(s.Mail, StringComparer.OrdinalIgnoreCase)
                    ? s with { Role = "Super-admin" }
                    : s)
                .ToList();
        }

        /// <summary>
        /// L'APERÇU DE LA SIGNATURE D'UN COMPTE — Camara, le 17/10/2026 : celle
        /// que recevront ses destinataires (son modèle, son nom, son rôle, son
        /// adresse de boîte ou, à défaut, de connexion).
        /// </summary>
        [HttpGet("parents/{id:int}/signature/apercu")]
        [Produces("text/html")]
        [SwaggerResponse(200, "La signature du compte, mise en page.")]
        [SwaggerResponse(404, "Compte inconnu parmi les signataires.")]
        public async Task<IActionResult> ApercuDuCompte(int id, CancellationToken ct)
        {
            var qui = (await SignatairesAsync(ct)).FirstOrDefault(s => s.ParentId == id);
            if (qui is null) return NotFound();

            var superAdmins = _configuration.GetSection("Admin:Emails").Get<string[]>() ?? [];
            var fonction = qui.Mail is not null && superAdmins.Contains(qui.Mail, StringComparer.OrdinalIgnoreCase)
                ? SignatureRendu.FonctionSuperAdmin
                : qui.Role ?? "Utilisateur";

            var modele = await _depot.SignatureDuParentAsync(id, ct);
            var html = modele is null
                ? SignatureRendu.Equipe
                : SignatureRendu.Html(modele,
                    new Signataire(qui.Prenom, qui.Nom, fonction, qui.AdresseBoite ?? qui.Mail),
                    string.Empty);
            return Content(html, "text/html; charset=utf-8");
        }

        [HttpPost("signatures")]
        [SwaggerResponse(201, "Modèle créé.", typeof(ModeleSignature))]
        [SwaggerResponse(400, "Nom manquant ou déjà pris.")]
        public async Task<IActionResult> Creer([FromBody] SaisieSignature saisie, CancellationToken ct)
        {
            var (modele, erreur) = await _depot.EnregistrerSignatureAsync(null, saisie, ct);
            if (modele is null) return BadRequest(new { message = erreur });
            _logger.LogInformation("Signature {Nom} creee.", modele.Nom);
            return StatusCode(201, modele);
        }

        [HttpPut("signatures/{id:int}")]
        [SwaggerResponse(200, "Modèle modifié.", typeof(ModeleSignature))]
        [SwaggerResponse(404, "Modèle inexistant.")]
        public async Task<IActionResult> Modifier(int id, [FromBody] SaisieSignature saisie, CancellationToken ct)
        {
            var (modele, erreur) = await _depot.EnregistrerSignatureAsync(id, saisie, ct);
            if (modele is null) return erreur is null ? NotFound() : BadRequest(new { message = erreur });
            return Ok(modele);
        }

        [HttpDelete("signatures/{id:int}")]
        [SwaggerResponse(204, "Modèle supprimé ; ses comptes reviennent au modèle par défaut.")]
        [SwaggerResponse(400, "Le dernier modèle ne se supprime pas.")]
        public async Task<IActionResult> Supprimer(int id, CancellationToken ct)
        {
            var (fait, erreur) = await _depot.SupprimerSignatureAsync(id, ct);
            if (!fait) return erreur is null ? NotFound() : BadRequest(new { message = erreur });
            return NoContent();
        }

        /// <summary>Attribue un modèle à un compte ; null : le modèle par défaut.</summary>
        [HttpPut("parents/{id:int}/signature")]
        [SwaggerResponse(204, "Modèle attribué.")]
        [SwaggerResponse(404, "Compte ou modèle inexistant.")]
        public async Task<IActionResult> Attribuer(int id, [FromBody] SignatureParentRequest requete, CancellationToken ct) =>
            await _depot.AttribuerSignatureAsync(id, requete?.SignatureId, ct) ? NoContent() : NotFound();

        /// <summary>
        /// L'aperçu d'un modèle (enregistré ou en cours de saisie), au nom de la
        /// personne connectée : son prénom, son rôle, son adresse.
        /// </summary>
        [HttpPost("signatures/apercu")]
        [Produces("text/html")]
        [SwaggerResponse(200, "La signature mise en page.")]
        public async Task<IActionResult> Apercu(
            [FromBody] SaisieSignature saisie, [FromServices] IBoitesAdminService boites, CancellationToken ct)
        {
            var b = await boites.ResoudreAsync(ct);
            var modele = new ModeleSignature(0, saisie.Nom ?? string.Empty, saisie.Formule, saisie.LigneLibre,
                saisie.AfficherRole, saisie.AfficherAdresse, saisie.AfficherSite, SignatureRendu.CouleurSure(saisie.Couleur), false, 0,
                saisie.Telephone?.Trim());
            var qui = b is null
                ? new Signataire("Prénom", "Nom", "Rôle", "adresse@mimia.fr")
                : new Signataire(b.Qui.Prenom, b.Qui.Nom, b.Role, b.Adresse ?? b.Qui.Mail);

            // Images en chemin relatif : l'aperçu les prend sur le site où il s'affiche.
            return Content(SignatureRendu.Html(modele, qui, string.Empty), "text/html; charset=utf-8");
        }

        /// <param name="SignatureId">Le modèle ; null pour le modèle par défaut.</param>
        public record SignatureParentRequest(int? SignatureId);
    }
}
