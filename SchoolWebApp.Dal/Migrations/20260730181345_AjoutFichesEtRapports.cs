using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AjoutFichesEtRapports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FicheRevision",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: true),
                    notion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    domaine = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    contenu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_mise_a_jour = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FicheRevision", x => x.id);
                    table.ForeignKey(
                        name: "FK_FicheRevision_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FicheRevision_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RapportSeance",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    travaille = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    note_comprehension = table.Column<double>(type: "float", nullable: true),
                    note_revision = table.Column<double>(type: "float", nullable: true),
                    remarque = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    a_revoir = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapportSeance", x => x.id);
                    table.ForeignKey(
                        name: "FK_RapportSeance_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_RapportSeance_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RapportSeance_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FicheRevision_eleve_id_matiere_id_date_mise_a_jour",
                table: "FicheRevision",
                columns: new[] { "eleve_id", "matiere_id", "date_mise_a_jour" });

            migrationBuilder.CreateIndex(
                name: "IX_FicheRevision_eleve_id_matiere_id_notion",
                table: "FicheRevision",
                columns: new[] { "eleve_id", "matiere_id", "notion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FicheRevision_matiere_id",
                table: "FicheRevision",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_RapportSeance_conversation_id",
                table: "RapportSeance",
                column: "conversation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RapportSeance_eleve_id_date_creation",
                table: "RapportSeance",
                columns: new[] { "eleve_id", "date_creation" });

            migrationBuilder.CreateIndex(
                name: "IX_RapportSeance_matiere_id",
                table: "RapportSeance",
                column: "matiere_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FicheRevision");

            migrationBuilder.DropTable(
                name: "RapportSeance");
        }
    }
}
