using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAbonnements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offre",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    accroche = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    prix_mensuel_centimes = table.Column<int>(type: "int", nullable: false),
                    prix_annuel_centimes = table.Column<int>(type: "int", nullable: false),
                    nombre_enfants_max = table.Column<int>(type: "int", nullable: false),
                    minutes_pot_mensuel = table.Column<int>(type: "int", nullable: false),
                    minutes_plafond_enfant = table.Column<int>(type: "int", nullable: false),
                    jours_validite = table.Column<int>(type: "int", nullable: false),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false),
                    est_essai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offre", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OffreRecharge",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    minutes = table.Column<int>(type: "int", nullable: false),
                    prix_centimes = table.Column<int>(type: "int", nullable: false),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffreRecharge", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Abonnement",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    offre_id = table.Column<int>(type: "int", nullable: false),
                    date_debut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_fin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    periode_debut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    periode_fin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    derniere_pause = table.Column<DateTime>(type: "datetime2", nullable: true),
                    pause_jusquau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    alerte_quota_envoyee = table.Column<bool>(type: "bit", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abonnement", x => x.id);
                    table.ForeignKey(
                        name: "FK_Abonnement_Offre_offre_id",
                        column: x => x.offre_id,
                        principalTable: "Offre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abonnement_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsommationEleve",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    abonnement_id = table.Column<int>(type: "int", nullable: false),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    periode_debut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    secondes_consommees = table.Column<int>(type: "int", nullable: false),
                    derniere_activite = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsommationEleve", x => x.id);
                    table.ForeignKey(
                        name: "FK_ConsommationEleve_Abonnement_abonnement_id",
                        column: x => x.abonnement_id,
                        principalTable: "Abonnement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsommationEleve_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Recharge",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    abonnement_id = table.Column<int>(type: "int", nullable: false),
                    periode_debut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    minutes = table.Column<int>(type: "int", nullable: false),
                    prix_centimes = table.Column<int>(type: "int", nullable: false),
                    date_achat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recharge", x => x.id);
                    table.ForeignKey(
                        name: "FK_Recharge_Abonnement_abonnement_id",
                        column: x => x.abonnement_id,
                        principalTable: "Abonnement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abonnement_offre_id",
                table: "Abonnement",
                column: "offre_id");

            migrationBuilder.CreateIndex(
                name: "IX_Abonnement_parent_id_statut",
                table: "Abonnement",
                columns: new[] { "parent_id", "statut" });

            migrationBuilder.CreateIndex(
                name: "IX_Abonnement_periode_fin",
                table: "Abonnement",
                column: "periode_fin");

            migrationBuilder.CreateIndex(
                name: "IX_ConsommationEleve_abonnement_id_eleve_id_periode_debut",
                table: "ConsommationEleve",
                columns: new[] { "abonnement_id", "eleve_id", "periode_debut" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsommationEleve_eleve_id",
                table: "ConsommationEleve",
                column: "eleve_id");

            migrationBuilder.CreateIndex(
                name: "IX_Offre_code",
                table: "Offre",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OffreRecharge_code",
                table: "OffreRecharge",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recharge_abonnement_id_periode_debut",
                table: "Recharge",
                columns: new[] { "abonnement_id", "periode_debut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsommationEleve");

            migrationBuilder.DropTable(
                name: "OffreRecharge");

            migrationBuilder.DropTable(
                name: "Recharge");

            migrationBuilder.DropTable(
                name: "Abonnement");

            migrationBuilder.DropTable(
                name: "Offre");
        }
    }
}
