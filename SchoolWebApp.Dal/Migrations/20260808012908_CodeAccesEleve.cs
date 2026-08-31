using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class CodeAccesEleve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "acces_suspendu_le",
                table: "Eleve",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code_acces",
                table: "Eleve",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Eleve_code_acces",
                table: "Eleve",
                column: "code_acces",
                unique: true,
                filter: "[code_acces] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Eleve_code_acces",
                table: "Eleve");

            migrationBuilder.DropColumn(
                name: "acces_suspendu_le",
                table: "Eleve");

            migrationBuilder.DropColumn(
                name: "code_acces",
                table: "Eleve");
        }
    }
}
