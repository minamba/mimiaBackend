using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les bornes de niveau d'une matière.
    ///
    /// Jusqu'ici une matière n'avait qu'un booléen `active` : ouverte, ou
    /// fermée, partout pareil. Or « Sciences et technologie » n'existe que
    /// jusqu'à la 6e, et se scinde en SVT et physique-chimie dès la 5e — et le
    /// même besoin reviendra pour la philosophie, la technologie ou la SNT.
    ///
    /// Les valeurs par défaut ouvrent toute la scolarité : une matière déjà en
    /// base garde exactement le comportement qu'elle avait avant cette
    /// migration. C'est le seeder qui pose ensuite les vraies bornes.
    /// </summary>
    public partial class PlageNiveauxMatiere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "niveau_ordre_max",
                table: "Matiere",
                type: "int",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "niveau_ordre_min",
                table: "Matiere",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "niveau_ordre_max",
                table: "Matiere");

            migrationBuilder.DropColumn(
                name: "niveau_ordre_min",
                table: "Matiere");
        }
    }
}
