using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La phrase que chaque matière promet à l'enfant.
    ///
    /// Une colonne sur `Matiere` : « Calculs, problèmes et géométrie ».
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES DEUX PRÉCÉDENTES, ET POUR LA MÊME RAISON.
    ///
    /// `dotnet ef migrations add` ne peut plus être utilisé sur ce projet :
    /// deux migrations antérieures — `20260817020000_JournalAppelsClaude` et
    /// `20260819000000_PaiementDesRecharges` — sont manuscrites et n'ont pas de
    /// `.Designer.cs`. L'outil ne trouve donc aucun modèle de référence pour
    /// l'état précédent, recule jusqu'au vide, et produit une migration qui
    /// RECRÉE TOUTE LA BASE. Appliquée en production, elle échouerait sur la
    /// première table déjà présente — au démarrage de l'API, donc site fermé.
    ///
    /// LES DEUX ATTRIBUTS CI-DESSOUS NE SONT PAS DÉCORATIFS. Ils vivent
    /// d'ordinaire dans le `.Designer.cs`. Sans eux, EF ne reconnaît pas le
    /// fichier comme une migration : `GetPendingMigrations` rend « aucune »,
    /// sans la moindre erreur, et la colonne n'apparaît jamais.
    ///
    /// LE SNAPSHOT A ÉTÉ CORRIGÉ À LA MAIN dans le même geste. S'il ne décrit
    /// pas la colonne, le prochain démarrage lève `PendingModelChangesWarning`
    /// et l'API refuse de se lancer.
    ///
    /// NULLABLE, ET SANS VALEUR PAR DÉFAUT. Les sept matières déjà en base
    /// reçoivent leur phrase du semeur au démarrage suivant, à partir d'une
    /// table qui les connaît une par une. Une valeur par défaut aurait posé la
    /// même phrase sur toutes, ce qui est pire que rien : un blanc se voit, une
    /// phrase fausse se lit.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260824000000_PromesseMatiere")]
    public partial class PromesseMatiere : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "promesse",
                table: "Matiere",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "promesse", table: "Matiere");
        }
    }
}
