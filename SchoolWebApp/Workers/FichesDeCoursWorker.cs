using SchoolWebApp.Api.Services.MesFiches;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>Réveille la tâche des fiches de révision dès qu'un cours est envoyé ou qu'une fiche est demandée.</summary>
    public sealed class ReveilFichesDeCours : Sonnette
    {
    }

    /// <summary>
    /// LIRE LES COURS IMPORTÉS ET EN ÉCRIRE LES FICHES — 14/10/2026.
    ///
    /// Un cours passe ici deux fois au plus :
    /// 1. À LIRE : le modèle en trouve le titre et les notions. Un cours d'une
    ///    seule notion, ou un type qui couvre tout le cours (express, spéciale
    ///    contrôle), part directement en rédaction ; sinon l'enfant choisit
    ///    ses notions, et le cours attend (état « notions »).
    /// 2. À GÉNÉRER : une fiche par notion demandée — ou une seule pour le
    ///    cours entier —, l'une après l'autre. Chaque fiche est enregistrée dès
    ///    qu'elle est prête : l'enfant les voit arriver.
    ///
    /// UNE FICHE RATÉE NE BLOQUE PAS LES AUTRES, et le cours n'est pas
    /// réessayé indéfiniment : trois passages au plus, puis il s'arrête — prêt
    /// avec ce qui a réussi, ou en échec avec une phrase pour l'enfant.
    ///
    /// Réveillé par une sonnette ; à défaut, toutes les cinq minutes — c'est
    /// aussi ce qui reprend un cours laissé en plan par un redémarrage, et ce
    /// qui purge les brouillons abandonnés et les vieilles pages.
    /// </summary>
    public class FichesDeCoursWorker : BackgroundService
    {
        private const int TailleLot = 5;
        private const int TentativesMax = 3;
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopes;
        private readonly RedacteurFichesService _redacteur;
        private readonly ReveilFichesDeCours _reveil;
        private readonly ILogger<FichesDeCoursWorker> _logger;

        public FichesDeCoursWorker(
            IServiceScopeFactory scopes,
            RedacteurFichesService redacteur,
            ReveilFichesDeCours reveil,
            ILogger<FichesDeCoursWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _redacteur = redacteur ?? throw new ArgumentNullException(nameof(redacteur));
            _reveil = reveil ?? throw new ArgumentNullException(nameof(reveil));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await TourAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec du tour des fiches de revision.");
                }

                try
                {
                    await _reveil.AttendreAsync(Intervalle, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task TourAsync(CancellationToken ct)
        {
            using var portee = _scopes.CreateScope();
            var services = portee.ServiceProvider;
            var depot = services.GetRequiredService<IFichesDeCoursRepository>();
            var reglages = services.GetRequiredService<IReglageRepository>();
            var quota = services.GetRequiredService<QuotaFichesService>();

            var purges = await depot.PurgerAsync(DateTime.UtcNow, ct);
            if (purges > 0) _logger.LogInformation("Fiches de revision : {Nombre} brouillon(s) ou cours purge(s).", purges);

            // Les deux interrupteurs : le module lui-même, et le coupe-circuit
            // de toutes les tâches de fond. Un cours en attente reste en
            // attente ; il sera traité au rallumage.
            if (!await reglages.EstActifAsync(Regles.Reglage, Regles.ActifParDefaut, ct)
                || !await reglages.EstActifAsync(Controllers.ReglagesController.TachesDeFond, true, ct))
            {
                return;
            }

            foreach (var id in await depot.GetATraiterAsync(TailleLot, ct))
            {
                await TraiterAsync(depot, quota, id, ct);
            }

            // Un lot plein : il en reste peut-être. On se resonne plutôt que
            // d'attendre cinq minutes.
            if ((await depot.GetATraiterAsync(1, ct)).Count > 0) _reveil.Sonner();
        }

        private async Task TraiterAsync(IFichesDeCoursRepository depot, QuotaFichesService quota, int id, CancellationToken ct)
        {
            var cours = await depot.GetPourTraitementAsync(id, ct);
            if (cours is null) return;

            if (cours.Pages.Count == 0)
            {
                await depot.MarquerAsync(id, Regles.EtatEchec,
                    "Les pages de ce cours ne sont plus disponibles : importe-le à nouveau.", cours.Tentatives, DateTime.UtcNow, ct);
                return;
            }

            if (cours.Etat == Regles.EtatALire)
            {
                cours = await LireAsync(depot, cours, ct);
                if (cours is null || cours.Etat != Regles.EtatAGenerer) return;
            }

            if (cours.Etat == Regles.EtatAGenerer) await RedigerAsync(depot, quota, cours, ct);
        }

        /// <summary>Lit le cours ; rend le cours relu s'il doit partir en rédaction tout de suite.</summary>
        private async Task<CoursATraiter?> LireAsync(IFichesDeCoursRepository depot, CoursATraiter cours, CancellationToken ct)
        {
            LectureDuCours lecture;
            try
            {
                lecture = await _redacteur.LireAsync(cours, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Lecture du cours {CoursId} impossible.", cours.Id);
                var tentatives = cours.Tentatives + 1;
                await depot.MarquerAsync(cours.Id,
                    tentatives >= TentativesMax ? Regles.EtatEchec : Regles.EtatALire,
                    tentatives >= TentativesMax ? "Je n’ai pas réussi à lire ton cours. Importe-le à nouveau dans un moment." : null,
                    tentatives, DateTime.UtcNow, ct);
                return null;
            }

            var maintenant = DateTime.UtcNow;

            if (!lecture.Lisible)
            {
                await depot.EnregistrerLectureAsync(cours.Id, Regles.EtatEchec, null, [], lecture.Remarque, maintenant, ct);
                return null;
            }

            await depot.EnregistrerLectureAsync(cours.Id, Regles.EtatNotions, lecture.Titre, lecture.Notions, null, maintenant, ct);
            _logger.LogInformation("Cours {CoursId} lu : {Notions} notion(s).", cours.Id, lecture.Notions.Count);

            // L'ENFANT NE CHOISIT QUE S'IL Y A UN CHOIX : plusieurs notions, et
            // un type qui fait une fiche par notion. Sinon, on rédige tout de suite.
            var type = Regles.Type(cours.Type);
            var parNotion = type?.ParNotion ?? true;
            if (parNotion && lecture.Notions.Count > 1) return null;

            var rangs = parNotion ? new List<int> { 1 } : [];
            if (!await depot.DemanderAsync(ProprietaireDe(cours), cours.Id, cours.Type, rangs, maintenant, ct)) return null;

            return await depot.GetPourTraitementAsync(cours.Id, ct);
        }

        private async Task RedigerAsync(IFichesDeCoursRepository depot, QuotaFichesService quota, CoursATraiter cours, CancellationToken ct)
        {
            var parNotion = Regles.Type(cours.Type)?.ParNotion ?? true;
            var attendues = parNotion
                ? cours.NotionsDemandees.Where(r => r >= 1 && r <= cours.Notions.Count).Distinct().ToList()
                : [0];

            if (attendues.Count == 0)
            {
                await depot.MarquerAsync(cours.Id, Regles.EtatEchec, "Aucune notion choisie.", cours.Tentatives, DateTime.UtcNow, ct);
                return;
            }

            var ratees = 0;

            foreach (var rang in attendues.Where(r => !cours.DejaRediges.Contains(r)))
            {
                ContenuFiche.Fiche? fiche = null;
                try
                {
                    fiche = await _redacteur.RedigerAsync(cours, rang, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Fiche {Type} (notion {Rang}) du cours {CoursId} impossible.", cours.Type, rang, cours.Id);
                }

                if (fiche is null)
                {
                    ratees += 1;
                    continue;
                }

                await depot.AjouterFicheAsync(
                    cours.Id, cours.Type, fiche.Titre,
                    rang == 0 ? null : rang, rang == 0 ? null : cours.Notions.Count,
                    fiche.BlocsJson, DateTime.UtcNow, ct);
            }

            var maintenant = DateTime.UtcNow;

            if (ratees == 0)
            {
                await depot.MarquerAsync(cours.Id, Regles.EtatPret, null, 0, maintenant, ct);
                await DecompterAsync(quota, cours, ct);
                return;
            }

            var tentatives = cours.Tentatives + 1;
            if (tentatives < TentativesMax)
            {
                // On réessaie au prochain tour, seulement les fiches qui manquent.
                await depot.MarquerAsync(cours.Id, Regles.EtatAGenerer, null, tentatives, maintenant, ct);
                return;
            }

            var reussies = attendues.Count - ratees;
            await depot.MarquerAsync(
                cours.Id,
                reussies > 0 ? Regles.EtatPret : Regles.EtatEchec,
                reussies > 0
                    ? $"{ratees} fiche{(ratees > 1 ? "s n’ont" : " n’a")} pas pu être créée{(ratees > 1 ? "s" : "")}. Importe ton cours à nouveau pour {(ratees > 1 ? "les" : "la")} créer."
                    : "Ta fiche n’a pas pu être créée. Importe ton cours à nouveau dans un moment.",
                tentatives, maintenant, ct);

            if (reussies > 0) await DecompterAsync(quota, cours, ct);
        }

        /// <summary>
        /// UN COURS PRÊT = UN COURS DÉCOMPTÉ, quel que soit le nombre de fiches
        /// (Camara, le 16/10/2026). Après le passage à « prêt », jamais avant :
        /// un arrêt entre les deux oublie de décompter (au bénéfice de
        /// l'enfant), il ne décompte jamais deux fois.
        /// </summary>
        private static Proprietaire ProprietaireDe(CoursATraiter cours) =>
            cours.ParentId is int parentId ? Proprietaire.Parent(parentId) : Proprietaire.Eleve(cours.EleveId ?? 0);

        private async Task DecompterAsync(QuotaFichesService quota, CoursATraiter cours, CancellationToken ct)
        {
            try
            {
                // UN COURS DE PARENT (espace parent, 16/10/2026) : sur les crédits de
                // la famille, ou rien pour un administrateur.
                if (cours.ParentId is int parentId) await quota.ImputerParentAsync(parentId, cours.Poids, cours.SansLimite, ct);
                else if (cours.EleveId is int eleveId) await quota.ImputerAsync(eleveId, cours.Poids, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Cours {CoursId} pret mais non decompte du quota.", cours.Id);
            }
        }
    }
}
