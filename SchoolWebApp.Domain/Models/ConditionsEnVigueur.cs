namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// LES CONDITIONS GÉNÉRALES QUE LES PARENTS DOIVENT AVOIR ACCEPTÉES — Camara, le 08/11/2026 : « à chaque fois qu'on
    /// mettra à jour les CGU, la popup devra apparaître pour les parents déjà inscrits ; une fois validée, elle
    /// n'apparaîtra plus jusqu'à la prochaine mise à jour ».
    ///
    /// POUR PUBLIER UNE NOUVELLE VERSION, DEUX GESTES : changer le texte et la version dans `ConditionsVente.js` (front),
    /// puis ici `Version` et `Depuis`. Tant que `Version` ne change pas, personne ne revoit la fenêtre.
    ///
    /// `Version` EST CELLE DE L'ENSEMBLE ACCEPTÉ : CGV + politique de confidentialité. Changer l'un des deux textes sur le
    /// fond suffit à la faire monter.
    ///
    /// VERSION 2.0 (19/11/2026) — Min's : c'est la POLITIQUE DE CONFIDENTIALITÉ qui change (offres et nouveautés par email :
    /// finalité, consentement, durée, désinscription ; Brevo parmi les prestataires). Les CGV restent en 1.0, sans
    /// changement. Tous les parents qui ont accepté la 1.0 revoient donc la fenêtre une fois : « J'ai lu et j'accepte la
    /// politique de confidentialité » (obligatoire) et, en dessous, la case des offres à part, jamais cochée d'avance.
    /// </summary>
    public static class ConditionsEnVigueur
    {
        public const string Version = "2.0";

        /// <summary>
        /// Depuis quand cette version s'applique (UTC). Une case cochée à l'inscription avant cette date portait sur la
        /// version d'avant : elle ne compte pas.
        ///
        /// À METTRE AU JOUR DE LA MISE EN LIGNE. Plus tôt, une case cochée sur l'ancien texte vaudrait pour le nouveau ; plus
        /// tard, un inscrit de l'entre-deux revoit seulement la fenêtre une fois, sans dommage.
        /// </summary>
        public static readonly DateTime Depuis = new(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

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
