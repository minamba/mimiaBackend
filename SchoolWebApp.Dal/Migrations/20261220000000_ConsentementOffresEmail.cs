using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class ConsentementOffresEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "offres_email",
                table: "Parent",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "offres_email_consenti_le",
                table: "Parent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "offres_email_retire_le",
                table: "Parent",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "offres_email",
                table: "Parent");

            migrationBuilder.DropColumn(
                name: "offres_email_consenti_le",
                table: "Parent");

            migrationBuilder.DropColumn(
                name: "offres_email_retire_le",
                table: "Parent");
        }
    }
}
