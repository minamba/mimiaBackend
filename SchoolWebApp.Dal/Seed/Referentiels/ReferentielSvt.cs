namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel de SVT, de la cinquième à la terminale.
    ///
    /// TROIS THÈMES QUI REVIENNENT CHAQUE ANNÉE
    /// ---------------------------------------
    /// La planète Terre et l'environnement, le vivant et son évolution, le
    /// corps humain et la santé : les trois thèmes du cycle 4 sont repris
    /// chaque année à un niveau croissant, puis prolongés au lycée. Un même
    /// objet — la respiration, le climat, la génétique — est donc abordé
    /// plusieurs fois, et ce sont les libellés qui disent à quel niveau
    /// d'exigence.
    ///
    /// LA MOITIÉ DE LA NOTE EST MÉTHODOLOGIQUE
    /// --------------------------------------
    /// En SVT, l'exercice canonique est l'exploitation de documents : dire ce
    /// que le document montre, en déduire, mettre en relation. Un élève qui
    /// connaît son cours mais confond « le graphique montre » et « donc » perd
    /// autant de points qu'un élève qui n'a rien appris. D'où un domaine
    /// « Méthode » aussi fourni que les thèmes eux-mêmes.
    /// </summary>
    public static class ReferentielSvt
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- 5e ---
                ("CINQUIEME", "SV_5E_TERRE_METEO",    "La planète Terre", "Distinguer météorologie et climatologie et lire des données climatiques", 1),
                ("CINQUIEME", "SV_5E_TERRE_EROSION",  "La planète Terre", "Décrire l'érosion, le transport et la sédimentation", 2),
                ("CINQUIEME", "SV_5E_TERRE_RISQUE",   "La planète Terre", "Distinguer aléa, vulnérabilité et risque", 3),
                ("CINQUIEME", "SV_5E_VIV_NUTRITION",  "Le vivant et son évolution", "Relier les besoins des organes à leur approvisionnement par le sang", 4),
                ("CINQUIEME", "SV_5E_VIV_RESPIRATION","Le vivant et son évolution", "Expliquer les échanges gazeux respiratoires", 5),
                ("CINQUIEME", "SV_5E_VIV_PLANTES",    "Le vivant et son évolution", "Expliquer comment une plante se nourrit sans se déplacer", 6),
                ("CINQUIEME", "SV_5E_VIV_MICROORGANISMES","Le vivant et son évolution", "Distinguer les micro-organismes utiles des micro-organismes pathogènes", 7),
                ("CINQUIEME", "SV_5E_CORPS_DIGESTION","Le corps humain et la santé", "Décrire la digestion et le passage des nutriments dans le sang", 8),
                ("CINQUIEME", "SV_5E_CORPS_ALIMENTATION","Le corps humain et la santé", "Relier alimentation, besoins de l'organisme et santé", 9),
                ("CINQUIEME", "SV_5E_CORPS_HYGIENE",  "Le corps humain et la santé", "Expliquer une mesure d'hygiène par son effet sur les micro-organismes", 10),
                ("CINQUIEME", "SV_5E_METH_DOCUMENT",  "Méthode", "Prélever dans un document l'information qui répond à la question", 11),
                ("CINQUIEME", "SV_5E_METH_GRAPHIQUE", "Méthode", "Lire un graphique en identifiant d'abord les axes et les unités", 12),
                ("CINQUIEME", "SV_5E_METH_OBS_DEDUIT","Méthode", "Distinguer ce que le document montre de ce qu'on en déduit", 13),
                ("CINQUIEME", "SV_5E_METH_SCHEMA",    "Méthode", "Compléter un schéma fonctionnel avec des flèches légendées", 14),

                // --- 4e ---
                ("QUATRIEME", "SV_4E_TERRE_TECTONIQUE","La planète Terre", "Relier séismes et volcanisme aux limites de plaques", 1),
                ("QUATRIEME", "SV_4E_TERRE_PLAQUES",  "La planète Terre", "Décrire le mouvement des plaques lithosphériques", 2),
                ("QUATRIEME", "SV_4E_TERRE_ROCHES",   "La planète Terre", "Relier une roche à ses conditions de formation", 3),
                ("QUATRIEME", "SV_4E_VIV_REPRODUCTION","Le vivant et son évolution", "Comparer reproduction sexuée et asexuée et leurs conséquences", 4),
                ("QUATRIEME", "SV_4E_VIV_PEUPLEMENT", "Le vivant et son évolution", "Expliquer le peuplement d'un milieu au fil des saisons", 5),
                ("QUATRIEME", "SV_4E_VIV_SELECTION",  "Le vivant et son évolution", "Expliquer une évolution par la sélection naturelle, sans finalisme", 6),
                ("QUATRIEME", "SV_4E_VIV_BIODIVERSITE","Le vivant et son évolution", "Relier biodiversité et conditions du milieu", 7),
                ("QUATRIEME", "SV_4E_CORPS_PUBERTE",  "Le corps humain et la santé", "Décrire les transformations de la puberté et leur origine hormonale", 8),
                ("QUATRIEME", "SV_4E_CORPS_REPRODUCTION","Le corps humain et la santé", "Expliquer la fécondation et le début de la grossesse", 9),
                ("QUATRIEME", "SV_4E_CORPS_CONTRACEPTION","Le corps humain et la santé", "Expliquer le principe d'une méthode de contraception", 10),
                ("QUATRIEME", "SV_4E_CORPS_SYSTEME_NERVEUX","Le corps humain et la santé", "Décrire le trajet d'un message nerveux dans un réflexe", 11),
                ("QUATRIEME", "SV_4E_METH_CONFRONTER","Méthode", "Mettre en relation deux documents pour construire une explication", 12),
                ("QUATRIEME", "SV_4E_METH_HYPOTHESE", "Méthode", "Formuler une hypothèse testable à partir d'une observation", 13),
                ("QUATRIEME", "SV_4E_METH_CORRELATION","Méthode", "Ne pas conclure à une cause à partir d'une simple corrélation", 14),
                ("QUATRIEME", "SV_4E_METH_ECHELLE",   "Méthode", "Préciser l'échelle à laquelle on raisonne : cellule, organe, organisme", 15),

                ("QUATRIEME", "SV_4E_TERRE_SOLAIRE", "La planète Terre", "Situer la Terre parmi les planètes telluriques et gazeuses", 16),
                ("QUATRIEME", "SV_4E_TERRE_ERES", "La planète Terre", "Situer les grandes ères géologiques", 17),
                ("QUATRIEME", "SV_4E_VIV_ECOSYSTEME", "Le vivant et son évolution", "Expliquer l’organisation et le fonctionnement d’un écosystème", 18),

                // --- 3e ---
                ("TROISIEME", "SV_3E_TERRE_CLIMAT",   "La planète Terre", "Expliquer l'effet de serre et l'origine du réchauffement actuel", 1),
                ("TROISIEME", "SV_3E_TERRE_RESSOURCES","La planète Terre", "Relier l'exploitation d'une ressource à son impact sur l'environnement", 2),
                ("TROISIEME", "SV_3E_TERRE_PASSE",    "La planète Terre", "Reconstituer un climat passé à partir d'indices géologiques", 3),
                ("TROISIEME", "SV_3E_VIV_CHROMOSOMES","Le vivant et son évolution", "Relier chromosomes, ADN et gènes", 4),
                ("TROISIEME", "SV_3E_VIV_MITOSE",     "Le vivant et son évolution", "Expliquer la conservation de l'information génétique lors d'une division", 5),
                ("TROISIEME", "SV_3E_VIV_MEIOSE",     "Le vivant et son évolution", "Expliquer l'origine de la diversité génétique des individus", 6),
                ("TROISIEME", "SV_3E_VIV_MUTATION",   "Le vivant et son évolution", "Expliquer l'apparition d'un nouvel allèle par mutation", 7),
                ("TROISIEME", "SV_3E_VIV_EVOLUTION",  "Le vivant et son évolution", "Reconstituer une parenté entre espèces à partir de caractères partagés", 8),
                ("TROISIEME", "SV_3E_CORPS_IMMUNITE", "Le corps humain et la santé", "Distinguer réaction immunitaire innée et adaptative", 9),
                ("TROISIEME", "SV_3E_CORPS_VACCINATION","Le corps humain et la santé", "Expliquer le principe de la vaccination et son intérêt collectif", 10),
                ("TROISIEME", "SV_3E_CORPS_HORMONES", "Le corps humain et la santé", "Expliquer une régulation par voie hormonale", 11),
                ("TROISIEME", "SV_3E_CORPS_ADDICTIONS","Le corps humain et la santé", "Expliquer l'effet d'une substance sur le système nerveux", 12),
                ("TROISIEME", "SV_3E_CORPS_EFFORT",   "Le corps humain et la santé", "Relier l'effort physique aux adaptations de l'organisme", 13),
                ("TROISIEME", "SV_3E_METH_ARGUMENTER","Méthode", "Rédiger une explication reliant documents et connaissances", 14),
                ("TROISIEME", "SV_3E_METH_TABLEAU",   "Méthode", "Exploiter un tableau de résultats et repérer le témoin", 15),
                ("TROISIEME", "SV_3E_METH_CRITIQUE",  "Méthode", "Évaluer la fiabilité d'une information scientifique et de sa source", 16),

                ("TROISIEME", "SV_3E_TERRE_ECOSYSTEME", "La planète Terre", "Expliquer comment une activité humaine modifie un écosystème", 17),

                // --- Seconde : tronc commun ---
                ("SECONDE", "SV_2DE_VIV_BIODIVERSITE","La biodiversité", "Décrire la biodiversité à ses trois échelles et son évolution", 1),
                ("SECONDE", "SV_2DE_VIV_CELLULE",     "L'organisme vivant", "Décrire l'organisation cellulaire commune aux êtres vivants", 2),
                ("SECONDE", "SV_2DE_VIV_ADN",         "L'organisme vivant", "Relier structure de l'ADN, gènes et universalité du vivant", 3),
                ("SECONDE", "SV_2DE_VIV_METABOLISME", "L'organisme vivant", "Comparer photosynthèse et respiration comme voies métaboliques", 4),
                ("SECONDE", "SV_2DE_VIV_ECOSYSTEME",  "La biodiversité", "Analyser le fonctionnement d'un écosystème et ses services", 5),
                ("SECONDE", "SV_2DE_VIV_AGROSYSTEME", "La biodiversité", "Comparer un agrosystème et un écosystème naturel", 6),
                ("SECONDE", "SV_2DE_TERRE_EROSION",   "La Terre", "Expliquer l'érosion, le transport et la sédimentation", 7),
                ("SECONDE", "SV_2DE_TERRE_RESSOURCES","La Terre", "Relier la formation d'une ressource géologique à son exploitation", 8),
                ("SECONDE", "SV_2DE_TERRE_HISTOIRE",  "La Terre", "Situer les grandes étapes de l'histoire de la Terre et de la vie", 9),
                ("SECONDE", "SV_2DE_CORPS_PROCREATION","Le corps humain", "Expliquer le contrôle hormonal de la procréation", 10),
                ("SECONDE", "SV_2DE_CORPS_SEXUALITE", "Le corps humain", "Distinguer sexualité, procréation et prévention", 11),
                ("SECONDE", "SV_2DE_CORPS_MICROBIOTE","Le corps humain", "Expliquer le rôle du microbiote dans la santé", 12),
                ("SECONDE", "SV_2DE_CORPS_EFFORT",    "Le corps humain", "Relier activité physique et adaptations cardio-respiratoires", 13),
                ("SECONDE", "SV_2DE_METH_DOCUMENT",   "Méthode", "Exploiter un ensemble de documents pour construire une réponse", 14),
                ("SECONDE", "SV_2DE_METH_DEMARCHE",   "Méthode", "Concevoir un protocole comportant un témoin", 15),
                ("SECONDE", "SV_2DE_METH_STATISTIQUE","Méthode", "Interpréter des données chiffrées et une variabilité", 16),
                ("SECONDE", "SV_2DE_METH_REDIGER",    "Méthode", "Rédiger une explication structurée et sans finalisme", 17),

                ("SECONDE", "SV_2DE_CORPS_PATHOGENES", "Corps humain et santé", "Expliquer la transmission d’une maladie vectorielle et sa prévention", 18),
                ("SECONDE", "SV_2DE_VIV_SELECTION_SEX", "Le vivant et son évolution", "Expliquer la communication intra-spécifique et la sélection sexuelle", 19),

                // --- Première : spécialité ---
                ("PREMIERE", "SV_1RE_GEN_MITOSE",     "Génétique", "Expliquer la conservation du génome par la mitose et la réplication", 1),
                ("PREMIERE", "SV_1RE_GEN_MUTATIONS",  "Génétique", "Relier mutations, réparation de l'ADN et variabilité", 2),
                ("PREMIERE", "SV_1RE_GEN_EXPRESSION", "Génétique", "Expliquer l'expression d'un gène de la transcription à la traduction", 3),
                ("PREMIERE", "SV_1RE_GEN_PHENOTYPE",  "Génétique", "Relier génotype, phénotype et influence de l'environnement", 4),
                ("PREMIERE", "SV_1RE_TERRE_TECTONIQUE","La dynamique interne", "Expliquer la construction de la théorie de la tectonique des plaques", 5),
                ("PREMIERE", "SV_1RE_TERRE_LITHOSPHERE","La dynamique interne", "Expliquer la formation et la disparition de la lithosphère océanique", 6),
                ("PREMIERE", "SV_1RE_TERRE_MAGMATISME","La dynamique interne", "Relier magmatisme et contexte géodynamique", 7),
                ("PREMIERE", "SV_1RE_CLIMAT_PASSE",   "Les climats", "Reconstituer un climat passé à partir d'indicateurs", 8),
                ("PREMIERE", "SV_1RE_CLIMAT_ACTUEL",  "Les climats", "Expliquer le réchauffement actuel et ses causes anthropiques", 9),
                ("PREMIERE", "SV_1RE_CLIMAT_MODELES", "Les climats", "Interpréter une projection climatique et ses incertitudes", 10),
                ("PREMIERE", "SV_1RE_ECO_FONCTIONNEMENT","Écosystèmes", "Analyser les flux de matière et d'énergie dans un écosystème", 11),
                ("PREMIERE", "SV_1RE_ECO_SERVICES",   "Écosystèmes", "Analyser les services rendus par un écosystème et leur fragilité", 12),
                ("PREMIERE", "SV_1RE_SANTE_MUTATION", "Corps humain et santé", "Relier une altération du génome au développement d'un cancer", 13),
                ("PREMIERE", "SV_1RE_SANTE_VARIABILITE","Corps humain et santé", "Relier variabilité génétique et sensibilité à une maladie", 14),
                ("PREMIERE", "SV_1RE_SANTE_IMMUNITE", "Corps humain et santé", "Décrire les acteurs et les étapes de l'immunité adaptative", 15),
                ("PREMIERE", "SV_1RE_METH_ANALYSE",   "Méthode", "Analyser un document en distinguant résultat, interprétation et conclusion", 16),
                ("PREMIERE", "SV_1RE_METH_MODELE",    "Méthode", "Discuter les limites d'un modèle scientifique", 17),
                ("PREMIERE", "SV_1RE_METH_ARGUMENTER","Méthode", "Construire une argumentation reliant plusieurs documents", 18),

                ("PREMIERE", "SV_1RE_VIV_ENZYMES", "Le vivant et son évolution", "Expliquer le rôle catalytique et la spécificité d’une enzyme", 19),
                ("PREMIERE", "SV_1RE_CORPS_IMMUNITE_ADAPT", "Corps humain et santé", "Décrire l’immunité adaptative et la mémoire immunitaire", 20),
                ("PREMIERE", "SV_1RE_TERRE_DIVERGENCE", "La planète Terre", "Expliquer la dynamique d’une zone de divergence", 21),

                // --- Terminale : spécialité ---
                ("TERMINALE", "SV_TLE_GEN_MEIOSE",    "Génétique et évolution", "Expliquer le brassage génétique lors de la méiose et de la fécondation", 1),
                ("TERMINALE", "SV_TLE_GEN_ANOMALIES", "Génétique et évolution", "Interpréter une anomalie chromosomique par un accident de méiose", 2),
                ("TERMINALE", "SV_TLE_GEN_DIVERSIFICATION","Génétique et évolution", "Expliquer la diversification du vivant sans modification du génome", 3),
                ("TERMINALE", "SV_TLE_GEN_ESPECE",    "Génétique et évolution", "Discuter la notion d'espèce et le mécanisme de spéciation", 4),
                ("TERMINALE", "SV_TLE_GEN_POPULATION","Génétique et évolution", "Exploiter le modèle de Hardy-Weinberg pour repérer une évolution", 5),
                ("TERMINALE", "SV_TLE_GEN_HUMAIN",    "Génétique et évolution", "Situer l'humain dans l'évolution des primates", 6),
                ("TERMINALE", "SV_TLE_TERRE_ARCHIVES","Le passé de la Terre", "Dater un événement géologique par les méthodes relative et absolue", 7),
                ("TERMINALE", "SV_TLE_TERRE_CHAINES", "Le passé de la Terre", "Reconstituer l'histoire d'une chaîne de montagnes", 8),
                ("TERMINALE", "SV_TLE_TERRE_ATMOSPHERE","Le passé de la Terre", "Retracer l'évolution de l'atmosphère et du climat à long terme", 9),
                ("TERMINALE", "SV_TLE_IMMU_INNEE",    "Immunologie", "Décrire la réaction inflammatoire aiguë et ses acteurs", 10),
                ("TERMINALE", "SV_TLE_IMMU_ADAPTATIVE","Immunologie", "Expliquer la sélection clonale et la mémoire immunitaire", 11),
                ("TERMINALE", "SV_TLE_IMMU_VIH",      "Immunologie", "Expliquer l'effet du VIH sur le système immunitaire", 12),
                ("TERMINALE", "SV_TLE_IMMU_VACCIN",   "Immunologie", "Expliquer l'action d'un vaccin et d'un rappel", 13),
                ("TERMINALE", "SV_TLE_CORPS_MOUVEMENT","Corps humain et santé", "Relier commande nerveuse, contraction musculaire et effort", 14),
                ("TERMINALE", "SV_TLE_CORPS_CERVEAU", "Corps humain et santé", "Expliquer la plasticité cérébrale et ses conséquences", 15),
                ("TERMINALE", "SV_TLE_CORPS_GLYCEMIE","Corps humain et santé", "Expliquer la régulation de la glycémie et le diabète", 16),
                ("TERMINALE", "SV_TLE_ECO_CLIMAT",    "Écosystèmes et climat", "Analyser les conséquences du changement climatique sur les écosystèmes", 17),
                ("TERMINALE", "SV_TLE_METH_SYNTHESE", "Méthode", "Rédiger une synthèse argumentée en temps limité", 18),
                ("TERMINALE", "SV_TLE_METH_PRATIQUE", "Méthode", "Concevoir et réaliser un protocole pour l'épreuve pratique", 19),
                ("TERMINALE", "SV_TLE_METH_CRITIQUE", "Méthode", "Discuter la portée et les limites d'un résultat expérimental", 20),
            };

        /// <summary>
        /// Deux chaînes portent le graphe :
        ///
        ///   — la GÉNÉTIQUE, qui est la colonne vertébrale de la matière. Sans
        ///     le lien chromosome-ADN-gène de troisième, rien de la première ni
        ///     de la terminale ne tient : ni l'expression du génome, ni le
        ///     brassage méiotique, ni les cancers.
        ///   — la MÉTHODE, et particulièrement la distinction entre ce qu'un
        ///     document montre et ce qu'on en déduit. C'est ce que la SVT
        ///     évalue le plus, et ce sur quoi les élèves perdent le plus.
        /// </summary>
        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // Ce que le cycle 3 a laissé.
                ("SV_5E_VIV_NUTRITION",      "SC_6E_VIV_NUTRITION",      3),
                ("SV_5E_CORPS_DIGESTION",    "SC_CM2_VIV_DIGESTION",     3),
                ("SV_5E_VIV_RESPIRATION",    "SC_CM2_VIV_RESPIRATION",   3),
                ("SV_5E_TERRE_METEO",        "SC_6E_TERRE_METEO",        3),
                ("SV_5E_TERRE_RISQUE",       "SC_6E_TERRE_RISQUES",      3),
                ("SV_5E_METH_OBS_DEDUIT",    "SC_6E_DEM_CONCLURE",       3),
                ("SV_5E_METH_GRAPHIQUE",     "SC_6E_DEM_GRAPHIQUE",      3),

                // 5e → 4e
                ("SV_4E_VIV_PEUPLEMENT",     "SC_6E_VIV_PEUPLEMENT",     2),
                ("SV_4E_VIV_REPRODUCTION",   "SC_6E_VIV_REPRODUCTION",   3),
                ("SV_4E_VIV_BIODIVERSITE",   "SV_4E_VIV_PEUPLEMENT",     2),
                ("SV_4E_VIV_SELECTION",      "SV_4E_VIV_BIODIVERSITE",   3),
                ("SV_4E_CORPS_REPRODUCTION", "SV_4E_CORPS_PUBERTE",      3),
                ("SV_4E_CORPS_CONTRACEPTION","SV_4E_CORPS_REPRODUCTION", 3),
                ("SV_4E_METH_CONFRONTER",    "SV_5E_METH_DOCUMENT",      3),
                ("SV_4E_METH_HYPOTHESE",     "SV_5E_METH_OBS_DEDUIT",    3),
                ("SV_4E_METH_CORRELATION",   "SV_5E_METH_GRAPHIQUE",     3),
                ("SV_4E_METH_ECHELLE",       "SV_5E_METH_SCHEMA",        2),

                // 4e → 3e : la génétique commence ici.
                ("SV_3E_VIV_MITOSE",         "SV_3E_VIV_CHROMOSOMES",    3),
                ("SV_3E_VIV_MEIOSE",         "SV_3E_VIV_CHROMOSOMES",    3),
                ("SV_3E_VIV_MEIOSE",         "SV_4E_VIV_REPRODUCTION",   3),
                ("SV_3E_VIV_MUTATION",       "SV_3E_VIV_CHROMOSOMES",    3),
                ("SV_3E_VIV_EVOLUTION",      "SV_4E_VIV_SELECTION",      3),
                ("SV_3E_VIV_EVOLUTION",      "SV_3E_VIV_MUTATION",       3),
                ("SV_3E_CORPS_VACCINATION",  "SV_3E_CORPS_IMMUNITE",     3),
                ("SV_3E_CORPS_IMMUNITE",     "SV_5E_VIV_MICROORGANISMES",3),
                ("SV_3E_CORPS_HORMONES",     "SV_4E_CORPS_PUBERTE",      2),
                ("SV_3E_CORPS_ADDICTIONS",   "SV_4E_CORPS_SYSTEME_NERVEUX",3),
                ("SV_3E_CORPS_EFFORT",       "SV_5E_VIV_NUTRITION",      2),
                ("SV_3E_TERRE_CLIMAT",       "SV_5E_TERRE_METEO",        3),
                ("SV_3E_TERRE_PASSE",        "SV_4E_TERRE_ROCHES",       3),
                ("SV_3E_METH_ARGUMENTER",    "SV_4E_METH_CONFRONTER",    3),
                ("SV_3E_METH_TABLEAU",       "SV_4E_METH_HYPOTHESE",     2),
                ("SV_3E_METH_CRITIQUE",      "SV_4E_METH_CORRELATION",   2),

                // 3e → seconde
                ("SV_2DE_VIV_ADN",           "SV_3E_VIV_CHROMOSOMES",    3),
                ("SV_2DE_VIV_CELLULE",       "SC_6E_VIV_CELLULE",        3),
                ("SV_2DE_VIV_METABOLISME",   "SV_5E_VIV_RESPIRATION",    3),
                ("SV_2DE_VIV_METABOLISME",   "SV_5E_VIV_PLANTES",        3),
                ("SV_2DE_VIV_BIODIVERSITE",  "SV_3E_VIV_EVOLUTION",      3),
                ("SV_2DE_VIV_ECOSYSTEME",    "SV_4E_VIV_BIODIVERSITE",   3),
                ("SV_2DE_TERRE_EROSION",     "SV_5E_TERRE_EROSION",      3),
                ("SV_2DE_TERRE_HISTOIRE",    "SV_3E_TERRE_PASSE",        2),
                ("SV_2DE_CORPS_PROCREATION", "SV_3E_CORPS_HORMONES",     3),
                ("SV_2DE_CORPS_SEXUALITE",   "SV_4E_CORPS_CONTRACEPTION",3),
                ("SV_2DE_CORPS_EFFORT",      "SV_3E_CORPS_EFFORT",       3),
                ("SV_2DE_CORPS_MICROBIOTE",  "SV_5E_VIV_MICROORGANISMES",2),
                ("SV_2DE_METH_DOCUMENT",     "SV_3E_METH_ARGUMENTER",    3),
                ("SV_2DE_METH_DEMARCHE",     "SV_3E_METH_TABLEAU",       3),
                ("SV_2DE_METH_STATISTIQUE",  "SV_4E_METH_CORRELATION",   3),
                ("SV_2DE_METH_REDIGER",      "SV_3E_METH_ARGUMENTER",    3),

                // Seconde → première
                ("SV_1RE_GEN_MITOSE",        "SV_3E_VIV_MITOSE",         3),
                ("SV_1RE_GEN_MITOSE",        "SV_2DE_VIV_ADN",           3),
                ("SV_1RE_GEN_MUTATIONS",     "SV_3E_VIV_MUTATION",       3),
                ("SV_1RE_GEN_EXPRESSION",    "SV_2DE_VIV_ADN",           3),
                ("SV_1RE_GEN_PHENOTYPE",     "SV_1RE_GEN_EXPRESSION",    3),
                ("SV_1RE_TERRE_LITHOSPHERE", "SV_4E_TERRE_PLAQUES",      3),
                ("SV_1RE_TERRE_TECTONIQUE",  "SV_4E_TERRE_TECTONIQUE",   3),
                ("SV_1RE_TERRE_MAGMATISME",  "SV_1RE_TERRE_LITHOSPHERE", 3),
                ("SV_1RE_CLIMAT_ACTUEL",     "SV_3E_TERRE_CLIMAT",       3),
                ("SV_1RE_CLIMAT_PASSE",      "SV_3E_TERRE_PASSE",        3),
                ("SV_1RE_CLIMAT_MODELES",    "SV_2DE_METH_STATISTIQUE",  3),
                ("SV_1RE_ECO_FONCTIONNEMENT","SV_2DE_VIV_ECOSYSTEME",    3),
                ("SV_1RE_ECO_SERVICES",      "SV_2DE_VIV_AGROSYSTEME",   3),
                ("SV_1RE_SANTE_MUTATION",    "SV_1RE_GEN_MUTATIONS",     3),
                ("SV_1RE_SANTE_VARIABILITE", "SV_1RE_GEN_PHENOTYPE",     3),
                ("SV_1RE_SANTE_IMMUNITE",    "SV_3E_CORPS_IMMUNITE",     3),
                ("SV_1RE_METH_ANALYSE",      "SV_2DE_METH_DOCUMENT",     3),
                ("SV_1RE_METH_ARGUMENTER",   "SV_2DE_METH_REDIGER",      3),
                ("SV_1RE_METH_MODELE",       "SV_2DE_METH_DEMARCHE",     2),

                // Première → terminale
                ("SV_TLE_GEN_MEIOSE",        "SV_3E_VIV_MEIOSE",         3),
                ("SV_TLE_GEN_MEIOSE",        "SV_1RE_GEN_MITOSE",        3),
                ("SV_TLE_GEN_ANOMALIES",     "SV_TLE_GEN_MEIOSE",        3),
                ("SV_TLE_GEN_DIVERSIFICATION","SV_1RE_GEN_PHENOTYPE",    3),
                ("SV_TLE_GEN_ESPECE",        "SV_2DE_VIV_BIODIVERSITE",  3),
                ("SV_TLE_GEN_POPULATION",    "SV_TLE_GEN_MEIOSE",        3),
                ("SV_TLE_GEN_HUMAIN",        "SV_TLE_GEN_ESPECE",        2),
                ("SV_TLE_TERRE_ARCHIVES",    "SV_2DE_TERRE_HISTOIRE",    3),
                ("SV_TLE_TERRE_CHAINES",     "SV_1RE_TERRE_MAGMATISME",  3),
                ("SV_TLE_TERRE_ATMOSPHERE",  "SV_1RE_CLIMAT_PASSE",      3),
                ("SV_TLE_IMMU_ADAPTATIVE",   "SV_1RE_SANTE_IMMUNITE",    3),
                ("SV_TLE_IMMU_INNEE",        "SV_3E_CORPS_IMMUNITE",     3),
                ("SV_TLE_IMMU_VIH",          "SV_TLE_IMMU_ADAPTATIVE",   3),
                ("SV_TLE_IMMU_VACCIN",       "SV_TLE_IMMU_ADAPTATIVE",   3),
                ("SV_TLE_CORPS_MOUVEMENT",   "SV_4E_CORPS_SYSTEME_NERVEUX",3),
                ("SV_TLE_CORPS_CERVEAU",     "SV_TLE_CORPS_MOUVEMENT",   2),
                ("SV_TLE_CORPS_GLYCEMIE",    "SV_3E_CORPS_HORMONES",     3),
                ("SV_TLE_ECO_CLIMAT",        "SV_1RE_ECO_FONCTIONNEMENT",3),
                ("SV_TLE_METH_SYNTHESE",     "SV_1RE_METH_ARGUMENTER",   3),
                ("SV_TLE_METH_PRATIQUE",     "SV_2DE_METH_DEMARCHE",     3),
                ("SV_TLE_METH_CRITIQUE",     "SV_1RE_METH_MODELE",       3),

                // Le pont vers les autres matières. Un élève qui ne sait pas
                // lire un graphique perd des points en SVT sans que la SVT y
                // soit pour rien ; une explication de SVT est notée sur sa
                // rédaction autant que sur son contenu.
                ("SV_5E_METH_GRAPHIQUE",     "MATH_6E_DATA_GRAPHIQUE",    3),
                ("SV_2DE_METH_STATISTIQUE",  "MATH_2DE_STAT_INDICATEURS",2),
                ("SV_3E_METH_ARGUMENTER",    "FR_3E_ECR_SUJET_REFLEXION",2),
                ("SV_1RE_METH_ARGUMENTER",   "FR_2DE_ECR_INTRODUCTION",  2),
                ("SV_2DE_VIV_METABOLISME",   "PC_2DE_CHIM_TRANSFO",      2),
                ("SV_1RE_CLIMAT_ACTUEL",     "HG_5E_GEO_CLIMAT",         1),
            };
    }
}
