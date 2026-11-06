using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE TUTORIEL, UNE FOIS PAR COMPTE PARENT — Camara, le 02/11/2026. Vide tant
    /// que le parent ne l'a pas vu : il se lance à sa première visite sur la page
    /// d'un enfant.
    ///
    /// LES COMPTES DÉJÀ LÀ SONT MARQUÉS « VU » : le tutoriel ne doit pas surprendre
    /// en plein usage quelqu'un qui connaît déjà l'application. Ils gardent le
    /// bouton « Voir le tuto », comme tout le monde.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN : Visual Studio tenait le dossier de sortie.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261120000000_TutorielDuParent")]
    public partial class TutorielDuParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "tutoriel_vu_le",
                table: "Parent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Parent] SET [tutoriel_vu_le] = SYSUTCDATETIME();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tutoriel_vu_le",
                table: "Parent");
        }
    }
}
