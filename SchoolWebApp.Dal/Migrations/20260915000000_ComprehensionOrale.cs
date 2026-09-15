using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les compréhensions orales archivées : le passage lu par le
    /// professeur, ce que l'élève en a compris, et l'audio régénéré à
    /// l'archivage.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    ///
    /// Une seule écriture par ligne, jamais de mise à jour — contrairement à
    /// Dictee : un exercice de compréhension orale se conclut en un seul
    /// échange (écoute, réponse, retour), donc pas de colonne « etat » ni
    /// « date_mise_a_jour ».
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260915000000_ComprehensionOrale")]
    public partial class ComprehensionOrale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComprehensionOrale",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: true),
                    titre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    langue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    passage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    comprehension = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    remarque = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    audio_donnees = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    audio_efface_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_consultation = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprehensionOrale", x => x.id);

                    table.ForeignKey(
                        name: "FK_ComprehensionOrale_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // Restrict, pas Cascade : trois chemins de suppression
                    // convergeraient sinon vers ComprehensionOrale (élève,
                    // matière, conversation), et SQL Server refuse les
                    // cascades multiples.
                    table.ForeignKey(
                        name: "FK_ComprehensionOrale_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    // NoAction : une conversation purgée au bout d'un an ne doit
                    // pas emporter l'archive.
                    table.ForeignKey(
                        name: "FK_ComprehensionOrale_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_ComprehensionOrale_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensionOrale_conversation_id",
                table: "ComprehensionOrale",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensionOrale_matiere_id",
                table: "ComprehensionOrale",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensionOrale_niveau_scolaire_id",
                table: "ComprehensionOrale",
                column: "niveau_scolaire_id");

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensionOrale_eleve_id_matiere_id_date_creation",
                table: "ComprehensionOrale",
                columns: ["eleve_id", "matiere_id", "date_creation"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ComprehensionOrale");
        }
    }
}
