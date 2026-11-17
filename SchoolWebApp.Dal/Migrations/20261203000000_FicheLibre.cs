using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// UNE FICHE LIBRE — Camara, le 06/11/2026 : « le message doit s'adapter selon qu'on est en libre ou pas ». Le
    /// cours garde qu'il a été écrit ou dicté : ses pages, elles, s'effacent une fois les fiches faites. Faux pour
    /// tous les cours existants. Datée après les migrations écrites à la main (jusqu'au 02/12/2026).
    /// </summary>
    public partial class FicheLibre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "libre",
                table: "CoursImporte",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Les fiches libres déjà créées (pages encore là) : une page en texte brut.
            migrationBuilder.Sql(
                "UPDATE c SET libre = 1 FROM CoursImporte c WHERE EXISTS (SELECT 1 FROM CoursImportePage p "
                + "WHERE p.cours_id = c.id AND p.type_mime LIKE 'text/plain%')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "libre", table: "CoursImporte");
        }
    }
}
