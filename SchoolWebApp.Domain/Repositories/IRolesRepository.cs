using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    /// <summary>
    /// Les rôles des comptes parents (17/10/2026) — voir <see cref="RolesComptes"/>.
    /// Toutes les écritures sont réservées au super-administrateur (contrôleur).
    /// </summary>
    public interface IRolesRepository
    {
        /// <summary>Tous les rôles, dans l'ordre d'affichage, avec le nombre de comptes qui les portent.</summary>
        Task<IReadOnlyList<RoleCompte>> ListerAsync(CancellationToken ct = default);

        Task<ResultatRole> CreerAsync(string nom, IEnumerable<string>? onglets, IEnumerable<string>? courriels = null, CancellationToken ct = default);

        /// <summary>Renomme et redéfinit les droits et les courriels ; recalcule l'accès des comptes qui le portent.</summary>
        Task<ResultatRole> ModifierAsync(int id, string nom, IEnumerable<string>? onglets, IEnumerable<string>? courriels = null, CancellationToken ct = default);

        /// <summary>
        /// Les adresses des comptes dont le rôle reçoit ce courriel (code de <see cref="CourrielsDeRole"/>),
        /// sans doublon. L'adresse de Camara n'y est pas : elle a son interrupteur à part.
        /// </summary>
        Task<IReadOnlyList<string>> AdressesPourCourrielAsync(string courriel, CancellationToken ct = default);

        /// <summary>Supprime un rôle CRÉÉ ; ses comptes redeviennent « Utilisateur ».</summary>
        Task<ResultatRole> SupprimerAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Donne un rôle à un parent (null = Utilisateur) et met à jour son accès
        /// à l'administration. Refuse le rôle Super-admin. Faux si le parent ou le
        /// rôle n'existe pas.
        /// </summary>
        Task<(bool Fait, string? Erreur)> AttribuerAsync(int parentId, int? roleId, CancellationToken ct = default);
    }
}
