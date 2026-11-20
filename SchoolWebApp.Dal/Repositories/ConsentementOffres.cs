using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;
using SchoolWebApp.Domain.Models;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// LE SEUL ENDROIT QUI CHANGE LE CONSENTEMENT AUX OFFRES PAR E-MAIL (19/11/2026) : l'état du parent (colonnes de
    /// `Parent`) et la ligne du journal, ENSEMBLE, dans une transaction — un consentement sans trace, ou une trace sans
    /// consentement, ne doit pas pouvoir exister.
    ///
    /// SEULEMENT SI L'ÉTAT CHANGE : consentir deux fois garde la date d'origine et n'écrit qu'une ligne ; retirer un
    /// consentement jamais donné n'écrit rien (pas de fausse date de retrait).
    ///
    /// SANS LE SUIVI DES ENTITÉS : `ExecuteUpdate` et un INSERT direct. Un `SaveChanges` ici enregistrerait au passage
    /// toute autre entité modifiée dans le même contexte par l'appelant.
    /// </summary>
    internal static class ConsentementOffres
    {
        public static Task<bool> ChangerAsync(
            SchoolWebAppDatabaseContext context, int parentId, bool consenti, string source, DateTime le, CancellationToken ct) =>
            DansUneTransactionAsync(context, () => EcrireAsync(context, parentId, consenti, source, le, ct), ct);

        /// <summary>
        /// LE RETRAIT FAIT PENDANT UN RETOUR ARRIÈRE (20/11/2026, revue d'Omar). L'ancien code, qui ne connaît pas
        /// `OffresEmail`, désinscrit en écrivant une ligne `DesabonnementMail` « diffusion » et laisse `OffresEmail = 1`.
        /// L'envoi ne s'y trompe pas (`EnvoiAutomatiqueRepository.ConsentEnVigueur`) ; ceci l'inscrit en plus dans l'état
        /// et le journal, à la date de cette ligne, source `retour_arriere`. Vrai si un tel retrait existait.
        /// </summary>
        public static Task<bool> InscrireRetraitDuRetourArriereAsync(
            SchoolWebAppDatabaseContext context, int parentId, CancellationToken ct) =>
            DansUneTransactionAsync(context, () => InscrireRetraitAsync(context, parentId, ct), ct);

        private static Task<bool> DansUneTransactionAsync(
            SchoolWebAppDatabaseContext context, Func<Task<bool>> ecrire, CancellationToken ct)
        {
            // DANS UNE TRANSACTION DÉJÀ OUVERTE PAR L'APPELANT : on écrit dedans, c'est lui qui valide.
            if (context.Database.CurrentTransaction is not null) return ecrire();

            // SINON, PAR LA STRATÉGIE D'EXÉCUTION DU CONTEXTE (20/11/2026, revue d'Omar). Aujourd'hui `UseSqlServer` est
            // enregistré sans `EnableRetryOnFailure` (Program.cs) : la stratégie exécute une fois, sans rien changer.
            // Mais si un jour on active les nouvelles tentatives, un `BeginTransaction` ouvert hors de la stratégie lève
            // une exception : passer par `ExecuteAsync` garde ce code juste dans les deux cas (la transaction entière
            // est rejouée, jamais une moitié).
            return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(ct);
                var change = await ecrire();
                await transaction.CommitAsync(ct);
                return change;
            });
        }

        private static async Task<bool> EcrireAsync(
            SchoolWebAppDatabaseContext context, int parentId, bool consenti, string source, DateTime le, CancellationToken ct)
        {
            // UN NOUVEL ACCORD APRÈS UN RETRAIT DU RETOUR ARRIÈRE : le retrait entre d'abord au journal, à sa date ; sinon
            // `OffresEmail`, resté à 1, ferait croire qu'il n'y a rien à changer, et l'accord ne serait pas écrit.
            if (consenti) await InscrireRetraitAsync(context, parentId, ct);

            return await ModifierAsync(context, parentId, consenti, source, le, ct);
        }

        private static async Task<bool> InscrireRetraitAsync(SchoolWebAppDatabaseContext context, int parentId, CancellationToken ct)
        {
            var categorie = CategorieDesabonnement.Diffusion;

            var le = await context.DesabonnementsMail
                .AsNoTracking()
                .Where(d => d.ParentId == parentId
                            && d.Categorie == categorie
                            && context.Parents.Any(p => p.Id == parentId
                                                        && p.OffresEmail
                                                        && (p.OffresEmailConsentiLe == null || d.DateCreation > p.OffresEmailConsentiLe)))
                .OrderBy(d => d.DateCreation)
                .Select(d => (DateTime?)d.DateCreation)
                .FirstOrDefaultAsync(ct);

            return le is not null
                   && await ModifierAsync(context, parentId, false, SourceConsentement.RetourArriere, le.Value, ct);
        }

        private static async Task<bool> ModifierAsync(
            SchoolWebAppDatabaseContext context, int parentId, bool consenti, string source, DateTime le, CancellationToken ct)
        {
            var parents = context.Parents.Where(p => p.Id == parentId && p.OffresEmail != consenti);

            var changes = consenti
                ? await parents.ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.OffresEmail, true)
                    .SetProperty(p => p.OffresEmailConsentiLe, le)
                    .SetProperty(p => p.OffresEmailSource, source), ct)
                : await parents.ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.OffresEmail, false)
                    .SetProperty(p => p.OffresEmailRetireLe, le)
                    .SetProperty(p => p.OffresEmailSource, source), ct);

            if (changes > 0)
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO [ConsentementOffresJournal] ([parent_id], [consenti], [source], [le]) VALUES ({parentId}, {consenti}, {source}, {le})",
                    ct);
            }

            return changes > 0;
        }
    }
}
