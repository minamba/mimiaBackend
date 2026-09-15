namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel de mathématiques, du CP à la terminale.
    ///
    /// Un libellé décrit un GESTE vérifiable : « Additionner deux fractions de
    /// même dénominateur », pas « les fractions ».
    ///
    /// PROVENANCE — vérifiée le 5 septembre 2026 contre les textes officiels.
    /// Voir <see cref="ReferentielSeeder"/> pour le tableau complet des sources
    /// et des dates d'entrée en vigueur, toutes matières confondues.
    ///
    ///   CP, CE1, CE2   arrêté du 22/10/2024, BO n° 41 du 31/10/2024,
    ///                  annexe 4 — en vigueur depuis la rentrée 2025.
    ///   CM1, CM2, 6e   arrêté publié au BO n° 16 du 17/04/2025 — CM1 et 6e
    ///                  depuis la rentrée 2025, CM2 depuis la rentrée 2026.
    ///   5e, 4e, 3e     arrêté publié au BO n° 10 de 2026 — 5e depuis la
    ///                  rentrée 2026, 4e à la rentrée 2027, 3e à la rentrée 2028.
    ///                  Les niveaux 4e et 3e suivent donc ENCORE le programme
    ///                  précédent cette année.
    ///   Première      trois arrêtés du 26/02/2026, JO du 27/03/2026, BO n° 14
    ///                 du 02/04/2026. Art. 2 de chacun : « Les dispositions du
    ///                 présent arrêté entrent en application à la rentrée
    ///                 scolaire 2026-2027. » Textes et annexes relus le
    ///                 14/09/2026, lignes PREMIERE et PREMIERE_TECHNO réécrites
    ///                 d'après eux ce jour-là.
    ///     1re générale, spécialité     NOR MENE2602917A — remplace l'annexe
    ///                                  de l'arrêté du 17/01/2019.
    ///     1re générale, maths de       NOR MENE2602916A — remplace l'annexe
    ///     l'enseignement scientifique  de l'arrêté du 06/07/2022. Seul cours
    ///                                  de maths des élèves sans la spécialité
    ///                                  (lignes du domaine « Enseignement
    ///                                  scientifique »).
    ///     1re technologique            NOR MENE2602918A — remplace l'annexe
    ///                                  de l'arrêté du 17/01/2019 modifié.
    ///   Terminale     ce qui reste de 2019, en vigueur en 2026-2027 pour la
    ///                 dernière année : spécialité, arrêté du 19/07/2019, NOR
    ///                 MENE1921246A, BO spécial n° 8 du 25/07/2019 ; voie
    ///                 technologique, NOR MENE1921242A, même BO. Leurs
    ///                 remplaçants (arrêtés du 26/02/2026, MENE2602919A et
    ///                 MENE2602921A) entrent en application à la rentrée
    ///                 2027-2028 : il faudra repasser ici.
    ///   Seconde       À VÉRIFIER. Un nouveau programme (arrêté du 26/02/2026,
    ///                 NOR MENE2602914A, même BO n° 14) est en application à la
    ///                 rentrée 2026-2027. Les lignes SECONDE ci-dessous n'ont
    ///                 PAS été confrontées à ce texte.
    ///
    /// Les codes des lignes réécrites sont gardés quand la notion est la même,
    /// même si le libellé change : les maîtrises des élèves y sont rattachées.
    ///
    /// La première version de ce fichier portait la mention « Le contenu vient
    /// d'Éduscol » sans référence, sans date, et sans que personne ait ouvert
    /// un texte. Elle était fausse par omission : le référentiel décrivait les
    /// programmes d'avant 2025. Une provenance qu'on ne peut pas vérifier vaut
    /// moins que pas de provenance du tout.
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

                ("CP", "MATH_CP_NUM_ORDINAUX", "Nombres et calculs", "Utiliser les nombres ordinaux jusqu’à vingtième", 13),
                ("CP", "MATH_CP_NUM_DEMI_DROITE", "Nombres et calculs", "Placer un nombre sur une demi-droite graduée", 14),
                ("CP", "MATH_CP_NUM_SIGNES", "Nombres et calculs", "Comparer et encadrer avec les signes =, < et >", 15),
                ("CP", "MATH_CP_CALC_ADD_POSEE", "Nombres et calculs", "Poser et effectuer une addition en colonnes", 16),
                ("CP", "MATH_CP_CALC_DIZAINES", "Nombres et calculs", "Ajouter ou soustraire 10, puis un nombre entier de dizaines", 17),
                ("CP", "MATH_CP_MULT_SENS", "Nombres et calculs", "Comprendre le sens de la multiplication", 18),
                ("CP", "MATH_CP_PROB_TYPES", "Nombres et calculs", "Reconnaître un problème de parties-tout et un problème de comparaison", 19),
                ("CP", "MATH_CP_PROB_DEUX_ETAPES", "Nombres et calculs", "Résoudre un problème additif en deux étapes", 20),
                ("CP", "MATH_CP_PROB_MULT", "Nombres et calculs", "Résoudre un problème multiplicatif en une étape", 21),
                ("CP", "MATH_CP_MES_MASSE", "Grandeurs et mesures", "Comparer et mesurer des masses", 22),
                ("CP", "MATH_CP_MES_MONNAIE", "Grandeurs et mesures", "Reconnaître les pièces et les billets et payer une somme", 23),
                ("CP", "MATH_CP_GEO_SOLIDES", "Espace et géométrie", "Reconnaître et décrire les solides usuels", 24),
                ("CP", "MATH_CP_DATA_TABLEAU", "Gestion de données", "Lire et compléter un tableau ou un diagramme en barres", 25),

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

                ("CE1", "MATH_CE1_FRAC_SENS", "Nombres et calculs", "Comprendre, lire et écrire une fraction d’un tout", 14),
                ("CE1", "MATH_CE1_FRAC_COMPARER", "Nombres et calculs", "Comparer des fractions de même dénominateur", 15),
                ("CE1", "MATH_CE1_FRAC_ADD", "Nombres et calculs", "Additionner et soustraire des fractions de même dénominateur", 16),
                ("CE1", "MATH_CE1_MULT_COMMUTATIVE", "Nombres et calculs", "Savoir que la multiplication est commutative", 17),
                ("CE1", "MATH_CE1_MULT_DIX", "Nombres et calculs", "Multiplier un nombre par 10", 18),
                ("CE1", "MATH_CE1_PROB_COMPARAISON", "Nombres et calculs", "Résoudre un problème additif de comparaison", 19),
                ("CE1", "MATH_CE1_PROB_MULT", "Nombres et calculs", "Résoudre un problème multiplicatif en une étape", 20),
                ("CE1", "MATH_CE1_PROB_MIXTE", "Nombres et calculs", "Résoudre un problème mixte en deux étapes", 21),
                ("CE1", "MATH_CE1_GEO_SOLIDES", "Espace et géométrie", "Reconnaître et décrire les solides usuels", 22),
                ("CE1", "MATH_CE1_DATA_TABLEAU", "Gestion de données", "Recueillir des données et les présenter dans un tableau", 23),

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

                ("CE2", "MATH_CE2_FRAC_SUP_UN", "Nombres et calculs", "Lire et écrire des fractions, y compris supérieures à 1", 12),
                ("CE2", "MATH_CE2_FRAC_ADD", "Nombres et calculs", "Additionner et soustraire des fractions", 13),
                ("CE2", "MATH_CE2_FRAC_EGALITES", "Nombres et calculs", "Établir des égalités entre fractions simples", 14),
                ("CE2", "MATH_CE2_FRAC_LONGUEUR", "Nombres et calculs", "Mesurer une longueur avec des fractions d’unité", 15),
                ("CE2", "MATH_CE2_MULT_DIX_CENT", "Nombres et calculs", "Multiplier un nombre entier par 10 ou par 100", 16),
                ("CE2", "MATH_CE2_PROB_CARTESIEN", "Nombres et calculs", "Résoudre un problème de produit cartésien", 17),
                ("CE2", "MATH_CE2_PROB_COMP_MULT", "Nombres et calculs", "Résoudre un problème de comparaison multiplicative", 18),
                ("CE2", "MATH_CE2_PROB_MIXTE", "Nombres et calculs", "Résoudre un problème mixte en deux ou trois étapes", 19),
                ("CE2", "MATH_CE2_MES_MASSE", "Grandeurs et mesures", "Utiliser le gramme et le kilogramme", 20),
                ("CE2", "MATH_CE2_MES_CONTENANCE", "Grandeurs et mesures", "Comparer et mesurer des contenances", 21),
                ("CE2", "MATH_CE2_MES_MONNAIE", "Grandeurs et mesures", "Lire un prix à virgule : euros et centimes", 22),
                ("CE2", "MATH_CE2_DATA_DIAGRAMME", "Gestion de données", "Lire et construire un tableau ou un diagramme en barres", 23),

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

                ("CM1", "MATH_CM1_NUM_MULTIPLES", "Nombres et calculs", "Reconnaître les multiples de 2, de 5 et de 10", 14),
                ("CM1", "MATH_CM1_FRAC_SUP_UN", "Nombres et calculs", "Écrire une fraction supérieure à 1 comme un entier et une fraction", 15),
                ("CM1", "MATH_CM1_FRAC_OPERATEUR", "Nombres et calculs", "Calculer une fraction d’une quantité", 16),
                ("CM1", "MATH_CM1_DEC_FRACTIONS", "Nombres et calculs", "Passer d’une fraction décimale à une écriture à virgule", 17),
                ("CM1", "MATH_CM1_ALG_INCONNUE", "Algèbre", "Représenter un nombre inconnu par un symbole", 18),
                ("CM1", "MATH_CM1_ALG_MOTIFS", "Algèbre", "Poursuivre et généraliser une suite de motifs ou de nombres", 19),
                ("CM1", "MATH_CM1_PROP_IDENTIFIER", "Gestion de données", "Reconnaître une situation de proportionnalité", 20),
                ("CM1", "MATH_CM1_PROP_RESOUDRE", "Gestion de données", "Résoudre un problème de proportionnalité", 21),
                ("CM1", "MATH_CM1_PROBA_ALEATOIRE", "Gestion de données", "Reconnaître une expérience aléatoire et ses issues possibles", 22),
                ("CM1", "MATH_CM1_PROBA_VOCABULAIRE", "Gestion de données", "Employer impossible, possible, certain, probable", 23),
                ("CM1", "MATH_CM1_MES_CONTENANCE", "Grandeurs et mesures", "Utiliser les unités de contenance, du millilitre à l’hectolitre", 24),
                ("CM1", "MATH_CM1_MES_ANGLE", "Grandeurs et mesures", "Comparer des angles et employer leur vocabulaire", 25),
                ("CM1", "MATH_CM1_ALGO_DEPLACEMENT", "Algorithmique", "Exécuter et écrire un programme de déplacement simple", 26),

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

                ("CM2", "MATH_CM2_ALG_INCONNUE", "Algèbre", "Résoudre un problème mettant en jeu un nombre inconnu", 13),
                ("CM2", "MATH_CM2_ALG_SCHEMA", "Algèbre", "Modéliser un problème par un schéma en barres", 14),
                ("CM2", "MATH_CM2_ALG_EGALITE", "Algèbre", "Comprendre le signe = comme une relation entre deux quantités", 15),
                ("CM2", "MATH_CM2_FRAC_DECIMALES", "Nombres et calculs", "Utiliser les fractions décimales", 16),
                ("CM2", "MATH_CM2_PROP_RESOUDRE", "Gestion de données", "Reconnaître et traiter une situation de proportionnalité", 17),
                ("CM2", "MATH_CM2_PROBA_COMPARER", "Gestion de données", "Comparer la probabilité de deux événements", 18),
                ("CM2", "MATH_CM2_PROBA_EQUI", "Gestion de données", "Reconnaître une situation d’équiprobabilité", 19),
                ("CM2", "MATH_CM2_DATA_DIAGRAMME", "Gestion de données", "Construire un diagramme à partir de données", 20),
                ("CM2", "MATH_CM2_MES_ANGLE", "Grandeurs et mesures", "Comparer et mesurer des angles", 21),
                ("CM2", "MATH_CM2_MES_DUREE", "Grandeurs et mesures", "Résoudre un problème impliquant des durées", 22),
                ("CM2", "MATH_CM2_GEO_DEPLACEMENT", "Espace et géométrie", "Coder et décrire un déplacement dans l’espace", 23),
                ("CM2", "MATH_CM2_ALGO_PROGRAMME", "Algorithmique", "Écrire un programme simple comportant une répétition", 24),

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

                ("SIXIEME", "MATH_6E_ALG_INCONNUE", "Algèbre", "Résoudre un problème en raisonnant sur un nombre inconnu", 41),
                ("SIXIEME", "MATH_6E_ALG_MOTIFS", "Algèbre", "Généraliser une suite de motifs et exprimer la relation", 42),
                ("SIXIEME", "MATH_6E_FRAC_QUOTIENT", "Nombres et calculs", "Comprendre la fraction comme un quotient", 43),
                ("SIXIEME", "MATH_6E_MES_DISTANCE", "Grandeurs et mesures", "Comprendre et utiliser la notion de distance", 44),

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

                ("CINQUIEME", "MATH_5E_PUISSANCES", "Nombres et calculs", "Découvrir les puissances d’un nombre", 27),
                ("CINQUIEME", "MATH_5E_FONC_NOTION", "Gestion de données", "Découvrir la notion de fonction", 28),
                ("CINQUIEME", "MATH_5E_GEO_REPERAGE", "Espace et géométrie", "Se repérer dans le plan avec des coordonnées", 29),

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

                ("TROISIEME", "MATH_3E_VECTEURS", "Espace et géométrie", "Utiliser un vecteur pour décrire une translation", 26),

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

                ("SECONDE", "MATH_2DE_LOGIQUE_ENSEMBLES", "Nombres et calculs", "Employer le vocabulaire des ensembles : appartenance, inclusion, réunion, intersection", 23),
                ("SECONDE", "MATH_2DE_LOGIQUE_RAISONNER", "Nombres et calculs", "Distinguer implication et équivalence, et réfuter par un contre-exemple", 24),

                // --- Première générale, SPÉCIALITÉ — arrêté du 26/02/2026, NOR MENE2602917A ---
                // Reprise des « Capacités attendues » de l'annexe, partie par
                // partie. Les « Démonstrations », « Exemples d'algorithme » et
                // « Approfondissements possibles » ne sont pas des attendus.
                // Codes anciens gardés quand la notion est la même ; retiré :
                // MATH_1RE_TRIGO_FONCTIONS (l'étude des fonctions sinus et
                // cosinus relève du programme de terminale, voir plus bas).

                // --- Première spécialité : vocabulaire ensembliste et logique ---
                ("PREMIERE", "MATH_1RE_ENS_NOTATIONS",          "Vocabulaire ensembliste et logique", "Utiliser appartenance, inclusion, réunion, intersection, complémentaire et cardinal d'un ensemble fini", 1),
                ("PREMIERE", "MATH_1RE_ENS_PRODUIT",            "Vocabulaire ensembliste et logique", "Reconnaître un couple et le produit cartésien de deux ensembles", 2),
                ("PREMIERE", "MATH_1RE_LOG_CONNECTEURS",        "Vocabulaire ensembliste et logique", "Lire et écrire une proposition contenant les connecteurs « et », « ou »", 3),
                ("PREMIERE", "MATH_1RE_LOG_CONTRE_EXEMPLE",     "Vocabulaire ensembliste et logique", "Montrer qu'une proposition est fausse à l'aide d'un contre-exemple", 4),
                ("PREMIERE", "MATH_1RE_LOG_IMPLICATION",        "Vocabulaire ensembliste et logique", "Formuler une implication, sa réciproque, sa contraposée et une équivalence", 5),
                ("PREMIERE", "MATH_1RE_LOG_NECESSAIRE",         "Vocabulaire ensembliste et logique", "Employer à bon escient « condition nécessaire » et « condition suffisante »", 6),
                ("PREMIERE", "MATH_1RE_LOG_STATUT",             "Vocabulaire ensembliste et logique", "Distinguer identité et équation, et le statut d'une lettre : variable, inconnue, paramètre", 7),
                ("PREMIERE", "MATH_1RE_LOGIQUE_QUANTIF",        "Vocabulaire ensembliste et logique", "Repérer les quantificateurs, même implicites, et formuler la négation d'une proposition quantifiée", 8),
                ("PREMIERE", "MATH_1RE_LOGIQUE_CONTRAPOSEE",    "Vocabulaire ensembliste et logique", "Raisonner par disjonction des cas, par l'absurde ou par contraposée", 9),

                // --- Première spécialité : algorithmique et programmation (notion de liste) ---
                ("PREMIERE", "MATH_1RE_ALGO_LISTE_GENERER",     "Algorithmique et programmation", "Générer une liste en extension, par ajouts successifs ou en compréhension", 10),
                ("PREMIERE", "MATH_1RE_ALGO_LISTE_MANIPULER",   "Algorithmique et programmation", "Ajouter ou supprimer des éléments d'une liste et utiliser leurs indices", 11),
                ("PREMIERE", "MATH_1RE_ALGO_LISTE_PARCOURIR",   "Algorithmique et programmation", "Parcourir une liste et itérer sur ses éléments", 12),

                // --- Première spécialité : automatismes (QCM de l'épreuve anticipée) ---
                // Mot pour mot les mêmes que dans les deux autres programmes de
                // première de 2026. Les lignes « Enseignement scientifique » ont
                // leurs propres codes, pour les élèves sans la spécialité.
                ("PREMIERE", "MATH_1RE_AUTO_TAUX_APPLIQUER",    "Automatismes", "Appliquer un taux d'évolution pour calculer une valeur finale ou initiale", 13),
                ("PREMIERE", "MATH_1RE_AUTO_TAUX_CALCULER",     "Automatismes", "Calculer un taux d'évolution et l'exprimer en pourcentage", 14),
                ("PREMIERE", "MATH_1RE_AUTO_TAUX_SUCCESSIFS",   "Automatismes", "Calculer le taux d'évolution équivalent à plusieurs évolutions successives", 15),
                ("PREMIERE", "MATH_1RE_AUTO_TAUX_RECIPROQUE",   "Automatismes", "Calculer un taux d'évolution réciproque", 16),
                ("PREMIERE", "MATH_1RE_AUTO_PRODUIT_NUL",       "Automatismes", "Déterminer les solutions d'une équation produit nul", 17),
                ("PREMIERE", "MATH_1RE_AUTO_SIGNE",             "Automatismes", "Déterminer le signe d'une expression du premier degré ou d'une expression factorisée du second degré", 18),
                ("PREMIERE", "MATH_1RE_AUTO_CALCUL_LITTERAL",   "Automatismes", "Développer, factoriser et réduire une expression algébrique simple", 19),
                ("PREMIERE", "MATH_1RE_AUTO_GRAPH_EQUATION",    "Automatismes", "Résoudre graphiquement une équation ou une inéquation du type f(x) = k ou f(x) < k", 20),
                ("PREMIERE", "MATH_1RE_AUTO_GRAPH_SIGNE",       "Automatismes", "Déterminer graphiquement le signe d'une fonction ou son tableau de variations", 21),
                ("PREMIERE", "MATH_1RE_AUTO_DROITE_TRACER",     "Automatismes", "Tracer une droite donnée par son équation réduite ou par un point et son coefficient directeur", 22),
                ("PREMIERE", "MATH_1RE_AUTO_DROITE_LIRE",       "Automatismes", "Lire graphiquement l'équation réduite d'une droite", 23),
                ("PREMIERE", "MATH_1RE_AUTO_COEFF_DIRECTEUR",   "Automatismes", "Déterminer le coefficient directeur d'une droite à partir des coordonnées de deux de ses points", 24),
                ("PREMIERE", "MATH_1RE_AUTO_STAT_LIRE",         "Automatismes", "Lire un graphique, un histogramme, un diagramme en barres, circulaire ou en boîte (origine, unités, échelles)", 25),
                ("PREMIERE", "MATH_1RE_AUTO_STAT_DONNEES",      "Automatismes", "Passer d'un graphique aux données et inversement", 26),
                ("PREMIERE", "MATH_1RE_AUTO_STAT_INDICATEURS",  "Automatismes", "Calculer et interpréter les indicateurs statistiques d'une série", 27),
                ("PREMIERE", "MATH_1RE_PROBA_COND",             "Automatismes", "Calculer une probabilité conditionnelle à partir d'un tableau croisé d'effectifs ou d'un arbre pondéré", 28),
                ("PREMIERE", "MATH_1RE_AUTO_PROBA_NOTATIONS",   "Automatismes", "Distinguer P(A ∩ B), P_A(B) et P_B(A)", 29),

                // --- Première spécialité : algèbre — suites numériques, modèles discrets ---
                ("PREMIERE", "MATH_1RE_SUITES_NOTION",          "Algèbre", "Définir une suite de façon explicite, par une relation de récurrence, par un algorithme ou par un motif", 30),
                ("PREMIERE", "MATH_1RE_SUITES_REGISTRES",       "Algèbre", "Passer du langage naturel au registre algébrique ou graphique pour étudier une suite", 31),
                ("PREMIERE", "MATH_1RE_SUITES_MOTIF",           "Algèbre", "Trouver une relation explicite ou de récurrence pour une suite issue d'un motif ou d'un dénombrement", 32),
                ("PREMIERE", "MATH_1RE_SUITES_TERMES",          "Algèbre", "Calculer des termes d'une suite définie explicitement, par récurrence ou par un algorithme", 33),
                ("PREMIERE", "MATH_1RE_SUITES_ARITH",           "Algèbre", "Calculer le terme général d'une suite arithmétique et la somme 1 + 2 + … + n", 34),
                ("PREMIERE", "MATH_1RE_SUITES_GEO",             "Algèbre", "Calculer le terme général d'une suite géométrique et la somme 1 + q + … + qⁿ", 35),
                ("PREMIERE", "MATH_1RE_SUITES_VARIATION",       "Algèbre", "Déterminer le sens de variation d'une suite, notamment arithmétique ou géométrique", 36),
                ("PREMIERE", "MATH_1RE_SUITES_MODELE",          "Algèbre", "Modéliser une croissance linéaire par une suite arithmétique, exponentielle par une suite géométrique", 37),
                ("PREMIERE", "MATH_1RE_ALGO_SEUIL",             "Algèbre", "Écrire un algorithme calculant des termes, une somme de termes ou un seuil", 38),
                ("PREMIERE", "MATH_1RE_SUITES_LIMITE",          "Algèbre", "Conjecturer, dans des cas simples, la limite éventuelle d'une suite", 39),

                // --- Première spécialité : algèbre — équations, fonctions polynômes du second degré ---
                // « Le calcul effectif de la forme canonique dans le cas général
                // n'est pas un attendu du programme » : d'où « choisir » la forme.
                ("PREMIERE", "MATH_1RE_TRINOME_SIGNE",          "Algèbre", "Étudier le signe d'une fonction polynôme du second degré, sous forme factorisée ou à l'aide du discriminant", 40),
                ("PREMIERE", "MATH_1RE_TRINOME_DEUX_RACINES",   "Algèbre", "Déterminer les fonctions polynômes du second degré s'annulant en deux réels distincts donnés", 41),
                ("PREMIERE", "MATH_1RE_TRINOME_FACTORISER",     "Algèbre", "Factoriser un trinôme par racine évidente, somme et produit des racines, identité remarquable ou formules", 42),
                ("PREMIERE", "MATH_1RE_TRINOME_RACINES",        "Algèbre", "Résoudre une équation du second degré à l'aide du discriminant", 43),
                ("PREMIERE", "MATH_1RE_TRINOME_FORME",          "Algèbre", "Choisir la forme développée, canonique ou factorisée adaptée à une équation, une inéquation ou une optimisation", 44),

                // --- Première spécialité : analyse — dérivation ---
                ("PREMIERE", "MATH_1RE_DERIV_TAUX",             "Analyse", "Calculer un taux de variation et la pente d'une sécante", 45),
                ("PREMIERE", "MATH_1RE_DERIV_INTERPRETER",      "Analyse", "Interpréter un nombre dérivé en contexte : pente de tangente, vitesse instantanée, coût marginal", 46),
                ("PREMIERE", "MATH_1RE_DERIV_GRAPHIQUE",        "Analyse", "Lire graphiquement un nombre dérivé et construire la tangente connaissant le nombre dérivé", 47),
                ("PREMIERE", "MATH_1RE_DERIV_NOMBRE",           "Analyse", "Déterminer l'équation de la tangente en un point à la courbe représentative d'une fonction", 48),
                ("PREMIERE", "MATH_1RE_DERIV_APPROX",           "Analyse", "Calculer une valeur approchée de f(a + h) par l'approximation affine f(a) + f'(a)h", 49),
                ("PREMIERE", "MATH_1RE_DERIV_REFERENCE",        "Analyse", "Dériver les fonctions carré, cube, inverse, racine carrée et x ↦ xⁿ pour n entier relatif", 50),
                ("PREMIERE", "MATH_1RE_DERIV_FONCTION",         "Analyse", "Calculer une fonction dérivée à l'aide des opérations : somme, produit, inverse, quotient", 51),
                ("PREMIERE", "MATH_1RE_DERIV_NON_DERIVABLE",    "Analyse", "Étudier la dérivabilité en 0 de la fonction valeur absolue et de la fonction racine carrée", 52),

                // --- Première spécialité : analyse — variations et courbes représentatives ---
                ("PREMIERE", "MATH_1RE_FONC_PARITE",            "Analyse", "Reconnaître algébriquement et graphiquement une fonction paire ou impaire", 53),
                ("PREMIERE", "MATH_1RE_DERIV_VARIATION",        "Analyse", "Étudier les variations d'une fonction et déterminer ses extrémums par le signe de sa dérivée", 54),
                ("PREMIERE", "MATH_1RE_DERIV_OPTIMISATION",     "Analyse", "Résoudre un problème d'optimisation", 55),
                ("PREMIERE", "MATH_1RE_DERIV_INEGALITE",        "Analyse", "Établir une inégalité ou la position relative de deux courbes à l'aide des variations", 56),
                ("PREMIERE", "MATH_1RE_TRINOME_DERIVATION",     "Analyse", "Étudier par dérivation un polynôme du second degré : variations, extrémum, allure selon le signe de a", 57),

                // --- Première spécialité : analyse — fonction exponentielle ---
                ("PREMIERE", "MATH_1RE_EXP",                    "Analyse", "Transformer une expression avec les propriétés algébriques de la fonction exponentielle", 58),
                ("PREMIERE", "MATH_1RE_EXP_ETUDE",              "Analyse", "Connaître le signe, le sens de variation et la courbe de la fonction exponentielle", 59),
                ("PREMIERE", "MATH_1RE_EXP_DERIVEE",            "Analyse", "Dériver la fonction t ↦ e^(at) pour a réel", 60),
                ("PREMIERE", "MATH_1RE_EXP_MODELE",             "Analyse", "Modéliser une croissance ou une décroissance exponentielle et représenter t ↦ e^(kt) et t ↦ e^(–kt)", 61),

                // --- Première spécialité : analyse — trigonométrie ---
                ("PREMIERE", "MATH_1RE_TRIGO_CERCLE",           "Analyse", "Placer sur le cercle trigonométrique le point image d'un réel (radian, longueur d'arc)", 62),
                ("PREMIERE", "MATH_1RE_TRIGO_VALEURS",          "Analyse", "Lire sur le cercle le cosinus et le sinus des valeurs remarquables et des angles associés", 63),

                // --- Première spécialité : géométrie — calcul vectoriel et produit scalaire ---
                ("PREMIERE", "MATH_1RE_SCAL_DEFINITION",        "Géométrie", "Calculer un produit scalaire par projection orthogonale, coordonnées, normes et angle, ou normes seules", 64),
                ("PREMIERE", "MATH_1RE_SCAL_ORTHOGONAL",        "Géométrie", "Démontrer une orthogonalité à l'aide du produit scalaire", 65),
                ("PREMIERE", "MATH_1RE_SCAL_APPLICATION",       "Géométrie", "Calculer un angle ou une longueur dans le plan par le produit scalaire ou la formule d'Al-Kashi", 66),
                ("PREMIERE", "MATH_1RE_SCAL_PROPRIETES",        "Géométrie", "Utiliser la bilinéarité, la symétrie et le développement de ‖u + v‖² et ‖u – v‖²", 67),
                ("PREMIERE", "MATH_1RE_SCAL_MA_MB",             "Géométrie", "Transformer MA·MB et déterminer l'ensemble des points M tels que MA·MB = 0", 68),

                // --- Première spécialité : géométrie — géométrie repérée ---
                ("PREMIERE", "MATH_1RE_REP_VECTEUR_NORMAL",     "Géométrie", "Déterminer une équation cartésienne de droite connaissant un point et un vecteur normal", 69),
                ("PREMIERE", "MATH_1RE_REP_PROJETE",            "Géométrie", "Déterminer les coordonnées du projeté orthogonal d'un point sur une droite", 70),
                ("PREMIERE", "MATH_1RE_CERCLE_EQUATION",        "Géométrie", "Écrire l'équation d'un cercle de centre et rayon donnés, et retrouver centre et rayon d'une équation", 71),
                ("PREMIERE", "MATH_1RE_REP_CONFIGURATION",      "Géométrie", "Utiliser un repère orthonormé pour étudier une configuration", 72),

                // --- Première spécialité : probabilités conditionnelles et indépendance ---
                ("PREMIERE", "MATH_1RE_PROBA_ARBRE",            "Statistiques et probabilités", "Calculer une probabilité à l'aide de la formule des probabilités totales", 73),
                ("PREMIERE", "MATH_1RE_PROBA_INDEP",            "Statistiques et probabilités", "Utiliser ou justifier l'indépendance de deux événements", 74),
                ("PREMIERE", "MATH_1RE_PROBA_DEUX_EPREUVES",    "Statistiques et probabilités", "Représenter la succession de deux épreuves indépendantes par un arbre ou un tableau", 75),
                ("PREMIERE", "MATH_1RE_PROBA_BERNOULLI",        "Statistiques et probabilités", "Calculer des probabilités sur l'arbre de n ≤ 4 épreuves de Bernoulli indépendantes et identiques", 76),

                // --- Première spécialité : variables aléatoires réelles ---
                ("PREMIERE", "MATH_1RE_VA_NOTATIONS",           "Statistiques et probabilités", "Interpréter et utiliser les notations {X = a}, {X ≤ a}, P(X = a), P(X ≤ a)", 77),
                ("PREMIERE", "MATH_1RE_VA_LOI",                 "Statistiques et probabilités", "Modéliser une situation par une variable aléatoire et déterminer sa loi de probabilité", 78),
                ("PREMIERE", "MATH_1RE_VA_ESPERANCE",           "Statistiques et probabilités", "Calculer l'espérance, la variance et l'écart type d'une variable aléatoire", 79),
                ("PREMIERE", "MATH_1RE_VA_LINEARITE",           "Statistiques et probabilités", "Utiliser la linéarité de l'espérance et la formule de König-Huygens", 80),
                ("PREMIERE", "MATH_1RE_VA_PROBLEME",            "Statistiques et probabilités", "Utiliser l'espérance pour résoudre un problème, par exemple fixer la mise d'un jeu équitable", 81),

                // --- Première spécialité : expérimentations ---
                ("PREMIERE", "MATH_1RE_SIMUL_VA",               "Statistiques et probabilités", "Simuler une variable aléatoire avec Python ou un tableur", 82),
                ("PREMIERE", "MATH_1RE_SIMUL_MOYENNE",          "Statistiques et probabilités", "Lire, comprendre et écrire une fonction Python renvoyant la moyenne d'un échantillon de taille n", 83),
                ("PREMIERE", "MATH_1RE_SIMUL_ECART",            "Statistiques et probabilités", "Simuler N échantillons et calculer la proportion de moyennes à au plus 2σ/√n de l'espérance", 84),

                // --- Première générale, maths de l'ENSEIGNEMENT SCIENTIFIQUE — arrêté du 26/02/2026, NOR MENE2602916A ---
                ("PREMIERE", "MATH_1RE_ES_TAUX_APPLIQUER", "Enseignement scientifique", "Appliquer un taux d’évolution pour trouver une valeur initiale ou finale", 85),
                ("PREMIERE", "MATH_1RE_ES_TAUX_CALCULER", "Enseignement scientifique", "Calculer un taux d’évolution et l’exprimer en pourcentage", 86),
                ("PREMIERE", "MATH_1RE_ES_TAUX_SUCCESSIFS", "Enseignement scientifique", "Calculer le taux équivalent à plusieurs évolutions successives", 87),
                ("PREMIERE", "MATH_1RE_ES_TAUX_RECIPROQUE", "Enseignement scientifique", "Calculer un taux d’évolution réciproque", 88),
                ("PREMIERE", "MATH_1RE_ES_GRAPH_SIGNE", "Enseignement scientifique", "Lire graphiquement le signe et les variations d’une fonction", 89),
                ("PREMIERE", "MATH_1RE_ES_GRAPH_DROITE", "Enseignement scientifique", "Lire et tracer une droite à partir de son équation réduite", 90),
                ("PREMIERE", "MATH_1RE_ES_GRAPH_DONNEES", "Enseignement scientifique", "Lire un graphique ou un diagramme et revenir aux données", 91),
                ("PREMIERE", "MATH_1RE_ES_TABLEUR", "Enseignement scientifique", "Représenter des données avec un tableur", 92),
                ("PREMIERE", "MATH_1RE_ES_AJUSTEMENT", "Enseignement scientifique", "Utiliser un ajustement affine pour interpoler ou extrapoler", 93),
                ("PREMIERE", "MATH_1RE_ES_PROBA_COND", "Enseignement scientifique", "Calculer une probabilité conditionnelle avec un tableau croisé ou un arbre", 94),
                ("PREMIERE", "MATH_1RE_ES_BERNOULLI", "Enseignement scientifique", "Représenter par un arbre la répétition d’épreuves identiques et indépendantes", 95),
                ("PREMIERE", "MATH_1RE_ES_LINEAIRE", "Enseignement scientifique", "Reconnaître et modéliser une croissance linéaire", 96),
                ("PREMIERE", "MATH_1RE_ES_ARITHMETIQUE", "Enseignement scientifique", "Calculer un terme d’une suite arithmétique et résoudre un problème de seuil", 97),
                ("PREMIERE", "MATH_1RE_ES_QUADRATIQUE", "Enseignement scientifique", "Associer une parabole à une expression du second degré et trouver ses racines", 98),
                ("PREMIERE", "MATH_1RE_ES_EXPONENTIELLE", "Enseignement scientifique", "Reconnaître et modéliser une croissance ou une décroissance exponentielle", 99),
                ("PREMIERE", "MATH_1RE_ES_TAUX_MOYEN", "Enseignement scientifique", "Calculer un taux d’évolution moyen", 100),
                ("PREMIERE", "MATH_1RE_ES_SEUIL_EXPO", "Enseignement scientifique", "Résoudre un problème de seuil en croissance exponentielle", 101),

                // Compléments du 14/09/2026 : capacités attendues de l'annexe
                // MENE2602916A qui n'avaient aucune ligne (automatismes évalués au
                // QCM de l'épreuve anticipée, information chiffrée, phénomènes
                // aléatoires, variations linéaire, quadratique et exponentielle).
                ("PREMIERE", "MATH_1RE_ES_PRODUIT_NUL", "Enseignement scientifique", "Déterminer les solutions d'une équation produit nul", 102),
                ("PREMIERE", "MATH_1RE_ES_SIGNE_EXPRESSION", "Enseignement scientifique", "Déterminer le signe d'une expression du premier degré ou d'une expression factorisée du second degré", 103),
                ("PREMIERE", "MATH_1RE_ES_CALCUL_LITTERAL", "Enseignement scientifique", "Développer, factoriser et réduire une expression algébrique simple", 104),
                ("PREMIERE", "MATH_1RE_ES_GRAPH_EQUATION", "Enseignement scientifique", "Résoudre graphiquement une équation ou une inéquation du type f(x) = k ou f(x) < k", 105),
                ("PREMIERE", "MATH_1RE_ES_COEFF_DIRECTEUR", "Enseignement scientifique", "Déterminer le coefficient directeur d'une droite à partir des coordonnées de deux de ses points", 106),
                ("PREMIERE", "MATH_1RE_ES_DROITE_LIRE", "Enseignement scientifique", "Lire graphiquement l'équation réduite d'une droite", 107),
                ("PREMIERE", "MATH_1RE_ES_STAT_INDICATEURS", "Enseignement scientifique", "Calculer et interpréter les indicateurs statistiques d'une série", 108),
                ("PREMIERE", "MATH_1RE_ES_PROBA_NOTATIONS", "Enseignement scientifique", "Distinguer P(A ∩ B), P_A(B) et P_B(A)", 109),
                ("PREMIERE", "MATH_1RE_ES_TABLEAU_CROISE", "Enseignement scientifique", "Analyser deux caractères qualitatifs à l'aide d'un tableau croisé d'effectifs", 110),
                ("PREMIERE", "MATH_1RE_ES_POINT_MOYEN", "Enseignement scientifique", "Calculer les coordonnées du point moyen d'un nuage de points", 111),
                ("PREMIERE", "MATH_1RE_ES_INDEPENDANCE", "Enseignement scientifique", "Utiliser ou justifier l'indépendance de deux événements", 112),
                ("PREMIERE", "MATH_1RE_ES_GRAPH_LINEAIRE", "Enseignement scientifique", "Réaliser et exploiter la représentation graphique d'une suite arithmétique ou d'une fonction affine", 113),
                ("PREMIERE", "MATH_1RE_ES_PARABOLE_ELEMENTS", "Enseignement scientifique", "Déterminer sans formule l'axe de symétrie, le sommet et les variations de x ↦ ax² + bx + c", 114),
                ("PREMIERE", "MATH_1RE_ES_GEOMETRIQUE", "Enseignement scientifique", "Calculer un terme de rang donné d'une suite géométrique définie par une relation fonctionnelle ou de récurrence", 115),
                ("PREMIERE", "MATH_1RE_ES_GRAPH_EXPO", "Enseignement scientifique", "Réaliser et exploiter la représentation graphique d'une suite géométrique ou d'une fonction x ↦ aˣ", 116),
                ("PREMIERE", "MATH_1RE_ES_ORDRE_GRANDEUR", "Enseignement scientifique", "Estimer l'ordre de grandeur d'une quantité en croissance ou décroissance exponentielle", 117),

                // --- Terminale : Analyse ---
                // Programme de spécialité de 2019 (NOR MENE1921246A), en vigueur
                // en 2026-2027. MATH_TLE_LOGIQUE_RECURRENCE retirée : doublon de
                // MATH_TLE_RECURRENCE, le code le plus ancien, gardé.
                ("TERMINALE", "MATH_TLE_RECURRENCE",     "Analyse", "Démontrer par récurrence", 1),
                ("TERMINALE", "MATH_TLE_SUITES_LIMITE",  "Analyse", "Déterminer la limite d'une suite", 2),
                ("TERMINALE", "MATH_TLE_SUITES_CONV",    "Analyse", "Utiliser les théorèmes de convergence et de comparaison", 3),
                ("TERMINALE", "MATH_TLE_FONC_LIMITE",    "Analyse", "Déterminer la limite d'une fonction et ses asymptotes", 4),
                ("TERMINALE", "MATH_TLE_CONTINUITE",     "Analyse", "Utiliser la continuité et le théorème des valeurs intermédiaires", 5),
                ("TERMINALE", "MATH_TLE_DERIV_COMPOSEE", "Analyse", "Dériver une fonction composée", 6),
                ("TERMINALE", "MATH_TLE_LOG",            "Analyse", "Utiliser la fonction logarithme népérien", 7),

                // Section « Fonctions sinus et cosinus » du programme de 2019.
                ("TERMINALE", "MATH_TLE_TRIGO_FONCTIONS", "Analyse", "Connaître les dérivées, les variations et les courbes représentatives des fonctions sinus et cosinus", 8),
                ("TERMINALE", "MATH_TLE_TRIGO_EQUATION", "Analyse", "Résoudre une équation cos(x) = a ou une inéquation cos(x) ≤ a sur [–π, π]", 9),
                ("TERMINALE", "MATH_TLE_TRIGO_ETUDE",    "Analyse", "Étudier une fonction simple définie à partir de sinus et cosinus pour déterminer ses variations ou un optimum", 10),

                ("TERMINALE", "MATH_TLE_CONVEXITE",      "Analyse", "Étudier la convexité d'une fonction", 11),
                ("TERMINALE", "MATH_TLE_PRIMITIVES",     "Analyse", "Déterminer les primitives d'une fonction", 12),
                ("TERMINALE", "MATH_TLE_INTEGRALE",      "Analyse", "Calculer une intégrale et une aire", 13),
                ("TERMINALE", "MATH_TLE_EQ_DIFF",        "Analyse", "Résoudre une équation différentielle du premier ordre", 14),

                // --- Terminale : Géométrie dans l'espace ---
                ("TERMINALE", "MATH_TLE_VECT_ESPACE",    "Géométrie", "Utiliser les vecteurs de l'espace et la colinéarité", 15),
                ("TERMINALE", "MATH_TLE_DROITES_PLANS",  "Géométrie", "Étudier les positions relatives de droites et de plans", 16),
                ("TERMINALE", "MATH_TLE_SCAL_ESPACE",    "Géométrie", "Utiliser le produit scalaire dans l'espace", 17),
                ("TERMINALE", "MATH_TLE_PARAMETRIQUE",   "Géométrie", "Déterminer une représentation paramétrique de droite", 18),
                ("TERMINALE", "MATH_TLE_PLAN_CARTESIEN", "Géométrie", "Déterminer une équation cartésienne de plan", 19),
                ("TERMINALE", "MATH_TLE_ORTHOGONALITE",  "Géométrie", "Résoudre un problème d'orthogonalité et de distance", 20),

                // --- Terminale : Combinatoire et dénombrement (section du programme de 2019) ---
                ("TERMINALE", "MATH_TLE_DENOMBR_REPRESENTER", "Combinatoire et dénombrement", "Choisir une représentation adaptée (ensembles, arbres, tableaux, diagrammes) et reconnaître les objets à dénombrer", 21),
                ("TERMINALE", "MATH_TLE_DENOMBR_PRINCIPES", "Combinatoire et dénombrement", "Dénombrer par le principe additif (ensembles disjoints) et le principe multiplicatif (produit cartésien)", 22),
                ("TERMINALE", "MATH_TLE_DENOMBR_KUPLETS", "Combinatoire et dénombrement", "Dénombrer les k-uplets d'un ensemble à n éléments et les parties d'un ensemble à n éléments", 23),
                ("TERMINALE", "MATH_TLE_DENOMBR_PERMUTATIONS", "Combinatoire et dénombrement", "Dénombrer les k-uplets d'éléments distincts et les permutations d'un ensemble à n éléments à l'aide de n!", 24),
                ("TERMINALE", "MATH_TLE_DENOMBR_COMBINAISONS", "Combinatoire et dénombrement", "Calculer le nombre de combinaisons de k éléments parmi n et l'interpréter en termes de mots ou de chemins", 25),
                ("TERMINALE", "MATH_TLE_DENOMBR_PASCAL",  "Combinatoire et dénombrement", "Utiliser la symétrie des coefficients binomiaux, la relation et le triangle de Pascal", 26),

                // --- Terminale : Probabilités ---
                ("TERMINALE", "MATH_TLE_BINOMIALE",      "Statistiques et probabilités", "Utiliser la loi binomiale et les coefficients binomiaux", 27),
                ("TERMINALE", "MATH_TLE_VA_SOMME",       "Statistiques et probabilités", "Calculer espérance et variance d'une somme de variables", 28),
                ("TERMINALE", "MATH_TLE_CONCENTRATION",  "Statistiques et probabilités", "Utiliser l'inégalité de concentration et la loi des grands nombres", 29),

                ("TERMINALE", "MATH_TLE_ALGO_SIMULATION","Algorithmique", "Écrire un programme de simulation ou de calcul approché", 30),
                ("TERMINALE", "MATH_TLE_LOGIQUE_NECESSAIRE", "Analyse", "Distinguer condition nécessaire et condition suffisante", 31),

                // ================================================================
                // VOIE TECHNOLOGIQUE ET VOIE PROFESSIONNELLE
                // ================================================================
                //
                // POURQUOI CES CINQ CLASSES ONT DÉSORMAIS LEUR PROPRE PROGRAMME.
                //
                // Le référentiel était écrit pour les classes générales, et les
                // autres voies en héritaient par le RANG d'année. Un élève de
                // terminale professionnelle recevait donc les 23 compétences de
                // la SPÉCIALITÉ de terminale générale — récurrence, logarithme
                // népérien, équations différentielles, produit scalaire dans
                // l'espace. Il n'en étudie aucune, et son propre programme de
                // mathématiques, lui, n'existait nulle part.
                //
                // Les mathématiques sont OBLIGATOIRES dans ces deux voies, pour
                // tous les élèves, ce qui n'est plus le cas en voie générale
                // depuis 2019. C'est donc là que l'erreur touchait le plus de
                // monde.
                //
                // `VoiesScolaires.RetenirPourLaVoie` fait le tri : ces
                // compétences-ci ne partent qu'aux élèves de la voie concernée,
                // et l'héritage par rang continue de s'appliquer partout où une
                // voie n'a pas de référentiel propre.
                //
                // SOURCES, vérifiées le 6 septembre 2026 :
                //   Seconde pro      arrêté du 03/04/2019, BO spécial n° 5 du
                //                    11/04/2019 — en vigueur.
                //   1re et Tle pro   arrêté du 03/02/2020, BO spécial n° 1 du
                //                    06/02/2020, annexes 1 et 2 — en vigueur.
                //   1re techno       arrêté du 26/02/2026, NOR MENE2602918A,
                //                    BO n° 14 du 02/04/2026 — EN VIGUEUR DEPUIS
                //                    LA RENTRÉE 2026. Le programme de 2019 est
                //                    périmé. Lignes réécrites d'après l'annexe
                //                    le 14/09/2026 : les précédentes, bien que
                //                    datées de 2026, suivaient encore 2019.
                //   Tle techno       arrêté du 19/07/2019, BO spécial n° 8 —
                //                    encore en vigueur cette année. Son
                //                    remplaçant (arrêté du 26/02/2026) entre en
                //                    application à la RENTRÉE 2027 : repasser
                //                    ici à ce moment-là.
                //
                // Le programme du bac pro est découpé en groupements de
                // spécialités (A, B, C). La base ne connaît pas la spécialité de
                // l'élève : comme pour la physique-chimie, on retient le tronc
                // commun des trois groupements et on ajoute les modules propres
                // à un seul — mieux vaut proposer à tous que retirer à ceux qui
                // l'ont.

                // --- Seconde professionnelle : statistique et probabilités ---
                ("SECONDE_PRO", "MATH_2PRO_STAT_ORGANISER",   "Statistiques et probabilités", "Recueillir et organiser des données statistiques par classes", 1),
                ("SECONDE_PRO", "MATH_2PRO_STAT_REPRESENTER", "Statistiques et probabilités", "Représenter une série statistique par le diagramme adapté", 2),
                ("SECONDE_PRO", "MATH_2PRO_STAT_EXTRAIRE",    "Statistiques et probabilités", "Extraire une information de la représentation d'une série", 3),
                ("SECONDE_PRO", "MATH_2PRO_STAT_POSITION",    "Statistiques et probabilités", "Comparer des séries à l'aide d'indicateurs de position", 4),
                ("SECONDE_PRO", "MATH_2PRO_STAT_DISPERSION",  "Statistiques et probabilités", "Comparer des séries à l'aide d'indicateurs de dispersion", 5),
                ("SECONDE_PRO", "MATH_2PRO_STAT_BOITE",       "Statistiques et probabilités", "Construire et interpréter un diagramme en boîte à moustaches", 6),
                ("SECONDE_PRO", "MATH_2PRO_PROB_FLUCTUATION", "Statistiques et probabilités", "Observer la fluctuation d'une fréquence selon les échantillons", 7),
                ("SECONDE_PRO", "MATH_2PRO_PROB_SIMULATION",  "Statistiques et probabilités", "Réaliser une simulation informatique d'un échantillonnage", 8),
                ("SECONDE_PRO", "MATH_2PRO_PROB_ESTIMER",     "Statistiques et probabilités", "Estimer une probabilité à partir des fréquences observées", 9),
                ("SECONDE_PRO", "MATH_2PRO_PROB_CALCULER",    "Statistiques et probabilités", "Calculer la probabilité d'un événement dans une situation simple", 10),

                // --- Seconde professionnelle : algèbre et analyse ---
                ("SECONDE_PRO", "MATH_2PRO_ALG_EQUATION",     "Algèbre et analyse", "Résoudre algébriquement une équation du premier degré", 11),
                ("SECONDE_PRO", "MATH_2PRO_ALG_INEQUATION",   "Algèbre et analyse", "Résoudre une inéquation du premier degré et représenter ses solutions", 12),
                ("SECONDE_PRO", "MATH_2PRO_ALG_GRAPHIQUE",    "Algèbre et analyse", "Résoudre graphiquement un problème du premier degré", 13),
                ("SECONDE_PRO", "MATH_2PRO_ALG_MODELISER",    "Algèbre et analyse", "Traduire une situation professionnelle par une équation", 14),
                ("SECONDE_PRO", "MATH_2PRO_PROP_COMMERCE",    "Algèbre et analyse", "Calculer un prix, un coût, une marge, une taxe ou une remise", 15),
                ("SECONDE_PRO", "MATH_2PRO_PROP_FACTURE",     "Algèbre et analyse", "Compléter une facture, un bon de commande ou un devis", 16),
                ("SECONDE_PRO", "MATH_2PRO_PROP_INTERET",     "Algèbre et analyse", "Calculer un intérêt simple et une valeur acquise", 17),
                ("SECONDE_PRO", "MATH_2PRO_PROP_PLACEMENT",   "Algèbre et analyse", "Déterminer un taux, une durée ou un capital de placement", 18),

                // --- Seconde professionnelle : géométrie ---
                ("SECONDE_PRO", "MATH_2PRO_GEO_GRANDEURS",    "Géométrie", "Calculer des longueurs, des aires et des volumes", 19),
                ("SECONDE_PRO", "MATH_2PRO_GEO_THEOREMES",    "Géométrie", "Utiliser Pythagore, Thalès et la trigonométrie du triangle rectangle", 20),
                ("SECONDE_PRO", "MATH_2PRO_GEO_ESPACE",       "Géométrie", "Lire et produire la représentation d'un solide de l'espace", 21),

                // --- Seconde professionnelle : algorithmique et logique ---
                ("SECONDE_PRO", "MATH_2PRO_ALGO_FONCTION",    "Algorithmique et programmation", "Écrire et utiliser une fonction dans un programme", 22),
                ("SECONDE_PRO", "MATH_2PRO_ALGO_BOUCLE",      "Algorithmique et programmation", "Utiliser une boucle et une instruction conditionnelle", 23),
                ("SECONDE_PRO", "MATH_2PRO_LOGIQUE_INTERVALLE", "Algorithmique et programmation", "Utiliser le vocabulaire ensembliste et les intervalles", 24),

                // --- Première professionnelle : statistique et probabilités ---
                ("PREMIERE_PRO", "MATH_1PRO_STAT_NUAGE",       "Statistiques et probabilités", "Représenter un nuage de points", 1),
                ("PREMIERE_PRO", "MATH_1PRO_STAT_AJUSTEMENT",  "Statistiques et probabilités", "Déterminer l'équation réduite d'une droite d'ajustement", 2),
                ("PREMIERE_PRO", "MATH_1PRO_STAT_INTERPOLER",  "Statistiques et probabilités", "Interpoler ou extrapoler une valeur à partir d'un ajustement", 3),
                ("PREMIERE_PRO", "MATH_1PRO_STAT_PERTINENCE",  "Statistiques et probabilités", "Évaluer la pertinence d'un ajustement affine", 4),
                ("PREMIERE_PRO", "MATH_1PRO_PROB_ARBRE",       "Statistiques et probabilités", "Calculer une probabilité à l'aide d'un arbre pondéré ou d'un tableau", 5),
                ("PREMIERE_PRO", "MATH_1PRO_PROB_CONDITION",   "Statistiques et probabilités", "Calculer une probabilité conditionnelle", 6),

                // --- Première professionnelle : algèbre et analyse ---
                ("PREMIERE_PRO", "MATH_1PRO_SUITE_RECONNAITRE", "Algèbre et analyse", "Reconnaître une suite arithmétique ou géométrique", 7),
                ("PREMIERE_PRO", "MATH_1PRO_SUITE_TERME",      "Algèbre et analyse", "Calculer un terme d'une suite et déterminer son sens de variation", 8),
                ("PREMIERE_PRO", "MATH_1PRO_SUITE_MODELISER",  "Algèbre et analyse", "Modéliser une évolution par une suite", 9),
                ("PREMIERE_PRO", "MATH_1PRO_POLY_FACTORISER",  "Algèbre et analyse", "Factoriser un polynôme de degré 2", 10),
                ("PREMIERE_PRO", "MATH_1PRO_POLY_RACINES",     "Algèbre et analyse", "Déterminer les racines et le signe d'un polynôme de degré 2", 11),
                ("PREMIERE_PRO", "MATH_1PRO_POLY_ETUDIER",     "Algèbre et analyse", "Étudier une fonction polynôme de degré 2", 12),
                ("PREMIERE_PRO", "MATH_1PRO_DERIV_CALCULER",   "Algèbre et analyse", "Calculer la fonction dérivée d'une fonction de référence", 13),
                ("PREMIERE_PRO", "MATH_1PRO_DERIV_VARIATIONS", "Algèbre et analyse", "Dresser un tableau de variations à partir du signe de la dérivée", 14),
                ("PREMIERE_PRO", "MATH_1PRO_DERIV_EXTREMUM",   "Algèbre et analyse", "Déterminer un extremum et l'interpréter dans son contexte", 15),

                // --- Première professionnelle : géométrie ---
                ("PREMIERE_PRO", "MATH_1PRO_VECT_COORD",       "Géométrie", "Utiliser les coordonnées d'un vecteur du plan", 16),
                ("PREMIERE_PRO", "MATH_1PRO_VECT_COLINEAIRE",  "Géométrie", "Reconnaître deux vecteurs colinéaires", 17),
                ("PREMIERE_PRO", "MATH_1PRO_TRIGO_CERCLE",     "Géométrie", "Utiliser le cercle trigonométrique, le cosinus et le sinus", 18),
                ("PREMIERE_PRO", "MATH_1PRO_GEO_ESPACE",       "Géométrie", "Calculer des grandeurs dans un solide de l'espace", 19),

                // --- Première professionnelle : algorithmique et logique ---
                ("PREMIERE_PRO", "MATH_1PRO_ALGO_LISTE",       "Algorithmique et programmation", "Écrire un programme utilisant une liste", 20),
                ("PREMIERE_PRO", "MATH_1PRO_LOGIQUE",          "Algorithmique et programmation", "Employer le vocabulaire ensembliste et les connecteurs logiques", 21),

                // --- Terminale professionnelle : statistique et probabilités ---
                ("TERMINALE_PRO", "MATH_TPRO_STAT_AJUSTEMENT", "Statistiques et probabilités", "Ajuster un nuage de points par une droite ou une courbe", 1),
                ("TERMINALE_PRO", "MATH_TPRO_STAT_EXPLOITER",  "Statistiques et probabilités", "Exploiter un ajustement pour estimer une valeur", 2),

                // --- Terminale professionnelle : algèbre et analyse ---
                ("TERMINALE_PRO", "MATH_TPRO_SUITE_COMPORT",   "Algèbre et analyse", "Étudier le comportement d'une suite arithmétique ou géométrique", 3),
                ("TERMINALE_PRO", "MATH_TPRO_SUITE_SOMME",     "Algèbre et analyse", "Calculer la somme des termes d'une suite", 4),
                ("TERMINALE_PRO", "MATH_TPRO_POLY3_DERIVEE",   "Algèbre et analyse", "Calculer la dérivée d'une fonction polynôme de degré 3", 5),
                ("TERMINALE_PRO", "MATH_TPRO_POLY3_VARIATIONS", "Algèbre et analyse", "Dresser le tableau de variations d'une fonction polynôme de degré 3", 6),
                ("TERMINALE_PRO", "MATH_TPRO_POLY3_EXPLOITER", "Algèbre et analyse", "Exploiter un tableau de variations pour résoudre un problème", 7),
                ("TERMINALE_PRO", "MATH_TPRO_EXPO_FONCTION",   "Algèbre et analyse", "Utiliser une fonction exponentielle de base q", 8),
                ("TERMINALE_PRO", "MATH_TPRO_EXPO_TAUX",       "Algèbre et analyse", "Calculer un taux d'évolution moyen", 9),
                ("TERMINALE_PRO", "MATH_TPRO_LOG_DECIMAL",     "Algèbre et analyse", "Utiliser le logarithme décimal et ses propriétés", 10),
                ("TERMINALE_PRO", "MATH_TPRO_LOG_EQUATION",    "Algèbre et analyse", "Résoudre une équation où l'inconnue est un exposant", 11),
                ("TERMINALE_PRO", "MATH_TPRO_LOG_DUREE",       "Algèbre et analyse", "Déterminer la durée d'un placement à taux fixe", 12),

                // --- Terminale professionnelle : géométrie ---
                ("TERMINALE_PRO", "MATH_TPRO_TRIGO_TRIANGLE",  "Géométrie", "Résoudre un triangle quelconque", 13),
                ("TERMINALE_PRO", "MATH_TPRO_VECT_SCALAIRE",   "Géométrie", "Utiliser le produit scalaire de deux vecteurs", 14),

                // --- Terminale professionnelle : algorithmique et logique ---
                ("TERMINALE_PRO", "MATH_TPRO_ALGO",            "Algorithmique et programmation", "Écrire un programme pour résoudre un problème", 15),
                ("TERMINALE_PRO", "MATH_TPRO_LOGIQUE",         "Algorithmique et programmation", "Employer le vocabulaire ensembliste et les connecteurs logiques", 16),

                // --- Première technologique — arrêté du 26/02/2026, NOR MENE2602918A ---
                // Reprise des « Capacités attendues » de l'annexe. Codes anciens
                // gardés quand la notion est la même. Retirés, absents du texte :
                // MATH_1TEC_POLY_CANONIQUE (aucune occurrence de « canonique ») et
                // MATH_1TEC_POLY_EQUATION (« le calcul des racines à l'aide du
                // discriminant ne figure pas au programme »). MATH_1TEC_VA_ESPERANCE
                // perd la variance et l'écart type, hors programme.
                //
                // L'épreuve anticipée (note MENE2515469N) ne porte que sur les
                // domaines communs : ni « Algorithmique et programmation » ni
                // « Activités géométriques » (STD2A). La base ne connaît pas la
                // série : les deux blocs sont gardés pour tous.

                // --- Première techno : vocabulaire ensembliste et logique ---
                ("PREMIERE_TECHNO", "MATH_1TEC_ENS_NOTATIONS",      "Vocabulaire ensembliste et logique", "Utiliser appartenance, inclusion, réunion, intersection, complémentaire et cardinal d'un ensemble fini", 1),
                ("PREMIERE_TECHNO", "MATH_1TEC_LOGIQUE",            "Vocabulaire ensembliste et logique", "Utiliser correctement les connecteurs logiques « et », « ou »", 2),
                ("PREMIERE_TECHNO", "MATH_1TEC_LOG_STATUT",         "Vocabulaire ensembliste et logique", "Identifier le statut d'une égalité et des lettres : variable, indéterminée, inconnue, paramètre", 3),
                ("PREMIERE_TECHNO", "MATH_1TEC_LOG_CONTRE_EXEMPLE", "Vocabulaire ensembliste et logique", "Infirmer une proposition universelle à l'aide d'un contre-exemple", 4),
                ("PREMIERE_TECHNO", "MATH_1TEC_LOG_RECIPROQUE",     "Vocabulaire ensembliste et logique", "Distinguer une proposition de sa réciproque et de sa contraposée", 5),
                ("PREMIERE_TECHNO", "MATH_1TEC_LOG_NECESSAIRE",     "Vocabulaire ensembliste et logique", "Employer condition nécessaire, condition suffisante et équivalence logique", 6),

                // --- Première techno : algorithmique et programmation (toutes séries sauf STD2A ; hors épreuve anticipée) ---
                ("PREMIERE_TECHNO", "MATH_1TEC_ALGO_BERNOULLI",     "Algorithmique et programmation", "Simuler une loi de Bernoulli de paramètre p avec un générateur de nombres aléatoires entre 0 et 1", 7),
                ("PREMIERE_TECHNO", "MATH_1TEC_ALGO_COMPTEUR",      "Algorithmique et programmation", "Utiliser un compteur et un accumulateur pour calculer une somme ou un produit", 8),
                ("PREMIERE_TECHNO", "MATH_1TEC_ALGO_FONCTION",      "Algorithmique et programmation", "Identifier les entrées et sorties d'une fonction et structurer un programme en fonctions", 9),
                ("PREMIERE_TECHNO", "MATH_1TEC_ALGO_LISTE",         "Algorithmique et programmation", "Générer une liste en extension, par ajouts successifs ou en compréhension", 10),
                ("PREMIERE_TECHNO", "MATH_1TEC_ALGO_LISTE_MANIP",   "Algorithmique et programmation", "Manipuler les éléments d'une liste et leurs indices, et itérer sur ses éléments", 11),
                ("PREMIERE_TECHNO", "MATH_1TEC_ALGO_DONNEES",       "Algorithmique et programmation", "Traiter un fichier de données réelles pour en extraire une information et l'analyser", 12),
                ("PREMIERE_TECHNO", "MATH_1TEC_ALGO_TABLEAU_CROISE","Algorithmique et programmation", "Réaliser un tableau croisé de données sur deux critères à partir de données brutes", 13),

                // --- Première techno : activités géométriques (série STD2A seulement ; hors épreuve anticipée) ---
                ("PREMIERE_TECHNO", "MATH_1TEC_GEO_POLYGONES",      "Activités géométriques", "Construire un polygone régulier par un motif et des transformations, et calculer ses grandeurs", 14),
                ("PREMIERE_TECHNO", "MATH_1TEC_GEO_FRISES",         "Activités géométriques", "Analyser une frise ou un pavage et en rechercher un motif élémentaire", 15),
                ("PREMIERE_TECHNO", "MATH_1TEC_GEO_REPERE_ESPACE",  "Activités géométriques", "Repérer un point dans un repère orthonormal de l'espace et calculer une distance", 16),
                ("PREMIERE_TECHNO", "MATH_1TEC_GEO_PERSPECTIVE",    "Activités géométriques", "Représenter un objet en perspective cavalière à partir d'un quadrillage ou d'un cube", 17),
                ("PREMIERE_TECHNO", "MATH_1TEC_GEO_SECTIONS",       "Activités géométriques", "Construire et représenter les sections planes d'un cube ou d'un cylindre de révolution", 18),
                ("PREMIERE_TECHNO", "MATH_1TEC_GEO_ELLIPSE",        "Activités géométriques", "Construire l'image perspective d'un cercle et un parallélogramme circonscrit à une ellipse", 19),

                // --- Première techno : automatismes (QCM de l'épreuve anticipée) ---
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_TAUX_APPLIQUER", "Automatismes", "Appliquer un taux d'évolution pour calculer une valeur finale ou initiale", 20),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_EVOLUTION",     "Automatismes", "Calculer un taux d'évolution et l'exprimer en pourcentage", 21),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_TAUX_SUCCESSIFS","Automatismes", "Calculer le taux d'évolution équivalent à plusieurs évolutions successives", 22),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_TAUX_RECIPROQUE","Automatismes", "Calculer un taux d'évolution réciproque", 23),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_PRODUIT_NUL",   "Automatismes", "Résoudre une équation produit nul", 24),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_SIGNE",         "Automatismes", "Déterminer le signe d'une expression du premier degré ou d'une expression factorisée du second degré", 25),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_ALGEBRE",       "Automatismes", "Développer, factoriser et réduire une expression algébrique simple", 26),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_GRAPHIQUE",     "Automatismes", "Résoudre graphiquement une équation ou une inéquation du type f(x) = k ou f(x) < k", 27),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_GRAPH_SIGNE",   "Automatismes", "Déterminer graphiquement le signe d'une fonction ou son tableau de variations", 28),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_DROITE_TRACER", "Automatismes", "Tracer une droite donnée par son équation réduite ou par un point et son coefficient directeur", 29),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_DROITE_LIRE",   "Automatismes", "Lire graphiquement l'équation réduite d'une droite", 30),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_COEFF_DIRECTEUR","Automatismes", "Déterminer le coefficient directeur d'une droite à partir des coordonnées de deux de ses points", 31),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_STAT_LIRE",     "Automatismes", "Lire un graphique, un histogramme, un diagramme en barres, circulaire ou en boîte (origine, unités, échelles)", 32),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_STAT_DONNEES",  "Automatismes", "Passer d'un graphique aux données et inversement", 33),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_STAT_INDIC",    "Automatismes", "Calculer et interpréter les indicateurs statistiques d'une série", 34),
                ("PREMIERE_TECHNO", "MATH_1TEC_PROB_CONDITION",     "Automatismes", "Calculer une probabilité conditionnelle à partir d'un tableau croisé d'effectifs ou d'un arbre pondéré", 35),
                ("PREMIERE_TECHNO", "MATH_1TEC_AUTO_PROBA_NOTATIONS","Automatismes", "Distinguer P(A ∩ B), P_A(B) et P_B(A)", 36),

                // --- Première techno : analyse — suites numériques ---
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_MODELISER",    "Analyse", "Modéliser une situation à l'aide d'une suite", 37),
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_RECONNAITRE",  "Analyse", "Reconnaître si une situation relève d'un modèle discret de variation linéaire ou exponentielle", 38),
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_TERME",        "Analyse", "Calculer un terme de rang donné d'une suite définie par une relation fonctionnelle ou de récurrence", 39),
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_GRAPHIQUE",    "Analyse", "Réaliser et exploiter la représentation graphique des termes d'une suite", 40),
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_CONJECTURE",   "Analyse", "Conjecturer graphiquement qu'une suite est arithmétique ou géométrique", 41),
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_DEMONTRER",    "Analyse", "Démontrer qu'une suite est arithmétique ou géométrique", 42),
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_VARIATION",    "Analyse", "Déterminer le sens de variation d'une suite arithmétique ou géométrique à l'aide de sa raison", 43),
                ("PREMIERE_TECHNO", "MATH_1TEC_SUITE_ALGO",         "Analyse", "Calculer par programme un terme, une somme finie ou le rang où les termes franchissent un seuil", 44),

                // --- Première techno : analyse — fonctions de la variable réelle ---
                ("PREMIERE_TECHNO", "MATH_1TEC_FONC_MODELISER",     "Analyse", "Modéliser la dépendance entre deux grandeurs à l'aide d'une fonction", 45),
                ("PREMIERE_TECHNO", "MATH_1TEC_FONC_TAUX",          "Analyse", "Calculer un taux de variation et l'interpréter comme pente d'une sécante", 46),
                ("PREMIERE_TECHNO", "MATH_1TEC_FONC_MONOTONIE",     "Analyse", "Relier la monotonie d'une fonction sur un intervalle au signe de ses taux de variation", 47),
                ("PREMIERE_TECHNO", "MATH_1TEC_PARABOLE_ASSOCIER",  "Analyse", "Associer une parabole à une expression de la forme ax², ax² + c ou a(x – x1)(x – x2)", 48),
                ("PREMIERE_TECHNO", "MATH_1TEC_PARABOLE_ELEMENTS",  "Analyse", "Déterminer sans formule l'axe de symétrie, le sommet et les variations de x ↦ ax² + bx + c", 49),
                ("PREMIERE_TECHNO", "MATH_1TEC_POLY_FACTORISER",    "Analyse", "Vérifier qu'une valeur est racine d'un polynôme de degré 2 et le factoriser connaissant une racine", 50),
                ("PREMIERE_TECHNO", "MATH_1TEC_POLY_RACINES",       "Analyse", "Trouver les racines et le signe d'un polynôme de degré 2 donné sous forme factorisée", 51),
                ("PREMIERE_TECHNO", "MATH_1TEC_FONC_BALAYAGE",      "Analyse", "Calculer par balayage une valeur approchée d'une solution d'équation", 52),

                // --- Première techno : analyse — dérivation ---
                ("PREMIERE_TECHNO", "MATH_1TEC_DERIV_NOMBRE",       "Analyse", "Interpréter le nombre dérivé comme coefficient directeur de la tangente", 53),
                ("PREMIERE_TECHNO", "MATH_1TEC_DERIV_TANGENTE",     "Analyse", "Construire la tangente à une courbe en un point et déterminer son équation réduite", 54),
                ("PREMIERE_TECHNO", "MATH_1TEC_DERIV_CALCULER",     "Analyse", "Calculer la dérivée d'une fonction polynôme de degré inférieur ou égal à 3", 55),
                ("PREMIERE_TECHNO", "MATH_1TEC_DERIV_VARIATIONS",   "Analyse", "Déterminer le sens de variation d'une fonction polynôme de degré au plus 3 par le signe de sa dérivée", 56),
                ("PREMIERE_TECHNO", "MATH_1TEC_DERIV_EXTREMUM",     "Analyse", "Déterminer les extrémums d'une fonction polynôme de degré au plus 3 et les interpréter", 57),

                // --- Première techno : statistiques — séries à deux variables quantitatives ---
                ("PREMIERE_TECHNO", "MATH_1TEC_STAT_NUAGE",         "Statistiques et probabilités", "Représenter le nuage de points d'une série statistique à deux variables quantitatives", 58),
                ("PREMIERE_TECHNO", "MATH_1TEC_STAT_POINT_MOYEN",   "Statistiques et probabilités", "Calculer les coordonnées du point moyen d'un nuage", 59),
                ("PREMIERE_TECHNO", "MATH_1TEC_STAT_INTERPOLER",    "Statistiques et probabilités", "Déterminer un ajustement affine et l'utiliser pour interpoler ou extrapoler", 60),
                ("PREMIERE_TECHNO", "MATH_1TEC_STAT_PERTINENCE",    "Statistiques et probabilités", "Juger la pertinence d'un ajustement affine et les limites d'une extrapolation", 61),

                // --- Première techno : probabilités ---
                ("PREMIERE_TECHNO", "MATH_1TEC_PROB_INDEPENDANCE",  "Statistiques et probabilités", "Utiliser ou justifier l'indépendance de deux événements", 62),
                ("PREMIERE_TECHNO", "MATH_1TEC_PROB_TOTALES",       "Statistiques et probabilités", "Calculer une probabilité à l'aide de la formule des probabilités totales", 63),
                ("PREMIERE_TECHNO", "MATH_1TEC_PROB_REPETITION",    "Statistiques et probabilités", "Représenter par un arbre n ≤ 4 épreuves de Bernoulli identiques et indépendantes et calculer", 64),
                ("PREMIERE_TECHNO", "MATH_1TEC_VA_LOI",             "Statistiques et probabilités", "Déterminer la loi d'une variable aléatoire discrète, interpréter {X = a}, {X ≤ a} et calculer leurs probabilités", 65),
                ("PREMIERE_TECHNO", "MATH_1TEC_VA_ESPERANCE",       "Statistiques et probabilités", "Calculer et interpréter en contexte l'espérance d'une variable aléatoire discrète", 66),
                ("PREMIERE_TECHNO", "MATH_1TEC_VA_BERNOULLI",       "Statistiques et probabilités", "Reconnaître une situation aléatoire modélisée par une loi de Bernoulli", 67),
                ("PREMIERE_TECHNO", "MATH_1TEC_ECH_SIMULER",        "Statistiques et probabilités", "Simuler N échantillons de taille n d'une loi de Bernoulli et représenter les fréquences observées", 68),
                ("PREMIERE_TECHNO", "MATH_1TEC_ECH_DISTANCE",       "Statistiques et probabilités", "Interpréter la distance à p de la fréquence observée des 1 dans un échantillon de taille n", 69),

                // --- Terminale technologique : analyse ---
                ("TERMINALE_TECHNO", "MATH_TTEC_SUITE_ARITH",     "Analyse", "Étudier une suite arithmétique et calculer la somme de ses termes", 1),
                ("TERMINALE_TECHNO", "MATH_TTEC_SUITE_GEO",       "Analyse", "Étudier une suite géométrique à termes positifs", 2),
                ("TERMINALE_TECHNO", "MATH_TTEC_SUITE_SEUIL",     "Analyse", "Résoudre un problème de seuil à l'aide d'une suite", 3),
                ("TERMINALE_TECHNO", "MATH_TTEC_EXPO_VARIATION",  "Analyse", "Déterminer le sens de variation d'une fonction exponentielle de base a", 4),
                ("TERMINALE_TECHNO", "MATH_TTEC_EXPO_ALGEBRE",    "Analyse", "Utiliser les propriétés algébriques des fonctions exponentielles", 5),
                ("TERMINALE_TECHNO", "MATH_TTEC_EXPO_TAUX_MOYEN", "Analyse", "Calculer un taux d'évolution moyen équivalent à des évolutions successives", 6),
                ("TERMINALE_TECHNO", "MATH_TTEC_LOG_DEFINITION",  "Analyse", "Utiliser le logarithme décimal et son sens de variation", 7),
                ("TERMINALE_TECHNO", "MATH_TTEC_LOG_EQUATION",    "Analyse", "Résoudre une équation ou une inéquation à l'aide du logarithme décimal", 8),
                ("TERMINALE_TECHNO", "MATH_TTEC_LOG_ALGEBRE",     "Analyse", "Utiliser les propriétés algébriques du logarithme décimal", 9),
                ("TERMINALE_TECHNO", "MATH_TTEC_INVERSE",         "Analyse", "Étudier la fonction inverse et l'utiliser dans un problème", 10),

                // --- Terminale technologique : statistique et probabilités ---
                ("TERMINALE_TECHNO", "MATH_TTEC_STAT_NUAGE",      "Statistiques et probabilités", "Représenter un nuage de points et rechercher un ajustement pertinent", 11),
                ("TERMINALE_TECHNO", "MATH_TTEC_STAT_AJUSTEMENT", "Statistiques et probabilités", "Interpoler ou extrapoler à partir d'un ajustement, affine ou non", 12),
                ("TERMINALE_TECHNO", "MATH_TTEC_PROB_CONDITION",  "Statistiques et probabilités", "Calculer une probabilité conditionnelle", 13),
                ("TERMINALE_TECHNO", "MATH_TTEC_PROB_INDEPEND",   "Statistiques et probabilités", "Reconnaître l'indépendance de deux événements", 14),
                ("TERMINALE_TECHNO", "MATH_TTEC_VA_LOI",          "Statistiques et probabilités", "Déterminer la loi d'une variable aléatoire discrète finie", 15),
                ("TERMINALE_TECHNO", "MATH_TTEC_VA_BINOMIALE",    "Statistiques et probabilités", "Reconnaître et utiliser une loi binomiale", 16),
                ("TERMINALE_TECHNO", "MATH_TTEC_VA_ESPERANCE",    "Statistiques et probabilités", "Calculer l'espérance et l'écart type d'une loi binomiale", 17),

                // --- Terminale technologique : algorithmique et automatismes ---
                ("TERMINALE_TECHNO", "MATH_TTEC_ALGO_FONCTION",   "Algorithmique et programmation", "Structurer un programme à l'aide de fonctions", 18),
                ("TERMINALE_TECHNO", "MATH_TTEC_ALGO_LISTE",      "Algorithmique et programmation", "Générer, parcourir et manipuler une liste", 19),
                ("TERMINALE_TECHNO", "MATH_TTEC_ALGO_DONNEES",    "Algorithmique et programmation", "Traiter un fichier de données pour en extraire une information", 20),
                ("TERMINALE_TECHNO", "MATH_TTEC_AUTO_PROPORTION", "Automatismes", "Calculer et exprimer une proportion sous ses différentes formes", 21),
                ("TERMINALE_TECHNO", "MATH_TTEC_AUTO_EVOLUTION",  "Automatismes", "Passer d'une formulation additive à une formulation multiplicative d'une évolution", 22),
                ("TERMINALE_TECHNO", "MATH_TTEC_LOGIQUE",         "Automatismes", "Employer le vocabulaire ensembliste et les connecteurs logiques", 23),
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
