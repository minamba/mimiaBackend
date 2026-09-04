using SchoolWebApp.Domain.Models;
using SchoolWebApp.Domain.Services;

namespace SchoolWebApp.Api.Utils
{
    public record ContexteConversation(Eleve Eleve, Conversation Conversation);

    /// <summary>
    /// Vérifie la chaîne de propriété JWT → Parent → Élève → Conversation.
    /// Chaque maillon doit être contrôlé : sans ça, il suffirait de changer un
    /// identifiant dans l'URL pour lire la conversation de l'enfant d'un autre.
    /// </summary>
    public interface IChatContexteResolver
    {
        /// <summary>Null si l'élève n'appartient pas au parent authentifié.</summary>
        Task<Eleve?> ResoudreEleveAsync(int eleveId);

        /// <summary>Null si la conversation ne remonte pas au parent authentifié.</summary>
        Task<ContexteConversation?> ResoudreConversationAsync(int conversationId);

        /// <summary>
        /// Le parent porteur du jeton, créé au premier appel s'il n'existe pas
        /// encore côté métier. Lève si le jeton n'a pas de claim `sub`.
        /// </summary>
        Task<Parent> ResoudreParentAsync();
    }

    public class ChatContexteResolver : IChatContexteResolver
    {
        private readonly ICurrentUserAccessor _currentUser;
        private readonly IParentService _parentService;
        private readonly IEleveService _eleveService;
        private readonly IConversationService _conversationService;

        public ChatContexteResolver(
            ICurrentUserAccessor currentUser,
            IParentService parentService,
            IEleveService eleveService,
            IConversationService conversationService)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
            _eleveService = eleveService ?? throw new ArgumentNullException(nameof(eleveService));
            _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
        }

        public async Task<Eleve?> ResoudreEleveAsync(int eleveId)
        {
            // UN ENFANT N'EST QUE LUI-MÊME, ET C'EST ICI QUE ÇA SE JOUE.
            //
            // Une session enfant porte le `sub` de son PARENT — c'est ce qui
            // fait fonctionner tous les contrôles d'appartenance sans les
            // réécrire. Mais du coup, aux yeux de ces contrôles, l'enfant est
            // son parent : il passerait donc sur les données de son frère.
            //
            // Le filtre des routes couvre les adresses qui portent un
            // `eleveId`. Il ne couvre PAS `/conversations/42/messages`, où 42
            // est la conversation. Or tout finit par passer ici : la
            // conversation remonte à son élève, et cet élève est vérifié ici.
            //
            // Une ligne à cet endroit ferme donc ce que cent contrôles
            // dispersés auraient laissé ouvert.
            if (_currentUser.EleveId is int enfant && enfant != eleveId) return null;

            var parent = await ResoudreParentAsync();
            return await _eleveService.GetEleveForParentAsync(eleveId, parent.Id);
        }

        public async Task<ContexteConversation?> ResoudreConversationAsync(int conversationId)
        {
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId);
            if (conversation is null) return null;

            // Remonte à l'élève, puis vérifie que cet élève est bien au parent authentifié.
            var eleve = await ResoudreEleveAsync(conversation.EleveId);
            if (eleve is null) return null;

            return new ContexteConversation(eleve, conversation);
        }

        public async Task<Parent> ResoudreParentAsync()
        {
            var identityUserId = _currentUser.IdentityUserId;
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                throw new UnauthorizedAccessException("Le jeton ne contient pas de claim `sub`.");
            }

            return await _parentService.GetOrCreateAsync(
                identityUserId, _currentUser.Mail, _currentUser.Prenom, _currentUser.Nom,

                // SEULEMENT SI CE N EST PAS L ENFANT. Son jeton porte le « sub »
                // du parent : sans ce test, chaque clic d un enfant ferait
                // passer son parent pour présent.
                noterLaVenue: _currentUser.EleveId is null);
        }
    }
}
