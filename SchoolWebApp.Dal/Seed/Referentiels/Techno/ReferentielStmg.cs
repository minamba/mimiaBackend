namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// Les enseignements de spécialité de la série STMG, tels que les textes officiels les
    /// définissent — provenance détaillée en tête du fichier. Réparti entre les matières par
    /// `ReferentielsSeriesTechnologiques`.
    /// </summary>
    public static class ReferentielStmg
    {
        // =====================================================================================
        // RÉFÉRENTIELS STMG (série technologique « sciences et technologies du management et
        // de la gestion »), programmes EN VIGUEUR en 2026-2027.
        // Lecture des textes : 14/09/2026. Chaque libellé vient des colonnes « capacités »
        // (« L'élève est capable : ») et « notions » des annexes citées ci-dessous.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // Tableau | Niveau              | Texte officiel (intitulé exact de l'annexe)
        //         |                     | Arrêté / NOR / BO / entrée en vigueur (article 2 lu)
        //         |                     | PDF lu
        // -------------------------------------------------------------------------------------
        // DE1     | PREMIERE_STMG       | « Programme de droit et économie de première STMG »
        //         |                     | Annexe 1 de l'arrêté du 17-1-2019, NOR MENE1901646A,
        //         |                     | BO spécial n° 1 du 22-1-2019 ; rentrée 2019.
        //         |                     | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/86/8/spe646_annexe1_22-1_1063868.pdf
        // MGT1    | PREMIERE_STMG       | « Programme de management de première STMG »
        //         |                     | Annexe 2, même arrêté (MENE1901646A), même BO ; rentrée 2019.
        //         |                     | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/60/2/spe646_annexe2_1063602.pdf
        // SGN1    | PREMIERE_STMG       | « Programme de sciences de gestion et numérique de première STMG »
        //         |                     | Annexe 3, même arrêté (MENE1901646A), même BO ; rentrée 2019.
        //         |                     | https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/60/6/spe646_annexe3_1063606.pdf
        // DET     | TERMINALE_STMG      | « Programme de droit et économie de terminale STMG »
        //         |                     | Annexe 1 de l'arrêté du 19-7-2019, NOR MENE1921262A,
        //         |                     | BO spécial n° 8 du 25-7-2019 ; rentrée 2020.
        //         |                     | MODIFIÉE par l'arrêté du 25-1-2024, NOR MENE2401678A, JORF n° 47
        //         |                     | du 25-2-2024 (sous-thème 8.1 « L'entreprise individuelle » :
        //         |                     | « patrimoine personnel » -> « professionnel, mais » ; notion
        //         |                     | complétée par « et le statut de l'entrepreneur individuel »).
        //         |                     | Légifrance (loda JORFTEXT000038799988) : annexe 1 en vigueur
        //         |                     | depuis le 26-2-2024, annexe 2 inchangée depuis 2019.
        //         |                     | https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/13/9/spe262_annexe1_1159139.pdf
        //         |                     | https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000049192354
        // MSGNT   | TERMINALE_STMG      | « Programme de management, sciences de gestion et numérique de
        // GFT     | TERMINALE_STMG_GF   |   terminale STMG » : Enseignement commun + les quatre
        // MERT    | TERMINALE_STMG_MERC |   enseignements spécifiques, TOUS DANS LA MÊME ANNEXE 2
        // RHCT    | TERMINALE_STMG_RHC  | Annexe 2 de l'arrêté du 19-7-2019, NOR MENE1921262A,
        // SIGT    | TERMINALE_STMG_SIG  | BO spécial n° 8 du 25-7-2019 ; rentrée 2020.
        //         |                     | https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/14/1/spe262_annexe2_1159141.pdf
        // -------------------------------------------------------------------------------------
        // Vérifications de non-remplacement (14/09/2026) :
        //  - Légifrance, arrêté du 17-1-2019 (loda JORFTEXT000038029571) : aucune modification,
        //    « dernière mise à jour des données de ce texte : 02 septembre 2019 ».
        //  - Légifrance, arrêté du 19-7-2019 : seule modification = arrêté du 25-1-2024.
        //  - Éduscol « Programmes et ressources en série STMG » (page 5862) : liste toujours ces
        //    cinq PDF (BO spéciaux 1 et 8 de 2019) comme programmes en vigueur.
        //  - Aucun projet ni texte STMG trouvé dans les BO 2025-2026 (recherche web ; cette
        //    absence n'est pas une preuve, voir le rapport).
        // Tous les fichiers lus sont conservés dans techno\sources\stmg\.
        //
        // NOTES DE LECTURE
        //  - Domaine = titre court du thème officiel. Pour MSGNT, le programme ne sépare PAS
        //    management et gestion : ses trois thèmes mêlent les deux. Le préfixe « Management : »
        //    ou « Gestion : » est un classement fait ici (stratégie, organisation du travail,
        //    acteurs, éthique -> management ; marketing, finance, coûts, RH opérationnelle, SI,
        //    communication -> gestion). Ce n'est pas le texte.
        //  - La note de service du 12-7-2021 (MENE2121281N) qui retirait de l'examen plusieurs
        //    notions (ruine des bâtiments, grève, CSE, OMC, blockchain, cluster...) est ABROGÉE
        //    (version consolidée Éduscol d'août 2024) : ces notions sont gardées.
        // =====================================================================================
        
        // ------------------------------------------------------------------------------------
        // 1. Première STMG — Sciences de gestion et numérique (4 thèmes, 8 questions de gestion)
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SGN1 =
        {
            // Thème 1 : De l'individu à l'acteur
            ("PREMIERE_STMG", "SGN1_ORGA_TYPES",           "De l'individu à l'acteur", "Caractériser une organisation par ses buts, sa structure de propriété et le mode de contrôle des dirigeants", 1),
            ("PREMIERE_STMG", "SGN1_COMPORT_GROUPES",      "De l'individu à l'acteur", "Caractériser les comportements individuels au sein des groupes", 2),
            ("PREMIERE_STMG", "SGN1_IDENTITE_NUMERIQUE",   "De l'individu à l'acteur", "Identifier les bonnes pratiques de gestion de l'identité numérique", 3),
            ("PREMIERE_STMG", "SGN1_CULTURE_VALEURS",      "De l'individu à l'acteur", "Repérer ce qui, dans les relations, révèle la culture et les valeurs de l'organisation", 4),
            ("PREMIERE_STMG", "SGN1_SITUATION_COMM",       "De l'individu à l'acteur", "Analyser une situation de communication à partir de ses composantes et des phénomènes relationnels", 5),
            ("PREMIERE_STMG", "SGN1_QUALIF_COMPETENCE",    "De l'individu à l'acteur", "Distinguer l'approche par la qualification de l'approche par la compétence", 6),
            ("PREMIERE_STMG", "SGN1_INDICATEURS_ACTIVITE", "De l'individu à l'acteur", "Mesurer l'activité de travail à l'aide d'indicateurs pertinents", 7),
            ("PREMIERE_STMG", "SGN1_COUT_GLOBAL_TRAVAIL",  "De l'individu à l'acteur", "Évaluer le coût global du travail avec les charges", 8),
            ("PREMIERE_STMG", "SGN1_PRODUCTIVITE_TRAVAIL", "De l'individu à l'acteur", "Évaluer la productivité du travail", 9),
            ("PREMIERE_STMG", "SGN1_CONDITIONS_TRAVAIL",   "De l'individu à l'acteur", "Établir un lien entre les conditions de travail et le comportement des membres de l'organisation", 10),
        
            // Thème 2 : Numérique et intelligence collective
            ("PREMIERE_STMG", "SGN1_DONNEE_INFORMATION",   "Numérique et intelligence collective", "Repérer l'origine d'une information et les étapes de sa transformation, de la donnée à la connaissance", 11),
            ("PREMIERE_STMG", "SGN1_DONNEES_PERSONNELLES", "Numérique et intelligence collective", "Distinguer les données à caractère personnel et les contraintes de leur utilisation", 12),
            ("PREMIERE_STMG", "SGN1_DONNEES_OUVERTES",     "Numérique et intelligence collective", "Manipuler des données ouvertes pour créer de l'information", 13),
            ("PREMIERE_STMG", "SGN1_ENV_NUMERIQUE",        "Numérique et intelligence collective", "Se situer dans un environnement numérique : rôles, droits, responsabilités", 14),
            ("PREMIERE_STMG", "SGN1_USAGES_NUMERIQUE",     "Numérique et intelligence collective", "Comprendre la variété des usages du numérique dans les organisations et de leurs impacts", 15),
            ("PREMIERE_STMG", "SGN1_ARCHITECTURE_NUM",     "Numérique et intelligence collective", "Contribuer à l'architecture numérique globale de l'organisation", 16),
            ("PREMIERE_STMG", "SGN1_PROCESSUS_SCHEMA",     "Numérique et intelligence collective", "Identifier les étapes d'un processus de gestion et en schématiser l'enchaînement", 17),
            ("PREMIERE_STMG", "SGN1_AUTOMATISATION",       "Numérique et intelligence collective", "Repérer les effets de l'automatisation des activités de gestion sur l'information, le travail et les acteurs", 18),
            ("PREMIERE_STMG", "SGN1_ACTEURS_APPLI_SI",     "Numérique et intelligence collective", "Situer le rôle des acteurs et des applications du système d'information dans un processus de gestion", 19),
            ("PREMIERE_STMG", "SGN1_IA_ORGA_TACHES",       "Numérique et intelligence collective", "Imaginer une nouvelle organisation des tâches intégrant l'intelligence artificielle", 20),
        
            // Thème 3 : Création de valeur et performance
            ("PREMIERE_STMG", "SGN1_ACTEURS_VALEUR",       "Création de valeur et performance", "Identifier le rôle des différents acteurs dans le processus de création de valeur", 21),
            ("PREMIERE_STMG", "SGN1_TYPES_VALEUR",         "Création de valeur et performance", "Caractériser les types de valeur (financière, partenariale, perçue) et les relier aux attentes des acteurs", 22),
            ("PREMIERE_STMG", "SGN1_REPARTITION_VA",       "Création de valeur et performance", "Repérer, à partir de la valeur ajoutée, les répartitions possibles pour répondre aux attentes des acteurs", 23),
            ("PREMIERE_STMG", "SGN1_BILAN_COMPTE_RESULTAT", "Création de valeur et performance", "Utiliser un bilan et un compte de résultat pour repérer la valeur financière produite", 24),
            ("PREMIERE_STMG", "SGN1_INDICATEURS_VALEUR",   "Création de valeur et performance", "Utiliser des indicateurs simples pour repérer la valeur produite par l'organisation", 25),
            ("PREMIERE_STMG", "SGN1_PRIX_COUT_QUALITE",    "Création de valeur et performance", "Analyser la relation entre le prix, le coût et le niveau de qualité d'un produit ou d'un service", 26),
            ("PREMIERE_STMG", "SGN1_INDICATEURS_PERF",     "Création de valeur et performance", "Identifier les indicateurs de performance commerciale, financière, sociale et environnementale d'un tableau de bord", 27),
            ("PREMIERE_STMG", "SGN1_COMPARER_PERF",        "Création de valeur et performance", "Comparer dans le temps et dans l'espace la performance d'une organisation", 28),
            ("PREMIERE_STMG", "SGN1_ASPIRATIONS_ACTEURS",  "Création de valeur et performance", "Repérer en quoi les aspirations des acteurs sont contraintes ou opportunités dans la recherche de performance", 29),
            ("PREMIERE_STMG", "SGN1_PERF_CONTRADICTOIRES", "Création de valeur et performance", "Percevoir le caractère potentiellement contradictoire des différents types de performance", 30),
        
            // Thème 4 : Temps et risque
            ("PREMIERE_STMG", "SGN1_TEMPS_INCERTITUDE",    "Temps et risque", "Expliquer en quoi le temps est source d'incertitude et identifier les temps caractéristiques de l'organisation", 31),
            ("PREMIERE_STMG", "SGN1_ASYMETRIE_INFO",       "Temps et risque", "Expliquer pourquoi il peut y avoir rétention et asymétrie d'informations dans l'organisation", 32),
            ("PREMIERE_STMG", "SGN1_INFO_ACTUALISEE",      "Temps et risque", "Repérer l'importance d'une information actualisée et de la veille pour prendre des décisions pertinentes", 33),
            ("PREMIERE_STMG", "SGN1_SEUIL_RENTABILITE",    "Temps et risque", "Utiliser des données prospectives pour mesurer l'incidence de l'activité sur le résultat (seuil de rentabilité)", 34),
            ("PREMIERE_STMG", "SGN1_DEMARCHE_BUDGETAIRE",  "Temps et risque", "Utiliser des données prospectives pour mesurer l'incidence de l'activité sur la trésorerie (démarche budgétaire)", 35),
            ("PREMIERE_STMG", "SGN1_RISQUES_EXTERNES",     "Temps et risque", "Repérer les risques externes auxquels les organisations sont confrontées", 36),
            ("PREMIERE_STMG", "SGN1_RISQUES_DECISION",     "Temps et risque", "Repérer les risques induits par une décision dans un contexte organisationnel donné", 37),
            ("PREMIERE_STMG", "SGN1_RISQUE_PERFORMANCE",   "Temps et risque", "Apprécier l'incidence du risque sur la performance de l'organisation", 38),
            ("PREMIERE_STMG", "SGN1_CONSEQ_ECOLOGIQUES",   "Temps et risque", "Mesurer les conséquences écologiques de la recherche de performance", 39),
        };
        
        // ------------------------------------------------------------------------------------
        // 2. Première STMG — Management (3 thèmes, 11 questions)
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] MGT1 =
        {
            // Thème 1 : À la rencontre du management des organisations
            ("PREMIERE_STMG", "MGT1_ACTION_COLLECTIVE",    "À la rencontre du management", "Articuler action individuelle et action collective organisée", 1),
            ("PREMIERE_STMG", "MGT1_RESSOURCES",           "À la rencontre du management", "Identifier les ressources humaines, financières, matérielles, immatérielles et technologiques d'une organisation", 2),
            ("PREMIERE_STMG", "MGT1_CRITERES_ORGA",        "À la rencontre du management", "Identifier les critères permettant de distinguer les grandes catégories d'organisations", 3),
            ("PREMIERE_STMG", "MGT1_CARACTERISER_ORGA",    "À la rencontre du management", "Caractériser une organisation donnée : finalité, production, effectif, financement, secteur, champ géographique", 4),
            ("PREMIERE_STMG", "MGT1_ENTREPRISES_PRIVEES",  "À la rencontre du management", "Présenter les spécificités des entreprises privées : finalité lucrative et responsabilité sociale et environnementale", 5),
            ("PREMIERE_STMG", "MGT1_ORGA_PUBLIQUES",       "À la rencontre du management", "Présenter les spécificités des organisations et entreprises publiques et de leur financement", 6),
            ("PREMIERE_STMG", "MGT1_SOCIETE_CIVILE",       "À la rencontre du management", "Présenter les spécificités des organisations de la société civile : associations, ONG, syndicats, fondations", 7),
            ("PREMIERE_STMG", "MGT1_FONCTIONS_MGT",        "À la rencontre du management", "Définir le management et ses fonctions : fixer des objectifs, organiser, animer, mobiliser, contrôler, évaluer", 8),
            ("PREMIERE_STMG", "MGT1_MGT_PUBLIC_PRIVE",     "À la rencontre du management", "Distinguer management privé et management public", 9),
            ("PREMIERE_STMG", "MGT1_STRAT_OPERATIONNEL",   "À la rencontre du management", "Repérer les décisions relevant du management stratégique et celles du management opérationnel", 10),
            ("PREMIERE_STMG", "MGT1_EFFICACITE_MGT",       "À la rencontre du management", "Évaluer l'efficacité du management", 11),
            ("PREMIERE_STMG", "MGT1_PARTIES_PRENANTES",    "À la rencontre du management", "Identifier les parties prenantes de l'organisation vue comme un système complexe", 12),
            ("PREMIERE_STMG", "MGT1_REPONSE_ENVIRONNEMENT", "À la rencontre du management", "Montrer en quoi les décisions managériales répondent aux évolutions de l'environnement", 13),
            ("PREMIERE_STMG", "MGT1_NUMERIQUE_ECOLOGIE_RSE", "À la rencontre du management", "Analyser l'effet des transformations numériques, des mutations écologiques et de la RSE sur le management", 14),
        
            // Thème 2 : Le management stratégique, du diagnostic à la fixation des objectifs
            ("PREMIERE_STMG", "MGT1_DEF_STRATEGIE",        "Management stratégique", "Définir la notion de stratégie et repérer les étapes de la démarche stratégique", 15),
            ("PREMIERE_STMG", "MGT1_VEILLE_STRATEGIQUE",   "Management stratégique", "Expliquer le rôle de la veille stratégique dans la démarche stratégique", 16),
            ("PREMIERE_STMG", "MGT1_DIAG_INTERNE",         "Management stratégique", "Réaliser un diagnostic interne : forces, faiblesses, ressources et compétences distinctives", 17),
            ("PREMIERE_STMG", "MGT1_DIAG_EXTERNE",         "Management stratégique", "Réaliser un diagnostic externe : opportunités, menaces et facteurs clés de succès", 18),
            ("PREMIERE_STMG", "MGT1_OBJECTIFS_STRAT",      "Management stratégique", "Déterminer les objectifs et les décisions stratégiques à partir de la finalité et du diagnostic", 19),
            ("PREMIERE_STMG", "MGT1_CONFLITS_CONSENSUS",   "Management stratégique", "Identifier des nœuds de conflit et des points de consensus autour des objectifs stratégiques", 20),
            ("PREMIERE_STMG", "MGT1_INDICATEURS",          "Management stratégique", "Questionner la pertinence des indicateurs de résultats", 21),
            ("PREMIERE_STMG", "MGT1_MESURES_CORRECTRICES", "Management stratégique", "Évaluer la performance de l'organisation grâce aux indicateurs et proposer des mesures correctrices", 22),
        
            // Thème 3 : Les choix stratégiques des organisations
            ("PREMIERE_STMG", "MGT1_FINALITES_NIVEAUX",    "Choix stratégiques des organisations", "Repérer les finalités des organisations et les niveaux auxquels s'opèrent les choix stratégiques", 23),
            ("PREMIERE_STMG", "MGT1_DAS",                  "Choix stratégiques des organisations", "Segmenter les activités d'une entreprise en domaines d'activité stratégique (DAS)", 24),
            ("PREMIERE_STMG", "MGT1_STRAT_GLOBALE",        "Choix stratégiques des organisations", "Distinguer stratégie globale (spécialisation, diversification) et stratégie de domaine", 25),
            ("PREMIERE_STMG", "MGT1_AVANTAGE_CONCURRENTIEL", "Choix stratégiques des organisations", "Caractériser l'avantage concurrentiel : domination par les coûts ou différenciation", 26),
            ("PREMIERE_STMG", "MGT1_CHAINE_VALEUR",        "Choix stratégiques des organisations", "Analyser les choix sur la chaîne de valeur : externalisation, intégration", 27),
            ("PREMIERE_STMG", "MGT1_MODALITES_DEVELOPPEMENT", "Choix stratégiques des organisations", "Distinguer croissance interne, croissance externe, partenariats et internationalisation", 28),
            ("PREMIERE_STMG", "MGT1_NUMERISATION_ECONOMIE", "Choix stratégiques des organisations", "Prendre en compte la numérisation de l'économie dans les choix stratégiques", 29),
            ("PREMIERE_STMG", "MGT1_STRAT_PUBLIQUES",      "Choix stratégiques des organisations", "Analyser les stratégies des organisations publiques : intérêt général, service public, délégation de service public", 30),
            ("PREMIERE_STMG", "MGT1_NIVEAUX_ACTION_PUBLIQUE", "Choix stratégiques des organisations", "Situer l'action publique aux niveaux central, territorial et européen, face aux groupes de pression", 31),
            ("PREMIERE_STMG", "MGT1_STRAT_SOCIETE_CIVILE", "Choix stratégiques des organisations", "Analyser la stratégie d'une organisation de la société civile au regard de son objet social et de ses ressources", 32),
            ("PREMIERE_STMG", "MGT1_TRANSPARENCE",         "Choix stratégiques des organisations", "Évaluer le degré de transparence des objectifs dans les trois types d'organisations", 33),
        };
        
        // ------------------------------------------------------------------------------------
        // 3. Première STMG — Droit et économie (droit : thèmes 1 à 4 ; économie : thèmes 1 à 5)
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] DE1 =
        {
            // Droit, thème 1 : Qu'est-ce que le droit ?
            ("PREMIERE_STMG", "DE1_FONCTIONS_DROIT",       "Droit : qu'est-ce que le droit ?", "Expliquer et distinguer les fonctions du droit, et distinguer droit et morale", 1),
            ("PREMIERE_STMG", "DE1_CARACTERES_REGLE",      "Droit : qu'est-ce que le droit ?", "Vérifier les caractères de la règle de droit : légitime, générale et obligatoire", 2),
            ("PREMIERE_STMG", "DE1_QUALIFICATION",         "Droit : qu'est-ce que le droit ?", "Qualifier juridiquement une situation de fait", 3),
            ("PREMIERE_STMG", "DE1_SOURCES_DROIT",         "Droit : qu'est-ce que le droit ?", "Identifier la source d'une règle de droit, l'institution qui l'édicte et sa place dans la hiérarchie des normes", 4),
            ("PREMIERE_STMG", "DE1_DECISION_JUSTICE",      "Droit : qu'est-ce que le droit ?", "Expliquer le sens et la portée d'une décision de justice", 5),
        
            // Droit, thème 2 : Comment le droit permet-il de régler un litige ?
            ("PREMIERE_STMG", "DE1_ELEMENTS_LITIGE",       "Droit : régler un litige", "Identifier les éléments d'un litige : parties, faits, prétentions, question de droit", 6),
            ("PREMIERE_STMG", "DE1_ACTE_FAIT_JURIDIQUE",   "Droit : régler un litige", "Déterminer si un litige naît d'un acte ou d'un fait juridique pour envisager un mode de preuve adapté", 7),
            ("PREMIERE_STMG", "DE1_FORCE_PROBANTE",        "Droit : régler un litige", "Apprécier la force probante d'une preuve : acte authentique, sous signature privée, électronique, témoignage, aveu", 8),
            ("PREMIERE_STMG", "DE1_JURIDICTION",           "Droit : régler un litige", "Sélectionner la juridiction compétente et déterminer la juridiction qui a rendu une décision", 9),
            ("PREMIERE_STMG", "DE1_PROCES_CIVIL_PENAL",    "Droit : régler un litige", "Distinguer procès civil et procès pénal, identifier les phases d'un procès et les enjeux de la partie civile", 10),
            ("PREMIERE_STMG", "DE1_VOIES_RECOURS",         "Droit : régler un litige", "Expliquer les voies de recours : appel, pourvoi en cassation", 11),
        
            // Droit, thème 3 : Qui peut faire valoir ses droits ?
            ("PREMIERE_STMG", "DE1_PERSONNE_JURIDIQUE",    "Droit : la personne juridique", "Identifier et qualifier une personne juridique, physique ou morale", 12),
            ("PREMIERE_STMG", "DE1_ATTRIBUTS_PERSONNE",    "Droit : la personne juridique", "Analyser les conséquences de la personnalité juridique et identifier les attributs d'une personne", 13),
            ("PREMIERE_STMG", "DE1_INCAPACITE",            "Droit : la personne juridique", "Expliquer les conséquences de l'incapacité juridique et les mécanismes de la représentation", 14),
            ("PREMIERE_STMG", "DE1_PATRIMOINE",            "Droit : la personne juridique", "Définir le patrimoine d'une personne juridique", 15),
        
            // Droit, thème 4 : Quels sont les droits reconnus aux personnes ?
            ("PREMIERE_STMG", "DE1_PATRIM_EXTRAPATRIM",    "Droit : droits des personnes", "Distinguer les droits patrimoniaux et les droits extrapatrimoniaux", 16),
            ("PREMIERE_STMG", "DE1_ATTEINTE_EXTRAPATRIM",  "Droit : droits des personnes", "Identifier une atteinte à la vie privée ou au droit à l'image et appliquer les règles applicables", 17),
            ("PREMIERE_STMG", "DE1_DONNEES_PERSONNELLES",  "Droit : droits des personnes", "Expliquer les enjeux de la protection des données personnelles et vérifier le respect des obligations", 18),
            ("PREMIERE_STMG", "DE1_BIENS_CORP_INCORP",     "Droit : droits des personnes", "Distinguer biens corporels et biens incorporels", 19),
            ("PREMIERE_STMG", "DE1_DROIT_PROPRIETE",       "Droit : droits des personnes", "Identifier les attributs et caractères du droit de propriété et qualifier un trouble anormal du voisinage", 20),
            ("PREMIERE_STMG", "DE1_DROIT_AUTEUR",          "Droit : droits des personnes", "Identifier les composantes du droit d'auteur", 21),
            ("PREMIERE_STMG", "DE1_MARQUE",                "Droit : droits des personnes", "Connaître les enjeux de la protection de la marque et les conséquences de son utilisation non autorisée", 22),
        
            // Économie, thème 1 : Quelles sont les grandes questions économiques ?
            ("PREMIERE_STMG", "DE1_AGENTS_BIENS",          "Économie : grandes questions économiques", "Identifier les agents économiques et leurs fonctions, et distinguer la nature des biens et services", 23),
            ("PREMIERE_STMG", "DE1_CHOIX_ECONOMIQUES",     "Économie : grandes questions économiques", "Décrire les choix économiques à l'aide du coût d'opportunité, de l'utilité, de la rationalité et des préférences", 24),
            ("PREMIERE_STMG", "DE1_UTILITE_MARGINALE",     "Économie : grandes questions économiques", "Expliciter l'utilité marginale et le raisonnement à la marge (coût marginal et recette marginale)", 25),
            ("PREMIERE_STMG", "DE1_MONNAIE_CIRCUIT",       "Économie : grandes questions économiques", "Définir les fonctions de la monnaie et représenter un circuit économique élémentaire", 26),
        
            // Économie, thème 2 : Comment la richesse se crée-t-elle et se répartit-elle ?
            ("PREMIERE_STMG", "DE1_FACTEURS_PRODUCTION",   "Économie : création et répartition de la richesse", "Identifier les facteurs de production et distinguer un input d'un output", 27),
            ("PREMIERE_STMG", "DE1_PRODUCTIVITE",          "Économie : création et répartition de la richesse", "Définir la productivité et analyser l'évolution des gains de productivité", 28),
            ("PREMIERE_STMG", "DE1_VALEUR_AJOUTEE_PIB",    "Économie : création et répartition de la richesse", "Calculer la valeur ajoutée dans des cas simples et expliquer le calcul du PIB", 29),
            ("PREMIERE_STMG", "DE1_LIMITES_PIB",           "Économie : création et répartition de la richesse", "Discuter les limites du PIB et utiliser des indicateurs complémentaires comme l'IDH", 30),
            ("PREMIERE_STMG", "DE1_REVENUS_PRIMAIRES",     "Économie : création et répartition de la richesse", "Distinguer les revenus du travail, du capital et mixtes selon leur origine", 31),
            ("PREMIERE_STMG", "DE1_PARTAGE_VA",            "Économie : création et répartition de la richesse", "Analyser le partage de la valeur ajoutée à partir d'un graphique", 32),
            ("PREMIERE_STMG", "DE1_REDISTRIBUTION",        "Économie : création et répartition de la richesse", "Expliquer le passage au revenu disponible par les prélèvements obligatoires et les revenus de transfert", 33),
        
            // Économie, thème 3 : Comment les ménages décident-ils d'affecter leur revenu ?
            ("PREMIERE_STMG", "DE1_DETERMINANTS_CONSO",    "Économie : affectation du revenu", "Identifier les déterminants de la consommation et de l'épargne", 34),
            ("PREMIERE_STMG", "DE1_PROPENSIONS",           "Économie : affectation du revenu", "Calculer et interpréter les propensions moyenne et marginale à consommer et à épargner", 35),
            ("PREMIERE_STMG", "DE1_EPARGNE_PATRIMOINE",    "Économie : affectation du revenu", "Montrer le lien entre épargne, revenu et patrimoine et interpréter un tableau de leur répartition", 36),
            ("PREMIERE_STMG", "DE1_POUVOIR_ACHAT",         "Économie : affectation du revenu", "Expliquer l'évolution du pouvoir d'achat à l'aide de l'indice des prix à la consommation", 37),
            ("PREMIERE_STMG", "DE1_STRUCTURE_CONSO",       "Économie : affectation du revenu", "Analyser l'évolution de la structure de consommation en valeur et en volume (coefficients budgétaires)", 38),
        
            // Économie, thème 4 : Quels modes de financement de l'activité économique ?
            ("PREMIERE_STMG", "DE1_BESOIN_CAPACITE",       "Économie : financement de l'activité", "Identifier les agents à besoin et à capacité de financement", 39),
            ("PREMIERE_STMG", "DE1_MODES_FINANCEMENT",     "Économie : financement de l'activité", "Comparer autofinancement, financement direct par le marché financier et financement indirect bancaire", 40),
            ("PREMIERE_STMG", "DE1_ROLE_BANQUES",          "Économie : financement de l'activité", "Caractériser le rôle des banques dans le financement de l'économie et la création monétaire", 41),
            ("PREMIERE_STMG", "DE1_MARCHES_FIN_BCE",       "Économie : financement de l'activité", "Analyser les fonctions des marchés financiers et le rôle de la Banque centrale européenne", 42),
        
            // Économie, thème 5 : Les marchés des biens et services sont-ils concurrentiels ?
            ("PREMIERE_STMG", "DE1_MARCHE_PERTINENT",      "Économie : marchés et concurrence", "Définir un marché pertinent et identifier les intervenants sur un marché", 43),
            ("PREMIERE_STMG", "DE1_CONCENTRATION",         "Économie : marchés et concurrence", "Calculer un degré de concentration et caractériser concurrence, oligopole et monopole", 44),
            ("PREMIERE_STMG", "DE1_PRIX_EQUILIBRE",        "Économie : marchés et concurrence", "Déterminer la fixation du prix d'équilibre sur un marché concurrentiel", 45),
            ("PREMIERE_STMG", "DE1_ELASTICITE_PRIX",       "Économie : marchés et concurrence", "Calculer et interpréter une élasticité prix de la demande dans des exemples simples", 46),
            ("PREMIERE_STMG", "DE1_STRATEGIES_PRIX",       "Économie : marchés et concurrence", "Analyser les stratégies de prix selon la structure des coûts et la concentration du marché", 47),
            ("PREMIERE_STMG", "DE1_COUT_MOYEN_MARGINAL",   "Économie : marchés et concurrence", "Calculer un coût moyen et un coût marginal de production et interpréter les résultats", 48),
            ("PREMIERE_STMG", "DE1_INNOVATION_DIFFERENC",  "Économie : marchés et concurrence", "Expliquer comment innovation et différenciation des produits permettent de dépasser l'intensité concurrentielle", 49),
        };
        
        // ------------------------------------------------------------------------------------
        // 4. Terminale STMG — Droit et économie (droit : thèmes 5 à 8 ; économie : thèmes 6 à 9)
        //    Version modifiée par l'arrêté du 25-1-2024 (sous-thème 8.1).
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] DET =
        {
            // Droit, thème 5 : Quel est le rôle du contrat ?
            ("TERMINALE_STMG", "DET_QUALIFIER_CONTRAT",     "Droit : le contrat", "Qualifier juridiquement un contrat et les parties : consommateur, professionnel, débiteur, créancier", 1),
            ("TERMINALE_STMG", "DET_VALIDITE_CONTRAT",      "Droit : le contrat", "Apprécier les conditions de validité d'un contrat et sa sanction par la nullité relative ou absolue", 2),
            ("TERMINALE_STMG", "DET_PRINCIPES_CONTRAT",     "Droit : le contrat", "Expliquer la liberté contractuelle, la force obligatoire, l'effet relatif et la bonne foi", 3),
            ("TERMINALE_STMG", "DET_CLAUSES_OBLIGATIONS",   "Droit : le contrat", "Qualifier une clause et les obligations des parties : obligation de moyens, obligation de résultat", 4),
            ("TERMINALE_STMG", "DET_INEXECUTION",           "Droit : le contrat", "Identifier les sanctions de l'inexécution : exception d'inexécution, mise en demeure, résolution, résiliation", 5),
            ("TERMINALE_STMG", "DET_CYBERCONSOMMATEUR",     "Droit : le contrat", "Justifier la protection du consommateur et du cyberconsommateur : information, rétractation, clause abusive", 6),
        
            // Droit, thème 6 : Qu'est-ce qu'être responsable ?
            ("TERMINALE_STMG", "DET_CIVILE_PENALE",         "Droit : la responsabilité", "Distinguer la responsabilité civile de la responsabilité pénale", 7),
            ("TERMINALE_STMG", "DET_DOMMAGE_REPARABLE",     "Droit : la responsabilité", "Qualifier les dommages réparables : corporel, matériel, moral, préjudice écologique", 8),
            ("TERMINALE_STMG", "DET_CONDITIONS_RESP",       "Droit : la responsabilité", "Vérifier les conditions de la responsabilité : fait générateur ou faute, dommage, lien de causalité", 9),
            ("TERMINALE_STMG", "DET_REGIMES_SPECIAUX",      "Droit : la responsabilité", "Identifier les régimes spéciaux : accident du travail, accident de la circulation, produits défectueux", 10),
            ("TERMINALE_STMG", "DET_RESP_CONTRACTUELLE",    "Droit : la responsabilité", "Appliquer la responsabilité contractuelle : obligation de sécurité, clause limitative ou exonératoire", 11),
            ("TERMINALE_STMG", "DET_RESP_EXTRACONTRACT",    "Droit : la responsabilité", "Appliquer la responsabilité extracontractuelle : fait personnel, des choses, d'autrui, des animaux, ruine des bâtiments", 12),
            ("TERMINALE_STMG", "DET_EXONERATION",           "Droit : la responsabilité", "Apprécier les moyens d'exonération : force majeure, fait d'un tiers, faute de la victime", 13),
            ("TERMINALE_STMG", "DET_INDEMNISATION_ASSUR",   "Droit : la responsabilité", "Expliquer la construction du système d'indemnisation des victimes et la mutualisation des risques par l'assurance", 14),
        
            // Droit, thème 7 : Comment le droit encadre-t-il le travail salarié ?
            ("TERMINALE_STMG", "DET_SUBORDINATION",         "Droit : le travail salarié", "Qualifier une relation de travail par le lien de subordination et la distinguer du contrat d'entreprise", 15),
            ("TERMINALE_STMG", "DET_POUVOIRS_EMPLOYEUR",    "Droit : le travail salarié", "Identifier les pouvoirs de direction, réglementaire et disciplinaire de l'employeur", 16),
            ("TERMINALE_STMG", "DET_CDI_CONFORMITE",        "Droit : le travail salarié", "Vérifier la conformité d'un CDI aux normes, y compris la convention collective ou l'accord d'entreprise", 17),
            ("TERMINALE_STMG", "DET_CLAUSES_SPECIFIQUES",   "Droit : le travail salarié", "Vérifier la validité des clauses de mobilité, de non-concurrence et de télétravail", 18),
            ("TERMINALE_STMG", "DET_AUTRES_CONTRATS",       "Droit : le travail salarié", "Présenter les spécificités du CDD, du CDI de chantier, du contrat saisonnier et du travail temporaire", 19),
            ("TERMINALE_STMG", "DET_RUPTURE_LICENCIEMENT",  "Droit : le travail salarié", "Vérifier la validité d'une rupture du contrat de travail et distinguer licenciement personnel et économique", 20),
            ("TERMINALE_STMG", "DET_LIBERTES_INDIVIDUELLES", "Droit : le travail salarié", "Apprécier les limites des libertés individuelles du salarié : expression, vie privée, devoir de loyauté", 21),
            ("TERMINALE_STMG", "DET_GREVE",                 "Droit : le travail salarié", "Apprécier le caractère licite d'une grève", 22),
            ("TERMINALE_STMG", "DET_REPRESENTANTS",         "Droit : le travail salarié", "Expliquer les rôles des partenaires sociaux, du comité social et économique et du délégué syndical", 23),
        
            // Droit, thème 8 : Dans quel cadre et comment entreprendre ?
            ("TERMINALE_STMG", "DET_ENTREPRENEUR_INDIV",    "Droit : entreprendre", "Expliquer les principes de l'entreprise individuelle et le statut de l'entrepreneur individuel", 24),
            ("TERMINALE_STMG", "DET_PATRIMOINE_FORME",      "Droit : entreprendre", "Distinguer les conséquences de la forme juridique sur le patrimoine : unicité, patrimoine d'affectation, EURL", 25),
            ("TERMINALE_STMG", "DET_CHOIX_FORME",           "Droit : entreprendre", "Proposer et justifier le choix d'une forme juridique d'entreprise dans une situation donnée", 26),
            ("TERMINALE_STMG", "DET_CONTRAT_SOCIETE",       "Droit : entreprendre", "Identifier les éléments du contrat de société : apports, affectio societatis, limitation de responsabilité", 27),
            ("TERMINALE_STMG", "DET_COOPERATIVE",           "Droit : entreprendre", "Identifier les caractéristiques de la société coopérative (SCOP)", 28),
            ("TERMINALE_STMG", "DET_CONCURRENCE_DELOYALE",  "Droit : entreprendre", "Distinguer pratiques loyales et concurrence déloyale et expliquer l'action en concurrence déloyale", 29),
            ("TERMINALE_STMG", "DET_PARTENARIATS",          "Droit : entreprendre", "Identifier les obligations des contrats de franchise et d'entreprise et repérer entente et abus de position dominante", 30),
        
            // Économie, thème 6 : Comment l'État peut-il intervenir dans l'économie ?
            ("TERMINALE_STMG", "DET_ETAT_GENDARME_PROVID",  "Économie : intervention de l'État", "Expliquer les différences entre État-gendarme et État-providence et la participation de l'État aux entreprises", 31),
            ("TERMINALE_STMG", "DET_DEFICIT_DETTE",         "Économie : intervention de l'État", "Distinguer le déficit public de la dette publique", 32),
            ("TERMINALE_STMG", "DET_DEFAILLANCES",          "Économie : intervention de l'État", "Justifier l'intervention de l'État face aux défaillances de marché et citer des défaillances de l'État", 33),
            ("TERMINALE_STMG", "DET_POLITIQUES_ECO",        "Économie : intervention de l'État", "Distinguer politiques budgétaire et monétaire, politiques d'offre et de demande, conjoncturelles ou structurelles", 34),
            ("TERMINALE_STMG", "DET_CADRE_EUROPEEN",        "Économie : intervention de l'État", "Décrire l'évolution du rôle de l'État dans le cadre européen : politique monétaire, critères de déficit", 35),
            ("TERMINALE_STMG", "DET_FINANCEMENT_PUBLIC",    "Économie : intervention de l'État", "Énumérer les modes de financement des dépenses publiques et caractériser la progressivité des prélèvements", 36),
            ("TERMINALE_STMG", "DET_PROTECTION_SOCIALE",    "Économie : intervention de l'État", "Expliquer les risques sociaux couverts et distinguer redistribution horizontale et verticale", 37),
        
            // Économie, thème 7 : Quelle est l'influence de l'État sur l'emploi et le chômage ?
            ("TERMINALE_STMG", "DET_TRAVAIL_EMPLOI",        "Économie : emploi et chômage", "Distinguer offre et demande de travail, offre et demande d'emploi, population active et inactive", 38),
            ("TERMINALE_STMG", "DET_TAUX_ACTIVITE_CHOMAGE", "Économie : emploi et chômage", "Calculer et interpréter le taux d'activité, le taux de chômage et le taux d'emploi", 39),
            ("TERMINALE_STMG", "DET_FORMES_CHOMAGE",        "Économie : emploi et chômage", "Définir plein emploi et sous-emploi et distinguer les formes de chômage", 40),
            ("TERMINALE_STMG", "DET_IMPERFECTIONS_TRAVAIL", "Économie : emploi et chômage", "Identifier les imperfections du marché du travail : salaire minimum, contraintes légales et conventionnelles", 41),
            ("TERMINALE_STMG", "DET_POLITIQUES_EMPLOI",     "Économie : emploi et chômage", "Catégoriser les politiques de l'emploi en politiques actives ou passives", 42),
        
            // Économie, thème 8 : Comment organiser le commerce international ?
            ("TERMINALE_STMG", "DET_ECHANGES_BALANCE",      "Économie : commerce international", "Expliquer le développement et la régionalisation des échanges et interpréter la balance des biens et services", 43),
            ("TERMINALE_STMG", "DET_CHAINE_VALEUR_MONDE",   "Économie : commerce international", "Décrire la mondialisation de la chaîne de valeur et les flux créés par la segmentation de la production", 44),
            ("TERMINALE_STMG", "DET_IDE_FMN",               "Économie : commerce international", "Interpréter les flux d'IDE et apprécier la place des firmes multinationales", 45),
            ("TERMINALE_STMG", "DET_LIBRE_ECHANGE",         "Économie : commerce international", "Décrire avantages et limites du libre-échange et de l'autarcie et expliquer les mesures protectionnistes", 46),
            ("TERMINALE_STMG", "DET_OMC",                   "Économie : commerce international", "Définir les missions et règles de l'OMC, dont l'organe de règlement des différends", 47),
        
            // Économie, thème 9 : Comment concilier croissance et développement durable ?
            ("TERMINALE_STMG", "DET_CROISSANCE_DD",         "Économie : croissance et développement durable", "Définir croissance économique et développement durable et interpréter leurs indicateurs", 48),
            ("TERMINALE_STMG", "DET_PAUVRETE_EDUCATION",    "Économie : croissance et développement durable", "Distinguer pauvreté absolue et pauvreté relative et expliquer le rôle de l'éducation et de la formation", 49),
            ("TERMINALE_STMG", "DET_RESSOURCES_TRANSITION", "Économie : croissance et développement durable", "Distinguer ressources renouvelables et non renouvelables et identifier les instruments de la transition écologique", 50),
            ("TERMINALE_STMG", "DET_IMPACT_TRANSITION",     "Économie : croissance et développement durable", "Discuter l'impact des instruments de la transition écologique sur la production, la consommation et l'investissement", 51),
            ("TERMINALE_STMG", "DET_CIRCULAIRE_ESS",        "Économie : croissance et développement durable", "Décrire l'économie collaborative et circulaire et identifier les acteurs de l'économie sociale et solidaire", 52),
            ("TERMINALE_STMG", "DET_BIENS_PUBLICS_MONDIAUX", "Économie : croissance et développement durable", "Définir un bien public mondial et expliquer la nécessité de la coopération internationale", 53),
        };
        
        // ------------------------------------------------------------------------------------
        // 5. Terminale STMG — Management, sciences de gestion et numérique : ENSEIGNEMENT COMMUN
        //    (3 thèmes, 12 questions ; environ 60 % de l'horaire selon le texte).
        //    Préfixe « Management : » / « Gestion : » = classement fait ici, pas officiel.
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] MSGNT =
        {
            // Thème 1 : Les organisations et l'activité de production de biens et de services
            ("TERMINALE_STMG", "MSGNT_DEMARCHE_MARKETING",    "Gestion : organisations et production", "Présenter les caractéristiques du marché et la détection des tendances et besoins : étude de marché, veille", 1),
            ("TERMINALE_STMG", "MSGNT_APPROCHES_MARKETING",   "Gestion : organisations et production", "Distinguer les approches marketing anticipatrice, créative, réactive et médiatrice", 2),
            ("TERMINALE_STMG", "MSGNT_CREATION_VALEUR",       "Gestion : organisations et production", "Analyser la création de valeur à l'aide d'indicateurs extraits du compte de résultat et du bilan", 3),
            ("TERMINALE_STMG", "MSGNT_MODELE_ECONOMIQUE",     "Management : organisations et production", "Analyser le modèle économique d'une organisation et l'innovation de produits (plateforme, low cost, freemium)", 4),
            ("TERMINALE_STMG", "MSGNT_RESSOURCES_TANGIBLES",  "Management : organisations et production", "Distinguer les ressources tangibles et intangibles nécessaires pour produire", 5),
            ("TERMINALE_STMG", "MSGNT_CHOIX_FINANCEMENT",     "Gestion : organisations et production", "Identifier les choix de financement de l'investissement et de l'exploitation", 6),
            ("TERMINALE_STMG", "MSGNT_ANALYSE_FONCTIONNELLE", "Gestion : organisations et production", "Apprécier les besoins financiers à l'aide du FRNG, du BFR et de la trésorerie nette", 7),
            ("TERMINALE_STMG", "MSGNT_GPEC",                  "Gestion : organisations et production", "Repérer les moyens d'une gestion prévisionnelle des emplois et des compétences : recrutement, formation", 8),
            ("TERMINALE_STMG", "MSGNT_MODE_PRODUCTION",       "Management : organisations et production", "Identifier et justifier le mode de production choisi : unité, série, continu, discontinu, services", 9),
            ("TERMINALE_STMG", "MSGNT_INNOVATION_PROCEDES",   "Management : organisations et production", "Expliquer l'innovation de procédés au service de la flexibilité, de la qualité et de la maîtrise des coûts", 10),
            ("TERMINALE_STMG", "MSGNT_LOGISTIQUE",            "Gestion : organisations et production", "Analyser la logistique et la gestion de la chaîne logistique : flux tendus, flux poussés, flux physiques et d'informations", 11),
            ("TERMINALE_STMG", "MSGNT_QUALITE",               "Gestion : organisations et production", "Analyser la qualité des biens et des services comme enjeu concurrentiel : contrôle, amélioration continue", 12),
            ("TERMINALE_STMG", "MSGNT_CONTROLE_COUTS",        "Gestion : organisations et production", "Déterminer la pertinence d'un contrôle des coûts selon la méthode du coût complet ou du coût spécifique", 13),
            ("TERMINALE_STMG", "MSGNT_CYCLE_VIE_PLM",         "Gestion : organisations et production", "Intégrer la gestion du cycle de vie des produits (PLM) dans la détermination des coûts", 14),
            ("TERMINALE_STMG", "MSGNT_NUMERIQUE_PRODUCTION",  "Gestion : organisations et production", "Identifier le rôle du numérique dans la production : dématérialisation, workflow, cloud, objets connectés, IA", 15),
            ("TERMINALE_STMG", "MSGNT_DIAGRAMME_FLUX",        "Gestion : organisations et production", "Représenter la circulation des données et des informations par un diagramme des flux", 16),
            ("TERMINALE_STMG", "MSGNT_ORGANISATION_TRAVAIL",  "Management : organisations et production", "Distinguer et justifier une organisation du travail souple ou rigide (taylorisme, toyotisme)", 17),
            ("TERMINALE_STMG", "MSGNT_COORDINATION",          "Management : organisations et production", "Décrire les mécanismes de coordination du travail mis en place", 18),
            ("TERMINALE_STMG", "MSGNT_CENTRALISATION",        "Management : organisations et production", "Apprécier la ligne hiérarchique, le degré de centralisation du pouvoir et le lean management", 19),
        
            // Thème 2 : Les organisations et les acteurs
            ("TERMINALE_STMG", "MSGNT_ACTEURS_INTERETS",      "Management : organisations et acteurs", "Identifier les acteurs internes de l'organisation et leurs intérêts convergents ou divergents", 20),
            ("TERMINALE_STMG", "MSGNT_CULTURE_ORGA",          "Management : organisations et acteurs", "Expliquer le rôle de la culture de l'organisation dans la cohésion de ses membres", 21),
            ("TERMINALE_STMG", "MSGNT_STYLES_DIRECTION",      "Management : organisations et acteurs", "Reconnaître les styles de direction : paternaliste, autoritaire, consultatif, participatif", 22),
            ("TERMINALE_STMG", "MSGNT_DYNAMIQUE_GROUPE",      "Management : organisations et acteurs", "Analyser la dynamique de groupe : leadership, cohésion, décision de groupe", 23),
            ("TERMINALE_STMG", "MSGNT_COOPERATION",           "Management : organisations et acteurs", "Identifier les modalités de coopération et les outils collaboratifs d'une organisation", 24),
            ("TERMINALE_STMG", "MSGNT_MOTIVATION_QVT",        "Management : organisations et acteurs", "Distinguer les facteurs de motivation et le rôle de la qualité de vie au travail", 25),
            ("TERMINALE_STMG", "MSGNT_COMPORTEMENT_CONSO",    "Gestion : organisations et acteurs", "Analyser le processus d'achat et les facteurs explicatifs du comportement du consommateur", 26),
            ("TERMINALE_STMG", "MSGNT_RELATION_CLIENT_NUM",   "Gestion : organisations et acteurs", "Décrire l'apport du numérique aux relations avec les clients et usagers : digitalisation, administration électronique", 27),
            ("TERMINALE_STMG", "MSGNT_STRATEGIE_COMM",        "Gestion : organisations et acteurs", "Distinguer communication interne, externe, institutionnelle et commerciale dans une stratégie de communication", 28),
            ("TERMINALE_STMG", "MSGNT_IDENTITE_ORGA",         "Gestion : organisations et acteurs", "Analyser l'identité de l'organisation : marque employeur, e-réputation, identité numérique", 29),
            ("TERMINALE_STMG", "MSGNT_COMM_FINANCIERE",       "Gestion : organisations et acteurs", "Expliquer les besoins d'information financière des partenaires et le rôle du plan d'affaires", 30),
        
            // Thème 3 : Les organisations et la société
            ("TERMINALE_STMG", "MSGNT_ETHIQUE",               "Management : organisations et société", "Préciser les enjeux éthiques de l'activité d'une organisation : déontologie, lobbying, greenwashing", 31),
            ("TERMINALE_STMG", "MSGNT_NORMALISATION_COMPTA",  "Gestion : organisations et société", "Expliquer les principes de la normalisation comptable et la transparence de l'information financière", 32),
            ("TERMINALE_STMG", "MSGNT_DISCRIMINATIONS",       "Management : organisations et société", "Analyser la lutte contre les discriminations et l'égalité femmes-hommes dans les relations de travail", 33),
            ("TERMINALE_STMG", "MSGNT_MODES_VIE",             "Management : organisations et société", "Analyser les évolutions des modes de vie, du rapport au travail et de la consommation à prendre en compte", 34),
            ("TERMINALE_STMG", "MSGNT_ORGA_CIVIQUE",          "Management : organisations et société", "Identifier les pratiques d'organisation civique : mécénat, démocratie participative", 35),
            ("TERMINALE_STMG", "MSGNT_DONNEES_RGPD",          "Gestion : organisations et société", "Expliquer les obligations d'utilisation et de protection des données personnelles et stratégiques (RGPD)", 36),
            ("TERMINALE_STMG", "MSGNT_ALGORITHMES_BLOCKCHAIN", "Gestion : organisations et société", "Expliquer la transparence des algorithmes et l'apport des chaînes de blocs à la sécurisation des échanges", 37),
            ("TERMINALE_STMG", "MSGNT_ECOSYSTEME",            "Management : organisations et société", "Distinguer les relations entre une organisation et son écosystème : implantation, cluster, territoire", 38),
        };
        
        // ------------------------------------------------------------------------------------
        // 6a. Terminale STMG — Enseignement spécifique GESTION ET FINANCE (3 thèmes, 10 questions)
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] GFT =
        {
            // Thème 1 : Appliquer les règles comptables
            ("TERMINALE_STMG", "GFT_PROCESSUS_SIC",         "Appliquer les règles comptables", "Représenter les principaux processus du système d'information comptable", 1),
            ("TERMINALE_STMG", "GFT_DOCUMENTS_PGI",         "Appliquer les règles comptables", "Repérer les documents commerciaux, financiers et comptables disponibles dans le système d'information (PGI)", 2),
            ("TERMINALE_STMG", "GFT_SECURITE_SI",           "Appliquer les règles comptables", "Expliquer les enjeux de la sécurité des traitements et les techniques de sauvegarde", 3),
            ("TERMINALE_STMG", "GFT_PARTIE_DOUBLE",         "Appliquer les règles comptables", "Appliquer la partie double : actif/passif, charge/produit, créance/dette, plan de comptes", 4),
            ("TERMINALE_STMG", "GFT_POSTES_BILAN_CR",       "Appliquer les règles comptables", "Expliciter les postes figurant au bilan et au compte de résultat", 5),
            ("TERMINALE_STMG", "GFT_ACHAT_VENTE",           "Appliquer les règles comptables", "Comptabiliser et analyser les opérations simples du processus achat-vente et leur règlement", 6),
            ("TERMINALE_STMG", "GFT_TVA",                   "Appliquer les règles comptables", "Déterminer la TVA due à partir du mécanisme de la taxe sur la valeur ajoutée", 7),
            ("TERMINALE_STMG", "GFT_LETTRAGE_RAPPROCHEMENT", "Appliquer les règles comptables", "Contrôler les comptes par lettrage et rapprochement d'états", 8),
            ("TERMINALE_STMG", "GFT_COUT_ACQUISITION",      "Appliquer les règles comptables", "Distinguer actif et charge et décrire les éléments du coût d'acquisition d'une immobilisation corporelle", 9),
            ("TERMINALE_STMG", "GFT_PLAN_AMORTISSEMENT",    "Appliquer les règles comptables", "Élaborer le plan d'amortissement d'une immobilisation selon une approche économique", 10),
            ("TERMINALE_STMG", "GFT_PRINCIPES_COMPTABLES",  "Appliquer les règles comptables", "Expliquer les principes de prudence, d'indépendance des exercices, de continuité et de permanence des méthodes", 11),
            ("TERMINALE_STMG", "GFT_INVENTAIRE",            "Appliquer les règles comptables", "Justifier les opérations d'inventaire : stocks, amortissements, dépréciations, cession d'immobilisation", 12),
        
            // Thème 2 : Analyser la situation de l'entreprise
            ("TERMINALE_STMG", "GFT_PROFITABILITE",         "Analyser la situation de l'entreprise", "Analyser la profitabilité à l'aide de l'EBE, du résultat d'exploitation, du RCAI et de la CAF", 13),
            ("TERMINALE_STMG", "GFT_RENTABILITE_LEVIER",    "Analyser la situation de l'entreprise", "Analyser la rentabilité économique et financière et relier endettement et effet de levier, sans le calculer", 14),
            ("TERMINALE_STMG", "GFT_EVOLUTION_ACTIVITE",    "Analyser la situation de l'entreprise", "Estimer les effets d'une évolution de l'activité sur la performance de l'entreprise", 15),
            ("TERMINALE_STMG", "GFT_BILAN_FONCTIONNEL",     "Analyser la situation de l'entreprise", "Analyser un bilan fonctionnel : FRNG, BFR et trésorerie nette", 16),
            ("TERMINALE_STMG", "GFT_RATIOS",                "Analyser la situation de l'entreprise", "Interpréter les ratios de rotation, d'indépendance financière et de capacité de remboursement", 17),
            ("TERMINALE_STMG", "GFT_STRUCTURE_FINANCIERE",  "Analyser la situation de l'entreprise", "Recommander une amélioration de la structure financière et énoncer les limites de l'analyse fonctionnelle", 18),
        
            // Thème 3 : Accompagner la prise de décision
            ("TERMINALE_STMG", "GFT_ARBITRAGE_FINANCEMENT", "Accompagner la prise de décision", "Recenser et arbitrer entre financements internes et externes selon la situation et la stratégie de l'entreprise", 19),
            ("TERMINALE_STMG", "GFT_REMBOURSEMENT_EMPRUNT", "Accompagner la prise de décision", "Comparer plusieurs modalités de remboursement d'un emprunt par simulation sur tableur", 20),
            ("TERMINALE_STMG", "GFT_BUDGET_TRESORERIE",     "Accompagner la prise de décision", "Caractériser la situation de trésorerie à partir d'un budget et choisir une solution pour l'équilibrer", 21),
            ("TERMINALE_STMG", "GFT_AFFECTATION_RESULTAT",  "Accompagner la prise de décision", "Présenter les possibilités d'affectation du résultat : réserves, distribution de bénéfices, report à nouveau", 22),
            ("TERMINALE_STMG", "GFT_COUTS_COMPLETS_PARTIELS", "Accompagner la prise de décision", "Calculer des coûts complets et partiels à partir des charges directes, indirectes, variables et fixes", 23),
            ("TERMINALE_STMG", "GFT_COUT_SPECIFIQUE",       "Accompagner la prise de décision", "Calculer une marge sur coûts spécifiques et apprécier la pertinence de la méthode de calcul de coûts", 24),
            ("TERMINALE_STMG", "GFT_COMMANDE_SUPPLEMENT",   "Accompagner la prise de décision", "Mesurer l'impact d'une commande supplémentaire sur le résultat à l'aide du coût marginal", 25),
            ("TERMINALE_STMG", "GFT_OFFRE_COUTS",           "Accompagner la prise de décision", "Mesurer l'impact de la modification de l'offre commerciale sur les coûts", 26),
        };
        
        // ------------------------------------------------------------------------------------
        // 6b. Terminale STMG — Enseignement spécifique MERCATIQUE (MARKETING) (3 thèmes, 8 questions)
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] MERT =
        {
            // Thème 1 : La définition de l'offre
            ("TERMINALE_STMG", "MERT_COUPLE_PRODUIT_MARCHE", "La définition de l'offre", "Analyser les choix d'offre à partir du couple produit/marché : segmentation, ciblage, positionnement", 1),
            ("TERMINALE_STMG", "MERT_TYPES_MARKETING",      "La définition de l'offre", "Distinguer marketing de masse, différencié, concentré et individualisé (one-to-one)", 2),
            ("TERMINALE_STMG", "MERT_COMPOSANTES_OFFRE",    "La définition de l'offre", "Caractériser les composantes d'une offre : conditionnement, design, qualité, offre globale, gamme", 3),
            ("TERMINALE_STMG", "MERT_POLITIQUE_MARQUE",     "La définition de l'offre", "Mesurer l'importance de la politique de marque", 4),
            ("TERMINALE_STMG", "MERT_B2B_B2C",              "La définition de l'offre", "Distinguer une offre Business to Business (B2B) d'une offre Business to Consumer (B2C)", 5),
            ("TERMINALE_STMG", "MERT_EXPERIENCE_CONSO",     "La définition de l'offre", "Montrer le lien entre acte et expérience de consommation : marketing expérientiel, valeur perçue, satisfaction", 6),
            ("TERMINALE_STMG", "MERT_POLITIQUE_PRIX",       "La définition de l'offre", "Caractériser une politique tarifaire : prix unique, prix différencié, yield management, prix forfaitaire", 7),
            ("TERMINALE_STMG", "MERT_GRATUITE",             "La définition de l'offre", "Analyser les modèles de gratuité : marché tripartite, subventions croisées, freemium, don, vente de données", 8),
            ("TERMINALE_STMG", "MERT_FIXATION_PRIX",        "La définition de l'offre", "Fixer un prix à partir de la sensibilité-prix, des coûts, du taux de marge et du prix cible", 9),
        
            // Thème 2 : La distribution de l'offre
            ("TERMINALE_STMG", "MERT_STRATEGIE_DISTRIB",    "La distribution de l'offre", "Qualifier une stratégie de distribution : directe, indirecte, intensive, sélective, exclusive", 10),
            ("TERMINALE_STMG", "MERT_INTERMEDIATION",       "La distribution de l'offre", "Analyser intermédiation, désintermédiation et réintermédiation", 11),
            ("TERMINALE_STMG", "MERT_PRODUCTEUR_DISTRIB",   "La distribution de l'offre", "Identifier les relations de coopération ou de conflit entre producteur et distributeur", 12),
            ("TERMINALE_STMG", "MERT_OMNICANALITE",         "La distribution de l'offre", "Caractériser les réseaux physiques et virtuels et leur complémentarité : ROPO, omnicanalité, cross-canal", 13),
            ("TERMINALE_STMG", "MERT_DISTRIB_NUMERIQUE",    "La distribution de l'offre", "Analyser la numérisation de la distribution : unités commerciales digitalisées, place de marché, e-commerce, m-commerce", 14),
        
            // Thème 3 : La communication de l'offre
            ("TERMINALE_STMG", "MERT_OBJECTIFS_CIBLE",      "La communication de l'offre", "Identifier l'objet, la cible et les objectifs d'une action de communication", 15),
            ("TERMINALE_STMG", "MERT_MOYENS_COMM",          "La communication de l'offre", "Analyser la pertinence des moyens de communication et des types d'exposition (paid, owned, earned media)", 16),
            ("TERMINALE_STMG", "MERT_MESSAGE_PUBLICITAIRE", "La communication de l'offre", "Analyser la construction d'un message publicitaire et sa cohérence avec la cible (copie stratégie)", 17),
            ("TERMINALE_STMG", "MERT_FIDELISATION",         "La communication de l'offre", "Analyser une politique de fidélisation et ses outils : programmes, valeur vie client, gestion de la relation client", 18),
            ("TERMINALE_STMG", "MERT_COMM_NUMERIQUE",       "La communication de l'offre", "Mesurer l'influence de la communication numérique : marketing d'influence, community management", 19),
            ("TERMINALE_STMG", "MERT_EVALUER_COMM",         "La communication de l'offre", "Évaluer les actions de communication à l'aide d'indicateurs clés de performance (KPI)", 20),
            ("TERMINALE_STMG", "MERT_E_REPUTATION",         "La communication de l'offre", "Analyser les risques de la communication numérique : ad-blocker, buzz, e-réputation, communication de crise", 21),
        };
        
        // ------------------------------------------------------------------------------------
        // 6c. Terminale STMG — Enseignement spécifique RESSOURCES HUMAINES ET COMMUNICATION
        //     (3 thèmes, 9 questions)
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] RHCT =
        {
            // Thème 1 : Les compétences au service de l'organisation
            ("TERMINALE_STMG", "RHCT_BESOINS_COMPETENCES",  "Compétences et organisation", "Identifier comment l'organisation traduit ses besoins opérationnels en compétences (GPEC)", 1),
            ("TERMINALE_STMG", "RHCT_RECRUTEMENT",          "Compétences et organisation", "Vérifier la pertinence d'un recrutement dans le respect des obligations légales : non-discrimination, handicap", 2),
            ("TERMINALE_STMG", "RHCT_ACCUEIL_INTEGRATION",  "Compétences et organisation", "Apprécier les modalités d'accueil, d'intégration et de fidélisation des nouveaux recrutés", 3),
            ("TERMINALE_STMG", "RHCT_FORMES_RELATION",      "Compétences et organisation", "Comparer salariat, intérim, bénévolat et travail indépendant et leurs conséquences en gestion des RH", 4),
            ("TERMINALE_STMG", "RHCT_EVALUATION",           "Compétences et organisation", "Distinguer entretien professionnel, entretien périodique et bilan de compétences et apprécier leur intérêt", 5),
            ("TERMINALE_STMG", "RHCT_FORMATION",            "Compétences et organisation", "Apprécier une politique de formation : plan de développement des compétences, compte personnel de formation", 6),
            ("TERMINALE_STMG", "RHCT_MOBILITE",             "Compétences et organisation", "Analyser les dispositifs de mobilité professionnelle et leur effet sur l'employabilité", 7),
        
            // Thème 2 : Facteurs de motivation et de satisfaction
            ("TERMINALE_STMG", "RHCT_QVT_RSE",              "Motivation et satisfaction", "Caractériser la qualité de vie au travail dans le cadre de la responsabilité sociétale de l'organisation", 8),
            ("TERMINALE_STMG", "RHCT_QVT_MOTIVATION",       "Motivation et satisfaction", "Apprécier la qualité de vie au travail et ses effets sur la motivation des individus", 9),
            ("TERMINALE_STMG", "RHCT_ERGONOMIE",            "Motivation et satisfaction", "Rechercher des mesures ergonomiques simples pour améliorer un poste de travail", 10),
            ("TERMINALE_STMG", "RHCT_SITUATIONS_RISQUE",    "Motivation et satisfaction", "Identifier les situations de travail à risque (RPS, TMS, harcèlement) et analyser les indicateurs sociaux", 11),
            ("TERMINALE_STMG", "RHCT_PREVENTION",           "Motivation et satisfaction", "Proposer des actions de prévention répondant aux obligations de l'employeur en santé et sécurité", 12),
            ("TERMINALE_STMG", "RHCT_SECURITE_SOCIALE",     "Motivation et satisfaction", "Décrire la prise en charge des accidents du travail par la sécurité sociale et le rôle de la complémentaire santé", 13),
            ("TERMINALE_STMG", "RHCT_FORMES_REMUNERATION",  "Motivation et satisfaction", "Repérer les formes de rémunération individualisée et collective : primes, intéressement, participation, épargne salariale", 14),
            ("TERMINALE_STMG", "RHCT_EFFETS_REMUNERATION",  "Motivation et satisfaction", "Identifier les effets de la rémunération sur la satisfaction, l'attractivité et la performance", 15),
        
            // Thème 3 : La recherche de cohésion
            ("TERMINALE_STMG", "RHCT_RELATIONS_PRO",        "Cohésion et climat social", "Caractériser les relations professionnelles et les évolutions du climat social", 16),
            ("TERMINALE_STMG", "RHCT_CONFLIT",              "Cohésion et climat social", "Analyser un conflit, ses conséquences et ses modalités de résolution", 17),
            ("TERMINALE_STMG", "RHCT_DIALOGUE_SOCIAL",      "Cohésion et climat social", "Identifier le rôle, les acteurs et les formes du dialogue social", 18),
            ("TERMINALE_STMG", "RHCT_NEGOCIATION",          "Cohésion et climat social", "Distinguer les domaines de la négociation collective et apprécier leurs enjeux RH", 19),
            ("TERMINALE_STMG", "RHCT_BILAN_SOCIAL",         "Cohésion et climat social", "Évaluer les indicateurs du bilan social et interpréter la base de données économiques et sociales", 20),
            ("TERMINALE_STMG", "RHCT_OUTILS_COMM_RH",       "Cohésion et climat social", "Identifier les outils de communication RH : communication interne et externe, marque employeur, intranet, RSE", 21),
        };
        
        // ------------------------------------------------------------------------------------
        // 6d. Terminale STMG — Enseignement spécifique SYSTÈMES D'INFORMATION DE GESTION
        //     (4 thèmes, 8 questions)
        // ------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SIGT =
        {
            // Thème 1 : Organisation et numérisation
            ("TERMINALE_STMG", "SIGT_CARACTERISER_SI",      "Organisation et numérisation", "Caractériser le système d'information d'une organisation : dimensions, ressources, objectifs, niveaux", 1),
            ("TERMINALE_STMG", "SIGT_APPORTS_NUMERIQUE",    "Organisation et numérisation", "Identifier les apports du numérique au SI et ses effets sur les métiers, le travail et la coordination", 2),
            ("TERMINALE_STMG", "SIGT_QUALITE_DONNEE",       "Organisation et numérisation", "Énoncer les critères de qualité d'une donnée et de son traitement", 3),
            ("TERMINALE_STMG", "SIGT_SYSTEME_INFORMATIQUE", "Organisation et numérisation", "Articuler les éléments d'un système informatique : matériel, logiciel, infrastructure de communication", 4),
            ("TERMINALE_STMG", "SIGT_MODELISER_PROCESSUS",  "Organisation et numérisation", "Représenter une organisation en processus : acteurs, activités, tâches, synchronisation", 5),
            ("TERMINALE_STMG", "SIGT_ADAPTER_PROCESSUS",    "Organisation et numérisation", "Interpréter un schéma de processus et l'adapter à de nouveaux besoins de gestion", 6),
            ("TERMINALE_STMG", "SIGT_RISQUES_NUMERIQUES",   "Organisation et numérisation", "Repérer les risques liés aux technologies numériques et proposer des mesures pour les atténuer", 7),
            ("TERMINALE_STMG", "SIGT_RGPD",                 "Organisation et numérisation", "Identifier les obligations de protection des données personnelles (RGPD) et proposer des solutions techniques", 8),
            ("TERMINALE_STMG", "SIGT_CYBERSECURITE",        "Organisation et numérisation", "Repérer les principales menaces du cyberespace et les solutions adaptées pour se protéger", 9),
        
            // Thème 2 : Management du système d'information et performance
            ("TERMINALE_STMG", "SIGT_METIERS_SI",           "Management du SI et performance", "Identifier les activités de gestion du SI et la diversité des métiers et compétences associés", 10),
            ("TERMINALE_STMG", "SIGT_EXTERNALISATION",      "Management du SI et performance", "Distinguer prestations internalisées et externalisées (infogérance) et les conséquences de ces choix", 11),
            ("TERMINALE_STMG", "SIGT_INDICATEUR_PERF",      "Management du SI et performance", "Construire, calculer et interpréter un indicateur de performance dans un tableau de bord opérationnel", 12),
            ("TERMINALE_STMG", "SIGT_SOLUTIONS_ALIGNEMENT", "Management du SI et performance", "Repérer des solutions technologiques adaptées aux métiers et apprécier leur adéquation avec la stratégie", 13),
            ("TERMINALE_STMG", "SIGT_PROJET_RESSOURCES",    "Management du SI et performance", "Quantifier les ressources d'un projet de SI, choisir un mode de gestion de projet et mesurer les écarts", 14),
            ("TERMINALE_STMG", "SIGT_PLANIFICATION_PROJET", "Management du SI et performance", "Représenter la planification d'un projet et repérer le niveau de criticité d'une tâche", 15),
        
            // Thème 3 : Information, action et décision
            ("TERMINALE_STMG", "SIGT_REQUETES_SQL",         "Information, action et décision", "Manipuler les données d'une base à l'aide de requêtes SQL pour répondre à un besoin de gestion", 16),
            ("TERMINALE_STMG", "SIGT_MODELE_RELATIONNEL",   "Information, action et décision", "Interpréter et adapter un schéma relationnel : clés, dépendances fonctionnelles, contraintes d'intégrité", 17),
            ("TERMINALE_STMG", "SIGT_SOURCES_STOCKAGE",     "Information, action et décision", "Percevoir la diversité des sources de données et l'utilité des nouvelles bases de stockage (lacs de données)", 18),
            ("TERMINALE_STMG", "SIGT_DONNEES_OUVERTES",     "Information, action et décision", "Utiliser des données ouvertes pour produire une nouvelle information en réponse à un besoin", 19),
            ("TERMINALE_STMG", "SIGT_TRACES_NUMERIQUES",    "Information, action et décision", "Repérer les techniques de recueil de traces et de données et les possibilités de leur utilisation", 20),
            ("TERMINALE_STMG", "SIGT_LIRE_PROGRAMME",       "Information, action et décision", "Expliciter le comportement d'un programme à la lecture de son code source et décrire sa logique", 21),
            ("TERMINALE_STMG", "SIGT_REGLES_GESTION",       "Information, action et décision", "Déduire de règles de gestion les résultats, opérations et données, et contrôler la vraisemblance des résultats", 22),
            ("TERMINALE_STMG", "SIGT_ADAPTER_PROGRAMME",    "Information, action et décision", "Adapter une solution à un nouveau besoin de gestion et utiliser une bibliothèque externe dans un programme", 23),
            ("TERMINALE_STMG", "SIGT_IA_TRAITEMENT",        "Information, action et décision", "Apprécier les apports de l'intelligence artificielle dans le traitement des données", 24),
        
            // Thème 4 : Système d'information et échange
            ("TERMINALE_STMG", "SIGT_DOCUMENT_NUMERIQUE",   "Système d'information et échange", "Distinguer numérisation, structuration et indexation d'un contenu numérique et leurs contraintes", 25),
            ("TERMINALE_STMG", "SIGT_LANGAGE_BALISAGE",     "Système d'information et échange", "Adapter un fichier structuré avec un langage de balisage", 26),
            ("TERMINALE_STMG", "SIGT_ARCHITECTURE_RESEAU",  "Système d'information et échange", "Repérer les composants d'un réseau et les principales architectures selon les besoins de l'organisation", 27),
            ("TERMINALE_STMG", "SIGT_PROTOCOLES",           "Système d'information et échange", "Identifier les principaux protocoles mis en jeu dans l'accès à une ressource distante", 28),
            ("TERMINALE_STMG", "SIGT_CONFIG_HOTE",          "Système d'information et échange", "Repérer les éléments de la configuration d'un hôte du réseau", 29),
            ("TERMINALE_STMG", "SIGT_SOLUTION_COMM",        "Système d'information et échange", "Caractériser une solution de communication numérique en matière de qualité, de sécurité et de performance", 30),
            ("TERMINALE_STMG", "SIGT_CLOUD_STOCKAGE",       "Système d'information et échange", "Choisir une solution de stockage et d'accès à distance et distinguer ses bénéfices et ses risques", 31),
        };
    }
}
