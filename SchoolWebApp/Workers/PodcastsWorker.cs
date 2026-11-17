using System.Text.Json;
using SchoolWebApp.Api.Services.MesFiches;
using SchoolWebApp.Api.Services.Podcasts;
using SchoolWebApp.Api.Services.Voix;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using Regles = SchoolWebApp.Domain.Models.FichesRevisionEleve;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>Réveille la tâche des podcasts dès qu'un cours est envoyé ou que des épisodes sont demandés.</summary>
    public sealed class ReveilPodcasts : Sonnette
    {
    }

    /// <summary>
    /// LIRE LES COURS IMPORTÉS ET EN FAIRE DES PODCASTS — Camara, le 15/11/2026.
    ///
    /// Le même chemin que les fiches (`FichesDeCoursWorker`), dans une portée qui ne voit QUE les podcasts
    /// (`IFichesDeCoursRepository.Nature`) :
    /// 1. À LIRE : la même lecture que les fiches trouve le titre et les notions (douze au plus). Une seule notion :
    ///    l'épisode part tout de suite ; sinon l'enfant choisit ses notions.
    /// 2. À GÉNÉRER : un ÉPISODE par notion choisie. Pour chacun : le texte (Claude, sur le cache des pages), puis la
    ///    voix du professeur de la matière, paragraphe par paragraphe (`ISyntheseVocaleService.CopierAudioAsync`, avec sa
    ///    garde contre les relectures), assemblée, compressée en Opus et rangée sur le disque. Chaque épisode est
    ///    enregistré dès qu'il est prêt : l'enfant les voit arriver.
    ///
    /// Comme les fiches : un épisode raté ne bloque pas les autres, trois passages au plus, et le cours n'est décompté
    /// du quota qu'une fois prêt.
    /// </summary>
    public class PodcastsWorker : BackgroundService
    {
        private const int TailleLot = 3;
        private const int TentativesMax = 3;
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(5);

        /// <summary>Les paragraphes d'un épisode se disent deux à la fois : plus vite, sans saturer le fournisseur.</summary>
        private const int VoixEnParallele = 2;

        /// <summary>Le ménage du disque : une fois par jour suffit.</summary>
        private static readonly TimeSpan IntervalleMenage = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopes;
        private readonly ReveilPodcasts _reveil;
        private readonly ProgressionPodcasts _progression;
        private readonly ILogger<PodcastsWorker> _logger;
        private DateTime _dernierMenage = DateTime.MinValue;

        public PodcastsWorker(
            IServiceScopeFactory scopes, ReveilPodcasts reveil, ProgressionPodcasts progression, ILogger<PodcastsWorker> logger)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
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
                    _logger.LogError(ex, "Echec du tour des podcasts.");
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

            // TOUTE CETTE PORTÉE NE VOIT QUE LES PODCASTS : dépôt, quota et crédits partagent le même contexte.
            depot.Nature = NaturesCours.Podcasts;

            var reglages = services.GetRequiredService<IReglageRepository>();
            var quota = services.GetRequiredService<QuotaFichesService>();

            var purges = await depot.PurgerAsync(DateTime.UtcNow, ct);
            if (purges > 0) _logger.LogInformation("Podcasts : {Nombre} brouillon(s) ou cours purge(s).", purges);

            await MenageDuDisqueAsync(depot, services.GetRequiredService<IStockagePodcasts>(), ct);

            if (!await reglages.EstActifAsync(PodcastsEleve.Reglage, PodcastsEleve.ActifParDefaut, ct)
                || !await reglages.EstActifAsync(Controllers.ReglagesController.TachesDeFond, true, ct))
            {
                return;
            }

            foreach (var id in await depot.GetATraiterAsync(TailleLot, ct))
            {
                await TraiterAsync(services, depot, quota, id, ct);
            }

            if ((await depot.GetATraiterAsync(1, ct)).Count > 0) _reveil.Sonner();
        }

        /// <summary>
        /// LE DISQUE SUIT LA BASE, une fois par jour : le dossier d'un cours supprimé (par l'enfant, avec l'élève, avec
        /// le compte) part. Aucune suppression de la base n'a besoin de penser au disque.
        /// </summary>
        private async Task MenageDuDisqueAsync(IFichesDeCoursRepository depot, IStockagePodcasts stockage, CancellationToken ct)
        {
            if (DateTime.UtcNow - _dernierMenage < IntervalleMenage) return;
            _dernierMenage = DateTime.UtcNow;

            var dossiers = stockage.Dossiers();
            if (dossiers.Count == 0) return;

            var existants = await depot.CoursExistantsAsync(dossiers.Select(d => d.CoursId).Distinct().ToList(), ct);
            var orphelins = dossiers.Where(d => !existants.Contains(d.CoursId)).ToList();
            foreach (var (eleveId, coursId) in orphelins) stockage.SupprimerCours(eleveId, coursId);

            if (orphelins.Count > 0) _logger.LogInformation("Podcasts : {Nombre} dossier(s) audio orphelin(s) efface(s).", orphelins.Count);
        }

        private async Task TraiterAsync(
            IServiceProvider services, IFichesDeCoursRepository depot, QuotaFichesService quota, int id, CancellationToken ct)
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
                cours = await LireAsync(services, depot, cours, ct);
                if (cours is null || cours.Etat != Regles.EtatAGenerer) return;
            }

            if (cours.Etat == Regles.EtatAGenerer) await GenererAsync(services, depot, quota, cours, ct);
        }

        private async Task<CoursATraiter?> LireAsync(
            IServiceProvider services, IFichesDeCoursRepository depot, CoursATraiter cours, CancellationToken ct)
        {
            var redacteur = services.GetRequiredService<RedacteurFichesService>();
            LectureDuCours lecture;
            try
            {
                lecture = await redacteur.LireAsync(
                    cours, ct, PodcastsEleve.Limites(cours.SansLimite).NotionsMax, "podcasts-lecture");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Lecture du cours de podcast {CoursId} impossible.", cours.Id);
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
            _logger.LogInformation("Cours de podcast {CoursId} lu : {Notions} notion(s).", cours.Id, lecture.Notions.Count);

            // L'ENFANT NE CHOISIT QUE S'IL Y A UN CHOIX : une seule notion, l'épisode part tout de suite.
            if (lecture.Notions.Count > 1) return null;

            var proprietaire = cours.ParentId is int parentId ? Proprietaire.Parent(parentId) : Proprietaire.Eleve(cours.EleveId ?? 0);
            if (!await depot.DemanderAsync(proprietaire, cours.Id, PodcastsEleve.Type, [1], maintenant, ct)) return null;

            return await depot.GetPourTraitementAsync(cours.Id, ct);
        }

        private async Task GenererAsync(
            IServiceProvider services, IFichesDeCoursRepository depot, QuotaFichesService quota, CoursATraiter cours, CancellationToken ct)
        {
            var attendus = cours.NotionsDemandees.Where(r => r >= 1 && r <= cours.Notions.Count).Distinct().ToList();
            if (attendus.Count == 0)
            {
                await depot.MarquerAsync(cours.Id, Regles.EtatEchec, "Aucune notion choisie.", cours.Tentatives, DateTime.UtcNow, ct);
                return;
            }

            var ratees = 0;
            // LA BARRE DE L'ÉCRAN (15/11/2026) : les épisodes déjà faits comptent d'emblée.
            _progression.Commencer(cours.Id, attendus.Count, attendus.Count(r => cours.DejaRediges.Contains(r)));
            try
            {
                foreach (var rang in attendus.Where(r => !cours.DejaRediges.Contains(r)))
                {
                    var episode = await EpisodeAsync(services, cours, rang, ct);
                    if (episode is null)
                    {
                        ratees += 1;
                        _progression.EpisodeFini(cours.Id);
                        continue;
                    }

                    await depot.AjouterFicheAsync(
                        cours.Id, PodcastsEleve.Type, episode.Value.Titre, rang, cours.Notions.Count,
                        episode.Value.Contenu, DateTime.UtcNow, ct);
                    _progression.EpisodeFini(cours.Id);
                }
            }
            finally
            {
                _progression.Terminer(cours.Id);
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
                await depot.MarquerAsync(cours.Id, Regles.EtatAGenerer, null, tentatives, maintenant, ct);
                return;
            }

            var reussis = attendus.Count - ratees;
            await depot.MarquerAsync(
                cours.Id,
                reussis > 0 ? Regles.EtatPret : Regles.EtatEchec,
                reussis > 0
                    ? $"{ratees} épisode{(ratees > 1 ? "s n’ont" : " n’a")} pas pu être créé{(ratees > 1 ? "s" : "")}. Importe ton cours à nouveau pour {(ratees > 1 ? "les" : "le")} créer."
                    : "Ton podcast n’a pas pu être créé. Importe ton cours à nouveau dans un moment.",
                tentatives, maintenant, ct);

            if (reussis > 0) await DecompterAsync(quota, cours, ct);
        }

        /// <summary>Un épisode : son texte, sa voix, son fichier. Null s'il a échoué (rien n'est gardé à moitié).</summary>
        private async Task<(string Titre, string Contenu)?> EpisodeAsync(
            IServiceProvider services, CoursATraiter cours, int rang, CancellationToken ct)
        {
            var redacteur = services.GetRequiredService<RedacteurPodcastService>();
            var voix = services.GetRequiredService<ISyntheseVocaleService>();
            var stockage = services.GetRequiredService<IStockagePodcasts>();

            TexteEpisode? texte;
            try
            {
                texte = await redacteur.EcrireAsync(cours, rang, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Texte de l'episode {Rang} du cours {CoursId} impossible.", rang, cours.Id);
                return null;
            }

            if (texte is null)
            {
                _logger.LogWarning("Texte de l'episode {Rang} du cours {CoursId} illisible.", rang, cours.Id);
                return null;
            }

            _progression.Avancer(cours.Id, ProgressionPodcasts.PartTexte);

            // LA VOIX, PARAGRAPHE PAR PARAGRAPHE, deux à la fois ; l'ordre est remis à l'assemblage.
            var avatar = cours.ProfAvatar;
            var dits = 0;
            // Un parent n'a pas d'âge en base : une voix pour un adulte.
            var age = cours.Age > 0 ? cours.Age : cours.Rang == Regles.RangAdulte ? 30 : 11;
            var morceaux = new byte[texte.Paragraphes.Count][];
            using var limite = new SemaphoreSlim(VoixEnParallele);

            try
            {
                await Task.WhenAll(texte.Paragraphes.Select(async (paragraphe, i) =>
                {
                    await limite.WaitAsync(ct);
                    try
                    {
                        morceaux[i] = await DireAsync(voix, paragraphe, avatar, age, ct);
                        var fait = Interlocked.Increment(ref dits);
                        _progression.Avancer(
                            cours.Id,
                            ProgressionPodcasts.PartTexte + (ProgressionPodcasts.PartVoix * fait / texte.Paragraphes.Count));
                    }
                    finally
                    {
                        limite.Release();
                    }
                }));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Voix de l'episode {Rang} du cours {CoursId} impossible.", rang, cours.Id);
                return null;
            }

            if (morceaux.Any(m => m is null || m.Length == 0)) return null;

            // L'ASSEMBLAGE : un souffle au début, une courte pause entre deux paragraphes.
            // LES REPÈRES (15/11/2026) : où commence chaque paragraphe, et chacune de ses phrases, dans le temps de
            // l'épisode — le texte de l'écran suit alors la voix (`ReperesPodcast`).
            using var pcm = new MemoryStream();
            var reperes = new List<ReperesParagraphe>(morceaux.Length);
            pcm.Write(AudioPodcast.Silence(0.3));
            for (var i = 0; i < morceaux.Length; i += 1)
            {
                if (i > 0) pcm.Write(AudioPodcast.Silence(0.55));
                reperes.Add(ReperesPodcast.Paragraphe(
                    texte.Paragraphes[i],
                    System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>(morceaux[i]),
                    pcm.Length / 2d / AudioPodcast.Frequence));
                pcm.Write(morceaux[i]);
            }

            pcm.Write(AudioPodcast.Silence(0.6));

            var brut = pcm.ToArray();
            var opus = AudioPodcast.EncoderOpus(brut);
            var chemin = await stockage.EcrireAsync(cours.EleveId ?? 0, cours.Id, opus, ct);
            if (chemin is null) return null;

            var duree = AudioPodcast.Duree(brut.LongLength);
            _logger.LogInformation(
                "Episode {Rang} du cours {CoursId} pret : {Paragraphes} paragraphe(s), {Duree} s, {Taille} Ko.",
                rang, cours.Id, texte.Paragraphes.Count, duree, opus.Length / 1024);

            var contenu = JsonSerializer.Serialize(new ContenuEpisode(texte.Paragraphes, chemin, duree, avatar, cours.ProfPrenom, reperes));
            return (texte.Titre, contenu);
        }

        /// <summary>Un paragraphe dit par la voix du professeur ; une seconde chance s'il revient vide.</summary>
        private static async Task<byte[]> DireAsync(ISyntheseVocaleService voix, string paragraphe, string? avatar, int age, CancellationToken ct)
        {
            for (var essai = 0; essai < 2; essai += 1)
            {
                using var flux = new MemoryStream();
                await voix.CopierAudioAsync(paragraphe, avatar, age, flux, ct: ct, origine: PodcastsEleve.OrigineVoix);
                if (flux.Length > 0) return flux.ToArray();
            }

            return [];
        }

        private async Task DecompterAsync(QuotaFichesService quota, CoursATraiter cours, CancellationToken ct)
        {
            try
            {
                if (cours.ParentId is int parentId) await quota.ImputerParentAsync(parentId, cours.Poids, cours.SansLimite, ct);
                else if (cours.EleveId is int eleveId) await quota.ImputerAsync(eleveId, cours.Poids, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Cours de podcast {CoursId} pret mais non decompte du quota.", cours.Id);
            }
        }
    }

    /// <summary>Ce qu'un épisode garde en base (colonne `contenu` de `FicheDeCours`) : son texte et son audio.</summary>
    /// <remarks>`Reperes` (15/11/2026) : un par paragraphe ; absent des épisodes faits avant, l'écran estime alors.</remarks>
    public sealed record ContenuEpisode(
        IReadOnlyList<string> Paragraphes, string Audio, int Duree, string? Voix, string? Prof,
        IReadOnlyList<ReperesParagraphe>? Reperes = null);
}
