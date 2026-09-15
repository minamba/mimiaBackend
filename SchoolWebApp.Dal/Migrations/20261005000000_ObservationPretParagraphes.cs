using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La justification du verdict « prêt » n'a plus de longueur maximale, pour
    /// les contrôles comme pour les épreuves d'examen : `nvarchar(1000)` devient
    /// `nvarchar(max)`.
    ///
    /// Voulu par Camara le 14/09/2026. La justification s'écrit désormais en
    /// paragraphes — une phrase d'ensemble, puis un paragraphe par domaine (ou
    /// par notion), séparés par des lignes vides. Mesuré avant ce changement : la
    /// seule justification d'épreuve en base faisait déjà 965 caractères d'un
    /// seul tenant. Une limite relevée à 2 500 a d'abord été écrite, puis retirée
    /// avant d'être appliquée : « si le prof fait un jour une énorme
    /// justification, il ne doit pas être bloqué ». Toute limite finit par couper
    /// en plein mot, et c'est le dernier domaine — souvent celui qui explique le
    /// verdict — qui disparaît.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme `20260925000000_ObservationPretAllongee` qu'elle
    /// prolonge ; les deux miroirs (contexte et snapshot) sont mis à jour avec.
    /// Les dépôts ne tronquent plus rien non plus.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261005000000_ObservationPretParagraphes")]
    public partial class ObservationPretParagraphes : Migration
    {
        private static readonly string[] Tables = { "ControleScolaire", "PreparationEpreuve" };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AlterColumn<string>(
                    name: "pret_observation",
                    table: table,
                    type: "nvarchar(max)",
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(1000)",
                    oldMaxLength: 1000,
                    oldNullable: true);
            }
        }

        /// <remarks>
        /// Revenir à 1 000 caractères échouerait si une justification plus longue
        /// a été enregistrée entre-temps : SQL Server refuse de tronquer. On coupe
        /// donc d'abord ce qui dépasse, explicitement, plutôt que de laisser la
        /// migration de retour planter à mi-chemin.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql(
                    $"UPDATE [{table}] SET [pret_observation] = LEFT([pret_observation], 1000) "
                    + "WHERE LEN([pret_observation]) > 1000;");

                migrationBuilder.AlterColumn<string>(
                    name: "pret_observation",
                    table: table,
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(max)",
                    oldNullable: true);
            }
        }
    }
}
