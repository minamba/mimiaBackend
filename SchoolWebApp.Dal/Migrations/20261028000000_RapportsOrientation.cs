using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les rapports d'orientation : un par séance avec la conseillère, adressé
    /// à l'élève, lu aussi par le parent (Camara, le 11/10/2026).
    /// </summary>
    /// <remarks>
    /// Une table à part, et non les fiches de révision, que les professeurs
    /// lisent pour savoir ce qui a été travaillé. Liée à l'élève, qui l'emporte
    /// avec lui (cascade). <c>vu_le</c> nul = nouveau pour l'élève.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261028000000_RapportsOrientation")]
    public partial class RapportsOrientation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RapportOrientation",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: true),
                    discussion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    pistes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    chemin = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    atouts = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    a_consolider = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    etapes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    vu_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapportOrientation", x => x.id);
                    table.ForeignKey(
                        name: "FK_RapportOrientation_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RapportOrientation_eleve_id_date_creation",
                table: "RapportOrientation",
                columns: new[] { "eleve_id", "date_creation" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RapportOrientation");
        }
    }
}
