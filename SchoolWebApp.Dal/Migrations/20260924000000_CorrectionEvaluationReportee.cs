using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Une évaluation dont la note est donnée mais dont la correction orale est
    /// remise au cours suivant, faute de temps.
    ///
    /// Deux colonnes : le report lui-même, et le compteur qui garantit qu'on
    /// ne le repropose pas indéfiniment — même mécanique que le bilan d'un
    /// contrôle passé (voir `RelanceBilanControle`).
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les cinq précédentes, et pour la même raison :
    /// `dotnet ef migrations add` recréerait la base entière faute de fichiers
    /// Designer. Les deux miroirs (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260924000000_CorrectionEvaluationReportee")]
    public partial class CorrectionEvaluationReportee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Faux par défaut, et c'est le sens juste pour les notes déjà en
            // base : aucune d'elles n'a été déclarée reportée, et leur inventer
            // une correction en attente ferait ouvrir le prochain cours de
            // chaque enfant sur une copie vieille de plusieurs semaines.
            migrationBuilder.AddColumn<bool>(
                name: "correction_reportee",
                table: "Evaluation",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "relances_correction",
                table: "Evaluation",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "correction_reportee", table: "Evaluation");
            migrationBuilder.DropColumn(name: "relances_correction", table: "Evaluation");
        }
    }
}
