using System.Threading.Channels;

namespace SchoolWebApp.Api.Services
{
    public interface IFileConclusion
    {
        /// <summary>Demande la conclusion d'une séance quittée avant l'heure. Ne bloque pas l'appelant.</summary>
        void Demander(int conversationId);

        /// <summary>Consommé par le worker.</summary>
        IAsyncEnumerable<int> LireAsync(CancellationToken ct);
    }

    /// <summary>
    /// File d'attente des séances à conclure après un départ anticipé.
    ///
    /// Même raison d'être que <see cref="FileObservation"/> : l'élève qui
    /// clique « Quitter le cours » ne doit pas attendre le temps d'un appel
    /// au modèle pour que la page le laisse partir. La demande est déposée
    /// ici, et le worker s'en charge dans la foulée, hors du chemin de la
    /// requête HTTP.
    /// </summary>
    public class FileConclusion : IFileConclusion
    {
        /// <summary>
        /// Borne haute : au-delà, on laisse tomber les demandes plutôt que de
        /// laisser la file grossir sans fin. Une séance perdue ici reste
        /// visible dans son historique de messages — seul le compte rendu
        /// manquera, pas le travail lui-même.
        /// </summary>
        private const int Capacite = 200;

        private readonly Channel<int> _canal = Channel.CreateBounded<int>(
            new BoundedChannelOptions(Capacite)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
            });

        private readonly ILogger<FileConclusion> _logger;

        public FileConclusion(ILogger<FileConclusion> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Demander(int conversationId)
        {
            if (!_canal.Writer.TryWrite(conversationId))
            {
                _logger.LogWarning(
                    "File de conclusion saturee : la seance {ConversationId} restera sans compte rendu.",
                    conversationId);
            }
        }

        public IAsyncEnumerable<int> LireAsync(CancellationToken ct) =>
            _canal.Reader.ReadAllAsync(ct);
    }
}
