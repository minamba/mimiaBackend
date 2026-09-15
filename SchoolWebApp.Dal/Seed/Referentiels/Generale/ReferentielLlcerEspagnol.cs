namespace SchoolWebApp.Dal.Seed.Referentiels.Generale
{
    /// <summary>
    /// La spécialité LLCER espagnol de la voie générale, première et terminale — provenance détaillée en tête du fichier.
    /// </summary>
    public static class ReferentielLlcerEspagnol
    {
        // =====================================================================================
        // RÉFÉRENTIEL DE LA SPÉCIALITÉ LLCER ESPAGNOL (langues, littératures et cultures étrangères et
        // régionales — espagnol), VOIE GÉNÉRALE, PREMIÈRE ET TERMINALE
        // Programmes en vigueur pour l'année scolaire 2026-2027. Compétences rédigées depuis le texte
        // officiel téléchargé et lu le 14/09/2026, rien de mémoire. Sources gardées dans specialites\sources\llcer-espagnol\.
        // Distinct du référentiel d'espagnol LVA/LVB du tronc commun (ReferentielEspagnol.cs) : rien n'en est repris.
        //
        // PROVENANCE
        // -------------------------------------------------------------------------------------
        // | Tableau  | Niveau    | Intitulé exact (page de titre de l'annexe)                                                  |
        // |----------|-----------|---------------------------------------------------------------------------------------------|
        // | LLCERES1 | PREMIERE  | Programme de langues, littératures et cultures étrangères - espagnol - de première générale |
        // | LLCEREST | TERMINALE | Programme de langues, littératures et cultures étrangères et régionales - espagnol de terminale générale |
        //
        // PREMIÈRE : arrêté du 17-1-2019, J.O. du 20-1-2019, NOR MENE1901590A,
        //   BO spécial n° 1 du 22 janvier 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2019 ».
        //   Quatre annexes (allemand, anglais, espagnol, italien) ; annexe 3 = espagnol, lue en entier (28 pages).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special1/MENE1901590A.htm
        //   Annexe 3        : https://cache.media.education.gouv.fr/file/SP1-MEN-22-1-2019/85/2/spe590_annexe3_22-1_1063852.pdf
        //
        // TERMINALE : arrêté du 19-7-2019, J.O. du 23-7-2019, NOR MENE1921256A,
        //   BO spécial n° 8 du 25 juillet 2019. Article 2 : « entrent en vigueur à la rentrée scolaire 2020 ».
        //   Onze annexes (4 langues étrangères, 7 régionales) ; annexe 3 = espagnol, lue en entier (37 pages).
        //   Page de l'arrêté : https://www.education.gouv.fr/bo/19/Special8/MENE1921256A.htm
        //   Annexe 3        : https://cache.media.education.gouv.fr/file/SPE8_MENJ_25_7_2019/00/1/spe256_annexe3_1159001.pdf
        //
        // TOUJOURS EN VIGUEUR EN 2026-2027 :
        //   - La page éduscol « Programmes et ressources en langues, littératures et cultures étrangères et régionales
        //     - voie G » (https://eduscol.education.gouv.fr/5814/programmes-et-ressources-en-langues-litteratures-et-cultures-etrangeres-et-regionales-voie-g,
        //     datée « juin 2026 ») cite ces arrêtés sous « Programmes en vigueur ». Ses PDF espagnol font exactement la taille des
        //     annexes du BO (452 595 et 657 534 octets) : ce sont les mêmes fichiers.
        //   - L'arrêté du 5-5-2025 (NOR MENE2504621A, BO n° 22 du 29-5-2025) fixe les programmes « communs et optionnels » de
        //     langues vivantes étrangères. Son article 3 abroge « l'arrêté du 17 janvier 2019 susvisé » en ce qu'il concerne
        //     les langues vivantes étrangères, c'est-à-dire l'arrêté des enseignements commun et optionnel (MENE1901585A).
        //     Le texte ne mentionne jamais la spécialité ni LLCER (vérifié par recherche dans la page).
        //     Il NE TOUCHE PAS la spécialité.
        //   - Les notes de service des programmes limitatifs de 2025 et 2026 (MENE2504606N, MENE2611474N) renvoient toujours
        //     aux arrêtés de 2019.
        //
        // ÉCARTS AVEC LA STRUCTURE SUPPOSÉE
        //   - L'arrêté de première a été modifié : la note MENE2504606N cite « arrêté du 17-1-2019 modifié », avec le BO n° 28 du 11-7-2019.
        //     Éduscol mentionne en plus l'arrêté du 28-6-2019. Ce texte modificatif n'a PAS pu être lu (page BO 2019 introuvable).
        //     Mais le PDF espagnol en vigueur sur éduscol a le même poids, à l'octet près, que l'annexe 3 de 2019 : l'annexe
        //     espagnol n'a donc pas changé.
        //   - Intitulé de première : « étrangères » seulement. Terminale : « étrangères et régionales ».
        //   - Les axes sont « ni limitatifs ni exhaustifs ». Les œuvres citées dans les libellés sont les exemples du texte,
        //     pas des obligations. Les œuvres obligatoires relèvent des programmes limitatifs (voir llcer-espagnol.prof.md).
        //   - « Activités langagières », « Compétences linguistiques » et « Méthode » reprennent le préambule commun et le préambule
        //     espagnol ; les autres domaines suivent les thématiques et axes d'étude.
        //
        // DÉCOMPTE (vérifié par script : codes uniques, ASCII, <= 40 caractères, libellés < 150, Ordre continu)
        //   LLCERES1 40 · LLCEREST 44 · total 84
        // =====================================================================================

        // -------------------------------------------------------------------------------------
        // 1. PREMIÈRE — LLCER ESPAGNOL (annexe 3 de MENE1901590A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] LLCERES1 =
        {
            // Activités langagières (préambule commun)
            ("PREMIERE", "LLCERES1_LIRE_TEXTES_LONGS",      "Activités langagières", "Lire des textes de plus en plus longs, littéraires, critiques ou de presse, et en restituer oralement l'essentiel", 1),
            ("PREMIERE", "LLCERES1_GENRES_LITTERAIRES",     "Activités langagières", "Identifier le genre d'un texte littéraire classique ou contemporain : théâtre, poésie, roman, nouvelle, autobiographie", 2),
            ("PREMIERE", "LLCERES1_AUDIOVISUEL",            "Activités langagières", "Comprendre l'information d'une émission ou d'un film dans une langue authentique non standardisée", 3),
            ("PREMIERE", "LLCERES1_EXPLICITE_IMPLICITE",    "Activités langagières", "Dégager le sens explicite et implicite d'un document", 4),
            ("PREMIERE", "LLCERES1_EXPOSE_NOTES",           "Activités langagières", "Faire un exposé devant la classe à partir de simples notes", 5),
            ("PREMIERE", "LLCERES1_ARGUMENTER",             "Activités langagières", "Argumenter pour convaincre en précisant sa pensée et en explicitant son raisonnement", 6),
            ("PREMIERE", "LLCERES1_INTERPRETER_TEXTE",      "Activités langagières", "Mémoriser un texte et l'interpréter de façon théâtrale ou musicale", 7),
            ("PREMIERE", "LLCERES1_INTERVIEW_TABLE_RONDE",  "Activités langagières", "Réaliser une interview ou animer une table ronde en espagnol", 8),
            ("PREMIERE", "LLCERES1_INTERACTION",            "Activités langagières", "Interagir pour construire collectivement le sens d'un support : écouter, dialoguer, échanger", 9),
            ("PREMIERE", "LLCERES1_MEDIATION_SYNTHESE",     "Activités langagières", "Paraphraser ou synthétiser un propos ou un dossier documentaire pour un camarade", 10),
            ("PREMIERE", "LLCERES1_MEDIATION_REPERES",      "Activités langagières", "Repérer les références culturelles inaccessibles à autrui et les lui rendre compréhensibles", 11),
            ("PREMIERE", "LLCERES1_TRADUCTION",             "Activités langagières", "Traduire un court passage en français en comparant le fonctionnement des deux langues", 12),

            // Compétences linguistiques (préambule commun)
            ("PREMIERE", "LLCERES1_PHONOLOGIE",             "Compétences linguistiques", "Restituer phonèmes, rythme, accentuation et intonation de l'espagnol dans une lecture à haute voix", 13),
            ("PREMIERE", "LLCERES1_GRAPHIE_PHONIE",         "Compétences linguistiques", "Relier l'orthographe espagnole et sa réalisation phonologique", 14),
            ("PREMIERE", "LLCERES1_LEXIQUE_COMMENTAIRE",    "Compétences linguistiques", "Employer le vocabulaire du commentaire de texte, d'image et de film", 15),
            ("PREMIERE", "LLCERES1_GRAMMAIRE_REGLE",        "Compétences linguistiques", "Dégager et formuler une règle de grammaire à partir d'exemples, en la comparant au français", 16),

            // Méthode (préambule spécifique à l'espagnol)
            ("PREMIERE", "LLCERES1_CONTEXTE_PRODUCTION",    "Méthode", "Situer une œuvre ou un artiste dans son contexte historique, politique et social", 17),
            ("PREMIERE", "LLCERES1_HISTOIRE_LITTERAIRE",    "Méthode", "Situer un auteur classique dans l'histoire littéraire : romance, comedia, roman picaresque, réalisme magique", 18),
            ("PREMIERE", "LLCERES1_DOC_CIVILISATION",       "Méthode", "Commenter un article, une donnée chiffrée ou une infographie sur la réalité d'un pays hispanophone", 19),
            ("PREMIERE", "LLCERES1_ANALYSE_IMAGE",          "Méthode", "Analyser une image (peinture, gravure, photographie, bande dessinée) sans la réduire à une illustration", 20),
            ("PREMIERE", "LLCERES1_CINEMA_POINT_DE_VUE",    "Méthode", "Analyser un extrait de film en espagnol en dégageant point de vue, engagement et parti pris", 21),
            ("PREMIERE", "LLCERES1_OEUVRE_INTEGRALE",       "Méthode", "Rendre compte d'une œuvre intégrale du programme limitatif et la relier à sa thématique", 22),
            ("PREMIERE", "LLCERES1_DOSSIER_PERSONNEL",      "Méthode", "Présenter les documents de son dossier personnel et justifier leur lien avec les thématiques", 23),
            ("PREMIERE", "LLCERES1_PLACE_DANS_LE_MONDE",    "Méthode", "Expliquer le rôle passé et présent de l'Espagne et de l'Amérique latine dans le monde", 24),

            // Thématique « Circulation des hommes et circulation des idées » — Voyages et exils
            ("PREMIERE", "LLCERES1_EXPLORATION_TERRITOIRE", "Circulation — Voyages et exils", "Présenter un voyage réel ou imaginaire qui explore le territoire (Don Quichotte, Colomb, Ernesto Guevara)", 25),
            ("PREMIERE", "LLCERES1_PELERINAGE_ETRANGERS",   "Circulation — Voyages et exils", "Évoquer le chemin de Saint-Jacques et le regard des voyageurs étrangers sur l'Espagne et l'Amérique latine", 26),
            ("PREMIERE", "LLCERES1_EXILS_XXE",              "Circulation — Voyages et exils", "Expliquer les exils politiques et économiques du XXe siècle et les œuvres qu'ils ont fait naître", 27),

            // Circulation — Mémoire(s) : écrire l'histoire, écrire son histoire
            ("PREMIERE", "LLCERES1_FIGURES_EMBLEMATIQUES",  "Circulation — Mémoire(s)", "Montrer comment chroniques et biographies de figures emblématiques façonnent l'imaginaire collectif", 28),
            ("PREMIERE", "LLCERES1_DEVOIR_DE_MEMOIRE",      "Circulation — Mémoire(s)", "Exposer le débat sur la mémoire des dictatures et la réconciliation", 29),
            ("PREMIERE", "LLCERES1_AUTOBIO_PICARESQUE",     "Circulation — Mémoire(s)", "Distinguer autobiographie et fausse confession picaresque (Neruda, Lazarillo de Tormes, Cela)", 30),

            // Circulation — Échanges et transmissions
            ("PREMIERE", "LLCERES1_ADAPTATIONS",            "Circulation — Échanges et transmissions", "Comparer une œuvre écrite et son adaptation radiophonique, télévisée ou cinématographique", 31),
            ("PREMIERE", "LLCERES1_AVANT_GARDES",           "Circulation — Échanges et transmissions", "Repérer l'influence des avant-gardes artistiques européennes et latino-américaines sur la création hispanique", 32),
            ("PREMIERE", "LLCERES1_MEDIAS_TERTULIAS",       "Circulation — Échanges et transmissions", "Analyser une chronique d'écrivain ou un débat médiatique et le relier à la tradition des tertulias", 33),

            // Thématique « Diversité du monde hispanophone » — Pluralité des espaces, pluralité des langues
            ("PREMIERE", "LLCERES1_TERRITOIRES",            "Diversité — Espaces et langues", "Décrire la diversité des territoires hispanophones (mégapoles, déserts, pueblo) comme protagonistes des œuvres", 34),
            ("PREMIERE", "LLCERES1_STATUT_DES_LANGUES",     "Diversité — Espaces et langues", "Expliquer les tensions autour du statut de l'espagnol, du quechua, du nahuatl, du catalan, du basque, du galicien", 35),

            // Diversité — Altérité et convivencia
            ("PREMIERE", "LLCERES1_CONVIVENCIA",            "Diversité — Altérité et convivencia", "Présenter la convivencia médiévale, mythe ou réalité, à partir des romances fronterizos", 36),
            ("PREMIERE", "LLCERES1_REGARD_CONQUERANTS",     "Diversité — Altérité et convivencia", "Analyser dans une chronique le regard des conquérants sur les peuples rencontrés (Cortés, Cartas de relación)", 37),
            ("PREMIERE", "LLCERES1_VIVRE_ENSEMBLE",         "Diversité — Altérité et convivencia", "Débattre du vivre-ensemble aujourd'hui : nationalismes, accueil de l'étranger, revendications indiennes", 38),

            // Diversité — Métissages et syncrétisme
            ("PREMIERE", "LLCERES1_METISSAGES",             "Diversité — Métissages et syncrétisme", "Illustrer métissages et syncrétismes : arabe et castillan, santería cubaine, tango, gastronomie péruvienne", 39),
            ("PREMIERE", "LLCERES1_POESIE_CROISEMENTS",     "Diversité — Métissages et syncrétisme", "Montrer les croisements de cultures dans la poésie (Lorca, Diván del Tamarit ; Guillén, Sóngoro cosongo)", 40),
        };

        // -------------------------------------------------------------------------------------
        // 2. TERMINALE — LLCER ESPAGNOL (annexe 3 de MENE1921256A)
        // -------------------------------------------------------------------------------------
        public static readonly (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] LLCEREST =
        {
            // Activités langagières (préambule commun et introduction de terminale)
            ("TERMINALE", "LLCEREST_THEMES_ABSTRAITS",      "Activités langagières", "Aborder des thèmes abstraits, littéraires, artistiques ou historiques, à partir de documents authentiques", 1),
            ("TERMINALE", "LLCEREST_ACCENTS_VARIES",        "Activités langagières", "Comprendre des locuteurs aux accents variés sans se limiter à une seule variante de l'espagnol", 2),
            ("TERMINALE", "LLCEREST_EXPLICITE_IMPLICITE",   "Activités langagières", "Dégager le sens explicite et implicite d'un texte, d'une image ou d'un film", 3),
            ("TERMINALE", "LLCEREST_SYNTHESE_DOCUMENTS",    "Activités langagières", "Synthétiser plusieurs documents en faisant apparaître les liens de sens qui les unissent", 4),
            ("TERMINALE", "LLCEREST_COMMENTAIRE_CONTRACTION","Activités langagières", "Commenter ou contracter un texte de civilisation ou de littérature", 5),
            ("TERMINALE", "LLCEREST_VERSION",               "Activités langagières", "Traduire en français un passage d'un texte (version) sans rester calqué sur l'original", 6),
            ("TERMINALE", "LLCEREST_REALITE_REPRESENTATION","Activités langagières", "Prendre du recul sur un document en distinguant réalité et représentation", 7),
            ("TERMINALE", "LLCEREST_MEDIATION_DEBAT",       "Activités langagières", "Transmettre des informations précises à un camarade et gérer un débat en situation de désaccord", 8),
            ("TERMINALE", "LLCEREST_REFORMULER",            "Activités langagières", "Reformuler en espagnol le sens d'un document pour le rendre accessible à autrui", 9),
            ("TERMINALE", "LLCEREST_PAROLE_CONTINUE",       "Activités langagières", "Parler en continu à partir de notes avec fluidité, précision et richesse lexicale", 10),
            ("TERMINALE", "LLCEREST_INTERACTION",           "Activités langagières", "Interagir, réagir avec pertinence et relancer l'échange en s'appuyant sur des références culturelles", 11),

            // Compétences linguistiques
            ("TERMINALE", "LLCEREST_PHONOLOGIE",            "Compétences linguistiques", "Restituer rythme, accentuation et intonation dans une prise de parole préparée ou spontanée", 12),
            ("TERMINALE", "LLCEREST_LEXIQUE_NUANCE",        "Compétences linguistiques", "Employer un lexique nuancé, des expressions idiomatiques et des structures complexes maîtrisées", 13),

            // Méthode (introduction de terminale et préambule espagnol)
            ("TERMINALE", "LLCEREST_DOSSIER_PERSONNEL",     "Méthode", "Présenter son dossier personnel, en justifier le choix des documents et en expliquer la logique interne", 14),
            ("TERMINALE", "LLCEREST_OEUVRE_INTEGRALE",      "Méthode", "Analyser une œuvre intégrale du programme limitatif en alternant extraits et lecture d'ensemble", 15),
            ("TERMINALE", "LLCEREST_OEUVRE_FILMIQUE",       "Méthode", "Analyser l'œuvre filmique du programme limitatif comme une écriture du monde en image et en son", 16),
            ("TERMINALE", "LLCEREST_GRANDES_PERIODES",      "Méthode", "Situer une œuvre dans les grandes périodes de l'histoire espagnole et latino-américaine", 17),
            ("TERMINALE", "LLCEREST_GENRES_MOUVEMENTS",     "Méthode", "Rattacher une œuvre à son genre et à un mouvement artistique ou littéraire hispanophone majeur", 18),
            ("TERMINALE", "LLCEREST_JUGEMENT_ESTHETIQUE",   "Méthode", "Formuler une appréciation argumentée d'un texte, d'une image ou d'un film selon des critères esthétiques", 19),
            ("TERMINALE", "LLCEREST_CROISER_THEMATIQUES",   "Méthode", "Croiser thématiques et domaines (arts, histoire, sociologie…) pour problématiser un sujet", 20),
            ("TERMINALE", "LLCEREST_RECHERCHE_DOCUMENTAIRE","Méthode", "Mener une recherche documentaire et en exposer les résultats avec un regard critique", 21),

            // Thématique « Représentations culturelles : entre imaginaires et réalités » — Nature et mythologies
            ("TERMINALE", "LLCEREST_MYTHES_PRECOLOMBIENS",  "Représentations — Nature et mythologies", "Présenter un mythe précolombien de création (Popol Vuh, Quetzalcóatl, Inti) et sa reprise par les artistes", 22),
            ("TERMINALE", "LLCEREST_MYTHES_CONQUETE",       "Représentations — Nature et mythologies", "Expliquer les mythes nés de la conquête, comme El Dorado, et les figures archétypales comme la Malinche", 23),
            ("TERMINALE", "LLCEREST_MYTHOLOGIE_IBERIQUE",   "Représentations — Nature et mythologies", "Évoquer les légendes ibériques (meigas, Basajaun) et la nature comme protagoniste (Machado, Neruda)", 24),

            // Représentations — Les représentations du réel
            ("TERMINALE", "LLCEREST_ROMAN_REALISTE",        "Représentations — Représentations du réel", "Caractériser le réalisme espagnol du XIXe au XXe siècle (Galdós, Clarín, Pardo Bazán, Delibes, Cela)", 25),
            ("TERMINALE", "LLCEREST_ENVERS_DU_REEL",        "Représentations — Représentations du réel", "Analyser l'écart avec le réel : illusion et folie (Quijote, La vida es sueño, Goya), esperpento de Valle-Inclán", 26),
            ("TERMINALE", "LLCEREST_REALISME_MAGIQUE",      "Représentations — Représentations du réel", "Distinguer fantastique et réalisme magique (Silvina Ocampo, Cortázar, García Márquez)", 27),

            // Représentations — Du type au stéréotype : construction et dépassement
            ("TERMINALE", "LLCEREST_TYPES_LITTERAIRES",     "Représentations — Type et stéréotype", "Retracer la réinterprétation d'un type : Don Juan, la Célestine, le pícaro, le galán, le gracioso", 28),
            ("TERMINALE", "LLCEREST_STEREOTYPES",           "Représentations — Type et stéréotype", "Déconstruire des figures stéréotypées diffusées par le cinéma : gitane, torero, bandolero, gaucho, indio", 29),

            // Thématique « Dominations et insoumissions » — Oppression, résistances et révoltes
            ("TERMINALE", "LLCEREST_FIGURE_DICTATEUR",      "Dominations — Oppression et révoltes", "Analyser la représentation du dictateur, du portrait officiel à la critique (Botero, Roa Bastos)", 30),
            ("TERMINALE", "LLCEREST_ART_MILITANT",          "Dominations — Oppression et révoltes", "Présenter l'art militant face à l'oppression : poésie, chanson engagée, bande dessinée, cinéma populaire", 31),
            ("TERMINALE", "LLCEREST_DOMINATION_TERRE",      "Dominations — Oppression et révoltes", "Expliquer les dominations liées à la terre : maître et esclave, cacique, indien, ouvrier agricole", 32),
            ("TERMINALE", "LLCEREST_CONVENTIONS_GENRE",     "Dominations — Oppression et révoltes", "Débattre des résistances au poids des conventions et aux violences de genre (marches contre les feminicidios)", 33),

            // Dominations — Révolutions et ruptures
            ("TERMINALE", "LLCEREST_REVOLUTION_MEXICAINE",  "Dominations — Révolutions et ruptures", "Exposer la révolution mexicaine, des muralistes et corridos aux désillusions de Rulfo ou Fuentes", 34),
            ("TERMINALE", "LLCEREST_CUBA_CHILI",            "Dominations — Révolutions et ruptures", "Comparer révolution cubaine et Chili d'Allende à travers poésie, chanson et documentaire", 35),
            ("TERMINALE", "LLCEREST_INDEPENDANCES",         "Dominations — Révolutions et ruptures", "Présenter les indépendances hispano-américaines et l'exaltation des libertadores (Bolívar, Martí)", 36),

            // Dominations — Culture officielle et émancipations culturelles
            ("TERMINALE", "LLCEREST_CULTURE_OFFICIELLE",    "Dominations — Culture officielle", "Analyser la culture officielle d'un régime autoritaire : propagande franquiste, censure, culte du leader", 37),
            ("TERMINALE", "LLCEREST_CONTRE_CULTURES",       "Dominations — Culture officielle", "Présenter les contre-cultures : underground madrilène et barcelonais après Franco, lucha libre mexicaine", 38),

            // Thématique « L'Espagne et l'Amérique latine dans le monde » — Monde globalisé : contacts et influences
            ("TERMINALE", "LLCEREST_MONDIALISATION",        "Monde — Monde globalisé", "Débattre des effets du libre-échange et de l'exploitation des ressources sur les populations et les milieux", 39),
            ("TERMINALE", "LLCEREST_CULTURE_MONDIALISEE",   "Monde — Monde globalisé", "Analyser la diffusion mondiale de productions hispaniques : telenovelas, plateformes, figure de Don Quichotte", 40),

            // Monde — Crises et violences
            ("TERMINALE", "LLCEREST_CRISES",                "Monde — Crises et violences", "Exposer une crise et ses représentations : corralito argentin, conflit colombien, crise espagnole de 2008", 41),
            ("TERMINALE", "LLCEREST_REPONSES_CITOYENNES",   "Monde — Crises et violences", "Expliquer les réponses aux crises : Indignés, Podemos, revendications indépendantistes, remise en cause de la monarchie", 42),

            // Monde — La frontière en question
            ("TERMINALE", "LLCEREST_FRONTIERES_MIGRATOIRES","Monde — La frontière en question", "Analyser la frontière Mexique–États-Unis ou le détroit de Gibraltar dans la littérature, le film et la fresque", 43),
            ("TERMINALE", "LLCEREST_FRONTIERES_SOCIALES",   "Monde — La frontière en question", "Montrer comment la ville trace des frontières sociales et discuter de l'espace privé et public à l'ère numérique", 44),
        };
    }
}
