using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolWebApp.Dal.Entities;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// La vue qui répond, une fois pour toutes, à « combien d'heures ce
    /// compte a-t-il ce mois-ci ? ».
    /// </summary>
    /// <remarks>
    /// POURQUOI UNE VUE, ET PAS DU C#
    /// ------------------------------
    /// La réponse était calculée à DEUX endroits : `AbonnementRepository` pour
    /// l'écran du parent, `AdminRepository` pour le tableau de l'exploitant.
    /// Impossible de partager l'expression — l'une est une requête autonome,
    /// l'autre une sous-requête corrélée à l'intérieur d'une projection, et
    /// aucune expression LINQ partagée ne peut capturer l'abonnement en cours
    /// de projection.
    ///
    /// Les deux moitiés ont donc divergé DEUX FOIS :
    ///
    ///   1. La règle des heures remboursées, corrigée côté parent et oubliée
    ///      ici : l'administration affichait 18 h pour un parent qui en
    ///      voyait 14.
    ///
    ///   2. Le rattachement des recharges à la période, corrigé côté parent le
    ///      04/09/2026 et oublié ici : le parent voyait 12 h,
    ///      l'administration 9.
    ///
    /// Un commentaire disant « à changer aux deux endroits » n'a pas suffi la
    /// première fois, et n'aurait pas plus suffi la troisième. La règle
    /// descend donc dans la base, où il n'y a qu'un exemplaire possible.
    ///
    /// CE QUE ÇA CONCÈDE. Un morceau de logique métier quitte le C# : il faut
    /// une migration pour le modifier, et il n'est pas couvert par le
    /// compilateur. C'est le prix d'une règle qui ne peut plus se dédoubler,
    /// et il est petit devant un écart de facturation qu'on découvre par une
    /// réclamation.
    ///
    /// `EXEC` AUTOUR DU `CREATE VIEW` : SQL Server exige que cette instruction
    /// soit la première de son lot. Passée telle quelle dans une migration qui
    /// en contient d'autres, elle échoue.
    /// </remarks>
    [DbContext(typeof(SchoolWebAppDatabaseContext))]
    [Migration("20260905120000_VueForfaitAbonnement")]
    public partial class VueForfaitAbonnement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // REJOUABLE : une vue se remplace, elle ne s'ajoute pas. Sans ce
            // retrait préalable, la migration échoue sur une base où la vue
            // existe déjà — celle de développement, où on l'a essayée à la
            // main, ou une production où une version antérieure a été posée.
            migrationBuilder.Sql("EXEC('DROP VIEW IF EXISTS vw_ForfaitAbonnement')");

            migrationBuilder.Sql(@"
EXEC('
CREATE VIEW vw_ForfaitAbonnement AS
SELECT
    a.id                                                AS abonnement_id,
    o.minutes_pot_mensuel                               AS minutes_forfait,
    ISNULL(rech.minutes, 0)                             AS minutes_recharge,
    o.minutes_pot_mensuel    + ISNULL(rech.minutes, 0)  AS minutes_allouees,
    o.minutes_plafond_enfant + ISNULL(rech.minutes, 0)  AS minutes_plafond_enfant
FROM Abonnement a
JOIN Offre o ON o.id = a.offre_id
OUTER APPLY (
    SELECT SUM(r.minutes) AS minutes
    FROM Recharge r
    WHERE r.abonnement_id = a.id

      -- LES HEURES REMBOURSÉES NE COMPTENT PLUS. La ligne reste pour
      -- l''historique, elle sort simplement du pot.
      AND r.date_remboursement IS NULL

      -- LE RATTACHEMENT À LA PÉRIODE, EN DEUX BRANCHES.
      --
      -- L''horodatage d''abord : c''est la règle d''origine, et elle est juste
      -- pour tout ce qui a été acheté en cours de période.
      --
      -- La date d''achat ensuite, parce que ''invoice.paid'' REDATE la période
      -- quelques instants après une souscription. Les heures créditées entre
      -- les deux gardaient l''ancien horodatage et devenaient orphelines :
      -- présentes en base, invisibles à l''écran.
      --
      -- Les deux branches ensemble ne peuvent pas ramener une recharge d''une
      -- AUTRE période : celles du mois passé ont une date d''achat antérieure
      -- au début de la période en cours, et un horodatage qui ne correspond
      -- plus.
      AND (r.periode_debut = a.periode_debut
           OR (r.date_achat >= a.periode_debut AND r.date_achat < a.periode_fin))
) rech;
')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("EXEC('DROP VIEW IF EXISTS vw_ForfaitAbonnement')");
        }
    }
}
