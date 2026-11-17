using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    public class JeuDeTestRequest
    {
        [Required(ErrorMessage = "Le titre est obligatoire."), StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères.")]
        public string? Titre { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La classe est obligatoire.")]
        public int NiveauId { get; set; }
    }

    public class TestDuJeuRequest
    {
        [Required(ErrorMessage = "Le titre est obligatoire."), StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères.")]
        public string? Titre { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La matière est obligatoire.")]
        public int MatiereId { get; set; }

        /// <summary>Combien de lignes créer. Ignoré quand on modifie un test existant.</summary>
        [Range(1, StatutsDeTest.LignesMaxParAjout, ErrorMessage = "Le nombre de tests va de 1 à 50.")]
        public int Nombre { get; set; } = 1;

        /// <summary>Ce qui est déjà écrit, ligne par ligne ; les lignes restantes attendent d'être remplies.</summary>
        public List<string?>? Competences { get; set; }
    }

    public class LigneDeTestRequest
    {
        [StringLength(1000, ErrorMessage = "La compétence ne peut pas dépasser 1 000 caractères.")]
        public string? Competence { get; set; }

        public string? Statut { get; set; }

        [StringLength(2000, ErrorMessage = "Le commentaire ne peut pas dépasser 2 000 caractères.")]
        public string? Commentaire { get; set; }
    }

    public class StatutLigneRequest
    {
        [Required]
        public string? Statut { get; set; }
    }

    public class PositionLigneRequest
    {
        /// <summary>Son rang une fois déplacée, à partir de 0 (tout en haut du test).</summary>
        public int Position { get; set; }
    }

    /// <summary>
    /// L'ONGLET « JEUX DE TESTS » — Camara, le 04/11/2026 : des carnets de test par section et
    /// par classe, chaque test créant autant de lignes qu'on en demande.
    ///
    /// LA CLASSE ET LA MATIÈRE SE RELISENT ICI, dans le référentiel : l'écran n'envoie qu'un
    /// identifiant, et la section se déduit du cycle de la classe. Elles ne peuvent donc pas
    /// se contredire.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "EstAdmin")]
    [Route("admin/jeux-tests")]
    public class JeuxDeTestsController : ControllerBase
    {
        private const string JeuIntrouvable = "Ce jeu de test n'existe plus.";
        private const string TestIntrouvable = "Ce test n'existe plus.";
        private const string LigneIntrouvable = "Cette ligne n'existe plus.";
        private const string StatutInconnu = "Statut inconnu.";

        private readonly IJeuxDeTestsRepository _jeux;
        private readonly IReferentielService _referentiel;
        private readonly ICurrentUserAccessor _utilisateur;
        private readonly ISignalementRepository _signalements;
        private readonly IServiceScopeFactory _scopes;
        private readonly OptionsEmail _optionsEmail;
        private readonly ILogger<JeuxDeTestsController> _logger;

        public JeuxDeTestsController(
            IJeuxDeTestsRepository jeux, IReferentielService referentiel, ICurrentUserAccessor utilisateur,
            ISignalementRepository signalements, IServiceScopeFactory scopes, IOptions<OptionsEmail> optionsEmail,
            ILogger<JeuxDeTestsController> logger)
        {
            _signalements = signalements ?? throw new ArgumentNullException(nameof(signalements));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _optionsEmail = optionsEmail?.Value ?? throw new ArgumentNullException(nameof(optionsEmail));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jeux = jeux ?? throw new ArgumentNullException(nameof(jeux));
            _referentiel = referentiel ?? throw new ArgumentNullException(nameof(referentiel));
            _utilisateur = utilisateur ?? throw new ArgumentNullException(nameof(utilisateur));
        }

        [HttpGet]
        [SwaggerResponse(200, "Tous les jeux de test, le plus récent d'abord.", typeof(IReadOnlyList<JeuDeTest>))]
        public async Task<IActionResult> Lister(CancellationToken ct) =>
            Ok(await _jeux.ListerAsync(ct));

        [HttpGet("{id:int}")]
        [SwaggerResponse(200, "Le jeu, ses tests et leurs lignes.", typeof(JeuDeTest))]
        [SwaggerResponse(404, "Jeu inexistant.")]
        public async Task<IActionResult> Lire(int id, CancellationToken ct) =>
            Rendre(await _jeux.GetAsync(id, ct), JeuIntrouvable);

        [HttpPost]
        [SwaggerResponse(201, "Jeu créé.", typeof(JeuDeTest))]
        [SwaggerResponse(400, "Classe inconnue.")]
        public async Task<IActionResult> Creer([FromBody] JeuDeTestRequest requete, CancellationToken ct)
        {
            var titre = requete.Titre?.Trim();
            if (string.IsNullOrEmpty(titre)) return BadRequest(new { message = "Le titre est obligatoire." });

            var classe = await ClasseAsync(requete.NiveauId);
            if (classe is null) return BadRequest(new { message = "Cette classe n'existe pas." });

            var jeu = await _jeux.CreerAsync(titre, classe.Value.Section, classe.Value.Code, classe.Value.Libelle, Auteur(), ct);
            return StatusCode(StatusCodes.Status201Created, jeu);
        }

        [HttpPut("{id:int}")]
        [SwaggerResponse(200, "Jeu modifié.", typeof(JeuDeTest))]
        [SwaggerResponse(400, "Classe inconnue.")]
        [SwaggerResponse(404, "Jeu inexistant.")]
        public async Task<IActionResult> Modifier(int id, [FromBody] JeuDeTestRequest requete, CancellationToken ct)
        {
            var titre = requete.Titre?.Trim();
            if (string.IsNullOrEmpty(titre)) return BadRequest(new { message = "Le titre est obligatoire." });

            var classe = await ClasseAsync(requete.NiveauId);
            if (classe is null) return BadRequest(new { message = "Cette classe n'existe pas." });

            return Rendre(await _jeux.ModifierAsync(id, titre, classe.Value.Section, classe.Value.Code, classe.Value.Libelle, ct), JeuIntrouvable);
        }

        [HttpDelete("{id:int}")]
        [SwaggerResponse(204, "Jeu supprimé, avec ses tests et leurs lignes.")]
        [SwaggerResponse(404, "Jeu inexistant.")]
        public async Task<IActionResult> Supprimer(int id, CancellationToken ct) =>
            await _jeux.SupprimerAsync(id, ct) ? NoContent() : NotFound(new { message = JeuIntrouvable });

        [HttpPost("{id:int}/tests")]
        [SwaggerResponse(200, "Test créé, avec une ligne par test demandé.", typeof(JeuDeTest))]
        [SwaggerResponse(400, "Titre, matière ou nombre invalide.")]
        [SwaggerResponse(404, "Jeu inexistant.")]
        public async Task<IActionResult> AjouterTest(int id, [FromBody] TestDuJeuRequest requete, CancellationToken ct)
        {
            var titre = requete.Titre?.Trim();
            if (string.IsNullOrEmpty(titre)) return BadRequest(new { message = "Le titre est obligatoire." });

            var matiere = await MatiereAsync(requete.MatiereId);
            if (matiere is null) return BadRequest(new { message = "Cette matière n'existe pas." });

            var competences = Competences(requete.Competences, requete.Nombre);
            if (competences is null) return BadRequest(new { message = "Une compétence ne peut pas dépasser 1 000 caractères." });

            return Rendre(
                await _jeux.AjouterTestAsync(id, titre, matiere.Value.Code, matiere.Value.Libelle, competences, ct),
                JeuIntrouvable);
        }

        [HttpPut("{id:int}/tests/{testId:int}")]
        [SwaggerResponse(200, "Test modifié.", typeof(JeuDeTest))]
        [SwaggerResponse(400, "Titre ou matière invalide.")]
        [SwaggerResponse(404, "Test inexistant dans ce jeu.")]
        public async Task<IActionResult> ModifierTest(
            int id, int testId, [FromBody] TestDuJeuRequest requete, CancellationToken ct)
        {
            var titre = requete.Titre?.Trim();
            if (string.IsNullOrEmpty(titre)) return BadRequest(new { message = "Le titre est obligatoire." });

            var matiere = await MatiereAsync(requete.MatiereId);
            if (matiere is null) return BadRequest(new { message = "Cette matière n'existe pas." });

            return Rendre(
                await _jeux.ModifierTestAsync(id, testId, titre, matiere.Value.Code, matiere.Value.Libelle, ct),
                TestIntrouvable);
        }

        [HttpDelete("{id:int}/tests/{testId:int}")]
        [SwaggerResponse(200, "Test supprimé, le jeu relu.", typeof(JeuDeTest))]
        [SwaggerResponse(404, "Test inexistant dans ce jeu.")]
        public async Task<IActionResult> SupprimerTest(int id, int testId, CancellationToken ct) =>
            Rendre(await _jeux.SupprimerTestAsync(id, testId, ct), TestIntrouvable);

        [HttpPost("{id:int}/tests/{testId:int}/lignes")]
        [SwaggerResponse(200, "Ligne ajoutée, à tester.", typeof(JeuDeTest))]
        [SwaggerResponse(404, "Test inexistant dans ce jeu.")]
        public async Task<IActionResult> AjouterLigne(int id, int testId, CancellationToken ct) =>
            Rendre(await _jeux.AjouterLigneAsync(id, testId, string.Empty, ct), TestIntrouvable);

        [HttpPut("{id:int}/lignes/{ligneId:int}")]
        [SwaggerResponse(200, "Ligne modifiée.", typeof(JeuDeTest))]
        [SwaggerResponse(400, "Statut inconnu ou champ trop long.")]
        [SwaggerResponse(404, "Ligne inexistante dans ce jeu.")]
        public async Task<IActionResult> ModifierLigne(
            int id, int ligneId, [FromBody] LigneDeTestRequest requete, CancellationToken ct)
        {
            if (!StatutsDeTest.Connu(requete.Statut)) return BadRequest(new { message = StatutInconnu });

            var commentaire = requete.Commentaire?.Trim();
            var avant = await StatutAvantAsync(ligneId, ct);

            var jeu = await _jeux.ModifierLigneAsync(
                id, ligneId, requete.Competence?.Trim() ?? string.Empty, requete.Statut!,
                string.IsNullOrEmpty(commentaire) ? null : commentaire, HeureFrance.Locale(DateTime.UtcNow), ct);

            if (jeu is not null) await ApresChangementDeStatutAsync(ligneId, avant, requete.Statut!, ct);
            return Rendre(jeu, LigneIntrouvable);
        }

        [HttpPut("{id:int}/lignes/{ligneId:int}/statut")]
        [SwaggerResponse(200, "Statut changé, le jeu relu avec son statut recalculé.", typeof(JeuDeTest))]
        [SwaggerResponse(400, "Statut inconnu.")]
        [SwaggerResponse(404, "Ligne inexistante dans ce jeu.")]
        public async Task<IActionResult> ChangerStatut(
            int id, int ligneId, [FromBody] StatutLigneRequest requete, CancellationToken ct)
        {
            if (!StatutsDeTest.Connu(requete.Statut)) return BadRequest(new { message = StatutInconnu });

            var avant = await StatutAvantAsync(ligneId, ct);
            var jeu = await _jeux.ChangerStatutAsync(id, ligneId, requete.Statut!, HeureFrance.Locale(DateTime.UtcNow), ct);

            if (jeu is not null) await ApresChangementDeStatutAsync(ligneId, avant, requete.Statut!, ct);
            return Rendre(jeu, LigneIntrouvable);
        }

        [HttpPut("{id:int}/lignes/{ligneId:int}/position")]
        [SwaggerResponse(200, "Ligne déplacée dans son test, l'ordre est mémorisé.", typeof(JeuDeTest))]
        [SwaggerResponse(404, "Ligne inexistante dans ce jeu.")]
        public async Task<IActionResult> DeplacerLigne(
            int id, int ligneId, [FromBody] PositionLigneRequest requete, CancellationToken ct) =>
            Rendre(await _jeux.DeplacerLigneAsync(id, ligneId, requete.Position, ct), LigneIntrouvable);

        private async Task<string?> StatutAvantAsync(int ligneId, CancellationToken ct) =>
            (await _jeux.LignesDeSignalementAsync([ligneId], ct)).FirstOrDefault()?.Statut;

        /// <summary>
        /// CE QUE LE TESTEUR DÉCIDE PART AU SUPPORT — Camara, le 06/11/2026. Seulement au PASSAGE d'un statut à
        /// l'autre (une ligne réenregistrée telle quelle ne prévient personne), et seulement pour une ligne reliée à
        /// un signalement admin. Les mails partent après l'écriture : une panne d'envoi ne défait rien, elle se lit
        /// dans le journal.
        ///
        /// - KO : « ça remet le ticket en Nouveau, au lieu de Traité, avec un badge montrant qu'il a été rouvert
        ///   après un test » — seulement s'il était TRAITÉ (un KO avant toute correction n'a rien à rouvrir).
        /// - OK : « un mail dans la boîte du support pour dire que le test est OK ». Le signalement ne bouge pas.
        /// </summary>
        private async Task ApresChangementDeStatutAsync(int ligneId, string? avant, string apres, CancellationToken ct)
        {
            if (apres == avant) return;

            if (apres == StatutsDeTest.Ko)
            {
                var rouvert = await _signalements.RouvrirApresTestAsync(ligneId, DateTime.UtcNow, ct);
                if (rouvert is null) return;

                _logger.LogInformation("Signalement {Id} rouvert apres un test KO (ligne {Ligne}, {Fois} fois).",
                    rouvert.Id, ligneId, rouvert.Reouvertures);
                await EcrireAuSupportAsync(ligneId, rouvert, "Test KO : le bug « {0} » est rouvert", "test-ko",
                    rouvert.Reouvertures > 1 ? $" (pour la {rouvert.Reouvertures}<sup>e</sup> fois)" : string.Empty, ct);
                return;
            }

            if (apres == StatutsDeTest.Ok)
            {
                var signalement = await _signalements.GetAdminDeLigneAsync(ligneId, ct);
                if (signalement is null) return;

                _logger.LogInformation("Signalement {Id} : test OK (ligne {Ligne}).", signalement.Id, ligneId);
                await EcrireAuSupportAsync(ligneId, signalement, "Test réussi : « {0} » s'est terminé avec succès", "test-ok",
                    signalement.Reouvertures > 0
                        ? $" Il avait été rouvert {signalement.Reouvertures} fois avant."
                        : string.Empty, ct);
            }
        }

        /// <summary>Un mail à la boîte du support (`OptionsEmail.RecipientEmail`), sur une ligne et son signalement.</summary>
        private async Task EcrireAuSupportAsync(
            int ligneId, Signalement signalement, string sujet, string gabarit, string fois, CancellationToken ct)
        {
            var support = string.IsNullOrWhiteSpace(_optionsEmail.RecipientEmail) ? _optionsEmail.SenderEmail : _optionsEmail.RecipientEmail;
            var ligne = (await _jeux.LignesDeSignalementAsync([ligneId], ct)).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(support) || ligne is null)
            {
                _logger.LogWarning("Mail « {Gabarit} » non envoye : pas de boite support ou ligne {Ligne} introuvable.", gabarit, ligneId);
                return;
            }

            static string Html(string? texte) => System.Net.WebUtility.HtmlEncode(texte ?? string.Empty);
            var section = ligne.Section switch { "elementaire" => "Élémentaire", "college" => "Collège", "lycee" => "Lycée", _ => ligne.Section };
            var nom = ligne.Competence.Length > 90 ? ligne.Competence[..90].TrimEnd() + "…" : ligne.Competence;

            var valeurs = new Dictionary<string, string>
            {
                ["competence"] = Html(ligne.Competence),
                ["jeu"] = Html(string.IsNullOrWhiteSpace(ligne.JeuTitre) ? $"{section} · {ligne.Classe}" : ligne.JeuTitre),
                ["classe"] = Html($"{section} · {ligne.Classe}"),
                ["bloc"] = Html($"{ligne.TestTitre} · {ligne.Matiere}"),
                ["commentaire"] = Html(ligne.Commentaire ?? signalement.Description),
                ["testeur"] = Html(Auteur()),
                ["fois"] = fois,
            };

            // EN ARRIÈRE-PLAN (06/11/2026) : le serveur d'envoi peut mettre des secondes, voire deux minutes à
            // répondre ; le clic du testeur, lui, doit revenir tout de suite. Une portée à part : celle de la
            // requête est fermée quand le mail part.
            var objet = string.Format(sujet, nom);
            var signalementId = signalement.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var portee = _scopes.CreateScope();
                    var email = portee.ServiceProvider.GetRequiredService<IServiceEmail>();
                    if (!await email.EnvoyerAsync(support, objet, gabarit, valeurs, CancellationToken.None))
                    {
                        _logger.LogError("Mail « {Gabarit} » NON ENVOYE au support (signalement {Id}).", gabarit, signalementId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mail « {Gabarit} » au support en echec (signalement {Id}).", gabarit, signalementId);
                }
            });
        }

        [HttpDelete("{id:int}/lignes/{ligneId:int}")]
        [SwaggerResponse(200, "Ligne supprimée, le jeu relu.", typeof(JeuDeTest))]
        [SwaggerResponse(404, "Ligne inexistante dans ce jeu.")]
        public async Task<IActionResult> SupprimerLigne(int id, int ligneId, CancellationToken ct) =>
            Rendre(await _jeux.SupprimerLigneAsync(id, ligneId, ct), LigneIntrouvable);

        private IActionResult Rendre(JeuDeTest? jeu, string introuvable) =>
            jeu is null ? NotFound(new { message = introuvable }) : Ok(jeu);

        /// <summary>Le nom de l'administrateur connecté, en texte : le carnet le garde même si ce compte disparaît.</summary>
        private string Auteur()
        {
            var nom = $"{_utilisateur.Prenom} {_utilisateur.Nom}".Trim();
            if (nom.Length > 0) return nom.Length <= 255 ? nom : nom[..255];

            return string.IsNullOrWhiteSpace(_utilisateur.Mail) ? "Administrateur" : _utilisateur.Mail;
        }

        /// <summary>Une classe qui peut accueillir un élève ; les troncs communs n'en sont pas.</summary>
        private async Task<(string Section, string Code, string Libelle)?> ClasseAsync(int niveauId)
        {
            var niveaux = await _referentiel.GetNiveauxScolairesAsync();
            var niveau = niveaux.FirstOrDefault(n => n.Id == niveauId);

            if (niveau?.Code is null || niveau.Libelle is null || !VoiesScolaires.EstSelectionnable(niveau.Code)) return null;

            var section = StatutsDeTest.SectionDuCycle(niveau.Cycle);
            return section is null ? null : (section, niveau.Code, niveau.Libelle);
        }

        /// <summary>Toutes les matières, même fermées : on teste aussi ce qui n'est pas encore ouvert.</summary>
        private async Task<(string Code, string Libelle)?> MatiereAsync(int matiereId)
        {
            var matieres = await _referentiel.GetMatieresAsync(activesSeulement: false);
            var matiere = matieres.FirstOrDefault(m => m.Id == matiereId);

            return matiere?.Code is null || matiere.Libelle is null ? null : (matiere.Code, matiere.Libelle);
        }

        /// <summary>Exactement <paramref name="nombre"/> lignes ; null si l'une d'elles est trop longue.</summary>
        private static List<string>? Competences(IReadOnlyList<string?>? ecrites, int nombre)
        {
            var lignes = Enumerable.Range(0, nombre)
                .Select(i => ecrites is not null && i < ecrites.Count ? ecrites[i]?.Trim() ?? string.Empty : string.Empty)
                .ToList();

            return lignes.Any(l => l.Length > 1000) ? null : lignes;
        }
    }
}
