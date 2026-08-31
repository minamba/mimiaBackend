using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolWebApp.IdentityServer.Data
{
    /// <summary>
    /// Permet à `dotnet ef` de construire le DbContext sans démarrer l'application.
    /// La chaîne de connexion ici est strictement locale/design-time.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(
                "Server=.\\SQLExpress;Database=SCHOOL_Identity_Database;Trusted_Connection=True;TrustServerCertificate=True;"
            );

            // OpenIddict enregistre ses entités via le DbContext, il faut donc
            // aussi les déclarer au design-time, sinon la migration les ignore.
            optionsBuilder.UseOpenIddict();

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
