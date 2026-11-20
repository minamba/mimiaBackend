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
        public sealed record Verdict(string Question, string Attendu, string Donne, Jugement Jugement);

        // Un nombre : entier ou décimal à virgule (« 2,3 »). Neuf chiffres au plus : pas de débordement.
        private const string NombreMotif = @"\d{1,9}(?:,\d{1,6})?";
        private const string OperateurMotif = @"(?:[+\-−–×x*:÷]|plus|moins|fois|divis[ée]\s+par)";
        private const string ExpressionMotif = NombreMotif + @"(?:\s*" + OperateurMotif + @"\s*" + NombreMotif + @")+";

        // Pas le morceau d'une expression plus longue qu'on ne sait pas lire : « (2 + 3) × 4 », « 1/2 + 1/4 »,
        // « 3² + 4 », « -4 + 3 », « 1 000 + 200 ». Les deux-points restent permis devant (« Calcule : 4 + 3 = ? »),
        // un mot aussi (« Calcule 4 + 3 »), le crochet de [ARDOISE] aussi.
        private const string PasDeSuiteAvant = @"(?<![\d,./()^²³+\-−–×*÷=]\s*)(?<![\p{L}\d])";
        private const string PasDeSuiteApres = @"(?!\s*[\d/^²³(])";

        /// <summary>« 4 + 3 = ? », « 23 × 14 = … », « 9 - 4 = ___ ».</summary>
        [GeneratedRegex(PasDeSuiteAvant + @"(?<expr>" + ExpressionMotif + @")\s*=\s*(?:\?|…|\.{2,}|_+)", RegexOptions.IgnoreCase)]
        private static partial Regex EgalQuestion();

        /// <summary>« Combien font 4 + 3 ? », « combien fait 8 moins 3 ».</summary>
        [GeneratedRegex(@"combien\s+(?:font|fait|ça\s+fait|ca\s+fait|égalent|égale)\s+(?<expr>" + ExpressionMotif + @")" + PasDeSuiteApres
            + @"(?!\s*=)", RegexOptions.IgnoreCase)]
        private static partial Regex CombienFont();

        /// <summary>« 4 + 3, ça fait combien ? », « Et 8 - 3 ? ».</summary>
        [GeneratedRegex(PasDeSuiteAvant + @"(?<expr>" + ExpressionMotif + @")\s*(?:,?\s*(?:ça|ca|cela)\s+fait\s+combien|\?)",
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

            return question.Value.Decomposition is { } decomposition
                ? JugerDecomposition(decomposition, reponse)
                : JugerCalcul(question.Value.Expression!, question.Value.Resultat, reponse);
        }

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
                + "n'est pas encore ça et aide-le à retrouver le résultat sans le lui donner tout de suite (règle des trois essais).]",
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

        private readonly record struct Question(string? Expression, string Resultat, DecompositionPosee? Decomposition);

        /// <summary>
        /// Une décomposition à donner : le nombre, la valeur attendue de chaque rang (c, d, u), les rangs qu'on demande
        /// vraiment (les cases vides du tableau, ou tous pour « combien de dizaines et d'unités dans 20 ? ») et si la
        /// décomposition attendue est l'ordinaire (2 dizaines et 0 unité) ou une autre que le tableau impose
        /// (« 20 = 1 dizaine + ? unités » attend 10 unités).
        /// </summary>
        private sealed record DecompositionPosee(int N, Dictionary<char, int> Attendues, HashSet<char> Demandes, bool Ordinaire, string Enonce);

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

            // Le rang que demande la relance la plus récente (« Combien d'unités restent, alors ? ») : '\0' tant
            // qu'aucune relance n'en demande, null si elle en demande plusieurs.
            char? rangDeLaRelance = '\0';

            foreach (var message in messagesDuProfesseur.Reverse().Take(MessagesRemontes))
            {
                if (string.IsNullOrWhiteSpace(message)) continue;

                var calculs = Calculs(message, out var illisible);
                if (illisible) return null;

                var decompositions = Decompositions(message);
                if (decompositions is null) return null;

                if (calculs.Count + decompositions.Count > 1) return null;

                if (calculs.Count == 1)
                {
                    // Une relance « combien d'unités ? » ne parle pas d'un calcul : on ne sait plus ce qui est demandé.
                    if (rangDeLaRelance != '\0') return null;

                    var (expression, resultat) = calculs[0];
                    var operandes = Operandes(expression);
                    return nombresDesRelances.All(operandes.Contains)
                        ? new Question(expression, Ecrire(resultat), null)
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
                        d = d with { Demandes = new HashSet<char> { rang } };
                    }

                    return new Question(null, d.N.ToString(CultureInfo.InvariantCulture), d);
                }

                // Un tableau réécrit ou effacé sans qu'on lise de question : l'exercice d'avant n'est plus affiché.
                if (Regex.IsMatch(message, @"\[(?:ARDOISE|TABLEAU_EFFACE)\]", RegexOptions.IgnoreCase)) return null;

                // Le message le plus récent ne pose rien, ne parle d'aucun nombre : la question d'avant reste ouverte
                // (« Ce n'est pas encore ça. C'est ton dernier essai. »).
                var nombres = NombresDuMessage(message);
                if (!message.Contains('?') && nombres.Count == 0) continue;

                // LA RELANCE DE LA MÊME QUESTION — Minamba, le 20/11/2026, signalement #45 : après « 8 - 3 = ? » au
                // tableau, Nora a relancé « Non, pas tout à fait Adam… Combien il en reste ? », sans réécrire le calcul.
                // Le tableau l'affiche toujours (voir EtatDuTableau) : c'est la même question, et c'est justement au
                // deuxième essai que le professeur recopie son « non » d'avant. Une relance se reconnaît à ses mots
                // (« il en reste », « ça fait », « en tout », « essaie »…) et ne cite aucun autre nombre que ceux du
                // calcul. Sinon (« Maintenant, calcule 9 - 4. », « Combien de billes a Paul ? ») : on ne remonte plus.
                // Une relance ne félicite pas : « Bravo, tu as trouvé le résultat ! Tu en veux un autre ? » clôt la question.
                var rangs = RangDemande().Matches(message).Select(m => char.ToLowerInvariant(m.Groups["u"].Value[0])).Distinct().ToList();
                if (!(RelanceDeCalcul().IsMatch(message) || rangs.Count > 0) || Felicite().IsMatch(message)) return null;
                nombresDesRelances.AddRange(nombres);

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

            // La décomposition en toutes lettres ne vaut que si la question parle aussi des unités (ou des
            // centaines) : « combien de dizaines dans 47 ? » attend 4 ou 47 selon la classe, pas une décomposition.
            if (Regex.IsMatch(message, @"unit[ée]s|centaines", RegexOptions.IgnoreCase))
            {
                foreach (Match m in QuestionDecomposition().Matches(message))
                {
                    var n = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
                    var avecCentaines = n >= 100 || Regex.IsMatch(message, "centaines", RegexOptions.IgnoreCase);
                    var attendues = Ordinaire(n, avecCentaines);
                    parNombre[n] = new DecompositionPosee(n, attendues, attendues.Keys.ToHashSet(), true,
                        $"combien de {(avecCentaines ? "centaines, de dizaines" : "dizaines")} et d'unités dans {n}");
                }
            }

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

        private static HashSet<decimal> Operandes(string expression) =>
            NombreEnChiffres().Matches(expression)
                .Select(m => decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture))
                .ToHashSet();

        private static List<(string Expression, decimal Resultat)> Calculs(string message, out bool illisible)
        {
            var trouves = new List<(string, decimal)>();
            illisible = false;

            foreach (var motif in new[] { EgalQuestion(), CombienFont(), ExpressionInterrogee() })
            {
                foreach (Match m in motif.Matches(message))
                {
                    var expression = m.Groups["expr"].Value;

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
        private static decimal? ValeurUnique(string dit)
        {
            // Un nombre négatif (« moins 1 ») ne se lit pas encore : pas de verdict.
            if (Regex.IsMatch(dit, @"[-−–]\s*\d|\bmoins\b", RegexOptions.IgnoreCase)) return null;

            var valeurs = NombreEnChiffres().Matches(dit)
                .Select(m => decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
                .ToList();
            if (valeurs.Any(v => v is null)) return null;

            var toutes = valeurs.Select(v => v!.Value)
                .Concat(NombresEnLettres.Lire(dit).Select(l => (decimal)l.Valeur))
                .Distinct()
                .ToList();

            return toutes.Count == 1 ? toutes[0] : null;
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
                var attenduDuRang = new Dictionary<char, int> { [rang] = d.Attendues[rang] };
                var donneDuRang = new Dictionary<char, int> { [rang] = (int)seul.Value };
                return new Verdict(d.Enonce, Decrire(attenduDuRang), Decrire(donneDuRang),
                    (int)seul.Value == d.Attendues[rang] ? Jugement.Juste : Jugement.Faux);
            }

            // « 2 dizaines » quand le tableau ne demande que les unités : il ne répond pas à la question posée.
            if (!parties.Keys.Any(d.Demandes.Contains)) return null;

            // Des centaines dans une question qui n'en parle pas : on compte quand même leur valeur.
            var total = parties.Sum(p => p.Value * Poids(p.Key));
            var attendu = Decrire(d.Attendues);
            var donne = Decrire(parties);

            var conformes = parties.All(p => d.Attendues.TryGetValue(p.Key, out var a) && a == p.Value);
            if (conformes)
            {
                var manque = d.Demandes.Any(r => !parties.ContainsKey(r) && d.Attendues[r] != 0);
                return new Verdict(d.Enonce, attendu, donne, manque ? Jugement.Incomplet : Jugement.Juste);
            }

            // Le bon nombre, autrement décomposé : « essaie avec le moins d'unités possible » n'a de sens que si on
            // attendait la décomposition ordinaire. Sinon (« 20 = 1 dizaine + ? unités »), on ne tranche pas.
            if (total == d.N) return d.Ordinaire ? new Verdict(d.Enonce, attendu, donne, Jugement.AutreDecomposition) : null;

            return new Verdict(d.Enonce, attendu, donne, Jugement.Faux);
        }

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
