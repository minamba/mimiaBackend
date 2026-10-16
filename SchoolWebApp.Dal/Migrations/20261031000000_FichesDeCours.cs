using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les fiches de révision de l'élève, générées depuis son propre cours
    /// (Camara, le 14/10/2026) : le cours importé, ses pages, et ses fiches.
    /// </summary>
    /// <remarks>
    /// TROIS TABLES NEUVES, rien de touché ailleurs. <c>CoursImporte</c> et ses
    /// <c>CoursImportePage</c> (les octets, en base comme les pièces jointes du
    /// chat), <c>FicheDeCours</c> (le contenu en briques JSON). Tout part avec
    /// l'élève (cascade) ; les fiches et les pages partent avec leur cours.
    ///
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN, à la date
    /// qui la range après les migrations manuscrites précédentes. Le snapshot
    /// n'a reçu que les blocs de ces trois tables.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261031000000_FichesDeCours")]
    public partial class FichesDeCours : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoursImporte",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    etat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    remarque = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    notions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    notions_demandees = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    demande_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    tentatives = table.Column<int>(type: "int", nullable: false),
                    envoye_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pages_effacees_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursImporte", x => x.id);
                    table.ForeignKey(
                        name: "FK_CoursImporte_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CoursImporte_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "CoursImportePage",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cours_id = table.Column<int>(type: "int", nullable: false),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    nom_fichier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    type_mime = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    taille = table.Column<int>(type: "int", nullable: false),
                    nombre_pages = table.Column<int>(type: "int", nullable: false),
                    donnees = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursImportePage", x => x.id);
                    table.ForeignKey(
                        name: "FK_CoursImportePage_CoursImporte_cours_id",
                        column: x => x.cours_id,
                        principalTable: "CoursImporte",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FicheDeCours",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cours_id = table.Column<int>(type: "int", nullable: false),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    notion_rang = table.Column<int>(type: "int", nullable: true),
                    notion_total = table.Column<int>(type: "int", nullable: true),
                    contenu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FicheDeCours", x => x.id);
                    table.ForeignKey(
                        name: "FK_FicheDeCours_CoursImporte_cours_id",
                        column: x => x.cours_id,
                        principalTable: "CoursImporte",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoursImporte_eleve_id_date_creation",
                table: "CoursImporte",
                columns: new[] { "eleve_id", "date_creation" });

            migrationBuilder.CreateIndex(
                name: "IX_CoursImporte_etat",
                table: "CoursImporte",
                column: "etat");

            migrationBuilder.CreateIndex(
                name: "IX_CoursImporte_matiere_id",
                table: "CoursImporte",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_CoursImportePage_cours_id",
                table: "CoursImportePage",
                column: "cours_id");

            migrationBuilder.CreateIndex(
                name: "IX_FicheDeCours_cours_id",
                table: "FicheDeCours",
                column: "cours_id");

            migrationBuilder.CreateIndex(
                name: "IX_FicheDeCours_eleve_id_date_creation",
                table: "FicheDeCours",
                columns: new[] { "eleve_id", "date_creation" });
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoursImportePage");

            migrationBuilder.DropTable(
                name: "FicheDeCours");

            migrationBuilder.DropTable(
                name: "CoursImporte");
        }
    }
}
