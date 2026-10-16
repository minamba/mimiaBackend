using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le quota de fiches de révision et les crédits de la famille (Camara, le
    /// 15/10/2026) : vingt fiches par mois et par enfant, puis des packs
    /// achetés (10 fiches 5,99 €, 20 fiches 9,99 €) qui n'expirent jamais.
    /// </summary>
    /// <remarks>
    /// QUATRE TABLES NEUVES, rien de touché ailleurs : <c>FicheConsommee</c>
    /// (le registre du quota, sans contenu), <c>CreditFichesFamille</c> (un
    /// solde par famille), <c>AchatFiches</c> (une ligne par achat, session
    /// Stripe unique), <c>OffreFiches</c> (le catalogue, rempli par le semeur).
    ///
    /// GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN, à la date
    /// qui la range après les migrations manuscrites précédentes. Le snapshot
    /// n'a reçu que les blocs de ces quatre tables.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261101000000_CreditsFiches")]
    public partial class CreditsFiches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchatFiches",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    fiches = table.Column<int>(type: "int", nullable: false),
                    prix_centimes = table.Column<int>(type: "int", nullable: false),
                    date_achat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    stripe_session_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    stripe_paiement_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    date_remboursement = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchatFiches", x => x.id);
                    table.ForeignKey(
                        name: "FK_AchatFiches_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditFichesFamille",
                columns: table => new
                {
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    solde = table.Column<int>(type: "int", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditFichesFamille", x => x.parent_id);
                    table.ForeignKey(
                        name: "FK_CreditFichesFamille_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FicheConsommee",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    fiche_id = table.Column<int>(type: "int", nullable: true),
                    imputation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FicheConsommee", x => x.id);
                    table.ForeignKey(
                        name: "FK_FicheConsommee_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OffreFiches",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    fiches = table.Column<int>(type: "int", nullable: false),
                    stripe_prix_id = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    prix_centimes = table.Column<int>(type: "int", nullable: false),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffreFiches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchatFiches_parent_id_date_achat",
                table: "AchatFiches",
                columns: new[] { "parent_id", "date_achat" });

            migrationBuilder.CreateIndex(
                name: "IX_AchatFiches_stripe_paiement",
                table: "AchatFiches",
                column: "stripe_paiement_id",
                filter: "[stripe_paiement_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AchatFiches_stripe_session",
                table: "AchatFiches",
                column: "stripe_session_id",
                unique: true,
                filter: "[stripe_session_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FicheConsommee_eleve_id_date_creation",
                table: "FicheConsommee",
                columns: new[] { "eleve_id", "date_creation" });

            migrationBuilder.CreateIndex(
                name: "IX_OffreFiches_code",
                table: "OffreFiches",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchatFiches");

            migrationBuilder.DropTable(
                name: "CreditFichesFamille");

            migrationBuilder.DropTable(
                name: "FicheConsommee");

            migrationBuilder.DropTable(
                name: "OffreFiches");
        }
    }
}
