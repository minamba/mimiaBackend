using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'ESPACE PARENT ET SES FICHES DE RÉVISION (16/10/2026) — Camara : « le système
    /// des fiches, mais pour les parents qui en ont le droit, pas lié aux enfants,
    /// classé dans leurs propres catégories ».
    /// </summary>
    /// <remarks>
    /// <c>Parent.peut_creer_fiches</c> (le droit, faux par défaut) ; la table
    /// <c>CategorieFichesParent</c> ; un cours et ses fiches appartiennent à un élève
    /// OU à un parent (<c>eleve_id</c> et <c>matiere_id</c> deviennent facultatifs,
    /// <c>parent_id</c> et <c>categorie_id</c> apparaissent, contrainte
    /// <c>CK_CoursImporte_Proprietaire</c>). Liens vers le parent et la catégorie en
    /// NO ACTION : SQL Server refuse deux chemins de cascade.
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261109000000_EspaceParentFiches")]
    public partial class EspaceParentFiches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "peut_creer_fiches",
                table: "Parent",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "matiere_id",
                table: "FicheDeCours",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "eleve_id",
                table: "FicheDeCours",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "FicheDeCours",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "matiere_id",
                table: "CoursImporte",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "eleve_id",
                table: "CoursImporte",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "categorie_id",
                table: "CoursImporte",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "CoursImporte",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategorieFichesParent",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    couleur = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorieFichesParent", x => x.id);
                    table.ForeignKey(
                        name: "FK_CategorieFichesParent_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FicheDeCours_parent_id_date_creation",
                table: "FicheDeCours",
                columns: new[] { "parent_id", "date_creation" });

            migrationBuilder.CreateIndex(
                name: "IX_CoursImporte_categorie_id",
                table: "CoursImporte",
                column: "categorie_id");

            migrationBuilder.CreateIndex(
                name: "IX_CoursImporte_parent_id_date_creation",
                table: "CoursImporte",
                columns: new[] { "parent_id", "date_creation" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CoursImporte_Proprietaire",
                table: "CoursImporte",
                sql: "(eleve_id IS NOT NULL AND parent_id IS NULL) OR (eleve_id IS NULL AND parent_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CategorieFichesParent_parent_id",
                table: "CategorieFichesParent",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "FK_CoursImporte_CategorieFichesParent_categorie_id",
                table: "CoursImporte",
                column: "categorie_id",
                principalTable: "CategorieFichesParent",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_CoursImporte_Parent_parent_id",
                table: "CoursImporte",
                column: "parent_id",
                principalTable: "Parent",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoursImporte_CategorieFichesParent_categorie_id",
                table: "CoursImporte");

            migrationBuilder.DropForeignKey(
                name: "FK_CoursImporte_Parent_parent_id",
                table: "CoursImporte");

            migrationBuilder.DropTable(
                name: "CategorieFichesParent");

            migrationBuilder.DropIndex(
                name: "IX_FicheDeCours_parent_id_date_creation",
                table: "FicheDeCours");

            migrationBuilder.DropIndex(
                name: "IX_CoursImporte_categorie_id",
                table: "CoursImporte");

            migrationBuilder.DropIndex(
                name: "IX_CoursImporte_parent_id_date_creation",
                table: "CoursImporte");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CoursImporte_Proprietaire",
                table: "CoursImporte");

            migrationBuilder.DropColumn(
                name: "peut_creer_fiches",
                table: "Parent");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "FicheDeCours");

            migrationBuilder.DropColumn(
                name: "categorie_id",
                table: "CoursImporte");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "CoursImporte");

            migrationBuilder.AlterColumn<int>(
                name: "matiere_id",
                table: "FicheDeCours",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "eleve_id",
                table: "FicheDeCours",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "matiere_id",
                table: "CoursImporte",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "eleve_id",
                table: "CoursImporte",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
