using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Deux fichiers par planche : la légendée et la muette.
    /// </summary>
    /// <remarks>
    /// Camara, le 17/09/2026. Les planches sont toutes légendées, et c'est ce
    /// qu'il faut pour ENSEIGNER. Pour INTERROGER, il faut la même figure sans
    /// ses mots : une carte de France muette sur laquelle l'enfant montre la
    /// région qu'on lui demande.
    ///
    /// POURQUOI UNE VARIANTE ET NON UNE DEUXIÈME CLÉ
    /// --------------------------------------------
    /// Une clé « hg-france-regions-muette » aurait doublé le catalogue, obligé
    /// le professeur à connaître deux noms pour la même figure, et surtout
    /// laissé la muette ORPHELINE : sans légendes, elle n'a aucune carte de
    /// repères, donc aucun moyen de corriger un clic.
    ///
    /// Rattachée à sa parente, elle hérite de la sienne. Les deux images sont
    /// le même fond au même cadrage — c'est la condition, et c'est aussi la
    /// façon naturelle de les produire : on efface les mots. Les coordonnées
    /// des étiquettes valent donc sur les deux, et l'exercice se corrige tout
    /// seul : « L'ÉLÈVE A MONTRÉ : BRETAGNE. »
    ///
    /// L'INDEX UNIQUE PASSE DE `cle` À `(cle, variante)`. C'est tout ce qui
    /// empêchait deux lignes de coexister. Le reste du code ne voit rien
    /// changer : chaque requête existante est cantonnée à `legende`, sauf
    /// celles qui doivent servir ou lister les deux.
    ///
    /// NON NULLABLE AVEC DÉFAUT `legende` : les lignes déjà en base SONT les
    /// légendées. Aucune reprise de données, aucune ambiguïté.
    ///
    /// ÉCRITE À LA MAIN, comme les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261013000000_PlancheVariante")]
    public partial class PlancheVariante : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "variante",
                table: "PlancheSchema",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "legende");

            // DANS CET ORDRE : la colonne existe avant que l'index la nomme.
            migrationBuilder.DropIndex(
                name: "IX_PlancheSchema_cle",
                table: "PlancheSchema");

            migrationBuilder.CreateIndex(
                name: "IX_PlancheSchema_cle_variante",
                table: "PlancheSchema",
                columns: new[] { "cle", "variante" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // LES MUETTES PARTENT AVANT L'INDEX. Sans ça, recréer un unique sur
            // `cle` seul échouerait sur la première paire encore en base.
            migrationBuilder.Sql(
                "DELETE FROM [PlancheSchema] WHERE [variante] <> N'legende';");

            migrationBuilder.DropIndex(
                name: "IX_PlancheSchema_cle_variante",
                table: "PlancheSchema");

            migrationBuilder.CreateIndex(
                name: "IX_PlancheSchema_cle",
                table: "PlancheSchema",
                column: "cle",
                unique: true);

            migrationBuilder.DropColumn(name: "variante", table: "PlancheSchema");
        }
    }
}
