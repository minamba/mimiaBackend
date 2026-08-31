using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEtatFiches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "date_consultation",
                table: "FicheRevision",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "etat",
                table: "FicheRevision",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "date_consultation",
                table: "FicheRevision");

            migrationBuilder.DropColumn(
                name: "etat",
                table: "FicheRevision");
        }
    }
}
