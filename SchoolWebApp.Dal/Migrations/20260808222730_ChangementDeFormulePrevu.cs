using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class ChangementDeFormulePrevu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "changement_prevu_le",
                table: "Abonnement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "offre_prevue_id",
                table: "Abonnement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "periodicite_prevue",
                table: "Abonnement",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Abonnement_offre_prevue_id",
                table: "Abonnement",
                column: "offre_prevue_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Abonnement_Offre_offre_prevue_id",
                table: "Abonnement",
                column: "offre_prevue_id",
                principalTable: "Offre",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abonnement_Offre_offre_prevue_id",
                table: "Abonnement");

            migrationBuilder.DropIndex(
                name: "IX_Abonnement_offre_prevue_id",
                table: "Abonnement");

            migrationBuilder.DropColumn(
                name: "changement_prevu_le",
                table: "Abonnement");

            migrationBuilder.DropColumn(
                name: "offre_prevue_id",
                table: "Abonnement");

            migrationBuilder.DropColumn(
                name: "periodicite_prevue",
                table: "Abonnement");
        }
    }
}
