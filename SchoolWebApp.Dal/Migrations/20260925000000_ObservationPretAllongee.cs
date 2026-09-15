using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La justification du verdict « prêt » passe de 400 à 1 000 caractères.
    ///
    /// Voulu par Camara le 13/09/2026 : la justification doit passer en revue
    /// TOUTES les notions du programme, puis les difficultés liées rencontrées
    /// pendant la préparation. Une seule notion commentée tenait déjà en 330
    /// caractères ; deux notions et une difficulté dépassent 400. Et le dépôt
    /// tronque sans prévenir : l'enfant aurait lu une justification coupée au
    /// milieu d'un mot, précisément sur la dernière notion — souvent celle qui
    /// explique le verdict.
    /// </summary>
    /// <remarks>
    /// MIGRATION SÉPARÉE, et pas une retouche de `20260923000000_PretPourLeControle` :
    /// celle-ci est déjà appliquée sur la base de développement, et une
    /// migration passée n'est jamais rejouée. ÉCRITE À LA MAIN, comme les
    /// précédentes ; les deux miroirs sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260925000000_ObservationPretAllongee")]
    public partial class ObservationPretAllongee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "pret_observation",
                table: "ControleScolaire",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "pret_observation",
                table: "ControleScolaire",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
