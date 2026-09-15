namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// Le vocabulaire attendu par le transcripteur, selon la matière.
    ///
    /// POURQUOI CE FICHIER EXISTE
    /// --------------------------
    /// Un élève a dit « pluriel » ; il est arrivé « la pupille féminin ». Le
    /// transcripteur arbitre entre des sons proches d'après ce qu'il juge
    /// probable, et hors contexte « pupille » est un mot courant quand
    /// « pluriel » ne l'est pas. Rien ne lui disait qu'il écoutait un cours.
    ///
    /// POURQUOI PAR MATIÈRE ET NON UNE LISTE UNIQUE
    /// --------------------------------------------
    /// Une liste unique a été essayée : elle mélangeait « hypoténuse » et
    /// « photosynthèse » à « participe passé ». Ce champ PONDÈRE — plus il
    /// contient de termes hors sujet, moins il pèse sur ceux qui comptent. Les
    /// mots de SVT n'ont rien à faire dans un cours d'anglais, et ils y
    /// diluent l'effet qu'on cherche.
    ///
    /// COURT, ET SANS PHRASES. Ce champ oriente le vocabulaire, il ne donne
    /// pas de consigne : une instruction rédigée y serait comprise comme du
    /// texte à transcrire et pourrait ressortir dans la réponse de l'élève.
    /// D'où des listes de termes, et rien d'autre.
    /// </summary>
    public static class VocabulaireTranscription
    {
        /// <summary>
        /// Ce qu'on entend dans TOUTES les matières : le langage de la classe
        /// lui-même. Il tient en une ligne pour laisser la place au reste.
        /// </summary>
        private const string Commun =
            "consigne, exercice, énoncé, exemple, question, réponse, "
            + "je n'ai pas compris, tu peux répéter";

        private static readonly IReadOnlyDictionary<string, string> ParMatiere =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FRANCAIS"] =
                    "singulier, pluriel, masculin, féminin, accord, sujet, verbe, "
                    + "participe passé, auxiliaire, conjugaison, orthographe, dictée, "
                    + "homophone, terminaison, adjectif, adverbe, complément, "
                    + "proposition, subordonnée, imparfait, passé simple, subjonctif, "

                    // LES LETTRES ÉPELÉES, PARCE QU'ELLES SE PERDENT.
                    //
                    // « é-e », épelé pour donner un accord au féminin, est
                    // ressorti « et eux » : les deux se prononcent pareil, et
                    // hors contexte « et eux » est de très loin le plus
                    // courant. Nommer ici les terminaisons de l'accord déplace
                    // ce pari du bon côté — c'est tout ce que ce champ sait
                    // faire, et c'est déjà ça.
                    //
                    // Ça ne rend pas l'épellation fiable pour autant : la
                    // consigne du professeur lui dit de faire ÉCRIRE une
                    // terminaison plutôt que de la faire dire.
                    + "é, ée, és, ées, e accent aigu, e accent grave, e muet, "
                    + "s final, épeler, lettre, terminaison en ée, "

                    // LE VOCABULAIRE DE L'ANALYSE, ET LA PHRASE QUI REVIENT.
                    //
                    // « COD » est ressorti « CUD » : le sigle n'existe pas hors
                    // de l'école, et rien ne disait qu'on y était. « est avant »
                    // est ressorti « étardant » — la liaison donne /ɛ.ta.vɑ̃/,
                    // et « étardant » n'est pas un mot, mais « est avant » ne
                    // l'était pas davantage pour un modèle qui ne sait pas de
                    // quoi on parle.
                    //
                    // La phrase entière est nommée, pas seulement ses mots :
                    // c'est elle qui revient à chaque tour de cette leçon, et
                    // c'est sur elle que le pari doit porter.
                    + "COD, COI, complément d'objet direct, complément d'objet "
                    + "indirect, placé avant le verbe, placé après le verbe, "
                    + "auxiliaire avoir, auxiliaire être",

                // LES FAMILLES DE VERBES, ET NON LA LISTE DE GRAMMAIRE SEULE.
                //
                // Le 07/09/2026 : un élève cherchait le participe passé de
                // « drink », c'est-à-dire « drunk ». La transcription a
                // entendu « drink » à chaque tentative — la forme la plus
                // fréquente du triplet, retenue par défaut faute d'indice
                // contraire. Le professeur a repris l'élève trois fois sur
                // une réponse juste.
                //
                // « prétérit, verbe irrégulier » dit au modèle QUELLE
                // CATÉGORIE grammaticale il entend, jamais QUELS MOTS elle
                // contient. Cette liste répare ça pour les triplets qui ne
                // changent qu'UNE voyelle d'une forme à l'autre — la famille
                // de « drink, drank, drunk » — et pour les paires de sons
                // (i bref contre i long) qui reviennent à chaque cours de
                // vocabulaire. Toutes deux sont des classiques enseignés en
                // tant que tels : ce n'est pas une liste inventée, c'est celle
                // du manuel.
                ["ANGLAIS"] =
                    "prétérit, present perfect, verbe irrégulier, pluriel, "
                    + "prononciation, traduction, vocabulaire, temps, auxiliaire, "
                    + "adjectif, préposition, comparatif, superlatif, "

                    // Triplets à voyelle unique — la famille de drink/drank/drunk.
                    + "drink drank drunk, sing sang sung, ring rang rung, "
                    + "begin began begun, swim swam swum, sink sank sunk, "
                    + "run ran run, come came come, "

                    // Paires de sons qui reviennent au vocabulaire de base.
                    + "sheep ship, live leave, sit seat, bit beat, full fool, "
                    + "chip cheap",

                ["MATHS"] =
                    "fraction, numérateur, dénominateur, équation, inéquation, "
                    + "multiplication, division, soustraction, addition, périmètre, "
                    + "aire, volume, hypoténuse, théorème, Pythagore, Thalès, "
                    + "proportionnalité, pourcentage, décimal, entier, puissance, "
                    + "racine carrée, abscisse, ordonnée, parallèle, perpendiculaire",

                ["PHYSIQUE_CHIMIE"] =
                    "molécule, atome, électron, proton, neutron, ion, masse, volume, "
                    + "densité, tension, intensité, résistance, circuit, ampoule, "
                    + "réaction, réactif, produit, solution, soluté, solvant, "
                    + "distillation, ébullition, condensation, énergie, force, vitesse",

                ["SVT"] =
                    "cellule, noyau, membrane, mitochondrie, chloroplaste, "
                    + "photosynthèse, respiration, digestion, nutriment, sang, "
                    + "artère, veine, alvéole, chromosome, gène, ADN, mitose, méiose, "
                    + "fécondation, espèce, évolution, sélection naturelle, séisme, "
                    + "plaque tectonique, subduction, érosion, sédiment",

                ["SCIENCES"] =
                    "matière, état, solide, liquide, gaz, mélange, volume, masse, "
                    + "circuit, énergie, vivant, végétal, animal, croissance, "
                    + "alimentation, hygiène, planète, Terre, Soleil, Lune, saison",

                ["HISTOIRE_GEO"] =
                    "siècle, révolution, république, monarchie, empire, guerre, "
                    + "traité, colonisation, industrialisation, démocratie, "
                    + "territoire, population, densité, métropole, littoral, "
                    + "climat, urbanisation, mondialisation, latitude, longitude",

                ["PHILOSOPHIE"] =
                    "conscience, inconscient, liberté, devoir, bonheur, justice, "
                    + "vérité, raison, nature, culture, technique, travail, langage, "
                    + "État, problématique, dissertation, thèse, argument, objection, "
                    + "concept, notion",
            };

        /// <summary>
        /// Les matières où l'élève PARLE UNE AUTRE LANGUE que le français.
        ///
        /// POURQUOI ELLES CHANGENT LA DONNE
        /// --------------------------------
        /// La langue est imposée au transcripteur, et c'est une bonne chose :
        /// sans elle, « deux » revenait en chinois et « bravo » en arabe. Un
        /// élève français parle français, il n'y a rien à deviner.
        ///
        /// Sauf en cours de langue. Là il parle les DEUX : il pose ses
        /// questions en français et produit ses phrases dans la langue
        /// étudiée — souvent dans le même tour. « J'ai juste dit my parents
        /// work » est ressorti « I juste dit my parents work ».
        ///
        /// Aucun code ISO ne dit « français OU anglais ». On lâche donc la
        /// contrainte pour ces matières et on laisse la détection faire son
        /// travail, segment par segment.
        ///
        /// LE CHOIX EST DÉSÉQUILIBRÉ, ET C'EST VOULU. Imposer le français
        /// abîme la phrase anglaise — celle qu'on ÉVALUE. La détection, elle,
        /// ne se trompe que sur des mots isolés, et ce qu'elle produit alors
        /// est écarté plus loin par le filtre d'alphabet. On préfère abîmer le
        /// bavardage que la réponse.
        /// </summary>
        private static readonly HashSet<string> MatieresBilingues =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "ANGLAIS", "ESPAGNOL", "ALLEMAND", "ITALIEN", "CHINOIS",
                // Spécialités de langue de la voie générale.
                "LLCER_ANGLAIS", "AMC", "LLCER_ESPAGNOL",
            };

        /// <summary>
        /// La langue à imposer, ou null pour laisser le transcripteur décider.
        ///
        /// Ce champ ne sert plus qu'aux modèles qui ne connaissent PAS
        /// `languages` : voir <see cref="Langues"/>, qui dit la même chose en
        /// mieux dès que le modèle sait l'entendre.
        /// </summary>
        public static string? Langue(string? matiereCode) =>
            matiereCode is not null && MatieresBilingues.Contains(matiereCode) ? null : "fr";

        /// <summary>
        /// La langue étudiée dans ce cours, en code ISO 639-1.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> LangueEtudiee =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ANGLAIS"] = "en",
                ["ESPAGNOL"] = "es",
                ["ALLEMAND"] = "de",
                ["ITALIEN"] = "it",
                ["CHINOIS"] = "zh",
                ["LLCER_ANGLAIS"] = "en",
                ["AMC"] = "en",
                ["LLCER_ESPAGNOL"] = "es",
            };

        /// <summary>
        /// LES DEUX LANGUES DU COURS, ce que l'ancien champ ne pouvait pas dire.
        ///
        /// Le commentaire de `MatieresBilingues` explique qu'on lâche la
        /// contrainte de langue en cours de langue, faute de code ISO disant
        /// « français OU anglais ». C'ÉTAIT VRAI, ÇA NE L'EST PLUS : les
        /// modèles récents acceptent une LISTE, et le service la retient —
        /// vérifié le 07/09/2026, la session renvoyée contenait bien
        /// `languages: ["fr","en"]`.
        ///
        /// La différence est réelle : aujourd'hui, en cours d'anglais, le
        /// transcripteur choisit parmi TOUTES les langues qu'il connaît — d'où
        /// les « deux » rendus en chinois et les « bravo » en arabe que le
        /// filtre d'alphabet doit écarter en aval. Avec cette liste, il choisit
        /// entre deux.
        ///
        /// Rend null hors des cours de langue : une matière ordinaire garde le
        /// français imposé par <see cref="Langue"/>, qui suffit et qui marche
        /// sur tous les modèles.
        /// </summary>
        public static string[]? Langues(string? matiereCode)
        {
            if (matiereCode is null) return null;
            if (!LangueEtudiee.TryGetValue(matiereCode, out var etudiee)) return null;

            // Le français d'abord : c'est la langue dans laquelle l'élève pose
            // ses questions, et celle de toutes ses phrases hors exercice.
            return ["fr", etudiee];
        }

        /// <summary>
        /// Les mots que le transcripteur doit guetter LITTÉRALEMENT.
        ///
        /// Distinct du vocabulaire de <see cref="Pour"/>, qui décrit une
        /// ambiance ; celui-ci nomme des mots précis. C'est le champ prévu
        /// pour ça par le service.
        ///
        /// CE QUI EST VÉRIFIÉ ET CE QUI NE L'EST PAS. Le service ACCEPTE ce
        /// champ sur `gpt-transcribe` — la session n'est pas refusée. En
        /// revanche il ne le RENVOIE pas dans la session, contrairement à
        /// `languages` : impossible de confirmer par la poignée de main qu'il
        /// est réellement pris en compte. On l'envoie parce que c'est le
        /// mécanisme documenté pour ce besoin, en sachant que son effet n'est
        /// pas démontré.
        ///
        /// POURQUOI CES MOTS-LÀ. Le 07/09/2026, un élève cherchait « drunk » et
        /// la transcription rendait « drink » à chaque essai. Les trois formes
        /// d'un même verbe irrégulier ne diffèrent que par une voyelle — c'est
        /// exactement ce qui les rend dures à distinguer, et c'est exactement
        /// ce que l'exercice travaille.
        /// </summary>
        public static string[]? MotsCles(string? matiereCode)
        {
            if (matiereCode is null) return null;

            // Seul l'anglais est ouvert aujourd'hui. Les autres langues
            // recevront leur liste le jour où elles arriveront : une liste
            // vide ne vaut pas mieux que pas de liste du tout.
            if (!matiereCode.Equals("ANGLAIS", StringComparison.OrdinalIgnoreCase)) return null;

            // LE CHOIX DES MOTS, ET POURQUOI PAS « TOUT LE VOCABULAIRE ».
            //
            // Mesuré le 07/09/2026 : 294 mots font MIEUX que 30, sans diluer
            // — « I drank orange juice » passait 2 fois sur 3 avec la petite
            // liste, 3 fois sur 3 avec celle-ci. Et le français reste intact,
            // y compris une phrase mixte (« le mot c'est drank, non ? »).
            // Le service accepte d'ailleurs jusqu'à 5000 mots sans broncher.
            //
            // On s'arrête pourtant ici, et c'est un choix : on ne nomme que ce
            // que la transcription RATE. Trois familles, toutes des difficultés
            // connues d'un francophone :
            //
            //   1. LES FORMES DE VERBES IRRÉGULIERS. Un prétérit est un mot
            //      RARE dans l'anglais courant — « drank » l'est bien plus que
            //      « drink » — donc celui que la transcription abandonne au
            //      moindre doute. C'est exactement le cas mesuré : sans cette
            //      liste, « drank » sortait « drink » 9 fois sur 9.
            //   2. LES PAIRES i BREF / i LONG (sheep/ship), que le français ne
            //      distingue pas et qu'un élève ne produit donc pas nettement.
            //   3. LE « TH » (think/sink), qui n'existe pas en français.
            //
            // « house », « school » ou « happy » n'y sont PAS : ils ne sont
            // jamais mal transcrits, et chaque mot inutile est une occasion de
            // reconnaître de l'anglais dans du français.
            return
            [
                "drink", "drank", "drunk", "sing", "sang", "sung",
                "ring", "rang", "rung", "begin", "began", "begun",
                "swim", "swam", "swum", "sink", "sank", "sunk",
                "spring", "sprang", "sprung", "shrink", "shrank", "shrunk",
                "write", "wrote", "written", "drive", "drove", "driven",
                "ride", "rode", "ridden", "rise", "rose", "risen",
                "break", "broke", "broken", "speak", "spoke", "spoken",
                "wake", "woke", "woken", "choose", "chose", "chosen",
                "freeze", "froze", "frozen", "steal", "stole", "stolen",
                "wear", "wore", "worn", "tear", "tore", "torn",
                "swear", "swore", "sworn", "bear", "bore", "born",
                "forget", "forgot", "forgotten", "get", "got", "gotten",
                "hide", "hid", "hidden", "bite", "bit", "bitten",
                "shake", "shook", "shaken", "take", "took", "taken",
                "mistake", "mistook", "mistaken", "throw", "threw", "thrown",
                "grow", "grew", "grown", "blow", "blew", "blown",
                "know", "knew", "known", "fly", "flew", "flown",
                "draw", "drew", "drawn", "give", "gave", "given",
                "eat", "ate", "eaten", "see", "saw", "seen",
                "go", "went", "gone", "do", "did", "done",
                "be", "was", "were", "been", "come", "came",
                "become", "became", "run", "ran", "lie", "lay",
                "lain", "fall", "fell", "fallen", "buy", "bought",
                "bring", "brought", "think", "thought", "fight", "fought",
                "seek", "sought", "catch", "caught", "teach", "taught",
                "feel", "felt", "keep", "kept", "sleep", "slept",
                "creep", "crept", "sweep", "swept", "weep", "wept",
                "leave", "left", "mean", "meant", "send", "sent",
                "spend", "spent", "build", "built", "lend", "lent",
                "bend", "bent", "lose", "lost", "find", "found",
                "hold", "held", "stand", "stood", "understand", "understood",
                "win", "won", "sit", "sat", "meet", "met",
                "lead", "led", "feed", "fed", "bleed", "bled",
                "hang", "hung", "stick", "stuck", "strike", "struck",
                "dig", "dug", "spin", "spun", "say", "said",
                "tell", "told", "sell", "sold", "make", "made",
                "pay", "paid", "hear", "heard", "read", "sheep",
                "ship", "live", "seat", "beat", "fit", "feet",
                "hit", "heat", "bin", "rich", "reach", "slip",
                "still", "steel", "lip", "leap", "grin", "green",
                "pitch", "peach", "will", "wheel", "this", "these",
                "is", "ease", "full", "fool", "pull", "pool",
                "look", "Luke", "cat", "cut", "bad", "bud",
                "hat", "hut", "bag", "bug", "match", "much",
                "ankle", "uncle", "three", "tree", "thin", "tin",
                "thing", "thank", "mouth", "mouse", "both", "boat",
                "path", "pass", "with", "wiz", "hair", "air",
                "old", "high", "eye", "present perfect", "past simple", "past participle",
                "preterite", "irregular verb", "infinitive", "auxiliary", "comparative", "superlative",            ];
        }

        /// <summary>
        /// Ce modèle comprend-il `languages` et `keywords` ?
        ///
        /// LA QUESTION PORTE SUR LE MODÈLE, JAMAIS SUR LA MATIÈRE, et c'est ce
        /// qui rend le retour en arrière sans danger : reposer l'ancien modèle
        /// dans la configuration suffit à ne plus rien envoyer de neuf. Aucune
        /// session ne peut être refusée par une option restée cochée quelque
        /// part.
        ///
        /// Le modèle courant les REFUSE, explicitement — « The 'languages'
        /// parameter is not supported for this model » — et une session
        /// refusée, c'est un élève sans micro. D'où une liste blanche : ce
        /// qu'on n'a pas vérifié ne reçoit rien.
        /// </summary>
        public static bool AccepteLesIndicesRiches(string? modele) =>
            modele is not null && ModelesRiches.Contains(modele);

        private static readonly HashSet<string> ModelesRiches =
            new(StringComparer.OrdinalIgnoreCase) { "gpt-transcribe" };

        /// <summary>
        /// Le modèle de transcription pour cette matière.
        ///
        /// Les cours de langue seuls basculent sur le modèle qui sait borner
        /// les langues ; tout le reste garde celui d'aujourd'hui. Voir
        /// <see cref="OptionsVoix.ModeleTranscriptionLangues"/> pour le
        /// pourquoi et pour le coût.
        /// </summary>
        /// <remarks>
        /// LE MEME MODELE POUR TOUTES LES MATIERES, ET C EST UNE REGLE DE BASE.
        ///
        /// Les cours de langue ont longtemps ete seuls a recevoir le modele
        /// complet ; partout ailleurs on gardait le « mini ». La difference ne
        /// s entendait pas sur une reponse courte — un nombre, un mot — et
        /// devenait ecrasante des que l eleve expliquait : en maths, sur une
        /// justification de trente secondes, il ne revenait que les derniers
        /// mots. Un enfant qui raisonne a voix haute et voit disparaitre ce
        /// qu il vient de dire arrete de raisonner a voix haute.
        ///
        /// Un enfant qui explique un theoreme merite exactement la meme oreille
        /// qu un enfant qui repete une phrase en anglais. Une matiere ajoutee
        /// demain en herite sans que personne ait a y penser.
        /// </remarks>
        public static string Modele(string? matiereCode, string ordinaire, string langues) =>
            string.IsNullOrWhiteSpace(langues) ? ordinaire : langues;

        /// <summary>
        /// Les mots à souffler pour cette matière.
        ///
        /// Une matière inconnue — ou une session ouverte sans contexte — reçoit
        /// le seul socle commun. C'est le bon défaut : mieux vaut une
        /// pondération faible que celle d'une autre matière.
        /// </summary>
        public static string Pour(string? matiereCode)
        {
            var socle = "Cours particulier, France. " + Commun;

            return matiereCode is not null && ParMatiere.TryGetValue(matiereCode, out var termes)
                ? socle + ", " + termes
                : socle;
        }
    }
}
