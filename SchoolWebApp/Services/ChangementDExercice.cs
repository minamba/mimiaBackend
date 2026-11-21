using System.Text.RegularExpressions;
using SchoolWebApp.Api.Services.Voix;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'ÉLÈVE CHANGE D'EXERCICE QUAND IL VEUT — Camara, le 14/11/2026, signalement d'une famille (Adrien, CP) :
    /// « Lorsque l'élève demande au professeur de lui proposer un nouvel exercice, il commence à présenter la consigne
    /// du nouvel exercice, puis revient automatiquement à l'exercice précédent portant sur la prononciation des
    /// syllabes. » Et : « il faut absolument que le professeur change sans poser de question et qu'il ne revienne pas
    /// sur l'exercice précédent ; un enfant peut changer d'exercice autant qu'il veut ». Pour TOUS les professeurs.
    ///
    /// CE N'ÉTAIT PAS LE PROFESSEUR, C'ÉTAIT LE CODE. Le Noyau dit déjà de basculer tout de suite. Mais un exercice
    /// tenu par le code (sons, syllabes, lecture en images, lecture à voix haute, lecture des nombres) se reconnaît
    /// dans les DOUZE derniers messages : la demande de l'enfant n'y changeait rien. Le professeur disait « d'accord,
    /// voici un nouvel exercice », et le code, lui, ajoutait encore « Regarde bien la syllabe en couleur… » à la fin de
    /// son message, lui redonnait le fait « tu dictes UNE syllabe », et jugeait la réponse suivante comme une syllabe.
    ///
    /// DEUX CHOSES, DONC :
    /// - LE FAIT, joint au message de l'élève à ce tour (une règle de prompt ne vaut pas un fait), puis un rappel court
    ///   aux tours qui suivent ;
    /// - LA COUPURE : tout ce qui précède sa demande ne compte plus pour reconnaître l'exercice en cours. Après elle,
    ///   une phrase du professeur qui nomme seulement l'ancien exercice (« on laisse les syllabes ») ne le rouvre pas :
    ///   il faut un vrai signe d'exercice (balise de voix, tableau, image, consigne) ou un choix de l'enfant.
    ///
    /// JAMAIS PENDANT UN CONTRÔLE NOTÉ (on n'y touche pas) : c'est l'appelant qui l'écarte.
    ///
    /// Les deux faits évitent les mots qui chargent les consignes des exercices de langue (DetecteurExercicesLangue).
    /// </summary>
    public static partial class ChangementDExercice
    {
        public enum Nature
        {
            Aucune,

            /// <summary>« Un autre exercice », « on change », « je veux faire autre chose ».</summary>
            Changer,

            /// <summary>« J'en ai marre », « je veux arrêter cet exercice » : il ne dit pas vouloir autre chose.</summary>
            Arreter,
        }

        /// <summary>Au-delà, ce n'est plus une demande : un texte qu'il a rédigé ou collé.</summary>
        private const int MotsMax = 40;

        /// <summary>Le rappel « ne reviens pas à l'ancien » suit la demande pendant ces messages de l'historique (trois tours).</summary>
        public const int MessagesDeSuite = 6;

        public const string FaitChanger =
            "[Fait pour ce tour, qui l'emporte sur tous les rappels d'exercice : l'élève demande à CHANGER d'exercice. C'est SA "
            + "décision et il peut changer autant de fois qu'il veut. Tu arrêtes l'exercice en cours TOUT DE SUITE : tu ne le "
            + "finis pas, pas de « une dernière avant », tu ne lui demandes ni pourquoi ni s'il est sûr, aucun reproche. S'il a "
            + "dit ce qu'il veut faire, tu le fais ; sinon tu choisis toi-même un exercice DIFFÉRENT du précédent et tu le "
            + "commences dans ce message. Tu ne reviens pas à l'ancien exercice, ni à la fin de ce message ni aux suivants, "
            + "sauf s'il le redemande lui-même." + JamaisPlusTard + "]";

        public const string FaitArreter =
            "[Fait pour ce tour, qui l'emporte sur tous les rappels d'exercice : l'élève veut ARRÊTER l'exercice en cours. C'est "
            + "SA décision. Tu l'arrêtes TOUT DE SUITE : tu ne le finis pas, pas de « une dernière avant », tu ne lui demandes "
            + "pas pourquoi, aucun reproche. S'il a dit ce qu'il veut faire à la place, tu le fais ; sinon propose-lui en une "
            + "phrase quelque chose de DIFFÉRENT. Tu ne reviens pas à cet exercice, ni à la fin de ce message ni aux suivants, "
            + "sauf s'il le redemande lui-même.]";

        public const string RappelDeSuite =
            "[Rappel : il a demandé il y a peu à changer d'exercice. Tu restes sur ce qu'il fait maintenant. Tu ne reviens pas "
            + "de toi-même à l'exercice d'avant et tu n'insistes jamais pour le reprendre : c'est lui qui décide.]";

        /// <summary>
        /// CE QU'IL DEMANDE SE FAIT MAINTENANT — signalement #56 (Adam, 21/11/2026) : « On fera du vocabulaire la prochaine
        /// fois. Pour l'instant, je te propose nos petits jeux de syllabes et de mots. » Pour tous les professeurs.
        /// </summary>
        public const string JamaisPlusTard =
            " Jamais « on fera ça la prochaine fois » ni « pour l'instant, je te propose… » : ce qu'il demande, tu le fais maintenant.";

        /// <summary>
        /// UNE HISTOIRE ET SES QUESTIONS — signalement #55 (Adam, 21/11/2026) : deux phrases lues, aucune question, puis UNE
        /// seule question au bout de trois relances, et une réponse hors de l'histoire laissée sans correction. Les mots qui
        /// chargent les consignes de langue (DetecteurExercicesLangue) sont évités, comme dans les autres faits.
        /// </summary>
        public const string FaitHistoire =
            "[Fait pour ce tour : ce qu'il demande, c'est une HISTOIRE avec des questions. S'il parle de celle que vous venez "
            + "d'écouter ou de lire, tu poses tes questions sur ELLE, sans en raconter une autre. Sinon tu la commences dans CE "
            + "message : tu l'annonces en une phrase (« Écoute bien cette petite histoire. »), puis une histoire courte et simple de son âge "
            + "(3 à 6 phrases au CP), lue dans ta balise de voix comme une écoute, jamais écrite dans ta bulle ni au tableau, puis "
            + "ta PREMIÈRE question sur ce qui s'y passe (qui, quoi, où, pourquoi). Ensuite tu poses 2 ou 3 questions en tout, UNE "
            + "à la fois, avant de passer à autre chose." + RegleDesReponsesDHistoire + "]";

        public const string RappelHistoire =
            "[Rappel : il a demandé une histoire avec des questions. Tant que tu n'as pas posé 2 ou 3 questions sur elle, UNE à la "
            + "fois, tu restes sur l'histoire : tu ne repars pas de toi-même vers un autre exercice et tu n'annonces pas une "
            + "question sans la poser." + RegleDesReponsesDHistoire + " S'il demande lui-même autre chose, tu le suis.]";

        private const string RegleDesReponsesDHistoire =
            " Une réponse juste : tu le félicites et tu poses la question suivante. Une réponse fausse, ou qui raconte autre chose "
            + "que l'histoire, ne se valide jamais, même gentiment (pas de « quelle belle histoire », pas de « on garde ça pour une "
            + "prochaine fois ») : tu lui dis simplement que ce n'est pas ce qui se passe dans l'histoire, tu lui relis dans ta "
            + "balise de voix la seule phrase qui donne la réponse, et tu reposes la même question.";

        /// <summary>
        /// DU VOCABULAIRE, PAS UN EXERCICE DE SONS — signalement #56 (Adam, 21/11/2026) : après la définition de « potager »,
        /// « écoute bien ce mot et redis-le-moi », puis « Voici le premier mot. pantalon. Qu'as-tu entendu ? ». Le seul jeu
        /// qu'il connaissait était celui des sons.
        /// </summary>
        public const string FaitVocabulaire =
            "[Fait pour ce tour : ce qu'il demande, c'est du VOCABULAIRE. Tu commences dans CE message une activité de "
            + "vocabulaire de sa classe : au CP, les mots d'un thème qu'il connaît (la cuisine, les animaux, l'école), une "
            + "famille de mots (jardin, jardinier, jardiner), ce que veut dire un mot, puis l'employer dans une phrase à lui. "
            + "Une question à la fois. Ce n'est PAS un exercice de sons : tu ne lui fais pas répéter un mot que tu prononces, "
            + "pas de « redis-le-moi », pas de « qu'as-tu entendu », pas de mot seul dans ta balise de voix.]";

        public static string Fait(Nature nature) => nature == Nature.Arreter ? FaitArreter : FaitChanger;

        /// <summary>
        /// Le fait du changement, suivi de celui de l'activité qu'il nomme (une histoire, du vocabulaire) : le professeur
        /// sait non seulement qu'il change, mais ce qu'il doit faire à la place, et que ça se fait maintenant.
        /// </summary>
        public static string Fait(Nature nature, string? messageDeLEleve)
        {
            var fait = Fait(nature);
            var dit = EntreCrochets().Replace(messageDeLEleve ?? string.Empty, " ");
            var activite = ActiviteNommee().Matches(dit).LastOrDefault();
            if (activite is null) return fait;

            return Histoire().IsMatch(activite.Value) ? fait + "\n\n" + FaitHistoire
                : Vocabulaire().IsMatch(activite.Value) ? fait + "\n\n" + FaitVocabulaire
                : fait;
        }

        // ------------------------------------------------------------------ ce que dit l'enfant

        // « change d'exercice », « on change d'activité », « changer de jeu ».
        [GeneratedRegex(@"\bchang\w*\s+(?:d['’]\s*(?:exercice|exo|activit[ée])|de\s+(?:jeu|sujet|th[èe]me|le[çc]on|chapitre))", RegexOptions.IgnoreCase)]
        private static partial Regex ChangeDe();

        // « on change », « je veux changer », « on peut changer ? » — mais pas « on change de syllabe » : le suivant du
        // même exercice n'est pas un autre exercice.
        [GeneratedRegex(@"\b(?:on|je\s+(?:veux|voudrais|peux|aimerais|pr[ée]f[èe]re)|j['’]aimerais|tu\s+peux|peux[- ]tu|on\s+peut|est-ce\s+qu['’]on\s+peut)\s+(?:bien\s+)?chang(?:e|er)\b(?!\s+(?:de\b|d['’]))", RegexOptions.IgnoreCase)]
        private static partial Regex ChangeToutCourt();

        // « un autre exercice », « un nouvel exercice », « une autre activité », « l'exercice suivant », « un exercice différent ».
        [GeneratedRegex(@"\b(?:autres?|nouvel|nouveaux?|nouvelles?|prochaine?s?)\s+(?:exercices?|exos?|activit[ée]s?|jeux?)\b|\b(?:exercices?|exos?|activit[ée]s?|jeux?)\s+(?:suivante?s?|diff[ée]rente?s?)\b|\bautre\s+(?:type|genre|sorte)\s+d['’]\s*(?:exercice|exo|activit[ée])", RegexOptions.IgnoreCase)]
        private static partial Regex AutreExercice();

        // « propose-moi un exercice », « donne-moi un jeu ».
        [GeneratedRegex(@"\b(?:propose|donne|fais|trouve|invente|choisis)\w*[- ](?:moi|nous)\s+une?\s+(?:exercice|exo|activit[ée]|jeu)\b", RegexOptions.IgnoreCase)]
        private static partial Regex DonneUnExercice();

        // « on fait autre chose », « je veux passer à autre chose ». Pas « j'ai entendu autre chose » : il faut le verbe.
        [GeneratedRegex(@"\b(?:fai\w*|pass\w*|veux|voudrais|aimerais|pr[ée]f[èe]re\w*|propose\w*|donne\w*|essa\w*)\b[^.?!\n]{0,25}\bautre\s+chose\b|^\W*autre\s+chose\W*$", RegexOptions.IgnoreCase)]
        private static partial Regex AutreChose();

        // UNE AUTRE ACTIVITÉ, NOMMÉE — signalements #55 et #56 (Adam, 21/11/2026) : « Je voudrais écouter une petite histoire
        // et répondre à des questions dessus », « je voudrais apprendre des mots nouveaux, du vocabulaire. On peut faire un jeu
        // de vocabulaire ? ». Ce n'était pas une demande de changement : en pleine lecture en images, le code gardait
        // l'exercice des syllabes, retirait les questions du professeur (LectureDImage.Reecrire) et ajoutait « Regarde bien la
        // syllabe en couleur… » à la fin de son histoire ; « des mots » passait même pour le choix de l'écoute des mots.
        // Un sujet qui demande (je veux / on peut / tu me / raconte-moi / maintenant), un verbe d'activité facultatif
        // (faire, écouter, lire, apprendre, raconter…), puis l'activité : une histoire ou un conte (UNE / DES : « l'histoire »
        // est celle qu'on écoute déjà, la réécouter n'est pas changer), du vocabulaire, des mots nouveaux, de la grammaire,
        // de la conjugaison, de l'orthographe, une poésie, une rédaction, de la compréhension — éventuellement dans « un jeu
        // de / un exercice de / une activité sur ». Une négation entre le sujet et l'activité (« je veux pas faire de
        // grammaire ») ne correspond pas : rien n'y est permis entre les deux.
        private const string SujetQuiDemande =
            @"(?<!\p{L})(?:(?:je|j['’])\s*(?:veux|voudrais|aimerais|pr[ée]f[èe]re(?:rais)?|peux)(?:\s+bien)?"
            + @"|(?:on|nous)(?:\s+(?:peut|pourrait|va|veut|pourra))?"
            + @"|tu\s+(?:(?:peux|veux|pourrais|voudrais)\s+)?(?:me|m['’]|nous)"
            + @"|peux[- ]tu(?:\s+(?:me|nous)|\s*m['’])?"
            + @"|(?:raconte|lis|dis|fais|donne|propose|apprends|pose)[- ](?:moi|nous)"
            + @"|maintenant|passons\s+(?:à|a|au|aux))";

        private const string VerbeDActivite =
            @"(?:\s*(?:fai(?:re|t|s)|refaire|[ée]cout(?:er|e|es)|entendre|lire|li[st]|apprendre|travailler|jouer(?:\s+(?:à|a|au|aux))?"
            + @"|racont(?:er|e|es)|d[ée]couvrir|[ée]tudier|r[ée]viser|pass(?:e|er)\s+(?:à|a|au|aux)|commencer|essayer|avoir))?";

        private const string ActiviteDeLangue =
            @"(?:(?:une?|des|d['’])\s*(?:(?:petite?|autre|nouvelle|nouveau|belle|beau|courte?|vraie?)s?\s+)?(?:histoires?|contes?)"
            + @"|(?:des\s+|de\s+)?(?:mots\s+nouveaux|nouveaux\s+mots)"
            + @"|(?:(?:une?|des)\s+(?:petite?s?\s+)?(?:jeux?|exercices?|exos?|activit[ée]s?)\s+(?:de|d['’]|sur)\s*(?:la\s+|le\s+|l['’]\s*|les\s+)?"
            + @"|du\s+|de\s+la\s+|de\s+l['’]\s*|la\s+|le\s+|l['’]\s*|un\s+peu\s+de\s+|de\s+|d['’]\s*|(?:une?|des)\s+(?:(?:petite?|autre|nouvelle|nouveau|belle|beau)s?\s+)?)?"
            + @"(?:vocabulaire|grammaire|conjugaison|orthographe|po[ée]sies?|po[èe]mes?|r[ée]dactions?|compr[ée]hension))(?!\p{L})";

        [GeneratedRegex(SujetQuiDemande + VerbeDActivite + @"\s*" + ActiviteDeLangue, RegexOptions.IgnoreCase)]
        private static partial Regex ActiviteNommee();

        [GeneratedRegex(@"histoires?|contes?|compr[ée]hension", RegexOptions.IgnoreCase)]
        private static partial Regex Histoire();

        [GeneratedRegex(@"vocabulaire|mots\s+nouveaux|nouveaux\s+mots", RegexOptions.IgnoreCase)]
        private static partial Regex Vocabulaire();

        /// <summary>Nomme-t-il une autre activité (une histoire, du vocabulaire, de la grammaire…) ? Voir ActiviteNommee.</summary>
        public static bool NommeUneActivite(string? messageDeLEleve) =>
            !string.IsNullOrWhiteSpace(messageDeLEleve) && ActiviteNommee().IsMatch(EntreCrochets().Replace(messageDeLEleve, " "));

        // « j'en ai marre », « je veux plus faire ça », « j'ai plus envie », « on arrête », « arrête cet exercice ». Jamais
        // « stop » ni « arrête » seuls : ce sont aussi des mots qu'on lui fait lire. Ni « je veux plus d'exercices » :
        // à l'oral, c'est aussi « j'en veux davantage ».
        [GeneratedRegex(@"\bj['’]en\s+ai\s+(?:marre|assez|ras)\b|\bje\s+(?:ne\s+)?(?:veux|voudrais)\s+plus\s+(?:faire|continuer|lire|jouer|[ée]couter|r[ée]p[ée]ter|ça|ca|de\s+(?:ça|ca|syllabes|mots|nombres))\b|\bj['’]ai\s+plus\s+envie\b|\bje\s+n['’]ai\s+plus\s+envie\b|\b(?:j['’]aime|je\s+n['’]aime)\s+pas\s+(?:cet?\s+(?:exercice|exo|jeu)|cette\s+activit[ée])|\bon\s+arr[êe]te\b|\bj['’]arr[êe]te\b|\bje\s+(?:veux|voudrais)\s+arr[êe]ter\b|\barr[êe]t\w*\s+(?:cet?\s+(?:exercice|exo|jeu)|cette\s+activit[ée]|l['’]\s*(?:exercice|exo|activit[ée])|les\s+(?:syllabes|mots|nombres|sons))", RegexOptions.IgnoreCase)]
        private static partial Regex Lassitude();

        // « t'as encore changé d'exercice », « pourquoi tu changes d'exercice » : une plainte, pas une demande.
        [GeneratedRegex(@"\b(?:t['’]as|tu\s+as|t['’]a|il\s+a|elle\s+a)\s+(?:encore\s+|d[ée]j[àa]\s+)?chang[ée]|\bpourquoi\b[^.?!\n]{0,40}\bchang", RegexOptions.IgnoreCase)]
        private static partial Regex Plainte();

        // « je veux pas changer », « pas d'autre exercice », « on change pas ».
        [GeneratedRegex(@"\b(?:pas|jamais)\s+(?:envie\s+de\s+|besoin\s+de\s+)?(?:chang\w*|d['’]\s*autres?\s+(?:exercice|exo|activit[ée]|jeu)|un\s+autre\s+(?:exercice|exo|jeu)|une\s+autre\s+activit[ée]|autre\s+chose|arr[êe]t\w*)|\bchang\w*\s+pas\b", RegexOptions.IgnoreCase)]
        private static partial Regex Refus();

        // « J'ai lu la syllabe… », « J'ai entendu le mot… » : une réponse à l'exercice, écrite par le relais du micro.
        [GeneratedRegex(@"^\W*j['’]ai\s+(?:lu|entendu)\b", RegexOptions.IgnoreCase)]
        private static partial Regex ReponseALExercice();

        [GeneratedRegex(@"\[[^\]]*\]")]
        private static partial Regex EntreCrochets();

        /// <summary>Ce que l'élève demande dans ce message. Les faits entre crochets que l'application y accroche sont écartés.</summary>
        public static Nature Lire(string? messageDeLEleve)
        {
            if (string.IsNullOrWhiteSpace(messageDeLEleve)) return Nature.Aucune;

            var dit = EntreCrochets().Replace(messageDeLEleve, " ").Trim();
            if (dit.Length == 0 || dit.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > MotsMax) return Nature.Aucune;
            if (ReponseALExercice().IsMatch(dit) || Plainte().IsMatch(dit) || Refus().IsMatch(dit)) return Nature.Aucune;

            if (ChangeDe().IsMatch(dit) || ChangeToutCourt().IsMatch(dit) || AutreExercice().IsMatch(dit)
                || DonneUnExercice().IsMatch(dit) || AutreChose().IsMatch(dit) || ActiviteNommee().IsMatch(dit))
            {
                return Nature.Changer;
            }

            return Lassitude().IsMatch(dit) ? Nature.Arreter : Nature.Aucune;
        }

        public static bool Demande(string? messageDeLEleve) => Lire(messageDeLEleve) != Nature.Aucune;

        [GeneratedRegex(@"[,.;:!?\n]+|\s+(?:et|mais|puis|donc|alors)\s+", RegexOptions.IgnoreCase)]
        private static partial Regex Propositions();

        // « un autre exercice que les syllabes », « plus de mots », « pas les sons », « sans lire ».
        [GeneratedRegex(@"\b(?:que|sauf|pas|plus|sans|marre)\s+(?:de\s+|d['’]\s*|les\s+|des\s+|la\s+|le\s+|l['’]\s*)?(?:syllabes?|mots?|sons?|nombres?|lecture|[ée]coute|lire|[ée]couter)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Rejet();

        /// <summary>
        /// CE QUE SA DEMANDE CHOISIT POUR LA SUITE : ses propositions, moins celles où il REJETTE quelque chose. « J'en
        /// ai marre des syllabes » ne choisit rien (le mot « syllabes » y relançait l'exercice) ; « je veux plus faire de
        /// mots, je veux faire les syllabes » choisit les syllabes. Null : rien de choisi. Un message qui n'est pas une
        /// demande revient tel quel.
        /// </summary>
        public static string? PartieQuiChoisit(string? messageDeLEleve)
        {
            if (!Demande(messageDeLEleve)) return messageDeLEleve;

            var gardees = Propositions().Split(EntreCrochets().Replace(messageDeLEleve!, " "))
                .Select(p => p.Trim())
                // L'activité qu'il nomme (« je voudrais apprendre des mots nouveaux ») n'est pas un exercice de sons : elle
                // ne choisit rien pour eux (« des mots » y passait pour le choix de l'écoute des mots, signalement #56).
                .Where(p => p.Length > 0 && !Lassitude().IsMatch(p) && !Rejet().IsMatch(p) && !ActiviteNommee().IsMatch(p))
                .ToList();

            // LE BOUTON DE LA CARTE DIT À VOIX HAUTE (« lire ! ») : l'écran joint la phrase du bouton dans un fait entre
            // crochets (choixExerciceSons.js). C'est un choix : il se garde.
            gardees.AddRange(ExerciceChoisi().Matches(messageDeLEleve!).Select(c => c.Groups[1].Value.Trim().TrimEnd('.')).Where(c => c.Length > 0));

            return gardees.Count == 0 ? null : string.Join(". ", gardees) + ".";
        }

        [GeneratedRegex(@"\[EXERCICE CHOISI : ([^\]]*)\]", RegexOptions.IgnoreCase)]
        private static partial Regex ExerciceChoisi();

        // ------------------------------------------------------------------ la coupure

        /// <summary>Un message d'élève qui demande à changer : jamais une réponse déjà jugée (elle porte un verdict).</summary>
        private static bool EstUneDemande((string Role, string? Contenu, bool? SonJuste) m) =>
            m.Role == "user" && m.SonJuste is null && Demande(m.Contenu);

        /// <summary>L'index de sa dernière demande dans ces messages (du plus ancien au plus récent) ; -1 s'il n'y en a pas.</summary>
        public static int DerniereDemande(IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> messages)
        {
            for (var i = messages.Count - 1; i >= 0; i--)
            {
                if (EstUneDemande(messages[i])) return i;
            }

            return -1;
        }

        /// <summary>
        /// CE QUI COMPTE ENCORE POUR RECONNAÎTRE L'EXERCICE EN COURS. `messages` : les derniers de l'historique, SANS le
        /// message de ce tour. S'il demande à changer À CE TOUR, plus rien d'avant ne compte. S'il l'a demandé plus tôt,
        /// seuls comptent sa demande et ce qui la suit — et, parmi les messages du professeur, seulement ceux qui
        /// portent un vrai signe d'exercice : les autres sont vidés (null), pas retirés, pour garder les rangs.
        /// Sans demande, la liste revient telle quelle.
        /// </summary>
        public static IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> Fil(
            IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> messages, bool changeACeTour = false)
        {
            if (changeACeTour) return [];

            var debut = DerniereDemande(messages);
            if (debut < 0) return messages;

            var fil = new List<(string Role, string? Contenu, bool? SonJuste)>(messages.Count - debut);
            for (var i = debut; i < messages.Count; i++)
            {
                var m = messages[i];
                if (i == debut) fil.Add((m.Role, PartieQuiChoisit(m.Contenu), m.SonJuste));
                else fil.Add(m.Role == "assistant" && !RegleVerificationSon.PorteUnExercice(m.Contenu) ? (m.Role, null, m.SonJuste) : m);
            }

            return fil;
        }

        /// <summary>A-t-il demandé à changer dans les derniers messages de l'historique (sans celui de ce tour) ?</summary>
        public static bool DemandeRecente(IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> historique) =>
            DerniereDemande(historique.Skip(Math.Max(0, historique.Count - MessagesDeSuite)).ToList()) >= 0;

        /// <summary>Le rappel de l'histoire suit sa demande pendant ces messages de l'historique (l'histoire et trois questions).</summary>
        public const int MessagesDHistoire = 8;

        /// <summary>
        /// L'HISTOIRE QU'IL A DEMANDÉE EST-ELLE EN COURS ? Sa dernière demande, dans les derniers messages de l'historique
        /// (sans celui de ce tour), nomme une histoire ou de la compréhension — signalement #55 : après UNE question, le
        /// professeur repartait sur la grammaire. Une demande plus récente (« on change ») l'emporte : on la suit.
        /// </summary>
        public static bool HistoireEnCours(IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)> historique)
        {
            var derniers = historique.Skip(Math.Max(0, historique.Count - MessagesDHistoire)).ToList();
            var i = DerniereDemande(derniers);
            if (i < 0) return false;

            var activite = ActiviteNommee().Matches(EntreCrochets().Replace(derniers[i].Contenu ?? string.Empty, " ")).LastOrDefault();
            return activite is not null && Histoire().IsMatch(activite.Value);
        }
    }
}
