namespace SchoolWebApp.Dal.Entities
{
    /// <summary>
    /// L'abonnement d'un parent. Un seul actif à la fois par compte.
    ///
    /// Les quotas sont comptés par PÉRIODE et non par mois calendaire : un
    /// abonnement souscrit le 17 court du 17 au 17. Compter en mois civils
    /// offrirait un mois entier de quota à qui s'inscrit le 30.
    /// </summary>
    public partial class Abonnement
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public int OffreId { get; set; }

        public DateTime DateDebut { get; set; }

        /// <summary>Fin de validité. Null pour un abonnement qui se reconduit.</summary>
        public DateTime? DateFin { get; set; }

        /// <summary>
        /// Mensuel ou annuel. Voir <see cref="PeriodiciteAbonnement"/>.
        ///
        /// L'offre porte les DEUX prix ; c'est ici qu'on retient celui qui a
        /// été choisi. Sans ce champ, rien ne permettait de dire quel montant
        /// prélever, ni quand renouveler — la question ne s'était pas posée
        /// tant que rien n'était facturé, elle devient bloquante avec Stripe.
        /// </summary>
        public string Periodicite { get; set; } = Domain.Models.PeriodiciteAbonnement.Mensuel;

        /// <summary>Actif, EnPause, Resilie.</summary>
        public string Statut { get; set; } = StatutAbonnement.Actif;

        /// <summary>Début de la période de facturation en cours.</summary>
        public DateTime PeriodeDebut { get; set; }

        /// <summary>Fin de la période en cours. C'est à cette date que les quotas repartent à zéro.</summary>
        public DateTime PeriodeFin { get; set; }

        /// <summary>
        /// Date de la dernière mise en pause. Une seule par année glissante :
        /// la pause remplace la résiliation d'un mois creux, elle n'est pas un
        /// moyen de ne payer que les mois où l'on travaille.
        /// </summary>
        public DateTime? DernierePause { get; set; }

        /// <summary>Fin de la pause en cours. Null si l'abonnement n'est pas en pause.</summary>
        public DateTime? PauseJusquau { get; set; }

        /// <summary>
        /// Date à laquelle le parent a demandé à ne pas reconduire.
        ///
        /// Une résiliation ne coupe RIEN sur-le-champ : le parent a payé sa
        /// période, il la garde jusqu'au bout. Cette date arrête seulement la
        /// reconduction tacite, et le statut ne bascule sur « Résilié » qu'à
        /// `PeriodeFin`.
        ///
        /// Elle est nullable et non un booléen : elle sert aussi de trace, et
        /// permet de revenir en arrière — la remettre à null annule la demande
        /// tant que la période court encore.
        /// </summary>
        public DateTime? ResiliationDemandeeLe { get; set; }

        /// <summary>
        /// L'alerte des 80 % a déjà été envoyée pour la période en cours. Remis
        /// à faux au renouvellement — sinon le parent ne serait prévenu qu'une
        /// seule fois dans la vie de son abonnement.
        /// </summary>
        public bool AlerteQuotaEnvoyee { get; set; }

        /// <summary>
        /// Date du premier prélèvement refusé resté sans suite. Null quand tout
        /// est à jour.
        ///
        /// CE QU'ELLE NE FAIT PAS : couper l'accès. La période en cours est
        /// payée, le parent la garde jusqu'au bout — Stripe relance la carte
        /// pendant une quinzaine de jours, et la plupart de ces échecs se
        /// résolvent seuls. Couper au premier refus priverait de cours un
        /// enfant dont le parent a simplement changé de carte.
        ///
        /// CE QU'ELLE FAIT : empêcher le RENOUVELLEMENT. À l'échéance, la
        /// période ne repart pas et le pot d'heures ne se recharge pas tant
        /// que la facture n'est pas réglée. Sans elle, l'application offrait un
        /// mois neuf à chaque échéance, payée ou non — le compte restait
        /// pleinement utilisable jusqu'à ce que Stripe abandonne, deux
        /// semaines plus tard. Et si le webhook de clôture se perdait, jamais.
        /// </summary>
        public DateTime? ImpayeDepuis { get; set; }

        /// <summary>
        /// La formule qui remplacera celle-ci au prochain renouvellement.
        ///
        /// POURQUOI UNE DESCENTE DE GAMME N'EST PAS IMMÉDIATE
        /// -------------------------------------------------
        /// Le pot d'heures suit la formule. Passer de Duo (16 h) à Solo (9 h)
        /// le 20 du mois, après en avoir consommé 12, ferait tomber le solde à
        /// zéro sur-le-champ : le parent perdrait des heures qu'il a payées, et
        /// le vivrait comme une panne. Une montée en gamme, elle, n'enlève
        /// rien — elle reste immédiate.
        ///
        /// Null tant qu'aucun changement n'est prévu. Appliqué par
        /// `RafraichirAsync` au passage de période, et reflété chez Stripe par
        /// un calendrier d'abonnement pour que la facturation suive.
        /// </summary>
        public int? OffrePrevueId { get; set; }

        /// <summary>Le rythme de la formule prévue. Null si aucun changement.</summary>
        public string? PeriodicitePrevue { get; set; }

        /// <summary>
        /// Quand le changement prendra effet — la fin de la période en cours au
        /// moment de la demande. Gardée explicitement pour l'écrire au parent
        /// sans avoir à la recalculer, et parce que la période peut bouger.
        /// </summary>
        public DateTime? ChangementPrevuLe { get; set; }

        public virtual Offre? OffrePrevue { get; set; }

        /// <summary>
        /// L'abonnement correspondant chez Stripe (`sub_…`).
        ///
        /// C'est par lui que les webhooks retrouvent leur cible : un
        /// renouvellement, un impayé ou une résiliation faite depuis le
        /// portail client ne citent que cet identifiant.
        ///
        /// Nul pour les abonnements qui ne passent pas par la caisse :
        /// l'essai gratuit, et les comptes de l'exploitant.
        /// </summary>
        public string? StripeAbonnementId { get; set; }

        public DateTime DateCreation { get; set; }

        public virtual Parent? Parent { get; set; }

        public virtual Offre? Offre { get; set; }

        public virtual ICollection<ConsommationEleve> Consommations { get; set; } = new List<ConsommationEleve>();

        public virtual ICollection<Recharge> Recharges { get; set; } = new List<Recharge>();
    }

    public static class StatutAbonnement
    {
        public const string Actif = "Actif";
        public const string EnPause = "EnPause";
        public const string Resilie = "Resilie";
    }

    /// <summary>
    /// Ce qu'un enfant a consommé sur une période donnée.
    ///
    /// Une ligne par enfant et par période, incrémentée À CHAQUE TOUR de parole
    /// et non en fin de séance : un onglet laissé ouvert ou un plantage
    /// laisserait sinon passer une séance entière hors quota.
    /// </summary>
    public partial class ConsommationEleve
    {
        public int Id { get; set; }

        public int AbonnementId { get; set; }

        public int EleveId { get; set; }

        /// <summary>Période à laquelle cette consommation se rattache.</summary>
        public DateTime PeriodeDebut { get; set; }

        /// <summary>
        /// Secondes consommées. En secondes et non en minutes : arrondir chaque
        /// tour à la minute ferait payer une heure pour soixante échanges de
        /// dix secondes.
        /// </summary>
        public int SecondesConsommees { get; set; }

        public DateTime DerniereActivite { get; set; }

        public virtual Abonnement? Abonnement { get; set; }

        public virtual Eleve? Eleve { get; set; }
    }

    /// <summary>
    /// Un achat d'heures supplémentaires. Rattaché à une période : le crédit ne
    /// se reporte pas au mois suivant, et c'est dit à l'achat.
    /// </summary>
    public partial class Recharge
    {
        public int Id { get; set; }

        public int AbonnementId { get; set; }

        /// <summary>Période sur laquelle le crédit est utilisable.</summary>
        public DateTime PeriodeDebut { get; set; }

        public int Minutes { get; set; }

        public int PrixCentimes { get; set; }

        public DateTime DateAchat { get; set; }

        /// <summary>
        /// La session de paiement Stripe qui a payé ce pack.
        ///
        /// C'EST LA GARANTIE QU'UNE RECHARGE N'EST CRÉDITÉE QU'UNE FOIS.
        /// Stripe réémet un événement tant qu'il n'a pas reçu de 200, et
        /// n'ordonne pas ses envois : sans cette colonne et son index unique,
        /// un webhook rejoué offrirait les heures une seconde fois.
        ///
        /// Nulle pour les recharges accordées sans paiement — comptes
        /// exemptés, gestes commerciaux — d'où l'index FILTRÉ : plusieurs
        /// lignes peuvent porter NULL, deux ne peuvent pas porter la même
        /// session.
        /// </summary>
        public string? StripeSessionId { get; set; }

        /// <summary>
        /// Le paiement Stripe (`pi_…`) derrière cette recharge.
        ///
        /// POURQUOI EN PLUS DE LA SESSION. Un remboursement n'arrive pas par
        /// la session mais par la charge, qui ne connaît que son paiement. Sans
        /// cette colonne il faudrait, à chaque remboursement, redemander à
        /// Stripe quelle session correspond — un aller-retour réseau dans un
        /// webhook, pour une information qu'on avait sous la main au moment du
        /// crédit.
        /// </summary>
        public string? StripePaiementId { get; set; }

        /// <summary>
        /// Quand ces heures ont été reprises, remboursement fait.
        ///
        /// MARQUÉE PLUTÔT QU'EFFACÉE. Supprimer la ligne ferait disparaître la
        /// trace de l'achat : on ne saurait plus qu'un parent a payé puis été
        /// remboursé, et sa réclamation deviendrait impossible à instruire. Le
        /// calcul du quota ignore simplement les lignes remboursées.
        /// </summary>
        public DateTime? DateRemboursement { get; set; }

        /// <summary>
        /// Pourquoi ces heures ont été ajoutées ou retirées à la main.
        ///
        /// OBLIGATOIRE SUR UN AJUSTEMENT MANUEL, ET C'EST LE POINT. Une ligne
        /// de +180 minutes sans explication devient, six mois plus tard, un
        /// cadeau que personne ne s'explique — et si un parent conteste, on n'a
        /// rien à lui opposer. Nul sur les achats : leur motif, c'est le
        /// paiement.
        /// </summary>
        public string? Motif { get; set; }

        public virtual Abonnement? Abonnement { get; set; }
    }
}
