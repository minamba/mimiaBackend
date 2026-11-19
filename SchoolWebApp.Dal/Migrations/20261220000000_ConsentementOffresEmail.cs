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

            // La source du dernier geste et le journal du consentement, en ajout seul (19/11/2026, retour d'Omar).
            migrationBuilder.AddColumn<string>(
                name: "offres_email_source",
                table: "Parent",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsentementOffresJournal",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    consenti = table.Column<bool>(type: "bit", nullable: false),
                    source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    le = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentementOffresJournal", x => x.id);
                    table.ForeignKey(
                        name: "FK_ConsentementOffresJournal_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentementOffresJournal_parent_id",
                table: "ConsentementOffresJournal",
                column: "parent_id");
        }

        /// <inheritdoc />
        /// <remarks>
        /// NE JAMAIS LANCER EN PRODUCTION : `Down` supprime les colonnes ET le journal, c'est-à-dire les preuves de
        /// consentement. Retour arrière = remettre l'ancien binaire ; les colonnes et la table en trop ne gênent pas
        /// l'ancienne version.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentementOffresJournal");

            migrationBuilder.DropColumn(
                name: "offres_email_source",
                table: "Parent");

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
