using SchoolWebApp.Api.Services.Paiement;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Api.Workers;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Api.Services.MesFiches
{
    /// <summary>Un refus, avec la phrase écrite pour l'enfant.</summary>
    public sealed record Refus(string Message);

    /// <summary>
    /// LES RÈGLES DE L'IMPORT D'UN COURS, À UN SEUL ENDROIT — 14/10/2026.
    ///
    /// Les routes de l'élève et le scan au téléphone passent tous deux par
    /// ici : un format, un poids, vingt pages et le quota de fiches se
    /// vérifient une fois, avec les mêmes phrases pour l'enfant.
    ///
    /// L'appartenance du cours à l'élève est garantie par le dépôt (chaque
    /// requête filtre sur l'élève) ; celle de l'élève à l'appelant par le
    /// contrôleur, avant d'arriver ici.
    /// </summary>
    public class ImportCoursService
    {
        private readonly IFichesDeCoursRepository _depot;
        private readonly IReglageRepository _reglages;
        private readonly ReveilFichesDeCours _reveil;
        private readonly QuotaFichesService _quota;
        private readonly AccesForfaitService _acces;

        public ImportCoursService(
            IFichesDeCoursRepository depot, IReglageRepository reglages, ReveilFichesDeCours reveil, QuotaFichesService quota,
            AccesForfaitService acces)
        {
            _acces = acces ?? throw new ArgumentNullException(nameof(acces));
            _quota = quota ?? throw new ArgumentNullException(nameof(quota));
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
        }

        public Task<bool> ModuleActifAsync(CancellationToken ct) =>
            _reglages.EstActifAsync(Regles.Reglage, Regles.ActifParDefaut, ct);

        public Task<QuotaFiches> QuotaAsync(int eleveId, CancellationToken ct) => _quota.LireAsync(eleveId, ct);

        /// <summary>
        /// Les plafonds d'import : ceux de tout le monde, ou ceux d'une famille
        /// administratrice. Dans l'espace parent, ceux du parent lui-même.
        /// </summary>
        public async Task<Regles.LimitesImport> LimitesAsync(Proprietaire proprietaire, CancellationToken ct) =>
            Regles.Limites(proprietaire.ParentId is int parentId
                ? (await _depot.DroitsEspaceParentAsync(parentId, ct)).SansLimite
                : await _depot.FamilleSansLimiteAsync(proprietaire.EleveId ?? 0, ct));

        /// <summary>
        /// LE QUOTA DU PROPRIÉTAIRE : le mois de l'enfant puis les crédits de la
        /// famille ; pour un parent, les crédits seuls — ou rien à compter pour un
        /// administrateur (16/10/2026).
        /// </summary>
        public async Task<QuotaFiches> QuotaDuProprietaireAsync(Proprietaire proprietaire, CancellationToken ct) =>
            proprietaire.ParentId is int parentId
                ? await _quota.LireParentAsync(parentId, (await _depot.DroitsEspaceParentAsync(parentId, ct)).SansLimite, ct)
                : await _quota.LireAsync(proprietaire.EleveId ?? 0, ct);

        /// <summary>
        /// LES FICHES DEMANDENT UN VRAI FORFAIT (Camara, le 16/10/2026) : ni
        /// essai, ni pause, ni plus d'abonnement. Null quand la porte est
        /// ouverte, sinon la phrase pour l'enfant.
        /// </summary>
        public async Task<Refus?> PorteDuForfaitAsync(Proprietaire proprietaire, CancellationToken ct)
        {
            // L'ESPACE PARENT : c'est le droit coché dans Admin › Parents qui ouvre
            // la porte, pas le forfait (les crédits suffisent à un parent).
            if (proprietaire.ParentId is int parentId)
            {
                return (await _depot.DroitsEspaceParentAsync(parentId, ct)).Fiches
                    ? null
                    : new Refus("Les fiches de révision ne sont pas ouvertes sur votre compte.");
            }

            return (await _acces.LirePourEleveAsync(proprietaire.EleveId ?? 0, ct)).Ouvert
                ? null
                : new Refus("Il faut un forfait pour accéder aux fiches de révision. Demande à tes parents d’en choisir un.");
        }

        private static Refus QuotaEpuise(QuotaFiches quota, bool parent) => parent
            ? new("Vous n’avez plus de crédits de fiches. Vous pouvez en acheter dans Mon forfait.")
            : new(
                $"Tu as utilisé tes {quota.Maximum} cours de ce mois-ci. Demande à tes parents d’acheter des "
                + $"crédits pour en importer d’autres. Sinon, tes cours reviennent le {quota.RemiseLe:dd/MM}.");

        /// <summary>Une page de plus sur un brouillon : validée, comptée, rangée.</summary>
        public async Task<(PageDuCours? Page, Refus? Refus)> AjouterPageAsync(
            Proprietaire proprietaire, int coursId, string? nomFichier, string? typeAnnonce, byte[] donnees, CancellationToken ct)
        {
            if (!await ModuleActifAsync(ct)) return (null, new Refus("Les fiches de révision ne sont pas ouvertes."));
            if (await PorteDuForfaitAsync(proprietaire, ct) is Refus fermee) return (null, fermee);

            var cours = await _depot.GetAsync(proprietaire, coursId, ct);
            if (cours is null || cours.Etat != Regles.EtatBrouillon)
            {
                return (null, new Refus("Ce cours n’accepte plus de pages : il est déjà envoyé."));
            }

            var limites = await LimitesAsync(proprietaire, ct);

            if (cours.Pages.Count >= limites.PiecesMax)
            {
                return (null, new Refus($"Tu as déjà {limites.PiecesMax} pièces : c’est le maximum pour un cours."));
            }

            var verdict = ValidationPieceJointe.Valider(donnees, typeAnnonce, limites.PagesMax, limites.TailleFichierMax);
            if (!verdict.Accepte) return (null, new Refus(verdict.Motif ?? "Ce fichier n’est pas accepté."));

            var pages = Math.Max(1, verdict.NombrePages);
            var total = cours.Pages.Sum(p => p.NombrePages) + pages;
            if (total > limites.PagesMax)
            {
                return (null, new Refus(
                    $"Avec ce fichier, ton cours ferait {total} pages : c’est {limites.PagesMax} au maximum. "
                    + "Garde seulement les pages du chapitre."));
            }

            // Pour une famille administratrice, le poids du cours ENTIER : il part
            // à la lecture en une seule requête (voir `FichesRevisionEleve`).
            if (limites.TailleCoursMax is int plafond && cours.Pages.Sum(p => (long)p.Taille) + donnees.Length > plafond)
            {
                return (null, new Refus(
                    $"Avec ce fichier, ton cours dépasserait {plafond / (1024 * 1024)} Mo, le maximum que la lecture "
                    + "accepte en une fois. Sépare-le en deux cours."));
            }

            var typeReel = ValidationPieceJointe.ReconnaitreType(donnees) ?? typeAnnonce ?? "image/jpeg";
            var nom = string.IsNullOrWhiteSpace(nomFichier) ? null : Path.GetFileName(nomFichier.Trim());
            if (nom is { Length: > 200 }) nom = nom[..200];

            var page = await _depot.AjouterPageAsync(proprietaire, coursId, nom, typeReel, donnees, pages, DateTime.UtcNow, ct);
            return page is null ? (null, new Refus("Ce cours n’accepte plus de pages.")) : (page, null);
        }

        /// <summary>
        /// Le brouillon part à la lecture. La limite du mois se compte ici : un
        /// brouillon abandonné ne coûte rien et ne compte pas.
        /// </summary>
        public async Task<Refus?> EnvoyerAsync(Proprietaire proprietaire, int coursId, CancellationToken ct)
        {
            if (!await ModuleActifAsync(ct)) return new Refus("Les fiches de révision ne sont pas ouvertes.");
            if (await PorteDuForfaitAsync(proprietaire, ct) is Refus fermee) return fermee;

            var maintenant = DateTime.UtcNow;

            var cours = await _depot.GetAsync(proprietaire, coursId, ct);
            var pages = cours?.Pages.Sum(p => Math.Max(1, p.NombrePages)) ?? 0;
            var poids = Regles.Poids(pages);

            // UN COURS DE PLUS DOIT TENIR, cours en route compris : envoyer, c'est
            // réserver sa place (Camara, le 16/10/2026). Lire un cours coûte —
            // on ne le lit pas s'il ne pourra pas devenir des fiches. Un cours
            // de plus de dix pages demande DEUX places.
            var quota = await QuotaDuProprietaireAsync(proprietaire, ct);
            if (quota.Disponibles <= 0) return QuotaEpuise(quota, proprietaire.EstParent);
            if (quota.Disponibles < poids && proprietaire.EstParent)
            {
                return new Refus(
                    $"Ce cours fait {pages} pages : au-delà de {Regles.PagesCoursSimple} pages, il compte pour 2 crédits, "
                    + "et il ne vous en reste qu’un. Gardez seulement les pages utiles, ou achetez des crédits dans Mon forfait.");
            }

            if (quota.Disponibles < poids)
            {
                return new Refus(
                    $"Ton cours fait {pages} pages : au-delà de {Regles.PagesCoursSimple} pages, il compte pour 2 cours, "
                    + "et il ne t’en reste qu’un. Garde seulement les pages de la leçon "
                    + $"({Regles.PagesCoursSimple} au plus), ou demande à tes parents d’acheter des crédits.");
            }

            if (!await _depot.EnvoyerAsync(proprietaire, coursId, poids, maintenant, ct))
            {
                return new Refus("Ajoute au moins une page de ton cours avant de continuer.");
            }

            _reveil.Sonner();
            return null;
        }

        /// <summary>Une demande de fiches sur un cours déjà lu : un type, et les notions choisies.</summary>
        public async Task<Refus?> DemanderAsync(Proprietaire proprietaire, int coursId, string type, IReadOnlyList<int>? notions, CancellationToken ct)
        {
            if (!await ModuleActifAsync(ct)) return new Refus("Les fiches de révision ne sont pas ouvertes.");
            if (await PorteDuForfaitAsync(proprietaire, ct) is Refus fermee) return fermee;

            var regle = Regles.Type(type);
            if (regle is null) return new Refus("Ce type de fiche n’existe pas.");

            var cours = await _depot.GetAsync(proprietaire, coursId, ct);
            if (cours is null) return new Refus("Ce cours n’existe plus.");

            if (!cours.PagesDisponibles)
            {
                return new Refus("Ce cours a été effacé : importe-le à nouveau pour créer d’autres fiches.");
            }

            var rangs = regle.ParNotion
                ? (notions ?? []).Where(r => r >= 1 && r <= cours.Notions.Count).Distinct().OrderBy(r => r).ToList()
                : [];

            if (regle.ParNotion && rangs.Count == 0) return new Refus("Choisis au moins une notion.");

            // PAS DE CONTRÔLE DU QUOTA ICI : le cours a réservé sa place en
            // partant à la lecture, et toutes ses notions ne comptent que pour
            // UN cours (16/10/2026).

            if (!await _depot.DemanderAsync(proprietaire, coursId, regle.Code, rangs, DateTime.UtcNow, ct))
            {
                return new Refus("Ce cours est encore en cours de lecture. Attends qu’il soit prêt.");
            }

            _reveil.Sonner();
            return null;
        }
    }
}
