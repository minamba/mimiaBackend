namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// Une formule d'abonnement.
    ///
    /// En table et non en constantes : les prix et les quotas changeront, et un
    /// changement de tarif ne doit pas imposer un déploiement. Les abonnements
    /// existants gardent leur offre — une hausse ne s'applique jamais
    /// rétroactivement à qui a déjà signé.
    /// </summary>
    public partial class Offre
    {
        public int Id { get; set; }

        /// <summary>ESSAI, SOLO, DUO, FAMILLE.</summary>
        public string Code { get; set; } = string.Empty;

        public string? Libelle { get; set; }

        /// <summary>Une phrase pour la page de tarifs.</summary>
        public string? Accroche { get; set; }

        /// <summary>
        /// Prix TTC en centimes. En entier : un prix en double finit toujours
        /// par produire un 59,99 € qu'on ne sait pas expliquer.
        /// </summary>
        public int PrixMensuelCentimes { get; set; }

        /// <summary>Prix TTC annuel en centimes. Zéro si l'offre n'existe qu'au mois.</summary>
        public int PrixAnnuelCentimes { get; set; }

        public int NombreEnfantsMax { get; set; }

        /// <summary>Minutes de cours par mois, partagées par toute la fratrie.</summary>
        public int MinutesPotMensuel { get; set; }

        /// <summary>
        /// Minutes maximum qu'un SEUL enfant peut prendre dans le pot. C'est ce
        /// plafond qui empêche un enfant d'épuiser la part de ses frères, et qui
        /// rend le partage de compte entre familles inintéressant.
        /// </summary>
        public int MinutesPlafondEnfant { get; set; }

        /// <summary>Durée de validité en jours. Zéro = reconductible sans fin (offre payante).</summary>
        public int JoursValidite { get; set; }

        public int Ordre { get; set; }

        public bool Active { get; set; }

        /// <summary>Vrai pour l'essai : ne se renouvelle pas, ne se met pas en pause.</summary>
        public bool EstEssai { get; set; }

        /// <summary>
        /// Identifiants des tarifs chez Stripe, un par rythme de facturation.
        ///
        /// EN BASE ET NON EN CONFIGURATION, parce qu'ils DIFFÈRENT entre le
        /// mode test et la production : recopier les produits vers la
        /// production en crée de nouveaux, avec de nouveaux identifiants. Les
        /// écrire en dur ou dans un fichier obligerait à les remplacer à la
        /// main le jour de la mise en ligne, en espérant n'en oublier aucun.
        ///
        /// Chaque base porte donc les siens, et la synchronisation les
        /// renseigne. Nuls tant que la formule n'a pas été poussée chez Stripe
        /// — l'essai gratuit, lui, n'en aura jamais : il ne se paie pas.
        /// </summary>
        public string? StripePrixMensuelId { get; set; }

        public string? StripePrixAnnuelId { get; set; }

        public virtual ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();
    }

    /// <summary>Un pack d'heures supplémentaires, vendu à l'unité.</summary>
    public partial class OffreRecharge
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string? Libelle { get; set; }

        public int Minutes { get; set; }

        /// <summary>Le tarif Stripe correspondant. Paiement unique, pas un abonnement.</summary>
        public string? StripePrixId { get; set; }

        public int PrixCentimes { get; set; }

        public int Ordre { get; set; }

        public bool Active { get; set; }
    }
}
