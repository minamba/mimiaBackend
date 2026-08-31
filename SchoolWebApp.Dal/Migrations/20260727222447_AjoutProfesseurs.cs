using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AjoutProfesseurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prof_avatar",
                table: "Matiere",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prof_couleur",
                table: "Matiere",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prof_prenom",
                table: "Matiere",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prof_avatar",
                table: "Matiere");

            migrationBuilder.DropColumn(
                name: "prof_couleur",
                table: "Matiere");

            migrationBuilder.DropColumn(
                name: "prof_prenom",
                table: "Matiere");
        }
    }
}
