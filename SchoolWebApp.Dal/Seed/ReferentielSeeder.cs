using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Dal.Seed.Referentiels;

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
    ///     CP, CE1, CE2             BO spécial n° 40         rentrée 2025
    ///                              du 31/10/2024
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
    ///     2de, 1re, Tle            programmes de 2019, en vigueur en
    ///                              2026-2027. Un nouveau programme de maths
    ///                              de terminale entre en application à la
    ///                              rentrée 2027 — À FAIRE AVANT.
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
    /// </summary>
    public static class ReferentielSeeder
    {
        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            await SeedNiveauxAsync(context, ct);
            await SeedMatieresAsync(context, ct);

            await SeedCompetencesAsync(context, "MATHS", ReferentielMaths.Competences, ct);
            await SeedCompetencesAsync(context, "FRANCAIS", ReferentielFrancais.Competences, ct);
            await SeedCompetencesAsync(context, "HISTOIRE_GEO", ReferentielHistoireGeo.Competences, ct);
            await SeedCompetencesAsync(context, "ANGLAIS", ReferentielAnglais.Competences, ct);
            await SeedCompetencesAsync(context, "SCIENCES", ReferentielSciences.Competences, ct);
            await SeedCompetencesAsync(context, "PHYSIQUE_CHIMIE", ReferentielPhysiqueChimie.Competences, ct);
            await SeedCompetencesAsync(context, "SVT", ReferentielSvt.Competences, ct);
            await SeedCompetencesAsync(context, "PHILOSOPHIE", ReferentielPhilosophie.Competences, ct);

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
            }

            if (context.ChangeTracker.HasChanges())
            {
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
            };

            // Libellés d'origine, remplacés depuis. Même précaution que pour les
            // couleurs : on ne réaligne que si la valeur en base est encore
            // celle qu'on avait posée.
            var libellesRemplaces = new Dictionary<string, string>
            {
                ["SCIENCES"] = "Sciences",
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
            };

            var existants = await context.Matieres.ToListAsync(ct);

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
                else if (couleursRemplacees.TryGetValue(m.Code, out var anciennes)
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

            var existants = await context.Competences.Select(c => c.Code).ToListAsync(ct);

            var aAjouter = competences
                .Where(c => !existants.Contains(c.Code))
                // Un niveau absent de la base n'est pas une erreur : on écarte
                // ses compétences plutôt que de faire échouer tout le semis.
                .Where(c => niveaux.ContainsKey(c.Niveau))
                .Select(c => new Competence
                {
                    MatiereId = matiere.Id,
                    NiveauScolaireId = niveaux[c.Niveau],
                    Code = c.Code,
                    Domaine = c.Domaine,
                    Libelle = c.Libelle,
                    Ordre = c.Ordre
                })
                .ToList();

            if (aAjouter.Count > 0)
            {
                context.Competences.AddRange(aAjouter);
                await context.SaveChangesAsync(ct);
            }
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
