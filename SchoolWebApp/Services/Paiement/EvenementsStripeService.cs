using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Stripe;
using Stripe.Checkout;

namespace SchoolWebApp.Api.Services.Paiement
{
    public interface IEvenementsStripeService
    {
        /// <summary>
        /// Traite un événement Stripe. `signature` est l'en-tête
        /// `Stripe-Signature` ; sans elle, rien n'est traité.
        /// </summary>
        Task TraiterAsync(string charge, string? signature, CancellationToken ct = default);
    }

    /// <summary>
    /// Ce que Stripe nous raconte, et ce qu'on en fait.
    ///
    /// POURQUOI C'EST ICI QUE LES DROITS S'OUVRENT, ET NULLE PART AILLEURS
    /// -----------------------------------------------------------------
    /// Le retour du navigateur après paiement est une simple URL : n'importe
    /// qui peut la taper. Ouvrir l'abonnement là-bas donnerait un abonnement
    /// gratuit à qui devine l'adresse. À l'inverse, un parent qui ferme son
    /// onglet pendant la redirection n'y passe jamais — il aurait payé sans
    /// rien recevoir. Le webhook n'a aucun de ces deux défauts : il vient de
    /// Stripe, il est signé, et il arrive même si le navigateur est fermé.
    ///
    /// LA SIGNATURE EST LA SEULE CHOSE QUI PROTÈGE CETTE ROUTE
    /// -----------------------------------------------------
    /// Elle est publique — Stripe doit l'atteindre sans jeton. Sans
    /// vérification, un inconnu pourrait poster « paiement réussi » et
    /// s'ouvrir un abonnement à vie. `ConstructEvent` recalcule l'empreinte
    /// avec le secret partagé et refuse tout le reste ; il rejette aussi les
    /// événements trop anciens, ce qui interdit de rejouer un vrai paiement
    /// capturé au passage.
    ///
    /// TOUT EST REJOUABLE
    /// -----------------
    /// Stripe réémet en cas de doute — un délai de réponse, un 500, une
    /// coupure. Chaque traitement doit donc supporter d'être exécuté deux
    /// fois : on vérifie l'état avant d'agir plutôt que d'appliquer un delta.
    /// </summary>
    public class EvenementsStripeService : IEvenementsStripeService
    {
        private readonly IAbonnementRepository _abonnements;
        private readonly IOffreLancementService _lancement;
        private readonly IParentRepository _parents;
        private readonly Notifications.ITelegramService _telegram;
        private readonly SchoolWebApp.Domain.Emails.IServiceEmail _email;
        private readonly IOptions<OptionsStripe> _options;
        private readonly ILogger<EvenementsStripeService> _logger;

        public EvenementsStripeService(
            IAbonnementRepository abonnements,
            IOffreLancementService lancement,
            IParentRepository parents,
            IOptions<OptionsStripe> options,
            Notifications.ITelegramService telegram,
            SchoolWebApp.Domain.Emails.IServiceEmail email,
            ILogger<EvenementsStripeService> logger)
        {
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _lancement = lancement ?? throw new ArgumentNullException(nameof(lancement));
            _parents = parents ?? throw new ArgumentNullException(nameof(parents));
            _telegram = telegram ?? throw new ArgumentNullException(nameof(telegram));
            _email = email ?? throw new ArgumentNullException(nameof(email));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task TraiterAsync(
            string charge, string? signature, CancellationToken ct = default)
        {
            var secret = _options.Value.SecretWebhook;

            if (string.IsNullOrWhiteSpace(secret))
            {
                // On ne se rabat PAS sur une lecture non vérifiée. Un webhook
                // dont on ne peut pas prouver l'origine vaut moins que pas de
                // webhook du tout : il ouvre des abonnements sur commande.
                throw new InvalidOperationException(
                    "SecretWebhook absent : impossible de vérifier l'origine de l'événement. "
                    + "En local, lancez `stripe listen --forward-to localhost:5066/stripe/webhook` "
                    + "et recopiez le `whsec_…` affiché dans appsettings.Development.json.");
            }

            // throwOnApiVersionMismatch à faux : la version d'API du compte
            // Stripe évolue sans nous, et un écart de version n'est pas une
            // raison de refuser un paiement bien réel.
            var evenement = EventUtility.ConstructEvent(
                charge, signature, secret, throwOnApiVersionMismatch: false);

            switch (evenement.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    await OuvrirAsync((Session)evenement.Data.Object, ct);
                    break;

                case EventTypes.InvoicePaid:
                    await ReporterAsync((Invoice)evenement.Data.Object, ct);
                    break;

                case EventTypes.InvoicePaymentFailed:
                    await AlerterAsync((Invoice)evenement.Data.Object, ct);
                    break;

                case EventTypes.CustomerSubscriptionDeleted:
                    await CloreAsync((Subscription)evenement.Data.Object, ct);
                    break;

                case EventTypes.ChargeRefunded:
                    await ReprendreAsync((Charge)evenement.Data.Object, ct);
                    break;

                default:
                    // Un événement non traité n'est pas une erreur : Stripe en
                    // émet des dizaines. Répondre 200 lui dit de ne pas le
                    // réémettre, ce qui est le comportement voulu.
                    _logger.LogDebug("Evenement Stripe ignore : {Type}.", evenement.Type);
                    break;
            }
        }

        /// <summary>
        /// Le paiement est confirmé : l'abonnement s'ouvre.
        ///
        /// C'est le seul endroit du programme où un abonnement payant naît.
        /// </summary>
        private async Task OuvrirAsync(Session session, CancellationToken ct)
        {
            // `paid` et non `complete` : une session peut être « complète »
            // avec un paiement encore en attente — un virement SEPA met
            // plusieurs jours. Ouvrir là-dessus donnerait l'accès avant
            // l'argent, et `invoice.paid` viendra de toute façon ensuite.
            if (session.PaymentStatus != "paid")
            {
                _logger.LogInformation(
                    "Session {Session} complete mais paiement {Etat} : on attend.",
                    session.Id, session.PaymentStatus);
                return;
            }

            if (!Etiquette(session.Metadata, CaisseStripeService.EtiquetteParent, out var parentId))
            {
                _logger.LogError(
                    "Session {Session} sans etiquette parent : abonnement non ouvert.", session.Id);
                return;
            }

            var codeOffre = Lire(session.Metadata, CaisseStripeService.EtiquetteOffre);
            var periodicite = Lire(session.Metadata, CaisseStripeService.EtiquettePeriodicite);

            if (string.IsNullOrWhiteSpace(codeOffre))
            {
                _logger.LogError(
                    "Session {Session} sans etiquette offre : abonnement non ouvert.", session.Id);
                return;
            }

            // UNE RECHARGE OU UNE FORMULE ? LES DEUX ARRIVENT PAR ICI.
            //
            // Étiquette absente = formule : c'est le cas des sessions créées
            // avant que ce drapeau existe, et les traiter autrement romprait
            // un paiement en cours au moment du déploiement.
            var type = Lire(session.Metadata, CaisseStripeService.EtiquetteType);

            if (string.Equals(type, CaisseStripeService.TypeRecharge, StringComparison.OrdinalIgnoreCase))
            {
                await CrediterRechargeAsync(session, parentId, codeOffre, ct);
                return;
            }

            var etat = await _abonnements.SouscrireAsync(
                parentId, codeOffre, periodicite ?? PeriodiciteAbonnement.Mensuel,
                session.SubscriptionId, ct);

            if (etat is null)
            {
                _logger.LogError(
                    "Formule {Offre} inconnue a l'ouverture de la session {Session}.",
                    codeOffre, session.Id);
                return;
            }

            _logger.LogWarning(
                "Abonnement {Offre} ouvert pour le parent {ParentId} apres paiement "
                + "(abonnement Stripe {Sub}).", codeOffre, parentId, session.SubscriptionId);

            await OffrirLancementAsync(session, parentId, codeOffre, periodicite, ct);

            // ALERTE DEPUIS LE WEBHOOK, ET NON DEPUIS LE CONTRÔLEUR.
            //
            // C'est ici qu'un abonnement payant naît. Le contrôleur, lui, ne
            // fait qu'ouvrir une page de paiement : alerter là-bas signalerait
            // des intentions, pas des ventes — y compris celles que le parent
            // abandonne devant sa carte.
            await _telegram.NotifierAbonnementAsync(
                await _parents.GetParentByIdAsync(parentId),
                etat.OffreLibelle ?? codeOffre, etat.Periodicite, changement: false);
        }

        /// <summary>
        /// Les trois heures de l'offre de lancement, créditées à la
        /// souscription.
        ///
        /// CHEZ NOUS ET NON CHEZ STRIPE, et ce choix porte loin. Un coupon
        /// Stripe s'attache à un abonnement et le suit : il faudrait le
        /// retirer un par un à la fin de la campagne, et un oubli offrirait
        /// la remise à vie. Ici, l'offre est un CRÉDIT PONCTUEL sur la
        /// période en cours — elle s'éteint d'elle-même, et éteindre
        /// l'interrupteur suffit à arrêter les suivantes.
        ///
        /// ICI ET NON DANS LE CONTRÔLEUR : les heures ne sont offertes
        /// qu'après ENCAISSEMENT. Les donner à l'ouverture de la page de
        /// paiement les donnerait aussi à qui renonce devant sa carte.
        ///
        /// IDEMPOTENCE PAR LA SESSION, comme pour un pack acheté : Stripe
        /// réémet tout événement dont il n'a pas eu de 200. Le préfixe
        /// `lancement:` garantit que cette clé ne peut jamais entrer en
        /// collision avec celle d'une vraie recharge payée — et rend la
        /// ligne lisible en base, ce qu'un identifiant nu ne ferait pas.
        ///
        /// AUCUN PAIEMENT N'EST RATTACHÉ, et c'est délibéré : un
        /// remboursement d'abonnement reprend les heures de la recharge qui
        /// porte son paiement. Un cadeau ne se reprend pas comme un achat.
        ///
        /// ET IL EST ENREGISTRÉ À ZÉRO EURO. `OffrirMinutesAsync` plutôt que
        /// `RechargerApresPaiementAsync` : la seconde inscrit le prix du
        /// catalogue, ce qui est juste pour un achat et faux pour un cadeau.
        /// Chaque bénéficiaire aurait ajouté un chiffre d'affaires fictif à
        /// l'administration — sur l'écran même qui sert à décider des prix.
        ///
        /// DES MINUTES ET NON UN PACK, aussi. Le catalogue des packs sert à
        /// VENDRE : ses entrées sont poussées chez Stripe. Offrir cinq heures
        /// n'a pas à passer par la création d'un produit marchand.
        /// </summary>
        private async Task OffrirLancementAsync(
            Session session, int parentId, string codeOffre,
            string? periodicite, CancellationToken ct)
        {
            // MENSUEL SEULEMENT, parce que la carte annonce « 12 h LE PREMIER
            // MOIS ». Les heures se créditent sur la période en cours : sur un
            // abonnement annuel, cette période dure un an, et « trois heures
            // offertes sur douze mois » ne veut plus rien dire.
            //
            // La page des tarifs retire la mention quand on bascule en annuel.
            // Cette garde est ce qui empêche les deux de diverger — sans elle,
            // on offrirait en silence ce qu'on n'a pas promis.
            // `EstAnnuel` plutôt qu'une égalité à « Mensuel » : c'est le seul
            // des deux qui traite l'absence d'étiquette comme du mensuel, ce que
            // fait déjà l'ouverture de l'abonnement juste au-dessus.
            if (PeriodiciteAbonnement.EstAnnuel(periodicite)) return;


            // RELUE MAINTENANT, au moment d'encaisser — pas à l'ouverture de
            // la caisse. C'est la lecture qui fait foi : une offre éteinte
            // entre le clic et le paiement ne doit plus rien offrir.
            // RELUE MAINTENANT, ET C'EST ELLE QUI DIT QUEL PACK OFFRIR.
            //
            // Le pack est réglé dans l'onglet Modes, pas écrit ici : offrir
            // dix heures au lieu de trois ne demande aucun déploiement. Et
            // c'est LA MÊME lecture qui a servi à l'annoncer sur la page des
            // tarifs — les deux ne peuvent pas désigner des packs différents.
            var offre = await _lancement.LireAsync(ct);

            if (!offre.Active) return;

            // LA FORMULE EST-ELLE CONCERNÉE ? Vérifié APRÈS la lecture, et
            // sur la MÊME lecture que celle qui a servi à l'annoncer : la
            // page des tarifs et ce webhook ne peuvent pas viser des
            // formules différentes.
            if (!offre.Formules.Contains(codeOffre, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }


            var etat = await _abonnements.OffrirMinutesAsync(
                parentId,
                offre.MinutesOffertes,
                $"lancement:{session.Id}",
                "Offre de lancement",
                ct);

            if (etat is null)
            {
                // Pack absent du catalogue, ou événement déjà traité. Ni
                // l'un ni l'autre n'autorise à recréditer.
                _logger.LogWarning(
                    "Offre de lancement non creditee au parent {ParentId} (session {Session}) : "
                    + "essai en cours, sans abonnement, ou session deja traitee.",
                    parentId, session.Id);
                return;
            }

            _logger.LogWarning(
                "Offre de lancement : {Minutes} min offertes au parent {ParentId} "
                + "(session {Session}).", offre.MinutesOffertes, parentId, session.Id);
        }

        /// <summary>
        /// Le pack d'heures est payé : on le crédite.
        ///
        /// C'EST LE SEUL ENDROIT OÙ DES HEURES ACHETÉES SONT AJOUTÉES.
        /// Elles l'étaient auparavant dès le clic, sans passer par la caisse :
        /// n'importe quel parent pouvait s'en offrir autant qu'il voulait.
        ///
        /// IDEMPOTENCE. Stripe réémet un événement dont il n'a pas eu de 200,
        /// et il n'ordonne pas ses envois. Créditer deux fois donnerait des
        /// heures gratuites au premier parent qui rafraîchit au mauvais
        /// moment ; c'est le dépôt qui refuse le doublon, sur l'identifiant de
        /// session, et non un test de date qui laisserait passer une seconde
        /// tentative une minute plus tard.
        /// </summary>
        private async Task CrediterRechargeAsync(
            Session session, int parentId, string codePack, CancellationToken ct)
        {
            // Le paiement est retenu MAINTENANT, pas au remboursement : c'est ici
            // qu'on l'a sous la main. Le chercher plus tard demanderait un
            // aller-retour chez Stripe au milieu d'un webhook.
            var etat = await _abonnements.RechargerApresPaiementAsync(
                parentId, codePack, session.Id, session.PaymentIntentId, ct);

            if (etat is null)
            {
                // Deux causes possibles, et aucune n'autorise à recréditer :
                // le pack n'existe pas (catalogue désynchronisé), ou cette
                // session a déjà été créditée. Le dépôt journalise laquelle.
                _logger.LogWarning(
                    "Recharge {Pack} non creditee pour le parent {ParentId} (session {Session}) : "
                    + "pack inconnu ou paiement deja traite.", codePack, parentId, session.Id);
                return;
            }

            _logger.LogWarning(
                "Recharge {Pack} creditee au parent {ParentId} apres paiement (session {Session}).",
                codePack, parentId, session.Id);
        }

        /// <summary>
        /// Un paiement a été remboursé : les heures achetées sont reprises.
        ///
        /// SEULEMENT LE REMBOURSEMENT TOTAL. Un remboursement partiel — un
        /// geste commercial de quelques euros — ne dit pas combien d'heures
        /// retirer, et en deviner une fraction donnerait un pot faux dans un
        /// sens ou dans l'autre. On le journalise pour qu'un humain tranche.
        ///
        /// Un remboursement d'ABONNEMENT passe aussi par ici : le dépôt ne
        /// trouve alors aucune recharge et ne fait rien, ce qui est correct —
        /// c'est Stripe qui pilote la période d'un abonnement, pas nous.
        /// </summary>
        private async Task ReprendreAsync(Charge charge, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(charge.PaymentIntentId))
            {
                _logger.LogWarning(
                    "Remboursement {Charge} sans paiement associe : rien a reprendre.", charge.Id);
                return;
            }

            var total = charge.AmountRefunded >= charge.Amount;

            if (!total)
            {
                _logger.LogWarning(
                    "Remboursement PARTIEL de {Rembourse}/{Total} sur le paiement {Paiement} : "
                    + "les heures sont LAISSEES, a arbitrer a la main.",
                    charge.AmountRefunded, charge.Amount, charge.PaymentIntentId);

                // LE PARENT EST PRÉVENU DANS LES DEUX CAS, TOTAL COMME PARTIEL.
                //
                // Le remboursement est parti de chez nous ; ce qu'on décide
                // ensuite des heures est une affaire interne. Se taire sur un
                // partiel laisserait un parent voir arriver cinq euros sans
                // savoir d'où ils viennent.
                await PrevenirDuRemboursementAsync(charge, total: false, heuresReprises: false, ct);
                return;
            }

            var repris = await _abonnements.RembourserRechargeAsync(charge.PaymentIntentId, ct);

            if (!repris)
            {
                // Cas normal : remboursement d'un abonnement, ou reprise déjà
                // faite sur un événement précédent.
                _logger.LogInformation(
                    "Remboursement {Paiement} : aucune recharge a reprendre.", charge.PaymentIntentId);

                await PrevenirDuRemboursementAsync(charge, total: true, heuresReprises: false, ct);
                return;
            }

            _logger.LogWarning(
                "Heures REPRISES apres remboursement total du paiement {Paiement}.",
                charge.PaymentIntentId);

            await PrevenirDuRemboursementAsync(charge, total: true, heuresReprises: true, ct);
        }

        /// <summary>
        /// Écrit au parent que son remboursement est parti.
        ///
        /// POURQUOI CE COURRIEL EXISTE
        /// ---------------------------
        /// Le remboursement est instantané chez nous et invisible chez lui : sa
        /// banque met cinq à dix jours ouvrés. Entre les deux, un parent qui ne
        /// voit rien revenir écrit au support — ou conteste auprès de sa banque,
        /// ce qui coûte des frais et abîme la réputation du compte de paiement.
        /// Annoncer le délai coûte un courriel et évite les deux.
        ///
        /// LE PARENT EST RETROUVÉ PAR SON CLIENT STRIPE, pas par la recharge :
        /// un remboursement d'ABONNEMENT n'a pas de recharge derrière, et c'est
        /// justement le cas où le silence serait le plus mal compris.
        /// </summary>
        private async Task PrevenirDuRemboursementAsync(
            Charge charge, bool total, bool heuresReprises, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(charge.CustomerId)) return;

                var parent = await _parents.GetParentParClientStripeAsync(charge.CustomerId, ct);
                if (string.IsNullOrWhiteSpace(parent?.Mail)) return;

                // LE MONTANT DU REMBOURSEMENT QUI VIENT D'AVOIR LIEU, et non le
                // cumul : `AmountRefunded` additionne tous les remboursements du
                // paiement. Sur un second partiel, il annoncerait au parent une
                // somme qu'il ne verra jamais arriver d'un coup.
                var dernier = charge.Refunds?.Data?.LastOrDefault()?.Amount;
                var centimes = dernier ?? charge.AmountRefunded;

                var consequence = heuresReprises
                    ? "Les heures achetées ont été retirées de votre forfait."
                    : total
                        ? "Votre forfait n'est pas modifié."
                        : "Votre forfait n'est pas modifié par ce remboursement partiel.";

                await _email.EnvoyerAsync(
                    parent.Mail!,
                    "Votre remboursement a bien été effectué",
                    "remboursement",
                    new Dictionary<string, string>
                    {
                        ["montant"] = $"{centimes / 100m:0.00} €".Replace('.', ','),
                        ["achat"] = total ? "Remboursement total" : "Remboursement partiel",
                        ["date"] = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                        ["consequence"] = consequence,
                        ["lien"] = _options.Value.UrlRetour ?? string.Empty,
                    },
                    ct);

                _logger.LogInformation(
                    "Courriel de remboursement envoye a {Mail} ({Centimes} centimes).",
                    parent.Mail, centimes);
            }
            catch (Exception ex)
            {
                // UN COURRIEL RATÉ NE DOIT PAS FAIRE ÉCHOUER LE WEBHOOK.
                // L'argent est rendu, les heures sont reprises : rejouer
                // l'événement pour un problème de SMTP recréditerait rien mais
                // encombrerait les relances de Stripe pour rien.
                _logger.LogError(ex,
                    "Courriel de remboursement NON ENVOYE pour le paiement {Paiement}.",
                    charge.PaymentIntentId);
            }
        }

        /// <summary>
        /// Une facture est payée : la période repart.
        ///
        /// La PREMIÈRE facture passe aussi par ici, juste après l'ouverture —
        /// et c'est sans conséquence : reporter la période sur les dates que
        /// Stripe vient de fixer donne le même résultat que ce qu'on venait
        /// d'écrire. C'est le renouvellement du mois suivant qui compte.
        /// </summary>
        private async Task ReporterAsync(Invoice facture, CancellationToken ct)
        {
            var abonnementId = AbonnementDe(facture);

            if (string.IsNullOrWhiteSpace(abonnementId))
            {
                // Facture hors abonnement : une recharge, par exemple.
                _logger.LogDebug("Facture {Facture} sans abonnement : ignoree.", facture.Id);
                return;
            }

            // Les dates viennent de la LIGNE de facture : c'est elle qui porte
            // la période couverte. La facture, prise globalement, n'en a pas —
            // elle peut mélanger un abonnement et un ajustement.
            var ligne = facture.Lines?.Data?.FirstOrDefault();
            var debut = ligne?.Period?.Start;
            var fin = ligne?.Period?.End;

            if (debut is null || fin is null)
            {
                _logger.LogWarning(
                    "Facture {Facture} sans periode lisible : report ignore.", facture.Id);
                return;
            }

            var reporte = await _abonnements.ReporterPeriodeAsync(
                abonnementId, debut.Value, fin.Value, ct);

            if (!reporte)
            {
                // Cas normal et attendu : la facture arrive avant que
                // `checkout.session.completed` ait été traité. Stripe
                // n'ordonne pas ses événements — celui-ci sera réémis, et
                // l'abonnement existera alors.
                _logger.LogInformation(
                    "Aucun abonnement local pour {Sub} : report differe.", abonnementId);
                return;
            }

            _logger.LogInformation(
                "Periode reportee au {Fin:d} pour l'abonnement {Sub}.", fin.Value, abonnementId);
        }

        /// <summary>
        /// Un prélèvement a échoué.
        ///
        /// ON NE COUPE PAS L'ACCÈS ICI, et on ne le laisse pas courir non plus.
        ///
        /// Pas de coupure : Stripe relance la carte plusieurs fois sur une
        /// quinzaine de jours, et la plupart de ces échecs se résolvent seuls —
        /// un plafond atteint en fin de mois. Couper au premier refus
        /// priverait de cours un enfant dont le parent a changé de carte. La
        /// période en cours est payée, elle reste servie jusqu'au bout.
        ///
        /// Mais l'abonnement est marqué, et c'est nouveau : à l'échéance, la
        /// période ne se renouvellera pas tant que la facture n'est pas réglée.
        /// Auparavant elle repartait quoi qu'il arrive, et le compte restait
        /// pleinement utilisable — jusqu'à ce que Stripe abandonne deux
        /// semaines plus tard, et pour toujours si ce dernier webhook se
        /// perdait.
        /// </summary>
        private async Task AlerterAsync(Invoice facture, CancellationToken ct)
        {
            var abonnementId = AbonnementDe(facture);

            if (string.IsNullOrWhiteSpace(abonnementId))
            {
                _logger.LogWarning(
                    "Prelevement refuse sur la facture {Facture}, sans abonnement rattache.",
                    facture.Id);
                return;
            }

            var marque = await _abonnements.MarquerImpayeAsync(abonnementId, ct);

            _logger.LogWarning(
                marque
                    ? "Prelevement refuse pour l'abonnement {Sub} (facture {Facture}). Acces "
                      + "maintenu jusqu'a l'echeance, pas de renouvellement sans reglement."
                    : "Prelevement refuse pour l'abonnement {Sub} (facture {Facture}), deja "
                      + "marque impaye ou inconnu ici.",
                abonnementId, facture.Id);
        }

        /// <summary>
        /// Stripe déclare l'abonnement terminé : fin de résiliation, ou
        /// impayé au bout des relances. Là, on clôt.
        /// </summary>
        private async Task CloreAsync(Subscription abonnement, CancellationToken ct)
        {
            var clos = await _abonnements.CloreParStripeAsync(abonnement.Id, ct);

            _logger.LogWarning(
                clos
                    ? "Abonnement {Sub} clos : Stripe le declare termine."
                    : "Abonnement {Sub} declare termine par Stripe, mais inconnu ici.",
                abonnement.Id);
        }

        /// <summary>
        /// L'abonnement porté par une facture.
        ///
        /// Passé en version 3 de l'API Stripe du champ `subscription` à la
        /// ligne de facture ; on regarde les deux plutôt que de parier sur la
        /// version du compte, qui n'est pas la nôtre à choisir.
        /// </summary>
        private static string? AbonnementDe(Invoice facture) =>
            facture.Parent?.SubscriptionDetails?.SubscriptionId
            ?? facture.Lines?.Data?
                .Select(l => l.Parent?.SubscriptionItemDetails?.Subscription)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        private static string? Lire(IDictionary<string, string>? etiquettes, string cle) =>
            etiquettes is not null && etiquettes.TryGetValue(cle, out var valeur) ? valeur : null;

        private static bool Etiquette(
            IDictionary<string, string>? etiquettes, string cle, out int valeur) =>
            int.TryParse(Lire(etiquettes, cle), out valeur);
    }
}
