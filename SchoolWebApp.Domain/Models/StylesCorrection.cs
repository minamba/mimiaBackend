namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// COMMENT L'ÉLÈVE VEUT CORRIGER LA COPIE D'UN CONTRÔLE PASSÉ — voulu par
    /// Camara le 26/09/2026, sur la remontée d'un parent : « l'enfant voulait
    /// essayer de se corriger tout seul, mais le professeur a donné la
    /// réponse ».
    ///
    /// Une fois l'énoncé et la copie reçus, l'écran pose la question ; le
    /// professeur n'analyse rien avant la réponse. Le choix est ensuite
    /// rappelé au professeur à CHAQUE tour de la correction : une consigne
    /// lue une fois s'oublie en trois échanges, un fait qui voyage avec le
    /// tour, non.
    /// </summary>
    public static class StylesCorrection
    {
        /// <summary>
        /// « Je t'aide à te corriger » : le professeur ne donne JAMAIS la
        /// réponse. Il fait comprendre pourquoi c'est faux, jusqu'à ce que
        /// l'élève la trouve lui-même. Seul l'élève peut en sortir, par le
        /// bouton « Finalement, corrige et explique-moi ».
        /// </summary>
        public const string Aide = "aide";

        /// <summary>« Je corrige et t'explique » : le professeur donne la bonne réponse et explique l'erreur.</summary>
        public const string Explique = "explique";

        /// <summary>Le style tel qu'il est, ou null s'il n'est pas l'un des deux.</summary>
        public static string? Normaliser(string? style) => style switch
        {
            Aide => Aide,
            Explique => Explique,
            _ => null,
        };
    }
}
