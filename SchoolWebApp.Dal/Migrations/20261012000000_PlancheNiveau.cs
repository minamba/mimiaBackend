using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le niveau scolaire d'une planche.
    /// </summary>
    /// <remarks>
    /// Camara, le 17/09/2026 : un élève de 6e s'est vu proposer la droite
    /// graduée du CP. Le niveau existait — mais SEULEMENT dans l'écran d'import,
    /// comme étiquette d'affichage. Il n'était jamais envoyé au serveur, donc le
    /// professeur ne pouvait pas en tenir compte : il choisissait sur le nom de
    /// la clé et les légendes.
    ///
    /// NULLABLE, ET IL LE RESTERA. Les planches déjà importées n'ont pas de
    /// niveau ; une valeur par défaut inventée serait pire que rien, puisque le
    /// professeur la croirait vraie. Une planche sans niveau est simplement
    /// annoncée sans mention, comme aujourd'hui.
    ///
    /// UNE ÉTIQUETTE, PAS UNE CLÉ ÉTRANGÈRE vers `NiveauScolaire`. Ce champ ne
    /// sert qu'à être LU par le professeur dans son prompt — « (CP) » — et non à
    /// filtrer une requête. Une clé étrangère aurait imposé de faire coïncider
    /// les libellés du catalogue d'import avec les codes de la table des
    /// niveaux, pour un gain nul.
    ///
    /// ÉCRITE À LA MAIN, comme les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261012000000_PlancheNiveau")]
    public partial class PlancheNiveau : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "niveau",
                table: "PlancheSchema",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "niveau", table: "PlancheSchema");
        }
    }
}
