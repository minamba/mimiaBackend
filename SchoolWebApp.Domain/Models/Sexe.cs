namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Sexe de l'élève.
    ///
    /// Donnée strictement grammaticale : le français accorde les adjectifs et
    /// les participes, et un professeur qui dit « tu es prêt ? » à une fille à
    /// chaque séance rappelle en permanence qu'il ne la connaît pas.
    ///
    /// <see cref="NonPrecise"/> vaut 0 pour que les profils créés avant l'ajout
    /// de ce champ restent valides : l'agent bascule alors sur des formulations
    /// neutres plutôt que de deviner.
    /// </summary>
    public enum Sexe
    {
        NonPrecise = 0,
        Fille = 1,
        Garcon = 2
    }
}
