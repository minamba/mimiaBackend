using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// DÉPLACER UNE LIGNE DE TEST — Camara, le 08/11/2026 : « changer la position du test dans la liste et la mémoriser ;
    /// quand on bouge un test, il n'est plus assujetti à sa date de création ». Vide pour toutes les lignes existantes :
    /// elles suivent la date tant que personne n'a déplacé une ligne de leur test.
    /// </summary>
    public partial class PositionDesLignesDeTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "LigneDeTest",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "position", table: "LigneDeTest");
        }
    }
}
