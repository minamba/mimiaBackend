namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Les matières qui n'existent pas dans certaines classes, alors qu'elles
    /// existent au même rang d'année.
    ///
    /// POURQUOI CETTE TABLE EXISTE
    /// ---------------------------
    /// Une matière est bornée par un RANG D'ANNÉE — de tel niveau à tel niveau.
    /// C'est suffisant tant qu'un rang correspond à une seule classe, et ça l'a
    /// été jusqu'au jour où les voies sont apparues : trois classes partagent
    /// désormais le rang 12, et elles n'ont pas le même emploi du temps.
    ///
    /// Deux cas concrets, et ils vont en sens inverse :
    ///
    /// - LE FRANÇAIS. Il s'arrête en terminale GÉNÉRALE et TECHNOLOGIQUE : les
    ///   épreuves anticipées ont lieu en fin de première, et il n'y a plus de
    ///   cours après. Mais la terminale PROFESSIONNELLE, elle, a du français
    ///   jusqu'au bout. Le borner au rang 11 en privait l'élève de bac pro ;
    ///   l'étendre au rang 12 le proposait à toute la voie générale, qui ne
    ///   l'a plus.
    ///
    /// - LA PHILOSOPHIE. Elle n'existe qu'en terminale, et pas dans la voie
    ///   professionnelle. Sans exclusion, un élève de terminale pro se verrait
    ///   proposer une matière qu'il n'aura jamais, et un professeur lui
    ///   parlerait d'une épreuve qu'il ne passera pas.
    ///
    /// POURQUOI EN CODE ET NON EN BASE
    /// -------------------------------
    /// C'est une donnée de RÉFÉRENCE — elle décrit l'école, pas l'exploitation.
    /// Elle ne change que quand le ministère change les programmes, et jamais
    /// d'un déploiement à l'autre. Les bornes de niveau des matières vivent
    /// déjà en code pour la même raison, dans le semeur ; cette table est leur
    /// prolongement, pas une exception.
    ///
    /// Une colonne en base aurait coûté une migration, et surtout aurait laissé
    /// croire qu'un administrateur peut la modifier — ce qui reviendrait à lui
    /// faire décider seul du programme national.
    /// </summary>
    public static class VoiesScolaires
    {
        private static readonly IReadOnlyDictionary<string, string[]> ParMatiere =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // Plus de français après la première, sauf en voie
                // professionnelle où il continue jusqu'au bac.
                ["FRANCAIS"] = ["TERMINALE", "TERMINALE_TECHNO"],

                // Pas de philosophie au bac professionnel.
                ["PHILOSOPHIE"] = ["TERMINALE_PRO"],

                // PAS DE SVT AU BAC PROFESSIONNEL — AUCUNE ANNÉE, AUCUNE
                // SPÉCIALITÉ.
                //
                // L enseignement général du bac pro tient en français,
                // histoire-géographie-EMC, mathématiques, langue vivante,
                // physique-chimie pour les secteurs industriels, économie et
                // prévention-santé-environnement. La SVT n y figure pas.
                //
                // Elle était pourtant proposée : ouverte du rang 7 au rang 12
                // sans exclusion de voie, elle apparaissait dans la grille d un
                // élève de seconde professionnelle comme une matière à
                // travailler. Treize compétences de son année portaient sur un
                // programme qu il ne verra jamais.
                //
                // LA PHYSIQUE-CHIMIE RESTE, ELLE, et c est un choix : elle est
                // au programme des spécialités industrielles et de plusieurs
                // spécialités tertiaires. La base ne connaît pas la spécialité
                // de l élève — la proposer à tous vaut mieux que de la retirer
                // à ceux qui l ont.
                ["SVT"] = ["SECONDE_PRO", "PREMIERE_PRO", "TERMINALE_PRO"],
            };

        /// <summary>
        /// Les codes de niveaux où cette matière ne doit PAS être proposée,
        /// même si le rang de l'élève tombe dans ses bornes.
        ///
        /// Rend un tableau vide plutôt que null : l'appelant enchaîne sur un
        /// `Contains`, et un null l'obligerait à se protéger partout où la
        /// règle ne s'applique pas — c'est-à-dire presque partout.
        /// </summary>
        public static string[] NiveauxExclus(string? codeMatiere) =>
            codeMatiere is not null && ParMatiere.TryGetValue(codeMatiere, out var exclus)
                ? exclus
                : [];

        /// <summary>
        /// Cette matière figure-t-elle à l emploi du temps de cette classe ?
        ///
        /// LA RÈGLE N EXISTE QU ICI, ET C EST TOUT L OBJET DE CETTE MÉTHODE.
        ///
        /// Elle était écrite deux fois : une fois en C# pour le serveur, une
        /// fois en JavaScript pour la grille. Le serveur, lui, ne la vérifiait
        /// nulle part — il contrôlait qu une matière EXISTE, jamais qu elle
        /// concerne l élève. Un enfant de sixième a donc pu ouvrir un cours de
        /// philosophie : la grille l affichait par un défaut d affichage, et
        /// rien derrière ne l a arrêté.
        ///
        /// Un filtre d interface est un CONFORT. Il évite de proposer ce qui
        /// n a pas de sens ; il n interdit rien. Ce qui interdit, c est ce
        /// contrôle-ci, appelé au moment d ouvrir une séance.
        /// </summary>
        public static bool EstAuProgramme(Matiere? matiere, NiveauScolaire? niveau)
        {
            if (matiere is null || niveau is null) return false;

            if (niveau.Ordre < matiere.NiveauOrdreMin) return false;
            if (niveau.Ordre > matiere.NiveauOrdreMax) return false;

            return !NiveauxExclus(matiere.Code)
                .Contains(niveau.Code, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Les voies auxquelles appartient une classe de lycée.
        ///
        /// La seconde est GÉNÉRALE ET TECHNOLOGIQUE : une seule classe pour
        /// deux voies, la séparation n'ayant lieu qu'en première. Elle porte
        /// donc les deux lettres, et c'est la seule.
        ///
        /// Les classes absentes de cette table — du CP à la troisième — sont
        /// le tronc commun : elles ne relèvent d'aucune voie, et leurs
        /// compétences concernent tout le monde.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string[]> VoiesDuNiveau =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["SECONDE"]          = ["G", "T"],
                ["PREMIERE"]         = ["G"],
                ["TERMINALE"]        = ["G"],
                ["PREMIERE_TECHNO"]  = ["T"],
                ["TERMINALE_TECHNO"] = ["T"],
                ["SECONDE_PRO"]      = ["P"],
                ["PREMIERE_PRO"]     = ["P"],
                ["TERMINALE_PRO"]    = ["P"],
            };

        /// <summary>
        /// Une compétence écrite pour telle classe concerne-t-elle un élève de
        /// telle autre classe ?
        ///
        /// Répond OUI dès qu'un doute subsiste : hors du lycée il n'y a pas de
        /// voie, et un élève de troisième qui regarde le rang au-dessus n'a
        /// encore choisi ni la voie générale, ni la professionnelle.
        /// </summary>
        public static bool NiveauConcerne(string? codeEleve, string? codeCompetence)
        {
            if (codeCompetence is null
                || !VoiesDuNiveau.TryGetValue(codeCompetence, out var voiesCompetence))
                return true;

            if (codeEleve is null
                || !VoiesDuNiveau.TryGetValue(codeEleve, out var voiesEleve))
                return true;

            return voiesCompetence.Intersect(voiesEleve, StringComparer.OrdinalIgnoreCase).Any();
        }

        /// <summary>
        /// Ne garder que les compétences qui concernent la voie de l'élève —
        /// AVEC REPLI SUR L'HÉRITAGE PAR RANG.
        ///
        /// POURQUOI UN REPLI, ET NON UN FILTRE SEC.
        /// ----------------------------------------
        /// Le référentiel est écrit pour les classes générales, et les autres
        /// voies en héritent par le rang d'année : c'est ce qui donne un
        /// programme à une première professionnelle sans qu'on ait rien écrit
        /// pour elle. Filtrer sèchement sur la voie viderait sa carte de tout
        /// ce dont elle vit aujourd'hui.
        ///
        /// La règle est donc : SI la voie de l'élève a son propre référentiel
        /// pour cette matière et ce rang, il remplace l'héritage ; SINON
        /// l'héritage continue de s'appliquer, inchangé.
        ///
        /// Concrètement, un élève de terminale professionnelle reçoit les
        /// mathématiques du bac pro — écrites pour lui — et continue de
        /// recevoir la philosophie de terminale générale, faute de mieux.
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

            // Les couples (matière, rang) où la voie de l'élève a écrit le
            // sien. Là, et seulement là, l'héritage s'efface.
            var specialises = liste
                .Where(c => NiveauConcerne(codeNiveauEleve, codeNiveau(c)))
                .Select(groupe)
                .ToHashSet();

            return liste
                .Where(c => !specialises.Contains(groupe(c))
                            || NiveauConcerne(codeNiveauEleve, codeNiveau(c)))
                .ToList();
        }
    }
}
