using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Les rôles des comptes parents (17/10/2026) — voir <see cref="RoleUtilisateur"/>.
    ///
    /// L'ACCÈS SUIT LE RÔLE, À CHAQUE ÉCRITURE : attribuer un rôle, changer ses
    /// onglets ou le supprimer recalcule <c>Parent.EstAdministrateur</c> des
    /// comptes concernés. C'est ce drapeau que lit le serveur d'identité pour
    /// mettre « Admin » dans le jeton : il reste la seule porte, tenue à jour ici.
    /// </summary>
    public class RolesRepository : IRolesRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public RolesRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IReadOnlyList<RoleCompte>> ListerAsync(CancellationToken ct = default)
        {
            var roles = await _context.RolesUtilisateurs.AsNoTracking()
                .OrderBy(r => r.Ordre).ThenBy(r => r.Nom).ToListAsync(ct);

            var comptes = await _context.Parents.AsNoTracking()
                .GroupBy(p => p.RoleId)
                .Select(g => new { RoleId = g.Key, Nombre = g.Count() })
                .ToListAsync(ct);

            int Nombre(RoleUtilisateur r) => r.Code == RolesComptes.Utilisateur
                // Sans rôle = Utilisateur : les deux se comptent ensemble.
                ? comptes.Where(c => c.RoleId == null || c.RoleId == r.Id).Sum(c => c.Nombre)
                : comptes.Where(c => c.RoleId == r.Id).Sum(c => c.Nombre);

            return roles.Select(r => Projeter(r, Nombre(r))).ToList();
        }

        public async Task<ResultatRole> CreerAsync(
            string nom, IEnumerable<string>? onglets, IEnumerable<string>? courriels = null, CancellationToken ct = default)
        {
            var propre = Nettoyer(nom);
            if (propre is null) return new ResultatRole(null, "Donnez un nom au rôle (60 caractères au plus).");
            if (await NomPrisAsync(propre, null, ct)) return new ResultatRole(null, $"Un rôle « {propre} » existe déjà.");

            var ordre = await _context.RolesUtilisateurs.Select(r => (int?)r.Ordre).MaxAsync(ct) ?? 10;
            var role = new RoleUtilisateur
            {
                Nom = propre,
                Onglets = Valeur(onglets),
                Courriels = CourrielsDeRole.Ecrire(courriels),
                Ordre = Math.Max(ordre, 10) + 1,
                DateCreation = DateTime.UtcNow,
            };

            _context.RolesUtilisateurs.Add(role);
            await _context.SaveChangesAsync(ct);
            return new ResultatRole(Projeter(role, 0), null);
        }

        public async Task<ResultatRole> ModifierAsync(
            int id, string nom, IEnumerable<string>? onglets, IEnumerable<string>? courriels = null, CancellationToken ct = default)
        {
            var role = await _context.RolesUtilisateurs.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (role is null) return new ResultatRole(null, null, Introuvable: true);

            // UTILISATEUR ET SUPER-ADMIN NE SE TOUCHENT PAS : l'un n'a aucun droit
            // par définition, l'autre a tous les droits par la configuration.
            if (role.Code is RolesComptes.Utilisateur or RolesComptes.SuperAdmin)
            {
                return new ResultatRole(null, "Ce rôle système ne se modifie pas.");
            }

            // « Administrateur » garde son nom (le serveur s'y réfère) ; ses droits, eux, se règlent.
            if (role.Code is null)
            {
                var propre = Nettoyer(nom);
                if (propre is null) return new ResultatRole(null, "Donnez un nom au rôle (60 caractères au plus).");
                if (await NomPrisAsync(propre, id, ct)) return new ResultatRole(null, $"Un rôle « {propre} » existe déjà.");
                role.Nom = propre;
            }

            role.Onglets = Valeur(onglets);
            role.Courriels = CourrielsDeRole.Ecrire(courriels);
            await _context.SaveChangesAsync(ct);

            await RecalculerAccesAsync(role, ct);
            return new ResultatRole(Projeter(role, await NombreAsync(role.Id, ct)), null);
        }

        public async Task<ResultatRole> SupprimerAsync(int id, CancellationToken ct = default)
        {
            var role = await _context.RolesUtilisateurs.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (role is null) return new ResultatRole(null, null, Introuvable: true);
            if (role.Code is not null) return new ResultatRole(null, "Un rôle système ne se supprime pas.");

            // Ses comptes redeviennent « Utilisateur », et perdent l'accès qu'il donnait.
            await _context.Parents.Where(p => p.RoleId == id)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(p => p.RoleId, (int?)null)
                    .SetProperty(p => p.EstAdministrateur, false), ct);

            _context.RolesUtilisateurs.Remove(role);
            await _context.SaveChangesAsync(ct);
            return new ResultatRole(Projeter(role, 0), null);
        }

        public async Task<(bool Fait, string? Erreur)> AttribuerAsync(int parentId, int? roleId, CancellationToken ct = default)
        {
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Id == parentId, ct);
            if (parent is null) return (false, null);

            RoleUtilisateur? role = null;
            if (roleId is int id)
            {
                role = await _context.RolesUtilisateurs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
                if (role is null) return (false, null);
                if (role.Code == RolesComptes.SuperAdmin)
                {
                    return (false, "Le rôle Super-admin ne s'attribue pas : il vient de la configuration du serveur.");
                }
            }

            // Utilisateur s'écrit null : un seul sens pour « aucun rôle particulier ».
            parent.RoleId = role is null || role.Code == RolesComptes.Utilisateur ? null : role.Id;
            parent.EstAdministrateur = role is not null && RolesComptes.OuvreAdministration(role.Code, role.Onglets);
            await _context.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<IReadOnlyList<string>> AdressesPourCourrielAsync(string courriel, CancellationToken ct = default)
        {
            // Peu de rôles : on lit leurs courriels et on choisit ici, plutôt qu'un LIKE sur une liste à virgules.
            var roles = await _context.RolesUtilisateurs.AsNoTracking()
                .Where(r => r.Courriels != null)
                .Select(r => new { r.Id, r.Courriels })
                .ToListAsync(ct);

            var ids = roles
                .Where(r => CourrielsDeRole.Lire(r.Courriels).Contains(courriel, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Id)
                .ToList();
            if (ids.Count == 0) return [];

            var mails = await _context.Parents.AsNoTracking()
                .Where(p => p.RoleId != null && ids.Contains(p.RoleId.Value) && p.Mail != null && p.Mail != "")
                .Select(p => p.Mail!)
                .ToListAsync(ct);

            return mails.Select(m => m.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ------------------------------------------------------------------

        private async Task RecalculerAccesAsync(RoleUtilisateur role, CancellationToken ct)
        {
            var ouvre = RolesComptes.OuvreAdministration(role.Code, role.Onglets);
            await _context.Parents.Where(p => p.RoleId == role.Id)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.EstAdministrateur, ouvre), ct);
        }

        private Task<int> NombreAsync(int roleId, CancellationToken ct) =>
            _context.Parents.CountAsync(p => p.RoleId == roleId, ct);

        private Task<bool> NomPrisAsync(string nom, int? sauf, CancellationToken ct) =>
            _context.RolesUtilisateurs.AnyAsync(r => r.Nom == nom && (sauf == null || r.Id != sauf), ct);

        private static string? Nettoyer(string? nom)
        {
            var propre = nom?.Trim();
            return string.IsNullOrEmpty(propre) || propre.Length > RolesComptes.NomMax ? null : propre;
        }

        private static string? Valeur(IEnumerable<string>? onglets)
        {
            var valeur = OngletsAdmin.Ecrire(onglets);
            return string.IsNullOrEmpty(valeur) ? null : valeur;
        }

        internal static RoleCompte Projeter(RoleUtilisateur r, int nombre) => new(
            r.Id, r.Nom, r.Code, OngletsAdmin.Lire(r.Onglets), nombre,
            RolesComptes.OuvreAdministration(r.Code, r.Onglets),
            CourrielsDeRole.Lire(r.Courriels));
    }
}
