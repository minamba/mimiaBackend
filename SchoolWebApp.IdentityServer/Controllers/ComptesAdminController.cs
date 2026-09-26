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
        ///
        /// DÉSIGNÉ PAR SON `sub` DEPUIS LE 23/09/2026 — voir `ChangerEmail`
        /// plus bas pour la raison : l'adresse n'est pas une clé sûre.
        /// </summary>
        [HttpPut("par-id/{id}/mot-de-passe")]
        public async Task<IActionResult> ReinitialiserMotDePasse(
            string id, [FromBody] ChangerMotDePasseRequest requete)
        {
            // Le rôle se lit dans le claim `role` et le refus est rendu
            // directement — mêmes raisons que pour la suppression plus bas.
            if (!User.HasClaim("role", "Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var utilisateur = await _userManager.FindByIdAsync(id.Trim());
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
        /// CHANGE L'ADRESSE DE CONNEXION D'UN PARENT — Camara, le 23/09/2026 :
        /// « j'ai changé l'adresse mail d'un parent dans l'onglet
        /// administrateur, mais elle ne peut plus se connecter ».
        ///
        /// LE DÉFAUT QU'ON CORRIGE, ET IL ÉTAIT SÉRIEUX. « Modifier le compte »
        /// n'appelait QUE l'API métier, qui écrit `Parents.Mail` — une colonne
        /// d'affichage et d'envoi de courriels. Rien n'atteignait cette base-ci,
        /// où vivent `UserName` et `Email`. Le parent se retrouvait avec :
        ///
        ///   — une connexion qui n'accepte plus que son ANCIENNE adresse, sans
        ///     que rien ne le lui dise ;
        ///   — des bilans et des relances envoyés à la NOUVELLE ;
        ///   — un « mot de passe oublié » qui répond « aucun compte » s'il y
        ///     saisit la nouvelle.
        ///
        /// Aucune donnée n'était perdue — la fiche est liée au `sub`, pas à
        /// l'adresse — mais le compte devenait inaccessible à qui ne devinait
        /// pas qu'il fallait garder l'ancienne.
        ///
        /// PAR `SetEmailAsync` ET `SetUserNameAsync`, JAMAIS EN ÉCRIVANT LES
        /// PROPRIÉTÉS. Identity cherche un compte par ses colonnes NORMALISÉES
        /// (`NormalizedEmail`, `NormalizedUserName`), pas par celles qu'on lit.
        /// Poser `utilisateur.Email = ...` produirait exactement le symptôme
        /// qu'on vient de corriger : une adresse juste à l'écran, et un compte
        /// introuvable à la connexion. Ces deux méthodes renormalisent et
        /// persistent.
        ///
        /// LES DEUX, ET PAS SEULEMENT L'ADRESSE. `UserName` vaut l'adresse
        /// depuis la création du compte (`Creer`, plus haut) et la page de
        /// connexion s'en sert. N'en changer qu'une laisserait le compte
        /// joignable par l'ancienne et par la nouvelle, selon le chemin — une
        /// incohérence qui ne se voit qu'au pire moment.
        ///
        /// L'ADRESSE RESTE CONFIRMÉE. `SetEmailAsync` repasse `EmailConfirmed`
        /// à faux : c'est le bon réflexe quand le titulaire change son adresse
        /// lui-même, pas ici. C'est l'administrateur qui en répond, comme à la
        /// création — et un compte non confirmé aurait été bloqué à l'entrée.
        ///
        /// LES SESSIONS OUVERTES TOMBENT, et c'est voulu : `SetEmailAsync`
        /// renouvelle le tampon de sécurité. Changer l'identifiant de connexion
        /// de quelqu'un sans couper ses sessions laisserait un jeton valide
        /// portant une adresse qui n'existe plus.
        ///
        /// TROIS COMPTES SONT HORS DE PORTÉE, les mêmes que pour le mot de
        /// passe, plus un : un compte de `Admin:Emails`. C'est l'ADRESSE qui y
        /// porte le rôle — la renommer ne déplace pas l'administrateur, elle le
        /// fait disparaître, et personne ne peut plus le rétablir depuis le
        /// site. L'API métier garde déjà cette porte (`ComptesProteges`) ;
        /// celle-ci la garde de son côté, parce qu'une seule des deux fermée
        /// n'est pas une garde.
        ///
        /// DÉSIGNÉ PAR SON `sub`, ET SURTOUT PAS PAR SON ADRESSE.
        ///
        /// La première version de cette route prenait l'ancienne adresse comme
        /// clé — la seule que l'administration avait. Elle ne pouvait donc PAS
        /// réparer un compte déjà désynchronisé : l'écran lui passait l'adresse
        /// métier, l'identité en portait une autre, et la route répondait 404.
        /// Autrement dit, la route écrite pour remettre les deux bases d'accord
        /// échouait précisément sur les comptes qui en avaient besoin. Constaté
        /// sur un vrai compte le 23/09/2026, quelques heures après sa mise en
        /// ligne.
        ///
        /// Le `sub` ne bouge jamais. C'est lui qui relie déjà la fiche à
        /// l'identité, et la seule clé qu'un changement d'adresse ne périme
        /// pas. L'API métier l'expose désormais à l'administration seule
        /// (`ParentAdmin.IdentityUserId`).
        ///
        /// ELLE RÉPARE EN PASSANT. Le front l'appelle à chaque enregistrement
        /// d'une fiche parent, sans chercher à deviner si l'adresse a changé :
        /// c'est cette route qui compare, et elle compare à ce que l'identité
        /// porte VRAIMENT. Un compte dont les deux bases avaient divergé se
        /// remet donc d'aplomb au premier passage dans la fenêtre, sans que
        /// personne ait à s'en apercevoir.
        /// </summary>
        [HttpPut("par-id/{id}/email")]
        public async Task<IActionResult> ChangerEmail(
            string id, [FromBody] ChangerEmailRequest requete)
        {
            if (!User.HasClaim("role", "Admin"))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var nouvelle = requete.Email!.Trim();

            var utilisateur = await _userManager.FindByIdAsync(id.Trim());
            if (utilisateur is null) return NotFound();

            // LA COMPARAISON SE FAIT CONTRE CE QUE L'IDENTITÉ PORTE, jamais
            // contre ce que l'écran affiche. C'est toute la différence : si les
            // deux bases avaient divergé, l'écran dirait « rien n'a changé » là
            // où il y a justement tout à recoller.
            var ancienne = utilisateur.Email ?? string.Empty;

            // RIEN À FAIRE VAUT SUCCÈS. Le front appelle à chaque
            // enregistrement, même pour un nom corrigé : une adresse identique
            // ne doit pas couper les sessions du parent pour rien.
            if (string.Equals(ancienne, nouvelle, StringComparison.OrdinalIgnoreCase)
                && string.Equals(utilisateur.UserName ?? string.Empty, nouvelle, StringComparison.OrdinalIgnoreCase))
            {
                return NoContent();
            }

            if (EstCompteDemonstration(utilisateur.Email))
            {
                return BadRequest(new
                {
                    message = "L'adresse du compte de démonstration ne peut pas "
                              + "être changée : elle est partagée.",
                });
            }

            if (EstCompteProtege(utilisateur.Email))
            {
                return BadRequest(new
                {
                    message = "L'adresse d'un administrateur ne se change pas ici : "
                              + "c'est elle qui porte le rôle, et la renommer ferait "
                              + "perdre l'accès à l'administration.",
                });
            }

            if (await _userManager.IsInRoleAsync(utilisateur, "SuperAdmin"))
            {
                return BadRequest(new
                {
                    message = "L'adresse d'un super-administrateur ne se change pas "
                              + "depuis l'administration.",
                });
            }

            if (await _bannissements.EstBanniAsync(nouvelle))
            {
                return BadRequest(new
                {
                    message = "Cette adresse est bannie : aucun compte ne peut la porter.",
                });
            }

            // LES DEUX CONFLITS SE VÉRIFIENT AVANT D'ÉCRIRE QUOI QUE CE SOIT.
            // Sans ça, l'adresse pouvait passer et le nom d'utilisateur échouer
            // juste après — laissant le compte à moitié renommé, c'est-à-dire
            // dans l'état même qu'on répare ici.
            //
            // SAUF LUI-MÊME. Un compte à moitié désynchronisé — l'adresse déjà
            // posée, le nom d'utilisateur resté en arrière — SE trouverait
            // lui-même et se verrait refuser sa propre réparation. C'est
            // exactement la situation qu'on vient de corriger à la main.
            var porteurEmail = await _userManager.FindByEmailAsync(nouvelle);
            var porteurNom = await _userManager.FindByNameAsync(nouvelle);

            if ((porteurEmail is not null && porteurEmail.Id != utilisateur.Id)
                || (porteurNom is not null && porteurNom.Id != utilisateur.Id))
            {
                return Conflict(new { message = "Un compte existe déjà avec cette adresse." });
            }

            var pose = await _userManager.SetEmailAsync(utilisateur, nouvelle);
            if (!pose.Succeeded) return EchecChangement(utilisateur.Id, pose);

            var renomme = await _userManager.SetUserNameAsync(utilisateur, nouvelle);
            if (!renomme.Succeeded) return EchecChangement(utilisateur.Id, renomme);

            // Reposée APRÈS les deux : `SetEmailAsync` l'a remise à faux.
            utilisateur.EmailConfirmed = true;
            var confirme = await _userManager.UpdateAsync(utilisateur);
            if (!confirme.Succeeded) return EchecChangement(utilisateur.Id, confirme);

            // Tracé en avertissement, sans les adresses : c'est un acte
            // d'administration sur le compte de quelqu'un d'autre, et il lui
            // coupe ses sessions en cours.
            _logger.LogWarning(
                "Adresse de connexion du compte {UserId} changee par un administrateur.",
                utilisateur.Id);

            return NoContent();
        }

        private IActionResult EchecChangement(string userId, IdentityResult resultat)
        {
            _logger.LogError(
                "Echec du changement d'adresse du compte {UserId} : {Erreurs}.",
                userId,
                string.Join(" ", resultat.Errors.Select(e => e.Description)));

            // LES RAISONS REMONTENT : la cause la plus probable est une adresse
            // mal formée ou déjà prise, et l'administrateur doit lire laquelle.
            return BadRequest(new
            {
                message = string.Join(" ", resultat.Errors.Select(e => e.Description)),
            });
        }

        public class ChangerEmailRequest
        {
            [Required, EmailAddress(ErrorMessage = "Adresse email invalide."), StringLength(255)]
            public string? Email { get; set; }
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
        /// Efface l'identité d'un autre utilisateur, désigné par son `sub`.
        ///
        /// APPELÉ APRÈS l'effacement des données par l'API métier, jamais avant.
        /// C'est le même ordre que pour une suppression demandée par le parent
        /// lui-même, et pour la même raison : tant que l'identité existe,
        /// quelqu'un peut encore atteindre les données. Dans l'autre sens, on
        /// laisserait des données d'enfants sans personne pour y accéder.
        ///
        /// LE `sub` ET NON L'ADRESSE — changé le 23/09/2026, avec les deux
        /// autres routes. L'adresse était le seul lien dont l'administration
        /// disposait entre les deux bases ; c'était aussi le seul qui pouvait
        /// devenir faux. Sur un compte désynchronisé, cette route effaçait donc
        /// les données SANS effacer l'identité, et rendait 404 — le parent
        /// « supprimé » gardait son accès, et l'API lui recréait un compte
        /// vierge à la connexion suivante. Exactement le défaut que ce
        /// contrôleur avait été écrit pour fermer.
        /// </summary>
        [HttpDelete("par-id/{id}")]
        public async Task<IActionResult> Supprimer(string id)
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

            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var utilisateur = await _userManager.FindByIdAsync(id.Trim());

            // DÉJÀ PARTI VAUT SUCCÈS. L'administrateur veut que ce compte
            // n'existe plus ; s'il n'existe pas, c'est fait. Rendre 404 ferait
            // échouer l'enchaînement du front sur une suppression pourtant
            // aboutie — le cas se produit dès qu'on rejoue l'opération.
            if (utilisateur is null)
            {
                _logger.LogInformation(
                    "Suppression admin : aucune identite sous cet identifiant, rien a faire.");
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

        /// <summary>
        /// Ce compte est-il celui d'un administrateur déclaré ?
        ///
        /// LA MÊME CLÉ QUE LE SEMIS DES RÔLES, `Admin:Emails` : c'est elle qui
        /// décide du rôle au démarrage, et lire ailleurs ferait diverger les
        /// deux réponses au premier changement d'adresse — la garde
        /// protégerait alors un compte qui n'est plus le bon. Miroir de
        /// `ComptesProteges` côté API métier.
        /// </summary>
        private bool EstCompteProtege(string? mail)
        {
            if (string.IsNullOrWhiteSpace(mail)) return false;

            var adresses = _configuration.GetSection("Admin:Emails").Get<string[]>()
                           ?? Array.Empty<string>();

            return adresses.Any(a =>
                string.Equals(a?.Trim(), mail.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
