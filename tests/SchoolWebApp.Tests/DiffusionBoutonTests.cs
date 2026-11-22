using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// LE BOUTON DU MESSAGE DE DIFFUSION (feu vert de Min's, 22/11/2026) : un paragraphe qui ne contient QUE
    /// `[[texte]](https://…)` devient un bouton « à l'épreuve d'Outlook ». Rien ne change pour un texte sans doubles
    /// crochets ; une adresse qui n'est pas http(s) reste du texte. Le Titre et l'objet sont échappés dans le HTML.
    /// Aucun accès base : tout est en mémoire.
    /// </summary>
    public class DiffusionBoutonTests
    {
        private static JetonDesabonnement Jetons(string? urlApi) =>
            new(
                new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Courrier:CleDesabonnement"] = "cle-de-test-uniquement-0123456789",
                        ["Courrier:UrlApi"] = urlApi,
                    }).Build(),
                Mock.Of<Microsoft.Extensions.Hosting.IHostEnvironment>(h => h.EnvironmentName == "Production"),
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail { UrlSite = "https://mimia.fr" }),
                NullLogger<JetonDesabonnement>.Instance);

        private static DiffusionService Service() =>
            new(Mock.Of<IServiceScopeFactory>(), Jetons(null), NullLogger<DiffusionService>.Instance);

        /// <summary>Le bouton attendu, à l'octet près : c'est lui que l'aperçu ET l'envoi doivent produire.</summary>
        internal static string BoutonAttendu(string texteHtml, string adresseHtml) =>
            "<table role=\"presentation\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" "
            + "style=\"margin:6px auto 24px auto;\"><tr>"
            + "<td align=\"center\" bgcolor=\"#f5b301\" style=\"background-color:#f5b301; border-radius:12px;\">"
            + $"<a href=\"{adresseHtml}\" style=\"display:inline-block; padding:14px 28px; "
            + "font-family:-apple-system,'Segoe UI',Helvetica,Arial,sans-serif; font-size:16px; line-height:20px; "
            + "font-weight:700; color:#141414; text-decoration:none; border-radius:12px;\">"
            + $"{texteHtml}</a>"
            + "</td></tr></table>";

        [Fact]
        public void Un_paragraphe_qui_ne_contient_que_le_bouton_devient_un_bouton()
        {
            var html = Service().ComposerCorps(
                "Bonjour {{prenom}},\n\n  [[Voir les formules]](https://mimia.fr/tarifs?offre=bf&duree=12)  \n\nÀ bientôt",
                0,
                new Dictionary<string, ValeurVariable> { ["prenom"] = new("Léa") });

            // Le paragraphe entier est remplacé par le bouton (espaces autour tolérées), l'adresse échappée (&amp;).
            Assert.Contains(BoutonAttendu("Voir les formules", "https://mimia.fr/tarifs?offre=bf&amp;duree=12"), html);
            Assert.DoesNotContain("[[", html);
            Assert.DoesNotContain("<p style=\"margin:0 0 18px 0; font-size:15px; line-height:1.7; color:#16233a;\"><table", html);

            // Les autres paragraphes ne bougent pas.
            Assert.Contains(">Bonjour Léa,</p>", html.Replace("&#233;", "é"));
            Assert.Contains("bientôt</p>", System.Net.WebUtility.HtmlDecode(html));
            Assert.Equal(3, html.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);

            // Insensible à la casse du schéma, comme les liens nommés.
            Assert.Contains(BoutonAttendu("Go", "HTTPS://mimia.fr"), Service().ComposerCorps("[[Go]](HTTPS://mimia.fr)", 0));
        }

        [Fact]
        public void Une_adresse_non_http_ou_un_bouton_dans_une_phrase_reste_du_texte_et_rien_ne_change_sans_doubles_crochets()
        {
            var service = Service();

            // Adresse non http(s) : aucun bouton, aucun lien ; le texte reste lisible et échappé.
            foreach (var adresse in new[] { "javascript:alert(1)", "ftp://mimia.fr/a", "mailto:x@exemple.invalid", "//mimia.fr" })
            {
                var html = service.ComposerCorps($"[[Cliquez]]({adresse})", 0);
                Assert.DoesNotContain("<table", html);
                Assert.DoesNotContain("<a ", html);
                Assert.StartsWith("<p ", html);
                Assert.Contains("[[Cliquez]]", html);
            }

            // Le bouton doit être SEUL dans son paragraphe : au milieu d'une phrase, ou suivi d'un saut simple, pas de bouton.
            Assert.DoesNotContain("<table", service.ComposerCorps("Allez voir [[nos formules]](https://mimia.fr/tarifs) !", 0));
            Assert.DoesNotContain("<table", service.ComposerCorps("[[Voir]](https://mimia.fr)\nà bientôt", 0));

            // Texte vide ou trop long (> 80 caractères) : pas de bouton.
            Assert.DoesNotContain("<table", service.ComposerCorps("[[ ]](https://mimia.fr)", 0));
            Assert.DoesNotContain("<table", service.ComposerCorps($"[[{new string('a', 81)}]](https://mimia.fr)", 0));

            // RIEN NE CHANGE pour un modèle existant : un lien nommé seul dans son paragraphe reste un lien texte.
            Assert.Equal(
                "<p style=\"margin:0 0 18px 0; font-size:15px; line-height:1.7; color:#16233a;\">"
                + "<a href=\"https://mimia.fr/tarifs\" style=\"color:#e8603c; font-weight:600; text-decoration:underline;\">"
                + "Voir les formules</a></p>" + Environment.NewLine,
                service.ComposerCorps("[Voir les formules](https://mimia.fr/tarifs)", 0));
        }

        [Fact]
        public void Le_texte_du_bouton_est_echappe_variables_comprises()
        {
            var html = Service().ComposerCorps(
                "[[<b>Go</b> & {{prenom}}]](https://mimia.fr/tarifs)",
                0,
                new Dictionary<string, ValeurVariable> { ["prenom"] = new("<img src=x onerror=alert(1)>") });

            Assert.Contains(
                BoutonAttendu("&lt;b&gt;Go&lt;/b&gt; &amp; &lt;img src=x onerror=alert(1)&gt;", "https://mimia.fr/tarifs"), html);
            Assert.DoesNotContain("<b>", html);
            Assert.DoesNotContain("<img", html);
        }

        [Fact]
        public async Task Le_titre_de_la_page_est_echappe_dans_l_enveloppe()
        {
            var html = await new ServiceEmail(
                    Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<ServiceEmail>.Instance)
                .ComposerAsync("Black Friday <b>-50 %</b> & plus", "diffusion", new Dictionary<string, string>
                {
                    ["titreMessage"] = "Titre",
                    ["corpsMessage"] = "<p>Corps</p>",
                });

            Assert.Contains("<title>Black Friday &lt;b&gt;-50 %&lt;/b&gt; &amp; plus</title>", html);
            Assert.DoesNotContain("<b>-50", html);
        }

        /// <summary>
        /// LE RÉGLAGE DE LA MEP GROUPÉE (accord de Min's, 22/11/2026) : `Courrier:UrlApi = https://mimia.fr` en prod. Les
        /// en-têtes d'un envoi commercial annoncent alors le désabonnement en un clic (POST), sans rien envoyer pour le vérifier.
        /// </summary>
        [Fact]
        public void Avec_UrlApi_de_prod_les_en_tetes_annoncent_le_desabonnement_en_un_clic()
        {
            var jetons = Jetons("https://mimia.fr");
            var enTetes = jetons.EnTetes(4242, CategorieDesabonnement.Diffusion)!;

            var liste = enTetes["List-Unsubscribe"];
            Assert.Matches(
                @"^<https://mimia\.fr/desabonnement/un-clic\?jeton=[^>\s]+>, <https://mimia\.fr/desabonnement\?jeton=[^>\s]+>$", liste);
            Assert.Equal("List-Unsubscribe=One-Click", enTetes["List-Unsubscribe-Post"]);

            // Le jeton du lien un clic est bien celui du parent et de la catégorie.
            var jeton = Uri.UnescapeDataString(liste[(liste.IndexOf("jeton=", StringComparison.Ordinal) + 6)..liste.IndexOf('>')]);
            Assert.Equal((4242, CategorieDesabonnement.Diffusion), jetons.Lire(jeton));

            // Une barre oblique finale dans le réglage ne double pas la barre.
            Assert.StartsWith("<https://mimia.fr/desabonnement/un-clic?", Jetons("https://mimia.fr/").EnTetes(1, CategorieDesabonnement.Diffusion)!["List-Unsubscribe"]);

            // Sans le réglage (prod d'aujourd'hui) : le lien seul, pas de POST un clic.
            var sansReglage = Jetons(null).EnTetes(4242, CategorieDesabonnement.Diffusion)!;
            Assert.Matches(@"^<https://mimia\.fr/desabonnement\?jeton=[^>\s]+>$", sansReglage["List-Unsubscribe"]);
            Assert.False(sansReglage.ContainsKey("List-Unsubscribe-Post"));
        }
    }
}
