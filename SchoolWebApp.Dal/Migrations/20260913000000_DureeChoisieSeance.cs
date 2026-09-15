using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La durée choisie par l'élève pour sa séance (15/25/35/45 min), posée
    /// sur <see cref="Conversation"/> à l'accueil et recopiée sur
    /// <see cref="RapportSeance"/> quand le compte rendu de cette séance est
    /// écrit — voir les commentaires sur ces deux propriétés.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260913000000_DureeChoisieSeance")]
    public partial class DureeChoisieSeance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duree_choisie_minutes",
                table: "Conversation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duree_choisie_minutes",
                table: "RapportSeance",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "duree_choisie_minutes", table: "Conversation");
            migrationBuilder.DropColumn(name: "duree_choisie_minutes", table: "RapportSeance");
        }
    }
}
