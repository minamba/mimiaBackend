using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les conversations d'expression orale archivées : l'élève a parlé dans
    /// la langue étudiée, le professeur lui a répondu, et on garde l'échange.
    /// </summary>
    /// <remarks>
    /// Voulu par Camara le 18/09/2026. À NE PAS CONFONDRE AVEC LA
    /// COMPRÉHENSION ORALE, qui a sa propre table depuis le 15/09 : là-bas le
    /// professeur lit un passage et l'élève répond à des questions dessus ;
    /// ici, les deux parlent en alternance dans la langue du cours.
    ///
    /// AUCUNE COLONNE D'AUDIO, ET C'EST LE POINT : « sans besoin d'avoir les
    /// audio. On aura juste la discussion affichée comme dans une messagerie
    /// classique. » Un échange de dix tours resynthétisé coûterait dix fois ce
    /// que coûte une compréhension orale, pour une conversation qu'on relit
    /// plutôt qu'on ne réécoute.
    ///
    /// L'ÉCHANGE TIENT DANS UNE COLONNE JSON plutôt que dans une table de
    /// tours : il s'écrit d'un bloc et se lit d'un bloc, on n'interroge jamais
    /// son intérieur. Voir le commentaire de l'entité.
    ///
    /// UNE SEULE ÉCRITURE PAR LIGNE, jamais de mise à jour — comme la
    /// compréhension orale : la conversation se conclut dans la séance, donc
    /// pas de colonne « etat » ni « date_mise_a_jour ».
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261015000000_ExpressionOrale")]
    public partial class ExpressionOrale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpressionOrale",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: true),

                    // OBLIGATOIRE, contrairement au titre d'une compréhension
                    // orale : celle-ci se reconnaît aux premiers mots de son
                    // passage, une conversation non — « Hello! How are you? »
                    // ne distingue rien de rien.
                    titre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),

                    langue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    echange = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    remarque = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_consultation = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpressionOrale", x => x.id);

                    table.ForeignKey(
                        name: "FK_ExpressionOrale_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // Restrict, pas Cascade : trois chemins de suppression
                    // convergeraient sinon vers ExpressionOrale (élève,
                    // matière, conversation), et SQL Server refuse les
                    // cascades multiples. Même raison que pour la
                    // compréhension orale.
                    table.ForeignKey(
                        name: "FK_ExpressionOrale_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    // NoAction : une conversation purgée au bout d'un an ne
                    // doit pas emporter l'archive.
                    table.ForeignKey(
                        name: "FK_ExpressionOrale_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_ExpressionOrale_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionOrale_conversation_id",
                table: "ExpressionOrale",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionOrale_matiere_id",
                table: "ExpressionOrale",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionOrale_niveau_scolaire_id",
                table: "ExpressionOrale",
                column: "niveau_scolaire_id");

            // L'index de la liste : les conversations d'un élève dans une
            // matière, de la plus récente à la plus ancienne.
            migrationBuilder.CreateIndex(
                name: "IX_ExpressionOrale_eleve_id_matiere_id_date_creation",
                table: "ExpressionOrale",
                columns: ["eleve_id", "matiere_id", "date_creation"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExpressionOrale");
        }
    }
}
