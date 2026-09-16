using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class ModeleMailRepository : IModeleMailRepository
    {
        /// <summary>
        /// Les courriels reliés à du code d'envoi : jamais supprimés, par
        /// aucun appelant. La liste vit ici, dans la condition de la requête,
        /// pour qu'aucune porte ne l'oublie.
        /// </summary>
        private static readonly string[] CodesProteges =
        [
            CodeModeleMail.Bilans,
            CodeModeleMail.FinEssai,
            CodeModeleMail.AvisEssai,
            CodeModeleMail.AvisGeneral,
        ];

        private readonly SchoolWebAppDatabaseContext _context;

        public ModeleMailRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// LA PROJECTION DE LA LISTE, SANS TEXTE NI OCTETS.
        ///
        /// Les compteurs et le poids sont calculés par SQL Server en
        /// sous-requêtes : les pièces ne sont pas chargées pour les compter.
        /// Sans `Select`, EF matérialiserait chaque modèle avec son texte, et
        /// une liste de vingt templates tirerait tout ce qu'ils contiennent.
        /// </summary>
        private static readonly Expression<Func<ModeleMail, ModeleMailResume>> Resume =
            m => new ModeleMailResume
            {
                Id = m.Id,
                Nature = m.Nature,
                Code = m.Code,
                Nom = m.Nom,
                Description = m.Description,
                Sujet = m.Sujet,
                Frequence = m.Frequence,
                HeureEnvoi = m.HeureEnvoi,
                JourSemaine = m.JourSemaine,
                JourMois = m.JourMois,
                Actif = m.Actif,
                DerniereOccurrence = m.DerniereOccurrence,
                DernierEnvoiLe = m.DernierEnvoiLe,
                DernierResultat = m.DernierResultat,
                DateCreation = m.DateCreation,
                DateModification = m.DateModification,
                NombreImages = m.Pieces.Count(p => p.Genre == GenrePieceMail.Image),
                NombreDocuments = m.Pieces.Count(p => p.Genre == GenrePieceMail.Document),
                PoidsTotal = m.Pieces.Sum(p => (int?)p.Taille) ?? 0,
            };

        public async Task<IReadOnlyList<ModeleMailResume>> GetResumesAsync(
            string nature, CancellationToken ct = default)
        {
            var requete = _context.ModelesMail.AsNoTracking().Where(m => m.Nature == nature);

            // Les automatiques gardent l'ordre où ils ont été créés — celui du
            // menu, les nouveaux à la suite. Les diffusions, la plus récemment
            // touchée en tête : c'est celle qu'on vient reprendre.
            requete = nature == NatureModeleMail.Automatique
                ? requete.OrderBy(m => m.Id)
                : requete.OrderByDescending(m => m.DateModification ?? m.DateCreation);

            return await requete.Select(Resume).ToListAsync(ct);
        }

        public async Task<ModeleMailDetail?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            var detail = await _context.ModelesMail
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new ModeleMailDetail
                {
                    Id = m.Id,
                    Nature = m.Nature,
                    Code = m.Code,
                    Nom = m.Nom,
                    Description = m.Description,
                    Sujet = m.Sujet,
                    Titre = m.Titre,
                    Texte = m.Texte,
                    Frequence = m.Frequence,
                    HeureEnvoi = m.HeureEnvoi,
                    JourSemaine = m.JourSemaine,
                    JourMois = m.JourMois,
                    Actif = m.Actif,
                    DerniereOccurrence = m.DerniereOccurrence,
                    DernierEnvoiLe = m.DernierEnvoiLe,
                    DernierResultat = m.DernierResultat,
                    DateCreation = m.DateCreation,
                    DateModification = m.DateModification,
                })
                .FirstOrDefaultAsync(ct);

            if (detail is null) return null;

            var pieces = await _context.PiecesModelesMail
                .AsNoTracking()
                .Where(p => p.ModeleMailId == id)
                .OrderBy(p => p.Genre)
                .ThenBy(p => p.Rang)
                .Select(p => new PieceModeleMailInfo
                {
                    Id = p.Id,
                    Genre = p.Genre,
                    Rang = p.Rang,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                })
                .ToListAsync(ct);

            detail.Images = pieces.Where(p => p.Genre == GenrePieceMail.Image).ToList();
            detail.Documents = pieces.Where(p => p.Genre == GenrePieceMail.Document).ToList();
            detail.NombreImages = detail.Images.Count;
            detail.NombreDocuments = detail.Documents.Count;
            detail.PoidsTotal = pieces.Sum(p => p.Taille);

            return detail;
        }

        public async Task<IReadOnlyList<PieceModeleMailContenu>> GetPiecesAsync(
            int id, CancellationToken ct = default) =>
            await _context.PiecesModelesMail
                .AsNoTracking()
                .Where(p => p.ModeleMailId == id)
                .OrderBy(p => p.Genre)
                .ThenBy(p => p.Rang)
                .Select(p => new PieceModeleMailContenu
                {
                    Id = p.Id,
                    Genre = p.Genre,
                    Rang = p.Rang,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                    Donnees = p.Donnees,
                })
                .ToListAsync(ct);

        public Task<PieceModeleMailContenu?> GetPieceAsync(
            int id, int pieceId, CancellationToken ct = default) =>
            _context.PiecesModelesMail
                .AsNoTracking()
                .Where(p => p.Id == pieceId && p.ModeleMailId == id)
                .Select(p => new PieceModeleMailContenu
                {
                    Id = p.Id,
                    Genre = p.Genre,
                    Rang = p.Rang,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                    Donnees = p.Donnees,
                })
                .FirstOrDefaultAsync(ct);

        public async Task<ModeleMailDetail> CreerAsync(
            string nature,
            string nom,
            string? description,
            string sujet,
            string titre,
            string texte,
            IReadOnlyList<NouvellePieceMail> images,
            IReadOnlyList<NouvellePieceMail> documents,
            CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;

            // SANS CODE : un courriel automatique créé depuis l'écran n'est relié
            // à aucune règle d'envoi. Il ne se programme pas tant qu'on ne l'a
            // pas écrite, comme Relance et Rappels.
            var modele = new ModeleMail
            {
                Nature = nature,
                Nom = nom,
                Description = description,
                Sujet = sujet,
                Titre = titre,
                Texte = texte,
                Frequence = FrequenceEnvoi.Aucune,
                DateCreation = maintenant,
                DateModification = maintenant,
            };

            // L'ORDRE REÇU FAIT LE RANG : la première image envoyée par l'écran
            // est celle que le texte appelle `[image:1]`.
            Ajouter(modele, GenrePieceMail.Image, images, maintenant);
            Ajouter(modele, GenrePieceMail.Document, documents, maintenant);

            _context.ModelesMail.Add(modele);
            await _context.SaveChangesAsync(ct);

            return (await GetDetailAsync(modele.Id, ct))!;
        }

        private static void Ajouter(
            ModeleMail modele, string genre, IReadOnlyList<NouvellePieceMail> pieces, DateTime date)
        {
            for (var i = 0; i < pieces.Count; i++)
            {
                modele.Pieces.Add(new PieceModeleMail
                {
                    Genre = genre,
                    Rang = i + 1,
                    NomFichier = pieces[i].NomFichier,
                    TypeMime = pieces[i].TypeMime,
                    Taille = pieces[i].Donnees.Length,
                    Donnees = pieces[i].Donnees,
                    DateCreation = date,
                });
            }
        }

        public async Task<DateTime?> ModifierContenuAsync(
            int id,
            string? nom,
            string? description,
            string sujet,
            string titre,
            string texte,
            CancellationToken ct = default)
        {
            // Sans `Include` : les pièces et leurs octets restent en base, seul
            // le texte du modèle est chargé pour être modifié.
            var modele = await _context.ModelesMail.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (modele is null) return null;

            if (nom is not null) modele.Nom = nom;
            if (description is not null) modele.Description = description;

            modele.Sujet = sujet;
            modele.Titre = titre;
            modele.Texte = texte;
            modele.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return modele.DateModification;
        }

        public async Task<ResultatAjoutPiece> AjouterPieceAsync(
            int id,
            string genre,
            NouvellePieceMail piece,
            int poidsMax,
            CancellationToken ct = default)
        {
            var modele = await _context.ModelesMail.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (modele is null) return new ResultatAjoutPiece(null, Introuvable: true, TropLourd: false);

            // LE PLAFOND PORTE SUR LE MODÈLE ENTIER, pas sur le fichier : c'est
            // le poids du courriel qui décide s'il arrive chez le parent.
            var poids = await _context.PiecesModelesMail
                .Where(p => p.ModeleMailId == id)
                .SumAsync(p => (int?)p.Taille, ct) ?? 0;

            if (poids + piece.Donnees.Length > poidsMax)
            {
                return new ResultatAjoutPiece(null, Introuvable: false, TropLourd: true);
            }

            var rang = (await _context.PiecesModelesMail
                .Where(p => p.ModeleMailId == id && p.Genre == genre)
                .MaxAsync(p => (int?)p.Rang, ct) ?? 0) + 1;

            var ligne = new PieceModeleMail
            {
                ModeleMailId = id,
                Genre = genre,
                Rang = rang,
                NomFichier = piece.NomFichier,
                TypeMime = piece.TypeMime,
                Taille = piece.Donnees.Length,
                Donnees = piece.Donnees,
                DateCreation = DateTime.UtcNow,
            };

            _context.PiecesModelesMail.Add(ligne);
            modele.DateModification = ligne.DateCreation;

            await _context.SaveChangesAsync(ct);

            return new ResultatAjoutPiece(
                new PieceModeleMailInfo
                {
                    Id = ligne.Id,
                    Genre = ligne.Genre,
                    Rang = ligne.Rang,
                    NomFichier = ligne.NomFichier,
                    TypeMime = ligne.TypeMime,
                    Taille = ligne.Taille,
                },
                Introuvable: false,
                TropLourd: false);
        }

        /// <summary>
        /// LES TROIS ÉCRITURES DANS UNE SEULE TRANSACTION.
        ///
        /// Supprimer la pièce, faire reculer les rangs suivants, réécrire les
        /// marqueurs du texte : une coupure entre deux d'entre elles laisserait
        /// `[image:3]` pointer une image qui est désormais la deuxième, ou un
        /// texte renuméroté pour une image encore là. Aucun des deux ne se voit
        /// avant qu'un parent reçoive la mauvaise image au mauvais endroit.
        /// </summary>
        public async Task<ModeleMailDetail?> RetirerPieceAsync(
            int id, int pieceId, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            var modele = await _context.ModelesMail.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (modele is null) return null;

            var piece = await _context.PiecesModelesMail
                .Where(p => p.Id == pieceId && p.ModeleMailId == id)
                .Select(p => new { p.Genre, p.Rang })
                .FirstOrDefaultAsync(ct);

            // Déjà retirée — un double clic : rien à faire, le modèle tel quel.
            if (piece is null) return await GetDetailAsync(id, ct);

            await _context.PiecesModelesMail
                .Where(p => p.Id == pieceId)
                .ExecuteDeleteAsync(ct);

            await _context.PiecesModelesMail
                .Where(p => p.ModeleMailId == id && p.Genre == piece.Genre && p.Rang > piece.Rang)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Rang, p => p.Rang - 1), ct);

            if (piece.Genre == GenrePieceMail.Image)
            {
                modele.Texte = MarqueursImages.RetirerImage(modele.Texte, piece.Rang);
            }

            modele.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return await GetDetailAsync(id, ct);
        }

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            // Les codes protégés sont DANS la condition : un courriel relié à un
            // envoi ne peut pas disparaître par cette porte, même appelée par
            // erreur. Les pièces partent avec, par la cascade de la clé étrangère.
            var supprimes = await _context.ModelesMail
                .Where(m => m.Id == id && (m.Code == null || !CodesProteges.Contains(m.Code)))
                .ExecuteDeleteAsync(ct);

            return supprimes > 0;
        }

        public Task NoterEnvoiAsync(
            int id, DateTime dateUtc, string? resultat, CancellationToken ct = default) =>
            _context.ModelesMail
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.DernierEnvoiLe, dateUtc)
                    .SetProperty(m => m.DernierResultat, resultat), ct);

        public Task NoterResultatAsync(int id, string resultat, CancellationToken ct = default) =>
            _context.ModelesMail
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.DernierResultat, resultat), ct);

        // ------------------------------------------------- courriels automatiques

        public async Task<ModeleMailDetail?> GetParCodeAsync(string code, CancellationToken ct = default)
        {
            var id = await _context.ModelesMail
                .AsNoTracking()
                .Where(m => m.Code == code)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync(ct);

            return id is null ? null : await GetDetailAsync(id.Value, ct);
        }

        public async Task<IReadOnlyList<ModeleMailResume>> GetAutomatiquesActifsAsync(CancellationToken ct = default) =>
            await _context.ModelesMail
                .AsNoTracking()
                .Where(m => m.Nature == NatureModeleMail.Automatique && m.Actif)
                .OrderBy(m => m.Id)
                .Select(Resume)
                .ToListAsync(ct);

        public async Task<bool> DefinirPlanificationAsync(
            int id,
            string frequence,
            TimeOnly? heure,
            int? jourSemaine,
            int? jourMois,
            bool actif,
            DateTime? derniereOccurrence,
            CancellationToken ct = default)
        {
            var modifies = await _context.ModelesMail
                .Where(m => m.Id == id && m.Nature == NatureModeleMail.Automatique)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.Frequence, frequence)
                    .SetProperty(m => m.HeureEnvoi, heure)
                    .SetProperty(m => m.JourSemaine, jourSemaine)
                    .SetProperty(m => m.JourMois, jourMois)
                    .SetProperty(m => m.Actif, actif)
                    .SetProperty(m => m.DerniereOccurrence, derniereOccurrence)
                    .SetProperty(m => m.DateModification, DateTime.UtcNow), ct);

            return modifies > 0;
        }

        /// <summary>
        /// UNE SEULE ÉCRITURE, CONDITIONNELLE : c'est ce qui la rend sûre. Lire
        /// la dernière occurrence puis l'écrire en deux temps laisserait deux
        /// passages concurrents la prendre tous les deux.
        /// </summary>
        public async Task<bool> ReserverOccurrenceAsync(int id, DateTime occurrenceUtc, CancellationToken ct = default)
        {
            var modifies = await _context.ModelesMail
                .Where(m => m.Id == id
                            && m.Actif
                            && (m.DerniereOccurrence == null || m.DerniereOccurrence < occurrenceUtc))
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.DerniereOccurrence, occurrenceUtc), ct);

            return modifies > 0;
        }
    }
}
