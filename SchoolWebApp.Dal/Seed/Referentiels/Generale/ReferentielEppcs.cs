namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// L'enseignement de spécialité « Éducation physique, pratiques et culture sportives » (EPPCS)
    /// de la voie générale, tel que le texte officiel le définit — provenance détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielEppcs
    {
        // =====================================================================================
        // RÉFÉRENTIEL DE LA SPÉCIALITÉ EPPCS (voie générale, première et terminale)
        // Programme en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis le texte
        // officiel téléchargé et lu le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\eppcs\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau    | Intitulé exact (titre de l'annexe)                                              |
        // |---------|-----------|---------------------------------------------------------------------------------|
        // | EPPCS1  | PREMIERE  | Programme de spécialité d'éducation physique, pratiques et culture sportives   |
        // |         |           | de première et terminale générales — partie « Classe de première »              |
        // | EPPCST  | TERMINALE | même annexe — partie « Classe terminale »                                       |
        //
        // UN SEUL TEXTE POUR LES DEUX ANNÉES : arrêté du 2-6-2021, J.O. du 13-6-2021, NOR MENE2116606A,
        //   BO n° 25 du 24 juin 2021. Article 2 lu : « entrent en application à la rentrée de l'année scolaire
        //   2021-2022 pour la classe de première et à la rentrée de l'année scolaire 2022-2023 pour la classe
        //   de terminale ». Une seule annexe (10 pages), lue en entier.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/21/Hebdo25/MENE2116606A.htm
        //   Annexe          : https://cache.media.education.gouv.fr/file/25/49/1/ensel606_annexe_1413491.pdf
        //                     (copie éduscol : ensel606annexe1413491pdf-84870.pdf, même taille, même empreinte md5)
        //   Légifrance      : https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000043648296 (« Version initiale »)
        //   ATTENTION : le PDF de l'annexe n'a AUCUNE couche texte (export Word, texte en images). Les pages
        //   ont été rendues en PNG (pdf.js) et lues à l'écran. Aucune copie non officielle n'a servi de source.
        //   Création de la spécialité : arrêté du 17-2-2021, NOR MENE2105562A (BO n° 15 du 15-4-2021),
        //   4 h en première, 6 h en terminale, réservée aux élèves qui ne suivent pas l'option EPS.
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : la page éduscol « Programmes et ressources en éducation physique
        //   et sportive (EPS) - voie GT » (https://eduscol.education.gouv.fr/5784/..., datée « mars 2026 »)
        //   cite ce programme (BO n° 25 du 24 juin 2021) comme programme en vigueur. Légifrance ne montre
        //   aucune modification de l'arrêté. Aucun texte modificatif trouvé au BO de 2021 à 2026 (recherche
        //   du 14/09/2026). La note de service du 20-2-2026 (MENE2531948N) porte sur l'EPS commune et ne
        //   mentionne pas l'EPPCS. Les seuls changements concernent l'ÉPREUVE (voir eppcs.prof.md).
        //
        // ÉCARTS ET CHOIX
        //   - Le programme rédige ses compétences deux fois. Le corps du texte (pages 2 à 9) et le tableau
        //     « Synthèse du programme » (page 10) ne disent pas la même chose : « pouvoirs moteurs » au lieu
        //     de « capacités motrices », « Porter un regard analytique » au lieu de « Analyser sa pratique ».
        //     Les attendus diffèrent aussi. Les libellés suivent le CORPS du texte.
        //   - La note de service de l'épreuve nomme la thématique de la dissertation « enjeux de la pratique
        //     sportive dans le monde contemporain ». Le programme l'intitule « La pratique physique dans le
        //     monde contemporain » : le Domaine reprend le titre du programme.
        //   - Les cinq champs d'apprentissage concernent tout le cycle terminal (« au moins une Apsa dans
        //     chacun des cinq champs »). Ils sont rangés en TERMINALE, où l'épreuve pratique les mobilise.
        //   - Préfixe « Pratique — » (tiret cadratin) : lignes qui ne se travaillent qu'en pratique physique
        //     (Apsa, conduite d'échauffement ou de séance, animation de projet). Mimia ne peut pas les faire
        //     travailler. Ne pas filtrer sur « Pratique » seul : le Domaine « Pratique physique et santé »
        //     est entièrement théorique.
        //   - Les objectifs d'apprentissage qui énumèrent plusieurs idées ont été découpés, une idée par
        //     ligne. Les exemples entre parenthèses du texte sont gardés quand la place le permet.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   EPPCS1 42 (dont 6 « Pratique — ») · EPPCST 50 (dont 10 « Pratique — ») · total 92
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — EPPCS (annexe de MENE2116606A, parties « Classe de première »)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] EPPCS1 =
        {
            // Compétences « Pratiquer » et apports pratiques
            ("PREMIERE", "EPPCS1_PRAT_CAPACITES_MOTRICES",  "Pratique — Apsa", "Développer et renforcer ses capacités motrices par la découverte et l'approfondissement d'Apsa", 1),
            ("PREMIERE", "EPPCS1_PRAT_PERFORMANCE",         "Pratique — Apsa", "Se préparer et accomplir une performance physique ou sportive, en toute sécurité, en mobilisant au mieux ses ressources", 2),
            ("PREMIERE", "EPPCS1_PRAT_TROIS_APSA",          "Pratique — Apsa", "Pratiquer au moins trois Apsa de champs d'apprentissage différents, en séquences d'au moins 18 heures", 3),
            ("PREMIERE", "EPPCS1_PRAT_COMPETENCES_GROUPE",  "Pratique — Apsa", "Identifier et exploiter la diversité des compétences au sein d'un groupe pour atteindre un objectif commun", 4),

            // Métiers du sport et du corps humain
            ("PREMIERE", "EPPCS1_METIERS_SECTEURS",         "Métiers du sport et du corps humain", "Identifier les secteurs professionnels du sport et du corps : enseignement, entraînement, santé, gestion, loisirs, sécurité", 5),
            ("PREMIERE", "EPPCS1_METIERS_FORMATIONS",       "Métiers du sport et du corps humain", "Connaître les formations, les diplômes et les qualifications associés aux métiers du sport et du corps", 6),
            ("PREMIERE", "EPPCS1_METIERS_EMPLOI",           "Métiers du sport et du corps humain", "Connaître les caractéristiques du travail et de l'emploi dans le domaine des pratiques physiques", 7),
            ("PREMIERE", "EPPCS1_METIERS_ORIENTATION",      "Métiers du sport et du corps humain", "Situer son projet d'orientation parmi les secteurs professionnels relatifs au sport et au corps humain", 8),
            ("PREMIERE", "EPPCS1_PUBLICS_TYPES",            "Métiers du sport et du corps humain", "Identifier les publics de pratiquants : enfants, adolescents, adultes, seniors, personnes en situation de handicap", 9),
            ("PREMIERE", "EPPCS1_PUBLICS_CARACTERISTIQUES", "Métiers du sport et du corps humain", "Prendre en compte les caractéristiques psychologiques et physiologiques d'un public pour organiser sa pratique", 10),
            ("PREMIERE", "EPPCS1_CONTEXTES_PRATIQUE",       "Métiers du sport et du corps humain", "Connaître les contextes de pratique : écoles, clubs, associations de quartier, structures privées, de réadaptation", 11),

            // Pratique physique et santé (thématique théorique, malgré son nom)
            ("PREMIERE", "EPPCS1_SANTE_DIMENSIONS",         "Pratique physique et santé", "Connaître et illustrer les trois dimensions principales de la santé : physiologique, psychologique, sociale", 12),
            ("PREMIERE", "EPPCS1_HYGIENE_VIE",              "Pratique physique et santé", "Connaître les principes d'une bonne hygiène de vie : sommeil, alimentation, activité physique, relations sociales", 13),
            ("PREMIERE", "EPPCS1_SANTE_PUBLIQUE",           "Pratique physique et santé", "Différencier santé individuelle et politique de santé publique", 14),
            ("PREMIERE", "EPPCS1_SEDENTARITE_DEFINITION",   "Pratique physique et santé", "Définir sédentarité et activité physique par leurs critères et expliquer qu'on peut être à la fois actif et sédentaire", 15),
            ("PREMIERE", "EPPCS1_INTENSITE_INDICATEURS",    "Pratique physique et santé", "Connaître les indicateurs et intensités d'une activité physique et les outils, dont objets connectés, qui les évaluent", 16),
            ("PREMIERE", "EPPCS1_MESURER_EFFETS",           "Pratique physique et santé", "Mesurer et analyser les effets de l'activité physique à l'aide d'indicateurs", 17),
            ("PREMIERE", "EPPCS1_SEDENTARITE_CONSEQUENCES", "Pratique physique et santé", "Connaître les conséquences d'un excès de sédentarité : obésité, maladies cardio-vasculaires, hypertension", 18),
            ("PREMIERE", "EPPCS1_ACTIVITE_REGULIERE",       "Pratique physique et santé", "Expliquer pourquoi une activité physique régulière permet le bon fonctionnement de l'organisme", 19),
            ("PREMIERE", "EPPCS1_SPORT_SANTE",              "Pratique physique et santé", "Expliquer pourquoi sont apparues les notions de « sport santé » et de « sport sur ordonnance »", 20),
            ("PREMIERE", "EPPCS1_LESIONS",                  "Pratique physique et santé", "Différencier les lésions ligamentaires, tendineuses, musculaires, osseuses ou cartilagineuses d'une pratique inappropriée", 21),
            ("PREMIERE", "EPPCS1_EMOTIONS",                 "Pratique physique et santé", "Connaître et illustrer les émotions liées à la pratique : plaisir, appréhension, peur, satisfaction, fierté, agressivité", 22),
            ("PREMIERE", "EPPCS1_ESTIME_SOI",               "Pratique physique et santé", "Expliquer pourquoi et à quelles conditions la pratique physique améliore ou dégrade l'estime et la confiance en soi", 23),
            ("PREMIERE", "EPPCS1_LIEN_SOCIAL",              "Pratique physique et santé", "Expliquer pourquoi et à quelles conditions la pratique physique peut favoriser le lien social", 24),
            ("PREMIERE", "EPPCS1_QUALITES_PHYSIQUES",       "Pratique physique et santé", "Connaître les qualités physiques fondamentales : endurance, vitesse, force, souplesse, coordination", 25),
            ("PREMIERE", "EPPCS1_TESTS_QUALITES",           "Pratique physique et santé", "Connaître et mettre en œuvre des tests permettant d'évaluer les qualités physiques", 26),
            ("PREMIERE", "EPPCS1_FILIERES_ENERGETIQUES",    "Pratique physique et santé", "Connaître les filières énergétiques et les mettre en lien avec les efforts effectués dans des Apsa", 27),

            // Technologie des Apsa (première)
            ("PREMIERE", "EPPCS1_ECHAUFFEMENT_ROLE",        "Technologie des Apsa", "Expliquer pourquoi il est indispensable de s'échauffer avant la pratique d'une Apsa", 28),
            ("PREMIERE", "EPPCS1_ECHAUFFEMENT_CONDUIRE",    "Pratique — Technologie des Apsa", "Conduire un échauffement avant la pratique d'une Apsa", 29),
            ("PREMIERE", "EPPCS1_CHARGE_TRAVAIL",           "Technologie des Apsa", "Connaître les indicateurs d'une charge de travail dans une Apsa", 30),
            ("PREMIERE", "EPPCS1_RECUPERATION_EFFORT",      "Technologie des Apsa", "Expliquer l'intérêt d'alterner périodes de récupération et d'effort dans l'entraînement d'une Apsa", 31),
            ("PREMIERE", "EPPCS1_FACTEURS_PERFORMANCE",     "Technologie des Apsa", "Identifier les facteurs psychologiques, physiologiques, techniques et tactiques d'une performance dans une Apsa", 32),
            ("PREMIERE", "EPPCS1_ETAPES_PROGRESSION",       "Technologie des Apsa", "Identifier les étapes de progression dans une Apsa en s'appuyant sur des indicateurs", 33),
            ("PREMIERE", "EPPCS1_OUTILS_NUMERIQUES",        "Technologie des Apsa", "Exploiter les apports des outils numériques pour décrire et analyser une performance dans une Apsa", 34),
            ("PREMIERE", "EPPCS1_DECRIRE_PRESTATION",       "Technologie des Apsa", "Décrire et commenter une prestation physique, la sienne ou celle d'un autre élève, à l'aide de différents indicateurs", 35),

            // Compétences « Communiquer » et carnet de suivi
            ("PREMIERE", "EPPCS1_ARGUMENTATION",            "Compétences — Communiquer", "Développer une argumentation sur une thématique de la pratique physique avec des connaissances de différents domaines", 36),
            ("PREMIERE", "EPPCS1_CARNET_SUIVI",             "Carnet de suivi", "Rendre compte de ses expériences, réussites, difficultés et progrès dans un carnet de suivi", 37),

            // Projet de première : organisation d'un événement
            ("PREMIERE", "EPPCS1_PROJET_EVENEMENTS",        "Projet — Organisation d'un événement", "Identifier les événements liés à la pratique physique ou sportive susceptibles de servir de support à un projet", 38),
            ("PREMIERE", "EPPCS1_PROJET_CONCEPTION",        "Projet — Organisation d'un événement", "Concevoir et promouvoir, au sein d'un groupe, un projet relatif à la pratique physique ou sportive", 39),
            ("PREMIERE", "EPPCS1_PROJET_ANIMATION",         "Pratique — Projet", "S'engager dans la construction et l'animation d'un projet collectif relatif à la pratique physique", 40),
            ("PREMIERE", "EPPCS1_PROJET_INDICATEURS",       "Projet — Organisation d'un événement", "Choisir et mobiliser des indicateurs pour évaluer la réalisation d'un projet et l'atteinte de ses objectifs", 41),
            ("PREMIERE", "EPPCS1_PROJET_BILAN",             "Projet — Organisation d'un événement", "Rendre compte du déroulement d'un projet et des résultats atteints dans un bilan écrit", 42),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE — EPPCS (annexe de MENE2116606A, parties « Classe terminale »)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] EPPCST =
        {
            // Compétences « Pratiquer »
            ("TERMINALE", "EPPCST_PRAT_CAPACITES_MOTRICES", "Pratique — Apsa", "Affiner et stabiliser ses capacités motrices par la découverte et l'approfondissement d'Apsa", 1),
            ("TERMINALE", "EPPCST_PRAT_PERFORMANCE",        "Pratique — Apsa", "Se préparer et accomplir une performance, individuellement et collectivement, en toute sécurité", 2),
            ("TERMINALE", "EPPCST_PRAT_ENTRAIDE",           "Pratique — Apsa", "S'entraider et progresser ensemble, en valorisant les capacités de chacun", 3),

            // Les cinq champs d'apprentissage (au moins une Apsa dans chacun sur le cycle terminal)
            ("TERMINALE", "EPPCST_CA1_PERFORMANCE_MAXIMALE", "Pratique — Champs d'apprentissage", "Réaliser une performance motrice maximale mesurable à une échéance donnée", 4),
            ("TERMINALE", "EPPCST_CA2_ENVIRONNEMENTS",      "Pratique — Champs d'apprentissage", "Adapter son déplacement à des environnements variés ou incertains", 5),
            ("TERMINALE", "EPPCST_CA3_PRESTATION_CORPORELLE", "Pratique — Champs d'apprentissage", "Réaliser une prestation corporelle destinée à être vue et appréciée", 6),
            ("TERMINALE", "EPPCST_CA4_AFFRONTEMENT",        "Pratique — Champs d'apprentissage", "Conduire et maîtriser un affrontement collectif ou interindividuel pour gagner", 7),
            ("TERMINALE", "EPPCST_CA5_RESSOURCES",          "Pratique — Champs d'apprentissage", "Réaliser et orienter une activité physique pour développer ses ressources et s'entretenir", 8),

            // La pratique physique dans le monde contemporain
            ("TERMINALE", "EPPCST_FORMES_PRATIQUE",         "La pratique physique dans le monde contemporain", "Distinguer les formes de pratique : compétition, loisir, bien-être, santé, aventure, entretien de soi, convivialité", 9),
            ("TERMINALE", "EPPCST_MANIERES_PRATIQUER",      "La pratique physique dans le monde contemporain", "Caractériser une manière de pratiquer : lieux et conditions, encadrement, attentes, motivations et profils des publics", 10),
            ("TERMINALE", "EPPCST_DONNEES_CHIFFREES",       "La pratique physique dans le monde contemporain", "Connaître des données chiffrées sur la pratique en France : licenciés, dirigeants, pratiquants occasionnels, publics", 11),
            ("TERMINALE", "EPPCST_JO_ORIGINE",              "La pratique physique dans le monde contemporain", "Connaître l'origine des Jeux olympiques et leur rénovation par Pierre de Coubertin", 12),
            ("TERMINALE", "EPPCST_JO_ENJEUX_SOCIETE",       "La pratique physique dans le monde contemporain", "Illustrer la manière dont les Jeux olympiques sont révélateurs de certains enjeux de société", 13),
            ("TERMINALE", "EPPCST_VALEURS_OLYMPISME",       "La pratique physique dans le monde contemporain", "Expliquer ce que signifient les trois valeurs de l'Olympisme", 14),
            ("TERMINALE", "EPPCST_MOUVEMENT_OLYMPIQUE",     "La pratique physique dans le monde contemporain", "Connaître l'organisation du mouvement olympique", 15),
            ("TERMINALE", "EPPCST_CHARTE_ETHIQUE",          "La pratique physique dans le monde contemporain", "Connaître et illustrer les principes de la charte d'éthique et de déontologie du Comité national olympique et sportif", 16),
            ("TERMINALE", "EPPCST_PARALYMPISME",            "La pratique physique dans le monde contemporain", "Connaître la naissance du mouvement paralympique et expliquer ses enjeux et son fonctionnement", 17),
            ("TERMINALE", "EPPCST_FEDERATIONS_CLUBS",       "La pratique physique dans le monde contemporain", "Expliquer l'organisation et le fonctionnement des fédérations sportives, des comités et des clubs", 18),
            ("TERMINALE", "EPPCST_ASSOCIATION_SPORTIVE",    "La pratique physique dans le monde contemporain", "Expliquer l'organisation et le fonctionnement de l'association sportive de son lycée", 19),
            ("TERMINALE", "EPPCST_RESULTATS_INTERNATIONAUX", "La pratique physique dans le monde contemporain", "Expliquer pourquoi les pays accordent de l'importance aux résultats sportifs internationaux de leurs athlètes", 20),
            ("TERMINALE", "EPPCST_ECONOMIE_SPORT",          "La pratique physique dans le monde contemporain", "Identifier et illustrer la diversité des activités économiques relatives au sport", 21),
            ("TERMINALE", "EPPCST_PLEINE_NATURE",           "La pratique physique dans le monde contemporain", "Illustrer les incidences des activités physiques de pleine nature sur les environnements naturels et la biodiversité", 22),
            ("TERMINALE", "EPPCST_DERIVES",                 "La pratique physique dans le monde contemporain", "Identifier les dérives du sport : violences, tricheries, dopage, corruptions, discriminations", 23),
            ("TERMINALE", "EPPCST_VIOLENCE_TRICHERIE",      "La pratique physique dans le monde contemporain", "Expliquer pourquoi le sport peut amener à des phénomènes de violences et de tricherie", 24),
            ("TERMINALE", "EPPCST_PREVENTION_DERIVES",      "La pratique physique dans le monde contemporain", "Connaître des actions de prévention des dérives liées au sport", 25),
            ("TERMINALE", "EPPCST_FEMMES_PRATIQUE",         "La pratique physique dans le monde contemporain", "Expliquer pourquoi les femmes pratiquent moins le sport et sont moins présentes dans les instances dirigeantes", 26),
            ("TERMINALE", "EPPCST_FEMMES_PLANS_ACTION",     "La pratique physique dans le monde contemporain", "Connaître et illustrer des plans d'action de l'État et du mouvement sportif pour la place des femmes dans le sport", 27),
            ("TERMINALE", "EPPCST_HANDICAP_IMPORTANCE",     "La pratique physique dans le monde contemporain", "Expliquer l'importance de la pratique physique pour des personnes en situation de handicap", 28),
            ("TERMINALE", "EPPCST_HANDICAP_ADAPTATION",     "La pratique physique dans le monde contemporain", "Expliquer la nécessité d'adapter les pratiques physiques aux différentes formes de handicap", 29),
            ("TERMINALE", "EPPCST_INNOVATIONS_SPORT_ADAPTE", "La pratique physique dans le monde contemporain", "Connaître des exemples d'innovations technologiques au service du sport adapté", 30),
            ("TERMINALE", "EPPCST_SPORT_PARTAGE",           "La pratique physique dans le monde contemporain", "Expliquer les enjeux du « sport partagé », qui associe personnes valides et personnes en situation de handicap", 31),

            // Technologie des Apsa (terminale)
            ("TERMINALE", "EPPCST_APSA_ORIGINE",            "Technologie des Apsa", "Connaître l'origine d'une Apsa et les étapes marquantes de son évolution", 32),
            ("TERMINALE", "EPPCST_APSA_FACTEURS_EVOLUTION", "Technologie des Apsa", "Expliquer que l'évolution d'une Apsa et l'émergence de nouvelles formes de pratique tiennent à plusieurs facteurs", 33),
            ("TERMINALE", "EPPCST_APSA_MATERIAUX",          "Technologie des Apsa", "Illustrer comment les transformations des matériaux et les nouvelles technologies font évoluer une Apsa", 34),
            ("TERMINALE", "EPPCST_APSA_PRATIQUANTS",        "Technologie des Apsa", "Illustrer la manière dont les pratiquants eux-mêmes font évoluer une Apsa", 35),
            ("TERMINALE", "EPPCST_APSA_REGLEMENT",          "Technologie des Apsa", "Illustrer comment les modifications d'un règlement sportif influent sur l'évolution d'une Apsa", 36),
            ("TERMINALE", "EPPCST_BIOMECANIQUE",            "Technologie des Apsa", "Définir la biomécanique du sport et expliquer son intérêt pour l'optimisation de la performance", 37),
            ("TERMINALE", "EPPCST_TECHNIQUES_FONDAMENTALES", "Technologie des Apsa", "Connaître les techniques fondamentales d'une Apsa et expliquer leurs principes d'efficacité", 38),
            ("TERMINALE", "EPPCST_PRINCIPES_TACTIQUES",     "Technologie des Apsa", "Connaître et illustrer les principes tactiques fondamentaux d'une Apsa", 39),
            ("TERMINALE", "EPPCST_TECHNIQUE_TACTIQUE",      "Technologie des Apsa", "Expliquer et illustrer le lien étroit entre composantes techniques et tactiques de la performance dans une Apsa", 40),
            ("TERMINALE", "EPPCST_NIVEAU_PERFORMANCE",      "Technologie des Apsa", "Identifier un niveau de performance dans une Apsa en choisissant des indicateurs pertinents", 41),
            ("TERMINALE", "EPPCST_COMMENTER_PRESTATION",    "Technologie des Apsa", "Commenter une prestation physique, la sienne ou celle d'un autre élève, en s'appuyant sur des éléments pertinents", 42),
            ("TERMINALE", "EPPCST_PISTES_AMELIORATION",     "Technologie des Apsa", "Identifier des pistes d'amélioration à la suite de l'analyse d'une prestation", 43),
            ("TERMINALE", "EPPCST_OUTILS_NUMERIQUES",       "Technologie des Apsa", "Exploiter les apports des outils numériques pour analyser et améliorer une performance dans une Apsa", 44),

            // Compétences « Analyser » / « Communiquer » et carnet de suivi
            ("TERMINALE", "EPPCST_CARNET_SUIVI",            "Carnet de suivi", "Analyser sa pratique en sélectionnant et en ordonnant des éléments de son carnet de suivi", 45),
            ("TERMINALE", "EPPCST_POINT_DE_VUE",            "Compétences — Communiquer", "Développer un point de vue convaincant sur la culture sportive avec des connaissances établies, précises et adaptées", 46),
            ("TERMINALE", "EPPCST_PERSPECTIVES_ORIENTATION", "Compétences — Communiquer", "Présenter ses perspectives d'orientation et ses projets dans la société en s'appuyant sur des expériences vécues", 47),

            // Projet de terminale : intervention en direction de pratiquants
            ("TERMINALE", "EPPCST_CONDUIRE_SEANCE",         "Pratique — Projet d'intervention", "Conduire des exercices ou des séances dans une Apsa auprès d'un groupe de pratiquants", 48),
            ("TERMINALE", "EPPCST_PRESENTER_TRAVAIL",       "Pratique — Projet d'intervention", "Présenter, animer et accompagner un travail à réaliser auprès d'un groupe d'élèves", 49),
            ("TERMINALE", "EPPCST_BILAN_INTERVENTION",      "Projet d'intervention", "Dresser le bilan d'une intervention avec le professeur et les élèves qui l'ont vécue", 50),
        };
    }
}
