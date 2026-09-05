namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Ce que ce compte a le droit de consommer ce mois-ci.
    ///
    /// LU DEPUIS UNE VUE, JAMAIS CALCULÉ ICI
    /// -------------------------------------
    /// La règle — quelles recharges comptent, et lesquelles sont périmées ou
    /// remboursées — vit dans `vw_ForfaitAbonnement`. Elle y est descendue
    /// après avoir divergé DEUX FOIS entre l'écran du parent et le tableau de
    /// l'exploitant : la première en affichant 18 h contre 14, la seconde 9 h
    /// contre 12.
    ///
    /// Aucun appelant ne doit refaire ce calcul. Ajouter une formule, changer
    /// un forfait ou vendre un nouveau pack ne demande donc rien ici : la vue
    /// part des mêmes tables et les deux écrans suivent ensemble.
    ///
    /// SANS CLÉ, ET C'EST VOULU. Une entité sans clé n'est jamais suivie par
    /// le contexte : chaque lecture repart de la base. C'est exactement ce
    /// qu'on veut d'un total qui change dès qu'une recharge est ajoutée —
    /// une valeur mise en cache dans le suivi resterait celle d'avant l'achat.
    /// </summary>
    public partial class ForfaitAbonnement
    {
        public int AbonnementId { get; set; }

        /// <summary>Le forfait de la formule, sans rien d'ajouté.</summary>
        public int MinutesForfait { get; set; }

        /// <summary>Ce que les recharges valables ajoutent. Zéro s'il n'y en a pas.</summary>
        public int MinutesRecharge { get; set; }

        /// <summary>Le pot réel : forfait + recharges.</summary>
        public int MinutesAllouees { get; set; }

        /// <summary>
        /// Le plafond d'un seul enfant, recharges comprises.
        ///
        /// LES RECHARGES LE LÈVENT AUSSI. Le plafond protège les frères et
        /// sœurs d'un aîné qui viderait le pot ; une recharge est ajoutée
        /// exprès pour débloquer quelqu'un. La refuser à celui qu'elle vise
        /// vidait le geste de son sens — et sur Solo, où plafond et forfait
        /// sont égaux, rendait tout pack acheté inutilisable.
        /// </summary>
        public int MinutesPlafondEnfant { get; set; }
    }
}
