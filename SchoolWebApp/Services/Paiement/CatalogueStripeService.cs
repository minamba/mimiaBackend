using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Stripe;

namespace SchoolWebApp.Api.Services.Paiement
{
    /// <summary>Ce qu'une synchronisation a fait, pour l'afficher à l'administrateur.</summary>
    public record LigneCatalogue(string Code, string Libelle, string Etat);

    public interface ICatalogueStripeService
    {
        Task<IReadOnlyList<LigneCatalogue>> SynchroniserAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Recopie le catalogue de la base vers Stripe.
    ///
    /// POURQUOI UN SCRIPT ET NON UNE SAISIE À LA MAIN
    /// ----------------------------------------------
    /// Il y a huit tarifs : trois formules × mensuel/annuel, plus deux
    /// recharges. Les saisir dans le tableau de bord, c'est huit occasions de
    /// se tromper d'un centime — et il faudrait recommencer à l'identique en
    /// production. Une erreur de saisie ne se verrait pas : le parent paierait
    /// simplement un autre prix que celui affiché sur la page des tarifs.
    ///
    /// Ici, la base fait foi. Stripe n'est qu'un reflet.
    ///
    /// IDEMPOTENT : relancer ne crée pas de doublons. Un tarif déjà enregistré
    /// est laissé tel quel — sauf si son montant a changé, auquel cas on en
    /// crée un nouveau. Chez Stripe, un tarif est IMMUABLE : on ne modifie pas
    /// un prix, on le remplace. Les abonnements en cours gardent l'ancien, ce
    /// qui est exactement le comportement voulu — augmenter ses prix ne doit
    /// pas augmenter la facture de ceux qui ont déjà souscrit.
    /// </summary>
    public class CatalogueStripeService : ICatalogueStripeService
    {
        private const string Devise = "eur";

        private readonly IAbonnementRepository _abonnements;
        private readonly IOptions<OptionsStripe> _options;
        private readonly ILogger<CatalogueStripeService> _logger;

        public CatalogueStripeService(
            IAbonnementRepository abonnements,
            IOptions<OptionsStripe> options,
            ILogger<CatalogueStripeService> logger)
        {
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<LigneCatalogue>> SynchroniserAsync(
            CancellationToken ct = default)
        {
            if (!_options.Value.EstConfigure)
            {
                throw new InvalidOperationException("Aucune clé Stripe configurée.");
            }

            var client = new StripeClient(_options.Value.CleSecrete);
            var produits = new ProductService(client);
            var prix = new PriceService(client);

            var lignes = new List<LigneCatalogue>();

            foreach (var offre in await _abonnements.GetOffresAsync(ct))
            {
                // L'essai ne se paie pas : lui créer un tarif à zéro euro
                // encombrerait le catalogue Stripe d'un produit que personne
                // n'achètera jamais.
                if (offre.EstEssai)
                {
                    lignes.Add(new LigneCatalogue(offre.Code!, offre.Libelle!, "ignorée (essai gratuit)"));
                    continue;
                }

                var produit = await TrouverOuCreerProduitAsync(
                    produits, $"formule-{offre.Code!.ToLowerInvariant()}", offre.Libelle!, ct);

                var mensuel = await AssurerPrixAsync(
                    prix, produit.Id, offre.PrixMensuelCentimes, "month",
                    offre.StripePrixMensuelId, ct);

                var annuel = await AssurerPrixAsync(
                    prix, produit.Id, offre.PrixAnnuelCentimes, "year",
                    offre.StripePrixAnnuelId, ct);

                await _abonnements.EnregistrerPrixStripeAsync(
                    offre.Code!, mensuel.Id, annuel.Id, ct);

                lignes.Add(new LigneCatalogue(
                    offre.Code!, offre.Libelle!,
                    $"mensuel {mensuel.UnitAmount / 100.0:0.00} € · annuel {annuel.UnitAmount / 100.0:0.00} €"));
            }

            foreach (var recharge in await _abonnements.GetOffresRechargeAsync(ct))
            {
                var produit = await TrouverOuCreerProduitAsync(
                    produits, $"recharge-{recharge.Code!.ToLowerInvariant()}", recharge.Libelle!, ct);

                // Sans `recurrence` : une recharge s'achète une fois, elle ne
                // se renouvelle pas toute seule.
                var tarif = await AssurerPrixAsync(
                    prix, produit.Id, recharge.PrixCentimes, null, recharge.StripePrixId, ct);

                await _abonnements.EnregistrerPrixRechargeStripeAsync(recharge.Code!, tarif.Id, ct);

                lignes.Add(new LigneCatalogue(
                    recharge.Code!, recharge.Libelle!,
                    $"paiement unique {tarif.UnitAmount / 100.0:0.00} €"));
            }

            _logger.LogWarning(
                "Catalogue Stripe synchronise : {Nombre} entrees, mode {Mode}.",
                lignes.Count, _options.Value.EstEnProduction ? "PRODUCTION" : "test");

            return lignes;
        }

        /// <summary>
        /// Le produit est retrouvé par une clé de recherche stable plutôt que
        /// par son identifiant Stripe : on ne stocke pas les identifiants de
        /// produits, seulement ceux des tarifs, qui sont les seuls dont le
        /// paiement a besoin.
        /// </summary>
        private static async Task<Product> TrouverOuCreerProduitAsync(
            ProductService produits, string reference, string libelle, CancellationToken ct)
        {
            var existants = await produits.SearchAsync(
                new ProductSearchOptions { Query = $"metadata['reference']:'{reference}'" },
                cancellationToken: ct);

            if (existants.Data.Count > 0) return existants.Data[0];

            return await produits.CreateAsync(
                new ProductCreateOptions
                {
                    Name = $"Mimia — {libelle}",
                    Metadata = new Dictionary<string, string> { ["reference"] = reference },
                },
                cancellationToken: ct);
        }

        /// <summary>
        /// Garantit qu'un tarif au bon montant existe, et le rend.
        ///
        /// Si l'identifiant connu pointe déjà sur le bon montant, on le
        /// réutilise. Sinon on en crée un — jamais de modification : chez
        /// Stripe un tarif est immuable, et c'est une bonne chose. Les
        /// abonnements souscrits continuent sur l'ancien montant.
        /// </summary>
        private static async Task<Price> AssurerPrixAsync(
            PriceService prix,
            string produitId,
            int montantCentimes,
            string? intervalle,
            string? idConnu,
            CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(idConnu))
            {
                try
                {
                    var existant = await prix.GetAsync(idConnu, cancellationToken: ct);

                    if (existant.Active
                        && existant.UnitAmount == montantCentimes
                        && existant.Recurring?.Interval == intervalle)
                    {
                        return existant;
                    }
                }
                catch (StripeException)
                {
                    // Identifiant inconnu de ce compte : typiquement une base
                    // recopiée depuis un autre environnement. On repart de zéro.
                }
            }

            return await prix.CreateAsync(
                new PriceCreateOptions
                {
                    Product = produitId,
                    Currency = Devise,
                    UnitAmount = montantCentimes,
                    Recurring = intervalle is null
                        ? null
                        : new PriceRecurringOptions { Interval = intervalle },
                },
                cancellationToken: ct);
        }
    }
}
