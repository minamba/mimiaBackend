namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Le texte que le professeur a écrit pour le bilan d'une semaine
    /// (17/10/2026) : écrit une fois, relu par l'envoi du lundi comme par le
    /// bouton « Voir le bilan hebdomadaire ».
    /// </summary>
    public interface IBilansRedigesRepository
    {
        /// <summary>La réponse gardée pour cette semaine, ou null.</summary>
        Task<string?> LireAsync(int eleveId, DateTime finSemaine, CancellationToken ct = default);

        /// <summary>Garde la réponse (la remplace si elle existe) et purge les vieilles semaines.</summary>
        Task EnregistrerAsync(int eleveId, DateTime finSemaine, string reponse, CancellationToken ct = default);
    }
}
