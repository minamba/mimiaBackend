using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES DATES OFFICIELLES DES ÉPREUVES — Camara, le 25/11/2026 : « récupérer les dates des sessions officielles
    /// (bac, brevet…) et un système automatique comme pour les programmes ». Une ligne VALIDEE (ce que voient les
    /// familles) et au plus une ligne PROPOSEE (ce que la veille a lu, en attente de validation) par épreuve.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, Designer copié de la précédente et complété, comme le snapshot.</remarks>
    public partial class DatesEpreuvesOfficielles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DateEpreuveOfficielle",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    epreuve_code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    etat = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    nature = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    date_epreuve = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_fin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    heure_debut = table.Column<TimeSpan>(type: "time", nullable: true),
                    heure_fin = table.Column<TimeSpan>(type: "time", nullable: true),
                    remplacement_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source_libelle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    source_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    annexe_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    extrait = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    propose_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    valide_le = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DateEpreuveOfficielle", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DateEpreuveOfficielle_epreuve_code_etat",
                table: "DateEpreuveOfficielle",
                columns: new[] { "epreuve_code", "etat" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DateEpreuveOfficielle");
        }
    }
}
