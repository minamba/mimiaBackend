using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'ESSAI SANS DÉLAI — Camara, le 18/10/2026 : les 30 minutes restent valables jusqu'à ce qu'elles soient utilisées.
    /// L'offre ESSAI passe à 0 jour, et les essais en cours perdent leur date de fin.
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261118000000_EssaiSansDelai")]
    public partial class EssaiSansDelai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // L'offre : plus de délai. Le semeur ne réécrit pas une offre déjà là.
            migrationBuilder.Sql("UPDATE [Offre] SET [jours_validite] = 0 WHERE [est_essai] = 1;");

            // Les essais en cours gardent leurs minutes sans échéance. Un essai
            // déjà résilié (expiré ou remplacé par une formule) n'est pas rouvert.
            migrationBuilder.Sql(@"
UPDATE a SET a.[date_fin] = NULL
FROM [Abonnement] a
INNER JOIN [Offre] o ON o.[id] = a.[offre_id]
WHERE o.[est_essai] = 1 AND a.[statut] <> 'Resilie';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Le délai de sept jours revient ; les essais en cours repartent de
            // leur date de début.
            migrationBuilder.Sql("UPDATE [Offre] SET [jours_validite] = 7 WHERE [est_essai] = 1;");
            migrationBuilder.Sql(@"
UPDATE a SET a.[date_fin] = DATEADD(day, 7, a.[date_debut])
FROM [Abonnement] a
INNER JOIN [Offre] o ON o.[id] = a.[offre_id]
WHERE o.[est_essai] = 1 AND a.[statut] <> 'Resilie' AND a.[date_fin] IS NULL;");
        }
    }
}
