using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Dal.Repositories;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Tests
{
    /// <summary>Ce que fait l'ANCIEN code (master d'avant le lot) quand un parent clique « me désinscrire ».</summary>
    internal static class RetourArriere
    {
        /// <summary>
        /// Une ligne `DesabonnementMail` « diffusion », datée de maintenant (UTC, comme l'ancien
        /// `EnvoiAutomatiqueRepository.DesabonnerAsync`), sans toucher à `OffresEmail`. Rend la date relue en base.
        /// </summary>
        public static async Task<DateTime> DesinscriptionParLAncienCodeAsync(Func<SchoolWebAppDatabaseContext> contexte, int parentId)
        {
            await using var c = contexte();
            var ligne = new DesabonnementMail { ParentId = parentId, Categorie = CategorieDesabonnement.Diffusion, DateCreation = DateTime.UtcNow };
            c.DesabonnementsMail.Add(ligne);
            await c.SaveChangesAsync();

            await using var relecture = contexte();
            return await relecture.DesabonnementsMail.Where(d => d.Id == ligne.Id).Select(d => d.DateCreation).SingleAsync();
        }
    }

    /// <summary>
    /// LE PIÈGE DU RETOUR ARRIÈRE (20/11/2026, revue d'Omar, majeur 2) : un parent désinscrit par l'ancien code pendant un
    /// retour arrière garde `OffresEmail = 1`. À la remise en ligne du lot, il ne doit RIEN recevoir d'offre, sans aucun
    /// SQL manuel ; et l'information de service, elle, lui parvient toujours.
    /// </summary>
    public class RetourArriereTests : BaseDeTest
    {
        private const string Offres = CategorieDesabonnement.Diffusion;

        [Fact]
        public async Task Desinscrit_par_l_ancien_code_ne_recoit_plus_aucune_offre()
        {
            var parent = await CreerParentAsync(consenti: true);
            var temoin = await CreerParentAsync(consenti: true);
            await RetourArriere.DesinscriptionParLAncienCodeAsync(NouveauContexte, parent.Id);

            Assert.True((await RelireAsync(parent.Id)).OffresEmail); // l'ancien code n'y a pas touché

            var diffusion = (await Envois.GetDestinatairesDiffusionAsync()).Select(d => d.ParentId).ToList();
            Assert.DoesNotContain(parent.Id, diffusion);
            Assert.Contains(temoin.Id, diffusion);

            var cible = (await Envois.GetDestinatairesCibleAsync(CibleEnvoi.Tous, Offres, DateTime.UtcNow)).Select(d => d.ParentId).ToList();
            Assert.DoesNotContain(parent.Id, cible);
            Assert.Contains(temoin.Id, cible);

            Assert.Null(await Envois.GetDestinataireParMailAsync(parent.Mail!, Offres));
            Assert.True(await Envois.EstDesabonneAsync(parent.Id, Offres));
            Assert.False(await Envois.EstDesabonneAsync(temoin.Id, Offres));

            // L'information de service n'est pas une offre : elle part quand même.
            Assert.Contains(parent.Id, (await Envois.GetDestinatairesInformationServiceAsync()).Select(d => d.ParentId));
        }

        [Fact]
        public async Task Une_ligne_anterieure_a_l_accord_ne_compte_pas()
        {
            // L'ancien désabonnement des « annonces », dépassé par la case cochée depuis (comportement inchangé).
            var parent = await CreerParentAsync(consenti: true);
            await using (var c = NouveauContexte())
            {
                c.DesabonnementsMail.Add(new DesabonnementMail { ParentId = parent.Id, Categorie = Offres, DateCreation = DateTime.UtcNow.AddDays(-3) });
                await c.SaveChangesAsync();
            }

            Assert.Contains(parent.Id, (await Envois.GetDestinatairesDiffusionAsync()).Select(d => d.ParentId));
            Assert.False(await Envois.EstDesabonneAsync(parent.Id, Offres));
        }

        [Fact]
        public async Task Desinscription_apres_le_retour_arriere_inscrit_le_retrait_a_sa_date()
        {
            var parent = await CreerParentAsync(consenti: true);
            var date = await RetourArriere.DesinscriptionParLAncienCodeAsync(NouveauContexte, parent.Id);

            Assert.False(await Envois.DesabonnerAsync(parent.Id, Offres, SourceConsentement.UnClic));

            var relu = await RelireAsync(parent.Id);
            Assert.False(relu.OffresEmail);
            Assert.Equal(SourceConsentement.RetourArriere, relu.OffresEmailSource);
            Assert.Equal(date, relu.OffresEmailRetireLe!.Value, TimeSpan.FromMilliseconds(5));

            var journal = Assert.Single(await JournalAsync(parent.Id));
            Assert.False(journal.Consenti);
            Assert.Equal(SourceConsentement.RetourArriere, journal.Source);
        }

        [Fact]
        public async Task Nouvel_accord_depuis_l_espace_parent_apres_le_retour_arriere()
        {
            var parent = await CreerParentAsync(consenti: true);
            await RetourArriere.DesinscriptionParLAncienCodeAsync(NouveauContexte, parent.Id);

            await Envois.ReabonnerAsync(parent.Id, Offres, SourceConsentement.EspaceParent);

            var relu = await RelireAsync(parent.Id);
            Assert.True(relu.OffresEmail);
            Assert.Equal(SourceConsentement.EspaceParent, relu.OffresEmailSource);
            Assert.True(relu.OffresEmailConsentiLe > relu.OffresEmailRetireLe);
            Assert.False(await Envois.EstDesabonneAsync(parent.Id, Offres));
            Assert.Contains(parent.Id, (await Envois.GetDestinatairesDiffusionAsync()).Select(d => d.ParentId));

            // Le retrait, puis le nouvel accord : toute l'histoire est au journal.
            var journal = await JournalAsync(parent.Id);
            Assert.Equal([false, true], journal.Select(j => j.Consenti));
            Assert.Equal([SourceConsentement.RetourArriere, SourceConsentement.EspaceParent], journal.Select(j => j.Source));
        }

        [Fact]
        public async Task Nouvel_accord_dans_la_fenetre_des_conditions_apres_le_retour_arriere()
        {
            var parent = await CreerParentAsync(consenti: true);
            await RetourArriere.DesinscriptionParLAncienCodeAsync(NouveauContexte, parent.Id);

            await new ParentRepository(Contexte).ConsentirOffresEmailAsync(parent.Id, DateTime.UtcNow.AddSeconds(1), SourceConsentement.FenetreConditions);

            Assert.False(await Envois.EstDesabonneAsync(parent.Id, Offres));
            Assert.Equal(
                [SourceConsentement.RetourArriere, SourceConsentement.FenetreConditions],
                (await JournalAsync(parent.Id)).Select(j => j.Source));
        }

        [Fact]
        public async Task Le_consentement_marche_aussi_avec_une_strategie_a_nouvelles_tentatives()
        {
            // « À fournir » d'Omar : avec `EnableRetryOnFailure`, un `BeginTransaction` ouvert hors de la stratégie lève
            // une exception. La prod ne l'active pas (Program.cs) ; le code passe quand même par la stratégie.
            var parent = await CreerParentAsync(consenti: true);
            var options = new DbContextOptionsBuilder<SchoolWebAppDatabaseContext>()
                .UseSqlServer(Contexte.Database.GetConnectionString(), o => o.EnableRetryOnFailure())
                .Options;

            await using var contexte = new SchoolWebAppDatabaseContext(options);
            var envois = new EnvoiAutomatiqueRepository(contexte);

            Assert.True(await envois.DesabonnerAsync(parent.Id, Offres, SourceConsentement.LienCourriel));
            await envois.ReabonnerAsync(parent.Id, Offres, SourceConsentement.EspaceParent);

            Assert.True((await RelireAsync(parent.Id)).OffresEmail);
            Assert.Equal([false, true], (await JournalAsync(parent.Id)).Select(j => j.Consenti));
        }
    }
}
