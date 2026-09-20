using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les mots surlignés par le professeur, gardés avec le texte archivé.
    /// </summary>
    /// <remarks>
    /// Voulu par Camara le 19/09/2026 : « je veux les badges dans l'archive
    /// aussi ». En séance, le tableau surligne les mots repris et les numérote ;
    /// l'archive ne gardait que le texte nu et la liste des corrections, et
    /// l'enfant qui la relisait ne voyait plus OÙ étaient les fautes.
    ///
    /// UNE COLONNE À PART, ET LE TEXTE RESTE NU. `texte` est la copie de
    /// l'enfant, mot pour mot : c'est elle que tout le reste lit (aperçus,
    /// comptages, transcription). `texte_surligne` est la même copie avec les
    /// marques <c>==mot==</c> du premier tableau de correction — celui qui
    /// porte tous les badges, avant que le professeur en retire à mesure. NULL
    /// pour les archives d'avant, et quand le tableau et la copie ne se
    /// recoupent pas : on n'affiche pas des badges sur un autre texte.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261021000000_ExpressionEcriteSurlignes")]
    public partial class ExpressionEcriteSurlignes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "texte_surligne",
                table: "ExpressionEcrite",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "texte_surligne", table: "ExpressionEcrite");
        }
    }
}
