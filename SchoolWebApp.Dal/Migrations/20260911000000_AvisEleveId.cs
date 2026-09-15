using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Remplace <c>depose_par_enfant</c> (un simple oui/non, ajouté par
    /// <see cref="AvisDeposeParEnfant"/>) par <c>eleve_id</c> : lequel des
    /// enfants du foyer a déposé cette version de l'avis, et non plus
    /// seulement « un enfant, sans dire lequel ». Un foyer a souvent plus
    /// d'un enfant — le prénom affiché doit être celui qui a vraiment écrit,
    /// pas celui du parent sous une étiquette « Étudiant » trompeuse. Voir
    /// le commentaire sur <see cref="AvisClient.EleveId"/>.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, comme les précédentes : `dotnet ef migrations add`
    /// ne peut pas servir sur ce projet.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260911000000_AvisEleveId")]
    public partial class AvisEleveId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "depose_par_enfant", table: "AvisClient");

            migrationBuilder.AddColumn<int>(
                name: "eleve_id",
                table: "AvisClient",
                type: "int",
                nullable: true);

            // Restrict, pas Cascade : Parent -> Eleve cascade déjà vers cette
            // table via parent_id. Un second chemin en cascade (Parent ->
            // Eleve -> AvisClient) ferait deux chemins de suppression
            // convergents, que SQL Server refuse.
            // L'INDEX SUR LA CLÉ ÉTRANGÈRE, EXPLICITE — EF Core le crée par
            // convention à partir du modèle, mais une migration écrite à la
            // main ne bénéficie pas de cette convention : l'omettre ici
            // aurait laissé le modèle et la base diverger, et le prochain
            // démarrage aurait échoué sur `PendingModelChangesWarning`.
            migrationBuilder.CreateIndex(
                name: "IX_AvisClient_eleve_id",
                table: "AvisClient",
                column: "eleve_id");

            migrationBuilder.AddForeignKey(
                name: "FK_AvisClient_Eleve",
                table: "AvisClient",
                column: "eleve_id",
                principalTable: "Eleve",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_AvisClient_Eleve", table: "AvisClient");
            migrationBuilder.DropIndex(name: "IX_AvisClient_eleve_id", table: "AvisClient");
            migrationBuilder.DropColumn(name: "eleve_id", table: "AvisClient");

            migrationBuilder.AddColumn<bool>(
                name: "depose_par_enfant",
                table: "AvisClient",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
