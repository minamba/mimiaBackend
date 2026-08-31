using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class PrixStripe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stripe_prix_id",
                table: "OffreRecharge",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_prix_annuel_id",
                table: "Offre",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_prix_mensuel_id",
                table: "Offre",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stripe_prix_id",
                table: "OffreRecharge");

            migrationBuilder.DropColumn(
                name: "stripe_prix_annuel_id",
                table: "Offre");

            migrationBuilder.DropColumn(
                name: "stripe_prix_mensuel_id",
                table: "Offre");
        }
    }
}
