using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE TEXTE DES BILANS HEBDOMADAIRES, GARDÉ (17/10/2026) — Camara : un bouton
    /// « Voir le bilan hebdomadaire » sur la fiche de l'enfant. Une ligne par enfant
    /// et par semaine : la réponse du professeur, relue par l'envoi comme par
    /// l'affichage, pour le même texte sans second appel au modèle.
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261110000000_BilansRediges")]
    public partial class BilansRediges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BilanRedige",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    fin_semaine = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BilanRedige", x => x.id);
                    table.ForeignKey(
                        name: "FK_BilanRedige_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BilanRedige_eleve_id_fin_semaine",
                table: "BilanRedige",
                columns: new[] { "eleve_id", "fin_semaine" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BilanRedige");
        }
    }
}
