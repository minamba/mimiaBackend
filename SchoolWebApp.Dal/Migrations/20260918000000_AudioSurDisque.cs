using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'audio des compréhensions orales quitte la base pour le disque.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes.
    ///
    /// Mesuré chez le premier élève : `ComprehensionOrale` pesait 9,14 Mo pour
    /// QUINZE lignes — six cents kilo-octets par exercice, en `varbinary(max)`.
    /// À mille élèves faisant deux compréhensions orales par semaine sur une
    /// année scolaire, cela fait une cinquantaine de giga-octets d'audio dans
    /// une base qu'il faut sauvegarder chaque nuit, restaurer et migrer.
    ///
    /// La base ne garde donc plus que le chemin. `audio_donnees` SURVIT sans
    /// être supprimée : les lignes écrites avant ce changement y ont encore
    /// leur son, la lecture y retombe quand `audio_chemin` est nul, et la
    /// purge les videra comme les autres. Supprimer la colonne maintenant
    /// effacerait ces archives-là.
    ///
    /// AUCUNE DONNÉE N'EST TOUCHÉE : une colonne s'ajoute, rien ne se réécrit.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260918000000_AudioSurDisque")]
    public partial class AudioSurDisque : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "audio_chemin",
                table: "ComprehensionOrale",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audio_chemin",
                table: "ComprehensionOrale");
        }
    }
}
