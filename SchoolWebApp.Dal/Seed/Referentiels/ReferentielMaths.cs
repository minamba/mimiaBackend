namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel de mathématiques, du CP à la terminale.
    ///
    /// Le contenu vient d'Éduscol. Un libellé décrit un GESTE
    /// vérifiable : « Additionner deux fractions de même dénominateur », pas « les fractions ».
    /// </summary>
    public static class ReferentielMaths
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- CP : les tout premiers nombres ---
                // Sans ces niveaux, un élève de CP n'a AUCUNE compétence à
                // laquelle rattacher ce qu'il fait : l'observation ne trouve
                // rien à écrire, et la mémoire longue reste vide.
                ("CP", "MATH_CP_NUM_100",          "Nombres et calculs",  "Lire, écrire et comparer les nombres jusqu'à 100", 1),
                ("CP", "MATH_CP_NUM_DECOMPOSER",   "Nombres et calculs",  "Décomposer un nombre en dizaines et unités", 2),
                ("CP", "MATH_CP_CALC_ADD_10",      "Nombres et calculs",  "Additionner deux nombres jusqu'à 10", 3),
                ("CP", "MATH_CP_CALC_COMPLEMENT",  "Nombres et calculs",  "Trouver le complément à 10", 4),
                ("CP", "MATH_CP_CALC_ADD_DIZAINE", "Nombres et calculs",  "Additionner en franchissant la dizaine", 5),
                ("CP", "MATH_CP_CALC_SOUS",        "Nombres et calculs",  "Soustraire deux nombres inférieurs à 20", 6),
                ("CP", "MATH_CP_CALC_DOUBLE",      "Nombres et calculs",  "Connaître les doubles et les moitiés jusqu'à 20", 7),
                ("CP", "MATH_CP_PROB_UNE_ETAPE",   "Nombres et calculs",  "Résoudre un problème en une seule étape", 8),
                ("CP", "MATH_CP_MES_LONGUEUR",     "Grandeurs et mesures","Comparer et mesurer des longueurs", 9),
                ("CP", "MATH_CP_MES_TEMPS",        "Grandeurs et mesures","Se repérer dans le temps et lire l'heure entière", 10),
                ("CP", "MATH_CP_GEO_FIGURES",      "Espace et géométrie", "Reconnaître carré, rectangle, triangle et cercle", 11),
                ("CP", "MATH_CP_GEO_REPERAGE",     "Espace et géométrie", "Se repérer dans l'espace et sur un quadrillage", 12),

                // --- CE1 ---
                ("CE1", "MATH_CE1_NUM_1000",       "Nombres et calculs",  "Lire, écrire et comparer les nombres jusqu'à 1000", 1),
                ("CE1", "MATH_CE1_NUM_DECOMPOSER", "Nombres et calculs",  "Décomposer en centaines, dizaines et unités", 2),
                ("CE1", "MATH_CE1_CALC_ADD_POSEE", "Nombres et calculs",  "Poser une addition avec retenue", 3),
                ("CE1", "MATH_CE1_CALC_SOUS_POSEE","Nombres et calculs",  "Poser une soustraction", 4),
                ("CE1", "MATH_CE1_MULT_SENS",      "Nombres et calculs",  "Comprendre le sens de la multiplication", 5),
                ("CE1", "MATH_CE1_TABLES_BASE",    "Nombres et calculs",  "Connaître les tables de 2, 3, 4 et 5", 6),
                ("CE1", "MATH_CE1_PROB_DEUX_ETAPES","Nombres et calculs", "Résoudre un problème en deux étapes", 7),
                ("CE1", "MATH_CE1_MES_LONGUEUR",   "Grandeurs et mesures","Mesurer en mètres et centimètres", 8),
                ("CE1", "MATH_CE1_MES_MASSE",      "Grandeurs et mesures","Comparer et mesurer des masses", 9),
                ("CE1", "MATH_CE1_MES_HEURE",      "Grandeurs et mesures","Lire l'heure et la demi-heure", 10),
                ("CE1", "MATH_CE1_MES_MONNAIE",    "Grandeurs et mesures","Rendre la monnaie et calculer un prix", 11),
                ("CE1", "MATH_CE1_GEO_PROPRIETES", "Espace et géométrie", "Décrire une figure par ses propriétés", 12),
                ("CE1", "MATH_CE1_GEO_ALIGNEMENT", "Espace et géométrie", "Tracer et reconnaître des points alignés", 13),

                // --- CE2 ---
                ("CE2", "MATH_CE2_NUM_10000",      "Nombres et calculs",  "Lire, écrire et comparer les nombres jusqu'à 10 000", 1),
                ("CE2", "MATH_CE2_TABLES",         "Nombres et calculs",  "Connaître toutes les tables de multiplication", 2),
                ("CE2", "MATH_CE2_MULT_POSEE",     "Nombres et calculs",  "Poser une multiplication à un chiffre", 3),
                ("CE2", "MATH_CE2_DIV_SENS",       "Nombres et calculs",  "Comprendre le sens de la division et du partage", 4),
                ("CE2", "MATH_CE2_FRAC_DECOUVERTE","Nombres et calculs",  "Découvrir les demis, tiers et quarts", 5),
                ("CE2", "MATH_CE2_PROB_MULT",      "Nombres et calculs",  "Résoudre un problème multiplicatif", 6),
                ("CE2", "MATH_CE2_MES_CONVERSION", "Grandeurs et mesures","Convertir les unités de longueur usuelles", 7),
                ("CE2", "MATH_CE2_MES_DUREE",      "Grandeurs et mesures","Calculer une durée en heures et minutes", 8),
                ("CE2", "MATH_CE2_MES_PERIMETRE",  "Grandeurs et mesures","Calculer le périmètre d'un polygone", 9),
                ("CE2", "MATH_CE2_GEO_ANGLE_DROIT","Espace et géométrie", "Reconnaître et tracer un angle droit", 10),
                ("CE2", "MATH_CE2_GEO_SYMETRIE",   "Espace et géométrie", "Reconnaître un axe de symétrie", 11),

                // --- CM1 ---
                ("CM1", "MATH_CM1_NUM_MILLIONS",   "Nombres et calculs",  "Lire et écrire les grands nombres entiers", 1),
                ("CM1", "MATH_CM1_NUM_DECIMAUX",   "Nombres et calculs",  "Découvrir les nombres décimaux", 2),
                ("CM1", "MATH_CM1_FRAC_SENS",      "Nombres et calculs",  "Comprendre et écrire une fraction simple", 3),
                ("CM1", "MATH_CM1_MULT_2CHIFFRES", "Nombres et calculs",  "Poser une multiplication à deux chiffres", 4),
                ("CM1", "MATH_CM1_DIV_POSEE",      "Nombres et calculs",  "Poser une division par un nombre à un chiffre", 5),
                ("CM1", "MATH_CM1_CALC_MENTAL",    "Nombres et calculs",  "Calculer mentalement sur les nombres entiers", 6),
                ("CM1", "MATH_CM1_PROB_ETAPES",    "Nombres et calculs",  "Résoudre un problème à plusieurs étapes", 7),
                ("CM1", "MATH_CM1_MES_CONVERSION", "Grandeurs et mesures","Convertir longueurs, masses et contenances", 8),
                ("CM1", "MATH_CM1_MES_AIRE",       "Grandeurs et mesures","Comparer et mesurer des aires", 9),
                ("CM1", "MATH_CM1_MES_DUREE",      "Grandeurs et mesures","Calculer avec des durées", 10),
                ("CM1", "MATH_CM1_GEO_PERPENDICULAIRE","Espace et géométrie","Tracer perpendiculaires et parallèles", 11),
                ("CM1", "MATH_CM1_GEO_SYMETRIE",   "Espace et géométrie", "Construire le symétrique d'une figure simple", 12),
                ("CM1", "MATH_CM1_DATA_TABLEAU",   "Gestion de données",  "Lire et compléter un tableau", 13),

                // --- CM2 : socle amont, sert de cible au diagnostic remontant ---
                ("CM2", "MATH_CM2_NUM_ENTIERS",      "Nombres et calculs",  "Lire, écrire et comparer les nombres entiers", 1),
                ("CM2", "MATH_CM2_NUM_DECIMAUX",     "Nombres et calculs",  "Lire et écrire les nombres décimaux", 2),
                ("CM2", "MATH_CM2_CALC_ADD_SOUS",    "Nombres et calculs",  "Poser une addition et une soustraction", 3),
                ("CM2", "MATH_CM2_CALC_MULT",        "Nombres et calculs",  "Poser une multiplication", 4),
                ("CM2", "MATH_CM2_CALC_DIV",         "Nombres et calculs",  "Poser une division euclidienne", 5),
                ("CM2", "MATH_CM2_FRAC_INTRO",       "Nombres et calculs",  "Comprendre le sens d'une fraction simple", 6),
                ("CM2", "MATH_CM2_MES_LONGUEUR",     "Grandeurs et mesures","Utiliser les unités de longueur", 7),
                ("CM2", "MATH_CM2_MES_AIRE",         "Grandeurs et mesures","Calculer l'aire d'un rectangle", 8),
                ("CM2", "MATH_CM2_GEO_FIGURES",      "Espace et géométrie", "Reconnaître les figures usuelles", 9),
                ("CM2", "MATH_CM2_GEO_PERPENDICULAIRE","Espace et géométrie","Reconnaître droites perpendiculaires et parallèles", 10),
                ("CM2", "MATH_CM2_GEO_SYMETRIE",     "Espace et géométrie", "Construire le symétrique d'une figure", 11),
                ("CM2", "MATH_CM2_DATA_TABLEAU",     "Gestion de données",  "Lire les informations d'un tableau", 12),

                // --- 6e : Nombres et calculs ---
                ("SIXIEME", "MATH_6E_NUM_ENTIERS",      "Nombres et calculs", "Lire, écrire, comparer et ranger les nombres entiers", 1),
                ("SIXIEME", "MATH_6E_NUM_DECIMAUX",     "Nombres et calculs", "Lire, écrire, comparer et ranger les nombres décimaux", 2),
                ("SIXIEME", "MATH_6E_NUM_DEMI_DROITE",  "Nombres et calculs", "Repérer et placer un nombre sur une demi-droite graduée", 3),
                ("SIXIEME", "MATH_6E_NUM_ARRONDI",      "Nombres et calculs", "Encadrer, arrondir, donner une valeur approchée", 4),
                ("SIXIEME", "MATH_6E_CALC_ADD",         "Nombres et calculs", "Additionner et soustraire des nombres décimaux", 5),
                ("SIXIEME", "MATH_6E_CALC_MULT",        "Nombres et calculs", "Multiplier des nombres décimaux", 6),
                ("SIXIEME", "MATH_6E_CALC_DIV_EUCL",    "Nombres et calculs", "Effectuer une division euclidienne : quotient et reste", 7),
                ("SIXIEME", "MATH_6E_CALC_DIV_DEC",     "Nombres et calculs", "Effectuer une division décimale", 8),
                ("SIXIEME", "MATH_6E_CALC_MENTAL",      "Nombres et calculs", "Calculer mentalement et estimer un ordre de grandeur", 9),
                ("SIXIEME", "MATH_6E_CALC_PRIORITES",   "Nombres et calculs", "Appliquer les priorités opératoires", 10),
                ("SIXIEME", "MATH_6E_DIVISIBILITE",     "Nombres et calculs", "Utiliser les critères de divisibilité, multiples et diviseurs", 11),
                ("SIXIEME", "MATH_6E_FRAC_SENS",        "Nombres et calculs", "Comprendre le sens d'une fraction", 12),
                ("SIXIEME", "MATH_6E_FRAC_REPERER",     "Nombres et calculs", "Placer une fraction sur une demi-droite graduée", 13),
                ("SIXIEME", "MATH_6E_FRAC_EGALES",      "Nombres et calculs", "Reconnaître des fractions égales et simplifier", 14),
                ("SIXIEME", "MATH_6E_FRAC_COMPARER",    "Nombres et calculs", "Comparer deux fractions", 15),
                ("SIXIEME", "MATH_6E_FRAC_ADD",         "Nombres et calculs", "Additionner des fractions de même dénominateur", 16),
                ("SIXIEME", "MATH_6E_FRAC_QUANTITE",    "Nombres et calculs", "Calculer une fraction d'une quantité", 17),

                // --- 6e : Proportionnalité et gestion de données ---
                ("SIXIEME", "MATH_6E_PROP_RECONNAITRE", "Gestion de données", "Reconnaître une situation de proportionnalité", 18),
                ("SIXIEME", "MATH_6E_PROP_COEFF",       "Gestion de données", "Utiliser le coefficient de proportionnalité", 19),
                ("SIXIEME", "MATH_6E_PROP_POURCENT",    "Gestion de données", "Appliquer un pourcentage simple", 20),
                ("SIXIEME", "MATH_6E_PROP_ECHELLE",     "Gestion de données", "Utiliser une échelle", 21),
                ("SIXIEME", "MATH_6E_DATA_TABLEAU",     "Gestion de données", "Lire et construire un tableau de données", 22),
                ("SIXIEME", "MATH_6E_DATA_GRAPHIQUE",   "Gestion de données", "Lire et construire un diagramme", 23),

                // --- 6e : Grandeurs et mesures ---
                ("SIXIEME", "MATH_6E_MES_LONGUEUR",     "Grandeurs et mesures", "Convertir des unités de longueur", 24),
                ("SIXIEME", "MATH_6E_MES_MASSE",        "Grandeurs et mesures", "Convertir des masses et des contenances", 25),
                ("SIXIEME", "MATH_6E_MES_DUREE",        "Grandeurs et mesures", "Calculer avec des durées et des horaires", 26),
                ("SIXIEME", "MATH_6E_MES_PERIMETRE",    "Grandeurs et mesures", "Calculer le périmètre des figures usuelles", 27),
                ("SIXIEME", "MATH_6E_MES_AIRE",         "Grandeurs et mesures", "Calculer l'aire d'un rectangle et d'un triangle", 28),
                ("SIXIEME", "MATH_6E_MES_VOLUME",       "Grandeurs et mesures", "Calculer le volume d'un pavé droit", 29),
                ("SIXIEME", "MATH_6E_MES_ANGLE",        "Grandeurs et mesures", "Mesurer et construire un angle", 30),

                // --- 6e : Espace et géométrie ---
                ("SIXIEME", "MATH_6E_GEO_VOCABULAIRE",  "Espace et géométrie", "Utiliser le vocabulaire : point, droite, segment, demi-droite", 31),
                ("SIXIEME", "MATH_6E_GEO_PERPENDICULAIRE","Espace et géométrie","Tracer et reconnaître des droites perpendiculaires", 32),
                ("SIXIEME", "MATH_6E_GEO_PARALLELE",    "Espace et géométrie", "Tracer et reconnaître des droites parallèles", 33),
                ("SIXIEME", "MATH_6E_GEO_CERCLE",       "Espace et géométrie", "Construire un cercle : centre, rayon, diamètre", 34),
                ("SIXIEME", "MATH_6E_GEO_TRIANGLES",    "Espace et géométrie", "Reconnaître et construire les triangles particuliers", 35),
                ("SIXIEME", "MATH_6E_GEO_QUADRILATERES","Espace et géométrie", "Reconnaître et construire les quadrilatères particuliers", 36),
                ("SIXIEME", "MATH_6E_GEO_SYMETRIE",     "Espace et géométrie", "Construire le symétrique d'une figure par rapport à un axe", 37),
                ("SIXIEME", "MATH_6E_GEO_CONSTRUCTION", "Espace et géométrie", "Suivre et rédiger un programme de construction", 38),
                ("SIXIEME", "MATH_6E_GEO_SOLIDES",      "Espace et géométrie", "Reconnaître les solides usuels et leurs patrons", 39),
                ("SIXIEME", "MATH_6E_ALGO_DEPLACEMENT", "Algorithmique",       "Décrire et exécuter un algorithme de déplacement", 40),

                // ============================================================
                // Collège, cycle 4 — 5e, 4e, 3e
                // ============================================================
                //
                // Sans ces trois niveaux, le suivi d'un collégien se rattachait
                // aux compétences de 6e et en deçà : la fenêtre d'observation
                // remonte de cinq ans, elle trouvait donc toujours quelque
                // chose, mais toujours en dessous. Un élève de 3e voyait toutes
                // ses compétences étiquetées « 6e » — mesuré sur le programme
                // qu'il avait quitté trois ans plus tôt.

                // --- 5e : Nombres et calculs ---
                ("CINQUIEME", "MATH_5E_FRAC_ADD",        "Nombres et calculs", "Additionner et soustraire des fractions", 1),
                ("CINQUIEME", "MATH_5E_FRAC_MULT",       "Nombres et calculs", "Multiplier des fractions", 2),
                ("CINQUIEME", "MATH_5E_REL_SENS",        "Nombres et calculs", "Comprendre et repérer les nombres relatifs", 3),
                ("CINQUIEME", "MATH_5E_REL_ADD",         "Nombres et calculs", "Additionner et soustraire des nombres relatifs", 4),
                ("CINQUIEME", "MATH_5E_PRIORITES",       "Nombres et calculs", "Calculer avec les priorités et les parenthèses", 5),
                ("CINQUIEME", "MATH_5E_LITT_EXPRESSION", "Nombres et calculs", "Écrire et réduire une expression littérale", 6),
                ("CINQUIEME", "MATH_5E_LITT_SUBSTITUER", "Nombres et calculs", "Substituer une valeur et tester une égalité", 7),
                ("CINQUIEME", "MATH_5E_DIVISIBILITE",    "Nombres et calculs", "Utiliser multiples, diviseurs et critères de divisibilité", 8),

                // --- 5e : Proportionnalité, données, probabilités ---
                ("CINQUIEME", "MATH_5E_PROP_QUATRIEME",  "Gestion de données", "Résoudre un problème par la quatrième proportionnelle", 9),
                ("CINQUIEME", "MATH_5E_PROP_POURCENT",   "Gestion de données", "Appliquer et calculer un pourcentage", 10),
                ("CINQUIEME", "MATH_5E_PROP_ECHELLE",    "Gestion de données", "Utiliser une échelle et un agrandissement", 11),
                ("CINQUIEME", "MATH_5E_STAT_EFFECTIFS",  "Gestion de données", "Calculer effectifs et fréquences", 12),
                ("CINQUIEME", "MATH_5E_STAT_MOYENNE",    "Gestion de données", "Calculer une moyenne", 13),
                ("CINQUIEME", "MATH_5E_PROBA_INTRO",     "Gestion de données", "Comprendre le hasard et exprimer une probabilité", 14),

                // --- 5e : Grandeurs et mesures ---
                ("CINQUIEME", "MATH_5E_MES_AIRE_PARAL",  "Grandeurs et mesures", "Calculer l'aire d'un parallélogramme et d'un triangle", 15),
                ("CINQUIEME", "MATH_5E_MES_AIRE_DISQUE", "Grandeurs et mesures", "Calculer le périmètre et l'aire d'un disque", 16),
                ("CINQUIEME", "MATH_5E_MES_VOLUME",      "Grandeurs et mesures", "Calculer le volume d'un prisme droit et d'un cylindre", 17),
                ("CINQUIEME", "MATH_5E_MES_CONVERSION",  "Grandeurs et mesures", "Convertir des aires et des volumes", 18),
                ("CINQUIEME", "MATH_5E_MES_DUREE",       "Grandeurs et mesures", "Calculer avec des durées", 19),

                // --- 5e : Espace et géométrie ---
                ("CINQUIEME", "MATH_5E_GEO_SYM_CENTRALE","Espace et géométrie", "Construire le symétrique par symétrie centrale", 20),
                ("CINQUIEME", "MATH_5E_GEO_ANGLES",      "Espace et géométrie", "Utiliser les angles alternes-internes et correspondants", 21),
                ("CINQUIEME", "MATH_5E_GEO_TRI_ANGLES",  "Espace et géométrie", "Utiliser la somme des angles d'un triangle", 22),
                ("CINQUIEME", "MATH_5E_GEO_TRI_INEG",    "Espace et géométrie", "Utiliser l'inégalité triangulaire", 23),
                ("CINQUIEME", "MATH_5E_GEO_PARALLELO",   "Espace et géométrie", "Connaître et utiliser les propriétés du parallélogramme", 24),
                ("CINQUIEME", "MATH_5E_GEO_SOLIDES",     "Espace et géométrie", "Représenter prismes, cylindres et leurs patrons", 25),
                ("CINQUIEME", "MATH_5E_ALGO_BOUCLE",     "Algorithmique",       "Écrire un algorithme avec boucles et variables", 26),

                // --- 4e : Nombres et calculs ---
                ("QUATRIEME", "MATH_4E_PUISSANCES",      "Nombres et calculs", "Utiliser les puissances d'un nombre", 1),
                ("QUATRIEME", "MATH_4E_PUISS_DIX",       "Nombres et calculs", "Utiliser les puissances de 10 et la notation scientifique", 2),
                ("QUATRIEME", "MATH_4E_FRAC_OPERATIONS", "Nombres et calculs", "Effectuer les quatre opérations sur les fractions", 3),
                ("QUATRIEME", "MATH_4E_REL_MULT",        "Nombres et calculs", "Multiplier et diviser des nombres relatifs", 4),
                ("QUATRIEME", "MATH_4E_LITT_DEVELOPPER", "Nombres et calculs", "Développer une expression avec la distributivité", 5),
                ("QUATRIEME", "MATH_4E_LITT_FACTORISER", "Nombres et calculs", "Factoriser une expression simple", 6),
                ("QUATRIEME", "MATH_4E_EQUATION",        "Nombres et calculs", "Résoudre une équation du premier degré", 7),
                ("QUATRIEME", "MATH_4E_PROB_EQUATION",   "Nombres et calculs", "Mettre un problème en équation", 8),

                // --- 4e : Proportionnalité, données, probabilités ---
                ("QUATRIEME", "MATH_4E_PROP_VITESSE",    "Gestion de données", "Calculer avec des grandeurs quotients : vitesse, débit", 9),
                ("QUATRIEME", "MATH_4E_PROP_POURCENT",   "Gestion de données", "Enchaîner et comparer des pourcentages", 10),
                ("QUATRIEME", "MATH_4E_STAT_MOY_POND",   "Gestion de données", "Calculer une moyenne pondérée", 11),
                ("QUATRIEME", "MATH_4E_STAT_MEDIANE",    "Gestion de données", "Déterminer une médiane et une étendue", 12),
                ("QUATRIEME", "MATH_4E_PROBA_CALCUL",    "Gestion de données", "Calculer une probabilité dans une situation simple", 13),

                // --- 4e : Grandeurs et mesures ---
                ("QUATRIEME", "MATH_4E_MES_VOLUME",      "Grandeurs et mesures", "Calculer le volume d'une pyramide et d'un cône", 14),
                ("QUATRIEME", "MATH_4E_MES_COMPOSEES",   "Grandeurs et mesures", "Calculer avec des grandeurs composées", 15),

                // --- 4e : Espace et géométrie ---
                ("QUATRIEME", "MATH_4E_PYTHAGORE",       "Espace et géométrie", "Utiliser le théorème de Pythagore", 16),
                ("QUATRIEME", "MATH_4E_PYTHAGORE_REC",   "Espace et géométrie", "Utiliser la réciproque de Pythagore", 17),
                ("QUATRIEME", "MATH_4E_MILIEUX",         "Espace et géométrie", "Utiliser le théorème des milieux", 18),
                ("QUATRIEME", "MATH_4E_THALES_INTRO",    "Espace et géométrie", "Utiliser la proportionnalité dans un triangle", 19),
                ("QUATRIEME", "MATH_4E_COSINUS",         "Espace et géométrie", "Utiliser le cosinus d'un angle aigu", 20),
                ("QUATRIEME", "MATH_4E_TRANSLATION",     "Espace et géométrie", "Construire l'image d'une figure par translation", 21),
                ("QUATRIEME", "MATH_4E_GEO_SOLIDES",     "Espace et géométrie", "Représenter pyramides et cônes, et leurs sections", 22),
                ("QUATRIEME", "MATH_4E_ALGO_CONDITION",  "Algorithmique",       "Écrire un algorithme avec conditions et boucles imbriquées", 23),

                // --- 3e : Nombres et calculs ---
                ("TROISIEME", "MATH_3E_ARITH_DIVISEURS", "Nombres et calculs", "Déterminer les diviseurs communs et le PGCD", 1),
                ("TROISIEME", "MATH_3E_ARITH_PREMIERS",  "Nombres et calculs", "Décomposer un nombre en facteurs premiers", 2),
                ("TROISIEME", "MATH_3E_FRAC_IRRED",      "Nombres et calculs", "Rendre une fraction irréductible", 3),
                ("TROISIEME", "MATH_3E_RACINE",          "Nombres et calculs", "Calculer avec des racines carrées", 4),
                ("TROISIEME", "MATH_3E_PUISSANCES",      "Nombres et calculs", "Maîtriser puissances et notation scientifique", 5),
                ("TROISIEME", "MATH_3E_LITT_REMARQUABLE","Nombres et calculs", "Développer avec les identités remarquables", 6),
                ("TROISIEME", "MATH_3E_LITT_FACTORISER", "Nombres et calculs", "Factoriser une expression littérale", 7),
                ("TROISIEME", "MATH_3E_EQ_PRODUIT",      "Nombres et calculs", "Résoudre une équation produit nul", 8),
                ("TROISIEME", "MATH_3E_EQ_PROBLEME",     "Nombres et calculs", "Résoudre un problème par mise en équation", 9),

                // --- 3e : Fonctions et données ---
                ("TROISIEME", "MATH_3E_FONC_NOTION",     "Gestion de données", "Comprendre la notion de fonction : image et antécédent", 10),
                ("TROISIEME", "MATH_3E_FONC_LINEAIRE",   "Gestion de données", "Utiliser une fonction linéaire", 11),
                ("TROISIEME", "MATH_3E_FONC_AFFINE",     "Gestion de données", "Utiliser une fonction affine", 12),
                ("TROISIEME", "MATH_3E_FONC_GRAPHIQUE",  "Gestion de données", "Lire et construire la représentation graphique d'une fonction", 13),
                ("TROISIEME", "MATH_3E_STAT_QUARTILES",  "Gestion de données", "Déterminer médiane, quartiles et étendue", 14),
                ("TROISIEME", "MATH_3E_PROBA_DEUX",      "Gestion de données", "Calculer une probabilité sur une expérience à deux épreuves", 15),

                // --- 3e : Grandeurs et mesures ---
                ("TROISIEME", "MATH_3E_AGRANDISSEMENT",  "Grandeurs et mesures", "Utiliser l'effet d'un agrandissement sur aires et volumes", 16),
                ("TROISIEME", "MATH_3E_MES_SPHERE",      "Grandeurs et mesures", "Calculer l'aire d'une sphère et le volume d'une boule", 17),
                ("TROISIEME", "MATH_3E_MES_COMPOSEES",   "Grandeurs et mesures", "Calculer et convertir des grandeurs composées", 18),

                // --- 3e : Espace et géométrie ---
                ("TROISIEME", "MATH_3E_THALES",          "Espace et géométrie", "Utiliser le théorème de Thalès", 19),
                ("TROISIEME", "MATH_3E_THALES_REC",      "Espace et géométrie", "Utiliser la réciproque de Thalès", 20),
                ("TROISIEME", "MATH_3E_TRIGO",           "Espace et géométrie", "Utiliser sinus, cosinus et tangente dans un triangle rectangle", 21),
                ("TROISIEME", "MATH_3E_ROTATION",        "Espace et géométrie", "Construire l'image d'une figure par rotation", 22),
                ("TROISIEME", "MATH_3E_HOMOTHETIE",      "Espace et géométrie", "Construire l'image d'une figure par homothétie", 23),
                ("TROISIEME", "MATH_3E_REPERAGE",        "Espace et géométrie", "Se repérer sur une sphère : latitude et longitude", 24),
                ("TROISIEME", "MATH_3E_ALGO_FONCTION",   "Algorithmique",       "Écrire un algorithme avec variables, boucles et fonctions", 25),

                // ============================================================
                // Lycée — Seconde, Première et Terminale (spécialité)
                // ============================================================

                // --- Seconde : Nombres et calculs ---
                ("SECONDE", "MATH_2DE_ENSEMBLES",        "Nombres et calculs", "Situer un nombre dans les ensembles N, Z, D, Q et R", 1),
                ("SECONDE", "MATH_2DE_INTERVALLES",      "Nombres et calculs", "Utiliser les intervalles et la valeur absolue", 2),
                ("SECONDE", "MATH_2DE_CALC_LITTERAL",    "Nombres et calculs", "Développer, factoriser et transformer une expression", 3),
                ("SECONDE", "MATH_2DE_EQUATIONS",        "Nombres et calculs", "Résoudre équations et inéquations du premier degré", 4),
                ("SECONDE", "MATH_2DE_SIGNE",            "Nombres et calculs", "Étudier le signe d'un produit ou d'un quotient", 5),
                ("SECONDE", "MATH_2DE_PUISSANCES",       "Nombres et calculs", "Calculer avec puissances et racines carrées", 6),
                ("SECONDE", "MATH_2DE_POURCENTAGES",     "Nombres et calculs", "Calculer avec taux d'évolution et coefficients multiplicateurs", 7),

                // --- Seconde : Fonctions ---
                ("SECONDE", "MATH_2DE_FONC_NOTION",      "Analyse", "Utiliser la notion de fonction : image, antécédent, ensemble de définition", 8),
                ("SECONDE", "MATH_2DE_FONC_VARIATION",   "Analyse", "Décrire les variations et les extremums d'une fonction", 9),
                ("SECONDE", "MATH_2DE_FONC_AFFINE",      "Analyse", "Étudier une fonction affine", 10),
                ("SECONDE", "MATH_2DE_FONC_CARRE",       "Analyse", "Étudier la fonction carré et la fonction inverse", 11),
                ("SECONDE", "MATH_2DE_FONC_GRAPHIQUE",   "Analyse", "Résoudre graphiquement une équation ou une inéquation", 12),

                // --- Seconde : Géométrie ---
                ("SECONDE", "MATH_2DE_REPERAGE",         "Géométrie", "Utiliser coordonnées, distance et milieu dans un repère", 13),
                ("SECONDE", "MATH_2DE_VECTEURS",         "Géométrie", "Utiliser les vecteurs : somme, coordonnées", 14),
                ("SECONDE", "MATH_2DE_COLINEARITE",      "Géométrie", "Utiliser la colinéarité de deux vecteurs", 15),
                ("SECONDE", "MATH_2DE_DROITES",          "Géométrie", "Déterminer et utiliser une équation de droite", 16),
                ("SECONDE", "MATH_2DE_TRIGO",            "Géométrie", "Utiliser la trigonométrie du triangle rectangle", 17),

                // --- Seconde : Statistiques et probabilités ---
                ("SECONDE", "MATH_2DE_STAT_INDICATEURS", "Statistiques et probabilités", "Calculer moyenne, médiane, quartiles et écart-type", 18),
                ("SECONDE", "MATH_2DE_PROBA_MODELE",     "Statistiques et probabilités", "Modéliser une expérience aléatoire", 19),
                ("SECONDE", "MATH_2DE_PROBA_EVENEMENTS", "Statistiques et probabilités", "Calculer avec réunion, intersection et événement contraire", 20),
                ("SECONDE", "MATH_2DE_ECHANTILLON",      "Statistiques et probabilités", "Interpréter la fluctuation d'échantillonnage", 21),
                ("SECONDE", "MATH_2DE_ALGO_PYTHON",      "Algorithmique", "Écrire un programme Python : variables, boucles, fonctions", 22),

                // --- Première : Algèbre ---
                ("PREMIERE", "MATH_1RE_SUITES_NOTION",   "Algèbre", "Définir une suite de façon explicite ou par récurrence", 1),
                ("PREMIERE", "MATH_1RE_SUITES_ARITH",    "Algèbre", "Reconnaître et utiliser une suite arithmétique", 2),
                ("PREMIERE", "MATH_1RE_SUITES_GEO",      "Algèbre", "Reconnaître et utiliser une suite géométrique", 3),
                ("PREMIERE", "MATH_1RE_TRINOME_FORME",   "Algèbre", "Utiliser les formes développée, canonique et factorisée", 4),
                ("PREMIERE", "MATH_1RE_TRINOME_RACINES", "Algèbre", "Résoudre une équation du second degré", 5),
                ("PREMIERE", "MATH_1RE_TRINOME_SIGNE",   "Algèbre", "Étudier le signe d'un trinôme et résoudre une inéquation", 6),

                // --- Première : Analyse ---
                ("PREMIERE", "MATH_1RE_DERIV_NOMBRE",    "Analyse", "Calculer un nombre dérivé et une équation de tangente", 7),
                ("PREMIERE", "MATH_1RE_DERIV_FONCTION",  "Analyse", "Dériver les fonctions usuelles et leurs opérations", 8),
                ("PREMIERE", "MATH_1RE_DERIV_VARIATION", "Analyse", "Étudier les variations d'une fonction par sa dérivée", 9),
                ("PREMIERE", "MATH_1RE_EXP",             "Analyse", "Utiliser la fonction exponentielle et ses propriétés", 10),
                ("PREMIERE", "MATH_1RE_TRIGO_CERCLE",    "Analyse", "Utiliser le cercle trigonométrique et le radian", 11),
                ("PREMIERE", "MATH_1RE_TRIGO_FONCTIONS", "Analyse", "Étudier les fonctions cosinus et sinus", 12),

                // --- Première : Géométrie ---
                ("PREMIERE", "MATH_1RE_SCAL_DEFINITION", "Géométrie", "Calculer un produit scalaire sous ses différentes formes", 13),
                ("PREMIERE", "MATH_1RE_SCAL_ORTHOGONAL", "Géométrie", "Caractériser l'orthogonalité par le produit scalaire", 14),
                ("PREMIERE", "MATH_1RE_SCAL_APPLICATION","Géométrie", "Calculer longueurs et angles par le produit scalaire", 15),
                ("PREMIERE", "MATH_1RE_CERCLE_EQUATION", "Géométrie", "Déterminer une équation de cercle", 16),

                // --- Première : Probabilités et statistiques ---
                ("PREMIERE", "MATH_1RE_PROBA_COND",      "Statistiques et probabilités", "Calculer une probabilité conditionnelle", 17),
                ("PREMIERE", "MATH_1RE_PROBA_ARBRE",     "Statistiques et probabilités", "Utiliser un arbre pondéré et la formule des probabilités totales", 18),
                ("PREMIERE", "MATH_1RE_PROBA_INDEP",     "Statistiques et probabilités", "Reconnaître l'indépendance de deux événements", 19),
                ("PREMIERE", "MATH_1RE_VA_LOI",          "Statistiques et probabilités", "Déterminer la loi d'une variable aléatoire", 20),
                ("PREMIERE", "MATH_1RE_VA_ESPERANCE",    "Statistiques et probabilités", "Calculer espérance, variance et écart-type", 21),
                ("PREMIERE", "MATH_1RE_ALGO_SEUIL",      "Algorithmique", "Écrire un algorithme de seuil sur une suite", 22),

                // --- Terminale : Analyse ---
                ("TERMINALE", "MATH_TLE_RECURRENCE",     "Analyse", "Démontrer par récurrence", 1),
                ("TERMINALE", "MATH_TLE_SUITES_LIMITE",  "Analyse", "Déterminer la limite d'une suite", 2),
                ("TERMINALE", "MATH_TLE_SUITES_CONV",    "Analyse", "Utiliser les théorèmes de convergence et de comparaison", 3),
                ("TERMINALE", "MATH_TLE_FONC_LIMITE",    "Analyse", "Déterminer la limite d'une fonction et ses asymptotes", 4),
                ("TERMINALE", "MATH_TLE_CONTINUITE",     "Analyse", "Utiliser la continuité et le théorème des valeurs intermédiaires", 5),
                ("TERMINALE", "MATH_TLE_DERIV_COMPOSEE", "Analyse", "Dériver une fonction composée", 6),
                ("TERMINALE", "MATH_TLE_LOG",            "Analyse", "Utiliser la fonction logarithme népérien", 7),
                ("TERMINALE", "MATH_TLE_CONVEXITE",      "Analyse", "Étudier la convexité d'une fonction", 8),
                ("TERMINALE", "MATH_TLE_PRIMITIVES",     "Analyse", "Déterminer les primitives d'une fonction", 9),
                ("TERMINALE", "MATH_TLE_INTEGRALE",      "Analyse", "Calculer une intégrale et une aire", 10),
                ("TERMINALE", "MATH_TLE_EQ_DIFF",        "Analyse", "Résoudre une équation différentielle du premier ordre", 11),

                // --- Terminale : Géométrie dans l'espace ---
                ("TERMINALE", "MATH_TLE_VECT_ESPACE",    "Géométrie", "Utiliser les vecteurs de l'espace et la colinéarité", 12),
                ("TERMINALE", "MATH_TLE_DROITES_PLANS",  "Géométrie", "Étudier les positions relatives de droites et de plans", 13),
                ("TERMINALE", "MATH_TLE_SCAL_ESPACE",    "Géométrie", "Utiliser le produit scalaire dans l'espace", 14),
                ("TERMINALE", "MATH_TLE_PARAMETRIQUE",   "Géométrie", "Déterminer une représentation paramétrique de droite", 15),
                ("TERMINALE", "MATH_TLE_PLAN_CARTESIEN", "Géométrie", "Déterminer une équation cartésienne de plan", 16),
                ("TERMINALE", "MATH_TLE_ORTHOGONALITE",  "Géométrie", "Résoudre un problème d'orthogonalité et de distance", 17),

                // --- Terminale : Probabilités ---
                ("TERMINALE", "MATH_TLE_BINOMIALE",      "Statistiques et probabilités", "Utiliser la loi binomiale et les coefficients binomiaux", 18),
                ("TERMINALE", "MATH_TLE_VA_SOMME",       "Statistiques et probabilités", "Calculer espérance et variance d'une somme de variables", 19),
                ("TERMINALE", "MATH_TLE_CONCENTRATION",  "Statistiques et probabilités", "Utiliser l'inégalité de concentration et la loi des grands nombres", 20),
                ("TERMINALE", "MATH_TLE_ALGO_SIMULATION","Algorithmique", "Écrire un programme de simulation ou de calcul approché", 21),
            };

        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // Chaînage interne CP : tout part du complément à 10, qui
                // conditionne le franchissement de la dizaine.
                ("MATH_CP_NUM_DECOMPOSER",    "MATH_CP_NUM_100",            3),
                ("MATH_CP_CALC_COMPLEMENT",   "MATH_CP_CALC_ADD_10",        2),
                ("MATH_CP_CALC_ADD_DIZAINE",  "MATH_CP_CALC_ADD_10",        3),
                ("MATH_CP_CALC_ADD_DIZAINE",  "MATH_CP_CALC_COMPLEMENT",    3),
                ("MATH_CP_CALC_SOUS",         "MATH_CP_CALC_ADD_10",        2),
                ("MATH_CP_CALC_DOUBLE",       "MATH_CP_CALC_ADD_10",        2),
                ("MATH_CP_PROB_UNE_ETAPE",    "MATH_CP_CALC_ADD_10",        3),

                // CP → CE1
                ("MATH_CE1_NUM_1000",         "MATH_CP_NUM_100",            3),
                ("MATH_CE1_NUM_DECOMPOSER",   "MATH_CP_NUM_DECOMPOSER",     3),
                ("MATH_CE1_CALC_ADD_POSEE",   "MATH_CP_CALC_ADD_DIZAINE",   3),
                ("MATH_CE1_CALC_ADD_POSEE",   "MATH_CP_NUM_DECOMPOSER",     2),
                ("MATH_CE1_CALC_SOUS_POSEE",  "MATH_CP_CALC_SOUS",          3),
                ("MATH_CE1_MULT_SENS",        "MATH_CP_CALC_ADD_10",        2),
                ("MATH_CE1_PROB_DEUX_ETAPES", "MATH_CP_PROB_UNE_ETAPE",     3),
                ("MATH_CE1_MES_LONGUEUR",     "MATH_CP_MES_LONGUEUR",       3),
                ("MATH_CE1_MES_HEURE",        "MATH_CP_MES_TEMPS",          3),
                ("MATH_CE1_GEO_PROPRIETES",   "MATH_CP_GEO_FIGURES",        3),
                ("MATH_CE1_TABLES_BASE",      "MATH_CE1_MULT_SENS",         3),
                ("MATH_CE1_MES_MONNAIE",      "MATH_CE1_CALC_ADD_POSEE",    2),

                // CE1 → CE2
                ("MATH_CE2_NUM_10000",        "MATH_CE1_NUM_1000",          3),
                ("MATH_CE2_TABLES",           "MATH_CE1_TABLES_BASE",       3),
                ("MATH_CE2_MULT_POSEE",       "MATH_CE2_TABLES",            3),
                ("MATH_CE2_MULT_POSEE",       "MATH_CE1_CALC_ADD_POSEE",    2),
                ("MATH_CE2_DIV_SENS",         "MATH_CE1_MULT_SENS",         3),
                ("MATH_CE2_PROB_MULT",        "MATH_CE1_PROB_DEUX_ETAPES",  3),
                ("MATH_CE2_PROB_MULT",        "MATH_CE2_TABLES",            2),
                ("MATH_CE2_MES_CONVERSION",   "MATH_CE1_MES_LONGUEUR",      3),
                ("MATH_CE2_MES_DUREE",        "MATH_CE1_MES_HEURE",         3),
                ("MATH_CE2_MES_PERIMETRE",    "MATH_CE2_MES_CONVERSION",    2),
                ("MATH_CE2_GEO_ANGLE_DROIT",  "MATH_CE1_GEO_PROPRIETES",    2),
                ("MATH_CE2_GEO_SYMETRIE",     "MATH_CE1_GEO_ALIGNEMENT",    2),

                // CE2 → CM1
                ("MATH_CM1_NUM_MILLIONS",     "MATH_CE2_NUM_10000",         3),
                ("MATH_CM1_FRAC_SENS",        "MATH_CE2_FRAC_DECOUVERTE",   3),
                ("MATH_CM1_NUM_DECIMAUX",     "MATH_CM1_FRAC_SENS",         2),
                ("MATH_CM1_MULT_2CHIFFRES",   "MATH_CE2_MULT_POSEE",        3),
                ("MATH_CM1_DIV_POSEE",        "MATH_CE2_DIV_SENS",          3),
                ("MATH_CM1_DIV_POSEE",        "MATH_CE2_TABLES",            3),
                ("MATH_CM1_CALC_MENTAL",      "MATH_CE2_TABLES",            2),
                ("MATH_CM1_PROB_ETAPES",      "MATH_CE2_PROB_MULT",         3),
                ("MATH_CM1_MES_CONVERSION",   "MATH_CE2_MES_CONVERSION",    3),
                ("MATH_CM1_MES_AIRE",         "MATH_CE2_MES_PERIMETRE",     2),
                ("MATH_CM1_MES_DUREE",        "MATH_CE2_MES_DUREE",         3),
                ("MATH_CM1_GEO_PERPENDICULAIRE","MATH_CE2_GEO_ANGLE_DROIT", 3),
                ("MATH_CM1_GEO_SYMETRIE",     "MATH_CE2_GEO_SYMETRIE",      3),

                // CM1 → CM2 : la jonction qui manquait pour que le graphe
                // descende jusqu'au CP d'un seul tenant.
                ("MATH_CM2_NUM_ENTIERS",      "MATH_CM1_NUM_MILLIONS",      3),
                ("MATH_CM2_NUM_DECIMAUX",     "MATH_CM1_NUM_DECIMAUX",      3),
                ("MATH_CM2_CALC_ADD_SOUS",    "MATH_CE1_CALC_ADD_POSEE",    2),
                ("MATH_CM2_CALC_MULT",        "MATH_CM1_MULT_2CHIFFRES",    3),
                ("MATH_CM2_CALC_DIV",         "MATH_CM1_DIV_POSEE",         3),
                ("MATH_CM2_FRAC_INTRO",       "MATH_CM1_FRAC_SENS",         3),
                ("MATH_CM2_MES_LONGUEUR",     "MATH_CM1_MES_CONVERSION",    3),
                ("MATH_CM2_MES_AIRE",         "MATH_CM1_MES_AIRE",          3),
                ("MATH_CM2_GEO_FIGURES",      "MATH_CE1_GEO_PROPRIETES",    2),
                ("MATH_CM2_GEO_PERPENDICULAIRE","MATH_CM1_GEO_PERPENDICULAIRE",3),
                ("MATH_CM2_GEO_SYMETRIE",     "MATH_CM1_GEO_SYMETRIE",      3),
                ("MATH_CM2_DATA_TABLEAU",     "MATH_CM1_DATA_TABLEAU",      3),

                // Continuité verticale CM2 → 6e : le cœur du diagnostic remontant
                ("MATH_6E_NUM_ENTIERS",       "MATH_CM2_NUM_ENTIERS",       3),
                ("MATH_6E_NUM_DECIMAUX",      "MATH_CM2_NUM_DECIMAUX",      3),
                ("MATH_6E_CALC_ADD",          "MATH_CM2_CALC_ADD_SOUS",     3),
                ("MATH_6E_CALC_MULT",         "MATH_CM2_CALC_MULT",         3),
                ("MATH_6E_CALC_DIV_EUCL",     "MATH_CM2_CALC_DIV",          3),
                ("MATH_6E_FRAC_SENS",         "MATH_CM2_FRAC_INTRO",        3),
                ("MATH_6E_MES_LONGUEUR",      "MATH_CM2_MES_LONGUEUR",      3),
                ("MATH_6E_MES_AIRE",          "MATH_CM2_MES_AIRE",          3),
                ("MATH_6E_GEO_PERPENDICULAIRE","MATH_CM2_GEO_PERPENDICULAIRE",3),
                ("MATH_6E_GEO_SYMETRIE",      "MATH_CM2_GEO_SYMETRIE",      3),
                ("MATH_6E_DATA_TABLEAU",      "MATH_CM2_DATA_TABLEAU",      3),
                ("MATH_6E_GEO_TRIANGLES",     "MATH_CM2_GEO_FIGURES",       2),
                ("MATH_6E_GEO_QUADRILATERES", "MATH_CM2_GEO_FIGURES",       2),

                // Nombres et calculs, chaînage interne 6e
                ("MATH_6E_NUM_DEMI_DROITE",   "MATH_6E_NUM_DECIMAUX",       2),
                ("MATH_6E_NUM_ARRONDI",       "MATH_6E_NUM_DECIMAUX",       3),
                ("MATH_6E_CALC_ADD",          "MATH_6E_NUM_DECIMAUX",       3),
                ("MATH_6E_CALC_MULT",         "MATH_6E_NUM_DECIMAUX",       3),
                ("MATH_6E_CALC_DIV_DEC",      "MATH_6E_CALC_DIV_EUCL",      3),
                ("MATH_6E_CALC_DIV_DEC",      "MATH_6E_NUM_DECIMAUX",       2),
                ("MATH_6E_CALC_PRIORITES",    "MATH_6E_CALC_ADD",           3),
                ("MATH_6E_CALC_PRIORITES",    "MATH_6E_CALC_MULT",          3),
                ("MATH_6E_CALC_MENTAL",       "MATH_6E_CALC_ADD",           2),
                ("MATH_6E_CALC_MENTAL",       "MATH_6E_CALC_MULT",          2),
                ("MATH_6E_DIVISIBILITE",      "MATH_6E_CALC_DIV_EUCL",      3),

                // Fractions
                ("MATH_6E_FRAC_REPERER",      "MATH_6E_FRAC_SENS",          3),
                ("MATH_6E_FRAC_REPERER",      "MATH_6E_NUM_DEMI_DROITE",    2),
                ("MATH_6E_FRAC_EGALES",       "MATH_6E_FRAC_SENS",          3),
                ("MATH_6E_FRAC_EGALES",       "MATH_6E_CALC_MULT",          2),
                ("MATH_6E_FRAC_COMPARER",     "MATH_6E_FRAC_EGALES",        3),
                ("MATH_6E_FRAC_ADD",          "MATH_6E_FRAC_SENS",          3),
                ("MATH_6E_FRAC_QUANTITE",     "MATH_6E_FRAC_SENS",          3),
                ("MATH_6E_FRAC_QUANTITE",     "MATH_6E_CALC_MULT",          2),

                // Proportionnalité
                ("MATH_6E_PROP_RECONNAITRE",  "MATH_6E_CALC_MULT",          2),
                ("MATH_6E_PROP_RECONNAITRE",  "MATH_6E_DATA_TABLEAU",       2),
                ("MATH_6E_PROP_COEFF",        "MATH_6E_PROP_RECONNAITRE",   3),
                ("MATH_6E_PROP_COEFF",        "MATH_6E_CALC_DIV_DEC",       2),
                ("MATH_6E_PROP_POURCENT",     "MATH_6E_FRAC_QUANTITE",      3),
                ("MATH_6E_PROP_POURCENT",     "MATH_6E_PROP_COEFF",         2),
                ("MATH_6E_PROP_ECHELLE",      "MATH_6E_PROP_COEFF",         3),
                ("MATH_6E_PROP_ECHELLE",      "MATH_6E_MES_LONGUEUR",       2),
                ("MATH_6E_DATA_GRAPHIQUE",    "MATH_6E_DATA_TABLEAU",       3),

                // Grandeurs et mesures
                ("MATH_6E_MES_PERIMETRE",     "MATH_6E_MES_LONGUEUR",       3),
                ("MATH_6E_MES_PERIMETRE",     "MATH_6E_CALC_ADD",           2),
                ("MATH_6E_MES_AIRE",          "MATH_6E_CALC_MULT",          3),
                ("MATH_6E_MES_VOLUME",        "MATH_6E_MES_AIRE",           3),
                ("MATH_6E_MES_ANGLE",         "MATH_6E_GEO_VOCABULAIRE",    2),

                // Géométrie
                ("MATH_6E_GEO_PARALLELE",     "MATH_6E_GEO_PERPENDICULAIRE",2),
                ("MATH_6E_GEO_CERCLE",        "MATH_6E_GEO_VOCABULAIRE",    2),
                ("MATH_6E_GEO_TRIANGLES",     "MATH_6E_MES_ANGLE",          2),
                ("MATH_6E_GEO_QUADRILATERES", "MATH_6E_GEO_PARALLELE",      3),
                ("MATH_6E_GEO_SYMETRIE",      "MATH_6E_GEO_PERPENDICULAIRE",2),
                ("MATH_6E_GEO_CONSTRUCTION",  "MATH_6E_GEO_PERPENDICULAIRE",2),
                ("MATH_6E_GEO_CONSTRUCTION",  "MATH_6E_GEO_CERCLE",         2),
                ("MATH_6E_GEO_SOLIDES",       "MATH_6E_MES_VOLUME",         2),
                ("MATH_6E_ALGO_DEPLACEMENT",  "MATH_6E_GEO_VOCABULAIRE",    1),
            };
    }
}
