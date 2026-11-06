using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES JEUX DE TESTS — Camara, le 04/11/2026 : les carnets de test de l'administration,
    /// sur trois étages (le jeu, ses tests, leurs lignes), effacés en cascade.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes : le Designer n'est pas généré.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261126000000_JeuxDeTests")]
    public partial class JeuxDeTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JeuDeTest",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    niveau_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    classe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    auteur = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JeuDeTest", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TestDuJeu",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    jeu_id = table.Column<int>(type: "int", nullable: false),
                    titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    matiere_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    matiere = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestDuJeu", x => x.id);
                    table.ForeignKey(
                        name: "FK_TestDuJeu_JeuDeTest_jeu_id",
                        column: x => x.jeu_id,
                        principalTable: "JeuDeTest",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LigneDeTest",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    test_id = table.Column<int>(type: "int", nullable: false),
                    competence = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_effectuee = table.Column<DateTime>(type: "date", nullable: true),
                    commentaire = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneDeTest", x => x.id);
                    table.ForeignKey(
                        name: "FK_LigneDeTest_TestDuJeu_test_id",
                        column: x => x.test_id,
                        principalTable: "TestDuJeu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestDuJeu_jeu_id",
                table: "TestDuJeu",
                column: "jeu_id");

            migrationBuilder.CreateIndex(
                name: "IX_LigneDeTest_test_id",
                table: "LigneDeTest",
                column: "test_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LigneDeTest");

            migrationBuilder.DropTable(
                name: "TestDuJeu");

            migrationBuilder.DropTable(
                name: "JeuDeTest");
        }
    }
}
