using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Stripe;
using Stripe.Checkout;

namespace SchoolWebApp.Api.Services.Paiement
{
    /// <summary>Ce qu'il faut renvoyer au navigateur pour aller payer.</summary>
    public record OuvertureCaisse(string Url);

    public interface ICaisseStripeService
    {
        /// <summary>
        /// Ouvre une page de paiement pour cette formule. Null si la formule
        /// est inconnue ou n'a pas de tarif chez Stripe.
        /// </summary>
        Task<OuvertureCaisse?> OuvrirAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default);

        /// <summary>
        /// Ouvre une page de paiement pour un pack d'heures. Null si le pack
        /// est inconnu ou n'a pas de tarif chez Stripe.
        ///
        /// PAIEMENT UNIQUE, PAS ABONNEMENT. Une recharge s'achète une fois :
        /// elle ne se reconduit pas, ne crée pas de client récurrent, et ne
        /// doit surtout pas apparaître dans le portail de résiliation à côté
        /// de la formule mensuelle.
        /// </summary>
        Task<OuvertureCaisse?> OuvrirRechargeAsync(
            int parentId, string codePack, CancellationToken ct = default);

        /// <summary>
        /// Ouvre le portail client : changer de carte, lire ses factures,
        /// résilier. Null si le parent n'a jamais payé — il n'a alors pas de
        /// client chez Stripe, donc rien à y consulter.
        /// </summary>
        Task<OuvertureCaisse?> OuvrirPortailAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Coupe tout prélèvement à venir pour ce parent, sur-le-champ.
        ///
        /// Appelé avant d'effacer un compte : sans ça, l'abonnement
        /// continuerait de vivre chez Stripe et de débiter la carte tous les
        /// mois — pour un compte qui n'existe plus, et un webhook qui ne
        /// trouverait plus personne à créditer. Le pire cas d'un paiement en
        /// ligne : on encaisse et on ne rend rien.
        /// </summary>
        Task CouperLesPrelevementsAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Ouvre l'abonnement d'un paiement que le webhook n'a pas rapporté.
        /// Idempotente : rend null s'il n'y avait rien à rattraper.
        /// </summary>
        Task<EtatQuota?> RattraperAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Change la formule d'un abonnement Stripe EXISTANT, sans repasser par
        /// la caisse. Null si le parent n'a pas d'abonnement Stripe en cours —
        /// l'appelant doit alors ouvrir une caisse.
        /// </summary>
        Task<EtatQuota?> ChangerDeFormuleAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default);

        /// <summary>
        /// Annule une descente de gamme programmée, chez Stripe et chez nous.
        /// </summary>
        Task<EtatQuota?> AnnulerLeChangementAsync(int parentId, CancellationToken ct = default);

        /// <summary>
        /// Arrête la reconduction chez Stripe. L'abonnement court jusqu'au bout
        /// de la période payée, puis s'éteint de lui-même.
        /// </summary>
        Task ArreterLaReconductionAsync(int parentId, CancellationToken ct = default);

        /// <summary>Relance la reconduction : le parent est revenu sur sa résiliation.</summary>
        Task ReprendreLaReconductionAsync(int parentId, CancellationToken ct = default);
    }

    /// <summary>
    /// Le tunnel de paiement, côté serveur.
    ///
    /// POURQUOI CHECKOUT ET NON UN FORMULAIRE DE CARTE À NOUS
    /// -----------------------------------------------------
    /// Un champ « numéro de carte » sur notre domaine ferait transiter le
    /// numéro par notre serveur, et nous ferait entrer dans le périmètre PCI —
    /// un audit annuel pour vendre des cours de maths. La page hébergée de
    /// Stripe évite tout cela : la carte ne nous touche jamais. Elle apporte
    /// en prime le 3D Secure, Apple Pay, les traductions et les mises en
    /// conformité à venir, gratuitement.
    ///
    /// CE QUI N'EST PAS DÉCIDÉ ICI
    /// --------------------------
    /// Cette classe n'ouvre aucun abonnement. Elle mène à la caisse, rien de
    /// plus — c'est le webhook, et lui seul, qui ouvre les droits quand Stripe
    /// confirme l'encaissement. La différence n'est pas théorique : le retour
    /// du navigateur est une URL que n'importe qui peut appeler à la main, et
    /// un parent qui ferme l'onglet trop tôt ne repasse jamais par elle.
    /// </summary>
    public class CaisseStripeService : ICaisseStripeService
    {
        private readonly IAbonnementRepository _abonnements;
        private readonly IParentRepository _parents;
        private readonly IOptions<OptionsStripe> _options;
        private readonly ILogger<CaisseStripeService> _logger;

        public CaisseStripeService(
            IAbonnementRepository abonnements,
            IParentRepository parents,
            IOptions<OptionsStripe> options,
            ILogger<CaisseStripeService> logger)
        {
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _parents = parents ?? throw new ArgumentNullException(nameof(parents));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OuvertureCaisse?> OuvrirAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default)
        {
            var reglages = _options.Value;
            if (!reglages.EstConfigure) throw new InvalidOperationException("Aucune clé Stripe configurée.");

            var offre = (await _abonnements.GetOffresAsync(ct))
                .FirstOrDefault(o => string.Equals(o.Code, codeOffre, StringComparison.OrdinalIgnoreCase));

            if (offre is null) return null;

            var annuel = PeriodiciteAbonnement.EstAnnuel(periodicite);
            var prixId = annuel ? offre.StripePrixAnnuelId : offre.StripePrixMensuelId;

            // Un tarif absent n'est pas une erreur du parent : c'est le
            // catalogue qui n'a pas été poussé chez Stripe. On le dit dans les
            // logs de l'exploitant, pas au visiteur, qui n'y peut rien.
            if (string.IsNullOrWhiteSpace(prixId))
            {
                _logger.LogError(
                    "Formule {Offre} ({Rythme}) sans tarif Stripe : catalogue non synchronise.",
                    codeOffre, annuel ? "annuel" : "mensuel");
                return null;
            }

            var client = new StripeClient(reglages.CleSecrete);
            var clientId = await AssurerClientAsync(client, parentId, ct);

            var session = await new SessionService(client).CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "subscription",
                    Customer = clientId,
                    LineItems = [new SessionLineItemOptions { Price = prixId, Quantity = 1 }],

                    SuccessUrl = $"{reglages.UrlRetour}?paiement=ok",
                    CancelUrl = $"{reglages.UrlAnnulation}?paiement=abandon",

                    // CE QUE LE WEBHOOK LIRA.
                    //
                    // Porté par la session ET par l'abonnement créé derrière.
                    // Sans ça il faudrait, à chaque événement, remonter du
                    // tarif Stripe jusqu'à la formule — un aller-retour de
                    // plus, et une correspondance à maintenir des deux côtés.
                    Metadata = Etiquettes(parentId, offre.Code!, annuel),
                    SubscriptionData = new SessionSubscriptionDataOptions
                    {
                        Metadata = Etiquettes(parentId, offre.Code!, annuel),
                    },
                },
                cancellationToken: ct);

            _logger.LogInformation(
                "Caisse ouverte pour le parent {ParentId} : {Offre} {Rythme}, session {Session}.",
                parentId, offre.Code, annuel ? "annuel" : "mensuel", session.Id);

            return new OuvertureCaisse(session.Url);
        }

        public async Task<OuvertureCaisse?> OuvrirRechargeAsync(
            int parentId, string codePack, CancellationToken ct = default)
        {
            var reglages = _options.Value;
            if (!reglages.EstConfigure) throw new InvalidOperationException("Aucune clé Stripe configurée.");

            var pack = (await _abonnements.GetOffresRechargeAsync(ct))
                .FirstOrDefault(o => string.Equals(o.Code, codePack, StringComparison.OrdinalIgnoreCase));

            if (pack is null) return null;

            // Un tarif absent n'est pas une erreur du parent : c'est le
            // catalogue qui n'a pas été poussé chez Stripe.
            if (string.IsNullOrWhiteSpace(pack.StripePrixId))
            {
                _logger.LogError(
                    "Pack {Pack} sans tarif Stripe : catalogue non synchronise.", codePack);
                return null;
            }

            var client = new StripeClient(reglages.CleSecrete);
            var clientId = await AssurerClientAsync(client, parentId, ct);

            var etiquettes = new Dictionary<string, string>
            {
                [EtiquetteParent] = parentId.ToString(),
                [EtiquetteOffre] = pack.Code!,

                // CE QUI DISTINGUE UNE RECHARGE D'UNE FORMULE POUR LE WEBHOOK.
                //
                // Les deux arrivent par `checkout.session.completed`, avec les
                // mêmes étiquettes de parent et d'offre. Sans ce drapeau, une
                // recharge serait lue comme une souscription : le webhook
                // chercherait une formule portant le code du pack, ne la
                // trouverait pas, et le parent aurait payé sans rien recevoir.
                [EtiquetteType] = TypeRecharge,
            };

            var session = await new SessionService(client).CreateAsync(
                new SessionCreateOptions
                {
                    // `payment` et non `subscription` : un pack d'heures ne se
                    // reconduit pas. En faire un abonnement le ferait
                    // reprélever tous les mois.
                    Mode = "payment",
                    Customer = clientId,
                    LineItems = [new SessionLineItemOptions { Price = pack.StripePrixId, Quantity = 1 }],

                    SuccessUrl = $"{reglages.UrlRetour}?paiement=ok",
                    CancelUrl = $"{reglages.UrlAnnulation}?paiement=abandon",

                    Metadata = etiquettes,

                    // La facture porte les mêmes étiquettes : c'est elle qu'on
                    // relit quand un parent écrit à propos d'un débit.
                    PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = etiquettes },
                },
                cancellationToken: ct);

            _logger.LogInformation(
                "Caisse ouverte pour le parent {ParentId} : recharge {Pack}, session {Session}.",
                parentId, pack.Code, session.Id);

            return new OuvertureCaisse(session.Url);
        }

        public async Task<OuvertureCaisse?> OuvrirPortailAsync(
            int parentId, CancellationToken ct = default)
        {
            var reglages = _options.Value;
            if (!reglages.EstConfigure) throw new InvalidOperationException("Aucune clé Stripe configurée.");

            var parent = await _parents.GetParentByIdAsync(parentId);
            if (string.IsNullOrWhiteSpace(parent?.StripeClientId)) return null;

            var session = await new Stripe.BillingPortal.SessionService(new StripeClient(reglages.CleSecrete))
                .CreateAsync(
                    new Stripe.BillingPortal.SessionCreateOptions
                    {
                        Customer = parent.StripeClientId,
                        ReturnUrl = reglages.UrlRetour,
                    },
                    cancellationToken: ct);

            return new OuvertureCaisse(session.Url);
        }

        /// <summary>
        /// Change la formule d'un abonnement déjà en cours.
        ///
        /// LE DÉFAUT QUE ÇA CORRIGE
        /// -----------------------
        /// Sans elle, « passer à cette formule » rouvrait une caisse, donc un
        /// SECOND abonnement chez Stripe. L'ancien continuait de vivre : deux
        /// prélèvements par mois, pendant que notre base — qui clôt bien
        /// l'ancienne ligne — affichait un seul abonnement. Le parent payait
        /// deux fois sans que rien ne le montre nulle part.
        ///
        /// Chez Stripe, changer de formule n'est pas un nouvel abonnement :
        /// c'est le même objet dont on remplace l'article. La carte reste
        /// enregistrée, le cycle de facturation ne bouge pas, et il n'y a
        /// aucune page de paiement à repasser.
        ///
        /// LA PRORATISATION
        /// ---------------
        /// `create_prorations` : Stripe calcule la différence au prorata des
        /// jours restants et la porte sur la PROCHAINE facture — en plus pour
        /// une montée en gamme, en crédit pour une descente. Pas de
        /// prélèvement immédiat, donc pas de carte qui peut être refusée au
        /// moment précis où le parent clique. Le changement prend effet tout
        /// de suite dans les deux cas ; c'est le règlement qui attend le
        /// prochain cycle.
        /// </summary>
        public async Task<EtatQuota?> ChangerDeFormuleAsync(
            int parentId, string codeOffre, string periodicite, CancellationToken ct = default)
        {
            var reglages = _options.Value;
            if (!reglages.EstConfigure) return null;

            var identifiant = await _abonnements.AbonnementStripeCourantAsync(parentId, ct);
            if (string.IsNullOrWhiteSpace(identifiant)) return null;

            var offres = (await _abonnements.GetOffresAsync(ct)).ToList();

            var offre = offres.FirstOrDefault(
                o => string.Equals(o.Code, codeOffre, StringComparison.OrdinalIgnoreCase));

            if (offre is null) return null;

            var annuel = PeriodiciteAbonnement.EstAnnuel(periodicite);
            var prixId = annuel ? offre.StripePrixAnnuelId : offre.StripePrixMensuelId;

            if (string.IsNullOrWhiteSpace(prixId))
            {
                _logger.LogError(
                    "Formule {Offre} ({Rythme}) sans tarif Stripe : changement impossible.",
                    codeOffre, annuel ? "annuel" : "mensuel");
                return null;
            }

            var client = new StripeClient(reglages.CleSecrete);

            // MONTÉE OU DESCENTE ? LA RÉPONSE CHANGE TOUT.
            //
            // Comparaison sur le prix MENSUEL des deux formules, jamais sur le
            // montant du tarif visé : passer de Solo mensuel (39,90) à Solo
            // annuel (438,90) n'est pas une montée en gamme, c'est le même
            // service payé autrement. La gamme se lit sur la formule seule ;
            // le rythme, lui, s'applique toujours immédiatement.
            var actuel = await _abonnements.GetEtatQuotaAsync(parentId, ct);

            var offreActuelle = offres.FirstOrDefault(
                o => string.Equals(o.Code, actuel?.OffreCode, StringComparison.OrdinalIgnoreCase));

            var descente = offreActuelle is not null
                           && offre.PrixMensuelCentimes < offreActuelle.PrixMensuelCentimes;

            if (descente)
            {
                return await ProgrammerLaDescenteAsync(
                    client, identifiant, parentId, offre, annuel, periodicite, ct);
            }

            // Une montée en gamme annule une descente qui était programmée :
            // le parent a changé d'avis, et le calendrier chez Stripe doit
            // partir avec, sinon il rabaisserait la formule au renouvellement.
            if (actuel?.ChangementPrevuLe is not null)
            {
                await LibererLeCalendrierAsync(client, identifiant, ct);
                await _abonnements.AnnulerChangementPrevuAsync(parentId, ct);
            }

            var service = new SubscriptionService(client);

            var abonnement = await service.GetAsync(identifiant, cancellationToken: ct);

            // On REMPLACE l'article existant au lieu d'en ajouter un : sans son
            // identifiant, Stripe empilerait les deux formules sur le même
            // abonnement, et facturerait la somme des deux.
            var article = abonnement.Items?.Data?.FirstOrDefault();

            if (article is null)
            {
                _logger.LogError(
                    "Abonnement Stripe {Sub} sans article : changement impossible.", identifiant);
                return null;
            }

            await service.UpdateAsync(
                identifiant,
                new SubscriptionUpdateOptions
                {
                    Items =
                    [
                        new SubscriptionItemOptions { Id = article.Id, Price = prixId },
                    ],
                    ProrationBehavior = "create_prorations",

                    // Une résiliation programmée est levée : demander une autre
                    // formule, c'est vouloir continuer. La laisser courir
                    // couperait l'abonnement qu'on vient de choisir.
                    CancelAtPeriodEnd = false,

                    // Les étiquettes suivent la nouvelle formule, sinon le
                    // rattrapage et les webhooks rouvriraient l'ancienne.
                    Metadata = Etiquettes(parentId, offre.Code!, annuel),
                },
                cancellationToken: ct);

            _logger.LogWarning(
                "Formule changee pour le parent {ParentId} : {Offre} {Rythme} "
                + "sur l'abonnement Stripe {Sub}.",
                parentId, offre.Code, annuel ? "annuel" : "mensuel", identifiant);

            return await _abonnements.ChangerDeFormuleAsync(parentId, codeOffre, periodicite, ct);
        }

        /// <summary>
        /// Programme une descente de gamme pour le prochain renouvellement.
        ///
        /// POURQUOI PAS TOUT DE SUITE
        /// -------------------------
        /// Le pot d'heures suit la formule. Descendre de Duo (16 h) à Solo
        /// (9 h) le 20 du mois, après en avoir consommé 12, ferait tomber le
        /// solde à zéro sur-le-champ — le parent perdrait des heures qu'il a
        /// déjà payées. Il garde donc ce qu'il a acheté jusqu'au bout de la
        /// période, et la nouvelle formule prend le relais ensuite.
        ///
        /// POURQUOI UN CALENDRIER CHEZ STRIPE
        /// ---------------------------------
        /// Un simple drapeau chez nous ne suffirait pas : au renouvellement,
        /// Stripe facturerait encore l'ancien tarif, et le parent paierait Duo
        /// pour du Solo. Le calendrier décrit les deux phases — l'actuelle
        /// jusqu'à l'échéance, la nouvelle après — et Stripe facture en
        /// conséquence, sans que nous ayons à intervenir le jour dit.
        ///
        /// `release` en fin de calendrier : l'abonnement redevient un
        /// abonnement ordinaire une fois la bascule faite, au lieu de rester
        /// piloté par un calendrier qu'il faudrait défaire au changement
        /// suivant.
        /// </summary>
        private async Task<EtatQuota?> ProgrammerLaDescenteAsync(
            StripeClient client,
            string identifiant,
            int parentId,
            OffreTarif offre,
            bool annuel,
            string periodicite,
            CancellationToken ct)
        {
            var prixId = annuel ? offre.StripePrixAnnuelId : offre.StripePrixMensuelId;
            var calendriers = new SubscriptionScheduleService(client);

            // Un calendrier existe peut-être déjà — le parent change d'avis sur
            // sa descente. On repart du sien plutôt que d'en créer un second,
            // que Stripe refuserait.
            var abonnement = await new SubscriptionService(client)
                .GetAsync(identifiant, cancellationToken: ct);

            var calendrier = string.IsNullOrWhiteSpace(abonnement.ScheduleId)
                ? await calendriers.CreateAsync(
                    new SubscriptionScheduleCreateOptions { FromSubscription = identifiant },
                    cancellationToken: ct)
                : await calendriers.GetAsync(abonnement.ScheduleId, cancellationToken: ct);

            // La phase en cours est reprise TELLE QUELLE : ses dates et son
            // tarif sont ceux que le parent a payés, on n'y touche pas.
            var phase = calendrier.Phases[^1];

            await calendriers.UpdateAsync(
                calendrier.Id,
                new SubscriptionScheduleUpdateOptions
                {
                    EndBehavior = "release",
                    Phases =
                    [
                        new SubscriptionSchedulePhaseOptions
                        {
                            // `PriceId` et non `Price.Id` : sur un calendrier,
                            // Stripe ne développe pas l'objet tarif — il n'en
                            // rend que la référence, et lire `.Id` dessus
                            // casse. Le repli couvre le cas inverse, quand
                            // l'objet est bien là.
                            Items = phase.Items
                                .Select(i => new SubscriptionSchedulePhaseItemOptions
                                {
                                    Price = i.PriceId ?? i.Price?.Id,
                                    Quantity = i.Quantity,
                                })
                                .ToList(),
                            StartDate = phase.StartDate,
                            EndDate = phase.EndDate,
                        },
                        new SubscriptionSchedulePhaseOptions
                        {
                            Items =
                            [
                                new SubscriptionSchedulePhaseItemOptions { Price = prixId, Quantity = 1 },
                            ],

                            // Une durée d'un cycle, puis `release` rend
                            // l'abonnement à lui-même — il continue sur ce
                            // tarif, sans calendrier à défaire au changement
                            // suivant. Une phase finale sans durée serait
                            // refusée par Stripe.
                            Duration = new SubscriptionSchedulePhaseDurationOptions
                            {
                                Interval = annuel ? "year" : "month",
                                IntervalCount = 1,
                            },

                            // Aucun prorata : la bascule tombe pile sur la
                            // frontière de période, il n'y a rien à répartir.
                            ProrationBehavior = "none",
                            Metadata = Etiquettes(parentId, offre.Code!, annuel),
                        },
                    ],
                },
                cancellationToken: ct);

            _logger.LogWarning(
                "Descente de gamme PROGRAMMEE pour le parent {ParentId} : {Offre} {Rythme} "
                + "au prochain renouvellement (abonnement {Sub}).",
                parentId, offre.Code, annuel ? "annuel" : "mensuel", identifiant);

            return await _abonnements.PlanifierChangementAsync(parentId, offre.Code!, periodicite, ct);
        }

        /// <summary>
        /// Détache le calendrier de l'abonnement, qui reprend sa vie ordinaire.
        /// Appelé quand une descente programmée est annulée — par une montée en
        /// gamme, ou par le parent lui-même.
        /// </summary>
        private async Task LibererLeCalendrierAsync(
            StripeClient client, string identifiant, CancellationToken ct)
        {
            var abonnement = await new SubscriptionService(client)
                .GetAsync(identifiant, cancellationToken: ct);

            if (string.IsNullOrWhiteSpace(abonnement.ScheduleId)) return;

            try
            {
                await new SubscriptionScheduleService(client)
                    .ReleaseAsync(abonnement.ScheduleId, cancellationToken: ct);
            }
            catch (StripeException ex)
            {
                // Déjà libéré ou terminé : la bascule a eu lieu entre-temps.
                _logger.LogWarning(ex,
                    "Liberation sans effet du calendrier {Calendrier}.", abonnement.ScheduleId);
            }
        }

        /// <summary>
        /// Annule une descente de gamme programmée : chez Stripe d'abord, puis
        /// chez nous. Rend l'état à jour, ou null s'il n'y avait rien à annuler.
        /// </summary>
        public async Task<EtatQuota?> AnnulerLeChangementAsync(
            int parentId, CancellationToken ct = default)
        {
            var reglages = _options.Value;

            var identifiant = await _abonnements.AbonnementStripeCourantAsync(parentId, ct);

            if (reglages.EstConfigure && !string.IsNullOrWhiteSpace(identifiant))
            {
                await LibererLeCalendrierAsync(new StripeClient(reglages.CleSecrete), identifiant, ct);
            }

            return await _abonnements.AnnulerChangementPrevuAsync(parentId, ct);
        }

        /// <summary>
        /// Arrête la reconduction chez Stripe.
        ///
        /// LE DÉFAUT QUE ÇA CORRIGE
        /// -----------------------
        /// La résiliation ne posait qu'une date dans NOTRE base. L'accès
        /// s'arrêtait bien à la fin de la période — et Stripe continuait de
        /// prélever, tous les mois, indéfiniment. Le parent avait résilié, il
        /// n'avait plus rien, et il payait toujours. C'est la faute la plus
        /// coûteuse qu'un paiement en ligne puisse commettre : elle se répare
        /// en remboursements, en réclamations, et en réputation.
        ///
        /// POURQUOI `cancel_at_period_end` ET NON UNE RÉSILIATION SÈCHE
        /// ----------------------------------------------------------
        /// La période est payée : le parent la garde jusqu'au bout, c'est ce
        /// que l'écran lui promet. Stripe a exactement cette notion — il cesse
        /// de reconduire et laisse courir. Une résiliation immédiate lui
        /// retirerait un mois qu'il a réglé.
        ///
        /// Un échec ne remonte pas : la résiliation locale a déjà eu lieu, et
        /// refuser l'opération laisserait le parent croire qu'il est toujours
        /// abonné. Le cri va dans les logs, où il est rattrapable.
        /// </summary>
        public Task ArreterLaReconductionAsync(int parentId, CancellationToken ct = default) =>
            ReglerLaReconductionAsync(parentId, arreter: true, ct);

        public Task ReprendreLaReconductionAsync(int parentId, CancellationToken ct = default) =>
            ReglerLaReconductionAsync(parentId, arreter: false, ct);

        private async Task ReglerLaReconductionAsync(
            int parentId, bool arreter, CancellationToken ct)
        {
            var reglages = _options.Value;
            if (!reglages.EstConfigure) return;

            var identifiant = await _abonnements.AbonnementStripeCourantAsync(parentId, ct);

            // Pas d'abonnement Stripe : essai gratuit, ou compte exempté. Il
            // n'y a rien à arrêter, et ce n'est pas une anomalie.
            if (string.IsNullOrWhiteSpace(identifiant)) return;

            var client = new StripeClient(reglages.CleSecrete);

            try
            {
                // UN CALENDRIER PRIME SUR L'ABONNEMENT.
                //
                // Si une descente de gamme est programmée, le calendrier
                // continue de piloter l'abonnement et défait ce qu'on lui
                // demande. On le libère d'abord : quelqu'un qui résilie ne veut
                // plus d'une formule de rechange le mois prochain.
                if (arreter) await LibererLeCalendrierAsync(client, identifiant, ct);

                await new SubscriptionService(client).UpdateAsync(
                    identifiant,
                    new SubscriptionUpdateOptions { CancelAtPeriodEnd = arreter },
                    cancellationToken: ct);

                _logger.LogWarning(
                    arreter
                        ? "Reconduction ARRETEE chez Stripe pour le parent {ParentId} ({Sub})."
                        : "Reconduction relancee chez Stripe pour le parent {ParentId} ({Sub}).",
                    parentId, identifiant);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    arreter
                        ? "ECHEC de l'arret de reconduction chez Stripe pour le parent {ParentId} "
                          + "({Sub}) : LE PRELEVEMENT CONTINUERA. A reprendre a la main."
                        : "Echec de la relance de reconduction pour le parent {ParentId} ({Sub}).",
                    parentId, identifiant);
            }
        }

        /// <summary>
        /// Rattrape un paiement dont le webhook n'est pas arrivé.
        ///
        /// POURQUOI IL FAUT LES DEUX CHEMINS
        /// --------------------------------
        /// Le webhook est le chemin fiable — il vient de Stripe, il est signé,
        /// il arrive même si le navigateur est fermé. Mais il peut se perdre :
        /// tunnel de développement éteint, coupure réseau, redémarrage au
        /// mauvais moment, incident chez Stripe. Quand ça arrive, le parent a
        /// payé et ne reçoit rien — le pire état possible, et il n'a aucun
        /// moyen de s'en sortir seul.
        ///
        /// Cette méthode est le second chemin : au retour du navigateur, on
        /// demande à Stripe ce qu'il a encaissé, et on ouvre ce qui manque.
        /// Elle ne remplace pas le webhook — elle ne se déclenche que si
        /// quelqu'un revient sur la page — mais elle couvre le cas de très
        /// loin le plus fréquent, celui du parent qui vient de payer.
        ///
        /// IDEMPOTENTE : un abonnement déjà enregistré est ignoré. Les deux
        /// chemins peuvent donc arriver dans n'importe quel ordre, ou tous les
        /// deux, sans jamais ouvrir deux fois.
        ///
        /// Rend l'état du quota quand quelque chose a été rattrapé, null quand
        /// il n'y avait rien à rattraper.
        /// </summary>
        public async Task<EtatQuota?> RattraperAsync(
            int parentId, CancellationToken ct = default)
        {
            var reglages = _options.Value;
            if (!reglages.EstConfigure) return null;

            var parent = await _parents.GetParentByIdAsync(parentId);
            if (string.IsNullOrWhiteSpace(parent?.StripeClientId)) return null;

            var connus = new HashSet<string>(
                await _abonnements.AbonnementsStripeAsync(parentId, ct));

            StripeList<Subscription> chezStripe;

            try
            {
                chezStripe = await new SubscriptionService(new StripeClient(reglages.CleSecrete))
                    .ListAsync(
                        new SubscriptionListOptions
                        {
                            Customer = parent.StripeClientId,
                            Status = "active",
                            Limit = 10,
                        },
                        cancellationToken: ct);
            }
            catch (StripeException ex)
            {
                // Stripe injoignable : le webhook reste le chemin principal, il
                // rattrapera. On ne fait pas échouer l'affichage d'une page
                // pour ça.
                _logger.LogWarning(ex,
                    "Rattrapage impossible pour le parent {ParentId} : Stripe injoignable.",
                    parentId);
                return null;
            }

            // Du plus récent au plus ancien : si plusieurs abonnements
            // traînaient, c'est celui qui vient d'être payé qui compte.
            foreach (var abonnement in chezStripe.Data.OrderByDescending(a => a.Created))
            {
                if (connus.Contains(abonnement.Id)) continue;

                var code = Etiquette(abonnement.Metadata, EtiquetteOffre);

                if (string.IsNullOrWhiteSpace(code))
                {
                    // Sans étiquette, impossible de savoir quelle formule
                    // ouvrir. Ça ne devrait pas arriver — nous les posons à la
                    // création — mais un abonnement fabriqué à la main dans le
                    // tableau de bord n'en aurait pas.
                    _logger.LogWarning(
                        "Abonnement Stripe {Sub} sans etiquette d'offre : rattrapage impossible.",
                        abonnement.Id);
                    continue;
                }

                EtatQuota? etat;

                try
                {
                    etat = await _abonnements.SouscrireAsync(
                        parentId,
                        code,
                        Etiquette(abonnement.Metadata, EtiquettePeriodicite)
                            ?? PeriodiciteAbonnement.Mensuel,
                        abonnement.Id,
                        ct);
                }
                catch (DbUpdateException)
                {
                    // QUELQU'UN A RATTRAPÉ LE MÊME PAIEMENT ENTRE-TEMPS.
                    //
                    // `stripe_abonnement_id` porte un index UNIQUE : deux
                    // rattrapages du même paiement lancés en parallèle passent
                    // tous deux le test `connus.Contains`, puisque ni l'un ni
                    // l'autre n'a encore écrit. Le second se heurte alors à
                    // l'index.
                    //
                    // Ça arrive pour de bon. En développement, `StrictMode`
                    // monte les effets DEUX FOIS et déclenche donc deux
                    // rattrapages simultanés au retour de la caisse. En
                    // production, il suffit de deux onglets ouverts sur la page
                    // de retour, ou d'un F5 pendant que le premier appel court.
                    //
                    // Et l'index a raison : il n'y a bien qu'un abonnement. Le
                    // perdant de la course n'a rien à réparer, il a juste à
                    // lire ce que l'autre vient d'écrire. Rendre une erreur
                    // afficherait « Votre forfait n'a pas pu être chargé » sur
                    // un forfait parfaitement ouvert — le message le plus
                    // inquiétant possible, au pire moment : juste après un
                    // paiement.
                    _logger.LogInformation(
                        "Rattrapage concurrent sur l'abonnement {Sub} du parent {ParentId} : "
                        + "l'autre appel a gagne, on lit son resultat.",
                        abonnement.Id, parentId);

                    return await _abonnements.GetEtatQuotaAsync(parentId, ct);
                }

                _logger.LogWarning(
                    "Paiement RATTRAPE au retour du navigateur : {Offre} pour le parent "
                    + "{ParentId} ({Sub}). Le webhook n'etait pas passe.",
                    code, parentId, abonnement.Id);

                return etat;
            }

            return null;
        }

        private static string? Etiquette(IDictionary<string, string>? etiquettes, string cle) =>
            etiquettes is not null && etiquettes.TryGetValue(cle, out var valeur) ? valeur : null;

        public async Task CouperLesPrelevementsAsync(
            int parentId, CancellationToken ct = default)
        {
            var reglages = _options.Value;

            // Pas de clé : il n'y a jamais eu de prélèvement à couper. Ce n'est
            // pas une raison d'empêcher la suppression du compte.
            if (!reglages.EstConfigure) return;

            var stripe = new StripeClient(reglages.CleSecrete);
            var abonnements = new SubscriptionService(stripe);

            // CE QU'IL FAUT COUPER SE DEMANDE À STRIPE, PAS À NOUS.
            //
            // Nos propres enregistrements ne suffisent pas, et la fenêtre où
            // ils mentent est précisément la plus dangereuse : entre le
            // paiement et le traitement du webhook, l'abonnement existe chez
            // Stripe et pas encore chez nous. Un parent qui paie puis se
            // ravise dans la minute — ce qui arrive — verrait son compte
            // effacé et son prélèvement courir, sans plus aucune trace pour le
            // relier à lui. C'est l'abonnement orphelin, dans sa forme la plus
            // désagréable : on encaisse et il n'y a plus personne à créditer.
            //
            // Le client Stripe, lui, existe dès le passage en caisse — il est
            // créé avant la session de paiement. Interroger par le client
            // attrape donc ce que le webhook n'a pas encore rapporté.
            var aResilier = new HashSet<string>(
                await _abonnements.AbonnementsStripeAsync(parentId, ct));

            var parent = await _parents.GetParentByIdAsync(parentId);

            if (!string.IsNullOrWhiteSpace(parent?.StripeClientId))
            {
                try
                {
                    var chezStripe = await abonnements.ListAsync(
                        new SubscriptionListOptions
                        {
                            Customer = parent.StripeClientId,
                            Status = "all",

                            // Cent : un parent n'a qu'un abonnement à la fois,
                            // et il faudrait des années de va-et-vient pour en
                            // accumuler autant. Pas de pagination pour un cas
                            // qui n'arrivera pas.
                            Limit = 100,
                        },
                        cancellationToken: ct);

                    foreach (var abonnement in chezStripe.Data)
                    {
                        // Déjà terminés : les rappeler ferait lever une
                        // exception pour rien.
                        if (abonnement.Status is "canceled" or "incomplete_expired") continue;

                        aResilier.Add(abonnement.Id);
                    }
                }
                catch (StripeException ex)
                {
                    // Stripe injoignable ne doit pas empêcher un parent de
                    // supprimer son compte — c'est un droit, pas une faveur.
                    // On se rabat sur ce qu'on connaît, et on crie : un
                    // prélèvement qui survit à un compte effacé se répare à la
                    // main, mais encore faut-il le savoir.
                    _logger.LogError(ex,
                        "Impossible de lister les abonnements Stripe du client {Client} "
                        + "(compte {ParentId}) : verifier a la main qu'aucun prelevement ne subsiste.",
                        parent.StripeClientId, parentId);
                }
            }

            foreach (var identifiant in aResilier)
            {
                try
                {
                    // Immédiat, et non « à la fin de la période » : le compte
                    // disparaît maintenant. Laisser courir un mois déjà payé
                    // n'aurait de sens que s'il restait quelqu'un pour en
                    // profiter.
                    await abonnements.CancelAsync(
                        identifiant, cancellationToken: ct);

                    _logger.LogWarning(
                        "Abonnement Stripe {Sub} resilie : suppression du compte {ParentId}.",
                        identifiant, parentId);
                }
                catch (StripeException ex)
                {
                    // Déjà résilié, ou inconnu de ce compte Stripe — le cas
                    // normal pour un abonnement ancien. On le note et on
                    // continue : un échec ici ne doit pas retenir en otage la
                    // suppression d'un compte, qui est un droit.
                    _logger.LogWarning(ex,
                        "Resiliation Stripe sans effet pour {Sub} (compte {ParentId}).",
                        identifiant, parentId);
                }
            }

            // Le client Stripe lui-même est CONSERVÉ, volontairement. Il porte
            // les factures déjà émises, que la comptabilité doit garder — une
            // obligation qui ne s'efface pas avec un compte. Il ne contient
            // plus rien qui rattache ces factures à une famille chez nous.
        }

        /// <summary>
        /// Le client Stripe du parent, créé à la première visite et réutilisé
        /// ensuite.
        ///
        /// En créer un par paiement donnerait au même foyer autant de clients
        /// que d'abonnements : la carte enregistrée et les factures resteraient
        /// sur le précédent, et le portail client n'en montrerait qu'une part.
        /// </summary>
        private async Task<string> AssurerClientAsync(
            StripeClient client, int parentId, CancellationToken ct)
        {
            var parent = await _parents.GetParentByIdAsync(parentId)
                ?? throw new InvalidOperationException($"Parent {parentId} introuvable.");

            if (!string.IsNullOrWhiteSpace(parent.StripeClientId)) return parent.StripeClientId;

            var cree = await new CustomerService(client).CreateAsync(
                new CustomerCreateOptions
                {
                    Email = parent.Mail,
                    Name = $"{parent.Prenom} {parent.Nom}".Trim(),

                    // Le lien de retour, du côté Stripe : un tableau de bord
                    // qui ne montre que des `cus_…` ne permet pas de répondre
                    // à un parent qui écrit pour un problème de facture.
                    Metadata = new Dictionary<string, string>
                    {
                        [EtiquetteParent] = parentId.ToString(),
                    },
                },
                cancellationToken: ct);

            await _parents.EnregistrerClientStripeAsync(parentId, cree.Id, ct);

            _logger.LogInformation(
                "Client Stripe {Client} cree pour le parent {ParentId}.", cree.Id, parentId);

            return cree.Id;
        }

        public const string EtiquetteParent = "parent_id";
        public const string EtiquetteOffre = "offre";
        public const string EtiquettePeriodicite = "periodicite";

        /// <summary>
        /// Ce que la session vend : une formule, ou un pack d'heures.
        ///
        /// ABSENTE SUR LES FORMULES, ET C'EST VOULU. Les sessions déjà créées
        /// avant cette étiquette n'en portent pas ; les lire comme des
        /// recharges romprait les paiements en cours de route. Le webhook
        /// traite donc « absente » comme « formule ».
        /// </summary>
        public const string EtiquetteType = "type";

        /// <summary>La valeur qui marque une recharge.</summary>
        public const string TypeRecharge = "recharge";

        private static Dictionary<string, string> Etiquettes(
            int parentId, string codeOffre, bool annuel) =>
            new()
            {
                [EtiquetteParent] = parentId.ToString(),
                [EtiquetteOffre] = codeOffre,
                [EtiquettePeriodicite] = annuel
                    ? PeriodiciteAbonnement.Annuel
                    : PeriodiciteAbonnement.Mensuel,
            };
    }
}
