using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES COMMENTAIRES SUR LES SIGNALEMENTS — Camara, le 03/11/2026. Des notes de
    /// l'administration, modifiables et supprimables, qui partent avec leur signalement.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN : Visual Studio tenait le dossier de sortie.</remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261122000000_CommentairesSignalement")]
    public partial class CommentairesSignalement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignalementCommentaire",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    signalement_id = table.Column<int>(type: "int", nullable: false),
                    auteur_mail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    texte = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_mise_a_jour = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalementCommentaire", x => x.id);
                    table.ForeignKey(
                        name: "FK_SignalementCommentaire_Signalement_signalement_id",
                        column: x => x.signalement_id,
                        principalTable: "Signalement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignalementCommentaire_signalement_id",
                table: "SignalementCommentaire",
                column: "signalement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalementCommentaire");
        }
    }
}
