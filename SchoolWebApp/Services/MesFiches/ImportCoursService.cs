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
        private readonly ReveilPodcasts? _reveilPodcasts;
        private readonly QuotaFichesService _quota;
        private readonly AccesForfaitService _acces;

        public ImportCoursService(
            IFichesDeCoursRepository depot, IReglageRepository reglages, ReveilFichesDeCours reveil, QuotaFichesService quota,
            AccesForfaitService acces, ReveilPodcasts? reveilPodcasts = null)
        {
            _reveilPodcasts = reveilPodcasts;
            _acces = acces ?? throw new ArgumentNullException(nameof(acces));
            _quota = quota ?? throw new ArgumentNullException(nameof(quota));
            _depot = depot ?? throw new ArgumentNullException(nameof(depot));
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
        }

        /// <summary>
        /// FICHES OU PODCASTS (15/11/2026) : le service sert les deux, la nature est celle du dépôt (donc de la portée,
        /// posée par le contrôleur ou le QR code). Les règles et les phrases qui changent sont ci-dessous.
        /// </summary>
        public string Nature
        {
            get => _depot.Nature;
            set => _depot.Nature = value;
        }

        private bool Podcasts => _depot.Nature == NaturesCours.Podcasts;

        /// <summary>La tâche de fond de la bonne nature : fiches, ou podcasts.</summary>
        private void Sonner()
        {
            if (Podcasts) _reveilPodcasts?.Sonner();
            else _reveil.Sonner();
        }

        /// <summary>« fiches de révision » ou « podcasts », pour les phrases communes.</summary>
        private string Module => Podcasts ? "podcasts" : "fiches de révision";

        private Refus FermeModule() => new(Podcasts ? "Les podcasts ne sont pas ouverts." : "Les fiches de révision ne sont pas ouvertes.");

        public Task<bool> ModuleActifAsync(CancellationToken ct) => Podcasts
            ? _reglages.EstActifAsync(PodcastsEleve.Reglage, PodcastsEleve.ActifParDefaut, ct)
            : _reglages.EstActifAsync(Regles.Reglage, Regles.ActifParDefaut, ct);

        public Task<QuotaFiches> QuotaAsync(int eleveId, CancellationToken ct) => _quota.LireAsync(eleveId, ct);

        /// <summary>Le module des podcasts, quelle que soit la nature de cette portée (le menu de l'espace parent).</summary>
        public Task<bool> PodcastsActifsAsync(CancellationToken ct) =>
            _reglages.EstActifAsync(PodcastsEleve.Reglage, PodcastsEleve.ActifParDefaut, ct);

        /// <summary>
        /// Les plafonds d'import : ceux de tout le monde, ou ceux d'une famille
        /// administratrice. Dans l'espace parent, ceux du parent lui-même.
        /// </summary>
        public async Task<Regles.LimitesImport> LimitesAsync(Proprietaire proprietaire, CancellationToken ct)
        {
            var sansLimite = proprietaire.ParentId is int parentId
                ? (await _depot.DroitsEspaceParentAsync(parentId, ct)).SansLimite
                : await _depot.FamilleSansLimiteAsync(proprietaire.EleveId ?? 0, ct);

            // PODCASTS (15/11/2026) : quarante pages par cours (vingt pour les fiches).
            return Podcasts ? PodcastsEleve.Limites(sansLimite) : Regles.Limites(sansLimite);
        }

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
        /// pause, ni plus d'abonnement. Null quand la porte est ouverte, sinon
        /// la phrase pour l'enfant.
        ///
        /// SAUF UN COURS D'ESSAI PAR FAMILLE (Camara, le 18/10/2026) : en essai,
        /// on consulte toujours ses fiches, on commence un cours tant que
        /// l'essai est libre, et on fait rédiger le cours de l'essai tant qu'il
        /// n'a pas ses fiches. Ensuite : « il faut être abonné ».
        /// </summary>
        public async Task<Refus?> PorteDuForfaitAsync(Proprietaire proprietaire, UsageFiches usage, CancellationToken ct)
        {
            // L'ESPACE PARENT : c'est le droit coché dans Admin › Parents qui ouvre
            // la porte, pas le forfait (les crédits suffisent à un parent).
            if (proprietaire.ParentId is int parentId)
            {
                // PODCASTS DU PARENT (15/11/2026) : le même droit que ses fiches (décision de Camara).
                if (!(await _depot.DroitsEspaceParentAsync(parentId, ct)).Fiches)
                    return new Refus($"Les {Module} ne sont pas ouverts sur votre compte.");

                // PAS DE PODCASTS AVEC STARTER (Camara, le 15/11/2026).
                return Podcasts && !await _acces.PodcastsOuvertsAsync(parentId, ct)
                    ? new Refus(PodcastsEleve.MessageNonInclusParent)
                    : null;
            }

            var eleveId = proprietaire.EleveId ?? 0;

            // PAS DE PODCASTS AVEC STARTER (Camara, le 15/11/2026) : même pour relire ceux d'avant.
            if (Podcasts && !await _acces.PodcastsPourEleveAsync(eleveId, ct)) return new Refus(PodcastsEleve.MessageNonInclusEnfant);

            var acces = await _acces.LirePourEleveAsync(eleveId, ct);
            if (acces.Ouvert) return null;

            if (acces.Motif == AccesForfaitService.MotifEssai)
            {
                if (usage == UsageFiches.Consulter) return null;

                var essai = await _depot.EssaiFichesAsync(eleveId, ct);
                var passe = usage == UsageFiches.NouveauCours
                    ? essai == Regles.EssaiLibre
                    : essai != Regles.EssaiUtilise;
                return passe ? null : new Refus(Podcasts ? PodcastsEleve.MessageEssaiUtilise : Regles.MessageEssaiUtilise);
            }

            return new Refus($"Il faut un forfait pour accéder aux {Module}. Demande à tes parents d’en choisir un.");
        }

        /// <summary>
        /// L'essai des fiches de cet élève, pour les pages : null hors essai
        /// (abonné ou porte fermée), sinon libre, en route ou utilisé.
        /// </summary>
        public async Task<string?> EssaiFichesAsync(int eleveId, CancellationToken ct) =>
            (await _acces.LirePourEleveAsync(eleveId, ct)) is { Ouvert: false, Motif: AccesForfaitService.MotifEssai }
                ? await _depot.EssaiFichesAsync(eleveId, ct)
                : null;

        private Refus QuotaEpuise(QuotaFiches quota, bool parent) => parent
            ? new($"Vous n’avez plus de crédits de {(Podcasts ? "podcasts" : "fiches")}. Vous pouvez en acheter dans Mon forfait.")
            : new(
                $"Tu as utilisé tes {quota.Maximum} {(Podcasts ? "podcasts" : "cours")} de ce mois-ci. Demande à tes parents d’acheter des "
                + $"crédits pour en créer d’autres. Sinon, ils reviennent le {quota.RemiseLe:dd/MM}.");

        /// <summary>Une page de plus sur un brouillon : validée, comptée, rangée.</summary>
        public async Task<(PageDuCours? Page, Refus? Refus)> AjouterPageAsync(
            Proprietaire proprietaire, int coursId, string? nomFichier, string? typeAnnonce, byte[] donnees, CancellationToken ct)
        {
            if (!await ModuleActifAsync(ct)) return (null, FermeModule());
            if (await PorteDuForfaitAsync(proprietaire, UsageFiches.NouveauCours, ct) is Refus fermee) return (null, fermee);

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
        /// LE TEXTE D'UNE FICHE LIBRE (06/11/2026) : ce que l'enfant a écrit ou dicté,
        /// rangé comme l'unique page du brouillon. Réécrit, il REMPLACE le précédent
        /// (un nouvel essai après une erreur ne doit pas l'envoyer deux fois).
        /// </summary>
        public async Task<(PageDuCours? Page, Refus? Refus)> EcrireTexteAsync(
            Proprietaire proprietaire, int coursId, string? texte, CancellationToken ct, bool livre = false)
        {
            if (!await ModuleActifAsync(ct)) return (null, FermeModule());
            if (await PorteDuForfaitAsync(proprietaire, UsageFiches.NouveauCours, ct) is Refus fermee) return (null, fermee);

            var propre = (texte ?? string.Empty).Replace("\r\n", "\n").Trim();
            if (propre.Length < Regles.TexteMin)
            {
                return (null, new Refus("Écris ou dicte d’abord ce que tu veux réviser."));
            }

            if (propre.Length > Regles.TexteMax)
            {
                return (null, new Refus(
                    $"Ton texte fait {propre.Length} caractères : c’est {Regles.TexteMax} au maximum. Garde seulement la leçon."));
            }

            var cours = await _depot.GetAsync(proprietaire, coursId, ct);
            if (cours is null || cours.Etat != Regles.EtatBrouillon)
            {
                return (null, new Refus("Ce cours n’accepte plus de texte : il est déjà envoyé."));
            }

            foreach (var ancienne in cours.Pages)
            {
                await _depot.SupprimerPageAsync(proprietaire, coursId, ancienne.Id, ct);
            }

            var octets = System.Text.Encoding.UTF8.GetBytes(propre);
            // LE RÉSUMÉ DE LIVRE (15/11/2026) : le même texte, marqué « livre » pour la lecture et la rédaction.
            var page = await _depot.AjouterPageAsync(
                proprietaire, coursId,
                livre ? PodcastsEleve.NomLivre : Regles.NomTexte,
                livre ? PodcastsEleve.TypeLivre : Regles.TypeTexte,
                octets, 1, DateTime.UtcNow, ct);
            return page is null ? (null, new Refus("Ce cours n’accepte plus de texte.")) : (page, null);
        }

        /// <summary>
        /// Le brouillon part à la lecture. La limite du mois se compte ici : un
        /// brouillon abandonné ne coûte rien et ne compte pas.
        /// </summary>
        public async Task<Refus?> EnvoyerAsync(Proprietaire proprietaire, int coursId, CancellationToken ct)
        {
            if (!await ModuleActifAsync(ct)) return FermeModule();
            if (await PorteDuForfaitAsync(proprietaire, UsageFiches.NouveauCours, ct) is Refus fermee) return fermee;

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

            Sonner();
            return null;
        }

        /// <summary>Une demande de fiches sur un cours déjà lu : un type, et les notions choisies.</summary>
        public async Task<Refus?> DemanderAsync(Proprietaire proprietaire, int coursId, string type, IReadOnlyList<int>? notions, CancellationToken ct)
        {
            if (!await ModuleActifAsync(ct)) return FermeModule();
            if (await PorteDuForfaitAsync(proprietaire, UsageFiches.Rediger, ct) is Refus fermee) return fermee;

            // PODCASTS (15/11/2026) : un seul « type », un épisode par notion choisie.
            var regle = Podcasts
                ? new Regles.TypeFiche(PodcastsEleve.Type, "Podcast", ParNotion: true, RangMin: 1)
                : Regles.Type(type);
            if (regle is null) return new Refus("Ce type de fiche n’existe pas.");

            var cours = await _depot.GetAsync(proprietaire, coursId, ct);
            if (cours is null) return new Refus("Ce cours n’existe plus.");

            if (!cours.PagesDisponibles)
            {
                return new Refus($"Ce cours a été effacé : importe-le à nouveau pour créer d’autres {(Podcasts ? "épisodes" : "fiches")}.");
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

            Sonner();
            return null;
        }
    }
}
