namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une échéance de référentiel, avec son suivi complet — pour l'écran d'administration.</summary>
    public record EcheanceReferentielDetail(
        int Id,
        string MatiereLibelle,
        string NiveauxConcernes,
        DateTime DateEcheance,
        bool DateConnue,
        bool Sentinelle,
        string? TexteOfficiel,
        string? Notes,
        string? Url,
        string? DernierStatutVeille,
        DateTime? DernierePageVerifieeLe,
        DateTime? DerniereAlerteLe,
        DateTime? TraiteeLe);
}
