using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'index qui rend la file d'observation lisible sans balayer la table.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes.
    ///
    /// Le worker cherche les séances inactives dont le marqueur d'observation
    /// est en retard, les plus anciennes d'abord. Aucun index ne portait
    /// `date_dernier_message` en tête — celui qui existe commence par
    /// `eleve_id`, inutilisable ici puisque l'élève n'est pas filtré. Le plan
    /// mesuré était donc un balayage complet SUIVI D'UN TRI, relancé toutes
    /// les dix minutes. À treize conversations c'est gratuit ; à cinquante
    /// mille, c'est un tri de cinquante mille lignes pour en garder vingt.
    ///
    /// `date_derniere_observation` EST EMBARQUÉE, ET CE N'EST PAS UN DÉTAIL.
    ///
    /// Sans elle, l'index ne porte que la date de tête ; le filtre qui compare
    /// les deux dates oblige alors à retourner chercher la seconde dans la
    /// table, POUR CHAQUE LIGNE PARCOURUE — pas seulement pour les vingt
    /// retenues. Or la lecture commence par les conversations les plus
    /// anciennes, qui sont justement celles déjà observées : à grande échelle,
    /// c'est des milliers d'allers-retours avant de trouver vingt lignes
    /// utiles, soit pire que le balayage qu'on voulait supprimer. Mesuré sur
    /// treize conversations : 2 lectures en balayage, 24 avec l'index nu.
    ///
    /// Avec la colonne embarquée, tout le filtre tient dans l'index et la
    /// table n'est jamais rouverte.
    ///
    /// AUCUNE DONNÉE N'EST TOUCHÉE : un index se crée et se supprime sans
    /// rien changer au contenu.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260917000000_IndexFileObservation")]
    public partial class IndexFileObservation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Conversation_date_dernier_message",
                table: "Conversation",
                column: "date_dernier_message")
                .Annotation("SqlServer:Include", new[] { "date_derniere_observation" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversation_date_dernier_message",
                table: "Conversation");
        }
    }
}
