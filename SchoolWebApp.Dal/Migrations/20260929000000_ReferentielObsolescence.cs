using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Une compétence peut quitter le programme sans quitter la base — voir
    /// <see cref="Competence.Actif"/> — et une échéance peut être une
    /// sentinelle permanente — voir <see cref="EcheanceReferentiel.Sentinelle"/>.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les neuf précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec. `actif` vaut 1 pour
    /// toutes les lignes existantes : rien n'est obsolète tant que le semeur
    /// ne l'a pas constaté.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260929000000_ReferentielObsolescence")]
    public partial class ReferentielObsolescence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "actif",
                table: "Competence",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_fin_validite",
                table: "Competence",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sentinelle",
                table: "EcheanceReferentiel",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "actif", table: "Competence");
            migrationBuilder.DropColumn(name: "date_fin_validite", table: "Competence");
            migrationBuilder.DropColumn(name: "sentinelle", table: "EcheanceReferentiel");
        }
    }
}
