using System.Security.Claims;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Domain.Repositories;

namespace SchoolWebApp.Api.Workers
{
    /// <summary>
    /// Fait conclure les séances quittées avant l'heure, hors du chemin de
    /// la requête HTTP.
    ///
    /// « Quitter le cours » ne redonnait jamais la parole au professeur : la
    /// séance disparaissait sans compte rendu, sans fiche, sans évaluation
    /// prévue — un vrai travail sans aucune trace visible pour le parent ou
    /// l'élève. Ce worker vide la file déposée par
    /// `ChatViewModelBuilder.MarquerSortieAsync`, exactement comme
    /// <see cref="ObservationWorker"/> le fait pour l'analyse des
    /// compétences — même raison : ne pas faire attendre l'élève le temps
    /// d'un appel au modèle pour le laisser partir.
    /// </summary>
    public class ConclusionAnticipeeWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IFileConclusion _file;
        private readonly ILogger<ConclusionAnticipeeWorker> _logger;

        public ConclusionAnticipeeWorker(
            IServiceProvider services,
            IFileConclusion file,
            ILogger<ConclusionAnticipeeWorker> logger)
        {
            _services = services;
            _file = file;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var conversationId in _file.LireAsync(stoppingToken))
                {
                    try
                    {
                        using var portee = _services.CreateScope();

                        // UNE TÂCHE DE FOND N'A PAS D'IDENTITÉ, ET TOUT LE
                        // RESTE EN EXIGE UNE.
                        //
                        // `ConclureDepartAnticipeAsync` repasse par le même
                        // chemin qu'un tour de parole ordinaire, et ce chemin
                        // commence par résoudre « qui appelle » via
                        // `ICurrentUserAccessor` — qui lit les claims du
                        // HttpContext. Ici il n'y en a aucun : la résolution
                        // levait donc « le jeton ne contient pas de claim sub »,
                        // l'exception était avalée juste en dessous, et AUCUNE
                        // séance quittée en cours de route n'a jamais eu son
                        // compte rendu. Le parent ne voyait ni la séance, ni la
                        // fiche, ni l'évaluation prévue — un vrai travail sans
                        // la moindre trace.
                        //
                        // On rend donc au worker l'identité du parent
                        // propriétaire, lue en base. Le `HttpContext` posé ici
                        // vit dans un AsyncLocal : il ne concerne que ce flux
                        // d'exécution, jamais une requête d'un autre parent.
                        var conversations = portee.ServiceProvider
                            .GetRequiredService<IConversationRepository>();

                        var identityUserId = await conversations.GetIdentityUserIdAsync(
                            conversationId, stoppingToken);

                        if (string.IsNullOrWhiteSpace(identityUserId))
                        {
                            _logger.LogWarning(
                                "Conclusion de depart anticipe ignoree : conversation {ConversationId} "
                                + "sans parent identifiable.",
                                conversationId);
                            continue;
                        }

                        var accesseur = portee.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                        accesseur.HttpContext = new DefaultHttpContext
                        {
                            RequestServices = portee.ServiceProvider,
                            User = new ClaimsPrincipal(new ClaimsIdentity(
                                [new Claim("sub", identityUserId)], "conclusion-anticipee")),
                        };

                        var builder = portee.ServiceProvider.GetRequiredService<IChatViewModelBuilder>();

                        await builder.ConclureDepartAnticipeAsync(conversationId, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Une séance illisible ne doit pas arrêter la file : les
                        // suivantes ont droit à leur conclusion.
                        _logger.LogError(
                            ex, "Conclusion de depart anticipe impossible pour {ConversationId}.", conversationId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt de l'application : rien à signaler.
            }
        }
    }
}
