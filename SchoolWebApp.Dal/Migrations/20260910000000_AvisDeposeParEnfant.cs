using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Distingue, sur chaque avis, si c'est le parent ou un enfant connecté
    /// avec son propre code qui l'a déposé — pour afficher « Parent » ou
    /// « Étudiant » à la lecture. L'avis reste celui du foyer dans les deux
    /// cas ; voir le commentaire sur <see cref="AvisClient.DeposeParEnfant"/>.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260910000000_AvisDeposeParEnfant")]
    public partial class AvisDeposeParEnfant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "depose_par_enfant",
                table: "AvisClient",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "depose_par_enfant", table: "AvisClient");
        }
    }
}
