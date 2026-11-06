using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'ANGLAIS AU PRIMAIRE — Camara, le 05/11/2026 : « une liste fermée de tous les mots du CP au
    /// CM2 ». « chat » ou « pain » existent dans les deux langues, avec des sons différents : la clé
    /// devient le mot ET sa langue. Tout ce qui existe déjà est du français.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes : le Designer n'est pas généré.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261128000000_LangueDesMotsVerifies")]
    public partial class LangueDesMotsVerifies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "langue",
                table: "MotVerifie",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "fr");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MotVerifie",
                table: "MotVerifie");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MotVerifie",
                table: "MotVerifie",
                columns: new[] { "mot", "langue" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MotVerifie WHERE langue <> 'fr';");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MotVerifie",
                table: "MotVerifie");

            migrationBuilder.DropColumn(
                name: "langue",
                table: "MotVerifie");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MotVerifie",
                table: "MotVerifie",
                column: "mot");
        }
    }
}
