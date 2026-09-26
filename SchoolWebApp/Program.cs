using Anthropic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Builders.impl;
using SchoolWebApp.Api.Mapper;
using SchoolWebApp.Api.Services;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Api.Services.Voix;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Api.Workers;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Dal.Seed;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using SchoolWebApp.Domain.Services.impl;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// CORS
//
// En production le site est servi par cette application elle-même (wwwroot),
// donc le front et l'API partagent une origine et CORS ne joue aucun rôle. Il
// reste indispensable en développement, où React tourne sur son propre serveur
// au port 3000. Les origines viennent de la configuration : codées en dur, elles
// se seraient tues au premier changement de domaine.
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

// ---------------------------------------------------------------------------
// Controllers + Swagger
// ---------------------------------------------------------------------------
// Le filtre confine les sessions enfants : posé GLOBALEMENT, il ferme par
// défaut toute route qui ne porte pas explicitement `[AutoriseEleve]`.
builder.Services.AddHttpClient();

// Les trois alertes Telegram de l'exploitant : inscription, abonnement,
// résiliation. Singleton : il ne porte que trois chaînes de configuration et
// emprunte ses connexions à la fabrique.
builder.Services.AddSingleton<SchoolWebApp.Api.Services.Notifications.ITelegramService,
                              SchoolWebApp.Api.Services.Notifications.TelegramService>();

// La diffusion d un message a tous les parents. Singleton : c est lui qui
// porte le verrou « une seule diffusion a la fois », et deux instances ne se
// verraient pas.
builder.Services.AddSingleton<SchoolWebApp.Api.Services.Notifications.IDiffusionService,
                              SchoolWebApp.Api.Services.Notifications.DiffusionService>();

builder.Services.AddControllers(options =>
    options.Filters.Add<SchoolWebApp.Api.Auth.RestrictionEleveFilter>());
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SchoolWebApp API", Version = "v1" });
    c.EnableAnnotations();

    // Permet de coller un access token dans Swagger pour tester les endpoints protégés.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Collez ici l'access token émis par SchoolWebApp.IdentityServer."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------------------------------------------------------------------
// Base de données métier
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<SchoolWebAppDatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------------------
// AutoMapper — syntaxe obligatoire en 16.x
// ---------------------------------------------------------------------------
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MapperProfile>());

// ---------------------------------------------------------------------------
// Validation des access tokens émis par SchoolWebApp.IdentityServer
//
// Les jetons ne sont pas chiffrés (DisableAccessTokenEncryption côté serveur),
// donc l'API peut les valider localement après avoir récupéré les clés
// publiques via le document de découverte OIDC.
// ---------------------------------------------------------------------------
var identityServerUrl = builder.Configuration["Authentication:IdentityServer:Issuer"]
                        ?? "https://localhost:7185/";

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(identityServerUrl);
        options.AddAudiences("school-ia-api");

        // Récupère la configuration et le JWKS du serveur d'identité en HTTP.
        options.UseSystemNetHttp();

        options.UseAspNetCore();
    });

// DEUX VOIES D'ENTRÉE, ESSAYÉES DANS CET ORDRE.
//
// Le parent porte un JWT d'OpenIddict ; l'enfant porte un jeton de session,
// annoncé par `Authorization: Eleve <jeton>`. Le second schéma se retire sans
// se prononcer quand l'en-tête n'est pas le sien, ce qui laisse le premier
// faire son travail — les deux cohabitent sans se gêner.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "MimiaParentOuEleve";
})
    .AddPolicyScheme("MimiaParentOuEleve", "Parent ou élève", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString()
                .StartsWith("Eleve ", StringComparison.Ordinal)
                ? SchoolWebApp.Api.Auth.AuthentificationEleve.Schema
                : OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults
                    .AuthenticationScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, SchoolWebApp.Api.Auth.AuthentificationEleve>(
        SchoolWebApp.Api.Auth.AuthentificationEleve.Schema, _ => { });

builder.Services.AddAuthorization(options =>
{
    // On teste le claim `role` directement plutôt que RequireRole() : le type
    // de claim de rôle d'un jeton OpenIddict est "role", pas le ClaimTypes.Role
    // de .NET, et IsInRole() renverrait faux sans remappage.
    options.AddPolicy("EstAdmin", policy =>
        policy.RequireAssertion(contexte =>
            contexte.User.HasClaim("role", "Admin") ||
            contexte.User.IsInRole("Admin")));

    // -----------------------------------------------------------------------
    // LE SUPER-ADMINISTRATEUR : CE QUI NE SE DÉLÈGUE PAS
    // -----------------------------------------------------------------------
    // Deux choses lui restent : les Modes — mode test, maintenance, catalogue
    // Stripe, qui touchent le site entier — et l'attribution du droit
    // d'administrer lui-même.
    //
    // POURQUOI CELLE-LÀ SURTOUT. Un administrateur qui pourrait promouvoir
    // aurait le pouvoir de se donner un successeur, puis de se faire retirer
    // sans perdre la main. Le droit de distribuer les droits est le seul qui
    // ne doit jamais se distribuer.
    //
    // Son rôle vient de la configuration « Admin:Emails », jamais de la base :
    // aucune manœuvre depuis l'interface ne peut le lui retirer, et donc
    // aucune ne peut fermer la maison.
    options.AddPolicy("EstSuperAdmin", policy =>
        policy.RequireAssertion(contexte =>
            contexte.User.HasClaim("role", "SuperAdmin") ||
            contexte.User.IsInRole("SuperAdmin")));

    // -----------------------------------------------------------------------
    // L'AGENT QUI COLLECTE LES PLANCHES
    // -----------------------------------------------------------------------
    // Un administrateur connecté, OU un secret partagé en en-tête.
    //
    // POURQUOI PAS UN JETON OAUTH
    // --------------------------
    // Le serveur d'identité n'a qu'un client public en authorization_code : il
    // suppose un navigateur et un humain qui se connecte. Un automate n'a ni
    // l'un ni l'autre. Lui ouvrir un flux client_credentials voudrait dire
    // fabriquer un rôle Admin sans utilisateur derrière — une identité
    // d'administrateur qui n'appartient à personne, pour importer des images.
    //
    // Un secret dédié, portant EXACTEMENT ce droit-là, est plus honnête.
    //
    // CE QUE CE SECRET N'OUVRE PAS
    // ---------------------------
    // Lister et importer des planches. Rien d'autre. La suppression reste
    // réservée à un administrateur connecté : un agent qui déraille doit
    // pouvoir ajouter du mauvais, jamais effacer du bon.
    //
    // Sans `Planches:CleAgent` en configuration, l'en-tête ne vaut RIEN : pas
    // de clé par défaut, donc pas de porte ouverte sur une installation où
    // personne n'a pris la décision de l'ouvrir.
    options.AddPolicy("PeutImporterPlanches", policy =>
        policy.RequireAssertion(contexte =>
        {
            if (contexte.User.HasClaim("role", "Admin") ||
                contexte.User.IsInRole("Admin"))
            {
                return true;
            }

            var attendue = builder.Configuration["Planches:CleAgent"];
            if (string.IsNullOrWhiteSpace(attendue)) return false;

            if (contexte.Resource is not HttpContext http) return false;
            if (!http.Request.Headers.TryGetValue("X-Import-Key", out var fournie)) return false;

            // Comparaison à temps constant : un `==` sur une chaîne s'arrête au
            // premier caractère différent, et ce temps de réponse se mesure.
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(fournie.ToString()),
                System.Text.Encoding.UTF8.GetBytes(attendue));
        }));
});

// ---------------------------------------------------------------------------
// Injections
// ---------------------------------------------------------------------------
/*
 * Compression des réponses.
 *
 * Le JSON de l'API est très répétitif — les mêmes noms de champs sur chaque
 * ligne, le libellé de la matière et le prénom du professeur recopiés à chaque
 * fiche. C'est exactement ce qu'un compresseur avale : mesuré sur la liste de
 * trois cents fiches de révision, 167 Ko tombent sous les 25.
 *
 * Sur HTTPS et pour des réponses issues de données authentifiées, la
 * compression a été associée à des attaques par recoupement (BREACH). Le risque
 * suppose que l'attaquant puisse injecter du texte choisi DANS la réponse et en
 * observer la taille — ce qui n'est pas le cas ici. On l'active donc aussi en
 * HTTPS, faute de quoi elle ne servirait jamais en production.
 *
 * Le flux SSE du chat en est EXCLU : un compresseur met le texte en tampon, ce
 * qui réintroduirait exactement l'attente qu'on a passé des jours à supprimer.
 */
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();

    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "image/svg+xml"]);

    // Ni l'audio ni le flux d'événements : le premier est déjà compressé, le
    // second doit partir octet par octet.
    options.ExcludedMimeTypes = ["text/event-stream", "audio/pcm", "audio/mpeg", "audio/wav"];
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

builder.Services.AddScoped<IParentRepository, ParentRepository>();
builder.Services.AddScoped<IEleveRepository, EleveRepository>();
builder.Services.AddScoped<IReferentielRepository, ReferentielRepository>();

builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<IEleveService, EleveService>();
builder.Services.AddScoped<IReferentielService, ReferentielService>();
builder.Services.AddScoped<ISignalementService, SignalementService>();

builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMaitriseRepository, MaitriseRepository>();
builder.Services.AddScoped<IEcheanceReferentielRepository, EcheanceReferentielRepository>();
builder.Services.AddScoped<IProgrammeScolaireRepository, ProgrammeScolaireRepository>();
builder.Services.AddScoped<IEvaluationRepository, EvaluationRepository>();
builder.Services.AddScoped<IRapportRepository, RapportRepository>();
builder.Services.AddScoped<IFicheRepository, FicheRepository>();
builder.Services.AddScoped<IDicteeRepository, DicteeRepository>();
// L ARCHIVE AUDIO EST UN SINGLETON : elle ne porte qu une racine de chemin,
// relue a chaque appel depuis le disque. Rien a etendre par requete.
builder.Services.AddSingleton<SchoolWebApp.Domain.Services.IArchiveAudio,
    SchoolWebApp.Dal.Stockage.ArchiveAudioDisque>();

builder.Services.AddScoped<IComprehensionOraleRepository, ComprehensionOraleRepository>();
builder.Services.AddScoped<IExpressionOraleRepository, ExpressionOraleRepository>();
builder.Services.AddScoped<IExpressionEcriteRepository, ExpressionEcriteRepository>();
builder.Services.AddScoped<IEvaluationPrevueRepository, EvaluationPrevueRepository>();
builder.Services.AddScoped<IControleScolaireRepository, ControleScolaireRepository>();
builder.Services.AddScoped<IExamenRepository, ExamenRepository>();
builder.Services.AddScoped<ISignalementRepository, SignalementRepository>();
builder.Services.AddScoped<IPlancheRepository, PlancheRepository>();
builder.Services.AddScoped<ISessionEleveRepository, SessionEleveRepository>();

// AddMemoryCache est idempotent (TryAdd) : il est appelé ici parce que le
// service des planches en dépend et que rien d'autre ne garantit sa présence.
builder.Services.AddMemoryCache();

// Les QR codes du scanner de séance : dix minutes de vie, en mémoire. Singleton
// par nécessité — le jeton créé par l'ordinateur doit être retrouvé par la
// requête du téléphone. Voir JetonsScanMobile pour la limite d'instance unique.
builder.Services.AddSingleton<SchoolWebApp.Api.Services.ScanMobile.JetonsScanMobile>();

// L'identification d'une planche par son empreinte, auprès de Wikimedia.
// Un client nommé plutôt qu'un `new HttpClient()` : c'est lui qui porte l'agent
// utilisateur que Wikimedia exige, et il refuse les anonymes.
builder.Services.AddHttpClient<IOrigineCommonsService, OrigineCommonsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mimia-Planches/1.0 (https://mimia.fr; contact@mimia.fr)");
});
builder.Services.AddScoped<IBibliothequePlanchesService, BibliothequePlanchesService>();
builder.Services.AddScoped<IReglageRepository, ReglageRepository>();
builder.Services.AddScoped<IMesureVoixRepository, MesureVoixRepository>();
builder.Services.AddScoped<ICatalogueStripeService, CatalogueStripeService>();
builder.Services.AddScoped<ICaisseStripeService, CaisseStripeService>();
builder.Services.AddScoped<IEvenementsStripeService, EvenementsStripeService>();
// Le sel des empreintes d'adresses passe par la configuration : il ne doit
// jamais vivre dans la base qu'il protège. Absent, la garde de l'essai
// fonctionne toujours — voir la note sur `EssaiConsomme`.
builder.Services.AddScoped<IAbonnementRepository>(fournisseur =>
    new AbonnementRepository(
        fournisseur.GetRequiredService<SchoolWebAppDatabaseContext>(),
        builder.Configuration["Essai:Sel"]
            ?? Environment.GetEnvironmentVariable("MIMIA_SEL_ESSAI"),
        fournisseur.GetRequiredService<SchoolWebApp.Domain.Services.IComptesProteges>()));
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

// Le journal des appels de fond : sans lui, seule la moitié de la facture
// Claude est visible — le dialogue — et les workers dépensent en silence.
// Singleton : il ouvre sa propre portée à chaque écriture, ce qui le rend
// appelable depuis les workers comme depuis les services de requête.
builder.Services.AddSingleton<IJournalClaudeRepository, JournalClaudeRepository>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMaitriseService, MaitriseService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAvisRepository, AvisRepository>();
builder.Services.AddScoped<IPromoRepository, PromoRepository>();
builder.Services.AddScoped<IIdeeRepository, IdeeRepository>();
builder.Services.AddScoped<IModeleMailRepository, ModeleMailRepository>();
// Sans état, et appuyé sur le service de diffusion qui est lui-même un
// singleton : un template se compose comme un message tapé à la main.
builder.Services.AddSingleton<SchoolWebApp.Api.Services.Notifications.IComposeurModeleMail,
    SchoolWebApp.Api.Services.Notifications.ComposeurModeleMail>();

// Les courriels automatiques programmés, le journal de leurs envois et les
// liens de désabonnement. Le jeton est un singleton : sa clé est lue une fois,
// et une clé jetable de développement doit rester la même pour toute la
// session, sans quoi les liens envoyés le matin ne marcheraient plus l'après-midi.
builder.Services.AddScoped<IEnvoiAutomatiqueRepository, EnvoiAutomatiqueRepository>();
builder.Services.AddSingleton<SchoolWebApp.Api.Services.Notifications.IJetonDesabonnement,
    SchoolWebApp.Api.Services.Notifications.JetonDesabonnement>();
builder.Services.AddScoped<SchoolWebApp.Api.Services.Notifications.IEnvoisAutomatiquesService,
    SchoolWebApp.Api.Services.Notifications.EnvoisAutomatiquesService>();
builder.Services.AddHostedService<SchoolWebApp.Api.Workers.EnvoisAutomatiquesWorker>();
builder.Services.AddScoped<IBannissementRepository, BannissementRepository>();
builder.Services.AddScoped<IVerrouBannissement, VerrouBannissement>();
builder.Services.AddScoped<IOffreLancementService, OffreLancementService>();
builder.Services.AddSingleton<SchoolWebApp.Api.Services.ComptesProteges>();
builder.Services.AddSingleton<SchoolWebApp.Domain.Services.IComptesProteges>(
    fournisseur => fournisseur.GetRequiredService<SchoolWebApp.Api.Services.ComptesProteges>());

builder.Services.AddScoped<IEleveViewModelBuilder, EleveViewModelBuilder>();
builder.Services.AddScoped<IReferentielViewModelBuilder, ReferentielViewModelBuilder>();
builder.Services.AddScoped<IChatViewModelBuilder, ChatViewModelBuilder>();
builder.Services.AddScoped<IChatContexteResolver, ChatContexteResolver>();

// ---------------------------------------------------------------------------
// Claude — la clé vient de ANTHROPIC_API_KEY (user-secrets en dev,
// variable d'environnement en production). Jamais d'appsettings versionné.
// Le client est thread-safe et sans état : singleton.
// ---------------------------------------------------------------------------
builder.Services.Configure<OptionsClaude>(builder.Configuration.GetSection(OptionsClaude.Section));

// La clé passe par IConfiguration, donc elle accepte n'importe quelle source :
// user-secrets en dev, variable d'environnement sur le VPS, secret Docker,
// coffre cloud. On retombe sur ANTHROPIC_API_KEY, que le SDK lit nativement.
//
// `user-secrets` seul ne suffirait pas : ASP.NET ne charge ce fournisseur
// qu'en Development, il est ignoré en production.
builder.Services.AddSingleton(_ =>
{
    var cle = builder.Configuration["Claude:ApiKey"]
              ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    return string.IsNullOrWhiteSpace(cle)
        ? new AnthropicClient()
        : new AnthropicClient { ApiKey = cle };
});

builder.Services.AddScoped<IAgentPedagogiqueService, AgentPedagogiqueService>();

// LES JEUX QUE LE PROFESSEUR PEUT PROPOSER — Camara, le 23/09/2026. Le
// catalogue est lu une fois depuis la ressource embarquée (singleton) ; le
// service qui filtre pour un élève lit les réglages et la maîtrise (scoped).
builder.Services.AddSingleton<SchoolWebApp.Api.Services.Jeux.CatalogueJeux>();
builder.Services.AddScoped<SchoolWebApp.Api.Services.Jeux.IJeuxService,
    SchoolWebApp.Api.Services.Jeux.JeuxService>();
// Et le catalogue se relit depuis le site toutes les heures : publier le
// front suffit à ce que les professeurs connaissent un nouveau jeu.
builder.Services.AddHostedService<SchoolWebApp.Api.Workers.CatalogueJeuxWorker>();

// ---------------------------------------------------------------------------
// Synthèse vocale — la voix des professeurs.
//
// Même logique de clé que Claude : configuration d'abord, variable
// d'environnement ensuite. Sans clé, le service se déclare indisponible et le
// front bascule sur la voix du navigateur au lieu de rester muet.
// ---------------------------------------------------------------------------
// ---------------------------------------------------------------------------
// Stripe
// ---------------------------------------------------------------------------
builder.Services.Configure<ExemptionFacturation>(
    builder.Configuration.GetSection(ExemptionFacturation.Section));

builder.Services.Configure<OptionsStripe>(options =>
{
    builder.Configuration.GetSection(OptionsStripe.Section).Bind(options);
    options.CleSecrete ??= Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
    options.SecretWebhook ??= Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
});

builder.Services.Configure<OptionsVoix>(options =>
{
    builder.Configuration.GetSection(OptionsVoix.Section).Bind(options);
    options.ApiKey ??= Environment.GetEnvironmentVariable("OPENAI_API_KEY");
});

// La transcription tient une liaison WebSocket sortante : pas de client HTTP à
// injecter, mais une durée de vie par requête comme le reste.
builder.Services.AddScoped<ITranscriptionTempsReelService, TranscriptionTempsReelService>();

// Le façonnage des passages d'écoute lents : un cache mémoire par passage,
// donc un singleton — le même texte n'est découpé qu'une fois par processus.
builder.Services.AddSingleton<IFaconneurEcoute, FaconneurEcoute>();

// L'annonce des changements de réglage aux navigateurs ouverts. Singleton :
// il tient la liste des écoutes en cours, qui survit aux requêtes — un
// service à durée de vie de requête n'aurait personne à qui parler.
builder.Services.AddSingleton<IDiffusionReglages, DiffusionReglages>();

// LA SALLE D'ATTENTE, en un seul exemplaire pour la même raison : elle tient
// la liste de ceux qui sont entrés et de ceux qui patientent. Un service à
// durée de vie de requête recommencerait la file à zéro à chaque appel.
builder.Services.AddSingleton<
    SchoolWebApp.Api.Services.Affluence.ISalleDAttente,
    SchoolWebApp.Api.Services.Affluence.SalleDAttente>();
builder.Services.AddSingleton<SchoolWebApp.Api.Services.Affluence.ReglageAffluence>();

builder.Services.AddHttpClient<ISyntheseVocaleService, SyntheseVocaleService>(client =>
{
    // Une phrase courte revient en moins d'une seconde ; au-delà de 30 s c'est
    // que le service ne répondra pas, et l'élève attend devant un écran muet.
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ---------------------------------------------------------------------------
// Bilan hebdomadaire envoyé aux parents.
// ---------------------------------------------------------------------------
builder.Services.Configure<OptionsEmail>(options =>
{
    builder.Configuration.GetSection(OptionsEmail.Section).Bind(options);
    options.SenderPassword = string.IsNullOrWhiteSpace(options.SenderPassword)
        ? Environment.GetEnvironmentVariable("SMTP_PASSWORD")
        : options.SenderPassword;
    options.UrlSite = builder.Configuration["Clients:Spa:Url"] ?? options.UrlSite;
});

// LE SERVICE D'ENVOI, AVEC LA LISTE DES BANNIS DEVANT — Camara, le 15/09/2026 :
// une adresse bannie ne reçoit plus rien du tout. Tout ce qui demande
// `IServiceEmail` (bilans, diffusions, alertes, courriels automatiques) reçoit
// l'enveloppe filtrée ; le service réel n'est jamais demandé directement.
builder.Services.AddScoped<ServiceEmail>();
builder.Services.AddScoped<IFiltreEnvoi, SchoolWebApp.Api.Services.FiltreEnvoiBannissement>();
builder.Services.AddScoped<IServiceEmail>(fournisseur => new ServiceEmailFiltre(
    fournisseur.GetRequiredService<ServiceEmail>(),
    fournisseur.GetRequiredService<IFiltreEnvoi>(),
    fournisseur.GetRequiredService<ILogger<ServiceEmailFiltre>>()));

// ---------------------------------------------------------------------------
// La boîte de support, lue et répondue depuis l'administration.
//
// Ses identifiants sont DISTINCTS de ceux des courriels transactionnels : ces
// derniers partent de no-reply@mimia.fr, une adresse que personne ne lit. Une
// réponse au support doit partir de support@mimia.fr, sinon la réponse du
// parent se perd.
//
// Le mot de passe peut venir de l'environnement, comme celui du SMTP : c'est
// ce qui permet de ne pas l'écrire dans un fichier sur le serveur.
// ---------------------------------------------------------------------------
builder.Services.Configure<OptionsMessagerie>(options =>
{
    builder.Configuration.GetSection(OptionsMessagerie.Section).Bind(options);
    options.MotDePasse = string.IsNullOrWhiteSpace(options.MotDePasse)
        ? Environment.GetEnvironmentVariable("SUPPORT_PASSWORD")
        : options.MotDePasse;
});

builder.Services.AddScoped<IMessagerieService, MessagerieService>();
builder.Services.AddScoped<IBilanRepository, BilanRepository>();
builder.Services.AddScoped<IRedacteurBilanService, RedacteurBilanService>();
builder.Services.AddScoped<IEnvoiBilansService, EnvoiBilansService>();
builder.Services.AddHostedService<BilanHebdomadaireWorker>();

// ---------------------------------------------------------------------------
// Observation des compétences : la mémoire longue du professeur.
// Tourne après les séances, jamais pendant — la conversation ne doit rien
// attendre de plus qu'elle n'attend déjà.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IObservateurCompetencesService, ObservateurCompetencesService>();
builder.Services.AddScoped<IPlanificateurControleService, PlanificateurControleService>();

// Singleton : la file traverse les requêtes. Un contrôleur y dépose, le
// worker y puise.
builder.Services.AddSingleton<IFileObservation, FileObservation>();
builder.Services.AddHostedService<ObservationWorker>();

// Même recette pour les séances quittées avant l'heure : « Quitter le
// cours » dépose une demande de conclusion, le worker s'en charge hors de
// la requête HTTP — voir ChatViewModelBuilder.MarquerSortieAsync.
builder.Services.AddSingleton<IFileConclusion, FileConclusion>();
builder.Services.AddHostedService<ConclusionAnticipeeWorker>();

// Singleton : la liste des onglets d'administration abonnés doit survivre
// entre les requêtes, et être la MÊME pour tout le monde qui publie.
builder.Services.AddSingleton<
    SchoolWebApp.Api.Services.Notifications.IEvenementsAdminHub,
    SchoolWebApp.Api.Services.Notifications.EvenementsAdminHub>();

// Alertes de quota : la file est un singleton (elle traverse les requêtes), le
// service qui envoie le mail est scopé comme le DbContext dont il dépend.
// Purge RGPD : efface les échanges verbatim des conversations passé douze
// mois. La `DatePurge` était posée depuis toujours sur chaque conversation,
// mais personne ne l'appliquait — la base portait une promesse que rien ne
// tenait, et la politique de confidentialité s'apprêtait à la publier.
builder.Services.AddHostedService<PurgeConversationsWorker>();

// Garde le noyau du prompt chaud dans le cache d'Anthropic entre deux
// séances : un appel de 0,0135 $ toutes les cinquante minutes, contre 0,27 $
// par ouverture à froid — voir RechauffeurCacheWorker.
builder.Services.AddHostedService<RechauffeurCacheWorker>();

// La veille des tarifs d'Anthropic et d'OpenAI : une lecture par jour de leur
// page de tarifs, les nouveaux prix appliqués à nos calculs et annoncés sur
// Telegram — voir VeilleTarifsWorker.
builder.Services.AddHostedService<VeilleTarifsAnthropicWorker>();
builder.Services.AddHostedService<VeilleTarifsOpenAiWorker>();

// Le calendrier scolaire officiel (data.education.gouv.fr), resynchronisé
// une fois par jour — voir CalendrierScolaireSyncWorker pour ce que ça
// remplace : la ressaisie annuelle à la main, oubliée à coup sûr un été.
builder.Services.AddHttpClient<ICalendrierScolaireApiService, CalendrierScolaireApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mimia-Calendrier/1.0 (https://mimia.fr; contact@mimia.fr)");
});
builder.Services.AddHostedService<CalendrierScolaireSyncWorker>();

// UN VRAI USER-AGENT, PAS CELUI PAR DÉFAUT DE .NET : un site gouvernemental
// peut bloquer un client anonyme sans le dire — un timeout ou un refus
// silencieux serait alors pris pour « la page a disparu », à tort.
builder.Services.AddHttpClient<IVeilleReferentielService, VeilleReferentielService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mimia-VeilleReferentiel/1.0 (https://mimia.fr; contact@mimia.fr)");
});
builder.Services.AddHostedService<EcheanceReferentielWorker>();

// La transcription tourne AVANT la purge des octets, et c'est vital : le
// dépôt refuse d'effacer un document non transcrit, donc un worker absent ne
// perd rien — il fait juste enfler la base.
//
// Singleton comme la sonnette des planches : le contrôleur qui reçoit le
// document doit pouvoir réveiller CE worker-là. Deux instances ne se
// verraient jamais.
builder.Services.AddSingleton<SchoolWebApp.Api.Workers.ReveilDocuments>();
builder.Services.AddHostedService<TranscriptionDocumentsWorker>();

// Décrire les planches importées : sans ça, le professeur affiche une figure
// qu'il n'a jamais vue et interroge dessus au hasard de ce qu'il croit y être.
// Singleton : c'est le point de rendez-vous entre le contrôleur d'import, qui
// sonne, et le worker, qui écoute. Deux instances ne se verraient jamais.
builder.Services.AddSingleton<SchoolWebApp.Api.Workers.ReveilPlanches>();

// ENREGISTRÉ DEUX FOIS, ET C'EST VOULU. `AddHostedService<T>()` seul crée une
// instance que personne d'autre ne peut atteindre — or l'import a besoin de lui
// parler pour faire décrire SA planche tout de suite. On enregistre donc le
// singleton, puis on demande à l'hôte d'utiliser CELUI-LÀ : un seul objet, deux
// portes d'entrée.
builder.Services.AddSingleton<DescriptionPlanchesWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DescriptionPlanchesWorker>());

builder.Services.AddSingleton<IFileAlertesQuota, FileAlertesQuota>();
builder.Services.AddScoped<IAlerteQuotaService, AlerteQuotaService>();
builder.Services.AddHostedService<AlerteQuotaWorker>();

// L'onglet « Anthropic / OpenAI » : un essai minuscule chez chaque fournisseur,
// toutes les trente minutes, et un courriel quand l'un des deux refuse.
// L'état est un singleton parce que le worker l'écrit et que l'écran le lit.
builder.Services.AddSingleton<SchoolWebApp.Api.Services.Fournisseurs.EtatFournisseurs>();
builder.Services.AddHttpClient<SchoolWebApp.Api.Services.Fournisseurs.SurveillanceFournisseurs>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHostedService<SchoolWebApp.Api.Workers.SurveillanceFournisseursWorker>();

var app = builder.Build();

// Garde-fou de paiement, AVANT que la moindre requête soit servie.
//
// Une clé de production (sk_live_) en développement débiterait de vraies cartes
// à chaque essai. On refuse de démarrer plutôt que de le signaler : un
// avertissement se lit après coup, quand l'argent est déjà parti.
app.Services.GetRequiredService<IOptions<OptionsStripe>>().Value.Verifier(
    app.Environment.IsDevelopment(),
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Stripe"));

// Même garde, pour le courrier.
//
// `RedirectionDev` détourne TOUT le courrier vers une seule boîte. En
// production, plus aucun parent ne recevrait ni bilan, ni bienvenue, ni
// confirmation — sans la moindre erreur nulle part. Une panne totale et
// silencieuse, qu'on ne découvrirait que par une réclamation.
app.Services.GetRequiredService<IOptions<OptionsEmail>>().Value.Verifier(
    app.Environment.IsDevelopment(),
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Courrier"));

// ---------------------------------------------------------------------------
// MIGRATION DE LA BASE : LA RÈGLE DÉPEND DE L'ENVIRONNEMENT.
//
// PRODUCTION — on applique TOUJOURS les migrations en attente.
//
//   Le test précédent était « migrer seulement si la base n'existe pas ». Il
//   protégeait le poste de développement, mais il avait une conséquence qu'on
//   n'avait pas vue : la base de production existe, donc elle n'était JAMAIS
//   migrée. Un déploiement livrait du code qui attendait des colonnes absentes,
//   et toutes les routes qui les touchaient répondaient 500 — un site en panne
//   après une mise en ligne réussie.
//
//   Le déploiement devient donc auto-suffisant : publier et redémarrer suffit,
//   il n'y a plus de script SQL à ne pas oublier de lancer à côté.
//
// DÉVELOPPEMENT — LES MIGRATIONS S'APPLIQUENT AUSSI, ET C'EST UN CHANGEMENT.
//
//   La protection d'origine s'en tenait à `dotnet ef database update` en local,
//   pour ne pas bousculer un jeu d'essai en changeant de branche. L'expérience
//   a tranché dans l'autre sens : le code partait avec sa migration, la base
//   locale restait en arrière, et l'API répondait 500 sur les routes qui
//   touchaient aux colonnes neuves. Le symptôme ne dit rien de sa cause — on
//   cherche un défaut dans le code alors que c'est le schéma qui manque.
//
//   Le risque évité était théorique, celui qu'on subissait était quotidien.
//
// Migrate() ne rejoue rien : il lit `__EFMigrationsHistory` et n'applique que ce
// qui manque. Sur une base à jour, il ne fait rien. EF pose au passage un verrou
// exclusif, ce qui protège du cas où deux instances démarreraient ensemble.
//
// En cas d'échec, le démarrage s'arrête : mieux vaut un service qui ne monte pas
// — visible tout de suite — qu'un service qui répond 500 sur la moitié de ses
// routes sans qu'on sache pourquoi.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolWebAppDatabaseContext>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            if (!db.Database.CanConnect())
            {
                db.Database.Migrate();
                Console.WriteLine("Base de donnees creee avec succes.");
            }
            else
            {
                var enAttente = db.Database.GetPendingMigrations().ToList();

                if (enAttente.Count > 0)
                {
                    Console.WriteLine(
                        $"{enAttente.Count} migration(s) en attente : {string.Join(", ", enAttente)}");

                    db.Database.Migrate();
                    Console.WriteLine("Migrations appliquees.");
                }
            }

            // Référentiel (niveaux, matières, graphe de compétences).
            // Idempotent : n'insère que ce qui manque, donc sûr à chaque démarrage.
            await ReferentielSeeder.SeedAsync(db);
            await EcheanceReferentielSeeder.SeedAsync(db);
            await ExamenSeeder.SeedAsync(db);
            await OffresSeeder.SeedAsync(db);

            // Les six courriels automatiques du menu. N'écrase jamais une ligne
            // existante : le texte et la programmation appartiennent à
            // l'administration dès qu'ils ont été semés.
            await ModelesMailSeeder.SeedAsync(db);
            Console.WriteLine("Referentiel et grille tarifaire verifies.");

            // LE COUPE-CIRCUIT DU FLUX, RELU AU DÉMARRAGE. L'état de la
            // diffusion vit en mémoire — c'est ce qui lui évite d'interroger la
            // base dans chaque écoute. Sans cette lecture, un redémarrage
            // rallumerait donc une diffusion que l'administration avait éteinte,
            // et l'interrupteur ne tiendrait pas le premier `docker restart`.
            try
            {
                var fluxActif = await scope.ServiceProvider
                    .GetRequiredService<SchoolWebApp.Domain.Repositories.IReglageRepository>()
                    .EstActifAsync(SchoolWebApp.Api.Controllers.ReglagesController.FluxSse, true);

                scope.ServiceProvider
                    .GetRequiredService<SchoolWebApp.Domain.Services.IDiffusionReglages>()
                    .DefinirActif(fluxActif);

                if (!fluxActif)
                    Console.WriteLine("Diffusion temps reel ETEINTE par reglage.");
            }
            catch (Exception ex)
            {
                // Une lecture qui échoue laisse la diffusion allumée : c'est
                // l'état normal du produit, et elle ne coûte rien tant que
                // personne n'écoute.
                Console.WriteLine($"Lecture du reglage FLUX_SSE impossible : {ex.Message}");
            }

            // LES CARTES D'EXAMEN, VÉRIFIÉES À CHAQUE DÉMARRAGE : une faute de
            // frappe dans une partie retenue vide une carte sans rien faire
            // planter. Le relevé complet est dans l'administration, onglet
            // « Programme scolaire ».
            try
            {
                var verification = await scope.ServiceProvider
                    .GetRequiredService<SchoolWebApp.Domain.Repositories.IExamenRepository>()
                    .VerifierAsync();

                var cartes = verification.Sum(e => e.Epreuves.Count);
                var problemes = verification.Sum(e => e.NombreProblemes);

                Console.WriteLine($"Cartes d'examen verifiees : {cartes} cartes, {problemes} probleme(s).");

                foreach (var examen in verification)
                {
                    foreach (var probleme in examen.Problemes)
                        Console.WriteLine($"  [{examen.Code}] {probleme}");

                    foreach (var epreuve in examen.Epreuves)
                        foreach (var probleme in epreuve.Problemes)
                            Console.WriteLine($"  [{epreuve.Code}] {probleme}");
                }
            }
            catch (Exception ex)
            {
                // Un relevé qui échoue ne doit jamais empêcher le serveur de démarrer.
                Console.WriteLine($"Verification des cartes d'examen impossible : {ex.Message}");
            }

            // Trace la configuration Claude effective : c'est la première chose
            // qu'on veut vérifier quand une facture surprend.
            var claude = scope.ServiceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<OptionsClaude>>().Value;
            Console.WriteLine(
                $"Claude — dialogue: {claude.ModeleDialogue} (effort {claude.EffortDialogue}), " +
                $"complexe: {claude.ModeleComplexe} (effort {claude.EffortComplexe}).");
            var cleClaude = app.Configuration["Claude:ApiKey"]
                            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            Console.WriteLine(
                string.IsNullOrWhiteSpace(cleClaude)
                    ? "ATTENTION : aucune cle Claude (Claude:ApiKey ou ANTHROPIC_API_KEY) — le chat renverra une erreur."
                    : $"Cle Claude detectee (…{cleClaude[^6..]}).");

            var cleVoix = app.Configuration["Voix:ApiKey"]
                          ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            Console.WriteLine(
                string.IsNullOrWhiteSpace(cleVoix)
                    ? "ATTENTION : aucune cle de synthese vocale (Voix:ApiKey ou OPENAI_API_KEY) — repli sur la voix du navigateur."
                    : $"Cle de synthese vocale detectee (…{cleVoix[^6..]}).");

            break;
        }
        catch (Exception ex)
        {
            // Le message d'origine annonçait toujours « SQL Server pas encore
            // pret », quelle qu'ait été la panne : une erreur de seed ou de
            // configuration se déguisait en base indisponible et on cherchait
            // au mauvais endroit. Le bloc couvre plus que la connexion, la
            // trace doit le dire.
            retries--;
            Console.WriteLine($"Demarrage impossible : {ex.GetType().Name} — {ex.Message}");
            if (ex.InnerException is { } interne)
            {
                Console.WriteLine($"  cause : {interne.GetType().Name} — {interne.Message}");
            }

            if (retries == 0) break;
            Console.WriteLine($"Nouvelle tentative dans 5s... ({retries} restants)");
            Thread.Sleep(5000);
        }
    }
}

// ---------------------------------------------------------------------------
// Ordre du middleware — ne pas réorganiser
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Avant tout le reste : la compression doit envelopper les réponses de toute
// la chaîne, y compris les fichiers statiques du build React.
app.UseResponseCompression();

// UNE NAVIGATION DU NAVIGATEUR APPARTIENT TOUJOURS AU FRONT.
//
// LE DÉFAUT. Le SPA et l'API partagent un domaine, et leurs adresses se
// recouvrent : `/eleves`, `/profil`, `/admin` sont à la fois un écran React et
// un contrôleur. Tant qu'on navigue dans l'application, React s'en charge et
// personne ne voit le problème. Mais un F5 sur `/eleves` envoie une VRAIE
// requête au serveur, le contrôleur la prend, exige un jeton — qu'aucun
// navigateur n'attache à une navigation — et répond 401.
//
// L'utilisateur voit « Cette page ne fonctionne pas ». Pas la page de connexion,
// pas une erreur de l'application : une page blanche du navigateur, sur un
// compte parfaitement valide. Il suffisait d'actualiser.
//
// CE QUI DISTINGUE LES DEUX. Un navigateur qui charge une page annonce
// `Sec-Fetch-Dest: document` et réclame du `text/html`. Une requête de
// l'application — axios, fetch — ne fait ni l'un ni l'autre : elle demande du
// JSON. Le même chemin, deux intentions, et elles se lisent dans l'en-tête.
//
// La réécriture a lieu AVANT `UseRouting` : c'est là que l'endpoint se choisit,
// et une fois le contrôleur retenu il est trop tard.
//
// POURQUOI CE SENS PAR DÉFAUT. L'API n'est consommée qu'en XHR — aucun de ses
// points d'entrée n'est fait pour être ouvert dans un onglet. Donc toute
// navigation revient au front, sauf ce qui porte une extension de fichier
// (`/favicon.ico`, `/static/...`) et sauf Swagger, qui se visite justement à la
// main. Une liste blanche des écrans React aurait obligé le serveur à connaître
// les routes du front, et à les tenir à jour à chaque écran ajouté.
app.Use(async (contexte, suivant) =>
{
    var requete = contexte.Request;

    var estUneLecture = HttpMethods.IsGet(requete.Method) || HttpMethods.IsHead(requete.Method);

    var vientDeLaBarreDAdresse =
        string.Equals(requete.Headers["Sec-Fetch-Dest"], "document", StringComparison.Ordinal)
        || requete.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

    var chemin = requete.Path.Value ?? "/";

    // Un fichier se reconnaît à son extension. Sans ce test, une navigation
    // vers /favicon.ico rendrait index.html.
    var viseUnFichier = Path.HasExtension(chemin);

    var reserveAuServeur = chemin.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

    // UNE POIGNÉE DE MAIN WEBSOCKET N'EST PAS UNE NAVIGATION, MÊME QUAND ELLE
    // LUI RESSEMBLE.
    //
    // Elle voyage en GET, et FIREFOX y joint son en-tête `Accept` habituel —
    // `text/html` compris. Sans ce test, l'écoute de l'élève recevait donc
    // `index.html` en guise de liaison : le micro ne marchait plus du tout sur
    // ce navigateur, silencieusement. Vérifié en envoyant les deux poignées de
    // main, celle de Chrome et celle de Firefox, sur le serveur local.
    //
    // On lit `Sec-WebSocket-Key`, que la norme rend obligatoire, plutôt que
    // `HttpContext.WebSockets` : ce dernier dépend d'une fonctionnalité que
    // `UseWebSockets` installe plus loin dans le pipeline, et nous sommes ici
    // avant elle.
    var demandeUneBascule =
        requete.Headers.ContainsKey("Sec-WebSocket-Key")
        || string.Equals(requete.Headers.Upgrade, "websocket", StringComparison.OrdinalIgnoreCase);

    if (estUneLecture && vientDeLaBarreDAdresse
        && !viseUnFichier && !reserveAuServeur && !demandeUneBascule)
    {
        requete.Path = "/index.html";
    }

    await suivant();
});

app.UseRouting();
app.UseCors("AllowFront");      // APRÈS UseRouting, AVANT UseAuthentication

// Le navigateur ne sait pas poser d'en-tête sur une connexion WebSocket : son
// API ne l'autorise pas. Le jeton arrive donc en paramètre d'URL, et on le
// remet dans l'en-tête avant l'authentification, qui n'y voit que du feu.
//
// Restreint aux chemins d'écoute : ailleurs, accepter un jeton dans l'URL
// ouvrirait une porte inutile — les URL se retrouvent dans les journaux, les
// historiques et les en-têtes Referer.
//
// LE SCHÉMA VIENT DU CLIENT, ON NE LE DEVINE PLUS.
//
// Cette ligne écrivait « Bearer » quoi qu'il arrive. Un parent porte bien ce
// schéma-là, mais un enfant entre par un code et porte « Eleve » : sa session
// était donc réétiquetée en jeton OIDC, refusée à la validation, et le
// WebSocket se fermait aussitôt — code 1006, sans un mot d'explication.
//
// Le client envoie maintenant l'en-tête complète. Le préfixe « Bearer » n'est
// ajouté que si la valeur n'annonce aucun schéma, ce qui garde compatible un
// navigateur resté sur l'ancien front le temps que le cache se vide.
app.Use(async (contexte, suivant) =>
{
    if (contexte.Request.Path.StartsWithSegments("/api/ecoute")
        && contexte.Request.Query.TryGetValue("access_token", out var jeton)
        && !contexte.Request.Headers.ContainsKey("Authorization"))
    {
        var valeur = jeton.ToString().Trim();

        // Un schéma, c'est un mot suivi d'une espace puis du jeton. Un jeton nu
        // n'en contient aucune : c'est ce qui les distingue sans avoir à tenir
        // la liste des schémas connus.
        var annonceSonSchema = valeur.Contains(' ');

        contexte.Request.Headers.Authorization =
            annonceSonSchema ? valeur : $"Bearer {valeur}";
    }

    await suivant();
});

app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();

// APRES L AUTHENTIFICATION : le verrou a besoin de savoir QUI appelle.
// Avant les points de terminaison : un banni ne doit atteindre aucun d eux.
app.UseMiddleware<SchoolWebApp.Api.Middleware.VerrouBannissementMiddleware>();

// LE GARDE DE LA SALLE D'ATTENTE, juste après — et l'ordre compte deux fois.
//
// Après l'authentification, parce qu'il laisse toujours entrer
// l'administrateur : celui qui a allumé la salle doit pouvoir l'éteindre.
//
// Après le verrou de bannissement, parce qu'un compte fermé n'a pas à occuper
// une place dans la file avant de se faire refouler.
app.UseMiddleware<SchoolWebApp.Api.Middleware.GardeAffluenceMiddleware>();
// ---------------------------------------------------------------------------
// Le site React, servi depuis wwwroot/ — le contenu de `npm run build`.
//
// LES EN-TÊTES DE CACHE NE SONT PAS UN DÉTAIL
// -------------------------------------------
// Sans eux, le navigateur applique sa propre heuristique et garde `index.html`
// en mémoire. Au déploiement suivant, il réclame les bundles cités par
// l'ancienne page — dont les noms portent l'empreinte d'avant et qui n'existent
// plus. L'utilisateur voit une page blanche, et un rechargement ordinaire n'y
// change rien : il faut vider le cache, ce qu'aucun parent ne fera.
//
// D'où deux régimes opposés :
//   /static/** porte une empreinte dans le nom  -> immuable, gardé un an ;
//               un nouveau contenu = un nouveau nom, jamais le même fichier.
//   index.html a un nom fixe                    -> revalidé à chaque visite.
// ---------------------------------------------------------------------------
var optionsFichiers = new StaticFileOptions
{
    OnPrepareResponse = contexte =>
    {
        var chemin = contexte.Context.Request.Path.Value ?? string.Empty;

        contexte.Context.Response.Headers.CacheControl =
            chemin.StartsWith("/static/", StringComparison.OrdinalIgnoreCase)
                ? "public, max-age=31536000, immutable"
                : "no-cache";
    }
};

app.UseDefaultFiles();
app.UseStaticFiles(optionsFichiers);
app.MapControllers();

// ---------------------------------------------------------------------------
// UNE ROUTE D'API INCONNUE DOIT ÉCHOUER, PAS RENDRE DU HTML.
// ---------------------------------------------------------------------------
//
// Le repli ci-dessous renvoie index.html avec un code 200 pour TOUTE adresse
// non reconnue. Pour un navigateur c'est ce qu'on veut ; pour un appel d'API
// c'est un piège : la réponse est un succès, le client lit les champs qu'il
// attend sur une chaîne de HTML, les trouve absents, et affiche des zéros
// parfaitement plausibles.
//
// C'est arrivé : un écran a montré « 0 compte parent » sur une base qui en
// comptait onze, parce que la route venait d'être écrite et pas encore
// déployée. Aucune erreur nulle part — le symptôme ressemblait à une donnée,
// ce qui est la panne la plus coûteuse à diagnostiquer.
//
// LE TRI SE FAIT SUR L'INTENTION, PAS SUR L'ADRESSE. Un filtre par préfixe
// serait plus simple à lire mais faux ici : « /admin » est À LA FOIS le
// préfixe d'AdminController et une route React. Le même chemin sert donc les
// deux usages, et seul l'en-tête « Accept » les distingue.
//
// ON RECONNAÎT UN APPEL D'API À CE QU'IL RÉCLAME DU JSON — et non à ce qu'il
// omet de réclamer du HTML. La nuance a coûté cher.
//
// LA PREMIÈRE VERSION RENDAIT 404 À TOUT CE QUI NE DEMANDAIT PAS EXPRESSÉMENT
// « text/html ». Or un navigateur est à peu près le seul client au monde à le
// demander. Tous les autres envoient « */* », ou rien du tout — et recevaient
// donc une 404 sur la PAGE D'ACCUEIL :
//
//   — le validateur de Google, qui a refusé le 25/09/2026 de publier une
//     application OAuth au motif que « https://mimia.fr ne répond pas » ;
//   — les aperçus de lien des messageries, les sondes de disponibilité, et
//     une partie des robots d'indexation.
//
// Le site était debout, et invisible pour tout ce qui n'était pas un
// navigateur. Personne ne l'a vu pendant des mois, parce qu'on ne teste
// jamais son site avec « curl » sans y penser.
//
// « application/json » EST LA BONNE SIGNATURE : axios, qui porte tous les
// appels du front, envoie « application/json, text/plain, */* ». Le cas
// d'origine — un écran affichant « 0 compte parent » parce qu'une route
// n'était pas encore déployée — reste donc couvert. Ce qui change, c'est que
// l'inconnu reçoit désormais la page plutôt qu'un refus.
app.Use(async (contexte, suivant) =>
{
    var versLeSpa = contexte.GetEndpoint()?.Metadata.GetMetadata<ReplieVersLeSpa>() is not null;

    if (versLeSpa && contexte.Request.Headers.Accept.ToString()
            .Contains("application/json", StringComparison.OrdinalIgnoreCase))
    {
        contexte.Response.StatusCode = StatusCodes.Status404NotFound;

        await contexte.Response.WriteAsJsonAsync(new
        {
            message = "Cette route n'existe pas.",
            chemin = contexte.Request.Path.Value,
        });

        return;
    }

    await suivant();
});

// Le repli SPA : toute adresse qui ne correspond à aucun contrôleur ni à aucun
// fichier rend index.html, et c'est React qui décide de la page. Sans lui,
// rafraîchir /tarifs ou /mes-enfants renverrait une 404.
// Les mêmes options qu'au-dessus, sinon index.html repartirait sans en-tête
// de cache par ce chemin-là — c'est pourtant le plus fréquent.
app.MapFallbackToFile("index.html", optionsFichiers)
   .WithMetadata(new ReplieVersLeSpa());

app.Run();

/// <summary>
/// Marque le point de repli SPA pour le filtre ci-dessus.
///
/// Une métadonnée plutôt qu'une comparaison de nom : le libellé d'un endpoint
/// est un détail interne du framework, qui changerait sans prévenir et sans
/// rien casser de visible — le filtre cesserait simplement de filtrer.
/// </summary>
internal sealed record ReplieVersLeSpa;
