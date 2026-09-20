namespace SchoolWebApp.Domain.Services
{
    /// <summary>
    /// Le compte du super-administrateur, reconnu par son adresse.
    ///
    /// L'INTERFACE EST ICI, L'IMPLÉMENTATION DANS L'API : c'est l'API qui lit
    /// `Admin:Emails`. Le dépôt des abonnements en a besoin depuis le
    /// 20/09/2026 — Camara : « quel que soit l'abonnement qu'il a, ne jamais
    /// l'empêcher d'ajouter des enfants, il peut en ajouter à l'infini. Cette
    /// exception est uniquement pour le compte super-admin. »
    ///
    /// Une implémentation qui ne protège personne est légitime : la limite
    /// d'enfants s'applique alors à tout le monde, comme avant.
    /// </summary>
    public interface IComptesProteges
    {
        bool Protege(string? mail);
    }
}
