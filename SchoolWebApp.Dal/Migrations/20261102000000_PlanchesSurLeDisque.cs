using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les images des planches sortent de la base (Camara, le 15/10/2026 : la
    /// base SQL Server Express plafonne à 10 Go, le disque du VPS en a 400).
    /// </summary>
    /// <remarks>
    /// <c>donnees</c> devient nullable, <c>fichier</c> (chemin relatif au
    /// dossier des planches) et <c>empreinte</c> (SHA-256) s'ajoutent. Rien
    /// n'est déplacé ici : c'est <c>DeplacementPlanchesWorker</c> qui écrit
    /// chaque image sur le disque, la relit, puis la retire de la base — et
    /// seulement quand le dossier monté porte la marque STOCKAGE-MIMIA.
    ///
    /// La colonne <c>donnees</c> reste, vide : elle sera supprimée par une
    /// migration ultérieure, une fois toutes les planches de la production
    /// vérifiées sur le disque.
    ///
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN ; le
    /// snapshot n'a reçu que les trois changements du bloc PlancheSchema.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261102000000_PlanchesSurLeDisque")]
    public partial class PlanchesSurLeDisque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "donnees",
                table: "PlancheSchema",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AddColumn<string>(
                name: "empreinte",
                table: "PlancheSchema",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fichier",
                table: "PlancheSchema",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "empreinte",
                table: "PlancheSchema");

            migrationBuilder.DropColumn(
                name: "fichier",
                table: "PlancheSchema");

            migrationBuilder.AlterColumn<byte[]>(
                name: "donnees",
                table: "PlancheSchema",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);
        }
    }
}
