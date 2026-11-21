using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// LIRE LES NOMBRES AFFICHÉS AU TABLEAU — Camara, le 12/11/2026 : « tu peux tout construire sur la même base que
    /// l'exercice des syllabes et des mots ». Du CP à la 3e, en maths : le CODE choisit un nombre de la classe de l'enfant
    /// (et de la classe d'avant, pour réviser), l'affiche au tableau par une balise [LECTURE_IMAGE:nombre]cible
    /// [/LECTURE_IMAGE], écrit la consigne, et JUGE ce que l'enfant a lu. Le professeur ne dit jamais le nombre à lire.
    ///
    /// LE JUGEMENT SE FAIT SUR LE TEXTE, PAS SUR LE SON : le micro écrit « soixante-treize » ou « 73 », « trois virgule
    /// vingt-cinq », « trois quarts », « dix puissance moins trois », « racine carrée de deux »… Chaque lecture est ramenée
    /// à une écriture canonique (« 73 », « 3,25 », « 3/4 », « 10^-3 », « rac2 ») et comparée à la cible. Rien ne passe
    /// par le relais du micro ni par le juge du son : le français du primaire n'est pas touché.
    ///
    /// LE CATALOGUE EST CELUI DU PDF « Mimia - Nombres a illustrer (CP-Terminale) » : la cible donne aussi le nom de
    /// l'image (`nombresALire.js`, côté écran). Le lycée n'y est pas : ce n'est plus un exercice de lecture.
    /// </summary>
    public static partial class LectureDesNombres
    {
        public const string Genre = "nombre";

        public sealed record Element(string Cible, int Rang);

        /// <summary>Ce que le code montre à ce tour : l'élément, s'il est redemandé ou nouveau, et s'il vient d'abandonner.</summary>
        /// <remarks><paramref name="HorsCatalogue"/> : le nombre qu'il a demandé et qu'on ne peut pas montrer en image (pas au
        /// catalogue de sa classe ni d'en dessous) ; le tableau garde alors son nombre (signalement #51).</remarks>
        /// <remarks><paramref name="SansEssai"/> : il a posé une question au lieu de lire ; il n'a pas encore essayé, la
        /// consigne ne dit donc pas « essaie encore » (contrôle d'Omar, #50/#51).</remarks>
        /// <remarks><paramref name="Avant"/> : pour un nombre demandé, celui que le tableau montrait jusque-là (null s'il n'y en
        /// avait pas). Le nombre demandé le REMPLACE : le professeur ne doit plus en parler (retest d'Adam, #51, 21/11/2026).</remarks>
        public sealed record Tour(Element Element, bool Redite, bool Premier, bool Abandon, bool Demande = false, string? HorsCatalogue = null,
            bool SansEssai = false, Element? Avant = null);

        /// <summary>Le verdict du message de l'enfant : ce qu'il devait lire, s'il est juste, et ce qu'il a dit.</summary>
        public sealed record Verdict(Element Montre, bool Juste, string Dit);

        // ------------------------------------------------------------------ le catalogue (PDF du 12/11/2026)

        private static readonly (int Rang, string[] Cibles)[] Catalogue =
        [
            (1, Enumerable.Range(0, 101).Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray()),
            (2, ["101", "108", "110", "115", "120", "170", "180", "190", "199", "200", "208", "250", "271", "300", "345", "391", "400",
                 "470", "491", "500", "512", "600", "666", "700", "709", "780", "800", "871", "880", "900", "960", "999", "1000"]),
            (3, ["1001", "1010", "1100", "1200", "1500", "1999", "2000", "2008", "2026", "2470", "3000", "3333", "4091", "4500", "5000",
                 "5780", "6300", "7071", "7999", "8000", "8180", "9000", "9090", "9999", "10000"]),
            (4, ["10500", "12345", "20000", "45080", "70071", "99999", "100000", "101010", "250000", "480300", "600006", "999999",
                 "1000000", "0,5", "0,25", "1,2", "2,5", "3,75", "7,08", "10,05", "12,4", "1/2", "1/3", "1/4", "3/4", "2/3", "5/10"]),
            (5, ["1200000", "3450000", "10000000", "25600080", "100000000", "999999999", "1000000000", "2500000000", "0,001",
                 "0,125", "3,141", "12,007", "45,6", "100,01", "7/10", "25/100", "3/5", "5/8", "7/4", "3/1000"]),
            (6, ["12000000000", "304050607", "0,75", "0,09", "104,05", "3,5", "2026,26", "1/5", "2/7", "11/6", "9/4", "13/100"]),
            (7, ["-1", "-5", "-12", "+4", "-100", "-0,5", "-3,7", "-2026", "5/3", "-2/5", "-1/2", "7/9"]),
            (8, ["10^2", "10^3", "10^6", "10^-1", "10^-3", "2^3", "3^2", "5^2", "(-2)^2", "-2^2", "2,5x10^4", "3x10^-2", "1,5x10^8",
                 "6,02x10^23"]),
            (9, ["rac9", "rac16", "rac25", "rac2", "rac50", "3rac2", "-rac3", "10^-6"]),
        ];

        public static IReadOnlyList<Element> Tous { get; } =
            Catalogue.SelectMany(c => c.Cibles.Select(cible => new Element(cible, c.Rang))).ToList();

        private static readonly Dictionary<string, int> Rangs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CP"] = 1, ["CE1"] = 2, ["CE2"] = 3, ["CM1"] = 4, ["CM2"] = 5,
            ["SIXIEME"] = 6, ["6E"] = 6, ["CINQUIEME"] = 7, ["5E"] = 7, ["QUATRIEME"] = 8, ["4E"] = 8,
            ["TROISIEME"] = 9, ["TROISIEME_PREPA"] = 9, ["3E"] = 9,
        };

        /// <summary>Le rang de la classe (CP = 1 … 3e = 9) ; 0 hors de l'exercice (lycée, inconnu).</summary>
        public static int Rang(string? niveau) => niveau is not null && Rangs.TryGetValue(niveau.Trim(), out var r) ? r : 0;

        /// <summary>L'exercice existe pour ce cours : les maths, du CP à la 3e.</summary>
        public static bool Concerne(string? agentSlug, string? niveau) =>
            agentSlug is not null && agentSlug.Contains("math", StringComparison.OrdinalIgnoreCase) && Rang(niveau) > 0;

        /// <summary>Ce qu'il peut avoir à lire : sa classe, et celle d'avant pour réviser.</summary>
        public static IReadOnlyList<Element> Possibles(string? niveau)
        {
            var rang = Rang(niveau);
            return Tous.Where(e => e.Rang == rang || e.Rang == rang - 1).ToList();
        }

        // ------------------------------------------------------------------ la balise

        [GeneratedRegex(@"\[LECTURE_IMAGE:nombre\]([^\[\]]+)\[/LECTURE_IMAGE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Marqueur();

        public static string Balise(Element e) => $"[LECTURE_IMAGE:{Genre}]{e.Cible}[/LECTURE_IMAGE]";

        /// <summary>Le dernier nombre montré dans ce message, ou null.</summary>
        public static Element? Affiche(string? contenu)
        {
            if (string.IsNullOrEmpty(contenu)) return null;
            var trouve = Marqueur().Matches(contenu).LastOrDefault();
            if (trouve is null) return null;
            var cible = trouve.Groups[1].Value.Trim();
            return Tous.FirstOrDefault(e => e.Cible == cible) ?? new Element(cible, 0);
        }

        // ------------------------------------------------------------------ entrer, rester, sortir

        // « Je veux lire des nombres », « on lit des chiffres », « la lecture des nombres ».
        // « On RElit des nombres », « je veux RElire des nombres » (retest d'Adam, #51, 21/11/2026) : après « Quitter », le
        // professeur propose « Tu veux relire des nombres ? » (FaitLectureTerminee) ; « relit » n'était pas reconnu (le \b
        // devant « lit » ne passe pas après « re »), le code n'ouvrait pas l'exercice et le tableau restait vide.
        [GeneratedRegex(@"(?<!\p{L})(?:re)?(?:lire|lis|lisons|lisez|lit|lecture)(?!\p{L})[^.?!\n]{0,30}\b(?:nombres?|chiffres?)\b|\b(?:nombres?|chiffres?)\b[^.?!\n]{0,20}\b(?:à|a)\s+(?:re)?lire\b", RegexOptions.IgnoreCase)]
        private static partial Regex DemandeDeLecture();

        // La proposition du professeur : « Tu veux qu'on lise des nombres ? », « Tu veux relire des nombres ? », et dans
        // l'autre sens « On repart avec un nouveau nombre à lire ? » (vu au bout en bout du retest d'Adam, #51).
        // RESSERRÉE (contrôle d'Omar sur c807c90, 21/11/2026) : « (re)lire … nombres/chiffres … ? » ouvrait la lecture sur un
        // « oui » pendant un AUTRE exercice — « Tu veux que je relise le nombre ? », « Tu veux que je te relise les
        // chiffres ? », « Tu veux relire ton calcul, les chiffres sont bons ? », « Prends le temps de lire les chiffres du
        // calcul. Tu es prêt ? », « Tu peux relire l'énoncé avec les nombres ? ». Ne compte plus que :
        //  1. (tu veux / on / qu'on / envie de / ça te dit de) (re)lire DES / D'AUTRES / QUELQUES / UN NOUVEAU nombre(s) ;
        //     le sujet est collé au verbe, donc « que je (te) relise » (le professeur relit) ne compte pas ;
        //  2. « un nouveau nombre à lire ? », « d'autres nombres à lire ? » ;
        //  3. (on continue / on repart sur / on refait / tu veux faire) la/une lecture de(s) nombre(s) ? (phrasés du modèle vus
        //     en bout en bout : « On repart sur la lecture des nombres ? », « On refait une lecture de nombre ? ») ;
        // jamais des chiffres « du calcul / de ton calcul / de l'énoncé… » (PasDUnCalcul), et la question doit être dans la
        // MÊME phrase (plus de « … lecture des nombres ! Combien font 5 + 3 ? »).
        // FORME 3 AU PASSÉ (contrôle d'Omar sur f4f26ab, 21/11/2026) : « On a fait la lecture des nombres tout à l'heure,
        // tu te souviens ? », « Tu as aimé faire la lecture des nombres ? », « Tu as bien fait la lecture des nombres hier ? »
        // ouvraient la lecture sur un « oui ». La forme 3 exige maintenant, comme la forme 1, un sujet qui PROPOSE collé au
        // verbe (on / qu'on / tu veux / ça te dit de / envie de…) : « on a fait », « tu as (bien) fait », « tu as aimé faire »
        // (auxiliaire a / as / ai / avons / avez entre le sujet et le verbe) ne passent donc plus ; « eu / avais envie de »
        // (passé) non plus.
        private const string Sujet = @"(?<!\p{L})(?:tu\s+veux(?:\s+bien)?|veux-tu|tu\s+voudrais|voudrais-tu|qu['’]on|on(?:\s+(?:va|peut|pourrait))?"
            + @"|(?<!(?<!\p{L})(?:eu|avais|avait|aviez|avions)\s+)(?:tu\s+as\s+|t['’]as\s+)?envie\s+de|(?:ça|ca)\s+te\s+dit\s+de|pr[êe]te?\s+(?:à|a))";

        private const string PasDUnCalcul = @"(?!\s+(?:du|des|de\s+|d['’]|dans\s+)\s*(?:(?:ton|ta|tes|le|la|les|l['’]|ce|cette|cet|mon|ma|son|sa)\s*)?"
            + @"(?:calculs?|énoncés?|enonces?|opérations?|operations?|problèmes?|problemes?|consignes?|additions?|soustractions?|multiplications?|divisions?|leçons?|lecons?|textes?|histoires?|exercices?)(?!\p{L}))";

        [GeneratedRegex(
            // 1. sujet + (re)lire + des/d'autres… + nombres
            Sujet
            + @"\s+(?:(?:continuer|continue|recommencer|recommence)\s+(?:à|a)\s+)?(?:re)?(?:lire|lise|lisions|lit|lis)(?!\p{L})"
            + @"\s+(?:encore\s+)?(?:des|d['’]autres|quelques|de\s+nouveaux|un\s+(?:autre|nouveau))\s+(?:nombres?|chiffres?)(?!\p{L})" + PasDUnCalcul + @"[^.!?\n]*\?"
            // 2. un nouveau nombre / d'autres nombres + à (re)lire
            + @"|(?<!\p{L})(?:(?:encore\s+)?un\s+(?:autre|nouveau)\s+nombre|d['’]autres\s+(?:nombres|chiffres)|de\s+nouveaux\s+(?:nombres|chiffres)|des\s+nombres|quelques\s+nombres)\s+(?:à|a)\s+(?:re)?lire(?!\p{L})[^.!?\n]*\?"
            // 3. sujet qui propose + continue / repart sur / refait… la lecture des nombres (jamais au passé : voir Sujet)
            + "|" + Sujet + @"\s+(?:continue|continuer|reprend|reprendre|reprenne|repart|repartir|refait|refaire|refasse|fait|faire|fasse|recommence|recommencer)\s+(?:sur\s+|avec\s+)?(?:la|une|ta)\s+lecture\s+(?:des|de)\s+(?:nombres?|chiffres?)(?!\p{L})" + PasDUnCalcul + @"[^.!?\n]*\?",
            RegexOptions.IgnoreCase)]
        private static partial Regex Proposition();

        [GeneratedRegex(@"^\s*(?:oui|ouais|ouai|d'accord|dac|ok|okay|vas-y|vas y|allez|go|on y va|carr[ée]ment|volontiers|avec plaisir|je veux bien|bien s[uû]r)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Accepte();

        // Il veut arrêter ou changer : l'exercice s'arrête là.
        [GeneratedRegex(@"\b(?:arr[êe]te|arr[êe]ter|stop|autre\s+(?:chose|exercice)|on\s+change|change(?:r)?\s+d.exercice|plus\s+de\s+nombres|j.en\s+ai\s+marre|c.est\s+bon\s+les\s+nombres|on\s+fait\s+autre)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Arret();

        private static bool EstTechnique(string? contenu) =>
            string.IsNullOrWhiteSpace(contenu) || Regex.IsMatch(contenu, @"^\s*\[(?:FICHE|RAPPORT|FIN_SEANCE|EVALUATION)\]", RegexOptions.IgnoreCase);

        /// <summary>Le dernier message du professeur qui parle à l'élève (ni rapport, ni fiche).</summary>
        private static string? DernierDuProfesseur(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents) =>
            recents.LastOrDefault(m => m.Role == "assistant" && !EstTechnique(m.Contenu)).Contenu;

        /// <summary>
        /// L'élève est-il dans l'exercice à ce tour ? Il le demande, demande d'afficher un nombre de sa classe (le
        /// <paramref name="niveau"/> sert alors), accepte la proposition, ou y était déjà.
        /// </summary>
        public static bool EnCours(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents, string? messageDeLEleve,
            string? niveau = null)
        {
            var message = messageDeLEleve ?? string.Empty;
            if (DemandeDeLecture().IsMatch(message)) return true;
            // « Propose-moi un nouvel exercice » (14/11/2026) : la même règle que partout, voir ChangementDExercice.
            // « Maintenant on fait une addition » (signalement #51) : il nomme une autre activité, l'exercice s'arrête.
            if (Arret().IsMatch(message) || ChangementDExercice.Demande(message) || DemandeUneAutreActivite(message)) return false;

            // IL DEMANDE D'AFFICHER UN NOMBRE (commentaire de Min's sur le signalement #50, 20/11/2026 : « quand l'élève
            // demande d'afficher un chiffre, Nora devra afficher l'image correspondante, comme pour les exercices de
            // comptage où elle affiche les images »). « Affiche le nombre 23 », « écris-moi 7 au tableau », même hors de
            // la lecture des nombres : le code montre l'IMAGE du nombre (nb-23), au lieu de laisser le professeur l'écrire
            // à la craie. Seulement un nombre du catalogue de sa classe ou d'une classe en dessous (il a une image).
            // « Pourquoi pas 8 ? » n'y fait PAS entrer (contrôle d'Omar, v3 — régression #45) : pendant un calcul
            // (« Combien font 5 + 3 ? »), c'est sa RÉPONSE au calcul, qui doit être jugée. Il ne compte comme demande que
            // si un nombre est déjà au tableau (la lecture est ouverte : voir plus bas, Affiche(dernier)).
            if (NombreDemande(message, niveau, pourquoiPas: false) is not null) return true;

            var dernier = DernierDuProfesseur(recents);
            if (dernier is null) return false;
            if (Affiche(dernier) is not null) return true;

            // Il dit oui à « Tu veux qu'on lise des nombres ? » — une réponse courte, pas une phrase.
            return Proposition().IsMatch(dernier) && Accepte().IsMatch(message) && message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 8;
        }

        // IL NOMME UNE AUTRE ACTIVITÉ DE MATHS — signalement #51 (Adam, 20/11/2026) : « cinquante-quatre. Maintenant on fait
        // une addition » → « Oui, cinquante-quatre, bravo », puis le même 54 et « on fait toujours cet exercice ». Ni
        // « autre exercice » ni « on change » : ChangementDExercice ne le voyait pas. Un verbe qui propose (« on fait »,
        // « je veux faire », « passons à », « donne-moi »), puis une activité nommée. Pas « on a fait une addition ».
        //
        // Contrôle d'Omar (#50/#51) : le motif était trop étroit (« je veux une addition », « maintenant une addition »,
        // « on fait la table de 2 », « on fait les fractions », « on fait une dictée », « on fait du français », « on compte
        // jusqu'à 100 », « on compare des nombres » restaient en lecture, alors que le fait dit « tu la fais »). Trois
        // formes : un verbe qui propose + une activité nommée ; « maintenant » + une activité nommée ; « on » / « je veux »
        // + un verbe d'activité (compter, comparer, calculer…).
        //
        // Contrôle d'Omar (v3) : les verbes nus faisaient quitter la lecture sur une remarque de MÉTHODE (« on compte les
        // dizaines ? », « trente-quatre, on compte les dizaines », « je peux compter sur mes doigts ? », « on va comparer
        // avec quarante »). Suivis de dizaines / unités / centaines / doigts, ou de « avec » + un nombre, ils ne nomment
        // plus une activité (voir ObjetDeMethode et RemarqueDeMethode) : il reste dans la lecture.
        [GeneratedRegex(
            @"(?:(?:(?<!\p{L})(?:on|nous)\s+(?:(?:peut|pourrait|va|veut|pourra)\s+)?(?:fai(?:t|s|re)|refai(?:t|re)|pass(?:e|er)\s+(?:à|a|aux|au)|essai(?:e|er)|commenc(?:e|er))"
            + @"|(?<!\p{L})(?:je|j['’])\s*(?:veux|voudrais|aimerais|pr[ée]f[èe]re(?:rais)?|peux)(?:\s+(?:faire|refaire|passer\s+(?:à|a|aux|au)|essayer|apprendre))?"
            + @"|(?<!\p{L})(?:faisons|passons\s+(?:à|a|aux|au)|maintenant[\s,]*)"
            + @"|(?<!\p{L})(?:fais|donne|propose|pose)[- ](?:moi|nous))"
            + @"\s*(?:une?\s+|des\s+|du\s+|de\s+la\s+|les?\s+|la\s+|l['’]\s*|d['’]\s*)?(?:autres?\s+|nouvel(?:le)?s?\s+|petite?s?\s+)?"
            + @"(?:additions?|soustractions?|multiplications?|divisions?|calculs?|op[ée]rations?|probl[èe]mes?|g[ée]om[ée]trie|formes?|mesures?"
            + @"|fractions?|tables?\s+(?:de|des|du|d['’])|dict[ée]es?|fran[çc]ais|conjugaison|grammaire|orthographe|anglais|histoire|g[ée]ographie|sciences?"
            + @"|comparaisons?|monnaie)(?!\p{L})"
            + @"|(?<!\p{L})(?:(?:on|nous)\s+(?:(?:peut|pourrait|va|veut|pourra)\s+)?|(?:je|j['’])\s*(?:veux|voudrais|aimerais|peux)\s+)"
            + @"(?:compter?|comparer?|calculer?|ranger?|mesurer?|conjuguer?|dessiner?|r[ée]viser?\s+(?:les|la|le|l['’])\s*(?:tables?|additions?|soustractions?|fractions?))(?!\p{L})(?!" + ObjetDeMethode + ")"
            + @"|(?<!\p{L})(?:comptons|comparons|calculons|rangeons|mesurons|conjuguons|dessinons)(?!\p{L})(?!" + ObjetDeMethode + "))",
            RegexOptions.IgnoreCase)]
        private static partial Regex AutreActivite();

        // Les petits nombres dits en lettres (« avec quarante », « avec dix-sept »). Pas « un » / « une » : « avec un autre… ».
        private const string MotDeNombre =
            @"(?:z[ée]ro|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize|vingts?|trente|quarante|cinquante|soixante|cents?|mille)(?!\p{L})";

        // Ce qui suit un verbe de méthode (contrôle d'Omar, v3) : les dizaines, les unités, les centaines, les doigts, ou
        // « avec / à » + un nombre. « On compte jusqu'à 100 », « on compare des nombres » n'y sont pas : ce sont des activités.
        private const string ObjetDeMethode =
            @"\s+(?:(?:sur|avec|par|en)\s+)?(?:(?:les|des|mes|tes|ses|nos|vos|leurs?|la|le)\s+|l['’]\s*)?(?:dizaines?|unit[ée]s?|centaines?|milliers?|doigts?)(?!\p{L})"
            + @"|\s+(?:avec|à|a)\s+(?:(?:le|ce)\s+)?(?:nombre\s+)?(?:\d|" + MotDeNombre + ")";

        // UNE REMARQUE DE MÉTHODE PENDANT LA LECTURE (contrôle d'Omar, v3) : « on compte les dizaines ? », « je peux compter
        // sur mes doigts ? », « on va comparer avec quarante ». Ce n'est ni une autre activité, ni une lecture : sa lecture
        // éventuelle est ce qui précède (« trente-quatre, on compte les dizaines » = 34), le nombre qui suit « avec » n'est
        // pas lu (« on va comparer avec quarante » n'est pas une lecture fausse de 40).
        [GeneratedRegex(@"(?<!\p{L})(?:(?:on|nous)\s+(?:(?:peut|pourrait|va|veut|pourra|doit)\s+)?|(?:je|j['’])\s*(?:(?:veux|voudrais|aimerais|peux|vais|dois)\s+)?|tu\s+(?:(?:peux|veux|vas)\s+)?)?"
            + @"(?:compt(?:e|es|er|ons|ez)|compar(?:e|es|er|ons|ez)|calcul(?:e|es|er|ons|ez)|rang(?:e|es|er|eons|ez)|mesur(?:e|es|er|ons|ez))(?=" + ObjetDeMethode + ")",
            RegexOptions.IgnoreCase)]
        private static partial Regex RemarqueDeMethode();

        /// <summary>Il nomme une autre activité de maths (« on fait une addition ») : la lecture des nombres s'arrête.</summary>
        public static bool DemandeUneAutreActivite(string? message) =>
            !string.IsNullOrWhiteSpace(message) && AutreActivite().IsMatch(message);

        /// <summary>Il fait une remarque de méthode (« on compte les dizaines ? ») : il reste dans la lecture.</summary>
        public static bool FaitUneRemarqueDeMethode(string? message) =>
            !string.IsNullOrWhiteSpace(message) && RemarqueDeMethode().IsMatch(message);

        // Les mots qui relient sa lecture à sa demande : « vingt-huit. Maintenant on fait une addition ».
        [GeneratedRegex(@"(?:[\s,.;:!…-]|(?<!\p{L})(?:maintenant|et|alors|puis|apr[èe]s|bon|ok|d['’]accord|euh)(?!\p{L}))*$", RegexOptions.IgnoreCase)]
        private static partial Regex Liaison();

        /// <summary>
        /// SA LECTURE, PUIS SA DEMANDE — contrôle d'Omar (#50/#51) : « vingt-huit. Maintenant on fait une addition » est une
        /// lecture de 28 ET une demande d'addition. La lecture est jugée (« Bonne lecture »), puis on change d'exercice.
        /// Renvoie la partie à juger (tout le message s'il ne nomme pas d'autre activité) et la demande qui suit (ou null).
        /// </summary>
        /// <remarks>Une remarque de méthode (« trente-quatre, on compte les dizaines », contrôle d'Omar v3) se coupe de la même
        /// façon : 34 est jugé, la remarque reste pour le professeur, et on reste dans la lecture.</remarks>
        public static (string Lecture, string? Suite) Couper(string? message)
        {
            var texte = message ?? string.Empty;
            var coupure = new[] { AutreActivite().Match(texte), RemarqueDeMethode().Match(texte) }
                .Where(m => m.Success).OrderBy(m => m.Index).FirstOrDefault();
            if (coupure is null) return (texte, null);
            var avant = texte[..coupure.Index];
            var liaison = Liaison().Match(avant);
            var fin = liaison.Success ? liaison.Index : avant.Length;
            var suite = texte[fin..].TrimStart(' ', ',', '.', ';', ':', '!', '…', '-', '\n', '\t');
            return (texte[..fin].Trim(), suite.Length == 0 ? null : char.ToUpperInvariant(suite[0]) + suite[1..]);
        }

        /// <summary>Le nombre qu'il avait à lire à ce tour (celui du dernier message du professeur), ou null.</summary>
        public static Element? DernierMontre(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents) =>
            Affiche(DernierDuProfesseur(recents));

        /// <summary>
        /// LE VERDICT DE SON MESSAGE : null s'il n'y a pas de nombre à lire, ou si ce qu'il dit n'est pas une lecture
        /// (« je sais pas », une question) — l'image reste, l'essai n'est pas compté.
        /// </summary>
        public static Verdict? Juger(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents, string? messageDeLEleve) =>
            JugerContre(DernierMontre(recents), messageDeLEleve);

        /// <summary>
        /// LE VERDICT CONTRE LE NOMBRE AFFICHÉ — le même pour le clavier (ChatViewModelBuilder) et pour le micro
        /// (EcouteController) : une demande, une question ou « je sais pas » ne sont pas des lectures. S'il nomme ensuite une
        /// autre activité, seule sa lecture est jugée (<see cref="Couper"/>).
        /// </summary>
        public static Verdict? JugerContre(Element? montre, string? messageDeLEleve)
        {
            if (montre is null || string.IsNullOrWhiteSpace(messageDeLEleve) || DemandeDeLecture().IsMatch(messageDeLEleve)) return null;
            var (lecture, _) = Couper(messageDeLEleve);
            if (string.IsNullOrWhiteSpace(lecture)) return null;
            // « Écris le nombre 23 » n'est pas une lecture du nombre affiché : c'est une demande (signalement #50).
            if (CanoniqueDemande(lecture) is not null) return null;
            // « C'est toi qui choisis le nombre ? Pourquoi pas le 23 ? » (signalement #51, vu en test de bout en bout) :
            // jugé comme une lecture FAUSSE de 34 (les mots autour du nombre sont ignorés). Une question n'est pas une lecture.
            if (EstUneQuestion(lecture)) return null;
            var lu = Canonique(lecture);
            if (lu is null) return null;
            return new Verdict(montre, lu == Normaliser(montre.Cible), lecture.Trim());
        }

        // « pourquoi », « comment » : toujours une question. Les autres mots ne comptent qu'avec un point d'interrogation :
        // « c'est 34 ? » reste une lecture (il hésite), « tu peux mettre le 23 ? » n'en est pas une.
        [GeneratedRegex(@"(?<!\p{L})(?:pourquoi|comment)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex MotQuestionToujours();

        [GeneratedRegex(@"(?<!\p{L})(?:est[- ]ce|qui|quoi|quel(?:le)?s?|peux|peut|pourrais|choisis|choisit|veux|sais|as|es|on)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex MotQuestion();

        // LA LECTURE HÉSITANTE — contrôle d'Omar (régression #28/#49) : « est-ce que c'est trente-quatre ? », « on dirait
        // trente-quatre ? », « je sais ! trente-quatre ? », « tu veux dire trente-quatre ? » sont des LECTURES (il propose
        // un nombre en hésitant), pas des questions à la maîtresse. Ces mots-là ôtés, il ne reste que le nombre.
        [GeneratedRegex(@"(?<!\p{L})(?:est[- ]ce\s+qu(?:e|['’])|on\s+dirait(?:\s+que)?|je\s+(?:sais|crois|pense)(?:\s+que)?|j['’]\s*(?:crois|pense)(?:\s+que)?"
            + @"|tu\s+veux\s+dire|c['’]\s*est\s+(?:ça|ca)|c['’]\s*est|ce\s+serait|ça\s+fait|ca\s+fait|ça\s+se\s+lit|ca\s+se\s+lit|peut[- ][êe]tre"
            + @"|euh+|heu+|hum+|hm+|bah|ben|alors|ah|oh|bon|hein|voil[àa]|oui|le\s+nombre|nombre|ma[iî]tresse|nora)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex MotsDHesitation();

        // Ce qu'une lecture peut contenir en plus des mots de nombre : « virgule », « sur », « au carré », « racine de »…
        private static bool MotDeLecture(string mot) =>
            EstMotNombre(mot) || Denominateur(mot) is not null
            || mot is "et" or "zero" or "virgule" or "moins" or "plus" or "sur" or "puissance" or "exposant" or "carre" or "carree"
                or "cube" or "fois" or "racine" or "de" or "tout" or "le" or "au" or "x";

        /// <summary>Son message n'est qu'un nombre, dit en hésitant (« on dirait trente-quatre ? ») : c'est une lecture.</summary>
        public static bool EstUneLectureHesitante(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            var reste = MotsDHesitation().Replace(message, " ");
            if (Canonique(reste) is null) return false;
            var simple = Regex.Replace(SansAccents(reste.ToLowerInvariant()), @"(?<=[a-z])-(?=[a-z])", " ");
            return Regex.Matches(simple, "[a-z]+").All(m => MotDeLecture(m.Value));
        }

        /// <summary>Son message est une question à la maîtresse, pas une lecture du nombre.</summary>
        public static bool EstUneQuestion(string? message) =>
            !string.IsNullOrWhiteSpace(message)
            && !EstUneLectureHesitante(message)
            && (MotQuestionToujours().IsMatch(message) || (message.Contains('?') && MotQuestion().IsMatch(message)));

        /// <summary>Ce que garde le message de l'enfant : « J'ai lu le nombre soixante-treize. »</summary>
        public static string TexteDeLEleve(Verdict v) =>
            // Déjà écrit par le relais du micro (EcouteController) : on ne l'enveloppe pas une seconde fois.
            // « est-ce que c'est 34 ? » : sans le « ? » final (sinon « …34 ?. »).
            VerdictDeLecture().IsMatch(v.Dit) ? v.Dit.Trim() : $"J'ai lu le nombre {v.Dit.Trim().TrimEnd('.', '!', '?', ' ', '…')}.";

        [GeneratedRegex(@"^\s*j.ai lu le nombre\b", RegexOptions.IgnoreCase)]
        private static partial Regex VerdictDeLecture();

        /// <summary>
        /// Le message gardé quand il a lu PUIS demandé autre chose (contrôle d'Omar, #50/#51) : « J'ai lu le nombre
        /// vingt-huit. Maintenant on fait une addition » — le verdict reste, et le professeur lit sa demande.
        /// </summary>
        public static string TexteAvecLaSuite(Verdict v, string? message)
        {
            var texte = TexteDeLEleve(v).TrimEnd();
            if (Couper(message).Suite is not { } suite) return texte;
            if (texte.Length > 0 && !".!?".Contains(texte[^1])) texte += ".";
            return $"{texte} {suite}";
        }

        // ------------------------------------------------------------------ le nombre qu'il demande (signalement #50)

        // « Écris le nombre 23 au tableau », « montre-moi 48 », « affiche le chiffre sept », « mets-moi un autre nombre : 12 »,
        // « je veux que tu affiches 23 ».
        // Le verbe, puis seulement des petits mots de consigne, puis le nombre. « un » / « une » ne sont des mots de
        // consigne que devant « nombre », « chiffre », « autre » ou « nouveau » (« écris un nombre » ne demande pas « 1 »).
        // « Je propose 23 », « je mets 23 », « j'écris 23 », « je te donne 23 » : c'est SA réponse, pas une demande
        // (sinon une réponse à un calcul ouvrirait la lecture des nombres, et une lecture serait prise pour une demande).
        // « Pourquoi pas le 23 ? » (contrôle d'Omar, #50/#51) : une DEMANDE aussi — Nora promettait « le 23 viendra après »
        // (faux) ; la règle de Min's (#50) est d'afficher le nombre demandé.
        // Le groupe « pourquoi » dit que la demande est un « pourquoi pas N » (ne fait pas entrer dans la lecture, voir EnCours).
        [GeneratedRegex(@"(?<!\p{L})(?<!\bje\s+(?:te\s+|t['’]\s*)?|\bj['’]\s*)(?:[ée]cri(?:s|re|vez|ves?)|affiche[rsz]?|montre[rsz]?|mets|mettre|mettes?|donne[rz]?|propose[rz]?|(?<pourquoi>pourquoi\s+pas))(?!\p{L})"
            + @"(?:[-\s,:'’]+(?:moi|nous|le|la|l|ce|cet|au|du|tableau|nombre|chiffre|autre|nouveau|s|il|te|pla[iî]t|stp|maintenant|alors|plut[oô]t|(?:un|une)(?=[-\s]+(?:nombre|chiffre|autre|nouveau)))(?!\p{L}))*"
            + @"[-\s,:'’«""]+(?<suite>[^.!?;\n]{1,60})", RegexOptions.IgnoreCase)]
        private static partial Regex Demande();

        // « … 34 affiche-le » (au micro, sans virgule) : un verbe d'affichage peut suivre le nombre demandé.
        [GeneratedRegex(@"^(?:au|aux|sur|a|et|s'il|s’il|stp|svp|pour|que|qu|je|j|tableau|en|dans|ici|maintenant|merci|alors|puis|stp|please|affiche|montre|mets|ecris|donne)\b", RegexOptions.IgnoreCase)]
        private static partial Regex MotApresLeNombre();

        // IL DIT QUEL NOMBRE IL PRÉFÈRE — retest d'Adam (#51, 21/11/2026) : « Je préfère le 34, affiche-le s'il te plaît. »,
        // devant 82, était jugé comme une lecture FAUSSE de 82 (le verbe « affiche » venait APRÈS le nombre, Demande() ne le
        // voyait pas, et Canonique() ignore les mots autour du 34). « je préfère / je veux / je voudrais / j'aimerais / je
        // choisis / je prends (le) N », « on prend / on fait / on lit (le) N », « et si on prenait le N ». Pas « plutôt » seul :
        // « non, plutôt trente-quatre » corrige sa LECTURE. Pas « je préfère dire / lire… » : c'est encore sa lecture.
        // Ces formes ne font pas entrer dans la lecture des nombres (comme « pourquoi pas N », voir EnCours) : pendant un
        // calcul, « je prends 8 » reste sa réponse.
        [GeneratedRegex(@"(?<!\p{L})(?:je|j['’])\s*(?:pr[ée]f[èe]re(?:rais)?|veux|voudrais|aimerais|choisis|prends)(?!\p{L})"
            + @"|(?<!\p{L})(?:on|nous)\s+(?:(?:peut|pourrait|va)\s+)?(?:prend(?:re)?|prenons|fai(?:t|re)|li(?:t|re))(?!\p{L})"
            + @"|(?<!\p{L})(?:et\s+)?si\s+on\s+(?:prenait|faisait|lisait)(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex Preference();

        // « Le 34, affiche-le », « trente-quatre, montre-le-moi » : le verbe vient APRÈS le nombre, avec un pronom.
        [GeneratedRegex(@"(?<!\p{L})(?:affiche|montre|mets|[ée]cris|donne|propose)[- ](?:le|la|l['’])(?:[- ](?:moi|nous))?(?!\p{L})", RegexOptions.IgnoreCase)]
        private static partial Regex VerbeApresLeNombre();

        /// <summary>Le nombre qu'il demande d'afficher, en écriture canonique (« 23 »), ou null.</summary>
        /// <param name="pourquoiPas">Faux : « pourquoi pas 8 ? », « je préfère 8 » ne sont pas des demandes (hors de la lecture,
        /// voir EnCours).</param>
        public static string? CanoniqueDemande(string? message, bool pourquoiPas = true)
        {
            if (string.IsNullOrWhiteSpace(message) || VerdictDeLecture().IsMatch(message)) return null;
            foreach (Match m in Demande().Matches(message))
            {
                if (!pourquoiPas && m.Groups["pourquoi"].Success) continue;
                if (NombreQuiSuit(m.Groups["suite"].Value) is { } lu) return lu;
            }

            // « Le 34, affiche-le s'il te plaît » : le nombre juste AVANT le verbe (au plus quelques mots de liaison entre).
            foreach (Match m in VerbeApresLeNombre().Matches(message))
            {
                if (NombreQuiPrecede(message[..m.Index]) is { } lu) return lu;
            }

            if (!pourquoiPas) return null;
            foreach (Match m in Preference().Matches(message))
            {
                var suite = message[(m.Index + m.Length)..];
                // « je préfère dire… », « on lit des nombres » : pas un nombre choisi.
                var mots = Regex.Match(suite, @"^[\s,:'’]*(?:(?:le|la|l|ce|cet|plut[oô]t|alors|maintenant|nombre|chiffre)(?:[\s,:'’]+|(?=\d)))*(?<suite>[^.!?;\n]{1,60})", RegexOptions.IgnoreCase);
                if (!mots.Success) continue;
                if (NombreQuiSuit(mots.Groups["suite"].Value) is { } lu) return lu;
            }
            return null;
        }

        /// <summary>Le nombre au début de ce texte, mot à mot (« 23 au tableau et je te le lis » → « 23 »), ou null.</summary>
        private static string? NombreQuiSuit(string texte)
        {
            var mots = new List<string>();
            string? apres = null;
            foreach (var brut in texte.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var mot = brut.Trim('«', '»', '"', '“', '”', ',');
                var simple = SansAccents(mot.ToLowerInvariant());
                var estNombre = Regex.IsMatch(mot, @"^\(?[+-]?\d[\d,./^x()−-]*$")
                    || simple.Split('-', StringSplitOptions.RemoveEmptyEntries).All(p => EstMotNombre(p) || p is "et" or "zero" or "virgule");
                if (!estNombre || simple.Length == 0) { apres = simple; break; }
                mots.Add(mot);
                // « Je préfère le 34, affiche-le » : la virgule ferme le nombre.
                if (brut.EndsWith(',')) break;
            }
            while (mots.Count > 0 && SansAccents(mots[^1].ToLowerInvariant()) is "et" or "virgule") mots.RemoveAt(mots.Count - 1);
            if (mots.Count == 0) return null;
            // « Donne-moi deux exemples », « montre-moi un indice » : un nom suit, ce n'est pas un nombre à afficher.
            if (apres is not null && !MotApresLeNombre().IsMatch(apres)) return null;
            return Canonique(string.Join(' ', mots));
        }

        /// <summary>Le nombre à la fin de ce texte (« Je préfère le 34, » → « 34 »), ou null.</summary>
        private static string? NombreQuiPrecede(string texte)
        {
            // Les mots de liaison entre le nombre et le verbe : « le 34, alors affiche-le », « le 34 et montre-le ».
            var fin = Regex.Replace(texte, @"(?:[\s,.;:!…-]|(?<!\p{L})(?:et|alors|donc|maintenant|stp|svp|s['’]il\s+te\s+pla[iî]t)(?!\p{L}))*$", string.Empty,
                RegexOptions.IgnoreCase);
            var mots = new List<string>();
            foreach (var brut in fin.Split(' ', StringSplitOptions.RemoveEmptyEntries).Reverse())
            {
                var mot = brut.Trim('«', '»', '"', '“', '”', ',');
                var simple = SansAccents(mot.ToLowerInvariant());
                var estNombre = Regex.IsMatch(mot, @"^\(?[+-]?\d[\d,./^x()−-]*$")
                    || simple.Split('-', StringSplitOptions.RemoveEmptyEntries).All(p => EstMotNombre(p) || p is "et" or "zero" or "virgule");
                if (!estNombre || simple.Length == 0 || brut.EndsWith(',')) break;
                mots.Insert(0, mot);
            }
            while (mots.Count > 0 && SansAccents(mots[0].ToLowerInvariant()) is "et" or "virgule") mots.RemoveAt(0);
            return mots.Count == 0 ? null : Canonique(string.Join(' ', mots));
        }

        /// <summary>Le nombre demandé, s'il est au catalogue de sa classe ou d'une classe en dessous ; null sinon.</summary>
        public static Element? NombreDemande(string? message, string? niveau, bool pourquoiPas = true)
        {
            var rang = Rang(niveau);
            if (rang == 0 || CanoniqueDemande(message, pourquoiPas) is not { } lu) return null;
            return Tous.FirstOrDefault(e => e.Rang >= 1 && e.Rang <= rang && Normaliser(e.Cible) == lu);
        }

        // ------------------------------------------------------------------ ce que le code montre

        private const int EssaisMax = 3;

        /// <summary>
        /// LE NOMBRE DE CE TOUR. Le même tant qu'il ne l'a pas lu juste ; un autre après une bonne lecture, après trois
        /// lectures fausses de suite (on ne donne jamais la réponse, on passe), ou au début de l'exercice.
        /// </summary>
        public static Tour? AMontrer(string? niveau, IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents, Verdict? verdict, string graine,
            string? messageDeLEleve = null)
        {
            var possibles = Possibles(niveau);
            if (possibles.Count == 0) return null;

            var precedent = DernierMontre(recents);

            // IL DEMANDE UN NOMBRE PRÉCIS (20/11/2026, signalement #50 : « Écris le nombre 23 au tableau, et je te le lis »,
            // le code en montrait un autre, et « vingt-trois » était refusé). Hors d'une lecture en cours de jugement, le
            // nombre demandé est celui du tour, s'il est au catalogue de sa classe ou d'une classe en dessous.
            if (verdict is null && NombreDemande(messageDeLEleve, niveau) is { } demande)
            {
                return new Tour(demande, Redite: precedent?.Cible == demande.Cible, Premier: precedent is null, Abandon: false, Demande: true,
                    Avant: precedent);
            }

            // IL DEMANDE UN NOMBRE QU'ON NE PEUT PAS MONTRER (signalement #51 : sa demande était ignorée sans un mot, « on fait
            // toujours cet exercice »). Le tableau garde son nombre, mais le professeur reçoit sa demande (FaitHorsCatalogue)
            // et la consigne ne dit pas « essaie encore » : il n'a pas encore essayé.
            var horsCatalogue = verdict is null ? CanoniqueDemande(messageDeLEleve) : null;
            if (horsCatalogue is not null && precedent is not null)
            {
                return new Tour(precedent, Redite: true, Premier: false, Abandon: false, HorsCatalogue: horsCatalogue);
            }

            // Les lectures fausses de suite sur ce nombre, celle de ce tour comprise.
            var abandon = false;
            if (precedent is not null && verdict is { Juste: false })
            {
                var faux = 1;
                for (var i = recents.Count - 1; i >= 0; i--)
                {
                    var m = recents[i];
                    if (m.Role == "assistant" && !EstTechnique(m.Contenu))
                    {
                        if (Affiche(m.Contenu)?.Cible != precedent.Cible) break;
                        continue;
                    }
                    if (m.Role == "user" && VerdictDeLecture().IsMatch(m.Contenu ?? string.Empty))
                    {
                        if (m.SonJuste == false) faux++;
                        else break;
                    }
                }
                abandon = faux >= EssaisMax;
            }

            if (precedent is not null && verdict is not { Juste: true } && !abandon)
            {
                // Une question au lieu d'une lecture : il n'a pas essayé, la consigne ne dit pas « essaie encore » (Omar).
                // Une remarque de méthode sans lecture (« on va comparer avec quarante ») non plus (Omar, v3).
                // « On relit des nombres » non plus (retest d'Adam, #51, 21/11/2026) : il demande l'exercice, il n'a rien lu.
                return new Tour(precedent, Redite: true, Premier: false, Abandon: false,
                    SansEssai: verdict is null && (EstUneQuestion(messageDeLEleve) || FaitUneRemarqueDeMethode(messageDeLEleve)
                        || DemandeDeLecture().IsMatch(messageDeLEleve ?? string.Empty)));
            }

            var dejaVus = recents.Select(m => Affiche(m.Contenu)?.Cible).Where(c => c is not null).ToHashSet();
            var neufs = possibles.Where(e => !dejaVus.Contains(e.Cible)).ToList();
            var pool = neufs.Count > 0 ? neufs : possibles.Where(e => e.Cible != precedent?.Cible).ToList();
            if (pool.Count == 0) pool = possibles.ToList();

            var choisi = pool[Indice(graine, pool.Count)];
            return new Tour(choisi, Redite: false, Premier: precedent is null, Abandon: abandon, HorsCatalogue: horsCatalogue);
        }

        // ------------------------------------------------------------------ le message du professeur

        [GeneratedRegex(@"\[ARDOISE\][\s\S]*?\[/ARDOISE\]|\[TABLEAU_EFFACE\]|\[LECTURE_IMAGE:[^\]]*\][^\[]*\[/LECTURE_IMAGE\]|\[FIGURE\][\s\S]*?\[/FIGURE\]", RegexOptions.IgnoreCase)]
        private static partial Regex Tableau();

        [GeneratedRegex(@"\[(FR|EN)\]\s*([^\[\]]+?)\s*\[/\1\]", RegexOptions.IgnoreCase)]
        private static partial Regex Voix();

        [GeneratedRegex(@"[^.!?\n]*\?+")]
        private static partial Regex Question();

        // Sa propre consigne (« Lis ce nombre à voix haute ») : celle du code la remplace.
        // Défaut e3 (Omar, 20/11/2026) : « Regarde le tableau, lis ce nouveau nombre : » restait devant la consigne du code.
        // Le nombre peut porter un adjectif (nouveau, autre, prochain…), le tableau peut venir AVANT le verbe, et « lis-le »
        // ou « lis-moi » suffisent seuls. « Tu lis très bien les nombres » (pluriel, sans tableau) reste.
        // « dis-le-moi à voix haute », « Regarde bien le tableau et dis-moi ce que tu vois », « celui qui est affiché… dis-moi »
        // (vus au bout en bout de cette tâche) aussi, mais « dis-le-moi » seul reste (« si tu es fatigué, dis-le-moi ! »).
        // Refus d'Omar (3bc3b0b) : « dis-moi » n'est une consigne que s'il porte sur LE NOMBRE LUI-MÊME (« dis-moi ce nombre »,
        // « dis-moi comment il s'appelle », « dis-moi ce que tu vois », « dis-moi ! » seul après le tableau) ; « Dis-moi combien de
        // dizaines a le nombre. », « Dis-moi d'abord le chiffre des dizaines, le tableau t'aide. » sont de vrais indices et restent.
        // Même chose pour « lis / dis-le » suivis de « d'abord », « combien », « le chiffre » (`PasUnIndice`) ; « dis-le » ne s'arrête
        // jamais avant « -moi » (sinon « Dis-le-moi d'abord… » échappait à la garde).
        [GeneratedRegex(@"[^.!?\n]*(?:"
            + @"(?<!\p{L})(?:re)?lis-(?:le|la|moi)(?!\p{L})"
            + @"|(?<!\p{L})(?:lis|lire|relis|relire|dis-le(?:-moi)?)(?![\p{L}-])" + PasUnIndice + @"[^.!?\n]*(?:\b(?:voix\s+haute|au\s+tableau|le\s+tableau)\b|" + CeNombre + @")"
            + @"|(?<!\p{L})" + DisMoi + @"\s*" + CeNombre
            + @"|(?<!\p{L})" + DisMoi + @"\s*ce\s+que\s+tu\s+vois\b[^.!?\n]*\b(?:au|le|sur\s+le)\s+tableau\b"
            + @"|(?:\btableau\b|\baffich\p{L}*|\bcelui\s+qui\b|" + CeNombre + @")[^.!?\n]*(?<!\p{L})(?:"
            + @"(?:lis|relis|dis-le(?:-moi)?)(?![\p{L}-])" + PasUnIndice
            + @"|" + DisMoi + @"\s*(?:" + CeNombre + @"|comment\s+il\s+s['’]appelle\b|ce\s+que\s+tu\s+vois\b|(?=[.!:]|\r?\n|$))"
            + @")"
            + @")[^.!?\n]*[.!:]*", RegexOptions.IgnoreCase)]
        private static partial Regex ConsigneDuProfesseur();

        // « dis-moi », « dis-moi bien », « dis-moi, à voix haute, » : l'objet qui suit décide si c'est une consigne.
        private const string DisMoi = @"dis-moi(?!\p{L})(?:\s+(?:bien|vite|donc))?(?:\s*,?\s*[àa]\s+voix\s+haute\s*,?)?";

        // Ce qui suit le verbe en fait un indice sur une partie du nombre, pas l'ordre de le lire (Omar, refus de 3bc3b0b).
        private const string PasUnIndice = @"(?!\s+(?:d['’]abord|combien|(?:le|les|un|ce|chaque)\s+chiffres?\b|seulement|juste\b))";

        // « Regarde le prochain nombre. », « Regarde bien celui qui est au tableau. » : une phrase qui ne fait QUE montrer le
        // nombre double la consigne du code (vu au bout en bout). « Regarde bien le nombre, le chiffre de gauche… » reste.
        [GeneratedRegex(@"(?<=^|[.!?\n])[ \t]*(?:(?:allez|continue|maintenant|et)\s*,?\s*)?regarde(?:\s+bien)?\s+(?:" + CeNombre
            + @"|le\s+tableau|celui\s+qui\s+est\s+(?:au\s+tableau|affich\p{L}*))(?:\s+suivant)?"
            + @"(?:\s+(?:qui\s+est\s+)?(?:affich\p{L}*|au\s+tableau|sur\s+le\s+tableau)){0,2}(?:\s+maintenant)?[ \t]*(?:[.!:]+|(?=\r?\n|$))",
            RegexOptions.IgnoreCase)]
        private static partial Regex RegardeSeulement();

        // « Mais on regarde un autre nombre maintenant, regarde le tableau : » (bout en bout final) : la fin de phrase qui
        // renvoie au tableau, juste avant la consigne du code, part ; le début de la phrase reste, avec un point.
        [GeneratedRegex(@"[ \t]*,[ \t]*(?:et\s+)?regarde(?:\s+bien)?\s+(?:le\s+tableau|" + CeNombre + @"(?:\s+suivant)?)[ \t]*[.!:]*(?=[ \t]*(?:\r?\n|$))",
            RegexOptions.IgnoreCase)]
        private static partial Regex RegardeEnFin();

        // « Encore un nombre à lire. » (capture t18 du retest d'Adam, #51), « Regarde le tableau, voici un nouveau nombre à
        // lire. » (bout en bout de cette tâche) : une phrase qui ne fait qu'ANNONCER le nombre suivant double la consigne du
        // code (« Voici un autre nombre : lis-le à voix haute. »). « Encore un nombre à lire et on fait une pause » reste.
        [GeneratedRegex(@"(?<=^|[.!?\n])[ \t]*(?:(?:allez|et|maintenant|regarde(?:\s+bien)?\s+le\s+tableau)\s*,?\s*)?(?:voici|voil[àa]|encore|place\s+[àa])\s+"
            + @"(?:un|le|ton)\s+(?:(?:nouveau|autre|prochain|petit)\s+)?nombre(?:\s+(?:[àa]\s+lire|pour\s+toi|suivant))*[ \t]*(?:[.!:]+|(?=\r?\n|$))",
            RegexOptions.IgnoreCase)]
        private static partial Regex AnnonceSeulement();

        // « ce nombre », « le nouveau nombre », « cet autre nombre », « l'autre nombre », « un nouveau nombre ».
        private const string CeNombre =
            @"(?:\b(?:ce|cet|le|un)\s+|\bl['’]\s*)(?:(?:nouveau|nouvel|autre|prochain|petit|grand|beau|deuxi[eè]me|second|dernier|m[eê]me)\s+)?nombre\b";

        // « Écoute bien le nombre et dis-le-moi » (signalement #50) : il ne l'entend pas, il le LIT. La phrase s'en va,
        // la consigne du code (« Regarde bien le nombre… ») dit la bonne chose.
        [GeneratedRegex(@"[^.!?\n]*(?<!\p{L})(?:[ée]coute(?:s|z|r)?|entend(?:s|re)?)(?!\p{L})[^.!?\n]*[.!]*", RegexOptions.IgnoreCase)]
        private static partial Regex PhraseDEcoute();

        [GeneratedRegex(@"\n{3,}")]
        private static partial Regex LignesVides();

        /// <summary>
        /// LE MESSAGE DU PROFESSEUR, REMIS EN ORDRE : sa phrase (bravo, encouragement), la consigne du code, la balise. Ce
        /// qu'il aurait écrit au tableau, ses questions et sa propre consigne s'en vont. Un au revoir reste tel quel.
        /// </summary>
        public static string Reecrire(string texte, Tour tour) => Reecrire(texte, tour, null);

        [GeneratedRegex(@"[^.!?\n]+[.!?]*")]
        private static partial Regex Phrase();

        // ------------------------------------------------------------------ hors de l'exercice (défaut e8)

        [GeneratedRegex(@"\[LECTURE_IMAGE:nombre\][^\[\]]*\[/LECTURE_IMAGE\]", RegexOptions.IgnoreCase)]
        private static partial Regex BaliseComplete();

        // Une balise ouverte et jamais fermée (fin de réponse) : elle part avec ce qui la suit sur la ligne.
        [GeneratedRegex(@"\[LECTURE_IMAGE:nombre\][^\[\]\n]*(?:\[/LECTURE_IMAGE\]?)?", RegexOptions.IgnoreCase)]
        private static partial Regex BaliseOuverte();

        /// <summary>Une balise de nombre, même mal fermée, est-elle dans ce texte ?</summary>
        public static bool ContientUneBalise(string? texte) => !string.IsNullOrEmpty(texte) && BaliseOuverte().IsMatch(texte);

        /// <summary>Retire les balises de nombre complètes (et, si <paramref name="ouverteAussi"/>, une balise restée ouverte).</summary>
        public static string SansBaliseComplete(string texte, bool ouverteAussi, out bool retire)
        {
            var propre = (ouverteAussi ? BaliseOuverte() : BaliseComplete()).Replace(texte ?? string.Empty, string.Empty);
            retire = propre.Length != (texte ?? string.Empty).Length;
            return propre;
        }

        public static string SansBaliseComplete(string texte) => SansBaliseComplete(texte, false, out _);

        /// <summary>
        /// HORS DE LA LECTURE DES NOMBRES, la balise d'un nombre ne vient que du modèle qui recopie l'historique : elle part, et
        /// la consigne qui l'annonçait (« Regarde le tableau, lis ce nombre à voix haute : ») aussi, sinon l'enfant cherche un
        /// nombre qui n'est pas au tableau. Un texte sans balise revient tel quel, au caractère près.
        /// </summary>
        public static string SansBaliseHorsLecture(string texte, out bool retire)
        {
            retire = ContientUneBalise(texte);
            if (!retire) return texte ?? string.Empty;

            var propre = BaliseOuverte().Replace(texte, string.Empty);
            propre = ConsigneDuProfesseur().Replace(propre, string.Empty);
            var lignes = propre.Split('\n').Select(l => l.TrimEnd());
            return LignesVides().Replace(string.Join("\n", lignes), "\n\n").Trim();
        }

        /// <summary>
        /// La phrase cite le nombre à lire : en chiffres (« 86 », « 12 345 », jamais dans « 186 » ni « 86,5 »), ou en lettres
        /// (« quatre-vingt-six ») pour un entier à partir de 10 — en dessous, « un », « deux » sont des mots de tous les jours.
        /// </summary>
        public static bool CiteLeNombre(string phrase, Element element)
        {
            foreach (var forme in new[] { Affichage(element.Cible), element.Cible }.Distinct())
            {
                if (Regex.IsMatch(phrase, @"(?<![\d,])" + Regex.Escape(forme) + @"(?!\d|,\d)")) return true;
            }
            // « Soixante-treize et quarante » : le « et » de « soixante et onze » collait les deux nombres en un seul (vu au
            // bout en bout) ; on lit aussi la phrase avec chaque « et » comme une coupure (un mot qui n'est pas un nombre), sauf devant « un » et « onze ».
            return long.TryParse(element.Cible, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= 10
                && new[] { phrase, Regex.Replace(phrase, @"\bet\b(?!\s+(?:un|une|onze)\b)", " puis ", RegexOptions.IgnoreCase) }
                    .Any(p => Jetons(p).Any(j => j.Sorte == Sorte.Nombre && j.Valeur == element.Cible));
        }

        /// <summary>
        /// POUR LE JOURNAL DE DIAGNOSTIC SEULEMENT (refus d'Omar, 3bc3b0b) : le texte brut du professeur appelle l'enfant par son
        /// prénom. Le prénom (en entier, et chaque partie d'un prénom composé dès 2 lettres) devient « [prénom] », sans tenir
        /// compte de la casse, et jamais à l'intérieur d'un autre mot (« Léo » ne touche pas « Léonard »… ni l'inverse).
        /// </summary>
        public static string SansPrenom(string? texte, string? prenom)
        {
            var propre = texte ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prenom) || propre.Length == 0) return propre;

            var formes = new[] { prenom.Trim() }
                .Concat(prenom.Split(new[] { '-', ' ', '\'', '’' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(f => f.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(f => f.Length);
            foreach (var forme in formes)
            {
                propre = Regex.Replace(propre, @"(?<![\p{L}\p{N}])" + Regex.Escape(forme) + @"(?![\p{L}\p{N}])", "[prénom]",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            return propre;
        }

        /// <summary>Le texte dit quelque chose à l'élève (des lettres, hors tableau et balises).</summary>
        public static bool AUnePhrase(string? texte) =>
            Regex.IsMatch(Tableau().Replace(texte ?? string.Empty, " "), @"\p{L}");

        /// <summary>
        /// Même réécriture ; <paramref name="retraits"/> reçoit le nom de chaque règle qui a retiré quelque chose (« question »,
        /// « consigne »…), pour le journal : une phrase entièrement retirée doit pouvoir s'expliquer (défaut e5, Omar).
        /// </summary>
        public static string Reecrire(string texte, Tour tour, ICollection<string>? retraits)
        {
            if (LectureDImage.ConclutLaSeance(texte)) return texte;

            string Etape(string avant, string regle, Func<string, string> retirer)
            {
                var apres = retirer(avant);
                if (retraits is not null && apres != avant) retraits.Add(regle);
                return apres;
            }

            var propre = Etape(texte ?? string.Empty, "tableau", t => Tableau().Replace(t, " "));
            propre = Voix().Replace(propre, "$2");
            propre = Etape(propre, "question", t => Question().Replace(t, string.Empty));
            propre = Etape(propre, "consigne", t => AnnonceSeulement().Replace(RegardeEnFin().Replace(
                RegardeSeulement().Replace(ConsigneDuProfesseur().Replace(t, string.Empty), string.Empty), "."), string.Empty));
            propre = Etape(propre, "ecoute", t => PhraseDEcoute().Replace(t, string.Empty));
            if (tour.Demande) propre = Etape(propre, "refus", t => RefusDAfficher().Replace(t, string.Empty));
            // « Le tableau est vide » (vu par Omar après un rechargement, #51) : pendant la lecture, le nombre est au tableau.
            propre = Etape(propre, "tableau-vide", t => TableauVide().Replace(t, string.Empty));
            // « C'est le code qui va choisir un nombre pour toi » (signalement #51) : jamais de fonctionnement interne à l'enfant.
            propre = Etape(propre, "jargon", JargonTechnique.Retirer);

            // Le nombre à lire lui-même, s'il l'a cité (il ne le connaît pas, mais une redite peut le reprendre) : la PHRASE part.
            // Défaut e5 (Omar, 20/11/2026) : c'était la LIGNE entière. « Oui, 86 c'est bien plus grand que 40 ! Beau
            // raisonnement. » perdait aussi « Beau raisonnement. », et il ne restait que la consigne du code (journal Debug).
            if (tour.Redite)
            {
                propre = Etape(propre, "nombre", t => Phrase().Replace(t, m => CiteLeNombre(m.Value, tour.Element) ? string.Empty : m.Value));
                // Signalement #52 : un indice FAUX sur les chiffres du nombre (« plus grand que cinq, donc ça commence par
                // soixante » devant 58) est remplacé par l'indice exact, calculé par le code.
                propre = Etape(propre, "indice-faux", t => SansIndiceFaux(t, tour.Element));
            }

            // LE NOMBRE DEMANDÉ REMPLACE CELUI DU TABLEAU (retest d'Adam, #51, 21/11/2026) : « C'est 35 qui s'affiche
            // maintenant » (35 n'y est plus), « On est déjà sur le 23 » (il vient d'arriver). La phrase qui cite l'ancien nombre ou
            // le nombre demandé (c'est lui qui le lit), ou qui prétend qu'on y était déjà, part ; la consigne du code dit
            // « voici le nombre que tu as demandé ».
            if (tour is { Demande: true, Redite: false, Avant: { } avant })
            {
                propre = Etape(propre, "contredit", t => Phrase().Replace(ContreditLeTableau().Replace(t, string.Empty),
                    m => CiteLeNombre(m.Value, avant) || CiteLeNombre(m.Value, tour.Element) ? string.Empty : m.Value));
            }

            var lignes = propre.Split('\n').Select(l => Regex.Replace(l, @"[ \t]{2,}", " ").Trim());
            var corps = LignesVides().Replace(string.Join("\n", lignes), "\n\n").Trim();

            var morceaux = new List<string>();
            if (corps.Length > 0) morceaux.Add(corps);
            morceaux.Add(Consigne(tour));
            morceaux.Add(Balise(tour.Element));
            return string.Join("\n\n", morceaux);
        }

        /// <summary>
        /// La consigne du code. SES DÉBUTS SONT RECONNUS PAR L'ÉCRAN (`CONSIGNE_DE_LECTURE` dans lectureDImage.js) pour
        /// faire attendre la nouvelle image jusqu'à la voix : les deux se modifient ensemble.
        /// </summary>
        public static string Consigne(Tour tour) =>
            // Un nombre demandé qui REMPLACE celui du tableau (retest d'Adam, #51, 21/11/2026) : la consigne du code dit
            // elle-même que le tableau montre SA demande (le professeur disait « C'est 35 qui s'affiche maintenant » devant 23).
            tour.Demande && !tour.Redite && tour.Avant is not null ? "Regarde le tableau : voici le nombre que tu as demandé, lis-le à voix haute."
            // Un nombre demandé (même celui qui est déjà au tableau), hors catalogue, ou après une question : il n'a pas encore
            // essayé de le lire, donc jamais « essaie encore » (contrôle d'Omar, #50/#51).
            : tour.HorsCatalogue is not null || tour.SansEssai || tour.Demande ? "Regarde le tableau : lis ce nombre à voix haute."
            : tour.Redite ? "Regarde bien le nombre, et essaie encore de le lire à voix haute."
            : tour.Premier ? "Regarde le tableau : lis ce nombre à voix haute."
            : "Voici un autre nombre : lis-le à voix haute.";

        /// <summary>Le fait de l'exercice, à chaque tour.</summary>
        public const string FaitType =
            // Signalement #51 : « C'est le CODE qui choisit le nombre » était recopié tel quel à l'enfant (« c'est le code qui
            // va choisir un nombre pour toi »). Le fait ne parle plus du code, et donne la règle de JargonTechnique.
            "[Fait pour ce tour : l'élève fait l'exercice de LECTURE DES NOMBRES. Le nombre (ou celui que l'élève a demandé, "
            + "quand c'est possible) s'affiche au tableau en image, et la consigne de lecture est ajoutée à la fin de ton message. "
            + "Toi, tu écris seulement une ou deux phrases courtes (bravo, encouragement, un indice de méthode), SANS lui "
            + "redemander de lire : la consigne est déjà là. Tu n'écris et ne prononces JAMAIS le nombre à lire, et tu n'écris "
            + "rien au tableau (ni [ARDOISE], ni [FIGURE]). S'il demande une autre activité, tu la fais. Il LIT le nombre au "
            + "tableau, il ne l'entend pas : ne dis jamais « écoute » ni « tu as entendu », dis « regarde ». Ne promets jamais "
            + "qu'un nombre viendra « après » ou « plus tard ». "
            + JargonTechnique.Regle + "]";

        // ------------------------------------------------------------------ au retour (contrôle d'Omar, 21/11/2026)

        /// <summary>
        /// LA LECTURE REPREND AU TOUR D'ARRIVÉE — refus d'Omar sur le retest #51 (21/11/2026), régression du défaut e8 :
        /// l'enfant recharge la page pendant la lecture, et le professeur écrit un message de retour (« Coucou, on se
        /// retrouve… »). Ce tour était hors de l'exercice : sa balise de nombre était retirée (<see cref="SansBaliseHorsLecture"/>),
        /// l'exercice se fermait, « sept » n'était plus jugé et le professeur disait « le tableau est vide ». Si son dernier
        /// message montrait un nombre, le tour d'arrivée RESTE dans l'exercice et le code remontre ce nombre : le même (pas
        /// de nouveau tirage, les essais faux déjà faits comptent toujours), et la consigne ne dit pas « essaie encore » (il
        /// n'a rien lu à ce tour). Null s'il n'y avait pas de nombre au tableau.
        ///
        /// Pas de reprise après une séance terminée (contrôle d'Omar sur 44fd5c6, 21/11/2026) : <see cref="DernierDuProfesseur"/>
        /// saute les messages qui COMMENCENT par [RAPPORT] ou [FIN_SEANCE], donc un « [RAPPORT]… [FIN_SEANCE] » écrit après
        /// le nombre (bouton « Quitter », conclusion du minuteur) laissait le nombre revenir. Si le message du nombre ou
        /// n'importe quel message postérieur contient l'une de ces balises, où qu'elle soit, la séance est close : null.
        ///
        /// OPTION RECHARGEMENT (tâche 0140) : un rechargement de page passe par le même chemin serveur que « Quitter »
        /// (sortie signalée, puis le worker de départ écrit un message technique [FICHE]/[RAPPORT]…[FIN_SEANCE]). Avec
        /// <paramref name="rechargement"/> (voir <see cref="EstUnRechargement"/>), ces messages TECHNIQUES postérieurs au
        /// nombre ne ferment pas la lecture ; une balise de fin dans le message du nombre lui-même la ferme toujours.
        /// </summary>
        public static Tour? Reprise(IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> recents, bool rechargement = false)
        {
            var dernier = -1;
            for (var i = recents.Count - 1; i >= 0; i--)
            {
                if (recents[i].Role == "assistant" && !EstTechnique(recents[i].Contenu)) { dernier = i; break; }
            }
            if (dernier < 0 || Affiche(recents[dernier].Contenu) is not { } montre) return null;
            for (var i = dernier; i < recents.Count; i++)
            {
                if (rechargement && i > dernier && recents[i].Role == "assistant" && EstTechnique(recents[i].Contenu)) continue;
                if (ConclutLaSeance(recents[i].Contenu)) return null;
            }
            return new Tour(montre, Redite: true, Premier: false, Abandon: false, SansEssai: true);
        }

        /// <summary>Au-delà, ce n'est plus un rechargement de page mais une absence (option rechargement, tâche 0140).</summary>
        public static readonly TimeSpan FenetreRechargement = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Un RETOUR qui suit de moins d'une minute la sortie signalée par l'écran : la page a été rechargée (le serveur ne
        /// distingue pas un rechargement d'une fermeture ni du bouton « Quitter » ; un « Quitter » suivi d'un retour en
        /// moins d'une minute est donc traité comme un rechargement).
        /// </summary>
        public static bool EstUnRechargement(TypeAccueil type, TimeSpan? absenceApresSortie) =>
            type == TypeAccueil.Retour && absenceApresSortie is { } a && a >= TimeSpan.Zero && a <= FenetreRechargement;

        /// <summary>
        /// Les tours d'arrivée où la lecture peut reprendre (décision du coordinateur après le contrôle d'Omar sur 44fd5c6,
        /// 21/11/2026, le choix le plus sûr) : le RETOUR (rechargement de la page, retour après une absence dans la même
        /// séance) et l'annonce « cinq minutes » du minuteur. Jamais une NOUVELLE SÉANCE : elle repart à zéro. Le bouton
        /// « Quitter » ou une séance terminée sont fermés par <see cref="Reprise"/> ([RAPPORT] / [FIN_SEANCE]).
        /// </summary>
        public static bool RepriseAuTourDArrivee(TypeAccueil type) => type is TypeAccueil.Retour or TypeAccueil.FinProche;

        // Une balise de fin de séance n'importe où dans le message (pas seulement au début, voir EstTechnique).
        [GeneratedRegex(@"\[(?:RAPPORT|FIN_SEANCE)\]", RegexOptions.IgnoreCase)]
        private static partial Regex BaliseDeFinDeSeance();

        private static bool ConclutLaSeance(string? contenu) =>
            !string.IsNullOrEmpty(contenu) && BaliseDeFinDeSeance().IsMatch(contenu);

        // « Le tableau est vide », « j'ai effacé le tableau », « le tableau est tout propre » : toujours faux pendant la lecture
        // des nombres, le nombre à lire y est (refus d'Omar sur le retest #51 : « le tableau est vide » après un rechargement).
        [GeneratedRegex(@"[^.!?\n]*(?:\btableau\b[^.!?\n]*(?<!\p{L})(?:vide|effac\p{L}*|tout\s+propre)(?!\p{L})"
            + @"|(?<!\p{L})(?:vide|effac\p{L}*)(?!\p{L})[^.!?\n]*\btableau\b"
            + @"|(?<!\p{L})j['’]ai\s+(?:tout\s+)?effac\p{L}*|\bplus\s+rien\s+(?:au|sur\s+le)\s+tableau\b)[^.!?\n]*[.!]*", RegexOptions.IgnoreCase)]
        private static partial Regex TableauVide();

        /// <summary>
        /// Le fait du tour d'arrivée qui reprend la lecture (<see cref="Reprise"/>) : sans lui, le modèle croit le tableau
        /// effacé (le bloc « LE TABLEAU VIENT D'ÊTRE EFFACÉ » d'un retour) ou propose autre chose.
        /// </summary>
        public const string FaitReprise =
            "[Fait pour ce tour : il revient pendant l'exercice de LECTURE DES NOMBRES, et l'exercice CONTINUE. Le nombre "
            + "qu'il avait à lire s'affiche DE NOUVEAU au tableau, en image : le tableau n'est pas vide. Écris seulement UNE "
            + "phrase courte qui ne parle ni du tableau ni de lire (« Te revoilà ! », « Content de te retrouver, on continue ! », "
            + "ou le temps qui reste), SANS lui proposer une autre activité ni lui demander ce qu'il veut faire : la consigne de "
            + "lecture est ajoutée après ta phrase, ne l'écris pas toi-même. Ne prononce pas le nombre et n'écris rien au "
            + "tableau : c'est lui qui le lit. "
            + JargonTechnique.Regle + "]";

        /// <summary>
        /// Le même fait pour l'annonce « cinq minutes » du minuteur au milieu de la lecture : il n'est pas parti (le fait du
        /// retour lui faisait dire « Te revoilà ! », vu au bout en bout de la tâche 0100).
        /// </summary>
        public const string FaitRepriseApresAnnonce =
            "[Fait pour ce tour : il est en pleine LECTURE DES NOMBRES, il n'est pas parti, et l'exercice CONTINUE : le nombre "
            + "qu'il doit lire reste au tableau, en image. Écris seulement UNE phrase courte qui dit le temps qui reste, sans "
            + "parler du tableau ni de lire et sans dire « te revoilà » : la consigne de lecture est ajoutée après ta phrase, ne "
            + "l'écris pas toi-même. Ne prononce pas le nombre et n'écris rien au tableau : c'est lui qui le lit. "
            + JargonTechnique.Regle + "]";

        /// <summary>
        /// Le fait d'une arrivée où la lecture NE reprend PAS alors qu'un nombre était au tableau (nouvelle séance, retour
        /// après « Quitter » ou une séance conclue : contrôle d'Omar sur 44fd5c6, tâche 0140). Sans lui, le professeur
        /// disait « Regarde bien le tableau » ou « On repart sur le nombre qu'on regardait ? » devant un tableau vide.
        /// </summary>
        public const string FaitLectureTerminee =
            "[Fait pour ce tour : la lecture des nombres de la dernière fois est TERMINÉE. Aucun nombre n'est affiché au "
            + "tableau, et l'ancien nombre ne reviendra pas. Ne lui demande pas de regarder ni de lire un nombre au tableau, "
            + "ne reprends pas cet ancien nombre et ne le réécris pas dans [ARDOISE]. Accueille-le, puis propose-lui une "
            + "activité ou demande-lui ce qu'il veut faire : s'il veut relire des nombres, un nouveau nombre s'affichera. "
            + JargonTechnique.Regle + "]";

        /// <summary>
        /// Le fait d'une arrivée sans reprise quand l'historique montre des nombres de la lecture, mais pas dans le dernier
        /// message du professeur (retest d'Adam, #51, 21/11/2026) : relancé 22 s après « Quitter », le professeur disait « On
        /// repart sur le 23, tu me le lis ? » devant un tableau vide (23 venait de plus haut dans l'historique). Il ne pousse
        /// pas à changer d'activité (une autre activité peut être en cours) : il dit seulement l'état réel du tableau.
        /// </summary>
        public const string FaitAucunNombreAffiche =
            "[Fait pour ce tour : aucun nombre de la lecture des nombres n'est affiché au tableau. Ne parle d'aucun nombre "
            + "comme s'il était affiché, ne lui demande pas de lire un nombre au tableau et ne reprends pas un ancien nombre de "
            + "l'historique : s'il veut lire des nombres, un nouveau nombre s'affichera. "
            + JargonTechnique.Regle + "]";

        /// <summary>Un nombre de la lecture apparaît-il dans un message du professeur de cet historique ?</summary>
        public static bool AMontreDesNombres(IEnumerable<(string? Role, string? Contenu)> historique) =>
            historique.Any(m => m.Role == "assistant" && ContientUneBalise(m.Contenu));

        /// <summary>
        /// Le fait d'un changement d'exercice alors qu'un nombre était au tableau (défaut e8, bout en bout de la tâche
        /// 2026-11-20-2016) : sa balise de nombre est désormais retirée hors de l'exercice, il doit donc savoir que le tableau
        /// n'en montrera plus, sinon il annonce « lis ce nombre » ou « on y va ! » devant un tableau vide.
        /// </summary>
        public const string FaitFinDeLecture =
            "[Fait pour ce tour : l'exercice de lecture des nombres est terminé, et plus aucun nombre ne s'affiche en image au "
            + "tableau. Ne lui demande pas de lire « le nombre du tableau ». Si l'activité qu'il demande utilise des nombres "
            + "(compter, comparer…), écris-les toi-même dans ton message ou au tableau avec [ARDOISE], et commence-la tout de suite.]";

        /// <summary>
        /// Le fait d'un nombre qu'il a demandé lui-même (signalement #50) : sans lui, le professeur répondait « je ne choisis
        /// pas moi-même le nombre, c'est l'exercice qui s'en charge » alors que le tableau montrait justement le sien.
        /// </summary>
        public const string FaitDemande =
            "[Fait pour ce tour : il a demandé lui-même ce nombre, et il S'AFFICHE BIEN au tableau, en image (le nombre "
            + "qu'il a demandé, pas un autre). Dis-lui en une phrase que c'est parti. Ne dis JAMAIS que tu ne peux pas "
            + "l'afficher, que tu ne choisis pas le nombre ou qu'on continue avec un autre nombre, et ne le prononce pas : "
            + "c'est lui qui le lit.]";

        /// <summary>
        /// Le fait d'un nombre demandé, selon le tableau d'avant — retest d'Adam (#51, 21/11/2026) : 35 au tableau, « Pourquoi
        /// pas le 23 ? », le tableau passait à 23 et le professeur disait « C'est 35 qui s'affiche maintenant » ou « On est
        /// déjà sur le 23 » (il voyait encore 35 dans l'historique). Le fait dit maintenant ce qui a CHANGÉ au tableau.
        /// </summary>
        public static string FaitDemandeDe(Tour tour)
        {
            if (tour.Redite)
            {
                return "[Fait pour ce tour : le nombre qu'il demande est DÉJÀ celui du tableau, en image. Dis-lui en une phrase que "
                    + "oui, c'est bien celui-là, et ne le prononce pas : c'est lui qui le lit.]";
            }
            if (tour.Avant is { } avant)
            {
                return $"[Fait pour ce tour : il a demandé le nombre {Affichage(tour.Element.Cible)}. Le tableau montrait jusqu'ici "
                    + $"{Affichage(avant.Cible)} : ce nombre-là a DISPARU, et le tableau montre MAINTENANT, en image, le nombre qu'il a "
                    + "demandé. Dis-lui juste en une phrase que tu es d'accord (« D'accord ! », « Bonne idée ! ») : la consigne du "
                    + "code dit déjà que c'est son nombre. Ne parle plus de l'ancien nombre, ne dis pas qu'on y était déjà ni qu'un "
                    + "autre nombre s'affiche, ne dis JAMAIS que tu ne peux pas l'afficher, et ne prononce aucun des deux nombres : "
                    + "c'est lui qui lit.]";
            }
            return FaitDemande;
        }

        // « On est déjà sur le 23 », « il est déjà au tableau », « c'est un autre nombre qui s'affiche » : faux quand le nombre
        // demandé vient de remplacer celui du tableau (retest d'Adam, #51). « Tu lis déjà très bien ! » reste.
        [GeneratedRegex(@"[^.!?\n]*(?:(?<!\p{L})d[ée]j[àa](?!\p{L})[^.!?\n]*(?:\bsur\s+(?:le|ce|celui)\b|\btableau\b|\baffich|\b[ée]cran\b|(?<!\p{L})l[àa](?!\p{L})|\bcelui)"
            + @"|\b(?:un\s+)?autre\s+nombre\b[^.!?\n]*\baffich|\baffich\p{L}*\b[^.!?\n]*\b(?:un\s+)?autre\s+nombre\b)[^.!?\n]*[.!]*", RegexOptions.IgnoreCase)]
        private static partial Regex ContreditLeTableau();

        /// <summary>
        /// Le fait d'un nombre demandé qu'on ne peut pas montrer en image (signalement #51) : le tableau garde le sien, et
        /// le professeur répond à sa demande comme un enseignant, au lieu de l'ignorer.
        /// </summary>
        public static string FaitHorsCatalogue(string demande) =>
            $"[Fait pour ce tour : il t'a demandé d'afficher le nombre {Affichage(demande)}, mais celui-là ne s'affiche pas "
            + "dans cet exercice : le tableau garde le nombre d'avant. Réponds à sa demande en une phrase gentille et simple, "
            + "comme une maîtresse (« Celui-là, on le lira une autre fois ! »), puis encourage-le à lire celui du tableau. Ne "
            + "prononce pas le nombre du tableau. " + JargonTechnique.Regle + "]";

        // Le refus que le modèle écrit encore parfois devant un nombre demandé (« je ne peux pas te l'afficher pour
        // l'instant », « je ne choisis pas moi-même le nombre ») alors que le tableau le montre : la phrase s'en va.
        [GeneratedRegex(@"[^.!?\n]*\b(?:(?:je\s+)?ne\s+(?:peux|pourrai|sais)\s+pas|impossible)\b[^.!?\n]*\b(?:afficher|montrer|[ée]crire|mettre)\b[^.!?\n]*[.!]*"
            + @"|[^.!?\n]*\bje\s+ne\s+choisis\s+pas\b[^.!?\n]*[.!]*"
            + @"|[^.!?\n]*\bon\s+continue\s+(?:plut[oô]t\s+)?avec\s+(?:les|des|un|d.autres?)\s+(?:autres?\s+)?nombres?\b[^.!?\n]*[.!]*", RegexOptions.IgnoreCase)]
        private static partial Regex RefusDAfficher();

        /// <summary>Le fait du verdict.</summary>
        public static string FaitVerdict(Verdict v, Tour? tour)
        {
            const string fin = " Ne parle jamais du micro ni de cet outil à l'élève. " + JargonTechnique.Regle + "]";
            var cible = Affichage(v.Montre.Cible);
            var lu = Canonique(v.Dit) is { } c ? Affichage(c) : null;

            if (v.Juste)
            {
                return $"[Fait pour ce tour : il a bien lu le nombre {cible}. Son écran affiche déjà « Bonne lecture » : félicite-le en une "
                    + "phrase. Le nombre suivant s'affiche ensuite au tableau." + fin;
            }

            if (tour is { Abandon: true })
            {
                return $"[Fait pour ce tour : il devait lire {cible} et a dit « {v.Dit} »{(lu is null ? string.Empty : $" (soit {lu})")}, pour la "
                    + "troisième fois sans y arriver. Un autre nombre s'affiche à sa place. Dis-lui en une phrase que ce n'est pas grave et qu'on "
                    + "y reviendra. NE DONNE PAS la réponse et ne la prononce pas." + fin;
            }

            // Signalement #52 : l'indice sur les chiffres est CALCULÉ ici (le professeur inventait « plus grand que cinq » devant 58).
            if (IndiceExact(v.Montre.Cible, Canonique(v.Dit)) is { } indice)
            {
                return $"[Fait pour ce tour : il devait lire {cible} et a dit « {v.Dit} »{(lu is null ? string.Empty : $" (soit {lu})")}. Son écran "
                    + "affiche « On réessaie ». Encourage-le en une phrase, puis donne l'indice ci-dessous. NE DONNE JAMAIS la réponse et "
                    + "ne la prononce pas : le même nombre reste au tableau. " + ConsigneDIndice(indice) + fin;
            }

            return $"[Fait pour ce tour : il devait lire {cible} et a dit « {v.Dit} »{(lu is null ? string.Empty : $" (soit {lu})")}. Son écran "
                + "affiche « On réessaie ». Encourage-le en une phrase et, si tu vois l'erreur (soixante-dix et quatre-vingts, un zéro "
                + "oublié, la virgule, le signe moins, le dénominateur), donne un indice de MÉTHODE. NE DONNE JAMAIS la réponse et ne la "
                + "prononce pas : le même nombre reste au tableau." + fin;
        }

        /// <summary>
        /// Le fait du verdict quand il a lu PUIS demandé une autre activité (« vingt-huit. Maintenant on fait une addition »,
        /// contrôle d'Omar #50/#51) : on juge sa lecture et on change d'exercice. Aucun nouveau nombre ne s'affiche.
        /// </summary>
        public static string FaitVerdictPuisChangement(Verdict v)
        {
            const string fin = " Ne parle jamais du micro ni de cet outil à l'élève. " + JargonTechnique.Regle + "]";
            var cible = Affichage(v.Montre.Cible);
            var lu = Canonique(v.Dit) is { } c ? Affichage(c) : null;
            return v.Juste
                ? $"[Fait pour ce tour : il a bien lu le nombre {cible}. Son écran affiche déjà « Bonne lecture » : félicite-le en une "
                    + "phrase, puis commence TOUT DE SUITE l'activité qu'il demande. Aucun nouveau nombre ne s'affiche au tableau." + fin
                : $"[Fait pour ce tour : il devait lire {cible} et a dit « {v.Dit} »{(lu is null ? string.Empty : $" (soit {lu})")}. Son écran "
                    + "affiche « On réessaie ». Dis-lui en une phrase gentille que ce n'était pas tout à fait ça, SANS donner la réponse ni la "
                    + "prononcer, puis commence TOUT DE SUITE l'activité qu'il demande. Aucun nouveau nombre ne s'affiche au tableau." + fin;
        }

        /// <summary>La cible telle qu'on l'écrit pour le professeur : « 12 345 », « 3/4 », « 10^-3 », « 3√2 ».</summary>
        public static string Affichage(string cible) =>
            Regex.Replace(cible.Replace("rac", "√").Replace("x10^", " × 10^"), @"\d{4,}", m =>
                long.Parse(m.Value, CultureInfo.InvariantCulture).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' '));

        // ------------------------------------------------------------------ lire ce qu'il a dit

        /// <summary>L'écriture canonique d'une cible : « +4 » vaut « 4 », « 3,50 » vaut « 3,5 ».</summary>
        public static string Normaliser(string cible)
        {
            var c = cible.Trim();
            if (Regex.IsMatch(c, @"^[+-]?\d+(,\d+)?$")) return Nombre(c.TrimStart('+'));
            var sci = Regex.Match(c, @"^(\d+(?:,\d+)?)x10\^(-?\d+)$");
            if (sci.Success) return $"{Nombre(sci.Groups[1].Value)}x10^{sci.Groups[2].Value}";
            return c;
        }

        private static string Nombre(string texte)
        {
            var negatif = texte.StartsWith('-');
            var valeur = decimal.Parse(texte.TrimStart('-').Replace(',', '.'), CultureInfo.InvariantCulture);
            var ecrit = valeur.ToString("0.#############", CultureInfo.InvariantCulture).Replace('.', ',');
            return negatif && valeur != 0 ? "-" + ecrit : ecrit;
        }

        private enum Sorte { Nombre, Zero, Moins, Plus, Virgule, Sur, Puissance, Carre, Cube, Fois, Racine, Tout, Parenthese, Denominateur }

        private sealed record Jeton(Sorte Sorte, string Valeur = "");

        private static readonly Dictionary<string, long> Unites = new()
        {
            ["zero"] = 0, ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6, ["sept"] = 7,
            ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11, ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14,
            ["quinze"] = 15, ["seize"] = 16,
        };

        private static readonly Dictionary<string, long> Dizaines = new()
        {
            ["vingt"] = 20, ["vingts"] = 20, ["trente"] = 30, ["quarante"] = 40, ["cinquante"] = 50, ["soixante"] = 60,
        };

        private static readonly Dictionary<string, long> Echelles = new()
        {
            ["cent"] = 100, ["cents"] = 100, ["mille"] = 1_000, ["million"] = 1_000_000, ["millions"] = 1_000_000,
            ["milliard"] = 1_000_000_000, ["milliards"] = 1_000_000_000,
        };

        private static bool EstMotNombre(string m) => Unites.ContainsKey(m) || Dizaines.ContainsKey(m) || Echelles.ContainsKey(m);

        /// <summary>« quatre cent quatre-vingts » → 480 ; null si ce n'est pas une suite de mots de nombre.</summary>
        private static long? Entier(IReadOnlyList<string> mots)
        {
            if (mots.Count == 0) return null;
            long total = 0, groupe = 0, petit = 0;
            foreach (var m in mots)
            {
                if (m == "et") continue;
                if (Unites.TryGetValue(m, out var u)) petit += u;
                else if (m is "vingt" or "vingts") petit = petit is > 1 and < 10 ? petit * 20 : petit + 20;
                else if (Dizaines.TryGetValue(m, out var d)) petit += d;
                else if (m is "cent" or "cents") { groupe += (petit == 0 ? 1 : petit) * 100; petit = 0; }
                else if (Echelles.TryGetValue(m, out var e))
                {
                    var facteur = groupe + petit;
                    total += (facteur == 0 ? 1 : facteur) * e;
                    groupe = 0;
                    petit = 0;
                }
                else return null;
            }
            return total + groupe + petit;
        }

        /// <summary>« huitièmes » → 8, « demi » → 2 ; null sinon.</summary>
        private static long? Denominateur(string mot)
        {
            switch (mot)
            {
                case "demi" or "demis": return 2;
                case "tiers": return 3;
                case "quart" or "quarts": return 4;
            }
            var m = Regex.Match(mot, @"^(.+?)iemes?$");
            if (!m.Success) return null;
            var base_ = m.Groups[1].Value;
            if (base_ == "un") return null; // « unième » seul n'est pas un dénominateur
            if (base_.EndsWith("qu")) base_ = base_[..^1];
            if (base_ == "neuv") base_ = "neuf";
            foreach (var essai in new[] { base_, base_ + "e" })
            {
                if (Entier(essai.Split('-', StringSplitOptions.RemoveEmptyEntries)) is long n && n > 1) return n;
            }
            return null;
        }

        private static string SansAccents(string texte)
        {
            var decompose = texte.Normalize(NormalizationForm.FormD);
            return new string(decompose.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
                .Normalize(NormalizationForm.FormC);
        }

        private static readonly Dictionary<char, char> Exposants = new()
        {
            ['⁰'] = '0', ['¹'] = '1', ['²'] = '2', ['³'] = '3', ['⁴'] = '4', ['⁵'] = '5', ['⁶'] = '6', ['⁷'] = '7', ['⁸'] = '8', ['⁹'] = '9', ['⁻'] = '-',
        };

        private static List<Jeton> Jetons(string dit)
        {
            var t = dit.ToLowerInvariant()
                .Replace('−', '-').Replace('–', '-').Replace('×', 'x').Replace('*', 'x').Replace('·', 'x')
                .Replace('’', '\'').Replace("√", " racine ");

            // Les exposants écrits en petit (10³, 10⁻²) deviennent « ^3 », « ^-2 ».
            t = Regex.Replace(t, "[⁰¹²³⁴⁵⁶⁷⁸⁹⁻]+", m => "^" + new string(m.Value.Select(c => Exposants[c]).ToArray()));
            t = SansAccents(t);

            // « 1 200 000 » : les espaces entre les groupes de trois chiffres tombent.
            t = Regex.Replace(t, @"(?<=\d)[   ](?=\d{3}(?!\d))", string.Empty);
            // « soixante-treize » : le trait d'union entre deux mots les sépare.
            t = Regex.Replace(t, @"(?<=[a-z])-(?=[a-z])", " ");

            var bruts = Regex.Matches(t, @"\d+(?:[.,]\d+)?|[a-z]+|[\^/()+-]").Select(m => m.Value).ToList();
            var jetons = new List<Jeton>();
            var mots = new List<string>();

            void Vider()
            {
                if (mots.Count == 0) return;
                // « et » en fin de suite n'appartient pas au nombre.
                while (mots.Count > 0 && mots[^1] == "et") mots.RemoveAt(mots.Count - 1);
                if (mots.Count > 0 && Entier(mots) is long n) jetons.Add(new Jeton(Sorte.Nombre, n.ToString(CultureInfo.InvariantCulture)));
                mots.Clear();
            }

            for (var i = 0; i < bruts.Count; i++)
            {
                var b = bruts[i];
                if (b == "zero")
                {
                    Vider();
                    jetons.Add(new Jeton(Sorte.Zero, "0"));
                    continue;
                }
                if (EstMotNombre(b) || (b == "et" && mots.Count > 0))
                {
                    // « deux cinq » (des chiffres dits un à un, après la virgule surtout) : deux unités de suite ne font
                    // pas un nombre, la seconde en commence un autre. « dix-sept », « vingt-cinq » restent entiers.
                    static bool EstUnite(string m) => Unites.TryGetValue(m, out var v) && v is >= 1 and <= 16 and not 10;
                    if (EstUnite(b) && mots.Count > 0 && EstUnite(mots[^1])) Vider();
                    mots.Add(b);
                    continue;
                }
                Vider();

                if (char.IsDigit(b[0])) { jetons.Add(new Jeton(Sorte.Nombre, b.Replace('.', ','))); continue; }
                if (Denominateur(b) is long den) { jetons.Add(new Jeton(Sorte.Denominateur, den.ToString(CultureInfo.InvariantCulture))); continue; }

                var sorte = b switch
                {
                    "moins" or "-" => Sorte.Moins,
                    "plus" or "+" => Sorte.Plus,
                    "virgule" => Sorte.Virgule,
                    "sur" or "/" => Sorte.Sur,
                    "puissance" or "exposant" or "^" => Sorte.Puissance,
                    "carre" => Sorte.Carre,
                    "cube" => Sorte.Cube,
                    "fois" or "x" => Sorte.Fois,
                    "racine" => Sorte.Racine,
                    "tout" => Sorte.Tout,
                    "(" => Sorte.Parenthese,
                    _ => (Sorte?)null,
                };
                if (sorte is { } s) jetons.Add(new Jeton(s));
            }
            Vider();
            return jetons;
        }

        /// <summary>
        /// CE QU'IL A LU, EN ÉCRITURE CANONIQUE : « 73 », « 3,25 », « -5 », « 3/4 », « 10^-3 », « (-2)^2 », « 2,5x10^4 »,
        /// « 3rac2 » ; null si ce n'est pas une lecture de nombre (« je sais pas »).
        /// </summary>
        public static string? Canonique(string? dit)
        {
            if (string.IsNullOrWhiteSpace(dit)) return null;
            var j = Jetons(dit);
            if (j.Count == 0) return null;

            var i = 0;
            Jeton? Voir(int k = 0) => i + k < j.Count ? j[i + k] : null;
            bool Est(Sorte s, int k = 0) => Voir(k)?.Sorte == s;

            var parenthese = false;
            if (Est(Sorte.Parenthese)) { parenthese = true; i++; }
            var moins = false;
            if (Est(Sorte.Moins)) { moins = true; i++; }
            else if (Est(Sorte.Plus)) i++;

            // « racine carrée de deux » (le coefficient vient plus bas : « trois racine de deux »)
            if (Est(Sorte.Racine) && Est(Sorte.Nombre, 1))
            {
                return Fin(i + 2, j) ? $"{(moins ? "-" : string.Empty)}rac{j[i + 1].Valeur}" : null;
            }

            // Le premier nombre : un entier, ou un décimal (« trois virgule zéro huit »).
            if (!(Est(Sorte.Nombre) || Est(Sorte.Zero))) return null;
            var nombre = Voir()!.Valeur;
            i++;
            if (Est(Sorte.Virgule))
            {
                i++;
                var decimales = new StringBuilder();
                while (Est(Sorte.Nombre) || Est(Sorte.Zero)) { decimales.Append(Voir()!.Valeur); i++; }
                if (decimales.Length == 0) return null;
                nombre = $"{nombre},{decimales}";
            }
            nombre = Nombre(nombre);
            var signe = moins ? "-" : string.Empty;

            // Rien après : un nombre.
            if (i >= j.Count) return moins && nombre != "0" ? "-" + nombre : nombre;

            // « trois quarts », « cinq huitièmes »
            if (Est(Sorte.Denominateur) && Fin(i + 1, j)) return $"{signe}{nombre}/{Voir()!.Valeur}";

            // « trois sur quatre »
            if (Est(Sorte.Sur) && Est(Sorte.Nombre, 1) && Fin(i + 2, j)) return $"{signe}{nombre}/{j[i + 1].Valeur}";

            // « trois racine (carrée) de deux »
            if (Est(Sorte.Racine) && Est(Sorte.Nombre, 1) && Fin(i + 2, j)) return $"{signe}{nombre}rac{j[i + 1].Valeur}";

            // « deux virgule cinq fois dix puissance quatre »
            if (Est(Sorte.Fois) && Voir(1) is { Sorte: Sorte.Nombre, Valeur: "10" } && Est(Sorte.Puissance, 2))
            {
                i += 3;
                var exposant = Exposant(ref i, j);
                return exposant is not null && Fin(i, j) ? $"{signe}{nombre}x10^{exposant}" : null;
            }

            // Une puissance : « dix puissance moins trois », « trois au carré », « moins deux, le tout au carré ».
            var tout = parenthese;
            if (Est(Sorte.Tout)) { tout = true; i++; }
            string? exp = null;
            if (Est(Sorte.Carre)) { exp = "2"; i++; }
            else if (Est(Sorte.Cube)) { exp = "3"; i++; }
            else if (Est(Sorte.Puissance)) { i++; exp = Exposant(ref i, j); }
            if (Est(Sorte.Tout)) { tout = true; i++; if (Est(Sorte.Carre)) { exp = "2"; i++; } else if (Est(Sorte.Cube)) { exp = "3"; i++; } }
            if (exp is null || !Fin(i, j)) return null;

            return tout && moins ? $"(-{nombre})^{exp}" : $"{signe}{nombre}^{exp}";
        }

        private static string? Exposant(ref int i, List<Jeton> j)
        {
            var moins = false;
            if (i < j.Count && j[i].Sorte == Sorte.Moins) { moins = true; i++; }
            if (i >= j.Count || j[i].Sorte is not (Sorte.Nombre or Sorte.Zero)) return null;
            var valeur = j[i].Valeur;
            i++;
            return (moins ? "-" : string.Empty) + valeur;
        }

        /// <summary>Plus rien d'utile après : la lecture est entière.</summary>
        private static bool Fin(int i, List<Jeton> j) => i >= j.Count;

        private static int Indice(string graine, int taille)
        {
            var somme = 0;
            foreach (var c in graine) somme = unchecked(somme * 31 + c);
            return (int)((uint)somme % (uint)taille);
        }
    }

    /// <summary>
    /// LE FILTRE DE LA LECTURE DES NOMBRES : la réponse du professeur est retenue en entier, puis remise en ordre par
    /// <see cref="LectureDesNombres.Reecrire"/> (sa phrase, la consigne du code, la balise). Même principe que
    /// FiltreMotsVerifies pour les syllabes, sans liste ni micro.
    /// </summary>
    public sealed class FiltreLectureNombres(LectureDesNombres.Tour tour)
    {
        private string _tampon = string.Empty;

        /// <summary>Le texte brut du professeur, tel qu'il l'a écrit (pour le journal de TEST seulement).</summary>
        public string Brut { get; private set; } = string.Empty;

        /// <summary>Les règles qui ont retiré quelque chose de sa réponse.</summary>
        public List<string> Retraits { get; } = [];

        /// <summary>Il avait écrit une phrase, et il n'en reste rien : seule la consigne du code part (défaut e5, Omar).</summary>
        public bool PhraseRetiree { get; private set; }

        public string Passer(string fragment)
        {
            _tampon += fragment;
            return string.Empty;
        }

        public string Terminer()
        {
            Brut = _tampon;
            var texte = LectureDesNombres.Reecrire(_tampon, tour, Retraits);
            // Sa seule phrase était sa propre consigne (« Lis ce nombre à voix haute ! ») : c'est voulu, celle du code la remplace.
            PhraseRetiree = LectureDesNombres.AUnePhrase(_tampon) && !LectureDesNombres.AUnePhrase(SansConsigneNiBalise(texte))
                && Retraits.Any(r => r is not ("tableau" or "consigne"));
            _tampon = string.Empty;
            return texte;
        }

        private string SansConsigneNiBalise(string texte) =>
            texte.Replace(LectureDesNombres.Consigne(tour), string.Empty).Replace(LectureDesNombres.Balise(tour.Element), string.Empty);

        public string Corriger(string texte) => LectureDesNombres.Reecrire(texte, tour);
    }

    /// <summary>
    /// LA BALISE D'UN NOMBRE HORS DE LA LECTURE DES NOMBRES (défaut e8, Omar, 20/11/2026) : après « on compte jusqu'à 100 »,
    /// l'exercice change, mais le modèle recopie l'historique et écrit lui-même « Regarde le tableau, lis ce nombre à voix
    /// haute : [LECTURE_IMAGE:nombre]52[/LECTURE_IMAGE] ». L'écran affichait le 52, et au tour suivant la lecture des
    /// nombres reprenait (le dernier message montrait un nombre). Hors de l'exercice, seul le code pose cette balise : elle
    /// part, avec la consigne orpheline qui l'annonçait.
    ///
    /// Deux allures. JUSTE APRÈS une lecture (un nombre montré dans les derniers messages), la réponse est retenue en
    /// entier, comme pendant l'exercice, pour que la consigne orpheline parte aussi de l'écran et de la voix. Plus loin, le
    /// texte coule sans retard : seule une balise en cours d'écriture est retenue, puis retirée. Le texte gardé en base est
    /// nettoyé dans les deux cas (<see cref="LectureDesNombres.SansBaliseHorsLecture"/>).
    /// </summary>
    public sealed class FiltreBaliseNombreHorsLecture(bool retenirTout)
    {
        private const string Ouverture = "[LECTURE_IMAGE:nombre]";
        private string _tampon = string.Empty;

        /// <summary>Une balise a été retirée (pour le journal).</summary>
        public bool ARetire { get; private set; }

        public string Passer(string fragment)
        {
            _tampon += fragment;
            if (retenirTout) return string.Empty;

            var propre = LectureDesNombres.SansBaliseComplete(_tampon);
            if (propre.Length != _tampon.Length) ARetire = true;
            _tampon = propre;

            // Une balise ouverte et pas encore fermée, ou un « [LECT » qui peut en devenir une : retenue à partir de là.
            var coupe = _tampon.LastIndexOf(Ouverture, StringComparison.OrdinalIgnoreCase);
            if (coupe < 0)
            {
                var crochet = _tampon.LastIndexOf('[');
                coupe = crochet >= 0 && _tampon.Length - crochet < Ouverture.Length
                    && Ouverture.StartsWith(_tampon[crochet..], StringComparison.OrdinalIgnoreCase) ? crochet : _tampon.Length;
            }

            var sortie = _tampon[..coupe];
            _tampon = _tampon[coupe..];
            return sortie;
        }

        public string Terminer()
        {
            var texte = retenirTout
                ? LectureDesNombres.SansBaliseHorsLecture(_tampon, out var retire)
                : LectureDesNombres.SansBaliseComplete(_tampon, ouverteAussi: true, out retire);
            ARetire |= retire;
            _tampon = string.Empty;
            return texte;
        }
    }
}
