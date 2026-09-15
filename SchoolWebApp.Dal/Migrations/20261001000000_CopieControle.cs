using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La copie d'un contrôle passé, regardée avec le professeur : l'élève
    /// dit si l'énoncé et sa copie sont séparés, les envoie, et le professeur
    /// n'analyse rien avant de les avoir tous. Voir <see cref="ControleScolaire.CopieSeparee"/>.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les onze précédentes. Les deux miroirs sont mis à jour avec.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261001000000_CopieControle")]
    public partial class CopieControle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "copie_separee",
                table: "ControleScolaire",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "copie_demandee_le",
                table: "ControleScolaire",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "enonce_piece_jointe_id",
                table: "ControleScolaire",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "copie_piece_jointe_id",
                table: "ControleScolaire",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "copie_analysee_le",
                table: "ControleScolaire",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "copie_separee", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "copie_demandee_le", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "enonce_piece_jointe_id", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "copie_piece_jointe_id", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "copie_analysee_le", table: "ControleScolaire");
        }
    }
}
