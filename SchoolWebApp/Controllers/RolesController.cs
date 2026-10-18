using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// LES RÔLES DES COMPTES PARENTS — voulus par Camara le 17/10/2026 : un onglet
    /// « Rôles » où créer des rôles, et un rôle à choisir pour chaque parent.
    ///
    /// RÉSERVÉ AU SUPER-ADMINISTRATEUR, de bout en bout. Un rôle porte des droits
    /// d'administration : qui pourrait en créer ou en attribuer se donnerait
    /// des pouvoirs. C'est la même règle que l'ancien bouton « Administrateur ».
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EstSuperAdmin")]
    [Route("admin")]
    public class RolesController : ControllerBase
    {
        private readonly IRolesRepository _roles;
        private readonly IAdminService _admin;
        private readonly ComptesProteges _comptesProteges;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            IRolesRepository roles, IAdminService admin, ComptesProteges comptesProteges, ILogger<RolesController> logger)
        {
            _roles = roles ?? throw new ArgumentNullException(nameof(roles));
            _admin = admin ?? throw new ArgumentNullException(nameof(admin));
            _comptesProteges = comptesProteges ?? throw new ArgumentNullException(nameof(comptesProteges));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Les rôles, et la liste des onglets qu'un rôle peut ouvrir.</summary>
        [HttpGet("roles")]
        [SwaggerResponse(200, "Les rôles et les onglets possibles.")]
        public async Task<IActionResult> Lister(CancellationToken ct) =>
            Ok(new { roles = await _roles.ListerAsync(ct), onglets = OngletsAdmin.Toutes });

        [HttpPost("roles")]
        [SwaggerResponse(201, "Rôle créé.", typeof(RoleCompte))]
        [SwaggerResponse(400, "Nom vide, trop long ou déjà pris.")]
        public async Task<IActionResult> Creer([FromBody] RoleRequest requete, CancellationToken ct)
        {
            var r = await _roles.CreerAsync(requete?.Nom ?? string.Empty, requete?.Onglets, ct);
            if (r.Role is null) return BadRequest(new { message = r.Erreur });

            _logger.LogWarning("Role {Role} cree : {Onglets}.", r.Role.Nom, string.Join(",", r.Role.Onglets));
            return StatusCode(201, r.Role);
        }

        [HttpPut("roles/{id:int}")]
        [SwaggerResponse(200, "Rôle modifié.", typeof(RoleCompte))]
        [SwaggerResponse(400, "Rôle système non modifiable, ou nom refusé.")]
        [SwaggerResponse(404, "Rôle inexistant.")]
        public async Task<IActionResult> Modifier(int id, [FromBody] RoleRequest requete, CancellationToken ct)
        {
            var r = await _roles.ModifierAsync(id, requete?.Nom ?? string.Empty, requete?.Onglets, ct);
            if (r.Introuvable) return NotFound();
            if (r.Role is null) return BadRequest(new { message = r.Erreur });

            // TRACÉ EN AVERTISSEMENT : c'est la ligne qu'on relit quand on se
            // demande qui a eu accès à quoi, et depuis quand.
            _logger.LogWarning("Role {Role} redefini : {Onglets}.", r.Role.Nom, string.Join(",", r.Role.Onglets));
            return Ok(r.Role);
        }

        [HttpDelete("roles/{id:int}")]
        [SwaggerResponse(204, "Rôle supprimé ; ses comptes redeviennent « Utilisateur ».")]
        [SwaggerResponse(400, "Rôle système.")]
        [SwaggerResponse(404, "Rôle inexistant.")]
        public async Task<IActionResult> Supprimer(int id, CancellationToken ct)
        {
            var r = await _roles.SupprimerAsync(id, ct);
            if (r.Introuvable) return NotFound();
            if (r.Role is null) return BadRequest(new { message = r.Erreur });

            _logger.LogWarning("Role {Role} supprime.", r.Role.Nom);
            return NoContent();
        }

        /// <summary>
        /// Donne un rôle à un parent (null = Utilisateur). Le compte du
        /// super-administrateur ne change pas de rôle, et le rôle Super-admin ne
        /// se donne à personne.
        /// </summary>
        [HttpPut("parents/{id:int}/role")]
        [SwaggerResponse(204, "Rôle attribué.")]
        [SwaggerResponse(400, "Rôle Super-admin, ou compte protégé.")]
        [SwaggerResponse(404, "Parent ou rôle inexistant.")]
        public async Task<IActionResult> Attribuer(int id, [FromBody] RoleParentRequest requete, CancellationToken ct)
        {
            if (_comptesProteges.Protege(await _admin.MailDuParentAsync(id)))
            {
                return BadRequest(new { message = "Le compte super-administrateur ne change pas de rôle." });
            }

            var (fait, erreur) = await _roles.AttribuerAsync(id, requete?.RoleId, ct);
            if (erreur is not null) return BadRequest(new { message = erreur });
            if (!fait) return NotFound();

            _logger.LogWarning("Role du parent {ParentId} : {RoleId}.", id, requete?.RoleId?.ToString() ?? "Utilisateur");
            return NoContent();
        }

        /// <param name="Nom">Le nom du rôle (60 caractères au plus).</param>
        /// <param name="Onglets">Les clés des onglets qu'il ouvre.</param>
        public record RoleRequest(string? Nom, string[]? Onglets);

        /// <param name="RoleId">Le rôle ; null pour « Utilisateur ».</param>
        public record RoleParentRequest(int? RoleId);
    }
}
