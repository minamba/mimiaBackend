using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Indispensable pour que `dotnet ef` construise le DbContext sans démarrer
    /// l'application. La chaîne ci-dessous est strictement locale/design-time
    /// et n'est jamais utilisée en production.
    /// </summary>
    public class SchoolWebAppDatabaseContextFactory : IDesignTimeDbContextFactory<SchoolWebAppDatabaseContext>
    {
        public SchoolWebAppDatabaseContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SchoolWebAppDatabaseContext>();
            optionsBuilder.UseSqlServer(
                "Server=.\\SQLExpress;Database=SCHOOL_Database;Trusted_Connection=True;TrustServerCertificate=True;"
            );
            return new SchoolWebAppDatabaseContext(optionsBuilder.Options);
        }
    }
}
