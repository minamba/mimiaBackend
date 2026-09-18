using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les textes écrits par l'élève dans la langue étudiée, et leur correction.
    /// </summary>
    /// <remarks>
    /// Voulu par Camara le 18/09/2026 : « faut archiver comme les autres ».
    ///
    /// LE TROISIÈME DE LA FAMILLE, et les trois ne se confondent pas :
    /// <c>ComprehensionOrale</c> garde ce que l'élève a ENTENDU (avec l'audio),
    /// <c>ExpressionOrale</c> ce qu'il a DIT (sans audio), celle-ci ce qu'il a
    /// ÉCRIT. C'est la seule où son orthographe se voit.
    ///
    /// DEUX COLONNES DE TEXTE ET NON UNE, ET C'EST TOUT L'INTÉRÊT : le texte
    /// brut d'un côté, fautes comprises, la correction de l'autre. Ce qui
    /// apprend, ce n'est ni l'un ni l'autre séparément — c'est de les voir
    /// côte à côte. Même raison que pour la copie d'évaluation que Camara a
    /// fait rendre à l'élève.
    ///
    /// LA CORRECTION EST UNE COLONNE JSON, comme l'échange d'une conversation :
    /// elle s'écrit d'un bloc, se lit d'un bloc, on n'interroge jamais son
    /// intérieur. Ses `type` valent `reussi`, `orthographe`, `grammaire`,
    /// `vocabulaire` ou `construction` — la grille de l'examen.
    ///
    /// UNE SEULE ÉCRITURE PAR LIGNE, jamais de mise à jour : le texte se
    /// corrige dans la séance où il s'écrit. Pas de colonne « etat ».
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261016000000_ExpressionEcrite")]
    public partial class ExpressionEcrite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpressionEcrite",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: true),

                    // Obligatoire, pour la même raison qu'à l'expression orale :
                    // les premiers mots d'un texte d'élève ne distinguent rien.
                    titre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),

                    langue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),

                    // SANS ELLE LE TEXTE NE SE RELIT PAS : on ne saurait plus si
                    // l'élève a répondu à côté, ni pourquoi il s'est arrêté là.
                    consigne = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),

                    // nvarchar(max) des deux côtés : un texte de lycée et ses
                    // reprises dépassent 4000 caractères sans difficulté.
                    texte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    corrections = table.Column<string>(type: "nvarchar(max)", nullable: false),

                    remarque = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_consultation = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpressionEcrite", x => x.id);

                    table.ForeignKey(
                        name: "FK_ExpressionEcrite_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // Restrict, pas Cascade : trois chemins de suppression
                    // convergeraient sinon ici (élève, matière, conversation),
                    // et SQL Server refuse les cascades multiples.
                    table.ForeignKey(
                        name: "FK_ExpressionEcrite_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);

                    // NoAction : une conversation purgée au bout d'un an ne doit
                    // pas emporter l'archive.
                    table.ForeignKey(
                        name: "FK_ExpressionEcrite_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_ExpressionEcrite_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionEcrite_conversation_id",
                table: "ExpressionEcrite",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionEcrite_matiere_id",
                table: "ExpressionEcrite",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressionEcrite_niveau_scolaire_id",
                table: "ExpressionEcrite",
                column: "niveau_scolaire_id");

            // L'index de la liste : les textes d'un élève dans une matière, du
            // plus récent au plus ancien.
            migrationBuilder.CreateIndex(
                name: "IX_ExpressionEcrite_eleve_id_matiere_id_date_creation",
                table: "ExpressionEcrite",
                columns: ["eleve_id", "matiere_id", "date_creation"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExpressionEcrite");
        }
    }
}
