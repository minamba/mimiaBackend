namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// Les spécialités arts plastiques, histoire des arts et cinéma-audiovisuel de la voie générale — provenance détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielArtsVisuels
    {
        // =====================================================================================
        // RÉFÉRENTIELS DE LA SPÉCIALITÉ ARTS (VOIE GÉNÉRALE) — ARTS PLASTIQUES, HISTOIRE DES ARTS,
        // CINÉMA-AUDIOVISUEL. Programmes en vigueur pour l'année scolaire 2026-2027.
        // Compétences rédigées depuis les textes officiels téléchargés et lus le 14/09/2026, rien de
        // mémoire. Sources gardées dans specialites\sources\arts-a\.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau  | Niveau    | Intitulé exact (titre de la partie dans l'annexe)                  |
        // |----------|-----------|--------------------------------------------------------------------|
        // | ARTSPLA1 | PREMIERE  | « Arts plastiques - classe de première »                           |
        // | ARTSPLAT | TERMINALE | « Arts plastiques - classe terminale »                             |
        // | HDA1     | PREMIERE  | « Histoire des arts - classe de première »                         |
        // | HDAT     | TERMINALE | « Histoire des arts - classe terminale »                           |
        // | CAV1     | PREMIERE  | « Cinéma-audiovisuel - classe de première »                        |
        // | CAVT     | TERMINALE | « Cinéma-audiovisuel - classe terminale »                          |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901567A, BO spécial n° 1 du
        //   22 janvier 2019, « Programme d'enseignement de spécialité d'arts des classes de première et
        //   terminale de la voie générale ». Article 2 lu : « entrent en vigueur à la rentrée scolaire 2019
        //   pour la classe de première et à la rentrée 2020 pour la classe terminale ».
        //   Annexe unique « Programme de spécialité d'arts de première et terminale générales » (sept
        //   parties : cirque, arts plastiques, CAV, danse, histoire des arts, musique, théâtre). Lues :
        //   arts plastiques (p. 247-1425 du texte extrait), CAV (1426-1747), histoire des arts (2051-2365).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901567A.htm
        //   Annexe (copie éduscol lue) : https://eduscol.education.gouv.fr/sites/default/files/document/spe567annexe22-11063846pdf-83196.pdf
        //
        // TERMINALE : arrêté du 19-7-2019, J.O. du 23-7-2019, NOR MENE1921245A, BO spécial n° 8 du
        //   25 juillet 2019, « Programme de l'enseignement de spécialité d'arts des classes de première et
        //   terminale de la voie générale : modification ». Article 1 lu : l'annexe de l'arrêté du
        //   17-1-2019 « est complétée par l'annexe 2 » fixant le programme de terminale. L'entrée en
        //   vigueur est donc celle de l'arrêté de 2019 modifié : rentrée 2020 pour la terminale.
        //   Annexe 2 « Programme de spécialité d'arts de terminale générale ». Lues : arts plastiques
        //   (l. 276-956), CAV (957-1416), histoire des arts (1813-2087).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921245A.htm
        //   Annexe 2 : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/02/6/spe245_annexe_1159026.pdf
        //
        // PROGRAMMES LIMITATIFS DE TERMINALE (session 2027), tous trois « D'après le BOENJS n° 4 du
        //   22 janvier 2026 », lus dans les copies PDF éduscol (le NOR de cette note n'a PAS pu être établi :
        //   page BO non retrouvée).
        //   - Annexe 2 : arts plastiques, corpus « à compter de la rentrée scolaire 2026 » ; abroge la note
        //     du 4-12-2023 (MENE2330071N, BO n° 1 du 4-1-2024). progrlimitatif-spe-artpla-tleg-rs2026pdf-125603.pdf
        //   - Annexe 3 : cinéma-audiovisuel, « pour l'année scolaire 2026-2027 ».
        //     progrlimitatif-spe-cav-tleg-annee2026-2027pdf-125606.pdf
        //   - Annexe 5 : histoire des arts, « à compter de la rentrée scolaire 2026 ».
        //     progrlimitatif-spe-hida-tlegrs2026pdf-125612.pdf
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 : les trois pages éduscol « Programmes et ressources en … - voie GT »
        //   (arts plastiques 5772 et histoire des arts 5796 datées « janvier 2026 », cinéma-audiovisuel 5775
        //   datée « juillet 2026 ») listent sous « Programmes en vigueur » les deux mêmes textes de spécialité
        //   (BO spécial n° 1 du 22-1-2019 et BO spécial n° 8 du 25-7-2019) et rien d'autre. Le titre Légifrance
        //   de l'arrêté (JORFTEXT000038029389) est inchangé. Aucun arrêté modificatif postérieur à juillet 2019
        //   n'a été trouvé (sondage des BO spéciaux 2019, pages éduscol, Légifrance, le 14/09/2026). Les
        //   changements récents portent sur les ÉPREUVES et les programmes limitatifs, pas sur les programmes.
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - Il n'y a pas un texte par enseignement : un seul arrêté porte les sept arts en 1re, et un
        //     arrêté modificatif ajoute l'annexe 2 de terminale.
        //   - Arts plastiques Tle : le programme ne réécrit pas les questionnements ; il n'ajoute que des
        //     « repères et points d'appui ». ARTSPLAT reprend ces ajouts, les exigences propres à la terminale
        //     (écrit, carnet de travail, approche diachronique) et le corpus limitatif de la rentrée 2026.
        //   - Histoire des arts et CAV Tle : une partie du tableau dépend du programme limitatif (lignes QL_
        //     et PL_), valable pour la session 2027. Les lignes CAVT_PL_ sont À RENOUVELER chaque année
        //     (programme renouvelé par tiers) ; ARTSPLAT_QL_ et HDAT_QL_ à chaque nouvelle note.
        //   - Arts plastiques 1re : les compétences travaillées et les attendus sont communs au cycle ; on
        //     ne les répète pas en Tle, sauf ceux que la terminale met en avant.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   ARTSPLA1 45 · ARTSPLAT 40 · HDA1 41 · HDAT 40 · CAV1 38 · CAVT 39 · total 243
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — ARTS PLASTIQUES (annexe de MENE1901567A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] ARTSPLA1 =
        {
            ("PREMIERE", "ARTSPLA1_PRAT_LANGAGES",       "Pratique — Expérimenter, produire, créer", "Choisir, expérimenter et maîtriser des langages et des moyens plastiques variés dans tous les champs de la pratique", 1),
            ("PREMIERE", "ARTSPLA1_PRAT_NUMERIQUE",      "Pratique — Expérimenter, produire, créer", "Recourir à des outils numériques de captation et de production à des fins de création artistique", 2),
            ("PREMIERE", "ARTSPLA1_PRAT_DOCUMENTATION",  "Pratique — Expérimenter, produire, créer", "Exploiter des informations et une documentation, notamment iconique, pour servir un projet de création", 3),
            ("PREMIERE", "ARTSPLA1_PRAT_2D3D",           "Pratique — Expérimenter, produire, créer", "Proposer des réponses plastiques en deux et en trois dimensions à un questionnement artistique", 4),
            ("PREMIERE", "ARTSPLA1_PROJET_CONCEVOIR",    "Pratique — Projet artistique", "Concevoir, réaliser et donner à voir un projet artistique individuel ou collectif", 5),
            ("PREMIERE", "ARTSPLA1_PROJET_REORIENTER",   "Pratique — Projet artistique", "Confronter intention et réalisation pour adapter et réorienter un projet", 6),
            ("PREMIERE", "ARTSPLA1_QUEST_ANALYSER",      "Questionner le fait artistique", "Analyser et interpréter une pratique, une démarche, une œuvre", 7),
            ("PREMIERE", "ARTSPLA1_QUEST_SITUER",        "Questionner le fait artistique", "Situer des œuvres dans l'espace et dans le temps en se repérant dans les domaines liés aux arts plastiques", 8),
            ("PREMIERE", "ARTSPLA1_QUEST_AUTEUR_SPECT",  "Questionner le fait artistique", "Interroger une œuvre ou une démarche du point de vue de l'auteur et de celui du spectateur", 9),
            ("PREMIERE", "ARTSPLA1_EXPO_CONDITIONS",     "Exposer l'œuvre, la démarche, la pratique", "Prendre en compte les conditions de présentation et de réception d'une production dès sa conception", 10),
            ("PREMIERE", "ARTSPLA1_EXPO_DIRE",           "Exposer l'œuvre, la démarche, la pratique", "Dire sa démarche et sa pratique, écouter et accepter des avis divers et contradictoires", 11),
            ("PREMIERE", "ARTSPLA1_PRATIQUES_TYPES",     "Types de pratiques", "Distinguer les quatre types de pratiques : bidimensionnelles, tridimensionnelles, image fixe et animée, numérique", 12),
            // Champ des questionnements plasticiens (au moins 75 % du temps annuel)
            ("PREMIERE", "ARTSPLA1_REPR_DESSIN_STATUTS", "La représentation", "Expliquer la diversité des statuts et finalités du dessin : observer, projeter un projet, créer", 13),
            ("PREMIERE", "ARTSPLA1_REPR_DESSIN_OUTILS",  "La représentation", "Décrire les outils du dessin conventionnels, inventés ou détournés et l'extension du dessin à l'espace et au paysage", 14),
            ("PREMIERE", "ARTSPLA1_REPR_REEL_ECART",     "La représentation", "Expliquer le rapport au réel : mimesis, ressemblance, vraisemblance et valeur expressive de l'écart", 15),
            ("PREMIERE", "ARTSPLA1_REPR_CORPS",          "La représentation", "Comparer des partis pris de représentation du corps et leurs enjeux éthiques (stéréotypes, tabous)", 16),
            ("PREMIERE", "ARTSPLA1_REPR_ESPACE",         "La représentation", "Distinguer des conceptions de la représentation de l'espace : systèmes perspectifs et non perspectifs", 17),
            ("PREMIERE", "ARTSPLA1_FIG_ESPACES",         "La figuration et l'image, la non-figuration", "Analyser les espaces narratifs d'une image figurative et ses dialogues avec le support, l'écrit, l'oral", 18),
            ("PREMIERE", "ARTSPLA1_FIG_TEMPS",           "La figuration et l'image, la non-figuration", "Expliquer comment une narration figurée suggère temps et mouvement, de la fresque aux dispositifs multimédias", 19),
            ("PREMIERE", "ARTSPLA1_FIG_NONFIGURATION",   "La figuration et l'image, la non-figuration", "Caractériser le passage à la non-figuration : perte du référent, systèmes non figuratifs (couleur, trace, signe)", 20),
            ("PREMIERE", "ARTSPLA1_MAT_PROPRIETES",      "La matière, les matériaux et la matérialité", "Décrire les propriétés physiques et sensibles des matériaux et leur transformation dans une œuvre", 21),
            ("PREMIERE", "ARTSPLA1_MAT_COULEUR",         "La matière, les matériaux et la matérialité", "Expliquer la couleur comme matériau de l'œuvre : matière colorée, perception, rapport à l'espace", 22),
            ("PREMIERE", "ARTSPLA1_MAT_REEL",            "La matière, les matériaux et la matérialité", "Expliquer l'intégration du réel dans l'œuvre : collage, matériaux non artistiques, ready-made", 23),
            ("PREMIERE", "ARTSPLA1_MAT_LUMIERE",         "La matière, les matériaux et la matérialité", "Expliquer les usages de la lumière, naturelle ou artificielle, comme matériau ou comme médium exclusif", 24),
            ("PREMIERE", "ARTSPLA1_MAT_AUTHENTICITE",    "La matière, les matériaux et la matérialité", "Discuter matérialité, immatérialité et authenticité d'une œuvre", 25),
            ("PREMIERE", "ARTSPLA1_PRES_DISPOSITIFS",    "La présentation de l'œuvre", "Comparer les dispositifs traditionnels (cadre, socle, cimaise) et contemporains de présentation d'une œuvre", 26),
            ("PREMIERE", "ARTSPLA1_PRES_SPECTATEUR",     "La présentation de l'œuvre", "Expliquer comment l'artiste ou le commissaire sollicite le spectateur : sens, corps, participation", 27),
            ("PREMIERE", "ARTSPLA1_MONS_ATELIER",        "La monstration et la diffusion de l'œuvre", "Distinguer l'atelier, la monstration entre pairs et l'exposition dans des espaces spécialisés", 28),
            ("PREMIERE", "ARTSPLA1_MONS_EXPOSITION",     "La monstration et la diffusion de l'œuvre", "Décrire les étapes et les langages de la conception d'une exposition, d'une édition ou d'une diffusion numérique", 29),
            ("PREMIERE", "ARTSPLA1_RECEP_PUBLIC",        "La réception par un public", "Expliquer les formes de monstration vers un large public : imprimé, objet, écran, accès en ligne", 30),
            ("PREMIERE", "ARTSPLA1_RECEP_ECRITS",        "La réception par un public", "Identifier le rôle des écrits autour de l'œuvre : titres, cartels, déclarations d'intention, catalogues", 31),
            ("PREMIERE", "ARTSPLA1_IDEE_PROJET",         "L'idée, la réalisation et le travail de l'œuvre", "Expliquer le passage du projet à l'œuvre : dessins préparatoires, maquettes, simulations numériques", 32),
            ("PREMIERE", "ARTSPLA1_IDEE_PROCESSUS",      "L'idée, la réalisation et le travail de l'œuvre", "Expliquer l'œuvre comme projet : improvisation, éphémère, trace, dépassement du prévu", 33),
            ("PREMIERE", "ARTSPLA1_COLLECTIF",           "Créer à plusieurs plutôt que seul", "Décrire les formes de création à plusieurs : atelier collectif, artiste concepteur et assistants, collectif", 34),
            // Champs interdisciplinaire et transversal
            ("PREMIERE", "ARTSPLA1_INTER_ARCHITECTURE",  "Interdisciplinaire — architecture, paysage, design", "Relier partis pris, formes, matériaux et usages d'une architecture, d'un paysage ou d'un objet de design", 35),
            ("PREMIERE", "ARTSPLA1_INTER_IMAGES_ANIMEES","Interdisciplinaire — cinéma, animation, jeu vidéo", "Analyser les espaces de diffusion des images animées : projections, écrans, immersion, interaction", 36),
            ("PREMIERE", "ARTSPLA1_INTER_THEATRALISER",  "Interdisciplinaire — théâtre, danse, musique", "Expliquer la théâtralisation d'une œuvre : mise en espace, mise en scène, place du public", 37),
            ("PREMIERE", "ARTSPLA1_TRANS_SOCIETE",       "Transversal — l'artiste et la société", "Analyser comment un artiste fait œuvre face à l'histoire et à la politique : engagement, archives, mémoire", 38),
            ("PREMIERE", "ARTSPLA1_TRANS_SCIENCES",      "Transversal — art, sciences et technologies", "Expliquer les dialogues entre art, sciences et technologies : artiste chercheur, collaborations", 39),
            ("PREMIERE", "ARTSPLA1_TRANS_MONDIAL",       "Transversal — mondialisation de la création", "Expliquer métissages et hybridations dans la mondialisation de la création artistique", 40),
            // Culture artistique, analyse, attendus
            ("PREMIERE", "ARTSPLA1_ANALYSE_METHODE",     "Analyse d'œuvres", "Décrire une œuvre avec un vocabulaire précis et organiser l'analyse : sujet, couleur, composition, spatialité", 41),
            ("PREMIERE", "ARTSPLA1_CULTURE_RELIER",      "Culture artistique", "Mettre en relation des œuvres d'époques et d'aires géographiques différentes pour en apprécier le sens", 42),
            ("PREMIERE", "ARTSPLA1_ATT_XXE",             "Culture artistique", "Caractériser des repères essentiels d'œuvres et de démarches du champ des arts plastiques au XXe siècle", 43),
            ("PREMIERE", "ARTSPLA1_ATT_TECHNIQUES",      "Culture artistique", "Situer une œuvre dans son contexte en mesurant l'impact des innovations techniques sur la création", 44),
            ("PREMIERE", "ARTSPLA1_ATT_ORAL_DEMARCHE",   "Exposer l'œuvre, la démarche, la pratique", "Présenter oralement les intentions de sa production et justifier les moyens plastiques choisis", 45),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE — ARTS PLASTIQUES (annexe 2 de MENE1921245A + corpus limitatif BOENJS n° 4 du 22-1-2026)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] ARTSPLAT =
        {
            // Repères et points d'appui ajoutés en terminale
            ("TERMINALE", "ARTSPLAT_REPR_CORPS_DESSIN",   "La représentation", "Expliquer la relation du corps au dessin : geste, instrument, trace, apports des machines et du numérique", 1),
            ("TERMINALE", "ARTSPLAT_REPR_ARTS_MONDE",     "La représentation", "Comparer les conceptions de l'espace et du corps dans les arts du monde : associations, métissages", 2),
            ("TERMINALE", "ARTSPLAT_FIG_RHETORIQUE",      "La figuration et l'image, la non-figuration", "Identifier les rhétoriques de l'image figurative : symbolisation, allégorie, métaphore, métonymie, synecdoque", 3),
            ("TERMINALE", "ARTSPLAT_FIG_ABSTRACTION",     "La figuration et l'image, la non-figuration", "Définir l'abstraction : stylisation, autoréférentialité, modernité, traditions occidentales et autres cultures", 4),
            ("TERMINALE", "ARTSPLAT_MAT_EXPRESSIVE",      "La matière, les matériaux et la matérialité", "Expliquer la valeur expressive des matériaux et la primauté du langage plastique des matériaux", 5),
            ("TERMINALE", "ARTSPLAT_MAT_EXTENSION",       "La matière, les matériaux et la matérialité", "Expliquer l'extension de la notion de matériau : données numériques, sons, gestes, lumière, mots, idées", 6),
            ("TERMINALE", "ARTSPLAT_MAT_HAPPENING",       "La matière, les matériaux et la matérialité", "Analyser pratiques sociales, événements, rites et happenings comme sujets et moyens d'expression des œuvres", 7),
            ("TERMINALE", "ARTSPLAT_PRES_INSITU",         "La présentation de l'œuvre", "Expliquer les pratiques de l'in situ et du ready-made au regard du lieu de présentation", 8),
            ("TERMINALE", "ARTSPLAT_MONS_EPHEMERE",       "La monstration et la diffusion de l'œuvre", "Analyser les monstrations éphémères et en lieux non spécialisés : espace naturel ou public, biennales, festivals", 9),
            ("TERMINALE", "ARTSPLAT_MONS_COMMISSAIRES",   "La monstration et la diffusion de l'œuvre", "Décrire l'évolution des concepteurs d'exposition : artistes commissaires et commissaires-auteurs", 10),
            ("TERMINALE", "ARTSPLAT_RECEP_MEDIATION",     "La réception par un public", "Identifier les outils d'une médiation : plan de salle, texte, visite commentée, récit, atelier", 11),
            ("TERMINALE", "ARTSPLAT_RECEP_ESPACES",       "La réception par un public", "Comparer espaces et temporalités de l'exposition : white cube, black box, espace virtuel, réalité augmentée", 12),
            ("TERMINALE", "ARTSPLAT_IDEE_DEVENIR",        "L'idée, la réalisation et le travail de l'œuvre", "Expliquer le devenir d'un projet artistique : inachèvement, transformation, réemploi, accident, recréation", 13),
            ("TERMINALE", "ARTSPLAT_COLLECTIF_ECONOMIE",  "Créer à plusieurs plutôt que seul", "Décrire l'économie de la production collective : collectif d'artistes, réseau, FabLab, ateliers partagés", 14),
            ("TERMINALE", "ARTSPLAT_INTER_APPROFONDIR",   "Interdisciplinaire", "Approfondir les liens des arts plastiques avec l'architecture et le design, le cinéma et le jeu vidéo, la scène", 15),
            ("TERMINALE", "ARTSPLAT_TRANS_APPROFONDIR",   "Transversal", "Approfondir l'artiste et la société, l'art et les sciences, la mondialisation de la création", 16),
            // Culture, analyse et écrit (exigences de terminale)
            ("TERMINALE", "ARTSPLAT_CULT_DIACHRONIE",     "Culture artistique", "Situer une œuvre dans les grandes périodes qui organisent l'histoire de l'art", 17),
            ("TERMINALE", "ARTSPLAT_ANALYSE_GROUPEMENT",  "Analyse d'œuvres", "Conduire l'analyse méthodique d'un groupement d'œuvres constitué de manière synchronique ou diachronique", 18),
            ("TERMINALE", "ARTSPLAT_ECRIT_REFLEXION",     "Écrit — culture plastique et artistique", "Rédiger une réflexion argumentée sur un aspect de la création, étayée de références précises et situées", 19),
            ("TERMINALE", "ARTSPLAT_ECRIT_COMMENTAIRE",   "Écrit — culture plastique et artistique", "Rédiger le commentaire critique d'un document sur l'art en lien avec un questionnement transversal", 20),
            ("TERMINALE", "ARTSPLAT_ECRIT_NOTE_EXPO",     "Écrit — culture plastique et artistique", "Rédiger la note d'intention d'un projet d'exposition d'une œuvre, accompagnée de schémas et croquis", 21),
            ("TERMINALE", "ARTSPLAT_ECRITS_PROS",         "Culture artistique", "Exploiter des écrits professionnels sur l'art en lien avec sa pratique et ses projets", 22),
            // Œuvres, thèmes et questions de référence (à compter de la rentrée 2026, session 2027)
            ("TERMINALE", "ARTSPLAT_QL_VERNET",           "Questions limitatives — documenter ou augmenter le réel", "Analyser La ville et la rade de Toulon de Joseph Vernet (1756) comme manière de documenter le réel", 23),
            ("TERMINALE", "ARTSPLAT_QL_GURSKY",           "Questions limitatives — documenter ou augmenter le réel", "Analyser 99 Cent d'Andreas Gursky (1999) comme manière de documenter ou d'augmenter le réel", 24),
            ("TERMINALE", "ARTSPLAT_QL_REEL_ECART",       "Questions limitatives — documenter ou augmenter le réel", "Comparer Vernet et Gursky sur le rapport au réel : mimèsis, vraisemblance, valeur expressive de l'écart", 25),
            ("TERMINALE", "ARTSPLAT_QL_BOURGEOIS",        "Questions limitatives — espaces physiques et symboliques", "Analyser Maman de Louise Bourgeois (1999) comme expérience des espaces physiques et symboliques de l'œuvre", 26),
            ("TERMINALE", "ARTSPLAT_QL_GIAMBOLOGNA",      "Questions limitatives — espaces physiques et symboliques", "Analyser le Colosse de l'Apennin de Giambologna (1579-1583) : échelle, roche et enduits, chambres intérieures", 27),
            ("TERMINALE", "ARTSPLAT_QL_MATERIAUX",        "Questions limitatives — questionnements", "Expliquer propriétés et transformations des matériaux dans les œuvres du corpus de référence", 28),
            ("TERMINALE", "ARTSPLAT_QL_MONSTRATION",      "Questions limitatives — questionnements", "Relier les œuvres du corpus à leurs contextes de monstration : lieux, situations, publics", 29),
            ("TERMINALE", "ARTSPLAT_QL_PROJET",           "Questions limitatives — questionnements", "Expliquer le passage du projet à la production artistique à partir des œuvres du corpus", 30),
            ("TERMINALE", "ARTSPLAT_QL_USAGES",           "Questions limitatives — questionnements", "Relier les œuvres du corpus à l'environnement et aux usages de l'œuvre ou de l'objet", 31),
            ("TERMINALE", "ARTSPLAT_QL_MONDIALISATION",   "Questions limitatives — questionnements", "Discuter, à partir du corpus, la mondialisation de la création : métissages ou relativité des cultures du monde", 32),
            // Pratique, projet, oral
            ("TERMINALE", "ARTSPLAT_PRAT_PROJET_ABOUTI",  "Pratique — Projet artistique", "Conduire un projet abouti à visée artistique, de l'intention à la réalisation finale", 33),
            ("TERMINALE", "ARTSPLAT_PRAT_DOSSIER",        "Pratique — Projet artistique", "Constituer le dossier d'un projet : esquisses, réalisations préparatoires, traces des étapes, captations", 34),
            ("TERMINALE", "ARTSPLAT_PRAT_CARNET",         "Pratique — Carnet de travail", "Tenir un carnet de travail personnel témoignant des projets, expériences, rencontres et références de l'année", 35),
            ("TERMINALE", "ARTSPLAT_PRAT_ALEA",           "Pratique — Expérimenter, produire, créer", "Réajuster la conduite de son travail en tirant parti de l'aléa, de l'accident, de la découverte", 36),
            ("TERMINALE", "ARTSPLAT_ORAL_PRESENTER",      "Oral — pratique et culture plastiques", "Présenter un projet abouti : motivations du choix, intentions, démarche, moyens et processus mobilisés", 37),
            ("TERMINALE", "ARTSPLAT_ORAL_RETOUR_CRITIQUE","Oral — pratique et culture plastiques", "Porter un retour critique sur son projet et situer les influences et esthétiques qui l'ont nourri", 38),
            ("TERMINALE", "ARTSPLAT_ATT_REFERENCES",      "Questionner le fait artistique", "Identifier les références implicites de son travail au regard des pratiques artistiques présentes et passées", 39),
            ("TERMINALE", "ARTSPLAT_EXPO_PROJET",         "Exposer l'œuvre, la démarche, la pratique", "Créer, seul ou à plusieurs, les conditions d'un projet d'exposition pour un public", 40),
        };

        // -------------------------------------------------------------------------------------
        // 3. PREMIÈRE — HISTOIRE DES ARTS (annexe de MENE1901567A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HDA1 =
        {
            // Compétences travaillées
            ("PREMIERE", "HDA1_ESTH_LIEUX",              "Compétences d'ordre esthétique", "Décrire les codes d'un lieu artistique ou patrimonial fréquenté : musée, monument, salle de spectacle", 1),
            ("PREMIERE", "HDA1_ESTH_RAISON_EMOTION",     "Compétences d'ordre esthétique", "Exprimer ce qu'une œuvre fait ressentir et le relier à des arguments raisonnés", 2),
            ("PREMIERE", "HDA1_METH_FORME_FONCTION",     "Compétences d'ordre méthodologique", "Expliquer les liens entre la forme d'une œuvre et son format, son matériau, sa fonction, sa charge symbolique", 3),
            ("PREMIERE", "HDA1_METH_TYPES_EXPRESSION",   "Compétences d'ordre méthodologique", "Distinguer des types d'expression artistique par leurs particularités matérielles, formelles, temporelles et spatiales", 4),
            ("PREMIERE", "HDA1_METH_REPRODUCTION",       "Compétences d'ordre méthodologique", "Distinguer la présence d'une œuvre de l'image qu'en donne une reproduction, une captation ou un enregistrement", 5),
            ("PREMIERE", "HDA1_CULT_EMBLEMATIQUE",       "Compétences d'ordre culturel", "Présenter une œuvre emblématique du patrimoine mondial : genèse, codes, réception, raisons de son actualité", 6),
            ("PREMIERE", "HDA1_CULT_REPERES",            "Compétences d'ordre culturel", "Situer des œuvres par des repères d'histoire et de géographie des civilisations : ruptures, continuités, circulations", 7),
            ("PREMIERE", "HDA1_CULT_VOCABULAIRE",        "Compétences d'ordre culturel", "Employer un vocabulaire technique et formel propre aux différents arts", 8),
            // Cinq modalités pour saisir une œuvre
            ("PREMIERE", "HDA1_MOD_CONDITIONS",          "Saisir une œuvre — cinq modalités", "Analyser une œuvre selon ses conditions concrètes de réalisation", 9),
            ("PREMIERE", "HDA1_MOD_AUTEUR",              "Saisir une œuvre — cinq modalités", "Présenter l'auteur d'une œuvre, ou son anonymat, ou son caractère collectif", 10),
            ("PREMIERE", "HDA1_MOD_CONTEXTE",            "Saisir une œuvre — cinq modalités", "Replacer une œuvre dans son contexte socio-historique de création, commande ou marché compris", 11),
            ("PREMIERE", "HDA1_MOD_CIRCULATION",         "Saisir une œuvre — cinq modalités", "Retracer la diffusion et la circulation d'une œuvre, de son apparition à sa situation actuelle", 12),
            ("PREMIERE", "HDA1_MOD_RECEPTION",           "Saisir une œuvre — cinq modalités", "Expliquer la réception passée et présente d'une œuvre et l'influence des jugements des générations antérieures", 13),
            // Compétences acquises au cycle terminal
            ("PREMIERE", "HDA1_CYCLE_ANALYSE_FORMELLE",  "Analyse des œuvres", "Comparer des œuvres de natures diverses par l'analyse formelle et sémantique : rythme, couleurs, texture, ornement", 14),
            ("PREMIERE", "HDA1_CYCLE_PARENTES",          "Analyse des œuvres", "Rattacher une œuvre à un artiste, un courant, un langage ou une époque par ses parentés stylistiques", 15),
            ("PREMIERE", "HDA1_CYCLE_PATRIMOINE",        "Analyse des œuvres", "Relier le patrimoine de proximité au patrimoine mondial à l'aide de références acquises", 16),
            ("PREMIERE", "HDA1_CYCLE_COPIE_REMPLOI",     "Analyse des œuvres", "Distinguer dans une œuvre ce qui est singulier de ce qui relève de la copie, du remploi ou de la reprise", 17),
            ("PREMIERE", "HDA1_SOURCES",                 "Sources et documentation", "Croiser et hiérarchiser des sources : livres, articles, ressources numériques", 18),
            // Six études thématiques
            ("PREMIERE", "HDA1_T1_PRODUCTION",           "Thème I — matières, techniques et formes", "Relier matières et techniques employées aux formes d'une œuvre", 19),
            ("PREMIERE", "HDA1_T1_UNIQUE_MULTIPLE",      "Thème I — matières, techniques et formes", "Expliquer ce qui distingue production et reproduction d'une œuvre unique ou multiple", 20),
            ("PREMIERE", "HDA1_T2_ARTISTE",              "Thème II — l'artiste", "Caractériser le statut d'un créateur : individuel, collectif ou anonyme", 21),
            ("PREMIERE", "HDA1_T3_LIEUX",                "Thème III — les lieux de l'art", "Retracer l'histoire et l'organisation d'un musée, d'une institution ou d'un événement artistique", 22),
            ("PREMIERE", "HDA1_T3_LIMITES",              "Thème III — les lieux de l'art", "Discuter les limites des musées, institutions et événements artistiques", 23),
            ("PREMIERE", "HDA1_T4_RECEPTION",            "Thème IV — la réception de l'art", "Distinguer le rôle des commanditaires, des critiques, du public et de la postérité dans la réception d'une œuvre", 24),
            ("PREMIERE", "HDA1_T5_MARCHE",               "Thème V — la valeur économique de l'art", "Expliquer le fonctionnement du marché de l'art, de ses lieux et de ses acteurs", 25),
            ("PREMIERE", "HDA1_T6_CIRCULATION",          "Thème VI — circulation et échanges", "Expliquer la circulation des œuvres et les échanges artistiques entre époques et aires culturelles", 26),
            ("PREMIERE", "HDA1_THEMES_DOMAINES",         "Études thématiques", "Mobiliser des exemples pris dans les arts visuels, l'architecture et l'urbanisme, la musique ou la danse", 27),
            // Projet collectif (15 à 20 heures)
            ("PREMIERE", "HDA1_PROJ_RESPONSABILITE",     "Pratique — Projet collectif", "Tenir un rôle propre dans un projet de classe fondé sur un lieu ou une institution culturelle de proximité", 28),
            ("PREMIERE", "HDA1_PROJ_EXPOSITION",         "Pratique — Projet collectif", "Contribuer à une exposition matérielle ou virtuelle : conception, scénographie, notices de catalogue, cartels", 29),
            ("PREMIERE", "HDA1_PROJ_MEDIATEUR",          "Projet collectif", "Présenter à un public varié une œuvre, un monument ou un bâtiment en médiateur", 30),
            ("PREMIERE", "HDA1_PROJ_DEBAT",              "Projet collectif", "Argumenter dans un débat sur le patrimoine ou sur des œuvres d'art", 31),
            ("PREMIERE", "HDA1_PROJ_METIERS",            "Projet collectif", "Décrire des métiers rencontrés : conservation, restauration, archéologie, scénographie d'exposition, muséographie", 32),
            // Situations d'enseignement obligatoires
            ("PREMIERE", "HDA1_OBLIG_OEUVRE_ORIGINALE",  "Pratique — Contact direct avec les œuvres", "Étudier de manière approfondie une œuvre d'art visuel originale, devant elle et non en reproduction", 33),
            ("PREMIERE", "HDA1_OBLIG_ARCHITECTURE",      "Pratique — Contact direct avec les œuvres", "Visiter un bâtiment ou un ensemble architectural et en rendre compte", 34),
            ("PREMIERE", "HDA1_OBLIG_SPECTACLE",         "Pratique — Contact direct avec les œuvres", "Assister à un spectacle ou à un concert et en rendre compte", 35),
            // Évaluation et exercices
            ("PREMIERE", "HDA1_EVAL_COMMENTAIRE",        "Exercices et évaluation", "Rédiger ou exposer un commentaire organisé d'une œuvre ou d'une production artistique", 36),
            ("PREMIERE", "HDA1_EVAL_COMMENTAIRE_GUIDE",  "Exercices et évaluation", "Rédiger un commentaire guidé mettant en relation un ensemble restreint d'œuvres et de documents", 37),
            ("PREMIERE", "HDA1_EVAL_ORAL_SANS_NOTES",    "Exercices et évaluation", "Prendre la parole sans support écrit devant un groupe à partir d'une œuvre ou d'une thématique", 38),
            ("PREMIERE", "HDA1_EVAL_DISSERTATION",       "Exercices et évaluation", "Construire une dissertation articulée avec les thématiques du programme", 39),
            ("PREMIERE", "HDA1_EVAL_JUGEMENT",           "Exercices et évaluation", "Formuler un jugement esthétique et critique argumenté", 40),
            ("PREMIERE", "HDA1_CARNET_BORD",             "Carnet de bord", "Constituer un carnet de bord réunissant notes, recherches, comptes rendus d'expériences et documents choisis", 41),
        };

        // -------------------------------------------------------------------------------------
        // 4. TERMINALE — HISTOIRE DES ARTS (annexe 2 de MENE1921245A + programme limitatif BOENJS n° 4 du 22-1-2026)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HDAT =
        {
            // Trois thématiques du programme
            ("TERMINALE", "HDAT_ARTISTE_COMMANDE",        "Un artiste en son temps", "Expliquer les conditions de la commande, de la pratique et de la réception d'une œuvre dans une période donnée", 1),
            ("TERMINALE", "HDAT_ARTISTE_FORTUNE",         "Un artiste en son temps", "Retracer la présentation, la circulation et la fortune critique des œuvres et des idées d'un artiste", 2),
            ("TERMINALE", "HDAT_ARTISTE_STYLE",           "Un artiste en son temps", "Identifier une œuvre et un style et les situer dans l'histoire et la théorie des arts", 3),
            ("TERMINALE", "HDAT_ARTISTE_CONTEMPORAINS",   "Un artiste en son temps", "Relier un artiste aux expressions artistiques et littéraires de son temps, à son héritage et à sa postérité", 4),
            ("TERMINALE", "HDAT_VILLE_URBANISME",         "Arts, ville, politique et société", "Identifier une politique urbaine, un parti architectural et leurs conditions", 5),
            ("TERMINALE", "HDAT_VILLE_POL_CULTURELLES",   "Arts, ville, politique et société", "Expliquer l'histoire, le développement et les enjeux des politiques culturelles", 6),
            ("TERMINALE", "HDAT_VILLE_POUVOIR",           "Arts, ville, politique et société", "Analyser la diversité des rapports entre l'art, les artistes et l'autorité politique", 7),
            ("TERMINALE", "HDAT_VILLE_STATUT_ARTISTE",    "Arts, ville, politique et société", "Expliquer la relativité du statut de l'artiste", 8),
            ("TERMINALE", "HDAT_ENJEUX_RAISON_EMOTION",   "Objets et enjeux de l'histoire des arts", "Articuler raisonnement et émotion dans l'appréhension d'une œuvre d'art", 9),
            ("TERMINALE", "HDAT_ENJEUX_RELIER",           "Objets et enjeux de l'histoire des arts", "Mettre en relation domaines artistiques, époques et aires culturelles autour d'un même enjeu", 10),
            ("TERMINALE", "HDAT_ENJEUX_EXEMPLES",         "Objets et enjeux de l'histoire des arts", "Choisir des exemples pertinents dans l'ensemble de ses connaissances pour traiter une question transversale", 11),
            ("TERMINALE", "HDAT_ENJEUX_PROBLEMATIQUE",    "Objets et enjeux de l'histoire des arts", "Traiter une question diachronique par une approche problématique plutôt que par un simple découpage chronologique", 12),
            // Questions limitatives à compter de la rentrée 2026 (session 2027)
            ("TERMINALE", "HDAT_QL_VLD_OEUVRE",           "Question limitative — Viollet-le-Duc", "Présenter l'œuvre protéiforme de Viollet-le-Duc (1814-1879) : restauration, dessin, nature, création architecturale", 13),
            ("TERMINALE", "HDAT_QL_VLD_PATRIMOINE",       "Question limitative — Viollet-le-Duc", "Situer l'action de Viollet-le-Duc dans l'émergence d'une conscience patrimoniale au XIXe siècle", 14),
            ("TERMINALE", "HDAT_QL_VLD_RESTAURATION",     "Question limitative — Viollet-le-Duc", "Discuter authenticité et pérennité dans la restauration, à partir de Notre-Dame de Paris et de sa flèche", 15),
            ("TERMINALE", "HDAT_QL_VLD_TRANSMISSION",     "Question limitative — Viollet-le-Duc", "Expliquer le rôle de professeur et d'historien de Viollet-le-Duc dans la transmission des connaissances", 16),
            ("TERMINALE", "HDAT_QL_VLD_RECEPTION",        "Question limitative — Viollet-le-Duc", "Argumenter sur sa double image : précurseur de l'architecture moderne ou restaurateur trop interventionniste", 17),
            ("TERMINALE", "HDAT_QL_PARIS_CAPITALE",       "Question limitative — Paris, capitale des arts", "Expliquer pourquoi Paris devient la capitale des arts dans la première moitié du XXe siècle", 18),
            ("TERMINALE", "HDAT_QL_PARIS_LIEUX",          "Question limitative — Paris, capitale des arts", "Situer la vie artistique parisienne dans ses lieux : cafés, cabarets, galeries, ateliers, académies, quartiers", 19),
            ("TERMINALE", "HDAT_QL_PARIS_MARCHE",         "Question limitative — Paris, capitale des arts", "Expliquer le rôle du marché de l'art et des galeries dans la place centrale de Paris", 20),
            ("TERMINALE", "HDAT_QL_PARIS_CHAMPS",         "Question limitative — Paris, capitale des arts", "Relier avant-gardes et artistes étrangers aux champs peinture, sculpture, photographie, architecture, musique, danse", 21),
            ("TERMINALE", "HDAT_QL_PARIS_DECLIN",         "Question limitative — Paris, capitale des arts", "Expliquer le déclin de Paris au milieu du XXe siècle au profit d'autres foyers, notamment américains", 22),
            ("TERMINALE", "HDAT_QL_NATURE_CONCEPT",       "Question limitative — Nature ?", "Expliquer l'évolution du traitement du concept de nature par les arts et sa construction culturelle et sociale", 23),
            ("TERMINALE", "HDAT_QL_NATURE_IDEE_FORMES",   "Question limitative — Nature ?", "Distinguer l'idée ou le sentiment de nature dans l'art et les formes produites dans ou avec la nature", 24),
            ("TERMINALE", "HDAT_QL_NATURE_DOMAINES",      "Question limitative — Nature ?", "Mobiliser des œuvres sur la nature en peinture, sculpture, architecture, musique, lettres et cinéma", 25),
            ("TERMINALE", "HDAT_QL_NATURE_AIRES",         "Question limitative — Nature ?", "Comparer les traductions du concept de nature dans des aires culturelles extra-occidentales", 26),
            ("TERMINALE", "HDAT_QL_NATURE_DIACHRONIE",    "Question limitative — Nature ?", "Situer des œuvres de la fusion originelle aux ruptures technicistes en passant par les naturalismes", 27),
            // Compétences et méthode
            ("TERMINALE", "HDAT_COMP_ANALYSE",            "Compétences du cycle terminal", "Décrire, analyser, interpréter et comparer des œuvres de natures diverses par l'analyse formelle et sémantique", 28),
            ("TERMINALE", "HDAT_COMP_CONTEXTE",           "Compétences du cycle terminal", "Replacer une œuvre dans son contexte de production et de réception en dégageant ses spécificités et ses enjeux", 29),
            ("TERMINALE", "HDAT_COMP_PATRIMOINE",         "Compétences du cycle terminal", "Reconnaître la valeur artistique du patrimoine de proximité et le relier au patrimoine mondial", 30),
            ("TERMINALE", "HDAT_COMP_POSITION",           "Compétences du cycle terminal", "Soutenir une position personnelle en croisant expérience esthétique et sources d'information", 31),
            ("TERMINALE", "HDAT_METH_REPERES",            "Méthode", "Maîtriser des repères culturels, géographiques et chronologiques", 32),
            ("TERMINALE", "HDAT_METH_SOURCES",            "Méthode", "Réunir et hiérarchiser des sources diverses : livres, articles, ressources numériques", 33),
            ("TERMINALE", "HDAT_OBLIG_CONTACT",           "Pratique — Contact direct avec les œuvres", "Étudier une œuvre visuelle originale, visiter un ensemble architectural et assister à un spectacle dans l'année", 34),
            // Épreuve : écrit et oral
            ("TERMINALE", "HDAT_ECRIT_DISSERTATION",      "Écrit — dissertation", "Construire une dissertation personnelle et argumentée appuyée sur des références précises à des œuvres", 35),
            ("TERMINALE", "HDAT_ECRIT_COMPOSITION",       "Écrit — composition sur documents", "Répondre à une question en exploitant un dossier de sept documents au plus renvoyant à cinq œuvres", 36),
            ("TERMINALE", "HDAT_EVAL_COMMENTAIRE_GUIDE",  "Exercices et évaluation", "Rédiger un commentaire guidé problématisé mettant en relation un ensemble restreint d'œuvres et de documents", 37),
            ("TERMINALE", "HDAT_ORAL_PORTFOLIO",          "Oral — portfolios", "Composer un portfolio de trois à huit œuvres autour d'une problématique liée à une question limitative", 38),
            ("TERMINALE", "HDAT_ORAL_EXPOSE",             "Oral — portfolios", "Exposer sans notes sa problématique en justifiant le choix, l'ordre et les liens des œuvres retenues", 39),
            ("TERMINALE", "HDAT_ORAL_IDENTIFIER",         "Oral — portfolios", "Identifier et situer précisément chaque œuvre présentée et indiquer la source de chaque document", 40),
        };

        // -------------------------------------------------------------------------------------
        // 5. PREMIÈRE — CINÉMA-AUDIOVISUEL (annexe de MENE1901567A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] CAV1 =
        {
            // Connaissances et compétences travaillées
            ("PREMIERE", "CAV1_COMP_SENS_CONTEXTE",      "Comprendre une œuvre", "Expliquer le sens d'une œuvre cinématographique ou audiovisuelle en lien avec son contexte et son public", 1),
            ("PREMIERE", "CAV1_COMP_GESTE",              "Comprendre une œuvre", "Apprécier la spécificité d'un geste artistique dans le domaine cinématographique et audiovisuel", 2),
            ("PREMIERE", "CAV1_COMP_ANALYSER",           "Analyser", "Analyser de manière précise et argumentée une production cinématographique ou audiovisuelle", 3),
            ("PREMIERE", "CAV1_COMP_OUTILS",             "Analyser", "Choisir les outils et méthodes d'analyse pertinents selon les supports et les contextes d'écriture", 4),
            ("PREMIERE", "CAV1_COMP_REPERES",            "Histoire et techniques", "Situer une œuvre par les principaux repères de l'histoire du cinéma et de l'audiovisuel et des autres arts", 5),
            ("PREMIERE", "CAV1_COMP_INNOVATION",         "Histoire et techniques", "Relier une innovation technique à des choix de création cinématographique à une époque donnée", 6),
            ("PREMIERE", "CAV1_COMP_GOUTS",              "Spectateur et créateur", "Éprouver et argumenter ses propres choix esthétiques par la découverte et l'échange", 7),
            ("PREMIERE", "CAV1_COMP_RESPONSABILITE",     "Spectateur et créateur", "Affirmer les valeurs propres à sa responsabilité de spectateur et de créateur", 8),
            ("PREMIERE", "CAV1_PRAT_CHOIX_PROJET",       "Pratique — Projet de création", "Déterminer les choix constitutifs d'un projet de création et les mettre en œuvre", 9),
            ("PREMIERE", "CAV1_PRAT_ANALYSE_ECRITURE",   "Pratique — Projet de création", "Mobiliser ses analyses au service de sa propre écriture cinématographique et audiovisuelle", 10),
            ("PREMIERE", "CAV1_PROJET_DEFENDRE",         "Projet de création", "Présenter et défendre son projet artistique et les choix qui le fondent", 11),
            ("PREMIERE", "CAV1_AXES",                    "Axes d'étude", "Distinguer les cinq axes : Émotion(s), Motifs et représentations, Écritures, Histoire(s) et techniques, Économie(s)", 12),
            // Les genres cinématographiques, de la production à la réception
            ("PREMIERE", "CAV1_GENRE_DEFINIR",           "Les genres, de la production à la réception", "Définir le genre comme catégorie de classement et d'interprétation liée à un contrat avec le spectateur", 13),
            ("PREMIERE", "CAV1_GENRE_CONSTANTES",        "Les genres, de la production à la réception", "Identifier les constantes thématiques, narratives et stylistiques qui caractérisent un genre", 14),
            ("PREMIERE", "CAV1_GENRE_CONTEXTE",          "Les genres, de la production à la réception", "Étudier un genre historiquement situé dans son contexte de production et de réception", 15),
            ("PREMIERE", "CAV1_GENRE_VARIATION",         "Les genres, de la production à la réception", "Analyser répétition et variation dans un genre face à la spécificité d'un geste d'auteur", 16),
            ("PREMIERE", "CAV1_GENRE_MEDIATION",         "Les genres, de la production à la réception", "Expliquer le genre comme outil de production et de médiation qui organise les attentes du public", 17),
            // Être auteur, de l'écriture de scénario au final cut
            ("PREMIERE", "CAV1_AUTEUR_ETAPES",           "Être auteur, du scénario au final cut", "Décrire les étapes d'écriture d'un film : documents préparatoires, scénario, réalisation, mise en scène, montage", 18),
            ("PREMIERE", "CAV1_AUTEUR_PARTAGE",          "Être auteur, du scénario au final cut", "Expliquer le partage de la responsabilité artistique entre producteur, scénariste, réalisateur, monteur, techniciens", 19),
            ("PREMIERE", "CAV1_AUTEUR_MARQUES",          "Être auteur, du scénario au final cut", "Reconnaître les marques stylistiques et artistiques qui permettent d'identifier un auteur", 20),
            ("PREMIERE", "CAV1_AUTEUR_LIMITES",          "Être auteur, du scénario au final cut", "Expliquer les limites et ambiguïtés de l'autorité de l'auteur dans la chaîne de production", 21),
            ("PREMIERE", "CAV1_PRAT_POINT_DE_VUE",       "Pratique — Être auteur", "Affirmer son point de vue dans un projet simple de réalisation", 22),
            // Une technique dans son histoire
            ("PREMIERE", "CAV1_TECH_HISTORICITE",        "Une technique dans son histoire", "Expliquer le caractère historique d'une technique : son, parole, couleur, lumière, profondeur de champ", 23),
            ("PREMIERE", "CAV1_TECH_SON",                "Une technique dans son histoire", "Retracer le passage des films « muets » accompagnés au parlant et ses conséquences sur la mise en scène et l'émotion", 24),
            ("PREMIERE", "CAV1_TECH_ECRITURE",           "Une technique dans son histoire", "Analyser l'incidence d'une évolution technique sur l'écriture d'un film ou d'une série", 25),
            // Les studios
            ("PREMIERE", "CAV1_STUDIO_DEFINIR",          "Les studios", "Définir un studio comme structure de production industrielle dans un pays et une époque donnés", 26),
            ("PREMIERE", "CAV1_STUDIO_RATIONALISATION",  "Les studios", "Expliquer la rationalisation de la production : standardisation, division des tâches, star system, innovations", 27),
            ("PREMIERE", "CAV1_STUDIO_ESTHETIQUE",       "Les studios", "Caractériser par l'analyse formelle l'esthétique et la marque de fabrique d'un studio", 28),
            ("PREMIERE", "CAV1_STUDIO_AUTEURS",          "Les studios", "Expliquer comment des auteurs affirment leur point de vue face aux contraintes d'un studio", 29),
            ("PREMIERE", "CAV1_STUDIO_NEGOCIATION",      "Les studios", "Expliquer une œuvre comme négociation entre vision d'auteur, contraintes financières et techniques, contexte culturel", 30),
            // Situations d'apprentissage, attendus, évaluation
            ("PREMIERE", "CAV1_ATT_EXTRAIT",             "Analyser", "Analyser de manière détaillée un extrait ou une œuvre à partir d'un questionnement, avec les outils adaptés", 31),
            ("PREMIERE", "CAV1_ANALYSE_POINT_DE_VUE",    "Analyser", "Analyser la construction du point de vue dans une œuvre", 32),
            ("PREMIERE", "CAV1_EVAL_REECRITURE",         "Écriture", "Réécrire un fragment scénaristique ou filmique à partir d'une consigne", 33),
            ("PREMIERE", "CAV1_PRAT_SCENARIO_MONTAGE",   "Pratique — Écriture filmique", "Mener un exercice ou un projet de l'écriture du scénario jusqu'au montage final", 34),
            ("PREMIERE", "CAV1_PRAT_CARNET",             "Pratique — Carnet de création", "Tenir un carnet de création accompagnant le projet de l'année et les réflexions qu'il suscite", 35),
            ("PREMIERE", "CAV1_REFLEXION_ARGUMENTEE",    "Réflexion et théorie", "Présenter une réflexion argumentée sur des œuvres, à l'écrit ou à l'oral", 36),
            ("PREMIERE", "CAV1_REFLEXION_DOCUMENTS",     "Réflexion et théorie", "Confronter des supports et documents divers pour construire une réflexion personnelle", 37),
            ("PREMIERE", "CAV1_THEORIE_INITIATION",      "Réflexion et théorie", "S'initier à la réflexion théorique à partir de références historiques et esthétiques", 38),
        };

        // -------------------------------------------------------------------------------------
        // 6. TERMINALE — CINÉMA-AUDIOVISUEL (annexe 2 de MENE1921245A + programme limitatif 2026-2027)
        //    Les lignes CAVT_PL_ sont à renouveler chaque année.
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] CAVT =
        {
            ("TERMINALE", "CAVT_THEORIES_RECEPTION",      "Théories du cinéma", "Expliquer comment les principales théories du cinéma orientent l'interprétation et la réception des œuvres", 1),
            ("TERMINALE", "CAVT_THEORIES_ART_INDUSTRIE",  "Théories du cinéma", "Discuter la valeur d'une œuvre entre singularité d'une démarche, circulation des formes et réception des publics", 2),
            ("TERMINALE", "CAVT_APPROCHES",               "Théories du cinéma", "Distinguer les approches possibles d'une œuvre : pragmatique, culturelle, esthétique, historique, économique", 3),
            // Réceptions et publics (Émotion(s))
            ("TERMINALE", "CAVT_RECEP_LECTURES",          "Réceptions et publics", "Expliquer les variations de lecture d'un film selon les époques, les aires culturelles, le marketing et la critique", 4),
            ("TERMINALE", "CAVT_RECEP_OUTILS",            "Réceptions et publics", "Utiliser les outils économiques et statistiques d'analyse de la réception : box-office, études de fréquentation", 5),
            ("TERMINALE", "CAVT_RECEP_SPECTATEUR",        "Réceptions et publics", "Caractériser l'expérience perceptive et cognitive du spectateur de cinéma", 6),
            ("TERMINALE", "CAVT_RECEP_LEGITIMATION",      "Réceptions et publics", "Expliquer la reconnaissance et la légitimation des œuvres, y compris les contenus partagés sur les réseaux sociaux", 7),
            // Transferts et circulations culturels (Motifs et représentations)
            ("TERMINALE", "CAVT_TRANSF_DEFINIR",          "Transferts et circulations culturels", "Définir les transferts culturels entre deux aires ou systèmes de production, par exemple Hollywood et l'Europe", 8),
            ("TERMINALE", "CAVT_TRANSF_EXIL",             "Transferts et circulations culturels", "Analyser l'œuvre d'un cinéaste émigré ou exilé tournant dans un pays qui n'est pas le sien", 9),
            ("TERMINALE", "CAVT_TRANSF_HYBRIDATION",      "Transferts et circulations culturels", "Distinguer naturalisation et hybridation dans l'appropriation de thèmes et de formes venus d'ailleurs", 10),
            // Un cinéaste au travail (Écritures)
            ("TERMINALE", "CAVT_CINEASTE_PROCESSUS",      "Un cinéaste au travail", "Retracer le processus créatif d'un film, de la pré-production à sa matérialisation", 11),
            ("TERMINALE", "CAVT_CINEASTE_DOCUMENTS",      "Un cinéaste au travail", "Exploiter des documents de travail : notes, scénarios, story-board, matériaux audiovisuels", 12),
            ("TERMINALE", "CAVT_CINEASTE_INFLUENCES",     "Un cinéaste au travail", "Identifier les influences artistiques, historiques et socio-économiques qui conditionnent le travail d'un cinéaste", 13),
            // Périodes et courants (Histoire(s) et techniques)
            ("TERMINALE", "CAVT_PERIODES_COURANTS",       "Périodes et courants", "Caractériser un courant : expressionnisme allemand, classique hollywoodien, réalisme poétique, Nouvelle Vague…", 14),
            ("TERMINALE", "CAVT_PERIODES_TECHNIQUES",     "Périodes et courants", "Situer les périodes fixées par la technique : généralisation du parlant (1926-1934), du numérique (dès 2009)", 15),
            ("TERMINALE", "CAVT_PERIODES_PERTINENCE",     "Périodes et courants", "Discuter la pertinence des périodes et courants dans la constitution d'une histoire du cinéma", 16),
            // Art et industrie (Économie(s))
            ("TERMINALE", "CAVT_INDUSTRIE_TENSIONS",      "Art et industrie", "Expliquer les tensions entre création et production sur la chaîne fabrication, distribution, exploitation", 17),
            ("TERMINALE", "CAVT_INDUSTRIE_NOTIONS",       "Art et industrie", "Définir majors, final cut, blockbuster, box-office, franchise, streaming et exception culturelle", 18),
            ("TERMINALE", "CAVT_INDUSTRIE_AUTONOMIE",     "Art et industrie", "Analyser la conquête d'une autonomie créatrice au sein ou en marge d'un système de production", 19),
            // Programme limitatif 2026-2027 (session 2027)
            ("TERMINALE", "CAVT_PL_CLAIR_DADA",           "Programme limitatif — Entr'acte et Paris qui dort", "Situer Entr'acte (1924) et Paris qui dort (1925) de René Clair entre Dada et surréalisme (périodes et courants)", 20),
            ("TERMINALE", "CAVT_PL_CLAIR_TEMPS",          "Programme limitatif — Entr'acte et Paris qui dort", "Analyser le traitement du temps dans les deux films : accélération continue, arrêt et ville redécouverte", 21),
            ("TERMINALE", "CAVT_PL_CLAIR_GROUPE",         "Programme limitatif — Entr'acte et Paris qui dort", "Étudier les deux films comme œuvre d'une bande et invention d'un réalisateur (un cinéaste au travail)", 22),
            ("TERMINALE", "CAVT_PL_CLAIR_RYTHME",         "Programme limitatif — Entr'acte et Paris qui dort", "Analyser la scène des « scenic railways » d'Entr'acte comme leçon de rythme et de montage", 23),
            ("TERMINALE", "CAVT_PL_FELINE_CONTEXTE",      "Programme limitatif — La Féline", "Situer La Féline de Jacques Tourneur (1942) dans la relance du film d'horreur par Val Lewton à la RKO", 24),
            ("TERMINALE", "CAVT_PL_FELINE_TRANSFERTS",    "Programme limitatif — La Féline", "Expliquer ce que La Féline emprunte à l'expressionnisme allemand (transferts et circulations culturels)", 25),
            ("TERMINALE", "CAVT_PL_FELINE_SUGGESTION",    "Programme limitatif — La Féline", "Analyser l'écriture de la suggestion : ombres, indices visuels et sonores, scène de la piscine", 26),
            ("TERMINALE", "CAVT_PL_IRMAVEP_TOURNAGE",     "Programme limitatif — Irma Vep", "Analyser les épisodes 1 à 3 d'Irma Vep d'Olivier Assayas (2022) comme récit des conditions de création d'une fiction", 27),
            ("TERMINALE", "CAVT_PL_IRMAVEP_VALEUR",       "Programme limitatif — Irma Vep", "Discuter la frontière entre film et série et ce qui fonde la valeur d'une création (un cinéaste au travail)", 28),
            ("TERMINALE", "CAVT_PL_IRMAVEP_INDUSTRIE",    "Programme limitatif — Irma Vep", "Analyser les liens entre tournage, financement et publicité de luxe dans Irma Vep (art et industrie)", 29),
            ("TERMINALE", "CAVT_PL_ECHO_FELINE_IRMAVEP",  "Programme limitatif", "Mettre en écho La Féline et Irma Vep : figures félines, écritures de l'invisible et du caché", 30),
            // Épreuve écrite, pratique, oral, attendus
            ("TERMINALE", "CAVT_ECRIT_ANALYSE_EXTRAIT",   "Écrit — analyse d'extrait", "Analyser un extrait de quatre minutes au plus d'une œuvre au programme, sous forme linéaire ou composée", 31),
            ("TERMINALE", "CAVT_ECRIT_REECRITURE",        "Écrit — sujet créatif", "Rédiger une note d'intention de réécriture d'un extrait : point de vue, genre, ton, espace, temporalité", 32),
            ("TERMINALE", "CAVT_ECRIT_REFLEXION",         "Écrit — sujet réflexif", "Répondre à une question sur une œuvre au programme en s'appuyant sur un corpus de deux à quatre documents", 33),
            ("TERMINALE", "CAVT_PRAT_REALISATION",        "Pratique — Projet de création", "Réaliser en équipe un court métrage ou un fragment finalisé issu d'un ensemble plus vaste", 34),
            ("TERMINALE", "CAVT_PRAT_CARNET",             "Pratique — Carnet de création", "Constituer un carnet de création : note d'intention, étapes, analyse réflexive, documents de travail", 35),
            ("TERMINALE", "CAVT_ORAL_PRESENTER",          "Oral — projet de création", "Présenter en dix minutes son projet de création : intentions, démarche, engagement personnel", 36),
            ("TERMINALE", "CAVT_ORAL_ANALYSE_CREATION",   "Oral — projet de création", "Répondre de manière précise et argumentée à une question d'analyse portant sur son propre projet", 37),
            ("TERMINALE", "CAVT_ATT_HISTOIRE_CINEMA",     "Périodes et courants", "Réfléchir aux conditions de constitution d'une histoire du cinéma à partir de repères acquis", 38),
            ("TERMINALE", "CAVT_ATT_ECRITURE_LONGUE",     "Pratique — Projet de création", "Se projeter dans une écriture longue dont la réalisation de l'année ne serait qu'une préfiguration", 39),
        };
    }
}
