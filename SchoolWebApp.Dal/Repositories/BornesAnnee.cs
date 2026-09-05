using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Les bornes d'une année scolaire pour un élève donné.
    ///
    /// TOUT SE RATTACHE PAR LA DATE, ET RIEN PAR UNE COLONNE.
    /// Une conversation traverse les années — c'est ce qui permet au professeur
    /// de dire « cette notion, tu l'as vue l'an dernier » — donc elle ne peut
    /// appartenir à aucune. Seuls les ÉVÉNEMENTS DATÉS se rangent dans une
    /// année : une séance, un message, une note, une observation de maîtrise.
    /// L'historique des classes fournit l'encadrement.
    ///
    /// Déclaré une fois et appelé de trois dépôts : trois résolutions
    /// séparées finiraient par ne plus encadrer la même période.
    /// </summary>
    internal static class BornesAnnee
    {
        /// <summary>
        /// Renvoie (depuis, jusqua) pour l'année demandée, ou (null, null) pour
        /// toute la scolarité.
        ///
        /// UNE FIN NULLE VEUT DIRE « ENCORE EN COURS » : l'année n'est pas
        /// bornée à droite et tout ce qui arrive aujourd'hui lui appartient.
        ///
        /// Une même classe peut avoir été occupée sur DEUX INTERVALLES — un
        /// redoublement, ou une saisie corrigée puis rétablie. On prend du
        /// premier début à la dernière fin : un seul encadrement couvre les
        /// deux, et c'est plus juste que d'en ignorer un.
        /// </summary>
        public static async Task<(DateTime? Depuis, DateTime? Jusqua)> ResoudreAsync(
            SchoolWebAppDatabaseContext context,
            int eleveId,
            int? niveauScolaireId,
            CancellationToken ct = default)
        {
            if (niveauScolaireId is not int niveau) return (null, null);

            var intervalles = await context.HistoriquesClasse
                .AsNoTracking()
                .Where(h => h.EleveId == eleveId && h.NiveauScolaireId == niveau)
                .Select(h => new { h.Debut, h.Fin })
                .ToListAsync(ct);

            if (intervalles.Count == 0) return (null, null);

            var depuis = intervalles.Min(i => i.Debut);

            var jusqua = intervalles.All(i => i.Fin != null)
                ? intervalles.Max(i => i.Fin)
                : null;

            return (depuis, jusqua);
        }
    }
}
