using Microsoft.EntityFrameworkCore;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Dal.Entities;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Tient les périodes de vacances à jour, tout seul, sans ressaisie
    /// annuelle.
    ///
    /// CE QU'IL REMPLACE
    /// ------------------
    /// Le plan initial du calendrier de l'élève posait cette donnée comme un
    /// référentiel écrit à la main, sur le modèle des programmes scolaires —
    /// et l'a délibérément laissée vide, faute de pouvoir vérifier des dates
    /// sans les inventer. Ce worker répond à la vraie objection : les dates
    /// SONT vérifiables, et à la source — le ministère les publie lui-même en
    /// open data (data.education.gouv.fr, jeu de données
    /// « fr-en-calendrier-scolaire »). Autant les lire là qu'un humain les
    /// recopie chaque été, avec le risque d'oubli que ça suppose.
    ///
    /// UPSERT, PAS SEULEMENT INSERTION.
    /// Contrairement au semis des référentiels (programmes, académies), qui
    /// n'insère que ce qui manque, celui-ci MET AUSSI À JOUR les dates d'une
    /// période déjà connue : la source corrige parfois un jour férié ou un
    /// pont après publication initiale, et une donnée resynchronisée chaque
    /// jour doit refléter la correction, pas la première version lue.
    /// </summary>
    public class CalendrierScolaireSyncWorker : BackgroundService
    {
        /// <summary>
        /// Une fois par jour : la source elle-même ne change pas plus
        /// souvent, et l'appel est gratuit et minuscule (moins de deux cents
        /// lignes de JSON).
        /// </summary>
        private static readonly TimeSpan Intervalle = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<CalendrierScolaireSyncWorker> _logger;

        public CalendrierScolaireSyncWorker(
            IServiceScopeFactory scopes, ILogger<CalendrierScolaireSyncWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Laisse l'application finir de démarrer : migrations et semis
            // des référentiels, dont les académies que ce worker suppose déjà
            // en place.
            try { await Task.Delay(TimeSpan.FromMinutes(2), ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await SynchroniserAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // La donnée déjà en base reste valable : un échec ici ne
                    // prive personne de calendrier, il reporte juste sa mise
                    // à jour au lendemain.
                    _logger.LogError(ex, "Echec de la synchronisation du calendrier scolaire.");
                }

                try { await Task.Delay(Intervalle, ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        /// <summary>
        /// L'année scolaire en cours ET la suivante : le ministère publie
        /// souvent le calendrier de l'année d'après bien avant la rentrée
        /// concernée, et la lire par avance évite d'attendre le jour J.
        /// Demander une année pas encore publiée ne coûte rien — la source
        /// renvoie simplement une liste vide pour elle.
        /// </summary>
        private static IEnumerable<string> AnneesAvSynchroniser(DateTime aujourdhuiUtc)
        {
            // La règle de bascule (1er août) vit dans `AnneeScolaire`, avec
            // le badge du site et le titre de l'administration : une seule
            // définition de « l'année en cours » pour tout le produit.
            yield return Domain.Models.AnneeScolaire.Courante(aujourdhuiUtc);
            yield return Domain.Models.AnneeScolaire.Suivante(aujourdhuiUtc);
        }

        private async Task SynchroniserAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ICalendrierScolaireApiService>();
            var db = scope.ServiceProvider.GetRequiredService<SchoolWebAppDatabaseContext>();

            var inserees = 0;
            var misesAJour = 0;

            foreach (var annee in AnneesAvSynchroniser(DateTime.UtcNow))
            {
                var brutes = await api.RecupererAsync(annee, ct);
                if (brutes.Count == 0) continue;

                var existantes = await db.PeriodesVacances
                    .Where(p => p.AnneeScolaire == annee)
                    .ToListAsync(ct);

                var parCle = existantes.ToDictionary(p => (p.Zone, p.Libelle));

                foreach (var brute in brutes)
                {
                    if (parCle.TryGetValue((brute.Zone, brute.Libelle), out var existante))
                    {
                        if (existante.DateDebut != brute.Debut || existante.DateFin != brute.Fin)
                        {
                            existante.DateDebut = brute.Debut;
                            existante.DateFin = brute.Fin;
                            misesAJour++;
                        }

                        continue;
                    }

                    db.PeriodesVacances.Add(new PeriodeVacances
                    {
                        Zone = brute.Zone,
                        AnneeScolaire = annee,
                        Libelle = brute.Libelle,
                        DateDebut = brute.Debut,
                        DateFin = brute.Fin,
                    });

                    inserees++;
                }
            }

            if (inserees > 0 || misesAJour > 0)
            {
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Calendrier scolaire officiel synchronise : {Inserees} periode(s) ajoutee(s), "
                    + "{MisesAJour} corrigee(s).",
                    inserees, misesAJour);
            }
        }
    }
}
