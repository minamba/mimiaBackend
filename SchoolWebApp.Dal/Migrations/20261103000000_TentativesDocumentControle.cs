using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le compte des lectures en échec d'un document de contrôle (Camara, le
    /// 16/10/2026) : au troisième, le document est déclaré illisible et ses
    /// octets partent, au lieu de rester en base pour toujours.
    /// </summary>
    /// <remarks>
    /// Une colonne neuve, <c>tentatives</c>, à 0 pour les lignes existantes.
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN ; le
    /// snapshot n'a reçu que cette propriété.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261103000000_TentativesDocumentControle")]
    public partial class TentativesDocumentControle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tentatives",
                table: "ControleDocument",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tentatives",
                table: "ControleDocument");
        }
    }
}
