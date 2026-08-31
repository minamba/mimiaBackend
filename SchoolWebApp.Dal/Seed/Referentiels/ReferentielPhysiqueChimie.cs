namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel de physique-chimie, de la cinquième à la terminale.
    ///
    /// LE COLLÈGE EST EN SPIRALE, PAS EN CHAPITRES
    /// ------------------------------------------
    /// Les quatre thèmes — matière, mouvements, énergie, signaux — reviennent
    /// chaque année du cycle 4, à un niveau d'exigence croissant. Un même
    /// intitulé de chapitre couvre donc des gestes différents en 5e et en 3e,
    /// et c'est le libellé qui porte la différence : « reconnaître » en 5e,
    /// « calculer » en 4e, « exploiter » en 3e.
    ///
    /// AU LYCÉE, C'EST UNE SPÉCIALITÉ
    /// ------------------------------
    /// La seconde est de tronc commun ; en première et en terminale, la
    /// physique-chimie n'est suivie que par les élèves qui l'ont choisie, et le
    /// niveau d'exigence change d'un cran. Le référentiel suit le programme de
    /// spécialité, qui est celui sur lequel ils sont évalués au baccalauréat.
    /// </summary>
    public static class ReferentielPhysiqueChimie
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- 5e ---
                ("CINQUIEME", "PC_5E_MAT_ETATS",     "Organisation de la matière", "Décrire les trois états de la matière à l'échelle des molécules", 1),
                ("CINQUIEME", "PC_5E_MAT_MASSE",     "Organisation de la matière", "Montrer que la masse se conserve lors d'un changement d'état", 2),
                ("CINQUIEME", "PC_5E_MAT_MELANGES",  "Organisation de la matière", "Distinguer mélange homogène et hétérogène et séparer leurs constituants", 3),
                ("CINQUIEME", "PC_5E_MAT_SOLUBILITE","Organisation de la matière", "Distinguer dissolution et fusion", 4),
                ("CINQUIEME", "PC_5E_MAT_VOLUME",    "Organisation de la matière", "Mesurer un volume et une masse et les exprimer dans la bonne unité", 5),
                ("CINQUIEME", "PC_5E_MOUV_RELATIF",  "Mouvements et interactions", "Décrire un mouvement en précisant le référentiel choisi", 6),
                ("CINQUIEME", "PC_5E_MOUV_TRAJECTOIRE","Mouvements et interactions", "Caractériser une trajectoire et une vitesse", 7),
                ("CINQUIEME", "PC_5E_ENER_SOURCES",  "L'énergie", "Identifier les sources, les formes et les transferts d'énergie", 8),
                ("CINQUIEME", "PC_5E_ENER_CIRCUIT",  "L'énergie", "Réaliser et schématiser un circuit en série et en dérivation", 9),
                ("CINQUIEME", "PC_5E_ENER_COURT",    "L'énergie", "Reconnaître un court-circuit et expliquer son danger", 10),
                ("CINQUIEME", "PC_5E_SIG_LUMIERE",   "Signaux", "Décrire la propagation rectiligne de la lumière et la formation des ombres", 11),
                ("CINQUIEME", "PC_5E_SIG_SOURCES",   "Signaux", "Distinguer une source primaire d'un objet diffusant", 12),
                ("CINQUIEME", "PC_5E_METH_SCHEMA",   "Méthode", "Réaliser un schéma normalisé d'expérience ou de circuit", 13),
                ("CINQUIEME", "PC_5E_METH_UNITES",   "Méthode", "Accompagner tout résultat de son unité", 14),
                ("CINQUIEME", "PC_5E_METH_SECURITE", "Méthode", "Appliquer les règles de sécurité au laboratoire", 15),

                // --- 4e ---
                ("QUATRIEME", "PC_4E_MAT_ATOMES",    "Organisation de la matière", "Décrire la matière en atomes et en molécules", 1),
                ("QUATRIEME", "PC_4E_MAT_FORMULES",  "Organisation de la matière", "Lire et écrire la formule d'une molécule courante", 2),
                ("QUATRIEME", "PC_4E_MAT_TRANSFO",   "Organisation de la matière", "Distinguer transformation physique et transformation chimique", 3),
                ("QUATRIEME", "PC_4E_MAT_EQUATION",  "Organisation de la matière", "Écrire et équilibrer une équation de réaction simple", 4),
                ("QUATRIEME", "PC_4E_MAT_MASSE_VOLUMIQUE","Organisation de la matière", "Calculer une masse volumique et l'utiliser pour identifier un matériau", 5),
                ("QUATRIEME", "PC_4E_MOUV_VITESSE",  "Mouvements et interactions", "Calculer une vitesse moyenne et convertir les unités", 6),
                ("QUATRIEME", "PC_4E_MOUV_FORCES",   "Mouvements et interactions", "Représenter une force par une flèche et en donner les caractéristiques", 7),
                ("QUATRIEME", "PC_4E_MOUV_POIDS",    "Mouvements et interactions", "Distinguer masse et poids et calculer P = m × g", 8),
                ("QUATRIEME", "PC_4E_ENER_TENSION",  "L'énergie", "Mesurer une tension et une intensité avec le bon appareil", 9),
                ("QUATRIEME", "PC_4E_ENER_LOIS",     "L'énergie", "Appliquer les lois des tensions et des intensités dans un circuit", 10),
                ("QUATRIEME", "PC_4E_ENER_OHM",      "L'énergie", "Utiliser la loi d'Ohm", 11),
                ("QUATRIEME", "PC_4E_ENER_PUISSANCE","L'énergie", "Relier puissance, énergie et durée", 12),
                ("QUATRIEME", "PC_4E_SIG_SON",       "Signaux", "Décrire la propagation d'un son et calculer sa vitesse", 13),
                ("QUATRIEME", "PC_4E_SIG_FREQUENCE", "Signaux", "Relier hauteur d'un son et fréquence", 14),
                ("QUATRIEME", "PC_4E_METH_TABLEAU",  "Méthode", "Exploiter un tableau de mesures pour établir une relation", 15),
                ("QUATRIEME", "PC_4E_METH_CONVERSION","Méthode", "Convertir avant de calculer, jamais après", 16),

                // --- 3e ---
                ("TROISIEME", "PC_3E_MAT_IONS",      "Organisation de la matière", "Décrire la structure d'un atome et la formation d'un ion", 1),
                ("TROISIEME", "PC_3E_MAT_PH",        "Organisation de la matière", "Relier le pH d'une solution à son caractère acide ou basique", 2),
                ("TROISIEME", "PC_3E_MAT_TESTS",     "Organisation de la matière", "Identifier un ion par un test caractéristique", 3),
                ("TROISIEME", "PC_3E_MAT_METAUX",    "Organisation de la matière", "Interpréter la réaction d'un métal avec un acide", 4),
                ("TROISIEME", "PC_3E_MAT_CONCENTRATION","Organisation de la matière", "Calculer une concentration en masse et préparer une solution", 5),
                ("TROISIEME", "PC_3E_MOUV_RELATIF",  "Mouvements et interactions", "Analyser un mouvement dans deux référentiels différents", 6),
                ("TROISIEME", "PC_3E_MOUV_GRAVITATION","Mouvements et interactions", "Décrire l'interaction gravitationnelle entre deux corps", 7),
                ("TROISIEME", "PC_3E_MOUV_BILAN",    "Mouvements et interactions", "Réaliser un bilan des forces et prévoir l'effet sur le mouvement", 8),
                ("TROISIEME", "PC_3E_ENER_FORMES",   "L'énergie", "Calculer une énergie cinétique et une énergie de position", 9),
                ("TROISIEME", "PC_3E_ENER_CONSERVATION","L'énergie", "Établir un bilan d'énergie et repérer les pertes", 10),
                ("TROISIEME", "PC_3E_ENER_CENTRALE", "L'énergie", "Décrire les conversions d'énergie dans une centrale électrique", 11),
                ("TROISIEME", "PC_3E_ENER_CONSOMMATION","L'énergie", "Calculer l'énergie consommée par un appareil et son coût", 12),
                ("TROISIEME", "PC_3E_SIG_LUMIERE",   "Signaux", "Relier couleur, longueur d'onde et spectre", 13),
                ("TROISIEME", "PC_3E_SIG_TRANSMISSION","Signaux", "Décrire la transmission d'un signal et sa vitesse", 14),
                ("TROISIEME", "PC_3E_METH_GRAPHIQUE","Méthode", "Tracer un graphique et déterminer si deux grandeurs sont proportionnelles", 15),
                ("TROISIEME", "PC_3E_METH_RESOUDRE", "Méthode", "Identifier les données, l'inconnue et la relation qui les lie", 16),
                ("TROISIEME", "PC_3E_METH_ORDRE",    "Méthode", "Vérifier la vraisemblance d'un résultat par son ordre de grandeur", 17),

                // --- Seconde : tronc commun ---
                ("SECONDE", "PC_2DE_CHIM_ESPECES",   "Constitution de la matière", "Identifier une espèce chimique et distinguer corps pur et mélange", 1),
                ("SECONDE", "PC_2DE_CHIM_ENTITES",   "Constitution de la matière", "Décrire un atome, un ion et une molécule à partir du tableau périodique", 2),
                ("SECONDE", "PC_2DE_CHIM_MOLE",      "Constitution de la matière", "Utiliser la quantité de matière et la constante d'Avogadro", 3),
                ("SECONDE", "PC_2DE_CHIM_CONCENTRATION","Constitution de la matière", "Calculer une concentration molaire et préparer une solution par dilution", 4),
                ("SECONDE", "PC_2DE_CHIM_TRANSFO",   "Transformations de la matière", "Écrire l'équation d'une réaction et identifier le réactif limitant", 5),
                ("SECONDE", "PC_2DE_CHIM_TABLEAU",   "Transformations de la matière", "Construire un tableau d'avancement", 6),
                ("SECONDE", "PC_2DE_MOUV_VITESSE",   "Mouvement et interactions", "Déterminer un vecteur vitesse à partir d'une chronophotographie", 7),
                ("SECONDE", "PC_2DE_MOUV_FORCES",    "Mouvement et interactions", "Modéliser une action par une force et faire un bilan", 8),
                ("SECONDE", "PC_2DE_MOUV_INERTIE",   "Mouvement et interactions", "Appliquer le principe d'inertie", 9),
                ("SECONDE", "PC_2DE_MOUV_GRAVITATION","Mouvement et interactions", "Utiliser la loi de la gravitation universelle", 10),
                ("SECONDE", "PC_2DE_SIG_SON",        "Ondes et signaux", "Relier hauteur, fréquence et période d'un son", 11),
                ("SECONDE", "PC_2DE_SIG_LENTILLE",   "Ondes et signaux", "Construire l'image donnée par une lentille convergente", 12),
                ("SECONDE", "PC_2DE_SIG_REFRACTION", "Ondes et signaux", "Appliquer les lois de la réfraction", 13),
                ("SECONDE", "PC_2DE_SIG_ELECTRIQUE", "Ondes et signaux", "Exploiter la loi d'Ohm et les lois des circuits", 14),
                ("SECONDE", "PC_2DE_METH_INCERTITUDE","Méthode", "Exprimer un résultat avec le bon nombre de chiffres significatifs", 15),
                ("SECONDE", "PC_2DE_METH_HOMOGENEITE","Méthode", "Contrôler l'homogénéité d'une relation par ses unités", 16),
                ("SECONDE", "PC_2DE_METH_MODELISER", "Méthode", "Choisir le modèle adapté à la situation et en dire les limites", 17),
                ("SECONDE", "PC_2DE_METH_PROTOCOLE", "Méthode", "Concevoir et mettre en œuvre un protocole expérimental", 18),

                // --- Première : spécialité ---
                ("PREMIERE", "PC_1RE_CHIM_SUIVI",    "Constitution de la matière", "Suivre l'évolution d'une transformation par une grandeur physique", 1),
                ("PREMIERE", "PC_1RE_CHIM_DOSAGE",   "Constitution de la matière", "Réaliser et exploiter un titrage colorimétrique", 2),
                ("PREMIERE", "PC_1RE_CHIM_COHESION", "Constitution de la matière", "Relier la cohésion d'un solide aux interactions entre entités", 3),
                ("PREMIERE", "PC_1RE_CHIM_SOLUBILITE","Constitution de la matière", "Prévoir la solubilité d'une espèce selon la polarité du solvant", 4),
                ("PREMIERE", "PC_1RE_CHIM_ACIDE_BASE","Transformations de la matière", "Identifier un couple acide-base et écrire la réaction associée", 5),
                ("PREMIERE", "PC_1RE_CHIM_OXYDO",    "Transformations de la matière", "Écrire les demi-équations d'une réaction d'oxydoréduction", 6),
                ("PREMIERE", "PC_1RE_CHIM_ENERGIE",  "Transformations de la matière", "Estimer l'énergie libérée par une combustion", 7),
                ("PREMIERE", "PC_1RE_MOUV_VECTEURS", "Mouvement et interactions", "Déterminer les vecteurs vitesse et variation de vitesse", 8),
                ("PREMIERE", "PC_1RE_MOUV_NEWTON",   "Mouvement et interactions", "Relier la variation du vecteur vitesse à la somme des forces", 9),
                ("PREMIERE", "PC_1RE_ENER_TRAVAIL",  "L'énergie", "Calculer le travail d'une force constante", 10),
                ("PREMIERE", "PC_1RE_ENER_MECANIQUE","L'énergie", "Établir un bilan d'énergie mécanique et repérer sa conservation", 11),
                ("PREMIERE", "PC_1RE_ENER_ELECTRIQUE","L'énergie", "Analyser les transferts d'énergie dans un circuit électrique", 12),
                ("PREMIERE", "PC_1RE_ENER_PREMIER",  "L'énergie", "Appliquer le premier principe à un système incompressible", 13),
                ("PREMIERE", "PC_1RE_ONDES_MECANIQUES","Ondes et signaux", "Relier célérité, retard et distance pour une onde mécanique", 14),
                ("PREMIERE", "PC_1RE_ONDES_PERIODIQUES","Ondes et signaux", "Relier longueur d'onde, célérité et fréquence", 15),
                ("PREMIERE", "PC_1RE_ONDES_LUNETTE", "Ondes et signaux", "Modéliser une lunette astronomique et calculer son grossissement", 16),
                ("PREMIERE", "PC_1RE_ONDES_PHOTON",  "Ondes et signaux", "Relier énergie d'un photon et longueur d'onde", 17),
                ("PREMIERE", "PC_1RE_METH_CALCUL",   "Méthode", "Mener un calcul littéral avant l'application numérique", 18),
                ("PREMIERE", "PC_1RE_METH_INCERTITUDE","Méthode", "Évaluer une incertitude et comparer un résultat à une valeur de référence", 19),
                ("PREMIERE", "PC_1RE_METH_PYTHON",   "Méthode", "Exploiter un programme Python de traitement de mesures", 20),

                // --- Terminale : spécialité ---
                ("TERMINALE", "PC_TLE_CHIM_PH",      "Constitution de la matière", "Relier pH, concentration et constante d'acidité", 1),
                ("TERMINALE", "PC_TLE_CHIM_FORCE",   "Constitution de la matière", "Comparer la force de deux acides à partir de leur pKa", 2),
                ("TERMINALE", "PC_TLE_CHIM_DOSAGE",  "Constitution de la matière", "Exploiter un titrage suivi par pH-métrie ou conductimétrie", 3),
                ("TERMINALE", "PC_TLE_CHIM_CINETIQUE","Transformations de la matière", "Déterminer un temps de demi-réaction et une vitesse volumique", 4),
                ("TERMINALE", "PC_TLE_CHIM_MECANISME","Transformations de la matière", "Analyser un mécanisme réactionnel et ses étapes", 5),
                ("TERMINALE", "PC_TLE_CHIM_SPONTANE","Transformations de la matière", "Prévoir le sens d'évolution spontanée d'un système", 6),
                ("TERMINALE", "PC_TLE_CHIM_PILE",    "Transformations de la matière", "Décrire le fonctionnement d'une pile et d'une électrolyse", 7),
                ("TERMINALE", "PC_TLE_CHIM_SYNTHESE","Transformations de la matière", "Optimiser une synthèse organique et calculer un rendement", 8),
                ("TERMINALE", "PC_TLE_MOUV_NEWTON",  "Mouvement et interactions", "Appliquer la deuxième loi de Newton à un système", 9),
                ("TERMINALE", "PC_TLE_MOUV_CHAMP",   "Mouvement et interactions", "Étudier un mouvement dans un champ de pesanteur uniforme", 10),
                ("TERMINALE", "PC_TLE_MOUV_ELECTRIQUE","Mouvement et interactions", "Étudier un mouvement dans un champ électrique uniforme", 11),
                ("TERMINALE", "PC_TLE_MOUV_KEPLER",  "Mouvement et interactions", "Appliquer les lois de Kepler à un mouvement orbital", 12),
                ("TERMINALE", "PC_TLE_ENER_PREMIER", "L'énergie", "Appliquer le premier principe de la thermodynamique", 13),
                ("TERMINALE", "PC_TLE_ENER_THERMIQUE","L'énergie", "Modéliser un transfert thermique et une résistance thermique", 14),
                ("TERMINALE", "PC_TLE_ENER_BILAN",   "L'énergie", "Établir un bilan énergétique sur un système ouvert ou fermé", 15),
                ("TERMINALE", "PC_TLE_ONDES_INTENSITE","Ondes et signaux", "Relier intensité sonore et niveau d'intensité en décibels", 16),
                ("TERMINALE", "PC_TLE_ONDES_DOPPLER","Ondes et signaux", "Exploiter l'effet Doppler pour déterminer une vitesse", 17),
                ("TERMINALE", "PC_TLE_ONDES_DIFFRACTION","Ondes et signaux", "Exploiter la figure de diffraction d'une fente", 18),
                ("TERMINALE", "PC_TLE_ONDES_INTERFERENCES","Ondes et signaux", "Interpréter une figure d'interférences", 19),
                ("TERMINALE", "PC_TLE_ONDES_DUALITE","Ondes et signaux", "Choisir entre modèle ondulatoire et modèle particulaire", 20),
                ("TERMINALE", "PC_TLE_METH_ANALYSE", "Méthode", "Résoudre un problème ouvert en explicitant sa démarche", 21),
                ("TERMINALE", "PC_TLE_METH_PYTHON",  "Méthode", "Écrire ou compléter un programme Python de simulation", 22),
            };

        /// <summary>
        /// Deux dépendances dominent, et elles sortent de la matière :
        ///
        ///   — les MATHS. La physique-chimie est la première matière où une
        ///     lacune en proportionnalité, en conversion d'unités ou en calcul
        ///     littéral devient invisible : l'élève croit ne pas comprendre la
        ///     physique alors qu'il bute sur une division. Ces arêtes-là sont
        ///     celles qui font gagner le plus de temps au diagnostic.
        ///   — la DÉMARCHE de sciences et technologie du cycle 3, qui n'est pas
        ///     réenseignée au collège mais reste attendue.
        /// </summary>
        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // Ce que le cycle 3 a laissé, et sur quoi la 5e s'appuie.
                ("PC_5E_MAT_ETATS",          "SC_6E_MAT_ETATS",          3),
                ("PC_5E_MAT_MELANGES",       "SC_6E_MAT_MELANGES",       3),
                ("PC_5E_MAT_VOLUME",         "SC_6E_MAT_MASSE_VOLUME",   3),
                ("PC_5E_MOUV_TRAJECTOIRE",   "SC_6E_MAT_MOUVEMENT",      3),
                ("PC_5E_ENER_SOURCES",       "SC_6E_ENERGIE_FORMES",     3),
                ("PC_5E_ENER_CIRCUIT",       "SC_6E_ENERGIE_CIRCUIT",    3),
                ("PC_5E_METH_SCHEMA",        "SC_6E_ENERGIE_CIRCUIT",    2),
                ("PC_5E_METH_UNITES",        "SC_6E_DEM_MESURE",         3),
                ("PC_5E_METH_SECURITE",      "SC_6E_ENERGIE_SECURITE",   2),

                // 5e → 4e
                ("PC_4E_MAT_ATOMES",         "PC_5E_MAT_ETATS",          3),
                ("PC_4E_MAT_FORMULES",       "PC_4E_MAT_ATOMES",         3),
                ("PC_4E_MAT_TRANSFO",        "PC_5E_MAT_SOLUBILITE",     2),
                ("PC_4E_MAT_EQUATION",       "PC_4E_MAT_FORMULES",       3),
                ("PC_4E_MAT_EQUATION",       "PC_4E_MAT_TRANSFO",        3),
                ("PC_4E_MAT_MASSE_VOLUMIQUE","PC_5E_MAT_VOLUME",         3),
                ("PC_4E_MOUV_VITESSE",       "PC_5E_MOUV_TRAJECTOIRE",   3),
                ("PC_4E_MOUV_POIDS",         "PC_4E_MOUV_FORCES",        3),
                ("PC_4E_ENER_LOIS",          "PC_4E_ENER_TENSION",       3),
                ("PC_4E_ENER_LOIS",          "PC_5E_ENER_CIRCUIT",       3),
                ("PC_4E_ENER_OHM",           "PC_4E_ENER_LOIS",          3),
                ("PC_4E_ENER_PUISSANCE",     "PC_4E_ENER_TENSION",       2),
                ("PC_4E_SIG_SON",            "PC_4E_MOUV_VITESSE",       2),
                ("PC_4E_METH_CONVERSION",    "PC_5E_METH_UNITES",        3),

                // 4e → 3e
                ("PC_3E_MAT_IONS",           "PC_4E_MAT_ATOMES",         3),
                ("PC_3E_MAT_TESTS",          "PC_3E_MAT_IONS",           3),
                ("PC_3E_MAT_PH",             "PC_3E_MAT_IONS",           2),
                ("PC_3E_MAT_METAUX",         "PC_4E_MAT_EQUATION",       3),
                ("PC_3E_MAT_CONCENTRATION",  "PC_4E_MAT_MASSE_VOLUMIQUE",3),
                ("PC_3E_MOUV_BILAN",         "PC_4E_MOUV_FORCES",        3),
                ("PC_3E_MOUV_GRAVITATION",   "PC_4E_MOUV_POIDS",         3),
                ("PC_3E_ENER_FORMES",        "PC_4E_MOUV_VITESSE",       3),
                ("PC_3E_ENER_CONSERVATION",  "PC_3E_ENER_FORMES",        3),
                ("PC_3E_ENER_CONSOMMATION",  "PC_4E_ENER_PUISSANCE",     3),
                ("PC_3E_METH_GRAPHIQUE",     "PC_4E_METH_TABLEAU",       3),
                ("PC_3E_METH_RESOUDRE",      "PC_4E_METH_CONVERSION",    2),

                // 3e → seconde
                ("PC_2DE_CHIM_ESPECES",      "PC_5E_MAT_MELANGES",       3),
                ("PC_2DE_CHIM_ENTITES",      "PC_3E_MAT_IONS",           3),
                ("PC_2DE_CHIM_MOLE",         "PC_2DE_CHIM_ENTITES",      3),
                ("PC_2DE_CHIM_CONCENTRATION","PC_2DE_CHIM_MOLE",         3),
                ("PC_2DE_CHIM_CONCENTRATION","PC_3E_MAT_CONCENTRATION",  3),
                ("PC_2DE_CHIM_TRANSFO",      "PC_4E_MAT_EQUATION",       3),
                ("PC_2DE_CHIM_TABLEAU",      "PC_2DE_CHIM_TRANSFO",      3),
                ("PC_2DE_CHIM_TABLEAU",      "PC_2DE_CHIM_MOLE",         3),
                ("PC_2DE_MOUV_VITESSE",      "PC_4E_MOUV_VITESSE",       3),
                ("PC_2DE_MOUV_FORCES",       "PC_3E_MOUV_BILAN",         3),
                ("PC_2DE_MOUV_INERTIE",      "PC_2DE_MOUV_FORCES",       3),
                ("PC_2DE_MOUV_GRAVITATION",  "PC_3E_MOUV_GRAVITATION",   3),
                ("PC_2DE_SIG_SON",           "PC_4E_SIG_FREQUENCE",      3),
                ("PC_2DE_SIG_ELECTRIQUE",    "PC_4E_ENER_OHM",           3),
                ("PC_2DE_METH_HOMOGENEITE",  "PC_4E_METH_CONVERSION",    3),
                ("PC_2DE_METH_PROTOCOLE",    "SC_6E_DEM_PROTOCOLE",      2),

                // Seconde → première
                ("PC_1RE_CHIM_DOSAGE",       "PC_2DE_CHIM_TABLEAU",      3),
                ("PC_1RE_CHIM_SUIVI",        "PC_2DE_CHIM_TABLEAU",      3),
                ("PC_1RE_CHIM_COHESION",     "PC_2DE_CHIM_ENTITES",      3),
                ("PC_1RE_CHIM_SOLUBILITE",   "PC_1RE_CHIM_COHESION",     3),
                ("PC_1RE_CHIM_ACIDE_BASE",   "PC_3E_MAT_PH",             2),
                ("PC_1RE_CHIM_OXYDO",        "PC_2DE_CHIM_ENTITES",      3),
                ("PC_1RE_MOUV_VECTEURS",     "PC_2DE_MOUV_VITESSE",      3),
                ("PC_1RE_MOUV_NEWTON",       "PC_2DE_MOUV_INERTIE",      3),
                ("PC_1RE_MOUV_NEWTON",       "PC_1RE_MOUV_VECTEURS",     3),
                ("PC_1RE_ENER_TRAVAIL",      "PC_2DE_MOUV_FORCES",       3),
                ("PC_1RE_ENER_MECANIQUE",    "PC_3E_ENER_CONSERVATION",  3),
                ("PC_1RE_ENER_MECANIQUE",    "PC_1RE_ENER_TRAVAIL",      3),
                ("PC_1RE_ENER_ELECTRIQUE",   "PC_2DE_SIG_ELECTRIQUE",    3),
                ("PC_1RE_ONDES_PERIODIQUES", "PC_2DE_SIG_SON",           3),
                ("PC_1RE_ONDES_LUNETTE",     "PC_2DE_SIG_LENTILLE",      3),
                ("PC_1RE_METH_INCERTITUDE",  "PC_2DE_METH_INCERTITUDE",  3),

                // Première → terminale
                ("PC_TLE_CHIM_PH",           "PC_1RE_CHIM_ACIDE_BASE",   3),
                ("PC_TLE_CHIM_FORCE",        "PC_TLE_CHIM_PH",           3),
                ("PC_TLE_CHIM_DOSAGE",       "PC_1RE_CHIM_DOSAGE",       3),
                ("PC_TLE_CHIM_CINETIQUE",    "PC_1RE_CHIM_SUIVI",        3),
                ("PC_TLE_CHIM_SPONTANE",     "PC_1RE_CHIM_OXYDO",        3),
                ("PC_TLE_CHIM_PILE",         "PC_TLE_CHIM_SPONTANE",     3),
                ("PC_TLE_CHIM_SYNTHESE",     "PC_2DE_CHIM_TABLEAU",      3),
                ("PC_TLE_MOUV_NEWTON",       "PC_1RE_MOUV_NEWTON",       3),
                ("PC_TLE_MOUV_CHAMP",        "PC_TLE_MOUV_NEWTON",       3),
                ("PC_TLE_MOUV_ELECTRIQUE",   "PC_TLE_MOUV_CHAMP",        3),
                ("PC_TLE_MOUV_KEPLER",       "PC_2DE_MOUV_GRAVITATION",  3),
                ("PC_TLE_ENER_PREMIER",      "PC_1RE_ENER_PREMIER",      3),
                ("PC_TLE_ENER_THERMIQUE",    "PC_TLE_ENER_PREMIER",      3),
                ("PC_TLE_ONDES_DOPPLER",     "PC_1RE_ONDES_PERIODIQUES", 3),
                ("PC_TLE_ONDES_DIFFRACTION", "PC_1RE_ONDES_PERIODIQUES", 3),
                ("PC_TLE_ONDES_INTERFERENCES","PC_TLE_ONDES_DIFFRACTION", 3),
                ("PC_TLE_ONDES_DUALITE",     "PC_1RE_ONDES_PHOTON",      3),
                ("PC_TLE_METH_PYTHON",       "PC_1RE_METH_PYTHON",       3),
                ("PC_TLE_METH_ANALYSE",      "PC_3E_METH_RESOUDRE",      2),

                // LES MATHS, cause invisible d'une grande part des blocages.
                ("PC_4E_MOUV_VITESSE",       "MATH_4E_PROP_VITESSE",     3),
                ("PC_4E_MAT_MASSE_VOLUMIQUE","MATH_6E_PROP_COEFF",        3),
                ("PC_4E_METH_CONVERSION",    "MATH_CM1_MES_CONVERSION",   3),
                ("PC_3E_MAT_CONCENTRATION",  "MATH_5E_PROP_POURCENT", 2),
                ("PC_3E_METH_GRAPHIQUE",     "MATH_6E_DATA_GRAPHIQUE",    3),
                ("PC_2DE_CHIM_CONCENTRATION","MATH_4E_LITT_DEVELOPPER",    3),
                ("PC_1RE_METH_CALCUL",       "MATH_4E_EQUATION",         3),
                ("PC_1RE_MOUV_VECTEURS",     "MATH_2DE_VECTEURS",        3),
                ("PC_TLE_MOUV_NEWTON",       "MATH_TLE_DERIV_COMPOSEE",  2),
            };
    }
}
