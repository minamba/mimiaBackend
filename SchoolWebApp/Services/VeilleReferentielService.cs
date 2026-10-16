using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Ce qu'un relevé de page a constaté — jamais ce qu'il en pense.</summary>
    /// <param name="Reussi">Faux si la page n'a pas pu être lue (réseau, page retirée, blocage).</param>
    /// <param name="Hash">L'empreinte du contenu lu, si la lecture a réussi.</param>
    /// <param name="CopieDu">
    /// La page n'a pas pu être lue sur le site officiel, mais sur la copie de
    /// l'Internet Archive de cette date. Null : lue en direct.
    /// </param>
    public record ReleveVeille(bool Reussi, string? Hash, DateTime? CopieDu = null);

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

        // ================================================================
        // LE SECOURS : L'INTERNET ARCHIVE — 15/10/2026
        // ================================================================
        //
        // Ce jour-là, les 41 adresses surveillées étaient « injoignables » : les
        // sites du ministère (education.gouv.fr, Éduscol, Légifrance) bloquent
        // désormais les robots derrière Cloudflare — 403, même à un navigateur
        // simulé. La veille était aveugle sans le savoir. Camara : « c'est une
        // tâche très importante pour l'application ».
        //
        // Quand la lecture directe échoue, on lit la DERNIÈRE COPIE de la page
        // conservée par l'Internet Archive, en version BRUTE (`id_` : les octets
        // d'origine, sans la barre de l'Archive). Le texte en est extrait de la
        // même façon : une page inchangée garde la même empreinte, qu'elle soit
        // lue en direct ou sur la copie — passer de l'une à l'autre ne crée
        // aucune fausse alerte.
        //
        // CE QUE ÇA NE DIT PAS : « la page n'a pas bougé aujourd'hui », mais « elle
        // n'avait pas bougé à la date de la copie ». La date est gardée et
        // affichée. Une copie de plus de 14 jours déclenche une demande de copie
        // fraîche (« Save Page Now »), au plus dix par jour, sans l'attendre.

        // L'index CDX, et non l'API « available » : celle-ci répond « aucune
        // copie » pour la moitié des pages qui en ont pourtant (vérifié le
        // 15/10/2026 : 17 pages sur 35 « sans copie », toutes présentes au CDX).
        // « limit=-1 » rend la DERNIÈRE copie lue avec succès (statut 200).
        private const string IndexArchive =
            "https://web.archive.org/cdx/search/cdx?filter=statuscode:200&limit=-1&fl=timestamp&url=";
        private static readonly TimeSpan CopieTropAncienne = TimeSpan.FromDays(14);
        private const int DemandesDeCopieParJour = 10;

        // L'ARCHIVE RATIONNE : interrogée en rafale, elle répond « Temporarily
        // Offline » (vu le 15/10/2026, 9 échecs sur 35 pour des pages pourtant
        // copiées). Un appel à la fois, deux secondes entre deux appels, et deux
        // reprises espacées (10 s puis 30 s) quand elle refuse. La nuit, les
        // 35 pages prennent un peu plus de deux minutes : rien ne presse.
        private static readonly SemaphoreSlim UnAppelALaFois = new(1, 1);
        private static readonly TimeSpan EcartEntreAppels = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan[] AttentesAvantReprise = { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) };
        private static DateTime _dernierAppel = DateTime.MinValue;

        private static readonly object VerrouDemandes = new();
        private static DateTime _jourDesDemandes;
        private static int _demandesDuJour;

        public async Task<ReleveVeille> RelireAsync(string url, CancellationToken ct = default)
        {
            var direct = await RelireEnDirectAsync(url, ct);
            if (direct.Reussi) return direct;

            return await RelireDepuisArchiveAsync(url, ct) ?? direct;
        }

        private async Task<ReleveVeille?> RelireDepuisArchiveAsync(string url, CancellationToken ct)
        {
            try
            {
                var horodatage = (await LireALArchiveAsync(IndexArchive + Uri.EscapeDataString(url), url, ct))?.Trim();
                if (horodatage is null) return null;

                if (horodatage.Length == 0)
                {
                    // Aucune copie : on en demande une, elle servira la nuit suivante.
                    _logger.LogInformation("Veille referentiel : aucune copie de {Url} a l'Internet Archive.", url);
                    DemanderUneCopieFraiche(url);
                    return null;
                }

                if (!DateTime.TryParseExact(horodatage, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var copieDu))
                {
                    _logger.LogWarning("Veille referentiel : reponse inattendue de l'index de l'Archive pour {Url}.", url);
                    return null;
                }

                var html = await LireALArchiveAsync($"https://web.archive.org/web/{horodatage}id_/{url}", url, ct);
                if (html is null) return null;

                var texte = ExtraireTexte(html);
                if (EstUnDefi(html) || texte.Length < LongueurMinimale) return null;

                if (DateTime.UtcNow - copieDu > CopieTropAncienne) DemanderUneCopieFraiche(url);

                _logger.LogInformation(
                    "Veille referentiel : {Url} lue sur la copie de l'Internet Archive du {Copie:dd/MM/yyyy}.", url, copieDu);

                return new ReleveVeille(true, Empreinte(texte), copieDu);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Veille referentiel : copie de l'Internet Archive illisible pour {Url}.", url);
                return null;
            }
        }

        /// <summary>
        /// Un appel à l'Archive, au rythme qu'elle tolère. Null : elle a refusé
        /// trois fois, ou répondu une erreur qui ne se reprend pas (404…).
        /// </summary>
        private async Task<string?> LireALArchiveAsync(string adresse, string url, CancellationToken ct)
        {
            for (var essai = 0; ; essai++)
            {
                string? corps = null;
                var reprendre = false;

                await UnAppelALaFois.WaitAsync(ct);
                try
                {
                    var attente = _dernierAppel + EcartEntreAppels - DateTime.UtcNow;
                    if (attente > TimeSpan.Zero) await Task.Delay(attente, ct);

                    using var reponse = await _http.GetAsync(adresse, ct);
                    var code = (int)reponse.StatusCode;
                    if (reponse.IsSuccessStatusCode)
                    {
                        corps = await reponse.Content.ReadAsStringAsync(ct);
                        // La page « hors ligne » arrive parfois avec un statut 200.
                        reprendre = corps.Contains("Temporarily Offline", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        reprendre = code == 429 || code >= 500;
                    }
                }
                catch (HttpRequestException)
                {
                    reprendre = true;
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested)
                {
                    reprendre = true; // délai dépassé
                }
                finally
                {
                    _dernierAppel = DateTime.UtcNow;
                    UnAppelALaFois.Release();
                }

                if (!reprendre) return corps;
                if (essai >= AttentesAvantReprise.Length)
                {
                    _logger.LogWarning("Veille referentiel : l'Internet Archive refuse de repondre pour {Url}.", url);
                    return null;
                }

                await Task.Delay(AttentesAvantReprise[essai], ct);
            }
        }

        /// <summary>
        /// Demande à l'Archive de reprendre la page, sans attendre : la copie
        /// fraîche servira au prochain passage. Au plus dix par jour.
        /// </summary>
        private void DemanderUneCopieFraiche(string url)
        {
            lock (VerrouDemandes)
            {
                var aujourdhui = DateTime.UtcNow.Date;
                if (_jourDesDemandes != aujourdhui)
                {
                    _jourDesDemandes = aujourdhui;
                    _demandesDuJour = 0;
                }

                if (_demandesDuJour >= DemandesDeCopieParJour) return;
                _demandesDuJour += 1;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    using var reponse = await _http.GetAsync("https://web.archive.org/save/" + url);
                    _logger.LogInformation(
                        "Veille referentiel : copie fraiche demandee pour {Url} ({Code}).", url, (int)reponse.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Veille referentiel : demande de copie refusee pour {Url} ({Erreur}).", url, ex.Message);
                }
            });
        }

        private static string Empreinte(string texte) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texte)));

        private async Task<ReleveVeille> RelireEnDirectAsync(string url, CancellationToken ct)
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

                return new ReleveVeille(true, Empreinte(texte));
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
