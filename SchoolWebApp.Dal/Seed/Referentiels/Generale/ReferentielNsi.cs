namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// La spécialité numérique et sciences informatiques de la voie générale, première et terminale — provenance détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielNsi
    {
        // =====================================================================================
        // RÉFÉRENTIEL DE LA SPÉCIALITÉ NUMÉRIQUE ET SCIENCES INFORMATIQUES (NSI) — VOIE GÉNÉRALE
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis le texte
        // officiel téléchargé et lu le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\nsi\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau    | Intitulé exact (page de titre de l'annexe)                        |
        // |---------|-----------|-------------------------------------------------------------------|
        // | NSI1    | PREMIERE  | Programme de numérique et sciences informatiques de première générale |
        // | NSIT    | TERMINALE | Programme de numérique et sciences informatiques de terminale générale |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901633A,
        //   BO spécial n° 1 du 22 janvier 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2019 ».
        //   Une seule annexe, lue en entier : huit rubriques.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901633A.htm
        //   Annexe : https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/26/8/spe633_annexe_1063268.pdf
        //            (copie éduscol spe633annexe1063268pdf-89499.pdf : md5 identique, 41de6e1e…)
        //
        // TERMINALE : arrêté du 19-7-2019, J.O. du 23-7-2019, NOR MENE1921247A,
        //   BO spécial n° 8 du 25 juillet 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2020 ».
        //   Une seule annexe, lue en entier : six rubriques.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921247A.htm
        //   Annexe : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/93/3/spe247_annexe_1158933.pdf
        //            (copie éduscol spe247annexe1158933pdf-89502.pdf : md5 identique, e27f817b…)
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : la page éduscol « Programmes et ressources en numérique et
        //   sciences informatiques - voie G » (https://eduscol.education.gouv.fr/5823/programmes-et-ressources-en-numerique-et-sciences-informatiques-voie-g,
        //   datée « février 2026 ») liste ces deux textes, et eux seuls, sous « Programme en vigueur ».
        //   Aucun arrêté modificatif du programme n'a été trouvé de 2019 à 2026. Les textes postérieurs ne touchent
        //   que l'ÉPREUVE : MENE2323020N (26-9-2023, tout le programme de Tle évaluable dès la session 2024) et
        //   MENE2516123N (4-7-2025, BO n° 31 du 21-8-2025, nouvelle épreuve à compter de la session 2026).
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - Première : huit rubriques, dont « Histoire de l'informatique », transversale. Le texte ne l'enseigne
        //     pas à part : il la décline dans les sept autres. Elle garde ici une ligne.
        //   - Terminale : six rubriques, et non huit. La « Représentation des données », le « Traitement de données
        //     en tables » et les « Interactions sur le Web » n'existent qu'en première.
        //   - L'ancienne note d'épreuve (MENE2001797N, 11-2-2020) date l'arrêté de terminale du « 17 juillet 2019 ».
        //     La page du BO porte « du 19-7-2019 ». C'est la date du BO qui est retenue.
        //   - Quelques lignes reprennent la colonne « Commentaires », quand elle fixe un attendu vérifiable :
        //     0.2 + 0.1 n'est pas égal à 0.3, xor, doublons, SQL sans GROUP BY, tri fusion en n log2 n.
        //     Le texte y marque aussi ce qui n'est PAS exigible : norme IEEE-754, tranches (slices), coût de Boyer-Moore,
        //     polymorphisme et héritage, négociation SSL. Aucune ligne ne porte ces points.
        //   - Pour 2026-2027, aucune restriction de programme ne s'applique. La limitation de 2022 (MENE2227884N)
        //     est abrogée par MENE2323020N depuis la session 2024.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   NSI1 57 · NSIT 61 · total 118
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE GÉNÉRALE — NUMÉRIQUE ET SCIENCES INFORMATIQUES (annexe de MENE1901633A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] NSI1 =
        {
            // Histoire de l'informatique (rubrique transversale)
            ("PREMIERE", "NSI1_HIST_EVENEMENTS",          "Histoire de l'informatique", "Situer dans le temps les principaux événements de l'histoire de l'informatique et leurs protagonistes", 1),

            // Représentation des données : types et valeurs de base
            ("PREMIERE", "NSI1_BASE_CONVERSION",          "Représentation des données : types et valeurs de base", "Passer de la représentation d'un entier positif dans une base à une autre (bases 2, 10 et 16)", 2),
            ("PREMIERE", "NSI1_ENTIER_NB_BITS",           "Représentation des données : types et valeurs de base", "Évaluer le nombre de bits nécessaires pour écrire en base 2 un entier, la somme ou le produit de deux entiers", 3),
            ("PREMIERE", "NSI1_COMPLEMENT_A_DEUX",        "Représentation des données : types et valeurs de base", "Utiliser le complément à 2 pour représenter un entier relatif en binaire (8, 16, 32 ou 64 bits)", 4),
            ("PREMIERE", "NSI1_FLOTTANTS",                "Représentation des données : types et valeurs de base", "Calculer la représentation de réels comme 0.1, 0.25 ou 1/3 et expliquer pourquoi 0.2 + 0.1 n'est pas égal à 0.3", 5),
            ("PREMIERE", "NSI1_TABLE_BOOLEENNE",          "Représentation des données : types et valeurs de base", "Dresser la table d'une expression booléenne construite avec and, or et not", 6),
            ("PREMIERE", "NSI1_XOR_ADDITION",             "Représentation des données : types et valeurs de base", "Utiliser le ou exclusif (xor) et l'appliquer à l'addition binaire", 7),
            ("PREMIERE", "NSI1_OPERATEURS_SEQUENTIELS",   "Représentation des données : types et valeurs de base", "Expliquer le caractère séquentiel des opérateurs and et or : le second opérande n'est pas toujours évalué", 8),
            ("PREMIERE", "NSI1_ENCODAGES_INTERET",        "Représentation des données : types et valeurs de base", "Identifier l'intérêt des différents systèmes d'encodage d'un texte : ASCII, ISO-8859-1, Unicode", 9),
            ("PREMIERE", "NSI1_ENCODAGE_CONVERSION",      "Représentation des données : types et valeurs de base", "Convertir un fichier texte dans différents formats d'encodage", 10),

            // Représentation des données : types construits
            ("PREMIERE", "NSI1_TUPLE_RENVOI",             "Représentation des données : types construits", "Écrire une fonction renvoyant un p-uplet de valeurs", 11),
            ("PREMIERE", "NSI1_TABLEAU_INDEX",            "Représentation des données : types construits", "Lire et modifier les éléments d'un tableau grâce à leurs index", 12),
            ("PREMIERE", "NSI1_TABLEAU_COMPREHENSION",    "Représentation des données : types construits", "Construire un tableau par compréhension", 13),
            ("PREMIERE", "NSI1_MATRICE",                  "Représentation des données : types construits", "Utiliser des tableaux de tableaux pour représenter une matrice avec la notation a[i][j]", 14),
            ("PREMIERE", "NSI1_TABLEAU_ITERER",           "Représentation des données : types construits", "Itérer sur les éléments d'un tableau", 15),
            ("PREMIERE", "NSI1_DICO_ENTREE",              "Représentation des données : types construits", "Construire une entrée de dictionnaire associant une clé à une valeur", 16),
            ("PREMIERE", "NSI1_DICO_ITERER",              "Représentation des données : types construits", "Itérer sur les éléments d'un dictionnaire avec les méthodes keys(), values() et items()", 17),

            // Traitement de données en tables
            ("PREMIERE", "NSI1_TABLE_IMPORT",             "Traitement de données en tables", "Importer une table depuis un fichier texte tabulé ou un fichier CSV", 18),
            ("PREMIERE", "NSI1_TABLE_RECHERCHE",          "Traitement de données en tables", "Rechercher les lignes d'une table vérifiant des critères exprimés en logique propositionnelle", 19),
            ("PREMIERE", "NSI1_TABLE_COHERENCE",          "Traitement de données en tables", "Rechercher les doublons et tester la cohérence d'une table", 20),
            ("PREMIERE", "NSI1_TABLE_TRI",                "Traitement de données en tables", "Trier une table suivant une colonne", 21),
            ("PREMIERE", "NSI1_TABLE_FUSION",             "Traitement de données en tables", "Construire une nouvelle table en combinant les données de deux tables", 22),

            // Interactions entre l'homme et la machine sur le Web
            ("PREMIERE", "NSI1_WEB_COMPOSANTS",           "Interactions entre l'homme et la machine sur le Web", "Identifier les différents composants graphiques permettant d'interagir avec une application Web", 23),
            ("PREMIERE", "NSI1_WEB_EVENEMENTS",           "Interactions entre l'homme et la machine sur le Web", "Identifier les événements que les fonctions associées aux composants graphiques sont capables de traiter", 24),
            ("PREMIERE", "NSI1_WEB_CLIC_BOUTON",          "Interactions entre l'homme et la machine sur le Web", "Analyser et modifier les méthodes exécutées lors d'un clic sur un bouton d'une page Web", 25),
            ("PREMIERE", "NSI1_CLIENT_SERVEUR_ORDRE",     "Interactions entre l'homme et la machine sur le Web", "Distinguer ce qui est exécuté sur le client ou sur le serveur, et dans quel ordre", 26),
            ("PREMIERE", "NSI1_CLIENT_MEMOIRE",           "Interactions entre l'homme et la machine sur le Web", "Distinguer ce qui est mémorisé dans le client et retransmis au serveur", 27),
            ("PREMIERE", "NSI1_TRANSMISSION_CHIFFREE",    "Interactions entre l'homme et la machine sur le Web", "Reconnaître quand et pourquoi la transmission est chiffrée", 28),
            ("PREMIERE", "NSI1_FORMULAIRE",               "Interactions entre l'homme et la machine sur le Web", "Analyser le fonctionnement d'un formulaire simple d'une page Web", 29),
            ("PREMIERE", "NSI1_GET_POST",                 "Interactions entre l'homme et la machine sur le Web", "Distinguer les transmissions de paramètres par les requêtes GET ou POST selon les valeurs et leur confidentialité", 30),

            // Architectures matérielles et systèmes d'exploitation
            ("PREMIERE", "NSI1_VON_NEUMANN",              "Architectures matérielles et systèmes d'exploitation", "Distinguer les rôles et les caractéristiques des différents constituants d'une machine (modèle de von Neumann)", 31),
            ("PREMIERE", "NSI1_LANGAGE_MACHINE",          "Architectures matérielles et systèmes d'exploitation", "Dérouler l'exécution d'une séquence d'instructions simples du type langage machine", 32),
            ("PREMIERE", "NSI1_PAQUETS_ENCAPSULATION",    "Architectures matérielles et systèmes d'exploitation", "Mettre en évidence l'intérêt du découpage des données en paquets et de leur encapsulation", 33),
            ("PREMIERE", "NSI1_BIT_ALTERNE",              "Architectures matérielles et systèmes d'exploitation", "Dérouler le fonctionnement d'un protocole simple de récupération de perte de paquets (bit alterné)", 34),
            ("PREMIERE", "NSI1_RESEAU_SIMULER",           "Architectures matérielles et systèmes d'exploitation", "Simuler ou mettre en œuvre un réseau et décrire le rôle de ses constituants", 35),
            ("PREMIERE", "NSI1_OS_FONCTIONS",             "Architectures matérielles et systèmes d'exploitation", "Identifier les fonctions d'un système d'exploitation", 36),
            ("PREMIERE", "NSI1_LIGNE_COMMANDE",           "Architectures matérielles et systèmes d'exploitation", "Utiliser les commandes de base en ligne de commande", 37),
            ("PREMIERE", "NSI1_DROITS_FICHIERS",          "Architectures matérielles et systèmes d'exploitation", "Gérer les droits et permissions d'accès aux fichiers", 38),
            ("PREMIERE", "NSI1_CAPTEURS_ACTIONNEURS",     "Architectures matérielles et systèmes d'exploitation", "Identifier le rôle des capteurs et des actionneurs", 39),
            ("PREMIERE", "NSI1_IHM_PROGRAMMER",           "Architectures matérielles et systèmes d'exploitation", "Réaliser par programmation une IHM répondant à un cahier des charges donné", 40),

            // Langages et programmation
            ("PREMIERE", "NSI1_CONSTRUCTIONS_ELEMENTAIRES", "Langages et programmation", "Mettre en évidence les constructions élémentaires : séquence, affectation, conditionnelle, boucles, appel de fonction", 41),
            ("PREMIERE", "NSI1_LANGAGES_TRAITS",          "Langages et programmation", "Repérer, dans un nouveau langage de programmation, les traits communs et les traits particuliers à ce langage", 42),
            ("PREMIERE", "NSI1_PROTOTYPER",               "Langages et programmation", "Prototyper une fonction : nom, paramètres et valeur renvoyée", 43),
            ("PREMIERE", "NSI1_PRECONDITIONS",            "Langages et programmation", "Décrire les préconditions sur les arguments d'une fonction, au besoin garanties par des assertions", 44),
            ("PREMIERE", "NSI1_POSTCONDITIONS",           "Langages et programmation", "Décrire des postconditions sur les résultats d'une fonction", 45),
            ("PREMIERE", "NSI1_JEUX_TESTS",               "Langages et programmation", "Utiliser des jeux de tests et expliquer que leur succès ne garantit pas la correction d'un programme", 46),
            ("PREMIERE", "NSI1_DOC_BIBLIOTHEQUE",         "Langages et programmation", "Utiliser la documentation d'une bibliothèque", 47),

            // Algorithmique
            ("PREMIERE", "NSI1_RECHERCHE_OCCURRENCE",     "Algorithmique", "Écrire un algorithme de recherche d'une occurrence dans un tableau et montrer que son coût est linéaire", 48),
            ("PREMIERE", "NSI1_EXTREMUM_MOYENNE",         "Algorithmique", "Écrire un algorithme de recherche d'un extremum et de calcul d'une moyenne par parcours séquentiel", 49),
            ("PREMIERE", "NSI1_TRI_INSERTION",            "Algorithmique", "Écrire l'algorithme de tri par insertion", 50),
            ("PREMIERE", "NSI1_TRI_SELECTION",            "Algorithmique", "Écrire l'algorithme de tri par sélection", 51),
            ("PREMIERE", "NSI1_TRI_INVARIANT",            "Algorithmique", "Décrire un invariant de boucle qui prouve la correction des tris par insertion et par sélection", 52),
            ("PREMIERE", "NSI1_TRI_TERMINAISON_COUT",     "Algorithmique", "Justifier la terminaison des tris par insertion et par sélection et montrer leur coût quadratique dans le pire cas", 53),
            ("PREMIERE", "NSI1_KNN",                      "Algorithmique", "Écrire un algorithme qui prédit la classe d'un élément selon la classe majoritaire de ses k plus proches voisins", 54),
            ("PREMIERE", "NSI1_DICHOTOMIE",               "Algorithmique", "Mettre en œuvre la recherche dichotomique dans un tableau trié", 55),
            ("PREMIERE", "NSI1_DICHOTOMIE_VARIANT",       "Algorithmique", "Montrer la terminaison de la recherche dichotomique à l'aide d'un variant de boucle", 56),
            ("PREMIERE", "NSI1_GLOUTON",                  "Algorithmique", "Résoudre un problème grâce à un algorithme glouton, par exemple le rendu de monnaie ou le sac à dos", 57),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE GÉNÉRALE — NUMÉRIQUE ET SCIENCES INFORMATIQUES (annexe de MENE1921247A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] NSIT =
        {
            // Histoire de l'informatique (rubrique transversale)
            ("TERMINALE", "NSIT_HIST_EVENEMENTS",         "Histoire de l'informatique", "Situer dans le temps les principaux événements de l'histoire de l'informatique et leurs protagonistes", 1),
            ("TERMINALE", "NSIT_HIST_LOGICIEL_MATERIEL",  "Histoire de l'informatique", "Identifier l'évolution des rôles relatifs des logiciels et des matériels", 2),

            // Structures de données
            ("TERMINALE", "NSIT_SD_INTERFACE",            "Structures de données", "Spécifier une structure de données par son interface", 3),
            ("TERMINALE", "NSIT_SD_INTERFACE_IMPLEM",     "Structures de données", "Distinguer l'interface d'une structure de données de son implémentation", 4),
            ("TERMINALE", "NSIT_SD_IMPLEMENTATIONS",      "Structures de données", "Écrire plusieurs implémentations d'une même structure, par exemple une file avec un tableau ou avec deux piles", 5),
            ("TERMINALE", "NSIT_POO_CLASSE",              "Structures de données", "Écrire la définition d'une classe", 6),
            ("TERMINALE", "NSIT_POO_ATTRIBUTS_METHODES",  "Structures de données", "Accéder aux attributs et aux méthodes d'une classe", 7),
            ("TERMINALE", "NSIT_LINEAIRES_METHODES",      "Structures de données", "Distinguer listes, piles et files par le jeu des méthodes qui les caractérisent (modes LIFO et FIFO)", 8),
            ("TERMINALE", "NSIT_CHOIX_STRUCTURE",         "Structures de données", "Choisir une structure de données adaptée à la situation à modéliser", 9),
            ("TERMINALE", "NSIT_RECHERCHE_LISTE_DICO",    "Structures de données", "Distinguer la recherche d'une valeur dans une liste et dans un dictionnaire", 10),
            ("TERMINALE", "NSIT_ARBRE_SITUATIONS",        "Structures de données", "Identifier des situations nécessitant une structure de données arborescente", 11),
            ("TERMINALE", "NSIT_ARBRE_VOCABULAIRE",       "Structures de données", "Nommer les éléments d'un arbre binaire : nœuds, racine, feuilles, sous-arbres gauche et droit", 12),
            ("TERMINALE", "NSIT_ARBRE_MESURES",           "Structures de données", "Évaluer quelques mesures d'un arbre binaire : taille, encadrement de la hauteur", 13),
            ("TERMINALE", "NSIT_GRAPHE_MODELISER",        "Structures de données", "Modéliser une situation par un graphe orienté ou non orienté : sommets, arcs, arêtes", 14),
            ("TERMINALE", "NSIT_GRAPHE_IMPLEMENTATIONS",  "Structures de données", "Implémenter un graphe par matrice d'adjacence et par liste de successeurs ou de prédécesseurs", 15),
            ("TERMINALE", "NSIT_GRAPHE_CONVERSION",       "Structures de données", "Passer d'une représentation d'un graphe à une autre", 16),

            // Bases de données
            ("TERMINALE", "NSIT_MODELE_RELATIONNEL",      "Bases de données", "Identifier les concepts du modèle relationnel : relation, attribut, domaine, clef primaire, clef étrangère, schéma", 17),
            ("TERMINALE", "NSIT_CONTRAINTES_INTEGRITE",   "Bases de données", "Exprimer les contraintes d'intégrité de domaine, de relation et de référence", 18),
            ("TERMINALE", "NSIT_STRUCTURE_CONTENU",       "Bases de données", "Distinguer la structure d'une base de données de son contenu", 19),
            ("TERMINALE", "NSIT_ANOMALIES_SCHEMA",        "Bases de données", "Repérer des anomalies dans le schéma d'une base : redondance, anomalies d'insertion, de suppression, de mise à jour", 20),
            ("TERMINALE", "NSIT_SGBD_SERVICES",           "Bases de données", "Identifier les services d'un SGBD relationnel : persistance, accès concurrents, efficacité des requêtes, sécurité", 21),
            ("TERMINALE", "NSIT_SQL_COMPOSANTS",          "Bases de données", "Identifier les composants d'une requête SQL", 22),
            ("TERMINALE", "NSIT_SQL_SELECT",              "Bases de données", "Construire des requêtes d'interrogation avec les clauses SELECT, FROM et WHERE", 23),
            ("TERMINALE", "NSIT_SQL_JOIN",                "Bases de données", "Construire une requête d'interrogation portant sur plusieurs tables avec JOIN", 24),
            ("TERMINALE", "NSIT_SQL_DISTINCT_ORDER",      "Bases de données", "Utiliser DISTINCT, ORDER BY et les fonctions d'agrégation, sans GROUP BY ni HAVING", 25),
            ("TERMINALE", "NSIT_SQL_MISE_A_JOUR",         "Bases de données", "Construire des requêtes d'insertion et de mise à jour avec INSERT, UPDATE et DELETE", 26),

            // Architectures matérielles, systèmes d'exploitation et réseaux
            ("TERMINALE", "NSIT_SOC_COMPOSANTS",          "Architectures matérielles, systèmes d'exploitation et réseaux", "Identifier les principaux composants d'un système sur puce sur un schéma de circuit", 27),
            ("TERMINALE", "NSIT_SOC_AVANTAGES",           "Architectures matérielles, systèmes d'exploitation et réseaux", "Expliquer les avantages de l'intégration des composants sur puce en termes de vitesse et de consommation", 28),
            ("TERMINALE", "NSIT_PROCESSUS_CREATION",      "Architectures matérielles, systèmes d'exploitation et réseaux", "Décrire la création d'un processus par le système d'exploitation", 29),
            ("TERMINALE", "NSIT_ORDONNANCEMENT",          "Architectures matérielles, systèmes d'exploitation et réseaux", "Décrire l'ordonnancement de plusieurs processus par le système d'exploitation", 30),
            ("TERMINALE", "NSIT_INTERBLOCAGE",            "Architectures matérielles, systèmes d'exploitation et réseaux", "Mettre en évidence le risque d'interblocage (deadlock) entre processus", 31),
            ("TERMINALE", "NSIT_ROUTAGE_RIP",             "Architectures matérielles, systèmes d'exploitation et réseaux", "Identifier la route d'un paquet avec le protocole RIP (nombre de sauts), les tables de routage étant données", 32),
            ("TERMINALE", "NSIT_ROUTAGE_OSPF",            "Architectures matérielles, systèmes d'exploitation et réseaux", "Identifier la route d'un paquet avec le protocole OSPF (coût des routes), les tables de routage étant données", 33),
            ("TERMINALE", "NSIT_CHIFFREMENT_SYM_ASYM",    "Architectures matérielles, systèmes d'exploitation et réseaux", "Décrire les principes du chiffrement symétrique (clef partagée) et asymétrique (clef privée, clef publique)", 34),
            ("TERMINALE", "NSIT_HTTPS_ECHANGE_CLEF",      "Architectures matérielles, systèmes d'exploitation et réseaux", "Décrire l'échange d'une clef symétrique par un protocole asymétrique pour sécuriser une communication HTTPS", 35),

            // Langages et programmation
            ("TERMINALE", "NSIT_PROGRAMME_DONNEE",        "Langages et programmation", "Expliquer que tout programme est aussi une donnée, avec l'exemple d'un interpréteur ou d'un compilateur", 36),
            ("TERMINALE", "NSIT_CALCULABILITE",           "Langages et programmation", "Expliquer que la calculabilité ne dépend pas du langage de programmation utilisé", 37),
            ("TERMINALE", "NSIT_ARRET_INDECIDABLE",       "Langages et programmation", "Montrer, sans formalisme théorique, que le problème de l'arrêt est indécidable", 38),
            ("TERMINALE", "NSIT_RECURSIF_ECRIRE",         "Langages et programmation", "Écrire un programme récursif", 39),
            ("TERMINALE", "NSIT_RECURSIF_ANALYSER",       "Langages et programmation", "Analyser le fonctionnement d'un programme récursif", 40),
            ("TERMINALE", "NSIT_API_BIBLIOTHEQUES",       "Langages et programmation", "Utiliser des API ou des bibliothèques et exploiter leur documentation", 41),
            ("TERMINALE", "NSIT_MODULES",                 "Langages et programmation", "Créer des modules simples et les documenter", 42),
            ("TERMINALE", "NSIT_PARADIGMES_DISTINGUER",   "Langages et programmation", "Distinguer sur des exemples les paradigmes impératif, fonctionnel et objet", 43),
            ("TERMINALE", "NSIT_PARADIGME_CHOISIR",       "Langages et programmation", "Choisir le paradigme de programmation selon le champ d'application d'un programme", 44),
            ("TERMINALE", "NSIT_BUGS_TYPAGE_EFFETS",      "Langages et programmation", "Répondre aux causes typiques de bugs liées au typage et aux effets de bord non désirés", 45),
            ("TERMINALE", "NSIT_BUGS_TABLEAUX_CONDITIONS", "Langages et programmation", "Répondre aux bugs de débordement dans les tableaux et d'instruction conditionnelle non exhaustive", 46),
            ("TERMINALE", "NSIT_BUGS_COMPARAISONS",       "Langages et programmation", "Répondre aux bugs liés au choix des inégalités, aux calculs entre flottants et au mauvais nommage des variables", 47),

            // Algorithmique
            ("TERMINALE", "NSIT_ARBRE_TAILLE_HAUTEUR",    "Algorithmique", "Calculer la taille et la hauteur d'un arbre binaire", 48),
            ("TERMINALE", "NSIT_ARBRE_PARCOURS_PROFONDEUR", "Algorithmique", "Parcourir un arbre binaire dans les ordres préfixe, infixe et suffixe", 49),
            ("TERMINALE", "NSIT_ARBRE_PARCOURS_LARGEUR",  "Algorithmique", "Parcourir un arbre binaire en largeur d'abord", 50),
            ("TERMINALE", "NSIT_ABR_RECHERCHE",           "Algorithmique", "Rechercher une clé dans un arbre binaire de recherche et expliquer le coût logarithmique s'il est équilibré", 51),
            ("TERMINALE", "NSIT_ABR_INSERTION",           "Algorithmique", "Insérer une clé dans un arbre binaire de recherche", 52),
            ("TERMINALE", "NSIT_GRAPHE_PARCOURS_PROFONDEUR", "Algorithmique", "Parcourir un graphe en profondeur d'abord", 53),
            ("TERMINALE", "NSIT_GRAPHE_PARCOURS_LARGEUR", "Algorithmique", "Parcourir un graphe en largeur d'abord", 54),
            ("TERMINALE", "NSIT_GRAPHE_CYCLE",            "Algorithmique", "Repérer la présence d'un cycle dans un graphe", 55),
            ("TERMINALE", "NSIT_GRAPHE_CHEMIN",           "Algorithmique", "Chercher un chemin dans un graphe", 56),
            ("TERMINALE", "NSIT_DIVISER_REGNER",          "Algorithmique", "Écrire un algorithme utilisant la méthode « diviser pour régner »", 57),
            ("TERMINALE", "NSIT_TRI_FUSION",              "Algorithmique", "Écrire le tri fusion et justifier son coût en n log2 n dans le pire des cas", 58),
            ("TERMINALE", "NSIT_PROG_DYNAMIQUE",          "Algorithmique", "Utiliser la programmation dynamique pour écrire un algorithme, par exemple le rendu de monnaie", 59),
            ("TERMINALE", "NSIT_BOYER_MOORE",             "Algorithmique", "Étudier l'algorithme de Boyer-Moore pour chercher un motif dans un texte et dire l'intérêt du prétraitement", 60),
            ("TERMINALE", "NSIT_COUTS_COMPARER",          "Algorithmique", "Comparer des coûts d'exécution en n², en n log2 n et en log2 n, en temps ou en mémoire", 61),
        };
    }
}
