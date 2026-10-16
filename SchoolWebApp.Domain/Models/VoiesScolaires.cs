namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// CE QU'UNE CLASSE DE LYCÉE ÉTUDIE — voies et séries technologiques — et
    /// donc quelles matières et quelles compétences la concernent.
    ///
    /// POURQUOI CETTE TABLE EXISTE
    /// ---------------------------
    /// Une matière est bornée par un RANG D'ANNÉE — de tel niveau à tel niveau.
    /// C'est suffisant tant qu'un rang correspond à une seule classe. Au lycée,
    /// ce n'est plus vrai : la première générale, la première STMG et la
    /// première professionnelle partagent le rang 11 et n'ont pas le même
    /// emploi du temps.
    ///
    /// LES SÉRIES TECHNOLOGIQUES — voulues par Camara le 14/09/2026, puis
    /// ramenées le même jour aux séries les plus suivies et à une seule classe
    /// par année : « il faut rester dans les classes les plus connues ».
    /// STMG, ST2S, STL. La STI2D n'y est pas : sa recherche de programmes n'a
    /// rien donné, et « je préfère ne rien ajouter que laisser visible une
    /// matière qui n'a pas de référentiel ». L'option de terminale (mercatique, systèmes
    /// d'information et numérique…) n'est PAS une classe : elle se range dans la
    /// matière, et le professeur la demande à l'élève.
    ///
    /// LES ÉTIQUETTES
    /// --------------
    /// Chaque classe porte des ÉTIQUETTES : « G » (générale), « T »
    /// (technologique), « P » (professionnelle), et la série (« STMG »). Une
    /// compétence est écrite pour une classe ; elle concerne un élève dès que
    /// l'une des étiquettes de sa classe figure parmi celles de l'élève. Un
    /// élève de STMG porte « STMG » et reçoit aussi « T » : le tronc commun de
    /// la voie technologique (maths, français, histoire-géographie, philosophie).
    ///
    /// Les deux classes technologiques sans série ne se CHOISISSENT PLUS : elles
    /// portent ce tronc commun. Un élève qui y était déjà les garde, et l'écran
    /// demande à son parent de préciser la série.
    ///
    /// POURQUOI EN CODE ET NON EN BASE
    /// -------------------------------
    /// C'est une donnée de RÉFÉRENCE — elle décrit l'école, pas l'exploitation.
    /// Une colonne en base laisserait croire qu'un administrateur peut la
    /// modifier, ce qui reviendrait à lui faire décider seul du programme
    /// national.
    /// </summary>
    public static class VoiesScolaires
    {
        private sealed record Classe(
            string Code, int Rang, string[] Etiquettes, string[] RecoitAussi, bool Selectionnable, string Groupe);

        private const string GroupeGeneral = "Lycée général et technologique";
        private const string GroupeTechno = "Lycée technologique";
        private const string GroupePro = "Voie professionnelle";

        private static readonly Classe[] Classes =
        [
            new("SECONDE",   10, ["G", "T"], [], true, GroupeGeneral),
            new("PREMIERE",  11, ["G"],      [], true, GroupeGeneral),
            new("TERMINALE", 12, ["G"],      [], true, GroupeGeneral),

            new("SECONDE_PRO",   10, ["P"], [], true, GroupePro),
            new("PREMIERE_PRO",  11, ["P"], [], true, GroupePro),
            new("TERMINALE_PRO", 12, ["P"], [], true, GroupePro),

            // Le tronc commun de la voie technologique.
            new("PREMIERE_TECHNO",  11, ["T"], [], false, GroupeTechno),
            new("TERMINALE_TECHNO", 12, ["T"], [], false, GroupeTechno),

            new("PREMIERE_STMG",   11, ["STMG"],  ["T"], true, GroupeTechno),
            new("TERMINALE_STMG",  12, ["STMG"],  ["T"], true, GroupeTechno),
            new("PREMIERE_ST2S",   11, ["ST2S"],  ["T"], true, GroupeTechno),
            new("TERMINALE_ST2S",  12, ["ST2S"],  ["T"], true, GroupeTechno),
            new("PREMIERE_STL",    11, ["STL"],   ["T"], true, GroupeTechno),
            new("TERMINALE_STL",   12, ["STL"],   ["T"], true, GroupeTechno),

            // RETIRÉES LE 14/09/2026, APRÈS AVOIR ÉTÉ SEMÉES : la STI2D n'a pas
            // de référentiel. Ces deux classes restent connues ici pour ne plus
            // être proposées — une classe absente de cette table se choisirait
            // par défaut — et, si un élève y a été inscrit entre-temps, pour
            // qu'il reçoive au moins le tronc commun technologique.
            new("PREMIERE_STI2D",  11, ["STI2D"], ["T"], false, GroupeTechno),
            new("TERMINALE_STI2D", 12, ["STI2D"], ["T"], false, GroupeTechno),
        ];

        /// <summary>
        /// LES MATIÈRES RETIRÉES : jamais au programme de personne. Voulu par
        /// Camara le 14/09/2026 : « je préfère ne rien ajouter que laisser visible
        /// une matière qui n'a pas de référentiel ». Elles avaient déjà été
        /// semées ; les retirer seulement du semeur les aurait laissées actives en
        /// base, et sans réservation elles se seraient ouvertes à tout le lycée.
        /// </summary>
        /// <remarks>
        /// MUSIQUE et DANSE retirées le 17/09/2026 — Camara : « je ne veux pas que le
        /// professeur d'arts les enseigne ». Ce n'est pas un changement de programme :
        /// les textes officiels existent toujours, et Jeanne garde les cinq autres arts.
        /// C'est un choix de catalogue, et il se pose ICI comme les précédents : les
        /// retirer du seul semeur les aurait laissées actives en base, donc ouvertes.
        /// </remarks>
        private static readonly HashSet<string> MatieresRetirees =
            new(["INGENIERIE", "PHYSIQUE_CHIMIE_MATHS", "MUSIQUE", "DANSE"],
                StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<string, Classe> ParCode =
            Classes.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// LES MATIÈRES RÉSERVÉES À UNE SÉRIE. Une matière de cette table n'est
        /// au programme que si l'élève porte l'une de ces étiquettes — le
        /// management n'existe qu'en STMG. Les autres matières restent ouvertes
        /// à tous, dans leurs bornes de rang, sauf exclusion plus bas.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string[]> ReserveeAux =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["SCIENCES_GESTION"] = ["STMG"],
                ["MANAGEMENT"] = ["STMG"],
                ["DROIT_ECONOMIE"] = ["STMG"],
                ["SANITAIRE_SOCIAL"] = ["ST2S"],
                ["BIOLOGIE_HUMAINE"] = ["ST2S"],
                ["BIOTECHNOLOGIES"] = ["STL"],
                ["SPCL"] = ["STL"],

                // La voie professionnelle (16/10/2026) : ses deux matières
                // générales propres, de la seconde à la terminale pro.
                ["PSE"] = ["P"],
                ["ECONOMIE_DROIT"] = ["P"],
                ["ECONOMIE_GESTION"] = ["P"],
            };

        /// <summary>Les séries sans SVT : leur biologie a un autre nom et un autre programme.</summary>
        private static readonly string[] SeriesSansSvt = ["STMG", "ST2S", "STL"];

        /// <summary>
        /// Les séries sans physique-chimie sous ce nom. En STL, c'est « physique-chimie
        /// et mathématiques », qui n'a pas encore de référentiel et n'est donc pas
        /// proposée : Camara, le 14/09/2026, « je préfère ne rien ajouter que
        /// laisser visible une matière qui n'a pas de référentiel ». La ST2S garde
        /// la physique-chimie : pour la santé en première, chimie en terminale.
        /// </summary>
        private static readonly string[] SeriesSansPhysiqueChimie = ["STMG", "STL"];

        /// <summary>
        /// LES CLASSES OÙ UNE LV2 EXISTE — de la 5e à la terminale, voies générale
        /// et technologique. C'est là, et seulement là, que le parent peut dire que
        /// son enfant a l'espagnol en LV2 : voulu par Camara le 14/09/2026, « cette
        /// option ne doit pas s'afficher sur toutes les classes ».
        /// </summary>
        private static readonly HashSet<string> ClassesAvecLv2 = new(
            [
                // 5e à 3e : obligatoire (arrêté du 19-5-2015, annexe remplacée
                // le 10-3-2026). La 6e bilangue n'y est pas : facultative, et
                // réservée à une minorité d'élèves.
                "CINQUIEME", "QUATRIEME", "TROISIEME",
                // 3e prépa-métiers : « LV1 et LV2 », arrêté du 10-4-2019.
                "TROISIEME_PREPA",
                // Lycée général et technologique : LVA et LVB obligatoires. La
                // voie professionnelle n'y est pas : la LV2 n'existe que dans
                // certaines spécialités, et son programme n'est pas couvert.
                "SECONDE", "PREMIERE", "TERMINALE",
                "PREMIERE_TECHNO", "TERMINALE_TECHNO",
                "PREMIERE_STMG", "TERMINALE_STMG",
                "PREMIERE_ST2S", "TERMINALE_ST2S",
                "PREMIERE_STL", "TERMINALE_STL",
            ],
            StringComparer.OrdinalIgnoreCase);

        /// <summary>Le parent peut-il cocher « espagnol en LV2 » pour cette classe ?</summary>
        public static bool AUneLv2(string? codeNiveau) =>
            codeNiveau is not null && ClassesAvecLv2.Contains(codeNiveau);

        /// <summary>
        /// LES AUTRES LANGUES VIVANTES B — Camara, le 04/10/2026 : allemand,
        /// italien, chinois, arabe, « selon les classes où ça doit apparaître ».
        /// Les mêmes classes que l'espagnol : la LVB commence en 5e, et n'existe
        /// qu'en voies générale et technologique au lycée.
        ///
        /// UN CHOIX DE LA FAMILLE, COMME L'ESPAGNOL — mais RANGÉ AVEC LES
        /// SPÉCIALITÉS, dans la même liste (`Eleve.Specialites`), et c'est
        /// voulu. L'espagnol avait reçu sa propre colonne ; en donner une à
        /// chaque langue aurait demandé une migration par langue et un paramètre
        /// de plus dans les huit endroits qui demandent « cette matière est-elle
        /// au programme de cet élève ? ». Ces endroits passent DÉJÀ la liste des
        /// choix de la famille : une langue cochée y voyage sans rien changer.
        /// Ce que voit le navigateur reste séparé — `Langues` d'un côté,
        /// `Specialites` de l'autre : voir `LanguesDe` et `SansLangues`.
        ///
        /// Le code est celui de la matière. L'espagnol n'y est pas : il garde sa
        /// case (`Eleve.Lv2Espagnol`).
        /// </summary>
        public static readonly IReadOnlyList<(string Code, string Libelle)> LanguesAuChoix =
        [
            ("ALLEMAND", "Allemand"),
            ("ITALIEN", "Italien"),
            ("CHINOIS", "Chinois"),
            ("ARABE", "Arabe"),
        ];

        public static bool EstUneLangueAuChoix(string? codeMatiere) =>
            codeMatiere is not null
            && LanguesAuChoix.Any(l => l.Code.Equals(codeMatiere.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>Parmi les choix de la famille, les langues.</summary>
        public static List<string> LanguesDe(IEnumerable<string>? choix) =>
            (choix ?? []).Where(EstUneLangueAuChoix).Select(c => c.Trim().ToUpperInvariant()).Distinct().ToList();

        /// <summary>Les choix de la famille, sans les langues : les seules spécialités.</summary>
        public static List<string> SansLangues(IEnumerable<string>? choix) =>
            (choix ?? []).Where(c => !EstUneLangueAuChoix(c)).ToList();

        /// <summary>
        /// Ce qu'on garde des langues envoyées par le navigateur : des codes
        /// connus, sans doublon, et rien dans une classe sans LVB. Le serveur ne
        /// croit jamais la liste sur parole — même règle que les spécialités.
        /// </summary>
        public static List<string> LanguesRetenues(string? codeNiveau, IEnumerable<string>? codes) =>
            AUneLv2(codeNiveau) ? LanguesDe(codes) : [];

        /// <summary>Une spécialité de la voie générale : son code EST celui de la matière qui l'enseigne.</summary>
        /// <remarks>
        /// `Precision` : le nom officiel en toutes lettres, affiché en petit sous
        /// le libellé. Voulu par Camara le 15/09/2026 : les spécialités de langue
        /// s'écrivaient les unes en sigle (« LLCER anglais, monde contemporain »),
        /// les autres en toutes lettres (« Langues, littératures et cultures
        /// étrangères — espagnol ») — si bien qu'en cherchant « LLCER espagnol »
        /// dans la liste, on ne le trouvait pas. Le sigle vient donc en tête
        /// pour toutes, et le nom complet reste lisible dessous.
        /// </remarks>
        public sealed record Specialite(string Code, string Libelle, string? Precision = null);

        private const string Llcer = "Langues, littératures et cultures étrangères";
        private const string Llca = "Littérature, langues et cultures de l'Antiquité";

        /// <summary>
        /// LES SPÉCIALITÉS DE LA VOIE GÉNÉRALE — voulues par Camara le 14/09/2026 :
        /// « il faut absolument que les terminales que je gère puissent préparer le
        /// bac ». Au bac général, les écrits de terminale sont la philosophie ET
        /// les deux spécialités que l'élève a gardées : sans les connaître, aucune
        /// section « Préparation au bac » ne peut être juste.
        ///
        /// UN CHOIX DE LA FAMILLE, comme la LV2 : trois cochées en première, les
        /// deux gardées en terminale. La biologie-écologie n'y est pas — elle
        /// n'existe qu'en lycée agricole —, ni les LLCER des langues que Mimia
        /// n'enseigne pas.
        ///
        /// Proposée au parent SEULEMENT si la matière existe et est ouverte : une
        /// case sans professeur derrière serait une promesse vide (voir
        /// `ReferentielViewModelBuilder`).
        /// </summary>
        public static readonly IReadOnlyList<Specialite> SpecialitesGenerales =
        [
            new("MATHS", "Mathématiques"),
            new("PHYSIQUE_CHIMIE", "Physique-chimie"),
            new("SVT", "Sciences de la vie et de la Terre"),
            new("SES", "Sciences économiques et sociales"),
            new("HGGSP", "Histoire-géographie, géopolitique et sciences politiques"),
            new("HLP", "Humanités, littérature et philosophie"),
            new("LLCER_ANGLAIS", "LLCER anglais", Llcer),
            new("AMC", "LLCER anglais, monde contemporain (AMC)", Llcer),
            new("LLCER_ESPAGNOL", "LLCER espagnol", Llcer),
            new("LLCA_LATIN", "LLCA latin", Llca),
            new("LLCA_GREC", "LLCA grec", Llca),
            new("NSI", "Numérique et sciences informatiques"),
            new("SI", "Sciences de l'ingénieur"),
            new("EPPCS", "Éducation physique, pratiques et culture sportives"),
            new("ARTS_PLASTIQUES", "Arts plastiques"),
            new("HISTOIRE_ARTS", "Histoire des arts"),
            new("CINEMA_AUDIOVISUEL", "Cinéma-audiovisuel"),
            new("THEATRE", "Théâtre"),
            // Musique et danse retirées le 17/09/2026 : voir MatieresRetirees.
            new("ARTS_CIRQUE", "Arts du cirque"),
        ];

        private static readonly HashSet<string> CodesSpecialites =
            new(SpecialitesGenerales.Select(s => s.Code), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// LES MATIÈRES QUI N'EXISTENT QUE COMME SPÉCIALITÉ. Maths, physique-chimie
        /// et SVT n'en font pas partie : elles ont une vie avant la première, et
        /// les retirer à l'élève dont la famille n'a encore rien coché lui ferait
        /// perdre des cours qu'il suit.
        /// </summary>
        private static readonly HashSet<string> MatieresDeSpecialiteSeulement = new(
            SpecialitesGenerales.Select(s => s.Code).Except(["MATHS", "PHYSIQUE_CHIMIE", "SVT"]),
            StringComparer.OrdinalIgnoreCase);

        /// <summary>Combien de spécialités dans cette classe : 3 en première générale, 2 en terminale, 0 ailleurs.</summary>
        public static int NombreSpecialites(string? codeNiveau) => codeNiveau?.ToUpperInvariant() switch
        {
            "PREMIERE" => 3,
            "TERMINALE" => 2,
            _ => 0,
        };

        /// <summary>
        /// Ce qu'on garde d'un choix envoyé par le navigateur : des codes connus,
        /// sans doublon, pas plus que la classe n'en a — et rien hors de la voie
        /// générale. Le serveur ne croit jamais la liste sur parole.
        /// </summary>
        public static List<string> SpecialitesRetenues(string? codeNiveau, IEnumerable<string>? codes)
        {
            var nombre = NombreSpecialites(codeNiveau);
            if (nombre == 0 || codes is null) return [];

            return codes
                .Where(c => !string.IsNullOrWhiteSpace(c) && CodesSpecialites.Contains(c.Trim()))
                .Select(c => SpecialitesGenerales.First(s => s.Code.Equals(c.Trim(), StringComparison.OrdinalIgnoreCase)).Code)
                .Distinct()
                .Take(nombre)
                .ToList();
        }

        /// <summary>La colonne en base : les codes séparés par « ; ».</summary>
        public static List<string> LireSpecialites(string? stockees) =>
            (stockees ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        public static string? EcrireSpecialites(IEnumerable<string>? codes)
        {
            var liste = (codes ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            return liste.Count == 0 ? null : string.Join(';', liste);
        }

        // ================================================================
        // LA FILIÈRE PROFESSIONNELLE — Camara, le 15/10/2026
        // ================================================================
        //
        // « Un élève en pro va s'y retrouver ? » Le professeur ne savait rien
        // de son métier. Le parent indique désormais la FAMILLE DE MÉTIERS en
        // seconde pro (le choix de la spécialité n'a lieu qu'en première) et la
        // SPÉCIALITÉ en première et terminale pro. Le professeur y prend ses
        // exemples ; la conseillère d'orientation y lit sa filière ; et la
        // physique-chimie se retire aux filières qui ne l'ont pas.
        //
        // PAS LES CENT SPÉCIALITÉS : les douze plus suivies, qui font 73 % des
        // élèves de première et de terminale pro (DEPP, effectifs au 01/10/2025,
        // jeu `fr-en-lycee_pro-effectifs-niveau-sexe-mef`), et « Autre », avec un
        // champ libre. Une spécialité souvent écrite dans « Autre » est la
        // prochaine à ajouter.
        //
        // LES QUINZE FAMILLES EN VIGUEUR EN 2026-2027 : annexe de l'arrêté du
        // 19/04/2019 (MENE1911939A) modifiée le 07/03/2024. L'arrêté du 16/03/2026
        // (MENE2601641A) la remplace à la RENTRÉE 2027 seulement — hôtellerie-
        // restauration et alimentation disparaissent, Cuisine et CSR deviennent
        // « Métiers de la restauration », hors famille. REPASSER ICI À CE MOMENT.
        // La page Onisep montre déjà la liste de 2027 : ne pas la recopier avant.
        //
        // PHYSIQUE-CHIMIE OU LANGUE VIVANTE B : annexe de l'arrêté du 13/06/2024
        // (grilles par spécialité). Relation client et gestion-transport-logistique
        // ont une LVB, hôtellerie-restauration et alimentation des « sciences
        // appliquées » : ni les unes ni les autres n'ont de physique-chimie.
        //
        // RANGÉES AVEC LES SPÉCIALITÉS, comme les langues : même liste
        // (`Eleve.Specialites`), préfixes FAMILLE_ et BACPRO_, et le texte libre
        // derrière BACPRO_LIBRE: (sans « ; », qui sépare la liste).

        // L'ÉCONOMIE SUIT LA SPÉCIALITÉ AUSSI (16/10/2026) : annexe I de l'arrêté
        // du 19/04/2019 (NOR MENE1911952A), remplacée par l'arrêté du 13/06/2024 et
        // lue en version consolidée au 15/10/2026. Économie-droit pour les
        // spécialités tertiaires, économie-gestion pour les autres — MAIS LE
        // SECTEUR NE SUFFIT PAS : l'AEPA, classée « services », suit l'économie-
        // gestion. La cuisine et le service en restauration ont leur propre
        // matière (gestion appliquée, mercatique et gestion appliquée), la famille
        // alimentation aussi (environnement économique, juridique et management) :
        // aucun de nos deux programmes, Mimia ne leur en propose donc aucun.

        /// <summary>La matière d'économie d'une filière.</summary>
        public enum EconomiePro
        {
            /// <summary>Économie-droit (spécialités tertiaires).</summary>
            Droit,
            /// <summary>Économie-gestion (production, et l'AEPA).</summary>
            Gestion,
            /// <summary>Un autre programme, que Mimia n'enseigne pas.</summary>
            Autre,
        }

        /// <summary>Une famille de métiers de seconde pro, si ses spécialités ont de la physique-chimie, et leur économie (null = non vérifiée).</summary>
        public sealed record FamilleMetiers(string Code, string Libelle, bool PhysiqueChimie, EconomiePro? Economie);

        /// <summary>Une spécialité de bac pro : sa famille (null = hors famille), sa physique-chimie et son économie (null = inconnues).</summary>
        public sealed record SpecialitePro(string Code, string Libelle, string? Famille, bool? PhysiqueChimie, EconomiePro? Economie);

        public const string HorsFamille = "HORS_FAMILLE";
        public const string SpecialiteProAutre = "AUTRE";
        public const int LongueurSpecialiteLibreMax = 80;

        private const string PrefixeFamille = "FAMILLE_";
        private const string PrefixeSpecialitePro = "BACPRO_";
        private const string PrefixeLibre = "BACPRO_LIBRE:";

        public static readonly IReadOnlyList<FamilleMetiers> FamillesMetiers =
        [
            new("CONSTRUCTION", "Construction durable, bâtiment et travaux publics", true, EconomiePro.Gestion),
            new("GATL", "Gestion administrative, transport et logistique", false, EconomiePro.Droit),
            new("RELATION_CLIENT", "Relation client", false, EconomiePro.Droit),
            new("GRAPHIQUE", "Industries graphiques et communication", true, EconomiePro.Gestion),
            new("ALIMENTATION", "Alimentation", false, EconomiePro.Autre),
            new("BEAUTE", "Beauté et bien-être", true, EconomiePro.Gestion),
            new("ETUDES_BATIMENT", "Études et modélisation numérique du bâtiment", true, EconomiePro.Gestion),
            new("AERONAUTIQUE", "Aéronautique", true, EconomiePro.Gestion),
            new("HOTELLERIE", "Hôtellerie-restauration", false, EconomiePro.Autre),
            new("MAINTENANCE", "Maintenance des matériels et des véhicules", true, EconomiePro.Gestion),
            new("MECANIQUE", "Réalisation d'ensembles mécaniques et industriels", true, EconomiePro.Gestion),
            new("AGENCEMENT", "Agencement, menuiserie et ameublement", true, EconomiePro.Gestion),
            new("TRANSITIONS", "Transitions numérique et énergétique", true, EconomiePro.Gestion),
            new("PILOTAGE", "Pilotage et maintenance d'installations automatisées", true, EconomiePro.Gestion),
            new("MER", "Métiers de la mer", true, null),
        ];

        /// <summary>Les douze spécialités les plus suivies (73 % des élèves de 1re et Tle pro), puis « Autre ».</summary>
        public static readonly IReadOnlyList<SpecialitePro> SpecialitesPro =
        [
            new("MCV", "Métiers du commerce et de la vente", "RELATION_CLIENT", false, EconomiePro.Droit),
            new("ASSP", "Accompagnement, soins et services à la personne (ASSP)", null, true, EconomiePro.Gestion),
            new("AGORA", "Assistance à la gestion des organisations et de leurs activités (AGOrA)", "GATL", false, EconomiePro.Droit),
            new("MELEC", "Métiers de l'électricité et de ses environnements connectés (MELEC)", "TRANSITIONS", true, EconomiePro.Gestion),
            new("CIEL", "Cybersécurité, informatique et réseaux, électronique (CIEL)", "TRANSITIONS", true, EconomiePro.Gestion),
            new("ACCUEIL", "Métiers de l'accueil", "RELATION_CLIENT", false, EconomiePro.Droit),
            new("MV", "Maintenance des véhicules", "MAINTENANCE", true, EconomiePro.Gestion),
            new("MSPC", "Maintenance des systèmes de production connectés (MSPC)", "PILOTAGE", true, EconomiePro.Gestion),
            new("CUISINE", "Cuisine", "HOTELLERIE", false, EconomiePro.Autre),
            new("CSR", "Commercialisation et services en restauration (CSR)", "HOTELLERIE", false, EconomiePro.Autre),
            new("AEPA", "Animation – enfance et personnes âgées (AEPA)", null, false, EconomiePro.Gestion),
            new("SECURITE", "Métiers de la sécurité", null, false, EconomiePro.Droit),
            new(SpecialiteProAutre, "Une autre spécialité", null, null, null),
        ];

        /// <summary>Les classes où la filière professionnelle se renseigne.</summary>
        public static bool EstVoiePro(string? codeNiveau) =>
            codeNiveau?.ToUpperInvariant() is "SECONDE_PRO" or "PREMIERE_PRO" or "TERMINALE_PRO";

        /// <summary>Ce choix de la famille est-il une famille de métiers ou une spécialité de bac pro ?</summary>
        public static bool EstUnChoixPro(string? choix) =>
            choix is not null
            && (choix.StartsWith(PrefixeFamille, StringComparison.OrdinalIgnoreCase)
                || choix.StartsWith(PrefixeSpecialitePro, StringComparison.OrdinalIgnoreCase));

        /// <summary>Les choix de la famille, sans la filière pro.</summary>
        public static List<string> SansChoixPro(IEnumerable<string>? choix) =>
            (choix ?? []).Where(c => !EstUnChoixPro(c)).ToList();

        /// <summary>La filière d'un élève, lue dans ses choix : famille, spécialité, texte libre.</summary>
        public sealed record FiliereProfessionnelle(string? Famille, string? Specialite, string? Libre)
        {
            public static readonly FiliereProfessionnelle Vide = new(null, null, null);
        }

        public static FiliereProfessionnelle FiliereProDe(IEnumerable<string>? choix)
        {
            string? famille = null, specialite = null, libre = null;
            foreach (var c in choix ?? [])
            {
                if (c.StartsWith(PrefixeLibre, StringComparison.OrdinalIgnoreCase)) libre = c[PrefixeLibre.Length..];
                else if (c.StartsWith(PrefixeSpecialitePro, StringComparison.OrdinalIgnoreCase)) specialite = c[PrefixeSpecialitePro.Length..].ToUpperInvariant();
                else if (c.StartsWith(PrefixeFamille, StringComparison.OrdinalIgnoreCase)) famille = c[PrefixeFamille.Length..].ToUpperInvariant();
            }

            return new FiliereProfessionnelle(famille, specialite, libre);
        }

        /// <summary>
        /// Ce qu'on garde de la filière envoyée par le navigateur : des codes
        /// connus, et rien hors d'une classe pro. La famille se déduit de la
        /// spécialité quand elle est connue. Le texte libre n'accompagne que
        /// « Autre », sans « ; », et borné.
        /// </summary>
        public static List<string> FiliereProRetenue(string? codeNiveau, string? famille, string? specialite, string? libre)
        {
            if (!EstVoiePro(codeNiveau)) return [];

            var retenue = new List<string>();
            var spe = SpecialitesPro.FirstOrDefault(s => s.Code.Equals(specialite?.Trim(), StringComparison.OrdinalIgnoreCase));
            var fam = spe?.Famille
                ?? (string.Equals(famille?.Trim(), HorsFamille, StringComparison.OrdinalIgnoreCase)
                    ? HorsFamille
                    : FamillesMetiers.FirstOrDefault(f => f.Code.Equals(famille?.Trim(), StringComparison.OrdinalIgnoreCase))?.Code);

            if (fam is not null) retenue.Add(PrefixeFamille + fam);
            if (spe is not null) retenue.Add(PrefixeSpecialitePro + spe.Code);

            if (spe?.Code == SpecialiteProAutre && !string.IsNullOrWhiteSpace(libre))
            {
                var propre = libre.Replace(";", ",").Trim();
                if (propre.Length > LongueurSpecialiteLibreMax) propre = propre[..LongueurSpecialiteLibreMax].Trim();
                if (propre.Length > 0) retenue.Add(PrefixeLibre + propre);
            }

            return retenue;
        }

        /// <summary>
        /// La filière a-t-elle de la physique-chimie ? Null quand on ne sait pas
        /// (rien renseigné, « Autre », « hors famille » sans spécialité) : dans
        /// le doute, la matière reste proposée.
        /// </summary>
        public static bool? PhysiqueChimieDeLaFiliere(IEnumerable<string>? choix)
        {
            var filiere = FiliereProDe(choix);
            var spe = SpecialitesPro.FirstOrDefault(s => s.Code == filiere.Specialite);
            if (spe is not null && spe.Code != SpecialiteProAutre) return spe.PhysiqueChimie;

            return FamillesMetiers.FirstOrDefault(f => f.Code == filiere.Famille)?.PhysiqueChimie;
        }

        /// <summary>
        /// La matière d'économie de la filière. Null quand on ne sait pas (rien
        /// renseigné, « Autre », « hors famille » sans spécialité, métiers de la
        /// mer) : dans le doute, les deux restent proposées, comme la physique-
        /// chimie.
        /// </summary>
        public static EconomiePro? EconomieDeLaFiliere(IEnumerable<string>? choix)
        {
            var filiere = FiliereProDe(choix);
            var spe = SpecialitesPro.FirstOrDefault(s => s.Code == filiere.Specialite);
            if (spe is not null && spe.Code != SpecialiteProAutre) return spe.Economie;

            return FamillesMetiers.FirstOrDefault(f => f.Code == filiere.Famille)?.Economie;
        }

        /// <summary>La filière en toutes lettres, pour le professeur et la conseillère. Null si rien n'est renseigné.</summary>
        public static string? DecrireFilierePro(IEnumerable<string>? choix)
        {
            var filiere = FiliereProDe(choix);
            var spe = SpecialitesPro.FirstOrDefault(s => s.Code == filiere.Specialite);
            var famille = FamillesMetiers.FirstOrDefault(f => f.Code == filiere.Famille);

            var nomSpecialite = spe is null ? null
                : spe.Code == SpecialiteProAutre ? (string.IsNullOrWhiteSpace(filiere.Libre) ? null : filiere.Libre)
                : spe.Libelle;

            if (nomSpecialite is not null && famille is not null) return $"bac pro {nomSpecialite} (famille de métiers « {famille.Libelle} »)";
            if (nomSpecialite is not null) return $"bac pro {nomSpecialite}";
            if (famille is not null) return $"famille de métiers « {famille.Libelle} » (la spécialité se choisit en fin de seconde)";
            return null;
        }

        /// <summary>Les étiquettes que porte un élève de cette classe. Vide hors du lycée.</summary>
        private static HashSet<string> EtiquettesEleve(string? codeNiveau) =>
            codeNiveau is not null && ParCode.TryGetValue(codeNiveau, out var classe)
                ? classe.Etiquettes.Concat(classe.RecoitAussi).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Cette classe peut-elle être choisie pour un élève ?</summary>
        public static bool EstSelectionnable(string? codeNiveau) =>
            codeNiveau is null || !ParCode.TryGetValue(codeNiveau, out var classe) || classe.Selectionnable;

        /// <summary>Le groupe sous lequel ranger la classe dans une liste de choix.</summary>
        public static string Groupe(string? codeNiveau, string? cycle)
        {
            if (codeNiveau is not null && ParCode.TryGetValue(codeNiveau, out var classe)) return classe.Groupe;

            return cycle switch
            {
                "Primaire" => "École élémentaire",
                "College" => "Collège",
                "Lycee" => GroupeGeneral,
                _ => cycle ?? string.Empty,
            };
        }

        /// <summary>
        /// Les codes de niveaux où cette matière ne doit PAS être proposée,
        /// même si le rang de l'élève tombe dans ses bornes.
        /// </summary>
        public static string[] NiveauxExclus(string? codeMatiere) =>
            codeMatiere is null
                ? []
                : Classes.Where(c => !ConcerneLaVoie(codeMatiere, c.Code, c.Rang)).Select(c => c.Code).ToArray();

        /// <summary>
        /// Cette matière figure-t-elle à l emploi du temps de cette classe ?
        ///
        /// LA RÈGLE N EXISTE QU ICI, ET C EST TOUT L OBJET DE CETTE MÉTHODE.
        /// Elle était écrite deux fois, une fois en C#, une fois en JavaScript, et
        /// le serveur ne la vérifiait nulle part : un enfant de sixième a pu ouvrir
        /// un cours de philosophie.
        /// </summary>
        /// <param name="lv2Espagnol">
        /// La case « espagnol en LV2 » de l'élève. Seul l'espagnol la lit ; sans
        /// elle, l'espagnol n'est au programme de personne.
        /// </param>
        /// <param name="specialites">
        /// Les spécialités cochées par la famille. Seules les matières qui
        /// n'existent que comme spécialité (SES, NSI…) la lisent.
        /// </param>
        public static bool EstAuProgramme(
            Matiere? matiere, NiveauScolaire? niveau, bool lv2Espagnol = false,
            IReadOnlyCollection<string>? specialites = null)
        {
            if (matiere is null || niveau is null) return false;

            return EstAuProgramme(
                matiere.Code, matiere.NiveauOrdreMin, matiere.NiveauOrdreMax, niveau.Code, niveau.Ordre,
                lv2Espagnol, specialites);
        }

        /// <summary>
        /// La même règle, sur des valeurs brutes — pour les couches qui tiennent
        /// des entités de base de données et non les modèles du domaine.
        /// </summary>
        public static bool EstAuProgramme(
            string? codeMatiere, int niveauOrdreMin, int niveauOrdreMax, string? codeNiveau, int rang,
            bool lv2Espagnol = false, IReadOnlyCollection<string>? specialites = null)
        {
            if (rang < niveauOrdreMin || rang > niveauOrdreMax) return false;

            // L'ESPAGNOL EST UN CHOIX, PAS UNE CLASSE : il faut une classe où la
            // LV2 existe ET la case cochée par le parent.
            if (string.Equals(codeMatiere, "ESPAGNOL", StringComparison.OrdinalIgnoreCase))
            {
                return lv2Espagnol && AUneLv2(codeNiveau);
            }

            // LES AUTRES LANGUES AUSSI : une classe où la LVB existe, et la langue
            // cochée — elle voyage dans la liste des choix de la famille.
            if (EstUneLangueAuChoix(codeMatiere))
            {
                return AUneLv2(codeNiveau)
                    && specialites is not null
                    && specialites.Contains(codeMatiere!, StringComparer.OrdinalIgnoreCase);
            }

            if (!MatiereDeLaFiliere(codeMatiere, codeNiveau, specialites)) return false;

            // UNE SPÉCIALITÉ AUSSI : une classe qui en a, et la case cochée.
            if (codeMatiere is not null && MatieresDeSpecialiteSeulement.Contains(codeMatiere))
            {
                return NombreSpecialites(codeNiveau) > 0
                    && specialites is not null
                    && specialites.Contains(codeMatiere, StringComparer.OrdinalIgnoreCase);
            }

            return ConcerneLaVoie(codeMatiere, codeNiveau, rang);
        }

        /// <summary>
        /// LA MATIÈRE EST-ELLE CELLE DE LA FILIÈRE PRO DE L'ÉLÈVE ? Vrai hors de
        /// la voie pro, et vrai dans le doute (filière inconnue). Une seule règle
        /// pour la grille des matières et pour les épreuves du bac pro.
        ///
        /// - La physique-chimie (15/10/2026) : les filières tertiaires et la
        ///   restauration ne l'ont pas.
        /// - L'économie (16/10/2026) : l'élève ne voit que la sienne ; aucune si
        ///   sa filière a un autre programme (cuisine, restauration,
        ///   alimentation) ; les deux tant qu'on ne sait pas. Hors de la voie
        ///   pro, `ReserveeAux` les retire déjà.
        /// </summary>
        public static bool MatiereDeLaFiliere(string? codeMatiere, string? codeNiveau, IEnumerable<string>? choix)
        {
            if (codeMatiere is null || !EstVoiePro(codeNiveau)) return true;

            switch (codeMatiere.ToUpperInvariant())
            {
                case "PHYSIQUE_CHIMIE":
                    return PhysiqueChimieDeLaFiliere(choix) != false;

                case "ECONOMIE_DROIT" or "ECONOMIE_GESTION":
                    var attendue = codeMatiere.Equals("ECONOMIE_DROIT", StringComparison.OrdinalIgnoreCase)
                        ? EconomiePro.Droit
                        : EconomiePro.Gestion;
                    var economie = EconomieDeLaFiliere(choix);
                    return economie is null || economie == attendue;

                default:
                    return true;
            }
        }

        private static bool ConcerneLaVoie(string? codeMatiere, string? codeNiveau, int rang)
        {
            if (codeMatiere is null) return true;

            if (MatieresRetirees.Contains(codeMatiere)) return false;

            var etiquettes = EtiquettesEleve(codeNiveau);

            if (ReserveeAux.TryGetValue(codeMatiere, out var reservee) && !reservee.Any(etiquettes.Contains))
            {
                return false;
            }

            return codeMatiere.ToUpperInvariant() switch
            {
                // Plus de français après la première, sauf en voie
                // professionnelle où il continue jusqu'au bac.
                "FRANCAIS" => !(rang == 12 && (etiquettes.Contains("G") || etiquettes.Contains("T"))),

                // Pas de philosophie au bac professionnel.
                "PHILOSOPHIE" => !etiquettes.Contains("P"),

                // PAS DE SVT AU BAC PROFESSIONNEL, ni dans les séries
                // technologiques, où la biologie a son propre programme.
                "SVT" => !etiquettes.Contains("P") && !SeriesSansSvt.Any(etiquettes.Contains),

                // LA PHYSIQUE-CHIMIE RESTE EN VOIE PROFESSIONNELLE, et c est un
                // choix : la base ne connaît pas la spécialité de l élève — la
                // proposer à tous vaut mieux que de la retirer à ceux qui l ont.
                "PHYSIQUE_CHIMIE" => !SeriesSansPhysiqueChimie.Any(etiquettes.Contains),

                _ => true,
            };
        }

        /// <summary>
        /// Une compétence écrite pour telle classe concerne-t-elle un élève de
        /// telle autre classe ? Répond OUI dès qu'un doute subsiste : hors du
        /// lycée il n'y a pas de voie.
        /// </summary>
        public static bool NiveauConcerne(string? codeEleve, string? codeCompetence)
        {
            if (codeCompetence is null || !ParCode.TryGetValue(codeCompetence, out var classeCompetence))
                return true;

            if (codeEleve is null || !ParCode.ContainsKey(codeEleve))
                return true;

            var etiquettes = EtiquettesEleve(codeEleve);
            return classeCompetence.Etiquettes.Any(etiquettes.Contains);
        }

        /// <summary>
        /// Ne garder que les compétences qui concernent la voie de l'élève —
        /// AVEC REPLI SUR L'HÉRITAGE PAR RANG.
        ///
        /// SI la classe de l'élève a son propre référentiel pour cette matière et
        /// ce rang — sa voie ou sa série —, il remplace l'héritage ; SINON
        /// l'héritage continue de s'appliquer. Filtrer sèchement viderait la carte
        /// d'une première professionnelle de tout ce dont elle vit.
        ///
        /// L'ordre d'entrée est conservé : les appelants trient par rang puis
        /// par ordre avant d'appeler, et ce tri ne doit pas être défait.
        /// </summary>
        public static List<T> RetenirPourLaVoie<T>(
            IEnumerable<T> competences,
            string? codeNiveauEleve,
            Func<T, string?> codeNiveau,
            Func<T, (int Matiere, int Rang)> groupe)
        {
            var liste = competences as IList<T> ?? competences.ToList();

            // Les couples (matière, rang) où la classe de l'élève a écrit le
            // sien. Là, et seulement là, l'héritage s'efface.
            var specialises = liste
                .Where(c => ParCode.ContainsKey(codeNiveau(c) ?? string.Empty)
                            && NiveauConcerne(codeNiveauEleve, codeNiveau(c)))
                .Select(groupe)
                .ToHashSet();

            return liste
                .Where(c => !specialises.Contains(groupe(c))
                            || NiveauConcerne(codeNiveauEleve, codeNiveau(c)))
                .ToList();
        }
    }
}
