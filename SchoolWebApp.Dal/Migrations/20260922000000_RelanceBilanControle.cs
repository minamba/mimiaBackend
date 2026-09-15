using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le compteur qui garantit qu'on ne relance pas un enfant indéfiniment
    /// sur un contrôle dont il ne veut plus parler.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes.
    ///
    /// Migration séparée de `20260921000000_ControleResultat` — et pas ajoutée
    /// dedans — parce que celle-ci a pu être appliquée entre-temps sur la base
    /// de développement : une migration déjà passée n'est jamais rejouée, la
    /// colonne n'aurait jamais existé.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260922000000_RelanceBilanControle")]
    public partial class RelanceBilanControle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "relances_bilan",
                table: "ControleScolaire",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "relances_bilan", table: "ControleScolaire");
        }
    }
}
