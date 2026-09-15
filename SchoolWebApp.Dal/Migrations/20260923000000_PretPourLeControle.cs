using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le verdict du professeur sur la préparation : l'élève est-il prêt ?
    ///
    /// Trois colonnes et pas une seule : le verdict sans sa raison serait une
    /// couleur sans explication, et sans sa date on ne pourrait pas dire à
    /// l'enfant QUAND on l'a trouvé prêt — or le verdict ne périme pas, il se
    /// date (voir ControleScolaire.PretLe).
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les quatre précédentes, et pour la même raison :
    /// `dotnet ef migrations add` recréerait la base entière faute de fichiers
    /// Designer. Les deux miroirs (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260923000000_PretPourLeControle")]
    public partial class PretPourLeControle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable, et c'est le cœur du sens : tant que le professeur ne
            // s'est pas prononcé, il n'y a PAS de verdict — et surtout pas un
            // « pas prêt » par défaut écrit en base. L'affichage décidera quoi
            // montrer, la base ne ment pas.
            migrationBuilder.AddColumn<string>(
                name: "pret_verdict",
                table: "ControleScolaire",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pret_observation",
                table: "ControleScolaire",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pret_le",
                table: "ControleScolaire",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "pret_verdict", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "pret_observation", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "pret_le", table: "ControleScolaire");
        }
    }
}
