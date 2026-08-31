using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.IdentityServer.Data;
using SchoolWebApp.IdentityServer.Models;
using SchoolWebApp.IdentityServer.Services;

namespace SchoolWebApp.IdentityServer.Controllers
{
    /// <summary>
    /// Pages de connexion / inscription hébergées par le serveur d'identité.
    /// La SPA React n'affiche pas ces écrans : elle redirige vers /connect/authorize,
    /// qui aboutit ici quand l'utilisateur n'est pas encore authentifié.
    /// </summary>
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IServiceEmail _email;
        private readonly IModeTestService _modeTest;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IServiceEmail email,
            IModeTestService modeTest,
            ILogger<AccountController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _modeTest = modeTest ?? throw new ArgumentNullException(nameof(modeTest));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------------------------------------------------------
        // Connexion par email / mot de passe
        // ------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl,
                FournisseursExternes = await FournisseursExternesAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.FournisseursExternes = await FournisseursExternesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Le compte de démonstration éteint se comporte comme un compte
            // INEXISTANT — pas comme un compte verrouillé. Dire « ce compte est
            // désactivé » confirmerait qu'il existe, et donnerait à qui cherche
            // une adresse valide sur laquelle s'acharner.
            //
            // Vérifié AVANT l'appel à Identity : passer par la vérification du
            // mot de passe incrémenterait son compteur de tentatives, et un
            // compte éteint finirait verrouillé pour de bon.
            // LA MAINTENANCE REFUSE, ELLE NE SE CONTENTE PAS DE CACHER.
            //
            // Le rideau du site n'est qu'un affichage : il masque les écrans,
            // l'API répond toujours derrière. Ici, c'est un vrai refus — sans
            // jeton, aucune session ne s'ouvre, et le rideau ne se contourne
            // plus en tapant une adresse à la main.
            //
            // Vérifié AVANT le mot de passe, pour la même raison que le compte
            // de démonstration juste en dessous : passer par Identity
            // incrémenterait le compteur de tentatives, et un parent qui
            // insiste pendant une maintenance verrouillerait son propre compte
            // pour un quart d'heure — après la fin des travaux.
            if (await MaintenanceBloqueAsync(model.Email))
            {
                _logger.LogInformation(
                    "Connexion refusee : le site est en maintenance.");

                ModelState.AddModelError(string.Empty,
                    "Le site est actuellement en maintenance. La connexion est momentanement "
                    + "impossible, revenez dans quelques minutes.");

                return View(model);
            }

            if (await EstCompteTestEteintAsync(model.Email))
            {
                _logger.LogInformation(
                    "Connexion refusee : le compte de demonstration est desactive.");

                ModelState.AddModelError(string.Empty, "Adresse email ou mot de passe incorrect.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Connexion réussie pour {Email}.", model.Email);
                return RedirectToLocalOrHome(model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Ce compte est temporairement verrouillé suite à plusieurs tentatives échouées. Réessayez dans 15 minutes.");
                return View(model);
            }

            // ADRESSE NON CONFIRMÉE : on le DIT, contrairement au reste.
            //
            // Ailleurs on reste volontairement vague pour ne pas révéler
            // quelles adresses sont inscrites. Ici c'est différent : le mot de
            // passe vient d'être vérifié et il est bon — la personne EST le
            // titulaire du compte. Lui répondre « email ou mot de passe
            // incorrect » l'enverrait changer un mot de passe qui n'a rien,
            // encore et encore, sans jamais deviner qu'il manque un clic dans
            // sa boîte mail.
            if (result.IsNotAllowed)
            {
                _logger.LogInformation(
                    "Connexion refusee : adresse non confirmee pour {Email}.", model.Email);

                return RedirectToAction(nameof(ConfirmezVotreAdresse), new
                {
                    mail = model.Email,
                    returnUrl = model.ReturnUrl,
                });
            }

            // Message volontairement générique : ne pas révéler si l'email existe.
            ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
            return View(model);
        }

        // ------------------------------------------------------------------
        // Inscription
        // ------------------------------------------------------------------
        /// <summary>
        /// EN ACCÈS PRIVÉ, L'INSCRIPTION EST FERMÉE — POUR DE BON.
        ///
        /// Le mode test ne faisait que MASQUER le lien « Créer un compte
        /// parent » sur la page de connexion. Le formulaire, lui, répondait
        /// toujours : taper l'adresse suffisait à créer un compte pendant une
        /// bêta censée être fermée. Un lien caché n'a jamais fermé une porte.
        ///
        /// Vérifié sur les DEUX verbes. Le GET ne suffirait pas : un formulaire
        /// ouvert avant la bascule reste postable, et c'est le POST qui crée
        /// le compte.
        ///
        /// Redirige vers la connexion plutôt que de refuser sèchement : c'est
        /// la page qui explique l'accès privé, et celle dont un bêta-testeur a
        /// besoin. Le `returnUrl` la traverse pour qu'il reparte ensuite là où
        /// il allait.
        /// </summary>
        private async Task<IActionResult?> RefuserSiAccesPriveAsync(string? returnUrl)
        {
            if (!await _modeTest.EstActifAsync()) return null;

            _logger.LogWarning(
                "Inscription refusee : le service est en acces prive.");

            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        [HttpGet]
        public async Task<IActionResult> Register(string? returnUrl = null)
        {
            if (await RefuserSiAccesPriveAsync(returnUrl) is { } refus) return refus;

            return View(new RegisterViewModel
            {
                ReturnUrl = returnUrl,
                FournisseursExternes = await FournisseursExternesAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (await RefuserSiAccesPriveAsync(model.ReturnUrl) is { } refus) return refus;

            model.FournisseursExternes = await FournisseursExternesAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Prenom = model.Prenom,
                Nom = model.Nom
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, TraduireErreurIdentity(error));
                }
                return View(model);
            }

            _logger.LogInformation("Nouveau compte parent créé : {Email}.", model.Email);

            // Alerte Telegram, APRÈS la création et avant la redirection. Elle
            // n'attend rien et ne peut rien casser : ses erreurs sont avalées
            // et journalisées à l'intérieur.
            await Helpers.TelegramHelper.NotifierNouveauParentAsync(
                _configuration, _logger, model.Prenom, model.Nom, model.Email);

            // PAS DE CONNEXION AUTOMATIQUE : l'adresse n'est pas encore
            // confirmée. Le connecter maintenant viderait la confirmation de
            // son sens — il utiliserait le service et ne cliquerait jamais,
            // jusqu'au jour où il aurait besoin d'un mot de passe oublié.
            await EnvoyerConfirmationAsync(user, model.Prenom);

            return RedirectToAction(nameof(ConfirmezVotreAdresse), new
            {
                mail = user.Email,
                returnUrl = model.ReturnUrl,
            });
        }

        /// <summary>
        /// « Vérifiez votre boîte. » L'écran qui suit une inscription.
        ///
        /// Une page à part plutôt qu'un message sur le formulaire : le parent
        /// doit comprendre qu'il n'a plus rien à faire ICI, et aller ailleurs.
        /// Un bandeau au-dessus des champs qu'il vient de remplir laisse croire
        /// qu'il reste quelque chose à corriger.
        /// </summary>
        [HttpGet]
        public IActionResult ConfirmezVotreAdresse(string? mail = null, string? returnUrl = null) =>
            View(new ConfirmationViewModel { Mail = mail, ReturnUrl = returnUrl });

        /// <summary>
        /// Le clic depuis le mail.
        ///
        /// Le jeton d'Identity est à usage unique et lié à ce compte : personne
        /// ne peut confirmer l'adresse d'un autre, et un lien rejoué ne fait
        /// rien de plus.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ConfirmerAdresse(
            string? userId, string? code, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                return View("AdresseNonConfirmee");
            }

            var utilisateur = await _userManager.FindByIdAsync(userId);
            if (utilisateur is null) return View("AdresseNonConfirmee");

            // Déjà confirmée : on ne renvoie pas d'erreur. Un parent qui
            // reclique sur son lien la semaine suivante n'a rien fait de mal.
            if (utilisateur.EmailConfirmed)
            {
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var resultat = await _userManager.ConfirmEmailAsync(
                utilisateur, Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)));

            if (!resultat.Succeeded)
            {
                _logger.LogWarning(
                    "Confirmation d'adresse refusee pour {UserId} : jeton invalide ou expire.",
                    userId);

                return View("AdresseNonConfirmee");
            }

            _logger.LogInformation("Adresse confirmee pour {UserId}.", userId);

            // Le mail de bienvenue part MAINTENANT, pas à l'inscription : il
            // invite à créer le profil de l'enfant, ce qui n'a de sens qu'une
            // fois le compte réellement ouvert.
            await EnvoyerBienvenueAsync(utilisateur.Email!, utilisateur.Prenom);

            // Connecté dans la foulée : il vient de prouver qu'il tient
            // l'adresse, lui redemander son mot de passe serait une porte de
            // plus pour rien.
            await _signInManager.SignInAsync(utilisateur, isPersistent: true);

            return RedirectToLocalOrHome(returnUrl);
        }

        /// <summary>
        /// Renvoie le lien de confirmation.
        ///
        /// Répond la même chose quoi qu'il arrive — adresse inconnue, déjà
        /// confirmée, ou lien réellement renvoyé. Distinguer les cas ferait de
        /// ce formulaire un outil pour savoir qui est inscrit.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenvoyerConfirmation(
            string? mail, string? returnUrl = null)
        {
            var utilisateur = string.IsNullOrWhiteSpace(mail)
                ? null
                : await _userManager.FindByEmailAsync(mail);

            if (utilisateur is not null && !utilisateur.EmailConfirmed)
            {
                await EnvoyerConfirmationAsync(utilisateur, utilisateur.Prenom);
            }

            TempData["ConfirmationRenvoyee"] = true;

            return RedirectToAction(nameof(ConfirmezVotreAdresse), new { mail, returnUrl });
        }

        /// <summary>
        /// Fabrique le jeton et envoie le lien.
        ///
        /// Le jeton est encodé en base64url : il voyage dans une URL, et sa
        /// forme brute contient des caractères que les clients de messagerie
        /// coupent ou réécrivent.
        /// </summary>
        private async Task EnvoyerConfirmationAsync(ApplicationUser utilisateur, string? prenom)
        {
            var jeton = await _userManager.GenerateEmailConfirmationTokenAsync(utilisateur);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(jeton));

            var lien = Url.Action(
                nameof(ConfirmerAdresse),
                "Account",
                new { userId = utilisateur.Id, code },
                Request.Scheme)!;

            await _email.EnvoyerAsync(
                utilisateur.Email!,
                "Confirmez votre adresse — Mimia",
                "confirmation",
                new Dictionary<string, string>
                {
                    ["virgulePrenom"] = string.IsNullOrWhiteSpace(prenom) ? "" : $", {prenom}",
                    ["apercu"] = "Un clic, et votre compte est ouvert.",
                    ["lien"] = lien,
                });
        }

        // ------------------------------------------------------------------
        // Mot de passe oublié
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult ForgotPassword(string? returnUrl = null) =>
            View(new ForgotPasswordViewModel { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email!);

            // La confirmation est identique que le compte existe ou non. Distinguer
            // les deux cas transformerait ce formulaire en outil d'énumération :
            // n'importe qui pourrait vérifier si une adresse est inscrite.
            if (user is not null && await _userManager.HasPasswordAsync(user))
            {
                var jeton = await _userManager.GeneratePasswordResetTokenAsync(user);
                var lien = Url.Action(
                    nameof(ResetPassword), "Account",
                    new { jeton, email = user.Email, returnUrl = model.ReturnUrl },
                    Request.Scheme)!;

                await _email.EnvoyerAsync(
                    user.Email!,
                    "Réinitialiser votre mot de passe Mimia",
                    "reinitialisation",
                    new Dictionary<string, string>
                    {
                        ["apercu"] = "Un lien valable deux heures pour choisir un nouveau mot de passe.",
                        ["lien"] = lien,
                    });
            }
            else if (user is not null)
            {
                // Compte Google : il n'a pas de mot de passe à réinitialiser.
                _logger.LogInformation(
                    "Reinitialisation demandee pour un compte externe : {Email}.", model.Email);
            }

            model.Envoye = true;
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string? jeton = null, string? email = null, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(jeton) || string.IsNullOrWhiteSpace(email))
            {
                return View("ResetPasswordInvalide");
            }

            return View(new ResetPasswordViewModel { Jeton = jeton, Email = email, ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email!);

            // Même réponse qu'en cas de succès si le compte n'existe pas : le
            // formulaire ne doit pas non plus servir à énumérer les adresses.
            if (user is null)
            {
                model.Reussi = true;
                return View(model);
            }

            var resultat = await _userManager.ResetPasswordAsync(user, model.Jeton!, model.Password!);

            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, TraduireErreurIdentity(erreur));
                }
                return View(model);
            }

            _logger.LogInformation("Mot de passe reinitialise pour {Email}.", model.Email);

            model.Reussi = true;
            return View(model);
        }

        /// <summary>
        /// URL publique du site, lue dans la configuration et jamais dans la
        /// requête : un en-tête Host falsifié enverrait des liens de
        /// réinitialisation vers un domaine contrôlé par l'attaquant.
        /// </summary>
        private string UrlDuSite() =>
            (_configuration["Clients:Spa:Url"] ?? "https://mimia.fr").TrimEnd('/');

        /// <summary>
        /// Le mail de bienvenue, quel que soit le chemin d'inscription.
        ///
        /// Écrit une seule fois parce qu'il partait de DEUX endroits — le
        /// formulaire et Google — et qu'un seul des deux l'envoyait vraiment.
        /// Deux copies d'un même message finissent toujours par diverger ;
        /// ici, l'une d'elles n'existait même pas.
        ///
        /// L'envoi n'est jamais bloquant : un SMTP en panne ne doit pas
        /// transformer une inscription réussie en erreur. Le service journalise
        /// et rend `false`, l'inscription se poursuit.
        /// </summary>
        /// <summary>
        /// L'adresse visée est-elle celle du compte de démonstration, alors
        /// qu'il est éteint ?
        ///
        /// Ne concerne QUE cette adresse : aucun compte de parent réel ne peut
        /// être bloqué par ce chemin, quelle que soit la valeur du réglage.
        /// </summary>
        private async Task<bool> EstCompteTestEteintAsync(string? mail)
        {
            var adresse = _configuration["Admin:CompteTest:Email"];

            if (string.IsNullOrWhiteSpace(adresse)
                || !string.Equals(mail?.Trim(), adresse, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !await _modeTest.CompteTestActifAsync();
        }

        private Task EnvoyerBienvenueAsync(string mail, string? prenom) =>
            _email.EnvoyerAsync(
                mail,
                "Bienvenue sur Mimia",
                "bienvenue",
                new Dictionary<string, string>
                {
                    ["virgulePrenom"] = string.IsNullOrWhiteSpace(prenom) ? "" : $", {prenom}",
                    ["apercu"] = "Votre compte est ouvert. Il ne reste qu'à créer le profil de votre enfant.",
                    ["lien"] = $"{UrlDuSite()}/eleves/nouveau",
                });

        // ------------------------------------------------------------------
        // Connexion externe (Google)
        // ------------------------------------------------------------------
        /// <summary>
        /// Cette adresse doit-elle être refusée parce que le site est en
        /// maintenance ?
        ///
        /// LA RÈGLE EST « ADMINISTRATEUR », PAS UNE ADRESSE ÉCRITE EN DUR.
        ///
        /// Une adresse dans le code aurait tenu jusqu'au jour où elle change —
        /// et ce jour-là, la seule façon de rouvrir le site aurait été un
        /// redéploiement. Le rôle, lui, est déjà la source de vérité : il est
        /// accordé aux adresses listées dans `Admin:Emails`, et c'est ce même
        /// rôle qui ouvre l'administration. Une seule liste à tenir.
        ///
        /// CE QUE ÇA CONCÈDE, ET C'EST ASSUMÉ. Pendant une maintenance, un
        /// administrateur qui se trompe de mot de passe lit « email ou mot de
        /// passe incorrect » là où les autres lisent « site en maintenance ».
        /// La différence dit donc qu'une adresse est administratrice. C'est le
        /// prix d'un message utile — et il ne se paie que pendant les travaux.
        /// </summary>
        private async Task<bool> MaintenanceBloqueAsync(string? email)
        {
            if (!await _modeTest.MaintenanceAsync()) return false;

            if (string.IsNullOrWhiteSpace(email)) return true;

            var utilisateur = await _userManager.FindByEmailAsync(email.Trim());

            // Compte inconnu : refusé comme les autres. Le message parle de
            // maintenance et non du compte — on ne renseigne personne sur
            // l'existence d'une adresse.
            if (utilisateur is null) return true;

            return !await _userManager.IsInRoleAsync(utilisateur, "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            // Sans ce contrôle, un provider non enregistré fait planter le
            // pipeline en 500 au lieu de renvoyer un message compréhensible.
            var disponibles = await FournisseursExternesAsync();
            if (!disponibles.Contains(provider, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Tentative de connexion via {Provider}, non configuré.", provider);
                TempData["ErreurExterne"] =
                    "Ce mode de connexion n'est pas disponible pour le moment. Utilisez votre email.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError is not null)
            {
                _logger.LogWarning("Erreur renvoyée par le fournisseur externe : {Error}", remoteError);
                TempData["ErreurExterne"] = "La connexion avec Google a échoué. Réessayez ou utilisez votre email.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                TempData["ErreurExterne"] = "Impossible de récupérer les informations Google. Réessayez.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            // LA MÊME PORTE, ET AVANT TOUTE OUVERTURE DE SESSION.
            //
            // Le bouton Google disparaît de la page pendant la maintenance,
            // mais l'adresse de retour reste appelable : ne fermer que le
            // formulaire aurait laissé un chemin ouvert à qui l'avait en
            // signet.
            //
            // Posé ICI et non plus bas : la branche suivante ouvre la session
            // d'un compte DÉJÀ rattaché à Google. Un garde placé après elle
            // n'aurait arrêté que les comptes à rattacher — c'est-à-dire
            // personne, puisque les habitués sont justement ceux qui sont
            // rattachés.
            var emailExterne = info.Principal.FindFirst(
                System.Security.Claims.ClaimTypes.Email)?.Value;

            if (await MaintenanceBloqueAsync(emailExterne))
            {
                _logger.LogInformation("Connexion externe refusee : le site est en maintenance.");

                TempData["ErreurExterne"] =
                    "Le site est actuellement en maintenance. La connexion est momentanement "
                    + "impossible, revenez dans quelques minutes.";

                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            // 1. Le compte externe est déjà lié à un utilisateur.
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("Connexion {Provider} réussie.", info.LoginProvider);
                return RedirectToLocalOrHome(returnUrl);
            }

            if (signInResult.IsLockedOut)
            {
                TempData["ErreurExterne"] = "Ce compte est temporairement verrouillé.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            // 2. Pas encore lié : on récupère l'email fourni par Google.
            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErreurExterne"] =
                    "Google n'a pas transmis d'adresse email. Créez un compte avec votre email à la place.";
                return RedirectToAction(nameof(Register), new { returnUrl });
            }

            // 3. Un compte local existe déjà avec cet email : on rattache le login externe.
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                var linkResult = await _userManager.AddLoginAsync(existingUser, info);
                if (!linkResult.Succeeded)
                {
                    TempData["ErreurExterne"] =
                        "Un compte existe déjà avec cette adresse. Connectez-vous avec votre mot de passe.";
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }

                // Google vient de prouver qu'il tient cette boîte : la
                // confirmation par mail n'apporterait rien de plus, et
                // laisserait un compte bloqué alors qu'il est déjà vérifié.
                if (!existingUser.EmailConfirmed)
                {
                    existingUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(existingUser);
                }

                await _signInManager.SignInAsync(existingUser, isPersistent: true);
                _logger.LogInformation("Compte {Email} rattaché au fournisseur {Provider}.", email, info.LoginProvider);
                return RedirectToLocalOrHome(returnUrl);
            }

            // 4. Création d'un nouveau compte parent depuis Google.
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, // Google a déjà vérifié l'adresse.
                Prenom = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value,
                Nom = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value,
                FournisseurExterne = info.LoginProvider
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                TempData["ErreurExterne"] = string.Join(" ", createResult.Errors.Select(TraduireErreurIdentity));
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            await _userManager.AddLoginAsync(user, info);

            // Le MÊME mail de bienvenue que par le formulaire.
            //
            // Il manquait sur ce chemin : un parent arrivé par Google était
            // inscrit, connecté, et reparti sans rien — pas même le lien vers
            // la création du profil de son enfant, qui est pourtant l'étape
            // suivante et la seule qui compte. Il n'y a aucune raison de
            // l'accueillir moins bien parce qu'il a cliqué sur un autre bouton.
            await EnvoyerBienvenueAsync(user.Email!, user.Prenom);

            await _signInManager.SignInAsync(user, isPersistent: true);

            _logger.LogInformation("Nouveau compte parent créé via {Provider} : {Email}.", info.LoginProvider, email);

            // LA MÊME ALERTE QUE PAR LE FORMULAIRE, ET IL FALLAIT Y PENSER.
            //
            // Une inscription par Google est une inscription. Ne la signaler
            // que sur le chemin du formulaire aurait rendu le compteur faux
            // sans que rien ne le dise — on aurait cru l'acquisition en baisse
            // alors qu'elle serait passée par l'autre porte.
            await Helpers.TelegramHelper.NotifierNouveauParentAsync(
                _configuration, _logger, user.Prenom, user.Nom, user.Email);

            return RedirectToLocalOrHome(returnUrl);
        }

        // ------------------------------------------------------------------
        // Déconnexion depuis les pages du serveur d'identité
        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        /// <summary>Noms des fournisseurs externes réellement enregistrés.</summary>
        private async Task<IList<string>> FournisseursExternesAsync()
        {
            var schemas = await _signInManager.GetExternalAuthenticationSchemesAsync();
            return schemas.Select(s => s.Name).ToList();
        }

        private IActionResult RedirectToLocalOrHome(string? returnUrl)
        {
            // Ne jamais rediriger vers une URL absolue fournie en query string :
            // c'est la faille open-redirect classique. Seul un chemin local passe.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Pas de returnUrl : l'utilisateur est arrivé directement sur
            // /Account/Register ou /Account/Login. Il est maintenant connecté,
            // donc on le renvoie vers l'application, pas vers la page de
            // connexion qu'il vient de quitter.
            //
            // L'URL vient de la configuration, jamais de la requête : une valeur
            // fournie par l'utilisateur rouvrirait la faille qu'on ferme au-dessus.
            var urlApplication = _configuration["Clients:Spa:Url"];
            return string.IsNullOrWhiteSpace(urlApplication)
                ? RedirectToAction(nameof(Login))
                : Redirect(urlApplication);
        }

        private static string TraduireErreurIdentity(IdentityError error) => error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Un compte existe déjà avec cette adresse email.",
            "PasswordTooShort" => "Le mot de passe doit contenir au moins 10 caractères.",
            "PasswordRequiresDigit" => "Le mot de passe doit contenir au moins un chiffre.",
            "PasswordRequiresUpper" => "Le mot de passe doit contenir au moins une majuscule.",
            "InvalidEmail" => "Adresse email invalide.",
            _ => error.Description
        };
    }
}
