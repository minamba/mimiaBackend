using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La table des signalements déposés depuis le bouton « Signaler » — voir
    /// <see cref="SchoolWebApp.Dal.Entities.Signalement"/>.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260914000000_Signalement")]
    public partial class Signalement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Signalement",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    eleve_id = table.Column<int>(type: "int", nullable: true),
                    categorie = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    etat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "nouveau"),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_mise_a_jour = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signalement", x => x.id);

                    table.ForeignKey(
                        name: "FK_Signalement_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // Restrict : Parent -> Eleve cascade déjà vers cette table
                    // via parent_id. Un second chemin en cascade
                    // (Parent -> Eleve -> Signalement) ferait deux chemins de
                    // suppression convergents, que SQL Server refuse.
                    table.ForeignKey(
                        name: "FK_Signalement_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Signalement_etat",
                table: "Signalement",
                column: "etat");

            migrationBuilder.CreateIndex(
                name: "IX_Signalement_parent_id",
                table: "Signalement",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_Signalement_eleve_id",
                table: "Signalement",
                column: "eleve_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Signalement");
        }
    }
}
