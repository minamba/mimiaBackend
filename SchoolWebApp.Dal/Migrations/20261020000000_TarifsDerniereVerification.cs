using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// Ce que la veille des tarifs ajoute à la grille : la date de sa dernière
    /// lecture, et le prix au jeton de la voix d'OpenAI.
    /// </summary>
    /// <remarks>
    /// POURQUOI UNE MIGRATION À PART, ET PAS DANS `TarifsFournisseurs`. Ces
    /// changements y avaient d'abord été ajoutés, APRÈS qu'elle avait été
    /// appliquée en local : la base la tenait pour faite et ne l'a pas rejouée.
    /// La colonne manquait, et l'écran des parents plantait (erreur 500 : « Nom de
    /// colonne non valide : derniere_verification »). Une migration appliquée ne
    /// se modifie plus ; on en ajoute une.
    ///
    /// LA VOIX SE FACTURE AU JETON — 0,60 $ le million de jetons de texte en
    /// entrée, 12 $ le million de jetons audio en sortie, lu sur la grille
    /// d'OpenAI le 19/09/2026. Le prix à la minute (0,015 $) s'en déduit à
    /// raison de 1 250 jetons audio par minute : c'est lui que nos calculs
    /// utilisent, et il ne change pas.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261020000000_TarifsDerniereVerification")]
    public partial class TarifsDerniereVerification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "derniere_verification",
                table: "TarifFournisseur",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE TarifFournisseur
                SET prix_entree = 0.60, prix_sortie = 12,
                    prix_entree_applique = 0.60, prix_sortie_applique = 12,
                    usage = N'La voix des professeurs, facturée au jeton : texte en entrée, audio en sortie. Environ 1 250 jetons audio par minute ; 0,4 minute de parole estimée par tour.'
                WHERE fournisseur = N'openai' AND modele = N'gpt-4o-mini-tts';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "derniere_verification", table: "TarifFournisseur");
        }
    }
}
