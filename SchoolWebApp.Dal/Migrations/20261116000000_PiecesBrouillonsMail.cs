using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les pieces des brouillons de la messagerie, chemins sur le disque (18/10/2026).
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261116000000_PiecesBrouillonsMail")]
    public partial class PiecesBrouillonsMail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PieceBrouillonMail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    brouillon_id = table.Column<int>(type: "int", nullable: false),
                    genre = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    rang = table.Column<int>(type: "int", nullable: false),
                    nom_fichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type_mime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    taille = table.Column<int>(type: "int", nullable: false),
                    chemin = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PieceBrouillonMail", x => x.id);
                    table.ForeignKey(
                        name: "FK_PieceBrouillonMail_BrouillonMail_brouillon_id",
                        column: x => x.brouillon_id,
                        principalTable: "BrouillonMail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PieceBrouillonMail_brouillon_id_genre_rang",
                table: "PieceBrouillonMail",
                columns: new[] { "brouillon_id", "genre", "rang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PieceBrouillonMail");
        }
    }
}
