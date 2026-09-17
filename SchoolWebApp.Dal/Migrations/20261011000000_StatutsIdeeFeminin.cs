using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les statuts d'une idée s'accordent au féminin.
    /// </summary>
    /// <remarks>
    /// Camara, le 17/09/2026 : « dans Implémentée il manque un e, c'est au
    /// féminin ». La règle vaut pour les trois participes — « une idée » est
    /// féminin, donc nouvelle, validée, implémentée. Les quatre autres libellés
    /// sont invariables et ne bougent pas.
    ///
    /// UNE MIGRATION DE DONNÉES, ET PAS SEULEMENT UN RENOMMAGE DANS LE CODE.
    /// Le statut est stocké EN TOUTES LETTRES (voir `IdeeEvolution`), ce qui
    /// rend une ligne lisible en SQL sans table de correspondance — mais ce qui
    /// veut dire aussi qu'un libellé changé dans le code laisse en base des
    /// valeurs que plus personne ne reconnaît : elles perdraient leur couleur,
    /// et toute modification de l'idée serait refusée en « statut inconnu ».
    ///
    /// ÉCRITE À LA MAIN, comme les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261011000000_StatutsIdeeFeminin")]
    public partial class StatutsIdeeFeminin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [IdeeEvolution] SET [statut] = N'Nouvelle' WHERE [statut] = N'Nouveau';");

            migrationBuilder.Sql(
                "UPDATE [IdeeEvolution] SET [statut] = N'Validée' WHERE [statut] = N'Validé';");

            migrationBuilder.Sql(
                "UPDATE [IdeeEvolution] SET [statut] = N'Implémentée' WHERE [statut] = N'Implémenté';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [IdeeEvolution] SET [statut] = N'Nouveau' WHERE [statut] = N'Nouvelle';");

            migrationBuilder.Sql(
                "UPDATE [IdeeEvolution] SET [statut] = N'Validé' WHERE [statut] = N'Validée';");

            migrationBuilder.Sql(
                "UPDATE [IdeeEvolution] SET [statut] = N'Implémenté' WHERE [statut] = N'Implémentée';");
        }
    }
}
