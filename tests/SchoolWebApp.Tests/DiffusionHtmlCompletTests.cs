using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// LE MODE « HTML COMPLET » DU MESSAGE DE DIFFUSION (Min's, 24/11/2026) : le HTML collé (mail Black Friday de Slimane)
    /// part tel quel, sans le gabarit blanc, mais avec tout ce que le gabarit garantissait — lien de désinscription signé,
    /// `List-Unsubscribe`, envoi aux seuls consentants si « Promotion », pied légal si la balise manque, images du site en
    /// `cid:`. Le HTML est nettoyé (script, `on…=`, `javascript:`).
    ///
    /// AUCUNE BASE, AUCUN ENVOI : la liste des destinataires vient d'un dépôt simulé, la session d'envoi note ce qui
    /// serait parti. `MIMIA_TEST_HTML=<fichier>` remplace la maquette d'essai par un vrai HTML (preuve sur le mail de
    /// Slimane) ; `MIMIA_TEST_RENDU=<dossier>` y écrit le HTML généré pour un destinataire et l'aperçu.
    /// </summary>
    public class DiffusionHtmlCompletTests
    {
        private const string Cle = "cle-de-test-uniquement-0123456789";

        /// <summary>Une maquette sombre réduite, construite comme celle de Slimane (pied, balise en href, SIRET, logo du site).</summary>
        private const string Maquette =
            "<!DOCTYPE html><html lang=\"fr\"><head><meta charset=\"utf-8\"><title>Black Friday</title>"
            + "<!-- Note de Slimane : {{lien_desinscription}} est remplacé par le serveur. -->"
            + "<style>body{background:#0b0b0f}</style></head>"
            + "<body style=\"margin:0;background:#0b0b0f\">"
            + "<div style=\"display:none;max-height:0;overflow:hidden\">Offre valable jusqu'au samedi 28/11.</div>"
            + "<!--[if mso]><table><tr><td><![endif]-->"
            + "<img src=\"https://mimia.fr/email/logo-sombre.png\" width=\"140\" alt=\"mimia.fr\">"
            + "<h1 style=\"color:#fff\">Bonjour {{prenom}}, -50 % sur le 1er mois</h1>"
            + "<a href=\"https://mimia.fr/tarifs?utm_source=email&amp;utm_content=bouton\">Voir les formules</a>"
            + "<img src=\"https://mimia.fr/email/inconnue.png\" alt=\"\">"
            + "<p>Vous recevez cet email parce que vous avez accepté de recevoir les offres de Mimia. "
            + "<a href=\"{{lien_desinscription}}\" style=\"color:#ffe199\">Se désinscrire en un clic</a>.</p>"
            + "<p>Mins Corp (entrepreneur individuel), Évry-Courcouronnes, SIRET&nbsp;924&nbsp;291&nbsp;800&nbsp;00010.</p>"
            + "<!--[if mso]></td></tr></table><![endif]-->"
            + "</body></html>";

        private static string MaquetteOuFichier() =>
            Environment.GetEnvironmentVariable("MIMIA_TEST_HTML") is { Length: > 0 } chemin
                ? File.ReadAllText(chemin)
                : Maquette;

        private static string EnHtmlComplet(string html) => html + "\n" + HtmlComplet.Marqueur;

        private static DiffusionService Service(JetonDesabonnement? jetons = null, IServiceScopeFactory? scopes = null) =>
            new(scopes ?? Mock.Of<IServiceScopeFactory>(), jetons ?? Jetons(Cle), NullLogger<DiffusionService>.Instance);

        private static JetonDesabonnement Jetons(string? cle) =>
            new(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Courrier:CleDesabonnement"] = cle,
                        ["Courrier:UrlApi"] = "https://mimia.fr",
                    }).Build(),
                Mock.Of<Microsoft.Extensions.Hosting.IHostEnvironment>(h => h.EnvironmentName == "Production"),
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail { UrlSite = "https://mimia.fr" }),
                NullLogger<JetonDesabonnement>.Instance);

        private static ServiceEmail Email() =>
            new(Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ServiceEmail>.Instance);

        private static void Ecrire(string nom, string html)
        {
            var dossier = Environment.GetEnvironmentVariable("MIMIA_TEST_RENDU");
            if (!string.IsNullOrWhiteSpace(dossier)) File.WriteAllText(Path.Combine(dossier, nom), html);
        }

        private sealed record Envoi(
            string Destinataire, string Sujet, string Gabarit, IDictionary<string, string> Valeurs,
            IReadOnlyDictionary<string, string>? EnTetes);

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
                lock (envois) envois.Add(new Envoi(destinataire, sujet, gabarit, new Dictionary<string, string>(valeurs), enTetes));
                Envoyes++;
                return Task.FromResult(true);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        /// <summary>Le vrai service, une liste simulée par mode (rien en base), une session qui n'envoie rien.</summary>
        private static (DiffusionService Service, List<Envoi> Envois) Diffusion()
        {
            var envois = new List<Envoi>();
            var email = new Mock<IServiceEmail>();
            email.Setup(e => e.OuvrirSession()).Returns(() => new SessionFactice(envois));

            var depot = new Mock<IEnvoiAutomatiqueRepository>();
            depot.Setup(d => d.GetDestinatairesAsync(ModeDiffusion.Offres, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new DestinataireAutomatique(901, "consentant@exemple.invalid", "Léa")]);
            depot.Setup(d => d.GetDestinatairesAsync(ModeDiffusion.Information, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new DestinataireAutomatique(901, "consentant@exemple.invalid", "Léa"),
                    new DestinataireAutomatique(902, "autre@exemple.invalid", "Sacha"),
                ]);

            var services = new ServiceCollection();
            services.AddScoped(_ => depot.Object);
            services.AddSingleton(email.Object);

            return (Service(scopes: services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>()), envois);
        }

        private static async Task<EtatDiffusion> AttendreFinAsync(DiffusionService service)
        {
            var limite = DateTime.UtcNow.AddSeconds(30);
            while (service.Etat.EnCours && DateTime.UtcNow < limite) await Task.Delay(20);
            return service.Etat;
        }

        // ------------------------------------------------------------------ le marqueur

        [Fact]
        public void Le_marqueur_en_fin_de_texte_signe_le_mode_et_se_retire()
        {
            Assert.True(HtmlComplet.Est("<p>a</p>\n" + HtmlComplet.Marqueur + "\r\n  "));
            Assert.Equal("<p>a</p>", HtmlComplet.Contenu("<p>a</p>\n" + HtmlComplet.Marqueur));

            // Le mode texte ne bascule jamais par hasard : marqueur absent ou ailleurs qu'à la fin.
            Assert.False(HtmlComplet.Est("Bonjour,\n\n**Gras**"));
            Assert.False(HtmlComplet.Est(HtmlComplet.Marqueur + "\n<p>a</p>"));
            Assert.False(HtmlComplet.Est(null));
            Assert.Equal("Bonjour", HtmlComplet.Contenu("Bonjour"));
        }

        // ------------------------------------------------------------------ nettoyage

        [Theory]
        [InlineData("<p>a</p><script>alert(1)</script><p>b</p>", "<p>a</p><p>b</p>")]
        [InlineData("<p>a</p><SCRIPT type=\"text/javascript\">x</SCRIPT >", "<p>a</p>")]
        [InlineData("<p>a<script src=\"//x\"></p>", "<p>a</p>")]
        [InlineData("<scr<script>x</script>ipt>alert(1)</script>", "alert(1)")]
        [InlineData("<iframe src=\"https://x\"></iframe><p>a</p>", "<p>a</p>")]
        [InlineData("<img src=\"x.png\" onerror=\"alert(1)\" alt=\"\">", "<img src=\"x.png\" alt=\"\">")]
        [InlineData("<a href=\"#\" OnClick='x()' onmouseover=y>a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<a href=\"javascript:alert(1)\">a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<a href=\" JaVaScRiPt:alert(1)\">a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<a href=\"java&#x09;script:alert(1)\">a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<a href=\"&#106;avascript:alert(1)\">a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<a href=javascript:alert(1)>a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<a href=\"vbscript:x\">a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<a href=\"data:text/html;base64,PHNjcmlwdD4=\">a</a>", "<a href=\"#\">a</a>")]
        [InlineData("<meta http-equiv=\"refresh\" content=\"0;url=https://x\"><p>a</p>", "<p>a</p>")]
        [InlineData("<p>a</p><!-- note interne --><p>b</p>", "<p>a</p><p>b</p>")]
        public void Le_html_colle_est_nettoye(string colle, string attendu) =>
            Assert.Equal(attendu, HtmlComplet.Nettoyer(colle));

        [Fact]
        public void Le_nettoyage_garde_la_maquette_intacte()
        {
            // Une maquette d'e-mail ordinaire ne perd rien : styles en ligne, liens https, mailto, commentaires d'Outlook.
            const string propre =
                "<table role=\"presentation\" style=\"font-family:'Outfit',Arial;color:#fff\"><tr><td>"
                + "<!--[if mso]><v:roundrect><![endif]--><!--[if !mso]><!--><span>x</span><!--<![endif]-->"
                + "<a href=\"https://mimia.fr/tarifs?a=1&amp;b=2\" style=\"color:#ffe199\">Voir</a> "
                + "<a href=\"mailto:support@mimia.fr\">support</a><img src=\"https://mimia.fr/email/nora.png\" alt=\"Nora\">"
                + "</td></tr></table>";
            Assert.Equal(propre, HtmlComplet.Nettoyer(propre));
        }

        // ------------------------------------------------------------------ composition

        [Fact]
        public void Promotion_le_lien_signe_remplace_la_balise_et_le_pied_de_la_maquette_n_est_pas_double()
        {
            const string lien = "https://mimia.fr/desabonnement?jeton=abc.def&x=1";
            var variables = new Dictionary<string, ValeurVariable> { ["prenom"] = new("<b>Léa</b>") };
            DiffusionService.AjouterLienDesinscription(variables, lien);

            var html = Service().ComposerHtmlComplet(EnHtmlComplet(Maquette), variables);

            // La balise en href devient l'ADRESSE signée (échappée), jamais un lien imbriqué dans un lien.
            Assert.Contains("<a href=\"https://mimia.fr/desabonnement?jeton=abc.def&amp;x=1\" style=\"color:#ffe199\">", html);
            Assert.DoesNotContain("{{", html);

            // Le pied de la maquette porte balise et SIRET : rien n'est ajouté, la mention n'apparaît qu'une fois.
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "SIRET"));
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "désinscrire", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
            Assert.DoesNotContain("accepté de recevoir les offres et nouveautés", html);

            // Le prénom saisi par un parent est échappé ; la note de la maquette (commentaire) est partie.
            Assert.Contains("Bonjour &lt;b&gt;L&#233;a&lt;/b&gt;,", html);
            Assert.DoesNotContain("Note de Slimane", html);

            // Le logo du site part en pièce liée ; une image inconnue de l'assembly garde son adresse.
            Assert.Contains("src=\"cid:mimia-email-logo-sombre\"", html);
            Assert.Contains("src=\"https://mimia.fr/email/inconnue.png\"", html);

            // La maquette garde son pré-entête, son style, ses commentaires d'Outlook, et AUCUNE trace du gabarit blanc.
            Assert.Contains("Offre valable jusqu'au samedi 28/11.", html);
            Assert.Contains("<!--[if mso]>", html);
            Assert.DoesNotContain("cid:logoMimia", html);
            Assert.DoesNotContain(HtmlComplet.Marqueur, html);
        }

        [Fact]
        public void Promotion_sans_la_balise_le_pied_legal_complet_est_ajoute_une_fois_avant_la_fin_du_corps()
        {
            var variables = new Dictionary<string, ValeurVariable>();
            DiffusionService.AjouterLienDesinscription(variables, "https://mimia.fr/d?j=1");

            var html = Service().ComposerHtmlComplet(
                EnHtmlComplet("<html><body><h1>Offre</h1><!-- {{lien_desinscription}} dans une note ne compte pas --></body></html>"),
                variables);

            Assert.Contains("accepté de recevoir les offres et nouveautés", html);
            Assert.Contains(WebUtility.HtmlEncode(DiffusionService.MentionIdentite), html);
            Assert.Contains("href=\"https://mimia.fr/d?j=1\"", html);
            Assert.EndsWith("</td></tr></table></body></html>", html);
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "SIRET"));
        }

        [Fact]
        public void Promotion_avec_la_balise_mais_sans_identite_seule_l_identite_est_ajoutee()
        {
            var variables = new Dictionary<string, ValeurVariable>();
            DiffusionService.AjouterLienDesinscription(variables, "https://mimia.fr/d?j=1");

            var html = Service().ComposerHtmlComplet(
                EnHtmlComplet("<p>Offre</p><p>{{lien_desinscription}}</p>"), variables);

            // Dans le texte (hors balise), la variable devient un lien « Se désinscrire ».
            Assert.Contains("<p><a href=\"https://mimia.fr/d?j=1\" style=\"color:inherit; text-decoration:underline;\">Se désinscrire</a></p>", html);
            Assert.Contains(WebUtility.HtmlEncode(DiffusionService.MentionIdentite), html);
            Assert.DoesNotContain("accepté de recevoir", html);
        }

        [Fact]
        public void Information_le_html_part_tel_quel_sans_pied_ajoute()
        {
            var variables = new Dictionary<string, ValeurVariable> { ["prenom"] = new(string.Empty) };
            DiffusionService.RetirerLienDesinscription(variables);

            var html = Service().ComposerHtmlComplet(EnHtmlComplet("<html><body><p>Bonjour {{prenom}}, nouveauté.</p></body></html>"), variables);

            Assert.Equal("<html><body><p>Bonjour, nouveauté.</p></body></html>", html);
        }

        // ------------------------------------------------------------------ bout en bout sans envoi

        [Fact]
        public async Task La_promotion_en_html_complet_part_aux_seuls_consentants_avec_lien_signe_et_List_Unsubscribe()
        {
            var jetons = Jetons(Cle);
            var (diffusion, envois) = Diffusion();

            Assert.True(diffusion.Lancer(
                "Black Friday : -50 %", "ignoré", EnHtmlComplet(MaquetteOuFichier()), [], [], ModeDiffusion.Offres));
            var etat = await AttendreFinAsync(diffusion);
            Assert.Null(etat.Erreur);

            // La liste des OFFRES (un consentant), pas celle de l'information (deux).
            var envoi = Assert.Single(envois);
            Assert.Equal("consentant@exemple.invalid", envoi.Destinataire);

            // L'en-tête List-Unsubscribe du parent, et SON lien signé dans le HTML.
            Assert.NotNull(envoi.EnTetes);
            Assert.Contains("List-Unsubscribe", envoi.EnTetes!.Keys);
            var lienSigne = jetons.Lien(901, CategorieDesabonnement.Diffusion)!;
            var html = envoi.Valeurs[HtmlComplet.Cle];
            Assert.Contains($"href=\"{WebUtility.HtmlEncode(lienSigne)}\"", html);
            Assert.Contains($"<{lienSigne}>", envoi.EnTetes["List-Unsubscribe"]);
            Assert.Equal("List-Unsubscribe=One-Click", envoi.EnTetes["List-Unsubscribe-Post"]);

            // CE QUI PART VRAIMENT (ServiceEmail, sans SMTP) : le HTML composé, sans l'enveloppe blanche du site.
            var compose = await Email().ComposerAsync(envoi.Sujet, envoi.Gabarit, envoi.Valeurs);
            Assert.Equal(html, compose);
            Assert.DoesNotContain("le professeur particulier de vos enfants", compose);
            Assert.DoesNotContain("cid:logoMimia", compose);
            Assert.Contains("cid:mimia-email-logo-sombre", compose);

            // La version texte (exigée par les filtres) ne garde ni style ni pré-entête.
            var texte = TexteBrut.Depuis(compose);
            Assert.DoesNotContain("<", texte);
            Assert.DoesNotContain("{", texte);

            // Les pièces liées : le logo du site est joint, la bannière blanche ne l'est pas.
            var constructeur = new BodyBuilder { HtmlBody = compose };
            HtmlComplet.AttacherImagesDuSite(constructeur);
            var liees = constructeur.LinkedResources.Select(r => r.ContentId).ToList();
            Assert.Contains("mimia-email-logo-sombre", liees);
            Assert.DoesNotContain("logoMimia", liees);
            Assert.All(liees, cid => Assert.StartsWith(HtmlComplet.PrefixeCid, cid));
            Assert.Equal(liees.Count, liees.Distinct().Count());

            Ecrire("html-complet-genere-destinataire.html", compose);
            Ecrire("html-complet-lien-signe.txt", $"Destinataire fictif : parent 901\nLien signé : {lienSigne}\nList-Unsubscribe : {envoi.EnTetes["List-Unsubscribe"]}\n");
        }

        [Fact]
        public async Task L_apercu_de_l_ecran_montre_le_html_tel_quel_images_du_site_comprises()
        {
            var admin = new AdminController(
                Mock.Of<SchoolWebApp.Domain.Services.IAdminService>(),
                NullLogger<AdminController>.Instance,
                new SchoolWebApp.Api.Services.ComptesProteges(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()),
                Microsoft.Extensions.Options.Options.Create(new SchoolWebApp.Api.Services.Paiement.ExemptionFacturation()));

            var resultat = await admin.ApercuDiffusion(
                "Black Friday", string.Empty, EnHtmlComplet(MaquetteOuFichier()), Service(), Email(),
                Mock.Of<SchoolWebApp.Api.Services.IBoitesAdminService>(), mode: "offres");

            var html = Assert.IsType<ContentResult>(resultat).Content!;
            Assert.DoesNotContain("le professeur particulier de vos enfants", html);
            Assert.DoesNotContain("cid:", html);
            Assert.Contains("data:image/png;base64,", html);
            Assert.DoesNotContain("{{lien_desinscription}}", html);

            Ecrire("html-complet-apercu.html", html);
        }

        [Fact]
        public async Task Un_template_html_complet_envoye_comme_information_avec_la_balise_est_refuse()
        {
            var modeles = new Mock<IModeleMailRepository>();
            modeles.Setup(m => m.GetDetailAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new ModeleMailDetail
            {
                Id = 7, Nature = NatureModeleMail.Diffusion, Sujet = "Black Friday", Texte = EnHtmlComplet(Maquette),
            });
            modeles.Setup(m => m.GetPiecesAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            var (diffusion, envois) = Diffusion();
            var controleur = new ModelesMailController(modeles.Object, new ComposeurModeleMail(diffusion), diffusion,
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ModelesMailController>.Instance);

            Assert.IsType<BadRequestObjectResult>(await controleur.Diffuser(7, CancellationToken.None));
            Assert.Empty(envois);
            Assert.False(diffusion.Etat.EnCours);
        }

        [Fact]
        public void Le_composeur_de_template_rend_le_meme_html_que_la_diffusion()
        {
            var variables = new Dictionary<string, ValeurVariable> { ["prenom"] = new("Camille") };
            DiffusionService.AjouterLienDesinscription(variables, "#");
            var service = Service();
            var detail = new ModeleMailDetail { Nature = NatureModeleMail.Diffusion, Sujet = "BF", Texte = EnHtmlComplet(Maquette) };

            var courriel = new ComposeurModeleMail(service).Composer(detail, [], variables);

            Assert.Equal(service.ComposerHtmlComplet(detail.Texte, variables), courriel.Valeurs[HtmlComplet.Cle]);
            Assert.Equal(string.Empty, courriel.Valeurs["corpsMessage"]);
        }

        [Fact]
        public void Le_mode_texte_ne_change_pas()
        {
            // Sans le marqueur, le même HTML collé reste du TEXTE échappé, et aucune clé « htmlComplet » n'est posée.
            var detail = new ModeleMailDetail { Nature = NatureModeleMail.Diffusion, Sujet = "BF", Texte = "<b>gras</b> **vrai gras**" };
            var courriel = new ComposeurModeleMail(Service()).Composer(detail, []);

            Assert.False(courriel.Valeurs.ContainsKey(HtmlComplet.Cle));
            Assert.Contains("&lt;b&gt;gras&lt;/b&gt; <strong>vrai gras</strong>", courriel.Valeurs["corpsMessage"]);
        }

        [Fact]
        public async Task Le_mode_texte_de_la_diffusion_garde_son_gabarit_et_son_pied()
        {
            // Non-régression de l'écran : un message texte (Promotion cochée) part comme avant, dans l'enveloppe du site,
            // avec le pied commercial, la bannière `cid:logoMimia` et l'en-tête List-Unsubscribe.
            var (diffusion, envois) = Diffusion();
            Assert.True(diffusion.Lancer("Offre", "Offre", "Bonjour {{prenom}},\n\n<b>-50 %</b>", [], [], ModeDiffusion.Offres));
            Assert.Null((await AttendreFinAsync(diffusion)).Erreur);

            var envoi = Assert.Single(envois);
            Assert.False(envoi.Valeurs.ContainsKey(HtmlComplet.Cle));
            Assert.Equal("diffusion", envoi.Gabarit);
            Assert.NotNull(envoi.EnTetes);
            Assert.Contains("Me désinscrire", envoi.Valeurs["mentionPied"]);

            var html = await Email().ComposerAsync(envoi.Sujet, envoi.Gabarit, envoi.Valeurs);
            Assert.Contains("le professeur particulier de vos enfants", html);
            Assert.Contains("cid:logoMimia", html);
            Assert.Contains("&lt;b&gt;-50 %&lt;/b&gt;", html);
            Assert.Contains("Bonjour L&#233;a,", html);
        }

        [Fact]
        public void Les_images_du_site_sont_embarquees()
        {
            var noms = typeof(HtmlComplet).Assembly.GetManifestResourceNames();
            Assert.Contains("SchoolWebApp.Domain.Emails.Ressources.email-logo-sombre.png", noms);
            Assert.Contains("SchoolWebApp.Domain.Emails.Ressources.email-nora.png", noms);
        }
    }
}
