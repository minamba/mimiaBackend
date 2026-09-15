using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les dictées corrigées, archivées avec le texte dicté et la copie de
    /// l'élève.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    ///
    /// Table indépendante des dictées « en vol » : le mécanisme d'oral
    /// (balises [DICTEE]/[/DICTEE]) reste purement côté client, il ne touche
    /// pas la base. Cette table n'archive que les dictées CORRIGÉES, une
    /// fois la copie reçue et comparée — un événement daté, comme
    /// Evaluation, jamais fusionné entre deux séances comme FicheRevision.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260908000000_AjoutDictees")]
    public partial class AjoutDictees : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dictee",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: true),
                    titre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    texte_dicte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    copie = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    remarque = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_consultation = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dictee", x => x.id);

                    table.ForeignKey(
                        name: "FK_Dictee_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // Restrict, pas Cascade : trois chemins de suppression
                    // convergeraient sinon vers Dictee (élève, matière,
                    // conversation), et SQL Server refuse les cascades multiples.
                    table.ForeignKey(
                        name: "FK_Dictee_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    // NoAction : une conversation purgée au bout d'un an ne doit
                    // pas emporter la dictée archivée.
                    table.ForeignKey(
                        name: "FK_Dictee_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_Dictee_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dictee_conversation_id",
                table: "Dictee",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_Dictee_matiere_id",
                table: "Dictee",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_Dictee_niveau_scolaire_id",
                table: "Dictee",
                column: "niveau_scolaire_id");

            migrationBuilder.CreateIndex(
                name: "IX_Dictee_eleve_id_matiere_id_date_creation",
                table: "Dictee",
                columns: ["eleve_id", "matiere_id", "date_creation"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Dictee");
        }
    }
}
