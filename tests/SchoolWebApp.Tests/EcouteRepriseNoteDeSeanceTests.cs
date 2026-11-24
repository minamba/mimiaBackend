using SchoolWebApp.Api.Services.Voix;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// SIGNALEMENT #64 (Adam, 23/11/2026) — écoute des mots en anglais : au retour après « Quitter », Marine redisait
    /// « orange » au lieu de « Two », le mot qui attendait sa réponse. La note de séance écrite au départ ([FICHE] …
    /// « tout comme [EN]orange[/EN] » … [FIN_SEANCE]) passait pour le dernier mot dicté. Fil réel (conversation de test),
    /// raccourci : le message d'accueil, la demande, Two, le refus, Two redit, puis la note du départ.
    /// </summary>
    public class EcouteRepriseNoteDeSeanceTests
    {
        private const string Note =
            "[FICHE]\nnotion: Répéter un mot en respectant les sons anglais\ncontenu:\n## Un exemple\n"
            + "Le mot anglais [EN]blue[/EN] a été très bien réussi par Adam après une ou deux réécoutes, tout comme [EN]orange[/EN].\n[/FICHE]\n\n"
            + "[RAPPORT]\ntravaille: vocabulaire des nombres en anglais\n[/RAPPORT]\n\n[FIN_SEANCE]";

        private static readonly List<string> Fil =
        [
            "[TABLEAU_EFFACE]\n\nSalut Adam ! On continue avec les mots anglais ?\n\nVoici le prochain mot.\n\n[EN]Seven[/EN]\n\nQu'as-tu entendu ?",
            "On fait les nombres ? Tu me dis two ?",
            "D'accord Adam, on passe aux nombres !\n\nVoici le prochain mot.\n\n[EN]Two[/EN]\n\nQu'as-tu entendu ?",
            "J'ai entendu le mot deux (en français).",
            "Deux, c'est le mot en français : tu as compris le sens.\n\nJe te le redis.\n\n[EN]Two[/EN]\n\nQu'as-tu entendu ?",
            Note,
        ];

        [Fact]
        public void Le_dernier_mot_dicte_n_est_jamais_pris_dans_une_note_de_seance()
        {
            Assert.Equal("Two", RegleVerificationSon.DernierMotDicte(Fil));
            Assert.Null(RegleVerificationSon.DernierMotDicte([Note]));
        }

        [Theory]
        [InlineData("On reprend l'écoute des mots.\n\n[EN]Two[/EN]\n\nQu'as-tu entendu ?")]
        [InlineData("On reprend l'écoute des mots.")]
        [InlineData("On reprend l'écoute des mots.\n\n[EN]Orange[/EN]\n\nQu'as-tu entendu ?")]
        public void Au_retour_le_mot_attendu_est_redit_meme_si_la_note_du_depart_porte_une_balise_de_voix(string reponseDuModele)
        {
            var filtre = new FiltreMotsVerifies(LanguesDesSons.Anglais, new List<string>(), Fil, 7, niveau: "CP",
                redire: RegleVerificationSon.RedireLeMot.Toujours, arrivee: true);

            var ecran = filtre.Passer(reponseDuModele) + filtre.Terminer();
            var garde = filtre.Corriger(reponseDuModele);

            Assert.Equal("Two", DicteeDeMots.MotDit(ecran));
            Assert.Equal("Two", DicteeDeMots.MotDit(garde));
            Assert.DoesNotContain("orange", garde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Hors_du_tour_d_arrivee_la_note_du_depart_ferme_toujours_l_ecoute()
        {
            // Inchangé : sans rechargement, la note du départ (avec sa balise de voix) ferme l'écoute — voir motAReprendre.
            Assert.False(RegleVerificationSon.EcouteEnAttente(Fil));
            Assert.True(RegleVerificationSon.EcouteEnAttente(Fil.Take(Fil.Count - 1).ToList()));
        }
    }
}
