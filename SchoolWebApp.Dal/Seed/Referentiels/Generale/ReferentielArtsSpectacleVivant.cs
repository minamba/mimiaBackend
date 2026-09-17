namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// Spécialité « Arts » de la voie générale : théâtre et arts du cirque, tels que les textes
    /// officiels les définissent — provenance détaillée en tête du fichier. (Espace de noms et nom de classe à
    /// ajuster au dépôt ; arts plastiques, histoire des arts et cinéma-audiovisuel sont traités ailleurs.)
    /// </summary>
    public static class ReferentielArtsSpectacleVivant
    {
        // =====================================================================================
        // SPÉCIALITÉ ARTS (VOIE GÉNÉRALE) — THÉÂTRE, ARTS DU CIRQUE
        //
        // MUSIQUE ET DANSE RETIRÉES LE 17/09/2026, et il faut dire pourquoi : ce n'est PAS
        // que leurs textes auraient changé ou seraient devenus introuvables. Ils sont
        // toujours en vigueur, et la provenance ci-dessous reste vraie pour eux. C'est
        // Camara qui ne veut pas que la professeure d'arts les enseigne. Les tableaux MUS1,
        // MUST, DANSE1 et DANSET ont donc été supprimés, et les matières MUSIQUE et DANSE
        // fermées en base (voir VoiesScolaires.MatieresRetirees). Le jour où la décision
        // change, c'est un travail de relecture des annexes, pas une ligne à décommenter.
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis les textes
        // officiels téléchargés et lus le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\arts-b\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau  | Niveau    | Intitulé exact (titre de section de l'annexe)                        |
        // |----------|-----------|----------------------------------------------------------------------|
        // | THEA1    | PREMIERE  | « Théâtre - classe de première » (annexe de MENE1901567A)             |
        // | THEAT    | TERMINALE | « Théâtre - classe terminale » (annexe 2 de MENE1921245A)             |
        // | CIRQUE1  | PREMIERE  | « Arts du cirque - classe de première » (annexe de MENE1901567A)      |
        // | CIRQUET  | TERMINALE | « Arts du cirque - classe terminale » (annexe 2 de MENE1921245A)      |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901567A,
        //   BO spécial n° 1 du 22 janvier 2019. « Programme d'enseignement de spécialité d'arts des classes de
        //   première et terminale de la voie générale ». Article 2 (lu dans le PDF du BO spécial n° 1 et sur
        //   Légifrance) : « entrent en vigueur à la rentrée scolaire 2019 pour la classe de première et à la
        //   rentrée 2020 pour la classe terminale ».
        //   Annexe (titre : « Programme de spécialité d'arts de première et terminale générales ») :
        //     https://eduscol.education.gouv.fr/sites/default/files/document/spe567annexe22-11063846pdf-83196.pdf
        //   BO spécial n° 1 (PDF complet) : http://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/06/0/SP1_MEN_22_1_2019_10H40_1064060.pdf
        //   Légifrance : https://www.legifrance.gouv.fr/eli/arrete/2019/1/17/MENE1901567A/jo/texte
        //
        // TERMINALE (théâtre, cirque) : arrêté du 19-7-2019, J.O. du 23-7-2019, NOR MENE1921245A,
        //   BO spécial n° 8 du 25 juillet 2019, « modifiant l'arrêté du 17 janvier 2019 ». Il complète
        //   l'annexe par une annexe 2 « Programme de spécialité d'arts de terminale générale » (cirque, arts
        //   plastiques, cinéma-audiovisuel, danse, histoire des arts, théâtre — PAS de musique).
        //     https://eduscol.education.gouv.fr/sites/default/files/document/spe245annexe1159026pdf-83199.pdf
        //   Légifrance : https://www.legifrance.gouv.fr/eli/arrete/2019/7/19/MENE1921245A/jo/texte
        //   Entrée en vigueur : celle de l'arrêté modifié (rentrée 2020 pour la terminale). L'article 2 de
        //   MENE1921245A n'a pas pu être lu en clair (page BO bloquée par Cloudflare, HTML Légifrance
        //   non exploitable) : point non établi par lecture directe.
        //
        // PROGRAMMES LIMITATIFS (lignes « PL » ; À RETIRER OU REMPLACER après la session 2027)
        //   - Note de service du 29-12-2025, NOR MENE2536492N, BOENJS n° 4 du 22 janvier 2026 (PDF du BO lu) :
        //     annexe 1 cirque, annexe 7 théâtre. (Les annexes 4 et 6, danse et musique, ne servent plus ici.)
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : les pages éduscol « Programmes et ressources en … - voie GT »
        //   (théâtre 5865, arts du cirque 5769), lues trois fois le 14/09/2026, listent
        //   sous « Programmes en vigueur » la « Spécialité arts en première et terminale de la voie générale (BO
        //   spécial n° 1 du 22 janvier 2019) » et, sauf en musique, la « Spécialité arts en terminale générale
        //   (BO spécial n° 8 du 25 juillet 2019) ». Aucun arrêté modificatif postérieur n'a été trouvé
        //   (recherche BO, Légifrance et éduscol 2019-2026). Les notes de 2022-2026 touchent les ÉPREUVES et les
        //   programmes limitatifs, pas les programmes.
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - Arts du cirque : le préambule compte « quatre grandes familles » (avec le dressage), la compétence
        //     de première en cite « trois » (sans le dressage). Les deux formulations sont reprises telles quelles.
        //   - Théâtre de première : le texte propose sept objets d'étude dont « au moins deux » sont retenus par
        //     le professeur ; les sept sont listés.
        //   - Domaine « Pratique — … » : ce qui ne se vérifie qu'au plateau, à la piste, en studio ou en jeu
        //     musical ; le tuteur vocal peut en parler, pas l'évaluer.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   THEA1 30 · THEAT 32 · CIRQUE1 23 · CIRQUET 26 · total 111
        //   dont lignes de programme limitatif : THEAT 4 · CIRQUET 3
        //   (avant le retrait du 17/09/2026 : MUS1 27 · MUST 36 · DANSE1 29 · DANSET 27, total 230)
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — THÉÂTRE (annexe de MENE1901567A, « Théâtre - classe de première »)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] THEA1 =
        {
            ("PREMIERE", "THEA1_PROJET_COLLECTIF",       "Pratique — Jeu et plateau", "S'engager avec rigueur dans un projet collectif, écouter ses partenaires et se faire entendre", 1),
            ("PREMIERE", "THEA1_OBJET_THEATRE",          "Pratique — Jeu et plateau", "Participer au jeu, et éventuellement à la scénographie, au son ou à la lumière, d'un objet-théâtre présenté à un public", 2),
            ("PREMIERE", "THEA1_SITUATION_NOUVELLE",     "Pratique — Jeu et plateau", "S'impliquer dans une situation de jeu nouvelle en mettant en œuvre une démarche de création adaptée", 3),
            ("PREMIERE", "THEA1_DEUX_PROJETS",           "Pratique — Jeu et plateau", "Mener au moins deux projets de plateau sur des matériaux d'époques, d'écritures ou de registres différents", 4),
            ("PREMIERE", "THEA1_ANALYSER_PLATEAU",       "Pratique — Jeu et plateau", "Analyser son travail de plateau et celui de ses partenaires à l'aune de ses connaissances et de son regard de spectateur", 5),
            ("PREMIERE", "THEA1_COMPOSANTES_SPECTACLE",  "Culture théâtrale", "Décrire les composantes d'un spectacle et leur fonctionnement avec le vocabulaire spécifique du théâtre", 6),
            ("PREMIERE", "THEA1_ENJEUX_DEMARCHE",        "Culture théâtrale", "Repérer les enjeux d'une démarche théâtrale", 7),
            ("PREMIERE", "THEA1_PROCESSUS_CREATION",     "Culture théâtrale", "Réfléchir sur les processus de création et sur leur incidence sur le spectacle", 8),
            ("PREMIERE", "THEA1_DRAMATURGIE",            "Culture théâtrale", "Interroger la dramaturgie et les modes de composition d'un spectacle", 9),
            ("PREMIERE", "THEA1_EVENEMENT_PERFORMATIF",  "Culture théâtrale", "Observer la représentation comme un événement performatif, éphémère et unique, situé dans un temps, un lieu, devant un public", 10),
            ("PREMIERE", "THEA1_SITUER_DOCUMENT",        "Culture théâtrale", "Situer, décrire et analyser un texte dramatique ou théorique, une image, une captation ou un spectacle vu", 11),
            ("PREMIERE", "THEA1_NON_TEXTUEL",            "Culture théâtrale", "Analyser les éléments non textuels de la représentation (scénographie, lumière, son, vidéo) et leur interaction", 12),
            ("PREMIERE", "THEA1_TEXTE_VARIABLE",         "Culture théâtrale", "Montrer que le texte dit sur scène varie par sa nature, sa place, ses auteurs, ses codes et son usage", 13),
            ("PREMIERE", "THEA1_FAIT_THEATRAL",          "Fait théâtral et performance", "Expliquer le fait théâtral : le théâtre comme pratique sociale dont la fonction varie selon les époques et les publics", 14),
            ("PREMIERE", "THEA1_REPRESENTATION_PERFORMANCE", "Fait théâtral et performance", "Distinguer les notions de représentation et de performance et les appliquer à un spectacle", 15),
            ("PREMIERE", "THEA1_CADRE_ANTHROPOLOGIQUE",  "Fait théâtral et performance", "Relier une forme théâtrale à son cadre rituel, religieux, festif, carnavalesque, commémoratif ou civique", 16),
            ("PREMIERE", "THEA1_OBJ_ANTIQUE",            "Objets d'étude — histoire du théâtre", "Présenter la tragédie antique et le théâtre romain dans leur réalité anthropologique et leurs enjeux esthétiques", 17),
            ("PREMIERE", "THEA1_OBJ_MOYEN_AGE",          "Objets d'étude — histoire du théâtre", "Caractériser le théâtre au Moyen Âge : théâtre et fête, théâtre et ville, formes populaires et savantes", 18),
            ("PREMIERE", "THEA1_OBJ_XVII_XVIII",         "Objets d'étude — histoire du théâtre", "Distinguer théâtre baroque et classique, tragédie, comédie et drame bourgeois aux XVIIe et XVIIIe siècles", 19),
            ("PREMIERE", "THEA1_OBJ_XIX",                "Objets d'étude — histoire du théâtre", "Caractériser le mélodrame, le spectaculaire, le drame romantique, le vaudeville et le boulevard au XIXe siècle", 20),
            ("PREMIERE", "THEA1_OBJ_MODERNE",            "Objets d'étude — histoire du théâtre", "Expliquer la crise du drame (Ibsen, Tchekhov) et l'apparition du metteur en scène", 21),
            ("PREMIERE", "THEA1_OBJ_BRECHT",             "Objets d'étude — histoire du théâtre", "Présenter Brecht et ses précurseurs, le théâtre politique et populaire du XXe siècle et le post-brechtien", 22),
            ("PREMIERE", "THEA1_OBJ_CONTEMPORAIN",       "Objets d'étude — histoire du théâtre", "Identifier dramaturgies contemporaines, écritures de plateau, théâtre documentaire et formes hybrides", 23),
            ("PREMIERE", "THEA1_EXPERIENCE_SENSIBLE",    "Spectateur et méthode", "Formuler son expérience sensible d'un spectacle, la partager et développer un point de vue personnel argumenté", 24),
            ("PREMIERE", "THEA1_ANALYSE_SPECTACLE",      "Spectateur et méthode", "Proposer une analyse orale ou écrite du travail de plateau et des spectacles vus", 25),
            ("PREMIERE", "THEA1_LIRE_TEXTE",             "Spectateur et méthode", "Lire un texte de théâtre en relation avec des questions de représentation", 26),
            ("PREMIERE", "THEA1_ARCHIVE",                "Spectateur et méthode", "Analyser une archive de spectacle en sachant qu'une captation documente le théâtre mais n'est pas le théâtre", 27),
            ("PREMIERE", "THEA1_PARCOURS_SPECTATEUR",    "Spectateur et méthode", "Rendre compte d'un parcours de spectateur d'au moins neuf spectacles variés dans l'année", 28),
            ("PREMIERE", "THEA1_LECTURES",               "Spectateur et méthode", "Lire des textes patrimoniaux et contemporains, dramatiques et théoriques, et en restituer les connaissances", 29),
            ("PREMIERE", "THEA1_CARNET_BORD",            "Carnet de bord", "Tenir un carnet de bord : projet, indications de jeu, synthèses, spectacles vus, recherches et lexique", 30),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE — THÉÂTRE (annexe 2 de MENE1921245A ; programme limitatif MENE2536492N, annexe 7)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] THEAT =
        {
            ("TERMINALE", "THEAT_PROJET_COLLECTIF",       "Pratique — Jeu et plateau", "S'engager avec rigueur dans un projet collectif et faire avancer le travail commun par ses connaissances et propositions", 1),
            ("TERMINALE", "THEAT_PROJET_PUBLIC",          "Pratique — Jeu et plateau", "Participer au jeu, et éventuellement à une autre composante, d'un projet théâtral présenté devant un public", 2),
            ("TERMINALE", "THEAT_SITUATION_NOUVELLE",     "Pratique — Jeu et plateau", "S'impliquer dans une situation de jeu nouvelle en mettant en œuvre une démarche de création adaptée", 3),
            ("TERMINALE", "THEAT_ANALYSER_PLATEAU",       "Pratique — Jeu et plateau", "Analyser son travail de plateau et celui de ses partenaires à l'aune de ses connaissances dramaturgiques", 4),
            ("TERMINALE", "THEAT_AUTONOMIE",              "Pratique — Jeu et plateau", "Développer son autonomie artistique et faire des propositions au plateau", 5),
            ("TERMINALE", "THEAT_CULTURE_AU_PLATEAU",     "Pratique — Jeu et plateau", "Mettre sa culture théâtrale et ses acquis théoriques au service de ses propositions scéniques", 6),
            ("TERMINALE", "THEAT_ENJEUX_ESTHETIQUES",     "Culture théâtrale", "Approcher les questions au programme dans leurs enjeux esthétiques et historiques", 7),
            ("TERMINALE", "THEAT_ENJEUX_SOCIAUX",         "Culture théâtrale", "Envisager les œuvres au programme dans leurs enjeux sociaux et le rapport du théâtre au monde qu'elles impliquent", 8),
            ("TERMINALE", "THEAT_CONTEXTE_ELARGI",        "Culture théâtrale", "Mettre les questions au programme en perspective dans un contexte théâtral et dramaturgique élargi", 9),
            ("TERMINALE", "THEAT_COMPOSANTES_SPECTACLE",  "Culture théâtrale", "Décrire les composantes d'un spectacle et repérer les enjeux d'une démarche théâtrale avec le vocabulaire du théâtre", 10),
            ("TERMINALE", "THEAT_PROCESSUS_CREATION",     "Culture théâtrale", "Réfléchir sur les processus de création et sur leur incidence sur le spectacle", 11),
            ("TERMINALE", "THEAT_DRAMATURGIE",            "Culture théâtrale", "Interroger la dramaturgie et les modes de composition d'un spectacle", 12),
            ("TERMINALE", "THEAT_EVENEMENT_PERFORMATIF",  "Culture théâtrale", "Observer la représentation comme un événement performatif, éphémère et unique, situé dans un temps, un lieu, devant un public", 13),
            ("TERMINALE", "THEAT_SITUER_DOCUMENT",        "Culture théâtrale", "Situer, décrire et analyser un texte dramatique ou théorique, une image, une captation ou un spectacle vu", 14),
            ("TERMINALE", "THEAT_ART_DEUX_TEMPS",         "Textes et représentation", "Expliquer l'approche d'une œuvre du répertoire comme « art à deux temps », entre sa création et ses reprises", 15),
            ("TERMINALE", "THEAT_MISE_EN_SCENE",          "Textes et représentation", "Présenter la mise en scène comme catégorie esthétique relativement récente et en comparer plusieurs", 16),
            ("TERMINALE", "THEAT_DRAMATURGIE_REPRESENTATION", "Textes et représentation", "Analyser la dramaturgie comme construction de la représentation : jeu, architecture, scénographie, lumière, son, vidéo", 17),
            ("TERMINALE", "THEAT_FORMES_PASSE",           "Un art au présent", "Analyser une forme théâtrale du passé dans la globalité de ses enjeux sociaux, variables selon les époques", 18),
            ("TERMINALE", "THEAT_RELATION_PUBLIC",        "Un art au présent", "Analyser un spectacle vu dans sa relation au public et dans ce qu'il met en jeu dans le monde où il apparaît", 19),
            ("TERMINALE", "THEAT_AUTRES_ARTS",            "Le théâtre et les autres arts", "Analyser les échanges du théâtre avec d'autres arts et entre culture savante et culture populaire", 20),
            ("TERMINALE", "THEAT_TYPES_ARCHIVES",         "L'archive théâtrale", "Distinguer les types d'archives vidéo (diffusion télévisée, captation témoin, teaser) et leurs usages", 21),
            ("TERMINALE", "THEAT_LIMITES_CAPTATION",      "L'archive théâtrale", "Montrer ce qu'une captation ne contient pas et comment cadrage et montage infléchissent la perception", 22),
            ("TERMINALE", "THEAT_SOURCES_DOCUMENTAIRES",  "L'archive théâtrale", "Mobiliser iconographie, témoignages, critiques, photographies et écrits d'artistes pour documenter un spectacle", 23),
            ("TERMINALE", "THEAT_EXPERIENCE_SENSIBLE",    "Spectateur et méthode", "Formuler son expérience sensible d'un spectacle, la partager, développer un point de vue argumenté et débattre", 24),
            ("TERMINALE", "THEAT_ANALYSE_SPECTACLE",      "Spectateur et méthode", "Proposer une analyse orale ou écrite du travail de plateau et des spectacles vus", 25),
            ("TERMINALE", "THEAT_LIRE_TEXTE",             "Spectateur et méthode", "Lire un texte de théâtre en relation avec des questions de représentation", 26),
            ("TERMINALE", "THEAT_PRISE_PAROLE",           "Spectateur et méthode", "Maîtriser la prise de parole publique", 27),
            ("TERMINALE", "THEAT_CARNET_BORD",            "Carnet de bord", "Tenir un carnet de bord de recherche et de création sur les objets du programme limitatif et les spectacles vus", 28),
            ("TERMINALE", "THEAT_PL27_CHAPEAU_PAILLE",    "Programme limitatif 2026-2027", "Analyser Un Chapeau de paille d'Italie de Labiche et Marc-Michel, mis en scène par Giorgio Barberio Corsetti (2012)", 29),
            ("TERMINALE", "THEAT_PL27_VOYAGES_ROMANESQUES", "Programme limitatif 2026-2027", "Comparer L'Autre Monde de Benjamin Lazar et Gulliver de Lesort et Hecq, deux adaptations de romans à la scène", 30),
            ("TERMINALE", "THEAT_PL27_PROPOSITION_PLATEAU", "Programme limitatif 2026-2027", "Rédiger une proposition justifiée pour le plateau sur une partie ou un aspect d'une œuvre au programme", 31),
            ("TERMINALE", "THEAT_PL27_PROPOSITION_JEU",   "Pratique — Programme limitatif 2026-2027", "Préparer une proposition de jeu sur un extrait de chacune des deux œuvres au programme et accepter un « re-jeu »", 32),
        };

        // -------------------------------------------------------------------------------------
        // 3. PREMIÈRE — ARTS DU CIRQUE (annexe de MENE1901567A, « Arts du cirque - classe de première »)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] CIRQUE1 =
        {
            ("PREMIERE", "CIRQUE1_DISCIPLINES",          "Pratique — Pratiquer", "Se spécialiser dans une ou deux disciplines choisies parmi les familles du cirque", 1),
            ("PREMIERE", "CIRQUE1_SECURITE",             "Pratique — Pratiquer", "Se préparer, réaliser figures ou prouesses et récupérer en préservant son intégrité physique et en sécurité", 2),
            ("PREMIERE", "CIRQUE1_GERER_RISQUES",        "Pratique — Pratiquer", "Gérer les risques inhérents à sa pratique pour se protéger tout en progressant", 3),
            ("PREMIERE", "CIRQUE1_PARAMETRES_CREATION",  "Pratique — Création collective", "Gérer les paramètres d'une création collective : intention, composition, scénographie", 4),
            ("PREMIERE", "CIRQUE1_DEFENDRE_CHOIX",       "Pratique — Création collective", "Exprimer, défendre et faire évoluer ses choix au sein d'un processus de création collective", 5),
            ("PREMIERE", "CIRQUE1_PROCEDES",             "Pratique — Création collective", "Utiliser des procédés de composition : reprise, imitation, synchronisation, faire avec, faire contre, improvisation", 6),
            ("PREMIERE", "CIRQUE1_PRESENTER_PUBLIC",     "Pratique — Création collective", "Présenter en public son travail au sein d'une création circassienne collective", 7),
            ("PREMIERE", "CIRQUE1_TROUVER_PLACE",        "Questionnement", "Répondre à la question « Comment trouver ma place dans une création collective ? » à partir de sa pratique", 8),
            ("PREMIERE", "CIRQUE1_QUATRE_FAMILLES",      "Culture circassienne", "Nommer les quatre familles de disciplines : pratiques acrobatiques, manipulations d'objets, jeu comique, dressage", 9),
            ("PREMIERE", "CIRQUE1_SITUER_PRATIQUE",      "Culture circassienne", "Situer sa pratique dans l'histoire des familles du cirque : acrobatie, manipulation d'objets, jeu comique", 10),
            ("PREMIERE", "CIRQUE1_HISTOIRE_DISCIPLINE",  "Culture circassienne", "Présenter l'histoire, le répertoire, les artistes et les compagnies liés à sa discipline", 11),
            ("PREMIERE", "CIRQUE1_VALEURS_SYMBOLIQUES",  "Culture circassienne", "Interroger les valeurs symboliques et les dimensions poétiques portées par une discipline de cirque", 12),
            ("PREMIERE", "CIRQUE1_FORMES_ESTHETIQUES",   "Culture circassienne", "Se repérer dans les formes et les esthétiques du cirque et dans ses échanges avec les autres arts", 13),
            ("PREMIERE", "CIRQUE1_DIMENSIONS_ETHIQUES",  "Culture circassienne", "Interroger les dimensions économiques, politiques et éthiques du cirque", 14),
            ("PREMIERE", "CIRQUE1_PROCESSUS_ARTISTE",    "Culture circassienne", "Étudier le processus de création d'un artiste pour enrichir sa propre démarche", 15),
            ("PREMIERE", "CIRQUE1_INTENTIONS_EFFETS",    "Spectateur et analyse", "Identifier les intentions et les effets recherchés par les artistes et leur réception par le public", 16),
            ("PREMIERE", "CIRQUE1_ANALYSE_ARGUMENTEE",   "Spectateur et analyse", "Construire une analyse personnelle argumentée d'un spectacle de cirque, à l'écrit comme à l'oral", 17),
            ("PREMIERE", "CIRQUE1_ANALYSE_REFLEXIVE",    "Spectateur et analyse", "Mener une analyse réflexive sur un mouvement, une œuvre ou un artiste en les resituant dans leurs contextes", 18),
            ("PREMIERE", "CIRQUE1_INSTITUTIONS",         "Institutions et métiers", "Décrire le contexte économique et social du cirque : structures, écoles, entreprises, compagnies, parcours de formation", 19),
            ("PREMIERE", "CIRQUE1_CAPTATION",            "Rendre compte", "Analyser sa prestation au sein du collectif, notamment à partir d'une captation vidéo", 20),
            ("PREMIERE", "CIRQUE1_CARNET_BORD",          "Rendre compte", "Tenir un carnet de bord, lieu de réflexion, de recherche, d'analyse et de jugement critique", 21),
            ("PREMIERE", "CIRQUE1_PROPOS_ETAYE",         "Rendre compte", "Déployer un propos étayé sous forme de note d'intention, d'exposé oral ou d'entretien avec un jury", 22),
            ("PREMIERE", "CIRQUE1_PORTFOLIO",            "Rendre compte", "Élaborer son « portfolio de compétences » et le faire valoir", 23),
        };

        // -------------------------------------------------------------------------------------
        // 4. TERMINALE — ARTS DU CIRQUE (annexe 2 de MENE1921245A ; programme limitatif MENE2536492N, annexe 1)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] CIRQUET =
        {
            ("TERMINALE", "CIRQUET_PERSONNALISER",       "Pratique — Pratiquer", "Personnaliser sa pratique d'une ou deux disciplines en référence aux artistes et aux œuvres", 1),
            ("TERMINALE", "CIRQUET_LANGAGE_CORPOREL",    "Pratique — Pratiquer", "Enrichir son langage corporel et les intentions attribuées aux figures et aux mouvements", 2),
            ("TERMINALE", "CIRQUET_RISQUE_MESURE",       "Pratique — Pratiquer", "S'engager corporellement et se dépasser avec une prise de risque mesurée", 3),
            ("TERMINALE", "CIRQUET_AUTONOMIE",           "Pratique — Pratiquer", "Gérer en autonomie la préparation, l'entraînement, la récupération et le matériel", 4),
            ("TERMINALE", "CIRQUET_SECURITE_CORPS",      "Pratique — Pratiquer", "Évoluer en sécurité selon les capacités et limites de son corps, avec réflexes pratiques et connaissances physiologiques", 5),
            ("TERMINALE", "CIRQUET_PRESENCE_JEU",        "Pratique — Pratiquer", "Travailler la recherche originale, la fluidité du mouvement, la présence et le jeu scénique", 6),
            ("TERMINALE", "CIRQUET_STYLE_PERSONNEL",     "Pratique — Processus de création", "Faire émerger une démarche et un style personnels à partir de l'analyse de son travail", 7),
            ("TERMINALE", "CIRQUET_SON_SCENOGRAPHIE",    "Pratique — Processus de création", "Intégrer univers sonore et musical et scénographie dans ses choix, étayés par une intention et un propos", 8),
            ("TERMINALE", "CIRQUET_ESPACE_RYTHME",       "Pratique — Processus de création", "Travailler l'espace, le rythme, l'énergie et la présence pour donner du sens à sa proposition", 9),
            ("TERMINALE", "CIRQUET_PRESTATION_PUBLIC",   "Pratique — Processus de création", "Concevoir, mettre en piste et présenter une prestation individuelle ou collective devant un public", 10),
            ("TERMINALE", "CIRQUET_SOURCES_INSPIRATION", "Processus de création", "Expliciter ses sources d'inspiration et défendre son processus créatif", 11),
            ("TERMINALE", "CIRQUET_ENTRER_EN_PISTE",     "Questionnement", "Répondre à « Pourquoi est-ce que j'entre en piste ? » : intentions, rapport au public, interprétation", 12),
            ("TERMINALE", "CIRQUET_ETAT_CORPS_JUSTE",    "Questionnement", "Expliquer la notion d'« état de corps juste », perçu par le spectateur comme naturel, évocateur et sincère", 13),
            ("TERMINALE", "CIRQUET_ESPACE_DE_JEU",       "Questionnement", "Choisir l'espace de jeu adapté à son propos, la piste désignant tout espace circassien et public", 14),
            ("TERMINALE", "CIRQUET_RECHERCHE_DOC",       "Analyse des œuvres", "Mener des recherches documentaires sur les œuvres étudiées et approfondir sa connaissance du répertoire", 15),
            ("TERMINALE", "CIRQUET_METTRE_EN_LIEN",      "Analyse des œuvres", "Mettre en lien un spectacle avec d'autres créations et identifier intentions, effets et réception", 16),
            ("TERMINALE", "CIRQUET_PROCESSUS_OEUVRES",   "Analyse des œuvres", "Analyser le processus de création d'œuvres de cirque et leurs dimensions poétiques ou symboliques", 17),
            ("TERMINALE", "CIRQUET_DEFINITION_CIRQUE",   "Analyse des œuvres", "Formuler une définition personnelle du cirque", 18),
            ("TERMINALE", "CIRQUET_VOCABULAIRE_REPERTOIRE", "Analyse des œuvres", "Maîtriser le vocabulaire, les références historiques et le répertoire de sa ou ses disciplines", 19),
            ("TERMINALE", "CIRQUET_CURSUS_METIERS",      "Institutions et métiers", "Présenter les formations supérieures et les métiers artistiques, techniques et administratifs du cirque", 20),
            ("TERMINALE", "CIRQUET_AUTRES_DOMAINES",     "Institutions et métiers", "Identifier d'autres domaines où investir ses compétences : Staps, médecine, sciences humaines, communication…", 21),
            ("TERMINALE", "CIRQUET_CARNET_BORD",         "Rendre compte", "Retracer dans son carnet de bord les phases de son acte créatif et l'apport des rencontres et spectacles", 22),
            ("TERMINALE", "CIRQUET_COURT_ESSAI",         "Rendre compte", "Déployer un propos argumenté sous forme de court essai, note d'intention, exposé oral ou entretien", 23),
            ("TERMINALE", "CIRQUET_PL27_JONGLAGES",      "Programme limitatif 2026-2027", "Analyser les jonglages, famille de disciplines au programme : histoire, répertoire, artistes", 24),
            ("TERMINALE", "CIRQUET_PL27_HOURVARI",       "Programme limitatif 2026-2027", "Analyser Hourvari, spectacle de la Compagnie Rasposo (2024)", 25),
            ("TERMINALE", "CIRQUET_PL27_PROPOSITION",    "Programme limitatif 2026-2027", "Rédiger une proposition personnelle de création de spectacle ou de numéro à partir d'un dossier, en la justifiant", 26),
        };
    }
}
