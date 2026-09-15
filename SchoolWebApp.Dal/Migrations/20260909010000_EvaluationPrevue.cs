using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Une évaluation proposée par le professeur, reportée « à la prochaine
    /// fois » plutôt que donnée dans l'instant — voir
    /// <see cref="SchoolWebApp.Dal.Entities.EvaluationPrevue"/>.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260909010000_EvaluationPrevue")]
    public partial class AjoutEvaluationPrevue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationPrevue",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    notion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    consommee_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPrevue", x => x.id);

                    table.ForeignKey(
                        name: "FK_EvaluationPrevue_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // Restrict, même raison que sur Dictee : trois chemins de
                    // suppression convergeraient sinon vers cette table
                    // (élève, matière, conversation), et SQL Server refuse
                    // les cascades multiples.
                    table.ForeignKey(
                        name: "FK_EvaluationPrevue_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_EvaluationPrevue_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPrevue_conversation_id",
                table: "EvaluationPrevue",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPrevue_matiere_id",
                table: "EvaluationPrevue",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPrevue_eleve_id_matiere_id_consommee_le",
                table: "EvaluationPrevue",
                columns: ["eleve_id", "matiere_id", "consommee_le"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EvaluationPrevue");
        }
    }
}
