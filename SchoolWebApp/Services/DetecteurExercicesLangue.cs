using System.Text.RegularExpressions;
using SchoolWebApp.Api.Services.Prompts;
using DomainMessage = SchoolWebApp.Domain.Models.Message;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// QUELS EXERCICES DE LANGUE CONCERNENT CE TOUR — donc quelles consignes
    /// complètes envoyer au professeur.
    ///
    /// Voulu par Camara le 19/09/2026, après la mesure du coût : les consignes
    /// détaillées des quatre exercices (~23 000 jetons) étaient relues à chaque
    /// tour de langue, même sans exercice. Elles ne sont plus envoyées que
    /// quand elles servent — voir <see cref="PromptsPedagogiques.ExercicesLangue"/>.
    ///
    /// UN FAUX POSITIF COÛTE UN PEU, UN FAUX NÉGATIF COÛTE LA QUALITÉ, et c'est
    /// Camara qui a fixé la priorité : « on perdrait en qualité, c'est pas
    /// possible ». Tout le réglage penche donc vers le chargement. Un mot qui
    /// évoque l'exercice suffit ; il faut qu'aucune trace ne subsiste pour que
    /// la section parte.
    ///
    /// ON LIT TOUTE LA FENÊTRE D'HISTORIQUE ENVOYÉE, ET NON LES DERNIERS
    /// MESSAGES — c'est ce qui rend le déchargement presque gratuit. La fenêtre
    /// avance par paliers de vingt messages ; une section ne part donc qu'au
    /// moment où sa dernière trace sort de la fenêtre, c'est-à-dire pendant un
    /// saut qui réécrit l'historique de toute façon. Décharger dix messages
    /// après la fin d'un exercice aurait forcé une réécriture de plus.
    /// </summary>
    public static partial class DetecteurExercicesLangue
    {
        [GeneratedRegex(
            @"\[/?DICTEE|dict[ée]e|dictation|dictado|dettato|diktat|听写",
            RegexOptions.IgnoreCase)]
        private static partial Regex Dictee();

        /// <summary>
        /// PAS « écoute » TOUT SEUL : le professeur dit « je t'écoute » à longueur
        /// de séance. Le compter aurait gardé la section chargée en permanence,
        /// et tout le gain se serait envolé sur ce seul mot.
        /// </summary>
        [GeneratedRegex(
            @"\[/?COMPREHENSION_(?:ORALE|SUPPRIMEE)|\[VITESSE\]"
            + @"|compr[ée]hension\s+orale|exercice\s+d.[ée]coute"
            + @"|[ée]cout(?:e|er)\s+(?:un|le|ce|ton|mon)\s+(?:texte|passage|extrait)"
            + @"|listening|comprensi[óo]n\s+oral",
            RegexOptions.IgnoreCase)]
        private static partial Regex Ecoute();

        [GeneratedRegex(
            @"\[CONVERSATION\]|\[/?EXPRESSION_ORALE|expression\s+orale|conversation|speaking",
            RegexOptions.IgnoreCase)]
        private static partial Regex ExpressionOrale();

        /// <summary>
        /// La consigne au tableau compte aussi : un professeur qui reprend un
        /// texte d'une séance passée l'y remet souvent sans aucun autre signe.
        /// </summary>
        [GeneratedRegex(
            @"\[SUPPORT_ECRIT\]|\[/?EXPRESSION_ECRITE|\[TEXTE\s+(?:AU|RENDU)"
            + @"|expression\s+[ée]crite|texte\s+[ée]crit|r[ée]daction|r[ée]dige|writing"
            + @"|\[ARDOISE\][^\[]*?^[ \t]*la\s+consigne[ \t]*:?[ \t]*\r?$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline)]
        private static partial Regex ExpressionEcrite();

        /// <summary>
        /// LA LISTE DES DICTÉES ARCHIVÉES, injectée à chaque arrivée de l'élève —
        /// à écarter avant de chercher.
        ///
        /// ELLE PARLE DE DICTÉE SANS QU'IL Y EN AIT UNE : c'est un sommaire, posé
        /// là au cas où l'élève voudrait y revenir. La compter aurait chargé la
        /// dictée au premier tour de chaque séance, puis l'aurait déchargée au
        /// tour suivant — deux réécritures de cache à chaque arrivée, pour rien.
        ///
        /// Le bloc se termine toujours par la dernière ligne de la liste, qui dit
        /// « corrigée » ou « en attente de correction », suivie du crochet : on
        /// s'arrête à la première de ces fins, qui est forcément la dernière
        /// ligne. Un simple <c>\[[^\]]*\]</c> se serait arrêté au crochet de la
        /// balise d'exemple qu'il contient.
        ///
        /// LA DICTÉE EN ATTENTE, ELLE, N'EST PAS ÉCARTÉE : « une dictée attend sa
        /// correction » est un vrai travail à reprendre, et le professeur a
        /// besoin de ses consignes pour le faire.
        /// </summary>
        [GeneratedRegex(
            @"\[Ses dictées archivées[\s\S]*?(?:corrigée|en attente de correction)\]",
            RegexOptions.IgnoreCase)]
        private static partial Regex ListeDesDictees();

        /// <summary>
        /// LA PHRASE DU MINUTEUR QUI PARLE DE DICTÉE — à écarter avant de chercher.
        ///
        /// « Une dictée demandée maintenant fait N lignes » part avec CHAQUE tour
        /// tant qu'il reste du temps. Trouvé le 19/09/2026, mesure à l'appui : la
        /// séance de français avait écrit 22 000 jetons de consignes au deuxième
        /// tour, soit l'expression écrite demandée (6 000) PLUS la dictée (16 000)
        /// que personne n'avait demandée. Le mot suffisait au détecteur. Tous les
        /// cours de langue, exercice ou pas, relisaient ces 16 000 jetons à chaque
        /// tour.
        /// </summary>
        [GeneratedRegex(@"Une dictée demandée maintenant fait \d+ lignes[^.\]]*\.", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseDuMinuteur();

        /// <summary>
        /// Le marqueur qui clôt une séance : ce qui le précède appartient à une
        /// autre séance.
        /// </summary>
        [GeneratedRegex(@"\[FIN_SEANCE\]", RegexOptions.IgnoreCase)]
        private static partial Regex FinDeSeance();

        /// <summary>
        /// Combien de messages récents de la séance on relit : dix échanges.
        ///
        /// ASSEZ POUR UN EXERCICE EN COURS : ses balises reviennent à chaque
        /// étape — [DICTEE] à chaque phrase dictée, [CONVERSATION] à chaque
        /// réplique, et la correction d'une expression écrite porte son rappel à
        /// chaque tour. Et le résumé permanent des exercices, qui dit comment
        /// chacun démarre, reste dans le prompt quoi qu'il arrive.
        /// </summary>
        private const int MessagesRecents = 20;

        /// <summary>
        /// Les exercices dont les consignes doivent accompagner ce tour.
        ///
        /// LA SÉANCE EN COURS ET SES DERNIERS ÉCHANGES, PAS TOUT L'HISTORIQUE —
        /// corrigé le 19/09/2026, mesure à l'appui. La fenêtre d'historique
        /// remonte sur les séances précédentes : dans une expression écrite, les
        /// dictées et les conversations des jours d'avant chargeaient les
        /// consignes des QUATRE exercices, ~24 000 jetons relus à chaque tour
        /// dont ~19 000 pour rien. Le simple mot « conversation » d'un autre jour
        /// suffisait à charger l'expression orale.
        /// </summary>
        public static IReadOnlyCollection<PromptsPedagogiques.ExerciceLangue> Concernes(
            IEnumerable<DomainMessage> historique, string? messageEleve)
        {
            var liste = historique.ToList();

            // La séance commence après le dernier [FIN_SEANCE] du professeur.
            // S'il est le dernier message, l'élève en ouvre une nouvelle : rien
            // de l'ancienne ne compte, et c'est juste.
            var debut = 0;
            for (var i = liste.Count - 1; i >= 0; i--)
            {
                if (liste[i].Role == "assistant" && FinDeSeance().IsMatch(liste[i].Contenu ?? string.Empty))
                {
                    debut = i + 1;
                    break;
                }
            }

            var textes = liste
                .Skip(Math.Max(debut, liste.Count - MessagesRecents))
                .Select(m => m.Contenu ?? string.Empty)
                .Append(PhraseDuMinuteur().Replace(ListeDesDictees().Replace(messageEleve ?? string.Empty, " "), " "))
                .ToList();

            var concernes = new List<PromptsPedagogiques.ExerciceLangue>();

            if (textes.Any(t => Dictee().IsMatch(t)))
                concernes.Add(PromptsPedagogiques.ExerciceLangue.Dictee);

            if (textes.Any(t => Ecoute().IsMatch(t)))
                concernes.Add(PromptsPedagogiques.ExerciceLangue.Ecoute);

            if (textes.Any(t => ExpressionOrale().IsMatch(t)))
                concernes.Add(PromptsPedagogiques.ExerciceLangue.ExpressionOrale);

            if (textes.Any(t => ExpressionEcrite().IsMatch(t)))
                concernes.Add(PromptsPedagogiques.ExerciceLangue.ExpressionEcrite);

            return concernes;
        }
    }
}
