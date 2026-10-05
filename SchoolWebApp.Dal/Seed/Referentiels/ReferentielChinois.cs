namespace SchoolWebApp.Dal.Seed.Referentiels
{
    public static class ReferentielChinois
    {
        /// <summary>
        /// Référentiel de CHINOIS (mandarin) enseigné en LANGUE VIVANTE B (LV2),
        /// de la cinquième à la terminale, voies générale et technologique.
        /// Construit sur le modèle exact de <see cref="ReferentielEspagnol"/>.
        ///
        /// ================== PROVENANCE (lue le 04/10/2026) ==================
        ///
        ///   NIVEAU   TEXTE APPLICABLE EN 2026-2027                       DEPUIS
        ///   --------------------------------------------------------------------
        ///   5e       Arrêté du 5-5-2025, MENE2504621A, BO n° 22 du       rentrée 2026
        ///            29-5-2025, ANNEXE 7 « Programme de chinois pour
        ///            les classes de collège »
        ///   4e       Programme du cycle 4, arrêté du 9-11-2015 modifié,  rentrée 2020
        ///            BO n° 31 du 30-7-2020 (volet LV commun à toutes     (annexe 7 :
        ///            les langues). L'annexe 7 ne s'y applique qu'en      rentrée 2027)
        ///            2027 (art. 4 de MENE2504621A).
        ///   3e       Même texte de 2020. Annexe 7 : rentrée 2028.        rentrée 2020
        ///   2de GT   MENE2504621A, ANNEXE 8 « Programme de chinois       rentrée 2025
        ///            pour les classes de lycée général et techno. »
        ///   1re GT   MENE2504621A, annexe 8                              rentrée 2026
        ///   Tle GT   MENE2504621A, annexe 8                              rentrée 2026
        ///
        ///   URL RÉELLEMENT LUES le 04/10/2026 (PDF téléchargés, texte extrait
        ///   et lu : 39 et 47 pages) :
        ///     https://www.education.gouv.fr/sites/default/files/ensel621_annexe7_ok.pdf
        ///       (identique octet pour octet à .../ensel621_annexe7.pdf)
        ///     https://www.education.gouv.fr/sites/default/files/ensel621_annexe8.pdf
        ///     https://eduscol.education.gouv.fr/sites/default/files/document/programme-d-enseignement-du-cycle-4-67722.pdf
        ///       (seule la phrase sur la LV2 y a été relue : « Pour la LV2, le
        ///       niveau A2 du CECRL dans au moins deux activités langagières »)
        ///
        /// ================== NIVEAUX CECRL VISÉS EN LVB ==================
        ///
        ///   Annexes 7 et 8, tableau du préambule commun ET tableau de chaque
        ///   classe du programme de chinois — ce sont les mêmes qu'en espagnol :
        ///     5e A1+   4e A1+   3e A2   2de A2+   1re B1   Tle B1
        ///   « + » : le niveau supérieur est atteint dans au moins une activité
        ///   langagière. Le texte NE DÉCOTE PAS l'écrit pour la LVB : la seule
        ///   réserve (« il est difficile d'atteindre le niveau B1 dans les
        ///   compétences d'écrit [...] le niveau attendu en LVC est A2+-B1 »)
        ///   vise la LVC, pas la LVB. En revanche le texte pose pour le chinois,
        ///   à chaque classe : « la progression de la communication orale
        ///   devance celle de la communication écrite », et une « disjonction
        ///   oral-écrit » (un mot su à l'oral n'a pas à être su à l'écrit).
        ///   4e et 3e : le programme de 2020 ne fixe que A2 dans au moins deux
        ///   activités en FIN DE CYCLE ; le A1+ de 4e est un repère du nouveau
        ///   texte, pas encore une obligation.
        ///
        /// ================== CARACTÈRES : CE QUE DIT LE TEXTE ==================
        ///
        ///   Minimum de NOUVEAUX caractères ACTIFS par an en LVB, « en référence
        ///   au seuil des 355 (BO n° 10 du 11/03/2021) » — annexes 7 et 8 :
        ///     5e 80   4e 80   3e 65   2de 50   1re 40   Tle 40   (total : 355)
        ///   (LVA : 90, 85, 70, 60, 60, 50 de la 5e à la terminale, seuil des 505 ;
        ///   non repris ici.) Les annexes 7 et 8 ne donnent AUCUN chiffre de
        ///   caractères PASSIFS : le référentiel n'en avance donc aucun.
        ///   « Lors des activités de production écrite, les élèves peuvent
        ///   recourir à la transcription pinyin pour les caractères hors seuil ou
        ///   pas encore appris » (annexe 7, 5e ; même phrase en 4e).
        ///   Le pinyin « ne peut se substituer à l'entrainement phonologique » ;
        ///   quatre tons, initiales et finales, lien son-sens-code graphique.
        ///   Compétence graphique : traits, composants graphiques, caractères.
        ///   Les caractères simplifiés sont ceux qu'emploie l'annexe elle-même.
        ///
        /// ================== CE QUI EST PRESCRIPTIF ==================
        ///
        ///   PRESCRIPTIF : les niveaux CECRL ; les axes ; l'axe 6 OBLIGATOIRE
        ///   (de la 5e à la terminale en voie générale : 5 axes sur 6 dont
        ///   l'axe 6). En voie TECHNOLOGIQUE : au moins 3 axes, l'axe 6
        ///   « vivement recommandé ». Les minima annuels de caractères actifs.
        ///   Axes 6 du chinois : 5e « Les quartiers chinois dans le monde »,
        ///   4e « Singapour », 3e « Taiwan », 2de « Le monde sinophone »,
        ///   1re « Identités taïwanaises », Tle « La Chine et le monde ».
        ///   INDICATIF : les objets d'étude (« proposés à titre indicatif ») et
        ///   les listes de lexique et de grammaire (« indicatives, génériques et
        ///   non exhaustives »), qui de plus « correspondent aux besoins en
        ///   expression orale » : les points de grammaire retenus ici ne sont
        ///   pas exigibles un par un, et encore moins à l'écrit.
        ///   Dans les listes de l'annexe, le niveau supérieur est marqué en
        ///   bleu. On n'a gardé que la colonne LVB : ce qui est « bleu » (A2 en
        ///   5e, A2+ en 4e, B1 en 3e, B1+ en 2de et 1re, B2 en terminale)
        ///   relève de la LVA et n'est pas demandé ici (phrase en 被, 刚,
        ///   redoublement de l'adjectif, 难道, etc.).
        ///
        ///   4e et 3e : comme en espagnol, les axes 6 du nouveau programme
        ///   (Singapour, Taiwan) et les minima de caractères (80, 65) figurent
        ///   déjà dans le référentiel ; ils ne deviennent obligatoires qu'en
        ///   2027 et en 2028.
        ///   Le domaine « Méthode » ne porte que la ligne CECRL, comme en
        ///   espagnol. Tons, pinyin, tracé et seuils de caractères sont rangés
        ///   dans Phonologie, Compréhension de l'écrit et Expression écrite.
        ///
        /// ================== NON VÉRIFIÉ (04/10/2026) ==================
        ///
        ///   1. La page du BO https://www.education.gouv.fr/bo/2025/Hebdo22/MENE2504621A
        ///      a répondu 403 : l'ARTICLE 4 (dates d'entrée en vigueur par
        ///      classe) et la numérotation « annexe 7 / annexe 8 » n'ont PAS été
        ///      relus ce jour. Ils sont repris de l'en-tête de
        ///      ReferentielEspagnol.cs (lu le 14/09/2026) et confirmés seulement
        ///      par le nom des fichiers (ensel621_annexe7, _annexe8) et par le
        ///      titre des PDF. Le tableau « TEXTE APPLICABLE » ci-dessus en dépend.
        ///   2. Le BO n° 10 du 11-3-2021 (MENE2104725N, seuils de caractères) a
        ///      répondu 403 : la LISTE des 355 caractères et toute distinction
        ///      actif / passif qu'il contiendrait n'ont pas été lues. Seuls les
        ///      minima annuels cités par les annexes 7 et 8 sont vérifiés.
        ///   3. Le programme de 2020 n'a été relu que pour la phrase sur le
        ///      niveau de LV2 ; ses thèmes culturels, dits ici « compatibles »
        ///      avec les axes de 4e et 3e, sont repris du modèle espagnol.
        ///   4. Les ressources Éduscol propres au chinois (livret
        ///      d'accompagnement collège 2026, objets d'étude) n'ont pas été lues.
        ///   5. Les pinyin entre parenthèses sont ajoutés par nous : l'annexe
        ///      cite les mots en caractères, presque jamais en pinyin.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- 5e : LVB débutant, A1+ visé (annexe 7, en vigueur) ---
                ("CINQUIEME", "ZH_5E_CO_CONSIGNES",     "Compréhension de l'oral", "Comprendre le chinois de classe et des énoncés très simples dits lentement, en repérant les mots clés", 1),
                ("CINQUIEME", "ZH_5E_CE_CARACTERES",    "Compréhension de l'écrit", "Reconnaître les caractères appris et relier à un titre ou à un thème des expressions isolées d'un texte court", 2),
                ("CINQUIEME", "ZH_5E_CE_MOT_NOUVEAU",   "Compréhension de l'écrit", "S'appuyer sur un caractère connu pour comprendre un mot nouveau : 家 (jiā) dans 家庭 (jiātíng)", 3),
                ("CINQUIEME", "ZH_5E_EO_PRESENTER",     "Expression orale", "Se présenter : nom, âge, famille, nationalité, d'où l'on vient, où l'on habite", 4),
                ("CINQUIEME", "ZH_5E_EO_DECRIRE_GOUTS", "Expression orale", "Décrire très simplement une personne, un objet ou un lieu et dire ses goûts avec 喜欢 (xǐhuan)", 5),
                ("CINQUIEME", "ZH_5E_INT_QUESTIONS",    "Interaction", "Poser des questions simples et y répondre dans des situations répétées", 6),
                ("CINQUIEME", "ZH_5E_INT_POLITESSE",    "Interaction", "Saluer, prendre congé, remercier, s'excuser et demander de répéter ou comment se dit un mot", 7),
                ("CINQUIEME", "ZH_5E_EE_TRACE",         "Expression écrite", "Tracer et recopier les caractères en respectant leurs traits et leurs composants graphiques", 8),
                ("CINQUIEME", "ZH_5E_EE_SEUIL_80",      "Expression écrite", "Savoir écrire de mémoire au moins 80 caractères actifs dans l'année, sur le seuil LVB des 355", 9),
                ("CINQUIEME", "ZH_5E_EE_PHRASES",       "Expression écrite", "Écrire des phrases courtes d'après un modèle, en pinyin pour les caractères pas encore appris", 10),
                ("CINQUIEME", "ZH_5E_MED_INFOS",        "Médiation", "Transmettre les informations factuelles d'une invitation ou d'un prospectus : lieu, horaire, prix", 11),
                ("CINQUIEME", "ZH_5E_PHONO_TONS",       "Phonologie", "Percevoir et produire les quatre tons, sur des mots isolés puis dans des phrases simples", 12),
                ("CINQUIEME", "ZH_5E_PHONO_PINYIN",     "Phonologie", "Lire et écrire le pinyin, initiales, finales et ton, et épeler le pinyin d'un caractère", 13),
                ("CINQUIEME", "ZH_5E_GRAM_PHRASE",      "Grammaire", "Construire la phrase simple en respectant l'ordre fixe des mots, sans conjuguer le verbe", 14),
                ("CINQUIEME", "ZH_5E_GRAM_SHI_HEN",     "Grammaire", "Employer 是 (shì) pour l'identité seulement et qualifier avec 很 (hěn) sans 是 : 他很高", 15),
                ("CINQUIEME", "ZH_5E_GRAM_NEGATION",    "Grammaire", "Nier avec 不 (bù) et 没 (méi) et employer les adverbes 也 (yě) et 都 (dōu)", 16),
                ("CINQUIEME", "ZH_5E_GRAM_CLASSIFICATEURS", "Grammaire", "Compter avec un classificateur (个, 口, 只, 条, 张, 本) et distinguer 两 (liǎng) et 二 (èr)", 17),
                ("CINQUIEME", "ZH_5E_GRAM_QUESTIONS",   "Grammaire", "Interroger avec 吗 (ma), la forme affirmative-négative ou un mot interrogatif (谁, 什么, 哪儿, 几)", 18),
                ("CINQUIEME", "ZH_5E_LEX_QUOTIDIEN",    "Lexique", "Employer le lexique de la famille, du portrait, de l'école, des loisirs, des nombres jusqu'à 100 et de l'heure", 19),
                ("CINQUIEME", "ZH_5E_CULT_QUARTIERS",   "Culture", "Découvrir les quartiers chinois dans le monde, 唐人街 (Tángrénjiē), et les grandes diasporas", 20),
                ("CINQUIEME", "ZH_5E_CULT_FETES_LEGENDES", "Culture", "Connaître fêtes, saisons et légendes du monde chinois, dont Sun Wukong, le Roi Singe", 21),
                ("CINQUIEME", "ZH_5E_CECRL",            "Méthode", "Atteindre le niveau A1+ visé en fin de cinquième pour une LVB débutée cette année", 22),

                // --- 4e : A1+ consolidé (programme 2020 en vigueur, annexe 7 en 2027) ---
                ("QUATRIEME", "ZH_4E_CO_MESSAGE",       "Compréhension de l'oral", "Reconnaître plusieurs informations simples d'un message oral clair et les relier entre elles", 1),
                ("QUATRIEME", "ZH_4E_CE_PHRASE",        "Compréhension de l'écrit", "Lire un texte court en caractères connus en découpant la phrase en mots", 2),
                ("QUATRIEME", "ZH_4E_CE_STRUCTURE_MOT", "Compréhension de l'écrit", "Déduire le sens d'un mot de sa structure : 书包 (shūbāo), 画家 (huàjiā), 天天 (tiāntiān)", 3),
                ("QUATRIEME", "ZH_4E_EO_DECRIRE",       "Expression orale", "Décrire en coordonnant plusieurs éléments simples : personne, lieu, activité", 4),
                ("QUATRIEME", "ZH_4E_EO_RACONTER",      "Expression orale", "Raconter brièvement un événement en le situant dans le temps et dans l'espace", 5),
                ("QUATRIEME", "ZH_4E_INT_ECHANGE",      "Interaction", "Engager et clore une conversation simple, demander et donner des nouvelles", 6),
                ("QUATRIEME", "ZH_4E_INT_ACCORD",       "Interaction", "Exprimer accord, désaccord, autorisation et interdiction : 可以吗 (kěyǐ ma), 不行 (bù xíng)", 7),
                ("QUATRIEME", "ZH_4E_EE_SEUIL_80",      "Expression écrite", "Savoir écrire de mémoire au moins 80 nouveaux caractères actifs dans l'année, sur le seuil LVB des 355", 8),
                ("QUATRIEME", "ZH_4E_EE_MESSAGE",       "Expression écrite", "Rédiger un court message d'après un modèle, le pinyin restant permis pour un caractère non appris", 9),
                ("QUATRIEME", "ZH_4E_MED_REFORMULER",   "Médiation", "Reformuler pour un camarade l'essentiel d'un document simple et signaler une difficulté d'ordre culturel", 10),
                ("QUATRIEME", "ZH_4E_PHONO_PROSODIE",   "Phonologie", "Tenir les tons dans la phrase entière et soigner la prosodie, à partir de l'écoute", 11),
                ("QUATRIEME", "ZH_4E_GRAM_DE",          "Grammaire", "Employer 的 (de) pour la possession, la détermination et la relative : 我喜欢看的书", 12),
                ("QUATRIEME", "ZH_4E_GRAM_ASPECTS",     "Grammaire", "Marquer l'action réalisée avec 了 (le) et l'expérience vécue avec 过 (guo), sans conjugaison", 13),
                ("QUATRIEME", "ZH_4E_GRAM_RESULTATIFS", "Grammaire", "Employer des composés verbaux de résultat et d'appréciation : 看懂, 听不懂, 说得好", 14),
                ("QUATRIEME", "ZH_4E_GRAM_PREPOSITIONS","Grammaire", "Placer avant le verbe le groupe introduit par 在 (zài), 跟 (gēn), 用 (yòng) ou 坐 (zuò)", 15),
                ("QUATRIEME", "ZH_4E_GRAM_EGALITE",     "Grammaire", "Comparer à égalité avec 和…一样 et mettre en relief avec 是…的", 16),
                ("QUATRIEME", "ZH_4E_GRAM_CONNECTEURS", "Grammaire", "Exprimer la cause avec 因为…所以… et l'opposition avec 可是 (kěshì)", 17),
                ("QUATRIEME", "ZH_4E_LEX_VILLE_VOYAGE", "Lexique", "Employer le lexique du sport, du voyage, des déplacements, de la ville et des inventions", 18),
                ("QUATRIEME", "ZH_4E_CULT_SINGAPOUR",   "Culture", "Connaître Singapour : histoire, langues, société multiculturelle", 19),
                ("QUATRIEME", "ZH_4E_CULT_VOYAGES",     "Culture", "Découvrir voyages et explorations : routes de la soie, Xuanzang, Zheng He, Marco Polo", 20),
                ("QUATRIEME", "ZH_4E_CULT_VILLES",      "Culture", "Découvrir villes, villages et quartiers traditionnels de Chine, entre rénovation et patrimoine", 21),
                ("QUATRIEME", "ZH_4E_CECRL",            "Méthode", "Consolider le niveau A1+ en fin de quatrième en LVB", 22),

                // --- 3e : A2 (programme 2020 : A2 dans au moins deux activités) ---
                ("TROISIEME", "ZH_3E_CO_INFOS",         "Compréhension de l'oral", "Repérer des informations simples dans une conversation ou un récit dits clairement et lentement", 1),
                ("TROISIEME", "ZH_3E_CE_TEXTE",         "Compréhension de l'écrit", "Comprendre un texte simple en caractères connus : récit, message, article court", 2),
                ("TROISIEME", "ZH_3E_EO_QUOTIDIEN",     "Expression orale", "Décrire activités quotidiennes, personnes et lieux, et parler de ses projets proches avec 想 et 要", 3),
                ("TROISIEME", "ZH_3E_EO_OPINION",       "Expression orale", "Dire ce qu'on aime ou non et donner son avis avec 我觉得 (wǒ juéde) sur un sujet personnel", 4),
                ("TROISIEME", "ZH_3E_EO_RECIT_PASSE",   "Expression orale", "Relater brièvement un événement ou une expérience passée en la situant dans le temps", 5),
                ("TROISIEME", "ZH_3E_INT_TACHE",        "Interaction", "Échanger des informations simples et directes pour mener une tâche courante", 6),
                ("TROISIEME", "ZH_3E_INT_PRECISIONS",   "Interaction", "Demander de l'aide ou une précision : …是什么意思, 请你说得慢一点儿", 7),
                ("TROISIEME", "ZH_3E_EE_SEUIL_65",      "Expression écrite", "Savoir écrire de mémoire au moins 65 nouveaux caractères actifs dans l'année, sur le seuil LVB des 355", 8),
                ("TROISIEME", "ZH_3E_EE_RECIT",         "Expression écrite", "Écrire le début ou la suite d'une histoire en s'aidant d'un dictionnaire", 9),
                ("TROISIEME", "ZH_3E_MED_POINTS",       "Médiation", "Transmettre les points principaux d'un texte ou d'une conversation sur un sujet familier", 10),
                ("TROISIEME", "ZH_3E_PHONO_CLARTE",     "Phonologie", "S'exprimer de façon intelligible en tenant tons et prosodie dans une prise de parole suivie", 11),
                ("TROISIEME", "ZH_3E_GRAM_BI",          "Grammaire", "Comparer avec 比 (bǐ) pour la supériorité et 没有 (méiyǒu) pour l'infériorité", 12),
                ("TROISIEME", "ZH_3E_GRAM_SHI_DE",      "Grammaire", "Insister sur une circonstance passée avec 是…的 : 我是去年认识他的", 13),
                ("TROISIEME", "ZH_3E_GRAM_PREPOSITIONS","Grammaire", "Employer les prépositions 给 (gěi), 对 (duì), 从 (cóng), 到 (dào) et 离 (lí)", 14),
                ("TROISIEME", "ZH_3E_GRAM_HYPOTHESE",   "Grammaire", "Exprimer l'hypothèse avec 要是…就… ou 如果…就… et la simultanéité avec 一边…一边…", 15),
                ("TROISIEME", "ZH_3E_GRAM_QUESTIONS",   "Grammaire", "Poser une question alternative avec 还是 (háishi) et nuancer avec les particules 呢 et 吧", 16),
                ("TROISIEME", "ZH_3E_GRAM_LE",          "Grammaire", "Distinguer 了 suffixe du verbe (action réalisée) et 了 en fin de phrase (situation nouvelle)", 17),
                ("TROISIEME", "ZH_3E_LEX_TRAVAIL",      "Lexique", "Employer le lexique du travail, des voyages, des migrations et des médias", 18),
                ("TROISIEME", "ZH_3E_CULT_TAIWAN",      "Culture", "Connaître Taiwan : de Formose à aujourd'hui, environnement, diversité ethnique et linguistique", 19),
                ("TROISIEME", "ZH_3E_CULT_MEDIAS",      "Culture", "Comprendre la place de l'écriture dans les médias chinois, du 大字报 (dàzìbào) aux réseaux sociaux", 20),
                ("TROISIEME", "ZH_3E_CULT_TRAVAIL",     "Culture", "Découvrir le travail, les migrations intérieures et les formes d'engagement dans le monde chinois", 21),
                ("TROISIEME", "ZH_3E_CECRL",            "Méthode", "Atteindre le niveau A2 dans au moins deux activités langagières en fin de troisième", 22),

                // --- Seconde : A2+ visé (annexe 8, en vigueur depuis 2025) ---
                ("SECONDE", "ZH_2DE_CO_INFOS",          "Compréhension de l'oral", "Comprendre les informations principales d'une conversation ou d'une émission claire et lente", 1),
                ("SECONDE", "ZH_2DE_CO_HYPOTHESES",     "Compréhension de l'oral", "Formuler des hypothèses à partir du contexte et préparer l'écoute d'un passage difficile", 2),
                ("SECONDE", "ZH_2DE_CE_TEXTES",         "Compréhension de l'écrit", "Comprendre les informations principales de textes variés au vocabulaire fréquent sur des sujets familiers", 3),
                ("SECONDE", "ZH_2DE_EO_PRESENTER",      "Expression orale", "Présenter son quotidien, des projets, des activités passées et des expériences personnelles", 4),
                ("SECONDE", "ZH_2DE_EO_JUSTIFIER",      "Expression orale", "Expliquer pourquoi une chose plaît ou déplaît et comparer de façon simple", 5),
                ("SECONDE", "ZH_2DE_INT_ECHANGER",      "Interaction", "Échanger idées et renseignements avec une aisance raisonnable en situation prévisible", 6),
                ("SECONDE", "ZH_2DE_EE_SEUIL_50",       "Expression écrite", "Savoir écrire au moins 50 nouveaux caractères actifs dans l'année et réactiver ceux du collège", 7),
                ("SECONDE", "ZH_2DE_EE_RENDRE_COMPTE",  "Expression écrite", "Rendre compte par écrit d'un document étudié ou le transformer : article en interview, récit en saynète", 8),
                ("SECONDE", "ZH_2DE_MED_INFOS",         "Médiation", "Transmettre les informations pertinentes d'un texte informatif court et bien structuré", 9),
                ("SECONDE", "ZH_2DE_PHONO_PROSODIE",    "Phonologie", "S'exprimer davantage en continu en maintenant des tons et une prosodie intelligibles", 10),
                ("SECONDE", "ZH_2DE_GRAM_DIRECTIONNELS","Grammaire", "Employer les composés verbaux directionnels : 进来, 回去, 拿出来", 11),
                ("SECONDE", "ZH_2DE_GRAM_REDOUBLEMENT", "Grammaire", "Redoubler le verbe pour atténuer ou essayer : 吃吃看, 休息休息", 12),
                ("SECONDE", "ZH_2DE_GRAM_DUREE",        "Grammaire", "Exprimer l'état qui dure avec 着 (zhe) et l'action en cours avec 正在 (zhèngzài)", 13),
                ("SECONDE", "ZH_2DE_GRAM_INDEFINIS",    "Grammaire", "Employer un interrogatif comme indéfini avec 都 ou 也 : 我哪儿也不想去", 14),
                ("SECONDE", "ZH_2DE_GRAM_BURU",         "Grammaire", "Comparer à l'infériorité avec 不如 (bùrú) : 他不如我认真", 15),
                ("SECONDE", "ZH_2DE_GRAM_CONCESSION",   "Grammaire", "Exprimer la concession avec 虽然…但是…, l'hypothèse avec 要是…的话 et l'addition avec 又…又…", 16),
                ("SECONDE", "ZH_2DE_GRAM_BA",           "Grammaire", "Employer la phrase en 把 (bǎ) dans les situations de classe : 请你把窗打开", 17),
                ("SECONDE", "ZH_2DE_GRAM_COMPLEMENTS",  "Grammaire", "Placer correctement les compléments de temps, de lieu et d'objet dans la phrase", 18),
                ("SECONDE", "ZH_2DE_LEX_AXES",          "Lexique", "Mobiliser le lexique de la famille et des générations, du patrimoine, de l'environnement et des technologies", 19),
                ("SECONDE", "ZH_2DE_CULT_SINOPHONE",    "Culture", "Connaître le monde sinophone hors de Chine continentale : Taïwan, Singapour, Hong Kong, quartiers chinois", 20),
                ("SECONDE", "ZH_2DE_CULT_PASSE",        "Culture", "Analyser le passé dans le présent : fêtes traditionnelles, écriture chinoise, héritages impérial et maoïste", 21),
                ("SECONDE", "ZH_2DE_CECRL",             "Méthode", "Atteindre le niveau A2+ visé en fin de seconde en LVB", 22),

                // --- Première : B1 visé (annexe 8, en vigueur en 2026) ---
                ("PREMIERE", "ZH_1RE_CO_POINTS",        "Compréhension de l'oral", "Comprendre les points principaux d'un bulletin d'information ou d'un film en langue claire", 1),
                ("PREMIERE", "ZH_1RE_CE_RECIT",         "Compréhension de l'écrit", "Suivre l'intrigue d'un récit simple ou d'une bande dessinée en s'aidant d'un dictionnaire", 2),
                ("PREMIERE", "ZH_1RE_EO_EXPOSE",        "Expression orale", "Faire un exposé préparé, suivi sans difficulté, sur un sujet familier", 3),
                ("PREMIERE", "ZH_1RE_EO_RACONTER",      "Expression orale", "Raconter une histoire ou un événement et expliquer ce que l'on a ressenti", 4),
                ("PREMIERE", "ZH_1RE_INT_CONVERSATION", "Interaction", "Prendre part à une conversation prolongée sur un sujet familier en prenant quelques initiatives", 5),
                ("PREMIERE", "ZH_1RE_INT_CLARIFIER",    "Interaction", "Faire répéter, demander confirmation et poser des questions pour clarifier un raisonnement", 6),
                ("PREMIERE", "ZH_1RE_EE_SEUIL_40",      "Expression écrite", "Savoir écrire au moins 40 nouveaux caractères actifs dans l'année et réactiver les caractères déjà appris", 7),
                ("PREMIERE", "ZH_1RE_EE_CRITIQUE",      "Expression écrite", "Rédiger une critique simple d'un film ou d'un livre en donnant des raisons simples", 8),
                ("PREMIERE", "ZH_1RE_MED_RESUMER",      "Médiation", "Résumer en français l'information et les arguments d'un dossier en chinois sur un sujet familier", 9),
                ("PREMIERE", "ZH_1RE_MED_SOURCES",      "Médiation", "Rassembler des informations de sources diverses et les résumer pour quelqu'un d'autre", 10),
                ("PREMIERE", "ZH_1RE_PHONO_PROSODIE",   "Phonologie", "Garder tons et prosodie justes dans un exposé ou un récit de plusieurs phrases", 11),
                ("PREMIERE", "ZH_1RE_GRAM_RANG",        "Grammaire", "Faire faire ou laisser faire avec le verbe factitif 让 (ràng)", 12),
                ("PREMIERE", "ZH_1RE_GRAM_CAI_JIU",     "Grammaire", "Opposer les adverbes 才 (cái) et 就 (jiù) : plus tard ou plus tôt que prévu", 13),
                ("PREMIERE", "ZH_1RE_GRAM_ASPECTS",     "Grammaire", "Choisir entre les suffixes d'aspect 过, 了 et 着 selon ce que l'on veut dire", 14),
                ("PREMIERE", "ZH_1RE_GRAM_COMPARER",    "Grammaire", "Comparer avec une large gamme de tournures : 比, 没有, 不如, 和…一样", 15),
                ("PREMIERE", "ZH_1RE_GRAM_BA",          "Grammaire", "Construire soi-même une phrase en 把 (bǎ) avec un verbe suivi de son résultat", 16),
                ("PREMIERE", "ZH_1RE_GRAM_COMPLEXE",    "Grammaire", "Enrichir l'expression de la cause, de la concession, du but, de la condition et de l'hypothèse", 17),
                ("PREMIERE", "ZH_1RE_LEX_AXES",         "Lexique", "Mobiliser le lexique des échanges, de la diversité, de l'art, de l'innovation et de la nature", 18),
                ("PREMIERE", "ZH_1RE_CULT_TAIWAN",      "Culture", "Approfondir les identités taïwanaises : histoire, langues et écriture, identités aborigènes, modes de vie", 19),
                ("PREMIERE", "ZH_1RE_CULT_ART_POUVOIR", "Culture", "Analyser les liens entre art et pouvoir : engagement, scènes indépendantes, censure et propagande", 20),
                ("PREMIERE", "ZH_1RE_CULT_NATURE",      "Culture", "Étudier le rapport entre l'être humain et la nature, de la pensée chinoise au développement durable", 21),
                ("PREMIERE", "ZH_1RE_CECRL",            "Méthode", "Atteindre le niveau B1 visé en fin de première en LVB", 22),

                // --- Terminale : B1 visé (annexe 8) ---
                ("TERMINALE", "ZH_TLE_CO_DOCUMENT",     "Compréhension de l'oral", "Comprendre les points principaux d'un reportage ou d'un film en distinguant information et prise de position", 1),
                ("TERMINALE", "ZH_TLE_CE_RENDRE_COMPTE","Compréhension de l'écrit", "Rendre compte du contenu explicite et implicite d'un ou de plusieurs documents", 2),
                ("TERMINALE", "ZH_TLE_CE_PROJET",       "Compréhension de l'écrit", "Mener un projet de lecture à partir d'une problématique en s'appuyant sur indices lexicaux et culturels", 3),
                ("TERMINALE", "ZH_TLE_EO_EXPOSE",       "Expression orale", "Faire un exposé préparé sur un axe étudié, clair et suivi sans difficulté", 4),
                ("TERMINALE", "ZH_TLE_EO_JUSTIFIER",    "Expression orale", "Donner des raisons simples pour justifier un point de vue et expliquer ce que l'on ressent", 5),
                ("TERMINALE", "ZH_TLE_INT_CONVERSATION","Interaction", "Soutenir une conversation prolongée en langue standard sur un sujet familier, en faisant répéter au besoin", 6),
                ("TERMINALE", "ZH_TLE_EE_SEUIL_355",    "Expression écrite", "Savoir écrire au moins 40 nouveaux caractères actifs dans l'année, pour atteindre le seuil LVB des 355", 7),
                ("TERMINALE", "ZH_TLE_EE_ARGUMENTER",   "Expression écrite", "Rédiger une critique simple, une lettre ouverte ou une réponse à une problématique à partir de documents", 8),
                ("TERMINALE", "ZH_TLE_MED_RESUMER",     "Médiation", "Résumer en français l'information et les arguments de textes en chinois sur des sujets familiers", 9),
                ("TERMINALE", "ZH_TLE_MED_SYNTHESE",    "Médiation", "Rassembler des informations de sources diverses et les résumer pour quelqu'un d'autre", 10),
                ("TERMINALE", "ZH_TLE_PHONO_PROSODIE",  "Phonologie", "Soigner tons, prosodie et débit dans une prise de parole en continu", 11),
                ("TERMINALE", "ZH_TLE_GRAM_BA",         "Grammaire", "Employer la phrase en 把 (bǎ) à bon escient dans un récit ou une consigne", 12),
                ("TERMINALE", "ZH_TLE_GRAM_COMPOSES",   "Grammaire", "Maîtriser les composés verbaux appris : résultat, possibilité, direction", 13),
                ("TERMINALE", "ZH_TLE_GRAM_LE_SHI_DE",  "Grammaire", "Maîtriser la particule finale 了 (le) et la construction 是…的", 14),
                ("TERMINALE", "ZH_TLE_GRAM_INDEFINIS",  "Grammaire", "Maîtriser les formes interrogatives et l'emploi des interrogatifs comme indéfinis", 15),
                ("TERMINALE", "ZH_TLE_GRAM_COMPLEXE",   "Grammaire", "Articuler cause, concession, but, condition et hypothèse dans une phrase complexe", 16),
                ("TERMINALE", "ZH_TLE_GRAM_MAITRISE",   "Grammaire", "Maîtriser ordre des mots, classificateurs, 的 et expression du passé et du futur, travaillés depuis le collège", 17),
                ("TERMINALE", "ZH_TLE_LEX_AXES",        "Lexique", "Mobiliser le lexique de la mémoire, de la communication, du numérique et des relations internationales", 18),
                ("TERMINALE", "ZH_TLE_CULT_CHINE_MONDE","Culture", "Analyser les relations de la Chine avec le monde : Europe, Afrique, États-Unis, Sud global", 19),
                ("TERMINALE", "ZH_TLE_CULT_MEMOIRE",    "Culture", "Étudier territoire et mémoire : patrimoine, Révolution culturelle, lieux de mémoire et leurs enjeux", 20),
                ("TERMINALE", "ZH_TLE_CULT_ESPACES",    "Culture", "Interroger espace privé et espace public : habitat, place des femmes, famille, internet chinois", 21),
                ("TERMINALE", "ZH_TLE_CECRL",           "Méthode", "Atteindre le niveau B1 visé en fin de terminale en LVB", 22),
            };
    }
}
