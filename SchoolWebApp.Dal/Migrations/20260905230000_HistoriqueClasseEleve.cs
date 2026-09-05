using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les années scolaires d'un élève, en intervalles.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add` ne
    /// peut pas servir sur ce projet (migrations manuscrites sans
    /// `.Designer.cs`, l'outil recule jusqu'au vide et recrée toute la base).
    /// Les deux attributs remplacent le fichier absent, et le snapshot est
    /// corrigé dans le même geste.
    ///
    /// À QUOI SERT CETTE TABLE.
    /// La fiche d'un élève se lit par année. Il faut donc savoir, pour chaque
    /// trace de travail, dans quelle classe l'enfant était ce jour-là. Une
    /// colonne sur chaque table aurait imposé une lecture de plus en base à
    /// chaque MESSAGE du chat — plus de mille par matière et par élève, sur le
    /// chemin le plus chaud du produit. Ici, tout se rattache par la date.
    ///
    /// CE QUI N'EST PAS DÉCOUPÉ : LA CONVERSATION.
    /// Le fil d'une matière traverse les années, et c'est ce qui permet au
    /// professeur de dire « cette notion, tu l'as vue l'an dernier ». Seule la
    /// fiche se lit année par année ; l'enseignement, lui, garde tout le
    /// parcours.
    ///
    /// LE REMPLISSAGE INITIAL EST LA MOITIÉ DU TRAVAIL.
    /// Sans lui, les élèves déjà inscrits n'auraient aucune année et leur fiche
    /// serait vide. On ouvre donc pour chacun un intervalle qui part de sa DATE
    /// DE CRÉATION — pas d'aujourd'hui — avec sa classe actuelle.
    ///
    /// C'est une approximation, et il faut la nommer : un élève qui aurait
    /// changé de classe avant cette migration verra tout son travail rattaché à
    /// sa classe d'aujourd'hui. On ne peut pas faire mieux, rien n'enregistrait
    /// les changements. Elle est sans conséquence au lancement, où aucun élève
    /// n'a encore passé une rentrée.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260905230000_HistoriqueClasseEleve")]
    public partial class HistoriqueClasseEleve : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoriqueClasseEleve",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    eleve_id = table.Column<int>(type: "int", nullable: false),
                    niveau_scolaire_id = table.Column<int>(type: "int", nullable: false),
                    debut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fin = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueClasseEleve", x => x.id);

                    table.ForeignKey(
                        name: "FK_HistoriqueClasseEleve_Eleve_eleve_id",
                        column: x => x.eleve_id,
                        principalTable: "Eleve",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_HistoriqueClasseEleve_NiveauScolaire_niveau_scolaire_id",
                        column: x => x.niveau_scolaire_id,
                        principalTable: "NiveauScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueClasseEleve_eleve_id_debut",
                table: "HistoriqueClasseEleve",
                columns: ["eleve_id", "debut"]);

            // Un intervalle ouvert par élève existant, depuis sa création.
            //
            // `date_creation` et non `GETUTCDATE()` : partir d'aujourd'hui
            // laisserait tout son travail passé en dehors de toute année, et sa
            // fiche s'ouvrirait vide sur une classe où il a pourtant travaillé.
            migrationBuilder.Sql(@"
INSERT INTO HistoriqueClasseEleve (eleve_id, niveau_scolaire_id, debut, fin)
SELECT e.id, e.niveau_scolaire_id, e.date_creation, NULL
FROM Eleve e
WHERE e.niveau_scolaire_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM HistoriqueClasseEleve h WHERE h.eleve_id = e.id);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HistoriqueClasseEleve");
        }
    }
}
