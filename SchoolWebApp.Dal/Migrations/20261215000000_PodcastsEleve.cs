using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES PODCASTS AUDIO DE L'ÉLÈVE — Camara, le 15/11/2026. Ils reprennent le chemin des fiches de révision (cours
    /// importé, lecture des notions, quota, crédits, packs) : les six tables du module portent désormais leur NATURE,
    /// « fiches » pour tout ce qui existe déjà. Le solde de crédits d'une famille devient un solde PAR nature.
    /// Voir `NatureCours.cs` (Dal) et `PodcastsEleve` (Domain).
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes ; Designer = le snapshot à jour.</remarks>
    public partial class PodcastsEleve : Migration
    {
        private static readonly string[] Tables =
            ["CoursImporte", "FicheDeCours", "FicheConsommee", "CreditFichesFamille", "AchatFiches", "OffreFiches"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(
                    name: "nature",
                    table: table,
                    type: "nvarchar(16)",
                    maxLength: 16,
                    nullable: false,
                    defaultValue: "fiches");
            }

            migrationBuilder.DropPrimaryKey(name: "PK_CreditFichesFamille", table: "CreditFichesFamille");
            migrationBuilder.AddPrimaryKey(
                name: "PK_CreditFichesFamille",
                table: "CreditFichesFamille",
                columns: ["parent_id", "nature"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Les lignes de podcasts n'ont pas de place dans l'ancien schéma : elles partent.
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"DELETE FROM [{table}] WHERE [nature] <> 'fiches';");
            }

            migrationBuilder.DropPrimaryKey(name: "PK_CreditFichesFamille", table: "CreditFichesFamille");
            migrationBuilder.AddPrimaryKey(name: "PK_CreditFichesFamille", table: "CreditFichesFamille", column: "parent_id");

            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "nature", table: table);
            }
        }
    }
}
