using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class SessionEleve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionEleve",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    jeton_hache = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dernier_acces = table.Column<DateTime>(type: "datetime2", nullable: false),
                    appareil = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionEleve", x => x.id);
                    table.ForeignKey(
                        name: "FK_SessionEleve_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionEleve_eleve_id",
                table: "SessionEleve",
                column: "eleve_id");

            migrationBuilder.CreateIndex(
                name: "IX_SessionEleve_jeton_hache",
                table: "SessionEleve",
                column: "jeton_hache",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionEleve");
        }
    }
}
