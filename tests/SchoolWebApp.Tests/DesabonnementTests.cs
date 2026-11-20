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

        // ---------------------------------------------------------------------------------------------------------------
        // Revue d'Omar du 20/11/2026 (lot v3)
        // ---------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task Parent_non_consentant_pas_de_jeton_d_annulation_et_reabonnement_403()
        {
            // Le contournement du point 6 : POST /desabonnement puis POST /desabonnement/reabonnement, sur un parent qui
            // n'a jamais consenti, fabriquait un consentement « annulation_desinscription ».
            var parent = await CreerParentAsync();
            var controleur = Controleur();

            var r = await controleur.Desabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None);

            Assert.Equal(200, Statut(r));
            Assert.Null(Lire<string?>(r, "jetonAnnulation"));

            // Même avec un « jeton d'annulation » absent, vide ou inventé : 403, et rien n'est écrit.
            Assert.Equal(403, Statut(await controleur.Reabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None)));
            Assert.Equal(403, Statut(await controleur.Reabonner(new() { Jeton = JetonDe(parent.Id), JetonAnnulation = "" }, CancellationToken.None)));
            var relu = await RelireAsync(parent.Id);
            Assert.False(relu.OffresEmail);
            Assert.Null(relu.OffresEmailConsentiLe);
            Assert.Null(relu.OffresEmailSource);
            Assert.Empty(await JournalAsync(parent.Id));
        }

        [Fact]
        public async Task Parent_deja_desinscrit_un_second_clic_ne_donne_pas_de_jeton_d_annulation()
        {
            var parent = await CreerParentAsync(consenti: true);
            var controleur = Controleur();

            var premier = await controleur.Desabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None);
            var second = await controleur.Desabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None);

            Assert.False(string.IsNullOrEmpty(Lire<string>(premier, "jetonAnnulation")));
            Assert.Equal(200, Statut(second));
            Assert.Null(Lire<string?>(second, "jetonAnnulation"));
            Assert.Single(await JournalAsync(parent.Id));
        }

        [Fact]
        public async Task Desinscription_au_jeton_valide_jamais_bloquee_meme_depuis_une_seule_IP()
        {
            // Les POST « un clic » de Gmail ou Yahoo arrivent de quelques IP partagées : 80 désinscriptions d'une même IP
            // dans l'heure (plus du double de l'ancienne limite de 30) passent toutes.
            var contexte = ContexteHttp();
            var parents = new List<Domain.Models.Parent>();
            for (var i = 0; i < 40; i++) parents.Add(await CreerParentAsync(consenti: true));

            foreach (var p in parents)
            {
                var c = new DesabonnementController(Jetons, Envois, NullLogger<DesabonnementController>.Instance) { ControllerContext = contexte };
                Assert.Equal(200, Statut(await c.UnClic(JetonDe(p.Id), CancellationToken.None)));
                Assert.Equal(200, Statut(await c.Desabonner(new() { Jeton = JetonDe(p.Id) }, CancellationToken.None)));
            }

            foreach (var p in parents) Assert.False((await RelireAsync(p.Id)).OffresEmail);
        }

        [Fact]
        public async Task Seuls_les_jetons_refuses_sont_limites_a_30_par_heure_et_par_IP()
        {
            var parent = await CreerParentAsync(consenti: true);
            var contexte = ContexteHttp();
            DesabonnementController C() => new(Jetons, Envois, NullLogger<DesabonnementController>.Instance) { ControllerContext = contexte };
            var faux = JetonDe(parent.Id)[..^3] + "AAA";

            for (var i = 0; i < 30; i++) Assert.Equal(400, Statut(await C().UnClic(faux, CancellationToken.None)));

            // Le 31e jeton refusé de cette IP : 429, sur toutes les routes.
            Assert.Equal(429, Statut(await C().UnClic(faux, CancellationToken.None)));
            Assert.Equal(429, Statut(await C().Desabonner(new() { Jeton = faux }, CancellationToken.None)));
            Assert.Equal(429, Statut(await C().Lire(faux, CancellationToken.None)));

            // Mais un jeton valide, depuis cette même IP, passe toujours.
            Assert.Equal(200, Statut(await C().Lire(JetonDe(parent.Id), CancellationToken.None)));
            Assert.Equal(200, Statut(await C().UnClic(JetonDe(parent.Id), CancellationToken.None)));
            Assert.False((await RelireAsync(parent.Id)).OffresEmail);

            // Et une autre IP n'est pas touchée.
            Assert.Equal(400, Statut(await Controleur().UnClic(faux, CancellationToken.None)));
        }

        [Fact]
        public async Task Desinscription_pendant_un_retour_arriere_pas_de_jeton_d_annulation()
        {
            // L'ancien code a désinscrit ce parent (ligne `DesabonnementMail`) sans toucher à `OffresEmail` : il ne
            // reçoit déjà plus d'offres. Repasser par le lien n'ouvre donc aucun droit de « réabonnement ».
            var parent = await CreerParentAsync(consenti: true);
            var dateRetourArriere = await RetourArriere.DesinscriptionParLAncienCodeAsync(NouveauContexte, parent.Id);
            var controleur = Controleur();

            var lu = await controleur.Lire(JetonDe(parent.Id), CancellationToken.None);
            Assert.True(Lire<bool>(lu, "desabonne"));

            var r = await controleur.Desabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None);
            Assert.Equal(200, Statut(r));
            Assert.Null(Lire<string?>(r, "jetonAnnulation"));
            Assert.Equal(403, Statut(await controleur.Reabonner(new() { Jeton = JetonDe(parent.Id) }, CancellationToken.None)));

            // Le retrait du retour arrière est entré au journal, à sa date.
            var journal = Assert.Single(await JournalAsync(parent.Id));
            Assert.False(journal.Consenti);
            Assert.Equal(SourceConsentement.RetourArriere, journal.Source);
            Assert.Equal(dateRetourArriere, journal.Le, TimeSpan.FromMilliseconds(5));
        }

        private string JetonDe(int parentId) => Uri.UnescapeDataString(Jetons.Lien(parentId, Offres)!.Split("jeton=")[1]);
    }
}
