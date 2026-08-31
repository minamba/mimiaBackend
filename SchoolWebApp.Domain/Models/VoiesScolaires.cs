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
    }
}
