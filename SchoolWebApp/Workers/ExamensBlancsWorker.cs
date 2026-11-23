using System.Collections.Concurrent;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// EXAMENS BLANCS, ÉTAPE 2 — la lecture des documents puis leur rangement en dossiers, en tâche de fond
    /// (voir <see cref="LecteurExamensBlancs"/>). Toutes les 20 secondes : un lot de documents à lire, puis les
    /// matières dont les dossiers sont à refaire (documents tous lus, plus aucun envoi depuis une minute).
    ///
    /// CE QUI NE DOIT JAMAIS TOURNER EN BOUCLE SUR UN ÉCHEC (la leçon des planches, qui avait brûlé le crédit) :
    ///   - un document qui ne se lit pas compte ses échecs en base, et s'arrête au troisième (marqué illisible) ;
    ///   - un rangement qui ne rend rien d'exploitable est retenté au plus trois fois, avec une pause croissante ;
    ///     au troisième échec, la matière est marquée rangée sans dossier (l'écran le dit, l'enfant peut renvoyer).
    /// Les tâches de fond éteintes par l'administration arrêtent tout, comme la lecture des documents de contrôle.
    /// </summary>
    public class ExamensBlancsWorker : BackgroundService
    {
        private static readonly TimeSpan Pause = TimeSpan.FromSeconds(20);
        private const int LotLecture = 5;
        private const int LotRangement = 3;
        private const int RangementsMax = 3;

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ExamensBlancsWorker> _logger;

        /// <summary>Les rangements ratés, par matière : (nombre, prochain essai permis).</summary>
        private readonly ConcurrentDictionary<int, (int Echecs, DateTime Prochain)> _echecs = new();

        public ExamensBlancsWorker(IServiceScopeFactory scopes, ILogger<ExamensBlancsWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(45), ct);

                while (!ct.IsCancellationRequested)
                {
                    await TourAsync(ct);
                    await Task.Delay(Pause, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
        }

        private async Task TourAsync(CancellationToken ct)
        {
            try
            {
                using var portee = _scopes.CreateScope();
                var services = portee.ServiceProvider;

                var reglages = services.GetRequiredService<IReglageRepository>();
                if (!await reglages.EstActifAsync(Controllers.ReglagesController.TachesDeFond, true, ct)) return;

                var lecteur = services.GetRequiredService<LecteurExamensBlancs>();
                await lecteur.LireAsync(LotLecture, ct);

                var examens = services.GetRequiredService<IExamenBlancRepository>();
                var maintenant = DateTime.UtcNow;
                var aRanger = await examens.GetMatieresANotionnerAsync(
                    maintenant - ExamensBlancs.AttenteAvantRangement, LotRangement * 3, ct);

                var faits = 0;
                foreach (var matiere in aRanger)
                {
                    if (faits >= LotRangement) break;
                    if (_echecs.TryGetValue(matiere.LigneId, out var e) && e.Prochain > maintenant) continue;

                    faits++;
                    bool reussi;
                    try
                    {
                        reussi = await lecteur.RangerAsync(matiere, ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Rangement de la matiere d'examen blanc {LigneId} rate.", matiere.LigneId);
                        reussi = false;
                    }

                    if (reussi)
                    {
                        _echecs.TryRemove(matiere.LigneId, out _);
                        continue;
                    }

                    var echecs = (_echecs.TryGetValue(matiere.LigneId, out var avant) ? avant.Echecs : 0) + 1;
                    if (echecs >= RangementsMax)
                    {
                        _logger.LogError(
                            "Rangement de la matiere d'examen blanc {LigneId} ABANDONNE apres {Echecs} essais : aucun dossier.",
                            matiere.LigneId, echecs);
                        await examens.PoserNotionsAsync(matiere.LigneId, [], DateTime.UtcNow, ct);
                        _echecs.TryRemove(matiere.LigneId, out _);
                    }
                    else
                    {
                        _echecs[matiere.LigneId] = (echecs, maintenant.AddMinutes(2 * echecs));
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tour de lecture des examens blancs interrompu.");
            }
        }
    }
}
