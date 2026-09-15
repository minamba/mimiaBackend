using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// UNE COPIE NOTÉE EST UNE MESURE, PAS UNE IMPRESSION.
    ///
    /// LE DÉFAUT QUE CE FICHIER CORRIGE
    /// --------------------------------
    /// Relevé par Camara le 13/09/2026. Bilal passe une évaluation sur
    /// « Utiliser la réciproque de Thalès » et obtient 17,5/20 — trois
    /// questions justes, une partielle. Sa fiche affiche 59 %, « en cours
    /// d'acquisition ». Il a rafraîchi la page en croyant à un problème
    /// d'affichage ; il n'y en avait aucun.
    ///
    /// Rejoué depuis la base, le compte est sans appel : la séance du contrôle
    /// a bien produit une observation, et cette observation était
    /// « hésitant ». Le score est donc PASSÉ DE 0,648 À 0,595 — le 17,5/20 lui
    /// a fait perdre cinq points de maîtrise.
    ///
    /// La cause n'est pas une erreur de calcul. Le bloc [EVALUATION] est
    /// retiré du contexte de l'observateur (voir ObservateurCompetencesService,
    /// « Nettoyer ») : il juge les échanges, pas la conclusion du professeur.
    /// Il a donc lu la conversation, vu l'élève répondre à moitié à une
    /// question avant d'être relancé, et conclu à une hésitation. Ce n'est pas
    /// déraisonnable — c'est ce que la conversation montre. Mais la mesure,
    /// elle, disait autre chose, et personne ne l'écoutait.
    ///
    /// CE QUI REND CETTE INVERSION INTENABLE
    /// -------------------------------------
    /// Une évaluation est le SEUL moment où l'on mesure vraiment : l'élève
    /// répond seul, sans guidage, sur une notion nommée, et chaque réponse
    /// reçoit un verdict. C'est l'évidence la plus forte dont le produit
    /// dispose. Elle pesait exactement autant qu'une impression de bavardage,
    /// et pouvait être écrasée par elle.
    ///
    /// Depuis le 13/09/2026, le professeur ne commente plus rien pendant un
    /// contrôle (voir MarqueurEvaluationEnCours). La conversation d'une séance
    /// d'évaluation ne porte donc PLUS AUCUN signal exploitable, pendant que la
    /// copie les porte tous. Continuer à juger sur l'impression reviendrait à
    /// juger sur du silence.
    ///
    /// CE QUE FAIT CE FICHIER, ET CE QU'IL NE FAIT PAS
    /// -----------------------------------------------
    /// Il transforme les verdicts d'une copie en observations, une par
    /// question. Il ne touche NI au moteur bayésien, NI aux seuils : quatre
    /// questions valent quatre observations parce que ce sont quatre réponses
    /// réelles, pas parce qu'on aurait décidé de peser plus lourd. La nuance
    /// « partiel » se traduit en « hésitant », qui existait déjà pour ça.
    ///
    /// Il est PUR et sans dépendance : la traduction d'un verdict en
    /// observation est une règle, et une règle se vérifie sans base de données.
    /// </summary>
    public static class ObservationsDeLEvaluation
    {
        /// <summary>
        /// Les verdicts de la copie, traduits en observations sur la compétence
        /// évaluée.
        ///
        /// Liste vide si la notion ne se rattache à aucune compétence du
        /// référentiel : on ne devine pas. Une évaluation sur une notion hors
        /// référentiel garde sa note et sa copie — c'est seulement la maîtrise
        /// qui ne bouge pas, faute de savoir de quoi elle parlerait.
        /// </summary>
        /// <param name="code">
        /// Le code de la compétence reconnue, déjà rapproché par l'appelant —
        /// c'est lui qui a la liste du référentiel, pas ce module.
        /// </param>
        public static List<ObservationCompetence> Pour(
            string? code, IEnumerable<QuestionEvaluation> questions)
        {
            if (string.IsNullOrWhiteSpace(code)) return [];

            var observations = new List<ObservationCompetence>();

            foreach (var question in questions)
            {
                var resultat = Traduire(question.Verdict);

                // UNE QUESTION SANS VERDICT LISIBLE NE COMPTE PAS.
                //
                // Même règle que pour le calcul de la note (voir
                // LecteurEvaluation.NoteCalculee) : on écarte la question
                // douteuse plutôt que de la compter comme une faute. Un oubli
                // de verdict n'est pas une erreur de l'élève.
                if (resultat is null) continue;

                observations.Add(new ObservationCompetence(
                    code, resultat.Value, Abreger(question.Enonce)));
            }

            return observations;
        }

        /// <summary>
        /// Le verdict d'une question, dans le vocabulaire du suivi.
        ///
        /// « partiel » devient « hésitant » et non une demi-réussite : le
        /// moteur ne connaît que trois valeurs, et « hésitant » dit exactement
        /// ce qu'est une réponse à moitié juste — ni un échec, ni une maîtrise.
        /// </summary>
        public static ResultatObservation? Traduire(string? verdict) =>
            (verdict ?? "").Trim().ToLowerInvariant() switch
            {
                "juste" => ResultatObservation.Reussi,
                "partiel" => ResultatObservation.Hesitant,
                "faux" => ResultatObservation.Echoue,
                _ => null,
            };

        /// <summary>
        /// L'énoncé sert d'indice de traçabilité, pas de contenu : on le coupe
        /// pour ne pas faire grossir la ligne de maîtrise avec un problème
        /// entier.
        /// </summary>
        private static string? Abreger(string? enonce)
        {
            var propre = (enonce ?? "").Trim();
            if (propre.Length == 0) return null;

            return propre.Length <= 120 ? propre : propre[..117] + "…";
        }
    }
}
