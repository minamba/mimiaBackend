namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Une échéance telle que le worker la manipule : ce qu'il faut pour
    /// relever sa page, décider d'alerter, et composer le courriel.
    /// </summary>
    public record EcheanceASignaler(
        int Id,
        string MatiereLibelle,
        string NiveauxConcernes,
        DateTime DateEcheance,
        bool DateConnue,
        bool Sentinelle,
        string? TexteOfficiel,
        string? Notes,
        string? Url,
        string? DernierHashPage,
        string? DernierStatutVeille);
}
