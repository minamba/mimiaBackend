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
    /// UN COURRIEL PROGRAMMÉ CRÉÉ DEPUIS L'ÉCRAN (catégorie « diffusion », donc commercial) — question d'Omar, 19/11/2026 :
    /// part-il sans lien quand la clé de désinscription manque ? Non : il est bloqué comme la diffusion, rien ne part et
    /// rien n'est réservé dans le journal. Avec la clé, il ne part qu'aux consentants, avec lien, pied et en-têtes.
    /// </summary>
    public class EnvoiProgrammeCommercialTests : BaseDeTest
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
            Nature = NatureModeleMail.Diffusion,
            Code = null, // créé depuis l'écran → catégorie « diffusion »
            Nom = "Offre programmée de test",
            Sujet = "Notre offre, {{prenom}}",
            Titre = "Notre offre",
            Texte = "Bonjour {{prenom}}, voici notre offre. {{lien_desinscription}}",
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

        private async Task<int> LignesDuJournalAsync()
        {
            await using var c = NouveauContexte();
            return await c.EnvoisAutomatiques.CountAsync(e => e.CodeModele == CodeModeleMail.CodeJournal(null, IdModele));
        }

        private async Task EffacerLeJournalAsync()
        {
            await using var c = NouveauContexte();
            await c.EnvoisAutomatiques.Where(e => e.CodeModele == CodeModeleMail.CodeJournal(null, IdModele)).ExecuteDeleteAsync();
        }

        [Fact]
        public async Task Sans_cle_de_desinscription_le_courriel_programme_commercial_ne_part_pas()
        {
            await CreerParentAsync(consenti: true);
            var (service, email, envois) = Preparer(CreerJetons(null));

            var resultat = await service.ExecuterAsync(ModeleDeLEcran(), DateTime.UtcNow);

            Assert.Contains("Clé de désabonnement absente", resultat);
            Assert.Empty(envois);
            email.Verify(e => e.OuvrirSession(), Times.Never);
            Assert.Equal(0, await LignesDuJournalAsync());
        }

        [Fact]
        public async Task Avec_la_cle_seulement_aux_consentants_avec_lien_pied_et_en_tetes()
        {
            var consentant = await CreerParentAsync(consenti: true, prenom: "Alix");
            var nonConsentant = await CreerParentAsync(prenom: "Sacha");
            var (service, _, envois) = Preparer(Jetons);

            try
            {
                await service.ExecuterAsync(ModeleDeLEcran(), DateTime.UtcNow);

                Assert.DoesNotContain(envois, e => e.Destinataire == nonConsentant.Mail);

                var envoi = Assert.Single(envois, e => e.Destinataire == consentant.Mail);
                Assert.NotNull(envoi.EnTetes);
                Assert.True(envoi.EnTetes!.ContainsKey("List-Unsubscribe"));
                Assert.Contains("Me désinscrire", envoi.Valeurs["mentionPied"]);
                Assert.Contains("/desabonnement?jeton=", envoi.Valeurs["corpsMessage"]);
                Assert.DoesNotContain("{{lien_desinscription}}", envoi.Valeurs["corpsMessage"]);
            }
            finally
            {
                await EffacerLeJournalAsync();
            }
        }
    }
}
