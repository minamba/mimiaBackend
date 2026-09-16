using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Dal.Seed.Referentiels;
using SchoolWebApp.Dal.Seed.Referentiels.Generale;
using SchoolWebApp.Dal.Seed.Referentiels.Techno;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// Alimente les tables de référence : niveaux, matières, et le graphe de
    /// compétences. Idempotent — chaque élément est identifié par son Code et
    /// n'est inséré que s'il est absent, donc l'exécution peut être répétée
    /// à chaque démarrage sans dupliquer.
    ///
    /// Chaque référentiel couvre la scolarité entière de sa matière, y compris
    /// les niveaux antérieurs à celui de l'élève. C'est ce qui permet au
    /// diagnostic de remonter d'un blocage en 6e vers une lacune de CM1 :
    /// sans les niveaux amont, une lacune ancienne n'aurait aucune compétence
    /// à laquelle se rattacher, et le professeur s'acharnerait sur l'aval.
    ///
    /// ================== PROVENANCE DU RÉFÉRENTIEL ==================
    ///
    /// VÉRIFIÉ LE 5 SEPTEMBRE 2026 contre les textes officiels, matière par
    /// matière et niveau par niveau. CE TABLEAU SE MET À JOUR, il ne se
    /// recopie pas : une ligne périmée ici est un élève qui ne se retrouve
    /// pas dans son année.
    ///
    ///   MATIÈRE / NIVEAUX          TEXTE                    EN VIGUEUR DEPUIS
    ///   ------------------------------------------------------------------
    ///   Français et maths
    ///     CP, CE1, CE2             Arrêté du 22/10/2024,    rentrée 2025
    ///                              BO n° 41 du 31/10/2024
    ///     CM1, 6e                  BO n° 16 du 17/04/2025   rentrée 2025
    ///     CM2                      BO n° 16 du 17/04/2025   rentrée 2026
    ///     5e                       BO n° 10 de 2026         rentrée 2026
    ///     4e                       BO n° 10 de 2026         rentrée 2027  (*)
    ///     3e                       BO n° 10 de 2026         rentrée 2028  (*)
    ///
    ///   Langues vivantes
    ///     CP, CM1                  BO n° 12 de 2026         rentrée 2026
    ///     CE1, CE2, CM2            BO n° 12 de 2026         rentrée 2027  (*)
    ///     5e                       BO n° 22 du 29/05/2025   rentrée 2026
    ///     4e                       BO n° 22 du 29/05/2025   rentrée 2027  (*)
    ///     3e                       BO n° 22 du 29/05/2025   rentrée 2028  (*)
    ///     Lycée GT                 BO n° 22 du 29/05/2025   rentrée 2026
    ///
    ///   Histoire-géographie
    ///     CM1                      BO n° 22 du 28/05/2026   rentrée 2026
    ///     CM2, 6e                  BO n° 22 du 28/05/2026   rentrée 2027  (*)
    ///     Cycle 4                  programmes de 2020, projet de refonte
    ///                              en cours — rien à changer aujourd'hui
    ///
    ///   Sciences et technologie
    ///     CP, CM1                  BO n° 24 du 11/06/2026   rentrée 2026
    ///     CE1                      BO n° 24 du 11/06/2026   rentrée 2027  (*)
    ///     CE2, CM2, 6e             ancien programme encore en vigueur
    ///                              en 2026-2027
    ///
    ///   Physique-chimie et SVT (cycle 4)
    ///     5e, 4e, 3e               programmes en vigueur ; de nouveaux
    ///                              textes étaient en consultation nationale
    ///                              en mai-juin 2026 — À SURVEILLER
    ///
    ///   Lycée général et technologique (toutes matières)
    ///     2de, 1re, Tle            programmes de 2019 pour l'essentiel,
    ///                              en vigueur en 2026-2027. TROIS
    ///                              EXCEPTIONS DÉJÀ BASCULÉES : maths de
    ///                              2de GT, spécialité de 1re générale et
    ///                              maths de 1re TECHNOLOGIQUE suivent les
    ///                              arrêtés du 26/02/2026. La spécialité de
    ///                              Tle générale et les maths de Tle
    ///                              technologique basculent à la rentrée
    ///                              2027 — À FAIRE AVANT.
    ///
    ///   (*) Ces niveaux suivent ENCORE le programme précédent cette année.
    ///       Les compétences du nouveau texte y ont malgré tout été ajoutées
    ///       quand elles étaient déjà valables sous l'ancien : rien n'a été
    ///       retiré, et rien d'inconnu n'est montré en avance.
    ///
    /// CE QUI AVAIT ÉTÉ MANQUÉ, et qui dit ce qu'il faut regarder la
    /// prochaine fois. Le référentiel décrivait les programmes d'AVANT la
    /// refonte de 2025, sur sept niveaux du CP à la 5e. Manquaient entre
    /// autres : les fractions au CE1 et au CE2, les nombres ordinaux au CP,
    /// la typologie des problèmes arithmétiques, l'algèbre au cours moyen et
    /// en 6e, les probabilités au CM1 et au CM2, l'organisation de données au
    /// cycle 2 ; en français, les formes de phrase, la synonymie et
    /// l'antonymie, la polysémie, « écouter pour comprendre » et les écrits
    /// réflexifs — six notions qui n'apparaissaient à AUCUN niveau ; en
    /// anglais, les paliers du CECRL et les axes culturels de l'année.
    ///
    /// La leçon tient en une ligne : un programme scolaire change, et il
    /// change par niveau et par date, pas d'un bloc.
    ///
    /// ---------------------------------------------------------------
    ///
    /// DEUXIÈME PASSE, LE 5 SEPTEMBRE 2026 : LES MATIÈRES DONT LE
    /// PROGRAMME N'AVAIT PAS CHANGÉ.
    ///
    /// « Le programme n'a pas changé » ne dit pas « le référentiel le
    /// décrit correctement ». Ces matières-là n'étaient pas périmées,
    /// elles étaient NON VÉRIFIÉES — et la vérification a trouvé :
    ///
    ///   — physique-chimie : l'attendu de fin de cycle « Décrire
    ///     l'organisation de la matière dans l'Univers » n'était
    ///     représenté par aucune compétence ; manquaient aussi la
    ///     classification périodique, les gaz à effet de serre, et le
    ///     couple signal-information, concept-titre d'un thème entier ;
    ///   — mathématiques : « Vocabulaire ensembliste et logique » ne
    ///     figurait à AUCUN des trois niveaux du lycée, alors qu'il est
    ///     au programme des trois. C'est le raisonnement lui-même ;
    ///   — histoire-géographie de terminale : le programme ouvre en
    ///     1929 (crise, totalitarismes, Seconde Guerre mondiale) ; le
    ///     référentiel faisait commencer l'année en 1945. L'élève ne
    ///     trouvait rien du premier chapitre traité en septembre ;
    ///   — français de seconde : deux périodes étaient INTERVERTIES
    ///     avec celles de première — poésie et littérature d'idées ;
    ///   — histoire de première : quatre chapitres couverts sur onze.
    ///     Manquaient la Révolution française comme NOUVELLE CONCEPTION
    ///     DE LA NATION — elle n'existait qu'en 4e, comme récit
    ///     d'événements — et les deux chapitres qui encadrent la
    ///     Grande Guerre. Le texte est l'ANNEXE 2 de MENE1901577A :
    ///     un même arrêté porte les trois classes, et le script n'en
    ///     lisait que la première annexe. On liste TOUTES les annexes
    ///     d'un arrêté avant de conclure qu'un texte n'existe pas ;
    ///   — philosophie : rien. Les dix-sept notions étaient déjà là,
    ///     rangées selon les trois perspectives du programme. Une
    ///     vérification ne trouve pas toujours un trou, et c'est une
    ///     information aussi.
    ///
    /// CE QUI RESTE À CONFRONTER À UN TEXTE :
    ///
    ///   — les sciences en CE1, CE2, CM2 et 6e, et l'anglais en CE1,
    ///     CE2 et CM2, face aux programmes de 2026 : ces niveaux
    ///     suivent encore l'ancien texte et basculeront en 2027.
    ///
    /// L'anglais a été confronté en entier le 5 septembre 2026 : paliers
    /// du CECRL, axes culturels, et outils linguistiques. Le référentiel
    /// y était VERBAL — le verbe était couvert à tous les niveaux, mais
    /// les adverbes et groupes prépositionnels, catégorie entière du
    /// programme à sept niveaux sur huit, n'apparaissaient nulle part ;
    /// le groupe nominal se réduisait au génitif de 6e ; la phonologie
    /// était absente de TOUT le lycée, alors que le baccalauréat comporte
    /// une épreuve orale. La médiation — expliciter un message pour
    /// autrui — manquait également : c'est une activité langagière à part
    /// entière du CECRL, ni compréhension ni expression.
    ///
    /// ENFIN, DEUX LIMITES QUI NE SONT PAS DES OUBLIS MAIS DES CHOIX :
    /// les compétences ajoutées lors de ces deux passes n'ont PAS de
    /// prérequis — elles s'affichent, mais le diagnostic remontant ne
    /// les traverse pas encore — et aucun professeur ne les a relues.
    ///
    /// ---------------------------------------------------------------
    ///
    /// TROISIÈME PASSE, LE 6 SEPTEMBRE 2026 : LES VOIES TECHNOLOGIQUE ET
    /// PROFESSIONNELLE N'AVAIENT PAS DE PROGRAMME DE MATHÉMATIQUES.
    ///
    /// Le référentiel était écrit pour les classes GÉNÉRALES, et les
    /// autres voies en héritaient par le rang d'année — un choix assumé,
    /// qui donnait un programme à six classes sans rien écrire pour
    /// elles. Il tient tant que les classes d'un même rang étudient la
    /// même chose. En mathématiques, elles ne l'étudient pas :
    ///
    ///   — un élève de terminale PROFESSIONNELLE recevait les 23
    ///     compétences de la SPÉCIALITÉ de terminale générale —
    ///     récurrence, logarithme népérien, équations différentielles,
    ///     produit scalaire dans l'espace. Il n'en étudie aucune ;
    ///   — un élève de terminale TECHNOLOGIQUE recevait les mêmes ;
    ///   — et leurs propres programmes, tous deux OBLIGATOIRES et
    ///     évalués au baccalauréat, n'existaient nulle part.
    ///
    /// Ce cas touchait plus d'élèves que celui de la spécialité de maths
    /// en voie générale : les maths y sont obligatoires pour TOUS, alors
    /// qu'elles ont quitté le tronc commun général en 2019.
    ///
    /// 109 compétences ont été ajoutées, et `VoiesScolaires` sait
    /// désormais à quelle voie une classe appartient. La règle a un
    /// REPLI : l'héritage par rang continue de s'appliquer partout où une
    /// voie n'a pas de référentiel propre, faute de quoi ces six classes
    /// auraient tout perdu ailleurs qu'en mathématiques.
    ///
    /// UN PIÈGE DE DATE, ÉVITÉ DE JUSTESSE. Le BO n° 14 du 2 avril 2026
    /// porte SEPT programmes de mathématiques, et ils n'entrent pas tous
    /// en vigueur la même année :
    ///
    ///   rentrée 2026 (maintenant)  2de GT, spécialité de 1re générale,
    ///                              maths dans l'enseignement scientifique
    ///                              de 1re, et 1re TECHNOLOGIQUE ;
    ///   rentrée 2027               spécialité de Tle générale, maths
    ///                              complémentaires, et Tle TECHNOLOGIQUE.
    ///
    /// La première technologique suit donc le texte de 2026 ; la terminale
    /// technologique suit ENCORE celui de 2019. Prendre le même BO pour
    /// les deux aurait donné un programme d'avance à la terminale.
    ///
    /// CE QUI RESTE À FAIRE SUR CE SUJET :
    ///
    ///   — les MATHS COMPLÉMENTAIRES de terminale générale (option 3 h,
    ///     ouverte à tout élève ne présentant pas la spécialité au bac,
    ///     qu'il l'ait suivie ou non en première) n'ont aucune compétence.
    ///     Un terminale général sans spécialité voit donc encore le
    ///     programme de la spécialité. Il faudra pour cela savoir, ÉLÈVE
    ///     PAR ÉLÈVE, s'il l'a gardée — la voie ne suffit plus, c'est un
    ///     choix. Même besoin pour la LV2 ;
    ///   — le PROGRAMME COMPLÉMENTAIRE de terminale professionnelle,
    ///     destiné à la poursuite d'études, n'a pas été dépouillé ;
    ///   — les 109 compétences ajoutées ici n'ont pas de prérequis, et
    ///     aucun professeur ne les a relues.
    ///
    /// ---------------------------------------------------------------
    ///
    /// QUATRIÈME PASSE, LE 14 SEPTEMBRE 2026 : LES SÉRIES TECHNOLOGIQUES.
    ///
    /// Un élève de première technologique n'avait ni série ni spécialités :
    /// hors maths, il recevait les programmes de la voie générale. Camara :
    /// « ils doivent absolument être présents », puis, le même jour, « il
    /// faut rester dans les classes les plus connues ». Des séries, une
    /// classe par année : STMG, ST2S, STL. La STI2D a été retirée le même
    /// jour : sa recherche de programmes n'a rien donné, et Camara préfère
    /// ne rien montrer qu'une matière sans référentiel.
    ///
    ///   SÉRIE / PROGRAMMES         TEXTE                    EN VIGUEUR DEPUIS
    ///   ------------------------------------------------------------------
    ///   STMG, première             MENE1901646A, BO spé 1   rentrée 2019
    ///   STMG, terminale            MENE1921262A, BO spé 8   rentrée 2020
    ///                              modifié par MENE2401678A (droit, 2024)
    ///   ST2S, première             MENE1901642A, BO spé 1   rentrée 2019
    ///   ST2S, terminale            MENE1921258A, BO spé 8   rentrée 2020
    ///   STL, première              MENE1901645A, BO spé 1   rentrée 2019
    ///   STL, terminale             MENE1921260A, BO spé 8   rentrée 2020
    ///   Tronc commun techno :
    ///     français 1re             MENE1901575A, annexe 2   rentrée 2019
    ///     histoire-géo 1re / Tle   MENE1901577A an. 3 /     rentrée 2019 /
    ///                              MENE1921243A an. 2       rentrée 2020
    ///     EMC                      MENE2413934A (2024)      1re 2025, Tle 2026
    ///     philosophie Tle          MENE1921238A, annexe 2   rentrée 2020
    ///
    /// Provenance détaillée — annexes, URL, dates de lecture, écarts — en tête
    /// de chaque fichier de Referentiels/Techno/. Les options de terminale
    /// (STMG) et les spécialités au choix (STL) ne sont pas des classes : elles
    /// se rangent dans la matière, nommées dans le domaine.
    ///
    /// L'ESPAGNOL EN LV2, LE MÊME JOUR : 132 compétences, de la 5e à la
    /// terminale, d'après l'arrêté du 5-5-2025 (MENE2504621A, annexes 9 et
    /// 10 ; 5e, 1re et Tle en 2026, 4e en 2027, 3e en 2028, et la SECONDE dès
    /// 2025 — un an plus tôt que ne le disait la ligne « Lycée GT » du tableau
    /// du 5 septembre). Un choix de la famille : `Eleve.Lv2Espagnol`, proposé
    /// dans les seules classes où une LV2 existe. La 6e bilangue et la voie
    /// professionnelle n'y sont pas. Provenance en tête de ReferentielEspagnol.cs.
    ///
    /// LES SPÉCIALITÉS DE LA VOIE GÉNÉRALE, LE MÊME JOUR — voulues par Camara :
    /// « il faut absolument que les terminales que je gère puissent préparer le
    /// bac ». Un choix de la famille (`Eleve.Specialites`), trois en première,
    /// deux en terminale. Chaque programme a été lu dans son arrêté, première et
    /// terminale, avec la note de service de son épreuve : provenance en tête de
    /// chaque fichier de Referentiels/Generale/. Elles vont aux professeurs qui
    /// les enseignent au lycée : l'HGGSP à Salim, l'HLP à Camille, les SES à
    /// Karim, les LLCER d'anglais et l'AMC à Marine, la LLCER
    /// d'espagnol à Lucía, les sciences de l'ingénieur à Yann, la LLCA latin et
    /// grec à Adrien. Deux nouveaux
    /// visages : Théo pour l'EPPCS, Jeanne pour les sept enseignements artistiques.
    /// Puis un troisième le 15/09/2026 : Minamba pour la NSI, d'abord confiée à Nora.
    /// Les lignes de pratique physique ou artistique portent un domaine
    /// « Pratique — » : un tuteur vocal ne les fait pas travailler, et elles ne
    /// comptent pas dans la barre de préparation de l'écrit.
    ///
    /// CE QUI RESTE : ces compétences n'ont pas de prérequis, aucun professeur
    /// ne les a relues ; les langues vivantes techno suivent le programme
    /// commun du lycée et n'ont rien de propre. Relevé en chemin, hors du
    /// sujet : l'EMC de la voie GÉNÉRALE du référentiel est encore celle de
    /// 2019, alors que le programme de 2024 l'a remplacée.
    /// </summary>
    public static class ReferentielSeeder
    {
        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            await SeedNiveauxAsync(context, ct);
            await SeedMatieresAsync(context, ct);
            await SeedAcademiesAsync(context, ct);
            await SeedPeriodesVacancesAsync(context, ct);

            await SeedCompetencesAsync(context, "MATHS", ReferentielMaths.Competences, ct);

            // Le tronc commun de la voie technologique rejoint le fichier de sa
            await SeedCompetencesAsync(context, "FRANCAIS",
                ReferentielFrancais.Competences
                    .Concat(ReferentielsSeriesTechnologiques.Francais)
                    .ToArray(), ct);
            await SeedCompetencesAsync(context, "HISTOIRE_GEO",
                ReferentielHistoireGeo.Competences
                    .Concat(ReferentielsSeriesTechnologiques.HistoireGeo)
                    .ToArray(), ct);
            await SeedCompetencesAsync(context, "ANGLAIS", ReferentielAnglais.Competences, ct);
            await SeedCompetencesAsync(context, "ESPAGNOL", ReferentielEspagnol.Competences, ct);
            await SeedCompetencesAsync(context, "SCIENCES", ReferentielSciences.Competences, ct);
            // La physique-chimie des séries technologiques rejoint celle de la
            // voie générale DANS LE MÊME APPEL : le semeur rend obsolète toute
            // ligne de la matière absente de ce qu'on lui passe.
            await SeedCompetencesAsync(context, "PHYSIQUE_CHIMIE",
                ReferentielPhysiqueChimie.Competences
                    .Concat(ReferentielsSeriesTechnologiques.PhysiqueChimie)
                    .ToArray(), ct);
            await SeedCompetencesAsync(context, "SVT", ReferentielSvt.Competences, ct);
            await SeedCompetencesAsync(context, "PHILOSOPHIE",
                ReferentielPhilosophie.Competences
                    .Concat(ReferentielsSeriesTechnologiques.Philosophie)
                    .ToArray(), ct);

            // LES SPÉCIALITÉS DES SÉRIES TECHNOLOGIQUES — voir la quatrième passe
            // en tête de ce fichier, et `ReferentielsSeriesTechnologiques` pour la
            // répartition entre professeurs.
            await SeedCompetencesAsync(context, "SCIENCES_GESTION", ReferentielsSeriesTechnologiques.SciencesGestion, ct);
            await SeedCompetencesAsync(context, "MANAGEMENT", ReferentielsSeriesTechnologiques.Management, ct);
            await SeedCompetencesAsync(context, "DROIT_ECONOMIE", ReferentielsSeriesTechnologiques.DroitEconomie, ct);
            await SeedCompetencesAsync(context, "SANITAIRE_SOCIAL", ReferentielsSeriesTechnologiques.SanitaireSocial, ct);
            await SeedCompetencesAsync(context, "BIOLOGIE_HUMAINE", ReferentielsSeriesTechnologiques.BiologieHumaine, ct);
            await SeedCompetencesAsync(context, "BIOTECHNOLOGIES", ReferentielsSeriesTechnologiques.Biotechnologies, ct);
            await SeedCompetencesAsync(context, "SPCL", ReferentielsSeriesTechnologiques.Spcl, ct);

            // LES SPÉCIALITÉS DE LA VOIE GÉNÉRALE — une matière chacune, ouverte
            // seulement à l'élève dont la famille l'a cochée (`VoiesScolaires`).
            await SeedCompetencesAsync(context, "HGGSP",
                ReferentielHggsp.HGGSP1.Concat(ReferentielHggsp.HGGSPT).ToArray(), ct);
            await SeedCompetencesAsync(context, "HLP",
                ReferentielHlp.HLP1.Concat(ReferentielHlp.HLPT).ToArray(), ct);
            await SeedCompetencesAsync(context, "SES",
                ReferentielSes.SES1.Concat(ReferentielSes.SEST).ToArray(), ct);
            await SeedCompetencesAsync(context, "NSI",
                ReferentielNsi.NSI1.Concat(ReferentielNsi.NSIT).ToArray(), ct);
            await SeedCompetencesAsync(context, "LLCER_ANGLAIS",
                ReferentielLlcerAnglais.LLCERAN1.Concat(ReferentielLlcerAnglais.LLCERANT).ToArray(), ct);
            await SeedCompetencesAsync(context, "AMC",
                ReferentielLlcerAnglais.AMC1.Concat(ReferentielLlcerAnglais.AMCT).ToArray(), ct);
            await SeedCompetencesAsync(context, "LLCER_ESPAGNOL",
                ReferentielLlcerEspagnol.LLCERES1.Concat(ReferentielLlcerEspagnol.LLCEREST).ToArray(), ct);
            await SeedCompetencesAsync(context, "SI",
                ReferentielSi.SI1.Concat(ReferentielSi.SIT).ToArray(), ct);
            await SeedCompetencesAsync(context, "EPPCS",
                ReferentielEppcs.EPPCS1.Concat(ReferentielEppcs.EPPCST).ToArray(), ct);
            await SeedCompetencesAsync(context, "ARTS_PLASTIQUES",
                ReferentielArtsVisuels.ARTSPLA1.Concat(ReferentielArtsVisuels.ARTSPLAT).ToArray(), ct);
            await SeedCompetencesAsync(context, "HISTOIRE_ARTS",
                ReferentielArtsVisuels.HDA1.Concat(ReferentielArtsVisuels.HDAT).ToArray(), ct);
            await SeedCompetencesAsync(context, "CINEMA_AUDIOVISUEL",
                ReferentielArtsVisuels.CAV1.Concat(ReferentielArtsVisuels.CAVT).ToArray(), ct);
            await SeedCompetencesAsync(context, "MUSIQUE",
                ReferentielArtsSpectacleVivant.MUS1.Concat(ReferentielArtsSpectacleVivant.MUST).ToArray(), ct);
            await SeedCompetencesAsync(context, "THEATRE",
                ReferentielArtsSpectacleVivant.THEA1.Concat(ReferentielArtsSpectacleVivant.THEAT).ToArray(), ct);
            await SeedCompetencesAsync(context, "DANSE",
                ReferentielArtsSpectacleVivant.DANSE1.Concat(ReferentielArtsSpectacleVivant.DANSET).ToArray(), ct);
            await SeedCompetencesAsync(context, "LLCA_LATIN",
                ReferentielLlca.LATIN1.Concat(ReferentielLlca.LATINT).ToArray(), ct);
            await SeedCompetencesAsync(context, "LLCA_GREC",
                ReferentielLlca.GREC1.Concat(ReferentielLlca.GRECT).ToArray(), ct);
            await SeedCompetencesAsync(context, "ARTS_CIRQUE",
                ReferentielArtsSpectacleVivant.CIRQUE1.Concat(ReferentielArtsSpectacleVivant.CIRQUET).ToArray(), ct);

            // Les arêtes en dernier, et toutes ensemble : un prérequis peut
            // pointer vers une autre matière — la proportionnalité des maths
            // conditionne les calculs de concentration en physique-chimie — et
            // il faut donc que TOUTES les compétences existent avant de relier.
            await SeedPrerequisAsync(
                context,
                ReferentielMaths.Prerequis
                    .Concat(ReferentielFrancais.Prerequis)
                    .Concat(ReferentielHistoireGeo.Prerequis)
                    .Concat(ReferentielAnglais.Prerequis)
                    .Concat(ReferentielSciences.Prerequis)
                    .Concat(ReferentielPhysiqueChimie.Prerequis)
                    .Concat(ReferentielSvt.Prerequis)
                    .Concat(ReferentielPhilosophie.Prerequis)
                    .ToArray(),
                ct);
        }

        // ------------------------------------------------------------------
        // Niveaux scolaires : CP → Terminale
        // ------------------------------------------------------------------
        private static async Task SeedNiveauxAsync(SchoolWebAppDatabaseContext context, CancellationToken ct)
        {
            var niveaux = new (string Code, string Libelle, string Cycle, int Ordre)[]
            {
                ("CP",         "CP",          "Primaire", 1),
                ("CE1",        "CE1",         "Primaire", 2),
                ("CE2",        "CE2",         "Primaire", 3),
                ("CM1",        "CM1",         "Primaire", 4),
                ("CM2",        "CM2",         "Primaire", 5),
                ("SIXIEME",    "6e",          "College",  6),
                ("CINQUIEME",  "5e",          "College",  7),
                ("QUATRIEME",  "4e",          "College",  8),
                ("TROISIEME",  "3e",          "College",  9),

                // LA VOIE EST UNE ÉTIQUETTE, L'ORDRE RESTE L'ANNÉE.
                // -----------------------------------------------
                // Trois classes partagent le rang 11 : première générale,
                // technologique, professionnelle. C'est voulu, et c'est ce qui
                // rend l'ajout gratuit.
                //
                // `Ordre` est le RANG D'ANNÉE, et toute la machinerie
                // pédagogique en dépend : le rattachement des compétences
                // cherche celles du rang de l'élève, cinq ans en amont et un an
                // en aval ; les bornes des matières s'expriment en rangs. En
                // partageant le rang, une première professionnelle hérite sans
                // rien écrire du référentiel, de la mémoire longue et de la
                // remontée sur les lacunes de collège.
                //
                // Ce qui change, c'est le LIBELLÉ — et il compte : le
                // professeur le reçoit tel quel dans sa consigne (« Classe :
                // Première professionnelle ») et adapte son registre et ses
                // exemples. Seul `Code` est unique en base ; `Ordre` ne l'est
                // pas, rien ne s'oppose au partage.
                //
                // POURQUOI PAS DE « SECONDE TECHNOLOGIQUE ».
                // Elle n'existe pas. Après la troisième, on entre en seconde
                // GÉNÉRALE ET TECHNOLOGIQUE — une seule classe, le choix de la
                // voie ne se fait qu'en première — ou en seconde
                // professionnelle. Inventer une classe pour faire joli dans une
                // liste déroutera le parent qui ne la trouve pas sur le bulletin
                // de son enfant.
                ("TROISIEME_PREPA",   "3e prépa-métiers",                 "College", 9),

                ("SECONDE",           "Seconde générale et technologique", "Lycee",  10),
                ("SECONDE_PRO",       "Seconde professionnelle",           "Lycee",  10),

                ("PREMIERE",          "Première générale",                 "Lycee",  11),
                ("PREMIERE_TECHNO",   "Première technologique",            "Lycee",  11),
                ("PREMIERE_PRO",      "Première professionnelle",          "Lycee",  11),

                ("TERMINALE",         "Terminale générale",                "Lycee",  12),
                ("TERMINALE_TECHNO",  "Terminale technologique",           "Lycee",  12),
                ("TERMINALE_PRO",     "Terminale professionnelle",         "Lycee",  12),

                // LES SÉRIES TECHNOLOGIQUES LES PLUS SUIVIES — voulues par Camara le
                // 14/09/2026, ramenées le même jour à quatre séries et une classe
                // par année : « il faut rester dans les classes les plus connues ».
                // Pas de classe par option : l'option de terminale se range dans la
                // matière. Le libellé est celui du bulletin, et celui que le
                // professeur lit dans sa consigne. Voir `VoiesScolaires`.
                ("PREMIERE_STMG",   "Première STMG",   "Lycee", 11),
                ("TERMINALE_STMG",  "Terminale STMG",  "Lycee", 12),
                ("PREMIERE_ST2S",   "Première ST2S",   "Lycee", 11),
                ("TERMINALE_ST2S",  "Terminale ST2S",  "Lycee", 12),
                ("PREMIERE_STL",    "Première STL",    "Lycee", 11),
                ("TERMINALE_STL",   "Terminale STL",   "Lycee", 12),
            };

            // Libellés d'origine, remplacés depuis.
            //
            // MÊME PRÉCAUTION QUE POUR LES MATIÈRES : on ne réaligne QUE si la
            // base porte encore la valeur qu'on y avait posée. « Seconde » est
            // devenue « Seconde générale et technologique » le jour où les voies
            // sont apparues ; sans cette table, les bases déjà semées gardaient
            // l'ancien libellé et un élève de seconde professionnelle se serait
            // vu proposer une classe nommée « Seconde » à côté de la sienne.
            //
            // Les élèves déjà inscrits ne bougent pas : le CODE ne change pas,
            // seul le libellé. Personne n'est à migrer.
            var libellesNiveauxRemplaces = new Dictionary<string, string>
            {
                ["SECONDE"]   = "Seconde",
                ["PREMIERE"]  = "Première",
                ["TERMINALE"] = "Terminale",
            };

            // Les deux classes technologiques sans série deviennent le TRONC
            // COMMUN de la voie : elles ne se choisissent plus, et leur libellé
            // le dit — à l'élève qui y est encore, comme dans l'administration.
            var libellesTechno = new Dictionary<string, (string Ancien, string Nouveau)>
            {
                ["PREMIERE_TECHNO"]  = ("Première technologique",  "Première technologique — série à préciser"),
                ["TERMINALE_TECHNO"] = ("Terminale technologique", "Terminale technologique — série à préciser"),
            };

            var enBase = await context.NiveauxScolaires.ToListAsync(ct);
            var parCode = enBase
                .Where(n => n.Code is not null)
                .ToDictionary(n => n.Code!);

            var aAjouter = niveaux
                .Where(n => !parCode.ContainsKey(n.Code))
                .Select(n => new NiveauScolaire
                {
                    Code = n.Code,
                    Libelle = n.Libelle,
                    Cycle = n.Cycle,
                    Ordre = n.Ordre
                })
                .ToList();

            if (aAjouter.Count > 0)
            {
                context.NiveauxScolaires.AddRange(aAjouter);
            }

            foreach (var n in niveaux)
            {
                if (!parCode.TryGetValue(n.Code, out var existant)) continue;

                if (libellesNiveauxRemplaces.TryGetValue(n.Code, out var ancien)
                    && string.Equals(existant.Libelle, ancien, StringComparison.Ordinal))
                {
                    existant.Libelle = n.Libelle;
                }

                if (libellesTechno.TryGetValue(n.Code, out var techno)
                    && string.Equals(existant.Libelle, techno.Ancien, StringComparison.Ordinal))
                {
                    existant.Libelle = techno.Nouveau;
                }
            }

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync(ct);
            }
        }

        // ------------------------------------------------------------------
        // Académies — table de référence, pilote la zone de vacances
        // ------------------------------------------------------------------
        private static async Task SeedAcademiesAsync(SchoolWebAppDatabaseContext context, CancellationToken ct)
        {
            var enBase = await context.Academies.ToListAsync(ct);
            var parCode = enBase.ToDictionary(a => a.Code);

            var aAjouter = ReferentielVacancesScolaires.Academies
                .Where(a => !parCode.ContainsKey(a.Code))
                .Select(a => new Academie
                {
                    Code = a.Code,
                    Libelle = a.Libelle,
                    Zone = a.Zone,
                })
                .ToList();

            if (aAjouter.Count > 0)
            {
                context.Academies.AddRange(aAjouter);
                await context.SaveChangesAsync(ct);
            }
        }

        // ------------------------------------------------------------------
        // Périodes de vacances — une ligne par zone et par période. Voir
        // ReferentielVacancesScolaires.Periodes : volontairement vide tant
        // que les dates officielles de l'année en cours n'ont pas été
        // saisies à la main.
        // ------------------------------------------------------------------
        private static async Task SeedPeriodesVacancesAsync(SchoolWebAppDatabaseContext context, CancellationToken ct)
        {
            if (ReferentielVacancesScolaires.Periodes.Length == 0) return;

            // Identifiées par (zone, année scolaire, libellé) : pas de code
            // unique sur cette table, la clé naturelle suffit — une même
            // période ne se resaisit jamais deux fois pour la même année.
            var enBase = await context.PeriodesVacances.ToListAsync(ct);
            var existantes = enBase
                .Select(p => (p.Zone, p.AnneeScolaire, p.Libelle))
                .ToHashSet();

            var aAjouter = ReferentielVacancesScolaires.Periodes
                .Where(p => !existantes.Contains((p.Zone, p.AnneeScolaire, p.Libelle)))
                .Select(p => new PeriodeVacances
                {
                    Zone = p.Zone,
                    AnneeScolaire = p.AnneeScolaire,
                    Libelle = p.Libelle,
                    DateDebut = p.Debut,
                    DateFin = p.Fin,
                })
                .ToList();

            if (aAjouter.Count > 0)
            {
                context.PeriodesVacances.AddRange(aAjouter);
                await context.SaveChangesAsync(ct);
            }
        }

        // ------------------------------------------------------------------
        // Matières — une matière = un agent conversationnel
        // Seules les matières actives sont proposées à l'élève.
        // ------------------------------------------------------------------
        private static async Task SeedMatieresAsync(SchoolWebAppDatabaseContext context, CancellationToken ct)
        {
            // Un professeur par matière, avec un prénom, un visage et une couleur.
            // Pour un enfant, « l'agent maths » n'existe pas — Nora, si.
            //
            // LES DEUX DERNIÈRES COLONNES BORNENT LE NIVEAU (Ordre, incluses).
            // ---------------------------------------------------------------
            // Une matière n'existe pas partout, et « Sciences » est le cas
            // d'école : c'est UNE matière jusqu'à la 6e — « Sciences et
            // technologie », une seule note — puis elle se scinde en SVT et
            // physique-chimie dès la 5e, deux professeurs et deux notes. Le
            // découpage suit donc l'école, pas notre commodité : un CM1 ne doit
            // pas voir « Physique-Chimie », un 4e ne doit pas voir « Sciences ».
            //
            // L'histoire-géo commence au cycle 3 : avant le CM1, c'est
            // « Questionner le monde », qui n'est pas la même matière.
            var matieres = new (string Code, string Libelle, string AgentSlug,
                                string Prenom, string Avatar, string Couleur,
                                int Ordre, bool Active, int NiveauMin, int NiveauMax,
                                string Promesse)[]
            {
                ("MATHS",           "Mathématiques",          "agent-maths",           "Nora",   "nora",   "#0E7C7B", 1, true, 1,  12, "Calculs, problèmes et géométrie"),
                // Le français s'arrête en première : les épreuves anticipées ont
                // lieu en fin de 1re, et la terminale générale et technologique
                // n'a plus de cours de français. Aller jusqu'à 12 proposerait à
                // un élève de terminale une matière qu'il n'a plus.
                // Le français va jusqu au rang 12, mais PAS dans toutes les
                // classes de ce rang : les épreuves anticipées closent la
                // matière en fin de première dans les voies générale et
                // technologique, tandis que la terminale professionnelle en a
                // jusqu au bac. Les deux exclusions sont dans `VoiesScolaires`
                // — les bornes de rang ne savent pas distinguer trois classes
                // qui partagent la même année.
                ("FRANCAIS",        "Français",               "agent-francais",        "Adrien", "adrien", "#B4531F", 2, true, 1,  12, "Lecture, écriture et grammaire"),
                ("HISTOIRE_GEO",    "Histoire-Géographie",    "agent-histoire-geo",    "Salim",  "salim",  "#99277F", 3, true, 4,  12, "Le passé, les cartes et le monde"),
                ("ANGLAIS",         "Anglais",                "agent-anglais",         "Marine", "marine", "#1D6FB8", 4, true, 1,  12, "Parler, comprendre et écrire"),
                ("SCIENCES",        "Sciences et technologie","agent-sciences",        "Yann",   "yann",   "#6B4400", 5, true, 1,  6, "Le vivant, la matière et les expériences"),
                ("PHYSIQUE_CHIMIE", "Physique-Chimie",        "agent-physique-chimie", "Yann",   "yann",   "#6B4400", 6, true, 7,  12, "Mesures, réactions et électricité"),
                ("SVT",             "SVT",                    "agent-svt",             "Inès",   "ines",   "#5C7F14", 7, true, 7,  12, "Le vivant, le corps et la Terre"),

                // LA PHILOSOPHIE N EXISTE QU EN TERMINALE, ET PAS PARTOUT.
                //
                // Bornée au seul rang 12, et exclue de la terminale
                // professionnelle par `VoiesScolaires` : le bac pro n a pas de
                // philosophie. Toute autre terminale en a — c est même la seule
                // matière que personne ne peut éviter, et celle dont l épreuve
                // pèse le plus lourd en voie générale.
                ("PHILOSOPHIE",     "Philosophie",            "agent-philosophie",     "Camille", "camille", "#5A4A8C", 8, true, 12, 12, "Problématiser, argumenter, disserter"),

                // L'ESPAGNOL EN LV2 (14/09/2026) — un CHOIX de la famille, pas une
                // classe. Bornes de la 5e à la terminale ; `VoiesScolaires` ne
                // l'ouvre qu'aux classes où une LV2 existe ET à l'élève dont le
                // parent a coché la case. Voulu par Camara : « un simple checkbox
                // permet de dire que l'enfant a espagnol en LV2 ».
                ("ESPAGNOL",        "Espagnol",               "agent-espagnol",        "Lucía",  "lucia",  "#BE123C", 5, true, 7,  12, "Parler, comprendre et découvrir le monde hispanique"),

                // LES SPÉCIALITÉS DES QUATRE SÉRIES TECHNOLOGIQUES — 14/09/2026.
                //
                // Chacune est RÉSERVÉE à sa série par `VoiesScolaires` : les
                // bornes de rang disent seulement « première et terminale ». Un
                // professeur par famille de métier, comme dans un lycée : Karim
                // tient l'économie-gestion de STMG, Élodie le sanitaire et social
                // de ST2S ; Yann et Inès gardent les sciences qu'ils enseignent
                // déjà.
                //
                // AUCUNE MATIÈRE SANS RÉFÉRENTIEL — Camara, le 14/09/2026. La STI2D
                // et « physique-chimie et mathématiques » (STI2D et STL) n'ont pas
                // de programme vérifié : elles ne sont pas semées.
                ("SCIENCES_GESTION",      "Sciences de gestion et numérique",                "agent-sciences-gestion",      "Karim",  "karim",  "#166534",  9, true, 11, 12, "Organisations, données et décisions de gestion"),
                ("MANAGEMENT",            "Management",                                      "agent-management",            "Karim",  "karim",  "#166534", 10, true, 11, 12, "Comprendre et décider dans une organisation"),
                ("DROIT_ECONOMIE",        "Droit et économie",                               "agent-droit-economie",        "Karim",  "karim",  "#166534", 11, true, 11, 12, "Les règles du droit et les mécanismes économiques"),
                ("SANITAIRE_SOCIAL",      "Sciences et techniques sanitaires et sociales",   "agent-sanitaire-social",      "Élodie", "elodie", "#B91C1C", 14, true, 11, 12, "Santé, bien-être et cohésion sociale"),
                ("BIOLOGIE_HUMAINE",      "Biologie et physiopathologie humaines",           "agent-biologie-humaine",      "Inès",   "ines",   "#5C7F14", 15, true, 11, 12, "Le corps humain, la santé et la maladie"),
                ("BIOTECHNOLOGIES",       "Biochimie, biologie et biotechnologies",          "agent-biotechnologies",       "Inès",   "ines",   "#5C7F14", 16, true, 11, 12, "Le vivant étudié au laboratoire"),
                ("SPCL",                  "Sciences physiques et chimiques en laboratoire",  "agent-spcl",                  "Yann",   "yann",   "#6B4400", 17, true, 11, 12, "Mesurer, analyser et synthétiser au laboratoire"),

                // LES SPÉCIALITÉS DE LA VOIE GÉNÉRALE — 14/09/2026. Un CHOIX de la
                // famille : `VoiesScolaires` ne les ouvre qu'en première et
                // terminale générales, et seulement à l'élève qui les a cochées.
                // Chacune va au professeur qui l'enseigne au lycée : un élève
                // retrouve Salim en HGGSP, Camille en HLP, Karim en SES. Maths,
                // physique-chimie et SVT gardent leur matière.
                ("HGGSP",                 "Histoire-géographie, géopolitique et sciences politiques", "agent-hggsp", "Salim",   "salim",   "#99277F", 18, true, 11, 12, "Comprendre les grands enjeux du monde contemporain"),
                ("HLP",                   "Humanités, littérature et philosophie",           "agent-hlp",                   "Camille", "camille", "#5A4A8C", 19, true, 11, 12, "Lire, interpréter et penser les grandes questions humaines"),
                ("SES",                   "Sciences économiques et sociales",                "agent-ses",                   "Karim",   "karim",   "#166534", 20, true, 11, 12, "L'économie, la société et le politique"),
                // LA NSI A SON PROFESSEUR — Camara, le 15/09/2026 : « c'est une
                // matière à part entière, on ne peut pas la confier à un
                // professeur qui n'a rien à voir ». Confiée à Nora jusque-là.
                // Orange, choisi par Camara comme la veste de son portrait ; la
                // version foncée, pour tenir 4,5:1 sur fond clair — voir
                // `couleurMatiere.js`.
                ("NSI",                   "Numérique et sciences informatiques",             "agent-nsi",                   "Minamba", "minamba", "#AA5F04", 21, true, 11, 12, "Programmer, structurer les données, comprendre les réseaux"),
                ("LLCER_ANGLAIS",         "Langues, littératures et cultures étrangères — anglais", "agent-llcer-anglais", "Marine", "marine", "#1D6FB8", 22, true, 11, 12, "La littérature et les cultures du monde anglophone"),
                ("AMC",                   "LLCER anglais, monde contemporain (AMC)",         "agent-amc",                   "Marine",  "marine",  "#1D6FB8", 23, true, 11, 12, "Les grands enjeux du monde anglophone d'aujourd'hui"),
                ("LLCER_ESPAGNOL",        "Langues, littératures et cultures étrangères — espagnol", "agent-llcer-espagnol", "Lucía", "lucia", "#BE123C", 24, true, 11, 12, "La littérature et les cultures du monde hispanophone"),
                ("LLCA_LATIN",            "Littérature, langues et cultures de l'Antiquité — latin", "agent-llca-latin", "Adrien", "adrien", "#B4531F", 34, true, 11, 12, "Lire, traduire et comprendre les textes latins"),
                ("LLCA_GREC",             "Littérature, langues et cultures de l'Antiquité — grec", "agent-llca-grec", "Adrien", "adrien", "#B4531F", 35, true, 11, 12, "Lire, traduire et comprendre les textes grecs"),
                ("SI",                    "Sciences de l'ingénieur",                         "agent-si",                    "Yann",    "yann",    "#6B4400", 25, true, 11, 12, "Analyser, modéliser et faire évoluer un système"),

                // DEUX NOUVEAUX VISAGES, faute de professeur déjà là pour ces
                // enseignements : Théo pour l'EPPCS, Jeanne pour les arts. Un seul
                // professeur pour les sept arts : l'élève n'en suit qu'un.
                ("EPPCS",                 "Éducation physique, pratiques et culture sportives", "agent-eppcs",              "Théo",    "theo",    "#C2410C", 26, true, 11, 12, "Le sport, le corps et la société"),
                ("ARTS_PLASTIQUES",       "Arts plastiques",                                 "agent-arts-plastiques",       "Jeanne",  "jeanne",  "#86198F", 27, true, 11, 12, "Regarder, analyser et penser les œuvres"),
                ("HISTOIRE_ARTS",         "Histoire des arts",                               "agent-histoire-arts",         "Jeanne",  "jeanne",  "#86198F", 28, true, 11, 12, "Les œuvres dans leur histoire"),
                ("CINEMA_AUDIOVISUEL",    "Cinéma-audiovisuel",                              "agent-cinema-audiovisuel",    "Jeanne",  "jeanne",  "#86198F", 29, true, 11, 12, "Analyser et écrire le cinéma"),
                ("MUSIQUE",               "Musique",                                         "agent-musique",               "Jeanne",  "jeanne",  "#86198F", 30, true, 11, 12, "Écouter, analyser et comprendre la musique"),
                ("THEATRE",               "Théâtre",                                         "agent-theatre",               "Jeanne",  "jeanne",  "#86198F", 31, true, 11, 12, "Le texte, la scène et le spectateur"),
                ("DANSE",                 "Danse",                                           "agent-danse",                 "Jeanne",  "jeanne",  "#86198F", 32, true, 11, 12, "Le corps, la création et les œuvres chorégraphiques"),
                ("ARTS_CIRQUE",           "Arts du cirque",                                  "agent-arts-cirque",           "Jeanne",  "jeanne",  "#86198F", 33, true, 11, 12, "Les disciplines, la création et les spectacles de cirque"),
            };

            // Libellés d'origine, remplacés depuis. Même précaution que pour les
            // couleurs : on ne réaligne que si la valeur en base est encore
            // celle qu'on avait posée.
            var libellesRemplaces = new Dictionary<string, string>
            {
                ["SCIENCES"] = "Sciences",
                // Nommée comme son texte le 14/09/2026 : c'est une LLCER, pas l'anglais
                // du tronc commun.
                ["AMC"] = "Anglais, monde contemporain",
            };

            // Prénoms de professeurs d'origine, remplacés depuis.
            //
            // Même précaution que pour les libellés et les couleurs : on ne
            // réaligne QUE si la base porte encore l'ancien prénom. Un prénom
            // choisi ailleurs n'est pas écrasé par un redémarrage.
            //
            // Sans cette table, un changement de prénom ne quittait jamais la
            // machine où il avait été fait : le seeder complète un professeur
            // absent, mais ne touche jamais à un professeur déjà nommé. La
            // professeure d'anglais s'appelait donc Marine en développement et
            // Chloé en production — deux écoles différentes selon le serveur.
            var prenomsRemplaces = new Dictionary<string, string>
            {
                ["ANGLAIS"] = "Chloé",
                // Pas un renommage : la NSI change de professeur (15/09/2026).
                ["NSI"] = "Nora",
            };

            // Couleurs d'origine de deux matières, remplacées depuis.
            //
            // Elles avaient été choisies une par une, chacune sur son propre
            // écran, où elles allaient très bien. Le jour où le nom de la
            // matière a pris sa couleur dans les listes de la fiche élève,
            // elles se sont retrouvées côte à côte : le vert des sciences se
            // confondait avec le sarcelle de Nora, le violet de l'histoire-géo
            // avec le bleu de Marine. Les deux matières n'étant pas encore
            // ouvertes, on réaligne — mais uniquement si la valeur en base est
            // encore celle qu'on avait posée. Une couleur choisie depuis
            // l'administration ne doit pas être écrasée par un redémarrage.
            // Plusieurs valeurs possibles par matière : sciences est passée par
            // deux teintes successives, et une base peut être restée sur l'une
            // ou l'autre. Le vert d'origine se confondait avec le sarcelle de
            // Nora, d'où l'olive ; l'olive est ensuite partie à la SVT, qui en
            // est la continuité — la biologie du cycle 3 devient la SVT du
            // cycle 4 — et Yann a pris l'ambre, qu'il garde en physique-chimie.
            // Un professeur, une couleur, quel que soit le niveau.
            var couleursRemplacees = new Dictionary<string, string[]>
            {
                ["HISTOIRE_GEO"] = new[] { "#7A3E9D" },
                ["SCIENCES"] = new[] { "#2E7D32", "#5C7F14" },
                // Le sarcelle de Nora, que la NSI portait avant d'avoir son
                // professeur, puis le violet essayé le même jour pour Minamba.
                ["NSI"] = new[] { "#0E7C7B", "#8158DA" },
            };

            var existants = await context.Matieres.ToListAsync(ct);

            // LES MATIÈRES RETIRÉES SE FERMENT EN BASE. Semées un temps, puis
            // retirées faute de référentiel (14/09/2026). Retirer leur ligne
            // de la table ci-dessus ne suffisait pas : le semeur ne touche
            // jamais une matière qu'il ne connaît plus, et elle restait
            // ouverte. `VoiesScolaires` les écarte aussi de toute grille.
            foreach (var retiree in existants.Where(x =>
                         x.Code is "INGENIERIE" or "PHYSIQUE_CHIMIE_MATHS" && x.Active))
            {
                retiree.Active = false;
            }

            foreach (var m in matieres)
            {
                var existant = existants.FirstOrDefault(x => x.Code == m.Code);

                if (existant is null)
                {
                    context.Matieres.Add(new Matiere
                    {
                        Code = m.Code,
                        Libelle = m.Libelle,
                        AgentSlug = m.AgentSlug,
                        ProfPrenom = m.Prenom,
                        Promesse = m.Promesse,
                        ProfAvatar = m.Avatar,
                        ProfCouleur = m.Couleur,
                        Ordre = m.Ordre,
                        NiveauOrdreMin = m.NiveauMin,
                        NiveauOrdreMax = m.NiveauMax,
                        Active = m.Active
                    });

                    continue;
                }

                // CE QUI EST STRUCTUREL EST TOUJOURS RÉALIGNÉ.
                //
                // La plage de niveaux et l'ouverture d'une matière ne sont pas
                // des préférences : elles décrivent le système scolaire
                // français, et rien dans l'application ne permet de les
                // modifier. Les laisser dériver ne protégerait donc aucun
                // réglage — ça ne ferait que garder en base une description
                // fausse de l'école.
                //
                // Ce qui suit, en revanche, est cosmétique et peut avoir été
                // choisi ailleurs : prénom, visage et couleur gardent leur
                // protection.
                existant.NiveauOrdreMin = m.NiveauMin;

                // LA PROMESSE EST TOUJOURS RÉALIGNÉE, SANS PRÉCAUTION.
                //
                // Contrairement au prénom du professeur ou au libellé, elle
                // n'est jamais choisie ailleurs : personne ne la modifie depuis
                // l'administration, elle n'existe que dans cette table. La
                // protéger d'un écrasement ne protégerait rien, et laisserait
                // une matière déjà en base sans phrase pour toujours.
                existant.Promesse = m.Promesse;
                existant.NiveauOrdreMax = m.NiveauMax;
                existant.Active = m.Active;
                existant.AgentSlug = m.AgentSlug;
                existant.Ordre = m.Ordre;

                if (libellesRemplaces.TryGetValue(m.Code, out var ancienLibelle)
                    && string.Equals(existant.Libelle, ancienLibelle,
                                     StringComparison.OrdinalIgnoreCase))
                {
                    existant.Libelle = m.Libelle;
                }

                if (string.IsNullOrWhiteSpace(existant.ProfPrenom))
                {
                    // Matière créée avant l'ajout des professeurs : on complète,
                    // sans écraser ce qui aurait été personnalisé depuis.
                    existant.ProfPrenom = m.Prenom;
                    existant.ProfAvatar = m.Avatar;
                    existant.ProfCouleur = m.Couleur;
                }
                else if (prenomsRemplaces.TryGetValue(m.Code, out var ancienPrenom)
                         && string.Equals(existant.ProfPrenom, ancienPrenom,
                                          StringComparison.OrdinalIgnoreCase))
                {
                    // Le prénom en base est encore celui qu'on avait posé : on
                    // le réaligne, avec son visage — les deux vont ensemble,
                    // renommer sans changer l'avatar donnerait une Marine au
                    // visage de Chloé.
                    existant.ProfPrenom = m.Prenom;
                    existant.ProfAvatar = m.Avatar;
                }

                // LA COULEUR SE VÉRIFIE À PART, et non plus en `else` du prénom :
                // un nouveau professeur change À LA FOIS de prénom et de couleur
                // (la NSI, 15/09/2026). Chaîner les deux laissait Minamba en
                // sarcelle, la couleur de Nora. Même précaution : seule une
                // valeur encore identique à l'ancienne est réalignée.
                if (couleursRemplacees.TryGetValue(m.Code, out var anciennes)
                         && anciennes.Any(a => string.Equals(existant.ProfCouleur, a,
                                                             StringComparison.OrdinalIgnoreCase)))
                {
                    existant.ProfCouleur = m.Couleur;
                }
            }

            await context.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------
        // Compétences : un référentiel par matière, chacun dans son fichier.
        //
        // POURQUOI LE SEMIS EST DEVENU GÉNÉRIQUE
        // --------------------------------------
        // Il y avait ici une méthode pour les seules mathématiques, contenu et
        // mécanique mêlés : trois cents lignes de programme scolaire au milieu
        // du code qui les insère. Avec six matières, le fichier devenait
        // illisible et la moindre relecture d'un libellé obligeait à traverser
        // les autres. Le contenu vit désormais dans Referentiels/, une classe
        // par matière ; il ne reste ici que la mécanique, écrite une fois.
        //
        // LE FICHIER FAIT FOI — DANS LES DEUX SENS. Décidé par Camara le
        // 13/09/2026, en revenant sur « on n'enlève rien, on ajoute seulement »
        // du 5 septembre :
        //   - un code présent dans le fichier et absent de la base est AJOUTÉ ;
        //   - un code présent des deux côtés est MIS À JOUR (libellé, domaine,
        //     ordre, niveau) — une coquille corrigée dans le fichier atteint
        //     enfin la base, ce qu'elle ne faisait pas avant ;
        //   - un code présent en base et absent du fichier de SA matière
        //     devient OBSOLÈTE : `Actif = false`, jamais supprimé. Les
        //     maîtrises que des élèves y ont gagnées restent à eux ; les
        //     nouveaux élèves ne la voient plus. Voir `Competence.Actif`.
        //   - un code qui revient dans le fichier redevient actif.
        //
        // La conséquence pratique, à ne pas oublier en relisant un fichier de
        // Referentiels/ : RETIRER UNE LIGNE N'EST PLUS ANODIN. C'est le geste
        // qui sort une notion du programme pour tous les élèves à venir.
        // ------------------------------------------------------------------
        private static async Task SeedCompetencesAsync(
            SchoolWebAppDatabaseContext context,
            string codeMatiere,
            (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] competences,
            CancellationToken ct)
        {
            var matiere = await context.Matieres
                .FirstOrDefaultAsync(m => m.Code == codeMatiere, ct);

            if (matiere is null) return;

            // Tous les niveaux, et non une liste énumérée : les référentiels
            // vont du CP à la terminale, et une liste à tenir à jour en
            // parallèle du contenu est une occasion d'oubli de plus. Les
            // compétences dont le niveau manquerait en base sont simplement
            // écartées plus bas.
            var niveaux = await context.NiveauxScolaires
                .ToDictionaryAsync(n => n.Code!, n => n.Id, ct);

            if (niveaux.Count == 0) return;

            // Les lignes de CETTE matière, suivies : on va les modifier. Et la
            // liste de TOUS les codes, pour ne pas créer un doublon si un code
            // existait déjà sous une autre matière — le comportement d'avant.
            var enBase = await context.Competences
                .Where(c => c.MatiereId == matiere.Id)
                .ToListAsync(ct);

            var codesAilleurs = await context.Competences
                .Where(c => c.MatiereId != matiere.Id && c.Code != null)
                .Select(c => c.Code!)
                .ToHashSetAsync(ct);

            var parCode = enBase
                .Where(c => c.Code != null)
                .ToDictionary(c => c.Code!);

            var codesSemes = new HashSet<string>();
            var maintenant = DateTime.UtcNow;

            foreach (var c in competences)
            {
                // Un niveau absent de la base n'est pas une erreur : on écarte
                // ses compétences plutôt que de faire échouer tout le semis.
                if (!niveaux.ContainsKey(c.Niveau)) continue;

                codesSemes.Add(c.Code);

                if (parCode.TryGetValue(c.Code, out var existante))
                {
                    // DATÉE SEULEMENT SI QUELQUE CHOSE CHANGE. Le semis
                    // tourne à chaque démarrage : dater chaque passage ferait
                    // lire « modifiée ce matin » sur toutes les lignes, à
                    // chaque redémarrage — c'est-à-dire rien.
                    var change = existante.NiveauScolaireId != niveaux[c.Niveau]
                        || existante.Domaine != c.Domaine
                        || existante.Libelle != c.Libelle
                        || existante.Ordre != c.Ordre;

                    if (change)
                    {
                        existante.NiveauScolaireId = niveaux[c.Niveau];
                        existante.Domaine = c.Domaine;
                        existante.Libelle = c.Libelle;
                        existante.Ordre = c.Ordre;
                        existante.DateModification = maintenant;
                    }

                    if (!existante.Actif)
                    {
                        existante.Actif = true;
                        existante.DateFinValidite = null;
                        existante.DateModification = maintenant;
                    }

                    continue;
                }

                if (codesAilleurs.Contains(c.Code)) continue;

                context.Competences.Add(new Competence
                {
                    MatiereId = matiere.Id,
                    NiveauScolaireId = niveaux[c.Niveau],
                    Code = c.Code,
                    Domaine = c.Domaine,
                    Libelle = c.Libelle,
                    Ordre = c.Ordre,
                    DateCreation = maintenant,
                });
            }

            // Les lignes de la matière que le fichier ne cite plus. Une ligne
            // sans code ne vient pas d'un fichier : on ne touche pas à ce
            // qu'on ne connaît pas.
            foreach (var orpheline in enBase.Where(c => c.Actif && c.Code != null && !codesSemes.Contains(c.Code!)))
            {
                orpheline.Actif = false;
                orpheline.DateFinValidite = maintenant;
            }

            await context.SaveChangesAsync(ct);
        }

        // ------------------------------------------------------------------
        // Arêtes du graphe, toutes matières confondues.
        // Poids : 1 = utile, 2 = important, 3 = bloquant.
        // Un prérequis bloquant non maîtrisé fait basculer le diagnostic
        // vers la compétence amont plutôt que de s'acharner sur l'aval.
        //
        // Un code inconnu ne fait pas échouer le semis : l'arête est écartée.
        // C'est ce qui permet d'écrire un prérequis vers une matière voisine
        // sans dépendre de l'ordre dans lequel les référentiels sont semés.
        // ------------------------------------------------------------------
        private static async Task SeedPrerequisAsync(
            SchoolWebAppDatabaseContext context,
            (string Competence, string Prerequis, int Poids)[] aretes,
            CancellationToken ct)
        {
            var parCode = await context.Competences
                .Where(c => c.Code != null)
                .ToDictionaryAsync(c => c.Code!, c => c.Id, ct);

            var existantes = await context.CompetencesPrerequis
                .Select(p => new { p.CompetenceId, p.PrerequisId })
                .ToListAsync(ct);

            var dejaLa = existantes
                .Select(p => (p.CompetenceId, p.PrerequisId))
                .ToHashSet();

            var aAjouter = new List<CompetencePrerequis>();

            foreach (var (competence, prerequis, poids) in aretes)
            {
                if (!parCode.TryGetValue(competence, out var competenceId)) continue;
                if (!parCode.TryGetValue(prerequis, out var prerequisId)) continue;
                if (dejaLa.Contains((competenceId, prerequisId))) continue;

                aAjouter.Add(new CompetencePrerequis
                {
                    CompetenceId = competenceId,
                    PrerequisId = prerequisId,
                    Poids = poids
                });
            }

            if (aAjouter.Count > 0)
            {
                context.CompetencesPrerequis.AddRange(aAjouter);
                await context.SaveChangesAsync(ct);
            }
        }
    }
}
