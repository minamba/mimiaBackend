using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La part des jetons écrits en cache qui l'ont été pour UNE HEURE.
    /// </summary>
    /// <remarks>
    /// Voulue par Camara le 19/09/2026 : « j'ai besoin de chiffres exacts car
    /// là les prix me font trop peur, il me faut la vérité ».
    ///
    /// LE TROU QU'ELLE BOUCHE. Anthropic facture l'écriture en cache 1,25x le
    /// prix d'entrée pour cinq minutes et 2x pour une heure. `tokens_cache_ecriture`
    /// ne gardait que le TOTAL : le coût était donc estimé à 1,75x, une moyenne
    /// supposée et jamais mesurée. L'API rend pourtant le détail à chaque tour.
    ///
    /// UNE SEULE COLONNE SUFFIT : la part de cinq minutes est le total moins
    /// celle-ci. Deux colonnes auraient pu se contredire avec le total.
    ///
    /// NULLABLE, ET LE NULL A UN SENS : « tour enregistré avant le 19/09/2026,
    /// détail inconnu ». Ces tours-là restent estimés à 1,75x ; un zéro aurait
    /// affirmé qu'aucun jeton n'était allé dans le cache d'une heure, et les
    /// aurait tous comptés à 1,25x — un coût passé faussement bas.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261018000000_CacheEcritureUneHeure")]
    public partial class CacheEcritureUneHeure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tokens_cache_ecriture_1h",
                table: "Message",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "tokens_cache_ecriture_1h", table: "Message");
        }
    }
}
