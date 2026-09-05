using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La date à laquelle l'enfant a vu sa progression pour la dernière fois.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add` ne
    /// peut pas servir sur ce projet, plusieurs migrations antérieures n'ayant
    /// pas de `.Designer.cs`. Les deux attributs et le snapshot corrigé en
    /// tiennent lieu.
    ///
    /// POURQUOI UNE DATE ET PAS UN COMPTEUR
    /// ------------------------------------
    /// La récompense n'est pas « combien de compétences as-tu ? » mais
    /// « qu'as-tu gagné DEPUIS LA DERNIÈRE FOIS ? ». Un compteur dirait le
    /// total ; il faut une frontière dans le temps pour distinguer ce qui est
    /// nouveau — et c'est ce qui rend le moment digne d'une animation.
    ///
    /// La maîtrise est calculée par un observateur qui tourne APRÈS la séance :
    /// la victoire ne peut donc pas être annoncée pendant le cours. Elle attend
    /// le retour de l'enfant, et c'est cette date qui sait quoi lui montrer.
    ///
    /// NULLE AU DÉPART, et c'est voulu : un enfant qui ouvre sa carte pour la
    /// première fois voit TOUT ce qu'il a déjà acquis comme nouveau. C'est la
    /// bonne première impression — pas un écran vide sur des mois de travail.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260905180000_ProgressionVue")]
    public partial class ProgressionVue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "progression_vue_le",
                table: "Eleve",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "progression_vue_le", table: "Eleve");
        }
    }
}
