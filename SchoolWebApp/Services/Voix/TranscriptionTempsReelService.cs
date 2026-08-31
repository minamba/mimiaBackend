using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SchoolWebApp.Api.Services.Voix
{
    /// <summary>Un morceau de transcription renvoyé au navigateur.</summary>
    /// <param name="Texte">Le texte reconnu.</param>
    /// <param name="Final">
    /// Vrai quand le tour de parole est clos. Un partiel s'affiche au fil de
    /// l'eau, un final déclenche l'envoi au professeur.
    /// </param>
    /// <param name="Erreur">
    /// Le fournisseur a refusé quelque chose. Remonté jusqu'au navigateur et
    /// pas seulement journalisé : une session mal configurée laissait le micro
    /// ouvert, l'interface allumée, et rien ne remontait — impossible à
    /// diagnostiquer sans lire les logs du serveur.
    /// </param>
    public record FragmentTranscription(string Texte, bool Final, string? Erreur = null);

    /// <summary>
    /// Ce qui circule du navigateur vers le fournisseur : de l'audio, ou
    /// l'ordre de clore le tour.
    ///
    /// La distinction est le cœur du problème du « allo ». Avec la détection
    /// sémantique, le fournisseur ne transcrit qu'APRÈS avoir décidé que le
    /// tour était fini — s'il ne décide pas, il n'envoie rien du tout, pas même
    /// un fragment provisoire. Attendre son verdict, ou tenter de le deviner à
    /// partir de ce qu'il a déjà transcrit, ne pouvait donc pas marcher : il
    /// n'y avait rien à deviner.
    ///
    /// Le navigateur, lui, mesure le niveau sonore et sait parfaitement quand
    /// l'élève s'est tu. Il envoie donc l'ordre, et le fournisseur transcrit.
    /// </summary>
    public readonly record struct BlocAudio(ReadOnlyMemory<byte> Donnees, bool FinDeTour)
    {
        public static BlocAudio Son(ReadOnlyMemory<byte> donnees) => new(donnees, false);

        public static BlocAudio Fin() => new(default, true);
    }

    public interface ITranscriptionTempsReelService
    {
        /// <summary>
        /// Ouvre une session de transcription et fait le relais entre le
        /// navigateur et le fournisseur jusqu'à ce que l'un des deux raccroche.
        /// </summary>
        /// <param name="audio">L'audio brut du navigateur, par blocs PCM 16 bits.</param>
        /// <param name="surFragment">Appelé à chaque bout de texte reconnu.</param>
        /// <param name="surSecondes">
        /// Appelé régulièrement avec le nombre de secondes d'audio transmises.
        /// C'est ce qui alimente le décompte du quota.
        /// </param>
        /// <param name="matiereCode">
        /// Oriente le vocabulaire du transcripteur : « pluriel » en cours de
        /// français, « hypoténuse » en mathématiques. Null = socle commun
        /// seulement — mieux vaut une pondération faible que celle d'une autre
        /// matière.
        /// </param>
        Task RelayerAsync(
            IAsyncEnumerable<BlocAudio> audio,
            Func<FragmentTranscription, Task> surFragment,
            Func<double, Task>? surSecondes = null,
            string? matiereCode = null,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Transcription en flux, par relais serveur.
    ///
    /// Le navigateur ne parle JAMAIS directement au fournisseur : la clé
    /// resterait à la portée de n'importe quel enfant ouvrant la console, et on
    /// perdrait le décompte des minutes. Il envoie donc son audio ici, et c'est
    /// ce service qui tient la liaison sortante.
    ///
    /// Le Web Speech du navigateur, qu'on remplace, avait deux défauts qu'aucun
    /// réglage ne corrigeait : il n'existe pas sur Firefox, et sa détection de
    /// fin de tour attend un silence long, non réglable, qui donnait cette
    /// impression de parler à une machine qui attend son tour.
    /// </summary>
    public class TranscriptionTempsReelService : ITranscriptionTempsReelService
    {
        /// <summary>Deux octets par échantillon : du PCM 16 bits, mono.</summary>
        private const int OctetsParEchantillon = 2;

        private const int TailleTampon = 16 * 1024;

        /// <summary>
        /// Le fournisseur refuse un commit portant sur moins de 100 ms de son.
        /// Ce n'est pas un avertissement : il répond une erreur, qui remontait
        /// jusqu'à l'élève en anglais.
        /// </summary>
        private const int MillisecondesMinimumCommit = 100;

        /// <summary>Code d'erreur du commit à vide, à taire côté élève.</summary>
        private const string CodeCommitVide = "input_audio_buffer_commit_empty";

        private readonly OptionsVoix _options;
        private readonly ILogger<TranscriptionTempsReelService> _logger;

        public TranscriptionTempsReelService(
            IOptions<OptionsVoix> options,
            ILogger<TranscriptionTempsReelService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RelayerAsync(
            IAsyncEnumerable<BlocAudio> audio,
            Func<FragmentTranscription, Task> surFragment,
            Func<double, Task>? surSecondes = null,
            string? matiereCode = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Aucune clé de transcription configurée (Voix:ApiKey).");
            }

            using var fournisseur = new ClientWebSocket();
            fournisseur.Options.SetRequestHeader("Authorization", $"Bearer {_options.ApiKey}");

            await fournisseur.ConnectAsync(new Uri(_options.UrlTempsReel), ct);
            var vocabulaire = VocabulaireTranscription.Pour(matiereCode);
            await ConfigurerSessionAsync(
                fournisseur, vocabulaire, VocabulaireTranscription.Langue(matiereCode), ct);

            // Les deux sens tournent en parallèle : l'élève parle pendant que
            // les transcriptions reviennent. Les enchaîner ajouterait la latence
            // qu'on cherche précisément à supprimer.
            // Partagé par les deux sens : l'un remplit le tampon du fournisseur,
            // l'autre apprend qu'il a été vidé. Voir TamponFournisseur.
            var tampon = new TamponFournisseur();

            var montant = PousserAudioAsync(fournisseur, audio, surSecondes, tampon, ct);
            var descendant = LireTranscriptionsAsync(
                fournisseur, surFragment, tampon, vocabulaire, ct);

            await Task.WhenAny(montant, descendant);

            // Le premier arrêté ferme la liaison ; l'autre en sort par
            // l'exception de socket close, qu'on ignore.
            await FermerAsync(fournisseur);

            try { await Task.WhenAll(montant, descendant); }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
        }

        /// <summary>
        /// Déclare une session de transcription seule — pas de génération de
        /// réponse : le cerveau, c'est Claude, et lui seul.
        /// </summary>
        private async Task ConfigurerSessionAsync(
            ClientWebSocket socket, string vocabulaire, string? langue, CancellationToken ct)
        {
            // LE CHAMP EST OMIS, PAS MIS À NULL.
            //
            // En cours de langue, l'élève parle le français ET la langue
            // étudiée, souvent dans le même tour, et aucun code ISO ne dit les
            // deux. Un dictionnaire plutôt qu'un objet anonyme : c'est la seule
            // façon de ne pas envoyer la clé du tout — envoyer « null » n'est
            // pas la même chose que se taire, et le fournisseur le refuse.
            var transcription = new Dictionary<string, object>
            {
                ["model"] = _options.ModeleTranscription,

                // Le vocabulaire attendu. Voir VocabulaireTranscription :
                // « pluriel » était rendu « la pupille féminin », faute de
                // savoir qu'on est en cours.
                ["prompt"] = vocabulaire,
            };

            if (langue is not null) transcription["language"] = langue;

            var configuration = new
            {
                type = "session.update",
                session = new
                {
                    type = "transcription",
                    audio = new
                    {
                        input = new
                        {
                            format = new { type = "audio/pcm", rate = _options.FrequenceAudio },
                            transcription,

                            turn_detection = DetectionTour(),
                        },
                    },
                },
            };

            await EnvoyerAsync(socket, configuration, ct);
        }

        /// <summary>
        /// Comment on décide que l'élève a fini de parler.
        ///
        /// La détection sémantique lit les MOTS : « euh », « hmm », une phrase
        /// laissée en suspens ne closent pas le tour. C'est ce qui permet à un
        /// enfant de réfléchir à voix haute sans se faire couper — une
        /// détection à l'énergie prend le moindre blanc pour une fin de phrase.
        /// </summary>
        private object DetectionTour() =>
            _options.TypeDetectionTour == "server_vad"
                ? new
                {
                    type = "server_vad",
                    silence_duration_ms = _options.SilenceFinDeTourMs,
                }
                : new
                {
                    type = "semantic_vad",
                    eagerness = _options.EmpressementTour,
                };

        private async Task PousserAudioAsync(
            ClientWebSocket socket,
            IAsyncEnumerable<BlocAudio> audio,
            Func<double, Task>? surSecondes,
            TamponFournisseur tampon,
            CancellationToken ct)
        {
            long octets = 0;
            var secondesSignalees = 0d;
            var plafond = _options.MinutesAudioMax * 60d;

            // Cent millisecondes de PCM : le plancher du fournisseur, exprimé
            // dans la seule unité qu'on manipule ici.
            var minimumCommit =
                _options.FrequenceAudio * OctetsParEchantillon * MillisecondesMinimumCommit / 1000;

            await foreach (var bloc in audio.WithCancellation(ct))
            {
                // L'ordre de clore le tour. C'est LUI qui garantit une réponse :
                // sans commit explicite, la détection sémantique peut garder la
                // phrase indéfiniment, sans rien transcrire ni renvoyer.
                if (bloc.FinDeTour)
                {
                    // MAIS on ne clôt que ce qui contient quelque chose.
                    //
                    // Le fournisseur a SA PROPRE détection de fin de tour, et
                    // elle vide le tampon quand elle se déclenche. Notre commit
                    // arrivait alors sur un tampon déjà vidé — « buffer only has
                    // 0.00ms » —, ce qu'il traite comme une erreur, remontée
                    // telle quelle jusqu'à l'élève, en anglais, au beau milieu
                    // d'un cours. Le tour, lui, avait bien été transcrit : le
                    // message d'erreur ne signalait rien d'autre que notre
                    // propre redondance.
                    //
                    // Sous le plancher, on ne jette rien : le son reste dans le
                    // tampon et repart avec le tour suivant. C'est d'ailleurs ce
                    // qu'il faut faire d'une syllabe isolée — la recoller à la
                    // suite plutôt que la transcrire seule.
                    if (tampon.Octets < minimumCommit) continue;

                    tampon.Vider();
                    await EnvoyerAsync(socket, new { type = "input_audio_buffer.commit" }, ct);
                    continue;
                }

                if (bloc.Donnees.Length == 0) continue;

                tampon.Ajouter(bloc.Donnees.Length);
                octets += bloc.Donnees.Length;
                var secondes = (double)octets / (_options.FrequenceAudio * OctetsParEchantillon);

                // Garde-fou de facturation : un onglet oublié devant un micro
                // qui capte du bruit ne diffuse pas une nuit entière.
                if (secondes > plafond)
                {
                    _logger.LogWarning(
                        "Transcription interrompue : plafond de {Minutes} minutes d'audio atteint.",
                        _options.MinutesAudioMax);
                    return;
                }

                await EnvoyerAsync(
                    socket,
                    new
                    {
                        type = "input_audio_buffer.append",
                        audio = Convert.ToBase64String(bloc.Donnees.Span),
                    },
                    ct);

                // On ne remonte pas la consommation à chaque bloc : ce sont des
                // écritures en base, et il en arrive plusieurs par seconde.
                if (surSecondes is not null && secondes - secondesSignalees >= 10)
                {
                    secondesSignalees = secondes;
                    await surSecondes(secondes);
                }
            }

            if (surSecondes is not null && secondesSignalees > 0)
            {
                await surSecondes((double)octets / (_options.FrequenceAudio * OctetsParEchantillon));
            }
        }

        private async Task LireTranscriptionsAsync(
            ClientWebSocket socket,
            Func<FragmentTranscription, Task> surFragment,
            TamponFournisseur tampon,
            string vocabulaire,
            CancellationToken ct)
        {
            var reception = new byte[TailleTampon];
            var message = new StringBuilder();

            // LE RECOLLAGE SE FAIT ICI, ET NULLE PART AILLEURS.
            //
            // Le fournisseur envoie des DELTAS : « singulier » arrive en
            // « sing », « uli », « er ». Le navigateur les recollait, et c'était
            // le mauvais endroit — parce que le filtre d'écho, lui, vit ici.
            //
            // Chaque delta fait moins de trente caractères : tous passaient
            // sous le seuil du filtre, un par un, et le navigateur les
            // rassemblait ensuite en une liste de vocabulaire entière, envoyée
            // au professeur sous le nom de l'élève. En boucle.
            //
            // On juge donc ce qu'on ASSEMBLE, pas ce qu'on reçoit. Le
            // navigateur reçoit désormais le texte recollé et n'a plus à
            // connaître le protocole du fournisseur.
            var partiel = new StringBuilder();
            var echoRepere = false;

            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var recu = await socket.ReceiveAsync(new ArraySegment<byte>(reception), ct);

                if (recu.MessageType == WebSocketMessageType.Close) return;

                message.Append(Encoding.UTF8.GetString(reception, 0, recu.Count));
                if (!recu.EndOfMessage) continue;

                var brut = message.ToString();
                message.Clear();

                var fragment = Interpreter(brut, tampon);

                // L'ÉCHO DU VOCABULAIRE N'EST PAS DE LA PAROLE.
                //
                // Sur un segment vide ou du bruit, ces modèles rendent le
                // PROMPT au lieu de rendre du vide. L'élève a vu partir au
                // professeur, sous son nom, la liste « Cours particulier,
                // France. consigne, exercice, énoncé… » — il n'avait rien dit
                // de tel.
                //
                // Le filtre est ici et non côté navigateur : c'est ici que le
                // vocabulaire est connu, et le comparer ailleurs demanderait de
                // l'y recopier.
                if (fragment is null) continue;

                // Une panne du fournisseur part telle quelle : elle ne se
                // recolle pas et ne se juge pas.
                if (fragment.Erreur is not null)
                {
                    await surFragment(fragment);
                    continue;
                }

                if (fragment.Final)
                {
                    // Le fournisseur a tranché : c'est lui qui pose la
                    // frontière du tour, et le recollage repart de zéro.
                    partiel.Clear();
                    var etaitEmpoisonne = echoRepere;
                    echoRepere = false;

                    if (etaitEmpoisonne || EstUnEcho(fragment.Texte, vocabulaire)) continue;

                    await surFragment(fragment);
                    continue;
                }

                partiel.Append(fragment.Texte);

                // Le tour est déjà reconnu comme un écho : on n'en fait plus
                // rien jusqu'à ce que le fournisseur le clôture.
                if (echoRepere) continue;

                var assemble = partiel.ToString();

                if (EstUnEcho(assemble, vocabulaire))
                {
                    echoRepere = true;

                    // ON EFFACE CE QUI EST DÉJÀ AFFICHÉ. Le début de la liste
                    // est passé sous le seuil de longueur — sans cette remise à
                    // vide, l'élève garderait « Cours particulier, France. » dans
                    // son champ, et ce bout partirait au professeur.
                    await surFragment(new FragmentTranscription("", false));
                    continue;
                }

                await surFragment(new FragmentTranscription(assemble, false));
            }
        }

        /// <summary>
        /// Ce texte est-il le vocabulaire qu'on a soufflé, renvoyé tel quel ?
        ///
        /// POURQUOI UN TEST PAR INCLUSION ET NON UNE ÉGALITÉ
        /// -------------------------------------------------
        /// Le modèle ne rend pas toujours le prompt en entier : il en rend un
        /// morceau, coupé n'importe où, parfois reponctué. Une égalité stricte
        /// ne l'attraperait qu'une fois sur trois.
        ///
        /// On compare donc les deux dépouillés de leur ponctuation, de leurs
        /// accents et de leur casse, et on regarde si le texte reçu est CONTENU
        /// dans le vocabulaire. Un élève ne prononce pas naturellement une
        /// suite de vingt termes dans l'ordre exact d'une liste qu'il n'a
        /// jamais vue.
        ///
        /// LE SEUIL DE LONGUEUR PROTÈGE LA PAROLE RÉELLE. « Pluriel » seul est
        /// contenu dans le vocabulaire, et c'est pourtant exactement ce qu'on
        /// veut entendre. En dessous d'une trentaine de caractères, on laisse
        /// passer : le risque de jeter une vraie réponse dépasse celui de
        /// laisser filer un écho court.
        /// </summary>
        private static bool EstUnEcho(string texte, string vocabulaire)
        {
            var propre = Depouiller(texte);
            if (propre.Length < 30) return false;

            var voc = Depouiller(vocabulaire);

            // Premier filet : le morceau rendu tel quel, dans l ordre.
            if (voc.Contains(propre, StringComparison.Ordinal)) return true;

            // SECOND FILET : PRESQUE TOUS LES MOTS VIENNENT DE LA LISTE.
            //
            // Le premier ne couvre que l écho FIDÈLE. Rien ne garantit que le
            // modèle rende les termes dans l ordre exact : il en saute, il en
            // réordonne, et le test par inclusion le laisse alors passer.
            //
            // Une phrase d élève, elle, est faite de mots qui ne sont PAS dans
            // la liste — articles, pronoms, verbes ordinaires. Le vocabulaire
            // n en contient aucun, et c est ce qui rend ce test sûr : « le sujet
            // c est les filles donc le participe passé s accorde » tombe très
            // bas, alors qu une suite de termes techniques touche les cent pour
            // cent.
            var termes = voc.Split(' ').ToHashSet(StringComparer.Ordinal);
            var mots = propre.Split(' ');

            // Sous six mots, la proportion ne veut rien dire : trois termes
            // d affilée arrivent dans une vraie réponse.
            if (mots.Length < 6) return false;

            return mots.Count(termes.Contains) >= mots.Length * 0.9;
        }

        /// <summary>
        /// Minuscules, sans accents, sans ponctuation, espaces normalisés.
        /// Deux textes qui ne diffèrent que par là sont le même texte.
        /// </summary>
        private static string Depouiller(string texte)
        {
            var sansAccents = new string(texte
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                            != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());

            var lettres = sansAccents
                .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');

            return string.Join(' ', new string(lettres.ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Traduit un événement du fournisseur. Renvoie null pour tout ce qui
        /// ne concerne pas la transcription — il en passe beaucoup, et les
        /// ignorer silencieusement vaut mieux que de casser le flux.
        /// </summary>
        private FragmentTranscription? Interpreter(string brut, TamponFournisseur tampon)
        {
            try
            {
                using var document = JsonDocument.Parse(brut);
                var racine = document.RootElement;

                if (!racine.TryGetProperty("type", out var type)) return null;

                return type.GetString() switch
                {
                    "conversation.item.input_audio_transcription.delta"
                        when racine.TryGetProperty("delta", out var delta) =>
                        new FragmentTranscription(delta.GetString() ?? "", false),

                    "conversation.item.input_audio_transcription.completed"
                        when racine.TryGetProperty("transcript", out var texte) =>
                        new FragmentTranscription(texte.GetString() ?? "", true),

                    // Le tampon vient d'être vidé — par nous, ou par la détection
                    // de tour du fournisseur, qui commit toute seule. C'est le
                    // second cas qui compte : sans cette remise à zéro, on
                    // croirait encore avoir de quoi clore un tour déjà clos.
                    "input_audio_buffer.committed" => Oublier(tampon),

                    "error" => Signaler(racine),

                    _ => null,
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static FragmentTranscription? Oublier(TamponFournisseur tampon)
        {
            tampon.Vider();
            return null;
        }

        private FragmentTranscription? Signaler(JsonElement racine)
        {
            var detail = racine.TryGetProperty("error", out var erreur)
                ? erreur.ToString()
                : racine.ToString();

            var code = racine.TryGetProperty("error", out var champ)
                && champ.TryGetProperty("code", out var valeur)
                ? valeur.GetString()
                : null;

            // Un commit sur un tampon vide n'a jamais rien cassé : le tour avait
            // été transcrit par la détection du fournisseur. Ça ne regarde donc
            // pas l'élève — et surtout pas en anglais, en plein cours.
            if (code == CodeCommitVide)
            {
                _logger.LogDebug(
                    "Commit sur tampon vide ignore : le fournisseur avait deja clos le tour.");
                return null;
            }

            _logger.LogError("Erreur de transcription renvoyee par le fournisseur : {Erreur}", detail);

            var message = racine.TryGetProperty("error", out var bloc)
                && bloc.TryGetProperty("message", out var texte)
                ? texte.GetString()
                : "Le service de transcription a refusé la session.";

            return new FragmentTranscription("", false, message);
        }

        /// <summary>
        /// Combien d'octets attendent dans le tampon du fournisseur.
        ///
        /// Le compte est tenu des DEUX côtés du relais — on l'incrémente en
        /// poussant l'audio, on le remet à zéro en voyant passer
        /// `input_audio_buffer.committed` — d'où les opérations atomiques : les
        /// deux sens tournent en parallèle, c'est tout l'intérêt du relais.
        /// </summary>
        private sealed class TamponFournisseur
        {
            private long _octets;

            public long Octets => Interlocked.Read(ref _octets);

            public void Ajouter(int octets) => Interlocked.Add(ref _octets, octets);

            public void Vider() => Interlocked.Exchange(ref _octets, 0);
        }

        private static Task EnvoyerAsync(ClientWebSocket socket, object charge, CancellationToken ct) =>
            socket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(charge),
                WebSocketMessageType.Text,
                endOfMessage: true,
                ct);

        private static async Task FermerAsync(ClientWebSocket socket)
        {
            if (socket.State != WebSocketState.Open) return;

            try
            {
                // Sans jeton d'annulation : la fermeture doit aboutir même quand
                // c'est l'annulation qui l'a provoquée.
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, "fin de séance", CancellationToken.None);
            }
            catch (WebSocketException) { }
        }
    }
}
