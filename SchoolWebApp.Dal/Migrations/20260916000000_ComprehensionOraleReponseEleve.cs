using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La réponse de l'élève, verbatim, séparée de l'évaluation du
    /// professeur.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes. `comprehension` restait la
    /// paraphrase du professeur — utile, mais ne montrait jamais ce que
    /// l'élève avait vraiment dit. Colonne ajoutée `NOT NULL DEFAULT ''` :
    /// la ligne de test déjà archivée reçoit une chaîne vide plutôt qu'un
    /// blocage de migration.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260916000000_ComprehensionOraleReponseEleve")]
    public partial class ComprehensionOraleReponseEleve : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reponse_eleve",
                table: "ComprehensionOrale",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reponse_eleve",
                table: "ComprehensionOrale");
        }
    }
}
