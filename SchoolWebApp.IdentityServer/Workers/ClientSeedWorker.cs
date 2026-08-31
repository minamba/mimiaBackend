using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SchoolWebApp.IdentityServer.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SchoolWebApp.IdentityServer.Workers
{
    /// <summary>
    /// Enregistre au démarrage le client SPA React et le scope "api".
    ///
    /// Idempotent, mais pas passif : le client SPA est RÉALIGNÉ sur la
    /// configuration à chaque démarrage, de sorte qu'un changement de domaine
    /// suffise à faire fonctionner la connexion sans toucher à la base.
    /// </summary>
    public class ClientSeedWorker : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClientSeedWorker> _logger;

        public ClientSeedWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ClientSeedWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var frontUrl = _configuration["Clients:Spa:Url"] ?? "http://localhost:3000";

            var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            // Le descriptif est construit une fois puis appliqué, que le client
            // existe déjà ou non.
            //
            // POURQUOI METTRE À JOUR ET NON SEULEMENT CRÉER
            // ---------------------------------------------
            // Les URI de redirection sont dérivées de `Clients:Spa:Url`. La
            // version précédente ne semait qu'en l'absence du client : déployer
            // sur un nouveau domaine sans repartir d'une base vierge laissait
            // les anciennes URI en place, et OpenIddict refusait la connexion
            // avec « invalid redirect_uri » — une erreur qui ne nomme jamais la
            // cause. Réappliquer à chaque démarrage rend la configuration
            // souveraine : l'URL du fichier de configuration est celle qui vaut.
            var descriptif = new OpenIddictApplicationDescriptor
            {
                ClientId = "school-ia-spa",
                DisplayName = "Mimia — application React",

                // Client public : le code s'exécute dans le navigateur,
                // aucun secret ne peut y être gardé. PKCE protège l'échange.
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,

                RedirectUris =
                {
                    new Uri($"{frontUrl}/callback"),
                    new Uri($"{frontUrl}/silent-renew")
                },
                PostLogoutRedirectUris =
                {
                    new Uri($"{frontUrl}/")
                },

                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,

                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,

                    Permissions.ResponseTypes.Code,

                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api"
                },

                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            };

            var clientExistant = await applicationManager.FindByClientIdAsync("school-ia-spa", cancellationToken);

            if (clientExistant is null)
            {
                await applicationManager.CreateAsync(descriptif, cancellationToken);
                _logger.LogInformation("Client OpenIddict 'school-ia-spa' créé (redirect : {FrontUrl}/callback).", frontUrl);
            }
            else
            {
                await applicationManager.UpdateAsync(clientExistant, descriptif, cancellationToken);
                _logger.LogInformation("Client OpenIddict 'school-ia-spa' réaligné (redirect : {FrontUrl}/callback).", frontUrl);
            }

            var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            if (await scopeManager.FindByNameAsync("api", cancellationToken) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "api",
                    DisplayName = "API Mimia",
                    Description = "Accès à l'API pédagogique (élèves, compétences, conversations).",
                    Resources = { "school-ia-api" }
                }, cancellationToken);

                _logger.LogInformation("Scope OpenIddict 'api' créé.");
            }

            await SeedAdministrateursAsync(scope, cancellationToken);
        }

        /// <summary>
        /// Crée le rôle Admin et l'attribue aux comptes listés dans
        /// `Admin:Emails`. Passer par la configuration plutôt que par une case
        /// à cocher en base évite qu'un administrateur puisse s'auto-promouvoir
        /// ou promouvoir quelqu'un d'autre depuis l'application.
        /// </summary>
        private async Task SeedAdministrateursAsync(IServiceScope scope, CancellationToken ct)
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                _logger.LogInformation("Rôle Admin créé.");
            }

            await SeedCompteInitialAsync(userManager);
            await SeedCompteTestAsync(userManager);

            var emails = _configuration.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>();

            foreach (var email in emails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                var utilisateur = await userManager.FindByEmailAsync(email.Trim());

                // Le compte n'existe pas encore : il sera promu au prochain
                // démarrage, une fois inscrit. Rien à signaler.
                if (utilisateur is null) continue;

                if (!await userManager.IsInRoleAsync(utilisateur, "Admin"))
                {
                    await userManager.AddToRoleAsync(utilisateur, "Admin");
                    _logger.LogInformation("{Email} promu administrateur.", email);
                }
            }
        }

        /// <summary>
        /// Crée le compte d'exploitation au premier démarrage, pour qu'il existe
        /// un administrateur sans passer par le formulaire d'inscription.
        /// Idempotent : si le compte existe, on ne touche pas à son mot de passe.
        /// </summary>
        /// <summary>
        /// Le compte de démonstration, créé une fois pour toutes.
        ///
        /// Distinct du compte initial d'administration : celui-ci n'est pas
        /// administrateur, il sert à voir le produit avec les yeux d'un parent
        /// ordinaire. Il s'éteint et se rallume depuis l'onglet Modes, sans
        /// jamais être supprimé — c'est ce qui lui permet de garder son
        /// historique, ses fiches et ses évaluations d'une démonstration à
        /// l'autre.
        ///
        /// Idempotent : si le compte existe, on ne touche à rien, pas même à
        /// son mot de passe.
        /// </summary>
        private async Task SeedCompteTestAsync(UserManager<ApplicationUser> userManager)
        {
            var email = _configuration["Admin:CompteTest:Email"];
            var motDePasse = _configuration["Admin:CompteTest:MotDePasse"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(motDePasse)) return;
            if (await userManager.FindByEmailAsync(email) is not null) return;

            var utilisateur = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Prenom = _configuration["Admin:CompteTest:Prenom"] ?? "Compte",
                Nom = _configuration["Admin:CompteTest:Nom"] ?? "Démonstration",
            };

            // Même contournement que pour le compte initial : le mot de passe
            // est choisi par l'exploitant, pas par un utilisateur final, et le
            // faire échouer en silence au démarrage serait pire.
            var creation = await userManager.CreateAsync(utilisateur);
            if (!creation.Succeeded)
            {
                _logger.LogError(
                    "Creation du compte de demonstration {Email} impossible : {Erreurs}",
                    email, string.Join(" ", creation.Errors.Select(e => e.Description)));
                return;
            }

            utilisateur.PasswordHash = userManager.PasswordHasher.HashPassword(utilisateur, motDePasse);
            await userManager.UpdateAsync(utilisateur);

            _logger.LogWarning("Compte de demonstration cree : {Email}.", email);
        }

        private async Task SeedCompteInitialAsync(UserManager<ApplicationUser> userManager)
        {
            var email = _configuration["Admin:CompteInitial:Email"];
            var motDePasse = _configuration["Admin:CompteInitial:MotDePasse"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(motDePasse))
            {
                return;
            }

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                return;
            }

            var utilisateur = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Prenom = _configuration["Admin:CompteInitial:Prenom"],
                Nom = _configuration["Admin:CompteInitial:Nom"]
            };

            // Création SANS mot de passe, puis pose directe du hash.
            //
            // CreateAsync(user, password) applique la politique de mots de passe,
            // qui exige une majuscule. Le mot de passe d'exploitation est choisi
            // par l'administrateur, pas par un utilisateur final : le faire
            // échouer silencieusement au démarrage serait pire que de contourner
            // le validateur ici. Les comptes créés par le formulaire, eux,
            // restent soumis à la politique complète.
            var creation = await userManager.CreateAsync(utilisateur);
            if (!creation.Succeeded)
            {
                _logger.LogError(
                    "Creation du compte initial {Email} impossible : {Erreurs}",
                    email, string.Join(" ", creation.Errors.Select(e => e.Description)));
                return;
            }

            utilisateur.PasswordHash = userManager.PasswordHasher.HashPassword(utilisateur, motDePasse);
            await userManager.UpdateAsync(utilisateur);

            await userManager.AddToRoleAsync(utilisateur, "Admin");

            _logger.LogWarning(
                "Compte administrateur initial cree : {Email}. Changez son mot de passe avant toute mise en production.",
                email);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
