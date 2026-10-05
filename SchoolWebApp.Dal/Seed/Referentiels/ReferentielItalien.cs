namespace SchoolWebApp.Dal.Seed.Referentiels
{
    public static class ReferentielItalien
    {
        /// <summary>
        /// Référentiel d'ITALIEN enseigné en LANGUE VIVANTE B (LV2), de la
        /// cinquième à la terminale, voies générale et technologique.
        /// Construit sur le modèle exact de `ReferentielEspagnol`.
        ///
        /// ================== PROVENANCE (lue le 04/10/2026) ==================
        ///
        ///   NIVEAU   TEXTE APPLICABLE EN 2026-2027                       DEPUIS
        ///   --------------------------------------------------------------------
        ///   5e       Arrêté du 5-5-2025, MENE2504621A, BO n° 22 du       rentrée 2026
        ///            29-5-2025, ANNEXE 13 « Programme d'italien pour
        ///            les classes de collège »
        ///   4e       Programme du cycle 4, arrêté du 9-11-2015 modifié,  rentrée 2020
        ///            BO n° 31 du 30-7-2020 (volet LV commun à toutes     (annexe 13 :
        ///            les langues). L'annexe 13 ne s'y applique qu'en     rentrée 2027)
        ///            2027 (art. 4 de MENE2504621A).
        ///   3e       Même texte de 2020. Annexe 13 : rentrée 2028.       rentrée 2020
        ///   2de GT   MENE2504621A, ANNEXE 14 « Programme d'italien       rentrée 2025
        ///            pour les classes de lycée général et techno. »
        ///   1re GT   MENE2504621A, annexe 14                             rentrée 2026
        ///   Tle GT   MENE2504621A, annexe 14                             rentrée 2026
        ///
        ///   URL RÉELLEMENT LUES le 04/10/2026 (PDF téléchargés, texte extrait
        ///   et lu : 38 et 39 pages) :
        ///     https://www.education.gouv.fr/sites/default/files/ensel621_annexe13.pdf
        ///     https://www.education.gouv.fr/sites/default/files/ensel621_annexe14.pdf
        ///
        ///   Ce qui y a été lu : le préambule commun (tableaux des niveaux,
        ///   règle des axes), puis, pour CHAQUE classe de la 5e à la terminale,
        ///   les six axes avec leurs objets d'étude, la phonologie, le lexique
        ///   et la grammaire.
        ///
        /// ================== NON VÉRIFIÉ (à relire avant de s'y fier) ==================
        ///
        ///   1. La page du BO elle-même
        ///      (https://www.education.gouv.fr/bo/2025/Hebdo22/MENE2504621A)
        ///      a répondu « 403 Forbidden » le 04/10/2026. L'ARTICLE 4 (calendrier
        ///      d'entrée en vigueur : 2de en 2025, 5e/1re/Tle en 2026, 4e en 2027,
        ///      3e en 2028) n'a donc PAS été relu pour l'italien : il est repris
        ///      de la lecture du 14/09/2026 consignée dans `ReferentielEspagnol`.
        ///      L'arrêté est unique pour toutes les langues, mais ce n'est pas
        ///      une relecture.
        ///   2. Le programme du cycle 4 de 2020 (BO n° 31 du 30-7-2020), qui
        ///      s'applique encore en 4e et en 3e, n'a pas été relu ce jour :
        ///      même source que ci-dessus (lecture du 14/09/2026, espagnol).
        ///   3. Les tableaux « Ce que sait faire l'élève » (descripteurs par
        ///      activité langagière) des annexes 13 et 14 n'ont PAS été lus
        ///      ligne à ligne ; seuls les paragraphes d'introduction de la
        ///      compréhension en 5e, 4e et 3e l'ont été. Les libellés des
        ///      domaines Compréhension, Expression, Interaction et Médiation
        ///      reprennent donc les descripteurs communs à toutes les langues,
        ///      tels que transcrits dans `ReferentielEspagnol`. Ils ne sont pas
        ///      une citation de l'annexe d'italien.
        ///   4. Les formats d'évaluation de terminale (document d'une minute
        ///      trente écouté trois fois, oral de dix minutes sur un axe)
        ///      viennent de la note de service MENE2618195N, NON relue ce
        ///      jour : repris du référentiel d'espagnol.
        ///   5. Les horaires de LVB (arrêtés du 19-5-2015 et du 16-7-2018) ne
        ///      sont pas relus : voir l'en-tête de `ReferentielEspagnol`.
        ///   6. Aucune ressource Éduscol propre à l'italien n'a été lue (le
        ///      livret d'accompagnement 2026 pour le collège existe, non lu).
        ///   7. Dans le PDF, le niveau supérieur est marqué EN BLEU. Le texte
        ///      extrait ne garde pas la couleur : on s'est fié aux mentions
        ///      « (A2) », « (A2+) », « (B1) », « (B1+) », « (B2) » qui précèdent
        ///      les éléments bleus. Un élément bleu non étiqueté aurait pu
        ///      passer pour de la LVB.
        ///   8. Trois formes citées dans les libellés ne figurent PAS dans les
        ///      listes de l'annexe et viennent de la grammaire usuelle :
        ///      « non + infinitif » pour l'impératif négatif à tu, les exemples
        ///      de pronoms (lo, la, li, le, gli) et les exemples d'enclise
        ///      (dimmi, vederlo).
        ///
        /// ================== NIVEAUX CECRL VISÉS EN LVB ==================
        ///
        ///   Annexes 13 et 14, tableau du préambule commun (lu, identique à
        ///   celui de l'espagnol) :
        ///     5e A1+   4e A1+   3e A2   2de A2+   1re B1   Tle B1
        ///   « lorsque le niveau visé est accompagné du signe "+", cela signifie
        ///   que le niveau supérieur est atteint dans au moins l'une des
        ///   activités langagières ». L'élève de LVB DÉBUTE en 5e (en 6e, A1 :
        ///   bilangue seulement).
        ///   Programme de 2020, encore applicable en 4e et 3e : A2 dans au moins
        ///   deux activités langagières en FIN DE CYCLE, sans palier annuel. Le
        ///   A1+ de 4e vient du nouveau texte : un repère, pas encore une
        ///   obligation.
        ///
        /// ================== CE QUI EST PRESCRIPTIF ==================
        ///
        ///   PRESCRIPTIF : les niveaux CECRL ; les axes ; l'axe 6 OBLIGATOIRE
        ///   (« en 5e, 4e et 3e, cinq axes parmi les six proposés doivent être
        ///   traités pendant l'année, dont obligatoirement l'axe 6 » ; même
        ///   règle au lycée en voie générale). En voie TECHNOLOGIQUE : « au
        ///   moins trois axes », l'axe 6 « vivement recommandé ».
        ///   Axes 6 de l'italien : 5e ROME, 4e FLORENCE, 3e MILAN, 2de LA
        ///   RENAISSANCE, 1re CONSTRUCTION DE L'ITALIE ET AVÈNEMENT DES
        ///   ITALIENS, Tle DE LA RÉPUBLIQUE À L'EUROPE.
        ///   INDICATIF : les objets d'étude (« proposés à titre indicatif ») et
        ///   les listes de phonologie, de lexique et de grammaire (« indicatives
        ///   et non exhaustives, laissées à l'appréciation des professeurs »).
        ///   Les points de grammaire retenus ici suivent ces listes : ils ne
        ///   sont pas exigibles un par un.
        ///   On n'a gardé que la colonne LVB : ce qui est marqué au niveau
        ///   supérieur (A2 en 5e, A2+ en 4e, B1 en 3e, B1+ en 2de et 1re, B2 en
        ///   terminale) relève de la LVA et n'est pas demandé ici. Exemples
        ///   écartés : venire, bere, uscire, dire en 5e ; le conditionnel
        ///   présent en 3e, 2de et 1re ; le subjonctif imparfait, l'hypothèse
        ///   avec se et les pronoms groupés en terminale. Le passé simple n'est
        ///   qu'en REPÉRAGE (compréhension), en 1re comme en terminale.
        ///
        ///   4e et 3e : comme pour l'espagnol, les axes 6 du nouveau programme
        ///   (Florence, Milan) et ses listes de langue figurent déjà dans le
        ///   référentiel. Ils ne deviennent obligatoires qu'en 2027 et en 2028.
        ///   Le domaine « Méthode » ne porte que la ligne CECRL, comme en espagnol.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- 5e : LVB débutant, A1+ visé (annexe 13, en vigueur) ---
                ("CINQUIEME", "IT_5E_CO_CONSIGNES",     "Compréhension de l'oral", "Comprendre des consignes de classe et des énoncés très simples et courts", 1),
                ("CINQUIEME", "IT_5E_CO_MOTS_CLES",     "Compréhension de l'oral", "Repérer des mots familiers et des informations concrètes dans un document court, puis les relier", 2),
                ("CINQUIEME", "IT_5E_CE_TEXTE_COURT",   "Compréhension de l'écrit", "Associer les informations d'un texte court et simple à une image ou à un titre", 3),
                ("CINQUIEME", "IT_5E_EO_PRESENTER",     "Expression orale", "Se présenter : nom, âge, famille, nationalité, d'où l'on vient, où l'on habite", 4),
                ("CINQUIEME", "IT_5E_EO_DECRIRE",       "Expression orale", "Faire très simplement le portrait physique et moral d'une personne à partir d'un modèle", 5),
                ("CINQUIEME", "IT_5E_EO_GOUTS",         "Expression orale", "Exprimer ses goûts avec mi piace et mi piacciono", 6),
                ("CINQUIEME", "IT_5E_INT_QUESTIONS",    "Interaction", "Poser des questions simples et y répondre dans des situations répétées", 7),
                ("CINQUIEME", "IT_5E_INT_POLITESSE",    "Interaction", "Saluer, prendre congé, remercier, s'excuser et demander de répéter", 8),
                ("CINQUIEME", "IT_5E_EE_COPIE_DICTEE",  "Expression écrite", "Copier, écrire sous la dictée et compléter une fiche ou un court message", 9),
                ("CINQUIEME", "IT_5E_MED_INFOS",        "Médiation", "Transmettre les informations factuelles d'une invitation ou d'un prospectus : lieu, horaire, prix", 10),
                ("CINQUIEME", "IT_5E_PHONO_SONS",       "Phonologie", "Prononcer à l'italienne c, g, sc, z, toutes les voyelles et les doubles consonnes, sans nasaliser", 11),
                ("CINQUIEME", "IT_5E_PHONO_ACCENT",     "Phonologie", "Reconnaître la place de l'accent tonique et prononcer correctement les mots mémorisés", 12),
                ("CINQUIEME", "IT_5E_GRAM_PRESENT",     "Grammaire", "Conjuguer au présent essere, avere et les verbes réguliers des trois groupes (-are, -ere, -ire)", 13),
                ("CINQUIEME", "IT_5E_GRAM_IRREGULIERS", "Grammaire", "Conjuguer au présent andare, stare, fare, potere, volere, dovere et les verbes pronominaux", 14),
                ("CINQUIEME", "IT_5E_GRAM_GN",          "Grammaire", "Accorder nom et adjectif en genre et en nombre, employer les articles définis et indéfinis et quelques possessifs", 15),
                ("CINQUIEME", "IT_5E_GRAM_PHRASE",      "Grammaire", "Construire la négation et des questions avec dove, che cosa, come, quando, quanto, quale, perché", 16),
                ("CINQUIEME", "IT_5E_GRAM_CE_SUPERLATIF","Grammaire", "Dire « il y a » avec c'è et ci sono, et employer le superlatif relatif et absolu", 17),
                ("CINQUIEME", "IT_5E_GRAM_PREPOSITIONS","Grammaire", "Employer a, di, in, su et leur contraction avec l'article", 18),
                ("CINQUIEME", "IT_5E_LEX_QUOTIDIEN",    "Lexique", "Employer le lexique du portrait, de la famille, de la maison, de l'école, des loisirs et des repas", 19),
                ("CINQUIEME", "IT_5E_CULT_ROME",        "Culture", "Découvrir Rome : la capitale italienne, son patrimoine et son imaginaire", 20),
                ("CINQUIEME", "IT_5E_CULT_LIEUX_LANGUE","Culture", "Découvrir le quotidien en Italie, ses régions et ses saisons, et la naissance de la langue italienne", 21),
                ("CINQUIEME", "IT_5E_CECRL",            "Méthode", "Atteindre le niveau A1+ visé en fin de cinquième pour une LVB débutée cette année", 22),

                // --- 4e : A1+ consolidé (programme 2020 en vigueur, annexe 13 en 2027) ---
                ("QUATRIEME", "IT_4E_CO_MESSAGE",       "Compréhension de l'oral", "Reconnaître plusieurs informations simples d'un message oral court et les mettre en lien", 1),
                ("QUATRIEME", "IT_4E_CE_DOCUMENT",      "Compréhension de l'écrit", "Comprendre un document écrit simple et court : consigne, description, récit, dialogue", 2),
                ("QUATRIEME", "IT_4E_EO_DECRIRE",       "Expression orale", "Décrire en coordonnant plusieurs éléments simples : personne, lieu, activité", 3),
                ("QUATRIEME", "IT_4E_EO_RACONTER",      "Expression orale", "Raconter brièvement un événement passé avec quelques verbes courants", 4),
                ("QUATRIEME", "IT_4E_EO_LIRE_VOIX",     "Expression orale", "Lire ou dire un texte court, un poème ou un dialogue en plaçant bien l'accent", 5),
                ("QUATRIEME", "IT_4E_INT_ECHANGE",      "Interaction", "Engager et clore une conversation simple, demander et donner des nouvelles", 6),
                ("QUATRIEME", "IT_4E_INT_ACCORD",       "Interaction", "Exprimer accord, désaccord, autorisation et interdiction en contexte connu", 7),
                ("QUATRIEME", "IT_4E_EE_MESSAGE",       "Expression écrite", "Rédiger un court message, une carte ou un récit bref à partir d'un modèle", 8),
                ("QUATRIEME", "IT_4E_MED_REFORMULER",   "Médiation", "Reformuler pour un camarade l'essentiel d'un document simple", 9),
                ("QUATRIEME", "IT_4E_PHONO_ACCENT",     "Phonologie", "Identifier les mots selon leur accentuation et prononcer nettement les doubles consonnes", 10),
                ("QUATRIEME", "IT_4E_GRAM_PASSE_COMPOSE","Grammaire", "Former le passé composé avec essere ou avere et accorder le participe passé", 11),
                ("QUATRIEME", "IT_4E_GRAM_IMPARFAIT",   "Grammaire", "Conjuguer l'imparfait de l'indicatif pour décrire et raconter au passé", 12),
                ("QUATRIEME", "IT_4E_GRAM_FUTUR",       "Grammaire", "Conjuguer le futur simple pour parler d'un projet", 13),
                ("QUATRIEME", "IT_4E_GRAM_IMPERATIF",   "Grammaire", "Donner un ordre ou un conseil à l'impératif, à la forme affirmative et négative", 14),
                ("QUATRIEME", "IT_4E_GRAM_PRONOMS_COD", "Grammaire", "Remplacer un complément par un pronom COD (lo, la, li, le)", 15),
                ("QUATRIEME", "IT_4E_GRAM_PIACERE",     "Grammaire", "Construire piacere avec mi, ti, ci, vi, et dire « il faut » avec bisogna et occorre, « il suffit » avec basta", 16),
                ("QUATRIEME", "IT_4E_GRAM_PREP_DA",     "Grammaire", "Employer a, di, in, su, da contractées avec l'article et la relative simple introduite par che", 17),
                ("QUATRIEME", "IT_4E_LEX_VILLE_VOYAGE", "Lexique", "Employer le lexique du sport, du voyage, de la ville et de l'itinéraire", 18),
                ("QUATRIEME", "IT_4E_CULT_VOYAGES",     "Culture", "Découvrir l'Italie, terre de voyages : patrimoine touristique et grands explorateurs, dont Marco Polo", 19),
                ("QUATRIEME", "IT_4E_CULT_ARTS",        "Culture", "Découvrir des langages artistiques italiens : Renaissance, opéra, chanson, cinéma, art urbain", 20),
                ("QUATRIEME", "IT_4E_CULT_FLORENCE",    "Culture", "Connaître Florence : berceau de la Renaissance, capitale de la création, foyer de la langue italienne", 21),
                ("QUATRIEME", "IT_4E_CECRL",            "Méthode", "Consolider le niveau A1+ en fin de quatrième en LVB", 22),

                // --- 3e : A2 (programme 2020 : A2 dans au moins deux activités) ---
                ("TROISIEME", "IT_3E_CO_IDEE_GENERALE", "Compréhension de l'oral", "Dégager les idées principales et secondaires d'un document oral clair sur un sujet familier", 1),
                ("TROISIEME", "IT_3E_CE_RECIT",         "Compréhension de l'écrit", "Comprendre les informations simples d'un récit, d'un article ou d'un dialogue et les relier", 2),
                ("TROISIEME", "IT_3E_EO_QUOTIDIEN",     "Expression orale", "Décrire activités quotidiennes, personnes et lieux, et parler de ses projets proches", 3),
                ("TROISIEME", "IT_3E_EO_OPINION",       "Expression orale", "Dire ce qu'on aime ou non et donner son avis sur un sujet d'intérêt personnel", 4),
                ("TROISIEME", "IT_3E_EO_RECIT_PASSE",   "Expression orale", "Relater brièvement un événement ou une expérience passée en la situant dans le temps", 5),
                ("TROISIEME", "IT_3E_INT_TACHE",        "Interaction", "Échanger des informations simples et directes pour mener une tâche courante", 6),
                ("TROISIEME", "IT_3E_INT_PRECISIONS",   "Interaction", "Demander et donner des précisions, relancer et reformuler pour se faire comprendre", 7),
                ("TROISIEME", "IT_3E_EE_RECIT",         "Expression écrite", "Écrire le début ou la suite d'une histoire en s'aidant d'un dictionnaire", 8),
                ("TROISIEME", "IT_3E_EE_COURRIER",      "Expression écrite", "Rédiger un courriel ou une lettre simple avec les formules d'adresse et de congé", 9),
                ("TROISIEME", "IT_3E_MED_POINTS",       "Médiation", "Transmettre les points principaux d'un texte ou d'une conversation sur un sujet familier", 10),
                ("TROISIEME", "IT_3E_PHONO_CLARTE",     "Phonologie", "Lire et dire un court paragraphe avec une relative aisance : accent tonique, doubles consonnes, pas de nasales", 11),
                ("TROISIEME", "IT_3E_GRAM_TEMPS",       "Grammaire", "Choisir entre passé composé et imparfait dans un récit simple, et employer le futur simple", 12),
                ("TROISIEME", "IT_3E_GRAM_POLITESSE",   "Grammaire", "Vouvoyer avec Lei, le verbe à la troisième personne, et les formules scusi et dica", 13),
                ("TROISIEME", "IT_3E_GRAM_IMPERATIF",   "Grammaire", "Employer l'impératif à tu, noi et voi, et la défense avec non + infinitif à tu", 14),
                ("TROISIEME", "IT_3E_GRAM_GERONDIF",    "Grammaire", "Former le gérondif et dire l'action en cours avec stare + gérondif", 15),
                ("TROISIEME", "IT_3E_GRAM_SI_IMPERSONNEL","Grammaire", "Traduire « on » par la forme impersonnelle avec si, et « il faut » par ci vuole et ci vogliono", 16),
                ("TROISIEME", "IT_3E_GRAM_PLURIELS",    "Grammaire", "Former le pluriel des mots en -co et -go, des possessifs et des démonstratifs questo et quello", 17),
                ("TROISIEME", "IT_3E_LEX_TRAVAIL",      "Lexique", "Employer le lexique du travail, des migrations, des médias et de l'engagement", 18),
                ("TROISIEME", "IT_3E_CULT_MILAN",       "Culture", "Connaître Milan : capitale économique, ville innovante, espace urbain en mutation", 19),
                ("TROISIEME", "IT_3E_CULT_MIGRATIONS",  "Culture", "Comprendre les migrations italiennes : de terre de départ à terre d'accueil", 20),
                ("TROISIEME", "IT_3E_CULT_ENGAGEMENT",  "Culture", "Découvrir des formes d'engagement en Italie : slow food, écologie, lutte contre la mafia", 21),
                ("TROISIEME", "IT_3E_CECRL",            "Méthode", "Atteindre le niveau A2 dans au moins deux activités langagières en fin de troisième", 22),

                // --- Seconde : A2+ visé (annexe 14, en vigueur depuis 2025) ---
                ("SECONDE", "IT_2DE_CO_INFOS",          "Compréhension de l'oral", "Comprendre les informations principales d'une conversation ou d'une émission claire et lente", 1),
                ("SECONDE", "IT_2DE_CO_HYPOTHESES",     "Compréhension de l'oral", "Formuler des hypothèses à partir des indices et vérifier sa compréhension par étapes", 2),
                ("SECONDE", "IT_2DE_CE_TEXTES",         "Compréhension de l'écrit", "Comprendre les informations principales de textes variés sur des sujets familiers", 3),
                ("SECONDE", "IT_2DE_EO_PRESENTER",      "Expression orale", "Présenter son quotidien, des projets, des activités passées et des expériences personnelles", 4),
                ("SECONDE", "IT_2DE_EO_JUSTIFIER",      "Expression orale", "Expliquer pourquoi une chose plaît ou déplaît et comparer de façon simple", 5),
                ("SECONDE", "IT_2DE_INT_ECHANGER",      "Interaction", "Échanger idées et renseignements avec une aisance raisonnable en situation prévisible", 6),
                ("SECONDE", "IT_2DE_EE_RENDRE_COMPTE",  "Expression écrite", "Rendre compte par écrit d'un document étudié et en rédiger un bref résumé", 7),
                ("SECONDE", "IT_2DE_EE_TRANSFORMER",    "Expression écrite", "Transformer un document : article en interview, récit en saynète", 8),
                ("SECONDE", "IT_2DE_MED_INFOS",         "Médiation", "Transmettre les informations pertinentes d'un texte informatif court et bien structuré", 9),
                ("SECONDE", "IT_2DE_PHONO_ACCENTS",     "Phonologie", "Identifier les mots selon leur accentuation et placer l'accent tonique", 10),
                ("SECONDE", "IT_2DE_PHONO_LECTURE",     "Phonologie", "Lire, réciter ou jouer un texte court avec une relative fluidité, doubles consonnes comprises", 11),
                ("SECONDE", "IT_2DE_GRAM_PRESENT",      "Grammaire", "Conjuguer au présent les irréguliers bere, uscire, dire, venire et les verbes pronominaux", 12),
                ("SECONDE", "IT_2DE_GRAM_TEMPS",        "Grammaire", "Employer passé composé avec ses accords, imparfait et futur simple", 13),
                ("SECONDE", "IT_2DE_GRAM_POLITESSE",    "Grammaire", "Vouvoyer avec Lei et ses possessifs, et employer l'impératif à tu, noi et voi", 14),
                ("SECONDE", "IT_2DE_GRAM_PRONOMS",      "Grammaire", "Employer les pronoms COD et COI (lo, la, li, le, gli) et construire piacere à toutes les personnes", 15),
                ("SECONDE", "IT_2DE_GRAM_ENCLISE",      "Grammaire", "Accrocher le pronom à l'impératif et à l'infinitif (dimmi, vederlo)", 16),
                ("SECONDE", "IT_2DE_GRAM_RELATIFS",     "Grammaire", "Relier deux propositions avec che et avec les relatifs composés de cui", 17),
                ("SECONDE", "IT_2DE_GRAM_GERONDIF_SI",  "Grammaire", "Employer stare + gérondif et la forme impersonnelle avec si", 18),
                ("SECONDE", "IT_2DE_LEX_AXES",          "Lexique", "Mobiliser le lexique du portrait, de la famille, des traditions, du patrimoine et des transitions", 19),
                ("SECONDE", "IT_2DE_CULT_RENAISSANCE",  "Culture", "Connaître la Renaissance : héritage de l'Antiquité, humanisme, rayonnement dans le monde", 20),
                ("SECONDE", "IT_2DE_CULT_PASSE",        "Culture", "Analyser comment l'Italie fait vivre son passé : traditions populaires, città d'arte, lieux de mémoire", 21),
                ("SECONDE", "IT_2DE_CECRL",             "Méthode", "Atteindre le niveau A2+ visé en fin de seconde en LVB", 22),

                // --- Première : B1 visé (annexe 14, en vigueur en 2026) ---
                ("PREMIERE", "IT_1RE_CO_POINTS",        "Compréhension de l'oral", "Comprendre les points principaux d'un bulletin d'information ou d'un film en langue claire", 1),
                ("PREMIERE", "IT_1RE_CE_RECIT",         "Compréhension de l'écrit", "Suivre l'intrigue d'un récit simple en y reconnaissant les formes du passé simple", 2),
                ("PREMIERE", "IT_1RE_EO_EXPOSE",        "Expression orale", "Faire un exposé préparé, suivi sans difficulté, sur un sujet familier", 3),
                ("PREMIERE", "IT_1RE_EO_OPINION",       "Expression orale", "Exprimer et justifier une opinion en comparant et en pesant le pour et le contre", 4),
                ("PREMIERE", "IT_1RE_INT_CONVERSATION", "Interaction", "Prendre part à une conversation prolongée sur un sujet familier en prenant des initiatives", 5),
                ("PREMIERE", "IT_1RE_INT_NUANCE",       "Interaction", "Exprimer accord ou désaccord avec nuance et reformuler pour vérifier la compréhension", 6),
                ("PREMIERE", "IT_1RE_EE_CRITIQUE",      "Expression écrite", "Rédiger une critique simple d'un film ou d'un livre en justifiant son point de vue", 7),
                ("PREMIERE", "IT_1RE_EE_CONTROLER",     "Expression écrite", "Contrôler sa production après coup et la structurer avec des connecteurs variés", 8),
                ("PREMIERE", "IT_1RE_MED_RESUMER",      "Médiation", "Résumer en français l'information et les arguments d'un dossier en italien", 9),
                ("PREMIERE", "IT_1RE_MED_IMPLICITE",    "Médiation", "Expliciter pour autrui une référence culturelle implicite présente dans un message", 10),
                ("PREMIERE", "IT_1RE_PHONO_SPONTANE",   "Phonologie", "S'exprimer spontanément et de façon intelligible : accent tonique, diphtongues, consonnes géminées", 11),
                ("PREMIERE", "IT_1RE_GRAM_SUBJONCTIF",  "Grammaire", "Conjuguer le subjonctif présent et l'employer après un verbe d'opinion ou bisogna che", 12),
                ("PREMIERE", "IT_1RE_GRAM_COMPARATIF",  "Grammaire", "Comparer : égalité, supériorité, infériorité", 13),
                ("PREMIERE", "IT_1RE_GRAM_PROCHES",     "Grammaire", "Exprimer le passé proche avec appena et le futur proche avec stare per", 14),
                ("PREMIERE", "IT_1RE_GRAM_PASSIF",      "Grammaire", "Construire la voix passive", 15),
                ("PREMIERE", "IT_1RE_GRAM_CONNECTEURS", "Grammaire", "Articuler opposition, conséquence et condition : invece, però, quindi, perciò, se, anche se", 16),
                ("PREMIERE", "IT_1RE_GRAM_INDEFINIS",   "Grammaire", "Employer qualche, alcuni, qualunque, qualsiasi et placer niente et nessuno par rapport au verbe", 17),
                ("PREMIERE", "IT_1RE_LEX_DERIVATION",   "Lexique", "Enrichir son lexique par les suffixes et mobiliser celui de l'identité, de l'art, de la science et de la nature", 18),
                ("PREMIERE", "IT_1RE_CULT_UNITE",       "Culture", "Connaître la construction de l'Italie : Risorgimento, unité, « fare gli Italiani »", 19),
                ("PREMIERE", "IT_1RE_CULT_ART_POUVOIR", "Culture", "Analyser les liens entre art et pouvoir : mécénat, art contestataire, satire", 20),
                ("PREMIERE", "IT_1RE_CULT_IDENTITES",   "Culture", "Étudier identités et échanges : l'Italie entre émigration et immigration, creuset culturel", 21),
                ("PREMIERE", "IT_1RE_CECRL",            "Méthode", "Atteindre le niveau B1 visé en fin de première en LVB", 22),

                // --- Terminale : B1 attendu (annexe 14) ---
                ("TERMINALE", "IT_TLE_CO_DOCUMENT",     "Compréhension de l'oral", "Rendre compte en français d'un document audio ou vidéo d'une minute trente écouté trois fois", 1),
                ("TERMINALE", "IT_TLE_CE_DOSSIER",      "Compréhension de l'écrit", "Rendre compte d'un ou deux textes, de façon libre ou guidée, en français ou en italien", 2),
                ("TERMINALE", "IT_TLE_CE_LIENS",        "Compréhension de l'écrit", "Sélectionner et hiérarchiser les informations et relier plusieurs documents entre eux", 3),
                ("TERMINALE", "IT_TLE_EO_AXE",          "Expression orale", "Expliquer en cinq minutes quel document ou citation illustre le mieux un axe étudié", 4),
                ("TERMINALE", "IT_TLE_EO_ARGUMENTER",   "Expression orale", "Argumenter pour expliciter son raisonnement, notamment en vue du grand oral", 5),
                ("TERMINALE", "IT_TLE_INT_ENTRETIEN",   "Interaction", "Soutenir un entretien élargi sur le travail mené autour d'un axe du programme", 6),
                ("TERMINALE", "IT_TLE_EE_ARGUMENTATIF", "Expression écrite", "Répondre en italien à une ou deux questions liées au dossier par un texte argumenté", 7),
                ("TERMINALE", "IT_TLE_EE_CREATIF",      "Expression écrite", "Produire un écrit créatif : suite de texte, lettre, dialogue, critique de film", 8),
                ("TERMINALE", "IT_TLE_MED_TRADUIRE",    "Médiation", "S'initier à la traduction en comparant des textes traduits à leur original", 9),
                ("TERMINALE", "IT_TLE_MED_SYNTHESE",    "Médiation", "Rassembler des informations de sources diverses et les résumer pour quelqu'un d'autre", 10),
                ("TERMINALE", "IT_TLE_PHONO_FLUIDITE",  "Phonologie", "Soigner accentuation, musicalité et débit pour enchaîner plusieurs phrases de façon intelligible", 11),
                ("TERMINALE", "IT_TLE_GRAM_COND_PASSE", "Grammaire", "Conjuguer le conditionnel passé", 12),
                ("TERMINALE", "IT_TLE_GRAM_PROGRESSIVE","Grammaire", "Employer la forme progressive stare + gérondif à l'imparfait et au futur", 13),
                ("TERMINALE", "IT_TLE_GRAM_IMPERSONNEL","Grammaire", "Construire è + adjectif + infinitif à tous les temps (era, sarà, sarebbe)", 14),
                ("TERMINALE", "IT_TLE_GRAM_PASSE_SIMPLE","Grammaire", "Reconnaître le passé simple dans un texte, sans avoir à le produire", 15),
                ("TERMINALE", "IT_TLE_GRAM_MAITRISE",   "Grammaire", "Maîtriser pronoms COD et COI, forme de politesse, relatifs avec cui et verbes de mouvement suivis de a", 16),
                ("TERMINALE", "IT_TLE_LEX_SUFFIXES",    "Lexique", "Nuancer avec les suffixes diminutifs, augmentatifs et dépréciatifs et avec des connecteurs logiques", 17),
                ("TERMINALE", "IT_TLE_LEX_AXES",        "Lexique", "Mobiliser le lexique de la mémoire, de l'engagement, de la communication et de l'Europe", 18),
                ("TERMINALE", "IT_TLE_CULT_EUROPE",     "Culture", "Analyser l'Italie de la République à l'Europe : antifascisme, traité de Rome, citoyenneté européenne", 19),
                ("TERMINALE", "IT_TLE_CULT_MEMOIRE",    "Culture", "Étudier territoire et mémoire : devoir de mémoire, mémoires difficiles, ville-musée", 20),
                ("TERMINALE", "IT_TLE_CULT_ESPACES",    "Culture", "Interroger espace privé et espace public : la piazza, place des femmes, engagement antimafia", 21),
                ("TERMINALE", "IT_TLE_CECRL",           "Méthode", "Atteindre le niveau B1 visé en fin de terminale en LVB", 22),
            };
    }
}
