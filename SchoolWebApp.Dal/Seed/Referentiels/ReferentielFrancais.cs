namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel de français, du CP à la première.
    ///
    /// POURQUOI ÇA S'ARRÊTE EN PREMIÈRE
    /// --------------------------------
    /// Les épreuves anticipées de français se passent en fin de première. La
    /// terminale générale et technologique n'a plus de cours de français : le
    /// référentiel s'arrête donc là où la matière s'arrête.
    ///
    /// UN LIBELLÉ EST UN GESTE, PAS UN CHAPITRE
    /// ---------------------------------------
    /// « Accorder le participe passé employé avec avoir » se vérifie sur une
    /// copie ; « le participe passé » ne se vérifie pas. C'est ce qui permet à
    /// l'observateur de trancher réussi / échoué après une séance, et à la
    /// courbe de révision de savoir sur quoi revenir.
    /// </summary>
    public static class ReferentielFrancais
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- CP : entrer dans le code ---
                // Tout le reste du français en dépend. Un élève qui déchiffre
                // mal ne comprend pas ce qu'il lit, et son orthographe reste
                // bloquée des années : c'est la racine du graphe.
                ("CP", "FR_CP_LECT_GRAPHEMES",   "Lecture", "Connaître les correspondances entre lettres et sons", 1),
                ("CP", "FR_CP_LECT_SYLLABES",    "Lecture", "Déchiffrer des syllabes et des mots réguliers", 2),
                ("CP", "FR_CP_LECT_MOTS_OUTILS", "Lecture", "Reconnaître sans hésiter les mots outils fréquents", 3),
                ("CP", "FR_CP_LECT_VOIX_HAUTE",  "Lecture", "Lire à voix haute une phrase courte sans se reprendre", 4),
                ("CP", "FR_CP_LECT_COMPRENDRE",  "Lecture", "Répondre à une question sur un texte court lu seul", 5),
                ("CP", "FR_CP_ECR_CURSIVE",      "Écriture", "Écrire en cursive toutes les lettres, minuscules et majuscules", 6),
                ("CP", "FR_CP_ECR_COPIE",        "Écriture", "Copier une phrase sans erreur", 7),
                ("CP", "FR_CP_ECR_DICTEE",       "Écriture", "Écrire sous la dictée des mots simples déjà étudiés", 8),
                ("CP", "FR_CP_ECR_PHRASE",       "Écriture", "Produire une phrase pour raconter ou décrire", 9),
                ("CP", "FR_CP_LANG_PHRASE",      "Étude de la langue", "Repérer une phrase à sa majuscule et son point", 10),
                ("CP", "FR_CP_LANG_PLURIEL",     "Étude de la langue", "Marquer le pluriel d'un nom par un -s", 11),
                ("CP", "FR_CP_ORAL_RACONTER",    "Oral", "Raconter un événement de façon compréhensible", 12),

                ("CP", "FR_CP_ORAL_ECOUTER", "Oral", "Écouter une histoire et montrer qu’on l’a comprise", 13),
                ("CP", "FR_CP_LECT_DEVENIR", "Lecture", "Choisir un livre et dire ce qu’on y a aimé", 14),
                ("CP", "FR_CP_ECR_ENCODER", "Écriture", "Encoder un mot en s’appuyant sur les sons qu’on entend", 15),
                ("CP", "FR_CP_LANG_TYPES", "Étude de la langue", "Reconnaître une phrase qui interroge et une phrase qui ordonne", 16),
                ("CP", "FR_CP_LANG_VOC_MOTS", "Étude de la langue", "Apprendre des mots nouveaux et les réemployer", 17),
                ("CP", "FR_CP_LANG_VOC_ORTHO", "Étude de la langue", "Mémoriser l’orthographe des mots courants", 18),

                // --- CE1 : automatiser la lecture ---
                ("CE1", "FR_CE1_LECT_COMPLEXES",  "Lecture", "Lire des mots contenant des graphies complexes (ou, on, an, ill)", 1),
                ("CE1", "FR_CE1_LECT_FLUENCE",    "Lecture", "Lire à voix haute un texte court d'un seul tenant", 2),
                ("CE1", "FR_CE1_LECT_PERSONNAGES","Lecture", "Identifier les personnages, les lieux et l'ordre des événements", 3),
                ("CE1", "FR_CE1_LECT_IMPLICITE",  "Lecture", "Déduire une information qui n'est pas écrite dans le texte", 4),
                ("CE1", "FR_CE1_ECR_COPIE",       "Écriture", "Copier un texte court sans erreur et sans le relire mot à mot", 5),
                ("CE1", "FR_CE1_ECR_TEXTE",       "Écriture", "Rédiger un texte de trois à cinq phrases qui s'enchaînent", 6),
                ("CE1", "FR_CE1_ECR_RELIRE",      "Écriture", "Relire son texte pour corriger les majuscules et les points", 7),
                ("CE1", "FR_CE1_LANG_VERBE_SUJET","Étude de la langue", "Identifier le verbe et son sujet dans une phrase simple", 8),
                ("CE1", "FR_CE1_LANG_PRESENT",    "Étude de la langue", "Conjuguer être, avoir et les verbes en -er au présent", 9),
                ("CE1", "FR_CE1_LANG_GN",         "Étude de la langue", "Accorder le déterminant, le nom et l'adjectif en genre et en nombre", 10),
                ("CE1", "FR_CE1_LANG_A_ET",       "Étude de la langue", "Distinguer a et à, et et est", 11),
                ("CE1", "FR_CE1_LANG_ALPHABET",   "Étude de la langue", "Chercher un mot dans le dictionnaire", 12),
                ("CE1", "FR_CE1_ORAL_ECHANGE",    "Oral", "Participer à un échange en écoutant et en répondant au propos", 13),

                ("CE1", "FR_CE1_ORAL_ECOUTER", "Oral", "Comprendre un message oral ou une histoire entendue", 14),
                ("CE1", "FR_CE1_LECT_DEVENIR", "Lecture", "Choisir un livre et dire pourquoi on l’a aimé", 15),
                ("CE1", "FR_CE1_LANG_TYPES", "Étude de la langue", "Distinguer les phrases déclarative, interrogative et impérative", 16),
                ("CE1", "FR_CE1_LANG_NEGATION", "Étude de la langue", "Écrire une phrase à la forme négative", 17),
                ("CE1", "FR_CE1_LANG_VOC_RELATIONS", "Étude de la langue", "Employer des synonymes et des contraires", 18),
                ("CE1", "FR_CE1_LANG_VOC_ORTHO", "Étude de la langue", "Mémoriser l’orthographe des mots fréquents", 19),

                // --- CE2 : lire pour comprendre, écrire pour être lu ---
                ("CE2", "FR_CE2_LECT_FLUENCE",     "Lecture", "Lire à voix haute en respectant la ponctuation et le sens", 1),
                ("CE2", "FR_CE2_LECT_DOCUMENT",    "Lecture", "Prélever une information dans un texte documentaire", 2),
                ("CE2", "FR_CE2_LECT_JUSTIFIER",   "Lecture", "Justifier sa réponse en citant le passage du texte", 3),
                ("CE2", "FR_CE2_ECR_RECIT",        "Écriture", "Rédiger un récit d'une dizaine de lignes qui a un début et une fin", 4),
                ("CE2", "FR_CE2_ECR_TEMPS",        "Écriture", "Tenir le même temps du début à la fin d'un récit", 5),
                ("CE2", "FR_CE2_LANG_CLASSES",     "Étude de la langue", "Distinguer nom, verbe, déterminant, adjectif et pronom", 6),
                ("CE2", "FR_CE2_LANG_COMPLEMENTS", "Étude de la langue", "Repérer les compléments qui répondent à où, quand, comment", 7),
                ("CE2", "FR_CE2_LANG_TROIS_TEMPS", "Étude de la langue", "Conjuguer les verbes fréquents au présent, à l'imparfait et au futur", 8),
                ("CE2", "FR_CE2_LANG_ACCORD_SV",   "Étude de la langue", "Accorder le verbe avec son sujet, même éloigné", 9),
                ("CE2", "FR_CE2_LANG_SON_ONT",     "Étude de la langue", "Distinguer son et sont, on et ont", 10),
                ("CE2", "FR_CE2_LANG_FEMININ",     "Étude de la langue", "Former le féminin et le pluriel des noms et des adjectifs", 11),
                ("CE2", "FR_CE2_ORAL_PRESENTER",   "Oral", "Présenter un travail devant la classe sans lire ses notes mot à mot", 12),

                ("CE2", "FR_CE2_ORAL_ECOUTER", "Oral", "Comprendre un message oral et en restituer l’essentiel", 13),
                ("CE2", "FR_CE2_ECR_COPIE_LONGUE", "Écriture", "Copier un texte long en s’aidant de stratégies", 14),
                ("CE2", "FR_CE2_LANG_FORMES", "Étude de la langue", "Transformer une phrase : négative, exclamative, interrogative", 15),
                ("CE2", "FR_CE2_LANG_VOC_RELATIONS", "Étude de la langue", "Utiliser synonymes, contraires et familles de mots", 16),
                ("CE2", "FR_CE2_LANG_VOC_ORTHO", "Étude de la langue", "Mémoriser l’orthographe des mots fréquents et irréguliers", 17),

                // --- CM1 : la phrase se complexifie ---
                ("CM1", "FR_CM1_LECT_LONG",        "Lecture", "Rendre compte d'un texte long lu en plusieurs fois", 1),
                ("CM1", "FR_CM1_LECT_REPRISES",    "Lecture", "Identifier à qui renvoient les pronoms et les reprises", 2),
                ("CM1", "FR_CM1_LECT_POESIE",      "Lecture", "Repérer une comparaison ou une image dans un texte", 3),
                ("CM1", "FR_CM1_ECR_PARAGRAPHES",  "Écriture", "Organiser un texte d'une quinzaine de lignes en paragraphes", 4),
                ("CM1", "FR_CM1_ECR_CONNECTEURS",  "Écriture", "Employer des connecteurs de temps et de logique", 5),
                ("CM1", "FR_CM1_ECR_REVISER",      "Écriture", "Corriger son texte : orthographe, ponctuation, répétitions", 6),
                ("CM1", "FR_CM1_LANG_COD_COI",     "Étude de la langue", "Distinguer le complément d'objet direct et indirect", 7),
                ("CM1", "FR_CM1_LANG_TEMPS_SIMPLES","Étude de la langue", "Conjuguer aux quatre temps simples de l'indicatif", 8),
                ("CM1", "FR_CM1_LANG_PASSE_COMPOSE","Étude de la langue", "Former le passé composé et accorder le participe passé avec être", 9),
                ("CM1", "FR_CM1_LANG_GN_ELOIGNE",  "Étude de la langue", "Accorder dans le groupe nominal quand l'adjectif est éloigné du nom", 10),
                ("CM1", "FR_CM1_LANG_CE_SE",       "Étude de la langue", "Distinguer ce et se, ces et ses, la, là et l'a", 11),
                ("CM1", "FR_CM1_LANG_FAMILLES",    "Étude de la langue", "Utiliser les familles de mots, les préfixes et les suffixes", 12),
                ("CM1", "FR_CM1_ORAL_ARGUMENTER",  "Oral", "Donner son avis et l'appuyer sur une raison", 13),

                ("CM1", "FR_CM1_LECT_FLUENCE", "Lecture", "Lire sans effort un texte d’une page, environ 110 mots par minute", 14),
                ("CM1", "FR_CM1_LECT_GENRES", "Lecture", "Distinguer un poème, une scène de théâtre et un récit", 15),
                ("CM1", "FR_CM1_LECT_SOURCE", "Lecture", "Donner la nature et la source d’un document", 16),
                ("CM1", "FR_CM1_ORAL_ECOUTER", "Oral", "Comprendre un reportage ou une interview", 17),
                ("CM1", "FR_CM1_ECR_REFLEXIF", "Écriture", "Écrire pour apprendre : reformuler une leçon avec ses mots", 18),
                ("CM1", "FR_CM1_ECR_COPIE", "Écriture", "Copier un texte long de manière fluide et sans erreur", 19),
                ("CM1", "FR_CM1_LANG_TYPES", "Étude de la langue", "Identifier les trois types de phrases et les transformer", 20),
                ("CM1", "FR_CM1_LANG_FORMES", "Étude de la langue", "Identifier et employer les formes négative et exclamative", 21),
                ("CM1", "FR_CM1_LANG_VOC_SYNONYMES", "Étude de la langue", "Employer synonymes et antonymes à bon escient", 22),
                ("CM1", "FR_CM1_LANG_VOC_ORTHO", "Étude de la langue", "Mémoriser l’orthographe des mots fréquents", 23),

                // --- CM2 : préparer l'entrée au collège ---
                ("CM2", "FR_CM2_LECT_OEUVRE",      "Lecture", "Lire une œuvre intégrale et en restituer l'essentiel", 1),
                ("CM2", "FR_CM2_LECT_NARRATEUR",   "Lecture", "Distinguer l'auteur du narrateur et repérer qui raconte", 2),
                ("CM2", "FR_CM2_ECR_CONSIGNE",     "Écriture", "Rédiger un texte d'une vingtaine de lignes en respectant une consigne", 3),
                ("CM2", "FR_CM2_ECR_EXPLICATIF",   "Écriture", "Rédiger un texte qui explique ou une lettre", 4),
                ("CM2", "FR_CM2_LANG_COMPL_PHRASE","Étude de la langue", "Distinguer les compléments du verbe des compléments de phrase", 5),
                ("CM2", "FR_CM2_LANG_ATTRIBUT",    "Étude de la langue", "Identifier l'attribut du sujet et l'accorder", 6),
                ("CM2", "FR_CM2_LANG_TEMPS_COMPOSES","Étude de la langue", "Conjuguer aux temps composés et au conditionnel présent", 7),
                ("CM2", "FR_CM2_LANG_PP_AVOIR",    "Étude de la langue", "Accorder le participe passé employé avec avoir", 8),
                ("CM2", "FR_CM2_LANG_HOMOPHONES",  "Étude de la langue", "Distinguer ou et où, leur et leurs, quel, quelle et qu'elle", 9),
                ("CM2", "FR_CM2_LANG_SENS",        "Étude de la langue", "Distinguer sens propre et sens figuré, et les niveaux de langue", 10),
                ("CM2", "FR_CM2_ORAL_EXPRESSIF",   "Oral", "Lire à voix haute un texte préparé de façon expressive", 11),
                ("CM2", "FR_CM2_ORAL_DEBAT",       "Oral", "Participer à un débat en répondant à l'argument de l'autre", 12),

                ("CM2", "FR_CM2_LECT_FLUENCE", "Lecture", "Lire un texte long sans effort et sans se reprendre", 13),
                ("CM2", "FR_CM2_ORAL_ECOUTER", "Oral", "Comprendre un document oral ou médiatique et le restituer", 14),
                ("CM2", "FR_CM2_ECR_REFLEXIF", "Écriture", "Écrire pour apprendre et mémoriser une leçon", 15),
                ("CM2", "FR_CM2_ECR_VARIES", "Écriture", "Produire des écrits variés : raconter, expliquer, donner son avis", 16),
                ("CM2", "FR_CM2_LANG_TYPES", "Étude de la langue", "Identifier les types et les formes de phrases et les transformer", 17),
                ("CM2", "FR_CM2_LANG_SIMPLE_COMPLEXE", "Étude de la langue", "Différencier phrase simple et phrase complexe", 18),
                ("CM2", "FR_CM2_LANG_VOC_SYNONYMES", "Étude de la langue", "Employer synonymes, antonymes et mots polysémiques", 19),
                ("CM2", "FR_CM2_LANG_VOC_ORTHO", "Étude de la langue", "Mémoriser l’orthographe des mots fréquents", 20),

                // --- 6e : le récit, et la grammaire de phrase ---
                ("SIXIEME", "FR_6E_LECT_OEUVRE",      "Lecture", "Lire une œuvre intégrale et en rendre compte à l'écrit", 1),
                ("SIXIEME", "FR_6E_LECT_SCHEMA",      "Lecture", "Identifier le schéma narratif d'un récit", 2),
                ("SIXIEME", "FR_6E_LECT_MYTHE",       "Lecture", "Comprendre un texte fondateur de l'Antiquité et ses références", 3),
                ("SIXIEME", "FR_6E_LECT_NARRATEUR",   "Lecture", "Identifier le narrateur et le point de vue adopté", 4),
                ("SIXIEME", "FR_6E_LECT_IMAGE",       "Lecture", "Décrire une image fixe et en dégager le sens", 5),
                ("SIXIEME", "FR_6E_LECT_DISCOURS",    "Lecture", "Repérer les paroles rapportées dans un récit", 6),
                ("SIXIEME", "FR_6E_ECR_RECIT",        "Écriture", "Rédiger un récit complet respectant le schéma narratif", 7),
                ("SIXIEME", "FR_6E_ECR_DIALOGUE",     "Écriture", "Insérer un dialogue dans un récit avec la bonne ponctuation", 8),
                ("SIXIEME", "FR_6E_ECR_DESCRIPTION",  "Écriture", "Rédiger une description organisée et imagée", 9),
                ("SIXIEME", "FR_6E_ECR_REECRIRE",     "Écriture", "Améliorer un texte déjà rédigé à partir d'une remarque", 10),
                ("SIXIEME", "FR_6E_LANG_CLASSES",     "Étude de la langue", "Identifier les huit classes grammaticales", 11),
                ("SIXIEME", "FR_6E_LANG_FONCTIONS",   "Étude de la langue", "Identifier sujet, COD, COI, attribut et compléments circonstanciels", 12),
                ("SIXIEME", "FR_6E_LANG_INDICATIF",   "Étude de la langue", "Conjuguer aux huit temps de l'indicatif", 13),
                ("SIXIEME", "FR_6E_LANG_VALEURS",     "Étude de la langue", "Employer l'imparfait et le passé simple selon leur valeur", 14),
                ("SIXIEME", "FR_6E_LANG_PP",          "Étude de la langue", "Accorder le participe passé avec être et avec avoir", 15),
                ("SIXIEME", "FR_6E_LANG_HOMOPHONES",  "Étude de la langue", "Écrire sans erreur les homophones grammaticaux courants", 16),
                ("SIXIEME", "FR_6E_LANG_FORMATION",   "Étude de la langue", "Analyser un mot en radical, préfixe et suffixe", 17),
                ("SIXIEME", "FR_6E_LANG_PHRASE",      "Étude de la langue", "Distinguer phrase simple et phrase complexe", 18),
                ("SIXIEME", "FR_6E_ORAL_VOIX_HAUTE",  "Oral", "Lire à voix haute un texte littéraire de façon expressive", 19),
                ("SIXIEME", "FR_6E_ORAL_RACONTER",    "Oral", "Raconter une histoire à l'oral sans support écrit", 20),

                ("SIXIEME", "FR_6E_LECT_FLUENCE", "Lecture", "Lire un texte long avec fluidité", 21),
                ("SIXIEME", "FR_6E_ORAL_ECOUTER", "Oral", "Écouter un document et en restituer l’essentiel", 22),
                ("SIXIEME", "FR_6E_ECR_REFLEXIF", "Écriture", "Écrire pour réfléchir, apprendre et mémoriser", 23),
                ("SIXIEME", "FR_6E_ECR_MAIN", "Écriture", "Écrire à la main de manière fluide et efficace", 24),
                ("SIXIEME", "FR_6E_LANG_VOC_RELATIONS", "Étude de la langue", "Établir des relations entre les mots : synonymie, antonymie, polysémie", 25),
                ("SIXIEME", "FR_6E_LANG_VOC_ORTHO", "Étude de la langue", "Mémoriser l’orthographe des mots étudiés", 26),

                // --- 5e : la phrase complexe et le théâtre ---
                ("CINQUIEME", "FR_5E_LECT_CHEVALERIE", "Lecture", "Lire un récit chevaleresque et en situer les valeurs", 1),
                ("CINQUIEME", "FR_5E_LECT_THEATRE",    "Lecture", "Lire une pièce de théâtre : réplique, didascalie, acte, scène", 2),
                ("CINQUIEME", "FR_5E_LECT_COMIQUE",    "Lecture", "Identifier les procédés du comique dans une scène", 3),
                ("CINQUIEME", "FR_5E_LECT_VOYAGE",     "Lecture", "Comprendre un récit de voyage et le regard porté sur l'autre", 4),
                ("CINQUIEME", "FR_5E_LECT_FIGURES",    "Lecture", "Repérer comparaison, métaphore et personnification", 5),
                ("CINQUIEME", "FR_5E_ECR_SUITE",       "Écriture", "Écrire la suite d'un texte en respectant son univers et son style", 6),
                ("CINQUIEME", "FR_5E_ECR_THEATRE",     "Écriture", "Rédiger une scène de théâtre avec didascalies", 7),
                ("CINQUIEME", "FR_5E_ECR_ARGUMENTER",  "Écriture", "Rédiger un paragraphe qui défend un avis avec un exemple", 8),
                ("CINQUIEME", "FR_5E_ECR_BROUILLON",   "Écriture", "Utiliser un brouillon : plan, ajouts, suppressions", 9),
                ("CINQUIEME", "FR_5E_LANG_PROPOSITIONS","Étude de la langue", "Distinguer juxtaposition, coordination et subordination", 10),
                ("CINQUIEME", "FR_5E_LANG_RELATIVE",   "Étude de la langue", "Identifier une proposition subordonnée relative et son antécédent", 11),
                ("CINQUIEME", "FR_5E_LANG_CONJONCTIVE","Étude de la langue", "Identifier une subordonnée conjonctive complément du verbe", 12),
                ("CINQUIEME", "FR_5E_LANG_SUBJONCTIF", "Étude de la langue", "Conjuguer et employer le subjonctif présent", 13),
                ("CINQUIEME", "FR_5E_LANG_CONDITIONNEL","Étude de la langue", "Distinguer le conditionnel du futur et employer chacun à sa place", 14),
                ("CINQUIEME", "FR_5E_LANG_VOIX",       "Étude de la langue", "Transformer une phrase de la voix active à la voix passive", 15),
                ("CINQUIEME", "FR_5E_LANG_ACCORDS",    "Étude de la langue", "Réussir les accords dans une phrase longue", 16),
                ("CINQUIEME", "FR_5E_LANG_LEXIQUE",    "Étude de la langue", "Employer le lexique du jugement et du sentiment", 17),
                ("CINQUIEME", "FR_5E_ORAL_THEATRE",    "Oral", "Dire un texte de théâtre en le jouant", 18),
                ("CINQUIEME", "FR_5E_ORAL_EXPOSE",     "Oral", "Présenter un exposé structuré de cinq minutes", 19),

                ("CINQUIEME", "FR_5E_ORAL_ECOUTER", "Oral", "Écouter, comprendre et interpréter un document oral", 20),
                ("CINQUIEME", "FR_5E_ECR_REFLEXIF", "Écriture", "Écrire pour réfléchir, apprendre et mémoriser", 21),
                ("CINQUIEME", "FR_5E_ECR_EVALUER", "Écriture", "Évaluer son écrit et savoir le faire évoluer", 22),
                ("CINQUIEME", "FR_5E_LANG_VOC_RELATIONS", "Étude de la langue", "Identifier les relations entre les mots et enrichir son vocabulaire", 23),
                ("CINQUIEME", "FR_5E_LANG_ORTHOGRAPHE", "Étude de la langue", "Écrire avec justesse : orthographe lexicale et grammaticale", 24),

                // --- 4e : le regard critique ---
                ("QUATRIEME", "FR_4E_LECT_REALISME",    "Lecture", "Lire une nouvelle réaliste ou fantastique et en analyser la chute", 1),
                ("QUATRIEME", "FR_4E_LECT_FANTASTIQUE", "Lecture", "Identifier ce qui crée le doute dans un récit fantastique", 2),
                ("QUATRIEME", "FR_4E_LECT_POESIE",      "Lecture", "Analyser un poème : vers, strophes, rimes, rythme", 3),
                ("QUATRIEME", "FR_4E_LECT_PRESSE",      "Lecture", "Distinguer information, opinion et source dans un article", 4),
                ("QUATRIEME", "FR_4E_LECT_REGISTRE",    "Lecture", "Identifier le registre d'un texte et ce qui le produit", 5),
                ("QUATRIEME", "FR_4E_ECR_NOUVELLE",     "Écriture", "Rédiger une nouvelle avec une chute préparée", 6),
                ("QUATRIEME", "FR_4E_ECR_ARGUMENTATION","Écriture", "Rédiger un texte argumenté organisé en paragraphes", 7),
                ("QUATRIEME", "FR_4E_ECR_COMMENTER",    "Écriture", "Rédiger un paragraphe d'analyse citant le texte", 8),
                ("QUATRIEME", "FR_4E_ECR_PORTRAIT",     "Écriture", "Rédiger un portrait qui donne un point de vue sur le personnage", 9),
                ("QUATRIEME", "FR_4E_LANG_DISCOURS",    "Étude de la langue", "Passer du discours direct au discours indirect", 10),
                ("QUATRIEME", "FR_4E_LANG_CONCORDANCE", "Étude de la langue", "Respecter la concordance des temps dans un récit au passé", 11),
                ("QUATRIEME", "FR_4E_LANG_CIRCONSTANCIELLE","Étude de la langue", "Identifier les subordonnées circonstancielles et leur sens", 12),
                ("QUATRIEME", "FR_4E_LANG_INTERROGATIVE","Étude de la langue", "Construire les trois formes de phrase interrogative", 13),
                ("QUATRIEME", "FR_4E_LANG_NEGATION",    "Étude de la langue", "Employer la négation totale et partielle sans oublier le ne", 14),
                ("QUATRIEME", "FR_4E_LANG_PP_PRONOMINAL","Étude de la langue", "Accorder le participe passé des verbes pronominaux", 15),
                ("QUATRIEME", "FR_4E_LANG_CONNOTATION", "Étude de la langue", "Distinguer dénotation et connotation d'un mot", 16),
                ("QUATRIEME", "FR_4E_ORAL_DEBAT",       "Oral", "Soutenir un point de vue dans un débat et répondre aux objections", 17),
                ("QUATRIEME", "FR_4E_ORAL_RECITER",     "Oral", "Dire un poème de mémoire en respectant son rythme", 18),

                ("QUATRIEME", "FR_4E_ORAL_ECOUTER", "Oral", "Écouter, comprendre et interpréter un document oral", 19),
                ("QUATRIEME", "FR_4E_ECR_EVALUER", "Écriture", "Évaluer son écrit et savoir le faire évoluer", 20),
                ("QUATRIEME", "FR_4E_LANG_VOC_RELATIONS", "Étude de la langue", "Identifier les relations entre les mots et enrichir son vocabulaire", 21),

                // --- 3e : préparer le brevet et le lycée ---
                ("TROISIEME", "FR_3E_LECT_AUTOBIO",     "Lecture", "Lire un récit autobiographique et distinguer auteur, narrateur, personnage", 1),
                ("TROISIEME", "FR_3E_LECT_GUERRE",      "Lecture", "Analyser un texte qui dit l'expérience de la guerre", 2),
                ("TROISIEME", "FR_3E_LECT_ENGAGEMENT",  "Lecture", "Repérer la thèse et les arguments d'un texte engagé", 3),
                ("TROISIEME", "FR_3E_LECT_ROMAN",       "Lecture", "Analyser la construction d'un personnage de roman", 4),
                ("TROISIEME", "FR_3E_LECT_IMPLICITE",   "Lecture", "Dégager l'implicite et l'ironie d'un texte", 5),
                ("TROISIEME", "FR_3E_ECR_SUJET_REFLEXION","Écriture", "Traiter un sujet de réflexion : thèse, arguments, exemples", 6),
                ("TROISIEME", "FR_3E_ECR_SUJET_IMAGINATION","Écriture", "Traiter un sujet d'imagination en respectant les contraintes", 7),
                ("TROISIEME", "FR_3E_ECR_DICTEE",       "Écriture", "Réussir une dictée d'une quinzaine de lignes", 8),
                ("TROISIEME", "FR_3E_ECR_REFORMULER",   "Écriture", "Reformuler une idée sans la déformer ni recopier", 9),
                ("TROISIEME", "FR_3E_LANG_ANALYSE",     "Étude de la langue", "Analyser complètement une phrase complexe", 10),
                ("TROISIEME", "FR_3E_LANG_MODES",       "Étude de la langue", "Employer chaque mode verbal selon sa valeur", 11),
                ("TROISIEME", "FR_3E_LANG_HYPOTHESE",   "Étude de la langue", "Construire les trois systèmes hypothétiques avec si", 12),
                ("TROISIEME", "FR_3E_LANG_MODALISATION","Étude de la langue", "Repérer les marques de jugement et de certitude dans un texte", 13),
                ("TROISIEME", "FR_3E_LANG_ORTHO",       "Étude de la langue", "Relire une copie en ciblant les accords et les homophones", 14),
                ("TROISIEME", "FR_3E_LANG_LEXIQUE",     "Étude de la langue", "Employer un lexique précis plutôt que des mots passe-partout", 15),
                ("TROISIEME", "FR_3E_ORAL_EXPOSE",      "Oral", "Présenter un projet à l'oral et répondre aux questions du jury", 16),
                ("TROISIEME", "FR_3E_ORAL_LECTURE",     "Oral", "Lire à voix haute un texte long sans perdre son auditoire", 17),

                ("TROISIEME", "FR_3E_ORAL_ECOUTER", "Oral", "Écouter, comprendre et interpréter un document oral", 18),
                ("TROISIEME", "FR_3E_ECR_EVALUER", "Écriture", "Évaluer son écrit et savoir le faire évoluer", 19),
                ("TROISIEME", "FR_3E_LANG_VOC_RELATIONS", "Étude de la langue", "Identifier les relations entre les mots et enrichir son vocabulaire", 20),

                // --- Seconde : les quatre objets d'étude ---
                ("SECONDE", "FR_2DE_LECT_ROMAN",       "Littérature", "Analyser un roman du XVIIIe au XXIe siècle et son personnage", 1),
                ("SECONDE", "FR_2DE_LECT_THEATRE",     "Littérature", "Analyser une tragédie ou une comédie du XVIIe siècle", 2),
                ("SECONDE", "FR_2DE_LECT_POESIE",      "Littérature", "Analyser un poème du XIXe au XXIe siècle", 3),
                ("SECONDE", "FR_2DE_LECT_ARGUMENTATION","Littérature", "Analyser un texte de la littérature d'idées du XVIe au XVIIIe", 4),
                ("SECONDE", "FR_2DE_LECT_MOUVEMENT",   "Littérature", "Situer un texte dans son mouvement littéraire", 5),
                ("SECONDE", "FR_2DE_LECT_PROCEDES",    "Littérature", "Nommer un procédé d'écriture et dire l'effet qu'il produit", 6),
                ("SECONDE", "FR_2DE_METH_LINEAIRE",    "Méthode", "Conduire une explication linéaire suivant le mouvement du texte", 7),
                ("SECONDE", "FR_2DE_METH_PROBLEMATIQUE","Méthode", "Formuler une problématique à partir d'un texte", 8),
                ("SECONDE", "FR_2DE_METH_PLAN",        "Méthode", "Construire un plan de commentaire en parties et sous-parties", 9),
                ("SECONDE", "FR_2DE_METH_CITATION",    "Méthode", "Intégrer une citation courte dans une phrase d'analyse", 10),
                ("SECONDE", "FR_2DE_ECR_COMMENTAIRE",  "Écriture", "Rédiger un commentaire littéraire complet", 11),
                ("SECONDE", "FR_2DE_ECR_DISSERTATION", "Écriture", "Rédiger une dissertation sur une œuvre : thèse, antithèse, dépassement", 12),
                ("SECONDE", "FR_2DE_ECR_INTRODUCTION", "Écriture", "Rédiger une introduction complète et une conclusion", 13),
                ("SECONDE", "FR_2DE_ECR_TRANSITION",   "Écriture", "Enchaîner les parties par des transitions explicites", 14),
                ("SECONDE", "FR_2DE_LANG_SYNTAXE",     "Étude de la langue", "Analyser la syntaxe d'une phrase pour en tirer un effet de sens", 15),
                ("SECONDE", "FR_2DE_LANG_INTERROGATION","Étude de la langue", "Analyser les formes et les valeurs de l'interrogation", 16),
                ("SECONDE", "FR_2DE_LANG_NEGATION",    "Étude de la langue", "Analyser les formes et les valeurs de la négation", 17),
                ("SECONDE", "FR_2DE_LANG_LEXIQUE",     "Étude de la langue", "Analyser un mot par son étymologie et son évolution", 18),
                ("SECONDE", "FR_2DE_ORAL_LECTURE",     "Oral", "Lire à voix haute un texte littéraire en rendant son sens", 19),

                ("SECONDE", "FR_2DE_LECT_POESIE_ANCIENNE", "Littérature", "Analyser un poème du Moyen Âge au XVIIIe siècle", 20),
                ("SECONDE", "FR_2DE_LECT_PRESSE", "Littérature", "Analyser un texte de la littérature d’idées et de la presse du XIXe au XXIe siècle", 21),

                // --- Première : l'année des épreuves anticipées ---
                ("PREMIERE", "FR_1RE_LECT_OEUVRES",     "Littérature", "Connaître les quatre œuvres au programme et leur parcours associé", 1),
                ("PREMIERE", "FR_1RE_LECT_ROMAN",       "Littérature", "Analyser l'œuvre romanesque au programme", 2),
                ("PREMIERE", "FR_1RE_LECT_THEATRE",     "Littérature", "Analyser l'œuvre théâtrale au programme", 3),
                ("PREMIERE", "FR_1RE_LECT_POESIE",      "Littérature", "Analyser le recueil poétique au programme", 4),
                ("PREMIERE", "FR_1RE_LECT_IDEES",       "Littérature", "Analyser l'œuvre de littérature d'idées au programme", 5),
                ("PREMIERE", "FR_1RE_LECT_PARCOURS",    "Littérature", "Relier une œuvre à son parcours et aux textes complémentaires", 6),
                ("PREMIERE", "FR_1RE_METH_LINEAIRE",    "Méthode", "Mener une explication linéaire en dégageant les mouvements du texte", 7),
                ("PREMIERE", "FR_1RE_METH_GRAMMAIRE",   "Méthode", "Traiter la question de grammaire de l'oral sur un extrait", 8),
                ("PREMIERE", "FR_1RE_METH_OEUVRE",      "Méthode", "Présenter et défendre son œuvre choisie devant l'examinateur", 9),
                ("PREMIERE", "FR_1RE_METH_ENTRETIEN",   "Méthode", "Soutenir un entretien de huit minutes sur son œuvre", 10),
                ("PREMIERE", "FR_1RE_ECR_COMMENTAIRE",  "Écriture", "Rédiger un commentaire composé en quatre heures", 11),
                ("PREMIERE", "FR_1RE_ECR_DISSERTATION", "Écriture", "Rédiger une dissertation sur œuvre et parcours", 12),
                ("PREMIERE", "FR_1RE_ECR_PROBLEMATISER","Écriture", "Problématiser un sujet de dissertation avant de rédiger", 13),
                ("PREMIERE", "FR_1RE_ECR_EXEMPLES",     "Écriture", "Nourrir une dissertation d'exemples précis tirés des œuvres", 14),
                ("PREMIERE", "FR_1RE_ECR_REDACTION",    "Écriture", "Rédiger dans une langue correcte et sans familiarités", 15),
                ("PREMIERE", "FR_1RE_LANG_SUBORDINATION","Étude de la langue", "Analyser la subordination et son rôle dans la construction du sens", 16),
                ("PREMIERE", "FR_1RE_LANG_INTERROGATION","Étude de la langue", "Analyser une phrase interrogative pour la question de grammaire", 17),
                ("PREMIERE", "FR_1RE_LANG_NEGATION",    "Étude de la langue", "Analyser une phrase négative pour la question de grammaire", 18),
                ("PREMIERE", "FR_1RE_ORAL_EXPRESSION",  "Oral", "S'exprimer douze minutes sans lire ses notes", 19),

                // --- Terminale professionnelle : le français continue ici ---
                //
                // Il s'arrête en première dans les voies générale et
                // technologique, pas au bac professionnel, où il reste une
                // épreuve. Ces lignes portent le rang 12 mais ne seront vues
                // que par la terminale professionnelle : VoiesScolaires exclut
                // le français des deux autres terminales, et la carte applique
                // cette exclusion avant d'afficher.
                ("TERMINALE_PRO", "FR_TPRO_LECT_OEUVRE", "Littérature", "Lire une œuvre de la littérature d’idées et en rendre compte", 1),
                ("TERMINALE_PRO", "FR_TPRO_LECT_GROUPEMENT", "Littérature", "Analyser un groupement de textes et de documents autour d’un thème", 2),
                ("TERMINALE_PRO", "FR_TPRO_LECT_FORMES", "Littérature", "Reconnaître les formes de la littérature d’idées : essai, apologue, utopie, dystopie", 3),
                ("TERMINALE_PRO", "FR_TPRO_LECT_METIER", "Littérature", "Lire une étude ou un essai sur le monde du travail et ses évolutions", 4),
                ("TERMINALE_PRO", "FR_TPRO_ECR_REFLEXIF", "Écriture", "Rédiger un écrit réflexif qui développe un raisonnement", 5),
                ("TERMINALE_PRO", "FR_TPRO_ECR_HIERARCHISER", "Écriture", "Trier et hiérarchiser ses connaissances à l’appui d’une thèse", 6),
                ("TERMINALE_PRO", "FR_TPRO_ECR_NUANCER", "Écriture", "Nuancer son point de vue et envisager les arguments contraires", 7),
                ("TERMINALE_PRO", "FR_TPRO_ECR_COMPTE_RENDU", "Écriture", "Rédiger un compte rendu à partir de paroles prononcées", 8),
                ("TERMINALE_PRO", "FR_TPRO_ECR_PROFESSIONNEL", "Écriture", "Adapter son écrit au support : courriel et message professionnel", 9),
                ("TERMINALE_PRO", "FR_TPRO_ORAL_DEBAT", "Oral", "Prendre part à un débat d’idées en formulant sa pensée", 10),
                ("TERMINALE_PRO", "FR_TPRO_ORAL_REAGIR", "Oral", "S’adapter à son interlocuteur, relancer et préciser sa pensée", 11),
                ("TERMINALE_PRO", "FR_TPRO_ORAL_PRESENTER", "Oral", "Présenter et justifier une démarche à l’oral", 12),
                ("TERMINALE_PRO", "FR_TPRO_METH_CONVAINCRE", "Méthode", "Distinguer convaincre, persuader et négocier", 13),
                ("TERMINALE_PRO", "FR_TPRO_METH_POINTS_VUE", "Méthode", "Confronter des points de vue pour construire un raisonnement personnel", 14),
            };

        /// <summary>
        /// Le graphe du français est presque entièrement VERTICAL : chaque
        /// notion reprend celle de l'année précédente. C'est ce qui rend la
        /// matière si punitive — une lacune de CE1 sur l'accord sujet-verbe se
        /// paie encore en troisième — et c'est précisément ce que le graphe
        /// sert à retrouver.
        /// </summary>
        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // Le déchiffrage conditionne tout le reste, dès le CP.
                ("FR_CP_LECT_SYLLABES",      "FR_CP_LECT_GRAPHEMES",    3),
                ("FR_CP_LECT_VOIX_HAUTE",    "FR_CP_LECT_SYLLABES",     3),
                ("FR_CP_LECT_COMPRENDRE",    "FR_CP_LECT_SYLLABES",     3),
                ("FR_CP_ECR_DICTEE",         "FR_CP_LECT_GRAPHEMES",    3),
                ("FR_CP_ECR_PHRASE",         "FR_CP_ECR_CURSIVE",       2),
                ("FR_CP_ECR_PHRASE",         "FR_CP_LANG_PHRASE",       2),

                // CP → CE1
                ("FR_CE1_LECT_COMPLEXES",    "FR_CP_LECT_GRAPHEMES",    3),
                ("FR_CE1_LECT_FLUENCE",      "FR_CP_LECT_VOIX_HAUTE",   3),
                ("FR_CE1_LECT_FLUENCE",      "FR_CE1_LECT_COMPLEXES",   3),
                ("FR_CE1_LECT_PERSONNAGES",  "FR_CP_LECT_COMPRENDRE",   3),
                ("FR_CE1_LECT_IMPLICITE",    "FR_CE1_LECT_PERSONNAGES", 2),
                ("FR_CE1_ECR_TEXTE",         "FR_CP_ECR_PHRASE",        3),
                ("FR_CE1_LANG_GN",           "FR_CP_LANG_PLURIEL",      3),
                ("FR_CE1_LANG_PRESENT",      "FR_CE1_LANG_VERBE_SUJET", 3),

                // CE1 → CE2
                ("FR_CE2_LECT_FLUENCE",      "FR_CE1_LECT_FLUENCE",     3),
                ("FR_CE2_LECT_JUSTIFIER",    "FR_CE1_LECT_IMPLICITE",   2),
                ("FR_CE2_ECR_RECIT",         "FR_CE1_ECR_TEXTE",        3),
                ("FR_CE2_ECR_TEMPS",         "FR_CE2_LANG_TROIS_TEMPS", 3),
                ("FR_CE2_LANG_CLASSES",      "FR_CE1_LANG_VERBE_SUJET", 3),
                ("FR_CE2_LANG_TROIS_TEMPS",  "FR_CE1_LANG_PRESENT",     3),
                ("FR_CE2_LANG_ACCORD_SV",    "FR_CE1_LANG_VERBE_SUJET", 3),
                ("FR_CE2_LANG_FEMININ",      "FR_CE1_LANG_GN",          3),
                ("FR_CE2_LANG_SON_ONT",      "FR_CE1_LANG_A_ET",        2),

                // CE2 → CM1
                ("FR_CM1_LECT_REPRISES",     "FR_CE2_LANG_CLASSES",     2),
                ("FR_CM1_ECR_PARAGRAPHES",   "FR_CE2_ECR_RECIT",        3),
                ("FR_CM1_ECR_REVISER",       "FR_CE2_LANG_ACCORD_SV",   2),
                ("FR_CM1_LANG_COD_COI",      "FR_CE2_LANG_COMPLEMENTS", 3),
                ("FR_CM1_LANG_COD_COI",      "FR_CE2_LANG_CLASSES",     3),
                ("FR_CM1_LANG_TEMPS_SIMPLES","FR_CE2_LANG_TROIS_TEMPS", 3),
                ("FR_CM1_LANG_PASSE_COMPOSE","FR_CM1_LANG_TEMPS_SIMPLES",3),
                ("FR_CM1_LANG_GN_ELOIGNE",   "FR_CE2_LANG_FEMININ",     3),
                ("FR_CM1_LANG_CE_SE",        "FR_CE2_LANG_SON_ONT",     2),

                // CM1 → CM2
                ("FR_CM2_LECT_NARRATEUR",    "FR_CM1_LECT_REPRISES",    2),
                ("FR_CM2_ECR_CONSIGNE",      "FR_CM1_ECR_PARAGRAPHES",  3),
                ("FR_CM2_LANG_COMPL_PHRASE", "FR_CM1_LANG_COD_COI",     3),
                ("FR_CM2_LANG_ATTRIBUT",     "FR_CM1_LANG_COD_COI",     2),
                ("FR_CM2_LANG_TEMPS_COMPOSES","FR_CM1_LANG_PASSE_COMPOSE",3),
                ("FR_CM2_LANG_PP_AVOIR",     "FR_CM1_LANG_PASSE_COMPOSE",3),
                ("FR_CM2_LANG_HOMOPHONES",   "FR_CM1_LANG_CE_SE",       2),

                // CM2 → 6e
                ("FR_6E_LECT_SCHEMA",        "FR_CM2_LECT_OEUVRE",      2),
                ("FR_6E_LECT_NARRATEUR",     "FR_CM2_LECT_NARRATEUR",   3),
                ("FR_6E_ECR_RECIT",          "FR_CM2_ECR_CONSIGNE",     3),
                ("FR_6E_ECR_RECIT",          "FR_6E_LECT_SCHEMA",       3),
                ("FR_6E_ECR_DIALOGUE",       "FR_6E_LECT_DISCOURS",     2),
                ("FR_6E_LANG_CLASSES",       "FR_CE2_LANG_CLASSES",     3),
                ("FR_6E_LANG_FONCTIONS",     "FR_CM2_LANG_COMPL_PHRASE",3),
                ("FR_6E_LANG_FONCTIONS",     "FR_6E_LANG_CLASSES",      3),
                ("FR_6E_LANG_INDICATIF",     "FR_CM2_LANG_TEMPS_COMPOSES",3),
                ("FR_6E_LANG_PP",            "FR_CM2_LANG_PP_AVOIR",    3),
                ("FR_6E_LANG_HOMOPHONES",    "FR_CM2_LANG_HOMOPHONES",  3),
                ("FR_6E_LANG_PHRASE",        "FR_6E_LANG_FONCTIONS",    3),

                // 6e → 5e : c'est ici que la phrase complexe apparaît, et
                // c'est le premier vrai décrochage de la scolarité.
                ("FR_5E_LANG_PROPOSITIONS",  "FR_6E_LANG_PHRASE",       3),
                ("FR_5E_LANG_RELATIVE",      "FR_5E_LANG_PROPOSITIONS", 3),
                ("FR_5E_LANG_CONJONCTIVE",   "FR_5E_LANG_PROPOSITIONS", 3),
                ("FR_5E_LANG_SUBJONCTIF",    "FR_6E_LANG_INDICATIF",    3),
                ("FR_5E_LANG_CONDITIONNEL",  "FR_6E_LANG_INDICATIF",    3),
                ("FR_5E_LANG_VOIX",          "FR_6E_LANG_FONCTIONS",    3),
                ("FR_5E_LANG_ACCORDS",       "FR_6E_LANG_PP",           3),
                ("FR_5E_ECR_SUITE",          "FR_6E_ECR_RECIT",         3),
                ("FR_5E_ECR_THEATRE",        "FR_5E_LECT_THEATRE",      3),
                ("FR_5E_LECT_FIGURES",       "FR_CM1_LECT_POESIE",      2),

                // 5e → 4e
                ("FR_4E_LANG_DISCOURS",      "FR_5E_LANG_CONJONCTIVE",  3),
                ("FR_4E_LANG_CONCORDANCE",   "FR_5E_LANG_SUBJONCTIF",   3),
                ("FR_4E_LANG_CIRCONSTANCIELLE","FR_5E_LANG_CONJONCTIVE",3),
                ("FR_4E_LANG_PP_PRONOMINAL", "FR_5E_LANG_ACCORDS",      3),
                ("FR_4E_ECR_ARGUMENTATION",  "FR_5E_ECR_ARGUMENTER",    3),
                ("FR_4E_ECR_COMMENTER",      "FR_4E_LECT_REGISTRE",     2),
                ("FR_4E_LECT_POESIE",        "FR_5E_LECT_FIGURES",      3),
                ("FR_4E_LECT_REGISTRE",      "FR_5E_LECT_FIGURES",      2),

                // 4e → 3e
                ("FR_3E_LANG_ANALYSE",       "FR_4E_LANG_CIRCONSTANCIELLE",3),
                ("FR_3E_LANG_MODES",         "FR_4E_LANG_CONCORDANCE",  3),
                ("FR_3E_LANG_HYPOTHESE",     "FR_5E_LANG_CONDITIONNEL", 3),
                ("FR_3E_LANG_ORTHO",         "FR_4E_LANG_PP_PRONOMINAL",3),
                ("FR_3E_ECR_SUJET_REFLEXION","FR_4E_ECR_ARGUMENTATION", 3),
                ("FR_3E_ECR_DICTEE",         "FR_3E_LANG_ORTHO",        3),
                ("FR_3E_LECT_ENGAGEMENT",    "FR_4E_ECR_ARGUMENTATION", 2),
                ("FR_3E_LECT_IMPLICITE",     "FR_4E_LECT_REGISTRE",     2),

                // 3e → seconde : le passage au commentaire, l'autre décrochage.
                ("FR_2DE_METH_LINEAIRE",     "FR_3E_LECT_IMPLICITE",    3),
                ("FR_2DE_METH_PROBLEMATIQUE","FR_3E_ECR_SUJET_REFLEXION",3),
                ("FR_2DE_LECT_PROCEDES",     "FR_4E_LECT_POESIE",       3),
                ("FR_2DE_METH_PLAN",         "FR_2DE_LECT_PROCEDES",    3),
                ("FR_2DE_METH_CITATION",     "FR_3E_ECR_REFORMULER",    2),
                ("FR_2DE_ECR_COMMENTAIRE",   "FR_2DE_METH_PLAN",        3),
                ("FR_2DE_ECR_COMMENTAIRE",   "FR_2DE_METH_CITATION",    3),
                ("FR_2DE_ECR_DISSERTATION",  "FR_2DE_METH_PROBLEMATIQUE",3),
                ("FR_2DE_ECR_INTRODUCTION",  "FR_2DE_METH_PROBLEMATIQUE",3),
                ("FR_2DE_LANG_SYNTAXE",      "FR_3E_LANG_ANALYSE",      3),

                // Seconde → première
                ("FR_1RE_METH_LINEAIRE",     "FR_2DE_METH_LINEAIRE",    3),
                ("FR_1RE_ECR_COMMENTAIRE",   "FR_2DE_ECR_COMMENTAIRE",  3),
                ("FR_1RE_ECR_DISSERTATION",  "FR_2DE_ECR_DISSERTATION", 3),
                ("FR_1RE_ECR_PROBLEMATISER", "FR_2DE_METH_PROBLEMATIQUE",3),
                ("FR_1RE_ECR_EXEMPLES",      "FR_1RE_LECT_PARCOURS",    3),
                ("FR_1RE_LANG_SUBORDINATION","FR_2DE_LANG_SYNTAXE",     3),
                ("FR_1RE_LANG_INTERROGATION","FR_2DE_LANG_INTERROGATION",3),
                ("FR_1RE_LANG_NEGATION",     "FR_2DE_LANG_NEGATION",    3),
                ("FR_1RE_METH_GRAMMAIRE",    "FR_1RE_LANG_SUBORDINATION",3),
                ("FR_1RE_ORAL_EXPRESSION",   "FR_2DE_ORAL_LECTURE",     2),
                ("FR_1RE_METH_ENTRETIEN",    "FR_1RE_METH_OEUVRE",      3),
            };
    }
}
