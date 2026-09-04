using SchoolWebApp.Api.Controllers;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'offre de lancement : trois heures offertes le premier mois sur Solo.
    ///
    /// POURQUOI UN SERVICE POUR TROIS LIGNES
    /// -------------------------------------
    /// La question « l'offre est-elle vivante ? » est posée à DEUX endroits
    /// qui ne se voient pas : la page des tarifs, qui la promet, et le webhook
    /// de paiement, qui la tient. S'ils répondent différemment ne serait-ce
    /// qu'une minute, un parent paie en voyant « 3 h offertes » et ne les
    /// reçoit pas — la pire panne possible sur un site marchand, parce qu'elle
    /// est invisible côté serveur et parfaitement visible côté client.
    ///
    /// La règle vit donc ici, une fois, et les deux appelants la lisent.
    ///
    /// LA DATE LIMITE EST CONTRAIGNANTE, PAS DÉCORATIVE
    /// ------------------------------------------------
    /// Le compte à rebours de la page d'accueil annonce une échéance. Une
    /// échéance qu'on n'applique pas est un mensonge commercial : le visiteur
    /// qui souscrit deux heures après la fin verrait « offre terminée » sur la
    /// page et recevrait quand même le cadeau — ou, bien pire, l'inverse.
    ///
    /// L'interrupteur reste le maître : éteint, l'offre est morte quelle que
    /// soit la date. La date ne fait que l'éteindre TOUTE SEULE le moment
    /// venu, sans qu'il faille y penser un dimanche soir.
    /// </summary>
    public interface IOffreLancementService
    {
        /// <summary>
        /// L'offre est-elle vivante EN CE MOMENT ? Interrupteur allumé, et
        /// date limite non dépassée.
        /// </summary>
        Task<bool> EstVivanteAsync(CancellationToken ct = default);

        /// <summary>Ce que le navigateur a besoin de savoir pour l'afficher.</summary>
        Task<OffreLancement> LireAsync(CancellationToken ct = default);
    }

    /// <param name="Active">Vivante en ce moment : interrupteur ET date.</param>
    /// <param name="Texte">Ce qui s'écrit entre parenthèses à côté de « Solo ».</param>
    /// <param name="Fin">L'échéance du compte à rebours. Nulle = sans terme.</param>
    /// <param name="Bandeau">
    /// Le compte à rebours s'affiche-t-il sur la page d'accueil ?
    ///
    /// TOUJOURS FAUX QUAND L'OFFRE EST MORTE, quel que soit son propre
    /// interrupteur. Un décompte sans offre derrière presse le visiteur vers
    /// une promotion qui ne sera pas honorée — le mensonge le plus cher
    /// qu'un site marchand puisse faire. La dépendance est écrite ici, une
    /// fois, plutôt que laissée à chaque écran qui l'afficherait.
    /// </param>
    public record OffreLancement(bool Active, string Texte, DateTime? Fin, bool Bandeau);

    public class OffreLancementService : IOffreLancementService
    {
        /// <summary>
        /// Le pack crédité. C'est une VRAIE offre du catalogue, pas un nombre
        /// de minutes écrit en dur — c'est ce qui permet d'annoncer « valeur
        /// 14,90 € » avec un prix que le parent peut vérifier lui-même sur la
        /// page des tarifs.
        /// </summary>
        public const string PackOffert = "PACK3H";

        /// <summary>La seule formule concernée.</summary>
        public const string OffreConcernee = "SOLO";

        /// <summary>
        /// Ce qui s'affiche si personne n'a rien écrit. Pas de chaîne vide :
        /// une paire de parenthèses vides à côté de « Solo » se lit comme un
        /// défaut d'affichage, pas comme une promotion.
        /// </summary>
        private const string TexteParDefaut = "OFFRE LANCEMENT";

        private readonly IReglageRepository _reglages;
        private readonly ILogger<OffreLancementService> _logger;

        public OffreLancementService(
            IReglageRepository reglages, ILogger<OffreLancementService> logger)
        {
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> EstVivanteAsync(CancellationToken ct = default) =>
            (await LireAsync(ct)).Active;

        public async Task<OffreLancement> LireAsync(CancellationToken ct = default)
        {
            try
            {
                var allumee = await _reglages.EstActifAsync(
                    ReglagesController.OffreLancement, false, ct);

                var texte = await _reglages.LireAsync(ReglagesController.OffreLancementTexte, ct);
                var fin = Echeance(await _reglages.LireAsync(ReglagesController.OffreLancementFin, ct));

                // `> DateTime.UtcNow` et non `>=` : l'échéance est le dernier
                // instant où l'offre vaut. Une seconde après, elle est morte.
                var vivante = allumee && (fin is null || fin > DateTime.UtcNow);

                // ALLUMÉ PAR DÉFAUT, et jamais sans offre vivante derrière.
                var bandeau = vivante && await _reglages.EstActifAsync(
                    ReglagesController.OffreLancementBandeau, true, ct);

                return new OffreLancement(
                    vivante,
                    string.IsNullOrWhiteSpace(texte) ? TexteParDefaut : texte.Trim(),
                    fin,
                    bandeau);
            }
            catch (Exception ex)
            {
                // ÉTEINTE QUAND ON NE SAIT PAS, et c'est le seul défaut tenable
                // dans les deux sens : une lecture ratée ne doit ni promettre
                // un cadeau qu'on ne donnera pas, ni le donner sans l'avoir
                // promis.
                _logger.LogError(ex, "Lecture de l'offre de lancement impossible.");
                return new OffreLancement(false, TexteParDefaut, null, false);
            }
        }

        /// <summary>
        /// L'échéance stockée, relue en UTC.
        ///
        /// `RoundtripKind` PARCE QUE LA CHAÎNE PORTE SON FUSEAU. Sans lui,
        /// `TryParse` rendrait un `Unspecified` que la comparaison à
        /// `UtcNow` traiterait comme de l'UTC — soit deux heures de décalage
        /// en été, sur une date que l'administrateur a saisie à la minute près
        /// pour faire finir sa promotion à minuit.
        /// </summary>
        private static DateTime? Echeance(string? brut)
        {
            if (string.IsNullOrWhiteSpace(brut)) return null;

            return DateTime.TryParse(
                brut, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date)
                ? date.ToUniversalTime()
                : null;
        }
    }
}
