using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Les sections du tableau de bord ouvertes à un administrateur.
    /// </summary>
    /// <remarks>
    /// Camara, le 17/09/2026 : « en tant que super admin, je veux pouvoir gérer
    /// les droits des administrateurs… ceux qui sont cochés seront visibles par
    /// l'administrateur, ceux qui ne sont pas cochés seront invisibles ».
    ///
    /// NULLE PAR DÉFAUT, ET ÇA VEUT DIRE « RIEN ». Camara : « de base quand le
    /// super admin passe un utilisateur en admin, tout est décoché ». C'est le
    /// bon sens d'une délégation — on donne ce qu'on veut donner, on n'enlève
    /// pas ce qu'on aurait oublié d'enlever — et c'est aussi ce qui rend cette
    /// migration sans effet sur l'existant : les administrateurs déjà nommés
    /// repartent de zéro, et le super-administrateur leur rouvre ce qu'il veut.
    ///
    /// UNE COLONNE DE TEXTE ET NON UNE TABLE DE JOINTURE. Quatorze clés fixes,
    /// attachées à un compte, jamais interrogées dans l'autre sens : on ne
    /// demande jamais « qui a le droit sur les idées ? ». Une table aurait
    /// ajouté une entité, une migration et un `Include` à chaque lecture de
    /// parent pour ranger quatorze mots. Voir `OngletsAdmin` pour la lecture.
    ///
    /// PAS DE VALEUR PAR DÉFAUT DÉCLARÉE, donc rien à répéter dans le contexte :
    /// la colonne est nullable, et NULL se lit comme une liste vide.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261014000000_OngletsAdmin")]
    public partial class OngletsAdmin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "onglets_admin",
                table: "Parent",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "onglets_admin", table: "Parent");
        }
    }
}
