using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class RapportParSeance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RapportSeance_conversation_id",
                table: "RapportSeance");

            migrationBuilder.CreateIndex(
                name: "IX_RapportSeance_conversation_id",
                table: "RapportSeance",
                column: "conversation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RapportSeance_conversation_id",
                table: "RapportSeance");

            migrationBuilder.CreateIndex(
                name: "IX_RapportSeance_conversation_id",
                table: "RapportSeance",
                column: "conversation_id",
                unique: true);
        }
    }
}
