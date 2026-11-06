using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    public class SignalementAdminCreationRequest
    {
        /// <summary>PROFESSEUR / TECHNIQUE / SUGGESTION / AUTRE.</summary>
        [Required(ErrorMessage = "La catégorie est obligatoire.")]
        public string? Categorie { get; set; }

        [Required(ErrorMessage = "La description est obligatoire."), StringLength(2000, MinimumLength = 3, ErrorMessage = "La description fait de 3 à 2 000 caractères.")]
        public string? Description { get; set; }

        /// <summary>« Compétence / test à réaliser » : obligatoire dès qu'un bloc est choisi.</summary>
        [StringLength(1000, ErrorMessage = "La compétence ne peut pas dépasser 1 000 caractères.")]
        public string? Competence { get; set; }

        public int? JeuId { get; set; }

        /// <summary>Le bloc du jeu (un test : Français, Mathématiques…).</summary>
        public int? TestId { get; set; }
    }

    /// <summary>
    /// LES SIGNALEMENTS DES ADMINISTRATEURS — Camara, le 05/11/2026 : « le but c'est que les signalements soient
    /// reliés aux jeux de test ». Le bouton à côté du bouclier les crée ; l'onglet « Signalements Admin » les liste.
    /// Relié à un bloc, le signalement pose une ligne « KO » dans le jeu : compétence = son champ, commentaire = sa
    /// description. Passé à « Traité », la ligne repasse « à tester » et un mail part (AdminController.ModifierSignalement).
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EstAdmin")]
    [Route("admin/signalements-admin")]
    public class SignalementsAdminController : ControllerBase
    {
        private static readonly HashSet<string> CategoriesConnues =
            new(StringComparer.OrdinalIgnoreCase) { "PROFESSEUR", "TECHNIQUE", "SUGGESTION", "AUTRE" };

        private readonly ISignalementService _signalements;
        private readonly IJeuxDeTestsRepository _jeux;
        private readonly IParentService _parentService;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly IEvenementsAdminHub _evenementsAdmin;
        private readonly ILogger<SignalementsAdminController> _logger;

        public SignalementsAdminController(
            ISignalementService signalements,
            IJeuxDeTestsRepository jeux,
            IParentService parentService,
            ICurrentUserAccessor currentUser,
            IEvenementsAdminHub evenementsAdmin,
            ILogger<SignalementsAdminController> logger)
        {
            _signalements = signalements ?? throw new ArgumentNullException(nameof(signalements));
            _jeux = jeux ?? throw new ArgumentNullException(nameof(jeux));
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _evenementsAdmin = evenementsAdmin ?? throw new ArgumentNullException(nameof(evenementsAdmin));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [SwaggerResponse(200, "Les signalements des administrateurs, avec leur ligne de test.", typeof(IEnumerable<Signalement>))]
        public async Task<IActionResult> Lister(CancellationToken ct)
        {
            var liste = (await _signalements.GetAdminAsync(ct)).ToList();
            var lignes = await _jeux.LignesDeSignalementAsync(
                liste.Where(s => s.LigneDeTestId is not null).Select(s => s.LigneDeTestId!.Value), ct);
            var parId = lignes.ToDictionary(l => l.LigneId);

            foreach (var s in liste)
            {
                if (s.LigneDeTestId is int id && parId.TryGetValue(id, out var ligne)) s.Test = ligne;
            }

            return Ok(liste);
        }

        [HttpPost]
        [SwaggerResponse(201, "Signalement enregistré, et sa ligne posée dans le jeu de test s'il y est relié.")]
        [SwaggerResponse(400, "Catégorie, description, compétence ou bloc invalide.")]
        public async Task<IActionResult> Creer(
            [FromBody] SignalementAdminCreationRequest requete, [FromServices] ITelegramService telegram, CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var categorie = requete.Categorie?.Trim().ToUpperInvariant();
            if (categorie is null || !CategoriesConnues.Contains(categorie)) return BadRequest(new { message = "Catégorie inconnue." });

            var description = requete.Description!.Trim();
            var competence = string.IsNullOrWhiteSpace(requete.Competence) ? null : requete.Competence.Trim();

            var relie = requete.TestId is not null || requete.JeuId is not null;
            if (relie && (requete.JeuId is null || requete.TestId is null))
            {
                return BadRequest(new { message = "Choisis le jeu de test et son bloc." });
            }
            if (relie && competence is null)
            {
                return BadRequest(new { message = "La compétence / test à réaliser est obligatoire pour l'ajouter au jeu de test." });
            }

            var identifiant = _currentUser.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identifiant)) throw new UnauthorizedAccessException("Le jeton ne contient pas de claim `sub`.");

            var parent = await _parentService.GetOrCreateAsync(
                identifiant, _currentUser.Mail, _currentUser.Prenom, _currentUser.Nom, noterLaVenue: true);

            int? ligneId = null;
            if (relie)
            {
                ligneId = await _jeux.PoserLigneDeSignalementAsync(requete.JeuId!.Value, requete.TestId!.Value, competence!, description, ct);
                if (ligneId is null) return BadRequest(new { message = "Ce bloc n'existe plus dans ce jeu de test." });
            }

            var signalement = await _signalements.CreerAdminAsync(parent.Id, categorie, description, competence, ligneId, ct);

            _evenementsAdmin.Publier("signalement");
            _logger.LogInformation("Signalement admin {Id} ({Categorie}), ligne de test {Ligne}.", signalement.Id, categorie, ligneId);

            // LE GROUPE TELEGRAM DES MAILS (06/11/2026), APRÈS l'écriture : une panne de Telegram ne fait jamais
            // perdre le signalement, et l'envoi avale ses propres erreurs.
            string? jeu = null;
            if (ligneId is int idLigne)
            {
                var ligne = (await _jeux.LignesDeSignalementAsync([idLigne], ct)).FirstOrDefault();
                if (ligne is not null)
                {
                    jeu = $"{(string.IsNullOrWhiteSpace(ligne.JeuTitre) ? ligne.Classe : ligne.JeuTitre)} › {ligne.TestTitre}";
                }
            }
            await telegram.NotifierSignalementAdminAsync(categorie, parent.Mail ?? _currentUser.Mail, description, competence, jeu);

            return StatusCode(201, new { message = "Signalement enregistré.", id = signalement.Id, ligneDeTestId = ligneId });
        }
    }
}
