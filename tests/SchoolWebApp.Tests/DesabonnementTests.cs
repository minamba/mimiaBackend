using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// Le lien de désinscription : GET sans effet, POST et un-clic qui retirent, signature fausse refusée, et pour les
    /// offres, réabonnement seulement avec le jeton d'annulation remis par la désinscription qui vient d'avoir lieu.
    /// </summary>
    public class DesabonnementTests : BaseDeTest
    {
        private const string Offres = CategorieDesabonnement.Diffusion;

        private DesabonnementController Controleur() =>
            new(Jetons, Envois, NullLogger<DesabonnementController>.Instance) { ControllerContext = ContexteHttp() };

        private static T Lire<T>(IActionResult resultat, string propriete)
        {
            var valeur = Assert.IsAssignableFrom<ObjectResult>(resultat).Value!;
            return (T)valeur.GetType().GetProperty(propriete)!.GetValue(valeur)!;
        }

        private static int Statut(IActionResult r) => r switch
        {
            ObjectResult o => o.StatusCode ?? 200,
            StatusCodeResult s => s.StatusCode,
            _ => throw new InvalidOperationException(r.GetType().Name),
        };

        [Fact]
        public async Task Ouvrir_la_page_ne_desinscrit_pas()
        {
            var parent = await CreerParentAsync(consenti: true);

            var r = await Controleur().Lire(JetonDe(parent.Id), CancellationToken.None);

            Assert.Equal(200, Statut(r));
            Assert.False(Lire<bool>(r, "desabonne"));
            Assert.True(Lire<bool>(r, "commerciale"));
            Assert.True((await RelireAsync(parent.Id)).OffresEmail);
        }

        [Fact]
        public async Task Le_bouton_retire_le_consentement_et_remet_un_jeton_d_annulation()
        {
            var parent = await CreerParentAsync(consenti: true);

            var r = await Controleur().Desabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None);

            Assert.Equal(200, Statut(r));
            Assert.False(string.IsNullOrEmpty(Lire<string>(r, "jetonAnnulation")));
            var relu = await RelireAsync(parent.Id);
            Assert.False(relu.OffresEmail);
            Assert.Equal(SourceConsentement.LienCourriel, relu.OffresEmailSource);
        }

        [Fact]
        public async Task Desinscription_en_un_clic()
        {
            var parent = await CreerParentAsync(consenti: true);

            var r = await Controleur().UnClic(JetonDe(parent.Id), CancellationToken.None);

            Assert.Equal(200, Statut(r));
            var relu = await RelireAsync(parent.Id);
            Assert.False(relu.OffresEmail);
            Assert.Equal(SourceConsentement.UnClic, relu.OffresEmailSource);
        }

        [Fact]
        public async Task Signature_fausse_refusee_partout()
        {
            var parent = await CreerParentAsync(consenti: true);
            var faux = JetonDe(parent.Id)[..^3] + "AAA";
            var autreCle = CreerJetons("une-autre-cle-0123456789").Lien(parent.Id, Offres)!.Split("jeton=")[1];

            Assert.Equal(400, Statut(await Controleur().Lire(faux, CancellationToken.None)));
            Assert.Equal(400, Statut(await Controleur().Desabonner(new() { Jeton = faux }, CancellationToken.None)));
            Assert.Equal(400, Statut(await Controleur().UnClic(Uri.UnescapeDataString(autreCle), CancellationToken.None)));
            Assert.True((await RelireAsync(parent.Id)).OffresEmail);
        }

        [Fact]
        public async Task Un_vieux_lien_seul_ne_redonne_pas_le_consentement_aux_offres()
        {
            var parent = await CreerParentAsync(consenti: true);
            await Envois.DesabonnerAsync(parent.Id, Offres, SourceConsentement.EspaceParent);

            var r = await Controleur().Reabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None);

            Assert.Equal(403, Statut(r));
            Assert.False((await RelireAsync(parent.Id)).OffresEmail);
        }

        [Fact]
        public async Task Je_me_suis_trompe_juste_apres_la_desinscription()
        {
            var parent = await CreerParentAsync(consenti: true);
            var controleur = Controleur();
            var annulation = Lire<string>(await controleur.Desabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None), "jetonAnnulation");

            var r = await controleur.Reabonner(new() { Jeton = JetonDe(parent.Id), JetonAnnulation = annulation }, CancellationToken.None);

            Assert.Equal(200, Statut(r));
            var relu = await RelireAsync(parent.Id);
            Assert.True(relu.OffresEmail);
            Assert.Equal(SourceConsentement.AnnulationDesinscription, relu.OffresEmailSource);
        }

        [Fact]
        public async Task Jeton_d_annulation_expire_ou_d_un_autre_parent_refuse()
        {
            var parent = await CreerParentAsync();
            var autre = await CreerParentAsync();
            var expire = Jetons.CreerAnnulation(parent.Id, Offres, DateTime.UtcNow.AddHours(-1), TimeSpan.FromMinutes(30));
            var dUnAutre = Jetons.CreerAnnulation(autre.Id, Offres, DateTime.UtcNow, TimeSpan.FromMinutes(30));

            Assert.Equal(403, Statut(await Controleur().Reabonner(new() { Jeton = JetonDe(parent.Id), JetonAnnulation = expire }, CancellationToken.None)));
            Assert.Equal(403, Statut(await Controleur().Reabonner(new() { Jeton = JetonDe(parent.Id), JetonAnnulation = dUnAutre }, CancellationToken.None)));
            Assert.Equal(403, Statut(await Controleur().Reabonner(new() { Jeton = JetonDe(parent.Id), JetonAnnulation = JetonDe(parent.Id) }, CancellationToken.None)));
            Assert.False((await RelireAsync(parent.Id)).OffresEmail);
        }

        [Fact]
        public async Task Un_jeton_d_annulation_ne_sert_pas_de_lien_de_desinscription()
        {
            var parent = await CreerParentAsync(consenti: true);
            var annulation = Jetons.CreerAnnulation(parent.Id, Offres, DateTime.UtcNow, TimeSpan.FromMinutes(30));

            Assert.Equal(400, Statut(await Controleur().Desabonner(new() { Jeton = annulation }, CancellationToken.None)));
            Assert.True((await RelireAsync(parent.Id)).OffresEmail);
        }

        [Fact]
        public async Task Categorie_non_commerciale_le_lien_suffit_comme_avant()
        {
            var parent = await CreerParentAsync();
            var jeton = Uri.UnescapeDataString(Jetons.Lien(parent.Id, CategorieDesabonnement.Avis)!.Split("jeton=")[1]);

            var r1 = await Controleur().Desabonner(new() { Jeton = jeton }, CancellationToken.None);
            Assert.Null(Lire<string?>(r1, "jetonAnnulation"));
            Assert.True(await Envois.EstDesabonneAsync(parent.Id, CategorieDesabonnement.Avis));

            Assert.Equal(200, Statut(await Controleur().Reabonner(new() { Jeton = jeton }, CancellationToken.None)));
            Assert.False(await Envois.EstDesabonneAsync(parent.Id, CategorieDesabonnement.Avis));
        }

        private string JetonDe(int parentId) => Uri.UnescapeDataString(Jetons.Lien(parentId, Offres)!.Split("jeton=")[1]);
    }
}
