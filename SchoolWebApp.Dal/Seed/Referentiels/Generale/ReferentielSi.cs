namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// La spécialité sciences de l'ingénieur de la voie générale, première et terminale — provenance détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielSi
    {
        // =====================================================================================
        // RÉFÉRENTIEL DE L'ENSEIGNEMENT DE SPÉCIALITÉ SCIENCES DE L'INGÉNIEUR (SI) — VOIE GÉNÉRALE
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis les textes
        // officiels téléchargés et lus le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\si\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau    | Intitulé exact (page de titre de l'annexe)                                         |
        // |---------|-----------|------------------------------------------------------------------------------------|
        // | SI1     | PREMIERE  | Programme de sciences de l'ingénieur de première et terminale générales (items « 1e ») |
        // | SIT     | TERMINALE | Même programme (items « Tale ») + Programme de sciences physiques, complément des  |
        // |         |           | sciences de l'ingénieur de terminale générale (Domaine « Sciences physiques — … ») |
        //
        // SCIENCES DE L'INGÉNIEUR (1re ET Tle) : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901640A,
        //   BO spécial n° 1 du 22 janvier 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2019 pour la
        //   classe de première et à la rentrée 2020 pour la classe terminale ». Une seule annexe, lue en entier.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901640A.htm
        //   Annexe : https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/43/6/spe640_annexe_1063436.pdf
        //            (copie éduscol spe640annexe1063436pdf-82092.pdf : même texte, vérifié par diff)
        //   Le programme est commun au cycle. Chaque ligne des tableaux porte « 1e » (« acquis et évalués à la fin
        //   de la classe de première », remobilisables en terminale) ou « Tale » (« développés sur l'ensemble du
        //   cycle et acquis en fin de cycle »). SI1 = lignes « 1e », SIT = lignes « Tale ». Le texte précise :
        //   « L'évaluation de fin de cycle porte sur l'ensemble du programme des deux années. »
        //
        // SCIENCES PHYSIQUES (Tle) : arrêté du 19-7-2019, J.O. n° 0169 du 23-7-2019, NOR MENE1921269A,
        //   BO spécial n° 8 du 25 juillet 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2020 ».
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921269A.htm
        //   Annexe : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/03/6/spe269_annexe_1159036.pdf
        //            (copie éduscol spe269annexe1159036pdf-82095.pdf : même texte, vérifié par diff)
        //   Légifrance JORFTEXT000038800024. Deux heures hebdomadaires assurées par un professeur de physique-chimie.
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027
        //   - Légifrance, arrêté MENE1901640A (JORFTEXT000038029537) « en vigueur au 14/09/2026 » : trois articles
        //     d'origine, aucun « Modifié par ».
        //   - La page éduscol « Programmes et ressources en sciences de l'ingénieur - voie GT », datée « mars 2026 »
        //     (https://eduscol.education.gouv.fr/5832/programmes-et-ressources-en-sciences-de-l-ingenieur-voie-gt),
        //     liste sous « Programmes en vigueur » ces deux programmes et l'option de seconde, rien d'autre.
        //   - Recherches Légifrance (par titre) et BO du 14/09/2026 : aucun arrêté modificatif de 2019 à 2026.
        //   - La note de service MENE2408179N (BO n° 19 du 9-5-2024, applicable depuis la session 2025) fait
        //     porter l'épreuve sur ces deux programmes « en vigueur ».
        //
        // ÉCARTS ET POINTS DE VIGILANCE
        //   - La version consolidée Légifrance de l'arrêté de sciences physiques n'a pas pu être ouverte (Cloudflare,
        //     HTTP 403, trois essais). La version JORF est lue ; l'absence de modificatif repose sur la recherche
        //     Légifrance et sur la page éduscol de mars 2026.
        //   - La page éduscol date par erreur le programme de sciences physiques « BO spécial n° 8 du 25 février
        //     2019 ». Le BO et le J.O. donnent le 25 juillet 2019.
        //   - La mise en page du PDF rend trois cellules ambiguës. Elles ont été rattachées d'après l'ordre du texte
        //     (pdftotext -raw) : « Cartes heuristiques » va avec « Imaginer une solution originale » (1e) ;
        //     « Veille technologique » va avec « Élaborer une démarche globale d'innovation » (Tale) ;
        //     « Présenter et formaliser une idée » va avec « Présenter un protocole… » (Tale).
        //   - Le projet (12 h en 1re, 48 h en Tle) figure dans le préambule, pas dans les tableaux. Il est ajouté
        //     une fois par année, dans le Domaine « Innover », car la compétence « innover » s'y développe.
        //   - Mesure et incertitudes figure en tête du programme de sciences physiques. On le range comme les
        //     autres thèmes, sous un Domaine « Sciences physiques — ».
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   SI1 44 (Innover 5, Analyser 12, Modéliser et résoudre 19, Expérimenter et simuler 3, Communiquer 5)
        //   SIT 61 = sciences de l'ingénieur 36 (Innover 5, Analyser 13, Modéliser et résoudre 9,
        //            Expérimenter et simuler 6, Communiquer 3) + sciences physiques 25 (Mesure et incertitudes 4,
        //            Mouvement et interactions 9, Énergie 5, Ondes et signaux 7)
        //   total 105
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — SCIENCES DE L'INGÉNIEUR (items « 1e » de l'annexe de MENE1901640A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SI1 =
        {
            // Créer des produits innovants — Innover
            ("PREMIERE", "SI1_HISTOIRE_INNOVATIONS",      "Innover", "Situer une innovation de rupture ou incrémentale à l'aide d'éléments d'histoire des produits", 1),
            ("PREMIERE", "SI1_IMAGINER_CREATIVITE",       "Innover", "Imaginer une solution originale par brainstorming, analogie, détournement d'usage ou carte heuristique", 2),
            ("PREMIERE", "SI1_SCENARIO_USAGE",            "Innover", "Décrire un scénario d'usage et l'expérience utilisateur visée par une solution", 3),
            ("PREMIERE", "SI1_INTERFACE_ERGONOMIE",       "Innover", "Justifier un choix de design d'interface et d'interaction par des éléments d'ergonomie", 4),
            ("PREMIERE", "SI1_PROJET_12H",                "Innover", "Imaginer et matérialiser en équipe tout ou partie d'une solution originale lors du projet de 12 heures", 5),

            // Analyser les produits existants pour appréhender leur complexité
            ("PREMIERE", "SI1_BESOIN_EXIGENCES",          "Analyser", "Identifier le besoin, les exigences et leurs critères à l'aide des diagrammes d'ingénierie-système (SysML)", 6),
            ("PREMIERE", "SI1_CAS_UTILISATION",           "Analyser", "Lire un diagramme de cas d'utilisation et relier les acteurs aux services rendus par le produit", 7),
            ("PREMIERE", "SI1_ANALYSE_STRUCTURELLE",      "Analyser", "Décrire l'organisation matérielle et fonctionnelle d'un produit par une analyse structurelle", 8),
            ("PREMIERE", "SI1_GRANDEURS_PHYSIQUES",       "Analyser", "Identifier les grandeurs mécaniques, électriques ou thermiques mobilisées par le fonctionnement d'un produit", 9),
            ("PREMIERE", "SI1_EFFORT_FLUX",               "Analyser", "Associer à chaque procédé sa grandeur d'effort et sa grandeur de flux (force et vitesse, tension et courant…)", 10),
            ("PREMIERE", "SI1_ECHANGES_ENERGIE",          "Analyser", "Repérer les échanges d'énergie sur un diagramme structurel", 11),
            ("PREMIERE", "SI1_RENDEMENT_PERTES",          "Analyser", "Calculer un rendement et localiser les pertes dans une chaîne de puissance", 12),
            ("PREMIERE", "SI1_PROTOCOLES_TRAMES",         "Analyser", "Expliquer protocole, trame et encapsulation dans un réseau de communication", 13),
            ("PREMIERE", "SI1_SUPPORTS_COMMUNICATION",    "Analyser", "Comparer les supports de communication filaires et sans fil", 14),
            ("PREMIERE", "SI1_ERREURS_PRECISION",         "Analyser", "Estimer les erreurs et la précision d'une mesure expérimentale ou d'un résultat simulé", 15),
            ("PREMIERE", "SI1_TRAITEMENT_DONNEES",        "Analyser", "Traiter des données par tableau, graphique, valeur moyenne, écart type et incertitude de mesure", 16),
            ("PREMIERE", "SI1_ECARTS_PERFORMANCES",       "Analyser", "Quantifier l'écart entre valeurs attendues, mesurées et simulées en choisissant un critère de comparaison", 17),

            // Modéliser les produits pour prévoir leurs performances — Modéliser et résoudre
            ("PREMIERE", "SI1_HYPOTHESES",                "Modéliser et résoudre", "Proposer et justifier des hypothèses simplificatrices, dont le passage à une modélisation plane", 18),
            ("PREMIERE", "SI1_ENTREES_SORTIES",           "Modéliser et résoudre", "Caractériser les grandeurs effort et flux en entrée et en sortie d'un modèle multi-physique", 19),
            ("PREMIERE", "SI1_PUISSANCE_ENERGIE",         "Modéliser et résoudre", "Calculer une énergie, une puissance instantanée et une puissance moyenne", 20),
            ("PREMIERE", "SI1_REVERSIBILITE_CHAINE",      "Modéliser et résoudre", "Indiquer si une chaîne de puissance modélisée est réversible et justifier la réponse", 21),
            ("PREMIERE", "SI1_SOURCES_INTERRUPTEURS",     "Modéliser et résoudre", "Modéliser un composant par une source parfaite de flux ou d'effort ou par un interrupteur parfait", 22),
            ("PREMIERE", "SI1_COMPOSANTS_CHAINE",         "Modéliser et résoudre", "Associer un modèle aux composants de transformation, modulation, conversion ou stockage de l'énergie", 23),
            ("PREMIERE", "SI1_ALGORITHME",                "Modéliser et résoudre", "Traduire un comportement attendu par un algorithme : variables, fonctions, boucles et conditions", 24),
            ("PREMIERE", "SI1_ETATS_TRANSITIONS",         "Modéliser et résoudre", "Décrire un comportement séquentiel par un diagramme d'états-transitions", 25),
            ("PREMIERE", "SI1_CIRCUIT_ELECTRIQUE",        "Modéliser et résoudre", "Représenter une structure électrique par un schéma de circuit", 26),
            ("PREMIERE", "SI1_SCHEMA_CINEMATIQUE",        "Modéliser et résoudre", "Construire le schéma cinématique et le graphe des liaisons et des actions mécaniques d'un mécanisme", 27),
            ("PREMIERE", "SI1_LIAISONS_TORSEURS",         "Modéliser et résoudre", "Modéliser une liaison par ses mouvements possibles et ses torseurs cinématique et d'actions transmissibles", 28),
            ("PREMIERE", "SI1_ACTIONS_MECANIQUES",        "Modéliser et résoudre", "Modéliser une action mécanique de contact ou à distance et relier mouvement relatif et actions associées", 29),
            ("PREMIERE", "SI1_SIGNAUX_DONNEES",           "Modéliser et résoudre", "Caractériser la nature des signaux, des données et du support d'un échange d'informations", 30),
            ("PREMIERE", "SI1_DEBIT",                     "Modéliser et résoudre", "Distinguer et calculer le débit maximal et le débit utile d'une transmission", 31),
            ("PREMIERE", "SI1_CAPTEURS",                  "Modéliser et résoudre", "Identifier le rôle d'un capteur et la grandeur qu'il convertit en information", 32),
            ("PREMIERE", "SI1_KIRCHHOFF",                 "Modéliser et résoudre", "Déterminer courants et tensions d'un circuit par les lois de Kirchhoff et les lois de comportement", 33),
            ("PREMIERE", "SI1_CINEMATIQUE_VECTEURS",      "Modéliser et résoudre", "Exprimer positions, vitesses et accélérations linéaires et angulaires sous forme vectorielle", 34),
            ("PREMIERE", "SI1_COMPOSITION_VITESSES",      "Modéliser et résoudre", "Utiliser le champ des vitesses et la composition des vitesses dans une chaîne ouverte", 35),
            ("PREMIERE", "SI1_LOI_ENTREE_SORTIE",         "Modéliser et résoudre", "Établir la loi d'entrée-sortie d'une chaîne fermée par fermeture géométrique", 36),

            // Valider les performances par les expérimentations et les simulations — Expérimenter et simuler
            ("PREMIERE", "SI1_ORDRE_GRANDEUR_MESURE",     "Expérimenter et simuler", "Prévoir l'ordre de grandeur d'une mesure, choisir l'appareil ou le capteur adapté et identifier les erreurs", 37),
            ("PREMIERE", "SI1_ESSAIS_SECURITE",           "Expérimenter et simuler", "Conduire des essais en sécurité selon un protocole fourni en raccordant correctement appareils et capteurs", 38),
            ("PREMIERE", "SI1_RELEVER_PROTOCOLE",         "Expérimenter et simuler", "Relever les caractéristiques des signaux, la trame et le débit d'un protocole de communication", 39),

            // S'informer, choisir, produire de l'information — Communiquer
            ("PREMIERE", "SI1_RENDRE_COMPTE",             "Communiquer", "Rendre compte de résultats par un tableau, un graphique, un diaporama ou une carte mentale", 40),
            ("PREMIERE", "SI1_RECHERCHE_INFORMATION",     "Communiquer", "Collecter, comparer et synthétiser des informations tirées d'un dossier technique, d'internet ou d'une base de données", 41),
            ("PREMIERE", "SI1_TUTORIEL",                  "Communiquer", "Réaliser un tutoriel ou une communication à distance avec un montage audio ou vidéo", 42),
            ("PREMIERE", "SI1_COLLABORER",                "Communiquer", "Travailler en équipe via un espace de fichiers partagés et trouver un tiers expert", 43),
            ("PREMIERE", "SI1_ADAPTER_PUBLIC",            "Communiquer", "Adapter un document au public visé en choisissant médias, croquis, schémas ou diagrammes d'ingénierie-système", 44),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE — SCIENCES DE L'INGÉNIEUR (items « Tale » de MENE1901640A)
        //    + SCIENCES PHYSIQUES, COMPLÉMENT DE SI (annexe de MENE1921269A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SIT =
        {
            // Créer des produits innovants — Innover
            ("TERMINALE", "SIT_DEMARCHE_INNOVATION",      "Innover", "Élaborer une démarche d'innovation : veille technologique, approche design, méthodes agiles et leurs limites", 1),
            ("TERMINALE", "SIT_REPRESENTER_SOLUTION",     "Innover", "Représenter une solution originale avec un outil graphique numérique ou un modeleur volumique", 2),
            ("TERMINALE", "SIT_PROTOTYPER",               "Innover", "Matérialiser une solution par prototypage rapide, y compris le prototypage de la commande", 3),
            ("TERMINALE", "SIT_EVALUER_SOLUTION",         "Innover", "Évaluer une solution innovante par des mesures et des tests de performances, dans une démarche d'amélioration continue", 4),
            ("TERMINALE", "SIT_PROJET_48H",               "Innover", "Mener en équipe le projet de 48 heures : réalisations numérique et matérielle, programmation, note interdisciplinaire", 5),

            // Analyser les produits existants pour appréhender leur complexité
            ("TERMINALE", "SIT_REVERSIBILITE",            "Analyser", "Analyser le sens des transmissions de puissance, le stockage de l'énergie et la réversibilité des constituants", 6),
            ("TERMINALE", "SIT_TRAITEMENT_INFORMATION",   "Analyser", "Analyser le traitement de l'information d'un produit en lisant un algorithme ou un programme", 7),
            ("TERMINALE", "SIT_INTELLIGENCE_ARTIFICIELLE","Analyser", "Présenter une intelligence artificielle comme une relation entrées/sorties (apprentissage, moteur d'inférence)", 8),
            ("TERMINALE", "SIT_EVENEMENTS_DISCRETS",      "Analyser", "Analyser le comportement d'un objet à événements discrets à partir d'un diagramme d'états-transitions", 9),
            ("TERMINALE", "SIT_CLIENT_SERVEUR",           "Analyser", "Analyser les échanges d'un système avec un réseau : architecture client/serveur, cloud, architecture du réseau", 10),
            ("TERMINALE", "SIT_DEBIT_TRANSMISSION",       "Analyser", "Quantifier un flux d'informations par la quantité de données et le débit ou la vitesse de transmission", 11),
            ("TERMINALE", "SIT_MODULATION",               "Analyser", "Expliquer qualitativement une modulation-démodulation numérique en amplitude ou en fréquence", 12),
            ("TERMINALE", "SIT_ASSERVISSEMENT_STRUCTURE", "Analyser", "Analyser un système asservi linéaire en régime permanent : chaîne directe ou bouclée, comparateur, perturbation", 13),
            ("TERMINALE", "SIT_ERREUR_STATIQUE",          "Analyser", "Relier le correcteur proportionnel à la précision d'un asservissement et à son erreur statique", 14),
            ("TERMINALE", "SIT_CHARGES_OUVRAGE",          "Analyser", "Identifier les charges permanentes et d'exploitation appliquées à un ouvrage ou à une structure", 15),
            ("TERMINALE", "SIT_RESULTATS_EXPERIENCE",     "Analyser", "Analyser des résultats d'expérience ou de simulation à l'aide des lois physiques et des critères de performance", 16),
            ("TERMINALE", "SIT_CAUSES_ECARTS",            "Analyser", "Calculer un écart de performance absolu ou relatif et proposer des causes aux écarts constatés", 17),
            ("TERMINALE", "SIT_VALIDER_MODELE_ETABLI",    "Analyser", "Valider ou invalider un modèle du comportement d'un objet à partir des écarts observés", 18),

            // Modéliser les produits pour prévoir leurs performances — Modéliser et résoudre
            ("TERMINALE", "SIT_PROGRAMME_EXECUTABLE",     "Modéliser et résoudre", "Traduire un algorithme en un programme exécutable (langage Python)", 19),
            ("TERMINALE", "SIT_MODELE_ASSERVI",           "Modéliser et résoudre", "Associer un modèle à un système asservi : consigne, grandeur de sortie, perturbation, erreur, correcteur proportionnel", 20),
            ("TERMINALE", "SIT_MODELE_CONNAISSANCE",      "Modéliser et résoudre", "Élaborer un modèle de connaissance d'ordre 0, 1 ou 2 (gain pur, intégrateur, dérivateur) à partir des lois effort-flux", 21),
            ("TERMINALE", "SIT_STATIQUE",                 "Modéliser et résoudre", "Déterminer les actions mécaniques d'équilibre d'un mécanisme ou d'une structure par le principe fondamental de la statique", 22),
            ("TERMINALE", "SIT_FROTTEMENT_COULOMB",       "Modéliser et résoudre", "Modéliser un frottement par la loi de Coulomb dans un problème d'équilibre", 23),
            ("TERMINALE", "SIT_DYNAMIQUE_FLUX",           "Modéliser et résoudre", "Déterminer une vitesse linéaire ou angulaire imposée par les actions mécaniques avec le principe fondamental de la dynamique", 24),
            ("TERMINALE", "SIT_DYNAMIQUE_EFFORT",         "Modéliser et résoudre", "Déterminer la force ou le couple nécessaire pour imposer une translation ou une rotation autour d'un axe fixe", 25),
            ("TERMINALE", "SIT_INERTIE_EQUIVALENTE",      "Modéliser et résoudre", "Utiliser la notion d'inertie et calculer une inertie équivalente dans une chaîne de transmission", 26),
            ("TERMINALE", "SIT_RESOLUTION_EQUATIONS",     "Modéliser et résoudre", "Quantifier les performances d'un objet en résolvant ses équations de façon analytique ou numérique", 27),

            // Valider les performances par les expérimentations et les simulations — Expérimenter et simuler
            ("TERMINALE", "SIT_PROTOCOLE_EXPERIMENTAL",   "Expérimenter et simuler", "Proposer et justifier un protocole expérimental permettant de quantifier une performance", 28),
            ("TERMINALE", "SIT_CHAINE_ACQUISITION",       "Expérimenter et simuler", "Instrumenter un produit : choisir les capteurs, paramétrer la chaîne d'acquisition, utiliser une carte microcontrôleur", 29),
            ("TERMINALE", "SIT_COMMUNICATION_OBJETS",     "Expérimenter et simuler", "Mettre en œuvre une communication entre objets connectés en configurant les paramètres du réseau", 30),
            ("TERMINALE", "SIT_OPTIMISER_COMMANDE",       "Expérimenter et simuler", "Modifier les paramètres influents et le programme de commande pour optimiser les performances du produit", 31),
            ("TERMINALE", "SIT_PARAMETRER_SIMULATION",    "Expérimenter et simuler", "Paramétrer une simulation multi-physique : durée, incrément temporel, grandeurs affichées, échelles adaptées", 32),
            ("TERMINALE", "SIT_VALIDER_MODELE_NUMERIQUE", "Expérimenter et simuler", "Valider un modèle numérique en comparant performances simulées et mesurées, et préciser ses limites de validité", 33),

            // S'informer, choisir, produire de l'information — Communiquer
            ("TERMINALE", "SIT_PRESENTER_SOLUTION",       "Communiquer", "Présenter une idée, un protocole ou une solution par des diagrammes fonctionnels, des schémas et des croquis", 34),
            ("TERMINALE", "SIT_DOCUMENTER_PROGRAMME",     "Communiquer", "Documenter un programme informatique par des commentaires pertinents", 35),
            ("TERMINALE", "SIT_CONVAINCRE",               "Communiquer", "Communiquer de façon convaincante en travaillant placement de la voix, qualité d'expression et gestion du temps", 36),

            // Sciences physiques — Mesure et incertitudes
            ("TERMINALE", "SIT_SP_SERIE_MESURES",         "Sciences physiques — Mesure et incertitudes", "Exploiter une série de mesures indépendantes : histogramme, moyenne, écart-type, influence de l'instrument", 37),
            ("TERMINALE", "SIT_SP_INCERTITUDE_TYPE",      "Sciences physiques — Mesure et incertitudes", "Évaluer une incertitude-type par une approche statistique (type A) ou non statistique (type B)", 38),
            ("TERMINALE", "SIT_SP_INCERTITUDES_COMPOSEES","Sciences physiques — Mesure et incertitudes", "Évaluer une incertitude-type composée à l'aide d'une formule fournie", 39),
            ("TERMINALE", "SIT_SP_COMPARER_REFERENCE",    "Sciences physiques — Mesure et incertitudes", "Écrire un résultat avec des chiffres significatifs adaptés et le comparer à une référence par |mmes − mref|/u(m)", 40),

            // Sciences physiques — Mouvement et interactions
            ("TERMINALE", "SIT_SP_REPERE_FRENET",         "Sciences physiques — Mouvement et interactions", "Exploiter les coordonnées des vecteurs vitesse et accélération dans le repère de Frenet d'un mouvement circulaire", 41),
            ("TERMINALE", "SIT_SP_VECTEUR_ACCELERATION",  "Sciences physiques — Mouvement et interactions", "Caractériser le vecteur accélération des mouvements rectilignes et circulaires, uniformes ou non", 42),
            ("TERMINALE", "SIT_SP_CHRONOPHOTOGRAPHIE",    "Sciences physiques — Mouvement et interactions", "Exploiter une vidéo ou une chronophotographie pour obtenir position, vitesse et accélération", 43),
            ("TERMINALE", "SIT_SP_REFERENTIEL_GALILEEN",  "Sciences physiques — Mouvement et interactions", "Justifier la position du centre de masse et discuter du caractère galiléen d'un référentiel", 44),
            ("TERMINALE", "SIT_SP_DEUXIEME_LOI_NEWTON",   "Sciences physiques — Mouvement et interactions", "Utiliser la deuxième loi de Newton pour relier les forces appliquées à l'accélération du centre de masse", 45),
            ("TERMINALE", "SIT_SP_CHAMP_UNIFORME",        "Sciences physiques — Mouvement et interactions", "Établir équations horaires et trajectoire d'un mouvement dans un champ de pesanteur ou électrique uniforme", 46),
            ("TERMINALE", "SIT_SP_CONDENSATEUR_PLAN",     "Sciences physiques — Mouvement et interactions", "Discuter de l'influence des grandeurs physiques sur le champ électrique d'un condensateur plan", 47),
            ("TERMINALE", "SIT_SP_ENERGIE_MECANIQUE",     "Sciences physiques — Mouvement et interactions", "Exploiter la conservation de l'énergie mécanique ou le théorème de l'énergie cinétique en champ uniforme", 48),
            ("TERMINALE", "SIT_SP_KEPLER",                "Sciences physiques — Mouvement et interactions", "Étudier un mouvement circulaire dans un champ de gravitation et établir la troisième loi de Kepler", 49),

            // Sciences physiques — L'énergie : conversions et transferts
            ("TERMINALE", "SIT_SP_ENERGIE_INTERNE",       "Sciences physiques — L'énergie : conversions et transferts", "Citer les contributions microscopiques à l'énergie interne d'un système", 50),
            ("TERMINALE", "SIT_SP_PREMIER_PRINCIPE",      "Sciences physiques — L'énergie : conversions et transferts", "Faire un bilan d'énergie avec le premier principe en séparant variation d'énergie du système et transferts", 51),
            ("TERMINALE", "SIT_SP_CAPACITE_THERMIQUE",    "Sciences physiques — L'énergie : conversions et transferts", "Utiliser la variation d'énergie interne d'un système incompressible, ΔU = C·ΔT, dans un bilan", 52),
            ("TERMINALE", "SIT_SP_TRANSFERTS_THERMIQUES", "Sciences physiques — L'énergie : conversions et transferts", "Décrire conduction, convection et rayonnement et relier flux thermique, résistance thermique et écart de température", 53),
            ("TERMINALE", "SIT_SP_LOI_NEWTON_THERMIQUE",  "Sciences physiques — L'énergie : conversions et transferts", "Établir l'évolution de la température d'un système au contact d'un thermostat avec la loi de Newton fournie", 54),

            // Sciences physiques — Ondes et signaux
            ("TERMINALE", "SIT_SP_NIVEAU_SONORE",         "Sciences physiques — Ondes et signaux", "Exploiter le niveau d'intensité sonore en décibels et illustrer les atténuations géométrique et par absorption", 55),
            ("TERMINALE", "SIT_SP_DIFFRACTION",           "Sciences physiques — Ondes et signaux", "Caractériser la diffraction et relier angle caractéristique, longueur d'onde et taille de l'ouverture", 56),
            ("TERMINALE", "SIT_SP_INTERFERENCES",         "Sciences physiques — Ondes et signaux", "Établir les conditions d'interférences constructives ou destructives et exploiter l'interfrange", 57),
            ("TERMINALE", "SIT_SP_EFFET_DOPPLER",         "Sciences physiques — Ondes et signaux", "Expliquer l'effet Doppler, établir le décalage Doppler et l'exploiter pour déterminer une vitesse", 58),
            ("TERMINALE", "SIT_SP_EFFET_PHOTOELECTRIQUE", "Sciences physiques — Ondes et signaux", "Décrire l'effet photoélectrique et l'interpréter avec le modèle particulaire de la lumière", 59),
            ("TERMINALE", "SIT_SP_BILAN_PHOTON",          "Sciences physiques — Ondes et signaux", "Établir par un bilan d'énergie la relation entre énergie cinétique des électrons et fréquence de la lumière", 60),
            ("TERMINALE", "SIT_SP_PHOTOVOLTAIQUE",        "Sciences physiques — Ondes et signaux", "Expliquer le fonctionnement d'une cellule photoélectrique et déterminer le rendement d'une cellule photovoltaïque", 61),
        };
    }
}
