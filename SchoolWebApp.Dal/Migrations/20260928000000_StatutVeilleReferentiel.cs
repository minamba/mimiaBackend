using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le dernier constat du relevé automatique, en toutes lettres — pour
    /// l'onglet « Programme scolaire » de l'administration, qui affiche ce
    /// statut sans avoir à recomparer les empreintes lui-même.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les huit précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260928000000_StatutVeilleReferentiel")]
    public partial class StatutVeilleReferentiel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dernier_statut_veille",
                table: "EcheanceReferentiel",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "dernier_statut_veille", table: "EcheanceReferentiel");
        }
    }
}
