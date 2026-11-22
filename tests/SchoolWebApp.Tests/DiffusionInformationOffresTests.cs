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
    /// LE « MESSAGE DE DIFFUSION » EST UNE INFORMATION À TOUS (décision de Min's, 20/11/2026) : par défaut il part à TOUS
    /// les parents joignables, consentement aux offres ou non, sans lien de désinscription. Avec la case « Promotion »
    /// cochée (`mode=offres`), il ne part qu'aux seuls consentants, avec lien et `List-Unsubscribe`. Le décompte de
    /// l'écran est la liste d'envoi.
    /// </summary>
    public class DiffusionInformationOffresTests : BaseDeTest
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

        private ModelesMailController Controleur(Mock<IModeleMailRepository> modeles, DiffusionService diffusion) =>
            new(modeles.Object, new ComposeurModeleMail(diffusion), diffusion,
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ModelesMailController>.Instance);

        private static Mock<IModeleMailRepository> Modele(int id, string texte, string? titre = null)
        {
            var modeles = new Mock<IModeleMailRepository>();
            modeles.Setup(m => m.GetDetailAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new ModeleMailDetail
            {
                Id = id, Nature = NatureModeleMail.Diffusion, Sujet = "Nouvelles de Mimia", Titre = titre ?? string.Empty, Texte = texte,
            });
            modeles.Setup(m => m.GetPiecesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            return modeles;
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
        public async Task Message_de_diffusion_a_tous_les_joignables_offres_aux_seuls_consentants()
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

            var information = (await Envois.GetDestinatairesAsync(ModeDiffusion.Information)).Select(d => d.ParentId).ToList();
            Assert.Contains(consentant.Id, information);
            Assert.Contains(nonConsentant.Id, information);
            Assert.Contains(ancienDesabonne.Id, information);
            Assert.Contains(desabonneAvis.Id, information);
            Assert.DoesNotContain(banni.Id, information);
            Assert.DoesNotContain(sansAdresse.Id, information);

            var offres = (await Envois.GetDestinatairesAsync(ModeDiffusion.Offres)).Select(d => d.ParentId).ToList();
            Assert.Contains(consentant.Id, offres);
            Assert.DoesNotContain(nonConsentant.Id, offres);
            Assert.DoesNotContain(ancienDesabonne.Id, offres);
            Assert.DoesNotContain(desabonneAvis.Id, offres);
            Assert.DoesNotContain(banni.Id, offres);
            Assert.DoesNotContain(sansAdresse.Id, offres);

            // Les offres sont un sous-ensemble du message de diffusion.
            Assert.Empty(offres.Except(information));
        }

        [Fact]
        public async Task Le_decompte_de_l_ecran_est_la_liste_d_envoi_dans_chaque_mode()
        {
            await CreerParentAsync(consenti: true);
            await CreerParentAsync();

            // L'action ne lit aucun champ du contrôleur : pas besoin de ses dizaines de dépendances.
            var admin = (AdminController)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AdminController));

            var tous = (await Envois.GetDestinatairesInformationAsync()).Count;
            var consentants = (await Envois.GetDestinatairesOffresAsync()).Count;
            Assert.True(tous > consentants);

            // PAR DÉFAUT, le décompte est celui du message de diffusion : tous les parents joignables.
            Assert.Equal(tous, Nombre(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None)));
            Assert.Equal(tous, Nombre(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "information")));
            Assert.Equal(consentants, Nombre(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "offres")));
            Assert.IsType<BadRequestObjectResult>(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "promo"));

            // L'ancien mode « service » (onglet fusionné dans « Message de diffusion ») n'existe plus.
            Assert.IsType<BadRequestObjectResult>(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "service"));
        }

        [Theory]
        [InlineData(null, "information", false)]
        [InlineData("information", "information", false)]
        [InlineData("OFFRES", "offres", false)]
        [InlineData("service", "", true)]
        [InlineData("promo", "", true)]
        public void Le_mode_se_lit_sans_aucune_confirmation(string? mode, string attendu, bool refuse)
        {
            var faute = ModelesMailController.VerifierModeDiffusion(mode, out var modeLu);

            Assert.Equal(attendu, modeLu);
            Assert.Equal(refuse, faute is not null);
        }

        [Fact]
        public async Task Un_template_part_a_tous_sans_mode_ni_case_a_cocher()
        {
            var consentant = await CreerParentAsync(consenti: true, prenom: "Alix");
            var nonConsentant = await CreerParentAsync(prenom: "Sacha");
            var modeles = Modele(5, "Bonjour {{prenom}},\n\nNouvelle matière disponible sur Mimia.");
            var (diffusion, envois) = Diffusion(Jetons);

            // Exactement l'appel de l'onglet « Message de diffusion » : ni mode, ni confirmation.
            Assert.IsType<AcceptedResult>(await Controleur(modeles, diffusion).Diffuser(5, CancellationToken.None));

            var etat = await AttendreFinAsync(diffusion);
            Assert.Null(etat.Erreur);
            Assert.Equal(ModeDiffusion.Information, etat.Mode);
            Assert.Contains(envois, e => e.Destinataire == consentant.Mail);
            Assert.Contains(envois, e => e.Destinataire == nonConsentant.Mail);
            modeles.Verify(m => m.NoterEnvoiAsync(5, It.IsAny<DateTime>(), "Diffusion à tous les parents", It.IsAny<CancellationToken>()));
        }

        [Theory]
        [InlineData("Bonjour {{prenom}}, profitez de -50 % : {{lien_desinscription}}", null, true)]
        [InlineData("Texte sans variable", "Titre {{lienDesinscription}}", true)]
        [InlineData("Texte {{ Lien_Desinscription }}", null, true)]
        [InlineData("Nos prix changent le 1er décembre, {{prenom}}.", "Nouveaux prix", false)]
        [InlineData("Le lien de désinscription n'existe pas ici.", null, false)]
        public async Task Un_modele_d_offre_envoye_comme_message_de_diffusion_est_renvoye_vers_offres(
            string texte, string? titre, bool refuse)
        {
            // Revue d'Omar du 20/11/2026 (mineur 5) : `{{lien_desinscription}}` = un modèle d'offre (Black Friday…). Envoyé
            // à TOUS, il est refusé avec un message qui dit où l'envoyer ; personne ne reçoit rien.
            await CreerParentAsync();
            var modeles = Modele(7, texte, titre);
            var (diffusion, envois) = Diffusion(Jetons);

            var reponse = await Controleur(modeles, diffusion).Diffuser(7, CancellationToken.None);

            if (refuse)
            {
                var refus = Assert.IsType<BadRequestObjectResult>(reponse);
                var message = (string)refus.Value!.GetType().GetProperty("message")!.GetValue(refus.Value)!;
                Assert.Contains("{{lien_desinscription}}", message);
                Assert.Contains("Cochez « Promotion »", message);
                Assert.False(diffusion.Etat.EnCours);
                Assert.Empty(envois);
                modeles.Verify(m => m.NoterEnvoiAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            }
            else
            {
                Assert.IsType<AcceptedResult>(reponse);
                await AttendreFinAsync(diffusion);
            }
        }

        [Fact]
        public async Task Le_meme_template_part_en_offres_avec_son_lien()
        {
            var modeles = Modele(8, "Profitez-en : {{lien_desinscription}}");
            var (diffusion, _) = Diffusion(Jetons);

            Assert.IsType<AcceptedResult>(await Controleur(modeles, diffusion).Diffuser(8, CancellationToken.None, "offres"));
            var etat = await AttendreFinAsync(diffusion);
            Assert.Equal(ModeDiffusion.Offres, etat.Mode);
            modeles.Verify(m => m.NoterEnvoiAsync(8, It.IsAny<DateTime>(), "Promotion aux parents qui acceptent les offres", It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Le_message_de_diffusion_part_aux_non_consentants_sans_lien_ni_en_tetes_meme_sans_cle()
        {
            var consentant = await CreerParentAsync(consenti: true, prenom: "Alix");
            var nonConsentant = await CreerParentAsync(prenom: "Sacha");

            // SANS CLÉ DE DÉSABONNEMENT : une offre ne partirait pas ; un message d'information, si.
            var (diffusion, envois) = Diffusion(CreerJetons(null));

            Assert.True(diffusion.Lancer(
                "Du nouveau sur Mimia, {{prenom}}", "Du nouveau",
                "Bonjour {{prenom}},\n\nLes fiches de révision arrivent.\n\nLien : {{lien_desinscription}}",
                [], []));

            var etat = await AttendreFinAsync(diffusion);
            Assert.Null(etat.Erreur);
            Assert.Equal(ModeDiffusion.Information, etat.Mode);

            var mesEnvois = envois.Where(e => e.Destinataire == consentant.Mail || e.Destinataire == nonConsentant.Mail).ToList();
            Assert.Equal(2, mesEnvois.Count);

            foreach (var envoi in mesEnvois)
            {
                Assert.Null(envoi.EnTetes);
                Assert.Contains("vous avez un compte Mimia", envoi.Valeurs["mentionPied"]);

                // Ni pied commercial, ni avertissement : l'outil d'information de Min's.
                Assert.DoesNotContain("offres", envoi.Valeurs["mentionPied"], StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("désinscrire", envoi.Valeurs["mentionPied"], StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Ne plus recevoir", envoi.Valeurs["mentionPied"]);
                Assert.DoesNotContain("SIRET", envoi.Valeurs["mentionPied"]);
                Assert.DoesNotContain("{{lien_desinscription}}", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("desabonnement", envoi.Valeurs["corpsMessage"], StringComparison.OrdinalIgnoreCase);
            }

            Assert.Equal("Du nouveau sur Mimia, Sacha", mesEnvois.Single(e => e.Destinataire == nonConsentant.Mail).Sujet);

            // LE RENDU RÉEL dans le gabarit embarqué « diffusion ». Avec `MIMIA_TEST_RENDU=<dossier>`, le HTML y est écrit
            // (capture du compte rendu).
            var aSacha = mesEnvois.Single(e => e.Destinataire == nonConsentant.Mail);
            var html = await new ServiceEmail(
                    Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ServiceEmail>.Instance)
                .RendreAsync(aSacha.Sujet, "diffusion", aSacha.Valeurs);

            Assert.Contains("Bonjour Sacha", html);
            Assert.DoesNotContain("desabonnement", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{{", html);

            var dossier = Environment.GetEnvironmentVariable("MIMIA_TEST_RENDU");
            if (!string.IsNullOrWhiteSpace(dossier))
            {
                await File.WriteAllTextAsync(Path.Combine(dossier, "message-de-diffusion.html"), html);
            }
        }

        [Fact]
        public async Task Offres_et_promotions_ne_part_qu_aux_consentants_avec_lien_et_en_tetes()
        {
            var consentant = await CreerParentAsync(consenti: true);
            var nonConsentant = await CreerParentAsync();
            var (diffusion, envois) = Diffusion(Jetons);

            Assert.True(diffusion.Lancer("Notre offre", "Notre offre", "Bonjour {{prenom}} !", [], [], ModeDiffusion.Offres));

            var etat = await AttendreFinAsync(diffusion);
            Assert.Null(etat.Erreur);
            Assert.Equal(ModeDiffusion.Offres, etat.Mode);

            Assert.DoesNotContain(envois, e => e.Destinataire == nonConsentant.Mail);
            var envoi = Assert.Single(envois, e => e.Destinataire == consentant.Mail);
            Assert.NotNull(envoi.EnTetes);
            Assert.Contains("Me désinscrire", envoi.Valeurs["mentionPied"]);
        }

        /// <summary>
        /// LE BOUTON ET LE TITRE (22/11/2026) : l'aperçu de l'écran et l'envoi réel rendent le MÊME bouton (même fonction
        /// `ComposerCorps`), et le Titre est échappé des deux côtés — prénom saisi par le parent compris.
        /// </summary>
        [Fact]
        public async Task L_apercu_et_l_envoi_rendent_le_meme_bouton_et_un_titre_echappe()
        {
            var parent = await CreerParentAsync(consenti: true, prenom: "<i>Léa</i>");
            const string texte = "Bonjour {{prenom}},\n\n-50 % sur la 1re année.\n\n[[Voir les formules]](https://mimia.fr/tarifs?a=1&b=2)";
            const string titre = "Black Friday <b>-50 %</b> & {{prenom}}";
            var bouton = DiffusionBoutonTests.BoutonAttendu("Voir les formules", "https://mimia.fr/tarifs?a=1&amp;b=2");

            // L'ENVOI.
            var (diffusion, envois) = Diffusion(Jetons);
            Assert.True(diffusion.Lancer("Black Friday", titre, texte, [], [], ModeDiffusion.Offres));
            Assert.Null((await AttendreFinAsync(diffusion)).Erreur);
            var envoi = Assert.Single(envois, e => e.Destinataire == parent.Mail);

            Assert.Contains(bouton, envoi.Valeurs["corpsMessage"]);
            Assert.Equal(
                "Black Friday &lt;b&gt;-50 %&lt;/b&gt; &amp; &lt;i&gt;L&#233;a&lt;/i&gt;", envoi.Valeurs["titreMessage"]);
            Assert.DoesNotContain("<i>", envoi.Valeurs["corpsMessage"]);

            // L'APERÇU de l'écran « Message de diffusion » (route `POST /admin/diffusion/apercu`).
            IDictionary<string, string>? apercu = null;
            var email = new Mock<IServiceEmail>();
            email.Setup(e => e.RendreAsync(It.IsAny<string>(), "diffusion", It.IsAny<IDictionary<string, string>>()))
                .Callback<string, string, IDictionary<string, string>>((_, _, v) => apercu = v)
                .ReturnsAsync("<html></html>");
            var admin = new AdminController(
                Mock.Of<SchoolWebApp.Domain.Services.IAdminService>(),
                NullLogger<AdminController>.Instance,
                new SchoolWebApp.Api.Services.ComptesProteges(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()),
                Microsoft.Extensions.Options.Options.Create(new SchoolWebApp.Api.Services.Paiement.ExemptionFacturation()));

            await admin.ApercuDiffusion(
                "Black Friday", "Black Friday <b>-50 %</b> & co", texte, diffusion, email.Object,
                Mock.Of<SchoolWebApp.Api.Services.IBoitesAdminService>(), mode: "offres");

            Assert.NotNull(apercu);
            Assert.Contains(bouton, apercu!["corpsMessage"]);
            Assert.Equal("Black Friday &lt;b&gt;-50 %&lt;/b&gt; &amp; co", apercu["titreMessage"]);

            // LE RENDU RÉEL dans le gabarit embarqué (capture du compte rendu avec `MIMIA_TEST_RENDU=<dossier>`).
            var html = await new ServiceEmail(
                    Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ServiceEmail>.Instance)
                .RendreAsync(envoi.Sujet, "diffusion", envoi.Valeurs);
            Assert.Contains(bouton, html);
            Assert.DoesNotContain("<i>L", html);

            var dossier = Environment.GetEnvironmentVariable("MIMIA_TEST_RENDU");
            if (!string.IsNullOrWhiteSpace(dossier))
            {
                await File.WriteAllTextAsync(Path.Combine(dossier, "message-avec-bouton.html"), html);
            }
        }

        [Fact]
        public async Task Sans_cle_offres_et_promotions_ne_part_pas()
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
