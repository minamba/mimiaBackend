using System.Threading.Channels;

namespace SchoolWebApp.Api.Services
{
    public interface IFileObservation
    {
        /// <summary>Demande l'analyse d'une séance. Ne bloque pas l'appelant.</summary>
        void Demander(int conversationId);

        /// <summary>Consommé par le worker.</summary>
        IAsyncEnumerable<int> LireAsync(CancellationToken ct);
    }

    /// <summary>
    /// File d'attente des séances à analyser.
    ///
    /// L'élève qui clique « Quitter le cours » ne doit pas patienter le temps
    /// d'un appel au modèle : la demande est déposée ici et la réponse HTTP
    /// part immédiatement. Le worker s'en occupe dans la foulée, ce qui rend
    /// les compétences visibles dans la fiche en quelques secondes plutôt qu'à
    /// la demi-heure suivante.
    /// </summary>
    public class FileObservation : IFileObservation
    {
        /// <summary>
        /// Borne haute : au-delà, on laisse tomber les demandes plutôt que de
        /// laisser la file grossir sans fin. Le balayage périodique du worker
        /// rattrapera ces séances de toute façon — rien n'est perdu.
        /// </summary>
        private const int Capacite = 200;

        private readonly Channel<int> _canal = Channel.CreateBounded<int>(
            new BoundedChannelOptions(Capacite)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
            });

        private readonly ILogger<FileObservation> _logger;

        public FileObservation(ILogger<FileObservation> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Demander(int conversationId)
        {
            if (!_canal.Writer.TryWrite(conversationId))
            {
                _logger.LogWarning(
                    "File d'observation saturee : la seance {ConversationId} attendra le balayage periodique.",
                    conversationId);
            }
        }

        public IAsyncEnumerable<int> LireAsync(CancellationToken ct) =>
            _canal.Reader.ReadAllAsync(ct);
    }
}
