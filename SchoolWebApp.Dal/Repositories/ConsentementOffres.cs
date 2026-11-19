using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

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
        public static async Task<bool> ChangerAsync(
            SchoolWebAppDatabaseContext context, int parentId, bool consenti, string source, DateTime le, CancellationToken ct)
        {
            var proprietaire = context.Database.CurrentTransaction is null;
            await using var transaction = proprietaire ? await context.Database.BeginTransactionAsync(ct) : null;

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

            if (transaction is not null) await transaction.CommitAsync(ct);

            return changes > 0;
        }
    }
}
