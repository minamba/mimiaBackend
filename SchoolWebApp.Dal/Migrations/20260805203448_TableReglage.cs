using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class TableReglage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reglage",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    valeur = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reglage", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reglage_cle",
                table: "Reglage",
                column: "cle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reglage");
        }
    }
}
