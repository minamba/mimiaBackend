namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Les deux visages d'une même planche.
    ///
    /// POURQUOI DES CHAÎNES ET NON UNE ÉNUMÉRATION
    /// ------------------------------------------
    /// Cette valeur voyage dans une adresse (`/planches/hg-france-regions/muette`),
    /// dans une balise d'ardoise (`SCHEMA:hg-france-regions/muette`) et dans une
    /// colonne lue à l'œil en base. Une énumération obligerait à traduire aux
    /// trois frontières, pour un gain nul : il n'y en aura jamais dix.
    /// </summary>
    public static class VariantePlanche
    {
        /// <summary>La planche telle qu'elle est importée depuis toujours : avec ses mots.</summary>
        public const string Legende = "legende";

        /// <summary>
        /// La même figure sans aucune légende, pour interroger.
        ///
        /// Elle n'a ni description ni carte de repères à elle : elle emprunte
        /// celles de sa légendée, parce que c'est le même fond au même cadrage.
        /// </summary>
        public const string Muette = "muette";

        /// <summary>
        /// La variante demandée, ramenée à l'une des deux. Tout ce qui n'est pas
        /// reconnu vaut <see cref="Legende"/> — une adresse fantaisiste rend la
        /// planche normale plutôt qu'un 404 incompréhensible.
        /// </summary>
        public static string Normaliser(string? brut) =>
            string.Equals(brut?.Trim(), Muette, System.StringComparison.OrdinalIgnoreCase)
                ? Muette
                : Legende;
    }
}
