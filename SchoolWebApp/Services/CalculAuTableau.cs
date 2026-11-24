using System.Globalization;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LE CALCUL POSÉ PAR LE PROFESSEUR, JUGÉ PAR LE CODE — Minamba, le 20/11/2026, signalement #29 rouvert par Adam
    /// (avec #45 et #46). Le rappel de <c>ReponseChiffree</c> (« refais le calcul avant de juger ») ne suffisait pas :
    /// sur mimia.fr en ligne, « 4 + 3 = ? » → « 8 » a reçu « Oui, c'est ça, 4 + 3 = 8 ! Bravo ! », « 9 - 4 = ? » →
    /// « 7 » aussi, puis « 8 - 3 = ? » → « 5 », juste, a été refusé ; et « 2 dizaines et 0 unité » pour 20 a été
    /// refusé avant un « je me suis trompée tout à l'heure, désolée ! ». Un enfant de CP apprend un résultat faux.
    ///
    /// Le résultat d'un calcul simple écrit au tableau est un FAIT que le code sait établir. Comme pour la dictée de
    /// nombres (<c>Voix.DicteeDeNombres.FaitVerdict</c>), on donne au professeur le verdict, juste à côté de la
    /// réponse : il n'a plus à le deviner, seulement à l'annoncer.
    ///
    /// PRUDENCE : on ne juge que ce qu'on lit sans ambiguïté — UNE question reconnue dans le dernier message du
    /// professeur qui pose une question, et UNE valeur dans la réponse de l'élève. Sinon : rien, et le rappel
    /// ordinaire de <c>ReponseChiffree</c> reprend sa place.
    /// </summary>
    public static partial class CalculAuTableau
    {
        public enum Jugement { Juste, Faux, Incomplet, AutreDecomposition }

        /// <summary>La question lue, la valeur attendue, ce que l'élève a donné et le verdict.</summary>
        public sealed record Verdict(string Question, string Attendu, string Donne, Jugement Jugement)
        {
            /// <summary>Un indice calculé à donner sur une réponse fausse (suite des nombres, #53) ; null sinon.</summary>
            public string? Indice { get; init; }
        }

        // Un nombre : entier ou décimal à virgule (« 2,3 »). Neuf chiffres au plus : pas de débordement.
        private const string NombreMotif = @"\d{1,9}(?:,\d{1,6})?";
        private const string OperateurMotif = @"(?:[+\-−–×x*:÷]|plus|moins|fois|divis[ée]\s+par)";
        private const string ExpressionMotif = NombreMotif + @"(?:\s*" + OperateurMotif + @"\s*" + NombreMotif + @")+";

        // Pas le morceau d'une expression plus longue qu'on ne sait pas lire : « (2 + 3) × 4 », « 1/2 + 1/4 »,
        // « 3² + 4 », « -4 + 3 », « 1 000 + 200 ». Les deux-points restent permis devant (« Calcule : 4 + 3 = ? »),
        // un mot aussi (« Calcule 4 + 3 »), le crochet de [ARDOISE] aussi.
        // La virgule qui suit un MOT n'en fait pas partie : « Alors ça fait combien, 10 + 2 ? » (tâche 1941) ; celle d'un
        // nombre, si : « 2,5 + 1 », « 12, 4 + 3 ».
        // Un saut de ligne sépare aussi : au tableau, « 7 + 3 = 10⏎10 + 2 = ? » pose bien « 10 + 2 » (tâche 1941, conv. 703).
        private const string PasDeSuiteAvant = @"(?<![\d./()^²³+\-−–×*÷=][ \t]*)(?<!(?<!\p{L}),\s*)(?<![\p{L}\d])";
        private const string PasDeSuiteApres = @"(?!\s*[\d/^²³(])";

        /// <summary>« 4 + 3 = ? », « 23 × 14 = … », « 9 - 4 = ___ », « 10 + 2 = combien ? ».</summary>
        [GeneratedRegex(PasDeSuiteAvant + @"(?<expr>" + ExpressionMotif + @")\s*=\s*(?:\?|…|\.{2,}|_+|combien(?!\s+(?:de|d'|d’)))", RegexOptions.IgnoreCase)]
        private static partial Regex EgalQuestion();

        /// <summary>« Combien font 4 + 3 ? », « combien fait 8 moins 3 », « Combien ça fait, 10 + 2 ? ».</summary>
        [GeneratedRegex(@"combien\s+(?:font|fait|ça\s+fait|ca\s+fait|cela\s+fait|ça\s+donne|ca\s+donne|égalent|égale)\s*,?\s+(?<expr>" + ExpressionMotif + @")" + PasDeSuiteApres
            + @"(?!\s*=)", RegexOptions.IgnoreCase)]
        private static partial Regex CombienFont();

        /// <summary>
        /// « 4 + 3, ça fait combien ? », « Et 8 - 3 ? », « Alors ça fait combien, 10 + 2 ? », « 10 + 2, c'est combien ? ».
        /// Tâche 1941 (vu par Omar sur master) : la relance « Alors ça fait combien, 10 + 2 ? » n'était pas lue (la virgule
        /// devant le calcul), aucun fait ne partait, et « 14 » a été validé.
        /// </summary>
        [GeneratedRegex(PasDeSuiteAvant + @"(?<expr>" + ExpressionMotif + @")\s*(?:,?\s*(?:(?:ça|ca|cela)\s+(?:fait|donne)\s+combien"
            + @"|combien\s+(?:ça|ca|cela)\s+(?:fait|donne)|c['’]est\s+combien|(?:font|fait|égale|égalent|donne)\s+combien)(?!\s+(?:de|d'|d’))|\?)",
            RegexOptions.IgnoreCase)]
        private static partial Regex ExpressionInterrogee();

        [GeneratedRegex(NombreMotif + "|" + OperateurMotif, RegexOptions.IgnoreCase)]
        private static partial Regex Jeton();

        /// <summary>« Combien de dizaines et combien d'unités il y a dans 20 ? », « 47, c'est combien de dizaines… ».</summary>
        [GeneratedRegex(@"combien\s+de\s+(?:centaines|dizaines|unit[ée]s)[^?\n]*?\b(?:dans|pour)\s+(?:le\s+nombre\s+)?(?<n>\d{1,4})(?![\d,])"
            + @"|(?<![\d,])(?<n>\d{1,4})\s*,?\s*c'est\s+combien\s+de\s+(?:centaines|dizaines)"
            + @"|d[ée]compose\s+(?:le\s+nombre\s+)?(?<n>\d{1,4})(?![\d,])", RegexOptions.IgnoreCase)]
        private static partial Regex QuestionDecomposition();

        /// <summary>« 2 dizaines », « 0 unité », « deux dizaines et zéro unité », « vingt et une unités ».</summary>
        [GeneratedRegex(@"(?<v>\d{1,4}|[\p{L}-]+(?:\s+et\s+(?:un|une|onze))?)\s+(?<u>centaines?|dizaines?|unit[ée]s?)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex PartieDecomposee();

        /// <summary>
        /// LE TABLEAU À TROUS — Minamba, le 20/11/2026, signalement #46. Au 2e essai sur 20, Nora a réécrit le tableau
        /// « 20 = 2 dizaines + ? unités » et demandé « Il reste combien d'unités, dans 20 ? » : une question que
        /// <see cref="QuestionDecomposition"/> ne lit pas (elle ne porte que sur les unités). Le code ne jugeait donc
        /// rien, et « 2 dizaines et 0 unité », juste, a été refusé, puis « 0 » validé avec « je me suis trompée ».
        /// Le tableau dit exactement ce qui est demandé : les cases « ? » (ou « … », « ___ »).
        /// </summary>
        [GeneratedRegex(@"(?<![\d,])(?<n>\d{1,4})\s*=\s*(?<parties>" + CaseDuTableau + @"(?:\s*(?:\+|et|,)\s*" + CaseDuTableau + @"){0,2})(?![ \t]*[+\d])",
            RegexOptions.IgnoreCase)]
        private static partial Regex TableauATrous();

        private const string CaseDuTableau = @"(?:\d{1,4}|\?|…|_+|\.{2,})\s*(?:centaines?|dizaines?|unit[ée]s?)(?!\p{L})";

        [GeneratedRegex(@"(?<v>\d{1,4}|\?|…|_+|\.{2,})\s*(?<u>centaines?|dizaines?|unit[ée]s?)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex CaseLue();

        /// <summary>
        /// Une relance qui ne demande qu'UN rang : « Combien d'unités restent, alors ? », « Il reste combien
        /// d'unités ? ». Elle ne sert qu'à préciser une décomposition déjà posée plus haut, jamais seule : sans
        /// contexte, « combien d'unités dans 20 ? » peut attendre 0 (le chiffre) ou 20 (le nombre d'unités).
        /// </summary>
        [GeneratedRegex(@"combien\s+(?:d'|d’|de\s+)(?<u>centaines|dizaines|unit[ée]s)", RegexOptions.IgnoreCase)]
        private static partial Regex RangDemande();

        /// <summary>
        /// LA QUESTION SUR UN SEUL RANG — Mins, le 24/11/2026, signalement #71 (Adam, CE1, nombres jusqu'à 1000) : Nora a
        /// écrit 347 au tableau et demandé « Dans ce nombre, combien y a-t-il de centaines ? », puis « dans 347, combien y
        /// a-t-il de dizaines ? », puis « combien reste-t-il d'unités dans 347 ? ». Le juge ne lisait que « combien de …
        /// dans N » : aucun fait, et le professeur a validé 5 réponses fausses sur 5 (« 4 centaines », « 7 dizaines »,
        /// « 9 unités », puis « 2 dizaines » pour 582), avant de récapituler « 347, c'est 4 centaines, 7 dizaines et
        /// 9 unités ». Formes lues : « combien (y a-t-il | il y a | reste-t-il | il reste …) de/d' &lt;rang&gt; ».
        /// </summary>
        [GeneratedRegex(@"combien(?:\s+(?:y\s+a[-\s]t[-\s]il|il\s+y\s+a|y\s+en\s+a[-\s]t[-\s]il|reste[-\s]t[-\s]il|il\s+reste|en\s+reste[-\s]t[-\s]il"
            + @"|as[-\s]tu|vois[-\s]tu|tu\s+vois|compte[s]?[-\s]tu|tu\s+comptes))?\s+(?:de\s+|d['’]\s*)(?<u>centaines?|dizaines?|unit[ée]s?)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex QuestionDUnRang();

        [GeneratedRegex(@"(?<!\p{L})(?<u>centaines?|dizaines?|unit[ée]s?)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex RangNomme();

        /// <summary>Le nombre de la question : « dans 347 », « pour le nombre 582 » — pas « dans 2 dizaines » (une conversion).</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:dans|pour)\s+(?:le\s+nombre\s+)?(?<n>\d{1,4})(?!\d|,\d|\s*(?:centaines?|dizaines?|unit[ée]s?)(?!\p{L}))",
            RegexOptions.IgnoreCase)]
        private static partial Regex NombreDuRang();

        /// <summary>« Dans ce nombre, … » : le nombre est celui écrit au tableau du même message.</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:ce|cet|le|du)\s+nombre(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex CeNombre();

        [GeneratedRegex(@"\[ARDOISE\](?<c>.*?)\[/ARDOISE\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Ardoise();

        [GeneratedRegex(@"\d+(?:[.,]\d+)?")]
        private static partial Regex NombreEnChiffres();

        /// <summary>Au-delà, ce n'est plus une réponse à un calcul : une phrase qui porte un nombre.</summary>
        private const int LongueurMaxReponse = 90;

        /// <summary>
        /// Le verdict, ou null si la question ou la réponse ne se lit pas sans ambiguïté. Les messages du professeur
        /// sont dans l'ordre, le plus récent en dernier.
        /// </summary>
        public static Verdict? Juger(IEnumerable<string?> messagesDuProfesseur, string? messageDeLEleve)
        {
            if (string.IsNullOrWhiteSpace(messageDeLEleve)) return null;

            var reponse = Regex.Replace(messageDeLEleve, @"\[[^\]]*\]", " ").Trim();
            if (reponse.Length == 0 || reponse.Length > LongueurMaxReponse) return null;

            var question = DerniereQuestion(messagesDuProfesseur);
            if (question is null) return null;

            if (question.Value.Suite is { } suite) return JugerSuite(suite, reponse);

            if (question.Value.Decomposition is { } decomposition) return JugerDecomposition(decomposition, reponse);

            // Signalement #76 : « entre 820 et 802, lequel est le plus grand ? ».
            if (question.Value.Comparaison is { } comparaison) return JugerComparaison(comparaison, reponse);

            var verdict = JugerCalcul(question.Value.Expression!, question.Value.Resultat, reponse);

            // Signalement #70 : « 3 » pour « 2 + 1 + la retenue de 1 », c'est la retenue oubliée — l'erreur type du CE1.
            return verdict is { Jugement: Jugement.Faux } && question.Value.Retenue is { } retenue && RetenueOubliee(verdict, retenue)
                ? verdict with { Indice = IndiceRetenueOubliee }
                : verdict;
        }

        private const string IndiceRetenueOubliee =
            "Son erreur : il a oublié d'ajouter la retenue ; fais-lui regarder la retenue.";

        private static bool RetenueOubliee(Verdict verdict, decimal retenue) =>
            decimal.TryParse(verdict.Attendu.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var attendu)
            && decimal.TryParse(verdict.Donne.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var donne)
            && retenue != 0
            && donne == attendu - retenue;

        /// <summary>Le fait à glisser à côté de la réponse de l'élève ; null sans verdict.</summary>
        public static string? Fait(IEnumerable<string?> messagesDuProfesseur, string? messageDeLEleve) =>
            Juger(messagesDuProfesseur, messageDeLEleve) is { } verdict ? Fait(verdict) : null;

        public static string Fait(Verdict v) => v.Jugement switch
        {
            Jugement.Juste =>
                $"[Vérifié par l'application : la question posée était « {v.Question} », et la bonne réponse est "
                + $"{v.Attendu}. L'élève a répondu « {v.Donne} » : c'est JUSTE. Ta réponse COMMENCE par « Oui » — jamais "
                + "« pas tout à fait », « presque », « non » ou « tu es sûr ? ». Ne reprends pas le verdict de tes "
                + "messages précédents.]",
            Jugement.Faux =>
                $"[Vérifié par l'application : la question posée était « {v.Question} », et la bonne réponse est "
                + $"{v.Attendu}, pas {v.Donne}. L'élève a répondu « {v.Donne} » : c'est FAUX. Ne dis surtout pas que "
                + "c'est juste, ne le félicite pas, et n'écris ni ne dis jamais une égalité fausse. Dis-lui que ce "
                // Ni « tout de suite » (Camara, 11/11/2026 : on ne donne JAMAIS la réponse), ni de parenthèse qui nomme la
                // règle : le professeur la recopiait en aparté, « (On garde 3e réponse fausse → …) » (tâche 1320).
                + "n'est pas encore ça et aide-le à retrouver le résultat lui-même, sans jamais le lui donner."
                + (v.Indice is null ? "" : " " + v.Indice) + "]",
            Jugement.Incomplet =>
                $"[Vérifié par l'application : la question posée était « {v.Question} », et la bonne réponse est "
                + $"{v.Attendu}. L'élève a répondu « {v.Donne} » : ce qu'il a dit est JUSTE, mais il manque une "
                + "partie. Commence par lui dire que c'est juste, puis demande-lui ce qui manque.]",
            _ =>
                $"[Vérifié par l'application : la question posée était « {v.Question} », et la décomposition "
                + $"attendue est {v.Attendu}. L'élève a répondu « {v.Donne} » : c'est bien le même nombre, mais pas "
                + "cette décomposition. Ne dis pas que c'est faux : dis-lui que ça fait bien ce nombre, puis "
                + "demande-lui de le dire avec le moins d'unités possible.]",
        };

        /// <summary>
        /// LA RÉPONSE QUE L'ÉLÈVE CHERCHE ENCORE — tâche 1941 (Omar, master : « 7 + 5, ça fait 12 : je te le dis cette
        /// fois », environ 10 réponses sur 40). La règle des essais (Camara, Min's : ne JAMAIS donner la réponse) ne
        /// tient pas par le texte seul. Le code connaît la réponse attendue : <see cref="ReponseNonDonnee"/> la retire
        /// de ce que le professeur écrit, dit et garde en mémoire.
        ///
        /// La question ouverte est celle du juge (calcul ou suite des nombres, jamais une décomposition : « 2 dizaines »
        /// se dit pour expliquer), lue exactement comme pour le verdict. Rien à cacher si l'élève l'a trouvée (verdict
        /// JUSTE) ou s'il a dit ce nombre, même dans une phrase que le juge ne lit pas (« c'est 12 ou 13 ? ») : le
        /// professeur peut alors le reprendre. Null sinon, et null sans question lue.
        /// </summary>
        public static (string Question, decimal Valeur)? ReponseCherchee(IEnumerable<string?> messagesDuProfesseur, string? messageDeLEleve)
        {
            var messages = messagesDuProfesseur.ToList();
            string enonce;
            decimal valeur;
            if (DerniereQuestion(messages) is { } question)
            {
                // Ni décomposition, ni comparaison (#76) : le professeur doit pouvoir citer les deux nombres comparés.
                if (question.Decomposition is not null || question.Comparaison is not null) return null;
                if (!decimal.TryParse(question.Resultat.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valeur))
                    return null;
                enonce = question.Suite is { } s ? s.Enonce : question.Expression + " = ?";
            }
            else if (CalculToujoursOuvert(messages) is { } ouvert)
            {
                (enonce, valeur) = ouvert;
            }
            else
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(messageDeLEleve))
            {
                if (Juger(messages, messageDeLEleve) is { Jugement: Jugement.Juste or Jugement.Incomplet }) return null;
                if (NombresDuMessage(messageDeLEleve).Contains(valeur)) return null;
            }

            return (enonce, valeur);
        }

        /// <summary>
        /// POUR LA PROTECTION SEULEMENT (jamais pour un verdict) — tâche 1941 (bout en bout, conv. 748) : « Lève 7 doigts.
        /// Plie un doigt à la fois en comptant : 8, 9, 10, 11… Tu t'arrêtes à combien de doigts pliés ? » pose une
        /// SOUS-QUESTION : le juge ne rend plus de verdict, à raison (l'élève peut répondre 5). Mais le calcul n'est pas
        /// résolu pour autant, et le professeur a donné 12 au tour suivant. Le calcul posé le plus récemment (un seul
        /// résultat) reste à protéger tant qu'aucun message plus récent ne le ferme : une félicitation, un tableau effacé,
        /// une autre question de calcul, de décomposition ou de suite.
        /// </summary>
        private static (string Question, decimal Valeur)? CalculToujoursOuvert(IReadOnlyList<string?> messagesDuProfesseur)
        {
            foreach (var message in messagesDuProfesseur.Reverse().Take(MessagesRemontes))
            {
                if (string.IsNullOrWhiteSpace(message)) continue;

                var calculs = Calculs(message, out var illisible);
                if (illisible) return null;
                if (calculs.Count > 0)
                {
                    return calculs.Select(c => c.Resultat).Distinct().Count() == 1 && calculs[0].Resultat >= 0
                        ? (calculs[0].Expression + " = ?", calculs[0].Resultat)
                        : null;
                }

                if (Felicite().IsMatch(message) || message.Contains("[TABLEAU_EFFACE]", StringComparison.OrdinalIgnoreCase)) return null;
                if (Decompositions(message) is not { Count: 0 }) return null;
                if (Suites(message) is { Count: > 0 }) return null;
                if (Comparaisons(message) is not { Count: 0 }) return null;
            }

            return null;
        }

        [GeneratedRegex(@"^\W*(?:oui|bravo|exact|super|parfait|génial|bien\s+joué|c'est\s+(?:ça|juste|bon))\b", RegexOptions.IgnoreCase)]
        private static partial Regex OuvertureQuiValide();

        [GeneratedRegex(@"^\W*(?:non\b|pas\s+tout\s+à\s+fait|pas\s+encore|presque|tu\s+es\s+sûr|ce\s+n'est\s+pas|hmm)", RegexOptions.IgnoreCase)]
        private static partial Regex OuvertureQuiRefuse();

        /// <summary>
        /// Le professeur a-t-il contredit le verdict ? Pour les journaux seulement (« Verdict de calcul contredit »
        /// dans docker logs mimia) : savoir si le fait suffit, sans rien changer à ce que l'enfant reçoit.
        /// </summary>
        public static bool Contredit(Verdict verdict, string? reponseDuProfesseur)
        {
            if (string.IsNullOrWhiteSpace(reponseDuProfesseur)) return false;
            var debut = reponseDuProfesseur.TrimStart();

            return verdict.Jugement switch
            {
                Jugement.Juste => OuvertureQuiRefuse().IsMatch(debut),
                Jugement.Faux => OuvertureQuiValide().IsMatch(debut)
                    || (verdict.Question.EndsWith(" = ?")
                        && Regex.IsMatch(debut, Regex.Escape(verdict.Question[..^4]) + @"\s*=\s*" + Regex.Escape(verdict.Donne) + @"(?![\d,])")),
                _ => false,
            };
        }

        // ───────────── La question ─────────────

        /// <param name="Retenue">La retenue nommée dans le calcul (« 2 + 1 + la retenue de 1 », signalement #70), sinon null.</param>
        /// <param name="Comparaison">« entre 820 et 802, lequel est le plus grand ? » (signalement #76), sinon null.</param>
        private readonly record struct Question(string? Expression, string Resultat, DecompositionPosee? Decomposition, SuitePosee? Suite = null, decimal? Retenue = null,
            ComparaisonPosee? Comparaison = null);

        /// <summary>
        /// Une décomposition à donner : le nombre, la valeur attendue de chaque rang (c, d, u), les rangs qu'on demande
        /// vraiment (les cases vides du tableau, ou tous pour « combien de dizaines et d'unités dans 20 ? ») et si la
        /// décomposition attendue est l'ordinaire (2 dizaines et 0 unité) ou une autre que le tableau impose
        /// (« 20 = 1 dizaine + ? unités » attend 10 unités).
        /// </summary>
        private sealed record DecompositionPosee(int N, Dictionary<char, int> Attendues, HashSet<char> Demandes, bool Ordinaire, string Enonce)
        {
            /// <summary>
            /// Le rang demandé vient d'une relance en mots (« Combien d'unités restent ? ») ou d'une question en mots sur
            /// un seul rang (« dans 347, combien y a-t-il de dizaines ? », signalement #71), pas d'une case vide.
            /// </summary>
            public bool RangDeRelance { get; init; }

            /// <summary>
            /// La question ne porte que sur UN rang (signalement #71) : le verdict ne parle que de ce rang (deux questions
            /// sur deux rangs du même nombre ne sont pas la même question pour le compte des erreurs), et une réponse
            /// fausse reçoit l'indice de la place du chiffre.
            /// </summary>
            public bool QuestionDUnRang { get; init; }
        }

        [GeneratedRegex(@"combien\s+(?:de\s+|d'|d’)(?<x>\p{L}+)", RegexOptions.IgnoreCase)]
        private static partial Regex CombienDe();

        private static bool EstUnRang(string mot) => Regex.IsMatch(mot, @"^(?:centaines?|dizaines?|unit[ée]s?)$", RegexOptions.IgnoreCase);

        // « ça fait combien d'unités », « en tout », « dans 2 dizaines, il y a combien d'unités ? », « une dizaine, c'est
        // 10 unités » : on demande de CONVERTIR un rang en un autre, pas la case du tableau.
        [GeneratedRegex(@"\b(?:ça|ca|cela)\s+(?:fait|font|donne)\b|\ben\s+tout\b"
            + @"|\bdans\s+(?:\d{1,4}|une?|[\p{L}-]+)\s+(?:centaines?|dizaines?)\b"
            + @"|\b(?:centaines?|dizaines?)\s*,?\s*(?:c'est|c’est|vaut|valent|font|fait|égale|égalent)\s+(?:\d|combien)", RegexOptions.IgnoreCase)]
        private static partial Regex Conversion();

        /// <summary>
        /// Contrôle d'Omar (paquet 1, #71) : « combien de dizaines AU TOTAL / EN TOUT / ENTIÈRES dans 347 ? » attend toutes
        /// les dizaines (34), pas le chiffre (4) ; le juge rendait JUSTE « 4 ». Pas de verdict.
        /// Même chose pour « combien de dizaines COMPLÈTES dans 347 ? » (contrôle d'Omar v3, W08). « toutes les dizaines »
        /// n'en fait PAS partie : « on a compté toutes les dizaines possibles » est une explication, pas la question (h46).
        /// </summary>
        [GeneratedRegex(@"(?<!\p{L})(?:au\s+total|en\s+tout|enti[èe]re?s?|en\s+totalit[ée]|compl[èe]te?s?)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex TotalDuRang();

        /// <summary>
        /// Contrôle d'Omar (paquet 1, #71) : « Dans 25 boîtes de dix crayons, combien y a-t-il de dizaines de crayons ? »,
        /// « Dans 12 sachets de dix billes, combien de dizaines de billes ? » comptent des OBJETS groupés : le juge lisait
        /// « combien de dizaines dans 25 » et jugeait FAUX la bonne réponse 25. Un nombre suivi d'un nom, un rang suivi de
        /// « de &lt;nom&gt; », « de dix / de cent » : pas de verdict.
        /// </summary>
        [GeneratedRegex(@"(?<!\p{L})(?:centaines?|dizaines?|unit[ée]s?)\s+(?:de\s+|d['’]\s*)(?!(?:chaque|ce|cet|ces|ton|ta|tes|mon|ma|mes|son|sa|ses|la|le|les|leurs?|l['’]|un|une)(?!\p{L}))\p{L}"
            + @"|(?<!\p{L})(?:de|d['’])\s*(?:dix|cent|mille|10|100|1000)(?![\d\p{L}])", RegexOptions.IgnoreCase)]
        private static partial Regex DesObjets();

        /// <summary>« Dans 25 boîtes… » : le nombre de la question est suivi d'un nom (pas d'un rang, pas de « combien »).</summary>
        [GeneratedRegex(@"(?<![\d,])\d{1,4}[ \t]+(?!(?:centaines?|dizaines?|unit[ée]s?|combien)(?!\p{L}))\p{L}", RegexOptions.IgnoreCase)]
        private static partial Regex NombreSuiviDUnNom();

        // « j'ai 7 ans », « attends 2 minutes », « c'est quoi 7 ? » : un nombre dans une phrase qui ne répond pas
        // (contrôle d'Omar, 20/11/2026). « 7 ? » tout court reste une réponse hésitante.
        // « Pourquoi pas 8 ? » (contrôle d'Omar #50/#51 v3) est une réponse hésitante aussi : seul « pourquoi » sans « pas »
        // (« pourquoi 7 ? ») ne répond pas.
        [GeneratedRegex(@"(?<!\p{L})(?:ans?|minutes?|heures?|secondes?|quoi|pourquoi(?!\s+pas(?!\p{L}))|comment|attends?|attendez|pause)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex PasUneReponse();

        /// <summary>
        /// La question du message le plus récent qui en pose une. Un message sans « ? » (« Ce n'est pas encore ça.
        /// C'est ton dernier essai. ») laisse la question d'avant ouverte ; un message qui pose une AUTRE question,
        /// qu'on ne sait pas lire, la ferme. Deux questions différentes dans le même message : on ne choisit pas.
        /// </summary>
        private static Question? DerniereQuestion(IEnumerable<string?> messagesDuProfesseur)
        {
            // Les nombres des relances traversées (« Compte : 4, puis encore 3. Combien ça fait ? ») : ils doivent
            // tous appartenir à la question retrouvée plus haut, sinon on ne sait pas de quoi la relance parle.
            var nombresDesRelances = new List<decimal>();

            // Les mêmes, sans les nombres que la relance REFUSE ou prête à l'élève (« C'est encore 7 », « ce n'est pas 7 ») :
            // pour une suite seulement (retest KO #53, conversation de test 813).
            var nombresDesRelancesSansRefus = new List<decimal>();

            // Le rang que demande la relance la plus récente (« Combien d'unités restent, alors ? ») : '\0' tant
            // qu'aucune relance n'en demande, null si elle en demande plusieurs.
            char? rangDeLaRelance = '\0';

            // Une relance traversée ne peut relancer qu'une suite des nombres (tableau réécrit, ou simple refus en tête).
            var seulementUneSuite = false;

            var recents = messagesDuProfesseur.Reverse().Take(MessagesRemontes).ToList();
            for (var j = 0; j < recents.Count; j++)
            {
                var message = recents[j];
                if (string.IsNullOrWhiteSpace(message)) continue;

                var calculs = CalculsLus(message, out var illisible, out var retenues);
                if (illisible) return null;

                // LA MÊME QUESTION, DÉCOMPOSÉE — tâche 1941 (vu par Omar sur master, conversation de test 457) : « [ARDOISE]
                // 7 + 5 = ? 7 + 3 = 10 Il reste 2 à ajouter : 10 + 2 = ? [/ARDOISE] … Ça fait combien, 10 + 2 ? ». Deux
                // calculs, donc aucun verdict, et « 14 » a reçu « Oui, c'est ça ! 10 + 2 = 12 ». Des calculs qui ont TOUS le
                // même résultat attendent la même réponse : une seule question (ses nombres sont ceux de tous les calculs).
                var operandesEnPlus = new HashSet<decimal>();
                if (calculs.Count > 1 && calculs.Select(c => c.Resultat).Distinct().Count() == 1)
                {
                    foreach (var autre in calculs.Skip(1)) operandesEnPlus.UnionWith(Operandes(autre.Expression));
                    calculs = [calculs[0]];
                }

                var decompositions = Decompositions(message);
                if (decompositions is null) return null;

                // Signalement #53 : « Après 73, qu'est-ce que tu dis ? », « 71, 72, 73… ? ».
                var suites = Suites(message);

                // Signalement #76 : « entre 820 et 802, lequel est le plus grand ? ». Deux comparaisons différentes rendent le message illisible.
                var comparaisons = Comparaisons(message);
                if (comparaisons is null) return null;

                // UNE RELANCE QUE LE JUGE DE SUITE NE SAIT PAS LIRE — tâche 1941 (bout en bout, conv. 749) : « compte
                // encore 5 de plus en disant : 8, 9, 10, 11… à quel nombre tu arrives ? » (le pas annoncé, 5, n'est pas
                // celui de la suite écrite) rendait le message illisible et faisait tomber le calcul posé plus haut : ni
                // verdict, ni protection. Ce n'est illisible que pour une suite : si un calcul est posé plus haut, le
                // message est lu comme ses autres relances (mêmes règles, plus bas).
                if (suites is null)
                {
                    if (calculs.Count + decompositions.Count + comparaisons.Count > 0
                        || j + 1 >= recents.Count
                        || DerniereQuestion(recents.Skip(j + 1).Reverse()) is not { Expression: not null }) return null;
                    suites = [];
                }

                if (calculs.Count + decompositions.Count + suites.Count + comparaisons.Count > 1) return null;

                if (suites.Count == 1)
                {
                    // Une relance « combien d'unités ? » ne parle pas d'une suite.
                    if (rangDeLaRelance != '\0') return null;

                    var s = suites[0];

                    // LA SUITE QUI COMPTE VERS LE RÉSULTAT D'UN CALCUL — tâche 1941 (corpus de test, conversation 489) :
                    // après « Combien font 7 + 5 ? », la relance « Compte à partir de 7 : 8, 9, 10... continue jusqu'où ? » se
                    // lisait comme la suite « 8, 9, 10… ? », qui attend 11 : « 12 », juste, aurait été jugé FAUX. Si un calcul
                    // est posé plus haut et que la suite ne fait que compter vers son résultat (ses nombres et celui qu'elle
                    // attend sont entre les termes et le résultat), c'est le calcul qu'on attend.
                    if (j + 1 < recents.Count
                        && DerniereQuestion(recents.Skip(j + 1).Reverse()) is { Expression: { } exprPlusHaut } calculPlusHaut
                        && decimal.TryParse(calculPlusHaut.Resultat.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var resultatPlusHaut))
                    {
                        var permisDuCalcul = NombresDUneRelance(exprPlusHaut, resultatPlusHaut, []);
                        if (s.Cites.All(c => permisDuCalcul.Contains(c)) && permisDuCalcul.Contains(s.Attendu))
                        {
                            return nombresDesRelances.All(permisDuCalcul.Contains) ? calculPlusHaut : null;
                        }
                    }

                    // Une relance ne cite que les nombres de la suite (« Compte avec moi : 71, 72, 73… »), son pas, 1 ou 10.
                    return nombresDesRelancesSansRefus.All(NombresDUneRelanceDeSuite(s).Contains)
                        ? new Question(null, s.Attendu.ToString(CultureInfo.InvariantCulture), null, s)
                        : null;
                }

                // Un tableau réécrit, ou une relance qui n'est qu'un refus (« Non, … »), ne laisse ouverte qu'une SUITE.
                if (seulementUneSuite && calculs.Count + decompositions.Count + comparaisons.Count > 0) return null;

                if (comparaisons.Count == 1)
                {
                    // Une relance d'une comparaison parle des deux nombres et de leurs chiffres (« Regarde les centaines de
                    // chaque nombre : 347 a combien de centaines, et 582 en a combien ? », séance d'Adam, #76). Une réponse à
                    // cette sous-question (« 3 et 5 ») ne nomme aucun des deux nombres : elle n'a pas de verdict.
                    var c = comparaisons[0];
                    return nombresDesRelances.All(NombresDUneRelanceDeComparaison(c).Contains)
                        ? new Question(null, c.Attendu.ToString(CultureInfo.InvariantCulture), null, Comparaison: c)
                        : null;
                }

                if (calculs.Count == 1)
                {
                    // Une relance « combien d'unités ? » ne parle pas d'un calcul : on ne sait plus ce qui est demandé.
                    if (rangDeLaRelance != '\0') return null;

                    var (expression, resultat) = calculs[0];
                    var operandes = NombresDUneRelance(expression, resultat, operandesEnPlus);
                    return nombresDesRelances.All(operandes.Contains)
                        ? new Question(expression, Ecrire(resultat), null, Retenue: retenues.TryGetValue(expression, out var retenue) ? retenue : null)
                        : null;
                }

                if (decompositions.Count == 1)
                {
                    var d = decompositions[0];

                    // Une relance sur une décomposition parle du nombre, de ses chiffres et de la dizaine : « Regarde :
                    // 20, c'est bien 2 dizaines », « une dizaine, c'est 10 unités ».
                    var permis = d.Attendues.Values.Select(v => (decimal)v).Append(d.N).Append(1).Append(10).Append(100).ToHashSet();
                    if (!nombresDesRelances.All(permis.Contains)) return null;

                    // « Combien d'unités restent, alors ? » après « 20 = 2 dizaines + ? unités » : on attend les unités.
                    if (rangDeLaRelance is char rang && rang != '\0')
                    {
                        if (!d.Attendues.ContainsKey(rang)) return null;
                        d = d with { Demandes = new HashSet<char> { rang }, RangDeRelance = true };
                    }

                    return new Question(null, d.N.ToString(CultureInfo.InvariantCulture), d);
                }

                // Un tableau effacé sans qu'on lise de question : l'exercice d'avant n'est plus affiché.
                if (Regex.IsMatch(message, @"\[TABLEAU_EFFACE\]", RegexOptions.IgnoreCase)) return null;

                // UN TABLEAU RÉÉCRIT POUR RECOMPTER — retest KO #53 (Adam, 22/11/2026, en ligne b5dfd9f) : au 2e essai,
                // Nora a refusé « Sept » en réécrivant la suite, « Pas tout à fait, Adam. On relit ensemble : 78, 79, 80...
                // [ARDOISE]78, 79, 80, ...[/ARDOISE] … », sans question qu'on lise. Un tableau réécrit fermait la question :
                // aucun verdict, et le 2e « sept » a été validé (« c'est bien 81 ! », 2 fois sur 2). Un tableau réécrit
                // laisse donc ouverte une question de SUITE posée plus haut, si le message la relance (mots de relance ou
                // refus en tête) et ne cite que des nombres du recomptage (vérifié quand on la retrouve). Pour un calcul ou
                // une décomposition, rien ne change : le tableau réécrit sans question ferme l'exercice d'avant.
                var tableauReecrit = Regex.IsMatch(message, @"\[ARDOISE\]", RegexOptions.IgnoreCase);

                // Le message le plus récent ne pose rien, ne parle d'aucun nombre : la question d'avant reste ouverte
                // (« Ce n'est pas encore ça. C'est ton dernier essai. »).
                var nombres = NombresDuMessage(message);
                if (!tableauReecrit && !message.Contains('?') && nombres.Count == 0) continue;

                // LA RELANCE DE LA MÊME QUESTION — Minamba, le 20/11/2026, signalement #45 : après « 8 - 3 = ? » au
                // tableau, Nora a relancé « Non, pas tout à fait Adam… Combien il en reste ? », sans réécrire le calcul.
                // Le tableau l'affiche toujours (voir EtatDuTableau) : c'est la même question, et c'est justement au
                // deuxième essai que le professeur recopie son « non » d'avant. Une relance se reconnaît à ses mots
                // (« il en reste », « ça fait », « en tout », « essaie »…) et ne cite aucun autre nombre que ceux du
                // calcul. Sinon (« Maintenant, calcule 9 - 4. », « Combien de billes a Paul ? ») : on ne remonte plus.
                // Une relance ne félicite pas : « Bravo, tu as trouvé le résultat ! Tu en veux un autre ? » clôt la question.
                // Signalement #71 : « Et combien y a-t-il d'unités ? » demande aussi un rang (QuestionDUnRang lit les mêmes
                // formes que RangDemande, et celles en « y a-t-il », « reste-t-il »…).
                var rangs = RangDemande().Matches(message).Concat(QuestionDUnRang().Matches(message))
                    .Select(m => char.ToLowerInvariant(m.Groups["u"].Value[0])).Distinct().ToList();

                // Un refus en tête (« Non, regarde : 78, 79, 80… ») ou une relance douce (« Pas grave, tu peux me le
                // redire ? ») relance aussi une SUITE (retest #53) ; un calcul garde ses seuls mots de relance.
                var refus = (OuvertureQuiRefuse().IsMatch(message.TrimStart()) || RelanceDeSuite().IsMatch(message)) && !AutreChose().IsMatch(message);
                if (!(RelanceDeCalcul().IsMatch(message) || rangs.Count > 0 || refus) || Felicite().IsMatch(message)) return null;
                if (tableauReecrit && rangs.Count > 0) return null;
                if (tableauReecrit || !(RelanceDeCalcul().IsMatch(message) || rangs.Count > 0)) seulementUneSuite = true;

                // UNE RELANCE QUI POSE UNE SOUS-QUESTION — contrôle d'Omar, 20/11/2026 : « Pas encore. Lève 8 doigts.
                // Combien de doigts tu as levés ? » après « 8 - 3 = ? ». L'élève répond 8, juste à CETTE question, et le
                // code le jugeait FAUX sur le calcul. Comme FormesAuTableau.EstRelance : « combien de/d' <nom> » (autre
                // qu'un rang) est une autre question, on ne remonte plus.
                if (CombienDe().Matches(message).Any(m => !EstUnRang(m.Groups["x"].Value))) return null;

                // UNE RELANCE QUI CONVERTIT — contrôle d'Omar : « Une dizaine, c'est 10 unités. Alors 2 dizaines, ça fait
                // combien d'unités ? » attend 20 (le nombre d'unités), pas le chiffre des unités (0). Un rang demandé avec
                // « ça fait », « ça donne », « en tout » n'est pas la case du tableau : pas de verdict.
                if (rangs.Count > 0 && Conversion().IsMatch(message)) return null;

                // Contrôle d'Omar (paquet 1, #71) : « Et des dizaines au total ? », « combien de dizaines de crayons ? » ne
                // relancent pas le chiffre du rang.
                if (rangs.Count > 0 && (TotalDuRang().IsMatch(message) || DesObjets().IsMatch(message))) return null;

                nombresDesRelances.AddRange(nombres);
                var refuses = NombresRefuses(message);
                nombresDesRelancesSansRefus.AddRange(nombres.Where(x => !refuses.Contains(x)));

                // Signalement #46 : seule la relance la plus récente dit ce qu'on attend maintenant.
                if (rangDeLaRelance == '\0' && rangs.Count > 0) rangDeLaRelance = rangs.Count == 1 ? rangs[0] : null;
            }

            return null;
        }

        /// <summary>
        /// Les décompositions demandées dans un message : en toutes lettres (« combien de dizaines et d'unités dans
        /// 20 ? ») ou par un tableau à trous (« 20 = 2 dizaines + ? unités »). Plusieurs nombres : null (illisible).
        /// Un tableau entièrement rempli (« 17 = 1 dizaine + 7 unités », l'exemple) ne demande rien.
        /// </summary>
        private static List<DecompositionPosee>? Decompositions(string message)
        {
            var parNombre = new Dictionary<int, DecompositionPosee>();

            // Signalement #71 : « dans 347, combien y a-t-il de dizaines ? » ne demande qu'un rang.
            var unRang = DecompositionDUnRang(message);

            // La décomposition en toutes lettres ne vaut que si la question parle aussi des unités (ou des
            // centaines) : « combien de dizaines dans 47 ? » attend 4 ou 47 selon la classe, pas une décomposition.
            if (Regex.IsMatch(message, @"unit[ée]s|centaines", RegexOptions.IgnoreCase))
            {
                foreach (Match m in QuestionDecomposition().Matches(message))
                {
                    var n = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);

                    // « Combien de centaines dans 582 ? » ne demande que les centaines : « 5 centaines » n'est pas incomplet.
                    if (unRang is not null && unRang.N == n) continue;

                    var avecCentaines = n >= 100 || Regex.IsMatch(message, "centaines", RegexOptions.IgnoreCase);
                    var attendues = Ordinaire(n, avecCentaines);
                    parNombre[n] = new DecompositionPosee(n, attendues, attendues.Keys.ToHashSet(), true,
                        $"combien de {(avecCentaines ? "centaines, de dizaines" : "dizaines")} et d'unités dans {n}");
                }
            }

            if (unRang is not null && !parNombre.ContainsKey(unRang.N)) parNombre[unRang.N] = unRang;

            foreach (Match m in TableauATrous().Matches(message))
            {
                var tableau = LireTableau(m);
                if (tableau is null) continue;

                // Le tableau dit précisément ce qui manque ; la question en lettres sur le même nombre ne retire rien.
                if (parNombre.TryGetValue(tableau.N, out var enLettres))
                {
                    if (!enLettres.Attendues.All(a => tableau.Attendues.TryGetValue(a.Key, out var v) && v == a.Value)) return null;
                    tableau = tableau with { Demandes = tableau.Demandes.Union(enLettres.Demandes).ToHashSet() };
                }
                parNombre[tableau.N] = tableau;
            }

            return parNombre.Values.ToList();
        }

        /// <summary>
        /// La question en mots sur UN SEUL rang d'un nombre de 10 à 999 (signalement #71) : « Dans ce nombre, combien y
        /// a-t-il de centaines ? » (le nombre est le seul écrit au tableau du message), « dans 347, combien y a-t-il de
        /// dizaines ? », « combien reste-t-il d'unités dans 347 ? », « Combien de centaines dans 582 ? ». Null si le
        /// message demande plusieurs rangs (« combien de dizaines ? et d'unités ? » : la lecture ordinaire s'en charge),
        /// si la phrase en nomme plusieurs, si elle convertit (« dans 2 dizaines, combien d'unités ? »), cite un autre
        /// nombre, ou ne dit pas lequel.
        /// Au CE1, « combien de dizaines dans 347 ? » attend 4 (le chiffre) ou 34 (toutes les dizaines) : les deux se
        /// lisent juste (rang en mots, voir <see cref="JugerDecomposition"/>), seule une réponse qui ne vaut ni l'un ni
        /// l'autre est FAUSSE.
        /// </summary>
        private static DecompositionPosee? DecompositionDUnRang(string message)
        {
            if (QuestionDUnRang().Matches(message).Select(m => char.ToLowerInvariant(m.Groups["u"].Value[0])).Distinct().Count() != 1) return null;

            // Le nombre écrit seul au tableau du message (« [ARDOISE]347[/ARDOISE] »), pour « dans ce nombre ».
            var ardoises = Ardoise().Matches(message);
            int? auTableau = ardoises.Count == 1
                && Regex.Match(ardoises[0].Groups["c"].Value, @"^[\s*_#]*(?<n>\d{1,4})[\s*_#]*$") is { Success: true } seulAuTableau
                ? int.Parse(seulAuTableau.Groups["n"].Value, CultureInfo.InvariantCulture)
                : null;

            var texte = Regex.Replace(Ardoise().Replace(message, "\n"), @"\[[^\]]*\]", " ");
            DecompositionPosee? trouvee = null;
            foreach (Match phrase in Regex.Matches(texte, @"[^.!?\n]*\?"))
            {
                var p = phrase.Value;
                var question = QuestionDUnRang().Match(p);
                if (!question.Success) continue;

                if (RangNomme().Matches(p).Select(m => char.ToLowerInvariant(m.Groups["u"].Value[0])).Distinct().Count() != 1) return null;
                if (Conversion().IsMatch(p)) return null;

                // Contrôle d'Omar (paquet 1) : « au total », « entières » (on attend 34, pas 4) ; des objets groupés (« 25 boîtes
                // de dix crayons ») ou un nombre en lettres dans la phrase (« vingt-cinq boîtes ») : on ne sait pas ce qui est compté.
                if (TotalDuRang().IsMatch(p) || DesObjets().IsMatch(p) || NombreSuiviDUnNom().IsMatch(p) || NombresEnLettres.Lire(p).Any(l => l.Valeur >= 2)) return null;

                var nombres = NombreEnChiffres().Matches(p).Select(m => m.Value).Distinct().ToList();
                int n;
                if (nombres.Count == 0)
                {
                    if (!CeNombre().IsMatch(p) || auTableau is null) return null;
                    n = auTableau.Value;
                }
                else
                {
                    var lus = NombreDuRang().Matches(p).Select(m => m.Groups["n"].Value).Distinct().ToList();
                    if (lus.Count != 1 || nombres.Count != 1 || nombres[0] != lus[0]) return null;
                    n = int.Parse(lus[0], CultureInfo.InvariantCulture);
                }

                var rang = char.ToLowerInvariant(question.Groups["u"].Value[0]);
                if (n < 10 || n > 999 || n < Poids(rang)) return null;

                var attendues = Ordinaire(n, n >= 100);
                var nom = rang switch { 'c' => "de centaines", 'd' => "de dizaines", _ => "d'unités" };
                var lue = new DecompositionPosee(n, attendues, [rang], true, $"combien {nom} dans {n}")
                {
                    RangDeRelance = true,
                    QuestionDUnRang = true,
                };

                // Deux phrases sur des nombres différents : on ne choisit pas.
                if (trouvee is not null && trouvee.N != n) return null;
                trouvee = lue;
            }

            return trouvee;
        }

        /// <summary>
        /// L'indice d'une réponse fausse à une question sur un rang (signalement #71 : « regarde le chiffre du milieu… ») :
        /// la PLACE du chiffre, jamais sa valeur.
        /// </summary>
        private static string IndiceDuRang(int n, char rang)
        {
            var chiffres = n.ToString(CultureInfo.InvariantCulture).Length;
            var (nom, place) = rang switch
            {
                'c' => ("centaines", "le premier chiffre, à gauche"),
                'd' => ("dizaines", chiffres == 3 ? "le chiffre du milieu" : "le premier chiffre, à gauche"),
                _ => ("unités", "le dernier chiffre, à droite"),
            };
            // Sans parenthèse : le professeur recopiait les parenthèses du fait en aparté (tâche 1320).
            var ordre = chiffres == 3 ? "les centaines, les dizaines, puis les unités" : "les dizaines, puis les unités";
            return $"Son erreur : il n'a pas pris le bon chiffre. Fais-lui regarder le chiffre des {nom} de {n} : c'est {place} ; "
                + $"de gauche à droite, on lit {ordre}. Ne lui dis pas ce chiffre.";
        }

        /// <summary>« 20 = 2 dizaines + ? unités » → 20, on attend 2 dizaines et 0 unité, on demande les unités.</summary>
        private static DecompositionPosee? LireTableau(Match m)
        {
            var n = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
            var remplies = new Dictionary<char, int>();
            var vides = new List<char>();

            foreach (Match c in CaseLue().Matches(m.Groups["parties"].Value))
            {
                var rang = char.ToLowerInvariant(c.Groups["u"].Value[0]);
                if (remplies.ContainsKey(rang) || vides.Contains(rang)) return null;

                if (int.TryParse(c.Groups["v"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var v)) remplies[rang] = v;
                else vides.Add(rang);
            }

            if (vides.Count == 0) return null;

            var ordinaire = Ordinaire(n, n >= 100 || remplies.ContainsKey('c') || vides.Contains('c'));
            Dictionary<char, int> attendues;

            if (vides.Count == 1)
            {
                // Une seule case vide : ce qui reste du nombre, dans le rang de la case (« 20 = 1 dizaine + ? unités »
                // attend 10). Si ça ne tombe pas juste, on ne sait pas ce qui est demandé.
                var reste = n - remplies.Sum(r => r.Value * Poids(r.Key));
                if (reste < 0 || reste % Poids(vides[0]) != 0) return null;
                attendues = new Dictionary<char, int>(remplies) { [vides[0]] = reste / Poids(vides[0]) };
            }
            else
            {
                // Plusieurs cases vides : seulement la décomposition ordinaire, rang par rang.
                var rangs = remplies.Keys.Concat(vides).ToHashSet();
                if (!rangs.SetEquals(ordinaire.Keys) || remplies.Any(r => ordinaire[r.Key] != r.Value)) return null;
                attendues = ordinaire;
            }

            var estOrdinaire = attendues.Count == ordinaire.Count && attendues.All(a => ordinaire.TryGetValue(a.Key, out var o) && o == a.Value);
            var enonce = Regex.Replace(m.Value.Trim(), @"\s+", " ");
            return new DecompositionPosee(n, attendues, vides.ToHashSet(), estOrdinaire, enonce);
        }

        private static Dictionary<char, int> Ordinaire(int n, bool avecCentaines) => avecCentaines
            ? new Dictionary<char, int> { ['c'] = n / 100, ['d'] = n / 10 % 10, ['u'] = n % 10 }
            : new Dictionary<char, int> { ['d'] = n / 10, ['u'] = n % 10 };

        private static int Poids(char rang) => rang switch { 'c' => 100, 'd' => 10, _ => 1 };

        /// <summary>Trois messages : la question, puis les deux relances de la règle des trois essais.</summary>
        public const int MessagesRemontes = 3;

        [GeneratedRegex(@"\b(?:il\s+en\s+reste|en\s+tout|ça\s+(?:fait|donne)|ca\s+(?:fait|donne)|cela\s+fait|combien\s+(?:font|fait|ça|ca)"
            + @"|le\s+résultat|essaie|réessaie|recommence|recompte|compte|regarde\s+(?:bien\s+)?(?:le\s+)?tableau|dernier\s+essai"
            + @"|pas\s+(?:tout\s+à\s+fait|encore|ça)|presque)", RegexOptions.IgnoreCase)]
        private static partial Regex RelanceDeCalcul();

        [GeneratedRegex(@"\b(?:oui|bravo|exact|exactement|parfait|super|génial|bien\s+joué|c'est\s+(?:ça|juste|bon))\b", RegexOptions.IgnoreCase)]
        private static partial Regex Felicite();

        private static List<decimal> NombresDuMessage(string message)
        {
            var sansBalises = Regex.Replace(message, @"\[[^\]]*\]", " ");
            return NombreEnChiffres().Matches(sansBalises)
                .Select(m => decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d) ? d : -1m)
                .Concat(NombresEnLettres.Lire(sansBalises).Select(l => (decimal)l.Valeur))
                .ToList();
        }

        /// <summary>
        /// Les nombres qu'une RELANCE d'un calcul peut citer sans parler d'autre chose — tâche 1941 (bout en bout réel,
        /// 3 séances sur 5) : « Montre-moi 7 doigts, puis ajoute un doigt à chaque fois : 8, 9, 10... », « après 7 tu dis
        /// 8, puis 9, puis 10, puis 11… » citaient d'autres nombres que 7 et 5 : le juge abandonnait la question, le
        /// professeur ne recevait aucun fait, validait « 14 » ou donnait 12. Une relance peut citer : les termes, le
        /// comptage de l'un à l'autre jusqu'au résultat (résultat compris), et le passage par la dizaine d'une addition
        /// (7 + 5 = 7 + 3 + 2 : 10, 3 et 2).
        /// </summary>
        private static HashSet<decimal> NombresDUneRelance(string expression, decimal resultat, HashSet<decimal> operandesEnPlus)
        {
            var permis = Operandes(expression);
            permis.UnionWith(operandesEnPlus);
            var bornes = permis.Append(resultat).ToList();

            // « 1 de plus », « on avance de 1 en 1 » (le pas d'une suite qui compte vers le résultat, conversation 696), et
            // les doigts d'un terme qu'on compte un par un : « ajoute 1, 2, 3, 4, 5 en comptant : 8, 9, 10, 11… » (693).
            permis.Add(1);
            foreach (var terme in Operandes(expression).Where(t => t == Math.Floor(t) && t >= 1 && t <= 20))
            {
                for (var n = 1m; n <= terme; n++) permis.Add(n);
            }
            var (min, max) = (bornes.Min(), bornes.Max());
            if (bornes.All(b => b == Math.Floor(b)) && min >= 0 && max - min <= 30)
            {
                for (var n = min; n <= max; n++) permis.Add(n);
            }

            // Le passage par 10 : « 7 + 5 » → 7 + 3 = 10, puis 10 + 2.
            var termes = NombreEnChiffres().Matches(expression).Select(m => decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture)).ToList();
            if (termes.Count == 2 && expression.Contains('+') && resultat > 10)
            {
                foreach (var (a, b) in new[] { (termes[0], termes[1]), (termes[1], termes[0]) })
                {
                    var complement = 10 - a;
                    if (complement > 0 && complement < b) permis.UnionWith([10, complement, b - complement]);
                }
            }

            return permis;
        }

        private static HashSet<decimal> Operandes(string expression) =>
            NombreEnChiffres().Matches(expression)
                .Select(m => decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture))
                .ToHashSet();

        private static List<(string Expression, decimal Resultat)> Calculs(string message, out bool illisible) =>
            CalculsLus(message, out illisible, out _);

        /// <summary>
        /// LA RETENUE NOMMÉE DANS LE CALCUL — Mins, le 24/11/2026, signalement #70 (Adam, CE1, addition posée 27 + 15) :
        /// « Maintenant les dizaines : combien ça fait, 2 + 1 + la retenue de 1 ? ». Le juge lisait « 2 + 1 » (les mots
        /// « la retenue de » coupaient le calcul), comptait 3, et disait au professeur « L'élève a répondu 3 : c'est JUSTE,
        /// ta réponse COMMENCE par Oui ». Nora a validé l'oubli de la retenue — l'erreur type du CE1 —, puis écrit 42.
        /// La retenue nommée est un terme du calcul : « + la retenue de 1 » se lit « + 1 », « + 1 (la retenue) » aussi.
        /// Sans valeur (« 2 + 1 + la retenue ? »), elle vaut 1 quand la colonne additionne deux chiffres (une addition
        /// de deux chiffres retient au plus 1) ; sinon on ne la devine pas, et le calcul coupé ne se juge pas (plus bas).
        /// Les retenues lues sont rendues par calcul (expression réécrite → valeur), pour l'indice « retenue oubliée ».
        /// </summary>
        [GeneratedRegex(@"(?<op>\+|(?<!\p{L})plus)\s*(?:la|ta)\s+retenue(?!\p{L})(?:\s*(?:de|qui\s+vaut|:)\s*(?<n>\d)(?!\d|,\d))?", RegexOptions.IgnoreCase)]
        private static partial Regex RetenueNommee();

        [GeneratedRegex(@"(?<!\d|\d,)(?<n>\d)\s*\(\s*(?:c['’]est\s+)?(?:la|ta)\s+retenue\s*\)", RegexOptions.IgnoreCase)]
        private static partial Regex RetenueEntreParentheses();

        // « 2 + 1 » juste avant « + la retenue » : deux chiffres seuls (pas « 20 + 10 », pas « 3 × 4 »). Le calcul ENTIER est
        // « chiffre + chiffre » (contrôle d'Omar, paquet 1) : rien devant (« 1 + 1 + 2 + la retenue », « 7 × 2 + 3 + la
        // retenue » se lisaient « 2 + la retenue de 1 » et le juge rendait JUSTE une réponse fausse).
        // Devant le premier chiffre : ni chiffre collé (« 12 », « 2,5 »), ni opérateur (« 1 + 1 + 2 », « 7 × 2 + 3 », « 8 : 2 + 1 »),
        // ni mot d'opération ; « Les dizaines : 2 + 1 », « ça fait, 2 + 1 » restent permis.
        [GeneratedRegex(@"(?<![\d,.])(?<![+\-−–×*÷/=^][ \t]*)(?<!\d[ \t]*[:,.][ \t]*)(?<!(?<!\p{L})(?:x|plus|moins|fois|par)[ \t]*)\d[ \t]*(?:\+|plus)[ \t]*\d[ \t]*$", RegexOptions.IgnoreCase)]
        private static partial Regex DeuxChiffresAvant();

        // Après « + la retenue », le calcul continue (« 2 + 1 + la retenue + 3 ») : la retenue de « 2 + 1 » ne se devine plus.
        [GeneratedRegex(@"\G[ \t]*(?:[+\-−–×*:÷/]|(?:plus|moins|fois)(?!\p{L}))", RegexOptions.IgnoreCase)]
        private static partial Regex SuiteApresLaRetenue();

        // « + la retenue de 10 », « + la retenue (1) » : une valeur qu'on n'a pas lue ; on ne remplace pas par 1.
        [GeneratedRegex(@"\G\s*(?:(?:de|qui\s+vaut|:)\s*)?\(?\s*\d", RegexOptions.IgnoreCase)]
        private static partial Regex ValeurNonLue();

        // Les retenues qui font partie du calcul sans être réécrites : « la retenue de 1 + 2 + 1 » (le 1 est lu dans le
        // calcul), « 2 + 1 + 1 de retenue ».
        [GeneratedRegex(@"(?<!\p{L})(?:la|ta)\s+retenue\s+(?:de|qui\s+vaut)\s+\d(?=[ \t]*(?:\+|plus(?!\p{L})))|\d[ \t]+de\s+retenue(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex RetenueDansLeCalcul();

        [GeneratedRegex(@"(?<!\p{L})retenue?s?(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex MotRetenue();

        /// <summary>
        /// UN CALCUL COUPÉ NE SE JUGE PAS — signalement #70 : après ce qu'on a lu vient encore un opérateur (« 4 + 3 + les 2
        /// de tout à l'heure », « 2 plus 1 plus ce que tu as retenu ») : on n'a lu que le début du calcul, son résultat
        /// n'est pas celui de la question. Avant, le juge rendait le verdict du début, et validait la réponse fausse.
        /// Contrôle d'Omar (paquet 1) : aussi après « - », « − », « : », « / » (« 7 - 4 - la retenue de 1 ? » rendait JUSTE
        /// « 3 », « 9 - 3 - les 2 de tout à l'heure » rendait JUSTE « 6 »).
        /// </summary>
        [GeneratedRegex(@"\G[ \t]*(?:[+\-−–×*÷:/]|x(?!\p{L})|(?:plus|moins|fois)(?!\p{L}))", RegexOptions.IgnoreCase)]
        private static partial Regex CalculQuiContinue();

        private static string AvecLesRetenues(string message, List<(int Debut, decimal Valeur)> retenues)
        {
            var lues = RetenueNommee().Matches(message).Select(m => (m, Nommee: true))
                .Concat(RetenueEntreParentheses().Matches(message).Select(m => (m, Nommee: false)))
                .OrderBy(x => x.m.Index)
                .ToList();
            if (lues.Count == 0) return message;

            var sortie = new System.Text.StringBuilder();
            var fin = 0;
            foreach (var (m, nommee) in lues)
            {
                if (m.Index < fin) continue;

                // « 2 + 1, plus la retenue de 1 » : la retenue vient APRÈS le calcul, séparée par une virgule ; ce n'est pas
                // un terme de « 2 + 1 » (contrôle d'Omar, paquet 1). On ne la réécrit pas, et la phrase ne se juge pas (plus bas).
                if (nommee && !Regex.IsMatch(message[..m.Index], @"\d[ \t]*$")) continue;

                int? valeur = m.Groups["n"].Success ? m.Groups["n"].Value[0] - '0' : null;
                if (valeur is null && nommee
                    && DeuxChiffresAvant().IsMatch(message[..m.Index])
                    && !SuiteApresLaRetenue().IsMatch(message, m.Index + m.Length)
                    && !ValeurNonLue().IsMatch(message, m.Index + m.Length)) valeur = 1;
                if (valeur is null) continue;

                sortie.Append(message, fin, m.Index - fin);
                if (nommee) sortie.Append(m.Groups["op"].Value).Append(' ');
                retenues.Add((sortie.Length, valeur.Value));
                sortie.Append(valeur.Value.ToString(CultureInfo.InvariantCulture));
                fin = m.Index + m.Length;
            }
            sortie.Append(message, fin, message.Length - fin);
            return sortie.ToString();
        }

        /// <summary>
        /// La phrase du calcul (entre deux « . », « ! », « ? » ou sauts de ligne) nomme encore une retenue qui n'est pas un
        /// terme du calcul lu (message déjà réécrit par <see cref="AvecLesRetenues"/>).
        /// </summary>
        private static bool RetenueHorsDuCalcul(string message, int debut, int finExpr)
        {
            var avant = message.LastIndexOfAny(['.', '!', '?', '\n'], Math.Max(debut - 1, 0));
            var apres = message.IndexOfAny(['.', '!', '?', '\n'], finExpr);
            var (de, a) = (avant < 0 ? 0 : avant + 1, apres < 0 ? message.Length : apres);
            if (debut == 0) de = 0;
            var phrase = message[de..a];

            var dansLeCalcul = RetenueDansLeCalcul().Matches(phrase);
            return MotRetenue().Matches(phrase).Any(r => !dansLeCalcul.Any(c => r.Index >= c.Index && r.Index < c.Index + c.Length));
        }

        private static List<(string Expression, decimal Resultat)> CalculsLus(string message, out bool illisible, out Dictionary<string, decimal> retenuesDesCalculs)
        {
            var trouves = new List<(string, decimal)>();
            illisible = false;
            retenuesDesCalculs = new Dictionary<string, decimal>();

            var retenues = new List<(int Debut, decimal Valeur)>();
            message = AvecLesRetenues(message, retenues);

            foreach (var motif in new[] { EgalQuestion(), CombienFont(), ExpressionInterrogee() })
            {
                foreach (Match m in motif.Matches(message))
                {
                    var expression = m.Groups["expr"].Value;
                    var (debut, finExpr) = (m.Groups["expr"].Index, m.Groups["expr"].Index + m.Groups["expr"].Length);

                    if (CalculQuiContinue().IsMatch(message, finExpr))
                    {
                        illisible = true;
                        continue;
                    }

                    // UNE RETENUE DANS LA PHRASE, HORS DU CALCUL — contrôle d'Omar (paquet 1) : « 2 + 1, plus la retenue de 1 »,
                    // « 2 + 1, sans oublier la retenue », « 2 + 1 et la retenue » rendaient JUSTE « 3 ». La retenue lue dans le
                    // calcul est déjà réécrite en nombre ; s'il en reste une dans la même phrase, on ne sait pas ce qui est demandé.
                    // Sauf si le calcul contient déjà un terme de retenue lu (contrôle d'Omar, paquet 1 v3, V22) : « Maintenant
                    // les dizaines, sans oublier la retenue : combien ça fait, 2 + 1 + la retenue de 1 ? » annonce la retenue
                    // que le calcul additionne ; le juge ne rendait aucun verdict et laissait passer « 3 ».
                    var retenueLueDansLeCalcul = retenues.Any(r => r.Debut >= debut && r.Debut < finExpr);
                    if (!retenueLueDansLeCalcul && RetenueHorsDuCalcul(message, debut, finExpr))
                    {
                        illisible = true;
                        continue;
                    }

                    // « Question 2 : 6 : 3 = ? » — le « : » d'un intitulé se lirait comme une division.
                    if (expression.Contains(':') && Regex.IsMatch(message[..m.Groups["expr"].Index], @"[\p{L}°]\s*$"))
                    {
                        illisible = true;
                        continue;
                    }

                    var lu = Evaluer(expression);
                    if (lu is null)
                    {
                        // Une question bien posée qu'on ne sait pas calculer : on ne juge rien plutôt que de juger
                        // une autre question du même message.
                        illisible = true;
                        continue;
                    }

                    if (!trouves.Any(t => t.Item1 == lu.Value.Ecrite)) trouves.Add((lu.Value.Ecrite, lu.Value.Valeur));

                    var dedans = retenues.Where(r => r.Debut >= debut && r.Debut < finExpr).ToList();
                    if (dedans.Count == 1) retenuesDesCalculs[lu.Value.Ecrite] = dedans[0].Valeur;
                }
            }

            return trouves;
        }

        /// <summary>
        /// L'expression réécrite proprement (« 4 + 3 ») et sa valeur, priorités respectées. Null pour une division
        /// qui ne tombe pas juste : on ne juge pas un arrondi.
        /// </summary>
        public static (string Ecrite, decimal Valeur)? Evaluer(string expression)
        {
            var jetons = Jeton().Matches(expression).Select(m => m.Value).ToList();
            if (jetons.Count < 3 || jetons.Count % 2 == 0) return null;

            var nombres = new List<decimal>();
            var operateurs = new List<char>();

            for (var i = 0; i < jetons.Count; i++)
            {
                if (i % 2 == 0)
                {
                    if (!decimal.TryParse(jetons[i].Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var nombre))
                        return null;
                    nombres.Add(nombre);
                }
                else
                {
                    var op = Operateur(jetons[i]);
                    if (op is null) return null;
                    operateurs.Add(op.Value);
                }
            }

            // « 12 : 3 + 1 » existe, mais « Exercice 1 : 4 + 3 » aussi : un « : » mêlé à d'autres opérations ne se
            // juge pas.
            if (operateurs.Contains(':') && operateurs.Any(o => o != ':')) return null;

            var ecrite = new System.Text.StringBuilder(Ecrire(nombres[0]));
            for (var i = 0; i < operateurs.Count; i++) ecrite.Append(' ').Append(operateurs[i]).Append(' ').Append(Ecrire(nombres[i + 1]));

            try
            {
                // D'abord les multiplications et les divisions, de gauche à droite.
                var termes = new List<decimal> { nombres[0] };
                var signes = new List<char>();
                for (var i = 0; i < operateurs.Count; i++)
                {
                    var suivant = nombres[i + 1];
                    switch (operateurs[i])
                    {
                        case '×':
                            termes[^1] *= suivant;
                            break;
                        case ':':
                            if (suivant == 0) return null;
                            var quotient = termes[^1] / suivant;
                            if (Math.Round(quotient, 6) != quotient || quotient * suivant != termes[^1]) return null;
                            termes[^1] = quotient;
                            break;
                        default:
                            signes.Add(operateurs[i]);
                            termes.Add(suivant);
                            break;
                    }
                }

                var valeur = termes[0];
                for (var i = 0; i < signes.Count; i++) valeur = signes[i] == '+' ? valeur + termes[i + 1] : valeur - termes[i + 1];
                return (ecrite.ToString(), valeur);
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        private static char? Operateur(string jeton) => jeton.ToLowerInvariant() switch
        {
            "+" or "plus" => '+',
            "-" or "−" or "–" or "moins" => '-',
            "×" or "x" or "*" or "fois" => '×',
            ":" or "÷" => ':',
            var j when j.StartsWith("divis") => ':',
            _ => null,
        };

        private static string Ecrire(decimal d) =>
            d.ToString("0.######", CultureInfo.InvariantCulture).Replace('.', ',');

        // ───────────── Le calcul ─────────────

        private static Verdict? JugerCalcul(string expression, string attendu, string reponse)
        {
            // Un résultat négatif (« moins 1 ») ne se lit pas encore : pas de verdict.
            if (attendu.StartsWith('-')) return null;

            // « 4 + 3 = 7 » : c'est ce qui suit le « = » qui répond — à condition que ce soit LE calcul posé.
            // Minamba, le 20/11/2026, signalement #45 (étape 5) : « tu as dit que 4 + 3 = 8 et 9 - 4 = 7, c'est
            // vrai ? », alors que le tableau affichait 6 + 2. Ce n'est pas une réponse : c'est une question sur
            // d'autres calculs, et le 7 n'a rien à voir avec 6 + 2. Plusieurs « = », ou un autre calcul avant le
            // « = » : pas de verdict.
            if (reponse.Count(c => c == '=') > 1) return null;
            if (reponse.Contains('='))
            {
                var avant = reponse[..reponse.IndexOf('=')];
                var calculCite = Regex.Match(avant, ExpressionMotif, RegexOptions.IgnoreCase);
                if (calculCite.Success)
                {
                    if (Evaluer(calculCite.Value)?.Ecrite != expression) return null;
                }
                else if (avant.Any(char.IsDigit))
                {
                    return null;
                }
            }

            var dit = reponse.Contains('=') ? reponse[(reponse.LastIndexOf('=') + 1)..] : reponse;
            var valeur = ValeurUnique(dit);
            if (valeur is null) return null;

            var donne = Ecrire(valeur.Value);
            return new Verdict(expression + " = ?", attendu, donne, donne == attendu ? Jugement.Juste : Jugement.Faux);
        }

        /// <summary>La seule valeur que porte la réponse (« 7 », « sept », « c'est 7 je crois »), sinon null.</summary>
        private static decimal? ValeurUnique(string dit) =>
            Valeurs(dit) is { Count: 1 } une ? une[0] : null;

        /// <summary>Les valeurs que porte la réponse, sans doublon ; null si elle ne se lit pas comme une réponse.</summary>
        private static List<decimal>? Valeurs(string dit)
        {
            // Un nombre négatif (« moins 1 ») ne se lit pas encore : pas de verdict.
            if (Regex.IsMatch(dit, @"[-−–]\s*\d|\bmoins\b", RegexOptions.IgnoreCase)) return null;
            if (PasUneReponse().IsMatch(dit)) return null;

            var valeurs = NombreEnChiffres().Matches(dit)
                .Select(m => decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
                .ToList();
            if (valeurs.Any(v => v is null)) return null;

            return valeurs.Select(v => v!.Value)
                .Concat(NombresEnLettres.Lire(dit).Select(l => (decimal)l.Valeur))
                .Distinct()
                .ToList();
        }

        // ───────────── La décomposition ─────────────

        private static Verdict? JugerDecomposition(DecompositionPosee d, string reponse)
        {
            var parties = new Dictionary<char, int>();

            foreach (Match m in PartieDecomposee().Matches(reponse))
            {
                var valeur = ValeurDuMot(m.Groups["v"].Value);
                if (valeur is null) return null;

                var rang = char.ToLowerInvariant(m.Groups["u"].Value[0]);
                if (parties.TryGetValue(rang, out var deja) && deja != valeur.Value) return null;
                parties[rang] = valeur.Value;
            }

            if (parties.Count == 0)
            {
                // « 0 » tout court, quand on ne demande qu'un rang (« 20 = 2 dizaines + ? unités », « Combien d'unités
                // restent ? ») — signalement #46, étape 4. Quand on demande tout, un nombre seul ne se lit pas.
                if (d.Demandes.Count != 1) return null;
                var seul = ValeurUnique(reponse);
                if (seul is null || seul.Value != Math.Floor(seul.Value) || seul.Value > 9999) return null;

                var rang = d.Demandes.Single();

                // Rang venu d'une relance en mots : « 20 » pour « combien d'unités ? » est le nombre d'unités de 20, une
                // lecture juste de la question (contrôle d'Omar). On ne le juge pas faux. De même « 34 » pour « combien de
                // dizaines dans 347 ? » : toutes ses dizaines (signalement #71).
                if (d.RangDeRelance && (int)seul.Value != d.Attendues[rang] && (int)seul.Value == d.N / Poids(rang)) return null;

                // « 40 » ou « quarante » pour « combien de dizaines dans 347 ? » : la VALEUR du bon chiffre, pas un mauvais
                // chiffre ; l'indice « il n'a pas pris le bon chiffre » serait faux (contrôle d'Omar, paquet 1). Pas de verdict.
                if (d.QuestionDUnRang && Poids(rang) > 1 && d.Attendues[rang] != 0 && (int)seul.Value == d.Attendues[rang] * Poids(rang)) return null;

                var attenduDuRang = new Dictionary<char, int> { [rang] = d.Attendues[rang] };
                var donneDuRang = new Dictionary<char, int> { [rang] = (int)seul.Value };
                return AvecIndiceDuRang(d, new Verdict(d.Enonce, Decrire(attenduDuRang), Decrire(donneDuRang),
                    (int)seul.Value == d.Attendues[rang] ? Jugement.Juste : Jugement.Faux));
            }

            // « 2 dizaines » quand le tableau ne demande que les unités : il ne répond pas à la question posée.
            if (!parties.Keys.Any(d.Demandes.Contains)) return null;

            // « 34 dizaines » pour « combien de dizaines dans 347 ? » : toutes ses dizaines, une lecture juste de la
            // question (signalement #71). On ne le juge pas faux.
            if (d.RangDeRelance && d.Demandes.Count == 1 && parties.Count == 1
                && parties.TryGetValue(d.Demandes.Single(), out var seulRang) && seulRang != d.Attendues[d.Demandes.Single()]
                && seulRang == d.N / Poids(d.Demandes.Single())) return null;

            // Des centaines dans une question qui n'en parle pas : on compte quand même leur valeur.
            // Une question sur un seul rang (#71) n'attend que ce rang.
            var total = parties.Sum(p => p.Value * Poids(p.Key));
            var attendu = Decrire(d.QuestionDUnRang
                ? d.Attendues.Where(a => d.Demandes.Contains(a.Key)).ToDictionary(a => a.Key, a => a.Value)
                : d.Attendues);
            var donne = Decrire(parties);

            var conformes = parties.All(p => d.Attendues.TryGetValue(p.Key, out var a) && a == p.Value);
            if (conformes)
            {
                var manque = d.Demandes.Any(r => !parties.ContainsKey(r) && d.Attendues[r] != 0);
                return new Verdict(d.Enonce, attendu, donne, manque ? Jugement.Incomplet : Jugement.Juste);
            }

            // Le bon nombre, autrement décomposé : « essaie avec le moins d'unités possible » n'a de sens que si on
            // attendait la décomposition ordinaire. Sinon (« 20 = 1 dizaine + ? unités »), on ne tranche pas.
            // Après une relance en mots (« combien d'unités ? »), « 20 unités » répond aussi : on ne tranche pas.
            if (total == d.N) return d.Ordinaire && !d.RangDeRelance ? new Verdict(d.Enonce, attendu, donne, Jugement.AutreDecomposition) : null;

            return AvecIndiceDuRang(d, new Verdict(d.Enonce, attendu, donne, Jugement.Faux));
        }

        /// <summary>Une réponse fausse à une question sur un seul rang (#71) reçoit l'indice de la place du chiffre.</summary>
        private static Verdict AvecIndiceDuRang(DecompositionPosee d, Verdict v) =>
            v.Jugement == Jugement.Faux && d.QuestionDUnRang && d.Demandes.Count == 1
                ? v with { Indice = IndiceDuRang(d.N, d.Demandes.Single()) }
                : v;

        private static int? ValeurDuMot(string mot)
        {
            var m = mot.Trim().ToLowerInvariant();
            if (int.TryParse(m, NumberStyles.None, CultureInfo.InvariantCulture, out var entier)) return entier;
            if (m is "un" or "une") return 1;

            var lus = NombresEnLettres.Lire(m);
            return lus.Count == 1 && lus[0].Dit.Length == m.Length && lus[0].Valeur <= 9999 ? (int)lus[0].Valeur : null;
        }

        private static string Decrire(Dictionary<char, int> parties)
        {
            var morceaux = new List<string>();
            foreach (var (rang, nom) in new[] { ('c', "centaine"), ('d', "dizaine"), ('u', "unité") })
            {
                if (parties.TryGetValue(rang, out var v)) morceaux.Add($"{v} {nom}{(v > 1 ? "s" : "")}");
            }
            return morceaux.Count <= 1
                ? string.Join("", morceaux)
                : string.Join(", ", morceaux.Take(morceaux.Count - 1)) + " et " + morceaux[^1];
        }
    }
}
