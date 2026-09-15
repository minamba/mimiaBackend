using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'élève a-t-il l'espagnol en LV2 ? Un choix de la famille, que la classe
    /// ne dit pas — voulu par Camara le 14/09/2026 : « un simple checkbox permet
    /// de dire que l'enfant a espagnol en LV2 ». Faux par défaut : aucun élève
    /// déjà inscrit ne voit apparaître une matière qu'il n'a pas choisie.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes. Les deux miroirs sont mis à jour avec.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261003000000_Lv2Espagnol")]
    public partial class Lv2Espagnol : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "lv2_espagnol",
                table: "Eleve",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "lv2_espagnol", table: "Eleve");
        }
    }
}
