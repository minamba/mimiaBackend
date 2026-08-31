using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SchoolWebApp.IdentityServer.Data
{
    /// <summary>
    /// Contexte de la base d'identité (SCHOOL_Identity_Database).
    /// Contient les tables ASP.NET Identity + les tables OpenIddict
    /// (applications, autorisations, scopes, jetons).
    /// Distinct de SchoolWebAppDatabaseContext, qui porte le métier.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Prenom).HasMaxLength(100);
                entity.Property(e => e.Nom).HasMaxLength(100);
                entity.Property(e => e.FournisseurExterne).HasMaxLength(50);
            });
        }
    }
}
