using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;


namespace SchoolWebApp.Api.Services
{
    public interface IObservateurCompetencesService
    {
        /// <summary>
        /// Analyse une séance terminée et met à jour la maîtrise de l'élève.
        /// Retourne le nombre d'observations réellement appliquées.
        /// </summary>
        Task<int> ObserverAsync(SeanceAObserver seance, CancellationToken ct = default);

        /// <summary>
        /// Analyse une séance désignée par son identifiant, puis avance son
        /// marqueur. Utilisé quand l'élève vient de quitter le cours.
        /// </summary>
        Task<int> ObserverSeanceAsync(int conversationId, CancellationToken ct = default);

        /// <summary>
        /// Vrai si la séance est assez longue et assez fournie pour qu'on en
        /// tire quelque chose. Une séance non exploitable n'est pas marquée
        /// comme analysée : elle pourra l'être une fois qu'elle se sera étoffée.
        /// </summary>
        bool Exploitable(SeanceAObserver seance);

        /// <summary>
        /// Le filet de fin de préparation, seul : si un contrôle de cette
        /// matière approche et que la séance l'a laissé sans verdict, le
        /// professeur est rappelé pour conclure sur la transcription.
        ///
        /// EXPOSÉ POUR LE BALAYAGE, qui écarte lui-même les séances trop
        /// courtes avant d'appeler <see cref="ObserverAsync"/> — et le filet
        /// avec elles. Relevé par Camara le 13/09/2026 : une séance de deux
        /// minutes, conclue par « tu es prêt » à l'oral, que trois portes
        /// successives ont refusée avant même d'atteindre le filet. Il n'a
        /// pas le seuil des compétences, seulement le sien (quatre messages)
        /// et le jugement du professeur. Sans effet si la fenêtre a déjà été
        /// soumise : le rappeler deux fois ne coûte rien.
        /// </summary>
        Task ConclurePreparationAsync(SeanceAObserver seance, CancellationToken ct = default);
    }

    /// <summary>
    /// Transforme une séance en observations de compétences.
    ///
    /// C'est la pièce qui manquait pour que le professeur ait une mémoire : sans
    /// elle, chaque séance repart de zéro et la promesse du produit — remonter
    /// d'un blocage de 6e jusqu'à une lacune de CM1 — n'est qu'une intention.
    ///
    /// L'analyse tourne APRÈS la séance, dans un worker : elle n'ajoute aucune
    /// latence à la conversation, où chaque seconde s'entend.
    /// </summary>
    public class ObservateurCompetencesService : IObservateurCompetencesService
    {
        /// <summary>Niveaux en amont proposés au rattachement. Cinq ans couvrent le primaire entier.</summary>
        private const int MargeAmont = 5;

        /// <summary>Un élève en avance touche parfois la notion de l'année suivante.</summary>
        private const int MargeAval = 1;

        /// <summary>En dessous, la séance est trop courte pour rien conclure.</summary>
        private const int MessagesMinimum = 4;

        /// <summary>
        /// Durée en deçà de laquelle on n'évalue rien.
        ///
        /// Le nombre de messages ne suffit pas : quatre échanges expédiés en
        /// deux minutes ne disent rien d'une compétence, et une observation
        /// pèse durablement sur le score. Mieux vaut ne rien écrire que d'écrire
        /// du bruit — une lacune inventée enverrait le professeur réexpliquer
        /// une notion déjà acquise.
        /// </summary>
        private static readonly TimeSpan DureeMinimum = TimeSpan.FromMinutes(10);

        /// <summary>
        /// LE VERDICT DE PRÉPARATION A SON PROPRE SEUIL, ET IL EST PLUS BAS.
        ///
        /// Relevé par Camara le 13/09/2026 : « j'ai fait une séance mais je suis
        /// parti vite » — et aucun verdict n'était apparu. La cause : le filet
        /// de conclusion vivait derrière <see cref="Exploitable"/>, taillé pour
        /// tout autre chose.
        ///
        /// LES DEUX N'ONT PAS LE MÊME COÛT D'ERREUR, et c'est ce qui justifie
        /// deux seuils. Une observation de compétence écrit un SCORE qui pèse
        /// durablement : sur cinq minutes bâclées, elle inventerait une lacune,
        /// et le professeur irait réexpliquer une notion déjà acquise. Dix
        /// minutes sont un prix raisonnable pour s'en garder.
        ///
        /// Un verdict de préparation, lui, n'écrit aucun score : il écrit une
        /// phrase à l'enfant. Sur une séance écourtée, la phrase juste existe
        /// et elle est utile — « on n'a pas eu le temps d'aller loin, il te
        /// reste la réciproque ». La perdre coûte plus cher que de la rendre
        /// sur peu de matière : l'enfant retrouve alors sa fiche exactement
        /// dans l'état où il l'avait laissée, comme s'il n'avait rien fait.
        ///
        /// PLUS DE SEUIL DE DURÉE, ET VOICI POURQUOI — le 13/09/2026, en deux
        /// temps.
        ///
        /// Camara avait d'abord tranché pour cinq minutes plutôt qu'un nombre
        /// de messages, avec le contre-exemple qui l'imposait : « Bonjour »,
        /// « On avance sur quoi ? », « Les fractions », « D'accord » — quatre
        /// messages, pas une information. Compter les tours mesure la politesse
        /// aussi bien que le travail.
        ///
        /// Puis le cas inverse s'est produit : une séance coupée en deux par une
        /// observation intermédiaire, dont la seconde moitié — huit messages
        /// sur une minute cinquante — contenait une justification complète ET
        /// le « tu es prêt pour ton contrôle » du professeur. Sous cinq minutes,
        /// donc jamais conclue, à chaque passage, indéfiniment. Un chronomètre
        /// ne sait pas lire.
        ///
        /// Ce qui distingue une séance creuse d'une séance courte mais pleine,
        /// c'est son CONTENU — et c'est au professeur qu'on le soumet, avec la
        /// consigne de ne rien rendre s'il n'y a rien à juger. Ce jugement-là
        /// remplace le chronomètre. Le plancher de quatre messages reste :
        /// en dessous, il n'y a pas eu d'échange du tout, et l'appel ne
        /// servirait à rien.
        /// </summary>
        private const int MessagesMinimumVerdict = 4;

        /// <summary>
        /// La dernière fenêtre soumise au filet de conclusion, par conversation
        /// — identifiée par la date de son dernier message.
        ///
        /// POURQUOI CE REGISTRE EXISTE. Une séance trop courte pour les
        /// compétences n'est jamais marquée observée : le balayage la
        /// représente toutes les dix minutes, jusqu'à ce qu'elle grandisse ou
        /// vieillisse de vingt-quatre heures. Sans ce registre, le filet
        /// rappellerait le professeur à chaque passage sur la MÊME fenêtre —
        /// jusqu'à cent quarante-quatre appels facturés pour une séance où il
        /// avait répondu « rien à conclure » dès le premier.
        ///
        /// Statique et en mémoire, parce que le service est recréé à chaque
        /// observation : c'est le seul endroit qui survive d'un passage à
        /// l'autre. Perdu au redémarrage — ce qui coûte au pire un appel de
        /// plus par conversation, jamais une boucle.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, DateTime>
            ConclusionsTentees = new();

        /// <summary>Longueur d'un message dans la transcription analysée.</summary>
        private const int LongueurMessage = 600;

        /// <summary>Combien d exercices notés au plus dans le relevé de séance.</summary>
        private const int RelevesMaximum = 6;

        /// <summary>Combien de questions au plus par évaluation relevée.</summary>
        private const int QuestionsMaximum = 12;

        /// <summary>Longueur d un énoncé, d une réponse ou d un passage relevé.</summary>
        private const int LongueurReleve = 200;

        /// <summary>Longueur du texte attendu et de la production : il faut pouvoir les comparer.</summary>
        private const int LongueurAttendu = 500;

        private readonly AnthropicClient _client;
        private readonly IMaitriseRepository _maitrises;
        private readonly IConversationRepository _conversations;
        private readonly IControleScolaireRepository _controles;
        private readonly IExamenRepository _examens;
        private readonly OptionsClaude _options;
        private readonly IJournalClaudeRepository _journal;
        private readonly ILogger<ObservateurCompetencesService> _logger;

        public ObservateurCompetencesService(
            AnthropicClient client,
            IMaitriseRepository maitrises,
            IConversationRepository conversations,
            IControleScolaireRepository controles,
            IExamenRepository examens,
            IOptions<OptionsClaude> options,
            IJournalClaudeRepository journal,
            ILogger<ObservateurCompetencesService> logger)
        {
            _examens = examens ?? throw new ArgumentNullException(nameof(examens));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _maitrises = maitrises ?? throw new ArgumentNullException(nameof(maitrises));
            _conversations = conversations ?? throw new ArgumentNullException(nameof(conversations));
            _controles = controles ?? throw new ArgumentNullException(nameof(controles));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Exploitable(SeanceAObserver seance)
        {
            if (seance.Messages.Count < MessagesMinimum) return false;

            var duree = seance.Messages[^1].Date - seance.Messages[0].Date;
            return duree >= DureeMinimum;
        }

        public async Task<int> ObserverSeanceAsync(int conversationId, CancellationToken ct = default)
        {
            var seance = await _conversations.GetSeanceAObserverAsync(conversationId, ct);

            // Null : déjà analysée, ou sans message. Rien à faire, et surtout
            // pas de marqueur à avancer.
            if (seance is null) return 0;

            // AVANT LE SEUIL DES COMPÉTENCES, et c'est tout l'intérêt : une
            // préparation écourtée n'apprend rien au moteur de maîtrise, mais
            // elle a quand même eu lieu, et l'enfant doit en retrouver la
            // trace. Le verdict a son propre seuil — voir
            // MessagesMinimumVerdict.
            await ConclurePreparationAsync(seance, ct);

            // Trop courte : on ne marque PAS comme analysée. L'élève peut
            // revenir dans la même conversation cet après-midi ; ces messages
            // feront alors partie d'une séance assez longue pour être lue.
            // Le contrôle ne coûte rien : aucun appel au modèle n'a lieu.
            if (!Exploitable(seance)) return 0;

            var appliquees = await ObserverAsync(seance, ct);

            // Le marqueur avance même sans observation retenue : sinon une
            // séance dont on ne peut rien tirer serait réanalysée à chaque
            // passage du worker, indéfiniment et à chaque fois facturée.
            await _conversations.MarquerObserveeAsync(conversationId, seance.Jusqua, ct);

            return appliquees;
        }

        public async Task<int> ObserverAsync(SeanceAObserver seance, CancellationToken ct = default)
        {
            // UNE PRÉPARATION NE SE REFERME JAMAIS SANS SON VERDICT.
            //
            // Voulu par Camara le 13/09/2026 : « une séance de préparation ne
            // doit pas être clôturée sans le bloc ». Le professeur reçoit bien
            // la consigne à chaque tour, mais une consigne s'oublie — et
            // surtout, elle ne peut RIEN quand l'élève ferme l'onglet : il n'y
            // a alors plus de tour du tout, donc plus personne pour l'écrire.
            //
            // POURQUOI ICI. Ce service est le seul endroit du produit où les
            // TROIS sorties se rejoignent : « quitter le cours » et la fin de
            // séance passent par la file, l'onglet fermé par le balayage
            // périodique. Accrocher la conclusion ailleurs obligerait à
            // recoder ce filet une fois par sortie, et il en manquerait une.
            //
            // AVANT `Exploitable`, ET C'EST LE POINT. Le seuil des dix minutes
            // est celui des compétences ; le filet a le sien (quatre messages,
            // et le jugement du professeur). Il a d'abord été placé devant le
            // seuil dans `ObserverSeanceAsync` — la porte de la file — mais
            // laissé derrière lui ici, la porte du balayage périodique. Relevé
            // par Camara le 13/09/2026 : seuil retiré, backend relancé,
            // balayage passé… et rien de conclu, parce qu'une séance de deux
            // minutes prise par le balayage butait sur les dix minutes avant
            // même d'atteindre le filet.
            //
            // Appelé ici ET dans `ObserverSeanceAsync` : les deux sont des
            // points d'entrée réels. Le registre `ConclusionsTentees` rend le
            // double passage inoffensif — la seconde porte trouve la fenêtre
            // déjà soumise.
            await ConclurePreparationAsync(seance, ct);

            if (!Exploitable(seance)) return 0;

            var candidates = (await _maitrises.GetCandidatesAsync(
                seance.MatiereId, seance.NiveauOrdre, MargeAmont, MargeAval,
                seance.NiveauCode, ct)).ToList();

            if (candidates.Count == 0)
            {
                // Pas de compétence au référentiel pour ce niveau : rien à
                // rattacher. C'est un trou de contenu, pas une erreur technique.
                _logger.LogWarning(
                    "Aucune competence au referentiel pour la matiere {MatiereId} au niveau {Niveau} : seance {SeanceId} non observee.",
                    seance.MatiereId, seance.NiveauLibelle, seance.ConversationId);
                return 0;
            }

            var reponse = await AppelerAsync(seance, candidates, ct);
            var observations = Analyser(reponse, candidates);

            // UNE NOTION ÉVALUÉE NE SE REJUGE PAS À L'IMPRESSION.
            //
            // Relevé par Camara le 13/09/2026 : une évaluation à 17,5/20 sur
            // « Utiliser la réciproque de Thalès » a FAIT BAISSER le score, de
            // 0,648 à 0,595. L'observateur avait lu la conversation, vu l'élève
            // hésiter une fois avant de se reprendre, et conclu « hésitant » —
            // pendant que la copie disait trois justes sur quatre.
            //
            // Sa lecture n'était pas absurde : c'est ce que la conversation
            // montre. Mais sur une notion qui vient d'être formellement
            // évaluée, l'impression n'a plus rien à apporter, et tout à
            // abîmer : l'élève a répondu SEUL, sans guidage, question par
            // question, et chaque réponse a reçu un verdict. C'est la mesure la
            // plus forte dont ce produit dispose.
            //
            // Elle est désormais portée au suivi par le chemin de la copie
            // (voir PorterLaCopieAuSuiviAsync) : la laisser passer ici EN PLUS
            // compterait deux fois la même performance, une fois mesurée et une
            // fois devinée. On retire donc ces notions-là, et elles seules.
            //
            // Depuis le 13/09/2026, le professeur ne commente d'ailleurs plus
            // rien pendant un contrôle : la conversation d'une séance
            // d'évaluation ne porte plus aucun signal, et juger dessus
            // reviendrait à juger sur du silence.
            var mesurees = NotionsDejaMesurees(seance, candidates);

            if (mesurees.Count > 0)
            {
                var ecartees = observations.Where(o => mesurees.Contains(o.Code)).ToList();
                observations = observations.Where(o => !mesurees.Contains(o.Code)).ToList();

                if (ecartees.Count > 0)
                {
                    _logger.LogInformation(
                        "{Nombre} observation(s) ecartee(s) : la copie fait foi sur ces notions "
                        + "(eleve {EleveId}, seance {SeanceId}).",
                        ecartees.Count, seance.EleveId, seance.ConversationId);
                }
            }

            if (observations.Count == 0) return 0;

            var appliquees = await _maitrises.AppliquerObservationsAsync(
                seance.EleveId, observations, "Conversation", ct);

            // TRAVAILLER UNE NOTION AU PROGRAMME D'UN CONTRÔLE, C'EST PRÉPARER
            // CE CONTRÔLE — que le professeur ait pensé à le déclarer ou non.
            //
            // C'est le filet du bloc [CONTROLE_NOTIONS], qu'il oubliera : ici
            // on ne dépend plus de ce qu'il écrit, mais de ce qui a été
            // réellement observé. Silencieux : rater ce marquage ne doit pas
            // faire perdre les observations qui viennent d'être appliquées.
            try
            {
                await _controles.MarquerNotionsTravailleesAsync(
                    seance.EleveId, seance.MatiereId,
                    observations.Select(o => o.Code), DateTime.UtcNow, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du marquage des notions travaillees (eleve {EleveId}, matiere {MatiereId}).",
                    seance.EleveId, seance.MatiereId);
            }

            return appliquees;
        }

        /// <summary>
        /// Les codes des compétences qu'une ÉVALUATION de cette séance a déjà
        /// mesurées, et sur lesquelles l'impression n'a donc plus voix.
        ///
        /// SEULEMENT LES ÉVALUATIONS, ET C'EST VOLONTAIRE. Une dictée ou une
        /// compréhension orale ne porte pas sur une notion nommée : leur titre
        /// ne se rattache à aucune compétence, et ce qu'elles apprennent se lit
        /// justement dans le détail des erreurs — c'est le travail de
        /// l'observateur. Une évaluation, elle, ANNONCE la notion qu'elle
        /// mesure et rend un verdict par question.
        ///
        /// Rapprochement sur libellé normalisé, contre la liste fermée des
        /// candidates : exactement le même rapprochement que celui qui a porté
        /// la copie au suivi. S'il échoue des deux côtés, rien n'est écarté et
        /// rien n'est écrit — les deux chemins restent cohérents.
        /// </summary>
        private static HashSet<string> NotionsDejaMesurees(
            SeanceAObserver seance, List<CompetenceCandidate> candidates)
        {
            var mesurees = new HashSet<string>(StringComparer.Ordinal);

            foreach (var exercice in seance.Exercices)
            {
                if (exercice.Note is null) continue;
                if (string.IsNullOrWhiteSpace(exercice.Titre)) continue;

                var connue = candidates.FirstOrDefault(
                    c => c.Code is not null
                        && c.Libelle is not null
                        && LecteurBloc.Normaliser(c.Libelle)
                            == LecteurBloc.Normaliser(exercice.Titre));

                if (connue?.Code is not null) mesurees.Add(connue.Code);
            }

            return mesurees;
        }

        private async Task<string> AppelerAsync(
            SeanceAObserver seance, List<CompetenceCandidate> candidates, CancellationToken ct)
        {
            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 1500,
                System = new List<TextBlockParam> { new() { Text = Consigne(seance) } },
                Messages = new List<MessageParam>
                {
                    new() { Role = Role.User, Content = Corpus(seance, candidates) },
                },

                // Tâche de classement, pas de raisonnement : `low` suffit et
                // divise le coût d'une analyse qui tourne après chaque séance.
                OutputConfig = new OutputConfig { Effort = Effort.Low },
            };

            var texte = new StringBuilder();

            // EN DIFFUSION, L'USAGE ARRIVE EN DEUX FOIS.
            //
            // Le début du message porte les jetons d'entrée et de cache — ils
            // sont connus dès l'envoi. La fin porte ceux de sortie, une fois
            // qu'ils sont écrits. Ne lire que l'un des deux compterait la
            // moitié de l'appel, et toujours la même moitié.
            long entree = 0, sortie = 0, cacheLu = 0, cacheEcrit = 0;
            string? modele = null;

            await foreach (var evenement in _client.Messages.CreateStreaming(parametres, cancellationToken: ct))
            {
                if (evenement.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var bloc))
                {
                    texte.Append(bloc.Text);
                }
                else if (evenement.TryPickStart(out var debut))
                {
                    modele = debut.Message.Model;
                    entree = debut.Message.Usage.InputTokens;
                    cacheLu = debut.Message.Usage.CacheReadInputTokens ?? 0;
                    cacheEcrit = debut.Message.Usage.CacheCreationInputTokens ?? 0;
                    sortie = debut.Message.Usage.OutputTokens;
                }
                else if (evenement.TryPickDelta(out var fin))
                {
                    sortie = fin.Usage.OutputTokens;
                }
            }

            // Ce que cette observation a coûté. Sans cette ligne, le poste
            // dépense en silence : seul le dialogue portait ses jetons.
            await _journal.EnregistrerAsync(
                "observation-competences", modele, entree, sortie, cacheLu, cacheEcrit, ct: ct);

            return texte.ToString();
        }

        /// <summary>
        /// LE FILET DE FIN DE PRÉPARATION : si le professeur n'a pas conclu, on
        /// le lui fait conclure, après coup, sur la transcription.
        ///
        /// POURQUOI ON DEMANDE ENCORE AU MODÈLE PLUTÔT QUE DE DÉDUIRE
        /// ---------------------------------------------------------
        /// La tentation était de composer le verdict à partir des
        /// pourcentages. C'est exactement ce que Camara a refusé le
        /// 13/09/2026 — « on ne devine pas, c'est le professeur qui gère » — et
        /// il a raison : la barre mesure ce qui est acquis, elle ignore ce que
        /// le contrôle demandera. Un seuil rendrait un verdict faux avec
        /// l'aplomb d'un chiffre.
        ///
        /// On ne remplace donc pas son jugement : on le lui REDEMANDE, sur ce
        /// qui s'est réellement passé pendant la séance. C'est le même juge,
        /// simplement rattrapé à la sortie.
        ///
        /// SILENCIEUX ET SANS CONSÉQUENCE EN CAS D'ÉCHEC. Ce filet ne doit
        /// jamais empêcher l'observation des compétences de se faire : perdre
        /// un verdict est regrettable, perdre le suivi de l'élève l'est plus.
        /// </summary>
        public async Task ConclurePreparationAsync(SeanceAObserver seance, CancellationToken ct = default)
        {
            try
            {
                // LE SEUL SEUIL : quatre messages. Voir MessagesMinimumVerdict
                // pour ce qu'il reste de la durée, et pourquoi elle ne juge plus.
                if (seance.Messages.Count < MessagesMinimumVerdict) return;

                // UNE FENÊTRE DÉJÀ SOUMISE NE L'EST PAS DEUX FOIS. Voir
                // ConclusionsTentees. Inscrite AVANT l'appel : un échec du
                // modèle ne doit pas non plus relancer en boucle.
                if (ConclusionsTentees.TryGetValue(seance.ConversationId, out var deja)
                    && deja == seance.Jusqua)
                {
                    return;
                }

                ConclusionsTentees[seance.ConversationId] = seance.Jusqua;

                var debut = seance.Messages.Min(m => m.Date);

                // SEULEMENT UNE SÉANCE DE PRÉPARATION, ET POUR SA CIBLE — voulu
                // par Camara le 14/09/2026. Un cours normal ne rend aucun
                // verdict, même s'il a touché au programme d'un contrôle : ses
                // notions font monter les barres par le suivi, pas par un
                // jugement. Voir ModesSeance.
                var mode = ModesSeance.Normaliser(seance.ModeSeance);

                if (mode == ModesSeance.Examen && !string.IsNullOrWhiteSpace(seance.ModeEpreuveCode))
                {
                    await ConclureExamenAsync(seance, seance.ModeEpreuveCode, debut, ct);
                    return;
                }

                if (mode != ModesSeance.Controle || seance.ModeControleId is not int cible) return;

                var controle = await _controles.GetAsync(seance.EleveId, cible, ct);

                // Un contrôle déjà passé n'a plus de préparation à conclure.
                if (controle is null
                    || controle.MatiereId != seance.MatiereId
                    || controle.DateControle.Date < seance.Jusqua.Date)
                {
                    return;
                }

                // Un verdict rendu pendant la séance suffit : le professeur a
                // fait son travail, on ne repasse pas derrière lui.
                var dejaJuge = await _controles.GetPreparationAsync(seance.EleveId, cible, ct);
                if (dejaJuge?.PretLe is DateTime juge && juge >= debut) return;

                var reponse = await AppelerConclusionAsync(seance, controle, ct);

                // LE MÊME LECTEUR QUE PENDANT LA SÉANCE, et c'est voulu : un
                // seul format de bloc, une seule grammaire de verdicts. Deux
                // analyseurs finiraient par accepter des choses différentes.
                var declare = LecteurControlePret.Lire(reponse);

                if (declare is null)
                {
                    // PAS UNE ERREUR, LE PLUS SOUVENT : la consigne demande
                    // explicitement de ne RIEN rendre quand la séance n'a
                    // porté sur rien. Le verdict précédent reste alors en
                    // place, ce qui est exactement ce qu'on veut — un enfant
                    // qui bavarde cinq minutes n'a pas reculé.
                    _logger.LogInformation(
                        "Aucune conclusion pour le controle {ControleId} (seance "
                        + "{ConversationId}) : rien de travaille, le verdict precedent reste.",
                        controle.Id, seance.ConversationId);
                    return;
                }

                await _controles.PoserVerdictPretAsync(
                    seance.EleveId, controle.Id, seance.MatiereId,
                    declare.Verdict, declare.Observation, DateTime.UtcNow, ct);

                _logger.LogInformation(
                    "Preparation conclue apres coup : controle {ControleId} «{Verdict}» "
                    + "(seance {ConversationId}).",
                    controle.Id, declare.Verdict, seance.ConversationId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la conclusion de preparation (seance {ConversationId}).",
                    seance.ConversationId);
            }
        }

        /// <summary>
        /// Le filet de fin de préparation, pour une épreuve d'examen : même
        /// principe que pour un contrôle — on redemande le jugement du
        /// professeur sur la transcription, on ne le compose jamais.
        /// </summary>
        private async Task ConclureExamenAsync(
            SeanceAObserver seance, string epreuveCode, DateTime debut, CancellationToken ct)
        {
            var epreuve = await _examens.GetPreparationEpreuveAsync(
                seance.EleveId, seance.NiveauCode, epreuveCode, ct);

            var matiere = epreuve?.Matieres.FirstOrDefault(m => m.MatiereId == seance.MatiereId);
            if (epreuve is null || matiere is null) return;

            if (matiere.Preparation.PretLe is DateTime juge && juge >= debut) return;

            var corpus = new StringBuilder();
            corpus.AppendLine(
                $"# Séance de préparation de l'épreuve « {epreuve.Libelle} » ({epreuve.Code}) — "
                + $"{epreuve.ExamenLibelle} {epreuve.Session}, en {matiere.Libelle}");
            corpus.AppendLine();
            corpus.AppendLine("# Le programme de l'épreuve, domaine par domaine");

            foreach (var domaine in matiere.Preparation.Notions
                .GroupBy(n => string.IsNullOrWhiteSpace(n.Domaine) ? "Autres notions" : n.Domaine))
            {
                corpus.AppendLine($"## {domaine.Key}");

                foreach (var notion in domaine)
                {
                    var etat = notion.EtatMesure switch
                    {
                        "acquise" => "tenue",
                        "a-confirmer" => "à confirmer",
                        "en-cours" => "en cours",
                        "fragile" => "fragile",
                        _ => "jamais travaillée",
                    };

                    corpus.AppendLine($"- {notion.Libelle} : {etat}");
                }
            }

            corpus.AppendLine();

            foreach (var message in seance.Messages)
            {
                var contenu = Nettoyer(message.Contenu);
                if (string.IsNullOrWhiteSpace(contenu)) continue;

                corpus.AppendLine(
                    $"{(message.Role == "assistant" ? "Professeur" : "Élève")} : {contenu}");
            }

            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 500,
                System = new List<TextBlockParam> { new() { Text = ConsigneConclusionExamen(epreuve.Code) } },
                Messages = new List<MessageParam>
                {
                    new() { Role = Role.User, Content = corpus.ToString() },
                },
                OutputConfig = new OutputConfig { Effort = Effort.Low },
            };

            var texte = new StringBuilder();

            await foreach (var evenement in _client.Messages.CreateStreaming(parametres, cancellationToken: ct))
            {
                if (evenement.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var bloc))
                {
                    texte.Append(bloc.Text);
                }
            }

            var declare = LecteurExamenPret.Lire(texte.ToString());

            if (declare is null
                || !string.Equals(declare.Epreuve, epreuve.Code, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Aucune conclusion pour l'epreuve {Epreuve} (seance {ConversationId}) : le verdict precedent reste.",
                    epreuve.Code, seance.ConversationId);
                return;
            }

            await _examens.PoserVerdictAsync(
                seance.EleveId, epreuve.Id, seance.MatiereId,
                declare.Verdict, declare.Observation, DateTime.UtcNow, ct);

            _logger.LogInformation(
                "Preparation d'epreuve conclue apres coup : {Epreuve} «{Verdict}» (seance {ConversationId}).",
                epreuve.Code, declare.Verdict, seance.ConversationId);
        }

        private static string ConsigneConclusionExamen(string epreuveCode) => $$"""
            Tu reprends une séance de préparation d'une épreuve d'examen qui
            vient de se terminer, et tu conclus à la place du professeur qui ne
            l'a pas fait : il a manqué de temps, ou l'élève a quitté le cours
            avant la fin. L'élève a ouvert cette séance par le bouton de
            l'épreuve : elle la concernait.

            Ton seul travail est de dire OÙ EN EST L'ÉLÈVE pour cette épreuve,
            à partir de ce que la séance montre et de l'état du programme — et
            rien d'autre.

            # Ce que tu rends, et uniquement cela

            [EXAMEN_PRET]
            epreuve: {{epreuveCode}}
            pret: pas_pret
            observation: Tu as bien avancé, mais tout le programme n'est pas encore tenu.

            Calcul littéral — développer et factoriser, c'est tenu : tu l'as fait seul, sans relance.

            Fonctions — en cours : tu confonds encore image et antécédent.

            Géométrie dans l'espace et probabilités — pas encore travaillées, ce seront nos prochaines séances.
            [/EXAMEN_PRET]

            `pret` vaut `pret`, `bientot` ou `pas_pret`. Rien d'autre.

            L'observation s'écrit en paragraphes, comme dans l'exemple : une
            phrase d'ensemble, puis un paragraphe par domaine, séparés par une
            ligne vide, chacun ouvert par le nom du domaine et un tiret long
            « — ». Jamais de deux-points juste après ce nom, jamais de puces.

            # SI RIEN N'A ÉTÉ TRAVAILLÉ, N'ÉCRIS RIEN

            Des salutations, une intention, un mot isolé : il ne s'est rien
            passé sur quoi juger. Réponds une ligne vide. Ton verdict REMPLACE
            le précédent : un verdict écrit sur du vide l'écraserait.

            # L'observation passe en revue le programme DOMAINE PAR DOMAINE

            Ce qui est tenu, ce qui pèche, ce qui n'a pas encore été vu — et
            pourquoi ce verdict. « Prêt » veut dire que TOUT le programme de
            l'épreuve est tenu : une séance ne suffit pas à le prouver. C'est à
            un enfant que tu parles : des phrases courtes, pas de jargon, pas
            de pourcentages recopiés, et un chemin plutôt qu'un constat.
            """;

        private async Task<string> AppelerConclusionAsync(
            SeanceAObserver seance, ControleScolaireEleve controle, CancellationToken ct)
        {
            var corpus = new StringBuilder();
            corpus.AppendLine(
                $"# Séance de préparation du contrôle n° {controle.Id} — "
                + $"{controle.MatiereLibelle}, le {controle.DateControle:dd/MM}");
            corpus.AppendLine($"Sujet : {controle.Sujet ?? "non précisé"}");
            corpus.AppendLine();

            // LE PROGRAMME COMPLET, ET PAS SEULEMENT CETTE SÉANCE.
            //
            // Relevé par Camara le 13/09/2026 : ce filet ne recevait que la
            // transcription de la dernière séance. Il a donc déclaré l'élève
            // « prêt » en ne parlant que de la notion travaillée ce jour-là —
            // la seconde du programme n'était pas nommée, alors que c'est la
            // maîtrise des DEUX qui rendait le verdict vrai. On ne juge pas un
            // programme sur la partie qu'on en voit.
            //
            // Sur `EtatMesure` et non `Etat` : l'encouragement affiché à
            // l'enfant ferait passer une notion fragile pour consolidée.
            static string EtatPourConclusion(string etatMesure) => etatMesure switch
            {
                "acquise" => "tenue",
                "a-confirmer" => "à confirmer",
                "en-cours" => "en cours",
                "fragile" => "fragile",
                _ => "jamais travaillée",
            };

            var preparation = await _controles.GetPreparationAsync(seance.EleveId, controle.Id, ct);

            corpus.AppendLine("# Le programme du contrôle, à couvrir EN ENTIER dans l'observation");
            if (preparation is null || preparation.Notions.Count == 0)
            {
                corpus.AppendLine("Pas encore connu.");
            }
            else
            {
                foreach (var notion in preparation.Notions)
                {
                    corpus.AppendLine($"- {notion.Libelle} : {EtatPourConclusion(notion.EtatMesure)}");
                }
            }

            corpus.AppendLine();

            // LES DIFFICULTÉS LIÉES, BORNÉES À LA PRÉPARATION. Une lacune
            // observée des mois avant que le contrôle soit posé n'a rien à
            // faire dans sa justification ; une division qui a bloqué la
            // semaine dernière, si — elle peut coûter des points le jour même.
            // Les notions du programme en sont retirées : elles sont déjà
            // listées au-dessus, avec leur état.
            var programme = preparation?.Notions
                .Where(n => n.CompetenceId.HasValue)
                .Select(n => n.CompetenceId!.Value)
                .ToHashSet() ?? new HashSet<int>();

            var difficultes = (await _maitrises.GetLacunesAsync(
                    seance.EleveId, seance.MatiereId, SeuilsMaitrise.Fragile, 10))
                .Where(m => m.DerniereEvaluation >= controle.DateCreation
                    && !programme.Contains(m.CompetenceId))
                .Take(4)
                .ToList();

            if (difficultes.Count > 0)
            {
                corpus.AppendLine("# Difficultés observées pendant la préparation, hors programme");

                foreach (var difficulte in difficultes)
                {
                    corpus.AppendLine($"- {difficulte.Libelle}");
                }

                corpus.AppendLine();
            }

            foreach (var message in seance.Messages)
            {
                var contenu = Nettoyer(message.Contenu);
                if (string.IsNullOrWhiteSpace(contenu)) continue;

                corpus.AppendLine(
                    $"{(message.Role == "assistant" ? "Professeur" : "Élève")} : {contenu}");
            }

            var parametres = new MessageCreateParams
            {
                Model = _options.Modele(TypeTache.Dialogue),
                MaxTokens = 400,
                System = new List<TextBlockParam> { new() { Text = ConsigneConclusion(controle) } },
                Messages = new List<MessageParam>
                {
                    new() { Role = Role.User, Content = corpus.ToString() },
                },
                OutputConfig = new OutputConfig { Effort = Effort.Low },
            };

            var texte = new StringBuilder();

            await foreach (var evenement in _client.Messages.CreateStreaming(parametres, cancellationToken: ct))
            {
                if (evenement.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var bloc))
                {
                    texte.Append(bloc.Text);
                }
            }

            return texte.ToString();
        }

        private static string ConsigneConclusion(ControleScolaireEleve controle) => $$"""
            Tu reprends une séance de préparation de contrôle qui vient de se
            terminer, et tu conclus à la place du professeur qui ne l'a pas
            fait : il a manqué de temps, ou l'élève a quitté le cours avant la
            fin.

            Ton seul travail est de dire OÙ EN EST L'ÉLÈVE pour ce contrôle, à
            partir de ce que la séance montre — et rien d'autre.

            # Ce que tu rends, et uniquement cela

            [CONTROLE_PRET]
            controle: {{controle.Id}}
            pret: bientot
            observation: Tu es presque prêt.

            Théorème de Thalès — bien acquis : tu poses les rapports sans hésiter.

            Réciproque de Thalès — pas encore sûre : tu conclus avant de vérifier que les rapports sont égaux, on la reprend au prochain cours.

            Divisions décimales — attention, elles t'ont fait perdre du temps.
            [/CONTROLE_PRET]

            `pret` vaut `pret`, `bientot` ou `pas_pret`. Rien d'autre.

            L'observation s'écrit en paragraphes, comme dans l'exemple : une
            phrase d'ensemble, puis un paragraphe par notion, séparés par une
            ligne vide, chacun ouvert par le nom de la notion et un tiret long
            « — ». Jamais de deux-points juste après ce nom, jamais de puces.

            # Comment tu juges

            SUR CE QUI S'EST PASSÉ DANS LA SÉANCE, jamais sur une impression
            générale : ce qu'il a su faire seul, ce qu'il a fallu reprendre, ce
            qui n'a pas été abordé du tout.

            # CETTE SÉANCE A ÉTÉ OUVERTE POUR PRÉPARER CE CONTRÔLE

            L'élève a cliqué sur le bouton de ce contrôle pour l'ouvrir : tu
            n'as pas à te demander si elle le concernait. Ta seule question
            est de savoir s'il s'y est passé quelque chose.

            # SI RIEN N'A ÉTÉ TRAVAILLÉ NON PLUS, N'ÉCRIS RIEN

            Une séance peut n'être que des salutations et une intention :
            « bonjour », « on fait quoi ? », « les fractions », « d'accord ».
            Il ne s'y est RIEN passé sur quoi juger : même réponse, une ligne
            vide.

            C'est important, et voici pourquoi : ton verdict REMPLACE le
            précédent. Un élève que le professeur avait trouvé « bientôt prêt »
            hier, qui ouvre son cours aujourd'hui, bavarde cinq minutes et
            repart, ne doit pas voir son statut retomber. Il n'a pas reculé —
            il n'a simplement rien fait de plus. Le silence garde ce qui avait
            été jugé ; un verdict écrit sur du vide l'écrase.

            En revanche, dès qu'il a produit quelque chose — une réponse, un
            calcul, une explication, une erreur corrigée avec toi —, la séance
            compte, même courte. Elle ne le rend pas prêt pour autant : dis
            alors `pas_pret` ou `bientot`, et nomme ce qu'il reste à voir.

            # L'observation passe en revue TOUT le programme

            Ton contexte te donne le programme complet du contrôle, avec l'état
            de chaque notion. L'observation les couvre TOUTES — pas seulement
            celle travaillée pendant cette séance. « Prêt » veut dire que tout
            ce qui tombera est tenu : justifier le verdict par une seule notion
            sur deux ne le justifie pas.

            Dans cet ordre :

            1. CHAQUE NOTION DU PROGRAMME, nommée, avec où il en est et ce qui
               le montre — une phrase courte par notion ;
            2. LES DIFFICULTÉS LIÉES vues pendant la préparation, même hors
               programme — la liste « Difficultés observées » de ton contexte,
               et ce que la séance montre. S'il n'y en a aucune, n'en invente
               pas ;
            3. CE QUE ÇA DONNE : pourquoi ce verdict, et quoi faire ensuite.

            # UN « PRÊT » SE PROUVE, IL NE S'AFFIRME PAS

            Voulu par Camara le 13/09/2026 : « la justification doit justifier
            pourquoi je suis prêt ». Si ton verdict est `pret`, l'observation
            dit, pour CHAQUE notion du programme, ce qui montre qu'elle est
            tenue : une note obtenue (« validée à 17,5 »), un exercice réussi
            seul et sans relance, un raisonnement mené jusqu'au bout. Un « tu es
            prêt » sans preuve nommée n'est pas un verdict, c'est un
            encouragement — et l'enfant ne saurait pas sur quoi il peut compter.
            Même exigence pour `pas_pret` et `bientot` : ce qui manque est nommé,
            et ce qui est déjà tenu aussi.

            # L'observation s'adresse à l'ENFANT

            À la deuxième personne, en phrases courtes, sans pourcentages
            recopiés. Elle NOMME les notions — « il te reste la réciproque de
            Thalès à consolider », jamais « continue tes efforts ».
            Encourageante même quand le verdict ne l'est pas : un enfant a
            besoin d'un chemin, pas d'un constat.

            N'écris RIEN d'autre que ce bloc : ni salutation, ni explication.
            Personne ne lira ta réponse, seul le bloc est enregistré.
            """;

        private static string Consigne(SeanceAObserver seance) => $$"""
            Tu analyses la séance de travail d'un élève pour en tirer un constat
            de compétences. Tu n'es pas le professeur : tu es l'observateur qui
            regarde ce qui s'est passé.

            L'élève s'appelle {{seance.ElevePrenom}} et il est en {{seance.NiveauLibelle}}.

            # Ta tâche

            Rattache ce qui s'est réellement passé aux compétences de la liste
            fournie, et pour chacune dis ce que tu as constaté :

            - "reussi"   : l'élève y arrive seul, ou après un simple rappel.
            - "hesitant" : il trouve, mais avec de l'aide, des détours, des
                           reprises. C'est le cas le plus fréquent, ne l'évite pas.
            - "echoue"   : il n'y arrive pas, ou son raisonnement est faux au fond.

            # Règles strictes

            N'utilise QUE les codes de la liste. Un code inventé sera rejeté.

            N'observe QUE ce que la séance montre. Si l'élève n'a fait que trois
            additions, tu ne peux rien dire de la géométrie. Une liste courte et
            juste vaut infiniment mieux qu'une liste longue et supposée.

            Trois à six observations au maximum. Au-delà, tu extrapoles.

            Regarde AUSSI les compétences des niveaux inférieurs : quand un élève
            bute, la cause est souvent en amont. Si ses additions échouent parce
            qu'il ne maîtrise pas le complément à 10, c'est le complément à 10
            qu'il faut observer, pas seulement l'addition.

            LE PROFESSEUR PEUT S'ÊTRE TROMPÉ. Tu juges L'ÉLÈVE, sur ce que
            l'élève a produit — pas sur le verdict que le professeur a rendu.

            Ce cas s'est produit : l'élève donne la bonne réponse du premier
            coup, le professeur répond « attention, c'est l'inverse », trois
            échanges suivent, et le professeur finit par reconnaître que l'élève
            avait raison depuis le début. Vu de loin, la séance ressemble à une
            hésitation. Ce n'en est pas une : c'est un "reussi".

            Donc, quand un aller-retour s'ouvre sur une réponse, regarde OÙ IL
            ABOUTIT. Si le professeur se rétracte, ou si la réponse finalement
            retenue est celle que l'élève avait donnée au départ, l'élève avait
            juste — et le détour ne compte pas contre lui. Ne classe "hesitant"
            que ce qui a réellement demandé de l'aide À L'ÉLÈVE.

            UN EXERCICE NOTÉ PÈSE PLUS QUE L IMPRESSION LAISSÉE PAR LA
            CONVERSATION. Quand la séance porte un relevé de résultats — une
            évaluation, une dictée, une compréhension orale — c est du mesuré,
            pas du ressenti : une question fausse est un "echoue" sur la
            compétence qu elle teste, une dictée dont la copie s écarte du
            texte dicté dit exactement sur quoi l élève bute. Rattache ces
            faits-là en priorité.

            Attention tout de même à ne pas compter deux fois : si la
            conversation et le relevé parlent du même moment, c est UNE
            observation, pas deux.

            Si la séance ne permet aucun constat solide — bavardage, séance
            écourtée, aucun exercice — renvoie une liste vide. C'est une réponse
            valide et souvent la bonne.

            # Format

            Réponds UNIQUEMENT avec un objet JSON, sans texte autour ni balises
            de code :

            {
              "observations": [
                { "code": "CODE_EXACT_DE_LA_LISTE", "resultat": "reussi", "indice": "ce qui te le fait dire, en quelques mots" }
              ]
            }
            """;

        private static string Corpus(SeanceAObserver seance, List<CompetenceCandidate> candidates)
        {
            var texte = new StringBuilder();

            texte.AppendLine("# Compétences disponibles");
            texte.AppendLine();

            foreach (var groupe in candidates.GroupBy(c => c.NiveauLibelle))
            {
                texte.AppendLine($"## {groupe.Key}");
                foreach (var candidate in groupe)
                {
                    texte.AppendLine($"- {candidate.Code} : {candidate.Libelle}");
                }
                texte.AppendLine();
            }

            // LE RELEVÉ AVANT LA CONVERSATION, PARCE QU IL PÈSE PLUS QU ELLE.
            var mesures = ResultatsMesures(seance);
            if (!string.IsNullOrWhiteSpace(mesures))
            {
                texte.AppendLine("# Résultats mesurés pendant la séance");
                texte.AppendLine();
                texte.Append(mesures);
            }

            texte.AppendLine($"# Séance de {seance.ElevePrenom} — {seance.MatiereLibelle}");
            texte.AppendLine();

            foreach (var message in seance.Messages)
            {
                var contenu = Nettoyer(message.Contenu);
                if (string.IsNullOrWhiteSpace(contenu)) continue;

                texte.AppendLine($"{(message.Role == "assistant" ? "Professeur" : "Élève")} : {contenu}");
            }

            return texte.ToString();
        }

        /// <summary>
        /// CE QUI A ETE MESURE PENDANT LA SEANCE, POSE A PART DE LA CONVERSATION.
        ///
        /// Les blocs d exercices sont retires de la transcription (voir
        /// <see cref="Nettoyer"/>) : ils sont longs, et l observateur doit
        /// juger les echanges, pas recopier la conclusion du professeur.
        /// Consequence non voulue : AUCUN RESULTAT NOTE N ATTEIGNAIT
        /// L OBSERVATEUR. Une evaluation a 6 sur 20 et une dictee ratee
        /// laissaient la fiche exactement dans l etat ou elles l avaient
        /// trouvee — releve le 10/09/2026, cinquante-six lignes de maitrise en
        /// base, toutes de source « Conversation », aucune venue d un exercice.
        ///
        /// Ce releve n est pas le verdict du professeur : c est la liste des
        /// FAITS produits par l eleve — un enonce, ce qu il a repondu, si
        /// c etait juste. La consigne demande toujours a l observateur de juger
        /// l eleve, pas le professeur ; elle lui donne seulement, en plus de
        /// l impression laissee par la conversation, ce qui a ete mesure.
        /// </summary>
        private static string ResultatsMesures(SeanceAObserver seance)
        {
            var releves = new StringBuilder();

            // LES DERNIERS EXERCICES, PAS LES PREMIERS.
            //
            // Une conversation n est pas une seance : celle de Bilal en compte
            // sept cent quarante-sept messages. A la premiere observation, la
            // fenetre couvre tout le fil — prendre les six premiers exercices
            // aurait releve le travail de la semaine derniere en ignorant celui
            // d aujourd hui. La liste arrive triee par date : on prend la fin.
            foreach (var exercice in seance.Exercices.TakeLast(RelevesMaximum))
            {
                var titre = string.IsNullOrWhiteSpace(exercice.Titre)
                    ? "sans intitule"
                    : exercice.Titre!.Trim();

                var entete = $"## {exercice.Genre} — {titre}";
                if (exercice.Langue is not null) entete += $" (en {exercice.Langue})";
                if (exercice.Note is not null) entete += $" — {exercice.Note:0.#}/20";

                releves.AppendLine(entete);

                // LE GRAIN UTILE EST LA QUESTION, PAS LA NOTE GLOBALE.
                //
                // « 12/20 » ne se rattache a aucune competence. « faux sur
                // additionner deux fractions » s y rattache tout seul.
                foreach (var question in exercice.Questions.Take(QuestionsMaximum))
                {
                    var verdict = string.IsNullOrWhiteSpace(question.Verdict)
                        ? "sans verdict"
                        : question.Verdict!.Trim();

                    releves.AppendLine(
                        $"- {Abreger(question.Enonce, LongueurReleve)} : {verdict}");
                }

                // LES DEUX TEXTES D UNE DICTEE, PAS UN COMPTE D ERREURS.
                //
                // Un compte se resume a un nombre, et un nombre ne dit pas de
                // quoi il retourne. Avec le texte dicte et la copie en regard,
                // l observateur voit si la faute porte sur un accord, un
                // homophone ou une conjugaison — et c est cela qui se rattache
                // a une competence.
                if (!string.IsNullOrWhiteSpace(exercice.Attendu))
                {
                    var etiquette = exercice.Langue is null
                        ? "Texte dicte"
                        : "Passage ENTENDU, jamais lu";

                    releves.AppendLine(
                        $"{etiquette} : {Abreger(exercice.Attendu, LongueurAttendu)}");
                }

                if (!string.IsNullOrWhiteSpace(exercice.Production))
                {
                    releves.AppendLine(
                        $"Ce que l eleve a produit : {Abreger(exercice.Production, LongueurAttendu)}");
                }

                if (!string.IsNullOrWhiteSpace(exercice.Bilan))
                {
                    releves.AppendLine($"Bilan : {Abreger(exercice.Bilan, LongueurReleve)}");
                }

                releves.AppendLine();
            }

            return releves.ToString();
        }

        /// <summary>Coupe proprement, sans jamais rendre null.</summary>
        private static string Abreger(string? texte, int longueur)
        {
            var propre = (texte ?? "").Replace('\n', ' ').Replace('\r', ' ').Trim();
            return propre.Length > longueur ? propre[..longueur] + "…" : propre;
        }


        /// <summary>
        /// L'ardoise est conservée ici, contrairement au bilan destiné au parent :
        /// c'est précisément dans les calculs posés que se lit ce que l'élève
        /// sait faire. Seules les balises disparaissent.
        /// </summary>
        /// <summary>Les balises d ecoute, une par langue enseignee.</summary>
        private static readonly System.Text.RegularExpressions.Regex BaliseEcoute =
            new(@"\[(?:EN|FR|ES|DE|IT|ZH)\](?<passage>.*?)\[/(?:EN|FR|ES|DE|IT|ZH)\]",
                System.Text.RegularExpressions.RegexOptions.Singleline
                    | System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static string Nettoyer(string contenu)
        {
            // Le bloc d'évaluation part en entier, pas seulement ses balises :
            // il contient déjà un verdict, et l'observateur doit se prononcer
            // sur les échanges eux-mêmes, pas recopier la conclusion du
            // professeur.
            var texte = LecteurFiche.Retirer(LecteurRapport.Retirer(LecteurEvaluation.Retirer(contenu)));

            // Un simple numéro entre deux balises : sans ce retrait,
            // l'observateur lirait « 14 » au milieu d'une phrase du professeur.
            texte = LecteurEvaluationCorrigee.Retirer(texte);

            // LES ARCHIVES D EXERCICE PARTENT AUSSI, ET POUR LA MEME RAISON.
            //
            // [DICTEE_CORRIGEE] et [COMPREHENSION_ORALE] portent le verdict
            // deja rendu par le professeur, et ils sont longs : plusieurs
            // centaines de caracteres qui poussaient l echange lui-meme
            // au-dela de la troncature. L observateur recevait le bloc et
            // perdait la conversation dont il devait juger.
            //
            // Ce qu ils contiennent de mesurable n est pas perdu pour autant :
            // il revient par ResultatsMesures, lu depuis les archives.
            texte = LecteurComprehensionOrale.Retirer(LecteurDictee.Retirer(texte));

            // CE QUI A ETE ECOUTE DOIT SE LIRE COMME TEL.
            //
            // Les balises d ecoute disparaissaient en silence : le passage
            // restait, mais rien ne disait qu il avait ete ENTENDU et non lu.
            // Or c est exactement ce qui distingue une competence de
            // comprehension orale d une competence de lecture — et sans ce
            // signe, aucune observation ne se rattachait a l oral.
            texte = BaliseEcoute.Replace(texte, "(passage lu a voix haute : ${passage})");

            // La proposition de jeu est une balise pour l'application, pas un
            // échange : l'observateur n'a rien à en tirer.
            texte = Jeux.LecteurJeu.Retirer(texte);

            texte = texte
                .Replace("[ARDOISE]", " ")
                .Replace("[/ARDOISE]", " ")
                .Replace("[ALERTE_ADULTE]", " ")
                .Replace("[FIN_SEANCE]", " ")
                .Replace("[TABLEAU_EFFACE]", " ")
                .Replace("[DEMANDE_DOCUMENT]", " ")
                .Trim();

            return texte.Length > LongueurMessage ? texte[..LongueurMessage] + "…" : texte;
        }

        private List<ObservationCompetence> Analyser(
            string reponse, List<CompetenceCandidate> candidates)
        {
            var resultats = new List<ObservationCompetence>();

            var debut = reponse.IndexOf('{');
            var fin = reponse.LastIndexOf('}');
            if (debut < 0 || fin <= debut) return resultats;

            try
            {
                using var document = JsonDocument.Parse(reponse[debut..(fin + 1)]);

                if (!document.RootElement.TryGetProperty("observations", out var observations)
                    || observations.ValueKind != JsonValueKind.Array)
                {
                    return resultats;
                }

                var connus = candidates
                    .Where(c => c.Code is not null)
                    .Select(c => c.Code!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var element in observations.EnumerateArray())
                {
                    var code = element.TryGetProperty("code", out var c) ? c.GetString() : null;
                    if (code is null || !connus.Contains(code)) continue;

                    var brut = element.TryGetProperty("resultat", out var r) ? r.GetString() : null;
                    var indice = element.TryGetProperty("indice", out var i) ? i.GetString() : null;

                    var resultat = Convertir(brut);

                    // Verdict illisible : on n observe rien plutot que de
                    // deposer un constat qu on n a pas lu. Voir Convertir.
                    if (resultat is null)
                    {
                        _logger.LogWarning(
                            "Observation ignoree : resultat illisible « {Brut} » sur la competence {Code}.",
                            brut, code);
                        continue;
                    }

                    // UNE COMPETENCE, UNE OBSERVATION PAR SEANCE.
                    //
                    // Le modele repete parfois le meme code, et chaque
                    // repetition etait appliquee : une notion citee deux fois
                    // bougeait deux fois plus vite que les autres, sans qu il
                    // se soit rien passe de plus dans la seance.
                    if (resultats.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    resultats.Add(new ObservationCompetence(code, resultat.Value, indice));
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Reponse d'observation illisible, seance ignoree.");
            }

            return resultats;
        }

        /// <summary>
        /// UNE VALEUR INATTENDUE N EST PAS « HESITANT », ELLE N EST RIEN.
        ///
        /// « Hesitant » se voulait neutre, et ne l etait pas : dans le modele
        /// bayesien, une hesitation FAIT MONTER le score tant qu il est sous
        /// ~60 %. Un champ tronque, un mot mal orthographie par le modele,
        /// une reponse vide se transformaient donc en petite progression — du
        /// bruit qui poussait toujours dans le meme sens, vers le haut. Sur la
        /// fiche d un enfant en difficulte, c est exactement ce qu il ne faut
        /// pas.
        ///
        /// Null : rien de lisible, donc rien d observe. L observation est
        /// ecartee par l appelant.
        /// </summary>
        private static ResultatObservation? Convertir(string? valeur) => valeur?.ToLowerInvariant() switch
        {
            "reussi" or "réussi" or "acquis" or "ok" => ResultatObservation.Reussi,
            "echoue" or "échoué" or "echec" or "échec" or "rate" or "raté" => ResultatObservation.Echoue,
            "hesitant" or "hésitant" or "partiel" or "fragile" => ResultatObservation.Hesitant,
            _ => null,
        };
    }
}
