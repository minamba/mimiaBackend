using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Api.Services.MesFiches
{
    /// <summary>
    /// Où en est l'enfant, tel que sa jauge l'affiche, EN COURS IMPORTÉS : ses
    /// cours du mois (utilisés — en route compris —, maximum, restants, jour de
    /// remise à zéro) et les crédits de la famille encore libres.
    /// `Disponibles` = restants + crédits libres : ce qu'il peut encore importer.
    /// `CreditsPris` : les places que CET enfant a déjà prises sur les crédits
    /// pendant la période (en route compris) — la part dorée de sa jauge.
    /// </summary>
    public sealed record QuotaFiches(
        int Utilisees, int Maximum, int Restantes, int Credits, int Disponibles, DateTime RemiseLe, int CreditsPris = 0);

    /// <summary>
    /// LE QUOTA DES FICHES DE RÉVISION — Camara, le 15 puis le 16/10/2026.
    ///
    /// L'UNITÉ EST LE COURS IMPORTÉ, PAS LA FICHE : « même s'il est divisé en
    /// plusieurs sous-fiches, ça compte pour 1 ». Dix cours par mois et par
    /// enfant, qui reviennent COMME LES HEURES (période de l'abonnement de la
    /// famille ; mois civil sans abonnement). Au-delà, les crédits achetés par
    /// le parent : 1 crédit = 1 cours, partagés par la famille, sans expiration.
    ///
    /// UN COURS DE PLUS DE DIX PAGES COMPTE DOUBLE (16/10/2026) : deux places
    /// du mois, ou deux crédits — ou une de chaque, à la bascule. Partout ici,
    /// « un cours » se lit « son poids ».
    ///
    /// DÉCOMPTÉ À LA FIN, RÉSERVÉ DÈS LE DÉPART. Le cours n'est imputé qu'une
    /// fois ses fiches prêtes — un cours raté ne coûte rien. Mais tant qu'il
    /// est en route (lu, notions à choisir, en rédaction), il occupe sa place :
    /// la jauge le compte, et un enfant à qui il reste un cours ne peut pas en
    /// envoyer trois.
    ///
    /// La jauge, le refus à l'envoi et l'imputation de la tâche de fond lisent
    /// tous CETTE règle : la jauge ne peut pas dire « il t'en reste » quand
    /// l'envoi dirait non.
    /// </summary>
    public class QuotaFichesService
    {
        private readonly ICreditsFichesRepository _credits;
        private readonly IAbonnementRepository _abonnements;
        private readonly IFichesDeCoursRepository _cours;

        public QuotaFichesService(ICreditsFichesRepository credits, IAbonnementRepository abonnements, IFichesDeCoursRepository cours)
        {
            _cours = cours ?? throw new ArgumentNullException(nameof(cours));
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
        }

        private sealed record Periode(int ParentId, DateTime Debut, DateTime Fin);

        private async Task<Periode?> PeriodeAsync(int eleveId, CancellationToken ct)
        {
            var parentId = await _credits.ParentDeLEleveAsync(eleveId, ct);
            return parentId is null ? null : await PeriodeDuParentAsync(parentId.Value, ct);
        }

        private async Task<Periode> PeriodeDuParentAsync(int parentId, CancellationToken ct)
        {
            var etat = await _abonnements.GetEtatQuotaAsync(parentId, ct);
            if (etat is not null && etat.PeriodeFin > etat.PeriodeDebut)
            {
                return new Periode(parentId, etat.PeriodeDebut, etat.PeriodeFin);
            }

            var maintenant = DateTime.UtcNow;
            var debut = new DateTime(maintenant.Year, maintenant.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return new Periode(parentId, debut, debut.AddMonths(1));
        }

        public async Task<QuotaFiches> LireAsync(int eleveId, CancellationToken ct)
        {
            var periode = await PeriodeAsync(eleveId, ct);
            if (periode is null) return new QuotaFiches(0, Regles.CoursParMois, 0, 0, 0, DateTime.UtcNow);

            var decomptes = await _credits.CompterDuMoisAsync(eleveId, periode.Debut, ct);
            var prisSurCredits = await _credits.CompterCreditsPrisAsync(eleveId, periode.Debut, ct);
            var enRoute = await _credits.CompterEnCoursAsync(eleveId, ct);
            // Les cours du parent en route (espace parent) réservent déjà des
            // crédits de la famille : ils ne sont plus libres pour les enfants.
            var solde = Math.Max(0,
                await _credits.SoldeAsync(periode.ParentId, ct) - await _cours.CompterEnRouteDuParentAsync(periode.ParentId, ct));

            // Les cours en route prennent d'abord sur le mois, puis sur la
            // réserve — dans le même ordre que l'imputation qui viendra.
            var occupes = decomptes + enRoute;
            var restantes = Math.Max(0, Regles.CoursParMois - occupes);
            var reserveesSurCredits = Math.Max(0, occupes - Regles.CoursParMois);
            var creditsLibres = Math.Max(0, solde - reserveesSurCredits);

            return new QuotaFiches(
                Math.Min(occupes, Regles.CoursParMois), Regles.CoursParMois, restantes,
                creditsLibres, restantes + creditsLibres, periode.Fin,
                prisSurCredits + Math.Min(solde, reserveesSurCredits));
        }

        /// <summary>
        /// Décompte UN cours dont les fiches viennent d'être prêtes — une fois
        /// par cours, quel que soit le nombre de fiches —, pour son POIDS : une
        /// ligne par place prise, chacune sur le mois puis sur les crédits.
        /// </summary>
        public async Task<ImputationFiche?> ImputerAsync(int eleveId, int poids, CancellationToken ct)
        {
            var periode = await PeriodeAsync(eleveId, ct);
            if (periode is null) return null;

            ImputationFiche? derniere = null;
            for (var i = 0; i < Math.Max(1, poids); i += 1)
            {
                derniere = await _credits.ImputerAsync(
                    eleveId, periode.ParentId, null, periode.Debut, Regles.CoursParMois, DateTime.UtcNow, ct);
            }

            return derniere;
        }

        /// <summary>
        /// L'encart « Fiches de révision » du forfait du parent (16/10/2026) :
        /// la réserve de la famille, et les cours du mois de chaque enfant
        /// (en route compris). Une seule période pour toute la famille.
        /// </summary>
        /// <summary>
        /// LE QUOTA D'UN PARENT DANS SON ESPACE (16/10/2026) — Camara : « les
        /// autres parents ont le pot commun avec leurs enfants ; les admins et le
        /// super admin n'ont pas de quota ». Un parent n'a pas de mois à lui : il
        /// puise dans les crédits achetés par la famille, ses cours en route
        /// réservant les leurs. `Disponibles` vaut `int.MaxValue / 2` sans quota.
        /// </summary>
        public async Task<QuotaFiches> LireParentAsync(int parentId, bool sansLimite, CancellationToken ct)
        {
            var periode = await PeriodeDuParentAsync(parentId, ct);
            if (sansLimite) return new QuotaFiches(0, 0, 0, 0, int.MaxValue / 2, periode.Fin);

            var libres = Math.Max(0,
                await _credits.SoldeAsync(parentId, ct) - await _cours.CompterEnRouteDuParentAsync(parentId, ct));
            return new QuotaFiches(0, 0, 0, libres, libres, periode.Fin);
        }

        /// <summary>Un cours de parent prêt : un crédit de la famille par unité de poids (rien sans quota).</summary>
        public async Task ImputerParentAsync(int parentId, int poids, bool sansLimite, CancellationToken ct)
        {
            if (sansLimite) return;
            for (var i = 0; i < Math.Max(1, poids); i += 1)
            {
                await _credits.DebiterFamilleAsync(parentId, DateTime.UtcNow, ct);
            }
        }

        public async Task<(int Credits, int Maximum, DateTime RemiseLe, IReadOnlyList<EnfantFiches> Enfants)> LireFamilleAsync(
            int parentId, CancellationToken ct)
        {
            var periode = await PeriodeDuParentAsync(parentId, ct);
            var enfants = await _credits.EnfantsAsync(parentId, periode.Debut, ct);
            var credits = await _credits.SoldeAsync(parentId, ct);

            return (credits, Regles.CoursParMois, periode.Fin,
                enfants.Select(e => e with { Utilisees = Math.Min(e.Utilisees + e.EnCours, Regles.CoursParMois) }).ToList());
        }
    }
}
