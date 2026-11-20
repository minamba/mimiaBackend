using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// UN COURRIEL PROGRAMMÉ CRÉÉ DEPUIS L'ÉCRAN EST UNE INFORMATION (décision de Min's, 20/11/2026) : il part à TOUTE sa
    /// cible, consentement aux offres ou non, sans lien de désinscription ni en-têtes, et même sans clé de désinscription.
    /// Relances et rappels gardent leur propre désabonnement.
    /// </summary>
    public class EnvoiProgrammeInformationTests : BaseDeTest
    {
        // Un identifiant de modèle qui n'existe pas en base : son code de journal (`MODELE:<id>`) n'appartient qu'à ce test.
        private const int IdModele = 987650;

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

        private static ModeleMailDetail ModeleDeLEcran() => new()
        {
            Id = IdModele,
            Nature = NatureModeleMail.Automatique,
            Code = null, // créé depuis l'écran → information, sans catégorie
            Nom = "Information programmée de test",
            Sujet = "Du nouveau, {{prenom}}",
            Titre = "Du nouveau",
            // La variable, si elle traîne dans le texte, disparaît : pas de marqueur brut chez le parent.
            Texte = "Bonjour {{prenom}}, voici les nouveautés de la semaine. {{lien_desinscription}}",
            Cible = CibleEnvoi.Nouveaux,
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
        public void Un_courriel_cree_depuis_l_ecran_n_a_pas_de_categorie_relances_et_rappels_gardent_la_leur()
        {
            Assert.Null(CodeModeleMail.Categorie(null));
            Assert.Equal(CategorieDesabonnement.Relance, CodeModeleMail.Categorie(CodeModeleMail.Relance));
            Assert.Equal(CategorieDesabonnement.Rappels, CodeModeleMail.Categorie(CodeModeleMail.Rappels));
            Assert.Equal(CategorieDesabonnement.Avis, CodeModeleMail.Categorie(CodeModeleMail.AvisGeneral));
            Assert.Null(CodeModeleMail.Categorie(CodeModeleMail.Bilans));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Part_a_toute_la_cible_sans_lien_ni_en_tetes_avec_ou_sans_cle(bool avecCle)
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
                Assert.DoesNotContain("{{lien_desinscription}}", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("/desabonnement", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("désinscrire", envoi.Valeurs["mentionPied"], StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Ne plus recevoir", envoi.Valeurs["mentionPied"]);
            }
            finally
            {
                await EffacerLeJournalAsync();
            }
        }
    }
}
