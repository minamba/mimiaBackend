using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
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

        /// <summary>
        /// LA CONSEILLÈRE D'ORIENTATION, SELON LA FORMULE — Camara, le 14/11/2026 : pas de conseillère avec Starter.
        /// L'ESSAI LA GARDE (Camara, le 15/11/2026, après l'avoir fermée le même jour) : on la découvre pendant l'essai,
        /// l'écran dit bien qu'elle n'est incluse qu'à partir de Solo.
        /// Administrateurs et comptes exemptés : toujours. Sans abonnement lisible : oui ici, le quota refusera de
        /// toute façon. Voir <c>ConseillereOrientation.FormulesSansConseillere</c>.
        /// </summary>
        public async Task<bool> ConseillereOuverteAsync(int parentId, CancellationToken ct = default)
        {
            var parent = await _parents.GetParentByIdAsync(parentId);
            if (parent is null) return false;
            if (parent.EstAdministrateur || _exemptions.EstExempte(parent.Mail)) return true;

            var etat = await _abonnements.GetEtatQuotaAsync(parentId, ct);
            return etat is null || ConseillereOrientation.IncluseDans(etat.OffreCode);
        }

        public async Task<bool> ConseillerePourEleveAsync(int eleveId, CancellationToken ct = default)
        {
            var parentId = await _credits.ParentDeLEleveAsync(eleveId, ct);
            return parentId is not null && await ConseillereOuverteAsync(parentId.Value, ct);
        }

        /// <summary>
        /// LES PODCASTS, SELON LA FORMULE — Camara, le 15/11/2026 : ni avec Starter, ni en essai (« il faut minimum
        /// l'abonnement Solo »). Même lecture que la conseillère : administrateurs et exemptés toujours, sans abonnement
        /// lisible oui (le forfait refusera de toute façon). Voir <c>PodcastsEleve.FormulesSansPodcasts</c>.
        /// </summary>
        public async Task<bool> PodcastsOuvertsAsync(int parentId, CancellationToken ct = default)
        {
            var parent = await _parents.GetParentByIdAsync(parentId);
            if (parent is null) return false;
            if (parent.EstAdministrateur || _exemptions.EstExempte(parent.Mail)) return true;

            var etat = await _abonnements.GetEtatQuotaAsync(parentId, ct);
            return etat is null || (!etat.EstEssai && PodcastsEleve.InclusDans(etat.OffreCode));
        }

        public async Task<bool> PodcastsPourEleveAsync(int eleveId, CancellationToken ct = default)
        {
            var parentId = await _credits.ParentDeLEleveAsync(eleveId, ct);
            return parentId is not null && await PodcastsOuvertsAsync(parentId.Value, ct);
        }
    }
}
