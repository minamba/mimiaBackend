using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LE TYPE D'UN DOCUMENT WORD NE TENAIT PAS DANS SA COLONNE — signalement du 17/11/2026 (« échec de
    /// l'importation de fichiers Word pour la préparation d'un contrôle »). `ControleDocument.type_mime` faisait
    /// 60 caractères, fixés quand le cours n'acceptait que photos et PDF. Word en compte 71
    /// (`application/vnd.openxmlformats-officedocument.wordprocessingml.document`), PowerPoint 73, Excel 65 :
    /// l'ajout en base échouait (« String or binary data would be truncated »), l'API répondait 500 et l'élève
    /// lisait « Le document n'a pas pu être envoyé ». Photos et PDF passaient, d'où le symptôme limité à Office.
    ///
    /// 120, comme `PieceSignalement.type_mime`. Élargir ne touche aucune ligne existante.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN.</remarks>
    public partial class TypeMimeDocumentControle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type_mime",
                table: "ControleDocument",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type_mime",
                table: "ControleDocument",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);
        }
    }
}
