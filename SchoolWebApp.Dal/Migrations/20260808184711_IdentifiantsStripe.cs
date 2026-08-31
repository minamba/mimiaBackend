using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class IdentifiantsStripe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stripe_client_id",
                table: "Parent",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_abonnement_id",
                table: "Abonnement",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parent_stripe_client_id",
                table: "Parent",
                column: "stripe_client_id",
                unique: true,
                filter: "[stripe_client_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Abonnement_stripe_abonnement_id",
                table: "Abonnement",
                column: "stripe_abonnement_id",
                unique: true,
                filter: "[stripe_abonnement_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parent_stripe_client_id",
                table: "Parent");

            migrationBuilder.DropIndex(
                name: "IX_Abonnement_stripe_abonnement_id",
                table: "Abonnement");

            migrationBuilder.DropColumn(
                name: "stripe_client_id",
                table: "Parent");

            migrationBuilder.DropColumn(
                name: "stripe_abonnement_id",
                table: "Abonnement");
        }
    }
}
