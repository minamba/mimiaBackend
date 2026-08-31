using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Domain.Models;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Domain.Repositories;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// Grille tarifaire et abonnement du parent authentifié.
    ///
    /// Le catalogue est public — la page de tarifs doit s'afficher avant toute
    /// connexion. Tout le reste est rattaché au parent porteur du jeton : aucune
    /// route ne prend d'identifiant de parent en paramètre.
    /// </summary>
    [ApiController]
    [Route("abonnements")]
    public class AbonnementsController : Controller
    {
        private readonly IAbonnementRepository _abonnements;
        private readonly IChatContexteResolver _resolver;
        private readonly ExemptionFacturation _exemptions;
        private readonly ILogger<AbonnementsController> _logger;

        public AbonnementsController(
            IAbonnementRepository abonnements,
            IChatContexteResolver resolver,
            IOptions<ExemptionFacturation> exemptions,
            ILogger<AbonnementsController> logger)
        {
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _exemptions = exemptions?.Value ?? throw new ArgumentNullException(nameof(exemptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>La grille tarifaire. Accessible sans être connecté.</summary>
        [HttpGet("offres")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Les formules et les recharges.")]
        public async Task<IActionResult> Offres(CancellationToken ct)
        {
            try
            {
                return Ok(new
                {
                    formules = await _abonnements.GetOffresAsync(ct),
                    recharges = await _abonnements.GetOffresRechargeAsync(ct),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la grille tarifaire.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }

        /// <summary>Où en est la famille de son quota du mois.</summary>
        [HttpGet("mon-quota")]
        [Authorize]
        [SwaggerResponse(200, "L'état du quota.", typeof(EtatQuota))]
        [SwaggerResponse(204, "Aucun abonnement en cours.")]
        public Task<IActionResult> MonQuota(CancellationToken ct) =>
            Executer(async parentId =>
            {
                var etat = await _abonnements.GetEtatQuotaAsync(parentId, ct);
                return etat is null ? NoContent() : Ok(etat);
            });

        /// <summary>
        /// Combien d'enfants ce compte peut encore enregistrer.
        ///
        /// Une route à part de `mon-quota` : la liste des enfants doit pouvoir
        /// la consulter sans avoir besoin de tout l'état du forfait, et elle
        /// répond même quand aucun abonnement n'existe.
        /// </summary>
        [HttpGet("capacite-enfants")]
        [Authorize]
        [SwaggerResponse(200, "La capacité restante.", typeof(CapaciteEnfants))]
        public Task<IActionResult> CapaciteEnfants(CancellationToken ct) =>
            Executer(async parentId => Ok(await _abonnements.CapaciteAsync(parentId, ct)));

        /// <summary>
        /// Souscrit une formule.
        ///
        /// DEUX SORTIES POSSIBLES, ET LE FRONT DOIT LES DISTINGUER.
        ///
        /// Le cas courant renvoie `{ urlPaiement }` : la formule se paie, et
        /// le navigateur doit aller à cette adresse. AUCUN abonnement n'est
        /// ouvert à ce moment-là — c'est le webhook qui le fera quand Stripe
        /// confirmera l'encaissement.
        ///
        /// Le cas gratuit renvoie l'état du quota, comme avant : l'essai et
        /// les comptes de l'exploitant n'ont pas de caisse à passer.
        /// </summary>
        [HttpPost("souscrire")]
        [Authorize]
        [SwaggerResponse(200, "L'abonnement ouvert (formule gratuite), ou l'adresse de paiement.")]
        [SwaggerResponse(404, "Formule inconnue.")]
        [SwaggerResponse(503, "Formule payante alors que Stripe n'est pas configuré.")]
        public Task<IActionResult> Souscrire(
            [FromQuery] string offre,
            [FromQuery] string periodicite,
            [FromServices] ICaisseStripeService caisse,
            [FromServices] IOptions<OptionsStripe> stripe,
            [FromServices] ITelegramService telegram,
            CancellationToken ct) =>
            Executer(async parentId =>
            {
                if (string.IsNullOrWhiteSpace(offre)) return BadRequest(new { message = "Formule absente." });

                // Le rythme de facturation est EXIGÉ, pas déduit : un défaut
                // silencieux ferait souscrire au mois un parent qui croyait
                // prendre l'année, et il ne s'en apercevrait qu'au prélèvement.
                if (!PeriodiciteAbonnement.EstValide(periodicite))
                {
                    return BadRequest(new { message = "Rythme de facturation absent ou invalide (Mensuel ou Annuel)." });
                }

                var formule = (await _abonnements.GetOffresAsync(ct)).FirstOrDefault(
                    o => string.Equals(o.Code, offre, StringComparison.OrdinalIgnoreCase));

                if (formule is null) return NotFound(new { message = "Formule inconnue." });

                // QUI PASSE À LA CAISSE, ET QUI NON.
                //
                // L'essai est gratuit : lui présenter une page de carte
                // bancaire arrêterait net l'inscription qu'il est censé
                // faciliter. Les comptes de l'exploitant sont exemptés pour la
                // même raison pratique — c'est avec eux qu'on teste, et chaque
                // essai deviendrait un paiement à rembourser.
                //
                // Le court-circuit est ICI et non dans un log : jusqu'à
                // présent la règle était constatée sans être appliquée, ce qui
                // aurait envoyé nos propres comptes devant la caisse.
                var gratuite = formule.EstEssai || !await DoitPayerAsync();

                if (gratuite)
                {
                    _logger.LogInformation(
                        "Souscription {Offre} sans passer par la caisse ({Motif}).",
                        offre, formule.EstEssai ? "essai gratuit" : "compte exempte");

                    var direct = await _abonnements.SouscrireAsync(parentId, offre, periodicite, null, ct);
                    return direct is null ? NotFound(new { message = "Formule inconnue." }) : Ok(direct);
                }

                if (!stripe.Value.EstConfigure)
                {
                    _logger.LogError(
                        "Souscription payante {Offre} demandee alors qu'aucune cle Stripe n'est configuree.",
                        offre);

                    return StatusCode(503, new
                    {
                        message = "Le paiement n'est pas disponible pour le moment.",
                    });
                }

                // DÉJÀ UN ABONNEMENT STRIPE ? ON LE MODIFIE, ON N'EN OUVRE PAS
                // UN SECOND.
                //
                // Rouvrir une caisse créait un deuxième abonnement chez Stripe
                // pendant que l'ancien continuait de courir : deux
                // prélèvements par mois, invisibles depuis l'application, qui
                // ne montre jamais que l'abonnement courant. Le parent l'aurait
                // découvert sur son relevé bancaire.
                //
                // Le changement est immédiat et ne demande aucune carte : elle
                // est déjà enregistrée chez Stripe.
                var change = await caisse.ChangerDeFormuleAsync(parentId, offre, periodicite, ct);

                if (change is not null)
                {
                    // ALERTE ICI ET PAS DANS LE WEBHOOK. Un changement de
                    // formule ne repasse pas par la caisse — la carte est déjà
                    // chez Stripe, l'abonnement est modifié sur place. Aucun
                    // `checkout.session.completed` n'arrivera, et l'attendre
                    // laisserait ce cas sans alerte.
                    await telegram.NotifierAbonnementAsync(
                        await _resolver.ResoudreParentAsync(),
                        change.OffreLibelle ?? offre, change.Periodicite, changement: true);

                    return Ok(change);
                }

                var ouverture = await caisse.OuvrirAsync(parentId, offre, periodicite, ct);

                // Null = tarif absent chez Stripe, donc catalogue non
                // synchronisé. C'est notre défaut, pas celui du parent : il
                // reçoit un message neutre, la cause est dans les logs.
                return ouverture is null
                    ? StatusCode(503, new { message = "Cette formule n'est pas disponible à la vente." })
                    : Ok(new { urlPaiement = ouverture.Url });
            });

        /// <summary>
        /// Rattrape un paiement dont le webhook n'est pas arrivé.
        ///
        /// Appelée par la page de retour, après le paiement. Le webhook reste
        /// le chemin principal — celui-ci n'existe que parce qu'un webhook peut
        /// se perdre, et qu'un parent qui a payé sans rien recevoir n'a aucun
        /// moyen de s'en sortir seul.
        ///
        /// Sans effet quand tout s'est bien passé : l'abonnement est déjà
        /// enregistré, la route rend simplement l'état courant.
        /// </summary>
        [HttpPost("rattraper")]
        [Authorize]
        [SwaggerResponse(200, "L'état du forfait, rattrapé ou non.", typeof(EtatQuota))]
        [SwaggerResponse(204, "Aucun abonnement : il n'y avait rien à rattraper.")]
        public Task<IActionResult> Rattraper(
            [FromServices] ICaisseStripeService caisse, CancellationToken ct) =>
            Executer(async parentId =>
            {
                var rattrape = await caisse.RattraperAsync(parentId, ct);

                // Rien à rattraper n'est pas une erreur : c'est le cas normal
                // quand le webhook a fait son travail. On rend alors l'état
                // courant, que l'écran affichera de toute façon.
                var etat = rattrape ?? await _abonnements.GetEtatQuotaAsync(parentId, ct);

                return etat is null ? NoContent() : Ok(etat);
            });

        /// <summary>
        /// Ouvre l'essai gratuit, sans passer par la page des tarifs.
        ///
        /// C'est ce que « Commencer gratuitement » doit faire : le visiteur a
        /// cliqué sur une promesse de gratuité, l'envoyer choisir une formule
        /// après s'être inscrit lui demande de re-décider quelque chose qu'il
        /// avait déjà décidé — et la page des tarifs, à ce moment-là, ressemble
        /// à un péage.
        ///
        /// Sans danger pour un compte existant : le dépôt refuse d'ouvrir un
        /// second essai et rend l'état courant tel quel.
        /// </summary>
        [HttpPost("essai")]
        [Authorize]
        [SwaggerResponse(200, "L'essai ouvert, ou l'abonnement déjà en place.", typeof(EtatQuota))]
        [SwaggerResponse(404, "Aucune offre d'essai active.")]
        public Task<IActionResult> Essai(CancellationToken ct) =>
            Executer(async parentId =>
            {
                var etat = await _abonnements.OuvrirEssaiAsync(parentId, ct);

                return etat is null
                    ? NotFound(new { message = "Aucune offre d'essai disponible." })
                    : Ok(etat);
            });

        /// <summary>
        /// Annule une descente de gamme programmée.
        ///
        /// Le pendant de « je change d'avis » : tant que la bascule n'a pas eu
        /// lieu, le parent doit pouvoir revenir dessus — sinon la seule façon
        /// de rester sur sa formule serait de la reprendre après coup, en
        /// repassant par la caisse.
        /// </summary>
        [HttpPost("annuler-changement")]
        [Authorize]
        [SwaggerResponse(200, "Le forfait, changement annulé.", typeof(EtatQuota))]
        [SwaggerResponse(404, "Aucun abonnement en cours.")]
        public Task<IActionResult> AnnulerChangement(
            [FromServices] ICaisseStripeService caisse, CancellationToken ct) =>
            Executer(async parentId =>
            {
                var etat = await caisse.AnnulerLeChangementAsync(parentId, ct);

                return etat is null
                    ? NotFound(new { message = "Aucun abonnement en cours." })
                    : Ok(etat);
            });

        /// <summary>
        /// Ouvre le portail de facturation Stripe : changer de carte, lire ses
        /// factures, résilier.
        ///
        /// Trois écrans que Stripe fournit déjà, traduits et à jour. Les
        /// réécrire nous obligerait à manipuler des moyens de paiement.
        /// </summary>
        [HttpPost("portail")]
        [Authorize]
        [SwaggerResponse(200, "L'adresse du portail.")]
        [SwaggerResponse(404, "Ce compte n'a jamais rien payé : il n'a pas de portail.")]
        public Task<IActionResult> Portail(
            [FromServices] ICaisseStripeService caisse,
            [FromServices] IOptions<OptionsStripe> stripe,
            CancellationToken ct) =>
            Executer(async parentId =>
            {
                if (!stripe.Value.EstConfigure)
                {
                    return StatusCode(503, new { message = "Le paiement n'est pas disponible." });
                }

                var ouverture = await caisse.OuvrirPortailAsync(parentId, ct);

                return ouverture is null
                    ? NotFound(new { message = "Aucune facturation sur ce compte." })
                    : Ok(new { urlPortail = ouverture.Url });
            });

        /// <summary>
        /// Met l'abonnement en pause pour un mois. Une fois par année glissante.
        /// </summary>
        [HttpPost("pause")]
        [Authorize]
        [SwaggerResponse(200, "L'abonnement mis en pause.", typeof(EtatQuota))]
        [SwaggerResponse(409, "Pause indisponible : déjà utilisée cette année, ou offre d'essai.")]
        public Task<IActionResult> Pause(CancellationToken ct) =>
            Executer(async parentId =>
            {
                var etat = await _abonnements.MettreEnPauseAsync(parentId, ct);

                return etat is null
                    ? Conflict(new
                    {
                        message = "La pause n'est pas disponible : elle a déjà été utilisée cette année, "
                                  + "ou votre formule ne le permet pas.",
                    })
                    : Ok(etat);
            });

        /// <summary>
        /// Résilie l'abonnement, sans motif et sans frais.
        ///
        /// Cette route existe parce que l'article L215-1-1 du Code de la
        /// consommation impose, pour un contrat conclu en ligne, une
        /// résiliation en ligne aussi simple que la souscription. Une adresse
        /// e-mail à laquelle écrire ne suffit pas.
        ///
        /// L'accès n'est pas coupé : il court jusqu'à la fin de la période
        /// déjà réglée.
        /// </summary>
        [HttpPost("resilier")]
        [Authorize]
        [SwaggerResponse(200, "La résiliation est enregistrée.", typeof(EtatQuota))]
        [SwaggerResponse(409, "Aucun abonnement à résilier.")]
        public Task<IActionResult> Resilier(
            [FromServices] ICaisseStripeService caisse,
            [FromServices] ITelegramService telegram,
            CancellationToken ct) =>
            Executer(async parentId =>
            {
                var etat = await _abonnements.ResilierAsync(parentId, ct);

                if (etat is null)
                {
                    return Conflict(new
                    {
                        message = "Aucun abonnement en cours à résilier.",
                    });
                }

                // ALERTE AVANT MÊME LA COUPURE CHEZ STRIPE, et c'est voulu :
                // la décision du parent est prise, c'est elle qu'on veut
                // connaître. Si l'appel à Stripe échouait juste après, on
                // voudrait d'autant plus l'avoir su.
                await telegram.NotifierResiliationAsync(
                    await _resolver.ResoudreParentAsync(),
                    etat.OffreLibelle, etat.PeriodeFin);

                // ET CHEZ STRIPE, SANS QUOI LE PRÉLÈVEMENT CONTINUE.
                //
                // La ligne ci-dessus n'arrête que l'accès, à la fin de la
                // période. Sans celle-ci, la carte serait débitée tous les mois
                // pour un abonnement que le parent a résilié et dont il n'a
                // plus rien. Après l'enregistrement local et non avant : si
                // Stripe est injoignable, la résiliation reste acquise pour le
                // parent, et c'est un log d'erreur qui appelle la reprise à la
                // main.
                await caisse.ArreterLaReconductionAsync(parentId, ct);

                _logger.LogInformation(
                    "Résiliation demandée par le parent {ParentId}, effet au {Fin:yyyy-MM-dd}.",
                    parentId, etat.FinPrevue);

                return Ok(etat);
            });

        /// <summary>
        /// Revient sur une résiliation tant que la période court encore.
        ///
        /// Sans ce chemin, un parent qui se ravise devrait résilier puis
        /// re-souscrire — donc repayer une période déjà réglée.
        /// </summary>
        [HttpPost("annuler-resiliation")]
        [Authorize]
        [SwaggerResponse(200, "La résiliation est annulée.", typeof(EtatQuota))]
        [SwaggerResponse(409, "Trop tard : l'abonnement est déjà clos.")]
        public Task<IActionResult> AnnulerResiliation(
            [FromServices] ICaisseStripeService caisse, CancellationToken ct) =>
            Executer(async parentId =>
            {
                var etat = await _abonnements.AnnulerResiliationAsync(parentId, ct);

                // Symétrique de la résiliation : sans ça, l'accès reprendrait
                // chez nous pendant que Stripe cesserait de facturer à
                // l'échéance — un abonnement gratuit à vie, créé par un simple
                // aller-retour sur un bouton.
                if (etat is not null) await caisse.ReprendreLaReconductionAsync(parentId, ct);

                return etat is null
                    ? Conflict(new
                    {
                        message = "L'abonnement est déjà clos : souscrivez à nouveau pour reprendre les cours.",
                    })
                    : Ok(etat);
            });

        [HttpPost("reprendre")]
        [Authorize]
        [SwaggerResponse(200, "L'abonnement repris.", typeof(EtatQuota))]
        public Task<IActionResult> Reprendre(CancellationToken ct) =>
            Executer(async parentId =>
            {
                var etat = await _abonnements.ReprendreAsync(parentId, ct);
                return etat is null ? NoContent() : Ok(etat);
            });

        /// <summary>
        /// Ajoute un pack d'heures sur la période en cours.
        /// Comme la souscription, le paiement n'est pas encore branché.
        /// </summary>
        [HttpPost("recharger")]
        [Authorize]
        [SwaggerResponse(200, "Le quota rechargé.", typeof(EtatQuota))]
        [SwaggerResponse(404, "Pack inconnu, ou aucun abonnement en cours.")]
        [SwaggerResponse(409, "L'essai gratuit ne permet pas d'acheter des heures.")]
        public Task<IActionResult> Recharger(
            [FromQuery] string pack,
            [FromServices] ICaisseStripeService caisse,
            [FromServices] IOptions<OptionsStripe> stripe,
            CancellationToken ct) =>
            Executer(async parentId =>
            {
                if (string.IsNullOrWhiteSpace(pack)) return BadRequest(new { message = "Pack absent." });

                // Le refus de l'essai est distingué AVANT l'appel, pour qu'il
                // porte son vrai motif. Le dépôt rend `null` dans les trois cas
                // — pack inconnu, aucun abonnement, essai — et répondre « Pack
                // inconnu » à un parent en essai l'enverrait chercher une faute
                // de frappe là où il n'y en a pas.
                var courant = await _abonnements.GetEtatQuotaAsync(parentId, ct);

                if (courant?.EstEssai == true)
                {
                    return Conflict(new
                    {
                        message = "L'essai gratuit ne permet pas d'acheter des heures. "
                                  + "Choisissez une formule pour en ajouter.",
                    });
                }

                // LE PAIEMENT D'ABORD, LES HEURES ENSUITE.
                //
                // Cette route créditait directement : n'importe quel parent
                // abonné pouvait s'offrir autant de packs qu'il voulait, en
                // cliquant. C'était la principale source de revenu variable, et
                // elle était ouverte.
                //
                // Elle rend maintenant une URL de paiement, comme la
                // souscription. Les heures sont créditées par le webhook, après
                // encaissement, et à cet endroit-là seulement.
                if (await DoitPayerAsync())
                {
                    if (!stripe.Value.EstConfigure)
                    {
                        _logger.LogError("Recharge demandee sans configuration Stripe.");
                        return StatusCode(503, new
                        {
                            message = "Le paiement est momentanément indisponible. Réessayez plus tard.",
                        });
                    }

                    var ouverture = await caisse.OuvrirRechargeAsync(parentId, pack, ct);

                    return ouverture is null
                        ? NotFound(new { message = "Pack inconnu." })
                        : Ok(new { urlPaiement = ouverture.Url });
                }

                // Compte exempté : on crédite sans passer par la caisse, comme
                // pour la souscription. Aucune session Stripe, donc aucune
                // trace de paiement — c'est exactement ce que la colonne
                // `stripe_session_id` laisse à nul.
                var etat = await _abonnements.RechargerAsync(parentId, pack, ct);
                return etat is null ? NotFound(new { message = "Pack inconnu." }) : Ok(etat);
            });

        /// <summary>
        /// Résout le parent du jeton puis délègue. Extrait parce que les cinq
        /// routes authentifiées partagent la même résolution et le même filet
        /// d'exceptions — cinq copies finiraient par diverger.
        /// </summary>
        /// <summary>
        /// Ce compte doit-il passer par le paiement ?
        ///
        /// Aujourd'hui la question ne change rien : rien n'est facturé, tout le
        /// monde souscrit directement. Elle est posée quand même, et posée ICI,
        /// pour que l'arrivée de Stripe ne soit qu'un branchement de plus au
        /// lieu d'une règle à se rappeler.
        /// </summary>
        private async Task<bool> DoitPayerAsync()
        {
            var parent = await _resolver.ResoudreParentAsync();
            return !_exemptions.EstExempte(parent.Mail);
        }

        private async Task<IActionResult> Executer(Func<int, Task<IActionResult>> action)
        {
            try
            {
                var parent = await _resolver.ResoudreParentAsync();
                return await action(parent.Id);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur sur une operation d'abonnement.");
                return StatusCode(500, new { message = "Une erreur est survenue, veuillez réessayer." });
            }
        }
    }
}
