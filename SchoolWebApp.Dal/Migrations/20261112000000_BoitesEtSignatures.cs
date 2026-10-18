using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES BOÎTES MAIL DES ADMINISTRATEURS ET LEURS SIGNATURES (17/10/2026) — Camara :
    /// « que les messages soient envoyés sous son mail LWS », et des modèles de
    /// signature attribués aux comptes. BoiteMailAdmin (une par compte, mot de passe
    /// CHIFFRÉ), SignatureMail (le modèle de base est semé au démarrage) et
    /// Parent.signature_mail_id (null = modèle par défaut).
    /// </summary>
    /// <remarks>GÉNÉRÉE PAR EF DANS UN BROUILLON, PUIS RECOPIÉE ICI À LA MAIN.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261112000000_BoitesEtSignatures")]
    public partial class BoitesEtSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "signature_mail_id",
                table: "Parent",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BoiteMailAdmin",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    adresse = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    mot_de_passe_chiffre = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoiteMailAdmin", x => x.id);
                    table.ForeignKey(
                        name: "FK_BoiteMailAdmin_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignatureMail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nom = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    formule = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ligne_libre = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    afficher_role = table.Column<bool>(type: "bit", nullable: false),
                    afficher_adresse = table.Column<bool>(type: "bit", nullable: false),
                    afficher_site = table.Column<bool>(type: "bit", nullable: false),
                    couleur = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    par_defaut = table.Column<bool>(type: "bit", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureMail", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parent_signature_mail_id",
                table: "Parent",
                column: "signature_mail_id");

            migrationBuilder.CreateIndex(
                name: "IX_BoiteMailAdmin_parent_id",
                table: "BoiteMailAdmin",
                column: "parent_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignatureMail_nom",
                table: "SignatureMail",
                column: "nom",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Parent_SignatureMail_signature_mail_id",
                table: "Parent",
                column: "signature_mail_id",
                principalTable: "SignatureMail",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parent_SignatureMail_signature_mail_id",
                table: "Parent");

            migrationBuilder.DropTable(
                name: "BoiteMailAdmin");

            migrationBuilder.DropTable(
                name: "SignatureMail");

            migrationBuilder.DropIndex(
                name: "IX_Parent_signature_mail_id",
                table: "Parent");

            migrationBuilder.DropColumn(
                name: "signature_mail_id",
                table: "Parent");
        }
    }
}
