using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Services.Paiement
{
    /// <summary>
    /// Ce que la famille peut ouvrir : `Ouvert`, ou fermé avec son motif —
    /// « essai », « sans-forfait » ou « pause ».
    /// </summary>
    public sealed record AccesForfait(bool Ouvert, string? Motif)
    {
        public static readonly AccesForfait Libre = new(true, null);
    }

    /// <summary>
    /// LES FICHES ET LES JEUX DEMANDENT UN VRAI FORFAIT — Camara, le 16/10/2026 :
    /// « quand une personne est en essai ou n'a plus de forfait », la carte des
    /// fiches de révision est désactivée et les jeux sont grisés.
    ///
    /// VRAI FORFAIT = un abonnement en cours, qui n'est ni l'essai ni en pause.
    /// Même lecture que les heures (`AbonnementRepository.VerifierAsync` refuse
    /// sans abonnement et en pause). Les administrateurs et les comptes exemptés
    /// passent toujours : ce sont eux qui testent, et ils ne paient jamais.
    ///
    /// Une seule règle pour l'écran (la route `acces`) et pour le serveur (la
    /// création de fiches) : l'écran ne peut pas ouvrir ce que le serveur
    /// refuserait.
    /// </summary>
    public class AccesForfaitService
    {
        public const string MotifEssai = "essai";
        public const string MotifSansForfait = "sans-forfait";
        public const string MotifPause = "pause";

        private readonly IAbonnementRepository _abonnements;
        private readonly IParentRepository _parents;
        private readonly ICreditsFichesRepository _credits;
        private readonly ExemptionFacturation _exemptions;

        public AccesForfaitService(
            IAbonnementRepository abonnements,
            IParentRepository parents,
            ICreditsFichesRepository credits,
            IOptions<ExemptionFacturation> exemptions)
        {
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _parents = parents ?? throw new ArgumentNullException(nameof(parents));
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _exemptions = exemptions?.Value ?? throw new ArgumentNullException(nameof(exemptions));
        }

        public async Task<AccesForfait> LireAsync(int parentId, CancellationToken ct = default)
        {
            var parent = await _parents.GetParentByIdAsync(parentId);
            if (parent is null) return new AccesForfait(false, MotifSansForfait);

            if (parent.EstAdministrateur || _exemptions.EstExempte(parent.Mail)) return AccesForfait.Libre;

            var etat = await _abonnements.GetEtatQuotaAsync(parentId, ct);
            if (etat is null) return new AccesForfait(false, MotifSansForfait);
            if (etat.EstEssai) return new AccesForfait(false, MotifEssai);
            if (etat.EnPause) return new AccesForfait(false, MotifPause);

            return AccesForfait.Libre;
        }

        public async Task<AccesForfait> LirePourEleveAsync(int eleveId, CancellationToken ct = default)
        {
            var parentId = await _credits.ParentDeLEleveAsync(eleveId, ct);
            return parentId is null ? new AccesForfait(false, MotifSansForfait) : await LireAsync(parentId.Value, ct);
        }
    }
}
