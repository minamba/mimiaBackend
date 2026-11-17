using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// UNE RÉPONSE COURTE SE JUGE À NEUF — signalement du 14/11/2026 (Yann) : « Le lierre, qui reste en place plusieurs
    /// années, c'est une plante annuelle ou vivace ? » — « Une plante vivace. » — « Pas tout à fait ! […] C'est donc une
    /// plante vivace, tu as raison. »
    ///
    /// LE MÊME DÉFAUT QUE LES NOMBRES (<see cref="ReponseChiffree"/>, 09/11/2026) : le professeur plaque l'ouverture de ses
    /// tours précédents avant d'avoir comparé, puis se corrige en cours de phrase. Le rappel des nombres ne partait que
    /// sur un chiffre ; « une plante vivace » n'en a pas. Celui-ci part sur toute RÉPONSE COURTE à une QUESTION du
    /// professeur, juste à côté du message : la dernière chose lue avant d'écrire.
    ///
    /// Court, non mis en cache : une fraction de centime par réponse. Sans les mots « dictée », « écoute »,
    /// « conversation » (voir DetecteurExercicesLangue).
    /// </summary>
    public static partial class ReponseCourte
    {
        public const string Rappel =
            "[SA RÉPONSE EST COURTE : juge-la à neuf. AVANT ton premier mot, compare-la à la réponse que TU attends à TA "
            + "question. Si elle est juste, ta réponse COMMENCE par « Oui » — jamais « pas tout à fait », « presque », "
            + "« non » ou « tu es sûr ? » devant une réponse juste. Ne reprends pas l'ouverture de tes messages "
            + "précédents.]";

        private const int MotsMax = 12;

        [GeneratedRegex(@"\p{L}+|\d+")]
        private static partial Regex Mot();

        /// <summary>Le professeur a-t-il posé une question (un « ? » hors du tableau) ?</summary>
        [GeneratedRegex(@"\[ARDOISE\].*?\[/ARDOISE\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Tableau();

        /// <summary>Ouverture négative puis aveu dans le même message : le défaut, relevé pour le mesurer.</summary>
        [GeneratedRegex(@"^\W*(?:pas\s+tout\s+à\s+fait|pas\s+tout\s+a\s+fait|presque|pas\s+encore|non)\b", RegexOptions.IgnoreCase)]
        private static partial Regex OuvertureNegative();

        [GeneratedRegex(@"\btu\s+as\s+(?:bien\s+)?raison\b|\bc.est\s+(?:bien\s+)?(?:ça|juste|correct|exact)\b|\bbonne\s+réponse\b", RegexOptions.IgnoreCase)]
        private static partial Regex Aveu();

        /// <summary>
        /// Une réponse courte (1 à 12 mots, sans « ? ») au dernier message du professeur, qui posait une question. Les
        /// faits entre crochets que l'application accroche au message sont écartés.
        /// </summary>
        public static bool Concerne(string? messageEleve, string? dernierDuProfesseur)
        {
            if (string.IsNullOrWhiteSpace(messageEleve) || string.IsNullOrWhiteSpace(dernierDuProfesseur)) return false;
            var dit = Regex.Replace(messageEleve, @"\[[^\]]*\]", " ").Trim();
            if (dit.Length == 0 || dit.Contains('?')) return false;

            var mots = Mot().Matches(dit).Count;
            if (mots is 0 or > MotsMax) return false;

            return Tableau().Replace(dernierDuProfesseur, " ").Contains('?');
        }

        /// <summary>Le professeur commence par « Pas tout à fait » puis dit « tu as raison » : à relever dans les logs.</summary>
        public static bool SeContredit(string? reponseDuProfesseur)
        {
            if (string.IsNullOrWhiteSpace(reponseDuProfesseur)) return false;
            var debut = reponseDuProfesseur.TrimStart();
            return OuvertureNegative().IsMatch(debut) && Aveu().IsMatch(debut);
        }
    }
}
