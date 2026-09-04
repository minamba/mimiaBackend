using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le droit d'administration délégué à un compte parent.
    ///
    /// TROIS RÔLES, ET DEUX SOURCES QUI NE SE CONFONDENT PAS
    /// -----------------------------------------------------
    /// - SUPER-ADMINISTRATEUR : les adresses de `Admin:Emails`, en
    ///   configuration. C'est un fait de déploiement, pas une donnée
    ///   d'application : il ne se donne ni ne se retire depuis l'interface, et
    ///   c'est ce qui empêche qu'une fausse manœuvre ferme la maison.
    /// - ADMINISTRATEUR : cette colonne. Accordé et retiré par le
    ///   super-administrateur, depuis le tableau de bord.
    /// - UTILISATEUR : tous les autres. C'est l'absence des deux précédents.
    ///
    /// POURQUOI ICI ET NON DANS LA BASE D'IDENTITÉ. Les rôles y vivent
    /// d'ordinaire, mais l'API n'a pas cette base : seule la base métier lui
    /// est ouverte. Y placer le drapeau évite d'inventer un appel entre les
    /// deux services, avec le secret partagé qu'il faudrait pour l'authentifier.
    /// Le serveur d'identité, lui, ouvre déjà la base métier — il y lit les
    /// interrupteurs du mode test — et il y lira ce drapeau au moment d'émettre
    /// le jeton.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : plusieurs migrations
    /// antérieures n'ont pas de `.Designer.cs`, et `dotnet ef migrations add`
    /// produirait une migration qui recrée toute la base. Les deux attributs
    /// ci-dessous remplacent le fichier manquant ; sans eux, EF ignore ce
    /// fichier en silence. Le snapshot est corrigé dans le même geste.
    ///
    /// PAR DÉFAUT À FAUX, ET SANS EXCEPTION. Un compte existant ne devient pas
    /// administrateur parce qu'une colonne apparaît.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260901000000_ParentAdministrateur")]
    public partial class ParentAdministrateur : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "est_administrateur",
                table: "Parent",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "est_administrateur", table: "Parent");
        }
    }
}
