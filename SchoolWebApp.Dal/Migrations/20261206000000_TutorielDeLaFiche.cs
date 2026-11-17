using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE TUTORIEL DE LA FICHE, UNE FOIS PAR COMPTE PARENT — Camara, le 08/11/2026. Vide tant que le parent ne l'a pas vu.
    ///
    /// LES COMPTES DÉJÀ LÀ SONT MARQUÉS « VU », comme pour le premier tutoriel : il ne doit pas surprendre en plein usage
    /// quelqu'un qui connaît déjà sa fiche. Ils gardent le bouton « Voir le tuto ».
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN : Visual Studio tenait le dossier de sortie.</remarks>
    public partial class TutorielDeLaFiche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "tutoriel_fiche_vu_le",
                table: "Parent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Parent] SET [tutoriel_fiche_vu_le] = SYSUTCDATETIME();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "tutoriel_fiche_vu_le", table: "Parent");
        }
    }
}
