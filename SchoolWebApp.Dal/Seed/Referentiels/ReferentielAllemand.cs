namespace SchoolWebApp.Dal.Seed.Referentiels
{
    public static class ReferentielAllemand
    {
        /// <summary>
        /// Référentiel d'ALLEMAND enseigné en LANGUE VIVANTE B (LV2), de la
        /// cinquième à la terminale, voies générale et technologique.
        /// Construit sur le modèle exact de `ReferentielEspagnol`.
        ///
        /// ================== PROVENANCE (lue le 04/10/2026) ==================
        ///
        ///   NIVEAU   TEXTE APPLICABLE EN 2026-2027                       DEPUIS
        ///   --------------------------------------------------------------------
        ///   5e       Arrêté du 5-5-2025, MENE2504621A, BO n° 22 du       rentrée 2026
        ///            29-5-2025, ANNEXE 1 « Programme d'allemand pour
        ///            les classes de collège »
        ///   4e       Programme du cycle 4, arrêté du 9-11-2015 modifié,  rentrée 2020
        ///            BO n° 31 du 30-7-2020 (volet LV commun à toutes     (annexe 1 :
        ///            les langues). L'annexe 1 ne s'y applique qu'en      rentrée 2027)
        ///            2027 (art. 4 de MENE2504621A).
        ///   3e       Même texte de 2020. Annexe 1 : rentrée 2028.        rentrée 2020
        ///   2de GT   MENE2504621A, ANNEXE 2 « Programme d'allemand       rentrée 2025
        ///            pour les classes de lycée général et techno. »
        ///   1re GT   MENE2504621A, annexe 2                              rentrée 2026
        ///   Tle GT   MENE2504621A, annexe 2                              rentrée 2026
        ///
        ///   URL RÉELLEMENT LUES le 04/10/2026 (PDF téléchargés, texte extrait
        ///   et lu : tableaux de niveaux, axes et objets d'étude des six
        ///   classes, phonologie et grammaire de la 5e à la terminale, lexique
        ///   de 5e, 4e et 3e) :
        ///     https://www.education.gouv.fr/sites/default/files/ensel621_annexe1.pdf
        ///       (« Programme d'allemand pour les classes de collège »)
        ///     https://www.education.gouv.fr/sites/default/files/ensel621_annexe2.pdf
        ///       (« Programme d'allemand pour les classes de lycée général et
        ///       technologique » ; ensel621_annexe2_ok.pdf est le même fichier,
        ///       octet pour octet)
        ///
        /// ================== NON VÉRIFIÉ le 04/10/2026 ==================
        ///
        ///   1. La page du BO elle-même
        ///      (https://www.education.gouv.fr/bo/2025/Hebdo22/MENE2504621A)
        ///      a répondu 403 : l'ARTICLE 4 (dates d'application par classe) et
        ///      la numérotation des annexes (1 = collège, 2 = lycée) n'ont PAS
        ///      été relus. Ils sont repris de l'en-tête de `ReferentielEspagnol`
        ///      (lecture du 14/09/2026). Les PDF ne portent pas leur numéro
        ///      d'annexe dans le texte : seul le nom du fichier le donne.
        ///   2. Le programme du cycle 4 de 2020 (applicable en 4e et en 3e
        ///      cette année) n'a PAS été relu : même reprise du modèle espagnol.
        ///      Les lignes de 4e et de 3e suivent donc l'annexe 1, qui n'est
        ///      obligatoire qu'en 2027 et en 2028 (voir plus bas).
        ///   3. Les libellés des activités langagières (compréhension,
        ///      expression, interaction, médiation) reprennent ceux du modèle
        ///      espagnol : le préambule et les descripteurs sont communs à
        ///      toutes les langues. Vérifié par sondage seulement dans les
        ///      annexes d'allemand (consignes, prospectus et invitation, dictée,
        ///      dictionnaire, aisance raisonnable, bulletins d'information,
        ///      pour et contre, sources diverses), PAS ligne à ligne.
        ///   4. Les formats d'évaluation de terminale (document d'une minute
        ///      trente, oral sur un axe) viennent de la note de service
        ///      MENE2618195N, NON relue : les lignes DE_TLE_CO_DOCUMENT,
        ///      DE_TLE_EO_AXE et DE_TLE_INT_ENTRETIEN les reprennent du modèle.
        ///   5. Les horaires de LVB et l'article D. 312-16 (B1 en fin de
        ///      terminale) : non relus, voir l'en-tête de `ReferentielEspagnol`.
        ///   6. Le lexique du lycée n'a été lu que par ses premières lignes :
        ///      les lignes « Lexique » de 2de, 1re et Tle sont formulées à
        ///      partir des intitulés des axes, pas des listes.
        ///   7. Aucune ressource Éduscol propre à l'allemand n'a été consultée.
        ///
        /// ================== NIVEAUX CECRL VISÉS EN LVB ==================
        ///
        ///   Annexes 1 et 2, tableau du préambule commun, lu dans le texte de
        ///   l'allemand : identique à celui de l'espagnol.
        ///     5e A1+   4e A1+   3e A2   2de A2+   1re B1   Tle B1
        ///   « + » : « le niveau supérieur est atteint dans au moins l'une des
        ///   activités langagières ». Annexe 1, classe de 5e : certains élèves
        ///   ont commencé en primaire, en 6e LVA ou en 6e bilangue, « d'autres
        ///   enfin débutent en LVB en 5e » : c'est le cas visé ici.
        ///   4e et 3e : le texte de 2020 ne fixe que A2 dans au moins deux
        ///   activités langagières en fin de cycle (repris du modèle, point 2
        ///   ci-dessus) ; le A1+ de 4e est un repère du nouveau texte.
        ///
        /// ================== CE QUI EST PRESCRIPTIF ==================
        ///
        ///   PRESCRIPTIF : les niveaux CECRL ; les axes ; l'axe 6 OBLIGATOIRE
        ///   (5e, 4e, 3e et lycée général : cinq axes sur six « dont
        ///   obligatoirement l'axe 6 »). Voie TECHNOLOGIQUE : au moins trois
        ///   axes, l'axe 6 « vivement recommandé ».
        ///   Axe 6 de l'allemand : 5e « 16 nuances d'Allemagne », 4e « Suisse
        ///   et Autriche », 3e « Le monde franco-allemand », 2de « Les pays
        ///   germanophones au carrefour de l'Europe », 1re « L'Allemagne dans
        ///   le monde », Tle « Deutsch, Sprache der Dichter, Denker und
        ///   Künstler ».
        ///   INDICATIF : les objets d'étude (« proposés à titre indicatif ») et
        ///   les listes de phonologie, de lexique et de grammaire (« indicatives
        ///   et non exhaustives »). Les points de grammaire retenus ici suivent
        ///   donc des listes indicatives : ils ne sont pas exigibles un par un.
        ///   Dans ces listes, le niveau supérieur est marqué « en bleu » (dans
        ///   le texte extrait : préfixe « (A2) », « (A2+) », « (B1) »,
        ///   « (B1+) », « (B2) »). On n'a gardé que la colonne LVB : ce qui est
        ///   marqué A2 en 5e, A2+ en 4e, B1 en 3e, B1+ en 2de et en 1re, B2 en
        ///   terminale relève de la LVA et n'est pas demandé ici. Exemples
        ///   écartés à ce titre : le futur en werden et les subordonnées en 5e,
        ///   dass, obwohl et um… zu en 4e, le plus-que-parfait et le génitif en 3e,
        ///   als ob et je… desto avant la terminale, le subjonctif I et le
        ///   double infinitif en terminale.
        ///   L'annexe précise que plusieurs faits de langue sont employés « par
        ///   simple imitation » en début d'apprentissage (datif, adjectif
        ///   épithète) : d'où les « blocs » dans les libellés.
        ///
        ///   4e et 3e : comme en espagnol, les axes 6 du nouveau programme
        ///   (Suisse et Autriche, monde franco-allemand) et ses repères de
        ///   langue figurent déjà dans le référentiel ; ils ne deviennent
        ///   obligatoires qu'en 2027 et en 2028.
        ///   Le domaine « Méthode » ne porte que la ligne CECRL, comme en
        ///   espagnol.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- 5e : LVB débutant, A1+ visé (annexe 1, en vigueur) ---
                ("CINQUIEME", "DE_5E_CO_CONSIGNES",     "Compréhension de l'oral", "Comprendre des consignes de classe et des énoncés très simples prononcés lentement", 1),
                ("CINQUIEME", "DE_5E_CO_MOTS_CLES",     "Compréhension de l'oral", "Découper la chaîne sonore et repérer mots connus et mots transparents pour identifier le thème d'un document court", 2),
                ("CINQUIEME", "DE_5E_CE_TEXTE_COURT",   "Compréhension de l'écrit", "Repérer dans un texte court les noms grâce à leur majuscule et le verbe grâce à sa place", 3),
                ("CINQUIEME", "DE_5E_EO_PRESENTER",     "Expression orale", "Se présenter avec ich heiße, ich bin… Jahre alt, ich komme aus, ich wohne in", 4),
                ("CINQUIEME", "DE_5E_EO_DECRIRE",       "Expression orale", "Décrire très simplement une personne, un objet ou un lieu à partir d'un modèle", 5),
                ("CINQUIEME", "DE_5E_EO_GOUTS",         "Expression orale", "Exprimer ses goûts et ses souhaits avec ich mag, das gefällt mir, ich möchte, ich hätte gern", 6),
                ("CINQUIEME", "DE_5E_INT_QUESTIONS",    "Interaction", "Poser des questions simples et y répondre dans des situations répétées", 7),
                ("CINQUIEME", "DE_5E_INT_POLITESSE",    "Interaction", "Saluer, prendre congé, remercier, s'excuser et demander des nouvelles (Wie geht's dir? Mir geht's gut.)", 8),
                ("CINQUIEME", "DE_5E_EE_COPIE_DICTEE",  "Expression écrite", "Copier, écrire sous la dictée et compléter une fiche ou un court message", 9),
                ("CINQUIEME", "DE_5E_MED_INFOS",        "Médiation", "Transmettre les informations factuelles d'une invitation ou d'un prospectus : lieu, horaire, prix", 10),
                ("CINQUIEME", "DE_5E_PHONO_VOYELLES",   "Phonologie", "Relier graphie et son : voyelle brève devant consonne double (Sonne, Mutter), longue devant ß et h (Fuß, fahren)", 11),
                ("CINQUIEME", "DE_5E_PHONO_ACCENT",     "Phonologie", "Reproduire l'accent de mot et l'intonation, et accentuer le préverbe séparable (einladen, aufstehen)", 12),
                ("CINQUIEME", "DE_5E_GRAM_PRESENT",     "Grammaire", "Conjuguer au présent haben, sein, des verbes faibles et forts courants et quelques verbes à préverbe séparable", 13),
                ("CINQUIEME", "DE_5E_GRAM_MODAUX",      "Grammaire", "Employer können, müssen, wollen, mögen et dürfen au présent, avec l'infinitif en fin de phrase", 14),
                ("CINQUIEME", "DE_5E_GRAM_GN",          "Grammaire", "Écrire le nom avec une majuscule et employer articles définis et indéfinis, kein, possessifs, genre et pluriel courants", 15),
                ("CINQUIEME", "DE_5E_GRAM_VERBE_2",     "Grammaire", "Placer le verbe en deuxième position, y compris quand la phrase ne commence pas par le sujet (Heute ist er nicht da.)", 16),
                ("CINQUIEME", "DE_5E_GRAM_PASSE",       "Grammaire", "Employer war et hatte et le parfait de quelques verbes faibles (ich habe gelernt, ich habe nicht verstanden)", 17),
                ("CINQUIEME", "DE_5E_GRAM_COMPARER",    "Grammaire", "Comparer avec schöner als et so schön wie, employer besser, et relier avec und, aber, oder, denn", 18),
                ("CINQUIEME", "DE_5E_LEX_QUOTIDIEN",    "Lexique", "Employer le lexique de la famille, du portrait, de l'école, des loisirs, de la nourriture, de l'heure et des nombres jusqu'à 100", 19),
                ("CINQUIEME", "DE_5E_CULT_16_LAENDER",  "Culture", "Découvrir l'Allemagne des seize Länder : fédéralisme, symboles, journée de l'unité du 3 octobre", 20),
                ("CINQUIEME", "DE_5E_CULT_QUOTIDIEN",   "Culture", "Comparer le quotidien d'un jeune en Allemagne et en France : journée d'école, AG et associations, carnaval", 21),
                ("CINQUIEME", "DE_5E_CECRL",            "Méthode", "Atteindre le niveau A1+ visé en fin de cinquième pour une LVB débutée cette année", 22),

                // --- 4e : A1+ consolidé (programme 2020 en vigueur, annexe 1 en 2027) ---
                ("QUATRIEME", "DE_4E_CO_MESSAGE",       "Compréhension de l'oral", "Isoler des informations simples d'un message oral clair et les relier à un thème", 1),
                ("QUATRIEME", "DE_4E_CE_DOCUMENT",      "Compréhension de l'écrit", "Comprendre un document écrit simple en s'appuyant sur titres, slogans, verbes d'opinion et temps des verbes", 2),
                ("QUATRIEME", "DE_4E_EO_DECRIRE",       "Expression orale", "Décrire en coordonnant plusieurs éléments simples : personne, lieu, activité", 3),
                ("QUATRIEME", "DE_4E_EO_RACONTER",      "Expression orale", "Raconter brièvement un événement passé avec quelques verbes courants", 4),
                ("QUATRIEME", "DE_4E_EO_LIRE_VOIX",     "Expression orale", "Lire à voix haute ou dire un texte bref préparé, un poème par exemple, avec l'intonation juste", 5),
                ("QUATRIEME", "DE_4E_INT_ECHANGE",      "Interaction", "Engager et clore une conversation simple, demander et donner des nouvelles", 6),
                ("QUATRIEME", "DE_4E_INT_PROPOSER",     "Interaction", "Proposer, conseiller, accepter ou refuser en contexte connu (wir könnten, wir sollten)", 7),
                ("QUATRIEME", "DE_4E_EE_MESSAGE",       "Expression écrite", "Rédiger un court message, une carte ou un récit bref à partir d'un modèle", 8),
                ("QUATRIEME", "DE_4E_MED_REFORMULER",   "Médiation", "Reformuler pour un camarade l'essentiel d'un document simple", 9),
                ("QUATRIEME", "DE_4E_PHONO_SONS",       "Phonologie", "Prononcer l'inflexion au pluriel (Bruder, Brüder), le e final (Tasse, Blume) et le ch selon son entourage (Bauch, reich)", 10),
                ("QUATRIEME", "DE_4E_GRAM_FUTUR",       "Grammaire", "Exprimer le futur avec l'auxiliaire werden et l'infinitif en fin de phrase", 11),
                ("QUATRIEME", "DE_4E_GRAM_PRETERIT",    "Grammaire", "Employer au prétérit war, hatte, konnte, durfte, sollte et es gab, et distinguer un prétérit d'un présent", 12),
                ("QUATRIEME", "DE_4E_GRAM_PARFAIT",     "Grammaire", "Employer le parfait de verbes faibles et de quelques verbes forts, participe en fin de phrase (ich bin gekommen)", 13),
                ("QUATRIEME", "DE_4E_GRAM_SOUHAIT",     "Grammaire", "Exprimer un souhait avec ich würde gern et ich möchte suivis de l'infinitif", 14),
                ("QUATRIEME", "DE_4E_GRAM_CAS",         "Grammaire", "Marquer l'accusatif après un verbe transitif (Ich habe keinen Hund.) et employer le datif en blocs (mit dem Bus fahren)", 15),
                ("QUATRIEME", "DE_4E_GRAM_PRONOMS",     "Grammaire", "Employer les pronoms à l'accusatif (für mich, ohne ihn) et au datif (Das gefällt mir. Mir ist kalt.)", 16),
                ("QUATRIEME", "DE_4E_GRAM_SUBORDONNEE", "Grammaire", "Construire une subordonnée de cause ou de temps avec weil, wenn ou als, verbe conjugué en fin de proposition", 17),
                ("QUATRIEME", "DE_4E_LEX_VILLE_VOYAGE", "Lexique", "Employer le lexique du sport, du voyage, de la ville, de l'itinéraire, de la météo et de l'environnement", 18),
                ("QUATRIEME", "DE_4E_CULT_VOYAGES",     "Culture", "Découvrir voyages et explorations : goût allemand du voyage, explorateurs, échanges franco-allemands", 19),
                ("QUATRIEME", "DE_4E_CULT_ARTS",        "Culture", "Découvrir des langages artistiques germanophones : scènes musicales, musique classique, poetry slam", 20),
                ("QUATRIEME", "DE_4E_CULT_SUISSE_AUTR", "Culture", "Connaître la Suisse et l'Autriche : histoire, culture, rapports avec leurs voisins, particularités de langue", 21),
                ("QUATRIEME", "DE_4E_CECRL",            "Méthode", "Consolider le niveau A1+ en fin de quatrième en LVB", 22),

                // --- 3e : A2 (programme 2020 : A2 dans au moins deux activités) ---
                ("TROISIEME", "DE_3E_CO_IDEE_GENERALE", "Compréhension de l'oral", "Suivre l'idée générale d'un document oral clair sur un sujet familier", 1),
                ("TROISIEME", "DE_3E_CE_RECIT",         "Compréhension de l'écrit", "Comprendre les informations simples d'un récit, d'un article ou d'un échange écrit", 2),
                ("TROISIEME", "DE_3E_EO_QUOTIDIEN",     "Expression orale", "Décrire activités quotidiennes, personnes et lieux, et parler de ses projets proches", 3),
                ("TROISIEME", "DE_3E_EO_OPINION",       "Expression orale", "Dire ce qu'on aime ou non et donner son avis sur un sujet d'intérêt personnel", 4),
                ("TROISIEME", "DE_3E_EO_RECIT_PASSE",   "Expression orale", "Relater brièvement un événement ou une expérience passée en la situant dans le temps", 5),
                ("TROISIEME", "DE_3E_INT_TACHE",        "Interaction", "Échanger des informations simples et directes pour mener une tâche courante", 6),
                ("TROISIEME", "DE_3E_INT_PRECISIONS",   "Interaction", "Demander et donner des précisions, relancer et reformuler pour se faire comprendre", 7),
                ("TROISIEME", "DE_3E_EE_RECIT",         "Expression écrite", "Écrire le début ou la suite d'une histoire en s'aidant d'un dictionnaire", 8),
                ("TROISIEME", "DE_3E_EE_COURRIER",      "Expression écrite", "Rédiger un courriel ou une lettre simple avec les formules d'adresse et de congé", 9),
                ("TROISIEME", "DE_3E_MED_POINTS",       "Médiation", "Transmettre les points principaux d'un texte ou d'une conversation sur un sujet familier", 10),
                ("TROISIEME", "DE_3E_PHONO_FINALES",    "Phonologie", "Assourdir b, d, g, s en fin de mot (Erfolg, Glas) et accentuer les verbes en -ieren et les emprunts", 11),
                ("TROISIEME", "DE_3E_GRAM_PRETERIT",    "Grammaire", "Employer sein, haben et les modaux au prétérit et reconnaître celui de verbes fréquents (lebte, kam)", 12),
                ("TROISIEME", "DE_3E_GRAM_SUBJONCTIF_2","Grammaire", "Employer le subjonctif II : hätte, wäre, würde + infinitif, et au passé (Ich hätte nie gedacht, dass…)", 13),
                ("TROISIEME", "DE_3E_GRAM_PASSIF",      "Grammaire", "Comprendre la relation passive et l'employer à la troisième personne (Der Vertrag wird / wurde unterzeichnet.)", 14),
                ("TROISIEME", "DE_3E_GRAM_CAS",         "Grammaire", "Marquer nominatif et accusatif, le datif en blocs, et connaître les prépositions durch, um et aus, bei, nach, von, zu", 15),
                ("TROISIEME", "DE_3E_GRAM_SUBORDONNEES","Grammaire", "Distinguer als et wenn, employer obwohl pour la concession et um… zu pour le but", 16),
                ("TROISIEME", "DE_3E_GRAM_PHRASE",      "Grammaire", "Commencer la phrase par une subordonnée ou par also, deshalb, en gardant le verbe en deuxième position", 17),
                ("TROISIEME", "DE_3E_LEX_COMPOSES",     "Lexique", "Décomposer mots composés et dérivés pour en trouver le sens (arbeitslos, essbar, Ausland)", 18),
                ("TROISIEME", "DE_3E_LEX_TRAVAIL",      "Lexique", "Employer le lexique du travail, de la formation, des migrations, des médias et de l'environnement", 19),
                ("TROISIEME", "DE_3E_CULT_FRANCO_ALLEM","Culture", "Connaître le monde franco-allemand : amitié, coopération transfrontalière, acteurs d'hier et d'aujourd'hui", 20),
                ("TROISIEME", "DE_3E_CULT_EST_OUEST",   "Culture", "Comprendre la division et les retrouvailles allemandes : RDA et RFA, mur de Berlin, 1945 et 1989", 21),
                ("TROISIEME", "DE_3E_CECRL",            "Méthode", "Atteindre le niveau A2 dans au moins deux activités langagières en fin de troisième", 22),

                // --- Seconde : A2+ visé (annexe 2, en vigueur depuis 2025) ---
                ("SECONDE", "DE_2DE_CO_INFOS",          "Compréhension de l'oral", "Comprendre les informations principales d'une conversation ou d'une émission claire et lente", 1),
                ("SECONDE", "DE_2DE_CO_HYPOTHESES",     "Compréhension de l'oral", "Formuler des hypothèses à partir des indices et vérifier sa compréhension par étapes", 2),
                ("SECONDE", "DE_2DE_CE_TEXTES",         "Compréhension de l'écrit", "Comprendre les informations principales de textes variés sur des sujets familiers", 3),
                ("SECONDE", "DE_2DE_EO_PRESENTER",      "Expression orale", "Présenter son quotidien, des projets, des activités passées et des expériences personnelles", 4),
                ("SECONDE", "DE_2DE_EO_JUSTIFIER",      "Expression orale", "Expliquer pourquoi une chose plaît ou déplaît et comparer de façon simple", 5),
                ("SECONDE", "DE_2DE_INT_ECHANGER",      "Interaction", "Échanger idées et renseignements avec une aisance raisonnable en situation prévisible", 6),
                ("SECONDE", "DE_2DE_EE_RENDRE_COMPTE",  "Expression écrite", "Rendre compte par écrit d'un document étudié et en rédiger un bref résumé", 7),
                ("SECONDE", "DE_2DE_EE_TRANSFORMER",    "Expression écrite", "Transformer un document : article en interview, récit en saynète", 8),
                ("SECONDE", "DE_2DE_MED_INFOS",         "Médiation", "Transmettre les informations pertinentes d'un texte informatif court et bien structuré", 9),
                ("SECONDE", "DE_2DE_PHONO_V_W",         "Phonologie", "Distinguer v et w (Vertrag, verlassen, WG) et prononcer les finales, dont -er (aber)", 10),
                ("SECONDE", "DE_2DE_PHONO_SIGLES",      "Phonologie", "Accentuer les abréviations (EU, DDR, U-Bahn) et viser un rythme de phrase plus authentique", 11),
                ("SECONDE", "DE_2DE_GRAM_TEMPS",        "Grammaire", "Maîtriser les temps de l'indicatif : présent, parfait et prétérit des verbes forts et faibles irréguliers", 12),
                ("SECONDE", "DE_2DE_GRAM_SUBJONCTIF_2", "Grammaire", "Employer le subjonctif II présent : hätte, wäre, würde, les modaux, wüsste, es gäbe, er käme, et würde + infinitif", 13),
                ("SECONDE", "DE_2DE_GRAM_REGRET",       "Grammaire", "Exprimer le regret au subjonctif II passé (Wenn ich das gewusst hätte…)", 14),
                ("SECONDE", "DE_2DE_GRAM_PASSIF",       "Grammaire", "Employer le passif présent et prétérit avec le complément d'agent en von", 15),
                ("SECONDE", "DE_2DE_GRAM_RECTIONS",     "Grammaire", "Employer les verbes avec leur cas ou leur préposition : jm helfen, sprechen über, sich erinnern an, stolz sein auf", 16),
                ("SECONDE", "DE_2DE_GRAM_GENITIF",      "Grammaire", "Employer le génitif (complément du nom, wegen, während, trotz) et la relative au nominatif et à l'accusatif", 17),
                ("SECONDE", "DE_2DE_GRAM_SUBORDONNER",  "Grammaire", "Placer le verbe après coordination ou subordination, employer während et damit, distinguer um… zu et damit", 18),
                ("SECONDE", "DE_2DE_LEX_AXES",          "Lexique", "Mobiliser le lexique de l'identité, des générations, du passé et de la mémoire, des transitions", 19),
                ("SECONDE", "DE_2DE_CULT_CARREFOUR",    "Culture", "Situer les pays germanophones au carrefour de l'Europe : couple franco-allemand, Est et Ouest, Union européenne", 20),
                ("SECONDE", "DE_2DE_CULT_PASSE",        "Culture", "Analyser comment les pays germanophones assument leur passé : travail de mémoire, Ostalgie, traditions", 21),
                ("SECONDE", "DE_2DE_CECRL",             "Méthode", "Atteindre le niveau A2+ visé en fin de seconde en LVB", 22),

                // --- Première : B1 visé (annexe 2, en vigueur en 2026) ---
                ("PREMIERE", "DE_1RE_CO_POINTS",        "Compréhension de l'oral", "Comprendre les points principaux d'un bulletin d'information ou d'un film en langue claire", 1),
                ("PREMIERE", "DE_1RE_CE_RECIT",         "Compréhension de l'écrit", "Suivre l'intrigue d'un récit simple et déduire le sens des mots composés ou dérivés", 2),
                ("PREMIERE", "DE_1RE_EO_EXPOSE",        "Expression orale", "Faire un exposé préparé, suivi sans difficulté, sur un sujet familier", 3),
                ("PREMIERE", "DE_1RE_EO_OPINION",       "Expression orale", "Exprimer et justifier une opinion en comparant et en pesant le pour et le contre", 4),
                ("PREMIERE", "DE_1RE_INT_CONVERSATION", "Interaction", "Prendre part à une conversation prolongée sur un sujet familier en prenant des initiatives", 5),
                ("PREMIERE", "DE_1RE_INT_NUANCE",       "Interaction", "Exprimer accord ou désaccord avec nuance et reformuler pour vérifier la compréhension", 6),
                ("PREMIERE", "DE_1RE_EE_CRITIQUE",      "Expression écrite", "Rédiger une critique simple d'un film ou d'un livre en justifiant son point de vue", 7),
                ("PREMIERE", "DE_1RE_EE_CONTROLER",     "Expression écrite", "Contrôler sa production après coup et la structurer avec des connecteurs variés", 8),
                ("PREMIERE", "DE_1RE_MED_RESUMER",      "Médiation", "Résumer en français l'information et les arguments d'un dossier en allemand", 9),
                ("PREMIERE", "DE_1RE_MED_IMPLICITE",    "Médiation", "Expliciter pour autrui une référence culturelle implicite présente dans un message", 10),
                ("PREMIERE", "DE_1RE_PHONO_COMPOSES",   "Phonologie", "Accentuer noms composés et dérivés (Wissenschaftlerin) et prononcer y et h intérieur (Physik, Freiheit)", 11),
                ("PREMIERE", "DE_1RE_GRAM_TEMPS",       "Grammaire", "Consolider les temps de l'indicatif et le subjonctif II présent et passé", 12),
                ("PREMIERE", "DE_1RE_GRAM_PASSIF",      "Grammaire", "Consolider le passif présent et prétérit avec le complément d'agent en von", 13),
                ("PREMIERE", "DE_1RE_GRAM_RECTIONS",    "Grammaire", "Enrichir les rections : jm danken, jn um etwas bitten, Einfluss haben auf, interessiert sein an", 14),
                ("PREMIERE", "DE_1RE_GRAM_RELATIVES",   "Grammaire", "Compléter le nom par un génitif ou une relative, et employer was après etwas, alles, das", 15),
                ("PREMIERE", "DE_1RE_GRAM_INFINITIVES", "Grammaire", "Employer anstatt… zu et ohne… zu, et distinguer um… zu de damit pour le but", 16),
                ("PREMIERE", "DE_1RE_GRAM_CONNECTEURS", "Grammaire", "Exprimer cause, conséquence, concession et simultanéité : nämlich, deswegen, trotzdem, auch wenn, während", 17),
                ("PREMIERE", "DE_1RE_LEX_PREVERBES",    "Lexique", "Enrichir son lexique par les préverbes (auswandern) et mobiliser celui des échanges, de l'art et de la nature", 18),
                ("PREMIERE", "DE_1RE_CULT_MONDE",       "Culture", "Situer l'Allemagne dans le monde : émigration, passé colonial, poids économique, politique étrangère", 19),
                ("PREMIERE", "DE_1RE_CULT_ART_POUVOIR", "Culture", "Analyser les liens entre art et pouvoir : propagande sous les dictatures allemandes, artistes en exil, contestation", 20),
                ("PREMIERE", "DE_1RE_CULT_NATURE",      "Culture", "Étudier le rapport à la nature dans l'aire germanophone : romantisme, protection de l'environnement, écologie politique", 21),
                ("PREMIERE", "DE_1RE_CECRL",            "Méthode", "Atteindre le niveau B1 visé en fin de première en LVB", 22),

                // --- Terminale : B1 attendu (annexe 2 ; D. 312-16) ---
                ("TERMINALE", "DE_TLE_CO_DOCUMENT",     "Compréhension de l'oral", "Rendre compte en français d'un document audio ou vidéo d'une minute trente écouté trois fois", 1),
                ("TERMINALE", "DE_TLE_CE_DOSSIER",      "Compréhension de l'écrit", "Rendre compte d'un ou deux textes, de façon libre ou guidée, en français ou en allemand", 2),
                ("TERMINALE", "DE_TLE_CE_LIENS",        "Compréhension de l'écrit", "Sélectionner et hiérarchiser les informations et relier plusieurs documents entre eux", 3),
                ("TERMINALE", "DE_TLE_EO_AXE",          "Expression orale", "Expliquer en cinq minutes quel document ou citation illustre le mieux un axe étudié", 4),
                ("TERMINALE", "DE_TLE_EO_ARGUMENTER",   "Expression orale", "Argumenter pour expliciter son raisonnement et défendre un point de vue", 5),
                ("TERMINALE", "DE_TLE_INT_ENTRETIEN",   "Interaction", "Soutenir un entretien élargi sur le travail mené autour d'un axe du programme", 6),
                ("TERMINALE", "DE_TLE_EE_ARGUMENTATIF", "Expression écrite", "Répondre en allemand à une ou deux questions liées au dossier par un texte argumenté", 7),
                ("TERMINALE", "DE_TLE_EE_CREATIF",      "Expression écrite", "Produire un écrit créatif : suite de texte, lettre, dialogue, critique de film", 8),
                ("TERMINALE", "DE_TLE_MED_EXPLIQUER",   "Médiation", "Expliquer à quelqu'un, en français ou en allemand, le sens d'un document et ses références culturelles", 9),
                ("TERMINALE", "DE_TLE_MED_SYNTHESE",    "Médiation", "Rassembler des informations de sources diverses en allemand et les résumer pour quelqu'un d'autre", 10),
                ("TERMINALE", "DE_TLE_PHONO_FLUIDITE",  "Phonologie", "Former des énoncés fluides et expressifs, accentuer les mots d'origine étrangère, repérer l'origine de quelques accents", 11),
                ("TERMINALE", "DE_TLE_GRAM_PASSIF",     "Grammaire", "Employer le passif impersonnel (Es wurde diskutiert.) et le passif avec un modal (Es kann diskutiert werden.)", 12),
                ("TERMINALE", "DE_TLE_GRAM_ADJECTIF",   "Grammaire", "Décliner l'adjectif épithète après article défini, indéfini et sans article", 13),
                ("TERMINALE", "DE_TLE_GRAM_MASC_FAIBLES","Grammaire", "Employer les masculins faibles (der Franzose, der Mensch) et mixtes (der Name, der Glaube)", 14),
                ("TERMINALE", "DE_TLE_GRAM_PRONOM_DA",  "Grammaire", "Reprendre un groupe prépositionnel par un pronom en da- (Wir waren stolz darauf.)", 15),
                ("TERMINALE", "DE_TLE_GRAM_LIENS",      "Grammaire", "Comprendre les liens complexes : bevor, nachdem, da, so dass, als ob, je… desto, et les modalisateurs (vermutlich, angeblich)", 16),
                ("TERMINALE", "DE_TLE_LEX_GENRE",       "Lexique", "Maîtriser le genre des noms d'origine étrangère (die Kooperation, der Motor, das Argument)", 17),
                ("TERMINALE", "DE_TLE_LEX_AXES",        "Lexique", "Mobiliser le lexique de la mémoire, de la communication, des médias et des mondes virtuels", 18),
                ("TERMINALE", "DE_TLE_CULT_DICHTER",    "Culture", "Connaître le patrimoine culturel germanophone : Lumières et romantisme, théâtre, cinéma, danse", 19),
                ("TERMINALE", "DE_TLE_CULT_MEMOIRE",    "Culture", "Étudier territoire et mémoire : mur de Berlin, lieux de mémoire de la Seconde Guerre mondiale", 20),
                ("TERMINALE", "DE_TLE_CULT_ESPACES",    "Culture", "Interroger espace privé et espace public : émancipation des femmes, protection des données, éducation sous les dictatures", 21),
                ("TERMINALE", "DE_TLE_CECRL",           "Méthode", "Atteindre le niveau B1 visé en fin de terminale en LVB", 22),
            };
    }
}
