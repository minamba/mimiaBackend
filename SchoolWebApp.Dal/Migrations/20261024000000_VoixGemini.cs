using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La voix de Google entre dans les comptes — Camara, le 28/09/2026 : « le
    /// calcul des coûts doit être adapté aux prix de Gemini ; si je bascule de
    /// l'un à l'autre, les prix doivent être différenciés ».
    /// </summary>
    /// <remarks>
    /// DEUX CHOSES.
    ///
    /// 1. <c>Message.modele_voix</c> : le modèle qui a prêté sa voix à CETTE
    ///    réponse du professeur (<c>gpt-4o-mini-tts</c>, <c>gemini-3.8-flash-lite-tts</c>,
    ///    <c>tts-1</c> quand le secours est allumé). L'interrupteur de
    ///    l'administration se bascule à tout moment : sans cette trace, une
    ///    période mêlant les deux serait comptée entière au prix de l'un.
    ///    NULL pour tout ce qui précède : ces réponses ont été dites par OpenAI,
    ///    et c'est ainsi qu'elles restent comptées.
    ///
    /// 2. Les lignes Gemini de la grille des tarifs, lues sur la page de Google
    ///    le 28/09/2026 (<c>ai.google.dev/gemini-api/docs/pricing</c>) : texte en
    ///    entrée 0,50 $ le million de jetons ; audio en sortie 6 $ (Flash-Lite)
    ///    et 9 $ (Flash), jusqu'au 31/12/2026 — puis 1 $ / 12 $ / 18 $ à partir
    ///    du 1er janvier 2027, ce que la veille quotidienne reprendra d'elle-même.
    ///
    ///    LE PRIX À LA MINUTE COMPTE 39 JETONS AUDIO PAR SECONDE, et non les 25
    ///    qu'annonce Google : c'est ce que ses propres réponses facturaient
    ///    (<c>usageMetadata.candidatesTokenCount</c>) sur nos appels mesurés le
    ///    même jour. 6 $ × 2 340 jetons / 1 000 000 = 0,01404 $ la minute. Si la
    ///    facture Google dit moins, le prix appliqué se corrige à la main dans
    ///    l'onglet Tarifs.
    ///
    ///    Flash (au-dessus de Lite) est inscrit mais NON COMPTÉ : il ne sert pas.
    ///
    /// ÉCRITE À LA MAIN, comme toutes les précédentes.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20261024000000_VoixGemini")]
    public partial class VoixGemini : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "modele_voix",
                table: "Message",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO TarifFournisseur
                    (fournisseur, modele, usage, prix_entree, prix_sortie, prix_minute,
                     prix_entree_applique, prix_sortie_applique, prix_minute_applique,
                     ordre, date_mise_a_jour)
                VALUES
                    (N'gemini', N'gemini-3.8-flash-lite-tts',
                     N'La voix des professeurs quand l''interrupteur « Voix Gemini » est allumé. Facturée au jeton : environ 39 jetons audio par seconde mesurés (Google en annonce 25) ; 0,4 minute de parole estimée par tour.',
                     0.50, 6, 0.01404, 0.50, 6, 0.01404, 1, '2026-09-28'),
                    (N'gemini', N'gemini-3.8-flash-tts',
                     N'La version au-dessus de Flash-Lite. Non comptée : elle ne sert pas.',
                     0.50, 9, 0.02106, NULL, NULL, NULL, 2, '2026-09-28');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM TarifFournisseur WHERE fournisseur = N'gemini';");
            migrationBuilder.DropColumn(name: "modele_voix", table: "Message");
        }
    }
}
