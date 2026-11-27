using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Prompts;
using DomainMessage = SchoolWebApp.Domain.Models.Message;

namespace SchoolWebApp.Tests
{
    /// <summary>
    /// LES CONSIGNES D'EXERCICE CHARGÉES À TORT (27/11/2026) : la règle des questions complètes, ajoutée à chaque tour
    /// depuis le 21/11, contient « plus haut dans la conversation » ; le détecteur y voyait une expression orale et
    /// chargeait ses consignes (~4 400 jetons, « EXERCICES EN COURS ») à chaque tour de tous les cours de langue,
    /// français compris. Mesuré sur un contrôle de français réel (5,20 $ pour 1 h 20).
    /// </summary>
    public class DetecteurExercicesLangueTests
    {
        private static readonly DomainMessage[] AucunHistorique = [];

        [Fact]
        public void La_regle_des_questions_completes_ne_charge_aucun_exercice()
        {
            var concernes = DetecteurExercicesLangue.Concernes(
                AucunHistorique, "Ok.\n\n" + PromptsPedagogiques.RegleDesQuestionsCompletes);

            Assert.Empty(concernes);
        }

        /// <summary>
        /// LE RETOUR (27/11/2026, empreintes de la prod, conversation 12) : à chaque arrivée, le fait du tableau effacé
        /// (« un texte à dicter ne va jamais au tableau ») chargeait les consignes de dictée, retirées au tour suivant.
        /// </summary>
        [Fact]
        public void Le_tableau_efface_a_l_arrivee_ne_charge_pas_la_dictee()
        {
            var arrivee = string.Join("\n\n",
                PromptsPedagogiques.DeclencheurDeRetour(TimeSpan.FromMinutes(7)),
                PromptsPedagogiques.FaitTableauEfface,
                PromptsPedagogiques.RegleDesQuestionsCompletes);

            Assert.Empty(DetecteurExercicesLangue.Concernes(AucunHistorique, arrivee));
        }

        /// <summary>Une dictée réellement en cours (son bloc dans l'historique) charge toujours ses consignes.</summary>
        [Fact]
        public void Une_vraie_dictee_charge_toujours_ses_consignes()
        {
            DomainMessage[] historique =
            [
                new DomainMessage { Role = "assistant", Contenu = "On commence. [DICTEE]Le chat dort.[/DICTEE]" },
            ];

            var concernes = DetecteurExercicesLangue.Concernes(
                historique, "J'ai fini.\n\n" + PromptsPedagogiques.FaitTableauEfface);

            Assert.Contains(PromptsPedagogiques.ExerciceLangue.Dictee, concernes);
        }

        [Fact]
        public void Une_vraie_demande_de_conversation_charge_toujours_l_expression_orale()
        {
            var concernes = DetecteurExercicesLangue.Concernes(
                AucunHistorique, "On peut faire une conversation en anglais ?\n\n" + PromptsPedagogiques.RegleDesQuestionsCompletes);

            Assert.Contains(PromptsPedagogiques.ExerciceLangue.ExpressionOrale, concernes);
        }

        /// <summary>
        /// LE GARDE-FOU : les rappels qui partent à chaque tour HORS de tout exercice de langue ne doivent déclencher
        /// aucun exercice. Un nouveau rappel de ce genre s'ajoute ici ; s'il cite « dictée », « conversation »,
        /// « compréhension orale » ou « expression écrite », ce test le dit avant la facture.
        /// </summary>
        [Fact]
        public void Aucun_rappel_envoye_hors_exercice_ne_declenche_un_exercice()
        {
            var rappels = string.Join("\n\n",
                "Je ne sais pas.",
                PromptsPedagogiques.RegleDesQuestionsCompletes,
                PromptsPedagogiques.FaitTableauEfface,
                PromptsPedagogiques.RegleDesTroisEssais,
                PromptsPedagogiques.RegleDesIndices,
                PromptsPedagogiques.RegleDesReponsesFausses,
                PromptsPedagogiques.TonDesTracesEcrites,
                PromptsPedagogiques.MarqueurEvaluationEnCours(),
                PromptsPedagogiques.MarqueurGenre("Adrien", "adrien"),
                PromptsPedagogiques.MarqueurDateDuJour(new DateTime(2026, 11, 27, 18, 0, 0, DateTimeKind.Utc)),
                PromptsPedagogiques.DeclencheurDeRetour(TimeSpan.FromMinutes(7)),
                ReponseCourte.Rappel,
                InvitationAContinuer.Rappel,
                ChangementDExercice.RappelDeSuite,
                ChangementDExercice.RappelHistoire);

            Assert.Empty(DetecteurExercicesLangue.Concernes(AucunHistorique, rappels));
        }
    }
}
