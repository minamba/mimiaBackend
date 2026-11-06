using SchoolWebApp.Domain.Models;
using DomainMessage = SchoolWebApp.Domain.Models.Message;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// DES MOTS OU DES PHRASES — voulu par Camara le 01/11/2026.
    ///
    /// Remonté par un parent : son fils, en CP, révisait l'écriture des nombres
    /// de un à cinq ; le professeur a pris ces mots dans des phrases, qu'un
    /// enfant de CP ne sait pas encore écrire. La dictée de mots existe du CP
    /// au collège ; au lycée, une dictée est faite de phrases.
    ///
    /// Avant toute NOUVELLE dictée, le professeur pose la question avec la
    /// balise <c>[TYPE_DICTEE]</c>, l'écran affiche deux boutons, et le choix
    /// lui revient comme un FAIT accroché au message de l'élève
    /// (<c>src/lib/storage/typeDictee.js</c> — les débuts des deux faits
    /// doivent rester identiques des deux côtés).
    ///
    /// LA CONSIGNE EST DANS LE BLOC DES DICTÉES ; CE RAPPEL LA PORTE À CHAQUE
    /// TOUR. C'est la leçon de toujours (« une règle de prompt ne vaut pas un
    /// fait ») : la règle, lue une fois dans un bloc de onze mille jetons, ne
    /// pèse rien au moment où l'élève dit « on fait une dictée ».
    /// </summary>
    public static partial class TypeDeDictee
    {
        public const string Balise = "[TYPE_DICTEE]";

        /// <summary>Les débuts des deux faits écrits par l'écran.</summary>
        public const string FaitMots = "[DICTÉE DE MOTS";
        public const string FaitPhrases = "[DICTÉE DE PHRASES";

        private const string ChoisieMots =
            "\n\n[DICTÉE DE MOTS CHOISIE : ton message est la dictée — la question « sur ton "
            + "cahier ou au clavier ? » puis le bloc —, faite UNIQUEMENT de mots seuls, un par "
            + "ligne, chacun suivi d'un point. Aucune phrase.]";

        private const string ChoisiePhrases =
            "\n\n[DICTÉE DE PHRASES CHOISIE : ton message est la dictée — la question « sur "
            + "ton cahier ou au clavier ? » puis le bloc —, en phrases courtes à la mesure de "
            + "sa classe.]";

        /// <summary>Au lycée, pas de choix : une dictée est faite de phrases.</summary>
        public static bool AuChoix(Eleve eleve) =>
            !string.Equals(eleve.NiveauCycle, "Lycee", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Le rappel du tour, ou une chaîne vide (lycée, ou aucune dictée en vue).
        /// </summary>
        /// <param name="dicteeEnVue">Les consignes de la dictée sont chargées ce tour-ci.</param>
        public static string Rappel(
            Eleve eleve, bool dicteeEnVue, IEnumerable<DomainMessage> historique, string messageEleve)
        {
            if (!dicteeEnVue || !AuChoix(eleve)) return string.Empty;

            // Le choix fait DEPUIS la dernière dictée du professeur : s'il existe,
            // le prochain message est la dictée ; sinon, c'est la question.
            string? choixEnAttente = null;
            string? typeDuBloc = null;

            foreach (var message in historique)
            {
                var contenu = message.Contenu ?? string.Empty;

                if (message.Role == "assistant" && contenu.Contains("[DICTEE", StringComparison.Ordinal))
                {
                    typeDuBloc = choixEnAttente;
                    choixEnAttente = null;
                }
                else if (message.Role == "user" && LireChoix(contenu) is { } lu)
                {
                    choixEnAttente = lu;
                }
            }

            // Le message de ce tour vient après tout l'historique.
            if (LireChoix(messageEleve) is { } duTour) choixEnAttente = duTour;

            if (choixEnAttente is not null)
            {
                return choixEnAttente == "MOTS" ? ChoisieMots : ChoisiePhrases;
            }

            var enCours = typeDuBloc switch
            {
                "MOTS" => " La dictée en cours est une dictée de MOTS.",
                "PHRASES" => " La dictée en cours est une dictée de PHRASES.",
                _ => string.Empty,
            };

            return "\n\n[DICTÉE — MOTS OU PHRASES : toute NOUVELLE dictée commence par la question « Tu "
                + "veux une dictée de mots ou une dictée de phrases ? » avec " + Balise + ", dans un "
                + "message SANS bloc de dictée et sans aucun mot de la dictée. Elle part au message "
                + "suivant, selon son choix." + enCours + "]";
        }

        private static string? LireChoix(string? contenu)
        {
            if (string.IsNullOrEmpty(contenu)) return null;
            if (contenu.Contains(FaitMots, StringComparison.Ordinal)) return "MOTS";
            if (contenu.Contains(FaitPhrases, StringComparison.Ordinal)) return "PHRASES";

            // DÉJÀ DIT EN TOUTES LETTRES — 01/11/2026. « Je voudrais faire une
            // dictée de mots » : la question est réglée avant d'être posée. Le
            // professeur la reposait quand même, et la carte s'affichait devant
            // un enfant qui venait de répondre. Seule la forme complète compte
            // — « dictée de mots », « dictée de phrases » — pour ne rien lire
            // dans un « mot » dit en passant.
            var mots = DicteeDeMots().IsMatch(contenu);
            var phrases = DicteeDePhrases().IsMatch(contenu);
            if (mots != phrases) return mots ? "MOTS" : "PHRASES";

            return null;
        }

        [System.Text.RegularExpressions.GeneratedRegex(
            @"dict[ée]e\s+de\s+mots?\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
        private static partial System.Text.RegularExpressions.Regex DicteeDeMots();

        [System.Text.RegularExpressions.GeneratedRegex(
            @"dict[ée]e\s+de\s+phrases?\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
        private static partial System.Text.RegularExpressions.Regex DicteeDePhrases();
    }
}
