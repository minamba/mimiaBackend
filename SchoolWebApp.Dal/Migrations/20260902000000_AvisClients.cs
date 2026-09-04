using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les avis laissés par les familles.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES PRÉCÉDENTES.
    ///
    /// `dotnet ef migrations add` ne peut pas être utilisé sur ce projet :
    /// plusieurs migrations antérieures sont manuscrites et n'ont pas de
    /// `.Designer.cs`. L'outil ne trouve alors aucun modèle de référence pour
    /// l'état précédent, recule jusqu'au vide, et produit une migration qui
    /// RECRÉE TOUTE LA BASE. Appliquée en production, elle échouerait sur la
    /// première table déjà présente — au démarrage de l'API, donc site fermé.
    ///
    /// LES DEUX ATTRIBUTS CI-DESSOUS NE SONT PAS DÉCORATIFS. Ils vivent
    /// d'ordinaire dans le `.Designer.cs`. Sans eux, EF ne reconnaît pas le
    /// fichier comme une migration : `GetPendingMigrations` rend « aucune »,
    /// sans la moindre erreur, et la table n'apparaît jamais.
    ///
    /// LE SNAPSHOT A ÉTÉ CORRIGÉ À LA MAIN dans le même geste. S'il ne décrit
    /// pas la table, le prochain démarrage lève `PendingModelChangesWarning` et
    /// l'API refuse de se lancer.
    ///
    /// L'INDEX SUR `parent_id` EST UNIQUE, et c'est une règle métier posée en
    /// base plutôt qu'en code : un double-clic sur « Envoyer » suffirait sinon
    /// à créer deux avis du même foyer, et la page vitrine afficherait deux fois
    /// la même famille.
    ///
    /// LA SUPPRESSION EST EN CASCADE. Un parent qui efface son compte efface
    /// son avis avec lui : le laisser derrière publierait le témoignage de
    /// quelqu'un qui a demandé à ne plus exister chez nous.
    ///
    /// LA CONTRAINTE SUR LA NOTE EST EN BASE AUSSI. La route la valide déjà,
    /// mais une note à 0 ou à 12 fausserait la moyenne affichée en page
    /// d'accueil, et rien ne la rattraperait.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260902000000_AvisClients")]
    public partial class AvisClients : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvisClient",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<int>(type: "int", nullable: false),
                    titre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    commentaire = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    publie = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisClient", x => x.id);

                    table.ForeignKey(
                        name: "FK_AvisClient_Parent",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    table.CheckConstraint("CK_AvisClient_note", "[note] >= 1 AND [note] <= 5");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisClient_parent_id",
                table: "AvisClient",
                column: "parent_id",
                unique: true);

            // La page d'accueil ne lit QUE les avis publiés, du plus récent au
            // plus ancien. C'est la seule lecture chaude de cette table.
            migrationBuilder.CreateIndex(
                name: "IX_AvisClient_publie_date_creation",
                table: "AvisClient",
                columns: ["publie", "date_creation"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AvisClient");
        }
    }
}
