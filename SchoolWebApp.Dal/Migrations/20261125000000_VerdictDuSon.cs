using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE VERDICT DE LA VOIX SUR LE MESSAGE — Camara, le 04/11/2026 : pendant un
    /// exercice de syllabes, la bulle de l'élève écrit « Bonne prononciation » ou
    /// « On réessaie » à la place du son, et doit le refaire au
    /// rechargement de la page. NULL pour tout tour qui n'était pas un son jugé.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes : le Designer n'est pas généré.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261125000000_VerdictDuSon")]
    public partial class VerdictDuSon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "son_juste",
                table: "Message",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "son_juste",
                table: "Message");
        }
    }
}
