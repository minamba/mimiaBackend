namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// Les enseignements de spécialité LLCER anglais et LLCER anglais, monde contemporain (AMC) de la voie
    /// générale, tels que les textes officiels les définissent — provenance détaillée en tête du fichier.
    /// Distincts du référentiel d'anglais du tronc commun (LVA/LVB), qui n'est pas repris ici.
    /// </summary>
    public static class ReferentielLlcerAnglais
    {
        // =====================================================================================
        // RÉFÉRENTIELS DES SPÉCIALITÉS LLCER ANGLAIS ET LLCER ANGLAIS, MONDE CONTEMPORAIN (voie générale)
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis les textes
        // officiels téléchargés et lus le 14/09/2026, rien de mémoire. Sources gardées dans
        // specialites\sources\llcer-anglais\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau  | Niveau    | Intitulé exact (page de titre de l'annexe)                                                  |
        // |----------|-----------|---------------------------------------------------------------------------------------------|
        // | LLCERAN1 | PREMIERE  | Programme de langues, littératures et cultures étrangères - anglais - de première générale  |
        // | LLCERANT | TERMINALE | Programme de langues, littératures et cultures étrangères et régionales - anglais de terminale générale |
        // | AMC1     | PREMIERE  | Programme de langues, littératures et cultures étrangères et régionales – anglais, monde contemporain de première générale |
        // | AMCT     | TERMINALE | Programme de langues, littératures et cultures étrangères et régionales – anglais, monde contemporain de terminale générale |
        //
        // LLCERAN1 : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901590A, BO spécial n° 1 du 22 janvier 2019,
        //   annexe 2 (anglais). Article 2 : « entrent en vigueur à la rentrée scolaire 2019 ».
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901590A.htm
        //   Annexe 2 lue    : https://eduscol.education.gouv.fr/sites/default/files/document/spe590annexe222-11063850pdf-83328.pdf
        //   Modifié par l'arrêté du 28-6-2019 (BO n° 28 du 11-7-2019 ; modifie l'article 1 — texte non lu en entier,
        //   seule la fiche Légifrance l'a été) et par l'arrêté du 8-7-2020 (ajout de l'annexe AMC, voir AMC1) —
        //   Légifrance, version au 14/09/2026.
        //
        // LLCERANT : arrêté du 19-7-2019, J.O. du 23-7-2019, NOR MENE1921256A, BO spécial n° 8 du 25 juillet 2019,
        //   annexe 2 (anglais). Article 2 : « entrent en vigueur à la rentrée scolaire 2020 ».
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921256A.htm
        //   Annexe 2 lue    : https://eduscol.education.gouv.fr/sites/default/files/document/spe256annexe21158999pdf-83364.pdf
        //   Modifié par l'arrêté du 8-7-2020 (ajout de l'annexe AMC, voir AMCT) et par l'arrêté du 7-4-2025,
        //   NOR MENE2504619A (J.O. du 30-4-2025), qui ne touche QUE l'annexe 3 (espagnol) : sans effet sur l'anglais.
        //
        // AMC1 : arrêté du 8-7-2020, J.O. du 21-7-2020, NOR MENE2017287A, BO n° 30 du 23 juillet 2020.
        //   Article 1 : complète l'arrêté du 17-1-2019 par l'annexe « anglais, monde contemporain ».
        //   Article 2 : « entrent en vigueur à compter de la rentrée scolaire 2020 ».
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/20/Hebdo30/MENE2017287A.htm
        //   Annexe lue      : https://www.education.gouv.fr/media/69831/download
        //
        // AMCT : arrêté du 8-7-2020, J.O. du 21-7-2020, NOR MENE2017292A, BO n° 30 du 23 juillet 2020.
        //   Article 1 : complète l'arrêté du 19-7-2019 par l'annexe « anglais, monde contemporain ».
        //   Article 2 : « entrent en vigueur à compter de la rentrée scolaire 2020 ».
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/20/Hebdo30/MENE2017292A.htm
        //   Annexe lue      : https://www.education.gouv.fr/media/69834/download
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027
        //   - Page éduscol « Programmes et ressources en langues, littératures et cultures étrangères et régionales -
        //     voie G » (https://eduscol.education.gouv.fr/5814/..., datée « juin 2026 ») : « Programmes en vigueur » =
        //     arrêtés du 17-1-2019, 28-6-2019, 19-7-2019 et arrêtés de 2020 pour AMC ; elle renvoie aux quatre PDF lus.
        //   - Légifrance, versions en vigueur au 14/09/2026 : les arrêtés du 17-1-2019 (MENE1901590A) et du 19-7-2019
        //     (MENE1921256A) sont « en vigueur », non abrogés.
        //   - L'arrêté du 5-5-2025, NOR MENE2504621A (BO n° 22 du 29-5-2025) renouvelle les programmes « d'enseignements
        //     COMMUNS et OPTIONNELS » de langues vivantes étrangères ; son article 3 abroge « l'arrêté du 17 janvier 2019
        //     susvisé » en ce qu'il concerne les LVE : c'est l'arrêté du tronc commun et de l'option, PAS celui de la
        //     spécialité (Légifrance le garde en vigueur, la page éduscol 5811 réserve les nouveaux programmes au commun et
        //     à l'optionnel, et la page 5814 de juin 2026 cite toujours le 17-1-2019 pour la spécialité).
        //   - Programmes limitatifs en cours (LLCER anglais seulement ; AMC n'en a pas) : note MENE2504606N (BO n° 25 du
        //     19-6-2025) pour la 1re en 2025-2026 et 2026-2027 ; note MENE2611474N (BO n° 21 du 21-5-2026) pour la Tle en
        //     2026-2027 et 2027-2028. Les titres d'œuvres sont dans llcer-anglais.prof.md, pas dans les tableaux.
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - L'intitulé de première LLCER dit « langues, littératures et cultures étrangères » (sans « et régionales ») :
        //     c'est le titre de l'annexe de 2019, repris tel quel ; l'intitulé de l'arrêté, lui, porte « et régionales »
        //     depuis sa modification du 28-6-2019 (fiche Légifrance).
        //   - Les annexes AMC portent en en-tête « Bulletin officiel n° 29 du 16-7-2020 », alors que les arrêtés qui les
        //     publient sont au BO n° 30 du 23-7-2020. La page éduscol cite en outre un « arrêté du 10-7-2020 » qui n'a pas
        //     été retrouvé. Retenu : BO n° 30 (page officielle de l'arrêté lue).
        //   - Les programmes de LLCER anglais citent œuvres et artistes « à titre d'exemples » : les libellés ne les
        //     rendent pas obligatoires. Les axes de LLCER sont « indicatifs » en 1re ; ceux d'AMC « doivent tous avoir été
        //     abordés au moins une fois ».
        //   - Les lignes « Méthode » reprennent aussi la forme de l'épreuve terminale (note consolidée éduscol, mai 2024) :
        //     synthèse, traduction ou transposition, dossier personnel de 4 à 6 documents, oral 10 + 10 min.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII majuscules, <= 40 caractères, libellés < 150, Ordre continu)
        //   LLCERAN1 35 · LLCERANT 40 · AMC1 39 · AMCT 43 · total 157
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — LLCER ANGLAIS (annexe 2 de MENE1901590A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] LLCERAN1 =
        {
            // Activités langagières
            ("PREMIERE", "LLCERAN1_LIRE_TEXTES_LONGS",   "Activités langagières", "Rendre compte à l'oral d'un texte long issu de la littérature, de la critique ou de la presse", 1),
            ("PREMIERE", "LLCERAN1_SENS_IMPLICITE",      "Activités langagières", "Dégager le sens explicite et implicite d'un document écrit ou audiovisuel", 2),
            ("PREMIERE", "LLCERAN1_AUDIOVISUEL",         "Activités langagières", "Comprendre une émission ou un film dans une langue authentique aux accents variés", 3),
            ("PREMIERE", "LLCERAN1_EXPOSE_NOTES",        "Activités langagières", "Présenter un exposé en anglais devant un auditoire à partir de simples notes", 4),
            ("PREMIERE", "LLCERAN1_INTERACTION",         "Activités langagières", "Interagir pour construire collectivement le sens d'un support en écoutant et en relançant l'échange", 5),
            ("PREMIERE", "LLCERAN1_MEDIATION_SYNTHESE",  "Activités langagières", "Paraphraser ou synthétiser pour autrui un propos ou un dossier documentaire", 6),
            ("PREMIERE", "LLCERAN1_MEDIATION_REPERES",   "Activités langagières", "Rendre compréhensible à autrui un repère culturel du monde anglophone qui lui est inaccessible", 7),
            ("PREMIERE", "LLCERAN1_MISE_EN_VOIX",        "Activités langagières", "Interpréter un texte mémorisé ou animer une interview ou une table ronde en anglais", 8),
            ("PREMIERE", "LLCERAN1_PHONOLOGIE",          "Activités langagières", "Restituer rythme, accentuation et intonation dans une lecture à voix haute ou une prise de parole", 9),
            ("PREMIERE", "LLCERAN1_GRAMMAIRE_REGLE",     "Activités langagières", "Dégager et formuler une règle de grammaire à partir d'exemples tirés d'un document", 10),
            ("PREMIERE", "LLCERAN1_TRADUCTION",          "Activités langagières", "Traduire un court passage en français en expliquant un écart entre les deux langues", 11),

            // Méthode
            ("PREMIERE", "LLCERAN1_LEXIQUE_ANALYSE",     "Méthode", "Employer le vocabulaire du commentaire de texte littéraire ou non fictionnel, d'image et de film", 12),
            ("PREMIERE", "LLCERAN1_ANALYSE_IMAGE",       "Méthode", "Analyser une image fixe ou mobile en lien avec une thématique du programme", 13),
            ("PREMIERE", "LLCERAN1_GENRES_COURANTS",     "Méthode", "Situer une œuvre anglophone dans son genre, son mouvement ou son courant littéraire", 14),
            ("PREMIERE", "LLCERAN1_CONTEXTUALISER",      "Méthode", "Replacer une œuvre ou un artiste dans son contexte historique, politique et social", 15),
            ("PREMIERE", "LLCERAN1_CROISER_DOCUMENTS",   "Méthode", "Mettre en regard des documents de natures et d'époques différentes pour dégager continuités et ruptures", 16),
            ("PREMIERE", "LLCERAN1_ARGUMENTER",          "Méthode", "Défendre à l'oral une position argumentée sur un document, étayée par des exemples précis", 17),
            ("PREMIERE", "LLCERAN1_DOSSIER_PERSONNEL",   "Méthode", "Présenter les documents choisis pour son dossier personnel et justifier leur lien avec les thématiques", 18),

            // Œuvres intégrales (programme limitatif)
            ("PREMIERE", "LLCERAN1_OEUVRE_INTEGRALE",    "Œuvres intégrales", "Rendre compte de la lecture d'une œuvre intégrale du programme limitatif : intrigue, personnages, enjeux", 19),
            ("PREMIERE", "LLCERAN1_OEUVRE_THEMATIQUE",   "Œuvres intégrales", "Relier un extrait d'une œuvre intégrale étudiée à la thématique « Imaginaires » ou « Rencontres »", 20),

            // Thématique « Imaginaires »
            ("PREMIERE", "LLCERAN1_IMAG_RAPPORT_REEL",   "Imaginaires", "Expliquer comment une œuvre d'imagination s'éloigne du réel pour mieux le penser", 21),
            ("PREMIERE", "LLCERAN1_IMAG_MONDES",         "Imaginaires — Imagination créatrice et visionnaire", "Montrer comment une œuvre invente un monde extraordinaire ou donne forme à une vision onirique", 22),
            ("PREMIERE", "LLCERAN1_IMAG_SCIENCE",        "Imaginaires — Imagination créatrice et visionnaire", "Analyser une œuvre ou un article qui repousse les limites de la science par l'imagination", 23),
            ("PREMIERE", "LLCERAN1_IMAG_MONSTRE",        "Imaginaires — Imaginaires effrayants", "Analyser le motif du monstre et les techniques propres au gothique et à l'horreur", 24),
            ("PREMIERE", "LLCERAN1_IMAG_PEURS",          "Imaginaires — Imaginaires effrayants", "Discuter les peurs suscitées par les robots, les OGM, le clonage ou le transhumanisme", 25),
            ("PREMIERE", "LLCERAN1_IMAG_UTOPIE_DYSTOPIE","Imaginaires — Utopies et dystopies", "Distinguer utopie et dystopie et expliquer la mise en garde politique d'une dystopie", 26),
            ("PREMIERE", "LLCERAN1_IMAG_PROGRES",        "Imaginaires — Utopies et dystopies", "Débattre des faces positives et menaçantes du progrès à partir d'une œuvre d'imagination", 27),

            // Thématique « Rencontres »
            ("PREMIERE", "LLCERAN1_RENC_AMOUR_AMITIE",   "Rencontres — L'amour et l'amitié", "Analyser la représentation du lien amoureux ou amical et de son pendant, la solitude", 28),
            ("PREMIERE", "LLCERAN1_RENC_RUPTURE",        "Rencontres — L'amour et l'amitié", "Montrer comment l'amour ou l'amitié deviennent source de conflit, de perte ou de souffrance", 29),
            ("PREMIERE", "LLCERAN1_RENC_INDIVIDU_GROUPE","Rencontres — Relation entre l'individu et le groupe", "Analyser la relation entre un individu et un groupe : intégration, rejet, marginalisation", 30),
            ("PREMIERE", "LLCERAN1_RENC_ECART_NORME",    "Rencontres — Relation entre l'individu et le groupe", "Expliquer l'écart à la norme que fait apparaître une rencontre dans une œuvre", 31),
            ("PREMIERE", "LLCERAN1_RENC_LUTTES",         "Rencontres — Relation entre l'individu et le groupe", "Présenter un groupe qui affirme sa solidarité ou lutte contre la discrimination, l'injustice ou la pauvreté", 32),
            ("PREMIERE", "LLCERAN1_RENC_DIFFERENCE",     "Rencontres — La confrontation à la différence", "Analyser une rencontre avec l'Autre qui oblige à interroger ses propres valeurs", 33),
            ("PREMIERE", "LLCERAN1_RENC_COLONIAL",       "Rencontres — La confrontation à la différence", "Commenter la vision coloniale d'une œuvre en la replaçant dans son contexte", 34),
            ("PREMIERE", "LLCERAN1_RENC_LIEUX_MEMOIRE",  "Rencontres — La confrontation à la différence", "Expliquer comment une statue ou un lieu de mémoire devient source de conflit ou de réconciliation", 35),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE — LLCER ANGLAIS (annexe 2 de MENE1921256A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] LLCERANT =
        {
            // Activités langagières
            ("TERMINALE", "LLCERANT_THEMES_ABSTRAITS",   "Activités langagières", "Comprendre et reformuler un document sur un thème abstrait, littéraire, artistique ou historique", 1),
            ("TERMINALE", "LLCERANT_LANGUE_NON_STANDARD","Activités langagières", "Comprendre un document audiovisuel dans une langue qui n'est pas nécessairement standardisée", 2),
            ("TERMINALE", "LLCERANT_MEDIATION_DEBAT",    "Activités langagières", "Gérer un débat pour faciliter la communication dans un contexte de désaccord", 3),
            ("TERMINALE", "LLCERANT_REFORMULATION",      "Activités langagières", "Reformuler en anglais un propos pour en transmettre le sens à un interlocuteur", 4),
            ("TERMINALE", "LLCERANT_EXPRESSION_NUANCEE", "Activités langagières", "Produire un discours détaillé, construit et nuancé en tenant compte du contexte et du destinataire", 5),
            ("TERMINALE", "LLCERANT_VERSION",            "Activités langagières", "Traduire en français un passage d'un texte de manière fidèle sans calquer l'original", 6),

            // Méthode
            ("TERMINALE", "LLCERANT_SYNTHESE_DOSSIER",   "Méthode", "Présenter la synthèse d'un dossier de trois ou quatre documents en répondant à des questions guides", 7),
            ("TERMINALE", "LLCERANT_COMMENTAIRE",        "Méthode", "Commenter un texte de civilisation ou de littérature en dégageant son sens et ses procédés", 8),
            ("TERMINALE", "LLCERANT_CONTRACTION",        "Méthode", "Contracter un texte en anglais en conservant ses idées essentielles", 9),
            ("TERMINALE", "LLCERANT_FAITS_OPINIONS",     "Méthode", "Distinguer clairement faits, croyances et opinions dans un document ou un débat", 10),
            ("TERMINALE", "LLCERANT_RECHERCHE_DOC",      "Méthode", "Présenter le résultat d'une recherche documentaire en portant un regard critique sur ses sources", 11),
            ("TERMINALE", "LLCERANT_ANALYSE_IMAGE",      "Méthode", "Analyser une image fixe ou mobile sans la réduire à une simple illustration", 12),
            ("TERMINALE", "LLCERANT_CROISER_THEMATIQUES","Méthode", "Croiser deux thématiques du programme pour analyser un même document", 13),
            ("TERMINALE", "LLCERANT_DOSSIER_LOGIQUE",    "Méthode", "Présenter en 10 minutes son dossier personnel en justifiant ses choix et sa logique interne", 14),
            ("TERMINALE", "LLCERANT_DOSSIER_ECHANGE",    "Méthode", "Défendre les choix de son dossier dans un échange de 10 minutes avec un examinateur", 15),
            ("TERMINALE", "LLCERANT_DOCUMENT_DECOUVERT", "Méthode", "Prendre librement la parole à partir d'un document découvert, puis converser sur le sujet", 16),

            // Œuvres intégrales (programme limitatif)
            ("TERMINALE", "LLCERANT_LECTURE_ANALYTIQUE", "Œuvres intégrales", "Alterner lecture analytique d'un extrait et lecture de l'œuvre entière pour en faire émerger le sens", 17),
            ("TERMINALE", "LLCERANT_OEUVRE_LITTERAIRE",  "Œuvres intégrales", "Présenter une œuvre littéraire du programme limitatif et la relier à sa thématique", 18),
            ("TERMINALE", "LLCERANT_OEUVRE_FILMIQUE",    "Œuvres intégrales", "Analyser un extrait de l'œuvre filmique étudiée en lien avec sa thématique", 19),
            ("TERMINALE", "LLCERANT_OEUVRE_PERSPECTIVE", "Œuvres intégrales", "Mettre une œuvre étudiée en perspective avec un événement historique ou un mouvement artistique", 20),

            // Thématique « Arts et débats d'idées »
            ("TERMINALE", "LLCERANT_ART_CONTESTATION",   "Arts et débats d'idées — Art et contestation", "Montrer comment une œuvre défend un point de vue, apporte un témoignage ou dénonce une injustice", 21),
            ("TERMINALE", "LLCERANT_ART_CONTESTATAIRE",  "Arts et débats d'idées — Art et contestation", "Distinguer la contestation dans l'art et l'art contestataire selon l'intention et le contexte", 22),
            ("TERMINALE", "LLCERANT_ART_CONSERVATEUR",   "Arts et débats d'idées — Art et contestation", "Identifier une contribution artistique conservatrice ou nostalgique au débat d'idées", 23),
            ("TERMINALE", "LLCERANT_ART_POLEMIQUE",      "Arts et débats d'idées — L'art qui fait débat", "Expliquer pourquoi une œuvre a fait polémique : querelle esthétique, tabou moral ou critique politique", 24),
            ("TERMINALE", "LLCERANT_ART_CENSURE",        "Arts et débats d'idées — L'art qui fait débat", "Débattre de la censure d'une œuvre et des critères qui conduisent à en limiter l'accès", 25),
            ("TERMINALE", "LLCERANT_ART_RHETORIQUE",     "Arts et débats d'idées — L'art du débat", "Analyser les procédés d'un discours qui convainc ou qui manipule", 26),
            ("TERMINALE", "LLCERANT_ART_DEBATTRE",       "Arts et débats d'idées — L'art du débat", "Participer à un débat codifié en anglais avec aisance, posture et art de la répartie", 27),

            // Thématique « Expression et construction de soi »
            ("TERMINALE", "LLCERANT_SOI_EMOTIONS",       "Expression et construction de soi — L'expression des émotions", "Analyser l'expression des émotions dans une œuvre littéraire, picturale, filmique ou oratoire", 28),
            ("TERMINALE", "LLCERANT_SOI_COURANTS",       "Expression et construction de soi — L'expression des émotions", "Définir romantisme, transcendantalisme et expressionnisme en les comparant aux repères de français", 29),
            ("TERMINALE", "LLCERANT_SOI_MISE_EN_SCENE",  "Expression et construction de soi — Mise en scène de soi", "Analyser un autoportrait ou une autobiographie comme mise en scène de soi", 30),
            ("TERMINALE", "LLCERANT_SOI_SOI_COLLECTIF",  "Expression et construction de soi — Mise en scène de soi", "Montrer comment un récit de vie ou un hymne porte une identité collective, ou la conteste", 31),
            ("TERMINALE", "LLCERANT_SOI_APPRENTISSAGE",  "Expression et construction de soi — Initiation, apprentissage", "Retracer l'évolution morale ou psychologique d'un personnage de roman d'apprentissage", 32),
            ("TERMINALE", "LLCERANT_SOI_INITIATION",     "Expression et construction de soi — Initiation, apprentissage", "Expliquer le rôle d'un voyage, d'un mentor ou d'une épreuve dans une initiation", 33),

            // Thématique « Voyages, territoires, frontières »
            ("TERMINALE", "LLCERANT_VOY_EXPLORATION",    "Voyages, territoires, frontières — Exploration et aventure", "Présenter une exploration en rappelant les repères historiques de la démarche coloniale", 34),
            ("TERMINALE", "LLCERANT_VOY_FRONTIERE",      "Voyages, territoires, frontières — Exploration et aventure", "Expliquer la frontière et la « destinée manifeste » dans la construction des États-Unis", 35),
            ("TERMINALE", "LLCERANT_VOY_HERITAGE",       "Voyages, territoires, frontières — Ancrage et héritage", "Montrer comment une communauté célèbre ou défend son héritage et son territoire", 36),
            ("TERMINALE", "LLCERANT_VOY_TERRE_NATALE",   "Voyages, territoires, frontières — Ancrage et héritage", "Analyser la représentation d'une terre natale, rurale ou urbaine, par un écrivain ou un cinéaste", 37),
            ("TERMINALE", "LLCERANT_VOY_REPLI",          "Voyages, territoires, frontières — Ancrage et héritage", "Discuter les dérives d'une revendication d'héritage : repli identitaire, xénophobie", 38),
            ("TERMINALE", "LLCERANT_VOY_EMIGRATION",     "Voyages, territoires, frontières — Migration et exil", "Distinguer émigration choisie et subie et en présenter les causes et les effets", 39),
            ("TERMINALE", "LLCERANT_VOY_EXIL",           "Voyages, territoires, frontières — Migration et exil", "Analyser l'exil, l'exil intérieur ou l'hybridité culturelle dans un récit migrant ou postcolonial", 40),
        };

        // -------------------------------------------------------------------------------------
        // 3. PREMIÈRE — LLCER ANGLAIS, MONDE CONTEMPORAIN (annexe de MENE2017287A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] AMC1 =
        {
            // Activités langagières
            ("PREMIERE", "AMC1_REVUE_PRESSE",            "Activités langagières", "Présenter une revue de presse ou un point d'information sur l'actualité du monde anglophone", 1),
            ("PREMIERE", "AMC1_EXPOSE_TRAME",            "Activités langagières", "Faire un exposé en anglais à partir de notes ou d'une trame", 2),
            ("PREMIERE", "AMC1_DEBAT_NEGOCIATION",       "Activités langagières", "Participer à un débat ou à une négociation en anglais en réagissant aux arguments d'autrui", 3),
            ("PREMIERE", "AMC1_AUDIOVISUEL",             "Activités langagières", "Comprendre un journal télévisé, un débat ou une interview aux accents variés", 4),
            ("PREMIERE", "AMC1_ENJEUX_NUANCES",          "Activités langagières", "Dégager le sens explicite et implicite d'un document pour en saisir les enjeux et les nuances", 5),
            ("PREMIERE", "AMC1_MEDIATION",               "Activités langagières", "Transmettre à un camarade les informations d'un document ou lui expliquer des données", 6),
            ("PREMIERE", "AMC1_PERIPHRASE",              "Activités langagières", "Recourir à une périphrase quand le mot précis manque pour exprimer sa pensée", 7),
            ("PREMIERE", "AMC1_VARIETES_ANGLAIS",        "Activités langagières", "Reconnaître à l'oral des variétés nationales et régionales de l'anglais", 8),

            // Méthode
            ("PREMIERE", "AMC1_POINT_DE_VUE_EXPERT",     "Méthode", "Analyser un fait d'actualité du point de vue d'un historien, d'un économiste ou d'un sociologue", 9),
            ("PREMIERE", "AMC1_CONTEXTE_HISTORIQUE",     "Méthode", "Resituer une question d'actualité anglophone dans son contexte historique", 10),
            ("PREMIERE", "AMC1_DEFINIR_NOTIONS",         "Méthode", "Définir avec précision les notions d'un champ étudié, sans technicité excessive", 11),
            ("PREMIERE", "AMC1_DOCUMENT_ICONO",          "Méthode", "Analyser un document iconographique, graphique ou carte en lien avec le texte qu'il accompagne", 12),
            ("PREMIERE", "AMC1_VALIDER_SOURCES",         "Méthode", "Vérifier une information trouvée en ligne en évaluant sa source", 13),
            ("PREMIERE", "AMC1_DOSSIER_PERSONNEL",       "Méthode", "Présenter les documents de son dossier personnel et leur lien avec les thématiques", 14),

            // Thématique « Savoirs, création, innovation »
            ("PREMIERE", "AMC1_SAV_SOCIETE_SAVOIR",      "Savoirs, création, innovation — Production et circulation des savoirs", "Présenter les acteurs qui produisent et diffusent le savoir : écoles, universités, médias, usagers", 15),
            ("PREMIERE", "AMC1_SAV_LIMITES_PARTAGE",     "Savoirs, création, innovation — Production et circulation des savoirs", "Discuter les limites du partage des savoirs : fracture numérique, limites juridiques", 16),
            ("PREMIERE", "AMC1_SAV_SYSTEMES_EDUCATIFS",  "Savoirs, création, innovation — Production et circulation des savoirs", "Comparer deux systèmes éducatifs anglophones et l'égalité des chances qu'ils offrent", 17),
            ("PREMIERE", "AMC1_SAV_DONNEES",             "Savoirs, création, innovation — Production et circulation des savoirs", "Expliquer la marchandisation des données et le rôle des algorithmes de recommandation", 18),
            ("PREMIERE", "AMC1_SAV_COURSE_INNOVATION",   "Savoirs, création, innovation — Sciences et techniques, promesses et défis", "Présenter le poids économique et géopolitique de l'innovation dans un pays anglophone", 19),
            ("PREMIERE", "AMC1_SAV_IDEE_OBJET",          "Savoirs, création, innovation — Sciences et techniques, promesses et défis", "Retracer le parcours d'une innovation, de l'idée à la fabrication et à la commercialisation", 20),
            ("PREMIERE", "AMC1_SAV_HOMME_MACHINE",       "Savoirs, création, innovation — Sciences et techniques, promesses et défis", "Débattre des promesses et des risques de l'intelligence artificielle et de l'homme augmenté", 21),
            ("PREMIERE", "AMC1_SAV_ETHIQUE_GENETIQUE",   "Savoirs, création, innovation — Sciences et techniques, promesses et défis", "Discuter une controverse éthique liée à la génétique : OGM, tests génétiques, édition du génome", 22),
            ("PREMIERE", "AMC1_SAV_TRANSITION",          "Savoirs, création, innovation — Sciences et techniques, promesses et défis", "Évaluer les bénéfices et les écueils d'une énergie propre dans un pays anglophone", 23),
            ("PREMIERE", "AMC1_SAV_CREATION_NUMERIQUE",  "Savoirs, création, innovation — Sciences et techniques, promesses et défis", "Expliquer comment le numérique transforme la création artistique et l'expérience du public", 24),
            ("PREMIERE", "AMC1_SAV_VILLE",               "Savoirs, création, innovation — Sciences et techniques, promesses et défis", "Présenter une transformation urbaine : friches industrielles, ville intelligente, télétravail", 25),

            // Thématique « Représentations »
            ("PREMIERE", "AMC1_REP_SYSTEMES_POLITIQUES", "Représentations — Faire entendre sa voix : représentation et participation", "Comparer le système parlementaire britannique et la république fédérale des États-Unis", 26),
            ("PREMIERE", "AMC1_REP_PARTICIPATION",       "Représentations — Faire entendre sa voix : représentation et participation", "Présenter une forme d'engagement citoyen : vote, référendum, syndicat, pétition, association", 27),
            ("PREMIERE", "AMC1_REP_PAYSAGE_POLITIQUE",   "Représentations — Faire entendre sa voix : représentation et participation", "Analyser l'évolution d'un paysage politique : bipartisme, populisme, participation électorale", 28),
            ("PREMIERE", "AMC1_REP_MONARCHIE",           "Représentations — Faire entendre sa voix : représentation et participation", "Expliquer le rôle de la monarchie parlementaire britannique et du Commonwealth", 29),
            ("PREMIERE", "AMC1_REP_DEMOCRATIE_NUMERIQUE","Représentations — Faire entendre sa voix : représentation et participation", "Discuter le rôle des réseaux sociaux, des lanceurs d'alerte et du cyber-militantisme en démocratie", 30),
            ("PREMIERE", "AMC1_REP_MEDIAS_POUVOIR",      "Représentations — Informer et s'informer", "Analyser le rôle des médias comme contre-pouvoir et l'évolution de la communication politique", 31),
            ("PREMIERE", "AMC1_REP_LIBERTE_PRESSE",      "Représentations — Informer et s'informer", "Comparer des conceptions de la liberté d'expression et de la presse, dont le premier amendement", 32),
            ("PREMIERE", "AMC1_REP_TRAITEMENT_MEDIATIQUE","Représentations — Informer et s'informer", "Comparer le traitement d'un même événement par deux médias anglophones", 33),
            ("PREMIERE", "AMC1_REP_MODELES_MEDIAS",      "Représentations — Informer et s'informer", "Expliquer l'évolution du modèle économique des médias traditionnels face aux agrégateurs", 34),
            ("PREMIERE", "AMC1_REP_POST_VERITE",         "Représentations — Informer et s'informer", "Repérer une fausse information ou une théorie du complot et expliquer la vérification des faits", 35),
            ("PREMIERE", "AMC1_REP_AUTOPORTRAIT",        "Représentations — Représenter le monde et se représenter", "Analyser comment une production culturelle célèbre ou critique l'image qu'une société a d'elle-même", 36),
            ("PREMIERE", "AMC1_REP_STEREOTYPES",         "Représentations — Représenter le monde et se représenter", "Déconstruire un cliché sur une société anglophone véhiculé par un film ou un roman", 37),
            ("PREMIERE", "AMC1_REP_INFOGRAPHIE",         "Représentations — Représenter le monde et se représenter", "Analyser une carte, une infographie ou un sondage comme mise en scène du réel", 38),
            ("PREMIERE", "AMC1_REP_SOFT_POWER",          "Représentations — Représenter le monde et se représenter", "Expliquer comment une ville-monde, un monument ou la monarchie sert le soft power d'un pays", 39),
        };

        // -------------------------------------------------------------------------------------
        // 4. TERMINALE — LLCER ANGLAIS, MONDE CONTEMPORAIN (annexe de MENE2017292A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] AMCT =
        {
            // Activités langagières
            ("TERMINALE", "AMCT_MEDIATION_DESACCORD",    "Activités langagières", "Gérer un débat en contexte de désaccord pour faciliter la communication", 1),
            ("TERMINALE", "AMCT_POINT_DE_VUE_EXPERT",    "Activités langagières", "Présenter l'actualité en adoptant le point de vue d'un historien, d'un économiste ou d'un politologue", 2),
            ("TERMINALE", "AMCT_TRANSPOSITION",          "Activités langagières", "Traduire ou transposer en français les idées principales d'un texte de presse anglais", 3),

            // Méthode
            ("TERMINALE", "AMCT_SYNTHESE_DOSSIER",       "Méthode", "Présenter la synthèse d'un dossier de trois ou quatre documents, dont un article de presse", 4),
            ("TERMINALE", "AMCT_PRISE_POSITION",         "Méthode", "Prolonger une synthèse par une prise de position argumentée", 5),
            ("TERMINALE", "AMCT_NUANCER_PAYS",           "Méthode", "Nuancer une analyse en tenant compte des différences entre pays du monde anglophone", 6),
            ("TERMINALE", "AMCT_DEFINIR_CONCEPTS",       "Méthode", "Définir avec précision les concepts manipulés, sans technicité excessive", 7),
            ("TERMINALE", "AMCT_DOSSIER_PRESENTATION",   "Méthode", "Présenter en 10 minutes un dossier de quatre à six documents, dont au moins un article de presse", 8),
            ("TERMINALE", "AMCT_DOSSIER_ECHANGE",        "Méthode", "Défendre la logique de son dossier dans un échange de 10 minutes avec un examinateur", 9),
            ("TERMINALE", "AMCT_DOCUMENT_DECOUVERT",     "Méthode", "Prendre librement la parole sur un document ou une question d'actualité découverts, puis converser", 10),

            // Thématique « Faire société »
            ("TERMINALE", "AMCT_SOC_ACCENTS_LANGUES",    "Faire société — Unité et pluralité", "Expliquer le rôle des accents et des politiques linguistiques dans un pays anglophone", 11),
            ("TERMINALE", "AMCT_SOC_RELIGION_LAICITE",   "Faire société — Unité et pluralité", "Comparer l'accommodement religieux fondé sur la tolérance et la laïcité française", 12),
            ("TERMINALE", "AMCT_SOC_APPARTENANCE",       "Faire société — Unité et pluralité", "Expliquer comment une fête, une commémoration ou une compétition sportive renforce l'appartenance", 13),
            ("TERMINALE", "AMCT_SOC_STATUES",            "Faire société — Unité et pluralité", "Présenter une controverse autour d'une statue ou d'une journée nationale", 14),
            ("TERMINALE", "AMCT_SOC_INTEGRATION",        "Faire société — Unité et pluralité", "Distinguer assimilation, communautarisme et multiculturalisme à partir d'un exemple", 15),
            ("TERMINALE", "AMCT_SOC_LIBERTES",           "Faire société — Libertés publiques et libertés individuelles", "Présenter une liberté publique garantie dans un pays anglophone et ses limites", 16),
            ("TERMINALE", "AMCT_SOC_SEPARATISMES",       "Faire société — Libertés publiques et libertés individuelles", "Présenter une revendication identitaire : dévolution, référendum d'autodétermination", 17),
            ("TERMINALE", "AMCT_SOC_RECONCILIATION",     "Faire société — Libertés publiques et libertés individuelles", "Expliquer les demandes d'excuses, de réparation ou de restitution des peuples autochtones", 18),
            ("TERMINALE", "AMCT_SOC_LIBERALISMES",       "Faire société — Libertés publiques et libertés individuelles", "Comparer libéralisme étatsunien et britannique et discuter le rêve américain", 19),
            ("TERMINALE", "AMCT_SOC_EGALITE_CHANCES",    "Faire société — Égalités et inégalités", "Distinguer égalité et égalité des chances à partir d'un exemple anglophone", 20),
            ("TERMINALE", "AMCT_SOC_INEGALITES",         "Faire société — Égalités et inégalités", "Analyser une inégalité d'éducation, d'emploi ou de salaire à partir de données", 21),
            ("TERMINALE", "AMCT_SOC_DISCRIMINATION_POS", "Faire société — Égalités et inégalités", "Comparer positive discrimination britannique et affirmative action américaine", 22),
            ("TERMINALE", "AMCT_SOC_SANTE_PHILANTHROPIE","Faire société — Égalités et inégalités", "Expliquer le rôle d'un système de santé, de la philanthropie ou d'une association caritative", 23),

            // Thématique « Environnements en mutation »
            ("TERMINALE", "AMCT_ENV_FRONTIER_BORDER",    "Environnements en mutation — Frontière et espace", "Expliquer la Manifest Destiny et la différence entre frontier et border aux États-Unis", 24),
            ("TERMINALE", "AMCT_ENV_INSULARITE",         "Environnements en mutation — Frontière et espace", "Discuter les mythes et réalités de l'insularité britannique et de sa frontière avec l'Union européenne", 25),
            ("TERMINALE", "AMCT_ENV_MIGRATIONS",         "Environnements en mutation — Frontière et espace", "Présenter un débat sur l'immigration ou un mouvement de population dans le monde anglophone", 26),
            ("TERMINALE", "AMCT_ENV_ESPACE_TRANSPORTS",  "Environnements en mutation — Frontière et espace", "Expliquer le rôle des transports ou de la conquête spatiale dans la maîtrise d'un territoire", 27),
            ("TERMINALE", "AMCT_ENV_PRESERVATION",       "Environnements en mutation — De la protection de la nature à la transition écologique", "Distinguer préservationnistes et conservationnistes dans l'histoire environnementale américaine", 28),
            ("TERMINALE", "AMCT_ENV_AUTOCHTONES",        "Environnements en mutation — De la protection de la nature à la transition écologique", "Présenter une mobilisation autochtone pour protéger un territoire ou une ressource", 29),
            ("TERMINALE", "AMCT_ENV_CRISE_CLIMATIQUE",   "Environnements en mutation — De la protection de la nature à la transition écologique", "Rendre compte d'une crise climatique dans un pays anglophone et des réponses apportées", 30),
            ("TERMINALE", "AMCT_ENV_ECHELLES",           "Environnements en mutation — De la protection de la nature à la transition écologique", "Expliquer les contradictions entre l'échelle fédérale et celle des États en matière d'environnement", 31),
            ("TERMINALE", "AMCT_ENV_GENTRIFICATION",     "Environnements en mutation — Repenser la ville", "Expliquer les causes et les mécanismes de la gentrification dans une ville anglophone", 32),
            ("TERMINALE", "AMCT_ENV_INEGALITES_URBAINES","Environnements en mutation — Repenser la ville", "Décrire une inégalité urbaine : gated community, ghettoïsation, township, bidonville", 33),
            ("TERMINALE", "AMCT_ENV_VILLE_DURABLE",      "Environnements en mutation — Repenser la ville", "Présenter la reconversion d'une ville post-industrielle ou un projet de ville durable", 34),

            // Thématique « Relation au monde »
            ("TERMINALE", "AMCT_MONDE_HARD_SOFT_POWER",  "Relation au monde — Puissance et influence", "Distinguer hard power et soft power à partir d'un exemple du monde anglophone", 35),
            ("TERMINALE", "AMCT_MONDE_INFLUENCE_CULTURE","Relation au monde — Puissance et influence", "Montrer l'influence mondiale des médias, du cinéma ou des universités anglophones", 36),
            ("TERMINALE", "AMCT_MONDE_ECONOMIE",         "Relation au monde — Puissance et influence", "Expliquer le poids du dollar, des places financières ou des sanctions économiques", 37),
            ("TERMINALE", "AMCT_MONDE_DEFENSE",          "Relation au monde — Puissance et influence", "Présenter une alliance ou un accord de défense et de renseignement : Otan, Five Eyes", 38),
            ("TERMINALE", "AMCT_MONDE_PUISSANCE_PERCUE", "Relation au monde — Puissance et influence", "Confronter la puissance effective d'un pays à la représentation qu'il a de sa puissance", 39),
            ("TERMINALE", "AMCT_MONDE_RIVALITES",        "Relation au monde — Rivalités et interdépendances", "Analyser une rivalité ou une interdépendance : États-Unis et Chine, Royaume-Uni et Union européenne", 40),
            ("TERMINALE", "AMCT_MONDE_ORDRE_LIBERAL",    "Relation au monde — Rivalités et interdépendances", "Discuter les critiques du libéralisme économique et de la démocratie libérale", 41),
            ("TERMINALE", "AMCT_MONDE_ANGLAIS_MONDIAL",  "Relation au monde — Héritage commun et diversité", "Présenter la place de l'anglais dans le monde, ses variantes et les pidgins", 42),
            ("TERMINALE", "AMCT_MONDE_POST_IMPERIAL",    "Relation au monde — Héritage commun et diversité", "Expliquer un enjeu mémoriel du monde post-impérial : esclavage, colonisation, peuples autochtones", 43),
        };
    }
}
