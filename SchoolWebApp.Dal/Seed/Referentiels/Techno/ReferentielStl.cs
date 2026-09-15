namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// Les programmes de la STL, tels que les textes officiels les
    /// définissent — provenance détaillée en tête du fichier. Réparti entre les matières par
    /// `ReferentielsSeriesTechnologiques`.
    /// </summary>
    public static class ReferentielStl
    {
        // =============================================================================
        // RÉFÉRENTIELS DE LA SÉRIE TECHNOLOGIQUE STL (sciences et technologies de laboratoire)
        // Enseignements de spécialité de première et de terminale — hors « Physique-chimie
        // et mathématiques » (traité par un autre agent).
        //
        // PROVENANCE — textes téléchargés et lus le 14/09/2026. Rien n'est écrit de mémoire.
        // Copies locales : techno\sources\stl\ (PDF + .txt extraits par pdftotext -layout).
        //
        // | Tableau | Niveau                | Intitulé exact (page de titre de l'annexe)                                   | Arrêté / NOR                     | BO                                  | En vigueur          | PDF lu |
        // |---------|-----------------------|-------------------------------------------------------------------------------|----------------------------------|-------------------------------------|---------------------|--------|
        // | BB1     | PREMIERE_STL          | « Programme de biochimie-biologie de première STL » (annexe 1)                 | arrêté du 17/01/2019, MENE1901645A | BO spécial n° 1 du 22/01/2019       | rentrée 2019 (art. 2) | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/58/4/spe645_annexe1_1063584.pdf |
        // | BIO1    | PREMIERE_STL_BIOTECH  | « Programme de biotechnologies de première STL » (annexe 2)                    | arrêté du 17/01/2019, MENE1901645A | BO spécial n° 1 du 22/01/2019       | rentrée 2019 (art. 2) | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/86/2/spe645_annexe2_22-1_1063862.pdf |
        // | SPCL1   | PREMIERE_STL_SPCL     | « Programme de sciences physiques et chimiques en laboratoire de première STL » (annexe 4) | arrêté du 17/01/2019, MENE1901645A | BO spécial n° 1 du 22/01/2019 | rentrée 2019 (art. 2) | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/86/6/spe645_annexe4_22-1_1063866.pdf |
        // | BBBT    | TERMINALE_STL_BIOTECH | « Programme de biochimie, biologie et biotechnologies de terminale STL » (annexe 2) | arrêté du 19/07/2019, MENE1921260A | BO spécial n° 8 du 25/07/2019 | rentrée 2020 (art. 2) | https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/16/5/spe260_annexe2_1159165.pdf |
        // | SPCLT   | TERMINALE_STL_SPCL    | « Programme de sciences physiques et chimiques en laboratoire de terminale STL » (annexe 3) | arrêté du 19/07/2019, MENE1921260A | BO spécial n° 8 du 25/07/2019 | rentrée 2020 (art. 2) | https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/16/7/spe260_annexe3_1159167.pdf |
        //
        // Arrêtés lus : https://www.education.gouv.fr/bo/19/Special1/MENE1901645A.htm (4 annexes :
        // 1 biochimie-biologie, 2 biotechnologies, 3 physique-chimie et mathématiques, 4 SPCL) et
        // https://www.education.gouv.fr/bo/19/Special8/MENE1921260A.htm (3 annexes : 1 physique-chimie et
        // mathématiques, 2 biochimie-biologie-biotechnologies, 3 SPCL). Chaque annexe identifiée par sa
        // propre page de titre, pas par le sommaire.
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : la page éduscol « Programmes et ressources en série STL »
        // (https://eduscol.education.gouv.fr/5859/..., relue le 14/09/2026) ne liste que ces deux BO de 2019.
        // Aucun texte de 2020 à 2026 modifiant ces programmes n'a été trouvé. Les notes de service de
        // 2020 à 2024 ne touchent que le PÉRIMÈTRE DES ÉPREUVES, pas les programmes (voir stl.prof.md).
        //
        // LECTURE : les programmes de biologie sont écrits en trois colonnes (savoir-faire / concepts /
        // activités technologiques). Seules les deux premières sont « objectifs en fin de formation » :
        // les libellés en viennent. La colonne « activités » est laissée au professeur. Les programmes
        // de SPCL sont en deux colonnes (notions et contenus / capacités exigibles).
        // Codes : ASCII, <= 40 caractères. Ordre : 1..n dans l'ordre du programme, par tableau.
        // =============================================================================
        
        // -----------------------------------------------------------------------------
        // 1. PREMIÈRE STL — BIOCHIMIE-BIOLOGIE (enseignement suivi par tous les élèves de STL)
        //    Deux modules thématiques (nutrition ; reproduction et hérédité) et quatre modules
        //    transversaux (A structure/propriétés, B structure/fonction, C homéostasie, D information).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] BB1 =
        {
            ("PREMIERE_STL", "BB1_DIG_BIOMOLECULES_ALIMENTS",   "Nutrition : digestion", "Identifier les biomolécules qui composent les aliments (glucides, lipides, protides, vitamines)", 1),
            ("PREMIERE_STL", "BB1_DIG_BESOINS_NUTRITIONNELS",   "Nutrition : digestion", "Déterminer les besoins nutritionnels qualitatifs et quantitatifs (acides aminés et acides gras essentiels)", 2),
            ("PREMIERE_STL", "BB1_DIG_DESSIN_APPAREIL",         "Nutrition : digestion", "Représenter par un dessin les organes du tube digestif et les glandes annexes", 3),
            ("PREMIERE_STL", "BB1_DIG_HISTOLOGIE_ORGANE",       "Nutrition : digestion", "Établir un lien entre la fonction d'un organe digestif et son organisation histologique", 4),
            ("PREMIERE_STL", "BB1_DIG_SIMPLIFICATION",          "Nutrition : digestion", "Présenter la digestion comme une simplification moléculaire et construire le schéma du devenir des biomolécules", 5),
            ("PREMIERE_STL", "BB1_DIG_MECANISMES",              "Nutrition : digestion", "Distinguer les mécanismes des digestions enzymatique, chimique et mécanique", 6),
            ("PREMIERE_STL", "BB1_DIG_MICROBIOTE",              "Nutrition : digestion", "Montrer que la digestion est dépendante du métabolisme microbien (microbiote, symbiose)", 7),
            ("PREMIERE_STL", "BB1_DIG_ABSORPTION_PAROI",        "Nutrition : digestion", "Expliquer le lien entre la structure de la paroi intestinale et sa fonction d'absorption", 8),
            ("PREMIERE_STL", "BB1_DIG_CIRCULATION_NUTRIMENTS",  "Nutrition : digestion", "Mettre en relation la nature des nutriments et leur circulation sanguine ou lymphatique", 9),
            ("PREMIERE_STL", "BB1_DIG_STOCKAGE_NUTRIMENTS",     "Nutrition : digestion", "Schématiser le stockage et la libération des nutriments dans les hépatocytes et les adipocytes", 10),
            ("PREMIERE_STL", "BB1_DIG_REGULATION_GLYCEMIE",     "Nutrition : digestion", "Expliquer le rôle de l'insuline et du glucagon dans la régulation de la glycémie", 11),
            ("PREMIERE_STL", "BB1_EXC_DESSIN_APPAREIL",         "Nutrition : excrétion", "Représenter par un dessin les organes de l'appareil urinaire", 12),
            ("PREMIERE_STL", "BB1_EXC_ROLES_REIN",              "Nutrition : excrétion", "Illustrer les rôles du rein dans l'élimination de l'eau et des métabolites", 13),
            ("PREMIERE_STL", "BB1_EXC_FORMATION_URINE",         "Nutrition : excrétion", "Expliquer la filtration, la réabsorption et l'excrétion et leur localisation dans le néphron", 14),
            ("PREMIERE_STL", "BB1_EXC_CORPUSCULE",              "Nutrition : excrétion", "Mettre en relation la structure du corpuscule rénal et sa fonction de filtration", 15),
            ("PREMIERE_STL", "BB1_EXC_REABSORPTION_GLUCOSE",    "Nutrition : excrétion", "Expliquer à partir d'un schéma le mécanisme moléculaire de la réabsorption du glucose", 16),
            ("PREMIERE_STL", "BB1_EXC_ADH",                     "Nutrition : excrétion", "Expliquer à partir d'un schéma la réabsorption d'eau régulée par l'ADH", 17),
            ("PREMIERE_STL", "BB1_REP_APPAREILS",               "Physiologie de la reproduction", "Représenter par un dessin les appareils reproducteurs et associer une fonction à leurs organes", 18),
            ("PREMIERE_STL", "BB1_REP_TESTOSTERONE",            "Physiologie de la reproduction", "Expliquer le contrôle de la gamétogenèse et des caractères sexuels secondaires masculins par la testostérone", 19),
            ("PREMIERE_STL", "BB1_REP_CYCLE_OVARIEN",           "Physiologie de la reproduction", "Expliquer le caractère cyclique de l'ovaire en lien avec les hormones hypophysaires FSH et LH", 20),
            ("PREMIERE_STL", "BB1_REP_CYCLE_UTERIN",            "Physiologie de la reproduction", "Expliquer l'évolution de l'endomètre selon les concentrations en œstrogènes et en progestérone", 21),
            ("PREMIERE_STL", "BB1_REP_AXES_GONADOTROPES",       "Physiologie de la reproduction", "Expliquer la régulation des axes gonadotropes féminin et masculin (rétrocontrôles négatif et positif)", 22),
            ("PREMIERE_STL", "BB1_REP_CONTRACEPTION",           "Physiologie de la reproduction", "Mettre en relation le mode d'action d'une contraception hormonale et le fonctionnement des appareils génitaux", 23),
            ("PREMIERE_STL", "BB1_GEN_MEIOSE_GAMETES",          "Génétique moléculaire", "Expliquer la production de gamètes haploïdes par méiose et identifier les stades de la gamétogenèse", 24),
            ("PREMIERE_STL", "BB1_GEN_FECONDATION",             "Génétique moléculaire", "Décrire la chronologie de la fécondation et montrer ses conséquences chromosomiques", 25),
            ("PREMIERE_STL", "BB1_GEN_BRASSAGES",               "Génétique moléculaire", "Expliquer par un dessin les brassages interchromosomique et intrachromosomique", 26),
            ("PREMIERE_STL", "BB1_GEN_GENOTYPE_PHENOTYPE",      "Génétique moléculaire", "Mettre en relation génotype et phénotype à l'échelle moléculaire à l'aide d'un exemple", 27),
            ("PREMIERE_STL", "BB1_GEN_ARBRE_GENEALOGIQUE",      "Génétique moléculaire", "Identifier le mode de transmission d'un caractère héréditaire à partir d'un arbre généalogique", 28),
            ("PREMIERE_STL", "BB1_BIOM_FONCTIONS_CHIMIQUES",    "Structures et propriétés des biomolécules", "Identifier les fonctions chimiques (alcool, aldéhyde, cétone, acide carboxylique, amine, amide, ester) d'une molécule", 29),
            ("PREMIERE_STL", "BB1_BIOM_OSES",                   "Structures et propriétés des biomolécules", "Représenter le D-glucose (Haworth, Fischer) et comparer les principaux oses au D-glucose", 30),
            ("PREMIERE_STL", "BB1_BIOM_OSIDES",                 "Structures et propriétés des biomolécules", "Identifier les oses du maltose, du lactose, du saccharose, de l'amidon et du glycogène", 31),
            ("PREMIERE_STL", "BB1_BIOM_ACIDES_AMINES",          "Structures et propriétés des biomolécules", "Représenter un acide α-aminé de la série L et expliquer l'asymétrie du carbone α", 32),
            ("PREMIERE_STL", "BB1_BIOM_PEPTIDES_PROTEINES",     "Structures et propriétés des biomolécules", "Schématiser un peptide et identifier les liaisons de la structure tridimensionnelle d'une protéine", 33),
            ("PREMIERE_STL", "BB1_BIOM_ACIDES_NUCLEIQUES",      "Structures et propriétés des biomolécules", "Repérer les constituants d'un nucléotide, comparer ADN et ARN et expliquer l'appariement des bases", 34),
            ("PREMIERE_STL", "BB1_BIOM_CONFORMATION",           "Structures et propriétés des biomolécules", "Expliquer l'influence des paramètres physico-chimiques sur la conformation des biomolécules", 35),
            ("PREMIERE_STL", "BB1_BIOM_LIPIDES",                "Structures et propriétés des biomolécules", "Représenter un acide gras saturé ou mono-insaturé et reconnaître le noyau stérane", 36),
            ("PREMIERE_STL", "BB1_BIOM_POLARITE_MEMBRANE",      "Structures et propriétés des biomolécules", "Prévoir les interactions d'une biomolécule avec l'eau et représenter une membrane biologique", 37),
            ("PREMIERE_STL", "BB1_STRF_ECHELLES_MICROSCOPIE",   "Structures et fonctions physiologiques", "Estimer l'ordre de grandeur d'un objet et associer un type de microscope à la structure observée", 38),
            ("PREMIERE_STL", "BB1_STRF_ORGANE_TISSU",           "Structures et fonctions physiologiques", "Expliquer la contribution des organes à un appareil et le lien entre tissu épithélial et fonction", 39),
            ("PREMIERE_STL", "BB1_STRF_CELLULE_EUCARYOTE",      "Structures et fonctions physiologiques", "Schématiser une cellule eucaryote et décrire le rôle du réticulum, de l'appareil de Golgi et du noyau", 40),
            ("PREMIERE_STL", "BB1_STRF_IMAGERIE",               "Structures et fonctions physiologiques", "Relier le choix d'une technologie d'imagerie médicale (rayons X, ultrasons) à la nature du tissu observé", 41),
            ("PREMIERE_STL", "BB1_HOMEO_COMPARTIMENTS",         "Milieu intérieur et homéostasie", "Situer et comparer les compartiments liquidiens de l'organisme", 42),
            ("PREMIERE_STL", "BB1_HOMEO_TRANSFERTS",            "Milieu intérieur et homéostasie", "Schématiser le trajet d'une biomolécule et caractériser un transfert de matière entre compartiments", 43),
            ("PREMIERE_STL", "BB1_HOMEO_BOUCLE_REGULATION",     "Milieu intérieur et homéostasie", "Repérer les éléments d'une boucle de régulation au service d'une homéostasie", 44),
            ("PREMIERE_STL", "BB1_INFO_SYNTHESE_PROTEINES",     "Information et communication", "Schématiser la synthèse des protéines (transcription, traduction) à l'aide du code génétique", 45),
            ("PREMIERE_STL", "BB1_INFO_MUTATIONS",              "Information et communication", "Déterminer la conséquence d'une mutation ponctuelle de l'ADN sur la séquence peptidique", 46),
            ("PREMIERE_STL", "BB1_INFO_ADN_MITOSE_MEIOSE",      "Information et communication", "Expliquer la répartition de l'ADN lors de la mitose et de la méiose et tracer la quantité d'ADN", 47),
            ("PREMIERE_STL", "BB1_INFO_COMMUNICATION_HORMONALE","Information et communication", "Schématiser une communication hormonale et identifier les caractéristiques d'une hormone", 48),
            ("PREMIERE_STL", "BB1_INFO_MODE_ACTION_HORMONES",   "Information et communication", "Préciser le mode d'action d'une hormone hydrophile et d'une hormone hydrophobe sur la cellule cible", 49),
        };
        
        // -----------------------------------------------------------------------------
        // 2. PREMIÈRE STL — BIOTECHNOLOGIES (au choix, face à SPCL)
        //    Quatre modules transversaux « Travailler ensemble au laboratoire » (A à D) et huit modules
        //    « Fondamentaux technologiques et scientifiques » (1 à 8). Les « Thématiques » finales sont
        //    des contextes d'activité, pas des objectifs : non reprises.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] BIO1 =
        {
            ("PREMIERE_STL", "BIO1_PROJ_ENJEUX",            "Recherche expérimentale et projet", "Situer les évolutions des biotechnologies et illustrer une application dans chaque domaine", 1),
            ("PREMIERE_STL", "BIO1_PROJ_ETHIQUE",           "Recherche expérimentale et projet", "S'interroger sur les aspects éthiques des biotechnologies pour les êtres vivants et l'environnement", 2),
            ("PREMIERE_STL", "BIO1_PROJ_DEMARCHE",          "Recherche expérimentale et projet", "Formuler un questionnement, proposer une expérience avec témoin, l'exploiter et en rendre compte", 3),
            ("PREMIERE_STL", "BIO1_RISQ_DANGER_RISQUE",     "Prévention des risques", "Identifier un danger biologique, chimique ou électrique et le relier aux risques encourus", 4),
            ("PREMIERE_STL", "BIO1_RISQ_VOIES_EXPOSITION",  "Prévention des risques", "Identifier les voies d'exposition à un danger en lien avec la chaîne de transmission", 5),
            ("PREMIERE_STL", "BIO1_RISQ_ANALYSE",           "Prévention des risques", "Analyser une situation exposante : événements dangereux, probabilité et gravité du dommage", 6),
            ("PREMIERE_STL", "BIO1_RISQ_PREVENTION",        "Prévention des risques", "Mettre en relation les mesures de prévention (EPC, EPI) et l'analyse des risques", 7),
            ("PREMIERE_STL", "BIO1_RISQ_DESINFECTION",      "Prévention des risques", "Appliquer lavage des mains et désinfection de la paillasse et choisir le conteneur à déchets adapté", 8),
            ("PREMIERE_STL", "BIO1_METRO_GRANDEURS",        "Métrologie", "Utiliser les symboles, indices et unités des grandeurs de base et des grandeurs dérivées", 9),
            ("PREMIERE_STL", "BIO1_METRO_INSTRUMENTS",      "Métrologie", "Choisir un instrument selon ses caractéristiques métrologiques et l'utiliser avec sa fiche technique", 10),
            ("PREMIERE_STL", "BIO1_METRO_POINTS_CRITIQUES", "Métrologie", "Repérer les étapes de mesure et les points critiques d'une procédure opératoire", 11),
            ("PREMIERE_STL", "BIO1_METRO_ETALONNAGE",       "Métrologie", "Déterminer une valeur mesurée avec un étalon unique ou une courbe d'étalonnage", 12),
            ("PREMIERE_STL", "BIO1_METRO_MODELE_MESURE",    "Métrologie", "Établir les équations aux grandeurs, aux unités et aux valeurs numériques à partir du modèle de mesure", 13),
            ("PREMIERE_STL", "BIO1_METRO_RESULTAT",         "Métrologie", "Exprimer le résultat de mesure en écriture scientifique avec l'incertitude donnée", 14),
            ("PREMIERE_STL", "BIO1_METRO_ACCEPTABILITE",    "Métrologie", "Vérifier l'exactitude et l'acceptabilité des valeurs mesurées à l'aide d'un étalon de contrôle", 15),
            ("PREMIERE_STL", "BIO1_NUM_VISUALISATION_3D",   "Outils numériques", "Utiliser un logiciel de visualisation 3D de molécules d'intérêt biologique", 16),
            ("PREMIERE_STL", "BIO1_NUM_RECHERCHE_DOC",      "Outils numériques", "Consulter des bases de données, trier les ressources et élaborer une bibliographie", 17),
            ("PREMIERE_STL", "BIO1_NUM_TABLEUR",            "Outils numériques", "Exploiter des résultats expérimentaux avec un tableur ou un logiciel dédié", 18),
            ("PREMIERE_STL", "BIO1_NUM_COMMUNICATION",      "Outils numériques", "Partager des documents en ligne et réaliser un support oral ou un écrit rendant compte d'une démarche", 19),
            ("PREMIERE_STL", "BIO1_MICRO_ETAT_FRAIS_GRAM",  "Observer le vivant microscopique", "Réaliser un état frais d'une suspension bactérienne et mettre en œuvre la coloration de Gram", 20),
            ("PREMIERE_STL", "BIO1_MICRO_MICROSCOPE",       "Observer le vivant microscopique", "Maîtriser l'utilisation du microscope optique et estimer la taille d'un élément microscopique", 21),
            ("PREMIERE_STL", "BIO1_MICRO_DESSIN",           "Observer le vivant microscopique", "Dessiner une observation microscopique et la compléter par un titre, une échelle et des annotations", 22),
            ("PREMIERE_STL", "BIO1_MICRO_TYPES_CELLULAIRES","Observer le vivant microscopique", "Distinguer bactérie, micro-algue et levure et différencier clichés de microscopie optique et électronique", 23),
            ("PREMIERE_STL", "BIO1_CULT_ASEPSIE",           "Cultiver des micro-organismes", "Organiser le poste de travail et manipuler en conditions d'asepsie avec des milieux stériles", 24),
            ("PREMIERE_STL", "BIO1_CULT_MILIEUX",           "Cultiver des micro-organismes", "Choisir un milieu de culture adapté aux besoins nutritionnels et aux paramètres physico-chimiques", 25),
            ("PREMIERE_STL", "BIO1_CULT_MILIEU_SELECTIF",   "Cultiver des micro-organismes", "Choisir un milieu sélectif ou d'orientation en vue d'isoler un micro-organisme d'intérêt", 26),
            ("PREMIERE_STL", "BIO1_CULT_PREPARATION_MILIEU","Cultiver des micro-organismes", "Préparer et conditionner un milieu de culture et repérer barème de stérilisation et manomètre d'un autoclave", 27),
            ("PREMIERE_STL", "BIO1_CULT_ENSEMENCEMENT",     "Cultiver des micro-organismes", "Ensemencer un milieu liquide ou solide et définir température et durée d'incubation", 28),
            ("PREMIERE_STL", "BIO1_CULT_COLONIES",          "Cultiver des micro-organismes", "Décrire une colonie bactérienne par ses caractères macroscopiques et repérer un contaminant", 29),
            ("PREMIERE_STL", "BIO1_IDENT_NOMENCLATURE",     "Caractériser pour identifier", "Utiliser les règles d'écriture de la nomenclature des bactéries (famille, genre, espèce)", 30),
            ("PREMIERE_STL", "BIO1_IDENT_MORPHOLOGIE",      "Caractériser pour identifier", "Déterminer forme, taille et groupement des bactéries et distinguer levures et bactéries", 31),
            ("PREMIERE_STL", "BIO1_DENOMB_NUMERATION",      "Dénombrer des micro-organismes", "Réaliser une numération directe au microscope en cytomètre manuel (hématimètre)", 32),
            ("PREMIERE_STL", "BIO1_DENOMB_DILUTIONS",       "Dénombrer des micro-organismes", "Préparer une suspension et calculer puis effectuer des dilutions décimales", 33),
            ("PREMIERE_STL", "BIO1_DENOMB_UFC",             "Dénombrer des micro-organismes", "Exploiter un dénombrement après culture en milieu solide (UFC) au regard d'un critère microbiologique", 34),
            ("PREMIERE_STL", "BIO1_SOL_PROCEDURE",          "Préparer des solutions", "Concevoir une procédure de préparation de solution par pesée ou par dilution", 35),
            ("PREMIERE_STL", "BIO1_SOL_REALISATION",        "Préparer des solutions", "Choisir le matériel de précision et réaliser pesée et mesure de volume avec une gestuelle maîtrisée", 36),
            ("PREMIERE_STL", "BIO1_BIOM_REACTIF_CHIMIQUE",  "Détecter les biomolécules", "Détecter une biomolécule par un réactif chimique et analyser le résultat à l'aide de témoins", 37),
            ("PREMIERE_STL", "BIO1_BIOM_SPECTRE",           "Détecter les biomolécules", "Réaliser un spectre d'absorption et déterminer la longueur d'onde optimale", 38),
            ("PREMIERE_STL", "BIO1_BIOM_ENZYME",            "Détecter les biomolécules", "Détecter une enzyme par son activité à pH et température fixés", 39),
            ("PREMIERE_STL", "BIO1_SEP_CCM",                "Séparer les composants d'un mélange", "Réaliser une chromatographie sur couche mince et identifier les biomolécules par comparaison à des étalons", 40),
            ("PREMIERE_STL", "BIO1_SEP_ECHANGE_IONS",       "Séparer les composants d'un mélange", "Réaliser une chromatographie d'échange d'ions et expliquer les liaisons ioniques mises en jeu", 41),
            ("PREMIERE_STL", "BIO1_DOSAGE_SPECTRO",         "Doser une biomolécule", "Doser une biomolécule par spectrophotométrie (loi de Beer-Lambert, gamme d'étalonnage)", 42),
            ("PREMIERE_STL", "BIO1_DOSAGE_VOLUMETRIE",      "Doser une biomolécule", "Doser une biomolécule par volumétrie et déterminer le volume équivalent avec un indicateur coloré", 43),
        };
        
        // -----------------------------------------------------------------------------
        // 3. PREMIÈRE STL — SCIENCES PHYSIQUES ET CHIMIQUES EN LABORATOIRE (au choix, face à biotechnologies)
        //    Mesure et incertitudes ; Chimie et développement durable ; Image ; Instrumentation ;
        //    Ouverture vers la recherche ou l'industrie (démarche de projet).
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SPCL1 =
        {
            ("PREMIERE_STL", "SPCL1_MES_SOURCES_ERREURS",     "Mesure et incertitudes", "Identifier les principales sources d'erreurs lors d'une mesure", 1),
            ("PREMIERE_STL", "SPCL1_MES_JUSTESSE_FIDELITE",   "Mesure et incertitudes", "Exploiter des séries de mesures pour comparer des méthodes en termes de justesse et de fidélité", 2),
            ("PREMIERE_STL", "SPCL1_MES_TYPE_A_B",            "Mesure et incertitudes", "Procéder à une évaluation de type A ou de type B d'une incertitude-type", 3),
            ("PREMIERE_STL", "SPCL1_MES_EXPRESSION",          "Mesure et incertitudes", "Exprimer un résultat de mesure avec les chiffres significatifs adaptés et l'incertitude-type associée", 4),
            ("PREMIERE_STL", "SPCL1_MES_VALIDITE",            "Mesure et incertitudes", "Discuter la validité d'un résultat en comparant son écart à la valeur de référence à l'incertitude-type", 5),
            ("PREMIERE_STL", "SPCL1_SECU_REGLES",             "Chimie : sécurité et environnement", "Appliquer les règles de sécurité à l'aide des pictogrammes, des phrases H et P et des fiches de données de sécurité", 6),
            ("PREMIERE_STL", "SPCL1_SECU_ETIQUETTE_CLP",      "Chimie : sécurité et environnement", "Exploiter une étiquette CLP et justifier le mode d'élimination d'une espèce chimique", 7),
            ("PREMIERE_STL", "SPCL1_SECU_CHIMIE_VERTE",       "Chimie : sécurité et environnement", "Appliquer les principes de la chimie verte pour choisir un procédé de synthèse ou d'analyse", 8),
            ("PREMIERE_STL", "SPCL1_SYNTH_MONTAGE_REFLUX",    "Chimie : synthèses", "Prélever les réactifs et justifier puis réaliser un montage à reflux avec ampoule de coulée", 9),
            ("PREMIERE_STL", "SPCL1_SYNTH_SEPARATION",        "Chimie : synthèses", "Choisir un solvant et réaliser extraction, distillation simple, recristallisation et filtration sous vide", 10),
            ("PREMIERE_STL", "SPCL1_SYNTH_PURETE",            "Chimie : synthèses", "Contrôler la pureté d'un produit par CCM et par mesure d'une température de fusion", 11),
            ("PREMIERE_STL", "SPCL1_SYNTH_RENDEMENT",         "Chimie : synthèses", "Déterminer le réactif limitant et calculer le rendement en produit purifié", 12),
            ("PREMIERE_STL", "SPCL1_SYNTH_TYPE_REACTION",     "Chimie : synthèses", "Déterminer le type d'une réaction : substitution, addition, élimination ou acide-base", 13),
            ("PREMIERE_STL", "SPCL1_SYNTH_SITES_REACTIFS",    "Chimie : synthèses", "Identifier les sites électrophiles et nucléophiles des réactifs d'une synthèse", 14),
            ("PREMIERE_STL", "SPCL1_SYNTH_HYDROGENE_LABILE",  "Chimie : synthèses", "Identifier l'hydrogène labile et comparer l'acidité des alcools et des acides carboxyliques par mésomérie", 15),
            ("PREMIERE_STL", "SPCL1_SYNTH_FLECHES_COURBES",   "Chimie : synthèses", "Représenter un mécanisme réactionnel par des flèches courbes de mouvement de doublets d'électrons", 16),
            ("PREMIERE_STL", "SPCL1_SYNTH_ALCOOLS",           "Chimie : synthèses", "Interpréter la réactivité des alcools et écrire l'équation d'une hydrogénation ou d'une déshydratation", 17),
            ("PREMIERE_STL", "SPCL1_ANA_IDENTIFICATION",      "Chimie : analyses physico-chimiques", "Détecter un ion avec un témoin et mesurer température de changement d'état et masse volumique", 18),
            ("PREMIERE_STL", "SPCL1_ANA_SPECTROSCOPIES",      "Chimie : analyses physico-chimiques", "Relier structure moléculaire et rayonnement absorbé et exploiter des spectres UV-visible et IR", 19),
            ("PREMIERE_STL", "SPCL1_ANA_BEER_LAMBERT",        "Chimie : analyses physico-chimiques", "Déterminer une concentration par étalonnage spectrophotométrique (loi de Beer-Lambert et ses limites)", 20),
            ("PREMIERE_STL", "SPCL1_ANA_TITRAGE",             "Chimie : analyses physico-chimiques", "Réaliser un dosage par titrage et déterminer l'équivalence par changement de couleur ou pH-métrie", 21),
            ("PREMIERE_STL", "SPCL1_ANA_PKA_COURBE",          "Chimie : analyses physico-chimiques", "Estimer une valeur approchée de pKa par analyse d'une courbe de dosage pH-métrique", 22),
            ("PREMIERE_STL", "SPCL1_IMG_HISTOIRE_DROITS",     "Image", "Situer les dates clés de l'histoire de l'image et respecter droits d'auteur et droit à l'image", 23),
            ("PREMIERE_STL", "SPCL1_IMG_OEIL",                "Image", "Exploiter un modèle optique de l'œil pour expliquer accommodation, myopie et hypermétropie", 24),
            ("PREMIERE_STL", "SPCL1_IMG_VISION_COULEURS",     "Image", "Expliquer la vision des couleurs et le daltonisme à l'aide des cellules photosensibles de la rétine", 25),
            ("PREMIERE_STL", "SPCL1_IMG_SYNTHESES_COULEURS",  "Image", "Distinguer synthèses additive et soustractive des couleurs et exploiter le modèle RVB", 26),
            ("PREMIERE_STL", "SPCL1_IMG_FILTRES",             "Image", "Prévoir l'effet de filtres et la couleur perçue d'un objet éclairé par une lumière colorée", 27),
            ("PREMIERE_STL", "SPCL1_IMG_LENTILLES",           "Image", "Exploiter la relation de conjugaison et le grandissement d'une lentille mince convergente", 28),
            ("PREMIERE_STL", "SPCL1_IMG_FOCOMETRIE",          "Image", "Mesurer une distance focale par autocollimation et par la méthode de Bessel", 29),
            ("PREMIERE_STL", "SPCL1_IMG_LOUPE",               "Image", "Expliquer pourquoi l'image d'un objet réel donnée par une loupe n'est pas réelle", 30),
            ("PREMIERE_STL", "SPCL1_IMG_APPAREIL_PHOTO",      "Image", "Modéliser un appareil photographique numérique : nombre d'ouverture, temps de pose, angle et profondeur de champ", 31),
            ("PREMIERE_STL", "SPCL1_IMG_CAPTEUR_PIXEL",       "Image", "Expliquer le principe d'un capteur CCD et relier pixel, résolution et sensibilité", 32),
            ("PREMIERE_STL", "SPCL1_IMG_CODAGE_STOCKAGE",     "Image", "Expliquer le codage en niveaux de gris et RVB et évaluer la taille d'une image en octets", 33),
            ("PREMIERE_STL", "SPCL1_IMG_TRANSMISSION",        "Image", "Caractériser une transmission numérique par son débit binaire et prévoir une durée de transmission", 34),
            ("PREMIERE_STL", "SPCL1_INST_CHOIX_INSTRUMENT",   "Instrumentation", "Choisir un instrument de mesure selon sa résolution, son temps de réponse et son étendue de mesure", 35),
            ("PREMIERE_STL", "SPCL1_INST_CHAINE_BLOCS",       "Instrumentation", "Identifier les blocs d'une chaîne de mesure : capteur, conditionneur, CAN, calculateur, afficheur", 36),
            ("PREMIERE_STL", "SPCL1_INST_ETALONNAGE",         "Instrumentation", "Tracer et exploiter une courbe d'étalonnage et vérifier que la gamme est adaptée", 37),
            ("PREMIERE_STL", "SPCL1_INST_CAPTEUR_TRANSFERT",  "Instrumentation", "Exploiter la caractéristique de transfert et la sensibilité d'un ensemble capteur-conditionneur", 38),
            ("PREMIERE_STL", "SPCL1_INST_CONDITIONNEUR",      "Instrumentation", "Concevoir un conditionneur par pont diviseur et un montage à ALI à l'aide des lois des circuits", 39),
            ("PREMIERE_STL", "SPCL1_INST_CAN",                "Instrumentation", "Relier quantum, résolution et tension pleine échelle d'un CAN et expliquer son impact sur la mesure", 40),
            ("PREMIERE_STL", "SPCL1_INST_TOUT_OU_RIEN",       "Instrumentation", "Réaliser une chaîne de mesure en tout ou rien avec un microcontrôleur et fixer un seuil de déclenchement", 41),
            ("PREMIERE_STL", "SPCL1_INST_REGULATION_TEMP",    "Instrumentation", "Réguler une température en tout ou rien avec hystérésis et caractériser l'influence des seuils", 42),
            ("PREMIERE_STL", "SPCL1_PROJ_DEMARCHE",           "Démarche de projet", "Conduire une étude de cas ou un mini-projet : problématique, stratégie, expérience, note concise et oral", 43),
        };
        
        // -----------------------------------------------------------------------------
        // 4. TERMINALE STL — BIOCHIMIE, BIOLOGIE ET BIOTECHNOLOGIES (élèves ayant suivi biotechnologies)
        //    Partie S (concepts scientifiques, S1 à S4), partie T (fondamentaux technologiques, T1 à T10),
        //    partie L (travailler ensemble au laboratoire, L1 à L4). Programme le plus volumineux :
        //    53 lignes, au-delà de la fourchette habituelle, parce qu'il fusionne trois disciplines.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] BBBT =
        {
            ("TERMINALE_STL", "BBBT_S1_VOIES_METABOLIQUES",   "Enzymes et voies métaboliques", "Caractériser une chaîne de réactions biochimiques d'anabolisme ou de catabolisme", 1),
            ("TERMINALE_STL", "BBBT_S1_ENTHALPIE_LIBRE",      "Enzymes et voies métaboliques", "Déduire le sens d'évolution d'une réaction de son enthalpie libre et calculer celle de réactions couplées", 2),
            ("TERMINALE_STL", "BBBT_S1_ATP",                  "Enzymes et voies métaboliques", "Expliquer le rôle de l'ATP comme molécule énergétique intermédiaire du métabolisme", 3),
            ("TERMINALE_STL", "BBBT_S1_OXYDOREDUCTION",       "Enzymes et voies métaboliques", "Écrire des demi-équations d'oxydo-réduction et prévoir le sens d'une réaction à partir des potentiels E'°", 4),
            ("TERMINALE_STL", "BBBT_S1_RESPIRATION",          "Enzymes et voies métaboliques", "Établir les bilans de la glycolyse et décrire la chaîne respiratoire couplée à l'ATP synthase", 5),
            ("TERMINALE_STL", "BBBT_S1_PHOTOSYNTHESE",        "Enzymes et voies métaboliques", "Localiser la photosynthèse et relier chaîne membranaire photosynthétique et cycle de Calvin", 6),
            ("TERMINALE_STL", "BBBT_S1_FERMENTATION",         "Enzymes et voies métaboliques", "Établir le bilan d'une fermentation et la distinguer d'une respiration (accepteur final, rendement)", 7),
            ("TERMINALE_STL", "BBBT_S1_TYPES_TROPHIQUES",     "Enzymes et voies métaboliques", "Associer les types trophiques aux caractéristiques nutritionnelles et aux conditions de culture", 8),
            ("TERMINALE_STL", "BBBT_S1_CYCLES_C_N",           "Enzymes et voies métaboliques", "Identifier les interactions des micro-organismes et compléter les cycles du carbone et de l'azote", 9),
            ("TERMINALE_STL", "BBBT_S1_ENZYMES",              "Enzymes et voies métaboliques", "Caractériser un catalyseur biologique et analyser l'effet des conditions et des effecteurs sur son activité", 10),
            ("TERMINALE_STL", "BBBT_S1_CINETIQUE",            "Enzymes et voies métaboliques", "Mesurer une vitesse initiale et interpréter sa variation selon la concentration en substrat", 11),
            ("TERMINALE_STL", "BBBT_S2_SOI_NON_SOI",          "Immunité cellulaire et moléculaire", "Expliquer la notion de non-soi et mettre en relation une barrière naturelle et sa fonction", 12),
            ("TERMINALE_STL", "BBBT_S2_IMMUNITE_INNEE",       "Immunité cellulaire et moléculaire", "Décrire la reconnaissance par les cellules sentinelles, la réaction inflammatoire et la phagocytose", 13),
            ("TERMINALE_STL", "BBBT_S2_LYMPHOCYTES_T",        "Immunité cellulaire et moléculaire", "Expliquer la présentation de l'antigène et l'activation des lymphocytes T4 et T8", 14),
            ("TERMINALE_STL", "BBBT_S2_LYMPHOCYTES_B",        "Immunité cellulaire et moléculaire", "Expliquer l'activation d'un lymphocyte B en plasmocyte et la mémoire immunitaire", 15),
            ("TERMINALE_STL", "BBBT_S2_ANTICORPS",            "Immunité cellulaire et moléculaire", "Relier la structure des immunoglobulines à leurs rôles in vivo et à leur usage dans une procédure", 16),
            ("TERMINALE_STL", "BBBT_S2_VACCINS",              "Immunité cellulaire et moléculaire", "Identifier les constituants d'un vaccin et distinguer vaccination et sérothérapie", 17),
            ("TERMINALE_STL", "BBBT_S3_STRUCTURE_ADN",        "Propriétés de l'ADN et réplication", "Représenter la structure de l'ADN et en déduire ses propriétés physico-chimiques", 18),
            ("TERMINALE_STL", "BBBT_S3_CHROMOSOME_REPLICATION","Propriétés de l'ADN et réplication", "Représenter l'organisation d'un chromosome et légender le mécanisme de la réplication", 19),
            ("TERMINALE_STL", "BBBT_S3_CYCLE_CANCER",         "Propriétés de l'ADN et réplication", "Relier cycle cellulaire et cancer, différenciation et expression des gènes, cellules souches", 20),
            ("TERMINALE_STL", "BBBT_S4_BACTERIE",             "Micro-organismes et biotechnologies", "Schématiser une bactérie, comparer les parois Gram + et Gram - et expliquer leurs rôles", 21),
            ("TERMINALE_STL", "BBBT_S4_EUCARYOTES",           "Micro-organismes et biotechnologies", "Identifier l'ultrastructure d'une levure, d'une moisissure et d'une microalgue", 22),
            ("TERMINALE_STL", "BBBT_S4_HOTE_MICROBIOTE",      "Micro-organismes et biotechnologies", "Distinguer les interactions hôte humain - micro-organismes et localiser les microbiotes humains", 23),
            ("TERMINALE_STL", "BBBT_S4_BIO_INDUSTRIES",       "Micro-organismes et biotechnologies", "Identifier l'intérêt d'un micro-organisme en bioproduction, en dépollution et en contrôle microbiologique", 24),
            ("TERMINALE_STL", "BBBT_S4_VIRUS",                "Micro-organismes et biotechnologies", "Identifier la structure d'un virus et comparer les cycles viraux lytique et lysogène", 25),
            ("TERMINALE_STL", "BBBT_S4_VIH",                  "Micro-organismes et biotechnologies", "Repérer les stades de l'infection par le VIH et associer traitements et moyens de prévention", 26),
            ("TERMINALE_STL", "BBBT_T1_MICROSCOPIES",         "Observer la diversité du vivant", "Choisir un type de microscopie et mettre en œuvre une coloration spécifique à l'aide d'une fiche technique", 27),
            ("TERMINALE_STL", "BBBT_T2_POLYMICROBIEN",        "Cultiver et limiter la croissance", "Identifier les étapes de recherche d'un micro-organisme d'intérêt dans un produit polymicrobien", 28),
            ("TERMINALE_STL", "BBBT_T2_CROISSANCE",           "Cultiver et limiter la croissance", "Suivre une croissance et déterminer ses phases, le temps de génération et la vitesse spécifique", 29),
            ("TERMINALE_STL", "BBBT_T2_ANTIMICROBIENS",       "Cultiver et limiter la croissance", "Classer les agents antimicrobiens et exploiter un antibiogramme pour proposer un antibiotique adapté", 30),
            ("TERMINALE_STL", "BBBT_T3_METABOLISME",          "Caractériser pour identifier", "Explorer le métabolisme microbien : type respiratoire, indicateur de pH, exo-enzymes, auxanogramme", 31),
            ("TERMINALE_STL", "BBBT_T3_IDENTIFICATION",       "Caractériser pour identifier", "Identifier une souche pure par une démarche raisonnée : oxydase, catalase, galerie, méthode probabiliste", 32),
            ("TERMINALE_STL", "BBBT_T4_DENOMBREMENT",         "Dénombrer des micro-organismes", "Réaliser une numération avec test de viabilité et un dénombrement par filtration sur membrane", 33),
            ("TERMINALE_STL", "BBBT_T5_MICROVOLUMES",         "Solutions en biologie moléculaire", "Calculer et pipeter des micro-volumes pour un mix réactionnel, puis étiqueter et stocker les solutions", 34),
            ("TERMINALE_STL", "BBBT_T6_IMMUNODETECTION",      "Détecter les biomolécules", "Mettre en œuvre agglutination, précipitation et réaction immuno-enzymatique avec des témoins adaptés", 35),
            ("TERMINALE_STL", "BBBT_T7_FRACTIONNEMENT",       "Extraire, séparer, purifier", "Fractionner un mélange par filtration et centrifugation et purifier par chromatographie d'exclusion", 36),
            ("TERMINALE_STL", "BBBT_T7_ELECTROPHORESE",       "Extraire, séparer, purifier", "Prévoir le sens de migration et interpréter un électrophorégramme", 37),
            ("TERMINALE_STL", "BBBT_T7_PURIFICATION_ENZYME",  "Extraire, séparer, purifier", "Établir le tableau de suivi d'une purification d'enzyme (activité spécifique, rendement)", 38),
            ("TERMINALE_STL", "BBBT_T8_DOSAGE_SUBSTRAT",      "Doser une biomolécule", "Doser un substrat par méthode enzymatique en point final à partir de réactions couplées", 39),
            ("TERMINALE_STL", "BBBT_T8_ACTIVITE_ENZYMATIQUE", "Doser une biomolécule", "Doser une activité enzymatique en continu ou « deux points » et calculer z et b", 40),
            ("TERMINALE_STL", "BBBT_T8_DOSAGE_IMMUNO",        "Doser une biomolécule", "Doser une molécule par réaction antigène-anticorps et interpréter une méthode immuno-enzymatique", 41),
            ("TERMINALE_STL", "BBBT_T9_EXTRACTION_ADN",       "Technologies de l'ADN", "Extraire et purifier de l'ADN et contrôler l'efficacité de l'extraction", 42),
            ("TERMINALE_STL", "BBBT_T9_PCR",                  "Technologies de l'ADN", "Mettre en œuvre une PCR et en vérifier le résultat par électrophorèse", 43),
            ("TERMINALE_STL", "BBBT_T9_RESTRICTION_CLONAGE",  "Technologies de l'ADN", "Choisir une enzyme de restriction, prévoir la taille des fragments et décrire les étapes d'un clonage", 44),
            ("TERMINALE_STL", "BBBT_T9_ENJEUX",               "Technologies de l'ADN", "S'interroger sur la dimension éthique des technologies de l'ADN et sur la vulgarisation scientifique", 45),
            ("TERMINALE_STL", "BBBT_T10_VEGETAL",             "Technologies cellulaires végétales", "Expliquer la culture in vitro des végétaux et repérer le principe de la transgenèse végétale", 46),
            ("TERMINALE_STL", "BBBT_L1_METHODES",             "Démarche de projet", "Identifier le type de méthode expérimentale (recherche, analyse, contrôle, production) et l'enjeu d'une activité", 47),
            ("TERMINALE_STL", "BBBT_L1_PROJET",               "Démarche de projet", "Conduire un projet : diagnostic, objectifs opérationnels, hypothèse, expérience, suivi et valorisation", 48),
            ("TERMINALE_STL", "BBBT_L2_PREVENTION",           "Prévention des risques", "Évaluer les risques d'une situation de travail et proposer des mesures de prévention (EPC, EPI, déchets)", 49),
            ("TERMINALE_STL", "BBBT_L3_FIDELITE_JUSTESSE",    "Résultats de mesure fiables", "Quantifier justesse et fidélité d'une série de mesures et distinguer répétabilité et reproductibilité", 50),
            ("TERMINALE_STL", "BBBT_L3_ACCEPTABILITE",        "Résultats de mesure fiables", "Vérifier l'acceptabilité et la compatibilité de valeurs mesurées et repérer les sources d'incertitude (5 M)", 51),
            ("TERMINALE_STL", "BBBT_L4_BIOINFORMATIQUE",      "Outils numériques", "Interroger une banque de séquences, rechercher un motif et modéliser un phénomène par un programme simple", 52),
            ("TERMINALE_STL", "BBBT_L4_ETHIQUE_NUMERIQUE",    "Outils numériques", "Discuter la fiabilité des sources et respecter propriété intellectuelle et données personnelles", 53),
        };
        
        // -----------------------------------------------------------------------------
        // 5. TERMINALE STL — SCIENCES PHYSIQUES ET CHIMIQUES EN LABORATOIRE
        //    Mesure et incertitudes ; Projet ; Chimie et développement durable ; Ondes ; Systèmes et procédés.
        // -----------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SPCLT =
        {
            ("TERMINALE_STL", "SPCLT_MES_INCERTITUDES",        "Mesure et incertitudes", "Évaluer une incertitude-type de type A, de type B ou composée à l'aide d'une relation ou d'un logiciel", 1),
            ("TERMINALE_STL", "SPCLT_MES_SOURCES_ERREURS",     "Mesure et incertitudes", "Comparer le poids des sources d'erreurs et proposer des améliorations du protocole", 2),
            ("TERMINALE_STL", "SPCLT_MES_VALIDATION",          "Mesure et incertitudes", "Valider un résultat par rapport à une valeur de référence et comparer des protocoles en justesse et fidélité", 3),
            ("TERMINALE_STL", "SPCLT_PROJ_CONDUITE",           "Projet", "Conduire un projet d'équipe : problématique, recherche documentaire, stratégie, planification", 4),
            ("TERMINALE_STL", "SPCLT_PROJ_RESTITUTION",        "Projet", "Analyser les résultats d'un projet, produire des écrits de synthèse et soutenir une présentation orale", 5),
            ("TERMINALE_STL", "SPCLT_CHIM_SOLUBILITE",         "Chimie : composition des systèmes", "Exprimer Ks, prévoir une précipitation par comparaison de Qr et Ks et déterminer une solubilité", 6),
            ("TERMINALE_STL", "SPCLT_CHIM_PRECIPITATION_PH",   "Chimie : composition des systèmes", "Déterminer une gamme de pH de précipitation sélective pour un mélange d'hydroxydes", 7),
            ("TERMINALE_STL", "SPCLT_CHIM_KA_DISSOCIATION",    "Chimie : composition des systèmes", "Comparer des acides faibles par leur pKa et prévoir l'effet de la dilution sur le coefficient de dissociation", 8),
            ("TERMINALE_STL", "SPCLT_CHIM_EQUILIBRE_AB",       "Chimie : composition des systèmes", "Calculer la constante d'équilibre d'une réaction acide-base et prévoir son sens d'évolution spontanée", 9),
            ("TERMINALE_STL", "SPCLT_CHIM_HENDERSON_PH",       "Chimie : composition des systèmes", "Établir la relation de Henderson-Hasselbalch et estimer le pH d'un acide fort, d'une base forte, d'un tampon", 10),
            ("TERMINALE_STL", "SPCLT_CHIM_TITRAGES_AB",        "Chimie : composition des systèmes", "Exploiter des titrages acide-base directs et indirects, y compris de polyacides", 11),
            ("TERMINALE_STL", "SPCLT_CHIM_CONDUCTIMETRIE",     "Chimie : composition des systèmes", "Appliquer la loi de Kohlrausch et exploiter un dosage ou un titrage conductimétrique", 12),
            ("TERMINALE_STL", "SPCLT_CHIM_NERNST",             "Chimie : composition des systèmes", "Déterminer un potentiel par la relation de Nernst et calculer une constante d'équilibre d'oxydo-réduction", 13),
            ("TERMINALE_STL", "SPCLT_CHIM_TITRAGES_REDOX",     "Chimie : composition des systèmes", "Interpréter une courbe de titrage potentiométrique et exploiter un titrage redox direct ou indirect", 14),
            ("TERMINALE_STL", "SPCLT_SYNTH_ELECTROLYSE",       "Chimie : synthèses", "Représenter un électrolyseur, prévoir les réactions aux électrodes et calculer le rendement faradique", 15),
            ("TERMINALE_STL", "SPCLT_SYNTH_OPTIMISATION",      "Chimie : synthèses", "Optimiser rendement et vitesse d'une synthèse et comparer des protocoles selon la chimie verte", 16),
            ("TERMINALE_STL", "SPCLT_SYNTH_ESTERS_AMIDES",     "Chimie : synthèses", "Identifier ester, amide, anhydride, chlorure d'acyle et écrire estérification, hydrolyse et oxydation d'un alcool", 17),
            ("TERMINALE_STL", "SPCLT_SYNTH_DISTILLATION",      "Chimie : synthèses", "Expliquer et réaliser une distillation fractionnée et une hydrodistillation", 18),
            ("TERMINALE_STL", "SPCLT_SYNTH_SPECTRO_RMN",       "Chimie : synthèses", "Identifier ou confirmer une structure à partir de spectres UV-visible, IR et RMN", 19),
            ("TERMINALE_STL", "SPCLT_SYNTH_MECANISMES",        "Chimie : synthèses", "Illustrer un mécanisme par flèches courbes et comparer la stabilité des intermédiaires réactionnels", 20),
            ("TERMINALE_STL", "SPCLT_SYNTH_STEREOCHIMIE",      "Chimie : synthèses", "Repérer énantiomères et diastéréoisomères et déterminer un excès énantiomérique", 21),
            ("TERMINALE_STL", "SPCLT_ONDES_OSCILLATEURS",      "Ondes", "Caractériser les oscillations libres et amorties d'un système et les échanges d'énergie associés", 22),
            ("TERMINALE_STL", "SPCLT_ONDES_RESONANCE",         "Ondes", "Caractériser une résonance par sa fréquence de résonance et son facteur de qualité", 23),
            ("TERMINALE_STL", "SPCLT_ONDES_PROPAGATION",       "Ondes", "Caractériser une onde progressive sinusoïdale et exploiter le spectre d'une onde périodique", 24),
            ("TERMINALE_STL", "SPCLT_ONDES_DIFFRACTION",       "Ondes", "Exploiter l'angle d'ouverture de diffraction pour prévoir une figure ou mesurer la taille d'un objet", 25),
            ("TERMINALE_STL", "SPCLT_ONDES_ACOUSTIQUE",        "Ondes", "Caractériser un son par sa hauteur, son timbre et son niveau d'intensité sonore en décibels", 26),
            ("TERMINALE_STL", "SPCLT_ONDES_STATIONNAIRES",     "Ondes", "Interpréter les modes propres d'une corde et d'une colonne d'air par les ondes stationnaires", 27),
            ("TERMINALE_STL", "SPCLT_ONDES_EM_LASER",          "Ondes", "Relier fréquence et énergie du photon et exploiter flux, éclairement et durée d'exposition d'un laser", 28),
            ("TERMINALE_STL", "SPCLT_ONDES_RAYONNEMENT",       "Ondes", "Exploiter les lois de Wien et de Stefan et distinguer grandeurs énergétiques et photométriques", 29),
            ("TERMINALE_STL", "SPCLT_ONDES_REFRACTION",        "Ondes", "Exploiter les lois de Snell-Descartes, l'angle de réfraction limite et la réflexion totale", 30),
            ("TERMINALE_STL", "SPCLT_ONDES_POLARISATION_BIOT", "Ondes", "Prévoir l'effet d'un polariseur et exploiter la loi de Biot pour déterminer une concentration", 31),
            ("TERMINALE_STL", "SPCLT_ONDES_INTERFERENCES",     "Ondes", "Exploiter les conditions d'interférences à deux ondes et la formule des réseaux", 32),
            ("TERMINALE_STL", "SPCLT_ONDES_DOPPLER",           "Ondes", "Déterminer une vitesse à partir du décalage Doppler de la fréquence", 33),
            ("TERMINALE_STL", "SPCLT_ONDES_ECHOGRAPHIE",       "Ondes", "Expliquer le principe de l'échographie : réflexion, transmission, absorption et résolution", 34),
            ("TERMINALE_STL", "SPCLT_ONDES_MICROSCOPE",        "Ondes", "Modéliser une loupe et un microscope et exploiter grossissement commercial et pouvoir de résolution", 35),
            ("TERMINALE_STL", "SPCLT_ONDES_LUNETTE_TELESCOPE", "Ondes", "Modéliser une lunette et un télescope, établir leur grossissement et relier leur résolution à la diffraction", 36),
            ("TERMINALE_STL", "SPCLT_ONDES_TRANSMISSION",      "Ondes", "Comparer transmissions libre, par ligne bifilaire et par fibre optique (atténuation, ouverture numérique)", 37),
            ("TERMINALE_STL", "SPCLT_ONDES_STOCKAGE_AFFICHAGE","Ondes", "Expliquer la lecture interférentielle d'un support optique et le principe d'un afficheur à cristaux liquides", 38),
            ("TERMINALE_STL", "SPCLT_SYS_FLUX",                "Systèmes et procédés", "Identifier les flux de matière, d'énergie et d'informations et décrire le fonctionnement d'un procédé", 39),
            ("TERMINALE_STL", "SPCLT_SYS_FILTRES",             "Systèmes et procédés", "Caractériser un filtre (facteur d'amplification, bande passante) et proposer un gabarit", 40),
            ("TERMINALE_STL", "SPCLT_SYS_MOTEUR_PAS_A_PAS",    "Systèmes et procédés", "Caractériser un champ magnétique et piloter un moteur pas à pas avec un microcontrôleur", 41),
            ("TERMINALE_STL", "SPCLT_SYS_BOUCLE_REGULATION",   "Systèmes et procédés", "Identifier les éléments et les grandeurs réglée, réglante et perturbatrices d'une boucle de régulation", 42),
            ("TERMINALE_STL", "SPCLT_SYS_REGULATION_P_PI",     "Systèmes et procédés", "Comparer régulations TOR, P et PI et mesurer écart statique, temps de réponse et dépassement", 43),
            ("TERMINALE_STL", "SPCLT_SYS_TRANSFERTS_THERMIQUES","Systèmes et procédés", "Évaluer une puissance thermique à travers une paroi plane et dans un échangeur", 44),
            ("TERMINALE_STL", "SPCLT_SYS_CHAUDIERE",           "Systèmes et procédés", "Évaluer la puissance d'une chaudière et son débit de combustible à partir du pouvoir calorifique", 45),
            ("TERMINALE_STL", "SPCLT_SYS_POMPE_A_CHALEUR",     "Systèmes et procédés", "Réaliser le bilan énergétique d'une pompe à chaleur et évaluer son coefficient de performance", 46),
            ("TERMINALE_STL", "SPCLT_SYS_DEBIT_PRESSION",      "Systèmes et procédés", "Exploiter débits massique et volumique et le principe fondamental de la statique des fluides", 47),
            ("TERMINALE_STL", "SPCLT_SYS_BERNOULLI_POMPE",     "Systèmes et procédés", "Exploiter le théorème de Bernoulli avec pertes de charge et évaluer le rendement d'une pompe", 48),
            ("TERMINALE_STL", "SPCLT_SYS_DIAGRAMME_BINAIRE",   "Systèmes et procédés", "Exploiter un diagramme binaire liquide-vapeur et prévoir distillat et résidu d'une distillation fractionnée", 49),
            ("TERMINALE_STL", "SPCLT_SYS_CRISTALLISATION",     "Systèmes et procédés", "Utiliser une courbe de solubilité pour une cristallisation et évaluer son rendement", 50),
        };
    }
}
