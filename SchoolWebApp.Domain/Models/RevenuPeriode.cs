namespace SchoolWebApp.Domain.Models
{
    /// <summary>
    /// Ce que le produit rapporte sur un mois ou une année — voulu par Camara
    /// le 19/09/2026 : « comme ça je pourrais voir au mois si je suis en
    /// bénéfice ou totalement en perte ».
    ///
    /// PAS AU JOUR NI À LA SEMAINE. Un abonnement se paie au mois ou à
    /// l'année : le découper en jours inventerait des recettes qui n'existent
    /// pas. <see cref="Disponible"/> le dit à l'écran.
    ///
    /// LES MENSUELS COMPTENT LEURS ÉCHÉANCES. Une famille abonnée le 20 paie le
    /// 20 de chaque mois : chaque échéance tombée dans la période compte une
    /// fois, sauf pendant une pause ou un impayé.
    ///
    /// LES ANNUELS SONT ÉTALÉS : leur prix divisé par douze, pour chaque mois
    /// couvert par l'abonnement. Les compter en entier le mois du paiement
    /// ferait un mois faussement riche puis onze faussement pauvres — et le
    /// résultat du mois ne voudrait plus rien dire.
    ///
    /// LES PACKS D'HEURES comptent en entier le mois de l'achat, remboursés
    /// exclus : un achat ponctuel n'a rien à étaler.
    ///
    /// CE QUI N'Y EST PAS : les frais Stripe, et la TVA, qui n'est pas encore
    /// tranchée. Les prix sont ceux du catalogue, TTC — il n'existe aucune
    /// remise ni code promo dans le produit.
    /// </summary>
    public class RevenuPeriode
    {
        public DateTime Debut { get; set; }

        public DateTime Fin { get; set; }

        /// <summary>Faux au jour et à la semaine : rien n'est calculé.</summary>
        public bool Disponible { get; set; }

        /// <summary>Le détail par formule et par rythme, du plus rapporteur au moins.</summary>
        public List<LigneRevenu> Lignes { get; set; } = [];

        /// <summary>Échéances mensuelles tombées dans la période.</summary>
        public int Mensualites { get; set; }

        public decimal MensuelsEuros { get; set; }

        /// <summary>Abonnements annuels différents pris en compte.</summary>
        public int AnnuelsAbonnements { get; set; }

        /// <summary>
        /// Mois d'abonnement annuel comptés, chacun au douzième du prix. Égal
        /// à <see cref="AnnuelsAbonnements"/> sur un mois ; jusqu'à douze fois
        /// plus sur une année.
        /// </summary>
        public int AnnuelsMois { get; set; }

        public decimal AnnuelsEuros { get; set; }

        public int Packs { get; set; }

        public decimal PacksEuros { get; set; }

        public decimal TotalEuros { get; set; }
    }

    /// <summary>Une formule sur un rythme : « Solo, mensuel, 3 échéances, 119,70 € ».</summary>
    public class LigneRevenu
    {
        public string? Code { get; set; }

        public string? Libelle { get; set; }

        /// <summary><c>Mensuel</c> ou <c>Annuel</c>.</summary>
        public string? Periodicite { get; set; }

        /// <summary>Échéances (mensuel) ou mois étalés (annuel).</summary>
        public int Nombre { get; set; }

        /// <summary>Familles différentes derrière ce nombre.</summary>
        public int Abonnements { get; set; }

        /// <summary>Le prix du catalogue : mensuel, ou annuel ENTIER (avant division).</summary>
        public int PrixCentimes { get; set; }

        public decimal Euros { get; set; }
    }
}
