namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// Les programmes de la voie technologique (tronc commun), tels que les textes officiels les
    /// définissent — provenance détaillée en tête du fichier. Réparti entre les matières par
    /// `ReferentielsSeriesTechnologiques`.
    /// </summary>
    public static class ReferentielTroncCommunTechno
    {
        // =====================================================================================
        // TRONC COMMUN DE LA VOIE TECHNOLOGIQUE (hors mathématiques) — RÉFÉRENTIELS PROPRES
        // Établi le 14/09/2026 d'après les textes téléchargés dans sources\tronc-commun\ .
        // Rien n'est encore dans le dépôt : ces tableaux sont à reporter dans les fichiers
        // Referentiels/*.cs de chaque matière si la décision est prise.
        //
        // RAPPEL DE LA RÈGLE (VoiesScolaires.RetenirPourLaVoie) : dès qu'une compétence existe
        // au niveau PREMIERE_TECHNO (ou TERMINALE_TECHNO) pour une matière, l'élève techno perd
        // TOUTES les compétences de la voie générale de ce rang dans cette matière. Chaque
        // tableau ci-dessous est donc COMPLET pour son année, EMC compris en histoire-géo.
        //
        // ------------------------------------------------------------------------------------
        // PROVENANCE — TEXTE EN VIGUEUR EN 2026-2027, VÉRIFIÉ SUR LE TEXTE LUI-MÊME
        // ------------------------------------------------------------------------------------
        // MATIÈRE / CLASSE        TEXTE                                          EN VIGUEUR     VERDICT
        // Français 1re techno     Arrêté 17-1-2019 MENE1901575A, ANNEXE 2        rentrée 2019   DIFFÉRENT (épreuve
        //                         « Programme de français de première des                       + œuvres), même
        //                         voies générale et technologique » — TEXTE                     texte de programme
        //                         COMMUN aux deux voies (BO spécial n°1 22-1-2019)
        //                         modifié par arrêté 10-4-2020 (JORFTEXT000041825155) :
        //                         « renouvelé par moitié » -> « par quart »
        //                         Programme limitatif 2026-2027 : NDS 4-7-2025
        //                         MENE2518792N (BO n°30 du 24-7-2025), liste
        //                         PROPRE à la voie technologique
        //                         EAF : NDS 23-7-2020 MENE2019312N + version
        //                         consolidée Éduscol mars 2024 (intègre NDS
        //                         26-9-2023 MENE2323453N) — écrit techno :
        //                         commentaire OU contraction + essai
        //                         Coefficients : arrêté 16-7-2018 MENE1813140A,
        //                         version Légifrance en vigueur au 01/09/2025
        // Histoire-géo 1re techno Arrêté 17-1-2019 MENE1901577A, ANNEXE 3        rentrée 2019   DIFFÉRENT
        //                         « Programme d'histoire-géographie de première
        //                         technologique » (24 h hist. + 24 h géo)
        // Histoire-géo Tle techno Arrêté 19-7-2019 MENE1921243A, ANNEXE 2        rentrée 2020   DIFFÉRENT
        //                         (BO spécial n°8 25-7-2019) ; Légifrance
        //                         JORFTEXT000038799918 : « version initiale »,
        //                         aucune modification
        // EMC 1re (toutes voies)  Arrêté 29-5-2024 MENE2413934A (BO n°24          rentrée 2025   IDENTIQUE au général
        // EMC Tle (toutes voies)  13-6-2024), annexe CP -> Tle ; abroge          rentrée 2026   (« notions et contenus
        //                         MENE1901572A et l'arrêté EMC Tle du 19-7-2019                 identiques pour toutes
        //                                                                                        les voies ») — RECOPIÉ
        //                                                                                        ici à cause de la règle
        // Philosophie Tle techno  Arrêté 19-7-2019 MENE1921238A, ANNEXE 2        rentrée 2020   DIFFÉRENT (7 notions
        //                         « Programme de philosophie de terminale                       au lieu de 17, étude
        //                         technologique » ; Légifrance                                  suivie d'une œuvre
        //                         JORFTEXT000038799903 : version initiale                       non obligatoire)
        //                         Épreuve : NDS 11-2-2020 MENE2001090N (BO
        //                         spécial n°2 13-2-2020), consolidée mars 2024
        // LV A/B cycle terminal   Arrêté 5-5-2025 MENE2504621A (BO n°22           rentrée 2026   IDENTIQUE (programme
        //                         29-5-2025) — un seul programme « lycée                        commun GT ; seule
        //                         général et technologique » par langue ;                       différence : au moins
        //                         abroge pour les LVE l'arrêté 17-1-2019                        3 axes/an en techno au
        //                         (MENE1901585A)                                                lieu de 5) — RIEN PRODUIT
        // ETLV                    Arrêté horaires 16-7-2018 MENE1815612A art. 8 ;                — pas de programme propre,
        //                         NDS 29-7-2021 MENE2121395N ; NDS                               RIEN PRODUIT
        //                         MENE2618195N (BO n°30 2026), partie C
        //
        // CONVENTIONS : Domaine = celui des fichiers existants (Littérature, Méthode, Écriture,
        // Étude de la langue, Oral / Histoire, Géographie, EMC, Méthode). Ordre = ordre du texte
        // officiel (l'annexe HG ouvre sur les capacités et méthodes, l'EMC est un texte distinct
        // placé après). Sujets d'étude « au choix » NON transformés en compétences : un élève
        // n'en étudie qu'un sur deux, l'autre serait une lacune imaginaire.
        // =====================================================================================
        
        
        // -------------------------------------------------------------------------------------
        // FRANÇAIS — PREMIERE_TECHNO — 25 compétences
        // Même texte de programme que la voie générale, mais le référentiel général contient
        // « Rédiger une dissertation sur œuvre et parcours » (épreuve absente en techno) et ne
        // contient ni la contraction ni l'essai, qui sont la moitié de l'écrit techno.
        // -------------------------------------------------------------------------------------
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] FrancaisPremiereTechno =>
            new[]
            {
                // --- L'étude de la langue au lycée (partie commune 2de-1re) ---
                ("PREMIERE_TECHNO", "FR_1TEC_LANG_CIRCONSTANCIELLES", "Étude de la langue", "Analyser les subordonnées circonstancielles de cause, conséquence, but, condition et concession", 1),
                ("PREMIERE_TECHNO", "FR_1TEC_LANG_INTERROGATION",     "Étude de la langue", "Distinguer interrogation directe et interrogative indirecte : syntaxe, sémantique et pragmatique", 2),
                ("PREMIERE_TECHNO", "FR_1TEC_LANG_NEGATION",          "Étude de la langue", "Analyser l'expression de la négation : phrase négative, préfixation et antonymie", 3),
                ("PREMIERE_TECHNO", "FR_1TEC_LANG_LEXIQUE",           "Étude de la langue", "Enrichir son lexique en observant néologie et relations lexicales dans les textes", 4),
                ("PREMIERE_TECHNO", "FR_1TEC_LANG_ACQUIS_SECONDE",    "Étude de la langue", "Mobiliser les notions de seconde : accords, valeurs du verbe, phrase complexe, relatives", 5),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_RELATIONS_LOGIQUES", "Écriture", "Exprimer la condition, la cause, la conséquence, le but, la comparaison, l'opposition et la concession", 6),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_REFORMULER",         "Écriture", "Reformuler et synthétiser un propos, discuter et nuancer une opinion", 7),
        
                // --- Littérature : les quatre objets d'étude, programme limitatif propre à la voie techno ---
                ("PREMIERE_TECHNO", "FR_1TEC_LECT_OEUVRES",  "Littérature", "Connaître les quatre œuvres et parcours étudiés, choisis dans le programme de la voie technologique", 8),
                ("PREMIERE_TECHNO", "FR_1TEC_LECT_POESIE",   "Littérature", "Analyser l'œuvre poétique du XIXe au XXIe siècle au programme et son parcours associé", 9),
                ("PREMIERE_TECHNO", "FR_1TEC_LECT_IDEES",    "Littérature", "Analyser l'œuvre de littérature d'idées du XVIe au XVIIIe siècle au programme et son parcours", 10),
                ("PREMIERE_TECHNO", "FR_1TEC_LECT_ROMAN",    "Littérature", "Analyser l'œuvre du roman et du récit du Moyen Âge au XXIe siècle au programme et son parcours", 11),
                ("PREMIERE_TECHNO", "FR_1TEC_LECT_THEATRE",  "Littérature", "Analyser l'œuvre théâtrale du XVIIe au XXIe siècle au programme et son parcours associé", 12),
                ("PREMIERE_TECHNO", "FR_1TEC_LECT_CONTEXTE", "Littérature", "Situer une œuvre dans son contexte historique, littéraire et artistique", 13),
                ("PREMIERE_TECHNO", "FR_1TEC_LECT_CURSIVE",  "Littérature", "Lire en lecture cursive au moins une œuvre par objet d'étude, distincte des œuvres étudiées", 14),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_APPROPRIATION", "Écriture", "Produire des écrits d'appropriation : invention, intervention, jugement personnel sur une œuvre", 15),
        
                // --- Épreuve écrite anticipée (4 h) : commentaire OU contraction suivie d'un essai ---
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_COMMENTAIRE", "Écriture", "Rédiger le commentaire guidé d'un texte littéraire lié à un objet d'étude autre que la littérature d'idées", 16),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_CONTRACTION", "Écriture", "Contracter au quart un texte d'idées d'environ 750 mots en respectant énonciation, thèse et mouvement", 17),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_COMPTER_MOTS", "Écriture", "Tenir la longueur de la contraction à plus ou moins 10 % et indiquer le nombre de mots", 18),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_ESSAI",       "Écriture", "Rédiger un essai sur la question que le texte partage avec l'œuvre de littérature d'idées étudiée", 19),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_ETAYER_ESSAI", "Écriture", "Appuyer l'essai sur l'œuvre et les textes étudiés, ses lectures et sa culture personnelles", 20),
                ("PREMIERE_TECHNO", "FR_1TEC_ECR_LANGUE",      "Écriture", "Rédiger dans une langue correcte, l'orthographe étant prise en compte dans l'évaluation", 21),
        
                // --- Épreuve orale anticipée (20 min, préparation 30 min) ---
                ("PREMIERE_TECHNO", "FR_1TEC_ORAL_LECTURE",    "Oral", "Lire à voix haute un texte du récapitulatif de façon juste, pertinente et expressive", 22),
                ("PREMIERE_TECHNO", "FR_1TEC_METH_LINEAIRE",   "Méthode", "Mener l'explication linéaire d'un passage d'une vingtaine de lignes", 23),
                ("PREMIERE_TECHNO", "FR_1TEC_METH_GRAMMAIRE",  "Méthode", "Répondre à la question de grammaire : analyse syntaxique d'une courte phrase du texte", 24),
                ("PREMIERE_TECHNO", "FR_1TEC_ORAL_OEUVRE_CHOISIE", "Oral", "Présenter l'œuvre choisie, justifier ce choix et défendre son point de vue face aux relances", 25),
            };
        
        
        // -------------------------------------------------------------------------------------
        // HISTOIRE-GÉOGRAPHIE-EMC — PREMIERE_TECHNO — 33 compétences
        // Méthodes + histoire + géographie : MENE1901577A annexe 3.
        // EMC : MENE2413934A (programme 2024, en application en 1re depuis la rentrée 2025).
        // -------------------------------------------------------------------------------------
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HistoireGeoPremiereTechno =>
            new[]
            {
                // --- Capacités travaillées et méthodes acquises ---
                ("PREMIERE_TECHNO", "HG_1TEC_METH_REPERES",       "Méthode", "Identifier périodes, dates et acteurs clés, et localiser les grands repères géographiques", 1),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_CONTEXTUALISER","Méthode", "Mettre un événement ou une figure en perspective et changer d'échelle en géographie", 2),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_NOTIONS",       "Méthode", "Employer à bon escient les notions et le lexique de l'histoire et de la géographie", 3),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_LIRE_CARTE",    "Méthode", "Lire, comprendre et apprécier une carte, un croquis, un document iconographique, une statistique", 4),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_DEMARCHE",      "Méthode", "S'approprier un questionnement, construire et vérifier des hypothèses, justifier ses choix", 5),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_ANALYSE_DOC",   "Méthode", "Procéder à l'analyse critique d'un document selon une approche historique ou géographique", 6),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_ARGUMENTER",    "Méthode", "Utiliser une approche historique ou géographique pour construire une argumentation", 7),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_CROQUIS",       "Méthode", "Réaliser un croquis de géographie", 8),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_COMPOSITION",   "Méthode", "Rédiger une composition", 9),
                ("PREMIERE_TECHNO", "HG_1TEC_METH_NUMERIQUE",     "Méthode", "Utiliser le numérique pour réaliser des cartes, des graphiques, des présentations", 10),
        
                // --- Histoire : la France de 1789 aux lendemains de la Première Guerre mondiale ---
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_REVOLUTION",    "Histoire", "Expliquer la rupture révolutionnaire : nation de citoyens égaux en droit, chute de la monarchie, République", 11),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_NAPOLEON_VIENNE","Histoire", "Expliquer la domination européenne de Napoléon Bonaparte et le congrès de Vienne", 12),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_1848",          "Histoire", "Expliquer les principes démocratiques de 1848 : suffrage universel masculin, abolition de l'esclavage", 13),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_SECOND_EMPIRE", "Histoire", "Caractériser le Second Empire, régime autoritaire, industrialisation et urbanisation, jusqu'à sa chute en 1870", 14),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_PROJET_REPUBLICAIN","Histoire", "Expliquer le projet républicain avant 1914 : libertés fondamentales, lois scolaires, loi de 1905", 15),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_DREYFUS",       "Histoire", "Analyser l'antisémitisme autour de l'affaire Dreyfus", 16),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_EMPIRE_COLONIAL","Histoire", "Décrire les rivalités coloniales et le fonctionnement des sociétés coloniales sous la IIIe République", 17),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_1GM_CONFLIT",   "Histoire", "Caractériser la Première Guerre mondiale : guerre longue, industrielle, mondialisée, pluralité des fronts", 18),
                ("PREMIERE_TECHNO", "HG_1TEC_HIST_1GM_PAIX",      "Histoire", "Analyser les violences contre les civils, le génocide arménien et les traités qui mettent fin aux empires", 19),
        
                // --- Géographie : les dynamiques d'un monde en recomposition ---
                ("PREMIERE_TECHNO", "HG_1TEC_GEO_METROPOLES",     "Géographie", "Expliquer le poids croissant des métropoles et la diversité de leur rang et de leur influence", 20),
                ("PREMIERE_TECHNO", "HG_1TEC_GEO_ESPACES_PRODUCTIFS","Géographie", "Expliquer la métropolisation et la littoralisation des espaces productifs et l'essor des flux", 21),
                ("PREMIERE_TECHNO", "HG_1TEC_GEO_CHAINE_VALEUR",  "Géographie", "Expliquer l'organisation de la production en chaînes de valeur ajoutée à différentes échelles", 22),
                ("PREMIERE_TECHNO", "HG_1TEC_GEO_RURAUX",         "Géographie", "Analyser la multifonctionnalité des espaces ruraux et leurs liens croissants avec les espaces urbains", 23),
                ("PREMIERE_TECHNO", "HG_1TEC_GEO_CHINE",          "Géographie", "Analyser les recompositions spatiales de la Chine : urbanisation, littoralisation, espaces ruraux", 24),
        
                // --- EMC (programme 2024, identique pour toutes les voies) : cohésion et diversité ---
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_SOLIDARITE",     "EMC", "Expliquer la solidarité et la fraternité comme projet social porté par la devise de la République", 25),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_INEGALITES",     "EMC", "Montrer que les inégalités économiques et sociales peuvent menacer la cohésion sociale et la démocratie", 26),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_EGALITE_FH",     "EMC", "Analyser l'égalité femmes-hommes : un principe qui transforme la société en se heurtant à des résistances", 27),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_DISCRIMINATIONS","EMC", "Définir juridiquement les discriminations et expliquer la conception d'une société inclusive", 28),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_RACISME",        "EMC", "Expliquer que racisme, antisémitisme, antitsiganisme, xénophobie et haine anti-LGBT sont punis par la loi", 29),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_LAICITE",        "EMC", "Expliquer comment la laïcité rend possible la coexistence pacifique d'options philosophiques différentes", 30),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_INDIVISIBILITE", "EMC", "Expliquer l'équilibre entre République indivisible et organisation décentralisée", 31),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_NATION",         "EMC", "Expliquer l'acquisition de la nationalité, la citoyenneté européenne et les enjeux mémoriels de la Nation", 32),
                ("PREMIERE_TECHNO", "HG_1TEC_EMC_DEFENSE",        "EMC", "Expliquer la défense et la sécurité nationales face au terrorisme et à la cybersécurité", 33),
            };
        
        
        // -------------------------------------------------------------------------------------
        // HISTOIRE-GÉOGRAPHIE-EMC — TERMINALE_TECHNO — 39 compétences
        // Méthodes + histoire + géographie : MENE1921243A annexe 2.
        // EMC : MENE2413934A (programme 2024, en application en terminale à la rentrée 2026).
        // -------------------------------------------------------------------------------------
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HistoireGeoTerminaleTechno =>
            new[]
            {
                // --- Capacités travaillées et méthodes acquises (version terminale technologique) ---
                ("TERMINALE_TECHNO", "HG_TTEC_METH_REPERES",       "Méthode", "Identifier périodes, dates et acteurs des grands événements, et localiser les repères du programme", 1),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_CONTEXTUALISER","Méthode", "Mettre un événement en perspective et relier des faits de natures, périodes et lieux différents", 2),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_NOTIONS",       "Méthode", "Employer à bon escient les notions de terminale et réactiver les notions de première", 3),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_TRANSPOSER",    "Méthode", "Transposer un texte en croquis", 4),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_PRODUCTION_GRAPHIQUE","Méthode", "Réaliser des productions graphiques et cartographiques dans le cadre d'une analyse", 5),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_CRITIQUER_CARTE","Méthode", "Lire, comprendre et critiquer une carte, un croquis, un document iconographique, une série statistique", 6),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_DEMARCHE",      "Méthode", "S'approprier un questionnement, construire et vérifier des hypothèses, justifier une interprétation", 7),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_ANALYSE_DOC",   "Méthode", "Procéder à l'analyse critique d'un document selon une approche historique ou géographique", 8),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_COMPOSITION",   "Méthode", "Rédiger une composition en construisant une argumentation historique ou géographique", 9),
                ("TERMINALE_TECHNO", "HG_TTEC_METH_NUMERIQUE",     "Méthode", "Utiliser les outils numériques pour produire des cartes, des graphiques, des présentations", 10),
        
                // --- Histoire : totalitarismes, guerres et démocratie, des années 1920 à nos jours ---
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_TOTALITARISMES","Histoire", "Caractériser les totalitarismes soviétique et nazi : idéologies, violences et résistances", 11),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_2GM",           "Histoire", "Situer les protagonistes et les principaux théâtres d'opération de la Seconde Guerre mondiale", 12),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_GENOCIDES",     "Histoire", "Analyser les crimes de guerre, les crimes de masse et les génocides des Juifs et des Tsiganes", 13),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_FRANCE_1940",   "Histoire", "Distinguer le régime de Vichy, l'occupation, la collaboration et la Résistance", 14),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_ORDRE_1945",    "Histoire", "Expliquer les bases d'un nouvel ordre international : ONU, procès de Nuremberg et de Tokyo", 15),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_GUERRE_FROIDE", "Histoire", "Analyser la guerre froide, ses enjeux et l'effondrement du bloc soviétique (1947-1991)", 16),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_DECOLONISATION","Histoire", "Expliquer le processus de décolonisation et l'émergence du tiers monde", 17),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_APRES_1991",    "Histoire", "Analyser les relations entre les puissances après 1991 et les nouvelles formes de conflits", 18),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_PROJET_EUROPEEN","Histoire", "Retracer le projet européen et les étapes de sa réalisation", 19),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_GPRF",          "Histoire", "Expliquer les réformes politiques et sociales du Gouvernement provisoire de la République française", 20),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_FIN_EMPIRE",    "Histoire", "Expliquer la fin de l'empire colonial français", 21),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_VE_REPUBLIQUE", "Histoire", "Caractériser la Cinquième République et ses principales réformes institutionnelles", 22),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_SOCIETE",       "Histoire", "Analyser les transformations de la société française : démographie, immigration, place des femmes, Code civil", 23),
                ("TERMINALE_TECHNO", "HG_TTEC_HIST_PUISSANCE",     "Histoire", "Analyser l'évolution de la puissance française à l'échelle européenne et mondiale depuis 1945", 24),
        
                // --- Géographie : la mondialisation, une mise en relation inégale des territoires ---
                ("TERMINALE_TECHNO", "HG_TTEC_GEO_MERS",           "Géographie", "Expliquer le rôle des mers et des océans, vecteurs essentiels de la mondialisation", 25),
                ("TERMINALE_TECHNO", "HG_TTEC_GEO_ROUTES_DETROITS","Géographie", "Analyser les routes maritimes, canaux et détroits internationaux et leurs enjeux géostratégiques", 26),
                ("TERMINALE_TECHNO", "HG_TTEC_GEO_INTEGRATION",    "Géographie", "Expliquer l'inégal accès des territoires à la mondialisation, à toutes les échelles", 27),
                ("TERMINALE_TECHNO", "HG_TTEC_GEO_CENTRES_DECISION","Géographie", "Analyser la hiérarchie des centres de décision, métropoles, hubs et plateformes multimodales", 28),
                ("TERMINALE_TECHNO", "HG_TTEC_GEO_INFLUENCE_FRANCE","Géographie", "Identifier les lieux de l'influence française dans la mondialisation : diplomatie, culture, économie", 29),
                ("TERMINALE_TECHNO", "HG_TTEC_GEO_ATTRACTIVITE",   "Géographie", "Expliquer le rayonnement et l'attractivité de la France, renforcés par son appartenance à l'Union européenne", 30),
        
                // --- EMC (programme 2024, identique pour toutes les voies) : la vie démocratique ---
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_DEBAT_PLURALISME","EMC", "Expliquer comment la République organise le débat et garantit le pluralisme politique", 31),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_PARTIS",         "EMC", "Discuter le rôle des partis politiques et de la société civile organisée dans la vie démocratique", 32),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_OPINION",        "EMC", "Interroger la place de l'opinion publique : sondages, médias, pétitions, manifestations", 33),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_PARTICIPATIVE",  "EMC", "Identifier les espaces de la démocratie participative, dont la démocratie scolaire", 34),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_DEBAT_NUMERIQUE","EMC", "Analyser les nouvelles conditions du débat à l'ère numérique et la fiabilité des sources", 35),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_CITOYENNETE",    "EMC", "Présenter les voies d'accès aux responsabilités politiques et les formes d'engagement des jeunes", 36),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_LEGITIMITE",     "EMC", "Expliquer la légitimité des élus et la décision prise par consensus ou à la majorité", 37),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_LOI",            "EMC", "Retracer l'élaboration de la loi, son contrôle constitutionnel et la place des directives européennes", 38),
                ("TERMINALE_TECHNO", "HG_TTEC_EMC_ONU",            "EMC", "Analyser la délibération internationale à l'ONU, entre points de consensus et situations de blocage", 39),
            };
        
        
        // -------------------------------------------------------------------------------------
        // PHILOSOPHIE — TERMINALE_TECHNO — 21 compétences
        // MENE1921238A annexe 2. Sept notions : l'art, la justice, la liberté, la nature, la
        // religion, la technique, la vérité. Le programme interdit toute répartition prédéfinie
        // des notions entre perspectives : le domaine est donc « Notions », pas une perspective.
        // -------------------------------------------------------------------------------------
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PhilosophieTerminaleTechno =>
            new[]
            {
                // --- Perspectives ---
                ("TERMINALE_TECHNO", "PHILO_TTEC_PERSPECTIVES", "Perspectives", "Aborder les notions selon les trois perspectives : existence humaine et culture, morale et politique, connaissance", 1),
        
                // --- Notions (7) ---
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_ART",      "Notions", "Examiner la notion d'art dans ses dimensions essentielles et en relation avec les autres notions", 2),
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_JUSTICE",  "Notions", "Examiner la notion de justice dans ses dimensions essentielles et en relation avec les autres notions", 3),
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_LIBERTE",  "Notions", "Examiner la notion de liberté dans ses dimensions essentielles et en relation avec les autres notions", 4),
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_NATURE",   "Notions", "Examiner la notion de nature dans ses dimensions essentielles et en relation avec les autres notions", 5),
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_RELIGION", "Notions", "Examiner la notion de religion dans ses dimensions essentielles et en relation avec les autres notions", 6),
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_TECHNIQUE","Notions", "Examiner la technique : réflexion morale, type de connaissance procuré, place dans l'existence humaine", 7),
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_VERITE",   "Notions", "Examiner la vérité dans une réflexion sur la connaissance comme dans une perspective pratique", 8),
                ("TERMINALE_TECHNO", "PHILO_TTEC_NOT_ARTICULER","Notions", "Analyser, distinguer et articuler les notions entre elles de manière pertinente", 9),
        
                // --- Auteurs ---
                ("TERMINALE_TECHNO", "PHILO_TTEC_AUT_PERIODES", "Auteurs", "Situer un auteur du programme dans sa période : Antiquité et Moyen Âge, période moderne, période contemporaine", 10),
                ("TERMINALE_TECHNO", "PHILO_TTEC_AUT_TEXTES",   "Auteurs", "Étudier des textes d'une ampleur suffisante d'auteurs du programme", 11),
        
                // --- Repères ---
                ("TERMINALE_TECHNO", "PHILO_TTEC_REPERES",      "Repères", "Employer à bon escient les repères du programme : en fait/en droit, légal/légitime, croire/savoir…", 12),
        
                // --- Exercices : dissertation et explication de texte (épreuve écrite 4 h) ---
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_PROBLEME",     "Méthode", "Identifier, poser et formuler un problème lié à une ou plusieurs notions du programme", 13),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_DISSERTATION", "Méthode", "Construire, à partir de l'analyse d'une question simple, l'étude méthodique et progressive d'un problème", 14),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_ARGUMENTER",   "Méthode", "Étayer sa réflexion par des analyses conceptuelles, des références et des exemples pertinents", 15),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_OBJECTIONS",   "Méthode", "Discuter une thèse, examiner les objections et y répondre par des justifications raisonnées", 16),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_COMPOSER",     "Méthode", "Composer avec méthode : étapes, transitions, raisons explicites et conclusion justifiée", 17),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_EXPLICATION",  "Méthode", "Expliquer un texte : dégager son problème, son organisation et le sens de ses concepts", 18),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_QUESTIONS_TEXTE","Méthode", "Rédiger l'explication en répondant aux questions du sujet ou en suivant son propre développement", 19),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_EXPRESSION",   "Méthode", "Exprimer ses idées simplement et avec nuance, en précisant le sens retenu des termes employés", 20),
                ("TERMINALE_TECHNO", "PHILO_TTEC_METH_ORAL_CONTROLE","Méthode", "Présenter en dix minutes l'explication d'un texte étudié en classe, puis la développer en entretien", 21),
            };
    }
}
