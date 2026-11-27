using System.Globalization;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Ce qu'une lecture du calendrier officiel a donné.</summary>
    /// <param name="Lu">Faux si le modèle n'a rien rendu d'exploitable.</param>
    /// <param name="Proposees">Les épreuves qui ont reçu une NOUVELLE proposition (différente de la date validée).</param>
    /// <param name="Session">La session dont parle la page (2027, 2028…), si le modèle l'a vue.</param>
    public record LectureCalendrier(bool Lu, IReadOnlyList<string> Proposees, int? Session);

    /// <summary>
    /// LIRE LE CALENDRIER OFFICIEL ET PROPOSER DES DATES — Camara, le 25/11/2026, formule B : « quand tu proposes les
    /// dates, il faut les liens cliquables des sources officielles pour que je puisse valider ».
    ///
    /// LE MODÈLE PROPOSE, IL NE DÉCIDE PAS. Ce qu'il lit devient une ligne PROPOSEE, jamais une date vue des familles :
    /// Camara la compare, avec le lien de la page lue et la phrase d'où elle vient, puis valide ou rejette (onglet
    /// « Sessions d'examens »). C'est l'écart assumé avec la veille des programmes, qui ne fait que constater : une date
    /// se lit sans jugement, une compétence non.
    ///
    /// GARDE-FOUS : seules les épreuves connues (liste fermée) ; une date doit être écrite dans le texte, avec sa
    /// phrase ; un horaire absent du texte reprend celui déjà validé ; une proposition identique à la date validée
    /// n'est pas posée (le dépôt le vérifie).
    /// </summary>
    public class CalendrierExamensService
    {
        /// <summary>Au-delà, le texte est coupé : une page de calendrier tient très en deçà.</summary>
        private const int TexteMax = 60_000;

        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;
        private readonly ICalendrierExamensRepository _calendrier;
        private readonly ILogger<CalendrierExamensService> _logger;

        public CalendrierExamensService(
            AnthropicClient client, IOptions<OptionsClaude> options, IJournalClaudeRepository journal,
            ICalendrierExamensRepository calendrier, ILogger<CalendrierExamensService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _calendrier = calendrier ?? throw new ArgumentNullException(nameof(calendrier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LectureCalendrier> ProposerAsync(string texte, string url, DateTime? copieDu, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(texte)) return new LectureCalendrier(false, [], null);

            var epreuves = await _calendrier.GetEpreuvesAsync(ct);
            var validees = (await _calendrier.GetToutesAsync(ct))
                .Where(d => d.Etat == CalendrierExamens.Validee)
                .ToDictionary(d => d.EpreuveCode);

            var reponse = await AppelerAsync(Corpus(texte, epreuves, validees), ct);
            var lues = Analyser(reponse, out var session);
            if (lues is null)
            {
                _logger.LogWarning("Calendrier des examens : reponse du modele illisible pour {Url}.", url);
                return new LectureCalendrier(false, [], null);
            }

            var connues = epreuves.ToDictionary(e => e.Code);
            var proposees = new List<string>();
            var maintenant = DateTime.UtcNow;
            var source = copieDu is DateTime copie
                ? $"Page lue par la veille (copie de l'Internet Archive du {copie:dd/MM/yyyy})"
                : "Page lue par la veille";

            foreach (var lue in lues)
            {
                if (!connues.ContainsKey(lue.Code)) continue;
                validees.TryGetValue(lue.Code, out var validee);

                var proposition = new DateEpreuveOfficielle
                {
                    EpreuveCode = lue.Code,
                    Nature = lue.Nature ?? validee?.Nature ?? CalendrierExamens.Jour,
                    DateEpreuve = lue.Date,
                    DateFin = lue.DateFin ?? (lue.Nature is null ? validee?.DateFin : null),
                    // UN HORAIRE ABSENT DU TEXTE n'est pas un horaire changé : la page d'Éduscol donne les jours,
                    // l'annexe les heures. On garde celui qui est validé.
                    HeureDebut = lue.HeureDebut ?? validee?.HeureDebut,
                    HeureFin = lue.HeureFin ?? validee?.HeureFin,
                    RemplacementLe = lue.Remplacement ?? validee?.RemplacementLe,
                    SourceLibelle = source,
                    SourceUrl = url,
                    AnnexeUrl = validee?.AnnexeUrl,
                    Extrait = lue.Extrait,
                };

                if (await _calendrier.ProposerAsync(proposition, maintenant, ct)) proposees.Add(lue.Code);
            }

            _logger.LogInformation(
                "Calendrier des examens : {Url} lue, {Lues} date(s) relevee(s), {Proposees} nouvelle(s) proposition(s).",
                url, lues.Count, proposees.Count);

            return new LectureCalendrier(true, proposees, session);
        }

        private static string Corpus(
            string texte, IReadOnlyList<EpreuveCalendrier> epreuves, IReadOnlyDictionary<string, DateEpreuveOfficielle> validees)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ÉPREUVES CONNUES (code | examen | épreuve | session | date actuellement validée) :");
            foreach (var e in epreuves)
            {
                var date = validees.TryGetValue(e.Code, out var v)
                    ? $"{v.DateEpreuve:yyyy-MM-dd}{(v.DateFin is DateTime f ? $" / {f:yyyy-MM-dd}" : "")} {v.HeureDebut:hh\\:mm}-{v.HeureFin:hh\\:mm}"
                    : "aucune";
                sb.AppendLine($"{e.Code} | {e.ExamenLibelle} | {e.Libelle} | {e.Session} | {date}");
            }

            sb.AppendLine();
            sb.AppendLine("TEXTE DE LA PAGE OFFICIELLE :");
            sb.AppendLine(texte.Length > TexteMax ? texte[..TexteMax] : texte);
            return sb.ToString();
        }

        private async Task<string> AppelerAsync(string corpus, CancellationToken ct)
        {
            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 6000,
                System = new List<TextBlockParam> { new() { Text = Consigne } },
                Messages = new List<MessageParam> { new() { Role = Role.User, Content = corpus } },
                OutputConfig = new OutputConfig { Effort = Effort.Low },
            };

            var reponse = await _client.Messages.Create(parametres, cancellationToken: ct);

            await _journal.EnregistrerAsync(
                "calendrier-examens", reponse.Model,
                reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                reponse.Usage.CacheReadInputTokens ?? 0,
                reponse.Usage.CacheCreationInputTokens ?? 0,
                ct: ct);

            return string.Join("\n", reponse.Content
                .Where(b => b.TryPickText(out _))
                .Select(b => { b.TryPickText(out var t); return t!.Text; }));
        }

        /// <summary>Une date lue par le modèle, vérifiée champ par champ. Public pour être testé sans réseau.</summary>
        public sealed record DateLue(
            string Code, string? Nature, DateTime Date, DateTime? DateFin, TimeSpan? HeureDebut, TimeSpan? HeureFin,
            DateTime? Remplacement, string Extrait);

        /// <summary>
        /// Le JSON du modèle, champ par champ. Une ligne sans date valide ou sans extrait est ignorée : une date qu'on
        /// ne peut pas rattacher à une phrase du texte ne se propose pas. Null si le JSON est illisible.
        /// </summary>
        public static IReadOnlyList<DateLue>? Analyser(string reponse, out int? session)
        {
            session = null;
            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return null;

            try
            {
                using var doc = JsonDocument.Parse(reponse[debut..(fin + 1)]);
                var racine = doc.RootElement;
                if (racine.TryGetProperty("session", out var s) && s.ValueKind == JsonValueKind.Number) session = s.GetInt32();
                if (!racine.TryGetProperty("dates", out var dates) || dates.ValueKind != JsonValueKind.Array) return [];

                var sortie = new List<DateLue>();
                foreach (var d in dates.EnumerateArray())
                {
                    var code = Texte(d, "code");
                    var extrait = Texte(d, "extrait");
                    if (code is null || string.IsNullOrWhiteSpace(extrait) || Jour(Texte(d, "date")) is not DateTime jour) continue;

                    var nature = Texte(d, "nature");
                    if (nature is not null && !CalendrierExamens.Natures.Contains(nature)) nature = null;

                    sortie.Add(new DateLue(
                        code.Trim(), nature, jour, Jour(Texte(d, "date_fin")),
                        Heure(Texte(d, "heure_debut")), Heure(Texte(d, "heure_fin")),
                        Jour(Texte(d, "remplacement")), extrait.Trim()));
                }

                return sortie;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? Texte(JsonElement e, string nom) =>
            e.TryGetProperty(nom, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static DateTime? Jour(string? texte) =>
            DateTime.TryParseExact(texte, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        private static TimeSpan? Heure(string? texte) =>
            TimeSpan.TryParseExact(texte, @"hh\:mm", CultureInfo.InvariantCulture, out var h) ? h : null;

        private const string Consigne = """
            Tu lis une page officielle du ministère de l'Éducation nationale (calendrier des examens : brevet,
            baccalauréat…). On te donne la liste FERMÉE des épreuves que l'application connaît, avec leur code et la
            date actuellement retenue. Relève, pour chaque épreuve de la liste dont la page DONNE la date, la date
            écrite dans la page.

            Règles strictes :
            - Uniquement ce qui est ÉCRIT dans la page. Ne déduis rien, ne complète rien de mémoire.
            - Uniquement les codes de la liste ; jamais un code inventé.
            - Horaires de la MÉTROPOLE (heure de Paris). Si la page ne donne pas l'heure, laisse heure_debut et heure_fin
              à null : un horaire absent n'est pas un horaire changé.
            - nature : "JOUR" pour un jour fixe ; "UN_DES_DEUX" quand l'épreuve tombe l'un de deux jours selon la
              convocation (les spécialités du bac général : le premier jour dans "date", le second dans "date_fin") ;
              "PERIODE" pour une période (date = début, date_fin = fin).
            - remplacement : le jour de la session de remplacement de cette épreuve, s'il est écrit ; sinon null.
            - extrait : la phrase de la page d'où vient la date, recopiée telle quelle (300 caractères au plus).
              Sans extrait, ne mets pas la ligne.
            - session : l'année de la session dont parle la page (par exemple 2027).
            - Si la page ne concerne aucune épreuve de la liste, rends "dates": [].

            Réponds UNIQUEMENT par ce JSON, sans rien autour :
            {"session": 2027, "dates": [{"code": "DNB_2027_FRANCAIS", "nature": "JOUR", "date": "2027-06-24",
            "date_fin": null, "heure_debut": "08:00", "heure_fin": "11:15", "remplacement": "2027-09-09",
            "extrait": "Les épreuves écrites du diplôme national du brevet ... auront lieu les jeudi 24 ..."}]}
            """;
    }
}
