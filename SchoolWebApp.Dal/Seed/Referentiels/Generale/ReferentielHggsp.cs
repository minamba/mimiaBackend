namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// L'enseignement de spécialité « histoire-géographie, géopolitique et sciences politiques » (HGGSP)
    /// de la voie générale, première et terminale, tel que les textes officiels le définissent —
    /// provenance détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielHggsp
    {
        // =====================================================================================
        // RÉFÉRENTIEL DE LA SPÉCIALITÉ HGGSP (voie générale)
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis le texte
        // officiel téléchargé et lu le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\hggsp\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau    | Intitulé exact (page de titre de l'annexe)                                          |
        // |---------|-----------|-------------------------------------------------------------------------------------|
        // | HGGSP1  | PREMIERE  | Programme d'histoire-géographie, géopolitique et sciences politiques de première générale |
        // | HGGSPT  | TERMINALE | Programme d'histoire-géographie, géopolitique et sciences politiques de terminale générale |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. n° 0017 du 20-1-2019, NOR MENE1901576A,
        //   BO spécial n° 1 du 22 janvier 2019. Article 2 : « Les dispositions du présent arrêté entrent en
        //   vigueur à la rentrée scolaire 2019. » Annexe unique, lue en entier (128 heures).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901576A.htm
        //   Annexe          : https://eduscol.education.gouv.fr/sites/default/files/document/spe576annexe1062925pdf-83244.pdf
        //   Légifrance      : https://www.legifrance.gouv.fr/loda/id/JORFTEXT000038029429/
        //
        // TERMINALE : arrêté du 19-7-2019, J.O. n° 0169 du 23-7-2019, NOR MENE1921254A,
        //   BO spécial n° 8 du 25 juillet 2019. Article 2 : « Les dispositions du présent arrêté entrent en
        //   vigueur à la rentrée scolaire 2020. » Annexe unique, lue en entier (six thèmes de 26-28 heures).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921254A.htm
        //   Annexe          : https://eduscol.education.gouv.fr/sites/default/files/document/spe254annexe1159180pdf-83247.pdf
        //   Légifrance      : https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000038799953
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 :
        //   - La page éduscol « Programmes et ressources en histoire-géographie, géopolitique et sciences
        //     politiques - voie G » (https://eduscol.education.gouv.fr/5802/..., datée « mars 2026 ») liste sous
        //     « Programmes en vigueur » ces deux programmes, et eux seuls (BO spécial n° 1 du 22-1-2019 et
        //     BO spécial n° 8 du 25-7-2019).
        //   - Légifrance, arrêté de première « en vigueur au 14 septembre 2026 » : les trois articles sont en
        //     « version en vigueur depuis le 02/09/2019 », aucune modification. Arrêté de terminale : aucune
        //     version modifiée affichée.
        //   - Aucun arrêté modificatif trouvé (recherche BO/éduscol/Légifrance 2019-2026 le 14/09/2026).
        //     Le changement de 2025 touche l'ÉPREUVE, pas le programme : note de service du 27-8-2025,
        //     NOR MENE2521923N (BO n° 33 du 4-9-2025), rotation annuelle des thèmes évaluables (voir hggsp.prof.md).
        //
        // ÉCARTS / TEXTES VOISINS
        //   - Outre-mer (terminale seulement) : arrêté du 15-7-2021, NOR MENE2121062A (BO n° 30 du 29-7-2021),
        //     annexe 3. Ce sont des « propositions de contextualisation » qui « ne peuvent se substituer » aux axes
        //     et jalons : le référentiel national reste valable dans les Drom. Seul ajout : pour la Martinique,
        //     exemple des habitations-sucreries en complément du jalon sur le tourisme culturel.
        //   - MENE2610780N (21-4-2026) : adaptation du programme d'histoire-géographie du TRONC COMMUN pour le
        //     BFI allemand. Ne concerne pas la spécialité HGGSP.
        //   - Les jalons sont des « exemples » problématisés dont « le professeur apprécie le degré
        //     d'approfondissement » : chaque jalon est ici une ligne, pour que le tuteur puisse le vérifier.
        //   - Les lignes « Capacités et méthodes » de première viennent du préambule du programme. En terminale,
        //     elles croisent le préambule et les attendus de la note de service MENE2521923N (dissertation, étude
        //     critique, production graphique valorisée, oral de contrôle).
        //   - Première : les intitulés du texte portent les thèmes 1 à 5 plus une Introduction (4-5 heures) sur les
        //     quatre approches disciplinaires ; elle a son propre Domaine.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   HGGSP1 57 (Introduction 4 · Démocratie 10 · Puissances 10 · Frontières 9 · Information 11 ·
        //              États et religions 10 · Méthodes 3)
        //   HGGSPT 62 (Espaces de conquête 9 · Guerre et paix 10 · Histoire et mémoires 9 · Patrimoine 11 ·
        //              Environnement 8 · Connaissance 9 · Méthodes 6)
        //   Total 119
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE GÉNÉRALE — « Acquérir des clefs de compréhension du monde contemporain » (MENE1901576A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HGGSP1 =
        {
            // Introduction (4-5 heures) : les quatre approches
            ("PREMIERE", "HGGSP1_INTRO_HISTOIRE",            "Introduction — Les disciplines", "Distinguer la trace, l'archive, le témoignage et le récit dans le travail de l'historien", 1),
            ("PREMIERE", "HGGSP1_INTRO_GEOGRAPHIE",          "Introduction — Les disciplines", "Distinguer espace et territoire et mener une analyse multiscalaire appuyée sur une carte", 2),
            ("PREMIERE", "HGGSP1_INTRO_SCIENCE_POLITIQUE",   "Introduction — Les disciplines", "Expliquer ce qu'est « le politique » et ce que la science politique étudie", 3),
            ("PREMIERE", "HGGSP1_INTRO_GEOPOLITIQUE",        "Introduction — Les disciplines", "Définir la géopolitique : enjeux de pouvoir, coopérations et rivalités sur et entre les territoires, poids de l'histoire", 4),

            // Thème 1 : Comprendre un régime politique : la démocratie
            ("PREMIERE", "HGGSP1_DEMO_CARACTERISTIQUES",     "Thème 1 — La démocratie", "Identifier les caractéristiques communes aux démocraties : libertés, institutions représentatives, alternances politiques", 5),
            ("PREMIERE", "HGGSP1_DEMO_REGIMES_AUTORITAIRES", "Thème 1 — La démocratie", "Comparer une démocratie et un régime autoritaire à partir d'exemples précis", 6),
            ("PREMIERE", "HGGSP1_DEMO_DIRECTE_REPRESENTATIVE","Thème 1 — La démocratie", "Distinguer démocratie directe et démocratie représentative et en donner les limites respectives", 7),
            ("PREMIERE", "HGGSP1_DEMO_ATHENES",              "Thème 1 — La démocratie", "Expliquer en quoi la démocratie athénienne du Ve siècle est directe mais limitée : qui est citoyen ?", 8),
            ("PREMIERE", "HGGSP1_DEMO_CONSTANT",             "Thème 1 — La démocratie", "Opposer « liberté des Anciens » et « liberté des Modernes » selon Benjamin Constant : participer ou être représenté", 9),
            ("PREMIERE", "HGGSP1_DEMO_TOCQUEVILLE",          "Thème 1 — La démocratie", "Expliquer l'inquiétude de Tocqueville : comment la démocratie peut glisser vers la tyrannie", 10),
            ("PREMIERE", "HGGSP1_DEMO_CHILI",                "Thème 1 — La démocratie", "Analyser la crise et la fin de la démocratie au Chili de 1970 à 1973", 11),
            ("PREMIERE", "HGGSP1_DEMO_TRANSITIONS_IBERIQUES","Thème 1 — La démocratie", "Retracer le passage d'un régime autoritaire à la démocratie au Portugal et en Espagne de 1974 à 1982", 12),
            ("PREMIERE", "HGGSP1_DEMO_UE_FONCTIONNEMENT",     "Thème 1 — La démocratie", "Expliquer le fonctionnement de l'Union européenne entre démocratie représentative et démocratie déléguée", 13),
            ("PREMIERE", "HGGSP1_DEMO_UE_CONTESTATIONS",     "Thème 1 — La démocratie", "Présenter les remises en question de l'Union européenne par les citoyens et les États depuis 1992", 14),

            // Thème 2 : Analyser les dynamiques des puissances internationales
            ("PREMIERE", "HGGSP1_PUIS_FONDEMENTS",           "Thème 2 — Les puissances internationales", "Identifier les fondements et manifestations de la puissance : diplomatiques, militaires, culturels, économiques et financiers", 15),
            ("PREMIERE", "HGGSP1_PUIS_DYNAMIQUE",            "Thème 2 — Les puissances internationales", "Décrire la dynamique d'une puissance entre affirmation, domination et déclin", 16),
            ("PREMIERE", "HGGSP1_PUIS_OTTOMAN",              "Thème 2 — Les puissances internationales", "Expliquer l'essor puis le déclin de l'Empire ottoman", 17),
            ("PREMIERE", "HGGSP1_PUIS_RUSSIE",               "Thème 2 — Les puissances internationales", "Analyser comment la Russie reconstruit sa puissance après l'éclatement d'un empire, depuis 1991", 18),
            ("PREMIERE", "HGGSP1_PUIS_LANGUE",               "Thème 2 — Les puissances internationales", "Montrer que la langue est une forme indirecte de puissance : anglais, français, francophonie, instituts Confucius", 19),
            ("PREMIERE", "HGGSP1_PUIS_NUMERIQUE",            "Thème 2 — Les puissances internationales", "Discuter la puissance des géants du numérique (GAFAM, BATX) face aux États et aux organisations internationales", 20),
            ("PREMIERE", "HGGSP1_PUIS_ROUTES_SOIE",          "Thème 2 — Les puissances internationales", "Expliquer comment les « nouvelles routes de la Soie » font de la maîtrise des voies de communication un levier de puissance", 21),
            ("PREMIERE", "HGGSP1_PUIS_EU_LIEUX",             "Thème 2 — Les puissances internationales", "Localiser les lieux et les formes de la puissance des États-Unis : siège de l'ONU, Hollywood, MIT", 22),
            ("PREMIERE", "HGGSP1_PUIS_EU_UNILATERALISME",    "Thème 2 — Les puissances internationales", "Opposer unilatéralisme et multilatéralisme à propos de la politique des États-Unis", 23),
            ("PREMIERE", "HGGSP1_PUIS_EU_INFLUENCE",         "Thème 2 — Les puissances internationales", "Présenter les points d'appui et les zones d'influence des États-Unis dans un monde multipolaire", 24),

            // Thème 3 : Étudier les divisions politiques du monde : les frontières
            ("PREMIERE", "HGGSP1_FRONT_AUJOURDHUI",          "Thème 3 — Les frontières", "Montrer que les frontières, de plus en plus nombreuses et plus ou moins marquées, séparent et mettent en contact", 25),
            ("PREMIERE", "HGGSP1_FRONT_TRANSFRONTALIER",     "Thème 3 — Les frontières", "Définir un espace transfrontalier et expliquer son affirmation", 26),
            ("PREMIERE", "HGGSP1_FRONT_LIMES",               "Thème 3 — Les frontières", "Expliquer la fonction de protection du limes rhénan", 27),
            ("PREMIERE", "HGGSP1_FRONT_BERLIN_AFRIQUE",      "Thème 3 — Les frontières", "Expliquer comment la conférence de Berlin organise le partage de l'Afrique et ses conséquences", 28),
            ("PREMIERE", "HGGSP1_FRONT_COREES",              "Thème 3 — Les frontières", "Analyser la frontière entre les deux Corée comme séparation de deux systèmes politiques", 29),
            ("PREMIERE", "HGGSP1_FRONT_GERMANO_POLONAISE",   "Thème 3 — Les frontières", "Retracer la reconnaissance de la frontière germano-polonaise de 1939 à 1990, entre guerre et diplomatie", 30),
            ("PREMIERE", "HGGSP1_FRONT_DROIT_MER",           "Thème 3 — Les frontières", "Expliquer comment le droit de la mer s'applique à toutes les mers indépendamment des frontières", 31),
            ("PREMIERE", "HGGSP1_FRONT_SCHENGEN",            "Thème 3 — Les frontières", "Présenter les enjeux de Schengen et du contrôle aux frontières : venir en Europe, passer la frontière", 32),
            ("PREMIERE", "HGGSP1_FRONT_UE_ETAT_ET_QUOTIDIEN","Thème 3 — Les frontières", "Décrire les frontières d'un État adhérent et le franchissement quotidien dans un espace transfrontalier européen", 33),

            // Thème 4 : S'informer : un regard critique sur les sources et modes de communication
            ("PREMIERE", "HGGSP1_INFO_PRATIQUES",            "Thème 4 — S'informer", "Décrire la diversité des médias et des pratiques d'information selon les individus, les groupes sociaux et les territoires", 34),
            ("PREMIERE", "HGGSP1_INFO_IMPRIME",              "Thème 4 — S'informer", "Retracer l'essor de l'information imprimée, de la diffusion de l'imprimerie à la presse à grand tirage", 35),
            ("PREMIERE", "HGGSP1_INFO_RADIO_TV",             "Thème 4 — S'informer", "Expliquer la place prise par la radio et la télévision dans l'information au XXe siècle", 36),
            ("PREMIERE", "HGGSP1_INFO_INTERNET",             "Thème 4 — S'informer", "Expliquer la naissance et l'extension d'Internet et l'information mondialisée et individualisée qui en découle", 37),
            ("PREMIERE", "HGGSP1_INFO_DREYFUS",              "Thème 4 — S'informer", "Analyser le rôle de la presse dans l'affaire Dreyfus : une information dépendante de l'opinion ?", 38),
            ("PREMIERE", "HGGSP1_INFO_HAVAS_AFP",            "Thème 4 — S'informer", "Retracer l'histoire de l'agence Havas et de l'AFP, entre le marché et l'État", 39),
            ("PREMIERE", "HGGSP1_INFO_VIETNAM",              "Thème 4 — S'informer", "Analyser les rapports entre médias, information et propagande pendant la guerre du Vietnam", 40),
            ("PREMIERE", "HGGSP1_INFO_FRAGMENTEE",           "Thème 4 — S'informer", "Caractériser une information fragmentée et horizontale à l'heure d'Internet", 41),
            ("PREMIERE", "HGGSP1_INFO_LANCEURS_ALERTE",      "Thème 4 — S'informer", "Évaluer la place des témoignages et des lanceurs d'alerte dans l'information", 42),
            ("PREMIERE", "HGGSP1_INFO_COMPLOT",              "Thème 4 — S'informer", "Expliquer pourquoi les théories du complot trouvent une nouvelle jeunesse sur Internet", 43),
            ("PREMIERE", "HGGSP1_INFO_REGARD_CRITIQUE",      "Thème 4 — S'informer", "Porter un regard critique sur sa propre manière de s'informer : liberté, contrôle ou manipulation de l'information", 44),

            // Thème 5 : Analyser les relations entre États et religions
            ("PREMIERE", "HGGSP1_REL_DROIT_PUBLIC",          "Thème 5 — États et religions", "Comparer les relations entre États et religions en droit public : séparation, religion officielle", 45),
            ("PREMIERE", "HGGSP1_REL_LIBERTES",              "Thème 5 — États et religions", "Distinguer liberté de conscience, liberté religieuse et laïcité à partir d'exemples", 46),
            ("PREMIERE", "HGGSP1_REL_CHARLEMAGNE",           "Thème 5 — États et religions", "Expliquer ce que le couronnement de Charlemagne révèle des relations entre le pape et l'empereur", 47),
            ("PREMIERE", "HGGSP1_REL_CALIFE_BYZANCE",        "Thème 5 — États et religions", "Comparer le calife et l'empereur byzantin aux IXe-Xe siècles : pouvoir politique et magistère religieux", 48),
            ("PREMIERE", "HGGSP1_REL_SECULARISATION",        "Thème 5 — États et religions", "Montrer que la sécularisation est un mouvement localisé, d'intensité variable, et que la religion reste un enjeu géopolitique", 49),
            ("PREMIERE", "HGGSP1_REL_TURQUIE",               "Thème 5 — États et religions", "Expliquer la laïcité en Turquie à partir de l'abolition du califat par Mustapha Kemal en 1924", 50),
            ("PREMIERE", "HGGSP1_REL_ETATS_UNIS",            "Thème 5 — États et religions", "Analyser la place des religions dans la politique intérieure des États-Unis depuis la Seconde Guerre mondiale", 51),
            ("PREMIERE", "HGGSP1_REL_INDE_SECULARISME",      "Thème 5 — États et religions", "Expliquer le « sécularisme » indien et la dimension politique de la religion en Inde", 52),
            ("PREMIERE", "HGGSP1_REL_INDE_MINORITES",        "Thème 5 — États et religions", "Présenter la situation des minorités religieuses en Inde", 53),
            ("PREMIERE", "HGGSP1_REL_INDE_PAKISTAN",         "Thème 5 — États et religions", "Analyser les enjeux géopolitiques des relations entre l'Inde et le Pakistan", 54),

            // Capacités travaillées et méthodes acquises (préambule)
            ("PREMIERE", "HGGSP1_METH_QUESTIONNER",          "Capacités et méthodes", "Poser des questions sur un objet d'étude en confrontant les points de vue et les approches disciplinaires", 55),
            ("PREMIERE", "HGGSP1_METH_DOCUMENTER",           "Capacités et méthodes", "Rechercher une information ou une source, y compris sur Internet, et rédiger une fiche de lecture", 56),
            ("PREMIERE", "HGGSP1_METH_ORAL",                 "Capacités et méthodes", "Prendre la parole de façon régulière, structurée et pertinente sur une question du programme", 57),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE GÉNÉRALE — « Analyser les grands enjeux du monde contemporain » (MENE1921254A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HGGSPT =
        {
            // Thème 1 : De nouveaux espaces de conquête
            ("TERMINALE", "HGGSPT_CONQ_SPECIFICITES",        "Thème 1 — Nouveaux espaces de conquête", "Expliquer les spécificités de l'océan et de l'espace : une maîtrise en constante évolution, les dernières frontières ?", 1),
            ("TERMINALE", "HGGSPT_CONQ_COURSE_ESPACE",       "Thème 1 — Nouveaux espaces de conquête", "Analyser les enjeux de la course à l'espace, des années 1950 aux nouveaux acteurs (Chine, Inde, entreprises privées)", 2),
            ("TERMINALE", "HGGSPT_CONQ_DISSUASION",          "Thème 1 — Nouveaux espaces de conquête", "Expliquer comment la dissuasion nucléaire et les forces de projection maritimes affirment une puissance", 3),
            ("TERMINALE", "HGGSPT_CONQ_ISS",                 "Thème 1 — Nouveaux espaces de conquête", "Montrer que la station spatiale internationale est une coopération pour développer la recherche", 4),
            ("TERMINALE", "HGGSPT_CONQ_ZEE",                 "Thème 1 — Nouveaux espaces de conquête", "Expliquer la création des zones économiques exclusives par la convention de Montego Bay et les rivalités qu'elle encadre", 5),
            ("TERMINALE", "HGGSPT_CONQ_BBNJ",                "Thème 1 — Nouveaux espaces de conquête", "Présenter la gestion commune de la biodiversité marine au-delà des juridictions nationales (conférence BBNJ)", 6),
            ("TERMINALE", "HGGSPT_CONQ_RIVALITES_COOP",      "Thème 1 — Nouveaux espaces de conquête", "Distinguer rivalités interétatiques et coopérations rendues nécessaires par des intérêts communs dans ces espaces", 7),
            ("TERMINALE", "HGGSPT_CONQ_CHINE_VOLONTE",       "Thème 1 — Nouveaux espaces de conquête", "Décrire la volonté politique chinoise de conquérir l'espace, les mers et les océans : discours, investissements, appropriations", 8),
            ("TERMINALE", "HGGSPT_CONQ_CHINE_ENJEUX",        "Thème 1 — Nouveaux espaces de conquête", "Évaluer les enjeux économiques et géopolitiques de ces conquêtes pour la Chine et le reste du monde", 9),

            // Thème 2 : Faire la guerre, faire la paix : formes de conflits et modes de résolution
            ("TERMINALE", "HGGSPT_GUERRE_TYPOLOGIE",         "Thème 2 — Guerre et paix", "Classer les conflits armés actuels selon leur nature, leurs acteurs et leurs modes de résolution", 10),
            ("TERMINALE", "HGGSPT_GUERRE_CLAUSEWITZ",        "Thème 2 — Guerre et paix", "Expliquer la guerre comme « continuation de la politique par d'autres moyens » selon Clausewitz", 11),
            ("TERMINALE", "HGGSPT_GUERRE_7ANS_NAPOLEON",     "Thème 2 — Guerre et paix", "Illustrer le modèle de Clausewitz par les guerres de la guerre de Sept Ans aux guerres napoléoniennes", 12),
            ("TERMINALE", "HGGSPT_GUERRE_IRREGULIERES",      "Thème 2 — Guerre et paix", "Montrer les limites du modèle de Clausewitz face aux « guerres irrégulières », d'Al Qaïda à Daech", 13),
            ("TERMINALE", "HGGSPT_GUERRE_WESTPHALIE",        "Thème 2 — Guerre et paix", "Expliquer comment les traités de Westphalie (1648) font la paix par les traités", 14),
            ("TERMINALE", "HGGSPT_GUERRE_ONU_ANNAN",         "Thème 2 — Guerre et paix", "Analyser les actions de l'ONU sous les mandats de Kofi Annan (1997-2006) au nom de la sécurité collective", 15),
            ("TERMINALE", "HGGSPT_GUERRE_CONSTRUIRE_PAIX",   "Thème 2 — Guerre et paix", "Expliquer pourquoi la construction de la paix est complexe et quels enjeux diplomatiques elle soulève", 16),
            ("TERMINALE", "HGGSPT_GUERRE_ISRAEL_PALESTINE",  "Thème 2 — Guerre et paix", "Retracer les tentatives de résolution du conflit israélo-arabe puis israélo-palestinien depuis la création d'Israël", 17),
            ("TERMINALE", "HGGSPT_GUERRE_GOLFE",             "Thème 2 — Guerre et paix", "Expliquer les guerres du Golfe (1991, 2003) et le passage d'une guerre interétatique à un conflit asymétrique", 18),
            ("TERMINALE", "HGGSPT_GUERRE_ACTEURS_MO",        "Thème 2 — Guerre et paix", "Identifier les acteurs internationaux, étatiques et non étatiques, des conflits régionaux du Moyen-Orient", 19),

            // Thème 3 : Histoire et mémoires
            ("TERMINALE", "HGGSPT_MEM_HISTOIRE_MEMOIRE",     "Thème 3 — Histoire et mémoires", "Distinguer histoire et mémoire à partir d'un exemple", 20),
            ("TERMINALE", "HGGSPT_MEM_CRIME_GENOCIDE",       "Thème 3 — Histoire et mémoires", "Définir crime contre l'humanité et génocide et situer le contexte de leur élaboration", 21),
            ("TERMINALE", "HGGSPT_MEM_CAUSES_1GM",           "Thème 3 — Histoire et mémoires", "Exposer le débat historique sur les causes de la Première Guerre mondiale et ses implications politiques", 22),
            ("TERMINALE", "HGGSPT_MEM_ALGERIE",              "Thème 3 — Histoire et mémoires", "Analyser les mémoires et l'histoire de la guerre d'Algérie", 23),
            ("TERMINALE", "HGGSPT_MEM_GACACA",               "Thème 3 — Histoire et mémoires", "Expliquer le rôle des tribunaux gacaca, justice à l'échelle locale face au génocide des Tutsis", 24),
            ("TERMINALE", "HGGSPT_MEM_TPIY",                 "Thème 3 — Histoire et mémoires", "Expliquer la construction d'une justice pénale internationale face aux crimes de masse à travers le TPIY", 25),
            ("TERMINALE", "HGGSPT_MEM_LIEUX_GENOCIDE",       "Thème 3 — Histoire et mémoires", "Présenter des lieux de mémoire du génocide des Juifs et des Tsiganes", 26),
            ("TERMINALE", "HGGSPT_MEM_JUGER_NAZIS",          "Thème 3 — Histoire et mémoires", "Retracer comment les crimes nazis ont été jugés après Nuremberg", 27),
            ("TERMINALE", "HGGSPT_MEM_LITTERATURE_CINEMA",   "Thème 3 — Histoire et mémoires", "Analyser la représentation du génocide des Juifs et des Tsiganes dans la littérature et le cinéma", 28),

            // Thème 4 : Identifier, protéger et valoriser le patrimoine : enjeux géopolitiques
            ("TERMINALE", "HGGSPT_PAT_NOTION",               "Thème 4 — Le patrimoine", "Retracer l'élargissement de la notion de patrimoine, de la transmission entre individus à l'héritage de l'humanité", 29),
            ("TERMINALE", "HGGSPT_PAT_MATERIEL_IMMATERIEL",  "Thème 4 — Le patrimoine", "Distinguer patrimoine matériel et patrimoine immatériel à partir d'exemples", 30),
            ("TERMINALE", "HGGSPT_PAT_UNESCO",               "Thème 4 — Le patrimoine", "Expliquer le « patrimoine mondial » de l'Unesco, construction de plus en plus diversifiée mais spatialement concentrée", 31),
            ("TERMINALE", "HGGSPT_PAT_VERSAILLES",           "Thème 4 — Le patrimoine", "Montrer comment les usages de Versailles, de l'Empire à nos jours, réaménagent la mémoire", 32),
            ("TERMINALE", "HGGSPT_PAT_PARTHENON",            "Thème 4 — Le patrimoine", "Analyser le conflit de patrimoine autour des frises du Parthénon depuis le XIXe siècle", 33),
            ("TERMINALE", "HGGSPT_PAT_PARIS",                "Thème 4 — Le patrimoine", "Expliquer les tensions entre urbanisation, développement économique et préservation du patrimoine à Paris", 34),
            ("TERMINALE", "HGGSPT_PAT_MALI",                 "Thème 4 — Le patrimoine", "Analyser la destruction, la protection et la restauration du patrimoine au Mali comme enjeu géopolitique", 35),
            ("TERMINALE", "HGGSPT_PAT_VENISE",               "Thème 4 — Le patrimoine", "Discuter le tourisme culturel à Venise, entre valorisation touristique et protection du patrimoine", 36),
            ("TERMINALE", "HGGSPT_PAT_POLITIQUE_FRANCE",     "Thème 4 — Le patrimoine", "Décrire les évolutions de la gestion du patrimoine français comme politique publique", 37),
            ("TERMINALE", "HGGSPT_PAT_BASSIN_MINIER",        "Thème 4 — Le patrimoine", "Expliquer la patrimonialisation du bassin minier du Nord-Pas-de-Calais, entre héritage culturel et reconversion", 38),
            ("TERMINALE", "HGGSPT_PAT_RAYONNEMENT",          "Thème 4 — Le patrimoine", "Montrer que le patrimoine sert le rayonnement et la diplomatie de la France (repas gastronomique des Français)", 39),

            // Thème 5 : L'environnement, entre exploitation et protection : un enjeu planétaire
            ("TERMINALE", "HGGSPT_ENV_NOTION",               "Thème 5 — L'environnement", "Expliquer que l'environnement est une notion construite historiquement, socialement et politiquement", 40),
            ("TERMINALE", "HGGSPT_ENV_HISTOIRE",             "Thème 5 — L'environnement", "Situer les grandes étapes d'une histoire de l'environnement et des rapports entre sociétés et milieux", 41),
            ("TERMINALE", "HGGSPT_ENV_FORET_COLBERT",        "Thème 5 — L'environnement", "Expliquer comment la forêt française est exploitée et protégée depuis Colbert", 42),
            ("TERMINALE", "HGGSPT_ENV_DEUX_REVOLUTIONS",     "Thème 5 — L'environnement", "Discuter si « révolution néolithique » et « révolution industrielle » sont deux ruptures dans l'évolution des milieux", 43),
            ("TERMINALE", "HGGSPT_ENV_CLIMAT_EUROPE",        "Thème 5 — L'environnement", "Décrire les fluctuations du climat en Europe du Moyen Âge au XIXe siècle et leurs effets sur les sociétés", 44),
            ("TERMINALE", "HGGSPT_ENV_ACCORDS_CLIMAT",       "Thème 5 — L'environnement", "Analyser le climat comme enjeu des relations internationales : Sommets de la Terre, COP", 45),
            ("TERMINALE", "HGGSPT_ENV_ETATS_UNIS_INTERNE",   "Thème 5 — L'environnement", "Expliquer la question environnementale aux États-Unis depuis le XIXe siècle et les rôles de l'État fédéral et des États", 46),
            ("TERMINALE", "HGGSPT_ENV_ETATS_UNIS_MONDE",     "Thème 5 — L'environnement", "Analyser l'action des États-Unis sur l'environnement à l'échelle internationale : État, firmes transnationales, ONG", 47),

            // Thème 6 : L'enjeu de la connaissance
            ("TERMINALE", "HGGSPT_CONN_SOCIETE",             "Thème 6 — La connaissance", "Expliquer la notion de « société de la connaissance » (Peter Drucker, 1969), sa portée et les débats qu'elle suscite", 48),
            ("TERMINALE", "HGGSPT_CONN_COMMUNAUTE",          "Thème 6 — La connaissance", "Définir communauté savante et communauté scientifique en histoire des sciences", 49),
            ("TERMINALE", "HGGSPT_CONN_CIRCULATION",         "Thème 6 — La connaissance", "Identifier les acteurs et les modalités de la circulation de la connaissance", 50),
            ("TERMINALE", "HGGSPT_CONN_ALPHABETISATION",     "Thème 6 — La connaissance", "Retracer les grandes étapes de l'alphabétisation des femmes dans le monde du XVIe siècle à nos jours", 51),
            ("TERMINALE", "HGGSPT_CONN_RADIOACTIVITE",       "Thème 6 — La connaissance", "Montrer comment les recherches sur la radioactivité, de 1896 aux années 1950, font vivre une communauté savante", 52),
            ("TERMINALE", "HGGSPT_CONN_RENSEIGNEMENT",       "Thème 6 — La connaissance", "Analyser le renseignement des services secrets soviétiques et américains au service des États pendant la guerre froide", 53),
            ("TERMINALE", "HGGSPT_CONN_INDE",                "Thème 6 — La connaissance", "Expliquer les liens entre formation des étudiants, transferts de technologie et puissance économique en Inde", 54),
            ("TERMINALE", "HGGSPT_CONN_CYBERESPACE",         "Thème 6 — La connaissance", "Décrire le cyberespace entre réseaux et territoires : infrastructures, acteurs, liberté ou contrôle des données", 55),
            ("TERMINALE", "HGGSPT_CONN_CYBERDEFENSE",        "Thème 6 — La connaissance", "Expliquer la cyberdéfense française, entre coopération européenne et souveraineté nationale", 56),

            // Capacités et méthodes (préambule + attendus de la note de service MENE2521923N)
            ("TERMINALE", "HGGSPT_METH_PROBLEMATIQUE",       "Capacités et méthodes", "Construire une problématique à partir d'un sujet de dissertation ou du titre d'une étude critique", 57),
            ("TERMINALE", "HGGSPT_METH_DISSERTATION",        "Capacités et méthodes", "Organiser une dissertation : introduction avec problématique, plusieurs parties structurées, conclusion qui y répond", 58),
            ("TERMINALE", "HGGSPT_METH_ETUDE_CRITIQUE",      "Capacités et méthodes", "Mener l'étude critique d'un ou deux documents : sélectionner, hiérarchiser, expliciter, prendre un recul critique", 59),
            ("TERMINALE", "HGGSPT_METH_CROQUIS",             "Capacités et méthodes", "Réaliser un croquis ou un schéma à l'appui d'une argumentation, avec une légende organisée", 60),
            ("TERMINALE", "HGGSPT_METH_ORAL",                "Capacités et méthodes", "Exposer à l'oral, en autonomie et pendant une durée longue, une réponse construite à une question problématisée", 61),
            ("TERMINALE", "HGGSPT_METH_NOTIONS_PREMIERE",    "Capacités et méthodes", "Mobiliser les notions de première (démocratie, puissance, frontière, information, religion) dans un sujet de terminale", 62),
        };
    }
}
