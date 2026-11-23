using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// EXAMENS BLANCS, ÉTAPE 3 — Camara, le 23/11/2026 : « le professeur propose un programme de révision au tableau ;
    /// si l'enfant est d'accord, on officialise le programme ; une page avec le pourcentage global et par notion ».
    /// Chaque matière d'un examen blanc s'appuie sur un contrôle caché (sa préparation est celle d'un contrôle) :
    /// son identifiant, sans clé étrangère (voir l'entité), et la date de l'accord de l'élève sur le programme.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN, Designer copié de la précédente et complété, comme le snapshot.</remarks>
    public partial class ExamensBlancsSeance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "controle_id",
                table: "ExamenBlancMatiere",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "programme_valide_le",
                table: "ExamenBlancMatiere",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "programme_valide_le", table: "ExamenBlancMatiere");
            migrationBuilder.DropColumn(name: "controle_id", table: "ExamenBlancMatiere");
        }
    }
}
