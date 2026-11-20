using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// L'INFORMATION DE SERVICE (décision de Min's, 19/11/2026) : à TOUS les parents joignables, consentement aux offres
    /// ou non, sans lien de désinscription ; la diffusion des offres, elle, reste aux seuls consentants. Le décompte de
    /// l'écran est la liste d'envoi, et l'envoi exige la confirmation « aucune offre » de l'administrateur.
    /// </summary>
    public class InformationServiceTests : BaseDeTest
    {
        private sealed record Envoi(
            string Destinataire, string Sujet, IDictionary<string, string> Valeurs, IReadOnlyDictionary<string, string>? EnTetes);

        /// <summary>Une session d'envoi qui n'envoie rien : elle note ce qui serait parti.</summary>
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
                lock (envois) envois.Add(new Envoi(destinataire, sujet, new Dictionary<string, string>(valeurs), enTetes));
                Envoyes++;
                return Task.FromResult(true);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        /// <summary>Le vrai service de diffusion, branché sur la base de TEST et sur une session factice.</summary>
        private (DiffusionService Service, List<Envoi> Envois) Diffusion(JetonDesabonnement jetons)
        {
            var envois = new List<Envoi>();
            var email = new Mock<IServiceEmail>();
            email.Setup(e => e.OuvrirSession()).Returns(() => new SessionFactice(envois));

            var services = new ServiceCollection();
            services.AddScoped<IEnvoiAutomatiqueRepository>(_ => new EnvoiAutomatiqueRepository(NouveauContexte()));
            services.AddSingleton(email.Object);
            var fournisseur = services.BuildServiceProvider();

            return (new DiffusionService(
                fournisseur.GetRequiredService<IServiceScopeFactory>(), jetons, NullLogger<DiffusionService>.Instance), envois);
        }

        private static async Task<EtatDiffusion> AttendreFinAsync(DiffusionService service)
        {
            var limite = DateTime.UtcNow.AddSeconds(60);
            while (service.Etat.EnCours && DateTime.UtcNow < limite) await Task.Delay(50);
            return service.Etat;
        }

        private async Task AncienDesabonnementAnnoncesAsync(int parentId)
        {
            await using var c = NouveauContexte();
            c.DesabonnementsMail.Add(new DesabonnementMail
            {
                ParentId = parentId, Categorie = CategorieDesabonnement.Diffusion, DateCreation = DateTime.UtcNow.AddMonths(-2),
            });
            await c.SaveChangesAsync();
        }

        private static int Nombre(IActionResult r)
        {
            var valeur = Assert.IsType<OkObjectResult>(r).Value!;
            return (int)valeur.GetType().GetProperty("nombre")!.GetValue(valeur)!;
        }

        [Fact]
        public async Task Information_de_service_a_tous_les_joignables_offres_aux_seuls_consentants()
        {
            var consentant = await CreerParentAsync(consenti: true);
            var nonConsentant = await CreerParentAsync();
            var ancienDesabonne = await CreerParentAsync();
            await AncienDesabonnementAnnoncesAsync(ancienDesabonne.Id);
            var desabonneAvis = await CreerParentAsync();
            await Envois.DesabonnerAsync(desabonneAvis.Id, CategorieDesabonnement.Avis, SourceConsentement.EspaceParent);
            var banni = await CreerParentAsync(consenti: true);
            await BannirAsync(banni.Mail!);
            var sansAdresse = await CreerParentAsync(consenti: true);
            await Contexte.Parents.Where(p => p.Id == sansAdresse.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Mail, ""));

            var service = (await Envois.GetDestinatairesAsync(ModeDiffusion.Service)).Select(d => d.ParentId).ToList();
            Assert.Contains(consentant.Id, service);
            Assert.Contains(nonConsentant.Id, service);
            Assert.Contains(ancienDesabonne.Id, service);
            Assert.Contains(desabonneAvis.Id, service);
            Assert.DoesNotContain(banni.Id, service);
            Assert.DoesNotContain(sansAdresse.Id, service);

            var offres = (await Envois.GetDestinatairesAsync(ModeDiffusion.Offres)).Select(d => d.ParentId).ToList();
            Assert.Contains(consentant.Id, offres);
            Assert.DoesNotContain(nonConsentant.Id, offres);
            Assert.DoesNotContain(ancienDesabonne.Id, offres);
            Assert.DoesNotContain(desabonneAvis.Id, offres);
            Assert.DoesNotContain(banni.Id, offres);
            Assert.DoesNotContain(sansAdresse.Id, offres);

            // Les offres sont un sous-ensemble de l'information de service.
            Assert.Empty(offres.Except(service));
        }

        [Fact]
        public async Task Le_decompte_de_l_ecran_est_la_liste_d_envoi_dans_chaque_mode()
        {
            await CreerParentAsync(consenti: true);
            await CreerParentAsync();

            // L'action ne lit aucun champ du contrôleur : pas besoin de ses dizaines de dépendances.
            var admin = (AdminController)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AdminController));

            Assert.Equal((await Envois.GetDestinatairesInformationServiceAsync()).Count,
                Nombre(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "service")));
            Assert.Equal((await Envois.GetDestinatairesDiffusionAsync()).Count,
                Nombre(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "offres")));
            Assert.Equal((await Envois.GetDestinatairesDiffusionAsync()).Count,
                Nombre(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None)));
            Assert.IsType<BadRequestObjectResult>(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "promo"));
        }

        [Theory]
        [InlineData(null, false, "offres", false)]
        [InlineData("offres", false, "offres", false)]
        [InlineData("service", false, "service", true)]
        [InlineData("SERVICE", true, "service", false)]
        [InlineData("promo", true, "", true)]
        public void Le_mode_service_exige_la_confirmation_non_commerciale(
            string? mode, bool confirmation, string attendu, bool refuse)
        {
            var faute = ModelesMailController.VerifierModeDiffusion(mode, confirmation, out var modeLu);

            Assert.Equal(attendu, modeLu);
            Assert.Equal(refuse, faute is not null);
        }

        [Fact]
        public async Task L_envoi_d_un_template_en_service_sans_confirmation_est_refuse_et_rien_ne_part()
        {
            var modeles = new Mock<IModeleMailRepository>();
            var (diffusion, envois) = Diffusion(Jetons);
            var controleur = new ModelesMailController(
                modeles.Object, new ComposeurModeleMail(diffusion), diffusion,
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ModelesMailController>.Instance);

            var reponse = await controleur.Diffuser(1, CancellationToken.None, "service", confirmationNonCommerciale: false);

            Assert.IsType<BadRequestObjectResult>(reponse);
            Assert.False(diffusion.Etat.EnCours);
            Assert.Null(diffusion.Etat.Mode);
            Assert.Empty(envois);
            modeles.Verify(m => m.GetDetailAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task L_information_de_service_part_aux_non_consentants_sans_lien_ni_en_tetes_meme_sans_cle()
        {
            var consentant = await CreerParentAsync(consenti: true, prenom: "Alix");
            var nonConsentant = await CreerParentAsync(prenom: "Sacha");

            // SANS CLÉ DE DÉSABONNEMENT : une offre ne partirait pas ; une information de service, si.
            var (diffusion, envois) = Diffusion(CreerJetons(null));

            Assert.True(diffusion.Lancer(
                "Évolution des prix, {{prenom}}", "Nos prix changent",
                "Bonjour {{prenom}},\n\nNos prix changent le 1er janvier.\n\nLien : {{lien_desinscription}}",
                [], [], ModeDiffusion.Service));

            var etat = await AttendreFinAsync(diffusion);
            Assert.Null(etat.Erreur);
            Assert.Equal(ModeDiffusion.Service, etat.Mode);

            var mesEnvois = envois.Where(e => e.Destinataire == consentant.Mail || e.Destinataire == nonConsentant.Mail).ToList();
            Assert.Equal(2, mesEnvois.Count);

            foreach (var envoi in mesEnvois)
            {
                Assert.Null(envoi.EnTetes);
                Assert.Contains("aucune offre commerciale", envoi.Valeurs["mentionPied"]);
                Assert.DoesNotContain("désinscrire", envoi.Valeurs["mentionPied"], StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Ne plus recevoir", envoi.Valeurs["mentionPied"]);
                Assert.DoesNotContain("{{lien_desinscription}}", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("desabonnement", envoi.Valeurs["corpsMessage"], StringComparison.OrdinalIgnoreCase);
            }

            Assert.Equal("Évolution des prix, Sacha", mesEnvois.Single(e => e.Destinataire == nonConsentant.Mail).Sujet);

            // LE RENDU RÉEL dans le gabarit embarqué « diffusion » (20/11/2026). Avec `MIMIA_TEST_RENDU=<dossier>`, le HTML
            // y est écrit (capture du compte rendu).
            var aSacha = mesEnvois.Single(e => e.Destinataire == nonConsentant.Mail);
            var html = await new ServiceEmail(
                    Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ServiceEmail>.Instance)
                .RendreAsync(aSacha.Sujet, "diffusion", aSacha.Valeurs);

            Assert.Contains("Bonjour Sacha", html);
            Assert.Contains("aucune offre commerciale", html);
            Assert.DoesNotContain("desabonnement", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{{", html);

            var dossier = Environment.GetEnvironmentVariable("MIMIA_TEST_RENDU");
            if (!string.IsNullOrWhiteSpace(dossier))
            {
                await File.WriteAllTextAsync(Path.Combine(dossier, "information-service.html"), html);
            }
        }

        [Fact]
        public async Task La_diffusion_des_offres_ne_part_qu_aux_consentants_avec_lien_et_en_tetes()
        {
            var consentant = await CreerParentAsync(consenti: true);
            var nonConsentant = await CreerParentAsync();
            var (diffusion, envois) = Diffusion(Jetons);

            Assert.True(diffusion.Lancer("Notre offre", "Notre offre", "Bonjour {{prenom}} !", [], []));

            var etat = await AttendreFinAsync(diffusion);
            Assert.Null(etat.Erreur);
            Assert.Equal(ModeDiffusion.Offres, etat.Mode);

            Assert.DoesNotContain(envois, e => e.Destinataire == nonConsentant.Mail);
            var envoi = Assert.Single(envois, e => e.Destinataire == consentant.Mail);
            Assert.NotNull(envoi.EnTetes);
            Assert.Contains("Me désinscrire", envoi.Valeurs["mentionPied"]);
        }

        [Fact]
        public async Task Sans_cle_la_diffusion_des_offres_ne_part_pas()
        {
            await CreerParentAsync(consenti: true);
            var (diffusion, envois) = Diffusion(CreerJetons(null));

            Assert.True(diffusion.Lancer("Notre offre", "Notre offre", "Bonjour", [], [], ModeDiffusion.Offres));

            var etat = await AttendreFinAsync(diffusion);
            Assert.NotNull(etat.Erreur);
            Assert.Empty(envois);
        }
    }
}
