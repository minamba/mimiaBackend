using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le motif d'un ajustement de crédits de fiches fait à la main par
    /// l'administration (Camara, le 16/10/2026), comme pour les heures.
    /// </summary>
    /// <remarks>
    /// Une colonne neuve, <c>motif</c>, nulle pour les achats existants.
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN ; le
    /// snapshot n'a reçu que cette propriété.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261104000000_MotifCreditsFiches")]
    public partial class MotifCreditsFiches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "motif",
                table: "AchatFiches",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "motif",
                table: "AchatFiches");
        }
    }
}
