using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Domain.Services.impl
{
    public class SignalementService : ISignalementService
    {
        private readonly ISignalementRepository _signalements;
        private readonly IStockageSignalements _stockage;

        public SignalementService(ISignalementRepository signalements, IStockageSignalements stockage)
        {
            _signalements = signalements ?? throw new ArgumentNullException(nameof(signalements));
            _stockage = stockage ?? throw new ArgumentNullException(nameof(stockage));
        }

        public Task<Signalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description, CancellationToken ct = default) =>
            _signalements.CreerAsync(parentId, eleveId, categorie, description, ct);

        public Task<IEnumerable<Signalement>> GetTousAsync(CancellationToken ct = default) =>
            _signalements.GetTousAsync(ct);

        public Task<Signalement> CreerAdminAsync(
            int parentId, string categorie, string description, string? competence, int? ligneDeTestId,
            CancellationToken ct = default) =>
            _signalements.CreerAdminAsync(parentId, categorie, description, competence, ligneDeTestId, ct);

        public Task<IEnumerable<Signalement>> GetAdminAsync(CancellationToken ct = default) =>
            _signalements.GetAdminAsync(ct);

        public Task<Signalement?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _signalements.GetByIdAsync(id, ct);

        public Task<Signalement?> ModifierAsync(
            int id, string categorie, string description, string etat, string? competence = null, CancellationToken ct = default) =>
            _signalements.ModifierAsync(id, categorie, description, etat, competence, ct);

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            if (!await _signalements.SupprimerAsync(id, ct)) return false;

            // Les pièces en base sont parties en cascade : leurs fichiers suivent.
            _stockage.SupprimerDossier(id);
            return true;
        }

        public Task<IEnumerable<SignalementPiece>> GetPiecesAsync(int signalementId, CancellationToken ct = default) =>
            _signalements.GetPiecesAsync(signalementId, ct);

        public async Task<AjoutPieceSignalement> AjouterPieceAsync(
            int signalementId, string? nom, byte[] octets, string? ajoutePar, CancellationToken ct = default)
        {
            // JAMAIS EN BASE, MÊME EN REPLI : sans le dossier monté sur l'hôte, on refuse.
            if (!_stockage.Pret)
            {
                return new AjoutPieceSignalement(null, "L'envoi de fichiers n'est pas disponible pour le moment.", StockageIndisponible: true);
            }

            var propre = ReglesPieceSignalement.NomPropre(nom);
            var refus = ReglesPieceSignalement.Refus(propre, octets, out var typeMime, out var estImage);
            if (refus is not null) return new AjoutPieceSignalement(null, refus);

            if (await _signalements.GetByIdAsync(signalementId, ct) is not { } signalement)
            {
                return new AjoutPieceSignalement(null, Introuvable: true);
            }

            if (signalement.NombrePieces >= ReglesPieceSignalement.MaxParSignalement)
            {
                return new AjoutPieceSignalement(null, $"Un signalement porte au plus {ReglesPieceSignalement.MaxParSignalement} fichiers.");
            }

            var fichier = await _stockage.EcrireAsync(signalementId, octets, propre, ct);

            try
            {
                var piece = await _signalements.AjouterPieceAsync(new SignalementPiece
                {
                    SignalementId = signalementId,
                    Nom = propre,
                    TypeMime = typeMime,
                    Taille = octets.Length,
                    EstImage = estImage,
                    Fichier = fichier,
                    AjoutePar = ajoutePar,
                }, ct);

                return new AjoutPieceSignalement(piece);
            }
            catch
            {
                _stockage.Supprimer(fichier);
                throw;
            }
        }

        public async Task<(SignalementPiece Piece, byte[] Octets)?> LirePieceAsync(
            int signalementId, int id, CancellationToken ct = default)
        {
            var piece = (await _signalements.GetPiecesAsync(signalementId, ct)).FirstOrDefault(p => p.Id == id);
            if (piece is null) return null;

            var octets = await _stockage.LireAsync(piece.Fichier, ct);
            return octets is null ? null : (piece, octets);
        }

        public async Task<bool> SupprimerPieceAsync(int signalementId, int id, CancellationToken ct = default)
        {
            var piece = await _signalements.RetirerPieceAsync(signalementId, id, ct);
            if (piece is null) return false;

            _stockage.Supprimer(piece.Fichier);
            return true;
        }

        public Task<IEnumerable<SignalementCommentaire>> GetCommentairesAsync(int signalementId, CancellationToken ct = default) =>
            _signalements.GetCommentairesAsync(signalementId, ct);

        public Task<SignalementCommentaire?> AjouterCommentaireAsync(
            int signalementId, string? auteurMail, string texte, CancellationToken ct = default) =>
            _signalements.AjouterCommentaireAsync(signalementId, auteurMail, texte, ct);

        public Task<SignalementCommentaire?> ModifierCommentaireAsync(
            int signalementId, int id, string texte, CancellationToken ct = default) =>
            _signalements.ModifierCommentaireAsync(signalementId, id, texte, ct);

        public Task<bool> SupprimerCommentaireAsync(int signalementId, int id, CancellationToken ct = default) =>
            _signalements.SupprimerCommentaireAsync(signalementId, id, ct);
    }
}
