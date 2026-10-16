using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// UN GROS COURS COMPTE DOUBLE (Camara, le 16/10/2026) : au-delà de dix
    /// pages, un cours importé compte pour 2 dans le quota des fiches.
    /// </summary>
    /// <remarks>
    /// Une colonne neuve, <c>poids</c>, à 1 pour les cours existants.
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261105000000_PoidsCoursImporte")]
    public partial class PoidsCoursImporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "poids",
                table: "CoursImporte",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "poids",
                table: "CoursImporte");
        }
    }
}
