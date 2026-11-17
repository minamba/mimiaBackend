using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'ABONNEMENT OFFERT PAR L'ADMINISTRATION — Camara, le 13/11/2026 : « je voudrais pouvoir offrir un abonnement,
    /// associer un abonnement à un parent sans le faire payer ». La date où il a été offert et son motif (obligatoire).
    /// Rien n'est rempli pour les abonnements existants : aucun n'a été offert.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes.</remarks>
    public partial class AbonnementOffert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "offert_le",
                table: "Abonnement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motif_offert",
                table: "Abonnement",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "motif_offert", table: "Abonnement");
            migrationBuilder.DropColumn(name: "offert_le", table: "Abonnement");
        }
    }
}
