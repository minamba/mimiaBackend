namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une notion trouvée dans le cours importé. `Rang` ne sert qu'aux
    /// COMPLÉMENTS (16/10/2026) : le rang de la même notion dans le cours
    /// complété — un doublon, dont la fiche remplacera l'ancienne —, ou, une
    /// fois sa fiche écrite, le rang où cette notion nouvelle a été ajoutée.
    /// </summary>
    public sealed record NotionDuCours(string Titre, string? Resume, int? Rang = null);

    /// <summary>Ce qu'il faut pour écrire les flash cards et le quiz d'une fiche (16/10/2026).</summary>
    public sealed record FichePourEntrainement(
        int Id, string Titre, string Contenu, string? Quiz, string MatiereLibelle, string ClasseLibelle, int Rang);

    /// <summary>Le complément en route d'un cours (ou raté depuis peu), pour la page du cours.</summary>
    public sealed record ComplementDuCours(int Id, string Etat, string? Remarque);

    /// <summary>Une pièce déposée (photo, image ou PDF), sans ses octets.</summary>
    public sealed record PageDuCours(int Id, int Ordre, string? NomFichier, string TypeMime, int Taille, int NombrePages);

    /// <summary>Une fiche générée, avec ou sans son contenu (JSON des briques).</summary>
    public sealed record FicheDuCours(
        int Id, int CoursId, string Type, string Titre, int? NotionRang, int? NotionTotal,
        DateTime DateCreation, string? Contenu);

    /// <summary>Un cours importé, tel que l'élève le voit.</summary>
    public sealed class CoursDeLEleve
    {
        public int Id { get; init; }

        public int? EleveId { get; init; }

        /// <summary>Le parent propriétaire (espace parent, 16/10/2026).</summary>
        public int? ParentId { get; init; }

        public int? MatiereId { get; init; }

        /// <summary>La catégorie d'un cours de parent ; `MatiereLibelle` porte alors son nom.</summary>
        public int? CategorieId { get; init; }

        public string? CategorieCouleur { get; init; }

        public string? MatiereLibelle { get; init; }

        public string? MatiereCode { get; init; }

        public string? Titre { get; init; }

        public string Etat { get; init; } = null!;

        public string? Remarque { get; init; }

        public string Type { get; init; } = null!;

        public IReadOnlyList<NotionDuCours> Notions { get; init; } = [];

        public IReadOnlyList<int> NotionsDemandees { get; init; } = [];

        public DateTime? DemandeLe { get; init; }

        public bool PagesDisponibles { get; init; }

        public DateTime DateCreation { get; init; }

        /// <summary>Ce cours complète celui-ci (16/10/2026) ; null pour un cours.</summary>
        public int? ComplementDe { get; init; }

        /// <summary>Le titre du cours complété, pour l'en-tête du complément.</summary>
        public string? ComplementDeTitre { get; init; }

        /// <summary>Sur un cours : son complément en route, ou raté depuis moins d'un jour.</summary>
        public ComplementDuCours? Complement { get; init; }

        public IReadOnlyList<PageDuCours> Pages { get; init; } = [];

        public IReadOnlyList<FicheDuCours> Fiches { get; init; } = [];
    }

    /// <summary>Ce dont la tâche de fond a besoin pour lire un cours ou en écrire les fiches.</summary>
    public sealed class CoursATraiter
    {
        public int Id { get; init; }

        public int? EleveId { get; init; }

        /// <summary>Le parent propriétaire (espace parent, 16/10/2026).</summary>
        public int? ParentId { get; init; }

        public int? MatiereId { get; init; }

        public string MatiereLibelle { get; init; } = null!;

        public string ClasseLibelle { get; init; } = null!;

        /// <summary>1 = CP … 12 = terminale (`NiveauScolaire.Ordre`).</summary>
        public int Rang { get; init; }

        public string Etat { get; init; } = null!;

        public string? Titre { get; init; }

        public string Type { get; init; } = null!;

        public IReadOnlyList<NotionDuCours> Notions { get; init; } = [];

        /// <summary>La famille est administratrice : plus de notions à la lecture (`FichesRevisionEleve.Limites`).</summary>
        public bool SansLimite { get; init; }

        public IReadOnlyList<int> NotionsDemandees { get; init; } = [];

        public DateTime? DemandeLe { get; init; }

        public int Tentatives { get; init; }

        /// <summary>Ce que le cours compte dans le quota (1, ou 2 au-delà de dix pages).</summary>
        public int Poids { get; init; } = 1;

        /// <summary>Le cours que celui-ci complète ; null pour un cours.</summary>
        public int? ComplementDe { get; init; }

        /// <summary>Pour un complément : les notions déjà dans le cours complété, dans l'ordre.</summary>
        public IReadOnlyList<string> NotionsExistantes { get; init; } = [];

        /// <summary>Dans l'ordre de dépôt : c'est l'ordre des pages du cours.</summary>
        public IReadOnlyList<(string TypeMime, string? NomFichier, byte[] Donnees)> Pages { get; init; } = [];

        /// <summary>Les rangs de notion déjà rédigés pour la demande en cours (0 = le cours entier).</summary>
        public IReadOnlyList<int> DejaRediges { get; init; } = [];
    }
}
