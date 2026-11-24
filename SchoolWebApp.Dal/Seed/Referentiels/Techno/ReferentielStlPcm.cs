namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// La spécialité « physique-chimie et mathématiques » de la STL, telle que les textes
    /// officiels la définissent — provenance détaillée en tête du fichier. Spécialité PARTAGÉE :
    /// la partie physique-chimie va à la matière « Physique-Chimie », la partie mathématiques à la
    /// matière « Mathématiques ». Réparti entre les matières par `ReferentielsSeriesTechnologiques`.
    /// </summary>
    public static class ReferentielStlPcm
    {
        // =============================================================================
        // RÉFÉRENTIEL DE LA SÉRIE TECHNOLOGIQUE STL (sciences et technologies de laboratoire) —
        // enseignement de spécialité « Physique-chimie et mathématiques » de première et de
        // terminale, commun aux élèves de biotechnologies et de SPCL. Les autres spécialités de la
        // STL (biochimie-biologie, biotechnologies, SPCL) sont dans `ReferentielStl`.
        //
        // PROVENANCE — textes téléchargés et lus le 23/11/2026. Rien n'est écrit de mémoire.
        // Copies locales lues : PDF + .txt extraits par pdftotext -layout (UTF-8), passages
        // mathématiques relus dans le PDF avec PyMuPDF.
        //
        // | Tableau | Niveau        | Intitulé exact (page de titre de l'annexe)                                                                                 | Arrêté / NOR                       | BO                            | En vigueur            | PDF lu |
        // |---------|---------------|-----------------------------------------------------------------------------------------------------------------------------|------------------------------------|-------------------------------|-----------------------|--------|
        // | PCMLP1  | PREMIERE_STL  | « Programme de physique-chimie et mathématiques de première STL » (annexe 3), partie « Programme de physique-chimie »      | arrêté du 17/01/2019, MENE1901645A | BO spécial n° 1 du 22/01/2019 | rentrée 2019 (art. 2) | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/86/4/spe645_annexe3_22-1_1063864.pdf |
        // | PCMLM1  | PREMIERE_STL  | même annexe 3, partie « Programme de mathématiques »                                                                          | arrêté du 17/01/2019, MENE1901645A | BO spécial n° 1 du 22/01/2019 | rentrée 2019 (art. 2) | idem |
        // | PCMLPT  | TERMINALE_STL | « Programme de physique-chimie et mathématiques de terminale STL » (annexe 1), partie « Programme de physique-chimie »    | arrêté du 19/07/2019, MENE1921260A | BO spécial n° 8 du 25/07/2019 | rentrée 2020 (art. 2) | https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/16/3/spe260_annexe1_1159163.pdf |
        // | PCMLMT  | TERMINALE_STL | même annexe 1, partie « Programme de mathématiques »                                                                          | arrêté du 19/07/2019, MENE1921260A | BO spécial n° 8 du 25/07/2019 | rentrée 2020 (art. 2) | idem |
        //
        // Arrêtés lus (copies de l'Internet Archive) : MENE1901645A (art. 1 : programmes de
        // spécialité de première STL fixés par les annexes ; art. 2 : rentrée 2019 ; annexe 3 =
        // physique-chimie et mathématiques) et MENE1921260A (art. 1 : programmes de spécialité de
        // terminale STL ; art. 2 : rentrée 2020 ; annexe 1 = physique-chimie et mathématiques).
        // Chaque annexe identifiée par sa page de titre.
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : aucun texte de 2020 à 2026 ne remplace ces programmes
        // (même constat que pour les autres spécialités STL, voir `ReferentielStl`) ; seules les
        // épreuves du baccalauréat ont changé.
        //
        // SPÉCIALITÉ PARTAGÉE ENTRE DEUX PROFESSEURS : la partie physique-chimie (PCMLP1, PCMLPT) va
        // à la matière « Physique-Chimie », la partie mathématiques (PCMLM1, PCMLMT) à la matière
        // « Mathématiques ». Le texte lui-même sépare les deux programmes ; les « notions
        // mathématiques associées » et « liens avec l'enseignement de physique-chimie » ne sont
        // pas repris.
        //
        // LECTURE : la physique-chimie est écrite en deux colonnes (notions et contenus / capacités
        // exigibles, où sont précisées les capacités expérimentales et numériques) : les libellés
        // viennent de la seconde colonne. Les compétences de la démarche scientifique et les
        // « repères pour l'enseignement » sont laissés au professeur. Domaine = l'un des cinq
        // grands thèmes du texte (Mesure et incertitudes ; Constitution de la matière ;
        // Transformation chimique de la matière en première, Transformation de la matière en
        // terminale ; Mouvements et interactions ; Ondes et signaux en première, Énergie :
        // conversions et transferts en terminale).
        // Les mathématiques sont écrites en « Contenus / Capacités attendues / Commentaires ». Les
        // libellés viennent des capacités attendues ; quand un contenu n'est couvert par aucune
        // capacité (mesure principale, fonctions t ↦ A cos(ωt + φ), formes du produit scalaire,
        // propriétés de l'intégrale, dérivée de l'exponentielle…) ou qu'une « situation
        // algorithmique » est nommée, une ligne en est tirée, signalée par « (contenu) » ou
        // « (algorithmique) » dans le commentaire de bloc. Domaine = la partie du texte (Géométrie
        // dans le plan ; Analyse). Pas de nombres complexes en STL, contrairement à la STI2D.
        //
        // DOUTES DE LECTURE (numéros de ligne des .txt pdftotext), tous levés dans le PDF :
        // - première, lignes 856-858, 963, 1008 : ω et φ perdus (police Symbol, U+F077 et U+F06A
        //   relus par PyMuPDF) → « A cos(ωt + φ) » ; valeurs remarquables π/6, π/4, π/3, π/2
        //   (dénominateurs rejetés ligne 857) ; angles associés π/2 − x, π/2 + x (lignes 869-872).
        // - première, lignes 896-898 et 917 : produit scalaire illisible (θ = U+F071) ; page rendue
        //   en image : « Interpréter ‖u‖ cos(θ) en termes de projection ».
        // - terminale, lignes 856, 890, 952 : caractères mathématiques italiques non décodés
        //   (somme des rectangles, f, x ↦ e^(kx)) ; relus par PyMuPDF, sans effet sur les libellés.
        // Codes : ASCII, <= 40 caractères. Ordre : 1..n dans l'ordre du programme, par tableau.
        //
        // Comptes : PCMLP1 74 · PCMLM1 17 · PCMLPT 75 · PCMLMT 22 · total 188
        // =============================================================================

        // -----------------------------------------------------------------------------
        // 1. PREMIÈRE STL — PHYSIQUE-CHIMIE (matière « Physique-Chimie »)
        //    Mesure et incertitudes ; Constitution de la matière (structure spatiale et propriétés
        //    physiques, solvants et solutés) ; Transformation chimique de la matière (réactions
        //    acido-basiques, cinétique) ; Mouvements et interactions (mouvements, interactions,
        //    aspects énergétiques) ; Ondes et signaux (ondes mécaniques, ondes électromagnétiques).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMLP1 =
        {
            ("PREMIERE_STL", "PCMLP1_MES_SOURCES_ERREURS",       "Mesure et incertitudes", "Identifier les principales sources d'erreurs lors d'une mesure", 1),
            ("PREMIERE_STL", "PCMLP1_MES_JUSTESSE_FIDELITE",     "Mesure et incertitudes", "Exploiter des séries de mesures indépendantes (histogramme, moyenne, écart-type) pour comparer des méthodes en justesse et en fidélité", 2),
            ("PREMIERE_STL", "PCMLP1_MES_TYPE_A",                "Mesure et incertitudes", "Procéder à une évaluation de type A d'une incertitude-type", 3),
            ("PREMIERE_STL", "PCMLP1_MES_TYPE_B",                "Mesure et incertitudes", "Procéder à une évaluation de type B d'une incertitude-type à partir d'une relation fournie ou des notices constructeurs", 4),
            ("PREMIERE_STL", "PCMLP1_MES_EXPRESSION",            "Mesure et incertitudes", "Exprimer un résultat de mesure avec le nombre de chiffres significatifs adaptés et l'incertitude-type associée", 5),
            ("PREMIERE_STL", "PCMLP1_MES_VALIDITE",              "Mesure et incertitudes", "Discuter la validité d'un résultat en comparant son écart à la valeur de référence à l'incertitude-type", 6),
            ("PREMIERE_STL", "PCMLP1_STR_LEWIS",                 "Constitution de la matière", "Interpréter ou établir le schéma de Lewis de molécules ou d'ions (doublets liants et non liants, liaisons doubles et triples)", 7),
            ("PREMIERE_STL", "PCMLP1_STR_VSEPR",                 "Constitution de la matière", "Utiliser la théorie VSEPR pour déterminer la géométrie d'espèces AXnEm (n + m ≤ 4), l'atome central étant donné", 8),
            ("PREMIERE_STL", "PCMLP1_STR_MESOMERIE",             "Constitution de la matière", "Écrire des formes mésomères des ions nitrate et carbonate pour interpréter leur géométrie", 9),
            ("PREMIERE_STL", "PCMLP1_STR_POLARITE",              "Constitution de la matière", "Représenter les charges partielles d'une liaison covalente par l'électronégativité et relier polarité et géométrie d'une molécule", 10),
            ("PREMIERE_STL", "PCMLP1_STR_LIAISONS_INTERMOL",     "Constitution de la matière", "Définir et identifier les liaisons hydrogène et de Van der Waals et comparer leurs énergies à celles des liaisons covalentes", 11),
            ("PREMIERE_STL", "PCMLP1_STR_CHANGEMENT_ETAT",       "Constitution de la matière", "Interpréter ou classer qualitativement températures et énergies de changement d'état d'espèces en comparant leurs structures", 12),
            ("PREMIERE_STL", "PCMLP1_STR_FORMULES_ORGANIQUES",   "Constitution de la matière", "Écrire les formules développées, semi-développées et topologiques de molécules organiques et repérer leurs groupes caractéristiques", 13),
            ("PREMIERE_STL", "PCMLP1_STR_ISOMERIE_CRAM",         "Constitution de la matière", "Identifier des isomères de chaîne, de position ou de fonction et dessiner en Cram des conformères non cycliques", 14),
            ("PREMIERE_STL", "PCMLP1_STR_FONCTIONS_NOMS",        "Constitution de la matière", "Associer les fonctions alcool, aldéhyde, cétone, acide carboxylique, amine à leur groupe et le nom d'une molécule à sa formule", 15),
            ("PREMIERE_STL", "PCMLP1_STR_ACIDE_AMINE_GRAS",      "Constitution de la matière", "Identifier et représenter un acide α-aminé et un acide gras", 16),
            ("PREMIERE_STL", "PCMLP1_STR_ENANTIOMERIE",          "Constitution de la matière", "Identifier un atome de carbone asymétrique, définir l'énantiomérie et dessiner en Cram deux énantiomères", 17),
            ("PREMIERE_STL", "PCMLP1_SOL_ISOTOPES_MASSE_MOL",    "Constitution de la matière", "Déterminer la composition du noyau d'un isotope (A, Z) et la masse molaire d'un élément ou d'une espèce chimique", 18),
            ("PREMIERE_STL", "PCMLP1_SOL_MASSE_QUANTITE",        "Constitution de la matière", "Déterminer une masse par la densité ou la masse volumique et une quantité de matière en tenant compte de la pureté", 19),
            ("PREMIERE_STL", "PCMLP1_SOL_CONCENTRATION",         "Constitution de la matière", "Exploiter la concentration en mol·L⁻¹ d'une espèce dissoute et réaliser une gamme étalon par dilution", 20),
            ("PREMIERE_STL", "PCMLP1_SOL_SOLVANTS",              "Constitution de la matière", "Citer et identifier des solvants polaires et apolaires usuels", 21),
            ("PREMIERE_STL", "PCMLP1_SOL_DISSOLUTION",           "Constitution de la matière", "Décrire par les liaisons soluté-solvant et modéliser par une équation la dissolution d'une espèce ionique ou moléculaire", 22),
            ("PREMIERE_STL", "PCMLP1_SOL_BILAN_PREPARATION",     "Constitution de la matière", "Faire le bilan de matière de la dissolution totale d'un solide ionique et préparer une solution par dissolution ou dilution", 23),
            ("PREMIERE_STL", "PCMLP1_SOL_SOLUBILITE",            "Constitution de la matière", "Définir la solubilité molaire et massique et exploiter des données de solubilité pour établir qu'une solution est saturée", 24),
            ("PREMIERE_STL", "PCMLP1_SOL_SOLUBILITE_STRUCTURE",  "Constitution de la matière", "Relier et comparer les solubilités dans l'eau ou un solvant organique aux structures (hydrophile, lipophile, amphiphile…)", 25),
            ("PREMIERE_STL", "PCMLP1_SOL_INFLUENCE_PH_T",        "Constitution de la matière", "Interpréter l'influence du pH sur la solubilité et étudier expérimentalement celle du pH et de la température", 26),
            ("PREMIERE_STL", "PCMLP1_AB_DEFINITIONS",            "Transformation chimique de la matière", "Définir un acide (donneur de proton) et une base (accepteur de proton) par le schéma de Lewis et les identifier dans un couple", 27),
            ("PREMIERE_STL", "PCMLP1_AB_DILUTION_MESURE_PH",     "Transformation chimique de la matière", "Prévoir le sens d'évolution du pH par dilution, étalonner un pH-mètre et mesurer un pH", 28),
            ("PREMIERE_STL", "PCMLP1_AB_USUELS",                 "Transformation chimique de la matière", "Connaître les formules d'acides et de bases usuels, forts (chlorhydrique, nitrique, soude…) et faibles (éthanoïque, ammoniac…)", 29),
            ("PREMIERE_STL", "PCMLP1_AB_PH_CONCENTRATION",       "Transformation chimique de la matière", "Connaître et utiliser la relation pH = −log([H3O+]) pour estimer un pH ou une concentration en ions H3O+", 30),
            ("PREMIERE_STL", "PCMLP1_AB_REACTION_EAU",           "Transformation chimique de la matière", "Écrire la réaction d'un acide ou d'une base avec l'eau, totale (simple flèche) ou non (double flèche), et recenser les espèces spectatrices", 31),
            ("PREMIERE_STL", "PCMLP1_AB_FORT_FAIBLE",            "Transformation chimique de la matière", "Mesurer le pH d'une solution d'acide ou de base pour en apprécier le caractère fort ou faible", 32),
            ("PREMIERE_STL", "PCMLP1_AB_AUTOPROTOLYSE",          "Transformation chimique de la matière", "Écrire l'autoprotolyse de l'eau et utiliser Ke = [H3O+][HO−] et sa valeur à 25 °C pour le pH de l'eau pure", 33),
            ("PREMIERE_STL", "PCMLP1_AB_PKA_PREDOMINANCE",       "Transformation chimique de la matière", "Définir le pKa d'un couple et identifier l'espèce prédominante selon le pH, notamment pour les acides α-aminés", 34),
            ("PREMIERE_STL", "PCMLP1_AB_TAMPON",                 "Transformation chimique de la matière", "Citer les propriétés d'une solution tampon et la préparer en mélangeant un acide et sa base conjuguée", 35),
            ("PREMIERE_STL", "PCMLP1_CIN_VITESSES",              "Transformation chimique de la matière", "Définir les vitesses de disparition d'un réactif et d'apparition d'un produit et les estimer par le nombre dérivé sur une courbe", 36),
            ("PREMIERE_STL", "PCMLP1_CIN_DEMI_REACTION",         "Transformation chimique de la matière", "Estimer un temps de demi-réaction à partir d'une courbe ou d'un tableau de valeurs (temps, concentration)", 37),
            ("PREMIERE_STL", "PCMLP1_CIN_FACTEURS",              "Transformation chimique de la matière", "Exploiter des données expérimentales pour montrer l'influence de la température ou des concentrations sur la vitesse", 38),
            ("PREMIERE_STL", "PCMLP1_CIN_CATALYSE",              "Transformation chimique de la matière", "Définir un catalyseur, l'identifier dans une transformation et qualifier la catalyse (homogène, hétérogène, enzymatique)", 39),
            ("PREMIERE_STL", "PCMLP1_MVT_REFERENTIEL",           "Mouvements et interactions", "Choisir un référentiel d'étude et estimer des ordres de grandeur de vitesses et d'accélérations de la vie courante", 40),
            ("PREMIERE_STL", "PCMLP1_MVT_VITESSE_DERIVEE",       "Mouvements et interactions", "Relier la vitesse moyenne tirée de mesures de positions à la vitesse associée au nombre dérivé et mesurer une vitesse", 41),
            ("PREMIERE_STL", "PCMLP1_MVT_COORDONNEES",           "Mouvements et interactions", "Citer et exploiter la relation entre les coordonnées de la position et celles du vecteur vitesse", 42),
            ("PREMIERE_STL", "PCMLP1_MVT_ACCELERATION",          "Mouvements et interactions", "Exploiter la relation entre vitesse et accélération dans un mouvement rectiligne à accélération constante", 43),
            ("PREMIERE_STL", "PCMLP1_MVT_INCREMENTS",            "Mouvements et interactions", "Construire une approximation des positions par incréments de temps à partir d'une loi de vitesse et expliquer l'effet de l'incrément", 44),
            ("PREMIERE_STL", "PCMLP1_MVT_OUTIL_NUMERIQUE",       "Mouvements et interactions", "Représenter positions et trajectoire et calculer vitesses ou positions successives avec un tableur ou un programme", 45),
            ("PREMIERE_STL", "PCMLP1_INT_ACTIONS_FORCES",        "Mouvements et interactions", "Identifier et caractériser des actions mécaniques sur un objet, les modéliser par des forces et établir un bilan de forces", 46),
            ("PREMIERE_STL", "PCMLP1_INT_EQUILIBRE",             "Mouvements et interactions", "Effectuer un bilan quantitatif de forces pour un système à l'équilibre ou en mouvement rectiligne uniforme", 47),
            ("PREMIERE_STL", "PCMLP1_INT_VARIATION_VITESSE",     "Mouvements et interactions", "Relier la variation du vecteur vitesse entre deux instants voisins à la somme des forces pour estimer l'une ou l'autre", 48),
            ("PREMIERE_STL", "PCMLP1_INT_NEWTON",                "Mouvements et interactions", "Citer et exploiter la seconde loi de Newton dans le cas d'un mouvement rectiligne", 49),
            ("PREMIERE_STL", "PCMLP1_INT_POIDS_GRAVITATION",     "Mouvements et interactions", "Citer et exploiter l'expression du poids et de la force d'interaction gravitationnelle", 50),
            ("PREMIERE_STL", "PCMLP1_INT_ARCHIMEDE_FROTTEMENT",  "Mouvements et interactions", "Exploiter l'expression de la poussée d'Archimède et des forces de frottement et comparer l'ordre de grandeur des forces", 51),
            ("PREMIERE_STL", "PCMLP1_INT_CHUTE_LIBRE",           "Mouvements et interactions", "Caractériser une chute libre verticale et établir les lois d'évolution de sa vitesse et de sa position en fonction du temps", 52),
            ("PREMIERE_STL", "PCMLP1_INT_CHUTE_FREINEE",         "Mouvements et interactions", "Expliquer par l'expérience l'effet d'un frottement et de la poussée d'Archimède sur une chute verticale, face au modèle de la chute libre", 53),
            ("PREMIERE_STL", "PCMLP1_INT_VISCOSITE",             "Mouvements et interactions", "Mettre en œuvre un protocole pour mesurer une force de frottement fluide et en déduire la viscosité du fluide", 54),
            ("PREMIERE_STL", "PCMLP1_ENM_CINETIQUE_TRAVAIL",     "Mouvements et interactions", "Citer et exploiter les relations de l'énergie cinétique et du travail d'une force constante lors d'un mouvement rectiligne", 55),
            ("PREMIERE_STL", "PCMLP1_ENM_VARIATION_CINETIQUE",   "Mouvements et interactions", "Associer une variation d'énergie cinétique au travail des forces", 56),
            ("PREMIERE_STL", "PCMLP1_ENM_PUISSANCE",             "Mouvements et interactions", "Citer et exploiter la relation entre travail et puissance moyenne", 57),
            ("PREMIERE_STL", "PCMLP1_ENM_ESTIMER_PUISSANCE",     "Mouvements et interactions", "Estimer la puissance moyenne nécessaire pour modifier une vitesse sur une durée donnée ou la maintenir malgré les frottements", 58),
            ("PREMIERE_STL", "PCMLP1_OM_PROGRESSIVES",           "Ondes et signaux", "Citer des exemples d'ondes mécaniques progressives et distinguer onde longitudinale et onde transversale", 59),
            ("PREMIERE_STL", "PCMLP1_OM_RETARD",                 "Ondes et signaux", "Représenter à différents instants l'état d'un système parcouru par une onde et exploiter la relation retard-distance-célérité", 60),
            ("PREMIERE_STL", "PCMLP1_OM_SON_MILIEU",             "Ondes et signaux", "Énoncer qu'un milieu matériel est nécessaire au son et associer une onde sonore à une vibration du milieu et une pression acoustique", 61),
            ("PREMIERE_STL", "PCMLP1_OM_GRANDEURS",              "Ondes et signaux", "Définir célérité, amplitude, période, fréquence et longueur d'onde d'une onde mécanique sinusoïdale et exploiter leur relation", 62),
            ("PREMIERE_STL", "PCMLP1_OM_CELERITE_SON",           "Ondes et signaux", "Citer l'ordre de grandeur de la célérité du son dans un gaz, un liquide et un solide", 63),
            ("PREMIERE_STL", "PCMLP1_OM_MESURES_TEMPS_DE_VOL",   "Ondes et signaux", "Mesurer période, fréquence, longueur d'onde et célérité d'une onde sonore ou ultrasonore et en déduire des distances par temps de vol", 64),
            ("PREMIERE_STL", "PCMLP1_OM_PERCEPTION",             "Ondes et signaux", "Citer le niveau sonore et la fréquence comme grandeurs de la perception d'un son et le domaine des fréquences audibles", 65),
            ("PREMIERE_STL", "PCMLP1_OM_RISQUE_AUDITIF",         "Ondes et signaux", "Exploiter une courbe audiométrique, identifier des situations de risque auditif et mesurer un niveau d'intensité sonore en dB", 66),
            ("PREMIERE_STL", "PCMLP1_OEM_VIDE_CELERITE",         "Ondes et signaux", "Énoncer qu'une onde électromagnétique peut se propager dans le vide et citer la célérité de la lumière dans le vide ou l'air", 67),
            ("PREMIERE_STL", "PCMLP1_OEM_GRANDEURS",             "Ondes et signaux", "Définir les grandeurs d'une onde électromagnétique sinusoïdale et exploiter la relation entre longueur d'onde, célérité et fréquence", 68),
            ("PREMIERE_STL", "PCMLP1_OEM_SPECTRE",               "Ondes et signaux", "Classer les ondes électromagnétiques par fréquence et longueur d'onde dans le vide et citer les limites du spectre visible", 69),
            ("PREMIERE_STL", "PCMLP1_OEM_UTILISATIONS",          "Ondes et signaux", "Citer des domaines d'utilisation des différents types d'ondes électromagnétiques", 70),
            ("PREMIERE_STL", "PCMLP1_OEM_SOURCES_SPECTRES",      "Ondes et signaux", "Caractériser des sources lumineuses par leur spectre et distinguer spectres d'émission ou d'absorption, continus ou de raies", 71),
            ("PREMIERE_STL", "PCMLP1_OEM_OBSERVER_SPECTRES",     "Ondes et signaux", "Mettre en œuvre un protocole pour observer le spectre de sources lumineuses et le spectre d'absorption d'une solution", 72),
            ("PREMIERE_STL", "PCMLP1_OEM_PHOTON",                "Ondes et signaux", "Interpréter les échanges d'énergie lumière-matière par le photon et exploiter la relation entre son énergie et la fréquence", 73),
            ("PREMIERE_STL", "PCMLP1_OEM_RAIES",                 "Ondes et signaux", "Classer les ondes électromagnétiques selon l'énergie du photon et exploiter la présence de raies dans un spectre", 74),
        };

        // -----------------------------------------------------------------------------
        // 2. PREMIÈRE STL — MATHÉMATIQUES (matière « Mathématiques »)
        //    Géométrie dans le plan (trigonométrie, produit scalaire) ; Analyse (dérivées,
        //    primitives). Lignes tirées des contenus : 2, 5, 6, 8, 14 ; de la situation
        //    algorithmique : 17 (aussi capacité attendue).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMLM1 =
        {
            ("PREMIERE_STL", "PCMLM1_TRIGO_RADIAN",              "Géométrie dans le plan", "Effectuer des conversions de degré en radian et de radian en degré", 1),
            ("PREMIERE_STL", "PCMLM1_TRIGO_MESURE_PRINCIPALE",   "Géométrie dans le plan", "Utiliser le cercle trigonométrique et déterminer les mesures et la mesure principale d'un angle orienté", 2),
            ("PREMIERE_STL", "PCMLM1_TRIGO_EQUATIONS",           "Géométrie dans le plan", "Résoudre par lecture sur le cercle trigonométrique des équations du type cos(x) = a et sin(x) = a", 3),
            ("PREMIERE_STL", "PCMLM1_TRIGO_ANGLES_ASSOCIES",     "Géométrie dans le plan", "Connaître et utiliser les relations entre sinus et cosinus des angles associés : −x, π − x, π + x, π/2 − x, π/2 + x", 4),
            ("PREMIERE_STL", "PCMLM1_TRIGO_FONCTIONS_CIRC",      "Géométrie dans le plan", "Utiliser périodicité, parité, variations et valeurs remarquables de sinus et cosinus et justifier les symétries de leurs courbes", 5),
            ("PREMIERE_STL", "PCMLM1_TRIGO_SINUSOIDES",          "Géométrie dans le plan", "Lire amplitude, période et phase à l'origine de t ↦ A cos(ωt + φ) et t ↦ A sin(ωt + φ) et tracer leurs courbes", 6),
            ("PREMIERE_STL", "PCMLM1_PS_PROJECTION",             "Géométrie dans le plan", "Calculer la projection d'un vecteur sur un axe et interpréter ‖u‖ cos(θ) en termes de projection", 7),
            ("PREMIERE_STL", "PCMLM1_PS_CALCUL",                 "Géométrie dans le plan", "Calculer un produit scalaire par sa définition géométrique, par projection ou dans une base orthonormée", 8),
            ("PREMIERE_STL", "PCMLM1_PS_ORTHOGONALITE_ANGLE",    "Géométrie dans le plan", "Utiliser un produit scalaire pour démontrer l'orthogonalité de deux vecteurs ou calculer un angle non orienté", 9),
            ("PREMIERE_STL", "PCMLM1_PS_LONGUEURS",              "Géométrie dans le plan", "Utiliser un produit scalaire pour calculer des longueurs (théorème d'Al-Kashi, égalité du parallélogramme)", 10),
            ("PREMIERE_STL", "PCMLM1_DER_NOTATIONS",             "Analyse", "Utiliser les notations du taux de variation et du nombre dérivé en un point (Δy/Δx, dy/dx, f′(x0))", 11),
            ("PREMIERE_STL", "PCMLM1_DER_APPROX_AFFINE",         "Analyse", "Effectuer des calculs approchés à l'aide de l'approximation affine en un point", 12),
            ("PREMIERE_STL", "PCMLM1_DER_CALCUL",                "Analyse", "Calculer une fonction dérivée : somme, produit, inverse, quotient, xⁿ, 1/x, polynôme, cosinus et sinus", 13),
            ("PREMIERE_STL", "PCMLM1_DER_AFFINE_SINUSOIDE",      "Analyse", "Dériver x ↦ f(ax + b), t ↦ A cos(ωt + φ) et t ↦ A sin(ωt + φ)", 14),
            ("PREMIERE_STL", "PCMLM1_DER_VARIATIONS",            "Analyse", "Étudier le sens de variation d'une fonction", 15),
            ("PREMIERE_STL", "PCMLM1_PRIM_CALCUL",               "Analyse", "Calculer des primitives de polynômes et de t ↦ A cos(ωt + φ), t ↦ A sin(ωt + φ), à une constante près", 16),
            ("PREMIERE_STL", "PCMLM1_PRIM_EULER",                "Analyse", "Construire point par point par la méthode d'Euler une approximation de la solution de y′ = f(t), y(t0) = y0", 17),
        };

        // -----------------------------------------------------------------------------
        // 3. TERMINALE STL — PHYSIQUE-CHIMIE (matière « Physique-Chimie »)
        //    Mesure et incertitudes ; Constitution de la matière (structure spatiale) ;
        //    Transformation de la matière (acide-base, oxydo-réduction, cinétique, radioactivité) ;
        //    Mouvements et interactions (mouvements, interactions) ; Énergie : conversions et
        //    transferts (chaînes énergétiques, énergie mécanique, chimique, électrique, énergie et ondes).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMLPT =
        {
            ("TERMINALE_STL", "PCMLPT_MES_TYPE_A_B",              "Mesure et incertitudes", "Évaluer une incertitude-type par une approche statistique (type A) ou sur une mesure unique (type B, relation ou notice fournie)", 1),
            ("TERMINALE_STL", "PCMLPT_MES_SOURCES_POIDS",         "Mesure et incertitudes", "Identifier qualitativement les principales sources d'erreurs d'une mesure et comparer leur poids à l'aide d'une méthode fournie", 2),
            ("TERMINALE_STL", "PCMLPT_MES_MATERIEL_PROTOCOLE",    "Mesure et incertitudes", "Identifier le matériel adapté à la précision attendue et proposer des améliorations d'un protocole pour diminuer l'incertitude", 3),
            ("TERMINALE_STL", "PCMLPT_MES_COMPOSEE",              "Mesure et incertitudes", "Évaluer, par une relation fournie ou un logiciel, l'incertitude-type d'une mesure soumise à plusieurs sources d'erreurs", 4),
            ("TERMINALE_STL", "PCMLPT_MES_EXPRESSION",            "Mesure et incertitudes", "Exprimer un résultat de mesure avec le nombre de chiffres significatifs adaptés et l'incertitude-type associée", 5),
            ("TERMINALE_STL", "PCMLPT_MES_VALIDER",               "Mesure et incertitudes", "Valider un résultat en évaluant son écart à la valeur de référence en fonction de l'incertitude-type", 6),
            ("TERMINALE_STL", "PCMLPT_MES_JUSTESSE_FIDELITE",     "Mesure et incertitudes", "Exploiter la dispersion de séries de mesures indépendantes pour comparer des protocoles en justesse et en fidélité", 7),
            ("TERMINALE_STL", "PCMLPT_STR_CRAM",                  "Constitution de la matière", "Représenter en perspective de Cram une molécule comportant plusieurs atomes de carbone asymétriques", 8),
            ("TERMINALE_STL", "PCMLPT_STR_CHIRALITE",             "Constitution de la matière", "Définir et repérer une molécule chirale", 9),
            ("TERMINALE_STL", "PCMLPT_STR_STEREOISOMERES",        "Constitution de la matière", "Représenter et identifier des couples d'énantiomères et des diastéréoisomères, sur modèles moléculaires ou par logiciel", 10),
            ("TERMINALE_STL", "PCMLPT_STR_CONFIGURATION",         "Constitution de la matière", "Déterminer la configuration absolue d'un atome de carbone asymétrique (règles CIP, R et S)", 11),
            ("TERMINALE_STL", "PCMLPT_STR_PROPRIETES_BIO",        "Constitution de la matière", "Extraire et exploiter des informations sur les propriétés biologiques de stéréoisomères", 12),
            ("TERMINALE_STL", "PCMLPT_AB_KA_PKA",                 "Transformation de la matière", "Définir la constante d'équilibre acido-basique (constante d'acidité) et le pKa d'un couple acide/base", 13),
            ("TERMINALE_STL", "PCMLPT_AB_DISSOCIATION",           "Transformation de la matière", "Déterminer le coefficient de dissociation d'un acide faible par conservation de la matière et prévoir l'effet de la dilution", 14),
            ("TERMINALE_STL", "PCMLPT_AB_TAMPON",                 "Transformation de la matière", "Choisir le couple acide/base adapté à la préparation d'une solution tampon à partir de valeurs tabulées", 15),
            ("TERMINALE_STL", "PCMLPT_AB_CO2",                    "Transformation de la matière", "Relier la solubilité du dioxyde de carbone dans différents milieux à ses effets (physiologie, environnement)", 16),
            ("TERMINALE_STL", "PCMLPT_AB_PKA_SPECTRO",            "Transformation de la matière", "Mettre en œuvre un protocole montrant par spectrophotométrie l'invariance du pKa d'un couple acide/base", 17),
            ("TERMINALE_STL", "PCMLPT_AB_EXTRACTION",             "Transformation de la matière", "Réaliser une extraction ou une séparation faisant intervenir une espèce acide ou basique", 18),
            ("TERMINALE_STL", "PCMLPT_OXRED_NOMBRE_OXYDATION",    "Transformation de la matière", "Déterminer le nombre d'oxydation d'un élément d'une espèce inorganique et s'en servir pour identifier oxydant et réducteur", 19),
            ("TERMINALE_STL", "PCMLPT_OXRED_COUPLE_DEMI_EQ",      "Transformation de la matière", "Définir oxydant et réducteur d'un couple redox par transfert d'électrons et écrire une équation de demi-réaction", 20),
            ("TERMINALE_STL", "PCMLPT_OXRED_USUELS",              "Transformation de la matière", "Citer et donner la formule d'oxydants ou réducteurs usuels (dihydrogène, dioxygène, dichlore, diiode, eau oxygénée, ion fer(II))", 21),
            ("TERMINALE_STL", "PCMLPT_OXRED_EQUATION",            "Transformation de la matière", "Écrire l'équation d'une réaction d'oxydo-réduction en milieu acide", 22),
            ("TERMINALE_STL", "PCMLPT_OXRED_PILE_SCHEMA",         "Transformation de la matière", "Représenter une pile (deux demi-piles, pont salin) avec polarité, électrodes et sens des électrons, du courant et des ions", 23),
            ("TERMINALE_STL", "PCMLPT_OXRED_PILE_EQUATION",       "Transformation de la matière", "Écrire l'équation de fonctionnement d'une pile à partir de sa polarité et des couples redox impliqués", 24),
            ("TERMINALE_STL", "PCMLPT_OXRED_QUANTITE_ELEC",       "Transformation de la matière", "Déterminer la quantité d'électricité disponible dans une pile à partir des quantités de matière initiales", 25),
            ("TERMINALE_STL", "PCMLPT_OXRED_PILE_REALISER",       "Transformation de la matière", "Réaliser une pile et mesurer sa tension pour identifier anode, cathode, oxydant et réducteur", 26),
            ("TERMINALE_STL", "PCMLPT_CIN_LOI_VITESSE",           "Transformation de la matière", "Établir la loi d'évolution de la concentration d'une espèce en fonction du temps pour une réaction d'ordre 0 ou 1", 27),
            ("TERMINALE_STL", "PCMLPT_CIN_ORDRE",                 "Transformation de la matière", "Déterminer l'ordre d'une réaction, la constante de vitesse et le temps de demi-réaction à partir d'un suivi cinétique", 28),
            ("TERMINALE_STL", "PCMLPT_CIN_SUIVI",                 "Transformation de la matière", "Réaliser le suivi cinétique d'une transformation chimique et l'exploiter pour déterminer l'ordre de réaction", 29),
            ("TERMINALE_STL", "PCMLPT_RAD_TYPES_EQUATIONS",       "Transformation de la matière", "Définir les radioactivités α, β−, β+ et l'émission γ et écrire une équation nucléaire par les lois de conservation", 30),
            ("TERMINALE_STL", "PCMLPT_RAD_LOI_DECROISSANCE",      "Transformation de la matière", "Interpréter dN = −λN dt et établir la loi de décroissance radioactive par analogie avec une réaction d'ordre 1", 31),
            ("TERMINALE_STL", "PCMLPT_RAD_DEMI_VIE",              "Transformation de la matière", "Exploiter une courbe de décroissance radioactive, définir le temps de demi-vie et le relier à la constante de désintégration λ", 32),
            ("TERMINALE_STL", "PCMLPT_RAD_ACTIVITE",              "Transformation de la matière", "Définir l'activité et son unité (le becquerel) et citer des applications médicales ou industrielles de la radioactivité", 33),
            ("TERMINALE_STL", "PCMLPT_RAD_PROTECTION",            "Transformation de la matière", "Comparer l'activité de sources naturelles ou artificielles et choisir des modalités de protection à partir de documents", 34),
            ("TERMINALE_STL", "PCMLPT_RAD_COMPTAGE",              "Transformation de la matière", "Exploiter les données d'un appareil de comptage et tracer, modéliser et exploiter une courbe de décroissance radioactive", 35),
            ("TERMINALE_STL", "PCMLPT_MVT_ACCELERATION",          "Mouvements et interactions", "Citer et exploiter la relation entre les coordonnées du vecteur vitesse et celles du vecteur accélération", 36),
            ("TERMINALE_STL", "PCMLPT_MVT_ENREGISTREMENT",        "Mouvements et interactions", "Mesurer une accélération et réaliser et exploiter l'enregistrement d'un objet en mouvement", 37),
            ("TERMINALE_STL", "PCMLPT_MVT_OUTIL_NUMERIQUE",       "Mouvements et interactions", "Calculer avec un tableur ou un programme vitesses et accélérations à partir des positions, ou la vitesse à partir de l'accélération", 38),
            ("TERMINALE_STL", "PCMLPT_INT_FORCE_CHAMP",           "Mouvements et interactions", "Citer et exploiter la relation entre la force électrostatique et le champ électrostatique", 39),
            ("TERMINALE_STL", "PCMLPT_INT_ARMATURES",             "Mouvements et interactions", "Caractériser le champ électrostatique entre deux armatures planes et le relier à la tension et à la distance qui les sépare", 40),
            ("TERMINALE_STL", "PCMLPT_INT_NEWTON",                "Mouvements et interactions", "Effectuer un bilan des forces sur des objets en mouvement plan et citer et exploiter les lois de Newton", 41),
            ("TERMINALE_STL", "PCMLPT_INT_CHUTE_LIBRE_PLAN",      "Mouvements et interactions", "Établir et exploiter les lois horaires du mouvement plan de chute libre", 42),
            ("TERMINALE_STL", "PCMLPT_INT_VITESSE_LIMITE",        "Mouvements et interactions", "Établir l'expression de la vitesse en régime permanent en présence de frottements fluides (électrophorèse, chute dans un fluide)", 43),
            ("TERMINALE_STL", "PCMLPT_INT_FROTTEMENT_VISQUEUX",   "Mouvements et interactions", "Modéliser une chute verticale avec frottement visqueux : équation différentielle, régime permanent, temps caractéristique, loi horaire", 44),
            ("TERMINALE_STL", "PCMLPT_INT_REGIME_PERMANENT",      "Mouvements et interactions", "Identifier par l'expérience le régime permanent, estimer le temps caractéristique et mesurer la vitesse en régime permanent", 45),
            ("TERMINALE_STL", "PCMLPT_EN_CHAINE",                 "Énergie : conversions et transferts", "Schématiser une chaîne énergétique en identifiant les formes, les réservoirs et les convertisseurs d'énergie", 46),
            ("TERMINALE_STL", "PCMLPT_EN_QUANTITE",               "Énergie : conversions et transferts", "Évaluer une quantité d'énergie transférée, convertie ou stockée", 47),
            ("TERMINALE_STL", "PCMLPT_EN_BILAN_RENDEMENT",        "Énergie : conversions et transferts", "Exploiter la conservation de l'énergie pour un bilan énergétique, estimer l'énergie dissipée et calculer un rendement", 48),
            ("TERMINALE_STL", "PCMLPT_EN_DISSIPATION",            "Énergie : conversions et transferts", "Associer une dissipation d'énergie à un transfert thermique", 49),
            ("TERMINALE_STL", "PCMLPT_EMEC_TRAVAIL",              "Énergie : conversions et transferts", "Exprimer le travail d'une force pour un déplacement élémentaire et celui d'une force constante, et identifier les forces de travail nul", 50),
            ("TERMINALE_STL", "PCMLPT_EMEC_POTENTIELLE",          "Énergie : conversions et transferts", "Relier le travail du poids à la variation d'énergie potentielle de pesanteur et exploiter la relation qui définit celle-ci", 51),
            ("TERMINALE_STL", "PCMLPT_EMEC_MECANIQUE",            "Énergie : conversions et transferts", "Citer et exploiter la relation définissant l'énergie mécanique et exploiter sa conservation", 52),
            ("TERMINALE_STL", "PCMLPT_EMEC_TRANSFERTS",           "Énergie : conversions et transferts", "Analyser les transferts énergétiques au cours du mouvement d'un point matériel et associer frottements et variation d'énergie mécanique", 53),
            ("TERMINALE_STL", "PCMLPT_EMEC_RESERVOIR",            "Énergie : conversions et transferts", "Estimer à partir de documents l'énergie stockée dans un réservoir d'énergie mécanique ou la puissance moyenne disponible", 54),
            ("TERMINALE_STL", "PCMLPT_EMEC_ENREGISTREMENT",       "Énergie : conversions et transferts", "Réaliser et exploiter un enregistrement pour suivre les énergies cinétique, potentielle et mécanique d'un système", 55),
            ("TERMINALE_STL", "PCMLPT_ECH_DIAGRAMME_ETAT",        "Énergie : conversions et transferts", "Prévoir l'état physique d'un corps pur à température et pression données à l'aide de son diagramme d'état", 56),
            ("TERMINALE_STL", "PCMLPT_ECH_ENTHALPIE_ETAT",        "Énergie : conversions et transferts", "Définir une enthalpie de changement d'état et prévoir son signe lors du passage d'un état physique à un autre", 57),
            ("TERMINALE_STL", "PCMLPT_ECH_LOI_DE_HESS",           "Énergie : conversions et transferts", "Définir une enthalpie standard de formation et calculer une enthalpie standard de réaction par la loi de Hess", 58),
            ("TERMINALE_STL", "PCMLPT_ECH_EXO_ENDO",              "Énergie : conversions et transferts", "Identifier le caractère exothermique, endothermique ou athermique d'une réaction", 59),
            ("TERMINALE_STL", "PCMLPT_ECH_CAPACITE_THERMIQUE",    "Énergie : conversions et transferts", "Exploiter la relation entre variation d'enthalpie, capacité thermique et variation de température d'une phase condensée", 60),
            ("TERMINALE_STL", "PCMLPT_ECH_POUVOIR_CALORIFIQUE",   "Énergie : conversions et transferts", "Définir et utiliser le pouvoir calorifique pour comparer des combustibles et l'estimer par l'expérience", 61),
            ("TERMINALE_STL", "PCMLPT_EEL_LOIS",                  "Énergie : conversions et transferts", "Citer et exploiter la loi des nœuds et la loi des mailles dans un circuit simple, et la loi d'Ohm", 62),
            ("TERMINALE_STL", "PCMLPT_EEL_PUISSANCE_ENERGIE",     "Énergie : conversions et transferts", "Exploiter la puissance électrique fournie par un générateur ou reçue par un récepteur et la relation entre puissance et énergie", 63),
            ("TERMINALE_STL", "PCMLPT_EEL_ECHANGES_JOULE",        "Énergie : conversions et transferts", "Analyser les échanges d'énergie dans un circuit simple et interpréter l'effet Joule comme conversion en énergie thermique", 64),
            ("TERMINALE_STL", "PCMLPT_EEL_CIRCUIT_MESURES",       "Énergie : conversions et transferts", "Réaliser un circuit électrique d'après un schéma et y mesurer une tension et une intensité", 65),
            ("TERMINALE_STL", "PCMLPT_EEL_RENDEMENT_MOTEUR",      "Énergie : conversions et transferts", "Évaluer expérimentalement le rendement d'un moteur électrique à courant continu", 66),
            ("TERMINALE_STL", "PCMLPT_EEL_SOURCE_QUANTITE",       "Énergie : conversions et transferts", "Définir une source idéale de tension et exploiter la relation entre quantité d'électricité, durée de fonctionnement et intensité", 67),
            ("TERMINALE_STL", "PCMLPT_EEL_PILE_ACCUMULATEUR",     "Énergie : conversions et transferts", "Déterminer l'énergie disponible et estimer la durée de fonctionnement d'une pile ou d'un accumulateur", 68),
            ("TERMINALE_STL", "PCMLPT_EEL_DOCUMENTATION",         "Énergie : conversions et transferts", "Extraire d'une documentation les caractéristiques utiles d'une pile, d'un accumulateur, d'un panneau photovoltaïque ou d'une pile à combustible", 69),
            ("TERMINALE_STL", "PCMLPT_EEL_PHOTOVOLTAIQUE",        "Énergie : conversions et transferts", "Déterminer la caractéristique intensité-tension d'un panneau photovoltaïque, la comparer à une source idéale et en faire le bilan énergétique", 70),
            ("TERMINALE_STL", "PCMLPT_EOND_FLUX_ECLAIREMENT",     "Énergie : conversions et transferts", "Citer et exploiter la relation entre le flux énergétique (en W) et l'éclairement énergétique (en W·m⁻²)", 71),
            ("TERMINALE_STL", "PCMLPT_EOND_RENDEMENT_PV",         "Énergie : conversions et transferts", "Estimer ou mesurer le rendement d'un panneau photovoltaïque et identifier les facteurs limitants", 72),
            ("TERMINALE_STL", "PCMLPT_EOND_ENERGIE_RECUE",        "Énergie : conversions et transferts", "Établir et exploiter la relation entre l'énergie reçue par un système, le flux énergétique et la durée d'exposition", 73),
            ("TERMINALE_STL", "PCMLPT_EOND_LASER",                "Énergie : conversions et transferts", "Citer les principales propriétés d'un faisceau laser (directivité, monochromaticité, concentration d'énergie)", 74),
            ("TERMINALE_STL", "PCMLPT_EOND_LASER_SECURITE",       "Énergie : conversions et transferts", "Citer les consignes de sécurité, exploiter une norme pour la durée maximale d'exposition et relier les caractéristiques d'un laser à son usage", 75),
        };

        // -----------------------------------------------------------------------------
        // 4. TERMINALE STL — MATHÉMATIQUES (matière « Mathématiques »)
        //    Analyse seule (intégration, exponentielle, logarithme népérien, équations
        //    différentielles, composition). Lignes tirées des contenus : 1, 2, 8 ;
        //    des situations algorithmiques : 6, 7, 12, 19.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMLMT =
        {
            ("TERMINALE_STL", "PCMLMT_INT_AIRE_PRIMITIVE",        "Analyse", "Interpréter une intégrale comme une aire sous la courbe et la calculer par F(b) − F(a), F primitive de f", 1),
            ("TERMINALE_STL", "PCMLMT_INT_PROPRIETES",            "Analyse", "Utiliser linéarité, positivité, croissance et relation de Chasles de l'intégrale", 2),
            ("TERMINALE_STL", "PCMLMT_INT_CALCUL",                "Analyse", "Calculer l'intégrale d'une fonction sur un intervalle [a ; b]", 3),
            ("TERMINALE_STL", "PCMLMT_INT_VALEUR_MOYENNE",        "Analyse", "Calculer la valeur moyenne d'une fonction sur un intervalle [a ; b]", 4),
            ("TERMINALE_STL", "PCMLMT_INT_AIRES",                 "Analyse", "Calculer une aire sous une courbe ou entre deux courbes", 5),
            ("TERMINALE_STL", "PCMLMT_INT_RECTANGLES",            "Analyse", "Calculer une valeur approchée d'une intégrale par la méthode des rectangles", 6),
            ("TERMINALE_STL", "PCMLMT_INT_MONTE_CARLO",           "Analyse", "Estimer une aire par la méthode de Monte-Carlo", 7),
            ("TERMINALE_STL", "PCMLMT_EXP_DERIVEE",               "Analyse", "Dériver x ↦ eˣ et x ↦ e^(kx) pour k réel et connaître la courbe représentative de l'exponentielle", 8),
            ("TERMINALE_STL", "PCMLMT_EXP_ALGEBRE",               "Analyse", "Utiliser les propriétés algébriques de l'exponentielle pour transformer des expressions", 9),
            ("TERMINALE_STL", "PCMLMT_EXP_VARIATIONS",            "Analyse", "Étudier les variations de sommes, produits ou quotients de fonctions x ↦ e^(kx) et de fonctions polynômes", 10),
            ("TERMINALE_STL", "PCMLMT_EXP_LIMITES",               "Analyse", "Déterminer les limites en −∞ et en +∞ de telles fonctions, y compris par croissance comparée", 11),
            ("TERMINALE_STL", "PCMLMT_EXP_VALEUR_DE_E",           "Analyse", "Rechercher une valeur approchée de e par balayage ou dichotomie", 12),
            ("TERMINALE_STL", "PCMLMT_LN_ALGEBRE",                "Analyse", "Utiliser les propriétés algébriques du logarithme népérien pour transformer des expressions", 13),
            ("TERMINALE_STL", "PCMLMT_LN_EQUATIONS",              "Analyse", "Résoudre des équations et inéquations du type e^(ax) = b, e^(ax) > b, ln(x) = b, ln(x) > b", 14),
            ("TERMINALE_STL", "PCMLMT_LN_ETUDE",                  "Analyse", "Étudier des sommes, produits ou quotients de fonctions polynômes et de la fonction x ↦ ln(x)", 15),
            ("TERMINALE_STL", "PCMLMT_EQDIFF_VERIFIER",           "Analyse", "Vérifier qu'une fonction donnée est solution d'une équation différentielle", 16),
            ("TERMINALE_STL", "PCMLMT_EQDIFF_SOLUTIONS",          "Analyse", "Déterminer l'ensemble des solutions d'une équation différentielle du type y′ = ay + b", 17),
            ("TERMINALE_STL", "PCMLMT_EQDIFF_CONDITION_INIT",     "Analyse", "Déterminer la solution de y′ = ay + b vérifiant une condition initiale y(x0) donnée", 18),
            ("TERMINALE_STL", "PCMLMT_EQDIFF_EULER",              "Analyse", "Approcher par la méthode d'Euler la courbe de l'exponentielle, solution de y′ = y avec y(0) = 1", 19),
            ("TERMINALE_STL", "PCMLMT_COMP_IDENTIFIER",           "Analyse", "Identifier la composée de deux fonctions dans une expression simple", 20),
            ("TERMINALE_STL", "PCMLMT_COMP_DERIVEES",             "Analyse", "Calculer la dérivée de (u(x))ⁿ pour n entier relatif, cos(u(x)), sin(u(x)), e^(u(x)) et ln(u(x))", 21),
            ("TERMINALE_STL", "PCMLMT_COMP_PRIMITIVES",           "Analyse", "Calculer des primitives de f(ax + b), u′uⁿ, u′/u, u′eᵘ, u′cos u et u′sin u", 22),
        };
    }
}
