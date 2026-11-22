using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// « DERNIER ESSAI » ANNONCÉ TROP TÔT, RETIRÉ PAR LE CODE — contrôle d'Omar sur le lot groupé (22/11/2026, point 4). La
    /// règle des essais (<see cref="Prompts.PromptsPedagogiques.RegleDesTroisEssais"/>) annonce le dernier essai à la
    /// TROISIÈME erreur. Avec le rang compté par le code (<see cref="CalculAuTableau.RangDeLErreur"/>), le professeur
    /// l'annonçait encore dès la deuxième 1 fois sur 10 au bout en bout (« Ce n'est pas encore ça, Adam, et c'est ton dernier
    /// essai ! »), puis abandonnait la question à la troisième. Aux 1re et 2e erreurs comptées par le code, la mention part
    /// de sa phrase, à l'écran, dans la voix et dans l'historique (même découpage par phrase que <see cref="FelicitationRefusee"/>).
    /// Générique : tous les professeurs, tout calcul ou suite jugé par le code. Rien à la 3e erreur et au-delà.
    /// </summary>
    public static partial class DernierEssaiTropTot
    {
        [GeneratedRegex(
            @"(?:\s*,\s*|\s*[—–]\s*|\s+)?(?:\bet\s+)?(?:\b(?:allez|alors|bon|ok)\s*,?\s*)?(?:\bpour\s+)?(?:\bc['’]est\s+)?(?:\b(?:ton|ta|le|un|une)\s+)?"
            + @"\b(?:dernier\s+essai|derni[eè]re\s+(?:chance|tentative))\b\s*(?:!+|,|:)?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Mention();

        /// <summary>Le rang d'erreur (compté par le code) où l'annonce est trop tôt.</summary>
        public static bool TropTot(int? rang) => rang is 1 or 2;

        public static string Retirer(string? texte, out int retraits) => FelicitationRefusee.ParPhrase(texte, Mention(), out retraits);

        public static string Retirer(string? texte) => Retirer(texte, out _);

        public static FelicitationRefusee.Filtre Filtre() => new(Mention());
    }
}
