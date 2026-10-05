using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'AU REVOIR QUI SUIT UN BILAN DÉJÀ FAIT — relevé par Camara le 28/09/2026.
    ///
    /// Nora avait fait le bilan de la séance (« on a bien travaillé les
    /// additions jusqu'à 10… la prochaine fois, on essaiera… ») ; l'élève a
    /// répondu « à la prochaine, Nora », et la conclusion est repartie de
    /// zéro : le même bilan, redit mot pour mot ou presque.
    ///
    /// La règle de conclusion est dans le noyau, qu'on ne touche pas. Elle ne
    /// pouvait pas savoir que le bilan venait d'être fait — c'est un FAIT, et
    /// on le lui donne au tour de l'au revoir (voir « une règle de prompt ne
    /// vaut pas un fait »).
    /// </summary>
    public static partial class LecteurAuRevoir
    {
        // L'élève prend congé. Pas « merci » seul, ni « salut » en début de
        // phrase : ce sont aussi des bonjours et des politesses en cours.
        [GeneratedRegex(
            @"\b(au\s+revoir|[àa]\s+la\s+prochaine|[àa]\s+plus(\s+tard)?|[àa]\s+bient[ôo]t|[àa]\s+demain|bonne\s+(soir[ée]e|journ[ée]e|nuit)|bye|ciao|je\s+m'en\s+vais|j'arr[êe]te|on\s+(s')?arr[êe]te)\b",
            RegexOptions.IgnoreCase)]
        private static partial Regex Conge();

        // Le bilan d'une séance annonce toujours la suite : c'est ce qui le
        // distingue d'un simple « bravo ».
        [GeneratedRegex(
            @"la\s+prochaine\s+fois|prochaine\s+s[ée]ance|au\s+prochain\s+cours|on\s+reprendra|on\s+pourra\s+(essayer|voir|attaquer|reprendre)",
            RegexOptions.IgnoreCase)]
        private static partial Regex Suite();

        public static bool DitAuRevoir(string? messageEleve) =>
            !string.IsNullOrWhiteSpace(messageEleve) && Conge().IsMatch(messageEleve);

        /// <summary>
        /// LE MERCI QUI CLÔT, APRÈS UN BILAN — Camara, le 05/10/2026, en maths :
        /// Nora fait son bilan (« on a bien travaillé… la prochaine fois… »),
        /// l'élève répond « OK, pas de souci, je te remercie », et le bilan
        /// repart, « même si c'est dit un peu différemment ». Le merci n'était
        /// pas reconnu comme un congé : « merci » seul est exclu de `Conge`,
        /// parce qu'en plein cours c'est une politesse, pas un départ.
        ///
        /// JUSTE APRÈS UN BILAN, si : il n'y a plus rien à remercier que la
        /// séance. On ne le retient que sur une réplique COURTE — « merci, mais
        /// je n'ai pas compris le dernier calcul » reste une question.
        /// </summary>
        public static bool RemercieApresBilan(string? messageEleve) =>
            !string.IsNullOrWhiteSpace(messageEleve)
            && Merci().IsMatch(messageEleve)
            && messageEleve.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length <= 10
            && !messageEleve.Contains('?');

        [GeneratedRegex(@"\b(merci|je\s+te\s+remercie|je\s+vous\s+remercie|c'est\s+gentil|thanks?)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Merci();

        public static bool BilanDejaFait(string? dernierMessageProf) =>
            !string.IsNullOrWhiteSpace(dernierMessageProf) && Suite().IsMatch(dernierMessageProf);

        /// <summary>
        /// Le fait du tour. Court : il ne remplace pas la consigne de
        /// conclusion, il lui dit seulement que le bilan est déjà dit.
        /// </summary>
        public const string Marqueur =
            "[IL TE DIT AU REVOIR OU TE REMERCIE, ET TON MESSAGE PRÉCÉDENT A DÉJÀ FAIT LE BILAN DE LA SÉANCE "
            + "(ce qu'il a fait, ce que vous reprendrez). NE LE RÉPÈTE PAS, même reformulé : réponds à son au revoir "
            + "en UNE phrase chaleureuse, avec son prénom, sans question — puis tes balises de fin "
            + "habituelles et [FIN_SEANCE].]";
    }
}
