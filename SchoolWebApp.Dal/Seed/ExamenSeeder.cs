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
    /// ================== RELU LE 23/11/2026 CONTRE LE BO SPÉCIAL N° 4 ==================
    /// Le BO spécial n° 4 du 17/09/2026 abroge, à partir de la session 2027, toutes les notes d'épreuves sur
    /// lesquelles ces fiches avaient été écrites le 14/09 : brevet, français et maths anticipés, philosophie,
    /// spécialités générales, STMG, ST2S (et STL, STI2D, relues le même jour). Partout, la maîtrise de la langue
    /// compte : au moins 2 points sur 20 quand la note les chiffre (MENE2623195N), sinon « une copie défaillante
    /// ne peut avoir la moyenne ». Barèmes corrigés : SVT (6 + 7 + 2), SES composée (3,5 + 5,5 + 9 + 2), STSS
    /// (5 + 13 + 2) ; la partie 1 des maths anticipées n'est plus un QCM ; la CBPH n'annonce plus de cas
    /// clinique. Les thèmes et questionnements évalués (HGGSP, SES) et les parties exclues ne changent pas.
    /// Les LLCER n'ont pas de nouvelle note : leurs fiches restent sur la version consolidée de mai 2024.
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
    /// ================== BAC STI2D 2027 — rétabli le 23/11/2026 ==================
    /// Note de service du 11/09/2026, NOR MENE2622646N (BO spécial n° 4 du 17/09/2026), applicable à partir de la
    /// session 2027 : elle abroge celle du 10/06/2025 (MENE2515695N). Épreuve pratique de 2I2D : note de service du
    /// 01/10/2026, MENE2624111N (BO n° 38 du 08/10/2026). Textes lus dans leurs annexes PDF le 23/11/2026.
    ///
    /// Seulement les séries ouvertes dans Mimia (STMG, ST2S, STL, STI2D). Une terminale
    /// technologique dont la série n'est pas renseignée n'a pas de section : un
    /// « bac » réduit à la philosophie serait faux.
    ///
    /// ================== BAC PRO 2027 — terminale professionnelle ==================
    /// Étape 4 de la voie professionnelle, voulue par Camara le 16/10/2026.
    /// Vérifié le même jour (copies dans le dossier de travail de la session,
    /// pro-epreuves\ et pro-pse-eco\) :
    ///   - arrêté du 17/06/2020 (NOR MENE2015195A, BO n° 30 du 23/07/2020),
    ///     consolidé au 15/10/2026 : article 1 (coefficients), article 4 (QUI
    ///     passe QUOI en ponctuel), annexes I (français), II (HG-EMC), VI et VII
    ///     (économie-gestion, économie-droit), VIII (PSE) — aucune annexe
    ///     modifiée depuis 2020 ;
    ///   - programme limitatif de français : note MENE2402853N (BO n° 8 du
    ///     22/02/2024), dernière année en 2026-2027 ;
    ///   - calendrier : note MENE2622686N, BO spécial n° 2 du 25/08/2026,
    ///     annexe V — les écrits reviennent en juin, le matin.
    ///
    /// QUATRE ÉCRITS POUR UN ÉLÈVE SOUS STATUT SCOLAIRE : français, HG-EMC, PSE,
    /// et l'économie de sa spécialité. Les mathématiques, la physique-chimie, les
    /// langues vivantes, les arts appliqués et l'EPS sont en CCF pour lui (article
    /// 4) : pas de carte, une note le dit. Le « ponctuel sur support
    /// informatique » de maths et de physique-chimie (24 mai – 4 juin 2027) vise
    /// les candidats libres et les établissements non habilités au CCF.
    ///
    /// L'ÉCONOMIE SUIT LA FILIÈRE : deux cartes, et l'élève ne voit que la sienne
    /// (`ExamenRepository.ConcerneEleve`, `VoiesScolaires.MatiereDeLaFiliere`).
    ///
    /// Les échéances de revérification sont dans `EcheanceReferentielSeeder`.
    /// </summary>
    public static class ExamenSeeder
    {
        private const string Cycle4 = "CINQUIEME;QUATRIEME;TROISIEME";

        private const string SourceBrevet2027 =
            "Notes de service MENE2623194N (attribution) et MENE2623228N (épreuves), BO spécial n° 4 du "
            + "17/09/2026, à partir de la session 2027 ; arrêté du 10 avril 2025, article 7 ; arrêté du 17 juillet "
            + "2026, article 1 (« lorsqu'ils sont en vigueur ou, à défaut, du cycle 4 »). Relus le 23/11/2026.";

        private const string SourceBacTechno2027 =
            "Arrêté du 16/07/2018 modifié (MENE2508110A du 10/06/2025) ; notes de service du BO spécial n° 4 du "
            + "17/09/2026, à partir de la session 2027 : MENE2622658N (français), MENE2622663N (philosophie), "
            + "MENE2622652N (STMG), MENE2622648N (ST2S). Relus le 23/11/2026.";

        // La STL relue le 23/11/2026 contre sa note de la session 2027, qui abroge MENE2001092N.
        private const string SourceBacStl2027 =
            "Arrêté du 16/07/2018 modifié (MENE2508110A du 10/06/2025) ; notes de service MENE2622647N (épreuves "
            + "terminales STL, BO spécial n° 4 du 17/09/2026, à partir de la session 2027), MENE2622663N "
            + "(philosophie). Lus le 23/11/2026.";

        private const string SourceBacSti2d2027 =
            "Arrêté du 16/07/2018 modifié (MENE2508110A du 10/06/2025) ; notes de service MENE2622646N (épreuves "
            + "terminales STI2D, BO spécial n° 4 du 17/09/2026, à partir de la session 2027), MENE2624111N (épreuve "
            + "pratique de 2I2D, BO n° 38 du 08/10/2026), MENE2622663N (philosophie). Lus le 23/11/2026.";

        private const string SourceBacFrancaisGeneral2027 =
            "Note de service MENE2622658N (BO spécial n° 4 du 17/09/2026), qui remplace MENE2019312N. Relue le "
            + "23/11/2026. Épreuves anticipées de juin 2027, comptées pour la session 2028.";

        private const string SourceBacGeneral2027 =
            "Arrêté du 16/07/2018 modifié (MENE2508110A du 10/06/2025) ; calendrier MENE2622686N ; notes de "
            + "service du BO spécial n° 4 du 17/09/2026, à partir de la session 2027 : MENE2622661N "
            + "(philosophie), MENE2622655N (HGGSP), MENE2622659N (HLP), MENE2622656N (SES), MENE2622643N (NSI), "
            + "MENE2622642N (mathématiques), MENE2622644N (physique-chimie), MENE2622653N (SVT), MENE2622645N "
            + "(SI), MENE2622650N (EPPCS), MENE2622657N (arts), MENE2622660N (LLCA) ; consolidée Éduscol LLCER "
            + "(mai 2024), MENE2323020N (programme d'examen) ; programmes limitatifs MENE2611474N, MENE2536492N, "
            + "MENE2605298N. Relus le 23/11/2026.";

        private const string SourceMathsAnticipees2027 =
            " Épreuve anticipée de mathématiques : arrêté MENE2508110A et note de service MENE2622640N ; "
            + "programmes de première du 26/02/2026 (MENE2602916A, MENE2602917A, MENE2602918A).";

        private const string SourceBacPro2027 =
            "Arrêté du 17/06/2020 (MENE2015195A), articles 1 et 4, annexes I, II, VI, VII et VIII, consolidé au "
            + "15/10/2026 ; programme limitatif de français MENE2402853N (BO n° 8 du 22/02/2024) ; calendrier "
            + "MENE2622686N (BO spécial n° 2 du 25/08/2026, annexe V). Lus le 16/10/2026.";

        private const string TroisAnsPro = "SECONDE_PRO;PREMIERE_PRO;TERMINALE_PRO";

        // Relues le 23/11/2026 contre MENE2622663N : UN sujet à traiter parmi trois (l'ancienne fiche se lisait
        // « deux dissertations à écrire »), et les deux façons de rédiger l'explication.
        private const string DescriptionPhilosophieTechno =
            "4 h · coefficient 4 — trois sujets au choix : deux dissertations, chacune posée comme une question "
            + "simple, ou l'explication d'un texte d'un auteur du programme, guidée par des questions (analyse, "
            + "synthèse, commentaire). Note globale sur 20.";

        private const string RemarquePhilosophieTechno =
            "Sept notions en voie technologique : l'art, la justice, la liberté, la nature, la religion, la "
            + "technique, la vérité. Pour l'explication, tu réponds aux questions dans l'ordre ou tu rédiges à ta "
            + "façon, et tu l'indiques en tête de copie. Une copie défaillante en langue ne peut pas avoir la "
            + "moyenne.";

        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            var existants = await context.Examens
                .Include(e => e.Epreuves)
                .ToListAsync(ct);

            // ------------------------------------------------------ BREVET 2027
            var brevet = Examen(context, existants, "DNB_2027", "Brevet", "Préparation au brevet", 2027,
                "TROISIEME", SourceBrevet2027);

            Epreuve(brevet, "DNB_2027_FRANCAIS", "Brevet de français", "FRANCAIS", Cycle4, 1,
                "3 h · coefficient 2 — travail sur un texte littéraire, parfois avec une image : grammaire, "
                + "réécriture, compréhension, interprétation (50 points, 1 h 10) ; dictée (10 points, 20 min) ; "
                + "rédaction, au choix un sujet de réflexion ou d'imagination (40 points, 1 h 30).",
                "La langue compte dans tous les exercices. Le dictionnaire n'est permis que pour la rédaction. "
                + "Porte sur le programme du cycle 4 (5e, 4e, 3e) : le programme propre à la classe de 3e n'entre en "
                + "vigueur qu'à la rentrée 2028.");

            Epreuve(brevet, "DNB_2027_MATHS", "Brevet de mathématiques", "MATHS", Cycle4, 2,
                "2 h · coefficient 2 — automatismes, sans calculatrice (6 points, 20 min) ; raisonnement et "
                + "résolution de problèmes (14 points, 1 h 40). La maîtrise de la langue compte pour 2 points.",
                "Deux copies : la première est ramassée après les automatismes. Pas de QCM dans la deuxième partie,"
                + " et toutes les réponses se justifient. Porte sur le programme du cycle 4 (5e, 4e, 3e) : le "
                + "programme propre à la classe de 3e n'entre en vigueur qu'à la rentrée 2028.");

            Epreuve(brevet, "DNB_2027_HG_EMC", "Brevet d'histoire-géographie et EMC", "HISTOIRE_GEO", "TROISIEME", 3,
                "2 h — histoire-géographie, coefficient 1,5, sur 40 : analyse de documents (15 points), un "
                + "développement construit d'au moins 30 lignes (18 points), un croquis, un schéma ou une frise (7 "
                + "points). EMC, coefficient 0,5, sur 20 : une problématique à partir d'une situation pratique ou "
                + "d'un thème appuyé sur un ou deux documents. La rédaction compte pour 2 points dans chacun des "
                + "trois exercices.",
                "Toutes les réponses sont rédigées, même les plus courtes. Porte sur le programme de 3e.");

            // LA TECHNOLOGIE REJOINT L'ÉPREUVE (25/11/2026) : elle a enfin son professeur, Amine, et son référentiel
            // (programme du BO n° 9 du 29/02/2024, en vigueur en 3e depuis la rentrée 2026).
            Epreuve(brevet, "DNB_2027_SCIENCES", "Brevet de sciences", "PHYSIQUE_CHIMIE;SVT;TECHNOLOGIE", Cycle4, 4,
                "1 h · coefficient 2 — deux disciplines sur trois (physique-chimie, SVT, technologie), annoncées "
                + "deux mois avant l'examen : 30 minutes et 10 points chacune, dont 1 point pour la maîtrise de la "
                + "langue. Une seule copie.",
                "Porte sur le programme du cycle 4. Les deux disciplines tirées sont annoncées deux mois avant : "
                + "c'est sur elles que la révision se concentre.");

            // ------------------------------- BAC DE FRANÇAIS — première générale
            var francaisGeneral = Examen(context, existants, "BACFR_G_2027", "Bac de français et de maths",
                "Préparation au bac de français et de maths", 2027, "PREMIERE",
                SourceBacFrancaisGeneral2027 + SourceMathsAnticipees2027);

            Epreuve(francaisGeneral, "BACFR_G_2027_FRANCAIS", "Bac de français : l'écrit et l'oral", "FRANCAIS",
                "PREMIERE", 1,
                "Écrit : 4 h · coefficient 5 — au choix, le commentaire d'un texte qui n'est pas extrait des œuvres"
                + " au programme, ou une dissertation sur l'une des trois œuvres au programme et son parcours. Oral :"
                + " 20 min, préparées 30 min · coefficient 5 — lecture (2 points), explication linéaire (8 points), "
                + "question de grammaire (2 points), puis l'œuvre choisie et l'entretien (8 points).",
                "À l'oral, au moins quatre textes par objet d'étude dans le récapitulatif : deux extraits au moins "
                + "par œuvre, un au moins pour le parcours. À l'écrit, une copie défaillante en langue ne peut pas "
                + "avoir la moyenne.");

            // DEUX CARTES POUR UNE SEULE ÉPREUVE, et l'élève n'en voit qu'une : le
            // sujet de la spécialité, ou celui des maths de l'enseignement
            // scientifique. Sans spécialités cochées, aucune des deux — la
            // section le dit.
            Epreuve(francaisGeneral, "BACFR_G_2027_MATHS_SPE", "Maths anticipées (spécialité)", "MATHS", "PREMIERE", 2,
                "Lundi 21 juin 2027, 8 h – 10 h. 2 h · coefficient 2, calculatrice interdite. Partie 1 sur 6 points"
                + " : des automatismes, en questions à réponses courtes. Partie 2 sur 14 points : 2 ou 3 exercices "
                + "indépendants. La maîtrise de la langue compte pour 2 points sur 20. Porte sur tout le programme de"
                + " première de la spécialité, en vigueur depuis la rentrée 2026.",
                "La partie 1 porte sur les automatismes du programme ; aucune liste n'est publiée. Ce n'est plus "
                + "annoncé comme un QCM : entraîne-toi à écrire la réponse. La note compte pour le bac 2028.",
                specialiteRequise: "MATHS", domainesExclus: "Enseignement scientifique");

            Epreuve(francaisGeneral, "BACFR_G_2027_MATHS_ES", "Maths anticipées", "MATHS", "PREMIERE", 3,
                "Lundi 21 juin 2027, 8 h – 10 h. 2 h · coefficient 2, calculatrice interdite. Partie 1 sur 6 points"
                + " : des automatismes, en questions à réponses courtes. Partie 2 sur 14 points : 2 ou 3 exercices "
                + "indépendants. La maîtrise de la langue compte pour 2 points sur 20. Porte sur les mathématiques de"
                + " l'enseignement scientifique (programme de la rentrée 2026).",
                "Tu composes sur le sujet des élèves sans la spécialité mathématiques. Aucune liste d'automatismes "
                + "n'est publiée. La note compte pour le bac 2028.",
                specialiteExclue: "MATHS", domainesInclus: "Enseignement scientifique");

            // ---------------------------- BAC DE FRANÇAIS — première technologique
            var francaisTechno = Examen(context, existants, "BACFR_T_2027", "Bac de français et de maths",
                "Préparation au bac de français et de maths", 2027,
                "PREMIERE_TECHNO;PREMIERE_STMG;PREMIERE_ST2S;PREMIERE_STL;PREMIERE_STI2D",
                SourceBacTechno2027 + " Épreuves anticipées de juin 2027, comptées pour la session 2028."
                + SourceMathsAnticipees2027);

            Epreuve(francaisTechno, "BACFR_T_2027_FRANCAIS", "Bac de français : l'écrit et l'oral", "FRANCAIS",
                "PREMIERE_TECHNO", 1,
                "Écrit : 4 h · coefficient 5 — au choix, un commentaire guidé, ou la contraction au quart d'un "
                + "texte d'environ 750 mots (sur 10) suivie d'un essai lié à l'œuvre d'idées étudiée (sur 10). Oral :"
                + " 20 min, préparées 30 min · coefficient 5 — lecture (2 points), explication linéaire (8 points), "
                + "question de grammaire (2 points), puis l'œuvre choisie et l'entretien (8 points).",
                "Pas de dissertation en voie technologique, et le commentaire ne porte jamais sur la littérature "
                + "d'idées. Contraction : 10 % de marge, et tu écris le nombre de mots à la fin. À l'oral, au moins "
                + "trois textes par objet d'étude dans le récapitulatif. Une copie défaillante en langue ne peut pas "
                + "avoir la moyenne.");

            Epreuve(francaisTechno, "BACFR_T_2027_MATHS", "Maths anticipées", "MATHS", "PREMIERE_TECHNO", 2,
                "Lundi 21 juin 2027, 8 h – 10 h. 2 h · coefficient 2, calculatrice interdite. Partie 1 sur 6 points"
                + " : des automatismes, en questions à réponses courtes. Partie 2 sur 14 points : 2 ou 3 exercices "
                + "indépendants. La maîtrise de la langue compte pour 2 points sur 20. Porte sur le programme de "
                + "première technologique de la rentrée 2026 : vocabulaire ensembliste et logique, automatismes, "
                + "analyse, statistiques et probabilités.",
                "L'algorithmique, la programmation et les activités géométriques (propres à STD2A) ne sont pas "
                + "évaluées. Aucune liste d'automatismes n'est publiée. La note compte pour le bac 2028.",
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
                + "doctrine de l'auteur. Une copie défaillante en langue ne peut pas avoir la moyenne.");

            Epreuve(general, "BAC_GENERAL_2027_HGGSP", "Histoire-géographie, géopolitique et sciences politiques",
                "HGGSP", "TERMINALE", 10,
                "4 h · coefficient 16 — deux exercices : une dissertation (deux sujets au choix, sur deux thèmes "
                + "distincts ; un croquis est valorisé, jamais obligatoire) et l'étude critique d'un ou deux "
                + "documents sur un troisième thème. Un seul exercice traité : 10 sur 20 au plus.",
                "En 2027, seuls les thèmes 2 (guerre et paix), 4 (patrimoine), 5 (environnement) et 6 "
                + "(connaissance) tombent à l'écrit. Les thèmes 1 et 3 restent au programme de l'année. Les notions "
                + "de première peuvent être mobilisées. Une copie défaillante en langue ne peut pas avoir la moyenne.",
                specialiteRequise: "HGGSP", domainesExclus: "Thème 1;Thème 3");

            Epreuve(general, "BAC_GENERAL_2027_HLP", "Humanités, littérature et philosophie", "HLP", "TERMINALE", 11,
                "4 h · coefficient 16 — sur un texte lié à un thème de terminale : une interprétation littéraire "
                + "ou philosophique d'un enjeu du texte, puis un essai qui répond à une question qu'il soulève. "
                + "Chaque partie sur 10, corrigée l'une par un professeur de lettres, l'autre de philosophie.",
                "Tout le programme de terminale est évaluable. Aucune œuvre imposée. Les notions de première sont "
                + "mobilisables sans être le cœur du sujet. Une copie défaillante en langue ne peut pas avoir la "
                + "moyenne.",
                specialiteRequise: "HLP");

            Epreuve(general, "BAC_GENERAL_2027_SES", "Sciences économiques et sociales", "SES", "TERMINALE", 12,
                "4 h · coefficient 16 — au choix : une dissertation appuyée sur un dossier de trois ou quatre "
                + "documents factuels, ou une épreuve composée : mobilisation des connaissances (3,5 points), étude "
                + "d'un document statistique (5,5 points), raisonnement sur un dossier (9 points). Dans les deux cas,"
                + " 2 points sur 20 pour l'orthographe et la syntaxe.",
                "Neuf questionnements de terminale sur douze sont évaluables : ne tombent pas les crises "
                + "financières, l'action de l'École, les inégalités et la justice sociale. Les notions de première "
                + "doivent être connues, sans être le cœur du sujet. Aux sujets 2025, calculatrice et dictionnaire "
                + "étaient interdits.",
                specialiteRequise: "SES",
                domainesExclus: "Science économique — Crises financières;Sociologie — École;"
                    + "Regards croisés — Inégalités et justice sociale");

            Epreuve(general, "BAC_GENERAL_2027_NSI", "Numérique et sciences informatiques", "NSI", "TERMINALE", 13,
                "Coefficient 16 — un écrit de 3 h 30 à trois exercices indépendants (75 % de la note, dont 2 points"
                + " sur 20 pour la langue) et une épreuve pratique d'1 h sur ordinateur (25 %) : programmer une "
                + "application à partir d'un document, en dialogue avec un examinateur.",
                "Tout le programme de terminale est évaluable ; les notions de première doivent être connues, sans "
                + "être le cœur du sujet. L'épreuve pratique a changé en 2026 : les annales pratiques plus anciennes "
                + "ne suivent plus ce format.",
                specialiteRequise: "NSI");

            Epreuve(general, "BAC_GENERAL_2027_MATHS", "Mathématiques", "MATHS", "TERMINALE", 14,
                "Mercredi 16 ou jeudi 17 juin 2027, 8 h – 12 h. 4 h · coefficient 16. Quatre exercices "
                + "indépendants, chacun noté de 4 à 8 points. Porte sur tout le programme de terminale de "
                + "spécialité (celui de 2019, toujours en vigueur) et peut mobiliser les notions de première.",
                "Rien n'est exclu depuis la session 2024 : dénombrement, sinus et cosinus, calcul intégral et loi "
                + "des grands nombres compris. La maîtrise de la langue compte pour 2 points sur 20. Le sujet indique"
                + " si la calculatrice est autorisée.",
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
                + "sciences physiques (environ 1 h, deux exercices), 2 points sur 20 de langue dans chacune. Partie "
                + "pratique d'1 h. Note = 0,5 × SI + 0,25 × physique + 0,25 × pratique.",
                "Tout le programme de terminale de SI et de sciences physiques est évaluable ; les notions de "
                + "première sont mobilisables. Calculatrice autorisée. Candidats individuels, Cned et hors contrat : "
                + "pas de partie pratique.",
                specialiteRequise: "SI");

            Epreuve(general, "BAC_GENERAL_2027_EPPCS", "Éducation physique, pratiques et culture sportives", "EPPCS",
                "TERMINALE", 21,
                "Coefficient 16 — écrit de 3 h 30 : une dissertation de culture sportive sur les enjeux de la "
                + "pratique dans le monde contemporain (10 points), puis un sujet sur documents au choix parmi deux "
                + "(10 points). Oral de 30 min en deux temps de 15 min : pratique d'une activité (12 points) et "
                + "commentaire de la vidéo de sa prestation (8 points).",
                "Mimia prépare l'écrit et la partie théorique du commentaire, pas la pratique. Tout le programme de"
                + " terminale est évaluable ; les notions de première sont mobilisables. À l'écrit, 2 points (1 par "
                + "partie) jugent l'orthographe et la syntaxe. Un élève dispensé d'EPS obligatoire ne peut pas passer"
                + " l'épreuve.",
                specialiteRequise: "EPPCS", domainesExclus: "Pratique —");

            // LES SEPT ENSEIGNEMENTS ARTISTIQUES : même cadre (écrit de 3 h 30 et oral
            // de 30 min, chacun pour moitié), mais chacun son écrit et ses œuvres.
            Epreuve(general, "BAC_GENERAL_2027_ARTS_PLASTIQUES", "Arts plastiques", "ARTS_PLASTIQUES", "TERMINALE", 30,
                "Coefficient 16 — écrit de 3 h 30 et oral de 30 min, chacun pour moitié. Écrit : analyse d'un "
                + "corpus de 3 à 5 œuvres (12 points), puis au choix un commentaire critique ou une note d'intention "
                + "d'exposition, avec croquis obligatoires (8 points). Oral : présentation d'un projet abouti de "
                + "l'année (12 points), puis entretien (8 points).",
                "Questions limitatives 2026-2027 : Vernet et Gursky, Bourgeois et Giambologna. Tout le programme de"
                + " terminale est évaluable ; les notions de première sont mobilisables. À l'oral, on évalue ce que "
                + "l'élève dit de son projet, pas le projet lui-même.",
                specialiteRequise: "ARTS_PLASTIQUES", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_HISTOIRE_ARTS", "Histoire des arts", "HISTOIRE_ARTS", "TERMINALE", 31,
                "Coefficient 16 — écrit de 3 h 30 et oral de 30 min, chacun pour moitié. Écrit : un sujet parmi "
                + "trois, un par question limitative, en dissertation ou en composition sur documents (sept au plus)."
                + " Oral sans préparation : l'un de ses deux portfolios, tiré au sort, exposé en 15 min au plus puis "
                + "discuté avec le jury.",
                "Questions 2026-2027 : « Nature ? », « Paris, capitale des arts » et Eugène Viollet-le-Duc. Chaque "
                + "portfolio porte sur l'une de ces questions et montre 3 à 8 œuvres. Les notions de première sont "
                + "mobilisables.",
                specialiteRequise: "HISTOIRE_ARTS", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_CINEMA", "Cinéma-audiovisuel", "CINEMA_AUDIOVISUEL", "TERMINALE", 32,
                "Coefficient 16 — écrit de 3 h 30 et oral de 30 min, chacun pour moitié. Écrit : analyse d'un "
                + "extrait de 4 minutes au plus d'une œuvre au programme (10 points), puis au choix une réécriture de"
                + " l'extrait ou une réflexion sur documents (10 points). Oral sans préparation : présentation du "
                + "projet de création, question d'analyse sur ce projet, puis entretien.",
                "Œuvres 2027 : Entr'acte et Paris qui dort (René Clair), La Féline, Irma Vep (épisodes 1 à 3). Le "
                + "programme change par tiers chaque année : ne pas réviser sur une liste ancienne.",
                specialiteRequise: "CINEMA_AUDIOVISUEL", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_THEATRE", "Théâtre", "THEATRE", "TERMINALE", 34,
                "Coefficient 16 — écrit de 3 h 30 et oral de 30 min, chacun pour moitié. Écrit, textes du programme"
                + " autorisés : court essai à partir d'une captation (8 points), puis proposition pour le plateau "
                + "justifiée (12 points). Oral : jeu sur l'un des deux extraits préparés, choisi par le jury (12 "
                + "points), puis entretien appuyé sur le carnet de bord (8 points).",
                "Programme limitatif 2026-2027 : Un Chapeau de paille d'Italie (Labiche et Marc-Michel) et "
                + "« Voyages romanesques » (L'Autre Monde de Benjamin Lazar, Gulliver de Lesort et Hecq).",
                specialiteRequise: "THEATRE", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_ARTS_CIRQUE", "Arts du cirque", "ARTS_CIRQUE", "TERMINALE", 36,
                "Coefficient 16 — écrit de 3 h 30 et oral de 30 min, chacun pour moitié. Écrit sur le programme "
                + "limitatif, à partir d'un même dossier : court essai (10 points), puis proposition personnelle de "
                + "création justifiée, croquis permis (10 points). Oral : proposition artistique de 4 à 6 min (12 "
                + "points), puis entretien d'au moins 20 min (8 points).",
                "Programme limitatif 2026-2027 : les jonglages et Hourvari de la Compagnie Rasposo. Écrit le 16 "
                + "juin 2027 au matin.",
                specialiteRequise: "ARTS_CIRQUE", domainesExclus: "Pratique —");

            Epreuve(general, "BAC_GENERAL_2027_PHYSIQUE_CHIMIE", "Physique-chimie", "PHYSIQUE_CHIMIE", "TERMINALE", 15,
                "Coefficient 16 — écrit de 3 h 30 le 16 ou le 17 juin 2027 : trois exercices indépendants sur des "
                + "situations concrètes, dont 2 points sur 20 pour la langue. Partie pratique d'1 h (ECE) du 1er au 4"
                + " juin 2027 : une situation tirée au sort, où l'on manipule. Note = 80 % de l'écrit + 20 % de la "
                + "pratique.",
                "Tout le programme de terminale peut tomber : aucune partie n'est exclue. Les notions de première "
                + "doivent être connues, sans être le cœur du sujet. Place importante à la modélisation et aux "
                + "questions avec prise d'initiative. Les sujets de 2021 à 2023 portaient sur un programme réduit.",
                specialiteRequise: "PHYSIQUE_CHIMIE");

            Epreuve(general, "BAC_GENERAL_2027_SVT", "Sciences de la vie et de la Terre", "SVT", "TERMINALE", 16,
                "Coefficient 16 — écrit de 3 h 30 sur 15 points le 16 ou le 17 juin 2027 : un texte argumenté qui "
                + "répond à une question scientifique (6 points), puis un problème à résoudre à partir de documents "
                + "(7 points) ; 2 points vont à l'orthographe et à la syntaxe. Partie pratique d'1 h (ECE) sur 5 "
                + "points, du 1er au 4 juin 2027.",
                "Tout le programme de terminale peut être évalué, plante domestiquée, stress et climat compris. Les "
                + "notions de première peuvent être mobilisées. À l'exercice 1, des connaissances organisées, pas "
                + "une récitation.",
                specialiteRequise: "SVT");

            const string DescriptionLlca =
                "4 h · coefficient 16, dictionnaire seul autorisé. Trois textes : un extrait de l'œuvre antique au "
                + "programme (300 mots au plus, avec traduction), un extrait de l'œuvre moderne, un court texte "
                + "antique traduit. Partie 1 sur 10 : traduction d'environ 90 mots (6), fait de langue (1 de "
                + "grammaire, 1 d'interprétation), lexique (2), hors du passage traduit. Partie 2 sur 10 : un essai "
                + "argumenté sur les trois textes.";

            Epreuve(general, "BAC_GENERAL_2027_LLCA_LATIN", "LLCA latin", "LLCA_LATIN", "TERMINALE", 22,
                DescriptionLlca,
                "Œuvres 2027 : Ovide, Tristes III, et Karen Blixen, La ferme africaine, dans « L'homme, le monde, "
                + "le destin ». Tout le programme de terminale est évaluable ; les notions de première sont "
                + "mobilisables, sans être le cœur du sujet. Relégation d'Ovide n'est pas exil.",
                specialiteRequise: "LLCA_LATIN");

            Epreuve(general, "BAC_GENERAL_2027_LLCA_GREC", "LLCA grec", "LLCA_GREC", "TERMINALE", 23,
                DescriptionLlca,
                "Œuvres 2027 : Lucien, Histoires vraies, et Italo Calvino, Le baron perché, dans « L'homme, le "
                + "monde, le destin ». Tout le programme de terminale est évaluable ; les notions de première sont "
                + "mobilisables, sans être le cœur du sujet.",
                specialiteRequise: "LLCA_GREC");

            // ------------------------------------------------ BAC STMG 2027
            var stmg = Examen(context, existants, "BAC_STMG_2027", "Bac", "Préparation au bac technologique (STMG)", 2027,
                "TERMINALE_STMG", SourceBacTechno2027);

            Epreuve(stmg, "BAC_STMG_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE_TECHNO", 1,
                DescriptionPhilosophieTechno, RemarquePhilosophieTechno);

            Epreuve(stmg, "BAC_STMG_2027_DROIT_ECO", "Droit et économie", "DROIT_ECONOMIE", "TERMINALE_STMG", 2,
                "4 h · coefficient 16 — deux parties indépendantes, chacune sur 10 et prévue pour 2 h : en droit, "
                + "qualifier une situation, trouver la règle, proposer une solution, expliquer une règle et son "
                + "évolution ; en économie, expliquer un mécanisme, interpréter des données, calculer, argumenter sur"
                + " un débat d'actualité. Orthographe et syntaxe : 1,5 point en droit, 0,5 en économie.",
                "Porte sur le programme de terminale ; les notions de première peuvent être mobilisées.");

            Epreuve(stmg, "BAC_STMG_2027_MSGN", "Management, sciences de gestion et numérique",
                "MANAGEMENT;SCIENCES_GESTION", "TERMINALE_STMG", 3,
                "4 h · coefficient 16 — plusieurs dossiers documentaires sur des organisations réelles ; 2 points "
                + "sur 20 pour l'orthographe et la syntaxe.",
                "Porte sur la partie commune de terminale ; les notions de management et de sciences de gestion et "
                + "numérique de première peuvent être mobilisées. L'enseignement spécifique (gestion et finance, "
                + "mercatique, RH et communication, SIG) n'est pas évalué à l'écrit.");

            // ------------------------------------------------ BAC ST2S 2027
            var st2s = Examen(context, existants, "BAC_ST2S_2027", "Bac", "Préparation au bac technologique (ST2S)", 2027,
                "TERMINALE_ST2S", SourceBacTechno2027);

            Epreuve(st2s, "BAC_ST2S_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE_TECHNO", 1,
                DescriptionPhilosophieTechno, RemarquePhilosophieTechno);

            Epreuve(st2s, "BAC_ST2S_2027_CBPH", "Chimie, biologie et physiopathologie humaines",
                "BIOLOGIE_HUMAINE;PHYSIQUE_CHIMIE", "TERMINALE_ST2S", 2,
                "4 h · coefficient 16 — deux copies : la chimie (environ 1 h, coefficient 3, deux exercices) et la "
                + "biologie et physiopathologie humaines (environ 3 h, coefficient 13, au moins deux chapitres, des "
                + "documents comme des clichés d'imagerie médicale). 2 points sur 20 de langue dans chaque partie.",
                "Porte sur le programme de terminale. En chimie, la physique-chimie pour la santé de première peut "
                + "servir ; en biologie, les notions de première peuvent être mobilisées.");

            Epreuve(st2s, "BAC_ST2S_2027_STSS", "Sciences et techniques sanitaires et sociales",
                "SANITAIRE_SOCIAL", "TERMINALE_ST2S", 3,
                "3 h · coefficient 16 — une ou deux questions de connaissances, sans document (5 points), puis un "
                + "développement appuyé sur un dossier de cinq documents au plus (13 points) ; 2 points pour la "
                + "qualité de la rédaction.",
                "Porte surtout sur le pôle thématique du programme de terminale ; les notions de première peuvent "
                + "être mobilisées.");

            // ------------------------------------------------- BAC STL 2027
            // La « physique-chimie et mathématiques » ajoutée le 23/11/2026 : c'est l'une des deux spécialités de
            // terminale, que la fiche oubliait faute de programme. Préparée avec Yann et Nora, comme en STI2D.
            var stl = Examen(context, existants, "BAC_STL_2027", "Bac", "Préparation au bac technologique (STL)", 2027,
                "TERMINALE_STL", SourceBacStl2027);

            Epreuve(stl, "BAC_STL_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE_TECHNO", 1,
                DescriptionPhilosophieTechno, RemarquePhilosophieTechno);

            Epreuve(stl, "BAC_STL_2027_PCM", "Physique-chimie et mathématiques",
                "PHYSIQUE_CHIMIE;MATHS", "TERMINALE_STL", 2,
                "Écrit de 3 h · coefficient 16 — trois à cinq exercices indépendants, dont au moins un où les "
                + "mathématiques et la physique-chimie se complètent. Sur 20 : 12,5 points de physique-chimie, 5,5 "
                + "de mathématiques et 2 pour la maîtrise de la langue ; corrigé par un professeur de chaque discipline.",
                "Porte sur le programme de terminale ; les notions de première non reprises en terminale peuvent être "
                + "mobilisées.");

            Epreuve(stl, "BAC_STL_2027_BBB", "Biochimie, biologie et biotechnologies", "BIOTECHNOLOGIES",
                "TERMINALE_STL", 3,
                "Écrit de 3 h (coefficient 7) : six à neuf documents puis une question de synthèse ; 2 points sur "
                + "20 pour la maîtrise de la langue. Pratique de 3 h (coefficient 9) : procédure, dangers, "
                + "manipulation en autonomie, métrologie.",
                "Seulement si ta spécialité de terminale est la biochimie, biologie et biotechnologies.");

            Epreuve(stl, "BAC_STL_2027_SPCL", "Sciences physiques et chimiques en laboratoire", "SPCL",
                "TERMINALE_STL", 4,
                "Écrit de 3 h (coefficient 7) : trois ou quatre parties indépendantes, dont une au moins "
                + "exploite des résultats expérimentaux ; 2 points sur 20 pour la maîtrise de la langue. "
                + "Pratique de 3 h (coefficient 9).",
                "Seulement si ta spécialité de terminale est les sciences physiques et chimiques en "
                + "laboratoire.");

            // ------------------------------------------------ BAC STI2D 2027
            // Rétabli le 23/11/2026 (Camara : « exactement comme pour les autres filières »). La physique-chimie et
            // mathématiques est UNE épreuve, préparée avec deux professeurs : Yann pour sa physique-chimie, Nora pour
            // ses maths — comme la chimie et la biologie de la ST2S.
            var sti2d = Examen(context, existants, "BAC_STI2D_2027", "Bac", "Préparation au bac technologique (STI2D)", 2027,
                "TERMINALE_STI2D", SourceBacSti2d2027);

            Epreuve(sti2d, "BAC_STI2D_2027_PHILOSOPHIE", "Philosophie", "PHILOSOPHIE", "TERMINALE_TECHNO", 1,
                DescriptionPhilosophieTechno, RemarquePhilosophieTechno);

            Epreuve(sti2d, "BAC_STI2D_2027_PCM", "Physique-chimie et mathématiques",
                "PHYSIQUE_CHIMIE;MATHS", "TERMINALE_STI2D", 2,
                "Écrit de 3 h · coefficient 16 — trois à cinq exercices indépendants, dont au moins un où les "
                + "mathématiques et la physique-chimie se complètent, et un qui évalue des compétences expérimentales. "
                + "Sur 20 : 12,5 points de physique-chimie, 5,5 de mathématiques et 2 pour la maîtrise de la langue ; "
                + "corrigé par un professeur de chaque discipline.",
                "Porte sur le programme de terminale ; les notions de première non reprises en terminale peuvent être "
                + "mobilisées.");

            Epreuve(sti2d, "BAC_STI2D_2027_2I2D", "Ingénierie, innovation et développement durable", "INGENIERIE",
                "TERMINALE_STI2D", 3,
                "Écrit de 3 h 30 (coefficient 9) : l'analyse d'un produit pluritechnologique (matière, énergie, "
                + "information), puis un problème relevant de ton enseignement spécifique ; 2 points sur 20 pour la "
                + "maîtrise de la langue. Pratique de 2 h (coefficient 7), en laboratoire, sur ton enseignement "
                + "spécifique : proposer des solutions, interpréter des résultats, valider un choix à partir de "
                + "simulations et d'expérimentations.",
                "Ton enseignement spécifique est celui choisi à l'inscription : architecture et construction, énergies "
                + "et environnement, innovation technologique et éco-conception, ou systèmes d'information et "
                + "numérique. Les notions de première (innovation technologique, I2D) doivent être maîtrisées.");

            // ------------------------------------------------- BAC PRO 2027
            // Toutes les spécialités ensemble : l'économie de l'élève est
            // choisie par sa filière, le reste est commun.
            var bacPro = Examen(context, existants, "BAC_PRO_2027", "Bac pro", "Préparation au bac pro", 2027,
                "TERMINALE_PRO", SourceBacPro2027);

            Epreuve(bacPro, "BAC_PRO_2027_FRANCAIS", "Écrit de français", "FRANCAIS", "TERMINALE_PRO", 1,
                "Mardi 8 juin 2027, 8 h – 11 h. 3 h · coefficient 2,5. Sur un corpus de textes lié au thème du "
                + "programme limitatif, « Rythmes et cadences de la vie moderne : quel temps pour soi ? ». "
                + "Lecture sur 10 points : compréhension et interprétation, jusqu'à confronter les documents. "
                + "Écriture sur 10 points : un texte argumenté d'une quarantaine de lignes au moins.",
                "Une œuvre du programme limitatif est étudiée en classe, au choix du professeur parmi six. "
                + "L'oral n'est pas évalué à cet écrit.",
                domainesExclus: "Oral");

            Epreuve(bacPro, "BAC_PRO_2027_HG_EMC", "Écrit d'histoire-géographie et EMC", "HISTOIRE_GEO", "TERMINALE_PRO", 2,
                "Mercredi 9 juin 2027, 8 h – 10 h 30. 2 h 30 · coefficient 2,5. Partie 1 sur 6 points : questions "
                + "courtes en histoire ou en géographie. Partie 2 sur 8 points : une situation dans l'autre "
                + "discipline, trois documents au plus. Partie 3 sur 6 points : EMC, un dossier documentaire et "
                + "une position personnelle argumentée.",
                "Porte sur le programme de terminale. Si la partie 1 est en histoire, la partie 2 est en "
                + "géographie, et inversement.");

            Epreuve(bacPro, "BAC_PRO_2027_PSE", "Écrit de prévention-santé-environnement", "PSE", TroisAnsPro, 3,
                "Lundi 14 juin 2027, 8 h – 10 h. 2 h · coefficient 1. Partie 1 : les thématiques santé et "
                + "environnement, à partir de documents sur un enjeu sanitaire ou environnemental. Partie 2 : la "
                + "prévention au travail, à partir d'un dossier sur une situation professionnelle.",
                "Évalue les compétences de fin de formation : les modules des trois années comptent.");

            Epreuve(bacPro, "BAC_PRO_2027_ECO_DROIT", "Écrit d'économie-droit", "ECONOMIE_DROIT", TroisAnsPro, 4,
                "Mardi 15 juin 2027, 8 h – 10 h. 2 h · coefficient 1. Deux parties indépendantes : un dossier "
                + "documentaire à exploiter à l'aide de questions, puis une réponse construite et argumentée à une "
                + "question d'économie-droit.",
                "Un seul programme pour les trois ans : tous les modules peuvent tomber.");

            Epreuve(bacPro, "BAC_PRO_2027_ECO_GESTION", "Écrit d'économie-gestion", "ECONOMIE_GESTION", TroisAnsPro, 5,
                "Mardi 15 juin 2027, 8 h – 10 h. 2 h · coefficient 1. Deux parties indépendantes : des questions "
                + "sur une situation d'entreprise, puis une réponse argumentée à une question d'économie-gestion, "
                + "au choix parmi deux.",
                "Un seul programme pour les trois ans : tous les modules peuvent tomber.");

            // LES CARTES DES MATIÈRES RETIRÉES SE SUPPRIMENT — 17/09/2026. Elles ne se
            // ferment pas : une épreuve ne porte pas de drapeau « actif », contrairement
            // à une matière. Et retirer sa ligne du semeur ne suffisait pas non plus — il
            // n'enlève jamais ce qu'il ne connaît plus. Les deux cartes de musique et de
            // danse restaient donc en base après le retrait des matières, vérifiées à
            // chaque passage et signalées en rouge : « spécialité inconnue », « matière
            // fermée », « aucune notion au programme ». Quatre reproches chacune, tous
            // exacts, tous sans objet.
            //
            // SUPPRIMÉES PLUTÔT QU'EXCUSÉES. Une liste d'exceptions dans le contrôle
            // aurait rendu la page verte en laissant l'orpheline en place — et le jour
            // où une VRAIE carte se casserait de la même façon, l'exception l'aurait
            // couverte aussi.
            //
            // LA SUPPRESSION EMPORTE LES PRÉPARATIONS des élèves qui visaient ces
            // épreuves (clé étrangère en cascade). C'est voulu : préparer une épreuve
            // qui n'existe plus n'a pas de sens, et plus personne ne peut choisir ces
            // spécialités.
            var epreuvesRetirees = new[]
            {
                "BAC_GENERAL_2027_MUSIQUE",
                "BAC_GENERAL_2027_DANSE",
            };

            foreach (var examen in existants)
            {
                foreach (var retiree in examen.Epreuves
                             .Where(e => epreuvesRetirees.Contains(e.Code))
                             .ToList())
                {
                    examen.Epreuves.Remove(retiree);
                    context.Remove(retiree);
                }
            }

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
