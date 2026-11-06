using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES SONS QU'ON SAIT JUGER — Camara, le 04/11/2026 : un mot n'est jugé que s'il a été testé et
    /// trouvé fiable. Les résultats du premier banc arrivent par <c>MotsVerifiesSeeder</c>.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes : le Designer n'est pas généré.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261127000000_MotsVerifies")]
    public partial class MotsVerifies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MotVerifie",
                columns: table => new
                {
                    mot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    justes = table.Column<int>(type: "int", nullable: false),
                    total = table.Column<int>(type: "int", nullable: false),
                    date_test = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotVerifie", x => x.mot);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MotVerifie");
        }
    }
}
