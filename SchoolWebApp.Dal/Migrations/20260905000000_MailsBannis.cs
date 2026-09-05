using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La liste des adresses bannies.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES PRÉCÉDENTES. `dotnet ef migrations add` ne
    /// peut pas être utilisé sur ce projet : plusieurs migrations antérieures
    /// sont manuscrites et n'ont pas de `.Designer.cs`. L'outil ne trouve alors
    /// aucun modèle de référence, recule jusqu'au vide, et produit une
    /// migration qui RECRÉE TOUTE LA BASE. Les deux attributs ci-dessous
    /// vivent d'ordinaire dans le `.Designer.cs` ; sans eux, EF ne reconnaît
    /// pas le fichier comme une migration et la table n'apparaît jamais. Le
    /// snapshot a été corrigé dans le même geste.
    ///
    /// AUCUNE CLÉ ÉTRANGÈRE VERS `Parent`, ET C'EST TOUT L'INTÉRÊT. Une
    /// référence au compte ferait disparaître le bannissement avec lui — or
    /// c'est exactement à ce moment-là qu'il sert. La table ne connaît que des
    /// adresses, et elle survit à tout.
    ///
    /// L'INDEX EST UNIQUE parce que la question posée est « cette adresse
    /// est-elle bannie ? », pas « combien de fois ». Deux lignes pour la même
    /// adresse feraient un bannissement qu'on lève à moitié — le premier
    /// retrait semblerait réussir, et le refus continuerait.
    ///
    /// C'est aussi lui qui rend la lecture du serveur d'identité gratuite :
    /// une recherche exacte sur un index unique, à chaque tentative de
    /// connexion.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260905000000_MailsBannis")]
    public partial class MailsBannis : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailBanni",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    motif = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    date_creation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    banni_par = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailBanni", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_MailBanni_mail",
                table: "MailBanni",
                column: "mail",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MailBanni");
        }
    }
}
