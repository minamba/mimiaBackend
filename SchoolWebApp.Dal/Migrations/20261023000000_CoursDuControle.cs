using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le cours de l'élève, importé avec son contrôle.
    /// </summary>
    /// <remarks>
    /// Voulu par Camara le 26/09/2026, sur la remontée d'un parent : pour
    /// préparer un contrôle, le professeur doit avoir en tête la leçon de
    /// l'élève — celle de son professeur de classe — et en suivre la trame,
    /// en complétant avec les notions du référentiel qui y manquent. Sans
    /// cours, rien ne change : la préparation part du référentiel.
    ///
    /// UNE TABLE À PART, ET NON <c>PieceJointe</c> : une pièce jointe appartient
    /// à une conversation, et un contrôle créé depuis le calendrier n'en a
    /// aucune. Le cours doit servir à toutes les préparations, puis à la
    /// correction de la copie.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261023000000_CoursDuControle")]
    public partial class CoursDuControle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControleDocument",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    controle_id = table.Column<int>(type: "int", nullable: false),
                    nom_fichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type_mime = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    taille = table.Column<int>(type: "int", nullable: false),
                    nombre_pages = table.Column<int>(type: "int", nullable: false),
                    donnees = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    transcription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    donnees_effacees_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControleDocument", x => x.id);
                    table.ForeignKey(
                        name: "FK_ControleDocument_ControleScolaire_controle_id",
                        column: x => x.controle_id,
                        principalTable: "ControleScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControleDocument_controle_id",
                table: "ControleDocument",
                column: "controle_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ControleDocument");
        }
    }
}
