using SchoolWebApp.Api.Services;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// DÉLAI DES REGEX DE LA LECTURE DU SUPPORT (#68, s13, 🟠 d'Omar) — `ConstructionDeChoix` et
    /// `ConfirmationDuSupport` ont un délai de 200 ms. Sous forte charge (1er appel après un redémarrage, VPS
    /// partagé, harnais lancé pendant un build), le délai peut être dépassé : `RegexMatchTimeoutException`. Elle
    /// ne doit jamais remonter jusqu'au tour de l'élève : `SupportDit` et `SupportNommeAvantDepart` rendent alors
    /// `false` (pas de choix lu, pas de confirmation lue), et leurs appelants (`SupportChoisi`, `TopDepartAuTour`)
    /// ne lèvent rien.
    ///
    /// Le dépassement est FORCÉ par `LecteurEvaluation.DelaiDepasseForcePourTest` (AsyncLocal : limité à ce test).
    /// Chaque cas vérifie d'abord la lecture normale (vraie), puis la même lecture sous délai dépassé (fausse).
    /// </summary>
    public class LecteurEvaluationDelaiRegexTests
    {
        private const string Confirmation = "Très bien, au cahier ! Question 1 : combien font 7 × 8 ?";

        private static readonly (string? Role, string? Contenu)[] FilChoixTape =
        {
            ("assistant", "Tu préfères composer au cahier ou à l'ordinateur ? " + LecteurEvaluation.BaliseSupport),
            ("user", "au cahier"),
        };

        private static T SousDelaiDepasse<T>(Func<T> lecture)
        {
            LecteurEvaluation.DelaiDepasseForcePourTest.Value = true;
            try { return lecture(); }
            finally { LecteurEvaluation.DelaiDepasseForcePourTest.Value = false; }
        }

        [Fact]
        public void Par_defaut_le_delai_n_est_pas_force()
        {
            Assert.False(LecteurEvaluation.DelaiDepasseForcePourTest.Value);
        }

        [Theory]
        [InlineData("au cahier")]
        [InlineData("à l'oral stp")]
        [InlineData("oui l'ordi stp")]
        [InlineData("je vais prendre le cahier")]
        public void SupportDit_rend_faux_sans_exception_si_le_delai_est_depasse(string choix)
        {
            Assert.True(LecteurEvaluation.SupportDit(choix));

            var ex = Record.Exception(() => SousDelaiDepasse(() => LecteurEvaluation.SupportDit(choix)));
            Assert.Null(ex);
            Assert.False(SousDelaiDepasse(() => LecteurEvaluation.SupportDit(choix)));

            // Le forçage s'arrête avec le test : la lecture normale revient.
            Assert.True(LecteurEvaluation.SupportDit(choix));
        }

        [Theory]
        [InlineData(Confirmation)]
        [InlineData("D'accord, tu composes ici à l'écran. Question 1 : que vaut k ?")]
        [InlineData("**Très bien, au cahier !** Question 1 : combien font 7 × 8 ?")]
        public void SupportNommeAvantDepart_rend_faux_sans_exception_si_le_delai_est_depasse(string reponse)
        {
            Assert.True(LecteurEvaluation.SupportNommeAvantDepart(reponse));

            var ex = Record.Exception(() => SousDelaiDepasse(() => LecteurEvaluation.SupportNommeAvantDepart(reponse)));
            Assert.Null(ex);
            Assert.False(SousDelaiDepasse(() => LecteurEvaluation.SupportNommeAvantDepart(reponse)));
        }

        [Fact]
        public void SupportChoisi_ne_leve_rien_et_attend_le_clic_si_le_delai_est_depasse()
        {
            Assert.True(LecteurEvaluation.SupportChoisi(FilChoixTape));

            var ex = Record.Exception(() => SousDelaiDepasse(() => LecteurEvaluation.SupportChoisi(FilChoixTape)));
            Assert.Null(ex);
            Assert.False(SousDelaiDepasse(() => LecteurEvaluation.SupportChoisi(FilChoixTape)));
        }

        [Fact]
        public void SupportChoisi_par_le_clic_reste_lu_si_le_delai_est_depasse()
        {
            // Le clic est un fait de l'écran, sans regex à délai : il compte toujours.
            var fil = new (string? Role, string? Contenu)[]
            {
                ("assistant", "Cahier ou ordinateur ? " + LecteurEvaluation.BaliseSupport),
                ("user", "[ÉVALUATION AU CAHIER — l'élève a cliqué]"),
            };
            Assert.True(SousDelaiDepasse(() => LecteurEvaluation.SupportChoisi(fil)));
        }

        [Fact]
        public void TopDepartAuTour_ne_leve_rien_si_le_delai_est_depasse()
        {
            Assert.True(LecteurEvaluation.TopDepartAuTour(false, true, Confirmation));

            var ex = Record.Exception(() => SousDelaiDepasse(() => LecteurEvaluation.TopDepartAuTour(false, true, Confirmation)));
            Assert.Null(ex);
            // Confirmation non lue : pas de top départ par le filet…
            Assert.False(SousDelaiDepasse(() => LecteurEvaluation.TopDepartAuTour(false, true, Confirmation)));
            // … mais un support déjà choisi (clic) ouvre toujours le contrôle.
            Assert.True(SousDelaiDepasse(() => LecteurEvaluation.TopDepartAuTour(true, true, Confirmation)));
        }
    }
}
