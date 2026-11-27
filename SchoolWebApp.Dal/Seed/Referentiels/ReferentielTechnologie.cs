namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// La technologie du cycle 4 (5e, 4e, 3e), professeur Amine — rétablie le 25/11/2026. Provenance détaillée
    /// en tête du fichier.
    /// </summary>
    public static class ReferentielTechnologie
    {
        // =============================================================================
        // RÉFÉRENTIEL DE TECHNOLOGIE — CYCLE 4.
        //
        // POURQUOI CE FICHIER (25/11/2026) : la technologie n'avait jamais été construite. Le référentiel
        // scindait « Sciences et technologie » (jusqu'en 6e) en physique-chimie et SVT à partir de la 5e, et la
        // technologie, matière OBLIGATOIRE de la 5e à la 3e (1 h 30 par semaine) et troisième discipline possible
        // de l'épreuve de sciences du brevet, manquait. Repéré le 14/09/2026 dans la fiche du brevet, comblé ici
        // à la demande de Camara (« c'est grave ça »).
        //
        // PROVENANCE — texte téléchargé et lu le 25/11/2026. Rien n'est écrit de mémoire.
        //   Arrêté du 9 février 2024 modifiant l'arrêté du 9 novembre 2015 (programmes des cycles 2, 3 et 4),
        //   NOR MENE2402802A, JO du 28/02/2024, BO n° 9 du 29 février 2024 :
        //   https://www.education.gouv.fr/bo/2024/Hebdo9/MENE2402802A (lu sur la copie Internet Archive du
        //   18/12/2025 : le site répond « Just a moment » aux robots).
        //   Article 1 : la partie « technologie » de l'annexe 3 (cycle 4) est remplacée par l'annexe de l'arrêté.
        //   Article 2 : en vigueur à la rentrée 2024 en 5e, 2025 en 4e, 2026 en 3e. EN 2026-2027, LES TROIS
        //   CLASSES SUIVENT DONC CE PROGRAMME (l'ancien, BO n° 31 du 30/07/2020, n'est plus appliqué nulle part).
        //   Annexe lue : https://www.education.gouv.fr/sites/default/files/ensel802_annexe.pdf (16 pages, copie
        //   Internet Archive du 19/05/2024) — « Annexe — Programme de technologie du cycle 4 ».
        //   À SURVEILLER : un « projet d'aménagement des programmes de technologie pour le cycle 4 » est publié
        //   sur Éduscol STI (non paru au BO au 25/11/2026). Rien n'en est repris.
        //
        // LECTURE — comment le texte répartit les contenus, et ce qui est retenu :
        //  - Trois thèmes, neuf compétences de fin de cycle, déclinées en 22 sous-parties. Chaque sous-partie a un
        //    tableau « Repères de progressivité » à trois colonnes 5e | 4e | 3e : C'EST LE TEXTE QUI PLACE CHAQUE
        //    ATTENDU DANS SA CLASSE. Les tableaux ont été relus cellule par cellule dans la géométrie du PDF
        //    (PyMuPDF, find_tables), le texte brut mettant les trois colonnes à plat. Une seule sous-partie (« Le
        //    choix des matériaux », p. 13) n'est pas reconnue comme tableau : lue dans le texte, une cellule par
        //    colonne, sans ambiguïté. « Les objets communicants » n'a rien en 5e.
        //  - Domaine = la sous-partie (son titre, parfois resserré). Libellés : les repères sont repris en
        //    substance, « OST » (objet ou système technique) écrit en clair ; ils sont parfois resserrés, jamais
        //    élargis. Un repère identique d'une classe à l'autre (« Proposer un protocole de test… » en 4e et 3e)
        //    reste une ligne par classe : le texte l'attend aux deux.
        //  - Les blocs « Connaissances » ne sont PAS répartis par classe dans le texte (logique « spiralaire »).
        //    Chaque connaissance est placée dans la classe du PREMIER repère qui la mobilise, et écrite comme une
        //    compétence (« Connaître… », « Distinguer… »). Une connaissance « à remobiliser » du thème 3 n'est pas
        //    recopiée : elle a déjà sa ligne. Exception dite par le texte : le passage de la programmation par
        //    blocs à un langage textuel est « fin de 3e ».
        //  - Les liens avec le CRCN et avec les autres disciplines ne sont pas repris : ce sont des renvois.
        //  - Beaucoup d'attendus se font à l'atelier (fabriquer, assembler, réparer). Ils restent au référentiel :
        //    le professeur ne les fait pas faire en séance, il les explique, les prépare et en fait raisonner
        //    l'élève (consignes d'Amine, `PromptsPedagogiques.SpecialiteTechnologie`).
        // Codes : TE_<classe>_<sous-partie>_<suite>, ASCII, <= 40 caractères. Ordre : 1..n dans l'ordre du texte,
        // repères puis connaissances, classe par classe.
        // =============================================================================

        private const string Evolution = "L'évolution des objets techniques";
        private const string Numerique = "Usages et impacts du numérique";
        private const string Environnement = "L'objet technique dans son environnement";
        private const string ChoixDurable = "Choisir un objet technique (développement durable)";
        private const string Performance = "La performance des objets techniques";
        private const string ChaineEnergie = "La chaîne d'énergie";
        private const string Materiaux = "Matériaux et procédés";
        private const string ChaineInformation = "La chaîne d'information";
        private const string Donnees = "Structuration et traitement des données";
        private const string Reseau = "Les réseaux informatiques";
        private const string Reparation = "Le dépannage et la réparation";
        private const string NouvelleFonction = "Programmer une nouvelle fonctionnalité";
        private const string Projet = "La gestion de projet";
        private const string Prototypage = "Le prototypage de solutions";
        private const string ChoixMateriau = "Le choix des matériaux";
        private const string ChoixEnergie = "Le choix d'une source d'énergie";
        private const string Assemblage = "L'assemblage de constituants";
        private const string Fabrication = "La modélisation et la fabrication";
        private const string Communicants = "Les objets communicants";
        private const string ValidationMeca = "Valider la tenue mécanique d'un matériau";
        private const string ValidationPerf = "Valider les performances d'un objet technique";
        private const string Programmation = "La programmation des objets techniques";

        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =
        {
            // =========================================================================
            // CINQUIÈME
            // =========================================================================
            ("CINQUIEME", "TE_5E_EVOL_DONNEES",   Evolution, "Collecter, trier et analyser des données sur l'évolution d'un objet technique", 1),
            ("CINQUIEME", "TE_5E_EVOL_PRINCIPES", Evolution, "Comparer des principes techniques qui assurent une même fonction technique", 2),
            ("CINQUIEME", "TE_5E_EVOL_FONCTION",  Evolution, "Distinguer la fonction technique d'un objet et le principe technique qui la réalise", 3),

            ("CINQUIEME", "TE_5E_NUM_SI",         Numerique, "Décrire le rôle des systèmes d'information dans le partage de l'information", 4),
            ("CINQUIEME", "TE_5E_NUM_FICHIERS",   Numerique, "Recenser, classer et représenter des données, les stocker dans des fichiers et les retrouver dans une arborescence", 5),
            ("CINQUIEME", "TE_5E_NUM_CYBER",      Numerique, "Appliquer les bases de la cybersécurité et les règles de respect de la propriété intellectuelle", 6),
            ("CINQUIEME", "TE_5E_NUM_DERIVES",    Numerique, "Comprendre la responsabilité de chacun face aux dérives (cyberviolence, atteinte à la vie privée, usurpation d'identité)", 7),
            ("CINQUIEME", "TE_5E_NUM_FORMATS",    Numerique, "Connaître fichiers et dossiers, extensions et formats, droits de lecture et d'écriture", 8),
            ("CINQUIEME", "TE_5E_NUM_OCTETS",     Numerique, "Utiliser les unités d'information (bit, octet et leurs multiples) et les ordres de grandeur d'une image, d'un son, d'une vidéo", 9),

            ("CINQUIEME", "TE_5E_ENV_INTERACT",   Environnement, "Faire la liste des interacteurs extérieurs d'un objet technique (usagers, données, autres objets, environnement)", 10),
            ("CINQUIEME", "TE_5E_ENV_CONCEPTION", Environnement, "Repérer et expliquer les choix de conception liés à l'ergonomie, à la sécurité ou au développement durable", 11),

            ("CINQUIEME", "TE_5E_DUR_REPERER",    ChoixDurable, "Repérer dans un objet technique ses matériaux, ses sources et formes d'énergie et le traitement de l'information", 12),
            ("CINQUIEME", "TE_5E_DUR_CYCLEVIE",   ChoixDurable, "Identifier les étapes du cycle de vie d'un objet technique influencées par le choix des matériaux et de l'énergie", 13),
            ("CINQUIEME", "TE_5E_DUR_CHOISIR",    ChoixDurable, "Choisir un objet technique parmi plusieurs propositions pour répondre à un besoin", 14),
            ("CINQUIEME", "TE_5E_DUR_NOTICE",     ChoixDurable, "Lire une notice et une documentation technique : ses parties et leur organisation", 15),
            ("CINQUIEME", "TE_5E_DUR_PILIERS",    ChoixDurable, "Connaître les étapes du cycle de vie d'un objet technique et les trois piliers du développement durable", 16),

            ("CINQUIEME", "TE_5E_PERF_MESURER",   Performance, "Mesurer et comparer une performance d'un objet technique à partir d'un protocole fourni", 17),
            ("CINQUIEME", "TE_5E_PERF_CRITERES",  Performance, "Connaître les critères de performance d'un objet technique (grandeurs mesurables : vitesse, autonomie énergétique…)", 18),

            ("CINQUIEME", "TE_5E_ENER_SOLUTIONS", ChaineEnergie, "Associer des solutions techniques à une ou plusieurs fonctions techniques", 19),
            ("CINQUIEME", "TE_5E_ENER_CONSTIT",   ChaineEnergie, "Identifier des constituants de la chaîne d'énergie d'un objet technique (l'organisation de la chaîne étant fournie)", 20),
            ("CINQUIEME", "TE_5E_ENER_NATURE",    ChaineEnergie, "Indiquer la nature des énergies en entrée et en sortie des constituants de la chaîne d'énergie", 21),
            ("CINQUIEME", "TE_5E_ENER_FORMES",    ChaineEnergie, "Connaître les formes d'énergie : électrique, cinétique, potentielle, thermique, lumineuse", 22),

            ("CINQUIEME", "TE_5E_MAT_IDENTIFIER", Materiaux, "Identifier les principaux matériaux constitutifs d'un objet technique", 23),
            ("CINQUIEME", "TE_5E_MAT_PROPRIETES", Materiaux, "Connaître les caractéristiques et les propriétés des principaux matériaux", 24),

            ("CINQUIEME", "TE_5E_INFO_CONSTIT",   ChaineInformation, "Identifier des constituants de la chaîne d'information d'un objet technique (l'organisation de la chaîne étant fournie)", 25),

            ("CINQUIEME", "TE_5E_DON_DESCRIPT",   Donnees, "Choisir des descripteurs pour décrire des objets sous forme de données, en précisant leurs types et leurs formats", 26),
            ("CINQUIEME", "TE_5E_DON_VOCAB",      Donnees, "Employer le vocabulaire des données : objet, descripteur, collection (liste), type (chaîne de caractères, nombre, booléen)", 27),

            ("CINQUIEME", "TE_5E_RES_COMPOSANTS", Reseau, "Identifier les composants d'un réseau local (terminaux, commutateurs, liaisons filaires et sans fil) et sa topologie", 28),
            ("CINQUIEME", "TE_5E_RES_IDENTIFIER", Reseau, "Justifier qu'il faut identifier les terminaux pour communiquer sur un réseau local", 29),
            ("CINQUIEME", "TE_5E_RES_ROLES",      Reseau, "Connaître le réseau local et Internet, et le rôle d'un terminal, d'une carte réseau, d'un commutateur, d'un routeur, d'un serveur", 30),

            ("CINQUIEME", "TE_5E_REP_VISUEL",     Reparation, "Repérer visuellement une pièce défectueuse", 31),
            ("CINQUIEME", "TE_5E_REP_PROTOCOLE",  Reparation, "Réaliser une réparation en suivant un protocole fourni", 32),
            ("CINQUIEME", "TE_5E_REP_ATELIER",    Reparation, "Découvrir les procédés de réalisation d'un atelier de fabrication collaboratif", 33),
            ("CINQUIEME", "TE_5E_REP_SECURITE",   Reparation, "Connaître les règles de sécurité de l'atelier : équipements de protection, outillage manuel, machines de fabrication", 34),

            ("CINQUIEME", "TE_5E_FONC_DONNEES",   NouvelleFonction, "Identifier les données utilisées et produites par le programme d'une fonctionnalité (programme existant)", 35),
            ("CINQUIEME", "TE_5E_FONC_TRADUIRE",  NouvelleFonction, "Traduire en algorithme, en langage naturel, le programme associé à une fonctionnalité d'un objet technique", 36),
            ("CINQUIEME", "TE_5E_FONC_PARAM",     NouvelleFonction, "Modifier les paramètres d'un programme et évaluer ses effets sur la fonctionnalité", 37),
            ("CINQUIEME", "TE_5E_FONC_VARIABLES", NouvelleFonction, "Utiliser une variable (mot, nombre, booléen) et l'instruction d'affectation", 38),
            ("CINQUIEME", "TE_5E_FONC_EVENEMENT", NouvelleFonction, "Organiser des séquences d'instructions déclenchées par un événement", 39),
            ("CINQUIEME", "TE_5E_FONC_BLOCS",     NouvelleFonction, "Programmer avec un langage graphique par blocs", 40),

            ("CINQUIEME", "TE_5E_PROJ_SUIVRE",    Projet, "Suivre un processus de conception et de réalisation dans une durée, avec des tâches identifiées", 41),
            ("CINQUIEME", "TE_5E_PROTO_AMELIORER", Prototypage, "Fabriquer une solution pour améliorer un objet technique existant", 42),
            ("CINQUIEME", "TE_5E_CMAT_CHOISIR",   ChoixMateriau, "Choisir un matériau parmi plusieurs proposés en fonction de leurs caractéristiques", 43),
            ("CINQUIEME", "TE_5E_CENER_CHOISIR",  ChoixEnergie, "Choisir une source d'énergie parmi plusieurs proposées, et une forme d'énergie possible", 44),
            ("CINQUIEME", "TE_5E_ASSEMB_FOURNIS", Assemblage, "Assembler les constituants fournis pour réaliser un prototype", 45),
            ("CINQUIEME", "TE_5E_FAB_PROCEDURE",  Fabrication, "Mettre en œuvre les moyens de réaliser une forme selon une procédure fournie", 46),
            ("CINQUIEME", "TE_5E_FAB_MESURE",     Fabrication, "Utiliser les instruments de mesure", 47),
            ("CINQUIEME", "TE_5E_MECA_SIMUL",     ValidationMeca, "Utiliser une simulation fournie pour valider la tenue mécanique d'un matériau", 48),
            ("CINQUIEME", "TE_5E_MECA_TEST",      ValidationMeca, "Mettre en œuvre un protocole de test fourni pour valider la tenue mécanique d'un matériau", 49),
            ("CINQUIEME", "TE_5E_PERFV_VERIFIER", ValidationPerf, "Vérifier le comportement et les performances d'un objet technique en suivant un protocole fourni", 50),
            ("CINQUIEME", "TE_5E_PROG_ANALYSER",  Programmation, "Analyser un programme simple fourni et tester s'il répond au besoin posé", 51),
            ("CINQUIEME", "TE_5E_PROG_MODIFIER",  Programmation, "Modifier un programme fourni pour répondre à un besoin ou à un problème posé", 52),
            ("CINQUIEME", "TE_5E_PROG_COMMANDER", Programmation, "Réaliser et mettre au point un programme simple qui commande un objet technique", 53),

            // =========================================================================
            // QUATRIÈME
            // =========================================================================
            ("QUATRIEME", "TE_4E_EVOL_USAGES",    Evolution, "Mettre en relation des objets techniques avec leurs usages", 1),
            ("QUATRIEME", "TE_4E_EVOL_AVANTAGES", Evolution, "Identifier les avantages et les inconvénients des évolutions technologiques et informatiques", 2),
            ("QUATRIEME", "TE_4E_EVOL_BESOINS",   Evolution, "Justifier l'évolution d'un objet technique par l'évolution des besoins", 3),
            ("QUATRIEME", "TE_4E_EVOL_INNOVATION", Evolution, "Distinguer invention, innovation et développement durable dans l'évolution des besoins", 4),
            ("QUATRIEME", "TE_4E_EVOL_LIGNEE",    Evolution, "Situer un objet technique dans sa famille et dans sa lignée", 5),

            ("QUATRIEME", "TE_4E_NUM_USAGE",      Numerique, "Appliquer les règles d'un usage raisonné des objets communicants (propriété intellectuelle, identité numérique, témoins de connexion, géolocalisation)", 6),
            ("QUATRIEME", "TE_4E_NUM_TRACES",     Numerique, "Protéger ses données personnelles : traces numériques, identification et authentification", 7),
            ("QUATRIEME", "TE_4E_NUM_FLUX",       Numerique, "Connaître les incidences du stockage et des flux de données sur les réseaux", 8),

            ("QUATRIEME", "TE_4E_ENV_EXPERIENCE", Environnement, "Décrire l'expérience de l'utilisateur d'un objet technique, du langage naturel jusqu'au schéma, au graphique ou à l'algorithme", 9),
            ("QUATRIEME", "TE_4E_ENV_CONTRAINTES", Environnement, "Repérer et expliquer les contraintes prises en compte pour répondre aux utilisateurs (sécurité, environnement, ergonomie, qualité, fiabilité)", 10),
            ("QUATRIEME", "TE_4E_ENV_NORMES",     Environnement, "Connaître les exigences issues des normes et d'un cahier des charges, les labels et les certifications", 11),

            ("QUATRIEME", "TE_4E_DUR_CARACT",     ChoixDurable, "Identifier les caractéristiques à prendre en compte pour choisir un objet technique répondant à un besoin", 12),
            ("QUATRIEME", "TE_4E_DUR_COMPARER",   ChoixDurable, "Comparer plusieurs objets répondant au même besoin (incidences environnementales, bilan carbone, efficacité énergétique) et arrêter un choix", 13),
            ("QUATRIEME", "TE_4E_DUR_INDICES",    ChoixDurable, "Lire l'indice énergétique et l'indice de réparabilité d'un objet", 14),
            ("QUATRIEME", "TE_4E_DUR_IMPACTS",    ChoixDurable, "Connaître les indicateurs d'impact environnemental : air, eau, sol, santé", 15),
            ("QUATRIEME", "TE_4E_DUR_CRITERES",   ChoixDurable, "Utiliser les critères de choix : qualité, efficacité énergétique, durabilité, recyclabilité", 16),

            ("QUATRIEME", "TE_4E_PERF_APPAREILS", Performance, "Choisir les appareils de mesure adaptés pour mesurer une performance, à partir d'un protocole donné", 17),

            ("QUATRIEME", "TE_4E_ENER_CONSTIT",   ChaineEnergie, "Identifier les constituants d'une chaîne d'énergie et les associer à leurs fonctions", 18),
            ("QUATRIEME", "TE_4E_ENER_FLUX",      ChaineEnergie, "Repérer les transformations et les flux d'énergie au sein d'un objet technique", 19),
            ("QUATRIEME", "TE_4E_ENER_CONVERSIONS", ChaineEnergie, "Connaître les conversions d'énergie d'un moteur électrique, d'une lampe, d'un radiateur, d'une génératrice, d'un vérin", 20),
            ("QUATRIEME", "TE_4E_ENER_STOCKAGE",  ChaineEnergie, "Connaître la fonction d'une batterie et d'un relais ou d'un interrupteur", 21),
            ("QUATRIEME", "TE_4E_ENER_MOUVEMENTS", ChaineEnergie, "Connaître les mécanismes de transmission et de transformation de mouvement (engrenages, courroies, chaînes)", 22),

            ("QUATRIEME", "TE_4E_MAT_FORME",      Materiaux, "Mettre en relation la forme d'une pièce avec son procédé de réalisation", 23),
            ("QUATRIEME", "TE_4E_MAT_PROCEDES",   Materiaux, "Connaître les procédés de mise en forme disponibles au laboratoire et leurs caractéristiques", 24),

            ("QUATRIEME", "TE_4E_INFO_CONSTIT",   ChaineInformation, "Identifier les constituants de la chaîne d'information d'un objet réel et les associer à leur fonction", 25),
            ("QUATRIEME", "TE_4E_INFO_CAPTEURS",  ChaineInformation, "Connaître le rôle des capteurs (température, présence, distance…), du microcontrôleur et de l'interface humain-machine (boutons, afficheurs)", 26),

            ("QUATRIEME", "TE_4E_DON_TRANSFO",    Donnees, "Décrire et analyser la transformation des données téléversées ou issues d'un objet technique", 27),
            ("QUATRIEME", "TE_4E_DON_TABLE",      Donnees, "Décrire et analyser la structure d'une table de données pour interpréter le comportement d'un objet technique", 28),
            ("QUATRIEME", "TE_4E_DON_TABLEUR",    Donnees, "Mettre en forme et traiter des données dans un tableur (calculs, filtres, tris)", 29),

            ("QUATRIEME", "TE_4E_RES_IP",         Reseau, "Paramétrer une adresse IP fixe pour ajouter un objet connecté à un réseau local", 30),
            ("QUATRIEME", "TE_4E_RES_PANNE",      Reseau, "Résoudre un problème de communication entre les terminaux d'un réseau informatique", 31),
            ("QUATRIEME", "TE_4E_RES_SIMUL",      Reseau, "Compléter une simulation fournie pour valider le comportement d'un réseau informatique", 32),
            ("QUATRIEME", "TE_4E_RES_ADRESSE",    Reseau, "Connaître le rôle et la structure d'une adresse IP", 33),
            ("QUATRIEME", "TE_4E_RES_DEBIT",      Reseau, "Connaître la notion de débit et ses ordres de grandeur", 34),

            ("QUATRIEME", "TE_4E_REP_ORIGINE",    Reparation, "Proposer un protocole pour vérifier l'origine d'un dysfonctionnement", 35),
            ("QUATRIEME", "TE_4E_REP_REMPLACER",  Reparation, "Remplacer une pièce défectueuse sans protocole fourni (la pièce de remplacement étant fournie)", 36),
            ("QUATRIEME", "TE_4E_REP_PROCEDES",   Reparation, "Choisir les procédés de réalisation et les mettre en œuvre", 37),
            ("QUATRIEME", "TE_4E_REP_OBTENTION",  Reparation, "Connaître les procédés d'obtention (ajout, enlèvement de matière), de mise en forme (pliage, thermoformage) et d'assemblage (fixe, démontable)", 38),
            ("QUATRIEME", "TE_4E_REP_MACHINES",   Reparation, "Connaître les moyens de production : découpe laser, centre d'usinage, impression 3D", 39),
            ("QUATRIEME", "TE_4E_REP_ELECTRO",    Reparation, "Connaître les moyens de prototypage électronique : plaque d'essai, fils, composants, générateurs", 40),
            ("QUATRIEME", "TE_4E_REP_SOLLICIT",   Reparation, "Connaître les modes de sollicitation des matériaux (flexion, torsion) et leurs conductibilités électrique et thermique", 41),

            ("QUATRIEME", "TE_4E_FONC_ANALYSER",  NouvelleFonction, "Analyser des données pour en déduire les modifications à apporter à un programme", 42),
            ("QUATRIEME", "TE_4E_FONC_COMPLETER", NouvelleFonction, "Compléter un programme pour répondre à une fonctionnalité d'un objet technique", 43),
            ("QUATRIEME", "TE_4E_FONC_TESTER",    NouvelleFonction, "Tester et valider une modification de programme, dans un environnement simulé ou réel", 44),
            ("QUATRIEME", "TE_4E_FONC_OPERATEURS", NouvelleFonction, "Utiliser les opérateurs arithmétiques et logiques (ET, OU, NON)", 45),
            ("QUATRIEME", "TE_4E_FONC_CONDITION", NouvelleFonction, "Utiliser l'instruction conditionnelle et les instructions itératives (boucles)", 46),
            ("QUATRIEME", "TE_4E_FONC_ENTREES",   NouvelleFonction, "Identifier les entrées et les sorties d'un programme (capteurs, interface, actionneurs, fichiers)", 47),

            ("QUATRIEME", "TE_4E_PROJ_ORGANISER", Projet, "Organiser un processus de conception et de réalisation dans une durée, avec des tâches identifiées", 48),
            ("QUATRIEME", "TE_4E_PROJ_PLANNING",  Projet, "Lire et construire un diagramme de planification : tâches, durées, contraintes entre tâches", 49),
            ("QUATRIEME", "TE_4E_PROJ_REVUE",     Projet, "Connaître les étapes d'un projet et le rôle d'une revue de projet", 50),
            ("QUATRIEME", "TE_4E_PROTO_AJOUTER",  Prototypage, "Proposer et fabriquer une solution pour ajouter une nouvelle fonction à un objet technique", 51),
            ("QUATRIEME", "TE_4E_CMAT_COMPARER",  ChoixMateriau, "Comparer différents matériaux pour choisir le plus adapté", 52),
            ("QUATRIEME", "TE_4E_CENER_COMPARER", ChoixEnergie, "Comparer différentes sources d'énergie pour choisir la plus adaptée", 53),
            ("QUATRIEME", "TE_4E_ASSEMB_COMPLETER", Assemblage, "Identifier les constituants manquants d'un prototype et le compléter", 54),
            ("QUATRIEME", "TE_4E_FAB_MODIFIER",   Fabrication, "Modifier une forme à l'aide d'une modélisation", 55),
            ("QUATRIEME", "TE_4E_FAB_PRODUIRE",   Fabrication, "Choisir les moyens et produire la forme voulue", 56),
            ("QUATRIEME", "TE_4E_COM_RESEAU",     Communicants, "Interfacer un objet technique avec un réseau", 57),
            ("QUATRIEME", "TE_4E_MECA_SIMUL",     ValidationMeca, "Paramétrer une simulation fournie pour valider la tenue mécanique d'un matériau", 58),
            ("QUATRIEME", "TE_4E_MECA_TEST",      ValidationMeca, "Proposer un protocole de test pour valider la tenue mécanique d'un matériau", 59),
            ("QUATRIEME", "TE_4E_PERFV_PROTOCOLE", ValidationPerf, "Proposer un protocole de test pour valider le comportement et les performances d'un objet technique", 60),
            ("QUATRIEME", "TE_4E_PROG_ALGO",      Programmation, "Modifier un algorithme pour répondre au besoin ou au problème posé", 61),
            ("QUATRIEME", "TE_4E_PROG_TRADUIRE",  Programmation, "Traduire en programme un algorithme qui répond à un besoin simple", 62),
            ("QUATRIEME", "TE_4E_PROG_SYSTEME",   Programmation, "Réaliser et mettre au point un programme qui commande un système réel, avec éventuellement une interaction humain-machine", 63),

            // =========================================================================
            // TROISIÈME
            // =========================================================================
            ("TROISIEME", "TE_3E_EVOL_RUPTURE",   Evolution, "Identifier les innovations de rupture dans l'évolution d'un objet technique", 1),
            ("TROISIEME", "TE_3E_EVOL_DECOUVERTE", Evolution, "Relier une découverte scientifique à ses développements technologiques et à leurs effets sur la société", 2),
            ("TROISIEME", "TE_3E_EVOL_INCIDENCE", Evolution, "Exprimer en un court argumentaire l'incidence d'un objet technique sur la société", 3),
            ("TROISIEME", "TE_3E_EVOL_CONTRAINTES", Evolution, "Exprimer en un court argumentaire l'incidence des contraintes sociétales sur les objets techniques", 4),
            ("TROISIEME", "TE_3E_EVOL_IA",        Evolution, "Connaître les grands types d'apprentissage des intelligences artificielles et leurs usages (géolocalisation, identification, calcul, traduction)", 5),
            ("TROISIEME", "TE_3E_EVOL_BIAIS",     Evolution, "Repérer les biais d'une intelligence artificielle et les effets de son usage sur la société", 6),

            ("TROISIEME", "TE_3E_NUM_STRATEGIE",  Numerique, "Exprimer en un court argumentaire le rôle du numérique dans la société et dans les métiers", 7),

            ("TROISIEME", "TE_3E_ENV_REPRESENT",  Environnement, "Décrire l'expérience de l'utilisateur d'un objet technique avec un mode de représentation choisi", 8),

            ("TROISIEME", "TE_3E_DUR_LISTE",      ChoixDurable, "Établir une liste d'objets techniques possibles pour répondre à un besoin", 9),
            ("TROISIEME", "TE_3E_DUR_ARGUMENTER", ChoixDurable, "Choisir un objet technique et argumenter ce choix selon son cycle de vie et les trois piliers du développement durable", 10),
            ("TROISIEME", "TE_3E_DUR_EVALUER",    ChoixDurable, "Évaluer des objets techniques selon des critères identifiés (caractéristiques, performances, coût, indice de réparabilité)", 11),

            ("TROISIEME", "TE_3E_PERF_PROTOCOLE", Performance, "Définir et mettre en œuvre un protocole pour mesurer une caractéristique ou une performance d'un objet technique", 12),

            ("TROISIEME", "TE_3E_ENER_SCHEMA",    ChaineEnergie, "Élaborer, à l'aide d'un schéma-bloc, la chaîne d'énergie d'un objet technique", 13),

            ("TROISIEME", "TE_3E_MAT_JUSTIFIER",  Materiaux, "Justifier le choix d'un matériau et de son procédé de mise en forme au regard des contraintes techniques et environnementales", 14),
            ("TROISIEME", "TE_3E_MAT_RECYCLAGE",  Materiaux, "Connaître la disponibilité, la valorisation et le recyclage des matériaux", 15),

            ("TROISIEME", "TE_3E_INFO_DECRIRE",   ChaineInformation, "Décrire un objet technique en caractérisant sa chaîne d'information", 16),
            ("TROISIEME", "TE_3E_INFO_ANALOGIQUE", ChaineInformation, "Associer des grandeurs analogiques issues d'un objet technique à des données exploitables", 17),

            ("TROISIEME", "TE_3E_DON_REPRESENTER", Donnees, "Représenter sous forme de données les informations de diverses natures utilisées par un objet technique", 18),
            ("TROISIEME", "TE_3E_DON_STOCKER",    Donnees, "Identifier la mise en forme, la transmission ou le stockage de données dans des fichiers (texte, image, nombre)", 19),
            ("TROISIEME", "TE_3E_DON_BINAIRE",    Donnees, "Représenter l'information en binaire : le bit (0 ou 1), les booléens, les mots (code ASCII étendu), les entiers naturels", 20),

            ("TROISIEME", "TE_3E_RES_INTERNET",   Reseau, "Identifier et représenter la circulation d'une information dans le réseau Internet", 21),
            ("TROISIEME", "TE_3E_RES_ROUTAGE",    Reseau, "Justifier la nécessité d'un protocole de routage pour faire communiquer plusieurs réseaux (table de routage donnée)", 22),

            ("TROISIEME", "TE_3E_REP_HYPOTHESES", Reparation, "Formuler des hypothèses qui expliquent le dysfonctionnement d'un objet technique", 23),
            ("TROISIEME", "TE_3E_REP_PROTOCOLE",  Reparation, "Proposer un protocole de dépannage puis de réparation", 24),
            ("TROISIEME", "TE_3E_REP_REALISER",   Reparation, "Réaliser le dépannage ou la réparation d'un système défectueux", 25),
            ("TROISIEME", "TE_3E_REP_SURMESURE",  Reparation, "Réaliser une pièce sur mesure pour réparer un objet technique", 26),

            ("TROISIEME", "TE_3E_FONC_DONNEES",   NouvelleFonction, "Déterminer les données utilisées et produites par un programme en vue de le modifier", 27),
            ("TROISIEME", "TE_3E_FONC_PROGRAMMER", NouvelleFonction, "Programmer un algorithme lié à une nouvelle fonctionnalité", 28),
            ("TROISIEME", "TE_3E_FONC_TESTER",    NouvelleFonction, "Modifier et tester le programme associé à une nouvelle fonctionnalité d'un objet technique", 29),
            ("TROISIEME", "TE_3E_FONC_LISTES",    NouvelleFonction, "Utiliser une liste pour stocker des données produites par un programme, les parcourir et les traiter", 30),
            ("TROISIEME", "TE_3E_FONC_TEXTUEL",   NouvelleFonction, "Passer de la programmation graphique par blocs à la programmation textuelle", 31),

            ("TROISIEME", "TE_3E_PROJ_ELABORER",  Projet, "Élaborer un processus de conception et de réalisation dans une durée, avec des tâches identifiées", 32),
            ("TROISIEME", "TE_3E_PROJ_ECOCONCEPTION", Projet, "Connaître la démarche d'écoconception", 33),
            ("TROISIEME", "TE_3E_PROTO_NOUVEL",   Prototypage, "Proposer et fabriquer un ensemble de solutions pour produire un nouvel objet technique", 34),
            ("TROISIEME", "TE_3E_CMAT_CHOISIR",   ChoixMateriau, "Choisir un matériau constitutif d'un objet ou d'un système technique", 35),
            ("TROISIEME", "TE_3E_CENER_CHOISIR",  ChoixEnergie, "Choisir une source d'énergie pour un objet technique", 36),
            ("TROISIEME", "TE_3E_ASSEMB_CHOISIR", Assemblage, "Choisir les constituants et assembler un prototype", 37),
            ("TROISIEME", "TE_3E_FAB_MODELISER",  Fabrication, "Modéliser une forme voulue", 38),
            ("TROISIEME", "TE_3E_FAB_PRODUIRE",   Fabrication, "Choisir les moyens et produire la forme voulue", 39),
            ("TROISIEME", "TE_3E_COM_DEUX",       Communicants, "Interfacer deux objets techniques communicants", 40),
            ("TROISIEME", "TE_3E_MECA_SIMUL",     ValidationMeca, "Mettre en œuvre une simulation pour valider la tenue mécanique d'un matériau", 41),
            ("TROISIEME", "TE_3E_MECA_TEST",      ValidationMeca, "Proposer un protocole de test pour valider la tenue mécanique d'un matériau", 42),
            ("TROISIEME", "TE_3E_PERFV_PROTOCOLE", ValidationPerf, "Proposer un protocole de test pour valider le comportement et les performances d'un objet technique", 43),
            ("TROISIEME", "TE_3E_PROG_CONCEVOIR", Programmation, "Concevoir un algorithme, le traduire en un programme structuré (sous-programmes, fonctions), le tester et le mettre au point", 44),
            ("TROISIEME", "TE_3E_PROG_SYSTEME",   Programmation, "Réaliser et mettre au point un programme qui commande un système réel avec une interaction humain-machine", 45),
            ("TROISIEME", "TE_3E_PROG_MODULES",   Programmation, "Structurer un programme : organisation, modularité (sous-programmes, fonctions), commentaires", 46),
        };
    }
}
