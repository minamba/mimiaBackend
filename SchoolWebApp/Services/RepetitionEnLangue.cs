using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SchoolWebApp.Api.Services.Prompts;
using SchoolWebApp.Api.Services.Voix;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// LE MOT DIT EN FRANÇAIS N'EST PAS UNE RÉPÉTITION — signalement #57 (Adam, 21/11/2026, jeu CP, Anglais) : « Écoute la
    /// deuxième couleur : Blue. Tu répètes ? » — « Bleu! » — « Oui, parfait Adam ! » ; puis « Écoute : One, ça veut dire un.
    /// Tu répètes ? » — « Un! » — « Oui, parfait Adam ! ». L'enfant croit savoir le mot anglais alors qu'il a répondu en
    /// français.
    ///
    /// POURQUOI LE PROFESSEUR SE TROMPE : le socle commun des langues (<c>ReponseDansLaLangueEtudiee</c>) lui dit, à juste
    /// titre pour le micro, qu'un mot qui « ressemble à un mot français » est sans doute le mot attendu mal transcrit. Il en
    /// déduit que « Bleu » est « Blue ». Une règle relue plus haut ne vaut pas un fait au tour même (#29, #46, #52) : c'est
    /// donc le CODE qui dit au professeur, à ce tour, que la réponse est la TRADUCTION FRANÇAISE du mot à répéter.
    ///
    /// PORTÉE : GÉNÉRIQUE, tous les professeurs de langue (règle de Min's du 21/11/2026). Le défaut n'a rien de propre à
    /// Marine : n'importe quel professeur de langue peut demander « tu répètes ? » et recevoir la traduction. Rattaché à la
    /// liste des langues (<see cref="PromptsPedagogiques.LangueDe"/>), comme le socle commun : une langue ajoutée en hérite.
    /// Le français n'est pas concerné (il n'a pas de « traduction »).
    ///
    /// D'OÙ VIENT LA TRADUCTION FRANÇAISE :
    ///   - du professeur lui-même, dans ses derniers messages : « [EN]One[/EN], ça veut dire un », « bleu se dit
    ///     [EN]blue[/EN] » — vaut pour toutes les langues ;
    ///   - en anglais, de la table des mots illustrés (<see cref="MotsAnglaisIllustres"/>, 242 mots du CP au CM2) et de
    ///     quelques mots de début d'apprentissage qui n'y sont pas (nombres, salutations, consignes de classe).
    ///
    /// PRUDENT PAR CONSTRUCTION, parce qu'un refus à tort coûte plus cher qu'une validation : rien n'est dit si sa réponse
    /// contient le mot attendu, ou un mot qui en est à une lettre près (« blu », un essai dans la langue), si le mot est le
    /// même dans les deux langues (« orange », « six »), si le professeur demandait AUSSI le sens (« tu sais ce que ça veut
    /// dire ? » : là, « bleu » est juste), ou si la réponse contient autre chose que la traduction.
    /// </summary>
    public static partial class RepetitionEnLangue
    {
        /// <summary>Ce que le professeur demandait.</summary>
        public enum Demande
        {
            /// <summary>« [EN]Blue[/EN]. Tu répètes ? »</summary>
            Repeter,

            /// <summary>« Comment on dit bleu en anglais ? » (anglais seulement, mots de la table).</summary>
            Traduire,
        }

        /// <summary>Le tour jugé : la demande, la langue, le mot attendu, ce qu'il a dit et la traduction reconnue.</summary>
        public sealed record Tour(Demande Nature, string Balise, string Langue, string Attendu, string Dit, string Francais);

        /// <summary>Les messages du professeur relus pour y trouver une traduction (« ça veut dire un »).</summary>
        public const int MessagesLus = 4;

        /// <summary>Au-delà, ce n'est plus une répétition : une phrase à lui, qu'on ne juge pas ici.</summary>
        private const int MotsMax = 8;

        private static readonly Dictionary<string, string> Langues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["EN"] = "anglais", ["ES"] = "espagnol", ["DE"] = "allemand", ["IT"] = "italien", ["ZH"] = "chinois", ["AR"] = "arabe",
        };

        /// <summary>
        /// LES MOTS ANGLAIS DU DÉBUT QUI NE SONT PAS DANS LA TABLE DES IMAGES : les nombres qui n'y sont pas (« two »,
        /// « three »…), les salutations et les consignes de classe que Marine fait répéter au CP (« stand up », « repeat »).
        /// Plusieurs traductions possibles : séparées par « | ».
        /// </summary>
        private static readonly Dictionary<string, string> AnglaisDuDebut = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zero"] = "zéro", ["one"] = "un|une", ["two"] = "deux", ["three"] = "trois", ["four"] = "quatre", ["five"] = "cinq",
            ["seven"] = "sept", ["eight"] = "huit", ["nine"] = "neuf", ["ten"] = "dix", ["eleven"] = "onze", ["twelve"] = "douze",
            ["thirteen"] = "treize", ["fourteen"] = "quatorze", ["fifteen"] = "quinze", ["sixteen"] = "seize",
            ["seventeen"] = "dix-sept", ["eighteen"] = "dix-huit", ["nineteen"] = "dix-neuf", ["twenty"] = "vingt",
            ["hello"] = "bonjour|salut", ["hi"] = "salut|bonjour", ["good morning"] = "bonjour", ["goodbye"] = "au revoir",
            ["bye"] = "au revoir|salut", ["good night"] = "bonne nuit", ["yes"] = "oui", ["no"] = "non", ["please"] = "s'il te plaît|s'il vous plaît",
            ["thank you"] = "merci", ["thanks"] = "merci", ["stand up"] = "lève-toi|levez-vous|debout",
            ["sit down"] = "assieds-toi|asseyez-vous|assis", ["repeat"] = "répète|répétez", ["listen"] = "écoute|écoutez",
            ["look"] = "regarde|regardez", ["mother"] = "mère|maman", ["father"] = "père|papa", ["boy"] = "garçon", ["girl"] = "fille",
            ["school"] = "école", ["teacher"] = "maîtresse|maître|professeur", ["friend"] = "ami|amie|copain|copine",
            ["orange"] = "orange", ["sun"] = "soleil", ["moon"] = "lune", ["star"] = "étoile", ["tree"] = "arbre", ["flower"] = "fleur",
        };

        /// <summary>
        /// Ce qui ne compte pas dans une réponse : les hésitations, « c'est », « je dis », puis les articles. Retirés des deux
        /// côtés (sa réponse, la traduction) avant de comparer, en deux temps : si tout part, on garde ce qu'il y avait
        /// avant (« c'est un » → « un » ; « Oui » reste « oui »).
        /// </summary>
        private static readonly HashSet<string> Hesitations = new(StringComparer.Ordinal)
        {
            "euh", "heu", "hum", "ben", "bah", "alors", "ok", "okay", "d", "accord", "voila", "donc", "bon", "bien", "oui",
            "c", "est", "ca", "se", "dit", "je", "dis", "repete", "moi", "aussi",
        };

        private static readonly HashSet<string> Articles = new(StringComparer.Ordinal)
        {
            "le", "la", "les", "l", "un", "une", "des", "du", "de",
        };

        private static bool Remplissage(string mot) => Hesitations.Contains(mot) || Articles.Contains(mot);

        /// <summary>Les articles et consignes de la langue, retirés du mot attendu (« the cat » → cat).</summary>
        private static readonly HashSet<string> ArticlesEtrangers = new(StringComparer.Ordinal)
        {
            "the", "a", "an", "el", "los", "las", "una", "der", "die", "das", "ein", "eine", "il", "lo", "gli", "uno",
        };

        /// <summary>Une demande de répétition, en français ou dans la langue. « Je répète » (le professeur) n'en est pas une.</summary>
        [GeneratedRegex(
            @"\btu\s+(?:me\s+)?(?:le\s+|la\s+|les\s+|l['’]\s*)?(?:répètes|redis|répéter|redire)\b"
            + @"|(?<!\b(?:je|j['’]|que\s+je)\s*(?:(?:te|vous|le|la|les|l['’])\s*)*)\brépète(?:s|z)?\b"
            + @"|(?<!\bje\s+(?:vais|peux|veux|voudrais)\s+(?:(?:te|vous|le|la|les|l['’])\s*)*)\brépéter\b|\bredis[- ](?:le|la|les|moi)\b|\bredis\s+après\s+moi\b"
            + @"|\bà\s+toi\s*(?:[!.]|de\s+(?:le\s+|la\s+|les\s+)?(?:dire|répéter|redire|prononcer))"
            + @"|\bà\s+ton\s+tour\b|\bessaie\s+de\s+le\s+(?:dire|répéter|redire|prononcer)\b|\bdis[- ](?:le|la)\b|\bcomme\s+moi\b"
            + @"|\brepeat\b|\byour\s+turn\b|\brepite\b|\brepítelo\b|\bripeti\b|\bwiederhol|\bsprich\s+nach\b"
            // L'ÉCOUTE DES MOTS (retest KO #57, 21/11/2026, 15h05) : « [EN]Two[/EN]. Qu'as-tu entendu ? » — « Deux! » validé.
            // Là, l'enfant doit dire le mot ENTENDU (« two »), pas son sens : c'est une répétition. La question est celle que
            // le code écrit après chaque mot dicté (RegleVerificationSon.QuestionApresLeSon), et ses variantes.
            + @"|\bqu['’]as[- ]tu\s+entendu|\bqu['’]est[- ]ce\s+que\s+tu\s+(?:as\s+)?entend|\bce\s+que\s+tu\s+(?:as\s+)?entend(?:s|u)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DemandeDeRepetition();

        /// <summary>
        /// LE SENS EST AUSSI DEMANDÉ : « Blue, tu sais ce que ça veut dire ? ». Là, « bleu » est la bonne réponse : on ne
        /// juge rien.
        /// </summary>
        [GeneratedRegex(
            // « c'est pile ce que ça veut dire, bravo ! » est une AFFIRMATION, pas une question sur le sens (bout en bout de la
            // tâche 1600 : le juge se taisait, et l'essai suivant n'était plus compté).
            @"veut\s+dire\s+quoi|(?<!c['’]est\s+(?:\p{L}+\s+){0,2}ce\s+)que\s+(?:ça|ca|cela)\s+veut\s+dire|qu['’]est[- ]ce\s+que\s+(?:ça|ca|c['’]est|cela)|c['’]est\s+quoi"
            + @"|que\s+signifie|ça\s+signifie\s+quoi|en\s+français\s*\?|traduis|traduire|\bdis[- ]?(?:le|la|moi)?\s+en\s+français|comment\s+(?:on\s+dit|dit[- ]on|tu\s+dis)[^?]*\ben\s+français"
            + @"|quel(?:le)?\s+couleur|quel\s+(?:animal|nombre|chiffre|mot)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex SensDemande();

        /// <summary>Une consigne seule, dans la langue : « [EN]Repeat![/EN] » n'est pas le mot à répéter.</summary>
        [GeneratedRegex(
            @"^(?:repeat(?:\s+after\s+me)?|listen(?:\s+and\s+repeat)?|say\s+it(?:\s+again)?|again|your\s+turn|now\s+you|repite|ripeti|wiederhole|sprich\s+nach)\b[\s:,!.]*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ConsigneEnTete();

        /// <summary>« Comment on dit bleu en anglais ? » — le mot français est écrit en clair (le socle commun l'exige).</summary>
        [GeneratedRegex(
            @"comment\s+(?:on\s+dit|dit[- ]on|tu\s+dis|dirais[- ]tu|se\s+dit|on\s+pourrait\s+dire)\s+(?:le\s+mot\s+)?[«""]?\s*(?<fr>[^?«»""\[\n.!:;]{1,40}?)\s*[»""]?\s*,?\s+en\s+anglais\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DemandeDeTraduction();

        [GeneratedRegex(@"\[(?<b>EN|ES|DE|IT|ZH|AR)\](?<mot>.*?)\[/\k<b>\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Balise();

        [GeneratedRegex(@"\[ARDOISE\].*?\[/ARDOISE\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Tableau();

        /// <summary>Un mot cité entre guillemets (« Blue ») : retenu seulement s'il est dans la table anglaise.</summary>
        [GeneratedRegex(@"[«""]\s*(?<mot>[A-Za-z][A-Za-z' -]{0,30}?)\s*[!.?]?\s*[»""]")]
        private static partial Regex Guillemets();

        /// <summary>Le professeur l'a traduit après le mot : « [EN]One[/EN], ça veut dire un ».</summary>
        private static Regex TraductionApres(string balise, string mot) => new(
            $@"\[{balise}\][^\[\]]*?{Regex.Escape(mot)}\s*[!.?,]*\s*\[/{balise}\]\s*[»""]?\s*[,:;–-]?\s*(?:,\s*)?(?:(?:ça|ca|cela)\s+)?"
            + @"(?:(?<fort>veut\s+dire|signifie|se\s+traduit\s+par|en\s+français\s*,?\s*(?:c['’]est|ça\s+(?:donne|fait)))|c['’]est(?:\s+bien)?)\s*:?\s*[«""]?\s*"
            + @"(?<fr>[^.,!?;:»""\n\[()]{1,40})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Le professeur l'a traduit avant le mot : « bleu se dit [EN]blue[/EN] », « En anglais, bleu c'est [EN]blue[/EN] ».</summary>
        private static Regex TraductionAvant(string balise, string mot) => new(
            @"(?<fr>[\p{L}'’-]+(?:\s+[\p{L}'’-]+){0,2})\s*[»""]?\s*,?\s*(?:,?\s*en\s+\p{L}+\s*,?\s*)?(?:(?:ça|ca|cela)\s+)?"
            + $@"(?:(?<fort>se\s+dit)|c['’]est|ça\s+donne|devient)\s*(?:en\s+\p{{L}}+\s*)?[:,]?\s*\[{balise}\][^\[\]]*?{Regex.Escape(mot)}\s*[!.?,]*\s*\[/{balise}\]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Le professeur enseigne-t-il une langue étrangère ?</summary>
        public static bool Concerne(string? agentSlug) =>
            PromptsPedagogiques.LangueDe(agentSlug) is { } langue && !langue.Equals("fr", StringComparison.OrdinalIgnoreCase)
            && Langues.ContainsKey(langue);

        /// <summary>
        /// Le tour, si l'élève a répondu par la traduction française du mot demandé ; sinon null.
        /// <paramref name="messagesDuProfesseur"/> : ses derniers messages, du plus ancien au plus récent (le dernier est
        /// celui auquel l'élève répond).
        /// </summary>
        public static Tour? Juger(string? agentSlug, IReadOnlyList<string?> messagesDuProfesseur, string? messageEleve)
        {
            // Une réponse déjà jugée et gardée sous sa forme marquée (« J'ai répété : Bleu! ») se relit sans la marque.
            messageEleve = SansLaMarque(messageEleve);
            if (!Concerne(agentSlug) || string.IsNullOrWhiteSpace(messageEleve) || messagesDuProfesseur.Count == 0) return null;
            var dernier = messagesDuProfesseur[^1];
            if (string.IsNullOrWhiteSpace(dernier)) return null;

            var balise = PromptsPedagogiques.LangueDe(agentSlug)!.ToUpperInvariant();
            var langue = Langues[balise];

            var dit = Regex.Replace(messageEleve, @"\[[^\]]*\]", " ").Trim();
            if (dit.Length == 0 || dit.Contains('?')) return null;
            var motsDits = Mots(dit);
            if (motsDits.Count is 0 or > MotsMax) return null;
            var contenuDit = SansRemplissage(motsDits);

            var texte = Tableau().Replace(dernier, " ");
            if (SensDemande().IsMatch(SansBalises(texte))) return null;

            var demande = DemandeDeRepetition().Match(texte);
            if (demande.Success)
            {
                var cibles = Cibles(texte, balise, demande.Index);
                if (cibles.Count == 0) return null;

                // Il a dit (ou essayé de dire) un des mots attendus : ce n'est pas une réponse en français.
                if (cibles.Any(c => EssaiDansLaLangue(motsDits, c.Mot))) return null;

                foreach (var (mot, brut) in cibles)
                {
                    foreach (var (francais, fiable) in Traductions(balise, mot, brut, messagesDuProfesseur))
                    {
                        // « Oui, c'est [EN]blue[/EN] » ne dit pas que « oui » traduit « blue » : une traduction lue après
                        // « c'est » doit avoir un vrai mot. Après « veut dire », « se dit » ou dans la table, « un » suffit.
                        if (!fiable && Mots(francais).All(Remplissage)) continue;
                        var contenuFr = SansRemplissage(Mots(francais));
                        if (contenuFr.Count == 0 || contenuFr.SequenceEqual(MotsDeLaCible(mot))) continue; // même mot dans les deux langues
                        if (contenuFr.ToHashSet().SetEquals(contenuDit))
                        {
                            return new Tour(Demande.Repeter, balise, langue, brut, Court(dit), francais.Trim());
                        }
                    }
                }

                return null;
            }

            // « Comment on dit bleu en anglais ? » — « bleu ». Anglais seulement, et seulement pour un mot de la table dont
            // la traduction est différente : on sait alors, sans deviner, que la réponse est le mot français redit.
            if (balise == "EN" && DemandeDeTraduction().Match(SansBalises(texte)) is { Success: true } traduction)
            {
                var fr = traduction.Groups["fr"].Value.Trim();
                var contenuFr = SansRemplissage(Mots(fr));
                if (contenuFr.Count == 0 || !contenuFr.ToHashSet().SetEquals(contenuDit)) return null;

                var anglais = MotsAnglaisDe(fr);
                if (anglais.Count == 0 || anglais.Any(a => a.SequenceEqual(contenuFr))) return null;

                return new Tour(Demande.Traduire, balise, langue, fr, Court(dit), fr);
            }

            return null;
        }

        /// <summary>
        /// LE MÊME JUGE, POUR LE MICRO — retest KO #57 (Adam, 21/11/2026, 18h20 et 18h21) : « [EN]Blue[/EN]. Qu'as-tu
        /// entendu ? », il DIT « bleu » : verdict du relais <c>{"texte":"J'ai entendu le mot bleu.","juste":true}</c>, et
        /// « Bravo Adam, bien entendu « Blue » ! ». Au clavier, « Bleu! » était refusé. La différence : au micro, après
        /// l'écriture, le relais compare les SONS — et la prononciation ANGLAISE demandée pour « bleu » (lu comme un mot
        /// anglais) tombe sur celle de « blue ». Toute traduction française qui se lit comme le mot anglais passe ainsi
        /// (bleu/blue, et les mêmes risques pour zèbre/zebra, tigre/tiger…). Le son ne peut pas trancher : c'est le code qui
        /// reconnaît la traduction, AVANT le son, avec les mêmes garde-fous que <see cref="Juger"/> : rien si c'est un essai
        /// dans la langue (le mot, ou une lettre près : « blu »), rien si le mot est le même dans les deux langues
        /// (« orange », « violet », « rose »), rien si sa réponse contient autre chose que la traduction.
        ///
        /// <paramref name="langue"/> : la langue des sons (« en »), ou la balise (« EN »). <paramref name="attendu"/> : le
        /// mot dit par le professeur. <paramref name="messagesDuProfesseur"/> : ses derniers messages (« ça veut dire un »).
        /// Rend la traduction française reconnue, ou null.
        /// </summary>
        public static string? TraductionDite(string? langue, string? attendu, string? dit, IReadOnlyList<string?> messagesDuProfesseur)
        {
            if (string.IsNullOrWhiteSpace(langue) || string.IsNullOrWhiteSpace(attendu) || string.IsNullOrWhiteSpace(dit)) return null;
            var balise = langue.Trim().ToUpperInvariant();
            if (balise == "FR" || !Langues.ContainsKey(balise)) return null;

            var motsDits = Mots(dit);
            if (motsDits.Count is 0 or > MotsMax) return null;
            var contenuDit = SansRemplissage(motsDits);

            var mot = ConsigneEnTete().Replace(attendu.Trim(), string.Empty).Trim().TrimEnd('!', '.', '?', ',', ';', ':').Trim();
            if (mot.Length == 0 || EssaiDansLaLangue(motsDits, mot)) return null;

            foreach (var (francais, fiable) in Traductions(balise, mot, mot, messagesDuProfesseur))
            {
                if (!fiable && Mots(francais).All(Remplissage)) continue;
                var contenuFr = SansRemplissage(Mots(francais));
                if (contenuFr.Count == 0 || contenuFr.SequenceEqual(MotsDeLaCible(mot))) continue; // même mot dans les deux langues
                if (contenuFr.ToHashSet().SetEquals(contenuDit)) return francais.Trim();
            }

            return null;
        }

        /// <summary>
        /// LA TRADUCTION TAPÉE EST UN ESSAI RATÉ, AVEC SON VERDICT À L'ÉCRAN — contrôle d'Omar sur le #57 v2, accord de Min's
        /// (21/11/2026, 16 h : « oui faut modifier l'écran pour compter 3 essais au lieu de 5 »). Tapée au clavier, « Bleu! »
        /// pour « [EN]Blue[/EN]. Tu répètes ? » n'avait aucun verdict : la bulle restait « Bleu! », rien n'était compté, et le
        /// professeur pouvait redemander le même mot sans fin. Elle est désormais jugée au serveur avant d'être enregistrée,
        /// comme une lecture tapée (#49) : la bulle dit « On réessaie », et l'essai compte.
        ///
        /// LE MESSAGE GARDÉ PORTE UNE MARQUE, « J'ai répété : » suivi de ce qu'il a tapé. Un verdict enregistré fait croire au
        /// code qu'un exercice de sons est en cours (<see cref="RegleVerificationSon.EnExerciceDeSons(IEnumerable{ValueTuple{string, bool?}})"/>) :
        /// sans cette marque, une simple répétition hors exercice aurait ouvert, aux tours suivants, les filtres et le juge
        /// des sons. La marque sert aussi à compter les essais (<see cref="EssaisDeSuite"/>). Le professeur lit la réponse
        /// de l'enfant telle quelle, après la marque.
        /// </summary>
        public const string MarqueDeLaReponse = "J'ai répété : ";

        /// <summary>Les essais ratés sur le même mot, au-delà desquels on passe : la règle commune des trois essais (EssaisRates).</summary>
        public const int EssaisMax = EssaisRates.Maximum;

        /// <summary>Ce qui s'enregistre à la place de ce qu'il a tapé, quand sa réponse est jugée : voir <see cref="MarqueDeLaReponse"/>.</summary>
        public static string TexteDeLEleve(string tape) => MarqueDeLaReponse + tape.Trim();

        /// <summary>Une réponse tapée déjà jugée, gardée sous sa forme marquée ?</summary>
        public static bool EstUneReponseJugee(string? contenu) =>
            contenu is not null && contenu.StartsWith(MarqueDeLaReponse, StringComparison.Ordinal);

        private static string? SansLaMarque(string? contenu) =>
            EstUneReponseJugee(contenu) ? contenu![MarqueDeLaReponse.Length..] : contenu;

        /// <summary>
        /// LES ESSAIS RATÉS DE SUITE SUR LE MÊME MOT, celui de ce tour compris. <paramref name="historique"/> : les messages
        /// AVANT le sien, du plus ancien au plus récent. Ne comptent que ses réponses JUGÉES fausses (forme marquée, verdict
        /// faux) au même mot, chacune relue contre la demande du professeur qui la précède. Le décompte s'arrête au premier
        /// message de l'enfant qui n'en est pas une (une bonne réponse, une question, autre chose) ou à un autre mot :
        /// prudent, on ne passe jamais un mot trop tôt.
        /// </summary>
        public static int EssaisDeSuite(string? agentSlug, IReadOnlyList<(string? Role, string? Contenu, bool? SonJuste)> historique, Tour tour)
        {
            var essais = 1;
            for (var i = historique.Count - 1; i >= 0 && essais < EssaisMax; i--)
            {
                var (role, contenu, juste) = historique[i];
                if (role != "user") continue;
                if (juste != false || !EstUneReponseJugee(contenu)) break;

                var demandes = historique.Take(i)
                    .Where(m => m.Role == "assistant" && !EstTechnique(m.Contenu))
                    .Select(m => m.Contenu)
                    .TakeLast(MessagesLus)
                    .ToList();
                var avant = Juger(agentSlug, demandes, contenu);
                if (avant is null || avant.Nature != tour.Nature || !string.Equals(avant.Attendu, tour.Attendu, StringComparison.OrdinalIgnoreCase)) break;
                essais++;
            }

            return essais;
        }

        /// <summary>Un message technique du professeur (fiche, rapport) : il ne pose aucune question.</summary>
        internal static bool EstTechnique(string? contenu) =>
            string.IsNullOrWhiteSpace(contenu)
            || contenu.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
            || contenu.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// LE TROISIÈME ESSAI RATÉ : RÉEXPLIQUER, PUIS PASSER — la règle commune des trois essais (Camara, 07/11/2026 : « au
        /// bout de 3 essais il faut passer à la suivante, en lui disant que c'est pas grave et qu'on reviendra dessus »).
        /// Remplace <see cref="Fait"/> à ce tour : on ne lui redemande plus ce mot. Ni « avec douceur » ni « chaleureusement »
        /// (le modèle recopie ces mots chez l'enfant : « Doux moment »), et pas le nombre d'essais.
        /// </summary>
        public static string FaitDernierEssai(Tour tour) => tour.Nature switch
        {
            Demande.Traduire =>
                $"[Fait pour ce tour, qui l'emporte sur tous les faits précédents : vérifié par l'application, il a encore répondu "
                + $"« {tour.Dit} », le mot FRANÇAIS, à la place du mot en {tour.Langue}, et c'était son dernier essai sur ce mot. Ce n'est "
                + "pas la réponse : ne commence ni par « Oui », ni par « Parfait », ni par « Bravo ». NE LUI REDEMANDE PAS ce mot. Sans le "
                + $"gronder, réexplique-lui en une phrase que « {tour.Attendu} » est le mot français et que tu attendais le mot en "
                + $"{tour.Langue} ; puis dis-lui que ce n'est pas grave, que vous y reviendrez un peu plus tard, et passe à la suite "
                + "avec un autre mot. Ne lui dis pas combien d'essais il a faits.]",
            _ =>
                $"[Fait pour ce tour, qui l'emporte sur tous les faits précédents : vérifié par l'application, il a encore répondu "
                + $"« {tour.Dit} », le mot en FRANÇAIS (« {tour.Francais} »), à la place de [{tour.Balise}]{tour.Attendu}[/{tour.Balise}], "
                + "et c'était son dernier essai sur ce mot. Ce n'est pas la répétition demandée : ne commence ni par « Oui », ni par "
                + "« Parfait », ni par « Bravo ». NE LUI REDEMANDE PAS ce mot. Sans le gronder, réexplique-lui en une phrase la "
                + $"différence : « {tour.Francais} », c'est le mot en français, et en {tour.Langue} on dit "
                + $"[{tour.Balise}]{tour.Attendu}[/{tour.Balise}]. Puis dis-lui que ce n'est pas grave, que vous y reviendrez un peu plus "
                + "tard, et passe à la suite avec un autre mot. Ne lui dis pas combien d'essais il a faits.]",
        };

        /// <summary>Le fait donné au professeur à ce tour.</summary>
        public static string Fait(Tour tour) => tour.Nature switch
        {
            Demande.Traduire =>
                $"[Vérifié par l'application : tu lui as demandé comment on dit « {tour.Attendu} » en {tour.Langue}, et il a "
                + $"répondu « {tour.Dit} » : c'est le mot FRANÇAIS que tu lui as donné, pas le mot en {tour.Langue}. Ce n'est pas "
                + "la réponse : ne commence ni par « Oui », ni par « Parfait », ni par « Bravo », et ne dis pas que c'est juste. "
                + $"Sans le gronder, dis-lui qu'il a redit le mot en français, puis redemande-lui de le dire en {tour.Langue}, sans lui "
                + "donner la réponse.]",
            _ =>
                $"[Vérifié par l'application : tu lui as demandé de répéter [{tour.Balise}]{tour.Attendu}[/{tour.Balise}], et il a "
                + $"répondu « {tour.Dit} » : c'est le mot en FRANÇAIS (« {tour.Francais} »), pas le mot en {tour.Langue}. Ce n'est "
                + "PAS la répétition demandée : ne commence ni par « Oui », ni par « Parfait », ni par « Bravo », ni par « C'est bien ça », et ne dis pas "
                + "que c'est juste. Sans le gronder, dis-lui que ça, c'est le mot en français — il en a bien compris le sens —, puis "
                + $"redis LE MÊME MOT en {tour.Langue}, dans ta balise : [{tour.Balise}]{tour.Attendu}[/{tour.Balise}], et demande-lui "
                + "de le répéter comme toi. NE PASSE PAS à un autre mot à ce tour : aucun autre mot en "
                + $"{tour.Langue} dans ton message, ni « voici le prochain mot », ni « on continue » ; il doit d'abord redire celui-ci.]",
        };

        /// <summary>
        /// Les mots à répéter du dernier message : ceux de la balise de la langue (sans les consignes « Repeat! »), et, en
        /// anglais, un mot de la table cité entre guillemets. Celui qui précède la demande d'abord.
        /// </summary>
        private static List<(string Mot, string Brut)> Cibles(string texte, string balise, int positionDemande)
        {
            var avant = new List<(string, string)>();
            var apres = new List<(string, string)>();

            foreach (Match m in Balise().Matches(texte))
            {
                if (!m.Groups["b"].Value.Equals(balise, StringComparison.OrdinalIgnoreCase)) continue;
                var brut = m.Groups["mot"].Value.Trim();
                var sansConsigne = ConsigneEnTete().Replace(brut, string.Empty).Trim().TrimEnd('!', '.', '?', ',', ';', ':').Trim();
                if (sansConsigne.Length == 0 || Mots(sansConsigne).Count > 6) continue;
                (m.Index < positionDemande ? avant : apres).Add((sansConsigne, sansConsigne));
            }

            if (balise == "EN")
            {
                foreach (Match m in Guillemets().Matches(SansBalises(texte)))
                {
                    var mot = m.Groups["mot"].Value.Trim();
                    if (MotsAnglaisIllustres.Trouve(mot) is null && !AnglaisDuDebut.ContainsKey(mot.ToLowerInvariant())) continue;
                    (m.Index < positionDemande ? avant : apres).Add((mot, mot));
                }
            }

            avant.Reverse();
            return avant.Concat(apres).Take(6).ToList();
        }

        /// <summary>
        /// Les traductions françaises connues d'un mot : celles du professeur, puis la table anglaise. « Fiable » : la table,
        /// ou « veut dire / signifie / se dit » ; pas « c'est », qui peut suivre n'importe quoi (« Oui, c'est … »).
        /// </summary>
        private static IEnumerable<(string Francais, bool Fiable)> Traductions(string balise, string mot, string brut, IReadOnlyList<string?> messages)
        {
            foreach (var message in messages)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;

                foreach (Match m in TraductionApres(balise, brut).Matches(message))
                {
                    yield return (m.Groups["fr"].Value, m.Groups["fort"].Success);
                }

                foreach (Match m in TraductionAvant(balise, brut).Matches(message))
                {
                    // « la deuxième couleur bleu se dit » : le dernier mot, puis les deux, puis les trois.
                    var mots = m.Groups["fr"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (var n = 1; n <= mots.Length; n++) yield return (string.Join(' ', mots[^n..]), m.Groups["fort"].Success);
                }
            }

            if (balise != "EN") yield break;

            var cle = string.Join(' ', MotsDeLaCible(mot));
            if (MotsAnglaisIllustres.Tous.FirstOrDefault(i => i.Mot.Equals(cle, StringComparison.OrdinalIgnoreCase)) is { } illustre)
            {
                yield return (illustre.Francais, true);
            }

            if (AnglaisDuDebut.TryGetValue(cle, out var debut))
            {
                foreach (var f in debut.Split('|')) yield return (f, true);
            }
        }

        /// <summary>Les mots anglais connus pour un mot français (table des images et mots du début).</summary>
        private static List<List<string>> MotsAnglaisDe(string francais)
        {
            var cle = SansRemplissage(Mots(francais));
            var trouves = new List<List<string>>();

            foreach (var i in MotsAnglaisIllustres.Tous)
            {
                if (SansRemplissage(Mots(i.Francais)).SequenceEqual(cle)) trouves.Add(Mots(i.Mot));
            }

            foreach (var (anglais, fr) in AnglaisDuDebut)
            {
                if (fr.Split('|').Any(f => SansRemplissage(Mots(f)).SequenceEqual(cle))) trouves.Add(Mots(anglais));
            }

            return trouves;
        }

        /// <summary>
        /// Il a dit le mot attendu, ou s'en est approché à une lettre près (« blu », « bloo » pour blue) : un essai dans la
        /// langue, que le professeur juge seul. Les très petits mots (« la », « un ») ne comptent que si le mot attendu
        /// n'est fait que d'eux.
        /// </summary>
        private static bool EssaiDansLaLangue(List<string> motsDits, string cible)
        {
            var attendus = MotsDeLaCible(cible);
            if (attendus.Count == 0) return true;
            if (Contient(motsDits, attendus)) return true;

            var significatifs = attendus.Where(a => a.Length >= 3).ToList();
            if (significatifs.Count == 0) significatifs = attendus;

            return motsDits.Any(d => significatifs.Any(a => d == a || (a.Length >= 3 && d.Length >= 2 && Distance(d, a) <= 1)));
        }

        private static List<string> MotsDeLaCible(string cible)
        {
            var mots = Mots(cible);
            var sans = mots.Where(m => !ArticlesEtrangers.Contains(m)).ToList();
            return sans.Count > 0 ? sans : mots;
        }

        private static bool Contient(List<string> mots, List<string> suite)
        {
            for (var i = 0; i + suite.Count <= mots.Count; i++)
            {
                if (mots.Skip(i).Take(suite.Count).SequenceEqual(suite)) return true;
            }

            return false;
        }

        private static List<string> SansRemplissage(List<string> mots)
        {
            var distincts = mots.Distinct().ToList();
            var sansHesitation = distincts.Where(m => !Hesitations.Contains(m)).ToList();
            if (sansHesitation.Count == 0) return distincts;
            var sansArticle = sansHesitation.Where(m => !Articles.Contains(m)).ToList();
            return sansArticle.Count > 0 ? sansArticle : sansHesitation;
        }

        /// <summary>Minuscules, sans accents, sans ponctuation ; « c'est » → « c est », « lève-toi » → « leve toi ».</summary>
        internal static List<string> Mots(string? texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return [];
            var bas = texte.ToLowerInvariant().Replace("œ", "oe").Replace("æ", "ae");
            var sb = new StringBuilder(bas.Length);
            foreach (var c in bas.Normalize(NormalizationForm.FormD))
            {
                var categorie = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categorie == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            return sb.ToString().Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private static string SansBalises(string texte) => Balise().Replace(texte, " ");

        private static string Court(string dit) => dit.Length <= 60 ? dit : dit[..60].TrimEnd() + "…";

        private static int Distance(string a, string b)
        {
            var precedente = new int[b.Length + 1];
            var courante = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) precedente[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                courante[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cout = a[i - 1] == b[j - 1] ? 0 : 1;
                    courante[j] = Math.Min(Math.Min(courante[j - 1] + 1, precedente[j] + 1), precedente[j - 1] + cout);
                }

                (precedente, courante) = (courante, precedente);
            }

            return precedente[b.Length];
        }
    }
}
