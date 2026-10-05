using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Le document enregistré, ou le motif du refus — écrit pour l'élève.</summary>
    public record ResultatDocumentControle(DocumentControle? Document, string? Motif);

    /// <summary>
    /// LE COURS DE L'ÉLÈVE, IMPORTÉ AVEC SON CONTRÔLE — voulu par Camara le
    /// 26/09/2026. Une seule porte pour les deux chemins, l'ordinateur et le
    /// téléphone : un document arrivé par le QR code passe exactement les
    /// mêmes contrôles, comme pour les pièces jointes d'une séance.
    /// </summary>
    public class CoursDuControleService
    {
        private readonly IControleScolaireRepository _controles;
        private readonly Workers.ReveilDocuments _reveil;
        private readonly ILogger<CoursDuControleService> _logger;

        public CoursDuControleService(
            IControleScolaireRepository controles,
            Workers.ReveilDocuments reveil,
            ILogger<CoursDuControleService> logger)
        {
            _controles = controles ?? throw new ArgumentNullException(nameof(controles));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultatDocumentControle> AjouterAsync(
            int eleveId, int controleId, string? nomFichier, string? typeAnnonce, byte[] donnees,
            CancellationToken ct)
        {
            // LES MÊMES RÈGLES QUE LES PIÈCES JOINTES : images et PDF reconnus
            // par leurs octets, 5 Mo, 10 pages. Un cours n'a pas de raison
            // d'en avoir d'autres.
            var verdict = ValidationPieceJointe.Valider(donnees, typeAnnonce);
            if (!verdict.Accepte) return new ResultatDocumentControle(null, verdict.Motif);

            var nom = string.IsNullOrWhiteSpace(nomFichier) ? "cours" : Path.GetFileName(nomFichier).Trim();
            if (nom.Length > 200) nom = nom[^200..];

            var document = await _controles.AjouterDocumentAsync(eleveId, controleId, new DocumentControle
            {
                NomFichier = nom,
                TypeMime = ValidationPieceJointe.ReconnaitreType(donnees)!,
                Taille = donnees.Length,
                NombrePages = verdict.NombrePages,
                Donnees = donnees,
                DateCreation = DateTime.UtcNow,
            }, ct);

            if (document is null)
            {
                return new ResultatDocumentControle(null,
                    $"Tu as déjà importé {DocumentControle.Maximum} documents pour ce contrôle. "
                    + "Retires-en un pour en ajouter un autre.");
            }

            // LU TOUT DE SUITE, EN TÂCHE DE FOND : quand l'élève cliquera
            // « Préparer », le texte sera prêt — et sinon la séance le lira.
            _reveil.Sonner();

            _logger.LogInformation(
                "Cours du controle {ControleId} : document {Id} ({Type}, {Taille} octets, {Pages} pages).",
                controleId, document.Id, document.TypeMime, document.Taille, document.NombrePages);

            // Les octets ne repartent pas vers l'écran.
            document.Donnees = null;
            return new ResultatDocumentControle(document, null);
        }
    }
}
