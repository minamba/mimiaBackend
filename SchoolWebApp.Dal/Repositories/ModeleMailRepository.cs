using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        private readonly SchoolWebApp.Domain.Services.IStockageModelesMail? _stockage;
        private readonly Microsoft.Extensions.Logging.ILogger<ModeleMailRepository>? _logger;

        public ModeleMailRepository(
            SchoolWebAppDatabaseContext context,
            SchoolWebApp.Domain.Services.IStockageModelesMail? stockage = null,
            Microsoft.Extensions.Logging.ILogger<ModeleMailRepository>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _stockage = stockage;
            _logger = logger;
        }

        /// <summary>
        /// OÙ RANGER UNE PIÈCE (18/10/2026) : sur le disque dès que le dossier des
        /// templates est prêt ; en base sinon, ou si l'écriture échoue — on ne
        /// perd jamais une pièce.
        /// </summary>
        private async Task<(string? Fichier, byte[]? Donnees)> RangerAsync(
            byte[] donnees, string nomFichier, CancellationToken ct)
        {
            if (_stockage is not { Pret: true }) return (null, donnees);

            try
            {
                return (await _stockage.EcrireAsync(donnees, nomFichier, ct), null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(ex, "Piece de template {Nom} gardee en base : ecriture sur le disque impossible.", nomFichier);
                return (null, donnees);
            }
        }

        /// <summary>Les octets d'une pièce, où qu'ils soient.</summary>
        private async Task<byte[]> OctetsAsync(string? fichier, byte[]? donnees, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(fichier) && _stockage is not null)
            {
                return await _stockage.LireAsync(fichier, ct) ?? Array.Empty<byte>();
            }

            return donnees ?? Array.Empty<byte>();
        }

        public async Task<int> DeplacerPiecesVersDisqueAsync(int lot, CancellationToken ct = default)
        {
            if (_stockage is not { Pret: true }) return 0;

            var aDeplacer = await _context.PiecesModelesMail
                .AsNoTracking()
                .Where(p => p.Fichier == null && p.Donnees != null)
                .OrderBy(p => p.Id)
                .Select(p => new { p.Id, p.NomFichier })
                .Take(lot)
                .ToListAsync(ct);

            var deplacees = 0;
            foreach (var p in aDeplacer)
            {
                var octets = await _context.PiecesModelesMail.AsNoTracking()
                    .Where(x => x.Id == p.Id).Select(x => x.Donnees).FirstOrDefaultAsync(ct);
                if (octets is not { Length: > 0 }) continue;

                string fichier;
                try
                {
                    fichier = await _stockage.EcrireAsync(octets, p.NomFichier, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger?.LogError(ex, "Piece de template {Id} non deplacee : elle reste en base.", p.Id);
                    continue;
                }

                // Conditionnel : une pièce retirée entre-temps n'est pas touchée,
                // et le fichier écrit pour rien est effacé.
                var faites = await _context.PiecesModelesMail
                    .Where(x => x.Id == p.Id && x.Fichier == null && x.Donnees != null)
                    .ExecuteUpdateAsync(m => m
                        .SetProperty(x => x.Fichier, fichier)
                        .SetProperty(x => x.Donnees, (byte[]?)null), ct);

                if (faites == 0) _stockage.Supprimer(fichier);
                deplacees += faites;
            }

            // Plus rien à déplacer dans ce lot (tout a échoué) : on s'arrête, au
            // lieu de reboucler sur les mêmes pièces.
            return deplacees == 0 && aDeplacer.Count > 0 ? 0 : deplacees;
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
                Cible = m.Cible,
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
                Cible = m.Cible,
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
            int id, CancellationToken ct = default)
        {
            var lignes = await _context.PiecesModelesMail
                .AsNoTracking()
                .Where(p => p.ModeleMailId == id)
                .OrderBy(p => p.Genre)
                .ThenBy(p => p.Rang)
                .Select(p => new { p.Id, p.Genre, p.Rang, p.NomFichier, p.TypeMime, p.Taille, p.Fichier, p.Donnees })
                .ToListAsync(ct);

            var pieces = new List<PieceModeleMailContenu>();
            foreach (var p in lignes)
            {
                pieces.Add(new PieceModeleMailContenu
                {
                    Id = p.Id,
                    Genre = p.Genre,
                    Rang = p.Rang,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                    Donnees = await OctetsAsync(p.Fichier, p.Donnees, ct),
                });
            }

            return pieces;
        }

        public async Task<PieceModeleMailContenu?> GetPieceAsync(
            int id, int pieceId, CancellationToken ct = default)
        {
            var p = await _context.PiecesModelesMail
                .AsNoTracking()
                .Where(x => x.Id == pieceId && x.ModeleMailId == id)
                .Select(x => new { x.Id, x.Genre, x.Rang, x.NomFichier, x.TypeMime, x.Taille, x.Fichier, x.Donnees })
                .FirstOrDefaultAsync(ct);

            return p is null
                ? null
                : new PieceModeleMailContenu
                {
                    Id = p.Id,
                    Genre = p.Genre,
                    Rang = p.Rang,
                    NomFichier = p.NomFichier,
                    TypeMime = p.TypeMime,
                    Taille = p.Taille,
                    Donnees = await OctetsAsync(p.Fichier, p.Donnees, ct),
                };
        }

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
            await AjouterAsync(modele, GenrePieceMail.Image, images, maintenant, ct);
            await AjouterAsync(modele, GenrePieceMail.Document, documents, maintenant, ct);

            _context.ModelesMail.Add(modele);
            await _context.SaveChangesAsync(ct);

            return (await GetDetailAsync(modele.Id, ct))!;
        }

        private async Task AjouterAsync(
            ModeleMail modele, string genre, IReadOnlyList<NouvellePieceMail> pieces, DateTime date, CancellationToken ct)
        {
            for (var i = 0; i < pieces.Count; i++)
            {
                var (fichier, donnees) = await RangerAsync(pieces[i].Donnees, pieces[i].NomFichier, ct);
                modele.Pieces.Add(new PieceModeleMail
                {
                    Genre = genre,
                    Rang = i + 1,
                    NomFichier = pieces[i].NomFichier,
                    TypeMime = pieces[i].TypeMime,
                    Taille = pieces[i].Donnees.Length,
                    Fichier = fichier,
                    Donnees = donnees,
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

            var (fichier, donnees) = await RangerAsync(piece.Donnees, piece.NomFichier, ct);
            var ligne = new PieceModeleMail
            {
                ModeleMailId = id,
                Genre = genre,
                Rang = rang,
                NomFichier = piece.NomFichier,
                TypeMime = piece.TypeMime,
                Taille = piece.Donnees.Length,
                Fichier = fichier,
                Donnees = donnees,
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
                .Select(p => new { p.Genre, p.Rang, p.Fichier })
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

            // Le fichier part après la validation : une transaction annulée ne
            // laisse jamais une ligne pointer vers un fichier effacé.
            _stockage?.Supprimer(piece.Fichier);

            return await GetDetailAsync(id, ct);
        }

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            // Les codes protégés sont DANS la condition : un courriel relié à un
            // envoi ne peut pas disparaître par cette porte, même appelée par
            // erreur. Les pièces partent avec, par la cascade de la clé étrangère.
            var requete = _context.ModelesMail
                .Where(m => m.Id == id && (m.Code == null || !CodesProteges.Contains(m.Code)));

            // Leurs fichiers partent avec eux (les lignes suivent en cascade).
            var fichiers = await requete.SelectMany(m => m.Pieces.Where(p => p.Fichier != null).Select(p => p.Fichier!)).ToListAsync(ct);
            var supprimes = await requete.ExecuteDeleteAsync(ct);
            if (supprimes > 0) foreach (var f in fichiers) _stockage?.Supprimer(f);

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
            string? cible,
            CancellationToken ct = default)
        {
            var modifies = await _context.ModelesMail
                .Where(m => m.Id == id && m.Nature == NatureModeleMail.Automatique)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.Frequence, frequence)
                    .SetProperty(m => m.HeureEnvoi, heure)
                    .SetProperty(m => m.JourSemaine, jourSemaine)
                    .SetProperty(m => m.JourMois, jourMois)
                    .SetProperty(m => m.Cible, cible)
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
