using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La conseillère d'orientation — Camara, le 11/10/2026 : « accompagner les
    /// élèves dans leur orientation, après la 3e, après la 2de, après le bac »,
    /// en tenant compte de ce que propose VRAIMENT chaque lycée.
    /// </summary>
    /// <remarks>
    /// TROIS CHOSES.
    ///
    /// 1. <c>EtablissementOrientation</c> : l'offre de chaque lycée (code UAI),
    ///    réécrite chaque semaine depuis l'Onisep et l'Annuaire de l'éducation.
    ///
    /// 2. <c>ImportOrientation</c> : le journal de ces imports, lu par
    ///    Admin › Modes.
    ///
    /// 3. Cinq colonnes sur <c>Eleve</c> : l'établissement de l'élève, choisi par
    ///    le parent (UAI, nom, commune, position). Toutes NULL pour les profils
    ///    existants : la conseillère se passe alors de lycée, et le dit.
    ///
    /// La matière ORIENTATION et sa conseillère sont semées au démarrage par
    /// <c>ReferentielSeeder</c>, comme toutes les matières ; elle naît masquée
    /// (réglage <c>MODULE_ORIENTATION</c> absent = éteinte).
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes. Les deux miroirs
    /// (contexte et snapshot) sont mis à jour avec.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261025000000_ConseillereOrientation")]
    public partial class ConseillereOrientation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtablissementOrientation",
                columns: table => new
                {
                    uai = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    libelle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    statut = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    commune = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    code_postal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    departement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    academie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<double>(type: "float", nullable: true),
                    longitude = table.Column<double>(type: "float", nullable: true),
                    site_web = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    voie_generale = table.Column<bool>(type: "bit", nullable: false),
                    voie_technologique = table.Column<bool>(type: "bit", nullable: false),
                    voie_professionnelle = table.Column<bool>(type: "bit", nullable: false),
                    section_europeenne = table.Column<bool>(type: "bit", nullable: false),
                    section_internationale = table.Column<bool>(type: "bit", nullable: false),
                    section_sportive = table.Column<bool>(type: "bit", nullable: false),
                    section_arts = table.Column<bool>(type: "bit", nullable: false),
                    post_bac = table.Column<bool>(type: "bit", nullable: false),
                    apprentissage = table.Column<bool>(type: "bit", nullable: false),
                    specialites = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    options_2de = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    formations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    mis_a_jour_le = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtablissementOrientation", x => x.uai);
                });

            migrationBuilder.CreateTable(
                name: "ImportOrientation",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reussi = table.Column<bool>(type: "bit", nullable: false),
                    message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    lycees = table.Column<int>(type: "int", nullable: false),
                    sources = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    duree_secondes = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportOrientation", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportOrientation_date",
                table: "ImportOrientation",
                column: "date");

            migrationBuilder.AddColumn<string>(
                name: "etablissement_uai",
                table: "Eleve",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "etablissement_libelle",
                table: "Eleve",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "etablissement_commune",
                table: "Eleve",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "etablissement_latitude",
                table: "Eleve",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "etablissement_longitude",
                table: "Eleve",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "etablissement_longitude", table: "Eleve");
            migrationBuilder.DropColumn(name: "etablissement_latitude", table: "Eleve");
            migrationBuilder.DropColumn(name: "etablissement_commune", table: "Eleve");
            migrationBuilder.DropColumn(name: "etablissement_libelle", table: "Eleve");
            migrationBuilder.DropColumn(name: "etablissement_uai", table: "Eleve");
            migrationBuilder.DropTable(name: "ImportOrientation");
            migrationBuilder.DropTable(name: "EtablissementOrientation");
        }
    }
}
