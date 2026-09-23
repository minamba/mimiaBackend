using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using AutoMapper;
using SchoolWebApp.Api.Request;
using SchoolWebApp.Api.Services;
using SchoolWebApp.Api.Services.Prompts;
using SchoolWebApp.Api.Services.PiecesJointes;
using SchoolWebApp.Api.Services.Voix;
using SchoolWebApp.Api.Utils;
using SchoolWebApp.Api.ViewModels;
using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Repositories;
using SchoolWebApp.Domain.Services;
using DomainConversation = SchoolWebApp.Domain.Models.Conversation;
using DomainMessage = SchoolWebApp.Domain.Models.Message;

namespace SchoolWebApp.Api.Builders.impl
{
    public partial class ChatViewModelBuilder : IChatViewModelBuilder
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

        /// <summary>
        /// LA GOMME DU TABLEAU, POSÉE PAR LE SERVEUR — PAS DEMANDÉE AU MODÈLE.
        ///
        /// Le prompt dit depuis longtemps « le tableau est vide au début de
        /// chaque séance ». C'est une fiction que le modèle est censé jouer,
        /// pas un fait : rien ne vide vraiment `tableauAuto` côté client, qui
        /// remonte tout l'historique de la conversation et retrouve le
        /// dernier contenu écrit, même s'il date d'une séance précédente,
        /// avec un professeur différent, sur un exercice sans rapport.
        ///
        /// Relevé en séance : une dictée sur l'accord du COD au passé composé,
        /// et le tableau affichait la copie corrigée d'une dictée précédente,
        /// sur les jardiniers et les voisins — sans aucun rapport avec ce qui
        /// venait d'être fait. Le professeur ne l'a pas remarqué : il ne
        /// « voit » pas le tableau, il écrit dedans, et rien ne l'oblige à
        /// vérifier ce qui y traînait avant lui.
        ///
        /// On ne demande donc plus au modèle de faire ce geste : on le pose
        /// nous-mêmes, mécaniquement, à chaque VRAIE arrivée dans un cours —
        /// première séance, retour après une pause, retour après un contrôle
        /// abandonné, ou nouvelle séance enchaînée. Jamais sur les relances du
        /// minuteur (fin proche, fin de séance, clôture d'un contrôle) : ce
        /// sont des événements DANS la même séance, pas des arrivées, et ce
        /// qui est au tableau à cet instant reste ce qu'il y a de plus
        /// pertinent à montrer.
        ///
        /// Un modèle qui veut réafficher ce qu'il y avait la dernière fois
        /// peut toujours le faire — il lui suffit d'écrire un nouveau bloc
        /// [ARDOISE] avec ce contenu, comme n'importe quelle autre écriture au
        /// tableau. Ce qu'on lui retire, c'est seulement la possibilité de ne
        /// rien faire et de laisser l'ancien contenu par défaut.
        /// </summary>
        private const string TableauEfface = "[TABLEAU_EFFACE]\n\n";

        private readonly IMapper _mapper;
        private readonly IChatContexteResolver _resolver;
        private readonly IConversationService _conversationService;
        private readonly IReferentielService _referentielService;
        private readonly IAgentPedagogiqueService _agent;
        private readonly IFileObservation _fileObservation;
        private readonly IFileConclusion _fileConclusion;
        private readonly IEvaluationRepository _evaluations;
        private readonly IRapportRepository _rapports;
        private readonly IFicheRepository _fiches;
        private readonly IDicteeRepository _dictees;
        private readonly IComprehensionOraleRepository _comprehensionsOrales;
        private readonly IExpressionOraleRepository _expressionsOrales;
        private readonly IExpressionEcriteRepository _expressionsEcrites;
        private readonly ISyntheseVocaleService _syntheseVocale;
        private readonly IEvaluationPrevueRepository _evaluationsPrevues;
        private readonly IControleScolaireRepository _controles;
        private readonly IExamenRepository _examens;
        private readonly IMaitriseRepository _maitrises;
        private readonly IPlanificateurControleService _planificateur;
        private readonly IAbonnementRepository _abonnements;
        private readonly IFileAlertesQuota _fileAlertes;
        private readonly Services.Jeux.IJeuxService _jeux;
        private readonly ILogger<ChatViewModelBuilder> _logger;

        public ChatViewModelBuilder(
            IMapper mapper,
            IChatContexteResolver resolver,
            IConversationService conversationService,
            IReferentielService referentielService,
            IAgentPedagogiqueService agent,
            IFileObservation fileObservation,
            IFileConclusion fileConclusion,
            IEvaluationRepository evaluations,
            IRapportRepository rapports,
            IFicheRepository fiches,
            IDicteeRepository dictees,
            IComprehensionOraleRepository comprehensionsOrales,
            IExpressionOraleRepository expressionsOrales,
            IExpressionEcriteRepository expressionsEcrites,
            ISyntheseVocaleService syntheseVocale,
            IEvaluationPrevueRepository evaluationsPrevues,
            IControleScolaireRepository controles,
            IMaitriseRepository maitrises,
            IPlanificateurControleService planificateur,
            IAbonnementRepository abonnements,
            IFileAlertesQuota fileAlertes,
            IExamenRepository examens,
            Services.Jeux.IJeuxService jeux,
            ILogger<ChatViewModelBuilder> logger)
        {
            _jeux = jeux ?? throw new ArgumentNullException(nameof(jeux));
            _examens = examens ?? throw new ArgumentNullException(nameof(examens));
            _fiches = fiches ?? throw new ArgumentNullException(nameof(fiches));
            _dictees = dictees ?? throw new ArgumentNullException(nameof(dictees));
            _comprehensionsOrales = comprehensionsOrales ?? throw new ArgumentNullException(nameof(comprehensionsOrales));
            _expressionsOrales = expressionsOrales ?? throw new ArgumentNullException(nameof(expressionsOrales));
            _expressionsEcrites = expressionsEcrites ?? throw new ArgumentNullException(nameof(expressionsEcrites));
            _syntheseVocale = syntheseVocale ?? throw new ArgumentNullException(nameof(syntheseVocale));
            _evaluationsPrevues = evaluationsPrevues ?? throw new ArgumentNullException(nameof(evaluationsPrevues));
            _controles = controles ?? throw new ArgumentNullException(nameof(controles));
            _maitrises = maitrises ?? throw new ArgumentNullException(nameof(maitrises));
            _planificateur = planificateur ?? throw new ArgumentNullException(nameof(planificateur));
            _rapports = rapports ?? throw new ArgumentNullException(nameof(rapports));
            _abonnements = abonnements ?? throw new ArgumentNullException(nameof(abonnements));
            _fileAlertes = fileAlertes ?? throw new ArgumentNullException(nameof(fileAlertes));
            _evaluations = evaluations ?? throw new ArgumentNullException(nameof(evaluations));
            _fileObservation = fileObservation ?? throw new ArgumentNullException(nameof(fileObservation));
            _fileConclusion = fileConclusion ?? throw new ArgumentNullException(nameof(fileConclusion));
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

            if (!VoiesScolaires.EstAuProgramme(matiere, niveau, eleve.Lv2Espagnol, eleve.Specialites))
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
                    .GroupBy(p => p.MessageId!.Value)
                    .ToDictionary(g => g.Key, g => g.Select(Projeter).ToList());

                foreach (var vue in vues)
                {
                    if (!parMessage.TryGetValue(vue.Id, out var liste)) continue;

                    vue.PiecesJointes = liste;
                    // Le singulier garde la première : un navigateur resté sur
                    // la version précédente ne connaît que lui.
                    vue.PieceJointe = liste.FirstOrDefault();
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

            return await EnregistrerPieceAsync(conversationId, nomFichier, typeAnnonce, donnees, ct);
        }

        public Task<ResultatPieceJointe> AjouterPieceJointeParScanAsync(
            int conversationId, string? nomFichier, string? typeAnnonce,
            byte[] donnees, CancellationToken ct = default) =>
            // Le droit a été prouvé par le jeton, en amont — voir l'interface.
            EnregistrerPieceAsync(conversationId, nomFichier, typeAnnonce, donnees, ct);

        public async Task<EnTeteScan?> ResoudreEnTeteScanAsync(int conversationId)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);

            return contexte is null
                ? null
                : new EnTeteScan(contexte.Conversation.ProfPrenom, contexte.Conversation.MatiereLibelle);
        }

        public async Task<PieceJointeViewModel?> GetApercuPieceAsync(
            int conversationId, int pieceJointeId, CancellationToken ct = default)
        {
            if (await _resolver.ResoudreConversationAsync(conversationId) is null) return null;

            var piece = await _conversationService.GetPieceJointeAsync(pieceJointeId, ct);

            return piece is null || piece.ConversationId != conversationId ? null : Projeter(piece);
        }

        public async Task<bool> PoserChoixCopieAsync(
            int conversationId, int controleId, bool separee, CancellationToken ct = default)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);
            if (contexte is null) return false;

            return await _controles.PoserChoixCopieAsync(
                contexte.Eleve.Id, controleId, contexte.Conversation.MatiereId,
                separee, DateTime.UtcNow, ct);
        }

        public async Task<(bool Trouve, bool? Separee)> GetChoixCopieAsync(
            int conversationId, int controleId, CancellationToken ct = default)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);
            if (contexte is null) return (false, null);

            var controle = await _controles.GetAsync(contexte.Eleve.Id, controleId, ct);

            return controle is null || controle.MatiereId != contexte.Conversation.MatiereId
                ? (false, null)
                : (true, controle.CopieSeparee);
        }

        /// <summary>
        /// La validation et l'écriture d'un document, UNE seule fois pour les
        /// deux portes d'entrée — le trombone et le téléphone. Un document
        /// arrivé par le QR code passe exactement les mêmes contrôles.
        /// </summary>
        private async Task<ResultatPieceJointe> EnregistrerPieceAsync(
            int conversationId, string? nomFichier, string? typeAnnonce,
            byte[] donnees, CancellationToken ct)
        {
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
            int conversationId, string contenu, IReadOnlyList<int> pieceJointeIds,
            int? secondesRestantes, string? vitesseEcoute, CancellationToken ct) =>
            StreamTourAsync(conversationId, contenu, accueil: false, ct,
                secondesRestantes: secondesRestantes, pieceJointeIds: pieceJointeIds,
                vitesseEcoute: vitesseEcoute);

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

            // FERMER L'ONGLET DOIT VALOIR « QUITTER LE COURS ».
            //
            // Ce chemin ne demandait aucune conclusion : un élève qui ferme sa
            // page au lieu de cliquer sur le bouton perdait son compte rendu,
            // sa fiche et ses exercices archivés — pour un geste qui, de son
            // point de vue, veut dire exactement la même chose. Le worker sait
            // désormais retrouver seul l'identité du parent, ce qui rendait ce
            // dépôt impossible à l'époque où il a été écrit.
            //
            // Une double demande ne produit pas deux comptes rendus :
            // `ADuTravailNonConcluAsync` referme la porte dès que le premier
            // est écrit.
            _fileConclusion.Demander(conversationId);
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

            // Parti pendant la dictée, avant sa copie : elle est annulée. Le
            // marqueur passe AVANT l'archivage de secours, qui n'a alors rien à
            // archiver — et avant la date de sortie, pour la même raison que
            // celui du contrôle.
            await AbandonnerDicteeAsync(conversationId, contexte.Eleve.Id);
            await ArchiverDicteeAbandonneeAsync(conversationId, contexte.Eleve.Id);

            await _conversationService.MarquerSortieAsync(conversationId, contexte.Eleve.Id);

            // Le cours est fini : on demande l'analyse tout de suite plutôt que
            // d'attendre le balayage périodique. La demande est déposée dans une
            // file — l'élève ne patiente pas le temps d'un appel au modèle.
            _fileObservation.Demander(conversationId);

            // « Quitter le cours » ne redonne jamais la parole au professeur :
            // sans cette file, un vrai travail entamé puis interrompu ne
            // laissait ni compte rendu, ni fiche, ni évaluation prévue — rien
            // de visible sur le calendrier ou la fiche de l'élève. Le worker
            // décide lui-même s'il y a quoi que ce soit à conclure (voir
            // `IConversationService.ADuTravailNonConcluAsync`) : une séance
            // quittée sans un mot ne coûte donc rien.
            _fileConclusion.Demander(conversationId);
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

        /// <summary>
        /// L'élève part avant que le professeur n'ait pu archiver lui-même
        /// la dictée en cours — « Quitter le cours » ne produit aucun
        /// nouveau message, donc aucune occasion d'écrire [DICTEE_CORRIGEE].
        ///
        /// Silencieux en cas d'échec, comme l'abandon d'évaluation juste
        /// au-dessus : le départ de l'élève ne doit jamais en dépendre.
        /// </summary>
        private async Task ArchiverDicteeAbandonneeAsync(int conversationId, int eleveId)
        {
            try
            {
                var archivee = await _dictees.ArchiverAbandonneeAsync(conversationId, eleveId);

                if (archivee is not null)
                {
                    _logger.LogInformation(
                        "Dictee en attente archivee au depart de l'eleve {EleveId} "
                        + "(conversation {ConversationId}).",
                        eleveId, conversationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'archivage de la dictee a la sortie de la conversation {ConversationId}.",
                    conversationId);
            }
        }

        /// <summary>
        /// L'élève part pendant une dictée, avant d'avoir rendu sa copie : la
        /// dictée est ANNULÉE — voulu par Camara le 11/09/2026. Pas
        /// d'archivage, et un marqueur pour que le professeur le lui dise à
        /// son retour, au lieu de reprendre une dictée dont l'enfant ne se
        /// souvient plus.
        ///
        /// Silencieux en cas d'échec, comme l'abandon de contrôle.
        /// </summary>
        private async Task AbandonnerDicteeAsync(int conversationId, int eleveId)
        {
            try
            {
                var etat = await _dictees.EtatInterruptionAsync(conversationId, eleveId);
                if (etat != InterruptionDictee.ANoter) return;

                await _conversationService.AddMessageAsync(new DomainMessage
                {
                    ConversationId = conversationId,
                    Role = "assistant",
                    Contenu = LecteurDictee.Abandon,
                    DateCreation = DateTime.UtcNow,
                });

                _logger.LogInformation(
                    "Dictee annulee : l'eleve a quitte la conversation {ConversationId} avant de rendre sa copie.",
                    conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'annulation de dictee a la sortie de la conversation {ConversationId}.",
                    conversationId);
            }
        }

        /// <summary>
        /// La dictée interrompue à annoncer au retour, s'il y en a une.
        /// Une erreur de lecture ne doit jamais empêcher l'accueil : on
        /// accueille alors comme d'habitude.
        /// </summary>
        private async Task<InterruptionDictee> LireInterruptionDicteeAsync(
            int conversationId, int eleveId, CancellationToken ct)
        {
            try
            {
                return await _dictees.EtatInterruptionAsync(conversationId, eleveId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture de la derniere dictee (conversation {ConversationId}).",
                    conversationId);
                return InterruptionDictee.Aucune;
            }
        }

        /// <summary>
        /// UNE DICTÉE EN ATTENTE SE CORRIGE D'ABORD — voulu par Camara le
        /// 11/09/2026 : la copie est rendue, la séance s'est arrêtée avant la
        /// correction, et le cours suivant commence par elle.
        ///
        /// Porté par le tour d'accueil, avec le texte et la copie archivés :
        /// la dictée peut dater de plusieurs séances, et ne plus figurer dans
        /// la fenêtre d'historique que le professeur a sous les yeux. Une
        /// consigne seule lui aurait demandé de corriger ce qu'il ne voit pas.
        /// </summary>
        private async Task<string?> DicteeEnAttenteAsync(int eleveId, int matiereId, CancellationToken ct)
        {
            try
            {
                // LA PLUS RÉCENTE, PAS LA PLUS ANCIENNE. Relevé par Camara le
                // 11/09/2026 : l'élève revient après une dictée faite au
                // dernier cours, et le professeur lui ressort une vieille
                // dictée qu'il avait demandé d'abandonner. La dernière chose
                // qu'il a faite est celle dont il se souvient — et celle qu'il
                // risque d'oublier si on ne la lui rappelle pas.
                var enAttente = (await _dictees.GetParMatiereAsync(eleveId, matiereId, ct))
                    .Where(d => d.EnAttente)
                    .OrderByDescending(d => d.DateCreation)
                    .FirstOrDefault();

                if (enAttente is null) return null;

                return $"[Une dictée attend sa correction depuis le {enAttente.DateCreation:dd/MM} "
                    + $"(n° {enAttente.Id}) : tu COMMENCES la séance par elle, avant toute autre "
                    + "chose — voir « UNE DICTÉE EN ATTENTE SE CORRIGE D'ABORD ». Pour la "
                    + $"remettre au tableau : [DICTEE_AU_TABLEAU]{enAttente.Id}[/DICTEE_AU_TABLEAU]. "
                    + "S'il ne veut pas la corriger — ou s'il a DÉJÀ demandé plus haut à "
                    + "l'abandonner : dans ce cas ne la lui repropose pas —, écris "
                    + $"[DICTEE_SUPPRIMEE]{enAttente.Id}[/DICTEE_SUPPRIMEE] et passe à autre chose.\n"
                    + "Texte dicté :\n" + enAttente.TexteDicte + "\n"
                    + "Sa copie :\n" + enAttente.Copie + "]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture des dictees en attente de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// UNE COPIE RESTÉE EN PHOTO SE RECOPIE AU COURS SUIVANT — voulu par
        /// Camara le 18/09/2026 : « le prof pourra quand même refaire la
        /// transcription si elle a pas été faite. »
        ///
        /// PORTÉE AVEC L'IMAGE, et c'est tout l'enjeu : le texte peut dater de
        /// plusieurs séances et ne plus figurer dans la fenêtre d'historique que
        /// le professeur a sous les yeux. Une consigne seule lui demanderait de
        /// recopier ce qu'il ne voit pas — exactement le défaut évité sur la
        /// dictée en attente, dont on reprend ici la mécanique.
        ///
        /// LA PHOTO EST JOINTE AU TOUR, comme un document que l'élève viendrait
        /// d'envoyer : c'est le seul chemin par lequel une image entre dans le
        /// raisonnement du modèle.
        /// </summary>
        private async Task<(string Consigne, Domain.Models.PieceJointe Photo)?>
            TexteATranscrireAsync(int eleveId, int matiereId, CancellationToken ct)
        {
            try
            {
                var attente = await _expressionsEcrites.ATranscrireAsync(eleveId, matiereId, ct);
                if (attente is null) return null;

                var consigne =
                    $"[Un texte écrit le {attente.DateCreation:dd/MM} (n° {attente.Id}) attend "
                    + "encore d'être RECOPIÉ : sa photo est jointe à ce tour, et elle est la "
                    + "seule trace qui reste de son travail. Tu COMMENCES la séance par lui.\n"
                    + $"Ce qui était demandé : {attente.Consigne}\n"
                    + "Recopie son texte mot pour mot, fautes comprises, montre-le-lui au "
                    + "tableau sous « Ton texte », corrige-le comme d'habitude, puis écris le "
                    + "bloc [EXPRESSION_ECRITE] avec la ligne `numero: " + attente.Id + "` — "
                    + "titre, langue et consigne sont déjà enregistrés, tu n'as pas à les "
                    + "réécrire. Sans ce bloc, la photo finira par être effacée et son texte "
                    + "sera perdu.]";

                return (consigne, new Domain.Models.PieceJointe
                {
                    NomFichier = "copie.jpg",
                    TypeMime = attente.TypeMime,
                    Taille = attente.Photo.Length,
                    Donnees = attente.Photo,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture des textes a transcrire de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// UN CONTRÔLE À VENIR SE PROPOSE À CHAQUE ARRIVÉE — voulu par
        /// Camara le 12/09/2026, TANT QUE la date n'est pas dépassée.
        /// Contrairement à la dictée en attente, il n'y a pas de garde
        /// anti-répétition : le rappel revient à chaque fois, jusqu'à
        /// l'échéance.
        ///
        /// Le nombre de jours restants est calculé ICI, en heure de Paris
        /// (voir HeureFrance) — jamais laissé au modèle, même raison que le
        /// minuteur.
        /// </summary>
        private async Task<string?> ControleAVenirAsync(
            int eleveId, int matiereId, int controleCible, CancellationToken ct)
        {
            try
            {
                // UNE SÉANCE DE PRÉPARATION PARLE DE SON CONTRÔLE, ET DE LUI
                // SEUL — celui du bouton sur lequel l'élève a cliqué, même si un
                // autre tombe plus tôt. Voir ModesSeance.
                // L'HEURE DE PARIS, PAS SEULEMENT LA DATE : c'est elle qui dit
                // si le contrôle de 18 h a déjà commencé.
                var maintenantParis = HeureFrance.Locale(DateTime.UtcNow);

                var prochain = await _controles.GetAsync(eleveId, controleCible, ct);

                // Un contrôle d'une autre matière ne regarde pas ce
                // professeur-ci : il ne saurait pas le préparer.
                if (prochain is null || prochain.MatiereId != matiereId) return null;

                var aujourdhui = maintenantParis.Date;
                var joursRestants = (prochain.DateControle.Date - aujourdhui).Days;
                if (joursRestants < 0) return null;

                // COMMENCÉ, IL N'EST PLUS À PRÉPARER. Arrive quand la séance a
                // été ouverte « pour » un contrôle d'aujourd'hui dont l'heure
                // est passée : la ressortir ferait proposer une révision à un
                // élève qui sort de sa copie.
                if (joursRestants == 0
                    && prochain.HeureControle is TimeSpan debut
                    && debut <= maintenantParis.TimeOfDay)
                {
                    return null;
                }

                var preparation = await _controles.GetPreparationAsync(eleveId, prochain.Id, ct);

                return MarqueurControle(prochain, joursRestants, preparation, maintenantParis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture du controle a venir de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// LE CONTRÔLE DONT L'ÉLÈVE VIENT FAIRE LE POINT — celui du bouton
        /// « Faire le point sur ce contrôle ».
        ///
        /// Voulu par Camara le 14/09/2026 : le professeur ne demande plus de
        /// lui-même, en cours normal, comment s'est passé un contrôle. C'est
        /// l'élève qui ouvre le bilan, quand il le veut ; le professeur sait
        /// alors pourquoi il est là et commence par ça.
        /// </summary>
        private async Task<string?> ControleABilanAsync(
            int eleveId, int matiereId, int controleId, CancellationToken ct)
        {
            try
            {
                var passe = await ControleDuBilanAsync(eleveId, matiereId, controleId, ct);
                if (passe is null) return null;

                var jours = (HeureFrance.Locale(DateTime.UtcNow).Date - passe.DateControle.Date).Days;
                if (jours < 0) return null;

                // AUJOURD'HUI EST POSSIBLE depuis que l'heure compte : un
                // contrôle de 14 h est terminé à 16 h — voir GetADebrieferAsync.
                var quand = jours switch
                {
                    <= 0 => passe.HeureControle is TimeSpan heure
                        ? $"aujourd'hui à {heure:hh\\hmm}"
                        : "aujourd'hui",
                    1 => "hier",
                    <= 7 => $"il y a {jours} jours",
                    _ => $"le {passe.DateControle:dd/MM}",
                };

                // DÉJÀ DÉBRIEFÉ, IL REVIENT QUAND MÊME : pour sa copie, pour
                // sa note arrivée depuis, ou pour reprendre une erreur. On ne
                // lui repose pas la question comme s'il n'avait rien dit.
                var dejaFait = passe.BilanLe is not null;

                return $"[L'élève ouvre cette séance POUR FAIRE LE POINT sur son contrôle de "
                    + $"{passe.MatiereLibelle} n° {passe.Id} — {passe.Sujet ?? "sujet non précisé"} — "
                    + $"qui a eu lieu {quand}"
                    + (passe.NombrePreparations > 0
                        ? $" (vous l'aviez préparé ensemble, {passe.NombrePreparations} fois)."
                        : ".")
                    + (passe.Note is double note ? $" Sa note est déjà connue : {note:0.#}/20." : "")
                    + (dejaFait
                        ? " Vous en avez déjà parlé"
                            + (string.IsNullOrWhiteSpace(passe.Ressenti) ? "" : $" (« {passe.Ressenti} »)")
                            + " : ne lui redemande pas comment ça s'est passé comme si c'était la première "
                            + "fois, demande-lui ce qu'il veut regarder. "
                        : " C'est le sujet de la séance : demande-lui, dès le début et avec tes mots, "
                            + "comment ça s'est passé. ")
                    + "S'il a sa note, recopie-la. S'IL A SA COPIE SOUS LA MAIN, propose-lui de la "
                    + "regarder ensemble ; s'il accepte, écris "
                    + $"[COPIE_CONTROLE] controle: {passe.Id} [/COPIE_CONTROLE] — l'écran lui "
                    + "demande alors si l'énoncé et sa copie sont séparés et lui donne les boutons "
                    + "pour les envoyer : ne lui explique pas comment faire, et n'écris PAS "
                    + "[DEMANDE_DOCUMENT] pour cette copie. S'IL N'A PAS DE COPIE et veut "
                    + "seulement t'envoyer l'énoncé, écris [ENONCE_EXERCICE] à la place. "
                    + $"Dès que tu apprends quelque chose — la note, son ressenti, ce qui a été réussi "
                    + $"ou raté —, écris [CONTROLE_RESULTAT] avec controle: {passe.Id}. S'il ne veut "
                    + "finalement pas en parler, tu n'insistes pas : un `ressenti` qui le dit suffit. "
                    + "Une mauvaise note se constate, elle ne se reproche pas.]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture du controle en bilan de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// Le contrôle de la séance de bilan, s'il est bien à cet élève, dans
        /// cette matière, et déjà commencé. Null sinon — un lien recopié ne
        /// mène à rien.
        /// </summary>
        private async Task<ControleScolaireEleve?> ControleDuBilanAsync(
            int eleveId, int matiereId, int controleId, CancellationToken ct)
        {
            try
            {
                var controle = await _controles.GetAsync(eleveId, controleId, ct);
                return controle is null || controle.MatiereId != matiereId ? null : controle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture du controle {ControleId} (eleve {EleveId}).", controleId, eleveId);
                return null;
            }
        }

        /// <summary>
        /// L'ÉPREUVE QUE L'ÉLÈVE VIENT PRÉPARER — celle du bouton de la carte
        /// d'examen. Le pendant de `ControleAVenirAsync`, avec la même règle :
        /// les faits et l'intention, jamais la phrase.
        ///
        /// Le programme d'une épreuve compte des dizaines de notions : on n'en
        /// donne ici que ce qui oriente la séance. La liste entière n'arrive
        /// qu'au tour de conclusion, où le verdict en a besoin.
        ///
        /// Marque la séance de préparation, comme pour un contrôle.
        /// </summary>
        /// <summary>
        /// LA MAÎTRISE DE LA LANGUE COMPTE DANS TOUTES LES ÉPREUVES — voulu par
        /// Camara le 18/09/2026.
        ///
        /// POSÉE ICI ET NON DANS CHAQUE ÉPREUVE DU SEEDER. Camara : « quelle que
        /// soit la spécialité du bac ou du brevet, c'est partout ». Recopiée dans
        /// la quinzaine d'épreuves, elle aurait manqué à la première qu'on
        /// ajouterait ; ici, elle part avec chaque préparation, sans exception et
        /// sans entretien.
        ///
        /// CHACUN SON RÔLE, ET C'EST LE POINT PÉDAGOGIQUE. Le professeur de
        /// français ENTRAÎNE la compétence ; les autres RAPPELLENT de
        /// l'appliquer. Un cours de rédaction donné par le professeur de
        /// mathématiques prendrait le temps des mathématiques pour faire moins
        /// bien que celui qui sait le faire.
        ///
        /// SOURCE, ET CE QUI RESTE À VÉRIFIER. Annonce du ministre de
        /// l'Éducation nationale Édouard Geffray, interview au Figaro du
        /// 17/09/2026, reprise par 20 Minutes le même jour : la maîtrise de la
        /// langue entre explicitement dans les barèmes « quelle que soit la
        /// discipline », avec une grille commune (orthographe, syntaxe,
        /// vocabulaire, clarté) et jusqu'à deux points de pénalité, y compris en
        /// mathématiques ; en français et en histoire-géographie, une copie très
        /// mal rédigée ne peut pas avoir la moyenne. L'article renvoie au
        /// **Bulletin officiel du 17 septembre 2026**.
        ///
        /// CE BO N'A PAS PU ÊTRE LU : `education.gouv.fr` a répondu 403 le
        /// 18/09/2026, au User-Agent de navigateur ET à celui du worker — qui
        /// passait encore le 13/09. À confirmer contre le texte lui-même à la
        /// prochaine passe de vérification des référentiels, avec les barèmes
        /// par épreuve. En attendant, la formulation ci-dessous reste sur ce que
        /// la source dit, sans inventer de détail par épreuve.
        /// </summary>
        private const string MaitriseDeLaLangue =
            "LA QUALITÉ DE L'EXPRESSION EST NOTÉE DANS CETTE ÉPREUVE, comme dans "
            + "toutes les autres depuis la session 2027 : orthographe, syntaxe, "
            + "vocabulaire, clarté du propos. Jusqu'à DEUX POINTS s'y jouent, y "
            + "compris en mathématiques et en sciences ; en français et en "
            + "histoire-géographie, une copie très mal rédigée ne peut pas avoir "
            + "la moyenne.\n"
            + "CE QUE TU EN FAIS DÉPEND DE TA MATIÈRE. Si tu es professeur de "
            + "FRANÇAIS, c'est une compétence que tu ENTRAÎNES : elle fait partie "
            + "de ta préparation au même titre que le reste. Sinon, tu ne donnes "
            + "AUCUN cours de rédaction — tu le RAPPELLES, en une phrase, quand il "
            + "vient de rédiger une réponse : « pense à rédiger clairement, ça "
            + "compte aussi dans le barème ». Une remarque, pas une leçon.\n"
            + "ET JAMAIS SUR UN CALCUL OU UN MOT JETÉ. Reprendre l'orthographe "
            + "d'un élève qui répond « 42 » à une question de calcul mental est du "
            + "harcèlement, pas de la préparation.\n";

        /// <summary>
        /// LES CLASSES QUI PASSENT L'ÉPREUVE CETTE ANNÉE-LÀ — et elles seules.
        ///
        /// PAR PRÉFIXE ET NON PAR ÉNUMÉRATION : il existe PREMIERE_TECHNO,
        /// TERMINALE_STMG, TROISIEME_PREPA et quelques autres. Une liste en dur
        /// aurait oublié la moitié des élèves de la voie technologique, et le
        /// silence aurait été impossible à remarquer.
        /// </summary>
        private static bool ClasseDExamen(string? niveauCode)
        {
            var code = niveauCode ?? string.Empty;

            return code.StartsWith("TROISIEME", StringComparison.Ordinal)
                || code.StartsWith("PREMIERE", StringComparison.Ordinal)
                || code.StartsWith("TERMINALE", StringComparison.Ordinal);
        }

        /// <summary>
        /// LE RAPPEL DE LA MAÎTRISE DE LA LANGUE EN COURS NORMAL — voulu par
        /// Camara le 18/09/2026, après discussion.
        ///
        /// POURQUOI IL NE SUFFISAIT PAS DE L'AVOIR EN PRÉPARATION D'EXAMEN. Ce
        /// mode-là s'ouvre exprès, et l'essentiel des séances sont des cours
        /// normaux : un élève de 3e qui travaille les maths toute l'année sans
        /// jamais ouvrir la préparation ne l'aurait appris qu'en juin, en lisant
        /// son barème. Et ça ne se rattrape pas : deux points d'orthographe sur
        /// une copie de mathématiques, c'est une habitude d'écriture, elle se
        /// prend sur des mois.
        ///
        /// TROIS FILTRES, ET LE PREMIER N'EST PAS NÉGOCIABLE :
        ///
        ///   1. SEULEMENT EN 3e, 1re ET TERMINALE. Pour un élève de 5e, « ça
        ///      compte dans le barème » est FAUX — et une raison fausse donnée à
        ///      un enfant est pire que pas de raison du tout. Avant la 3e, c'est
        ///      le professeur de français qui s'en occupe, et il le fait déjà.
        ///   2. PAS EN PRÉPARATION D'EXAMEN : le bloc complet y est déjà, et le
        ///      dire deux fois dans la même séance le transformerait en bruit.
        ///   3. NI AU FRANÇAIS NI AUX LANGUES. Le professeur de français a sa
        ///      section entière — il ENTRAÎNE, il ne rappelle pas. Et dans un
        ///      cours d'anglais, ce qui s'écrit s'écrit en anglais : un rappel
        ///      sur la rédaction en français y serait hors sujet.
        ///
        /// LE RAPPEL EST CONDITIONNEL, JAMAIS SYSTÉMATIQUE : « quand il vient
        /// d'écrire une réponse développée ». Une séance de calcul pur ne le
        /// déclenche pas, et il ne revient pas trois fois — au troisième, un
        /// enfant l'entend comme un reproche.
        /// </summary>
        private async Task<string?> RappelMaitriseLangueAsync(
            string? niveauCode, int matiereId, string mode, CancellationToken ct)
        {
            if (mode == ModesSeance.Examen || !ClasseDExamen(niveauCode)) return null;

            try
            {
                var matieres = await _referentielService.GetMatieresAsync(activesSeulement: false);
                var slug = matieres.FirstOrDefault(m => m.Id == matiereId)?.AgentSlug;

                // Le français et les langues vivantes sont écartés du même coup :
                // `LangueDe` rend un code pour les neuf agents de langue, dont
                // `agent-francais`.
                if (slug is null || PromptsPedagogiques.LangueDe(slug) is not null) return null;

                _ = ct;

                return "[LA QUALITÉ DE SON EXPRESSION ÉCRITE COMPTE DANS TOUTES LES ÉPREUVES "
                    + "de son examen depuis la session 2027, y compris la tienne : orthographe, "
                    + "syntaxe, vocabulaire, clarté. Jusqu'à deux points s'y jouent.\n"
                    + "TU NE DONNES AUCUN COURS DE RÉDACTION — ce n'est pas ta matière, et le "
                    + "professeur de français le fait mieux que toi. Tu le RAPPELLES, EN UNE "
                    + "PHRASE, UNE SEULE FOIS DANS LA SÉANCE, et seulement s'il vient d'écrire "
                    + "une réponse développée : « pense à rédiger clairement, ça compte aussi "
                    + "dans le barème ».\n"
                    + "S'IL N'A ÉCRIT QUE DES CALCULS OU DES MOTS ISOLÉS, TU NE DIS RIEN. "
                    + "Reprendre l'orthographe d'un élève qui répond « 42 » à une question de "
                    + "calcul mental est du harcèlement, pas de la préparation.]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du rappel de maitrise de la langue (matiere {MatiereId}).", matiereId);
                return null;
            }
        }

        private async Task<string?> ExamenAVenirAsync(
            int eleveId, string? niveauCode, int matiereId, string epreuveCode, CancellationToken ct)
        {
            try
            {
                var epreuve = await _examens.GetPreparationEpreuveAsync(eleveId, niveauCode, epreuveCode, ct);
                var matiere = epreuve?.Matieres.FirstOrDefault(m => m.MatiereId == matiereId);
                if (epreuve is null || matiere is null) return null;

                await _examens.MarquerPreparationAsync(eleveId, epreuve.Id, matiereId, DateTime.UtcNow, ct);

                var p = matiere.Preparation;

                var autres = epreuve.Matieres
                    .Where(m => m.MatiereId != matiereId)
                    .Select(m => m.Libelle)
                    .ToList();

                return $"[L'élève ouvre cette séance POUR PRÉPARER l'épreuve « {epreuve.Libelle} » du "
                    + $"{epreuve.ExamenLibelle.ToLowerInvariant()} {epreuve.Session} — les épreuves écrites ont "
                    + $"lieu fin juin {epreuve.Session}, la date du jour t'est donnée. C'est le sujet de la "
                    + "séance : commence par là.\n"
                    + (string.IsNullOrWhiteSpace(epreuve.Description)
                        ? ""
                        : $"CE QUE DEMANDE L'ÉPREUVE : {epreuve.Description}\n")
                    + (string.IsNullOrWhiteSpace(epreuve.Remarque) ? "" : $"À SAVOIR : {epreuve.Remarque}\n")
                    + MaitriseDeLaLangue
                    + (autres.Count > 0
                        ? $"Cette épreuve se prépare aussi avec le professeur de {string.Join(" et de ", autres)} : "
                            + "tu ne prépares QUE ta matière.\n"
                        : "")
                    + $"Préparation mesurée en {matiere.Libelle} : {p.Pourcent} % — {p.Acquises} notion(s) "
                    + $"tenue(s) sur {p.Total} au programme de l'épreuve."
                    + Liste(" LÀ OÙ ÇA PÈCHE (déjà travaillé ensemble, mesuré fragile) : ",
                        p.Notions.Where(n => n.EtatMesure == "fragile").Select(n => n.Libelle).Take(5))
                    + Liste(" EN COURS, à consolider : ",
                        p.Notions.Where(n => n.EtatMesure is "en-cours" or "a-confirmer").Select(n => n.Libelle).Take(5))
                    + Liste(" VALIDÉES PAR UNE NOTE — ne propose PAS de nouvelle évaluation dessus : ",
                        p.Notions.Where(n => n.ValideeParMesure).Select(n => n.Libelle).Take(4))
                    + Liste(" JAMAIS TRAVAILLÉES AVEC TOI (à enseigner, pas à réviser), par exemple : ",
                        p.Notions.Where(n => n.EtatMesure == "a-decouvrir").Select(n => n.Libelle).Take(5))
                    + "\nINTENTION : un examen se prépare dans la durée. Pars de ce qui pèche s'il y en a ; "
                    + "sinon, propose-lui un plan — un domaine du programme à la fois — et annonce-le en une "
                    + "phrase. Quand une notion est tenue, entraîne-le au FORMAT de l'épreuve décrit ci-dessus.\n"
                    + VerdictPrecedent(p)
                    + "Dis tout cela AVEC TES MOTS, ne récite pas ce texte. "
                    + "ET À LA FIN DE CETTE SÉANCE — dans ton message de conclusion, avant [FIN_SEANCE] —, "
                    + $"écris [EXAMEN_PRET] avec epreuve: {epreuve.Code}, ton verdict (`pret`, `bientot` ou "
                    + "`pas_pret`) et une `observation` qui dit, domaine par domaine, ce qui est tenu, ce qui "
                    + "pèche et ce qui n'a pas encore été travaillé.]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture de l'epreuve {Epreuve} de l'eleve {EleveId}.", epreuveCode, eleveId);
                return null;
            }
        }

        /// <summary>
        /// L'épreuve préparée, présentée au tour de conclusion avec TOUT son
        /// programme : le verdict porte sur l'épreuve entière, pas sur la
        /// notion du jour.
        /// </summary>
        private async Task<string?> ExamenAConclureAsync(
            int eleveId, string? niveauCode, int matiereId, string epreuveCode, CancellationToken ct)
        {
            try
            {
                var epreuve = await _examens.GetPreparationEpreuveAsync(eleveId, niveauCode, epreuveCode, ct);
                var matiere = epreuve?.Matieres.FirstOrDefault(m => m.MatiereId == matiereId);
                if (epreuve is null || matiere is null) return null;

                return $"[CETTE SÉANCE A ÉTÉ OUVERTE POUR PRÉPARER l'épreuve « {epreuve.Libelle} » du "
                    + $"{epreuve.ExamenLibelle.ToLowerInvariant()} {epreuve.Session}, en {matiere.Libelle}.\n"
                    + ProgrammeEpreuvePourVerdict(matiere.Preparation) + "\n"
                    + VerdictPrecedent(matiere.Preparation)
                    + "TON VERDICT EST DÛ DANS CE MESSAGE, avant [FIN_SEANCE] : écris [EXAMEN_PRET] avec "
                    + $"epreuve: {epreuve.Code}, ton verdict et une observation qui passe en revue le programme "
                    + "DOMAINE PAR DOMAINE — ce qui est tenu, ce qui pèche, ce qui n'a pas encore été vu. "
                    + "« Prêt » veut dire que tout le programme de l'épreuve est tenu. Ce que tu lui dis de vive "
                    + "voix ne change rien à sa fiche ; seul le bloc le fait. Seule exception : s'il n'a RIEN "
                    + "produit pendant la séance, n'écris pas de verdict — le précédent reste.]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture de l'epreuve a conclure {Epreuve} (eleve {EleveId}).", epreuveCode, eleveId);
                return null;
            }
        }

        /// <summary>
        /// Le programme d'une épreuve pour le verdict, regroupé par domaine :
        /// les notions travaillées nommées avec leur état, les autres
        /// comptées et citées en partie. Une liste plate de soixante lignes
        /// noierait ce qui compte.
        /// </summary>
        private static string ProgrammeEpreuvePourVerdict(PreparationControle preparation)
        {
            if (preparation.Notions.Count == 0)
            {
                return "Le programme de l'épreuve n'est pas encore au référentiel de Mimia : dis-le dans ton "
                    + "observation plutôt que de juger sur une partie seulement.";
            }

            var lignes = new List<string> { "LE PROGRAMME DE L'ÉPREUVE, DOMAINE PAR DOMAINE :" };

            foreach (var domaine in preparation.Notions.GroupBy(n => string.IsNullOrWhiteSpace(n.Domaine) ? "Autres notions" : n.Domaine))
            {
                var vues = domaine.Where(n => n.EtatMesure != "a-decouvrir").ToList();
                var jamais = domaine.Where(n => n.EtatMesure == "a-decouvrir").Select(n => n.Libelle).ToList();

                lignes.Add($"## {domaine.Key} — {domaine.Count(n => n.EtatMesure == "acquise")} tenue(s) sur {domaine.Count()}");

                lignes.AddRange(vues.Select(n =>
                    $"- {n.Libelle} : {EtatPourVerdict(n.EtatMesure)} ({n.Pourcent} %)"
                    + (n.ValideeParMesure ? " — validée par une note" : "")));

                if (jamais.Count > 0)
                {
                    lignes.Add($"- jamais travaillées avec toi : {jamais.Count}"
                        + $" ({string.Join(" ; ", jamais.Take(6))}{(jamais.Count > 6 ? " ; …" : "")})");
                }
            }

            return string.Join("\n", lignes);
        }

        /// <summary>
        /// Note ce que l'élève vient de faire sur la carte de copie : sa
        /// réponse à « L'énoncé et ta copie sont-ils séparés ? », ou la pièce
        /// qu'il vient d'envoyer. Rien si son message n'en porte pas — le cas
        /// ordinaire, qui ne coûte qu'une expression régulière.
        /// </summary>
        private async Task EnregistrerCopieControleAsync(
            int eleveId, int matiereId, string? contenu, int? pieceAttachee, CancellationToken ct)
        {
            try
            {
                var maintenant = DateTime.UtcNow;

                if (LecteurCopieControle.LireChoix(contenu) is { } choix)
                {
                    await _controles.PoserChoixCopieAsync(
                        eleveId, choix.Controle, matiereId, choix.Separee, maintenant, ct);
                }

                // SEULEMENT SI LA PIÈCE A VRAIMENT ÉTÉ ATTACHÉE : un marqueur
                // « voici ma copie » sans document ne prouve rien, et le
                // professeur attendrait une feuille en la croyant reçue.
                if (pieceAttachee is int piece && LecteurCopieControle.LirePiece(contenu) is { } recue)
                {
                    await _controles.RecevoirPieceCopieAsync(
                        eleveId, recue.Controle, matiereId, recue.EstEnonce, piece, maintenant, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement de la copie de controle de l'eleve {EleveId}.", eleveId);
            }
        }

        /// <summary>
        /// Combien de temps une copie en attente reste rappelée au professeur.
        /// Douze heures : assez pour qu'un élève parti chercher sa feuille dans
        /// son sac et revenu le soir retrouve la même attente ; pas assez pour
        /// que le professeur la réclame encore la semaine suivante.
        /// </summary>
        private static readonly TimeSpan FenetreCopie = TimeSpan.FromHours(12);

        /// <summary>
        /// LE PROFESSEUR N'ANALYSE RIEN AVANT D'AVOIR TOUT — et c'est le code qui
        /// le lui rappelle, à chaque tour. Voulu par Camara le 13/09/2026 :
        /// « le professeur doit avoir les 2 obligatoirement avant de commencer
        /// l'analyse ». Même patron que la posture d'évaluation : une consigne
        /// lue à quinze cents lignes du tour courant s'oublie ; celle qui
        /// voyage avec le tour, non.
        /// </summary>
        private async Task<string?> CopieControleAsync(int eleveId, int matiereId, CancellationToken ct)
        {
            try
            {
                var copie = await _controles.GetCopieEnCoursAsync(
                    eleveId, matiereId, DateTime.UtcNow - FenetreCopie, ct);

                return copie is null ? null : MarqueurCopieControle(copie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture de la copie de controle de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// D'OÙ VIENT L'ÉLÈVE, RAPPELÉ À CHAQUE TOUR — et donc ce dont son
        /// professeur a le droit de parler. Voir ModesSeance.
        ///
        /// Même patron que la posture d'évaluation : une consigne lue une fois
        /// à l'arrivée tient trois échanges ; celle qui voyage avec le tour de
        /// l'élève, non. Quelques lignes, hors du préfixe mis en cache.
        /// </summary>
        private static string MarqueurModeSeance(string mode, int? controleId, string? epreuveCode) => mode switch
        {
            ModesSeance.Controle =>
                $"[SÉANCE DE PRÉPARATION DU CONTRÔLE n° {controleId} : l'élève est entré par le bouton de "
                + "ce contrôle, c'est de lui qu'on parle. NE PARLE JAMAIS d'un examen (brevet, bac) ni d'un "
                + $"autre contrôle. Les notions travaillées de ce contrôle vont dans [CONTROLE_NOTIONS] "
                + $"controle: {controleId} ; ta conclusion porte [CONTROLE_PRET] controle: {controleId}.]",

            ModesSeance.Bilan =>
                $"[SÉANCE DE BILAN DU CONTRÔLE n° {controleId}, déjà passé : l'élève est entré par « Faire le "
                + "point sur ce contrôle ». NE PARLE JAMAIS d'un examen, ni d'un contrôle à venir. Sa copie "
                + $"et l'énoncé passent par [COPIE_CONTROLE] controle: {controleId}, jamais par "
                + $"[DEMANDE_DOCUMENT] ; ce que tu apprends va dans [CONTROLE_RESULTAT] controle: {controleId}."
                + " SAUF S'IL N'A PAS DE COPIE : dès qu'il dit ne vouloir envoyer QUE l'énoncé, écris "
                + "[ENONCE_EXERCICE] à la place de [COPIE_CONTROLE] et cesse de réclamer la copie.]",

            ModesSeance.Examen =>
                $"[SÉANCE DE PRÉPARATION DE L'ÉPREUVE {epreuveCode} : l'élève est entré par le bouton de "
                + "cette épreuve, c'est d'elle qu'on parle. NE PARLE JAMAIS d'un contrôle de classe. Ta "
                + $"conclusion porte [EXAMEN_PRET] epreuve: {epreuveCode}.]",

            _ =>
                "[COURS NORMAL : l'élève est entré par la carte de sa matière. NE PARLE D'AUCUN CONTRÔLE NI "
                + "D'AUCUN EXAMEN de toi-même, et ne propose aucune préparation. S'il t'annonce un contrôle, "
                + "enregistre-le avec [CONTROLE_PROGRAMME], dis-lui en une phrase qu'il pourra le préparer "
                + "depuis « Mes contrôles », et n'en reparle plus. S'il veut te montrer la copie d'un "
                + "contrôle passé, dis-lui en une phrase de passer par « Faire le point sur ce contrôle », "
                + "dans « Mes contrôles ». Jamais de [CONTROLE_PRET] ni d'[EXAMEN_PRET] ici.]",
        };

        private static string MarqueurControleRecent(ControleScolaireEleve c) =>
            $"[CONTRÔLE PASSÉ dans cette matière : n° {c.Id} — {c.Sujet ?? "sujet non précisé"}, "
            + $"le {c.DateControle:dd/MM}"
            + (c.Note is double note ? $", noté {note:0.#}/20" : string.Empty)
            + ". SI TU LUI DEMANDES SA COPIE OU L'ÉNONCÉ de ce contrôle, ou s'il veut te les "
            + $"montrer, écris [COPIE_CONTROLE] controle: {c.Id} [/COPIE_CONTROLE] dans ce "
            + "message — JAMAIS [DEMANDE_DOCUMENT] : c'est ce bloc qui lui affiche la question "
            + "« L'énoncé et ta copie sont-ils séparés ? » et les boutons d'envoi. Ne lui "
            + "explique pas comment envoyer. Ce contrôle est PASSÉ : ne le confonds pas avec un "
            + "contrôle à venir. "
            + "S'IL N'A PAS DE COPIE et ne veut t'envoyer que l'énoncé — pour comprendre un "
            + "exercice, pas pour être corrigé — écris [ENONCE_EXERCICE] À LA PLACE : c'est "
            + "lui qui affiche les boutons d'envoi de l'énoncé, sans reposer la question de la "
            + "copie.]";

        private static string MarqueurCopieControle(ControleScolaireEleve c)
        {
            var entete = $"[COPIE DU CONTRÔLE n° {c.Id} — {c.MatiereLibelle}, "
                + $"{c.Sujet ?? "sujet non précisé"}. ";

            if (c.CopieComplete)
            {
                return entete
                    + (c.CopieSeparee == true
                        ? "Tu as TOUT : l'énoncé ET sa copie. "
                        : "Tu as TOUT : sa copie, énoncé et réponses sur la même feuille. ")
                    + "Accuse réception de ce qui vient d'arriver, puis commencez l'analyse "
                    + "ENSEMBLE : une erreur à la fois, en lui demandant d'abord ce qu'il pense "
                    + "s'être passé sur cette question avant de lui montrer la démarche. Tu "
                    + "t'appuies sur ce que le correcteur a compté juste ou faux, pas sur ton "
                    + "impression ; une mauvaise note se constate, elle ne se reproche pas. "
                    + "Quand l'analyse est faite, écris [CONTROLE_RESULTAT] avec "
                    + $"controle: {c.Id}, la note si elle figure sur la copie, et `reussies:` / "
                    + "`ratees:` d'après la copie.]";
            }

            var etat = (c.CopieSeparee, c.EnonceRecu, c.CopieRecue) switch
            {
                (true, false, false) =>
                    "L'élève a dit que l'énoncé et sa copie sont SÉPARÉS. Tu n'as encore reçu ni "
                    + "l'un ni l'autre — RIEN : ne dis surtout pas que tu as reçu quelque chose. "
                    + "Attends-les, dans l'ordre qu'il voudra.",
                (true, true, false) =>
                    "Tu as reçu l'ÉNONCÉ ; sa COPIE n'est pas encore arrivée. Dis-lui que tu as "
                    + "bien l'énoncé et que tu attends sa copie.",
                (true, false, true) =>
                    "Tu as reçu sa COPIE ; l'ÉNONCÉ n'est pas encore arrivé. Dis-lui que tu as "
                    + "bien sa copie et que tu attends l'énoncé.",
                _ =>
                    "L'énoncé et ses réponses sont sur la MÊME feuille : tu n'attends que sa "
                    + "copie, et elle n'est pas encore arrivée.",
            };

            return entete + etat
                + " TANT QUE TU N'AS PAS TOUT, TU NE COMMENCES AUCUNE ANALYSE : ni de l'énoncé "
                + "seul, ni de la copie seule — tu ne corriges rien, tu ne commentes aucune "
                + "réponse, tu ne devines pas ce qui manque. Les boutons d'envoi sont à l'écran : "
                + "inutile de lui expliquer comment faire.]";
        }

        /// <summary>
        /// « C'EST AUJOURD'HUI » NE SUFFIT PAS QUAND L'HEURE EST CONNUE.
        ///
        /// Relevé par Camara le 13/09/2026 : contrôle de mathématiques à 18 h,
        /// la professeure lui avait souhaité « bonne route pour ton contrôle ce
        /// soir », et à la séance suivante — l'après-midi même — elle ouvre sur
        /// « Salut Bilal ! Alors, c'est passé comment ? ». Elle lisait
        /// « le 13/09 à 18h00. C'est AUJOURD'HUI. » sans savoir quelle heure il
        /// était : la date du jour ne portait pas l'heure. Elle a comblé le trou
        /// avec l'historique, et l'historique disait « ce soir ».
        ///
        /// On lui donne donc le délai, et on lui dit en toutes lettres ce qu'il
        /// en découle. Sans heure, on lui dit de ne rien supposer.
        /// </summary>
        private static string QuandControle(
            ControleScolaireEleve controle, int joursRestants, DateTime maintenantParis)
        {
            if (joursRestants > 1) return $"Il reste {joursRestants} jours.";

            if (joursRestants == 1)
            {
                return controle.HeureControle is TimeSpan demain
                    ? $"C'est DEMAIN à {demain:hh\\hmm}."
                    : "C'est DEMAIN.";
            }

            if (controle.HeureControle is not TimeSpan heure)
            {
                return "C'est AUJOURD'HUI, à une heure qu'on ne connaît pas. Ne suppose PAS qu'il a "
                    + "déjà eu lieu : s'il en parle au passé, c'est lui qui te le dira.";
            }

            var reste = heure - maintenantParis.TimeOfDay;

            var delai = reste.TotalMinutes >= 60
                ? $"{(int)reste.TotalHours} h {reste.Minutes:00}"
                : $"{Math.Max(1, (int)Math.Ceiling(reste.TotalMinutes))} min";

            return $"C'est AUJOURD'HUI à {heure:hh\\hmm}, dans {delai}. IL N'A PAS ENCORE EU LIEU : "
                + "ne lui demande EN AUCUN CAS comment il s'est passé, même si vous en avez parlé "
                + "plus tôt — il est devant toi AVANT son contrôle.";
        }

        /// <summary>
        /// LE TON CHANGE AVEC L'ÉCHÉANCE — voulu par Camara le 13/09/2026.
        ///
        /// À une semaine, on commence tranquillement ; à trois jours, on
        /// recentre sur ce qui n'est pas tenu ; la veille, on fait court et
        /// ciblé. Le palier est calculé ICI, avec les notions nommées : sans
        /// ça, le professeur traite J-1 exactement comme J-7.
        ///
        /// ON DONNE L'INTENTION ET LES FAITS, JAMAIS LA PHRASE. Le prompt lui
        /// dit en toutes lettres de ne pas réciter ce bloc — c'est son métier
        /// de trouver ses mots, pas le nôtre.
        /// </summary>
        private static string MarqueurControle(
            ControleScolaireEleve controle, int joursRestants, PreparationControle? preparation,
            DateTime maintenantParis)
        {
            var intention = joursRestants switch
            {
                >= 7 =>
                    "INTENTION À CETTE ÉCHÉANCE : il reste du temps. Propose de "
                    + "commencer tranquillement, sans pression, par ce qu'il veut.",

                >= 2 =>
                    "INTENTION À CETTE ÉCHÉANCE : le temps se resserre. Recentre "
                    + "sur ce qui n'est pas encore tenu, et nomme-lui ces notions-là.",

                _ =>
                    "INTENTION À CETTE ÉCHÉANCE : c'est imminent. Propose un "
                    + "entraînement COURT — une quinzaine de minutes — sur les deux "
                    + "notions où il hésite encore. Rien de neuf, rien de long : "
                    + "consolider ce qui est déjà là vaut mieux qu'ouvrir un chantier.",
            };

            var quand = QuandControle(controle, joursRestants, maintenantParis);

            var etat = preparation switch
            {
                // LE PROGRAMME MANQUE : C'EST LA PREMIÈRE CHOSE À OBTENIR.
                // Sans lui, il n'y a rien à préparer, rien à mesurer, et la
                // barre de l'élève reste à zéro quoi qu'il travaille.
                { PerimetreConnu: false } =>
                    "ON NE SAIT PAS ENCORE CE QU'IL Y A AU PROGRAMME de ce contrôle — "
                    + "son sujet est trop vague ou n'a pas été précisé. C'est ta PREMIÈRE "
                    + "question, avant toute préparation : demande-lui sur quoi ça porte "
                    + "exactement, ce que son professeur a annoncé, ce que vous avez vu en "
                    + "classe ces dernières semaines. Puis écris [CONTROLE_NOTIONS] avec "
                    + $"controle: {controle.Id}, un `sujet:` reformulé en clair, et les "
                    + "notions correspondantes — le sujet sera corrigé partout. TANT QUE LE "
                    + "PROGRAMME N'EST PAS CONNU, ton verdict de fin de séance ne peut être que "
                    + "`pas_pret` : on n'est pas « bientôt prêt » pour un programme que personne "
                    + "ne connaît.",

                // CE QU'IL A DÉJÀ TRAVAILLÉ AVEC TOI ET CE QU'IL N'A JAMAIS VU
                // NE SE PRÉPARENT PAS PAREIL — relevé par Camara le
                // 13/09/2026, et c'est exact : les deux étaient dans le même
                // sac « encore fragiles ». Une notion sur laquelle il peine
                // depuis trois séances demande qu'on reprenne autrement ce
                // qui a déjà été dit ; une notion jamais abordée demande de
                // l'enseigner. Confondre les deux, c'est soit réexpliquer ce
                // qu'il sait, soit lui reprocher de ne pas savoir ce qu'on ne
                // lui a jamais montré.
                // SUR `EtatMesure`, PAS SUR `Etat` : le second remonte à « en
                // cours » toute notion travaillée, pour que la barre de
                // l'enfant bouge quand il a fourni un effort. Servir cet
                // encouragement au professeur lui ferait croire qu'une notion
                // est consolidée alors qu'elle est encore fragile — et il
                // passerait à autre chose la veille du contrôle.
                { Pourcent: int pourcent } =>
                    $"Préparation mesurée : {pourcent} %."
                    + Liste(" LÀ OÙ ÇA PÈCHE (déjà travaillé ensemble, mesuré fragile) : ",
                        preparation.Notions.Where(n => n.EtatMesure == "fragile")
                            .Select(n => n.Libelle).Take(4))
                    + Liste(" EN COURS, à consolider : ",
                        preparation.Notions.Where(n => n.EtatMesure is "en-cours" or "a-confirmer")
                            .Select(n => n.Libelle).Take(4))
                    // VALIDÉES SUR COPIE : NI RÉVISION, NI NOUVELLE ÉVALUATION.
                    //
                    // Voulu par Camara le 13/09/2026, après qu'une réciproque
                    // de Thalès validée à 17,5/20 s'est vu reproposer une
                    // évaluation — et que l'impression d'une séance hachée l'a
                    // fait retomber. Le professeur doit savoir lesquelles sont
                    // dans ce cas, pour ne pas y toucher sans demande.
                    + Liste(" VALIDÉES PAR UNE NOTE — ne propose PAS de nouvelle évaluation "
                        + "dessus ; s'il en redemande une, préviens-le d'abord qu'une note "
                        + "moins bonne fera baisser sa progression : ",
                        preparation.Notions.Where(n => n.ValideeParMesure)
                            .Select(n => n.Libelle).Take(4))
                    + Liste(" DÉJÀ TENUES (n'y reviens que s'il le demande) : ",
                        preparation.Notions.Where(n => n.EtatMesure == "acquise")
                            .Select(n => n.Libelle).Take(3))
                    + Liste(" JAMAIS TRAVAILLÉES AVEC TOI (à enseigner, pas à réviser) : ",
                        preparation.Notions.Where(n => n.EtatMesure == "a-decouvrir")
                            .Select(n => n.Libelle).Take(4)),

                _ => string.Empty,
            };

            return $"[Contrôle n° {controle.Id} — {controle.MatiereLibelle}, le "
                + $"{controle.DateControle:dd/MM}"
                + (controle.HeureControle is TimeSpan heure ? $" à {heure:hh\\hmm}" : "")
                + $", sujet : {controle.Sujet ?? "non précisé"}. {quand} {etat}\n"
                + $"{intention}\n"
                + VerdictPrecedent(preparation)
                + "Il est venu pour ce contrôle : c'est le sujet de la séance. S'il veut faire autre "
                + "chose en cours de route, tu le suis sans insister. "
                + "Dis tout cela AVEC TES MOTS, ne récite pas ce texte. "
                + $"Quand vous aurez travaillé des notions de ce contrôle, écris "
                + $"[CONTROLE_NOTIONS] avec controle: {controle.Id}. "
                // LA CONSIGNE VOYAGE AVEC LA SÉANCE, PAS SEULEMENT DANS LE
                // NOYAU. Le statut de l'enfant ne bouge que si ce bloc est
                // réécrit ; une consigne lue à quinze cents lignes du message
                // courant ne suffit pas à le garantir — c'est exactement ce
                // qui avait laissé la posture d'évaluation se déliter.
                + "ET À LA FIN DE CETTE SÉANCE — dans ton message de conclusion, "
                + "celui qui porte [RAPPORT] et [FIN_SEANCE], et AVANT eux —, écris "
                + $"[CONTROLE_PRET] avec controle: {controle.Id}, ton verdict (`pret`, "
                + "`bientot` ou `pas_pret`) et une `observation` qui PASSE EN REVUE TOUT LE "
                + "PROGRAMME, notion par notion — pas seulement ce que vous avez "
                + "fait aujourd'hui —, puis les difficultés liées que tu as vues "
                + "pendant la préparation, même hors programme. C'est ce qui "
                + "justifie le verdict : « prêt » veut dire que TOUT est tenu.\n"
                + ProgrammePourVerdict(preparation)
                + "]";
        }

        /// <summary>
        /// Le contrôle de la séance de préparation, présenté au tour de
        /// conclusion avec tout son programme.
        ///
        /// PLUS DE « TRANCHE » : depuis le 14/09/2026, une préparation s'ouvre
        /// par son bouton et seulement par lui — voir ModesSeance. Le
        /// professeur n'a donc plus à deviner si la séance concernait ce
        /// contrôle : elle a été ouverte pour lui.
        /// </summary>
        private async Task<string?> ControleAConclureAsync(
            int eleveId, int matiereId, int controleId, CancellationToken ct)
        {
            try
            {
                var prochain = await _controles.GetAsync(eleveId, controleId, ct);
                if (prochain is null || prochain.MatiereId != matiereId) return null;

                var preparation = await _controles.GetPreparationAsync(eleveId, prochain.Id, ct);

                return $"[CETTE SÉANCE A ÉTÉ OUVERTE POUR PRÉPARER le contrôle de {prochain.MatiereLibelle} "
                    + $"n° {prochain.Id}, le {prochain.DateControle:dd/MM}, sujet : {prochain.Sujet ?? "non précisé"}.\n"
                    + ProgrammePourVerdict(preparation) + "\n"
                    + VerdictPrecedent(preparation)
                    + "TON VERDICT EST DÛ DANS CE MESSAGE, avant [FIN_SEANCE] : écris [CONTROLE_PRET] "
                    + $"avec controle: {prochain.Id}, ton verdict et une observation qui passe en "
                    + "revue TOUT le programme. Ce que tu lui dis de vive voix ne change rien à sa "
                    + "fiche ; seul le bloc le fait. Seule exception : s'il n'a RIEN produit pendant la "
                    + "séance — des salutations, un mot isolé —, n'écris pas de verdict : le précédent reste.]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture du controle a conclure de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// LE PROGRAMME COMPLET, SANS PLAFOND, POUR LA JUSTIFICATION DU VERDICT.
        ///
        /// Relevé par Camara le 13/09/2026 : un « prêt » justifié par la seule
        /// notion évaluée à la dernière séance, sans un mot sur la seconde. Le
        /// verdict porte sur TOUT le programme ; sa justification doit donc le
        /// voir en entier.
        ///
        /// Distinct des listes par état juste au-dessus, et c'est voulu : elles
        /// sont plafonnées (quatre, trois) parce qu'elles servent à ORIENTER la
        /// séance — sur quoi travailler d'abord. Ici il s'agit de n'en OUBLIER
        /// aucune. Un contrôle compte six notions au plus : la liste entière
        /// tient en quelques lignes.
        ///
        /// Sur `EtatMesure` et non `Etat`, pour la même raison qu'au-dessus :
        /// l'encouragement affiché à l'enfant ferait passer une notion fragile
        /// pour consolidée, et le professeur la déclarerait tenue.
        /// </summary>
        private static string ProgrammePourVerdict(PreparationControle? preparation)
        {
            if (preparation is null || preparation.Notions.Count == 0)
            {
                return "Le programme n'est pas encore connu : dis-le dans ton observation "
                    + "plutôt que de juger sur une partie seulement.";
            }

            var lignes = preparation.Notions.Select(n =>
                $"- {n.Libelle} : {EtatPourVerdict(n.EtatMesure)} ({n.Pourcent} %)"
                + (n.ValideeParMesure ? " — validée par une note" : ""));

            return "LE PROGRAMME COMPLET, À COUVRIR EN ENTIER DANS TON OBSERVATION :\n"
                + string.Join("\n", lignes);
        }

        private static string EtatPourVerdict(string etatMesure) => etatMesure switch
        {
            "acquise" => "tenue",
            "a-confirmer" => "à confirmer",
            "en-cours" => "en cours",
            "fragile" => "fragile",
            _ => "jamais travaillée avec toi",
        };

        /// <summary>
        /// CE QU'IL AVAIT LUI-MÊME CONCLU LA DERNIÈRE FOIS.
        ///
        /// POURQUOI IL DOIT LE RELIRE — soulevé par Camara le 13/09/2026 :
        /// « on est d'accord que le professeur va faire évoluer tout seul le
        /// statut et les justifications ? » Il ne le pouvait pas. Le contexte
        /// lui donnait le pourcentage et l'état des notions, mais PAS son
        /// propre verdict précédent : à chaque séance il repartait de zéro,
        /// sans pouvoir dire « tu progresses depuis la dernière fois », ni
        /// même savoir s'il se contredisait.
        ///
        /// Faire évoluer un jugement suppose de se souvenir du précédent. On
        /// le lui remet donc sous les yeux, avec sa date.
        ///
        /// Vide quand il ne s'est jamais prononcé : il n'y a alors rien à
        /// faire évoluer, et annoncer une absence n'apprendrait rien.
        /// </summary>
        private static string VerdictPrecedent(PreparationControle? preparation)
        {
            if (preparation?.PretStatut is not string statut) return string.Empty;
            if (statut == PretControle.StatutPasCommence) return string.Empty;

            var dit = statut switch
            {
                PretControle.StatutPret => "PRÊT",
                PretControle.StatutBientot => "BIENTÔT PRÊT",
                _ => "PAS ENCORE PRÊT",
            };

            var quand = preparation.PretLe is DateTime le
                ? $" le {HeureFrance.Locale(le):dd/MM}"
                : "";

            var mot = string.IsNullOrWhiteSpace(preparation.PretObservation)
                ? ""
                : $" Tu lui avais dit : « {preparation.PretObservation} »";

            return $"LA DERNIÈRE FOIS TU L'AVAIS DÉCLARÉ {dit}{quand}.{mot} "
                + "Reprends à partir de là : s'il a progressé depuis, dis-le-lui et "
                + "fais évoluer ton verdict ; si rien n'a bougé, ne le répète pas à "
                + "l'identique sans lui expliquer ce qui manque encore.\n";
        }

        /// <summary>
        /// Garde la trace d'une séance ouverte pour préparer un contrôle.
        ///
        /// Silencieuse : un lien recopié ou périmé ne doit pas faire échouer
        /// l'accueil. `ControleAVenirAsync` a déjà vérifié que le contrôle est
        /// bien à cet élève et dans cette matière avant qu'on arrive ici.
        /// </summary>
        private async Task MarquerPreparationControleAsync(int eleveId, int controleId, CancellationToken ct)
        {
            try
            {
                await _controles.MarquerPreparationAsync(eleveId, controleId, DateTime.UtcNow, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du marquage de preparation du controle {ControleId} (eleve {EleveId}).",
                    controleId, eleveId);
            }
        }

        private static string Liste(string intro, IEnumerable<string> elements)
        {
            var liste = elements.ToList();
            return liste.Count == 0 ? string.Empty : intro + string.Join(", ", liste.Select(e => $"« {e} »")) + ".";
        }

        /// <summary>
        /// Ses dictées archivées dans la matière, pour que le professeur
        /// puisse en remettre une au tableau par son numéro — voulu par
        /// Camara le 11/09/2026 : revenir sur une dictée de huit mois doit la
        /// montrer comme dans « Mes dictées ». Le professeur ne voit plus
        /// l'historique de si loin ; il lui faut au moins la liste.
        /// </summary>
        private async Task<string?> ListeDicteesAsync(int eleveId, int matiereId, CancellationToken ct)
        {
            try
            {
                var dictees = (await _dictees.GetParMatiereAsync(eleveId, matiereId, ct))
                    .Take(12)
                    .ToList();

                if (dictees.Count == 0) return null;

                var lignes = dictees.Select(d =>
                    $"- n° {d.Id} — {d.DateCreation:dd/MM/yyyy} — « {d.Titre ?? "sans titre"} » — "
                    + (d.EnAttente ? "en attente de correction" : "corrigée"));

                return "[Ses dictées archivées dans cette matière, de la plus récente à la plus "
                    + "ancienne. Pour en remettre une au tableau — texte et copie d'origine, erreurs "
                    + "numérotées —, écris [DICTEE_AU_TABLEAU]n°[/DICTEE_AU_TABLEAU] :\n"
                    + string.Join("\n", lignes) + "]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de la lecture des dictees de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// L'élève parle de dictée, dans l'une des langues enseignées : c'est
        /// le moment où le professeur a besoin de la liste.
        /// </summary>
        [GeneratedRegex(@"dict[ée]e|dictation|dictado|dettato|diktat|听写", RegexOptions.IgnoreCase)]
        private static partial Regex MentionDictee();

        /// <summary>Le dernier bloc écrit au tableau, dans un message donné.</summary>
        [GeneratedRegex(@"\[ARDOISE\](?<corps>.*?)\[/ARDOISE\]", RegexOptions.Singleline)]
        private static partial Regex ArdoiseEcrite();

        /// <summary>
        /// CE QU'IL Y A VRAIMENT AU TABLEAU, DIT AU PROFESSEUR À CHAQUE TOUR.
        ///
        /// Relevé par Camara le 16/09/2026 : le professeur d'histoire ouvre par
        /// « on reprend le repérage des régions françaises, la carte est toujours
        /// affichée », puis demande à l'élève de cliquer dessus. Le tableau était
        /// noir — et la carte des régions n'existe même pas dans la bibliothèque :
        /// il ne pouvait ni la retrouver, ni la réafficher, ni s'en rendre compte.
        ///
        /// LA RÈGLE EXISTAIT DÉJÀ, ÉCRITE DEUX FOIS — « le tableau est vide au
        /// début de chaque séance », « on ne clique pas sur un tableau vide » —
        /// et le Noyau décrit même cet incident-ci, survenu une première fois le
        /// 13/09. Un rappel partait déjà à l'arrivée. Rien n'a tenu, et c'est
        /// instructif : tout cela demande au professeur de SE SOUVENIR d'un état
        /// que rien ne lui dit. Il devine, et il devine mal.
        ///
        /// On le lui DIT donc, à chaque tour. Une ligne, une dizaine de jetons.
        ///
        /// MÊME RÈGLE QUE L'ÉCRAN, à la lettre (`tableauAuto`, dans Chat.js) : on
        /// remonte les messages du professeur, le dernier bloc [ARDOISE] gagne, un
        /// effacement rencontré avant veut dire vide, et ON S'ARRÊTE AU PREMIER
        /// GESTE — sans quoi la gomme ne gommerait rien et l'on retrouverait
        /// l'exercice d'avant. Deux calculs divergents afficheraient une chose à
        /// l'élève en en annonçant une autre au professeur : c'est précisément le
        /// défaut qu'on répare.
        /// </summary>
        private static string EtatDuTableau(List<DomainMessage> historique)
        {
            string? contenu = null;
            var geste = false;

            for (var i = historique.Count - 1; i >= 0 && !geste; i--)
            {
                if (!string.Equals(historique[i].Role, "assistant", StringComparison.Ordinal)) continue;

                var texte = historique[i].Contenu ?? string.Empty;
                var ardoises = ArdoiseEcrite().Matches(texte);

                if (ardoises.Count > 0)
                {
                    contenu = ardoises[^1].Groups["corps"].Value.Trim();
                    geste = true;
                }
                else if (texte.Contains("[TABLEAU_EFFACE]", StringComparison.Ordinal))
                {
                    geste = true;
                }
            }

            // VIDE EST LE CAS QUI COMPTE, et il mérite sa consigne : c'est là que
            // le professeur invente un support et met l'élève en échec devant un
            // écran noir.
            if (!geste || string.IsNullOrWhiteSpace(contenu))
            {
                return "\n\n[TABLEAU : VIDE — l'élève a un tableau noir sous les yeux. "
                     + "Ne dis d'AUCUN support qu'il est affiché, et ne fais rien "
                     + "pointer ni cliquer : écris-le d'abord dans un bloc [ARDOISE] "
                     + "de CE message. Une figure absente de ta liste de planches ne "
                     + "peut pas être affichée du tout — dans ce cas, change "
                     + "d'exercice au lieu de la décrire comme si elle était là.]";
            }

            var quoi = contenu.StartsWith("SCHEMA:", StringComparison.OrdinalIgnoreCase)
                ? $"la planche « {contenu[7..].Trim()} »"
                : $"« {ResumerTableau(contenu)} »";

            return $"\n\n[TABLEAU : il affiche {quoi}. C'est la SEULE chose que "
                 + "l'élève voit. Tout autre support dont tu parlerais n'est pas "
                 + "devant lui : remets-le au tableau avant d'en parler.]";
        }

        /// <summary>
        /// Le tableau tient en une ligne dans le contexte : on l'abrège.
        ///
        /// Le professeur n'a pas besoin de relire ce qu'il a écrit — il l'a dans
        /// son historique. Il a besoin de savoir CE QUI EST LÀ. Une amorce suffit,
        /// et le contexte ne gonfle pas d'un exercice entier à chaque tour.
        /// </summary>
        private static string ResumerTableau(string contenu)
        {
            var plat = EspacesMultiples().Replace(contenu, " ").Trim();

            return plat.Length <= 160 ? plat : string.Concat(plat.AsSpan(0, 160), "…");
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex EspacesMultiples();

        /// <summary>
        /// LA DICTÉE REMISE AU TABLEAU, DÉPLIÉE POUR LE PROFESSEUR.
        ///
        /// Il ne pose que son numéro ; l'écran affiche l'archive. Mais au tour
        /// suivant, il doit savoir ce qu'il a sous les yeux pour la corriger :
        /// on joint donc au repère, dans l'historique qu'il relit — et nulle
        /// part ailleurs, rien n'est réécrit en base —, le texte dicté et la
        /// copie d'origine. Déterministe, donc sans effet sur le cache.
        /// </summary>
        private async Task DeplierDicteesAuTableauAsync(
            List<DomainMessage> historique, int eleveId, CancellationToken ct)
        {
            try
            {
                var duProfesseur = historique
                    .Where(m => m.Role == "assistant" && !string.IsNullOrEmpty(m.Contenu))
                    .ToList();

                var numeros = duProfesseur
                    .SelectMany(m => LecteurDictee.AuTableau().Matches(m.Contenu!))
                    .Select(m => int.TryParse(m.Groups["id"].Value, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (numeros.Count == 0) return;

                var textes = new Dictionary<int, string>();

                foreach (var numero in numeros)
                {
                    var d = await _dictees.GetDetailAsync(numero, eleveId, ct);
                    if (d is null) continue;

                    textes[numero] =
                        $"[Ajouté par l'application, ne le recopie jamais : l'écran affiche au tableau "
                        + $"la dictée n° {d.Id} du {d.DateCreation:dd/MM/yyyy}"
                        + (string.IsNullOrWhiteSpace(d.Titre) ? "" : $" « {d.Titre} »")
                        + ", avec ses erreurs surlignées et numérotées dans l'ordre du texte.\n"
                        + "La dictée\n" + d.TexteDicte + "\n\nTa copie\n" + d.Copie + "]";
                }

                foreach (var message in duProfesseur)
                {
                    message.Contenu = LecteurDictee.AuTableau().Replace(message.Contenu!, m =>
                        int.TryParse(m.Groups["id"].Value, out var id) && textes.TryGetValue(id, out var t)
                            ? m.Value + "\n" + t
                            : m.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec du depliage des dictees au tableau (eleve {EleveId}).", eleveId);
            }
        }

        /// <summary>
        /// L'élève ne veut plus d'une dictée : elle est supprimée de partout.
        /// Silencieux en cas d'échec, comme les autres archivages.
        /// </summary>
        private async Task SupprimerDicteeAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var demande = LecteurDictee.LireSuppression(texte);
            if (demande is null) return;

            try
            {
                var supprimees = await _dictees.SupprimerAsync(
                    conversationId, eleveId, demande.DicteeId, ct);

                _logger.LogInformation(
                    "Dictee supprimee a la demande de l'eleve {EleveId} (conversation {ConversationId}, "
                    + "numero {Numero}) : {Nombre} ligne(s).",
                    eleveId, conversationId, demande.DicteeId, supprimees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la suppression de dictee (conversation {ConversationId}).", conversationId);
            }
        }

        /// <summary>
        /// LE PROFESSEUR DIT « C'EST CORRIGÉ » SANS ARCHIVER : ON ARCHIVE.
        ///
        /// Relevé par Camara le 11/09/2026 — une dictée corrigée dans la
        /// foulée, et l'archive la montrait « en attente de correction », sans
        /// titre ni badges. Le bloc [DICTEE_CORRIGEE] est une consigne ; une
        /// consigne s'oublie. L'annonce, elle, est dans son message.
        ///
        /// Ne fait rien si le professeur a écrit le bloc — ce qu'il archive
        /// lui-même, titre et observation compris, vaut toujours mieux.
        /// </summary>
        private async Task ConstaterCorrectionDicteeAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            if (LecteurDictee.Lire(texte) is not null) return;
            if (!LecteurDictee.AnnonceLaCorrection(texte)) return;

            try
            {
                var corrigee = await _dictees.ConstaterCorrectionAsync(conversationId, eleveId, ct);

                if (corrigee is not null)
                {
                    _logger.LogInformation(
                        "Dictee {DicteeId} passee corrigee sur annonce du professeur, sans bloc "
                        + "(conversation {ConversationId}).",
                        corrigee.Id, conversationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du constat de correction de dictee (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// L'élève ne veut plus de cet exercice d'écoute : il est supprimé de
        /// partout — voulu par Camara le 12/09/2026, même règle que la dictée
        /// abandonnée. Silencieux en cas d'échec, comme tous les archivages.
        /// </summary>
        private async Task SupprimerComprehensionOraleAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var demande = LecteurComprehensionOrale.LireSuppression(texte);
            if (demande is null) return;

            try
            {
                var supprimees = await _comprehensionsOrales.SupprimerAsync(
                    conversationId, eleveId, demande.DicteeId, ct);

                _logger.LogInformation(
                    "Comprehension orale supprimee a la demande de l'eleve {EleveId} "
                    + "(conversation {ConversationId}, numero {Numero}) : {Nombre} fiche(s).",
                    eleveId, conversationId, demande.DicteeId, supprimees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la suppression d'une comprehension orale (conversation {ConversationId}).",
                    conversationId);
            }
        }

        public async IAsyncEnumerable<string> StreamAccueilAsync(
            int conversationId, int? dureeChoisieMinutes, int? controleId, string? mode, string? epreuveCode,
            [EnumeratorCancellation] CancellationToken ct)
        {
            // Posée AVANT le premier fragment, pas après : `EnregistrerRapportAsync`
            // la relit plus tard dans CETTE MÊME séance, parfois plusieurs
            // requêtes après celle-ci — elle doit déjà être en base.
            if (dureeChoisieMinutes is int duree && DureesValides.Contains(duree))
            {
                await _conversationService.DefinirDureeChoisieAsync(conversationId, duree, ct);
            }

            // LE MODE AUSSI, ET POUR LA MÊME RAISON : chaque tour de la séance,
            // le minuteur et la conclusion après un départ le relisent.
            await DefinirModeSeanceAsync(conversationId, mode, controleId, epreuveCode, ct);

            await foreach (var fragment in StreamTourAsync(
                conversationId, contenu: null, accueil: true, ct))
            {
                yield return fragment;
            }
        }

        /// <summary>
        /// POSE LE MODE DE LA SÉANCE, APRÈS L'AVOIR VÉRIFIÉ — voir ModesSeance.
        ///
        /// Le navigateur ne fait que DEMANDER un mode. Ce qui ne tient pas — un
        /// contrôle d'un autre élève ou d'une autre matière, une épreuve d'une
        /// autre classe, un lien périmé — ramène au cours normal, sans erreur :
        /// c'est le cloisonnement le plus sûr, et la séance s'ouvre quand même.
        ///
        /// Toute arrivée repose le mode, cours normal compris : c'est ce qui
        /// fait qu'un élève revenu par la carte de sa matière n'entend plus
        /// parler de la préparation d'hier.
        /// </summary>
        private async Task DefinirModeSeanceAsync(
            int conversationId, string? mode, int? controleId, string? epreuveCode, CancellationToken ct)
        {
            try
            {
                var contexte = await _resolver.ResoudreConversationAsync(conversationId);
                if (contexte is null) return;

                // Un ancien écran n'envoyait que le contrôle : c'était une préparation.
                var demande = string.IsNullOrWhiteSpace(mode) && controleId is not null
                    ? ModesSeance.Controle
                    : ModesSeance.Normaliser(mode);

                var retenu = ModesSeance.Cours;
                int? controleRetenu = null;
                string? epreuveRetenue = null;

                var aujourdhui = HeureFrance.Locale(DateTime.UtcNow).Date;

                if (demande is ModesSeance.Controle or ModesSeance.Bilan && controleId is int id)
                {
                    var controle = await _controles.GetAsync(contexte.Eleve.Id, id, ct);

                    var valide = controle is not null
                        && controle.MatiereId == contexte.Conversation.MatiereId
                        && (demande == ModesSeance.Controle
                            ? controle.DateControle.Date >= aujourdhui
                            : controle.DateControle.Date <= aujourdhui);

                    if (valide)
                    {
                        retenu = demande;
                        controleRetenu = id;
                    }
                }
                else if (demande == ModesSeance.Examen && !string.IsNullOrWhiteSpace(epreuveCode))
                {
                    var epreuve = await _examens.GetEpreuveApplicableAsync(
                        contexte.Eleve.NiveauCode, epreuveCode, ct);

                    if (epreuve is not null
                        && epreuve.Matieres.Contains(contexte.Conversation.MatiereCode, StringComparer.OrdinalIgnoreCase))
                    {
                        retenu = ModesSeance.Examen;
                        epreuveRetenue = epreuve.Code;
                    }
                }

                if (retenu != demande)
                {
                    _logger.LogInformation(
                        "Mode {Demande} refuse pour la conversation {ConversationId} : cours normal.",
                        demande, conversationId);
                }

                await _conversationService.DefinirModeSeanceAsync(
                    conversationId, retenu, controleRetenu, epreuveRetenue, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la definition du mode de seance (conversation {ConversationId}).", conversationId);
            }
        }

        /// <summary>
        /// Les seules durées proposées à l'élève — voir `DUREES` dans
        /// `GrilleMatieres.js`. Un paramètre hors de cette liste est ignoré :
        /// mieux vaut un compte rendu sans durée qu'une valeur fantaisiste
        /// affichée comme si l'élève l'avait choisie.
        /// </summary>
        private static readonly int[] DureesValides = [15, 25, 35, 45];

        public IAsyncEnumerable<string> StreamAnnonceAsync(
            int conversationId, TypeAccueil annonce, CancellationToken ct) =>
            StreamTourAsync(conversationId, contenu: null, accueil: true, ct, annonce);

        /// <summary>
        /// Fait conclure une séance quittée avant l'heure — voir
        /// <see cref="FileConclusion"/>. Appelée par le worker, jamais par une
        /// requête HTTP : personne n'écoute ce flux, il est simplement vidé
        /// jusqu'au bout pour que ses effets de bord (message persisté,
        /// [RAPPORT]/[FICHE]/[EVALUATION_PREVUE] enregistrés) se produisent.
        ///
        /// Silencieuse si rien ne mérite d'être conclu : cliquer « Quitter »
        /// sans avoir dit un mot ne doit pas déclencher d'appel au modèle.
        /// </summary>
        public async Task ConclureDepartAnticipeAsync(int conversationId, CancellationToken ct = default)
        {
            var aTravaille = await _conversationService.ADuTravailNonConcluAsync(conversationId, ct);
            if (!aTravaille) return;

            await foreach (var _ in StreamTourAsync(
                conversationId, contenu: null, accueil: true, ct, TypeAccueil.DepartAnticipe))
            {
                // Personne n'écoute : StreamTourAsync fait tout le travail qui
                // compte (persistance, RAPPORT, etc.) avant même de produire
                // ce fragment.
            }

            // APRÈS lui avoir laissé sa chance, et pas avant : ce qu'il a
            // archivé lui-même est toujours meilleur que ce qu'on reconstitue.
            await RattraperComprehensionsOralesAsync(conversationId, ct);
            await RattraperExpressionsOralesAsync(conversationId, ct);
            await RattraperExpressionsEcritesAsync(conversationId, ct);
        }

        /// <summary>
        /// Ne garde que les blocs entre crochets : tout ce qui laisse une
        /// trace, rien de ce qui parlait dans le vide.
        ///
        /// Les blocs sont conservés dans leur ordre et leur forme exacte —
        /// ce sont eux que relisent les lecteurs d'archives.
        /// </summary>
        private static string SansTexteLibre(string contenu)
        {
            var blocs = BlocsArchive().Matches(contenu);
            if (blocs.Count == 0) return contenu;

            return string.Join("\n\n",
                blocs.Select(b => b.Value.Trim()));
        }

        [GeneratedRegex(
            @"\[(?<nom>[A-Z_]+)\](?:.*?\[/\k<nom>\])?",
            RegexOptions.Singleline)]
        private static partial Regex BlocsArchive();

        /// <summary>
        /// Archive les exercices d'écoute que le professeur a oublié
        /// d'enregistrer, reconstitués depuis les messages.
        ///
        /// POURQUOI ON NE LUI FAIT PLUS CONFIANCE ICI. Le bloc
        /// [COMPREHENSION_ORALE] est une consigne, et une consigne s'oublie :
        /// le 10/09/2026, trois exercices d'affilée ont disparu alors que la
        /// séance avait bien son compte rendu et sa fiche. Le passage lu, lui,
        /// est écrit noir sur blanc dans son message — il n'y a rien à
        /// deviner. Silencieux en cas d'échec, comme tous les archivages.
        /// </summary>
        /// <summary>
        /// LE FILET DES CONVERSATIONS — Camara, le 18/09/2026 : « je veux que tu
        /// mettes le filet directement ».
        ///
        /// Je ne l'avais pas construit, en me disant qu'on verrait d'abord si le
        /// bloc passait. Camara a tranché, et il a raison : la compréhension
        /// orale a déjà payé pour l'apprendre — trois exercices disparus le
        /// 10/09/2026, alors que la séance avait bien son compte rendu. Attendre
        /// que ça arrive une seconde fois n'apprend rien de neuf.
        ///
        /// CE QUI MANQUE À UNE CONVERSATION RATTRAPÉE : son TITRE et la remarque
        /// du professeur. Le titre se compose à partir de la date — « Conversation
        /// du 18 septembre » — plutôt que d'être inventé : un titre deviné dirait
        /// à l'enfant que le professeur a nommé leur échange, ce qui serait faux.
        /// </summary>
        private async Task RattraperExpressionsOralesAsync(
            int conversationId, CancellationToken ct)
        {
            try
            {
                var contexte = await _resolver.ResoudreConversationAsync(conversationId);
                if (contexte is null) return;

                var manquantes = (await _expressionsOrales.GetNonArchiveesAsync(
                    conversationId, contexte.Eleve.Id, ct)).ToList();

                if (manquantes.Count == 0) return;

                foreach (var conversation in manquantes)
                {
                    await _expressionsOrales.AjouterAsync(
                        contexte.Eleve.Id, conversationId,
                        TitreDeConversation(conversation.DateExercice),
                        conversation.Langue, conversation.Echange,
                        remarque: null, conversation.DateExercice, ct);
                }

                _logger.LogInformation(
                    "{Total} conversation(s) rattrapee(s) pour la conversation {ConversationId}.",
                    manquantes.Count, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du rattrapage des expressions orales (conversation "
                    + "{ConversationId}).", conversationId);
            }
        }

        /// <summary>
        /// LE FILET DES TEXTES ÉCRITS, le troisième de la famille.
        ///
        /// IL RATTRAPE DEUX CHOSES DIFFÉRENTES, selon le support :
        ///
        ///   - AU CLAVIER, le texte lui-même — le message de l'enfant est en
        ///     base, il n'y a rien à deviner ;
        ///   - AU CAHIER, sa PHOTO, recopiée dans l'archive. Voulu par Camara le
        ///     18/09/2026 : « comme ça on perdra rien et le prof pourra quand
        ///     même refaire la transcription si elle a pas été faite. » La ligne
        ///     naît alors sans texte, et le professeur la reprend au cours
        ///     suivant — voir `TexteATranscrireAsync`.
        ///
        /// IL NE RATTRAPE JAMAIS LA CORRECTION, dans un cas comme dans l'autre :
        /// les reprises du professeur sont mêlées à sa pédagogie en français
        /// courant, et les redécouper en genres reviendrait à deviner.
        ///
        /// CE QU'IL SAUVE EST LA MOITIÉ QUI NE SE RETROUVE NULLE PART AILLEURS :
        /// le travail de l'enfant. Une archive sans reprises se lit encore ; un
        /// texte perdu, non.
        /// </summary>
        private async Task RattraperExpressionsEcritesAsync(
            int conversationId, CancellationToken ct)
        {
            try
            {
                var contexte = await _resolver.ResoudreConversationAsync(conversationId);
                if (contexte is null) return;

                var manquants = (await _expressionsEcrites.GetNonArchiveesAsync(
                    conversationId, contexte.Eleve.Id, ct)).ToList();

                if (manquants.Count == 0) return;

                // LA LANGUE VIENT DE LA MATIÈRE QUAND ELLE N'EST PAS DANS LES
                // MESSAGES, et c'est le cas le plus fréquent : une consigne de
                // rédaction se donne en français, sans qu'aucune balise de
                // langue ne soit écrite. Sans ce repli, tous les textes
                // rattrapés seraient perdus faute de savoir dans quelle langue
                // ils étaient écrits.
                var matieres = await _referentielService.GetMatieresAsync(activesSeulement: false);
                var slug = matieres
                    .FirstOrDefault(m => m.Id == contexte.Conversation.MatiereId)?.AgentSlug;

                var langueMatiere = PromptsPedagogiques.LangueDe(slug);

                var rattrapes = 0;

                foreach (var texte in manquants)
                {
                    var langue = string.IsNullOrWhiteSpace(texte.Langue)
                        ? langueMatiere
                        : texte.Langue;

                    // UNE MATIÈRE QUI N'EST PAS UNE LANGUE N'A RIEN À FAIRE ICI.
                    // L'exercice n'existe que dans les matières de langue ; si on
                    // ne sait pas laquelle, c'est que quelque chose ne colle pas,
                    // et on préfère ne rien écrire.
                    if (string.IsNullOrWhiteSpace(langue)) continue;

                    await _expressionsEcrites.AjouterAsync(
                        contexte.Eleve.Id, conversationId,
                        TitreDeTexteEcrit(texte.DateExercice),
                        langue, texte.Consigne, texte.Texte,
                        corrections: [], remarque: null, texte.DateExercice,
                        texte.Photo, texte.PhotoTypeMime, ct: ct);

                    rattrapes++;
                }

                if (rattrapes > 0)
                {
                    _logger.LogInformation(
                        "{Total} texte(s) ecrit(s) rattrape(s) pour la conversation {ConversationId}.",
                        rattrapes, conversationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du rattrapage des expressions ecrites (conversation "
                    + "{ConversationId}).", conversationId);
            }
        }

        /// <summary>
        /// Le titre d'un texte que le professeur n'a pas nommé. La date, comme
        /// pour une conversation rattrapée, et pour la même raison : un titre
        /// deviné dirait à l'enfant que le professeur a nommé son texte.
        /// </summary>
        private static string TitreDeTexteEcrit(DateTime quand) =>
            "Texte du " + quand.ToLocalTime().ToString(
                "d MMMM", new System.Globalization.CultureInfo("fr-FR"));

        /// <summary>
        /// Le titre d'une conversation que le professeur n'a pas nommée.
        ///
        /// LA DATE PLUTÔT QU'UN TITRE DEVINÉ. On pourrait prendre les premiers
        /// mots de l'échange — « Hello, how are you » — mais dix conversations
        /// commencent ainsi, et l'enfant ne distinguerait rien. La date, elle,
        /// distingue toujours.
        /// </summary>
        private static string TitreDeConversation(DateTime quand) =>
            "Conversation du " + quand.ToLocalTime().ToString(
                "d MMMM", new System.Globalization.CultureInfo("fr-FR"));

        private async Task RattraperComprehensionsOralesAsync(
            int conversationId, CancellationToken ct)
        {
            try
            {
                var contexte = await _resolver.ResoudreConversationAsync(conversationId);
                if (contexte is null) return;

                var manquantes = (await _comprehensionsOrales.GetNonArchiveesAsync(
                    conversationId, contexte.Eleve.Id, ct)).ToList();

                if (manquantes.Count == 0) return;

                var matieres = await _referentielService.GetMatieresAsync(activesSeulement: false);
                var avatar = matieres
                    .FirstOrDefault(m => m.Id == contexte.Conversation.MatiereId)?.ProfAvatar;

                foreach (var exercice in manquantes)
                {
                    byte[]? audio = null;

                    try
                    {
                        audio = await _syntheseVocale.SynthetiserWavAsync(
                            exercice.Passage, avatar, contexte.Eleve.Age,
                            dictee: false, langue: exercice.Langue, ct: ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Audio non regenere pour une comprehension orale rattrapee "
                            + "(conversation {ConversationId}).", conversationId);
                    }

                    await _comprehensionsOrales.AjouterAsync(
                        contexte.Eleve.Id, conversationId,
                        TitreDepuisPassage(exercice.Passage),
                        exercice.Langue, exercice.Passage, exercice.ReponseEleve,
                        exercice.Comprehension, remarque: null, audio,
                        exercice.DateExercice, ct);
                }

                _logger.LogInformation(
                    "{Total} comprehension(s) orale(s) rattrapee(s) pour la conversation {ConversationId}.",
                    manquantes.Count, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du rattrapage des comprehensions orales (conversation {ConversationId}).",
                    conversationId);
            }
        }

        private async IAsyncEnumerable<string> StreamTourAsync(
            int conversationId,
            string? contenu,
            bool accueil,
            [EnumeratorCancellation] CancellationToken ct,
            TypeAccueil annonce = TypeAccueil.Aucun,
            int? secondesRestantes = null,
            IReadOnlyList<int>? pieceJointeIds = null,
            string? vitesseEcoute = null)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId)
                ?? throw new UnauthorizedAccessException(
                    "Conversation inexistante ou n'appartenant pas au compte authentifié.");

            // LE MODE DE LA SÉANCE, POSÉ À L'ARRIVÉE ET RELU ICI — voir
            // ModesSeance. Sans sa cible, un mode retombe sur le cours normal.
            var mode = ModesSeance.Normaliser(contexte.Conversation.ModeSeance);
            var controleMode = contexte.Conversation.ModeControleId;
            var epreuveMode = contexte.Conversation.ModeEpreuveCode;

            if ((mode is ModesSeance.Controle or ModesSeance.Bilan && controleMode is null)
                || (mode == ModesSeance.Examen && string.IsNullOrWhiteSpace(epreuveMode)))
            {
                mode = ModesSeance.Cours;
            }

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

            // Vrai quand la dictée interrompue n'a encore laissé aucune trace
            // — l'élève a fermé son onglet au lieu de cliquer « Quitter » : le
            // marqueur voyage alors avec le message d'accueil lui-même.
            var marquerAbandonDictee = false;

            // LES documents joints à CE tour, ceux qui ont passé les contrôles,
            // dans l'ordre où l'élève les a ajoutés — Camara, le 16/09/2026 :
            // « le professeur reçoit tous les documents d'un coup ».
            var piecesDuTour = new List<Domain.Models.PieceJointe>();

            // Son identifiant, une fois RÉELLEMENT attaché au message — c'est
            // la preuve dont la copie de contrôle a besoin.
            int? pieceAttachee = null;

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

                    // Parti pendant une dictée, avant sa copie : même besoin
                    // de le dire en premier. Le contrôle passe avant — il ne
                    // peut pas y avoir les deux à la fois dans les faits.
                    var interruption = controleAbandonne
                        ? InterruptionDictee.Aucune
                        : await LireInterruptionDicteeAsync(conversationId, contexte.Eleve.Id, ct);

                    marquerAbandonDictee = interruption == InterruptionDictee.ANoter;

                    typeAccueil = controleAbandonne
                        ? TypeAccueil.RetourControleAbandonne
                        : interruption != InterruptionDictee.Aucune
                            ? TypeAccueil.RetourDicteeInterrompue
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
                foreach (var idPiece in pieceJointeIds ?? Array.Empty<int>())
                {
                    var piece = await _conversationService.GetPieceJointeAsync(idPiece, ct);

                    if (piece is not null && piece.ConversationId == conversationId
                        && piece.MessageId is null)
                    {
                        await _conversationService.AttacherAuMessageAsync(idPiece, messageEleve.Id, ct);
                        // LA COPIE DE CONTRÔLE NE RETIENT QUE LA PREMIÈRE, pour
                        // l'instant : une copie en plusieurs pages toucherait
                        // l'évaluation et demanderait une migration. Chantier à part.
                        pieceAttachee ??= idPiece;

                        // L'historique a été lu AVANT d'écrire ce message : le
                        // document n'y est donc pas. On le porte à part pour ce
                        // tour-ci ; dès le tour suivant il arrivera par
                        // l'historique comme n'importe quel autre.
                        var avecDonnees = await _conversationService
                            .GetPieceJointeAvecDonneesAsync(idPiece, ct);

                        if (avecDonnees is not null) piecesDuTour.Add(avecDonnees);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Piece jointe {PieceId} refusee sur la conversation {ConversationId} : "
                            + "inexistante, deja attachee, ou d'une autre conversation.",
                            idPiece, conversationId);
                    }
                }

                // LA COPIE D'UN CONTRÔLE : le choix et les pièces sont notés
                // AVANT que le professeur réponde — c'est ce qui lui permet,
                // dans ce tour même, d'accuser réception de ce qui vient
                // d'arriver.
                await EnregistrerCopieControleAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, contenu, pieceAttachee, ct);
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
                TypeAccueil.CopieAttendue =>
                    "[Il reste deux minutes de séance et sa copie n'est pas arrivée.]",
                TypeAccueil.RetourControleAbandonne =>
                    "[L'élève revient. Il était parti en plein contrôle : celui-ci est annulé.]",
                TypeAccueil.RetourDicteeInterrompue =>
                    "[L'élève revient. La dernière fois, il est parti pendant la dictée, avant "
                    + "d'avoir rendu sa copie : elle est annulée, et rien n'a été archivé.]",

                // SANS CETTE LIGNE, LE DÉPART ANTICIPÉ TOMBAIT DANS LE CAS PAR
                // DÉFAUT — « l'élève vient d'ouvrir la séance et met son
                // casque ». Le bloc système disait « il est parti, conclus » et
                // le tour utilisateur, juste derrière, disait l'exact
                // contraire : le professeur suivait le plus proche et
                // accueillait quelqu'un qui n'était plus là. Aucune séance
                // quittée en cours de route n'a donc jamais eu son [RAPPORT],
                // ni ses archives d'exercices — constaté trois fois de suite le
                // 10/09/2026 avant d'être compris.
                TypeAccueil.DepartAnticipe =>
                    "[L'élève a quitté le cours. La séance est terminée et il n'est plus là : "
                    + "personne ne lira ce message. Dépose la trace de ce qui vient d'être "
                    + "travaillé, puis referme la séance.]",

                TypeAccueil.Aucun => contenu!,
                _ => "[L'élève vient d'ouvrir la séance et met son casque.]",
            };

            // POSÉE ICI, PAS PLUS BAS : elle sert déjà au rappel de contrôle
            // ci-dessous, avant l'endroit où elle servait jusque-là (la
            // gomme du tableau). Pur déplacement, même calcul.
            var nouvelleArrivee = typeAccueil is TypeAccueil.PremiereSeance
                or TypeAccueil.Retour
                or TypeAccueil.RetourControleAbandonne
                or TypeAccueil.RetourDicteeInterrompue
                or TypeAccueil.NouvelleSeance;

            // Une dictée restée en attente de correction ouvre la séance.
            var retourEnCours = typeAccueil is TypeAccueil.Retour
                or TypeAccueil.RetourControleAbandonne
                or TypeAccueil.RetourDicteeInterrompue
                or TypeAccueil.NouvelleSeance;

            if (retourEnCours)
            {
                var enAttente = await DicteeEnAttenteAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, ct);

                if (enAttente is not null) declencheur += "\n\n" + enAttente;

                // Une correction d'évaluation remise faute de temps ouvre, elle
                // aussi, le cours suivant — voir CorrectionEnAttenteAsync.
                var copieACorriger = await CorrectionEnAttenteAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, ct);

                if (copieACorriger is not null) declencheur += "\n\n" + copieACorriger;

                // ET LA COPIE QUI N'EST QU'UNE PHOTO : elle part avec son image,
                // jointe à ce tour. C'est la plus urgente des trois — les deux
                // autres ont leur texte en base, celle-ci n'a que des pixels qui
                // finiront par être effacés.
                var aTranscrire = await TexteATranscrireAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, ct);

                if (aTranscrire is not null)
                {
                    declencheur += "\n\n" + aTranscrire.Value.Consigne;
                    piecesDuTour.Add(aTranscrire.Value.Photo);
                }

                // ET LE RAPPEL DE LA MAÎTRISE DE LA LANGUE, en cours normal, pour
                // les seules classes qui passent une épreuve cette année-là. Posé
                // AU TOUR D'ARRIVÉE et nulle part ailleurs : une fois par séance,
                // pas une fois par message.
                var rappelLangue = await RappelMaitriseLangueAsync(
                    contexte.Eleve.NiveauCode, contexte.Conversation.MatiereId, mode, ct);

                if (rappelLangue is not null) declencheur += "\n\n" + rappelLangue;
            }

            // Et la liste de ses dictées, quand il pourrait vouloir y revenir :
            // à son arrivée, ou dès qu'il en parle.
            var parleDeDictee = typeAccueil == TypeAccueil.Aucun
                && !string.IsNullOrEmpty(contenu)
                && MentionDictee().IsMatch(contenu);

            if (retourEnCours || parleDeDictee)
            {
                var liste = await ListeDicteesAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, ct);

                if (liste is not null) declencheur += "\n\n" + liste;
            }

            // LE TABLEAU VIENT D'ÊTRE EFFACÉ, ET LE PROFESSEUR DOIT LE SAVOIR.
            //
            // Relevé par Camara le 13/09/2026 : au retour, le tableau est bien
            // remis à zéro (voir `TableauEfface`), mais le professeur reprend
            // l'exercice laissé en suspens à l'oral seulement — « dans le
            // triangle avec AD = 3, AB = 9, AE = 4, AC = 12… ». Il voit dans
            // son historique le bloc [ARDOISE] de la dernière fois et croit
            // l'exercice encore affiché ; l'élève, lui, a un tableau noir, et
            // doit retenir quatre longueurs de tête.
            //
            // La règle existe dans le Noyau (« le tableau est vide au début de
            // chaque séance »), noyée parmi deux mille lignes : elle n'a pas
            // tenu. On la remet donc sous ses yeux au tour même où le code
            // efface le tableau — le seul moment où elle compte, et le seul où
            // l'on sache avec certitude qu'elle est vraie.
            if (nouvelleArrivee)
            {
                declencheur += "\n\n[LE TABLEAU VIENT D'ÊTRE EFFACÉ : l'élève a un "
                    + "tableau noir sous les yeux. Ce que tu y avais écrit la "
                    + "dernière fois, tu t'en souviens ; lui ne le voit plus. Si "
                    + "tu reprends un exercice, un énoncé, les valeurs d'un "
                    + "problème, un calcul ou une figure — même déjà écrits "
                    + "avant —, RÉÉCRIS-LES dans un bloc [ARDOISE] dans CE "
                    + "message, juste après les avoir dits. Le bloc ne se "
                    + "prononce pas : il ne compte pas dans la longueur de ton "
                    + "message. Seule exception, comme toujours : un texte à "
                    + "dicter ne va jamais au tableau. Une FIGURE, elle, ne se "
                    + "réécrit pas : elle se remet par sa clé, [ARDOISE] "
                    + "SCHEMA:la-cle [/ARDOISE]. Et si la figure dont tu te "
                    + "souviens n'est PAS dans ta liste de planches, tu ne peux "
                    + "pas la réafficher du tout : change d'exercice plutôt que "
                    + "de faire chercher à l'élève ce que personne ne voit.]";
            }
            else
            {
                // PAS UNE ARRIVÉE, ET POURTANT LA MÊME FAUTE. Le rappel ci-dessus
                // ne partait qu'au premier message : un professeur qui invente un
                // support au dixième passait au travers. On lui dit donc l'état
                // réel du tableau à CHAQUE tour — voir `EtatDuTableau`.
                declencheur += EtatDuTableau(historique);
            }

            // AU MOMENT DE CONCLURE, LE CONTRÔLE REVIENT SOUS SES YEUX.
            //
            // Relevé par Camara le 13/09/2026 : l'élève entre par le cours
            // ordinaire, le professeur reprend une notion du programme, dit
            // « tu es prêt pour ton contrôle ce soir ! » — et n'écrit pas le
            // bloc. Le contrôle ne lui avait été présenté qu'à l'arrivée,
            // vingt messages plus tôt, comme une proposition ; au tour de
            // conclusion, il n'était plus nulle part.
            //
            // C'est le professeur qui juge si la séance concernait le contrôle
            // — « c'est lui qui sait ce qu'on fait ». Le code lui donne de quoi
            // juger, au seul tour où ça compte : le programme, et la question.
            // `CopieAttendue` N'EST PAS UNE CONCLUSION, et c'est le point : le
            // professeur rappelle la copie, l'élève continue d'écrire, et la
            // séance suit son cours. La ranger ici ferait dire au revoir deux
            // minutes avant la fin, au milieu du contrôle.
            var conclusion = typeAccueil is TypeAccueil.FinSeance
                or TypeAccueil.FinImminente
                or TypeAccueil.ClotureProche
                or TypeAccueil.ClotureForcee
                or TypeAccueil.DepartAnticipe;

            // SEULEMENT DANS UNE SÉANCE DE PRÉPARATION — voulu par Camara le
            // 14/09/2026 : entré par la carte de sa matière, l'élève fait un
            // cours normal, et son professeur ne rend aucun verdict sur un
            // contrôle ou un examen. Ses notions travaillées font quand même
            // monter les barres : c'est la parole qui est cloisonnée, pas la
            // mesure.
            if (conclusion)
            {
                var aConclure = mode switch
                {
                    ModesSeance.Controle => await ControleAConclureAsync(
                        contexte.Eleve.Id, contexte.Conversation.MatiereId, controleMode!.Value, ct),
                    ModesSeance.Examen => await ExamenAConclureAsync(
                        contexte.Eleve.Id, contexte.Eleve.NiveauCode, contexte.Conversation.MatiereId,
                        epreuveMode!, ct),
                    _ => null,
                };

                if (aConclure is not null) declencheur += "\n\n" + aConclure;
            }

            // À L'ARRIVÉE, LE PROFESSEUR SAIT POURQUOI L'ÉLÈVE EST LÀ — et
            // commence par ça. En cours normal, rien ne s'ajoute : il ne parle
            // d'aucun contrôle ni d'aucun examen.
            if (nouvelleArrivee)
            {
                switch (mode)
                {
                    case ModesSeance.Controle:
                        var controleAVenir = await ControleAVenirAsync(
                            contexte.Eleve.Id, contexte.Conversation.MatiereId, controleMode!.Value, ct);

                        if (controleAVenir is not null)
                        {
                            declencheur += "\n\n[L'élève ouvre cette séance POUR préparer ce "
                                + "contrôle : c'est le sujet de la séance, commence par là.]";

                            await MarquerPreparationControleAsync(
                                contexte.Eleve.Id, controleMode.Value, ct);

                            declencheur += "\n\n" + controleAVenir;
                        }

                        break;

                    case ModesSeance.Bilan:
                        var bilanControle = await ControleABilanAsync(
                            contexte.Eleve.Id, contexte.Conversation.MatiereId, controleMode!.Value, ct);

                        if (bilanControle is not null) declencheur += "\n\n" + bilanControle;
                        break;

                    case ModesSeance.Examen:
                        var examenAVenir = await ExamenAVenirAsync(
                            contexte.Eleve.Id, contexte.Eleve.NiveauCode, contexte.Conversation.MatiereId,
                            epreuveMode!, ct);

                        if (examenAVenir is not null) declencheur += "\n\n" + examenAVenir;
                        break;
                }
            }

            // LA DATE DU JOUR, À CHAQUE TOUR — pas seulement à l'arrivée : un
            // élève peut annoncer un contrôle n'importe quand dans la
            // conversation, et le professeur doit pouvoir traduire « vendredi
            // prochain » en date absolue pour écrire [CONTROLE_PROGRAMME].
            declencheur += "\n\n" + PromptsPedagogiques.MarqueurDateDuJour(DateTime.UtcNow);

            declencheur += "\n\n" + MarqueurModeSeance(mode, controleMode, epreuveMode);

            // UN CONTRÔLE OUVERT SE RAPPELLE À CHAQUE TOUR, ET C'EST LE CODE QUI
            // S'EN CHARGE.
            //
            // Relevé par Camara le 13/09/2026 : le professeur annonce « je ne
            // vais plus te guider ni te donner d'indices » — comme le Noyau le
            // lui demande — puis valide chaque réponse deux messages plus tard
            // (« Exact, c'est juste… Bien joué »). L'élève apprend à la question
            // deux que sa méthode est bonne et aborde la trois avec la réponse à
            // moitié donnée : la note ne mesure plus rien.
            //
            // La consigne existait pourtant, et elle est relue à chaque tour —
            // mais à quinze cents lignes du message courant, au milieu de tout
            // ce qui régit un cours ORDINAIRE, où valider et encourager sont
            // exactement ce qu'on attend. Annoncée une fois, la posture tenait
            // deux échanges.
            //
            // Le serveur, lui, SAIT qu'un contrôle est ouvert. Il n'y a donc
            // rien à espérer de la mémoire du modèle : la consigne revient avec
            // le tour de l'élève, juste à côté de sa réponse. Même patron que le
            // bilan de fin de séance (voir MarqueurTemps), pour la même raison.
            //
            // Il s'éteint tout seul : `EstOuvert` devient faux dès que le bloc
            // [EVALUATION] est fermé, et le professeur retrouve alors le droit
            // de corriger et d'encourager — c'est même là que ça sert.
            var controleOuvert = LecteurEvaluation.EstOuvert(
                historique.Where(m => m.Role == "assistant").Select(m => m.Contenu));

            if (controleOuvert)
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurEvaluationEnCours();
            }

            // UN RÉSULTAT DONNÉ PAR L'ÉLÈVE SE VÉRIFIE AVANT D'ÊTRE JUGÉ — voir
            // `ReponseChiffree` : le 19/09/2026, « six » pour 3 × 2 a été reçu
            // par « Presque ! », sans que le professeur ait rien calculé.
            if (ReponseChiffree.Contient(contenu))
            {
                declencheur += "\n\n" + (controleOuvert
                    ? ReponseChiffree.RappelControle
                    : ReponseChiffree.Rappel);
            }

            // UNE EXPRESSION ÉCRITE EN COURS DE CORRECTION SE RAPPELLE AUSSI À
            // CHAQUE TOUR, et pour la même raison que le contrôle juste au-dessus :
            // le serveur sait qu'elle est ouverte, la mémoire du modèle non.
            //
            // Relevé par Camara le 18/09/2026 : API redémarrée, page rechargée, le
            // professeur reprend le texte d'une séance précédente, le remet au
            // tableau — et ne surligne rien. La consigne était là ; le rappel qui
            // part avec l'envoi du texte, lui, ne part pas sur une reprise, puisque
            // l'élève n'a rien envoyé.
            if (LecteurExpressionEcrite.EstOuvert(
                    historique.Where(m => m.Role == "assistant").Select(m => m.Contenu)))
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurExpressionEcriteEnCours();
            }

            // LA CORRECTION D'UNE DICTÉE SE FAIT DANS L'ORDRE DES NUMÉROS — voir
            // `LecteurDictee.CorrectionOuverte`. Relevé par Camara le 19/09/2026 :
            // vingt-quatre écarts numérotés au tableau, et le professeur a
            // commencé par le 5. « Dans l'ordre du texte » était dans sa consigne ;
            // il fallait le lui redire au tour même, comme pour le contrôle.
            if (LecteurDictee.CorrectionOuverte(
                    historique.Where(m => m.Role == "assistant").Select(m => m.Contenu)))
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurCorrectionDicteeEnCours();
            }

            // UNE COPIE DE CONTRÔLE EN COURS D'ENVOI : ce qui est déjà arrivé,
            // ce qui manque, et l'interdiction d'analyser avant d'avoir tout.
            // Voir MarqueurCopieControle.
            //
            // EN BILAN, À CHAQUE TOUR. Ailleurs, seulement au tour où l'élève
            // agit lui-même sur la carte de copie : le professeur accuse
            // réception de ce qui arrive, mais ne ressort pas de lui-même, en
            // cours normal, une copie laissée en plan.
            var agitSurLaCopie = LecteurCopieControle.LireChoix(contenu) is not null
                || LecteurCopieControle.LirePiece(contenu) is not null;

            // L'ÉLÈVE A DÉCLARÉ N'AVOIR QUE L'ÉNONCÉ : la réclamation de copie
            // s'arrête ici, et un fait contraire prend sa place.
            //
            // Sans cela, le rappel repartait à chaque tour et le professeur
            // réécrivait [COPIE_CONTROLE] juste après avoir dit « d'accord,
            // envoie-moi juste l'énoncé » — relevé par Camara le 20/09/2026,
            // deux fois de suite, API redémarrée et consigne bien chargée. Une
            // règle du préfixe ne pèse rien contre un fait répété au tour.
            // Voir LecteurEnonceExercice.
            //
            // LE CLIC « TA COPIE ET L'ÉNONCÉ » REFERME LA FENÊTRE AU TOUR MÊME.
            // Sans cette garde, le tour où l'enfant demande les deux portait
            // encore le fait « il n'a pas de copie » : le professeur recevait
            // deux ordres contraires dans le même message.
            var enonceSeul = !LecteurEnonceExercice.VeutAussiSaCopie(contenu)
                && LecteurEnonceExercice.DemandeOuverte(
                    historique.Where(m => m.Role == "assistant").Select(m => m.Contenu));

            var copieEnCours = !enonceSeul && (mode == ModesSeance.Bilan || agitSurLaCopie)
                ? await CopieControleAsync(contexte.Eleve.Id, contexte.Conversation.MatiereId, ct)
                : null;

            if (copieEnCours is not null) declencheur += "\n\n" + copieEnCours;

            if (enonceSeul)
            {
                declencheur += "\n\n" + LecteurEnonceExercice.Marqueur(controleMode);
            }

            // LE CONTRÔLE DU BILAN, MÊME CLOS — son numéro est ce qui permet
            // d'écrire [COPIE_CONTROLE]. Relevé par Camara le 13/09/2026 : le
            // contrôle de Thalès était déjà débriefé, plus aucun rappel ne
            // portait son numéro, et le professeur a réclamé la copie avec
            // [DEMANDE_DOCUMENT] — la question n'est jamais apparue. Voir aussi
            // le filet après la réponse, plus bas. Hors bilan, il n'y a pas de
            // contrôle dont parler.
            var controleRecent = mode == ModesSeance.Bilan
                ? await ControleDuBilanAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, controleMode!.Value, ct)
                : null;

            // Ce rappel porte lui aussi « demande-lui sa copie » : il se tait
            // quand l'élève vient de dire qu'il n'en a aucune.
            if (controleRecent is not null && copieEnCours is null && !enonceSeul)
            {
                declencheur += "\n\n" + MarqueurControleRecent(controleRecent);
            }

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

            // LA VITESSE D'ÉCOUTE, MÊME CHEMIN QUE LE TEMPS : elle vit dans le
            // navigateur, elle voyage avec le tour de l'élève, et elle n'est
            // pas persistée. Sans elle, « plus lent » n'a pas de cran de
            // référence et « tu es déjà au plus lent » est indicible.
            if (!string.IsNullOrWhiteSpace(vitesseEcoute))
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurVitesse(vitesseEcoute);
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
            // Une dictée remise au tableau par son numéro : le professeur doit
            // savoir ce qu'il y a dessus pour la corriger.
            await DeplierDicteesAuTableauAsync(historique, contexte.Eleve.Id, ct);

            var piecesHistorique = await _conversationService.GetPiecesDesMessagesAsync(
                historique.Where(m => m.Id > 0).Select(m => m.Id), ct);

            // GROUPÉES PAR MESSAGE, et non une par message : un tour peut en
            // porter plusieurs depuis le 16/09/2026. Un dictionnaire à clé
            // unique aurait levé à la deuxième.
            var parMessage = piecesHistorique
                .Where(p => p.MessageId.HasValue)
                .GroupBy(p => p.MessageId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Models.PieceJointe>)g.ToList());

            // LA GOMME, POSÉE AVANT QUE LE MODÈLE N'AIT ÉCRIT UN SEUL MOT.
            //
            // Une vraie arrivée dans un cours efface d'abord ce qui restait
            // de la dernière fois — voir TableauEfface. Les relances du
            // minuteur (fin proche, fin de séance, clôture d'un contrôle)
            // restent DANS la séance en cours : rien à effacer, le tableau
            // qu'elles trouvent est encore celui de ce cours-ci.
            if (nouvelleArrivee) yield return TableauEfface;

            // LE TABLEAU DE COMPARAISON DE LA DICTÉE, ÉCRIT PAR L'APPLICATION —
            // Camara, le 19/09/2026, pour le coût des cours de langue. Quand la
            // copie tapée arrive, le professeur retapait les deux textes au
            // tableau : 3 900 jetons de sortie dans une séance, et le risque de
            // ne pas recopier la copie à l'identique. Ici : le texte dicté relu
            // dans ses balises, la copie exactement telle qu'elle vient d'être
            // rendue. Il est envoyé AVANT sa réponse, et persisté avec elle. Au
            // cahier (photo), rien ne change : c'est lui qui transcrit.
            string? blocDicteeAjoute = null;
            var indexMarqueurClavier = contenu?.IndexOf("[DICTÉE AU CLAVIER", StringComparison.Ordinal) ?? -1;

            if (indexMarqueurClavier > 0)
            {
                var copieTapee = contenu![..indexMarqueurClavier].Trim();
                var texteDicte = await _dictees.TexteDicteCompletAsync(conversationId, contexte.Eleve.Id, ct);

                if (copieTapee.Length > 0 && !string.IsNullOrWhiteSpace(texteDicte))
                {
                    blocDicteeAjoute = LecteurDictee.TableauDeComparaison(texteDicte, copieTapee) + "\n\n";
                    declencheur += "\n\n" + PromptsPedagogiques.MarqueurTableauDicteeEcrit();
                    yield return blocDicteeAjoute;
                }
            }

            // L'écran doit l'apprendre aussi : il garde la copie deux heures,
            // et la rouvrirait pour une dictée que le professeur déclare perdue.
            if (marquerAbandonDictee) yield return LecteurDictee.Abandon;

            await foreach (var fragment in _agent.RepondreAsync(
                contexte.Eleve, contexte.Conversation, historique, declencheur, bilan,
                TypeTache.Dialogue, typeAccueil, depuis, secondesRestantes,
                parMessage, piecesDuTour, ct))
            {
                yield return fragment.Texte;
            }

            // LE FILET DE LA COPIE DE CONTRÔLE — et c'est le code qui le tend.
            //
            // Voulu par Camara le 13/09/2026 : « dès qu'on parle d'un contrôle,
            // tout ce qui est envoi de copie et d'énoncé doit forcément passer
            // par cette question ; ce mécanisme doit obligatoirement se
            // déclencher ». Si le professeur réclame la copie ou l'énoncé sans
            // avoir écrit [COPIE_CONTROLE], on l'écrit pour lui, à la fin de
            // sa réponse : l'écran affiche alors la question, à coup sûr. Le
            // bloc est invisible et muet — l'élève ne voit que la carte.
            string? blocCopieAjoute = null;

            if (controleRecent is not null && bilan.Task.IsCompletedSuccessfully)
            {
                var texteModele = (await bilan.Task).TexteComplet;

                if (LecteurCopieControle.DemandeLaCopieSansLeBloc(texteModele, contenu))
                {
                    blocCopieAjoute = $"\n[COPIE_CONTROLE]\ncontrole: {controleRecent.Id}\n[/COPIE_CONTROLE]";

                    _logger.LogInformation(
                        "Copie du controle {ControleId} demandee sans bloc : bloc ajoute (conversation {ConversationId}).",
                        controleRecent.Id, conversationId);

                    yield return blocCopieAjoute;
                }
            }

            // Le flux est terminé : on persiste la réponse et la consommation.
            // Si le client s'est déconnecté en cours de route, on enregistre quand
            // même ce qui a été produit — les tokens sont facturés dans tous les cas.
            if (bilan.Task.IsCompletedSuccessfully)
            {
                var resultat = await bilan.Task;

                // La gomme rejoint le texte PERSISTÉ, pas seulement le flux
                // envoyé au client : c'est en relisant l'historique que
                // `tableauAuto` retrouve le dernier geste sur le tableau, et
                // sans elle ici, une page rechargée à ce moment-là verrait
                // resurgir le contenu de la séance précédente.
                var contenuPersiste = nouvelleArrivee
                    ? TableauEfface + resultat.TexteComplet
                    : resultat.TexteComplet;

                // Le tableau écrit par l'application rejoint le texte persisté,
                // devant la réponse — et si le professeur en a écrit un quand
                // même, le sien part : celui de l'application fait foi.
                if (blocDicteeAjoute is not null)
                {
                    contenuPersiste = (nouvelleArrivee ? TableauEfface : string.Empty)
                        + blocDicteeAjoute
                        + LecteurDictee.SansComparaison(resultat.TexteComplet);
                }
                else if (LecteurDictee.CorrectionOuverte(
                             historique.Where(m => m.Role == "assistant").Select(m => m.Contenu)))
                {
                    // PENDANT LA CORRECTION, LE TABLEAU NE SE RÉÉCRIT PAS — Camara,
                    // le 19/09/2026, comme pour l'expression écrite : les deux
                    // textes réécrits à chaque faute réglée, sept cents jetons
                    // par tour pour rien. Le doublon part de l'historique ;
                    // l'écran garde le premier tableau (voir `tableauFige`).
                    var sansDoublon = LecteurDictee.SansComparaison(resultat.TexteComplet);

                    if (sansDoublon.Length < resultat.TexteComplet.Trim().Length)
                    {
                        contenuPersiste = (nouvelleArrivee ? TableauEfface : string.Empty) + sansDoublon;

                        _logger.LogInformation(
                            "Tableau de dictee reecrit pendant la correction : retire du message persiste (conversation {ConversationId}).",
                            conversationId);
                    }
                }

                // Le bloc ajouté par le filet rejoint le texte PERSISTÉ, pour
                // qu'une page rechargée retrouve la carte. Le signal générique
                // part avec : la carte a ses propres boutons, et laisser le
                // trombone s'allumer inviterait à contourner la question.
                if (blocCopieAjoute is not null)
                {
                    contenuPersiste = contenuPersiste.Replace("[DEMANDE_DOCUMENT]", string.Empty) + blocCopieAjoute;
                }

                // Le marqueur d'abandon est persisté avec l'accueil : c'est
                // lui qui dit, au prochain calcul, que l'annonce a été faite.
                if (marquerAbandonDictee) contenuPersiste = LecteurDictee.Abandon + contenuPersiste;

                // UNE CONCLUSION ECRITE APRES LE DEPART N A PAS DE LECTEUR.
                //
                // Ce tour-la ne sert qu a deposer les traces : [RAPPORT],
                // [FICHE], [DICTEE_CORRIGEE]. La consigne le dit en toutes
                // lettres — « personne ne lit ce que tu ecris ici » — et lui
                // demande pourtant deux phrases de constat.
                //
                // Elles sont ecrites a la TROISIEME personne, pour le parent,
                // et elles s affichaient a l enfant a son retour. Releve le
                // 11/09/2026, en tete du fil : « Bilal a rendu une dictee de
                // trois lignes... il est parti en cours de route. »
                //
                // Les blocs restent, entiers : ce sont eux qu on relit pour
                // archiver. Seul le texte libre part, puisqu il ne s adresse
                // a personne.
                if (annonce == TypeAccueil.DepartAnticipe)
                {
                    contenuPersiste = SansTexteLibre(contenuPersiste);
                }

                // LE TABLEAU DE CORRECTION D'UNE EXPRESSION ÉCRITE NE SE RÉÉCRIT
                // PAS — Camara, le 19/09/2026 : six réécritures en une séance,
                // malgré la consigne et malgré le rappel par tour. Le bloc réécrit
                // est retiré de ce qui est persisté : l'écran garde le premier
                // tableau avec ses badges, et l'historique ne relit pas six fois
                // le même texte. Voir `LecteurExpressionEcrite.SansTableauReecrit`.
                contenuPersiste = LecteurExpressionEcrite.SansTableauReecrit(
                    contenuPersiste,
                    historique.Where(m => m.Role == "assistant").Select(m => m.Contenu),
                    out var tableauRetire);

                if (tableauRetire)
                {
                    _logger.LogInformation(
                        "Tableau de correction reecrit par le professeur : retire du message persiste (conversation {ConversationId}).",
                        conversationId);
                }

                // UN JEU HORS LISTE NE PASSE PAS — Camara, le 23/09/2026 : « un
                // professeur ne va pas proposer des notions de CM2 à un CP ».
                // La liste que reçoit le professeur est déjà bornée ; ceci est
                // le filet derrière : une balise forgée, ou recopiée de travers,
                // est retirée AVANT d'être persistée. La phrase du professeur
                // reste — c'est la carte qui ne s'affichera pas, et le journal
                // dit pourquoi.
                if (Services.Jeux.LecteurJeu.Lire(contenuPersiste) is { } identifiantJeu
                    && await _jeux.AutoriseAsync(contexte.Eleve, contexte.Conversation, identifiantJeu, ct) is null)
                {
                    _logger.LogWarning(
                        "Jeu propose hors liste ({Identifiant}) dans la conversation {ConversationId} (eleve {EleveId}) : balise retiree.",
                        identifiantJeu, conversationId, contexte.Eleve.Id);
                    contenuPersiste = Services.Jeux.LecteurJeu.Retirer(contenuPersiste);
                }

                await _conversationService.AddMessageAsync(new DomainMessage
                {
                    ConversationId = conversationId,
                    Role = "assistant",
                    Contenu = contenuPersiste,
                    Modele = resultat.Modele,
                    TokensEntree = resultat.TokensEntree,
                    TokensSortie = resultat.TokensSortie,
                    TokensCacheLecture = resultat.TokensCacheLecture,
                    TokensCacheEcriture = resultat.TokensCacheEcriture,
                    TokensCacheEcriture1h = resultat.TokensCacheEcriture1h,
                    DateCreation = DateTime.UtcNow
                });

                // UNE DEMANDE DE COPIE — écrite par le professeur ou ajoutée par
                // le filet — repart de zéro : la question sera reposée.
                if (LecteurCopieControle.LireDemande(contenuPersiste) is int demandeCopie)
                {
                    try
                    {
                        await _controles.OuvrirCopieAsync(
                            contexte.Eleve.Id, demandeCopie, contexte.Conversation.MatiereId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Echec de l'ouverture de la copie du controle {ControleId}.", demandeCopie);
                    }
                }

                if (resultat.TexteComplet.Contains("[ALERTE_ADULTE]", StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "ALERTE : signal de detresse detecte dans la conversation {ConversationId} (eleve {EleveId}).",
                        conversationId, contexte.Eleve.Id);
                }

                await EnregistrerEvaluationAsync(
                    conversationId, contexte.Eleve.Id, contexte.Eleve.NiveauScolaireId,
                    resultat.TexteComplet, ct);

                await EnregistrerRapportAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet,
                    contexte.Conversation.DureeChoisieMinutes, ct);

                await EnregistrerFicheAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await EnregistrerDicteeAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await SupprimerDicteeAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await ConstaterCorrectionDicteeAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await EnregistrerComprehensionOraleAsync(
                    conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                    contexte.Eleve.Age, resultat.TexteComplet, ct);

                await SupprimerComprehensionOraleAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await EnregistrerExpressionOraleAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await EnregistrerExpressionEcriteAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet,
                    historique.Where(m => m.Role == "assistant").Select(m => m.Contenu), ct);

                await EnregistrerEvaluationPrevueAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                await EnregistrerControleProgrammeAsync(
                    conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                    contexte.Eleve.NiveauScolaireId, contexte.Eleve.Lv2Espagnol, contexte.Eleve.Specialites,
                    resultat.TexteComplet, ct);

                // LE PÉRIMÈTRE ET LE VERDICT D'UN CONTRÔLE NE S'ÉCRIVENT QUE
                // DANS SA SÉANCE DE PRÉPARATION, ET POUR LUI SEUL. Un bloc écrit
                // ailleurs — en cours normal, ou sur un autre numéro — est
                // ignoré : c'est le code qui tient le cloisonnement, pas la
                // mémoire du modèle.
                if (mode == ModesSeance.Controle)
                {
                    await EnregistrerControleNotionsAsync(
                        conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                        contexte.Eleve.NiveauScolaireId, controleMode!.Value, resultat.TexteComplet, ct);

                    await EnregistrerControlePretAsync(
                        conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                        controleMode.Value, resultat.TexteComplet, ct);
                }

                // Le résultat, lui, est un fait rapporté par l'élève : il
                // s'enregistre d'où qu'il vienne. Le dépôt revérifie le contrôle.
                await EnregistrerControleResultatAsync(
                    conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                    contexte.Eleve.NiveauScolaireId, resultat.TexteComplet, ct);

                if (mode == ModesSeance.Examen)
                {
                    await EnregistrerExamenPretAsync(
                        conversationId, contexte.Eleve.Id, contexte.Eleve.NiveauCode,
                        contexte.Conversation.MatiereId, epreuveMode!, resultat.TexteComplet, ct);
                }

                await EnregistrerEvaluationCorrigeeAsync(
                    conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);

                // FILET, PAS SEULEMENT POUR « QUITTER LE COURS ».
                //
                // La séance peut aussi se clore normalement — le temps est
                // écoulé, le professeur conclut avec [FIN_SEANCE] — sans que
                // le message de clôture pense à archiver une dictée restée en
                // cours de correction. C'est arrivé : le [RAPPORT] du même
                // message disait « la correction a commencé mais le temps a
                // manqué pour aller au bout », et pourtant aucun
                // [DICTEE_CORRIGEE] n'accompagnait ce constat. `Archiver...`
                // ne fait rien si tout est déjà en ordre — l'appeler à chaque
                // clôture ne coûte donc rien la plupart du temps.
                if (resultat.TexteComplet.Contains("[FIN_SEANCE]", StringComparison.Ordinal))
                {
                    // La séance se referme pendant la dictée, avant la copie :
                    // même règle qu'un départ — annulée, pas archivée.
                    await AbandonnerDicteeAsync(conversationId, contexte.Eleve.Id);
                    await ArchiverDicteeAbandonneeAsync(conversationId, contexte.Eleve.Id);

                    // Même filet pour l'écoute : la séance se referme, ce qui
                    // n'est pas archivé maintenant ne le sera jamais.
                    await RattraperComprehensionsOralesAsync(conversationId, ct);
                    await RattraperExpressionsOralesAsync(conversationId, ct);
                    await RattraperExpressionsEcritesAsync(conversationId, ct);
                }
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
            int conversationId, int eleveId, string texte, int? dureeChoisieMinutes, CancellationToken ct)
        {
            var declare = LecteurRapport.Lire(texte);
            if (declare is null) return;

            try
            {
                var enregistre = await _rapports.EnregistrerAsync(
                    eleveId, conversationId,
                    declare.Travaille, declare.NoteComprehension, declare.NoteRevision,
                    declare.Remarque, declare.ARevoir, dureeChoisieMinutes, ct);

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

        /// <summary>
        /// Archive la dictée si le professeur vient d'en corriger une.
        /// Silencieux en cas d'échec, comme les autres blocs — c'est la trace
        /// qui manque, jamais la conversation qui échoue.
        /// </summary>
        private async Task EnregistrerDicteeAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var declaree = LecteurDictee.Lire(texte);
            if (declaree is null) return;

            try
            {
                // LE TEXTE DICTÉ ET LA COPIE VIENNENT DES MESSAGES, PAS DE CE QUE
                // LE PROFESSEUR RECOPIE.
                //
                // Relevé le 11/09/2026 : une dictée de plusieurs phrases archivée
                // avec UNE SEULE — celle qu'il venait de relire. Depuis le
                // 19/09/2026 il peut laisser `dicte` et `copie` VIDES : les
                // balises de dictée portent le texte mot pour mot, et la copie
                // rendue est dans le message de l'élève. S'il les écrit quand
                // même, on ne prend le texte relu que s'il en dit plus.
                var dicte = declaree.Dicte;

                // LE TEXTE RELU L'EMPORTE DÈS QU'IL EXISTE — et non plus seulement
                // s'il est plus long (corrigé le 20/09/2026) : « plus long » avait
                // gardé une copie du professeur gonflée de sa propre remarque.
                // Les balises et le message de l'élève sont la source ; ce que le
                // professeur recopie n'est qu'un repli.
                var relu = await _dictees.TexteDicteCompletAsync(conversationId, eleveId, ct);
                if (!string.IsNullOrWhiteSpace(relu) && relu != dicte)
                {
                    _logger.LogInformation(
                        "Texte dicte repris depuis les messages : {Avant} -> {Apres} caracteres "
                        + "(conversation {ConversationId}).",
                        dicte.Length, relu.Length, conversationId);

                    dicte = relu;
                }

                if (string.IsNullOrWhiteSpace(dicte))
                {
                    _logger.LogWarning(
                        "Dictee non archivee : aucun texte dicte, ni dans le bloc ni dans les messages "
                        + "(conversation {ConversationId}).", conversationId);
                    return;
                }

                // UNE DICTÉE SANS COPIE NE S'ARCHIVE PAS, QUOI QUE LE MODÈLE
                // ÉCRIVE. La consigne le dit ; le code le garantit. Sans copie
                // arrivée — tapée et rendue, ou en photo —, un bloc
                // [DICTEE_CORRIGEE] porterait une copie inventée, ou une
                // réplique de l'élève prise pour elle.
                if (!await _dictees.PeutArchiverAsync(conversationId, eleveId, dicte, ct))
                {
                    _logger.LogWarning(
                        "Dictee non archivee : aucune copie recue pour la conversation {ConversationId}.",
                        conversationId);
                    return;
                }

                var copie = declaree.Copie;

                var copieRelue = await _dictees.CopieCompleteAsync(
                    conversationId, eleveId, dicte, ct);

                if (!string.IsNullOrWhiteSpace(copieRelue) && copieRelue != copie)
                {
                    _logger.LogInformation(
                        "Copie reprise depuis les messages : {Avant} -> {Apres} caracteres "
                        + "(conversation {ConversationId}).",
                        copie.Length, copieRelue.Length, conversationId);

                    copie = copieRelue;
                }

                if (string.IsNullOrWhiteSpace(copie))
                {
                    _logger.LogWarning(
                        "Dictee non archivee : aucune copie, ni dans le bloc ni dans les messages "
                        + "(conversation {ConversationId}).", conversationId);
                    return;
                }

                var enregistree = await _dictees.AjouterAsync(
                    eleveId, conversationId,
                    declaree.Titre, declaree.Etat, dicte, copie, declaree.Remarque, ct);

                if (enregistree is null)
                {
                    _logger.LogWarning(
                        "Dictee ignoree : conversation {ConversationId} introuvable pour l'eleve {EleveId}.",
                        conversationId, eleveId);
                    return;
                }

                _logger.LogInformation(
                    "Dictee corrigee archivee pour l'eleve {EleveId} (conversation {ConversationId}).",
                    eleveId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement de la dictee (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Archive l'exercice de compréhension orale si le professeur vient
        /// d'en conclure un. Silencieux en cas d'échec, comme les autres
        /// blocs — c'est la trace qui manque, jamais la conversation qui
        /// échoue.
        ///
        /// L'AUDIO EST RÉGÉNÉRÉ ICI, UNE SEULE FOIS — voir la remarque sur
        /// <see cref="Entities.ComprehensionOrale"/>. Si la synthèse échoue
        /// (quota, panne), on archive quand même le texte : mieux vaut une
        /// fiche sans audio qu'aucune fiche du tout.
        /// </summary>
        /// <summary>
        /// Archive la conversation d'expression orale si le professeur vient
        /// d'en conclure une — voulu par Camara le 18/09/2026.
        ///
        /// BEAUCOUP PLUS SIMPLE QUE LA COMPRÉHENSION ORALE juste en dessous :
        /// aucun audio à synthétiser, et rien à contre-vérifier dans les messages
        /// bruts. Ce que le professeur recopie EST la conversation — les deux
        /// ont parlé, il rend les deux voix.
        ///
        /// Silencieux en cas d'échec, comme les autres blocs : c'est la trace
        /// qui manque, jamais la conversation qui échoue.
        /// </summary>
        private async Task EnregistrerExpressionOraleAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var declaree = LecteurExpressionOrale.Lire(texte);

            if (declaree is null)
            {
                // LA BALISE EST LÀ MAIS LE CONTENU N'A PAS ÉTÉ RETENU.
                //
                // Même filet que pour la compréhension orale, et pour la même
                // raison : un bloc mal formé disparaissait sans laisser de trace,
                // et il a fallu un exercice perdu pour s'en apercevoir. `Lire`
                // reste silencieux par conception ; c'est ici qu'on distingue
                // « la balise n'a jamais été posée » de « posée mais rejetée ».
                if (texte.Contains("[EXPRESSION_ORALE]", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Bloc [EXPRESSION_ORALE] present mais non retenu (conversation "
                        + "{ConversationId}) -- titre, langue ou tours manquants.",
                        conversationId);
                }

                return;
            }

            try
            {
                var tours = declaree.Echange
                    .Select(t => new Domain.Models.TourExpressionOrale(t.Qui, t.Texte))
                    .ToList();

                // SA PREMIÈRE PHRASE NE SE PERD PAS — Camara, le 18/09/2026.
                //
                // Le professeur recopie la conversation lui-même, et il commence
                // systématiquement par la réponse de l'enfant : son propre
                // « Hello! What would you like? » disparaissait, et l'échange relu
                // s'ouvrait sur une réponse à une question absente.
                //
                // LA CONSIGNE A ÉTÉ CORRIGÉE — son exemple commençait par
                // `eleve:`, ce qui lui montrait exactement quoi faire — mais une
                // consigne ne vaut pas un fait. Sa vraie première réplique est
                // dans les messages : on la relit, et on la remet devant si elle
                // manque.
                if (tours.Count > 0 && tours[0].Qui == LecteurExpressionOrale.Eleve)
                {
                    var premiere = await _expressionsOrales.PremiereRepliqueAsync(
                        conversationId, eleveId, ct);

                    // ELLE NE DOIT PAS DÉJÀ Y ÊTRE PLUS BAS : le professeur peut
                    // avoir décalé son tour au lieu de l'oublier. La comparer
                    // évite de la compter deux fois.
                    if (!string.IsNullOrWhiteSpace(premiere)
                        && !tours.Any(t => string.Equals(
                            t.Texte.Trim(), premiere.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        tours.Insert(0, new Domain.Models.TourExpressionOrale(
                            LecteurExpressionOrale.Professeur, premiere));

                        _logger.LogInformation(
                            "Premiere replique du professeur remise en tete de "
                            + "l'expression orale (conversation {ConversationId}).",
                            conversationId);
                    }
                }

                var enregistree = await _expressionsOrales.AjouterAsync(
                    eleveId, conversationId, declaree.Titre, declaree.Langue,
                    tours, declaree.Remarque, ct: ct);

                if (enregistree is not null)
                {
                    _logger.LogInformation(
                        "Expression orale {Id} archivee ({Tours} tours, {Langue}) "
                        + "pour l'eleve {EleveId}.",
                        enregistree.Id, tours.Count, declaree.Langue, eleveId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'archivage de l'expression orale (conversation "
                    + "{ConversationId}).", conversationId);
            }
        }

        /// <summary>
        /// Archive le texte écrit que le professeur vient de corriger.
        ///
        /// SA CONTRE-VÉRIFICATION EST L'INVERSE DES AUTRES. Ailleurs, on
        /// rattrape ce que le modèle OUBLIE ; ici, on rattrape ce qu'il
        /// CORRIGE. La consigne lui demande de recopier la copie « mot pour
        /// mot, fautes comprises » — or corriger en recopiant est son réflexe
        /// le plus profond, et il détruit exactement ce qu'on voulait garder :
        /// une copie nettoyée rend la correction d'à côté incompréhensible,
        /// puisqu'elle parle de fautes devenues invisibles.
        ///
        /// Quand le texte a été TAPÉ, il n'y a rien à deviner : le message de
        /// l'enfant est en base, et on le préfère à la transcription.
        ///
        /// Silencieux en cas d'échec, comme les autres blocs.
        /// </summary>
        private async Task EnregistrerExpressionEcriteAsync(
            int conversationId, int eleveId, string texte,
            IEnumerable<string?> messagesProfesseur, CancellationToken ct)
        {
            var declaree = LecteurExpressionEcrite.Lire(texte);

            if (declaree is null)
            {
                // La balise est là mais le contenu n'a pas été retenu : même
                // filet que pour les deux autres exercices. `Lire` est
                // silencieux par conception ; c'est ici qu'on distingue « la
                // balise n'a jamais été posée » de « posée mais rejetée ».
                if (texte.Contains("[EXPRESSION_ECRITE]", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Bloc [EXPRESSION_ECRITE] present mais non retenu (conversation "
                        + "{ConversationId}) -- titre, langue ou consigne manquant.",
                        conversationId);
                }

                return;
            }

            try
            {
                var copie = declaree.Texte;

                // SA COPIE TELLE QU'IL L'A TAPÉE, quand elle existe en base.
                //
                // ON NE LA SUBSTITUE QUE SI ELLE DIFFÈRE VRAIMENT : un simple
                // écart d'espaces ou de retours à la ligne ne justifie pas de
                // remplacer ce que le professeur a recopié, et le journal ne
                // doit crier que quand il s'est réellement passé quelque chose.
                var tape = await _expressionsEcrites.TexteTapeAsync(conversationId, eleveId, ct);

                if (!string.IsNullOrWhiteSpace(tape) && !MemeTexte(tape, copie))
                {
                    _logger.LogInformation(
                        "Texte de l'eleve repris depuis ses messages : la transcription "
                        + "du professeur differait (conversation {ConversationId}).",
                        conversationId);

                    copie = tape;
                }

                // SUR CAHIER, le texte n'est que sur la photo — et au tableau,
                // sous « Ton texte », transcrit par le professeur. Depuis le
                // 19/09/2026 il peut laisser `texte` vide : on le reprend là.
                if (string.IsNullOrWhiteSpace(copie))
                {
                    copie = LecteurExpressionEcrite.TexteDuTableau(messagesProfesseur.Append(texte)) ?? string.Empty;

                    if (copie.Length == 0)
                    {
                        _logger.LogWarning(
                            "Expression ecrite non archivee : aucun texte, ni tape, ni au tableau "
                            + "(conversation {ConversationId}).", conversationId);
                        return;
                    }

                    _logger.LogInformation(
                        "Texte de l'eleve repris du tableau pour l'archive (conversation {ConversationId}).",
                        conversationId);
                }

                var corrections = declaree.Corrections
                    .Select(c => new Domain.Models.RepriseEcrite(c.Genre, c.Texte))
                    .ToList();

                // LES BADGES DANS L'ARCHIVE — Camara, le 19/09/2026. La section
                // « Ton texte » du premier tableau surligné, gardée telle quelle ;
                // seulement si c'est bien la même copie que celle archivée, sans
                // quoi les badges tomberaient sur les mauvais mots.
                var surligne = LecteurExpressionEcrite.TexteSurligneDuTableau(messagesProfesseur.Append(texte));
                var texteSurligne = surligne is not null && MemeTexte(SansSurlignes(surligne), copie)
                    ? surligne
                    : null;

                // IL REPREND UNE COPIE RESTÉE EN PHOTO, il n'en archive pas une
                // neuve. La ligne existe déjà, avec son titre et sa consigne du
                // jour de l'exercice : on ne fait qu'y poser la transcription et
                // la correction.
                if (declaree.Numero is { } numero)
                {
                    var completee = await _expressionsEcrites.CompleterAsync(
                        numero, eleveId, copie, corrections, declaree.Remarque,
                        texteSurligne: texteSurligne, ct: ct);

                    if (completee is not null)
                    {
                        _logger.LogInformation(
                            "Expression ecrite {Id} enfin transcrite ({Reprises} reprise(s)) "
                            + "pour l'eleve {EleveId}.",
                            numero, corrections.Count, eleveId);
                    }
                    else
                    {
                        // ELLE N'EXISTE PAS, N'EST PAS LA SIENNE, OU A DÉJÀ SON
                        // TEXTE. On n'écrase jamais une transcription — et on ne
                        // crée pas une ligne neuve en repli, ce qui doublerait le
                        // texte au lieu de le compléter.
                        _logger.LogWarning(
                            "Expression ecrite {Id} non completee (inexistante, deja "
                            + "transcrite, ou d'un autre eleve) -- eleve {EleveId}.",
                            numero, eleveId);
                    }

                    return;
                }

                var enregistree = await _expressionsEcrites.AjouterAsync(
                    eleveId, conversationId, declaree.Titre, declaree.Langue,
                    declaree.Consigne, copie, corrections, declaree.Remarque,
                    texteSurligne: texteSurligne, ct: ct);

                if (enregistree is not null)
                {
                    _logger.LogInformation(
                        "Expression ecrite {Id} archivee ({Reprises} reprise(s), {Langue}) "
                        + "pour l'eleve {EleveId}.",
                        enregistree.Id, corrections.Count, declaree.Langue, eleveId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'archivage de l'expression ecrite (conversation "
                    + "{ConversationId}).", conversationId);
            }
        }

        /// <summary>
        /// Deux textes qui ne diffèrent que par leurs espaces sont le même.
        ///
        /// LA CASSE ET LA PONCTUATION COMPTENT, ELLES : « frend » contre
        /// « friend », « i » contre « I », un point oublié — ce sont justement
        /// les fautes qu'on veut voir survivre. On ne tolère que ce qui ne veut
        /// rien dire : les blancs.
        /// </summary>
        /// <summary>Le texte sans ses marques <c>==mot==</c>.</summary>
        private static string SansSurlignes(string texte) =>
            System.Text.RegularExpressions.Regex.Replace(texte, @"==([^=\n]+?)==", "$1");

        private static bool MemeTexte(string a, string b)
        {
            static string Serrer(string t) =>
                string.Join(" ", t.Split(
                    [' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries));

            return string.Equals(Serrer(a), Serrer(b), StringComparison.Ordinal);
        }

        private async Task EnregistrerComprehensionOraleAsync(
            int conversationId, int eleveId, int matiereId, int age, string texte, CancellationToken ct)
        {
            var declaree = LecteurComprehensionOrale.Lire(texte);

            if (declaree is null)
            {
                // LA BALISE EST LÀ MAIS LE CONTENU N'A PAS ÉTÉ RETENU.
                //
                // Sans ce log, un bloc mal formé disparaît sans laisser de
                // trace — c'est exactement ce qui est arrivé le 10/09/2026 :
                // un exercice réel, une archive perdue, et rien dans les
                // journaux pour le dire. `Lire` reste silencieux par
                // conception (voir les autres `EnregistrerXAsync`) ; c'est
                // donc ici, au point d'appel, qu'on distingue « la balise
                // n'a jamais été posée » (rien à signaler) de « posée mais
                // rejetée » (à comprendre).
                if (texte.Contains("[COMPREHENSION_ORALE]", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Bloc [COMPREHENSION_ORALE] present mais non retenu (conversation {ConversationId}) "
                        + "-- champ obligatoire manquant ou mal forme.",
                        conversationId);
                }

                return;
            }

            try
            {
                byte[]? audio = null;

                try
                {
                    var matieres = await _referentielService.GetMatieresAsync(activesSeulement: false);
                    var avatar = matieres.FirstOrDefault(m => m.Id == matiereId)?.ProfAvatar;

                    audio = await _syntheseVocale.SynthetiserWavAsync(
                        declaree.Passage, avatar, age, dictee: false, langue: declaree.Langue, ct: ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Echec de la synthese audio pour la comprehension orale "
                        + "(conversation {ConversationId}) : archivage en texte seul.",
                        conversationId);
                }

                // CE QUE L ELEVE A DIT NE SE RESUME PAS, IL SE CITE.
                //
                // Le professeur ne recopie pas toujours sa reponse en entier :
                // le 10/09/2026, une explication longue s est retrouvee reduite
                // a sa derniere phrase. On relit donc les messages, et si sa
                // vraie reponse est plus complete que celle citee, c est elle
                // qui est archivee. Les langues se comportent ainsi toutes de
                // la meme facon, sans dependre de ce que le modele a recopie.
                var reponse = declaree.ReponseEleve;

                var retrouvee = await _comprehensionsOrales.MeilleureReponseAsync(
                    conversationId, eleveId, declaree.Passage, ct);

                if (!string.IsNullOrWhiteSpace(retrouvee)
                    && retrouvee.Length > (reponse?.Length ?? 0))
                {
                    reponse = retrouvee;
                }

                var enregistree = await _comprehensionsOrales.AjouterAsync(
                    eleveId, conversationId,
                    // LE MEME TITRE PARTOUT : LE DEBUT DU PASSAGE.
                    //
                    // Le professeur en inventait un de son cru — « Leo et sa
                    // grand-mere au marche » — la ou l anglais affichait les
                    // premiers mots du texte ecoute. Deux presentations pour la
                    // meme chose, et un titre qui ne correspondait a aucun mot
                    // de l audio. On derive donc toujours du passage : c est
                    // factuel, c est identique dans toutes les langues, et les
                    // points de suspension y gardent leur sens.
                    TitreDepuisPassage(declaree.Passage),
                    declaree.Langue, declaree.Passage, reponse ?? string.Empty,
                    declaree.Comprehension, declaree.Remarque, audio, ct: ct);

                if (enregistree is null)
                {
                    _logger.LogWarning(
                        "Comprehension orale ignoree : conversation {ConversationId} introuvable "
                        + "pour l'eleve {EleveId}.",
                        conversationId, eleveId);
                    return;
                }

                _logger.LogInformation(
                    "Comprehension orale archivee pour l'eleve {EleveId} (conversation {ConversationId}).",
                    eleveId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement de la comprehension orale (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Un titre lisible pour un exercice que le professeur n'a pas
        /// nommé lui-même.
        ///
        /// « Compréhension orale », répété douze fois dans la liste, ne
        /// permet pas de retrouver celui qu'on cherche. La première phrase
        /// du passage, elle, dit de quoi il parle — c'est déjà un repère.
        /// </summary>
        private static string TitreDepuisPassage(string passage)
        {
            var propre = passage.Replace((char)10, ' ').Replace((char)13, ' ').Trim();

            var fin = propre.IndexOfAny(['.', '!', '?']);
            if (fin > 0) propre = propre[..fin];

            propre = propre.Trim();

            return propre.Length <= 80 ? propre : propre[..77].TrimEnd() + "…";
        }

        /// <summary>
        /// Enregistre le report d'une évaluation proposée « à la prochaine
        /// fois ». Silencieux en cas d'échec, comme les autres blocs.
        /// </summary>
        private async Task EnregistrerEvaluationPrevueAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var declaree = LecteurEvaluationPrevue.Lire(texte);
            if (declaree is null) return;

            // GARDE-FOU : jamais les deux dans le même message. Si le
            // contrôle a vraiment lieu ici, c'est le chemin [EVALUATION] qui
            // fait foi — [EVALUATION_PREVUE] n'a rien à archiver.
            if (LecteurEvaluation.Lire(texte) is not null) return;

            try
            {
                var enregistree = await _evaluationsPrevues.EnregistrerAsync(
                    eleveId, conversationId, declaree.Notion, ct);

                if (enregistree is null)
                {
                    _logger.LogWarning(
                        "Evaluation prevue ignoree : conversation {ConversationId} introuvable pour l'eleve {EleveId}.",
                        conversationId, eleveId);
                    return;
                }

                _logger.LogInformation(
                    "Evaluation prevue enregistree pour l'eleve {EleveId} (conversation {ConversationId}).",
                    eleveId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement de l'evaluation prevue (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Archive un contrôle que l'élève vient d'annoncer en séance.
        ///
        /// SANS DATE, ON N'ARCHIVE RIEN — voir LecteurControleProgramme : la
        /// date est le seul champ qui donne un jour au contrôle dans le
        /// calendrier. Silencieux en cas d'échec, comme les autres
        /// EnregistrerXAsync : un bloc mal formé ne casse jamais le tour.
        /// </summary>
        private async Task EnregistrerControleProgrammeAsync(
            int conversationId, int eleveId, int matiereConversation,
            int? niveauScolaireId, bool lv2Espagnol, IReadOnlyCollection<string> specialites, string texte,
            CancellationToken ct)
        {
            var declare = LecteurControleProgramme.Lire(texte);
            if (declare is null || declare.Date is null) return;

            try
            {
                // LE MOT DU MODÈLE EST RAPPROCHÉ D'UNE LISTE FERMÉE, jamais
                // pris pour un identifiant. En cas de doute, la matière de la
                // conversation — voir `ResolveurMatiereDeclaree`.
                var matiereId = ResolveurMatiereDeclaree.Resoudre(
                    declare.Matiere,
                    await MatieresDeLEleveAsync(niveauScolaireId, lv2Espagnol, specialites),
                    matiereConversation);

                var enregistre = await _controles.EnregistrerDepuisConversationAsync(
                    eleveId, conversationId, matiereId, declare.Sujet,
                    declare.Date.Value, declare.Heure, ct);

                if (enregistre is null)
                {
                    _logger.LogWarning(
                        "Controle programme ignore : conversation {ConversationId} introuvable pour l'eleve {EleveId}.",
                        conversationId, eleveId);
                    return;
                }

                // Les notions ne sont retenues que dans la matière du
                // professeur : il ne connaît pas le programme des autres, et
                // le bloc lui dit de ne pas en écrire hors de chez lui.
                if (declare.Notions.Count > 0 && matiereId == matiereConversation)
                {
                    await AjouterNotionsAsync(
                        eleveId, enregistre.Id, matiereId, niveauScolaireId,
                        declare.Notions, travaillee: false, ct);
                }
                else
                {
                    // Rien de déclaré — soit le professeur a oublié, soit le
                    // contrôle est dans une autre matière que la sienne. Le
                    // programme se déduit alors du sujet, contre le
                    // référentiel de la MATIÈRE DU CONTRÔLE : c'est elle qui
                    // décide du programme, pas celle où l'élève se trouve.
                    await DeduireProgrammeAsync(
                        eleveId, enregistre.Id, matiereId, niveauScolaireId, declare.Sujet, ct);
                }

                _logger.LogInformation(
                    "Controle programme {ControleId} enregistre pour l'eleve {EleveId} (matiere {MatiereId}).",
                    enregistre.Id, eleveId, matiereId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement du controle programme (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Le professeur complète le programme d'un contrôle déjà enregistré,
        /// après l'avoir travaillé. Silencieux en cas d'échec, comme les
        /// autres blocs.
        /// </summary>
        /// <summary>
        /// UNE CORRECTION D'ÉVALUATION REMISE AU PROCHAIN COURS OUVRE CE COURS.
        ///
        /// Voulu par Camara le 13/09/2026 : quand le temps manque, le
        /// professeur donne la note tout de suite et remet la reprise des
        /// erreurs à la séance suivante. Sans ce rappel, « on la fera au
        /// prochain cours » serait une promesse que personne ne tient : la
        /// copie peut ne plus figurer dans la fenêtre d'historique qu'il a
        /// sous les yeux.
        ///
        /// Porté par le tour d'accueil, avec les questions à reprendre — même
        /// raison que la dictée en attente : une consigne seule lui demanderait
        /// de corriger ce qu'il ne voit pas.
        ///
        /// COMPTÉ AU MOMENT OÙ ON PROPOSE, comme le bilan d'un contrôle passé :
        /// c'est la proposition qui pèse sur l'élève. Passé le plafond, on n'en
        /// reparle plus, même si le professeur n'a rien clos.
        /// </summary>
        private async Task<string?> CorrectionEnAttenteAsync(int eleveId, int matiereId, CancellationToken ct)
        {
            try
            {
                var copie = await _evaluations.GetCorrectionEnAttenteAsync(eleveId, matiereId, ct);
                if (copie is null) return null;

                await _evaluations.MarquerRelanceCorrectionAsync(eleveId, copie.Id, ct);

                // Seulement ce qui est à reprendre : une question juste n'a
                // rien à apprendre, et la lister ferait croire au professeur
                // qu'il doit y revenir.
                var aReprendre = copie.Questions
                    .Where(q => q.Verdict is "faux" or "partiel")
                    .Select(q => $"- {q.Enonce} — il a répondu « {q.Reponse} » ({q.Verdict}). "
                        + $"Correction : {q.Commentaire}");

                var liste = string.Join("\n", aReprendre);

                return $"[La dernière fois, vous n'avez pas eu le temps de corriger son évaluation "
                    + $"du {copie.DateCreation:dd/MM} sur « {copie.Notion ?? "sujet non précisé"} » "
                    + $"(n° {copie.Id}, {copie.Note:0.#}/20). Il connaît déjà sa note. "
                    + "PROPOSE-LUI de commencer par reprendre ses erreurs — tu proposes, tu "
                    + "n'imposes pas. Qu'elle soit faite ou qu'il préfère passer à autre "
                    + $"chose, écris [EVALUATION_CORRIGEE]{copie.Id}[/EVALUATION_CORRIGEE] "
                    + "et n'y reviens plus.\n"
                    + (liste.Length > 0 ? "Les questions à reprendre :\n" + liste : "")
                    + "]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture de la correction en attente de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// La correction remise au prochain cours a eu lieu — ou l'élève n'a pas
        /// voulu : on n'en reparle plus. Silencieux, comme tous les archivages.
        /// </summary>
        private async Task EnregistrerEvaluationCorrigeeAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            if (LecteurEvaluationCorrigee.Lire(texte) is not int evaluationId) return;

            try
            {
                var close = await _evaluations.CloreCorrectionAsync(eleveId, evaluationId, ct);

                if (!close)
                {
                    _logger.LogWarning(
                        "Correction d'evaluation ignoree : evaluation {EvaluationId} introuvable pour l'eleve {EleveId}.",
                        evaluationId, eleveId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la cloture de correction (conversation {ConversationId}).", conversationId);
            }
        }

        /// <summary>
        /// Le professeur vient de dire si l'élève est prêt pour son contrôle.
        ///
        /// POURQUOI CE VERDICT EST DEMANDÉ AU MODÈLE ET NON CALCULÉ. C'est
        /// l'exception assumée à la règle du projet — ici, il n'y a rien à
        /// imposer dans le code, parce que la donnée n'existe nulle part
        /// ailleurs. La barre mesure ce qui est acquis ; elle ignore ce que le
        /// contrôle demandera. Voulu par Camara le 13/09/2026 : « c'est pas
        /// parce qu'on est pas à 100 % partout que l'élève n'est pas prêt ».
        ///
        /// Ce que le code garde pour lui, en revanche : savoir si la révision a
        /// commencé (un FAIT, déduit des préparations et des notions
        /// travaillées) et le cloisonnement par matière, revérifié par le
        /// dépôt. Le modèle ne fournit que le jugement.
        ///
        /// Silencieux : rater ce verdict ne doit pas faire perdre la séance.
        /// </summary>
        private async Task EnregistrerControlePretAsync(
            int conversationId, int eleveId, int matiereId, int controleCible,
            string texte, CancellationToken ct)
        {
            var declare = LecteurControlePret.Lire(texte);
            if (declare is null) return;

            if (declare.Controle != controleCible)
            {
                _logger.LogWarning(
                    "Verdict ignore : controle {ControleId} ecrit dans la preparation du controle {Cible} "
                    + "(conversation {ConversationId}).",
                    declare.Controle, controleCible, conversationId);
                return;
            }

            try
            {
                var pose = await _controles.PoserVerdictPretAsync(
                    eleveId, declare.Controle, matiereId,
                    declare.Verdict, declare.Observation, DateTime.UtcNow, ct);

                if (!pose)
                {
                    _logger.LogWarning(
                        "Verdict de preparation ignore : controle {ControleId} introuvable pour "
                        + "l'eleve {EleveId} en matiere {MatiereId}.",
                        declare.Controle, eleveId, matiereId);
                    return;
                }

                _logger.LogInformation(
                    "Controle {ControleId} : l'eleve {EleveId} est declare «{Verdict}».",
                    declare.Controle, eleveId, declare.Verdict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement du verdict de preparation (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Le verdict du professeur sur l'épreuve préparée — le pendant
        /// d'`EnregistrerControlePretAsync`. Seulement pour l'épreuve de la
        /// séance ; le dépôt plafonne le verdict comme pour un contrôle.
        /// </summary>
        private async Task EnregistrerExamenPretAsync(
            int conversationId, int eleveId, string? niveauCode, int matiereId, string epreuveCible,
            string texte, CancellationToken ct)
        {
            var declare = LecteurExamenPret.Lire(texte);
            if (declare is null) return;

            if (!string.Equals(declare.Epreuve, epreuveCible, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Verdict d'examen ignore : epreuve {Epreuve} ecrite dans la preparation de {Cible} "
                    + "(conversation {ConversationId}).",
                    declare.Epreuve, epreuveCible, conversationId);
                return;
            }

            try
            {
                var epreuve = await _examens.GetEpreuveApplicableAsync(niveauCode, epreuveCible, ct);
                if (epreuve is null) return;

                var pose = await _examens.PoserVerdictAsync(
                    eleveId, epreuve.Id, matiereId, declare.Verdict, declare.Observation, DateTime.UtcNow, ct);

                _logger.LogInformation(
                    "Epreuve {Epreuve} : verdict «{Verdict}» {Pose} pour l'eleve {EleveId}.",
                    epreuveCible, declare.Verdict, pose ? "pose" : "refuse", eleveId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement du verdict d'examen (conversation {ConversationId}).",
                    conversationId);
            }
        }

        private async Task EnregistrerControleNotionsAsync(
            int conversationId, int eleveId, int matiereId,
            int? niveauScolaireId, int controleCible, string texte, CancellationToken ct)
        {
            var declare = LecteurControleNotions.Lire(texte);
            if (declare is null) return;

            if (declare.Controle != controleCible)
            {
                _logger.LogWarning(
                    "Notions ignorees : controle {ControleId} ecrit dans la preparation du controle {Cible} "
                    + "(conversation {ConversationId}).",
                    declare.Controle, controleCible, conversationId);
                return;
            }

            try
            {
                // UNE DÉCLARATION INCOMPLÈTE SE COMPLÈTE EN PARLANT. « J'ai un
                // contrôle de maths » devient « les triangles semblables et le
                // théorème de Thalès » une fois que le professeur a posé la
                // question — et c'est ce sujet-là qui doit rester.
                var sujetPrecise = false;

                if (declare.Sujet is not null)
                {
                    sujetPrecise = await _controles.PreciserSujetAsync(
                        eleveId, declare.Controle, matiereId, declare.Sujet, ct);
                }

                // La matière de la conversation, sans rapprochement possible :
                // poser le périmètre d'une préparation est un acte pédagogique,
                // un professeur ne le fait que chez lui. Le dépôt revérifie.
                var posees = declare.Notions.Count > 0
                    ? await AjouterNotionsAsync(
                        eleveId, declare.Controle, matiereId, niveauScolaireId,
                        declare.Notions, travaillee: true, ct)
                    : 0;

                // Le sujet vient d'être précisé mais le professeur n'a pas
                // listé les notions : on les déduit du nouveau sujet, comme à
                // la création. Il a fait le plus dur — obtenir de l'élève ce
                // qu'il y aura vraiment dessus.
                if (posees == 0 && sujetPrecise)
                {
                    await DeduireProgrammeAsync(
                        eleveId, declare.Controle, matiereId, niveauScolaireId, declare.Sujet, ct);
                }

                if (posees == 0 && !sujetPrecise)
                {
                    _logger.LogWarning(
                        "Notions de controle ignorees : controle {ControleId} introuvable pour l'eleve {EleveId} en matiere {MatiereId}.",
                        declare.Controle, eleveId, matiereId);
                    return;
                }

                _logger.LogInformation(
                    "Controle {ControleId} complete (eleve {EleveId}) : sujet {Sujet}, {Nombre} notion(s).",
                    declare.Controle, eleveId, sujetPrecise ? "precise" : "inchange", posees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement des notions de controle (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Enregistre ce que le contrôle a donné, et RENVOIE LES LACUNES AU
        /// MOTEUR DE MAÎTRISE.
        ///
        /// C'est le geste qui referme la boucle : une notion ratée sur la
        /// copie corrigée devient une observation « echoue », donc une lacune,
        /// donc quelque chose que le professeur retrouvera dans son contexte
        /// aux séances suivantes. Sans ça, le résultat d'un contrôle resterait
        /// une ligne dans une fiche que personne ne relit.
        /// </summary>
        private async Task EnregistrerControleResultatAsync(
            int conversationId, int eleveId, int matiereId,
            int? niveauScolaireId, string texte, CancellationToken ct)
        {
            var declare = LecteurControleResultat.Lire(texte);
            if (declare is null) return;

            try
            {
                var referentiel = niveauScolaireId is int niveau
                    ? (await _maitrises.GetNotionsDuNiveauAsync(matiereId, niveau, ct)).ToList()
                    : [];

                var enregistre = await _controles.EnregistrerResultatAsync(
                    eleveId, declare.Controle, matiereId,
                    declare.Note, declare.Ressenti,
                    Rapprocher(declare.Reussies, referentiel),
                    Rapprocher(declare.Ratees, referentiel),
                    DateTime.UtcNow, ct);

                if (enregistre is null)
                {
                    _logger.LogWarning(
                        "Resultat de controle ignore : controle {ControleId} introuvable pour l'eleve {EleveId} en matiere {MatiereId}.",
                        declare.Controle, eleveId, matiereId);
                    return;
                }

                await PorterAuSuiviAsync(eleveId, enregistre, ct);

                // UN RÉSULTAT QUI DIT QUELQUE CHOSE DE LA COPIE clôt son
                // analyse — et seulement lui. Un bloc écrit au récit, avec le
                // seul ressenti, ne doit pas éteindre le rappel qui attend
                // encore la feuille. Le dépôt revérifie de son côté que toutes
                // les pièces sont là.
                if (declare.Note is not null || declare.Reussies.Count > 0 || declare.Ratees.Count > 0)
                {
                    await _controles.MarquerCopieAnalyseeAsync(
                        eleveId, declare.Controle, DateTime.UtcNow, ct);
                }

                _logger.LogInformation(
                    "Resultat du controle {ControleId} enregistre (eleve {EleveId}, note {Note}).",
                    declare.Controle, eleveId, declare.Note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement du resultat de controle (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// Porte au moteur de maîtrise ce que la copie corrigée a montré.
        ///
        /// LA SOURCE EST « Controle » ET PAS « Conversation » : ce n'est pas
        /// une impression tirée d'un échange, c'est un résultat écrit par un
        /// professeur sur une copie. Le distinguer permettra de savoir, plus
        /// tard, d'où vient une mesure.
        ///
        /// Séparé de l'enregistrement : un échec ici ne doit pas faire perdre
        /// la note et le ressenti, qui sont déjà en base.
        /// </summary>
        private async Task PorterAuSuiviAsync(
            int eleveId, ResultatEnregistre resultat, CancellationToken ct)
        {
            var observations = resultat.CodesReussis
                .Select(code => new ObservationCompetence(code, ResultatObservation.Reussi))
                .Concat(resultat.CodesRates
                    .Select(code => new ObservationCompetence(code, ResultatObservation.Echoue)))
                .ToList();

            if (observations.Count == 0) return;

            try
            {
                await _maitrises.AppliquerObservationsAsync(eleveId, observations, "Controle", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du report des resultats de controle au suivi (eleve {EleveId}).", eleveId);
            }
        }

        /// <summary>
        /// PORTE LA COPIE NOTÉE AU MOTEUR DE MAÎTRISE — une observation par
        /// question, et la source dit d'où elle vient.
        ///
        /// LE DÉFAUT CORRIGÉ ICI, relevé par Camara le 13/09/2026 : une
        /// évaluation à 17,5/20 sur « Utiliser la réciproque de Thalès » a
        /// laissé la fiche à 59 %, « en cours d'acquisition ». Rejoué depuis la
        /// base, le compte était sans appel : la séance du contrôle avait bien
        /// produit une observation, et c'était un « hésitant » — le score était
        /// PASSÉ de 0,648 à 0,595. La meilleure note de son année lui avait
        /// fait perdre de la maîtrise.
        ///
        /// La copie n'atteignait le suivi par AUCUN chemin. Seul l'observateur
        /// écrit dans la maîtrise, et le bloc [EVALUATION] est retiré de son
        /// contexte : il jugeait la conversation, où l'élève avait hésité une
        /// fois avant de se reprendre. L'impression écrasait la mesure.
        ///
        /// La copie est pourtant le SEUL moment où l'on mesure vraiment :
        /// l'élève répond seul, sans guidage, sur une notion nommée, et chaque
        /// réponse reçoit un verdict. Et depuis que le professeur ne commente
        /// plus rien pendant un contrôle (voir MarqueurEvaluationEnCours), la
        /// conversation d'une évaluation ne porte plus aucun signal : juger
        /// dessus reviendrait à juger sur du silence.
        ///
        /// UNE QUESTION = UNE OBSERVATION, et le moteur bayésien n'est pas
        /// touché. Quatre questions valent quatre observations parce que ce
        /// sont quatre réponses réelles, pas parce qu'on aurait décidé de peser
        /// plus lourd. Source « Evaluation » : une mesure doit pouvoir se
        /// distinguer d'une impression quand on relit une fiche.
        ///
        /// Séparé de l'enregistrement, et silencieux : un échec ici ne doit pas
        /// faire perdre la note et la copie, qui sont déjà en base. L'élève a
        /// entendu sa note — c'est la trace qui manquerait, pas la séance.
        /// </summary>
        private async Task PorterLaCopieAuSuiviAsync(
            int eleveId, int matiereId, int? niveauScolaireId,
            EvaluationDeclaree declaree, CancellationToken ct)
        {
            try
            {
                if (niveauScolaireId is not int niveau) return;
                if (string.IsNullOrWhiteSpace(declaree.Notion)) return;

                var referentiel = (await _maitrises.GetNotionsDuNiveauAsync(matiereId, niveau, ct))
                    .ToList();

                // MÊME RAPPROCHEMENT QUE PARTOUT AILLEURS, ET SUR UNE LISTE
                // FERMÉE. Le modèle écrit un libellé, jamais un identifiant :
                // s'il ne tombe pas exactement sur une notion du référentiel,
                // on ne devine pas. La note et la copie restent ; c'est
                // seulement la maîtrise qui ne bouge pas, faute de savoir de
                // quoi elle parlerait.
                var connue = referentiel.FirstOrDefault(
                    c => c.Libelle is not null
                        && LecteurBloc.Normaliser(c.Libelle)
                            == LecteurBloc.Normaliser(declaree.Notion));

                if (connue?.Code is null)
                {
                    // AVERTISSEMENT ET NON INFORMATION : c'est une note qui
                    // n'atteindra jamais le suivi. La copie sera enregistrée,
                    // la note s'affichera, la barre de préparation ne bougera
                    // pas — et rien, à l'écran, ne dira pourquoi. C'est
                    // exactement le silence qui a fait chercher un défaut
                    // d'affichage pendant une heure le 13/09/2026. Un libellé
                    // qui rate sa notion doit se voir dans le journal.
                    _logger.LogWarning(
                        "Copie NON portee au suivi : la notion «{Notion}» ne correspond a "
                        + "aucune competence du referentiel (eleve {EleveId}, matiere {MatiereId}, "
                        + "note {Note}/20). La barre de preparation ne bougera pas.",
                        declaree.Notion, eleveId, matiereId, declaree.Note);
                    return;
                }

                var observations = ObservationsDeLEvaluation.Pour(connue.Code, declaree.Questions);
                if (observations.Count == 0) return;

                var appliquees = await _maitrises.AppliquerObservationsAsync(
                    eleveId, observations, "Evaluation", ct);

                _logger.LogInformation(
                    "Copie portee au suivi : {Nombre} observation(s) sur «{Notion}» "
                    + "pour l'eleve {EleveId}.",
                    appliquees, connue.Libelle, eleveId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec du report de la copie au suivi (eleve {EleveId}).", eleveId);
            }
        }

        private static IEnumerable<NotionDeclaree> Rapprocher(
            IReadOnlyList<string> libelles, List<CompetenceCandidate> referentiel) =>
            libelles.Select(libelle =>
            {
                var connue = referentiel.FirstOrDefault(
                    c => c.Libelle is not null
                        && LecteurBloc.Normaliser(c.Libelle) == LecteurBloc.Normaliser(libelle));

                return new NotionDeclaree(connue?.Id, connue?.Libelle ?? libelle);
            });

        /// <summary>
        /// Déduit le programme d'un contrôle depuis son sujet. Silencieux :
        /// un contrôle sans programme reste utilisable, et le professeur le
        /// complétera à la première préparation.
        /// </summary>
        private async Task DeduireProgrammeAsync(
            int eleveId, int controleId, int matiereId, int? niveauScolaireId,
            string? sujet, CancellationToken ct)
        {
            try
            {
                var posees = await _planificateur.PoserProgrammeAsync(
                    eleveId, controleId, matiereId, niveauScolaireId, sujet, ct);

                if (posees > 0)
                {
                    _logger.LogInformation(
                        "{Nombre} notion(s) deduite(s) du sujet pour le controle {ControleId}.",
                        posees, controleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la deduction du programme du controle {ControleId}.", controleId);
            }
        }

        /// <summary>
        /// Rapproche des libellés déclarés du référentiel, puis les pose au
        /// programme du contrôle.
        ///
        /// UNE NOTION HORS RÉFÉRENTIEL N'EST PAS REJETÉE : six niveaux n'ont
        /// aucune compétence semée (voies pro, techno, 3e prépa-métiers). Elle
        /// est gardée avec son libellé, comme le fait déjà une fiche de
        /// révision — c'est ce qui permet à ces élèves d'avoir une préparation
        /// tout de même.
        /// </summary>
        private async Task<int> AjouterNotionsAsync(
            int eleveId, int controleId, int matiereId, int? niveauScolaireId,
            IReadOnlyList<string> libelles, bool travaillee, CancellationToken ct)
        {
            var referentiel = niveauScolaireId is int niveau
                ? (await _maitrises.GetNotionsDuNiveauAsync(matiereId, niveau, ct)).ToList()
                : [];

            return await _controles.AjouterNotionsAsync(
                eleveId, controleId, matiereId,
                Rapprocher(libelles, referentiel), travaillee, ct);
        }

        /// <summary>
        /// Les matières réellement au programme de cette classe — la liste
        /// fermée contre laquelle un nom de matière écrit par le modèle est
        /// rapproché. Vide si la classe est inconnue : on ne devine pas.
        /// </summary>
        private async Task<IEnumerable<MatiereReconnaissable>> MatieresDeLEleveAsync(
            int? niveauScolaireId, bool lv2Espagnol, IReadOnlyCollection<string> specialites)
        {
            if (niveauScolaireId is not int niveauId) return [];

            var niveaux = await _referentielService.GetNiveauxScolairesAsync();
            var niveau = niveaux.FirstOrDefault(n => n.Id == niveauId);
            if (niveau is null) return [];

            var matieres = await _referentielService.GetMatieresAsync(activesSeulement: true);

            return matieres
                .Where(m => VoiesScolaires.EstAuProgramme(m, niveau, lv2Espagnol, specialites))
                .Select(m => new MatiereReconnaissable(m.Id, m.Code, m.Libelle))
                .ToList();
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
            int conversationId, int eleveId, int? niveauScolaireId,
            string texte, CancellationToken ct)
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

                await PorterLaCopieAuSuiviAsync(
                    eleveId, enregistree.MatiereId, niveauScolaireId, declaree, ct);

                // LA NOTE EST DONNÉE, LA CORRECTION ATTEND LE PROCHAIN COURS.
                //
                // Voulu par Camara le 13/09/2026 : le professeur juge du temps
                // qui reste. La copie écrite est déjà enregistrée ci-dessus,
                // corrections comprises — seul le moment de les reprendre avec
                // l'enfant est remis. Silencieux : rater ce marquage ne doit
                // pas faire perdre la note ni le report au suivi.
                if (declaree.CorrectionReportee)
                {
                    try
                    {
                        await _evaluations.ReporterCorrectionAsync(eleveId, enregistree.Id, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Echec du report de correction de l'evaluation {EvaluationId}.",
                            enregistree.Id);
                    }
                }

                // LE CONTRÔLE A VRAIMENT EU LIEU : toute évaluation prévue
                // encore ouverte pour cette matière n'a plus lieu d'être —
                // c'est elle-même qui vient de se réaliser.
                await _evaluationsPrevues.MarquerConsommeesAsync(eleveId, enregistree.MatiereId, ct);
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
