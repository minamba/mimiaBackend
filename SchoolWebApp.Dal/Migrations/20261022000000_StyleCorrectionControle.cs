using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Comment l'élève veut corriger la copie d'un contrôle passé.
    /// </summary>
    /// <remarks>
    /// Voulu par Camara le 26/09/2026, sur la remontée d'un parent : sa fille
    /// voulait se corriger seule, et le professeur lui a donné les réponses.
    /// Une fois la copie reçue, l'écran propose « Je t'aide à te corriger » ou
    /// « Je corrige et t'explique » ; le choix est gardé ici, et rappelé au
    /// professeur à chaque tour de la correction.
    ///
    /// DEUX COLONNES : le choix (<c>aide</c> / <c>explique</c>) et sa date, qui
    /// borne le rappel. NULL pour tous les contrôles d'avant.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261022000000_StyleCorrectionControle")]
    public partial class StyleCorrectionControle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "style_correction",
                table: "ControleScolaire",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "style_correction_le",
                table: "ControleScolaire",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "style_correction_le", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "style_correction", table: "ControleScolaire");
        }
    }
}
