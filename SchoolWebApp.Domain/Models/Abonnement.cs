namespace SchoolWebApp.Domain.Models
{
    /// <summary>Une formule, telle qu'elle s'affiche sur la page de tarifs.</summary>
    public class OffreTarif
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public string? Accroche { get; set; }

        public int PrixMensuelCentimes { get; set; }

        public int PrixAnnuelCentimes { get; set; }

        public int NombreEnfantsMax { get; set; }

        public int MinutesPotMensuel { get; set; }

        public int MinutesPlafondEnfant { get; set; }

        public bool EstEssai { get; set; }

        public string? StripePrixMensuelId { get; set; }

        public string? StripePrixAnnuelId { get; set; }

        public int JoursValidite { get; set; }

        /// <summary>Heures du pot, pour l'affichage. Le parent compte en heures, pas en minutes.</summary>
        public double HeuresPot => Math.Round(MinutesPotMensuel / 60.0, 1);

        public double HeuresPlafondEnfant => Math.Round(MinutesPlafondEnfant / 60.0, 1);

        /// <summary>Ce que paie le parent par enfant, s'il remplit toutes les places.</summary>
        public int PrixParEnfantCentimes =>
            NombreEnfantsMax == 0 ? 0 : PrixMensuelCentimes / NombreEnfantsMax;

        /// <summary>Mois offerts sur l'engagement annuel. Zéro si l'annuel n'existe pas.</summary>
        public int MoisOfferts =>
            PrixAnnuelCentimes == 0 || PrixMensuelCentimes == 0
                ? 0
                : 12 - (int)Math.Round((double)PrixAnnuelCentimes / PrixMensuelCentimes);
    }

    public class OffreRechargeTarif
    {
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Libelle { get; set; }

        public int Minutes { get; set; }

        public int PrixCentimes { get; set; }

        public string? StripePrixId { get; set; }

        public double Heures => Math.Round(Minutes / 60.0, 1);
    }

    /// <summary>
    /// L'état du quota d'une famille, tel qu'on le montre au parent et tel que
    /// le chat l'interroge avant de laisser un enfant parler.
    /// </summary>
    public class EtatQuota
    {
        public int AbonnementId { get; set; }

        public string? OffreCode { get; set; }

        public string? OffreLibelle { get; set; }

        /// <summary>
        /// La formule en cours est-elle l'essai gratuit ?
        ///
        /// Rendu explicitement plutôt que laissé à déduire du code : la page
        /// des tarifs doit distinguer « votre essai est en cours » de « votre
        /// formule actuelle », et la faire comparer le code à la chaîne
        /// « ESSAI » installerait notre nom d'offre dans le navigateur.
        /// </summary>
        public bool EstEssai { get; set; }

        /// <summary>
        /// Mensuel ou Annuel. Sans lui, un écran ne peut pas distinguer « vous
        /// avez déjà cette formule » de « vous l'avez, mais au mois » — deux
        /// situations qui n'appellent pas le même bouton.
        /// </summary>
        public string? Periodicite { get; set; }

        public string? Statut { get; set; }

        public DateTime PeriodeDebut { get; set; }

        public DateTime PeriodeFin { get; set; }

        public int NombreEnfantsMax { get; set; }

        // ------------------------------------------------------------- le pot
        /// <summary>Minutes du pot mensuel, recharges comprises.</summary>
        public int MinutesAllouees { get; set; }

        public int MinutesConsommees { get; set; }

        /// <summary>Dont minutes venues de recharges achetées sur la période.</summary>
        public int MinutesRecharge { get; set; }

        public int MinutesRestantes => Math.Max(0, MinutesAllouees - MinutesConsommees);

        public double PartConsommee =>
            MinutesAllouees == 0 ? 1 : Math.Min(1, (double)MinutesConsommees / MinutesAllouees);

        // -------------------------------------------------------- par enfant
        public int MinutesPlafondEnfant { get; set; }

        public List<QuotaEnfant> Enfants { get; set; } = [];

        // ------------------------------------------------------------- pause
        public bool EnPause { get; set; }

        public DateTime? PauseJusquau { get; set; }

        /// <summary>Une pause par année glissante : dit si le droit est disponible.</summary>
        public bool PauseDisponible { get; set; }

        // ------------------------------------------------------------ impayé
        /// <summary>
        /// Un prélèvement est resté sans suite. La période en cours est payée
        /// et reste servie ; c'est le renouvellement qui attend le règlement.
        ///
        /// L'écran doit le dire AVANT l'échéance, avec le lien vers le portail
        /// de facturation : un parent prévenu met sa carte à jour en deux
        /// minutes, un parent qui découvre son compte bloqué écrit au support.
        /// </summary>
        public bool Impaye { get; set; }

        public DateTime? ImpayeDepuis { get; set; }

        // -------------------------------------------- changement programmé
        /// <summary>
        /// La formule qui prendra le relais au renouvellement, quand une
        /// descente de gamme a été demandée. Null sinon.
        ///
        /// L'écran doit pouvoir écrire « vous passerez à Solo le 8 septembre »
        /// et offrir d'annuler : un changement invisible jusqu'au jour où il
        /// s'applique est un changement qu'on subit.
        /// </summary>
        public string? OffrePrevueCode { get; set; }

        public string? OffrePrevueLibelle { get; set; }

        public string? PeriodicitePrevue { get; set; }

        public DateTime? ChangementPrevuLe { get; set; }

        // ------------------------------------------------------- résiliation
        /// <summary>
        /// Vrai quand le parent a demandé à ne pas reconduire. L'accès continue
        /// jusqu'à `PeriodeFin` : c'est une période déjà payée.
        /// </summary>
        public bool ResiliationDemandee { get; set; }

        /// <summary>
        /// Jour où l'accès prendra fin si la demande n'est pas annulée. C'est
        /// `PeriodeFin`, mais nommé explicitement : l'écran doit pouvoir écrire
        /// « vous gardez tout jusqu'au 6 septembre » sans avoir à le déduire.
        /// </summary>
        public DateTime? FinPrevue { get; set; }

        /// <summary>
        /// Faux quand le pot est épuisé, l'abonnement suspendu ou clos. Le test
        /// sur le statut compte autant que celui sur les minutes : un essai
        /// expiré garde un pot rempli, il n'ouvre plus rien pour autant.
        /// </summary>
        public bool PeutTravailler =>
            Statut == "Actif" && MinutesRestantes > 0;
    }

    /// <summary>
    /// Ce que la formule autorise en nombre de profils enfants.
    /// </summary>
    public record CapaciteEnfants(int Actuels, int Maximum, string? OffreLibelle)
    {
        public bool PeutAjouter => Actuels < Maximum;

        public int Restantes => Math.Max(0, Maximum - Actuels);
    }

    public class QuotaEnfant
    {
        public int EleveId { get; set; }

        public string? Prenom { get; set; }

        public int MinutesConsommees { get; set; }

        public int MinutesPlafond { get; set; }

        public int MinutesRestantes => Math.Max(0, MinutesPlafond - MinutesConsommees);

        public double PartConsommee =>
            MinutesPlafond == 0 ? 1 : Math.Min(1, (double)MinutesConsommees / MinutesPlafond);
    }

    /// <summary>
    /// Verdict rendu avant de laisser un enfant continuer une séance.
    /// Le motif sert à afficher le bon message : un pot familial épuisé et un
    /// plafond individuel atteint n'appellent pas la même réponse.
    /// </summary>
    public record VerdictQuota(bool Autorise, MotifRefus Motif, int MinutesRestantes)
    {
        public static VerdictQuota Ok(int restantes) =>
            new(true, MotifRefus.Aucun, restantes);

        public static VerdictQuota Refus(MotifRefus motif) => new(false, motif, 0);
    }

    public enum MotifRefus
    {
        Aucun = 0,

        /// <summary>Aucun abonnement, ou essai expiré.</summary>
        SansAbonnement = 1,

        /// <summary>Le parent a mis l'abonnement en pause.</summary>
        EnPause = 2,

        /// <summary>
        /// La période est échue et le prélèvement n'est pas passé.
        ///
        /// Distinct de `SansAbonnement` : l'abonnement existe, il est à jour
        /// d'un simple moyen de paiement. Le parent doit être envoyé mettre sa
        /// carte à jour, pas invité à souscrire une formule qu'il a déjà.
        /// </summary>
        Impaye = 7,

        /// <summary>Le pot de la famille est vide : une recharge le débloque.</summary>
        PotEpuise = 3,

        /// <summary>Cet enfant a atteint son plafond, mais le pot familial a encore des heures.</summary>
        PlafondEnfant = 4,

        /// <summary>
        /// La formule couvre moins d'enfants que le compte n'en compte, et les
        /// places du mois sont déjà prises par d'autres. Sans ce refus, un
        /// forfait Solo ferait travailler quatre enfants et personne n'aurait
        /// de raison de prendre Famille.
        /// </summary>
        TropDEnfants = 5,

        /// <summary>
        /// Garde-fou d'emballement : la consommation de jetons du jour dépasse
        /// tout usage plausible.
        ///
        /// N'a RIEN à voir avec le forfait, et ne se débloque pas par une
        /// recharge — c'est un disjoncteur, pas un quota. Il existe parce que
        /// les heures vendues et les jetons facturés par le modèle ne varient
        /// pas ensemble : dix messages courts en une minute déclenchent dix
        /// appels et ne décomptent qu'une poignée de secondes.
        ///
        /// Réglé assez haut pour qu'aucune séance réelle ne l'atteigne, même
        /// un cours de terminale dense en explications.
        /// </summary>
        PlafondJetons = 6,
    }
}
