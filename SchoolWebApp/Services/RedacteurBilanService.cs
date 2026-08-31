using System.Text;
using SchoolWebApp.Domain.Repositories;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services
{
    public interface IRedacteurBilanService
    {
        /// <summary>
        /// Fait rédiger le bilan par le professeur, à partir des échanges de la
        /// semaine. Renseigne <c>Remarque</c>, <c>Travaille</c> et
        /// <c>Difficultes</c> ; ne lève jamais — un bilan sans commentaire vaut
        /// mieux qu'un mail non envoyé.
        /// </summary>
        Task RedigerAsync(BilanEleve bilan, CancellationToken ct = default);
    }

    /// <summary>
    /// Le professeur écrit lui-même le bilan de la semaine.
    ///
    /// C'est ce qui distingue ce mail d'un relevé de compteurs : « il a passé
    /// 40 minutes en maths » n'apprend rien à un parent, « il bloque encore sur
    /// le passage à la dizaine, mais il pose ses calculs seul maintenant » lui
    /// donne de quoi parler avec son enfant.
    /// </summary>
    public class RedacteurBilanService : IRedacteurBilanService
    {
        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;
        private readonly ILogger<RedacteurBilanService> _logger;

        public RedacteurBilanService(
            AnthropicClient client,
            IOptions<OptionsClaude> options,
            IJournalClaudeRepository journal,
            ILogger<RedacteurBilanService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RedigerAsync(BilanEleve bilan, CancellationToken ct = default)
        {
            if (!bilan.Actif || bilan.Matieres.Count == 0) return;

            try
            {
                var reponse = await AppelerAsync(bilan, ct);
                Appliquer(bilan, reponse);
            }
            catch (Exception ex)
            {
                // Un bilan sans commentaire reste utile : les chiffres et les
                // matières travaillées sont déjà là. On n'annule pas l'envoi.
                _logger.LogError(ex, "Redaction du bilan impossible pour l'eleve {EleveId}.", bilan.EleveId);
            }
        }

        private async Task<string> AppelerAsync(BilanEleve bilan, CancellationToken ct)
        {
            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 2000,
                System = new List<TextBlockParam> { new() { Text = Consigne(bilan) } },
                Messages = new List<MessageParam>
                {
                    new() { Role = Role.User, Content = Transcription(bilan) },
                },
                OutputConfig = new OutputConfig { Effort = Effort.Medium },
            };

            var texte = new StringBuilder();

            // EN DIFFUSION, L'USAGE ARRIVE EN DEUX FOIS : l'entrée et le cache
            // au début du message, la sortie à la fin une fois écrite.
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

            // Ce que ce bilan a coûté. Sans cette ligne, le poste dépense en
            // silence : seul le dialogue portait ses jetons.
            await _journal.EnregistrerAsync(
                "bilan", modele, entree, sortie, cacheLu, cacheEcrit, ct: ct);

            return texte.ToString();
        }

        private static string Consigne(BilanEleve bilan)
        {
            var accord = bilan.Sexe switch
            {
                Sexe.Fille => "L'élève est une fille : accorde au féminin.",
                Sexe.Garcon => "L'élève est un garçon : accorde au masculin.",
                _ => "Le sexe de l'élève est inconnu : évite les accords.",
            };

            // `$$` et non `$` : le format de réponse contient des accolades JSON,
            // qui seraient prises pour des interpolations. Ici l'interpolation
            // s'écrit {{ }} et une accolade seule reste littérale.
            return $$"""
                Tu es {{bilan.SignatureProf}}, professeur particulier de
                {{bilan.SignatureMatiere}} de {{bilan.Prenom}}, {{bilan.Age}} ans, en
                {{bilan.NiveauLibelle}}. Tu écris le bilan hebdomadaire destiné à
                ses parents.

                {{accord}}

                # Ce qu'on attend de toi

                Tu t'adresses au parent, pas à l'enfant. Ton chaleureux et
                direct, sans jargon pédagogique et sans flatterie creuse. Un
                parent repère immédiatement un compliment automatique.

                Dis ce qui a réellement progressé et ce qui bloque encore. Si la
                semaine a été faible, dis-le franchement mais sans dramatiser :
                un enfant de cet âge a le droit d'avoir une semaine molle.

                Ne cite JAMAIS les échanges mot à mot. Le parent n'a pas à lire
                ce que son enfant a écrit : il a droit à ton analyse, pas à une
                transcription.

                N'invente rien. Si les échanges ne montrent pas de difficulté
                précise, laisse le champ vide plutôt que de meubler.

                # Format de ta réponse

                Réponds UNIQUEMENT avec un objet JSON valide, sans texte autour
                et sans balises de code. Structure exacte :

                {
                  "remarque": "3 à 5 phrases adressées au parent",
                  "matieres": [
                    {
                      "libelle": "nom exact de la matière",
                      "travaille": "une ou deux phrases sur ce qui a été vu",
                      "difficultes": "une phrase, ou une chaîne vide"
                    }
                  ]
                }
                """;
        }

        private static string Transcription(BilanEleve bilan)
        {
            var texte = new StringBuilder();
            texte.AppendLine($"Semaine du {bilan.Debut:dd/MM} au {bilan.Fin.AddDays(-1):dd/MM}.");
            texte.AppendLine($"{bilan.NombreSeances} séance(s), {bilan.NombreEchanges} échanges.");
            texte.AppendLine();

            foreach (var matiere in bilan.Matieres)
            {
                texte.AppendLine($"## {matiere.Libelle} ({matiere.NombreSeances} séance(s))");
                texte.AppendLine();
                foreach (var extrait in matiere.Extraits) texte.AppendLine(extrait);
                texte.AppendLine();
            }

            return texte.ToString();
        }

        private void Appliquer(BilanEleve bilan, string reponse)
        {
            // Le modèle encadre parfois le JSON de ```json malgré la consigne :
            // on récupère l'objet plutôt que d'échouer sur trois caractères.
            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return;

            using var document = JsonDocument.Parse(reponse[debut..(fin + 1)]);
            var racine = document.RootElement;

            if (racine.TryGetProperty("remarque", out var remarque))
            {
                bilan.Remarque = remarque.GetString();
            }

            if (!racine.TryGetProperty("matieres", out var matieres)
                || matieres.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var element in matieres.EnumerateArray())
            {
                var libelle = element.TryGetProperty("libelle", out var l) ? l.GetString() : null;

                var cible = bilan.Matieres.FirstOrDefault(m =>
                    string.Equals(m.Libelle, libelle, StringComparison.OrdinalIgnoreCase));

                // Une seule matière : on rattache même si le libellé a été
                // reformulé, plutôt que de perdre le contenu rédigé.
                cible ??= bilan.Matieres.Count == 1 ? bilan.Matieres[0] : null;
                if (cible is null) continue;

                if (element.TryGetProperty("travaille", out var t)) cible.Travaille = t.GetString();
                if (element.TryGetProperty("difficultes", out var d)) cible.Difficultes = d.GetString();
            }
        }
    }
}
