using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LA VEILLE DES PROGRAMMES LIT AUSSI L'INTERNET ARCHIVE (15/10/2026) : les
    /// sites du ministère bloquent désormais les robots ; quand la lecture
    /// directe échoue, la page est relevée sur sa dernière copie archivée.
    /// </summary>
    /// <remarks>
    /// Une colonne neuve, <c>copie_archive_du</c> : la date de la copie lue,
    /// nulle quand la page a été lue en direct.
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261108000000_CopieArchiveVeille")]
    public partial class CopieArchiveVeille : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "copie_archive_du",
                table: "EcheanceReferentiel",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "copie_archive_du",
                table: "EcheanceReferentiel");
        }
    }
}
