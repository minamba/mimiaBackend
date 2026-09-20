using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Api.Services.Prompts;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using DomainMessage = SchoolWebApp.Domain.Models.Message;

namespace SchoolWebApp.Api.Services
{
    /// <summary>Fragment émis pendant le streaming.</summary>
    public record FragmentReponse(string Texte);

    /// <summary>Bilan d'un tour, une fois le streaming terminé.</summary>
    public record BilanTour(string TexteComplet, string Modele, int TokensEntree, int TokensSortie,
                            int TokensCacheLecture, int TokensCacheEcriture,
                            int? TokensCacheEcriture1h = null);

    public interface IAgentPedagogiqueService
    {
        /// <summary>
        /// Diffuse la réponse de l'agent token par token.
        /// Le bilan (texte complet + consommation) est déposé dans <paramref name="bilan"/>
        /// une fois le flux terminé — un IAsyncEnumerable ne peut pas retourner deux choses.
        /// </summary>
        IAsyncEnumerable<FragmentReponse> RepondreAsync(
            Eleve eleve,
            Conversation conversation,
            IEnumerable<DomainMessage> historique,
            string messageEleve,
            TaskCompletionSource<BilanTour> bilan,
            TypeTache tache = TypeTache.Dialogue,
            TypeAccueil accueil = TypeAccueil.Aucun,
            TimeSpan? depuisDerniereSeance = null,
            int? secondesRestantes = null,
            IReadOnlyDictionary<int, IReadOnlyList<PieceJointe>>? piecesHistorique = null,
            IReadOnlyList<PieceJointe>? piecesDuTour = null,
            CancellationToken ct = default);
    }

    public partial class AgentPedagogiqueService : IAgentPedagogiqueService
    {
        /// <summary>
        /// Nombre de messages d'historique conservés au minimum. La fenêtre
        /// réelle va de ce plancher à ce plancher plus un pas, selon l'endroit
        /// où en est la conversation — voir <see cref="PasHistorique"/>.
        /// </summary>
        public const int PlancherHistorique = 30;

        /// <summary>
        /// La fenêtre d'historique ne glisse que par pas de dix messages.
        ///
        /// Elle glissait d'un message à chaque tour : le plus ancien sortait
        /// pendant qu'un neuf entrait. Le préfixe envoyé à l'API n'était donc
        /// jamais deux fois le même, et le cache le réécrivait intégralement à
        /// chaque appel — 4 129 tokens par tour à 1,25× le prix, pour être relus
        /// une seule fois. Mesuré : la mise en cache coûtait 37 % de PLUS que
        /// pas de cache du tout.
        ///
        /// Par pas de dix, le même préfixe sert dix tours d'affilée : une
        /// écriture, neuf lectures à un dixième du prix.
        /// </summary>
        /// Vingt et non dix : chaque tour ajoute DEUX messages, celui de l'élève
        /// et celui du professeur. Un pas de vingt messages, c'est bien dix
        /// tours de conversation avec le même préfixe.
        public const int PasHistorique = 20;

        private readonly AnthropicClient _client;
        private readonly IMaitriseService _maitriseService;
        private readonly IFicheRepository _fiches;
        private readonly IBibliothequePlanchesService _bibliotheque;
        private readonly IPlancheRepository _planches;
        private readonly IReglageRepository _reglages;
        private readonly IMemoryCache _memoire;
        private readonly OptionsClaude _options;
        private readonly ILogger<AgentPedagogiqueService> _logger;

        public AgentPedagogiqueService(
            AnthropicClient client,
            IMaitriseService maitriseService,
            IFicheRepository fiches,
            IBibliothequePlanchesService bibliotheque,
            IPlancheRepository planches,
            IReglageRepository reglages,
            IMemoryCache memoire,
            IOptions<OptionsClaude> options,
            ILogger<AgentPedagogiqueService> logger)
        {
            _reglages = reglages ?? throw new ArgumentNullException(nameof(reglages));
            _memoire = memoire ?? throw new ArgumentNullException(nameof(memoire));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _maitriseService = maitriseService ?? throw new ArgumentNullException(nameof(maitriseService));
            _fiches = fiches ?? throw new ArgumentNullException(nameof(fiches));
            _bibliotheque = bibliotheque ?? throw new ArgumentNullException(nameof(bibliotheque));
            _planches = planches ?? throw new ArgumentNullException(nameof(planches));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async IAsyncEnumerable<FragmentReponse> RepondreAsync(
            Eleve eleve,
            Conversation conversation,
            IEnumerable<DomainMessage> historique,
            string messageEleve,
            TaskCompletionSource<BilanTour> bilan,
            TypeTache tache = TypeTache.Dialogue,
            TypeAccueil accueil = TypeAccueil.Aucun,
            TimeSpan? depuisDerniereSeance = null,
            int? secondesRestantes = null,
            IReadOnlyDictionary<int, IReadOnlyList<PieceJointe>>? piecesHistorique = null,
            IReadOnlyList<PieceJointe>? piecesDuTour = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            // LES EXERCICES DE LANGUE QUI CONCERNENT CE TOUR — leurs consignes
            // complètes ne partent que s'ils sont demandés, proposés ou en cours.
            // Voir `DetecteurExercicesLangue`.
            var exercices = DetecteurExercicesLangue.Concernes(historique, messageEleve);

            var system = await ConstruireSystemAsync(
                eleve, conversation, accueil, depuisDerniereSeance, exercices, ct);
            // Quand l'élève montre du doigt, la planche part avec son geste.
            // Sans document joint, la planche montrée du doigt tient ce rôle.
            var pointage = piecesDuTour is { Count: > 0 }
                ? piecesDuTour
                : await PlancheMontreeAsync(messageEleve, ct) is { } planche
                    ? new[] { planche }
                    : null;

            var messages = ConstruireMessages(
                historique,
                messageEleve
                    + await NommerLEndroitMontreAsync(messageEleve, ct)
                    + await RappelDeLaFigureAsync(historique, ct),
                piecesHistorique,
                pointage);

            var modele = _options.Modele(tache);

            var parameters = new MessageCreateParams
            {
                Model = modele,

                // Le plafond couvre la réflexion ET la réponse : la réflexion
                // adaptative est active par défaut sur Sonnet 5, donc trop bas
                // tronquerait la réponse en plein milieu.
                MaxTokens = _options.MaxTokens(tache),

                System = system,
                Messages = messages,

                // Le dialogue pédagogique est un échange court et fréquent, pas
                // un problème de raisonnement profond : `medium` suffit et divise
                // la facture. Les tâches complexes montent à `high` via la config.
                OutputConfig = new OutputConfig { Effort = ConvertirEffort(_options.Effort(tache)) },
            };

            var empreintes = EmpreintesDuPrompt(system, messages);

            var texte = new StringBuilder();
            var tokensEntree = 0;
            var tokensSortie = 0;
            var cacheLecture = 0;
            var cacheEcriture = 0;
            int? cacheEcriture1h = null;

            await foreach (var evenement in _client.Messages.CreateStreaming(parameters, cancellationToken: ct))
            {
                if (evenement.TryPickStart(out var debut))
                {
                    var usage = debut.Message.Usage;
                    tokensEntree = (int)usage.InputTokens;
                    cacheLecture = (int)(usage.CacheReadInputTokens ?? 0);
                    cacheEcriture = (int)(usage.CacheCreationInputTokens ?? 0);

                    // LE DÉTAIL PAR DURÉE, pour un coût exact : une heure se
                    // paie 2x, cinq minutes 1,25x. Sans détail rendu, on garde
                    // NULL — « inconnu » —, jamais zéro, qui mentirait.
                    var uneHeure = usage.CacheCreation?.Ephemeral1hInputTokens;
                    cacheEcriture1h = uneHeure is null ? null : (int)uneHeure;
                }
                else if (evenement.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var bloc))
                {
                    texte.Append(bloc.Text);
                    yield return new FragmentReponse(bloc.Text);
                }
                else if (evenement.TryPickDelta(out var fin))
                {
                    tokensSortie = (int)(fin.Usage?.OutputTokens ?? 0);
                }
            }

            var resultat = new BilanTour(
                texte.ToString(), modele, tokensEntree, tokensSortie, cacheLecture, cacheEcriture,
                cacheEcriture1h);

            // cache-lu à 0 sur plusieurs tours consécutifs = le préfixe stable
            // n'atteint pas le minimum cacheable (1024 tokens sur Sonnet 5),
            // ou quelque chose de variable s'est glissé avant le point de césure.
            _logger.LogInformation(
                "Tour termine (conversation {ConversationId}, {Modele}) : {In} in / {Out} out / {CacheR} cache-lu.",
                conversation.Id, modele, tokensEntree, tokensSortie, cacheLecture);

            // QUEL BLOC A CHANGÉ — voulu par Camara le 19/09/2026, après la
            // mesure qui a montré qu’un tour sur deux cassait le cache en route.
            // Une empreinte courte par bloc : deux tours consécutifs dont une
            // empreinte diffère désignent le coupable, sans deviner.
            _logger.LogInformation(
                "Cache du tour (conversation {ConversationId}) : {Ecrit} ecrits dont {UneHeure} pour 1 h, {Lu} relus | {Empreintes}",
                conversation.Id, cacheEcriture, cacheEcriture1h, cacheLecture, empreintes);

            bilan.TrySetResult(resultat);
        }

        /// <summary>
        /// Public pour le réchauffeur du cache : il doit envoyer le MÊME effort que
        /// le cours, sinon son préfixe ne sert pas — voir RechauffeurCacheWorker.
        /// </summary>
        public static Effort ConvertirEffort(string valeur) => valeur?.ToLowerInvariant() switch
        {
            "low" => Effort.Low,
            "medium" => Effort.Medium,
            "high" => Effort.High,
            "max" => Effort.Max,
            _ => Effort.Medium
        };

        /// <summary>
        /// La durée de vie du cache pour le préfixe SYSTÈME : une heure, et non
        /// les cinq minutes par défaut.
        ///
        /// CE QUE LA MESURE A MONTRÉ
        /// -------------------------
        /// Sur 1 013 tours enregistrés, TOUS ceux séparés du précédent par plus
        /// de cinq minutes ont réécrit le préfixe en entier — sans une seule
        /// exception, 117 tours sur 1 013. Ces 12 % de tours portaient à eux
        /// seuls 67 % des jetons de cache écrits. Une pause de cinq minutes n'a
        /// rien d'exceptionnel pendant un cours : l'élève cherche, pose son
        /// casque, écrit sur son cahier, revient.
        ///
        /// CE QUE ÇA COÛTE ET CE QUE ÇA RAPPORTE
        /// -------------------------------------
        /// L'écriture passe de 1,25x à 2x le prix d'entrée, sur TOUTES les
        /// écritures — y compris celles qui n'auraient jamais expiré. Le solde
        /// reste positif, mais de peu : environ 9 % sur la facture de dialogue,
        /// une dizaine de centimes par séance de 45 minutes.
        ///
        /// C'est à retenir avant d'y toucher davantage : le cache tel qu'il est
        /// fait DÉJÀ économiser 64 % (26 $ contre 73 $ sur ces mêmes tours). Il
        /// n'y a pas de gros gisement ici, et croire le contraire fait perdre du
        /// temps sur des dixièmes de centime.
        ///
        /// POURQUOI L'HISTORIQUE RESTE, LUI, À CINQ MINUTES
        /// -----------------------------------------------
        /// Son point de césure se déplace à chaque tour : ce qu'il écrit est un
        /// petit delta, relu une fois puis abandonné. Le payer 2x serait perdre
        /// des deux côtés — la surprime sans la durée.
        /// </summary>
        private static CacheControlEphemeral CacheLong() => new() { Ttl = "1h" };

        /// <summary>
        /// Assemble les quatre couches du prompt système.
        ///
        /// L'ordre est dicté par le cache : le cache de prompt est un préfixe, donc
        /// tout ce qui est stable doit précéder tout ce qui varie. Le point de
        /// césure est posé sur la spécialité — noyau + spécialité sont relus à
        /// ~0,1x du prix à chaque tour, ce qui représente l'essentiel du contexte.
        /// </summary>
        private async Task<MessageCreateParamsSystem> ConstruireSystemAsync(
            Eleve eleve, Conversation conversation, TypeAccueil accueil,
            TimeSpan? depuisDerniereSeance,
            IReadOnlyCollection<PromptsPedagogiques.ExerciceLangue> exercices,
            CancellationToken ct)
        {
            // L’ORDRE DES BLOCS EST DICTÉ PAR LE CACHE, ET IL A CHANGÉ LE 19/09/2026.
            //
            // Le cache de prompt est un PRÉFIXE, partagé entre toutes les requêtes
            // qui commencent par le même texte au caractère près — tous élèves et
            // toutes matières confondus. Ce qui est commun doit donc passer
            // AVANT ce qui est propre.
            //
            // LE NOYAU EST PASSÉ EN TÊTE, DEVANT L’IDENTITÉ DU PROFESSEUR — idée de
            // Camara : « charger une fois, tout garder en cache ». Il fait ~38 000
            // jetons et il est IDENTIQUE pour toutes les matières ; or l’identité
            // (« tu t’appelles Salim, professeur d’histoire-géo ») le précédait. Le
            // noyau était donc mis en cache à part pour chaque professeur, et une
            // matière que personne n’avait ouverte depuis une heure le réécrivait
            // au prix fort, alors qu’il était peut-être chaud pour une autre.
            // Désormais une seule copie sert tout le monde.
            //
            // LE TEXTE DU NOYAU N’EST PAS TOUCHÉ, et c’est une décision de Camara :
            // « c’est le cœur du produit ». Seule sa place change.
            //
            // QUATRE POINTS DE CÉSURE, LE MAXIMUM ACCEPTÉ PAR L’API :
            //   1. après le noyau          — commun à TOUT le monde ;
            //   2. après la spécialité     — commun aux élèves d’une même matière ;
            //   3. sur le dernier bloc propre à l’élève (son contexte, ou les
            //      consignes d’exercices quand il y en a) ;
            //   4. l’historique, posé plus loin dans `ConstruireMessages`.
            var blocs = new List<TextBlockParam>
            {
                new()
                {
                    Text = PromptsPedagogiques.Noyau,
                    CacheControl = CacheLong(),   // commun à toutes les matières
                },

                // L’identité juste après : stable pour une matière, elle entre
                // dans le deuxième préfixe, avec la spécialité.
                new()
                {
                    Text = PromptsPedagogiques.Identite(
                        conversation.ProfPrenom, conversation.MatiereLibelle)
                },

                // La spécialité porte le catalogue des figures ; le bloc des
                // planches dit ce que celles qui ont été importées contiennent
                // vraiment. Les deux vont ensemble, donc ils partagent le même
                // bloc — et le point de césure du cache reste à la même place.
                new()
                {
                    Text = PromptsPedagogiques.Specialite(conversation.AgentSlug)
                           + await ConstruireBibliothequeAsync(conversation, ct),
                    CacheControl = CacheLong(),   // commun à la matière
                },

                new() { Text = PromptsPedagogiques.Profil(eleve.NiveauCycle, eleve.Age, eleve.Sexe) },
            };

            var contexteEleve = await ContexteDeLaSeanceAsync(eleve, conversation, accueil, ct);

            // LES CONSIGNES DES EXERCICES DE LANGUE EN COURS — voir
            // `DetecteurExercicesLangue`.
            //
            // APRÈS LE NOYAU ET LE PROFIL, ET C’EST TOUT L’ENJEU. Ce bloc change
            // quand un exercice commence ou quitte la fenêtre ; placé plus haut,
            // chaque changement aurait fait réécrire les 38 000 jetons du noyau.
            //
            // PAS DANS LE MESSAGE DE L’ÉLÈVE : ce qui est dans le message n’est pas
            // mis en cache, et pendant une dictée ses 11 000 jetons auraient été
            // payés plein tarif à chaque tour — dix fois le prix.
            var consignesExercices = PromptsPedagogiques.ExercicesLangue(conversation.AgentSlug, exercices);
            var avecExercices = consignesExercices.Length > 0;

            // LE TROISIÈME POINT DE CÉSURE SE POSE SUR LE DERNIER BLOC PROPRE À
            // L’ÉLÈVE, pas sur chacun : il n’en reste plus qu’un à distribuer.
            //
            // QUAND UN EXERCICE S’AJOUTE, RIEN N’EST PERDU POUR AUTANT. L’API
            // cherche d’elle-même, en remontant les blocs, le plus long préfixe
            // déjà en cache : elle retrouve celui qui s’arrêtait au contexte, et
            // n’écrit que les consignes de l’exercice.
            //
            // UNE HEURE PARTOUT : un exercice dure souvent plus de cinq minutes
            // avec des pauses — l’élève écrit sur son cahier —, et le cache court
            // aurait expiré en plein milieu.
            blocs.Add(new TextBlockParam
            {
                Text = contexteEleve,
                CacheControl = avecExercices ? null : CacheLong(),
            });

            if (avecExercices)
            {
                blocs.Add(new TextBlockParam
                {
                    Text = consignesExercices,
                    CacheControl = CacheLong(),
                });
            }

            // La consigne d'accueil arrive en dernier : elle ne concerne qu'un
            // seul tour. Elle est rare — quelques tours par séance — donc son
            // effet sur le cache est marginal ; le minuteur, lui, tombait à
            // chaque message et c'est pour cela qu'il est parti dans le tour de
            // l'élève.
            if (accueil != TypeAccueil.Aucun)
            {
                blocs.Add(new TextBlockParam
                {
                    Text = PromptsPedagogiques.Accueil(accueil, depuisDerniereSeance),
                });
            }

            // LE MODE DÉVELOPPEUR, S'IL EST ALLUMÉ — voir `ReglagesController`.
            //
            // EN DERNIER, ET APRÈS LES DEUX POINTS DE CÉSURE : un bloc
            // conditionnel placé plus haut réécrirait tout le préfixe mis en
            // cache à chaque bascule, et le ferait payer à toutes les séances.
            // Ici, il ne coûte que sa propre longueur.
            //
            // UNE LECTURE PAR TOUR, sur une table minuscule déjà lue par
            // ailleurs. Une panne de lecture ne doit pas ouvrir le mode : le
            // défaut est `false`, et l'exception est avalée — un réglage
            // illisible laisse le professeur se comporter normalement.
            var modeDeveloppeur = false;
            try
            {
                modeDeveloppeur = await _reglages.EstActifAsync(
                    Controllers.ReglagesController.ModeDeveloppeur, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lecture du mode developpeur impossible : mode ignore.");
            }

            if (modeDeveloppeur)
            {
                blocs.Add(new TextBlockParam { Text = PromptsPedagogiques.ModeDeveloppeur });
            }

            return blocs;
        }

        /// <summary>
        /// La planche que l'élève vient de montrer du doigt, à joindre au tour.
        ///
        /// POURQUOI L'IMAGE, ET PAS SEULEMENT LA POSITION
        /// ----------------------------------------------
        /// La première version n'envoyait que des pourcentages, au motif qu'une
        /// image coûte cher et qu'un professeur qui connaît la géographie sait
        /// où tombe « 52 % de la largeur ». C'était faux, et l'essai en séance
        /// l'a montré sans appel : un élève a cliqué sur l'Île-de-France et
        /// s'est vu répondre « région Grand Est ».
        ///
        /// La raison est mécanique, pas pédagogique. Les pourcentages portent
        /// sur le FICHIER, pas sur la carte : celui des régions françaises
        /// contient de larges marges blanches, cinq encarts d'outre-mer en bas
        /// à gauche et la Corse détachée à droite. La France n'occupe qu'une
        /// fraction du cadre, et laquelle, personne ne peut le deviner sans
        /// voir l'image. Aucune consigne ne rattrape ça.
        ///
        /// CE QUE ÇA NE COÛTE PAS
        /// ---------------------
        /// Rien n'est stocké, rien n'est téléversé, rien n'est à purger : les
        /// octets sont déjà en base, on les joint au tour et ils disparaissent
        /// avec lui. Le coût est celui d'une image par CLIC — pas par message.
        ///
        /// Le SVG est écarté : l'API ne l'accepte pas comme image. Ces
        /// figures-là gardent la position seule, ce qui suffit sur un schéma
        /// d'anatomie où les zones sont grandes et peu nombreuses.
        /// </summary>
        private async Task<PieceJointe?> PlancheMontreeAsync(string messageEleve, CancellationToken ct)
        {
            var trouve = Pointage().Match(messageEleve ?? string.Empty);
            if (!trouve.Success) return null;

            var x = int.Parse(trouve.Groups[3].Value) / 100.0;
            var y = int.Parse(trouve.Groups[4].Value) / 100.0;

            try
            {
                // LA VARIANTE COMPTE ICI : si l'élève clique sur la carte muette,
                // c'est la muette qu'il faut joindre. Lui envoyer la légendée
                // reviendrait à montrer au professeur une image que l'enfant n'a
                // pas sous les yeux — et à lui souffler la réponse.
                var planche = await _planches.GetAsync(
                    trouve.Groups[1].Value, trouve.Groups[2].Value, ct);

                if (planche?.Donnees is not { Length: > 0 }) return null;

                // UNE PLANCHE SVG EST UNE PLANCHE COMME UNE AUTRE.
                //
                // Ce `return null` privait le professeur de l'image pour UN
                // TIERS des planches. Il ne lui restait que « en bas à
                // gauche », et il nommait au hasard : « alvéoles » trois fois
                // de suite, dont une pour un clic sur le diaphragme. Rien dans
                // ses réponses ne trahissait qu'il n'avait rien sous les yeux.
                var lisible = RasteriseurSvg.PourLaVision(planche.Donnees, planche.TypeMime);
                if (lisible is not { } vue) return null;

                // LA MARQUE EST DESSINÉE SUR L'IMAGE, ET C'EST TOUT L'ENJEU.
                //
                // Joindre la planche et la position séparément ne suffisait
                // pas : le professeur devait reporter lui-même deux
                // pourcentages sur une image, autrement dit calculer des
                // coordonnées à l'œil. Il a répondu « Hauts-de-France » pour un
                // clic sur l'Île-de-France, en s'en excusant.
                //
                // Avec la cible dessinée, la question n'est plus « où tombe
                // 53 % / 27 % ? » mais « dans quelle région est ce cercle ? ».
                var (marquee, type) = MarqueurPlanche.Marquer(vue.Donnees, x, y);

                // LA TRACE QUI MANQUAIT.
                //
                // Quand le professeur nomme le mauvais organe, trois choses ont
                // pu se passer : les coordonnées sont fausses, l'image n'est pas
                // partie, ou le modèle a mal lu. Sans cette ligne, on ne peut
                // que supposer — et on a supposé de travers plusieurs fois.
                //
                // Un chiffre par clic, en Information : de quoi relire une
                // séance et savoir laquelle des trois est en cause.
                _logger.LogInformation(
                    "Pointage {Cle} a {X}% / {Y}% : image de {Octets} octets ({Type}) jointe au tour.",
                    trouve.Groups[1].Value, trouve.Groups[2].Value, trouve.Groups[3].Value,
                    marquee.Length, type);

                return new PieceJointe
                {
                    NomFichier = planche.NomFichier,
                    TypeMime = type,
                    Taille = marquee.Length,
                    Donnees = marquee,
                };
            }
            catch (Exception ex)
            {
                // Sans l'image, le professeur garde la position : dégradé, pas
                // cassé. Un cours ne s'arrête pas sur une lecture ratée.
                _logger.LogWarning(ex, "Planche montree introuvable pour ce tour.");
                return null;
            }
        }

        /// <summary>
        /// LE NOM DE L'ENDROIT CLIQUÉ, CALCULÉ ET NON DEVINÉ.
        ///
        /// C'est le remplacement d'une lecture par une mesure. Le professeur
        /// recevait l'image et une marque, et devait dire ce qu'il y avait
        /// dessous. Six versions de la marque plus tard — cercle, cercle épais,
        /// cercle cerclé de noir, agrandissement de la zone, épingle — il
        /// nommait encore l'organe voisin, parfois très éloigné : l'anus
        /// annoncé « prostate ».
        ///
        /// Un modèle de vision lit très bien du texte et situe très mal un point
        /// dans l'espace. Ici la question devient une distance entre deux
        /// points, et la réponse ne dépend plus de personne.
        ///
        /// L'IMAGE CONTINUE DE PARTIR. Le nom est donné, mais le professeur doit
        /// pouvoir parler de ce qu'il y a autour — ce que la carte ne dit pas.
        ///
        /// Silencieux quand la planche n'a pas encore de carte, ou quand le clic
        /// ne tombe près d'aucune étiquette : mieux vaut ne rien affirmer que
        /// nommer l'étiquette la moins lointaine d'un clic tombé dans une marge.
        /// </summary>
        private async Task<string> NommerLEndroitMontreAsync(
            string messageEleve, CancellationToken ct)
        {
            try
            {
                var trouve = Pointage().Match(messageEleve ?? string.Empty);
                if (!trouve.Success) return string.Empty;

                // TOUJOURS LA CARTE DE LA LÉGENDÉE, MÊME QUAND LE CLIC EST SUR LA
                // MUETTE — et c'est tout le montage.
                //
                // Une muette n'a aucun mot à lire : on ne peut rien en extraire,
                // donc elle n'a pas de carte à elle. Mais les deux images sont le
                // même fond au même cadrage, à ceci près que l'une porte les mots
                // et l'autre pas. Les positions relevées sur la légendée valent
                // donc sur la muette, et l'exercice se corrige tout seul :
                // l'enfant montre une région vide, le professeur reçoit son nom.
                var planche = await _planches.GetSansDonneesAsync(
                    trouve.Groups[1].Value, Domain.Models.VariantePlanche.Legende, ct);

                if (planche?.Reperes is null) return string.Empty;

                var repere = Domain.Services.CarteReperes.PlusProche(
                    planche.Reperes,
                    int.Parse(trouve.Groups[3].Value) / 100.0,
                    int.Parse(trouve.Groups[4].Value) / 100.0);

                if (repere is null)
                {
                    return "\n\n[Le clic ne tombe sur aucune étiquette de la figure — "
                         + "il est entre deux, ou dans une marge. Ne devine pas : "
                         + "demande à l'élève ce qu'il voit à cet endroit.]";
                }

                return $"\n\n[L'ÉLÈVE A MONTRÉ : {repere.Mot.ToUpperInvariant()}.]\n"
                     + "[Réponse CALCULÉE : position du clic comparée à la carte des "
                     + "étiquettes. Ne la vérifie pas sur l'image, ce que tu crois y "
                     + "lire ne l'emporte pas.]\n"
                     // LA CONSIGNE SYSTÈME NE SUFFIT PAS, ET C'EST MESURÉ.
                     //
                     // « Pas de remerciement » y figure déjà. Mais la conversation
                     // est pleine de ses propres « merci pour l'image, c'est net
                     // cette fois » : elle se relit et se recopie. Une règle placée
                     // AVANT tout l'historique perd contre vingt exemples placés
                     // après.
                     //
                     // Ici, c'est la dernière chose lue avant d'écrire.
                     + "[COMMENCE PAR LE NOM. Pas de « merci », pas de « c'est net », "
                     + "pas de commentaire sur l'image ni sur ta propre lecture. "
                     + "L'élève a cliqué pour apprendre, pas pour être remercié.]";
            }
            catch (Exception ex)
            {
                // Le cours ne s'arrête pas sur un repère manquant : le
                // professeur retombe sur l'image et la marque, comme avant.
                _logger.LogWarning(ex, "Nommage de l'endroit montre indisponible.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Ce que porte RÉELLEMENT la figure actuellement au tableau, rappelé
        /// dans le tour courant.
        ///
        /// POURQUOI ICI ET PAS SEULEMENT DANS LA CONSIGNE
        /// ---------------------------------------------
        /// La liste des planches est déjà dans le prompt système. Elle n'a pas
        /// suffi, et l'incident est instructif : un professeur avait affirmé
        /// qu'une figure ne portait aucun mot — c'était vrai de la version
        /// précédente du fichier — et il l'a maintenu NEUF fois de suite. À
        /// chaque tour, ses propres phrases revenaient dans l'historique, plus
        /// proches et plus nombreuses qu'une ligne de consigne lue avant elles.
        /// Un modèle se relit, et se croit.
        ///
        /// Le fait doit donc arriver LÀ OÙ IL PÈSE : collé au message de
        /// l'élève, dans le tour courant, après tout l'historique. C'est la
        /// dernière chose lue avant de répondre.
        ///
        /// Silencieux quand il n'y a rien à dire : aucune figure au tableau,
        /// ou aucune description encore extraite. On n'invente pas de rappel.
        /// </summary>
        private async Task<string> RappelDeLaFigureAsync(
            IEnumerable<DomainMessage> historique, CancellationToken ct)
        {
            try
            {
                // La dernière figure affichée, et elle seule : le professeur en
                // a peut-être montré trois dans la séance, une seule est à
                // l'écran.
                var derniere = historique
                    .Where(m => m.Role == "assistant" && m.Contenu is not null)
                    .Select(m => CleSchema().Match(m.Contenu!))
                    .LastOrDefault(t => t.Success);

                var cle = derniere?.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(cle)) return string.Empty;

                // LA MUETTE EST AU TABLEAU : ON NE LUI RAPPELLE SURTOUT PAS LES
                // MOTS.
                //
                // Le relevé est celui de la légendée, et il est exact — mais
                // l'enfant, lui, regarde une figure vide. Le lui réciter serait
                // lui donner la réponse de l'exercice en cours, et c'est
                // exactement ce que le rappel aurait fait tout seul, puisqu'il
                // travaille par clé.
                if (Domain.Models.VariantePlanche.Normaliser(derniere?.Groups[2].Value)
                    == Domain.Models.VariantePlanche.Muette)
                {
                    return "\n\n[LA FIGURE AU TABLEAU EST LA VERSION MUETTE : "
                         + "elle ne porte AUCUN mot. Ne demande pas de lire, et ne "
                         + "prononce pas les noms que tu connais de la version "
                         + "légendée — c'est ce que tu fais deviner. Demande de "
                         + "MONTRER. Quand l'élève montre, le nom exact t'est "
                         + "donné : tu sauras s'il a juste.]";
                }

                var planche = await _planches.GetSansDonneesAsync(cle, null, ct);
                var releve = planche?.Contenu?.Trim();

                if (string.IsNullOrWhiteSpace(releve)) return string.Empty;

                // Une figure volontairement muette se rappelle aussi : le
                // professeur doit savoir qu'il n'y a rien à lire dessus, sinon
                // il demande à l'élève de déchiffrer des étiquettes absentes.
                if (releve.StartsWith(
                        Workers.DescriptionPlanchesWorker.SansNomEcrit,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "\n\n[LA FIGURE AU TABLEAU NE PORTE AUCUNE ÉTIQUETTE. "
                         + "C'est une figure à faire légender de mémoire.]";
                }

                return "\n\n[LA FIGURE ACTUELLEMENT AU TABLEAU PORTE CES MOTS ÉCRITS "
                     + $"DESSUS : {releve}]\n"
                     + "[Ce relevé fait foi. Si tu as dit plus tôt dans cette "
                     + "conversation que la figure n'était pas légendée, c'était "
                     + "faux : corrige-toi sans en faire une affaire, et sers-toi "
                     + "de ces mots pour interroger l'élève.]";
            }
            catch (Exception ex)
            {
                // Un rappel manquant dégrade, il ne casse pas : le professeur
                // retombe sur la consigne système.
                _logger.LogWarning(ex, "Rappel de la figure au tableau indisponible.");
                return string.Empty;
            }
        }

        /// <summary>
        /// `SCHEMA:hg-france-regions` ou `SCHEMA:hg-france-regions/muette`.
        ///
        /// Le suffixe est FACULTATIF et absent de toutes les figures écrites
        /// avant le 17/09/2026 : sans lui, c'est la légendée.
        /// </summary>
        [System.Text.RegularExpressions.GeneratedRegex(@"SCHEMA:([a-z0-9-]+)(?:/(legende|muette))?")]
        private static partial System.Text.RegularExpressions.Regex CleSchema();

        /// <summary>`POINTAGE:cle@x,y` ou `POINTAGE:cle/muette@x,y`.</summary>
        [System.Text.RegularExpressions.GeneratedRegex(
            @"POINTAGE:([a-z0-9-]+)(?:/(legende|muette))?@(\d{1,3}),(\d{1,3})")]
        private static partial System.Text.RegularExpressions.Regex Pointage();

        /// <summary>
        /// Le relevé des légendes des planches importées de la matière.
        ///
        /// Une panne ici ne doit pas empêcher le cours : sans ce bloc, le
        /// professeur retombe sur le comportement d'avant les imports — il
        /// affiche la figure et parle de ce que le titre annonce. Dégradé,
        /// mais pas cassé.
        /// </summary>
        private async Task<string> ConstruireBibliothequeAsync(
            Conversation conversation, CancellationToken ct)
        {
            try
            {
                var bloc = await _bibliotheque.ConstruireAsync(conversation.MatiereCode, ct);

                return string.IsNullOrEmpty(bloc) ? string.Empty : "\n\n" + bloc;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Bibliotheque des planches indisponible pour {Matiere}.",
                    conversation.MatiereCode);

                return string.Empty;
            }
        }

        /// <summary>
        /// LE CONTEXTE ÉLÈVE, FIGÉ POUR TOUTE LA SÉANCE — voulu par Camara le
        /// 19/09/2026.
        ///
        /// LA MESURE QUI L'A IMPOSÉ. Les 17 et 18/09, 53 et 58 tours sur 105 et
        /// 122 relisaient le début du prompt puis réécrivaient tout à partir de
        /// ce bloc : 31 % du coût de dialogue, chaque jour. Ce bloc porte des
        /// scores que l'observateur met à jour en pleine séance, et des listes
        /// dont l'ordre n'est pas garanti entre deux notions de même score. Le
        /// moindre caractère changé fait réécrire le bloc au tarif d'une heure
        /// (2x) ET tout l'historique qui le suit.
        ///
        /// CE QU'ON PERD : RIEN D'UTILE. Un score mis à jour à la dixième
        /// minute ne dit rien au professeur qu'il ne sache déjà — il vient de
        /// le voir se produire, c'est dans la conversation. La séance suivante
        /// repart d'un contexte recalculé.
        ///
        /// RECALCULÉ À CHAQUE DÉBUT DE SÉANCE (premier accueil, retour, séance
        /// enchaînée) et après une heure sans tour — le cache d'Anthropic ne
        /// vit pas plus longtemps de toute façon. La clé porte la classe et la
        /// matière : un parent qui change la classe de son enfant n'hérite pas
        /// d'un contexte périmé.
        /// </summary>
        private async Task<string> ContexteDeLaSeanceAsync(
            Eleve eleve, Conversation conversation, TypeAccueil accueil, CancellationToken ct)
        {
            var cle = $"contexte-seance:{conversation.Id}:{eleve.Id}:{eleve.NiveauScolaireId}:{conversation.MatiereId}";

            var debutDeSeance = accueil is TypeAccueil.PremiereSeance
                or TypeAccueil.Retour
                or TypeAccueil.NouvelleSeance;

            if (!debutDeSeance && _memoire.TryGetValue(cle, out string? fige) && fige is not null)
            {
                return fige;
            }

            var contexte = await ConstruireContexteEleveAsync(eleve, conversation, ct);

            _memoire.Set(cle, contexte, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(1),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4),
            });

            return contexte;
        }

        /// <summary>
        /// Une empreinte de huit caractères par bloc du prompt système, avec sa
        /// longueur, puis le nombre de messages d'historique : de quoi dire,
        /// d'un tour à l'autre, ce qui a bougé.
        /// </summary>
        private static string EmpreintesDuPrompt(
            MessageCreateParamsSystem system, List<MessageParam> messages)
        {
            static string Empreinte(string texte) => Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(texte)))[..8];

            var parties = new List<string>();

            if (system.TryPickTextBlockParams(out var blocs))
            {
                var i = 0;
                foreach (var bloc in blocs)
                {
                    parties.Add($"s{++i}={Empreinte(bloc.Text)}({bloc.Text.Length})");
                }
            }

            parties.Add($"historique={messages.Count - 1}");

            return string.Join(" ", parties);
        }

        /// <summary>
        /// Le contexte élève : ce qui fait qu'un agent maths sait qu'un blocage
        /// vient d'une lacune de CM2, et qu'un agent français sait ce que l'élève
        /// travaille en maths. C'est la mémoire partagée entre toutes les matières.
        /// </summary>
        private async Task<string> ConstruireContexteEleveAsync(
            Eleve eleve, Conversation conversation, CancellationToken ct)
        {
            var contexte = new StringBuilder();
            contexte.AppendLine("# L'élève");
            contexte.AppendLine();
            contexte.AppendLine($"Prénom : {eleve.Prenom}");
            contexte.AppendLine($"Classe : {eleve.NiveauLibelle}");
            contexte.AppendLine($"Âge : {eleve.Age} ans");

            // Un âge décalé par rapport à la classe est une information sensible :
            // l'agent doit l'utiliser pour le ton, jamais la mentionner à l'élève.
            contexte.AppendLine();

            // SA SÉRIE TECHNOLOGIQUE, POUR TOUS SES PROFESSEURS — la professeure
            // de maths d'un élève de STMG comme son professeur de management.
            // Ici et non dans la spécialité : elle change d'un élève à l'autre.
            if (PromptsSeriesTechnologiques.ContexteSerie(eleve.NiveauCode) is { } serie)
            {
                contexte.AppendLine(serie);
                contexte.AppendLine();
            }

            // SES SPÉCIALITÉS DE VOIE GÉNÉRALE, POUR LA MÊME RAISON : la
            // professeure de maths d'un élève de première doit savoir sur quel
            // programme porte son épreuve anticipée.
            if (PromptsSpecialitesGenerales.ContexteSpecialites(eleve.NiveauCode, eleve.Specialites) is { } specialites)
            {
                contexte.AppendLine(specialites);
                contexte.AppendLine();
            }

            // LV2 ET LLCER D'ESPAGNOL CHEZ LE MÊME ÉLÈVE : deux cours, une professeure.
            if (PromptsEspagnol.ContexteDeuxCours(conversation.MatiereCode, eleve.Lv2Espagnol, eleve.Specialites) is { } deuxCours)
            {
                contexte.AppendLine(deuxCours);
                contexte.AppendLine();
            }

            // Même chose en anglais, avec la LLCER anglais ou l'AMC.
            if (PromptsSpecialitesGenerales.ContexteDeuxCoursAnglais(conversation.MatiereCode, eleve.Specialites) is { } deuxCoursAnglais)
            {
                contexte.AppendLine(deuxCoursAnglais);
                contexte.AppendLine();
            }

            // LES NOMS DU PROGRAMME, AVANT LES NOTIONS DE L ELEVE.
            //
            // Le professeur nomme ses fiches. Sans cette liste il n avait sous
            // les yeux que les notions DEJA mesurees chez cet enfant — rien sur
            // une notion neuve — et il inventait alors un titre. Les seize
            // premieres fiches en portent la trace : « Utiliser le theoreme de
            // Thales » a cote de « Division decimale », le libelle du programme
            // a cote d un titre de chapitre. Or les fiches sont regroupees par
            // libelle EXACT : deux noms pour une meme notion, ce sont deux
            // fiches la ou l enfant devrait en enrichir une seule.
            //
            // C est ecrit ici, dans le contexte commun a tous les professeurs :
            // celui d espagnol qu on ajoutera demain en heritera sans qu on ait
            // rien a reecrire.
            var programme = (await _maitriseService.GetNotionsDuProgrammeAsync(
                conversation.MatiereId, eleve.NiveauScolaireId)).ToList();

            if (programme.Count > 0)
            {
                contexte.AppendLine("## Les notions du programme, a son niveau");
                contexte.AppendLine();

                foreach (var groupe in programme.GroupBy(n => n.Domaine))
                {
                    contexte.AppendLine($"### {groupe.Key}");
                    foreach (var notion in groupe)
                    {
                        contexte.AppendLine($"- {notion.Libelle}");
                    }
                }

                contexte.AppendLine();
                contexte.AppendLine(
                    "QUAND TU ECRIS UNE FICHE, prends le `notion:` et le `domaine:` " +
                    "dans cette liste, au mot pres, des que la notion travaillee y " +
                    "figure. C est ce qui rattache la fiche a ce qui est mesure, et " +
                    "ce qui fait qu on enrichit la fiche existante au lieu d en " +
                    "creer une deuxieme a cote. Si ce que tu as travaille n y est " +
                    "vraiment pas, choisis un titre court et garde-le a l identique " +
                    "les fois suivantes.");
                contexte.AppendLine();
                contexte.AppendLine(
                    "Cette liste ne dicte PAS la seance : l eleve arrive avec son " +
                    "besoin, et son besoin passe avant. Elle sert a nommer, pas a " +
                    "programmer. Ne la recite jamais.");
                contexte.AppendLine();
            }

            var lacunes = (await _maitriseService.GetLacunesAsync(eleve.Id, conversation.MatiereId, 8)).ToList();
            var acquises = (await _maitriseService.GetAcquisesAsync(eleve.Id, conversation.MatiereId, 5)).ToList();

            if (lacunes.Count > 0)
            {
                contexte.AppendLine("## Lacunes connues, de la plus fragile à la moins fragile");
                contexte.AppendLine();
                foreach (var l in lacunes)
                {
                    contexte.AppendLine($"- [{l.NiveauLibelle}] {l.Libelle} (maîtrise : {l.Score:P0})");
                }
                contexte.AppendLine();
                contexte.AppendLine(
                    "Si le blocage actuel touche l'une de ces notions, remonte dessus par une " +
                    "question courte avant de continuer. Une lacune d'un niveau antérieur est " +
                    "presque toujours la vraie cause. Ne récite jamais cette liste à l'élève.");
                contexte.AppendLine();
            }
            else
            {
                contexte.AppendLine(
                    "Aucune évaluation enregistrée pour l'instant : profite de cet échange pour " +
                    "situer son niveau réel par tes questions, sans lui faire passer un test.");
                contexte.AppendLine();
            }

            // La révision espacée.
            //
            // POURQUOI CETTE SECTION EXISTE
            // -----------------------------
            // `ProchaineRevision` est calculée depuis toujours à chaque
            // observation — une lacune revient le lendemain, une notion solide
            // dans plusieurs semaines — et rien ne la lisait. C'est le seul
            // mécanisme qui transforme « il a compris en séance » en « il sait
            // encore trois semaines plus tard ».
            //
            // POURQUOI ICI ET NON DANS UN ÉCRAN DE QUIZ
            // -----------------------------------------
            // Un professeur particulier ne distribue pas de questionnaire : il
            // glisse la question dans la conversation. L'élève est déjà là,
            // casque sur les oreilles ; la révision ne lui coûte ni écran de
            // plus, ni décision de s'y mettre.
            //
            // C'est écrit ici, dans le contexte commun, et non dans le bloc
            // d'une matière : tous les professeurs — maths, français, à venir —
            // révisent de la même façon sans qu'on ait à le réécrire.
            var aRevoir = (await _maitriseService.GetARevoirAsync(
                eleve.Id, conversation.MatiereId, 3)).ToList();

            if (aRevoir.Count > 0)
            {
                contexte.AppendLine("## À réviser aujourd'hui");
                contexte.AppendLine();
                foreach (var r in aRevoir)
                {
                    var jours = Math.Max(0, (int)(DateTime.UtcNow - r.DerniereEvaluation).TotalDays);
                    var quand = jours == 0 ? "aujourd'hui"
                        : jours == 1 ? "hier"
                        : $"il y a {jours} jours";

                    contexte.AppendLine($"- {r.Libelle} (vue {quand}, maîtrise : {r.Score:P0})");
                }
                contexte.AppendLine();
                contexte.AppendLine(
                    "Ouvre la séance par UNE question courte sur la première de cette liste. " +
                    "Une seule question, deux minutes au plus : c'est un rappel, pas un contrôle.");
                contexte.AppendLine();
                contexte.AppendLine(
                    "SAUF si l'élève arrive avec quelque chose de précis — un contrôle qui " +
                    "approche, un devoir à rendre, un exercice qui le bloque. Son besoin passe " +
                    "AVANT la révision, toujours. Dans ce cas tu n'en parles pas en ouverture ; " +
                    "tu la glisses à la fin s'il reste du temps, ou tu la laisses pour la " +
                    "prochaine fois. Elle reviendra d'elle-même.");
                contexte.AppendLine();
                contexte.AppendLine(
                    "Et s'il refuse — « non, je veux faire autre chose » — tu passes " +
                    "immédiatement, sans insister ni négocier. Deux minutes arrachées ne font " +
                    "rien apprendre, et il retiendra surtout qu'on lui a forcé la main.");
                contexte.AppendLine();
                contexte.AppendLine(
                    "Ne dis JAMAIS que c'est une révision programmée, ne parle pas d'échéance " +
                    "ni de liste. Demande simplement, comme un professeur qui se souvient : " +
                    "« avant qu'on attaque, rappelle-moi… ». S'il répond juste, félicite en une " +
                    "phrase et enchaîne. S'il hésite, c'est que la notion n'était pas acquise — " +
                    "reprends-la maintenant, c'est plus utile que ce qui était prévu.");
                contexte.AppendLine();
            }

            if (acquises.Count > 0)
            {
                contexte.AppendLine("## Notions déjà acquises");
                contexte.AppendLine();
                foreach (var a in acquises)
                {
                    contexte.AppendLine($"- {a.Libelle}");
                }
                contexte.AppendLine();
                contexte.AppendLine("Ne les réexplique pas : appuie-toi dessus.");
                contexte.AppendLine();
            }

            // Les fiches déjà écrites.
            //
            // Le professeur travaillait à l'aveugle : il ne savait pas ce qu'il
            // avait déjà rédigé, et réinventait un intitulé à chaque séance.
            // Une notion reprise donnait donc une SECONDE fiche au lieu
            // d'enrichir la première — la règle « jamais deux fiches pour une
            // même notion » n'était pas tenable, et l'élève ne voyait jamais
            // le badge « Mise à jour » puisque rien n'était jamais mis à jour.
            var titres = await _fiches.GetTitresAsync(eleve.Id, conversation.MatiereId, ct);

            if (titres.Count > 0)
            {
                contexte.AppendLine("## Fiches de révision déjà écrites pour lui");
                contexte.AppendLine();
                foreach (var t in titres)
                {
                    contexte.AppendLine($"- {t}");
                }
                contexte.AppendLine();
                contexte.AppendLine(
                    "Si tu retravailles l'une de ces notions aujourd'hui, ne crée pas une " +
                    "seconde fiche : réécris CELLE-CI en entier, enrichie de la séance du " +
                    "jour, en reprenant son intitulé À L'IDENTIQUE. N'écris une fiche neuve " +
                    "que pour une notion qui ne figure pas dans cette liste.");
            }

            return contexte.ToString();
        }

        /// <summary>
        /// L'historique, puis le tour de l'élève.
        ///
        /// Le DERNIER message de l'historique porte un point de césure : au tour
        /// suivant, tout ce qui précède — prompt système et conversation — est
        /// relu à un dixième du prix au lieu d'être refacturé en entier.
        ///
        /// Sans lui, l'historique repartait plein tarif à chaque échange : près
        /// de 6 000 tokens par minute de cours, soit le premier poste de coût du
        /// produit devant la synthèse vocale.
        /// </summary>
        private static List<MessageParam> ConstruireMessages(
            IEnumerable<DomainMessage> historique,
            string messageEleve,
            IReadOnlyDictionary<int, IReadOnlyList<PieceJointe>>? piecesHistorique,
            IReadOnlyList<PieceJointe>? piecesDuTour)
        {
            // La fenêtre arrive déjà taillée par paliers : c'est le dépôt qui
            // s'en charge, à partir du nombre total de messages. La couper ici,
            // sur une liste qui glisse déjà, ne servirait à rien.
            //
            // Un message VIDE reste écarté — sauf s'il porte un document : un
            // élève qui envoie sa feuille sans un mot a bien pris un tour de
            // parole, et l'écarter ferait disparaître la feuille.
            var utiles = historique
                .Where(m => !string.IsNullOrWhiteSpace(m.Contenu)
                            || (piecesHistorique?.ContainsKey(m.Id) ?? false))
                .ToList();

            var messages = new List<MessageParam>();

            for (var i = 0; i < utiles.Count; i++)
            {
                var role = utiles[i].Role == "assistant" ? Role.Assistant : Role.User;
                var dernier = i == utiles.Count - 1;

                IReadOnlyList<PieceJointe>? pieces = null;
                piecesHistorique?.TryGetValue(utiles[i].Id, out pieces);

                // UN MARQUEUR DE SUPPORT NE VAUT QUE POUR SON TOUR.
                //
                // L ecran joint au message de l eleve un constat — sa copie
                // est au cahier, ou elle vient d arriver au clavier. C est
                // vrai a cet instant-la, et faux des le tour suivant.
                //
                // Releve le 11/09/2026 : l eleve rafraichit sa page en pleine
                // dictee, repart au clavier, et le professeur lui reclame une
                // photo. Onze messages de son historique portaient encore
                // « DICTEE AU CAHIER » — dont un de l avant-veille. Il ne
                // lisait pas l etat du moment, il lisait celui d hier.
                //
                // Ils partent donc de TOUT l historique. Le seul marqueur qui
                // subsiste est celui du tour courant, ajoute plus bas avec le
                // message de l eleve.
                var contenu = SansMarqueurDeSupport(utiles[i].Contenu);
                var blocs = ConstruireBlocs(contenu, pieces, marquerCache: dernier);

                // Le marqueur de cache se pose sur le DERNIER message de
                // l'historique et sur lui seul : le préfixe mis en cache doit
                // s'arrêter avant le tour courant, qui est neuf par définition.
                //
                // C'est ce marqueur qui rend les documents supportables. Une
                // page de PDF pèse des milliers de jetons et repart à CHAQUE
                // tour tant qu'elle est dans la fenêtre ; relue depuis le
                // cache, elle est facturée un dixième.
                messages.Add(blocs.Count == 1 && !dernier && pieces is not { Count: > 0 }
                    ? new MessageParam { Role = role, Content = contenu! }
                    : new MessageParam { Role = role, Content = blocs });
            }

            messages.Add(new MessageParam
            {
                Role = Role.User,
                Content = ConstruireBlocs(messageEleve, piecesDuTour, marquerCache: false),
            });

            return messages;
        }

        /// <summary>
        /// Retire le constat de support joint par l ecran au message.
        ///
        /// Il dit ou en est la copie AU MOMENT DU TOUR : sur un cahier et pas
        /// encore recue, ou tapee et deja entiere. Garde dans l historique, il
        /// affirme au tour suivant quelque chose qui n est plus vrai.
        /// </summary>
        private static string? SansMarqueurDeSupport(string? contenu) =>
            string.IsNullOrEmpty(contenu)
                ? contenu
                : MarqueurDeSupport().Replace(contenu, string.Empty).TrimEnd();

        [GeneratedRegex(@"
?[[^]]*(?:AU CAHIER|AU CLAVIER)[^]]*]",
            RegexOptions.IgnoreCase)]
        private static partial Regex MarqueurDeSupport();

        /// <summary>
        /// Le contenu d'un tour : son texte, et le document qui l'accompagne.
        ///
        /// L'ORDRE N'EST PAS INDIFFÉRENT — le document vient AVANT le texte.
        /// C'est la disposition que le modèle exploite le mieux : il a la pièce
        /// sous les yeux quand il lit la question qui la concerne, au lieu de
        /// lire « je bloque sur le 3 » avant de savoir de quoi il s'agit.
        ///
        /// Un tour sans texte reste un tour valide : la liste ne contient alors
        /// que le document. Un bloc de texte vide, lui, ferait échouer l'appel.
        /// </summary>
        private static List<ContentBlockParam> ConstruireBlocs(
            string? texte, IReadOnlyList<PieceJointe>? pieces, bool marquerCache)
        {
            var blocs = new List<ContentBlockParam>();
            var cache = marquerCache ? new CacheControlEphemeral() : null;

            // TOUS LES DOCUMENTS, DANS L'ORDRE, PUIS LE TEXTE — Camara, le
            // 16/09/2026 : « le professeur reçoit tous les documents d'un coup
            // et a tout en tête ». L'ordre est celui où l'élève les a ajoutés :
            // page 1, page 2, l'énoncé — c'est le sien, il faut le respecter.
            foreach (var piece in pieces ?? Array.Empty<PieceJointe>())
            {
                // LES OCTETS ONT ÉTÉ PURGÉS : ON ENVOIE LE TEXTE.
                //
                // Au-delà de quelques jours, seule la transcription subsiste. Ce
                // n'est pas un repli dégradé mais le comportement voulu : à ce
                // stade le document est sorti de la fenêtre d'historique et
                // n'était plus envoyé de toute façon. S'il y revient — l'élève
                // rouvre une vieille conversation — le professeur retrouve au
                // moins de quoi dire sur quoi on avait travaillé.
                //
                // Le préfixe est explicite : sans lui, le modèle prendrait un
                // énoncé recopié pour une parole de l'élève.
                if (!piece.Consultable && !string.IsNullOrWhiteSpace(piece.Transcription))
                {
                    blocs.Add(new TextBlockParam
                    {
                        Text = $"[Document envoyé le {piece.DateCreation:d} — "
                               + $"« {piece.NomFichier} ». L'image n'est plus disponible, "
                               + $"voici son contenu tel qu'il avait été relevé :]\n\n"
                               + piece.Transcription,
                    });
                }
                else if (piece.Donnees is { Length: > 0 } donnees)
                {
                    var base64 = Convert.ToBase64String(donnees);

                    if (piece.EstPdf)
                    {
                        blocs.Add(new DocumentBlockParam
                        {
                            Source = new Base64PdfSource { Data = base64 },

                            // Le nom du fichier voyage avec le document : c'est
                            // souvent lui qui dit ce que c'est — « DM4_fractions ».
                            Title = piece.NomFichier,
                        });
                    }
                    else
                    {
                        blocs.Add(new ImageBlockParam
                        {
                            Source = new Base64ImageSource
                            {
                                Data = base64,
                                MediaType = MediaTypeDepuis(piece.TypeMime),
                            },
                        });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(texte))
            {
                blocs.Add(new TextBlockParam { Text = texte, CacheControl = cache });
            }
            else if (blocs.Count == 0)
            {
                // Ni texte ni document : ne jamais produire un tour vide, l'API
                // le refuse. Ce cas ne devrait pas arriver, le contrôleur le
                // bloque en amont — mais un tour refusé casserait la séance.
                blocs.Add(new TextBlockParam { Text = "(sans texte)", CacheControl = cache });
            }
            else if (cache is not null)
            {
                // Document seul à marquer : le marqueur se pose sur le dernier
                // bloc quel qu'il soit, sinon le préfixe n'est pas mis en cache
                // et la pièce est refacturée plein tarif à chaque tour.
                var document = blocs[^1];
                blocs[^1] = document.Value is DocumentBlockParam doc
                    ? doc with { CacheControl = cache }
                    : document.Value is ImageBlockParam img
                        ? img with { CacheControl = cache }
                        : document;
            }

            return blocs;
        }

        /// <summary>
        /// Le type d'image, dans le vocabulaire du SDK. Les cinq formats
        /// acceptés à l'entrée sont exactement ceux d'ici : un type inconnu
        /// signifierait que la validation a laissé passer autre chose.
        /// </summary>
        private static MediaType MediaTypeDepuis(string? typeMime) => typeMime switch
        {
            "image/png" => MediaType.ImagePng,
            "image/gif" => MediaType.ImageGif,
            "image/webp" => MediaType.ImageWebP,
            _ => MediaType.ImageJpeg,
        };
    }
}
