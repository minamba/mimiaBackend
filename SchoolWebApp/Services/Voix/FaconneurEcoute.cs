using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>
    /// ÉCRIT LA LENTEUR DANS LE TEXTE, avant qu'il parte à la voix.
    ///
    /// Trois essais le 16/09/2026, et chacun a fermé une porte. Le facteur
    /// `speed` ralentit tout — syllabes et silences — et sonne comme une bande
    /// étirée. La consigne de diction sonne naturel mais ne produit rien de
    /// mesurable. Reste ce qu'un modèle de parole lit VRAIMENT : la
    /// ponctuation. Il marque un silence sur des points de suspension, et il
    /// détache ce qu'un point médian sépare — avec sa prosodie normale.
    ///
    /// Ce que Camara a demandé, mot pour mot : « en très lent, un découpage en
    /// syllabes en restant naturel, pas un audio mécanique qu'on aurait
    /// étiré ; en lent, des pauses entre les mots ».
    /// </summary>
    public interface IFaconneurEcoute
    {
        /// <summary>
        /// Le passage tel qu'il doit être lu à cette vitesse : espacé (« lent »)
        /// ou découpé en syllabes puis espacé (« très lent »). Les autres
        /// vitesses rendent le texte tel quel.
        /// </summary>
        Task<string> FaconnerAsync(string texte, string langue, string vitesse, CancellationToken ct);
    }

    public class FaconneurEcoute : IFaconneurEcoute
    {
        /// <summary>Le silence entre deux mots, tel que le modèle le lit.</summary>
        private const string Pause = " … ";

        /// <summary>Le séparateur de syllabes — jamais prononcé, voir la consigne de voix.</summary>
        private const char Syllabe = '·';

        /// <summary>
        /// Au-delà, le découpage n'a plus le temps d'arriver : l'élève attend
        /// devant un écran muet. On lit alors le passage seulement espacé —
        /// moins bien que prévu, mais tout de suite.
        /// </summary>
        private static readonly TimeSpan DelaiDecoupage = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Un passage est réentendu plusieurs fois — relecture, changement de
        /// vitesse, phrase repassée seule. Le découper une fois suffit. Borné,
        /// sans finesse : on vide tout au-delà, le prochain passage remplit.
        /// </summary>
        private const int TailleCacheMax = 500;

        private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

        private static readonly Regex Espaces = new(@"\s+", RegexOptions.Compiled);

        private readonly AnthropicClient _claude;
        private readonly OptionsClaude _options;
        private readonly IServiceScopeFactory _portees;
        private readonly ILogger<FaconneurEcoute> _logger;

        public FaconneurEcoute(
            AnthropicClient claude,
            IOptions<OptionsClaude> options,
            IServiceScopeFactory portees,
            ILogger<FaconneurEcoute> logger)
        {
            _claude = claude ?? throw new ArgumentNullException(nameof(claude));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _portees = portees ?? throw new ArgumentNullException(nameof(portees));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> FaconnerAsync(string texte, string langue, string vitesse, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(texte)) return texte;

            return vitesse switch
            {
                "lent" => EspacerLesMots(texte),
                "tres_lent" => EspacerLesMots(await DecouperEnSyllabesAsync(texte, langue, ct)),
                "rapide" => ResserrerLesPauses(texte),
                _ => texte,
            };
        }

        /// <summary>
        /// LE RAPIDE, SYMÉTRIQUE DU LENT — Camara, le 16/09/2026 : « pour la
        /// vitesse rapide, on réduit le temps de pause entre les mots par
        /// rapport au mode normal ». Une pointe d'accélération (1,08) avait
        /// été essayée : « un côté robotique ». La bande ne s'étire pas plus
        /// dans un sens que dans l'autre.
        ///
        /// En normal, les pauses viennent de la ponctuation. On retire donc
        /// celle qui SÉPARE À L'INTÉRIEUR d'une phrase — virgules,
        /// points-virgules, deux-points, points de suspension — et le modèle
        /// enchaîne les groupes de sens sans s'arrêter, avec sa voix normale.
        /// Les points, points d'exclamation et d'interrogation restent : ils
        /// portent l'intonation de fin de phrase, et sans eux le texte
        /// deviendrait une seule phrase interminable.
        ///
        /// Aucun mot ne change : l'élève n'a pas le texte sous les yeux, il
        /// entend les mêmes mots, simplement plus liés.
        /// </summary>
        public static string ResserrerLesPauses(string texte)
        {
            var resserre = PonctuationInterne.Replace(texte, string.Empty);
            return Espaces.Replace(resserre, " ").Trim();
        }

        /// <summary>
        /// La ponctuation de milieu de phrase. Le tiret et l'apostrophe n'en
        /// font pas partie : ils appartiennent aux mots (« well-known »,
        /// « don't »).
        /// </summary>
        private static readonly Regex PonctuationInterne = new(@"[,;:…]|\.{3}", RegexOptions.Compiled);

        /// <summary>
        /// Une pause entre chaque mot. Après une ponctuation, la pause existe
        /// déjà : on n'en empile pas une seconde, « table. … The » se lirait
        /// avec un blanc double.
        /// </summary>
        public static string EspacerLesMots(string texte)
        {
            var mots = Espaces.Split(texte.Trim());
            if (mots.Length < 2) return texte.Trim();

            var sortie = new System.Text.StringBuilder(mots[0]);
            for (var i = 1; i < mots.Length; i++)
            {
                var precedent = mots[i - 1];
                var finitParPonctuation = precedent.Length > 0
                    && ".!?…,;:".Contains(precedent[^1]);

                sortie.Append(finitParPonctuation ? " " : Pause).Append(mots[i]);
            }

            return sortie.ToString();
        }

        /// <summary>
        /// Le découpage en syllabes est demandé à un petit modèle de texte, pas
        /// calculé par règle : en anglais, « table » fait deux syllabes et
        /// « science » aussi, et aucune règle d'orthographe ne le sait. Le
        /// chinois n'est pas découpé — chaque caractère y est déjà une syllabe.
        ///
        /// LA RÉPONSE EST VÉRIFIÉE AVANT D'ÊTRE CRUE : mêmes mots, dans le même
        /// ordre, une fois les points médians retirés. Un modèle qui reformule,
        /// traduit ou « corrige » le passage ferait entendre à l'élève autre
        /// chose que le texte de l'exercice — on relit alors l'original.
        /// </summary>
        private async Task<string> DecouperEnSyllabesAsync(string texte, string langue, CancellationToken ct)
        {
            var nomLangue = NomLangue(langue);
            if (nomLangue is null) return texte;

            var cle = $"{langue}{texte}";
            if (_cache.TryGetValue(cle, out var connu)) return connu;

            try
            {
                using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
                delai.CancelAfter(DelaiDecoupage);

                var reponse = await _claude.Messages.Create(
                    new MessageCreateParams
                    {
                        Model = _options.ModeleTranscription,
                        MaxTokens = Math.Min(1024, texte.Length * 2 + 64),
                        Messages = new List<MessageParam>
                        {
                            new() { Role = Role.User, Content = Consigne(nomLangue, texte) },
                        },
                    },
                    cancellationToken: delai.Token);

                await JournaliserAsync(reponse, ct);

                var decoupe = string.Join("\n", reponse.Content
                    .Where(b => b.TryPickText(out _))
                    .Select(b => { b.TryPickText(out var t); return t!.Text; }))
                    .Trim();

                if (!MemesMots(texte, decoupe))
                {
                    _logger.LogWarning(
                        "Decoupage syllabique ecarte : le texte rendu ne reprend pas les mots du passage.");
                    return texte;
                }

                if (_cache.Count >= TailleCacheMax) _cache.Clear();
                _cache[cle] = decoupe;

                return decoupe;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Decoupage syllabique trop lent : passage lu sans syllabes.");
                return texte;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Decoupage syllabique impossible : passage lu sans syllabes.");
                return texte;
            }
        }

        private static string Consigne(string nomLangue, string texte) => $"""
            Découpe chaque mot du texte ci-dessous en syllabes, telles qu'on les
            prononce en {nomLangue}, en séparant les syllabes par le point médian « · ».

            Règles, sans exception :
            - ne change, n'ajoute et ne retire AUCUN mot ; garde la ponctuation,
              les majuscules et l'ordre ;
            - un mot d'une seule syllabe reste tel quel ;
            - le « · » ne se place qu'À L'INTÉRIEUR d'un mot, jamais avant ni après ;
            - réponds avec le texte découpé et rien d'autre : pas de commentaire,
              pas de guillemets, pas d'explication.

            Texte :
            {texte}
            """;

        private static string? NomLangue(string langue) => langue.ToLowerInvariant() switch
        {
            "en" => "anglais",
            "fr" => "français",
            "es" => "espagnol",
            "de" => "allemand",
            "it" => "italien",
            _ => null,
        };

        /// <summary>Les mêmes mots, dans le même ordre, points médians retirés.</summary>
        private static bool MemesMots(string original, string decoupe)
        {
            static IEnumerable<string> Mots(string t) =>
                Espaces.Split(t.Trim()).Where(m => m.Length > 0);

            return Mots(original).SequenceEqual(
                Mots(decoupe.Replace(Syllabe.ToString(), string.Empty)),
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Ce que le découpage a coûté. Le journal vit dans une portée courte ;
        /// ce service est un singleton, on ouvre donc une portée le temps
        /// d'écrire, comme les workers. Une écriture qui échoue ne prive pas
        /// l'élève de sa lecture.
        /// </summary>
        private async Task JournaliserAsync(Message reponse, CancellationToken ct)
        {
            try
            {
                using var portee = _portees.CreateScope();
                var journal = portee.ServiceProvider.GetRequiredService<IJournalClaudeRepository>();

                await journal.EnregistrerAsync(
                    "decoupage-syllabes", reponse.Model,
                    reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                    reponse.Usage.CacheReadInputTokens ?? 0,
                    reponse.Usage.CacheCreationInputTokens ?? 0,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Journal des jetons injoignable pour le decoupage syllabique.");
            }
        }
    }
}
