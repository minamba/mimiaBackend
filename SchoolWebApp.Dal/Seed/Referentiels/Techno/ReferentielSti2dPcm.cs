namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// La spécialité « physique-chimie et mathématiques » de la STI2D, telle que les textes
    /// officiels la définissent — provenance détaillée en tête du fichier. Spécialité PARTAGÉE :
    /// la partie physique-chimie va à la matière « Physique-Chimie », la partie mathématiques à la
    /// matière « Mathématiques ». Réparti entre les matières par `ReferentielsSeriesTechnologiques`.
    /// </summary>
    public static class ReferentielSti2dPcm
    {
        // =============================================================================
        // RÉFÉRENTIEL DE LA SÉRIE TECHNOLOGIQUE STI2D (sciences et technologies de l'industrie et
        // du développement durable) — enseignement de spécialité « Physique-chimie et mathématiques »
        // de première et de terminale. L'ingénierie (I2D / 2I2D) est traitée ailleurs.
        //
        // PROVENANCE — textes téléchargés et lus le 23/11/2026. Rien n'est écrit de mémoire.
        // Copies locales lues : PDF + .txt extraits par pdftotext -layout (Windows-1252).
        //
        // | Tableau | Niveau          | Intitulé exact (page de titre de l'annexe)                              | Arrêté / NOR                       | BO                            | En vigueur              | PDF lu |
        // |---------|-----------------|--------------------------------------------------------------------------|------------------------------------|-------------------------------|-------------------------|--------|
        // | PCMP1   | PREMIERE_STI2D  | « Programme de physique-chimie et mathématiques de première STI2D » (annexe 2), partie « Programme de physique-chimie »     | arrêté du 17/01/2019, MENE1901591A | BO spécial n° 1 du 22/01/2019 | rentrée 2019 (art. 2)   | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/85/6/spe591_annexe2_22-1_1063856.pdf |
        // | PCMM1   | PREMIERE_STI2D  | même annexe 2, partie « Programme de mathématiques »                                                                         | arrêté du 17/01/2019, MENE1901591A | BO spécial n° 1 du 22/01/2019 | rentrée 2019 (art. 2)   | idem |
        // | PCMPT   | TERMINALE_STI2D | « Programme de physique-chimie et mathématiques de terminale STI2D » (annexe 3), partie « Programme de physique-chimie »   | arrêté du 19/07/2019, MENE1921261A | BO spécial n° 8 du 25/07/2019 | rentrée 2020 (art. 2 de l'arrêté du 17/01/2019) | https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/93/5/spe261_annexe_1158935.pdf |
        // | PCMMT   | TERMINALE_STI2D | même annexe 3, partie « Programme de mathématiques »                                                                         | arrêté du 19/07/2019, MENE1921261A | BO spécial n° 8 du 25/07/2019 | rentrée 2020            | idem |
        //
        // Arrêtés lus : MENE1901591A (art. 1 : programmes de spécialité STI2D fixés par les annexes ;
        // art. 2 : rentrée 2019 pour la première, rentrée 2020 pour la terminale ; annexe 1 ingénierie,
        // annexe 2 PCM de première) et MENE1921261A (art. 1 : « l'annexe de l'arrêté du 17 janvier 2019
        // est complétée par l'annexe 3 » = PCM de terminale). Chaque annexe identifiée par sa page de titre.
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : aucun texte de 2020 à 2026 ne remplace ces programmes ;
        // seules les épreuves du baccalauréat ont changé.
        //
        // SPÉCIALITÉ PARTAGÉE ENTRE DEUX PROFESSEURS : la partie physique-chimie (PCMP1, PCMPT) va à la
        // matière « Physique-Chimie », la partie mathématiques (PCMM1, PCMMT) à la matière
        // « Mathématiques ». Le texte lui-même sépare les deux programmes ; les « liens avec les
        // mathématiques » et « liens avec l'enseignement de physique-chimie » ne sont pas repris.
        //
        // LECTURE : la physique-chimie est écrite en deux colonnes (notions et contenus / capacités
        // exigibles et activités expérimentales) : les libellés viennent de la seconde colonne. Les
        // rubriques « repères pour l'enseignement » et « exemples de situation-problème et mini-projets »
        // sont laissées au professeur. Domaine = l'une des quatre grandes parties du texte (Mesure et
        // incertitudes ; Énergie ; Matière et matériaux ; Ondes et information en première, Ondes et
        // signaux en terminale).
        // Les mathématiques sont écrites en « Contenus / Capacités attendues / Commentaires ». Les
        // libellés viennent des capacités attendues ; quand un contenu n'est couvert par aucune
        // capacité (mesure principale, forme algébrique, propriétés de l'intégrale, dérivée de
        // l'exponentielle, linéarisation…) ou qu'une « situation algorithmique » est nommée, une ligne
        // en est tirée, signalée par « (contenu) » ou « (algorithmique) » dans le commentaire de bloc.
        // Domaine = la partie du texte (Géométrie dans le plan ; Nombres complexes ; Analyse).
        // Symboles grecs perdus à l'extraction de la première (ω, φ : lignes 1020, 1145, 1191 du .txt) :
        // rétablis d'après la terminale, où ils sont lisibles (« a cos (ωt) + b sin (ωt) en A cos (ωt + φ) »).
        // Produit scalaire (lignes 1059-1070) : formule et lettre d'angle illisibles ; « ‖u‖ cos(θ) » est
        // la notation de la capacité, l'angle noté θ comme en terminale (« e^(iθ) »).
        // Codes : ASCII, <= 40 caractères. Ordre : 1..n dans l'ordre du programme, par tableau.
        //
        // Comptes : PCMP1 67 · PCMM1 20 · PCMPT 60 · PCMMT 27 · total 174
        // =============================================================================

        // -----------------------------------------------------------------------------
        // 1. PREMIÈRE STI2D — PHYSIQUE-CHIMIE (matière « Physique-Chimie »)
        //    Mesure et incertitudes ; Énergie (enjeux, chimique, électrique, interne, mécanique,
        //    lumière) ; Matière et matériaux (propriétés et organisation, combustions, oxydo-réduction) ;
        //    Ondes et information (notion d'onde, ondes sonores, ondes électromagnétiques).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMP1 =
        {
            ("PREMIERE_STI2D", "PCMP1_MES_GRANDEUR_UNITE",       "Mesure et incertitudes", "Distinguer grandeur, valeur et unité et citer les sept unités de base du Système international", 1),
            ("PREMIERE_STI2D", "PCMP1_MES_SOURCES_ERREURS",      "Mesure et incertitudes", "Identifier les principales sources d'erreurs lors d'une mesure", 2),
            ("PREMIERE_STI2D", "PCMP1_MES_JUSTESSE_FIDELITE",    "Mesure et incertitudes", "Exploiter des séries de mesures indépendantes (histogramme, moyenne, écart-type) pour comparer des méthodes en justesse et en fidélité", 3),
            ("PREMIERE_STI2D", "PCMP1_MES_INCERTITUDE_TYPE",     "Mesure et incertitudes", "Évaluer une incertitude-type par une approche statistique (type A) et l'estimer sur une mesure unique", 4),
            ("PREMIERE_STI2D", "PCMP1_MES_EXPRESSION",           "Mesure et incertitudes", "Exprimer un résultat de mesure avec les chiffres significatifs adaptés, l'incertitude-type associée et l'unité", 5),
            ("PREMIERE_STI2D", "PCMP1_MES_VALIDITE",             "Mesure et incertitudes", "Discuter la validité d'un résultat en comparant son écart à la valeur de référence à l'incertitude-type", 6),
            ("PREMIERE_STI2D", "PCMP1_EN_FORMES_SOURCES",        "Énergie", "Citer les formes d'énergie utilisées dans la vie courante, la production et les services, et les distinguer des sources associées", 7),
            ("PREMIERE_STI2D", "PCMP1_EN_PUISSANCE_DUREE",       "Énergie", "Exploiter la relation entre puissance, énergie et durée et citer des ordres de grandeur de puissances", 8),
            ("PREMIERE_STI2D", "PCMP1_EN_CHAINE_ENERGETIQUE",    "Énergie", "Identifier les principales conversions d'énergie et schématiser une chaîne énergétique (formes, sources, convertisseurs)", 9),
            ("PREMIERE_STI2D", "PCMP1_EN_QUANTITE",              "Énergie", "Évaluer ou mesurer une quantité d'énergie transférée, convertie ou stockée", 10),
            ("PREMIERE_STI2D", "PCMP1_EN_CONSERVATION_RENDEMENT","Énergie", "Exploiter la conservation de l'énergie d'un système isolé pour un bilan énergétique et le rendement d'une chaîne ou d'un convertisseur", 11),
            ("PREMIERE_STI2D", "PCMP1_EN_RENOUVELABLE",          "Énergie", "Énoncer qu'une ressource d'énergie est « renouvelable » si son renouvellement naturel est rapide à l'échelle d'une vie humaine", 12),
            ("PREMIERE_STI2D", "PCMP1_ECH_EFFET_THERMIQUE",      "Énergie", "Identifier le système chimique et associer une transformation exothermique ou endothermique à la variation de son énergie", 13),
            ("PREMIERE_STI2D", "PCMP1_ECH_COMBUSTION",           "Énergie", "Identifier combustible et comburant, l'apport d'énergie qui initie une combustion et son auto-entretien", 14),
            ("PREMIERE_STI2D", "PCMP1_ECH_POUVOIR_CALORIFIQUE",  "Énergie", "Comparer des pouvoirs calorifiques et déterminer expérimentalement celui d'un combustible", 15),
            ("PREMIERE_STI2D", "PCMP1_ECH_DANGERS",              "Énergie", "Citer les dangers liés aux combustions et les moyens de prévention et de protection associés", 16),
            ("PREMIERE_STI2D", "PCMP1_EEL_CIRCUIT_SCHEMA",       "Énergie", "Réaliser un circuit à partir d'un schéma, et inversement, et y représenter ampèremètre, voltmètre ou système d'acquisition", 17),
            ("PREMIERE_STI2D", "PCMP1_EEL_GRANDEURS_PERIODIQUES","Énergie", "Visualiser une tension ou un courant périodique et en analyser période, fréquence, composantes continue et alternative", 18),
            ("PREMIERE_STI2D", "PCMP1_EEL_MOYENNE_EFFICACE",     "Énergie", "Choisir le réglage des appareils et mesurer la valeur moyenne et la valeur efficace d'une tension ou d'une intensité", 19),
            ("PREMIERE_STI2D", "PCMP1_EEL_NOEUDS_MAILLES",       "Énergie", "Algébriser tensions et intensités et utiliser la loi des nœuds et la loi des mailles (trois mailles au plus)", 20),
            ("PREMIERE_STI2D", "PCMP1_EEL_TRANSFERTS",           "Énergie", "Analyser les transferts d'énergie dans un circuit à partir du signe de la puissance et de la convention choisie", 21),
            ("PREMIERE_STI2D", "PCMP1_EEL_PUISSANCE_ENERGIE",    "Énergie", "Calculer ou mesurer la puissance moyenne et l'énergie électrique mises en jeu pendant une durée donnée", 22),
            ("PREMIERE_STI2D", "PCMP1_EEL_MODELE_VALIDITE",      "Énergie", "Analyser le domaine de validité d'un modèle à partir de mesures sur des dipôles passifs résistifs", 23),
            ("PREMIERE_STI2D", "PCMP1_EEL_SECURITE",             "Énergie", "Adopter un comportement responsable et respecter les règles de sécurité électrique lors des manipulations", 24),
            ("PREMIERE_STI2D", "PCMP1_EIN_TEMPERATURE",          "Énergie", "Associer la température à l'agitation microscopique, convertir degrés Celsius et kelvins et mesurer des températures", 25),
            ("PREMIERE_STI2D", "PCMP1_EIN_ENERGIE_INTERNE",      "Énergie", "Relier l'énergie interne à ses contributions microscopiques et calculer sa variation lors d'une variation de température", 26),
            ("PREMIERE_STI2D", "PCMP1_EIN_CAPACITE_THERMIQUE",   "Énergie", "Définir et exploiter la capacité thermique massique et l'énergie massique de changement d'état", 27),
            ("PREMIERE_STI2D", "PCMP1_EIN_SENS_TRANSFERT",       "Énergie", "Prévoir le sens d'un transfert thermique entre deux systèmes pour déterminer leur état final", 28),
            ("PREMIERE_STI2D", "PCMP1_EIN_MODES_TRANSFERT",      "Énergie", "Décrire qualitativement conduction, convection et rayonnement en citant des exemples", 29),
            ("PREMIERE_STI2D", "PCMP1_EIN_BILAN_ENCEINTE",       "Énergie", "Réaliser expérimentalement le bilan thermique d'une enceinte en régime stationnaire", 30),
            ("PREMIERE_STI2D", "PCMP1_EMEC_REFERENTIEL",         "Énergie", "Choisir un référentiel, caractériser un mouvement et assimiler un solide en translation à son centre de masse", 31),
            ("PREMIERE_STI2D", "PCMP1_EMEC_VITESSE_ACCELERATION","Énergie", "Exploiter la vitesse moyenne, définir vitesse et accélération comme dérivées par rapport au temps et les mesurer", 32),
            ("PREMIERE_STI2D", "PCMP1_EMEC_FORCES",              "Énergie", "Modéliser par des forces les actions mécaniques sur un solide et faire un bilan de forces à l'équilibre ou en translation uniforme", 33),
            ("PREMIERE_STI2D", "PCMP1_EMEC_TRAVAIL",             "Énergie", "Écrire et exploiter l'expression du travail d'une force constante", 34),
            ("PREMIERE_STI2D", "PCMP1_EMEC_ENERGIE_CINETIQUE",   "Énergie", "Exploiter l'énergie cinétique d'un solide en translation et associer sa variation au travail des forces appliquées", 35),
            ("PREMIERE_STI2D", "PCMP1_EMEC_PUISSANCE",           "Énergie", "Exploiter la relation entre travail et puissance moyenne pour modifier une vitesse ou la maintenir malgré les frottements", 36),
            ("PREMIERE_STI2D", "PCMP1_EMEC_ENERGIE_MECANIQUE",   "Énergie", "Analyser un mouvement de translation par les échanges cinétique-potentielle et la conservation ou non de l'énergie mécanique", 37),
            ("PREMIERE_STI2D", "PCMP1_LUM_IRRADIANCE",           "Énergie", "Mesurer une irradiance et calculer la puissance reçue par une surface", 38),
            ("PREMIERE_STI2D", "PCMP1_LUM_LASER",                "Énergie", "Citer les caractéristiques de la lumière laser et estimer son irradiance pour conclure sur ses usages et les protections", 39),
            ("PREMIERE_STI2D", "PCMP1_LUM_PHOTOVOLTAIQUE",       "Énergie", "Effectuer expérimentalement le bilan énergétique et déterminer le rendement d'un panneau photovoltaïque", 40),
            ("PREMIERE_STI2D", "PCMP1_MAT_FAMILLES",             "Matière et matériaux", "Citer des matériaux métalliques, organiques, minéraux et composites et les identifier par des tests et des banques de données", 41),
            ("PREMIERE_STI2D", "PCMP1_MAT_CAHIER_DES_CHARGES",   "Matière et matériaux", "Choisir des matériaux selon un cahier des charges et mesurer résistivité, résistance thermique surfacique ou indice de réfraction", 42),
            ("PREMIERE_STI2D", "PCMP1_MAT_CYCLE_DE_VIE",         "Matière et matériaux", "Exploiter des informations sur la production industrielle, l'utilisation et le recyclage de matériaux usuels", 43),
            ("PREMIERE_STI2D", "PCMP1_MAT_LEWIS",                "Matière et matériaux", "Établir les schémas de Lewis de l'eau, du dioxygène, du dioxyde de carbone et du chlorure d'hydrogène", 44),
            ("PREMIERE_STI2D", "PCMP1_MAT_MOLECULES_ORGANIQUES", "Matière et matériaux", "Reconnaître molécule et macromolécule organiques, passer d'une formule à l'autre et repérer les groupes alcool et acide carboxylique", 45),
            ("PREMIERE_STI2D", "PCMP1_MAT_MASSE_MOLAIRE_CONC",   "Matière et matériaux", "Calculer une masse molaire moléculaire et déterminer la concentration d'un soluté (g/L ou mol/L)", 46),
            ("PREMIERE_STI2D", "PCMP1_MAT_DILUTION_DISSOLUTION", "Matière et matériaux", "Réaliser une solution de concentration donnée par dilution ou par dissolution d'un soluté", 47),
            ("PREMIERE_STI2D", "PCMP1_MAT_SECURITE_CLP",         "Matière et matériaux", "Adapter son attitude aux pictogrammes CLP des produits utilisés et aux consignes de sécurité correspondantes", 48),
            ("PREMIERE_STI2D", "PCMP1_COMB_CARBURANTS",          "Matière et matériaux", "Citer des carburants fossiles et des agro-carburants usuels et l'impact de leur utilisation sur l'environnement", 49),
            ("PREMIERE_STI2D", "PCMP1_COMB_EQUATIONS",           "Matière et matériaux", "Écrire et exploiter l'équation d'une combustion complète (réactif limitant, produits formés) ou incomplète", 50),
            ("PREMIERE_STI2D", "PCMP1_COMB_ALCANES_ALCOOLS",     "Matière et matériaux", "Identifier un alcane, un alcène ou un alcool (chaîne carbonée, groupe caractéristique) à partir de sa formule", 51),
            ("PREMIERE_STI2D", "PCMP1_OXRED_TRANSFERT",          "Matière et matériaux", "Identifier un transfert d'électrons et définir oxydant, réducteur, oxydation, réduction et couple oxydant/réducteur", 52),
            ("PREMIERE_STI2D", "PCMP1_OXRED_EQUATIONS",          "Matière et matériaux", "Écrire une demi-équation électronique et l'équation d'une réaction d'oxydo-réduction, les couples étant donnés", 53),
            ("PREMIERE_STI2D", "PCMP1_OXRED_CORROSION",          "Matière et matériaux", "Analyser une corrosion, citer des métaux ou alliages résistants et interpréter des méthodes de protection", 54),
            ("PREMIERE_STI2D", "PCMP1_OXRED_PILE",               "Matière et matériaux", "Analyser et étudier le fonctionnement d'une pile en termes de transfert d'électrons et d'oxydo-réduction", 55),
            ("PREMIERE_STI2D", "PCMP1_OND_MECA_EM",              "Ondes et information", "Citer des ondes mécaniques et leurs milieux de propagation et distinguer l'onde électromagnétique, qui n'en exige pas", 56),
            ("PREMIERE_STI2D", "PCMP1_OND_ENERGIE_TYPES",        "Ondes et information", "Associer une onde à un transfert d'énergie sans déplacement de matière et distinguer ondes longitudinale et transversale", 57),
            ("PREMIERE_STI2D", "PCMP1_OND_PERIODIQUE",           "Ondes et information", "Déterminer les grandeurs d'une onde périodique et exploiter la relation entre longueur d'onde, célérité et fréquence", 58),
            ("PREMIERE_STI2D", "PCMP1_OND_INFORMATION",          "Ondes et information", "Associer le transport de l'information à la propagation d'une onde modulée selon un code, de l'émetteur au récepteur", 59),
            ("PREMIERE_STI2D", "PCMP1_OND_TRANSMISSION_REFLEXION","Ondes et information", "Mettre en œuvre un guide d'onde et observer transmission, absorption et réflexion d'une onde", 60),
            ("PREMIERE_STI2D", "PCMP1_SON_GRANDEURS",            "Ondes et information", "Énoncer qu'un son exige un milieu matériel et déterminer célérité, période, amplitude, fréquence et longueur d'onde", 61),
            ("PREMIERE_STI2D", "PCMP1_SON_CELERITE_DISTANCES",   "Ondes et information", "Évaluer la célérité du son dans l'air, l'eau, un métal et déterminer des distances avec ou sans réflexion", 62),
            ("PREMIERE_STI2D", "PCMP1_SON_PERCEPTION",           "Ondes et information", "Associer fréquence et amplitude à la hauteur et à l'intensité d'un son et citer les limites des fréquences audibles", 63),
            ("PREMIERE_STI2D", "PCMP1_SON_PUISSANCE_INTENSITE",  "Ondes et information", "Exploiter la relation entre la puissance et l'intensité acoustiques", 64),
            ("PREMIERE_STI2D", "PCMP1_OEM_DOMAINES",             "Ondes et information", "Ordonner les domaines des ondes électromagnétiques par fréquence et longueur d'onde, citer le visible et la célérité dans le vide", 65),
            ("PREMIERE_STI2D", "PCMP1_OEM_SOURCES_LASER",        "Ondes et information", "Citer des caractéristiques de sources lumineuses courantes et exploiter celles d'un laser (longueur d'onde, puissance, directivité)", 66),
            ("PREMIERE_STI2D", "PCMP1_OEM_RISQUES",              "Ondes et information", "Citer les risques et les précautions associés à l'utilisation de sources lumineuses variées", 67),
        };

        // -----------------------------------------------------------------------------
        // 2. PREMIÈRE STI2D — MATHÉMATIQUES (matière « Mathématiques »)
        //    Géométrie dans le plan (trigonométrie, produit scalaire) ; Nombres complexes ;
        //    Analyse (dérivées, primitives). Lignes tirées des contenus : 2, 6, 8, 11, 17 ;
        //    de la situation algorithmique : 20 (aussi capacité attendue).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMM1 =
        {
            ("PREMIERE_STI2D", "PCMM1_TRIGO_RADIAN",             "Géométrie dans le plan", "Effectuer des conversions de degré en radian et de radian en degré", 1),
            ("PREMIERE_STI2D", "PCMM1_TRIGO_MESURE_PRINCIPALE",  "Géométrie dans le plan", "Utiliser le cercle trigonométrique et déterminer les mesures et la mesure principale d'un angle orienté", 2),
            ("PREMIERE_STI2D", "PCMM1_TRIGO_EQUATIONS",          "Géométrie dans le plan", "Résoudre par lecture sur le cercle trigonométrique des équations du type cos(x) = a et sin(x) = a", 3),
            ("PREMIERE_STI2D", "PCMM1_TRIGO_ANGLES_ASSOCIES",    "Géométrie dans le plan", "Connaître et utiliser les relations entre sinus et cosinus des angles associés : −x, π − x, π + x, π/2 − x, π/2 + x", 4),
            ("PREMIERE_STI2D", "PCMM1_TRIGO_FONCTIONS_CIRC",     "Géométrie dans le plan", "Utiliser périodicité, parité, variations et valeurs remarquables de sinus et cosinus et justifier les symétries de leurs courbes", 5),
            ("PREMIERE_STI2D", "PCMM1_TRIGO_SINUSOIDES",         "Géométrie dans le plan", "Lire amplitude, période et phase à l'origine de t ↦ A cos(ωt + φ) et t ↦ A sin(ωt + φ) et tracer leurs courbes", 6),
            ("PREMIERE_STI2D", "PCMM1_PS_PROJECTION",            "Géométrie dans le plan", "Calculer la projection d'un vecteur sur un axe et interpréter ‖u‖ cos(θ) en termes de projection", 7),
            ("PREMIERE_STI2D", "PCMM1_PS_CALCUL",                "Géométrie dans le plan", "Calculer un produit scalaire par sa définition géométrique, par projection ou dans une base orthonormée", 8),
            ("PREMIERE_STI2D", "PCMM1_PS_ORTHOGONALITE_ANGLE",   "Géométrie dans le plan", "Utiliser un produit scalaire pour démontrer l'orthogonalité de deux vecteurs ou calculer un angle non orienté", 9),
            ("PREMIERE_STI2D", "PCMM1_PS_LONGUEURS",             "Géométrie dans le plan", "Utiliser un produit scalaire pour calculer des longueurs (théorème d'Al-Kashi, égalité du parallélogramme)", 10),
            ("PREMIERE_STI2D", "PCMM1_CPX_FORME_ALGEBRIQUE",     "Nombres complexes", "Calculer somme, produit, quotient et conjugué de nombres complexes sous forme algébrique", 11),
            ("PREMIERE_STI2D", "PCMM1_CPX_INTERPRETATION",       "Nombres complexes", "Calculer et interpréter géométriquement partie réelle, partie imaginaire, conjugué, module et argument d'un nombre complexe", 12),
            ("PREMIERE_STI2D", "PCMM1_CPX_FORME_TRIGO",          "Nombres complexes", "Passer de la forme algébrique à la forme trigonométrique d'un nombre complexe et inversement", 13),
            ("PREMIERE_STI2D", "PCMM1_DER_NOTATIONS",            "Analyse", "Utiliser les notations du taux de variation et du nombre dérivé en un point (Δy/Δx, dy/dx, f′(x0))", 14),
            ("PREMIERE_STI2D", "PCMM1_DER_APPROX_AFFINE",        "Analyse", "Effectuer des calculs approchés à l'aide de l'approximation affine en un point", 15),
            ("PREMIERE_STI2D", "PCMM1_DER_CALCUL",               "Analyse", "Calculer une fonction dérivée : somme, produit, inverse, quotient, xⁿ, 1/x, polynôme, cosinus et sinus", 16),
            ("PREMIERE_STI2D", "PCMM1_DER_AFFINE_SINUSOIDE",     "Analyse", "Dériver x ↦ f(ax + b), t ↦ A cos(ωt + φ) et t ↦ A sin(ωt + φ)", 17),
            ("PREMIERE_STI2D", "PCMM1_DER_VARIATIONS",           "Analyse", "Étudier le sens de variation d'une fonction", 18),
            ("PREMIERE_STI2D", "PCMM1_PRIM_CALCUL",              "Analyse", "Calculer des primitives de polynômes et de t ↦ A cos(ωt + φ), t ↦ A sin(ωt + φ), à une constante près", 19),
            ("PREMIERE_STI2D", "PCMM1_PRIM_EULER",               "Analyse", "Construire point par point par la méthode d'Euler une approximation de la solution de y′ = f(t), y(t0) = y0", 20),
        };

        // -----------------------------------------------------------------------------
        // 3. TERMINALE STI2D — PHYSIQUE-CHIMIE (matière « Physique-Chimie »)
        //    Mesure et incertitudes ; Énergie (enjeux, chimique, électrique, interne, mécanique,
        //    lumière) ; Matière et matériaux (propriétés et organisation, combustions, oxydo-réduction,
        //    acide-base) ; Ondes et signaux (notion d'onde, ondes sonores, ondes électromagnétiques).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMPT =
        {
            ("TERMINALE_STI2D", "PCMPT_MES_TYPE_A",               "Mesure et incertitudes", "Évaluer une incertitude-type par une approche statistique (type A), y compris à l'aide d'un outil numérique", 1),
            ("TERMINALE_STI2D", "PCMPT_MES_MESURE_UNIQUE",        "Mesure et incertitudes", "Évaluer l'incertitude-type d'une mesure unique en exploitant une relation fournie", 2),
            ("TERMINALE_STI2D", "PCMPT_MES_COMPOSEE",             "Mesure et incertitudes", "Comparer le poids des sources d'erreur et évaluer par un outil numérique une incertitude-type composée", 3),
            ("TERMINALE_STI2D", "PCMPT_MES_AMELIORER_PROTOCOLE",  "Mesure et incertitudes", "Faire des propositions pour améliorer un protocole de mesure", 4),
            ("TERMINALE_STI2D", "PCMPT_MES_VALIDITE",             "Mesure et incertitudes", "Évaluer le nombre d'incertitudes-types séparant un résultat de la valeur de référence et discuter sa validité selon le contexte", 5),
            ("TERMINALE_STI2D", "PCMPT_MES_ECRITURE",             "Mesure et incertitudes", "Écrire un résultat en écriture scientifique avec chiffres significatifs, incertitude et unité, arrondi en cohérence", 6),
            ("TERMINALE_STI2D", "PCMPT_EN_PUISSANCE_INSTANTANEE", "Énergie", "Définir la puissance instantanée comme limite de la puissance moyenne et comme dérivée de l'énergie par rapport au temps", 7),
            ("TERMINALE_STI2D", "PCMPT_EN_ENERGIE_COURBE",        "Énergie", "Déterminer l'énergie mise en jeu pendant un intervalle de temps à partir de la courbe de la puissance en fonction du temps", 8),
            ("TERMINALE_STI2D", "PCMPT_EN_OUTIL_NUMERIQUE",       "Énergie", "Calculer avec un outil numérique la puissance à partir de valeurs de l'énergie au cours du temps, et inversement", 9),
            ("TERMINALE_STI2D", "PCMPT_EN_AUTONOMIE",             "Énergie", "Estimer la durée de fonctionnement d'un système autonome", 10),
            ("TERMINALE_STI2D", "PCMPT_EN_RENDEMENT_REVERSIBLE",  "Énergie", "Exploiter la relation du rendement d'une conversion ou d'un transfert et définir un fonctionnement réversible ou non", 11),
            ("TERMINALE_STI2D", "PCMPT_ECH_PILE_ACCUMULATEUR",    "Énergie", "Distinguer pile et accumulateur et calculer l'énergie stockée à partir de la tension et de la quantité d'électricité", 12),
            ("TERMINALE_STI2D", "PCMPT_ECH_CARACTERISTIQUES",     "Énergie", "Exploiter tension à vide, capacité, énergies massique et volumique et nombre de cycles d'une pile ou d'un accumulateur", 13),
            ("TERMINALE_STI2D", "PCMPT_EEL_PUISSANCES_P_S",       "Énergie", "Distinguer en régime sinusoïdal la puissance apparente S (dimensionnement) et la puissance active P (puissance moyenne)", 14),
            ("TERMINALE_STI2D", "PCMPT_EEL_MESURE_P_S",           "Énergie", "Mesurer P et S en régime sinusoïdal et calculer P avec un outil numérique à partir de la tension et du courant", 15),
            ("TERMINALE_STI2D", "PCMPT_EEL_FACTEUR_PUISSANCE",    "Énergie", "Calculer le facteur de puissance k = P/S d'un récepteur et le relier qualitativement aux pertes en ligne", 16),
            ("TERMINALE_STI2D", "PCMPT_EEL_RESEAU",               "Énergie", "Schématiser le transport et la distribution de l'énergie électrique (ligne monophasée) et citer les caractéristiques du réseau", 17),
            ("TERMINALE_STI2D", "PCMPT_EEL_TRANSFORMATEUR",       "Énergie", "Citer les rôles du transformateur et mesurer le rendement et le rapport de transformation d'un transformateur monophasé", 18),
            ("TERMINALE_STI2D", "PCMPT_EEL_TENSION_PERTES",       "Énergie", "Relier qualitativement l'augmentation de la tension de distribution à la diminution des pertes en ligne", 19),
            ("TERMINALE_STI2D", "PCMPT_EEL_PROTECTION",           "Énergie", "Exploiter les seuils de dangerosité du courant et citer les protections des personnes et des matériels", 20),
            ("TERMINALE_STI2D", "PCMPT_EIN_FLUX_THERMIQUE",       "Énergie", "Définir le flux thermique à travers une paroi comme une puissance et le calculer", 21),
            ("TERMINALE_STI2D", "PCMPT_EIN_RESISTANCE_THERMIQUE", "Énergie", "Exploiter la relation entre flux thermique, résistance thermique et écart de température en régime permanent", 22),
            ("TERMINALE_STI2D", "PCMPT_EIN_CALCUL_RTH",           "Énergie", "Calculer la résistance thermique d'une paroi simple ou multicouche à partir des épaisseurs et des conductivités", 23),
            ("TERMINALE_STI2D", "PCMPT_EIN_RTH_EXPERIMENTALE",    "Énergie", "Déterminer expérimentalement la résistance thermique d'une paroi", 24),
            ("TERMINALE_STI2D", "PCMPT_EMEC_PFD",                 "Énergie", "Relier accélération et résultante des forces extérieures pour un mouvement rectiligne (principe fondamental de la dynamique)", 25),
            ("TERMINALE_STI2D", "PCMPT_EMEC_CHUTE_LIBRE",         "Énergie", "Mesurer des accélérations et exploiter numériquement des résultats expérimentaux pour valider le modèle de la chute libre", 26),
            ("TERMINALE_STI2D", "PCMPT_EMEC_FROTTEMENTS",         "Énergie", "Modéliser une résistance aérodynamique et déterminer un frottement solide-solide par le travail des forces", 27),
            ("TERMINALE_STI2D", "PCMPT_EMEC_ROTATION_MOMENT",     "Énergie", "Relier vitesse linéaire et vitesse angulaire et calculer le moment d'une force et d'un couple de forces", 28),
            ("TERMINALE_STI2D", "PCMPT_EMEC_MOTEUR_CHARGE",       "Énergie", "Exploiter graphiquement la caractéristique d'un moteur pour déterminer le point de fonctionnement moteur-charge", 29),
            ("TERMINALE_STI2D", "PCMPT_EMEC_PRESSION",            "Énergie", "Définir la pression à partir des forces pressantes et distinguer pression absolue et pression relative", 30),
            ("TERMINALE_STI2D", "PCMPT_EMEC_HYDROSTATIQUE",       "Énergie", "Exploiter le principe fondamental de l'hydrostatique et mesurer des pressions ou des différences de pression", 31),
            ("TERMINALE_STI2D", "PCMPT_LUM_PHOTON",               "Énergie", "Interpréter les échanges d'énergie entre matière et lumière par le photon et exploiter la relation E = h.f", 32),
            ("TERMINALE_STI2D", "PCMPT_LUM_CONVERSIONS",          "Énergie", "Identifier les formes d'énergie d'une conversion photovoltaïque ou photothermique et en déterminer le rendement", 33),
            ("TERMINALE_STI2D", "PCMPT_LUM_POINT_FONCTIONNEMENT", "Énergie", "Exploiter la caractéristique tension-courant d'un panneau photovoltaïque pour identifier son point de fonctionnement", 34),
            ("TERMINALE_STI2D", "PCMPT_MAT_LIAISONS_HYDROGENE",   "Matière et matériaux", "Associer un changement d'état de l'eau à l'établissement ou à la rupture de liaisons hydrogène", 35),
            ("TERMINALE_STI2D", "PCMPT_MAT_DIAGRAMME_PT",         "Matière et matériaux", "Utiliser un diagramme d'état (P, T) pour déterminer l'état final d'un fluide lors d'une transformation d'un corps pur", 36),
            ("TERMINALE_STI2D", "PCMPT_MAT_BILAN_CHANGEMENT_ETAT","Matière et matériaux", "Établir le bilan énergétique d'une transformation physique avec énergies massiques de changement d'état et capacités thermiques", 37),
            ("TERMINALE_STI2D", "PCMPT_MAT_RADIOACTIVITE",        "Matière et matériaux", "Distinguer radioactivités naturelle et artificielle et citer les rayonnements alpha, bêta, gamma et les particules émises", 38),
            ("TERMINALE_STI2D", "PCMPT_MAT_ACTIVITE_DEMI_VIE",    "Matière et matériaux", "Définir l'activité d'une source et son unité et exploiter la demi-vie pour comparer deux décroissances radioactives", 39),
            ("TERMINALE_STI2D", "PCMPT_MAT_FISSION_FUSION",       "Matière et matériaux", "Distinguer fission et fusion et calculer défaut de masse et énergie libérée, l'équation nucléaire étant donnée", 40),
            ("TERMINALE_STI2D", "PCMPT_COMB_BILAN_ENERGETIQUE",   "Matière et matériaux", "Déterminer l'énergie échangée lors d'une combustion complète à l'aide du modèle de la réaction chimique", 41),
            ("TERMINALE_STI2D", "PCMPT_OXRED_POLARITE",           "Matière et matériaux", "Identifier l'oxydant et le réducteur d'une pile ou d'un accumulateur à partir de sa polarité ou des couples utilisés", 42),
            ("TERMINALE_STI2D", "PCMPT_OXRED_BILAN_MATIERE",      "Matière et matériaux", "Réaliser un bilan de matière pour la charge ou la décharge d'un accumulateur et pour une pile à combustible", 43),
            ("TERMINALE_STI2D", "PCMPT_AB_DEFINITIONS",           "Matière et matériaux", "Définir un acide (donneur de proton) et une base (accepteur de proton) et les identifier dans un couple", 44),
            ("TERMINALE_STI2D", "PCMPT_AB_PH",                    "Matière et matériaux", "Exploiter la relation entre concentration en ions H3O+ et pH, prévoir l'effet d'une dilution et mesurer un pH", 45),
            ("TERMINALE_STI2D", "PCMPT_AB_EQUATION",              "Matière et matériaux", "Écrire et exploiter l'équation d'une réaction entre un acide et une base, les couples étant donnés", 46),
            ("TERMINALE_STI2D", "PCMPT_AB_DILUTION",              "Matière et matériaux", "Proposer et réaliser un protocole de dilution pour obtenir une solution de concentration molaire donnée", 47),
            ("TERMINALE_STI2D", "PCMPT_SIG_DECOMPOSITION",        "Ondes et signaux", "Comprendre qu'un signal périodique se décompose en une composante continue et des signaux sinusoïdaux", 48),
            ("TERMINALE_STI2D", "PCMPT_SIG_SPECTRE",              "Ondes et signaux", "Exploiter un spectre d'amplitude : composante continue, fondamental, harmoniques et rang d'un harmonique", 49),
            ("TERMINALE_STI2D", "PCMPT_SIG_RELEVE_SPECTRE",       "Ondes et signaux", "Relever expérimentalement le spectre d'amplitude d'une onde périodique", 50),
            ("TERMINALE_STI2D", "PCMPT_SIG_BANDE_TRANSMISSION",   "Ondes et signaux", "Déterminer l'intervalle de fréquence nécessaire pour transmettre un signal comportant des harmoniques choisis", 51),
            ("TERMINALE_STI2D", "PCMPT_SON_SPECTRE",              "Ondes et signaux", "Relever avec un outil numérique le spectre d'un son pur ou complexe et en déterminer fondamental et harmoniques", 52),
            ("TERMINALE_STI2D", "PCMPT_SON_TIMBRE_HAUTEUR",       "Ondes et signaux", "Définir et distinguer le timbre et la hauteur d'un son", 53),
            ("TERMINALE_STI2D", "PCMPT_SON_NIVEAU_SONORE",        "Ondes et signaux", "Exploiter la relation entre intensité acoustique et niveau sonore en décibels et mesurer des niveaux sonores", 54),
            ("TERMINALE_STI2D", "PCMPT_SON_OREILLE",              "Ondes et signaux", "Exploiter les courbes de sensibilité de l'oreille humaine (fréquences audibles, seuils d'audibilité et de douleur)", 55),
            ("TERMINALE_STI2D", "PCMPT_SON_TRANSMISSION",         "Ondes et signaux", "Mettre en évidence expérimentalement la transmission ou l'absorption d'un son par différents matériaux", 56),
            ("TERMINALE_STI2D", "PCMPT_OEM_DOMAINES_TELECOM",     "Ondes et signaux", "Positionner les domaines de fréquence des télécommunications sur une échelle de fréquence ou de longueur d'onde", 57),
            ("TERMINALE_STI2D", "PCMPT_OEM_TRANSPOSITION",        "Ondes et signaux", "Associer la transmission d'informations différentes dans un même milieu à une transposition fréquentielle", 58),
            ("TERMINALE_STI2D", "PCMPT_OEM_ANTENNES",             "Ondes et signaux", "Relier le domaine de fréquence exploité à la dimension des antennes utilisées", 59),
            ("TERMINALE_STI2D", "PCMPT_OEM_TRANSMISSION",         "Ondes et signaux", "Mettre en œuvre une transmission d'informations par infrarouge, par onde radio ou par fibre optique", 60),
        };

        // -----------------------------------------------------------------------------
        // 4. TERMINALE STI2D — MATHÉMATIQUES (matière « Mathématiques »)
        //    Analyse (intégration, exponentielle, logarithme népérien, équations différentielles,
        //    composition) ; Nombres complexes. Lignes tirées des contenus : 1, 2, 8, 25 ;
        //    des situations algorithmiques : 6, 7, 12, 19.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PCMMT =
        {
            ("TERMINALE_STI2D", "PCMMT_INT_AIRE_PRIMITIVE",       "Analyse", "Interpréter une intégrale comme une aire sous la courbe et la calculer par F(b) − F(a), F primitive de f", 1),
            ("TERMINALE_STI2D", "PCMMT_INT_PROPRIETES",           "Analyse", "Utiliser linéarité, positivité, croissance et relation de Chasles de l'intégrale", 2),
            ("TERMINALE_STI2D", "PCMMT_INT_CALCUL",               "Analyse", "Calculer l'intégrale d'une fonction sur un intervalle [a ; b]", 3),
            ("TERMINALE_STI2D", "PCMMT_INT_VALEUR_MOYENNE",       "Analyse", "Calculer la valeur moyenne d'une fonction sur un intervalle [a ; b]", 4),
            ("TERMINALE_STI2D", "PCMMT_INT_AIRES",                "Analyse", "Calculer une aire sous une courbe ou entre deux courbes", 5),
            ("TERMINALE_STI2D", "PCMMT_INT_RECTANGLES",           "Analyse", "Calculer une valeur approchée d'une intégrale par la méthode des rectangles", 6),
            ("TERMINALE_STI2D", "PCMMT_INT_MONTE_CARLO",          "Analyse", "Estimer une aire par la méthode de Monte-Carlo", 7),
            ("TERMINALE_STI2D", "PCMMT_EXP_DERIVEE",              "Analyse", "Dériver x ↦ eˣ et x ↦ e^(kx) pour k réel et connaître la courbe représentative de l'exponentielle", 8),
            ("TERMINALE_STI2D", "PCMMT_EXP_ALGEBRE",              "Analyse", "Utiliser les propriétés algébriques de l'exponentielle pour transformer des expressions", 9),
            ("TERMINALE_STI2D", "PCMMT_EXP_VARIATIONS",           "Analyse", "Étudier les variations de sommes, produits ou quotients de fonctions x ↦ e^(kx) et de fonctions polynômes", 10),
            ("TERMINALE_STI2D", "PCMMT_EXP_LIMITES",              "Analyse", "Déterminer les limites en −∞ et en +∞ de telles fonctions, y compris par croissance comparée", 11),
            ("TERMINALE_STI2D", "PCMMT_EXP_VALEUR_DE_E",          "Analyse", "Rechercher une valeur approchée de e par balayage ou dichotomie", 12),
            ("TERMINALE_STI2D", "PCMMT_LN_ALGEBRE",               "Analyse", "Utiliser les propriétés algébriques du logarithme népérien pour transformer des expressions", 13),
            ("TERMINALE_STI2D", "PCMMT_LN_EQUATIONS",             "Analyse", "Résoudre des équations et inéquations du type e^(ax) = b, e^(ax) > b, ln(x) = b, ln(x) > b", 14),
            ("TERMINALE_STI2D", "PCMMT_LN_ETUDE",                 "Analyse", "Étudier des sommes, produits ou quotients de fonctions polynômes et de la fonction x ↦ ln(x)", 15),
            ("TERMINALE_STI2D", "PCMMT_EQDIFF_VERIFIER",          "Analyse", "Vérifier qu'une fonction donnée est solution d'une équation différentielle", 16),
            ("TERMINALE_STI2D", "PCMMT_EQDIFF_SOLUTIONS",         "Analyse", "Déterminer l'ensemble des solutions d'une équation différentielle du type y′ = ay + b", 17),
            ("TERMINALE_STI2D", "PCMMT_EQDIFF_CONDITION_INIT",    "Analyse", "Déterminer la solution de y′ = ay + b vérifiant une condition initiale y(x0) donnée", 18),
            ("TERMINALE_STI2D", "PCMMT_EQDIFF_EULER",             "Analyse", "Approcher par la méthode d'Euler la courbe de l'exponentielle, solution de y′ = y avec y(0) = 1", 19),
            ("TERMINALE_STI2D", "PCMMT_COMP_IDENTIFIER",          "Analyse", "Identifier la composée de deux fonctions dans une expression simple", 20),
            ("TERMINALE_STI2D", "PCMMT_COMP_DERIVEES",            "Analyse", "Calculer la dérivée de (u(x))ⁿ, cos(u(x)), sin(u(x)), e^(u(x)) et ln(u(x))", 21),
            ("TERMINALE_STI2D", "PCMMT_COMP_PRIMITIVES",          "Analyse", "Calculer des primitives de f(ax + b), u′uⁿ, u′/u, u′eᵘ, u′cos u et u′sin u", 22),
            ("TERMINALE_STI2D", "PCMMT_CPX_FORME_EXPONENTIELLE",  "Nombres complexes", "Passer de la forme algébrique d'un nombre complexe à une forme exponentielle r e^(iθ) et inversement", 23),
            ("TERMINALE_STI2D", "PCMMT_CPX_FORMULES_ADDITION",    "Nombres complexes", "Transformer a cos(ωt) + b sin(ωt) en A cos(ωt + φ) et inversement à l'aide des formules d'addition", 24),
            ("TERMINALE_STI2D", "PCMMT_CPX_LINEARISATION",        "Nombres complexes", "Linéariser cos²θ et sin²θ et l'appliquer au calcul de primitives", 25),
            ("TERMINALE_STI2D", "PCMMT_CPX_EQUATIONS",            "Nombres complexes", "Résoudre dans ℂ une équation du premier degré ou du type z² = a pour a réel", 26),
            ("TERMINALE_STI2D", "PCMMT_CPX_TRANSFORMATIONS",      "Nombres complexes", "Interpréter géométriquement z ↦ z + b et z ↦ az pour a réel non nul ou de module 1", 27),
        };
    }
}
