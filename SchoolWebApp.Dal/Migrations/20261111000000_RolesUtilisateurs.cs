using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES RÔLES DES COMPTES PARENTS (17/10/2026) — Camara : « je veux pouvoir créer
    /// des rôles et les attribuer aux utilisateurs ». Table RoleUtilisateur (nom, code
    /// système, onglets ouverts) et Parent.role_id (null = Utilisateur, mis à null si le
    /// rôle est supprimé). Les rôles système sont semés au démarrage (RolesSeeder).
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261111000000_RolesUtilisateurs")]
    public partial class RolesUtilisateurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role_id",
                table: "Parent",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleUtilisateur",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nom = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    onglets = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUtilisateur", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parent_role_id",
                table: "Parent",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUtilisateur_code",
                table: "RoleUtilisateur",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUtilisateur_nom",
                table: "RoleUtilisateur",
                column: "nom",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Parent_RoleUtilisateur_role_id",
                table: "Parent",
                column: "role_id",
                principalTable: "RoleUtilisateur",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parent_RoleUtilisateur_role_id",
                table: "Parent");

            migrationBuilder.DropTable(
                name: "RoleUtilisateur");

            migrationBuilder.DropIndex(
                name: "IX_Parent_role_id",
                table: "Parent");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "Parent");
        }
    }
}
