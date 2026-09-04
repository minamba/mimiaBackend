using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La dernière fois que le PARENT est venu sur le site.
    ///
    /// À ne pas confondre avec `derniere_activite`, qui vit sur l'élève et dit
    /// quand l'ENFANT a travaillé pour la dernière fois. Les deux se lisent
    /// ensemble et racontent des choses opposées : un enfant qui travaille tous
    /// les jours pendant que son parent n'ouvre plus rien depuis deux mois, ce
    /// n'est pas un compte en bonne santé — c'est un désabonnement qui se
    /// prépare, et rien dans le tableau de bord ne le montrait.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES PRÉCÉDENTES.
    ///
    /// `dotnet ef migrations add` ne peut pas être utilisé sur ce projet :
    /// plusieurs migrations antérieures sont manuscrites et n'ont pas de
    /// `.Designer.cs`. L'outil ne trouve alors aucun modèle de référence pour
    /// l'état précédent, recule jusqu'au vide, et produit une migration qui
    /// RECRÉE TOUTE LA BASE.
    ///
    /// LES DEUX ATTRIBUTS CI-DESSOUS NE SONT PAS DÉCORATIFS : sans eux, EF ne
    /// reconnaît pas le fichier comme une migration, `GetPendingMigrations`
    /// rend « aucune » sans la moindre erreur, et la colonne n'apparaît jamais.
    /// Le snapshot a été corrigé à la main dans le même geste.
    ///
    /// NULLABLE, ET SANS VALEUR PAR DÉFAUT. Les parents déjà en base n'ont pas
    /// d'historique de connexion : leur mettre la date du jour ferait croire
    /// qu'ils viennent tous de passer, et la colonne mentirait le jour même de
    /// sa création. Un tiret se lit ; une date fausse se croit.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260831120000_DerniereConnexionParent")]
    public partial class DerniereConnexionParent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "derniere_connexion",
                table: "Parent",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "derniere_connexion", table: "Parent");
        }
    }
}
