using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Notifications;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Le planificateur des courriels automatiques programmés depuis
    /// l'administration.
    ///
    /// UN TOUR TOUTES LES CINQ MINUTES, PAS UNE LONGUE ATTENTE JUSQU'À L'HEURE.
    /// Attendre d'un seul `Task.Delay` jusqu'à 10 h perdrait l'envoi à chaque
    /// redémarrage entre-temps. Ici, chaque tour regarde la dernière occurrence
    /// prévue et la compare à celle déjà prise en charge : un envoi en retard
    /// se voit au tour suivant.
    ///
    /// L'OCCURRENCE EST PRISE AVANT D'ENVOYER, par une écriture conditionnelle.
    /// Un redémarrage pendant l'envoi ne la relance pas ; les parents déjà
    /// servis sont de toute façon protégés par le journal des envois.
    ///
    /// UN RETARD DE PLUS DE SIX HEURES N'EST PAS RATTRAPÉ — Camara, le
    /// 15/09/2026 : il est noté « manqué » et l'écran le montre. Une seule
    /// occurrence est jamais considérée, la plus récente : trois jours
    /// d'arrêt ne donnent pas trois envois d'affilée.
    /// </summary>
    public class EnvoisAutomatiquesWorker : BackgroundService
    {
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EnvoisAutomatiquesWorker> _logger;

        public EnvoisAutomatiquesWorker(
            IServiceScopeFactory scopes,
            IConfiguration configuration,
            ILogger<EnvoisAutomatiquesWorker> logger)
        {
            _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            if (!_configuration.GetValue("Courrier:EnvoisAutomatiques:Actif", true))
            {
                _logger.LogInformation("Courriels automatiques desactives par configuration.");
                return;
            }

            // Laisse finir le démarrage : migrations et semis des modèles.
            try { await Task.Delay(TimeSpan.FromMinutes(3), ct); }
            catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await UnTourAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec d'un tour du planificateur de courriels automatiques.");
                }

                try { await Task.Delay(Intervalle, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task UnTourAsync(CancellationToken ct)
        {
            using var portee = _scopes.CreateScope();
            var modeles = portee.ServiceProvider.GetRequiredService<IModeleMailRepository>();
            var service = portee.ServiceProvider.GetRequiredService<IEnvoisAutomatiquesService>();

            var maintenant = DateTime.UtcNow;

            foreach (var modele in await modeles.GetAutomatiquesActifsAsync(ct))
            {
                // Les bilans ont leur propre envoi, étalé sur plusieurs jours.
                if (!CodeModeleMail.EnvoiGenerique(modele.Code)) continue;

                var occurrence = Planification.OccurrencePrecedente(maintenant, RegleEnvoi.De(modele));
                if (occurrence is null) continue;

                if (modele.DerniereOccurrence is not null && modele.DerniereOccurrence >= occurrence) continue;

                if (!await modeles.ReserverOccurrenceAsync(modele.Id, occurrence.Value, ct)) continue;

                if (maintenant - occurrence.Value > Planification.RetardMax)
                {
                    var manque = $"Envoi du {HeureFrance.Locale(occurrence.Value):dd/MM à HH:mm} manqué "
                                 + "(serveur arrêté) : il n’a pas été rattrapé.";

                    _logger.LogWarning(
                        "Courriel automatique {Code} : occurrence {Occurrence:u} manquee, ignoree.",
                        modele.Code, occurrence.Value);

                    await modeles.NoterResultatAsync(modele.Id, manque, ct);
                    continue;
                }

                var detail = await modeles.GetDetailAsync(modele.Id, ct);
                if (detail is null) continue;

                string resultat;

                try
                {
                    resultat = await service.ExecuterAsync(detail, occurrence.Value, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Courriel automatique {Code} : echec de l'envoi.", modele.Code);
                    resultat = "Échec de l’envoi : voir le journal du serveur.";
                }

                await modeles.NoterEnvoiAsync(
                    modele.Id, DateTime.UtcNow, resultat.Length > 400 ? resultat[..400] : resultat, ct);
            }
        }
    }
}
