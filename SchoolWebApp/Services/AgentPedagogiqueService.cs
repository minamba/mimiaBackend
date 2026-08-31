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
                            int TokensCacheLecture, int TokensCacheEcriture);

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
            IReadOnlyDictionary<int, PieceJointe>? piecesHistorique = null,
            PieceJointe? pieceDuTour = null,
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
        private readonly OptionsClaude _options;
        private readonly ILogger<AgentPedagogiqueService> _logger;

        public AgentPedagogiqueService(
            AnthropicClient client,
            IMaitriseService maitriseService,
            IFicheRepository fiches,
            IBibliothequePlanchesService bibliotheque,
            IPlancheRepository planches,
            IOptions<OptionsClaude> options,
            ILogger<AgentPedagogiqueService> logger)
        {
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
            IReadOnlyDictionary<int, PieceJointe>? piecesHistorique = null,
            PieceJointe? pieceDuTour = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var system = await ConstruireSystemAsync(
                eleve, conversation, accueil, depuisDerniereSeance, ct);
            // Quand l'élève montre du doigt, la planche part avec son geste.
            var pointage = pieceDuTour ?? await PlancheMontreeAsync(messageEleve, ct);

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

            var texte = new StringBuilder();
            var tokensEntree = 0;
            var tokensSortie = 0;
            var cacheLecture = 0;
            var cacheEcriture = 0;

            await foreach (var evenement in _client.Messages.CreateStreaming(parameters, cancellationToken: ct))
            {
                if (evenement.TryPickStart(out var debut))
                {
                    var usage = debut.Message.Usage;
                    tokensEntree = (int)usage.InputTokens;
                    cacheLecture = (int)(usage.CacheReadInputTokens ?? 0);
                    cacheEcriture = (int)(usage.CacheCreationInputTokens ?? 0);
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
                texte.ToString(), modele, tokensEntree, tokensSortie, cacheLecture, cacheEcriture);

            // cache-lu à 0 sur plusieurs tours consécutifs = le préfixe stable
            // n'atteint pas le minimum cacheable (1024 tokens sur Sonnet 5),
            // ou quelque chose de variable s'est glissé avant le point de césure.
            _logger.LogInformation(
                "Tour termine (conversation {ConversationId}, {Modele}) : {In} in / {Out} out / {CacheR} cache-lu.",
                conversation.Id, modele, tokensEntree, tokensSortie, cacheLecture);

            bilan.TrySetResult(resultat);
        }

        private static Effort ConvertirEffort(string valeur) => valeur?.ToLowerInvariant() switch
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
            TimeSpan? depuisDerniereSeance, CancellationToken ct)
        {
            var blocs = new List<TextBlockParam>
            {
                // L'identité est stable pour une matière donnée : elle entre
                // dans le préfixe mis en cache, avant le point de césure.
                new()
                {
                    Text = PromptsPedagogiques.Identite(
                        conversation.ProfPrenom, conversation.MatiereLibelle)
                },

                new() { Text = PromptsPedagogiques.Noyau },

                // La spécialité porte le catalogue des figures ; le bloc des
                // planches dit ce que celles qui ont été importées contiennent
                // vraiment. Les deux vont ensemble, donc ils partagent le même
                // bloc — et le point de césure du cache reste à la même place.
                new()
                {
                    Text = PromptsPedagogiques.Specialite(conversation.AgentSlug)
                           + await ConstruireBibliothequeAsync(conversation, ct),
                    CacheControl = CacheLong(),   // fin du préfixe stable
                },

                new() { Text = PromptsPedagogiques.Profil(eleve.NiveauCycle, eleve.Age, eleve.Sexe) },

                // Second point de césure. Le premier s'arrête à la spécialité et
                // protège le noyau quand les lacunes de l'élève changent ;
                // celui-ci fait entrer le profil et le contexte dans le préfixe
                // mis en cache, et c'est lui qui permet à l'historique — placé
                // juste après — d'être caché à son tour.
                //
                // Il est posé ICI et pas sur le dernier bloc de la liste : la
                // consigne d'accueil, quand elle existe, ne vaut que pour un
                // tour. Cacher un préfixe qui la contient paierait une écriture
                // de cache pour une entrée que personne ne relira jamais.
                new()
                {
                    Text = await ConstruireContexteEleveAsync(eleve, conversation, ct),
                    CacheControl = CacheLong(),
                },
            };

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

            var x = int.Parse(trouve.Groups[2].Value) / 100.0;
            var y = int.Parse(trouve.Groups[3].Value) / 100.0;

            try
            {
                var planche = await _planches.GetAsync(trouve.Groups[1].Value, ct);

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

                var planche = await _planches.GetSansDonneesAsync(trouve.Groups[1].Value, ct);
                if (planche?.Reperes is null) return string.Empty;

                var repere = Domain.Services.CarteReperes.PlusProche(
                    planche.Reperes,
                    int.Parse(trouve.Groups[2].Value) / 100.0,
                    int.Parse(trouve.Groups[3].Value) / 100.0);

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
                var cle = historique
                    .Where(m => m.Role == "assistant" && m.Contenu is not null)
                    .Select(m => CleSchema().Match(m.Contenu!))
                    .LastOrDefault(t => t.Success)
                    ?.Groups[1].Value;

                if (string.IsNullOrWhiteSpace(cle)) return string.Empty;

                var planche = await _planches.GetSansDonneesAsync(cle, ct);
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

        [System.Text.RegularExpressions.GeneratedRegex(@"SCHEMA:([a-z0-9-]+)")]
        private static partial System.Text.RegularExpressions.Regex CleSchema();

        [System.Text.RegularExpressions.GeneratedRegex(@"POINTAGE:([a-z0-9-]+)@(\d{1,3}),(\d{1,3})")]
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
            IReadOnlyDictionary<int, PieceJointe>? piecesHistorique,
            PieceJointe? pieceDuTour)
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

                PieceJointe? piece = null;
                piecesHistorique?.TryGetValue(utiles[i].Id, out piece);

                var blocs = ConstruireBlocs(utiles[i].Contenu, piece, marquerCache: dernier);

                // Le marqueur de cache se pose sur le DERNIER message de
                // l'historique et sur lui seul : le préfixe mis en cache doit
                // s'arrêter avant le tour courant, qui est neuf par définition.
                //
                // C'est ce marqueur qui rend les documents supportables. Une
                // page de PDF pèse des milliers de jetons et repart à CHAQUE
                // tour tant qu'elle est dans la fenêtre ; relue depuis le
                // cache, elle est facturée un dixième.
                messages.Add(blocs.Count == 1 && !dernier && piece is null
                    ? new MessageParam { Role = role, Content = utiles[i].Contenu! }
                    : new MessageParam { Role = role, Content = blocs });
            }

            messages.Add(new MessageParam
            {
                Role = Role.User,
                Content = ConstruireBlocs(messageEleve, pieceDuTour, marquerCache: false),
            });

            return messages;
        }

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
            string? texte, PieceJointe? piece, bool marquerCache)
        {
            var blocs = new List<ContentBlockParam>();
            var cache = marquerCache ? new CacheControlEphemeral() : null;

            // LES OCTETS ONT ÉTÉ PURGÉS : ON ENVOIE LE TEXTE.
            //
            // Au-delà de quelques jours, seule la transcription subsiste. Ce
            // n'est pas un repli dégradé mais le comportement voulu : à ce
            // stade le document est sorti de la fenêtre d'historique et n'était
            // plus envoyé de toute façon. S'il y revient — l'élève rouvre une
            // vieille conversation — le professeur retrouve au moins de quoi
            // dire sur quoi on avait travaillé.
            //
            // Le préfixe est explicite : sans lui, le modèle prendrait un
            // énoncé recopié pour une parole de l'élève.
            if (piece is not null && !piece.Consultable
                && !string.IsNullOrWhiteSpace(piece.Transcription))
            {
                blocs.Add(new TextBlockParam
                {
                    Text = $"[Document envoyé le {piece.DateCreation:d} — "
                           + $"« {piece.NomFichier} ». L'image n'est plus disponible, "
                           + $"voici son contenu tel qu'il avait été relevé :]\n\n"
                           + piece.Transcription,
                });
            }
            else if (piece?.Donnees is { Length: > 0 } donnees)
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
