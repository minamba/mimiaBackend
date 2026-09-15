namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// OÙ COMMENCE « ACQUIS », OÙ COMMENCE « FRAGILE » — UNE SEULE FOIS, POUR
    /// TOUT LE PRODUIT.
    ///
    /// Ces seuils vivaient à quatre endroits, avec quatre valeurs : 0,75 sur la
    /// fiche du parent, 0,80 sur la carte de l'enfant, 0,60 pour ce qu'on
    /// souffle au professeur, 0,40 pour « en cours ». Une compétence à 78 %
    /// était donc ACQUISE pour le parent et EN COURS pour son enfant, sur la
    /// même donnée, le même jour. Personne ne pouvait avoir raison.
    ///
    /// C'est le cœur de ce que l'application vend : la mesure doit dire la même
    /// chose à tout le monde. Toute lecture d'un score passe désormais par ici.
    /// </summary>
    public static class SeuilsMaitrise
    {
        /// <summary>
        /// Au-dessus, la notion est tenue. Volontairement exigeant : afficher
        /// « acquis » à un parent engage le produit, et l'élève, bien plus
        /// qu'un « en cours » de trop.
        /// </summary>
        public const double Acquis = 0.80;

        /// <summary>
        /// En dessous, la notion est fragile et mérite d'être reprise. Entre
        /// les deux, elle est « en cours » : commencée, pas encore tenue.
        /// </summary>
        public const double Fragile = 0.50;

        /// <summary>
        /// EN DESSOUS DE DEUX OBSERVATIONS, ON NE TRANCHE PAS.
        ///
        /// Un seul jugement porté sur une seule conversation devenait « 16 % de
        /// maîtrise » affiché à un parent comme un fait. La confiance était
        /// pourtant calculée — 38 % pour une observation — et n'était utilisée
        /// nulle part. Une compétence vue une seule fois se présente donc comme
        /// « à confirmer » plutôt que de rejoindre les points fragiles ou les
        /// acquis.
        /// </summary>
        public const int ObservationsPourTrancher = 2;

        /// <summary>
        /// PASSÉ CE DÉLAI SANS Y REVENIR, ON NE SAIT PLUS.
        ///
        /// Un score ne bouge qu'à une observation. Une notion montée à 85 %
        /// en septembre affichait donc encore 85 % en juin, présentée au
        /// parent exactement comme une mesure de la veille — rien à l'écran ne
        /// distinguait les deux.
        ///
        /// LE SCORE N'EST PAS DÉGRADÉ POUR AUTANT, et c'est délibéré : le
        /// faire baisser sans observation reviendrait à afficher un chiffre
        /// que rien n'a mesuré, à laisser un parent constater que le niveau
        /// de son enfant recule alors qu'il n'a rien raté, et à contaminer
        /// les mesures suivantes puisque le calcul repart du score précédent.
        /// Ce qui se périme, c'est ce QU'ON SAIT — pas ce que l'enfant sait.
        /// La notion rejoint donc « à confirmer », là où elle appelle une
        /// vérification, au lieu de rejoindre les fragiles où elle
        /// accuserait l'élève.
        ///
        /// Quatre-vingt-dix jours : la révision espacée plafonne à soixante
        /// pour une notion solide. Au-delà de quatre-vingt-dix, l'échéance a
        /// été manquée d'un mois entier — et les vacances les plus longues de
        /// l'année scolaire sont absorbées sans faux signal.
        /// </summary>
        public const int JoursAvantPeremption = 90;

        /// <summary>Ce que vaut ce score, une fois pour toutes.</summary>
        public static string Etat(double score, int observations) =>
            observations < ObservationsPourTrancher ? "a-confirmer"
            : score >= Acquis ? "acquise"
            : score >= Fragile ? "en-cours"
            : "fragile";

        /// <summary>
        /// Le même verdict, mais en tenant compte de l'âge de la mesure.
        ///
        /// Seules les notions ACQUISES se périment. Une lacune ancienne reste
        /// une lacune : ne plus l'annoncer parce qu'elle date la ferait
        /// disparaître de la fiche, alors que c'est justement celle qu'il faut
        /// reprendre.
        /// </summary>
        public static string Etat(double score, int observations, DateTime derniereMesure, DateTime maintenant)
        {
            var etat = Etat(score, observations);

            return etat == "acquise" && Perimee(derniereMesure, maintenant)
                ? "a-confirmer"
                : etat;
        }

        /// <summary>Vrai quand la mesure est trop vieille pour engager le produit.</summary>
        public static bool Perimee(DateTime derniereMesure, DateTime maintenant) =>
            (maintenant - derniereMesure).TotalDays > JoursAvantPeremption;
    }
}
