using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.IdentityServer.Data;

namespace SchoolWebApp.IdentityServer.Controllers
{
    /// <summary>
    /// Petite API appelée par la SPA pour ce qui touche aux identifiants.
    ///
    /// Le mot de passe ne peut pas transiter par l'API métier : elle n'a pas
    /// accès à la base d'identité, et lui donner ce pouvoir reviendrait à
    /// dupliquer la gestion des credentials dans deux applications.
    ///
    /// Le schéma est précisé explicitement : AddIdentity fait du cookie le
    /// schéma par défaut, et sans cette mention le jeton porteur serait ignoré.
    /// </summary>
    [ApiController]
    [Route("api/compte")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class CompteController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceEmail _email;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CompteController> _logger;

        public CompteController(
            UserManager<ApplicationUser> userManager,
            IServiceEmail email,
            IConfiguration configuration,
            ILogger<CompteController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Nature du compte. La SPA s'en sert pour n'afficher le formulaire de
        /// mot de passe qu'aux comptes qui en ont un : proposer à un utilisateur
        /// Google de « changer son mot de passe » n'aurait aucun sens.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Etat()
        {
            var utilisateur = await UtilisateurCourantAsync();
            if (utilisateur is null) return Unauthorized();

            var connexions = await _userManager.GetLoginsAsync(utilisateur);

            return Ok(new
            {
                email = utilisateur.Email,
                prenom = utilisateur.Prenom,
                nom = utilisateur.Nom,
                aMotDePasse = await _userManager.HasPasswordAsync(utilisateur),
                fournisseurs = connexions.Select(c => c.LoginProvider).ToArray(),

                // Compte de démonstration : son mot de passe est fixé par la
                // configuration, l'interface doit le dire au lieu de proposer
                // un formulaire qui sera refusé.
                estDemonstration = EstCompteDemonstration(utilisateur.Email),
            });
        }

        [HttpPost("mot-de-passe")]
        public async Task<IActionResult> ChangerMotDePasse([FromBody] ChangementMotDePasse requete)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var utilisateur = await UtilisateurCourantAsync();
            if (utilisateur is null) return Unauthorized();

            // Le compte de démonstration garde SON mot de passe.
            //
            // Il est fixé dans la configuration et sert à tous ceux à qui on
            // montre le produit. Le laisser changer le rendrait inaccessible
            // sans que personne ne sache par quoi il a été remplacé — et le
            // semis de démarrage ne le rétablirait pas, puisqu'il ne touche
            // jamais à un compte existant.
            if (EstCompteDemonstration(utilisateur.Email))
            {
                return BadRequest(new
                {
                    message = "Le mot de passe du compte de démonstration est fixé par la "
                              + "configuration et ne peut pas être modifié ici.",
                });
            }

            if (!await _userManager.HasPasswordAsync(utilisateur))
            {
                return BadRequest(new
                {
                    message = "Ce compte se connecte avec Google : il n'a pas de mot de passe.",
                });
            }

            var resultat = await _userManager.ChangePasswordAsync(
                utilisateur, requete.MotDePasseActuel!, requete.NouveauMotDePasse!);

            if (!resultat.Succeeded)
            {
                // Message unique : détailler « mot de passe actuel incorrect »
                // face à « nouveau mot de passe trop faible » aiderait quelqu'un
                // qui teste des mots de passe sur un jeton volé.
                var messages = resultat.Errors.Select(Traduire).ToArray();
                return BadRequest(new { message = string.Join(" ", messages) });
            }

            _logger.LogInformation("Mot de passe change pour {UserId}.", utilisateur.Id);

            return Ok(new { message = "Votre mot de passe a été changé." });
        }

        /// <summary>
        /// Efface le compte de connexion. Définitif.
        ///
        /// APPELÉ EN DERNIER, APRÈS QUE L'API A EFFACÉ LES DONNÉES.
        /// Cet ordre est ce qui garantit qu'on ne laisse jamais des données
        /// d'enfants sans personne pour y accéder : tant que l'identité existe,
        /// le parent peut se reconnecter et redemander la suppression.
        ///
        /// CE QUE ÇA EMPORTE : le mot de passe, les liaisons Google, les jetons
        /// d'actualisation. La reconnexion devient impossible, y compris par
        /// Google — dont la liaison est rompue, pas seulement oubliée. Une
        /// réinscription avec la même adresse donnera un compte NEUF, sans lien
        /// avec l'ancien : l'identifiant `sub` sera différent, et c'est lui, et
        /// non l'adresse, qui rattache un parent à ses données.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> Supprimer()
        {
            var utilisateur = await UtilisateurCourantAsync();
            if (utilisateur is null) return Unauthorized();

            // Le compte de démonstration ne se supprime pas.
            //
            // Il est partagé : c'est celui qu'on donne à voir. N'importe lequel
            // de ses visiteurs pourrait l'effacer pour tous les autres, et le
            // semis de démarrage ne le recréerait pas — il ne touche jamais à
            // un compte existant, mais il ne ressuscite pas un compte disparu.
            if (EstCompteDemonstration(utilisateur.Email))
            {
                return BadRequest(new
                {
                    message = "Le compte de démonstration ne peut pas être supprimé.",
                });
            }

            // L'adresse est relevée AVANT l'effacement : après, il n'y a plus
            // personne à qui écrire. C'est la dernière fois qu'on la connaît.
            var adresse = utilisateur.Email;
            var prenom = utilisateur.Prenom;

            var resultat = await _userManager.DeleteAsync(utilisateur);

            if (!resultat.Succeeded)
            {
                _logger.LogError(
                    "Echec de la suppression du compte {UserId} : {Erreurs}.",
                    utilisateur.Id,
                    string.Join(" ", resultat.Errors.Select(e => e.Description)));

                return StatusCode(500, new
                {
                    message = "Votre compte n'a pas pu être supprimé. Réessayez dans un instant.",
                });
            }

            _logger.LogWarning("Compte {UserId} supprime a la demande de son titulaire.", utilisateur.Id);

            await ConfirmerLaSuppressionAsync(adresse, prenom);

            return NoContent();
        }

        /// <summary>
        /// La confirmation écrite que le compte a bien été supprimé.
        ///
        /// ENVOYÉE APRÈS COUP, JAMAIS AVANT : le message affirme un fait
        /// accompli. Le poster avant l'effacement le rendrait mensonger dès
        /// que la suppression échoue — et personne ne peut vérifier de son
        /// côté, puisqu'il n'a précisément plus accès à rien.
        ///
        /// C'est aussi la seule trace qu'il lui reste, et c'est voulu : nous
        /// n'avons gardé aucun dossier à son nom pour lui en fournir une
        /// autre. D'où la ligne « gardez ce message » dans le gabarit.
        ///
        /// UN ÉCHEC D'ENVOI NE REMET RIEN EN CAUSE. Le compte est parti ; un
        /// SMTP en panne ne le ressuscite pas, et rendre une erreur au
        /// navigateur ferait croire au contraire — un parent qui recommence
        /// alors que tout est déjà effacé. `EnvoyerAsync` avale d'ailleurs
        /// déjà ses propres exceptions et rend `false`.
        /// </summary>
        private async Task ConfirmerLaSuppressionAsync(string? adresse, string? prenom)
        {
            if (string.IsNullOrWhiteSpace(adresse))
            {
                _logger.LogWarning("Compte supprime sans adresse : aucune confirmation envoyee.");
                return;
            }

            var envoye = await _email.EnvoyerAsync(
                adresse,
                "Votre compte Mimia a été supprimé",
                "compte-supprime",
                new Dictionary<string, string>
                {
                    ["virgulePrenom"] = string.IsNullOrWhiteSpace(prenom) ? "" : $", {prenom}",

                    // L'aperçu affiché dans la liste des messages, avant
                    // ouverture. Sans lui, le client de messagerie y met les
                    // premiers mots du gabarit, souvent une balise.
                    ["apercu"] = "Votre compte et toutes vos données ont été effacés de nos serveurs.",
                });

            if (!envoye)
            {
                // Tracé en erreur : c'est la confirmation d'une opération
                // irréversible qui n'est pas partie. Rien à rejouer — l'adresse
                // n'existe plus nulle part chez nous — mais il faut le savoir.
                _logger.LogError(
                    "Confirmation de suppression NON ENVOYEE : le compte est bien efface, "
                    + "mais son titulaire n'en a pas la trace ecrite.");
            }
        }

        /// <summary>
        /// L'adresse est-elle celle du compte de démonstration ?
        ///
        /// Lue dans la configuration, jamais écrite en dur : c'est la MÊME clé
        /// qui sert au semis de démarrage et à l'interrupteur d'activation. La
        /// recopier ici créerait une troisième vérité, qui finirait par
        /// diverger des deux autres.
        /// </summary>
        private bool EstCompteDemonstration(string? mail)
        {
            var adresse = _configuration["Admin:CompteTest:Email"];

            return !string.IsNullOrWhiteSpace(adresse)
                   && string.Equals(mail?.Trim(), adresse, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ApplicationUser?> UtilisateurCourantAsync()
        {
            // `sub` plutôt que le nom : c'est l'identifiant stable, le mail peut changer.
            var identifiant = User.FindFirst("sub")?.Value
                              ?? _userManager.GetUserId(User);

            return string.IsNullOrWhiteSpace(identifiant)
                ? null
                : await _userManager.FindByIdAsync(identifiant);
        }

        private static string Traduire(IdentityError erreur) => erreur.Code switch
        {
            "PasswordMismatch" => "Le mot de passe actuel est incorrect.",
            "PasswordTooShort" => "Le nouveau mot de passe doit contenir au moins 10 caractères.",
            "PasswordRequiresDigit" => "Le nouveau mot de passe doit contenir un chiffre.",
            "PasswordRequiresUpper" => "Le nouveau mot de passe doit contenir une majuscule.",
            "PasswordRequiresLower" => "Le nouveau mot de passe doit contenir une minuscule.",
            "PasswordRequiresNonAlphanumeric" => "Le nouveau mot de passe doit contenir un caractère spécial.",
            _ => erreur.Description,
        };

        public class ChangementMotDePasse
        {
            [Required(ErrorMessage = "Le mot de passe actuel est obligatoire.")]
            public string? MotDePasseActuel { get; set; }

            [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
            [StringLength(100, MinimumLength = 10,
                ErrorMessage = "Le mot de passe doit contenir au moins 10 caractères.")]
            public string? NouveauMotDePasse { get; set; }
        }
    }
}
