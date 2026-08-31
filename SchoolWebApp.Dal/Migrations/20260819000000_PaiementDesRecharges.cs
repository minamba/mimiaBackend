using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le paiement des recharges, et leur remboursement.
    ///
    /// Trois colonnes sur `Recharge` : la session de paiement, le paiement
    /// lui-même, et la date de reprise après remboursement.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, ET IL FAUT SAVOIR POURQUOI.
    ///
    /// `dotnet ef migrations add` a produit ici une migration qui RECRÉAIT
    /// TOUTE LA BASE. La cause : la migration précédente
    /// (`20260817020000_JournalAppelsClaude`) est elle aussi manuscrite et n'a
    /// pas de fichier `.Designer.cs`, donc aucun modèle de référence. En
    /// voulant revenir à l'état précédent, l'outil n'a rien trouvé et a reculé
    /// jusqu'au vide ; la migration suivante a donc été calculée contre une
    /// base inexistante.
    ///
    /// Appliquée en production, elle aurait échoué sur la première table déjà
    /// présente — au démarrage de l'API, donc site fermé.
    ///
    /// LES DEUX ATTRIBUTS CI-DESSOUS NE SONT PAS DÉCORATIFS. Ils vivent
    /// d'ordinaire dans le `.Designer.cs`. Sans eux, EF ne reconnaît pas le
    /// fichier comme une migration : `GetPendingMigrations` rend « aucune »,
    /// sans la moindre erreur, et la colonne n'apparaît jamais.
    ///
    /// Le snapshot, lui, est à jour : il a été régénéré par l'outil et décrit
    /// bien ces trois colonnes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260819000000_PaiementDesRecharges")]
    public partial class PaiementDesRecharges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stripe_session_id",
                table: "Recharge",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_paiement_id",
                table: "Recharge",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_remboursement",
                table: "Recharge",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motif",
                table: "Recharge",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // UNIQUE ET FILTRÉ. C'est la base qui interdit le double crédit :
            // deux webhooks rejoués en même temps passeraient tous deux un test
            // applicatif. Filtré, parce que les recharges offertes n'ont pas de
            // session et qu'un index unique ordinaire n'en tolérerait qu'une.
            migrationBuilder.CreateIndex(
                name: "UX_Recharge_stripe_session",
                table: "Recharge",
                column: "stripe_session_id",
                unique: true,
                filter: "[stripe_session_id] IS NOT NULL");

            // Non unique : c'est `date_remboursement` qui garantit qu'on ne
            // reprend les heures qu'une fois.
            migrationBuilder.CreateIndex(
                name: "IX_Recharge_stripe_paiement",
                table: "Recharge",
                column: "stripe_paiement_id",
                filter: "[stripe_paiement_id] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Recharge_stripe_paiement", table: "Recharge");
            migrationBuilder.DropIndex(name: "UX_Recharge_stripe_session", table: "Recharge");
            migrationBuilder.DropColumn(name: "motif", table: "Recharge");
            migrationBuilder.DropColumn(name: "date_remboursement", table: "Recharge");
            migrationBuilder.DropColumn(name: "stripe_paiement_id", table: "Recharge");
            migrationBuilder.DropColumn(name: "stripe_session_id", table: "Recharge");
        }
    }
}
