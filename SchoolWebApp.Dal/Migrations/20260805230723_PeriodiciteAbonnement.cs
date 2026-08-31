using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class PeriodiciteAbonnement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "periodicite",
                table: "Abonnement",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Mensuel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "periodicite",
                table: "Abonnement");
        }
    }
}
