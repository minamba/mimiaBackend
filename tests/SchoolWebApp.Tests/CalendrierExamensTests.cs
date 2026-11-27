using SchoolWebApp.Api.Services;
using SchoolWebApp.Dal.Seed;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// LES DATES OFFICIELLES DES ÉPREUVES (25/11/2026) : ce que le modèle rend n'est gardé que s'il est rattaché à une
    /// phrase du texte et à un format exact ; une date identique ne se propose pas ; le semis est cohérent.
    /// </summary>
    public class CalendrierExamensTests
    {
        [Fact]
        public void Une_date_complete_est_lue()
        {
            var lues = CalendrierExamensService.Analyser(
                """Voici : {"session": 2027, "dates": [{"code": "DNB_2027_FRANCAIS", "nature": "JOUR", "date": "2027-06-24", "date_fin": null, "heure_debut": "08:00", "heure_fin": "11:15", "remplacement": "2027-09-09", "extrait": "auront lieu les jeudi 24"}]}""",
                out var session);

            Assert.Equal(2027, session);
            var lue = Assert.Single(lues!);
            Assert.Equal("DNB_2027_FRANCAIS", lue.Code);
            Assert.Equal(new DateTime(2027, 6, 24), lue.Date);
            Assert.Equal(new TimeSpan(8, 0, 0), lue.HeureDebut);
            Assert.Equal(new TimeSpan(11, 15, 0), lue.HeureFin);
            Assert.Equal(new DateTime(2027, 9, 9), lue.Remplacement);
        }

        [Fact]
        public void Une_date_sans_extrait_ou_mal_formee_est_ignoree_et_une_nature_inconnue_oubliee()
        {
            var lues = CalendrierExamensService.Analyser(
                """
                {"dates": [
                    {"code": "A", "date": "2027-06-24", "extrait": ""},
                    {"code": "B", "date": "24/06/2027", "extrait": "le 24 juin"},
                    {"code": "C", "date": "2027-06-16", "date_fin": "2027-06-17", "nature": "PEUT_ETRE", "extrait": "le 16 ou le 17"}
                ]}
                """, out _);

            var lue = Assert.Single(lues!);
            Assert.Equal("C", lue.Code);
            Assert.Null(lue.Nature);
            Assert.Equal(new DateTime(2027, 6, 17), lue.DateFin);
        }

        [Fact]
        public void Un_json_illisible_rend_null()
        {
            Assert.Null(CalendrierExamensService.Analyser("pas de json", out _));
            Assert.Null(CalendrierExamensService.Analyser("{ \"dates\": [ ", out _));
        }

        [Fact]
        public void Une_date_identique_n_est_pas_une_nouvelle_date()
        {
            var a = new DateEpreuveOfficielle { Nature = CalendrierExamens.Jour, DateEpreuve = new(2027, 6, 24), HeureDebut = new(8, 0, 0) };
            var b = new DateEpreuveOfficielle { Nature = CalendrierExamens.Jour, DateEpreuve = new(2027, 6, 24, 0, 0, 0), HeureDebut = new(8, 0, 0) };
            var c = new DateEpreuveOfficielle { Nature = CalendrierExamens.Jour, DateEpreuve = new(2027, 6, 25), HeureDebut = new(8, 0, 0) };

            Assert.True(a.MemeDateQue(b));
            Assert.False(a.MemeDateQue(c));
        }

        [Fact]
        public void Le_semis_2027_a_un_code_unique_une_source_et_un_extrait_par_epreuve()
        {
            var dates = CalendrierExamensSeeder.Session2027();

            Assert.Equal(dates.Count, dates.Select(d => d.EpreuveCode).Distinct().Count());
            Assert.All(dates, d =>
            {
                Assert.StartsWith("https://", d.SourceUrl);
                Assert.False(string.IsNullOrWhiteSpace(d.Extrait));
                Assert.Contains(d.Nature, CalendrierExamens.Natures);
                Assert.Equal(2027, d.DateEpreuve.Year);
                Assert.True(d.HeureFin > d.HeureDebut, d.EpreuveCode);
                if (d.Nature == CalendrierExamens.UnDesDeux) Assert.NotNull(d.DateFin);
            });
        }
    }
}
