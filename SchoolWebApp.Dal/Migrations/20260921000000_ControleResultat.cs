using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Ce qui se passe APRÈS le contrôle : comment il s'est passé, la note
    /// quand elle arrive, et ce que la copie corrigée a montré notion par
    /// notion.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes.
    ///
    /// Pas de colonne « statut » : `bilan_le` en tient lieu. Un contrôle passé
    /// dont cette date est nulle attend son débriefing ; une fois posée, la
    /// question ne se repose plus. Une énumération de statuts aurait dit la
    /// même chose en autorisant des états incohérents.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260921000000_ControleResultat")]
    public partial class ControleResultat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "note",
                table: "ControleScolaire",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ressenti",
                table: "ControleScolaire",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "bilan_le",
                table: "ControleScolaire",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resultat",
                table: "ControleNotion",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "resultat", table: "ControleNotion");
            migrationBuilder.DropColumn(name: "bilan_le", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "ressenti", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "note", table: "ControleScolaire");
        }
    }
}
