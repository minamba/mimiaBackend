using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES COURRIELS QU'UN RÔLE REÇOIT — Camara, le 06/11/2026 : « dans les droits, j'affecte les mails à un type
    /// d'utilisateur ». Une colonne, vide pour tous les rôles existants : personne ne reçoit rien tant qu'aucune case
    /// n'est cochée. Datée après les migrations écrites à la main (jusqu'au 30/11/2026).
    /// </summary>
    public partial class CourrielsDesRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "courriels",
                table: "RoleUtilisateur",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "courriels",
                table: "RoleUtilisateur");
        }
    }
}
