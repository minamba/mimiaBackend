using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Tests
{
    /// <summary>Desabonner / Reabonner / EstDesabonne pour la catégorie commerciale : dates, source, journal en ajout seul.</summary>
    public class ConsentementTests : BaseDeTest
    {
        private const string Offres = CategorieDesabonnement.Diffusion;

        [Fact]
        public async Task Retrait_date_avec_sa_source_et_une_ligne_de_journal()
        {
            var parent = await CreerParentAsync(consenti: true);

            await Envois.DesabonnerAsync(parent.Id, Offres, SourceConsentement.LienCourriel);

            var relu = await RelireAsync(parent.Id);
            Assert.False(relu.OffresEmail);
            Assert.NotNull(relu.OffresEmailRetireLe);
            Assert.NotNull(relu.OffresEmailConsentiLe); // l'historique reste
            Assert.Equal(SourceConsentement.LienCourriel, relu.OffresEmailSource);
            Assert.True(await Envois.EstDesabonneAsync(parent.Id, Offres));

            var journal = Assert.Single(await JournalAsync(parent.Id));
            Assert.False(journal.Consenti);
            Assert.Equal(SourceConsentement.LienCourriel, journal.Source);
        }

        [Fact]
        public async Task Retirer_un_consentement_jamais_donne_n_ecrit_rien()
        {
            var parent = await CreerParentAsync();

            await Envois.DesabonnerAsync(parent.Id, Offres, SourceConsentement.UnClic);

            var relu = await RelireAsync(parent.Id);
            Assert.Null(relu.OffresEmailRetireLe);
            Assert.Null(relu.OffresEmailSource);
            Assert.Empty(await JournalAsync(parent.Id));
            Assert.True(await Envois.EstDesabonneAsync(parent.Id, Offres));
        }

        [Fact]
        public async Task Consentir_deux_fois_garde_la_date_d_origine_et_une_seule_ligne()
        {
            var parent = await CreerParentAsync();

            await Envois.ReabonnerAsync(parent.Id, Offres, SourceConsentement.EspaceParent);
            var premiere = (await RelireAsync(parent.Id)).OffresEmailConsentiLe;
            await Task.Delay(20);
            await Envois.ReabonnerAsync(parent.Id, Offres, SourceConsentement.EspaceParent);

            var relu = await RelireAsync(parent.Id);
            Assert.True(relu.OffresEmail);
            Assert.Equal(premiere, relu.OffresEmailConsentiLe);
            Assert.False(await Envois.EstDesabonneAsync(parent.Id, Offres));

            var journal = Assert.Single(await JournalAsync(parent.Id));
            Assert.True(journal.Consenti);
            Assert.Equal(SourceConsentement.EspaceParent, journal.Source);
        }

        [Fact]
        public async Task Le_journal_garde_toute_l_histoire_dans_l_ordre()
        {
            var parent = await CreerParentAsync();

            await Envois.ReabonnerAsync(parent.Id, Offres, SourceConsentement.EspaceParent);
            await Envois.DesabonnerAsync(parent.Id, Offres, SourceConsentement.LienCourriel);
            await Envois.ReabonnerAsync(parent.Id, Offres, SourceConsentement.AnnulationDesinscription);

            var journal = await JournalAsync(parent.Id);
            Assert.Equal([true, false, true], journal.Select(j => j.Consenti));
            Assert.Equal(
                [SourceConsentement.EspaceParent, SourceConsentement.LienCourriel, SourceConsentement.AnnulationDesinscription],
                journal.Select(j => j.Source));
            Assert.Equal(SourceConsentement.AnnulationDesinscription, (await RelireAsync(parent.Id)).OffresEmailSource);
        }

        [Fact]
        public async Task Categorie_non_commerciale_ligne_de_desabonnement_sans_journal()
        {
            var parent = await CreerParentAsync();

            await Envois.DesabonnerAsync(parent.Id, CategorieDesabonnement.Relance, SourceConsentement.LienCourriel);
            Assert.True(await Envois.EstDesabonneAsync(parent.Id, CategorieDesabonnement.Relance));

            await Envois.ReabonnerAsync(parent.Id, CategorieDesabonnement.Relance, SourceConsentement.LienCourriel);
            Assert.False(await Envois.EstDesabonneAsync(parent.Id, CategorieDesabonnement.Relance));

            Assert.Empty(await JournalAsync(parent.Id));
            Assert.False((await RelireAsync(parent.Id)).OffresEmail);
        }
    }
}
