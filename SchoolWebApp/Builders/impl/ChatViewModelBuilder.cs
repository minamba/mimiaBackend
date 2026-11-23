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
        private readonly IExamenBlancRepository? _examensBlancs;
        private readonly IAbonnementRepository _abonnements;
        private readonly Services.Paiement.AccesForfaitService _acces;
        private readonly IFileAlertesQuota _fileAlertes;
        private readonly Services.Jeux.IJeuxService _jeux;
        private readonly ILogger<ChatViewModelBuilder> _logger;

        /// <summary>Pour le projet d'orientation noté par la conseillère (11/10/2026).</summary>
        private readonly IEleveRepository _eleves;

        /// <summary>Les rapports d'orientation de la conseillère (11/10/2026).</summary>
        private readonly IRapportOrientationRepository _rapportsOrientation;

        /// <summary>Les syllabes du CP vérifiées sur la voix (04/11/2026).</summary>
        private readonly Services.Voix.IVerificationSonService _verificationSon;

        /// <summary>
        /// Le texte brut du professeur (lecture des nombres) peut aller au journal Debug pour un diagnostic — JAMAIS en
        /// production, et seulement si `Diagnostic:LectureDesNombresTexteBrut` vaut true (false par défaut). Même alors, le
        /// prénom de l'enfant est masqué (refus d'Omar, 3bc3b0b : seul le niveau de log le coupait).
        /// </summary>
        private readonly bool _texteBrutAuJournal;

        /// <summary>La clé de configuration qui ouvre le journal du texte brut (hors production).</summary>
        public const string CleTexteBrutAuJournal = "Diagnostic:LectureDesNombresTexteBrut";

        /// <summary>Le journal du texte brut est-il permis ? Faux en production, quoi que dise la configuration.</summary>
        public static bool TexteBrutAuJournalPermis(IConfiguration? configuration, IHostEnvironment? environnement) =>
            environnement is not null && !environnement.IsProduction()
            && configuration is not null && configuration.GetValue(CleTexteBrutAuJournal, false);

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
            ILogger<ChatViewModelBuilder> logger,
            IEleveRepository eleves,
            IRapportOrientationRepository rapportsOrientation,
            Services.Voix.IVerificationSonService verificationSon,
            Services.Paiement.AccesForfaitService acces,
            IConfiguration configuration,
            IHostEnvironment environnement,
            IExamenBlancRepository? examensBlancs = null)
        {
            _examensBlancs = examensBlancs;
            _texteBrutAuJournal = TexteBrutAuJournalPermis(configuration, environnement);
            _acces = acces ?? throw new ArgumentNullException(nameof(acces));
            _verificationSon = verificationSon ?? throw new ArgumentNullException(nameof(verificationSon));
            _eleves = eleves ?? throw new ArgumentNullException(nameof(eleves));
            _rapportsOrientation = rapportsOrientation ?? throw new ArgumentNullException(nameof(rapportsOrientation));
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

        public async Task<string?> LangueDuSonAsync(int conversationId)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);

            return contexte is null ? null : Services.Voix.RegleVerificationSon.LangueDe(
                contexte.Eleve.NiveauCycle, contexte.Conversation.MatiereCode);
        }

        public async Task<bool> EnExerciceDeSonsAsync(int conversationId)
        {
            var messages = await FilDeLExerciceAsync(conversationId);
            return Services.Voix.RegleVerificationSon.EnExerciceDeSons(messages.Select(m => (m.Contenu, m.SonJuste)));
        }

        public async Task<bool> EnEcouteDeMotsAsync(int conversationId)
        {
            var messages = await FilDeLExerciceAsync(conversationId);
            return Services.Voix.RegleVerificationSon.ContexteDEcoute(messages.Select(m => m.Contenu));
        }

        public async Task<string?> TypeDEcouteAsync(int conversationId)
        {
            var messages = await FilDeLExerciceAsync(conversationId);
            return Services.Voix.RegleVerificationSon.TypeDEcoute(messages.Select(m => m.Contenu));
        }

        /// <summary>
        /// Les derniers messages qui comptent pour l'exercice de sons en cours, côté micro : rien d'avant la dernière
        /// demande de changement d'exercice de l'enfant (14/11/2026, voir ChangementDExercice). Sans elle, sa parole
        /// suivante était encore jugée comme une syllabe de l'exercice qu'il venait de quitter.
        /// </summary>
        private async Task<IReadOnlyList<(string Role, string? Contenu, bool? SonJuste)>> FilDeLExerciceAsync(int conversationId)
        {
            var messages = await _conversationService.GetMessagesAsync(conversationId, MessagesPourExerciceDeSons);
            return ChangementDExercice.Fil(messages.Select(m => ((string)m.Role, (string?)m.Contenu, m.SonJuste)).ToList());
        }

        /// <summary>Assez pour retrouver « le son oin » dit au début de l'exercice.</summary>
        private const int MessagesPourExerciceDeSons = 12;

        public async Task<string?> DerniereParoleDuProfesseurAsync(int conversationId)
        {
            // LE RAPPORT N'EST PAS UNE PAROLE — Camara, le 04/11/2026 : il quitte, revient, Adrien
            // demande « main », et le rapport de la sortie arrive juste APRÈS cette demande. Lu
            // comme la dernière parole, il ne demande rien : « Main. » partait en clair.
            var messages = await _conversationService.GetMessagesAsync(conversationId, 8);
            return messages.LastOrDefault(m => m.Role == "assistant"
                && !Services.Voix.RegleVerificationSon.EstUneCloture(m.Contenu))?.Contenu;
        }

        /// <summary>
        /// UNE LECTURE EN IMAGES TAPÉE AU CLAVIER, JUGÉE COMME AU MICRO (signalement #49) : le texte gardé (« J'ai lu la syllabe
        /// son. ») et son verdict, ou null quand ce n'est pas une lecture à juger — pas d'image au tableau, un contrôle noté,
        /// une demande (changer d'exercice, au revoir), une phrase, un message marqué par l'écran, ou une panne.
        /// </summary>
        private async Task<(bool? SonJuste, string Contenu)?> LectureTapeeAsync(
            int conversationId, string? langue, IReadOnlyList<(string? Role, string? Contenu)> historique, string? contenu, CancellationToken ct)
        {
            if (langue is null || string.IsNullOrWhiteSpace(contenu)) return null;

            var aLire = Services.Voix.LectureDImage.ATaperA(historique, contenu);
            if (aLire is null) return null;

            try
            {
                var verdict = await _verificationSon.VerifierLectureAsync(conversationId, aLire, contenu, ct, langue, auClavier: true);
                if (verdict is null) return null;

                var texte = Services.Voix.LectureDImage.TexteDeLEleve(aLire, verdict);
                bool? juste = verdict.SansJugement ? null : verdict.Juste;
                _logger.LogInformation(
                    "Lecture tapee jugee (conversation {ConversationId}) : {Genre} {Cible}, ecrit {Ecrit}, juste {Juste}.",
                    conversationId, aLire.Genre, aLire.Cible, verdict.Entendu, juste);
                return (juste, texte);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Lecture tapee non jugee (conversation {ConversationId}).", conversationId);
                return null;
            }
        }

        /// <summary>
        /// L'ÉCOUTE DES MOTS TAPÉE AU CLAVIER, JUGÉE COMME AU MICRO — contrôle d'Omar sur le #57 v2, accord de Min's (21/11/2026) :
        /// voir RegleVerificationSon.EstUneEcouteTapee. Mêmes conditions et même service que le relais du micro
        /// (EcouteController.AvecLaVoixAsync) : le dernier message du professeur dicte un mot à écouter, l'exercice de sons est
        /// en cours, et la réponse est une tentative. Rend le texte gardé (« J'ai entendu le mot two. ») et son verdict, ou null
        /// — pas une écoute, une demande, une image à lire (c'est la lecture tapée, #49), une panne.
        /// </summary>
        private async Task<(bool SonJuste, string Contenu)?> EcouteTapeeAsync(
            int conversationId, string? langue, IReadOnlyList<string?> messagesDuProfesseur, string? contenu, CancellationToken ct)
        {
            if (langue is null || !Services.Voix.RegleVerificationSon.EstUneEcouteTapee(contenu)) return null;
            if (LecteurEvaluation.EstOuvert(messagesDuProfesseur)) return null;

            try
            {
                var parole = await DerniereParoleDuProfesseurAsync(conversationId);
                if (Services.Voix.LectureDImage.Affiche(parole) is not null) return null;
                if (!Services.Voix.RegleVerificationSon.EstEcouteDeMot(parole, await EnEcouteDeMotsAsync(conversationId))) return null;
                if (!await EnExerciceDeSonsAsync(conversationId)) return null;

                var typeDEcoute = await TypeDEcouteAsync(conversationId);
                var verdict = await _verificationSon.VerifierEcouteAsync(conversationId, parole!, contenu, ct, langue, typeDEcoute);
                if (verdict is null) return null;

                _logger.LogInformation(
                    "Ecoute tapee jugee (conversation {ConversationId}) : attendu {Attendu}, ecrit {Ecrit}, juste {Juste}.",
                    conversationId, verdict.Attendu, verdict.Entendu, verdict.Juste);
                return (verdict.Juste, Services.Voix.RegleVerificationSon.TexteDEcoute(verdict, typeDEcoute, parole, contenu));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Ecoute tapee non jugee (conversation {ConversationId}).", conversationId);
                return null;
            }
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

            var vue = _mapper.Map<ConversationViewModel>(conversation);
            vue.DicteeAuChoix = TypeDeDictee.AuChoix(eleve);
            return vue;
        }

        public async Task<IEnumerable<ConversationViewModel>?> GetConversationsAsync(int eleveId)
        {
            var eleve = await _resolver.ResoudreEleveAsync(eleveId);
            if (eleve is null) return null;

            var conversations = await _conversationService.GetConversationsByEleveAsync(eleve.Id);
            var vues = _mapper.Map<List<ConversationViewModel>>(conversations);

            var auChoix = TypeDeDictee.AuChoix(eleve);
            foreach (var vue in vues) vue.DicteeAuChoix = auChoix;

            return vues;
        }

        public async Task<IEnumerable<MessageViewModel>?> GetMessagesAsync(int conversationId)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);
            if (contexte is null) return null;

            var messages = (await _conversationService.GetMessagesAsync(
                conversationId, AgentPedagogiqueService.PlancherHistorique)).ToList();

            var vues = _mapper.Map<IEnumerable<MessageViewModel>>(messages).ToList();

            // UN APARTÉ DE RÉGIE DÉJÀ ENREGISTRÉ (avant le filtre du flux) ne se réaffiche pas quand l'écran se recharge :
            // même filtre que le flux, à la lecture seulement — la base n'est pas modifiée. Voir ApartesInternes.
            foreach (var vue in vues.Where(v => v.Role == "assistant" && v.Contenu is not null))
            {
                vue.Contenu = ApartesInternes.Retirer(vue.Contenu);
            }

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
            int? secondesRestantes, string? vitesseEcoute, bool? sonJuste, CancellationToken ct,
            IReadOnlyList<string>? figureIncomprise = null, VerdictDuTour? verdict = null) =>
            StreamTourAsync(conversationId, contenu, accueil: false, ct,
                secondesRestantes: secondesRestantes, pieceJointeIds: pieceJointeIds,
                vitesseEcoute: vitesseEcoute, sonJuste: sonJuste, figureIncomprise: figureIncomprise, verdictDuTour: verdict);

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

        public async Task PoserPauseAsync(int conversationId)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId)
                ?? throw new UnauthorizedAccessException(
                    "Conversation inexistante ou n'appartenant pas au compte authentifié.");

            await _conversationService.PoserPauseAsync(conversationId, contexte.Eleve.Id);
        }

        public async Task LeverPauseAsync(int conversationId)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId)
                ?? throw new UnauthorizedAccessException(
                    "Conversation inexistante ou n'appartenant pas au compte authentifié.");

            await _conversationService.LeverPauseAsync(conversationId, contexte.Eleve.Id);
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
        private async Task<(string Rappel, int Id)?> DicteeEnAttenteAsync(int eleveId, int matiereId, bool francais, CancellationToken ct)
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

                // LES NUMÉROS DU TABLEAU, DÈS L'ACCUEIL — Camara, le 04/10/2026 :
                // « l'erreur numéro 1 que le professeur m'indique est en fait la
                // numéro 2 ». Au tour d'accueil il recevait le texte et la copie,
                // jamais la liste numérotée — elle n'arrivait qu'au tour suivant,
                // une fois la dictée au tableau. Il numérotait donc de tête, et
                // repartait de ce qu'il avait écrit la fois d'avant (« l'erreur 1
                // porte sur les volets »), qui était faux. Voir `EcartsDictee`.
                var ecarts = EcartsDictee.Marqueur(enAttente.TexteDicte, enAttente.Copie);

                var numeros = ecarts is null
                    ? string.Empty
                    : "\n" + ecarts + "\n[Ces numéros font foi, même contre ce que tu as pu écrire "
                        + "à une séance précédente : si tu avais appelé « erreur 1 » un autre écart, "
                        + "c'était une erreur de ta part. Reprends au n°1, sauf si l'historique montre "
                        + "que l'élève en a déjà corrigé — alors au premier numéro qui reste.]";

                // L'APPLICATION LA REMET AU TABLEAU ELLE-MÊME — Camara, le
                // 04/10/2026 : « vu que le prof a dit qu'on reprenait la
                // correction, il doit la réafficher ». On demandait au professeur
                // d'écrire le repère ; il a annoncé « on reprend ta dictée » et
                // posé sa question devant un tableau vide. Une garantie d'usage
                // ne se demande pas : voir `tableauRepris`, qui se sert de l'Id.
                var rappel = $"[Une dictée attend sa correction depuis le {enAttente.DateCreation:dd/MM} "
                    + $"(n° {enAttente.Id}) : tu COMMENCES la séance par elle, avant toute autre "
                    + "chose — voir « UNE DICTÉE EN ATTENTE SE CORRIGE D'ABORD ». L'application la "
                    + "remet ELLE-MÊME au tableau à l'instant, avec ses numéros : n'écris pas de "
                    + "repère [DICTEE_AU_TABLEAU], ne dis pas « je te la remets ». "
                    + "S'il ne veut pas la corriger — ou s'il a DÉJÀ demandé plus haut à "
                    + "l'abandonner : dans ce cas ne la lui repropose pas —, écris "
                    + $"[DICTEE_SUPPRIMEE]{enAttente.Id}[/DICTEE_SUPPRIMEE] et passe à autre chose.\n"
                    + "Texte dicté :\n" + enAttente.TexteDicte + "\n"
                    + "Sa copie :\n" + enAttente.Copie + "]" + numeros;

                // LES LETTRES MUETTES DÈS L'ACCUEIL — retest KO #58 d'Adam, le 22/11/2026 : le fait des lettres muettes
                // partait au tour de la copie et pendant la correction ouverte, jamais à la REPRISE d'une dictée en
                // attente. Le professeur a donc rouvert la séance par « Il manque juste un petit son à la fin. Écoute
                // bien : t. Tu entends ce dernier son ? » pour « cha » au lieu de « chat ».
                var muettes = francais ? LettresMuettes.Fait(enAttente.TexteDicte, enAttente.Copie) : null;
                if (muettes is not null) rappel += "\n" + muettes;

                // ET LES MOTS EN CLAIR — signalement #61 : voir VoixEnClair.
                rappel += "\n" + Services.Voix.VoixEnClair.Consigne;

                return (rappel, enAttente.Id);
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
        /// Le fait « cette séance ne continue pas la précédente », avec le
        /// titre du contrôle d'avant quand il y en avait un (encore à cet
        /// élève) — voir SujetDeSeance. Null si la séance porte sur la même
        /// chose. Silencieux : sans titre, le fait part quand même.
        /// </summary>
        private async Task<string?> FaitAutreSujetAsync(
            int eleveId, SujetDeSeance.Sujet avant, SujetDeSeance.Sujet apres, CancellationToken ct)
        {
            if (!SujetDeSeance.Change(avant, apres)) return null;

            string? titreAvant = null;

            if (avant.ControleId is int controleAvant)
            {
                try
                {
                    titreAvant = (await _controles.GetAsync(eleveId, controleAvant, ct))?.Sujet;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex,
                        "Titre du controle precedent {ControleId} illisible (eleve {EleveId}).", controleAvant, eleveId);
                }
            }

            var fait = SujetDeSeance.Fait(avant, apres, titreAvant);

            if (fait is not null)
            {
                _logger.LogInformation(
                    "Seance d'un autre sujet que la precedente ({ModeAvant} {CibleAvant} -> {ModeApres} {CibleApres}).",
                    avant.Mode, (object?)avant.ControleId ?? avant.EpreuveCode,
                    apres.Mode, (object?)apres.ControleId ?? apres.EpreuveCode);
            }

            return fait;
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

                // SON COURS EST-IL LÀ ? Il décide de la première question quand
                // le programme est inconnu (Camara, 21/11/2026) : sans cours, on
                // demande ; avec, on le lit. Lu seulement dans ce cas-là.
                var nbCours = 0;
                if (preparation is { PerimetreConnu: false })
                {
                    nbCours = (await _controles.GetDocumentsAsync(eleveId, prochain.Id, ct))
                        .Count(d => d.Exploitable);
                }

                // LE CONTRÔLE CACHÉ D'UN EXAMEN BLANC (23/11/2026) : ses documents sont du côté de l'examen blanc,
                // rangés en dossiers ; on ne demande pas à l'élève sur quoi il porte.
                var etatExamenBlanc = _examensBlancs is null ? null : await _examensBlancs.GetEtatParControleAsync(prochain.Id, ct);

                return MarqueurControle(prochain, joursRestants, preparation, maintenantParis, nbCours, etatExamenBlanc);
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
                    + "`pas_pret`), une ligne `notions:` avec l'état (`tenue`, `en cours`, `fragile`) de chaque notion "
                    + "que la séance t'a permis de juger, et une `observation` qui dit, domaine par domaine, ce qui est "
                    + "tenu, ce qui pèche et ce qui n'a pas encore été travaillé.]";
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
                    + "DOMAINE PAR DOMAINE — ce qui est tenu, ce qui pèche, ce qui n'a pas encore été vu — et une "
                    + "ligne `notions:` avec l'état (`tenue`, `en cours`, `fragile`) de chaque notion que la séance "
                    + "t'a permis de juger, au mot près du programme ci-dessus. "
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
            int eleveId, int matiereId, string? contenu, IReadOnlyList<int> piecesAttachees, CancellationToken ct)
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
                //
                // L'ÉNONCÉ ET LA COPIE PARTENT ENSEMBLE depuis le 26/09/2026 : la
                // carte les garde jusqu'à ce que tout soit importé, puis un seul
                // message les porte, marqueurs et documents dans le même ordre.
                var recues = LecteurCopieControle.LirePieces(contenu);

                for (var i = 0; i < recues.Count && i < piecesAttachees.Count; i++)
                {
                    await _controles.RecevoirPieceCopieAsync(
                        eleveId, recues[i].Controle, matiereId, recues[i].EstEnonce,
                        piecesAttachees[i], maintenant, ct);
                }

                // « Je t'aide à te corriger » / « Je corrige et t'explique » —
                // noté AVANT la réponse : c'est ce choix qui décide, dès ce
                // tour-ci, si le professeur a le droit de donner une réponse.
                if (LecteurCopieControle.LireStyle(contenu) is { } style)
                {
                    await _controles.PoserStyleCorrectionAsync(
                        eleveId, style.Controle, matiereId, style.Style, maintenant, ct);
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
        private async Task<string?> CopieControleAsync(
            int eleveId, int matiereId, IReadOnlyList<(string Role, string Contenu)> fil, string? contenu,
            CancellationToken ct)
        {
            try
            {
                var copie = await _controles.GetCopieEnCoursAsync(
                    eleveId, matiereId, DateTime.UtcNow - FenetreCopie, ct);

                return copie is null
                    ? null
                    : MarqueurCopieControle(copie, LecteurCopieControle.EtatCorrection(fil, contenu, copie.Id));
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
        /// <param name="nbCours">
        /// LE COURS DE L'ÉLÈVE, DIT EN FAIT À CHAQUE TOUR (26/10/2026). Un parent
        /// a vu le professeur demander « as-tu des cours à me transmettre ? » à
        /// une enfant qui les avait importés, puis la faire travailler hors de
        /// sa leçon. Le cours est bien dans le contexte, avec sa consigne — mais
        /// une consigne lue une fois ne tient pas ; le fait, répété avec le tour,
        /// tient. Nul hors d'une préparation.
        /// </param>
        /// <param name="nbIllisibles">
        /// Les documents dont la lecture a échoué (mention à la place du texte) :
        /// ceux-là, le professeur doit les faire réimporter — le dire ici évite
        /// que « ne lui redemande pas » contredise la mention du préfixe.
        /// </param>
        /// <param name="programmeInconnu">
        /// LE PROGRAMME DU CONTRÔLE N'EST PAS ENCORE CONNU (aucune notion) —
        /// décision de Camara, le 21/11/2026. La consigne d'arrivée ne voyage
        /// qu'avec le premier tour ; c'est au tour SUIVANT que l'enfant répond
        /// « c'est sur les additions avec retenue ». Le fait revient donc à
        /// chaque tour tant que le programme manque : ne pas préparer avant
        /// de savoir, et écrire le bloc qui complète le titre dès qu'on sait.
        /// </param>
        /// <summary>
        /// LE RAPPEL DE CHAQUE TOUR, POUR UN EXAMEN BLANC — Camara, le 23/11/2026. La séance prépare le contrôle caché
        /// d'une matière d'examen blanc : ses documents sont rangés en dossiers ; tant que le programme n'est pas validé,
        /// le professeur le propose au tableau et le valide par [CONTROLE_NOTIONS] ; ensuite, révisions et verdict comme
        /// pour un contrôle.
        /// </summary>
        private static string MarqueurExamenBlanc(int controleId, int dossiers, bool programmeValide) =>
            $"[SÉANCE DE PRÉPARATION DE SON EXAMEN BLANC (contrôle n° {controleId}) : l'élève est entré par le bouton de "
            + "son examen blanc, c'est de lui qu'on parle (tu peux l'appeler brevet blanc ou bac blanc). "
            + (dossiers > 0
                ? "SES DOCUMENTS SONT LUS ET RANGÉS EN DOSSIERS (section « Ce contrôle est son … ») : NE LUI DEMANDE PAS "
                  + "sur quoi il porte ni ses cours. "
                : "Ses documents ne sont pas encore rangés : demande-lui sur quoi porte son examen blanc. ")
            + (programmeValide
                ? "SON PROGRAMME EST VALIDÉ : révise-le notion par notion en t'appuyant sur ses documents. Les notions "
                  + $"travaillées vont dans [CONTROLE_NOTIONS] controle: {controleId} ; ta conclusion porte "
                  + $"[CONTROLE_PRET] controle: {controleId}. "
                  + RetraitDeNotion(controleId)
                : "SON PROGRAMME N'EST PAS ENCORE VALIDÉ : propose-le AU TABLEAU ([ARDOISE], la liste des dossiers), "
                  + "demande-lui s'il est d'accord, s'il veut retirer ou ajouter une notion, et attends sa réponse. NE "
                  + "COMMENCE AUCUNE RÉVISION avant son accord. Dès qu'il est d'accord, écris dans ce message "
                  + $"[CONTROLE_NOTIONS] controle: {controleId} avec les notions retenues recopiées au mot près — c'est ce "
                  + "qui valide son programme —, dis-lui que son programme est prêt, puis commence. S'IL VEUT EN RETIRER "
                  + "UNE : ne la retire pas toi-même et ne l'enlève pas de `notions:` ; pose-lui la question fermée, seule "
                  + "dans sa phrase, avec le libellé exact du dossier : « Je retire toute la notion « <libellé exact> » de "
                  + "ton programme ? », et attends son oui (c'est le serveur qui la retire, et il te le dit). "
                  + NotionEntiere)
            + "]";

        /// <summary>
        /// LE RETRAIT D'UNE NOTION, RAPPELÉ À CHAQUE TOUR D'UNE PRÉPARATION — signalement #67 (23/11/2026). Le
        /// professeur répondait « je retire complètement les titrages » sans rien écrire qui les retire : le bloc ne savait
        /// qu'ajouter. La consigne vit dans le noyau (« UNE NOTION QUI N'EST PAS (OU PLUS) AU PROGRAMME SE RETIRE »), mais
        /// la demande arrive au milieu d'une séance, à quinze cents lignes de là : elle voyage avec chaque tour.
        /// </summary>
        private static string RetraitDeNotion(int? controleId) =>
            "S'IL TE DIT QU'UNE NOTION NE SERA PAS (OU PLUS) À SON CONTRÔLE, tu ne la retires pas toi-même : pose-lui la "
            + "question fermée, avec le libellé EXACT de la notion tel qu'il est écrit dans son programme, seule dans sa "
            + "phrase : « Je retire toute la notion « <libellé exact> » de ton programme ? » (une question par notion, "
            + "toutes celles du thème s'il retire un thème entier), et attends sa réponse. S'il répond oui au message "
            + "suivant, c'est le serveur qui la retire, et il te le dit. N'écris JAMAIS `retirer:` : il ne retire rien. "
            + "Ne dis jamais qu'une notion est retirée tant que le serveur ne te l'a pas dit. " + NotionEntiere;

        /// <summary>
        /// UNE NOTION SE GARDE OU SE RETIRE ENTIÈRE — verdict d'Omar sur le #67 (v2), sur la question fermée en v5.
        /// « Enlève les espèces amphotères » ne visait qu'un morceau de « Identifier un couple acide-base de Brønsted,
        /// une espèce amphotère, et écrire la réaction associée » : le serveur ne coupe pas une notion en deux, et ne
        /// retire qu'une notion ENTIÈRE, sur le oui de l'élève à la question fermée.
        /// </summary>
        private const string NotionEntiere =
            "UNE NOTION SE GARDE OU SE RETIRE ENTIÈRE : s'il ne refuse qu'un MORCEAU d'une notion de son programme (« les "
            + "amphotères » dans « Identifier un couple acide-base de Brønsted, une espèce amphotère… »), ou s'il en garde "
            + "une partie (« retire ceci mais garde cela »), la notion RESTE. Dis-lui de quelle notion ce morceau fait "
            + "partie, qu'une notion se garde ou se retire en entier, et que tu la gardes ; s'il veut quand même la retirer "
            + "entière, pose-lui la question fermée sur la notion entière. Avant de lui dire ce qu'il reste dans son "
            + "programme, relis la liste de son programme : c'est elle qui fait foi.";

        private static string MarqueurModeSeance(string mode, int? controleId, string? epreuveCode, int? nbCours = null, int nbIllisibles = 0, bool programmeInconnu = false) => mode switch
        {
            ModesSeance.Controle =>
                $"[SÉANCE DE PRÉPARATION DU CONTRÔLE n° {controleId} : l'élève est entré par le bouton de "
                + "ce contrôle, c'est de lui qu'on parle. NE PARLE JAMAIS d'un examen (brevet, bac) ni d'un "
                + $"autre contrôle. Les notions travaillées de ce contrôle vont dans [CONTROLE_NOTIONS] "
                + $"controle: {controleId} ; ta conclusion porte [CONTROLE_PRET] controle: {controleId}. "
                + (nbCours > 0
                    ? $"SON COURS EST DÉJÀ IMPORTÉ ({nbCours} document{(nbCours > 1 ? "s" : "")}, dans ta "
                      + "section « Le cours de l'élève pour ce contrôle ») : NE LUI REDEMANDE PAS sa leçon, "
                      + "ne lui demande pas s'il a des cours à t'envoyer. PRÉPARE-LE D'ABORD SUR CE QUE CE "
                      + "COURS CONTIENT, dans son ordre et avec ses mots ; une notion du programme absente "
                      + "de sa leçon ne vient qu'après, en le lui disant."
                    : "IL N'A IMPORTÉ AUCUN COURS" + (nbIllisibles > 0 ? " LISIBLE" : "")
                      + " : si tu veux sa leçon, demande-la UNE fois (il peut la "
                      + "photographier depuis « Mes contrôles ») ; sinon prépare-le sur le programme du contexte.")
                + (nbIllisibles > 0
                    ? $" {nbIllisibles} document{(nbIllisibles > 1 ? "s" : "")} importé{(nbIllisibles > 1 ? "s" : "")} "
                      + $"{(nbIllisibles > 1 ? "sont" : "est")} ILLISIBLE{(nbIllisibles > 1 ? "S" : "")} (lecture échouée) : "
                      + "dis-le lui simplement et demande-lui de le réimporter en photo nette ou en PDF."
                    : "")
                + (!programmeInconnu
                    ? ""
                    : nbCours > 0
                        ? " LE PROGRAMME DE CE CONTRÔLE N'EST PAS ENCORE ENREGISTRÉ : ne lui demande pas sur "
                          + "quoi il porte, tire-le de son cours, et écris dans ce message [CONTROLE_NOTIONS] "
                          + $"controle: {controleId} avec un `sujet:` court et précis tiré du cours et ses notions."
                        : " LE PROGRAMME DE CE CONTRÔLE N'EST PAS ENCORE CONNU : tant qu'il ne t'a pas dit sur "
                          + "quoi il porte, NE PRÉPARE RIEN (ni exercice, ni explication) et redemande-le-lui "
                          + "(s'il ne sait pas, qu'il demande à ses parents ou photographie sa leçon depuis "
                          + "« Mes contrôles »). DÈS QU'IL TE L'A DIT, écris dans ce message [CONTROLE_NOTIONS] "
                          + $"controle: {controleId} avec un `sujet:` court et précis (ex. « additions avec "
                          + "retenue » — il complète le titre de son contrôle) et les notions correspondantes, "
                          + "puis commence la préparation.")
                // Un programme connu peut perdre une notion en cours de route (signalement #67).
                + (programmeInconnu ? "" : " " + RetraitDeNotion(controleId))
                + "]",

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

        private static string MarqueurCopieControle(ControleScolaireEleve c, EtatCorrectionCopie correction)
        {
            var entete = $"[COPIE DU CONTRÔLE n° {c.Id} — {c.MatiereLibelle}, "
                + $"{c.Sujet ?? "sujet non précisé"}. ";

            if (c.CopieComplete)
            {
                var recu = c.CopieSeparee == true
                    ? "Tu as TOUT : l'énoncé ET sa copie. "
                    : "Tu as TOUT : sa copie, énoncé et réponses sur la même feuille. ";

                var appui = "Tu t'appuies sur ce que le correcteur a compté juste ou faux, pas sur ton "
                    + "impression ; une mauvaise note se constate, elle ne se reproche pas. ";

                if (correction.Termine)
                {
                    return entete + recu + appui
                        + "LA CORRECTION DE CETTE COPIE EST TERMINÉE : ne rouvre aucune erreur, sauf "
                        + "s'il te le demande.]";
                }

                // AUCUNE ERREUR PRÉSENTÉE ENCORE — Camara, le 26/09/2026 : le choix
                // « je t'aide / je corrige » se fait ERREUR PAR ERREUR. On présente
                // la première, et l'écran pose la question pour elle seule.
                if (correction.Erreur is null)
                {
                    return entete + recu + appui
                        + "Accuse réception en une phrase, puis présente SA PREMIÈRE ERREUR — la "
                        + "première question que le correcteur a comptée fausse : laquelle, et ce "
                        + "qu'il a écrit —, SANS dire pourquoi c'est faux ni la bonne réponse. Écris "
                        + "[ERREUR 1/N] à la fin de ton message, où N est le nombre de questions que le "
                        + "correcteur a comptées fausses : l'écran lui demandera alors comment il veut "
                        + "corriger CETTE erreur (« Je t'aide à te corriger » ou « Je corrige et "
                        + "t'explique »). Tu n'expliques rien avant son choix. S'IL N'A AUCUNE ERREUR, "
                        + "félicite-le, puis écris [CORRECTION_TERMINEE] et [CONTROLE_RESULTAT] avec "
                        + $"controle: {c.Id} et la note si elle figure sur la copie."
                        + CommencerToutDeSuite + "]";
                }

                return entete + recu + appui.TrimEnd() + "]"
                    + "\n\n" + MarqueurErreur(c.Id, correction);
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
                + "inutile de lui expliquer comment faire. N'ÉCRIS PAS [COPIE_CONTROLE] : la demande "
                + "est déjà ouverte, et l'écran garde ce qu'il a déjà importé jusqu'à ce que tout "
                + "soit là — l'énoncé et la copie t'arriveront ENSEMBLE, dans un seul message.]";
        }

        /// <summary>
        /// La copie EN COURS ET INCOMPLÈTE dans cette matière, ou null : une
        /// réponse ou une pièce récente, et tout n'est pas encore arrivé.
        /// </summary>
        private async Task<ControleScolaireEleve?> CopieOuverteAsync(
            int eleveId, int matiereId, CancellationToken ct)
        {
            try
            {
                var copie = await _controles.GetCopieEnCoursAsync(
                    eleveId, matiereId, DateTime.UtcNow - FenetreCopie, ct);

                return copie is not null && !copie.CopieComplete ? copie : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la lecture de la copie ouverte de l'eleve {EleveId}.", eleveId);
                return null;
            }
        }

        /// <summary>
        /// COMMENCER TOUT DE SUITE — Camara, le 26/09/2026. Au tour du choix, la
        /// professeure a réfléchi plus d'une minute sans écrire un mot : elle
        /// préparait la correction de TOUTE la copie avant de commencer. L'enfant,
        /// devant une bulle vide, a reparlé — et coupé la réponse. Une erreur à la
        /// fois, c'est aussi une erreur à la fois à PRÉPARER.
        /// </summary>
        private const string CommencerToutDeSuite =
            " COMMENCE TOUT DE SUITE par la première erreur : ne prépare pas la correction de "
            + "toute la copie avant d'écrire — tu regarderas la suivante au moment d'y passer.";

        /// <summary>
        /// L'ERREUR EN COURS, RAPPELÉE À CHAQUE TOUR — Camara, le 26/09/2026 :
        /// « à chaque erreur, la question de l'aide ou de la correction +
        /// explication apparaît, et ainsi de suite jusqu'à la fin ».
        ///
        /// UN FAIT PAR TOUR, PAS UNE RÈGLE DU NOYAU. Le noyau dit déjà « tu ne
        /// donnes jamais la réponse » ; c'est précisément ce qui n'a pas tenu,
        /// dans un sens (la fille d'un parent qui voulait se corriger seule) comme
        /// dans l'autre (Syan, qui a dû réclamer la réponse qu'il avait choisie).
        /// Le choix vaut pour UNE erreur : lu dans l'historique
        /// (<see cref="LecteurCopieControle.EtatCorrection"/>), comme l'écran.
        /// </summary>
        private static string MarqueurErreur(int controleId, EtatCorrectionCopie e)
        {
            var nom = $"ERREUR {e.Erreur}" + (e.Total is int total ? $" SUR {total}" : string.Empty);
            var suivante = (e.Erreur ?? 0) + 1;
            var totalEcrit = e.Total is int t ? $"/{t}" : "/N";

            var entete = $"[CORRECTION DE LA COPIE DU CONTRÔLE n° {controleId} — {nom}";

            // LA SUITE, POUR LES DEUX STYLES : l'erreur réglée, la suivante est
            // présentée SANS explication, et la balise repose la question.
            var suite = " QUAND CETTE ERREUR EST RÉGLÉE — il a trouvé, ou il a compris ta correction —, "
                + "présente L'ERREUR SUIVANTE : laquelle, et ce qu'il a écrit, SANS dire pourquoi c'est "
                + $"faux ni la bonne réponse, et écris [ERREUR {suivante}{totalEcrit}] à la fin de ton "
                + "message : l'écran lui reposera la question pour elle. S'IL N'Y EN A PLUS, écris "
                + $"[CORRECTION_TERMINEE], puis [CONTROLE_RESULTAT] avec controle: {controleId}, la note "
                + "si elle figure sur la copie, et `reussies:` / `ratees:` d'après la copie.";

            if (e.Style is null)
            {
                return entete + " : l'écran lui demande en ce moment comment il veut la corriger "
                    + "(« Je t'aide à te corriger » ou « Je corrige et t'explique »). ATTENDS SON "
                    + "CHOIX : tu n'expliques rien, tu ne donnes ni indice ni réponse. S'il te parle "
                    + "d'autre chose, réponds en une phrase et rappelle-lui de choisir sur l'écran.]";
            }

            if (e.Style == StylesCorrection.Aide)
            {
                return entete + " — IL A CHOISI « JE T'AIDE À TE CORRIGER » POUR CETTE ERREUR. RÈGLE "
                    + "ABSOLUE : TU NE DONNES JAMAIS LA BONNE RÉPONSE. Ni en entier, ni en partie, ni "
                    + "au tableau, ni glissée dans un exemple, une reformulation ou une question fermée "
                    + "qui la contient (« est-ce que c'est 12 ? »), ni en lui proposant deux réponses "
                    + "dont l'une est la bonne, ni en complétant toi-même ce qu'il a commencé. "
                    + "Demande-lui d'abord ce qu'il pense s'être passé ; fais-lui comprendre POURQUOI "
                    + "c'est faux — la règle, la notion, l'étape où ça a déraillé ; puis c'est LUI qui "
                    + "cherche. S'il se trompe encore, un indice de plus, plus précis, qui porte sur la "
                    + "MÉTHODE, jamais sur le résultat. C'est lui qui trouve et qui écrit la bonne "
                    + "réponse ; quand il l'a, tu la valides. "
                    + "S'IL BLOQUE — plusieurs indices sans avancer, ou « je n'y arrive pas » —, "
                    + "rappelle-lui avec gentillesse qu'il peut toucher « Finalement, corrige et "
                    + "explique-moi » sur l'écran pour cette erreur. S'il te demande la réponse, même "
                    + "chose. MAIS TU NE CHANGES JAMAIS DE MANIÈRE DE TOI-MÊME : c'est lui qui décide."
                    + suite + "]";
            }

            // RELEVÉ PAR CAMARA LE 26/09/2026 : en « je corrige et t'explique », le
            // professeur a quand même commencé par « regarde la figure… », et
            // l'enfant a dû insister. La règle générale du noyau (« ne donne
            // jamais la réponse ») est levée ici en toutes lettres.
            return entete + " — IL A CHOISI « JE CORRIGE ET T'EXPLIQUE » POUR CETTE ERREUR. Ici, la "
                + "règle « ne donne jamais la réponse » NE S'APPLIQUE PAS : il te l'a demandé, et le "
                + "contrôle est fini. NE LE FAIS PAS CHERCHER — pas de « regarde la figure », pas de "
                + "« à ton avis ». DANS CE MESSAGE : ce qu'il a écrit, pourquoi c'est faux — la règle, "
                + "la notion, l'étape où ça a déraillé —, puis LA BONNE RÉPONSE écrite en clair avec sa "
                + "démarche, et une petite question pour vérifier qu'il a compris."
                + suite + "]";
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
            DateTime maintenantParis, int nbCours = 0, (int Notions, DateTime? ProgrammeValideLe)? examenBlanc = null)
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
                // UN EXAMEN BLANC (Camara, le 23/11/2026 : « pourquoi la prof me pose la question alors que j'ai envoyé
                // des documents ») : le programme vide n'est pas un programme inconnu — il attend l'accord de l'élève
                // sur celui que ses dossiers proposent. Voir AgentPedagogiqueService.ContexteExamenBlanc.
                { PerimetreConnu: false } when examenBlanc is { Notions: > 0 } =>
                    "C'EST SON EXAMEN BLANC, ET SES DOCUMENTS SONT LUS ET RANGÉS EN DOSSIERS (section « Ce contrôle est "
                    + "son … »). NE LUI DEMANDE PAS sur quoi il porte, ni ses cours. Dans ce premier message, propose-lui "
                    + "AU TABLEAU le programme tiré de ses dossiers, demande-lui s'il est d'accord, puis attends sa "
                    + $"réponse ; dès qu'il l'est, écris [CONTROLE_NOTIONS] avec controle: {controle.Id} et les notions "
                    + "retenues — c'est ce qui valide son programme —, puis commence les révisions.",

                // LE PROGRAMME MANQUE : C'EST LA PREMIÈRE CHOSE À OBTENIR.
                // Sans lui, il n'y a rien à préparer, rien à mesurer, et la
                // barre de l'élève reste à zéro quoi qu'il travaille.
                // TITRE VAGUE, DEUX CAS — décision de Camara, le 21/11/2026 :
                // a) sans cours importé, le professeur DEMANDE et ne prépare
                //    rien avant d'avoir la réponse ; le titre est ensuite
                //    complété (« Contrôle de maths : additions avec retenue ») ;
                // b) avec un cours, il le LIT, en tire lui-même le sujet,
                //    prépare tout de suite, et dit à l'enfant que le titre était
                //    trop vague mais que son cours a suffi.
                { PerimetreConnu: false } when nbCours > 0 =>
                    "ON NE SAIT PAS ENCORE CE QU'IL Y A AU PROGRAMME de ce contrôle"
                    + (TitreDeControle.EstGenerique(controle.Sujet, controle.MatiereLibelle)
                        ? " : son titre est trop vague pour le dire"
                        : "")
                    + ". MAIS SON COURS EST IMPORTÉ : ne lui demande PAS sur quoi porte le contrôle. "
                    + "Lis son cours (section « Le cours de l'élève pour ce contrôle ») et déduis-en "
                    + "TOI-MÊME le sujet précis du contrôle. Dans CE premier message : "
                    + (TitreDeControle.EstGenerique(controle.Sujet, controle.MatiereLibelle)
                        ? "dis-lui simplement que le titre de son contrôle était trop vague pour savoir "
                          + "sur quoi il porte, mais que grâce au cours qu'il a posté tu as pu le "
                          + "déterminer, et nomme-lui ce sujet ; "
                        : "nomme-lui le sujet que tu as tiré de son cours ; ")
                    + $"écris [CONTROLE_NOTIONS] avec controle: {controle.Id}, un `sujet:` court et "
                    + "précis tiré du cours (sans majuscule au début sauf nom propre, ex. « additions "
                    + "avec retenue » — il complète le titre de son contrôle) et les notions de ce "
                    + "cours ; puis COMMENCE LA PRÉPARATION tout de suite, sur ce cours.",

                { PerimetreConnu: false } =>
                    "ON NE SAIT PAS ENCORE CE QU'IL Y A AU PROGRAMME de ce contrôle — "
                    + "son sujet est trop vague ou n'a pas été précisé, et il n'a importé aucun "
                    + "cours. C'est ta PREMIÈRE question : demande-lui sur quoi ça porte "
                    + "exactement, ce que son professeur a annoncé, ce que vous avez vu en "
                    + "classe ces dernières semaines. NE COMMENCE AUCUNE PRÉPARATION — ni "
                    + "exercice, ni explication, ni question de cours — tant qu'il ne te l'a pas "
                    + "dit. Dès qu'il te l'a dit, écris [CONTROLE_NOTIONS] avec "
                    + $"controle: {controle.Id}, un `sujet:` court et précis (sans majuscule au "
                    + "début sauf nom propre, ex. « additions avec retenue » — il complète le "
                    // Plus « le sujet sera corrigé partout » : un titre donné
                    // par la famille ne change plus (signalement #54).
                    + "titre de son contrôle) et les notions correspondantes — c'est ce qui pose "
                    + "son programme —, puis commence la préparation. TANT QUE LE "
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
        /// Les écarts numérotés de la dictée en correction, tels que le tableau
        /// les montre — voir <see cref="EcartsDictee"/>. Null si rien ne permet
        /// de les recalculer : le rappel de correction part alors seul.
        /// </summary>
        private async Task<string?> EcartsAuTableauAsync(CorrectionDictee correction, int eleveId, bool francais)
        {
            try
            {
                if (correction.DicteeId is int id)
                {
                    var dictee = await _dictees.GetDetailAsync(id, eleveId);
                    return dictee is null ? null : FaitsDeCorrection(dictee.TexteDicte, dictee.Copie, francais);
                }

                return FaitsDeCorrection(correction.Dicte, correction.Copie, francais);
            }
            catch (Exception ex)
            {
                // Un rappel manqué ne doit pas coûter la réponse du professeur.
                _logger.LogError(ex, "Ecarts de la dictee en correction non calcules (eleve {EleveId}).", eleveId);
                return null;
            }
        }

        /// <summary>
        /// Les faits d'une correction de dictée : les écarts numérotés du tableau, et en français les lettres
        /// muettes en cause (signalement #58 — voir <see cref="LettresMuettes"/>). Null s'il n'y a rien à dire.
        /// </summary>
        private static string? FaitsDeCorrection(string? dicte, string? copie, bool francais)
        {
            var ecarts = EcartsDictee.Marqueur(dicte, copie);
            var muettes = francais ? LettresMuettes.Fait(dicte, copie) : null;

            return (ecarts, muettes) switch
            {
                (null, null) => null,
                (_, null) => ecarts,
                (null, _) => muettes,
                _ => ecarts + "\n\n" + muettes,
            };
        }

        private const string AgentFrancais = "agent-francais";

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

            // LE NOUVEL EXERCICE REMPLACE L'ANCIEN AU TABLEAU — Camara, le
            // 05/10/2026 : « on est passé à l'exercice suivant sans que le
            // professeur mette à jour le tableau ». Il donnait le nouvel énoncé à
            // l'oral, et l'élève avait encore l'exercice d'avant sous les yeux,
            // avec d'autres valeurs. Le fait du tour disait déjà ce qui était
            // affiché ; il ne disait pas QUE CHANGER D'EXERCICE OBLIGE À RÉÉCRIRE.
            return $"\n\n[TABLEAU : il affiche {quoi}. C'est la SEULE chose que "
                 + "l'élève voit. Tout autre support dont tu parlerais n'est pas "
                 + "devant lui : remets-le au tableau avant d'en parler. Si tu "
                 + "passes à un NOUVEL exercice — un nouvel énoncé, de nouvelles "
                 + "valeurs, une nouvelle question —, ce qui est affiché est "
                 + "l'ANCIEN : écris le nouveau dans un bloc [ARDOISE] dans CE "
                 + "message, qui remplace l'ancien. Ne le donne jamais à l'oral "
                 + "seulement.]";
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
        /// <remarks>
        /// DU CÔTÉ DE L'ÉLÈVE, JAMAIS DANS LA BOUCHE DU PROFESSEUR — Camara, le
        /// 04/10/2026. La note était collée au bout du message du professeur qui
        /// portait le repère, dans son propre historique. À la deuxième reprise
        /// de la même dictée, il avait deux exemples de « lui-même » écrivant ce
        /// bloc : il l'a recopié entier dans sa réponse, texte anglais compris,
        /// malgré « ne le recopie jamais » — et la voix l'a lu. Une consigne ne
        /// vaut pas un fait, et un fait placé dans la bouche du modèle devient
        /// une habitude. La note va donc dans le MESSAGE DE L'ÉLÈVE qui suit, ou
        /// dans le déclencheur du tour quand le repère est dans la dernière
        /// réponse : c'est là que vivent tous les autres faits de l'application.
        /// Rendu : ce qui doit rejoindre le déclencheur ; vide sinon.
        /// </remarks>
        private async Task<string> DeplierDicteesAuTableauAsync(
            List<DomainMessage> historique, int eleveId, CancellationToken ct)
        {
            try
            {
                var numeros = historique
                    .Where(m => m.Role == "assistant" && !string.IsNullOrEmpty(m.Contenu))
                    .SelectMany(m => LecteurDictee.AuTableau().Matches(m.Contenu!))
                    .Select(m => int.TryParse(m.Groups["id"].Value, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (numeros.Count == 0) return string.Empty;

                var textes = new Dictionary<int, string>();

                foreach (var numero in numeros)
                {
                    var d = await _dictees.GetDetailAsync(numero, eleveId, ct);
                    if (d is null) continue;

                    textes[numero] =
                        $"[Ajouté par l'application : l'écran affiche au tableau la dictée n° {d.Id} "
                        + $"du {d.DateCreation:dd/MM/yyyy}"
                        + (string.IsNullOrWhiteSpace(d.Titre) ? "" : $" « {d.Titre} »")
                        + ", avec ses erreurs surlignées et numérotées dans l'ordre du texte.\n"
                        + "La dictée\n" + d.TexteDicte + "\n\nTa copie\n" + d.Copie + "]";
                }

                // Le texte complet une seule fois par dictée, sur sa DERNIÈRE
                // remise au tableau ; les remises antérieures n'en gardent qu'une
                // ligne — l'historique ne porte pas quatre copies du même texte.
                var complets = new HashSet<int>();
                var complement = new System.Text.StringBuilder();

                for (var i = historique.Count - 1; i >= 0; i--)
                {
                    var message = historique[i];
                    if (message.Role != "assistant" || string.IsNullOrEmpty(message.Contenu)) continue;

                    var notes = LecteurDictee.AuTableau().Matches(message.Contenu)
                        .Select(m => int.TryParse(m.Groups["id"].Value, out var id) ? id : 0)
                        .Where(textes.ContainsKey)
                        .Distinct()
                        .Select(id => complets.Add(id)
                            ? textes[id]
                            : $"[Ajouté par l'application : l'écran affichait alors la dictée n° {id} au tableau.]")
                        .ToList();

                    if (notes.Count == 0) continue;

                    var note = string.Join("\n", notes);
                    var suivant = i + 1 < historique.Count ? historique[i + 1] : null;

                    if (suivant is { Role: "user" })
                    {
                        suivant.Contenu = note + "\n\n" + suivant.Contenu;
                    }
                    else
                    {
                        // Le repère est dans la dernière réponse : le tour en
                        // cours est celui qui doit savoir ce qu'il y a au tableau.
                        complement.Insert(0, "\n\n" + note);
                    }
                }

                return complement.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec du depliage des dictees au tableau (eleve {EleveId}).", eleveId);
                return string.Empty;
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
            //
            // ET CE QU'ÉTAIT LA SÉANCE D'AVANT, lu juste avant d'être réécrit :
            // si elle portait sur autre chose, le tour d'arrivée le dit (retest
            // #54 d'Adam, voir SujetDeSeance).
            var precedent = await DefinirModeSeanceAsync(conversationId, mode, controleId, epreuveCode, ct);

            await foreach (var fragment in StreamTourAsync(
                conversationId, contenu: null, accueil: true, ct, sujetPrecedent: precedent))
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
        ///
        /// Rend le sujet de la séance PRÉCÉDENTE (mode et cible d'avant la
        /// réécriture), ou null si on ne le sait pas — voir SujetDeSeance.
        /// </summary>
        private async Task<SujetDeSeance.Sujet?> DefinirModeSeanceAsync(
            int conversationId, string? mode, int? controleId, string? epreuveCode, CancellationToken ct)
        {
            try
            {
                var contexte = await _resolver.ResoudreConversationAsync(conversationId);
                if (contexte is null) return null;

                var precedent = SujetDeSeance.De(
                    contexte.Conversation.ModeSeance,
                    contexte.Conversation.ModeControleId,
                    contexte.Conversation.ModeEpreuveCode);

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

                return precedent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de la definition du mode de seance (conversation {ConversationId}).", conversationId);
                return null;
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
            string? vitesseEcoute = null,
            bool? sonJuste = null,
            IReadOnlyList<string>? figureIncomprise = null,
            VerdictDuTour? verdictDuTour = null,
            SujetDeSeance.Sujet? sujetPrecedent = null)
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

            // LES COMPRÉHENSIONS ORALES ENCORE OUVERTES — voir EcouteAuTableau.
            // On parcourt l'historique dans l'ordre : chaque passage lu ouvre
            // une écoute, son archive la referme. Au passage, un tableau qui
            // reproduirait le texte lu est retiré de ce que le professeur
            // relit — sans quoi il se croirait fondé à l'y remettre, et l'état
            // du tableau qu'on lui annonce plus bas serait faux.
            var ecoutesOuvertes = new List<string>();

            foreach (var message in historique.Where(m => m.Role == "assistant"))
            {
                message.Contenu = EcouteAuTableau.Avancer(ecoutesOuvertes, message.Contenu);
            }

            var typeAccueil = TypeAccueil.Aucun;
            TimeSpan? depuis = null;
            // Le temps écoulé depuis la sortie signalée par l'écran (page rechargée ou fermée, bouton « Quitter ») ; null sans
            // sortie explicite. Sert à reconnaître un RECHARGEMENT (LectureDesNombres.EstUnRechargement).
            TimeSpan? absenceApresSortie = null;

            // La lecture des nombres de ce tour — voir LectureDesNombres, plus bas.
            var enLectureDeNombres = false;
            Services.Voix.LectureDesNombres.Verdict? verdictNombre = null;
            List<(string? Role, string? Contenu, bool? SonJuste)> recentsNombres = [];
            // Il quitte la lecture des nombres pour une autre activité qu'il nomme (« on fait une addition », #51).
            var quitteLectureDeNombres = false;
            // Sa répétition en langue tapée, jugée avant d'être enregistrée (la traduction au lieu du mot) — voir plus bas.
            RepetitionEnLangue.Tour? repetitionTapee = null;

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
            var piecesAttachees = new List<int>();

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

                    // LA SORTIE SE COMPARE AU DERNIER MOT DE L'ÉLÈVE, PAS AU DERNIER
                    // MESSAGE — Camara, le 01/11/2026 : « quand je suis revenu, le
                    // prof ne m'a pas dit bonjour ». Relevé en base : il quitte à
                    // 17:29:25, le worker de conclusion écrit son [RAPPORT] à
                    // 17:29:28 — un message APRÈS la sortie. Comparée à lui, la
                    // sortie n'était plus « explicite », et un retour une minute
                    // plus tard tombait sous le seuil : aucun accueil. Tout ce que
                    // l'application écrit après le départ (rapport, fiche, abandon)
                    // est postérieur par construction ; seul l'élève date vraiment
                    // la fin de sa présence.
                    var dernierDeLEleve = historique.LastOrDefault(m => m.Role == "user") ?? dernier;

                    var sortieExplicite =
                        marqueur is { } sortie && sortie >= dernierDeLEleve.DateCreation;
                    if (sortieExplicite) absenceApresSortie = DateTime.UtcNow - marqueur!.Value;

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
                // LA LECTURE DES NOMBRES (12/11/2026) — voir Services.Voix.LectureDesNombres. Le verdict se pose AVANT d'écrire son
                // message, comme celui d'un son : la bulle dit « Bonne lecture » ou « On réessaie », et le message garde
                // « J'ai lu le nombre … ». Jamais pendant un contrôle noté.
                if (Services.Voix.LectureDesNombres.Concerne(contexte.Conversation.AgentSlug, contexte.Eleve.NiveauCode)
                    && !string.IsNullOrWhiteSpace(contenu)
                    && !LecteurAuRevoir.DitAuRevoir(contenu)
                    && !LecteurEvaluation.EstOuvert(historique.Where(m => m.Role == "assistant").Select(m => m.Contenu)))
                {
                    recentsNombres = historique.TakeLast(MessagesPourExerciceDeSons)
                        .Select(m => ((string?)m.Role, (string?)m.Contenu, m.SonJuste)).ToList();
                    quitteLectureDeNombres = Services.Voix.LectureDesNombres.DernierMontre(recentsNombres) is not null
                        && Services.Voix.LectureDesNombres.DemandeUneAutreActivite(contenu);
                    // « vingt-huit. Maintenant on fait une addition » (contrôle d'Omar, #50/#51) : sa LECTURE est jugée
                    // (« Bonne lecture »), PUIS on change d'exercice — au clavier comme au micro (le relais a déjà jugé).
                    if (quitteLectureDeNombres || Services.Voix.LectureDesNombres.EnCours(recentsNombres, contenu, contexte.Eleve.NiveauCode))
                    {
                        enLectureDeNombres = !quitteLectureDeNombres;
                        verdictNombre = Services.Voix.LectureDesNombres.Juger(recentsNombres, contenu);
                        if (verdictNombre is not null)
                        {
                            var texteGarde = Services.Voix.LectureDesNombres.TexteAvecLaSuite(verdictNombre, contenu);
                            // Tapé au clavier : l'écran ne connaît pas encore le verdict, il le reçoit avec la réponse (#49).
                            // Seulement quand la vérification des syllabes est allumée (verdictDuTour.Juger), comme la
                            // lecture en images tapée : réglage éteint, l'écran ne reçoit aucun événement nouveau (Omar).
                            if (sonJuste is null && verdictDuTour is { Juger: true }) verdictDuTour.Poser(verdictNombre.Juste, texteGarde);
                            sonJuste ??= verdictNombre.Juste;
                            contenu = texteGarde;
                        }
                    }
                }

                // LA LECTURE EN IMAGES, TAPÉE AU CLAVIER — signalement #49 (Adam, 20/11/2026) : « son » écrit devant la carte
                // « poisson » recevait « Bravo… essaie encore », « pa » était félicité, « lit » refusé pour « lit », et la carte ne
                // changeait jamais. Seul le relais du micro jugeait (EcouteController) : un texte tapé arrivait sans verdict, le
                // code remontrait la même image et le professeur jugeait seul. Il est jugé ici, comme la lecture des nombres
                // tapée : même comparaison que le micro (VerifierLectureAsync), même message gardé (« J'ai lu la syllabe son. »),
                // même compte des essais. Pas une demande (changer d'exercice, au revoir, une phrase) : elle part telle quelle.
                if (!enLectureDeNombres && sonJuste is null && verdictDuTour is { Juger: true }
                    && await LectureTapeeAsync(
                        conversationId,
                        Services.Voix.RegleVerificationSon.LangueDe(contexte.Eleve.NiveauCycle, contexte.Conversation.MatiereCode),
                        historique.Select(m => ((string?)m.Role, (string?)m.Contenu)).ToList(),
                        contenu,
                        ct) is { } lectureTapee)
                {
                    sonJuste = lectureTapee.SonJuste;
                    contenu = lectureTapee.Contenu;
                    verdictDuTour.Poser(sonJuste, contenu);
                }

                // L'ÉCOUTE DES MOTS ET LA RÉPÉTITION EN LANGUE, TAPÉES AU CLAVIER — contrôle d'Omar sur le #57 v2, accord de Min's
                // (21/11/2026, 16 h : « oui faut modifier l'écran pour compter 3 essais au lieu de 5 »). Une traduction tapée
                // (« Deux! » pour Two, « Bleu! » pour Blue) n'avait aucun verdict : rien à l'écran, aucun essai compté, et le
                // même mot pouvait revenir cinq fois. Elle est jugée ici, comme la lecture tapée (#49) : le verdict part avant la
                // réponse, la bulle dit « Mauvaise écoute » ou « On réessaie » (une traduction, décision de Min's du 22/11/2026 :
                // voir RegleVerificationSon.TexteDEcoute), et l'essai compte pour la règle des trois essais.
                // Mêmes conditions que le micro et la lecture tapée : vérification des sons allumée, pas déjà jugé, pas en
                // lecture des nombres. Portée GÉNÉRIQUE : l'écoute des mots en français et en anglais, et tous les professeurs
                // de langue pour la répétition (RepetitionEnLangue).
                if (!enLectureDeNombres && sonJuste is null && verdictDuTour is { Juger: true, Pose: false })
                {
                    var langueDesSons = Services.Voix.RegleVerificationSon.LangueDe(contexte.Eleve.NiveauCycle, contexte.Conversation.MatiereCode);
                    var duProfesseur = historique.Where(m => m.Role == "assistant").Select(m => (string?)m.Contenu).ToList();

                    // L'écoute des mots, en exercice de sons : le même juge que le micro.
                    if (await EcouteTapeeAsync(conversationId, langueDesSons, duProfesseur, contenu, ct) is { } ecouteDuClavier)
                    {
                        sonJuste = ecouteDuClavier.SonJuste;
                        contenu = ecouteDuClavier.Contenu;
                        verdictDuTour.Poser(sonJuste, contenu);
                    }

                    // « Tu répètes ? » : seule la TRADUCTION française est reconnue (juge prudent), donc seul un verdict FAUX est
                    // posé. Le message gardé porte la marque « J'ai répété : » (voir RepetitionEnLangue.MarqueDeLaReponse) : sans
                    // elle, ce verdict ouvrirait un exercice de sons aux tours suivants. Y COMPRIS quand le code croit un exercice de
                    // sons en cours : il suffit que le professeur écrive « tu me redis ce son ? » ou « avec le son anglais » pendant
                    // une leçon de couleurs pour que la conversation y bascule (bout en bout de la tâche 1600), et l'essai suivant
                    // n'était plus compté. Les juges propres aux exercices de sons passent avant (lecture tapée #49, écoute tapée
                    // ci-dessus) ; une dictée n'est pas concernée (sa copie arrive sous « [DICTÉE AU CLAVIER : …] », ignorée par le juge).
                    else if (RepetitionEnLangue.Concerne(contexte.Conversation.AgentSlug)
                        && !LecteurAuRevoir.DitAuRevoir(contenu)
                        && ChangementDExercice.Lire(contenu) == ChangementDExercice.Nature.Aucune
                        && !LecteurEvaluation.EstOuvert(duProfesseur)
                        && RepetitionEnLangue.Juger(
                            contexte.Conversation.AgentSlug,
                            duProfesseur.Where(c => !RepetitionEnLangue.EstTechnique(c)).TakeLast(RepetitionEnLangue.MessagesLus).ToList(),
                            contenu) is { } repetition)
                    {
                        repetitionTapee = repetition;
                        sonJuste = false;
                        contenu = RepetitionEnLangue.TexteDeLEleve(contenu!);
                        verdictDuTour.Poser(sonJuste, contenu);
                        _logger.LogInformation(
                            "Repetition en langue tapee jugee (conversation {ConversationId}) : attendu « {Attendu} », traduction « {Francais} », juste False.",
                            conversationId, repetition.Attendu, repetition.Francais);
                    }
                }

                var messageEleve = await _conversationService.AddMessageAsync(new DomainMessage
                {
                    ConversationId = conversationId,
                    Role = "user",
                    Contenu = contenu,
                    // Le verdict de la voix (04/11/2026) : la bulle écrit « Bonne prononciation » ou
                    // « On réessaie » à la place du son, et doit le refaire
                    // au rechargement. Le professeur, lui, lit le son dans `contenu`.
                    SonJuste = sonJuste,
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
                        // DANS L'ORDRE D'ENVOI : la carte de copie envoie l'énoncé
                        // puis la copie, et ses marqueurs dans le même ordre. Une
                        // copie reste d'UNE page — voir la carte.
                        piecesAttachees.Add(idPiece);

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
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, contenu, piecesAttachees, ct);
            }

            // L'API exige que la conversation se termine par un tour utilisateur.
            // Pour l'accueil on fournit un marqueur de contexte, jamais persisté
            // ni affiché : c'est le bloc système Accueil qui porte la consigne.
            var declencheur = typeAccueil switch
            {
                TypeAccueil.FinProche => "[Le minuteur indique qu'il reste cinq minutes de séance.]",
                TypeAccueil.FinSeance => "[Le temps de la séance est écoulé.]",
                // LA RELANCE PARLE DANS LA CONTINUITÉ — Camara, le 11/11/2026 : « ce qu'on a vu hier », quelques
                // minutes après. Le temps écoulé voyage dans le tour : voir PromptsPedagogiques.SeuilRelance.
                TypeAccueil.NouvelleSeance => PromptsPedagogiques.DeclencheurNouvelleSeance,
                TypeAccueil.Retour => PromptsPedagogiques.DeclencheurDeRetour(depuis),
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

            // L'ARRIVÉE DANS LA PRÉPARATION D'UN EXAMEN BLANC NE REPREND PAS LE COURS D'AVANT — Camara, le 23/11/2026 :
            // « je suis entré dans la préparation d'un examen blanc, et elle me dit : On repart sur 38 ÷ 5 ? ; la
            // préparation d'un examen, ce n'est pas un cours normal ». La préparation vit dans la même conversation que
            // les cours de la matière : moins de 45 minutes après un cours ordinaire, la consigne de relance (« repars
            // sur ce que vous faisiez ») reprenait l'exercice de ce cours-là. Ici, l'arrivée annonce l'examen blanc.
            if (typeAccueil is TypeAccueil.Retour or TypeAccueil.NouvelleSeance or TypeAccueil.PremiereSeance
                && mode == ModesSeance.Controle && controleMode is int controleArrivee && _examensBlancs is not null
                && await _examensBlancs.GetEtatParControleAsync(controleArrivee, ct) is { } etatArrivee)
            {
                declencheur = etatArrivee.ProgrammeValideLe is null
                    ? "[L'élève vient d'ouvrir la PRÉPARATION DE SON EXAMEN BLANC (bouton de son examen blanc). Ce n'est "
                      + "PAS un cours ordinaire : ne reprends AUCUN exercice ni sujet d'un cours précédent, même récent. "
                      + "Salue-le en une phrase, puis présente-lui au tableau le programme tiré de ses documents et "
                      + "demande-lui s'il est d'accord.]"
                    : "[L'élève vient d'ouvrir la PRÉPARATION DE SON EXAMEN BLANC (bouton de son examen blanc). Ce n'est "
                      + "PAS un cours ordinaire : ne reprends aucun exercice d'un cours ordinaire. Repars sur son "
                      + "programme d'examen blanc : la notion où vous en étiez, ou la suivante qui n'est pas encore "
                      + "tenue, en le lui disant en une phrase.]";
            }

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

            int? dicteeEnAttenteId = null;

            // Une dictée, une correction ou une copie à transcrire ouvre la séance : la lecture des nombres ne reprend pas.
            var arriveeOccupee = false;

            if (retourEnCours)
            {
                var enAttente = await DicteeEnAttenteAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId,
                    contexte.Conversation.AgentSlug == AgentFrancais, ct);

                if (enAttente is { } attente)
                {
                    declencheur += "\n\n" + attente.Rappel;
                    dicteeEnAttenteId = attente.Id;
                    arriveeOccupee = true;
                }

                // Une correction d'évaluation remise faute de temps ouvre, elle
                // aussi, le cours suivant — voir CorrectionEnAttenteAsync.
                var copieACorriger = await CorrectionEnAttenteAsync(
                    contexte.Eleve.Id, contexte.Conversation.MatiereId, ct);

                if (copieACorriger is not null)
                {
                    declencheur += "\n\n" + copieACorriger;
                    arriveeOccupee = true;
                }

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
                    arriveeOccupee = true;
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

            // LA LECTURE DES NOMBRES REPREND AU TOUR D'ARRIVÉE — refus d'Omar sur le retest #51 (21/11/2026), régression du
            // défaut e8 : l'enfant recharge la page pendant la lecture, le professeur écrit son message de retour, et ce tour
            // était hors de l'exercice. Sa balise était retirée, l'exercice se fermait : « sept » n'était plus jugé et le
            // professeur disait « le tableau est vide ». Si son dernier message montrait un nombre, on reste dans l'exercice et
            // le code remontre ce nombre (voir LectureDesNombres.Reprise). Au retour et à l'annonce « cinq minutes » (le
            // minuteur qui parle au milieu de la lecture) SEULEMENT (LectureDesNombres.RepriseAuTourDArrivee, contrôle
            // d'Omar sur 44fd5c6) : une nouvelle séance repart à zéro, et après « Quitter » ou une séance conclue
            // ([RAPPORT] / [FIN_SEANCE]) Reprise ne rend rien. Jamais quand une dictée, une correction ou une copie ouvre
            // la séance, ni pendant un contrôle noté. La conclusion de séance, elle, ferme l'exercice comme avant.
            Services.Voix.LectureDesNombres.Tour? repriseNombres = null;
            if (Services.Voix.LectureDesNombres.RepriseAuTourDArrivee(typeAccueil)
                && !arriveeOccupee
                && Services.Voix.LectureDesNombres.Concerne(contexte.Conversation.AgentSlug, contexte.Eleve.NiveauCode)
                && !LecteurEvaluation.EstOuvert(historique.Where(m => m.Role == "assistant").Select(m => m.Contenu)))
            {
                var recents = historique.TakeLast(MessagesPourExerciceDeSons)
                    .Select(m => ((string?)m.Role, (string?)m.Contenu, m.SonJuste)).ToList();
                // Un RECHARGEMENT (retour quelques secondes après la sortie) passe par le même chemin que « Quitter » : le
                // worker de départ écrit [FICHE]/[RAPPORT]…[FIN_SEANCE] après le nombre. Ces messages techniques ne ferment
                // alors pas la lecture (option rechargement, tâche 0140) ; ils la ferment après une vraie absence.
                repriseNombres = Services.Voix.LectureDesNombres.Reprise(recents,
                    Services.Voix.LectureDesNombres.EstUnRechargement(typeAccueil, absenceApresSortie));
                if (repriseNombres is not null)
                {
                    recentsNombres = recents;
                    enLectureDeNombres = true;
                    _logger.LogInformation(
                        "Lecture des nombres reprise au tour d'arrivee ({Type}, conversation {ConversationId}).", typeAccueil, conversationId);
                }
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
            //
            // Sauf quand la lecture des nombres reprend (repriseNombres) : la gomme passe, puis le code remontre aussitôt le
            // nombre ; c'est LectureDesNombres.FaitReprise qui le dit au professeur, plus bas (« le tableau n'est pas vide »).
            if (nouvelleArrivee && repriseNombres is null)
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
            else if (!nouvelleArrivee)
            {
                // PAS UNE ARRIVÉE, ET POURTANT LA MÊME FAUTE. Le rappel ci-dessus
                // ne partait qu'au premier message : un professeur qui invente un
                // support au dixième passait au travers. On lui dit donc l'état
                // réel du tableau à CHAQUE tour — voir `EtatDuTableau`.
                declencheur += EtatDuTableau(historique);
            }

            // LA LECTURE DES NOMBRES NE REPREND PAS, ET LE PROFESSEUR DOIT LE SAVOIR (contrôle d'Omar sur 44fd5c6, tâche
            // 0140) : une nouvelle séance, ou un retour après « Quitter » / une séance conclue, repart à zéro. Vu au bout en
            // bout : sans ce fait, le professeur voit le nombre dans son historique et dit « Regarde bien le tableau » ou
            // « On repart sur le nombre qu'on regardait ? » devant un tableau vide (la balise qu'il recopie est retirée).
            if (nouvelleArrivee && repriseNombres is null
                && Services.Voix.LectureDesNombres.Concerne(contexte.Conversation.AgentSlug, contexte.Eleve.NiveauCode))
            {
                if (Services.Voix.LectureDesNombres.DernierMontre(historique.TakeLast(MessagesPourExerciceDeSons)
                    .Select(m => ((string?)m.Role, (string?)m.Contenu, m.SonJuste)).ToList()) is not null)
                {
                    declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitLectureTerminee;
                }
                // Des nombres plus haut dans l'historique, mais pas dans son dernier message (retest d'Adam, #51, 21/11/2026 :
                // « On repart sur le 23, tu me le lis ? » devant un tableau vide, relancé 22 s après « Quitter ») : il doit
                // savoir qu'aucun n'est affiché.
                else if (Services.Voix.LectureDesNombres.AMontreDesNombres(historique.Select(m => ((string?)m.Role, (string?)m.Contenu))))
                {
                    declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitAucunNombreAffiche;
                }
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
                // LA SÉANCE D'AVANT PORTAIT-ELLE SUR AUTRE CHOSE ? Retest #54
                // d'Adam (21/11/2026) : préparation d'un 2e contrôle de sciences
                // ouverte quelques minutes après celle d'un 1er, et le professeur
                // commençait par « On reprend : une plante… », la question restée
                // en bas de l'historique. Voir SujetDeSeance.
                string? autreSujet = null;
                if (typeAccueil != TypeAccueil.PremiereSeance && sujetPrecedent is not null)
                {
                    autreSujet = await FaitAutreSujetAsync(
                        contexte.Eleve.Id, sujetPrecedent, SujetDeSeance.De(mode, controleMode, epreuveMode), ct);
                }

                switch (mode)
                {
                    case ModesSeance.Controle:
                        var controleAVenir = await ControleAVenirAsync(
                            contexte.Eleve.Id, contexte.Conversation.MatiereId, controleMode!.Value, ct);

                        if (controleAVenir is not null)
                        {
                            declencheur += "\n\n[L'élève ouvre cette séance POUR préparer ce "
                                + "contrôle : c'est le sujet de la séance, commence par là.]";

                            if (autreSujet is not null) declencheur += "\n\n" + autreSujet;

                            // UN RECHARGEMENT NE ROUVRE PAS LA PRÉPARATION. Son
                            // ouverture (`DernierePreparationLe`) dit depuis quand
                            // l'enfant doit avoir répondu pour qu'un verdict
                            // s'écrive (VerdictsDuTour) ; or la conclusion de départ
                            // d'un rechargement arrive quelques secondes APRÈS ce
                            // nouvel accueil, et elle aurait écarté le verdict du
                            // travail fait juste avant. Même contrôle, retour moins
                            // d'une minute après la sortie : la séance continue.
                            var rechargementDuControle = sujetPrecedent is not null
                                && !SujetDeSeance.Change(sujetPrecedent, SujetDeSeance.De(mode, controleMode, epreuveMode))
                                && Services.Voix.LectureDesNombres.EstUnRechargement(typeAccueil, absenceApresSortie);

                            if (!rechargementDuControle)
                            {
                                await MarquerPreparationControleAsync(
                                    contexte.Eleve.Id, controleMode.Value, ct);
                            }

                            declencheur += "\n\n" + controleAVenir;
                        }

                        break;

                    case ModesSeance.Bilan:
                        var bilanControle = await ControleABilanAsync(
                            contexte.Eleve.Id, contexte.Conversation.MatiereId, controleMode!.Value, ct);

                        if (bilanControle is not null)
                        {
                            if (autreSujet is not null) declencheur += "\n\n" + autreSujet;
                            declencheur += "\n\n" + bilanControle;
                        }

                        break;

                    case ModesSeance.Examen:
                        var examenAVenir = await ExamenAVenirAsync(
                            contexte.Eleve.Id, contexte.Eleve.NiveauCode, contexte.Conversation.MatiereId,
                            epreuveMode!, ct);

                        if (examenAVenir is not null)
                        {
                            if (autreSujet is not null) declencheur += "\n\n" + autreSujet;
                            declencheur += "\n\n" + examenAVenir;
                        }

                        break;
                }
            }

            // LA DATE DU JOUR, À CHAQUE TOUR — pas seulement à l'arrivée : un
            // élève peut annoncer un contrôle n'importe quand dans la
            // conversation, et le professeur doit pouvoir traduire « vendredi
            // prochain » en date absolue pour écrire [CONTROLE_PROGRAMME].
            declencheur += "\n\n" + PromptsPedagogiques.MarqueurDateDuJour(DateTime.UtcNow);

            // Le nombre de documents du cours, relu à chaque tour (une requête
            // légère, sans octets) : c'est le FAIT que le marqueur énonce. Un
            // document illisible ne compte pas comme cours : il se fait réimporter.
            int? nbCours = null;
            var nbIllisibles = 0;
            var programmeInconnu = false;
            IReadOnlyList<string> notionsDuTour = [];

            // LE RETRAIT D'UNE NOTION, SUR UN OUI ET RIEN D'AUTRE — #67 v5. Fait AVANT de relire le programme du tour :
            // le professeur répond avec le programme tel qu'il est après le retrait, et le sait.
            var faitDuRetrait = mode == ModesSeance.Controle && controleMode is int controleConfirme && !accueil
                ? await RetraitConfirmeAsync(
                    conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId, controleConfirme,
                    historique.LastOrDefault(), contenu, ct)
                : "";

            if (mode == ModesSeance.Controle && controleMode is int controlePrepare)
            {
                try
                {
                    var documents = await _controles.GetDocumentsAsync(contexte.Eleve.Id, controlePrepare, ct);
                    nbCours = documents.Count(d => d.Exploitable);
                    nbIllisibles = documents.Count(d => d.Illisible);

                    // Le programme manque-t-il encore ? Voir `MarqueurModeSeance`.
                    var preparationDuTour = await _controles.GetPreparationAsync(contexte.Eleve.Id, controlePrepare, ct);
                    programmeInconnu = preparationDuTour is { PerimetreConnu: false };
                    notionsDuTour = preparationDuTour?.Notions.Select(n => n.Libelle).ToList() ?? [];
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Documents du controle {ControleId} illisibles pour le marqueur.", controlePrepare);
                }
            }

            // LE CONTRÔLE CACHÉ D'UN EXAMEN BLANC A SON PROPRE RAPPEL (23/11/2026) : celui d'un contrôle ordinaire
            // demande sur quoi il porte quand son programme est vide, et interdit de parler d'un brevet.
            var etatExamenBlanc = mode == ModesSeance.Controle && controleMode is int controleDeLExamen && _examensBlancs is not null
                ? await _examensBlancs.GetEtatParControleAsync(controleDeLExamen, ct)
                : null;

            declencheur += "\n\n" + (etatExamenBlanc is { } eb && controleMode is int idExamen
                ? MarqueurExamenBlanc(idExamen, eb.Notions, eb.ProgrammeValideLe is not null)
                : MarqueurModeSeance(mode, controleMode, epreuveMode, nbCours, nbIllisibles, programmeInconnu));

            // CE QUE LE OUI A FAIT (OU PAS), DIT AVANT LA RÉPONSE (#67 v5) ; et s'il ne nomme qu'un morceau d'une notion,
            // le professeur le sait aussi (#67 v3) : sinon il répond « c'est déjà fait ».
            if (mode == ModesSeance.Controle && controleMode is int && !accueil)
            {
                declencheur += faitDuRetrait;
                declencheur += MorceauNommeDuTour(contenu, notionsDuTour);
            }

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
            CalculAuTableau.Verdict? verdictCalcul = null;
            int? rangDeLErreur = null;
            if (ReponseChiffree.Contient(contenu))
            {
                // LE VERDICT D'UN CALCUL SIMPLE, COMPTÉ PAR LE CODE — Minamba, le 20/11/2026, signalement #29 rouvert
                // (avec #45, #46) : « 4 + 3 = ? » → « 8 » validé, « 8 - 3 = ? » → « 5 » refusé, « 2 dizaines et 0
                // unité » refusé pour 20, malgré le rappel. Voir CalculAuTableau. Quand la question ou la réponse ne se
                // lit pas sans ambiguïté, c'est le rappel d'avant. Pas dans la lecture des nombres (son verdict est
                // déjà compté), ni pendant un contrôle noté (Camara, 11/11/2026 : on n'y touche pas).
                verdictCalcul = controleOuvert || enLectureDeNombres
                    ? null
                    : CalculAuTableau.Juger(
                        historique.Where(m => m.Role == "assistant")
                            .Select(m => m.Contenu)
                            .Where(c => !string.IsNullOrWhiteSpace(c)
                                && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                                && !c.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))
                            .TakeLast(CalculAuTableau.MessagesRemontes),
                        contenu);

                var faitCalcul = verdictCalcul is null ? null : CalculAuTableau.Fait(verdictCalcul);
                if (faitCalcul is not null)
                {
                    _logger.LogInformation("Calcul juge (conversation {ConversationId}) : {Fait}", conversationId, faitCalcul);
                }

                // À QUELLE ERREUR IL EN EST, COMPTÉ PAR LE CODE — voir CalculAuTableau.RangDeLErreur (contrôle d'Omar sur le
                // lot groupé : « dernier essai » annoncé dès la 2e erreur, 1 séance sur 3).
                if (faitCalcul is not null && CalculAuTableau.RangDeLErreur(
                        historique.Select(m => ((string?)m.Role, (string?)m.Contenu)).ToList(), verdictCalcul!) is int rangErreur)
                {
                    rangDeLErreur = rangErreur;
                    faitCalcul += "\n\n" + CalculAuTableau.FaitRangDeLErreur(rangErreur);
                    _logger.LogInformation("Calcul faux : erreur numero {Rang} sur la question (conversation {ConversationId}).", rangErreur, conversationId);
                }

                declencheur += "\n\n" + (controleOuvert
                    ? ReponseChiffree.RappelControle
                    : faitCalcul ?? ReponseChiffree.RappelPour(contenu));
            }

            // « JE SAIS PAS » PENDANT UNE SUITE DE NOMBRES — Minamba, le 21/11/2026, signalement #60 : sans nombre,
            // pas de verdict, et Nora a inventé un indice faux (« ça avance d'une dizaine de plus » devant 71…80).
            // On lui donne l'indice calculé de la suite (CalculAuTableau.FaitSansReponse), sans verdict. Mêmes
            // exclusions que le verdict : ni contrôle noté, ni lecture des nombres.
            else if (!controleOuvert && !enLectureDeNombres
                && CalculAuTableau.FaitSansReponse(
                    historique.Where(m => m.Role == "assistant")
                        .Select(m => m.Contenu)
                        .Where(c => !string.IsNullOrWhiteSpace(c)
                            && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                            && !c.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))
                        .TakeLast(CalculAuTableau.MessagesRemontes),
                    contenu) is { } faitSansReponse)
            {
                _logger.LogInformation("Suite sans reponse (conversation {ConversationId}) : indice calcule donne", conversationId);
                declencheur += "\n\n" + faitSansReponse;
            }

            // ET SON SYMÉTRIQUE : « VAS-Y » N'EST PAS UNE RÉPONSE — voir
            // `InvitationAContinuer`. Le 03/10/2026, Nora a posé un exercice,
            // l'élève a répondu « Vas-y. », et elle a validé un 53 qu'elle
            // venait de calculer elle-même : « Oui, c'est exactement ça : 53 »,
            // puis « tu as tout retrouvé tout seul ».
            else if (InvitationAContinuer.Est(contenu))
            {
                declencheur += "\n\n" + InvitationAContinuer.Rappel;
            }

            // LA RÉPONSE QU'IL CHERCHE ENCORE NE LUI EST PAS DONNÉE — tâche 1941 (Omar, sur master : « 7 + 5, ça fait 12 : je
            // te le dis cette fois », environ 10 réponses sur 40). Camara et Min's : on ne donne JAMAIS la réponse. Le code
            // la connaît (même question que le juge) : tant que l'élève ne l'a pas trouvée, ReponseNonDonnee la retire de
            // l'écran, de la voix et de l'historique. Mêmes exclusions que le verdict : ni contrôle noté, ni lecture des nombres.
            var reponseCherchee = controleOuvert || enLectureDeNombres
                ? null
                : CalculAuTableau.ReponseCherchee(
                    historique.Where(m => m.Role == "assistant")
                        .Select(m => m.Contenu)
                        .Where(c => !string.IsNullOrWhiteSpace(c)
                            && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                            && !c.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))
                        .TakeLast(CalculAuTableau.MessagesRemontes),
                    contenu);

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
            // UNE ÉCOUTE OUVERTE SE RAPPELLE À CHAQUE TOUR — voir EcouteAuTableau :
            // le 04/10/2026, « je t'entends plus » a suffi pour que le passage
            // entier parte au tableau, malgré la consigne de l'exercice.
            if (ecoutesOuvertes.Count > 0) declencheur += "\n\n" + EcouteAuTableau.Rappel;

            var correctionDictee = LecteurDictee.EtatCorrection(
                historique.Where(m => m.Role == "assistant").Select(m => m.Contenu));

            if (correctionDictee.Ouverte)
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurCorrectionDicteeEnCours();

                // ET LES NUMÉROS EUX-MÊMES — voir `EcartsDictee`. Le 03/10/2026,
                // le professeur recomptait « par lignes » des erreurs que le
                // tableau numérote, s'est dédit deux fois, et a inventé un mot.
                // Le texte d'une dictée remise par son numéro n'est qu'au tour
                // d'accueil, jamais persisté : on le relit en base à chaque tour.
                // La même dictée que celle reprise à l'accueil : ses faits sont déjà dans le rappel d'attente.
                var dejaRappelee = dicteeEnAttenteId is int idRappele && correctionDictee.DicteeId == idRappele;
                var ecarts = dejaRappelee ? null : await EcartsAuTableauAsync(
                    correctionDictee, contexte.Eleve.Id, contexte.Conversation.AgentSlug == AgentFrancais);
                if (ecarts is not null) declencheur += "\n\n" + ecarts;

                // Les mots sont au tableau : ils se citent en clair (signalement #61, voir VoixEnClair).
                if (!dejaRappelee) declencheur += "\n\n" + Services.Voix.VoixEnClair.Consigne;

                // À L'ARRIVÉE, le tableau est effacé puis remis par l'application
                // elle-même (voir `tableauRepris`) : le professeur n'a ni à
                // l'annoncer ni à le réécrire.
                if (nouvelleArrivee)
                {
                    declencheur += "\n\n[L'élève revient : l'application remet ELLE-MÊME la dictée au tableau "
                        + "à l'instant, avec ses numéros. N'écris pas de repère [DICTEE_AU_TABLEAU], ne dis "
                        + "pas « je te la remets » : reprends la correction au numéro où vous en étiez.]";
                }
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

            var fil = historique.Select(m => (m.Role, m.Contenu ?? string.Empty)).ToList();

            var copieEnCours = !enonceSeul && (mode == ModesSeance.Bilan || agitSurLaCopie)
                ? await CopieControleAsync(contexte.Eleve.Id, contexte.Conversation.MatiereId, fil, contenu, ct)
                : null;

            if (copieEnCours is not null) declencheur += "\n\n" + copieEnCours;

            // L'ERREUR EN COURS SURVIT À LA CLÔTURE DE L'ANALYSE. Le rappel de copie
            // s'éteint dès que le professeur écrit la note ; la correction, erreur
            // par erreur, doit tenir jusqu'à la dernière. Quand la copie est encore
            // « en cours », son rappel porte déjà l'erreur : pas de doublon.
            if (copieEnCours is null && !enonceSeul && mode == ModesSeance.Bilan
                && controleMode is int controleDuBilan
                && LecteurCopieControle.EtatCorrection(fil, contenu, controleDuBilan) is
                    { Erreur: not null, Termine: false } correction)
            {
                declencheur += "\n\n" + MarqueurErreur(controleDuBilan, correction);
            }

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

            Services.Voix.EtatDuMot? etatDuMot = null;
            Services.Voix.ImageAuTableau.ImageMontree? imageAuTableau = null;
            Services.Voix.EssaisRates.Abandon? abandon = null;
            Services.Voix.LectureDImage.Tour? lectureDuTour = null;
            string? poserLeChoix = null;
            var langueDuSon = Services.Voix.RegleVerificationSon.LangueDe(contexte.Eleve.NiveauCycle, contexte.Conversation.MatiereCode);

            // IL CHANGE D'EXERCICE QUAND IL VEUT — Camara, le 14/11/2026, signalement d'une famille : voir
            // ChangementDExercice. Sa demande ferme l'exercice que le code tenait : ce qui la précède ne compte plus
            // (`filSons`), et ce qu'elle rejette (« j'en ai marre des syllabes ») ne choisit rien (`contenuPourLesSons`).
            // Une réponse jugée par le micro n'est jamais une demande. Pas pendant un contrôle noté : on n'y touche pas.
            var changement = controleOuvert || sonJuste is not null || LecteurAuRevoir.DitAuRevoir(contenu)
                ? ChangementDExercice.Nature.Aucune
                // L'activité qu'il nomme n'est pas un changement si c'est déjà celle en cours (contrôle d'Omar, #55/#56).
                : ChangementDExercice.Lire(contenu, historique.TakeLast(ChangementDExercice.MessagesDeLActiviteEnCours)
                    .Select(m => ((string)m.Role, (string?)m.Contenu, m.SonJuste)).ToList());
            // « Maintenant on fait une addition » pendant la lecture des nombres (signalement #51) : c'est une demande de
            // changement, même sans « autre exercice ». Le professeur reçoit le même fait, et l'ancien exercice est coupé.
            // Même quand sa lecture vient d'être jugée (« vingt-huit, maintenant on fait une addition », au micro ou tapé :
            // contrôle d'Omar) — le verdict reste, le changement se pose aussi.
            if (changement == ChangementDExercice.Nature.Aucune && quitteLectureDeNombres && !controleOuvert)
            {
                changement = ChangementDExercice.Nature.Changer;
            }
            var change = changement != ChangementDExercice.Nature.Aucune;
            var contenuPourLesSons = change ? ChangementDExercice.PartieQuiChoisit(contenu) : contenu;
            var filSons = ChangementDExercice.Fil(
                historique.TakeLast(MessagesPourExerciceDeSons).Select(m => ((string)m.Role, (string?)m.Contenu, m.SonJuste)).ToList(),
                change);

            // UNIQUEMENT DANS UN EXERCICE DE SONS OU DE SYLLABES — Camara, le 05/11/2026. Hors de lui, le
            // français garde son rappel d'avant et l'anglais ne reçoit rien : voir EnExerciceDeSons.
            var enExerciceDeSons = langueDuSon is not null && Services.Voix.RegleVerificationSon.EnExerciceDeSons(
                filSons.Select(m => (m.Contenu, m.SonJuste)).Append((contenuPourLesSons, null)));

            // LE FAIT DU CHANGEMENT, pour tous les professeurs. Pas quand sa demande choisit elle-même un exercice de
            // sons (« je veux lire des mots », la carte des exercices) : là, c'est le code qui ouvre le suivant.
            if (change && !(langueDuSon is not null && Services.Voix.LectureDImage.ChangeDExercice(contenuPourLesSons)))
            {
                // L'activité qu'il nomme (une histoire, du vocabulaire) suit le fait : signalements #55 et #56.
                declencheur += "\n\n" + ChangementDExercice.Fait(changement, contenu);
                // Il quitte les syllabes ou les mots sans nommer d'activité : pas la carte des sons une fois de plus (#55).
                if (langueDuSon is not null && !ChangementDExercice.NommeUneActivite(contenu)
                    && Services.Voix.RegleVerificationSon.EnExerciceDeSons(
                        historique.TakeLast(MessagesPourExerciceDeSons).Select(m => ((string?)m.Contenu, m.SonJuste))))
                {
                    declencheur += "\n\n" + ChangementDExercice.FaitApresLesSons;
                }
                // Un nombre était au tableau : il n'y sera plus (sa balise est retirée hors de l'exercice, défaut e8).
                if (!enLectureDeNombres && Services.Voix.LectureDesNombres.DernierMontre(recentsNombres) is not null)
                {
                    declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitFinDeLecture;
                }
                _logger.LogInformation(
                    "Changement d'exercice demande ({Nature}, conversation {ConversationId}).", changement, conversationId);
            }
            else if (!change && !controleOuvert && ChangementDExercice.DemandeRecente(
                historique.TakeLast(ChangementDExercice.MessagesDeSuite).Select(m => ((string)m.Role, (string?)m.Contenu, m.SonJuste)).ToList()))
            {
                declencheur += "\n\n" + ChangementDExercice.RappelDeSuite;
            }

            // L'HISTOIRE QU'IL A DEMANDÉE, JUSQU'À SES QUESTIONS — signalement #55 (Adam, 21/11/2026) : une seule question,
            // puis retour à la grammaire, et une réponse hors de l'histoire laissée sans correction. Pour tous les professeurs.
            if (!change && !controleOuvert && ChangementDExercice.HistoireEnCours(
                historique.TakeLast(ChangementDExercice.MessagesDHistoire).Select(m => ((string)m.Role, (string?)m.Contenu, m.SonJuste)).ToList()))
            {
                declencheur += "\n\n" + ChangementDExercice.RappelHistoire;
            }

            // LA RÈGLE DES TROIS ESSAIS, PARTOUT AILLEURS — Camara, le 11/11/2026, voir
            // `PromptsPedagogiques.RegleDesTroisEssais`. Pas dans un exercice de sons ou de lecture : il a sa propre
            // règle, comptée par le code (EssaisRates). Pas pendant un contrôle noté : on n'y touche pas.
            if (!enExerciceDeSons && !controleOuvert && !enLectureDeNombres)
            {
                declencheur += "\n\n" + PromptsPedagogiques.RegleDesTroisEssais;
            }

            // UN INDICE NE S'INVENTE PAS — contrôle d'Omar sur le signalement #52 (21/11/2026), voir
            // `PromptsPedagogiques.RegleDesIndices`. Commun à TOUS les professeurs et à tous les exercices, sons et lecture
            // compris (c'est en lecture des nombres que la règle inventée est apparue). Pas pendant un contrôle noté.
            if (!controleOuvert)
            {
                declencheur += "\n\n" + PromptsPedagogiques.RegleDesIndices;
            }

            // UNE RÉPONSE FAUSSE NE S'ADOUCIT PAS PAR UNE FAUSSE CONCESSION — signalement #66 (23/11/2026 : « a + b = c, ça
            // marche pour des longueurs quelconques mais pas pour un triangle rectangle »), voir
            // `PromptsPedagogiques.RegleDesReponsesFausses`. Commune à TOUS les professeurs, à chaque tour, juste après la
            // règle des indices ; pas pendant un contrôle noté (on n'y dit pas si c'est juste ou faux).
            if (!controleOuvert)
            {
                declencheur += "\n\n" + PromptsPedagogiques.RegleDesReponsesFausses;
            }

            // TOUTE QUESTION EST UNE PHRASE CORRECTE ET ENTIÈRE — Min's, le 21/11/2026, voir
            // `PromptsPedagogiques.RegleDesQuestionsCompletes`. Commune à TOUS les professeurs, à chaque tour (arrivée,
            // sons et lecture compris), dans le tour et pas dans le Noyau : le préfixe en cache ne change pas. Pas pendant
            // un contrôle noté. Le journal (Debug, sans contenu) dit à chaque tour si elle est partie.
            if (!controleOuvert)
            {
                declencheur += "\n\n" + PromptsPedagogiques.RegleDesQuestionsCompletes;
            }
            _logger.LogDebug(
                "Regle des questions completes (conversation {ConversationId}) : {Presente} (controle ouvert : {Controle}, accueil : {Accueil}).",
                conversationId, !controleOuvert, controleOuvert, typeAccueil);

            // LE TON DES TRACES ÉCRITES (13/11/2026) — voir PromptsPedagogiques.TonDesTracesEcrites. Aux tours où une
            // remarque ou un compte rendu s'écrit : pendant un contrôle (la remarque notée), dans une séance qui prépare un
            // contrôle ou une épreuve (le verdict « prêt »), et quand l'élève dit au revoir (le compte rendu).
            if (controleOuvert
                || contexte.Conversation.ModeSeance is ModesSeance.Controle or ModesSeance.Examen
                || LecteurAuRevoir.DitAuRevoir(contenu))
            {
                declencheur += "\n\n" + PromptsPedagogiques.TonDesTracesEcrites;
            }

            // LES NOMBRES À ÉCRIRE APRÈS LES AVOIR ENTENDUS (14/11/2026) — voir DicteeDeNombres : le rappel de la balise
            // [A_ECOUTER] part à chaque tour de la série. En maths, du CP à la 3e, hors contrôle noté et hors lecture
            // des nombres (là, c'est l'application qui montre le nombre).
            var dicteeDeNombres = !enLectureDeNombres && !controleOuvert
                && Services.Voix.LectureDesNombres.Concerne(contexte.Conversation.AgentSlug, contexte.Eleve.NiveauCode)
                && Services.Voix.DicteeDeNombres.EnCours(
                    historique.Where(m => m.Role == "assistant").TakeLast(2).Select(m => m.Contenu), contenu);

            // UNE RÉPONSE COURTE SE JUGE À NEUF (14/11/2026, Yann : « Pas tout à fait ! […] tu as raison » sur « Une plante
            // vivace ») — voir ReponseCourte. Le jumeau sans nombre de ReponseChiffree. Pas dans les exercices qui ont
            // leur propre verdict compté par le code (sons, lecture et dictée des nombres), ni pendant un contrôle noté.
            // Ni quand il demande à changer d'exercice : ce n'est pas une réponse à juger.
            if (!controleOuvert && !enExerciceDeSons && !enLectureDeNombres && !dicteeDeNombres && !change
                && !ReponseChiffree.Contient(contenu)
                && ReponseCourte.Concerne(
                    contenu,
                    historique.LastOrDefault(m => m.Role == "assistant"
                        && !string.IsNullOrWhiteSpace(m.Contenu)
                        && !m.Contenu!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                        && !m.Contenu.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))?.Contenu))
            {
                declencheur += "\n\n" + ReponseCourte.Rappel;
            }

            // LE MOT DIT EN FRANÇAIS N'EST PAS UNE RÉPÉTITION (21/11/2026, signalement #57 : « Blue. Tu répètes ? » —
            // « Bleu! » — « Oui, parfait ! ») — voir RepetitionEnLangue. Commun à TOUS les professeurs de langue : le code
            // reconnaît la traduction française du mot à répéter et le dit au professeur, au tour même. Pas pendant un
            // contrôle noté, ni quand il demande à changer d'exercice. Dans un exercice de sons, seulement à l'ÉCOUTE DES MOTS
            // TAPÉE (retest KO #57, 21/11/2026 : « [EN]Two[/EN]. Qu'as-tu entendu ? » — « Deux! » validé) : au micro, le relais
            // juge déjà (verdict faux), et la lecture en images ou la dictée ont leur propre juge.
            RepetitionEnLangue.Tour? repetitionEnLangue = null;
            // Son dernier essai sur ce mot (3e raté de suite) : on passe, le code ne le redit plus (filtre RedireLeMot).
            var repetitionAuDernierEssai = false;
            var messagesDuProfPourLaLangue = historique.Where(m => m.Role == "assistant")
                .Select(m => m.Contenu)
                .Where(c => !string.IsNullOrWhiteSpace(c)
                    && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                    && !c.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))
                .TakeLast(RepetitionEnLangue.MessagesLus)
                .ToList();
            var ecouteTapee = enExerciceDeSons && sonJuste is null
                && Services.Voix.RegleVerificationSon.DemandeCeQuIlAEntendu(messagesDuProfPourLaLangue.LastOrDefault() ?? string.Empty);
            // TAPÉE ET DÉJÀ JUGÉE avant d'être enregistrée (verdict « On réessaie » à l'écran, plus haut) : le même tour, et
            // l'essai COMPTE — au troisième raté de suite sur le même mot, réexplication puis passage (règle des trois essais,
            // contrôle d'Omar sur le #57 v2). Sans verdict (vérification des sons éteinte), le fait seul, comme avant.
            if (repetitionTapee is not null
                || (!controleOuvert && (!enExerciceDeSons || ecouteTapee) && !change
                    && RepetitionEnLangue.Concerne(contexte.Conversation.AgentSlug)))
            {
                var repetition = repetitionTapee ?? RepetitionEnLangue.Juger(
                    contexte.Conversation.AgentSlug,
                    messagesDuProfPourLaLangue,
                    contenu);
                repetitionEnLangue = repetition;
                if (repetition is not null)
                {
                    var essais = repetitionTapee is null ? 1 : RepetitionEnLangue.EssaisDeSuite(
                        contexte.Conversation.AgentSlug,
                        historique.Select(m => ((string?)m.Role, (string?)m.Contenu, m.SonJuste)).ToList(),
                        repetition);
                    repetitionAuDernierEssai = essais >= RepetitionEnLangue.EssaisMax;
                    declencheur += "\n\n" + (repetitionAuDernierEssai
                        ? RepetitionEnLangue.FaitDernierEssai(repetition)
                        : RepetitionEnLangue.Fait(repetition));
                    _logger.LogInformation(
                        "Repetition en langue : reponse en francais reconnue ({Nature}, {Balise}, attendu « {Attendu} », traduction « {Francais} », essai {Essai}, conversation {ConversationId}).",
                        repetition.Nature, repetition.Balise, repetition.Attendu, repetition.Francais, essais, conversationId);
                }
            }

            if (dicteeDeNombres)
            {
                declencheur += "\n\n" + Services.Voix.DicteeDeNombres.Rappel;

                // LE VERDICT, COMPTÉ PAR LE CODE (14/11/2026, signalement : 65 → « soixante-deux » validé). Le professeur
                // reçoit le fait ; il ne juge plus seul un nombre en lettres contre un nombre en chiffres.
                var messagesDuProf = historique.Where(m => m.Role == "assistant")
                    .Select(m => m.Contenu)
                    .Where(c => !string.IsNullOrWhiteSpace(c) && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                        && !c.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))
                    .TakeLast(4)
                    .ToList();
                var faitNombre = Services.Voix.DicteeDeNombres.FaitVerdict(messagesDuProf.LastOrDefault(), messagesDuProf, contenu);
                if (faitNombre is not null)
                {
                    declencheur += "\n\n" + faitNombre;
                    _logger.LogInformation("Nombre a ecrire juge (conversation {ConversationId}) : {Fait}", conversationId, faitNombre);
                }
            }

            // LES FORMES GÉOMÉTRIQUES (20/11/2026, signalement #48) — voir FormesAuTableau : le tableau sans dessin, le
            // verdict de « combien de côtés a un carré ? » et les 3 essais sans la réponse sont dits par le code. En maths,
            // du CP au CM2 seulement (contrôle d'Omar), hors contrôle noté et hors exercices qui ont leur propre verdict.
            FormesAuTableau.Tour? formesDuTour = null;
            if (!controleOuvert && !enExerciceDeSons && !enLectureDeNombres && !dicteeDeNombres && !change
                && FormesAuTableau.Concerne(contexte.Conversation.AgentSlug, contexte.Eleve.NiveauCode))
            {
                formesDuTour = FormesAuTableau.Lire(
                    historique.Where(m => !string.IsNullOrWhiteSpace(m.Contenu)
                            && (m.Role == "user"
                                || (m.Role == "assistant"
                                    && !m.Contenu!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                                    && !m.Contenu.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))))
                        .TakeLast(16)
                        .Select(m => (m.Role ?? string.Empty, (string?)m.Contenu))
                        .ToList(),
                    contenu);
                if (formesDuTour is not null)
                {
                    var faitFormes = FormesAuTableau.Fait(formesDuTour);
                    declencheur += "\n\n" + faitFormes;
                    _logger.LogInformation("Formes (conversation {ConversationId}) : {Fait}", conversationId, faitFormes);
                }
            }

            // LA LECTURE DES NOMBRES (12/11/2026) — voir LectureDesNombres : le code choisit le nombre de ce tour, le
            // professeur ne dit que son bravo ou son encouragement. Trois lectures fausses : on passe, sans la réponse.
            Services.Voix.LectureDesNombres.Tour? nombresDuTour = null;
            if (repriseNombres is not null)
            {
                // Le tour d'arrivée qui reprend la lecture (plus haut) : le même nombre, remontré par le code.
                nombresDuTour = repriseNombres;
                declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitType
                    + "\n\n" + (typeAccueil == TypeAccueil.FinProche
                        ? Services.Voix.LectureDesNombres.FaitRepriseApresAnnonce
                        : Services.Voix.LectureDesNombres.FaitReprise);
            }
            else if (enLectureDeNombres)
            {
                nombresDuTour = Services.Voix.LectureDesNombres.AMontrer(
                    contexte.Eleve.NiveauCode, recentsNombres, verdictNombre, $"{conversationId}|{historique.Count}", contenu);
                declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitType;
                if (nombresDuTour is { Demande: true }) declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitDemandeDe(nombresDuTour);
                if (nombresDuTour is { HorsCatalogue: { } horsCatalogue }) declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitHorsCatalogue(horsCatalogue);
                if (verdictNombre is not null)
                {
                    declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitVerdict(verdictNombre, nombresDuTour);
                }
            }
            else if (quitteLectureDeNombres && verdictNombre is not null)
            {
                // Il a lu, puis demandé une autre activité : le verdict de sa lecture, puis l'activité (contrôle d'Omar).
                declencheur += "\n\n" + Services.Voix.LectureDesNombres.FaitVerdictPuisChangement(verdictNombre);
            }

            if (langueDuSon is not null)
            {
                var enAnglais = langueDuSon == SchoolWebApp.Domain.Repositories.LanguesDesSons.Anglais;

                if (!enAnglais) declencheur += "\n\n" + PromptsPedagogiques.MarqueurSonsIsoles;

                // LA CARTE DES EXERCICES DE SONS (07/11/2026) : écouter ou lire, des syllabes ou des mots — voir LectureDImage.
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurChoixExercicesSons(enAnglais);

                // IL DEMANDE UN EXERCICE DE SONS SANS DIRE LEQUEL : c'est le code qui pose la carte, pas le professeur.
                poserLeChoix = Services.Voix.LectureDImage.SujetDeLaDemande(contenuPourLesSons);
                if (poserLeChoix is not null) declencheur += "\n\n" + Services.Voix.LectureDImage.FaitChoixAPoser(poserLeChoix);

                if (enExerciceDeSons)
                {
                    declencheur += "\n\n" + (enAnglais ? PromptsPedagogiques.MarqueurSonsAnglais : PromptsPedagogiques.RappelExerciceDeSons);

                    // L'EXERCICE CHOISI PAR L'ENFANT, REDIT À CHAQUE TOUR — voir TypeDEcoute.
                    var typeDEcoute = Services.Voix.RegleVerificationSon.TypeDEcoute(
                        filSons.Select(m => m.Contenu).Append(contenuPourLesSons));
                    if (Services.Voix.LectureDImage.EstLecture(typeDEcoute))
                    {
                        // LA LECTURE EN IMAGES (07/11/2026) : le code montre, le professeur ne dit rien à lire.
                        declencheur += "\n\n" + Services.Voix.LectureDImage.FaitType(typeDEcoute!, enAnglais);
                    }
                    else if (typeDEcoute is not null)
                    {
                        declencheur += "\n\n" + Services.Voix.RegleVerificationSon.FaitTypeDEcoute(
                            typeDEcoute, historique.TakeLast(MessagesPourExerciceDeSons).Select(m => m.Contenu),
                            contexte.Eleve.NiveauCode, enAnglais);
                    }

                    // LE MOT EN COURS, COMPARÉ ET COMPTÉ PAR LE CODE — voir DicteeDeMots.
                    etatDuMot = Services.Voix.DicteeDeMots.Lire(contenu, historique.Select(m => (m.Role, m.Contenu)).ToList());
                    if (etatDuMot is not null) declencheur += "\n\n" + Services.Voix.DicteeDeMots.Fait(etatDuMot, langueDuSon);
                }

                // CE QUE LA VOIX A DIT, quand elle a été vérifiée juste avant ce tour :
                // voir RegleVerificationSon. Rien dans tous les autres cas.
                if (_verificationSon.PrendreFait(contexte.Conversation.Id) is { } faitSon)
                {
                    declencheur += "\n\n" + faitSon;
                }

                // L'IMAGE QUE L'ENFANT VOIT AU TABLEAU, NOMMÉE PAR LE PROFESSEUR — Camara, le 07/11/2026 : voir ImageAuTableau.
                var questionPrecedente = historique.LastOrDefault(m => m.Role == "assistant" && !string.IsNullOrWhiteSpace(m.Contenu)
                    && !Services.Voix.ImageAuTableau.EstTechnique(m.Contenu))?.Contenu;
                imageAuTableau = Services.Voix.ImageAuTableau.Montree(
                    contenu, sonJuste, questionPrecedente, contexte.Eleve.NiveauCode, contexte.Conversation.MatiereCode);
                if (imageAuTableau is not null) declencheur += "\n\n" + Services.Voix.ImageAuTableau.Fait(imageAuTableau);

                // PAS D'IMAGE, PAS DE « COMME DANS… » — Camara, capture du 07/11/2026 : « jé », sans image, est devenu « comme
                // dans jéroboam… enfin comme dans le début de gentil ». Il imitait la formule des tours où l'image était là.
                // La lecture en images a son propre fait (l'image y est toujours) : « J'ai lu… » n'est pas concerné.
                else if (sonJuste == true && enExerciceDeSons && Services.Voix.LectureDImage.TypeDuMessage(contenu) is null) declencheur += "\n\n" + Services.Voix.ImageAuTableau.FaitSansImage;

                // TROIS ESSAIS, PAS PLUS — Camara, le 07/11/2026 : voir EssaisRates.
                if (enExerciceDeSons)
                {
                    abandon = Services.Voix.EssaisRates.Lire(
                        historique.Select(m => (m.Role, (string?)m.Contenu, m.SonJuste)).ToList(), sonJuste, contenu);
                    if (abandon is not null)
                    {
                        var typeDuTour = Services.Voix.RegleVerificationSon.TypeDEcoute(
                            filSons.Select(m => m.Contenu).Append(contenuPourLesSons));
                        // Son dernier essai était la traduction française du mot (« J'ai entendu le mot deux. » pour two, au
                        // micro ou tapé) : réexpliquer avant de passer (contrôle d'Omar sur le #57 v2).
                        // Ou sa répétition tapée, déjà reconnue comme la traduction (« J'ai répété : Bleu! ») : sans elle, dans une
                        // conversation que le code croit en exercice de sons, ce fait l'emportait sans réexplication (1/4 au bout en bout).
                        var francaisAbandon = repetitionEnLangue is { Nature: RepetitionEnLangue.Demande.Repeter } repete
                            && string.Equals(repete.Attendu, abandon.Son, StringComparison.OrdinalIgnoreCase)
                            ? repete.Francais
                            : sonJuste == false
                            ? RepetitionEnLangue.TraductionDite(langueDuSon, abandon.Son,
                                Services.Voix.RegleVerificationSon.ReponseDEcoute(contenu), messagesDuProfPourLaLangue)
                            : null;
                        declencheur += "\n\n" + Services.Voix.EssaisRates.Fait(abandon,
                            typeDuTour is Services.Voix.RegleVerificationSon.Syllabe or Services.Voix.LectureDImage.Syllabe
                            || (typeDuTour is null && Services.Voix.BanqueDeSons.Contient(abandon.Son)),
                            francaisAbandon);
                    }

                    // LA LECTURE EN IMAGES — Camara, le 07/11/2026 : « l'inverse de l'exercice des syllabes et des mots ».
                    // Ce que le tableau montre à ce tour est choisi ici ; le filtre l'écrit à la fin du message.
                    var typeDeLecture = Services.Voix.RegleVerificationSon.TypeDEcoute(
                        filSons.Select(m => m.Contenu).Append(contenuPourLesSons));
                    // PAS QUAND IL PART — Camara, le 11/11/2026 : « D'accord, pas de souci. À bientôt ! », puis « Regarde bien
                    // la syllabe en couleur, et essaie encore » ajouté par le code. Son au revoir n'est pas une lecture ratée.
                    if (Services.Voix.LectureDImage.EstLecture(typeDeLecture) && !LecteurAuRevoir.DitAuRevoir(contenu))
                    {
                        lectureDuTour = Services.Voix.LectureDImage.AMontrer(
                            typeDeLecture!,
                            langueDuSon == SchoolWebApp.Domain.Repositories.LanguesDesSons.Anglais,
                            contexte.Eleve.NiveauCode,
                            // Tout le fil, même d'avant un changement d'exercice : pour ne pas remontrer une image déjà lue.
                            historique.TakeLast(MessagesPourExerciceDeSons).Select(m => (string?)m.Contenu).ToList(),
                            sonJuste,
                            abandon?.Son,
                            contenuPourLesSons,
                            $"{conversationId}|{historique.Count}");
                    }
                }
            }

            // LA LECTURE À VOIX HAUTE, À CHAQUE TOUR DE L'EXERCICE — Camara, le 06/11/2026 : « il faut un exercice
            // de lecture pour que les enfants puissent lire ». Le texte au tableau est redit au professeur : il
            // l'avait soufflé de la 2e phrase à la 3e. Voir LectureAVoixHaute.
            if (Services.Voix.LectureAVoixHaute.Concerne(contexte.Eleve.NiveauCycle, contexte.Conversation.MatiereCode))
            {
                var duProfesseur = historique.Where(m => m.Role == "assistant").Select(m => (string?)m.Contenu).ToList();

                // IL A DEMANDÉ À CHANGER D'EXERCICE (14/11/2026, voir ChangementDExercice) : les « lis la phrase » d'avant sa
                // demande ne tiennent plus l'exercice ouvert. Sans demande, rien ne change.
                if (change) duProfesseur = [];
                else
                {
                    var derniers = historique.TakeLast(2 * Services.Voix.LectureAVoixHaute.MessagesSuivis)
                        .Select(m => (Role: (string)m.Role, Contenu: (string?)m.Contenu, m.SonJuste)).ToList();
                    var demande = ChangementDExercice.DerniereDemande(derniers);
                    if (demande >= 0)
                    {
                        duProfesseur = derniers.Skip(demande).Where(m => m.Role == "assistant").Select(m => m.Contenu).ToList();
                    }
                }

                if (Services.Voix.LectureAVoixHaute.EnCours(duProfesseur))
                {
                    declencheur += "\n\n" + Services.Voix.LectureAVoixHaute.Rappel(
                        Services.Voix.LectureAVoixHaute.TexteAuTableau(duProfesseur.TakeLast(Services.Voix.LectureAVoixHaute.MessagesSuivis)),
                        Services.Voix.LectureAVoixHaute.EnAnglais(contexte.Conversation.MatiereCode));
                }
            }

            if (PromptsPedagogiques.AnglaisInterdit(contexte.Conversation.ProfAvatar))
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurFrancaisSeul;
            }

            // LE GARDE-FOU DE LA FIGURE (11/11/2026) — voir LecteurFigure. En maths, à un tour d'élève seulement : ce que
            // l'écran n'a pas su dessiner revient au professeur ; sinon, la syntaxe lui est rappelée quand on parle de
            // géométrie. Le fait remplace le rappel : il porte déjà la syntaxe.
            if (typeAccueil == TypeAccueil.Aucun && LecteurFigure.EnMaths(contexte.Conversation.AgentSlug))
            {
                var dernierProfFigure = historique.LastOrDefault(m => m.Role == "assistant")?.Contenu;
                var nonDessinees = LecteurFigure.Verifiees(figureIncomprise, dernierProfFigure);

                if (nonDessinees.Count > 0)
                {
                    declencheur += "\n\n" + LecteurFigure.Fait(nonDessinees);
                    _logger.LogInformation(
                        "Figure incomprise renvoyee au professeur : {Lignes} ligne(s) (conversation {ConversationId}).",
                        nonDessinees.Count, conversationId);
                }
                else if (LecteurFigure.ParleDeGeometrie(contenu, dernierProfFigure))
                {
                    declencheur += "\n\n" + LecteurFigure.Rappel;
                }
            }

            // LE GENRE DU PROFESSEUR, À CHAQUE TOUR — voir MarqueurGenre : un
            // professeur qui « redevenait » une femme au fil d'une longue
            // conversation, malgré la règle du bloc d'identité.
            declencheur += "\n\n" + PromptsPedagogiques.MarqueurGenre(
                contexte.Conversation.ProfPrenom, contexte.Conversation.ProfAvatar);

            // LES HARAKAT, À CHAQUE TOUR DU COURS D'ARABE — Camara, le 04/10/2026 :
            // « j'ai toujours pas les harakat qui s'affichent ». La consigne de la
            // matière les demandait depuis une demi-heure ; le professeur a remis
            // au tableau un passage écrit plus tôt sans elles, recopié tel quel
            // depuis l'historique. Une règle ne vaut pas un fait : on le lui redit
            // au tour même, en nommant le cas de la recopie.
            if (string.Equals(contexte.Conversation.MatiereCode, "ARABE", StringComparison.OrdinalIgnoreCase))
            {
                declencheur += "\n\n[Rappel : tout mot arabe que tu écris à l'ardoise ou dans une balise [AR] "
                    + "porte TOUTES ses harakat — fatha, damma, kasra, soukoun, chadda, tanwîn : "
                    + "« اِسْمِي سَارَة », jamais « اسمي سارة ». Y compris quand tu recopies un passage déjà "
                    + "écrit plus haut sans elles : ajoute-les en le recopiant.]";
            }

            // LA VITESSE D'ÉCOUTE, MÊME CHEMIN QUE LE TEMPS : elle vit dans le
            // navigateur, elle voyage avec le tour de l'élève, et elle n'est
            // pas persistée. Sans elle, « plus lent » n'a pas de cran de
            // référence et « tu es déjà au plus lent » est indicible.
            if (!string.IsNullOrWhiteSpace(vitesseEcoute))
            {
                declencheur += "\n\n" + PromptsPedagogiques.MarqueurVitesse(vitesseEcoute);
            }

            // L'AU REVOIR APRÈS UN BILAN DÉJÀ FAIT — Camara, le 28/09/2026 : le
            // professeur redisait tout son bilan quand l'élève lui disait au
            // revoir juste après. Voir LecteurAuRevoir.
            if (typeAccueil == TypeAccueil.Aucun
                && (LecteurAuRevoir.DitAuRevoir(contenu) || LecteurAuRevoir.RemercieApresBilan(contenu))
                && LecteurAuRevoir.BilanDejaFait(
                    historique.LastOrDefault(m => m.Role == "assistant")?.Contenu))
            {
                declencheur += "\n\n" + LecteurAuRevoir.Marqueur;
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
            declencheur += await DeplierDicteesAuTableauAsync(historique, contexte.Eleve.Id, ct);

            // LES TOURNURES FAUTIVES DÉJÀ ENREGISTRÉES, corrigées dans ce que le
            // professeur relit — voir TournuresFautives : il imite ses propres
            // messages, et « je te réécoute l'histoire » revenait à chaque
            // reprise. Déterministe, donc sans effet sur le cache.
            // Et ses accords à lui, pour la même raison : « content de te
            // retrouver », écrit une fois, se recopie de séance en séance.
            var professeure = PromptsPedagogiques.EstUneProfesseure(contexte.Conversation.ProfAvatar);

            // Et ses apartés de régie (« (On garde 3e réponse fausse → …) ») : un aparté relu se recopie au tour suivant.
            // Déterministe aussi. Voir ApartesInternes.
            foreach (var message in historique.Where(m => m.Role == "assistant"))
            {
                message.Contenu = AccordDuProfesseur.Corriger(
                    TournuresFautives.Corriger(ApartesInternes.Retirer(message.Contenu)), professeure);
            }

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

            // LA DICTÉE EN CORRECTION REVIENT AU TABLEAU AVEC L'ÉLÈVE — Camara,
            // le 04/10/2026 : « la prof me demande de regarder ma copie mais
            // n'affiche rien au tableau ». L'arrivée efface le tableau, et le
            // professeur — qui connaît désormais les écarts par leur numéro — a
            // repris « l'erreur numéro 3 » de tête, devant un tableau vide.
            // Quand une correction était en cours, c'est l'APPLICATION qui
            // remet le tableau : une garantie d'usage ne se demande pas.
            string? tableauRepris = null;

            if (nouvelleArrivee && correctionDictee.Ouverte)
            {
                tableauRepris = correctionDictee switch
                {
                    { DicteeId: int idDictee } => $"\n[DICTEE_AU_TABLEAU]{idDictee}[/DICTEE_AU_TABLEAU]\n",
                    { Dicte: { } dicte, Copie: { } copie } => "\n" + LecteurDictee.TableauDeComparaison(dicte, copie) + "\n",
                    _ => null,
                };

                if (tableauRepris is not null) yield return tableauRepris;
            }

            // ET LA DICTÉE RESTÉE EN ATTENTE, de la même façon : après un départ,
            // la correction n'est plus « ouverte » dans l'historique, mais c'est
            // bien elle que le professeur va reprendre — voir DicteeEnAttenteAsync.
            if (nouvelleArrivee && tableauRepris is null && dicteeEnAttenteId is int idEnAttente)
            {
                tableauRepris = $"\n[DICTEE_AU_TABLEAU]{idEnAttente}[/DICTEE_AU_TABLEAU]\n";
                yield return tableauRepris;
            }

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

                    // LES FAITS DE LA CORRECTION PARTENT DÈS LE TOUR DE LA COPIE — signalement #58 : c'est dans
                    // CE message que le professeur commence « par l'erreur 1 », et la correction n'est pas encore
                    // « ouverte » dans l'historique (son dernier message porte le bloc [DICTEE]). Il expliquait donc
                    // l'erreur 1 sans ses numéros ni aucun fait : « cha » pour « chat » est devenu « tu l'entends,
                    // ce son à la fin, « cha-t » ? ».
                    var faits = FaitsDeCorrection(
                        texteDicte, copieTapee, contexte.Conversation.AgentSlug == AgentFrancais);
                    if (faits is not null) declencheur += "\n\n" + faits;
                    if (!correctionDictee.Ouverte) declencheur += "\n\n" + Services.Voix.VoixEnClair.Consigne;
                    yield return blocDicteeAjoute;
                }
            }

            // L'écran doit l'apprendre aussi : il garde la copie deux heures,
            // et la rouvrirait pour une dictée que le professeur déclare perdue.
            if (marquerAbandonDictee) yield return LecteurDictee.Abandon;

            // UNE DICTÉE AU TABLEAU POUR SA CORRECTION : ses mots ne se cachent plus (signalement #61, voir VoixEnClair).
            // Remise par l'application à l'arrivée, correction ouverte, ou copie tapée qui vient d'arriver. Ce n'est
            // pas un exercice de sons : la liste blanche ne s'applique pas (bout en bout du 22/11 : un historique qui
            // parlait de « sons » l'allumait à la reprise, et « chaton » devenait « pomme » ou « savon »).
            var dicteeAuTableau = (nouvelleArrivee && tableauRepris is not null)
                || correctionDictee.Ouverte
                || blocDicteeAjoute is not null;

            // PENDANT UNE ÉCOUTE, LE TEXTE LU NE PASSE PAS AU TABLEAU — voir
            // EcouteAuTableau. Le filtre n'existe que si une écoute est ouverte :
            // en dehors, le flux part tel quel, sans retenue ni retard.
            var filtreEcoute = ecoutesOuvertes.Count > 0
                ? new EcouteAuTableau.Filtre(ecoutesOuvertes)
                : null;

            // CE QUE LE PROFESSEUR ÉCRIT MAL, CORRIGÉ DANS LE FLUX — voir
            // CorrectionsDuFlux : ses accords (« content de te retrouver » dit par
            // Marine) et ses tournures fautives (« je te la réécoute »), avant
            // l'écran ET la voix.
            var filtreAccord = new CorrectionsDuFlux(
                PromptsPedagogiques.EstUneProfesseure(contexte.Conversation.ProfAvatar));

            // SES NOTES DE RÉGIE NE PARTENT PAS — voir ApartesInternes : « (On garde 3e réponse fausse → réexplication
            // douce + dernier essai) » arrivait à l'écran et dans la voix. Premier filtre du flux, chez TOUS les
            // professeurs ; seul le texte qui suit une parenthèse ouvrante est retenu, le temps de la fermer.
            var filtreApartes = new ApartesInternes.Filtre();

            // L'ÉCOUTE DES MOTS REPREND AU TOUR D'ARRIVÉE (retest d'Adam, #57, 21/11/2026, et e2e du 22/11/2026) : au retour
            // ou à l'annonce « cinq minutes », le dernier mot dit attend encore sa réponse. Marine jugeait une réponse qui
            // n'existait pas (« Juste ! C'est bien cat. », « Pas tout à fait ! »), citait le mot (« Ce mot, c'était le mot que
            // je viens de te dire. ») ou en dictait un autre. Le fait le lui dit, et le filtre redit le MÊME mot, sans verdict.
            // Comme pour la lecture des nombres (option rechargement, tâche 0140) : un [RAPPORT]/[FIN_SEANCE] ferme l'écoute
            // (« Quitter », séance conclue), sauf au RECHARGEMENT, où le worker de départ l'écrit juste après le mot.
            var rechargement = Services.Voix.LectureDesNombres.EstUnRechargement(typeAccueil, absenceApresSortie);
            var filSonsTextes = filSons.Select(m => m.Contenu ?? string.Empty)
                .Where(t => !rechargement || !Services.Voix.RegleVerificationSon.EstUneCloture(t)).ToList();
            var motAReprendre = typeAccueil is TypeAccueil.Retour or TypeAccueil.FinProche
                && enExerciceDeSons && !change && langueDuSon is not null && lectureDuTour is null && etatDuMot is null
                && poserLeChoix is null && !controleOuvert
                && Services.Voix.RegleVerificationSon.EcouteEnAttente(filSonsTextes)
                    ? Services.Voix.RegleVerificationSon.DernierMotDicte(filSonsTextes)
                    : null;
            if (motAReprendre is not null)
            {
                declencheur += "\n\n" + Services.Voix.RegleVerificationSon.FaitRepriseDeLEcoute(motAReprendre);
                _logger.LogInformation("Ecoute des mots reprise au tour d'arrivee ({Type}, conversation {ConversationId}).", typeAccueil, conversationId);
            }

            // IL PIOCHE DANS LA LISTE BLANCHE — voir FiltreMotsVerifies. Le français et l'anglais au
            // primaire, l'interrupteur allumé, et seulement dans un exercice de sons ou de syllabes :
            // ailleurs, pas de filtre, donc pas le moindre retard sur la voix.
            var filtreMots = (!enExerciceDeSons && poserLeChoix is null) || langueDuSon is null || dicteeAuTableau
                ? null
                : await _verificationSon.FiltrePourAsync(
                    langueDuSon,
                    // Le fil de l'exercice EN COURS : rien d'avant un changement d'exercice (voir ChangementDExercice).
                    filSons.Select(m => m.Contenu ?? string.Empty).ToList(),
                    conversationId + historique.Count,
                    ct,
                    supportConnu: Services.Voix.RegleVerificationSon.SupportSonConnu(contenu, historique.Select(m => m.Contenu)),
                    photoAttendue: contenu?.Contains(Services.Voix.RegleVerificationSon.MarqueurSonAuCahier, StringComparison.Ordinal) ?? false,
                    etat: etatDuMot,
                    retenirTout: contenu?.Contains(Services.Voix.RegleVerificationSon.MarqueurSonAuCahier, StringComparison.Ordinal) == true
                        || contenu?.Contains(Services.Voix.RegleVerificationSon.MarqueurSonAuClavier, StringComparison.Ordinal) == true,
                    niveau: contexte.Eleve.NiveauCode,
                    messageDeLEleve: contenuPourLesSons,
                    motGarde: imageAuTableau is { Anglais: true } ? imageAuTableau.Mot : null,
                    abandonne: abandon?.Son,
                    lecture: lectureDuTour,
                    poserLeChoix: poserLeChoix,
                    motDeLImage: imageAuTableau is { Syllabe: not null } ? imageAuTableau.Mot : null,
                    // APRÈS UN REFUS, LE MÊME MOT (retest KO #57) : voir RegleVerificationSon.EcouteDesMots. Toujours quand le code
                    // a refusé (verdict du micro faux, mot français reconnu) ; si rien n'est jugé, seulement quand il a répondu par
                    // un mot ou deux (« Orange! ») et que le professeur refuse sans rien valider. Jamais au 3e essai (abandon), ni
                    // quand il change d'exercice ou s'en va.
                    // Ni au dernier essai d'une répétition en langue (bout en bout de la tâche 1600 : « Red » remplacé par « Blue »
                    // redit, alors que le fait disait de passer). Au tour d'arrivée, le mot qui attend sa réponse reste (motAReprendre).
                    arrivee: motAReprendre is not null,
                    redire: motAReprendre is not null ? Services.Voix.RegleVerificationSon.RedireLeMot.Toujours
                        : change || abandon is not null || repetitionAuDernierEssai || sonJuste == true || LecteurAuRevoir.DitAuRevoir(contenu)
                        ? Services.Voix.RegleVerificationSon.RedireLeMot.Non
                        : sonJuste == false || repetitionEnLangue is not null
                            ? Services.Voix.RegleVerificationSon.RedireLeMot.Toujours
                            : Services.Voix.RegleVerificationSon.ReponseDEcoute(contenu) is not null
                                ? Services.Voix.RegleVerificationSon.RedireLeMot.SiRefus
                                : Services.Voix.RegleVerificationSon.RedireLeMot.Non);

            var filtreVoixEnClair = dicteeAuTableau ? new Services.Voix.VoixEnClair.Filtre() : null;

            // LA LECTURE DES NOMBRES : la réponse est retenue, puis remise en ordre (sa phrase, la consigne, la balise).
            var filtreNombres = nombresDuTour is null ? null : new Services.Voix.FiltreLectureNombres(nombresDuTour);

            // HORS DE LA LECTURE DES NOMBRES, PAS DE BALISE DE NOMBRE (défaut e8, Omar) : le modèle ne peut la recopier que
            // d'un historique qui en contient. Juste après l'exercice, sa réponse est retenue en entier (la consigne orpheline
            // part aussi) ; plus loin, seule une balise en cours d'écriture est retenue — voir FiltreBaliseNombreHorsLecture.
            var filtreBaliseNombre = nombresDuTour is null
                && historique.Any(m => m.Role == "assistant" && Services.Voix.LectureDesNombres.ContientUneBalise(m.Contenu))
                ? new Services.Voix.FiltreBaliseNombreHorsLecture(retenirTout: historique.TakeLast(MessagesPourExerciceDeSons)
                    .Any(m => m.Role == "assistant" && Services.Voix.LectureDesNombres.ContientUneBalise(m.Contenu)))
                : null;

            // LA RÉPONSE ATTENDUE NE PART PAS — voir ReponseNonDonnee (tâche 1941). Le filtre n'existe que si une question est
            // ouverte et que l'élève n'a pas trouvé : ailleurs, pas le moindre retard sur la voix. Dernier filtre du texte.
            var filtreReponse = reponseCherchee is { } cherchee ? new ReponseNonDonnee(cherchee.Valeur) : null;

            // DES CONSIGNES PLUTÔT QUE DES FILTRES — tâche 0000o (22/11/2026), décision du coordinateur selon la règle de Min's :
            // 4e tour de contrôle d'Omar où les filtres de PHRASES cassaient des phrases (« Ce que tu as dit, mais c'est le mot
            // français. », « Ce n'est pas encore, prends ton temps. », « Tu te souviens ? Et après 80 ? », « Tu répètes ? »
            // ajouté à une demande déjà là). FelicitationRefusee, DernierEssaiTropTot (retrait), AnalogieDeSuite (retrait) et
            // RepetitionEnLangue.SansBaliseEnDouble ne sont plus branchés : le professeur reçoit le FAIT au tour concerné, EN
            // DERNIER dans ses consignes (juste avant qu'il réponde). Restent dans le flux les règles dures seulement : réponse
            // jamais donnée (ReponseNonDonnee), suite gardée (SuiteGardee), mots vérifiés, tournures fautives.

            // UNE TRADUCTION REFUSÉE NE SE FÉLICITE PAS (contrôle d'Omar sur le lot groupé, point 5) : au tour où sa réponse est la
            // traduction française du mot (écoute au micro ou au clavier, répétition ou traduction en langue). Au dernier essai
            // (on passe à un autre mot), on ne lui redemande pas le mot.
            if ((sonJuste == false && Services.Voix.RegleVerificationSon.EstUneTraductionEntendue(contenu)) || repetitionEnLangue is not null)
            {
                declencheur += "\n\n" + RepetitionEnLangue.RappelTraductionRefuseeAuRang(
                    repetitionEnLangue?.Langue ?? (langueDuSon == SchoolWebApp.Domain.Repositories.LanguesDesSons.Anglais ? "anglais" : "langue étudiée"),
                    sensCompris: repetitionEnLangue?.Nature != RepetitionEnLangue.Demande.Traduire,
                    redemander: !repetitionAuDernierEssai && abandon is null,
                    // Le mot français qu'il a dit : la première phrase, sobre, que le code lui donne à dire.
                    mot: repetitionEnLangue is { } rep
                        ? (rep.Nature == RepetitionEnLangue.Demande.Traduire ? rep.Dit : rep.Francais)
                        : Services.Voix.RegleVerificationSon.MotDeLaTraduction(contenu),
                    // Le rang de ce refus (1, 2, 3…), lu dans ses messages d'avant : la première phrase change à chaque fois
                    // (contrôle d'Omar sur la v5, point 3 : la même phrase revenait mot pour mot à chaque refus).
                    rang: RepetitionEnLangue.RangDuRappel(messagesDuProfPourLaLangue));
            }

            // « DERNIER ESSAI » TROP TÔT AU RETOUR (décision de Min's du 22/11, 07h58 : au retour, le compte repart de zéro ;
            // conv. de test 1055 et 1089) : au tour d'arrivée d'un retour et à sa première réponse depuis, le fait du code. Pendant
            // un calcul ou une suite, le rang compté par le code a déjà son fait (CalculAuTableau.FaitRangDeLErreur), et aux 1re et
            // 2e erreurs le rappel court, en dernier (bout en bout #53 ×10 sur dc8c86b : « dernier essai » au 2e essai 3 fois sur 10).
            if (rangDeLErreur is 1 or 2)
            {
                declencheur += "\n\n" + CalculAuTableau.RappelPasEncoreDernierEssai;
            }
            else if (rangDeLErreur is null && (DernierEssaiTropTot.AuRetour(typeAccueil)
                || DernierEssaiTropTot.TropTotApresLeRetour(historique.Select(m => ((string?)m.Role, (string?)m.Contenu)).ToList())))
            {
                declencheur += "\n\n" + DernierEssaiTropTot.FaitAuRetour;
            }
            // AU TOUR D'ARRIVÉE, IL N'Y A RIEN À JUGER (contrôle d'Omar sur la v5, point 2 ; QR1, conv. de test 1272) : l'accueil
            // du retour recopiait « Bleu, c'est le mot en français : tu as compris le sens. » de l'historique. En dernier.
            if (DernierEssaiTropTot.AuRetour(typeAccueil))
            {
                declencheur += "\n\n" + DernierEssaiTropTot.FaitRienReponduAuRetour;
            }

            // PENDANT UNE SUITE NON TROUVÉE, ON NE CHANGE PAS D'EXERCICE — voir SuiteGardee (contrôle d'Omar sur le lot groupé v3,
            // point 6 : « on y reviendra… un autre petit exercice », « On passe à autre chose ! Qu'est-ce qui vient juste avant
            // 10 ? [ARDOISE]9, 10[/ARDOISE] » à la 3e erreur, 2 séries sur 10). Aux 1re-3e erreurs d'une suite seulement : la
            // réponse est retenue en entier, puis rendue telle quelle (avec la question du code à la fin si elle n'en posait
            // aucune) ou remplacée par le message du code (abandon, autre question, réponse donnée). Jamais de phrase coupée.
            var messagesDeLaSuite = historique.Where(m => m.Role == "assistant")
                .Select(m => m.Contenu)
                .Where(c => !string.IsNullOrWhiteSpace(c)
                    && !c!.TrimStart().StartsWith("[FICHE]", StringComparison.OrdinalIgnoreCase)
                    && !c.TrimStart().StartsWith("[RAPPORT]", StringComparison.OrdinalIgnoreCase))
                .TakeLast(CalculAuTableau.MessagesRemontes)
                .ToList();
            var gardeSuite = reponseCherchee is { } suiteGardee && SuiteGardee.AGarder(rangDeLErreur)
                && CalculAuTableau.EstUneSuite(messagesDeLaSuite)
                ? new SuiteGardee.Filtre(messagesDeLaSuite, suiteGardee.Question, suiteGardee.Valeur, rangDeLErreur!.Value)
                : null;

            // LE TEXTE BRUT DU MODÈLE, AU JOURNAL DE MISE AU POINT — contrôle d'Omar sur le lot groupé v2 (points 7 et 8) : pour
            // savoir quel filtre a retiré quoi, le texte tel que le modèle l'a écrit, avant tout filtre. Niveau Debug seulement
            // (jamais en production, où le journal est au niveau Information) ; c'est le texte du professeur, pas de l'élève.
            var texteBrut = _logger.IsEnabled(LogLevel.Debug) ? new System.Text.StringBuilder() : null;

            await foreach (var fragment in _agent.RepondreAsync(
                contexte.Eleve, contexte.Conversation, historique, declencheur, bilan,
                TypeTache.Dialogue, typeAccueil, depuis, secondesRestantes,
                parMessage, piecesDuTour, enExerciceDeSons, ct))
            {
                texteBrut?.Append(fragment.Texte);
                var accorde = filtreAccord.Passer(filtreApartes.Passer(fragment.Texte));
                if (gardeSuite is not null) accorde = gardeSuite.Passer(accorde);
                if (filtreMots is not null) accorde = filtreMots.Passer(accorde);
                if (filtreVoixEnClair is not null) accorde = filtreVoixEnClair.Passer(accorde);
                if (filtreNombres is not null) accorde = filtreNombres.Passer(accorde);
                if (filtreBaliseNombre is not null) accorde = filtreBaliseNombre.Passer(accorde);
                if (filtreReponse is not null) accorde = filtreReponse.Passer(accorde);
                if (accorde.Length == 0) continue;

                if (filtreEcoute is null)
                {
                    yield return accorde;
                    continue;
                }

                var passe = filtreEcoute.Passer(accorde);
                if (passe.Length > 0) yield return passe;
            }

            var finAccordee = filtreAccord.Passer(filtreApartes.Terminer()) + (filtreAccord.Terminer() ?? string.Empty);
            if (gardeSuite is not null)
            {
                finAccordee = gardeSuite.Passer(finAccordee) + gardeSuite.Terminer();
                if (gardeSuite.Remplacement is not null)
                {
                    _logger.LogInformation("Suite gardee : la reponse quittait la question (erreur numero {Rang}), message du code (conversation {ConversationId}).",
                        rangDeLErreur, conversationId);
                }
                else if (gardeSuite.QuestionAjoutee)
                {
                    _logger.LogInformation("Suite gardee : la reponse ne posait pas de question (erreur numero {Rang}), question du code ajoutee (conversation {ConversationId}).",
                        rangDeLErreur, conversationId);
                }
            }
            if (texteBrut is not null)
            {
                _logger.LogDebug("Texte brut du professeur (conversation {ConversationId}) : {Brut}",
                    conversationId, texteBrut.ToString().ReplaceLineEndings("⏎"));
            }
            if (filtreMots is not null)
            {
                finAccordee = filtreMots.Passer(finAccordee) + filtreMots.Terminer();

                if (filtreMots.ARemplace)
                {
                    _logger.LogInformation("Mot hors liste remplace (conversation {ConversationId}).", conversationId);
                }
            }

            if (filtreVoixEnClair is not null)
            {
                finAccordee = filtreVoixEnClair.Passer(finAccordee) + filtreVoixEnClair.Terminer();

                if (filtreVoixEnClair.ARemplace)
                {
                    _logger.LogInformation(
                        "Dictee au tableau : balise de voix [FR] remise en clair (conversation {ConversationId}).", conversationId);
                }
            }

            if (filtreNombres is not null)
            {
                finAccordee = filtreNombres.Passer(finAccordee) + filtreNombres.Terminer();

                // UNE PHRASE ENTIÈREMENT RETIRÉE (défaut e5, Omar) : il ne restait que la consigne du code. Le journal dit
                // quelles règles l'ont retirée, sans contenu. Le texte brut : seulement hors production, drapeau de
                // configuration allumé, niveau Debug, et prénom de l'enfant masqué (voir _texteBrutAuJournal).
                if (filtreNombres.PhraseRetiree)
                {
                    _logger.LogInformation(
                        "Lecture des nombres : phrase du professeur entierement retiree ({Regles}) (conversation {ConversationId}).",
                        string.Join(", ", filtreNombres.Retraits), conversationId);
                }
                if (_texteBrutAuJournal && _logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Lecture des nombres, texte brut du professeur (conversation {ConversationId}) : {Brut} || retraits : {Regles}",
                        conversationId,
                        Services.Voix.LectureDesNombres.SansPrenom(filtreNombres.Brut, contexte.Eleve.Prenom).ReplaceLineEndings(" / "),
                        string.Join(", ", filtreNombres.Retraits));
                }
            }

            if (filtreBaliseNombre is not null)
            {
                finAccordee = filtreBaliseNombre.Passer(finAccordee) + filtreBaliseNombre.Terminer();
            }

            if (filtreReponse is not null)
            {
                finAccordee = filtreReponse.Passer(finAccordee) + filtreReponse.Terminer();
            }

            if (finAccordee.Length > 0)
            {
                var passe = filtreEcoute is null ? finAccordee : filtreEcoute.Passer(finAccordee);
                if (passe.Length > 0) yield return passe;
            }

            if (filtreEcoute?.Terminer() is { Length: > 0 } finDeFlux) yield return finDeFlux;

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

            // UNE COPIE DÉJÀ EN COURS, INCOMPLÈTE — Camara, le 26/09/2026. Le
            // professeur qui dit « envoie-moi ta copie » après avoir reçu
            // l'énoncé ne rouvre pas une demande : la carte est déjà là, avec
            // ses coches. Ni le filet ne rajoute le bloc, ni le bloc écrit par
            // le professeur n'est gardé — il effaçait ce qui était reçu.
            var copieOuverte = await CopieOuverteAsync(
                contexte.Eleve.Id, contexte.Conversation.MatiereId, ct);

            if (controleRecent is not null && bilan.Task.IsCompletedSuccessfully
                && copieOuverte?.Id != controleRecent.Id)
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

            // UN RETRAIT VOULU SANS LE OUI DE L'ÉLÈVE NE SE FAIT PAS, ET ÇA SE DIT DANS LE MÊME TOUR — #67 v5 (verdict
            // d'Omar, v4R-3 : « 1. Cinétique chimique » à l'ardoise alors que la fiche en gardait deux, jusqu'à la fin). Le
            // professeur a écrit `retirer:` (ou a dit « je la retire ») : rien n'est parti, le serveur ajoute à la réponse
            // affichée la phrase vraie et la question fermée, à laquelle un oui au tour suivant retire la notion.
            string? retraitAjoute = null;
            if (mode == ModesSeance.Controle && controleMode is int controleDuRetrait && !accueil
                && bilan.Task.IsCompletedSuccessfully)
            {
                retraitAjoute = RetraitNonFaitDuTour((await bilan.Task).TexteComplet, notionsDuTour);

                if (retraitAjoute is not null)
                {
                    _logger.LogWarning(
                        "Retrait sans confirmation sur le controle {ControleId} (conversation {ConversationId}) : rien retire, "
                        + "question fermee ajoutee a la reponse.", controleDuRetrait, conversationId);

                    yield return retraitAjoute;
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
                // L'en-tête d'une arrivée : la gomme, et le tableau que
                // l'application a remis elle-même quand une correction de
                // dictée était en cours — voir plus haut, `tableauRepris`.
                var enTete = nouvelleArrivee ? TableauEfface + (tableauRepris ?? string.Empty) : string.Empty;

                var contenuPersiste = enTete + resultat.TexteComplet;

                // Le tableau écrit par l'application rejoint le texte persisté,
                // devant la réponse — et si le professeur en a écrit un quand
                // même, le sien part : celui de l'application fait foi.
                if (blocDicteeAjoute is not null)
                {
                    contenuPersiste = enTete
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
                        contenuPersiste = enTete + sansDoublon;

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

                if (copieOuverte is not null
                    && LecteurCopieControle.LireDemande(contenuPersiste) == copieOuverte.Id)
                {
                    contenuPersiste = LecteurCopieControle.SansDemande(contenuPersiste, copieOuverte.Id);

                    _logger.LogInformation(
                        "Demande de copie repetee pendant la copie en cours du controle {ControleId} : "
                        + "bloc retire (conversation {ConversationId}).", copieOuverte.Id, conversationId);
                }

                // La phrase vraie du retrait non fait rejoint le texte PERSISTÉ (#67 v5) : c'est elle, relue au tour
                // suivant, qui porte la question fermée qu'un oui confirme.
                if (retraitAjoute is not null) contenuPersiste += retraitAjoute;

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
                // Rien de ce que l'application souffle au professeur ne reste
                // dans sa bouche — voir LecteurDictee.SansNoteApplication.
                contenuPersiste = LecteurDictee.SansNoteApplication(contenuPersiste);

                // Ni un aparté de régie : même filtre que le flux, donc le même texte que l'écran et la voix. Un repère
                // au journal, sans contenu ; le texte de l'aparté seulement hors production, drapeau allumé, en Debug.
                var texteAvantApartes = contenuPersiste;
                contenuPersiste = ApartesInternes.Retirer(contenuPersiste, out var apartesRetires);
                if (apartesRetires > 0 || filtreApartes.Retires > 0)
                {
                    _logger.LogInformation(
                        "Aparte de regie retire du message du professeur ({Nombre}) (conversation {ConversationId}).",
                        Math.Max(apartesRetires, filtreApartes.Retires), conversationId);

                    if (_texteBrutAuJournal && _logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Aparte de regie, texte brut du professeur (conversation {ConversationId}) : {Brut}",
                            conversationId,
                            Services.Voix.LectureDesNombres.SansPrenom(texteAvantApartes, contexte.Eleve.Prenom).ReplaceLineEndings(" / "));
                    }
                }

                // Et une tournure fautive ne reste pas dans l'historique, où il
                // la reprendrait à la séance suivante — voir TournuresFautives.
                contenuPersiste = TournuresFautives.Corriger(contenuPersiste);

                // Le message du code a remplacé celui du professeur à l'écran : l'historique garde le même. Sinon, la question du
                // code ajoutée à l'écran l'est aussi ici (même méthode, SuiteGardee.AvecLaQuestion).
                if (gardeSuite?.Remplacement is { } remplacement) contenuPersiste = enTete + remplacement;
                else if (gardeSuite?.QuestionAjoutee == true && reponseCherchee is { } suiteRelancee)
                    contenuPersiste = SuiteGardee.AvecLaQuestion(contenuPersiste, suiteRelancee.Question);

                // L'historique dit ce que l'élève a vu et entendu : le mot remplacé n'y revient pas.
                if (filtreMots is not null) contenuPersiste = filtreMots.Corriger(contenuPersiste);

                // Les mots de la dictée au tableau, en clair comme à l'écran (signalement #61) : une page rechargée ne
                // montre pas de blanc, et l'historique relu ne lui réapprend pas la balise.
                if (filtreVoixEnClair is not null) contenuPersiste = Services.Voix.VoixEnClair.Corriger(contenuPersiste);

                if (filtreNombres is not null)
                {
                    // La gomme d'une arrivée (lecture des nombres reprise au retour) reste devant : la réécriture retire tout
                    // geste de tableau, et sans elle une page rechargée verrait resurgir le tableau d'avant le retour.
                    var gomme = enTete.Length > 0 && contenuPersiste.StartsWith(enTete, StringComparison.Ordinal) ? enTete : string.Empty;
                    contenuPersiste = gomme + filtreNombres.Corriger(contenuPersiste[gomme.Length..]);
                }

                // Hors de l'exercice, une balise de nombre ne reste pas en base : au tour suivant, la lecture des nombres
                // reprendrait sur ce nombre (le dernier message en montre un), au clavier comme au micro (défaut e8).
                if (nombresDuTour is null)
                {
                    contenuPersiste = Services.Voix.LectureDesNombres.SansBaliseHorsLecture(contenuPersiste, out var baliseRetiree);
                    if (baliseRetiree || filtreBaliseNombre?.ARetire == true)
                    {
                        _logger.LogInformation(
                            "Balise de nombre ecrite hors de la lecture des nombres : retiree (conversation {ConversationId}).",
                            conversationId);
                    }
                }

                // La réponse qu'il cherche encore ne reste pas non plus : l'historique dit ce que l'élève a vu et entendu, et
                // un « 7 + 5 = 12 » relu au tour suivant se recopierait. Un repère au journal, sans contenu (tâche 1941).
                if (filtreReponse is not null && reponseCherchee is { } protegee)
                {
                    contenuPersiste = ReponseNonDonnee.Retirer(contenuPersiste, protegee.Valeur, out var reponsesRetirees);
                    if (reponsesRetirees > 0 || filtreReponse.Retraits > 0)
                    {
                        _logger.LogInformation(
                            "Reponse attendue retiree du message du professeur ({Nombre}, question {Question}) (conversation {ConversationId}).",
                            Math.Max(reponsesRetirees, filtreReponse.Retraits), protegee.Question, conversationId);
                    }
                }

                // Ses accords aussi — déjà faits dans le flux, refaits ici pour
                // ce qui n'y passe pas (un bloc écrit par l'application).
                // UN REPÈRE DANS LE JOURNAL, SANS CONTENU : c'est ainsi qu'on retrouvera un cas comme celui de la maman.
                if (PromptsPedagogiques.AnglaisInterdit(contexte.Conversation.ProfAvatar) && PromptsPedagogiques.SembleEnAnglais(contenuPersiste))
                {
                    _logger.LogWarning("Reponse en anglais d'un professeur sans droit a l'anglais (conversation {ConversationId}).", conversationId);
                }

                contenuPersiste = AccordDuProfesseur.Corriger(
                    contenuPersiste, PromptsPedagogiques.EstUneProfesseure(contexte.Conversation.ProfAvatar));

                // Le texte d'une écoute ouverte ne reste pas au tableau non plus :
                // une page rechargée ne doit pas le faire réapparaître.
                contenuPersiste = EcouteAuTableau.Avancer([.. ecoutesOuvertes], contenuPersiste);

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

                    // QUI L'A DIT — pour que le coût de la voix suive la bascule
                    // OpenAI / Gemini de l'administration, réponse par réponse.
                    ModeleVoix = await ModeleVoixAsync(ct),
                    DateCreation = DateTime.UtcNow
                });

                // UNE DEMANDE DE COPIE — écrite par le professeur ou ajoutée par
                // le filet — repart de zéro : la question sera reposée.
                if (LecteurCopieControle.LireDemande(contenuPersiste) is int demandeCopie)
                {
                    try
                    {
                        await _controles.OuvrirCopieAsync(
                            contexte.Eleve.Id, demandeCopie, contexte.Conversation.MatiereId,
                            DateTime.UtcNow - FenetreCopie, ct);
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

                // LE FAIT DES FORMES A-T-IL SUFFI ? (20/11/2026, signalement #48) — grep « Reponse de forme donnee trop tot ».
                if (formesDuTour is not null && FormesAuTableau.DonneeTropTot(formesDuTour, resultat.TexteComplet))
                {
                    _logger.LogWarning(
                        "Reponse de forme donnee trop tot (conversation {ConversationId}, essai {Essai}) : {Debut}",
                        conversationId, formesDuTour.Essai,
                        resultat.TexteComplet.Length > 160 ? resultat.TexteComplet[..160] : resultat.TexteComplet);
                }

                // « PAS TOUT À FAIT ! […] TU AS RAISON » (14/11/2026) — relevé pour savoir si le rappel suffit : grep
                // « Ouverture contredite » dans docker logs mimia.
                if (ReponseCourte.SeContredit(resultat.TexteComplet))
                {
                    _logger.LogWarning(
                        "Ouverture contredite (conversation {ConversationId}) : {Debut}",
                        conversationId,
                        resultat.TexteComplet.Length > 160 ? resultat.TexteComplet[..160] : resultat.TexteComplet);
                }

                // LE VERDICT DU CODE A-T-IL SUFFI ? (20/11/2026, signalement #29) — grep « Verdict de calcul contredit ».
                if (verdictCalcul is not null && CalculAuTableau.Contredit(verdictCalcul, resultat.TexteComplet))
                {
                    _logger.LogWarning(
                        "Verdict de calcul contredit (conversation {ConversationId}, {Jugement} : {Question} -> {Donne}) : {Debut}",
                        conversationId, verdictCalcul.Jugement, verdictCalcul.Question, verdictCalcul.Donne,
                        resultat.TexteComplet.Length > 160 ? resultat.TexteComplet[..160] : resultat.TexteComplet);
                }

                await EnregistrerComprehensionOraleAsync(
                    conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                    contexte.Eleve.Age, filtreMots?.Corriger(resultat.TexteComplet) ?? resultat.TexteComplet, ct);

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

                // LE PROJET D'ORIENTATION, CHEZ LA CONSEILLÈRE SEULEMENT : un
                // professeur qui écrirait [PROJET] n'a pas eu l'accord de l'élève.
                if (ConseillereOrientation.Est(contexte.Conversation.MatiereCode))
                {
                    await EnregistrerProjetAsync(
                        contexte.Eleve.Id, contexte.Eleve.NiveauScolaireId, contexte.Eleve.Lv2Espagnol,
                        contexte.Eleve.Specialites, resultat.TexteComplet, ct);

                    await EnregistrerRapportOrientationAsync(
                        conversationId, contexte.Eleve.Id, resultat.TexteComplet, ct);
                }

                // LE PÉRIMÈTRE ET LE VERDICT D'UN CONTRÔLE NE S'ÉCRIVENT QUE
                // DANS SA SÉANCE DE PRÉPARATION, ET POUR LUI SEUL. Un bloc écrit
                // ailleurs — en cours normal, ou sur un autre numéro — est
                // ignoré : c'est le code qui tient le cloisonnement, pas la
                // mémoire du modèle.
                //
                // ET PAS TANT QUE L'ENFANT N'A RIEN RÉPONDU DANS CETTE
                // PRÉPARATION (retest #54 d'Adam, 21/11/2026 : carte d'un
                // contrôle neuf à 60 % « Pas encore prêt » sans une réponse —
                // verdict du tour d'arrivée ou de la conclusion de départ). Le
                // programme posé avant — lu dans son cours, par exemple — entre
                // au périmètre, mais ses notions ne sont pas « travaillées » et
                // aucun verdict ne s'écrit. Voir VerdictsDuTour.
                if (mode == ModesSeance.Controle)
                {
                    var aRepondu = await EnfantARepondu(
                        contexte.Eleve.Id, controleMode!.Value, nouvelleArrivee, typeAccueil, historique, ct);

                    await EnregistrerControleNotionsAsync(
                        conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                        contexte.Eleve.NiveauScolaireId, controleMode.Value, resultat.TexteComplet, ct,
                        travaillee: aRepondu);

                    if (aRepondu)
                    {
                        await EnregistrerControlePretAsync(
                            conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                            contexte.Eleve.NiveauScolaireId, controleMode.Value, resultat.TexteComplet, ct);
                    }
                    else if (LecteurControlePret.Lire(resultat.TexteComplet) is not null)
                    {
                        _logger.LogWarning(
                            "Verdict ignore : l'eleve n'a rien repondu dans cette preparation "
                            + "(controle {ControleId}, conversation {ConversationId}, tour {Type}).",
                            controleMode.Value, conversationId, typeAccueil);
                    }
                }

                // Le résultat, lui, est un fait rapporté par l'élève : il
                // s'enregistre d'où qu'il vienne. Le dépôt revérifie le contrôle.
                await EnregistrerControleResultatAsync(
                    conversationId, contexte.Eleve.Id, contexte.Conversation.MatiereId,
                    contexte.Eleve.NiveauScolaireId, resultat.TexteComplet, ct);

                // L'épreuve n'a pas de date d'ouverture lue ici : seule la règle
                // du tour d'arrivée s'applique (personne n'a encore parlé).
                if (mode == ModesSeance.Examen
                    && VerdictsDuTour.EnfantARepondu(nouvelleArrivee, typeAccueil == TypeAccueil.Aucun, null, []))
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

            await ImputerQuotaAsync(contexte.Eleve.Id, conversationId, historique.LastOrDefault()?.DateCreation, ct);
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
        /// LE RAPPORT D'ORIENTATION de la séance (11/10/2026) — voir
        /// <c>LecteurRapportOrientation</c>. Adressé à l'élève, lu aussi par le
        /// parent. Silencieux en cas d'échec, comme les autres blocs.
        /// </summary>
        private async Task EnregistrerRapportOrientationAsync(
            int conversationId, int eleveId, string texte, CancellationToken ct)
        {
            var rapport = LecteurRapportOrientation.Lire(texte)
                ?? DepuisLeRapportDeCours(LecteurRapport.Lire(texte));
            if (rapport is null) return;

            try
            {
                await _rapportsOrientation.EnregistrerAsync(eleveId, conversationId, rapport, ct);
                _logger.LogInformation(
                    "Rapport d'orientation enregistre pour l'eleve {EleveId} (conversation {ConversationId}).",
                    eleveId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Echec de l'enregistrement du rapport d'orientation (conversation {ConversationId}).",
                    conversationId);
            }
        }

        /// <summary>
        /// LE FILET : la conseillère a conclu avec son [RAPPORT] de séance mais
        /// sans [RAPPORT_ORIENTATION] (11/10/2026). Plutôt qu'une séance sans
        /// trace pour l'élève, un rapport tiré de SES mots : ce qui a été
        /// abordé, son observation, ce qui reste à voir. Rien d'inventé.
        /// </summary>
        private static RapportOrientationEleve? DepuisLeRapportDeCours(RapportDeclare? cours)
        {
            if (cours is null || (string.IsNullOrWhiteSpace(cours.Remarque) && string.IsNullOrWhiteSpace(cours.Travaille)))
            {
                return null;
            }

            return new RapportOrientationEleve
            {
                Discussion = string.IsNullOrWhiteSpace(cours.Remarque) ? cours.Travaille : cours.Remarque,
                Etapes = cours.ARevoir,
            };
        }

        /// <summary>
        /// LE PROJET D'ORIENTATION, noté par la conseillère avec l'accord de
        /// l'élève (11/10/2026) — voir <c>LecteurProjet</c>. Il remplace le
        /// précédent ; « but: retiré » l'efface.
        ///
        /// CHAQUE MATIÈRE EST UN MOT rapproché des matières de l'élève. Une ligne
        /// qu'on ne reconnaît pas est ÉCARTÉE (repli à -1) : la ranger dans une
        /// matière au hasard la ferait lire au mauvais professeur. Silencieux en
        /// cas d'échec, comme les autres blocs.
        /// </summary>
        private async Task EnregistrerProjetAsync(
            int eleveId, int? niveauScolaireId, bool lv2Espagnol, IReadOnlyCollection<string> specialites,
            string texte, CancellationToken ct)
        {
            var declare = LecteurProjet.Lire(texte);
            if (declare is null) return;

            try
            {
                if (declare.Retire)
                {
                    await _eleves.DefinirProjetOrientationAsync(eleveId, null, ct);
                    _logger.LogInformation("Projet d'orientation retire pour l'eleve {EleveId}.", eleveId);
                    return;
                }

                var matieres = (await MatieresDeLEleveAsync(niveauScolaireId, lv2Espagnol, specialites))
                    .Where(m => !ConseillereOrientation.Est(m.Code))
                    .ToList();

                var lignes = new List<LigneProjetOrientation>();
                foreach (var (mot, contenu) in declare.Matieres)
                {
                    var id = ResolveurMatiereDeclaree.Resoudre(mot, matieres, -1);
                    if (id < 0 || lignes.Any(l => l.MatiereId == id)) continue;

                    lignes.Add(new LigneProjetOrientation
                    {
                        MatiereId = id,
                        MatiereLibelle = matieres.First(m => m.Id == id).Libelle ?? mot,
                        Texte = contenu,
                    });
                }

                await _eleves.DefinirProjetOrientationAsync(eleveId, new ProjetOrientation
                {
                    Objectif = declare.Objectif!,
                    Matieres = lignes,
                    Le = DateTime.UtcNow,
                }, ct);

                _logger.LogInformation(
                    "Projet d'orientation note pour l'eleve {EleveId} : {Lignes} matiere(s) sur {Declarees}.",
                    eleveId, lignes.Count, declare.Matieres.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echec de l'enregistrement du projet d'orientation de l'eleve {EleveId}.", eleveId);
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
        /// L'enfant a-t-il répondu depuis l'ouverture de CETTE préparation ?
        /// Voir VerdictsDuTour. L'ouverture est relue en base (posée à
        /// l'arrivée par MarquerPreparationControleAsync) ; si elle est
        /// illisible, on ne bloque rien.
        /// </summary>
        private async Task<bool> EnfantARepondu(
            int eleveId, int controleId, bool tourDArrivee, TypeAccueil typeAccueil,
            IReadOnlyList<DomainMessage> historique, CancellationToken ct)
        {
            DateTime? ouverture = null;

            if (!tourDArrivee && typeAccueil != TypeAccueil.Aucun)
            {
                try
                {
                    ouverture = (await _controles.GetAsync(eleveId, controleId, ct))?.DernierePreparationLe;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Ouverture de la preparation {ControleId} illisible.", controleId);
                }
            }

            return VerdictsDuTour.EnfantARepondu(
                tourDArrivee,
                tourDeLEleve: typeAccueil == TypeAccueil.Aucun,
                ouverture,
                historique.Select(m => ((string?)m.Role, m.DateCreation)));
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
            int conversationId, int eleveId, int matiereId, int? niveauScolaireId, int controleCible,
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
                // Une notion jugée hors du programme du contrôle y entre
                // d'abord — sinon son état serait perdu et la barre ne
                // verrait que le tenu. Voir CompleterPerimetreAsync.
                var notions = declare.Notions;

                if (notions is { Count: > 0 })
                {
                    try
                    {
                        notions = await _planificateur.CompleterPerimetreAsync(
                            eleveId, declare.Controle, matiereId, niveauScolaireId, notions, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Notions du verdict non ajoutees au programme (controle {ControleId}).", declare.Controle);
                    }
                }

                var pose = await _controles.PoserVerdictPretAsync(
                    eleveId, declare.Controle, matiereId,
                    declare.Verdict, declare.Observation, DateTime.UtcNow, notions, ct);

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
                    eleveId, epreuve.Id, matiereId, declare.Verdict, declare.Observation, DateTime.UtcNow,
                    declare.Notions, ct);

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
            int? niveauScolaireId, int controleCible, string texte, CancellationToken ct,
            bool travaillee = true)
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

            // UN BLOC QUI NE FAIT QUE RETIRER (signalement #67) n'ajoute rien.
            if (declare.Sujet is not null || declare.Notions.Count > 0)
            {
                await CompleterProgrammeAsync(conversationId, eleveId, matiereId, niveauScolaireId, declare, travaillee, ct);
            }

            // `retirer:` NE RETIRE PLUS RIEN (#67 v5) : le retrait se fait sur le oui de l'élève à la question fermée
            // (voir `RetraitConfirmeAsync`). Le serveur a déjà ajouté à la réponse la phrase vraie et la question
            // (`RetraitNonFaitDuTour`) ; ici, seulement la trace.
            foreach (var demande in declare.NotionsRetirees.Concat(declare.NotionsEnConflit).Distinct())
            {
                _logger.LogWarning(
                    "Retrait NON applique sur le controle {ControleId} (eleve {EleveId}, conversation {ConversationId}) : "
                    + "«{Demande}» — motif SansConfirmation (retirer: sans oui a la question fermee).",
                    declare.Controle, eleveId, conversationId, demande);
            }
        }

        /// <summary>
        /// LE RETRAIT CONFIRMÉ — #67 v5. Si le dernier message du professeur (<paramref name="dernier"/>) pose la question
        /// fermée au libellé exact (<see cref="ConfirmationDeRetrait.QuestionFermee"/>) et que l'élève y répond par un oui
        /// clair (<see cref="ConfirmationDeRetrait.OuiClair"/>), les notions demandées sortent du programme, au mot près.
        /// Rend le FAIT à donner au professeur avant sa réponse (retiré, ou pas, et pourquoi) ; vide sans question fermée.
        /// Chaque ligne supprimée est écrite EN ENTIER au journal (restaurable, #67 v2). Silencieux en cas d'échec : un
        /// retrait raté ne doit pas faire perdre la séance.
        /// </summary>
        private async Task<string> RetraitConfirmeAsync(
            int conversationId, int eleveId, int matiereId, int controleId,
            DomainMessage? dernier, string? messageEleve, CancellationToken ct)
        {
            if (dernier is null || !string.Equals(dernier.Role, "assistant", StringComparison.Ordinal)) return "";

            var demandees = ConfirmationDeRetrait.NotionsDemandees(dernier.Contenu);
            if (demandees.Count == 0) return "";

            // Le reste du programme : un oui qui nomme une AUTRE notion n'est pas clair.
            IReadOnlyList<string> programme = [];
            try
            {
                programme = (await _controles.GetPreparationAsync(eleveId, controleId, ct))?.Notions
                    .Select(n => n.Libelle).ToList() ?? [];
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Programme du controle {ControleId} illisible pour la confirmation.", controleId);
            }

            if (!ConfirmationDeRetrait.OuiClair(messageEleve, demandees, programme))
            {
                _logger.LogWarning(
                    "Retrait NON applique sur le controle {ControleId} (eleve {EleveId}, conversation {ConversationId}) : "
                    + "pas de oui clair a la question fermee ({Nombre} notion(s)).",
                    controleId, eleveId, conversationId, demandees.Count);

                return FaitDuRetrait(controleId, BilanRetrait.Vide, demandees, ouiClair: false, programme);
            }

            try
            {
                var bilan = await _controles.RetirerNotionsConfirmeesAsync(eleveId, controleId, matiereId, demandees, ct);

                foreach (var ligne in bilan.Retirees)
                {
                    // Avertissement, pas information : une donnée d'élève supprimée doit rester au journal même quand
                    // l'environnement ne garde que les avertissements (c'est le réglage de la copie de test).
                    _logger.LogWarning(
                        "Notion retiree du programme (restaurable) : {NotionRetiree}",
                        System.Text.Json.JsonSerializer.Serialize(ligne));
                }

                foreach (var r in bilan.Refus)
                {
                    _logger.LogWarning(
                        "Retrait NON applique sur le controle {ControleId} (eleve {EleveId}, conversation {ConversationId}) : "
                        + "«{Demande}» — motif {Motif}.", controleId, eleveId, conversationId, r.Demande, r.Motif);
                }

                // LE PROGRAMME RELU APRÈS LE RETRAIT, EN ENTIER (#67 v5, bout en bout v5R-2 : l'ardoise réécrite « sans
                // les titrages » oubliait aussi la 864, restée au programme) : le professeur recopie cette liste.
                var apres = bilan.Retirees.Count == 0 ? programme
                    : (await _controles.GetPreparationAsync(eleveId, controleId, ct))?.Notions.Select(n => n.Libelle).ToList()
                      ?? programme;

                return FaitDuRetrait(controleId, bilan, demandees, ouiClair: true, apres);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Echec du retrait de notions du programme (conversation {ConversationId}).", conversationId);
                return FaitDuRetrait(controleId, BilanRetrait.Vide, demandees, ouiClair: true, programme);
            }
        }

        /// <summary>
        /// Le FAIT du retrait confirmé, donné au professeur avant sa réponse (#67 v5). Il dit ce qui EST : retirée (et le
        /// tableau à réécrire sans elle), ou toujours dans son programme (pas de oui clair, programme qui se viderait,
        /// libellé qui ne correspond plus à aucune ligne).
        /// </summary>
        internal static string FaitDuRetrait(
            int controleId, BilanRetrait bilan, IReadOnlyList<string> demandees, bool ouiClair,
            IReadOnlyList<string>? programme = null)
        {
            if (demandees.Count == 0) return "";

            // Ce qui EST, relu en base : tout tableau du programme reprend exactement cette liste.
            var liste = programme is { Count: > 0 }
                ? " SON PROGRAMME, RELU APRÈS CE TOUR : " + string.Join(" ; ", programme.Select(n => $"« {n} »"))
                  + ". Tout tableau de son programme reprend EXACTEMENT ces notions, ni plus ni moins, et tu ne dis pas "
                  + "qu'il en reste moins."
                : "";

            if (!ouiClair)
            {
                return "\n\n[RETRAIT : tu lui as demandé « " + string.Join(" » et « ", demandees) + " » à retirer, et il n'a "
                    + "PAS répondu par un oui clair. RIEN N'A ÉTÉ RETIRÉ : ces notions sont TOUJOURS DANS SON PROGRAMME, "
                    + "entières. Ne dis pas qu'elles sont retirées. Réponds à ce qu'il te dit ; s'il veut toujours en "
                    + "retirer une, repose la question fermée, seule dans sa phrase : « Je retire toute la notion « … » de "
                    + "ton programme ? » (libellé exact de son programme), et attends son oui." + liste + "]";
            }

            var refusees = bilan.Refus.Select(r => r.Demande).ToHashSet();
            var phrases = new List<string>();
            if (bilan.Retirees.Count > 0)
            {
                phrases.Add("il a dit oui : « " + string.Join(" » et « ", demandees.Where(d => !refusees.Contains(d)))
                    + " » N'EST PLUS DANS SON PROGRAMME (c'est fait, par le serveur). Dis-le-lui en une phrase et réécris "
                    + "le tableau du programme sans elle. N'écris pas de `retirer:`");
            }

            foreach (var r in bilan.Refus)
            {
                phrases.Add(r.Motif == MotifRefusRetrait.Plancher
                    ? $"retirer « {r.Demande} » aurait laissé son programme VIDE : rien n'a été retiré, elle est TOUJOURS "
                      + "dans son programme ; demande-lui ce qu'il y aura à la place"
                    : $"« {r.Demande} » n'est PAS le libellé exact d'une notion de son programme : ce libellé n'a RIEN "
                      + "retiré. Ne dis pas qu'il est retiré ; si c'est un morceau d'une notion de la liste ci-dessous, "
                      + "cette notion est TOUJOURS là");
            }

            return "\n\n[RETRAIT : " + string.Join(" ; ", phrases) + "." + liste + "]";
        }

        /// <summary>
        /// LA PHRASE VRAIE DU RETRAIT NON FAIT, DANS LE MÊME TOUR — #67 v5 (verdict d'Omar, point 4). Le professeur a
        /// écrit `retirer:` : rien ne part sans le oui de l'élève à la question fermée. Pour chaque notion du programme
        /// visée, le serveur ajoute à la réponse « Je n'ai pas encore retiré « X » : elle est toujours dans ton
        /// programme. » puis la question fermée (sauf si la réponse la pose déjà). Le rapprochement entre ce que le
        /// professeur a écrit et la ligne du programme ne sert qu'à CHOISIR la question : il ne retire jamais rien. Null
        /// s'il n'y a rien à dire (pas de `retirer:`, ou plus aucune ligne visée).
        /// </summary>
        internal static string? RetraitNonFaitDuTour(string? texte, IReadOnlyList<string> programme)
        {
            if (string.IsNullOrWhiteSpace(texte) || programme.Count == 0) return null;

            var declare = LecteurControleNotions.Lire(texte);
            var demandes = declare is null ? [] : declare.NotionsRetirees.Concat(declare.NotionsEnConflit).ToList();

            var posees = ConfirmationDeRetrait.NotionsDemandees(texte);
            var dejaPosees = posees.Select(LibelleNotion.CleSouple).ToHashSet();
            var cles = programme.Select(LibelleNotion.CleSouple).ToHashSet();
            var phrases = new List<string>();
            var vues = new HashSet<string>();

            // UNE QUESTION FERMÉE AU LIBELLÉ INEXACT (#67 v5, bout en bout v5R-1 : « Identifier un couple acide-base de
            // Brønsted », tronqué) : un oui n'y retirerait rien. Si UNE seule notion du programme répond, la question au
            // libellé exact est ajoutée : c'est elle que le oui confirmera.
            foreach (var inexacte in posees.Where(p => !cles.Contains(LibelleNotion.CleSouple(p))))
            {
                var visees = NotionsVisees(inexacte, programme).ToList();
                if (visees.Count != 1 || dejaPosees.Contains(LibelleNotion.CleSouple(visees[0]))) continue;
                if (!vues.Add(LibelleNotion.CleSouple(visees[0]))) continue;

                phrases.Add($"Le nom exact de cette notion dans ton programme est « {visees[0]} ». "
                    + ConfirmationDeRetrait.QuestionFermee(visees[0]));
            }

            foreach (var notion in demandes.SelectMany(d => NotionsVisees(d, programme)))
            {
                var cle = LibelleNotion.CleSouple(notion);
                if (!vues.Add(cle)) continue;

                var phrase = $"Je n'ai pas encore retiré « {notion} » : elle est toujours dans ton programme.";
                if (!dejaPosees.Contains(cle)) phrase += " " + ConfirmationDeRetrait.QuestionFermee(notion);
                phrases.Add(phrase);
            }

            // UNE AFFIRMATION FAUSSE SUR CE QUI RESTE (#67 v5, verdict d'Omar point 4 : « ton programme n'a plus qu'une
            // seule notion » alors que la fiche en gardait deux, jusqu'à la fin) : la liste relue en base est ajoutée,
            // dans le même tour. Le serveur n'ajoute qu'une phrase vraie ; il ne retire rien.
            if (phrases.Count == 0 && AffirmeUnProgrammeReduit(texte, programme.Count))
            {
                phrases.Add($"Pour être exact, ton programme contient toujours {(programme.Count == 1 ? "cette notion" : $"ces {programme.Count} notions")} : "
                    + string.Join(" ; ", programme.Select(n => $"« {n} »")) + ".");
            }

            return phrases.Count == 0 ? null : "\n\n" + string.Join("\n\n", phrases);
        }

        /// <summary>
        /// La réponse dit-elle que le programme a rétréci (« n'a plus qu'une seule notion », « ne porte plus que sur la
        /// cinétique », « il ne reste que… », « déjà retirée », « plus rien à réviser ») alors que la base en garde
        /// <paramref name="nombre"/> ? Deux notions ou plus pour « plus que / il ne reste que » ; une ou plus pour « déjà
        /// retirée / écartée » et « plus rien ».
        /// </summary>
        internal static bool AffirmeUnProgrammeReduit(string? texte, int nombre)
        {
            if (string.IsNullOrWhiteSpace(texte) || nombre == 0) return false;

            var cle = $" {LibelleNotion.CleSouple(texte)} ";
            return (nombre >= 2 && PlusQue().IsMatch(cle)) || DejaRetireOuVide().IsMatch(cle);
        }

        [GeneratedRegex(@" (plus qu une( seule)? notion|(programme|il|elle) (ne |n )?(contient|porte|comporte|a|garde|compte) plus que|ne porte plus que|seule notion qui (te )?reste|il ne (te )?reste (plus )?que (la|le|les|l) notion|(bac blanc|programme|examen|controle|il|elle) (portera|porte|portent|porteront) (donc )?(uniquement|seulement) sur) ")]
        private static partial Regex PlusQue();

        [GeneratedRegex(@" (deja (ete )?(retire|retiree|retirees|retires|ecarte|ecartee|ecartees|ecartes|enleve|enlevee|enlevees|enleves|supprime|supprimee|supprimees)|plus rien (a reviser|dans ton programme|au programme|a retirer)|programme (est )?vide) ")]
        private static partial Regex DejaRetireOuVide();

        /// <summary>
        /// Les lignes du programme que vise ce que le professeur a écrit dans `retirer:` — POUR LA QUESTION SEULEMENT :
        /// au mot près ; sinon les lignes qui ont tous ses vrais mots (un morceau, « les espèces amphotères ») ; sinon
        /// celles contenues dans ce qu'il a écrit (un libellé allongé). Trois au plus.
        /// </summary>
        private static IEnumerable<string> NotionsVisees(string demande, IReadOnlyList<string> programme)
        {
            var cle = LibelleNotion.CleSouple(demande);
            if (cle.Length == 0) return [];

            var exactes = programme.Where(p => LibelleNotion.CleSouple(p) == cle).ToList();
            if (exactes.Count > 0) return exactes;

            var mots = VraisMots(cle);
            var avecTousSesMots = mots.Count == 0 ? [] : programme.Where(p => mots.IsSubsetOf(VraisMots(LibelleNotion.CleSouple(p)))).ToList();
            if (avecTousSesMots.Count > 0) return avecTousSesMots.Take(3);

            return programme
                .Where(p => LibelleNotion.CleSouple(p) is { Length: > 0 } c && $" {cle} ".Contains($" {c} ", StringComparison.Ordinal))
                .Take(3);
        }

        /// <summary>Les vrais mots d'une clé (4 lettres et plus), sans leur « s » ou « x » final.</summary>
        private static HashSet<string> VraisMots(string cle) =>
            cle.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(m => m.Length >= 4)
                .Select(m => m.Length > 4 && (m.EndsWith('s') || m.EndsWith('x')) ? m[..^1] : m)
                .ToHashSet();

        /// <summary>
        /// LE MORCEAU DE NOTION, DIT AVANT LA RÉPONSE — #67 v3, revu en v5. « Retire les espèces amphotères » ne nomme
        /// qu'un morceau de « Identifier un couple acide-base de Brønsted, une espèce amphotère… » : le professeur le sait
        /// avant de répondre, pour poser la question fermée sur la notion ENTIÈRE. Simple information : ce rapprochement
        /// ne retire jamais rien (seul le oui à la question fermée retire). Vide si son message ne nomme aucun morceau
        /// seul, ou s'il ne parle pas de retrait.
        /// </summary>
        internal static string MorceauNommeDuTour(string? message, IReadOnlyList<string> notions)
        {
            if (string.IsNullOrWhiteSpace(message) || notions.Count == 0 || !MorceauDeNotion.ParleDeRetrait(message)) return "";

            var morceaux = notions
                .Select((n, i) => (Notion: n, Morceau: MorceauDeNotion.Nomme(message, n, notions.Where((_, j) => j != i))))
                .Where(x => x.Morceau is not null)
                .Select(x => $"« {x.Morceau} » n'est qu'un morceau de la notion « {x.Notion} »")
                .ToList();

            if (morceaux.Count == 0) return "";

            return "\n\n[S'IL TE DEMANDE DE RETIRER CE QU'IL VIENT DE NOMMER : " + string.Join(" ; ", morceaux) + ". Cette "
                + "notion est TOUJOURS DANS SON PROGRAMME, entière : rien n'a été retiré, ne dis pas que c'est déjà fait. "
                + "Dis-lui de quelle notion ce morceau fait partie et qu'une notion se garde ou se retire en entier ; s'il "
                + "veut vraiment la retirer, pose-lui la question fermée sur la notion ENTIÈRE, seule dans sa phrase : « Je "
                + "retire toute la notion « … » de ton programme ? », et attends son oui.]";
        }

        /// <summary>Le sujet et les notions d'un bloc [CONTROLE_NOTIONS] : validation d'un examen blanc, ou ajout.</summary>
        private async Task CompleterProgrammeAsync(
            int conversationId, int eleveId, int matiereId, int? niveauScolaireId,
            ControleNotionsDeclarees declare, bool travaillee, CancellationToken ct)
        {
            try
            {
                // LE PROGRAMME D'UN EXAMEN BLANC (23/11/2026) : son premier bloc le valide. Traité à part, puis on s'arrête.
                if (await ValiderProgrammeExamenBlancAsync(eleveId, matiereId, niveauScolaireId, declare, ct)) return;

                // UNE DÉCLARATION INCOMPLÈTE SE COMPLÈTE EN PARLANT. « J'ai un
                // contrôle de maths » devient « les triangles semblables et le
                // théorème de Thalès » une fois que le professeur a posé la
                // question — et c'est ce sujet-là qui doit rester.
                //
                // SAUF SI LA FAMILLE A NOMMÉ LE CONTRÔLE (signalement #54) : son
                // titre reste, et la précision ne sert qu'à poser le programme.
                // Le dépôt tranche (`TitreDeControle.PeutEtreRemplace`).
                var precision = PrecisionDuSujet.Introuvable;

                if (declare.Sujet is not null)
                {
                    precision = await _controles.PreciserSujetAsync(
                        eleveId, declare.Controle, matiereId, declare.Sujet, ct);
                }

                var sujetPrecise = precision != PrecisionDuSujet.Introuvable;

                // La matière de la conversation, sans rapprochement possible :
                // poser le périmètre d'une préparation est un acte pédagogique,
                // un professeur ne le fait que chez lui. Le dépôt revérifie.
                var posees = declare.Notions.Count > 0
                    ? await AjouterNotionsAsync(
                        eleveId, declare.Controle, matiereId, niveauScolaireId,
                        declare.Notions, travaillee, ct)
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
                    declare.Controle, eleveId,
                    precision switch
                    {
                        PrecisionDuSujet.Remplace => "precise",
                        PrecisionDuSujet.TitreGarde => "titre de la famille garde",
                        _ => "inchange",
                    },
                    posees);
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

        /// <summary>
        /// Rapproche les libellés du référentiel : au mot près d'abord, à la
        /// plume près ensuite (accents, casse, « de repérer… ») — voir
        /// <see cref="LibelleNotion"/>. Ce qui reste est rendu en texte libre.
        /// </summary>
        private static IEnumerable<NotionDeclaree> Rapprocher(
            IReadOnlyList<string> libelles, List<CompetenceCandidate> referentiel) =>
            libelles.Select(libelle =>
            {
                var connue = referentiel.FirstOrDefault(
                    c => c.Libelle is not null
                        && LecteurBloc.Normaliser(c.Libelle) == LecteurBloc.Normaliser(libelle))
                    ?? referentiel.FirstOrDefault(
                        c => c.Libelle is not null
                            && LibelleNotion.CleSouple(c.Libelle) == LibelleNotion.CleSouple(libelle));

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
        /// LE PROGRAMME D'UN EXAMEN BLANC, VALIDÉ AU TABLEAU — Camara, le 23/11/2026 : « le professeur propose un
        /// programme de révision au tableau ; si l'enfant est d'accord, on officialise le programme ». Le professeur l'a
        /// proposé (consigne dans le contexte, AgentPedagogiqueService.ContexteExamenBlanc) ; son PREMIER
        /// [CONTROLE_NOTIONS] sur le contrôle caché de la matière est l'accord de l'élève.
        ///
        /// Les notions recopiées d'un dossier gardent la notion du référentiel de ce dossier (pas de rapprochement à
        /// refaire) ; celles ajoutées à la demande de l'élève passent par le rapprochement habituel. AUCUNE N'EST
        /// « TRAVAILLÉE » : on vient de se mettre d'accord sur le programme, pas de le réviser — la barre part de zéro.
        /// Rend faux si ce contrôle n'est pas celui d'un examen blanc, ou si son programme est déjà validé (le bloc
        /// suit alors le chemin ordinaire : des notions qu'on vient de travailler).
        /// </summary>
        private async Task<bool> ValiderProgrammeExamenBlancAsync(
            int eleveId, int matiereId, int? niveauScolaireId, ControleNotionsDeclarees declare, CancellationToken ct)
        {
            if (_examensBlancs is null || declare.Notions.Count == 0) return false;

            var examen = await _examensBlancs.GetParControleAsync(declare.Controle, ct);
            if (examen is null || examen.ProgrammeValideLe is not null) return false;

            var dossiers = examen.Notions
                .GroupBy(n => LibelleNotion.CleSouple(n.Libelle))
                .ToDictionary(g => g.Key, g => g.First());
            var reconnues = new List<NotionDeclaree>();
            var libres = new List<string>();

            foreach (var libelle in declare.Notions)
            {
                var cle = LibelleNotion.CleSouple(libelle);
                var dossier = dossiers.TryGetValue(cle, out var exact)
                    ? exact
                    : examen.Notions.Where(n =>
                        {
                            var sienne = LibelleNotion.CleSouple(n.Libelle);
                            return sienne.Length > 0 && cle.Length > 0 && (sienne.Contains(cle) || cle.Contains(sienne));
                        }).Take(2).ToList() is { Count: 1 } proches ? proches[0] : null;

                if (dossier?.CompetenceId is int competence) reconnues.Add(new NotionDeclaree(competence, dossier.Libelle));
                else libres.Add(dossier?.Libelle ?? libelle);
            }

            var posees = reconnues.Count > 0
                ? await _controles.AjouterNotionsAsync(eleveId, declare.Controle, matiereId, reconnues, travaillee: false, ct)
                : 0;

            if (libres.Count > 0)
            {
                posees += await AjouterNotionsAsync(
                    eleveId, declare.Controle, matiereId, niveauScolaireId, libres, travaillee: false, ct);
            }

            if (posees == 0) return false;

            await _examensBlancs.MarquerProgrammeValideAsync(declare.Controle, DateTime.UtcNow, ct);

            _logger.LogInformation(
                "Examen blanc {ExamenBlancId} : programme valide en {Matiere}, {Nombre} notion(s) (controle {ControleId}).",
                examen.ExamenBlancId, examen.MatiereLibelle, posees, declare.Controle);

            return true;
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

            var notions = Rapprocher(libelles, referentiel).ToList();

            // CE QUI N'EST PAS RECONNU À LA PLUME PRÈS PASSE PAR LE
            // PLANIFICATEUR — Camara, le 13/10/2026. « Les figures de style
            // dans un poème » et « Repérer comparaison, métaphore et
            // personnification » sont la même notion ; aucune comparaison de
            // lettres ne le voit, un professeur le voit tout de suite. Sans
            // rattachement, la notion ne peut JAMAIS être mesurée : sa barre
            // reste au plancher quoi que fasse l'élève. Un petit appel, une
            // seule fois, et seulement s'il reste quelque chose à rapprocher.
            var libres = notions.Where(n => n.CompetenceId is null).Select(n => n.Libelle).ToList();

            if (libres.Count > 0 && referentiel.Count > 0)
            {
                try
                {
                    var rapprochees = await _planificateur.RapprocherAsync(libres, referentiel, ct);

                    notions = notions
                        .Select(n => n.CompetenceId is null
                            && rapprochees.TryGetValue(n.Libelle, out var connue)
                                ? connue
                                : n)
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Rapprochement des notions impossible : {Nombre} gardee(s) en texte libre (controle {ControleId}).",
                        libres.Count, controleId);
                }
            }

            return await _controles.AjouterNotionsAsync(
                eleveId, controleId, matiereId, notions, travaillee, ct);
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

        /// <summary>
        /// Le modèle de voix du moment, sans jamais faire échouer le tour : une
        /// lecture de réglage ratée coûte une ligne de compte, pas une réponse.
        /// </summary>
        private async Task<string?> ModeleVoixAsync(CancellationToken ct)
        {
            try
            {
                return await _syntheseVocale.ModeleCourantAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Modele de voix du moment illisible : la reponse est enregistree sans.");
                return null;
            }
        }

        public async Task<VerdictQuota> VerifierQuotaAsync(
            int conversationId, CancellationToken ct = default)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);

            if (contexte is null) return VerdictQuota.Refus(MotifRefus.SansAbonnement);

            var verdict = await _abonnements.VerifierAsync(contexte.Eleve.Id, ct);

            // PAS DE CONSEILLÈRE AVEC STARTER (14/11/2026) : vérifié ici, avant que Maëlle n'ouvre la bouche — l'accueil
            // et chaque tour passent par là. Une famille qui redescend sur Starter en pleine séance est arrêtée au tour
            // suivant. Les cours ne sont pas concernés.
            if (verdict.Autorise
                && ConseillereOrientation.Est(contexte.Conversation.MatiereCode)
                && !await _acces.ConseillerePourEleveAsync(contexte.Eleve.Id, ct))
            {
                return VerdictQuota.Refus(MotifRefus.ConseillereNonIncluse);
            }

            return verdict;
        }

        public async Task<EcheanceForfait?> EcheanceAsync(int conversationId, CancellationToken ct = default)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId);
            if (contexte is null) return null;

            var echeance = await _abonnements.EcheanceAsync(contexte.Eleve.Id, ct);
            if (echeance is null) return null;

            // LE TOUR EN COURS N'EST PAS ENCORE DÉCOMPTÉ : il le sera à la fin de
            // la réponse (ImputerQuotaAsync), depuis le dernier message. On le
            // retire ici, sinon l'échéance donnée au navigateur arriverait trop
            // tard. Au-delà du plafond d'inactivité, l'attente n'est pas comptée.
            var dernier = (await _conversationService.GetMessagesAsync(conversationId, 1)).LastOrDefault()?.DateCreation;
            var enCours = 0;
            if (dernier is { } date)
            {
                // La pause n'est pas du temps de cours : elle ne rapproche pas la coupure.
                var pause = await _conversationService.LirePauseAsync(conversationId, ct);
                var ecoule = DateTime.UtcNow - date - TimeSpan.FromSeconds(pause);
                if (ecoule <= PlafondInactivite) enCours = Math.Max(0, (int)ecoule.TotalSeconds);
            }

            return echeance with { Secondes = Math.Max(0, echeance.Secondes - enCours) };
        }

        public async Task<bool> TerminerForfaitAsync(int conversationId, CancellationToken ct = default)
        {
            var contexte = await _resolver.ResoudreConversationAsync(conversationId)
                ?? throw new UnauthorizedAccessException();

            if (!await _abonnements.EpuiserAsync(contexte.Eleve.Id, ct)) return false;

            // QUITTER = RAPPORT DÛ, ici aussi : la séance coupée a eu lieu, le
            // parent doit en lire le compte rendu.
            await MarquerSortieAsync(conversationId);
            return true;
        }

        /// <summary>
        /// Impute le temps écoulé depuis le tour précédent.
        ///
        /// On mesure le temps RÉEL passé en séance plutôt que de facturer un
        /// forfait par tour : c'est ce que l'élève voit sur son chronomètre, et
        /// c'est ce qui correspond au coût — la voix se facture à la durée.
        ///
        /// LA PAUSE N'EN FAIT PAS PARTIE — Camara, le 03/11/2026 : « le timer s'arrête ».
        /// Sans ce retrait, deux minutes de pause se payaient au tour suivant, alors
        /// que le chronomètre de l'élève était resté figé.
        /// </summary>
        private async Task ImputerQuotaAsync(
            int eleveId, int conversationId, DateTime? tourPrecedent, CancellationToken ct)
        {
            try
            {
                var pause = await _conversationService.SolderPauseAsync(conversationId, ct);

                var ecoule = tourPrecedent is { } precedent
                    ? DateTime.UtcNow - precedent - TimeSpan.FromSeconds(pause)
                    : TimeSpan.MaxValue;

                var secondes = ecoule > PlafondInactivite
                    ? SecondesTourNominal
                    : Math.Max(SecondesTourNominal, (int)ecoule.TotalSeconds);

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
