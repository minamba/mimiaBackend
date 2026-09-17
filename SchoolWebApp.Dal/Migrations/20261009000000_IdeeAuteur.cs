using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La signature d'une idée : qui l'a notée.
    /// </summary>
    /// <remarks>
    /// UNE SECONDE MIGRATION PLUTÔT QUE DE RETOUCHER LA PREMIÈRE, et c'est une
    /// leçon payée le 17/09/2026. Les deux colonnes avaient d'abord été ajoutées
    /// À L'INTÉRIEUR de `20261008000000_IdeesEvolution`, en la croyant jamais
    /// appliquée. Elle l'était déjà sur la machine de Camara : `Migrate()` lit
    /// `__EFMigrationsHistory` et ne rejoue pas ce qui y figure, donc les tables
    /// sont restées sans ces colonnes — et toute lecture du carnet répondait 500,
    /// « nom de colonne non valide ».
    ///
    /// LA RÈGLE QUI EN DÉCOULE : une migration écrite est une migration figée.
    /// Dès qu'elle a pu tourner QUELQUE PART — même sur un seul poste — la
    /// modifier crée deux bases qui se croient à jour et ne le sont pas de la
    /// même façon. Ce qui manque s'ajoute par une migration de plus.
    ///
    /// NULLABLES : les idées notées avant qu'on signe le carnet n'ont pas
    /// d'auteur, et un jeton peut ne porter aucun nom.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes — `dotnet ef migrations add`
    /// recréerait toute la base. Le snapshot porte déjà ces deux propriétés : il
    /// décrit le modèle final, pas le chemin pour y arriver.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261009000000_IdeeAuteur")]
    public partial class IdeeAuteur : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "auteur_prenom",
                table: "IdeeEvolution",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "auteur_nom",
                table: "IdeeEvolution",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "auteur_nom", table: "IdeeEvolution");
            migrationBuilder.DropColumn(name: "auteur_prenom", table: "IdeeEvolution");
        }
    }
}
