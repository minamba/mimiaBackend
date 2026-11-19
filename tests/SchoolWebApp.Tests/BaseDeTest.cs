using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Domain.Emails;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// UNE VRAIE BASE SQL SERVER, MAIS DE TEST. Les filtres d'envoi et le consentement s'écrivent en SQL
    /// (`ExecuteUpdate`, sous-requêtes) : un faux fournisseur en mémoire ne les exécuterait pas comme SQL Server.
    ///
    /// LA BASE : variable `MIMIA_TEST_DB` (chaîne de connexion), sinon `SCHOOL_Database_MinambaTest` sur `.\SQLExpress`
    /// (la base de test du PC de Min's, migrations à jour). GARDE-FOU : le nom de la base doit contenir « Test » — les
    /// tests créent puis effacent leurs propres parents fictifs (`@exemple.invalid`), ils ne doivent jamais tourner sur
    /// une base réelle.
    /// </summary>
    public abstract class BaseDeTest : IAsyncLifetime
    {
        private const string ParDefaut =
            "Server=.\\SQLExpress;Database=SCHOOL_Database_MinambaTest;Trusted_Connection=True;TrustServerCertificate=True;";

        private readonly List<int> _parents = [];
        private readonly List<string> _bannis = [];

        protected BaseDeTest()
        {
            var chaine = Environment.GetEnvironmentVariable("MIMIA_TEST_DB") ?? ParDefaut;
            var nom = new SqlConnectionStringBuilder(chaine).InitialCatalog;
            if (!nom.Contains("Test", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Base « {nom} » refusée : les tests ne tournent que sur une base de TEST.");
            }

            Options = new DbContextOptionsBuilder<SchoolWebAppDatabaseContext>().UseSqlServer(chaine).Options;
            Contexte = NouveauContexte();
            Envois = new EnvoiAutomatiqueRepository(Contexte);
            Jetons = CreerJetons("cle-de-test-uniquement-0123456789");
        }

        protected DbContextOptions<SchoolWebAppDatabaseContext> Options { get; }

        protected SchoolWebAppDatabaseContext Contexte { get; }

        protected EnvoiAutomatiqueRepository Envois { get; }

        protected JetonDesabonnement Jetons { get; }

        protected SchoolWebAppDatabaseContext NouveauContexte() => new(Options);

        protected static JetonDesabonnement CreerJetons(string? cle, string environnement = "Production")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Courrier:CleDesabonnement"] = cle,
                    ["Courrier:UrlApi"] = "https://api.exemple.invalid",
                })
                .Build();

            var env = new Mock<IHostEnvironment>();
            env.SetupGet(e => e.EnvironmentName).Returns(environnement);

            return new JetonDesabonnement(
                config, env.Object, Microsoft.Extensions.Options.Options.Create(new OptionsEmail { UrlSite = "https://site.exemple.invalid" }),
                NullLogger<JetonDesabonnement>.Instance);
        }

        /// <summary>Un parent fictif, créé comme à sa première connexion, éventuellement déjà consentant aux offres.</summary>
        protected async Task<Domain.Models.Parent> CreerParentAsync(bool consenti = false, string? prenom = "Camille")
        {
            var cle = Guid.NewGuid().ToString("N");
            // Sans service d'abonnement : aucun essai n'est ouvert, le parent fictif reste nu.
            var parent = await new Domain.Services.impl.ParentService(new ParentRepository(Contexte))
                .GetOrCreateAsync($"test-auto-{cle}", $"test-{cle}@exemple.invalid", prenom, "Test");
            _parents.Add(parent.Id);

            if (consenti)
            {
                await Contexte.Parents.Where(p => p.Id == parent.Id).ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.OffresEmail, true)
                    .SetProperty(p => p.OffresEmailConsentiLe, DateTime.UtcNow.AddDays(-1)));
                parent.OffresEmail = true;
            }

            return parent;
        }

        protected async Task BannirAsync(string mail)
        {
            await using var c = NouveauContexte();
            c.MailsBannis.Add(new MailBanni { Mail = mail, Motif = "test automatique", DateCreation = DateTime.UtcNow });
            await c.SaveChangesAsync();
            _bannis.Add(mail);
        }

        protected async Task<Parent> RelireAsync(int id)
        {
            await using var c = NouveauContexte();
            return await c.Parents.AsNoTracking().SingleAsync(p => p.Id == id);
        }

        protected async Task<List<JournalConsentementOffres>> JournalAsync(int id)
        {
            await using var c = NouveauContexte();
            return await c.JournalConsentementOffres.AsNoTracking().Where(j => j.ParentId == id).OrderBy(j => j.Id).ToListAsync();
        }

        /// <summary>Un contexte HTTP avec une adresse IP propre au test : la limite de 30 appels par heure est par IP.</summary>
        protected static ControllerContext ContexteHttp()
        {
            var http = new DefaultHttpContext();
            var octets = Guid.NewGuid().ToByteArray();
            http.Connection.RemoteIpAddress = new IPAddress([10, octets[0], octets[1], octets[2]]);
            return new ControllerContext { HttpContext = http };
        }

        public Task InitializeAsync() => Task.CompletedTask;

        /// <summary>Efface ce que le test a créé, et seulement cela (le journal et les désabonnements suivent en cascade).</summary>
        public async Task DisposeAsync()
        {
            await using var c = NouveauContexte();
            if (_parents.Count > 0) await c.Parents.Where(p => _parents.Contains(p.Id)).ExecuteDeleteAsync();
            if (_bannis.Count > 0) await c.MailsBannis.Where(b => _bannis.Contains(b.Mail)).ExecuteDeleteAsync();
            await Contexte.DisposeAsync();
        }
    }
}
