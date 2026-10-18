using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La cible des courriels automatiques programmes par cible (17/10/2026).
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261115000000_CibleCourrielsAutomatiques")]
    public partial class CibleCourrielsAutomatiques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cible",
                table: "ModeleMail",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cible",
                table: "ModeleMail");
        }
    }
}
