using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les bandeaux promotionnels de la page d'accueil.
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
    /// LES OCTETS SONT DANS LA TABLE, comme pour les planches. Un fichier posé
    /// sur le disque du conteneur ne survit pas à un `docker restart` : la
    /// promotion disparaîtrait au premier redéploiement, en laissant un cadre
    /// vide en haut de la page d'accueil. La base, elle, est sauvegardée.
    ///
    /// L'INDEX PORTE SUR UN BOOLÉEN, ce qui se discute d'ordinaire — un index
    /// à deux valeurs sert rarement. Ici il sert : au plus UNE ligne est vraie,
    /// et c'est la seule qu'on cherche, à chaque affichage de la page
    /// d'accueil, y compris pour les visiteurs anonymes. La distribution est
    /// exactement celle qui rend un index sur booléen utile.
    ///
    /// PAS DE CONTRAINTE D'UNICITÉ SUR `actif`, et c'est un choix. Un index
    /// unique filtré tiendrait la règle « un seul affiché » en base, mais il
    /// ferait ÉCHOUER l'allumage d'un second bandeau au lieu d'éteindre le
    /// premier — l'administrateur recevrait une erreur de base de données là
    /// où il attend un basculement. La règle est tenue par le dépôt, qui
    /// éteint les autres dans la même transaction.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260904000000_BandeauxPromo")]
    public partial class BandeauxPromo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BandeauPromo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    titre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    texte_alternatif = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    lien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    actif = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),

                    image_large = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    type_mime_large = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    taille_large = table.Column<int>(type: "int", nullable: false),

                    // La version téléphone est facultative : sans elle,
                    // l'affichage retombe sur l'image large.
                    image_mobile = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    type_mime_mobile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    taille_mobile = table.Column<int>(type: "int", nullable: false),

                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BandeauPromo", x => x.id);
                });

            // La seule lecture chaude de la table : « lequel est affiché ? ».
            migrationBuilder.CreateIndex(
                name: "IX_BandeauPromo_actif",
                table: "BandeauPromo",
                column: "actif");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BandeauPromo");
        }
    }
}
