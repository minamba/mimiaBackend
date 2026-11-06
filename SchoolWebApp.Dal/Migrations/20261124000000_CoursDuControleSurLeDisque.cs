using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE COURS DU CONTRÔLE SUR LE DISQUE, CONSULTABLE PAR L'ÉLÈVE — Camara, le 04/11/2026.
    /// Deux colonnes : le chemin du fichier (null pour les documents d'avant, qui gardent
    /// leurs octets en base jusqu'à leur transcription, et une fois le fichier purgé), et
    /// la date de la purge, une semaine après le contrôle.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme toutes les précédentes ; le snapshot est mis à jour avec.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261124000000_CoursDuControleSurLeDisque")]
    public partial class CoursDuControleSurLeDisque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chemin",
                table: "ControleDocument",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fichier_efface_le",
                table: "ControleDocument",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "chemin", table: "ControleDocument");
            migrationBuilder.DropColumn(name: "fichier_efface_le", table: "ControleDocument");
        }
    }
}
