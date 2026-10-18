using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les brouillons de la messagerie (BrouillonMail) et le telephone des signatures (17/10/2026).
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261113000000_BrouillonsEtTelephone")]
    public partial class BrouillonsEtTelephone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telephone",
                table: "SignatureMail",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BrouillonMail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    boite = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    message_origine = table.Column<long>(type: "bigint", nullable: true),
                    destinataire = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    sujet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    texte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrouillonMail", x => x.id);
                    table.ForeignKey(
                        name: "FK_BrouillonMail_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrouillonMail_parent_id_boite_date_modification",
                table: "BrouillonMail",
                columns: new[] { "parent_id", "boite", "date_modification" });

            migrationBuilder.CreateIndex(
                name: "IX_BrouillonMail_parent_id_boite_message_origine",
                table: "BrouillonMail",
                columns: new[] { "parent_id", "boite", "message_origine" },
                unique: true,
                filter: "[message_origine] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrouillonMail");

            migrationBuilder.DropColumn(
                name: "telephone",
                table: "SignatureMail");
        }
    }
}
