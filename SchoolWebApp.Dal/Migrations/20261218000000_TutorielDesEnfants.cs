using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE TUTORIEL DE « VOS ENFANTS », UNE FOIS PAR COMPTE PARENT — Camara, le 17/11/2026 : « quand un parent s'inscrit,
    /// il faut un tuto rapide sur la page Mes enfants ». Vide tant que le parent ne l'a pas vu.
    ///
    /// LES COMPTES DÉJÀ LÀ SONT MARQUÉS « VU », comme pour les deux autres tutoriels : seuls les nouveaux inscrits le
    /// voient arriver. Tous gardent le bouton « Voir le tuto ».
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN.</remarks>
    public partial class TutorielDesEnfants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "tutoriel_enfants_vu_le",
                table: "Parent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Parent] SET [tutoriel_enfants_vu_le] = SYSUTCDATETIME();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "tutoriel_enfants_vu_le", table: "Parent");
        }
    }
}
