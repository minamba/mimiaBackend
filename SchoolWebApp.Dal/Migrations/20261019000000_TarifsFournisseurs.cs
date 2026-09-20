using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La grille tarifaire des fournisseurs d'IA, en base.
    /// </summary>
    /// <remarks>
    /// Voulue par Camara le 19/09/2026 : un onglet « Tarifs » sous « Anthropic /
    /// OpenAI », avec pour chaque prix « Mis à jour le » et sa date. La grille
    /// est tenue à jour par `VeilleTarifsWorker` — la colonne qu'il lui faut
    /// vient avec la migration suivante, `TarifsDerniereVerification`.
    ///
    /// LE MÊME JOUR, LA FACTURE AVAIT DONNÉ TORT AU CODE : le coût affiché
    /// comptait Sonnet 5 à 3 $ / 15 $ depuis le 1er septembre, la console
    /// Anthropic en facturait 2 $ / 10 $. Un tarif écrit dans le code ne se
    /// relit pas ; une grille affichée, datée, et comparée au prix appliqué, si.
    ///
    /// PRÉ-REMPLIE AVEC LES PRIX EN VIGUEUR AU 19/09/2026 : Sonnet 5 vérifié
    /// contre la facture ; Haiku 4.5 et Opus 5 selon la documentation
    /// d'Anthropic ; la voix selon l'estimation d'OpenAI (~0,015 $ la minute) ;
    /// la transcription selon la mesure notée dans `OptionsVoix`. La
    /// transcription n'entre dans aucun calcul : ses prix appliqués restent
    /// vides, et l'écran la dit « non comptée ».
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261019000000_TarifsFournisseurs")]
    public partial class TarifsFournisseurs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TarifFournisseur",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fournisseur = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    modele = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    usage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    prix_entree = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    prix_sortie = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    prix_minute = table.Column<decimal>(type: "decimal(10,5)", precision: 10, scale: 5, nullable: true),
                    prix_entree_applique = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    prix_sortie_applique = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    prix_minute_applique = table.Column<decimal>(type: "decimal(10,5)", precision: 10, scale: 5, nullable: true),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    date_mise_a_jour = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifFournisseur", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TarifFournisseur_fournisseur_modele",
                table: "TarifFournisseur",
                columns: ["fournisseur", "modele"],
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO TarifFournisseur
                    (fournisseur, modele, usage, prix_entree, prix_sortie, prix_minute,
                     prix_entree_applique, prix_sortie_applique, prix_minute_applique,
                     ordre, date_mise_a_jour)
                VALUES
                    (N'anthropic', N'claude-sonnet-5',
                     N'Les professeurs en séance, les observations de séance, les bilans, les planches.',
                     2, 10, NULL, 2, 10, NULL, 1, '2026-09-19'),
                    (N'anthropic', N'claude-haiku-4-5',
                     N'La transcription des documents, le découpage des syllabes, la surveillance du fournisseur.',
                     1, 5, NULL, 1, 5, NULL, 2, '2026-09-19'),
                    (N'anthropic', N'claude-opus-5',
                     N'Les tâches complexes (réglage ModeleComplexe).',
                     5, 25, NULL, 5, 25, NULL, 3, '2026-09-19'),
                    (N'openai', N'gpt-4o-mini-tts',
                     N'La voix des professeurs. Estimée à 0,4 minute de parole par tour.',
                     NULL, NULL, 0.015, NULL, NULL, 0.015, 1, '2026-09-19'),
                    (N'openai', N'gpt-transcribe',
                     N'Le micro de l''élève (transcription). Non compté dans les coûts : un demi-centime par heure.',
                     NULL, NULL, 0.0045, NULL, NULL, NULL, 2, '2026-09-19');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TarifFournisseur");
        }
    }
}
