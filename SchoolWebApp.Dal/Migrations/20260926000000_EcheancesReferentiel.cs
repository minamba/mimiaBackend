using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Une table pour les échéances de référentiel (voir
    /// <see cref="EcheanceReferentiel"/>) et une colonne de validité, encore
    /// vide, sur chaque compétence.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les six précédentes. Les deux miroirs (contexte
    /// et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260926000000_EcheancesReferentiel")]
    public partial class EcheancesReferentiel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "date_debut_validite",
                table: "Competence",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EcheanceReferentiel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    matiere_libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    niveaux_concernes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    date_echeance = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_connue = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    texte_officiel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    derniere_alerte_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    traitee_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcheanceReferentiel", x => x.id);
                });

            // Le worker balaie « en attente, échéance approchant » à chaque
            // passage : cet index sert exactement cette requête.
            migrationBuilder.CreateIndex(
                name: "IX_EcheanceReferentiel_traitee_le_date_echeance",
                table: "EcheanceReferentiel",
                columns: new[] { "traitee_le", "date_echeance" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EcheanceReferentiel");
            migrationBuilder.DropColumn(name: "date_debut_validite", table: "Competence");
        }
    }
}
