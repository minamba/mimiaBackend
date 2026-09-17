using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Le carnet d'idées.
    ///
    /// AUCUNE PROJECTION NE RAMÈNE `donnees`. Chaque `Select` ci-dessous nomme
    /// ses colonnes une par une, et c'est délibéré : un `Include(i => i.Pieces)`
    /// tirerait les octets de toutes les images de toutes les idées pour
    /// dessiner un tableau de titres.
    /// </summary>
    public class IdeeRepository : IIdeeRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public IdeeRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Idee>> GetToutesAsync(CancellationToken ct = default)
        {
            return await _context.IdeesEvolution
                .AsNoTracking()
                .OrderByDescending(i => i.DateCreation)
                .Select(i => new Idee
                {
                    Id = i.Id,
                    Titre = i.Titre,
                    Urgence = i.Urgence,
                    Description = i.Description,
                    Statut = i.Statut,
                    AuteurPrenom = i.AuteurPrenom,
                    AuteurNom = i.AuteurNom,
                    DateCreation = i.DateCreation,
                    DateModification = i.DateModification,
                    // LE RANG D'ABORD, PUIS L'ORDRE DE DÉPÔT. Les images portent
                    // un rang à partir de 1 et sortent dans l'ordre de leurs
                    // marqueurs ; les autres pièces sont toutes à zéro et se
                    // suivent par identifiant, c'est-à-dire par ordre d'arrivée.
                    Pieces = i.Pieces
                        .OrderBy(p => p.Rang)
                        .ThenBy(p => p.Id)
                        .Select(p => new PieceJointeIdee
                        {
                            Id = p.Id,
                            Rang = p.Rang,
                            NomFichier = p.NomFichier,
                            TypeMime = p.TypeMime,
                            Taille = p.Taille,
                        })
                        .ToList(),
                })
                .ToListAsync(ct);
        }

        public async Task<Idee?> GetAsync(int id, CancellationToken ct = default)
        {
            return await _context.IdeesEvolution
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(i => new Idee
                {
                    Id = i.Id,
                    Titre = i.Titre,
                    Urgence = i.Urgence,
                    Description = i.Description,
                    Statut = i.Statut,
                    AuteurPrenom = i.AuteurPrenom,
                    AuteurNom = i.AuteurNom,
                    DateCreation = i.DateCreation,
                    DateModification = i.DateModification,
                    // LE RANG D'ABORD, PUIS L'ORDRE DE DÉPÔT. Les images portent
                    // un rang à partir de 1 et sortent dans l'ordre de leurs
                    // marqueurs ; les autres pièces sont toutes à zéro et se
                    // suivent par identifiant, c'est-à-dire par ordre d'arrivée.
                    Pieces = i.Pieces
                        .OrderBy(p => p.Rang)
                        .ThenBy(p => p.Id)
                        .Select(p => new PieceJointeIdee
                        {
                            Id = p.Id,
                            Rang = p.Rang,
                            NomFichier = p.NomFichier,
                            TypeMime = p.TypeMime,
                            Taille = p.Taille,
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<ImageIdee?> GetPieceAsync(
            int ideeId, int pieceId, CancellationToken ct = default)
        {
            // L'idée est dans la clause : sans elle, un identifiant de pièce
            // suffirait à lire celle d'une autre idée en devinant un nombre.
            var piece = await _context.PiecesIdees
                .AsNoTracking()
                .Where(p => p.Id == pieceId && p.IdeeEvolutionId == ideeId)
                .Select(p => new { p.Donnees, p.TypeMime, p.NomFichier })
                .FirstOrDefaultAsync(ct);

            return piece is null
                ? null
                : new ImageIdee(piece.Donnees, piece.TypeMime, piece.NomFichier);
        }

        public async Task<Idee> CreerAsync(
            string titre, string urgence, string? description,
            string? auteurPrenom, string? auteurNom, CancellationToken ct = default)
        {
            var idee = new IdeeEvolution
            {
                Titre = titre,
                Urgence = urgence,
                Description = description,
                AuteurPrenom = auteurPrenom,
                AuteurNom = auteurNom,

                // LE STATUT N'EST PAS UN PARAMÈTRE, et c'est voulu : une idée
                // naît « Nouveau », toujours. Laisser l'appelant le choisir
                // ouvrirait la porte à une idée créée « En production » par
                // mégarde, et le carnet perdrait son seul repère chronologique.
                Statut = StatutIdee.Nouveau,
                DateCreation = DateTime.UtcNow,
            };

            _context.IdeesEvolution.Add(idee);
            await _context.SaveChangesAsync(ct);

            return new Idee
            {
                Id = idee.Id,
                Titre = idee.Titre,
                Urgence = idee.Urgence,
                Description = idee.Description,
                Statut = idee.Statut,
                AuteurPrenom = idee.AuteurPrenom,
                AuteurNom = idee.AuteurNom,
                DateCreation = idee.DateCreation,
            };
        }

        public async Task<Idee?> ModifierAsync(
            int id, string titre, string urgence, string? description, string statut,
            CancellationToken ct = default)
        {
            var idee = await _context.IdeesEvolution
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (idee is null) return null;

            idee.Titre = titre;
            idee.Urgence = urgence;
            idee.Description = description;
            idee.Statut = statut;
            idee.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return await GetAsync(id, ct);
        }

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            var idee = await _context.IdeesEvolution
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (idee is null) return false;

            // LES PIÈCES PARTENT AVEC, ET ENTIÈREMENT — Camara, le 17/09/2026 :
            // « il ne doit rester aucune trace sinon le serveur va être saturé de
            // vieux documents inutiles ».
            //
            // Rien n'est écrit sur le disque : les octets d'une image, d'un audio
            // ou d'un PDF sont dans la colonne `donnees` de `PieceIdee`. La
            // cascade déclarée dans le contexte ET dans la contrainte SQL
            // (`FK_PieceIdee_IdeeEvolution_idee_evolution_id`, ON DELETE CASCADE)
            // efface donc la ligne et ses octets d'un seul coup. Il n'y a aucun
            // fichier orphelin possible, par construction.
            _context.IdeesEvolution.Remove(idee);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<PieceJointeIdee?> AjouterPieceAsync(
            int ideeId, string nomFichier, string typeMime, byte[] donnees,
            CancellationToken ct = default)
        {
            var existe = await _context.IdeesEvolution.AnyAsync(i => i.Id == ideeId, ct);
            if (!existe) return null;

            // SEULES LES IMAGES SONT NUMÉROTÉES, et il le faut : le rang est ce
            // que `[image:N]` désigne dans le texte. Un PDF déposé entre deux
            // captures aurait pris le numéro suivant, et toutes les images
            // postérieures se seraient décalées d'un cran par rapport à leurs
            // marqueurs — sans erreur nulle part, juste la mauvaise image au
            // mauvais endroit.
            //
            // Zéro pour tout le reste : un audio et un document ne vivent pas
            // dans le texte, ils se posent en dessous comme les pièces jointes
            // d'un courriel.
            var estImage = GenrePiece.Deduire(typeMime) == GenrePiece.Image;

            // Le rang suivant, lu en base et non compté en mémoire : deux
            // téléversements simultanés se verraient attribuer le même numéro
            // si on se fiait à une liste chargée avant.
            var dernier = estImage
                ? await _context.PiecesIdees
                    .Where(p => p.IdeeEvolutionId == ideeId && p.Rang > 0)
                    .MaxAsync(p => (int?)p.Rang, ct) ?? 0
                : 0;

            var piece = new PieceIdee
            {
                IdeeEvolutionId = ideeId,
                Rang = estImage ? dernier + 1 : 0,
                NomFichier = nomFichier,
                TypeMime = typeMime,
                Taille = donnees.Length,
                Donnees = donnees,
                DateCreation = DateTime.UtcNow,
            };

            _context.PiecesIdees.Add(piece);
            await _context.SaveChangesAsync(ct);

            return new PieceJointeIdee
            {
                Id = piece.Id,
                Rang = piece.Rang,
                NomFichier = piece.NomFichier,
                TypeMime = piece.TypeMime,
                Taille = piece.Taille,
            };
        }

        public async Task<string?> RetirerPieceAsync(
            int ideeId, int pieceId, CancellationToken ct = default)
        {
            var piece = await _context.PiecesIdees
                .FirstOrDefaultAsync(p => p.Id == pieceId && p.IdeeEvolutionId == ideeId, ct);

            if (piece is null) return null;

            var idee = await _context.IdeesEvolution
                .FirstOrDefaultAsync(i => i.Id == ideeId, ct);

            if (idee is null) return null;

            var rang = piece.Rang;

            // DANS UNE TRANSACTION : la suppression, la renumérotation et la
            // réécriture du texte forment un seul geste. Interrompu au milieu,
            // il laisserait des marqueurs pointant sur les mauvaises images —
            // une incohérence muette, que rien ne viendrait corriger ensuite.
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            _context.PiecesIdees.Remove(piece);

            // RIEN À RENUMÉROTER POUR UN AUDIO OU UN DOCUMENT : ils sont tous à
            // zéro et ne sont cités nulle part dans le texte. Décaler « les rangs
            // supérieurs à 0 » aurait renuméroté toutes les images de l'idée
            // pour le retrait d'un PDF, et décroché chaque marqueur.
            if (rang > 0)
            {
                var suivantes = await _context.PiecesIdees
                    .Where(p => p.IdeeEvolutionId == ideeId && p.Rang > rang)
                    .ToListAsync(ct);

                foreach (var s in suivantes) s.Rang -= 1;

                idee.Description = MarqueursImages.RetirerImage(idee.Description, rang);
            }

            idee.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return idee.Description;
        }
    }
}
