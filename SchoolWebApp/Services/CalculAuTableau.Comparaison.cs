using System.Globalization;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LA COMPARAISON DE DEUX NOMBRES, JUGÉE PAR LE CODE — Mins, le 24/11/2026, signalement #76 (Adam, CE1, « comparer et
    /// ranger des nombres ») : Nora a demandé « Pour tout de suite, une petite question : entre 913 et 913... non, entre 820
    /// et 802, lequel est le plus grand ? ». L'élève a répondu « 802 », et elle a écrit « Oui, exactement, 802 est plus grand
    /// que 820... attends, vérifie bien », puis fait le bilan « tu as très bien comparé les nombres à trois chiffres ». Le
    /// juge ne lisait que des calculs, des décompositions et des suites : aucun fait ne partait, et le modèle a validé seul
    /// (même famille que #70 et #71).
    ///
    /// Ce qui est lu, dans une phrase qui pose la question (« ? ») avec un mot interrogatif (lequel, quel, qui) :
    /// <list type="bullet">
    /// <item>« le plus grand » / « le plus petit » entre DEUX nombres entiers de la phrase (« entre 820 et 802 »,
    /// « 820 ou 802 »), ou, si la phrase n'en cite aucun, les deux seuls nombres écrits au tableau du message ;</item>
    /// <item>« lequel a le plus (le moins) de centaines / dizaines / unités » : jugé seulement quand le chiffre du rang et
    /// le nombre total de ce rang désignent le même nombre (820 / 802 : 2 et 0 dizaines, 82 et 80 dizaines).</item>
    /// </list>
    /// Une phrase qui se reprend (« entre 913 et 913... non, entre 820 et 802 ») se lit après la reprise. Deux nombres
    /// égaux, un nombre à virgule, un troisième nombre, une question fermée (« est-ce que 820 est plus grand que 802 ? »),
    /// « plus grand que 500 » : pas de verdict.
    ///
    /// La réponse : l'élève nomme UN des deux nombres (« 802 », « c'est 820 », « huit cent vingt », « 820 est le plus
    /// grand »), ou les deux avec le sens de la comparaison (« 820 est plus grand que 802 », « 820 > 802 »). Tout autre
    /// nombre, une négation, « pareil » : pas de verdict. Une réponse fausse reçoit un indice calculé (le rang où les deux
    /// nombres se départagent), sans jamais dire lequel est le plus grand.
    ///
    /// Portée : générique (tous les professeurs, tous les niveaux), comme le reste du juge.
    /// </summary>
    public static partial class CalculAuTableau
    {
        /// <summary>
        /// Une comparaison posée : les deux nombres dans l'ordre de la question, le nombre attendu, l'énoncé redit au
        /// professeur, le sens (« plus grand » : true) et le rang demandé (« le plus de dizaines » : 'd'), '\0' sinon.
        /// </summary>
        private sealed record ComparaisonPosee(int A, int B, int Attendu, string Enonce, bool PlusGrand, char Rang);

        /// <summary>« le plus grand », « plus petit », « le moins grand ».</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:le\s+)?(?<s>plus|moins)\s+(?<t>grand|petit)(?!\p{L})(?!\s+(?:que|qu['’]))", RegexOptions.IgnoreCase)]
        private static partial Regex SensDeLaComparaison();

        /// <summary>« lequel a le plus de dizaines », « qui a le moins de centaines ».</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:le\s+)?(?<s>plus|moins)\s+(?:de\s+|d['’]\s*)(?<u>milliers|centaines|dizaines|unit[ée]s)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex SensDUnRang();

        /// <summary>« plus grand que 500 », « plus petit qu'un » : une autre question (un seuil), pas une comparaison des deux.</summary>
        [GeneratedRegex(@"(?<!\p{L})plus\s+(?:grand|petit)\s+qu", RegexOptions.IgnoreCase)]
        private static partial Regex PlusQue();

        [GeneratedRegex(@"(?<!\p{L})(?:lequel|laquelle|lesquels|quel|quelle|qui)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex MotInterrogatif();

        /// <summary>« entre 913 et 913... non, entre 820 et 802 », « … attends, vérifie bien » : le professeur se reprend, la question vient après.</summary>
        [GeneratedRegex(@"…\s*,?\s*(?:non|pardon|oups|euh|enfin|plutôt|plutot|attends|attendez|je\s+veux\s+dire|je\s+voulais\s+dire)(?!\p{L})\s*,?", RegexOptions.IgnoreCase)]
        private static partial Regex Reprise();

        [GeneratedRegex(@"(?<![\d,.])\d{1,4}(?![\d]|[,.]\d)")]
        private static partial Regex EntierCompare();

        [GeneratedRegex(@"\d[,.]\d|\d{5,}")]
        private static partial Regex NombreHorsComparaison();

        /// <summary>
        /// Contrôle d'Omar sur le paquet 1 (24/11/2026) : un entier comparé est NU, suivi d'un séparateur (« ou », « et », une
        /// virgule, « ? »). Sinon c'est une mesure ou un nombre écrit autrement : « 1 km ou 900 m », « 3 m ou 250 cm »,
        /// « 1 h ou 45 min », « 3 € ou 250 centimes », « 7 dizaines ou 65 unités », « 5² ou 2⁵ », « 10 ou 5 + 6 » — le juge
        /// comparait 1 et 900 et jugeait FAUX la bonne réponse « 1 km ».
        /// </summary>
        [GeneratedRegex(@"\G[ \t]*(?:(?:ou|et)(?!\p{L})|[,?]|$)", RegexOptions.IgnoreCase)]
        private static partial Regex SeparateurApresUnEntier();

        /// <summary>
        /// Contrôle d'Omar : « le plus grand » ne compare les deux nombres que s'il est suivi de rien (« ? », « , », « : »),
        /// de « des deux », de « nombre : » ou de « entre ». « Le plus grand nombre qu'on peut écrire avec 3 et 7 », « le plus
        /// grand multiple de 5 inférieur à 23 », « le plus petit nombre de boîtes de 6 », « le plus grand nombre impair entre
        /// 10 et 20 » posent une autre question : le juge jugeait JUSTE une réponse fausse (7, 23, 6, 20).
        /// </summary>
        [GeneratedRegex(@"\G\s*(?:(?:des\s+deux(?:\s+nombres)?|de\s+ces\s+deux\s+nombres|nombre)\s*(?:[?,:]|$)|[?,:]|$|entre(?!\p{L}))", RegexOptions.IgnoreCase)]
        private static partial Regex FinDuSens();

        /// <summary>Contrôle d'Omar : une question qui porte sur autre chose que les deux nombres eux-mêmes.</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:multiples?|diviseurs?|communs?|communes?|chiffres?|pairs?|paires?|impairs?|impaires?|inf[ée]rieure?s?|sup[ée]rieure?s?|peux|peut|pourrais|possibles?|nombres?\s+(?:de|d['’]))(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex PorteSurAutreChose();

        /// <summary>Tous les entiers de la phrase sont nus : ni signe ou barre devant, ni zéro de tête, un séparateur après.</summary>
        private static bool EntiersNus(string p) => EntierCompare().Matches(p).All(m =>
            !(m.Length > 1 && m.Value[0] == '0')
            && (m.Index == 0 || !"-−–/^".Contains(p[m.Index - 1]))
            && SeparateurApresUnEntier().IsMatch(p, m.Index + m.Length));

        /// <summary>
        /// Les comparaisons posées dans un message, une par nombre attendu. Une phrase de comparaison qu'on ne lit pas (pas
        /// deux nombres, deux sens, un rang ambigu) est laissée de côté, comme avant ce juge : « Pas encore. Commence par le plus
        /// grand nombre : lequel est le plus grand ? » reste une relance du calcul posé plus haut. Null si le message pose deux
        /// comparaisons différentes (on ne choisit pas), ou une comparaison qui ne porte pas sur deux entiers nus (contrôle
        /// d'Omar : mesures, rangs, puissances, « le plus grand multiple de… »).
        /// </summary>
        private static List<ComparaisonPosee>? Comparaisons(string message)
        {
            // Les deux nombres écrits seuls au tableau (« [ARDOISE]820    802[/ARDOISE] ») : pour « Lequel est le plus grand ? ».
            var ardoises = Ardoise().Matches(message);
            List<int>? auTableau = null;
            // Contrôle d'Omar : des entiers nus seulement (ni « 45° », ni « 5² », ni « 3 € », ni « 1 000 »).
            if (ardoises.Count == 1 && Regex.IsMatch(ardoises[0].Groups["c"].Value, @"^[\s\d,;]+$") && !Regex.IsMatch(ardoises[0].Groups["c"].Value, @"\d[,.]\d"))
            {
                var nus = EntierCompare().Matches(ardoises[0].Groups["c"].Value);
                var lus = nus.Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
                if (lus.Count == 2 && lus[0] != lus[1] && nus.All(m => m.Length == 1 || m.Value[0] != '0')) auTableau = lus;
            }

            var texte = Regex.Replace(Ardoise().Replace(message, "\n"), @"\[[^\]]*\]", " ");
            texte = Regex.Replace(texte, @"\.{2,}", "…");

            var trouvees = new List<ComparaisonPosee>();
            foreach (Match phrase in Regex.Matches(texte, @"[^.!?\n]*\?"))
            {
                // La reprise (« entre 913 et 913... non, entre 820 et 802 ») : seule la fin de la phrase compte.
                var reprises = Reprise().Matches(phrase.Value);
                var p = reprises.Count > 0 ? phrase.Value[(reprises[^1].Index + reprises[^1].Length)..] : phrase.Value;

                var sens = SensDeLaComparaison().Matches(p);
                var sensDuRang = SensDUnRang().Matches(p);
                if (sens.Count + sensDuRang.Count == 0) continue;

                // « Est-ce que 820 est plus grand que 802 ? » (question fermée), « lequel est plus grand que 500 ? » (un seuil).
                if (!MotInterrogatif().IsMatch(p) || PlusQue().IsMatch(p)) continue;

                // Contrôle d'Omar (paquet 1) : une comparaison qui porte sur autre chose que deux entiers nus (une mesure, un
                // rang, une puissance, « le plus grand multiple… », « le plus petit nombre de boîtes… ») ne se juge pas, et le
                // message non plus : on ne juge pas à sa place le calcul qu'elle contient (« 10 ou 5 + 6 »). Dans le doute, rien.
                if (PorteSurAutreChose().IsMatch(p)
                    || sens.Concat(sensDuRang).Any(m => !FinDuSens().IsMatch(p, m.Index + m.Length))
                    || !EntiersNus(p)) return null;

                // Deux sens dans la même phrase (« le plus grand ou le plus petit ? ») : on ne choisit pas.
                var sensLus = sens.Select(m => (Grand: m.Groups["s"].Value.ToLowerInvariant() == "plus" == (m.Groups["t"].Value.ToLowerInvariant() == "grand"), Rang: '\0'))
                    .Concat(sensDuRang.Select(m => (Grand: m.Groups["s"].Value.ToLowerInvariant() == "plus", Rang: char.ToLowerInvariant(m.Groups["u"].Value[0]))))
                    .Distinct().ToList();
                if (sensLus.Count != 1) continue;
                var (plusGrand, rang) = sensLus[0];

                if (NombreHorsComparaison().IsMatch(p)) continue;
                var nombres = EntierCompare().Matches(p).Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture)).Distinct().ToList();
                if (nombres.Count == 0 && reprises.Count == 0 && auTableau is not null) nombres = auTableau;
                if (nombres.Count != 2) continue;

                var (a, b) = (nombres[0], nombres[1]);
                int? attendu = rang == '\0'
                    ? (plusGrand ? Math.Max(a, b) : Math.Min(a, b))
                    : ParLeRang(a, b, rang, plusGrand);
                if (attendu is null) continue;

                var enonce = rang == '\0'
                    ? $"entre {a} et {b}, lequel est le plus {(plusGrand ? "grand" : "petit")}"
                    : $"{a} ou {b}, lequel a le {(plusGrand ? "plus" : "moins")} de {NomDuRang(rang)}";
                var comparaison = new ComparaisonPosee(a, b, attendu.Value, enonce, plusGrand, rang);

                if (trouvees.Any(t => t.Attendu != comparaison.Attendu || !new[] { t.A, t.B }.ToHashSet().SetEquals([a, b]))) return null;
                if (trouvees.Count == 0) trouvees.Add(comparaison);
            }

            return trouvees;
        }

        /// <summary>
        /// « Lequel a le plus de dizaines ? » : le chiffre du rang (2 et 0 pour 820 et 802) et le nombre total de ce rang (82
        /// et 80) doivent désigner le même nombre ; sinon (190 et 280 : 9 et 8 dizaines, mais 19 et 28) on ne juge pas.
        /// </summary>
        private static int? ParLeRang(int a, int b, char rang, bool plus)
        {
            var poids = rang switch { 'm' => 1000, 'c' => 100, 'd' => 10, _ => 1 };
            var (chiffreA, chiffreB) = (a / poids % 10, b / poids % 10);
            var (totalA, totalB) = (a / poids, b / poids);
            if (chiffreA == chiffreB || totalA == totalB) return null;

            var parLeChiffre = (chiffreA > chiffreB) == plus ? a : b;
            var parLeTotal = (totalA > totalB) == plus ? a : b;
            return parLeChiffre == parLeTotal ? parLeChiffre : null;
        }

        private static string NomDuRang(char rang) => rang switch
        {
            'm' => "milliers",
            'c' => "centaines",
            'd' => "dizaines",
            _ => "unités",
        };

        // « 820 est plus grand que 802 », « 820 > 802 », « 802 est plus petit que 820 ».
        [GeneratedRegex(@"(?<x>\d{1,4})\s*(?:(?:est|c['’]est)\s+)?(?:(?<rel>plus\s+grand|plus\s+petit)\s+qu(?:e|['’])|(?<rel>[<>]))\s*(?<y>\d{1,4})", RegexOptions.IgnoreCase)]
        private static partial Regex RelationDonnee();

        // « 802 est plus grand », « 802, le plus petit » : le sens dit par l'élève avec un seul nombre.
        [GeneratedRegex(@"(?<!\p{L})(?<rel>plus\s+grand|plus\s+petit)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex SensDit();

        [GeneratedRegex(@"(?<!\p{L})(?:pas|non|ni|pareils?|pareilles?|[ée]gal\p{L}*|m[êe]mes?|les\s+deux)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex PasUnChoix();

        private static Verdict? JugerComparaison(ComparaisonPosee c, string reponse)
        {
            var valeurs = Valeurs(reponse);
            if (valeurs is null || valeurs.Count == 0) return null;
            if (valeurs.Any(v => v != c.A && v != c.B)) return null;
            if (PasUnChoix().IsMatch(reponse)) return null;

            int choisi;
            if (valeurs.Count == 1)
            {
                choisi = (int)valeurs[0];

                // « 802 est plus petit » à « lequel est le plus grand ? » : il ne nomme pas son choix.
                if (c.Rang == '\0' && SensDit().Matches(reponse).Any(m => m.Value.ToLowerInvariant().Contains("grand") != c.PlusGrand)) return null;
                if (c.Rang != '\0' && SensDit().IsMatch(reponse)) return null;
            }
            else
            {
                // Les deux nombres : seulement avec le sens de la comparaison (« 820 est plus grand que 802 », « 820 > 802 »).
                if (c.Rang != '\0') return null;
                var relations = RelationDonnee().Matches(reponse);
                if (relations.Count != 1) return null;
                var r = relations[0];
                var x = int.Parse(r.Groups["x"].Value, CultureInfo.InvariantCulture);
                var y = int.Parse(r.Groups["y"].Value, CultureInfo.InvariantCulture);
                if (!new[] { x, y }.ToHashSet().SetEquals([c.A, c.B])) return null;

                var rel = r.Groups["rel"].Value.ToLowerInvariant();
                var xPlusGrand = rel == ">" || rel.Contains("grand");
                var ditPlusGrand = xPlusGrand ? x : y;
                choisi = c.PlusGrand ? ditPlusGrand : (ditPlusGrand == x ? y : x);
            }

            var attendu = c.Attendu.ToString(CultureInfo.InvariantCulture);
            var donne = choisi.ToString(CultureInfo.InvariantCulture);
            var verdict = new Verdict(c.Enonce, attendu, donne, choisi == c.Attendu ? Jugement.Juste : Jugement.Faux);
            return verdict.Jugement == Jugement.Faux ? verdict with { Indice = IndiceDeComparaison(c) } : verdict;
        }

        /// <summary>
        /// L'indice d'une comparaison ratée (signalement #76, attendu d'Adam : « un indice, comparer les dizaines ») : le rang
        /// où les deux nombres se départagent, en partant de la gauche. Jamais lequel est le plus grand, ni la valeur des
        /// chiffres à comparer : c'est l'élève qui les lit. Sans parenthèse (le professeur les recopiait, tâche 1320).
        /// </summary>
        private static string IndiceDeComparaison(ComparaisonPosee c)
        {
            const string NePasDire = " Ne lui dis pas lequel des deux nombres est le bon, ni les chiffres à comparer : c'est lui qui doit les lire.";
            var (a, b) = (c.A.ToString(CultureInfo.InvariantCulture), c.B.ToString(CultureInfo.InvariantCulture));
            var gagne = c.PlusGrand ? "celui qui en a le plus est le plus grand" : "celui qui en a le moins est le plus petit";

            if (c.Rang != '\0')
            {
                return $"Son erreur : il n'a pas lu le bon chiffre. Fais-lui retrouver le chiffre des {NomDuRang(c.Rang)} de {a}, puis celui de {b} "
                    + "(de gauche à droite, on lit les centaines, les dizaines, puis les unités), et les comparer." + NePasDire;
            }

            if (a.Length != b.Length)
            {
                return $"Son erreur : il n'a pas regardé la taille des nombres. Fais-lui compter les chiffres de {a} et de {b} : un nombre qui a "
                    + $"plus de chiffres est plus grand qu'un nombre qui en a moins.{NePasDire}";
            }

            var noms = a.Length switch
            {
                4 => new[] { 'm', 'c', 'd', 'u' },
                3 => new[] { 'c', 'd', 'u' },
                2 => new[] { 'd', 'u' },
                _ => new[] { 'u' },
            };
            var i = 0;
            while (i < a.Length && a[i] == b[i]) i++;
            var rang = NomDuRang(noms[i]);

            if (i == 0)
            {
                return $"Son erreur : il n'a pas comparé les nombres en commençant par la gauche. Fais-lui regarder les {rang} de {a} et "
                    + $"de {b}, le premier chiffre à gauche : {gagne}.{NePasDire}";
            }

            var memes = string.Join(" et les ", noms.Take(i).Select(NomDuRang));
            return $"Son erreur : il n'a pas comparé les nombres rang par rang. Fais-lui comparer {a} et {b} en partant de la gauche : ils "
                + $"ont les mêmes {memes}, donc c'est aux {rang} qu'ils se départagent ; demande-lui combien de {rang} a chaque nombre : "
                + $"{gagne}.{NePasDire}";
        }

        /// <summary>
        /// Les nombres qu'une relance d'une comparaison peut citer sans parler d'autre chose : les deux nombres, leurs chiffres
        /// (« 347 a combien de centaines, et 582 en a combien ? », relance de la séance d'Adam), 1, 10 et 100.
        /// </summary>
        private static HashSet<decimal> NombresDUneRelanceDeComparaison(ComparaisonPosee c)
        {
            var permis = new HashSet<decimal> { c.A, c.B, 1, 10, 100 };
            foreach (var n in new[] { c.A, c.B })
            {
                foreach (var poids in new[] { 1, 10, 100, 1000 })
                {
                    if (n >= poids) permis.Add(n / poids % 10);
                }
            }
            return permis;
        }
    }
}
