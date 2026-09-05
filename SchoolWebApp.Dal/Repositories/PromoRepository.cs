using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    public class PromoRepository : IPromoRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;

        public PromoRepository(SchoolWebAppDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// LA PROJECTION N'EST PAS UNE COQUETTERIE.
        ///
        /// `Select` avant `ToListAsync` fait écrire à EF un SELECT qui NOMME
        /// ses colonnes, et donc qui laisse les deux colonnes d'images dans la
        /// base. Sans elle, EF matérialise l'entité entière : six lignes de
        /// bibliothèque tireraient une dizaine de mégaoctets pour dessiner un
        /// tableau de texte.
        ///
        /// Écrite une fois et partagée par les trois lectures, pour qu'aucune
        /// ne puisse l'oublier de son côté.
        /// </summary>
        private static readonly System.Linq.Expressions.Expression<Func<BandeauPromo, Promo>> SansOctets =
            b => new Promo
            {
                Id = b.Id,
                Titre = b.Titre,
                TexteAlternatif = b.TexteAlternatif,
                Lien = b.Lien,
                Actif = b.Actif,
                PleineLargeur = b.PleineLargeur,
                TypeMimeLarge = b.TypeMimeLarge,

                // `!= null` plutôt que `.Length > 0` : traduit en SQL, le
                // second lirait la colonne pour en mesurer la taille.
                AvecImageMobile = b.ImageMobile != null,

                TailleLarge = b.TailleLarge,
                TailleMobile = b.TailleMobile,
                DateCreation = b.DateCreation,
                DateModification = b.DateModification,
            };

        public Task<Promo?> GetActifAsync(CancellationToken ct = default) =>
            _context.BandeauxPromo
                .AsNoTracking()
                .Where(b => b.Actif)

                // ORDONNÉE MALGRÉ LA RÈGLE DU « UN SEUL ». La règle est tenue
                // par les écritures, mais une base modifiée à la main pourrait
                // en présenter deux ; un `First` sans ordre choisirait alors
                // au hasard, et la page d'accueil changerait de bandeau d'un
                // rafraîchissement à l'autre. Le plus récent gagne.
                .OrderByDescending(b => b.Id)
                .Select(SansOctets)
                .FirstOrDefaultAsync(ct);

        public async Task<IEnumerable<Promo>> GetTousAsync(CancellationToken ct = default) =>
            await _context.BandeauxPromo
                .AsNoTracking()

                // L'affiché en tête : c'est celui qu'on vient vérifier.
                .OrderByDescending(b => b.Actif)
                .ThenByDescending(b => b.Id)
                .Select(SansOctets)
                .ToListAsync(ct);

        public Task<Promo?> GetAsync(int id, CancellationToken ct = default) =>
            _context.BandeauxPromo
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(SansOctets)
                .FirstOrDefaultAsync(ct);

        /// <summary>
        /// LE REPLI SUR L'IMAGE LARGE EST ICI, ET NULLE PART AILLEURS.
        ///
        /// Une promo sans version téléphone doit rester visible sur un
        /// téléphone : petite, mal cadrée, mais visible. Placer cette règle
        /// dans l'écran obligerait chaque appelant à la connaître, et le
        /// premier qui l'oublie affiche un cadre vide à la moitié du trafic.
        ///
        /// Les deux colonnes d'octets ne sont JAMAIS chargées ensemble : la
        /// projection ne ramène que celle qu'on sert.
        /// </summary>
        public async Task<ImagePromo?> GetImageAsync(
            int id, bool mobile, CancellationToken ct = default)
        {
            var ligne = await _context.BandeauxPromo
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    Donnees = mobile && b.ImageMobile != null ? b.ImageMobile : b.ImageLarge,
                    TypeMime = mobile && b.ImageMobile != null ? b.TypeMimeMobile : b.TypeMimeLarge,
                    b.DateCreation,
                    b.DateModification,
                })
                .FirstOrDefaultAsync(ct);

            if (ligne?.Donnees is not { Length: > 0 }) return null;

            return new ImagePromo
            {
                Donnees = ligne.Donnees,
                TypeMime = string.IsNullOrWhiteSpace(ligne.TypeMime)
                    ? "application/octet-stream"
                    : ligne.TypeMime,
                Version = ligne.DateModification ?? ligne.DateCreation,
            };
        }

        public async Task<Promo> CreerAsync(
            string titre,
            string texteAlternatif,
            string? lien,
            byte[] imageLarge,
            string typeMimeLarge,
            byte[]? imageMobile,
            string? typeMimeMobile,
            bool pleineLargeur,
            CancellationToken ct = default)
        {
            var ligne = new BandeauPromo
            {
                Titre = titre,
                TexteAlternatif = texteAlternatif,
                Lien = lien,
                PleineLargeur = pleineLargeur,

                // ÉTEINT À LA CRÉATION : on regarde le visuel dans la
                // bibliothèque avant de le mettre sur la page d'accueil.
                Actif = false,

                ImageLarge = imageLarge,
                TypeMimeLarge = typeMimeLarge,
                TailleLarge = imageLarge.Length,

                ImageMobile = imageMobile,
                TypeMimeMobile = typeMimeMobile,
                TailleMobile = imageMobile?.Length ?? 0,

                DateCreation = DateTime.UtcNow,
            };

            _context.BandeauxPromo.Add(ligne);
            await _context.SaveChangesAsync(ct);

            return Projeter(ligne);
        }

        public async Task<Promo?> ModifierAsync(
            int id,
            string titre,
            string texteAlternatif,
            string? lien,
            byte[]? imageLarge,
            string? typeMimeLarge,
            byte[]? imageMobile,
            string? typeMimeMobile,
            bool pleineLargeur,
            CancellationToken ct = default)
        {
            var ligne = await _context.BandeauxPromo.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (ligne is null) return null;

            ligne.Titre = titre;
            ligne.TexteAlternatif = texteAlternatif;
            ligne.Lien = lien;
            ligne.PleineLargeur = pleineLargeur;

            // UNE IMAGE ABSENTE VEUT DIRE « NE TOUCHE PAS », jamais « efface ».
            // Corriger une faute dans le texte alternatif ne doit pas obliger
            // à retéléverser deux fichiers.
            if (imageLarge is { Length: > 0 })
            {
                ligne.ImageLarge = imageLarge;
                ligne.TypeMimeLarge = typeMimeLarge ?? ligne.TypeMimeLarge;
                ligne.TailleLarge = imageLarge.Length;
            }

            if (imageMobile is { Length: > 0 })
            {
                ligne.ImageMobile = imageMobile;
                ligne.TypeMimeMobile = typeMimeMobile ?? ligne.TypeMimeMobile;
                ligne.TailleMobile = imageMobile.Length;
            }

            // La date de modification est l'étiquette de cache des images :
            // sans elle, un visuel remplacé resterait l'ancien dans tous les
            // navigateurs qui l'avaient déjà vu.
            ligne.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return Projeter(ligne);
        }

        /// <summary>
        /// UN SEUL BANDEAU AFFICHÉ, GARANTI ICI.
        ///
        /// L'extinction des autres et l'allumage de celui-ci partent dans le
        /// MÊME `SaveChangesAsync`, donc dans la même transaction. Deux appels
        /// séparés laisseraient une fenêtre à deux bandeaux allumés — courte,
        /// mais un plantage entre les deux la rendrait permanente, et la page
        /// d'accueil afficherait deux bandes d'images superposées.
        /// </summary>
        public async Task<bool> DefinirAffichageAsync(
            int id, bool actif, CancellationToken ct = default)
        {
            var ligne = await _context.BandeauxPromo.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (ligne is null) return false;

            if (actif)
            {
                // `Where(b => b.Actif)` s'appuie sur l'index : on ne charge pas
                // toute la bibliothèque pour éteindre celui qui est allumé.
                var autres = await _context.BandeauxPromo
                    .Where(b => b.Actif && b.Id != id)
                    .ToListAsync(ct);

                foreach (var autre in autres) autre.Actif = false;
            }

            ligne.Actif = actif;

            // PAS DE `DateModification` ICI, et c'est délibéré : allumer un
            // bandeau ne change pas ses octets. La toucher invaliderait le
            // cache des images de tous les visiteurs sans qu'aucun pixel ait
            // bougé.
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> SupprimerAsync(int id, CancellationToken ct = default)
        {
            var ligne = await _context.BandeauxPromo.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (ligne is null) return false;

            _context.BandeauxPromo.Remove(ligne);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        /// La même projection, appliquée à une entité déjà en mémoire.
        ///
        /// Après une écriture, la ligne est là : la reprojeter par une requête
        /// serait un aller-retour de plus pour des valeurs qu'on tient déjà.
        /// </summary>
        private static Promo Projeter(BandeauPromo b) => new()
        {
            Id = b.Id,
            Titre = b.Titre,
            TexteAlternatif = b.TexteAlternatif,
            Lien = b.Lien,
            Actif = b.Actif,
            PleineLargeur = b.PleineLargeur,
            TypeMimeLarge = b.TypeMimeLarge,
            AvecImageMobile = b.ImageMobile is { Length: > 0 },
            TailleLarge = b.TailleLarge,
            TailleMobile = b.TailleMobile,
            DateCreation = b.DateCreation,
            DateModification = b.DateModification,
        };
    }
}
