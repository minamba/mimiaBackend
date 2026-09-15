namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// La spécialité sciences économiques et sociales de la voie générale, première et terminale — provenance détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielSes
    {
        // =====================================================================================
        // RÉFÉRENTIELS DE LA SPÉCIALITÉ SCIENCES ÉCONOMIQUES ET SOCIALES (SES), VOIE GÉNÉRALE
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis le texte
        // officiel téléchargé et lu le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\ses\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau | Niveau    | Intitulé exact (page de titre de l'annexe)                         |
        // |---------|-----------|--------------------------------------------------------------------|
        // | SES1    | PREMIERE  | Programme de sciences économiques et sociales de première générale |
        // | SEST    | TERMINALE | Programme de sciences économiques et sociales de terminale générale |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901639A,
        //   BO spécial n° 1 du 22 janvier 2019. Article 2 : « Les dispositions du présent arrêté entrent en
        //   vigueur à la rentrée scolaire 2019. » Annexe unique, lue en entier.
        //   Arrêté + annexe lus dans le PDF complet du BO spécial n° 1 (copie : sources\ses\BO_special1_2019-01-22.pdf,
        //     extrait : MENE1901639A_ses_premiere.txt).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901639A.htm (servie « Just a moment... »
        //     à curl : non lue directement, remplacée par le PDF du BO).
        //   Annexe éduscol : https://eduscol.education.gouv.fr/sites/default/files/document/spe639annexe1063544pdf-82752.pdf
        //     (texte comparé par script au BO : identique, hors en-têtes et pieds de page).
        //   Légifrance : https://www.legifrance.gouv.fr/loda/id/JORFTEXT000038029532 (« Version en vigueur depuis le 02/09/2019 »).
        //
        // TERMINALE : arrêté du 19-7-2019, J.O. n° 0169 du 23-7-2019, NOR MENE1921253A,
        //   BO spécial n° 8 du 25 juillet 2019. Article 2 : « Les dispositions du présent arrêté entrent en
        //   vigueur à la rentrée scolaire 2020. » Annexe unique, lue en entier.
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921253A.htm
        //   Annexe (BO)     : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/82/1/spe253_annexe_1158821.pdf
        //                     (copie identique, même md5, lue sur éduscol : spe253annexe1158821pdf-82755.pdf)
        //   Légifrance : https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000038799948 (« Version initiale », aucune modification).
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : la page éduscol « Programmes et ressources en sciences économiques
        //   et sociales - voie GT » (https://eduscol.education.gouv.fr/5838/programmes-et-ressources-en-sciences-economiques-et-sociales-voie-gt,
        //   datée « février 2026 », lue le 14/09/2026) liste sous « Programmes en vigueur » exactement ces deux
        //   programmes (BO spécial n° 1 du 22-1-2019 et BO spécial n° 8 du 25-7-2019), avec le programme de seconde.
        //   Légifrance donne l'arrêté de première en version unique depuis le 02/09/2019 et celui de terminale en
        //   version initiale. Aucun arrêté modificatif 2020-2026 trouvé (recherches BO, éduscol, Légifrance le 14/09/2026).
        //   Ce qui a changé depuis 2020, ce sont les parties ÉVALUÉES à l'écrit, pas le programme (voir ses.prof.md).
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - Aucun sur la liste : 12 questionnements en première, 12 en terminale, trois champs chacun
        //     (science économique ; sociologie et science politique ; regards croisés).
        //   - Dans le PDF de terminale, la mise en page en colonnes mélange les intitulés « Comment lutter contre le
        //     chômage ? », « Comment expliquer les crises financières… » et « Quelles politiques économiques dans le
        //     cadre européen ? ». Les objectifs ont été rattachés par leur contenu, en recoupant avec les intitulés des
        //     ressources d'accompagnement de la page éduscol. Même chose pour « Quelles mutations du travail et de
        //     l'emploi ? » et « Comment expliquer l'engagement politique… ».
        //   - Épreuve écrite de la session 2027 (note MENE2416667N, « à compter de la session 2025 ») : 9 questionnements
        //     de terminale sur 12 sont évaluables. HORS ÉCRIT mais toujours au programme : « Comment expliquer les crises
        //     financières et réguler le système financier ? », « Quelle est l'action de l'École sur les destins
        //     individuels et sur l'évolution de la société ? », « Quelles inégalités sont compatibles avec les différentes
        //     conceptions de la justice sociale ? ». Leurs lignes sont gardées (le programme les impose) et signalées
        //     par « hors écrit 2027 » dans les commentaires.
        //   - Les lignes « Compétences transversales » reprennent la liste du préambule du cycle terminal
        //     (argumentation en première ; problématique et dissertation en terminale).
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   SES1 64 · SEST 64 · total 128
        //   SEST : 12 lignes portent sur les trois questionnements hors écrit 2027 (5 crises financières,
        //   3 École, 4 inégalités et justice sociale) ; 52 lignes relèvent du périmètre de l'écrit ou des outils.
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE GÉNÉRALE — SCIENCES ÉCONOMIQUES ET SOCIALES (annexe de MENE1901639A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SES1 =
        {
            // Science économique : Comment un marché concurrentiel fonctionne-t-il ?
            ("PREMIERE", "SES1_MARCHE_INSTITUTION",            "Science économique — Marché concurrentiel", "Expliquer que le marché est une institution et classer des marchés selon leur degré de concurrence, de la concurrence parfaite au monopole", 1),
            ("PREMIERE", "SES1_OFFRE_DEMANDE_EQUILIBRE",       "Science économique — Marché concurrentiel", "Interpréter les courbes d'offre et de demande et leurs pentes, et expliquer comment leur confrontation fixe l'équilibre du marché", 2),
            ("PREMIERE", "SES1_DEPLACEMENTS_COURBES",          "Science économique — Marché concurrentiel", "Distinguer, sur un exemple chiffré, un déplacement de la courbe d'un déplacement sur la courbe", 3),
            ("PREMIERE", "SES1_TAXE_FORFAITAIRE",              "Science économique — Marché concurrentiel", "Montrer sur un exemple chiffré l'effet d'une taxe forfaitaire sur l'équilibre du marché", 4),
            ("PREMIERE", "SES1_OFFRE_COUT_MARGINAL",           "Science économique — Marché concurrentiel", "Déduire la courbe d'offre de la maximisation du profit : avec un coût marginal croissant, produire là où coût marginal = prix", 5),
            ("PREMIERE", "SES1_SURPLUS",                       "Science économique — Marché concurrentiel", "Définir et repérer sur un graphique le surplus du producteur et le surplus du consommateur", 6),
            ("PREMIERE", "SES1_GAINS_ECHANGE",                 "Science économique — Marché concurrentiel", "Expliquer les gains à l'échange et justifier que la somme des surplus est maximale à l'équilibre", 7),

            // Science économique : Comment les marchés imparfaitement concurrentiels fonctionnent-ils ?
            ("PREMIERE", "SES1_POUVOIR_MARCHE",                "Science économique — Concurrence imparfaite", "Illustrer les sources du pouvoir de marché : nombre limité d'offreurs, ententes, barrières à l'entrée", 8),
            ("PREMIERE", "SES1_MONOPOLE_TYPES",                "Science économique — Concurrence imparfaite", "Expliquer que le monopole est faiseur de prix et donner un exemple de monopole naturel, institutionnel et d'innovation", 9),
            ("PREMIERE", "SES1_MONOPOLE_INEFFICACE",           "Science économique — Concurrence imparfaite", "Montrer, par un graphique ou un exemple chiffré, que l'équilibre du monopole n'est pas efficace", 10),
            ("PREMIERE", "SES1_OLIGOPOLE_DILEMME",             "Science économique — Concurrence imparfaite", "Définir l'oligopole et expliquer avec le dilemme du prisonnier pourquoi les firmes ont intérêt à former des ententes", 11),
            ("PREMIERE", "SES1_POLITIQUE_CONCURRENCE",         "Science économique — Concurrence imparfaite", "Expliquer comment la politique de la concurrence (fusions, ententes, abus de position dominante) accroît le surplus du consommateur", 12),

            // Science économique : Quelles sont les principales défaillances du marché ?
            ("PREMIERE", "SES1_EXTERNALITES",                  "Science économique — Défaillances du marché", "Expliquer pourquoi le marché est défaillant en présence d'externalités, avec l'exemple de la pollution", 13),
            ("PREMIERE", "SES1_BIENS_COMMUNS_COLLECTIFS",      "Science économique — Défaillances du marché", "Distinguer bien commun et bien collectif et expliquer par des exemples pourquoi le marché y est défaillant", 14),
            ("PREMIERE", "SES1_SELECTION_ADVERSE_ALEA_MORAL",  "Science économique — Défaillances du marché", "Définir sélection adverse et aléa moral et les illustrer par les voitures d'occasion et par l'assurance", 15),
            ("PREMIERE", "SES1_ABSENCE_EQUILIBRE",             "Science économique — Défaillances du marché", "Expliquer comment la sélection adverse peut conduire à l'absence d'équilibre sur un marché", 16),
            ("PREMIERE", "SES1_INTERVENTION_PUBLIQUE",         "Science économique — Défaillances du marché", "Illustrer par un exemple l'intervention des pouvoirs publics face à chacune de ces défaillances", 17),

            // Science économique : Comment les agents économiques se financent-ils ?
            ("PREMIERE", "SES1_BESOIN_CAPACITE_FINANCEMENT",   "Science économique — Financement de l'économie", "Expliquer que le financement consiste à couvrir des besoins de financement par des capacités de financement", 18),
            ("PREMIERE", "SES1_TAUX_INTERET_PRIX",             "Science économique — Financement de l'économie", "Expliquer que le taux d'intérêt, rémunération du prêteur et coût du crédit, est le prix sur le marché des fonds prêtables", 19),
            ("PREMIERE", "SES1_MENAGES_EPARGNE",               "Science économique — Financement de l'économie", "Expliquer le partage du revenu disponible entre consommation et épargne et d'où vient un besoin ou une capacité de financement", 20),
            ("PREMIERE", "SES1_ENTREPRISES_FINANCEMENT",       "Science économique — Financement de l'économie", "Définir l'EBE et distinguer autofinancement et financement externe : emprunt bancaire, actions, obligations", 21),
            ("PREMIERE", "SES1_SOLDE_BUDGETAIRE",              "Science économique — Financement de l'économie", "Calculer un solde budgétaire (recettes fiscales et non fiscales − dépenses) et expliquer que le déficit se finance par l'emprunt", 22),
            ("PREMIERE", "SES1_RELANCE_EVICTION",              "Science économique — Financement de l'économie", "Expliquer les effets contradictoires des dépenses publiques sur l'activité : relance de la demande ou effet d'éviction", 23),

            // Science économique : Qu'est-ce que la monnaie et comment est-elle créée ?
            ("PREMIERE", "SES1_MONNAIE_FONCTIONS_FORMES",      "Science économique — Monnaie", "Citer les fonctions et les formes de la monnaie", 24),
            ("PREMIERE", "SES1_CREATION_MONETAIRE_CREDIT",     "Science économique — Monnaie", "Expliquer, avec les bilans simplifiés d'une banque et d'une entreprise, comment un crédit bancaire crée de la monnaie", 25),
            ("PREMIERE", "SES1_BANQUE_CENTRALE_TAUX",          "Science économique — Monnaie", "Expliquer le rôle de la banque centrale dans la création monétaire par le pilotage du taux d'intérêt à court terme", 26),
            ("PREMIERE", "SES1_BANQUE_CENTRALE_EFFETS",        "Science économique — Monnaie", "Expliquer les effets possibles des interventions de la banque centrale sur le niveau des prix et sur l'activité", 27),

            // Sociologie et science politique : Comment la socialisation contribue-t-elle à expliquer les différences de comportement des individus ?
            ("PREMIERE", "SES1_SOCIALISATION_DIFFERENCIEE",    "Sociologie — Socialisation", "Expliquer comment des façons d'agir, de penser et d'anticiper l'avenir socialement situées créent des différences de comportement", 28),
            ("PREMIERE", "SES1_CONFIGURATIONS_FAMILIALES",     "Sociologie — Socialisation", "Expliquer comment la diversité des configurations familiales modifie la socialisation des enfants et des adolescents", 29),
            ("PREMIERE", "SES1_SOCIALISATION_SECONDAIRE",      "Sociologie — Socialisation", "Distinguer socialisation primaire et socialisations secondaires (professionnelle, conjugale, politique) avec un exemple", 30),
            ("PREMIERE", "SES1_TRAJECTOIRES_IMPROBABLES",      "Sociologie — Socialisation", "Expliquer comment la pluralité des influences socialisatrices peut produire des trajectoires individuelles improbables", 31),

            // Sociologie et science politique : Comment se construisent et évoluent les liens sociaux ?
            ("PREMIERE", "SES1_LIENS_GROUPES_SOCIAUX",         "Sociologie — Liens sociaux", "Illustrer la diversité des liens dans les familles, groupes de pairs, univers professionnel, associations et réseaux", 32),
            ("PREMIERE", "SES1_PCS_CRITERES",                  "Sociologie — Liens sociaux", "Citer les critères de construction des professions et catégories socioprofessionnelles (PCS)", 33),
            ("PREMIERE", "SES1_INDIVIDUALISATION_SOLIDARITE",  "Sociologie — Liens sociaux", "Illustrer le processus d'individualisation et distinguer solidarité « mécanique » et solidarité « organique »", 34),
            ("PREMIERE", "SES1_SOCIABILITES_NUMERIQUES",       "Sociologie — Liens sociaux", "Expliquer comment les nouvelles sociabilités numériques contribuent au lien social", 35),
            ("PREMIERE", "SES1_RUPTURE_LIENS",                 "Sociologie — Liens sociaux", "Expliquer comment précarités, isolements, ségrégations et ruptures familiales affaiblissent ou rompent les liens sociaux", 36),

            // Sociologie et science politique : Quels sont les processus sociaux qui contribuent à la déviance ?
            ("PREMIERE", "SES1_NORMES_CONTROLE_SOCIAL",        "Sociologie — Déviance", "Distinguer normes sociales et normes juridiques et donner des formes variées de contrôle social", 37),
            ("PREMIERE", "SES1_DEVIANCE_RELATIVE",             "Sociologie — Déviance", "Définir la déviance comme transgression des normes et montrer qu'elle varie selon les sociétés et les groupes sociaux", 38),
            ("PREMIERE", "SES1_ETIQUETAGE_CARRIERE",           "Sociologie — Déviance", "Expliquer la déviance comme produit de processus sociaux : étiquetage, stigmatisation, carrière déviante", 39),
            ("PREMIERE", "SES1_DEVIANCE_DELINQUANCE",          "Sociologie — Déviance", "Distinguer déviance et délinquance à l'aide d'exemples", 40),
            ("PREMIERE", "SES1_MESURE_DELINQUANCE",            "Sociologie — Déviance", "Illustrer par un exemple les difficultés de mesure de la délinquance", 41),

            // Sociologie et science politique : Comment se forme et s'exprime l'opinion publique ?
            ("PREMIERE", "SES1_OPINION_DEMOCRATIE",            "Science politique — Opinion publique", "Expliquer que l'opinion publique naît avec la démocratie : d'abord celle des catégories « éclairées », puis du plus grand nombre", 42),
            ("PREMIERE", "SES1_SONDAGES_PRINCIPES",            "Science politique — Opinion publique", "Expliquer les principes et les techniques des sondages et les débats sur leur interprétation de l'opinion publique", 43),
            ("PREMIERE", "SES1_DEMOCRATIE_OPINION",            "Science politique — Opinion publique", "Expliquer comment les sondages forgent l'opinion et modifient la démocratie et la vie politique (démocratie d'opinion)", 44),

            // Sociologie et science politique : Voter : une affaire individuelle ou collective ?
            ("PREMIERE", "SES1_TAUX_ELECTORAUX",               "Science politique — Vote", "Interpréter des taux d'inscription sur les listes électorales, des taux de participation et d'abstention", 45),
            ("PREMIERE", "SES1_FACTEURS_PARTICIPATION",        "Science politique — Vote", "Expliquer la participation électorale par l'intégration sociale, l'intérêt, la compétence politique et le contexte", 46),
            ("PREMIERE", "SES1_VOTE_INDIVIDUEL_COLLECTIF",     "Science politique — Vote", "Montrer que le vote est à la fois un acte individuel (préférences, offre électorale) et collectif (appartenances sociales)", 47),
            ("PREMIERE", "SES1_VOLATILITE_ELECTORALE",         "Science politique — Vote", "Décrire les formes de la volatilité électorale et les relier au déclin de l'identification politique et au poids du contexte", 48),

            // Regards croisés : Comment l'assurance et la protection sociale contribuent-elles à la gestion des risques dans les sociétés développées ?
            ("PREMIERE", "SES1_TYPES_RISQUES",                 "Regards croisés — Risques et protection sociale", "Citer les principaux risques économiques et sociaux : maladie, accident, perte d'emploi, vieillesse", 49),
            ("PREMIERE", "SES1_EXPOSITION_ATTITUDE_RISQUE",    "Regards croisés — Risques et protection sociale", "Illustrer comment l'exposition et l'attitude face au risque diffèrent selon les individus, les groupes et les sociétés", 50),
            ("PREMIERE", "SES1_PARTAGE_RISQUES",               "Regards croisés — Risques et protection sociale", "Expliquer les effets positifs (bien-être, innovation) et négatifs (aléa moral) du partage des risques", 51),
            ("PREMIERE", "SES1_GESTION_COLLECTIVE_RISQUES",    "Regards croisés — Risques et protection sociale", "Illustrer les principes de gestion collective des risques : prévention, mutualisation, diversification", 52),
            ("PREMIERE", "SES1_INSTITUTIONS_RISQUES",          "Regards croisés — Risques et protection sociale", "Présenter le rôle de la famille, des sociétés et mutuelles d'assurance et des pouvoirs publics dans la gestion des risques", 53),
            ("PREMIERE", "SES1_ASSURANCE_ASSISTANCE",          "Regards croisés — Risques et protection sociale", "Distinguer logique d'assurance et logique d'assistance et expliquer la solidarité collective de la protection sociale", 54),

            // Regards croisés : Comment les entreprises sont-elles organisées et gouvernées ?
            ("PREMIERE", "SES1_CYCLE_VIE_ENTREPRISE",          "Regards croisés — Entreprises", "Retracer le cycle de vie d'une entreprise : création, croissance, changement de statut juridique, disparition", 55),
            ("PREMIERE", "SES1_FIGURES_ENTREPRENEUR",          "Regards croisés — Entreprises", "Distinguer les figures de l'entrepreneur par statut juridique et par fonction : innovateur, manager, actionnaire", 56),
            ("PREMIERE", "SES1_GOUVERNANCE_AUTORITE",          "Regards croisés — Entreprises", "Définir gouvernance et autorité et distinguer décisions centralisées et décentralisées dans une entreprise", 57),
            ("PREMIERE", "SES1_RELATIONS_SOCIALES_ENTREPRISE", "Regards croisés — Entreprises", "Expliquer que l'entreprise est un lieu de coopération, de hiérarchie et de conflit entre ses parties prenantes", 58),

            // Objectifs d'apprentissage concernant l'utilisation des données quantitatives et des représentations graphiques
            ("PREMIERE", "SES1_PROPORTION_REPARTITION",        "Données quantitatives et graphiques", "Calculer et interpréter une proportion et un pourcentage de répartition", 59),
            ("PREMIERE", "SES1_TAUX_VARIATION_INDICE",         "Données quantitatives et graphiques", "Calculer et interpréter un taux de variation, un taux cumulé, un coefficient multiplicateur et un indice simple", 60),
            ("PREMIERE", "SES1_MOYENNES",                      "Données quantitatives et graphiques", "Calculer et interpréter une moyenne arithmétique simple et une moyenne pondérée", 61),
            ("PREMIERE", "SES1_INDICE_SYNTHETIQUE_MEDIANE",    "Données quantitatives et graphiques", "Lire et interpréter un indice synthétique et une médiane", 62),
            ("PREMIERE", "SES1_NOMINAL_REEL",                  "Données quantitatives et graphiques", "Distinguer valeur nominale et valeur réelle, notamment taux d'intérêt nominal et taux d'intérêt réel", 63),

            // Compétences transversales de fin de première (préambule du cycle terminal)
            ("PREMIERE", "SES1_ARGUMENTATION",                 "Compétences transversales", "Construire à l'oral une argumentation rigoureuse qui mobilise connaissances et données d'un document", 64),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE GÉNÉRALE — SCIENCES ÉCONOMIQUES ET SOCIALES (annexe de MENE1921253A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SEST =
        {
            // Science économique : Quels sont les sources et les défis de la croissance économique ?
            ("TERMINALE", "SEST_SOURCES_CROISSANCE",             "Science économique — Croissance", "Expliquer les sources de la croissance : accumulation des facteurs et hausse de la productivité globale des facteurs", 1),
            ("TERMINALE", "SEST_PROGRES_TECHNIQUE_PGF",          "Science économique — Croissance", "Relier progrès technique et productivité globale des facteurs et expliquer que le progrès technique est endogène (innovation)", 2),
            ("TERMINALE", "SEST_INSTITUTIONS_CROISSANCE",        "Science économique — Croissance", "Expliquer comment les institutions, notamment les droits de propriété, influent sur l'incitation à investir et à innover", 3),
            ("TERMINALE", "SEST_DESTRUCTION_CREATRICE",          "Science économique — Croissance", "Expliquer le processus de destruction créatrice qui accompagne l'innovation", 4),
            ("TERMINALE", "SEST_PROGRES_INEGALITES",             "Science économique — Croissance", "Expliquer comment le progrès technique peut engendrer des inégalités de revenus", 5),
            ("TERMINALE", "SEST_CROISSANCE_SOUTENABLE",          "Science économique — Croissance", "Expliquer les limites écologiques d'une croissance soutenable et comment l'innovation peut aider à les reculer", 6),

            // Science économique : Quels sont les fondements du commerce international et de l'internationalisation de la production ?
            ("TERMINALE", "SEST_AVANTAGES_COMPARATIFS",          "Science économique — Commerce international", "Expliquer le rôle des dotations factorielles et technologiques (avantages comparatifs) dans la spécialisation internationale", 7),
            ("TERMINALE", "SEST_COMMERCE_PAYS_COMPARABLES",      "Science économique — Commerce international", "Expliquer le commerce entre pays comparables : différenciation et qualité des produits, fragmentation de la chaîne de valeur", 8),
            ("TERMINALE", "SEST_PRODUCTIVITE_COMPETITIVITE",     "Science économique — Commerce international", "Expliquer que la productivité des firmes sous-tend la compétitivité d'un pays, c'est-à-dire son aptitude à exporter", 9),
            ("TERMINALE", "SEST_CHAINE_VALEUR",                  "Science économique — Commerce international", "Illustrer par un exemple l'internationalisation de la chaîne de valeur", 10),
            ("TERMINALE", "SEST_EFFETS_COMMERCE",                "Science économique — Commerce international", "Présenter les effets du commerce international : baisse des prix, inégalités entre pays et inégalités au sein de chaque pays", 11),
            ("TERMINALE", "SEST_LIBRE_ECHANGE_PROTECTIONNISME",  "Science économique — Commerce international", "Exposer les termes du débat entre libre-échange et protectionnisme", 12),

            // Science économique : Comment lutter contre le chômage ?
            ("TERMINALE", "SEST_CHOMAGE_INDICATEURS",            "Science économique — Chômage", "Définir chômage et sous-emploi et interpréter un taux de chômage et un taux d'emploi", 13),
            ("TERMINALE", "SEST_CHOMAGE_STRUCTUREL",             "Science économique — Chômage", "Expliquer le chômage structurel par les appariements (frictions, inadéquations) et les asymétries d'information (salaire d'efficience)", 14),
            ("TERMINALE", "SEST_INSTITUTIONS_CHOMAGE",           "Science économique — Chômage", "Discuter les effets du salaire minimum et des règles de protection de l'emploi sur le chômage structurel", 15),
            ("TERMINALE", "SEST_CHOMAGE_CONJONCTUREL",           "Science économique — Chômage", "Expliquer comment les fluctuations de l'activité économique font varier le chômage conjoncturel", 16),
            ("TERMINALE", "SEST_POLITIQUES_EMPLOI",              "Science économique — Chômage", "Présenter les politiques contre le chômage : soutien de la demande, baisse du coût du travail, formation, flexibilisation", 17),

            // Science économique : Comment expliquer les crises financières et réguler le système financier ? (hors écrit 2027)
            ("TERMINALE", "SEST_CRISES_1930_2008",               "Science économique — Crises financières", "Décrire les crises financières des années 1930 et de 2008 : krach boursier, faillites en chaîne, chute du PIB, chômage", 18),
            ("TERMINALE", "SEST_BULLE_SPECULATIVE",              "Science économique — Crises financières", "Expliquer la formation et l'éclatement d'une bulle spéculative : comportements mimétiques, prophéties autoréalisatrices", 19),
            ("TERMINALE", "SEST_PANIQUE_BANCAIRE",               "Science économique — Crises financières", "Expliquer les phénomènes de panique bancaire et de faillites bancaires en chaîne", 20),
            ("TERMINALE", "SEST_TRANSMISSION_CRISE",             "Science économique — Crises financières", "Présenter les canaux de transmission d'une crise à l'économie réelle : effet de richesse, collatéral, contraction du crédit", 21),
            ("TERMINALE", "SEST_REGULATION_BANCAIRE",            "Science économique — Crises financières", "Expliquer comment la supervision bancaire et le ratio de solvabilité réduisent l'aléa moral des banques", 22),

            // Science économique : Quelles politiques économiques dans le cadre européen ?
            ("TERMINALE", "SEST_INTEGRATION_EUROPEENNE",         "Science économique — Politiques européennes", "Présenter le marché unique et la zone euro et expliquer les effets du marché unique sur la croissance", 23),
            ("TERMINALE", "SEST_CONCURRENCE_EUROPEENNE",         "Science économique — Politiques européennes", "Expliquer les objectifs, les modalités et les limites de la politique européenne de la concurrence", 24),
            ("TERMINALE", "SEST_POLITIQUES_CONJONCTURE",         "Science économique — Politiques européennes", "Expliquer comment la politique monétaire et la politique budgétaire agissent sur la conjoncture", 25),
            ("TERMINALE", "SEST_BCE_COORDINATION",               "Science économique — Politiques européennes", "Expliquer les difficultés d'une politique monétaire unique (BCE) face à des budgets nationaux : coordination, chocs asymétriques", 26),

            // Sociologie et science politique : Comment est structurée la société française actuelle ?
            ("TERMINALE", "SEST_FACTEURS_STRUCTURATION",         "Sociologie — Structure sociale", "Identifier les facteurs de hiérarchisation de l'espace social : PCS, revenu, diplôme, ménage, cycle de vie, sexe, résidence", 27),
            ("TERMINALE", "SEST_EVOLUTION_STRUCTURE",            "Sociologie — Structure sociale", "Décrire l'évolution de la structure socioprofessionnelle : salarisation, tertiarisation, qualification, féminisation", 28),
            ("TERMINALE", "SEST_MARX_WEBER",                     "Sociologie — Structure sociale", "Comparer les approches des classes et de la stratification sociale chez Marx et chez Weber", 29),
            ("TERMINALE", "SEST_DEBAT_CLASSES",                  "Sociologie — Structure sociale", "Discuter la pertinence des classes sociales aujourd'hui : distances inter et intra-classes, genre, identification, individualisation", 30),

            // Sociologie et science politique : Quelle est l'action de l'École sur les destins individuels et sur l'évolution de la société ? (hors écrit 2027)
            ("TERMINALE", "SEST_ECOLE_EGALITE_CHANCES",          "Sociologie — École", "Expliquer que l'école des sociétés démocratiques transmet des savoirs et vise à favoriser l'égalité des chances", 31),
            ("TERMINALE", "SEST_MASSIFICATION_DEMOCRATISATION",  "Sociologie — École", "Interpréter taux de scolarisation et taux d'accès à un diplôme en distinguant massification et démocratisation", 32),
            ("TERMINALE", "SEST_INEGALITES_REUSSITE_SCOLAIRE",   "Sociologie — École", "Expliquer les inégalités de réussite scolaire : école, capital culturel, investissements familiaux, genre, stratégies des ménages", 33),

            // Sociologie et science politique : Quels sont les caractéristiques contemporaines et les facteurs de la mobilité sociale ?
            ("TERMINALE", "SEST_MOBILITE_FORMES",                "Sociologie — Mobilité sociale", "Distinguer la mobilité sociale intergénérationnelle des mobilités géographique et professionnelle", 34),
            ("TERMINALE", "SEST_TABLES_MOBILITE_LIMITES",        "Sociologie — Mobilité sociale", "Expliquer la construction, les intérêts et les limites des tables de mobilité", 35),
            ("TERMINALE", "SEST_MOBILITE_STRUCTURELLE_FLUIDITE", "Sociologie — Mobilité sociale", "Distinguer mobilité observée, mobilité structurelle et fluidité sociale, et montrer que plus mobile ne veut pas dire plus fluide", 36),
            ("TERMINALE", "SEST_LECTURE_TABLES_MOBILITE",        "Sociologie — Mobilité sociale", "Repérer dans une table de mobilité ascension, reproduction sociale, déclassement et les différences hommes-femmes", 37),
            ("TERMINALE", "SEST_FACTEURS_MOBILITE",              "Sociologie — Mobilité sociale", "Expliquer la mobilité par la structure socioprofessionnelle, les niveaux de formation et les ressources familiales", 38),

            // Sociologie et science politique : Quelles mutations du travail et de l'emploi ?
            ("TERMINALE", "SEST_TRAVAIL_EMPLOI_NOTIONS",         "Sociologie — Travail et emploi", "Distinguer travail, activité, emploi salarié ou non, chômage, et expliquer le brouillage des frontières avec l'inactivité", 39),
            ("TERMINALE", "SEST_QUALITE_EMPLOIS",                "Sociologie — Travail et emploi", "Citer les descripteurs de la qualité des emplois : conditions de travail, salaire, sécurité, carrière, formation, tâches", 40),
            ("TERMINALE", "SEST_TAYLORIEN_POST_TAYLORIEN",       "Sociologie — Travail et emploi", "Comparer organisations taylorienne et post-taylorienne et leurs effets positifs et négatifs sur les conditions de travail", 41),
            ("TERMINALE", "SEST_NUMERIQUE_TRAVAIL",              "Sociologie — Travail et emploi", "Expliquer comment le numérique brouille les frontières du travail, transforme l'emploi et accroît la polarisation des emplois", 42),
            ("TERMINALE", "SEST_TRAVAIL_INTEGRATION",            "Sociologie — Travail et emploi", "Expliquer que le travail intègre et que précarisation, chômage élevé et polarisation peuvent affaiblir ce pouvoir intégrateur", 43),

            // Sociologie et science politique : Comment expliquer l'engagement politique dans les sociétés démocratiques ?
            ("TERMINALE", "SEST_FORMES_ENGAGEMENT",              "Science politique — Engagement politique", "Illustrer les formes de l'engagement politique : vote, militantisme, engagement associatif, consommation engagée", 44),
            ("TERMINALE", "SEST_PARADOXE_ACTION_COLLECTIVE",     "Science politique — Engagement politique", "Expliquer l'engagement malgré le paradoxe de l'action collective : incitations sélectives, rétributions, opportunités politiques", 45),
            ("TERMINALE", "SEST_VARIABLES_ENGAGEMENT",           "Science politique — Engagement politique", "Montrer que l'engagement dépend de la catégorie socioprofessionnelle, du diplôme, de l'âge et de la génération, du sexe", 46),
            ("TERMINALE", "SEST_TRANSFORMATIONS_ACTION_COLL",    "Science politique — Engagement politique", "Décrire la diversité et les transformations des objets, des acteurs et des répertoires de l'action collective", 47),

            // Regards croisés : Quelles inégalités sont compatibles avec les différentes conceptions de la justice sociale ? (hors écrit 2027)
            ("TERMINALE", "SEST_INEGALITES_TENDANCES",           "Regards croisés — Inégalités et justice sociale", "Décrire l'évolution des inégalités économiques depuis le début du XXe siècle et leur caractère multiforme et cumulatif", 48),
            ("TERMINALE", "SEST_MESURE_INEGALITES",              "Regards croisés — Inégalités et justice sociale", "Interpréter rapport inter-quantiles, courbe de Lorenz, coefficient de Gini, top 1 % et corrélation des revenus parents-enfants", 49),
            ("TERMINALE", "SEST_CONCEPTIONS_JUSTICE",            "Regards croisés — Inégalités et justice sociale", "Relier égalité des droits, des chances ou des situations aux conceptions utilitariste, libertarienne et égalitaristes de la justice", 50),
            ("TERMINALE", "SEST_ACTION_PUBLIQUE_JUSTICE",        "Regards croisés — Inégalités et justice sociale", "Discuter l'action publique de justice sociale : contrainte de financement, efficacité, légitimité, effets désincitatifs", 51),

            // Regards croisés : Quelle action publique pour l'environnement ?
            ("TERMINALE", "SEST_PROBLEME_PUBLIC_ENVIRONNEMENT",  "Regards croisés — Action publique environnementale", "Identifier les acteurs qui font de l'environnement un problème public mis à l'agenda, et leurs coopérations et conflits", 52),
            ("TERMINALE", "SEST_ECHELLES_ACTION_PUBLIQUE",       "Regards croisés — Action publique environnementale", "Montrer que l'action publique pour l'environnement articule les échelles locale, nationale, européenne et mondiale", 53),
            ("TERMINALE", "SEST_INSTRUMENTS_CLIMAT",             "Regards croisés — Action publique environnementale", "Présenter réglementation, marchés de quotas d'émission, taxation et subvention à l'innovation verte face au changement climatique", 54),
            ("TERMINALE", "SEST_LIMITES_INSTRUMENTS",            "Regards croisés — Action publique environnementale", "Comparer avantages et limites de ces instruments et les dysfonctionnements possibles de l'action publique", 55),
            ("TERMINALE", "SEST_ACCORDS_INTERNATIONAUX",         "Regards croisés — Action publique environnementale", "Expliquer comment passager clandestin et inégalités de développement contraignent les accords sur un bien commun", 56),

            // Objectifs d'apprentissage concernant l'utilisation des données quantitatives et des représentations graphiques
            ("TERMINALE", "SEST_TABLES_DESTINEE_RECRUTEMENT",    "Données quantitatives et graphiques", "Transformer une table de mobilité en tables de destinée et de recrutement à l'aide de pourcentages de répartition", 57),
            ("TERMINALE", "SEST_TAUX_VARIATION_MOYENNES",        "Données quantitatives et graphiques", "Calculer un taux de variation, un coefficient multiplicateur, un indice, une moyenne pondérée et lire un taux de variation moyen", 58),
            ("TERMINALE", "SEST_MEDIANE_QUANTILES_GINI",         "Données quantitatives et graphiques", "Lire et interpréter une médiane, un écart et un rapport inter-quantile, un coefficient de Gini", 59),
            ("TERMINALE", "SEST_CORRELATION_CAUSALITE",          "Données quantitatives et graphiques", "Distinguer corrélation et causalité à partir d'un exemple de données", 60),
            ("TERMINALE", "SEST_TABLEAUX_GRAPHIQUES",            "Données quantitatives et graphiques", "Lire un tableau à double entrée, un diagramme de répartition, une série chronologique et une courbe de Lorenz", 61),
            ("TERMINALE", "SEST_NOMINAL_REEL_FONCTIONS",         "Données quantitatives et graphiques", "Distinguer valeur nominale et réelle, et interpréter pentes et déplacements des courbes d'offre, de demande et de coût", 62),

            // Compétences transversales de fin de terminale (préambule du cycle terminal)
            ("TERMINALE", "SEST_PROBLEMATIQUE",                  "Compétences transversales", "Construire la problématique d'un sujet en définissant ses termes et en dégageant le problème posé", 63),
            ("TERMINALE", "SEST_DISSERTATION",                   "Compétences transversales", "Bâtir le plan cohérent et équilibré d'une dissertation qui répond à la question en mobilisant dossier et connaissances", 64),
        };
    }
}
