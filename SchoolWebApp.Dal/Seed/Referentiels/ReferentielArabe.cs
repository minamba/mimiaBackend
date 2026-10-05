namespace SchoolWebApp.Dal.Seed.Referentiels
{
    public static class ReferentielArabe
    {
        /// <summary>
        /// Référentiel d'ARABE enseigné en LANGUE VIVANTE B (LV2), de la
        /// cinquième à la terminale, voies générale et technologique.
        /// Construit sur le modèle de `ReferentielEspagnol` : mêmes domaines,
        /// même grain, mêmes codes de niveau.
        ///
        /// ================== PROVENANCE (lue le 04/10/2026) ==================
        ///
        ///   NIVEAU   TEXTE APPLICABLE EN 2026-2027                       DEPUIS
        ///   --------------------------------------------------------------------
        ///   5e       Arrêté du 5-5-2025, MENE2504621A, BO n° 22 du       rentrée 2026
        ///            29-5-2025, ANNEXE 5 « Programme d'arabe pour les
        ///            classes de collège »
        ///   4e       Programme du cycle 4, arrêté du 9-11-2015 modifié,  rentrée 2020
        ///            BO n° 31 du 30-7-2020 (volet LV commun à toutes     (annexe 5 :
        ///            les langues). L'annexe 5 ne s'y applique qu'en      rentrée 2027)
        ///            2027 (art. 4 de MENE2504621A).
        ///   3e       Même texte de 2020. Annexe 5 : rentrée 2028.        rentrée 2020
        ///   2de GT   MENE2504621A, ANNEXE 6 « Programme d'arabe pour     rentrée 2025
        ///            les classes de lycée général et technologique »
        ///   1re GT   MENE2504621A, annexe 6                              rentrée 2026
        ///   Tle GT   MENE2504621A, annexe 6                              rentrée 2026
        ///
        ///   Article 4 de MENE2504621A, relu le 04/10/2026 sur la page du BO :
        ///   application « à la rentrée de l'année scolaire 2025-2026 en tant
        ///   qu'elles s'appliquent à la classe de sixième et à la classe de
        ///   seconde générale et technologique, à la rentrée [...] 2026-2027
        ///   [...] à la classe de cinquième et aux classes de premières et
        ///   terminales générales et technologiques, à la rentrée [...]
        ///   2027-2028 [...] à la classe de quatrième et à la rentrée [...]
        ///   2028-2029 [...] à la classe de troisième ».
        ///   La page du BO liste bien 25 annexes ; l'arabe y occupe les
        ///   annexes 5 (collège) et 6 (lycée général et technologique).
        ///
        ///   URL (lues le 04/10/2026 ; les deux PDF ont été lus EN ENTIER,
        ///   texte extrait par pdftotext) :
        ///     https://www.education.gouv.fr/bo/2025/Hebdo22/MENE2504621A
        ///     https://www.education.gouv.fr/sites/default/files/annexe-5-programme-d-arabe-pour-les-classes-de-coll-ge-440364.pdf
        ///     https://www.education.gouv.fr/sites/default/files/annexe-6-programme-d-arabe-pour-les-classes-de-lyc-e-g-n-ral-et-technologique-440367.pdf
        ///     https://eduscol.education.gouv.fr/sites/default/files/document/programme-d-enseignement-du-cycle-4-67722.pdf
        ///   (les mêmes PDF — même taille, même texte extrait — sont aussi sous
        ///   .../files/ensel621_annexe5.pdf et .../files/ensel621_annexe6.pdf)
        ///
        ///   Horaires de la LVB : voir l'en-tête de `ReferentielEspagnol.cs`
        ///   (ils sont communs à toutes les langues ; non relus ici).
        ///
        /// ================== NIVEAUX CECRL VISÉS EN LVB ==================
        ///
        ///   Annexes 5 et 6, tableau du préambule commun — IDENTIQUES à ceux
        ///   de l'espagnol, l'arabe n'a pas d'échelle à part en LVB :
        ///     5e A1+   4e A1+   3e A2   2de A2+   1re B1   Tle B1
        ///   « + » : « le niveau supérieur est atteint dans au moins l'une des
        ///   activités langagières ». (6e bilangue : A1.)
        ///   La seule dérogation liée à l'écriture concerne la LVC, pas la
        ///   LVB : « pour certaines langues [...] n'utilisant pas l'alphabet
        ///   romain, il est difficile d'atteindre le niveau B1 dans les
        ///   compétences d'écrit », d'où A2+ / B1 en terminale LVC.
        ///   Programme de 2020, encore applicable en 4e et 3e : « Pour la LV2,
        ///   le niveau A2 du CECRL dans au moins deux activités langagières »,
        ///   en FIN DE CYCLE seulement. Le A1+ de 4e vient du nouveau texte :
        ///   c'est un repère, pas encore une obligation.
        ///   Annexe 5, 5e : la classe mêle des élèves venus de 6e bilangue et
        ///   des DÉBUTANTS ; c'est pourquoi le système graphique et les sons
        ///   (listés au niveau A1 de la 6e) figurent ici en cinquième.
        ///
        /// ================== CE QUE LE TEXTE DIT DE LA LANGUE ==================
        ///
        ///   Annexe 5, « Focus sur la langue enseignée » : la langue enseignée
        ///   est « la langue arabe moderne standard, dite "arabe littéral" ».
        ///   L'oral peut mêler standard et dialectal (« arabe moyen »), les
        ///   documents en dialecte servent à sensibiliser aux usages réels,
        ///   sans pénaliser les non-dialectophones ; « l'arabe standard
        ///   moderne demeure la langue cible » et la production ÉCRITE se fait
        ///   en arabe standard. Les voyelles finales (déclinaison) ne sont
        ///   attendues qu'à partir du niveau B2 : hors LVB.
        ///   Annexe 5, « Focus sur l'apprentissage du système graphique » :
        ///   le texte EXCLUT « le recours à la notation systématique des
        ///   signes diacritiques, dits de vocalisation » ; il admet seulement
        ///   « l'adjonction de quelques voyelles fonctionnelles ou non
        ///   prédictibles, notamment lors de la découverte d'un mot nouveau ».
        ///   Le duel et le féminin pluriel des conjugaisons, des pronoms, des
        ///   noms et des adjectifs sont marqués B1+ / B2 : ils relèvent de la
        ///   LVA et ne sont PAS demandés ici.
        ///
        /// ================== CE QUI EST PRESCRIPTIF ==================
        ///
        ///   PRESCRIPTIF : les niveaux CECRL ; les axes ; l'axe 6 OBLIGATOIRE
        ///   (de la 5e à la terminale en voie générale : 5 axes sur 6 dont
        ///   l'axe 6). En voie TECHNOLOGIQUE : au moins 3 axes, l'axe 6
        ///   « vivement recommandé ».
        ///   Axes 6 de l'arabe : 5e Algérie, Maroc, Tunisie ; 4e Égypte,
        ///   Soudan ; 3e Liban, Syrie ; 2de les sept pays de la Péninsule
        ///   arabique ; 1re trois pays du Maghreb (un palais ou un musée et un
        ///   personnage emblématique) ; Tle l'Égypte et le Liban (création
        ///   littéraire et artistique).
        ///   INDICATIF : les objets d'étude (« proposés à titre indicatif ») et
        ///   les listes de phonologie, de lexique et de grammaire
        ///   (« indicatives et non exhaustives »). Les points de langue retenus
        ///   ici suivent ces listes : ils ne sont pas exigibles un par un.
        ///   On n'a gardé que la colonne LVB : ce qui est étiqueté au niveau
        ///   supérieur (A2 en 5e, A2+ en 4e, B1 en 3e, B1+ en 2de et 1re, B2
        ///   en terminale) relève de la LVA et n'est pas repris.
        ///
        ///   4e et 3e : comme en espagnol, les axes 6 du nouveau programme
        ///   (Égypte et Soudan, Liban et Syrie) et ses listes de langue
        ///   figurent déjà dans le référentiel. Ils s'inscrivent dans les
        ///   quatre thèmes de 2020 (langages ; école et société ; voyages et
        ///   migrations ; rencontres avec d'autres cultures) mais ne deviennent
        ///   obligatoires qu'en 2027 et en 2028.
        ///   Le domaine « Méthode » ne porte que la ligne CECRL.
        ///
        /// ================== NON VÉRIFIÉ ==================
        ///
        ///   - Les COULEURS des annexes : l'extraction de texte perd le bleu
        ///     qui marque le niveau supérieur. On s'est fié aux étiquettes
        ///     écrites en toutes lettres — « (A2) », « (B1+) », « (B2) »... —
        ///     devant chaque fait de langue. Un point marqué en bleu SANS
        ///     étiquette aurait été pris à tort pour de la LVB (risque surtout
        ///     en terminale : schèmes verbaux dérivés, masdars et participes
        ///     des formes dérivées, complément de manière).
        ///   - Les mots arabes cités dans les libellés ont été RETAPÉS (le PDF
        ///     les restitue dans le désordre) : orthographe à faire relire par
        ///     un arabisant. Aucun n'est une citation littérale garantie.
        ///   - 4e et 3e : le programme de 2020 n'a AUCUN contenu propre à
        ///     l'arabe (volet commun à toutes les langues). Les points de
        ///     langue et de culture de ces deux niveaux viennent de l'annexe 5,
        ///     pas encore en vigueur : ce sont des repères. Les ressources
        ///     Éduscol propres à l'arabe (repères de progressivité de 2016,
        ///     attendus de fin d'année) n'ont PAS été lues.
        ///   - Les libellés d'activités langagières (compréhension, expression,
        ///     interaction, médiation) reprennent les descripteurs « Ce que
        ///     sait faire l'élève » des annexes, communs à toutes les langues ;
        ///     ceux de 4e sur le « message » et le « document » simples sont
        ///     repris du modèle espagnol, non relus dans le texte de 2020.
        ///   - Aucun format d'épreuve n'est repris ici (pas de « document
        ///     d'une minute trente écouté trois fois ») : la note de service
        ///     MENE2618195N sur les évaluations n'a pas été relue.
        ///   - Annexe 6, 1re, axe 6 : le texte officiel écrit « Tunisie :
        ///     l'Université Al Quaraouiyine et le rôle de Fatima al-Fihriya »,
        ///     alors que la Qarawiyyin est à Fès (Maroc). On n'a pas repris
        ///     cet objet d'étude dans les libellés.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- 5e : LVB débutant, A1+ visé (annexe 5, en vigueur) ---
                ("CINQUIEME", "AR_5E_CO_CONSIGNES",     "Compréhension de l'oral", "Comprendre des consignes de classe et des énoncés très simples prononcés lentement", 1),
                ("CINQUIEME", "AR_5E_CO_DISCRIMINER",   "Compréhension de l'oral", "Distinguer à l'écoute les mots-clés, noms propres et mots répétés pour identifier le thème d'un document court", 2),
                ("CINQUIEME", "AR_5E_CE_DECHIFFRER",    "Compréhension de l'écrit", "Déchiffrer de droite à gauche des mots et des phrases courtes en reconnaissant les lettres liées et les six qui ne s'attachent pas à la suivante", 3),
                ("CINQUIEME", "AR_5E_CE_SANS_VOYELLES", "Compréhension de l'écrit", "Lire un texte court et concret écrit sans voyelles brèves, comme dans l'usage courant, et le relier à un titre ou à un thème", 4),
                ("CINQUIEME", "AR_5E_EO_PRESENTER",     "Expression orale", "Se présenter : nom, âge, famille, nationalité, d'où l'on vient, où l'on habite", 5),
                ("CINQUIEME", "AR_5E_EO_DECRIRE",       "Expression orale", "Décrire très simplement une personne, un objet ou un lieu à partir d'un modèle", 6),
                ("CINQUIEME", "AR_5E_EO_GOUTS",         "Expression orale", "Exprimer ses goûts et préférences avec أحب, لا أحب, أفضّل (uḥibbu, lā uḥibbu, ufaḍḍilu)", 7),
                ("CINQUIEME", "AR_5E_INT_QUESTIONS",    "Interaction", "Poser des questions simples avec هل, من, ماذا, أين et y répondre dans des situations répétées", 8),
                ("CINQUIEME", "AR_5E_INT_POLITESSE",    "Interaction", "Saluer, prendre congé, remercier, s'excuser et demander de répéter ou d'épeler", 9),
                ("CINQUIEME", "AR_5E_EE_GRAPHIE",       "Expression écrite", "Tracer et lier les lettres, noter voyelles longues, chadda et tā' marbūṭa, copier et écrire sous la dictée", 10),
                ("CINQUIEME", "AR_5E_EE_TEXTE_COURT",   "Expression écrite", "Rédiger en arabe standard un court texte imitatif ou un message très simple à partir d'un modèle", 11),
                ("CINQUIEME", "AR_5E_MED_INFOS",        "Médiation", "Transmettre les informations factuelles d'une invitation ou d'un prospectus : lieu, horaire, prix", 12),
                ("CINQUIEME", "AR_5E_PHONO_SONS",       "Phonologie", "Réaliser les sons propres à l'arabe : ع, ح, خ, ق, la hamza, les emphatiques ص ض ط ظ, voyelles brèves et longues", 13),
                ("CINQUIEME", "AR_5E_PHONO_SOLAIRES",   "Phonologie", "Prononcer l'article devant lettres solaires (الشمس) et lunaires (القمر), la tā' marbūṭa, et distinguer ع/غ, ق/ك", 14),
                ("CINQUIEME", "AR_5E_GRAM_PHRASE_NOMINALE", "Grammaire", "Former des phrases nominales sans verbe « être » (أنا تونسي) et les nier avec ليس ou ليست", 15),
                ("CINQUIEME", "AR_5E_GRAM_INACCOMPLI",  "Grammaire", "Conjuguer à l'inaccompli les verbes sains (يدرس, يكتب) et le nier avec لا, hors duel et féminin pluriel", 16),
                ("CINQUIEME", "AR_5E_GRAM_FUTUR",       "Grammaire", "Exprimer le futur avec le préfixe سـ et le futur proche avec l'inaccompli seul", 17),
                ("CINQUIEME", "AR_5E_GRAM_GN",          "Grammaire", "Employer l'article الـ, le genre, l'adjectif placé après le nom et accordé (قلم أسود / القلم الأسود)", 18),
                ("CINQUIEME", "AR_5E_GRAM_ANNEXION",    "Grammaire", "Employer l'annexion (الإضافة), les pronoms isolés et affixes (اسمك, مدرستنا) et la possession avec عند, مع", 19),
                ("CINQUIEME", "AR_5E_GRAM_PLURIELS",    "Grammaire", "Former les pluriels externes (مصريون, مصريات), reconnaître les pluriels brisés (أولاد) et accorder les pluriels inanimés", 20),
                ("CINQUIEME", "AR_5E_LEX_QUOTIDIEN",    "Lexique", "Employer le lexique du portrait, du corps, des couleurs, des saisons, des fêtes et de l'hospitalité", 21),
                ("CINQUIEME", "AR_5E_LEX_CHIFFRES",     "Lexique", "Compter, dire l'heure et l'année, et lire la date écrite en chiffres indiens (١ ٢ ٣)", 22),
                ("CINQUIEME", "AR_5E_CULT_MAGHREB",     "Culture", "Découvrir trois pays du Maghreb, Algérie, Maroc, Tunisie : mer en partage, chansons, célébrations, plats", 23),
                ("CINQUIEME", "AR_5E_CULT_MONDE_ARABE", "Culture", "Situer les pays arabes et découvrir la calligraphie, le zellige, les mots voyageurs et le personnage de Joha", 24),
                ("CINQUIEME", "AR_5E_CECRL",            "Méthode", "Atteindre le niveau A1+ visé en fin de cinquième pour une LVB débutée cette année", 25),

                // --- 4e : A1+ consolidé (programme 2020 en vigueur, annexe 5 en 2027) ---
                ("QUATRIEME", "AR_4E_CO_MESSAGE",       "Compréhension de l'oral", "Isoler des informations simples d'un message oral clair et lent et les relier à un thème", 1),
                ("QUATRIEME", "AR_4E_CE_DOCUMENT",      "Compréhension de l'écrit", "Comprendre un document écrit simple et court : affiche, message, récit bref, page web", 2),
                ("QUATRIEME", "AR_4E_CE_RACINES",       "Compréhension de l'écrit", "Inférer le sens d'un mot en repérant sa racine, un schème fréquent et les marques de genre et de nombre", 3),
                ("QUATRIEME", "AR_4E_EO_DECRIRE",       "Expression orale", "Décrire en coordonnant plusieurs éléments simples : personne, lieu, activité", 4),
                ("QUATRIEME", "AR_4E_EO_RACONTER",      "Expression orale", "Raconter brièvement un événement ou une expérience à l'aide d'un répertoire mémorisé", 5),
                ("QUATRIEME", "AR_4E_EO_PREFERENCE",    "Expression orale", "Exprimer un goût, un souhait ou une préférence sur un sujet étudié", 6),
                ("QUATRIEME", "AR_4E_INT_ECHANGE",      "Interaction", "Engager et clore une conversation simple, demander et donner des nouvelles", 7),
                ("QUATRIEME", "AR_4E_INT_ACCORD",       "Interaction", "Exprimer accord, désaccord, autorisation et interdiction par des formules (ممكن؟ ممنوع)", 8),
                ("QUATRIEME", "AR_4E_EE_MESSAGE",       "Expression écrite", "Rédiger en arabe standard un court message, une carte ou un récit bref à partir d'un modèle", 9),
                ("QUATRIEME", "AR_4E_MED_REFORMULER",   "Médiation", "Expliciter pour un camarade le sens d'un document simple et vérifier qu'il a compris", 10),
                ("QUATRIEME", "AR_4E_PHONO_PAIRES",     "Phonologie", "Distinguer à l'écoute et à l'oral les paires proches : ع/غ, ق/ك, ط/ت, ذ/ز, ض/د", 11),
                ("QUATRIEME", "AR_4E_PHONO_LIAISON",    "Phonologie", "Assimiler l'article devant les lettres solaires et prononcer la tā' marbūṭa en finale ou en annexion", 12),
                ("QUATRIEME", "AR_4E_GRAM_FUTUR",       "Grammaire", "Employer le futur avec سـ et l'inaccompli pour parler de ses habitudes et de ses projets", 13),
                ("QUATRIEME", "AR_4E_GRAM_GN_DEFINI",   "Grammaire", "Mettre ou non l'article الـ devant chaque élément du groupe nominal (قلم أسود / القلم الأسود)", 14),
                ("QUATRIEME", "AR_4E_GRAM_PLURIELS",    "Grammaire", "Employer et accorder les pluriels courants, inanimés compris (البلدان العربية), et comprendre les pluriels brisés", 15),
                ("QUATRIEME", "AR_4E_GRAM_NEGATION",    "Grammaire", "Nier la phrase nominale avec ليس ou ليست, sans conjugaison complète ni déclinaison de l'attribut", 16),
                ("QUATRIEME", "AR_4E_GRAM_IL_Y_A",      "Grammaire", "Exprimer « il y a » par le groupe prépositionnel en tête (في الصورة امرأة) et « avoir » avec مع", 17),
                ("QUATRIEME", "AR_4E_GRAM_PHRASE_VERBALE", "Grammaire", "Construire une phrase verbale simple dans l'ordre verbe-sujet-objet et l'enchaîner avec ثمّ, بعد ذلك", 18),
                ("QUATRIEME", "AR_4E_GRAM_INTENSITE",   "Grammaire", "Nuancer avec جدًّا, كثيرًا, قليلًا, بسرعة et dire une année (1973, 2024)", 19),
                ("QUATRIEME", "AR_4E_LEX_SPORT_VILLE",  "Lexique", "Employer le lexique du sport, du voyage, de la ville et de ses lieux emblématiques", 20),
                ("QUATRIEME", "AR_4E_CULT_VOYAGES",     "Culture", "Découvrir voyages et explorations dans le monde arabe, d'Ibn Baṭṭūṭa aux carnets de voyage d'aujourd'hui", 21),
                ("QUATRIEME", "AR_4E_CULT_SCIENCES_ARTS", "Culture", "Découvrir l'âge d'or des sciences arabes et des langages artistiques : dabké, Ḥurufiyya, cinéma, conteur", 22),
                ("QUATRIEME", "AR_4E_CULT_EGYPTE_SOUDAN", "Culture", "Connaître l'Égypte et le Soudan : le Nil en partage, chansons, célébrations, plats populaires", 23),
                ("QUATRIEME", "AR_4E_CECRL",            "Méthode", "Consolider le niveau A1+ en fin de quatrième en LVB", 24),

                // --- 3e : A2 (programme 2020 : A2 dans au moins deux activités) ---
                ("TROISIEME", "AR_3E_CO_INFOS_SIMPLES", "Compréhension de l'oral", "Repérer des informations simples dans une histoire ou une conversation dite clairement et lentement", 1),
                ("TROISIEME", "AR_3E_CO_VARIETES",      "Compréhension de l'oral", "Reconnaître dans une chanson ou un film une part d'arabe dialectal et la rapporter à l'arabe standard", 2),
                ("TROISIEME", "AR_3E_CE_RECIT",         "Compréhension de l'écrit", "Comprendre les informations simples d'un récit, d'un article ou d'un échange écrit en langue usuelle", 3),
                ("TROISIEME", "AR_3E_EO_QUOTIDIEN",     "Expression orale", "Décrire activités quotidiennes, personnes et lieux, et parler de ses projets proches", 4),
                ("TROISIEME", "AR_3E_EO_OPINION",       "Expression orale", "Dire ce qu'on aime ou non et donner son avis avec أظنّ أنّ, يبدو لي أنّ", 5),
                ("TROISIEME", "AR_3E_EO_RECIT_PASSE",   "Expression orale", "Relater brièvement un événement, des activités passées ou une expérience personnelle", 6),
                ("TROISIEME", "AR_3E_INT_TACHE",        "Interaction", "Échanger des informations simples et directes pour mener une tâche courante", 7),
                ("TROISIEME", "AR_3E_INT_PRECISIONS",   "Interaction", "Demander de l'aide, faire répéter ou préciser, et relancer par des questions simples", 8),
                ("TROISIEME", "AR_3E_EE_RECIT",         "Expression écrite", "Écrire en arabe standard le début ou la suite d'une histoire en s'aidant d'un dictionnaire", 9),
                ("TROISIEME", "AR_3E_EE_COURRIER",      "Expression écrite", "Rédiger un message ou un courrier simple avec les formules d'adresse et de congé (تحياتي)", 10),
                ("TROISIEME", "AR_3E_MED_POINTS",       "Médiation", "Transmettre les points principaux d'un texte ou d'une conversation sur un sujet familier", 11),
                ("TROISIEME", "AR_3E_PHONO_PROXIMITE",  "Phonologie", "Distinguer des sons voisins qui se suivent (ر et ع dans عربية) et les emphatiques des non emphatiques", 12),
                ("TROISIEME", "AR_3E_GRAM_ACCOMPLI",    "Grammaire", "Conjuguer à l'accompli les verbes sains (كتبت, درسنا) et quelques verbes malades courants (رأيت)", 13),
                ("TROISIEME", "AR_3E_GRAM_KANA",        "Grammaire", "Conjuguer كان à l'accompli et à l'inaccompli, hors duel et féminin pluriel", 14),
                ("TROISIEME", "AR_3E_GRAM_NEGATIONS",   "Grammaire", "Nier le passé avec ما ou لم et le futur avec لن, sans systématiser la conjugaison", 15),
                ("TROISIEME", "AR_3E_GRAM_COMPLETIVES", "Grammaire", "Exprimer la cause avec لأنّ, le but avec لـ, et construire une complétive avec أنّ ou أن", 16),
                ("TROISIEME", "AR_3E_GRAM_INNA",        "Grammaire", "Employer le pronom affixe comme sujet après إنّ et أنّ (قال إنّه يتعلم العربية)", 17),
                ("TROISIEME", "AR_3E_GRAM_SCHEMES",     "Grammaire", "Relier un mot à sa racine et reconnaître les schèmes فاعل et مفعول", 18),
                ("TROISIEME", "AR_3E_LEX_TRAVAIL_MEDIAS", "Lexique", "Employer le lexique des métiers, du tourisme, des migrations, de la presse et de l'engagement", 19),
                ("TROISIEME", "AR_3E_LEX_REGISTRES",    "Lexique", "Nommer les variétés de la langue, فصحى, عامية, دارجة, et choisir le mot standard à l'écrit", 20),
                ("TROISIEME", "AR_3E_CULT_LIBAN_SYRIE", "Culture", "Connaître le Liban et la Syrie : poésie, chansons, sociétés composites, cuisine qui traverse les frontières", 21),
                ("TROISIEME", "AR_3E_CULT_ENGAGEMENT",  "Culture", "Découvrir des formes d'engagement dans le monde arabe : associations, arts urbains, femmes engagées", 22),
                ("TROISIEME", "AR_3E_CULT_RENCONTRES",  "Culture", "Étudier rencontres, voyages et migrations entre le monde arabe et l'Occident, dont Paris vu par des écrivains arabes", 23),
                ("TROISIEME", "AR_3E_CECRL",            "Méthode", "Atteindre le niveau A2 dans au moins deux activités langagières en fin de troisième", 24),

                // --- Seconde : A2+ visé (annexe 6, en vigueur depuis 2025) ---
                ("SECONDE", "AR_2DE_CO_INFOS",          "Compréhension de l'oral", "Comprendre les informations principales d'une conversation ou d'une émission claire et lente", 1),
                ("SECONDE", "AR_2DE_CO_INDICES",        "Compréhension de l'oral", "S'appuyer sur les indices visuels, sonores et culturels pour construire le sens d'un document", 2),
                ("SECONDE", "AR_2DE_CE_TEXTES",         "Compréhension de l'écrit", "Comprendre les informations principales de textes variés, non vocalisés, sur des sujets familiers", 3),
                ("SECONDE", "AR_2DE_EO_PRESENTER",      "Expression orale", "Présenter son quotidien, des projets, des activités passées et des expériences personnelles", 4),
                ("SECONDE", "AR_2DE_EO_JUSTIFIER",      "Expression orale", "Expliquer pourquoi une chose plaît ou déplaît et comparer de façon simple et directe", 5),
                ("SECONDE", "AR_2DE_INT_ECHANGER",      "Interaction", "Échanger idées et renseignements avec une aisance raisonnable en situation prévisible", 6),
                ("SECONDE", "AR_2DE_EE_RACONTER",       "Expression écrite", "Écrire en arabe standard pour présenter, décrire et raconter des activités passées", 7),
                ("SECONDE", "AR_2DE_EE_ORGANISER",      "Expression écrite", "Organiser son propos et structurer un récit à l'aide de connecteurs logiques et chronologiques", 8),
                ("SECONDE", "AR_2DE_MED_INFOS",         "Médiation", "Transmettre les informations pertinentes d'un texte informatif court et bien structuré", 9),
                ("SECONDE", "AR_2DE_PHONO_PROXIMITE",   "Phonologie", "Distinguer sons voisins et emphatiques dans des mots longs comme استطلاع ou اجتماع", 10),
                ("SECONDE", "AR_2DE_GRAM_LAM",          "Grammaire", "Nier le passé avec لم, sans systématiser la conjugaison de l'inaccompli apocopé", 11),
                ("SECONDE", "AR_2DE_GRAM_IMPERATIF",    "Grammaire", "Donner un ordre à l'impératif et exprimer l'interdiction avec لا", 12),
                ("SECONDE", "AR_2DE_GRAM_MODAUX",       "Grammaire", "Exprimer la volonté, l'obligation ou la possibilité avec un verbe modal suivi de أن", 13),
                ("SECONDE", "AR_2DE_GRAM_QUANTITE",     "Grammaire", "Employer كل et بعض, et غير devant un adjectif ou un pronom", 14),
                ("SECONDE", "AR_2DE_GRAM_ORDINAUX",     "Grammaire", "Employer les nombres ordinaux pour dire la date complète et l'heure", 15),
                ("SECONDE", "AR_2DE_GRAM_ACCORD_VSO",   "Grammaire", "Accorder le verbe placé avant son sujet et reconnaître un verbe à deux compléments (أعطها الكتاب)", 16),
                ("SECONDE", "AR_2DE_GRAM_ACQUIS",       "Grammaire", "Consolider accompli, inaccompli, futur et négations vus au collège, hors duel et féminin pluriel", 17),
                ("SECONDE", "AR_2DE_LEX_AXES",          "Lexique", "Mobiliser le lexique des réseaux sociaux, des générations, du numérique et du développement durable", 18),
                ("SECONDE", "AR_2DE_LEX_CREATION",      "Lexique", "Mobiliser le lexique du patrimoine et de la création : تراث, إبداع, فن الخط العربي", 19),
                ("SECONDE", "AR_2DE_CULT_PENINSULE",    "Culture", "Connaître les sept pays de la Péninsule arabique : sites archéologiques, perle et pétrole, souks et malls", 20),
                ("SECONDE", "AR_2DE_CULT_PASSE",        "Culture", "Analyser la place du passé dans les sociétés arabes d'aujourd'hui, entre valorisation et remise en question", 21),
                ("SECONDE", "AR_2DE_CULT_DEFIS",        "Culture", "Étudier les défis du monde arabe : eau, urbanisation, inégalités scolaires, intelligence artificielle", 22),
                ("SECONDE", "AR_2DE_CECRL",             "Méthode", "Atteindre le niveau A2+ visé en fin de seconde en LVB", 23),

                // --- Première : B1 visé (annexe 6, en vigueur en 2026) ---
                ("PREMIERE", "AR_1RE_CO_POINTS",        "Compréhension de l'oral", "Comprendre les points principaux d'un bulletin d'information ou d'un film en langue claire et standard", 1),
                ("PREMIERE", "AR_1RE_CO_PARLERS",       "Compréhension de l'oral", "Se familiariser, par des podcasts et des films, avec des accents et des parlers divers", 2),
                ("PREMIERE", "AR_1RE_CE_RECIT",         "Compréhension de l'écrit", "Suivre l'intrigue d'un récit simple et comprendre un texte factuel en s'aidant d'un dictionnaire", 3),
                ("PREMIERE", "AR_1RE_EO_EXPOSE",        "Expression orale", "Faire un exposé préparé, suivi sans difficulté, sur un sujet familier", 4),
                ("PREMIERE", "AR_1RE_EO_OPINION",       "Expression orale", "Raconter, exprimer ses sentiments et donner des raisons simples pour justifier un point de vue", 5),
                ("PREMIERE", "AR_1RE_INT_CONVERSATION", "Interaction", "Prendre part à une conversation prolongée sur un sujet familier en prenant quelques initiatives", 6),
                ("PREMIERE", "AR_1RE_INT_CLARIFIER",    "Interaction", "Faire répéter, demander confirmation et amener l'autre à clarifier son raisonnement", 7),
                ("PREMIERE", "AR_1RE_EE_CRITIQUE",      "Expression écrite", "Rédiger en arabe standard une critique simple d'un film ou d'un livre en justifiant son point de vue", 8),
                ("PREMIERE", "AR_1RE_EE_RENDRE_COMPTE", "Expression écrite", "Rendre compte par écrit de ses recherches et des activités menées sur un objet d'étude", 9),
                ("PREMIERE", "AR_1RE_MED_RESUMER",      "Médiation", "Résumer en français l'information et les arguments d'un dossier en arabe sur un sujet familier", 10),
                ("PREMIERE", "AR_1RE_PHONO_ACCENT",     "Phonologie", "Placer l'accent et l'intonation de l'arabe standard et distinguer س/ص, س/ث, خ, ض, ظ", 11),
                ("PREMIERE", "AR_1RE_GRAM_SUBJONCTIF",  "Grammaire", "Conjuguer l'inaccompli subjonctif (المضارع المنصوب) après أن, hors duel et féminin pluriel", 12),
                ("PREMIERE", "AR_1RE_GRAM_APOCOPE",     "Grammaire", "Conjuguer l'inaccompli apocopé (المضارع المجزوم) et nier le passé avec لم", 13),
                ("PREMIERE", "AR_1RE_GRAM_MASDAR",      "Grammaire", "Employer le مصدر (nom verbal) à la place de أن + verbe : أطلب منكم السكوت", 14),
                ("PREMIERE", "AR_1RE_GRAM_ELATIF",      "Grammaire", "Comparer avec l'élatif (أشهر من) et former le superlatif (من أقدم المدن)", 15),
                ("PREMIERE", "AR_1RE_GRAM_RELATIVES",   "Grammaire", "Construire une relative avec الذي, التي, الذين, et sans pronom après un antécédent indéfini", 16),
                ("PREMIERE", "AR_1RE_GRAM_SUBORDONNEES","Grammaire", "Construire des subordonnées de temps (عندما, قبل أن, بعد أن), de but (لكي) et de condition avec إذا", 17),
                ("PREMIERE", "AR_1RE_GRAM_RELIEF",      "Grammaire", "Rectifier avec لكن ou بل, mettre en relief avec أمّا ... فـ, et désigner avec ذلك, تلك, هؤلاء", 18),
                ("PREMIERE", "AR_1RE_GRAM_DECLINAISON", "Grammaire", "Reconnaître à la lecture l'alif du tanwīn (كتابًا) et l'alternance ـون / ـين du pluriel masculin", 19),
                ("PREMIERE", "AR_1RE_LEX_AXES",         "Lexique", "Mobiliser le lexique des identités, des migrations, de l'art, des sciences et de l'environnement", 20),
                ("PREMIERE", "AR_1RE_CULT_MAGHREB",     "Culture", "Étudier trois pays du Maghreb à travers un palais ou un musée et un personnage emblématique", 21),
                ("PREMIERE", "AR_1RE_CULT_ART_POUVOIR", "Culture", "Analyser les liens entre art et pouvoir : poète, satire, théâtre, cinéma, pouvoir de la voix", 22),
                ("PREMIERE", "AR_1RE_CULT_IDENTITES",   "Culture", "Étudier identités, échanges et diversité : diasporas arabes, minorités, villes aux identités multiples", 23),
                ("PREMIERE", "AR_1RE_CECRL",            "Méthode", "Atteindre le niveau B1 visé en fin de première en LVB", 24),

                // --- Terminale : B1 attendu (annexe 6) ---
                ("TERMINALE", "AR_TLE_CO_REPORTAGE",    "Compréhension de l'oral", "Comprendre les points principaux d'un reportage ou d'un film en langue claire et standard", 1),
                ("TERMINALE", "AR_TLE_CO_POSITION",     "Compréhension de l'oral", "Distinguer dans un document ce qui relève de l'information et ce qui relève de la prise de position", 2),
                ("TERMINALE", "AR_TLE_CE_CORPUS",       "Compréhension de l'écrit", "Trier et hiérarchiser les informations d'un corpus de documents pour en rendre compte", 3),
                ("TERMINALE", "AR_TLE_EO_EXPOSE",       "Expression orale", "Présenter un exposé préparé et clair sur une problématique liée à un axe étudié", 4),
                ("TERMINALE", "AR_TLE_EO_ARGUMENTER",   "Expression orale", "Exprimer une préférence ou une opinion et argumenter son choix de façon construite", 5),
                ("TERMINALE", "AR_TLE_INT_CONVERSATION","Interaction", "Soutenir une conversation prolongée en langue standard sur un sujet familier", 6),
                ("TERMINALE", "AR_TLE_INT_DEBAT",       "Interaction", "Participer à un débat ou à un jeu de rôle en faisant expliciter les différents points de vue", 7),
                ("TERMINALE", "AR_TLE_EE_RECIT",        "Expression écrite", "Raconter, décrire un événement et exprimer ses sentiments dans un texte en arabe standard", 8),
                ("TERMINALE", "AR_TLE_EE_STRUCTURER",   "Expression écrite", "Hiérarchiser son propos avec des connecteurs : أوّلًا, ثانيًا, أخيرًا, لذلك, بسبب", 9),
                ("TERMINALE", "AR_TLE_MED_IMPLICITE",   "Médiation", "Expliquer pour autrui, en termes courants, une référence culturelle implicite", 10),
                ("TERMINALE", "AR_TLE_MED_SYNTHESE",    "Médiation", "Rassembler des informations de sources diverses et les résumer pour quelqu'un d'autre", 11),
                ("TERMINALE", "AR_TLE_PHONO_PROSODIE",  "Phonologie", "Maîtriser l'accentuation et l'intonation de l'arabe standard dans une prise de parole suivie", 12),
                ("TERMINALE", "AR_TLE_GRAM_FORMES_DERIVEES", "Grammaire", "Reconnaître la forme de base et les formes dérivées du verbe, première sensibilisation", 13),
                ("TERMINALE", "AR_TLE_GRAM_MASDARS",    "Grammaire", "Reconnaître et employer masdars et participes des formes dérivées", 14),
                ("TERMINALE", "AR_TLE_GRAM_HAL",        "Grammaire", "Exprimer la manière avec le complément d'état (الحال)", 15),
                ("TERMINALE", "AR_TLE_GRAM_BUT",        "Grammaire", "Exprimer le but avec لـ, لكي ou من أجل suivi d'un مصدر", 16),
                ("TERMINALE", "AR_TLE_GRAM_SUBORDONNEES","Grammaire", "Maîtriser relatives, subordonnées de temps et conditionnelle avec إذا dans un propos suivi", 17),
                ("TERMINALE", "AR_TLE_GRAM_MODES",      "Grammaire", "Maîtriser inaccompli subjonctif et apocopé, hors duel et féminin pluriel", 18),
                ("TERMINALE", "AR_TLE_LEX_REGISTRE",    "Lexique", "Adapter la variété et le registre de l'arabe à la situation de communication", 19),
                ("TERMINALE", "AR_TLE_LEX_AXES",        "Lexique", "Mobiliser le lexique de l'espace public et privé, du patrimoine, des médias et de la citoyenneté", 20),
                ("TERMINALE", "AR_TLE_CULT_EGYPTE_LIBAN","Culture", "Étudier l'Égypte et le Liban à travers leur création littéraire et artistique : Le Caire, l'immeuble, la scène contemporaine", 21),
                ("TERMINALE", "AR_TLE_CULT_MEMOIRE",    "Culture", "Analyser territoire et mémoire : patrimoine détruit et reconstruit, Al-Andalus, Bayt al-Ḥikma", 22),
                ("TERMINALE", "AR_TLE_CULT_COMMUNICATION","Culture", "Interroger communication, fictions et mondes virtuels : séries, publicité, fausses informations", 23),
                ("TERMINALE", "AR_TLE_CECRL",           "Méthode", "Atteindre le niveau B1 visé en fin de terminale en LVB", 24),
            };
    }
}
