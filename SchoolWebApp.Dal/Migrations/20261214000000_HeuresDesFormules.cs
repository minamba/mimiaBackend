using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolWebApp.Dal.Migrations
{
    /// <summary>
    /// LES HEURES DES FORMULES — Camara, le 14/11/2026 : Starter 4 h (au lieu de 4 h 30), Solo 9 h (inchangé), Duo 18 h
    /// (au lieu de 16), Famille 28 h (au lieu de 24 ; 32 h demandées, ramenées à 28 après le calcul du pire cas : ~92 %
    /// du prix en coût d'IA à pleine consommation). Plafond par enfant : 12 h en Duo comme en Famille (au lieu de 10).
    ///
    /// Le seed n'écrase jamais une offre existante : d'où cette migration. Les abonnés en cours lisent l'offre en
    /// direct, ils ont donc les nouvelles heures dès le redémarrage. Starter n'existe peut-être pas encore (pas déployé) :
    /// la ligne ne touche rien, et le seed l'insère ensuite avec 4 h.
    /// </summary>
    /// <remarks>ÉCRITE À LA MAIN. Données seulement : le modèle ne change pas.</remarks>
    public partial class HeuresDesFormules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Offre] SET [minutes_pot_mensuel] = 240, [minutes_plafond_enfant] = 240 WHERE [code] = 'STARTER';");
            migrationBuilder.Sql("UPDATE [Offre] SET [minutes_pot_mensuel] = 1080, [minutes_plafond_enfant] = 720 WHERE [code] = 'DUO';");
            migrationBuilder.Sql("UPDATE [Offre] SET [minutes_pot_mensuel] = 1680, [minutes_plafond_enfant] = 720 WHERE [code] = 'FAMILLE';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Offre] SET [minutes_pot_mensuel] = 270, [minutes_plafond_enfant] = 270 WHERE [code] = 'STARTER';");
            migrationBuilder.Sql("UPDATE [Offre] SET [minutes_pot_mensuel] = 960, [minutes_plafond_enfant] = 600 WHERE [code] = 'DUO';");
            migrationBuilder.Sql("UPDATE [Offre] SET [minutes_pot_mensuel] = 1440, [minutes_plafond_enfant] = 600 WHERE [code] = 'FAMILLE';");
        }
    }
}
