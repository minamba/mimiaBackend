using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Emails;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class MessagerieAdminRepository : IMessagerieAdminRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;
        private readonly SchoolWebApp.Domain.Services.IStockageBrouillonsMail? _stockage;

        public MessagerieAdminRepository(
            SchoolWebAppDatabaseContext context,
            SchoolWebApp.Domain.Services.IStockageBrouillonsMail? stockage = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _stockage = stockage;
        }

        // ------------------------------------------------------------ boîtes
        public async Task<(string Adresse, string MotDePasseChiffre)?> BoiteAsync(int parentId, CancellationToken ct = default)
        {
            var b = await _context.BoitesMailAdmins.AsNoTracking()
                .Where(x => x.ParentId == parentId)
                .Select(x => new { x.Adresse, x.MotDePasseChiffre })
                .FirstOrDefaultAsync(ct);
            return b is null ? null : (b.Adresse, b.MotDePasseChiffre);
        }

        public async Task<IReadOnlyList<(int ParentId, string Adresse, string MotDePasseChiffre)>> BoitesAsync(CancellationToken ct = default) =>
            (await _context.BoitesMailAdmins.AsNoTracking()
                .Select(x => new { x.ParentId, x.Adresse, x.MotDePasseChiffre })
                .ToListAsync(ct))
            .Select(x => (x.ParentId, x.Adresse, x.MotDePasseChiffre))
            .ToList();

        public async Task EnregistrerBoiteAsync(int parentId, string adresse, string motDePasseChiffre, CancellationToken ct = default)
        {
            var maintenant = DateTime.UtcNow;
            var b = await _context.BoitesMailAdmins.FirstOrDefaultAsync(x => x.ParentId == parentId, ct);
            if (b is null)
            {
                _context.BoitesMailAdmins.Add(new BoiteMailAdmin
                {
                    ParentId = parentId,
                    Adresse = adresse,
                    MotDePasseChiffre = motDePasseChiffre,
                    DateCreation = maintenant,
                    DateModification = maintenant,
                });
            }
            else
            {
                b.Adresse = adresse;
                b.MotDePasseChiffre = motDePasseChiffre;
                b.DateModification = maintenant;
            }

            await _context.SaveChangesAsync(ct);
        }

        public Task SupprimerBoiteAsync(int parentId, CancellationToken ct = default) =>
            _context.BoitesMailAdmins.Where(x => x.ParentId == parentId).ExecuteDeleteAsync(ct);

        // -------------------------------------------------------- signatures
        public async Task<IReadOnlyList<ModeleSignature>> SignaturesAsync(CancellationToken ct = default)
        {
            var modeles = await _context.SignaturesMail.AsNoTracking()
                .OrderByDescending(s => s.ParDefaut).ThenBy(s => s.Nom).ToListAsync(ct);

            var comptes = await _context.Parents.AsNoTracking()
                .Where(p => p.SignatureMailId != null)
                .GroupBy(p => p.SignatureMailId)
                .Select(g => new { Id = g.Key, Nombre = g.Count() })
                .ToDictionaryAsync(x => x.Id!.Value, x => x.Nombre, ct);

            return modeles.Select(m => Projeter(m, comptes.GetValueOrDefault(m.Id))).ToList();
        }

        public async Task<ModeleSignature?> SignatureDuParentAsync(int parentId, CancellationToken ct = default)
        {
            var attribuee = await _context.Parents.AsNoTracking()
                .Where(p => p.Id == parentId && p.SignatureMail != null)
                .Select(p => p.SignatureMail)
                .FirstOrDefaultAsync(ct);

            var modele = attribuee
                ?? await _context.SignaturesMail.AsNoTracking().OrderByDescending(s => s.ParDefaut).ThenBy(s => s.Id).FirstOrDefaultAsync(ct);

            return modele is null ? null : Projeter(modele, 0);
        }

        public async Task<(ModeleSignature? Modele, string? Erreur)> EnregistrerSignatureAsync(
            int? id, SaisieSignature saisie, CancellationToken ct = default)
        {
            var nom = saisie.Nom?.Trim();
            if (string.IsNullOrEmpty(nom) || nom.Length > 60) return (null, "Donnez un nom au modèle (60 caractères au plus).");
            if (await _context.SignaturesMail.AnyAsync(s => s.Nom == nom && (id == null || s.Id != id), ct))
            {
                return (null, $"Un modèle « {nom} » existe déjà.");
            }

            SignatureMail? modele;
            if (id is int existant)
            {
                modele = await _context.SignaturesMail.FirstOrDefaultAsync(s => s.Id == existant, ct);
                if (modele is null) return (null, null);
            }
            else
            {
                modele = new SignatureMail { DateCreation = DateTime.UtcNow };
                _context.SignaturesMail.Add(modele);
            }

            modele.Nom = nom;
            modele.Formule = Court(saisie.Formule, 80);
            modele.LigneLibre = Court(saisie.LigneLibre, 160);
            modele.AfficherRole = saisie.AfficherRole;
            modele.AfficherAdresse = saisie.AfficherAdresse;
            modele.Telephone = Court(saisie.Telephone, 30);
            modele.AfficherSite = saisie.AfficherSite;
            modele.Couleur = SignatureRendu.CouleurSure(saisie.Couleur);

            // UN SEUL MODÈLE PAR DÉFAUT : en cocher un décoche l'autre. Décocher le
            // seul ne se fait pas — il en faut toujours un pour qui n'a rien reçu.
            var autresParDefaut = await _context.SignaturesMail.AnyAsync(s => s.ParDefaut && s.Id != modele.Id, ct);
            if (saisie.ParDefaut)
            {
                await _context.SignaturesMail.Where(s => s.ParDefaut && s.Id != modele.Id)
                    .ExecuteUpdateAsync(m => m.SetProperty(s => s.ParDefaut, false), ct);
                modele.ParDefaut = true;
            }
            else
            {
                modele.ParDefaut = !autresParDefaut;
            }

            await _context.SaveChangesAsync(ct);
            return (Projeter(modele, await _context.Parents.CountAsync(p => p.SignatureMailId == modele.Id, ct)), null);
        }

        public async Task<(bool Fait, string? Erreur)> SupprimerSignatureAsync(int id, CancellationToken ct = default)
        {
            var modele = await _context.SignaturesMail.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (modele is null) return (false, null);
            if (await _context.SignaturesMail.CountAsync(ct) <= 1) return (false, "Il faut garder au moins un modèle de signature.");

            await _context.Parents.Where(p => p.SignatureMailId == id)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.SignatureMailId, (int?)null), ct);
            _context.SignaturesMail.Remove(modele);
            await _context.SaveChangesAsync(ct);

            // Le modèle par défaut supprimé : le plus ancien prend la relève.
            if (modele.ParDefaut)
            {
                var suivant = await _context.SignaturesMail.OrderBy(s => s.Id).FirstAsync(ct);
                suivant.ParDefaut = true;
                await _context.SaveChangesAsync(ct);
            }

            return (true, null);
        }

        public async Task<IReadOnlyList<SignataireAdmin>> SignatairesAsync(IEnumerable<string> autresMails, CancellationToken ct = default)
        {
            var mails = autresMails.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).ToList();

            return await _context.Parents.AsNoTracking()
                .Where(p => p.EstAdministrateur || (p.Mail != null && mails.Contains(p.Mail)))
                .OrderBy(p => p.Prenom).ThenBy(p => p.Nom)
                .Select(p => new SignataireAdmin(
                    p.Id, p.Prenom, p.Nom, p.Mail,
                    p.Role != null ? p.Role.Nom : null,
                    p.SignatureMailId,
                    _context.BoitesMailAdmins.Where(b => b.ParentId == p.Id).Select(b => b.Adresse).FirstOrDefault()))
                .ToListAsync(ct);
        }

        public async Task<SignataireAdmin?> SignataireAsync(string? mail, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mail)) return null;
            var propre = mail.Trim();

            return await _context.Parents.AsNoTracking()
                .Where(p => p.Mail == propre)
                .Select(p => new SignataireAdmin(
                    p.Id, p.Prenom, p.Nom, p.Mail,
                    p.Role != null ? p.Role.Nom : null,
                    p.SignatureMailId,
                    _context.BoitesMailAdmins.Where(b => b.ParentId == p.Id).Select(b => b.Adresse).FirstOrDefault()))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> AttribuerSignatureAsync(int parentId, int? signatureId, CancellationToken ct = default)
        {
            if (signatureId is int id && !await _context.SignaturesMail.AnyAsync(s => s.Id == id, ct)) return false;
            return await _context.Parents.Where(p => p.Id == parentId)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.SignatureMailId, signatureId), ct) > 0;
        }

        // -------------------------------------------------------- brouillons
        public async Task<(IReadOnlyList<BrouillonMessagerie> Brouillons, int Total)> BrouillonsAsync(
            int parentId, string boite, int page, int taille, CancellationToken ct = default)
        {
            var requete = _context.BrouillonsMail.AsNoTracking()
                .Where(b => b.ParentId == parentId && b.Boite == boite);

            var total = await requete.CountAsync(ct);
            var liste = await requete
                .OrderByDescending(b => b.DateModification).ThenByDescending(b => b.Id)
                .Skip((Math.Max(1, page) - 1) * taille).Take(taille)
                .Select(b => new BrouillonMessagerie(b.Id, b.MessageOrigine, b.Destinataire, b.Sujet, b.Texte, b.DateModification, b.Titre, b.Copies, b.CopiesCachees))
                .ToListAsync(ct);

            return (liste, total);
        }

        public Task<BrouillonMessagerie?> BrouillonPourAsync(int parentId, string boite, long messageOrigine, CancellationToken ct = default) =>
            _context.BrouillonsMail.AsNoTracking()
                .Where(b => b.ParentId == parentId && b.Boite == boite && b.MessageOrigine == messageOrigine)
                .Select(b => new BrouillonMessagerie(b.Id, b.MessageOrigine, b.Destinataire, b.Sujet, b.Texte, b.DateModification, b.Titre, b.Copies, b.CopiesCachees))
                .FirstOrDefaultAsync(ct);

        public async Task<IReadOnlySet<long>> MessagesAvecBrouillonAsync(
            int parentId, string boite, IEnumerable<long> messages, CancellationToken ct = default)
        {
            var ids = messages.Distinct().ToList();
            if (ids.Count == 0) return new HashSet<long>();

            var trouves = await _context.BrouillonsMail.AsNoTracking()
                .Where(b => b.ParentId == parentId && b.Boite == boite
                    && b.MessageOrigine != null && ids.Contains(b.MessageOrigine.Value))
                .Select(b => b.MessageOrigine!.Value)
                .ToListAsync(ct);

            return trouves.ToHashSet();
        }

        public async Task<BrouillonMessagerie> EnregistrerBrouillonAsync(
            int parentId, string boite, long messageOrigine, string? destinataire, string? sujet, string texte,
            CancellationToken ct = default, string? copies = null, string? copiesCachees = null)
        {
            var b = await _context.BrouillonsMail
                .FirstOrDefaultAsync(x => x.ParentId == parentId && x.Boite == boite && x.MessageOrigine == messageOrigine, ct);

            if (b is null)
            {
                b = new BrouillonMail { ParentId = parentId, Boite = boite, MessageOrigine = messageOrigine };
                _context.BrouillonsMail.Add(b);
            }

            b.Destinataire = Court(destinataire, 255);
            b.Copies = Court(copies, 2000);
            b.CopiesCachees = Court(copiesCachees, 2000);
            b.Sujet = Court(sujet, 500);
            b.Texte = texte;
            b.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return new BrouillonMessagerie(b.Id, b.MessageOrigine, b.Destinataire, b.Sujet, b.Texte, b.DateModification, b.Titre, b.Copies, b.CopiesCachees);
        }

        public Task<BrouillonMessagerie?> BrouillonAsync(int parentId, string boite, int id, CancellationToken ct = default) =>
            _context.BrouillonsMail.AsNoTracking()
                .Where(b => b.Id == id && b.ParentId == parentId && b.Boite == boite)
                .Select(b => new BrouillonMessagerie(b.Id, b.MessageOrigine, b.Destinataire, b.Sujet, b.Texte, b.DateModification, b.Titre, b.Copies, b.CopiesCachees))
                .FirstOrDefaultAsync(ct);

        public async Task<BrouillonMessagerie> EnregistrerBrouillonLibreAsync(
            int parentId, string boite, int? id, string? destinataire, string? sujet, string? titre, string texte,
            CancellationToken ct = default, string? copies = null, string? copiesCachees = null)
        {
            // Le sien, sur cette boîte, et sans message d'origine : un identifiant
            // d'un autre brouillon ne s'écrase jamais, on en crée un nouveau.
            var b = id is int existant
                ? await _context.BrouillonsMail.FirstOrDefaultAsync(x => x.Id == existant
                    && x.ParentId == parentId && x.Boite == boite && x.MessageOrigine == null, ct)
                : null;

            if (b is null)
            {
                b = new BrouillonMail { ParentId = parentId, Boite = boite };
                _context.BrouillonsMail.Add(b);
            }

            b.Destinataire = Court(destinataire, 255);
            b.Copies = Court(copies, 2000);
            b.CopiesCachees = Court(copiesCachees, 2000);
            b.Sujet = Court(sujet, 500);
            b.Titre = Court(titre, 150);
            b.Texte = texte;
            b.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return new BrouillonMessagerie(b.Id, b.MessageOrigine, b.Destinataire, b.Sujet, b.Texte, b.DateModification, b.Titre, b.Copies, b.CopiesCachees);
        }

        public async Task<int> SupprimerBrouillonsAsync(int parentId, string boite, IEnumerable<int> ids, CancellationToken ct = default)
        {
            var liste = ids.Distinct().ToList();
            var requete = _context.BrouillonsMail
                .Where(b => b.ParentId == parentId && b.Boite == boite && liste.Contains(b.Id));

            // Leurs fichiers partent avec eux (les lignes, elles, suivent en cascade).
            var chemins = await requete.SelectMany(b => b.Pieces.Select(p => p.Chemin)).ToListAsync(ct);
            var supprimes = await requete.ExecuteDeleteAsync(ct);
            foreach (var chemin in chemins) _stockage?.Supprimer(chemin);
            return supprimes;
        }

        public Task SupprimerBrouillonPourAsync(int parentId, string boite, long messageOrigine, CancellationToken ct = default) =>
            _context.BrouillonsMail
                .Where(b => b.ParentId == parentId && b.Boite == boite && b.MessageOrigine == messageOrigine)
                .ExecuteDeleteAsync(ct);

        public async Task<IReadOnlyList<PieceBrouillon>?> PiecesBrouillonAsync(
            int parentId, string boite, int brouillonId, CancellationToken ct = default)
        {
            var existe = await _context.BrouillonsMail.AsNoTracking()
                .AnyAsync(b => b.Id == brouillonId && b.ParentId == parentId && b.Boite == boite, ct);
            if (!existe) return null;

            return await _context.PiecesBrouillonsMail.AsNoTracking()
                .Where(p => p.BrouillonId == brouillonId)
                .OrderBy(p => p.Genre).ThenBy(p => p.Rang)
                .Select(p => new PieceBrouillon(p.Id, p.Genre, p.Rang, p.NomFichier, p.TypeMime, p.Taille, p.Chemin))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<string>?> RemplacerPiecesBrouillonAsync(
            int parentId, string boite, int brouillonId, IReadOnlyList<NouvellePieceBrouillon> pieces, CancellationToken ct = default)
        {
            var brouillon = await _context.BrouillonsMail
                .Include(b => b.Pieces)
                .FirstOrDefaultAsync(b => b.Id == brouillonId && b.ParentId == parentId && b.Boite == boite, ct);
            if (brouillon is null) return null;

            var anciens = brouillon.Pieces.Select(p => p.Chemin).ToList();
            _context.PiecesBrouillonsMail.RemoveRange(brouillon.Pieces);
            foreach (var p in pieces)
            {
                brouillon.Pieces.Add(new PieceBrouillonMail
                {
                    Genre = p.Genre,
                    Rang = p.Rang,
                    NomFichier = Court(p.NomFichier, 255) ?? "piece",
                    TypeMime = Court(p.TypeMime, 100) ?? "application/octet-stream",
                    Taille = p.Taille,
                    Chemin = p.Chemin,
                });
            }

            brouillon.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return anciens;
        }

        // ------------------------------------------------------------------
        private static string? Court(string? s, int max)
        {
            var t = s?.Trim();
            return string.IsNullOrEmpty(t) ? null : t.Length > max ? t[..max] : t;
        }

        private static ModeleSignature Projeter(SignatureMail m, int nombre) => new(
            m.Id, m.Nom, m.Formule, m.LigneLibre, m.AfficherRole, m.AfficherAdresse, m.AfficherSite, m.Couleur, m.ParDefaut, nombre, m.Telephone);
    }
}
