using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES COPIES DES BROUILLONS — Camara, le 19/10/2026 : Cc et Cci « comme sur Gmail ».
    /// Deux colonnes facultatives sur BrouillonMail : copies et copies_cachees.
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261119000000_CopiesDesBrouillons")]
    public partial class CopiesDesBrouillons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "copies",
                table: "BrouillonMail",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "copies_cachees",
                table: "BrouillonMail",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "copies",
                table: "BrouillonMail");

            migrationBuilder.DropColumn(
                name: "copies_cachees",
                table: "BrouillonMail");
        }
    }
}
