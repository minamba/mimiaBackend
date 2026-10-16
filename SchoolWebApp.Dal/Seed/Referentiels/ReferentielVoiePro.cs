namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Les deux enseignements généraux PROPRES à la voie professionnelle : la
    /// prévention-santé-environnement (Hugo) et l'économie — économie-droit ou
    /// économie-gestion selon la spécialité (Karim). Provenance en tête.
    /// </summary>
    public static class ReferentielVoiePro
    {
        // =====================================================================================
        // VOIE PROFESSIONNELLE, ÉTAPE 3 — 16/10/2026. Compétences rédigées depuis les annexes
        // officielles téléchargées et lues le 16/10/2026, rien de mémoire. Sources et extraits
        // gardés dans le bloc-notes de la session (pro-pse-eco\).
        //
        // PSE — UN PROGRAMME PAR CLASSE, trois thématiques (A capital santé, B environnement,
        //   C prévention en milieu professionnel), modules numérotés en continu d'une classe à
        //   l'autre (A1… A9, B1… B5, C1… C12).
        //   2de pro      arrêté du 03/04/2019, NOR MENE1908632A, BO spécial n° 5 du 11/04/2019
        //                https://cache.media.education.gouv.fr/file/SP5-MEN-11-4-2019/98/2/spe632_annexe_1103982.pdf
        //   1re, Tle pro arrêté du 03/02/2020, NOR MENE2003005A, BO spécial n° 1 du 06/02/2020,
        //                annexe 1 (1re) et annexe 2 (Tle)
        //                https://cache.media.education.gouv.fr/file/SPE1-MENJ-6-2-2020/66/5/spe005_annexe1_1239665.pdf
        //                https://cache.media.education.gouv.fr/file/SPE1-MENJ-6-2-2020/66/7/spe005_annexe2_1239667.pdf
        //
        // ÉCONOMIE-DROIT — spécialités tertiaires. Arrêté du 03/04/2019, NOR MENE1908617A,
        //   BO spécial n° 5 du 11/04/2019, jamais modifié. Cinq modules.
        //   https://cache.media.education.gouv.fr/file/SP5-MEN-11-4-2019/76/9/spe617_annexe_1104769.pdf
        //
        // ÉCONOMIE-GESTION — spécialités de production. Arrêté du 03/04/2019, NOR MENE1908618A,
        //   BO spécial n° 5 du 11/04/2019. Quatre modules.
        //   https://cache.media.education.gouv.fr/file/SP5-MEN-11-4-2019/85/6/spe618_annexe_1104856.pdf
        //
        // Tous en vigueur à la rentrée 2026 : l'arrêté du 16/02/2026 (MENE2602913A) ne fait que
        // les étendre au brevet national des métiers d'art, sans toucher au contenu.
        //
        // ⚠️ L'ÉCONOMIE N'EST PAS RÉPARTIE PAR CLASSE PAR LE TEXTE : un seul programme pour les
        // trois ans. Seul l'ordre est imposé en économie-droit (module 1 d'abord, module 5 en
        // dernier ; l'économie-gestion laisse l'ordre libre, le module 1 « pose les cadres »).
        // La répartition ci-dessous est LA NÔTRE, faute de mieux — la base range chaque notion
        // dans une classe. Le professeur sait que l'épreuve de terminale porte sur les trois
        // ans (`PromptsVoiePro`).
        //
        // ÉPREUVES (arrêté du 17/06/2020, NOR MENE2015195A, annexes VI à VIII) : chacune un
        // écrit ponctuel de 2 h, coefficient 1, en fin de terminale.
        // =====================================================================================

        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Pse =
        {
            // ---- SECONDE PRO : A1 à A5, B1, B2, C1, C2 ----
            ("SECONDE_PRO", "PSE_2PRO_A1_FACTEURS",      "A1 · Le système de santé", "Repérer les facteurs internes et externes qui agissent sur la santé d'un individu", 1),
            ("SECONDE_PRO", "PSE_2PRO_A1_SECU_SOCIALE",  "A1 · Le système de santé", "Expliquer la solidarité nationale de la Sécurité sociale et les enjeux d'un plan santé", 2),
            ("SECONDE_PRO", "PSE_2PRO_A1_PARCOURS",      "A1 · Le système de santé", "Présenter le parcours de soins coordonnés : médecin traitant, carte Vitale, dossier médical partagé", 3),
            ("SECONDE_PRO", "PSE_2PRO_A2_SOMMEIL",       "A2 · Les rythmes biologiques, le sommeil", "Présenter les phases du sommeil, leurs rôles, et ce qui influence les rythmes biologiques", 4),
            ("SECONDE_PRO", "PSE_2PRO_A2_DESYNCHRO",     "A2 · Les rythmes biologiques, le sommeil", "Expliquer les effets d'une désynchronisation des rythmes et la réglementation du travail de nuit", 5),
            ("SECONDE_PRO", "PSE_2PRO_A2_HYGIENE",       "A2 · Les rythmes biologiques, le sommeil", "Proposer des mesures pour améliorer la qualité de son sommeil", 6),
            ("SECONDE_PRO", "PSE_2PRO_A3_MUSCLE",        "A3 · L'activité physique", "Expliquer le mouvement volontaire et la contraction du muscle strié squelettique", 7),
            ("SECONDE_PRO", "PSE_2PRO_A3_EFFORT",        "A3 · L'activité physique", "Repérer les modifications du corps pendant l'effort et l'intérêt d'une activité physique régulière", 8),
            ("SECONDE_PRO", "PSE_2PRO_A4_MECANISME",     "A4 · Les addictions", "Expliquer le mécanisme de l'addiction sur le cerveau : synapse, neurotransmetteur, circuit de la récompense", 9),
            ("SECONDE_PRO", "PSE_2PRO_A4_EFFETS",        "A4 · Les addictions", "Identifier les effets de l'alcool, du cannabis et de la polyconsommation, et les conséquences des addictions", 10),
            ("SECONDE_PRO", "PSE_2PRO_A4_PREVENTION",    "A4 · Les addictions", "Proposer des actions de prévention et repérer la réglementation des substances licites et illicites", 11),
            ("SECONDE_PRO", "PSE_2PRO_A5_REPRODUCTION",  "A5 · La sexualité, la contraception", "Expliquer les appareils reproducteurs et les étapes de la reproduction, des gamètes à la nidation", 12),
            ("SECONDE_PRO", "PSE_2PRO_A5_CONTRACEPTION", "A5 · La sexualité, la contraception", "Argumenter le choix d'une contraception et distinguer contraception d'urgence et IVG", 13),
            ("SECONDE_PRO", "PSE_2PRO_A5_STRUCTURES",    "A5 · La sexualité, la contraception", "Identifier les missions des structures d'accueil, d'aide et de soutien", 14),
            ("SECONDE_PRO", "PSE_2PRO_B1_ECORESPONSABLE","B1 · L'alimentation écoresponsable", "Identifier les critères d'une consommation alimentaire écoresponsable : circuit court, saison, labels", 15),
            ("SECONDE_PRO", "PSE_2PRO_B1_GASPILLAGE",    "B1 · L'alimentation écoresponsable", "Analyser les causes et les conséquences du gaspillage alimentaire et proposer des mesures pour le limiter", 16),
            ("SECONDE_PRO", "PSE_2PRO_B2_RISQUE_MAJEUR", "B2 · Les risques majeurs", "Identifier un risque majeur, naturel ou technologique, et repérer ceux de sa commune (DICRIM)", 17),
            ("SECONDE_PRO", "PSE_2PRO_B2_ALERTE",        "B2 · Les risques majeurs", "Connaître la conduite à tenir en cas d'alerte à la population", 18),
            ("SECONDE_PRO", "PSE_2PRO_C1_ENJEUX",        "C1 · Les enjeux de la santé et sécurité au travail", "Identifier les enjeux humains et économiques de la santé et de la sécurité au travail", 19),
            ("SECONDE_PRO", "PSE_2PRO_C1_AT_MP",         "C1 · Les enjeux de la santé et sécurité au travail", "Différencier accident du travail et maladie professionnelle", 20),
            ("SECONDE_PRO", "PSE_2PRO_C1_OBLIGATIONS",   "C1 · Les enjeux de la santé et sécurité au travail", "Repérer les obligations de l'employeur et du salarié, et les règles propres aux jeunes travailleurs", 21),
            ("SECONDE_PRO", "PSE_2PRO_C2_CONSIGNES",     "C2 · Les bases de la prévention des risques professionnels", "Expliquer les consignes de sécurité d'un poste de travail en atelier ou en plateau technique", 22),
            ("SECONDE_PRO", "PSE_2PRO_C2_DANGER",        "C2 · Les bases de la prévention des risques professionnels", "Repérer les dangers d'une activité de travail et les relier aux dommages possibles", 23),
            ("SECONDE_PRO", "PSE_2PRO_C2_RISQUE",        "C2 · Les bases de la prévention des risques professionnels", "Identifier un risque professionnel et sa famille de risque en analysant une activité de travail", 24),

            // ---- PREMIÈRE PRO : A6 à A8, B3, B4, C3 à C6 ----
            ("PREMIERE_PRO", "PSE_1PRO_A6_IST",          "A6 · Les infections sexuellement transmissibles", "Dégager les caractéristiques des principales IST, dont le sida, et leur fréquence en France", 1),
            ("PREMIERE_PRO", "PSE_1PRO_A6_PREVENTION",   "A6 · Les infections sexuellement transmissibles", "Expliquer comment prévenir la transmission des IST : préservatif, vaccin, dépistage", 2),
            ("PREMIERE_PRO", "PSE_1PRO_A6_CONDUITE",     "A6 · Les infections sexuellement transmissibles", "Savoir quoi faire après un comportement à risque et vers quelles structures se tourner", 3),
            ("PREMIERE_PRO", "PSE_1PRO_A7_DIGESTION",    "A7 · Les pratiques alimentaires", "Expliquer le devenir des aliments, de la digestion au passage des nutriments dans le sang", 4),
            ("PREMIERE_PRO", "PSE_1PRO_A7_PRATIQUES",    "A7 · Les pratiques alimentaires", "Analyser les enjeux d'une pratique alimentaire pour la santé et pour l'environnement", 5),
            ("PREMIERE_PRO", "PSE_1PRO_A7_ETIQUETTE",    "A7 · Les pratiques alimentaires", "Choisir un produit en lisant son étiquette : valeur énergétique, additifs, allergènes, provenance", 6),
            ("PREMIERE_PRO", "PSE_1PRO_A8_MECANISME",    "A8 · Le stress au quotidien", "Expliquer le mécanisme du stress par l'adrénaline et le cortisol, et ses facteurs au quotidien", 7),
            ("PREMIERE_PRO", "PSE_1PRO_A8_GERER",        "A8 · Le stress au quotidien", "Différencier stress aigu et stress chronique, et proposer des actions pour gérer son stress", 8),
            ("PREMIERE_PRO", "PSE_1PRO_B3_SON",          "B3 · Le bruit au quotidien", "Indiquer les caractéristiques du son et ce qui le rend dangereux : intensité, fréquence, durée", 9),
            ("PREMIERE_PRO", "PSE_1PRO_B3_AUDITION",     "B3 · Le bruit au quotidien", "Décrire la perception auditive et les conséquences du bruit sur la santé et la vie sociale", 10),
            ("PREMIERE_PRO", "PSE_1PRO_B3_PROTECTION",   "B3 · Le bruit au quotidien", "Proposer des mesures de prévention et de protection, collectives et individuelles, contre le bruit", 11),
            ("PREMIERE_PRO", "PSE_1PRO_B4_RESSOURCE",    "B4 · L'eau et le développement durable", "Analyser les ressources en eau et expliquer pourquoi on la traite et on l'assainit", 12),
            ("PREMIERE_PRO", "PSE_1PRO_B4_CONSOMMATION", "B4 · L'eau et le développement durable", "Analyser la consommation d'eau d'un ménage et citer des mesures pour la préserver", 13),
            ("PREMIERE_PRO", "PSE_1PRO_C3_ACTEURS",      "C3 · Les acteurs de prévention", "Identifier les acteurs de prévention dans et hors de l'entreprise : CSE, SST, inspection du travail, CARSAT", 14),
            ("PREMIERE_PRO", "PSE_1PRO_C4_SECOURS",      "C4 · L'assistance et le secours au travail", "Présenter l'organisation des secours en entreprise et rédiger un message d'alerte adapté", 15),
            ("PREMIERE_PRO", "PSE_1PRO_C4_ASSISTANCE",   "C4 · L'assistance et le secours au travail", "Expliquer l'obligation de porter assistance à une personne en danger", 16),
            ("PREMIERE_PRO", "PSE_1PRO_C5_ANALYSE",      "C5 · L'analyse des risques professionnels", "Analyser une activité de travail avec un outil : 5M, QQOQCP, ITAMaMi", 17),
            ("PREMIERE_PRO", "PSE_1PRO_C5_PROCESSUS",    "C5 · L'analyse des risques professionnels", "Représenter l'apparition d'un dommage : danger, situation dangereuse, événement déclencheur", 18),
            ("PREMIERE_PRO", "PSE_1PRO_C5_EVALUER",      "C5 · L'analyse des risques professionnels", "Évaluer un risque selon sa gravité et sa probabilité, et fixer un niveau de priorité", 19),
            ("PREMIERE_PRO", "PSE_1PRO_C5_MESURES",      "C5 · L'analyse des risques professionnels", "Proposer des mesures de prévention, de protection et de formation hiérarchisées", 20),
            ("PREMIERE_PRO", "PSE_1PRO_C6_SPECIFIQUE",   "C6 · Un risque spécifique à son secteur", "Analyser et évaluer un risque propre à son secteur professionnel et ses effets sur la santé", 21),
            ("PREMIERE_PRO", "PSE_1PRO_C6_HIERARCHIE",   "C6 · Un risque spécifique à son secteur", "Proposer des mesures hiérarchisées contre ce risque : supprimer, réduire, protéger, former", 22),

            // ---- TERMINALE PRO : A9, B5, C7 à C12 ----
            ("TERMINALE_PRO", "PSE_TPRO_A9_QUALITE",       "A9 · La sécurité alimentaire", "Expliquer la qualité microbiologique et chimique des aliments et l'intérêt de la chaîne du froid", 1),
            ("TERMINALE_PRO", "PSE_TPRO_A9_DISPOSITIF",    "A9 · La sécurité alimentaire", "Expliquer l'étiquetage des allergènes et le dispositif français de sécurité alimentaire", 2),
            ("TERMINALE_PRO", "PSE_TPRO_B5_SOURCES",       "B5 · L'énergie et le développement durable", "Caractériser les énergies renouvelables et non renouvelables, leurs avantages et leurs limites", 3),
            ("TERMINALE_PRO", "PSE_TPRO_B5_IMPACTS",       "B5 · L'énergie et le développement durable", "Identifier les impacts de la consommation d'énergie sur l'environnement et sur la santé", 4),
            ("TERMINALE_PRO", "PSE_TPRO_B5_MESURES",       "B5 · L'énergie et le développement durable", "Proposer des mesures collectives et des gestes individuels pour réduire sa consommation et ses émissions", 5),
            ("TERMINALE_PRO", "PSE_TPRO_C7_SUIVI",         "C7 · Le suivi de la santé au travail", "Identifier le suivi médical des salariés : visite d'information et de prévention, examen d'aptitude", 6),
            ("TERMINALE_PRO", "PSE_TPRO_C7_IMMUNITE",      "C7 · Le suivi de la santé au travail", "Expliquer les défenses de l'organisme contre les microbes et le principe de la vaccination", 7),
            ("TERMINALE_PRO", "PSE_TPRO_C8_DECLARATION",   "C8 · Accidents du travail et maladies professionnelles", "Identifier les étapes, les délais et les acteurs de la déclaration d'un accident du travail", 8),
            ("TERMINALE_PRO", "PSE_TPRO_C8_RESPONSABILITE","C8 · Accidents du travail et maladies professionnelles", "Différencier responsabilité civile et pénale de l'employeur, et repérer l'indemnisation des victimes", 9),
            ("TERMINALE_PRO", "PSE_TPRO_C9_RPS",           "C9 · Les risques psychosociaux", "Repérer les facteurs de risques psychosociaux et leurs conséquences sur la santé et sur l'entreprise", 10),
            ("TERMINALE_PRO", "PSE_TPRO_C9_PREVENTION",    "C9 · Les risques psychosociaux", "Proposer des mesures de prévention des risques psychosociaux", 11),
            ("TERMINALE_PRO", "PSE_TPRO_C10_TMS",          "C10 · Les risques liés à l'activité physique", "Identifier les atteintes liées à l'activité physique au travail, dont les TMS et celles du dos", 12),
            ("TERMINALE_PRO", "PSE_TPRO_C10_MESURES",      "C10 · Les risques liés à l'activité physique", "Proposer des mesures de prévention hiérarchisées contre les risques liés à l'activité physique", 13),
            ("TERMINALE_PRO", "PSE_TPRO_C11_SITUATION",    "C11 · L'analyse d'une situation de travail", "Analyser une situation de travail : travail prescrit et réel, déterminants, lien entre effet et causes", 14),
            ("TERMINALE_PRO", "PSE_TPRO_C11_MESURE",       "C11 · L'analyse d'une situation de travail", "Proposer une mesure qui supprime ou réduit le risque dans cette situation", 15),
            ("TERMINALE_PRO", "PSE_TPRO_C12_DISCRIMINATION","C12 · L'égalité de traitement au travail", "Repérer les discriminations au travail et l'égalité professionnelle entre femmes et hommes", 16),
            ("TERMINALE_PRO", "PSE_TPRO_C12_RECOURS",      "C12 · L'égalité de traitement au travail", "Identifier qui solliciter en cas de discrimination : CSE, Défenseur des droits, inspection du travail", 17),
        };

        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] EconomieDroit =
        {
            // ---- SECONDE PRO : modules 1 et 2 (répartition de Mimia, voir l'en-tête) ----
            ("SECONDE_PRO", "ECD_2PRO_M1_AGENTS",       "M1 · L'environnement économique et juridique", "Repérer le rôle des agents économiques, leurs échanges et les différents marchés", 1),
            ("SECONDE_PRO", "ECD_2PRO_M1_DROIT",        "M1 · L'environnement économique et juridique", "Identifier les fonctions du droit, l'organisation des juridictions et la hiérarchie des normes", 2),
            ("SECONDE_PRO", "ECD_2PRO_M1_SOURCE",       "M1 · L'environnement économique et juridique", "Identifier la source de droit qui s'applique dans une situation donnée", 3),
            ("SECONDE_PRO", "ECD_2PRO_M1_CONTRAT",      "M1 · L'environnement économique et juridique", "Identifier les parties, l'objet, les droits et les obligations d'un contrat", 4),
            ("SECONDE_PRO", "ECD_2PRO_M1_RESPONSABILITE","M1 · L'environnement économique et juridique", "Reconnaître les conditions de la responsabilité civile contractuelle et les dommages-intérêts", 5),
            ("SECONDE_PRO", "ECD_2PRO_M2_BUDGET",       "M2 · La consommation", "Identifier les postes budgétaires des ménages, leur évolution et les formes d'épargne", 6),
            ("SECONDE_PRO", "ECD_2PRO_M2_MODES",        "M2 · La consommation", "Montrer l'influence du développement durable et du numérique sur les choix de consommation", 7),
            ("SECONDE_PRO", "ECD_2PRO_M2_PROTECTION",   "M2 · La consommation", "Repérer les droits et les obligations du vendeur et du consommateur dans un contrat de vente", 8),
            ("SECONDE_PRO", "ECD_2PRO_M2_DONNEES",      "M2 · La consommation", "Présenter les enjeux de la protection des données personnelles et le rôle de la CNIL", 9),

            // ---- PREMIÈRE PRO : modules 3 et 4 ----
            ("PREMIERE_PRO", "ECD_1PRO_M3_FACTEURS",       "M3 · La production", "Identifier les facteurs de production et expliquer le choix d'une combinaison productive", 1),
            ("PREMIERE_PRO", "ECD_1PRO_M3_EXTERNALISATION","M3 · La production", "Expliquer pourquoi une entreprise externalise une partie de sa production", 2),
            ("PREMIERE_PRO", "ECD_1PRO_M3_RSE",            "M3 · La production", "Identifier les dimensions sociale et environnementale de la performance et les raisons de la RSE", 3),
            ("PREMIERE_PRO", "ECD_1PRO_M3_NUMERIQUE",      "M3 · La production", "Montrer comment le numérique crée de la valeur et change l'organisation de la production", 4),
            ("PREMIERE_PRO", "ECD_1PRO_M4_INDICATEURS",    "M4 · Le rôle de l'État", "Interpréter les indicateurs de l'activité économique : PIB, croissance, IDH, balance commerciale", 5),
            ("PREMIERE_PRO", "ECD_1PRO_M4_COMPARER",       "M4 · Le rôle de l'État", "Commenter les indicateurs de l'économie française face à d'autres pays européens", 6),
            ("PREMIERE_PRO", "ECD_1PRO_M4_ETAT",           "M4 · Le rôle de l'État", "Identifier les fonctions de l'État : allocation, redistribution, régulation", 7),
            ("PREMIERE_PRO", "ECD_1PRO_M4_POLITIQUE",      "M4 · Le rôle de l'État", "Repérer dans le budget de l'État des mesures de politique économique, dont la lutte contre le chômage", 8),

            // ---- TERMINALE PRO : module 5, qui « clôt les apprentissages » ----
            ("TERMINALE_PRO", "ECD_TPRO_M5_NUMERIQUE",    "M5 · Les mutations du travail", "Repérer comment le numérique change le travail : télétravail, travail collaboratif, flexibilité, déconnexion", 1),
            ("TERMINALE_PRO", "ECD_TPRO_M5_CONSEQUENCES", "M5 · Les mutations du travail", "Identifier les conséquences de ces nouvelles modalités du travail", 2),
            ("TERMINALE_PRO", "ECD_TPRO_M5_CONTRAT",      "M5 · Les mutations du travail", "Repérer les caractéristiques du contrat de travail et du contrat de prestation de services", 3),
            ("TERMINALE_PRO", "ECD_TPRO_M5_TYPES",        "M5 · Les mutations du travail", "Distinguer le CDI, le CDD et le contrat de travail temporaire", 4),
            ("TERMINALE_PRO", "ECD_TPRO_M5_STATUTS",      "M5 · Les mutations du travail", "Différencier le statut de salarié et celui de travailleur indépendant", 5),
            ("TERMINALE_PRO", "ECD_TPRO_M5_FORMATION",    "M5 · Les mutations du travail", "Repérer les enjeux de la formation tout au long de la vie et ses voies : CPF, VAE, apprentissage", 6),
            ("TERMINALE_PRO", "ECD_TPRO_EPREUVE",         "L'épreuve écrite", "Exploiter un dossier documentaire, puis rédiger une réponse argumentée à une question d'économie-droit", 7),
        };

        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] EconomieGestion =
        {
            // ---- SECONDE PRO : module 1 et début du module 2 (répartition de Mimia) ----
            ("SECONDE_PRO", "ECG_2PRO_M1_SECTEUR",      "M1 · L'environnement de l'entreprise", "Présenter un secteur d'activité, son poids dans l'économie et les formes d'organisations", 1),
            ("SECONDE_PRO", "ECG_2PRO_M1_AGENTS",       "M1 · L'environnement de l'entreprise", "Identifier les agents économiques et les échanges d'une entreprise avec eux", 2),
            ("SECONDE_PRO", "ECG_2PRO_M1_CONTRAT",      "M1 · L'environnement de l'entreprise", "Identifier les éléments d'un contrat et les droits et obligations des parties", 3),
            ("SECONDE_PRO", "ECG_2PRO_M1_INEXECUTION",  "M1 · L'environnement de l'entreprise", "Caractériser une inexécution du contrat et la responsabilité civile contractuelle", 4),
            ("SECONDE_PRO", "ECG_2PRO_M2_FONCTIONS",    "M2 · Les choix de production", "Identifier les fonctions de l'entreprise, lire un organigramme et s'y situer", 5),
            ("SECONDE_PRO", "ECG_2PRO_M2_OFFRE_DEMANDE","M2 · Les choix de production", "Caractériser l'offre et la demande, le cycle de vie du produit et l'innovation", 6),

            // ---- PREMIÈRE PRO : fin du module 2, module 3 ----
            ("PREMIERE_PRO", "ECG_1PRO_M2_ORGANISATION", "M2 · Les choix de production", "Repérer les facteurs de production et schématiser l'organisation de la production", 1),
            ("PREMIERE_PRO", "ECG_1PRO_M2_CHOIX",        "M2 · Les choix de production", "Expliquer un choix de production : flux tendus ou poussés, externalisation, investissement", 2),
            ("PREMIERE_PRO", "ECG_1PRO_M2_ENVIRONNEMENT","M2 · Les choix de production", "Expliquer une décision prise pour saisir une opportunité ou contrer une menace de l'environnement", 3),
            ("PREMIERE_PRO", "ECG_1PRO_M3_COUTS",        "M3 · De la création de valeur à sa répartition", "Calculer un coût d'achat, de production ou de revient, et la valeur ajoutée", 4),
            ("PREMIERE_PRO", "ECG_1PRO_M3_RESULTAT",     "M3 · De la création de valeur à sa répartition", "Commenter le résultat d'une entreprise et le partage de sa valeur ajoutée", 5),
            ("PREMIERE_PRO", "ECG_1PRO_M3_PRIX",         "M3 · De la création de valeur à sa répartition", "Repérer les composantes d'un prix de vente et expliquer sa fixation : marge, prix psychologique", 6),
            ("PREMIERE_PRO", "ECG_1PRO_M3_RICHESSE",     "M3 · De la création de valeur à sa répartition", "Commenter le PIB et l'IDH, et l'action de l'État dans la répartition de la richesse", 7),
            ("PREMIERE_PRO", "ECG_1PRO_M3_BUDGET",       "M3 · De la création de valeur à sa répartition", "Identifier les postes de consommation d'un ménage, ses formes d'épargne et de financement", 8),

            // ---- TERMINALE PRO : module 4 ----
            ("TERMINALE_PRO", "ECG_TPRO_M4_NUMERIQUE",   "M4 · Les mutations de la relation de travail", "Repérer les effets du numérique sur la relation de travail : plateformes, flexibilité, identité numérique", 1),
            ("TERMINALE_PRO", "ECG_TPRO_M4_STATUTS",     "M4 · Les mutations de la relation de travail", "Distinguer salarié et indépendant, et justifier le recours à un CDI, un CDD ou un contrat d'intérim", 2),
            ("TERMINALE_PRO", "ECG_TPRO_M4_REGLES",      "M4 · Les mutations de la relation de travail", "Repérer les règles du droit du travail : durée du travail, heures supplémentaires, congés, bulletin de paie", 3),
            ("TERMINALE_PRO", "ECG_TPRO_M4_OBLIGATIONS", "M4 · Les mutations de la relation de travail", "Identifier les droits et obligations de l'employeur et du salarié, et les représentants du personnel", 4),
            ("TERMINALE_PRO", "ECG_TPRO_M4_RUPTURE",     "M4 · Les mutations de la relation de travail", "Exposer les modes de rupture d'une relation de travail, salariée ou indépendante", 5),
            ("TERMINALE_PRO", "ECG_TPRO_M4_FORMATION",   "M4 · Les mutations de la relation de travail", "Repérer les possibilités de formation tout au long de la vie : CPF, VAE, apprentissage", 6),
            ("TERMINALE_PRO", "ECG_TPRO_EPREUVE",        "L'épreuve écrite", "Analyser une situation d'entreprise, puis rédiger une réponse argumentée à une question d'économie-gestion", 7),
        };
    }
}
