namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// À QUI APPARTIENT UN COURS IMPORTÉ — un élève, ou un parent (16/10/2026).
    ///
    /// Camara : « le système des fiches, mais pour les parents qui en ont le
    /// droit, PAS LIÉ AUX ENFANTS ». Le même module sert les deux : seul le
    /// propriétaire change. Exactement l'un des deux est renseigné.
    ///
    /// CONVERTIBLE DEPUIS UN IDENTIFIANT D'ÉLÈVE : tout le code écrit pour
    /// l'élève (`ImportCoursService`, le dépôt, les routes de l'élève) passe un
    /// `int` comme avant, et c'est un propriétaire-élève.
    /// </summary>
    public readonly record struct Proprietaire(int? EleveId, int? ParentId)
    {
        public static Proprietaire Eleve(int eleveId) => new(eleveId, null);

        public static Proprietaire Parent(int parentId) => new(null, parentId);

        public static implicit operator Proprietaire(int eleveId) => Eleve(eleveId);

        public bool EstParent => ParentId is not null;
    }

    /// <summary>Une catégorie de fiches, créée par le parent : elle tient lieu de matière.</summary>
    public sealed record CategorieFiches(int Id, string Libelle, string Couleur, int Ordre, int NombreCours);

    /// <summary>
    /// Ce que l'espace parent ouvre à ce parent. `Fiches` : le droit, coché dans
    /// Admin › Parents (le super-administrateur l'a toujours). `SansLimite` :
    /// administrateur ou super-administrateur — ni plafond d'import, ni quota.
    /// </summary>
    public sealed record DroitsEspaceParent(bool Fiches, bool SansLimite);
}
