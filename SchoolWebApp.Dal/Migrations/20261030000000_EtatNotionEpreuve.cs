using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'état que le professeur donne aux notions d'une épreuve d'examen à la
    /// fin d'une préparation — le pendant de <c>ControleNotion.etat_professeur</c>
    /// (Camara, le 13/10/2026 : « peut-être qu'on peut l'avoir pour les
    /// préparations aux examens »).
    /// </summary>
    /// <remarks>
    /// UNE TABLE À PART, <c>PreparationEpreuveNotion</c> : le programme d'une
    /// épreuve est une règle, pas une liste en base ; il n'y avait aucune
    /// ligne par notion où poser l'état. Une ligne par (préparation,
    /// compétence), remplacée à chaque verdict ; supprimée avec sa
    /// préparation.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261030000000_EtatNotionEpreuve")]
    public partial class EtatNotionEpreuve : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreparationEpreuveNotion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    preparation_id = table.Column<int>(type: "int", nullable: false),
                    competence_id = table.Column<int>(type: "int", nullable: false),
                    etat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    le = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationEpreuveNotion", x => x.id);
                    table.ForeignKey(
                        name: "FK_PreparationEpreuveNotion_PreparationEpreuve_preparation_id",
                        column: x => x.preparation_id,
                        principalTable: "PreparationEpreuve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreparationEpreuveNotion_Competence_competence_id",
                        column: x => x.competence_id,
                        principalTable: "Competence",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreparationEpreuveNotion_preparation_id_competence_id",
                table: "PreparationEpreuveNotion",
                columns: new[] { "preparation_id", "competence_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationEpreuveNotion_competence_id",
                table: "PreparationEpreuveNotion",
                column: "competence_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PreparationEpreuveNotion");
        }
    }
}
