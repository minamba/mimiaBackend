using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class TranscriptionPieceJointe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "donnees_effacees_le",
                table: "PieceJointe",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transcription",
                table: "PieceJointe",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PieceJointe_donnees_effacees_le_date_creation",
                table: "PieceJointe",
                columns: new[] { "donnees_effacees_le", "date_creation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PieceJointe_donnees_effacees_le_date_creation",
                table: "PieceJointe");

            migrationBuilder.DropColumn(
                name: "donnees_effacees_le",
                table: "PieceJointe");

            migrationBuilder.DropColumn(
                name: "transcription",
                table: "PieceJointe");
        }
    }
}
