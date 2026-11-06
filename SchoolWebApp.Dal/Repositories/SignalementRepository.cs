using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using DomainCommentaire = SchoolWebApp.Domain.Models.SignalementCommentaire;
using DomainPiece = SchoolWebApp.Domain.Models.SignalementPiece;
using DomainSignalement = SchoolWebApp.Domain.Models.Signalement;

namespace SchoolWebApp.Dal.Repositories
{
    public class SignalementRepository : ISignalementRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public SignalementRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DomainSignalement> CreerAsync(
            int parentId, int? eleveId, string categorie, string description,
            CancellationToken ct = default)
        {
            var entite = new Signalement
            {
                ParentId = parentId,
                EleveId = eleveId,
                Categorie = categorie,
                Description = description,
                Etat = "nouveau",
                DateCreation = DateTime.UtcNow,
            };

            _context.Signalements.Add(entite);
            await _context.SaveChangesAsync(ct);

            return await _context.Signalements
                .AsNoTracking()
                .Where(s => s.Id == entite.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<DomainSignalement> CreerAdminAsync(
            int parentId, string categorie, string description, string? competence, int? ligneDeTestId,
            CancellationToken ct = default)
        {
            var entite = new Signalement
            {
                ParentId = parentId,
                Categorie = categorie,
                Description = description,
                Etat = "nouveau",
                DateCreation = DateTime.UtcNow,
                EstAdmin = true,
                Competence = competence,
                LigneDeTestId = ligneDeTestId,
            };

            _context.Signalements.Add(entite);
            await _context.SaveChangesAsync(ct);

            return await _context.Signalements
                .AsNoTracking()
                .Where(s => s.Id == entite.Id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        // CEUX DES PARENTS SEULEMENT : ceux des administrateurs ont leur onglet (05/11/2026).
        public async Task<IEnumerable<DomainSignalement>> GetTousAsync(CancellationToken ct = default) =>
            await _context.Signalements
                .AsNoTracking()
                .Where(s => !s.EstAdmin)
                .OrderByDescending(s => s.DateCreation)
                .Select(Projection)
                .ToListAsync(ct);

        public async Task<IEnumerable<DomainSignalement>> GetAdminAsync(CancellationToken ct = default) =>
            await _context.Signalements
                .AsNoTracking()
                .Where(s => s.EstAdmin)
                .OrderByDescending(s => s.DateCreation)
                .Select(Projection)
                .ToListAsync(ct);

        public async Task<DomainSignalement?> GetByIdAsync(int id, CancellationToken ct = default) =>
            await _context.Signalements
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(Projection)
                .FirstOrDefaultAsync(ct);

        public async Task<DomainSignalement?> ModifierAsync(
            int id, string categorie, string description, string etat, string? competence = null, CancellationToken ct = default)
        {
            var entite = await _context.Signalements.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entite is null) return null;

            entite.Categorie = categorie;
            entite.Description = description;
            entite.Etat = etat;
            if (entite.EstAdmin && !string.IsNullOrWhiteSpace(competence)) entite.Competence = competence.Trim();
            entite.DateMiseAJour = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await _context.Signalements
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(Projection)
                .FirstAsync(ct);
        }

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            var entite = await _context.Signalements.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entite is null) return false;

            _context.Signalements.Remove(entite);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<DomainCommentaire>> GetCommentairesAsync(
            int signalementId, CancellationToken ct = default) =>
            await _context.SignalementCommentaires
                .AsNoTracking()
                .Where(c => c.SignalementId == signalementId)
                .OrderBy(c => c.DateCreation)
                .ThenBy(c => c.Id)
                .Select(c => new DomainCommentaire
                {
                    Id = c.Id,
                    SignalementId = c.SignalementId,
                    AuteurMail = c.AuteurMail,
                    Texte = c.Texte,
                    DateCreation = c.DateCreation,
                    DateMiseAJour = c.DateMiseAJour,
                })
                .ToListAsync(ct);

        public async Task<DomainCommentaire?> AjouterCommentaireAsync(
            int signalementId, string? auteurMail, string texte, CancellationToken ct = default)
        {
            if (!await _context.Signalements.AnyAsync(s => s.Id == signalementId, ct)) return null;

            var entite = new SignalementCommentaire
            {
                SignalementId = signalementId,
                AuteurMail = auteurMail,
                Texte = texte,
                DateCreation = DateTime.UtcNow,
            };

            _context.SignalementCommentaires.Add(entite);
            await _context.SaveChangesAsync(ct);

            return VersDomaine(entite);
        }

        public async Task<DomainCommentaire?> ModifierCommentaireAsync(
            int signalementId, int id, string texte, CancellationToken ct = default)
        {
            var entite = await _context.SignalementCommentaires
                .FirstOrDefaultAsync(c => c.Id == id && c.SignalementId == signalementId, ct);

            if (entite is null) return null;

            entite.Texte = texte;
            entite.DateMiseAJour = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return VersDomaine(entite);
        }

        public async Task<bool> SupprimerCommentaireAsync(int signalementId, int id, CancellationToken ct = default)
        {
            var entite = await _context.SignalementCommentaires
                .FirstOrDefaultAsync(c => c.Id == id && c.SignalementId == signalementId, ct);

            if (entite is null) return false;

            _context.SignalementCommentaires.Remove(entite);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<DomainPiece>> GetPiecesAsync(int signalementId, CancellationToken ct = default)
        {
            var pieces = await _context.SignalementPieces
                .AsNoTracking()
                .Where(p => p.SignalementId == signalementId)
                .OrderBy(p => p.DateCreation)
                .ThenBy(p => p.Id)
                .ToListAsync(ct);

            return pieces.Select(VersDomaine).ToList();
        }

        public async Task<DomainPiece> AjouterPieceAsync(DomainPiece piece, CancellationToken ct = default)
        {
            var entite = new SignalementPiece
            {
                SignalementId = piece.SignalementId,
                Nom = piece.Nom,
                TypeMime = piece.TypeMime,
                Taille = piece.Taille,
                EstImage = piece.EstImage,
                Fichier = piece.Fichier,
                AjoutePar = piece.AjoutePar,
                DateCreation = DateTime.UtcNow,
            };

            _context.SignalementPieces.Add(entite);
            await _context.SaveChangesAsync(ct);

            return VersDomaine(entite);
        }

        public async Task<DomainPiece?> RetirerPieceAsync(int signalementId, int id, CancellationToken ct = default)
        {
            var entite = await _context.SignalementPieces
                .FirstOrDefaultAsync(p => p.Id == id && p.SignalementId == signalementId, ct);

            if (entite is null) return null;

            _context.SignalementPieces.Remove(entite);
            await _context.SaveChangesAsync(ct);

            return VersDomaine(entite);
        }

        public async Task<IReadOnlyList<int>> IdsExistantsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            var liste = ids.ToList();

            return await _context.Signalements
                .AsNoTracking()
                .Where(s => liste.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync(ct);
        }

        private static DomainPiece VersDomaine(SignalementPiece p) => new()
        {
            Id = p.Id,
            SignalementId = p.SignalementId,
            Nom = p.Nom,
            TypeMime = p.TypeMime,
            Taille = p.Taille,
            EstImage = p.EstImage,
            Fichier = p.Fichier,
            AjoutePar = p.AjoutePar,
            DateCreation = p.DateCreation,
        };

        private static DomainCommentaire VersDomaine(SignalementCommentaire c) => new()
        {
            Id = c.Id,
            SignalementId = c.SignalementId,
            AuteurMail = c.AuteurMail,
            Texte = c.Texte,
            DateCreation = c.DateCreation,
            DateMiseAJour = c.DateMiseAJour,
        };

        private static readonly Expression<Func<Signalement, DomainSignalement>> Projection =
            s => new DomainSignalement
            {
                Id = s.Id,
                ParentId = s.ParentId,
                EleveId = s.EleveId,
                Categorie = s.Categorie,
                Description = s.Description,
                Etat = s.Etat,
                DateCreation = s.DateCreation,
                DateMiseAJour = s.DateMiseAJour,
                ParentMail = s.Parent!.Mail,
                ElevePrenom = s.Eleve == null ? null : s.Eleve.Prenom,
                NombreCommentaires = s.Commentaires.Count,
                NombrePieces = s.Pieces.Count,
                EstAdmin = s.EstAdmin,
                Competence = s.Competence,
                LigneDeTestId = s.LigneDeTestId,
            };
    }
}
