using System.Collections.Concurrent;
using System.Threading.Channels;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Services
{
    /// <summary>
    /// L'ANNONCE DES CHANGEMENTS DE RÉGLAGE, EN MÉMOIRE — voir
    /// <see cref="IDiffusionReglages"/> pour le pourquoi.
    ///
    /// Jusqu'ici, un navigateur lisait les drapeaux UNE FOIS par session : le
    /// style ne changeait qu'au rechargement de la page. Seul le bandeau était
    /// relu, toutes les deux minutes.
    ///
    /// POURQUOI SSE ET NON WEBSOCKET. La diffusion est à sens unique — le
    /// serveur annonce, le navigateur écoute, il n'a rien à répondre. Un
    /// WebSocket apporterait une liaison bidirectionnelle dont personne ne se
    /// sert, un protocole de plus à faire passer par le proxy, et pas de
    /// reconnexion automatique. `EventSource` se reconnecte seul, passe partout
    /// en HTTP, et l'API sait déjà écrire du SSE (voir `ConversationsController`).
    ///
    /// UN CANAL PAR NAVIGATEUR, BORNÉ ET QUI JETTE LE PLUS VIEUX. Ce qui voyage
    /// n'est pas une donnée mais un signal « quelque chose a changé, relis » :
    /// perdre le premier de deux signaux rapprochés ne coûte rien, le second
    /// dit la même chose. Un canal illimité, lui, ferait grossir la mémoire
    /// derrière un onglet endormi.
    ///
    /// EN MÉMOIRE, DONC PAR PROCESSUS. Le jour où l'API tournerait sur deux
    /// machines, un réglage changé sur l'une n'atteindrait que ses propres
    /// navigateurs — il faudrait alors passer par un relais commun. Aujourd'hui
    /// l'API est seule sur son VPS, et la relecture périodique reste le filet
    /// pour tout le monde.
    /// </summary>
    public class DiffusionReglages : IDiffusionReglages
    {
        /// <summary>
        /// Plafond d'écoutes simultanées. Un garde-fou, pas une limite d'usage :
        /// chaque écoute ne coûte qu'un petit canal, mais rien ne doit pouvoir
        /// faire grossir cette liste sans fin — un robot qui ouvrirait des
        /// connexions en boucle, par exemple. Au-delà, le navigateur retombe
        /// sur sa relecture périodique : il n'est jamais bloqué, juste moins
        /// rapide.
        /// </summary>
        private const int AbonnesMax = 2_000;

        private readonly ConcurrentDictionary<Guid, Channel<string>> _canaux = new();
        private readonly ILogger<DiffusionReglages> _logger;

        /// <summary>
        /// ALLUMÉE PAR DÉFAUT, puis fixée au démarrage d'après le réglage en
        /// base. `volatile` parce qu'elle est écrite par la requête de
        /// l'administrateur et lue par toutes les écoutes en cours, sur d'autres
        /// fils d'exécution.
        /// </summary>
        private volatile bool _actif = true;

        public DiffusionReglages(ILogger<DiffusionReglages> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Abonnes => _canaux.Count;

        public bool Actif => _actif;

        public void DefinirActif(bool actif)
        {
            if (_actif == actif) return;

            // L'ÉTAT D'ABORD, LE VIDAGE ENSUITE, et l'ordre compte : une écoute
            // qui arriverait entre les deux serait retirée de la liste sans
            // avoir été prévenue, et resterait suspendue jusqu'à ce que son
            // navigateur raccroche. `Abonner` refuse dès que le drapeau tombe.
            _actif = actif;

            if (actif)
            {
                _logger.LogWarning("Diffusion des reglages RALLUMEE.");
                return;
            }

            var ouvertes = _canaux.Count;

            // Compléter le canal fait sortir la boucle de l'écoute, qui se
            // désabonne elle-même dans son `finally` et referme sa connexion.
            foreach (var canal in _canaux.Values) canal.Writer.TryComplete();
            _canaux.Clear();

            _logger.LogWarning(
                "Diffusion des reglages ETEINTE : {Nombre} ecoute(s) fermee(s).", ouvertes);
        }

        public (Guid Id, ChannelReader<string> Lecteur) Abonner()
        {
            var canal = Channel.CreateBounded<string>(new BoundedChannelOptions(8)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

            // ÉTEINTE : on refuse net, exactement comme au-delà du plafond. Le
            // navigateur reçoit une erreur franche et retombe sur sa relecture
            // périodique, sans revenir toutes les trois secondes.
            if (!_actif)
            {
                canal.Writer.TryComplete();
                return (Guid.Empty, canal.Reader);
            }

            if (_canaux.Count >= AbonnesMax)
            {
                _logger.LogWarning(
                    "Diffusion des reglages : {Max} ecoutes deja ouvertes, celle-ci se fermera aussitot.",
                    AbonnesMax);

                canal.Writer.TryComplete();
                return (Guid.Empty, canal.Reader);
            }

            var id = Guid.NewGuid();
            _canaux[id] = canal;

            return (id, canal.Reader);
        }

        public void Desabonner(Guid id)
        {
            if (_canaux.TryRemove(id, out var canal)) canal.Writer.TryComplete();
        }

        public void Diffuser(string cle)
        {
            if (string.IsNullOrWhiteSpace(cle) || _canaux.IsEmpty) return;

            foreach (var canal in _canaux.Values)
            {
                // `TryWrite` et jamais d'attente : la diffusion se fait dans le
                // fil de celui qui écrit le réglage — l'administrateur attend
                // sa réponse HTTP, il n'a pas à attendre après un navigateur
                // lent à l'autre bout du monde.
                canal.Writer.TryWrite(cle);
            }

            _logger.LogInformation(
                "Reglage {Cle} annonce a {Nombre} navigateur(s).", cle, _canaux.Count);
        }
    }
}
