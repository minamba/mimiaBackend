using Microsoft.Extensions.Logging.Abstractions;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace SchoolWebApp.Tests
{
    /// <summary>`Joignables` : aucune offre ne part à un parent qui n'a pas consenti, ni à une adresse bannie.</summary>
    public class FiltreEnvoiTests : BaseDeTest
    {
        [Fact]
        public async Task Offres_seulement_aux_parents_consentants_et_non_bannis()
        {
            var consentant = await CreerParentAsync(consenti: true);
            var nonConsentant = await CreerParentAsync();
            var banni = await CreerParentAsync(consenti: true);
            await BannirAsync(banni.Mail!);

            var liste = (await Envois.GetDestinatairesOffresAsync()).Select(d => d.ParentId).ToList();

            Assert.Contains(consentant.Id, liste);
            Assert.DoesNotContain(nonConsentant.Id, liste);
            Assert.DoesNotContain(banni.Id, liste);
        }

        [Fact]
        public async Task Cible_commerciale_seulement_aux_parents_consentants()
        {
            var consentant = await CreerParentAsync(consenti: true);
            var nonConsentant = await CreerParentAsync();

            var liste = (await Envois.GetDestinatairesCibleAsync(CibleEnvoi.Tous, CategorieDesabonnement.Diffusion, DateTime.UtcNow))
                .Select(d => d.ParentId).ToList();

            Assert.Contains(consentant.Id, liste);
            Assert.DoesNotContain(nonConsentant.Id, liste);
        }

        [Fact]
        public async Task Categorie_non_commerciale_reste_en_opt_out()
        {
            var sansConsentement = await CreerParentAsync();
            var desabonne = await CreerParentAsync();
            await Envois.DesabonnerAsync(desabonne.Id, CategorieDesabonnement.Avis, SourceConsentement.EspaceParent);

            var liste = (await Envois.GetDestinatairesCibleAsync(CibleEnvoi.Tous, CategorieDesabonnement.Avis, DateTime.UtcNow))
                .Select(d => d.ParentId).ToList();

            Assert.Contains(sansConsentement.Id, liste);
            Assert.DoesNotContain(desabonne.Id, liste);
        }

        [Fact]
        public async Task Un_ancien_desabonnement_des_annonces_ne_bloque_pas_un_consentement_donne_depuis()
        {
            var parent = await CreerParentAsync(consenti: true);
            await using (var c = NouveauContexte())
            {
                c.DesabonnementsMail.Add(new DesabonnementMail
                {
                    ParentId = parent.Id, Categorie = CategorieDesabonnement.Diffusion, DateCreation = DateTime.UtcNow.AddMonths(-1),
                });
                await c.SaveChangesAsync();
            }

            var liste = (await Envois.GetDestinatairesOffresAsync()).Select(d => d.ParentId).ToList();

            Assert.Contains(parent.Id, liste);
        }

        [Fact]
        public async Task Le_decompte_de_l_ecran_admin_est_la_liste_d_envoi()
        {
            await CreerParentAsync(consenti: true);
            await CreerParentAsync();

            // L'action ne lit aucun champ du contrôleur : pas besoin de ses dizaines de dépendances.
            var admin = (AdminController)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AdminController));
            var reponse = Assert.IsType<OkObjectResult>(await admin.CompterDestinatairesDiffusion(Envois, CancellationToken.None, "offres"));
            var nombre = (int)reponse.Value!.GetType().GetProperty("nombre")!.GetValue(reponse.Value)!;

            Assert.Equal((await Envois.GetDestinatairesOffresAsync()).Count, nombre);
        }

        [Fact]
        public async Task Destinataire_par_mail_seulement_s_il_a_consenti()
        {
            var consentant = await CreerParentAsync(consenti: true);
            var nonConsentant = await CreerParentAsync();
            var banni = await CreerParentAsync(consenti: true);
            await BannirAsync(banni.Mail!);

            var trouve = await Envois.GetDestinataireParMailAsync($"  {consentant.Mail!.ToUpperInvariant()} ", CategorieDesabonnement.Diffusion);
            Assert.Equal(consentant.Id, trouve?.ParentId);

            Assert.Null(await Envois.GetDestinataireParMailAsync(nonConsentant.Mail!, CategorieDesabonnement.Diffusion));
            Assert.Null(await Envois.GetDestinataireParMailAsync(banni.Mail!, CategorieDesabonnement.Diffusion));
            Assert.Null(await Envois.GetDestinataireParMailAsync("inconnu-" + Guid.NewGuid().ToString("N") + "@exemple.invalid", CategorieDesabonnement.Diffusion));
        }
    }
}
