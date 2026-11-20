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

            return question.Value.Decomposition is int n
                ? JugerDecomposition(n, question.Value.AvecCentaines, reponse)
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

        private readonly record struct Question(string? Expression, string Resultat, int? Decomposition, bool AvecCentaines);

        /// <summary>
        /// La question du message le plus récent qui en pose une. Un message sans « ? » (« Ce n'est pas encore ça.
        /// C'est ton dernier essai. ») laisse la question d'avant ouverte ; un message qui pose une AUTRE question,
        /// qu'on ne sait pas lire, la ferme. Deux questions différentes dans le même message : on ne choisit pas.
        /// </summary>
        private static Question? DerniereQuestion(IEnumerable<string?> messagesDuProfesseur)
        {
            foreach (var message in messagesDuProfesseur.Reverse().Take(2))
            {
                if (string.IsNullOrWhiteSpace(message)) continue;

                var calculs = Calculs(message, out var illisible);
                if (illisible) return null;

                var decompositions = QuestionDecomposition().Matches(message)
                    .Select(m => int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture))
                    .Distinct()
                    .ToList();

                // La décomposition ne vaut que si la question parle aussi des unités (ou des centaines) : « combien
                // de dizaines dans 47 ? » attend 4, pas une décomposition.
                if (decompositions.Count > 0 && !Regex.IsMatch(message, @"unit[ée]s|centaines", RegexOptions.IgnoreCase))
                {
                    decompositions.Clear();
                }

                if (calculs.Count + decompositions.Count > 1) return null;

                if (calculs.Count == 1)
                {
                    var (expression, resultat) = calculs[0];
                    return new Question(expression, Ecrire(resultat), null, false);
                }

                if (decompositions.Count == 1)
                {
                    var n = decompositions[0];
                    var avecCentaines = n >= 100 || Regex.IsMatch(message, "centaines", RegexOptions.IgnoreCase);
                    return new Question(null, n.ToString(CultureInfo.InvariantCulture), n, avecCentaines);
                }

                // Le message le plus récent ne pose rien, ne parle d'aucun nombre : la question d'avant reste ouverte
                // (« Ce n'est pas encore ça. C'est ton dernier essai. »). Dès qu'il parle d'un nombre ou pose une
                // question qu'on ne sait pas lire (« Maintenant, calcule 9 - 4. »), on ne remonte plus.
                if (message.Contains('?') || message.Any(char.IsDigit) || NombresEnLettres.Lire(message).Count > 0) return null;
            }

            return null;
        }

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

            // « 4 + 3 = 7 » : c'est ce qui suit le dernier « = » qui répond.
            var dit = reponse.Contains('=') ? reponse[(reponse.LastIndexOf('=') + 1)..] : reponse;
            if (Regex.IsMatch(dit, @"[-−–]\s*\d|\bmoins\b", RegexOptions.IgnoreCase)) return null;

            var valeurs = NombreEnChiffres().Matches(dit)
                .Select(m => decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
                .ToList();
            if (valeurs.Any(v => v is null)) return null;

            var toutes = valeurs.Select(v => v!.Value)
                .Concat(NombresEnLettres.Lire(dit).Select(l => (decimal)l.Valeur))
                .Distinct()
                .ToList();

            if (toutes.Count != 1) return null;

            var donne = Ecrire(toutes[0]);
            return new Verdict(expression + " = ?", attendu, donne, donne == attendu ? Jugement.Juste : Jugement.Faux);
        }

        // ───────────── La décomposition ─────────────

        private static Verdict? JugerDecomposition(int n, bool avecCentaines, string reponse)
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

            if (parties.Count == 0) return null;

            var attendues = avecCentaines
                ? new Dictionary<char, int> { ['c'] = n / 100, ['d'] = n / 10 % 10, ['u'] = n % 10 }
                : new Dictionary<char, int> { ['d'] = n / 10, ['u'] = n % 10 };

            // Des centaines dans une question qui n'en parle pas : on compte quand même leur valeur.
            var total = parties.Sum(p => p.Key switch { 'c' => p.Value * 100, 'd' => p.Value * 10, _ => p.Value });
            var question = $"combien de {(avecCentaines ? "centaines, de dizaines" : "dizaines")} et d'unités dans {n}";
            var attendu = Decrire(attendues);
            var donne = Decrire(parties);

            var conformes = parties.All(p => attendues.TryGetValue(p.Key, out var a) && a == p.Value);
            if (conformes)
            {
                var manque = attendues.Any(a => !parties.ContainsKey(a.Key) && a.Value != 0);
                return new Verdict(question, attendu, donne, manque ? Jugement.Incomplet : Jugement.Juste);
            }

            return new Verdict(question, attendu, donne, total == n ? Jugement.AutreDecomposition : Jugement.Faux);
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
