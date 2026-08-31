using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Services.Voix;

namespace SchoolWebApp.Api.Controllers
{
    /// <summary>
    /// L'écoute de l'élève, en flux.
    ///
    /// Le navigateur ouvre une connexion WebSocket, y déverse son micro en PCM
    /// 16 bits, et reçoit les transcriptions au fil de l'eau. Le serveur fait le
    /// relais vers le fournisseur : la clé ne descend jamais dans le navigateur,
    /// et les minutes d'audio se comptent au passage.
    ///
    /// Pourquoi pas le Web Speech du navigateur, qui est gratuit : il n'existe
    /// pas sur Firefox, et sa détection de fin de tour attend un silence long
    /// et non réglable — c'est lui qui donnait l'impression de parler à une
    /// machine qui attend son tour.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/ecoute")]
    public class EcouteController : ControllerBase
    {
        /// <summary>
        /// Taille du tampon de réception. Le navigateur envoie des blocs de
        /// quelques kilo-octets ; au-delà, on lit en plusieurs fois.
        /// </summary>
        private const int TailleTampon = 16 * 1024;

        /// <summary>
        /// Profondeur de la file entre la réception et l'émission vers le
        /// fournisseur. Bornée : si la liaison sortante ralentit, on préfère
        /// perdre du son ancien plutôt que de gonfler la mémoire du serveur.
        /// </summary>
        private const int ProfondeurFile = 64;

        private readonly ITranscriptionTempsReelService _transcription;
        private readonly IChatViewModelBuilder _chatBuilder;
        private readonly ILogger<EcouteController> _logger;

        public EcouteController(
            ITranscriptionTempsReelService transcription,
            IChatViewModelBuilder chatBuilder,
            ILogger<EcouteController> logger)
        {
            _transcription = transcription ?? throw new ArgumentNullException(nameof(transcription));
            _chatBuilder = chatBuilder ?? throw new ArgumentNullException(nameof(chatBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Ouvre l'écoute d'une séance. WebSocket uniquement.</summary>
        [HttpGet("{conversationId:int}")]
        public async Task Ecouter(int conversationId)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // La conversation appartient-elle bien au compte du jeton ? Sans ce
            // contrôle, n'importe quel parent authentifié écouterait la séance
            // d'un autre enfant en changeant un chiffre dans l'URL.
            var verdict = await _chatBuilder.VerifierQuotaAsync(conversationId, HttpContext.RequestAborted);
            if (!verdict.Autorise)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // LA MATIÈRE ORIENTE LE VOCABULAIRE DU TRANSCRIPTEUR.
            //
            // Lue APRÈS le contrôle d'appartenance : la question ne se pose
            // que si la séance est bien celle de cet élève.
            //
            // Un échec ne ferme pas l'écoute — on transcrit alors avec le seul
            // socle commun. Une pondération faible vaut mieux qu'un cours muet.
            var matiereCode = await _chatBuilder.GetMatiereCodeAsync(conversationId);

            using var navigateur = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var ct = HttpContext.RequestAborted;

            // Le son transite par une file : la lecture du navigateur et
            // l'émission vers le fournisseur avancent chacune à son rythme.
            var file = Channel.CreateBounded<BlocAudio>(
                new BoundedChannelOptions(ProfondeurFile)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = true,
                });

            // LA LECTURE DU NAVIGATEUR DOIT POUVOIR ÊTRE COUPÉE D'ICI.
            //
            // Elle est parquée dans un ReceiveAsync qui n'attend que l'élève.
            // Quand c'est le RELAIS qui s'arrête, personne ne la réveille :
            // elle tient la liaison ouverte, le navigateur croit son micro
            // vivant, et le cours se termine en silence. D'où ce jeton, qu'on
            // déclenche nous-mêmes à la fin.
            using var fermeture = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var reception = RecevoirAsync(navigateur, file.Writer, fermeture.Token);

            try
            {
                // Les secondes d'audio sont JOURNALISÉES, pas facturées au
                // parent. Le quota qu'il achète est en minutes de COURS, et
                // l'audio est un sous-ensemble de ce temps : le décompter à
                // part reviendrait à lui faire payer deux fois la même minute.
                // Ce chiffre sert à comparer le coût réel au modèle — et c'est
                // `MinutesAudioMax` qui borne les dérives, pas le quota.
                await _transcription.RelayerAsync(
                    file.Reader.ReadAllAsync(ct),
                    fragment => EmettreAsync(navigateur, fragment, ct),
                    secondes =>
                    {
                        _logger.LogInformation(
                            "Audio transcrit : {Secondes:0} s (conversation {ConversationId}).",
                            secondes, conversationId);
                        return Task.CompletedTask;
                    },
                    matiereCode,
                    ct);
            }
            catch (OperationCanceledException)
            {
                // L'élève a fermé l'onglet : rien à signaler.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la transcription pour la conversation {ConversationId}.", conversationId);

                await EmettreAsync(
                    navigateur,
                    new FragmentTranscription("", false),
                    CancellationToken.None,
                    erreur: "La transcription est indisponible.");
            }

            file.Writer.TryComplete();

            // ON ANNONCE LA FERMETURE AVANT D'ATTENDRE, PAS APRÈS.
            //
            // C'est cette trame qui apprend au navigateur que son oreille est
            // morte — sans elle il continuerait d'y déverser du son, l'onde
            // allumée, jusqu'à la fin du cours. Et c'est aussi elle qui
            // débloque la réception : le client répond par sa propre trame de
            // fermeture, que le ReceiveAsync en attente reçoit enfin.
            //
            // CloseOutputAsync et non CloseAsync : le second attend la réponse
            // du client, et attendre une réponse pendant qu'une réception est
            // déjà en cours sur la même socket bloque les deux.
            if (navigateur.State == WebSocketState.Open)
            {
                await navigateur.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure, "fin d'écoute", CancellationToken.None);
            }

            // Un client qui ne répond pas ne doit pas retenir la requête pour
            // autant. Passé ce délai, on coupe la réception par le jeton.
            fermeture.CancelAfter(TimeSpan.FromSeconds(5));

            try { await reception; }
            catch (OperationCanceledException) { }
        }

        /// <summary>Lit l'audio du navigateur et le pousse dans la file.</summary>
        private static async Task RecevoirAsync(
            WebSocket navigateur, ChannelWriter<BlocAudio> file, CancellationToken ct)
        {
            var tampon = new byte[TailleTampon];

            try
            {
                while (navigateur.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var recu = await navigateur.ReceiveAsync(new ArraySegment<byte>(tampon), ct);

                    if (recu.MessageType == WebSocketMessageType.Close) break;

                    // Une trame TEXTE n'est pas du son : c'est le navigateur qui
                    // annonce que l'élève s'est tu. Lui seul le sait — il mesure
                    // le niveau sonore, le fournisseur ne fait que le supposer.
                    if (recu.MessageType == WebSocketMessageType.Text)
                    {
                        if (EstFinDeTour(tampon.AsSpan(0, recu.Count)))
                        {
                            await file.WriteAsync(BlocAudio.Fin(), ct);
                        }

                        continue;
                    }

                    if (recu.MessageType != WebSocketMessageType.Binary || recu.Count == 0) continue;

                    // Copie obligatoire : le tampon est réutilisé au tour
                    // suivant, et la file garde une référence.
                    await file.WriteAsync(BlocAudio.Son(tampon[..recu.Count].ToArray()), ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }

            // Le relais s'est arrêté et la file a été close pendant qu'on
            // lisait : l'audio suivant n'a plus où aller. C'est un dénouement
            // normal, pas une panne — sans ce filet il remontait en exception
            // et faisait tomber la requête au lieu de la clore.
            catch (ChannelClosedException) { }
            finally
            {
                file.TryComplete();
            }
        }

        /// <summary>
        /// La trame texte annonce-t-elle la fin d'un tour de parole ?
        ///
        /// Lecture volontairement stricte : c'est le seul ordre qu'un client
        /// puisse donner sur cette liaison, et il n'y a aucune raison d'y
        /// ouvrir un dialecte. Tout le reste est ignoré en silence.
        /// </summary>
        private static bool EstFinDeTour(ReadOnlySpan<byte> trame)
        {
            try
            {
                using var document = JsonDocument.Parse(Encoding.UTF8.GetString(trame));

                return document.RootElement.TryGetProperty("type", out var type)
                       && type.GetString() == "fin_tour";
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static async Task EmettreAsync(
            WebSocket navigateur,
            FragmentTranscription fragment,
            CancellationToken ct,
            string? erreur = null)
        {
            if (navigateur.State != WebSocketState.Open) return;

            var motif = erreur ?? fragment.Erreur;

            var charge = motif is null
                ? JsonSerializer.SerializeToUtf8Bytes(
                    new { type = fragment.Final ? "final" : "partiel", texte = fragment.Texte })
                : JsonSerializer.SerializeToUtf8Bytes(new { type = "erreur", message = motif });

            try
            {
                await navigateur.SendAsync(charge, WebSocketMessageType.Text, true, ct);
            }
            catch (WebSocketException) { }
            catch (OperationCanceledException) { }
        }
    }
}
