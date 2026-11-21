using System.Globalization;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LA SUITE DES NOMBRES, JUGÉE PAR LE CODE — Minamba, le 21/11/2026, signalement #53 (Adam, jeu CP, Maths/Nora).
    /// « Après 73, qu'est-ce que tu dis quand tu comptes : 71, 72, 73... ? » — l'élève répond « Sept. » à la voix,
    /// et Nora : « Oui, c'est bien 74 ! Après 73, on dit 74. » (2 fois sur 2), puis « vingt-trois » pour « Et après
    /// 74 ? » reçoit « Oui ! 23, c'est un bon nombre… ». Le rappel de <c>ReponseChiffree</c> lui donnait pourtant
    /// « « sept » = 7 » : il a validé quand même, en plaquant sa propre réponse. C'est la leçon de #29, #45 et #46 :
    /// seul un FAIT calculé au tour même tient (voir le haut de <see cref="CalculAuTableau"/>).
    ///
    /// Le nombre qui vient après (ou avant) un nombre donné est un fait que le code sait établir. On ne lit que des
    /// questions claires, dans une phrase qui finit par « ? » :
    ///   - « qu'est-ce qui vient (juste) après 73 ? », « après 73, qu'est-ce que tu dis ? », « Et après 74 ? »,
    ///     « le nombre qui suit 73 », « le précédent de 80 », « avant 80, on dit quoi ? » ;
    ///   - « quel nombre est entre 72 et 74 ? » ;
    ///   - une suite à trou : « 71, 72, 73… ? », « 10, 20, ?, 40 », « 71, 72, 73, … » au tableau avec « Qu'est-ce
    ///     qui vient ensuite ? ». Le pas est celui de la suite (de 2 en 2, de 10 en 10, en reculant).
    /// Pour « après / avant », le pas est 1, ou celui que la phrase annonce (« de 10 en 10 ») ou celui de la suite
    /// écrite dans le même message. Une question qui compte à rebours en mots, qui parle de dizaines, de nombres
    /// pairs, ou qui demande « combien » n'est pas lue : pas de verdict, le rappel ordinaire reprend sa place.
    ///
    /// Commun à tous les professeurs (portée générique) : le juge ne dépend d'aucune matière ni d'aucun prompt.
    /// </summary>
    public static partial class CalculAuTableau
    {
        /// <summary>
        /// Une question de suite : l'énoncé tel qu'on le redira au professeur, le nombre attendu, et les nombres que
        /// la question cite (une relance peut les reprendre, l'élève aussi : « après 73, c'est 74 »). <c>Pas</c> est le
        /// pas de la suite dans le sens de la question (+1 pour « après 73 », -1 pour « avant 80 », +10 pour « 10, 20, 30… »).
        /// </summary>
        private sealed record SuitePosee(string Enonce, int Attendu, HashSet<int> Cites, string Indice, int Pas);

        /// <summary>
        /// L'INDICE CALCULÉ — leçon de #52 (et #29, #46) : « aide-le sans donner la réponse » sans dire comment, le modèle
        /// donne la réponse ou invente. Au bout en bout du 21/11, sur « Sept. », Nora a répondu « Compte avec moi :
        /// soixante-treize... soixante-quatorze » : la réponse dans l'indice. On lui donne donc l'indice exact : les
        /// nombres à recompter, en s'arrêtant AVANT le trou.
        /// </summary>
        private static string IndiceDeSuite(string comment, int attendu) =>
            "Indice à donner : " + comment + " C'est LUI qui doit dire le nombre qui manque : ne dis et n'écris pas "
            + attendu.ToString(CultureInfo.InvariantCulture) + " toi-même à ce tour-ci, ni en chiffres ni en lettres.";

        private static string Recompte(IEnumerable<int> nombres) =>
            "« " + string.Join(", ", nombres.Where(x => x >= 0).Select(x => x.ToString(CultureInfo.InvariantCulture))) + "… »";

        // Signalement #60 : le cran est dit aussi quand il vaut 1. Sans lui, devant « 71, …, 80 » au tableau, Nora a
        // inventé « on compte par dizaines maintenant, 70, 80… » (ce qui mène à 90, pas à 81).
        private static string Cran(int pas) => pas > 0 ? $" (on avance de {pas} en {pas})" : $" (on recule de {-pas} en {-pas})";

        /// <summary>Au-delà, ce n'est plus la suite des nombres d'école.</summary>
        private const int SuiteMaximum = 9999;

        // « …après 73 », « …avant le nombre 80 », « …suit 73 », « …précède 80 », « …le suivant de 73 ». Le nombre ne
        // doit pas être suivi d'un nom : « après 3 essais », « avant 10 minutes » ne parlent pas de la suite.
        private const string ApresLeNombre = @"(?!\d|[,.]\d)(?=\s*(?:[,?:;!…]|\.(?!\d)|$)|\s+(?:qu|on|ça|ca|c['’]|il|tu|quel|quoi|vient|viennent|arrive|dis|dit|je|que|dans|si|quand|en|alors)(?!\p{L}))";

        [GeneratedRegex(@"(?<!\p{L})(?<sens>après|apres|avant)\s+(?:le\s+(?:nombre\s+)?)?(?<n>\d{1,4})" + ApresLeNombre, RegexOptions.IgnoreCase)]
        private static partial Regex ApresOuAvant();

        [GeneratedRegex(@"(?<!\p{L})(?<sens>suit|pr[ée]c[èe]de|suivant\s+(?:de|du)|pr[ée]c[ée]dent\s+(?:de|du))\s+(?:le\s+)?(?:nombre\s+)?(?<n>\d{1,4})" + ApresLeNombre,
            RegexOptions.IgnoreCase)]
        private static partial Regex SuitOuPrecede();

        [GeneratedRegex(@"(?<!\p{L})entre\s+(?:le\s+)?(?<a>\d{1,4})\s+et\s+(?:le\s+)?(?<b>\d{1,4})" + ApresLeNombre, RegexOptions.IgnoreCase)]
        private static partial Regex Entre();

        /// <summary>« 71, 72, 73… », « 10, 20, ?, 40 », « 2 ; 4 ; 6 ; ___ ». Trois éléments au moins.</summary>
        [GeneratedRegex(@"(?<![\d,])(?:\d{1,4}|\?|_+|…)(?:\s*[,;]\s*(?:\d{1,4}|\?|_+|…)){2,}(?:\s*…)?(?![\d,])")]
        private static partial Regex SuiteEcrite();

        /// <summary>
        /// LA QUESTION POSÉE SANS « ? » — signalement #60 (Adam, 21/11) : après « je sais pas », Nora a reposé la
        /// question en consigne, « Dis-moi le nombre juste après 80. », avec la suite réécrite au tableau. Sans « ? »,
        /// rien n'était lu, et le « 80 » de l'élève est parti sans verdict (indice inventé « par dizaines »). Une
        /// consigne qui COMMENCE par un verbe de demande suivi de « le nombre » / « ce qui vient » / « quel nombre »
        /// est une question ; « Après 73, on dit 74. » reste une phrase qui affirme.
        /// </summary>
        [GeneratedRegex(@"^\W*(?:\p{L}+\s*,\s*)?(?:(?:et|alors|maintenant)\s*,?\s+)?(?:dis|donne|écris|ecris|trouve|cherche)(?:-moi|-nous)?\s+(?:le\s+nombre|ce\s+qui\s+vient|quel\s+nombre)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex DemandeALImperatif();

        private static bool EstPosee(string phrase) => phrase.TrimEnd().EndsWith('?') || DemandeALImperatif().IsMatch(phrase);

        /// <summary>Les mots qui disent qu'on parle de la suite des nombres.</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:vient|viennent|arrive|arrivent|suit|suivent|suivant|ensuite|après|apres|dis|dit|dire|il\s+y\s+a|quel\s+nombre|qu['’]est-ce\s+qui|manque|continue|précède|precede|précédent|avant)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex MotDeSuite();

        /// <summary>
        /// Une autre question que « quel nombre vient après » : combien (« combien de nombres entre 70 et 80 ? »), les
        /// rangs (« quelle dizaine vient après 70 ? »), la parité, les durées, les calculs.
        /// </summary>
        [GeneratedRegex(@"(?<!\p{L})(?:combien|dizaines?|centaines?|unit[ée]s?|pairs?|impairs?|multiples?|plus\s+(?:grand|petit)|lettres?|jours?|mois|heures?|minutes?|ans)(?!\p{L})"
            + @"|\d\s*[+\-−–×x*:÷=]\s*\d", RegexOptions.IgnoreCase)]
        private static partial Regex PasUneSuite();

        /// <summary>
        /// On compte à l'envers : « après 10 » attend alors 9. Une suite ÉCRITE dit son sens toute seule
        /// (« 10, 9, 8… ? ») ; « après / avant N » dans un message qui parle de rebours ne se juge pas.
        /// </summary>
        [GeneratedRegex(@"(?<!\p{L})(?:rebours|l['’]envers|recul\p{L}*|descend\p{L}*|à\s+reculons)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex Rebours();

        /// <summary>« de 2 en 2 », « de dix en dix », « par 5 ».</summary>
        [GeneratedRegex(@"(?<!\p{L})de\s+(?<p>\d{1,4}|\p{L}+)\s+en\s+(?<q>\d{1,4}|\p{L}+)(?!\p{L})|(?<!\p{L})(?:compt\p{L}*|avance\p{L}*)\s+par\s+(?<p>\d{1,4}|\p{L}+)(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex PasAnnonce();

        /// <summary>
        /// Les questions de suite d'un message, une par nombre attendu (deux lectures de la même question — « après
        /// 73 » et « 71, 72, 73… » — n'en font qu'une). Null si le message en pose une qu'on lit mal (un pas annoncé
        /// incompréhensible) : on ne juge pas une autre question du même message.
        /// </summary>
        private static List<SuitePosee>? Suites(string message)
        {
            // Le texte des balises compte (une suite au tableau), pas les balises elles-mêmes. « ... » = « … ».
            var texte = Regex.Replace(message, @"\[/?[A-Z_]+(?::[^\]]*)?\]", "\n");
            texte = Regex.Replace(texte, @"\.{2,}", "…");
            texte = EnChiffres(texte);
            // « 2.5 » : virgule décimale, pour que le « . » ne coupe pas la phrase (#60 : une consigne finit par « . »).
            texte = Regex.Replace(texte, @"(?<=\d)\.(?=\d)", ",");

            var trouvees = new List<SuitePosee>();

            // Les phrases : un « ? », un « ! », un « . » ou une fin de ligne les termine ; « … » non.
            var phrases = Regex.Matches(texte, @"[^.!?\n]*[.!?\n]?").Select(m => m.Value).Where(p => p.Trim().Length > 0).ToList();
            var questionDeSuite = phrases.Any(p => EstPosee(p) && MotDeSuite().IsMatch(p) && !PasUneSuite().IsMatch(p));

            // Le pas que le message annonce en mots ; 0 s'il n'en annonce pas, null s'il est illisible.
            int? pasAnnonce = 0;
            foreach (Match m in PasAnnonce().Matches(texte))
            {
                var p = ValeurDuMot(m.Groups["p"].Value);
                var q = m.Groups["q"].Success ? ValeurDuMot(m.Groups["q"].Value) : p;
                if (p is null || p != q || p == 0 || (pasAnnonce != 0 && pasAnnonce != p)) return null;
                pasAnnonce = p;
            }

            // 1. Les suites écrites, avec un trou. Lues sur le texte entier : le « ? » d'un trou (« 10, 20, ?, 40 »)
            // n'est pas une fin de phrase.
            var suitesEcrites = new List<(int[] Nombres, int Pas)>();
            foreach (Match m in SuiteEcrite().Matches(texte))
            {
                var lue = LireSuite(m.Value);
                if (lue is null) continue;
                var (nombres, pas, trou, attendu, explicite) = lue.Value;

                suitesEcrites.Add((nombres, pas));

                if (trou < 0) continue;

                // La phrase autour de la suite, sans la suite.
                var debut = texte.LastIndexOfAny(['.', '!', '?', '\n'], Math.Max(m.Index - 1, 0)) + 1;
                if (m.Index == 0) debut = 0;
                var fin = texte.IndexOfAny(['.', '!', '?', '\n'], m.Index + m.Length);
                var avant = texte[debut..m.Index];
                var apres = fin < 0 ? texte[(m.Index + m.Length)..] : texte[(m.Index + m.Length)..(fin + 1)];
                var autour = avant + " " + apres;

                // Un « … » final n'est un trou que si on demande la suite : « 71, 72, 73… ? », ou « Qu'est-ce qui
                // vient ensuite ? » ailleurs dans le message. « Compte avec moi : 1, 2, 3… jusqu'à 10 ! » ne demande rien.
                // Et la question doit parler de la suite : « Compte à partir de 4 : 5, 6, 7… ? », relance de « 4 + 3 = ? »,
                // attend le total (7), pas le nombre suivant (8).
                var demande = explicite || (apres.TrimEnd().EndsWith('?') && MotDeSuite().IsMatch(autour)) || questionDeSuite;
                if (!demande || PasUneSuite().IsMatch(autour)) continue;

                if (pasAnnonce is > 0 && pasAnnonce != Math.Abs(pas)) return null;
                if (attendu < 0 || attendu > SuiteMaximum) continue;

                var indice = trou == nombres.Length
                    ? IndiceDeSuite($"fais-lui relire la suite avec toi, {Recompte(nombres.TakeLast(3))}{Cran(pas)}, et arrête-toi là.", attendu)
                    : IndiceDeSuite($"fais-lui trouver de combien on {(pas > 0 ? "avance" : "recule")} d'un nombre au suivant (ici de {Math.Abs(pas)}), puis s'en servir pour trouver le nombre du trou.", attendu);
                Ajouter(trouvees, new SuitePosee(Regex.Replace(m.Value.Trim(), @"\s+", " ") + " ?", attendu, nombres.ToHashSet(), indice, pas));
            }

            // 2. « après 73 », « avant 80 », « suit 73 », « entre 72 et 74 », dans une question.
            foreach (var phrase in phrases)
            {
                if (!EstPosee(phrase) || PasUneSuite().IsMatch(phrase)) continue;

                var aRebours = Rebours().IsMatch(texte);
                foreach (var m in ApresOuAvant().Matches(phrase).Concat(SuitOuPrecede().Matches(phrase)))
                {
                    if (aRebours) continue;

                    // « Et après 74 ? » se suffit ; sinon il faut un mot de la suite (« vient », « dis », « quel nombre »…).
                    var collee = Regex.IsMatch(phrase[(m.Index + m.Length)..], @"^\s*\?");
                    var sansLeNombre = phrase.Remove(m.Index, m.Length);
                    if (!collee && !MotDeSuite().IsMatch(sansLeNombre)) continue;

                    var n = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
                    var sens = m.Groups["sens"].Value.ToLowerInvariant();
                    var versLeHaut = sens.StartsWith("apr") || sens.StartsWith("sui");

                    // Le pas : annoncé en mots, sinon celui de la suite écrite qui contient ce nombre, sinon 1.
                    var pas = pasAnnonce is > 0 ? pasAnnonce.Value : 1;
                    if (pasAnnonce == 0)
                    {
                        var avecN = suitesEcrites.Where(s => s.Nombres.Contains(n)).Select(s => Math.Abs(s.Pas)).Distinct().ToList();
                        if (avecN.Count > 1) return null;
                        if (avecN.Count == 1) pas = avecN[0];
                    }

                    var attendu = versLeHaut ? n + pas : n - pas;
                    if (attendu < 0 || attendu > SuiteMaximum) continue;

                    var enonce = versLeHaut
                        ? $"quel nombre vient juste après {n}{(pas == 1 ? "" : $" (de {pas} en {pas})")}"
                        : $"quel nombre vient juste avant {n}{(pas == 1 ? "" : $" (de {pas} en {pas})")}";
                    var indice = versLeHaut
                        ? IndiceDeSuite($"fais-lui recompter avec toi {Recompte(new[] { n - 2 * pas, n - pas, n })}{Cran(pas)} et arrête-toi là.", attendu)
                        : IndiceDeSuite($"demande-lui de compter jusqu'à {n} en partant de {n - Math.Min(3, n / pas) * pas}{Cran(pas)}, et de te dire le nombre qu'il dit JUSTE AVANT {n}.", attendu);
                    Ajouter(trouvees, new SuitePosee(enonce, attendu, new HashSet<int> { n, pas }, indice, versLeHaut ? pas : -pas));
                }

                foreach (Match m in Entre().Matches(phrase))
                {
                    if (!MotDeSuite().IsMatch(phrase) && !Regex.IsMatch(phrase, @"quel(?:le)?\s+(?:nombre|est)", RegexOptions.IgnoreCase)) continue;

                    var a = int.Parse(m.Groups["a"].Value, CultureInfo.InvariantCulture);
                    var b = int.Parse(m.Groups["b"].Value, CultureInfo.InvariantCulture);
                    if (Math.Abs(b - a) != 2) continue;

                    var petit = Math.Min(a, b);
                    Ajouter(trouvees, new SuitePosee($"quel nombre est entre {a} et {b}", (a + b) / 2, new HashSet<int> { a, b },
                        IndiceDeSuite($"le nombre entre {a} et {b} vient juste après {petit} : fais-lui recompter {Recompte(new[] { petit - 1, petit })} et arrête-toi là.", (a + b) / 2), 1));
                }
            }

            return trouvees;
        }

        /// <summary>Deux lectures qui attendent le même nombre sont la même question ; on garde la plus parlante.</summary>
        private static void Ajouter(List<SuitePosee> trouvees, SuitePosee suite)
        {
            var meme = trouvees.FindIndex(s => s.Attendu == suite.Attendu);
            if (meme < 0)
            {
                trouvees.Add(suite);
                return;
            }

            trouvees[meme] = trouvees[meme] with { Cites = trouvees[meme].Cites.Union(suite.Cites).ToHashSet() };
        }

        /// <summary>
        /// Une suite écrite : ses nombres, son pas (négatif si elle descend), la place du trou (-1 sans trou), le
        /// nombre attendu dans le trou, et si le trou est explicite (« ? », « ___ ») plutôt qu'un « … » final. Null
        /// si ce n'est pas une suite régulière d'au moins trois nombres avec au plus un trou.
        /// </summary>
        private static (int[] Nombres, int Pas, int Trou, int Attendu, bool Explicite)? LireSuite(string ecrite)
        {
            // « 2,5 ; 3 » : une virgule collée entre deux chiffres est un décimal, pas une suite.
            if (Regex.IsMatch(ecrite, @"\d,\d")) return null;

            var elements = new List<string>();
            foreach (var brut in ecrite.Split([',', ';']))
            {
                var e = brut.Trim();
                // « 73… » : le nombre, puis le trou.
                var colle = Regex.Match(e, @"^(\d{1,4})\s*…$");
                if (colle.Success)
                {
                    elements.Add(colle.Groups[1].Value);
                    elements.Add("…");
                }
                else elements.Add(e);
            }

            var trous = elements.Select((e, i) => (e, i)).Where(x => !Regex.IsMatch(x.e, @"^\d+$")).ToList();
            var connus = elements.Select((e, i) => (e, i)).Where(x => Regex.IsMatch(x.e, @"^\d+$"))
                .Select(x => (Valeur: int.Parse(x.e, CultureInfo.InvariantCulture), Place: x.i)).ToList();

            if (connus.Count < 3 || trous.Count > 1) return null;

            // Un « … » au milieu veut dire « et ainsi de suite » (« 1, 2, …, 10 ») : pas un trou.
            if (trous.Count == 1 && trous[0].e == "…" && trous[0].i != elements.Count - 1) return null;

            var pas = (connus[1].Valeur - connus[0].Valeur) / (connus[1].Place - connus[0].Place);
            if (pas == 0) return null;
            var depart = connus[0].Valeur - connus[0].Place * pas;
            if (connus.Any(c => depart + c.Place * pas != c.Valeur)) return null;

            var nombres = connus.Select(c => c.Valeur).ToArray();
            if (trous.Count == 0) return (nombres, pas, -1, -1, false);

            var trou = trous[0].i;
            return (nombres, pas, trou, depart + trou * pas, trous[0].e != "…");
        }

        /// <summary>
        /// Les nombres que le professeur écrit en lettres (« après soixante-treize ? ») passent en chiffres pour la
        /// lecture de la question. « un » / « une » seuls restent des articles.
        /// </summary>
        private static string EnChiffres(string texte)
        {
            foreach (var lu in NombresEnLettres.Lire(VirgulesQuiSeparent(texte)).DistinctBy(l => l.Dit.ToLowerInvariant()))
            {
                if (lu.Valeur > SuiteMaximum) continue;
                var motif = @"(?<![\p{L}-])" + Regex.Escape(lu.Dit).Replace(@"\ ", @"\s+") + @"(?![\p{L}-])";
                texte = Regex.Replace(texte, motif, lu.Valeur.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
            }
            return texte;
        }

        /// <summary>
        /// UNE SUITE EN LETTRES — bout en bout du 21/11 (#53) : la relance de Nora « On recompte ensemble :
        /// soixante-douze, soixante-treize, soixante-quatorze... et après ? » était lue comme UN seul nombre (219) :
        /// <see cref="NombresEnLettres"/> ne ferme pas un nombre sur une virgule (comme le front, on n'y touche pas).
        /// Dans une suite, la virgule sépare toujours deux nombres : pour la lecture seulement, celle qui suit un mot
        /// devient « ; », qui ferme le nombre. Vaut aussi pour l'élève : « soixante-treize, soixante-quatorze » = 73
        /// puis 74 (73 est cité par la question, il reste 74), pas 147. « 2,5 » (chiffres) n'est pas touché.
        /// Même chose pour « soixante-quatorze… » : « … » collé n'est pas une ponctuation pour ce lecteur, et le mot
        /// n'était pas lu du tout ; on le décolle.
        /// </summary>
        private static string VirgulesQuiSeparent(string texte) =>
            Regex.Replace(Regex.Replace(texte, @"(?<=\p{L}),", ";"), @"(?<=\p{L})(?=…|\.{2,})", " ");

        private static Verdict? JugerSuite(SuitePosee s, string reponse)
        {
            // « soixante-treize soixante-quatorze » (micro, sans virgule) : 73 puis 74, pas 147. Dans le doute, pas de verdict.
            var enChiffres = EnChiffresSurs(VirgulesQuiSeparent(reponse));
            if (enChiffres is null) return null;

            var valeurs = Valeurs(enChiffres);
            if (valeurs is null || valeurs.Count == 0) return null;

            if (valeurs.Count > 1)
            {
                // L'élève recompte, ou continue de compter, en passant par le bon nombre : « soixante-treize
                // soixante-quatorze », « soixante-quatorze soixante-quinze » pour « après 73 ».
                if (CompteParLeNombreAttendu(s, enChiffres))
                {
                    var juste = s.Attendu.ToString(CultureInfo.InvariantCulture);
                    return new Verdict(s.Enonce, juste, juste, Jugement.Juste);
                }

                // « Après 73, c'est 74 » : le 73 vient de la question ; il reste la réponse.
                valeurs = valeurs.Where(v => !s.Cites.Contains((int)Math.Min(v, int.MaxValue)) || v != Math.Floor(v)).ToList();
                if (valeurs.Count != 1) return null;
            }

            var donne = valeurs[0];
            if (donne != Math.Floor(donne) || donne > SuiteMaximum) return null;

            var attendu = s.Attendu.ToString(CultureInfo.InvariantCulture);
            return (int)donne == s.Attendu
                ? new Verdict(s.Enonce, attendu, Ecrire(donne), Jugement.Juste)
                : new Verdict(s.Enonce, attendu, Ecrire(donne), Jugement.Faux) { Indice = s.Indice };
        }

        /// <summary>« je (ne) sais pas », « j'en sais rien », « je trouve pas », « aide-moi »…</summary>
        [GeneratedRegex(@"(?<!\p{L})(?:(?:je\s+(?:ne\s+)?|j['’]\s*)?sais\s+pas|chais\s+pas|j['’]en\s+sais\s+rien|je\s+(?:ne\s+)?trouve\s+pas|aucune\s+id[ée]e|je\s+(?:ne\s+)?comprends?\s+pas|aide[-\s]moi|je\s+(?:ne\s+)?m['’]en\s+souviens\s+(?:pas|plus))(?!\p{L})",
            RegexOptions.IgnoreCase)]
        private static partial Regex NeSaitPas();

        /// <summary>
        /// « JE SAIS PAS » PENDANT UNE SUITE — signalement #60 (Adam, 21/11, « après 80 ») : sans nombre dans la réponse,
        /// aucun verdict, donc aucun indice calculé, et Nora a inventé « ça avance d'une dizaine de plus » devant
        /// « 71, …, 80 » au tableau. Quand la question ouverte est une question de SUITE et que l'élève dit qu'il ne
        /// sait pas (sans nombre : un nombre est une réponse, elle passe par le juge), on donne au professeur le même
        /// indice calculé que pour une réponse fausse — sans verdict : ce n'est pas un essai raté. Null sinon (calcul,
        /// décomposition, aucune question : rien ne change).
        /// </summary>
        public static string? FaitSansReponse(IEnumerable<string?> messagesDuProfesseur, string? messageDeLEleve)
        {
            if (string.IsNullOrWhiteSpace(messageDeLEleve) || messageDeLEleve.Length > 80) return null;
            if (!NeSaitPas().IsMatch(messageDeLEleve)) return null;
            if (Regex.IsMatch(EnChiffres(VirgulesQuiSeparent(messageDeLEleve)), @"\d")) return null;
            if (DerniereQuestion(messagesDuProfesseur) is not { Suite: { } s }) return null;

            return $"[Vérifié par l'application : la question posée était « {s.Enonce} », et la bonne réponse est "
                + $"{s.Attendu.ToString(CultureInfo.InvariantCulture)}. L'élève dit qu'il ne sait pas : ce n'est pas une réponse "
                + "fausse et ça ne compte pas comme un essai. Ne lui donne pas la réponse. " + s.Indice + "]";
        }

        /// <summary>
        /// LA RÉPONSE NE FAIT QUE COMPTER — contrôle d'Omar sur 1e93c60 (#53) : une réponse faite UNIQUEMENT de nombres
        /// (pas de « et », pas de « ou », pas de phrase) qui avancent du pas de la suite et passent par le nombre
        /// attendu est juste : l'élève a recompté jusqu'au trou (« 72, 73, 74 », « soixante-treize soixante-quatorze »)
        /// ou continué la suite (« soixante-quatorze soixante-quinze »). Dans l'autre sens (« 75, 74 »), seulement
        /// s'il s'arrête SUR le nombre attendu. « 74 et 75 » ou « 74 ou 75 » hésitent : non.
        /// </summary>
        private static bool CompteParLeNombreAttendu(SuitePosee s, string enChiffres)
        {
            // Rien d'autre que des nombres entiers et de la ponctuation.
            if (Regex.IsMatch(enChiffres, @"\d[,.]\d") || Regex.Replace(enChiffres, @"[\d\s.,;:!?…]", "").Length > 0) return false;

            var nombres = Regex.Matches(enChiffres, @"\d+").Select(m => m.Value.Length > 5 ? -1 : int.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
            if (nombres.Count < 2 || nombres.Any(x => x < 0 || x > SuiteMaximum) || !nombres.Contains(s.Attendu)) return false;

            var ecart = nombres[1] - nombres[0];
            if (ecart == 0 || Math.Abs(ecart) != Math.Abs(s.Pas)) return false;
            if (nombres.Zip(nombres.Skip(1)).Any(p => p.Second - p.First != ecart)) return false;

            return ecart == s.Pas || nombres[^1] == s.Attendu;
        }

        // ───────────── Les nombres en lettres de la réponse, lus sans doute ─────────────

        private static readonly string[] UnitesEnMots =
        [
            "zero", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix", "onze", "douze",
            "treize", "quatorze", "quinze", "seize",
        ];

        private static readonly string[] DizainesEnMots = ["", "", "vingt", "trente", "quarante", "cinquante", "soixante"];

        /// <summary>Les mots que <see cref="NombresEnLettres"/> sait lire (même liste).</summary>
        private static readonly HashSet<string> MotsDeNombre =
        [
            .. UnitesEnMots, "zéro", "une", "vingt", "vingts", "trente", "quarante", "cinquante", "soixante", "cent", "cents", "mille", "et",
        ];

        /// <summary>
        /// Toutes les façons justes d'écrire 0 à <see cref="SuiteMaximum"/>, sous une forme simple, avec leur valeur :
        /// mots séparés par une espace, sans trait d'union ni « et », « vingts » → « vingt », « cents » → « cent »,
        /// « une » → « un ». « soixante treize » = 73, « quatre vingt dix sept » = 97, « cent quatre vingt » = 180.
        /// </summary>
        private static readonly Lazy<Dictionary<string, int>> Orthographes = new(() =>
            Enumerable.Range(0, SuiteMaximum + 1).ToDictionary(EnMots, n => n));

        private static string EnMots(int n)
        {
            if (n == 0) return "zero";
            var mots = new List<string>();
            var milliers = n / 1000;
            if (milliers == 1) mots.Add("mille");
            else if (milliers > 1) mots.Add(EnMots(milliers) + " mille");

            var centaines = n % 1000 / 100;
            if (centaines == 1) mots.Add("cent");
            else if (centaines > 1) mots.Add(UnitesEnMots[centaines] + " cent");

            if (n % 100 > 0) mots.Add(MoinsDeCent(n % 100));
            return string.Join(" ", mots);
        }

        private static string MoinsDeCent(int x) => x switch
        {
            <= 16 => UnitesEnMots[x],
            < 20 => "dix " + UnitesEnMots[x - 10],
            < 70 => DizainesEnMots[x / 10] + (x % 10 == 0 ? "" : " " + UnitesEnMots[x % 10]),
            < 80 => "soixante " + MoinsDeCent(x - 60),
            _ => "quatre vingt" + (x == 80 ? "" : " " + MoinsDeCent(x - 80)),
        };

        /// <summary>
        /// LES NOMBRES EN LETTRES DE LA RÉPONSE, EN CHIFFRES — contrôle d'Omar sur 1e93c60 (#53) : au micro,
        /// « soixante-treize soixante-quatorze » arrive sans virgule, et <see cref="NombresEnLettres"/> (copie du front,
        /// on n'y touche pas) le lit comme UN nombre : 60 + 13 + 60 + 14 = 147, donc FAUX contre 74.
        /// Pour le juge de suite seulement, chaque suite de mots-nombres est lue avec la table des orthographes justes
        /// (<see cref="Orthographes"/>) : un nombre bien écrit reste UN nombre (« soixante dix » = 70, « quatre vingt »
        /// = 80, « vingt deux » = 22) ; sinon elle est coupée en nombres bien écrits, le moins de morceaux possible
        /// (« soixante-treize | soixante-quatorze » → « 73; 74 »). Deux découpages aussi courts (« vingt quatre vingt » :
        /// 20 puis 80, ou 24 puis 20 ?) ou aucun : null, pas de verdict — mieux vaut ne pas juger que dire FAUX à un
        /// enfant qui a juste. La table lit aussi juste « cent quatre-vingts » (180), que le lecteur commun lit 124 :
        /// trouvé au balayage 1-998 du harnais, défaut ANCIEN du lecteur (front et back), signalé à part.
        /// « un » / « une » seuls restent des mots (des articles), comme pour le lecteur commun.
        /// </summary>
        private static string? EnChiffresSurs(string texte)
        {
            // Les mots et les blancs entre eux (indices pairs = mots), pour tout recoller à l'identique.
            var morceaux = Regex.Split(texte, @"(\s+)");
            var mots = new List<(int Place, string Debut, string Coeur, string Fin)>();
            for (var i = 0; i < morceaux.Length; i += 2)
            {
                if (morceaux[i].Length == 0) continue;
                var m = Regex.Match(morceaux[i], @"^([«""(]*)(.*?)([.,;:!?»"")…]*)$");
                mots.Add((i, m.Groups[1].Value, m.Groups[2].Value.ToLowerInvariant(), m.Groups[3].Value));
            }

            static string[] Parties(string coeur) => coeur.Split('-', StringSplitOptions.RemoveEmptyEntries);

            var serie = new List<int>();
            for (var k = 0; k <= mots.Count; k++)
            {
                var estNombre = k < mots.Count && Parties(mots[k].Coeur) is { Length: > 0 } parties && parties.All(MotsDeNombre.Contains)
                    && (mots[k].Coeur != "et" || (serie.Count > 0 && k + 1 < mots.Count && mots[k + 1].Coeur is "un" or "une" or "onze"));

                if (serie.Count > 0 && (!estNombre || mots[k].Debut.Length > 0))
                {
                    if (!Lire(serie)) return null;
                    serie.Clear();
                }

                if (!estNombre) continue;
                serie.Add(k);
                if (mots[k].Fin.Length > 0)
                {
                    if (!Lire(serie)) return null;
                    serie.Clear();
                }
            }

            return string.Concat(morceaux);

            // Remplace la série de mots (indices dans « mots ») par ses nombres ; false si on ne sait pas la lire sans doute.
            bool Lire(List<int> s)
            {
                int? Valeur(int de, int a)
                {
                    if (mots[s[de]].Coeur == "et" || mots[s[a - 1]].Coeur == "et") return null;
                    var simples = s.Skip(de).Take(a - de).SelectMany(k => Parties(mots[k].Coeur))
                        .Where(p => p != "et")
                        .Select(p => p switch { "une" => "un", "zéro" => "zero", "vingts" => "vingt", "cents" => "cent", _ => p });
                    return Orthographes.Value.TryGetValue(string.Join(" ", simples), out var v) ? v : null;
                }

                // Le moins de morceaux, et combien de découpages l'atteignent (2 = « plusieurs »).
                var n = s.Count;
                var moins = Enumerable.Repeat(int.MaxValue, n + 1).ToArray();
                var facons = new int[n + 1];
                var depuis = new int[n + 1];
                moins[0] = 0;
                facons[0] = 1;
                for (var a = 1; a <= n; a++)
                    for (var de = 0; de < a; de++)
                    {
                        if (moins[de] == int.MaxValue || Valeur(de, a) is null) continue;
                        if (moins[de] + 1 < moins[a]) { moins[a] = moins[de] + 1; facons[a] = facons[de]; depuis[a] = de; }
                        else if (moins[de] + 1 == moins[a]) facons[a] = Math.Min(2, facons[a] + facons[de]);
                    }

                if (moins[n] == int.MaxValue || facons[n] > 1) return false;

                for (var a = n; a > 0; a = depuis[a])
                {
                    var de = depuis[a];
                    var premier = mots[s[de]];
                    var dernier = mots[s[a - 1]];

                    // « un » ou « une » tout seul : un article, laissé tel quel (une coupe reste marquée).
                    if (a - de == 1 && premier.Coeur is "un" or "une")
                    {
                        if (a < n) morceaux[premier.Place] += ";";
                        continue;
                    }

                    morceaux[premier.Place] = premier.Debut + Valeur(de, a)!.Value.ToString(CultureInfo.InvariantCulture) + dernier.Fin + (a < n ? ";" : "");
                    for (var k = de + 1; k < a; k++)
                    {
                        morceaux[mots[s[k]].Place] = "";
                        morceaux[mots[s[k]].Place - 1] = "";
                    }
                }
                return true;
            }
        }
    }
}
