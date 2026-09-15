using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La dictée devient une ligne qu'on peut mettre à jour, comme une fiche
    /// de révision, plutôt qu'un événement figé.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes.
    ///
    /// Nouvelle migration plutôt que modification de
    /// `20260908000000_AjoutDictees` : cette dernière est déjà appliquée en
    /// local — l'amender laisserait la base et le fichier se contredire.
    ///
    /// `date_mise_a_jour` : NOT NULL sans valeur par défaut est sûr ici, la
    /// table est vide au moment de cette migration — aucune ligne existante
    /// à satisfaire.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260908010000_AjoutEtatDictee")]
    public partial class AjoutEtatDictee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "etat",
                table: "Dictee",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_mise_a_jour",
                table: "Dictee",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // La liste se trie maintenant par mise à jour, pas par création —
            // même index que FicheRevision, pour la même requête.
            migrationBuilder.CreateIndex(
                name: "IX_Dictee_eleve_id_matiere_id_date_mise_a_jour",
                table: "Dictee",
                columns: new[] { "eleve_id", "matiere_id", "date_mise_a_jour" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dictee_eleve_id_matiere_id_date_mise_a_jour", table: "Dictee");
            migrationBuilder.DropColumn(name: "etat", table: "Dictee");
            migrationBuilder.DropColumn(name: "date_mise_a_jour", table: "Dictee");
        }
    }
}
