using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La photo du cahier voyage avec le texte archivé, tant qu'elle n'a pas
    /// été transcrite.
    /// </summary>
    /// <remarks>
    /// Voulu par Camara le 18/09/2026 : « comme ça on perdra rien et le prof
    /// pourra quand même refaire la transcription si elle a pas été faite. »
    ///
    /// LE TROU QU'ELLE BOUCHE. Un texte écrit sur cahier n'existe que dans une
    /// photo, et une photo appartient à la CONVERSATION : ses octets sont
    /// effacés au bout de quelques jours, et la conversation elle-même finit
    /// par être purgée. Si le professeur partait sans recopier la copie — un
    /// départ brutal, une séance qui se termine —, le travail de l'enfant
    /// disparaissait sans trace. Pas « à refaire » : perdu.
    ///
    /// `texte` DEVIENT NULLABLE, ET C'EST LE CŒUR DE LA MIGRATION. Une ligne
    /// peut désormais exister avec sa photo et SANS sa transcription : elle dit
    /// « ce texte attend d'être recopié ». Sans ce null, il aurait fallu
    /// inventer une chaîne vide qui aurait signifié tantôt « pas transcrit »,
    /// tantôt « il n'a rien écrit » — deux choses très différentes.
    ///
    /// LA PURGE SUIT LA RÈGLE DÉJÀ ÉCRITE POUR LES PIÈCES JOINTES, et elle n'est
    /// pas négociable : « effacer les octets d'un document dont le texte n'a pas
    /// encore été extrait le perdrait définitivement — et sans bruit ». Les
    /// octets ne partent donc QUE quand `texte` est renseigné. Conserver
    /// l'écriture manuscrite d'un enfant au-delà de ce qui sert n'a aucune
    /// justification ; la perdre avant de l'avoir lue non plus.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261017000000_ExpressionEcritePhoto")]
    public partial class ExpressionEcritePhoto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Une ligne sans transcription est désormais légitime : elle porte
            // la photo, et attend que le professeur la recopie.
            migrationBuilder.AlterColumn<string>(
                name: "texte",
                table: "ExpressionEcrite",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "photo",
                table: "ExpressionEcrite",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_type_mime",
                table: "ExpressionEcrite",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_effacee_le",
                table: "ExpressionEcrite",
                type: "datetime2",
                nullable: true);

            // L'index de la purge : les photos encore présentes, les plus
            // anciennes d'abord. Même forme que celui des pièces jointes.
            migrationBuilder.CreateIndex(
                name: "IX_ExpressionEcrite_photo_effacee_le_date_creation",
                table: "ExpressionEcrite",
                columns: ["photo_effacee_le", "date_creation"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpressionEcrite_photo_effacee_le_date_creation",
                table: "ExpressionEcrite");

            migrationBuilder.DropColumn(name: "photo_effacee_le", table: "ExpressionEcrite");
            migrationBuilder.DropColumn(name: "photo_type_mime", table: "ExpressionEcrite");
            migrationBuilder.DropColumn(name: "photo", table: "ExpressionEcrite");

            // Le retour arrière ne peut pas inventer les textes manquants : les
            // lignes non transcrites partent, elles n'ont plus de place ici.
            migrationBuilder.Sql("DELETE FROM ExpressionEcrite WHERE texte IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "texte",
                table: "ExpressionEcrite",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
