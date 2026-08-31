using System.Threading.Channels;

namespace SchoolWebApp.Api.Services
{
    public interface IFileAlertesQuota
    {
        /// <summary>
        /// Demande l'envoi de l'alerte de quota au parent de cet élève.
        /// Ne bloque pas : l'élève est au milieu d'une phrase du professeur.
        /// </summary>
        void Demander(int eleveId);

        IAsyncEnumerable<int> LireAsync(CancellationToken ct);
    }

    /// <summary>
    /// File des alertes de quota à envoyer.
    ///
    /// Le franchissement des 80 % se détecte pendant un tour de parole, au beau
    /// milieu d'une explication. Envoyer le mail sur place ajouterait le temps
    /// d'un aller-retour SMTP à la réponse du professeur — plusieurs secondes de
    /// silence pour l'enfant, à cause d'un message destiné à son parent.
    /// </summary>
    public class FileAlertesQuota : IFileAlertesQuota
    {
        /// <summary>
        /// Volontairement petite : l'alerte n'est envoyée qu'une fois par
        /// période et par famille, la file ne monte jamais haut. Si elle sature,
        /// c'est un signe de dysfonctionnement, pas une charge normale.
        /// </summary>
        private const int Capacite = 50;

        private readonly Channel<int> _canal = Channel.CreateBounded<int>(
            new BoundedChannelOptions(Capacite)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
            });

        private readonly ILogger<FileAlertesQuota> _logger;

        public FileAlertesQuota(ILogger<FileAlertesQuota> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Demander(int eleveId)
        {
            if (!_canal.Writer.TryWrite(eleveId))
            {
                // Perdue pour de bon : contrairement aux observations, aucun
                // balayage périodique ne la rattrapera. Le marqueur en base est
                // déjà posé, donc le parent ne sera pas prévenu cette
                // période-ci — d'où l'avertissement, qui doit se voir.
                _logger.LogWarning(
                    "File d'alertes quota saturee : le parent de l'eleve {EleveId} ne sera pas prevenu.",
                    eleveId);
            }
        }

        public IAsyncEnumerable<int> LireAsync(CancellationToken ct) =>
            _canal.Reader.ReadAllAsync(ct);
    }
}
