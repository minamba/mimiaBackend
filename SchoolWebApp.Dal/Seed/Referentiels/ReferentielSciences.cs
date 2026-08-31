namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel de sciences et technologie, du CP à la sixième.
    ///
    /// DEUX PROGRAMMES SOUS UNE SEULE MATIÈRE
    /// -------------------------------------
    /// Du CP au CE2, la matière s'appelle « Questionner le monde » : on
    /// observe, on décrit, on classe. Du CM1 à la sixième elle devient
    /// « Sciences et technologie » et se structure en quatre domaines — la
    /// matière et l'énergie, le vivant, les objets techniques, la planète
    /// Terre. Le référentiel suit ce changement de nature ; c'est pour cela
    /// que les domaines du primaire et ceux du cycle 3 ne portent pas les
    /// mêmes noms.
    ///
    /// LA DÉMARCHE EST UNE COMPÉTENCE, PAS UN PRÉAMBULE
    /// -----------------------------------------------
    /// Formuler une hypothèse, imaginer un protocole, distinguer ce qu'on
    /// observe de ce qu'on en déduit : ces gestes sont évalués au même titre
    /// que les connaissances, et ce sont eux qui se transmettent ensuite à la
    /// physique-chimie et à la SVT. Ils ont donc leur propre domaine, et ce
    /// sont eux qui portent l'essentiel du graphe.
    /// </summary>
    public static class ReferentielSciences
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- CP : observer et décrire ---
                ("CP", "SC_CP_VIV_ANIMAUX",    "Le vivant", "Distinguer le vivant du non-vivant", 1),
                ("CP", "SC_CP_VIV_BESOINS",    "Le vivant", "Nommer les besoins d'une plante et d'un animal", 2),
                ("CP", "SC_CP_VIV_CORPS",      "Le vivant", "Nommer les parties du corps et les cinq sens", 3),
                ("CP", "SC_CP_VIV_HYGIENE",    "Le vivant", "Expliquer une règle d'hygiène et à quoi elle sert", 4),
                ("CP", "SC_CP_MAT_ETATS",      "La matière", "Reconnaître l'eau liquide et la glace", 5),
                ("CP", "SC_CP_MAT_OBJETS",     "La matière", "Classer des objets selon leur matière", 6),
                ("CP", "SC_CP_TEMPS_JOURNEE",  "Le temps et l'espace", "Situer les moments de la journée et les jours de la semaine", 7),
                ("CP", "SC_CP_TEMPS_SAISONS",  "Le temps et l'espace", "Nommer les saisons et ce qui les caractérise", 8),
                ("CP", "SC_CP_TECH_UTILISER",  "Objets techniques", "Utiliser un objet simple en respectant sa fonction", 9),
                ("CP", "SC_CP_DEM_OBSERVER",   "Démarche", "Décrire ce qu'on observe sans l'interpréter", 10),
                ("CP", "SC_CP_DEM_COMPARER",   "Démarche", "Comparer deux objets et dire ce qui les différencie", 11),

                // --- CE1 ---
                ("CE1", "SC_CE1_VIV_CYCLE",     "Le vivant", "Décrire les étapes de la vie d'une plante ou d'un animal", 1),
                ("CE1", "SC_CE1_VIV_CLASSER",   "Le vivant", "Classer des animaux selon un critère observable", 2),
                ("CE1", "SC_CE1_VIV_ALIMENTATION","Le vivant", "Distinguer les grandes familles d'aliments", 3),
                ("CE1", "SC_CE1_MAT_ETATS",     "La matière", "Décrire le passage de l'eau d'un état à l'autre", 4),
                ("CE1", "SC_CE1_MAT_MELANGES",  "La matière", "Distinguer ce qui se dissout de ce qui ne se dissout pas", 5),
                ("CE1", "SC_CE1_TEMPS_MESURER", "Le temps et l'espace", "Utiliser un calendrier et lire l'heure", 6),
                ("CE1", "SC_CE1_ESPACE_PLAN",   "Le temps et l'espace", "Se repérer sur un plan de la classe ou du quartier", 7),
                ("CE1", "SC_CE1_TECH_FONCTION", "Objets techniques", "Dire à quel besoin répond un objet technique", 8),
                ("CE1", "SC_CE1_DEM_MESURER",   "Démarche", "Mesurer une longueur, une masse ou une durée avec l'instrument adapté", 9),
                ("CE1", "SC_CE1_DEM_TABLEAU",   "Démarche", "Ranger ses observations dans un tableau", 10),
                ("CE1", "SC_CE1_DEM_HYPOTHESE", "Démarche", "Dire ce qu'on pense qu'il va se passer avant de le vérifier", 11),

                // --- CE2 ---
                ("CE2", "SC_CE2_VIV_REGIMES",    "Le vivant", "Établir une chaîne alimentaire simple", 1),
                ("CE2", "SC_CE2_VIV_DENTS",      "Le vivant", "Relier la denture d'un animal à son régime alimentaire", 2),
                ("CE2", "SC_CE2_VIV_CROISSANCE", "Le vivant", "Décrire la croissance humaine et ses étapes", 3),
                ("CE2", "SC_CE2_MAT_AIR",        "La matière", "Montrer que l'air est de la matière", 4),
                ("CE2", "SC_CE2_MAT_TEMPERATURE","La matière", "Relier un changement d'état à une température", 5),
                ("CE2", "SC_CE2_ESPACE_MILIEUX", "Le temps et l'espace", "Décrire un milieu et les êtres vivants qui l'occupent", 6),
                ("CE2", "SC_CE2_TECH_MONTAGE",   "Objets techniques", "Réaliser un montage simple en suivant une notice", 7),
                ("CE2", "SC_CE2_DEM_PROTOCOLE",  "Démarche", "Imaginer une expérience pour vérifier une hypothèse", 8),
                ("CE2", "SC_CE2_DEM_CONCLURE",   "Démarche", "Conclure à partir de ce qu'on a réellement observé", 9),
                ("CE2", "SC_CE2_DEM_SCHEMA",     "Démarche", "Dessiner un schéma légendé de ce qu'on a observé", 10),
                ("CE2", "SC_CE2_DEM_UNITES",     "Démarche", "Écrire une mesure avec son unité", 11),

                // --- CM1 : la matière devient « Sciences et technologie » ---
                ("CM1", "SC_CM1_MAT_ETATS",      "Matière, mouvement, énergie", "Identifier les trois états de la matière et les changements d'état", 1),
                ("CM1", "SC_CM1_MAT_MELANGES",   "Matière, mouvement, énergie", "Séparer les constituants d'un mélange par décantation ou filtration", 2),
                ("CM1", "SC_CM1_MAT_MASSE",      "Matière, mouvement, énergie", "Montrer que la masse se conserve lors d'un changement d'état", 3),
                ("CM1", "SC_CM1_ENERGIE_SOURCES","Matière, mouvement, énergie", "Identifier des sources et des formes d'énergie", 4),
                ("CM1", "SC_CM1_VIV_CLASSER",    "Le vivant", "Classer les êtres vivants d'après leurs attributs communs", 5),
                ("CM1", "SC_CM1_VIV_NUTRITION",  "Le vivant", "Expliquer les besoins alimentaires de l'être humain", 6),
                ("CM1", "SC_CM1_VIV_REPRODUCTION","Le vivant", "Décrire la reproduction sexuée chez les animaux et les plantes", 7),
                ("CM1", "SC_CM1_TECH_FONCTIONS", "Objets techniques", "Décrire les fonctions d'un objet technique et ses éléments", 8),
                ("CM1", "SC_CM1_TERRE_SEISMES",  "La planète Terre", "Décrire un phénomène géologique : séisme, volcan, érosion", 9),
                ("CM1", "SC_CM1_TERRE_METEO",    "La planète Terre", "Distinguer météo et climat", 10),
                ("CM1", "SC_CM1_DEM_PROTOCOLE",  "Démarche", "Concevoir une expérience en ne faisant varier qu'un seul facteur", 11),
                ("CM1", "SC_CM1_DEM_GRAPHIQUE",  "Démarche", "Lire et construire un graphique à partir de mesures", 12),
                ("CM1", "SC_CM1_DEM_OBS_DEDUIT", "Démarche", "Distinguer ce qu'on observe de ce qu'on en déduit", 13),

                // --- CM2 ---
                ("CM2", "SC_CM2_MAT_MOUVEMENT",  "Matière, mouvement, énergie", "Décrire un mouvement par sa trajectoire et sa vitesse", 1),
                ("CM2", "SC_CM2_MAT_SOLUTION",   "Matière, mouvement, énergie", "Distinguer un mélange homogène d'un mélange hétérogène", 2),
                ("CM2", "SC_CM2_ENERGIE_CIRCUIT","Matière, mouvement, énergie", "Réaliser un circuit électrique simple et repérer une boucle", 3),
                ("CM2", "SC_CM2_ENERGIE_CONVERSION","Matière, mouvement, énergie", "Suivre une chaîne de conversion d'énergie", 4),
                ("CM2", "SC_CM2_VIV_DIGESTION",  "Le vivant", "Décrire le trajet des aliments dans le corps", 5),
                ("CM2", "SC_CM2_VIV_RESPIRATION","Le vivant", "Relier respiration et circulation sanguine", 6),
                ("CM2", "SC_CM2_VIV_ECOSYSTEME", "Le vivant", "Construire un réseau alimentaire dans un milieu", 7),
                ("CM2", "SC_CM2_TECH_MATERIAUX", "Objets techniques", "Choisir un matériau selon la propriété recherchée", 8),
                ("CM2", "SC_CM2_TECH_INFORMATION","Objets techniques", "Décrire comment une information est transmise et stockée", 9),
                ("CM2", "SC_CM2_TERRE_SYSTEME",  "La planète Terre", "Situer la Terre dans le système solaire et expliquer l'alternance jour-nuit", 10),
                ("CM2", "SC_CM2_TERRE_RESSOURCES","La planète Terre", "Distinguer une ressource renouvelable d'une ressource épuisable", 11),
                ("CM2", "SC_CM2_DEM_EXPERIENCE", "Démarche", "Mener une expérience et en tirer une conclusion écrite", 12),

                // --- 6e : fin du cycle 3 ---
                ("SIXIEME", "SC_6E_MAT_ETATS",     "Matière, mouvement, énergie", "Décrire les états et les changements d'état à l'échelle de la matière", 1),
                ("SIXIEME", "SC_6E_MAT_MELANGES",  "Matière, mouvement, énergie", "Réaliser et interpréter une filtration et une décantation", 2),
                ("SIXIEME", "SC_6E_MAT_MASSE_VOLUME","Matière, mouvement, énergie", "Mesurer une masse et un volume et les distinguer", 3),
                ("SIXIEME", "SC_6E_MAT_MOUVEMENT", "Matière, mouvement, énergie", "Caractériser un mouvement et calculer une vitesse moyenne", 4),
                ("SIXIEME", "SC_6E_ENERGIE_FORMES","Matière, mouvement, énergie", "Identifier les formes d'énergie et leurs conversions", 5),
                ("SIXIEME", "SC_6E_ENERGIE_CIRCUIT","Matière, mouvement, énergie", "Schématiser un circuit électrique avec les symboles normalisés", 6),
                ("SIXIEME", "SC_6E_ENERGIE_SECURITE","Matière, mouvement, énergie", "Expliquer les règles de sécurité électrique", 7),
                ("SIXIEME", "SC_6E_VIV_CLASSIFICATION","Le vivant", "Classer les êtres vivants en groupes emboîtés", 8),
                ("SIXIEME", "SC_6E_VIV_CELLULE",   "Le vivant", "Reconnaître que les êtres vivants sont constitués de cellules", 9),
                ("SIXIEME", "SC_6E_VIV_NUTRITION", "Le vivant", "Expliquer les besoins nutritifs des végétaux et des animaux", 10),
                ("SIXIEME", "SC_6E_VIV_REPRODUCTION","Le vivant", "Comparer reproduction sexuée et reproduction asexuée", 11),
                ("SIXIEME", "SC_6E_VIV_PEUPLEMENT","Le vivant", "Relier le peuplement d'un milieu aux saisons", 12),
                ("SIXIEME", "SC_6E_TECH_BESOIN",   "Objets techniques", "Identifier le besoin auquel répond un objet et ses contraintes", 13),
                ("SIXIEME", "SC_6E_TECH_MATERIAUX","Objets techniques", "Comparer les propriétés de familles de matériaux", 14),
                ("SIXIEME", "SC_6E_TECH_ALGORITHME","Objets techniques", "Écrire un programme simple pilotant un objet", 15),
                ("SIXIEME", "SC_6E_TECH_RESEAU",   "Objets techniques", "Décrire le trajet d'une information dans un réseau", 16),
                ("SIXIEME", "SC_6E_TERRE_SOLAIRE", "La planète Terre", "Expliquer les saisons et les phases de la Lune", 17),
                ("SIXIEME", "SC_6E_TERRE_RISQUES", "La planète Terre", "Distinguer un aléa d'un risque naturel", 18),
                ("SIXIEME", "SC_6E_TERRE_METEO",   "La planète Terre", "Distinguer météorologie et climatologie et lire des données", 19),
                ("SIXIEME", "SC_6E_DEM_HYPOTHESE", "Démarche", "Formuler une hypothèse et proposer un moyen de la tester", 20),
                ("SIXIEME", "SC_6E_DEM_PROTOCOLE", "Démarche", "Concevoir un protocole avec un témoin et un seul paramètre variable", 21),
                ("SIXIEME", "SC_6E_DEM_MESURE",    "Démarche", "Exprimer un résultat avec son unité et un ordre de grandeur", 22),
                ("SIXIEME", "SC_6E_DEM_GRAPHIQUE", "Démarche", "Construire et exploiter un graphique de mesures", 23),
                ("SIXIEME", "SC_6E_DEM_CONCLURE",  "Démarche", "Rédiger une conclusion qui répond à la question posée", 24),
            };

        /// <summary>
        /// Le graphe des sciences est celui de la DÉMARCHE. Les contenus se
        /// juxtaposent plus qu'ils ne s'enchaînent — on peut comprendre les
        /// volcans sans rien savoir des circuits — mais on ne peut pas
        /// concevoir un protocole sans savoir formuler une hypothèse, ni
        /// conclure sans distinguer observation et déduction.
        ///
        /// Ces arêtes-là sont celles qui comptent : elles sortent de la matière
        /// et alimentent ensuite la physique-chimie et la SVT du collège.
        /// </summary>
        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // La chaîne de la démarche, du CP à la 6e.
                ("SC_CE1_DEM_HYPOTHESE",     "SC_CP_DEM_OBSERVER",       3),
                ("SC_CE1_DEM_TABLEAU",       "SC_CP_DEM_COMPARER",       2),
                ("SC_CE2_DEM_PROTOCOLE",     "SC_CE1_DEM_HYPOTHESE",     3),
                ("SC_CE2_DEM_CONCLURE",      "SC_CE2_DEM_PROTOCOLE",     3),
                ("SC_CE2_DEM_SCHEMA",        "SC_CP_DEM_OBSERVER",       2),
                ("SC_CE2_DEM_UNITES",        "SC_CE1_DEM_MESURER",       3),
                ("SC_CM1_DEM_PROTOCOLE",     "SC_CE2_DEM_PROTOCOLE",     3),
                ("SC_CM1_DEM_OBS_DEDUIT",    "SC_CE2_DEM_CONCLURE",      3),
                ("SC_CM1_DEM_GRAPHIQUE",     "SC_CE1_DEM_TABLEAU",       3),
                ("SC_CM2_DEM_EXPERIENCE",    "SC_CM1_DEM_PROTOCOLE",     3),
                ("SC_CM2_DEM_EXPERIENCE",    "SC_CM1_DEM_OBS_DEDUIT",    3),
                ("SC_6E_DEM_HYPOTHESE",      "SC_CE1_DEM_HYPOTHESE",     3),
                ("SC_6E_DEM_PROTOCOLE",      "SC_CM1_DEM_PROTOCOLE",     3),
                ("SC_6E_DEM_PROTOCOLE",      "SC_6E_DEM_HYPOTHESE",      3),
                ("SC_6E_DEM_MESURE",         "SC_CE2_DEM_UNITES",        3),
                ("SC_6E_DEM_GRAPHIQUE",      "SC_CM1_DEM_GRAPHIQUE",     3),
                ("SC_6E_DEM_CONCLURE",       "SC_CM2_DEM_EXPERIENCE",    3),
                ("SC_6E_DEM_CONCLURE",       "SC_CM1_DEM_OBS_DEDUIT",    3),

                // Lire un graphique de mesures suppose de savoir lire un
                // graphique tout court, et calculer une vitesse suppose la
                // division. Les sciences révèlent souvent une lacune en maths.
                ("SC_CM1_DEM_GRAPHIQUE",     "MATH_CM1_DATA_TABLEAU",   2),
                ("SC_6E_MAT_MOUVEMENT",      "MATH_4E_PROP_VITESSE",     2),
                ("SC_6E_DEM_MESURE",         "MATH_CM1_MES_CONVERSION",   2),

                // Les contenus, quand l'enchaînement est réel.
                ("SC_CE1_MAT_ETATS",         "SC_CP_MAT_ETATS",          3),
                ("SC_CE1_VIV_CYCLE",         "SC_CP_VIV_BESOINS",        2),
                ("SC_CE1_VIV_CLASSER",       "SC_CP_VIV_ANIMAUX",        3),
                ("SC_CE2_MAT_TEMPERATURE",   "SC_CE1_MAT_ETATS",         3),
                ("SC_CE2_VIV_REGIMES",       "SC_CE1_VIV_ALIMENTATION",  2),
                ("SC_CE2_VIV_DENTS",         "SC_CE2_VIV_REGIMES",       2),
                ("SC_CM1_MAT_ETATS",         "SC_CE2_MAT_TEMPERATURE",   3),
                ("SC_CM1_MAT_MELANGES",      "SC_CE1_MAT_MELANGES",      3),
                ("SC_CM1_MAT_MASSE",         "SC_CM1_MAT_ETATS",         3),
                ("SC_CM1_VIV_CLASSER",       "SC_CE1_VIV_CLASSER",       3),
                ("SC_CM1_TECH_FONCTIONS",    "SC_CE1_TECH_FONCTION",     3),
                ("SC_CM2_MAT_SOLUTION",      "SC_CM1_MAT_MELANGES",      3),
                ("SC_CM2_VIV_ECOSYSTEME",    "SC_CE2_VIV_REGIMES",       3),
                ("SC_CM2_VIV_DIGESTION",     "SC_CM1_VIV_NUTRITION",     3),
                ("SC_CM2_ENERGIE_CONVERSION","SC_CM1_ENERGIE_SOURCES",   3),
                ("SC_CM2_TERRE_RESSOURCES",  "SC_CM1_ENERGIE_SOURCES",   2),
                ("SC_6E_MAT_ETATS",          "SC_CM1_MAT_ETATS",         3),
                ("SC_6E_MAT_MELANGES",       "SC_CM2_MAT_SOLUTION",      3),
                ("SC_6E_MAT_MASSE_VOLUME",   "SC_CM1_MAT_MASSE",         3),
                ("SC_6E_MAT_MOUVEMENT",      "SC_CM2_MAT_MOUVEMENT",     3),
                ("SC_6E_ENERGIE_FORMES",     "SC_CM2_ENERGIE_CONVERSION",3),
                ("SC_6E_ENERGIE_CIRCUIT",    "SC_CM2_ENERGIE_CIRCUIT",   3),
                ("SC_6E_ENERGIE_SECURITE",   "SC_6E_ENERGIE_CIRCUIT",    2),
                ("SC_6E_VIV_CLASSIFICATION", "SC_CM1_VIV_CLASSER",       3),
                ("SC_6E_VIV_NUTRITION",      "SC_CM2_VIV_DIGESTION",     3),
                ("SC_6E_VIV_REPRODUCTION",   "SC_CM1_VIV_REPRODUCTION",  3),
                ("SC_6E_VIV_PEUPLEMENT",     "SC_CM2_VIV_ECOSYSTEME",    3),
                ("SC_6E_TECH_BESOIN",        "SC_CM1_TECH_FONCTIONS",    3),
                ("SC_6E_TECH_MATERIAUX",     "SC_CM2_TECH_MATERIAUX",    3),
                ("SC_6E_TECH_RESEAU",        "SC_CM2_TECH_INFORMATION",  3),
                ("SC_6E_TERRE_SOLAIRE",      "SC_CM2_TERRE_SYSTEME",     3),
                ("SC_6E_TERRE_RISQUES",      "SC_CM1_TERRE_SEISMES",     3),
                ("SC_6E_TERRE_METEO",        "SC_CM1_TERRE_METEO",       3),
            };
    }
}
