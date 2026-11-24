namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// Les enseignements technologiques de la STI2D (innovation technologique et ingénierie et
    /// développement durable en première ; ingénierie, innovation et développement durable en
    /// terminale, tronc commun et quatre enseignements spécifiques), tels que le texte officiel les
    /// définit — provenance détaillée en tête du fichier. Réparti entre les matières par
    /// `ReferentielsSeriesTechnologiques`.
    /// </summary>
    public static class ReferentielSti2d
    {
        // =============================================================================
        // RÉFÉRENTIELS DE LA SÉRIE TECHNOLOGIQUE STI2D (sciences et technologies de l'industrie et
        // du développement durable). Enseignements de spécialité technologiques de première et de
        // terminale — hors « Physique-chimie et mathématiques » (annexes 2 et 3 : voir ReferentielSti2dPcm).
        //
        // PROVENANCE — texte téléchargé et lu le 23/11/2026. Rien n'est écrit de mémoire.
        // Copie locale lue : spe591_annexe1_1063610.pdf + .txt extrait par pdftotext -layout ; les
        // niveaux de la grille des connaissances ont été relus dans la géométrie du PDF (bordures de
        // cellules), le .txt ne permettant pas de distinguer une cellule fusionnée d'une colonne seule.
        //
        // | Tableau | Niveau          | Intitulé exact (page de titre de l'annexe)                                     | Arrêté / NOR                      | BO                            | En vigueur            | PDF lu |
        // |---------|-----------------|---------------------------------------------------------------------------------|-----------------------------------|-------------------------------|-----------------------|--------|
        // | IT1     | PREMIERE_STI2D  | « Programme d'innovation technologique et d'ingénierie et développement durable | arrêté du 17/01/2019, MENE1901591A | BO spécial n° 1 du 22/01/2019 | rentrée 2019 (art. 2) | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/61/0/spe591_annexe1_1063610.pdf |
        // | I2D1    | PREMIERE_STI2D  |   de première et d'ingénierie, innovation et développement durable de terminale | même arrêté                       | même BO                       | rentrée 2019 (art. 2) | même PDF |
        // | I2DT    | TERMINALE_STI2D |   STI2D » (annexe 1) — UNE SEULE ANNEXE pour les sept tableaux                  | même arrêté                       | même BO                       | rentrée 2020 (art. 2) | même PDF |
        // | AC      | TERMINALE_STI2D |   enseignement spécifique « architecture et construction »                     | même arrêté                       | même BO                       | rentrée 2020 (art. 2) | même PDF |
        // | EE      | TERMINALE_STI2D |   enseignement spécifique « énergies et environnement »                        | même arrêté                       | même BO                       | rentrée 2020 (art. 2) | même PDF |
        // | ITEC    | TERMINALE_STI2D |   enseignement spécifique « innovation technologique et éco-conception »       | même arrêté                       | même BO                       | rentrée 2020 (art. 2) | même PDF |
        // | SIN     | TERMINALE_STI2D |   enseignement spécifique « systèmes d'information et numérique »              | même arrêté                       | même BO                       | rentrée 2020 (art. 2) | même PDF |
        //
        // Arrêté lu : https://www.education.gouv.fr/bo/19/Special1/MENE1901591A.htm — article 2 :
        // « entrent en vigueur à la rentrée scolaire 2019 pour la classe de première et à la rentrée
        // 2020 pour la classe terminale ». Deux annexes : 1 (ce fichier) et 2 (physique-chimie et
        // mathématiques de première STI2D).
        // TOUJOURS EN VIGUEUR EN 2026-2027 : les textes 2025-2026 ne modifient que les épreuves, pas
        // ce programme (notes MENE2515695N puis MENE2622646N, lues le 23/11/2026).
        //
        // LECTURE — comment le texte répartit les contenus, et ce qui est retenu :
        //  - Deux grilles. (1) « Objectifs et compétences » (p. 7 à 11 du PDF) : sept objectifs O1 à O7,
        //    déclinés en compétences CO, avec trois colonnes de croix IT / I2D / 2I2D (pas de croix =
        //    non mobilisée ; X = partiellement mobilisée ; XX = « totalement mobilisée et évaluée en
        //    priorité »). Trois compétences (CO5.8 « Concevoir », CO6.5 et CO7.6 « Expérimenter ») sont
        //    CONTEXTUALISÉES par enseignement spécifique (lignes AC1, AC2, EE1, EE2, ITEC1, ITEC2,
        //    SIN1, SIN2). (2) « Connaissances associées » (p. 12 à 53), six chapitres, avec un niveau
        //    taxonomique 1 à 4 dans les colonnes IT, I2D (première) et AC, ITEC, EE, SIN (terminale).
        //    Note 5 du texte : « Les cases fusionnées indiquent les enseignements de terminale communs
        //    aux quatre enseignements spécifiques ».
        //  - Compétences retenues : en première, celles marquées XX dans la colonne IT (pour IT1) ou
        //    I2D (pour I2D1) ; les X (mobilisation partielle) sont laissés de côté. En terminale, toutes
        //    les compétences XX de la colonne 2I2D vont au tronc I2DT, sauf les trois contextualisées,
        //    qui vont, ligne par ligne, dans AC, EE, ITEC ou SIN.
        //  - Connaissances retenues : seulement les niveaux 2 et 3. Le texte (Nota, p. 12) : « les
        //    évaluations permettant la certification ne peuvent porter que sur des compétences
        //    utilisant des savoirs [...] de niveau 2, 3 et 4 » ; le niveau 4 « n'est pas attendu au
        //    cycle terminal ». Les lignes de niveau 1 (information seule) ne sont pas reprises.
        //    Colonne IT -> IT1 ; colonne I2D -> I2D1 ; case fusionnée AC-SIN -> I2DT ; case AC, EE,
        //    ITEC ou SIN seule -> le tableau de cet enseignement (une même connaissance peut figurer
        //    dans plusieurs tableaux spécifiques : elle y est reformulée selon le commentaire propre).
        //  - Libellés : les compétences CO sont citées (parfois resserrées) ; les connaissances,
        //    écrites dans le texte sous forme nominale, sont mises à l'infinitif avec le verbe de la
        //    colonne « Commentaires » quand il existe (« il s'agit de... », « se limiter à... »).
        //    La colonne « Liens sciences » (renvois à la physique-chimie et aux maths) n'est pas reprise.
        //  - Domaine = l'objectif (O1 à O7) pour une compétence, le chapitre ou la section pour une
        //    connaissance.
        //  - Le projet (36 h en fin de première, 72 h en terminale) est une modalité, pas une ligne :
        //    il porte les compétences CO5.x et CO7.x.
        //  - Les tableaux AC, EE, ITEC et SIN portent tous le niveau TERMINALE_STI2D : un élève ne suit
        //    qu'UN enseignement spécifique ; c'est au branchement de ne servir que le sien.
        // Codes : ASCII, <= 40 caractères. Ordre : 1..n dans l'ordre du programme, par tableau.
        // =============================================================================

        // -----------------------------------------------------------------------------
        // 1. PREMIÈRE STI2D — INNOVATION TECHNOLOGIQUE (IT)
        //    Compétences XX de la colonne IT (O2, O4, O5, O7), puis connaissances de niveau 2 de la
        //    colonne IT : démarche de projet, design, ingénierie système, compétitivité, créativité,
        //    cycle de vie, maquette numérique et choix, solutions constructives, prototypage.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] IT1 =
        {
            ("PREMIERE_STI2D", "IT1_CO21_CAHIER_DES_CHARGES",   "Identifier les éléments influents du développement d'un produit", "Décoder le cahier des charges d'un produit et participer, si besoin, à sa modification", 1),
            ("PREMIERE_STI2D", "IT1_CO22_COMPETITIVITE",        "Identifier les éléments influents du développement d'un produit", "Évaluer la compétitivité d'un produit d'un point de vue technique et économique", 2),
            ("PREMIERE_STI2D", "IT1_CO41_REPRESENTER_IDEE",     "Communiquer une idée, une solution, un projet", "Décrire une idée, un principe, une solution, un projet en utilisant des outils de représentation adaptés", 3),
            ("PREMIERE_STI2D", "IT1_CO51_PROJET_GROUPE",        "Imaginer une solution, répondre à un besoin", "S'impliquer dans une démarche de projet menée en groupe", 4),
            ("PREMIERE_STI2D", "IT1_CO52_PROBLEME_TECHNIQUE",   "Imaginer une solution, répondre à un besoin", "Identifier et justifier un problème technique à partir de l'analyse globale d'un produit (matière, énergie, information)", 5),
            ("PREMIERE_STI2D", "IT1_CO54_PLANIFIER",            "Imaginer une solution, répondre à un besoin", "Planifier un projet (diagramme de Gantt, chemin critique) avec les outils adaptés et les données technico-économiques", 6),
            ("PREMIERE_STI2D", "IT1_CO55_CREATIVITE",           "Imaginer une solution, répondre à un besoin", "Proposer des solutions à un problème technique par des démarches de créativité, choisir et justifier la solution retenue", 7),
            ("PREMIERE_STI2D", "IT1_CO56_ETUDE_DESIGN",         "Imaginer une solution, répondre à un besoin", "Participer à une étude de design d'un produit dans une démarche de développement durable", 8),
            ("PREMIERE_STI2D", "IT1_CO57_STRUCTURE_MATERIELLE", "Imaginer une solution, répondre à un besoin", "Définir la structure matérielle d'un produit selon les caractéristiques technico-économiques et environnementales attendues", 9),
            ("PREMIERE_STI2D", "IT1_CO74_PROTOTYPE",            "Expérimenter et réaliser des prototypes ou des maquettes", "Réaliser et valider un prototype ou une maquette répondant à tout ou partie du cahier des charges initial", 10),
            ("PREMIERE_STI2D", "IT1_PROJ_INTERVENANTS",         "La démarche de projet", "Décrire le rôle et la responsabilité des intervenants d'un projet (maître d'ouvrage, maître d'œuvre, entreprises, contrôleurs)", 11),
            ("PREMIERE_STI2D", "IT1_PROJ_PHASES_PLANIFICATION", "La démarche de projet", "Situer les attendus des phases d'un projet (APS, APD, exécution) et utiliser les outils adaptés pour le planifier", 12),
            ("PREMIERE_STI2D", "IT1_PROJ_PROJET_INDUSTRIEL",    "La démarche de projet", "Situer les phases d'un projet industriel (marketing, conception, industrialisation, fin de vie) et son suivi (coût, budget)", 13),
            ("PREMIERE_STI2D", "IT1_PROJ_REGLEMENTATION",       "La démarche de projet", "Adapter les solutions constructives d'un projet aux réglementations en vigueur", 14),
            ("PREMIERE_STI2D", "IT1_COMM_OUTILS",               "La démarche de projet", "Choisir un outil de communication technique (carte mentale, SysML, croquis, maquette) selon le contenu et l'interlocuteur", 15),
            ("PREMIERE_STI2D", "IT1_DESIGN_CONTEXTE",           "Approche design et architecturale des produits", "Situer un produit dans son contexte culturel, écologique, économique et technologique, ses cibles et ses supports de diffusion", 16),
            ("PREMIERE_STI2D", "IT1_DESIGN_USAGES",             "Approche design et architecturale des produits", "Relier besoins, usages et formes d'un produit : services rendus, expérience utilisateur, design d'interaction et ergonomie", 17),
            ("PREMIERE_STI2D", "IT1_DESIGN_CONSTRUCTIONS",      "Approche design et architecturale des produits", "Identifier les types de constructions et relier une proposition architecturale à son contexte historique ou socio-culturel", 18),
            ("PREMIERE_STI2D", "IT1_IS_APPROCHE_SYSTEME",       "Outils de l'ingénierie système", "Délimiter un système : environnement, frontières, système d'intérêt, points de vue", 19),
            ("PREMIERE_STI2D", "IT1_IS_CYCLE_EN_V",             "Outils de l'ingénierie système", "Situer l'analyse du besoin, les spécifications techniques et la conception dans le cycle en V", 20),
            ("PREMIERE_STI2D", "IT1_IS_ANALYSE_BESOIN",         "Outils de l'ingénierie système", "Extraire d'un cahier des charges en SysML le besoin, la mission, les cas et scénarios d'utilisation, les parties prenantes", 21),
            ("PREMIERE_STI2D", "IT1_IS_SPECIFICATION",          "Outils de l'ingénierie système", "Spécifier les exigences et proposer une architecture fonctionnelle et structurelle (états, séquences, flux)", 22),
            ("PREMIERE_STI2D", "IT1_IS_IVVQ",                   "Outils de l'ingénierie système", "Distinguer intégration, vérification, validation et qualification d'un produit", 23),
            ("PREMIERE_STI2D", "IT1_COMP_LABELS_SERVICE",       "Compétitivité des produits", "Définir les labels de performance (HQE, E+C-) et l'importance du service rendu (besoin réel, besoin induit)", 24),
            ("PREMIERE_STI2D", "IT1_COMP_INNOVATION_BREVETS",   "Compétitivité des produits", "Distinguer les types d'innovation, consulter des bases de brevets et identifier comment protéger nom, design et technique", 25),
            ("PREMIERE_STI2D", "IT1_COMP_ERGONOMIE",            "Compétitivité des produits", "Analyser l'ergonomie d'un produit : confort, efficacité et sécurité des relations Homme-produit et Homme-système", 26),
            ("PREMIERE_STI2D", "IT1_COMP_FONCTION_COUT",        "Compétitivité des produits", "Comparer des solutions par les relations fonction/coût/besoin, fonction/coût/réalisation et fonction/impact environnemental", 27),
            ("PREMIERE_STI2D", "IT1_CREA_METHODES",             "Créativité et innovation technologique", "Mettre en œuvre des méthodes de créativité rationnelles et non rationnelles (brainstorming, principes d'innovation)", 28),
            ("PREMIERE_STI2D", "IT1_CREA_INTEGRATION",          "Créativité et innovation technologique", "Repérer les composants qui intègrent plusieurs fonctions et les transferts de technologie", 29),
            ("PREMIERE_STI2D", "IT1_ENV_CYCLE_DE_VIE",          "Approche environnementale", "Définir les phases du cycle de vie d'un produit, en particulier son cycle de développement", 30),
            ("PREMIERE_STI2D", "IT1_ECO_MAQUETTE_NUMERIQUE",    "Éco-conception des produits", "Modifier ou compléter l'assemblage d'une maquette numérique à partir d'un composant fourni", 31),
            ("PREMIERE_STI2D", "IT1_ECO_PRESENTATION_3D",       "Éco-conception des produits", "Présenter une solution à partir de la maquette numérique : vues photo-réalistes, éclatés, réalité virtuelle ou augmentée", 32),
            ("PREMIERE_STI2D", "IT1_ECO_OUTILS",                "Éco-conception des produits", "Utiliser les outils de l'éco-conception et de l'éco-construction pour améliorer la performance environnementale d'un produit", 33),
            ("PREMIERE_STI2D", "IT1_ECO_CHOIX_MATERIAU",        "Éco-conception des produits", "Choisir un couple matériau/procédé par une démarche structurée et argumentée (critères environnementaux, méthode d'Ashby)", 34),
            ("PREMIERE_STI2D", "IT1_ECO_MATRICE_CHOIX",         "Éco-conception des produits", "Choisir une solution avec une matrice de comparaison fournie : coût, fiabilité, environnement, ergonomie et design", 35),
            ("PREMIERE_STI2D", "IT1_ECO_CHOIX_LOGICIEL",        "Éco-conception des produits", "Choisir des bibliothèques logicielles, une unité de traitement et des interfaces adaptées à un produit", 36),
            ("PREMIERE_STI2D", "IT1_SOL_ENVELOPPES",            "Solutions constructives", "Choisir un constituant d'enveloppe (mur-rideau, bois, acier, béton) selon ses propriétés pour répondre à une exigence", 37),
            ("PREMIERE_STI2D", "IT1_SOL_PUISSANCE",             "Solutions constructives", "Caractériser convertisseurs, modulateurs et adaptateurs de puissance et organes mécaniques de transmission", 38),
            ("PREMIERE_STI2D", "IT1_SOL_INFORMATION",           "Solutions constructives", "Caractériser des capteurs analogiques et numériques et des constituants d'IHM (sonores, visuels, tactiles)", 39),
            ("PREMIERE_STI2D", "IT1_PROTO_PIECES",              "Prototypage et expérimentations", "Prototyper des pièces et la chaîne d'information par une chaîne numérique complète et continue (fablab)", 40),
            ("PREMIERE_STI2D", "IT1_PROTO_VALIDATION",          "Prototypage et expérimentations", "Intégrer les éléments prototypés d'un produit, puis mesurer et valider ses performances", 41),
        };

        // -----------------------------------------------------------------------------
        // 2. PREMIÈRE STI2D — INGÉNIERIE ET DÉVELOPPEMENT DURABLE (I2D)
        //    Compétences XX de la colonne I2D (O1, O3, O4, O5, O6, O7), puis connaissances de niveau 2
        //    et 3 de la colonne I2D : ressources et cycle de vie, flux MEI, ossatures et enveloppes,
        //    chaînes de puissance et d'information, simulation, comportements mécanique, énergétique et
        //    informationnel, schémas, réseaux, bâtiment, programmation, expérimentations.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] I2D1 =
        {
            ("PREMIERE_STI2D", "I2D1_CO11_STRUCTURES_FLUX",       "Caractériser des produits dans une approche de développement durable", "Justifier les choix des structures matérielles et/ou logicielles d'un produit et identifier les flux mis en œuvre", 1),
            ("PREMIERE_STI2D", "I2D1_CO12_ERGONOMIE_DESIGN",      "Caractériser des produits dans une approche de développement durable", "Justifier le choix d'une solution selon des contraintes d'ergonomie et de design", 2),
            ("PREMIERE_STI2D", "I2D1_CO13_PERF_ENVIRONNEMENT",    "Caractériser des produits dans une approche de développement durable", "Justifier les solutions constructives au regard des performances environnementales et estimer leur impact sur l'efficacité globale", 3),
            ("PREMIERE_STI2D", "I2D1_CO31_FONCTIONS_ES",          "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser les fonctions et les constituants d'un produit ainsi que ses entrées/sorties", 4),
            ("PREMIERE_STI2D", "I2D1_CO32_AGENCEMENT",            "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser l'agencement matériel et/ou logiciel d'un produit", 5),
            ("PREMIERE_STI2D", "I2D1_CO33_FONCT_TEMPOREL",        "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser le fonctionnement temporel d'un produit ou d'un processus", 6),
            ("PREMIERE_STI2D", "I2D1_CO34_SOLUTIONS_TECHNIQUES",  "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser des solutions techniques", 7),
            ("PREMIERE_STI2D", "I2D1_CO42_DECRIRE_FONCT",         "Communiquer une idée, une solution, un projet", "Décrire le fonctionnement et/ou l'exploitation d'un produit en utilisant l'outil de description le plus pertinent", 8),
            ("PREMIERE_STI2D", "I2D1_CO43_ARGUMENTER",            "Communiquer une idée, une solution, un projet", "Présenter de manière argumentée des démarches, des résultats, y compris dans une langue étrangère", 9),
            ("PREMIERE_STI2D", "I2D1_CO53_DIAGRAMMES",            "Imaginer une solution, répondre à un besoin", "Mettre en évidence les constituants d'un produit à partir des diagrammes pertinents", 10),
            ("PREMIERE_STI2D", "I2D1_CO61_MODELE_MULTIPHYSIQUE",  "Préparer une simulation et exploiter les résultats", "Expliquer des éléments d'une modélisation multiphysique relative au comportement de tout ou partie d'un produit", 11),
            ("PREMIERE_STI2D", "I2D1_CO62_PARAMETRES_SIMU",       "Préparer une simulation et exploiter les résultats", "Identifier et régler les variables et paramètres internes et externes utiles à une simulation multiphysique", 12),
            ("PREMIERE_STI2D", "I2D1_CO63_ECART_REEL_MODELE",     "Préparer une simulation et exploiter les résultats", "Évaluer l'écart entre le comportement du réel et les résultats du modèle et conclure sur la validité du modèle", 13),
            ("PREMIERE_STI2D", "I2D1_CO64_CHOIX_MODELE",          "Préparer une simulation et exploiter les résultats", "Choisir, pour une fonction donnée, un modèle de comportement à partir d'observations ou de mesures faites sur le produit", 14),
            ("PREMIERE_STI2D", "I2D1_CO75_SCENARIO_VALIDATION",   "Expérimenter et réaliser des prototypes ou des maquettes", "Mettre en œuvre un scénario de validation (essais, mesures, observations), interpréter les résultats et qualifier le produit", 15),
            ("PREMIERE_STI2D", "I2D1_ENV_RESSOURCES",             "Approche environnementale", "Comparer coûts, disponibilité et impacts environnementaux des matériaux et situer les enjeux énergétiques mondiaux", 16),
            ("PREMIERE_STI2D", "I2D1_ENV_MATERIAUX_CYCLE",        "Approche environnementale", "Relier les propriétés des matériaux aux impacts environnementaux du cycle de vie (masses, pertes, rejets, fin de vie)", 17),
            ("PREMIERE_STI2D", "I2D1_ENV_EFFICACITE",             "Approche environnementale", "Analyser l'efficacité énergétique d'un produit et l'apport de la chaîne d'information associée à la commande", 18),
            ("PREMIERE_STI2D", "I2D1_FLUX_MEI",                   "Représentation des flux MEI", "Différencier flux et stocks et caractériser les flux de matière, d'énergie et d'information (IBD SysML, diagramme de Sankey)", 19),
            ("PREMIERE_STI2D", "I2D1_STRUCT_ENVELOPPES_OSSATURES","Approche fonctionnelle et structurelle", "Identifier les types et fonctions des enveloppes et les sous-ensembles élémentaires des ossatures (poutres, treillis, coques)", 20),
            ("PREMIERE_STI2D", "I2D1_STRUCT_CHAINE_PUISSANCE",    "Approche fonctionnelle et structurelle", "Représenter une chaîne de puissance en schéma-blocs et identifier les types de stockage et de conversion d'énergie", 21),
            ("PREMIERE_STI2D", "I2D1_STRUCT_MODULATION",          "Approche fonctionnelle et structurelle", "Distinguer modulation commandée (TOR ou progressive) et adaptation non commandée de puissance (AC/DC, DC/AC...)", 22),
            ("PREMIERE_STI2D", "I2D1_STRUCT_CINEMATIQUE",         "Approche fonctionnelle et structurelle", "Produire ou modifier le schéma cinématique d'un système plan simple (liaisons, classes d'équivalence, graphe)", 23),
            ("PREMIERE_STI2D", "I2D1_STRUCT_CHAINE_INFO",         "Approche fonctionnelle et structurelle", "Représenter une chaîne d'information en schéma-blocs : acquérir, traiter, communiquer", 24),
            ("PREMIERE_STI2D", "I2D1_INFO_ACQUISITION",           "Approche fonctionnelle et structurelle", "Choisir un dispositif d'acquisition (étendue de mesure, résolution, sensibilité) et caractériser conditionnement et CAN", 25),
            ("PREMIERE_STI2D", "I2D1_INFO_CODAGE_ALGO",           "Approche fonctionnelle et structurelle", "Changer de base binaire/décimal et écrire un algorithme (structures conditionnelles et itératives, variables)", 26),
            ("PREMIERE_STI2D", "I2D1_INFO_RESEAUX",               "Approche fonctionnelle et structurelle", "Décrire la typologie des transmissions et l'architecture d'un réseau (modèle OSI, adresses physique et IPv4)", 27),
            ("PREMIERE_STI2D", "I2D1_INFO_APPLI_LOGICIELLE",      "Approche fonctionnelle et structurelle", "Analyser la structure d'une application logicielle : programme principal, interfaces, entrées-sorties, sous-programmes", 28),
            ("PREMIERE_STI2D", "I2D1_SIMU_MODELE",                "Modélisations et simulations", "Choisir un progiciel et paramétrer un modèle : variables internes et externes, grandeurs flux et effort, sources, rendus", 29),
            ("PREMIERE_STI2D", "I2D1_SIMU_SOLVEUR",               "Modélisations et simulations", "Paramétrer une simulation : solveur à pas fixe ou variable, compromis précision/temps de calcul", 30),
            ("PREMIERE_STI2D", "I2D1_SIMU_RESULTATS",             "Modélisations et simulations", "Exploiter et interpréter les résultats d'une simulation : courbes, tableaux, graphes, unités", 31),
            ("PREMIERE_STI2D", "I2D1_MECA_MOUVEMENTS",            "Comportement mécanique des produits", "Identifier la mobilité d'une structure et caractériser trajectoires, vitesses et lois entrée-sortie d'un mécanisme plan", 32),
            ("PREMIERE_STI2D", "I2D1_MECA_STATIQUE",              "Comportement mécanique des produits", "Appliquer le principe fondamental de la statique en modélisant actions mécaniques et liaisons, avec un progiciel", 33),
            ("PREMIERE_STI2D", "I2D1_MECA_RESISTANCE",            "Comportement mécanique des produits", "Exploiter contrainte normale, loi de Hooke, module d'Young et limite élastique en traction, compression et flexion simple", 34),
            ("PREMIERE_STI2D", "I2D1_ENERGIE_BILAN",              "Comportement énergétique des produits", "Appliquer la conservation de l'énergie entre source et charge et établir sur mesures le bilan énergétique et le rendement", 35),
            ("PREMIERE_STI2D", "I2D1_INFO_SIGNAL",                "Comportement informationnel des produits", "Lire un chronogramme : nature de l'information, période, fréquence, amplitude, niveau logique, rapport cyclique", 36),
            ("PREMIERE_STI2D", "I2D1_INFO_DIAGRAMMES",            "Comportement informationnel des produits", "Décrire des échanges d'information par des diagrammes de séquence, d'états et d'activités", 37),
            ("PREMIERE_STI2D", "I2D1_INFO_COMMUNICATION",         "Comportement informationnel des produits", "Caractériser une communication (synchrone, duplex, maître/esclave), lire une trame série binaire, décrire un routage", 38),
            ("PREMIERE_STI2D", "I2D1_INFO_BOUCLE",                "Comportement informationnel des produits", "Représenter l'organisation fonctionnelle d'une boucle de régulation ou d'asservissement", 39),
            ("PREMIERE_STI2D", "I2D1_ECO_SCHEMAS",                "Éco-conception des produits", "Lire un schéma architectural, électrique ou fluidique d'un produit", 40),
            ("PREMIERE_STI2D", "I2D1_ECO_RESEAUX",                "Éco-conception des produits", "Comparer des réseaux routiers, informatiques, énergétiques ou de fluides : maillage, nœuds, pertes, gestion intelligente", 41),
            ("PREMIERE_STI2D", "I2D1_ECO_BATIMENT",               "Éco-conception des produits", "Analyser les apports et dépenses énergétiques d'un bâtiment : isolation, rendement des équipements, domotique", 42),
            ("PREMIERE_STI2D", "I2D1_ECO_PROGRAMMATION",          "Éco-conception des produits", "Lister les entrées/sorties d'un système, coder dans un langage spécifique et mettre au point le programme", 43),
            ("PREMIERE_STI2D", "I2D1_EXP_ESSAIS",                 "Prototypage et expérimentations", "Établir un protocole d'essai sûr et expérimenter matériaux, structures et constituants des chaînes de puissance et d'information", 44),
        };

        // -----------------------------------------------------------------------------
        // 3. TERMINALE STI2D — INGÉNIERIE, INNOVATION ET DÉVELOPPEMENT DURABLE, TRONC COMMUN (2I2D)
        //    Compétences XX de la colonne 2I2D, hors les trois contextualisées (CO5.8, CO6.5, CO7.6),
        //    puis connaissances des cases fusionnées AC-SIN (« communs aux quatre enseignements
        //    spécifiques ») : communication, ingénierie système, flux, simulation, schéma
        //    architectural, éco-conception, prototypage et validation.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] I2DT =
        {
            ("TERMINALE_STI2D", "I2DT_CO11_STRUCTURES_FLUX",      "Caractériser des produits dans une approche de développement durable", "Justifier les choix des structures matérielles et/ou logicielles d'un produit et identifier les flux mis en œuvre", 1),
            ("TERMINALE_STI2D", "I2DT_CO12_ERGONOMIE_DESIGN",     "Caractériser des produits dans une approche de développement durable", "Justifier le choix d'une solution selon des contraintes d'ergonomie et de design", 2),
            ("TERMINALE_STI2D", "I2DT_CO13_PERF_ENVIRONNEMENT",   "Caractériser des produits dans une approche de développement durable", "Justifier les solutions constructives au regard des performances environnementales et estimer leur impact sur l'efficacité globale", 3),
            ("TERMINALE_STI2D", "I2DT_CO21_CAHIER_DES_CHARGES",   "Identifier les éléments influents du développement d'un produit", "Décoder le cahier des charges d'un produit et participer, si besoin, à sa modification", 4),
            ("TERMINALE_STI2D", "I2DT_CO22_COMPETITIVITE",        "Identifier les éléments influents du développement d'un produit", "Évaluer la compétitivité d'un produit d'un point de vue technique et économique", 5),
            ("TERMINALE_STI2D", "I2DT_CO31_FONCTIONS_ES",         "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser les fonctions et les constituants d'un produit ainsi que ses entrées/sorties", 6),
            ("TERMINALE_STI2D", "I2DT_CO32_AGENCEMENT",           "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser l'agencement matériel et/ou logiciel d'un produit", 7),
            ("TERMINALE_STI2D", "I2DT_CO33_FONCT_TEMPOREL",       "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser le fonctionnement temporel d'un produit ou d'un processus", 8),
            ("TERMINALE_STI2D", "I2DT_CO34_SOLUTIONS_TECHNIQUES", "Analyser l'organisation fonctionnelle et structurelle d'un produit", "Identifier et caractériser des solutions techniques", 9),
            ("TERMINALE_STI2D", "I2DT_CO41_REPRESENTER_IDEE",     "Communiquer une idée, une solution, un projet", "Décrire une idée, un principe, une solution, un projet en utilisant des outils de représentation adaptés", 10),
            ("TERMINALE_STI2D", "I2DT_CO42_DECRIRE_FONCT",        "Communiquer une idée, une solution, un projet", "Décrire le fonctionnement et/ou l'exploitation d'un produit en utilisant l'outil de description le plus pertinent", 11),
            ("TERMINALE_STI2D", "I2DT_CO43_ARGUMENTER",           "Communiquer une idée, une solution, un projet", "Présenter de manière argumentée des démarches, des résultats, y compris dans une langue étrangère", 12),
            ("TERMINALE_STI2D", "I2DT_CO51_PROJET_GROUPE",        "Imaginer une solution, répondre à un besoin", "S'impliquer dans une démarche de projet menée en groupe", 13),
            ("TERMINALE_STI2D", "I2DT_CO52_PROBLEME_TECHNIQUE",   "Imaginer une solution, répondre à un besoin", "Identifier et justifier un problème technique à partir de l'analyse globale d'un produit (matière, énergie, information)", 14),
            ("TERMINALE_STI2D", "I2DT_CO53_DIAGRAMMES",           "Imaginer une solution, répondre à un besoin", "Mettre en évidence les constituants d'un produit à partir des diagrammes pertinents", 15),
            ("TERMINALE_STI2D", "I2DT_CO54_PLANIFIER",            "Imaginer une solution, répondre à un besoin", "Planifier un projet (diagramme de Gantt, chemin critique) avec les outils adaptés et les données technico-économiques", 16),
            ("TERMINALE_STI2D", "I2DT_CO55_CREATIVITE",           "Imaginer une solution, répondre à un besoin", "Proposer des solutions à un problème technique par des démarches de créativité, choisir et justifier la solution retenue", 17),
            ("TERMINALE_STI2D", "I2DT_CO56_ETUDE_DESIGN",         "Imaginer une solution, répondre à un besoin", "Participer à une étude de design d'un produit dans une démarche de développement durable", 18),
            ("TERMINALE_STI2D", "I2DT_CO57_STRUCTURE_MATERIELLE", "Imaginer une solution, répondre à un besoin", "Définir la structure matérielle d'un produit selon les caractéristiques technico-économiques et environnementales attendues", 19),
            ("TERMINALE_STI2D", "I2DT_CO61_MODELE_MULTIPHYSIQUE", "Préparer une simulation et exploiter les résultats", "Expliquer des éléments d'une modélisation multiphysique relative au comportement de tout ou partie d'un produit", 20),
            ("TERMINALE_STI2D", "I2DT_CO62_PARAMETRES_SIMU",      "Préparer une simulation et exploiter les résultats", "Identifier et régler les variables et paramètres internes et externes utiles à une simulation multiphysique", 21),
            ("TERMINALE_STI2D", "I2DT_CO63_ECART_REEL_MODELE",    "Préparer une simulation et exploiter les résultats", "Évaluer l'écart entre le comportement du réel et les résultats du modèle et conclure sur la validité du modèle", 22),
            ("TERMINALE_STI2D", "I2DT_CO64_CHOIX_MODELE",         "Préparer une simulation et exploiter les résultats", "Choisir, pour une fonction donnée, un modèle de comportement à partir d'observations ou de mesures faites sur le produit", 23),
            ("TERMINALE_STI2D", "I2DT_CO74_PROTOTYPE",            "Expérimenter et réaliser des prototypes ou des maquettes", "Réaliser et valider un prototype ou une maquette répondant à tout ou partie du cahier des charges initial", 24),
            ("TERMINALE_STI2D", "I2DT_CO75_SCENARIO_VALIDATION",  "Expérimenter et réaliser des prototypes ou des maquettes", "Mettre en œuvre un scénario de validation (essais, mesures, observations), interpréter les résultats et qualifier le produit", 25),
            ("TERMINALE_STI2D", "I2DT_COMM_OUTILS",               "La démarche de projet", "Choisir et utiliser l'outil de communication technique adapté au contenu et à l'interlocuteur (SysML, croquis, maquette)", 26),
            ("TERMINALE_STI2D", "I2DT_COMM_COLLABORATIF",         "La démarche de projet", "Utiliser les outils de partage et de travail collaboratif (cloud, PLM, BIM) lors des projets", 27),
            ("TERMINALE_STI2D", "I2DT_IS_APPROCHE_SYSTEME",       "Outils de l'ingénierie système", "Délimiter un système : environnement, frontières, système d'intérêt, points de vue", 28),
            ("TERMINALE_STI2D", "I2DT_IS_ANALYSE_BESOIN",         "Outils de l'ingénierie système", "Extraire d'un cahier des charges en SysML le besoin, les cas et scénarios d'utilisation, les besoins des parties prenantes", 29),
            ("TERMINALE_STI2D", "I2DT_IS_ARCHITECTURE",           "Outils de l'ingénierie système", "Définir les exigences système puis proposer une architecture fonctionnelle et structurelle qui les satisfait", 30),
            ("TERMINALE_STI2D", "I2DT_IS_IVVQ",                   "Outils de l'ingénierie système", "Vérifier et valider un produit par rapport aux exigences et mesurer ses performances (IVVQ)", 31),
            ("TERMINALE_STI2D", "I2DT_FLUX_STOCKS",               "Représentation des flux MEI", "Identifier sur un produit les principaux flux et stocks de matière, d'énergie et d'information et les caractériser", 32),
            ("TERMINALE_STI2D", "I2DT_SIMU_PROGICIEL",            "Modélisations et simulations", "Choisir un progiciel et un type de modèle (volumique, multiphysique, schéma-bloc, états, régression) selon le besoin", 33),
            ("TERMINALE_STI2D", "I2DT_SIMU_PARAMETRAGE",          "Modélisations et simulations", "Paramétrer un modèle : variables internes et externes, sources de simulation et blocs de rendu graphique", 34),
            ("TERMINALE_STI2D", "I2DT_SIMU_POST_TRAITEMENT",      "Modélisations et simulations", "Exploiter ou affiner les résultats d'une simulation par un traitement postérieur des données", 35),
            ("TERMINALE_STI2D", "I2DT_ECO_SCHEMA_ARCHITECTURAL",  "Éco-conception des produits", "Décrire l'organisation structurelle d'un produit par un schéma architectural (mécanique, énergétique, informationnel)", 36),
            ("TERMINALE_STI2D", "I2DT_ECO_OUTILS",                "Éco-conception des produits", "Améliorer la performance environnementale d'un produit avec les outils de l'éco-conception ou de l'éco-construction", 37),
            ("TERMINALE_STI2D", "I2DT_PROTO_RAPIDE",              "Prototypage et expérimentations", "Prototyper des pièces et la chaîne d'information par une chaîne numérique complète et continue (fablab)", 38),
            ("TERMINALE_STI2D", "I2DT_PROTO_INTEGRATION",         "Prototypage et expérimentations", "Vérifier la conformité des éléments prototypés aux spécifications fonctionnelles avant leur intégration dans le produit", 39),
            ("TERMINALE_STI2D", "I2DT_PROTO_PERFORMANCES",        "Prototypage et expérimentations", "Mesurer et valider les performances d'un prototype et faire apparaître les écarts entre simulation et comportement réel", 40),
        };

        // -----------------------------------------------------------------------------
        // 4. TERMINALE STI2D — ENSEIGNEMENT SPÉCIFIQUE ARCHITECTURE ET CONSTRUCTION (AC)
        //    Compétences contextualisées AC1/AC2 (CO5.8, CO6.5, CO7.6 — pas d'AC2 en CO7.6), puis
        //    connaissances de la colonne AC seule : projet de construction, ossatures et enveloppes,
        //    mécanique des structures, maquette numérique (BIM), conception bioclimatique,
        //    fondations et porteurs, essais.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] AC =
        {
            ("TERMINALE_STI2D", "AC_CO58_SOLUTIONS_CONSTRUCTIVES", "Imaginer une solution, répondre à un besoin", "Proposer et choisir des solutions constructives répondant aux contraintes et attentes d'une construction", 1),
            ("TERMINALE_STI2D", "AC_CO58_PROCEDES_MISE_EN_OEUVRE", "Imaginer une solution, répondre à un besoin", "Proposer et choisir des procédés de mise en œuvre d'un projet de construction et organiser sa réalisation", 2),
            ("TERMINALE_STI2D", "AC_CO65_SIMULATION_USAGE",        "Préparer une simulation et exploiter les résultats", "Simuler un usage ou un comportement structurel, thermique, acoustique de tout ou partie d'une construction et conclure", 3),
            ("TERMINALE_STI2D", "AC_CO65_SIMULATION_PROCEDES",     "Préparer une simulation et exploiter les résultats", "Simuler des procédés pour valider un moyen de réalisation", 4),
            ("TERMINALE_STI2D", "AC_CO76_EXPERIMENTER_OUVRAGES",   "Expérimenter et réaliser des prototypes ou des maquettes", "Expérimenter sur des ouvrages ou des maquettes instrumentées pour étudier le comportement d'un ouvrage et valider des choix", 5),
            ("TERMINALE_STI2D", "AC_PROJ_INTERVENANTS",            "La démarche de projet", "Décrire le rôle et la responsabilité des intervenants d'un projet de construction (maîtres d'ouvrage et d'œuvre, contrôleurs)", 6),
            ("TERMINALE_STI2D", "AC_PROJ_PHASES_PLANNING",         "La démarche de projet", "Planifier un projet de construction selon ses phases (utilité publique, APS, APD, consultation, exécution)", 7),
            ("TERMINALE_STI2D", "AC_PROJ_REGLEMENTATION",          "La démarche de projet", "Adapter les solutions constructives d'un projet aux réglementations en vigueur", 8),
            ("TERMINALE_STI2D", "AC_ENV_IMPACTS_CYCLE",            "Approche environnementale", "Comparer les impacts environnementaux d'un ouvrage sur son cycle de vie (masses, assemblages, rejets, fin de vie)", 9),
            ("TERMINALE_STI2D", "AC_FLUX_SANKEY",                  "Représentation des flux MEI", "Analyser les flux d'un ouvrage sur un diagramme de Sankey (bilan énergétique, approvisionnement en matière ou fluides)", 10),
            ("TERMINALE_STI2D", "AC_STRUCT_ENVELOPPES",            "Approche fonctionnelle et structurelle", "Identifier, comparer et caractériser les fonctions assurées par différents types d'enveloppes (isolation, étanchéité)", 11),
            ("TERMINALE_STI2D", "AC_STRUCT_OSSATURES",             "Approche fonctionnelle et structurelle", "Reconnaître les sous-ensembles élémentaires d'une ossature et la continuité mécanique dans la transmission des sollicitations", 12),
            ("TERMINALE_STI2D", "AC_STRUCT_ASSEMBLAGES",           "Approche fonctionnelle et structurelle", "Différencier les assemblages fixes et démontables selon leur fonction (montage, fixation, scellement définitif)", 13),
            ("TERMINALE_STI2D", "AC_STRUCT_SCHEMAS_PORTEURS",      "Approche fonctionnelle et structurelle", "Décoder et compléter les schémas de structures porteuses planes immobiles", 14),
            ("TERMINALE_STI2D", "AC_MECA_LIAISONS",                "Comportement mécanique des produits", "Relier le comportement des liaisons élémentaires aux mouvements et aux efforts", 15),
            ("TERMINALE_STI2D", "AC_MECA_STATIQUE",                "Comportement mécanique des produits", "Résoudre un problème de statique par progiciel : actions mécaniques, liaisons, principe fondamental de la statique", 16),
            ("TERMINALE_STI2D", "AC_MECA_STABILITE",               "Comportement mécanique des produits", "Présenter les risques d'instabilité d'une structure : flambement, contreventement dans les trois directions", 17),
            ("TERMINALE_STI2D", "AC_MECA_DESCENTE_CHARGES",        "Comportement mécanique des produits", "Décrire par un schéma le cheminement des charges dans une ossature et calculer la descente de charges par logiciel", 18),
            ("TERMINALE_STI2D", "AC_MECA_RESISTANCE",              "Comportement mécanique des produits", "Analyser contraintes et déformations d'une structure isostatique en traction, compression ou flexion simple", 19),
            ("TERMINALE_STI2D", "AC_MECA_SCENARIO_SIMULATION",     "Comportement mécanique des produits", "Construire un scénario de simulation (éléments finis) pour comparer et valider une solution", 20),
            ("TERMINALE_STI2D", "AC_REPR_MAQUETTE_BIM",            "Éco-conception des produits", "Concevoir et exploiter la maquette numérique d'un ouvrage en démarche BIM (modeleur paramétrique, vues, éclatés)", 21),
            ("TERMINALE_STI2D", "AC_REPR_VISITE_VIRTUELLE",        "Éco-conception des produits", "Préparer une visite virtuelle pour valider les usages d'une construction (déplacements, organisation spatiale, sécurité)", 22),
            ("TERMINALE_STI2D", "AC_REPR_PLANS",                   "Éco-conception des produits", "Lire les plans et profils d'un projet de construction", 23),
            ("TERMINALE_STI2D", "AC_CONC_MATERIAUX",               "Éco-conception des produits", "Choisir un couple matériau/procédé par une démarche structurée et argumentée (multicritère, méthode d'Ashby)", 24),
            ("TERMINALE_STI2D", "AC_CONC_MATRICE",                 "Éco-conception des produits", "Élaborer une matrice de comparaison (coût, fiabilité, environnement, design) pour choisir une solution", 25),
            ("TERMINALE_STI2D", "AC_CONC_RESEAUX",                 "Éco-conception des produits", "Différencier les réseaux secs et humides (eau potable, assainissement, fibre) et le rôle du maillage et des nœuds", 26),
            ("TERMINALE_STI2D", "AC_CONC_BIOCLIMATIQUE",           "Éco-conception des produits", "Établir et prédimensionner un avant-projet partiel selon les principes de conception bioclimatique", 27),
            ("TERMINALE_STI2D", "AC_CONC_OSSATURE",                "Éco-conception des produits", "Proposer une structure porteuse (porteurs verticaux et horizontaux, fondations, charpentes) adaptée au projet", 28),
            ("TERMINALE_STI2D", "AC_CONC_ENVELOPPE",               "Éco-conception des produits", "Concevoir l'enveloppe d'une construction comme un objet multi-contraint (étanchéité, confort, sécurité, esthétique)", 29),
            ("TERMINALE_STI2D", "AC_CONC_AMENAGEMENTS",            "Éco-conception des produits", "Vérifier la conformité des aménagements et équipements aux exigences thermiques, acoustiques et d'éclairement", 30),
            ("TERMINALE_STI2D", "AC_CONC_BILAN_ENERGETIQUE",       "Éco-conception des produits", "Identifier apports et dépenses énergétiques et établir le bilan énergétique d'une construction par simulation", 31),
            ("TERMINALE_STI2D", "AC_SOL_ENVELOPPES",               "Solutions constructives", "Choisir un constituant d'enveloppe (mur-rideau, bois, acier, béton) et définir ses caractéristiques pour une exigence", 32),
            ("TERMINALE_STI2D", "AC_SOL_SOUTENEMENTS_FONDATIONS",  "Solutions constructives", "Choisir un soutènement (mur, paroi moulée, terre armée) et des fondations superficielles ou profondes (semelles, pieux)", 33),
            ("TERMINALE_STI2D", "AC_SOL_PORTEURS",                 "Solutions constructives", "Choisir porteurs verticaux et horizontaux, contreventement et charpentes en béton, bois ou métal", 34),
            ("TERMINALE_STI2D", "AC_EXP_MATERIAUX_STRUCTURES",     "Prototypage et expérimentations", "Expérimenter sur éprouvettes ou maquettes pour valider une forme, un matériau ou déterminer l'influence d'un paramètre", 35),
        };

        // -----------------------------------------------------------------------------
        // 5. TERMINALE STI2D — ENSEIGNEMENT SPÉCIFIQUE ÉNERGIES ET ENVIRONNEMENT (EE)
        //    Compétences contextualisées EE1/EE2 (CO5.8, CO6.5, CO7.6), puis connaissances de la
        //    colonne EE seule : chaîne de puissance, comportement énergétique, régulation, réseaux
        //    d'énergie, micro-énergies, convertisseurs et stockeurs, constituants de l'information.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] EE =
        {
            ("TERMINALE_STI2D", "EE_CO58_CHAINE_ENERGIE",          "Imaginer une solution, répondre à un besoin", "Définir ou modifier la structure, les constituants et les paramètres d'une chaîne d'énergie pour répondre à un cahier des charges", 1),
            ("TERMINALE_STI2D", "EE_CO58_GESTION_ENERGIE",         "Imaginer une solution, répondre à un besoin", "Définir, paramétrer et programmer le système de gestion d'une chaîne d'énergie pour améliorer sa performance énergétique", 2),
            ("TERMINALE_STI2D", "EE_CO65_SIMULATION_ENERGETIQUE",  "Préparer une simulation et exploiter les résultats", "Simuler le comportement énergétique (électrique, mécanique, thermique, lumineux) de tout ou partie d'un produit et conclure", 3),
            ("TERMINALE_STI2D", "EE_CO65_SIMULATION_GESTION",      "Préparer une simulation et exploiter les résultats", "Simuler la gestion d'une chaîne de puissance et interpréter les résultats", 4),
            ("TERMINALE_STI2D", "EE_CO76_PROCEDES_ENERGIE",        "Expérimenter et réaliser des prototypes ou des maquettes", "Expérimenter des procédés de stockage, production, transformation ou récupération d'énergie pour concevoir une chaîne de puissance", 5),
            ("TERMINALE_STI2D", "EE_CO76_PERFORMANCES",            "Expérimenter et réaliser des prototypes ou des maquettes", "Expérimenter une chaîne de puissance et sa gestion pour relever ses performances énergétiques et optimiser son fonctionnement", 6),
            ("TERMINALE_STI2D", "EE_ENV_IMPACTS_CYCLE",            "Approche environnementale", "Comparer les impacts environnementaux d'un produit sur son cycle de vie (minimisation et valorisation des pertes et rejets)", 7),
            ("TERMINALE_STI2D", "EE_ENV_EFFICACITE",               "Approche environnementale", "Minimiser la consommation énergétique d'un produit grâce à la chaîne d'information associée à la commande", 8),
            ("TERMINALE_STI2D", "EE_FLUX_IBD_SANKEY",              "Représentation des flux MEI", "Analyser les flux d'énergie d'un produit sur un diagramme de blocs internes ou de Sankey (bilan énergétique)", 9),
            ("TERMINALE_STI2D", "EE_PUIS_CHAINE",                  "Approche fonctionnelle et structurelle", "Représenter une chaîne de puissance par schéma-blocs et caractériser ses fonctions (captation, stockage, conversion, transmission)", 10),
            ("TERMINALE_STI2D", "EE_PUIS_MODULATION_ADAPTATION",   "Approche fonctionnelle et structurelle", "Distinguer modulation commandée (TOR, progressive) et adaptation de puissance (sur la forme, le flux ou l'effort)", 11),
            ("TERMINALE_STI2D", "EE_INFO_CHAINE",                  "Approche fonctionnelle et structurelle", "Représenter une chaîne d'information par schéma-blocs : acquérir, traiter, communiquer", 12),
            ("TERMINALE_STI2D", "EE_INFO_ALGORITHME",              "Approche fonctionnelle et structurelle", "Écrire un algorithme : structures conditionnelles et itératives, variables, appels de sous-programmes", 13),
            ("TERMINALE_STI2D", "EE_INFO_APPLI_LOGICIELLE",        "Approche fonctionnelle et structurelle", "Analyser la structure d'une application logicielle : programme principal, interfaces, entrées-sorties, fonctions", 14),
            ("TERMINALE_STI2D", "EE_SIMU_FLUX_EFFORT",             "Modélisations et simulations", "Identifier les grandeurs flux et effort d'un modèle multiphysique (mécanique, électrique, hydraulique)", 15),
            ("TERMINALE_STI2D", "EE_MECA_EFFORTS",                 "Comportement mécanique des produits", "Analyser la transmission des efforts dans un produit", 16),
            ("TERMINALE_STI2D", "EE_ENER_CONSERVATION",            "Comportement énergétique des produits", "Appliquer la conservation de l'énergie et analyser pertes, rendements et réversibilité d'une chaîne d'énergie", 17),
            ("TERMINALE_STI2D", "EE_ENER_SOURCES_CHARGES",         "Comportement énergétique des produits", "Étudier le point de fonctionnement d'une chaîne d'énergie entre une source et une charge (régimes établi et transitoire)", 18),
            ("TERMINALE_STI2D", "EE_ENER_OPTIMISATION",            "Comportement énergétique des produits", "Optimiser les échanges d'énergie entre source et charge en limitant les pertes (effet Joule, pertes de charge)", 19),
            ("TERMINALE_STI2D", "EE_ENER_COMPORTEMENT_TEMPOREL",   "Comportement énergétique des produits", "Étudier l'évolution de l'état énergétique des constituants (transfert, stockage) par des variables représentatives", 20),
            ("TERMINALE_STI2D", "EE_INFO_DIAGRAMME_ETATS",         "Comportement informationnel des produits", "Décrire ou simuler un comportement par un diagramme d'états ou d'activités", 21),
            ("TERMINALE_STI2D", "EE_REGUL_BOUCLE",                 "Comportement informationnel des produits", "Représenter une boucle de régulation ou d'asservissement et caractériser les paramètres influant sur son contrôle", 22),
            ("TERMINALE_STI2D", "EE_ECO_SCHEMAS",                  "Éco-conception des produits", "Exploiter un schéma électrique ou fluidique respectant les normes en vigueur", 23),
            ("TERMINALE_STI2D", "EE_ECO_MATERIAUX",                "Éco-conception des produits", "Choisir un matériau du point de vue de son comportement énergétique", 24),
            ("TERMINALE_STI2D", "EE_ECO_MATRICE",                  "Éco-conception des produits", "Élaborer une matrice de comparaison (coût, fiabilité, environnement, ergonomie) pour choisir une solution", 25),
            ("TERMINALE_STI2D", "EE_ECO_RESEAUX_ELECTRIQUES",      "Éco-conception des produits", "Distinguer réseaux électriques alternatifs et continus et structures de réseaux de fluides (chaleur, air comprimé)", 26),
            ("TERMINALE_STI2D", "EE_ECO_MICRO_ENERGIES",           "Éco-conception des produits", "Exploiter l'énergie de l'environnement local pour l'auto-alimentation de dispositifs autonomes (capteurs, objets connectés)", 27),
            ("TERMINALE_STI2D", "EE_ECO_RECUPERATION",             "Éco-conception des produits", "Évaluer la pertinence d'une récupération d'énergie sur le cycle de vie du produit et le retour sur investissement", 28),
            ("TERMINALE_STI2D", "EE_SOL_CONVERTISSEURS",           "Solutions constructives", "Choisir convertisseurs, modulateurs et adaptateurs de puissance à l'aide de formulaires et d'abaques", 29),
            ("TERMINALE_STI2D", "EE_SOL_STOCKEURS",                "Solutions constructives", "Comparer des stockeurs d'énergie : énergie et puissance massiques, capacité, rendement, diagramme de Ragone", 30),
            ("TERMINALE_STI2D", "EE_SOL_CAPTEURS_CAN",             "Solutions constructives", "Caractériser capteurs et convertisseurs analogique-numérique par leurs relations d'entrée/sortie", 31),
            ("TERMINALE_STI2D", "EE_SOL_PROGRAMMABLES",            "Solutions constructives", "Mettre en œuvre cartes à microcontrôleur, nano-ordinateurs et objets connectés", 32),
            ("TERMINALE_STI2D", "EE_SOL_BUS_SANS_FIL",             "Solutions constructives", "Caractériser les bus de communication (KNX, EnOcean, CAN, I2C) et les modules de transmission sans fil", 33),
            ("TERMINALE_STI2D", "EE_EXP_CHAINE_PUISSANCE",         "Prototypage et expérimentations", "Expérimenter des constituants standard de la chaîne de puissance pour vérifier leurs caractéristiques externes", 34),
        };

        // -----------------------------------------------------------------------------
        // 6. TERMINALE STI2D — ENSEIGNEMENT SPÉCIFIQUE INNOVATION TECHNOLOGIQUE ET ÉCO-CONCEPTION (ITEC)
        //    Compétences contextualisées ITEC1/ITEC2 (CO5.8, CO6.5, CO7.6), puis connaissances de la
        //    colonne ITEC seule : mécanique (mouvements, statique, résistance), maquette numérique,
        //    conception de pièce et procédés, organes de transmission, prototypage et essais.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] ITEC =
        {
            ("TERMINALE_STI2D", "ITEC_CO58_FORMES_PIECE",          "Imaginer une solution, répondre à un besoin", "Définir avec un modeleur numérique les formes et dimensions d'une pièce selon ses contraintes, son procédé et son matériau", 1),
            ("TERMINALE_STI2D", "ITEC_CO58_SOUS_ENSEMBLE",         "Imaginer une solution, répondre à un besoin", "Définir avec un modeleur numérique les modifications d'un sous-ensemble mécanique à partir des contraintes fonctionnelles", 2),
            ("TERMINALE_STI2D", "ITEC_CO65_SIMULATION_MECANIQUE",  "Préparer une simulation et exploiter les résultats", "Simuler un sous-ensemble mécanique : loi d'entrée/sortie ou comportement d'un assemblage sous charges", 3),
            ("TERMINALE_STI2D", "ITEC_CO65_SIMULATION_PROCEDES",   "Préparer une simulation et exploiter les résultats", "Simuler des procédés pour valider les formes et dimensions d'une pièce", 4),
            ("TERMINALE_STI2D", "ITEC_CO76_PROCEDES",              "Expérimenter et réaliser des prototypes ou des maquettes", "Expérimenter des procédés de réalisation pour caractériser les paramètres de transformation de la matière", 5),
            ("TERMINALE_STI2D", "ITEC_CO76_PERFORMANCES",          "Expérimenter et réaliser des prototypes ou des maquettes", "Mesurer les performances d'un constituant ou d'un sous-ensemble d'un produit", 6),
            ("TERMINALE_STI2D", "ITEC_ENV_IMPACTS_CYCLE",          "Approche environnementale", "Comparer les impacts environnementaux d'un produit sur son cycle de vie (optimisation des masses et des assemblages)", 7),
            ("TERMINALE_STI2D", "ITEC_FLUX_IBD",                   "Représentation des flux MEI", "Lire et modifier partiellement un diagramme de blocs internes SysML d'un produit", 8),
            ("TERMINALE_STI2D", "ITEC_STRUCT_ENVELOPPES",          "Approche fonctionnelle et structurelle", "Identifier et caractériser les fonctions des enveloppes d'un produit (carters, carénages, coques, boîtiers)", 9),
            ("TERMINALE_STI2D", "ITEC_STRUCT_ASSEMBLAGES",         "Approche fonctionnelle et structurelle", "Différencier les assemblages fixes et démontables selon leur fonction (montage, démontage, fixation)", 10),
            ("TERMINALE_STI2D", "ITEC_STRUCT_CHAINE_PUISSANCE",    "Approche fonctionnelle et structurelle", "Représenter une chaîne de puissance par schéma-blocs et caractériser ses fonctions", 11),
            ("TERMINALE_STI2D", "ITEC_STRUCT_CINEMATIQUE",         "Approche fonctionnelle et structurelle", "Produire ou modifier le schéma cinématique d'un système plan simple (liaisons, classes d'équivalence)", 12),
            ("TERMINALE_STI2D", "ITEC_MECA_MOUVEMENTS",            "Comportement mécanique des produits", "Caractériser trajectoires, vitesses et accélérations et rechercher la loi entrée-sortie d'un mécanisme plan", 13),
            ("TERMINALE_STI2D", "ITEC_MECA_LIAISONS",              "Comportement mécanique des produits", "Relier les performances cinématiques des liaisons aux conditions de chargement qui les génèrent", 14),
            ("TERMINALE_STI2D", "ITEC_MECA_STATIQUE",              "Comportement mécanique des produits", "Résoudre un problème de statique par progiciel : actions mécaniques, liaisons, principe fondamental de la statique", 15),
            ("TERMINALE_STI2D", "ITEC_MECA_STABILITE",             "Comportement mécanique des produits", "Présenter qualitativement les risques d'instabilité : flambement d'une pièce comprimée, comportement vibratoire", 16),
            ("TERMINALE_STI2D", "ITEC_MECA_EFFORTS",               "Comportement mécanique des produits", "Analyser la transmission des efforts dans un mécanisme ou une structure", 17),
            ("TERMINALE_STI2D", "ITEC_MECA_RESISTANCE",            "Comportement mécanique des produits", "Analyser contraintes normales et déformations d'une pièce en traction, compression ou flexion simple", 18),
            ("TERMINALE_STI2D", "ITEC_MECA_ELEMENTS_FINIS",        "Comportement mécanique des produits", "Construire un scénario de simulation par éléments finis pour comparer une solution ou modifier une pièce", 19),
            ("TERMINALE_STI2D", "ITEC_REPR_MAQUETTE",              "Éco-conception des produits", "Concevoir la maquette numérique d'un sous-ensemble avec un modeleur volumique paramétrique (arbres, robustesse)", 20),
            ("TERMINALE_STI2D", "ITEC_REPR_PRESENTATION",          "Éco-conception des produits", "Présenter une solution à partir de la maquette numérique (vues photo-réalistes, éclatés, réalité augmentée, PLM)", 21),
            ("TERMINALE_STI2D", "ITEC_CONC_MATERIAUX",             "Éco-conception des produits", "Choisir un couple matériau/procédé par une démarche structurée et argumentée (multicritère, méthode d'Ashby)", 22),
            ("TERMINALE_STI2D", "ITEC_CONC_MATRICE",               "Éco-conception des produits", "Élaborer une matrice de comparaison (coût, fiabilité, environnement, design) pour choisir une solution", 23),
            ("TERMINALE_STI2D", "ITEC_CONC_PIECE_NUMERIQUE",       "Éco-conception des produits", "Créer la maquette volumique d'une pièce extraite d'un assemblage en respectant ses contraintes fonctionnelles (jeux)", 24),
            ("TERMINALE_STI2D", "ITEC_CONC_PROCEDE_FORMES",        "Éco-conception des produits", "Relier le procédé de réalisation et le matériau choisis aux formes et dimensions d'une pièce simple", 25),
            ("TERMINALE_STI2D", "ITEC_SOL_TRANSMISSION",           "Solutions constructives", "Caractériser les organes de transmission et d'adaptation de puissance : réducteurs, lien flexible, accouplements", 26),
            ("TERMINALE_STI2D", "ITEC_SOL_TRANSFORMATION",         "Solutions constructives", "Caractériser les organes mécaniques de transformation de mouvement", 27),
            ("TERMINALE_STI2D", "ITEC_SOL_GUIDAGES_LIAISONS",      "Solutions constructives", "Choisir un guidage en translation ou en rotation et une liaison complète démontable ou non", 28),
            ("TERMINALE_STI2D", "ITEC_SOL_ETANCHEITE",             "Solutions constructives", "Choisir une solution d'étanchéité : protection contre les poussières, étanchéité aux fluides", 29),
            ("TERMINALE_STI2D", "ITEC_PROTO_COULAGE",              "Prototypage et expérimentations", "Prototyper des pièces de petites dimensions par coulage en résine ou en alliage métallique", 30),
            ("TERMINALE_STI2D", "ITEC_EXP_PROCEDES",               "Prototypage et expérimentations", "Expérimenter un procédé sur un système didactique pour en dégager les principes physiques et les contraintes techniques", 31),
            ("TERMINALE_STI2D", "ITEC_EXP_MATERIAUX",              "Prototypage et expérimentations", "Expérimenter sur éprouvettes ou maquettes pour valider une forme, une répartition de matière ou un matériau", 32),
        };

        // -----------------------------------------------------------------------------
        // 7. TERMINALE STI2D — ENSEIGNEMENT SPÉCIFIQUE SYSTÈMES D'INFORMATION ET NUMÉRIQUE (SIN)
        //    Compétences contextualisées SIN1/SIN2 (CO5.8, CO6.5 — SIN1 seul —, CO7.6), puis
        //    connaissances de la colonne SIN seule : chaîne d'information, codage et algorithmique,
        //    réseaux, signaux, communications, programmation, constituants programmables et IHM.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SIN =
        {
            ("TERMINALE_STI2D", "SIN_CO58_ARCHITECTURE",           "Imaginer une solution, répondre à un besoin", "Proposer ou choisir l'architecture d'une solution logicielle et matérielle au regard de la définition d'un produit", 1),
            ("TERMINALE_STI2D", "SIN_CO58_ALGORITHME",             "Imaginer une solution, répondre à un besoin", "Rechercher et écrire l'algorithme de fonctionnement puis programmer la réponse logicielle à une problématique posée", 2),
            ("TERMINALE_STI2D", "SIN_CO65_SIMULATION_INFO",        "Préparer une simulation et exploiter les résultats", "Simuler le comportement informationnel de constituants matériels et de traitements logiciels d'une chaîne d'information", 3),
            ("TERMINALE_STI2D", "SIN_CO76_CHAINE_INFO",            "Expérimenter et réaliser des prototypes ou des maquettes", "Expérimenter des moyens d'acquisition, de traitement, de stockage et de restitution pour concevoir une chaîne d'information", 4),
            ("TERMINALE_STI2D", "SIN_CO76_ARCHITECTURES",          "Expérimenter et réaliser des prototypes ou des maquettes", "Expérimenter des architectures matérielles et logicielles en réponse à une problématique posée", 5),
            ("TERMINALE_STI2D", "SIN_FLUX_IBD",                    "Représentation des flux MEI", "Lire et modifier partiellement un diagramme de blocs internes SysML d'un produit", 6),
            ("TERMINALE_STI2D", "SIN_INFO_CHAINE",                 "Approche fonctionnelle et structurelle", "Représenter une chaîne d'information par schéma-blocs et caractériser ses fonctions : acquérir, traiter, communiquer", 7),
            ("TERMINALE_STI2D", "SIN_INFO_ACQUISITION",            "Approche fonctionnelle et structurelle", "Choisir un dispositif d'acquisition : étendue de mesure, résolution, sensibilité, fonction de transfert, linéarité", 8),
            ("TERMINALE_STI2D", "SIN_INFO_CONDITIONNEMENT_CAN",    "Approche fonctionnelle et structurelle", "Caractériser le conditionnement (filtre passe-bas du premier ordre) et un CAN (nombre de bits, quantum, pleine échelle)", 9),
            ("TERMINALE_STI2D", "SIN_INFO_CODAGE",                 "Approche fonctionnelle et structurelle", "Identifier le type de codage d'une information : binaire, hexadécimal, ASCII", 10),
            ("TERMINALE_STI2D", "SIN_INFO_ALGORITHMIQUE",          "Approche fonctionnelle et structurelle", "Écrire un algorithme : structures conditionnelles et itératives, variables (type, taille), sous-programmes", 11),
            ("TERMINALE_STI2D", "SIN_INFO_TRAITEMENT",             "Approche fonctionnelle et structurelle", "Mettre en évidence les effets de bord d'un traitement numérique (taille des données, stockage, temps de traitement)", 12),
            ("TERMINALE_STI2D", "SIN_INFO_COMPRESSION",            "Approche fonctionnelle et structurelle", "Calculer un taux de compression de données sur des exemples simples", 13),
            ("TERMINALE_STI2D", "SIN_RES_TRANSMISSIONS",           "Approche fonctionnelle et structurelle", "Distinguer connexions point à point (filaire, sans fil) et typologies de réseaux (étoile, anneau à jeton)", 14),
            ("TERMINALE_STI2D", "SIN_RES_ARCHITECTURE",            "Approche fonctionnelle et structurelle", "Décrire un réseau selon le modèle OSI : protocoles, encapsulation, adresses physique et logique (IPv4)", 15),
            ("TERMINALE_STI2D", "SIN_RES_CLIENT_SERVEUR",          "Approche fonctionnelle et structurelle", "Décrire une architecture client/serveur : serveur Web, serveur DHCP, serveur de noms de domaine (DNS)", 16),
            ("TERMINALE_STI2D", "SIN_INFO_APPLI_LOGICIELLE",       "Approche fonctionnelle et structurelle", "Analyser et représenter la structure d'une application logicielle (programme principal, sous-programmes, interfaces)", 17),
            ("TERMINALE_STI2D", "SIN_COMP_NATURE_SIGNAL",          "Comportement informationnel des produits", "Distinguer signal logique, analogique et numérique et lire un chronogramme (période, fréquence, rapport cyclique)", 18),
            ("TERMINALE_STI2D", "SIN_COMP_FREQUENTIEL",            "Comportement informationnel des produits", "Décrire qualitativement la représentation spectrale d'un signal audio simple (basses, moyennes, hautes fréquences)", 19),
            ("TERMINALE_STI2D", "SIN_COMP_DIAGRAMMES",             "Comportement informationnel des produits", "Décrire un scénario par un diagramme de séquence et simuler des états par un diagramme d'états ou d'activités", 20),
            ("TERMINALE_STI2D", "SIN_COMP_COMMUNICATIONS",         "Comportement informationnel des produits", "Caractériser une communication : synchrone/asynchrone, half/full duplex, maître/esclave, client/serveur", 21),
            ("TERMINALE_STI2D", "SIN_COMP_LIAISON_SERIE",          "Comportement informationnel des produits", "Décoder une liaison série : protocole, bits de start/stop, bit de parité, débit", 22),
            ("TERMINALE_STI2D", "SIN_COMP_CONFIG_RESEAU",          "Comportement informationnel des produits", "Configurer un réseau : fonctionnement d'un switch et d'un routeur, adressage statique et dynamique", 23),
            ("TERMINALE_STI2D", "SIN_COMP_REQUETES_RESEAU",        "Comportement informationnel des produits", "Mettre en évidence expérimentalement les requêtes d'un réseau (trames TCP/IP, UDP, sockets, FTP, HTTP)", 24),
            ("TERMINALE_STI2D", "SIN_COMP_TEMPS_REEL",             "Comportement informationnel des produits", "Caractériser un système temps réel : temps de cycle, interruptions sur entrées ou cycliques", 25),
            ("TERMINALE_STI2D", "SIN_CONC_CHOIX_LOGICIEL",         "Éco-conception des produits", "Choisir bibliothèques, IDE, unité de traitement (microcontrôleur, nano-ordinateur) et interfaces de communication", 26),
            ("TERMINALE_STI2D", "SIN_CONC_DOMOTIQUE",              "Éco-conception des produits", "Concevoir des fonctionnalités domotiques pour le pilotage automatisé d'un bâtiment selon ses usages", 27),
            ("TERMINALE_STI2D", "SIN_CONC_ENTREES_SORTIES",        "Éco-conception des produits", "Lister les entrées/sorties d'un système selon leur nature et identifier les méthodes utiles des bibliothèques", 28),
            ("TERMINALE_STI2D", "SIN_CONC_CODAGE",                 "Éco-conception des produits", "Coder en Python ou C++ (pages web en HTML/CSS et PHP) en respectant les règles d'écriture", 29),
            ("TERMINALE_STI2D", "SIN_CONC_MISE_AU_POINT",          "Éco-conception des produits", "Mettre au point un programme : débogage pas à pas, points d'arrêt, fusion de programmes", 30),
            ("TERMINALE_STI2D", "SIN_SOL_CAPTEURS",                "Solutions constructives", "Caractériser capteurs, détecteurs et amplificateurs par leurs relations d'entrée/sortie", 31),
            ("TERMINALE_STI2D", "SIN_SOL_FILTRES_CAN",             "Solutions constructives", "Mettre en œuvre un filtre passe-bas du premier ordre et un convertisseur analogique-numérique", 32),
            ("TERMINALE_STI2D", "SIN_SOL_IHM",                     "Solutions constructives", "Choisir les constituants d'une IHM : afficheur, clavier, écran, interfaces tactiles ou haptiques", 33),
            ("TERMINALE_STI2D", "SIN_SOL_PROGRAMMABLES",           "Solutions constructives", "Mettre en œuvre cartes à microcontrôleur, nano-ordinateurs et objets connectés à internet", 34),
            ("TERMINALE_STI2D", "SIN_SOL_RESEAU_BUS",              "Solutions constructives", "Utiliser commutateurs, routeurs et serveurs et caractériser les bus de communication (longueur, débit, fiabilité)", 35),
            ("TERMINALE_STI2D", "SIN_SOL_SANS_FIL",                "Solutions constructives", "Mettre en œuvre des modules de transmission sans fil (WiFi, Bluetooth, RF)", 36),
            ("TERMINALE_STI2D", "SIN_PROTO_VIRTUALISATION",        "Prototypage et expérimentations", "Virtualiser une solution logicielle à l'aide de machines virtuelles", 37),
            ("TERMINALE_STI2D", "SIN_EXP_CHAINE_INFO",             "Prototypage et expérimentations", "Expérimenter des constituants standard de la chaîne d'information pour en vérifier les fonctionnalités", 38),
        };
    }
}
