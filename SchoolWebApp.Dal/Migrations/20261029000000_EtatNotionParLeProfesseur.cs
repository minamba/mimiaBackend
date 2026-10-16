using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'état que le professeur donne à chaque notion d'un contrôle à la fin
    /// d'une préparation (Camara, le 13/10/2026 : « il faut que le pourcentage
    /// soit en accord avec l'observation du professeur »).
    /// </summary>
    /// <remarks>
    /// DEUX COLONNES SUR <c>ControleNotion</c> : l'état (<c>etat_professeur</c> :
    /// tenue, en-cours, fragile) et sa date (<c>etat_professeur_le</c>). NULL
    /// pour tout ce qui existe : la barre garde alors sa mesure.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261029000000_EtatNotionParLeProfesseur")]
    public partial class EtatNotionParLeProfesseur : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "etat_professeur",
                table: "ControleNotion",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "etat_professeur_le",
                table: "ControleNotion",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "etat_professeur_le", table: "ControleNotion");
            migrationBuilder.DropColumn(name: "etat_professeur", table: "ControleNotion");
        }
    }
}
