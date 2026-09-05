using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Dal.Repositories
{
    /// <summary>
    /// Tient à jour les intervalles de classe d'un élève.
    ///
    /// ÉCRIT UNE FOIS, APPELÉ DE TROIS ENDROITS : la création d'un élève, sa
    /// modification par le parent, sa modification par l'administration. Trois
    /// copies de cette logique auraient fini par diverger, et une divergence
    /// ici ne se verrait qu'à la rentrée suivante — quand un élève verrait sa
    /// fiche vide sur une classe où il a travaillé.
    /// </summary>
    internal static class HistoriqueClasse
    {
        /// <summary>
        /// Ouvre le premier intervalle d'un élève.
        ///
        /// La date est celle de sa création et non l'instant présent : les deux
        /// se confondent à l'inscription, mais pas lors d'un rattrapage de
        /// données, où partir d'aujourd'hui laisserait tout son passé en dehors
        /// de toute année.
        /// </summary>
        public static void Ouvrir(
            SchoolWebAppDatabaseContext context, int eleveId, int? niveauId, DateTime depuis)
        {
            if (niveauId is not int niveau || niveau <= 0) return;

            context.HistoriquesClasse.Add(new HistoriqueClasseEleve
            {
                EleveId = eleveId,
                NiveauScolaireId = niveau,
                Debut = depuis,
                Fin = null,
            });
        }

        /// <summary>
        /// Ferme l'intervalle courant et en ouvre un autre, si la classe change
        /// vraiment.
        ///
        /// RIEN NE SE PASSE SI LA CLASSE EST LA MÊME. Un parent qui enregistre
        /// le prénom de son enfant sans toucher à sa classe passe par le même
        /// chemin : découper l'année à chaque sauvegarde émietterait
        /// l'historique en intervalles d'une minute, et la fiche afficherait
        /// autant d'onglets « 3e » que de modifications.
        ///
        /// L'intervalle est fermé et le suivant ouvert À LA MÊME SECONDE, sans
        /// trou : une séance qui tomberait entre les deux n'appartiendrait
        /// autrement à aucune année.
        /// </summary>
        public static async Task ChangerAsync(
            SchoolWebAppDatabaseContext context, int eleveId, int nouveauNiveauId, CancellationToken ct = default)
        {
            if (nouveauNiveauId <= 0) return;

            var courant = await context.HistoriquesClasse
                .Where(h => h.EleveId == eleveId && h.Fin == null)
                .OrderByDescending(h => h.Debut)
                .FirstOrDefaultAsync(ct);

            if (courant is not null && courant.NiveauScolaireId == nouveauNiveauId) return;

            var maintenant = DateTime.UtcNow;

            if (courant is not null) courant.Fin = maintenant;

            // Un élève sans intervalle ouvert — créé avant cette table, ou dont
            // l'historique a été purgé — en reçoit un à partir de maintenant.
            // C'est le mieux qu'on puisse faire : son passé reste sans année,
            // et l'inventer serait pire que de l'admettre.
            context.HistoriquesClasse.Add(new HistoriqueClasseEleve
            {
                EleveId = eleveId,
                NiveauScolaireId = nouveauNiveauId,
                Debut = maintenant,
                Fin = null,
            });
        }
    }
}
