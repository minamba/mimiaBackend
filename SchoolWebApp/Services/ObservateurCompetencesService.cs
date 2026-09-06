using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;


namespace SchoolWebApp.Api.Services
{
    public interface IObservateurCompetencesService
    {
        /// <summary>
        /// Analyse une séance terminée et met à jour la maîtrise de l'élève.
        /// Retourne le nombre d'observations réellement appliquées.
        /// </summary>
        Task<int> ObserverAsync(SeanceAObserver seance, CancellationToken ct = default);

        /// <summary>
        /// Analyse une séance désignée par son identifiant, puis avance son
        /// marqueur. Utilisé quand l'élève vient de quitter le cours.
        /// </summary>
        Task<int> ObserverSeanceAsync(int conversationId, CancellationToken ct = default);

        /// <summary>
        /// Vrai si la séance est assez longue et assez fournie pour qu'on en
        /// tire quelque chose. Une séance non exploitable n'est pas marquée
        /// comme analysée : elle pourra l'être une fois qu'elle se sera étoffée.
        /// </summary>
        bool Exploitable(SeanceAObserver seance);
    }

    /// <summary>
    /// Transforme une séance en observations de compétences.
    ///
    /// C'est la pièce qui manquait pour que le professeur ait une mémoire : sans
    /// elle, chaque séance repart de zéro et la promesse du produit — remonter
    /// d'un blocage de 6e jusqu'à une lacune de CM1 — n'est qu'une intention.
    ///
    /// L'analyse tourne APRÈS la séance, dans un worker : elle n'ajoute aucune
    /// latence à la conversation, où chaque seconde s'entend.
    /// </summary>
    public class ObservateurCompetencesService : IObservateurCompetencesService
    {
        /// <summary>Niveaux en amont proposés au rattachement. Cinq ans couvrent le primaire entier.</summary>
        private const int MargeAmont = 5;

        /// <summary>Un élève en avance touche parfois la notion de l'année suivante.</summary>
        private const int MargeAval = 1;

        /// <summary>En dessous, la séance est trop courte pour rien conclure.</summary>
        private const int MessagesMinimum = 4;

        /// <summary>
        /// Durée en deçà de laquelle on n'évalue rien.
        ///
        /// Le nombre de messages ne suffit pas : quatre échanges expédiés en
        /// deux minutes ne disent rien d'une compétence, et une observation
        /// pèse durablement sur le score. Mieux vaut ne rien écrire que d'écrire
        /// du bruit — une lacune inventée enverrait le professeur réexpliquer
        /// une notion déjà acquise.
        /// </summary>
        private static readonly TimeSpan DureeMinimum = TimeSpan.FromMinutes(10);

        /// <summary>Longueur d'un message dans la transcription analysée.</summary>
        private const int LongueurMessage = 600;

        private readonly AnthropicClient _client;
        private readonly IMaitriseRepository _maitrises;
        private readonly IConversationRepository _conversations;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;
        private readonly ILogger<ObservateurCompetencesService> _logger;

        public ObservateurCompetencesService(
            AnthropicClient client,
            IMaitriseRepository maitrises,
            IConversationRepository conversations,
            IOptions<OptionsClaude> options,
            IJournalClaudeRepository journal,
            ILogger<ObservateurCompetencesService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _maitrises = maitrises ?? throw new ArgumentNullException(nameof(maitrises));
            _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Exploitable(SeanceAObserver seance)
        {
            if (seance.Messages.Count < MessagesMinimum) return false;

            var duree = seance.Messages[^1].Date - seance.Messages[0].Date;
            return duree >= DureeMinimum;
        }

        public async Task<int> ObserverSeanceAsync(int conversationId, CancellationToken ct = default)
        {
            var seance = await _conversations.GetSeanceAObserverAsync(conversationId, ct);

            // Null : déjà analysée, ou sans message. Rien à faire, et surtout
            // pas de marqueur à avancer.
            if (seance is null) return 0;

            // Trop courte : on ne marque PAS comme analysée. L'élève peut
            // revenir dans la même conversation cet après-midi ; ces messages
            // feront alors partie d'une séance assez longue pour être lue.
            // Le contrôle ne coûte rien : aucun appel au modèle n'a lieu.
            if (!Exploitable(seance)) return 0;

            var appliquees = await ObserverAsync(seance, ct);

            // Le marqueur avance même sans observation retenue : sinon une
            // séance dont on ne peut rien tirer serait réanalysée à chaque
            // passage du worker, indéfiniment et à chaque fois facturée.
            await _conversations.MarquerObserveeAsync(conversationId, seance.Jusqua, ct);

            return appliquees;
        }

        public async Task<int> ObserverAsync(SeanceAObserver seance, CancellationToken ct = default)
        {
            if (!Exploitable(seance)) return 0;

            var candidates = (await _maitrises.GetCandidatesAsync(
                seance.MatiereId, seance.NiveauOrdre, MargeAmont, MargeAval,
                seance.NiveauCode, ct)).ToList();

            if (candidates.Count == 0)
            {
                // Pas de compétence au référentiel pour ce niveau : rien à
                // rattacher. C'est un trou de contenu, pas une erreur technique.
                _logger.LogWarning(
                    "Aucune competence au referentiel pour la matiere {MatiereId} au niveau {Niveau} : seance {SeanceId} non observee.",
                    seance.MatiereId, seance.NiveauLibelle, seance.ConversationId);
                return 0;
            }

            var reponse = await AppelerAsync(seance, candidates, ct);
            var observations = Analyser(reponse, candidates);

            if (observations.Count == 0) return 0;

            return await _maitrises.AppliquerObservationsAsync(
                seance.EleveId, observations, "Conversation", ct);
        }

        private async Task<string> AppelerAsync(
            SeanceAObserver seance, List<CompetenceCandidate> candidates, CancellationToken ct)
        {
            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 1500,
                System = new List<TextBlockParam> { new() { Text = Consigne(seance) } },
                Messages = new List<MessageParam>
                {
                    new() { Role = Role.User, Content = Corpus(seance, candidates) },
                },

                // Tâche de classement, pas de raisonnement : `low` suffit et
                // divise le coût d'une analyse qui tourne après chaque séance.
                OutputConfig = new OutputConfig { Effort = Effort.Low },
            };

            var texte = new StringBuilder();

            // EN DIFFUSION, L'USAGE ARRIVE EN DEUX FOIS.
            //
            // Le début du message porte les jetons d'entrée et de cache — ils
            // sont connus dès l'envoi. La fin porte ceux de sortie, une fois
            // qu'ils sont écrits. Ne lire que l'un des deux compterait la
            // moitié de l'appel, et toujours la même moitié.
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

            // Ce que cette observation a coûté. Sans cette ligne, le poste
            // dépense en silence : seul le dialogue portait ses jetons.
            await _journal.EnregistrerAsync(
                "observation-competences", modele, entree, sortie, cacheLu, cacheEcrit, ct: ct);

            return texte.ToString();
        }

        private static string Consigne(SeanceAObserver seance) => $$"""
            Tu analyses la séance de travail d'un élève pour en tirer un constat
            de compétences. Tu n'es pas le professeur : tu es l'observateur qui
            regarde ce qui s'est passé.

            L'élève s'appelle {{seance.ElevePrenom}} et il est en {{seance.NiveauLibelle}}.

            # Ta tâche

            Rattache ce qui s'est réellement passé aux compétences de la liste
            fournie, et pour chacune dis ce que tu as constaté :

            - "reussi"   : l'élève y arrive seul, ou après un simple rappel.
            - "hesitant" : il trouve, mais avec de l'aide, des détours, des
                           reprises. C'est le cas le plus fréquent, ne l'évite pas.
            - "echoue"   : il n'y arrive pas, ou son raisonnement est faux au fond.

            # Règles strictes

            N'utilise QUE les codes de la liste. Un code inventé sera rejeté.

            N'observe QUE ce que la séance montre. Si l'élève n'a fait que trois
            additions, tu ne peux rien dire de la géométrie. Une liste courte et
            juste vaut infiniment mieux qu'une liste longue et supposée.

            Trois à six observations au maximum. Au-delà, tu extrapoles.

            Regarde AUSSI les compétences des niveaux inférieurs : quand un élève
            bute, la cause est souvent en amont. Si ses additions échouent parce
            qu'il ne maîtrise pas le complément à 10, c'est le complément à 10
            qu'il faut observer, pas seulement l'addition.

            Si la séance ne permet aucun constat solide — bavardage, séance
            écourtée, aucun exercice — renvoie une liste vide. C'est une réponse
            valide et souvent la bonne.

            # Format

            Réponds UNIQUEMENT avec un objet JSON, sans texte autour ni balises
            de code :

            {
              "observations": [
                { "code": "CODE_EXACT_DE_LA_LISTE", "resultat": "reussi", "indice": "ce qui te le fait dire, en quelques mots" }
              ]
            }
            """;

        private static string Corpus(SeanceAObserver seance, List<CompetenceCandidate> candidates)
        {
            var texte = new StringBuilder();

            texte.AppendLine("# Compétences disponibles");
            texte.AppendLine();

            foreach (var groupe in candidates.GroupBy(c => c.NiveauLibelle))
            {
                texte.AppendLine($"## {groupe.Key}");
                foreach (var candidate in groupe)
                {
                    texte.AppendLine($"- {candidate.Code} : {candidate.Libelle}");
                }
                texte.AppendLine();
            }

            texte.AppendLine($"# Séance de {seance.ElevePrenom} — {seance.MatiereLibelle}");
            texte.AppendLine();

            foreach (var message in seance.Messages)
            {
                var contenu = Nettoyer(message.Contenu);
                if (string.IsNullOrWhiteSpace(contenu)) continue;

                texte.AppendLine($"{(message.Role == "assistant" ? "Professeur" : "Élève")} : {contenu}");
            }

            return texte.ToString();
        }

        /// <summary>
        /// L'ardoise est conservée ici, contrairement au bilan destiné au parent :
        /// c'est précisément dans les calculs posés que se lit ce que l'élève
        /// sait faire. Seules les balises disparaissent.
        /// </summary>
        private static string Nettoyer(string contenu)
        {
            // Le bloc d'évaluation part en entier, pas seulement ses balises :
            // il contient déjà un verdict, et l'observateur doit se prononcer
            // sur les échanges eux-mêmes, pas recopier la conclusion du
            // professeur.
            var texte = LecteurFiche.Retirer(LecteurRapport.Retirer(LecteurEvaluation.Retirer(contenu)))
                .Replace("[ARDOISE]", " ")
                .Replace("[/ARDOISE]", " ")
                .Replace("[ALERTE_ADULTE]", " ")
                .Replace("[FIN_SEANCE]", " ")
                .Trim();

            return texte.Length > LongueurMessage ? texte[..LongueurMessage] + "…" : texte;
        }

        private List<ObservationCompetence> Analyser(
            string reponse, List<CompetenceCandidate> candidates)
        {
            var resultats = new List<ObservationCompetence>();

            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return resultats;

            try
            {
                using var document = JsonDocument.Parse(reponse[debut..(fin + 1)]);

                if (!document.RootElement.TryGetProperty("observations", out var observations)
                    || observations.ValueKind != JsonValueKind.Array)
                {
                    return resultats;
                }

                var connus = candidates
                    .Where(c => c.Code is not null)
                    .Select(c => c.Code!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var element in observations.EnumerateArray())
                {
                    var code = element.TryGetProperty("code", out var c) ? c.GetString() : null;
                    if (code is null || !connus.Contains(code)) continue;

                    var brut = element.TryGetProperty("resultat", out var r) ? r.GetString() : null;
                    var indice = element.TryGetProperty("indice", out var i) ? i.GetString() : null;

                    resultats.Add(new ObservationCompetence(code, Convertir(brut), indice));
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Reponse d'observation illisible, seance ignoree.");
            }

            return resultats;
        }

        /// <summary>
        /// Une valeur inattendue devient « hésitant » plutôt que d'être rejetée :
        /// c'est le constat le plus neutre, il ne fausse ni dans un sens ni dans
        /// l'autre.
        /// </summary>
        private static ResultatObservation Convertir(string? valeur) => valeur?.ToLowerInvariant() switch
        {
            "reussi" or "réussi" => ResultatObservation.Reussi,
            "echoue" or "échoué" or "echec" => ResultatObservation.Echoue,
            _ => ResultatObservation.Hesitant,
        };
    }
}
