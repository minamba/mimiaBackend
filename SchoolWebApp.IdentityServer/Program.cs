using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.IdentityServer.Data;
using SchoolWebApp.IdentityServer.Services;
using SchoolWebApp.IdentityServer.Workers;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// MVC + vues Razor (pages de connexion / inscription)
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------------------
// Base de données d'identité
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // Déclare les entités OpenIddict (applications, autorisations, scopes, jetons).
    options.UseOpenIddict();
});

// ---------------------------------------------------------------------------
// ASP.NET Identity
// ---------------------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;

        options.User.RequireUniqueEmail = true;

        // Comptes parents : verrouillage après 5 tentatives, 15 minutes.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // L'ADRESSE DOIT ÊTRE CONFIRMÉE AVANT LA PREMIÈRE CONNEXION.
        //
        // C'était à `false` « tant que le service SMTP n'est pas branché » — il
        // l'est depuis. Deux raisons de le refermer :
        //
        // La faute de frappe. Un parent qui écrit `gmial.com` ne recevra jamais
        // ses bilans, ni son lien de réinitialisation. Il ne le découvrira pas :
        // rien n'échoue de son côté, le courrier part simplement dans le vide.
        // La confirmation transforme cette panne silencieuse en un message
        // immédiat — « vérifiez votre boîte » — au moment où il peut encore
        // corriger.
        //
        // L'adresse d'autrui. Sans vérification, on s'inscrit avec l'adresse de
        // n'importe qui, qui recevra ensuite des bilans sur des enfants qui ne
        // sont pas les siens.
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// LE JETON DE RÉINITIALISATION VIT DEUX HEURES, COMME LE COURRIEL L'ANNONCE.
//
// Le message de réinitialisation promet « un lien valable deux heures ». La
// durée n'était configurée nulle part : celle par défaut d'ASP.NET Identity est
// d'UN JOUR. Le courriel disait donc faux, et un lien qu'on croit périmé
// ouvrait encore le compte vingt-deux heures durant — dans une boîte mail qui,
// elle, ne se referme pas toute seule.
//
// Aligner le code sur la promesse plutôt que l'inverse : un lien de
// réinitialisation est une clé du compte, et deux heures suffisent largement à
// s'en servir.
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(2);
});

// OpenIddict a besoin que les claims Identity portent les noms standard OIDC.
builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserNameClaimType = Claims.Name;
    options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
    options.ClaimsIdentity.RoleClaimType = Claims.Role;
    options.ClaimsIdentity.EmailClaimType = Claims.Email;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// ---------------------------------------------------------------------------
// Connexion Google
// Les identifiants viennent de user-secrets en dev, des variables
// d'environnement en production. Jamais d'appsettings.json versionné.
// ---------------------------------------------------------------------------
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/signin-google";

            options.Scope.Add("email");
            options.Scope.Add("profile");

            // given_name / family_name sont déjà mappés par défaut vers
            // ClaimTypes.GivenName et ClaimTypes.Surname par le handler Google.

            options.SaveTokens = true;
        });
}

// ---------------------------------------------------------------------------
// OpenIddict
// ---------------------------------------------------------------------------
builder.Services.AddOpenIddict()

    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })

    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetUserInfoEndpointUris("connect/userinfo")
               .SetEndSessionEndpointUris("connect/logout");

        // SPA React : Authorization Code + PKCE, pas de client secret.
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Email,
            Scopes.Profile,
            Scopes.Roles,
            Scopes.OfflineAccess,
            "api");

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        // ------------------------------------------------------------------
        // LES CLÉS QUI SIGNENT LES JETONS.
        //
        // POURQUOI LES CERTIFICATS DE DÉVELOPPEMENT NE PEUVENT PAS RESTER
        // ---------------------------------------------------------------
        // `AddDevelopment…Certificate` fabrique un certificat au démarrage et
        // le range dans le profil de l'utilisateur courant. Dans un conteneur,
        // ce profil disparaît à chaque reconstruction : la clé change, et TOUS
        // les jetons signés avec l'ancienne deviennent invalides d'un coup.
        // Concrètement, à chaque déploiement, tous les parents sont déconnectés
        // et les enfants perdent leur session en plein cours.
        //
        // EN PRODUCTION on charge donc un certificat PERSISTANT, monté avec le
        // reste de la configuration. Il n'a pas besoin d'être signé par une
        // autorité : personne d'extérieur ne le valide, c'est ce serveur qui
        // signe et c'est lui qui vérifie. Un auto-signé de longue durée suffit.
        //
        // Et s'il manque, on REFUSE DE DÉMARRER plutôt que de retomber sur les
        // certificats jetables : le service fonctionnerait en apparence, et
        // déconnecterait tout le monde au déploiement suivant sans que rien ne
        // l'explique.
        // ------------------------------------------------------------------
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            var chiffrement = builder.Configuration["Certificats:Chiffrement"];
            var signature = builder.Configuration["Certificats:Signature"];
            var motDePasse = builder.Configuration["Certificats:MotDePasse"]
                             ?? Environment.GetEnvironmentVariable("MIMIA_CERT_MOTDEPASSE");

            static X509Certificate2 Charger(string? chemin, string? motDePasse, string usage)
            {
                // UN CHEMIN RELATIF PART DU DOSSIER DE L'APPLICATION.
                //
                // Pas du répertoire courant : celui-ci dépend de la façon dont
                // le programme a été lancé — service Windows, unité systemd,
                // conteneur, ou double-clic. Le même fichier de configuration
                // trouverait le certificat ici et pas là, sans qu'on comprenne
                // pourquoi.
                //
                // Résultat : « certificats/signature.pfx » désigne toujours un
                // dossier posé À CÔTÉ des DLL, que l'application soit publiée
                // sur un serveur ou montée dans un conteneur. Un chemin absolu
                // reste possible et prime.
                if (!string.IsNullOrWhiteSpace(chemin) && !Path.IsPathRooted(chemin))
                {
                    chemin = Path.Combine(AppContext.BaseDirectory, chemin);
                }

                if (string.IsNullOrWhiteSpace(chemin) || !File.Exists(chemin))
                {
                    throw new InvalidOperationException(
                        $"Certificat de {usage} introuvable : « {chemin ?? "(non configuré)"} ». "
                        + "En production, les jetons doivent être signés par un certificat "
                        + "PERSISTANT — sinon chaque redéploiement déconnecte tous les comptes. "
                        + "Générez-en un (openssl req -x509 -newkey rsa:2048 -keyout cle.pem "
                        + "-out cert.pem -days 3650 -nodes puis openssl pkcs12 -export -out "
                        + $"{usage}.pfx -inkey cle.pem -in cert.pem), montez-le dans le "
                        + "conteneur, et renseignez Certificats:Chiffrement / Certificats:Signature.");
                }

                // PersistKeySet : sans lui, la clé privée n'est pas conservée
                // après le chargement sur certaines plateformes, et la
                // signature échoue à la première requête.
                return new X509Certificate2(
                    chemin,
                    motDePasse,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            }

            options.AddEncryptionCertificate(Charger(chiffrement, motDePasse, "chiffrement"))
                   .AddSigningCertificate(Charger(signature, motDePasse, "signature"));
        }

        // L'API valide les access tokens en JWT : ils ne doivent pas être chiffrés.
        options.DisableAccessTokenEncryption();

        var aspNetCoreBuilder = options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough()
               .EnableStatusCodePagesIntegration();

        // OpenIddict refuse HTTP par défaut. On ne lève cette exigence qu'en
        // développement, pour pouvoir travailler sur le profil http (port 5067)
        // sans certificat. En production, HTTPS reste obligatoire.
        if (builder.Environment.IsDevelopment())
        {
            aspNetCoreBuilder.DisableTransportSecurityRequirement();
        }
    })

    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Seed du client SPA et des scopes au démarrage.
// ---------------------------------------------------------------------------
// Mails transactionnels : bienvenue et réinitialisation de mot de passe.
// Le mot de passe SMTP n'est jamais dans appsettings.json (versionné) : il
// vient d'appsettings.Development.json en local, d'une variable d'environnement
// en production.
// ---------------------------------------------------------------------------
builder.Services.Configure<OptionsEmail>(options =>
{
    builder.Configuration.GetSection(OptionsEmail.Section).Bind(options);
    options.SenderPassword = string.IsNullOrWhiteSpace(options.SenderPassword)
        ? Environment.GetEnvironmentVariable("SMTP_PASSWORD")
        : options.SenderPassword;
    options.UrlSite = builder.Configuration["Clients:Spa:Url"] ?? options.UrlSite;
});

builder.Services.AddScoped<IServiceEmail, ServiceEmail>();

// Singleton : il porte son propre cache de quinze secondes, un par requete le
// viderait a chaque affichage de page.
builder.Services.AddSingleton<IModeTestService, ModeTestService>();

builder.Services.AddHostedService<ClientSeedWorker>();

// Les comptes nés avant l'obligation de confirmation ne doivent pas se
// retrouver enfermés dehors par un changement de règle qu'ils n'ont pas
// demandé. Voir la note du worker : la reprise est bornée dans le temps.
builder.Services.AddHostedService<ConfirmationRetroactiveWorker>();

// ---------------------------------------------------------------------------
// CORS — le front React appelle /connect/token en cross-origin
//
// Les origines viennent de la configuration, jamais du code : le serveur
// d'identité vit forcément sur un autre domaine que le site (auth.mimia.fr
// contre mimia.fr), donc cette liste est le seul point qui autorise le front à
// échanger son code contre un jeton. Codée en dur sur localhost, elle bloquait
// silencieusement toute la connexion en production — le navigateur refuse la
// réponse et n'explique rien d'utile dans la console.
//
// `OriginesSupplementaires` existe pour les cas où le site répond sur plusieurs
// noms (avec et sans www, une préproduction), sans avoir à recompiler.
// ---------------------------------------------------------------------------
var originesAutorisees = new[] { builder.Configuration["Clients:Spa:Url"] ?? "http://localhost:3000" }
    .Concat(builder.Configuration.GetSection("Clients:Spa:OriginesSupplementaires").Get<string[]>() ?? [])
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Select(o => o!.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
    options.AddPolicy("AllowFront", policy =>
        policy.WithOrigins(originesAutorisees)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

var app = builder.Build();

// Garde-fou du courrier, AVANT que la moindre requête soit servie.
//
// Ce serveur envoie la bienvenue, la réinitialisation de mot de passe et la
// confirmation de suppression de compte. `RedirectionDev` détournerait tout
// vers une boîte de test, sans erreur nulle part : un parent qui a oublié son
// mot de passe ne recevrait jamais son lien, et personne ne le saurait.
//
// Le même contrôle vit dans l'API. Les deux applications lisent la même
// configuration mais démarrent séparément : une garde posée d'un seul côté
// laisserait l'autre partir avec le courrier détourné.
app.Services.GetRequiredService<IOptions<OptionsEmail>>().Value.Verifier(
    app.Environment.IsDevelopment(),
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Courrier"));

// ---------------------------------------------------------------------------
// Migration automatique uniquement si la base n'existe pas encore
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            if (!db.Database.CanConnect())
            {
                db.Database.Migrate();
                Console.WriteLine("Base d'identite creee avec succes.");
            }
            else if (!app.Environment.IsDevelopment())
            {
                // MÊME RÈGLE QUE L'API : en production, les migrations en
                // attente sont appliquées au démarrage.
                //
                // « Migrer seulement si la base n'existe pas » ne migrait
                // jamais la production, puisqu'elle existe. Le défaut ne s'est
                // pas encore manifesté ici — aucune migration d'identité n'a
                // suivi la mise en ligne — mais il attendait son tour, et il se
                // serait manifesté de la même façon : un déploiement réussi,
                // un service en panne.
                var enAttente = db.Database.GetPendingMigrations().ToList();

                if (enAttente.Count > 0)
                {
                    Console.WriteLine(
                        $"{enAttente.Count} migration(s) d'identite en attente : "
                        + string.Join(", ", enAttente));

                    db.Database.Migrate();
                    Console.WriteLine("Migrations d'identite appliquees.");
                }
            }
            break;
        }
        catch (Exception)
        {
            retries--;
            if (retries == 0) break;
            Console.WriteLine($"SQL Server pas encore pret, nouvelle tentative dans 5s... ({retries} restants)");
            Thread.Sleep(5000);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Account/Error");
    app.UseHsts();
}

// Pas de redirection HTTPS en développement.
//
// Visual Studio démarre avec le profil https, qui expose http (5067) ET
// https (7185). La redirection s'active alors et renvoie le front de
// http://localhost:5067 vers https://localhost:7185 — que le navigateur
// rejette tant que le certificat de développement n'est pas approuvé.
// Le front et la connexion Google sont configurés sur le port 5067 en clair :
// on laisse donc HTTP passer en local. En production, HTTPS reste imposé.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFront");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
