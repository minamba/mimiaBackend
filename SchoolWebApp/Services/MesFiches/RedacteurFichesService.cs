using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.MesFiches
{
    /// <summary>Ce que la lecture d'un cours a trouvé.</summary>
    public sealed record LectureDuCours(bool Lisible, string? Titre, IReadOnlyList<NotionDuCours> Notions, string? Remarque);

    /// <summary>
    /// LIRE LE COURS DE L'ÉLÈVE, PUIS EN ÉCRIRE LES FICHES — 14/10/2026.
    ///
    /// LES PAGES D'ABORD, LA CONSIGNE ENSUITE, ET C'EST TOUT L'ÉCONOMIE DU
    /// MODULE. Chaque appel envoie le même début — la même consigne système,
    /// puis les pages du cours, marquées pour le cache — et seulement après,
    /// ce qu'on lui demande cette fois (lire, ou rédiger telle notion). La
    /// lecture écrit le cache ; chaque fiche qui suit relit les pages à un
    /// dixième du prix. Un cours de dix pages lu une fois, puis quatre fiches,
    /// ne paie pas cinq fois ses dix pages.
    ///
    /// Le modèle et l'effort sont ceux du dialogue (`TypeTache.Dialogue`) : pas
    /// de décision de produit nouvelle. LE MÊME EFFORT POUR LIRE ET RÉDIGER, et
    /// ce n'est pas un détail : mesuré le 14/10/2026, une lecture en effort bas
    /// n'ouvrait pas le cache de la rédaction en effort moyen — chaque cours
    /// payait ses pages deux fois au prix fort.
    /// </summary>
    public class RedacteurFichesService
    {
        private const int MaxTokensLecture = 1500;
        private const int MaxTokensRedaction = 6000;

        /// <summary>Identique pour tous les appels d'un même cours : c'est le début mis en cache.</summary>
        private const string Systeme = """
            Tu aides un élève à réviser SON cours, à partir des pages qu'il a
            importées (photos ou document). Tu réponds uniquement par le JSON
            demandé, sans texte autour.
            """;

        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;

        public RedacteurFichesService(AnthropicClient client, IOptions<OptionsClaude> options, IJournalClaudeRepository journal)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        }

        /// <param name="notionsMaxImpose">Le plafond des notions d'un autre usage du cours (podcasts, 15/11/2026).</param>
        public async Task<LectureDuCours> LireAsync(
            CoursATraiter cours, CancellationToken ct, int? notionsMaxImpose = null, string origine = "fiches-lecture")
        {
            // Une famille administratrice n'a pas le plafond de huit notions
            // (Camara, le 16/10/2026) : la réponse grandit avec, à ~120 jetons
            // la notion (titre, résumé, rang).
            var notionsMax = notionsMaxImpose ?? FichesRevisionEleve.Limites(cours.SansLimite).NotionsMax;
            var maxTokens = MaxTokensLecture + Math.Max(0, notionsMax - FichesRevisionEleve.NotionsMax) * 120;

            var consigne = ConsignesFiches.Lecture(
                cours.ClasseLibelle, cours.MatiereLibelle, notionsMax, cours.NotionsExistantes);
            var effort = AgentPedagogiqueService.ConvertirEffort(_options.Effort(TypeTache.Dialogue));
            var reponse = await AppelerAsync(cours, consigne, maxTokens, effort, origine, ct);

            var racine = ContenuFiche.ObjetJson(reponse);
            if (racine is null)
            {
                return new LectureDuCours(false, null, [], "Je n’ai pas réussi à lire ton cours. Réessaie dans un moment.");
            }

            var lisible = racine["lisible"] is System.Text.Json.Nodes.JsonValue v && v.TryGetValue<bool>(out var oui) && oui;
            var remarque = ContenuFiche.Texte(racine["remarque"], 300);

            var notions = new List<NotionDuCours>();
            var reconnues = new HashSet<int>();
            if (racine["notions"] is System.Text.Json.Nodes.JsonArray brutes)
            {
                foreach (var brute in brutes.Take(notionsMax))
                {
                    if (brute is System.Text.Json.Nodes.JsonObject n && ContenuFiche.Texte(n["titre"], 160) is { } titre)
                    {
                        // Un complément : la notion déjà dans le cours, si la lecture
                        // la reconnaît — un numéro de la liste, et une seule fois.
                        int? deja = n["deja"] is System.Text.Json.Nodes.JsonValue d && d.TryGetValue<int>(out var k)
                            && k >= 1 && k <= cours.NotionsExistantes.Count && reconnues.Add(k)
                            ? k
                            : null;

                        notions.Add(new NotionDuCours(titre, ContenuFiche.Texte(n["resume"], 300), deja));
                    }
                }
            }

            // Lisible mais sans notion : on ne fabrique pas de fiche sur du vide.
            if (!lisible || notions.Count == 0)
            {
                return new LectureDuCours(false, null, [],
                    remarque ?? "Je n’arrive pas à lire ce cours. Vérifie que la photo est nette et que la page entière est visible.");
            }

            return new LectureDuCours(true, ContenuFiche.Texte(racine["titreCours"], 160) ?? notions[0].Titre, notions, null);
        }

        /// <summary>
        /// Une fiche. `rang` vaut 0 pour une fiche du cours entier (express,
        /// spéciale contrôle), sinon le rang de la notion à partir de 1.
        /// </summary>
        public async Task<ContenuFiche.Fiche?> RedigerAsync(CoursATraiter cours, int rang, CancellationToken ct)
        {
            var notion = rang > 0 && rang <= cours.Notions.Count ? cours.Notions[rang - 1].Titre : null;

            var consigne = ConsignesFiches.Redaction(
                cours.Type, cours.ClasseLibelle, cours.Rang, cours.MatiereLibelle, cours.Titre ?? "son cours",
                notion, rang, cours.Notions.Count, cours.Notions.Select(n => n.Titre));

            var effort = AgentPedagogiqueService.ConvertirEffort(_options.Effort(TypeTache.Dialogue));
            var reponse = await AppelerAsync(cours, consigne, MaxTokensRedaction, effort, "fiches-redaction", ct);

            return ContenuFiche.Lire(reponse, cours.Type);
        }

        /// <summary>
        /// L'appel commun : les pages du cours d'abord (mises en cache), la consigne ensuite. Interne pour que les
        /// podcasts (15/11/2026) écrivent leurs épisodes sur le MÊME cache que la lecture du cours.
        /// </summary>
        internal async Task<string> AppelerAsync(
            CoursATraiter cours, string consigne, int maxTokens, Effort effort, string origine, CancellationToken ct)
        {
            var blocs = new List<ContentBlockParam>();

            for (var i = 0; i < cours.Pages.Count; i += 1)
            {
                var (typeMime, nomFichier, donnees) = cours.Pages[i];
                var base64 = Convert.ToBase64String(donnees);

                // LE POINT DE CACHE SUR LA DERNIÈRE PAGE : tout ce qui précède
                // — système et pages — est relu à un dixième du prix aux appels
                // suivants du même cours.
                var cache = i == cours.Pages.Count - 1 ? new CacheControlEphemeral() : null;

                // UNE FICHE LIBRE (06/11/2026) : le texte écrit ou dicté par l'élève,
                // tel quel, avec le même point de cache qu'une page.
                if (FichesRevisionEleve.EstUnTexte(typeMime))
                {
                    blocs.Add(new TextBlockParam
                    {
                        Text = "Ce que l’élève a écrit ou dicté :\n\n" + System.Text.Encoding.UTF8.GetString(donnees),
                        CacheControl = cache,
                    });
                }
                else if (typeMime == ValidationPieceJointe.TypePdf)
                {
                    blocs.Add(new DocumentBlockParam
                    {
                        Source = new Base64PdfSource { Data = base64 },
                        Title = nomFichier,
                        CacheControl = cache,
                    });
                }
                else
                {
                    blocs.Add(new ImageBlockParam
                    {
                        Source = new Base64ImageSource
                        {
                            Data = base64,
                            MediaType = typeMime switch
                            {
                                "image/png" => MediaType.ImagePng,
                                "image/gif" => MediaType.ImageGif,
                                "image/webp" => MediaType.ImageWebP,
                                _ => MediaType.ImageJpeg,
                            },
                        },
                        CacheControl = cache,
                    });
                }
            }

            // Après les pages : la précision d'une fiche libre ne touche pas au cache.
            var libre = cours.Pages.Count > 0 && cours.Pages.All(p => FichesRevisionEleve.EstUnTexte(p.TypeMime));
            // Les podcasts (15/11/2026) ajoutent leur propre règle de rédaction libre : seule la lecture est commune.
            // LE RÉSUMÉ DE LIVRE (15/11/2026) : ses notions sont les parties du livre, et rien n'est inventé.
            var livre = libre && cours.Pages.All(p => PodcastsEleve.EstUnLivre(p.TypeMime));
            if (livre && origine.EndsWith("-lecture", StringComparison.Ordinal)) consigne += ConsignesFiches.LectureLivre;
            else if (libre && origine.EndsWith("-lecture", StringComparison.Ordinal)) consigne += ConsignesFiches.LectureLibre;
            else if (libre && origine.StartsWith("fiches", StringComparison.Ordinal)) consigne += ConsignesFiches.RedactionLibre;

            blocs.Add(new TextBlockParam { Text = consigne });

            var reponse = await _client.Messages.Create(
                new MessageCreateParams
                {
                    Model = _options.Modele(TypeTache.Dialogue),
                    MaxTokens = maxTokens,
                    System = new List<TextBlockParam> { new() { Text = Systeme } },
                    Messages = new List<MessageParam> { new() { Role = Role.User, Content = blocs } },
                    OutputConfig = new OutputConfig { Effort = effort },
                },
                cancellationToken: ct);

            await _journal.EnregistrerAsync(
                origine, reponse.Model,
                reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                reponse.Usage.CacheReadInputTokens ?? 0,
                reponse.Usage.CacheCreationInputTokens ?? 0,
                reference: $"cours:{cours.Id}",
                ct: ct);

            return string.Join("\n", reponse.Content
                .Select(b => b.TryPickText(out var t) ? t.Text : null)
                .OfType<string>());
        }
    }
}
