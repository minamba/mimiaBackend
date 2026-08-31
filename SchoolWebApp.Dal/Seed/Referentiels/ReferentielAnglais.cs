namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel d'anglais, du CP à la terminale.
    ///
    /// L'ÉCHELLE EST CELLE DU CECRL, PAS CELLE DE LA CLASSE
    /// ---------------------------------------------------
    /// En langue vivante, le programme n'énumère pas des chapitres mais des
    /// niveaux : A1 attendu en fin de CM2, A2 en fin de troisième, B2 en fin de
    /// terminale. Un même geste — se présenter, raconter, argumenter — revient
    /// donc d'année en année, mais sur des textes plus longs et une langue plus
    /// exigeante. Les libellés portent cette progression : « en quelques
    /// phrases » en sixième, « en cinq minutes sans notes » en terminale.
    ///
    /// PEU DE COMPÉTENCES AU PRIMAIRE, ET C'EST NORMAL
    /// ----------------------------------------------
    /// L'anglais du CP au CE2 est presque entièrement oral et se compte en
    /// dizaines de minutes par semaine. Gonfler artificiellement ces niveaux
    /// produirait des lacunes imaginaires sur des choses que l'école n'évalue
    /// pas encore.
    /// </summary>
    public static class ReferentielAnglais
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- CP : l'oreille d'abord ---
                ("CP", "AN_CP_ORAL_SALUER",     "Expression orale", "Saluer et prendre congé en anglais", 1),
                ("CP", "AN_CP_ORAL_PRENOM",     "Expression orale", "Dire son prénom et son âge", 2),
                ("CP", "AN_CP_LEX_COULEURS",    "Lexique", "Nommer les couleurs", 3),
                ("CP", "AN_CP_LEX_NOMBRES",     "Lexique", "Compter jusqu'à douze", 4),
                ("CP", "AN_CP_LEX_ANIMAUX",     "Lexique", "Nommer des animaux familiers", 5),
                ("CP", "AN_CP_COMP_CONSIGNES",  "Compréhension", "Réagir à une consigne simple de classe", 6),
                ("CP", "AN_CP_PHONO_SONS",      "Phonologie", "Répéter un mot en respectant les sons anglais", 7),
                ("CP", "AN_CP_CULT_FETES",      "Culture", "Reconnaître une fête du monde anglophone", 8),

                // --- CE1 ---
                ("CE1", "AN_CE1_ORAL_PRESENTER", "Expression orale", "Se présenter en trois phrases", 1),
                ("CE1", "AN_CE1_ORAL_GOUTS",     "Expression orale", "Dire ce qu'on aime et ce qu'on n'aime pas", 2),
                ("CE1", "AN_CE1_LEX_FAMILLE",    "Lexique", "Nommer les membres de la famille", 3),
                ("CE1", "AN_CE1_LEX_NOMBRES",    "Lexique", "Compter jusqu'à vingt", 4),
                ("CE1", "AN_CE1_LEX_CORPS",      "Lexique", "Nommer les parties du corps et les vêtements", 5),
                ("CE1", "AN_CE1_COMP_HISTOIRE",  "Compréhension", "Comprendre une histoire courte accompagnée d'images", 6),
                ("CE1", "AN_CE1_GRAM_ETRE",      "Grammaire", "Employer I am, you are, he is, she is", 7),
                ("CE1", "AN_CE1_PHONO_ACCENT",   "Phonologie", "Placer l'accent au bon endroit dans un mot courant", 8),
                ("CE1", "AN_CE1_CULT_PAYS",      "Culture", "Situer les principaux pays anglophones", 9),

                // --- CE2 ---
                ("CE2", "AN_CE2_ORAL_QUESTIONS", "Expression orale", "Poser et répondre à une question simple", 1),
                ("CE2", "AN_CE2_ORAL_DECRIRE",   "Expression orale", "Décrire une image en quelques phrases", 2),
                ("CE2", "AN_CE2_LEX_ECOLE",      "Lexique", "Nommer les objets de la classe et les matières", 3),
                ("CE2", "AN_CE2_LEX_TEMPS",      "Lexique", "Dire les jours, les mois et la date", 4),
                ("CE2", "AN_CE2_COMP_DIALOGUE",  "Compréhension", "Comprendre un dialogue court sur un sujet connu", 5),
                ("CE2", "AN_CE2_GRAM_AVOIR",     "Grammaire", "Employer have got pour dire ce qu'on possède", 6),
                ("CE2", "AN_CE2_GRAM_PLURIEL",   "Grammaire", "Former le pluriel des noms courants", 7),
                ("CE2", "AN_CE2_ECR_COPIE",      "Expression écrite", "Copier et compléter des phrases modèles", 8),
                ("CE2", "AN_CE2_PHONO_ALPHABET", "Phonologie", "Épeler un mot en anglais", 9),

                // --- CM1 : vers le niveau A1 ---
                ("CM1", "AN_CM1_ORAL_PRESENTER", "Expression orale", "Se présenter et présenter quelqu'un d'autre", 1),
                ("CM1", "AN_CM1_ORAL_ROUTINE",   "Expression orale", "Raconter sa journée type", 2),
                ("CM1", "AN_CM1_COMP_CONSIGNES", "Compréhension", "Comprendre des consignes et des indications de lieu", 3),
                ("CM1", "AN_CM1_COMP_TEXTE",     "Compréhension", "Comprendre un texte court illustré", 4),
                ("CM1", "AN_CM1_GRAM_PRESENT",   "Grammaire", "Conjuguer un verbe au présent simple, y compris le -s de la 3e personne", 5),
                ("CM1", "AN_CM1_GRAM_CAN",       "Grammaire", "Employer can pour dire ce qu'on sait faire", 6),
                ("CM1", "AN_CM1_GRAM_QUESTIONS", "Grammaire", "Poser une question avec do et does", 7),
                ("CM1", "AN_CM1_LEX_QUOTIDIEN",  "Lexique", "Employer le lexique des loisirs, des repas et de la maison", 8),
                ("CM1", "AN_CM1_ECR_PHRASES",    "Expression écrite", "Écrire cinq phrases sur soi ou sur un proche", 9),
                ("CM1", "AN_CM1_PHONO_INTONATION","Phonologie", "Distinguer l'intonation d'une question et d'une affirmation", 10),
                ("CM1", "AN_CM1_CULT_ECOLE",     "Culture", "Comparer l'école anglaise ou américaine à la sienne", 11),

                // --- CM2 : le A1 doit être acquis ---
                ("CM2", "AN_CM2_ORAL_DIALOGUE",  "Expression orale", "Tenir un dialogue simple sur un sujet familier", 1),
                ("CM2", "AN_CM2_ORAL_RACONTER",  "Expression orale", "Raconter un événement passé en quelques phrases", 2),
                ("CM2", "AN_CM2_COMP_ORAL",      "Compréhension", "Comprendre l'essentiel d'un enregistrement court et lent", 3),
                ("CM2", "AN_CM2_COMP_ECRIT",     "Compréhension", "Comprendre un texte court sans image", 4),
                ("CM2", "AN_CM2_GRAM_PRETERIT",  "Grammaire", "Employer le prétérit des verbes réguliers et de be", 5),
                ("CM2", "AN_CM2_GRAM_BE_ING",    "Grammaire", "Employer be + -ing pour dire ce qui se passe maintenant", 6),
                ("CM2", "AN_CM2_GRAM_NEGATION",  "Grammaire", "Construire une phrase négative avec don't et doesn't", 7),
                ("CM2", "AN_CM2_GRAM_ADJECTIFS", "Grammaire", "Placer l'adjectif avant le nom sans l'accorder", 8),
                ("CM2", "AN_CM2_ECR_TEXTE",      "Expression écrite", "Écrire un court texte de présentation ou une carte postale", 9),
                ("CM2", "AN_CM2_LEX_VILLE",      "Lexique", "Employer le lexique de la ville et des déplacements", 10),
                ("CM2", "AN_CM2_CULT_TRADITIONS","Culture", "Décrire une tradition d'un pays anglophone", 11),

                // --- 6e : consolider A1, entrer dans A2 ---
                ("SIXIEME", "AN_6E_ORAL_CONTINU",  "Expression orale", "Parler une minute en continu sur un sujet préparé", 1),
                ("SIXIEME", "AN_6E_ORAL_DIALOGUER","Expression orale", "Réagir dans un dialogue sans préparation", 2),
                ("SIXIEME", "AN_6E_COMP_ORAL",     "Compréhension", "Comprendre les informations essentielles d'un document audio", 3),
                ("SIXIEME", "AN_6E_COMP_ECRIT",    "Compréhension", "Repérer les informations clés d'un texte court", 4),
                ("SIXIEME", "AN_6E_GRAM_PRESENT",  "Grammaire", "Distinguer le présent simple du présent en -ing", 5),
                ("SIXIEME", "AN_6E_GRAM_PRETERIT", "Grammaire", "Employer le prétérit, y compris les verbes irréguliers courants", 6),
                ("SIXIEME", "AN_6E_GRAM_AUXILIAIRE","Grammaire", "Construire questions et négations avec le bon auxiliaire", 7),
                ("SIXIEME", "AN_6E_GRAM_POSSESSION","Grammaire", "Exprimer la possession avec le génitif et les possessifs", 8),
                ("SIXIEME", "AN_6E_GRAM_MODAUX",   "Grammaire", "Employer can, must et have to", 9),
                ("SIXIEME", "AN_6E_GRAM_AGE",      "Grammaire", "Dire son âge avec be et non avec have", 10),
                ("SIXIEME", "AN_6E_ECR_TEXTE",     "Expression écrite", "Rédiger un texte d'une dizaine de lignes sur un sujet connu", 11),
                ("SIXIEME", "AN_6E_LEX_DESCRIPTION","Lexique", "Décrire une personne : physique, caractère, goûts", 12),
                ("SIXIEME", "AN_6E_LEX_FAUX_AMIS", "Lexique", "Éviter les faux amis les plus fréquents", 13),
                ("SIXIEME", "AN_6E_PHONO_TERMINAISON","Phonologie", "Prononcer les terminaisons -s et -ed", 14),
                ("SIXIEME", "AN_6E_CULT_ROYAUME",  "Culture", "Situer les nations du Royaume-Uni et leurs symboles", 15),

                // --- 5e ---
                ("CINQUIEME", "AN_5E_ORAL_CONTINU",  "Expression orale", "Parler deux minutes en continu à partir de notes", 1),
                ("CINQUIEME", "AN_5E_ORAL_OPINION",  "Expression orale", "Donner son avis et le justifier avec because", 2),
                ("CINQUIEME", "AN_5E_COMP_ORAL",     "Compréhension", "Comprendre un document audio authentique court", 3),
                ("CINQUIEME", "AN_5E_COMP_ECRIT",    "Compréhension", "Comprendre un récit ou un article simple", 4),
                ("CINQUIEME", "AN_5E_GRAM_FUTUR",    "Grammaire", "Exprimer le futur avec will et be going to", 5),
                ("CINQUIEME", "AN_5E_GRAM_COMPARATIF","Grammaire", "Former le comparatif et le superlatif des adjectifs", 6),
                ("CINQUIEME", "AN_5E_GRAM_QUANTITE", "Grammaire", "Employer some, any, much, many et les indénombrables", 7),
                ("CINQUIEME", "AN_5E_GRAM_PRETERIT_ING","Grammaire", "Employer le prétérit en -ing pour une action en cours", 8),
                ("CINQUIEME", "AN_5E_GRAM_IMPERATIF","Grammaire", "Donner un conseil ou un ordre", 9),
                ("CINQUIEME", "AN_5E_ECR_RECIT",     "Expression écrite", "Rédiger un récit au passé d'une quinzaine de lignes", 10),
                ("CINQUIEME", "AN_5E_ECR_LETTRE",    "Expression écrite", "Rédiger un message ou une lettre informelle", 11),
                ("CINQUIEME", "AN_5E_LEX_VOYAGE",    "Lexique", "Employer le lexique du voyage et de l'alimentation", 12),
                ("CINQUIEME", "AN_5E_PHONO_ACCENT",  "Phonologie", "Placer l'accent tonique dans les mots de deux syllabes", 13),
                ("CINQUIEME", "AN_5E_CULT_ETATS_UNIS","Culture", "Situer les grands repères géographiques des États-Unis", 14),

                // --- 4e ---
                ("QUATRIEME", "AN_4E_ORAL_CONTINU",  "Expression orale", "Présenter un document en trois minutes sans le lire", 1),
                ("QUATRIEME", "AN_4E_ORAL_DEBAT",    "Expression orale", "Participer à un débat simple et nuancer son propos", 2),
                ("QUATRIEME", "AN_4E_COMP_ORAL",     "Compréhension", "Comprendre un reportage ou une interview courte", 3),
                ("QUATRIEME", "AN_4E_COMP_ECRIT",    "Compréhension", "Comprendre un texte littéraire ou journalistique adapté", 4),
                ("QUATRIEME", "AN_4E_GRAM_PRESENT_PERFECT","Grammaire", "Employer le present perfect et le distinguer du prétérit", 5),
                ("QUATRIEME", "AN_4E_GRAM_SINCE_FOR","Grammaire", "Employer since et for pour la durée", 6),
                ("QUATRIEME", "AN_4E_GRAM_RELATIVES","Grammaire", "Relier deux phrases avec who, which et that", 7),
                ("QUATRIEME", "AN_4E_GRAM_MODAUX",   "Grammaire", "Nuancer avec should, might et could", 8),
                ("QUATRIEME", "AN_4E_GRAM_PASSIF",   "Grammaire", "Comprendre et former une phrase à la voix passive", 9),
                ("QUATRIEME", "AN_4E_ECR_ARTICLE",   "Expression écrite", "Rédiger un article ou un récit d'une vingtaine de lignes", 10),
                ("QUATRIEME", "AN_4E_ECR_ARGUMENTER","Expression écrite", "Rédiger un texte qui défend un point de vue", 11),
                ("QUATRIEME", "AN_4E_LEX_MEDIAS",    "Lexique", "Employer le lexique des médias et de l'environnement", 12),
                ("QUATRIEME", "AN_4E_LEX_CONNECTEURS","Lexique", "Relier ses idées avec however, therefore, although", 13),
                ("QUATRIEME", "AN_4E_CULT_HISTOIRE", "Culture", "Situer un moment marquant de l'histoire du monde anglophone", 14),

                // --- 3e : le A2 est exigé au brevet ---
                ("TROISIEME", "AN_3E_ORAL_EXPOSE",   "Expression orale", "Présenter un exposé de cinq minutes et répondre aux questions", 1),
                ("TROISIEME", "AN_3E_ORAL_INTERAGIR","Expression orale", "Soutenir une conversation sur un sujet d'actualité", 2),
                ("TROISIEME", "AN_3E_COMP_ORAL",     "Compréhension", "Comprendre un document audio authentique de deux minutes", 3),
                ("TROISIEME", "AN_3E_COMP_ECRIT",    "Compréhension", "Comprendre un texte long et en dégager le thème et le ton", 4),
                ("TROISIEME", "AN_3E_GRAM_TEMPS",    "Grammaire", "Choisir le temps juste entre présent, prétérit et present perfect", 5),
                ("TROISIEME", "AN_3E_GRAM_CONDITIONNEL","Grammaire", "Construire les phrases hypothétiques avec if", 6),
                ("TROISIEME", "AN_3E_GRAM_DISCOURS", "Grammaire", "Rapporter les paroles de quelqu'un", 7),
                ("TROISIEME", "AN_3E_GRAM_PASSIF",   "Grammaire", "Employer la voix passive à plusieurs temps", 8),
                ("TROISIEME", "AN_3E_GRAM_INFINITIF","Grammaire", "Choisir entre infinitif et forme en -ing après un verbe", 9),
                ("TROISIEME", "AN_3E_ECR_TEXTE",     "Expression écrite", "Rédiger un texte structuré de trente lignes", 10),
                ("TROISIEME", "AN_3E_ECR_RELIRE",    "Expression écrite", "Relire son texte en ciblant les erreurs qu'on fait le plus", 11),
                ("TROISIEME", "AN_3E_LEX_SOCIETE",   "Lexique", "Employer le lexique du travail, des inégalités et de l'engagement", 12),
                ("TROISIEME", "AN_3E_PHONO_MOTS",    "Phonologie", "Prononcer les mots dont l'accent change le sens", 13),
                ("TROISIEME", "AN_3E_CULT_DIVERSITE","Culture", "Décrire la diversité culturelle du monde anglophone", 14),

                // --- Seconde : vers B1 ---
                ("SECONDE", "AN_2DE_ORAL_CONTINU",  "Expression orale", "S'exprimer cinq minutes en continu de façon organisée", 1),
                ("SECONDE", "AN_2DE_ORAL_INTERAGIR","Expression orale", "Réagir spontanément et relancer un échange", 2),
                ("SECONDE", "AN_2DE_COMP_ORAL",     "Compréhension", "Comprendre un document audio ou vidéo authentique", 3),
                ("SECONDE", "AN_2DE_COMP_ECRIT",    "Compréhension", "Comprendre un article de presse et en dégager la thèse", 4),
                ("SECONDE", "AN_2DE_COMP_IMPLICITE","Compréhension", "Repérer l'implicite et le ton d'un document", 5),
                ("SECONDE", "AN_2DE_GRAM_TEMPS",    "Grammaire", "Maîtriser le système des temps du passé", 6),
                ("SECONDE", "AN_2DE_GRAM_MODAUX",   "Grammaire", "Exprimer la certitude et le doute avec les modaux", 7),
                ("SECONDE", "AN_2DE_GRAM_COMPLEXE", "Grammaire", "Construire une phrase complexe avec plusieurs subordonnées", 8),
                ("SECONDE", "AN_2DE_ECR_ESSAI",     "Expression écrite", "Rédiger un texte argumenté de deux cents mots", 9),
                ("SECONDE", "AN_2DE_ECR_CREATIF",   "Expression écrite", "Rédiger un texte de création à partir d'un document", 10),
                ("SECONDE", "AN_2DE_LEX_PRECISION", "Lexique", "Remplacer les mots passe-partout par un lexique précis", 11),
                ("SECONDE", "AN_2DE_LEX_IDIOMES",   "Lexique", "Employer des expressions idiomatiques courantes", 12),
                ("SECONDE", "AN_2DE_METH_SYNTHESE", "Méthode", "Rendre compte d'un document sans le paraphraser", 13),
                ("SECONDE", "AN_2DE_CULT_AXES",     "Culture", "Traiter un axe du programme à partir de documents variés", 14),

                // --- Première : B1 consolidé, entrée en B2 ---
                ("PREMIERE", "AN_1RE_ORAL_PRESENTER","Expression orale", "Présenter et mettre en relation deux documents à l'oral", 1),
                ("PREMIERE", "AN_1RE_ORAL_DEBAT",   "Expression orale", "Débattre en anticipant et réfutant les objections", 2),
                ("PREMIERE", "AN_1RE_COMP_ORAL",    "Compréhension", "Comprendre un document audio long au débit naturel", 3),
                ("PREMIERE", "AN_1RE_COMP_ECRIT",   "Compréhension", "Analyser un texte littéraire ou argumentatif authentique", 4),
                ("PREMIERE", "AN_1RE_COMP_CROISER", "Compréhension", "Mettre en relation plusieurs documents sur un même axe", 5),
                ("PREMIERE", "AN_1RE_GRAM_NUANCE",  "Grammaire", "Nuancer un propos par les modaux et les adverbes", 6),
                ("PREMIERE", "AN_1RE_GRAM_HYPOTHESE","Grammaire", "Employer les trois types d'hypothèse, y compris l'irréel du passé", 7),
                ("PREMIERE", "AN_1RE_GRAM_ARTICULER","Grammaire", "Articuler un raisonnement avec des connecteurs variés", 8),
                ("PREMIERE", "AN_1RE_ECR_ESSAI",    "Expression écrite", "Rédiger un essai argumenté de trois cents mots", 9),
                ("PREMIERE", "AN_1RE_ECR_SYNTHESE", "Expression écrite", "Rédiger une synthèse de plusieurs documents", 10),
                ("PREMIERE", "AN_1RE_LEX_THEMATIQUE","Lexique", "Mobiliser le lexique des axes du programme", 11),
                ("PREMIERE", "AN_1RE_METH_ANALYSE", "Méthode", "Analyser un document en distinguant le fond et les procédés", 12),
                ("PREMIERE", "AN_1RE_METH_PLAN",    "Méthode", "Organiser un essai en introduction, développement, conclusion", 13),
                ("PREMIERE", "AN_1RE_CULT_PROBLEMATIQUE","Culture", "Problématiser une question culturelle du monde anglophone", 14),

                // --- Terminale : B2 attendu ---
                ("TERMINALE", "AN_TLE_ORAL_EPREUVE","Expression orale", "Tenir l'épreuve orale : présentation puis entretien", 1),
                ("TERMINALE", "AN_TLE_ORAL_SPONTANE","Expression orale", "S'exprimer spontanément et se reprendre sans perdre le fil", 2),
                ("TERMINALE", "AN_TLE_COMP_ORAL",   "Compréhension", "Comprendre un document authentique long et rapide", 3),
                ("TERMINALE", "AN_TLE_COMP_ECRIT",  "Compréhension", "Analyser un texte exigeant et en restituer les nuances", 4),
                ("TERMINALE", "AN_TLE_COMP_DOSSIER","Compréhension", "Traiter un dossier de documents et en dégager la tension", 5),
                ("TERMINALE", "AN_TLE_GRAM_MAITRISE","Grammaire", "Écrire sans erreur sur les points de grammaire du collège", 6),
                ("TERMINALE", "AN_TLE_GRAM_STYLE",  "Grammaire", "Varier les structures pour éviter la monotonie", 7),
                ("TERMINALE", "AN_TLE_ECR_ESSAI",   "Expression écrite", "Rédiger un essai argumenté et nuancé de cinq cents mots", 8),
                ("TERMINALE", "AN_TLE_ECR_TRADUIRE","Expression écrite", "Traduire un passage court sans calquer le français", 9),
                ("TERMINALE", "AN_TLE_LEX_REGISTRE","Lexique", "Adapter son registre de langue à la situation", 10),
                ("TERMINALE", "AN_TLE_METH_ARGUMENT","Méthode", "Soutenir une thèse en anticipant l'objection", 11),
                ("TERMINALE", "AN_TLE_METH_CITATION","Méthode", "S'appuyer sur les documents sans les recopier", 12),
                ("TERMINALE", "AN_TLE_CULT_AXES",   "Culture", "Traiter les axes du programme avec des exemples précis", 13),
            };

        /// <summary>
        /// Deux chaînes dominent le graphe de l'anglais, et ce sont les deux
        /// endroits où les élèves français décrochent :
        ///
        ///   — le SYSTÈME VERBAL. Présent simple contre présent en -ing, puis
        ///     prétérit contre present perfect. Chaque étage suppose le
        ///     précédent, et une confusion non traitée en sixième se retrouve
        ///     telle quelle en terminale.
        ///   — les AUXILIAIRES. Sans do, pas de question ni de négation ; sans
        ///     cette base, tout le reste s'écrit en calquant le français.
        /// </summary>
        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // Primaire : la base orale.
                ("AN_CE1_ORAL_PRESENTER",     "AN_CP_ORAL_PRENOM",         3),
                ("AN_CE1_GRAM_ETRE",          "AN_CP_ORAL_PRENOM",         2),
                ("AN_CE2_ORAL_QUESTIONS",     "AN_CE1_ORAL_PRESENTER",     2),
                ("AN_CE2_GRAM_AVOIR",         "AN_CE1_GRAM_ETRE",          3),
                ("AN_CE2_LEX_TEMPS",          "AN_CE1_LEX_NOMBRES",        2),
                ("AN_CM1_GRAM_PRESENT",       "AN_CE1_GRAM_ETRE",          3),
                ("AN_CM1_GRAM_QUESTIONS",     "AN_CM1_GRAM_PRESENT",       3),
                ("AN_CM1_ORAL_ROUTINE",       "AN_CM1_GRAM_PRESENT",       3),
                ("AN_CM1_ECR_PHRASES",        "AN_CE2_ECR_COPIE",          2),
                ("AN_CM2_GRAM_PRETERIT",      "AN_CM1_GRAM_PRESENT",       3),
                ("AN_CM2_GRAM_BE_ING",        "AN_CE1_GRAM_ETRE",          3),
                ("AN_CM2_GRAM_NEGATION",      "AN_CM1_GRAM_QUESTIONS",     3),
                ("AN_CM2_ORAL_RACONTER",      "AN_CM2_GRAM_PRETERIT",      3),
                ("AN_CM2_ECR_TEXTE",          "AN_CM1_ECR_PHRASES",        3),

                // 6e : le point de bascule du système verbal.
                ("AN_6E_GRAM_PRESENT",        "AN_CM1_GRAM_PRESENT",       3),
                ("AN_6E_GRAM_PRESENT",        "AN_CM2_GRAM_BE_ING",        3),
                ("AN_6E_GRAM_PRETERIT",       "AN_CM2_GRAM_PRETERIT",      3),
                ("AN_6E_GRAM_AUXILIAIRE",     "AN_CM2_GRAM_NEGATION",      3),
                ("AN_6E_GRAM_AUXILIAIRE",     "AN_CM1_GRAM_QUESTIONS",     3),
                ("AN_6E_GRAM_AGE",            "AN_CE1_GRAM_ETRE",          3),
                ("AN_6E_ORAL_CONTINU",        "AN_CM2_ORAL_DIALOGUE",      2),
                ("AN_6E_ECR_TEXTE",           "AN_CM2_ECR_TEXTE",          3),
                ("AN_6E_COMP_ORAL",           "AN_CM2_COMP_ORAL",          3),
                ("AN_6E_COMP_ECRIT",          "AN_CM2_COMP_ECRIT",         3),
                ("AN_6E_PHONO_TERMINAISON",   "AN_6E_GRAM_PRETERIT",       2),

                // 6e → 5e
                ("AN_5E_GRAM_FUTUR",          "AN_6E_GRAM_AUXILIAIRE",     3),
                ("AN_5E_GRAM_PRETERIT_ING",   "AN_6E_GRAM_PRETERIT",       3),
                ("AN_5E_GRAM_PRETERIT_ING",   "AN_6E_GRAM_PRESENT",        3),
                ("AN_5E_GRAM_QUANTITE",       "AN_CE2_GRAM_PLURIEL",       2),
                ("AN_5E_ECR_RECIT",           "AN_6E_GRAM_PRETERIT",       3),
                ("AN_5E_ECR_RECIT",           "AN_6E_ECR_TEXTE",           3),
                ("AN_5E_ORAL_CONTINU",        "AN_6E_ORAL_CONTINU",        3),
                ("AN_5E_COMP_ORAL",           "AN_6E_COMP_ORAL",           3),

                // 5e → 4e : le present perfect, l'autre grand décrochage.
                ("AN_4E_GRAM_PRESENT_PERFECT","AN_6E_GRAM_PRETERIT",       3),
                ("AN_4E_GRAM_PRESENT_PERFECT","AN_CE2_GRAM_AVOIR",         3),
                ("AN_4E_GRAM_SINCE_FOR",      "AN_4E_GRAM_PRESENT_PERFECT",3),
                ("AN_4E_GRAM_PASSIF",         "AN_6E_GRAM_PRETERIT",       3),
                ("AN_4E_GRAM_MODAUX",         "AN_6E_GRAM_MODAUX",         3),
                ("AN_4E_ECR_ARGUMENTER",      "AN_5E_ORAL_OPINION",        2),
                ("AN_4E_ECR_ARTICLE",         "AN_5E_ECR_RECIT",           3),
                ("AN_4E_COMP_ECRIT",          "AN_5E_COMP_ECRIT",          3),
                ("AN_4E_ORAL_CONTINU",        "AN_5E_ORAL_CONTINU",        3),

                // 4e → 3e
                ("AN_3E_GRAM_TEMPS",          "AN_4E_GRAM_PRESENT_PERFECT",3),
                ("AN_3E_GRAM_TEMPS",          "AN_5E_GRAM_PRETERIT_ING",   3),
                ("AN_3E_GRAM_CONDITIONNEL",   "AN_5E_GRAM_FUTUR",          3),
                ("AN_3E_GRAM_DISCOURS",       "AN_3E_GRAM_TEMPS",          3),
                ("AN_3E_GRAM_PASSIF",         "AN_4E_GRAM_PASSIF",         3),
                ("AN_3E_ECR_TEXTE",           "AN_4E_ECR_ARGUMENTER",      3),
                ("AN_3E_ORAL_EXPOSE",         "AN_4E_ORAL_CONTINU",        3),
                ("AN_3E_COMP_ORAL",           "AN_4E_COMP_ORAL",           3),
                ("AN_3E_ECR_RELIRE",          "AN_3E_GRAM_TEMPS",          2),

                // 3e → seconde
                ("AN_2DE_GRAM_TEMPS",         "AN_3E_GRAM_TEMPS",          3),
                ("AN_2DE_GRAM_COMPLEXE",      "AN_4E_GRAM_RELATIVES",      3),
                ("AN_2DE_GRAM_MODAUX",        "AN_4E_GRAM_MODAUX",         3),
                ("AN_2DE_ECR_ESSAI",          "AN_3E_ECR_TEXTE",           3),
                ("AN_2DE_ORAL_CONTINU",       "AN_3E_ORAL_EXPOSE",         3),
                ("AN_2DE_COMP_ECRIT",         "AN_3E_COMP_ECRIT",          3),
                ("AN_2DE_COMP_IMPLICITE",     "AN_2DE_COMP_ECRIT",         2),
                ("AN_2DE_METH_SYNTHESE",      "AN_2DE_COMP_ECRIT",         3),

                // Seconde → première
                ("AN_1RE_GRAM_HYPOTHESE",     "AN_3E_GRAM_CONDITIONNEL",   3),
                ("AN_1RE_GRAM_NUANCE",        "AN_2DE_GRAM_MODAUX",        3),
                ("AN_1RE_GRAM_ARTICULER",     "AN_4E_LEX_CONNECTEURS",     3),
                ("AN_1RE_ECR_ESSAI",          "AN_2DE_ECR_ESSAI",          3),
                ("AN_1RE_ECR_SYNTHESE",       "AN_2DE_METH_SYNTHESE",      3),
                ("AN_1RE_COMP_CROISER",       "AN_2DE_COMP_IMPLICITE",     3),
                ("AN_1RE_ORAL_PRESENTER",     "AN_2DE_ORAL_CONTINU",       3),
                ("AN_1RE_METH_PLAN",          "AN_1RE_ECR_ESSAI",          2),

                // Première → terminale
                ("AN_TLE_ECR_ESSAI",          "AN_1RE_ECR_ESSAI",          3),
                ("AN_TLE_ORAL_EPREUVE",       "AN_1RE_ORAL_PRESENTER",     3),
                ("AN_TLE_COMP_DOSSIER",       "AN_1RE_COMP_CROISER",       3),
                ("AN_TLE_GRAM_MAITRISE",      "AN_3E_GRAM_TEMPS",          3),
                ("AN_TLE_GRAM_MAITRISE",      "AN_6E_GRAM_AUXILIAIRE",     3),
                ("AN_TLE_METH_ARGUMENT",      "AN_1RE_ORAL_DEBAT",         2),
                ("AN_TLE_ECR_TRADUIRE",       "AN_TLE_GRAM_MAITRISE",      2),
                ("AN_TLE_COMP_ECRIT",         "AN_1RE_COMP_ECRIT",         3),
            };
    }
}
