using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Services;
using SchoolWebApp.Domain.Services.impl;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// `GET /profil` et `POST /profil/conditions` : le claim des offres n'est appliqué qu'une fois, la case des conditions
    /// ne vaut que pour la version sous laquelle elle a été cochée (`cgv_version`), et seul `offresEmail: true` consent.
    /// </summary>
    public class ProfilTests : BaseDeTest
    {
        private ProfilController Controleur(
            Domain.Models.Parent parent,
            DateTime? offresCocheesLe = null,
            DateTime? cgvCocheesLe = null,
            string? cgvVersion = null)
        {
            var utilisateur = new Mock<ICurrentUserAccessor>();
            utilisateur.SetupGet(u => u.IdentityUserId).Returns(parent.IdentityUserId);
            utilisateur.SetupGet(u => u.Mail).Returns(parent.Mail);
            utilisateur.SetupGet(u => u.EleveId).Returns((int?)null);
            utilisateur.SetupGet(u => u.OffresCocheesLe).Returns(offresCocheesLe);
            utilisateur.SetupGet(u => u.ConditionsCocheesLe).Returns(cgvCocheesLe);
            utilisateur.SetupGet(u => u.ConditionsVersionCochee).Returns(cgvVersion);

            var eleves = new Mock<IEleveService>();
            eleves.Setup(e => e.GetElevesByParentAsync(It.IsAny<int>())).ReturnsAsync([]);

            // Un contexte neuf par requête, comme en vrai (une portée par requête HTTP).
            var contexte = NouveauContexte();
            return new ProfilController(
                new ParentService(new ParentRepository(contexte)), eleves.Object, utilisateur.Object,
                NullLogger<ProfilController>.Instance);
        }

        private static T Lire<T>(IActionResult resultat, string propriete)
        {
            var valeur = Assert.IsType<OkObjectResult>(resultat).Value!;
            return (T)valeur.GetType().GetProperty(propriete)!.GetValue(valeur)!;
        }

        [Fact]
        public async Task La_case_offres_de_l_inscription_consent_une_fois_avec_sa_date_et_sa_source()
        {
            var parent = await CreerParentAsync();
            var clic = DateTime.UtcNow.AddMinutes(-5);

            var r = await Controleur(parent, offresCocheesLe: clic).Get();

            Assert.True(Lire<bool>(r, "OffresEmail"));
            var relu = await RelireAsync(parent.Id);
            Assert.True(relu.OffresEmail);
            Assert.Equal(clic, relu.OffresEmailConsentiLe!.Value, TimeSpan.FromMilliseconds(5));
            Assert.Equal(SourceConsentement.Inscription, relu.OffresEmailSource);
            Assert.Single(await JournalAsync(parent.Id));
        }

        [Fact]
        public async Task Le_claim_ne_reinscrit_pas_un_parent_qui_s_est_desinscrit()
        {
            var parent = await CreerParentAsync();
            var clic = DateTime.UtcNow.AddMinutes(-5);
            await Controleur(parent, offresCocheesLe: clic).Get();
            await Envois.DesabonnerAsync(parent.Id, CategorieDesabonnement.Diffusion, SourceConsentement.EspaceParent);

            // Connexion suivante : le jeton porte toujours la date du clic d'inscription.
            var r = await Controleur(parent, offresCocheesLe: clic).Get();

            Assert.False(Lire<bool>(r, "OffresEmail"));
            Assert.False((await RelireAsync(parent.Id)).OffresEmail);
            Assert.Equal(2, (await JournalAsync(parent.Id)).Count);
        }

        [Fact]
        public async Task Conditions_sans_corps_ou_a_faux_ne_touchent_pas_aux_offres()
        {
            var sansCorps = await CreerParentAsync();
            var aFaux = await CreerParentAsync();

            Assert.IsType<NoContentResult>(await Controleur(sansCorps).AccepterConditions(null, CancellationToken.None));
            Assert.IsType<NoContentResult>(await Controleur(aFaux).AccepterConditions(new() { OffresEmail = false }, CancellationToken.None));

            foreach (var id in new[] { sansCorps.Id, aFaux.Id })
            {
                var relu = await RelireAsync(id);
                Assert.Equal(ConditionsEnVigueur.Version, relu.ConditionsVersion);
                Assert.False(relu.OffresEmail);
                Assert.Empty(await JournalAsync(id));
            }
        }

        [Fact]
        public async Task Conditions_avec_la_case_offres_cochee_consent_depuis_la_fenetre()
        {
            var parent = await CreerParentAsync();

            await Controleur(parent).AccepterConditions(new() { OffresEmail = true }, CancellationToken.None);

            var relu = await RelireAsync(parent.Id);
            Assert.True(relu.OffresEmail);
            Assert.Equal(SourceConsentement.FenetreConditions, relu.OffresEmailSource);
            Assert.Equal(SourceConsentement.FenetreConditions, Assert.Single(await JournalAsync(parent.Id)).Source);
        }

        [Fact]
        public async Task La_case_des_conditions_vaut_seulement_pour_sa_version()
        {
            var aJour = await CreerParentAsync();
            var ancienne = await CreerParentAsync();
            var sansVersion = await CreerParentAsync();
            var clic = DateTime.UtcNow.AddMinutes(-1);

            Assert.False(Lire<bool>(await Controleur(aJour, cgvCocheesLe: clic, cgvVersion: ConditionsEnVigueur.Version).Get(), "ConditionsAAccepter"));
            Assert.True(Lire<bool>(await Controleur(ancienne, cgvCocheesLe: clic, cgvVersion: "1.0").Get(), "ConditionsAAccepter"));
            Assert.True(Lire<bool>(await Controleur(sansVersion, cgvCocheesLe: clic).Get(), "ConditionsAAccepter"));

            Assert.Equal(ConditionsEnVigueur.Version, (await RelireAsync(aJour.Id)).ConditionsVersion);
            Assert.Null((await RelireAsync(sansVersion.Id)).ConditionsVersion);
        }

        [Fact]
        public void Regle_de_la_case_independante_de_la_date_de_mise_en_ligne()
        {
            var avantDepuis = ConditionsEnVigueur.Depuis.AddDays(-30);

            Assert.True(ConditionsEnVigueur.ValentPourLaCaseDe(ConditionsEnVigueur.Version, avantDepuis));
            Assert.False(ConditionsEnVigueur.ValentPourLaCaseDe(null, DateTime.UtcNow));
            Assert.False(ConditionsEnVigueur.ValentPourLaCaseDe(ConditionsEnVigueur.Version, null));
        }
    }
}
