using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les besoins particuliers de l'élève — dyslexie, dyscalculie, TDAH —
    /// cochés par le parent (Camara, le 11/10/2026).
    /// </summary>
    /// <remarks>
    /// TROIS COLONNES SUR <c>Eleve</c> :
    ///   - <c>besoins_particuliers</c> : les codes cochés, séparés par « ; » ;
    ///   - <c>besoins_accord_le</c> : la date de la coche, preuve de l'accord du
    ///     parent (sa coche vaut accord, décision de Camara) ;
    ///   - <c>ecran_adapte</c> : l'affichage adapté, allumé par défaut, que le
    ///     parent peut couper.
    ///
    /// Donnée de santé (RGPD, article 9) : rien d'autre n'est gardé — ni
    /// diagnostic, ni document, ni texte libre. L'anonymisation d'un profil
    /// efface les deux premières.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261026000000_BesoinsParticuliers")]
    public partial class BesoinsParticuliers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "besoins_particuliers",
                table: "Eleve",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "besoins_accord_le",
                table: "Eleve",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ecran_adapte",
                table: "Eleve",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ecran_adapte", table: "Eleve");
            migrationBuilder.DropColumn(name: "besoins_accord_le", table: "Eleve");
            migrationBuilder.DropColumn(name: "besoins_particuliers", table: "Eleve");
        }
    }
}
