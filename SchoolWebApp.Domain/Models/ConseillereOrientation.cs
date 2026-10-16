namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LA CONSEILLÈRE D'ORIENTATION — Camara, le 11/10/2026 : « accompagner les
    /// élèves dans leur orientation : choix des études, filières, métiers,
    /// projets professionnels ; après la 3e, après la 2de, après le bac ».
    ///
    /// CE N'EST PAS UNE MATIÈRE COMME LES AUTRES, MAIS ELLE EN PREND LA FORME.
    /// Elle s'ouvre depuis la grille, a son prénom, sa voix et ses séances, et
    /// passe donc par tout ce qui existe déjà (micro, voix, archivage, heures
    /// de l'abonnement). Ce qui la distingue vit ailleurs : sa consigne
    /// (<c>PromptsOrientation</c>) et l'offre réelle des lycées, reçue à chaque
    /// tour (<c>ContexteOrientation</c>).
    ///
    /// UN INTERRUPTEUR DANS ADMIN › MODES, comme les langues : « afficher ou non
    /// la conseillère, qu'elle soit visible ou non dans l'application ». Éteinte,
    /// elle n'existe pour personne — grille, page d'accueil, ouverture d'une
    /// séance. ELLE NAÎT ÉTEINTE : elle se montre quand Camara le décide.
    /// </summary>
    public static class ConseillereOrientation
    {
        /// <summary>Le code de sa « matière » en base.</summary>
        public const string MatiereCode = "ORIENTATION";

        /// <summary>Son agent : il choisit sa consigne.</summary>
        public const string AgentSlug = "agent-orientation";

        /// <summary>Son avatar : il porte son genre, sa voix, son visage.</summary>
        public const string Avatar = "maelle";

        /// <summary>La clé de son interrupteur dans la table des réglages.</summary>
        public const string Reglage = "MODULE_ORIENTATION";

        /// <summary>Tant que personne n'a touché l'interrupteur : éteinte.</summary>
        public const bool AllumeeParDefaut = false;

        /// <summary>Est-elle allumée, au vu des réglages enregistrés ?</summary>
        public static bool EstAllumee(IReadOnlyDictionary<string, string> reglages) =>
            reglages.TryGetValue(Reglage, out var valeur)
                ? string.Equals(valeur, "true", StringComparison.OrdinalIgnoreCase)
                : AllumeeParDefaut;

        /// <summary>Ce code de matière est-il celui de la conseillère ?</summary>
        public static bool Est(string? matiereCode) =>
            string.Equals(matiereCode, MatiereCode, StringComparison.OrdinalIgnoreCase);
    }
}
