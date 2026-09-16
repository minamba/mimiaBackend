using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les modèles de courriel : diffusions enregistrées et courriels
    /// automatiques programmés, avec leurs images et documents.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES PRÉCÉDENTES — voir `20260904000000_BandeauxPromo`
    /// pour la raison : `dotnet ef migrations add` recréerait toute la base.
    /// Les deux attributs sont indispensables, et le snapshot a été corrigé à
    /// la main dans le même geste.
    ///
    /// LES COLONNES DE PLANIFICATION SONT LÀ DÈS MAINTENANT, alors que le
    /// premier lot ne livre que les diffusions. Les ajouter au lot suivant
    /// coûterait une seconde migration sur une table déjà remplie, pour des
    /// colonnes qui restent simplement vides en attendant.
    ///
    /// LES OCTETS SONT DANS LA TABLE, comme pour les bandeaux : le disque du
    /// conteneur ne survit pas à un redémarrage, la base est sauvegardée.
    ///
    /// LE CODE EST UNIQUE, MAIS FILTRÉ. Les courriels automatiques en portent
    /// un et il ne doit pas se répéter ; les diffusions n'en ont pas, et SQL
    /// Server refuserait sinon une deuxième ligne à `code` nul.
    ///
    /// CASCADE SUR LES PIÈCES : une image n'a aucune vie hors de son modèle.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261006000000_ModelesMail")]
    public partial class ModelesMail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModeleMail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    nom = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    sujet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    titre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    texte = table.Column<string>(type: "nvarchar(max)", nullable: false),

                    frequence = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    heure_envoi = table.Column<TimeOnly>(type: "time", nullable: true),
                    jour_semaine = table.Column<int>(type: "int", nullable: true),
                    jour_mois = table.Column<int>(type: "int", nullable: true),
                    actif = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    derniere_occurrence = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dernier_envoi_le = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dernier_resultat = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),

                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_modification = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeleMail", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PieceModeleMail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    modele_mail_id = table.Column<int>(type: "int", nullable: false),
                    genre = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    rang = table.Column<int>(type: "int", nullable: false),
                    nom_fichier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    type_mime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    taille = table.Column<int>(type: "int", nullable: false),
                    donnees = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PieceModeleMail", x => x.id);
                    table.ForeignKey(
                        name: "FK_PieceModeleMail_ModeleMail_modele_mail_id",
                        column: x => x.modele_mail_id,
                        principalTable: "ModeleMail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModeleMail_code",
                table: "ModeleMail",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            // La liste des templates se lit par nature, à chaque ouverture.
            migrationBuilder.CreateIndex(
                name: "IX_ModeleMail_nature",
                table: "ModeleMail",
                column: "nature");

            migrationBuilder.CreateIndex(
                name: "IX_PieceModeleMail_modele_mail_id_genre_rang",
                table: "PieceModeleMail",
                columns: new[] { "modele_mail_id", "genre", "rang" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PieceModeleMail");
            migrationBuilder.DropTable(name: "ModeleMail");
        }
    }
}
