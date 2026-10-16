using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// FLASH CARDS ET QUIZ (Camara, le 16/10/2026) : les questions d'une fiche,
    /// écrites au premier « Me tester », gardées avec elle.
    /// </summary>
    /// <remarks>
    /// Une colonne neuve, <c>quiz</c>, nulle pour toutes les fiches existantes.
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261107000000_QuizFiche")]
    public partial class QuizFiche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "quiz",
                table: "FicheDeCours",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quiz",
                table: "FicheDeCours");
        }
    }
}
