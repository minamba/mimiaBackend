using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'adresse de la page officielle d'une échéance, quand elle est connue
    /// et vérifiée, et l'empreinte de son dernier relevé — voir
    /// <see cref="EcheanceReferentiel.Url"/>.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les sept précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260927000000_VeilleReferentiel")]
    public partial class VeilleReferentiel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "url",
                table: "EcheanceReferentiel",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dernier_hash_page",
                table: "EcheanceReferentiel",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "derniere_page_verifiee_le",
                table: "EcheanceReferentiel",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "url", table: "EcheanceReferentiel");
            migrationBuilder.DropColumn(name: "dernier_hash_page", table: "EcheanceReferentiel");
            migrationBuilder.DropColumn(name: "derniere_page_verifiee_le", table: "EcheanceReferentiel");
        }
    }
}
