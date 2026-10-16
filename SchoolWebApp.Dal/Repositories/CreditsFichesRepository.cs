using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>Le quota de fiches et les crédits de la famille — voir `ICreditsFichesRepository`.</summary>
    public class CreditsFichesRepository : ICreditsFichesRepository
    {
        private readonly SchoolWebAppDatabaseContext _context;
        private readonly ILogger<CreditsFichesRepository> _logger;

        public CreditsFichesRepository(SchoolWebAppDatabaseContext context, ILogger<CreditsFichesRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string Code(ImputationFiche imputation) => imputation switch
        {
            ImputationFiche.Credit => "credit",
            ImputationFiche.Depassement => "depassement",
            _ => "mois",
        };

        public async Task<int?> ParentDeLEleveAsync(int eleveId, CancellationToken ct = default) =>
            await _context.Eleves
                .AsNoTracking()
                .Where(e => e.Id == eleveId)
                .Select(e => (int?)e.ParentId)
                .FirstOrDefaultAsync(ct);

        public Task<int> CompterDuMoisAsync(int eleveId, DateTime depuis, CancellationToken ct = default)
        {
            var mois = Code(ImputationFiche.Mois);
            return _context.FichesConsommees
                .CountAsync(f => f.EleveId == eleveId && f.DateCreation >= depuis && f.Imputation == mois, ct);
        }

        public Task<int> CompterCreditsPrisAsync(int eleveId, DateTime depuis, CancellationToken ct = default)
        {
            var credit = Code(ImputationFiche.Credit);
            return _context.FichesConsommees
                .CountAsync(f => f.EleveId == eleveId && f.DateCreation >= depuis && f.Imputation == credit, ct);
        }

        public async Task<int> SoldeAsync(int parentId, CancellationToken ct = default) =>
            await _context.CreditsFichesFamilles
                .AsNoTracking()
                .Where(c => c.ParentId == parentId)
                .Select(c => c.Solde)
                .FirstOrDefaultAsync(ct);

        private static readonly string[] EtatsEnRoute =
            [FichesRevisionEleve.EtatALire, FichesRevisionEleve.EtatNotions, FichesRevisionEleve.EtatAGenerer];

        // En POIDS, pas en nombre : un gros cours en route réserve deux places.
        public Task<int> CompterEnCoursAsync(int eleveId, CancellationToken ct = default) =>
            _context.CoursImportes
                .Where(c => c.EleveId == eleveId && EtatsEnRoute.Contains(c.Etat))
                .SumAsync(c => c.Poids, ct);

        public async Task<IReadOnlyList<EnfantFiches>> EnfantsAsync(
            int parentId, DateTime depuis, CancellationToken ct = default)
        {
            var mois = Code(ImputationFiche.Mois);

            // Tous les enfants du compte, comme l'encart des heures : un enfant
            // absent de la liste se lirait comme un enfant supprimé.
            return await _context.Eleves
                .AsNoTracking()
                .Where(e => e.ParentId == parentId)
                .OrderBy(e => e.Prenom)
                .Select(e => new EnfantFiches(
                    e.Id,
                    e.Prenom,
                    _context.FichesConsommees.Count(f => f.EleveId == e.Id && f.DateCreation >= depuis && f.Imputation == mois),
                    _context.CoursImportes.Where(c => c.EleveId == e.Id && EtatsEnRoute.Contains(c.Etat)).Sum(c => (int?)c.Poids) ?? 0))
                .ToListAsync(ct);
        }

        public async Task<ImputationFiche> ImputerAsync(
            int eleveId, int parentId, int? ficheId, DateTime depuis, int maximum, DateTime maintenant,
            CancellationToken ct = default)
        {
            // Les fiches d'un même enfant passent une à une (une seule tâche de
            // fond) : le compte du mois ne peut pas être doublé par lui-même.
            var imputation = ImputationFiche.Mois;

            if (await CompterDuMoisAsync(eleveId, depuis, ct) >= maximum)
            {
                // Le crédit, lui, est partagé par les enfants de la famille :
                // on le retire en UNE instruction conditionnelle, que deux
                // enfants au même instant ne peuvent pas passer tous les deux.
                var pris = await _context.CreditsFichesFamilles
                    .Where(c => c.ParentId == parentId && c.Solde > 0)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.Solde, c => c.Solde - 1)
                        .SetProperty(c => c.DateModification, maintenant), ct);

                imputation = pris > 0 ? ImputationFiche.Credit : ImputationFiche.Depassement;
            }

            if (imputation == ImputationFiche.Depassement)
            {
                _logger.LogWarning(
                    "Fiche {FicheId} de l'eleve {EleveId} faite sans quota ni credit (dernier credit pris en meme temps).",
                    ficheId, eleveId);
            }

            _context.FichesConsommees.Add(new FicheConsommee
            {
                EleveId = eleveId,
                FicheId = ficheId,
                Imputation = Code(imputation),
                DateCreation = maintenant,
            });

            await _context.SaveChangesAsync(ct);
            return imputation;
        }

        public async Task<bool> DebiterFamilleAsync(int parentId, DateTime maintenant, CancellationToken ct = default)
        {
            // Même instruction conditionnelle que pour un enfant : deux cours qui
            // finissent au même instant ne prennent pas tous deux le dernier crédit.
            var pris = await _context.CreditsFichesFamilles
                .Where(c => c.ParentId == parentId && c.Solde > 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Solde, c => c.Solde - 1)
                    .SetProperty(c => c.DateModification, maintenant), ct);

            if (pris == 0)
            {
                _logger.LogWarning("Cours de l'espace parent {ParentId} fait sans credit (dernier credit pris en meme temps).", parentId);
            }

            return pris > 0;
        }

        public async Task<IReadOnlyList<OffreFichesTarif>> GetOffresAsync(CancellationToken ct = default) =>
            await _context.OffresFiches
                .AsNoTracking()
                .Where(o => o.Active)
                .OrderBy(o => o.Ordre)
                .Select(o => new OffreFichesTarif
                {
                    Id = o.Id,
                    Code = o.Code,
                    Libelle = o.Libelle,
                    Fiches = o.Fiches,
                    PrixCentimes = o.PrixCentimes,
                    StripePrixId = o.StripePrixId,
                })
                .ToListAsync(ct);

        public async Task EnregistrerPrixStripeAsync(string code, string prixId, CancellationToken ct = default)
        {
            var offre = await _context.OffresFiches.FirstOrDefaultAsync(o => o.Code == code, ct);
            if (offre is null) return;

            offre.StripePrixId = prixId;
            await _context.SaveChangesAsync(ct);
        }

        public async Task<int?> CrediterAsync(
            int parentId, string codePack, string? sessionStripeId, string? paiementStripeId,
            CancellationToken ct = default)
        {
            var pack = await _context.OffresFiches
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Code == codePack && o.Active, ct);

            if (pack is null)
            {
                _logger.LogWarning("Pack de fiches {Pack} inconnu.", codePack);
                return null;
            }

            if (sessionStripeId is not null
                && await _context.AchatsFiches.AnyAsync(a => a.StripeSessionId == sessionStripeId, ct))
            {
                _logger.LogInformation("Session {Session} deja creditee en fiches.", sessionStripeId);
                return null;
            }

            var maintenant = DateTime.UtcNow;

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                _context.AchatsFiches.Add(new AchatFiches
                {
                    ParentId = parentId,
                    Code = pack.Code,
                    Fiches = pack.Fiches,
                    PrixCentimes = sessionStripeId is null ? 0 : pack.PrixCentimes,
                    DateAchat = maintenant,
                    StripeSessionId = sessionStripeId,
                    StripePaiementId = paiementStripeId,
                });

                await _context.SaveChangesAsync(ct);

                // LE SOLDE SE TOUCHE EN SQL, JAMAIS PAR UNE ENTITÉ CHARGÉE : la
                // prise d'un crédit (`ImputerAsync`) le fait déjà ainsi, et une
                // copie chargée plus tôt dans le même contexte serait périmée.
                var ajoutes = await _context.CreditsFichesFamilles
                    .Where(c => c.ParentId == parentId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.Solde, c => c.Solde + pack.Fiches)
                        .SetProperty(c => c.DateModification, maintenant), ct);

                if (ajoutes == 0)
                {
                    _context.CreditsFichesFamilles.Add(new CreditFichesFamille
                    {
                        ParentId = parentId,
                        Solde = pack.Fiches,
                        DateModification = maintenant,
                    });
                    await _context.SaveChangesAsync(ct);
                }

                await transaction.CommitAsync(ct);
                return await SoldeAsync(parentId, ct);
            }
            catch (DbUpdateException ex)
            {
                // L'index unique a gagné une course entre deux webhooks rejoués.
                _logger.LogWarning(ex, "Credit de fiches refuse par la base (session {Session}).", sessionStripeId);
                await transaction.RollbackAsync(ct);
                return null;
            }
        }

        public async Task<IReadOnlyList<LigneCreditsFiches>> HistoriqueAsync(int parentId, CancellationToken ct = default) =>
            await _context.AchatsFiches
                .AsNoTracking()
                .Where(a => a.ParentId == parentId)
                .OrderByDescending(a => a.DateAchat)
                .Select(a => new LigneCreditsFiches(a.DateAchat, a.Code, a.Fiches, a.PrixCentimes, a.Motif, a.DateRemboursement))
                .ToListAsync(ct);

        public async Task<int?> AjusterAsync(int parentId, int credits, string motif, CancellationToken ct = default)
        {
            if (credits == 0) return await SoldeAsync(parentId, ct);

            var maintenant = DateTime.UtcNow;
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            if (credits < 0)
            {
                // UN RETRAIT NE PASSE QUE S'IL TIENT, d'un bloc en SQL : un enfant
                // peut prendre un crédit au même instant.
                var retires = await _context.CreditsFichesFamilles
                    .Where(c => c.ParentId == parentId && c.Solde >= -credits)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.Solde, c => c.Solde + credits)
                        .SetProperty(c => c.DateModification, maintenant), ct);

                if (retires == 0)
                {
                    await transaction.RollbackAsync(ct);
                    return null;
                }
            }
            else
            {
                var ajoutes = await _context.CreditsFichesFamilles
                    .Where(c => c.ParentId == parentId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.Solde, c => c.Solde + credits)
                        .SetProperty(c => c.DateModification, maintenant), ct);

                if (ajoutes == 0)
                {
                    _context.CreditsFichesFamilles.Add(new CreditFichesFamille
                    {
                        ParentId = parentId,
                        Solde = credits,
                        DateModification = maintenant,
                    });
                }
            }

            _context.AchatsFiches.Add(new AchatFiches
            {
                ParentId = parentId,
                Code = CreditsFiches.CodeAjustement,
                Fiches = credits,
                PrixCentimes = 0,
                DateAchat = maintenant,
                Motif = motif.Trim().Length > 200 ? motif.Trim()[..200] : motif.Trim(),
            });

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return await SoldeAsync(parentId, ct);
        }

        public async Task<bool> RembourserAsync(string paiementStripeId, CancellationToken ct = default)
        {
            var achat = await _context.AchatsFiches
                .FirstOrDefaultAsync(a => a.StripePaiementId == paiementStripeId && a.DateRemboursement == null, ct);

            if (achat is null) return false;

            var maintenant = DateTime.UtcNow;
            achat.DateRemboursement = maintenant;

            // Les crédits déjà utilisés ont fait des fiches : on ne les reprend
            // pas sous zéro, l'enfant ne perd pas ce qu'il a révisé.
            await _context.SaveChangesAsync(ct);

            var repris = achat.Fiches;
            await _context.CreditsFichesFamilles
                .Where(c => c.ParentId == achat.ParentId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Solde, c => c.Solde > repris ? c.Solde - repris : 0)
                    .SetProperty(c => c.DateModification, maintenant), ct);

            return true;
        }
    }
}
