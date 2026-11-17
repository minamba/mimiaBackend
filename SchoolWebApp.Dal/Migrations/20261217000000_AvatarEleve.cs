using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// L'AVATAR DE L'ENFANT — Camara, le 16/11/2026 : à la création ou à la modification d'un enfant, le parent (ou
    /// l'administration) peut lui choisir un portrait d'après sa classe, sans obligation. On ne garde qu'un code du
    /// catalogue du front (« cp-ce1-asie-garcon-courts ») ; vide = l'avatar par défaut de sa tranche et de son sexe.
    /// Voir `Domain.Models.AvatarEnfant`.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, comme les précédentes ; Designer = le snapshot à jour.</remarks>
    public partial class AvatarEleve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar",
                table: "Eleve",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar",
                table: "Eleve");
        }
    }
}
