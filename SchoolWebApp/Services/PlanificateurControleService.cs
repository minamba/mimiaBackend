using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services
{
    public interface IPlanificateurControleService
    {
        /// <summary>
        /// Déduit le programme d'un contrôle à partir de son sujet, et le pose
        /// sur le contrôle. Renvoie le nombre de notions retenues.
        /// </summary>
        Task<int> PoserProgrammeAsync(
            int eleveId, int controleId, int matiereId, int? niveauScolaireId,
            string? sujet, CancellationToken ct = default);

        /// <summary>
        /// Rapproche des libellés écrits librement par le professeur des
        /// notions du référentiel. Rend, par libellé rapproché, la notion
        /// connue ; un libellé absent du résultat n'a rien trouvé.
        /// </summary>
        Task<IReadOnlyDictionary<string, NotionDeclaree>> RapprocherAsync(
            IReadOnlyList<string> libelles, IReadOnlyList<CompetenceCandidate> referentiel,
            CancellationToken ct = default);
    }

    /// <summary>
    /// « Contrôle sur le théorème de Thalès » → les notions qu'il faut tenir
    /// pour le réussir.
    ///
    /// POURQUOI CE SERVICE EXISTE. Le programme d'un contrôle ne se remplissait
    /// que si un professeur écrivait un bloc pendant une séance. Or un contrôle
    /// posé depuis le calendrier par un parent n'a vu AUCUN professeur : la
    /// barre restait à zéro et « on ne sait pas ce qu'il y a dessus » pour
    /// toujours — c'est-à-dire que la mécanique paraissait morte à l'endroit
    /// exact où elle devait convaincre.
    ///
    /// Relevé par Camara le 13/09/2026, avec l'argument qui tranche : un
    /// professeur CONNAÎT le programme de la classe. Un sujet de contrôle ne
    /// sort jamais de ce qu'il sait enseigner, et « Thalès », ce n'est pas un
    /// milliard de choses. Il n'y a donc rien à deviner, seulement à
    /// rapprocher.
    ///
    /// LE MODÈLE NE CHOISIT QUE DANS UNE LISTE FERMÉE — les notions du
    /// référentiel au niveau de cet élève, dans cette matière. Un code inventé
    /// est rejeté, exactement comme dans l'observateur de compétences : rien
    /// n'entre en base qui ne soit déjà au programme officiel.
    ///
    /// LES PRÉREQUIS N'Y ENTRENT PAS, ET C'EST VOULU. Le projet tient un
    /// graphe de prérequis entre compétences (`CompetencePrerequis`), et on
    /// pourrait verser dans le programme d'un contrôle ce qui le sous-tend —
    /// la proportionnalité derrière Thalès, par exemple. Tranché par Camara le
    /// 13/09/2026 : la fiche reste sur LE PROGRAMME OFFICIEL du contrôle, ce
    /// que le professeur de l'école va réellement demander.
    ///
    /// Ce n'est pas un angle mort pour autant : le professeur reçoit à chaque
    /// séance « ## Lacunes connues » avec les scores, tous niveaux confondus
    /// (voir `AgentPedagogiqueService`), et la consigne de remonter dessus —
    /// « une lacune d'un niveau antérieur est presque toujours la vraie
    /// cause ». Les prérequis sont donc traités, simplement pas affichés :
    /// une barre de préparation allongée de notions qui ne tomberont pas au
    /// contrôle mesurerait autre chose que ce qu'elle annonce.
    /// </summary>
    public class PlanificateurControleService : IPlanificateurControleService
    {
        /// <summary>
        /// Un contrôle porte sur un chapitre, pas sur un trimestre. Au-delà,
        /// c'est que le modèle a recopié le programme de l'année — et une
        /// barre de progression étalée sur vingt notions ne bouge jamais.
        /// </summary>
        private const int MaxNotions = 8;

        private readonly AnthropicClient _client;
        private readonly IMaitriseRepository _maitrises;
        private readonly IControleScolaireRepository _controles;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;
        private readonly ILogger<PlanificateurControleService> _logger;

        public PlanificateurControleService(
            AnthropicClient client,
            IMaitriseRepository maitrises,
            IControleScolaireRepository controles,
            IOptions<OptionsClaude> options,
            IJournalClaudeRepository journal,
            ILogger<PlanificateurControleService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _maitrises = maitrises ?? throw new ArgumentNullException(nameof(maitrises));
            _controles = controles ?? throw new ArgumentNullException(nameof(controles));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> PoserProgrammeAsync(
            int eleveId, int controleId, int matiereId, int? niveauScolaireId,
            string? sujet, CancellationToken ct = default)
        {
            // Sans sujet, il n'y a rien à rapprocher : le professeur posera
            // les notions à la première préparation.
            if (string.IsNullOrWhiteSpace(sujet)) return 0;
            if (niveauScolaireId is not int niveau) return 0;

            var candidates = (await _maitrises.GetNotionsDuNiveauAsync(matiereId, niveau, ct)).ToList();

            // Six niveaux n'ont aucune compétence au référentiel (voies pro,
            // techno, 3e prépa-métiers). Rien à proposer : ce n'est pas une
            // panne, c'est un trou de contenu.
            if (candidates.Count == 0)
            {
                _logger.LogInformation(
                    "Aucune notion au referentiel pour la matiere {MatiereId} au niveau {NiveauId} : "
                    + "programme du controle {ControleId} laisse vide.",
                    matiereId, niveau, controleId);
                return 0;
            }

            var reponse = await AppelerAsync(sujet, candidates, ct);
            var notions = Analyser(reponse, candidates);

            if (notions.Count == 0) return 0;

            return await _controles.AjouterNotionsAsync(
                eleveId, controleId, matiereId, notions, travaillee: false, ct);
        }

        /// <summary>
        /// « de repérer une personnification » → la notion du référentiel qui
        /// dit la même chose, ou rien — Camara, le 13/10/2026.
        ///
        /// MÊME LISTE FERMÉE, MÊME GARDE : un code absent de la liste est
        /// ignoré. Le modèle peut aussi ne rien rendre pour un libellé : une
        /// notion qui n'est pas au programme de la classe reste en texte libre,
        /// comme avant. Un appel pour tous les libellés à la fois.
        /// </summary>
        public async Task<IReadOnlyDictionary<string, NotionDeclaree>> RapprocherAsync(
            IReadOnlyList<string> libelles, IReadOnlyList<CompetenceCandidate> referentiel,
            CancellationToken ct = default)
        {
            var resultat = new Dictionary<string, NotionDeclaree>(StringComparer.OrdinalIgnoreCase);
            if (libelles.Count == 0 || referentiel.Count == 0) return resultat;

            var candidates = referentiel.ToList();
            var corpus = new StringBuilder();
            corpus.AppendLine("LIBELLÉS ÉCRITS PAR LE PROFESSEUR :");
            foreach (var libelle in libelles) corpus.AppendLine($"- {libelle}");
            corpus.AppendLine();
            corpus.AppendLine("NOTIONS DU PROGRAMME (choisis uniquement là-dedans) :");
            foreach (var candidate in candidates)
            {
                corpus.AppendLine($"- {candidate.Code} | {candidate.Domaine} | {candidate.Libelle}");
            }

            var reponse = await AppelerAsync(ConsigneRapprochement, corpus.ToString(), "rattachement-notions", ct);

            var connus = candidates
                .Where(c => c.Code is not null)
                .ToDictionary(c => c.Code!, c => c, StringComparer.OrdinalIgnoreCase);

            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return resultat;

            try
            {
                using var document = JsonDocument.Parse(reponse[debut..(fin + 1)]);

                if (!document.RootElement.TryGetProperty("rapprochements", out var lignes)
                    || lignes.ValueKind != JsonValueKind.Array)
                {
                    return resultat;
                }

                foreach (var element in lignes.EnumerateArray())
                {
                    var libelle = element.TryGetProperty("libelle", out var l) ? l.GetString() : null;
                    var code = element.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String
                        ? c.GetString()
                        : null;

                    if (libelle is null || code is null || !connus.TryGetValue(code, out var candidate)) continue;

                    // Le libellé rendu doit être l'un de ceux demandés : le
                    // modèle ne rapproche pas des notions qu'on ne lui a pas
                    // données.
                    var demande = libelles.FirstOrDefault(
                        d => string.Equals(d, libelle, StringComparison.OrdinalIgnoreCase)
                            || LibelleNotion.CleSouple(d) == LibelleNotion.CleSouple(libelle));
                    if (demande is null) continue;

                    resultat[demande] = new NotionDeclaree(candidate.Id, candidate.Libelle ?? code);

                    _logger.LogInformation(
                        "Notion rapprochee du referentiel : «{Libelle}» -> {Code}.", demande, code);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Reponse illisible du rapprochement de notions.");
            }

            return resultat;
        }

        private const string ConsigneRapprochement = """
            Un professeur a nommé, avec ses mots, des notions qu'il vient de
            travailler avec un élève. On te donne ses LIBELLÉS et la LISTE DES
            NOTIONS du programme officiel de la classe, dans cette matière.

            Pour chaque libellé, tu dis quelle notion de la liste désigne LA
            MÊME CHOSE — la même compétence, pas une compétence voisine.

            Règles :
            - N'invente AUCUN code. Tout code absent de la liste est ignoré.
            - « de repérer une personnification » et « Repérer comparaison,
              métaphore et personnification » sont la même notion ; « les
              rimes » et « Analyser un poème : vers, strophes, rimes, rythme »
              aussi. « Les fractions » et « Simplifier une fraction » ne le
              sont pas : le premier est un chapitre, pas une compétence.
            - Si rien dans la liste ne dit la même chose, rends null pour ce
              libellé : une notion hors programme reste telle quelle. Mieux
              vaut null qu'un rapprochement approximatif.
            - Recopie chaque libellé exactement comme il t'a été donné.

            Réponds en JSON strict, sans autre texte :
            {"rapprochements":[{"libelle":"de repérer une personnification","code":"FR_5E_LECT_FIGURES"},{"libelle":"les figures de style","code":null}]}
            """;

        private Task<string> AppelerAsync(
            string sujet, List<CompetenceCandidate> candidates, CancellationToken ct) =>
            AppelerAsync(Consigne, Corpus(sujet, candidates), "programme-controle", ct);

        private async Task<string> AppelerAsync(
            string consigne, string corpus, string origine, CancellationToken ct)
        {
            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 800,
                System = new List<TextBlockParam> { new() { Text = consigne } },
                Messages = new List<MessageParam>
                {
                    new() { Role = Role.User, Content = corpus },
                },

                // Choisir dans une liste, pas raisonner : `low` suffit, et
                // cet appel est payé à chaque contrôle créé.
                OutputConfig = new OutputConfig { Effort = Effort.Low },
            };

            var texte = new StringBuilder();
            long entree = 0, sortie = 0, cacheLu = 0, cacheEcrit = 0;
            string? modele = null;

            await foreach (var evenement in _client.Messages.CreateStreaming(parametres, cancellationToken: ct))
            {
                if (evenement.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var bloc))
                {
                    texte.Append(bloc.Text);
                }
                else if (evenement.TryPickStart(out var debut))
                {
                    modele = debut.Message.Model;
                    entree = debut.Message.Usage.InputTokens;
                    cacheLu = debut.Message.Usage.CacheReadInputTokens ?? 0;
                    cacheEcrit = debut.Message.Usage.CacheCreationInputTokens ?? 0;
                    sortie = debut.Message.Usage.OutputTokens;
                }
                else if (evenement.TryPickDelta(out var fin))
                {
                    sortie = fin.Usage.OutputTokens;
                }
            }

            await _journal.EnregistrerAsync(
                origine, modele, entree, sortie, cacheLu, cacheEcrit, ct: ct);

            return texte.ToString();
        }

        private const string Consigne = """
            Tu es un professeur qui prépare un élève à un contrôle. On te donne
            le SUJET du contrôle, tel que l'élève ou son parent l'a écrit, et la
            LISTE DES NOTIONS de son programme officiel dans cette matière.

            Tu choisis, dans cette liste et seulement dans cette liste, les
            notions qu'il faut tenir pour réussir ce contrôle.

            Règles :
            - De 2 à 6 notions. Un contrôle porte sur un chapitre, pas sur un
              trimestre. Si tu en retiens douze, c'est que tu as recopié le
              programme au lieu de choisir.
            - N'invente AUCUN code. Tout code absent de la liste est ignoré.
            - Si le sujet est trop vague pour décider (« contrôle de maths »),
              rends une liste VIDE plutôt que de prendre au hasard : le
              professeur posera les notions à la première séance.
            - Tu ne juges pas le niveau de l'élève, tu ne sais rien de lui. Tu
              dis seulement ce que le contrôle demande.

            Réponds en JSON strict, sans autre texte :
            {"notions":[{"code":"MATH_4E_THALES_APPLIQUER"}]}
            """;

        private static string Corpus(string sujet, List<CompetenceCandidate> candidates)
        {
            var liste = new StringBuilder();

            foreach (var candidate in candidates)
            {
                liste.AppendLine($"- {candidate.Code} | {candidate.Domaine} | {candidate.Libelle}");
            }

            return $"""
                SUJET DU CONTRÔLE :
                {sujet}

                NOTIONS DU PROGRAMME (choisis uniquement là-dedans) :
                {liste}
                """;
        }

        private List<NotionDeclaree> Analyser(string reponse, List<CompetenceCandidate> candidates)
        {
            var retenues = new List<NotionDeclaree>();

            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return retenues;

            try
            {
                using var document = JsonDocument.Parse(reponse[debut..(fin + 1)]);

                if (!document.RootElement.TryGetProperty("notions", out var notions)
                    || notions.ValueKind != JsonValueKind.Array)
                {
                    return retenues;
                }

                // MÊME GARDE QUE L'OBSERVATEUR : un code inventé ne crée rien.
                var connus = candidates
                    .Where(c => c.Code is not null)
                    .ToDictionary(c => c.Code!, c => c, StringComparer.OrdinalIgnoreCase);

                foreach (var element in notions.EnumerateArray())
                {
                    if (retenues.Count >= MaxNotions) break;

                    var code = element.TryGetProperty("code", out var c) ? c.GetString() : null;
                    if (code is null || !connus.TryGetValue(code, out var candidate)) continue;

                    if (retenues.Any(n => n.CompetenceId == candidate.Id)) continue;

                    retenues.Add(new NotionDeclaree(candidate.Id, candidate.Libelle ?? code));
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Reponse illisible du planificateur de controle.");
            }

            return retenues;
        }
    }
}
