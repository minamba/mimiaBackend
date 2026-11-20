using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// « Envoyer à un parent » un template d'OFFRE (il porte `{{lien_desinscription}}`) : seulement à un parent consentant,
    /// sans copie, avec son prénom, son lien signé, le pied commercial et les en-têtes `List-Unsubscribe`. Un template sans
    /// cette variable est une correspondance (20/11/2026) : il part à l'adresse choisie, copies comprises, sans lien.
    /// </summary>
    public class EnvoiCommercialTests : BaseDeTest
    {
        private const int IdModele = 4242;

        private sealed record Envoi(
            IReadOnlyList<string> Destinataires, IDictionary<string, string> Valeurs, IReadOnlyDictionary<string, string>? EnTetes);

        private const string TexteOffre = "Bonjour {{prenom}}, voici notre offre. Pour ne plus la recevoir : {{lien_desinscription}}.";

        private (ModelesMailController Controleur, Mock<IServiceEmail> Email, List<Envoi> Envois, Mock<IBoitesAdminService> Boites, Mock<IBannissementRepository> Bannis)
            Preparer(JetonDesabonnement jetons, string texte = TexteOffre)
        {
            var modeles = new Mock<IModeleMailRepository>();
            modeles.Setup(m => m.GetDetailAsync(IdModele, It.IsAny<CancellationToken>())).ReturnsAsync(new ModeleMailDetail
            {
                Id = IdModele,
                Nature = NatureModeleMail.Diffusion,
                Nom = "Offre de test",
                Sujet = "Bonjour {{prenom}}",
                Titre = "Notre offre",
                Texte = texte,
            });
            modeles.Setup(m => m.GetPiecesAsync(IdModele, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            var diffusion = new DiffusionService(Mock.Of<IServiceScopeFactory>(), jetons, NullLogger<DiffusionService>.Instance);
            var controleur = new ModelesMailController(
                modeles.Object, new ComposeurModeleMail(diffusion), diffusion,
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail { UrlSite = "https://site.exemple.invalid" }),
                NullLogger<ModelesMailController>.Instance);

            var envois = new List<Envoi>();
            var email = new Mock<IServiceEmail>();
            email.Setup(e => e.DiffuserAsync(
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IReadOnlyList<PieceMail>?>(), It.IsAny<IProgress<int>?>(),
                    It.IsAny<CancellationToken>(), It.IsAny<Expediteur?>(), It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<IReadOnlyList<string>?>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
                .Callback((IReadOnlyList<string> d, string _, string _, IDictionary<string, string> v, IReadOnlyList<PieceMail>? _,
                    IReadOnlyList<PieceMail>? _, IProgress<int>? _, CancellationToken _, Expediteur? _, IReadOnlyList<string>? _,
                    IReadOnlyList<string>? _, IReadOnlyDictionary<string, string>? t) => envois.Add(new Envoi(d, v, t)))
                .ReturnsAsync(new ResultatDiffusion(1, 0));
            email.Setup(e => e.ComposerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()))
                .ReturnsAsync("<html></html>");

            var boites = new Mock<IBoitesAdminService>();
            boites.Setup(b => b.ExpediteurAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((new ExpediteurMessage(Mock.Of<IMessagerieService>(), new Expediteur("admin@exemple.invalid", "Admin"), "Signature"), null));

            var bannis = new Mock<IBannissementRepository>();
            return (controleur, email, envois, boites, bannis);
        }

        private Task<IActionResult> EnvoyerAsync(
            (ModelesMailController Controleur, Mock<IServiceEmail> Email, List<Envoi> Envois, Mock<IBoitesAdminService> Boites, Mock<IBannissementRepository> Bannis) p,
            JetonDesabonnement jetons, string destinataire, List<string>? cc = null) =>
            p.Controleur.EnvoyerAUnParent(
                IdModele, new ModelesMailController.EnvoiParentRequest { Destinataire = destinataire, Cc = cc },
                p.Email.Object, p.Bannis.Object, p.Boites.Object, Envois, jetons, CancellationToken.None);

        private static string Message(IActionResult r)
        {
            var valeur = Assert.IsType<BadRequestObjectResult>(r).Value!;
            return (string)valeur.GetType().GetProperty("message")!.GetValue(valeur)!;
        }

        [Fact]
        public async Task Parent_consentant_recoit_l_offre_avec_lien_pied_et_en_tetes()
        {
            var parent = await CreerParentAsync(consenti: true, prenom: "Nadia");
            var p = Preparer(Jetons);

            var r = await EnvoyerAsync(p, Jetons, parent.Mail!);

            Assert.IsType<OkObjectResult>(r);
            var envoi = Assert.Single(p.Envois);
            Assert.Equal([parent.Mail!], envoi.Destinataires);
            Assert.Contains("Nadia", envoi.Valeurs["corpsMessage"]);
            Assert.Contains("/desabonnement?jeton=", envoi.Valeurs["corpsMessage"]);
            Assert.DoesNotContain("{{lien_desinscription}}", envoi.Valeurs["corpsMessage"]);
            Assert.Contains("accepté de recevoir les offres", envoi.Valeurs["mentionPied"]);
            Assert.Contains("/desabonnement?jeton=", envoi.Valeurs["mentionPied"]);
            Assert.NotNull(envoi.EnTetes);
            Assert.Contains("/desabonnement/un-clic?jeton=", envoi.EnTetes!["List-Unsubscribe"]);
            Assert.Equal("List-Unsubscribe=One-Click", envoi.EnTetes["List-Unsubscribe-Post"]);
        }

        /// <summary>
        /// LE COURRIEL TEL QUE LE PARENT LE REÇOIT : les valeurs exactes de l'envoi, rendues par le vrai service et le vrai
        /// gabarit embarqué « diffusion ». Avec `MIMIA_TEST_RENDU=<dossier>`, le HTML y est écrit (capture du compte rendu).
        /// </summary>
        [Fact]
        public async Task Rendu_reel_dans_le_gabarit_avec_pied_et_lien()
        {
            var parent = await CreerParentAsync(consenti: true, prenom: "Nadia");
            var p = Preparer(Jetons);
            await EnvoyerAsync(p, Jetons, parent.Mail!);
            var envoi = Assert.Single(p.Envois);

            var service = new ServiceEmail(
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ServiceEmail>.Instance);
            var html = await service.RendreAsync("Bonjour Nadia", "diffusion", envoi.Valeurs);

            Assert.Contains("Bonjour Nadia", html);
            Assert.Contains("https://site.exemple.invalid/desabonnement?jeton=", html);
            Assert.Contains("Me désinscrire de ces e-mails", html);
            Assert.Contains("accepté de recevoir les offres et nouveautés", html);
            Assert.DoesNotContain("{{", html);

            var dossier = Environment.GetEnvironmentVariable("MIMIA_TEST_RENDU");
            if (!string.IsNullOrWhiteSpace(dossier))
            {
                await File.WriteAllTextAsync(Path.Combine(dossier, "offre-individuelle.html"), html);
            }
        }

        [Fact]
        public async Task Parent_non_consentant_refuse_rien_ne_part()
        {
            var parent = await CreerParentAsync();
            var p = Preparer(Jetons);

            Assert.Contains("aucun parent consentant", Message(await EnvoyerAsync(p, Jetons, parent.Mail!)));
            Assert.Empty(p.Envois);
        }

        [Fact]
        public async Task Adresse_libre_refusee()
        {
            var p = Preparer(Jetons);

            Assert.Contains("aucun parent consentant", Message(await EnvoyerAsync(p, Jetons, $"libre-{Guid.NewGuid():N}@exemple.invalid")));
            Assert.Empty(p.Envois);
        }

        [Fact]
        public async Task Copies_refusees_meme_pour_un_parent_consentant()
        {
            var parent = await CreerParentAsync(consenti: true);
            var p = Preparer(Jetons);

            Assert.Contains("sans copie", Message(await EnvoyerAsync(p, Jetons, parent.Mail!, ["autre@exemple.invalid"])));
            Assert.Empty(p.Envois);
        }

        [Fact]
        public async Task Template_sans_lien_de_desinscription_une_correspondance_part_a_tout_parent_avec_copie()
        {
            // 20/11/2026 : un template n'est plus commercial d'office. Sans `{{lien_desinscription}}`, il part comme un
            // message personnel : parent non consentant, copie, sans lien ni en-têtes de désinscription.
            var parent = await CreerParentAsync();
            var p = Preparer(Jetons, "Bonjour {{prenom}}, voici la réponse à votre question.");

            var r = await EnvoyerAsync(p, Jetons, parent.Mail!, ["autre@exemple.invalid"]);

            Assert.IsType<OkObjectResult>(r);
            var envoi = Assert.Single(p.Envois);
            Assert.Equal([parent.Mail!], envoi.Destinataires);
            Assert.Null(envoi.EnTetes);
            Assert.DoesNotContain("/desabonnement", envoi.Valeurs["corpsMessage"]);
            Assert.DoesNotContain("désinscrire", envoi.Valeurs["mentionPied"], StringComparison.OrdinalIgnoreCase);
            Assert.Null(ModelesMailController.CategorieEnvoiIndividuel(new ModeleMailDetail { Sujet = "a", Texte = "b" }));
        }

        [Fact]
        public async Task Sans_cle_de_desinscription_aucune_offre_ne_part()
        {
            var parent = await CreerParentAsync(consenti: true);
            var sansCle = CreerJetons(null);
            var p = Preparer(sansCle);

            Assert.Contains("Clé de désabonnement absente", Message(await EnvoyerAsync(p, sansCle, parent.Mail!)));
            Assert.Empty(p.Envois);
        }
    }
}
