using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le projet d'orientation de l'élève, noté par la conseillère avec son
    /// accord, et transmis à ses professeurs (Camara, le 11/10/2026).
    /// </summary>
    /// <remarks>
    /// TROIS COLONNES SUR <c>Eleve</c> : le but (<c>projet_objectif</c>), ce
    /// qui compte matière par matière en JSON (<c>projet_matieres</c>) et la
    /// date (<c>projet_le</c>). Un seul projet, le plus récent. Toutes NULL
    /// pour les profils existants. L'anonymisation d'un profil les efface.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261027000000_ProjetOrientation")]
    public partial class ProjetOrientation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "projet_objectif",
                table: "Eleve",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "projet_matieres",
                table: "Eleve",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "projet_le",
                table: "Eleve",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "projet_le", table: "Eleve");
            migrationBuilder.DropColumn(name: "projet_matieres", table: "Eleve");
            migrationBuilder.DropColumn(name: "projet_objectif", table: "Eleve");
        }
    }
}
