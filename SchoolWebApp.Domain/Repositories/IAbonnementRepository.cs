using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Domain.Repositories
{
    public interface IAbonnementRepository
    {
        // ---------------------------------------------------------- catalogue
        Task<IEnumerable<OffreTarif>> GetOffresAsync(CancellationToken ct = default);

        Task<IEnumerable<OffreRechargeTarif>> GetOffresRechargeAsync(CancellationToken ct = default);

        // -------------------------------------------------------- abonnement
        /// <summary>
        /// L'état du quota du parent. Null s'il n'a aucun abonnement.
        ///
        /// Renouvelle la période au passage si elle est échue : le quota doit
        /// repartir à zéro dès la première lecture qui suit l'échéance, sans
        /// dépendre d'une tâche planifiée qui pourrait ne pas tourner.
        /// </summary>
        Task<EtatQuota?> GetEtatQuotaAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Ouvre un abonnement pour ce parent, en clôturant le précédent.
        /// Retourne null si le code d'offre est inconnu.
        /// </summary>
        /// <param name="periodicite">Mensuel ou Annuel — voir PeriodiciteAbonnement.</param>
        /// <summary>Retient les tarifs Stripe d'une formule après synchronisation.</summary>
        Task EnregistrerPrixStripeAsync(
            string codeOffre, string prixMensuelId, string prixAnnuelId, CancellationToken ct = default);

        /// <summary>Idem pour une recharge, qui n'a qu'un tarif.</summary>
        Task EnregistrerPrixRechargeStripeAsync(
            string codeRecharge, string prixId, CancellationToken ct = default);

        /// <summary>
        /// Jetons consommés par cet élève depuis minuit — entrée, sortie et
        /// cache confondus. Sert au seul disjoncteur d'emballement.
        /// </summary>
        Task<long> JetonsDuJourAsync(int eleveId, CancellationToken ct = default);

        /// <param name="stripeAbonnementId">
        /// L'abonnement Stripe qui vient d'être payé, quand il y en a un. Null
        /// pour l'essai gratuit et les comptes de l'exploitant : ceux-là ne
        /// passent pas par la caisse, et leur inventer un identifiant ferait
        /// chercher un abonnement inexistant au premier renouvellement.
        /// </param>
        Task<EtatQuota?> SouscrireAsync(
            int parentId,
            string codeOffre,
            string periodicite,
            string? stripeAbonnementId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Ouvre l'essai gratuit, UNE SEULE FOIS dans la vie du compte.
        ///
        /// La garde n'est pas un détail de confort, c'est ce qui rend la route
        /// appelable depuis un bouton public. « Commencer gratuitement » reste
        /// dans la barre de navigation une fois connecté : sans cette
        /// vérification, un parent qui paie 99 € par mois et qui le retouche
        /// par habitude verrait sa formule remplacée par l'essai. Et rien ne
        /// l'en avertirait — il ne s'en apercevrait qu'à court d'heures.
        ///
        /// Rend l'état courant sans rien changer si le compte a déjà eu un
        /// abonnement, quel qu'il soit. Null si aucune offre d'essai n'est
        /// active.
        /// </summary>
        Task<EtatQuota?> OuvrirEssaiAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Les abonnements Stripe de ce parent, pour aller les résilier chez
        /// Stripe avant d'effacer son compte. Vide s'il n'a jamais payé.
        /// </summary>
        Task<IReadOnlyList<string>> AbonnementsStripeAsync(
            int parentId, CancellationToken ct = default);

        /// <summary>
        /// L'abonnement Stripe de la formule EN COURS, ou null. C'est lui qu'on
        /// modifie quand le parent change de formule.
        /// </summary>
        Task<string?> AbonnementStripeCourantAsync(
            int parentId, CancellationToken ct = default);

        /// <summary>
        /// Change la formule SANS rouvrir un abonnement.
        ///
        /// POURQUOI PAS UN `SouscrireAsync` DE PLUS
        /// ---------------------------------------
        /// Souscrire clôt l'abonnement précédent et en ouvre un neuf. C'est
        /// juste quand on repart de zéro, faux quand on change de formule :
        /// chez Stripe, l'abonnement est le MÊME objet, on ne fait que
        /// remplacer son article. Deux lignes locales pour un seul abonnement
        /// Stripe, ce serait deux lignes portant le même `sub_…` — que l'index
        /// unique refuse, à juste titre.
        ///
        /// La période n'est pas remise à zéro : le cycle de facturation ne
        /// bouge pas chez Stripe non plus. Le nouveau pot d'heures s'applique
        /// immédiatement, et ce qui a déjà été consommé reste décompté —
        /// autrement, changer de formule le 30 du mois offrirait un pot neuf.
        /// </summary>
        Task<EtatQuota?> ChangerDeFormuleAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default);

        /// <summary>
        /// Note une descente de gamme pour le prochain renouvellement, sans
        /// rien changer à la formule en cours. Elle prend effet au passage de
        /// période, dans `RafraichirAsync`.
        /// </summary>
        Task<EtatQuota?> PlanifierChangementAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default);

        /// <summary>Efface un changement programmé : le parent a changé d'avis.</summary>
        Task<EtatQuota?> AnnulerChangementPrevuAsync(
            int parentId, CancellationToken ct = default);

        /// <summary>
        /// Reporte la période au rythme de la facturation Stripe.
        ///
        /// Appelé au renouvellement : c'est la facture payée qui fait foi, pas
        /// notre calendrier. Un mois de trente-et-un jours, un décalage de
        /// prélèvement, une remise — Stripe sait quand la période court, nous
        /// non. Faux si aucun abonnement ne porte cet identifiant.
        /// </summary>
        Task<bool> ReporterPeriodeAsync(
            string stripeAbonnementId,
            DateTime periodeDebut,
            DateTime periodeFin,
            CancellationToken ct = default);

        /// <summary>
        /// Note qu'un prélèvement a été refusé. La période EN COURS reste
        /// servie — elle est payée — mais elle ne sera pas renouvelée tant que
        /// la facture n'est pas réglée.
        ///
        /// Conserve la date du PREMIER refus : Stripe relance plusieurs fois,
        /// et écraser la date à chaque tentative ferait perdre l'ancienneté.
        /// </summary>
        Task<bool> MarquerImpayeAsync(
            string stripeAbonnementId, CancellationToken ct = default);

        /// <summary>
        /// Clôt l'abonnement que Stripe déclare terminé — fin de résiliation,
        /// ou impayé au bout des relances. Faux si l'identifiant est inconnu.
        /// </summary>
        Task<bool> CloreParStripeAsync(
            string stripeAbonnementId, CancellationToken ct = default);

        /// <summary>
        /// Peut-on laisser cet enfant continuer ? Vérifie l'abonnement, la
        /// pause, le pot familial et le plafond individuel, dans cet ordre.
        /// </summary>
        Task<VerdictQuota> VerifierAsync(int eleveId, CancellationToken ct = default);

        /// <summary>
        /// Combien d'enfants ce compte peut-il encore enregistrer ?
        ///
        /// Sans abonnement, une place reste ouverte : un parent qui vient de
        /// s'inscrire doit pouvoir créer un profil avant de choisir sa formule,
        /// sinon il ne peut même pas commencer l'essai.
        /// </summary>
        Task<CapaciteEnfants> CapaciteAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Impute des secondes de cours à un enfant. Appelé À CHAQUE TOUR de
        /// parole : compter en fin de séance laisserait passer une séance
        /// entière si l'onglet est fermé brutalement.
        ///
        /// Retourne vrai si le seuil d'alerte vient d'être franchi, pour que
        /// l'appelant prévienne le parent — une seule fois par période.
        /// </summary>
        Task<bool> ImputerAsync(
            int eleveId, int secondes, double seuilAlerte, CancellationToken ct = default);

        // --------------------------------------------------------------- pause
        /// <summary>
        /// Met l'abonnement en pause pour un mois. Retourne null si le droit
        /// n'est pas disponible (déjà utilisé dans l'année, ou offre d'essai).
        /// </summary>
        Task<EtatQuota?> MettreEnPauseAsync(int parentId, CancellationToken ct = default);

        Task<EtatQuota?> ReprendreAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Arrête la reconduction tacite. L'accès continue jusqu'à la fin de la
        /// période en cours : elle est déjà payée.
        /// </summary>
        Task<EtatQuota?> ResilierAsync(int parentId, CancellationToken ct = default);

        /// <summary>Revient sur une résiliation, tant que la période court.</summary>
        Task<EtatQuota?> AnnulerResiliationAsync(int parentId, CancellationToken ct = default);

        // ------------------------------------------------------------ recharge
        /// <summary>
        /// Crédite un pack d'heures sur la période en cours. Null si le code est
        /// inconnu ou le parent sans abonnement.
        /// </summary>
        Task<EtatQuota?> RechargerAsync(
            int parentId, string codeRecharge, CancellationToken ct = default);

        /// <summary>
        /// Crédite un pack APRÈS encaissement, une seule fois par paiement.
        ///
        /// Null si le pack est inconnu, si le parent n'a pas d'abonnement, ou
        /// si cette session a DÉJÀ été créditée — l'appelant ne peut pas
        /// distinguer les trois, et n'a pas à le faire : dans les trois cas il
        /// n'y a rien à ajouter.
        ///
        /// POURQUOI UNE SECONDE MÉTHODE PLUTÔT QU'UN PARAMÈTRE OPTIONNEL.
        /// Celle-ci est la seule appelable depuis un webhook, et la seule qui
        /// exige une session. Un paramètre facultatif aurait permis d'oublier
        /// de le passer — et l'oubli aurait rendu le double crédit possible
        /// sans que rien ne le signale.
        /// </summary>
        Task<EtatQuota?> RechargerApresPaiementAsync(
            int parentId, string codeRecharge, string sessionStripeId,
            string? paiementStripeId, CancellationToken ct = default);

        /// <summary>
        /// Offre des heures, sans encaissement, une seule fois par clé.
        /// </summary>
        /// <param name="cleUnicite">
        /// Ce qui garantit qu'on n'offre pas deux fois. Pour l'offre de
        /// lancement, la session de paiement qui a ouvert l'abonnement —
        /// Stripe réémet tout événement dont il n'a pas eu de 200.
        /// </param>
        /// <param name="motif">Ce qui apparaît dans l'historique du parent.</param>
        /// <remarks>
        /// SÉPARÉE DE `RechargerApresPaiementAsync`, ET CE N'EST PAS UN
        /// DOUBLON. Celle-là enregistre le prix du catalogue, parce que le
        /// parent l'a payé ; celle-ci enregistre ZÉRO. Les fondre en une
        /// seule méthode avec un booléen aurait laissé le choix du prix à
        /// l'appelant — et le premier qui l'oublie gonfle le chiffre
        /// d'affaires de l'administration sans que rien ne le signale.
        ///
        /// DES MINUTES ET NON UN CODE DE PACK. Un cadeau n'a rien à voir avec
        /// le catalogue des heures vendues : accrocher les deux obligerait à
        /// créer un produit Stripe pour offrir, et rendrait l'offre
        /// dépendante d'un pack qu'on pourrait désactiver sans y penser.
        /// </remarks>
        Task<EtatQuota?> OffrirMinutesAsync(
            int parentId, int minutes, string cleUnicite, string motif,
            CancellationToken ct = default);

        /// <summary>
        /// Reprend les heures d'une recharge remboursée.
        ///
        /// La ligne n'est pas effacée mais datée : le quota cesse de la
        /// compter, et l'achat reste visible dans l'historique — sans quoi une
        /// réclamation deviendrait impossible à instruire.
        ///
        /// Faux si ce paiement n'est pas une recharge (un abonnement
        /// remboursé passe par le même événement) ou s'il a déjà été repris.
        /// </summary>
        /// <summary>
        /// Ajoute ou retire des heures à la main, sur la période en cours.
        ///
        /// Le geste de l'exploitant : dédommager, corriger une erreur, solder
        /// un remboursement partiel. Enregistré comme une recharge à prix
        /// zéro, avec son motif — une ligne sans explication devient six mois
        /// plus tard un cadeau que personne ne s'explique.
        ///
        /// Null si le parent n'a pas d'abonnement, si le motif est vide, ou si
        /// le retrait ferait passer les heures SUPPLÉMENTAIRES sous zéro : le
        /// forfait de la formule n'est jamais entamé, il est facturé.
        /// </summary>
        /// <summary>
        /// L'historique du pot d'heures supplémentaires : achats payés et
        /// ajustements manuels, avec leur motif.
        ///
        /// C'est ce qu'on relit quand un parent conteste. Tous les abonnements
        /// du compte, pas seulement le courant.
        /// </summary>
        Task<IEnumerable<LigneHeures>> HistoriqueHeuresAsync(
            int parentId, CancellationToken ct = default);

        Task<EtatQuota?> AjusterHeuresAsync(
            int parentId, int minutes, string motif, CancellationToken ct = default);

        Task<bool> RembourserRechargeAsync(
            string paiementStripeId, CancellationToken ct = default);
    }
}
