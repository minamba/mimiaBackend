using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les venues sur le site public.
    ///
    /// Une table de deux colonnes utiles : l'identifiant que le navigateur
    /// s'est tiré au sort, et l'heure. Elle répond à la seule question que le
    /// tableau de bord ne savait pas poser — combien de gens sont passés,
    /// pas seulement combien se sont inscrits.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES QUATRE PRÉCÉDENTES.
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
    /// pas la table, le prochain démarrage lève `PendingModelChangesWarning`
    /// et l'API refuse de se lancer.
    ///
    /// L'INDEX PORTE SUR LA DATE SEULE. Toutes les lectures sont des tranches
    /// de temps qui dédoublonnent le visiteur À L'INTÉRIEUR de la tranche : on
    /// filtre par date, on ne cherche jamais un visiteur. Ajouter la colonne
    /// `visiteur` à l'index coûterait de l'écriture à chaque venue sans rien
    /// accélérer.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260831000000_VisitesDuSite")]
    public partial class VisitesDuSite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisiteSite",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    visiteur = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    horodatage = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisiteSite", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisiteSite_horodatage",
                table: "VisiteSite",
                column: "horodatage");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VisiteSite");
        }
    }
}
