namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// L'enseignement de spécialité « Littérature et langues et cultures de l'Antiquité » (LLCA) de la
    /// voie générale, en latin et en grec, tel que les textes officiels le définissent — provenance
    /// détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielLlca
    {
        // =====================================================================================
        // RÉFÉRENTIELS DE LA SPÉCIALITÉ LLCA (latin, grec) — VOIE GÉNÉRALE, PREMIÈRE ET TERMINALE
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis les textes
        // officiels téléchargés et lus le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\llca\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau    | Intitulé exact (page de titre de l'annexe)                                        |
        // |---------|-----------|-----------------------------------------------------------------------------------|
        // | LATIN1  | PREMIERE  | Programme de littérature et langues et cultures de l'Antiquité de première générale (partie latin)  |
        // | GREC1   | PREMIERE  | Programme de littérature et langues et cultures de l'Antiquité de première générale (partie grec)   |
        // | LATINT  | TERMINALE | Programme de littérature et langues et cultures de l'Antiquité de terminale générale (partie latin) |
        // | GRECT   | TERMINALE | Programme de littérature et langues et cultures de l'Antiquité de terminale générale (partie grec)  |
        //
        // PREMIÈRE : arrêté du 17-1-2019, NOR MENE1901582A, BO spécial n° 1 du 22 janvier 2019.
        //   Article 2 : « Les dispositions du présent arrêté entrent en vigueur à la rentrée scolaire 2019. »
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901582A.htm
        //   Annexe (unique) : https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/14/8/spe582_annexe_1063148.pdf
        //                     (copie identique, même md5, sur éduscol : spe582annexe1063148pdf-83520.pdf)
        //   Légifrance : https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000038029449
        //
        // TERMINALE : arrêté du 19-7-2019, NOR MENE1921257A, BO spécial n° 8 du 25 juillet 2019.
        //   Article 2 : « Les dispositions du présent arrêté entrent en vigueur à la rentrée scolaire 2020. »
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921257A.htm
        //   Annexe (unique) : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/89/5/spe257_annexe_1158895.pdf
        //                     (copie identique, même md5, sur éduscol : spe257annexe1158895pdf-83523.pdf)
        //   Légifrance : https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000038799968
        //
        // NOTIONS DE LANGUE DE L'OPTION, REPRISES PAR RENVOI : les deux programmes de spécialité disent que
        //   leurs notions linguistiques s'ajoutent à celles de l'enseignement optionnel de la même classe.
        //   Elles sont donc incluses (Domaine « Langue », commentées « communes avec l'option »), lues dans :
        //   - option LCA de première : annexe 2 de l'arrêté MENE1901579A (BO spécial n° 1 du 22-1-2019 ; l'arrêté fixe
        //     l'option de seconde en annexe 1 et de première en annexe 2),
        //     https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/85/5/spe579_annexe2_1062855.pdf ;
        //   - option LCA de terminale : arrêté MENE1921267A (BO spécial n° 8 du 25-7-2019),
        //     éduscol spe267annexe1158694pdf-83517.pdf.
        //
        // ŒUVRES INTÉGRALES DE TERMINALE (programme limitatif renouvelé tous les deux ans) :
        //   note de service du 23-2-2026, NOR MENE2605298N, BO n° 11 du 12 mars 2026, « pour les années
        //   scolaires 2026-2027 et 2027-2028 » : https://www.education.gouv.fr/bo/2026/Hebdo11/MENE2605298N
        //   - Grec : Lucien, Histoires vraies (Les Belles Lettres, 2011, p. 39-139) ; Italo Calvino, Le baron perché
        //     (trad. Martin Rueff, Folio, 2019). Objet d'étude « L'homme, le monde, le destin », sous-ensemble
        //     « Le "grand théâtre du monde" : vérité et illusion ».
        //   - Latin : Ovide, Tristes, livre III (Les Belles Lettres, Classiques en Poche, « à paraitre ») ;
        //     Karen Blixen, La ferme africaine (trad. Alain Gnaedig, Folio, 2006). Objet d'étude « L'homme, le monde,
        //     le destin » (aucun sous-ensemble précisé).
        //   Remplace MENE2404047N (BO n° 11 du 14-3-2024 : Aristophane / Atwood en grec, Loher en latin), valable
        //   pour 2024-2025 et 2025-2026 seulement.
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 :
        //   - La page éduscol « Programmes et ressources en langues et cultures de l'Antiquité - voie GT »
        //     (https://eduscol.education.gouv.fr/5808/programmes-et-ressources-en-langues-et-cultures-de-l-antiquite-voie-gt,
        //     datée « mars 2026 ») liste sous « Programmes en vigueur » exactement ces deux annexes de spécialité, et
        //     le programme limitatif 2026-2027 / 2027-2028.
        //   - Légifrance (lu le 14/09/2026) : les deux arrêtés n'affichent aucun texte modificatif.
        //   - Sommaires des BO hebdomadaires 2024, 2025 et 2026 (jusqu'au n° 34 du 10-9-2026) parcourus : les seuls
        //     textes sur la LLCA sont les deux programmes limitatifs (MENE2404047N, MENE2605298N). Aucun arrêté
        //     modifiant ou remplaçant les programmes. Sommaires 2023 lus en partie seulement (n° 16 à 48).
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - Latin et grec partagent le MÊME texte : objets d'étude communs, et « chacun des enseignements (latin ou
        //     grec) conserve sa singularité, notamment pour l'apprentissage de la langue ». Deux jeux complets ont été
        //     rédigés : les notions de langue sont propres à chaque langue (texte). Les exemples de figures et de
        //     mots-concepts cités en vrac par le texte (Thésée, Romulus, Denys de Syracuse, Alexandre, Auguste ;
        //     politès/ciuis, érôs/amor, hubris/furor…) ont été répartis par langue : ce tri est le nôtre.
        //   - Chaque objet d'étude liste des sous-ensembles qui « n'ont pas vocation à être tous abordés » : tous
        //     figurent ici, le professeur choisit. En première, il peut même en définir un autre.
        //   - Portfolio : obligatoire en première (« les élèves élaborent »), facultatif en terminale (« peuvent »).
        //   - Le programme de première (2019) évoque encore une « épreuve comportant une majeure et une mineure » et
        //     une « épreuve orale terminale » : c'est dépassé. L'épreuve réelle est un écrit de 4 h (MENE2001795N,
        //     version consolidée d'août 2024). Aucune ligne n'a été tirée de ces mentions.
        //   - Les lignes « Œuvres au programme » de LATINT et GRECT ne valent que pour les sessions 2027 et 2028 :
        //     À REVOIR pour la rentrée 2028 (nouveau programme limitatif attendu au BO début 2028).
        //   - Le texte du programme limitatif écrit « Tomi » (et non Tomes) : graphie reprise.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   LATIN1 49 · GREC1 49 · LATINT 43 · GRECT 43 · total 184
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — LLCA LATIN (annexe de MENE1901582A ; notions communes : annexe 2 de MENE1901579A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] LATIN1 =
        {
            // Culture — axes qui éclairent chaque année les objets d'étude
            ("PREMIERE", "LATIN1_CONFRONTATION",          "Culture", "Confronter une œuvre latine à une œuvre moderne ou contemporaine, française ou étrangère, en dégageant analogies et différences", 1),
            ("PREMIERE", "LATIN1_MOTS_CONCEPTS",          "Culture", "Expliquer en contexte un mot-concept latin (ciuis, populus, sacer) et ce qui le sépare de son héritier français", 2),
            ("PREMIERE", "LATIN1_FIGURES",                "Culture", "Présenter une grande figure mythologique, historique ou littéraire romaine étudiée dans l'année", 3),
            ("PREMIERE", "LATIN1_FRISE",                  "Culture", "Placer sur une frise chronologique simple les grands repères et événements romains étudiés", 4),
            ("PREMIERE", "LATIN1_REPERES_GEO",            "Culture", "Situer les grands repères géographiques et culturels du monde romain en les confrontant à l'espace actuel", 5),

            // La cité entre réalités et utopies
            ("PREMIERE", "LATIN1_CITE_GOUVERNEMENTS",     "La cité entre réalités et utopies", "Distinguer monarchie, oligarchie, démocratie et tyrannie et exposer leurs bienfaits et dangers selon les Anciens", 6),
            ("PREMIERE", "LATIN1_CITE_PRINCE",            "La cité entre réalités et utopies", "Comparer des figures du « prince » idéal, du mythe à l'histoire, de Romulus à Auguste", 7),
            ("PREMIERE", "LATIN1_CITE_UTOPIES",           "La cité entre réalités et utopies", "Confronter un récit utopique antique, comme l'âge d'Or, à une dystopie ou un récit d'anticipation moderne", 8),
            ("PREMIERE", "LATIN1_CITE_CONSTRUIRE",        "La cité entre réalités et utopies", "Montrer comment les canons architecturaux de la cité antique inspirent la modernité, jusqu'à Léonard de Vinci et Le Corbusier", 9),

            // Justice des dieux, justice des hommes
            ("PREMIERE", "LATIN1_JUSTICE_LOIS",           "Justice des dieux, justice des hommes", "Expliquer le rôle des lois, écrites ou non écrites, attribuées aux dieux ou aux hommes, dans la vie de la cité", 10),
            ("PREMIERE", "LATIN1_JUSTICE_RAISON_ETAT",    "Justice des dieux, justice des hommes", "Discuter du juste et de l'injuste face à la raison d'État à partir d'un texte latin", 11),
            ("PREMIERE", "LATIN1_JUSTICE_RESPONSABILITE", "Justice des dieux, justice des hommes", "Distinguer culpabilité et responsabilité dans un récit latin et répondre à la question « à qui la faute ? »", 12),
            ("PREMIERE", "LATIN1_JUSTICE_CHATIMENTS",     "Justice des dieux, justice des hommes", "Présenter le crime et le châtiment d'une figure mythologique ou historique romaine", 13),
            ("PREMIERE", "LATIN1_JUSTICE_FUROR",          "Justice des dieux, justice des hommes", "Expliquer la notion de furor et l'opposer à la mesure à partir d'un personnage de la littérature latine", 14),

            // Amour, Amours
            ("PREMIERE", "LATIN1_AMOUR_SEDUIRE",          "Amour, Amours", "Analyser dans un texte latin une rencontre, un coup de foudre, une blessure ou une trahison amoureuse", 15),
            ("PREMIERE", "LATIN1_AMOUR_TYPES",            "Amour, Amours", "Distinguer amor, amicitia et caritas et les relier à différents types d'amour", 16),
            ("PREMIERE", "LATIN1_AMOUR_DIRE",             "Amour, Amours", "Identifier comment un document latin dit ou chante l'amour : poème, lettre, théâtre, épitaphe ou graffiti", 17),
            ("PREMIERE", "LATIN1_AMOUR_REPRESENTER",      "Amour, Amours", "Commenter une représentation de Vénus ou de Cupidon dans la littérature ou l'histoire des arts", 18),
            ("PREMIERE", "LATIN1_AMOUR_ELEGIE",           "Amour, Amours", "Mettre en regard une élégie latine et un poème d'amour contemporain, par exemple d'Apollinaire ou de Bob Dylan", 19),
            ("PREMIERE", "LATIN1_AMOUR_SPECIFICITES",     "Amour, Amours", "Montrer ce qui sépare l'amour romain de nos représentations actuelles sans plaquer les unes sur l'autre", 20),

            // Méditerranée : conflits, influences et échanges
            ("PREMIERE", "LATIN1_MED_CONQUETES",          "Méditerranée : conflits, influences et échanges", "Retracer avec des repères historiques la colonisation et les conquêtes en Méditerranée", 21),
            ("PREMIERE", "LATIN1_MED_GUERRE_PAIX",        "Méditerranée : conflits, influences et échanges", "Présenter un épisode de guerre ou de paix en Méditerranée et ses enjeux politiques, économiques ou culturels", 22),
            ("PREMIERE", "LATIN1_MED_ECHANGES",           "Méditerranée : conflits, influences et échanges", "Montrer un échange culturel ou une influence réciproque d'une rive à l'autre de la Méditerranée", 23),

            // Traduction et lecture
            ("PREMIERE", "LATIN1_TRAD_REPERER",           "Traduction", "Repérer et identifier les éléments signifiants essentiels d'un texte latin avant de le traduire", 24),
            ("PREMIERE", "LATIN1_TRAD_CONTEXTUALISEE",    "Traduction", "Proposer une traduction contextualisée d'un passage latin tiré des textes supports de l'objet d'étude", 25),
            ("PREMIERE", "LATIN1_TRAD_COMPARER",          "Traduction", "Comparer plusieurs traductions d'un même extrait latin pour éclairer le texte original", 26),
            ("PREMIERE", "LATIN1_TRAD_DICTIONNAIRE",      "Traduction", "Utiliser le dictionnaire latin-français pour choisir le sens d'un mot selon le contexte", 27),
            ("PREMIERE", "LATIN1_LECTURE_BILINGUE",       "Traduction", "Suivre la lecture d'une œuvre latine en édition bilingue et situer l'extrait dans son contexte", 28),
            ("PREMIERE", "LATIN1_ORALISER",               "Traduction", "Lire à voix haute et réciter par cœur quelques vers ou quelques lignes de latin étudiés", 29),

            // Langue — notions communes avec l'option de première
            ("PREMIERE", "LATIN1_LG_REFLECHI",            "Langue", "Reconnaître le pronom personnel réfléchi et expliquer sa syntaxe dans la phrase", 30),
            ("PREMIERE", "LATIN1_LG_INDEFINIS",           "Langue", "Reconnaître uter, nemo, nihil, solus, totus, nullus, ullus et les degrés de l'adverbe", 31),
            ("PREMIERE", "LATIN1_LG_PARTICIPE_GERONDIF",  "Langue", "Reconnaître le participe aux trois temps, le gérondif et l'adjectif verbal et les traduire", 32),
            ("PREMIERE", "LATIN1_LG_PASSIF_DEPONENTS",    "Langue", "Reconnaître la forme passive et les déponents à tous les modes et temps et analyser une phrase passive", 33),
            ("PREMIERE", "LATIN1_LG_COMPLEMENTS",         "Langue", "Identifier le complément d'agent et le complément d'objet au datif, au génitif ou à l'ablatif", 34),
            ("PREMIERE", "LATIN1_LG_QUOD_UT",             "Langue", "Distinguer et traduire les subordonnées conjonctives introduites par quod et par ut", 35),

            // Langue — notions propres à la spécialité
            ("PREMIERE", "LATIN1_LG_LOCATIF_LIEU_TEMPS",  "Langue", "Reconnaître le locatif et traduire les cas difficiles d'expression du lieu et du temps", 36),
            ("PREMIERE", "LATIN1_LG_COMPARATIFS",         "Langue", "Former comparatifs et superlatifs des adjectifs en -ilis, -dicus, -ficus, -uolus", 37),
            ("PREMIERE", "LATIN1_LG_QUANTITE",            "Langue", "Identifier les adverbes de quantité et l'expression de la quantité et les traduire", 38),
            ("PREMIERE", "LATIN1_LG_INTERJECTIONS",       "Langue", "Reconnaître les interjections latines et préciser leur valeur en contexte", 39),
            ("PREMIERE", "LATIN1_LG_SYNCOPEES",           "Langue", "Reconnaître une forme verbale syncopée et restituer sa forme pleine", 40),
            ("PREMIERE", "LATIN1_LG_PASSIF_IMPERSONNEL",  "Langue", "Reconnaître et traduire un passif impersonnel", 41),
            ("PREMIERE", "LATIN1_LG_ACC_RELATION",        "Langue", "Identifier un accusatif de relation et le traduire", 42),
            ("PREMIERE", "LATIN1_LG_QUALITE",             "Langue", "Distinguer génitif et ablatif de qualité et les traduire", 43),
            ("PREMIERE", "LATIN1_LG_DOUBLE_DATIF",        "Langue", "Analyser et traduire une construction au double datif", 44),

            // Portfolio (obligatoire en première)
            ("PREMIERE", "LATIN1_PORTFOLIO_TEXTES",       "Portfolio", "Composer un diptyque associant un texte latin authentique traduit et un texte contemporain, français ou étranger", 45),
            ("PREMIERE", "LATIN1_PORTFOLIO_IMAGES",       "Portfolio", "Composer un diptyque associant une œuvre iconographique antique et une œuvre iconographique ou filmique contemporaine", 46),
            ("PREMIERE", "LATIN1_PORTFOLIO_PRESENTER",    "Portfolio", "Présenter son portfolio : auteurs, époques, analyse des œuvres et intérêt du rapprochement choisi", 47),

            // Argumentation
            ("PREMIERE", "LATIN1_ARGUMENTER",             "Argumentation", "Argumenter à l'oral en explicitant son raisonnement pour convaincre, et savoir faire évoluer sa position", 48),
            ("PREMIERE", "LATIN1_TEXTE_PERSONNEL",        "Argumentation", "Exposer une réflexion personnelle, écrite ou orale, liée à la thématique étudiée", 49),
        };

        // -------------------------------------------------------------------------------------
        // 2. PREMIÈRE — LLCA GREC (annexe de MENE1901582A ; notions communes : annexe 2 de MENE1901579A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] GREC1 =
        {
            // Culture — axes qui éclairent chaque année les objets d'étude
            ("PREMIERE", "GREC1_CONFRONTATION",           "Culture", "Confronter une œuvre grecque à une œuvre moderne ou contemporaine, française ou étrangère, en dégageant analogies et différences", 1),
            ("PREMIERE", "GREC1_MOTS_CONCEPTS",           "Culture", "Expliquer en contexte un mot-concept grec (politès, dèmos, hiéros) et ce qui le sépare de son héritier français", 2),
            ("PREMIERE", "GREC1_FIGURES",                 "Culture", "Présenter une grande figure mythologique, historique ou littéraire grecque étudiée dans l'année", 3),
            ("PREMIERE", "GREC1_FRISE",                   "Culture", "Placer sur une frise chronologique simple les grands repères et événements du monde grec étudiés", 4),
            ("PREMIERE", "GREC1_REPERES_GEO",             "Culture", "Situer les grands repères géographiques et culturels du monde grec en les confrontant à l'espace actuel", 5),

            // La cité entre réalités et utopies
            ("PREMIERE", "GREC1_CITE_GOUVERNEMENTS",      "La cité entre réalités et utopies", "Distinguer monarchie, oligarchie, démocratie et tyrannie et exposer leurs bienfaits et dangers selon les Anciens", 6),
            ("PREMIERE", "GREC1_CITE_PRINCE",             "La cité entre réalités et utopies", "Comparer des figures du « prince » idéal, du mythe à l'histoire : Thésée, Denys de Syracuse, Alexandre le Grand", 7),
            ("PREMIERE", "GREC1_CITE_UTOPIES",            "La cité entre réalités et utopies", "Confronter la cité idéale de Platon ou l'Atlantide à une dystopie ou un récit d'anticipation moderne", 8),
            ("PREMIERE", "GREC1_CITE_CONSTRUIRE",         "La cité entre réalités et utopies", "Montrer comment la cité antique inspire la modernité, d'Hippodamos de Milet à Léonard de Vinci et Le Corbusier", 9),

            // Justice des dieux, justice des hommes
            ("PREMIERE", "GREC1_JUSTICE_LOIS",            "Justice des dieux, justice des hommes", "Expliquer le rôle des lois, écrites ou non écrites, attribuées aux dieux ou aux hommes, dans la vie de la cité", 10),
            ("PREMIERE", "GREC1_JUSTICE_RAISON_ETAT",     "Justice des dieux, justice des hommes", "Discuter du juste et de l'injuste face à la raison d'État à partir d'un texte grec", 11),
            ("PREMIERE", "GREC1_JUSTICE_RESPONSABILITE",  "Justice des dieux, justice des hommes", "Distinguer culpabilité et responsabilité dans un récit grec et répondre à la question « à qui la faute ? »", 12),
            ("PREMIERE", "GREC1_JUSTICE_CHATIMENTS",      "Justice des dieux, justice des hommes", "Présenter le crime et le châtiment d'une figure mythologique ou historique grecque", 13),
            ("PREMIERE", "GREC1_JUSTICE_HUBRIS",          "Justice des dieux, justice des hommes", "Expliquer la notion d'hubris et l'opposer à la mesure à partir d'un personnage de la littérature grecque", 14),

            // Amour, Amours
            ("PREMIERE", "GREC1_AMOUR_SEDUIRE",           "Amour, Amours", "Analyser dans un texte grec une rencontre, un coup de foudre, une blessure ou une trahison amoureuse", 15),
            ("PREMIERE", "GREC1_AMOUR_TYPES",             "Amour, Amours", "Distinguer érôs, philia et agapè et les relier à différents types d'amour", 16),
            ("PREMIERE", "GREC1_AMOUR_DIRE",              "Amour, Amours", "Identifier comment un document grec dit ou chante l'amour : poème, lettre, théâtre, épitaphe ou graffiti", 17),
            ("PREMIERE", "GREC1_AMOUR_REPRESENTER",       "Amour, Amours", "Commenter une représentation d'Aphrodite ou d'Éros dans la littérature ou l'histoire des arts", 18),
            ("PREMIERE", "GREC1_AMOUR_POEMES",            "Amour, Amours", "Mettre en regard un poème d'amour grec et un poème d'amour contemporain, par exemple d'Apollinaire ou de Bob Dylan", 19),
            ("PREMIERE", "GREC1_AMOUR_SPECIFICITES",      "Amour, Amours", "Montrer ce qui sépare l'amour grec de nos représentations actuelles sans plaquer les unes sur l'autre", 20),

            // Méditerranée : conflits, influences et échanges
            ("PREMIERE", "GREC1_MED_COLONISATION",        "Méditerranée : conflits, influences et échanges", "Retracer avec des repères historiques la colonisation et les conquêtes en Méditerranée", 21),
            ("PREMIERE", "GREC1_MED_GUERRE_PAIX",         "Méditerranée : conflits, influences et échanges", "Présenter un épisode de guerre ou de paix en Méditerranée et ses enjeux politiques, économiques ou culturels", 22),
            ("PREMIERE", "GREC1_MED_ECHANGES",            "Méditerranée : conflits, influences et échanges", "Montrer un échange culturel ou une influence réciproque d'une rive à l'autre de la Méditerranée", 23),

            // Traduction et lecture
            ("PREMIERE", "GREC1_TRAD_REPERER",            "Traduction", "Repérer et identifier les éléments signifiants essentiels d'un texte grec avant de le traduire", 24),
            ("PREMIERE", "GREC1_TRAD_CONTEXTUALISEE",     "Traduction", "Proposer une traduction contextualisée d'un passage grec tiré des textes supports de l'objet d'étude", 25),
            ("PREMIERE", "GREC1_TRAD_COMPARER",           "Traduction", "Comparer plusieurs traductions d'un même extrait grec pour éclairer le texte original", 26),
            ("PREMIERE", "GREC1_TRAD_DICTIONNAIRE",       "Traduction", "Utiliser le dictionnaire grec-français pour choisir le sens d'un mot selon le contexte", 27),
            ("PREMIERE", "GREC1_LECTURE_BILINGUE",        "Traduction", "Suivre la lecture d'une œuvre grecque en édition bilingue et situer l'extrait dans son contexte", 28),
            ("PREMIERE", "GREC1_ORALISER",                "Traduction", "Lire à voix haute et réciter par cœur quelques vers ou quelques lignes de grec étudiés", 29),

            // Langue — notions communes avec l'option de première
            ("PREMIERE", "GREC1_LG_DECLINAISONS",         "Langue", "Décliner les noms de 3e déclinaison (τεῖχος, πόλις, βασιλεύς) et les adjectifs de 2e classe (ἀληθής)", 30),
            ("PREMIERE", "GREC1_LG_PRONOMS",              "Langue", "Distinguer l'interrogatif τίς/τί, l'indéfini τις/τι et le relatif ὅς, ἥ, ὅ, et accorder le relatif", 31),
            ("PREMIERE", "GREC1_LG_PARFAIT_IMPARFAIT",    "Langue", "Reconnaître l'indicatif parfait actif, οἶδα, l'imparfait moyen-passif et le présent et l'imparfait de φημί", 32),
            ("PREMIERE", "GREC1_LG_AORISTE_PARTICIPE",    "Langue", "Reconnaître le participe aoriste actif et moyen et l'infinitif aoriste actif sigmatique et thématique", 33),
            ("PREMIERE", "GREC1_LG_CONTRACTES_ACTIF",     "Langue", "Conjuguer les verbes contractes en -έω à l'indicatif présent et imparfait actif", 34),
            ("PREMIERE", "GREC1_LG_NEGATIONS_AGENT",      "Langue", "Distinguer les négations οὐ et μή et leurs composés et identifier le complément d'agent du passif", 35),
            ("PREMIERE", "GREC1_LG_PARTICIPE_APPOSE",     "Langue", "Donner la valeur circonstancielle d'un participe apposé et traduire un génitif absolu", 36),
            ("PREMIERE", "GREC1_LG_INFINITIVE",           "Langue", "Analyser une subordonnée infinitive : verbe introducteur, temps de l'infinitif et négation", 37),
            ("PREMIERE", "GREC1_LG_SUBST_PRONOMS_WS",     "Langue", "Traduire l'adjectif ou l'infinitif substantivé, les pronoms personnels non réfléchis et ὡς + verbe (ὡς λέγεις)", 38),

            // Langue — notions propres à la spécialité
            ("PREMIERE", "GREC1_LG_TRIERES_OSTIS",        "Langue", "Décliner le type τριήρης et reconnaître les relatifs ὅστις et ὅσπερ", 39),
            ("PREMIERE", "GREC1_LG_FUTUR",                "Langue", "Conjuguer et reconnaître l'indicatif futur actif, hors futur contracte", 40),
            ("PREMIERE", "GREC1_LG_CONTRACTES_MOYEN",     "Langue", "Reconnaître l'indicatif présent et l'imparfait moyen-passif des contractes en -έω", 41),
            ("PREMIERE", "GREC1_LG_EIMI",                 "Langue", "Reconnaître l'indicatif, le participe et l'infinitif présents de εἶμι", 42),
            ("PREMIERE", "GREC1_LG_SUBSTANTIVATION",      "Langue", "Traduire la substantivation d'un participe (ὁ λέγων), d'un groupe prépositionnel ou d'un adverbe (τὰ νῦν)", 43),
            ("PREMIERE", "GREC1_LG_POLLOI",               "Langue", "Expliquer la construction de πολλοί et le sens de οἱ πολλοί et τὰ πολλά", 44),
            ("PREMIERE", "GREC1_LG_PARTICIPE_COMPLETIF",  "Langue", "Traduire le participe complétif après τυγχάνω, ἀδικέω, φαίνομαι, ἄρχομαι ou παύομαι", 45),
            ("PREMIERE", "GREC1_LG_INTERROGATION",        "Langue", "Analyser une interrogation directe : particules, pronoms, adjectifs et adverbes interrogatifs", 46),

            // Portfolio (obligatoire en première)
            ("PREMIERE", "GREC1_PORTFOLIO_TEXTES",        "Portfolio", "Composer un diptyque associant un texte grec authentique traduit et un texte contemporain, français ou étranger", 47),
            ("PREMIERE", "GREC1_PORTFOLIO_IMAGES",        "Portfolio", "Composer un diptyque associant une œuvre iconographique antique et une œuvre iconographique ou filmique contemporaine", 48),

            // Argumentation
            ("PREMIERE", "GREC1_ARGUMENTER",              "Argumentation", "Présenter son portfolio et argumenter à l'oral en explicitant son raisonnement pour convaincre", 49),
        };

        // -------------------------------------------------------------------------------------
        // 3. TERMINALE — LLCA LATIN (annexe de MENE1921257A ; œuvres : MENE2605298N ; notions communes : MENE1921267A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] LATINT =
        {
            // Œuvres au programme — sessions 2027 et 2028 seulement (MENE2605298N)
            ("TERMINALE", "LATINT_OEUVRES_CONFRONTER",    "Œuvres au programme", "Confronter les Tristes (livre III) d'Ovide et La ferme africaine de Karen Blixen : vivre à l'étranger et écrire", 1),
            ("TERMINALE", "LATINT_OVIDE_RELEGATION",      "Œuvres au programme", "Situer la relégation d'Ovide à Tomi par Auguste en 8 ap. J.-C. et la distinguer juridiquement de l'exil", 2),
            ("TERMINALE", "LATINT_OVIDE_TRISTES",         "Œuvres au programme", "Présenter les Tristes comme une œuvre personnelle composée pendant la relégation, où Ovide mourut", 3),
            ("TERMINALE", "LATINT_BLIXEN_CONTEXTE",       "Œuvres au programme", "Situer La ferme africaine : la plantation de café de Karen Blixen au Kenya et son retour ruinée au Danemark en 1931", 4),
            ("TERMINALE", "LATINT_OEUVRES_DESTIN",        "Œuvres au programme", "Relier les deux œuvres à l'objet d'étude « L'homme, le monde, le destin » : deux destins que la vie n'a pas épargnés", 5),
            ("TERMINALE", "LATINT_OEUVRES_COMMENTER",     "Œuvres au programme", "Lire en latin, situer et commenter un passage des Tristes en le mettant en perspective avec La ferme africaine", 6),

            // L'homme, le monde, le destin
            ("TERMINALE", "LATINT_HMD_COSMOGONIES",       "L'homme, le monde, le destin", "Comparer des récits de genèse et de cosmogonie et la figure du démiurge", 7),
            ("TERMINALE", "LATINT_HMD_DESTIN",            "L'homme, le monde, le destin", "Distinguer hasard, nécessité, providence et destin (fatalité, prédestination, détermination) dans un texte", 8),
            ("TERMINALE", "LATINT_HMD_VOIX_DESTIN",       "L'homme, le monde, le destin", "Analyser une parole prophétique, oracle, prédiction ou rêve, qui fait entendre le destin", 9),
            ("TERMINALE", "LATINT_HMD_FAMILLES_MAUDITES", "L'homme, le monde, le destin", "Montrer comment, au théâtre ou dans les mythes, le destin s'exerce sur des héros et des familles maudites", 10),
            ("TERMINALE", "LATINT_HMD_THEATRUM_MUNDI",    "L'homme, le monde, le destin", "Expliquer la métaphore du theatrum mundi et le jeu de la vérité et de l'illusion", 11),

            // Croire, savoir, douter
            ("TERMINALE", "LATINT_CSD_MAGIE",             "Croire, savoir, douter", "Décrire des pratiques magiques gréco-romaines d'après les textes et des supports : amulettes, papyri, tablettes d'exécration", 12),
            ("TERMINALE", "LATINT_CSD_PENSEE_RATIONNELLE","Croire, savoir, douter", "Expliquer comment la pensée logique et scientifique propose une lecture du monde fondée sur la raison", 13),
            ("TERMINALE", "LATINT_CSD_MAITRES_DISCIPLES", "Croire, savoir, douter", "Présenter une relation entre maître et disciple et la transmission d'un savoir encyclopédique", 14),
            ("TERMINALE", "LATINT_CSD_RELIGIONS",         "Croire, savoir, douter", "Distinguer cultes polythéistes, cultes à mystères et monothéismes jusqu'à l'avènement du christianisme", 15),

            // Méditerranée : présence des mondes antiques
            ("TERMINALE", "LATINT_MED_SITES",             "Méditerranée : présence des mondes antiques", "Présenter un site archéologique méditerranéen et ce qu'il révèle du monde romain", 16),
            ("TERMINALE", "LATINT_MED_VILLES",            "Méditerranée : présence des mondes antiques", "Retracer les transformations d'une grande ville antique de Méditerranée jusqu'à aujourd'hui", 17),
            ("TERMINALE", "LATINT_MED_LIEUX_SAVOIR",      "Méditerranée : présence des mondes antiques", "Présenter un lieu de culture ou une figure du savoir : bibliothèque, école, philosophe ou savant", 18),
            ("TERMINALE", "LATINT_MED_ART",               "Méditerranée : présence des mondes antiques", "Comparer un modèle de l'art romain à une expression artistique moderne ou contemporaine", 19),

            // Culture — axes qui éclairent chaque année les objets d'étude
            ("TERMINALE", "LATINT_CONFRONTATION",         "Culture", "Confronter une œuvre latine à une œuvre médiévale, moderne ou contemporaine, française ou étrangère", 20),
            ("TERMINALE", "LATINT_MOTS_CONCEPTS",         "Culture", "Expliquer en contexte un mot-concept latin (natura, ars) et ce qui le sépare de son héritier français", 21),
            ("TERMINALE", "LATINT_FIGURES",               "Culture", "Présenter une grande figure mythologique, historique ou littéraire romaine liée aux objets d'étude", 22),
            ("TERMINALE", "LATINT_FRISE",                 "Culture", "Placer sur une frise chronologique simple les grands repères historiques des œuvres étudiées", 23),

            // Traduction
            ("TERMINALE", "LATINT_TRAD_EXTRAIT",          "Traduction", "Traduire avec le dictionnaire latin-français un extrait d'environ 90 mots de l'œuvre antique au programme", 24),
            ("TERMINALE", "LATINT_TRAD_PERSONNELLE",      "Traduction", "Proposer une traduction précise et personnelle d'un court extrait latin en se détachant de la traduction fournie", 25),
            ("TERMINALE", "LATINT_TRAD_REPERER",          "Traduction", "Repérer les éléments signifiants essentiels d'un texte latin avant d'en proposer la traduction", 26),
            ("TERMINALE", "LATINT_TRAD_COMPARER",         "Traduction", "Comparer des traductions d'un même passage latin pour éclairer le texte original", 27),

            // Langue — questions de l'épreuve
            ("TERMINALE", "LATINT_FAIT_DE_LANGUE",        "Langue", "Identifier un fait de langue dans un texte latin puis interpréter ce qu'il apporte au sens", 28),
            ("TERMINALE", "LATINT_LEXIQUE_EN_CONTEXTE",   "Langue", "Expliciter le sens en contexte d'une notion clé du texte latin liée à l'objet d'étude", 29),

            // Langue — notions communes avec l'option de terminale
            ("TERMINALE", "LATINT_LG_INDEFINIS_NUMERAUX", "Langue", "Reconnaître quisque, unusquisque, alius, alter, uterque et les numéraux cardinaux et ordinaux courants", 30),
            ("TERMINALE", "LATINT_LG_IRREGULIERS",        "Langue", "Conjuguer et reconnaître les verbes irréguliers eo, fero, uolo, nolo, malo et fio", 31),
            ("TERMINALE", "LATINT_LG_SEMI_DEPONENTS",     "Langue", "Reconnaître semi-déponents (audeo, gaudeo, soleo, fido) et verbes non-personnels (decet, libet, licet, paenitet, pudet)", 32),
            ("TERMINALE", "LATINT_LG_INTERROGATION",      "Langue", "Analyser l'interrogation directe (-ne, num, nonne, an, utrum…an) et la subordonnée interrogative indirecte", 33),
            ("TERMINALE", "LATINT_LG_UIDEOR_SUPIN",       "Langue", "Traduire les emplois de uideor, le supin et la double négation", 34),
            ("TERMINALE", "LATINT_LG_SUBORDONNEES",       "Langue", "Distinguer la relative au subjonctif et les circonstancielles de but, de conséquence et de comparaison", 35),

            // Langue — notions propres à la spécialité
            ("TERMINALE", "LATINT_LG_QUICUMQUE",          "Langue", "Reconnaître les relatifs indéfinis quicumque et quisquis et les déterminants exclamatifs", 36),
            ("TERMINALE", "LATINT_LG_PARFAITS_SANS_PRES", "Langue", "Traduire les parfaits sans présent memini, noui, odi", 37),
            ("TERMINALE", "LATINT_LG_AGE",                "Langue", "Reconnaître et traduire l'expression de l'âge en latin", 38),
            ("TERMINALE", "LATINT_LG_STYLE_INDIRECT",     "Langue", "Repérer le style indirect latin et le restituer en français", 39),
            ("TERMINALE", "LATINT_LG_ATTRACTION_MODALE",  "Langue", "Expliquer une attraction modale et en tenir compte dans la traduction", 40),

            // Argumentation et portfolio
            ("TERMINALE", "LATINT_ESSAI",                 "Argumentation", "Construire un essai organisé et argumenté sur trois textes en s'appuyant sur les deux œuvres au programme", 41),
            ("TERMINALE", "LATINT_ENTRETIEN",             "Argumentation", "Répondre avec pertinence, dans un langage clair et structuré, aux questions sur un commentaire et une traduction", 42),
            ("TERMINALE", "LATINT_PORTFOLIO",             "Portfolio", "Présenter un portfolio qui confronte textes ou œuvres antiques et modernes, et justifier ses choix", 43),
        };

        // -------------------------------------------------------------------------------------
        // 4. TERMINALE — LLCA GREC (annexe de MENE1921257A ; œuvres : MENE2605298N ; notions communes : MENE1921267A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] GRECT =
        {
            // Œuvres au programme — sessions 2027 et 2028 seulement (MENE2605298N)
            ("TERMINALE", "GRECT_OEUVRES_CONFRONTER",     "Œuvres au programme", "Confronter les Histoires vraies de Lucien et Le baron perché d'Italo Calvino, voyages à travers mondes réels et imaginaires", 1),
            ("TERMINALE", "GRECT_OEUVRES_THEATRE_MONDE",  "Œuvres au programme", "Relier les deux œuvres au sous-ensemble « Le grand théâtre du monde : vérité et illusion »", 2),
            ("TERMINALE", "GRECT_OEUVRES_REGARD",         "Œuvres au programme", "Analyser le regard amusé ou critique que les héros posent de loin sur leurs contemporains", 3),
            ("TERMINALE", "GRECT_CALVINO_CONTEXTE",       "Œuvres au programme", "Situer Le baron perché, second volet de Nos ancêtres, entre Grèce antique de Lucien et Europe des Lumières", 4),
            ("TERMINALE", "GRECT_OEUVRES_ACTEURS",        "Œuvres au programme", "Montrer que les héros sont à la fois observateurs et acteurs des mondes traversés : rencontres, amours, guerres", 5),
            ("TERMINALE", "GRECT_OEUVRES_COMMENTER",      "Œuvres au programme", "Lire en grec, situer et commenter un passage des Histoires vraies en le mettant en perspective avec Le baron perché", 6),

            // L'homme, le monde, le destin
            ("TERMINALE", "GRECT_HMD_COSMOGONIES",        "L'homme, le monde, le destin", "Comparer des récits de genèse et de cosmogonie et la figure du démiurge", 7),
            ("TERMINALE", "GRECT_HMD_DESTIN",             "L'homme, le monde, le destin", "Distinguer hasard, nécessité, providence et destin (fatalité, prédestination, détermination) dans un texte", 8),
            ("TERMINALE", "GRECT_HMD_VOIX_DESTIN",        "L'homme, le monde, le destin", "Analyser une parole prophétique, oracle, prédiction ou rêve, qui fait entendre le destin", 9),
            ("TERMINALE", "GRECT_HMD_FAMILLES_MAUDITES",  "L'homme, le monde, le destin", "Montrer comment, au théâtre ou dans les mythes, le destin s'exerce sur des héros et des familles maudites", 10),
            ("TERMINALE", "GRECT_HMD_THEATRE_DU_MONDE",   "L'homme, le monde, le destin", "Expliquer la métaphore du « grand théâtre du monde » et le jeu de la vérité et de l'illusion", 11),

            // Croire, savoir, douter
            ("TERMINALE", "GRECT_CSD_MAGIE",              "Croire, savoir, douter", "Décrire des pratiques magiques gréco-romaines d'après les textes et des supports : amulettes, papyri, tablettes d'exécration", 12),
            ("TERMINALE", "GRECT_CSD_PENSEE_RATIONNELLE", "Croire, savoir, douter", "Expliquer comment la pensée logique et scientifique propose une lecture du monde fondée sur la raison", 13),
            ("TERMINALE", "GRECT_CSD_MAITRES_DISCIPLES",  "Croire, savoir, douter", "Présenter une relation entre maître et disciple et la transmission d'un savoir encyclopédique", 14),
            ("TERMINALE", "GRECT_CSD_RELIGIONS",          "Croire, savoir, douter", "Distinguer cultes polythéistes, cultes à mystères et monothéismes jusqu'à l'avènement du christianisme", 15),

            // Méditerranée : présence des mondes antiques
            ("TERMINALE", "GRECT_MED_SITES",              "Méditerranée : présence des mondes antiques", "Présenter un site archéologique méditerranéen et ce qu'il révèle du monde grec", 16),
            ("TERMINALE", "GRECT_MED_VILLES",             "Méditerranée : présence des mondes antiques", "Retracer les transformations d'une grande ville antique de Méditerranée jusqu'à aujourd'hui", 17),
            ("TERMINALE", "GRECT_MED_LIEUX_SAVOIR",       "Méditerranée : présence des mondes antiques", "Présenter un lieu de culture ou une figure du savoir : bibliothèque, école, philosophe ou savant", 18),
            ("TERMINALE", "GRECT_MED_ART",                "Méditerranée : présence des mondes antiques", "Comparer un modèle de l'art grec à une expression artistique moderne ou contemporaine", 19),

            // Culture — axes qui éclairent chaque année les objets d'étude
            ("TERMINALE", "GRECT_CONFRONTATION",          "Culture", "Confronter une œuvre grecque à une œuvre médiévale, moderne ou contemporaine, française ou étrangère", 20),
            ("TERMINALE", "GRECT_MOTS_CONCEPTS",          "Culture", "Expliquer en contexte un mot-concept grec (phusis, technè) et ce qui le sépare de son héritier français", 21),
            ("TERMINALE", "GRECT_FIGURES",                "Culture", "Présenter une grande figure mythologique, historique ou littéraire grecque liée aux objets d'étude", 22),
            ("TERMINALE", "GRECT_FRISE",                  "Culture", "Placer sur une frise chronologique simple les grands repères historiques des œuvres étudiées", 23),

            // Traduction
            ("TERMINALE", "GRECT_TRAD_EXTRAIT",           "Traduction", "Traduire avec le dictionnaire grec-français un extrait d'environ 90 mots de l'œuvre antique au programme", 24),
            ("TERMINALE", "GRECT_TRAD_PERSONNELLE",       "Traduction", "Proposer une traduction précise et personnelle d'un court extrait grec en se détachant de la traduction fournie", 25),
            ("TERMINALE", "GRECT_TRAD_REPERER",           "Traduction", "Repérer les éléments signifiants essentiels d'un texte grec avant d'en proposer la traduction", 26),
            ("TERMINALE", "GRECT_TRAD_COMPARER",          "Traduction", "Comparer des traductions d'un même passage grec pour éclairer le texte original", 27),

            // Langue — questions de l'épreuve
            ("TERMINALE", "GRECT_FAIT_DE_LANGUE",         "Langue", "Identifier un fait de langue dans un texte grec puis interpréter ce qu'il apporte au sens", 28),
            ("TERMINALE", "GRECT_LEXIQUE_EN_CONTEXTE",    "Langue", "Expliciter le sens en contexte d'une notion clé du texte grec liée à l'objet d'étude", 29),

            // Langue — notions communes avec l'option de terminale
            ("TERMINALE", "GRECT_LG_DECLINAISON_COMPAR",  "Langue", "Décliner πατήρ et ἀνήρ et reconnaître les comparatifs et superlatifs irréguliers courants (ἀγαθός, μέγας, πολύς…)", 30),
            ("TERMINALE", "GRECT_LG_AORISTE_MOYEN_PASSIF","Langue", "Reconnaître l'indicatif aoriste moyen et passif, le participe aoriste passif et l'infinitif aoriste moyen et passif", 31),
            ("TERMINALE", "GRECT_LG_CONTRACTES_FUTUR_AOR","Langue", "Conjuguer au futur et à l'aoriste les verbes contractes en -έω", 32),
            ("TERMINALE", "GRECT_LG_SUBJONCTIF_ASPECT",   "Langue", "Repérer le subjonctif présent et aoriste, actif et moyen, et expliquer la valeur des modes et l'aspect", 33),
            ("TERMINALE", "GRECT_LG_AUTOS_TEMPS",         "Langue", "Distinguer les trois emplois de αὐτός et les compléments de temps exprimant la durée et la date", 34),
            ("TERMINALE", "GRECT_LG_BUT_CONSEQUENCE",     "Langue", "Distinguer et traduire les circonstancielles de but et de conséquence", 35),
            ("TERMINALE", "GRECT_LG_BILANS",              "Langue", "Faire le bilan des négations, des emplois du participe et des principales particules de liaison", 36),

            // Langue — notions propres à la spécialité
            ("TERMINALE", "GRECT_LG_ADJ_US_REFLECHIS",    "Langue", "Repérer les adjectifs en -ύς (ἡδύς, ἡδεῖα, ἡδύ) et les pronoms personnels réfléchis en emploi direct", 37),
            ("TERMINALE", "GRECT_LG_OPTATIF",             "Langue", "Reconnaître l'optatif présent actif (λύοιμι) et l'optatif aoriste actif sigmatique (λύσαιμι) et thématique (λίποιμι)", 38),
            ("TERMINALE", "GRECT_LG_ACC_RELATION",        "Langue", "Identifier un accusatif de relation et le traduire", 39),
            ("TERMINALE", "GRECT_LG_EVENTUEL",            "Langue", "Traduire l'éventuel : le futur dans les subordonnées temporelles, hypothétiques et relatives", 40),
            ("TERMINALE", "GRECT_LG_SOUHAIT_POTENTIEL",   "Langue", "Distinguer et traduire l'expression du souhait et celle du potentiel", 41),

            // Argumentation et portfolio
            ("TERMINALE", "GRECT_ESSAI",                  "Argumentation", "Construire un essai organisé et argumenté sur trois textes en s'appuyant sur les deux œuvres au programme", 42),
            ("TERMINALE", "GRECT_ENTRETIEN",              "Argumentation", "Répondre avec pertinence, dans un langage clair et structuré, aux questions sur un commentaire et une traduction", 43),
        };
    }
}
