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
    /// <param name="MinutesOffertes">
    /// Ce qui est offert à la souscription. Zéro éteint le cadeau — la
    /// mention et le décompte disparaissent avec lui, puisqu'ils
    /// n'annonceraient plus rien.
    /// </param>
    /// <param name="Formules">
    /// Les codes concernés. VIDE ÉTEINT L'OFFRE : une promotion qui ne
    /// s'applique à aucune formule ne promet rien, et la laisser « active »
    /// afficherait une mention sur des cartes qui ne changent pas.
    /// </param>
    /// <param name="FormulesTexte">
    /// Les mêmes, écrites pour un humain : « Solo », « Solo et Duo »,
    /// « Solo, Duo et Famille ». Composée ICI parce que le serveur est le
    /// seul à connaître les libellés — le compte à rebours de la page
    /// d'accueil, lui, ne charge pas le catalogue des formules.
    /// </param>
    public record OffreLancement(
        bool Active,
        string Texte,
        DateTime? Fin,
        bool Bandeau,
        int MinutesOffertes,
        IReadOnlyList<string> Formules,
        string FormulesTexte);

    public class OffreLancementService : IOffreLancementService
    {
        /// <summary>
        /// Le pack crédité. C'est une VRAIE offre du catalogue, pas un nombre
        /// de minutes écrit en dur — c'est ce qui permet d'annoncer « valeur
        /// 14,90 € » avec un prix que le parent peut vérifier lui-même sur la
        /// page des tarifs.
        /// </summary>
        /// <summary>
        /// Ce qu'on offre quand rien n'a été réglé : trois heures.
        ///
        /// UN DÉFAUT ET NON UNE FATALITÉ — le nombre saisi dans l'onglet
        /// Modes l'emporte. Celui-ci ne sert qu'à une base où le réglage n'a
        /// jamais été posé.
        /// </summary>
        public const int MinutesParDefaut = 180;

        /// <summary>
        /// La formule concernée quand rien n'a été réglé.
        ///
        /// Solo, parce que c'est l'entrée de gamme : une promotion sert à
        /// faire franchir la première marche, pas à remercier ceux qui ont
        /// déjà pris la plus chère.
        /// </summary>
        public const string FormuleParDefaut = "SOLO";

        /// <summary>
        /// Ce qui s'affiche si personne n'a rien écrit. Pas de chaîne vide :
        /// une paire de parenthèses vides à côté de « Solo » se lit comme un
        /// défaut d'affichage, pas comme une promotion.
        /// </summary>
        private const string TexteParDefaut = "OFFRE LANCEMENT";

        private readonly IReglageRepository _reglages;
        private readonly IAbonnementRepository _formules;
        private readonly ILogger<OffreLancementService> _logger;

        public OffreLancementService(
            IReglageRepository reglages,
            IAbonnementRepository formules,
            ILogger<OffreLancementService> logger)
        {
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _formules = formules ?? throw new ArgumentNullException(nameof(formules));
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

                // AUCUNE MINUTE, AUCUNE OFFRE. Un réglage à zéro éteint le
                // cadeau : la carte cesserait sinon de promettre quoi que ce
                // soit tout en restant marquée « offre de lancement ».
                var brut = await _reglages.LireAsync(
                    ReglagesController.OffreLancementMinutes, ct);

                var minutes = int.TryParse(brut, out var lu) ? lu : MinutesParDefaut;

                var codes = await FormulesAsync(ct);

                // `> DateTime.UtcNow` et non `>=` : l'échéance est le dernier
                // instant où l'offre vaut. Une seconde après, elle est morte.
                var vivante = allumee
                    && minutes > 0
                    && codes.Count > 0
                    && (fin is null || fin > DateTime.UtcNow);

                // ALLUMÉ PAR DÉFAUT, et jamais sans offre vivante derrière.
                var bandeau = vivante && await _reglages.EstActifAsync(
                    ReglagesController.OffreLancementBandeau, true, ct);

                return new OffreLancement(
                    vivante,
                    string.IsNullOrWhiteSpace(texte) ? TexteParDefaut : texte.Trim(),
                    fin,
                    bandeau,
                    minutes,
                    codes,
                    await EnToutesLettresAsync(codes, ct));
            }
            catch (Exception ex)
            {
                // ÉTEINTE QUAND ON NE SAIT PAS, et c'est le seul défaut tenable
                // dans les deux sens : une lecture ratée ne doit ni promettre
                // un cadeau qu'on ne donnera pas, ni le donner sans l'avoir
                // promis.
                _logger.LogError(ex, "Lecture de l'offre de lancement impossible.");
                return new OffreLancement(
                    false, TexteParDefaut, null, false, 0, [], string.Empty);
            }
        }

        /// <summary>
        /// Les codes réglés, ou le défaut si rien ne l'a jamais été.
        /// </summary>
        private async Task<IReadOnlyList<string>> FormulesAsync(CancellationToken ct)
        {
            var brut = await _reglages.LireAsync(
                ReglagesController.OffreLancementFormules, ct);

            // JAMAIS RÉGLÉ ET EXPLICITEMENT VIDÉ NE SE CONFONDENT PAS. Le
            // premier retombe sur le défaut ; le second est une décision —
            // l'administrateur a décoché toutes les formules, et l'offre ne
            // doit alors porter sur rien.
            if (brut is null) return [FormuleParDefaut];

            return brut
                .Split(',', StringSplitOptions.RemoveEmptyEntries
                            | StringSplitOptions.TrimEntries)
                .Select(c => c.ToUpperInvariant())
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// « Solo », « Solo et Duo », « Solo, Duo et Famille ».
        ///
        /// COMPOSÉE CÔTÉ SERVEUR parce qu'il est le seul à connaître les
        /// libellés. Le compte à rebours de la page d'accueil ne charge pas
        /// le catalogue des formules — lui faire deviner « SOLO » → « Solo »
        /// aurait marché jusqu'au premier code qui ne s'écrit pas comme son
        /// nom.
        /// </summary>
        private async Task<string> EnToutesLettresAsync(
            IReadOnlyList<string> codes, CancellationToken ct)
        {
            if (codes.Count == 0) return string.Empty;

            var catalogue = await _formules.GetOffresAsync(ct);

            var noms = codes
                .Select(code => catalogue
                    .FirstOrDefault(o => string.Equals(
                        o.Code, code, StringComparison.OrdinalIgnoreCase))?.Libelle

                    // Le code brut si la formule a disparu du catalogue :
                    // moins beau, mais toujours vrai.
                    ?? code)
                .ToList();

            if (noms.Count == 1) return noms[0];

            return string.Join(", ", noms.Take(noms.Count - 1)) + " et " + noms[^1];
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
