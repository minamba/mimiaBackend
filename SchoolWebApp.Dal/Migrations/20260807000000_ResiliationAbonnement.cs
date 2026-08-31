using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La date à laquelle le parent a demandé la résiliation.
    ///
    /// Nullable, et c'est tout le mécanisme : l'abonnement continue de courir
    /// jusqu'à la fin de la période déjà payée, la colonne dit seulement qu'il
    /// ne faut pas la renouveler. Annuler la résiliation remet la colonne à
    /// NULL — d'où l'absence de valeur par défaut.
    /// </summary>
    public partial class ResiliationAbonnement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "resiliation_demandee_le",
                table: "Abonnement",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "resiliation_demandee_le",
                table: "Abonnement");
        }
    }
}
