using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Camara, le 06/11/2026 :
    /// - « Effectué le » prend l'HEURE : la colonne passe de date à datetime2 (les dates déjà posées restent, à minuit) ;
    /// - un signalement admin traité puis repassé KO par le testeur est ROUVERT : quand, et combien de fois.
    /// Datée après les migrations écrites à la main (jusqu'au 01/12/2026).
    /// </summary>
    public partial class ReouvertureEtHeureDesTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "date_effectuee",
                table: "LigneDeTest",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rouvert_le",
                table: "Signalement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reouvertures",
                table: "Signalement",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "rouvert_le", table: "Signalement");
            migrationBuilder.DropColumn(name: "reouvertures", table: "Signalement");

            migrationBuilder.AlterColumn<DateTime>(
                name: "date_effectuee",
                table: "LigneDeTest",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
