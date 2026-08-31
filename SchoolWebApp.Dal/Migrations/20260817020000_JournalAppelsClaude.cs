using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La table des appels au modèle qui ne sont PAS des tours de dialogue.
    ///
    /// Écrite à la main plutôt que générée : l'API tournait et verrouillait ses
    /// assemblages, ce qui empêche `dotnet ef` de construire le projet. Le
    /// contenu est le même — une table, un index.
    /// </summary>
    /// <remarks>
    /// LES DEUX ATTRIBUTS NE SONT PAS DÉCORATIFS. Ils vivent d'ordinaire dans
    /// le fichier `.Designer.cs` que génère `dotnet ef`. Sans eux, EF ne
    /// reconnaît pas la classe comme une migration : `GetPendingMigrations`
    /// répond « aucune », `Migrate()` ne fait rien, et la table n'existe
    /// jamais — sans la moindre erreur pour le dire.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260817020000_JournalAppelsClaude")]
    public partial class JournalAppelsClaude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppelClaude",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    origine = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    modele = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    tokens_entree = table.Column<int>(type: "int", nullable: false),
                    tokens_sortie = table.Column<int>(type: "int", nullable: false),
                    tokens_cache_lecture = table.Column<int>(type: "int", nullable: false),
                    tokens_cache_ecriture = table.Column<int>(type: "int", nullable: false),
                    reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppelClaude", x => x.id);
                });

            // Les deux seules lectures prévues : « combien sur la période » et
            // « lequel des postes ». On filtre toujours par date d'abord, d'où
            // cet ordre.
            migrationBuilder.CreateIndex(
                name: "IX_AppelClaude_date_creation_origine",
                table: "AppelClaude",
                columns: new[] { "date_creation", "origine" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppelClaude");
        }
    }
}
