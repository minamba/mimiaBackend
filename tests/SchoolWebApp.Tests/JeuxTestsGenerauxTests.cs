using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Services;
using DomainJeu = SchoolWebApp.Domain.Models.JeuDeTest;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// « TESTS GÉNÉRAUX » DANS LES JEUX DE TESTS — Min's, le 22/11/2026 : une section et une classe « Tests Généraux »
    /// pour les bugs hors cours et hors professeur ; leurs blocs ont une CATÉGORIE libre (pas de matière), reproposée
    /// ensuite ; et un signalement admin créé sans jeu (ex. #62) peut y être rattaché après coup.
    ///
    /// Vraie base de TEST (les catégories se lisent en SQL). Le référentiel n'est jamais lu pour les tests généraux :
    /// son faux lève une exception si on l'appelle.
    /// </summary>
    public class JeuxTestsGenerauxTests : BaseDeTest, IAsyncLifetime
    {
        private readonly List<int> _jeux = [];
        private readonly List<int> _signalements = [];

        // Une catégorie propre à ce passage : une autre exécution (ou une vraie catégorie de la base de test) ne la voit pas.
        private readonly string _categorie = "Zz Test " + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        private JeuxDeTestsController Jeux()
        {
            var utilisateur = new Mock<ICurrentUserAccessor>();
            utilisateur.SetupGet(u => u.Prenom).Returns("Testeur");
            utilisateur.SetupGet(u => u.Nom).Returns("Auto");

            var referentiel = new Mock<IReferentielService>(MockBehavior.Strict);

            return new JeuxDeTestsController(
                new JeuxDeTestsRepository(NouveauContexte()), referentiel.Object, utilisateur.Object,
                new SignalementRepository(NouveauContexte()), new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<JeuxDeTestsController>.Instance);
        }

        private SignalementsAdminController Signalements() => new(
            new Domain.Services.impl.SignalementService(new SignalementRepository(NouveauContexte()), Mock.Of<IStockageSignalements>()),
            new JeuxDeTestsRepository(NouveauContexte()), Mock.Of<IParentService>(), Mock.Of<ICurrentUserAccessor>(),
            Mock.Of<IEvenementsAdminHub>(), NullLogger<SignalementsAdminController>.Instance);

        private static T Corps<T>(IActionResult resultat) where T : class =>
            (resultat as ObjectResult)?.Value as T ?? throw new InvalidOperationException($"Réponse inattendue : {resultat}");

        private static string Message(IActionResult resultat) =>
            (resultat as ObjectResult)?.Value?.GetType().GetProperty("message")?.GetValue((resultat as ObjectResult)!.Value) as string ?? string.Empty;

        private async Task<DomainJeu> CreerJeuGenerauxAsync()
        {
            var r = await Jeux().Creer(new JeuDeTestRequest { Titre = "Jeu auto Tests Généraux", Section = "generaux" }, default);
            var jeu = Corps<DomainJeu>(r);
            _jeux.Add(jeu.Id);
            return jeu;
        }

        [Fact]
        public async Task Un_jeu_de_tests_generaux_n_a_besoin_d_aucune_classe_du_referentiel()
        {
            var jeu = await CreerJeuGenerauxAsync();

            Assert.Equal("generaux", jeu.Section);
            Assert.Equal("Tests Généraux", jeu.Classe);
            Assert.Equal("GENERAUX", jeu.NiveauCode);

            // Modifier le titre en le laissant dans les tests généraux : même chose, toujours sans référentiel.
            var modifie = Corps<DomainJeu>(await Jeux().Modifier(jeu.Id, new JeuDeTestRequest { Titre = "Renommé", Section = "generaux" }, default));
            Assert.Equal("Renommé", modifie.Titre);
            Assert.Equal("Tests Généraux", modifie.Classe);
        }

        [Fact]
        public async Task Sans_section_generaux_ni_classe_le_jeu_est_refuse()
        {
            var r = await Jeux().Creer(new JeuDeTestRequest { Titre = "Sans classe" }, default);

            Assert.IsType<BadRequestObjectResult>(r);
            Assert.Equal("La classe est obligatoire.", Message(r));
        }

        [Fact]
        public async Task Un_bloc_des_tests_generaux_porte_une_categorie_libre_reproposee_ensuite()
        {
            var jeu = await CreerJeuGenerauxAsync();
            var controleur = Jeux();

            var r = await controleur.AjouterTest(jeu.Id, new TestDuJeuRequest
            {
                Titre = "Ajout d'enfant", Categorie = $"  {_categorie}  ", Nombre = 2, Competences = ["Un admin ajoute un 5e enfant"],
            }, default);
            var apres = Corps<DomainJeu>(r);

            var bloc = Assert.Single(apres.Tests);
            Assert.Equal("categorie", bloc.MatiereCode);
            Assert.Equal(_categorie, bloc.Matiere);
            Assert.Equal(2, bloc.Lignes.Count);

            var categories = Corps<IReadOnlyList<string>>(await controleur.Categories(default));
            Assert.Contains(_categorie, categories);

            // La même catégorie écrite autrement (casse, espaces) reprend l'orthographe déjà employée : pas de 2e groupe.
            var autrement = "  " + _categorie.ToLowerInvariant().Replace(" ", "   ") + " ";
            var deux = Corps<DomainJeu>(await controleur.AjouterTest(jeu.Id, new TestDuJeuRequest
            {
                Titre = "Compte parent", Categorie = autrement, Nombre = 1,
            }, default));
            Assert.All(deux.Tests, t => Assert.Equal(_categorie, t.Matiere));
            Assert.Single(Corps<IReadOnlyList<string>>(await controleur.Categories(default)), c => c.Equals(_categorie, StringComparison.OrdinalIgnoreCase));

            // Modifier le bloc : nouvelle catégorie libre.
            var neuve = _categorie + " bis";
            var modifie = Corps<DomainJeu>(await controleur.ModifierTest(jeu.Id, bloc.Id, new TestDuJeuRequest
            {
                Titre = "Ajout d'enfant (admin)", Categorie = neuve,
            }, default));
            var relu = Assert.Single(modifie.Tests, t => t.Id == bloc.Id);
            Assert.Equal(neuve, relu.Matiere);
            Assert.Equal("categorie", relu.MatiereCode);
        }

        [Fact]
        public async Task Un_bloc_des_tests_generaux_sans_categorie_ou_trop_longue_est_refuse()
        {
            var jeu = await CreerJeuGenerauxAsync();

            var vide = await Jeux().AjouterTest(jeu.Id, new TestDuJeuRequest { Titre = "Bloc", Categorie = "   ", MatiereId = 3 }, default);
            Assert.IsType<BadRequestObjectResult>(vide);
            Assert.Equal("La catégorie est obligatoire.", Message(vide));

            var longue = await Jeux().AjouterTest(jeu.Id, new TestDuJeuRequest { Titre = "Bloc", Categorie = new string('x', 101) }, default);
            Assert.IsType<BadRequestObjectResult>(longue);
        }

        [Fact]
        public async Task Un_signalement_admin_cree_sans_jeu_se_rattache_apres_coup_a_un_bloc()
        {
            var jeu = await CreerJeuGenerauxAsync();
            var avecBloc = Corps<DomainJeu>(await Jeux().AjouterTest(jeu.Id, new TestDuJeuRequest
            {
                Titre = "Abonnements", Categorie = _categorie, Nombre = 1,
            }, default));
            var bloc = Assert.Single(avecBloc.Tests);

            var parent = await CreerParentAsync();
            var depot = new SignalementRepository(NouveauContexte());
            var signalement = await depot.CreerAdminAsync(parent.Id, "AUTRE", "La formule la plus large mène aux tarifs.", "Formule pleine", null);
            _signalements.Add(signalement.Id);

            var r = await Signalements().Rattacher(signalement.Id, new RattachementJeuRequest { JeuId = jeu.Id, TestId = bloc.Id }, depot, default);
            var relie = Corps<Signalement>(r);

            Assert.NotNull(relie.LigneDeTestId);
            Assert.NotNull(relie.Test);
            Assert.Equal("Formule pleine", relie.Test!.Competence);
            Assert.Equal("ko", relie.Test.Statut);
            Assert.Equal("Tests Généraux", relie.Test.Classe);

            // La 1re ligne vide du bloc a servi : pas de ligne en plus.
            await using (var c = NouveauContexte())
            {
                Assert.Equal(1, await c.LignesDeTest.CountAsync(l => l.TestId == bloc.Id));
            }

            // Déjà relié : refusé, rien ne bouge.
            var encore = await Signalements().Rattacher(signalement.Id, new RattachementJeuRequest { JeuId = jeu.Id, TestId = bloc.Id }, depot, default);
            Assert.IsType<ConflictObjectResult>(encore);
        }

        [Fact]
        public async Task Un_signalement_deja_traite_rattache_part_a_tester()
        {
            var jeu = await CreerJeuGenerauxAsync();
            var bloc = Assert.Single(Corps<DomainJeu>(await Jeux().AjouterTest(jeu.Id, new TestDuJeuRequest
            {
                Titre = "Admin", Categorie = _categorie, Nombre = 1,
            }, default)).Tests);

            var parent = await CreerParentAsync();
            var depot = new SignalementRepository(NouveauContexte());
            var signalement = await depot.CreerAdminAsync(parent.Id, "TECHNIQUE", "Bug corrigé et en ligne.", null, null);
            _signalements.Add(signalement.Id);
            await depot.ModifierAsync(signalement.Id, "TECHNIQUE", "Bug corrigé et en ligne.", "traite");

            // Sans compétence (ni dans la requête ni dans le signalement) : refusé.
            var sans = await Signalements().Rattacher(signalement.Id, new RattachementJeuRequest { JeuId = jeu.Id, TestId = bloc.Id }, depot, default);
            Assert.IsType<BadRequestObjectResult>(sans);

            var r = await Signalements().Rattacher(signalement.Id, new RattachementJeuRequest
            {
                JeuId = jeu.Id, TestId = bloc.Id, Competence = "  Le bug ne revient pas  ",
            }, depot, default);
            var relie = Corps<Signalement>(r);

            Assert.Equal("a_tester", relie.Test!.Statut);
            Assert.Equal("Le bug ne revient pas", relie.Competence);
            Assert.Equal("traite", relie.Etat);
        }

        [Fact]
        public async Task Un_signalement_de_parent_ne_se_rattache_pas()
        {
            var jeu = await CreerJeuGenerauxAsync();
            var bloc = Assert.Single(Corps<DomainJeu>(await Jeux().AjouterTest(jeu.Id, new TestDuJeuRequest
            {
                Titre = "UX", Categorie = _categorie, Nombre = 1,
            }, default)).Tests);

            var parent = await CreerParentAsync();
            var depot = new SignalementRepository(NouveauContexte());
            var signalement = await depot.CreerAsync(parent.Id, null, "TECHNIQUE", "Signalement d'un parent.");
            _signalements.Add(signalement.Id);

            var r = await Signalements().Rattacher(signalement.Id, new RattachementJeuRequest { JeuId = jeu.Id, TestId = bloc.Id, Competence = "x" }, depot, default);
            Assert.IsType<NotFoundObjectResult>(r);
        }

        [Theory]
        [InlineData("generaux", "Tests Généraux", "Tests Généraux")]
        [InlineData("elementaire", "CP", "Élémentaire · CP")]
        [InlineData("lycee", "Terminale", "Lycée · Terminale")]
        public void La_section_et_la_classe_se_lisent_sans_doublon(string section, string classe, string attendu) =>
            Assert.Equal(attendu, StatutsDeTest.SectionEtClasse(section, classe));

        [Fact]
        public void Une_categorie_se_met_au_propre()
        {
            Assert.Equal("Compte parent", StatutsDeTest.Categorie("  Compte   parent "));
            Assert.Equal("UX", StatutsDeTest.Categorie("ux", ["Admin", "UX"]));
            Assert.Null(StatutsDeTest.Categorie("   "));
            Assert.Null(StatutsDeTest.Categorie(new string('a', 101)));
            Assert.Equal(new string('a', 100), StatutsDeTest.Categorie(new string('a', 100)));
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await using (var c = NouveauContexte())
            {
                if (_signalements.Count > 0) await c.Signalements.Where(s => _signalements.Contains(s.Id)).ExecuteDeleteAsync();
                if (_jeux.Count > 0) await c.JeuxDeTests.Where(j => _jeux.Contains(j.Id)).ExecuteDeleteAsync();
            }

            await DisposeAsync();
        }
    }
}
