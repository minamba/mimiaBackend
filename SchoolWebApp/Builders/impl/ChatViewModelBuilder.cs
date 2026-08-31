using System.Runtime.CompilerServices;
using AutoMapper;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Prompts;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Api.ViewModels;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using DomainConversation = SchoolWebApp.Domain.Models.Conversation;
using DomainMessage = SchoolWebApp.Domain.Models.Message;

namespace SchoolWebApp.Api.Builders.impl
{
    public class ChatViewModelBuilder : IChatViewModelBuilder
    {
        /// <summary>
        /// En deçà, on considère que l'élève n'a pas quitté la séance : il a
        /// rechargé la page ou changé d'onglet. Au-delà, c'est un retour, et le
        /// professeur reprend la parole.
        ///
        /// Volontairement court : quitter le cours et y revenir cinq minutes
        /// plus tard EST un retour du point de vue de l'enfant. Seul le
        /// rechargement immédiat doit rester silencieux.
        /// </summary>
        private static readonly TimeSpan SeuilRetour = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Durée de conservation des conversations (RGPD).
        /// Au-delà, une tâche planifiée purge les messages.
        /// </summary>
        private const int JoursConservation = 365;

        /// <summary>
        /// Au-delà de ce silence entre deux tours, on considère que l'élève
        /// n'était pas devant son écran : un onglet laissé ouvert toute la nuit
        /// ne doit pas consommer huit heures de quota.
        /// </summary>
        private static readonly TimeSpan PlafondInactivite = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Ce qu'on impute quand le silence dépasse le plafond — reprise après
        /// une pause, ou premier tour d'une séance. Un échange ordinaire.
        /// </summary>
        private const int SecondesTourNominal = 20;

        /// <summary>Part du pot consommée à partir de laquelle on prévient le parent.</summary>
        private const double SeuilAlerteQuota = 0.80;

        private readonly IMapper _mapper;
        private readonly IChatContexteResolver _resolver;
        private readonly IConversationService _conversationService;
        private readonly IReferentielService _referentielService;
        private readonly IAgentPedagogiqueService _agent;
        private readonly IFileObservation _fileObservation;
        private readonly IEvaluationRepository _evaluations;
        private readonly IRapportRepository _rapports;
        private readonly IFicheRepository _fiches;
        private readonly IAbonnementRepository _abonnements;
        private readonly IFileAlertesQuota _fileAlertes;
        private readonly ILogger<ChatViewModelBuilder> _logger;

        public ChatViewModelBuilder(
            IMapper mapper,
            IChatContexteResolver resolver,
            IConversationService conversationService,
            IReferentielService referentielService,
            IAgentPedagogiqueService agent,
            IFileObservation fileObservation,
            IEvaluationRepository evaluations,
            IRapportRepository rapports,
            IFicheRepository fiches,
            IAbonnementRepository abonnements,
            IFileAlertesQuota fileAlertes,
            ILogger<ChatViewModelBuilder> logger)
        {
            _fiches = fiches ?? throw new ArgumentNullException(nameof(fiches));
            _rapports = rapports ?? throw new ArgumentNullException(nameof(rapports));
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _fileAlertes = fileAlertes ?? throw new ArgumentNullException(nameof(fileAlertes));
            _evaluations = evaluations ?? throw new ArgumentNullException(nameof(evaluations));
            _fileObservation = fileObservation ?? throw new ArgumentNullException(nameof(fileObservation));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            _referentielService = referentielService ?? throw new ArgumentNullException(nameof(referentielService));
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> GetMatiereCodeAsync(int conversationId)
        {
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId);
            return conversation?.MatiereCode;
        }

        public async Task<ConversationViewModel?> CreerConversationAsync(CreerConversationRequest model)
        {
            var eleve = await _resolver.ResoudreEleveAsync(model.EleveId);
            if (eleve is null) return null;

            var matieres = await _referentielService.GetMatieresAsync(activesSeulement: true);
            var matiere = matieres.FirstOrDefault(m => m.Id == model.MatiereId);
            if (matiere is null) return null;

            // LA MATIÈRE DOIT ÊTRE AU PROGRAMME DE SA CLASSE.
            //
            // Ce contrôle manquait, et c'est ce qui a permis à un enfant de
            // sixième d'ouvrir un cours de philosophie. On vérifiait que la
            // matière EXISTE et qu'elle est ouverte — jamais qu'elle le
            // concerne.
            //
            // La grille filtrait déjà, mais un filtre d'interface est un
            // confort : il évite de proposer l'absurde, il n'interdit rien. Le
            // jour où il a cédé — une liste d'élèves pas encore chargée, et il
            // s'ouvrait en grand — plus rien n'était devant.
            //
            // Le professeur y aurait perdu aussi : sa consigne annonce « Classe :
            // 6e » à une professeure de philosophie qui n'existe qu'en
            // terminale. Elle aurait improvisé un cours hors programme à un
            // enfant de onze ans.
            var niveaux = await _referentielService.GetNiveauxScolairesAsync();
            var niveau = niveaux.FirstOrDefault(n => n.Id == eleve.NiveauScolaireId);

            if (!VoiesScolaires.EstAuProgramme(matiere, niveau))
            {
                _logger.LogWarning(
                    "Ouverture refusee : {Matiere} n est pas au programme de {Niveau} (eleve {EleveId}).",
                    matiere.Code, niveau?.Libelle ?? "niveau inconnu", eleve.Id);

                return null;
            }

            var conversation = await _conversationService.AddConversationAsync(new DomainConversation
            {
                EleveId = eleve.Id,
                MatiereId = matiere.Id,
                Titre = string.IsNullOrWhiteSpace(model.Titre) ? matiere.Libelle : model.Titre,
                DateCreation = DateTime.UtcNow,
                DatePurge = DateTime.UtcNow.AddDays(JoursConservation)
            });

            return _mapper.Map<ConversationViewModel>(conversation);
        }

        public async Task<IEnumerable<ConversationViewModel>?> GetConversationsAsync(int eleveId)
        {
            var eleve = await _resolver.ResoudreEleveAsync(eleveId);
            if (eleve is null) return null;

            var conversations = await _conversationService.GetConversationsByEleveAsync(eleve.Id);
            return _mapper.Map<IEnumerable<ConversationViewModel>>(conversations);
        }

        public async Task<IEnumerable<MessageViewModel>?> GetMessagesAsync(int conversationId)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);
            if (contexte is null) return null;

            var messages = (await _conversationService.GetMessagesAsync(
                conversationId, AgentPedagogiqueService.PlancherHistorique)).ToList();

            var vues = _mapper.Map<IEnumerable<MessageViewModel>>(messages).ToList();

            // Les documents en un seul aller-retour, puis répartis. Une requête
            // par message ferait vingt requêtes pour afficher un écran.
            var pieces = (await _conversationService.GetMetadonneesDesMessagesAsync(
                messages.Select(m => m.Id))).ToList();

            if (pieces.Count > 0)
            {
                var parMessage = pieces
                    .Where(p => p.MessageId.HasValue)
                    .ToDictionary(p => p.MessageId!.Value, Projeter);

                foreach (var vue in vues)
                {
                    if (parMessage.TryGetValue(vue.Id, out var piece)) vue.PieceJointe = piece;
                }
            }

            return vues;
        }

        private static PieceJointeViewModel Projeter(Domain.Models.PieceJointe p) => new()
        {
            Id = p.Id,
            NomFichier = p.NomFichier,
            TypeMime = p.TypeMime,
            Taille = p.Taille,
            NombrePages = p.NombrePages,
            EstImage = !p.EstPdf,
            Consultable = p.Consultable,
        };

        public async Task<ResultatPieceJointe> AjouterPieceJointeAsync(
            int conversationId, string? nomFichier, string? typeAnnonce,
            byte[] donnees, CancellationToken ct = default)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);
            if (contexte is null) return new ResultatPieceJointe(null, null, Autorise: false);

            var verdict = ValidationPieceJointe.Valider(donnees, typeAnnonce);
            if (!verdict.Accepte) return new ResultatPieceJointe(null, verdict.Motif);

            // Le type retenu est celui LU dans les octets, jamais celui annoncé
            // par le navigateur : c'est lui qu'on renverra au modèle, et une
            // erreur ici lui ferait refuser le document.
            var typeReel = ValidationPieceJointe.ReconnaitreType(donnees)!;

            // Le nom d'origine sert d'étiquette, à l'élève comme au professeur.
            // Il n'est jamais utilisé comme chemin — rien n'est écrit sur le
            // disque — mais on le tronque quand même : la colonne est bornée.
            var nom = string.IsNullOrWhiteSpace(nomFichier)
                ? "document"
                : Path.GetFileName(nomFichier).Trim();

            if (nom.Length > 200) nom = nom[^200..];

            var piece = new Domain.Models.PieceJointe
            {
                ConversationId = conversationId,
                NomFichier = nom,
                TypeMime = typeReel,
                Taille = donnees.Length,
                NombrePages = verdict.NombrePages,
                Donnees = donnees,
                DateCreation = DateTime.UtcNow,
            };

            piece.Id = await _conversationService.AjouterPieceJointeAsync(piece, ct);

            _logger.LogInformation(
                "Piece jointe {Id} ({Type}, {Taille} octets, {Pages} pages) deposee sur la conversation {ConversationId}.",
                piece.Id, typeReel, donnees.Length, verdict.NombrePages, conversationId);

            return new ResultatPieceJointe(Projeter(piece), null);
        }

        public async Task<ContenuPieceJointe?> GetPieceJointeAsync(
            int pieceJointeId, CancellationToken ct = default)
        {
            // Métadonnées d'abord : le contrôle d'accès n'a pas besoin des
            // octets, et les charger avant de savoir si on a le droit de les
            // rendre serait exactement le mauvais ordre.
            var entete = await _conversationService.GetPieceJointeAsync(pieceJointeId, ct);
            if (entete is null) return null;

            var contexte = await _resolver.ResoudreConversationAsync(entete.ConversationId);
            if (contexte is null) return null;

            var piece = await _conversationService.GetPieceJointeAvecDonneesAsync(pieceJointeId, ct);
            if (piece?.Donnees is null) return null;

            return new ContenuPieceJointe(
                piece.Donnees, piece.TypeMime ?? "application/octet-stream",
                piece.NomFichier ?? "document");
        }

        public IAsyncEnumerable<string> StreamReponseAsync(
            int conversationId, string contenu, int? pieceJointeId,
            int? secondesRestantes, CancellationToken ct) =>
            StreamTourAsync(conversationId, contenu, accueil: false, ct,
                secondesRestantes: secondesRestantes, pieceJointeId: pieceJointeId);

        /// <summary>
        /// La sortie signalée par une page qui se ferme, SANS identité.
        ///
        /// `sendBeacon` ne transporte aucun en-tête, donc aucun jeton : on ne
        /// peut pas vérifier que l'appelant est bien l'élève. On se contente
        /// donc du strict nécessaire — poser la date de sortie et demander
        /// l'analyse — sans jamais rien lire ni rendre.
        ///
        /// La différence avec `MarquerSortieAsync` tient là : pas de résolution
        /// de contexte, donc pas d'abandon d'évaluation en cours. Un élève qui
        /// ferme son onglet en plein contrôle verra son contrôle abandonné par
        /// le balayage, pas par ce chemin — il vaut mieux ne rien décider sans
        /// savoir qui parle.
        /// </summary>
        public async Task MarquerFermetureAsync(int conversationId)
        {
            await _conversationService.MarquerFermetureAsync(conversationId);

            // Le cours est fini : l'analyse part tout de suite plutôt que
            // d'attendre le balayage. C'est ce qui vide la file en continu au
            // lieu de la laisser s'accumuler par paquets de vingt.
            _fileObservation.Demander(conversationId);
        }

        public async Task MarquerSortieAsync(int conversationId)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId)
                ?? throw new UnauthorizedAccessException(
                    "Conversation inexistante ou n'appartenant pas au compte authentifié.");

            // L'ORDRE COMPTE, et il m'a coûté un accueil sur deux.
            //
            // Le retour de l'élève se reconnaît en comparant la date de sortie
            // au dernier message : `sortie >= dernier.DateCreation`. Or le
            // marqueur d'abandon EST un message. Écrit après la sortie, il
            // devenait le dernier, plus récent qu'elle — l'élève n'était donc
            // plus « parti » aux yeux du serveur, et il revenait dans le
            // silence, sans être salué.
            //
            // Le marqueur d'abord, la date ensuite.
            await AbandonnerEvaluationAsync(conversationId);

            await _conversationService.MarquerSortieAsync(conversationId, contexte.Eleve.Id);

            // Le cours est fini : on demande l'analyse tout de suite plutôt que
            // d'attendre le balayage périodique. La demande est déposée dans une
            // file — l'élève ne patiente pas le temps d'un appel au modèle.
            _fileObservation.Demander(conversationId);
        }

        /// <summary>
        /// L'élève part en plein contrôle : le contrôle est annulé.
        ///
        /// Sans ça, le marqueur d'ouverture restait seul dans l'historique et
        /// le contrôle n'était jamais refermé. L'élève revenait le lendemain,
        /// le professeur relisait le fil et reprenait à la question trois —
        /// alors que l'enfant ne se rappelait plus des deux premières, et que
        /// la note aurait porté sur une copie faite en deux fois, à un jour
        /// d'intervalle. Elle n'aurait rien voulu dire.
        ///
        /// On pose donc un marqueur d'abandon. Il n'y a ni copie ni note à
        /// enregistrer : le contrôle sera reproposé en entier.
        ///
        /// Silencieux en cas d'échec : le départ de l'élève ne doit jamais
        /// dépendre de la bonne fin de cette écriture.
        /// </summary>
        private async Task AbandonnerEvaluationAsync(int conversationId)
        {
            try
            {
                var messages = await _conversationService.GetMessagesAsync(conversationId, 60);

                var duProfesseur = messages
                    .Where(m => m.Role == "assistant")
                    .Select(m => m.Contenu);

                if (!LecteurEvaluation.EstOuvert(duProfesseur)) return;

                await _conversationService.AddMessageAsync(new DomainMessage
                {
                    ConversationId = conversationId,
                    Role = "assistant",
                    Contenu = LecteurEvaluation.Abandon,
                    DateCreation = DateTime.UtcNow,
                });

                _logger.LogInformation(
                    "Evaluation abandonnee : l'eleve a quitte la conversation {ConversationId} en plein controle.",
                    conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'annulation d'evaluation a la sortie de la conversation {ConversationId}.",
                    conversationId);
            }
        }

        public IAsyncEnumerable<string> StreamAccueilAsync(
            int conversationId, CancellationToken ct) =>
            StreamTourAsync(conversationId, contenu: null, accueil: true, ct);

        public IAsyncEnumerable<string> StreamAnnonceAsync(
            int conversationId, TypeAccueil annonce, CancellationToken ct) =>
            StreamTourAsync(conversationId, contenu: null, accueil: true, ct, annonce);

        private async IAsyncEnumerable<string> StreamTourAsync(
            int conversationId,
            string? contenu,
            bool accueil,
            [EnumeratorCancellation] CancellationToken ct,
            TypeAccueil annonce = TypeAccueil.Aucun,
            int? secondesRestantes = null,
            int? pieceJointeId = null)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId)
                ?? throw new UnauthorizedAccessException(
                    "Conversation inexistante ou n'appartenant pas au compte authentifié.");

            // L'historique est lu AVANT d'enregistrer le message courant :
            // sinon le message de l'élève apparaîtrait deux fois dans le prompt.
            //
            // Fenêtre à PALIERS et non fenêtre glissante : c'est elle qui rend
            // le préfixe cachable. Voir GetFenetreStableAsync.
            var historique = (await _conversationService.GetFenetreStableAsync(
                conversationId,
                AgentPedagogiqueService.PlancherHistorique,
                AgentPedagogiqueService.PasHistorique)).ToList();

            var typeAccueil = TypeAccueil.Aucun;
            TimeSpan? depuis = null;

            // Le document joint à CE tour, s'il a passé les contrôles.
            Domain.Models.PieceJointe? pieceDuTour = null;

            // Annonce déclenchée par le minuteur de séance : elle ne dépend
            // d'aucun délai de silence, c'est l'horloge qui commande.
            if (annonce != TypeAccueil.Aucun)
            {
                typeAccueil = annonce;
            }
            else if (accueil)
            {
                var dernier = historique.LastOrDefault();

                if (dernier is null)
                {
                    typeAccueil = TypeAccueil.PremiereSeance;
                }
                else
                {
                    depuis = DateTime.UtcNow - dernier.DateCreation;

                    // Sortie explicite : l'élève a cliqué « Quitter le cours ».
                    // Aucune raison de deviner à partir d'un délai — on sait
                    // qu'il est parti, on l'accueille.
                    //
                    // Le marqueur est CONSOMMÉ ici, à la décision. Il s'éteignait
                    // auparavant tout seul — le message d'accueil devenait le
                    // dernier message, donc postérieur à la date de sortie — mais
                    // seulement une fois ce message écrit, à la fin de la
                    // génération. Deux requêtes rapprochées lisaient donc toutes
                    // les deux « l'élève est parti », et il recevait deux
                    // bienvenues. Celle qui consomme le marqueur accueille ;
                    // l'autre se tait.
                    var marqueur = contexte.Conversation.DateSortie;

                    var sortieExplicite =
                        marqueur is { } sortie && sortie >= dernier.DateCreation;

                    if (marqueur is not null)
                    {
                        // On efface même un marqueur périmé — un message est
                        // arrivé après la sortie, il ne servira plus à rien.
                        var consomme = await _conversationService
                            .ConsommerSortieAsync(conversationId, ct);

                        if (sortieExplicite && !consomme) yield break;
                    }

                    // Sinon, on retombe sur le délai de silence. En dessous du
                    // seuil l'élève n'est pas « revenu » : il a rechargé la
                    // page, et le resaluer coûterait un appel au modèle.
                    if (!sortieExplicite && depuis < SeuilRetour) yield break;

                    // Parti en plein contrôle : ce n'est pas un retour ordinaire.
                    // Il faut le lui dire, et le lui dire À VOIX HAUTE — sinon il
                    // découvre l'annulation en voyant réapparaître la question un,
                    // et croit à un bug.
                    var controleAbandonne = LecteurEvaluation.EstAbandonne(
                        historique.Where(m => m.Role == "assistant").Select(m => m.Contenu));

                    typeAccueil = controleAbandonne
                        ? TypeAccueil.RetourControleAbandonne
                        : TypeAccueil.Retour;
                }
            }
            else
            {
                var messageEleve = await _conversationService.AddMessageAsync(new DomainMessage
                {
                    ConversationId = conversationId,
                    Role = "user",
                    Contenu = contenu,
                    DateCreation = DateTime.UtcNow
                });

                // Le document rejoint le message qui vient d'être écrit. C'est
                // ce geste, et lui seul, qui le fait entrer dans l'historique :
                // sans lui le professeur verrait la feuille au premier tour
                // puis l'aurait oubliée au second, ce qui est pire que de ne
                // jamais l'avoir vue.
                //
                // La pièce est revérifiée : rien n'empêche un client d'envoyer
                // l'identifiant d'un document déposé sur une AUTRE conversation.
                if (pieceJointeId is int idPiece)
                {
                    var piece = await _conversationService.GetPieceJointeAsync(idPiece, ct);

                    if (piece is not null && piece.ConversationId == conversationId
                        && piece.MessageId is null)
                    {
                        await _conversationService.AttacherAuMessageAsync(idPiece, messageEleve.Id, ct);

                        // L'historique a été lu AVANT d'écrire ce message : le
                        // document n'y est donc pas. On le porte à part pour ce
                        // tour-ci ; dès le tour suivant il arrivera par
                        // l'historique comme n'importe quel autre.
                        pieceDuTour = await _conversationService
                            .GetPieceJointeAvecDonneesAsync(idPiece, ct);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Piece jointe {PieceId} refusee sur la conversation {ConversationId} : "
                            + "inexistante, deja attachee, ou d'une autre conversation.",
                            idPiece, conversationId);
                    }
                }
            }

            // L'API exige que la conversation se termine par un tour utilisateur.
            // Pour l'accueil on fournit un marqueur de contexte, jamais persisté
            // ni affiché : c'est le bloc système Accueil qui porte la consigne.
            var declencheur = typeAccueil switch
            {
                TypeAccueil.FinProche => "[Le minuteur indique qu'il reste cinq minutes de séance.]",
                TypeAccueil.FinSeance => "[Le temps de la séance est écoulé.]",
                TypeAccueil.NouvelleSeance => "[L'élève relance une séance dans la foulée.]",
                TypeAccueil.FinImminente => "[Il reste trente secondes de séance.]",
                TypeAccueil.ClotureProche => "[Le contrôle dépasse l'horaire depuis dix minutes.]",
                TypeAccueil.ClotureForcee => "[Le délai supplémentaire est épuisé, il faut clôturer.]",
                TypeAccueil.RetourControleAbandonne =>
                    "[L'élève revient. Il était parti en plein contrôle : celui-ci est annulé.]",
                TypeAccueil.Aucun => contenu!,
                _ => "[L'élève vient d'ouvrir la séance et met son casque.]",
            };

            // Le minuteur voyage AVEC le tour de l'élève, et non dans le prompt
            // système. Sa valeur change à chaque message : placé en amont, il
            // interdisait la mise en cache de tout ce qui le suivait, à
            // commencer par l'historique de la conversation. Les consignes qui
            // vont avec sont dans le noyau, donc mises en cache une fois pour
            // toutes ; ici ne circule qu'un chiffre.
            //
            // Il n'est pas persisté : le message de l'élève reste propre en base
            // et à l'affichage.
            if (secondesRestantes is int restant && restant > 0)
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurTemps(
                    restant, contexte.Eleve.NiveauCycle, contexte.Eleve.Age);
            }

            // Un accueil de retour n'a de sens que si l'agent voit la dernière
            // séance : c'est l'historique qui porte « où on en était ».

            var bilan = new TaskCompletionSource<BilanTour>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            // Dialogue courant : Sonnet. Le diagnostic et la correction d'exercice
            // multi-étapes passeront TypeTache.Complexe pour basculer sur Opus.
            // Les documents des tours précédents encore dans la fenêtre. Un
            // seul aller-retour, et rien du tout quand il n'y en a aucun — ce
            // qui reste le cas de l'immense majorité des séances.
            var piecesHistorique = await _conversationService.GetPiecesDesMessagesAsync(
                historique.Where(m => m.Id > 0).Select(m => m.Id), ct);

            var parMessage = piecesHistorique
                .Where(p => p.MessageId.HasValue)
                .ToDictionary(p => p.MessageId!.Value);

            await foreach (var fragment in _agent.RepondreAsync(
                contexte.Eleve, contexte.Conversation, historique, declencheur, bilan,
                TypeTache.Dialogue, typeAccueil, depuis, secondesRestantes,
                parMessage, pieceDuTour, ct))
            {
                yield return fragment.Texte;
            }

            // Le flux est terminé : on persiste la réponse et la consommation.
            // Si le client s'est déconnecté en cours de route, on enregistre quand
            // même ce qui a été produit — les tokens sont facturés dans tous les cas.
            if (bilan.Task.IsCompletedSuccessfully)
            {
                var resultat = await bilan.Task;

                await _conversationService.AddMessageAsync(new DomainMessage
                {
                    ConversationId = conversationId,
                    Role = "assistant",
                    Contenu = resultat.TexteComplet,
                    Modele = resultat.Modele,
                    TokensEntree = resultat.TokensEntree,
                    TokensSortie = resultat.TokensSortie,
                    TokensCacheLecture = resultat.TokensCacheLecture,
                    TokensCacheEcriture = resultat.TokensCacheEcriture,
                    DateCreation = DateTime.UtcNow
                });

                if (resultat.TexteComplet.Contains("[ALERTE_ADULTE]", StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "ALERTE : signal de detresse detecte dans la conversation {ConversationId} (eleve {EleveId}).",
                        conversationId, contexte.Eleve.Id);
                }

                await EnregistrerEvaluationAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await EnregistrerRapportAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await EnregistrerFicheAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);
            }

            await _conversationService.TouchConversationAsync(conversationId, contexte.Eleve.Id);

            await ImputerQuotaAsync(contexte.Eleve.Id, historique.LastOrDefault()?.DateCreation, ct);
        }

        /// <summary>
        /// Enregistre le compte rendu si le professeur vient de conclure.
        ///
        /// Silencieux en cas d'échec, comme l'évaluation : un bloc mal formé ne
        /// doit jamais faire échouer le tour de parole. C'est la trace qui
        /// manque, pas la séance.
        /// </summary>
        private async Task EnregistrerRapportAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var declare = LecteurRapport.Lire(texte);
            if (declare is null) return;

            try
            {
                var enregistre = await _rapports.EnregistrerAsync(
                    eleveId, conversationId,
                    declare.Travaille, declare.NoteComprehension, declare.NoteRevision,
                    declare.Remarque, declare.ARevoir, ct);

                if (enregistre is null)
                {
                    _logger.LogWarning(
                        "Rapport ignore : conversation {ConversationId} introuvable pour l'eleve {EleveId}.",
                        conversationId, eleveId);
                    return;
                }

                _logger.LogInformation(
                    "Rapport enregistre pour la conversation {ConversationId} "
                    + "(comprehension {Comprehension}, revision {Revision}).",
                    conversationId, declare.NoteComprehension, declare.NoteRevision);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement du rapport (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Enregistre la fiche de révision si le professeur vient d'en rédiger
        /// une. Silencieux en cas d'échec, comme les deux autres blocs.
        /// </summary>
        private async Task EnregistrerFicheAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            // Le message de conclusion porte une fiche par notion travaillée :
            // il peut donc y en avoir plusieurs. Chacune est enregistrée pour
            // elle-même — une fiche mal formée ne doit pas emporter les autres.
            foreach (var declaree in LecteurFiche.LireToutes(texte))
            {
                try
                {
                    var enregistree = await _fiches.EnregistrerAsync(
                        eleveId, conversationId,
                        declaree.Notion, declaree.Domaine, declaree.Contenu, declaree.Etat, ct);

                    if (enregistree is null)
                    {
                        _logger.LogWarning(
                            "Fiche ignoree : conversation {ConversationId} introuvable pour l'eleve {EleveId}.",
                            conversationId, eleveId);
                        continue;
                    }

                    _logger.LogInformation(
                        "Fiche de revision enregistree pour l'eleve {EleveId} : «{Notion}» ({Etat}).",
                        eleveId, declaree.Notion, enregistree.Etat);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Echec de l'enregistrement de la fiche «{Notion}» (conversation {ConversationId}).",
                        declaree.Notion, conversationId);
                }
            }
        }

        public async Task<VerdictQuota> VerifierQuotaAsync(
            int conversationId, CancellationToken ct = default)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);

            return contexte is null
                ? VerdictQuota.Refus(MotifRefus.SansAbonnement)
                : await _abonnements.VerifierAsync(contexte.Eleve.Id, ct);
        }

        /// <summary>
        /// Impute le temps écoulé depuis le tour précédent.
        ///
        /// On mesure le temps RÉEL passé en séance plutôt que de facturer un
        /// forfait par tour : c'est ce que l'élève voit sur son chronomètre, et
        /// c'est ce qui correspond au coût — la voix se facture à la durée.
        /// </summary>
        private async Task ImputerQuotaAsync(
            int eleveId, DateTime? tourPrecedent, CancellationToken ct)
        {
            var ecoule = tourPrecedent is { } precedent
                ? DateTime.UtcNow - precedent
                : TimeSpan.MaxValue;

            var secondes = ecoule > PlafondInactivite
                ? SecondesTourNominal
                : Math.Max(SecondesTourNominal, (int)ecoule.TotalSeconds);

            try
            {
                var alerte = await _abonnements.ImputerAsync(eleveId, secondes, SeuilAlerteQuota, ct);
                if (alerte) _fileAlertes.Demander(eleveId);
            }
            catch (Exception ex)
            {
                // Un décompte manqué coûte quelques minutes de quota ; une
                // exception ici couperait la réponse du professeur alors qu'elle
                // a déjà été produite et facturée. On trace et on continue.
                _logger.LogError(ex,
                    "Echec du decompte de quota pour l'eleve {EleveId}.", eleveId);
            }
        }

        /// <summary>
        /// Enregistre la note si le professeur vient d'en rendre une.
        ///
        /// Volontairement silencieux en cas d'échec : une balise mal formée ne
        /// doit jamais faire échouer le tour de parole. L'élève a déjà entendu
        /// sa note — c'est la trace qui manque, pas la séance.
        /// </summary>
        private async Task EnregistrerEvaluationAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var declaree = LecteurEvaluation.Lire(texte);
            if (declaree is null) return;

            // Une note sans copie n'est pas une évaluation : c'est un jugement.
            // Le parent ne peut pas la vérifier, l'élève ne peut pas revoir ses
            // erreurs, et elle irait quand même peser sur la courbe de
            // progression. Mieux vaut aucune trace qu'une note injustifiable.
            if (declaree.Questions.Count == 0)
            {
                _logger.LogWarning(
                    "Evaluation REFUSEE (conversation {ConversationId}, eleve {EleveId}) : "
                    + "le bloc «{Notion}» note {Note}/20 ne contient aucune ligne q. "
                    + "Sans questions, la copie serait vide.",
                    conversationId, eleveId, declaree.Notion, declaree.Note);
                return;
            }

            try
            {
                var enregistree = await _evaluations.AjouterAsync(
                    eleveId, conversationId,
                    declaree.Notion, declaree.Note, declaree.Remarque, declaree.ARevoir,
                    declaree.Questions, ct);

                if (enregistree is null)
                {
                    _logger.LogWarning(
                        "Evaluation ignoree : conversation {ConversationId} introuvable pour l'eleve {EleveId}.",
                        conversationId, eleveId);
                    return;
                }

                _logger.LogInformation(
                    "Evaluation enregistree : {Note}/20 pour l'eleve {EleveId} (conversation {ConversationId}).",
                    declaree.Note, eleveId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement d'une evaluation (conversation {ConversationId}).",
                    conversationId);
            }
        }
    }
}
