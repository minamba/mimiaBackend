namespace SchoolWebApp.Dal.Seed.Referentiels
{
    public static class ReferentielEspagnol
    {
        /// <summary>
        /// Référentiel d'ESPAGNOL enseigné en LANGUE VIVANTE B (LV2), de la
        /// cinquième à la terminale, voies générale et technologique.
        ///
        /// ================== PROVENANCE (lue le 14/09/2026) ==================
        ///
        ///   NIVEAU   TEXTE APPLICABLE EN 2026-2027                       DEPUIS
        ///   --------------------------------------------------------------------
        ///   5e       Arrêté du 5-5-2025, MENE2504621A, BO n° 22 du       rentrée 2026
        ///            29-5-2025, ANNEXE 9 « Programme d'espagnol pour
        ///            les classes de collège »
        ///   4e       Programme du cycle 4, arrêté du 9-11-2015 modifié,  rentrée 2020
        ///            BO n° 31 du 30-7-2020 (volet LV commun à toutes     (annexe 9 :
        ///            les langues). L'annexe 9 ne s'y applique qu'en      rentrée 2027)
        ///            2027 (art. 4 de MENE2504621A).
        ///   3e       Même texte de 2020. Annexe 9 : rentrée 2028.        rentrée 2020
        ///   2de GT   MENE2504621A, ANNEXE 10 « Programme d'espagnol      rentrée 2025
        ///            pour les classes de lycée général et techno. »
        ///   1re GT   MENE2504621A, annexe 10                             rentrée 2026
        ///   Tle GT   MENE2504621A, annexe 10                             rentrée 2026
        ///
        ///   Article 4 de MENE2504621A, texte exact : application « à la rentrée
        ///   2025-2026 [...] à la classe de sixième et à la classe de seconde
        ///   générale et technologique, à la rentrée 2026-2027 [...] à la classe
        ///   de cinquième et aux classes de premières et terminales [...], à la
        ///   rentrée 2027-2028 [...] à la classe de quatrième et à la rentrée
        ///   2028-2029 [...] à la classe de troisième ». NB : la seconde a donc
        ///   basculé dès 2025, pas en 2026.
        ///   L'arrêté compte 25 annexes : 1-2 allemand, 3-4 anglais, 5-6 arabe,
        ///   7-8 chinois, 9-10 ESPAGNOL, 11-12 hébreu, 13-14 italien, 15-16
        ///   japonais, 17-18 néerlandais, 19 polonais (lycée), 20-21 portugais,
        ///   22-23 russe, 24-25 cadre commun des langues à faible diffusion.
        ///
        ///   URL (lues le 14/09/2026) :
        ///     https://www.education.gouv.fr/bo/2025/Hebdo22/MENE2504621A
        ///     https://www.education.gouv.fr/sites/default/files/annexe-9-programme-d-espagnol-pour-les-classes-de-coll-ge-440373.pdf
        ///     https://www.education.gouv.fr/sites/default/files/annexe-10-programme-d-espagnol-pour-les-classes-de-lyc-e-g-n-ral-et-technologique-440376.pdf
        ///     https://eduscol.education.gouv.fr/sites/default/files/document/programme-d-enseignement-du-cycle-4-67722.pdf
        ///
        ///   Horaires qui fondent l'existence de la LV2 (Légifrance, versions
        ///   en vigueur au 14/09/2026) :
        ///     collège    arrêté du 19-5-2015, annexe 2 remplacée par l'arrêté
        ///                du 10-3-2026 (MENE2601452A, en vigueur le 5-7-2026) :
        ///                LV2 2,5 h en 5e, 4e et 3e ; en 6e, LV2 FACULTATIVE
        ///                (art. 7 b, dans la limite de 6 h pour les deux LV)
        ///     2de GT     arrêté du 16-7-2018 : « LVA et LVB (enveloppe
        ///                globalisée) » ; 5 h 30 selon Éduscol (page 5664)
        ///     1re/Tle G  arrêté du 16-7-2018 modifié le 19-6-2026 : 4 h 30 / 4 h
        ///     1re/Tle T  4 h pour LVA + LVB, dont 1 h d'ETLV (Éduscol 5664)
        ///
        /// ================== NIVEAUX CECRL VISÉS EN LVB ==================
        ///
        ///   Annexes 9 et 10, tableau du préambule commun :
        ///     5e A1+   4e A1+   3e A2   2de A2+   1re B1   Tle B1
        ///   « + » : le niveau supérieur est atteint dans au moins une activité
        ///   langagière. L'élève de LVB DÉBUTE en 5e ; en 6e, la LVB n'existe
        ///   qu'en bilangue.
        ///   Programme de 2020, encore applicable en 4e et 3e : « Pour la LV2,
        ///   le niveau A2 du CECRL dans au moins deux activités langagières »,
        ///   en FIN DE CYCLE seulement, sans palier annuel. Le A1+ de 4e vient
        ///   donc du nouveau texte : c'est un repère, pas encore une obligation.
        ///   Fin de terminale : B1 en LVB (article D. 312-16 du code de
        ///   l'éducation, cité par la note de service MENE2618195N).
        ///
        /// ================== CE QUI EST PRESCRIPTIF ==================
        ///
        ///   PRESCRIPTIF : les niveaux CECRL ; les axes ; l'axe 6 OBLIGATOIRE
        ///   (de la 5e à la terminale en voie générale : 5 axes sur 6 dont
        ///   l'axe 6). En voie TECHNOLOGIQUE : au moins 3 axes, l'axe 6
        ///   « vivement recommandé ».
        ///   INDICATIF : les objets d'étude (« proposés à titre indicatif ») et
        ///   les listes de lexique et de grammaire (« indicatives et non
        ///   exhaustives »), ainsi que toutes les ressources Éduscol : attendus
        ///   de fin d'année 4e et 3e, repères de progressivité linguistique et
        ///   déclinaisons culturelles pour l'espagnol (2016), exemples pour le
        ///   lycée (2025). Les points de grammaire retenus ici suivent donc des
        ///   listes indicatives : ils ne sont pas exigibles un par un.
        ///   Dans les listes de l'annexe, le niveau supérieur est marqué en
        ///   bleu. On n'a gardé que la colonne LVB : ce qui est « bleu » (A2 en
        ///   5e, A2+ en 4e, B1 en 3e, B1+ en 2de et 1re, B2 en terminale)
        ///   relève de la LVA et n'est pas demandé ici.
        ///
        ///   4e et 3e : comme pour l'anglais, les axes 6 du nouveau programme
        ///   (Andalousie, 1492) figurent déjà dans le référentiel. Ils
        ///   s'inscrivent dans les thèmes de 2020 (« rencontres avec d'autres
        ///   cultures », « voyages et migrations ») mais ne deviennent
        ///   obligatoires qu'en 2027 et en 2028.
        ///   Le domaine « Méthode » ne porte que la ligne CECRL, comme en anglais.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- 5e : LVB débutant, A1+ visé (annexe 9, en vigueur) ---
                ("CINQUIEME", "ES_5E_CO_CONSIGNES",     "Compréhension de l'oral", "Comprendre des consignes de classe et des énoncés très simples prononcés lentement", 1),
                ("CINQUIEME", "ES_5E_CO_MOTS_CLES",     "Compréhension de l'oral", "Repérer mots transparents, voix et indices sonores pour identifier le thème d'un document court", 2),
                ("CINQUIEME", "ES_5E_CE_TEXTE_COURT",   "Compréhension de l'écrit", "Relier à un titre ou à un thème des expressions isolées d'un texte court et concret", 3),
                ("CINQUIEME", "ES_5E_EO_PRESENTER",     "Expression orale", "Se présenter : nom, âge, famille, nationalité, d'où l'on vient, où l'on habite", 4),
                ("CINQUIEME", "ES_5E_EO_DECRIRE",       "Expression orale", "Décrire très simplement une personne, un objet ou un lieu à partir d'un modèle", 5),
                ("CINQUIEME", "ES_5E_EO_GOUTS",         "Expression orale", "Exprimer ses goûts et préférences avec me gusta, me encanta, prefiero", 6),
                ("CINQUIEME", "ES_5E_INT_QUESTIONS",    "Interaction", "Poser des questions simples et y répondre dans des situations répétées", 7),
                ("CINQUIEME", "ES_5E_INT_POLITESSE",    "Interaction", "Saluer, prendre congé, remercier, s'excuser et demander de répéter", 8),
                ("CINQUIEME", "ES_5E_EE_COPIE_DICTEE",  "Expression écrite", "Copier, écrire sous la dictée et compléter une fiche ou un court message", 9),
                ("CINQUIEME", "ES_5E_MED_INFOS",        "Médiation", "Transmettre les informations factuelles d'une invitation ou d'un prospectus : lieu, horaire, prix", 10),
                ("CINQUIEME", "ES_5E_PHONO_SONS",       "Phonologie", "Prononcer à l'espagnole e, u, j, ñ, r, s, v et les diphtongues, sans nasaliser les voyelles", 11),
                ("CINQUIEME", "ES_5E_PHONO_INTONATION", "Phonologie", "Respecter l'accent tonique, l'intonation des questions et exclamations, et écrire ¿ et ¡", 12),
                ("CINQUIEME", "ES_5E_GRAM_PRESENT",     "Grammaire", "Conjuguer au présent les verbes réguliers en -ar, -er, -ir et ser, estar, ir, tener", 13),
                ("CINQUIEME", "ES_5E_GRAM_DIPHTONGUE",  "Grammaire", "Conjuguer au présent les verbes à diphtongue (poder, volver) et à alternance (decir, repetir)", 14),
                ("CINQUIEME", "ES_5E_GRAM_GN",          "Grammaire", "Accorder en genre et en nombre, employer articles, possessifs et poco, mucho", 15),
                ("CINQUIEME", "ES_5E_GRAM_PHRASE",      "Grammaire", "Construire la négation et des questions avec quién, dónde, cuántos, qué, sans partitif", 16),
                ("CINQUIEME", "ES_5E_GRAM_FUTUR_PROCHE","Grammaire", "Exprimer le futur proche avec ir a et l'action en cours avec estar + gérondif", 17),
                ("CINQUIEME", "ES_5E_GRAM_PREP_A",      "Grammaire", "Employer a devant un complément d'objet direct de personne et l'obligation hay que", 18),
                ("CINQUIEME", "ES_5E_LEX_QUOTIDIEN",    "Lexique", "Employer le lexique de la famille, du portrait, de l'école, des loisirs et de l'heure", 19),
                ("CINQUIEME", "ES_5E_CULT_MEXIQUE",     "Culture", "Découvrir le Mexique : nature et patrimoine, saveurs, fête des Morts", 20),
                ("CINQUIEME", "ES_5E_CULT_MONDE_HISP",  "Culture", "Situer l'espagnol dans le monde et comparer les rythmes de vie en Espagne et en Amérique latine", 21),
                ("CINQUIEME", "ES_5E_CECRL",            "Méthode", "Atteindre le niveau A1+ visé en fin de cinquième pour une LVB débutée cette année", 22),

                // --- 4e : A1+ consolidé (programme 2020 en vigueur, annexe 9 en 2027) ---
                ("QUATRIEME", "ES_4E_CO_MESSAGE",       "Compréhension de l'oral", "Isoler des informations simples d'un message oral clair et les relier à un thème", 1),
                ("QUATRIEME", "ES_4E_CE_DOCUMENT",      "Compréhension de l'écrit", "Comprendre un document écrit simple : affiche, lettre, récit court, page web", 2),
                ("QUATRIEME", "ES_4E_EO_DECRIRE",       "Expression orale", "Décrire en coordonnant plusieurs éléments simples : personne, lieu, activité", 3),
                ("QUATRIEME", "ES_4E_EO_RACONTER",      "Expression orale", "Raconter brièvement un événement passé avec quelques verbes courants", 4),
                ("QUATRIEME", "ES_4E_EO_LIRE_VOIX",     "Expression orale", "Lire à voix haute ou dire un texte bref préparé, un poème par exemple, avec l'intonation juste", 5),
                ("QUATRIEME", "ES_4E_INT_ECHANGE",      "Interaction", "Engager et clore une conversation simple, demander et donner des nouvelles", 6),
                ("QUATRIEME", "ES_4E_INT_ACCORD",       "Interaction", "Exprimer accord, désaccord, autorisation et interdiction en contexte connu", 7),
                ("QUATRIEME", "ES_4E_EE_MESSAGE",       "Expression écrite", "Rédiger un court message, une carte ou un récit bref à partir d'un modèle", 8),
                ("QUATRIEME", "ES_4E_MED_REFORMULER",   "Médiation", "Reformuler pour un camarade l'essentiel d'un document simple", 9),
                ("QUATRIEME", "ES_4E_PHONO_ACCENT",     "Phonologie", "Placer l'accent tonique, y compris sur l'antépénultième, et ne pas nasaliser devant m et n", 10),
                ("QUATRIEME", "ES_4E_GRAM_SER_ESTAR",   "Grammaire", "Distinguer ser et estar dans leurs emplois courants : identité, origine, lieu, état", 11),
                ("QUATRIEME", "ES_4E_GRAM_IMPARFAIT",   "Grammaire", "Former l'imparfait en -aba et -ía et employer era, iba, había", 12),
                ("QUATRIEME", "ES_4E_GRAM_PASSE_SIMPLE","Grammaire", "Employer le passé simple des verbes réguliers et de ser, ir, tener, estar, hacer", 13),
                ("QUATRIEME", "ES_4E_GRAM_FUTUR",       "Grammaire", "Exprimer le futur avec ir a et le futur simple des verbes réguliers", 14),
                ("QUATRIEME", "ES_4E_GRAM_GUSTAR",      "Grammaire", "Construire gustar, encantar et apetecer avec le pronom complément (me, te, le)", 15),
                ("QUATRIEME", "ES_4E_GRAM_ENCLISE",     "Grammaire", "Placer le pronom après l'impératif, l'infinitif ou le gérondif (dime, dámelo)", 16),
                ("QUATRIEME", "ES_4E_GRAM_USTED",       "Grammaire", "Vouvoyer avec usted et ustedes en conjuguant le verbe à la troisième personne", 17),
                ("QUATRIEME", "ES_4E_LEX_VILLE_VOYAGE", "Lexique", "Employer le lexique du sport, du voyage, de la ville et de l'itinéraire", 18),
                ("QUATRIEME", "ES_4E_CULT_VOYAGES",     "Culture", "Découvrir voyages et explorations dans le monde hispanique, dont le Machu Picchu", 19),
                ("QUATRIEME", "ES_4E_CULT_ARTS",        "Culture", "Découvrir des langages artistiques hispaniques : Dalí, Kahlo, Gaudí, art urbain", 20),
                ("QUATRIEME", "ES_4E_CULT_ANDALOUSIE",  "Culture", "Connaître l'Andalousie : musiques, héritage d'al-Ándalus, tourisme", 21),
                ("QUATRIEME", "ES_4E_CECRL",            "Méthode", "Consolider le niveau A1+ en fin de quatrième en LVB", 22),

                // --- 3e : A2 (programme 2020 : A2 dans au moins deux activités) ---
                ("TROISIEME", "ES_3E_CO_IDEE_GENERALE", "Compréhension de l'oral", "Suivre l'idée générale d'un document oral clair sur un sujet familier", 1),
                ("TROISIEME", "ES_3E_CE_RECIT",         "Compréhension de l'écrit", "Comprendre les informations simples d'un récit, d'un article ou d'un échange écrit", 2),
                ("TROISIEME", "ES_3E_EO_QUOTIDIEN",     "Expression orale", "Décrire activités quotidiennes, personnes et lieux, et parler de ses projets proches", 3),
                ("TROISIEME", "ES_3E_EO_OPINION",       "Expression orale", "Dire ce qu'on aime ou non et donner son avis sur un sujet d'intérêt personnel", 4),
                ("TROISIEME", "ES_3E_EO_RECIT_PASSE",   "Expression orale", "Relater brièvement un événement ou une expérience passée en la situant dans le temps", 5),
                ("TROISIEME", "ES_3E_INT_TACHE",        "Interaction", "Échanger des informations simples et directes pour mener une tâche courante", 6),
                ("TROISIEME", "ES_3E_INT_PRECISIONS",   "Interaction", "Demander et donner des précisions, relancer et reformuler pour se faire comprendre", 7),
                ("TROISIEME", "ES_3E_EE_RECIT",         "Expression écrite", "Écrire le début ou la suite d'une histoire en s'aidant d'un dictionnaire", 8),
                ("TROISIEME", "ES_3E_EE_COURRIER",      "Expression écrite", "Rédiger un courriel ou une lettre simple avec les formules d'adresse et de congé", 9),
                ("TROISIEME", "ES_3E_MED_POINTS",       "Médiation", "Transmettre les points principaux d'un texte ou d'une conversation sur un sujet familier", 10),
                ("TROISIEME", "ES_3E_PHONO_CLARTE",     "Phonologie", "S'exprimer clairement malgré l'accent, en respectant accent tonique et intonation", 11),
                ("TROISIEME", "ES_3E_GRAM_PASSES",      "Grammaire", "Choisir entre passé simple, passé composé et imparfait dans un récit simple", 12),
                ("TROISIEME", "ES_3E_GRAM_FUTUR",       "Grammaire", "Conjuguer le futur simple, y compris tener, poner et hacer", 13),
                ("TROISIEME", "ES_3E_GRAM_SUBJONCTIF",  "Grammaire", "Employer le subjonctif présent après para que et après cuando à valeur de futur", 14),
                ("TROISIEME", "ES_3E_GRAM_APOCOPE",     "Grammaire", "Appliquer l'apocope des adjectifs et employer les diminutifs -ito, -illo", 15),
                ("TROISIEME", "ES_3E_GRAM_OBLIGATION",  "Grammaire", "Exprimer l'obligation personnelle avec tener que et impersonnelle avec hay que", 16),
                ("TROISIEME", "ES_3E_GRAM_GERONDIF",    "Grammaire", "Employer seguir et ir + gérondif, et volver a + infinitif pour la répétition", 17),
                ("TROISIEME", "ES_3E_LEX_FAUX_AMIS",    "Lexique", "Éviter faux amis et paronymes : expresar/exprimir, crear/creer, sentarse/sentirse", 18),
                ("TROISIEME", "ES_3E_LEX_TRAVAIL",      "Lexique", "Employer le lexique du travail, des voyages, des migrations et des médias", 19),
                ("TROISIEME", "ES_3E_CULT_1492",        "Culture", "Comprendre 1492 : fin de la Reconquista, arrivée de Colomb, découverte ou dépossession", 20),
                ("TROISIEME", "ES_3E_CULT_ENGAGEMENT",  "Culture", "Découvrir des formes d'engagement dans le monde hispanique, dont la guerre civile espagnole", 21),
                ("TROISIEME", "ES_3E_CECRL",            "Méthode", "Atteindre le niveau A2 dans au moins deux activités langagières en fin de troisième", 22),

                // --- Seconde : A2+ visé (annexe 10, en vigueur depuis 2025) ---
                ("SECONDE", "ES_2DE_CO_INFOS",          "Compréhension de l'oral", "Comprendre les informations principales d'une conversation ou d'une émission claire et lente", 1),
                ("SECONDE", "ES_2DE_CO_HYPOTHESES",     "Compréhension de l'oral", "Formuler des hypothèses à partir des indices et vérifier sa compréhension par étapes", 2),
                ("SECONDE", "ES_2DE_CE_TEXTES",         "Compréhension de l'écrit", "Comprendre les informations principales de textes variés sur des sujets familiers", 3),
                ("SECONDE", "ES_2DE_EO_PRESENTER",      "Expression orale", "Présenter son quotidien, des projets, des activités passées et des expériences personnelles", 4),
                ("SECONDE", "ES_2DE_EO_JUSTIFIER",      "Expression orale", "Expliquer pourquoi une chose plaît ou déplaît et comparer de façon simple", 5),
                ("SECONDE", "ES_2DE_INT_ECHANGER",      "Interaction", "Échanger idées et renseignements avec une aisance raisonnable en situation prévisible", 6),
                ("SECONDE", "ES_2DE_EE_RENDRE_COMPTE",  "Expression écrite", "Rendre compte par écrit d'un document étudié et en rédiger un bref résumé", 7),
                ("SECONDE", "ES_2DE_EE_TRANSFORMER",    "Expression écrite", "Transformer un document : article en interview, récit en saynète", 8),
                ("SECONDE", "ES_2DE_MED_INFOS",         "Médiation", "Transmettre les informations pertinentes d'un texte informatif court et bien structuré", 9),
                ("SECONDE", "ES_2DE_PHONO_ACCENTS",     "Phonologie", "Distinguer mots oxytons, paroxytons et proparoxytons et placer l'accent écrit", 10),
                ("SECONDE", "ES_2DE_PHONO_SYNALEPHE",   "Phonologie", "Respecter la synalèphe, le h muet, l'accent des formes enclitiques et l'intonation", 11),
                ("SECONDE", "ES_2DE_GRAM_PRESENT",      "Grammaire", "Conjuguer au présent conocer, conducir, pedir, coger et les irréguliers dar, traer, saber", 12),
                ("SECONDE", "ES_2DE_GRAM_SUBJONCTIF",   "Grammaire", "Conjuguer le subjonctif présent et exprimer la volonté avec querer que", 13),
                ("SECONDE", "ES_2DE_GRAM_DEFENSE",      "Grammaire", "Donner un ordre à toutes les personnes et exprimer la défense avec no + subjonctif", 14),
                ("SECONDE", "ES_2DE_GRAM_PASSES",       "Grammaire", "Employer passé simple, imparfait et passé composé, participes irréguliers compris", 15),
                ("SECONDE", "ES_2DE_GRAM_CONDITION",    "Grammaire", "Former le conditionnel des verbes réguliers et l'imparfait du subjonctif de ser et ir", 16),
                ("SECONDE", "ES_2DE_GRAM_PRONOMS",      "Grammaire", "Employer usted, la double enclise, les possessifs et les démonstratifs à toutes les personnes", 17),
                ("SECONDE", "ES_2DE_GRAM_COMPARER",     "Grammaire", "Comparer avec más/menos que et tan/tanto como, et employer le superlatif absolu", 18),
                ("SECONDE", "ES_2DE_LEX_AXES",          "Lexique", "Mobiliser le lexique de l'identité, des générations, du patrimoine et des transitions", 19),
                ("SECONDE", "ES_2DE_CULT_ESPAGNE",      "Culture", "Connaître l'Espagne au-delà des clichés : paysages, Madrid, la mer", 20),
                ("SECONDE", "ES_2DE_CULT_PASSE",        "Culture", "Analyser comment les sociétés hispaniques célèbrent ou questionnent leur passé", 21),
                ("SECONDE", "ES_2DE_CECRL",             "Méthode", "Atteindre le niveau A2+ visé en fin de seconde en LVB", 22),

                // --- Première : B1 visé (annexe 10, en vigueur en 2026) ---
                ("PREMIERE", "ES_1RE_CO_POINTS",        "Compréhension de l'oral", "Comprendre les points principaux d'un bulletin d'information ou d'un film en langue claire", 1),
                ("PREMIERE", "ES_1RE_CE_RECIT",         "Compréhension de l'écrit", "Suivre l'intrigue d'un récit simple et déduire le sens des mots composés ou dérivés", 2),
                ("PREMIERE", "ES_1RE_EO_EXPOSE",        "Expression orale", "Faire un exposé préparé, suivi sans difficulté, sur un sujet familier", 3),
                ("PREMIERE", "ES_1RE_EO_OPINION",       "Expression orale", "Exprimer et justifier une opinion en comparant et en pesant le pour et le contre", 4),
                ("PREMIERE", "ES_1RE_INT_CONVERSATION", "Interaction", "Prendre part à une conversation prolongée sur un sujet familier en prenant des initiatives", 5),
                ("PREMIERE", "ES_1RE_INT_NUANCE",       "Interaction", "Exprimer accord ou désaccord avec nuance et reformuler pour vérifier la compréhension", 6),
                ("PREMIERE", "ES_1RE_EE_CRITIQUE",      "Expression écrite", "Rédiger une critique simple d'un film ou d'un livre en justifiant son point de vue", 7),
                ("PREMIERE", "ES_1RE_EE_CONTROLER",     "Expression écrite", "Contrôler sa production après coup et la structurer avec des connecteurs variés", 8),
                ("PREMIERE", "ES_1RE_MED_RESUMER",      "Médiation", "Résumer en français l'information et les arguments d'un dossier en espagnol", 9),
                ("PREMIERE", "ES_1RE_MED_IMPLICITE",    "Médiation", "Expliciter pour autrui une référence culturelle implicite présente dans un message", 10),
                ("PREMIERE", "ES_1RE_PHONO_PASSE_SIMPLE","Phonologie", "Accentuer le passé simple, formes irrégulières comprises, et distinguer synalèphe et diérèse", 11),
                ("PREMIERE", "ES_1RE_GRAM_PLUSQUEPARFAIT","Grammaire", "Former le plus-que-parfait et situer des actions passées les unes par rapport aux autres", 12),
                ("PREMIERE", "ES_1RE_GRAM_HYPOTHESE",   "Grammaire", "Formuler des hypothèses avec quizás, puede que et no creo que + subjonctif", 13),
                ("PREMIERE", "ES_1RE_GRAM_CONDITIONNEL","Grammaire", "Conjuguer le conditionnel des verbes irréguliers (tendría, haría, podría)", 14),
                ("PREMIERE", "ES_1RE_GRAM_EMPHASE",     "Grammaire", "Mettre en relief avec la tournure emphatique (fue… cuando) et le sujet postposé", 15),
                ("PREMIERE", "ES_1RE_GRAM_INDIRECTES",  "Grammaire", "Poser des questions indirectes (me pregunto si) et exprimer la simultanéité avec al + infinitif", 16),
                ("PREMIERE", "ES_1RE_GRAM_GERONDIF",    "Grammaire", "Exprimer la progression avec ir ou venir + gérondif et la continuité avec seguir", 17),
                ("PREMIERE", "ES_1RE_LEX_DERIVATION",   "Lexique", "Enrichir son lexique par dérivation et mobiliser celui des migrations, de l'art et de la nature", 18),
                ("PREMIERE", "ES_1RE_CULT_ANDES",       "Culture", "Connaître l'espace andin : sept pays, héritage inca, défis socio-économiques et environnementaux", 19),
                ("PREMIERE", "ES_1RE_CULT_ART_POUVOIR", "Culture", "Analyser les liens entre art et pouvoir : Diego Rivera, Víctor Jara, censure, artiste engagé", 20),
                ("PREMIERE", "ES_1RE_CULT_IDENTITES",   "Culture", "Étudier identités, frontières, migrations et pluralité linguistique du monde hispanique", 21),
                ("PREMIERE", "ES_1RE_CECRL",            "Méthode", "Atteindre le niveau B1 visé en fin de première en LVB", 22),

                // --- Terminale : B1 attendu (annexe 10 ; D. 312-16) ---
                ("TERMINALE", "ES_TLE_CO_DOCUMENT",     "Compréhension de l'oral", "Rendre compte en français d'un document audio ou vidéo d'une minute trente écouté trois fois", 1),
                ("TERMINALE", "ES_TLE_CE_DOSSIER",      "Compréhension de l'écrit", "Rendre compte d'un ou deux textes, de façon libre ou guidée, en français ou en espagnol", 2),
                ("TERMINALE", "ES_TLE_CE_LIENS",        "Compréhension de l'écrit", "Sélectionner et hiérarchiser les informations et relier plusieurs documents entre eux", 3),
                ("TERMINALE", "ES_TLE_EO_AXE",          "Expression orale", "Expliquer en cinq minutes quel document ou citation illustre le mieux un axe étudié", 4),
                ("TERMINALE", "ES_TLE_EO_ARGUMENTER",   "Expression orale", "Argumenter pour expliciter son raisonnement, notamment en vue du grand oral", 5),
                ("TERMINALE", "ES_TLE_INT_ENTRETIEN",   "Interaction", "Soutenir un entretien élargi sur le travail mené autour d'un axe du programme", 6),
                ("TERMINALE", "ES_TLE_EE_ARGUMENTATIF", "Expression écrite", "Répondre en espagnol à une ou deux questions liées au dossier par un texte argumenté", 7),
                ("TERMINALE", "ES_TLE_EE_CREATIF",      "Expression écrite", "Produire un écrit créatif : suite de texte, lettre, dialogue, critique de film", 8),
                ("TERMINALE", "ES_TLE_MED_TRADUIRE",    "Médiation", "S'initier à la traduction en comparant des textes traduits à leur original", 9),
                ("TERMINALE", "ES_TLE_MED_SYNTHESE",    "Médiation", "Rassembler des informations de sources diverses et les résumer pour quelqu'un d'autre", 10),
                ("TERMINALE", "ES_TLE_PHONO_PROSODIE",  "Phonologie", "Soigner prosodie, segmentation et débit, avec correction systématique de la prononciation", 11),
                ("TERMINALE", "ES_TLE_GRAM_SUBJ_IMPARFAIT","Grammaire", "Conjuguer le subjonctif imparfait des irréguliers courants : tuviera, dijera, hiciera", 12),
                ("TERMINALE", "ES_TLE_GRAM_CONCESSION", "Grammaire", "Exprimer la concession avec aunque, si bien et a pesar de que", 13),
                ("TERMINALE", "ES_TLE_GRAM_PASSIF",     "Grammaire", "Construire la voix passive avec ser + participe passé", 14),
                ("TERMINALE", "ES_TLE_GRAM_TEMPS",      "Grammaire", "Situer dans le temps avec hace, desde hace, dentro de et des locutions temporelles", 15),
                ("TERMINALE", "ES_TLE_GRAM_MAITRISE",   "Grammaire", "Maîtriser apocope, enclise, usted et accords, points travaillés depuis le collège", 16),
                ("TERMINALE", "ES_TLE_LEX_REGISTRE",    "Lexique", "Moduler le registre de langue et le degré de politesse selon l'interlocuteur", 17),
                ("TERMINALE", "ES_TLE_LEX_AXES",        "Lexique", "Mobiliser le lexique de la mémoire, de la communication et des mondes virtuels", 18),
                ("TERMINALE", "ES_TLE_CULT_METISSAGES", "Culture", "Analyser les métissages : al-Ándalus, conquête, commerce triangulaire, syncrétisme", 19),
                ("TERMINALE", "ES_TLE_CULT_MEMOIRE",    "Culture", "Étudier la mémoire historique et ses traces dans les territoires hispaniques", 20),
                ("TERMINALE", "ES_TLE_CULT_ESPACES",    "Culture", "Interroger espace privé et espace public : place des femmes, villes, réseaux sociaux", 21),
                ("TERMINALE", "ES_TLE_CECRL",           "Méthode", "Atteindre le niveau B1 visé en fin de terminale en LVB", 22),
            };
    }
}
