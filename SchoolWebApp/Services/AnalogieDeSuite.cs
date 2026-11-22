using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// PAS D'INDICE PAR COMPARAISON DANS UNE SUITE, RETIRÉ PAR LE CODE — contrôle d'Omar sur le lot groupé v2 (19abbfd, 22/11/2026,
    /// point 6). Le fait <see cref="CalculAuTableau.PasDAnalogie"/> ne suffisait pas : 2 séries sur 10 au bout en bout (#53)
    /// donnaient encore un indice par comparaison, « Maintenant compte un tout petit peu après quatre-vingts, comme quand tu
    /// comptais avant. » (conv. de test 1115), « …et après 80, c'est comme après n'importe quel nombre qui finit par 0, tu te
    /// souviens de 9, 10 ? » (conv. de test 1117). Les noms des nombres ne se construisent pas tous pareil : une comparaison
    /// mène l'élève à un nom qui n'existe pas, ou à un autre nombre.
    ///
    /// Pendant une SUITE de nombres que l'élève n'a pas encore trouvée (même question que le juge, <see cref="CalculAuTableau.ReponseCherchee"/>),
    /// la phrase qui compare part entière, à l'écran, dans la voix et dans l'historique (même découpage par phrase que
    /// <see cref="FelicitationRefusee"/>). Si c'était elle qui posait la question (plus aucun « ? » dans le message), la question
    /// du code est remise à la fin (« Quel nombre vient juste après 80 ? ») : le juge la relit au tour suivant. Une phrase
    /// qui porte une balise (l'ardoise) ne se touche pas. Générique : tous les professeurs, toute suite jugée par le code.
    ///
    /// TÂCHE 0000o (22/11/2026, décision du coordinateur, règle de Min's « des consignes plutôt que des filtres ») : le retrait
    /// de phrase n'est PLUS BRANCHÉ dans le flux ni dans l'historique (« Comme quand tu comptais avant… tu te souviens ? »
    /// devenait « Tu te souviens ? », une phrase sans objet). L'interdit reste dit par le fait <see cref="CalculAuTableau.PasDAnalogie"/> ;
    /// la réponse donnée en dizaines et unités (règle dure) est lue par <see cref="DonneEnDizaines"/> pour <see cref="SuiteGardee"/>, et
    /// <see cref="QuestionDuCode"/> / <see cref="AvecLaQuestion"/> servent toujours. Les méthodes de retrait restent pour les harnais.
    /// </summary>
    public static partial class AnalogieDeSuite
    {
        /// <summary>
        /// « comme quand … », « comme après 70 / avant 30 », « comme pour 20 », « comme vingt-et-un », « n'importe quel
        /// nombre », « c'est pareil qu'après 70 ». « Comme tout à l'heure », « comme moi », « comme avant » (= comme tout à
        /// l'heure) restent.
        /// </summary>
        // CONTRÔLE D'OMAR SUR LE LOT GROUPÉ v3 (89f010e, point 2) : le filtre prenait pour une comparaison une phrase qui ne
        // fait que RELIRE la suite (« Tu te souviens de 78, 79, 80… et après ? » → « et après ? » ; « Comme avant, on lit la
        // suite : 78, 79, 80… Et après ? » → « Et après ? » ; « Recompte comme avant : 78, 79, 80, et après ? » → rien). Ne
        // comptent plus : « tu te souviens de / tu te rappelles + nombre » (c'est la suite qu'il relit ; le « tu te souviens de
        // 9, 10 ? » de la conv. 1117 part par « n'importe quel nombre », dans la même phrase), ni « comme avant / comme après »
        // SANS nombre derrière (« comme avant » = comme tout à l'heure ; « comme après 70 » reste une comparaison).
        private const string AnalogieTexte =
            @"^.*?(?:\bcomme\s+(?:quand|lorsque)\b"
            + @"|\bcomme\s+(?:après|apres|avant)\s+(?:\d|(?:un|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize|vingt|trente|quarante|cinquante|soixante|cent|mille)\b)"
            + @"|\bcomme\s+(?:pour|avec)\s+(?:le\s+|les\s+)?(?:nombres?\s+)?\d"
            + @"|\bcomme\s+(?:vingt|trente|quarante|cinquante|soixante|cent)\b|\bn['’]importe\s+quel(?:le)?\s+(?:nombre|dizaine)\b"
            + @"|\b(?:pareil|la\s+même\s+chose)\s+qu(?:e\s+|['’]\s*)(?:après|apres|avant|pour|quand|\d))[^\n]*$";

        [GeneratedRegex(AnalogieTexte, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex Analogie();

        private static readonly string[] Unites = ["zéro", "(?:un|une)", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf"];

        /// <summary>
        /// LA RÉPONSE DONNÉE EN DIZAINES ET UNITÉS — bout en bout final du lot groupé v3 (série 7, conv. de test 1147) : « 80,
        /// c'est 8 dizaines. Le nombre juste après, c'est 8 dizaines et 1 : essaie de le dire. » (= 81, la réponse). La phrase
        /// qui dit « D dizaines et U » égal au nombre cherché part aussi (U de 1 à 9, en chiffres ou en lettres).
        /// </summary>
        public static Regex Motif(decimal? valeur)
        {
            if (valeur is not { } v || v != Math.Floor(v) || v < 11 || v > 9999 || v % 10 == 0) return Analogie();
            var d = (int)(v / 10);
            var u = (int)(v % 10);
            var dizaines = d < 10 ? $"(?:{d}|{Unites[d]})" : d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var decomposee = @"|^.*?(?<![\d,])" + dizaines + @"\s+dizaines?\s*(?:et|\+|,)\s*(?:" + u + "|" + Unites[u] + @")(?![\d\p{L}])[^\n]*$";
            return new Regex(AnalogieTexte + decomposee, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// LA RÉPONSE DONNÉE EN DIZAINES ET UNITÉS, SEULE (sans les comparaisons) — tâche 0000o (22/11/2026, décision du
        /// coordinateur « des consignes plutôt que des filtres ») : le retrait de phrase par comparaison n'est plus branché dans
        /// le flux ; seule cette forme reste lue, parce qu'elle DONNE la réponse (règle dure). Elle sert à
        /// <see cref="SuiteGardee"/>, qui remplace alors le message en entier (jamais de phrase coupée).
        /// </summary>
        public static bool DonneEnDizaines(string? texte, decimal valeur)
        {
            if (string.IsNullOrEmpty(texte) || valeur != Math.Floor(valeur) || valeur < 11 || valeur > 9999 || valeur % 10 == 0) return false;
            var d = (int)(valeur / 10);
            var u = (int)(valeur % 10);
            var dizaines = d < 10 ? $"(?:{d}|{Unites[d]})" : d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Regex.IsMatch(texte, @"(?<![\d,])" + dizaines + @"\s+dizaines?\s*(?:et|\+|,)\s*(?:" + u + "|" + Unites[u] + @")(?![\d\p{L}])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>« quel nombre vient juste après 80 » → « Quel nombre vient juste après 80 ? ».</summary>
        // « 78, 79, 80… ? » (énoncé d'une suite relancée, bout en bout final v4 sur 0c68fea, conv. de test 1239) : l'énoncé
        // porte déjà son « ? » ; on ne le double pas (« 78, 79, 80… ? ? »).
        public static string QuestionDuCode(string enonce) =>
            string.IsNullOrWhiteSpace(enonce) ? string.Empty
            : char.ToUpperInvariant(enonce.Trim()[0]) + enonce.Trim()[1..] + (enonce.TrimEnd().EndsWith('?') ? string.Empty : " ?");

        /// <summary>Le texte entier (historique) : mêmes retraits que le flux, même question remise.</summary>
        public static string Retirer(string? texte, string enonce, out int retraits) => Retirer(texte, enonce, null, out retraits);

        public static string Retirer(string? texte, string enonce, decimal? valeur, out int retraits)
        {
            var r = FelicitationRefusee.ParPhrase(texte, Motif(valeur), out retraits);
            return retraits > 0 ? AvecLaQuestion(r, enonce) : r;
        }

        /// <summary>
        /// JAMAIS UN MESSAGE SANS QUESTION APRÈS UN RETRAIT — contrôle d'Omar sur le lot groupé v3 (point 3, AG3) : « 80, c'est
        /// 8 dizaines. Le nombre juste après, c'est 8 dizaines et 1 : essaie de le dire. » devenait « 80, c'est 8 dizaines. »,
        /// sans rien demander. Dès qu'un retrait laisse le message sans « ? » (qu'il y en ait eu un avant ou non), la question
        /// du code revient à la fin ; s'il ne reste rien, elle est seule (jamais de lignes vides en tête).
        /// </summary>
        public static string AvecLaQuestion(string texte, string enonce) =>
            texte.Contains('?') ? texte
            : string.IsNullOrWhiteSpace(texte) ? QuestionDuCode(enonce)
            : texte + "\n\n" + QuestionDuCode(enonce);

        public static string Retirer(string? texte, string enonce) => Retirer(texte, enonce, out _);

        /// <summary>Le filtre du flux : phrase par phrase ; à la fin, la question du code si la seule question est partie.</summary>
        public sealed class Filtre(string enonce, decimal? valeur = null)
        {
            /// <summary>Les retraits des filtres d'avant dans le même flux (« dernier essai » trop tôt) : eux non plus ne laissent
            /// jamais le message sans la question (point 3 d'Omar sur la v3).</summary>
            public Func<int>? AutresRetraits { get; init; }

            private readonly FelicitationRefusee.Filtre _phrases = new(Motif(valeur));
            private bool _questionSortie;
            private bool _texteSorti;

            public int Retraits => _phrases.Retraits;

            public string Passer(string? fragment) => Noter(_phrases.Passer(fragment));

            public string Terminer()
            {
                var sortie = Noter(_phrases.Terminer());
                if (_phrases.Retraits + (AutresRetraits?.Invoke() ?? 0) > 0 && !_questionSortie) sortie += (_texteSorti ? "\n\n" : string.Empty) + QuestionDuCode(enonce);
                return sortie;
            }

            private string Noter(string sortie)
            {
                if (sortie.Contains('?')) _questionSortie = true;
                if (!string.IsNullOrWhiteSpace(sortie)) _texteSorti = true;
                return sortie;
            }
        }
    }
}
