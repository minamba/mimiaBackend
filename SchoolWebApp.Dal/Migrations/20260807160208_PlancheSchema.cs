using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class PlancheSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlancheSchema",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    matiere_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    nom_fichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    type_mime = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    taille = table.Column<int>(type: "int", nullable: false),
                    donnees = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    auteur = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    licence = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlancheSchema", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlancheSchema_cle",
                table: "PlancheSchema",
                column: "cle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlancheSchema_matiere_code",
                table: "PlancheSchema",
                column: "matiere_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlancheSchema");
        }
    }
}
