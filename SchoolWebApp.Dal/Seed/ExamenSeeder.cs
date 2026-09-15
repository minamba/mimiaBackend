using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// LES EXAMENS, TELS QUE LES TEXTES OFFICIELS LES DÉFINISSENT — et rien
    /// de fabriqué de mémoire.
    ///
    /// Idempotent et « le code fait foi », comme le référentiel : chaque
    /// démarrage remet les examens et leurs épreuves dans l'état écrit ici.
    ///
    /// UNE CARTE PAR ÉPREUVE ÉCRITE QUI PORTE SUR DES NOTIONS. Les oraux qui
    /// soutiennent un projet — l'oral du brevet, le grand oral du bac — n'en
    /// ont pas : il n'y a pas de programme à réviser dessus.
    ///
    /// ================== BREVET 2027 (série générale) ==================
    /// Vérifié le 14/09/2026 contre :
    ///   - la page « Le diplôme national du brevet » d'education.gouv.fr, mise à
    ///     jour le 01/06/2026 : les quatre épreuves écrites, leur durée, leur
    ///     coefficient et leurs parties ;
    ///   - l'arrêté du 10 avril 2025, article 7 (Légifrance
    ///     JORFTEXT000051451951) : « à compter de la session 2027 », les
    ///     épreuves écrites portent sur « les programmes de la classe de
    ///     troisième » et non plus du cycle 4 ;
    ///   - l'arrêté du 17 juillet 2026, article 1 (Légifrance
    ///     JORFTEXT000054746921) : « les programmes de la classe de troisième
    ///     lorsqu'ils sont en vigueur ou, à défaut, du cycle 4 ».
    ///
    /// Épreuve par épreuve : FRANÇAIS et MATHÉMATIQUES sur le cycle 4 (le
    /// programme de 3e n'entre en vigueur qu'à la rentrée 2028) ; HISTOIRE-
    /// GÉOGRAPHIE ET EMC sur la 3e ; SCIENCES sur le cycle 4. La 3e
    /// PRÉPA-MÉTIERS passe la série professionnelle, qui a d'autres épreuves.
    ///
    /// ================== BAC 2027 — voie technologique ==================
    /// Voulu par Camara le 14/09/2026 : « en terminale on passe le bac, en
    /// première le bac de français, il faut que ça apparaisse en fonction du
    /// niveau ». Vérifié le même jour (copies des textes dans le dossier de
    /// travail de la session, provenance détaillée en tête des fichiers de
    /// Referentiels/Techno/) :
    ///   - français : note de service MENE2019312N, version consolidée Éduscol
    ///     de mars 2024 ; programme MENE1901575A, annexe 2 ;
    ///   - philosophie : note de service MENE2001090N consolidée ; programme
    ///     MENE1921238A, annexe 2 ;
    ///   - spécialités STMG : MENE2001095N (consolidée août 2024) et
    ///     MENE2323020N ; ST2S : MENE2001091N, consolidée « CBPH et STSS » ;
    ///     STL : MENE2001092N (consolidée août 2024) ;
    ///   - coefficients : arrêté du 16/07/2018 modifié par l'arrêté du
    ///     10/06/2025 (MENE2508110A) — grand oral au coefficient 12 et épreuve
    ///     anticipée de mathématiques à partir de la session 2027.
    ///
    /// ================== BAC DE FRANÇAIS 2027 — première générale ==================
    /// Vérifié le 14/09/2026 : note de service MENE2019312N (BO spécial n° 6
    /// du 31/07/2020), modifiée par MENE2323453N (BO n° 36 du 28/09/2023,
    /// applicable depuis la session 2024) ; version consolidée Éduscol de mars
    /// 2024. Aucun texte plus récent ne modifie ces épreuves pour juin 2027.
    ///
    /// LE TITRE DIT « FRANÇAIS ET MATHS » — Camara, le 14/09/2026 : en première
    /// générale comme technologique, l'épreuve anticipée de mathématiques
    /// s'ajoute à celles de français à partir de juin 2027.
    ///
    /// ================== BAC 2027 — terminale générale ==================
    /// La philosophie, et UNE ÉPREUVE PAR SPÉCIALITÉ, chacune réservée à l'élève
    /// qui l'a cochée (`SpecialiteRequise`). Sans spécialités renseignées, la
    /// section le dit à l'élève plutôt que de réduire son bac à la philosophie.
    /// Textes : philosophie MENE2001789N ; HGGSP MENE2521923N ; HLP MENE2001793N
    /// et MENE2323020N ; SES MENE2001800N, MENE2323020N et MENE2416667N ; NSI
    /// MENE2516123N ; coefficients MENE2508110A ; calendrier MENE2622686N.
    /// Provenance détaillée en tête de chaque fichier de Referentiels/Generale/.
    ///
    /// LES ÉPREUVES ANTICIPÉES DE FRANÇAIS de juin 2027 comptent pour la
    /// session 2028 du baccalauréat. `Session` porte ici l'ANNÉE DE L'ÉPREUVE
    /// (2027), parce que c'est elle qui décide quels élèves la préparent cette
    /// année scolaire — voir `ExamenRepository`.
    ///
    /// Seulement les séries ouvertes dans Mimia (STMG, ST2S, STL). Une terminale
    /// technologique dont la série n'est pas renseignée n'a pas de section : un
    /// « bac » réduit à la philosophie serait faux.
    ///
    /// Les échéances de revérification sont dans `EcheanceReferentielSeeder`.
    /// </summary>
    public static class ExamenSeeder
    {
        private const string Cycle4 = "CINQUIEME;QUATRIEME;TROISIEME";

        private const string SourceBrevet2027 =
            "education.gouv.fr, « Le diplôme national du brevet » (mise à jour du 01/06/2026, lue le "
            + "14/09/2026) ; arrêté du 10 avril 2025, article 7 (session 2027 : programmes de la classe "
            + "de troisième) ; arrêté du 17 juillet 2026, article 1 (« lorsqu'ils sont en vigueur ou, à "
            + "défaut, du cycle 4 »).";

        private const string SourceBacTechno2027 =
            "Arrêté du 16/07/2018 modifié (MENE2508110A du 10/06/2025) ; notes de service MENE2019312N "
            + "(français), MENE2001090N (philosophie), MENE2001095N (STMG), MENE2001091N (ST2S), "
            + "MENE2001092N (STL), MENE2323020N (programme d'examen) ; versions consolidées Éduscol. Lus "
            + "le 14/09/2026.";

        private const string SourceBacFrancaisGeneral2027 =
            "Notes de service MENE2019312N (BO spécial n° 6 du 31/07/2020) et MENE2323453N (BO n° 36 du "
            + "28/09/2023) ; version consolidée Éduscol de mars 2024. Lus le 14/09/2026. Épreuves anticipées "
            + "de juin 2027, comptées pour la session 2028.";

        private const string SourceBacGeneral2027 =
            "Arrêté du 16/07/2018 modifié (MENE2508110A du 10/06/2025) ; calendrier MENE2622686N ; notes de "
            + "service MENE2001789N (philosophie), MENE2521923N (HGGSP), MENE2001793N et MENE2323020N (HLP), "
            + "MENE2001800N et MENE2416667N (SES), MENE2516123N (NSI), MENE2001796N (mathématiques), "
            + "MENE2001798N (physique-chimie), MENE2001799N (SVT), MENE2408179N (SI), MENE2121283N et "
            + "MENE2317750N (EPPCS), MENE2121271N (arts), MENE2001795N (LLCA), consolidée Éduscol LLCER (mai "
            + "2024), MENE2323020N (programme d'examen) ; programmes limitatifs MENE2611474N, MENE2536492N, "
            + "MENE2605298N. Lus le 14/09/2026.";

        private const string SourceMathsAnticipees2027 =
            " Épreuve anticipée de mathématiques : arrêté MENE2508110A et note de service MENE2515469N ; "
            + "programmes de première du 26/02/2026 (MENE2602916A, MENE2602917A, MENE2602918A).";

        private const string DescriptionPhilosophieTechno =
            "4 h · coefficient 4 — au choix, deux dissertations sur une question simple, ou une "
            + "explication de texte guidée par des questions.";

        private const string RemarquePhilosophieTechno =
            "Sept notions en voie technologique : l'art, la justice, la liberté, la nature, la religion, "
            + "la technique, la vérité.";

        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            var existants = await context.Examens
                .Include(e => e.Epreuves)
                .ToListAsync(ct);

            // ------------------------------------------------------ BREVET 2027
            var brevet = Examen(context, existants, "DNB_2027", "Brevet", "Préparation au brevet", 2027,
                "TROISIEME", SourceBrevet2027);

            Epreuve(brevet, "DNB_2027_FRANCAIS", "Brevet de français", "FRANCAIS", Cycle4, 1,
                "3 h · coefficient 2 — travail sur un texte littéraire : compréhension, interprétation, "
                + "grammaire (50 points) ; dictée (10 points) ; rédaction (40 points).",
                "Porte sur le programme du cycle 4 (5e, 4e, 3e) : le programme propre à la classe de 3e "
                + "n'entre en vigueur qu'à la rentrée 2028.");

            Epreuve(brevet, "DNB_2027_MATHS", "Brevet de mathématiques", "MATHS", Cycle4, 2,
                "2 h · coefficient 2 — automatismes, sans calculatrice (6 points) ; raisonnement et "
                + "résolution de problèmes (14 points).",
                "Porte sur le programme du cycle 4 (5e, 4e, 3e) : le programme propre à la classe de 3e "
                + "n'entre en vigueur qu'à la rentrée 2028.");

            Epreuve(brevet, "DNB_2027_HG_EMC", "Brevet d'histoire-géographie et EMC", "HISTOIRE_GEO", "TROISIEME", 3,
                "2 h — histoire-géographie, coefficient 1,5 : analyse de documents (15 points), "
                + "raisonnement et repères (25 points) ; EMC, coefficient 0,5 : une problématique à partir "
                + "d'une situation pratique (20 points).",
                "Porte sur le programme de 3e.");

            Epreuve(brevet, "DNB_2027_SCIENCES", "Brevet de sciences", "PHYSIQUE_CHIMIE;SVT", Cycle4, 4,
                "1 h · coefficient 2 — deux disciplines sur trois (physique-chimie, SVT, technologie), "
                + "10 points chacune.",
                "Porte sur le programme du cycle 4. La technologie, troisième discipline possible, n'est "
                + "pas encore couverte par Mimia.");

            // ------------------------------- BAC DE FRANÇAIS — première générale
            var francaisGeneral = Examen(context, existants, "BACFR_G_2027", "Bac de français et de maths",
                "Préparation au bac de français et de maths", 2027, "PREMIERE",
                SourceBacFrancaisGeneral2027 + SourceMathsAnticipees2027);

            Epreuve(francaisGeneral, "BACFR_G_2027_FRANCAIS", "Bac de français : l'écrit et l'oral", "FRANCAIS",
                "PREMIERE", 1,
                "Écrit : 4 h · coefficient 5 — au choix, le commentaire d'un texte qui n'est pas extrait des "
                + "œuvres au programme, ou une dissertation sur l'une des trois œuvres au programme et son "
                + "parcours. Oral : 20 min, préparées 30 min · coefficient 5 — lecture, explication linéaire, "
                + "question de grammaire (12 points), puis l'œuvre choisie et l'entretien (8 points).",
                "À l'oral, au moins quatre textes par objet d'étude dans le récapitulatif : deux extraits "
                + "au moins par œuvre, un au moins pour le parcours.");

            // DEUX CARTES POUR UNE SEULE ÉPREUVE, et l'élève n'en voit qu'une : le
            // sujet de la spécialité, ou celui des maths de l'enseignement
            // scientifique. Sans spécialités cochées, aucune des deux — la
            // section le dit.
            Epreuve(francaisGeneral, "BACFR_G_2027_MATHS_SPE", "Maths anticipées (spécialité)", "MATHS", "PREMIERE", 2,
                "Lundi 21 juin 2027, 8 h – 10 h. 2 h · coefficient 2, calculatrice interdite. Partie 1 sur 6 points : "
                + "QCM d'automatismes. Partie 2 sur 14 points : 2 ou 3 exercices indépendants. Porte sur tout le "
                + "programme de première de la spécialité, en vigueur depuis la rentrée 2026.",
                "Le QCM reprend la partie « Automatismes » du programme et les automatismes de seconde ; aucune "
                + "liste propre à 2026-2027 n'est parue. La note compte pour le bac 2028.",
                specialiteRequise: "MATHS", domainesExclus: "Enseignement scientifique");

            Epreuve(francaisGeneral, "BACFR_G_2027_MATHS_ES", "Maths anticipées", "MATHS", "PREMIERE", 3,
                "Lundi 21 juin 2027, 8 h – 10 h. 2 h · coefficient 2, calculatrice interdite. Partie 1 sur 6 points : "
                + "QCM d'automatismes. Partie 2 sur 14 points : 2 ou 3 exercices indépendants. Porte sur les "
                + "mathématiques de l'enseignement scientifique (programme de la rentrée 2026).",
                "Tu composes sur le sujet des élèves sans la spécialité mathématiques. Aucune liste d'automatismes "
                + "propre à 2026-2027 n'est parue. La note compte pour le bac 2028.",
                specialiteExclue: "MATHS", domainesInclus: "Enseignement scientifique");

            // ---------------------------- BAC DE FRANÇAIS — première technologique
            var francaisTechno = Examen(context, existants, "BACFR_T_2027", "Bac de français et de maths",
                "Préparation au bac de français et de maths", 2027,
                "PREMIERE_TECHNO;PREMIERE_STMG;PREMIERE_ST2S;PREMIERE_STL",
                SourceBacTechno2027 + " Épreuves anticipées de juin 2027, comptées pour la session 2028."
                + SourceMathsAnticipees2027);

            Epreuve(francaisTechno, "BACFR_T_2027_FRANCAIS", "Bac de français : l'écrit et l'oral", "FRANCAIS",
                "PREMIERE_TECHNO", 1,
                "Écrit : 4 h · coefficient 5 — au choix, un commentaire guidé, ou une contraction au quart "
                + "d'un texte d'environ 750 mots suivie d'un essai lié à l'œuvre d'idées étudiée. Oral : "
                + "20 min, préparées 30 min · coefficient 5 — lecture, explication linéaire, question de "
                + "grammaire, puis l'œuvre choisie et l'entretien.",
                "Pas de dissertation en voie technologique. À l'oral, au moins trois textes par objet "
                + "d'étude dans le récapitulatif.");

            Epreuve(francaisTechno, "BACFR_T_2027_MATHS", "Maths anticipées", "MATHS", "PREMIERE_TECHNO", 2,
                "Lundi 21 juin 2027, 8 h – 10 h. 2 h · coefficient 2, calculatrice interdite. Partie 1 sur 6 points : "
                + "QCM d'automatismes. Partie 2 sur 14 points : 2 ou 3 exercices indépendants. Porte sur le programme "
                + "de première technologique de la rentrée 2026, parties communes à toutes les séries.",
                "L'algorithmique, la programmation et les activités géométriques (propres à STD2A) ne sont pas "
                + "évaluées. Aucune liste d'automatismes propre à 2026-2027 n'est parue. La note compte pour le "
                + "bac 2028.",
                domainesExclus: "Algorithmique et programmation;Activités géométriques");

            // ------------------------------------- BAC GÉNÉRAL 2027 — terminale
            // LE TITRE NOMME LE BAC. Toutes les sections s'appelaient « Préparation
            // au bac » : en test, une terminale STMG a cru ne pas voir « la
            // section bac technologique » alors qu'elle l'avait sous les yeux
            // (15/09/2026). Réécrit en base à chaque démarrage, comme le reste.
            var general = Examen(context, existants, "BAC_GENERAL_2027", "Bac", "Préparation au bac général", 2027,
                "TERMINALE", SourceBacGeneral2027);

            Epreuve(general, "BAC_GENERAL_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE", 1,
                "4 h · coefficient 8 — au choix parmi trois sujets : deux dissertations, chacune posée comme une "
                + "question simple sur une ou plusieurs notions du programme, ou l'explication d'un texte d'un "
                + "auteur du programme. Note globale sur 20.",
                "Lundi 14 juin 2027, de 8 h à 12 h. Pour l'explication de texte, pas besoin de connaître la "
                + "doctrine de l'auteur.");

            Epreuve(general, "BAC_GENERAL_2027_HGGSP", "Histoire-géographie, géopolitique et sciences politiques",
                "HGGSP", "TERMINALE", 10,
                "4 h · coefficient 16 — deux exercices notés chacun sur 10 : une dissertation (deux sujets au "
                + "choix, sur deux thèmes distincts ; un croquis est valorisé, jamais obligatoire) et l'étude "
                + "critique d'un ou deux documents sur un troisième thème.",
                "En 2027, seuls les thèmes 2 (guerre et paix), 4 (patrimoine), 5 (environnement) et 6 "
                + "(connaissance) tombent à l'écrit. Les thèmes 1 et 3 restent au programme de l'année. Les "
                + "notions de première peuvent être mobilisées.",
                specialiteRequise: "HGGSP", domainesExclus: "Thème 1;Thème 3");

            Epreuve(general, "BAC_GENERAL_2027_HLP", "Humanités, littérature et philosophie", "HLP", "TERMINALE", 11,
                "4 h · coefficient 16 — sur un texte lié à un thème de terminale : une interprétation littéraire "
                + "ou philosophique d'un enjeu du texte, puis un essai qui répond à une question qu'il soulève. "
                + "Chaque partie sur 10, corrigée l'une par un professeur de lettres, l'autre de philosophie.",
                "Tout le programme de terminale est évaluable. Aucune œuvre imposée. Les notions de première "
                + "sont mobilisables sans être le cœur du sujet.",
                specialiteRequise: "HLP");

            Epreuve(general, "BAC_GENERAL_2027_SES", "Sciences économiques et sociales", "SES", "TERMINALE", 12,
                "4 h · coefficient 16 — au choix : une dissertation appuyée sur un dossier de trois ou quatre "
                + "documents, ou une épreuve composée : mobilisation des connaissances (4 points), étude d'un "
                + "document statistique (6 points), raisonnement sur un dossier (10 points).",
                "Neuf questionnements de terminale sur douze sont évaluables : ne tombent pas les crises "
                + "financières, l'action de l'École, les inégalités et la justice sociale. Aux sujets 2025, "
                + "calculatrice et dictionnaire étaient interdits.",
                specialiteRequise: "SES",
                domainesExclus: "Science économique — Crises financières;Sociologie — École;"
                    + "Regards croisés — Inégalités et justice sociale");

            Epreuve(general, "BAC_GENERAL_2027_NSI", "Numérique et sciences informatiques", "NSI", "TERMINALE", 13,
                "Coefficient 16 — un écrit de 3 h 30 à trois exercices indépendants (75 % de la note) et une "
                + "épreuve pratique d'1 h sur ordinateur (25 %) : programmer en Python une application à partir "
                + "d'un document, en dialogue avec un examinateur.",
                "Tout le programme de terminale est évaluable ; les notions de première restent mobilisables. "
                + "L'épreuve pratique a changé en 2026 : les annales pratiques plus anciennes ne suivent plus ce "
                + "format.",
                specialiteRequise: "NSI");

            Epreuve(general, "BAC_GENERAL_2027_MATHS", "Mathématiques", "MATHS", "TERMINALE", 14,
                "Mercredi 16 ou jeudi 17 juin 2027, 8 h – 12 h. 4 h · coefficient 16. Quatre exercices "
                + "indépendants, chacun noté de 4 à 8 points. Porte sur tout le programme de terminale de "
                + "spécialité (celui de 2019, toujours en vigueur) et peut mobiliser les notions de première.",
                "Rien n'est exclu depuis la session 2024 : dénombrement, sinus et cosinus, calcul intégral et loi "
                + "des grands nombres compris. Le sujet indique si la calculatrice est autorisée.",
                specialiteRequise: "MATHS");

            Epreuve(general, "BAC_GENERAL_2027_LLCER_ANGLAIS", "LLCER anglais", "LLCER_ANGLAIS", "TERMINALE", 17,
                "Coefficient 16 — écrit de 3 h 30 : synthèse en anglais d'un dossier de 3 ou 4 documents (environ "
                + "500 mots, 16 points), puis traduction ou transposition en français (4 points). Oral de 20 min "
                + "sans préparation sur un dossier personnel de 4 à 6 documents. Écrit et oral comptent chacun "
                + "pour moitié.",
                "Seul l'oral évalue les œuvres du programme limitatif. Le dossier n'est pas noté. Niveau attendu "
                + "B2/C1. Les annales de 2021 (4 h, 6 à 8 documents) ne correspondent plus à l'épreuve.",
                specialiteRequise: "LLCER_ANGLAIS");

            Epreuve(general, "BAC_GENERAL_2027_AMC", "LLCER anglais, monde contemporain", "AMC", "TERMINALE", 18,
                "Coefficient 16 — écrit de 3 h 30 : synthèse en anglais d'un dossier de 3 ou 4 documents dont un "
                + "texte de presse (environ 500 mots, 16 points), puis traduction ou transposition en français "
                + "(4 points). Oral de 20 min sans préparation sur un dossier personnel de 4 à 6 documents. Écrit "
                + "et oral comptent chacun pour moitié.",
                "Aucun programme limitatif d'œuvres. Le dossier oral compte au moins un article de presse et n'est "
                + "pas noté. Niveau attendu C1.",
                specialiteRequise: "AMC");

            Epreuve(general, "BAC_GENERAL_2027_LLCER_ESPAGNOL", "LLCER espagnol", "LLCER_ESPAGNOL", "TERMINALE", 19,
                "Coefficient 16 — écrit de 3 h 30 : synthèse en espagnol d'un dossier de 3 ou 4 documents (environ "
                + "500 mots, 16 points), puis traduction ou transposition en français (4 points). Oral de 20 min "
                + "sans préparation sur un dossier personnel de 4 à 6 documents. Écrit et oral comptent chacun "
                + "pour moitié.",
                "Seul l'oral évalue les œuvres du programme limitatif de terminale : Eréndira, El misterio de la "
                + "cripta embrujada, La familia, Odas elementales, También la lluvia. Le dossier n'est pas noté.",
                specialiteRequise: "LLCER_ESPAGNOL");

            Epreuve(general, "BAC_GENERAL_2027_SI", "Sciences de l'ingénieur", "SI", "TERMINALE", 20,
                "Coefficient 16 — écrit de 3 h 30 sur deux copies : sciences de l'ingénieur (environ 2 h 30) et "
                + "sciences physiques (environ 1 h, deux exercices). Partie pratique d'1 h. Note = 0,5 × SI + "
                + "0,25 × physique + 0,25 × pratique.",
                "Tout le programme de terminale de SI et de sciences physiques est évaluable ; les notions de "
                + "première sont mobilisables. Candidats individuels, Cned et hors contrat : pas de partie pratique.",
                specialiteRequise: "SI");

            Epreuve(general, "BAC_GENERAL_2027_EPPCS", "Éducation physique, pratiques et culture sportives", "EPPCS",
                "TERMINALE", 21,
                "Coefficient 16 — écrit de 3 h 30 : une dissertation sur la pratique physique dans le monde "
                + "contemporain (10 points), puis un sujet sur documents au choix parmi deux (10 points). Oral de "
                + "30 min : pratique d'une activité (12 points) et commentaire de la vidéo de sa prestation (8 points).",
                "Mimia prépare l'écrit et la partie théorique du commentaire, pas la pratique. Tout le programme de "
                + "terminale est évaluable. Un élève dispensé d'EPS obligatoire ne peut pas passer l'épreuve.",
                specialiteRequise: "EPPCS", domainesExclus: "Pratique —");

            // LES SEPT ENSEIGNEMENTS ARTISTIQUES : même cadre (écrit de 3 h 30 et oral
            // de 30 min, chacun pour moitié), mais chacun son écrit et ses œuvres.
            Epreuve(general, "BAC_GENERAL_2027_ARTS_PLASTIQUES", "Arts plastiques", "ARTS_PLASTIQUES", "TERMINALE", 30,
                "Coefficient 16 — écrit de 3 h 30 : analyse d'un corpus de 3 à 5 œuvres (12 points), puis au choix "
                + "un commentaire critique ou une note d'intention d'exposition (8 points). Oral de 30 min : "
                + "présentation d'un projet abouti de l'année, puis entretien (12 + 8 points).",
                "Questions limitatives 2026-2027 : Vernet et Gursky, Bourgeois et Giambologna. Tout le programme "
                + "de terminale est évaluable. À l'oral, on évalue ce que l'élève dit de son projet.",
                specialiteRequise: "ARTS_PLASTIQUES", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_HISTOIRE_ARTS", "Histoire des arts", "HISTOIRE_ARTS", "TERMINALE", 31,
                "Coefficient 16 — écrit de 3 h 30 : un sujet parmi trois, un par question limitative, en "
                + "dissertation ou composition sur documents. Oral de 30 min sans préparation : l'un de ses deux "
                + "portfolios numériques, tiré au sort, exposé puis discuté avec le jury.",
                "Questions 2026-2027 : « Nature ? », « Paris, capitale des arts » et Eugène Viollet-le-Duc. Les deux "
                + "portfolios portent sur deux questions différentes.",
                specialiteRequise: "HISTOIRE_ARTS", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_CINEMA", "Cinéma-audiovisuel", "CINEMA_AUDIOVISUEL", "TERMINALE", 32,
                "Coefficient 16 — écrit de 3 h 30 : analyse d'un extrait de 4 minutes au plus d'une œuvre au "
                + "programme, puis au choix une réécriture de l'extrait ou une réflexion appuyée sur des documents. "
                + "Oral de 30 min : présentation du projet de création, question d'analyse et entretien.",
                "Œuvres 2027 : Entr'acte et Paris qui dort (René Clair), La Féline, Irma Vep (épisodes 1 à 3). Le "
                + "programme change par tiers chaque année : ne pas réviser sur une liste ancienne.",
                specialiteRequise: "CINEMA_AUDIOVISUEL", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_MUSIQUE", "Musique", "MUSIQUE", "TERMINALE", 33,
                "Coefficient 16 — écrit de 3 h 30 en trois exercices : description d'un extrait hors programme, "
                + "commentaire comparé de deux extraits dont un du programme limitatif, commentaire d'un document "
                + "sur la vie musicale actuelle. Oral de 30 min : interprétation d'une création collective, exposé "
                + "et entretien.",
                "Programme limitatif 2026-2027 : Porgy and Bess (Armstrong-Fitzgerald), l'Allegro assai du Concerto "
                + "Wq 172 de C. P. E. Bach, « Écritures, formes, graphismes ». Écrit le 16 juin 2027 au matin.",
                specialiteRequise: "MUSIQUE", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_THEATRE", "Théâtre", "THEATRE", "TERMINALE", 34,
                "Coefficient 16 — écrit de 3 h 30 sur le programme limitatif : court essai à partir d'une captation "
                + "(8 points), puis proposition pour le plateau justifiée (12 points). Oral de 30 min : jeu sur "
                + "l'un des deux extraits préparés (12 points), puis entretien appuyé sur le carnet de bord (8 points).",
                "Programme limitatif 2026-2027 : Un Chapeau de paille d'Italie (Labiche et Marc-Michel) et "
                + "« Voyages romanesques » (L'Autre Monde de Benjamin Lazar, Gulliver de Lesort et Hecq).",
                specialiteRequise: "THEATRE", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_DANSE", "Danse", "DANSE", "TERMINALE", 35,
                "Coefficient 16 — écrit de 3 h 30 : deux sujets au choix sur « La danse, une interrogation portée "
                + "sur le monde », de culture chorégraphique ou d'analyse de documents. Oral de 30 min : composition "
                + "de 3 à 6 min en chorégraphe et interprète (12 points), puis entretien (8 points).",
                "Session 2027 : la terminale reste sur le programme limitatif de 2022, post-modern dance et Maguy "
                + "Marin. Les œuvres de la note de 2026 ne concernent que la première cette année.",
                specialiteRequise: "DANSE", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_ARTS_CIRQUE", "Arts du cirque", "ARTS_CIRQUE", "TERMINALE", 36,
                "Coefficient 16 — écrit de 3 h 30 sur le programme limitatif, à partir d'un même dossier : court "
                + "essai (10 points), puis proposition personnelle de création justifiée et illustrée (10 points). "
                + "Oral de 30 min : proposition artistique de 4 à 6 min (12 points), puis entretien (8 points).",
                "Programme limitatif 2026-2027 : les jonglages et Hourvari de la Compagnie Rasposo. Écrit le 16 "
                + "juin 2027 au matin.",
                specialiteRequise: "ARTS_CIRQUE", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_PHYSIQUE_CHIMIE", "Physique-chimie", "PHYSIQUE_CHIMIE", "TERMINALE", 15,
                "Coefficient 16 — écrit de 3 h 30 le 16 ou le 17 juin 2027 : trois exercices indépendants sur des "
                + "situations concrètes. Partie pratique d'1 h (ECE) du 1er au 4 juin 2027 : une situation tirée au "
                + "sort, où l'on manipule. Note = 80 % de l'écrit + 20 % de la pratique.",
                "Tout le programme de terminale peut tomber : aucune partie n'est exclue depuis 2024. Les notions de "
                + "première peuvent servir. Les sujets de 2021 à 2023 portaient sur un programme réduit.",
                specialiteRequise: "PHYSIQUE_CHIMIE");

            Epreuve(general, "BAC_GENERAL_2027_SVT", "Sciences de la vie et de la Terre", "SVT", "TERMINALE", 16,
                "Coefficient 16 — écrit de 3 h 30 sur 15 points le 16 ou le 17 juin 2027 : un texte argumenté qui "
                + "répond à une question scientifique (6 ou 7 points), puis un problème à résoudre à partir de "
                + "documents (8 ou 9 points). Partie pratique d'1 h (ECE) sur 5 points, du 1er au 4 juin 2027.",
                "Tout le programme de terminale peut être évalué, plante domestiquée, stress et climat compris. Les "
                + "notions de première peuvent être mobilisées. À l'exercice 1, des connaissances organisées, pas "
                + "une récitation.",
                specialiteRequise: "SVT");

            const string DescriptionLlca =
                "4 h · coefficient 16, dictionnaire seul autorisé. Trois textes : un extrait de l'œuvre antique au "
                + "programme (avec traduction), un extrait de l'œuvre moderne, un court texte antique traduit. "
                + "Partie 1 sur 10 : traduction d'environ 90 mots (6), fait de langue (2), lexique (2). Partie 2 "
                + "sur 10 : un essai argumenté sur les trois textes.";

            Epreuve(general, "BAC_GENERAL_2027_LLCA_LATIN", "LLCA latin", "LLCA_LATIN", "TERMINALE", 22,
                DescriptionLlca,
                "Œuvres 2027 : Ovide, Tristes III, et Karen Blixen, La ferme africaine, dans « L'homme, le monde, "
                + "le destin ». Tout le programme de terminale est évaluable ; les notions de première sont "
                + "mobilisables. Relégation d'Ovide n'est pas exil.",
                specialiteRequise: "LLCA_LATIN");

            Epreuve(general, "BAC_GENERAL_2027_LLCA_GREC", "LLCA grec", "LLCA_GREC", "TERMINALE", 23,
                DescriptionLlca,
                "Œuvres 2027 : Lucien, Histoires vraies, et Italo Calvino, Le baron perché, dans « L'homme, le "
                + "monde, le destin ». Tout le programme de terminale est évaluable ; les notions de première sont "
                + "mobilisables.",
                specialiteRequise: "LLCA_GREC");

            // ------------------------------------------------ BAC STMG 2027
            var stmg = Examen(context, existants, "BAC_STMG_2027", "Bac", "Préparation au bac technologique (STMG)", 2027,
                "TERMINALE_STMG", SourceBacTechno2027);

            Epreuve(stmg, "BAC_STMG_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE_TECHNO", 1,
                DescriptionPhilosophieTechno, RemarquePhilosophieTechno);

            Epreuve(stmg, "BAC_STMG_2027_DROIT_ECO", "Droit et économie", "DROIT_ECONOMIE", "TERMINALE_STMG", 2,
                "4 h · coefficient 16 — deux parties indépendantes, notées chacune sur 10 : en droit, "
                + "qualifier des faits, identifier la règle, proposer une solution ; en économie, expliquer "
                + "un mécanisme, interpréter des données, calculer, argumenter.",
                "Porte sur le programme de terminale ; les notions de première peuvent être mobilisées.");

            Epreuve(stmg, "BAC_STMG_2027_MSGN", "Management, sciences de gestion et numérique",
                "MANAGEMENT;SCIENCES_GESTION", "TERMINALE_STMG", 3,
                "4 h · coefficient 16 — plusieurs dossiers documentaires sur des organisations réelles.",
                "Porte sur la partie commune de terminale ; l'enseignement spécifique (gestion et finance, "
                + "mercatique, RH et communication, SIG) n'est pas évalué à l'écrit.");

            // ------------------------------------------------ BAC ST2S 2027
            var st2s = Examen(context, existants, "BAC_ST2S_2027", "Bac", "Préparation au bac technologique (ST2S)", 2027,
                "TERMINALE_ST2S", SourceBacTechno2027);

            Epreuve(st2s, "BAC_ST2S_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE_TECHNO", 1,
                DescriptionPhilosophieTechno, RemarquePhilosophieTechno);

            Epreuve(st2s, "BAC_ST2S_2027_CBPH", "Chimie, biologie et physiopathologie humaines",
                "BIOLOGIE_HUMAINE;PHYSIQUE_CHIMIE", "TERMINALE_ST2S", 2,
                "4 h · coefficient 16 — deux copies : la chimie (environ 1 h, coefficient 3, deux "
                + "exercices) et la biologie (environ 3 h, coefficient 13, un cas clinique et des documents).",
                "Porte sur le programme de terminale ; les notions de première peuvent être mobilisées.");

            Epreuve(st2s, "BAC_ST2S_2027_STSS", "Sciences et techniques sanitaires et sociales",
                "SANITAIRE_SOCIAL", "TERMINALE_ST2S", 3,
                "3 h · coefficient 16 — mobilisation des connaissances sans document (6 points), puis un "
                + "développement appuyé sur un dossier documentaire (14 points).",
                "Porte sur le programme de terminale ; les notions de première peuvent être mobilisées.");

            // ------------------------------------------------- BAC STL 2027
            var stl = Examen(context, existants, "BAC_STL_2027", "Bac", "Préparation au bac technologique (STL)", 2027,
                "TERMINALE_STL", SourceBacTechno2027);

            Epreuve(stl, "BAC_STL_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE_TECHNO", 1,
                DescriptionPhilosophieTechno, RemarquePhilosophieTechno);

            Epreuve(stl, "BAC_STL_2027_BBB", "Biochimie, biologie et biotechnologies", "BIOTECHNOLOGIES",
                "TERMINALE_STL", 2,
                "Écrit de 3 h (coefficient 7) : six à neuf documents puis une question de synthèse. "
                + "Pratique de 3 h (coefficient 9) : procédure, dangers, manipulation en autonomie, métrologie.",
                "Seulement si ta spécialité de terminale est la biochimie, biologie et biotechnologies.");

            Epreuve(stl, "BAC_STL_2027_SPCL", "Sciences physiques et chimiques en laboratoire", "SPCL",
                "TERMINALE_STL", 3,
                "Écrit de 3 h (coefficient 7) : trois ou quatre parties indépendantes, dont une au moins "
                + "exploite des résultats expérimentaux. Pratique de 3 h (coefficient 9).",
                "Seulement si ta spécialité de terminale est les sciences physiques et chimiques en "
                + "laboratoire.");

            await context.SaveChangesAsync(ct);
        }

        private static Examen Examen(
            SchoolWebAppDatabaseContext context, List<Examen> existants,
            string code, string libelle, string titreSection, int session, string niveaux, string source)
        {
            var examen = existants.FirstOrDefault(e => e.Code == code);

            if (examen is null)
            {
                examen = new Examen { Code = code, DateCreation = DateTime.UtcNow };
                context.Examens.Add(examen);
                existants.Add(examen);
            }

            examen.Libelle = libelle;
            examen.TitreSection = titreSection;
            examen.Session = session;
            examen.NiveauxCodes = niveaux;
            examen.Source = source;
            examen.Actif = true;

            return examen;
        }

        private static void Epreuve(
            Examen examen, string code, string libelle, string matieres, string niveaux, int ordre,
            string description, string remarque,
            string? specialiteRequise = null, string? specialiteExclue = null,
            string? domainesInclus = null, string? domainesExclus = null)
        {
            var epreuve = examen.Epreuves.FirstOrDefault(e => e.Code == code);

            if (epreuve is null)
            {
                epreuve = new EpreuveExamen { Code = code };
                examen.Epreuves.Add(epreuve);
            }

            epreuve.Libelle = libelle;
            epreuve.MatieresCodes = matieres;
            epreuve.NiveauxProgrammeCodes = niveaux;
            epreuve.Ordre = ordre;
            epreuve.Description = description;
            epreuve.Remarque = remarque;
            epreuve.SpecialiteRequise = specialiteRequise;
            epreuve.SpecialiteExclue = specialiteExclue;
            epreuve.DomainesInclus = domainesInclus;
            epreuve.DomainesExclus = domainesExclus;
        }
    }
}
