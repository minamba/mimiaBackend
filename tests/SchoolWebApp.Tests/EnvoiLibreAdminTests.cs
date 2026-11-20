using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// LES ROUTES D'ENVOI LIBRE DE L'ADMIN (revue d'Omar du lot v4, 20/11/2026) :
    /// - l'ancien `POST /admin/diffusion` (à TOUS, sans case ni garde-fou) n'existe plus ;
    /// - `POST /admin/mails/parent` (envoi libre à un parent, toujours une correspondance) refuse `{{lien_desinscription}}`
    ///   avec le même 400 `lien_sans_promotion` que l'envoi d'un template case décochée.
    /// Aucun accès base : tout est simulé.
    /// </summary>
    public class EnvoiLibreAdminTests
    {
        private sealed class Preparation
        {
            public required AdminController Controleur { get; init; }
            public required Mock<IServiceEmail> Email { get; init; }
            public required IDiffusionService Diffusion { get; init; }
            public required Mock<IBannissementRepository> Bannis { get; init; }
            public required Mock<IBoitesAdminService> Boites { get; init; }
        }

        private static Preparation Preparer()
        {
            var controleur = new AdminController(
                Mock.Of<SchoolWebApp.Domain.Services.IAdminService>(),
                NullLogger<AdminController>.Instance,
                new ComptesProteges(new ConfigurationBuilder().Build()),
                Microsoft.Extensions.Options.Options.Create(new ExemptionFacturation()))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() },
                },
            };

            var email = new Mock<IServiceEmail>();
            email.Setup(e => e.DiffuserAsync(
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IProgress<int>?>(),
                    It.IsAny<CancellationToken>(), It.IsAny<Expediteur?>(), It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<IReadOnlyList<string>?>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
                .ReturnsAsync(new ResultatDiffusion(1, 0));
            email.Setup(e => e.ComposerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()))
                .ReturnsAsync("<html></html>");

            var boites = new Mock<IBoitesAdminService>();
            boites.Setup(b => b.ExpediteurAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((new ExpediteurMessage(Mock.Of<IMessagerieService>(), new Expediteur("admin@exemple.invalid", "Admin"), "Signature"), null));

            var jetons = new JetonDesabonnement(
                new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Courrier:CleDesabonnement"] = "cle-de-test-uniquement-0123456789",
                        ["Courrier:UrlApi"] = "https://api.exemple.invalid",
                    }).Build(),
                Mock.Of<Microsoft.Extensions.Hosting.IHostEnvironment>(h => h.EnvironmentName == "Production"),
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail { UrlSite = "https://site.exemple.invalid" }),
                NullLogger<JetonDesabonnement>.Instance);

            return new Preparation
            {
                Controleur = controleur,
                Email = email,
                Diffusion = new DiffusionService(Mock.Of<IServiceScopeFactory>(), jetons, NullLogger<DiffusionService>.Instance),
                Bannis = new Mock<IBannissementRepository>(),
                Boites = boites,
            };
        }

        private static Task<IActionResult> EnvoyerAsync(Preparation p, string sujet, string texte, string titre = "Titre") =>
            p.Controleur.EnvoyerMailParent(
                "parent@exemple.invalid", sujet, titre, texte, p.Diffusion, p.Email.Object, p.Bannis.Object, p.Boites.Object);

        private static void VerifierAucunEnvoi(Preparation p) =>
            p.Email.Verify(e => e.DiffuserAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(),
                It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IProgress<int>?>(),
                It.IsAny<CancellationToken>(), It.IsAny<Expediteur?>(), It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<IReadOnlyList<string>?>(), It.IsAny<IReadOnlyDictionary<string, string>?>()), Times.Never);

        private static string Champ(IActionResult r, string nom)
        {
            var valeur = Assert.IsType<BadRequestObjectResult>(r).Value!;
            return (string)valeur.GetType().GetProperty(nom)!.GetValue(valeur)!;
        }

        [Theory]
        [InlineData("Bonjour", "Pour ne plus recevoir nos offres : {{lien_desinscription}}", "Titre")]
        [InlineData("Bonjour", "Se désinscrire : {{ lienDesinscription }}", "Titre")]
        [InlineData("Offre {{lien_desinscription}}", "Bonjour", "Titre")]
        [InlineData("Bonjour", "Bonjour", "Titre {{LIEN_DESINSCRIPTION}}")]
        public async Task Envoi_libre_a_un_parent_avec_le_lien_refuse_rien_ne_part(string sujet, string texte, string titre)
        {
            var p = Preparer();

            var r = await EnvoyerAsync(p, sujet, texte, titre);

            Assert.Equal("lien_sans_promotion", Champ(r, "code"));
            Assert.Equal(DiffusionService.RefusLienDansInformation, Champ(r, "message"));
            VerifierAucunEnvoi(p);
        }

        [Fact]
        public async Task Envoi_libre_a_un_parent_sans_le_lien_part_normalement()
        {
            var p = Preparer();

            var r = await EnvoyerAsync(p, "Votre question", "Bonjour, voici la réponse à votre question.");

            Assert.IsType<OkObjectResult>(r);
            p.Email.Verify(e => e.DiffuserAsync(
                It.Is<IReadOnlyList<string>>(d => d.Count == 1 && d[0] == "parent@exemple.invalid"), It.IsAny<string>(),
                "diffusion", It.IsAny<IDictionary<string, string>>(),
                It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IProgress<int>?>(),
                It.IsAny<CancellationToken>(), It.IsAny<Expediteur?>(), It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<IReadOnlyList<string>?>(), It.IsAny<IReadOnlyDictionary<string, string>?>()), Times.Once);
        }

        [Fact]
        public void L_ancien_envoi_libre_a_tous_POST_admin_diffusion_n_existe_plus()
        {
            // Toutes les routes POST de l'AdminController : aucune n'est plus « diffusion » seule. L'aperçu reste.
            var routesPost = typeof(AdminController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetCustomAttributes<HttpPostAttribute>())
                .Select(a => a.Template)
                .ToList();

            Assert.DoesNotContain("diffusion", routesPost);
            Assert.Contains("diffusion/apercu", routesPost);
            Assert.Contains("mails/parent", routesPost);
            Assert.Null(typeof(AdminController).GetMethod("LancerDiffusion"));
        }
    }
}
