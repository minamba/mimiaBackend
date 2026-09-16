using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Api.Services.Notifications
{
    /// <summary>Un courriel prêt à partir : objet, valeurs du gabarit et pièces.</summary>
    public record CourrielCompose(
        string Sujet,
        Dictionary<string, string> Valeurs,
        IReadOnlyList<PieceMail> Images,
        IReadOnlyList<PieceMail> Documents);

    public interface IComposeurModeleMail
    {
        /// <summary>
        /// Compose un modèle enregistré dans le gabarit « diffusion ».
        /// </summary>
        /// <param name="variables">Les valeurs de `{{prenom}}`, `{{lienAvis}}`… pour ce destinataire.</param>
        /// <param name="mentionPied">Nulle : la mention habituelle, avec le support.</param>
        CourrielCompose Composer(
            ModeleMailDetail modele,
            IReadOnlyList<PieceModeleMailContenu> pieces,
            IReadOnlyDictionary<string, ValeurVariable>? variables = null,
            string? mentionPied = null);
    }

    /// <summary>
    /// Un template enregistré, mis en forme exactement comme un message tapé
    /// à la main.
    ///
    /// LA MÊME COMPOSITION QUE LA DIFFUSION, ET PAS UNE COPIE : les paragraphes,
    /// le gras, les images et les variables passent par `IDiffusionService`. Un
    /// template envoyé ne doit pas ressembler à autre chose que le même message
    /// écrit sans template — ni l'aperçu à autre chose que l'envoi.
    ///
    /// LE RANG DEVIENT LA RÉFÉRENCE `cid:diffusionN`. Le dépôt tient les rangs
    /// sans trou : `[image:2]` dans le texte trouve toujours l'image de rang 2.
    /// </summary>
    public class ComposeurModeleMail : IComposeurModeleMail
    {
        private readonly IDiffusionService _diffusion;

        public ComposeurModeleMail(IDiffusionService diffusion)
        {
            _diffusion = diffusion ?? throw new ArgumentNullException(nameof(diffusion));
        }

        public CourrielCompose Composer(
            ModeleMailDetail modele,
            IReadOnlyList<PieceModeleMailContenu> pieces,
            IReadOnlyDictionary<string, ValeurVariable>? variables = null,
            string? mentionPied = null)
        {
            var images = pieces
                .Where(p => p.Genre == GenrePieceMail.Image)
                .OrderBy(p => p.Rang)
                .Select(p => new PieceMail($"diffusion{p.Rang}", p.NomFichier, p.TypeMime, p.Donnees))
                .ToList();

            var documents = pieces
                .Where(p => p.Genre == GenrePieceMail.Document)
                .OrderBy(p => p.Rang)
                .Select(p => new PieceMail(string.Empty, p.NomFichier, p.TypeMime, p.Donnees))
                .ToList();

            var sujet = _diffusion.RemplacerVariables(modele.Sujet, variables);

            // Même règle que l'écran : un titre laissé vide reprend l'objet.
            var titre = string.IsNullOrWhiteSpace(modele.Titre)
                ? sujet
                : _diffusion.RemplacerVariables(modele.Titre, variables);

            var valeurs = new Dictionary<string, string>
            {
                ["titre"] = sujet,
                ["titreMessage"] = titre,
                ["corpsMessage"] = _diffusion.ComposerCorps(modele.Texte ?? string.Empty, images.Count, variables),
                ["blocPiecesJointes"] =
                    _diffusion.ComposerPiecesJointes(documents.Select(d => d.NomFichier).ToList()),
                ["mentionPied"] = mentionPied ?? _diffusion.ComposerMentionPied(),
            };

            return new CourrielCompose(sujet, valeurs, images, documents);
        }
    }
}
