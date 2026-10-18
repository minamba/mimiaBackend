using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Dal.Seed
{
    /// <summary>
    /// LES RÔLES SYSTÈME, ET LA REPRISE DES ADMINISTRATEURS D'AVANT (17/10/2026).
    ///
    /// 1. Utilisateur, Administrateur et Super-admin existent toujours : créés
    ///    s'ils manquent, jamais renommés ni écrasés ensuite.
    ///
    /// 2. CHAQUE ADMINISTRATEUR SANS RÔLE GARDE EXACTEMENT SES DROITS. Avant les
    ///    rôles, les onglets se cochaient compte par compte (<c>OngletsAdmin</c>).
    ///    Il reçoit le rôle qui porte ces mêmes onglets s'il existe ; sinon
    ///    « Administrateur » s'il est encore vierge ; sinon un rôle
    ///    « Administrateur (2) », « (3) »… créé pour lui. Personne ne gagne ni ne
    ///    perd une section au passage.
    ///
    /// Idempotent : ne touche que ce qui manque, sûr à chaque démarrage.
    /// </summary>
    public static class RolesSeeder
    {
        public static async Task SeedAsync(SchoolWebAppDatabaseContext context, CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;
            var systeme = new (string Code, string Nom, int Ordre)[]
            {
                (RolesComptes.Utilisateur, "Utilisateur", 0),
                (RolesComptes.Administrateur, "Administrateur", 1),
                (RolesComptes.SuperAdmin, "Super-admin", 2),
            };

            foreach (var (code, nom, ordre) in systeme)
            {
                if (await context.RolesUtilisateurs.AnyAsync(r => r.Code == code, ct)) continue;
                context.RolesUtilisateurs.Add(new RoleUtilisateur { Code = code, Nom = nom, Ordre = ordre, DateCreation = maintenant });
            }

            await context.SaveChangesAsync(ct);

            var sansRole = await context.Parents
                .Where(p => p.EstAdministrateur && p.RoleId == null)
                .ToListAsync(ct);

            if (sansRole.Count == 0) return;

            var roles = await context.RolesUtilisateurs
                .Where(r => r.Code != RolesComptes.Utilisateur && r.Code != RolesComptes.SuperAdmin)
                .ToListAsync(ct);

            var administrateur = roles.First(r => r.Code == RolesComptes.Administrateur);
            var administrateurVierge = administrateur.Onglets is null
                && !await context.Parents.AnyAsync(p => p.RoleId == administrateur.Id, ct);

            // Le groupe le plus nombreux d'abord : c'est lui qui hérite de « Administrateur ».
            foreach (var groupe in sansRole
                         .GroupBy(p => OngletsAdmin.Ecrire(OngletsAdmin.Lire(p.OngletsAdmin)))
                         .OrderByDescending(g => g.Count()))
            {
                var valeur = string.IsNullOrEmpty(groupe.Key) ? null : groupe.Key;
                var role = roles.FirstOrDefault(r => (r.Onglets ?? null) == valeur && !(r == administrateur && administrateurVierge));

                if (role is null && administrateurVierge)
                {
                    administrateur.Onglets = valeur;
                    administrateurVierge = false;
                    role = administrateur;
                }

                if (role is null)
                {
                    var n = 2;
                    while (roles.Any(r => r.Nom == $"Administrateur ({n})")
                           || await context.RolesUtilisateurs.AnyAsync(r => r.Nom == $"Administrateur ({n})", ct)) n++;

                    role = new RoleUtilisateur
                    {
                        Nom = $"Administrateur ({n})",
                        Onglets = valeur,
                        Ordre = 10 + n,
                        DateCreation = maintenant,
                    };
                    context.RolesUtilisateurs.Add(role);
                    await context.SaveChangesAsync(ct);
                    roles.Add(role);
                }

                foreach (var parent in groupe)
                {
                    parent.RoleId = role.Id;
                    parent.EstAdministrateur = RolesComptes.OuvreAdministration(role.Code, role.Onglets);
                }
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
