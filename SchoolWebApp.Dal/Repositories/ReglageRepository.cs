using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Dal.Repositories
{
    public class ReglageRepository : IReglageRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;
        private readonly IDiffusionReglages? _diffusion;

        /// <summary>
        /// LA DIFFUSION EST FACULTATIVE, ET C'EST VOULU. Elle est fournie par
        /// l'API, qui tient la liste des navigateurs à l'écoute ; un semeur, un
        /// worker ou un test qui construirait ce dépôt sans elle écrit
        /// normalement — les navigateurs relisent alors à leur rythme habituel,
        /// sans que rien ne casse.
        /// </summary>
        public ReglageRepository(
            SchoolWebAppDatabaseContext context, IDiffusionReglages? diffusion = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _diffusion = diffusion;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetTousAsync(
            CancellationToken ct = default) =>
            await _context.Reglages
                .AsNoTracking()
                .ToDictionaryAsync(r => r.Cle, r => r.Valeur, ct);

        public async Task<bool> EstActifAsync(
            string cle, bool parDefaut = false, CancellationToken ct = default)
        {
            var valeur = await _context.Reglages
                .AsNoTracking()
                .Where(r => r.Cle == cle)
                .Select(r => r.Valeur)
                .FirstOrDefaultAsync(ct);

            // Jamais posé : c'est l'appelant qui sait ce que « absent » veut
            // dire pour lui. Rien ne dit qu'un drapeau absent vaut « éteint ».
            return valeur is null
                ? parDefaut
                : string.Equals(valeur, "true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task DefinirAsync(string cle, bool actif, CancellationToken ct = default)
        {
            var reglage = await _context.Reglages.FirstOrDefaultAsync(r => r.Cle == cle, ct);

            if (reglage is null)
            {
                reglage = new Reglage { Cle = cle };
                _context.Reglages.Add(reglage);
            }

            reglage.Valeur = actif ? "true" : "false";
            reglage.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            // ANNONCÉ ICI, ET NON DANS LES ROUTES — Camara, le 16/09/2026 :
            // « il faut que ce soit instantané ». Toute écriture passe par ce
            // dépôt ; annoncer route par route reviendrait à en oublier une le
            // jour où l'on en ajoute. APRÈS l'enregistrement : un navigateur
            // qui relit doit trouver la nouvelle valeur, pas l'ancienne.
            _diffusion?.Diffuser(cle);
        }

        public async Task<string?> LireAsync(string cle, CancellationToken ct = default) =>
            (await _context.Reglages
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Cle == cle, ct))?.Valeur;

        public async Task EcrireAsync(string cle, string valeur, CancellationToken ct = default)
        {
            var reglage = await _context.Reglages.FirstOrDefaultAsync(r => r.Cle == cle, ct);

            if (reglage is null)
            {
                reglage = new Reglage { Cle = cle };
                _context.Reglages.Add(reglage);
            }

            reglage.Valeur = valeur;
            reglage.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            // Même chose pour les valeurs en clair — le texte du bandeau,
            // l'offre de lancement : elles s'affichent chez le visiteur au
            // même titre qu'un interrupteur.
            _diffusion?.Diffuser(cle);
        }
    }
}
