using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le carnet d'idées d'évolution, avec ses images.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES PRÉCÉDENTES — voir `20260904000000_BandeauxPromo`
    /// pour la raison : `dotnet ef migrations add` recréerait toute la base.
    /// Les deux attributs sont indispensables, et le snapshot a été corrigé à la
    /// main dans le même geste.
    ///
    /// L'URGENCE ET LE STATUT SONT DU TEXTE, PAS DES NOMBRES. Une ligne lue
    /// directement en SQL doit se comprendre sans aller chercher la table de
    /// correspondance dans le code : « Haute » se lit, « 3 » se devine.
    ///
    /// LA DESCRIPTION EST EN `nvarchar(max)` : une idée peut tenir en trois mots
    /// comme en trois paragraphes, et rien ne justifie de choisir un plafond.
    ///
    /// LES OCTETS DES IMAGES SONT DANS LA TABLE, comme pour les bandeaux promo
    /// et les modèles de courriel : le disque du conteneur ne survit pas à un
    /// redémarrage, la base est sauvegardée.
    ///
    /// CASCADE SUR LES PIÈCES : une image n'a aucune vie hors de son idée.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261008000000_IdeesEvolution")]
    public partial class IdeesEvolution : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdeeEvolution",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    titre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    urgence = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeeEvolution", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PieceIdee",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idee_evolution_id = table.Column<int>(type: "int", nullable: false),
                    rang = table.Column<int>(type: "int", nullable: false),
                    nom_fichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type_mime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    taille = table.Column<int>(type: "int", nullable: false),
                    donnees = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PieceIdee", x => x.id);
                    table.ForeignKey(
                        name: "FK_PieceIdee_IdeeEvolution_idee_evolution_id",
                        column: x => x.idee_evolution_id,
                        principalTable: "IdeeEvolution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Le tableau se filtre par statut.
            migrationBuilder.CreateIndex(
                name: "IX_IdeeEvolution_statut",
                table: "IdeeEvolution",
                column: "statut");

            migrationBuilder.CreateIndex(
                name: "IX_PieceIdee_idee_evolution_id_rang",
                table: "PieceIdee",
                columns: new[] { "idee_evolution_id", "rang" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PieceIdee");
            migrationBuilder.DropTable(name: "IdeeEvolution");
        }
    }
}
