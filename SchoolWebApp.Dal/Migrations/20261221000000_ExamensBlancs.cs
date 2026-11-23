using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES EXAMENS BLANCS — Camara, le 23/11/2026 : « un bouton Brevet blanc / Bac blanc ; l'enfant choisit sa
    /// matière et envoie tous ses documents ; une page comme la préparation aux examens ; les documents s'effacent
    /// deux mois après l'examen blanc ».
    ///
    /// TROIS TABLES : l'examen blanc (une date), ses matières (une fois chacune), leurs documents. AUCUN OCTET EN
    /// BASE : le fichier est sur le disque, la ligne garde son chemin, puis la date de son effacement.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, Designer copié de la précédente et complété, comme le snapshot.</remarks>
    public partial class ExamensBlancs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamenBlanc",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    date_examen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    heure_examen = table.Column<TimeSpan>(type: "time", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamenBlanc", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamenBlanc_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamenBlancMatiere",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    examen_blanc_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamenBlancMatiere", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamenBlancMatiere_ExamenBlanc_examen_blanc_id",
                        column: x => x.examen_blanc_id,
                        principalTable: "ExamenBlanc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamenBlancMatiere_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamenBlancDocument",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    examen_blanc_matiere_id = table.Column<int>(type: "int", nullable: false),
                    nom_fichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type_mime = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    taille = table.Column<long>(type: "bigint", nullable: false),
                    nombre_pages = table.Column<int>(type: "int", nullable: false),
                    chemin = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    fichier_efface_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    transcription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamenBlancDocument", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamenBlancDocument_ExamenBlancMatiere_examen_blanc_matiere_id",
                        column: x => x.examen_blanc_matiere_id,
                        principalTable: "ExamenBlancMatiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamenBlanc_eleve_id_date_examen",
                table: "ExamenBlanc",
                columns: new[] { "eleve_id", "date_examen" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamenBlancMatiere_examen_blanc_id_matiere_id",
                table: "ExamenBlancMatiere",
                columns: new[] { "examen_blanc_id", "matiere_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamenBlancMatiere_matiere_id",
                table: "ExamenBlancMatiere",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamenBlancDocument_examen_blanc_matiere_id",
                table: "ExamenBlancDocument",
                column: "examen_blanc_matiere_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExamenBlancDocument");
            migrationBuilder.DropTable(name: "ExamenBlancMatiere");
            migrationBuilder.DropTable(name: "ExamenBlanc");
        }
    }
}
