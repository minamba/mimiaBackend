namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// L'enseignement de spécialité « Humanités, littérature et philosophie » (HLP) de la voie générale,
    /// première et terminale, tel que les textes officiels le définissent — provenance détaillée en tête.
    /// Spécialité distincte du tronc commun de philosophie de terminale (référentiel séparé, non recopié ici).
    /// </summary>
    public static class ReferentielHlp
    {
        // =====================================================================================
        // RÉFÉRENTIEL DE LA SPÉCIALITÉ HLP (humanités, littérature et philosophie), VOIE GÉNÉRALE
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis le texte
        // officiel téléchargé et lu le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\hlp\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau    | Intitulé exact (page de titre de l'annexe)                              |
        // |---------|-----------|-------------------------------------------------------------------------|
        // | HLP1    | PREMIERE  | Programme d'humanités, littérature et philosophie de première générale  |
        // | HLPT    | TERMINALE | Programme d'humanités, littérature et philosophie de terminale générale |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901578A,
        //   BO spécial n° 1 du 22 janvier 2019. Article 2 : « Les dispositions du présent arrêté entrent en
        //   vigueur à la rentrée scolaire 2019. » Annexe unique, lue en entier (préambule, programme, bibliographie).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901578A.htm
        //   Annexe (BO)      : https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/00/2/spe578_annexe_1063002.pdf
        //   Annexe (éduscol) : https://eduscol.education.gouv.fr/sites/default/files/document/spe578annexe1063002pdf-83919.pdf
        //                      (fichier identique octet pour octet à la copie du BO)
        //   Légifrance       : https://www.legifrance.gouv.fr/loda/id/JORFTEXT000038029439/
        //
        // TERMINALE : arrêté du 19-7-2019, J.O. du 23-7-2019, NOR MENE1921255A,
        //   BO spécial n° 8 du 25 juillet 2019. Article 2 : « Les dispositions du présent arrêté entrent en
        //   vigueur à la rentrée scolaire 2020. » Annexe unique, lue en entier.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921255A.htm
        //   Annexe (BO)      : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/92/0/spe255_annexe_1158920.pdf
        //   Annexe (éduscol) : https://eduscol.education.gouv.fr/sites/default/files/document/spe255annexe1158920pdf-83922.pdf
        //                      (fichier identique octet pour octet à la copie du BO)
        //   Légifrance       : https://www.legifrance.gouv.fr/loda/id/JORFTEXT000038799958/
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 :
        //   - la page éduscol « Programmes et ressources en humanités, littérature et philosophie - voie G »
        //     (https://eduscol.education.gouv.fr/5805/programmes-et-ressources-en-humanites-litterature-et-philosophie-voie-g,
        //     datée « février 2026 ») liste sous « Programmes en vigueur » ces deux textes et eux seuls
        //     (BO spécial n° 1 du 22-1-2019 et BO spécial n° 8 du 25-7-2019), avec les mêmes PDF ;
        //   - Légifrance, consulté le 14/09/2026 (« en vigueur au 14 septembre 2026 ») : arrêté de première en
        //     version unique « en vigueur depuis le 02/09/2019 », aucun texte modificatif ni abrogation ;
        //     arrêté de terminale sans mention de modification ni d'abrogation ;
        //   - recherche BO / éduscol / Légifrance 2019-2026 : aucun arrêté modifiant l'un ou l'autre programme.
        //     Les seuls changements touchent l'ÉPREUVE (voir hlp.prof.md).
        //
        // ÉCARTS ET POINTS D'ATTENTION
        //   - Tableau récapitulatif du préambule : « Éducation, transmission et émancipation » ; titre du chapitre
        //     dans le corps du programme : « Éducation, transmission, émancipation ». Le Domaine reprend le corps.
        //   - La note de service de 2020 (MENE2001793N) écrivait « Les limites de l'humain » ; le programme dit
        //     « L'humain et ses limites ». Le Domaine reprend le programme.
        //   - « Écrit d'appropriation » : absent des deux annexes HLP (notion du programme de français) ; non repris.
        //   - Les lignes « Méthode » de première reprennent le préambule du programme et la ressource éduscol
        //     « Attendus des épreuves - éléments d'évaluation » (nov. 2019) ; celles de terminale reprennent en plus
        //     la définition d'épreuve (MENE2001793N, version consolidée d'août 2024).
        //   - Les œuvres de la bibliographie sont « indicatives » et « sans caractère prescriptif » : aucune ligne
        //     n'exige la connaissance d'une œuvre précise.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   HLP1 46 (Méthode 7 · Parole 20 · Représentations du monde 19)
        //   HLPT 51 (Méthode 8 · Recherche de soi 23 · Humanité en question 20) · total 97
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — HUMANITÉS, LITTÉRATURE ET PHILOSOPHIE (annexe de MENE1901578A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HLP1 =
        {
            // Méthode — préambule du programme et attendus éduscol
            ("PREMIERE", "HLP1_METHODE_LECTURE_INTERPRETATION", "Méthode", "Lire et interpréter un texte littéraire ou philosophique en élucidant son sens avec précision", 1),
            ("PREMIERE", "HLP1_METHODE_LANGUE_LEXIQUE",         "Méthode", "Justifier une lecture par l'attention à la langue, au lexique et aux notions du texte", 2),
            ("PREMIERE", "HLP1_METHODE_QUESTION_REFLEXION",     "Méthode", "Élucider une question de réflexion et y répondre de façon précise, ordonnée et argumentée", 3),
            ("PREMIERE", "HLP1_METHODE_CONNAISSANCES",          "Méthode", "Mobiliser à bon escient textes et connaissances sans les réciter artificiellement", 4),
            ("PREMIERE", "HLP1_METHODE_ARGUMENTATION_ORALE",    "Méthode", "Argumenter à l'oral en explicitant son raisonnement pour convaincre, et réviser sa position si besoin", 5),
            ("PREMIERE", "HLP1_METHODE_REGARD_CROISE",          "Méthode", "Aborder une même question par une approche littéraire puis philosophique et relier les deux", 6),
            ("PREMIERE", "HLP1_METHODE_PERSPECTIVE_HISTORIQUE", "Méthode", "Comparer une problématique de la période de référence à des textes plus anciens ou plus récents", 7),

            // Semestre 1 : Les pouvoirs de la parole — période de référence : de l'Antiquité à l'âge classique
            ("PREMIERE", "HLP1_PAROLE_ROLE_SOCIETES",           "Semestre 1 — Les pouvoirs de la parole", "Expliquer le rôle du langage et de la parole dans les sociétés humaines, de l'Antiquité à l'âge classique", 8),
            ("PREMIERE", "HLP1_PAROLE_PROCEDES_EFFETS",         "Semestre 1 — Les pouvoirs de la parole", "Repérer, apprécier et analyser les procédés et les effets de l'art de la parole dans un discours", 9),
            ("PREMIERE", "HLP1_PAROLE_MISE_EN_OEUVRE",          "Semestre 1 — Les pouvoirs de la parole", "Mettre en œuvre soi-même ces procédés dans une expression écrite ou orale bien construite", 10),
            ("PREMIERE", "HLP1_PAROLE_CONFLITS_VALEURS",        "Semestre 1 — Les pouvoirs de la parole", "Mesurer les questions et conflits de valeurs que l'art de la parole a suscités", 11),

            // L'art de la parole
            ("PREMIERE", "HLP1_ART_RHETORIQUE",                 "Semestre 1 — L'art de la parole", "Définir la rhétorique comme art réglé de la parole et de l'éloquence et présenter ses divisions classiques", 12),
            ("PREMIERE", "HLP1_ART_GENRES_PARTIES_DISCOURS",    "Semestre 1 — L'art de la parole", "Identifier les genres de discours et les parties du discours dans un texte oratoire", 13),
            ("PREMIERE", "HLP1_ART_ORATEUR",                    "Semestre 1 — L'art de la parole", "Présenter les qualités et la culture attendues de l'orateur", 14),
            ("PREMIERE", "HLP1_ART_HERITAGE_CLASSIQUE",         "Semestre 1 — L'art de la parole", "Montrer l'héritage de la rhétorique antique dans l'esthétique de l'âge classique, « l'âge de l'éloquence »", 15),
            ("PREMIERE", "HLP1_ART_SITUATIONS_CONTEXTES",       "Semestre 1 — L'art de la parole", "Relier une prise de parole (assemblée, procès, cérémonie) à sa forme littéraire et à son contexte historique", 16),
            ("PREMIERE", "HLP1_ART_PAROLE_ECRITURE",            "Semestre 1 — L'art de la parole", "Distinguer parole et écriture et expliquer leurs relations", 17),

            // L'autorité de la parole
            ("PREMIERE", "HLP1_AUTORITE_POETE_MUSE",            "Semestre 1 — L'autorité de la parole", "Expliquer pourquoi le poète grec invoquant la Muse apparaît comme maître de vérité et garant de la mémoire", 18),
            ("PREMIERE", "HLP1_AUTORITE_PAROLES_AUTORISEES",    "Semestre 1 — L'autorité de la parole", "Distinguer paroles politique, religieuse, savante et didactique comme formes de parole autorisée", 19),
            ("PREMIERE", "HLP1_AUTORITE_STRATEGIES",            "Semestre 1 — L'autorité de la parole", "Analyser comment une parole établit son autorité : principes et valeurs invoqués, stratégies choisies", 20),
            ("PREMIERE", "HLP1_AUTORITE_REGLES_PAROLE_ACTION",  "Semestre 1 — L'autorité de la parole", "Réfléchir aux règles de la parole publique, aux codes sociaux de la communication et au lien parole-action", 21),

            // Les séductions de la parole
            ("PREMIERE", "HLP1_SEDUCTION_PLAIRE_EMOUVOIR",      "Semestre 1 — Les séductions de la parole", "Analyser le pouvoir de la parole de plaire, de séduire et d'émouvoir dans un texte poétique ou oratoire", 22),
            ("PREMIERE", "HLP1_SEDUCTION_POESIE_SCENE",         "Semestre 1 — Les séductions de la parole", "Étudier la parole poétique et la mise en scène de la parole dans ses relations avec les autres arts", 23),
            ("PREMIERE", "HLP1_SEDUCTION_FICTION",              "Semestre 1 — Les séductions de la parole", "Expliquer l'usage des procédés de fiction (fable, parabole, allégorie) pour persuader ou enseigner", 24),
            ("PREMIERE", "HLP1_SEDUCTION_SINCERITE_EMPRISE",    "Semestre 1 — Les séductions de la parole", "Distinguer véridique, sincère et authentique, et reconnaître la parole séductrice et ses procédés d'emprise", 25),
            ("PREMIERE", "HLP1_SEDUCTION_DECLARATION_AMOUR",    "Semestre 1 — Les séductions de la parole", "Analyser l'amour et ses déclarations comme usage séducteur de la parole", 26),
            ("PREMIERE", "HLP1_SEDUCTION_PROCES_SOPHISTIQUE",   "Semestre 1 — Les séductions de la parole", "Exposer la polémique antique : satire de l'orateur et du philosophe, procès du poète en sophistique et mensonge", 27),

            // Semestre 2 : Les représentations du monde — période de référence : Renaissance, âge classique, Lumières
            ("PREMIERE", "HLP1_MONDE_PERIODE_MUTATIONS",        "Semestre 2 — Les représentations du monde", "Situer la période XVe-XVIIIe siècle : humanisme, « nouveaux mondes », révolutions scientifiques et techniques", 28),
            ("PREMIERE", "HLP1_MONDE_VARIATION_REPRESENTATIONS","Semestre 2 — Les représentations du monde", "Montrer comment les représentations de la terre habitée et du cosmos varient et se transforment", 29),

            // Découverte du monde et pluralité des cultures
            ("PREMIERE", "HLP1_DECOUVERTE_NOUVELLES_TERRES",    "Semestre 2 — Découverte du monde et pluralité des cultures", "Expliquer les effets de la découverte de nouvelles terres sur la culture européenne", 30),
            ("PREMIERE", "HLP1_DECOUVERTE_REVOLUTION_ASTRO",    "Semestre 2 — Découverte du monde et pluralité des cultures", "Relier révolution astronomique et instruments d'optique au changement des dimensions du monde", 31),
            ("PREMIERE", "HLP1_DECOUVERTE_CRISE_CONSCIENCE",    "Semestre 2 — Découverte du monde et pluralité des cultures", "Montrer comment guerres de religion et violence des conquêtes suscitent un regard critique sur l'Europe", 32),
            ("PREMIERE", "HLP1_DECOUVERTE_MONDE_INFINI",        "Semestre 2 — Découverte du monde et pluralité des cultures", "Expliquer le passage du monde clos à l'espace infini et ses effets sur la place de l'homme dans l'univers", 33),
            ("PREMIERE", "HLP1_DECOUVERTE_GENRES_RECITS",       "Semestre 2 — Découverte du monde et pluralité des cultures", "Analyser récit de voyage, fiction d'île déserte ou regard étranger comme moyens de critique sociale", 34),
            ("PREMIERE", "HLP1_DECOUVERTE_AUTRES_CULTURES",     "Semestre 2 — Découverte du monde et pluralité des cultures", "Confronter la rencontre des cultures lointaines au XVIe-XVIIIe siècle et les interrogations d'aujourd'hui", 35),

            // Décrire, figurer, imaginer
            ("PREMIERE", "HLP1_FIGURER_LIVRE_IMPRIME",          "Semestre 2 — Décrire, figurer, imaginer", "Présenter le rôle du livre imprimé, de son illustration et de sa diffusion dans la représentation du monde", 36),
            ("PREMIERE", "HLP1_FIGURER_INVENTAIRES",            "Semestre 2 — Décrire, figurer, imaginer", "Expliquer le goût des inventaires du monde : histoire naturelle, atlas, cartographie, idéal encyclopédique", 37),
            ("PREMIERE", "HLP1_FIGURER_PERSPECTIVE",            "Semestre 2 — Décrire, figurer, imaginer", "Expliquer l'invention de la perspective artificielle et les enjeux de la représentation dans les arts visuels", 38),
            ("PREMIERE", "HLP1_FIGURER_IMITATION_FORMES",       "Semestre 2 — Décrire, figurer, imaginer", "Discuter la problématique de l'imitation en poésie et en littérature et l'évolution des formes", 39),
            ("PREMIERE", "HLP1_FIGURER_IMAGINATION_SAVOIR",     "Semestre 2 — Décrire, figurer, imaginer", "Évaluer le rôle de l'imagination et de la fiction dans le développement des savoirs sur la nature et l'homme", 40),

            // L'homme et l'animal
            ("PREMIERE", "HLP1_ANIMAL_FRONTIERE",               "Semestre 2 — L'homme et l'animal", "Montrer comment, de Montaigne à Buffon, la frontière entre l'homme et l'animal devient discutable", 41),
            ("PREMIERE", "HLP1_ANIMAL_MACHINE",                 "Semestre 2 — L'homme et l'animal", "Exposer les arguments de la querelle de « l'animal-machine »", 42),
            ("PREMIERE", "HLP1_ANIMAL_INTELLIGENCE",            "Semestre 2 — L'homme et l'animal", "Discuter l'intelligence animale et la communication entre animaux à partir de textes de la période", 43),
            ("PREMIERE", "HLP1_ANIMAL_FABULISTE_NATURALISTE",   "Semestre 2 — L'homme et l'animal", "Comparer la manière dont fabuliste et naturaliste explorent ressemblances et différences entre hommes et bêtes", 44),
            ("PREMIERE", "HLP1_ANIMAL_CONNAISSANCE_HOMME",      "Semestre 2 — L'homme et l'animal", "Expliquer ce que la connaissance des autres espèces apporte à la connaissance de l'homme", 45),
            ("PREMIERE", "HLP1_ANIMAL_QUESTIONS_VIVES",         "Semestre 2 — L'homme et l'animal", "Argumenter sur une question vive : exploitation animale, droits des animaux, « cultures animales »", 46),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE — HUMANITÉS, LITTÉRATURE ET PHILOSOPHIE (annexe de MENE1921255A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HLPT =
        {
            // Méthode — préambule, définition d'épreuve (MENE2001793N consolidée) et attendus éduscol
            ("TERMINALE", "HLPT_METHODE_INTERPRETATION",        "Méthode", "Exposer la compréhension et l'analyse d'un enjeu majeur d'un texte (interprétation littéraire ou philosophique)", 1),
            ("TERMINALE", "HLPT_METHODE_ESSAI",                 "Méthode", "Rédiger un essai : réponse étayée, personnelle et ordonnée à une question soulevée par le texte", 2),
            ("TERMINALE", "HLPT_METHODE_ESSAI_REFERENCES",      "Méthode", "Appuyer sa réflexion sur des références et des exemples précis tirés des œuvres et des textes", 3),
            ("TERMINALE", "HLPT_METHODE_APPROCHE_INDIQUEE",     "Méthode", "Adopter l'approche littéraire ou philosophique qu'indique explicitement l'intitulé de la question", 4),
            ("TERMINALE", "HLPT_METHODE_NOTIONS_PREMIERE",      "Méthode", "Mobiliser les notions de première (parole, représentations du monde) au service d'un sujet de terminale", 5),
            ("TERMINALE", "HLPT_METHODE_ARGUMENTATION_ORALE",   "Méthode", "Répondre à l'oral en 10 minutes à une question de littérature ou de philosophie, puis soutenir un entretien", 6),
            ("TERMINALE", "HLPT_METHODE_PERSPECTIVE",           "Méthode", "Comparer une problématique contemporaine à des problématiques plus anciennes", 7),
            ("TERMINALE", "HLPT_METHODE_LANGUE",                "Méthode", "Formuler une pensée nuancée et précise dans une langue correcte, au lexique et à la syntaxe maîtrisés", 8),

            // Semestre 1 : La recherche de soi — période de référence : du romantisme au XXe siècle
            ("TERMINALE", "HLPT_SOI_PERIODE",                   "Semestre 1 — La recherche de soi", "Relier les mutations du romantisme au XXe siècle aux nouveaux rapports entre individu et société", 9),

            // Éducation, transmission, émancipation
            ("TERMINALE", "HLPT_EDUC_RUPTURE_LUMIERES",         "Semestre 1 — Éducation, transmission, émancipation", "Expliquer la rupture des Lumières : primauté des choses sur les mots et éducation centrée sur l'utile", 10),
            ("TERMINALE", "HLPT_EDUC_ROUSSEAU_ENFANCE",         "Semestre 1 — Éducation, transmission, émancipation", "Présenter l'attention nouvelle à la pensée de l'enfant (Rousseau) et sa postérité dans l'éducation nouvelle", 11),
            ("TERMINALE", "HLPT_EDUC_INSTRUCTION_PUBLIQUE",     "Semestre 1 — Éducation, transmission, émancipation", "Expliquer pourquoi, dans la lignée de Condorcet, l'instruction des deux sexes devient clé de la démocratie", 12),
            ("TERMINALE", "HLPT_EDUC_EMANCIPATION_SOCIALE",     "Semestre 1 — Éducation, transmission, émancipation", "Exposer les conditions sociales et politiques de l'émancipation selon les penseurs révolutionnaires", 13),
            ("TERMINALE", "HLPT_EDUC_RECITS_ECOLIERS",          "Semestre 1 — Éducation, transmission, émancipation", "Analyser un récit de souvenirs d'écolier : ce que l'individu a reçu et ce avec quoi il a rompu", 14),
            ("TERMINALE", "HLPT_EDUC_AGES_ADULTE",              "Semestre 1 — Éducation, transmission, émancipation", "Réfléchir aux âges de la vie et à ce que veut dire être adulte", 15),
            ("TERMINALE", "HLPT_EDUC_FAMILLE_ECOLE_SOCIETE",    "Semestre 1 — Éducation, transmission, émancipation", "Discuter les parts de la famille, de l'école et de la société, et la liberté face aux institutions", 16),
            ("TERMINALE", "HLPT_EDUC_JUSTICE_EQUITE",           "Semestre 1 — Éducation, transmission, émancipation", "Argumenter sur la justice sociale et l'équité au sein d'un système éducatif moderne", 17),

            // Les expressions de la sensibilité
            ("TERMINALE", "HLPT_SENSIB_DROITS",                 "Semestre 1 — Les expressions de la sensibilité", "Montrer comment Diderot, Rousseau et Goethe affirment les droits de la sensibilité et ouvrent aux romantismes", 18),
            ("TERMINALE", "HLPT_SENSIB_RESTITUTION",            "Semestre 1 — Les expressions de la sensibilité", "Analyser comment un texte restitue perceptions subjectives, passions et pensées (direct, indirect, symbolique)", 19),
            ("TERMINALE", "HLPT_SENSIB_REALISME",               "Semestre 1 — Les expressions de la sensibilité", "Relier l'expression de la sensibilité au réalisme, au naturalisme et aux sociétés industrielles", 20),
            ("TERMINALE", "HLPT_SENSIB_CONSCIENCE_CORPS",       "Semestre 1 — Les expressions de la sensibilité", "Présenter l'exploration philosophique des données de la conscience, du corps vécu et du flux du vécu", 21),
            ("TERMINALE", "HLPT_SENSIB_SENSIBILITE_INTELLIGENCE","Semestre 1 — Les expressions de la sensibilité", "Discuter les relations entre sensibilité et intelligence et la formation des sentiments moraux", 22),
            ("TERMINALE", "HLPT_SENSIB_EMOTION_ESTHETIQUE",     "Semestre 1 — Les expressions de la sensibilité", "Expliquer l'émotion esthétique, la sacralisation de l'art et de l'artiste et les liens art-spiritualité", 23),
            ("TERMINALE", "HLPT_SENSIB_VIE_INTERIEURE",         "Semestre 1 — Les expressions de la sensibilité", "Caractériser la vie intérieure d'un personnage de fiction et la façon dont un événement l'affecte", 24),
            ("TERMINALE", "HLPT_SENSIB_LANGAGE_COMMUN",         "Semestre 1 — Les expressions de la sensibilité", "Problématiser le passage de l'expérience privée au langage commun : donnons-nous le même sens aux mots ?", 25),

            // Les métamorphoses du moi
            ("TERMINALE", "HLPT_MOI_DEFINITION",                "Semestre 1 — Les métamorphoses du moi", "Interroger ce que désigne le « moi » : a-t-il une réalité nette et stable, une unité, une identité ?", 26),
            ("TERMINALE", "HLPT_MOI_CONNAISSANCE",              "Semestre 1 — Les métamorphoses du moi", "Discuter qui connaît le mieux le moi et comment le décrire, de saint Augustin au sujet moderne", 27),
            ("TERMINALE", "HLPT_MOI_SOCIETE_REGARD",            "Semestre 1 — Les métamorphoses du moi", "Évaluer la part de la société et du regard des autres dans la définition du moi", 28),
            ("TERMINALE", "HLPT_MOI_ACTIONS_PENSEES",           "Semestre 1 — Les métamorphoses du moi", "Examiner si toutes mes actions et pensées émanent de « moi » au même degré", 29),
            ("TERMINALE", "HLPT_MOI_FIGURES_SUBJECTIVITE",      "Semestre 1 — Les métamorphoses du moi", "Identifier des figures de la subjectivité du « long XIXe siècle » (1789-1914) dans une œuvre", 30),
            ("TERMINALE", "HLPT_MOI_DECHIREMENTS_PSYCHANALYSE", "Semestre 1 — Les métamorphoses du moi", "Montrer comment œuvres et psychanalyse mettent en scène les déchirements internes de l'individu moderne", 31),

            // Semestre 2 : L'Humanité en question — période de référence : période contemporaine (XXe-XXIe siècles)
            ("TERMINALE", "HLPT_HUMANITE_PERIODE",              "Semestre 2 — L'Humanité en question", "Situer les interrogations et expériences caractéristiques du monde contemporain (XXe-XXIe siècles)", 32),

            // Création, continuités et ruptures
            ("TERMINALE", "HLPT_CREATION_AVANT_GARDES",         "Semestre 2 — Création, continuités et ruptures", "Présenter les avant-gardes (expressionnisme, futurisme, Dada, surréalisme) et le rôle de leurs manifestes", 33),
            ("TERMINALE", "HLPT_CREATION_RUPTURES_PENSEE",      "Semestre 2 — Création, continuités et ruptures", "Expliquer la volonté de rupture en philosophie (phénoménologie, empirisme logique, marxismes)", 34),
            ("TERMINALE", "HLPT_CREATION_CRISE_RATIONALITE",    "Semestre 2 — Création, continuités et ruptures", "Relier les bouleversements des savoirs du XXe siècle à l'idée d'une crise de la rationalité", 35),
            ("TERMINALE", "HLPT_CREATION_TECHNIQUES_ANTICIP",   "Semestre 2 — Création, continuités et ruptures", "Montrer comment techniques, radio et cinéma transforment la culture et nourrissent l'anticipation", 36),
            ("TERMINALE", "HLPT_CREATION_MODERNISME",           "Semestre 2 — Création, continuités et ruptures", "Identifier l'héritage du modernisme : éclatement des formes narratives, expérimentations, limites de la représentation", 37),
            ("TERMINALE", "HLPT_CREATION_FIN_OU_REPRISE",       "Semestre 2 — Création, continuités et ruptures", "Discuter s'il existe des ruptures radicales ou si l'ancien subsiste à côté du nouveau ou à travers lui", 38),
            ("TERMINALE", "HLPT_CREATION_QUERELLES",            "Semestre 2 — Création, continuités et ruptures", "Comparer les débats sur la création contemporaine à des querelles d'autres époques", 39),

            // Histoire et violence
            ("TERMINALE", "HLPT_VIOLENCE_XXE_SIECLE",           "Semestre 2 — Histoire et violence", "Expliquer en quoi les destructions et massacres du XXe siècle changent notre vision de l'Humanité et de l'histoire", 40),
            ("TERMINALE", "HLPT_VIOLENCE_DOMINATION_DIGNITE",   "Semestre 2 — Histoire et violence", "Présenter la revendication de dignité et d'indépendance des peuples soumis à une domination", 41),
            ("TERMINALE", "HLPT_VIOLENCE_PROGRES_HUMANISTE",    "Semestre 2 — Histoire et violence", "Discuter l'examen critique de la confiance « humaniste » en un progrès continu de la civilisation", 42),
            ("TERMINALE", "HLPT_VIOLENCE_IRREDUCTIBLE_DROIT",   "Semestre 2 — Histoire et violence", "Argumenter : la violence est-elle irréductible, et quel droit peut la limiter durablement ?", 43),
            ("TERMINALE", "HLPT_VIOLENCE_DISTINGUER_FORMES",    "Semestre 2 — Histoire et violence", "Distinguer guerre de conquête et de libération, régime oppressif et totalitaire, violences sociales diffuses", 44),
            ("TERMINALE", "HLPT_VIOLENCE_TEMOIGNAGE",           "Semestre 2 — Histoire et violence", "Analyser les pouvoirs de la littérature face à la violence : témoignage, engagement, dénonciation", 45),
            ("TERMINALE", "HLPT_VIOLENCE_DIRE_INHUMAIN",        "Semestre 2 — Histoire et violence", "Montrer comment l'écriture exprime la réalité de la violence jusque dans sa dimension d'inhumanité", 46),

            // L'humain et ses limites
            ("TERMINALE", "HLPT_LIMITES_JUSQUOU",               "Semestre 2 — L'humain et ses limites", "Discuter « Jusqu'où peut-on aller ? » face à l'extension technique des capacités humaines (numérique, génétique, IA)", 47),
            ("TERMINALE", "HLPT_LIMITES_ENVERS_PROGRES",        "Semestre 2 — L'humain et ses limites", "Exposer l'envers du progrès technique : dépendances, moyens de destruction, déséquilibres, monde inhabitable", 48),
            ("TERMINALE", "HLPT_LIMITES_IMAGINAIRE",            "Semestre 2 — L'humain et ses limites", "Analyser comment dystopies, mondes post-humains et univers parallèles expriment les inquiétudes contemporaines", 49),
            ("TERMINALE", "HLPT_LIMITES_HOMME_AUGMENTE",        "Semestre 2 — L'humain et ses limites", "Argumenter sur l'homme « réparé » ou « augmenté », la définition de l'humain et la vie humaine désirable", 50),
            ("TERMINALE", "HLPT_LIMITES_FINITUDE_NATURE",       "Semestre 2 — L'humain et ses limites", "Réfléchir à la finitude, à l'équilibre exploitation-conservation de la nature et à la sociabilité numérique", 51),
        };
    }
}
