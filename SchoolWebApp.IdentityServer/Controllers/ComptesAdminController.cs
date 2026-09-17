using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.IdentityServer.Data;
using SchoolWebApp.IdentityServer.Services;

namespace SchoolWebApp.IdentityServer.Controllers
{
    /// <summary>
    /// L'effacement d'une identité par un ADMINISTRATEUR.
    ///
    /// POURQUOI CE CONTRÔLEUR EXISTE
    /// -----------------------------
    /// Un compte vit dans deux bases : les données de la famille dans l'API
    /// métier, les identifiants ici. `CompteController` sait effacer la seconde,
    /// mais seulement pour SON titulaire — le jeton dit qui parle, et on efface
    /// celui-là.
    ///
    /// L'administrateur n'avait donc aucun chemin. Sa suppression d'un parent
    /// n'emportait que les données, et l'identité restait. Deux conséquences,
    /// dont la seconde est la grave :
    ///
    ///   1. Une réinscription avec la même adresse était refusée — « un compte
    ///      existe déjà avec cette adresse email ». C'est le symptôme visible.
    ///
    ///   2. Le parant « supprimé » pouvait SE RECONNECTER. Son identité étant
    ///      intacte, l'API lui recréait un compte parent vierge à la volée
    ///      (`GetOrCreateAsync`). La suppression effaçait les données, pas
    ///      l'accès.
    ///
    /// POURQUOI ICI ET NON DANS L'API MÉTIER
    /// -------------------------------------
    /// L'API n'a pas accès à la base d'identité, et lui donner ce pouvoir
    /// reviendrait à dupliquer la gestion des credentials dans deux
    /// applications. Le front enchaîne donc les deux appels, exactement comme il
    /// le fait déjà quand un parent efface son propre compte.
    /// </summary>
    [ApiController]
    [Route("api/admin/comptes")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class ComptesAdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IServiceEmail _email;
        private readonly IBannissementService _bannissements;
        private readonly ILogger<ComptesAdminController> _logger;

        public ComptesAdminController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IServiceEmail email,
            IBannissementService bannissements,
            ILogger<ComptesAdminController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _bannissements = bannissements ?? throw new ArgumentNullException(nameof(bannissements));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// CRÉE L'IDENTITÉ D'UN PARENT, À LA DEMANDE D'UN ADMINISTRATEUR —
        /// Camara, le 16/09/2026 : « je peux tout faire sauf créer un parent
        /// et lui donner un rôle ».
        ///
        /// DEUX FAÇONS DE NAÎTRE, ET C'EST L'ADMINISTRATEUR QUI CHOISIT.
        ///
        /// AVEC UN MOT DE PASSE — Camara, le 17/09/2026 : « je dois créer aussi
        /// le mot de passe… et quand je crée le compte, il doit être directement
        /// actif ». Le compte est utilisable dans la seconde, aucun courriel ne
        /// part, et le parent se connecte avec ce que l'administrateur lui a
        /// donné. C'est le cas des comptes créés en face à face ou au téléphone.
        ///
        /// CE QUE ÇA COÛTE, ET C'EST ASSUMÉ : le mot de passe passe par le
        /// navigateur de l'administrateur, et il le connaît. Il circulera donc
        /// par oral ou par message. Il reste changeable par le parent depuis son
        /// compte, et « mot de passe oublié » fonctionne comme pour tout le
        /// monde.
        ///
        /// SANS MOT DE PASSE, le comportement d'avant est conservé : le compte
        /// naît avec un mot de passe aléatoire que personne ne connaît, et le
        /// parent reçoit le courriel de réinitialisation — le même que « mot de
        /// passe oublié » — pour choisir le sien.
        ///
        /// Le mot de passe aléatoire n'est pas un détail : sans lui, le compte
        /// n'aurait « pas de mot de passe » au sens d'Identity, et « mot de
        /// passe oublié » refuserait de lui renvoyer un lien si le premier
        /// expirait (deux heures).
        ///
        /// L'ADRESSE EST CONFIRMÉE D'OFFICE : c'est l'administrateur qui en
        /// répond. Un lien de confirmation de plus, avant le lien de mot de
        /// passe, ferait deux courriels pour un seul geste.
        ///
        /// APPELÉ AVANT la création de la fiche par l'API métier — l'inverse de
        /// la suppression. Le `sub` rendu ici est ce qui les relie ; si la
        /// fiche échouait ensuite, la première connexion du parent la créerait
        /// (`GetOrCreateAsync`), rien ne serait perdu.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Creer([FromBody] CreerCompteRequest requete)
        {
            if (!User.HasClaim("role", "Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var email = requete.Email!.Trim();

            if (await _bannissements.EstBanniAsync(email))
            {
                return BadRequest(new { message = "Cette adresse est bannie : aucun compte ne peut être créé avec." });
            }

            if (await _userManager.FindByEmailAsync(email) is not null)
            {
                return Conflict(new { message = "Un compte existe déjà avec cette adresse." });
            }

            var utilisateur = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Prenom = requete.Prenom?.Trim(),
                Nom = requete.Nom?.Trim(),
                EmailConfirmed = true,
            };

            var choisi = !string.IsNullOrWhiteSpace(requete.MotDePasse);

            var creation = await _userManager.CreateAsync(
                utilisateur, choisi ? requete.MotDePasse! : MotDePasseAleatoire());

            if (!creation.Succeeded)
            {
                _logger.LogError(
                    "Echec de la creation admin d'un compte : {Erreurs}.",
                    string.Join(" ", creation.Errors.Select(e => e.Description)));

                // LES RAISONS REMONTENT QUAND LE MOT DE PASSE VIENT DE LUI.
                //
                // « Le compte n'a pas pu être créé » était suffisant tant que le
                // mot de passe était aléatoire : un échec ne pouvait alors venir
                // que du serveur, et l'administrateur n'y pouvait rien. Depuis
                // qu'il le saisit, la cause la plus probable est sa politique —
                // trop court, pas de chiffre — et il doit lire laquelle pour
                // corriger. Un message générique le ferait réessayer à
                // l'aveugle.
                if (choisi)
                {
                    return BadRequest(new
                    {
                        message = string.Join(" ", creation.Errors.Select(e => e.Description)),
                    });
                }

                return StatusCode(500, new { message = "Le compte de connexion n'a pas pu être créé." });
            }

            _logger.LogWarning(
                "Compte {UserId} cree par un administrateur ({Origine}).",
                utilisateur.Id,
                choisi ? "mot de passe fixe par l administrateur" : "courriel de choix du mot de passe");

            // AUCUN COURRIEL QUAND LE MOT DE PASSE EST DÉJÀ CHOISI.
            //
            // Envoyer quand même un lien de réinitialisation dirait au parent
            // que son compte attend quelque chose de lui, alors qu'il est prêt.
            // Et le lien resterait valable deux heures dans une boîte, pour un
            // mot de passe qu'on vient de lui donner de vive voix.
            if (choisi)
            {
                return StatusCode(StatusCodes.Status201Created, new
                {
                    id = utilisateur.Id,
                    email,
                    courrielParti = false,
                    motDePasseDefini = true,
                });
            }

            // LE COURRIEL PART APRÈS LA CRÉATION, et son échec ne la défait pas :
            // le compte existe, le parent peut toujours demander un lien depuis
            // « mot de passe oublié ». L'administrateur en est prévenu.
            var courrielParti = true;
            try
            {
                var jeton = await _userManager.GeneratePasswordResetTokenAsync(utilisateur);
                var lien = Url.Action("ResetPassword", "Account", new { jeton, email }, Request.Scheme)!;

                await _email.EnvoyerAsync(
                    email,
                    "Bienvenue sur Mimia — choisissez votre mot de passe",
                    "reinitialisation",
                    new Dictionary<string, string>
                    {
                        ["apercu"] = "Votre compte Mimia est prêt. Un lien valable deux heures pour choisir votre mot de passe.",
                        ["lien"] = lien,
                    });
            }
            catch (Exception ex)
            {
                courrielParti = false;
                _logger.LogError(ex, "Compte {UserId} cree, mais le courriel de mot de passe n'est pas parti.", utilisateur.Id);
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = utilisateur.Id,
                email,
                courrielParti,
                motDePasseDefini = false,
            });
        }

        /// <summary>
        /// RÉINITIALISE LE MOT DE PASSE D'UN PARENT — Camara, le 17/09/2026 :
        /// « en tant qu'admin, je peux réinitialiser le mot de passe du parent
        /// directement quand je vais dans les modifications ».
        ///
        /// PAR UN JETON, ET NON EN ÉCRIVANT LE HACHAGE. `ResetPasswordAsync` est
        /// le chemin prévu par Identity pour poser un mot de passe sans connaître
        /// l'ancien : il applique la politique, refait le hachage avec les
        /// paramètres courants, et — le point important — RENOUVELLE LE TAMPON DE
        /// SÉCURITÉ. Les sessions ouvertes ailleurs tombent donc à leur prochaine
        /// validation, ce qu'on attend d'une réinitialisation.
        ///
        /// TROIS COMPTES SONT HORS DE PORTÉE, et chacun pour sa raison :
        ///
        /// — LE SIEN. Changer son propre mot de passe se fait sur sa page de
        ///   profil, avec l'ancien mot de passe demandé. Le faire d'ici serait un
        ///   contournement de cette vérification.
        ///
        /// — UN SUPER-ADMINISTRATEUR. Un administrateur ordinaire qui pourrait
        ///   poser le mot de passe du compte au-dessus du sien prendrait la main
        ///   sur tout le site en un clic. C'est la seule prise de contrôle
        ///   complète que cette route rendrait possible, et elle est fermée pour
        ///   TOUT LE MONDE, super-administrateur compris : lui non plus ne passe
        ///   pas par ici, il a sa page de profil.
        ///
        /// — LE COMPTE DE DÉMONSTRATION. Il est partagé et son mot de passe est
        ///   celui du semis : le changer casserait la démonstration pour tout le
        ///   monde, sans que personne ne comprenne pourquoi.
        /// </summary>
        [HttpPut("{email}/mot-de-passe")]
        public async Task<IActionResult> ReinitialiserMotDePasse(
            string email, [FromBody] ChangerMotDePasseRequest requete)
        {
            // Le rôle se lit dans le claim `role` et le refus est rendu
            // directement — mêmes raisons que pour la suppression plus bas.
            if (!User.HasClaim("role", "Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(email)) return BadRequest();

            var utilisateur = await _userManager.FindByEmailAsync(email.Trim());
            if (utilisateur is null) return NotFound();

            var soi = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(soi) && soi == utilisateur.Id)
            {
                return BadRequest(new
                {
                    message = "Pour changer votre propre mot de passe, passez par "
                              + "votre page de profil.",
                });
            }

            if (await _userManager.IsInRoleAsync(utilisateur, "SuperAdmin"))
            {
                return BadRequest(new
                {
                    message = "Le mot de passe d'un super-administrateur ne se "
                              + "change pas depuis l'administration.",
                });
            }

            if (EstCompteDemonstration(utilisateur.Email))
            {
                return BadRequest(new
                {
                    message = "Le mot de passe du compte de démonstration ne peut "
                              + "pas être changé : il est partagé.",
                });
            }

            var jeton = await _userManager.GeneratePasswordResetTokenAsync(utilisateur);
            var resultat = await _userManager.ResetPasswordAsync(
                utilisateur, jeton, requete.MotDePasse!);

            if (!resultat.Succeeded)
            {
                // LES RAISONS REMONTENT. La cause la plus probable est la
                // politique — trop court, pas de chiffre — et l'administrateur
                // doit lire laquelle pour corriger.
                return BadRequest(new
                {
                    message = string.Join(" ", resultat.Errors.Select(e => e.Description)),
                });
            }

            // Tracé en avertissement : un acte d'administration sur le compte de
            // quelqu'un d'autre, et celui-ci lui coupe ses sessions en cours.
            _logger.LogWarning(
                "Mot de passe du compte {UserId} reinitialise par un administrateur.",
                utilisateur.Id);

            return NoContent();
        }

        public class ChangerMotDePasseRequest
        {
            /// <summary>
            /// PAS DE `MinLength` ICI : la longueur minimale est celle de la
            /// politique d'Identity, déclarée une seule fois au démarrage. La
            /// répéter en annotation ferait deux règles à tenir d'accord.
            /// </summary>
            [Required, StringLength(200)]
            public string? MotDePasse { get; set; }
        }

        /// <summary>
        /// Trente-deux octets aléatoires en base64 : assez long et varié pour
        /// passer toute politique de mot de passe, et jamais montré à personne.
        /// </summary>
        private static string MotDePasseAleatoire() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!";

        public class CreerCompteRequest
        {
            [Required, EmailAddress, StringLength(255)]
            public string? Email { get; set; }

            [StringLength(100)]
            public string? Prenom { get; set; }

            [StringLength(100)]
            public string? Nom { get; set; }

            /// <summary>
            /// Le mot de passe choisi par l'administrateur, ou null.
            ///
            /// FACULTATIF, et les deux cas sont voulus : renseigné, le compte est
            /// utilisable tout de suite et aucun courriel ne part ; absent, le
            /// parent reçoit le lien pour choisir le sien.
            ///
            /// PAS DE `MinLength` ICI. La longueur minimale est celle de la
            /// politique d'Identity, déclarée une seule fois au démarrage ; la
            /// répéter en annotation ferait deux règles à tenir d'accord, et
            /// celle-ci mentirait le jour où l'autre changerait. La borne haute,
            /// elle, ne protège que contre un envoi absurde.
            /// </summary>
            [StringLength(200)]
            public string? MotDePasse { get; set; }
        }

        /// <summary>
        /// Efface l'identité d'un autre utilisateur, désigné par son adresse.
        ///
        /// APPELÉ APRÈS l'effacement des données par l'API métier, jamais avant.
        /// C'est le même ordre que pour une suppression demandée par le parent
        /// lui-même, et pour la même raison : tant que l'identité existe,
        /// quelqu'un peut encore atteindre les données. Dans l'autre sens, on
        /// laisserait des données d'enfants sans personne pour y accéder.
        ///
        /// L'ADRESSE ET NON L'IDENTIFIANT : c'est ce que la liste des parents de
        /// l'administration affiche, et le seul lien qu'elle ait entre les deux
        /// bases. Le `sub` n'y figure pas — il est délibérément absent des
        /// réponses de l'API métier.
        /// </summary>
        [HttpDelete("{email}")]
        public async Task<IActionResult> Supprimer(string email)
        {
            // LE RÔLE SE LIT DANS LE CLAIM `role`, PAS AVEC IsInRole().
            //
            // Un jeton OpenIddict porte le rôle sous le type "role" et non sous
            // le `ClaimTypes.Role` de .NET. `IsInRole("Admin")` renverrait faux
            // sans remappage — et un contrôle d'accès qui échoue à l'ouverture
            // se remarque, alors qu'ici il aurait fermé la porte à tout le
            // monde, y compris aux administrateurs.
            //
            // ET ON REND LE CODE DIRECTEMENT, PAS `Forbid()`.
            //
            // `AddIdentity` fait du COOKIE le schéma par défaut de cette
            // application. `Forbid()` sans argument s'adresse à ce schéma-là,
            // qui répond par une REDIRECTION vers une page de refus — un 302 et
            // du HTML, là où le navigateur attend un statut. Le front aurait vu
            // une réponse incompréhensible au lieu d'un refus net.
            if (!User.HasClaim("role", "Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (string.IsNullOrWhiteSpace(email)) return BadRequest();

            var utilisateur = await _userManager.FindByEmailAsync(email.Trim());

            // DÉJÀ PARTI VAUT SUCCÈS. L'administrateur veut que ce compte
            // n'existe plus ; s'il n'existe pas, c'est fait. Rendre 404 ferait
            // échouer l'enchaînement du front sur une suppression pourtant
            // aboutie — le cas se produit dès qu'on rejoue l'opération.
            if (utilisateur is null)
            {
                _logger.LogInformation(
                    "Suppression admin : aucune identite pour cette adresse, rien a faire.");
                return NoContent();
            }

            // Le compte de démonstration ne se supprime pas, ici comme ailleurs.
            // Il est partagé, c'est celui qu'on donne à voir, et le semis de
            // démarrage ne ressuscite pas un compte disparu.
            if (EstCompteDemonstration(utilisateur.Email))
            {
                return BadRequest(new
                {
                    message = "Le compte de démonstration ne peut pas être supprimé.",
                });
            }

            // UN ADMINISTRATEUR NE S'EFFACE PAS PAR CETTE PORTE.
            //
            // Rien ne l'interdirait techniquement, et c'est justement le
            // problème : un clic dans une liste, et il perd son propre accès
            // sans avoir rien confirmé. La suppression de son compte existe, sur
            // sa page de profil, avec la saisie du prénom qui va avec.
            var soi = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(soi) && soi == utilisateur.Id)
            {
                return BadRequest(new
                {
                    message = "Vous ne pouvez pas supprimer votre propre compte depuis "
                              + "l'administration. Passez par votre page de profil.",
                });
            }

            var resultat = await _userManager.DeleteAsync(utilisateur);

            if (!resultat.Succeeded)
            {
                _logger.LogError(
                    "Echec de la suppression admin du compte {UserId} : {Erreurs}.",
                    utilisateur.Id,
                    string.Join(" ", resultat.Errors.Select(e => e.Description)));

                return StatusCode(500, new
                {
                    message = "Le compte de connexion n'a pas pu être supprimé.",
                });
            }

            // Tracé en avertissement : c'est un acte d'administration
            // irréversible sur le compte de quelqu'un d'autre.
            _logger.LogWarning(
                "Compte {UserId} supprime par un administrateur.", utilisateur.Id);

            return NoContent();
        }

        private bool EstCompteDemonstration(string? mail)
        {
            var adresse = _configuration["Admin:CompteTest:Email"];

            return !string.IsNullOrWhiteSpace(adresse)
                   && string.Equals(mail?.Trim(), adresse, StringComparison.OrdinalIgnoreCase);
        }
    }
}
