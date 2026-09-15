using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le mode de la séance sur la conversation (voir `ModesSeance`), et les
    /// trois tables de la préparation aux examens : l'examen versionné, ses
    /// épreuves, et où en est chaque élève sur chacune.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les douze précédentes. Les deux miroirs sont mis à jour avec.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261002000000_ModesSeanceEtExamens")]
    public partial class ModesSeanceEtExamens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mode_seance",
                table: "Conversation",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mode_controle_id",
                table: "Conversation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mode_epreuve_code",
                table: "Conversation",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Examen",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    titre_section = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    session = table.Column<int>(type: "int", nullable: false),
                    niveaux_codes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    source = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    actif = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examen", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Examen_code",
                table: "Examen",
                column: "code",
                unique: true);

            migrationBuilder.CreateTable(
                name: "EpreuveExamen",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    examen_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    matieres_codes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    niveaux_programme_codes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    remarque = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ordre = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpreuveExamen", x => x.id);
                    table.ForeignKey(
                        name: "FK_EpreuveExamen_Examen_examen_id",
                        column: x => x.examen_id,
                        principalTable: "Examen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EpreuveExamen_code",
                table: "EpreuveExamen",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpreuveExamen_examen_id",
                table: "EpreuveExamen",
                column: "examen_id");

            migrationBuilder.CreateTable(
                name: "PreparationEpreuve",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    epreuve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    nombre_preparations = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    derniere_preparation_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pret_verdict = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    pret_observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    pret_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationEpreuve", x => x.id);
                    table.ForeignKey(
                        name: "FK_PreparationEpreuve_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreparationEpreuve_EpreuveExamen_epreuve_id",
                        column: x => x.epreuve_id,
                        principalTable: "EpreuveExamen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreparationEpreuve_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreparationEpreuve_eleve_id_epreuve_id_matiere_id",
                table: "PreparationEpreuve",
                columns: new[] { "eleve_id", "epreuve_id", "matiere_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationEpreuve_epreuve_id",
                table: "PreparationEpreuve",
                column: "epreuve_id");

            migrationBuilder.CreateIndex(
                name: "IX_PreparationEpreuve_matiere_id",
                table: "PreparationEpreuve",
                column: "matiere_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PreparationEpreuve");
            migrationBuilder.DropTable(name: "EpreuveExamen");
            migrationBuilder.DropTable(name: "Examen");

            migrationBuilder.DropColumn(name: "mode_seance", table: "Conversation");
            migrationBuilder.DropColumn(name: "mode_controle_id", table: "Conversation");
            migrationBuilder.DropColumn(name: "mode_epreuve_code", table: "Conversation");
        }
    }
}
