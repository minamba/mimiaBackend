using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les contrôles scolaires à venir : posés depuis le calendrier par le
    /// parent ou l'enfant, ou par le professeur quand l'élève l'annonce en
    /// séance.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    ///
    /// conversation_id EST NULLABLE, contrairement à Dictee/ComprehensionOrale/
    /// EvaluationPrevue : un contrôle peut être posé hors séance, depuis le
    /// calendrier, sans qu'aucune conversation n'existe.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260919000000_ControleScolaire")]
    public partial class ControleScolaire : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControleScolaire",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: true),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: true),
                    pose_par = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    sujet = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    date_controle = table.Column<DateTime>(type: "datetime2", nullable: false),
                    heure_controle = table.Column<TimeSpan>(type: "time", nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControleScolaire", x => x.id);

                    table.ForeignKey(
                        name: "FK_ControleScolaire_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // Restrict, pas Cascade : trois chemins de suppression
                    // convergeraient sinon vers ControleScolaire (élève,
                    // matière, conversation), et SQL Server refuse les
                    // cascades multiples.
                    table.ForeignKey(
                        name: "FK_ControleScolaire_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    // NoAction, nullable : une conversation purgée au bout
                    // d'un an ne doit pas emporter le contrôle, et un
                    // contrôle posé depuis le calendrier n'en a aucune.
                    table.ForeignKey(
                        name: "FK_ControleScolaire_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_ControleScolaire_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControleScolaire_conversation_id",
                table: "ControleScolaire",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ControleScolaire_matiere_id",
                table: "ControleScolaire",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_ControleScolaire_niveau_scolaire_id",
                table: "ControleScolaire",
                column: "niveau_scolaire_id");

            // Sert la requête « prochain contrôle non dépassé pour cet élève
            // et cette matière », appelée à chaque arrivée en séance.
            migrationBuilder.CreateIndex(
                name: "IX_ControleScolaire_eleve_id_matiere_id_date_controle",
                table: "ControleScolaire",
                columns: ["eleve_id", "matiere_id", "date_controle"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ControleScolaire");
        }
    }
}
