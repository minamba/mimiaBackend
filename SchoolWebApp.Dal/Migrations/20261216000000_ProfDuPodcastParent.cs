using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES PODCASTS DE L'ESPACE PARENT — Camara, le 15/11/2026 : « dans mon espace parent je veux aussi la
    /// fonctionnalité podcast ». Un podcast d'élève est lu par le professeur de sa matière ; les cours d'un parent sont
    /// rangés dans SES catégories, sans professeur : le parent choisit la voix (décision de Camara, le même jour). Elle
    /// est gardée sur le cours : `prof_avatar`, le même code que `Matiere.prof_avatar`. Vide pour un cours d'élève.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes ; Designer = le snapshot à jour.</remarks>
    public partial class ProfDuPodcastParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prof_avatar",
                table: "CoursImporte",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prof_avatar",
                table: "CoursImporte");
        }
    }
}
