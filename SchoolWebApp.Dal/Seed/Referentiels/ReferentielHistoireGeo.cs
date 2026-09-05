namespace SchoolWebApp.Dal.Seed.Referentiels
{
    /// <summary>
    /// Référentiel d'histoire-géographie-EMC, du CM1 à la terminale.
    ///
    /// TROIS DISCIPLINES, TROIS DOMAINES
    /// ---------------------------------
    /// Histoire, géographie et EMC sont regroupées sous une seule note mais
    /// n'ont ni les mêmes méthodes ni les mêmes attendus. Le domaine les
    /// sépare, sans quoi une lacune de méthode en géographie viendrait
    /// contaminer le diagnostic en histoire.
    ///
    /// LE CONTENU N'EST PAS LA COMPÉTENCE
    /// ----------------------------------
    /// Ce qui est noté au collège comme au lycée, ce sont des gestes :
    /// analyser un document, situer, rédiger un développement construit,
    /// réaliser un croquis. Les repères de connaissance sont présents, mais
    /// une par période plutôt qu'une par événement — sinon le référentiel
    /// deviendrait un manuel, et l'observateur ne saurait plus quoi cocher.
    /// </summary>
    public static class ReferentielHistoireGeo
    {
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Competences =>
            new[]
            {
                // --- CM1 : entrée dans le récit historique ---
                ("CM1", "HG_CM1_HIST_FRISE",       "Histoire", "Placer des événements sur une frise chronologique", 1),
                ("CM1", "HG_CM1_HIST_GAULE",       "Histoire", "Situer la Gaule, la conquête romaine et la romanisation", 2),
                ("CM1", "HG_CM1_HIST_MOYENAGE",    "Histoire", "Caractériser la société du Moyen Âge et le rôle du roi", 3),
                ("CM1", "HG_CM1_HIST_RENAISSANCE", "Histoire", "Situer les grandes découvertes et la Renaissance", 4),
                ("CM1", "HG_CM1_HIST_DOCUMENT",    "Histoire", "Dire ce qu'est un document : sa nature, son auteur, sa date", 5),
                ("CM1", "HG_CM1_GEO_HABITER",      "Géographie", "Décrire comment on habite une ville, un espace rural, un littoral", 6),
                ("CM1", "HG_CM1_GEO_CARTE",        "Géographie", "Lire une carte : titre, légende, orientation, échelle", 7),
                ("CM1", "HG_CM1_GEO_SITUER",       "Géographie", "Situer un lieu en France, en Europe et dans le monde", 8),
                ("CM1", "HG_CM1_GEO_DEPLACEMENTS", "Géographie", "Expliquer comment on se déplace et pourquoi", 9),
                ("CM1", "HG_CM1_EMC_REGLES",       "EMC", "Distinguer une règle, une loi et une habitude", 10),
                ("CM1", "HG_CM1_EMC_SYMBOLES",     "EMC", "Identifier les symboles de la République et leur sens", 11),
                ("CM1", "HG_CM1_METH_VOCABULAIRE", "Méthode", "Employer le vocabulaire de la chronologie : siècle, décennie, avant et après J.-C.", 12),

                ("CM1", "HG_CM1_HIST_MONARCHIE", "Histoire", "Caractériser la monarchie en France aux XVIe et XVIIe siècles", 13),
                ("CM1", "HG_CM1_HIST_1789", "Histoire", "Raconter 1789, une année révolutionnaire", 14),
                ("CM1", "HG_CM1_HIST_QUOTIDIEN", "Histoire", "Décrire la vie quotidienne au Moyen Âge", 15),
                ("CM1", "HG_CM1_GEO_MODES_VIE", "Géographie", "Comparer des modes de vie dans le monde", 16),

                // --- CM2 : de la Révolution à aujourd'hui ---
                ("CM2", "HG_CM2_HIST_REVOLUTION",  "Histoire", "Expliquer ce que la Révolution française change dans la société", 1),
                ("CM2", "HG_CM2_HIST_XIXE",        "Histoire", "Caractériser l'âge industriel et ses transformations sociales", 2),
                ("CM2", "HG_CM2_HIST_REPUBLIQUE",  "Histoire", "Situer l'installation de la République et l'école de Jules Ferry", 3),
                ("CM2", "HG_CM2_HIST_GUERRES",     "Histoire", "Situer les deux guerres mondiales et leurs conséquences", 4),
                ("CM2", "HG_CM2_HIST_CAUSES",      "Histoire", "Distinguer une cause d'une conséquence dans un événement", 5),
                ("CM2", "HG_CM2_GEO_MONDE",        "Géographie", "Situer la France dans le monde : territoires, océans, continents", 6),
                ("CM2", "HG_CM2_GEO_COMMUNIQUER",  "Géographie", "Expliquer comment les réseaux relient les territoires", 7),
                ("CM2", "HG_CM2_GEO_MONDIALISATION","Géographie", "Décrire un espace touristique ou de production mondialisé", 8),
                ("CM2", "HG_CM2_GEO_CROQUIS",      "Géographie", "Compléter un croquis simple avec une légende ordonnée", 9),
                ("CM2", "HG_CM2_EMC_LAICITE",      "EMC", "Expliquer ce qu'est la laïcité à l'école", 10),
                ("CM2", "HG_CM2_EMC_ENGAGEMENT",   "EMC", "Distinguer un droit d'un devoir dans la vie collective", 11),
                ("CM2", "HG_CM2_METH_REPONSE",     "Méthode", "Rédiger une réponse complète en réutilisant les mots de la question", 12),

                ("CM2", "HG_CM2_HIST_EMPIRE", "Histoire", "Situer le passage de la République à l’Empire (1792-1815)", 13),
                ("CM2", "HG_CM2_HIST_DEPUIS_1945", "Histoire", "Décrire les transformations de la France depuis 1945", 14),
                ("CM2", "HG_CM2_HIST_DEUX_REPUBLIQUES", "Histoire", "Distinguer la IIe et la IIIe République", 15),

                // --- 6e : de la préhistoire à l'Empire romain ---
                ("SIXIEME", "HG_6E_HIST_PREHISTOIRE","Histoire", "Situer les premières traces de vie humaine et la révolution néolithique", 1),
                ("SIXIEME", "HG_6E_HIST_ORIENT",    "Histoire", "Caractériser les premiers États et les premières écritures", 2),
                ("SIXIEME", "HG_6E_HIST_GRECE",     "Histoire", "Expliquer ce qu'est la cité grecque et la démocratie athénienne", 3),
                ("SIXIEME", "HG_6E_HIST_ROME",      "Histoire", "Raconter la fondation de Rome et la construction de l'Empire", 4),
                ("SIXIEME", "HG_6E_HIST_MONOTHEISMES","Histoire", "Situer la naissance du judaïsme et du christianisme", 5),
                ("SIXIEME", "HG_6E_HIST_EMPIRES",   "Histoire", "Comparer l'Empire romain à un empire contemporain de la Chine", 6),
                ("SIXIEME", "HG_6E_GEO_METROPOLES", "Géographie", "Décrire et expliquer la vie dans une métropole", 7),
                ("SIXIEME", "HG_6E_GEO_RURAL",      "Géographie", "Décrire un espace de faible densité et ses contraintes", 8),
                ("SIXIEME", "HG_6E_GEO_LITTORAL",   "Géographie", "Expliquer les usages concurrents d'un littoral", 9),
                ("SIXIEME", "HG_6E_GEO_REPARTITION","Géographie", "Décrire la répartition de la population mondiale", 10),
                ("SIXIEME", "HG_6E_GEO_PLANETE",    "Géographie", "Situer les grands repères physiques de la planète", 11),
                ("SIXIEME", "HG_6E_EMC_COLLEGE",    "EMC", "Expliquer les règles du collège et leur raison d'être", 12),
                ("SIXIEME", "HG_6E_EMC_IDENTITE",   "EMC", "Distinguer identité personnelle et identité collective", 13),
                ("SIXIEME", "HG_6E_METH_DOCUMENT",  "Méthode", "Présenter un document : nature, auteur, date, source", 14),
                ("SIXIEME", "HG_6E_METH_PRELEVER",  "Méthode", "Prélever dans un document l'information qui répond à la question", 15),
                ("SIXIEME", "HG_6E_METH_FRISE",     "Méthode", "Construire une frise et y placer les repères du programme", 16),
                ("SIXIEME", "HG_6E_METH_CARTE",     "Méthode", "Localiser sur une carte et rédiger une légende", 17),
                ("SIXIEME", "HG_6E_METH_REDIGER",   "Méthode", "Rédiger un paragraphe qui répond entièrement à la consigne", 18),

                // --- 5e : Moyen Âge, Temps modernes, développement durable ---
                ("CINQUIEME", "HG_5E_HIST_ISLAM",     "Histoire", "Situer la naissance de l'islam et l'expansion des empires musulmans", 1),
                ("CINQUIEME", "HG_5E_HIST_FEODALITE", "Histoire", "Caractériser la société féodale et la seigneurie", 2),
                ("CINQUIEME", "HG_5E_HIST_ETAT",      "Histoire", "Expliquer l'affirmation de l'État royal en France", 3),
                ("CINQUIEME", "HG_5E_HIST_VILLES",    "Histoire", "Expliquer l'essor des villes et du commerce médiéval", 4),
                ("CINQUIEME", "HG_5E_HIST_DECOUVERTES","Histoire", "Expliquer les grandes découvertes et leurs conséquences", 5),
                ("CINQUIEME", "HG_5E_HIST_REFORMES",  "Histoire", "Caractériser la Renaissance, l'humanisme et les réformes religieuses", 6),
                ("CINQUIEME", "HG_5E_HIST_ABSOLUTISME","Histoire", "Expliquer ce qu'est la monarchie absolue sous Louis XIV", 7),
                ("CINQUIEME", "HG_5E_GEO_DEMOGRAPHIE","Géographie", "Expliquer la croissance démographique et ses effets", 8),
                ("CINQUIEME", "HG_5E_GEO_RESSOURCES", "Géographie", "Expliquer la gestion d'une ressource : eau, énergie, alimentation", 9),
                ("CINQUIEME", "HG_5E_GEO_CLIMAT",     "Géographie", "Expliquer le changement climatique et ses effets sur un territoire", 10),
                ("CINQUIEME", "HG_5E_GEO_RISQUE",     "Géographie", "Distinguer aléa, vulnérabilité et risque", 11),
                ("CINQUIEME", "HG_5E_GEO_INEGALITES", "Géographie", "Décrire des inégalités de richesse et de développement", 12),
                ("CINQUIEME", "HG_5E_EMC_EGALITE",    "EMC", "Expliquer le principe d'égalité et les discriminations qu'il interdit", 13),
                ("CINQUIEME", "HG_5E_EMC_SOLIDARITE", "EMC", "Expliquer une forme de solidarité et son financement", 14),
                ("CINQUIEME", "HG_5E_METH_CONFRONTER","Méthode", "Confronter deux documents et repérer ce qui les oppose", 15),
                ("CINQUIEME", "HG_5E_METH_DEVELOPPE", "Méthode", "Rédiger un développement construit d'une quinzaine de lignes", 16),
                ("CINQUIEME", "HG_5E_METH_CROQUIS",   "Méthode", "Réaliser un croquis simple avec une légende organisée", 17),
                ("CINQUIEME", "HG_5E_METH_CRITIQUE",  "Méthode", "Identifier le point de vue de l'auteur d'un document", 18),

                ("CINQUIEME", "HG_5E_HIST_BYZANCE", "Histoire", "Situer Byzance et l’Europe carolingienne, des mondes en contact", 19),
                ("CINQUIEME", "HG_5E_HIST_EMPIRES_XVI", "Histoire", "Décrire le monde au temps de Charles Quint et de Soliman", 20),

                // --- 4e : le XVIIIe, la révolution industrielle, la mondialisation ---
                ("QUATRIEME", "HG_4E_HIST_LUMIERES",  "Histoire", "Caractériser l'esprit des Lumières et sa diffusion", 1),
                ("QUATRIEME", "HG_4E_HIST_COMMERCE",  "Histoire", "Expliquer le commerce atlantique et la traite négrière", 2),
                ("QUATRIEME", "HG_4E_HIST_REVOLUTION","Histoire", "Expliquer les étapes de la Révolution française et de l'Empire", 3),
                ("QUATRIEME", "HG_4E_HIST_INDUSTRIE", "Histoire", "Expliquer l'industrialisation et la nouvelle société de classes", 4),
                ("QUATRIEME", "HG_4E_HIST_COLONISATION","Histoire", "Expliquer les motifs et les formes de la colonisation au XIXe siècle", 5),
                ("QUATRIEME", "HG_4E_HIST_REPUBLIQUE","Histoire", "Expliquer l'enracinement de la IIIe République", 6),
                ("QUATRIEME", "HG_4E_GEO_URBANISATION","Géographie", "Expliquer l'urbanisation mondiale et ses formes", 7),
                ("QUATRIEME", "HG_4E_GEO_MIGRATIONS", "Géographie", "Distinguer les types de migrations et leurs causes", 8),
                ("QUATRIEME", "HG_4E_GEO_TOURISME",   "Géographie", "Expliquer les effets du tourisme sur un espace", 9),
                ("QUATRIEME", "HG_4E_GEO_MARITIME",   "Géographie", "Expliquer le rôle des mers et des océans dans les échanges", 10),
                ("QUATRIEME", "HG_4E_GEO_FIRMES",     "Géographie", "Expliquer le rôle des firmes transnationales dans la mondialisation", 11),
                ("QUATRIEME", "HG_4E_EMC_LIBERTES",   "EMC", "Distinguer les libertés individuelles et collectives et leurs limites", 12),
                ("QUATRIEME", "HG_4E_EMC_INFORMATION","EMC", "Évaluer la fiabilité d'une information et de sa source", 13),
                ("QUATRIEME", "HG_4E_METH_ANALYSE",   "Méthode", "Analyser un document en distinguant ce qu'il dit et ce qu'il tait", 14),
                ("QUATRIEME", "HG_4E_METH_STATISTIQUE","Méthode", "Lire un graphique ou un tableau statistique et en tirer une idée", 15),
                ("QUATRIEME", "HG_4E_METH_PLAN",      "Méthode", "Organiser un développement construit en deux ou trois parties", 16),
                ("QUATRIEME", "HG_4E_METH_CROQUIS",   "Méthode", "Réaliser un croquis avec figurés adaptés et légende hiérarchisée", 17),

                ("QUATRIEME", "HG_4E_HIST_VOTER", "Histoire", "Retracer la difficile conquête du droit de vote de 1815 à 1870", 18),
                ("QUATRIEME", "HG_4E_HIST_FEMMES_XIX", "Histoire", "Décrire les conditions féminines dans la société du XIXe siècle", 19),
                ("QUATRIEME", "HG_4E_GEO_ETATS_UNIS", "Géographie", "Expliquer l’adaptation du territoire des États-Unis à la mondialisation", 20),
                ("QUATRIEME", "HG_4E_GEO_AFRIQUE", "Géographie", "Décrire les dynamiques d’un grand ensemble géographique africain", 21),

                // --- 3e : le XXe siècle et la France d'aujourd'hui ---
                ("TROISIEME", "HG_3E_HIST_1GM",       "Histoire", "Expliquer pourquoi la Première Guerre mondiale est une guerre totale", 1),
                ("TROISIEME", "HG_3E_HIST_TOTALITAIRES","Histoire", "Caractériser les régimes totalitaires de l'entre-deux-guerres", 2),
                ("TROISIEME", "HG_3E_HIST_2GM",       "Histoire", "Expliquer les phases de la Seconde Guerre mondiale et les génocides", 3),
                ("TROISIEME", "HG_3E_HIST_FRANCE_40", "Histoire", "Distinguer le régime de Vichy, la collaboration et la Résistance", 4),
                ("TROISIEME", "HG_3E_HIST_GUERRE_FROIDE","Histoire", "Expliquer l'affrontement Est-Ouest et ses formes", 5),
                ("TROISIEME", "HG_3E_HIST_DECOLONISATION","Histoire", "Expliquer la décolonisation et la naissance de nouveaux États", 6),
                ("TROISIEME", "HG_3E_HIST_VE_REPUBLIQUE","Histoire", "Caractériser les institutions de la Ve République", 7),
                ("TROISIEME", "HG_3E_HIST_FEMMES",    "Histoire", "Retracer la conquête des droits des femmes au XXe siècle", 8),
                ("TROISIEME", "HG_3E_GEO_AIRES",      "Géographie", "Caractériser une aire urbaine et ses dynamiques", 9),
                ("TROISIEME", "HG_3E_GEO_PRODUCTIF",  "Géographie", "Expliquer les recompositions des espaces productifs français", 10),
                ("TROISIEME", "HG_3E_GEO_FAIBLE_DENSITE","Géographie", "Expliquer les dynamiques des espaces de faible densité", 11),
                ("TROISIEME", "HG_3E_GEO_UE",         "Géographie", "Situer la France dans l'Union européenne et le monde", 12),
                ("TROISIEME", "HG_3E_GEO_ULTRAMARINS","Géographie", "Caractériser les territoires ultramarins et leurs contraintes", 13),
                ("TROISIEME", "HG_3E_GEO_AMENAGEMENT","Géographie", "Expliquer un aménagement du territoire et ses acteurs", 14),
                ("TROISIEME", "HG_3E_EMC_CITOYENNETE","EMC", "Expliquer les droits et devoirs du citoyen français et européen", 15),
                ("TROISIEME", "HG_3E_EMC_DEFENSE",    "EMC", "Expliquer les formes de l'engagement et de la défense nationale", 16),
                ("TROISIEME", "HG_3E_METH_DEVELOPPE", "Méthode", "Rédiger un développement construit répondant entièrement au sujet", 17),
                ("TROISIEME", "HG_3E_METH_DOCUMENT",  "Méthode", "Analyser un document en le replaçant dans son contexte", 18),
                ("TROISIEME", "HG_3E_METH_CARTE",     "Méthode", "Compléter la carte de la France ou de l'Europe demandée au brevet", 19),

                ("TROISIEME", "HG_3E_HIST_PROJET_EUROPEEN", "Histoire", "Expliquer l’affirmation et la mise en œuvre du projet européen", 20),
                ("TROISIEME", "HG_3E_HIST_APRES_1989", "Histoire", "Caractériser les enjeux et les conflits du monde après 1989", 21),
                ("TROISIEME", "HG_3E_HIST_REFONDER", "Histoire", "Expliquer comment la République est refondée entre 1944 et 1947", 22),

                // --- Seconde : les grandes ruptures ---
                ("SECONDE", "HG_2DE_HIST_MEDITERRANEE","Histoire", "Caractériser la Méditerranée antique et ses héritages", 1),
                ("SECONDE", "HG_2DE_HIST_MEDIEVALE",  "Histoire", "Expliquer les échanges et les affrontements en Méditerranée médiévale", 2),
                ("SECONDE", "HG_2DE_HIST_XVE_XVIE",   "Histoire", "Expliquer l'ouverture atlantique et la Renaissance", 3),
                ("SECONDE", "HG_2DE_HIST_ABSOLUTISME","Histoire", "Comparer l'absolutisme français et le modèle anglais", 4),
                ("SECONDE", "HG_2DE_HIST_REVOLUTIONS","Histoire", "Expliquer la Révolution française comme rupture politique et sociale", 5),
                ("SECONDE", "HG_2DE_HIST_XIXE",       "Histoire", "Expliquer l'industrialisation et les nouvelles idéologies du XIXe", 6),
                ("SECONDE", "HG_2DE_GEO_TRANSITIONS", "Géographie", "Expliquer la transition démographique et ses décalages", 7),
                ("SECONDE", "HG_2DE_GEO_DEVELOPPEMENT","Géographie", "Confronter développement et inégalités à différentes échelles", 8),
                ("SECONDE", "HG_2DE_GEO_RESSOURCES",  "Géographie", "Analyser la tension entre ressources disponibles et besoins", 9),
                ("SECONDE", "HG_2DE_GEO_ENERGIE",     "Géographie", "Analyser la transition énergétique d'un territoire", 10),
                ("SECONDE", "HG_2DE_GEO_MOBILITES",   "Géographie", "Analyser les mobilités touristiques et migratoires", 11),
                ("SECONDE", "HG_2DE_GEO_AFRIQUE",     "Géographie", "Étudier un espace africain à plusieurs échelles", 12),
                ("SECONDE", "HG_2DE_EMC_LIBERTE",     "EMC", "Analyser les fondements et les limites de la liberté d'expression", 13),
                ("SECONDE", "HG_2DE_METH_ANALYSE",    "Méthode", "Analyser un document en dégageant son intérêt et ses limites", 14),
                ("SECONDE", "HG_2DE_METH_PROBLEME",   "Méthode", "Dégager une problématique à partir d'un sujet", 15),
                ("SECONDE", "HG_2DE_METH_PLAN",       "Méthode", "Construire un plan en deux ou trois parties équilibrées", 16),
                ("SECONDE", "HG_2DE_METH_REDIGER",    "Méthode", "Rédiger une réponse organisée avec introduction et conclusion", 17),
                ("SECONDE", "HG_2DE_METH_CROQUIS",    "Méthode", "Réaliser un croquis de géographie avec une légende organisée", 18),
                ("SECONDE", "HG_2DE_METH_ECHELLES",   "Méthode", "Changer d'échelle pour nuancer une analyse", 19),

                ("SECONDE", "HG_2DE_HIST_SOCIETE_ORDRES", "Histoire", "Analyser les tensions et les mutations de la société d’ordres", 20),
                ("SECONDE", "HG_2DE_HIST_LUMIERES", "Histoire", "Expliquer les Lumières et le développement des sciences", 21),

                // --- Première : États, démocratie, mondialisation ---
                ("PREMIERE", "HG_1RE_HIST_NATIONS",   "Histoire", "Expliquer l'affirmation des nations en Europe au XIXe siècle", 1),
                ("PREMIERE", "HG_1RE_HIST_INDUSTRIE", "Histoire", "Analyser les transformations sociales de l'industrialisation", 2),
                ("PREMIERE", "HG_1RE_HIST_REPUBLIQUE","Histoire", "Expliquer l'enracinement de la République et l'affaire Dreyfus", 3),
                ("PREMIERE", "HG_1RE_HIST_COLONIAL",  "Histoire", "Analyser l'expansion coloniale européenne et ses contestations", 4),
                ("PREMIERE", "HG_1RE_HIST_1GM",       "Histoire", "Expliquer la brutalisation des sociétés dans la Grande Guerre", 5),
                ("PREMIERE", "HG_1RE_HIST_ENTRE_DEUX","Histoire", "Analyser la fragilisation des démocraties dans les années 1930", 6),
                ("PREMIERE", "HG_1RE_GEO_METROPOLISATION","Géographie", "Analyser la métropolisation et ses effets contrastés", 7),
                ("PREMIERE", "HG_1RE_GEO_RURAUX",     "Géographie", "Analyser les recompositions des espaces ruraux", 8),
                ("PREMIERE", "HG_1RE_GEO_PRODUCTIFS", "Géographie", "Analyser les logiques de localisation des espaces productifs", 9),
                ("PREMIERE", "HG_1RE_GEO_FRANCE",     "Géographie", "Analyser les fractures territoriales françaises", 10),
                ("PREMIERE", "HG_1RE_GEO_UE",         "Géographie", "Analyser les inégalités et les politiques territoriales européennes", 11),
                ("PREMIERE", "HG_1RE_EMC_DEMOCRATIE", "EMC", "Analyser les formes et les fragilités de la démocratie représentative", 12),
                ("PREMIERE", "HG_1RE_METH_COMPOSITION","Méthode", "Rédiger une composition problématisée et argumentée", 13),
                ("PREMIERE", "HG_1RE_METH_ETUDE_DOC", "Méthode", "Conduire une étude critique d'un ou deux documents", 14),
                ("PREMIERE", "HG_1RE_METH_CONTEXTE",  "Méthode", "Replacer un document dans son contexte pour l'interpréter", 15),
                ("PREMIERE", "HG_1RE_METH_CROQUIS",   "Méthode", "Réaliser un croquis répondant à un sujet de géographie", 16),
                ("PREMIERE", "HG_1RE_METH_ARGUMENT",  "Méthode", "Appuyer chaque argument sur un exemple précis et daté", 17),
                ("PREMIERE", "HG_1RE_METH_INTRO",     "Méthode", "Rédiger une introduction qui pose le sujet, les bornes et la problématique", 18),

                ("PREMIERE", "HG_1RE_HIST_REVOLUTION_NATION", "Histoire", "Expliquer comment la Révolution et l’Empire forgent une nouvelle conception de la nation", 19),
                ("PREMIERE", "HG_1RE_HIST_RESTAURATION", "Histoire", "Analyser l’Europe entre restauration et révolution, de 1814 à 1848", 20),
                ("PREMIERE", "HG_1RE_HIST_AGE_DEMOCRATIQUE", "Histoire", "Analyser la Deuxième République et le Second Empire", 21),
                ("PREMIERE", "HG_1RE_HIST_NOUVEAUX_ETATS", "Histoire", "Expliquer la construction de nouveaux États par la guerre et la diplomatie", 22),
                ("PREMIERE", "HG_1RE_HIST_SOCIETE_1914", "Histoire", "Analyser les permanences et les mutations de la société française jusqu’en 1914", 23),
                ("PREMIERE", "HG_1RE_HIST_EMBRASEMENT", "Histoire", "Retracer l’embrasement mondial de 1914 et ses grandes étapes", 24),
                ("PREMIERE", "HG_1RE_HIST_SORTIR_GUERRE", "Histoire", "Analyser la tentative de construction d’un ordre des nations après 1918", 25),

                // --- Terminale : le monde depuis 1945 ---
                ("TERMINALE", "HG_TLE_HIST_1945",     "Histoire", "Expliquer la reconstruction d'un ordre mondial après 1945", 1),
                ("TERMINALE", "HG_TLE_HIST_GUERRE_FROIDE","Histoire", "Analyser les formes et les crises de la guerre froide", 2),
                ("TERMINALE", "HG_TLE_HIST_DECOLONISATION","Histoire", "Analyser la décolonisation et l'émergence du tiers-monde", 3),
                ("TERMINALE", "HG_TLE_HIST_MUTATIONS","Histoire", "Analyser les mutations économiques et sociales des Trente Glorieuses", 4),
                ("TERMINALE", "HG_TLE_HIST_1989",     "Histoire", "Expliquer la fin de l'URSS et le nouvel ordre mondial", 5),
                ("TERMINALE", "HG_TLE_HIST_FRANCE",   "Histoire", "Analyser la Ve République et ses recompositions politiques", 6),
                ("TERMINALE", "HG_TLE_HIST_MEMOIRE",  "Histoire", "Distinguer histoire et mémoire d'un événement", 7),
                ("TERMINALE", "HG_TLE_GEO_MERS",      "Géographie", "Analyser les enjeux de puissance liés aux mers et aux océans", 8),
                ("TERMINALE", "HG_TLE_GEO_DYNAMIQUES","Géographie", "Analyser les dynamiques de la mondialisation et ses acteurs", 9),
                ("TERMINALE", "HG_TLE_GEO_FRONTIERES","Géographie", "Analyser le rôle des frontières dans le monde contemporain", 10),
                ("TERMINALE", "HG_TLE_GEO_ENVIRONNEMENT","Géographie", "Analyser la place de l'environnement dans les rapports internationaux", 11),
                ("TERMINALE", "HG_TLE_GEO_FRANCE",    "Géographie", "Analyser la place de la France dans la mondialisation", 12),
                ("TERMINALE", "HG_TLE_EMC_ENGAGEMENT","EMC", "Analyser les formes contemporaines de l'engagement citoyen", 13),
                ("TERMINALE", "HG_TLE_METH_COMPOSITION","Méthode", "Rédiger une composition en quatre heures sur un sujet du programme", 14),
                ("TERMINALE", "HG_TLE_METH_ETUDE_DOC","Méthode", "Conduire une étude critique en confrontant deux documents", 15),
                ("TERMINALE", "HG_TLE_METH_CROQUIS",  "Méthode", "Réaliser un croquis complet de géographie en temps limité", 16),
                ("TERMINALE", "HG_TLE_METH_REPERES",  "Méthode", "Mobiliser les repères chronologiques et spatiaux du programme", 17),
                ("TERMINALE", "HG_TLE_HIST_CRISE_1929", "Histoire", "Analyser l’impact de la crise de 1929 sur les sociétés et les régimes", 19),
                ("TERMINALE", "HG_TLE_HIST_TOTALITAIRES", "Histoire", "Caractériser les régimes totalitaires de l’entre-deux-guerres", 20),
                ("TERMINALE", "HG_TLE_HIST_2GM", "Histoire", "Analyser la Seconde Guerre mondiale et ses basculements", 21),
                ("TERMINALE", "HG_TLE_HIST_CONSTRUCTION_EU", "Histoire", "Analyser la construction européenne, ses élargissements et ses remises en question", 22),
                ("TERMINALE", "HG_TLE_HIST_FRANCE_1974", "Histoire", "Analyser le tournant social et politique de la France de 1974 à 1988", 23),

                ("TERMINALE", "HG_TLE_METH_NUANCER",  "Méthode", "Nuancer une thèse en confrontant les échelles et les acteurs", 18),
            };

        /// <summary>
        /// Le graphe est ici surtout MÉTHODOLOGIQUE. Les connaissances d'une
        /// période ne conditionnent pas celles de la suivante — on peut ignorer
        /// la féodalité et comprendre la Révolution — mais on ne peut pas
        /// rédiger une composition sans savoir construire un plan, ni analyser
        /// un document sans savoir en présenter la nature. C'est là que les
        /// élèves décrochent, et c'est donc là que les arêtes se concentrent.
        /// </summary>
        public static (string Competence, string Prerequis, int Poids)[] Prerequis =>
            new[]
            {
                // La chaîne méthodologique, du CM1 à la terminale.
                ("HG_CM2_METH_REPONSE",      "HG_CM1_HIST_DOCUMENT",     2),
                ("HG_6E_METH_DOCUMENT",      "HG_CM1_HIST_DOCUMENT",     3),
                ("HG_6E_METH_PRELEVER",      "HG_6E_METH_DOCUMENT",      3),
                ("HG_6E_METH_REDIGER",       "HG_CM2_METH_REPONSE",      3),
                ("HG_6E_METH_FRISE",         "HG_CM1_HIST_FRISE",        3),
                ("HG_6E_METH_CARTE",         "HG_CM1_GEO_CARTE",         3),
                ("HG_5E_METH_CONFRONTER",    "HG_6E_METH_PRELEVER",      3),
                ("HG_5E_METH_CRITIQUE",      "HG_6E_METH_DOCUMENT",      3),
                ("HG_5E_METH_DEVELOPPE",     "HG_6E_METH_REDIGER",       3),
                ("HG_5E_METH_CROQUIS",       "HG_6E_METH_CARTE",         3),
                ("HG_5E_METH_CROQUIS",       "HG_CM2_GEO_CROQUIS",       2),
                ("HG_4E_METH_ANALYSE",       "HG_5E_METH_CRITIQUE",      3),
                ("HG_4E_METH_ANALYSE",       "HG_5E_METH_CONFRONTER",    3),
                ("HG_4E_METH_PLAN",          "HG_5E_METH_DEVELOPPE",     3),
                ("HG_4E_METH_CROQUIS",       "HG_5E_METH_CROQUIS",       3),
                ("HG_4E_METH_STATISTIQUE",   "HG_6E_METH_PRELEVER",      2),
                ("HG_3E_METH_DEVELOPPE",     "HG_4E_METH_PLAN",          3),
                ("HG_3E_METH_DOCUMENT",      "HG_4E_METH_ANALYSE",       3),
                ("HG_3E_METH_CARTE",         "HG_4E_METH_CROQUIS",       2),
                ("HG_2DE_METH_ANALYSE",      "HG_3E_METH_DOCUMENT",      3),
                ("HG_2DE_METH_PROBLEME",     "HG_3E_METH_DEVELOPPE",     3),
                ("HG_2DE_METH_PLAN",         "HG_2DE_METH_PROBLEME",     3),
                ("HG_2DE_METH_REDIGER",      "HG_2DE_METH_PLAN",         3),
                ("HG_2DE_METH_CROQUIS",      "HG_3E_METH_CARTE",         3),
                ("HG_2DE_METH_ECHELLES",     "HG_2DE_METH_ANALYSE",      2),
                ("HG_1RE_METH_COMPOSITION",  "HG_2DE_METH_REDIGER",      3),
                ("HG_1RE_METH_INTRO",        "HG_2DE_METH_PROBLEME",     3),
                ("HG_1RE_METH_ETUDE_DOC",    "HG_2DE_METH_ANALYSE",      3),
                ("HG_1RE_METH_CONTEXTE",     "HG_2DE_METH_ANALYSE",      3),
                ("HG_1RE_METH_CROQUIS",      "HG_2DE_METH_CROQUIS",      3),
                ("HG_1RE_METH_ARGUMENT",     "HG_1RE_METH_COMPOSITION",  2),
                ("HG_TLE_METH_COMPOSITION",  "HG_1RE_METH_COMPOSITION",  3),
                ("HG_TLE_METH_ETUDE_DOC",    "HG_1RE_METH_ETUDE_DOC",    3),
                ("HG_TLE_METH_CROQUIS",      "HG_1RE_METH_CROQUIS",      3),
                ("HG_TLE_METH_NUANCER",      "HG_2DE_METH_ECHELLES",     2),

                // Rédiger en histoire-géo suppose de savoir rédiger tout court :
                // la moitié des points perdus au brevet sont des points de
                // français dans une copie d'histoire.
                ("HG_6E_METH_REDIGER",       "FR_CM2_ECR_CONSIGNE",      2),
                ("HG_5E_METH_DEVELOPPE",     "FR_6E_ECR_RECIT",          2),
                ("HG_3E_METH_DEVELOPPE",     "FR_3E_ECR_SUJET_REFLEXION",2),
                ("HG_2DE_METH_REDIGER",      "FR_2DE_ECR_INTRODUCTION",  2),
                ("HG_1RE_METH_COMPOSITION",  "FR_2DE_ECR_DISSERTATION",  2),

                // Lire un graphique en géographie suppose de savoir lire un
                // graphique tout court.
                ("HG_4E_METH_STATISTIQUE",   "MATH_6E_DATA_GRAPHIQUE",    2),

                // Quelques enchaînements de contenu qui, eux, sont réels.
                ("HG_CM2_HIST_REVOLUTION",   "HG_CM1_HIST_RENAISSANCE",  1),
                ("HG_CM2_HIST_CAUSES",       "HG_CM1_METH_VOCABULAIRE",  2),
                ("HG_6E_HIST_ROME",          "HG_6E_HIST_GRECE",         1),
                ("HG_5E_HIST_ETAT",          "HG_5E_HIST_FEODALITE",     2),
                ("HG_5E_HIST_REFORMES",      "HG_5E_HIST_DECOUVERTES",   1),
                ("HG_5E_HIST_ABSOLUTISME",   "HG_5E_HIST_ETAT",          3),
                ("HG_4E_HIST_REVOLUTION",    "HG_4E_HIST_LUMIERES",      2),
                ("HG_4E_HIST_REPUBLIQUE",    "HG_4E_HIST_REVOLUTION",    2),
                ("HG_4E_HIST_COLONISATION",  "HG_4E_HIST_INDUSTRIE",     2),
                ("HG_3E_HIST_TOTALITAIRES",  "HG_3E_HIST_1GM",           3),
                ("HG_3E_HIST_2GM",           "HG_3E_HIST_TOTALITAIRES",  3),
                ("HG_3E_HIST_FRANCE_40",     "HG_3E_HIST_2GM",           3),
                ("HG_3E_HIST_GUERRE_FROIDE", "HG_3E_HIST_2GM",           3),
                ("HG_3E_HIST_DECOLONISATION","HG_4E_HIST_COLONISATION",  3),
                ("HG_3E_HIST_VE_REPUBLIQUE", "HG_4E_HIST_REPUBLIQUE",    2),
                ("HG_5E_GEO_RISQUE",         "HG_5E_GEO_CLIMAT",         2),
                ("HG_4E_GEO_FIRMES",         "HG_4E_GEO_MARITIME",       1),
                ("HG_3E_GEO_AIRES",          "HG_4E_GEO_URBANISATION",   3),
                ("HG_3E_GEO_UE",             "HG_6E_GEO_PLANETE",        2),
                ("HG_2DE_GEO_DEVELOPPEMENT", "HG_5E_GEO_INEGALITES",     2),
                ("HG_2DE_GEO_RESSOURCES",    "HG_5E_GEO_RESSOURCES",     2),
                ("HG_2DE_GEO_ENERGIE",       "HG_5E_GEO_CLIMAT",         2),
                ("HG_1RE_GEO_METROPOLISATION","HG_3E_GEO_AIRES",         3),
                ("HG_1RE_GEO_FRANCE",        "HG_3E_GEO_AMENAGEMENT",    2),
                ("HG_1RE_HIST_1GM",          "HG_3E_HIST_1GM",           2),
                ("HG_TLE_HIST_GUERRE_FROIDE","HG_3E_HIST_GUERRE_FROIDE", 2),
                ("HG_TLE_HIST_DECOLONISATION","HG_3E_HIST_DECOLONISATION",2),
                ("HG_TLE_GEO_DYNAMIQUES",    "HG_4E_GEO_FIRMES",         2),
                ("HG_TLE_HIST_MEMOIRE",      "HG_TLE_HIST_1945",         1),
            };
    }
}
