using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AjoutEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluation",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    notion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    note = table.Column<double>(type: "float", nullable: false),
                    remarque = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    a_revoir = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluation", x => x.id);
                    table.ForeignKey(
                        name: "FK_Evaluation_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Evaluation_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Evaluation_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluation_conversation_id",
                table: "Evaluation",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluation_eleve_id_date_creation",
                table: "Evaluation",
                columns: new[] { "eleve_id", "date_creation" });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluation_matiere_id",
                table: "Evaluation",
                column: "matiere_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Evaluation");
        }
    }
}
