using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le périmètre d'un contrôle — sur quelles notions il porte — et les
    /// traces de sa préparation.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    ///
    /// ControleNotion NE PORTE AUCUN SCORE : l'état d'une notion reste mesuré
    /// par MaitriseEleve. Cette table ne dit que « cette notion est au
    /// programme de ce contrôle », plus la date à laquelle elle a été
    /// travaillée en préparation.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260920000000_ControlePreparation")]
    public partial class ControlePreparation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "date_modification",
                table: "ControleScolaire",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "derniere_preparation_le",
                table: "ControleScolaire",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "nombre_preparations",
                table: "ControleScolaire",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ControleNotion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    controle_id = table.Column<int>(type: "int", nullable: false),
                    competence_id = table.Column<int>(type: "int", nullable: true),
                    libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    travaillee_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControleNotion", x => x.id);

                    // Cascade, et c'est le SEUL chemin qui arrive ici : le
                    // périmètre n'a aucune vie hors de son contrôle. Un
                    // contrôle supprimé emporte ses notions, sans quoi elles
                    // resteraient orphelines pour toujours.
                    table.ForeignKey(
                        name: "FK_ControleNotion_ControleScolaire_controle_id",
                        column: x => x.controle_id,
                        principalTable: "ControleScolaire",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    // NoAction : une compétence du référentiel n'est jamais
                    // supprimée, la contrainte n'est là que pour interdire un
                    // identifiant fantaisiste.
                    table.ForeignKey(
                        name: "FK_ControleNotion_Competence_competence_id",
                        column: x => x.competence_id,
                        principalTable: "Competence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControleNotion_controle_id",
                table: "ControleNotion",
                column: "controle_id");

            // FILTRÉ SUR LES LIGNES RATTACHÉES AU RÉFÉRENTIEL : le professeur
            // redéclare volontiers la même notion d'une séance à l'autre, et
            // une notion ne doit compter qu'une fois dans le pourcentage. Les
            // lignes à libellé libre se dédoublonnent en C# sur le libellé
            // normalisé — une contrainte unique sur du texte libre serait
            // ingérable (casse, accents, ponctuation).
            migrationBuilder.CreateIndex(
                name: "UQ_ControleNotion_controle_competence",
                table: "ControleNotion",
                columns: ["controle_id", "competence_id"],
                unique: true,
                filter: "[competence_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ControleNotion_competence_id",
                table: "ControleNotion",
                column: "competence_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ControleNotion");

            migrationBuilder.DropColumn(name: "nombre_preparations", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "derniere_preparation_le", table: "ControleScolaire");
            migrationBuilder.DropColumn(name: "date_modification", table: "ControleScolaire");
        }
    }
}
