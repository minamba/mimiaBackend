using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// COMPLÉTER UN COURS (Camara, le 16/10/2026) : un import peut compléter un
    /// cours déjà fait ; ses fiches vont dans ce cours.
    /// </summary>
    /// <remarks>
    /// Une colonne neuve, <c>complement_de</c>, nulle pour tous les cours
    /// existants, et son index. Sans clé étrangère : une référence sur la même
    /// table ne peut pas être en cascade sous SQL Server.
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261106000000_ComplementCours")]
    public partial class ComplementCours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "complement_de",
                table: "CoursImporte",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursImporte_complement_de",
                table: "CoursImporte",
                column: "complement_de");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoursImporte_complement_de",
                table: "CoursImporte");

            migrationBuilder.DropColumn(
                name: "complement_de",
                table: "CoursImporte");
        }
    }
}
