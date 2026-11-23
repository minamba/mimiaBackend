using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Services;
using DomainJeu = SchoolWebApp.Domain.Models.JeuDeTest;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// ÉMOTICÔNES ET TEXTES AÉRÉS DANS LES JEUX DE TESTS ET LES SIGNALEMENTS — Min's, le 23/11/2026 : Adam réécrit
    /// les tests avec des émoticônes et un point par ligne. Les titres, les compétences, les commentaires, la
    /// description et les commentaires d'un signalement doivent revenir de la base À L'IDENTIQUE (émoticônes
    /// composées, lignes vides comprises).
    ///
    /// Et « Effectué le » ne bouge plus quand on réécrit le texte d'une ligne sans changer son statut.
    ///
    /// Vraie base de TEST, vrais contrôleurs ; chaque relecture passe par un contexte NEUF (rien ne vient du cache
    /// d'EF). `MIMIA_EXPORT_JEU` (facultatif) : chemin où écrire le jeu relu, en JSON comme l'API le rend, pour le
    /// montrer au vrai écran.
    /// </summary>
    public class JeuxTestsEmoticonesEtDateTests : BaseDeTest, IAsyncLifetime
    {
        private readonly List<int> _jeux = [];
        private readonly List<int> _signalements = [];

        private readonly string _categorie = "Zz Émoticônes " + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        // Émoticônes simples, à sélecteur de variante (⏱️), composées (famille, drapeau), et lignes vides.
        private const string TitreJeu = "🧪 Examens blancs 🎓 — réécriture";
        private const string TitreBloc = "📝 Démarrage de l'épreuve ✅";
        private const string Competence =
            "🧪 Ouvre l'examen blanc de maths\n\n✅ Le titre s'affiche\n👉 Clique sur « Commencer »\n⏱️ Le minuteur démarre\n\n👨‍👩‍👧 Famille 🇫🇷";
        private const string Commentaire =
            "🐞 KO au 2e essai\n\n1️⃣ Ouvrir\n2️⃣ Recharger la page\n\n➡️ Le minuteur repart de zéro 😬";
        private const string Description =
            "🔴 Le minuteur repart de zéro\n\n👉 Étapes :\n1️⃣ Ouvrir l'examen\n2️⃣ Recharger\n\n✅ Attendu : il continue";
        private const string CommentaireSignalement = "🛠️ Corrigé sur la branche\n\n⏳ En attente de mise en ligne\n👀 À retester";

        private JeuxDeTestsController Jeux()
        {
            var utilisateur = new Mock<ICurrentUserAccessor>();
            utilisateur.SetupGet(u => u.Prenom).Returns("Testeur");
            utilisateur.SetupGet(u => u.Nom).Returns("Auto");

            return new JeuxDeTestsController(
                new JeuxDeTestsRepository(NouveauContexte()), new Mock<IReferentielService>(MockBehavior.Strict).Object, utilisateur.Object,
                new SignalementRepository(NouveauContexte()), new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                Microsoft.Extensions.Options.Options.Create(new OptionsEmail()), NullLogger<JeuxDeTestsController>.Instance);
        }

        private static T Corps<T>(IActionResult resultat) where T : class =>
            (resultat as ObjectResult)?.Value as T ?? throw new InvalidOperationException($"Réponse inattendue : {resultat}");

        /// <summary>Un jeu des tests généraux (aucune classe du référentiel), un bloc, une ligne.</summary>
        private async Task<(DomainJeu Jeu, TestDuJeu Bloc, LigneDeTest Ligne)> CreerAsync()
        {
            var jeu = Corps<DomainJeu>(await Jeux().Creer(new JeuDeTestRequest { Titre = TitreJeu, Section = "generaux" }, default));
            _jeux.Add(jeu.Id);

            var avecBloc = Corps<DomainJeu>(await Jeux().AjouterTest(jeu.Id, new TestDuJeuRequest
            {
                Titre = TitreBloc, Categorie = _categorie, Nombre = 1,
            }, default));
            var bloc = Assert.Single(avecBloc.Tests);
            return (avecBloc, bloc, Assert.Single(bloc.Lignes));
        }

        private async Task<LigneDeTest> RelireLigneAsync(int jeuId, int ligneId)
        {
            var jeu = Corps<DomainJeu>(await Jeux().Lire(jeuId, default));
            return jeu.Tests.SelectMany(t => t.Lignes).Single(l => l.Id == ligneId);
        }

        [Fact]
        public async Task Emoticones_et_sauts_de_ligne_reviennent_a_l_identique_du_jeu_de_tests()
        {
            var (jeu, bloc, ligne) = await CreerAsync();

            await Jeux().ModifierLigne(jeu.Id, ligne.Id, new LigneDeTestRequest
            {
                Competence = Competence, Statut = StatutsDeTest.ATester, Commentaire = Commentaire,
            }, default);

            // Par l'API (contrôleur neuf, contexte neuf).
            var relu = Corps<DomainJeu>(await Jeux().Lire(jeu.Id, default));
            Assert.Equal(TitreJeu, relu.Titre);
            var blocRelu = Assert.Single(relu.Tests);
            Assert.Equal(TitreBloc, blocRelu.Titre);
            var ligneRelue = Assert.Single(blocRelu.Lignes);
            Assert.Equal(Competence, ligneRelue.Competence);
            Assert.Equal(Commentaire, ligneRelue.Commentaire);

            // Directement en base : les sauts de ligne sont bien des « \n », pas des « ?? » ni un seul paragraphe.
            await using (var c = NouveauContexte())
            {
                var brute = await c.LignesDeTest.AsNoTracking().SingleAsync(l => l.Id == ligne.Id);
                Assert.Equal(Competence, brute.Competence);
                Assert.Equal(Commentaire, brute.Commentaire);
                Assert.Equal(6, brute.Competence.Count(ch => ch == '\n')); // dont deux lignes vides
                Assert.DoesNotContain("??", brute.Competence);
                Assert.Equal(TitreBloc, (await c.TestsDesJeux.AsNoTracking().SingleAsync(t => t.Id == bloc.Id)).Titre);
                Assert.Equal(TitreJeu, (await c.JeuxDeTests.AsNoTracking().SingleAsync(j => j.Id == jeu.Id)).Titre);
            }

            var chemin = Environment.GetEnvironmentVariable("MIMIA_EXPORT_JEU");
            if (!string.IsNullOrWhiteSpace(chemin))
            {
                // Comme l'API : noms en camelCase. Les émoticônes restent écrites telles quelles dans le fichier.
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };
                await File.WriteAllTextAsync(chemin, JsonSerializer.Serialize(relu, options));
            }
        }

        [Fact]
        public async Task Reecrire_le_texte_d_une_ligne_sans_changer_son_statut_laisse_effectue_le_intact()
        {
            var (jeu, _, ligne) = await CreerAsync();

            // Le test est passé : statut OK. Puis on recule sa date à une valeur connue, pour la reconnaître.
            await Jeux().ChangerStatut(jeu.Id, ligne.Id, new StatutLigneRequest { Statut = StatutsDeTest.Ok }, default);
            var passeLe = new DateTime(2026, 11, 20, 9, 15, 0);
            await using (var c = NouveauContexte())
            {
                await c.LignesDeTest.Where(l => l.Id == ligne.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.DateEffectuee, passeLe));
            }

            // Réécriture du texte (compétence ET commentaire), MÊME statut : la date ne bouge pas.
            var r = await Jeux().ModifierLigne(jeu.Id, ligne.Id, new LigneDeTestRequest
            {
                Competence = Competence, Statut = StatutsDeTest.Ok, Commentaire = Commentaire,
            }, default);
            Assert.IsType<OkObjectResult>(r);

            var apresTexte = await RelireLigneAsync(jeu.Id, ligne.Id);
            Assert.Equal(Competence, apresTexte.Competence);
            Assert.Equal(Commentaire, apresTexte.Commentaire);
            Assert.Equal(StatutsDeTest.Ok, apresTexte.Statut);
            Assert.Equal(passeLe, apresTexte.DateEffectuee);

            // Retirer le commentaire, toujours sans changer le statut : la date ne bouge pas non plus.
            await Jeux().ModifierLigne(jeu.Id, ligne.Id, new LigneDeTestRequest
            {
                Competence = Competence, Statut = StatutsDeTest.Ok, Commentaire = "   ",
            }, default);
            var sansCommentaire = await RelireLigneAsync(jeu.Id, ligne.Id);
            Assert.Null(sansCommentaire.Commentaire);
            Assert.Equal(passeLe, sansCommentaire.DateEffectuee);

            // Le statut change (OK → KO) par la même route : là, « Effectué le » prend le moment présent.
            var avant = HeureFrance.Locale(DateTime.UtcNow).AddMinutes(-1);
            await Jeux().ModifierLigne(jeu.Id, ligne.Id, new LigneDeTestRequest
            {
                Competence = Competence, Statut = StatutsDeTest.Ko, Commentaire = Commentaire,
            }, default);
            var apresStatut = await RelireLigneAsync(jeu.Id, ligne.Id);
            Assert.Equal(StatutsDeTest.Ko, apresStatut.Statut);
            Assert.NotNull(apresStatut.DateEffectuee);
            Assert.True(apresStatut.DateEffectuee > avant, $"Date attendue après {avant:O}, lue {apresStatut.DateEffectuee:O}");
        }

        [Fact]
        public async Task Une_ligne_jamais_passee_reecrite_sans_changer_de_statut_reste_sans_date()
        {
            var (jeu, _, ligne) = await CreerAsync();

            await Jeux().ModifierLigne(jeu.Id, ligne.Id, new LigneDeTestRequest
            {
                Competence = Competence, Statut = StatutsDeTest.ATester, Commentaire = Commentaire,
            }, default);

            var relue = await RelireLigneAsync(jeu.Id, ligne.Id);
            Assert.Equal(StatutsDeTest.ATester, relue.Statut);
            Assert.Null(relue.DateEffectuee);
        }

        [Fact]
        public async Task Emoticones_et_sauts_de_ligne_reviennent_a_l_identique_d_un_signalement_et_de_ses_commentaires()
        {
            var parent = await CreerParentAsync();
            var depot = new SignalementRepository(NouveauContexte());
            var signalement = await depot.CreerAdminAsync(parent.Id, "TECHNIQUE", Description, Competence, null);
            _signalements.Add(signalement.Id);
            await depot.AjouterCommentaireAsync(signalement.Id, "testeur@exemple.invalid", CommentaireSignalement);

            var relecture = new SignalementRepository(NouveauContexte());
            var relu = await relecture.GetByIdAsync(signalement.Id);
            Assert.NotNull(relu);
            Assert.Equal(Description, relu!.Description);
            var commentaire = Assert.Single(await relecture.GetCommentairesAsync(signalement.Id));
            Assert.Equal(CommentaireSignalement, commentaire.Texte);

            await using var c = NouveauContexte();
            var brut = await c.Signalements.AsNoTracking().SingleAsync(s => s.Id == signalement.Id);
            Assert.Equal(Description, brut.Description);
            Assert.Equal(Competence, brut.Competence);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await using (var c = NouveauContexte())
            {
                if (_signalements.Count > 0)
                {
                    await c.SignalementCommentaires.Where(s => _signalements.Contains(s.SignalementId)).ExecuteDeleteAsync();
                    await c.Signalements.Where(s => _signalements.Contains(s.Id)).ExecuteDeleteAsync();
                }
                if (_jeux.Count > 0) await c.JeuxDeTests.Where(j => _jeux.Contains(j.Id)).ExecuteDeleteAsync();
            }

            await DisposeAsync();
        }
    }
}
