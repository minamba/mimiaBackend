using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// ACCEPTER LES CONDITIONS GÉNÉRALES — Camara, le 08/11/2026. Vide pour tous les parents existants : c'est ce qui leur
    /// fait voir la fenêtre à leur prochaine venue.
    /// </summary>
    public partial class ConditionsAcceptees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "conditions_version",
                table: "Parent",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "conditions_acceptees_le",
                table: "Parent",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "conditions_version", table: "Parent");
            migrationBuilder.DropColumn(name: "conditions_acceptees_le", table: "Parent");
        }
    }
}
