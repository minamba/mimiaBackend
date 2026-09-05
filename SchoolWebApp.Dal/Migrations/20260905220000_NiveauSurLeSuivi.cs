using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La classe de l'élève au moment de l'évaluation.
    /// </summary>
    /// <remarks>
    /// ÉCRITE À LA MAIN, COMME LES PRÉCÉDENTES. `dotnet ef migrations add` ne
    /// peut pas être utilisé sur ce projet : plusieurs migrations antérieures
    /// sont manuscrites et n'ont pas de `.Designer.cs`. L'outil ne trouve alors
    /// aucun modèle de référence, recule jusqu'au vide, et produit une migration
    /// qui RECRÉE TOUTE LA BASE. Les deux attributs ci-dessous vivent d'ordinaire
    /// dans le `.Designer.cs` ; sans eux, EF ne reconnaît pas le fichier comme
    /// une migration. Le snapshot est corrigé dans le même geste.
    ///
    /// POURQUOI CETTE COLONNE.
    /// Une note ne portait que sa matière, son libellé et sa date. Rien ne disait
    /// dans quelle classe l'élève était ce jour-là. La fiche empilait donc les
    /// notes du CP à l'année en cours dans une seule courbe, alors qu'on suit le
    /// niveau d'UNE classe. Le changement de classe étant une simple mise à jour
    /// de colonne sur `Eleve`, il ne laisse aucune trace datée : l'information
    /// n'existait nulle part et ne pouvait pas être déduite.
    ///
    /// NULLABLE, ET DÉFINITIVEMENT.
    /// Les notes déjà en base n'ont pas de classe et n'en auront jamais. Leur en
    /// attribuer une — celle de l'élève aujourd'hui, par exemple — daterait
    /// faussement le travail d'un enfant : une note de 6e deviendrait une note de
    /// 3e. Elles restent donc à NULL et se rangent dans « Toute la scolarité ».
    /// C'est une lacune assumée et bornée : elle ne concerne que l'existant.
    ///
    /// L'INDEX EST CELUI DE LA CLÉ ÉTRANGÈRE, pas un index de confort. EF en
    /// crée un par convention sous toute FK, et le snapshot l'attend : l'omettre
    /// ferait diverger le modèle de la base au premier contrôle. La colonne
    /// n'est de toute façon jamais un critère de recherche — les évaluations
    /// sont lues par élève, puis regroupées en mémoire.
    ///
    /// NoAction SUR LA CLÉ ÉTRANGÈRE. Un niveau scolaire est une donnée de
    /// référence : il ne se supprime pas. La contrainte est là pour interdire un
    /// identifiant fantaisiste, pas pour propager une suppression qui n'arrivera
    /// pas. Cascade aurait en plus croisé les chemins déjà existants vers
    /// Evaluation, que SQL Server refuse.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260905220000_NiveauSurLeSuivi")]
    public partial class NiveauSurLeSuivi : Migration
    {
        /// <summary>Les trois tables qui datent le travail d'un élève.</summary>
        private static readonly string[] Tables = ["Evaluation", "Conversation", "MaitriseEleve"];

        /// <remarks>
        /// ÉCRITE EN SQL GARDÉ PLUTÔT QU'AVEC `AddColumn`, ET C'EST UNE
        /// RÉPARATION.
        ///
        /// Une première version de cette migration, nommée
        /// `20260905220000_NiveauSurEvaluation`, ne touchait que la table
        /// Evaluation et A DÉJÀ ÉTÉ APPLIQUÉE en développement. Elle a ensuite
        /// été étendue aux trois tables et renommée — c'est-à-dire modifiée
        /// après avoir tourné, ce qu'on ne fait jamais : l'identifiant ayant
        /// changé, EF considère la nouvelle comme jamais appliquée et rejoue
        /// l'ajout d'une colonne qui existe déjà.
        ///
        /// Les gardes rendent donc l'opération rejouable : sur une base neuve —
        /// la production — les trois colonnes sont créées ; sur celle de
        /// développement, Evaluation est sautée et les deux autres ajoutées.
        /// Aucune suppression, aucune donnée touchée.
        ///
        /// Il restera en développement une ligne orpheline
        /// `NiveauSurEvaluation` dans `__EFMigrationsHistory`. Elle est inerte :
        /// EF ne regarde que les migrations qu'il connaît et ignore le reste.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('{table}') AND name = 'niveau_scolaire_id')
    ALTER TABLE [{table}] ADD [niveau_scolaire_id] int NULL;");

                migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_{table}_niveau_scolaire_id' AND object_id = OBJECT_ID('{table}'))
    CREATE INDEX [IX_{table}_niveau_scolaire_id] ON [{table}] ([niveau_scolaire_id]);");

                migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
               WHERE name = 'FK_{table}_NiveauScolaire_niveau_scolaire_id')
    ALTER TABLE [{table}] ADD CONSTRAINT [FK_{table}_NiveauScolaire_niveau_scolaire_id]
        FOREIGN KEY ([niveau_scolaire_id]) REFERENCES [NiveauScolaire] ([id]);");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = 'FK_{table}_NiveauScolaire_niveau_scolaire_id')
    ALTER TABLE [{table}] DROP CONSTRAINT [FK_{table}_NiveauScolaire_niveau_scolaire_id];");

                migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_{table}_niveau_scolaire_id' AND object_id = OBJECT_ID('{table}'))
    DROP INDEX [IX_{table}_niveau_scolaire_id] ON [{table}];");

                migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('{table}') AND name = 'niveau_scolaire_id')
    ALTER TABLE [{table}] DROP COLUMN [niveau_scolaire_id];");
            }
        }
    }
}
