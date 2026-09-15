using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES SPÉCIALITÉS DE LA VOIE GÉNÉRALE — voulues par Camara le 14/09/2026 :
    /// « il faut absolument que les terminales que je gère puissent préparer le
    /// bac ».
    ///
    /// Côté élève : les spécialités cochées par la famille (codes séparés par
    /// « ; », vide par défaut — aucun élève déjà inscrit ne voit apparaître une
    /// matière qu'il n'a pas choisie).
    ///
    /// Côté épreuve : deux conditions et deux filtres. Une épreuve de spécialité
    /// ne concerne que l'élève qui l'a (`specialite_requise`) ; l'épreuve
    /// anticipée de mathématiques de l'élève SANS spécialité maths porte sur une
    /// autre partie du programme (`specialite_exclue`, `domaines_inclus`).
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes. Les deux miroirs sont mis à jour avec.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261004000000_SpecialitesGenerales")]
    public partial class SpecialitesGenerales : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "specialites",
                table: "Eleve",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialite_requise",
                table: "EpreuveExamen",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialite_exclue",
                table: "EpreuveExamen",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "domaines_inclus",
                table: "EpreuveExamen",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "domaines_exclus",
                table: "EpreuveExamen",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "domaines_exclus", table: "EpreuveExamen");
            migrationBuilder.DropColumn(name: "domaines_inclus", table: "EpreuveExamen");
            migrationBuilder.DropColumn(name: "specialite_exclue", table: "EpreuveExamen");
            migrationBuilder.DropColumn(name: "specialite_requise", table: "EpreuveExamen");
            migrationBuilder.DropColumn(name: "specialites", table: "Eleve");
        }
    }
}
