using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le journal des courriels automatiques et les désabonnements.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, avec ses deux attributs, et le snapshot corrigé dans
    /// le même geste — voir `20260904000000_BandeauxPromo`.
    ///
    /// L'INDEX UNIQUE FILTRÉ SUR (code_modele, cle) EST LA GARDE CONTRE LE
    /// DOUBLON : une ligne est réservée avant chaque envoi, et la base refuse
    /// la seconde pour le même essai ou le même parent. Filtré parce que la
    /// demande d'avis générale n'a pas de clé — sa règle est une fenêtre de
    /// trois mois, lue sur `date_envoi`.
    ///
    /// CASCADE DEPUIS LE PARENT : un compte supprimé n'a plus rien à recevoir,
    /// et son journal n'a aucune raison de lui survivre.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261007000000_EnvoisAutomatiques")]
    public partial class EnvoisAutomatiques : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnvoiAutomatique",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code_modele = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    cle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    occurrence = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_envoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    statut = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvoiAutomatique", x => x.id);
                    table.ForeignKey(
                        name: "FK_EnvoiAutomatique_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DesabonnementMail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    categorie = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesabonnementMail", x => x.id);
                    table.ForeignKey(
                        name: "FK_DesabonnementMail_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvoiAutomatique_code_modele_cle",
                table: "EnvoiAutomatique",
                columns: new[] { "code_modele", "cle" },
                unique: true,
                filter: "[cle] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EnvoiAutomatique_parent_id_code_modele_date_envoi",
                table: "EnvoiAutomatique",
                columns: new[] { "parent_id", "code_modele", "date_envoi" });

            migrationBuilder.CreateIndex(
                name: "IX_DesabonnementMail_parent_id_categorie",
                table: "DesabonnementMail",
                columns: new[] { "parent_id", "categorie" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DesabonnementMail");
            migrationBuilder.DropTable(name: "EnvoiAutomatique");
        }
    }
}
