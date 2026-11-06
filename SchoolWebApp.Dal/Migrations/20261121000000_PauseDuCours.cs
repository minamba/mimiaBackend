using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE COURS EN PAUSE — Camara, le 03/11/2026. Le serveur doit la connaître pour
    /// ne pas décompter ce temps du forfait, ni conclure une séance en pause.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN : Visual Studio tenait le dossier de sortie.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261121000000_PauseDuCours")]
    public partial class PauseDuCours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "pause_depuis",
                table: "Conversation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pause_secondes",
                table: "Conversation",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pause_depuis",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "pause_secondes",
                table: "Conversation");
        }
    }
}
