using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les académies, leur zone de vacances scolaires, et le calendrier de
    /// chaque zone — pour le calendrier de l'élève.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    ///
    /// `academie_id` sur Eleve est nullable, et le reste : les profils
    /// existants n'ont pas cette information, rien à approximer — un enfant
    /// sans académie renseignée voit simplement un calendrier sans vacances
    /// affichées, jusqu'à ce qu'un parent la renseigne.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260909000000_AcademieEtVacances")]
    public partial class AcademieEtVacances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Academie",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    zone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Academie", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Academie_code",
                table: "Academie",
                column: "code",
                unique: true);

            migrationBuilder.CreateTable(
                name: "PeriodeVacances",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    zone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    annee_scolaire = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    date_debut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_fin = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodeVacances", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodeVacances_zone_annee_scolaire",
                table: "PeriodeVacances",
                columns: ["zone", "annee_scolaire"]);

            migrationBuilder.AddColumn<int>(
                name: "academie_id",
                table: "Eleve",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Eleve_academie_id",
                table: "Eleve",
                column: "academie_id");

            // Restrict, jamais Cascade : une académie est une donnée de
            // référence, elle ne doit jamais entraîner la suppression d'un
            // enfant.
            migrationBuilder.AddForeignKey(
                name: "FK_Eleve_Academie_academie_id",
                table: "Eleve",
                column: "academie_id",
                principalTable: "Academie",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Eleve_Academie_academie_id",
                table: "Eleve");

            migrationBuilder.DropIndex(
                name: "IX_Eleve_academie_id",
                table: "Eleve");

            migrationBuilder.DropColumn(
                name: "academie_id",
                table: "Eleve");

            migrationBuilder.DropTable(name: "PeriodeVacances");
            migrationBuilder.DropTable(name: "Academie");
        }
    }
}
