using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Le document enregistré, ou le motif du refus — écrit pour l'élève.</summary>
    /// <param name="StockageIndisponible">Le dossier du disque n'est pas prêt : 503, et non 400.</param>
    public record ResultatDocumentControle(DocumentControle? Document, string? Motif, bool StockageIndisponible = false);

    /// <summary>
    /// LE COURS DE L'ÉLÈVE, IMPORTÉ AVEC SON CONTRÔLE — voulu par Camara le
    /// 26/09/2026. Une seule porte pour les deux chemins, l'ordinateur et le
    /// téléphone : un document arrivé par le QR code passe exactement les
    /// mêmes contrôles, comme pour les pièces jointes d'une séance.
    ///
    /// SUR LE DISQUE, ET CONSULTABLE PAR L'ÉLÈVE — Camara, le 04/11/2026. Le fichier
    /// est rangé sous <c>/donnees/cours-controles</c> (jamais en base), l'élève le
    /// rouvre depuis sa fiche tant qu'il y est, et la purge l'efface une semaine
    /// après le contrôle. Sans la marque du dossier : refus, aucun repli en base.
    ///
    /// WORD, EXCEL, POWERPOINT : le texte est extrait ici, à l'envoi, sans modèle ;
    /// le document est « lu » tout de suite. Les images et les PDF gardent leur
    /// transcription par le modèle, en tâche de fond.
    /// </summary>
    public class CoursDuControleService
    {
        /// <summary>Le message de l'élève quand le dossier n'est pas monté.</summary>
        public const string MessageStockageIndisponible =
            "L'envoi de documents est indisponible pour le moment. Tu peux réessayer un peu plus tard.";

        private readonly IControleScolaireRepository _controles;
        private readonly IStockageCoursControles _stockage;
        private readonly Workers.ReveilDocuments _reveil;
        private readonly ILogger<CoursDuControleService> _logger;

        public CoursDuControleService(
            IControleScolaireRepository controles,
            IStockageCoursControles stockage,
            Workers.ReveilDocuments reveil,
            ILogger<CoursDuControleService> logger)
        {
            _controles = controles ?? throw new ArgumentNullException(nameof(controles));
            _stockage = stockage ?? throw new ArgumentNullException(nameof(stockage));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultatDocumentControle> AjouterAsync(
            int eleveId, int controleId, string? nomFichier, string? typeAnnonce, byte[] donnees,
            CancellationToken ct)
        {
            if (!_stockage.Pret)
            {
                _logger.LogWarning("Stockage des cours de controle NON PRET (marque STOCKAGE-MIMIA absente) : document refuse.");
                return new ResultatDocumentControle(null, MessageStockageIndisponible, StockageIndisponible: true);
            }

            var verdict = ValidationDocumentCours.Valider(donnees, typeAnnonce);
            if (!verdict.Accepte) return new ResultatDocumentControle(null, verdict.Motif);

            var nom = string.IsNullOrWhiteSpace(nomFichier) ? "cours" : Path.GetFileName(nomFichier).Trim();
            if (nom.Length > 200) nom = nom[^200..];

            var type = verdict.TypeMime!;
            string? transcription = null;

            if (ExtracteurTexteOffice.EstOffice(type))
            {
                try
                {
                    transcription = ExtracteurTexteOffice.Extraire(donnees, type);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Texte d'un document Office illisible ({Type}).", type);
                }

                if (string.IsNullOrWhiteSpace(transcription)) transcription = DocumentControle.MentionVide;
            }

            // LE CONTRÔLE EST-IL À L'ÉLÈVE, ET A-T-IL DE LA PLACE ? Vérifié par l'ajout en
            // base ; le fichier est écrit avant, et effacé si l'ajout refuse.
            var chemin = await _stockage.EcrireAsync(eleveId, controleId, donnees, nom, ct);

            DocumentControle? document;
            try
            {
                document = await _controles.AjouterDocumentAsync(eleveId, controleId, new DocumentControle
                {
                    NomFichier = nom,
                    TypeMime = type,
                    Taille = donnees.Length,
                    NombrePages = verdict.NombrePages,
                    Chemin = chemin,
                    Transcription = transcription,
                    DateCreation = DateTime.UtcNow,
                }, ct);
            }
            catch
            {
                _stockage.Supprimer(chemin);
                throw;
            }

            if (document is null)
            {
                _stockage.Supprimer(chemin);
                return new ResultatDocumentControle(null,
                    $"Tu as déjà importé {DocumentControle.Maximum} documents pour ce contrôle. "
                    + "Retires-en un pour en ajouter un autre.");
            }

            // LU TOUT DE SUITE, EN TÂCHE DE FOND : quand l'élève cliquera
            // « Préparer », le texte sera prêt — et sinon la séance le lira.
            if (transcription is null) _reveil.Sonner();

            _logger.LogInformation(
                "Cours du controle {ControleId} : document {Id} ({Type}, {Taille} octets, {Pages} pages) sur le disque.",
                controleId, document.Id, document.TypeMime, document.Taille, document.NombrePages);

            return new ResultatDocumentControle(document, null);
        }

        /// <summary>Retire un document et efface son fichier ; faux s'il n'existe pas ou n'est pas à cet élève.</summary>
        public async Task<bool> RetirerAsync(int eleveId, int controleId, int documentId, CancellationToken ct)
        {
            var retire = await _controles.SupprimerDocumentAsync(eleveId, controleId, documentId, ct);
            if (retire is null) return false;

            _stockage.Supprimer(retire.Chemin);
            return true;
        }

        /// <summary>Les octets du fichier pour l'aperçu ; null quand il n'est plus là (purgé, ou d'avant le disque).</summary>
        public async Task<(DocumentControle Document, byte[] Octets)?> LireAsync(
            int eleveId, int controleId, int documentId, CancellationToken ct)
        {
            var document = await _controles.GetDocumentAsync(eleveId, controleId, documentId, ct);
            if (document?.Chemin is null) return null;

            var octets = await _stockage.LireAsync(document.Chemin, ct);
            return octets is null ? null : (document, octets);
        }
    }
}
