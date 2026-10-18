using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les pieces des templates de courriel sur le disque : colonne fichier, donnees facultatives (18/10/2026).
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261117000000_PiecesModelesMailSurLeDisque")]
    public partial class PiecesModelesMailSurLeDisque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "donnees",
                table: "PieceModeleMail",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AddColumn<string>(
                name: "fichier",
                table: "PieceModeleMail",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fichier",
                table: "PieceModeleMail");

            migrationBuilder.AlterColumn<byte[]>(
                name: "donnees",
                table: "PieceModeleMail",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);
        }
    }
}
