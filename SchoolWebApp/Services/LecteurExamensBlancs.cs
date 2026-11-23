using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// EXAMENS BLANCS, ÉTAPE 2 — Camara, le 23/11/2026 : « l'élève envoie simplement tous ses documents ; le
    /// professeur extrait les notions, les organise en dossiers ». Deux gestes, en tâche de fond
    /// (<c>ExamensBlancsWorker</c>) :
    ///
    /// 1. LIRE chaque photo et chaque PDF. Le texte d'un PDF numérique (exporté de Word, téléchargé sur l'ENT) est
    ///    extrait GRATUITEMENT, page par page, par PdfPig ; seules les pages sans texte (scans, photos enregistrées
    ///    en PDF) passent par le modèle de lecture, par paquets de <see cref="PagesParPaquet"/> : un PDF de 80 pages
    ///    ne tient pas dans une seule réponse (4 000 jetons), et serait coupé. Les photos passent par le modèle.
    ///    Word, Excel et PowerPoint sont lus à l'envoi (ExamensBlancsService).
    ///
    /// 2. RANGER les documents d'une matière en dossiers, une fois tous lus et l'envoi calmé : un appel au modèle
    ///    reçoit un extrait de chaque document et la liste FERMÉE des notions du programme de la classe, et rend les
    ///    dossiers. Un code absent de la liste est ignoré (la notion reste en texte libre) ; un document peut être
    ///    dans plusieurs dossiers.
    ///
    /// AUCUN PLAFOND DE PAGES (Camara : « je pourrais perdre des clients ») : le coût se mesure dans le journal des
    /// appels, origines « transcription-examen-blanc » et « examen-blanc-dossiers ».
    /// </summary>
    public class LecteurExamensBlancs
    {
        /// <summary>Pages scannées lues ensemble : ~600 à 900 jetons rendus par page, sous les 4 000 de la réponse.</summary>
        public const int PagesParPaquet = 4;

        /// <summary>Une page « numérique » porte au moins ce texte ; en dessous, c'est un scan à lire au modèle.</summary>
        private const int TexteMinimalParPage = 40;

        /// <summary>Le texte gardé d'un document (une base de données n'est pas un manuel de 300 pages en entier).</summary>
        private const int CaracteresMax = 200_000;

        /// <summary>Ce que le rangement voit de chaque document : son début, assez pour en dire la notion.</summary>
        private const int ExtraitParDocument = 3_500;

        /// <summary>Douze dossiers au plus : un examen blanc porte sur des chapitres, pas sur l'année entière.</summary>
        private const int DossiersMax = 12;

        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;
        private readonly TranscripteurDocuments _transcripteur;
        private readonly IStockageExamensBlancs _stockage;
        private readonly IExamenBlancRepository _examens;
        private readonly IMaitriseRepository _maitrises;
        private readonly ILogger<LecteurExamensBlancs> _logger;

        public LecteurExamensBlancs(
            AnthropicClient client,
            IOptions<OptionsClaude> options,
            IJournalClaudeRepository journal,
            TranscripteurDocuments transcripteur,
            IStockageExamensBlancs stockage,
            IExamenBlancRepository examens,
            IMaitriseRepository maitrises,
            ILogger<LecteurExamensBlancs> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _transcripteur = transcripteur ?? throw new ArgumentNullException(nameof(transcripteur));
            _stockage = stockage ?? throw new ArgumentNullException(nameof(stockage));
            _examens = examens ?? throw new ArgumentNullException(nameof(examens));
            _maitrises = maitrises ?? throw new ArgumentNullException(nameof(maitrises));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------------------------------------------------------------------------------ 1. lire

        /// <summary>Lit un lot de documents. Rend le nombre de documents MARQUÉS (lus ou abandonnés), pas tentés.</summary>
        public async Task<int> LireAsync(int lot, CancellationToken ct)
        {
            var aLire = await _examens.GetDocumentsALireAsync(lot, ct);
            var marques = 0;

            foreach (var document in aLire)
            {
                ct.ThrowIfCancellationRequested();

                var octets = await _stockage.LireAsync(document.Chemin, ct);
                if (octets is not { Length: > 0 })
                {
                    marques += await EchecAsync(document, null, ct) ? 1 : 0;
                    continue;
                }

                try
                {
                    var texte = document.TypeMime == ValidationPieceJointe.TypePdf
                        ? await LirePdfAsync(octets, document.NomFichier, ct)
                        : await _transcripteur.TranscrireAsync(octets, document.TypeMime, document.NomFichier, "transcription-examen-blanc", ct);

                    if (string.IsNullOrWhiteSpace(texte)) texte = DocumentControle.MentionVide;
                    if (texte.Length > CaracteresMax) texte = texte[..CaracteresMax];

                    await _examens.EnregistrerLectureAsync(document.Id, texte.Trim(), ct);
                    marques++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    marques += await EchecAsync(document, ex, ct) ? 1 : 0;
                }
            }

            return marques;
        }

        private async Task<bool> EchecAsync(DocumentALire document, Exception? ex, CancellationToken ct)
        {
            var abandon = await _examens.SignalerEchecLectureAsync(document.Id, DocumentControle.MentionAbandon, ct);

            _logger.LogWarning(ex,
                "Examen blanc {ExamenBlancId}, document {Id} : lecture {Resultat}.",
                document.ExamenBlancId, document.Id, abandon ? "ABANDONNEE apres trois echecs" : "ratee, on reessaiera");

            return abandon;
        }

        /// <summary>
        /// Le texte d'un PDF : page par page, extrait sans modèle quand la page en porte ; les pages scannées sont lues
        /// par le modèle, par paquets de pages consécutives, puis remises à leur place.
        /// </summary>
        private async Task<string> LirePdfAsync(byte[] octets, string nom, CancellationToken ct)
        {
            using var pdf = PdfDocument.Open(octets);
            var pages = new string?[pdf.NumberOfPages + 1];
            var scannees = new List<int>();

            for (var n = 1; n <= pdf.NumberOfPages; n++)
            {
                string texte;
                try
                {
                    texte = pdf.GetPage(n).Text ?? string.Empty;
                }
                catch (Exception)
                {
                    texte = string.Empty;
                }

                if (texte.Trim().Length >= TexteMinimalParPage) pages[n] = texte.Trim();
                else scannees.Add(n);
            }

            foreach (var paquet in Paquets(scannees))
            {
                ct.ThrowIfCancellationRequested();

                var builder = new PdfDocumentBuilder();
                foreach (var n in paquet) builder.AddPage(pdf, n);
                var extrait = builder.Build();

                var lu = await _transcripteur.TranscrireAsync(
                    extrait, ValidationPieceJointe.TypePdf, $"{nom} (pages {paquet[0]}–{paquet[^1]})",
                    "transcription-examen-blanc", ct);

                // Le paquet rendu d'un bloc : placé sur sa première page, les suivantes vides.
                pages[paquet[0]] = lu?.Trim();
                foreach (var n in paquet.Skip(1)) pages[n] ??= string.Empty;
            }

            _logger.LogInformation(
                "PDF d'examen blanc lu : {Pages} page(s), dont {Scannees} scannee(s) lue(s) par le modele.",
                pdf.NumberOfPages, scannees.Count);

            var sortie = new StringBuilder();
            for (var n = 1; n < pages.Length; n++)
            {
                if (string.IsNullOrWhiteSpace(pages[n])) continue;
                sortie.AppendLine($"— Page {n} —").AppendLine(pages[n]).AppendLine();
            }

            return sortie.ToString();
        }

        /// <summary>Les pages scannées, en paquets de pages CONSÉCUTIVES (au plus <see cref="PagesParPaquet"/>).</summary>
        private static IEnumerable<List<int>> Paquets(List<int> pages)
        {
            var courant = new List<int>();
            foreach (var n in pages)
            {
                if (courant.Count > 0 && (n != courant[^1] + 1 || courant.Count >= PagesParPaquet))
                {
                    yield return courant;
                    courant = [];
                }

                courant.Add(n);
            }

            if (courant.Count > 0) yield return courant;
        }

        // ---------------------------------------------------------------------------------------- 2. ranger

        /// <summary>
        /// Range les documents d'une matière en dossiers. Rend faux si l'appel n'a rien donné d'exploitable (le
        /// travailleur réessaiera, puis abandonnera : voir ExamensBlancsWorker).
        /// </summary>
        public async Task<bool> RangerAsync(MatiereANotionner matiere, CancellationToken ct)
        {
            var referentiel = matiere.NiveauScolaireId is int niveau
                ? (await _maitrises.GetNotionsDuNiveauAsync(matiere.MatiereId, niveau, ct)).ToList()
                : [];

            var lisibles = matiere.Documents
                .Where(d => !d.Texte.StartsWith(DocumentControle.PrefixeIllisible, StringComparison.Ordinal))
                .ToList();

            if (lisibles.Count == 0)
            {
                await _examens.PoserNotionsAsync(matiere.LigneId, [], DateTime.UtcNow, ct);
                return true;
            }

            var corpus = new StringBuilder();
            corpus.AppendLine($"MATIÈRE : {matiere.MatiereLibelle}");
            corpus.AppendLine();
            corpus.AppendLine("NOTIONS DU PROGRAMME DE LA CLASSE (choisis les codes uniquement là-dedans) :");
            if (referentiel.Count == 0) corpus.AppendLine("(aucune : donne des libellés libres, code null)");
            foreach (var c in referentiel) corpus.AppendLine($"- {c.Code} | {c.Domaine} | {c.Libelle}");
            corpus.AppendLine();
            corpus.AppendLine("DOCUMENTS DE L'ÉLÈVE (début de chacun) :");

            foreach (var (id, nom, texte) in lisibles)
            {
                var extrait = texte.Length > ExtraitParDocument ? texte[..ExtraitParDocument] + " […]" : texte;
                corpus.AppendLine($"## Document {id} — {nom}");
                corpus.AppendLine(extrait);
                corpus.AppendLine();
            }

            var reponse = await AppelerAsync(corpus.ToString(), ct);
            var dossiers = Analyser(reponse, referentiel, lisibles.Select(d => d.Id).ToHashSet());
            if (dossiers is null) return false;

            await _examens.PoserNotionsAsync(matiere.LigneId, dossiers, DateTime.UtcNow, ct);

            _logger.LogInformation(
                "Examen blanc : matiere {LigneId} rangee en {Dossiers} dossier(s) pour {Documents} document(s).",
                matiere.LigneId, dossiers.Count, lisibles.Count);

            return true;
        }

        private static List<NotionProposee>? Analyser(string reponse, List<CompetenceCandidate> referentiel, HashSet<int> documents)
        {
            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return null;

            var connus = referentiel
                .Where(c => c.Code is not null)
                .GroupBy(c => c.Code!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            try
            {
                using var json = JsonDocument.Parse(reponse[debut..(fin + 1)]);
                if (!json.RootElement.TryGetProperty("dossiers", out var liste) || liste.ValueKind != JsonValueKind.Array) return null;

                var sortie = new List<NotionProposee>();
                var dejaVus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var element in liste.EnumerateArray())
                {
                    if (sortie.Count >= DossiersMax) break;

                    var code = element.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                    var libelle = element.TryGetProperty("libelle", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString()?.Trim() : null;

                    var ids = element.TryGetProperty("documents", out var d) && d.ValueKind == JsonValueKind.Array
                        ? d.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
                            .Select(x => x.GetInt32())
                            .Where(documents.Contains)
                            .Distinct()
                            .ToList()
                        : [];

                    // UN CODE INVENTÉ EST IGNORÉ : la notion reste, en texte libre.
                    var competence = code is not null && connus.TryGetValue(code, out var cand) ? cand : null;
                    libelle = string.IsNullOrWhiteSpace(libelle) ? competence?.Libelle : libelle;

                    if (string.IsNullOrWhiteSpace(libelle) || ids.Count == 0) continue;
                    if (!dejaVus.Add(competence?.Code ?? libelle)) continue;

                    sortie.Add(new NotionProposee(competence?.Id, libelle, ids));
                }

                return sortie;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<string> AppelerAsync(string corpus, CancellationToken ct)
        {
            // LE MÊME MODÈLE QUE LE RATTACHEMENT DES NOTIONS D'UN CONTRÔLE (PlanificateurControleService), pour le
            // même travail : rapprocher des textes d'une liste fermée de notions.
            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 2000,
                System = new List<TextBlockParam> { new() { Text = Consigne } },
                Messages = new List<MessageParam> { new() { Role = Role.User, Content = corpus } },
                OutputConfig = new OutputConfig { Effort = Effort.Low },
            };

            var reponse = await _client.Messages.Create(parametres, cancellationToken: ct);

            await _journal.EnregistrerAsync(
                "examen-blanc-dossiers", reponse.Model,
                reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                reponse.Usage.CacheReadInputTokens ?? 0,
                reponse.Usage.CacheCreationInputTokens ?? 0,
                ct: ct);

            return string.Join("\n", reponse.Content
                .Where(b => b.TryPickText(out _))
                .Select(b => { b.TryPickText(out var t); return t!.Text; }));
        }

        private const string Consigne = """
            Tu es le professeur de cette matière. Un élève prépare un examen blanc et t'a envoyé TOUS ses cours.
            Range ses documents en DOSSIERS : un dossier par notion qu'ils travaillent.

            Règles :
            - Une notion = une compétence du programme, prise dans la liste fournie quand elle y est (son code).
              Si un document travaille une notion absente de la liste, mets "code": null et un libellé court.
            - N'invente AUCUN code : un code absent de la liste sera ignoré.
            - Le libellé est pour l'élève : court et clair (« Le théorème de Thalès », « Les fractions »), 60
              caractères au plus.
            - Un document peut aller dans plusieurs dossiers s'il travaille plusieurs notions.
            - Un document qui ne travaille aucune notion de la matière (brouillon, page blanche) ne va nulle part.
            - 12 dossiers au plus. Regroupe plutôt que d'émietter : un examen blanc porte sur quelques chapitres.
            - Les dossiers dans l'ordre du cours, du plus simple au plus avancé.

            Réponds UNIQUEMENT par ce JSON, sans rien autour :
            {"dossiers": [{"code": "MATH_3E_THALES", "libelle": "Le théorème de Thalès", "documents": [12, 15]}]}
            """;
    }
}
