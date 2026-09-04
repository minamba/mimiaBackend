using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class AvisRepository : IAvisRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public AvisRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// LA MOYENNE PORTE SUR TOUS LES AVIS PUBLIÉS, LA LISTE SUR LES DERNIERS.
        ///
        /// Deux requêtes plutôt qu'une, et c'est voulu : calculer la moyenne sur
        /// la page affichée la ferait changer à chaque « voir plus », et
        /// « 4,3 sur 5 » cesserait de vouloir dire quoi que ce soit.
        /// </summary>
        public async Task<AvisPublics> GetPublicsAsync(
            int limite, int decalage = 0, CancellationToken ct = default)
        {
            var publies = _context.AvisClients.AsNoTracking().Where(a => a.Publie);

            var notes = await publies
                .GroupBy(a => a.Note)
                .Select(g => new { Note = g.Key, Nombre = g.Count() })
                .ToListAsync(ct);

            var total = notes.Sum(n => n.Nombre);

            var repartition = new List<int> { 0, 0, 0, 0, 0 };

            foreach (var ligne in notes)
            {
                if (ligne.Note is >= 1 and <= 5) repartition[ligne.Note - 1] = ligne.Nombre;
            }

            // LE NOM DE FAMILLE NE VA PAS JUSQU'AU NAVIGATEUR. On ramène prénom
            // et nom séparément, puis on assemble la signature ICI. Les poser sur
            // le contrat public les enverrait en clair dans la réponse, où
            // n'importe qui les lirait sans même regarder la page.
            var brut = await publies
                .OrderByDescending(a => a.DateCreation)
                .ThenByDescending(a => a.Id)
                .Skip(decalage)
                .Take(limite)
                .Select(a => new
                {
                    a.Id,
                    a.Note,
                    a.Titre,
                    a.Commentaire,
                    a.DateCreation,
                    a.Parent!.Prenom,
                    a.Parent.Nom,

                    // Le « achat vérifié » des sites marchands : au moins un
                    // abonnement payant en cours. Lu à l'affichage et non figé à
                    // la rédaction — l'avis reste vrai, le statut de la famille
                    // évolue.
                    Abonne = a.Parent.Abonnements.Any(
                        ab => ab.Statut == "Actif" && !ab.Offre!.EstEssai),
                })
                .ToListAsync(ct);

            return new AvisPublics
            {
                Total = total,

                Moyenne = total == 0
                    ? 0
                    : Math.Round(notes.Sum(n => (double)n.Note * n.Nombre) / total, 1),

                Repartition = repartition,

                Avis = brut.Select(a => new AvisPublic
                {
                    Id = a.Id,
                    Note = a.Note,
                    Titre = a.Titre,
                    Commentaire = a.Commentaire,
                    Date = a.DateCreation,
                    Auteur = Signature(a.Prenom, a.Nom),
                    Abonne = a.Abonne,
                }).ToList(),
            };
        }

        /// <summary>
        /// « Marie C. » — le prénom, puis l'initiale du nom.
        ///
        /// Le nom entier n'apporte rien au lecteur et expose une famille sur une
        /// page publique, indexée par les moteurs. L'initiale suffit à donner un
        /// visage à l'avis.
        /// </summary>
        private static string Signature(string? prenom, string? nom)
        {
            var p = (prenom ?? string.Empty).Trim();
            var n = (nom ?? string.Empty).Trim();

            if (p.Length == 0) return "Un parent";

            return n.Length == 0 ? p : p + " " + char.ToUpperInvariant(n[0]) + ".";
        }

        public Task<MonAvis?> GetMonAvisAsync(int parentId, CancellationToken ct = default) =>
            _context.AvisClients
                .AsNoTracking()
                .Where(a => a.ParentId == parentId)
                .Select(a => new MonAvis
                {
                    Note = a.Note,
                    Titre = a.Titre,
                    Commentaire = a.Commentaire,
                    Date = a.DateModification ?? a.DateCreation,
                    Publie = a.Publie,
                })
                .FirstOrDefaultAsync(ct);

        public async Task<MonAvis> DeposerAsync(
            int parentId, int note, string? titre, string? commentaire, CancellationToken ct = default)
        {
            var avis = await _context.AvisClients.FirstOrDefaultAsync(a => a.ParentId == parentId, ct);
            var maintenant = DateTime.UtcNow;

            if (avis is null)
            {
                avis = new AvisClient { ParentId = parentId, DateCreation = maintenant };
                _context.AvisClients.Add(avis);
            }
            else
            {
                avis.DateModification = maintenant;
            }

            avis.Note = note;
            avis.Titre = Vide(titre);
            avis.Commentaire = Vide(commentaire);

            // REPASSE EN ATTENTE À CHAQUE ÉCRITURE, même si l'avis était déjà
            // publié. Sans cette ligne, il suffirait de faire valider une phrase
            // anodine, puis de la remplacer par ce qu'on veut.
            avis.Publie = false;

            await _context.SaveChangesAsync(ct);

            return new MonAvis
            {
                Note = avis.Note,
                Titre = avis.Titre,
                Commentaire = avis.Commentaire,
                Date = avis.DateModification ?? avis.DateCreation,
                Publie = avis.Publie,
            };
        }

        private static string? Vide(string? valeur) =>
            string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

        public async Task<bool> RetirerAsync(int parentId, CancellationToken ct = default)
        {
            var avis = await _context.AvisClients.FirstOrDefaultAsync(a => a.ParentId == parentId, ct);
            if (avis is null) return false;

            _context.AvisClients.Remove(avis);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// LES AVIS EN ATTENTE D'ABORD, et c'est tout l'intérêt de cet écran :
        /// ce sont les seuls sur lesquels il y a une décision à prendre.
        /// </summary>
        public async Task<IEnumerable<AvisAdmin>> GetPourRelectureAsync(CancellationToken ct = default)
        {
            var brut = await _context.AvisClients
                .AsNoTracking()
                .OrderBy(a => a.Publie)
                .ThenByDescending(a => a.DateModification ?? a.DateCreation)
                .Select(a => new
                {
                    a.Id,
                    a.Note,
                    a.Titre,
                    a.Commentaire,
                    a.DateCreation,
                    a.DateModification,
                    a.Publie,
                    a.Parent!.Mail,
                    a.Parent.Prenom,
                    a.Parent.Nom,
                    Abonne = a.Parent.Abonnements.Any(
                        ab => ab.Statut == "Actif" && !ab.Offre!.EstEssai),
                })
                .ToListAsync(ct);

            // Le courriel accompagne la signature en administration : c'est là
            // qu'on décide, et décider suppose de savoir qui parle.
            return brut.Select(a => new AvisAdmin
            {
                Id = a.Id,
                Note = a.Note,
                Titre = a.Titre,
                Commentaire = a.Commentaire,
                Date = a.DateCreation,
                DateModification = a.DateModification,
                Publie = a.Publie,
                Mail = a.Mail,
                Auteur = Signature(a.Prenom, a.Nom),
                Abonne = a.Abonne,
            }).ToList();
        }

        public async Task<bool> PublierAsync(int avisId, bool publie, CancellationToken ct = default)
        {
            var avis = await _context.AvisClients.FirstOrDefaultAsync(a => a.Id == avisId, ct);
            if (avis is null) return false;

            avis.Publie = publie;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SupprimerAsync(int avisId, CancellationToken ct = default)
        {
            var avis = await _context.AvisClients.FirstOrDefaultAsync(a => a.Id == avisId, ct);
            if (avis is null) return false;

            _context.AvisClients.Remove(avis);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
