using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES PIÈCES JOINTES DES SIGNALEMENTS — Camara, le 03/11/2026. La table ne garde que
    /// le chemin : les fichiers sont sur le disque (aucun document en base).
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN : Visual Studio tenait le dossier de sortie.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261123000000_PiecesSignalement")]
    public partial class PiecesSignalement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignalementPiece",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    signalement_id = table.Column<int>(type: "int", nullable: false),
                    nom = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type_mime = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    taille = table.Column<long>(type: "bigint", nullable: false),
                    est_image = table.Column<bool>(type: "bit", nullable: false),
                    fichier = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ajoute_par = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalementPiece", x => x.id);
                    table.ForeignKey(
                        name: "FK_SignalementPiece_Signalement_signalement_id",
                        column: x => x.signalement_id,
                        principalTable: "Signalement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignalementPiece_signalement_id",
                table: "SignalementPiece",
                column: "signalement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalementPiece");
        }
    }
}
