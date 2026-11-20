using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// UN COURRIEL PROGRAMMÉ CRÉÉ DEPUIS L'ÉCRAN EST, PAR DÉFAUT, UNE INFORMATION (décision de Min's, 20/11/2026) : il part à
    /// TOUTE sa cible, consentement aux offres ou non, sans lien de désinscription ni en-têtes, et même sans clé de
    /// désinscription. SA CASE « PROMOTION » COCHÉE en fait une offre : aux seuls parents de la cible qui ont accepté les
    /// offres, avec le lien signé et `List-Unsubscribe`. Relances et rappels gardent leur propre désabonnement.
    /// </summary>
    public class EnvoiProgrammeInformationTests : BaseDeTest
    {
        // Un identifiant de modèle qui n'existe pas en base : son code de journal (`MODELE:<id>`) n'appartient qu'à ce test.
        private const int IdModele = 987650;

        private const string TexteInformation = "Bonjour {{prenom}}, voici les nouveautés de la semaine.";

        private const string TexteAvecLien = "Bonjour {{prenom}}, -20 % ce week-end. Se désinscrire : {{lien_desinscription}}";

        private sealed record Envoi(string Destinataire, IDictionary<string, string> Valeurs, IReadOnlyDictionary<string, string>? EnTetes);

        private sealed class SessionFactice(List<Envoi> envois) : ISessionEnvoi
        {
            public int Envoyes { get; private set; }

            public int Echecs => 0;

            public Task<bool> EnvoyerAsync(string destinataire, string sujet, string gabarit, IDictionary<string, string> valeurs,
                CancellationToken ct = default) =>
                EnvoyerAsync(destinataire, sujet, gabarit, valeurs, null, null, null, ct);

            public Task<bool> EnvoyerAsync(string destinataire, string sujet, string gabarit, IDictionary<string, string> valeurs,
                IReadOnlyList<PieceMail>? images, IReadOnlyList<PieceMail>? documents, IReadOnlyDictionary<string, string>? enTetes,
                CancellationToken ct = default)
            {
                lock (envois) envois.Add(new Envoi(destinataire, new Dictionary<string, string>(valeurs), enTetes));
                Envoyes++;
                return Task.FromResult(true);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private static ModeleMailDetail ModeleDeLEcran(string texte = TexteInformation, bool promotion = false) => new()
        {
            Id = IdModele,
            Nature = NatureModeleMail.Automatique,
            Code = null, // créé depuis l'écran : la case « Promotion » décide
            Nom = "Courriel programmé de test",
            Sujet = "Du nouveau, {{prenom}}",
            Titre = "Du nouveau",
            Texte = texte,
            Cible = CibleEnvoi.Nouveaux,
            Promotion = promotion,
            Actif = true,
        };

        private (EnvoisAutomatiquesService Service, Mock<IServiceEmail> Email, List<Envoi> Envois) Preparer(JetonDesabonnement jetons)
        {
            var envois = new List<Envoi>();
            var email = new Mock<IServiceEmail>();
            email.Setup(e => e.OuvrirSession()).Returns(() => new SessionFactice(envois));

            var modeles = new Mock<IModeleMailRepository>();
            modeles.Setup(m => m.GetPiecesAsync(IdModele, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            var diffusion = new DiffusionService(Mock.Of<IServiceScopeFactory>(), jetons, NullLogger<DiffusionService>.Instance);

            var service = new EnvoisAutomatiquesService(
                modeles.Object, Envois, email.Object, new ComposeurModeleMail(diffusion), diffusion, jetons,
                new ConfigurationBuilder().Build(),
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail { UrlSite = "https://site.exemple.invalid" }),
                NullLogger<EnvoisAutomatiquesService>.Instance);

            return (service, email, envois);
        }

        private async Task EffacerLeJournalAsync()
        {
            await using var c = NouveauContexte();
            await c.EnvoisAutomatiques.Where(e => e.CodeModele == CodeModeleMail.CodeJournal(null, IdModele)).ExecuteDeleteAsync();
        }

        [Fact]
        public void La_case_promotion_ne_change_que_les_courriels_crees_depuis_l_ecran()
        {
            Assert.Null(CodeModeleMail.Categorie(null));
            Assert.Null(CodeModeleMail.Categorie(null, promotion: false));
            Assert.Equal(CategorieDesabonnement.Diffusion, CodeModeleMail.Categorie(null, promotion: true));

            // Les courriels reliés à un code gardent leur catégorie, case cochée ou non.
            foreach (var promotion in new[] { false, true })
            {
                Assert.Equal(CategorieDesabonnement.Relance, CodeModeleMail.Categorie(CodeModeleMail.Relance, promotion));
                Assert.Equal(CategorieDesabonnement.Rappels, CodeModeleMail.Categorie(CodeModeleMail.Rappels, promotion));
                Assert.Equal(CategorieDesabonnement.Avis, CodeModeleMail.Categorie(CodeModeleMail.AvisGeneral, promotion));
                Assert.Null(CodeModeleMail.Categorie(CodeModeleMail.Bilans, promotion));
                Assert.Null(CodeModeleMail.Categorie(CodeModeleMail.FinEssai, promotion));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Case_decochee_part_a_toute_la_cible_sans_lien_ni_en_tetes_avec_ou_sans_cle(bool avecCle)
        {
            var consentant = await CreerParentAsync(consenti: true, prenom: "Alix");
            var nonConsentant = await CreerParentAsync(prenom: "Sacha");
            var (service, _, envois) = Preparer(avecCle ? Jetons : CreerJetons(null));

            try
            {
                var resultat = await service.ExecuterAsync(ModeleDeLEcran(), DateTime.UtcNow);
                Assert.DoesNotContain("Clé de désabonnement absente", resultat);

                Assert.Contains(envois, e => e.Destinataire == consentant.Mail);
                var envoi = Assert.Single(envois, e => e.Destinataire == nonConsentant.Mail);
                Assert.Null(envoi.EnTetes);
                Assert.Contains("Sacha", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("/desabonnement", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("désinscrire", envoi.Valeurs["mentionPied"], StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Ne plus recevoir", envoi.Valeurs["mentionPied"]);
            }
            finally
            {
                await EffacerLeJournalAsync();
            }
        }

        [Fact]
        public async Task Case_cochee_ne_part_qu_aux_consentants_avec_lien_et_en_tetes()
        {
            var consentant = await CreerParentAsync(consenti: true, prenom: "Alix");
            var nonConsentant = await CreerParentAsync(prenom: "Sacha");
            var (service, _, envois) = Preparer(Jetons);

            try
            {
                await service.ExecuterAsync(ModeleDeLEcran(TexteAvecLien, promotion: true), DateTime.UtcNow);

                Assert.DoesNotContain(envois, e => e.Destinataire == nonConsentant.Mail);
                var envoi = Assert.Single(envois, e => e.Destinataire == consentant.Mail);
                Assert.NotNull(envoi.EnTetes);
                Assert.Contains("/desabonnement/un-clic?jeton=", envoi.EnTetes!["List-Unsubscribe"]);
                Assert.Contains("/desabonnement?jeton=", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("{{lien_desinscription}}", envoi.Valeurs["corpsMessage"]);
                Assert.Contains("Me désinscrire", envoi.Valeurs["mentionPied"]);
            }
            finally
            {
                await EffacerLeJournalAsync();
            }
        }

        [Fact]
        public async Task Case_cochee_sans_cle_rien_ne_part()
        {
            await CreerParentAsync(consenti: true);
            var (service, _, envois) = Preparer(CreerJetons(null));

            var resultat = await service.ExecuterAsync(ModeleDeLEcran(promotion: true), DateTime.UtcNow);

            Assert.Contains("Clé de désabonnement absente", resultat);
            Assert.Empty(envois);
        }

        [Fact]
        public async Task Variable_du_lien_sans_la_case_rien_ne_part()
        {
            // Le texte a pu recevoir `{{lien_desinscription}}` après la programmation : un modèle d'offre ne part pas à
            // toute la cible. L'envoi est manqué, et l'écran le dit.
            await CreerParentAsync(consenti: true);
            await CreerParentAsync();
            var (service, _, envois) = Preparer(Jetons);

            var resultat = await service.ExecuterAsync(ModeleDeLEcran(TexteAvecLien, promotion: false), DateTime.UtcNow);

            Assert.Contains("sans la case « Promotion »", resultat);
            Assert.Empty(envois);
        }

        // ------------------------------------------------------------ la programmation (écran → API)

        private static (ModelesMailController Controleur, Mock<IModeleMailRepository> Modeles) Controleur(ModeleMailDetail modele)
        {
            var modeles = new Mock<IModeleMailRepository>();
            modeles.Setup(m => m.GetDetailAsync(modele.Id, It.IsAny<CancellationToken>())).ReturnsAsync(modele);
            modeles.Setup(m => m.DefinirPlanificationAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                    It.IsAny<bool>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var diffusion = new DiffusionService(Mock.Of<IServiceScopeFactory>(), CreerJetons(null), NullLogger<DiffusionService>.Instance);
            var controleur = new ModelesMailController(
                modeles.Object, new ComposeurModeleMail(diffusion), diffusion,
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail { UrlSite = "https://site.exemple.invalid" }),
                NullLogger<ModelesMailController>.Instance);

            return (controleur, modeles);
        }

        private static ModelesMailController.PlanificationRequest Regle(bool promotion, bool actif = true) => new()
        {
            Actif = actif,
            Frequence = FrequenceEnvoi.Jour,
            Heure = "10:00",
            Cible = CibleEnvoi.Tous,
            Promotion = promotion,
        };

        private static void VerifierPromotionEnregistree(Mock<IModeleMailRepository> modeles, int id, bool promotion) =>
            modeles.Verify(m => m.DefinirPlanificationAsync(
                id, It.IsAny<string>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<bool>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), promotion, It.IsAny<CancellationToken>()), Times.Once);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task La_programmation_enregistre_la_case_promotion(bool promotion)
        {
            var (controleur, modeles) = Controleur(ModeleDeLEcran());

            Assert.IsType<OkObjectResult>(await controleur.Planifier(IdModele, Regle(promotion), CancellationToken.None));
            VerifierPromotionEnregistree(modeles, IdModele, promotion);
        }

        [Fact]
        public async Task La_case_est_ignoree_pour_une_relance()
        {
            var relance = ModeleDeLEcran();
            relance.Code = CodeModeleMail.Relance;
            var (controleur, modeles) = Controleur(relance);

            Assert.IsType<OkObjectResult>(await controleur.Planifier(IdModele, Regle(promotion: true), CancellationToken.None));
            VerifierPromotionEnregistree(modeles, IdModele, promotion: false);
        }

        [Fact]
        public async Task Programmer_un_modele_d_offre_sans_la_case_est_refuse()
        {
            var (controleur, modeles) = Controleur(ModeleDeLEcran(TexteAvecLien));

            var refus = Assert.IsType<BadRequestObjectResult>(
                await controleur.Planifier(IdModele, Regle(promotion: false), CancellationToken.None));
            Assert.Contains("Cochez « Promotion »", (string)refus.Value!.GetType().GetProperty("message")!.GetValue(refus.Value)!);
            modeles.Verify(m => m.DefinirPlanificationAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeOnly?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<bool>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);

            // Cochée, il se programme ; et on peut toujours l'éteindre sans cocher.
            Assert.IsType<OkObjectResult>(await controleur.Planifier(IdModele, Regle(promotion: true), CancellationToken.None));
            Assert.IsType<OkObjectResult>(await controleur.Planifier(IdModele, Regle(promotion: false, actif: false), CancellationToken.None));
        }
    }
}
