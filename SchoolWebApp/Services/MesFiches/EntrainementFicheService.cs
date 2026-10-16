using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.MesFiches
{
    /// <summary>
    /// FLASH CARDS ET QUIZ SUR UNE FICHE — Camara, le 16/10/2026 : « pour
    /// chaque fiche, des flash cards et des quiz ».
    ///
    /// UN SEUL APPEL SERT AUX DEUX : dix questions, chacune avec sa réponse
    /// (le dos de la carte), trois leurres (les choix faux du quiz) et une
    /// explication. Écrit À PARTIR DE LA FICHE, pas du cours : la fiche est
    /// déjà du texte (≈ 500 à 1 500 jetons) ; renvoyer les pages coûterait dix
    /// fois plus. Mesuré le 16/10/2026 : ≈ 1,4 centime avec le modèle du
    /// dialogue.
    ///
    /// UNE FOIS PAR FICHE : généré au premier « Me tester », puis gardé avec la
    /// fiche (colonne `quiz`) — rejouer ne coûte rien. Un verrou par fiche :
    /// deux clics rapprochés ne paient pas deux appels.
    ///
    /// Hors quota : le prix d'un quiz est une fraction de celui d'un cours.
    /// Le journal le range avec les fiches (`fiches-quiz`).
    /// </summary>
    public class EntrainementFicheService
    {
        private const int MaxTokens = 4000;
        public const int QuestionsMin = 4;
        public const int QuestionsMax = 12;

        private static readonly ConcurrentDictionary<int, SemaphoreSlim> Verrous = new();

        private const string Systeme = """
            Tu aides un élève à réviser SA fiche de révision en se testant. Tu réponds
            uniquement par le JSON demandé, sans texte autour.
            """;

        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;

        public EntrainementFicheService(AnthropicClient client, IOptions<OptionsClaude> options, IJournalClaudeRepository journal)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        }

        /// <summary>Le quiz de la fiche : celui gardé, sinon écrit maintenant. Null si la fiche n'est pas à l'élève ou si l'écriture rate.</summary>
        public async Task<JsonNode?> LireOuCreerAsync(IFichesDeCoursRepository depot, Proprietaire proprietaire, int ficheId, CancellationToken ct)
        {
            var fiche = await depot.GetPourEntrainementAsync(proprietaire, ficheId, ct);
            if (fiche is null) return null;
            if (Lire(fiche.Quiz) is { } garde) return garde;

            var verrou = Verrous.GetOrAdd(ficheId, _ => new SemaphoreSlim(1, 1));
            await verrou.WaitAsync(ct);
            try
            {
                // Écrit pendant qu'on attendait le verrou : on le prend tel quel.
                fiche = await depot.GetPourEntrainementAsync(proprietaire, ficheId, ct);
                if (fiche is null) return null;
                if (Lire(fiche.Quiz) is { } entreTemps) return entreTemps;

                var quiz = await EcrireAsync(fiche, ct);
                if (quiz is null) return null;

                await depot.EnregistrerEntrainementAsync(ficheId, quiz.ToJsonString(), ct);
                return quiz;
            }
            finally
            {
                verrou.Release();
            }
        }

        /// <summary>
        /// « ME TESTER » SUR TOUT UN COURS (16/10/2026, depuis sa carte) : les
        /// questions de toutes ses fiches, chacune marquée de sa fiche. Les quiz
        /// manquants s'écrivent en parallèle, quatre à la fois — chacun dans sa
        /// propre portée (un contexte de base ne se partage pas entre deux
        /// tâches). Une fiche qui rate ne prive pas le cours des autres.
        /// </summary>
        public async Task<JsonObject?> LireOuCreerCoursAsync(
            IServiceScopeFactory portees, Proprietaire proprietaire, int coursId, CancellationToken ct)
        {
            IReadOnlyList<FicheDuCours> fiches;
            using (var portee = portees.CreateScope())
            {
                var cours = await portee.ServiceProvider.GetRequiredService<IFichesDeCoursRepository>().GetAsync(proprietaire, coursId, ct);
                if (cours is null) return null;
                fiches = cours.Fiches;
            }

            if (fiches.Count == 0) return null;

            using var limite = new SemaphoreSlim(4);
            var resultats = await Task.WhenAll(fiches.Select(async f =>
            {
                await limite.WaitAsync(ct);
                try
                {
                    using var portee = portees.CreateScope();
                    var depot = portee.ServiceProvider.GetRequiredService<IFichesDeCoursRepository>();
                    return (Fiche: f, Quiz: await LireOuCreerAsync(depot, proprietaire, f.Id, ct));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return (Fiche: f, Quiz: (JsonNode?)null);
                }
                finally
                {
                    limite.Release();
                }
            }));

            var toutes = new JsonArray();
            foreach (var (f, quiz) in resultats)
            {
                foreach (var q in quiz?["questions"]?.AsArray() ?? [])
                {
                    if (q?.DeepClone() is JsonObject copie)
                    {
                        copie["fiche"] = f.Titre;
                        toutes.Add(copie);
                    }
                }
            }

            return toutes.Count == 0 ? null : new JsonObject { ["questions"] = toutes };
        }

        private async Task<JsonObject?> EcrireAsync(FichePourEntrainement fiche, CancellationToken ct)
        {
            var consigne = ConsignesFiches.Entrainement(fiche.ClasseLibelle, fiche.Rang, fiche.MatiereLibelle, fiche.Titre, fiche.Contenu);
            var effort = AgentPedagogiqueService.ConvertirEffort(_options.Effort(TypeTache.Dialogue));

            var reponse = await _client.Messages.Create(
                new MessageCreateParams
                {
                    Model = _options.Modele(TypeTache.Dialogue),
                    MaxTokens = MaxTokens,
                    System = new List<TextBlockParam> { new() { Text = Systeme } },
                    Messages = new List<MessageParam> { new() { Role = Role.User, Content = consigne } },
                    OutputConfig = new OutputConfig { Effort = effort },
                },
                cancellationToken: ct);

            await _journal.EnregistrerAsync(
                "fiches-quiz", reponse.Model,
                reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                reponse.Usage.CacheReadInputTokens ?? 0,
                reponse.Usage.CacheCreationInputTokens ?? 0,
                reference: $"fiche:{fiche.Id}",
                ct: ct);

            var texte = string.Join("\n", reponse.Content
                .Select(b => b.TryPickText(out var t) ? t.Text : null)
                .OfType<string>());

            return Verifier(ContenuFiche.ObjetJson(texte));
        }

        /// <summary>
        /// Ne garde que des questions complètes : une question, une réponse,
        /// trois leurres distincts et différents de la réponse. Moins de quatre
        /// questions valables : on n'en garde aucune.
        /// </summary>
        public static JsonObject? Verifier(JsonObject? brut)
        {
            if (brut?["questions"] is not JsonArray liste) return null;

            var propres = new JsonArray();
            foreach (var element in liste)
            {
                if (propres.Count >= QuestionsMax) break;
                if (element is not JsonObject q
                    || ContenuFiche.Texte(q["question"], 300) is not { } question
                    || ContenuFiche.Texte(q["reponse"], 200) is not { } reponse)
                {
                    continue;
                }

                var leurres = new List<string>();
                if (q["leurres"] is JsonArray bruts)
                {
                    foreach (var l in bruts)
                    {
                        if (ContenuFiche.Texte(l, 200) is { } leurre
                            && !string.Equals(leurre, reponse, StringComparison.OrdinalIgnoreCase)
                            && !leurres.Contains(leurre, StringComparer.OrdinalIgnoreCase))
                        {
                            leurres.Add(leurre);
                        }
                    }
                }

                if (leurres.Count < 3) continue;

                var propre = new JsonObject
                {
                    ["question"] = question,
                    ["reponse"] = reponse,
                    ["leurres"] = new JsonArray(leurres.Take(3).Select(l => (JsonNode)JsonValue.Create(l)!).ToArray()),
                };
                if (ContenuFiche.Texte(q["explication"], 300) is { } explication) propre["explication"] = explication;
                propres.Add(propre);
            }

            return propres.Count < QuestionsMin ? null : new JsonObject { ["questions"] = propres };
        }

        private static JsonNode? Lire(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonNode.Parse(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
