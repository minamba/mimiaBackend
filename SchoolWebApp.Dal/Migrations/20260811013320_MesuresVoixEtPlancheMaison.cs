using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class MesuresVoixEtPlancheMaison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "maison",
                table: "PlancheSchema",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MesureVoix",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    seance = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    delai_ms = table.Column<int>(type: "int", nullable: false),
                    repli = table.Column<bool>(type: "bit", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesureVoix", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MesureVoix_date_creation",
                table: "MesureVoix",
                column: "date_creation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MesureVoix");

            migrationBuilder.DropColumn(
                name: "maison",
                table: "PlancheSchema");
        }
    }
}
