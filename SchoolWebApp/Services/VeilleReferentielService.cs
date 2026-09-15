using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Ce qu'un relevé de page a constaté — jamais ce qu'il en pense.</summary>
    /// <param name="Reussi">Faux si la page n'a pas pu être lue (réseau, page retirée, blocage).</param>
    /// <param name="Hash">L'empreinte du contenu lu, si la lecture a réussi.</param>
    public record ReleveVeille(bool Reussi, string? Hash);

    public interface IVeilleReferentielService
    {
        /// <summary>
        /// Va lire une page et en rend l'empreinte — RIEN DE PLUS. Ne
        /// remonte jamais d'exception : un site indisponible n'est jamais
        /// qu'un relevé manqué, pas une panne du worker qui l'appelle.
        /// </summary>
        Task<ReleveVeille> RelireAsync(string url, CancellationToken ct = default);
    }

    /// <summary>
    /// CONSTATE QU'UNE PAGE OFFICIELLE EXISTE OU A CHANGÉ — ET S'ARRÊTE LÀ.
    ///
    /// POURQUOI UNE EMPREINTE, ET PAS UNE LECTURE DU CONTENU
    /// --------------------------------------------------------
    /// La tentation serait de faire lire le texte par un modèle et d'en
    /// tirer une conclusion sur le référentiel. Ce service ne le fait PAS,
    /// et c'est un choix, pas une limite technique : transformer un texte de
    /// loi en compétences est un travail de jugement — c'est exactement ce
    /// que documente `ReferentielSeeder`, où trois passes de vérification
    /// humaine ont chacune trouvé une vraie erreur. Un résumé automatique
    /// donnerait la même fausse impression de certitude, avec les mêmes
    /// erreurs et personne pour les remarquer.
    ///
    /// L'EMPREINTE EST PRISE SUR LE TEXTE, PAS SUR LES OCTETS — ET C'EST LE
    /// RÉSULTAT D'UNE MESURE, PAS UNE PRÉFÉRENCE. Le 13/09/2026, les trois
    /// pages testées changeaient d'empreinte brute à deux secondes d'écart
    /// (jetons, horodatages, identifiants de session dans le HTML) : un
    /// worker qui les hachait telles quelles aurait dit « mis à jour » tous
    /// les jours, pour tout — et un statut qui crie sans arrêt est un statut
    /// qu'on n'écoute plus. Le texte visible de la région `main`, scripts et
    /// balises ôtés, espaces repliés, est stable d'une lecture à l'autre.
    ///
    /// Ce que ce service PEUT garantir, au total, tient en une phrase :
    /// « cette page a changé depuis la dernière fois », ou « personne n'a
    /// rien publié à cette adresse ». C'est un fait mécanique — un hachage
    /// de texte — jamais une interprétation.
    /// </summary>
    public class VeilleReferentielService : IVeilleReferentielService
    {
        /// <summary>
        /// La région de la page qui porte le contenu, quand le site en
        /// déclare une. En dehors : menus, pieds de page, blocs « à lire
        /// aussi » — tout ce qui bouge sans que le texte ait bougé.
        /// </summary>
        private static readonly Regex RegionPrincipale = new(
            @"<main\b[\s\S]*?</main>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Scripts = new(
            @"<(script|style|noscript)\b[\s\S]*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Commentaires = new(@"<!--[\s\S]*?-->", RegexOptions.Compiled);
        private static readonly Regex Balises = new(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex Entites = new(@"&[a-zA-Z#0-9]+;", RegexOptions.Compiled);
        private static readonly Regex Blancs = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// En dessous, ce n'est pas une page de programme — c'est une page
        /// d'erreur habillée, un défi anti-robot, ou une coquille vide. Un
        /// vrai texte officiel fait plusieurs milliers de caractères.
        /// </summary>
        private const int LongueurMinimale = 300;

        /// <summary>
        /// LES PHRASES D'UN DÉFI ANTI-ROBOT, telles que Légifrance les a
        /// servies au worker le 13/09/2026 (« Just a moment… Enable JavaScript
        /// and cookies to continue »). Sans ce garde-fou, cette page-là
        /// aurait été comptée comme un CHANGEMENT du texte officiel.
        /// </summary>
        private static readonly string[] MarquesDeDefi =
        {
            "Just a moment",
            "Enable JavaScript and cookies",
            "Checking your browser",
            "Vérification de votre navigateur",
            "Attention Required! | Cloudflare",
        };

        private readonly HttpClient _http;
        private readonly ILogger<VeilleReferentielService> _logger;

        public VeilleReferentielService(HttpClient http, ILogger<VeilleReferentielService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReleveVeille> RelireAsync(string url, CancellationToken ct = default)
        {
            try
            {
                using var reponse = await _http.GetAsync(url, ct);

                // UN 404 N'EST PAS UNE ERREUR À JOURNALISER EN ALARME : un
                // texte « en consultation » n'a simplement pas encore
                // d'adresse stable. Le worker le dit à sa façon, sans bruit
                // technique.
                if (!reponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Veille referentiel : {Url} a repondu {Code}.", url, (int)reponse.StatusCode);
                    return new ReleveVeille(false, null);
                }

                var html = await reponse.Content.ReadAsStringAsync(ct);
                var texte = ExtraireTexte(html);

                if (EstUnDefi(html) || texte.Length < LongueurMinimale)
                {
                    _logger.LogInformation(
                        "Veille referentiel : {Url} n'a pas rendu une page de contenu ({Longueur} car.).",
                        url, texte.Length);
                    return new ReleveVeille(false, null);
                }

                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texte)));

                return new ReleveVeille(true, hash);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Réseau, DNS, certificat : le site n'est simplement pas
                // joignable maintenant. Un prochain passage réessaiera.
                _logger.LogWarning(ex, "Veille referentiel impossible pour {Url}.", url);
                return new ReleveVeille(false, null);
            }
        }

        /// <summary>Le texte visible de la région principale — public pour être testable sans réseau.</summary>
        public static string ExtraireTexte(string html)
        {
            var region = RegionPrincipale.Match(html);
            var source = region.Success ? region.Value : html;

            source = Scripts.Replace(source, " ");
            source = Commentaires.Replace(source, " ");
            source = Balises.Replace(source, " ");
            source = Entites.Replace(source, " ");

            return Blancs.Replace(source, " ").Trim();
        }

        private static bool EstUnDefi(string html) =>
            MarquesDeDefi.Any(m => html.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
