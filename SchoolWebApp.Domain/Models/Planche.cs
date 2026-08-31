namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une planche importée, telle qu'elle circule entre les couches.
    ///
    /// <see cref="Donnees"/> n'est chargé que pour la servir : lister les
    /// planches de l'administration ferait sinon transiter plusieurs
    /// mégaoctets pour afficher des cases à cocher.
    /// </summary>
    public class Planche
    {
        public int Id { get; set; }

        public string Cle { get; set; } = string.Empty;

        public string MatiereCode { get; set; } = string.Empty;

        public string? NomFichier { get; set; }

        public string? TypeMime { get; set; }

        public int Taille { get; set; }

        public byte[]? Donnees { get; set; }

        public string? Auteur { get; set; }

        public string? Source { get; set; }

        public string? Licence { get; set; }

        /// <summary>
        /// Planche produite par nous : aucun auteur tiers à créditer, et le
        /// ramassage automatique des crédits manquants doit la laisser
        /// tranquille.
        /// </summary>
        public bool Maison { get; set; }

        /// <summary>
        /// Étiquettes et positions, en JSON. Voir <c>PlancheSchema.Reperes</c>.
        /// </summary>
        public string? Reperes { get; set; }

        public DateTime DateCreation { get; set; }

        /// <summary>La liste des légendes de la planche, extraite à l'import.</summary>
        public string? Contenu { get; set; }

        public DateTime? DateModification { get; set; }
    }

    /// <summary>
    /// Ce qui attend d'être traité par le worker des planches.
    ///
    /// Trois files distinctes, et il faut les distinguer : décrire coûte une
    /// lecture d'image, cartographier en coûte une autre, et créditer interroge
    /// une base extérieure. Un total unique cacherait laquelle s'emballe.
    /// </summary>
    public class FilesPlanches
    {
        public List<PlancheEnAttente> ADecrire { get; set; } = [];

        public List<PlancheEnAttente> ACartographier { get; set; } = [];

        public List<PlancheEnAttente> ACrediter { get; set; } = [];
    }

    /// <summary>Une planche en attente, réduite à ce qu'on affiche.</summary>
    public class PlancheEnAttente
    {
        public string Cle { get; set; } = string.Empty;

        public string? MatiereCode { get; set; }

        /// <summary>En octets : une figure lourde coûte plus cher à lire.</summary>
        public int Taille { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
