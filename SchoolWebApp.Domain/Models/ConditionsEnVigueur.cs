namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES CONDITIONS GÉNÉRALES QUE LES PARENTS DOIVENT AVOIR ACCEPTÉES — Camara, le 08/11/2026 : « à chaque fois qu'on
    /// mettra à jour les CGU, la popup devra apparaître pour les parents déjà inscrits ; une fois validée, elle
    /// n'apparaîtra plus jusqu'à la prochaine mise à jour ».
    ///
    /// POUR PUBLIER UNE NOUVELLE VERSION, DEUX GESTES : changer le texte et la version dans `ConditionsVente.js` (front),
    /// puis ici `Version` et `Depuis`. Tant que `Version` ne change pas, personne ne revoit la fenêtre.
    /// </summary>
    public static class ConditionsEnVigueur
    {
        public const string Version = "1.0";

        /// <summary>
        /// Depuis quand cette version s'applique (UTC). Une case cochée à l'inscription avant cette date portait sur la
        /// version d'avant : elle ne compte pas.
        /// </summary>
        public static readonly DateTime Depuis = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Le parent a-t-il accepté la version en vigueur ?</summary>
        public static bool Acceptees(string? versionDuParent) => versionDuParent == Version;

        /// <summary>La case cochée à l'inscription, à cette date, porte-t-elle sur la version en vigueur ?</summary>
        public static bool ValentPourLaCaseDu(DateTime? cocheeLe) => cocheeLe is { } le && le >= Depuis;

        /// <summary>
        /// Rien à « mettre à jour » pour lui : il n'a jamais rien accepté, et son compte est plus récent que le texte. C'est
        /// le parent arrivé par Google, qui n'a pas vu la case : lui dire « nous avons mis à jour » serait faux.
        /// </summary>
        public static bool PremiereFois(string? versionDuParent, DateTime compteCreeLe) =>
            versionDuParent is null && compteCreeLe >= Depuis;
    }
}
