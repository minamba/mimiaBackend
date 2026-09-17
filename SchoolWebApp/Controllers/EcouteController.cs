using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWebApp.Api.Builders;
using SchoolWebApp.Api.Services.Voix;
using SchoolWebApp.Api.Utils;

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
        /// fournisseur. Bornée, pour que la mémoire du serveur ne gonfle pas.
        ///
        /// ELLE NE JETTE PLUS RIEN — relevé par Camara le 13/09/2026 : cinquante
        /// secondes de parole dites pendant une coupure, gardées par la mémoire
        /// tampon du navigateur, puis perdues à l'arrivée. La file valait 64
        /// blocs — un tiers de seconde — et jetait le son le plus ANCIEN quand
        /// elle débordait. Or elle ne commence à se vider qu'une fois la
        /// session du fournisseur ouverte : le navigateur, qui renvoyait d'un
        /// coup tout ce qu'il avait gardé, la remplissait en quelques
        /// millisecondes, et tout sauf la dernière syllabe partait à la
        /// poubelle. Sans un mot.
        ///
        /// Pleine, elle fait désormais ATTENDRE la réception : le son en trop
        /// reste dans le navigateur, qui le garde de toute façon, et part dès
        /// qu'il y a de la place. La mémoire du serveur reste bornée — c'était
        /// la raison du plafond — et plus une syllabe ne se perd pour la tenir.
        /// </summary>
        private const int ProfondeurFile = 1024;

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

        /// <summary>
        /// Ouvre l'écoute d'une séance. WebSocket uniquement.
        ///
        /// OUVERTE AUX ENFANTS, ET C'EST UNE CORRECTION DU 17/09/2026. Elle ne
        /// l'était pas, et le filtre des sessions enfant ferme tout ce qui ne porte
        /// pas cet attribut : un enfant entré par son code recevait 403 à chaque
        /// tentative d'ouverture, en boucle. Le cours se déroulait normalement — le
        /// professeur parlait, le texte passait — mais AUCUNE parole ne pouvait
        /// partir du micro, et rien ne le disait à l'écran.
        ///
        /// C'est le défaut que trois familles ont remonté sous le nom « le micro ne
        /// marche pas ». Il ne se reproduisait pas chez Camara, qui teste depuis le
        /// compte parent : le parent porte « Bearer », il passe ; seul le code
        /// enfant portait « Eleve », et lui seul se faisait fermer la porte.
        ///
        /// LA DICTÉE, ELLE, L'AVAIT DEPUIS LE 15/09 — d'où un micro qui marchait
        /// dans le formulaire de contrôle et pas en cours, sur la même machine.
        ///
        /// OUVRIR NE RELÂCHE RIEN : l'appartenance se vérifie juste en dessous,
        /// dans `VerifierQuotaAsync`, qui remonte de la conversation à son élève ;
        /// et `ChatContexteResolver.ResoudreEleveAsync` refuse à une session enfant
        /// tout élève qui ne serait pas elle-même. Un enfant ne peut donc pas
        /// écouter la séance de son frère en changeant le chiffre.
        /// </summary>
        [HttpGet("{conversationId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
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
                // TRACÉ, PARCE QU'UN REFUS MUET COÛTE DES JOURS. Ce 403 ne dit rien à
                // l'élève — le navigateur affiche « WebSocket failed » et c'est tout —
                // et ne disait rien au serveur non plus. Trois familles ont cherché du
                // côté de leur matériel un défaut qui était ici. Le motif du refus
                // distingue le quota épuisé de la séance qui n'appartient pas à
                // l'appelant : deux causes opposées derrière le même code.
                _logger.LogWarning(
                    "Ecoute REFUSEE (conversation {ConversationId}) : {Motif}.",
                    conversationId, verdict.Motif);

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

            await RelayerAsync(matiereCode, $"conversation {conversationId}");
        }

        /// <summary>
        /// La dictée d'un contrôle, depuis le formulaire « Ajouter un contrôle ».
        /// WebSocket uniquement.
        ///
        /// LE MÊME MICRO QU'EN COURS — voulu par Camara le 15/09/2026 : « le micro
        /// marche déjà bien en cours, mets le même système ». Le formulaire
        /// passait par la reconnaissance du navigateur, refusée à Chrome sur
        /// iPhone ; ici c'est exactement le relais des séances, sans séance.
        ///
        /// OUVERTE AUX ENFANTS (`AutoriseEleve`) : c'est l'enfant qui dicte son
        /// contrôle. Le filtre des sessions enfants vérifie qu'il ne dicte que
        /// pour LUI (paramètre `eleveId`) ; pour un parent, le résolveur vérifie
        /// que l'élève est bien le sien.
        /// </summary>
        [HttpGet("dictee/{eleveId:int}")]
        [SchoolWebApp.Api.Auth.AutoriseEleve]
        public async Task Dicter(int eleveId, [FromServices] IChatContexteResolver resolveur)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (await resolveur.ResoudreEleveAsync(eleveId) is null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // Pas de matière : la phrase en contient justement une à reconnaître
            // (« en maths »), le socle commun du vocabulaire suffit.
            await RelayerAsync(null, $"dictée de contrôle, élève {eleveId}");
        }

        /// <summary>
        /// Le relais entre le micro du navigateur et le fournisseur — commun aux
        /// séances et à la dictée d'un contrôle. Appelé une fois l'accès vérifié.
        /// </summary>
        private async Task RelayerAsync(string? matiereCode, string origine)
        {
            using var navigateur = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var ct = HttpContext.RequestAborted;

            // Le son transite par une file : la lecture du navigateur et
            // l'émission vers le fournisseur avancent chacune à son rythme.
            var file = Channel.CreateBounded<BlocAudio>(
                new BoundedChannelOptions(ProfondeurFile)
                {
                    // ATTENDRE, JAMAIS JETER : voir ProfondeurFile.
                    FullMode = BoundedChannelFullMode.Wait,
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
                            "Audio transcrit : {Secondes:0} s ({Origine}).",
                            secondes, origine);
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
                    "Echec de la transcription ({Origine}).", origine);

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
