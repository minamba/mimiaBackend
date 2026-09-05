using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le bandeau promotionnel s'étend-il sur toute la largeur de la page ?
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES PRÉCÉDENTES. `dotnet ef migrations add` ne
    /// peut pas être utilisé sur ce projet : plusieurs migrations antérieures
    /// sont manuscrites et n'ont pas de `.Designer.cs`. L'outil ne trouve alors
    /// aucun modèle de référence, recule jusqu'au vide, et produit une
    /// migration qui RECRÉE TOUTE LA BASE.
    ///
    /// Les deux attributs ci-dessous vivent d'ordinaire dans le `.Designer.cs`.
    /// Sans eux, EF ne reconnaît pas le fichier comme une migration :
    /// `GetPendingMigrations` rend « aucune », sans erreur, et la colonne
    /// n'apparaît jamais. Le snapshot a été corrigé dans le même geste.
    ///
    /// UNE COLONNE ET NON DEUX. La vidéo n'en demande aucune : le type du
    /// média se lit déjà dans `type_mime_large`, qui commence par « video/ »
    /// ou par « image/ ». Ajouter un drapeau `est_video` aurait créé une
    /// seconde source de vérité, qu'un téléversement mal étiqueté aurait fait
    /// diverger de la première.
    ///
    /// DÉFAUT À FAUX, c'est-à-dire le comportement actuel — la bulle centrée
    /// dans la colonne du site. Une colonne ajoutée ne doit jamais changer
    /// l'apparence de ce qui existe déjà.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260904120000_BandeauPleineLargeur")]
    public partial class BandeauPleineLargeur : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "pleine_largeur",
                table: "BandeauPromo",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "pleine_largeur", table: "BandeauPromo");
        }
    }
}
