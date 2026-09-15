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
        private static readonly HashSet<string> MatieresRetirees =
            new(["INGENIERIE", "PHYSIQUE_CHIMIE_MATHS"], StringComparer.OrdinalIgnoreCase);

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
            new("MUSIQUE", "Musique"),
            new("THEATRE", "Théâtre"),
            new("DANSE", "Danse"),
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

            // UNE SPÉCIALITÉ AUSSI : une classe qui en a, et la case cochée.
            if (codeMatiere is not null && MatieresDeSpecialiteSeulement.Contains(codeMatiere))
            {
                return NombreSpecialites(codeNiveau) > 0
                    && specialites is not null
                    && specialites.Contains(codeMatiere, StringComparer.OrdinalIgnoreCase);
            }

            return ConcerneLaVoie(codeMatiere, codeNiveau, rang);
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
