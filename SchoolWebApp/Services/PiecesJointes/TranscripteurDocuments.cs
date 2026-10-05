using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.PiecesJointes
{
    /// <summary>
    /// LIRE UN DOCUMENT D'ÉLÈVE, EN TEXTE — une seule consigne pour tous.
    ///
    /// Sorti de <c>TranscriptionDocumentsWorker</c> le 26/09/2026, quand le
    /// cours importé avec un contrôle a eu besoin d'être lu lui aussi : deux
    /// copies de la consigne finiraient par dire deux choses différentes, et
    /// une pièce jointe et une page de cours sont le même objet — une feuille
    /// d'élève, photographiée ou scannée.
    ///
    /// SINGLETON : il ne tient aucun état, et le worker, qui en est un, s'en
    /// sert directement.
    /// </summary>
    public class TranscripteurDocuments
    {
        private readonly AnthropicClient _client;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;

        /// <summary>
        /// Plafond de sortie. Une page d'énoncé transcrite tient largement
        /// dedans ; au-delà, c'est que le modèle s'est mis à commenter.
        ///
        /// RELEVÉ DE 2 000 À 4 000. Le plafond ne borne pas que le texte rendu :
        /// la réflexion du modèle le consomme d'abord. Sur un document dense —
        /// un énoncé de trois pages photographié — elle peut l'épuiser AVANT
        /// d'écrire quoi que ce soit, et la réponse revient vide sans la moindre
        /// erreur. C'est le piège qui a coûté cher sur les planches, deux fois.
        ///
        /// On ne paie que ce qui est réellement produit : relever le plafond ne
        /// coûte rien sur les documents qui tenaient déjà dessous.
        /// </summary>
        private const int MaxTokens = 4000;

        public TranscripteurDocuments(
            AnthropicClient client,
            IOptions<OptionsClaude> options,
            IJournalClaudeRepository journal)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        }

        /// <summary>
        /// Le texte du document, ou une chaîne vide si le modèle n'a rien
        /// rendu. <paramref name="origine"/> nomme la dépense dans le journal
        /// des appels.
        /// </summary>
        public async Task<string?> TranscrireAsync(
            byte[] donnees, string typeMime, string nomFichier, string origine, CancellationToken ct)
        {
            var base64 = Convert.ToBase64String(donnees);

            var blocs = new List<ContentBlockParam>();

            if (typeMime == ValidationPieceJointe.TypePdf)
            {
                blocs.Add(new DocumentBlockParam
                {
                    Source = new Base64PdfSource { Data = base64 },
                    Title = nomFichier,
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
                });
            }

            blocs.Add(new TextBlockParam { Text = Consigne });

            var reponse = await _client.Messages.Create(
                new MessageCreateParams
                {
                    Model = _options.ModeleTranscription,
                    MaxTokens = MaxTokens,
                    Messages = new List<MessageParam>
                    {
                        new() { Role = Role.User, Content = blocs },
                    },
                },
                cancellationToken: ct);

            // Ce que cette transcription a coûté. Sans cette ligne, le poste
            // dépense en silence : seul le dialogue portait ses jetons.
            await _journal.EnregistrerAsync(
                origine, reponse.Model,
                reponse.Usage.InputTokens, reponse.Usage.OutputTokens,
                reponse.Usage.CacheReadInputTokens ?? 0,
                reponse.Usage.CacheCreationInputTokens ?? 0,
                ct: ct);

            // On concatène les blocs de texte : le modèle peut en renvoyer
            // plusieurs, et ne lire que le premier tronquerait l'énoncé.
            var morceaux = reponse.Content
                .Where(b => b.TryPickText(out _))
                .Select(b => { b.TryPickText(out var t); return t!.Text; });

            return string.Join("\n", morceaux);
        }

        private const string Consigne = """
            Transcris ce document en texte, fidèlement.

            - Restitue TOUT ce qui est écrit : énoncés, questions, numéros
              d'exercices, consignes, valeurs numériques, unités.
            - Ne résous rien, ne corrige rien, ne commente rien.
            - Ce qui est manuscrit compte autant que ce qui est imprimé. Si
              c'est le travail d'un élève, transcris-le tel quel, erreurs
              comprises.
            - Ce qui ne se transcrit pas — une figure, un graphique, un tableau,
              une photo — décris-le en une phrase entre crochets, par exemple
              [figure : triangle ABC rectangle en A, avec AB = 3 cm].
            - Si un passage est illisible, écris [illisible] plutôt que de
              deviner.
            - Aucune introduction, aucune conclusion : uniquement le contenu.
            """;
    }
}
