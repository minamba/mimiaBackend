using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le droit d'ajouter un enfant, accordé ou retiré parent par parent.
    /// </summary>
    /// <remarks>
    /// VRAI PAR DÉFAUT, et c'est le point. Le droit d'ajouter un enfant est
    /// l'état NORMAL : on le retire à un compte précis, on ne l'accorde pas un
    /// par un. Avec un défaut à faux, tous les parents déjà inscrits se
    /// seraient retrouvés bloqués du jour au lendemain, sans que personne ait
    /// rien décidé pour eux.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes — `dotnet ef migrations add`
    /// recréerait toute la base. Le snapshot est corrigé dans le même geste.
    ///
    /// LA VALEUR PAR DÉFAUT EST DÉCLARÉE DEUX FOIS : ici et dans le contexte.
    /// Sans la seconde, le modèle et l'instantané divergent et EF refuse de
    /// démarrer sur un « PendingModelChanges » — voir `est_administrateur`.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261010000000_ParentPeutAjouterEnfant")]
    public partial class ParentPeutAjouterEnfant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "peut_ajouter_enfant",
                table: "Parent",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "peut_ajouter_enfant", table: "Parent");
        }
    }
}
