using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Ce qu'il faut à l'écran « Programme scolaire » de l'administration
    /// pour dire, notion par notion, ce qui a été ajouté ou modifié et quand
    /// (<see cref="Competence.DateCreation"/>, <see cref="Competence.DateModification"/>),
    /// et pour ranger chaque échéance sous sa matière et sa classe par des
    /// codes plutôt que par du texte (<see cref="EcheanceReferentiel.MatieresCodes"/>).
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les dix précédentes. Les deux miroirs sont mis à jour avec.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260930000000_ProgrammeScolaireAdmin")]
    public partial class ProgrammeScolaireAdmin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "date_creation",
                table: "Competence",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_modification",
                table: "Competence",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "matieres_codes",
                table: "EcheanceReferentiel",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "niveaux_codes",
                table: "EcheanceReferentiel",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "date_creation", table: "Competence");
            migrationBuilder.DropColumn(name: "date_modification", table: "Competence");
            migrationBuilder.DropColumn(name: "matieres_codes", table: "EcheanceReferentiel");
            migrationBuilder.DropColumn(name: "niveaux_codes", table: "EcheanceReferentiel");
        }
    }
}
