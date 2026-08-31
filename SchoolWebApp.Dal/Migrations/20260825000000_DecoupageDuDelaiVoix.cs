using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Le délai ressenti par l'élève, décomposé.
    ///
    /// CE QU'ON MESURAIT, ET POURQUOI ÇA NE SUFFISAIT PAS
    /// --------------------------------------------------
    /// `delai_ms` ne couvre qu'un maillon : du texte affiché à la première
    /// syllabe entendue. Sur 759 mesures il est sain — médiane 699 ms, p90
    /// 1063 ms. Mais l'élève, lui, attend jusqu'à cinq secondes entre le
    /// moment où il se tait et celui où le professeur parle.
    ///
    /// Trois maillons manquaient, et l'un d'eux est probablement le plus long
    /// de la chaîne :
    ///
    ///   transcription_ms — du silence détecté au texte final reçu
    ///   assemblage_ms    — l'attente volontaire avant d'envoyer le tour
    ///   reponse_ms       — du message envoyé au premier caractère du modèle
    ///
    /// `assemblage_ms` est le suspect : il vaut 150 ms d'ordinaire, mais 2500
    /// quand la phrase semble inachevée ou quand le professeur venait de
    /// demander une justification. On peut le baisser — mais pas à l'aveugle :
    /// il a été mis là pour ne pas couper la parole à un enfant qui cherche
    /// ses mots.
    ///
    /// NULLABLES, ET SANS VALEUR PAR DÉFAUT. Les 759 mesures déjà prises n'ont
    /// pas ces chiffres et n'en auront jamais. Un zéro laisserait croire à une
    /// étape instantanée et fausserait toutes les moyennes ; `NULL` dit ce qui
    /// est vrai — on ne sait pas.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les trois précédentes. `dotnet ef migrations
    /// add` est inutilisable sur ce projet : plusieurs migrations antérieures
    /// n'ont pas de `.Designer.cs`, l'outil ne trouve aucun modèle de
    /// référence, recule jusqu'au vide et produit une migration qui RECRÉE
    /// TOUTE LA BASE.
    ///
    /// Les deux attributs ci-dessous ne sont pas décoratifs : sans eux, EF ne
    /// reconnaît pas le fichier comme une migration et `GetPendingMigrations`
    /// rend « aucune », sans la moindre erreur.
    ///
    /// Le snapshot a été corrigé à la main dans le même geste, sans quoi le
    /// démarrage lève `PendingModelChangesWarning`.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260825000000_DecoupageDuDelaiVoix")]
    public partial class DecoupageDuDelaiVoix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "transcription_ms",
                table: "MesureVoix",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "assemblage_ms",
                table: "MesureVoix",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reponse_ms",
                table: "MesureVoix",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "reponse_ms", table: "MesureVoix");
            migrationBuilder.DropColumn(name: "assemblage_ms", table: "MesureVoix");
            migrationBuilder.DropColumn(name: "transcription_ms", table: "MesureVoix");
        }
    }
}
