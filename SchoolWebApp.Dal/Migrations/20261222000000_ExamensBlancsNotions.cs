using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// EXAMENS BLANCS, ÉTAPE 2 — Camara, le 23/11/2026 : « le professeur extrait les notions, les organise en
    /// dossiers en fonction des notions ». Une table des dossiers (une notion, ses documents en liste d'identifiants),
    /// les dates qui disent quand refaire les dossiers, et le compteur de lectures ratées d'un document.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, Designer copié de la précédente et complété, comme le snapshot.</remarks>
    public partial class ExamensBlancsNotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tentatives",
                table: "ExamenBlancDocument",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "documents_modifies_le",
                table: "ExamenBlancMatiere",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "notions_le",
                table: "ExamenBlancMatiere",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExamenBlancNotion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    examen_blanc_matiere_id = table.Column<int>(type: "int", nullable: false),
                    competence_id = table.Column<int>(type: "int", nullable: true),
                    libelle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    document_ids = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamenBlancNotion", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamenBlancNotion_Competence_competence_id",
                        column: x => x.competence_id,
                        principalTable: "Competence",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ExamenBlancNotion_ExamenBlancMatiere_examen_blanc_matiere_id",
                        column: x => x.examen_blanc_matiere_id,
                        principalTable: "ExamenBlancMatiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamenBlancNotion_competence_id",
                table: "ExamenBlancNotion",
                column: "competence_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamenBlancNotion_examen_blanc_matiere_id",
                table: "ExamenBlancNotion",
                column: "examen_blanc_matiere_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExamenBlancNotion");
            migrationBuilder.DropColumn(name: "notions_le", table: "ExamenBlancMatiere");
            migrationBuilder.DropColumn(name: "documents_modifies_le", table: "ExamenBlancMatiere");
            migrationBuilder.DropColumn(name: "tentatives", table: "ExamenBlancDocument");
        }
    }
}
