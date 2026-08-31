using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Analyse les séances terminées et alimente la mémoire longue de l'élève.
    ///
    /// Volontairement hors du chemin de la conversation. Observer pendant la
    /// séance ajouterait une latence à chaque tour, dans un produit où l'enfant
    /// attend la voix de son professeur. Et une observation faite à chaud sur un
    /// seul échange conclurait sur trop peu.
    /// </summary>
    public class ObservationWorker : BackgroundService
    {
        /// <summary>Fréquence de passage.</summary>
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(10);

        /// <summary>Silence au-delà duquel on considère la séance terminée.</summary>
        private static readonly TimeSpan Inactivite = TimeSpan.FromMinutes(25);

        /// <summary>Séances traitées par passage : borne la facture d'un rattrapage.</summary>
        private const int ParPassage = 20;

        private readonly IServiceProvider _services;
        private readonly IFileObservation _file;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ObservationWorker> _logger;

        public ObservationWorker(
            IServiceProvider services,
            IFileObservation file,
            IConfiguration configuration,
            ILogger<ObservationWorker> logger)
        {
            _services = services;
            _file = file;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Observation:Actif", true))
            {
                _logger.LogInformation("Observation des competences desactivee par configuration.");
                return;
            }

            // Deux sources en parallèle. La file traite les séances que l'élève
            // vient de quitter — visible dans la fiche en quelques secondes.
            // Le balayage rattrape celles qu'il a abandonnées en fermant
            // l'onglet, où aucun signal n'est jamais parvenu.
            await Task.WhenAll(
                BoucleFileAsync(stoppingToken),
                BouclePeriodiqueAsync(stoppingToken));
        }

        private async Task BoucleFileAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var conversationId in _file.LireAsync(ct))
                {
                    try
                    {
                        using var portee = _services.CreateScope();
                        var observateur = portee.ServiceProvider
                            .GetRequiredService<IObservateurCompetencesService>();

                        var appliquees = await observateur.ObserverSeanceAsync(conversationId, ct);

                        _logger.LogInformation(
                            "Seance {ConversationId} analysee a la sortie du cours : {Total} competence(s).",
                            conversationId, appliquees);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Une séance illisible ne doit pas arrêter la file.
                        _logger.LogError(ex, "Observation immediate impossible pour {ConversationId}.", conversationId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt de l'application : rien à signaler.
            }
        }

        private async Task BouclePeriodiqueAsync(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await UnPassageAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echec du passage d'observation.");
                }

                try
                {
                    await Task.Delay(Intervalle, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task UnPassageAsync(CancellationToken ct)
        {
            using var portee = _services.CreateScope();

            var conversations = portee.ServiceProvider.GetRequiredService<IConversationRepository>();
            var observateur = portee.ServiceProvider.GetRequiredService<IObservateurCompetencesService>();

            var seances = (await conversations.GetSeancesAObserverAsync(Inactivite, ParPassage, ct)).ToList();
            if (seances.Count == 0) return;

            var total = 0;

            foreach (var seance in seances)
            {
                ct.ThrowIfCancellationRequested();

                // Séance trop courte : on la laisse en attente plutôt que de la
                // clore. L'élève peut la reprendre plus tard, et elle deviendra
                // alors assez longue pour être lue.
                if (!observateur.Exploitable(seance)) continue;

                try
                {
                    total += await observateur.ObserverAsync(seance, ct);
                }
                catch (Exception ex)
                {
                    // Une séance illisible ne doit pas bloquer les suivantes.
                    _logger.LogError(ex, "Observation impossible pour la seance {SeanceId}.", seance.ConversationId);
                }

                // Le marqueur avance même sans observation retenue : sinon une
                // séance dont on ne peut rien tirer serait réanalysée à chaque
                // passage, indéfiniment et à chaque fois facturée.
                await conversations.MarquerObserveeAsync(seance.ConversationId, seance.Jusqua, ct);
            }

            _logger.LogInformation(
                "Observation : {Seances} seance(s) analysee(s), {Total} competence(s) mise(s) a jour.",
                seances.Count, total);
        }
    }
}
