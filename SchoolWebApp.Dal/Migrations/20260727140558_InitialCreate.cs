using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matiere",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    agent_slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ordre = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matiere", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "NiveauScolaire",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    cycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NiveauScolaire", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Parent",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    identity_user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    mail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parent", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Competence",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    code_eduscol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    domaine = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    libelle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competence", x => x.id);
                    table.ForeignKey(
                        name: "FK_Competence_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Competence_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Eleve",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: false),
                    prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    age = table.Column<int>(type: "int", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    derniere_activite = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eleve", x => x.id);
                    table.ForeignKey(
                        name: "FK_Eleve_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eleve_Parent_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Parent",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencePrerequis",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    competence_id = table.Column<int>(type: "int", nullable: false),
                    prerequis_id = table.Column<int>(type: "int", nullable: false),
                    poids = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencePrerequis", x => x.id);
                    table.ForeignKey(
                        name: "FK_CompetencePrerequis_Competence_competence_id",
                        column: x => x.competence_id,
                        principalTable: "Competence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetencePrerequis_Competence_prerequis_id",
                        column: x => x.prerequis_id,
                        principalTable: "Competence",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Conversation",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    matiere_id = table.Column<int>(type: "int", nullable: false),
                    titre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_dernier_message = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_purge = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversation", x => x.id);
                    table.ForeignKey(
                        name: "FK_Conversation_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Conversation_Matiere_matiere_id",
                        column: x => x.matiere_id,
                        principalTable: "Matiere",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaitriseEleve",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    competence_id = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<double>(type: "float", nullable: false),
                    confiance = table.Column<double>(type: "float", nullable: false),
                    nombre_observations = table.Column<int>(type: "int", nullable: false),
                    derniere_evaluation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    prochaine_revision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaitriseEleve", x => x.id);
                    table.ForeignKey(
                        name: "FK_MaitriseEleve_Competence_competence_id",
                        column: x => x.competence_id,
                        principalTable: "Competence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaitriseEleve_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    contenu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    modele = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    tokens_entree = table.Column<int>(type: "int", nullable: false),
                    tokens_sortie = table.Column<int>(type: "int", nullable: false),
                    tokens_cache_lecture = table.Column<int>(type: "int", nullable: false),
                    tokens_cache_ecriture = table.Column<int>(type: "int", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.id);
                    table.ForeignKey(
                        name: "FK_Message_Conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Competence_code",
                table: "Competence",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Competence_matiere_id_niveau_scolaire_id",
                table: "Competence",
                columns: new[] { "matiere_id", "niveau_scolaire_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Competence_niveau_scolaire_id",
                table: "Competence",
                column: "niveau_scolaire_id");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencePrerequis_competence_id_prerequis_id",
                table: "CompetencePrerequis",
                columns: new[] { "competence_id", "prerequis_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetencePrerequis_prerequis_id",
                table: "CompetencePrerequis",
                column: "prerequis_id");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_date_purge",
                table: "Conversation",
                column: "date_purge");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_eleve_id_date_dernier_message",
                table: "Conversation",
                columns: new[] { "eleve_id", "date_dernier_message" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_matiere_id",
                table: "Conversation",
                column: "matiere_id");

            migrationBuilder.CreateIndex(
                name: "IX_Eleve_niveau_scolaire_id",
                table: "Eleve",
                column: "niveau_scolaire_id");

            migrationBuilder.CreateIndex(
                name: "IX_Eleve_parent_id",
                table: "Eleve",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_MaitriseEleve_competence_id",
                table: "MaitriseEleve",
                column: "competence_id");

            migrationBuilder.CreateIndex(
                name: "IX_MaitriseEleve_eleve_id_competence_id",
                table: "MaitriseEleve",
                columns: new[] { "eleve_id", "competence_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaitriseEleve_prochaine_revision",
                table: "MaitriseEleve",
                column: "prochaine_revision");

            migrationBuilder.CreateIndex(
                name: "IX_Matiere_code",
                table: "Matiere",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Message_conversation_id_date_creation",
                table: "Message",
                columns: new[] { "conversation_id", "date_creation" });

            migrationBuilder.CreateIndex(
                name: "IX_NiveauScolaire_code",
                table: "NiveauScolaire",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Parent_identity_user_id",
                table: "Parent",
                column: "identity_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parent_mail",
                table: "Parent",
                column: "mail",
                unique: true,
                filter: "[mail] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetencePrerequis");

            migrationBuilder.DropTable(
                name: "MaitriseEleve");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "Competence");

            migrationBuilder.DropTable(
                name: "Conversation");

            migrationBuilder.DropTable(
                name: "Eleve");

            migrationBuilder.DropTable(
                name: "Matiere");

            migrationBuilder.DropTable(
                name: "NiveauScolaire");

            migrationBuilder.DropTable(
                name: "Parent");
        }
    }
}
