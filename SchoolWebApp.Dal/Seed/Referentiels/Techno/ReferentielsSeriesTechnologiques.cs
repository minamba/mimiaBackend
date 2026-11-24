namespace SchoolWebApp.Dal.Seed.Referentiels.Techno
{
    /// <summary>
    /// LA RÉPARTITION DES PROGRAMMES DES SÉRIES TECHNOLOGIQUES ENTRE LES MATIÈRES.
    ///
    /// Trois séries depuis le 14/09/2026 — STMG, ST2S, STL —, quatre depuis le 23/11/2026 avec la STI2D,
    /// une classe par année. Les fichiers `Referentiel<Série>` recopient les textes
    /// officiels tels qu'ils sont découpés ; une matière de Mimia, elle, est un
    /// professeur. Quand un programme est enseigné par deux professeurs, il se
    /// partage ici, et nulle part ailleurs :
    ///
    ///   - la terminale ST2S « chimie, biologie et physiopathologie humaines » :
    ///     la chimie à Yann, la biologie à Inès ;
    ///   - la terminale STMG « management, sciences de gestion et numérique » :
    ///     le domaine de chaque ligne dit si elle relève du management ou de la
    ///     gestion ;
    ///   - la « physique-chimie et mathématiques » de STI2D, première et
    ///     terminale : sa physique-chimie à Yann, ses maths à Nora (décision de
    ///     Camara, 23/11/2026). Le texte sépare déjà les deux programmes. Même
    ///     partage pour celle de STL, un AUTRE texte (ajoutée le même jour).
    ///
    /// LES OPTIONS DE TERMINALE NE SONT PAS DES CLASSES. Les quatre
    /// enseignements spécifiques de STMG se rangent dans les sciences de
    /// gestion, chacun signalé par son nom dans le domaine : l'élève n'en suit
    /// qu'un, et c'est au professeur de le lui demander. Même règle pour les
    /// quatre enseignements spécifiques de STI2D (AC, EE, ITEC, SIN), rangés
    /// dans l'ingénierie de Lucas.
    ///
    /// UNE MATIÈRE EXISTANTE GARDE SON FICHIER : ce qui s'y ajoute passe dans le
    /// même appel au semeur, faute de quoi il rendrait obsolètes les lignes de
    /// l'autre.
    /// </summary>
    public static class ReferentielsSeriesTechnologiques
    {
        private static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Ensemble(
            params IEnumerable<(string Niveau, string Code, string Domaine, string Libelle, int Ordre)>[] parties) =>
            parties.SelectMany(p => p).ToArray();

        private static bool EstChimie((string Niveau, string Code, string Domaine, string Libelle, int Ordre) c) =>
            c.Domaine.StartsWith("Chimie", StringComparison.OrdinalIgnoreCase);

        private static bool EstManagement((string Niveau, string Code, string Domaine, string Libelle, int Ordre) c) =>
            c.Domaine.StartsWith("Management", StringComparison.OrdinalIgnoreCase);

        /// <summary>Une option de terminale, nommée dans le domaine de chacune de ses lignes.</summary>
        private static IEnumerable<(string Niveau, string Code, string Domaine, string Libelle, int Ordre)> Option(
            IEnumerable<(string Niveau, string Code, string Domaine, string Libelle, int Ordre)> tableau, string nom) =>
            tableau.Select(c => (c.Niveau, c.Code, $"Option {nom} — {c.Domaine}", c.Libelle, c.Ordre));

        // ------------------------------------------------------------ STMG
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SciencesGestion =>
            Ensemble(
                ReferentielStmg.SGN1,
                ReferentielStmg.MSGNT.Where(c => !EstManagement(c)),
                Option(ReferentielStmg.GFT, "gestion et finance"),
                Option(ReferentielStmg.MERT, "mercatique"),
                Option(ReferentielStmg.RHCT, "ressources humaines et communication"),
                Option(ReferentielStmg.SIGT, "systèmes d'information de gestion"));

        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Management =>
            Ensemble(ReferentielStmg.MGT1, ReferentielStmg.MSGNT.Where(EstManagement));

        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] DroitEconomie =>
            Ensemble(ReferentielStmg.DE1, ReferentielStmg.DET);

        // ------------------------------------------------------------ ST2S
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] SanitaireSocial =>
            Ensemble(ReferentielSt2s.STSS1, ReferentielSt2s.STSST);

        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] BiologieHumaine =>
            Ensemble(ReferentielSt2s.BPH1, ReferentielSt2s.CBPHT.Where(c => !EstChimie(c)));

        // ------------------------------------------------------------- STL
        /// <summary>
        /// Biochimie-biologie (première, toute la série), biotechnologies
        /// (première, au choix) et biochimie, biologie et biotechnologies
        /// (terminale). L'élève qui a choisi les sciences physiques et chimiques
        /// en laboratoire n'en suit que la biochimie-biologie de première.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Biotechnologies =>
            Ensemble(ReferentielStl.BB1, ReferentielStl.BIO1, ReferentielStl.BBBT);

        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Spcl =>
            Ensemble(ReferentielStl.SPCL1, ReferentielStl.SPCLT);

        // ----------------------------------------------------------- STI2D
        /// <summary>
        /// Tout l'enseignement technologique de la STI2D, chez Lucas :
        /// innovation technologique et I2D en première, tronc commun de 2I2D en
        /// terminale, puis les quatre enseignements spécifiques, rangés comme
        /// les options de STMG — même classe, nom de l'option dans le domaine.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Ingenierie =>
            Ensemble(
                ReferentielSti2d.IT1,
                ReferentielSti2d.I2D1,
                ReferentielSti2d.I2DT,
                Option(ReferentielSti2d.AC, "architecture et construction"),
                Option(ReferentielSti2d.EE, "énergies et environnement"),
                Option(ReferentielSti2d.ITEC, "innovation technologique et éco-conception"),
                Option(ReferentielSti2d.SIN, "systèmes d'information et numérique"));

        // ---------------------------- tronc commun de la voie technologique
        /// <summary>
        /// Français de première : même programme que la voie générale, mais un
        /// autre écrit (commentaire guidé ou contraction et essai, jamais de
        /// dissertation). COMPLET : il remplace celui de la voie générale pour
        /// les élèves technologiques.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Francais =>
            Ensemble(ReferentielTroncCommunTechno.FrancaisPremiereTechno);

        /// <summary>Histoire-géographie et EMC de la voie technologique, première et terminale.</summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] HistoireGeo =>
            Ensemble(
                ReferentielTroncCommunTechno.HistoireGeoPremiereTechno,
                ReferentielTroncCommunTechno.HistoireGeoTerminaleTechno);

        /// <summary>Philosophie de terminale technologique : sept notions, pas dix-sept.</summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Philosophie =>
            Ensemble(ReferentielTroncCommunTechno.PhilosophieTerminaleTechno);

        // --------------------------------------- matières déjà existantes
        /// <summary>
        /// La physique-chimie de ST2S (physique-chimie pour la santé en
        /// première, partie chimie de la terminale) et la partie physique-chimie
        /// des « physique-chimie et mathématiques » de STI2D et de STL.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] PhysiqueChimie =>
            Ensemble(
                ReferentielSt2s.PCS1, ReferentielSt2s.CBPHT.Where(EstChimie),
                ReferentielSti2dPcm.PCMP1, ReferentielSti2dPcm.PCMPT,
                ReferentielStlPcm.PCMLP1, ReferentielStlPcm.PCMLPT);

        /// <summary>
        /// La partie mathématiques des « physique-chimie et mathématiques »
        /// de STI2D et de STL. Elle s'ajoute au tronc commun de maths de la voie
        /// technologique, déjà dans `ReferentielMaths`.
        /// </summary>
        public static (string Niveau, string Code, string Domaine, string Libelle, int Ordre)[] Maths =>
            Ensemble(
                ReferentielSti2dPcm.PCMM1, ReferentielSti2dPcm.PCMMT,
                ReferentielStlPcm.PCMLM1, ReferentielStlPcm.PCMLMT);
    }
}
